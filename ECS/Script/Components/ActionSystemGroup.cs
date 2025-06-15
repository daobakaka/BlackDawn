using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace BlackDawn.DOTS
{
    /// <summary>
    /// 行为系统组， 主要归并自定义脚本在一个组里面运行，方便进行管理
    /// </summary>

    //[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    //[UpdateAfter(typeof(PhysicsSystemGroup))]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class ActionSystemGroup : ComponentSystemGroup
    {

    }
}
