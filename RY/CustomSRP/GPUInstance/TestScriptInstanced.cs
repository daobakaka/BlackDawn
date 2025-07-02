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
        // 预制体，必须包含 MeshFilter 和 MeshRenderer 组件
        public GameObject prefab;
        // 用于间接绘制时使用的材质（Shader 需支持从 StructuredBuffer 读取实例数据）
        public GameObject indirectPrefab;
        public float spacing = 2.0f;           // 对象之间的间距
        public float rotationSpeed = 10.0f;      // 旋转速度（度/秒）
        public int counter = 100;              // 生成实例数量
        public int rotate = 1;                 // 旋转方向（1 或 -1）
        public bool enableUpdate = true;
        // 是否使用间接绘制
        public bool enableIndricetDraw = false;

        // 用于 DrawMeshInstanced 调用的管理型矩阵数组
        private Matrix4x4[] matrices;
        // 存储每个实例的颜色（用于 GPU Instancing）
        private List<Vector4> instanceColors = new List<Vector4>();

        // 缓存 Mesh 与 Material
        private Mesh instanceMesh;
        private Material instanceMaterial;
        // 用于间接绘制时使用的材质（其 Shader 需要支持 StructuredBuffer 读取实例变换数据）
        private Material indirectInstanceMaterial;

        // 用于 Job 更新添加的 NativeArray，存储所有实例的矩阵（使用 UnityEngine.Matrix4x4）
        private NativeArray<Matrix4x4> nativeMatrices;

        // 持久 MaterialPropertyBlock，避免每帧创建
        private MaterialPropertyBlock mpb;

        // 用于 DrawMeshInstancedIndirect 的参数缓冲区
        private ComputeBuffer argumentBuffer;
        // 用于间接绘制时存储实例变换数据的 ComputeBuffer
        private ComputeBuffer instanceTransformBuffer;

        // 固定批次绘制缓冲区（避免每帧 new 数组），DrawMeshInstanced 每批最多 1023 个实例
        private Matrix4x4[] batchMatrixBuffer;

        void Start()
        {
            // 获取预制体的 Mesh 和 Material
            MeshFilter mf = prefab.GetComponent<MeshFilter>();
            MeshRenderer mr = prefab.GetComponent<MeshRenderer>();
            if (mf == null || mr == null)
            {
                Debug.LogError("Prefab must have MeshFilter and MeshRenderer components.");
                return;
            }
            instanceMesh = mf.sharedMesh;
            instanceMaterial = mr.sharedMaterial;

            // 对于间接绘制，我们假设 indirectPrefab 的 MeshRenderer 使用的材质支持 StructuredBuffer 读取实例数据
            MeshRenderer mri = indirectPrefab.GetComponent<MeshRenderer>();
            if (mri != null)
            {
                indirectInstanceMaterial = mri.sharedMaterial;
            }
            else
            {
                indirectInstanceMaterial = instanceMaterial;
            }

            // 初始化 MPB，避免每帧新建
            mpb = new MaterialPropertyBlock();

            // 动态计算 xSize, ySize, zSize 以尽量形成立方体布局
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
                        // 计算每个实例的位置，使整体居中，且上抬 10 个单位
                        Vector3 pos = new Vector3(
                            x * spacing - (xSize - 1) * spacing * 0.5f,
                            y * spacing - (ySize - 1) * spacing * 0.5f + 10,
                            z * spacing - (zSize - 1) * spacing * 0.5f
                        );
                        Matrix4x4 mat = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
                        matrixList.Add(mat);

                        // 为每个实例生成随机颜色
                        instanceColors.Add(new Vector4(Random.value, Random.value, Random.value, 1.0f));
                        cnt++;
                    }
                }
            }
            matrices = matrixList.ToArray();

            // 初始化 NativeArray，将管理型矩阵复制到 NativeArray，供 Job 并行更新使用
            nativeMatrices = new NativeArray<Matrix4x4>(counter, Allocator.Persistent);
            for (int i = 0; i < counter; i++)
            {
                nativeMatrices[i] = matrices[i];
            }

            // 分配批次绘制缓冲区（固定大小为1023）
            batchMatrixBuffer = new Matrix4x4[1023];

            // 如果使用间接绘制，创建 argumentBuffer 和 instanceTransformBuffer（仅创建一次）
            if (enableIndricetDraw)
            {
                // 创建参数缓冲区：5 个 uint：索引数、实例数、起始索引、基顶点、起始实例位置
                argumentBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
                uint[] args = new uint[5];
                args[0] = instanceMesh.GetIndexCount(0);
                args[1] = (uint)counter;
                args[2] = instanceMesh.GetIndexStart(0);
                args[3] = instanceMesh.GetBaseVertex(0);
                args[4] = 0;
                argumentBuffer.SetData(args);

                // 创建实例变换数据缓冲区，假设每个 Matrix4x4 占 16 floats，大小即 16 * sizeof(float)
                instanceTransformBuffer = new ComputeBuffer(counter, 16 * sizeof(float), ComputeBufferType.Structured);
                // 初始化该缓冲区数据为当前 nativeMatrices 中的数据
                instanceTransformBuffer.SetData(matrices);
                // 将该缓冲区绑定到 indirectInstanceMaterial，Shader 侧需声明 StructuredBuffer<float4x4> unity_ObjectToWorldBuffer;
                indirectInstanceMaterial.SetBuffer("unity_ObjectToWorldBuffer", instanceTransformBuffer);
                indirectInstanceMaterial.SetBuffer("unity_InstanceColorBuffer", instanceTransformBuffer);
            }
        }

        // Burst 编译的 Job，用于并行更新每个实例的矩阵
        [BurstCompile]
        struct UpdateMatricesJob : IJobParallelFor
        {
            public NativeArray<Matrix4x4> matrices;
            public float angle;   // 每帧旋转角度（度数）
            public int rotate;    // 旋转方向

            public void Execute(int index)
            {
                // 从 NativeArray 中获取当前矩阵
                Matrix4x4 mat = matrices[index];
                // 分离出平移部分
                Vector3 pos = mat.GetColumn(3);
                // 计算绕 Y 轴旋转 angle * rotate 度
                Quaternion rot = Quaternion.Euler(0, angle * rotate, 0);
                // 更新平移位置
                pos = rot * pos;
                // 重新构造矩阵，保留更新后的平移；旋转与缩放设为默认（这里简化为 TRS，只使用平移）
                matrices[index] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
            }
        }

        void Update()
        {
            if (enableUpdate)
            {
                // 计算本帧旋转角度（旋转速度 * Time.deltaTime）
                float angle = rotationSpeed * Time.deltaTime;
                // 调度并行 Job 更新实例矩阵
                UpdateMatricesJob job = new UpdateMatricesJob
                {
                    matrices = nativeMatrices,
                    angle = angle,
                    rotate = rotate
                };
                JobHandle handle = job.Schedule(nativeMatrices.Length, 64);
                handle.Complete();

                // 将 Job 更新后的数据复制回管理型矩阵数组
                nativeMatrices.CopyTo(matrices);

                // 如果使用间接绘制，则更新 instanceTransformBuffer
                if (enableIndricetDraw && instanceTransformBuffer != null)
                {
                    // 将更新后的矩阵数据上传到 GPU 缓冲区
                    instanceTransformBuffer.SetData(matrices);
                    // 也可在这里更新 argumentBuffer 的 instance 数量，如果有变化
                    uint[] args = new uint[5];
                    args[0] = instanceMesh.GetIndexCount(0);
                    args[1] = (uint)counter;  // 此处假设总实例数不变
                    args[2] = instanceMesh.GetIndexStart(0);
                    args[3] = instanceMesh.GetBaseVertex(0);
                    args[4] = 0;
                    argumentBuffer.SetData(args);
                }
            }

            // 更新 MaterialPropertyBlock 中的实例化颜色（每帧设置）
            mpb.SetVectorArray("_BaseColorInstance", instanceColors);

            if (!enableIndricetDraw)
            {
                // 使用 DrawMeshInstanced 分批绘制，每批最多 1023 个实例
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
                // 使用 DrawMeshInstancedIndirect 绘制实例
                // 注意：Shader 需支持从绑定的 StructuredBuffer（“unity_ObjectToWorldBuffer”）读取实例矩阵数据
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
