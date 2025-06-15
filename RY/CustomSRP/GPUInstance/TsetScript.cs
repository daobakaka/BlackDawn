using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BlackDawn
{
    public class TestScript : MonoBehaviour
    {
        public GameObject gameObject0;
        public float spacing = 2.0f; // ����֮��ļ��
        public float rotationSpeed = 10.0f; // ��ת�ٶ�
        public int counter = 100; // ��������
        private List<GameObject> clones = new List<GameObject>();
        public int rotate = 1;

        void Start()
        {
            // ��̬���� xSize, ySize, zSize�������ӽ������岼��
            int xSize = Mathf.CeilToInt(Mathf.Pow(counter, 1f / 3f)); // ������������ȡ��
            int ySize = Mathf.CeilToInt(Mathf.Sqrt(counter / (float)xSize)); // ʣ�ಿ�ְ��������
            int zSize = Mathf.CeilToInt((float)counter / (xSize * ySize)); // ȷ������ >= counter

            int count = 0;

            for (int x = 0; x < xSize && count < counter; x++)
            {
                for (int y = 0; y < ySize && count < counter; y++)
                {
                    for (int z = 0; z < zSize && count < counter; z++)
                    {
                        Vector3 position = new Vector3(
                            x * spacing - (xSize - 1) * spacing * 0.5f,
                            y * spacing - (ySize - 1) * spacing * 0.5f + 10, // ������̧
                            z * spacing - (zSize - 1) * spacing * 0.5f
                        );

                        // ʵ��������
                        GameObject clone = Instantiate(gameObject0, position, Quaternion.identity);
                        clones.Add(clone);
                        count++;

                        // ����ʵ������ɫ���� _BaseColorInstance
                        // Renderer renderer = clone.GetComponent<Renderer>();
                        var renderer = clone.GetComponent<MeshRenderer>();
                        if (renderer != null)
                        {
                            // �����µ� MaterialPropertyBlock
                            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                            // ���������ɫ������������Զ���
                            Color instColor = new Color(Random.value, Random.value, Random.value, 1.0f);
                            // ����������������� Shader �ж����ʵ������������һ�£����� "_BaseColorInstance"
                            mpb.SetColor("_BaseColorInstance", instColor);
                            renderer.SetPropertyBlock(mpb);
                        }
                    }
                }
            }
        }

        void Update()
        {
            // Χ�� Y ����ת���п�¡����
            foreach (GameObject clone in clones)
            {
                clone.transform.RotateAround(Vector3.zero, Vector3.up, rotationSpeed * Time.deltaTime * rotate);
            }
        }
    }
}