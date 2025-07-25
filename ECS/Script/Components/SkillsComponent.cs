using System;
using NUnit.Framework.Interfaces;
using Unity.Entities;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;

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

        /// <summary>并行处理数量,默认100，根据技能调整</summary>
        public int ParallelCount;
        // —— 临时性（控制属性） ——
        /// <summary>临时性控制属性</summary>
        public float tempFreeze;
        public float tempStun;
        public float tempFear;
        public float tempRoot;
        public float tempSlow;

        public float tempknockback;

        /// <summary>用于技能变化的整体伤害参，这里看到是在池化中进行计算的，也应该在技能表现中增加，默认值为1</summary>
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
        //击中后存活时间，用于构建爆炸或者其他属性(如分裂)
        public float hitSurvivalTime;
        public bool hit;

        public float timer;//通用记时器,1. 烈焰灵刃
        //原始存活时间
        public float originalSurvivalTime;
        //销毁标识
        public bool destory;

        ///技能特殊的牵引和爆炸属性标签
        public bool enablePull;
        public bool enableExplosion;

        //临时吸引值
        public float tempPull;
        //临时爆炸值
        public float tempExplosion;

    }



    /// <summary>
    /// 技能伤害持续性伤害参数封装，包含物理瞬时伤害、5 种元素瞬时伤害和 6 种持续性伤害
    /// </summary>
    [Serializable]
    public struct SkillsOverTimeDamageCalPar : IComponentData
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

        /// <summary>并行处理数量,默认100，根据技能调整</summary>
        public int ParallelCount;
        // —— 临时性（控制属性） ——
        /// <summary>临时性控制属性</summary>
        public float tempFreeze;
        public float tempStun;
        public float tempFear;
        public float tempRoot;
        public float tempSlow;

        /// <summary>用于技能变化的整体伤害参，这里看到是在池化中进行计算的，也应该在技能表现中增加，默认值为1</summary>
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
        //击中后存活时间，用于构建爆炸或者其他属性(如分裂)
        public float hitSurvivalTime;
        public bool hit;
        //原始存活时间
        public float originalSurvivalTime;
        //销毁标识
        public bool destory;

        ///技能特殊的牵引和爆炸属性标签
        public bool enablePull;
        public bool enableExplosion;
        //临时吸引值
        public float tempPull;
        //临时爆炸值
        public float tempExplosion;

    }






    /// <summary>
    /// 爆发性伤害技能标签，只能继承于原技能，并与其联动，靠技能标签进行单次计算
    /// </summary>
    [Serializable]
    public struct SkillsBurstDamageCalPar : IComponentData
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

        /// <summary>并行处理数量,默认100，根据技能调整</summary>
        public int ParallelCount;
        // —— 临时性（控制属性） ——
        /// <summary>临时性控制属性</summary>
        public float tempFreeze;
        public float tempStun;
        public float tempFear;
        public float tempRoot;
        public float tempSlow;
        public float tempknockback;

        public bool critTriggered;
        public bool vulTriggered;
        public bool supTriggered;
        public bool dotCritTriggered;
        public bool elemCritTriggered;

        /// <summary>用于技能变化的整体伤害参，这里看到是在池化中进行计算的，也应该在技能表现中增加，默认值为1</summary>
        public float damageChangePar;

        //伤害类型枚举
        public DamageTriggerType damageTriggerType;
        //击中后存活时间，用于构建爆炸或者其他属性(如分裂)
        public float hitSurvivalTime;
        public bool hit;
        //原始存活时间
        public float originalSurvivalTime;
        //销毁标识
        public bool destory;

        ///技能特殊的牵引和爆炸属性标签
        public bool enablePull;
        public bool enableExplosion;

        //临时引力值
        public float tempPull;
        //临时爆炸值
        public float tempExplosion;

        ///！！爆发时间
        public float burstTime;

    }

    /// <summary>
    /// 寻踪类技能标签
    /// </summary>
    public struct SkillsTrackingCalPar : IComponentData
    {
        //基于 连锁吞噬的通用处理    
        public Entity targetRef;
        public float3 currentDir; // 当前方向
        public bool enbaleChangeTarget;//这里用于碰撞之后的回调，只有在回调允许的时候，才能添加buffer和进行相关计算
        //寻迹象次数
        public int runCount;
        //原始储备次数
        public int originalCount;
        /// <summary>
        /// 事件记录器， 目标切换频率
        /// </summary>
        public float timer;

        /// <summary>
        /// 寻址类技能销毁标识
        /// </summary>
        public bool destory;

        //--基于 闪电链的特殊处理,处理三个折射点的数值
        public Entity pos2Ref;

        public Entity pos3Ref;

        public Entity pos4Ref;
        public Entity pos5Ref;

        public Entity pos6Ref;

        public Entity pos7Ref;
        public Entity pos8Ref;

    }



    /// <summary>
    /// 可关闭标签脉冲标签,初始化一个时间，用于存活判断,可以使用标签来定义 速度  二阶 存活时间
    /// </summary>
    public struct SkillPulseTag : IComponentData, IEnableableComponent
    {
        public float tagSurvivalTime;
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
    /// 暗能球附魔类
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
    /// 寒冰 附魔类
    /// </summary>
    public struct SkillFrostTag : IComponentData, IEnableableComponent
    {
        public float3 originalPosition;
        public float tagSurvivalTime;
        public float speed;
        //形变参数
        public float scaleChangePar;
        //允许第二阶段
        public bool enableSecond;
        //允许特殊执行逻辑
        public bool enableSpecialEffect;
        //伤害变化参数,默认为0，外部赋值1+
        public float skillDamageChangeParTag;
        //击中间隔时间
        public float hitIntervalTime;
        //击中次数
        public int hitCount;
        //碎片数量
        public int shrapnelCount;


    }
    /// <summary>
    /// 寒冰碎片标签
    /// </summary>
    public struct SkillFrostShrapneTag : IComponentData
    {
        public float tagSurvivalTime;
        public float speed;
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
    /// 黑炎 
    /// </summary>
    public struct SkillBlackFrameTag : IComponentData
    {
        public float tagSurvivalTime;
        public float scaleChangePar;
        //伤害变化参数,默认为0，外部赋值1+
        public float skillDamageChangeParTag;

        public bool enableSecondA;
        public bool enableSecondB;
        public bool enableSecondC;
        public int level;
    }
    //预定义黑炎A阶段标签， 加载在怪物身上，避免在渲染系统中进行计算，解耦
    public struct PreDefineHeroSkillBlackFrameATag : IComponentData, IEnableableComponent { }
    //预定义黑炎B阶段标签， 加载在怪物身上
    public struct PreDefineHeroSkillBlackFrameBTag : IComponentData, IEnableableComponent { }

    /// <summary>
    /// 横扫 
    /// </summary>
    public struct SkillSweepTag : IComponentData
    {
        public float tagSurvivalTime;
        //生成时间
        public float spawnTimer;

        public float interval;
        public float rotationTotalTime;
        public float scaleChangePar;
        //伤害变化参数,默认为0，外部赋值1+
        public float skillDamageChangeParTag;
        public float speed;
        public bool enableSecondA;
        public bool enableSecondB;

        public int level;

    }
    /// <summary>
    /// 横扫 渲染TAG
    /// </summary>
    public struct SkillSweepRenderTag : IComponentData
    {
        public float tagSurvivalTime;
        public bool destory;
    }
    /// <summary>
    /// 横扫B阶段 余震
    /// </summary>
    public struct SkillSweepBTag : IComponentData
    {
        public float tagSurvivalTime;
        public float scaleChangePar;
        //伤害变化参数,默认为0，外部赋值1+
        public float skillDamageChangeParTag;
        public float speed;
        public int level;

    }

    /// <summary>
    /// 技能 毒池 瞬时类技能
    /// </summary>
    public struct SkillPoisonPoolTag : IComponentData
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
    /// 保留目标和时间 按照dot分配？6秒,不能太长会超出16KB限定范围
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
    /// 法阵二阶技能渲染标签
    /// </summary>
    public struct SkillArcaneCircleSecondRenderTag : IComponentData, IEnableableComponent { }
    /// <summary>
    /// 三阶法阵标签
    /// </summary>
    public struct SkillArcaneCircleThirdTag : IComponentData, IEnableableComponent { }


    /// <summary>
    /// 元素共鸣
    /// </summary>
    public struct SkillElementResonanceTag : IComponentData
    {
        public float tagSurvivalTime;

        public bool enableSecondA;
        //二重变化因子，技能初始化时传入
        public float secondDamagePar;

        public bool enableSecondB;
        //三重变化因子，技能初始化时传入
        public float thridDamagePar;

    }
    /// <summary>
    /// 静电牢笼
    /// </summary>
    public struct SkillElectroCageTag : IComponentData
    {
        public float tagSurvivalTime;
        //形变参数
        public float scaleChangePar;
        public bool enableSecondA;
        public bool enableSecondB;
        //间隔时间
        public float intervalTimer;
        //内部标记时间
        public float timerA;
        public float timerB;
        //伤害变化参数,默认为0，外部赋值1+
        public float skillDamageChangeParTag;
        //这里添加等级标签，二阶技能扣除生命值跟等级有关
        public int level;
        //传导次数
        public int StackCount;

    }
    /// <summary>
    /// 静电牢笼2阶雷暴，1秒
    /// </summary>
    public struct SkillElectroCageScoendTag : IComponentData
    {

        public float tagSurvivalTime;

    }
    /// <summary>
    /// 毒爆地雷,可以关闭，就可以取消碰撞对收集，并打开二阶段的爆炸效果，重新赋值伤害
    /// </summary>
    public struct SkillMineBlastTag : IComponentData, IEnableableComponent
    {


        public float tagSurvivalTime;
        //形变参数
        public float scaleChangePar;

        //伤害变化参数,默认为0，外部赋值1+
        public float skillDamageChangeParTag;
        //这里添加等级标签，二阶技能扣除生命值跟等级有关
        public int level;

    }

    /// <summary>
    /// 毒爆，爆炸后的标签， 用于移除 原始标签的单独碰撞检测效果
    /// </summary>
    public struct SkillMineBlastExplosionTag : IComponentData, IEnableableComponent
    {

        public float tagSurvivalTime;
        //爆炸遗留时间
        public float tagSurvivalTimeSecond;
        //形变参数
        public float scaleChangePar;
        public bool enableSecondA;
        public bool enableSecondB;
        public bool enableSecondC;
        //伤害变化参数,默认为0，外部赋值1+
        public float skillDamageChangeParTag;
        //开始第二阶段
        public bool startSecondA;
        //等级
        public int level;
    }

    /// <summary>
    /// 毒雨
    /// </summary>
    public struct SkillPoisonRainTag : IComponentData
    {

        public float tagSurvivalTime;
        //形变参数
        public float scaleChangePar;
        public bool enableSecondA;
        public bool enableSecondB;
        public bool enableSecondC;
        //伤害变化参数,默认为0，外部赋值1+
        public float skillDamageChangeParTag;
        //开始第二阶段
        public bool startSecondA;
        //等级
        public int level;
    }
    /// <summary>
    /// 毒雨A 阶段TAG
    /// </summary>
    public struct SkillPoisonRainATag : IComponentData
    {
        public int level;
    }

    /// <summary>
    /// 暗影洪流
    /// </summary>
    public struct SkillShadowTideTag : IComponentData
    {

        public float tagSurvivalTime;
        //形变参数
        public float scaleChangePar;
        public bool enableSecondA;
        public bool enableSecondB;
        public bool enableSecondC;
        //伤害变化参数,默认为0，外部赋值1+
        public float skillDamageChangeParTag;
        //开始第二阶段
        public bool startSecondA;
        //等级
        public int level;
        //手动关闭
        public bool closed;
        //特效消亡时间
        public float effectDissolveTime;
        //B 阶段的时间计数器
        public float secondBTimer;
    }
    /// <summary>
    ///暗影洪流形成的烈焰喷射技能，仅储存一个时间标签即可，仅继承火焰伤害
    /// </summary>
    public struct SkillShadowTideBTag : IComponentData
    {

        public float tagSurvivalTime;

    }

    /// <summary>
    /// 冰霜新星
    /// </summary>
    public struct SkillFrostNovaTag : IComponentData
    {

        public float tagSurvivalTime;
        //形变参数
        public float scaleChangePar;
        public bool enableSecondA;
        public bool enableSecondB;
        public bool enableSecondC;
        //伤害变化参数,默认为0，外部赋值1+
        public float skillDamageChangeParTag;
        //开始第二阶段
        public bool startSecondA;
        //等级
        public int level;
    }

    /// <summary>
    /// 寒霜新星第二阶段技能标签，包含形变参数和二阶技能参数
    /// </summary>
    public struct SkillFrostNovaBTag : IComponentData
    {
        public float tagSurvivalTime;
        public int level;
    }
    /// <summary>
    /// 暗影之拥技能标签
    /// </summary>
    public struct SkillShadowEmbraceTag : IComponentData
    {


        public float tagSurvivalTime;
        //形变参数
        public float scaleChangePar;
        public bool enableSecondA;
        public bool enableSecondB;
        //伤害变化参数,默认为0，外部赋值1+
        public float skillDamageChangeParTag;
        //等级
        public int level;



    }
    /// <summary>
    /// 暗影辉耀造成的持续性伤害
    /// </summary>
    public struct SkillShadowEmbraceAOverTimeTag : IComponentData
    {

        public float tagSurvivalTime;
        //形变参数
        public int level;



    }


    /// <summary>
    /// 雷霆之握 技能标签
    /// </summary>
    public struct SkillThunderGripTag : IComponentData
    {
        public float tagSurvivalTime;
        //形变参数
        public float scaleChangePar;
        public bool enableSecondA;
        public bool enableSecondB;
        public bool enableSecondC;
        //伤害变化参数,默认为0，外部赋值1+
        public float skillDamageChangeParTag;
        //开始第二阶段
        public bool startSecondA;
        //等级
        public int level;

    }

    /// <summary>
    /// 雷霆之握标签,预加载失活状态
    /// </summary>
    public struct PreDefineHeroSkillThunderGripTag : IComponentData, IEnableableComponent
    {
    }

    /// <summary>
    /// 技能连锁吞噬
    /// </summary>
    public struct SkillChainDevourTag : IComponentData
    {
        public float tagSurvivalTime;
        public float speed;
        public bool enableSecondA;
        public bool enableSecondB;
        public float skillDamageChangeParTag;
        //等级
        public int level;

    }

    /// <summary>
    /// 闪电链
    /// </summary>
    public struct SkillLightningChainTag : IComponentData
    {
        public float tagSurvivalTime;

        public float laterTagSurvivalTime;
        public float speed;
        public bool enableSecondA;
        public bool enableSecondB;
        public float skillDamageChangeParTag;
        //等级
        public int level;
        //初始化的
        public bool initialized;
        //多线程中是否检查buffer 的
        public bool bufferChecked;
        //目标位置，闪电链侦擦器位置
        public float3 targetPostion;

        //闪电链 引用
        public LightningChainColliderRef colliderRef;

    }
    /// <summary>
    /// 用于渲染闪电链的标签
    /// </summary>
    public struct SkillLightningChainRenderTag : IComponentData
    {

        public float tagSurvivalTime;
        public bool initialized;
        public bool enableSecondA;
        public bool enableSecondB;
        //渲染链接引用，动态改变球体位置
        public LightningChainColliderRef colliderRef;

    }
    /// <summary>
    /// 用于渲染闪电链的二阶电弧
    /// </summary>
    public struct SkillLightningChainRenderBTag : IComponentData
    {

        public float tagSurvivalTime;
        public bool initialized;
        public bool enableSecondA;
        public bool enableSecondB;
        //渲染链接引用，动态改变球体位置
        public LightningChainColliderRef colliderRef;

    }
    /// <summary>
    /// 闪电链 电球碰撞器 碰撞体标签初始失活
    /// </summary>
    public struct skillLightningChianColliderTag : IComponentData, IEnableableComponent
    {

        public float tagSurvivalTime;

    }

    [Serializable]
    public struct LightningChainColliderRef
    {
        public Entity collider1;
        public Entity collider2;
        public Entity collider3;
        public Entity collider4;
        public Entity collider5;
        public Entity collider6;
        public Entity collider7;
        public Entity collider8;


    }

    /// <summary>
    /// 元素爆发标签
    /// </summary>
    public struct SkillElementBurstTag : IComponentData
    {
        public float tagSurvivalTime;

        public float startBurstTime;
        public float speed;
        public bool enableSecondA;
        public bool enableSecondB;
        public float skillDamageChangeParTag;
        //等级
        public int level;

    }

    /// <summary>
    /// 技能元素护盾
    /// 开始添加到英雄身上,动态更新自身的减伤值和增伤值 --作为英雄内部结构体添加？
    /// 分散添加， 独立于英雄自身结构体之外
    /// </summary>
    public struct SkillElementShieldTag_Hero : IComponentData, IEnableableComponent
    {

        public bool enableSecondA;
        public bool enableSecondB;

        public bool active;

        //伤害减免
        public float damageReduction;
        //伤害加深
        public float damageAmplification;

        public int level;

    }


    /// <summary>
    /// 技能冰霜护盾 
    /// 开始添加到英雄身上,动态更新自身的减伤值和增伤值 --作为英雄内部结构体添加？
    /// 分散添加， 独立于英雄自身结构体之外
    /// </summary>
    public struct SkillFrostShieldTag_Hero : IComponentData, IEnableableComponent
    {
        //持续60秒
        public float tagSurvivalTime;
        public bool enableSecondA;
        public bool enableSecondB;

        public bool active;

        public bool relaseSkill;

        public float iceConeDamage;

        //伤害减免
        public float damageReduction;
        //伤害加深
        public float damageAmplification;

        public int level;

    }
    /// <summary>
    /// 冰霜护盾A变体生成冰刺技能
    /// </summary>
    public struct SkillFrostShieldTagA : IComponentData
    {
        //默认存活时间为特效时间
        public float tagSurvivalTime;


    }
    /// <summary>
    /// 进击 保护类技能
    /// </summary>
    public struct SkillAdvanceTag_Hero : IComponentData, IEnableableComponent
    {

        public float tagSurvivalTime;
        public bool active;
        public bool enableSecondA;
        public int level;
    }

    /// <summary>
    /// 暗影之拥抱技能标签
    /// </summary>
    public struct SkillShadowEmbrace_Hero : IComponentData, IEnableableComponent
    {
        public float tagSurvivalTime;
        public bool active;
        public float shadowTime;
        public bool enableSecondA;
        public bool enableSecondB;
        public bool initialized;
        public int level;
    }
    /// <summary>
    /// 瘟疫蔓延， 运行逻辑需由技能开启，涉及到自身特效， 写在回调系统中
    /// </summary>
    public struct SkillPlagueSpread_Hero : IComponentData, IEnableableComponent
    {
        public float tagSurvivalTime;
        public bool active;
        public bool enableSecondA;
        public bool enableSecondB;
        public int level;
        //能量消耗参数
        public float energyCost;
        //初始化控制
        public bool initialized;
    }

    /// <summary>
    /// 时空 扭曲  --奇点爆炸
    /// </summary>
    public struct SkillChronoTwistTag : IComponentData
    {
        public float tagSurvivalTime;
        public bool enableSecondA;
        public bool enableSecondB;
        public int level;

        public float stratExplosionTime;
        public float skillDamageChangeParTag;
    }
    /// <summary>
    /// 时空扭曲第二阶段标签
    /// </summary>
    public struct SkillChronoTwistBTag : IComponentData
    {
        public float tagSurvivalTime;
        public float speed;
    }


    /// <summary>
    /// 幻影步
    /// </summary>
    public struct SkillPhantomStepTag : IComponentData
    {
        public float tagSurvivalTime;
        public bool enableSecondA;
        public bool enableSecondB;
        public bool enableSecondC;
        public int level;

    }
    /// <summary>
    /// 技能 暗影步
    /// </summary>
    public struct SkillShadowStepTag : IComponentData
    {

        public float tagSurvivalTime;
        public bool enableSecondA;
        public bool enableSecondB;

        public int level;
        public float skillDamageChangeParTag;

    }
    /// <summary>
    /// 时间缓速预加载标签
    /// </summary>
    public struct SkillTimeSlowTag_Hero : IComponentData, IEnableableComponent
    {

        public float tagSurvivalTime;
        public bool active;
        public bool enableSecondA;
        public bool enableSecondB;
        public int level;
        //是否初始化
        public bool initialized;
    }


    /// <summary>
    /// 烈焰灵刃 
    /// </summary>
    public struct SkillFlameSpiritBladeTag : IComponentData
    {
        public float tagSurvivalTime;

        public float speed;
        //形变参数
        public float scaleChangePar;
        public bool enableSecondA;
        public bool enableSecondB;
        //伤害变化参数,默认为0，外部赋值1+
        public float skillDamageChangeParTag;
        //等级
        public int level;

        public float3 startPosition;


    }
    /// <summary>
    /// 暗影之刺
    /// </summary>
    public struct SkillShadowStabTag : IComponentData
    {
        public float tagSurvivalTime;

        public float speed;
        //形变参数
        public float scaleChangePar;
        public bool enableSecondA;
        public bool enableSecondB;
        //伤害变化参数,默认为0，外部赋值1+
        public float skillDamageChangeParTag;
        //一级分裂概率
        public float secondAChance;
        //二级分裂概率
        public float secondBchance;

        //等级
        public int level;
        //分裂次数
        public int splitCount;
        public bool initialized;//初始化

    }
    /// <summary>
    ///烈焰爆发 爆发 幸运
    /// </summary>/
    public struct SkillFlameBurstTag : IComponentData
    {
        public float tagSurvivalTime;

        public float startBurstTime;
        public bool enableSecondA;
        public bool enableSecondB;
        public bool enbaleSecondC;
        public float skillDamageChangeParTag;
        //等级
        public int level;

    }
    /// <summary>
    /// 烈焰爆发记时器标签
    /// </summary>
    public struct SkillFlameBurst_Hero : IComponentData, IEnableableComponent
    {
        public float tagSurvivalTime;

    }


    /// <summary>
    /// 炽热烙印 技能标签
    /// </summary>
    public struct SkillScorchMarkTag : IComponentData
    {
        public float tagSurvivalTime;
        //形变参数
        public float scaleChangePar;
        public bool enableSecondA;
        public bool enableSecondC;
        //伤害变化参数,默认为0，外部赋值1+
        public float skillDamageChangeParTag;
        //等级
        public int level;

    }
    /// <summary>
    /// 执行标记类技能的预加载标签,这里根据持续时间进行-开关
    /// </summary>
    public struct PreDefineHeroSkillScorchMarkTag : IComponentData, IEnableableComponent
    {
        public float tagSurvivalTime;
    }


    /// <summary>
    /// 烈焰冲锋
    /// </summary>
    public struct SkillFlameChargeTag : IComponentData
    {

        public float tagSurvivalTime;
        //形变参数
        public float scaleChangePar;
        //等级
        public int level;
    }

    /// <summary>
    ///烈焰冲锋A阶段标签
    /// </summary>
    public struct SkillFlameChargeATag : IComponentData
    {

        public float tagSurvivalTime;
        //形变参数
        public float scaleChangePar;
        //等级
        public int level;
    }

    /// <summary>
    /// 冰霜路径 
    /// </summary>
    public struct SkillFrostTrailTag : IComponentData
    {
        public float tagSurvivalTime;
        //形变参数
        public float scaleChangePar;
        public bool enableSecondA;
        public bool enableSecondB;
        //伤害变化参数,默认为0，外部赋值1+
        public float skillDamageChangeParTag;
        //等级
        public int level;

        public float interval;

    }


    /// <summary>
    /// 冰霜之泾 B 阶段
    /// </summary>
    public struct SkillFrostTrailBTag : IComponentData
    {
        public float tagSurvivalTime;
        //形变参数
        public float scaleChangePar;
        public float skillDamageChangeParTag;
        //等级
        public int level;


    }
}

