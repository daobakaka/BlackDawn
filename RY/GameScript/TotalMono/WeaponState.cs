using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BlackDawn
{
    /// <summary>
    /// 让当前物体在父物体的 (父位置 + 世界偏移) 作为圆心，在水平面上做圆周运动，
    /// 朝向始终面向远离圆心的方向，且不受父物体自身旋转影响。
    /// </summary>
    public class WeaponState : MonoBehaviour
    {
        [Header("圆周运动设置")]
        [Tooltip("圆周运动半径")]
        public float radius = 2f;

        [Tooltip("运动速度（度/秒）")]
        public float orbitSpeed = 90f;

        [Tooltip("旋转轴（世界坐标系）")]
        public Vector3 rotationAxis = Vector3.up;

        [Tooltip("相对于父物体位置的固定世界坐标偏移量，作为圆心的额外偏移")]
        public Vector3 offsetWorld = Vector3.zero;

        [Tooltip("起始角度（度）")]
        public float startAngle = 0f;

        // 当前角度（弧度）
        private float _angle;

        void Start()
        {
            // 将起始角度转换为弧度
            _angle = startAngle * Mathf.Deg2Rad;

            // 初始化位置和朝向
            UpdatePosition();
        }

        void Update()
        {
            // 根据速度增量更新角度（度转弧度）
            _angle += orbitSpeed * Mathf.Deg2Rad * Time.deltaTime;

            // 保持角度在 0 到 2π 范围内
            if (_angle > Mathf.PI * 2f)
                _angle -= Mathf.PI * 2f;

            // 更新位置和朝向
            UpdatePosition();
        }

        /// <summary>
        /// 计算并设置世界坐标系中的位置和朝向
        /// </summary>
        private void UpdatePosition()
        {
            Transform parent = transform.parent;
            if (parent == null) return;

            // 圆心 = 父物体位置 + 固定偏移
            Vector3 center = parent.position + offsetWorld;

            // 计算圆周偏移（世界坐标系），绕 Y 轴在 XZ 平面
            float x = Mathf.Cos(_angle) * radius;
            float z = Mathf.Sin(_angle) * radius;
            Vector3 circleOffset = new Vector3(x, 0f, z);

            // 最终位置 = 圆心 + 圆周偏移
            transform.position = center + circleOffset;

            // 始终面向远离圆心的方向
            Vector3 forward = (transform.position - center).normalized;
            transform.rotation = Quaternion.LookRotation(forward, rotationAxis);
        }
    }
}