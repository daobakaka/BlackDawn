using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace BlackDawn.DOTS
{
    /// <summary>
    /// 行为系统组， 主要的伤害计算，以及行为变化
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class ActionSystemGroup : ComponentSystemGroup
    {

    }

    /// <summary>
    /// 渲染系统组
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class RenderSystemGroup : ComponentSystemGroup
    {

    }


    /// <summary>
    /// 主线程系统组，结构性变化，以及轻数据的更新
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ActionSystemGroup))]
    public partial class MainThreadSystemGroup : ComponentSystemGroup
    {

    }



}


