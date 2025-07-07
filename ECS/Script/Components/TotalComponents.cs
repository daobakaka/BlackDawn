using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using System;
using Unity.Physics;

namespace BlackDawn.DOTS
{
    /// <summary>
    /// 形状
    /// </summary>
  
    public enum OverLapShape
    {
        Sphere,
        Box

    }

    /// 怪物状态
    /// </summary>
    public struct LiveMonster : IComponentData, IEnableableComponent { }

    /// <summary>
    /// 行为标签
    /// </summary>
    public enum EActionType
    {
        Idle,
        Run,
        Attack,
        Die

    }

    /// <summary>
    /// 用于存储触发器事件数据
    /// </summary>
    [Serializable]
    public struct TriggerPairData
    {
        public Entity EntityA;
        public Entity EntityB;

    }

    /// <summary>
    /// 主动范围查询结构/球形/盒形，默认持续性技能
    /// </summary>
    public struct OverlapOverTimeQueryCenter : IComponentData
    {
        public OverLapShape shape;
        public float3 box;
        public float3 center;
        public float radius;
        public float3 offset;
        public float4 rotaion;
        public CollisionFilter filter; // 每个Overlap可自定义过滤规则
    }

    /// <summary>
    /// 主动爆发性技能 查询
    /// </summary>
    public struct OverlapBurstQueryCenter : IComponentData
    {
        public OverLapShape shape;
        public float3 box;
        public float3 center;
        public float radius;
        public float3 offset;
        public float4 rotaion;
        public CollisionFilter filter; // 每个Overlap可自定义过滤规则
    }
    /// <summary>
    /// 主动追踪型技能 查询（连锁吞噬）
    /// </summary>
    public struct OverlapTrackingQueryCenter : IComponentData
    {
        public OverLapShape shape;
        public float3 box;
        public float3 center;
        public float radius;
        public float3 offset;
        public float4 rotaion;
        public CollisionFilter filter; // 每个Overlap可自定义过滤规则
    }

    /// <summary>
    /// 主动范围查询Buffer
    /// </summary>
    [InternalBufferCapacity(100)]
    public struct OverlapDetectionResult : IBufferElementData
    {
        public Entity target;
    }

    #region 怪物标签
    /// <summary>
    /// 标签用于特殊情况控制以及播放动画等
    /// 丧尸
    /// </summary>
    public struct MoZombieCmp : IComponentData, IEnableableComponent { }

    /// <summary>
    ///恶犬
    /// </summary>
    public struct MoAlbonoCmp : IComponentData, IEnableableComponent { }


    /// <summary>
    /// 恶犬原始特效尺度参数
    /// </summary>
    public struct MoAlbonoEffectsCmp : IComponentData
    {
        //火焰原始尺寸
       public float3 fireOringinalScale;
    
    
    }

    /// <summary>
    /// 按值分组组件在不同的chunk中， 保证遍历的时候，提高速度
    /// </summary>
    public struct TTTTTTTTSSSSSSSS : ISharedComponentData { public int value; }
    /// <summary>
    ///恶龙升空者
    /// </summary>
    public struct MoAlbonoUpperCmp : IComponentData, IEnableableComponent { }

    /// <summary>
    /// 恶龙升空这原始特效尺度参数
    /// </summary>
    public struct MoAlbononUpperEffectsCmp : IComponentData
    {
        //火焰原始尺寸
       public float3 fireOringinalScale;

    }


    #endregion


    /// <summary>
    /// 子检测器标签
    /// </summary>
    public struct DetectorTag : IComponentData { };

    /// <summary>
    /// 侦测系统
    /// </summary>
    public struct Detection_DefaultCmpt : IComponentData
    {
        public Entity bufferOwner; // 用于写入 NearbyHit 的实体

        public float originalRadius;//原始半径
    }

    public struct HeroAttackTarget : IComponentData
    {
        public Entity attackTarget;//攻击目标
    }

    /// <summary>
    /// 英雄主体标识 全局唯一
    /// </summary>
    public struct HeroEntityMasterTag :IComponentData { }

    /// <summary>
    /// 英雄分支标识 主要用于英雄分身
    /// </summary>
    public struct HeroEntityBrachTag : IComponentData { }
    /// <summary>
    /// 动画控制信息
    /// </summary>
    public struct AnimationControllerData : IComponentData
    {
        public bool isAttack;
        public bool isFire;    
    }
    #region 攻击类型标签
    /// <summary>
    /// 近战攻击
    /// </summary>
    public struct AtMelee : IComponentData { };

    /// <summary>
    /// 远程攻击
    /// </summary>
    public struct AtRanged : IComponentData { };
    /// <summary>
    /// 混合攻击
    /// </summary>
    public struct AtHybrid : IComponentData { };
    #endregion

    #region 英雄技能 预施加 在怪物身上的标签
    /// <summary>
    /// 雷霆之握标签,预加载失活状态
    /// </summary>
    public struct PreDefineHeroSkillThunderGripTag : IComponentData, IEnableableComponent
    {
          
    }






        
    #endregion






    #region 英雄增益BUFF 标签
    /// <summary>
    /// 先使用综合性的标签， 这样更符合SIMD 指令集的结构
    /// </summary>
    [Serializable]
    public struct HeroIntgratedNoImmunityState : IComponentData
    {
        //控制非免疫
        public float controlNoImmunity;
        //内联伤害非免疫（如 法阵技能）
        public float inlineDamageNoImmunity;
        //dot伤害非免疫
        public float dotNoImmunity;
        // 物理伤害非免疫
        public float physicalDamageNoImmunity;
        //元素伤害非免疫
        public float elementDamageNoImmunity;

        /// <summary>
        /// 无用， 不提供无参构造函数
    }
    /// <summary>
    /// link 效果或者其他效果开启标识
    /// </summary>
    public struct HeroEffectsLinked : IComponentData, IEnableableComponent { }
    
    
    
    /// <summary>
    /// 状态标识自由意志
    /// </summary>
    public struct HeroStateWillUnchained : IComponentData, IEnableableComponent { };


    /// <summary>
    /// 圣母降临， 免疫所有伤害
    /// </summary>
    public struct HeroStateDivineDescent : IComponentData, IEnableableComponent { };


    /// <summary>
    /// 静默领域 免疫所有DOT类伤害
    /// </summary>

    public struct HeroStateSilentDomain : IComponentData, IEnableableComponent { };

    /// <summary>
    /// 灵能风暴  技能释放不消耗灵力，持续性技能不持续消耗精力
    /// </summary>
    public struct HeroStatePsionicSurge : IComponentData, IEnableableComponent { };

    /// <summary>
    /// 钛铭壳体  免疫所有直接物理伤害
    /// </summary>

    public struct HeroStateTitaniumShell : IComponentData, IEnableableComponent { };
    /// <summary>
    /// 禁断共鸣  免疫所有直接法术伤害
    /// </summary>

    public struct HeroStateForbiddenResonance : IComponentData, IEnableableComponent { };
    #endregion
}
