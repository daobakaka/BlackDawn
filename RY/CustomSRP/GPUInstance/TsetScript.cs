using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BlackDawn
{
    public class TestScript : MonoBehaviour
    {
        public GameObject gameObject0;
        public float spacing = 2.0f; // 对象之间的间距
        public float rotationSpeed = 10.0f; // 旋转速度
        public int counter = 100; // 生成数量
        private List<GameObject> clones = new List<GameObject>();
        public int rotate = 1;

        void Start()
        {
            // 动态计算 xSize, ySize, zSize，尽量接近立方体布局
            int xSize = Mathf.CeilToInt(Mathf.Pow(counter, 1f / 3f)); // 立方根，向上取整
            int ySize = Mathf.CeilToInt(Mathf.Sqrt(counter / (float)xSize)); // 剩余部分按面积分配
            int zSize = Mathf.CeilToInt((float)counter / (xSize * ySize)); // 确保总数 >= counter

            int count = 0;

            for (int x = 0; x < xSize && count < counter; x++)
            {
                for (int y = 0; y < ySize && count < counter; y++)
                {
                    for (int z = 0; z < zSize && count < counter; z++)
                    {
                        Vector3 position = new Vector3(
                            x * spacing - (xSize - 1) * spacing * 0.5f,
                            y * spacing - (ySize - 1) * spacing * 0.5f + 10, // 整体上抬
                            z * spacing - (zSize - 1) * spacing * 0.5f
                        );

                        // 实例化对象
                        GameObject clone = Instantiate(gameObject0, position, Quaternion.identity);
                        clones.Add(clone);
                        count++;

                        // 设置实例化颜色属性 _BaseColorInstance
                        // Renderer renderer = clone.GetComponent<Renderer>();
                        var renderer = clone.GetComponent<MeshRenderer>();
                        if (renderer != null)
                        {
                            // 创建新的 MaterialPropertyBlock
                            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                            // 生成随机颜色，或根据需求自定义
                            Color instColor = new Color(Random.value, Random.value, Random.value, 1.0f);
                            // 这里的属性名必须与 Shader 中定义的实例化属性名称一致，例如 "_BaseColorInstance"
                            mpb.SetColor("_BaseColorInstance", instColor);
                            renderer.SetPropertyBlock(mpb);
                        }
                    }
                }
            }
        }

        void Update()
        {
            // 围绕 Y 轴旋转所有克隆对象
            foreach (GameObject clone in clones)
            {
                clone.transform.RotateAround(Vector3.zero, Vector3.up, rotationSpeed * Time.deltaTime * rotate);
            }
        }
    }
}