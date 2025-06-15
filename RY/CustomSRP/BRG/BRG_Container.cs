using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

/*
    ���������ʹ�� BRG ��Ⱦ���浥Ԫ����Ƭ��
    ���浥Ԫ����Ƭ��������ͬ�� GPU ���ݲ��֣�
        - obj2world ����3 �� float4��
        - world2obj ����3 �� float4��
        - ��ɫ��1 �� float4��

    ���ÿ�� mesh �ܹ� 7 �� float4��

    ע�������Խṹ�����飨SoA������ʽ�洢��
*/
[BurstCompile]
public unsafe class BRG_Container
{
    // �� GLES ģʽ�£�BRG ԭʼ������Ϊ���������� (UBO)
    private bool UseConstantBuffer => BatchRendererGroup.BufferTarget == BatchBufferTarget.ConstantBuffer;
    private bool m_castShadows;

    private int m_maxInstances; // ���������ʵ����
    private int m_instanceCount; // ��ǰʵ����
    private int m_alignedGPUWindowSize; // BRG ԭʼ���ڴ�С
    private int m_maxInstancePerWindow; // ÿ���������ʵ����
    private int m_windowCount; // ����������SSBO ģʽ��Ϊ 1��UBO ģʽ�¿���Ϊ n��
    private int m_totalGpuBufferSize; // ԭʼ�������ܴ�С
    private NativeArray<float4> m_sysmemBuffer; // ԭʼ GPU ��������ϵͳ�ڴ��еĿ���
    private bool m_initialized;
    private int m_instanceSize; // ÿ��ʵ���Ĵ�С���ֽ�����
    private BatchID[] m_batchIDs; // ÿ�����ڶ�Ӧһ�� batchID
    private BatchMaterialID m_materialID;
    private BatchMeshID m_meshID;
    private BatchRendererGroup m_BatchRendererGroup; // BRG ����
    private GraphicsBuffer m_GPUPersistentInstanceData; // GPU �־�ʵ�����ݻ������������� SSBO �� UBO��

    // ���� BRG ���󲢷��仺����
    public bool Init(Mesh mesh, Material mat, int maxInstances, int instanceSize, bool castShadows)
    {
        // ���� BRG ���󣬲�ָ�� BRG �ص�����
        m_BatchRendererGroup = new BatchRendererGroup(this.OnPerformCulling, IntPtr.Zero);

        m_instanceSize = instanceSize;
        m_instanceCount = 0;
        m_maxInstances = maxInstances;
        m_castShadows = castShadows;

        // BRG ʹ��һ������ GPU ���������ڴ����ƽ̨��Ϊ RAW ���������� GLES ��Ϊ����������
        // ����ǳ��������������ǽ�����Ϊ������ڣ�ÿ�����ڴ�СΪ BatchRendererGroup.GetConstantBufferMaxWindowSize() �ֽ�
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

        // ʾ���У����� 3 ��ʵ�������ԣ�obj2world��world2obj �� baseColor
        var batchMetadata = new NativeArray<MetadataValue>(3, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

        // ������Ԫ���ݻ�����
        int objectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");
        int worldToObjectID = Shader.PropertyToID("unity_WorldToObject");
        int colorID = Shader.PropertyToID("_BaseColor");

        // �������� GPU ԭʼ��������ϵͳ�ڴ濽��
        m_sysmemBuffer = new NativeArray<float4>(m_totalGpuBufferSize / 16, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        // Ϊ���� BRG ԭʼ�������е�ÿ�������ڡ�ע��һ������
        m_batchIDs = new BatchID[m_windowCount];
        for (int b = 0; b < m_windowCount; b++)
        {
            batchMetadata[0] = CreateMetadataValue(objectToWorldID, 0, true);       // ��������
            batchMetadata[1] = CreateMetadataValue(worldToObjectID, m_maxInstancePerWindow * 3 * 16, true); // ���������
            batchMetadata[2] = CreateMetadataValue(colorID, m_maxInstancePerWindow * 3 * 2 * 16, true); // ��ɫ����
            int offset = b * m_alignedGPUWindowSize;
            m_batchIDs[b] = m_BatchRendererGroup.AddBatch(batchMetadata, m_GPUPersistentInstanceData.bufferHandle, (uint)offset, UseConstantBuffer ? (uint)m_alignedGPUWindowSize : 0);
        }

        // ������Ҫ���Ԫ�����������飬�ͷ���
        batchMetadata.Dispose();

        // ����һ���ǳ���İ�Χ�У�ȷ�� BRG ��Զ���ᱻ�޳�
        UnityEngine.Bounds bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1048576.0f, 1048576.0f, 1048576.0f));
        m_BatchRendererGroup.SetGlobalBounds(bounds);

        // ע�� mesh �� material
        if (mesh) m_meshID = m_BatchRendererGroup.RegisterMesh(mesh);
        if (mat) m_materialID = m_BatchRendererGroup.RegisterMaterial(mat);

        m_initialized = true;
        return true;
    }

    // ���� "ʵ������" �ϴ���С GPU ����
    // ����ʹ�� SoA���ṹ�����飩���Ҵ������ 3 �� BRG ���ԣ�2 ������� 1 ����ɫ�������һ�����ڿ�����Ҫ��� 3 �� SetData ����
    [BurstCompile]
    public bool UploadGpuData(int instanceCount)
    {
        if ((uint)instanceCount > (uint)m_maxInstances)
            return false;

        m_instanceCount = instanceCount;
        int completeWindows = m_instanceCount / m_maxInstancePerWindow;

        // һ���Ը��������������ڵ�����
        if (completeWindows > 0)
        {
            int sizeInFloat4 = (completeWindows * m_alignedGPUWindowSize) / 16;
            m_GPUPersistentInstanceData.SetData(m_sysmemBuffer, 0, 0, sizeInFloat4);
        }

        // �������һ�����������ģ����ڵ�����
        int lastBatchId = completeWindows;
        int itemInLastBatch = m_instanceCount - m_maxInstancePerWindow * completeWindows;

        if (itemInLastBatch > 0)
        {
            int windowOffsetInFloat4 = (lastBatchId * m_alignedGPUWindowSize) / 16;
            int offsetMat1 = windowOffsetInFloat4 + m_maxInstancePerWindow * 0;
            int offsetMat2 = windowOffsetInFloat4 + m_maxInstancePerWindow * 3;
            int offsetColor = windowOffsetInFloat4 + m_maxInstancePerWindow * 3 * 2;
            m_GPUPersistentInstanceData.SetData(m_sysmemBuffer, offsetMat1, offsetMat1, itemInLastBatch * 3);     // 3 �� float4 ��ʾ obj2world
            m_GPUPersistentInstanceData.SetData(m_sysmemBuffer, offsetMat2, offsetMat2, itemInLastBatch * 3);    // 3 �� float4 ��ʾ world2obj
            m_GPUPersistentInstanceData.SetData(m_sysmemBuffer, offsetColor, offsetColor, itemInLastBatch * 1);   // 1 �� float4 ��ʾ��ɫ
        }

        return true;
    }

    // �ͷ����з���Ļ�����
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

    // ����ϵͳ�ڴ滺�����ʹ��ڴ�С���Ա� BRG_Background �� BRG_Debris ���������
    public NativeArray<float4> GetSysmemBuffer(out int totalSize, out int alignedWindowSize)
    {
        totalSize = m_totalGpuBufferSize;
        alignedWindowSize = m_alignedGPUWindowSize;
        return m_sysmemBuffer;
    }

    // �������������� 32 λԪ����ֵ���� 31 λ�����λ����ʾ������Ϊ per-instance��ÿ��ʵ����ͬ��
    static MetadataValue CreateMetadataValue(int nameID, int gpuOffset, bool isPerInstance)
    {
        const uint kIsPerInstanceBit = 0x80000000;
        return new MetadataValue
        {
            NameID = nameID,
            Value = (uint)gpuOffset | (isPerInstance ? (kIsPerInstanceBit) : 0),
        };
    }

    // ������������ BRG �ص��з��仺����
    private static T* Malloc<T>(uint count) where T : unmanaged
    {
        return (T*)UnsafeUtility.Malloc(
            UnsafeUtility.SizeOf<T>() * count,
            UnsafeUtility.AlignOf<T>(),
            Allocator.TempJob);
    }

    // ÿ֡����Ҫ BRG ��ں�������ʾ���в�ʹ�� BatchCullingContext����Ϊ���ǲ���Ҫ�޳���
    // ����ص�������� cullingOutput���ṩ��Ⱦ����ʵ������Ļ������
    [BurstCompile]
    public JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext)
    {
        if (m_initialized)
        {
            BatchCullingOutputDrawCommands drawCommands = new BatchCullingOutputDrawCommands();

            // ������ UBO ģʽ����Ҫ�Ļ�������������ÿ�����ڶ�Ӧһ���������
            int drawCommandCount = (m_instanceCount + m_maxInstancePerWindow - 1) / m_maxInstancePerWindow;
            int maxInstancePerDrawCommand = m_maxInstancePerWindow;
            drawCommands.drawCommandCount = drawCommandCount;

            // ����һ�� BatchDrawRange�����л��������������� BatchDrawRange��
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
                // ��Ϊ����Ҫ�޳������л�������Ŀɼ������齫ʼ���� {0,1,2,3,...}��
                // ����ֻ���� maxInstancePerDrawCommand �Ŀռ䣬���������
                int visibilityArraySize = maxInstancePerDrawCommand;
                if (m_instanceCount < visibilityArraySize)
                    visibilityArraySize = m_instanceCount;

                drawCommands.visibleInstances = Malloc<int>((uint)visibilityArraySize);

                // ���������ǵĳ����в���Ҫ��׶���޳�����˿ɼ�������ֱ����� {0,1,2,3,...}
                for (int i = 0; i < visibilityArraySize; i++)
                    drawCommands.visibleInstances[i] = i;

                // ���� BatchDrawCommand ���飨���� drawCommandCount �����
                // �� SSBO ģʽ�£�drawCommandCount ����ֻ�� 1
                drawCommands.drawCommands = Malloc<BatchDrawCommand>((uint)drawCommandCount);
                int left = m_instanceCount;
                for (int b = 0; b < drawCommandCount; b++)
                {
                    int inBatchCount = left > maxInstancePerDrawCommand ? maxInstancePerDrawCommand : left;
                    drawCommands.drawCommands[b] = new BatchDrawCommand
                    {
                        visibleOffset = (uint)0,    // ���л��������ͬһ�� {0,1,2,3,...} �Ŀɼ�������
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
