using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BlackDawn
{
    /// <summary>
    /// �õ�ǰ�����ڸ������ (��λ�� + ����ƫ��) ��ΪԲ�ģ���ˮƽ������Բ���˶���
    /// ����ʼ������Զ��Բ�ĵķ����Ҳ��ܸ�����������תӰ�졣
    /// </summary>
    public class WeaponState : MonoBehaviour
    {
        [Header("Բ���˶�����")]
        [Tooltip("Բ���˶��뾶")]
        public float radius = 2f;

        [Tooltip("�˶��ٶȣ���/�룩")]
        public float orbitSpeed = 90f;

        [Tooltip("��ת�ᣨ��������ϵ��")]
        public Vector3 rotationAxis = Vector3.up;

        [Tooltip("����ڸ�����λ�õĹ̶���������ƫ��������ΪԲ�ĵĶ���ƫ��")]
        public Vector3 offsetWorld = Vector3.zero;

        [Tooltip("��ʼ�Ƕȣ��ȣ�")]
        public float startAngle = 0f;

        // ��ǰ�Ƕȣ����ȣ�
        private float _angle;

        void Start()
        {
            // ����ʼ�Ƕ�ת��Ϊ����
            _angle = startAngle * Mathf.Deg2Rad;

            // ��ʼ��λ�úͳ���
            UpdatePosition();
        }

        void Update()
        {
            // �����ٶ��������½Ƕȣ���ת���ȣ�
            _angle += orbitSpeed * Mathf.Deg2Rad * Time.deltaTime;

            // ���ֽǶ��� 0 �� 2�� ��Χ��
            if (_angle > Mathf.PI * 2f)
                _angle -= Mathf.PI * 2f;

            // ����λ�úͳ���
            UpdatePosition();
        }

        /// <summary>
        /// ���㲢������������ϵ�е�λ�úͳ���
        /// </summary>
        private void UpdatePosition()
        {
            Transform parent = transform.parent;
            if (parent == null) return;

            // Բ�� = ������λ�� + �̶�ƫ��
            Vector3 center = parent.position + offsetWorld;

            // ����Բ��ƫ�ƣ���������ϵ������ Y ���� XZ ƽ��
            float x = Mathf.Cos(_angle) * radius;
            float z = Mathf.Sin(_angle) * radius;
            Vector3 circleOffset = new Vector3(x, 0f, z);

            // ����λ�� = Բ�� + Բ��ƫ��
            transform.position = center + circleOffset;

            // ʼ������Զ��Բ�ĵķ���
            Vector3 forward = (transform.position - center).normalized;
            transform.rotation = Quaternion.LookRotation(forward, rotationAxis);
        }
    }
}