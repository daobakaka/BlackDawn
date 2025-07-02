using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace BlackDawn
{
    public class SimpleBRGExample : MonoBehaviour
    {
        public Mesh mesh;
        public Material material;

        // 通过 Inspector 传入实例数量、实例间距和旋转速度
        public int instanceCount = 27; // 默认生成 27 个实例（例如 3x3x3 的正方体布局）
        public float spacing = 2.0f;     // 实例之间的间距
        public float rotationSpeed = 10.0f; // 每秒绕 Y 轴旋转的角度
        public bool enableUpdate = true;

        private BatchRendererGroup m_BRG;
        private GraphicsBuffer m_InstanceData;
        private BatchID m_BatchID;
        private BatchMeshID m_MeshID;
        private BatchMaterialID m_MaterialID;

        // 一些辅助常量，便于计算
        private const int kSizeOfMatrix = sizeof(float) * 4 * 4;           // 4x4 矩阵所占字节数
        private const int kSizeOfPackedMatrix = sizeof(float) * 4 * 3;       // 压缩矩阵（PackedMatrix）的字节数（只存储每列前三个分量）
        private const int kSizeOfFloat4 = sizeof(float) * 4;               // float4 的字节数
        // 每个实例所需字节数 = 2 个压缩矩阵（obj2world 和 world2obj）+ 1 个 float4（颜色）
        private int kBytesPerInstance { get { return (kSizeOfPackedMatrix * 2) + kSizeOfFloat4; } }
        // 额外预留的字节数（例如对齐），不需要修改
        private const int kExtraBytes = kSizeOfMatrix * 2;

        // 使用 NativeArray 存储每个实例的变换矩阵（使用 math.float4x4），以便在 Job 中更新
        private NativeArray<float4x4> m_NativeInstanceMatrices;
        // 用于存储打包后的 obj2world 和 world2obj 数据，均使用自定义的 PackedMatrix 结构
        private NativeArray<PackedMatrix> m_NativeObjectToWorld;
        private NativeArray<PackedMatrix> m_NativeWorldToObject;

        // 用于初始化构造正方体布局时的临时数组（仅用于初始化时转换到 NativeArray）
        private Matrix4x4[] m_InstanceMatrices;

        // PackedMatrix：将 Unity.Mathematics.float4x4 转换为压缩格式（只保留每列前三个分量）
        public struct PackedMatrix
        {
            public float c0x;
            public float c0y;
            public float c0z;
            public float c1x;
            public float c1y;
            public float c1z;
            public float c2x;
            public float c2y;
            public float c2z;
            public float c3x;
            public float c3y;
            public float c3z;
        }

        // 将 UnityEngine.Matrix4x4 转换为 Unity.Mathematics.float4x4
        private float4x4 ConvertToFloat4x4(Matrix4x4 m)
        {
            return new float4x4(
                new float4(m.m00, m.m10, m.m20, m.m30),
                new float4(m.m01, m.m11, m.m21, m.m31),
                new float4(m.m02, m.m12, m.m22, m.m32),
                new float4(m.m03, m.m13, m.m23, m.m33)
            );
        }

        private void Start()
        {
            // 创建 BatchRendererGroup，并指定剔除回调函数 OnPerformCulling
            m_BRG = new BatchRendererGroup(this.OnPerformCulling, IntPtr.Zero);
            m_MeshID = m_BRG.RegisterMesh(mesh);
            m_MaterialID = m_BRG.RegisterMaterial(material);

            // 初始化 GPU 实例数据缓冲区
            AllocateInstanceDataBuffer();
            // 生成正方体布局的变换矩阵，并转换为 NativeArray 格式，供后续 Job 更新
            PopulateInstanceDataBuffer();
        }

        // 分配 GraphicsBuffer 用于存储实例数据
        private void AllocateInstanceDataBuffer()
        {
            m_InstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Raw,
                BufferCountForInstances(kBytesPerInstance, instanceCount, kExtraBytes),
                sizeof(int));
        }

        // 初始化实例数据：生成正方体布局的初始变换矩阵，并转换到 NativeArray 中
        private void PopulateInstanceDataBuffer()
        {
            // 1. 在实例数据缓冲区开始位置放置一个零矩阵，确保从地址 0 读取返回零值
            var zero = new Matrix4x4[1] { Matrix4x4.zero };

            // 2. 按正方体布局生成 instanceCount 个实例的变换矩阵
            int cubeSize = Mathf.CeilToInt(Mathf.Pow(instanceCount, 1f / 3f));
            float offset = (cubeSize - 1) * spacing * 0.5f; // 计算居中偏移量
            m_InstanceMatrices = new Matrix4x4[instanceCount];
            int idx = 0;
            for (int x = 0; x < cubeSize; x++)
            {
                for (int y = 0; y < cubeSize; y++)
                {
                    for (int z = 0; z < cubeSize; z++)
                    {
                        if (idx < instanceCount)
                        {
                            Vector3 pos = new Vector3(x * spacing - offset, y * spacing - offset, z * spacing - offset);
                            // 使用 TRS 构造变换矩阵（位置、无旋转、单位缩放）
                            m_InstanceMatrices[idx] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
                            idx++;
                        }
                    }
                }
            }

            // 将管理型 Matrix4x4 数组转换为 NativeArray<float4x4>（使用 math.float4x4）
            m_NativeInstanceMatrices = new NativeArray<float4x4>(instanceCount, Allocator.Persistent);
            for (int i = 0; i < instanceCount; i++)
            {
                m_NativeInstanceMatrices[i] = ConvertToFloat4x4(m_InstanceMatrices[i]);
            }

            // 分配用于存储打包后矩阵的 NativeArray
            m_NativeObjectToWorld = new NativeArray<PackedMatrix>(instanceCount, Allocator.Persistent);
            m_NativeWorldToObject = new NativeArray<PackedMatrix>(instanceCount, Allocator.Persistent);

            // 3. 为每个实例生成随机颜色（这里使用随机颜色），颜色数据通过 GraphicsBuffer 上传（本例仅上传一次）
            var colors = new Vector4[instanceCount];
            for (int i = 0; i < instanceCount; i++)
            {
                colors[i] = new Vector4(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1);
            }

            // 4. 设定缓冲区数据布局说明：
            // Offset | 描述
            //      0 | 64 字节的零值，保证从地址 0 读取返回零值
            //     64 | 32 字节未初始化区域（仅为 SetData 方便，不必须）
            //     96 | unity_ObjectToWorld，instanceCount 个压缩矩阵
            //    ... | unity_WorldToObject，instanceCount 个压缩矩阵
            //    ... | _BaseColor，instanceCount 个 float4

            uint byteAddressObjectToWorld = (uint)(kSizeOfPackedMatrix * 2);
            uint byteAddressWorldToObject = byteAddressObjectToWorld + (uint)(kSizeOfPackedMatrix * instanceCount);
            uint byteAddressColor = byteAddressWorldToObject + (uint)(kSizeOfPackedMatrix * instanceCount);

            // 7. 上传数据到 GraphicsBuffer 中；这里只上传颜色数据，矩阵数据以后通过 Job 更新上传
            m_InstanceData.SetData(zero, 0, 0, 1);
            m_InstanceData.SetData(colors, 0, (int)(byteAddressColor / kSizeOfFloat4), colors.Length);

            // 8. 设置元数据，用于指明每个属性在缓冲区中的起始字节地址
            //    将最高位设置为 0x80000000，告知 Shader 这些数据为 per-instance（按实例索引访问）
            var metadata = new NativeArray<MetadataValue>(3, Allocator.Temp);
            metadata[0] = new MetadataValue { NameID = Shader.PropertyToID("unity_ObjectToWorld"), Value = 0x80000000 | byteAddressObjectToWorld };
            metadata[1] = new MetadataValue { NameID = Shader.PropertyToID("unity_WorldToObject"), Value = 0x80000000 | byteAddressWorldToObject };
            metadata[2] = new MetadataValue { NameID = Shader.PropertyToID("_BaseColor"), Value = 0x80000000 | byteAddressColor };

            // 9. 将 GraphicsBuffer 和元数据传递给 BatchRendererGroup，创建批次，
            //    这样 Shader 在渲染时能正确读取每个实例数据
            m_BatchID = m_BRG.AddBatch(metadata, m_InstanceData.bufferHandle);
            metadata.Dispose();
        }

        // 计算所需的 int 数量，因为 GraphicsBuffer 以 int 为单位分配内存
        int BufferCountForInstances(int bytesPerInstance, int numInstances, int extraBytes = 0)
        {
            bytesPerInstance = (bytesPerInstance + sizeof(int) - 1) / sizeof(int) * sizeof(int);
            extraBytes = (extraBytes + sizeof(int) - 1) / sizeof(int) * sizeof(int);
            int totalBytes = bytesPerInstance * numInstances + extraBytes;
            return totalBytes / sizeof(int);
        }

        // Burst 编译的 Job，用于并行更新每个实例的矩阵（应用公共旋转，同时增加每个实例的随机旋转和位置扰动）
        [BurstCompile]
        private struct UpdateInstanceMatricesJob : IJobParallelFor
        {
            // 传入的公共旋转矩阵（math.float4x4）
            public float4x4 rotation;
            // 每个实例的变换矩阵（以 math.float4x4 存储）
            public NativeArray<float4x4> instanceMatrices;
            // 输出：打包后的 obj2world 数据
            public NativeArray<PackedMatrix> objectToWorld;
            // 输出：打包后的 world2obj 数据
            public NativeArray<PackedMatrix> worldToObject;

            public void Execute(int index)
            {
                // 获取当前实例的矩阵
                float4x4 current = instanceMatrices[index];
                // 使用 index 生成一个简单的伪随机值作为扰动种子
                uint hash = (uint)index;
                // 生成一个小的随机角度（单位：弧度），扰动范围约为 [0, 0.1] 弧度
                float randomAngle = math.frac(math.sin(hash * 12.9898f)) * 0.01f;
                // 生成围绕 Y 轴的随机旋转（扰动）
                quaternion randomRot = quaternion.AxisAngle(new float3(0, 1, 0), randomAngle);
                // 使用 TRS 构造随机旋转矩阵，平移为零，缩放为单位向量
                float4x4 randomRotMat = float4x4.TRS(float3.zero, randomRot, new float3(1, 1, 1));

                // 组合公共旋转和随机旋转
                float4x4 combinedRot = math.mul(rotation, randomRotMat);

                // 更新矩阵为：combinedRot * current
                float4x4 updated = math.mul(combinedRot, current);

                // 为位置扰动添加一个小偏移
                float3 pos = updated.c3.xyz;
                // 使用 hash 生成位置扰动，扰动范围约 [-0.1, 0.1]
                float disturbX = (math.frac(math.sin(hash * 7.123f)) - 0.5f) * 0.2f;
                float disturbY = (math.frac(math.sin(hash * 13.37f)) - 0.5f) * 0.2f;
                float disturbZ = (math.frac(math.sin(hash * 9.99f)) - 0.5f) * 0.2f;
                pos += new float3(disturbX, disturbY, disturbZ);
                // 更新平移部分
                updated.c3 = new float4(pos, 1);

                // 将更新后的矩阵写回实例矩阵数组
                instanceMatrices[index] = updated;

                // 打包矩阵数据作为 obj2world 数据（只存储每列的前三个分量）
                PackedMatrix pack;
                pack.c0x = updated.c0.x; pack.c0y = updated.c0.y; pack.c0z = updated.c0.z;
                pack.c1x = updated.c1.x; pack.c1y = updated.c1.y; pack.c1z = updated.c1.z;
                pack.c2x = updated.c2.x; pack.c2y = updated.c2.y; pack.c2z = updated.c2.z;
                pack.c3x = updated.c3.x; pack.c3y = updated.c3.y; pack.c3z = updated.c3.z;
                objectToWorld[index] = pack;

                // 计算 updated 的逆矩阵，并打包作为 world2obj 数据
                float4x4 inv = math.inverse(updated);
                PackedMatrix packInv;
                packInv.c0x = inv.c0.x; packInv.c0y = inv.c0.y; packInv.c0z = inv.c0.z;
                packInv.c1x = inv.c1.x; packInv.c1y = inv.c1.y; packInv.c1z = inv.c1.z;
                packInv.c2x = inv.c2.x; packInv.c2y = inv.c2.y; packInv.c2z = inv.c2.z;
                packInv.c3x = inv.c3.x; packInv.c3y = inv.c3.y; packInv.c3z = inv.c3.z;
                worldToObject[index] = packInv;
            }
        }

        // Update() 中调用 Job 更新实例变换矩阵，并上传更新后的矩阵数据到 GPU 缓冲区
        private void Update()
        {
            if (enableUpdate)
            {
                // 使用 public 的 rotationSpeed 计算公共旋转矩阵（绕 Y 轴旋转 rotationSpeed * Time.deltaTime 度）
                float angle = rotationSpeed * Time.deltaTime;
                // 注意：使用 Unity.Mathematics.quaternion.Euler 生成旋转时，角度需要转换为弧度
                quaternion mathQuat = quaternion.EulerXYZ(new float3(0, math.radians(angle), 0));
                float4x4 rotMat = float4x4.TRS(float3.zero, mathQuat, new float3(1, 1, 1));

                // 调度 Burst Job 并行更新实例矩阵
                UpdateInstanceMatricesJob job = new UpdateInstanceMatricesJob()
                {
                    rotation = rotMat,
                    instanceMatrices = m_NativeInstanceMatrices,
                    objectToWorld = m_NativeObjectToWorld,
                    worldToObject = m_NativeWorldToObject,
                };
                JobHandle handle = job.Schedule(m_NativeInstanceMatrices.Length, 64);
                handle.Complete();

                // 计算各部分数据在 GPU 缓冲区中的起始字节地址，与 PopulateInstanceDataBuffer() 中的保持一致
                uint byteAddressObjectToWorld = (uint)(kSizeOfPackedMatrix * 2);
                uint byteAddressWorldToObject = byteAddressObjectToWorld + (uint)(kSizeOfPackedMatrix * instanceCount);
                // 将更新后的矩阵数据上传到 GPU 缓冲区（直接传入 NativeArray，无需 GetUnsafePtr()）
                m_InstanceData.SetData(m_NativeObjectToWorld, 0, (int)(byteAddressObjectToWorld / kSizeOfPackedMatrix), m_NativeObjectToWorld.Length);
                m_InstanceData.SetData(m_NativeWorldToObject, 0, (int)(byteAddressWorldToObject / kSizeOfPackedMatrix), m_NativeWorldToObject.Length);
            }
        }

        // 在 OnDisable 和 OnDestroy 中释放所有分配的资源
        private void OnDisable()
        {
            m_BRG.Dispose();
            m_InstanceData.Dispose();
            if (m_NativeInstanceMatrices.IsCreated) m_NativeInstanceMatrices.Dispose();
            if (m_NativeObjectToWorld.IsCreated) m_NativeObjectToWorld.Dispose();
            if (m_NativeWorldToObject.IsCreated) m_NativeWorldToObject.Dispose();
        }

        private void OnDestroy()
        {
            m_BRG.Dispose();
            m_InstanceData.Dispose();
            if (m_NativeInstanceMatrices.IsCreated) m_NativeInstanceMatrices.Dispose();
            if (m_NativeObjectToWorld.IsCreated) m_NativeObjectToWorld.Dispose();
            if (m_NativeWorldToObject.IsCreated) m_NativeWorldToObject.Dispose();
        }

        // 每帧 BRG 剔除回调函数。此函数构造绘制命令，将所有实例视为可见（不做剔除）
        public unsafe JobHandle OnPerformCulling(
            BatchRendererGroup rendererGroup,
            BatchCullingContext cullingContext,
            BatchCullingOutput cullingOutput,
            IntPtr userContext)
        {
            // 获取对齐方式，使用 long 的对齐要求作为默认值
            int alignment = UnsafeUtility.AlignOf<long>();

            // 获取 BatchCullingOutputDrawCommands 结构的指针，便于直接修改
            var drawCommands = (BatchCullingOutputDrawCommands*)cullingOutput.drawCommands.GetUnsafePtr();

            // 分配内存给输出数组。此示例假设所有实例都可见，因此为每个实例分配内存：
            // - 一个绘制命令（绘制 instanceCount 个实例）
            // - 一个绘制范围（覆盖该绘制命令）
            // - instanceCount 个可见实例索引
            // 数组必须使用 Allocator.TempJob 分配
            drawCommands->drawCommands = (BatchDrawCommand*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BatchDrawCommand>(), alignment, Allocator.TempJob);
            drawCommands->drawRanges = (BatchDrawRange*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BatchDrawRange>(), alignment, Allocator.TempJob);
            drawCommands->visibleInstances = (int*)UnsafeUtility.Malloc(instanceCount * sizeof(int), alignment, Allocator.TempJob);
            drawCommands->drawCommandPickingInstanceIDs = null;

            drawCommands->drawCommandCount = 1;
            drawCommands->drawRangeCount = 1;
            drawCommands->visibleInstanceCount = instanceCount;

            // 不使用深度排序，因此 instanceSortingPositions 设为 null
            drawCommands->instanceSortingPositions = null;
            drawCommands->instanceSortingPositionFloatCount = 0;

            // 配置单个绘制命令，绘制 instanceCount 个实例，
            // 使用 Start() 中注册的 batchID、materialID 和 meshID，且没有设置特殊标志
            drawCommands->drawCommands[0].visibleOffset = 0;
            drawCommands->drawCommands[0].visibleCount = (uint)instanceCount;
            drawCommands->drawCommands[0].batchID = m_BatchID;
            drawCommands->drawCommands[0].materialID = m_MaterialID;
            drawCommands->drawCommands[0].meshID = m_MeshID;
            drawCommands->drawCommands[0].submeshIndex = 0;
            drawCommands->drawCommands[0].splitVisibilityMask = 0xff;
            drawCommands->drawCommands[0].flags = 0;
            drawCommands->drawCommands[0].sortingPosition = 0;

            // 配置绘制范围，使其覆盖偏移为 0 的单个绘制命令
            drawCommands->drawRanges[0].drawCommandsBegin = 0;
            drawCommands->drawRanges[0].drawCommandsCount = 1;

            // 此示例不关心阴影或运动矢量，因此除了 renderingLayerMask 设为全 1，其余保持默认 0
            drawCommands->drawRanges[0].filterSettings = new BatchFilterSettings { renderingLayerMask = 0xffffffff, };

            // 将可见实例索引写入数组。此示例假设所有实例都可见
            for (int i = 0; i < instanceCount; ++i)
                drawCommands->visibleInstances[i] = i;

            // 此简单示例不使用 Job 系统，因此返回一个空的 JobHandle。
            // 性能敏感的应用建议使用 Burst Job 来实现高性能剔除，并返回相应的 JobHandle。
            return new JobHandle();
        }
    }
}
