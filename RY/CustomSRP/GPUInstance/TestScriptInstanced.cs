using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;

namespace BlackDawn
{
    public class TestScriptInstanced : MonoBehaviour
    {
        // Ԥ���壬������� MeshFilter �� MeshRenderer ���
        public GameObject prefab;
        // ���ڼ�ӻ���ʱʹ�õĲ��ʣ�Shader ��֧�ִ� StructuredBuffer ��ȡʵ�����ݣ�
        public GameObject indirectPrefab;
        public float spacing = 2.0f;           // ����֮��ļ��
        public float rotationSpeed = 10.0f;      // ��ת�ٶȣ���/�룩
        public int counter = 100;              // ����ʵ������
        public int rotate = 1;                 // ��ת����1 �� -1��
        public bool enableUpdate = true;
        // �Ƿ�ʹ�ü�ӻ���
        public bool enableIndricetDraw = false;

        // ���� DrawMeshInstanced ���õĹ����;�������
        private Matrix4x4[] matrices;
        // �洢ÿ��ʵ������ɫ������ GPU Instancing��
        private List<Vector4> instanceColors = new List<Vector4>();

        // ���� Mesh �� Material
        private Mesh instanceMesh;
        private Material instanceMaterial;
        // ���ڼ�ӻ���ʱʹ�õĲ��ʣ��� Shader ��Ҫ֧�� StructuredBuffer ��ȡʵ���任���ݣ�
        private Material indirectInstanceMaterial;

        // ���� Job ������ӵ� NativeArray���洢����ʵ���ľ���ʹ�� UnityEngine.Matrix4x4��
        private NativeArray<Matrix4x4> nativeMatrices;

        // �־� MaterialPropertyBlock������ÿ֡����
        private MaterialPropertyBlock mpb;

        // ���� DrawMeshInstancedIndirect �Ĳ���������
        private ComputeBuffer argumentBuffer;
        // ���ڼ�ӻ���ʱ�洢ʵ���任���ݵ� ComputeBuffer
        private ComputeBuffer instanceTransformBuffer;

        // �̶����λ��ƻ�����������ÿ֡ new ���飩��DrawMeshInstanced ÿ����� 1023 ��ʵ��
        private Matrix4x4[] batchMatrixBuffer;

        void Start()
        {
            // ��ȡԤ����� Mesh �� Material
            MeshFilter mf = prefab.GetComponent<MeshFilter>();
            MeshRenderer mr = prefab.GetComponent<MeshRenderer>();
            if (mf == null || mr == null)
            {
                Debug.LogError("Prefab must have MeshFilter and MeshRenderer components.");
                return;
            }
            instanceMesh = mf.sharedMesh;
            instanceMaterial = mr.sharedMaterial;

            // ���ڼ�ӻ��ƣ����Ǽ��� indirectPrefab �� MeshRenderer ʹ�õĲ���֧�� StructuredBuffer ��ȡʵ������
            MeshRenderer mri = indirectPrefab.GetComponent<MeshRenderer>();
            if (mri != null)
            {
                indirectInstanceMaterial = mri.sharedMaterial;
            }
            else
            {
                indirectInstanceMaterial = instanceMaterial;
            }

            // ��ʼ�� MPB������ÿ֡�½�
            mpb = new MaterialPropertyBlock();

            // ��̬���� xSize, ySize, zSize �Ծ����γ������岼��
            int xSize = Mathf.CeilToInt(Mathf.Pow(counter, 1f / 3f));
            int ySize = Mathf.CeilToInt(Mathf.Sqrt(counter / (float)xSize));
            int zSize = Mathf.CeilToInt((float)counter / (xSize * ySize));

            List<Matrix4x4> matrixList = new List<Matrix4x4>();
            int cnt = 0;
            for (int x = 0; x < xSize && cnt < counter; x++)
            {
                for (int y = 0; y < ySize && cnt < counter; y++)
                {
                    for (int z = 0; z < zSize && cnt < counter; z++)
                    {
                        // ����ÿ��ʵ����λ�ã�ʹ������У�����̧ 10 ����λ
                        Vector3 pos = new Vector3(
                            x * spacing - (xSize - 1) * spacing * 0.5f,
                            y * spacing - (ySize - 1) * spacing * 0.5f + 10,
                            z * spacing - (zSize - 1) * spacing * 0.5f
                        );
                        Matrix4x4 mat = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
                        matrixList.Add(mat);

                        // Ϊÿ��ʵ�����������ɫ
                        instanceColors.Add(new Vector4(Random.value, Random.value, Random.value, 1.0f));
                        cnt++;
                    }
                }
            }
            matrices = matrixList.ToArray();

            // ��ʼ�� NativeArray���������;����Ƶ� NativeArray���� Job ���и���ʹ��
            nativeMatrices = new NativeArray<Matrix4x4>(counter, Allocator.Persistent);
            for (int i = 0; i < counter; i++)
            {
                nativeMatrices[i] = matrices[i];
            }

            // �������λ��ƻ��������̶���СΪ1023��
            batchMatrixBuffer = new Matrix4x4[1023];

            // ���ʹ�ü�ӻ��ƣ����� argumentBuffer �� instanceTransformBuffer��������һ�Σ�
            if (enableIndricetDraw)
            {
                // ����������������5 �� uint����������ʵ��������ʼ�����������㡢��ʼʵ��λ��
                argumentBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
                uint[] args = new uint[5];
                args[0] = instanceMesh.GetIndexCount(0);
                args[1] = (uint)counter;
                args[2] = instanceMesh.GetIndexStart(0);
                args[3] = instanceMesh.GetBaseVertex(0);
                args[4] = 0;
                argumentBuffer.SetData(args);

                // ����ʵ���任���ݻ�����������ÿ�� Matrix4x4 ռ 16 floats����С�� 16 * sizeof(float)
                instanceTransformBuffer = new ComputeBuffer(counter, 16 * sizeof(float), ComputeBufferType.Structured);
                // ��ʼ���û���������Ϊ��ǰ nativeMatrices �е�����
                instanceTransformBuffer.SetData(matrices);
                // ���û������󶨵� indirectInstanceMaterial��Shader �������� StructuredBuffer<float4x4> unity_ObjectToWorldBuffer;
                indirectInstanceMaterial.SetBuffer("unity_ObjectToWorldBuffer", instanceTransformBuffer);
                indirectInstanceMaterial.SetBuffer("unity_InstanceColorBuffer", instanceTransformBuffer);
            }
        }

        // Burst ����� Job�����ڲ��и���ÿ��ʵ���ľ���
        [BurstCompile]
        struct UpdateMatricesJob : IJobParallelFor
        {
            public NativeArray<Matrix4x4> matrices;
            public float angle;   // ÿ֡��ת�Ƕȣ�������
            public int rotate;    // ��ת����

            public void Execute(int index)
            {
                // �� NativeArray �л�ȡ��ǰ����
                Matrix4x4 mat = matrices[index];
                // �����ƽ�Ʋ���
                Vector3 pos = mat.GetColumn(3);
                // ������ Y ����ת angle * rotate ��
                Quaternion rot = Quaternion.Euler(0, angle * rotate, 0);
                // ����ƽ��λ��
                pos = rot * pos;
                // ���¹�����󣬱������º��ƽ�ƣ���ת��������ΪĬ�ϣ������Ϊ TRS��ֻʹ��ƽ�ƣ�
                matrices[index] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
            }
        }

        void Update()
        {
            if (enableUpdate)
            {
                // ���㱾֡��ת�Ƕȣ���ת�ٶ� * Time.deltaTime��
                float angle = rotationSpeed * Time.deltaTime;
                // ���Ȳ��� Job ����ʵ������
                UpdateMatricesJob job = new UpdateMatricesJob
                {
                    matrices = nativeMatrices,
                    angle = angle,
                    rotate = rotate
                };
                JobHandle handle = job.Schedule(nativeMatrices.Length, 64);
                handle.Complete();

                // �� Job ���º�����ݸ��ƻع����;�������
                nativeMatrices.CopyTo(matrices);

                // ���ʹ�ü�ӻ��ƣ������ instanceTransformBuffer
                if (enableIndricetDraw && instanceTransformBuffer != null)
                {
                    // �����º�ľ��������ϴ��� GPU ������
                    instanceTransformBuffer.SetData(matrices);
                    // Ҳ����������� argumentBuffer �� instance ����������б仯
                    uint[] args = new uint[5];
                    args[0] = instanceMesh.GetIndexCount(0);
                    args[1] = (uint)counter;  // �˴�������ʵ��������
                    args[2] = instanceMesh.GetIndexStart(0);
                    args[3] = instanceMesh.GetBaseVertex(0);
                    args[4] = 0;
                    argumentBuffer.SetData(args);
                }
            }

            // ���� MaterialPropertyBlock �е�ʵ������ɫ��ÿ֡���ã�
            mpb.SetVectorArray("_BaseColorInstance", instanceColors);

            if (!enableIndricetDraw)
            {
                // ʹ�� DrawMeshInstanced �������ƣ�ÿ����� 1023 ��ʵ��
                int instanceCount = matrices.Length;
                int batchSize = 1023;
                for (int i = 0; i < instanceCount; i += batchSize)
                {
                    int count = Mathf.Min(batchSize, instanceCount - i);
                    System.Array.Copy(matrices, i, batchMatrixBuffer, 0, count);
                    Graphics.DrawMeshInstanced(instanceMesh, 0, instanceMaterial, batchMatrixBuffer, count, mpb);
                }
            }
            else
            {
                // ʹ�� DrawMeshInstancedIndirect ����ʵ��
                // ע�⣺Shader ��֧�ִӰ󶨵� StructuredBuffer����unity_ObjectToWorldBuffer������ȡʵ����������
                Bounds bounds = new Bounds(Vector3.zero, new Vector3(10000, 10000, 10000));
                Graphics.DrawMeshInstancedIndirect(instanceMesh, 0, indirectInstanceMaterial, bounds, argumentBuffer, 0, mpb);
            }
        }

        private void OnDisable()
        {
            if (nativeMatrices.IsCreated)
                nativeMatrices.Dispose();

            if (argumentBuffer != null)
            {
                argumentBuffer.Release();
                argumentBuffer = null;
            }

            if (instanceTransformBuffer != null)
            {
                instanceTransformBuffer.Release();
                instanceTransformBuffer = null;
            }
        }

        private void OnDestroy()
        {
            if (nativeMatrices.IsCreated)
                nativeMatrices.Dispose();

            if (argumentBuffer != null)
            {
                argumentBuffer.Release();
                argumentBuffer = null;
            }

            if (instanceTransformBuffer != null)
            {
                instanceTransformBuffer.Release();
                instanceTransformBuffer = null;
            }
        }
    }
}
