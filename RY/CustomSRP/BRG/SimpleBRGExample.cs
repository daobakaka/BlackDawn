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

        // ͨ�� Inspector ����ʵ��������ʵ��������ת�ٶ�
        public int instanceCount = 27; // Ĭ������ 27 ��ʵ�������� 3x3x3 �������岼�֣�
        public float spacing = 2.0f;     // ʵ��֮��ļ��
        public float rotationSpeed = 10.0f; // ÿ���� Y ����ת�ĽǶ�
        public bool enableUpdate = true;

        private BatchRendererGroup m_BRG;
        private GraphicsBuffer m_InstanceData;
        private BatchID m_BatchID;
        private BatchMeshID m_MeshID;
        private BatchMaterialID m_MaterialID;

        // һЩ�������������ڼ���
        private const int kSizeOfMatrix = sizeof(float) * 4 * 4;           // 4x4 ������ռ�ֽ���
        private const int kSizeOfPackedMatrix = sizeof(float) * 4 * 3;       // ѹ������PackedMatrix�����ֽ�����ֻ�洢ÿ��ǰ����������
        private const int kSizeOfFloat4 = sizeof(float) * 4;               // float4 ���ֽ���
        // ÿ��ʵ�������ֽ��� = 2 ��ѹ������obj2world �� world2obj��+ 1 �� float4����ɫ��
        private int kBytesPerInstance { get { return (kSizeOfPackedMatrix * 2) + kSizeOfFloat4; } }
        // ����Ԥ�����ֽ�����������룩������Ҫ�޸�
        private const int kExtraBytes = kSizeOfMatrix * 2;

        // ʹ�� NativeArray �洢ÿ��ʵ���ı任����ʹ�� math.float4x4�����Ա��� Job �и���
        private NativeArray<float4x4> m_NativeInstanceMatrices;
        // ���ڴ洢������ obj2world �� world2obj ���ݣ���ʹ���Զ���� PackedMatrix �ṹ
        private NativeArray<PackedMatrix> m_NativeObjectToWorld;
        private NativeArray<PackedMatrix> m_NativeWorldToObject;

        // ���ڳ�ʼ�����������岼��ʱ����ʱ���飨�����ڳ�ʼ��ʱת���� NativeArray��
        private Matrix4x4[] m_InstanceMatrices;

        // PackedMatrix���� Unity.Mathematics.float4x4 ת��Ϊѹ����ʽ��ֻ����ÿ��ǰ����������
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

        // �� UnityEngine.Matrix4x4 ת��Ϊ Unity.Mathematics.float4x4
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
            // ���� BatchRendererGroup����ָ���޳��ص����� OnPerformCulling
            m_BRG = new BatchRendererGroup(this.OnPerformCulling, IntPtr.Zero);
            m_MeshID = m_BRG.RegisterMesh(mesh);
            m_MaterialID = m_BRG.RegisterMaterial(material);

            // ��ʼ�� GPU ʵ�����ݻ�����
            AllocateInstanceDataBuffer();
            // ���������岼�ֵı任���󣬲�ת��Ϊ NativeArray ��ʽ�������� Job ����
            PopulateInstanceDataBuffer();
        }

        // ���� GraphicsBuffer ���ڴ洢ʵ������
        private void AllocateInstanceDataBuffer()
        {
            m_InstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Raw,
                BufferCountForInstances(kBytesPerInstance, instanceCount, kExtraBytes),
                sizeof(int));
        }

        // ��ʼ��ʵ�����ݣ����������岼�ֵĳ�ʼ�任���󣬲�ת���� NativeArray ��
        private void PopulateInstanceDataBuffer()
        {
            // 1. ��ʵ�����ݻ�������ʼλ�÷���һ�������ȷ���ӵ�ַ 0 ��ȡ������ֵ
            var zero = new Matrix4x4[1] { Matrix4x4.zero };

            // 2. �������岼������ instanceCount ��ʵ���ı任����
            int cubeSize = Mathf.CeilToInt(Mathf.Pow(instanceCount, 1f / 3f));
            float offset = (cubeSize - 1) * spacing * 0.5f; // �������ƫ����
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
                            // ʹ�� TRS ����任����λ�á�����ת����λ���ţ�
                            m_InstanceMatrices[idx] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
                            idx++;
                        }
                    }
                }
            }

            // �������� Matrix4x4 ����ת��Ϊ NativeArray<float4x4>��ʹ�� math.float4x4��
            m_NativeInstanceMatrices = new NativeArray<float4x4>(instanceCount, Allocator.Persistent);
            for (int i = 0; i < instanceCount; i++)
            {
                m_NativeInstanceMatrices[i] = ConvertToFloat4x4(m_InstanceMatrices[i]);
            }

            // �������ڴ洢��������� NativeArray
            m_NativeObjectToWorld = new NativeArray<PackedMatrix>(instanceCount, Allocator.Persistent);
            m_NativeWorldToObject = new NativeArray<PackedMatrix>(instanceCount, Allocator.Persistent);

            // 3. Ϊÿ��ʵ�����������ɫ������ʹ�������ɫ������ɫ����ͨ�� GraphicsBuffer �ϴ����������ϴ�һ�Σ�
            var colors = new Vector4[instanceCount];
            for (int i = 0; i < instanceCount; i++)
            {
                colors[i] = new Vector4(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1);
            }

            // 4. �趨���������ݲ���˵����
            // Offset | ����
            //      0 | 64 �ֽڵ���ֵ����֤�ӵ�ַ 0 ��ȡ������ֵ
            //     64 | 32 �ֽ�δ��ʼ�����򣨽�Ϊ SetData ���㣬�����룩
            //     96 | unity_ObjectToWorld��instanceCount ��ѹ������
            //    ... | unity_WorldToObject��instanceCount ��ѹ������
            //    ... | _BaseColor��instanceCount �� float4

            uint byteAddressObjectToWorld = (uint)(kSizeOfPackedMatrix * 2);
            uint byteAddressWorldToObject = byteAddressObjectToWorld + (uint)(kSizeOfPackedMatrix * instanceCount);
            uint byteAddressColor = byteAddressWorldToObject + (uint)(kSizeOfPackedMatrix * instanceCount);

            // 7. �ϴ����ݵ� GraphicsBuffer �У�����ֻ�ϴ���ɫ���ݣ����������Ժ�ͨ�� Job �����ϴ�
            m_InstanceData.SetData(zero, 0, 0, 1);
            m_InstanceData.SetData(colors, 0, (int)(byteAddressColor / kSizeOfFloat4), colors.Length);

            // 8. ����Ԫ���ݣ�����ָ��ÿ�������ڻ������е���ʼ�ֽڵ�ַ
            //    �����λ����Ϊ 0x80000000����֪ Shader ��Щ����Ϊ per-instance����ʵ���������ʣ�
            var metadata = new NativeArray<MetadataValue>(3, Allocator.Temp);
            metadata[0] = new MetadataValue { NameID = Shader.PropertyToID("unity_ObjectToWorld"), Value = 0x80000000 | byteAddressObjectToWorld };
            metadata[1] = new MetadataValue { NameID = Shader.PropertyToID("unity_WorldToObject"), Value = 0x80000000 | byteAddressWorldToObject };
            metadata[2] = new MetadataValue { NameID = Shader.PropertyToID("_BaseColor"), Value = 0x80000000 | byteAddressColor };

            // 9. �� GraphicsBuffer ��Ԫ���ݴ��ݸ� BatchRendererGroup���������Σ�
            //    ���� Shader ����Ⱦʱ����ȷ��ȡÿ��ʵ������
            m_BatchID = m_BRG.AddBatch(metadata, m_InstanceData.bufferHandle);
            metadata.Dispose();
        }

        // ��������� int ��������Ϊ GraphicsBuffer �� int Ϊ��λ�����ڴ�
        int BufferCountForInstances(int bytesPerInstance, int numInstances, int extraBytes = 0)
        {
            bytesPerInstance = (bytesPerInstance + sizeof(int) - 1) / sizeof(int) * sizeof(int);
            extraBytes = (extraBytes + sizeof(int) - 1) / sizeof(int) * sizeof(int);
            int totalBytes = bytesPerInstance * numInstances + extraBytes;
            return totalBytes / sizeof(int);
        }

        // Burst ����� Job�����ڲ��и���ÿ��ʵ���ľ���Ӧ�ù�����ת��ͬʱ����ÿ��ʵ���������ת��λ���Ŷ���
        [BurstCompile]
        private struct UpdateInstanceMatricesJob : IJobParallelFor
        {
            // ����Ĺ�����ת����math.float4x4��
            public float4x4 rotation;
            // ÿ��ʵ���ı任������ math.float4x4 �洢��
            public NativeArray<float4x4> instanceMatrices;
            // ����������� obj2world ����
            public NativeArray<PackedMatrix> objectToWorld;
            // ����������� world2obj ����
            public NativeArray<PackedMatrix> worldToObject;

            public void Execute(int index)
            {
                // ��ȡ��ǰʵ���ľ���
                float4x4 current = instanceMatrices[index];
                // ʹ�� index ����һ���򵥵�α���ֵ��Ϊ�Ŷ�����
                uint hash = (uint)index;
                // ����һ��С������Ƕȣ���λ�����ȣ����Ŷ���ΧԼΪ [0, 0.1] ����
                float randomAngle = math.frac(math.sin(hash * 12.9898f)) * 0.01f;
                // ����Χ�� Y ��������ת���Ŷ���
                quaternion randomRot = quaternion.AxisAngle(new float3(0, 1, 0), randomAngle);
                // ʹ�� TRS ���������ת����ƽ��Ϊ�㣬����Ϊ��λ����
                float4x4 randomRotMat = float4x4.TRS(float3.zero, randomRot, new float3(1, 1, 1));

                // ��Ϲ�����ת�������ת
                float4x4 combinedRot = math.mul(rotation, randomRotMat);

                // ���¾���Ϊ��combinedRot * current
                float4x4 updated = math.mul(combinedRot, current);

                // Ϊλ���Ŷ����һ��Сƫ��
                float3 pos = updated.c3.xyz;
                // ʹ�� hash ����λ���Ŷ����Ŷ���ΧԼ [-0.1, 0.1]
                float disturbX = (math.frac(math.sin(hash * 7.123f)) - 0.5f) * 0.2f;
                float disturbY = (math.frac(math.sin(hash * 13.37f)) - 0.5f) * 0.2f;
                float disturbZ = (math.frac(math.sin(hash * 9.99f)) - 0.5f) * 0.2f;
                pos += new float3(disturbX, disturbY, disturbZ);
                // ����ƽ�Ʋ���
                updated.c3 = new float4(pos, 1);

                // �����º�ľ���д��ʵ����������
                instanceMatrices[index] = updated;

                // �������������Ϊ obj2world ���ݣ�ֻ�洢ÿ�е�ǰ����������
                PackedMatrix pack;
                pack.c0x = updated.c0.x; pack.c0y = updated.c0.y; pack.c0z = updated.c0.z;
                pack.c1x = updated.c1.x; pack.c1y = updated.c1.y; pack.c1z = updated.c1.z;
                pack.c2x = updated.c2.x; pack.c2y = updated.c2.y; pack.c2z = updated.c2.z;
                pack.c3x = updated.c3.x; pack.c3y = updated.c3.y; pack.c3z = updated.c3.z;
                objectToWorld[index] = pack;

                // ���� updated ������󣬲������Ϊ world2obj ����
                float4x4 inv = math.inverse(updated);
                PackedMatrix packInv;
                packInv.c0x = inv.c0.x; packInv.c0y = inv.c0.y; packInv.c0z = inv.c0.z;
                packInv.c1x = inv.c1.x; packInv.c1y = inv.c1.y; packInv.c1z = inv.c1.z;
                packInv.c2x = inv.c2.x; packInv.c2y = inv.c2.y; packInv.c2z = inv.c2.z;
                packInv.c3x = inv.c3.x; packInv.c3y = inv.c3.y; packInv.c3z = inv.c3.z;
                worldToObject[index] = packInv;
            }
        }

        // Update() �е��� Job ����ʵ���任���󣬲��ϴ����º�ľ������ݵ� GPU ������
        private void Update()
        {
            if (enableUpdate)
            {
                // ʹ�� public �� rotationSpeed ���㹫����ת������ Y ����ת rotationSpeed * Time.deltaTime �ȣ�
                float angle = rotationSpeed * Time.deltaTime;
                // ע�⣺ʹ�� Unity.Mathematics.quaternion.Euler ������תʱ���Ƕ���Ҫת��Ϊ����
                quaternion mathQuat = quaternion.EulerXYZ(new float3(0, math.radians(angle), 0));
                float4x4 rotMat = float4x4.TRS(float3.zero, mathQuat, new float3(1, 1, 1));

                // ���� Burst Job ���и���ʵ������
                UpdateInstanceMatricesJob job = new UpdateInstanceMatricesJob()
                {
                    rotation = rotMat,
                    instanceMatrices = m_NativeInstanceMatrices,
                    objectToWorld = m_NativeObjectToWorld,
                    worldToObject = m_NativeWorldToObject,
                };
                JobHandle handle = job.Schedule(m_NativeInstanceMatrices.Length, 64);
                handle.Complete();

                // ��������������� GPU �������е���ʼ�ֽڵ�ַ���� PopulateInstanceDataBuffer() �еı���һ��
                uint byteAddressObjectToWorld = (uint)(kSizeOfPackedMatrix * 2);
                uint byteAddressWorldToObject = byteAddressObjectToWorld + (uint)(kSizeOfPackedMatrix * instanceCount);
                // �����º�ľ��������ϴ��� GPU ��������ֱ�Ӵ��� NativeArray������ GetUnsafePtr()��
                m_InstanceData.SetData(m_NativeObjectToWorld, 0, (int)(byteAddressObjectToWorld / kSizeOfPackedMatrix), m_NativeObjectToWorld.Length);
                m_InstanceData.SetData(m_NativeWorldToObject, 0, (int)(byteAddressWorldToObject / kSizeOfPackedMatrix), m_NativeWorldToObject.Length);
            }
        }

        // �� OnDisable �� OnDestroy ���ͷ����з������Դ
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

        // ÿ֡ BRG �޳��ص��������˺�������������������ʵ����Ϊ�ɼ��������޳���
        public unsafe JobHandle OnPerformCulling(
            BatchRendererGroup rendererGroup,
            BatchCullingContext cullingContext,
            BatchCullingOutput cullingOutput,
            IntPtr userContext)
        {
            // ��ȡ���뷽ʽ��ʹ�� long �Ķ���Ҫ����ΪĬ��ֵ
            int alignment = UnsafeUtility.AlignOf<long>();

            // ��ȡ BatchCullingOutputDrawCommands �ṹ��ָ�룬����ֱ���޸�
            var drawCommands = (BatchCullingOutputDrawCommands*)cullingOutput.drawCommands.GetUnsafePtr();

            // �����ڴ��������顣��ʾ����������ʵ�����ɼ������Ϊÿ��ʵ�������ڴ棺
            // - һ������������� instanceCount ��ʵ����
            // - һ�����Ʒ�Χ�����Ǹû������
            // - instanceCount ���ɼ�ʵ������
            // �������ʹ�� Allocator.TempJob ����
            drawCommands->drawCommands = (BatchDrawCommand*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BatchDrawCommand>(), alignment, Allocator.TempJob);
            drawCommands->drawRanges = (BatchDrawRange*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BatchDrawRange>(), alignment, Allocator.TempJob);
            drawCommands->visibleInstances = (int*)UnsafeUtility.Malloc(instanceCount * sizeof(int), alignment, Allocator.TempJob);
            drawCommands->drawCommandPickingInstanceIDs = null;

            drawCommands->drawCommandCount = 1;
            drawCommands->drawRangeCount = 1;
            drawCommands->visibleInstanceCount = instanceCount;

            // ��ʹ������������ instanceSortingPositions ��Ϊ null
            drawCommands->instanceSortingPositions = null;
            drawCommands->instanceSortingPositionFloatCount = 0;

            // ���õ�������������� instanceCount ��ʵ����
            // ʹ�� Start() ��ע��� batchID��materialID �� meshID����û�����������־
            drawCommands->drawCommands[0].visibleOffset = 0;
            drawCommands->drawCommands[0].visibleCount = (uint)instanceCount;
            drawCommands->drawCommands[0].batchID = m_BatchID;
            drawCommands->drawCommands[0].materialID = m_MaterialID;
            drawCommands->drawCommands[0].meshID = m_MeshID;
            drawCommands->drawCommands[0].submeshIndex = 0;
            drawCommands->drawCommands[0].splitVisibilityMask = 0xff;
            drawCommands->drawCommands[0].flags = 0;
            drawCommands->drawCommands[0].sortingPosition = 0;

            // ���û��Ʒ�Χ��ʹ�串��ƫ��Ϊ 0 �ĵ�����������
            drawCommands->drawRanges[0].drawCommandsBegin = 0;
            drawCommands->drawRanges[0].drawCommandsCount = 1;

            // ��ʾ����������Ӱ���˶�ʸ������˳��� renderingLayerMask ��Ϊȫ 1�����ౣ��Ĭ�� 0
            drawCommands->drawRanges[0].filterSettings = new BatchFilterSettings { renderingLayerMask = 0xffffffff, };

            // ���ɼ�ʵ������д�����顣��ʾ����������ʵ�����ɼ�
            for (int i = 0; i < instanceCount; ++i)
                drawCommands->visibleInstances[i] = i;

            // �˼�ʾ����ʹ�� Job ϵͳ����˷���һ���յ� JobHandle��
            // �������е�Ӧ�ý���ʹ�� Burst Job ��ʵ�ָ������޳�����������Ӧ�� JobHandle��
            return new JobHandle();
        }
    }
}
