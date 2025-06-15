using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

/*
    这个类用于使用 BRG 渲染地面单元和碎片。
    地面单元与碎片都采用相同的 GPU 数据布局：
        - obj2world 矩阵（3 个 float4）
        - world2obj 矩阵（3 个 float4）
        - 颜色（1 个 float4）

    因此每个 mesh 总共 7 个 float4。

    注意数据以结构化数组（SoA）的形式存储。
*/
[BurstCompile]
public unsafe class BRG_Container
{
    // 在 GLES 模式下，BRG 原始缓冲区为常量缓冲区 (UBO)
    private bool UseConstantBuffer => BatchRendererGroup.BufferTarget == BatchBufferTarget.ConstantBuffer;
    private bool m_castShadows;

    private int m_maxInstances; // 容器中最大实例数
    private int m_instanceCount; // 当前实例数
    private int m_alignedGPUWindowSize; // BRG 原始窗口大小
    private int m_maxInstancePerWindow; // 每个窗口最多实例数
    private int m_windowCount; // 窗口数量（SSBO 模式下为 1，UBO 模式下可能为 n）
    private int m_totalGpuBufferSize; // 原始缓冲区总大小
    private NativeArray<float4> m_sysmemBuffer; // 原始 GPU 缓冲区在系统内存中的拷贝
    private bool m_initialized;
    private int m_instanceSize; // 每个实例的大小（字节数）
    private BatchID[] m_batchIDs; // 每个窗口对应一个 batchID
    private BatchMaterialID m_materialID;
    private BatchMeshID m_meshID;
    private BatchRendererGroup m_BatchRendererGroup; // BRG 对象
    private GraphicsBuffer m_GPUPersistentInstanceData; // GPU 持久实例数据缓冲区（可能是 SSBO 或 UBO）

    // 创建 BRG 对象并分配缓冲区
    public bool Init(Mesh mesh, Material mat, int maxInstances, int instanceSize, bool castShadows)
    {
        // 创建 BRG 对象，并指定 BRG 回调函数
        m_BatchRendererGroup = new BatchRendererGroup(this.OnPerformCulling, IntPtr.Zero);

        m_instanceSize = instanceSize;
        m_instanceCount = 0;
        m_maxInstances = maxInstances;
        m_castShadows = castShadows;

        // BRG 使用一个大型 GPU 缓冲区。在大多数平台上为 RAW 缓冲区，在 GLES 上为常量缓冲区
        // 如果是常量缓冲区，我们将其拆分为多个窗口，每个窗口大小为 BatchRendererGroup.GetConstantBufferMaxWindowSize() 字节
        if (UseConstantBuffer)
        {
            m_alignedGPUWindowSize = BatchRendererGroup.GetConstantBufferMaxWindowSize();
            m_maxInstancePerWindow = m_alignedGPUWindowSize / instanceSize;
            m_windowCount = (m_maxInstances + m_maxInstancePerWindow - 1) / m_maxInstancePerWindow;
            m_totalGpuBufferSize = m_windowCount * m_alignedGPUWindowSize;
            m_GPUPersistentInstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Constant, m_totalGpuBufferSize / 16, 16);
        }
        else
        {
            m_alignedGPUWindowSize = (m_maxInstances * instanceSize + 15) & (-16);
            m_maxInstancePerWindow = maxInstances;
            m_windowCount = 1;
            m_totalGpuBufferSize = m_windowCount * m_alignedGPUWindowSize;
            m_GPUPersistentInstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Raw, m_totalGpuBufferSize / 4, 4);
        }

        // 示例中，管理 3 个实例化属性：obj2world、world2obj 和 baseColor
        var batchMetadata = new NativeArray<MetadataValue>(3, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

        // 批处理元数据缓冲区
        int objectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");
        int worldToObjectID = Shader.PropertyToID("unity_WorldToObject");
        int colorID = Shader.PropertyToID("_BaseColor");

        // 创建大型 GPU 原始缓冲区的系统内存拷贝
        m_sysmemBuffer = new NativeArray<float4>(m_totalGpuBufferSize / 16, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        // 为大型 BRG 原始缓冲区中的每个“窗口”注册一个批次
        m_batchIDs = new BatchID[m_windowCount];
        for (int b = 0; b < m_windowCount; b++)
        {
            batchMetadata[0] = CreateMetadataValue(objectToWorldID, 0, true);       // 矩阵数据
            batchMetadata[1] = CreateMetadataValue(worldToObjectID, m_maxInstancePerWindow * 3 * 16, true); // 逆矩阵数据
            batchMetadata[2] = CreateMetadataValue(colorID, m_maxInstancePerWindow * 3 * 2 * 16, true); // 颜色数据
            int offset = b * m_alignedGPUWindowSize;
            m_batchIDs[b] = m_BatchRendererGroup.AddBatch(batchMetadata, m_GPUPersistentInstanceData.bufferHandle, (uint)offset, UseConstantBuffer ? (uint)m_alignedGPUWindowSize : 0);
        }

        // 不再需要这个元数据描述数组，释放它
        batchMetadata.Dispose();

        // 设置一个非常大的包围盒，确保 BRG 永远不会被剔除
        UnityEngine.Bounds bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1048576.0f, 1048576.0f, 1048576.0f));
        m_BatchRendererGroup.SetGlobalBounds(bounds);

        // 注册 mesh 和 material
        if (mesh) m_meshID = m_BatchRendererGroup.RegisterMesh(mesh);
        if (mat) m_materialID = m_BatchRendererGroup.RegisterMaterial(mat);

        m_initialized = true;
        return true;
    }

    // 根据 "实例数量" 上传最小 GPU 数据
    // 由于使用 SoA（结构化数组）并且此类管理 3 个 BRG 属性（2 个矩阵和 1 个颜色），最后一个窗口可能需要最多 3 次 SetData 调用
    [BurstCompile]
    public bool UploadGpuData(int instanceCount)
    {
        if ((uint)instanceCount > (uint)m_maxInstances)
            return false;

        m_instanceCount = instanceCount;
        int completeWindows = m_instanceCount / m_maxInstancePerWindow;

        // 一次性更新所有完整窗口的数据
        if (completeWindows > 0)
        {
            int sizeInFloat4 = (completeWindows * m_alignedGPUWindowSize) / 16;
            m_GPUPersistentInstanceData.SetData(m_sysmemBuffer, 0, 0, sizeInFloat4);
        }

        // 更新最后一个（不完整的）窗口的数据
        int lastBatchId = completeWindows;
        int itemInLastBatch = m_instanceCount - m_maxInstancePerWindow * completeWindows;

        if (itemInLastBatch > 0)
        {
            int windowOffsetInFloat4 = (lastBatchId * m_alignedGPUWindowSize) / 16;
            int offsetMat1 = windowOffsetInFloat4 + m_maxInstancePerWindow * 0;
            int offsetMat2 = windowOffsetInFloat4 + m_maxInstancePerWindow * 3;
            int offsetColor = windowOffsetInFloat4 + m_maxInstancePerWindow * 3 * 2;
            m_GPUPersistentInstanceData.SetData(m_sysmemBuffer, offsetMat1, offsetMat1, itemInLastBatch * 3);     // 3 个 float4 表示 obj2world
            m_GPUPersistentInstanceData.SetData(m_sysmemBuffer, offsetMat2, offsetMat2, itemInLastBatch * 3);    // 3 个 float4 表示 world2obj
            m_GPUPersistentInstanceData.SetData(m_sysmemBuffer, offsetColor, offsetColor, itemInLastBatch * 1);   // 1 个 float4 表示颜色
        }

        return true;
    }

    // 释放所有分配的缓冲区
    public void Shutdown()
    {
        if (m_initialized)
        {
            for (uint b = 0; b < m_windowCount; b++)
                m_BatchRendererGroup.RemoveBatch(m_batchIDs[b]);

            m_BatchRendererGroup.UnregisterMaterial(m_materialID);
            m_BatchRendererGroup.UnregisterMesh(m_meshID);
            m_BatchRendererGroup.Dispose();
            m_GPUPersistentInstanceData.Dispose();
            m_sysmemBuffer.Dispose();
        }
    }

    // 返回系统内存缓冲区和窗口大小，以便 BRG_Background 和 BRG_Debris 填充新内容
    public NativeArray<float4> GetSysmemBuffer(out int totalSize, out int alignedWindowSize)
    {
        totalSize = m_totalGpuBufferSize;
        alignedWindowSize = m_alignedGPUWindowSize;
        return m_sysmemBuffer;
    }

    // 辅助函数：创建 32 位元数据值。第 31 位（最高位）表示该属性为 per-instance（每个实例不同）
    static MetadataValue CreateMetadataValue(int nameID, int gpuOffset, bool isPerInstance)
    {
        const uint kIsPerInstanceBit = 0x80000000;
        return new MetadataValue
        {
            NameID = nameID,
            Value = (uint)gpuOffset | (isPerInstance ? (kIsPerInstanceBit) : 0),
        };
    }

    // 辅助函数：在 BRG 回调中分配缓冲区
    private static T* Malloc<T>(uint count) where T : unmanaged
    {
        return (T*)UnsafeUtility.Malloc(
            UnsafeUtility.SizeOf<T>() * count,
            UnsafeUtility.AlignOf<T>(),
            Allocator.TempJob);
    }

    // 每帧的主要 BRG 入口函数。此示例中不使用 BatchCullingContext，因为我们不需要剔除。
    // 这个回调负责填充 cullingOutput，提供渲染所有实例所需的绘制命令。
    [BurstCompile]
    public JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext)
    {
        if (m_initialized)
        {
            BatchCullingOutputDrawCommands drawCommands = new BatchCullingOutputDrawCommands();

            // 计算在 UBO 模式下需要的绘制命令数量（每个窗口对应一个绘制命令）
            int drawCommandCount = (m_instanceCount + m_maxInstancePerWindow - 1) / m_maxInstancePerWindow;
            int maxInstancePerDrawCommand = m_maxInstancePerWindow;
            drawCommands.drawCommandCount = drawCommandCount;

            // 分配一个 BatchDrawRange（所有绘制命令都将引用这个 BatchDrawRange）
            drawCommands.drawRangeCount = 1;
            drawCommands.drawRanges = Malloc<BatchDrawRange>(1);
            drawCommands.drawRanges[0] = new BatchDrawRange
            {
                drawCommandsBegin = 0,
                drawCommandsCount = (uint)drawCommandCount,
                filterSettings = new BatchFilterSettings
                {
                    renderingLayerMask = 1,
                    layer = 0,
                    motionMode = MotionVectorGenerationMode.Camera,
                    shadowCastingMode = m_castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                    receiveShadows = true,
                    staticShadowCaster = false,
                    allDepthSorted = false
                }
            };

            if (drawCommands.drawCommandCount > 0)
            {
                // 因为不需要剔除，所有绘制命令的可见性数组将始终是 {0,1,2,3,...}，
                // 所以只分配 maxInstancePerDrawCommand 的空间，并填充它。
                int visibilityArraySize = maxInstancePerDrawCommand;
                if (m_instanceCount < visibilityArraySize)
                    visibilityArraySize = m_instanceCount;

                drawCommands.visibleInstances = Malloc<int>((uint)visibilityArraySize);

                // 由于在我们的场景中不需要视锥体剔除，因此可见性数组直接填充 {0,1,2,3,...}
                for (int i = 0; i < visibilityArraySize; i++)
                    drawCommands.visibleInstances[i] = i;

                // 分配 BatchDrawCommand 数组（共有 drawCommandCount 个命令）
                // 在 SSBO 模式下，drawCommandCount 可能只有 1
                drawCommands.drawCommands = Malloc<BatchDrawCommand>((uint)drawCommandCount);
                int left = m_instanceCount;
                for (int b = 0; b < drawCommandCount; b++)
                {
                    int inBatchCount = left > maxInstancePerDrawCommand ? maxInstancePerDrawCommand : left;
                    drawCommands.drawCommands[b] = new BatchDrawCommand
                    {
                        visibleOffset = (uint)0,    // 所有绘制命令共享同一个 {0,1,2,3,...} 的可见性数组
                        visibleCount = (uint)inBatchCount,
                        batchID = m_batchIDs[b],
                        materialID = m_materialID,
                        meshID = m_meshID,
                        submeshIndex = 0,
                        splitVisibilityMask = 0xff,
                        flags = BatchDrawCommandFlags.None,
                        sortingPosition = 0
                    };
                    left -= inBatchCount;
                }
            }

            cullingOutput.drawCommands[0] = drawCommands;
            drawCommands.instanceSortingPositions = null;
            drawCommands.instanceSortingPositionFloatCount = 0;
        }

        return new JobHandle();
    }
}
