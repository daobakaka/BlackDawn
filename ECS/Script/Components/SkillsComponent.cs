using System;
using Unity.Entities;
using Unity.Mathematics;

namespace BlackDawn.DOTS
{
    /// <summary>
    /// 技能伤害参数封装，包含物理瞬时伤害、5 种元素瞬时伤害和 6 种持续性伤害
    /// </summary>
    [Serializable]
    public struct SkillsDamageCalPar : IComponentData
    {

        public Entity heroRef;
        /// <summary>物理瞬时带暴击伤害</summary>
        public float instantPhysicalDamage;

        // —— 元素瞬时伤害 ——
        /// <summary>冰霜瞬时伤害</summary>
        public float frostDamage;
        /// <summary>闪电瞬时伤害</summary>
        public float lightningDamage;
        /// <summary>毒素瞬时伤害</summary>
        public float poisonDamage;
        /// <summary>暗影瞬时伤害</summary>
        public float shadowDamage;
        /// <summary>火焰瞬时伤害</summary>
        public float fireDamage;

        // —— 持续性（DOT）伤害 ——
        /// <summary>冰霜持续性伤害</summary>
        public float frostDotDamage;
        /// <summary>闪电持续性伤害</summary>
        public float lightningDotDamage;
        /// <summary>毒素持续性伤害</summary>
        public float poisonDotDamage;
        /// <summary>暗影持续性伤害</summary>
        public float shadowDotDamage;
        /// <summary>火焰持续性伤害</summary>
        public float fireDotDamage;
        /// <summary>流血持续性伤害，由物理伤害触发</summary>
        public float bleedDotDamage;


        /// <summary>用于技能变化的整体伤害参数</summary>
        public float damageChangePar;

        /// <summary>
        ///判定几种暴击状态，用于生成不同颜色字体，和字体跳动动画
        /// </summary>
        public bool critTriggered;
        public bool vulTriggered;
        public bool supTriggered;
        public bool dotCritTriggered;
        public bool elemCritTriggered;

        //伤害类型枚举
        public DamageTriggerType damageTriggerType;
        //击中后存活时间，用于构建爆炸或者其他属性
        public float hitSurvivalTime;
        //原始存活时间
        public float originalSurvivalTime;
        //销毁标识
        public bool  destory;
 
        ///技能特殊的牵引和爆炸属性标签
        public bool enablePull;
        public bool enableExplosion;


    }
    /// <summary>
    /// 可关闭标签脉冲标签,初始化一个时间，用于存活判断,可以使用标签来定义 速度  二阶 存活时间
    /// </summary>
    public struct SkillPulseTag : IComponentData, IEnableableComponent
    { public float tagSurvivalTime;
      public float speed;
      //形变参数
      public float scaleChangePar;
      public bool enableSecond;
        //伤害变化参数,默认为0，外部赋值1+
     public float skillDamageChangeParTag;

    }

    /// <summary>
    /// 可关闭脉冲二阶段爆炸请求标签,不继承IComponentData不合法
    /// </summary>
    public struct SkillPulseSecondExplosionRequestTag : IComponentData, IEnableableComponent { }



    /// <summary>
    /// 暗能球
    /// </summary>
    public struct SkillDarkEnergyTag : IComponentData, IEnableableComponent
    {
        public float tagSurvivalTime;
        public float speed;
        //形变参数
        public float scaleChangePar;
        public bool enableSecond;
        //允许特殊执行逻辑
        public bool enableSpecialEffect;
        //伤害变化参数,默认为0，外部赋值1+
        public float skillDamageChangeParTag;

    }

    /// <summary>
    /// 冰火球 ，持续旋转 模板
    /// </summary>
    public struct SkillIceFireTag : IComponentData, IEnableableComponent
    {
        public float tagSurvivalTime;
        public float speed;
        public float radius;
        public float currentAngle;      // 当前角度，单位：弧度
        //形变参数，这一般第二阶段技能用于动态改变的参数
        public float originalScale;
        public float scaleChangePar;
        public bool enableSecond;
        //二阶状态持续时间， 这里一阶状态会继续持续
        public float secondSurvivalTime;
        //允许特殊执行逻辑
        public bool enableSpecialEffect;
        //伤害变化参数,默认为0，外部赋值1+
        public float skillDamageChangeParTag;

    }


    /// <summary>
    /// 可关闭冰火球二阶段爆炸请求标签
    /// </summary>
    public struct SkillIceFireSecondExplosionRequestTag : IComponentData, IEnableableComponent { }


    /// <summary>
    /// 落雷可理解为速度为0 的静态飞行道具，这里有一个持续释放效果，或应当改变逻辑，用携程处理？
    /// </summary>
    public struct SkillThunderStrikeTag : IComponentData, IEnableableComponent
    {
        public float tagSurvivalTime;
        //形变参数
        public float scaleChangePar;
        //伤害变化参数,默认为0，外部赋值1+
        public float skillDamageChangeParTag;

    }

    /// <summary>
    /// 法阵是持续性消耗类道具，同一时间只能存在一个？这里设计回调？clsss中单例获取 不加可关闭标签
    /// </summary>
    public struct SkillArcaneCircleTag : IComponentData
    {
        //这里要作为技能链接点
        public Entity heroRef;

        public float tagSurvivalTime;
        //形变参数
        public float scaleChangePar;
        public bool enableSecondA;
        public bool enableSecondB;

        //伤害变化参数,默认为0，外部赋值1+
        public float skillDamageChangeParTag;
        //这里添加等级标签，二阶技能扣除生命值跟等级有关
        public int level;

        //手动关闭
        public bool closed;

    }
    /// <summary>
    /// 二阶灵能法阵标签，这里应该是一个buffer，这个buffer可以添加不同的link，用于把相关的参数链接
    /// 保留目标和时间 按照dot分配？6秒
    /// </summary>
    [InternalBufferCapacity(1000)]
    public struct SkillArcaneCircleSecondBufferTag : IBufferElementData 
    
    {
        public Entity target;
        public float tagSurvivalTime;
        
    }
    /// <summary>
    /// 仅仅用于收集碰撞对
    /// </summary>
    public struct SkillArcaneCircleSecondTag : IComponentData { }
    /// <summary>
    /// 三阶法阵标签
    /// </summary>
    public struct SkillArcaneCircleThirdTag : IComponentData, IEnableableComponent { }
}

