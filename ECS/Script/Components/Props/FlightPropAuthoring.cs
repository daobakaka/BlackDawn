using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BlackDawn.DOTS
{
    /// <summary>
    /// 飞行道具
    /// </summary>
    public class FlightPropAuthoring : MonoBehaviour
    {
        public class FlightPropBaker : Baker<FlightPropAuthoring>
        {
            public override void Bake(FlightPropAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                AddComponent(entity, new FlightPropCmptShooter());

               //报错测试
                AddComponent(entity, new FlightPropCmpt());
            }
        }
    }

    public struct FlightPropCmptShooter : IComponentData
    {
        /// <summary>
        /// 拥有者
        /// </summary>
        public Entity owner;
        /// <summary>
        /// 攻击力
        /// </summary>
        public float atk;

      


    }


}