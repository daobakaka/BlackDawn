using BlackDawn.DOTS;
using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
namespace BlackDawn
{
    public class VFXBaseParameters
    {
    }

    /// <summary>
    /// 通用链接特效 虹吸
    /// </summary>
    [Serializable]
    public struct LinkTargetData
    {
        public float3 target;
    }
}