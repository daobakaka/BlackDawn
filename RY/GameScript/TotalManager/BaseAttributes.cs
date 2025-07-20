using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using BlackDawn.DOTS;
using Unity.Mathematics;
using Unity.Collections;
// 用于存放 IcomponentData 的序列化结构体以及对应枚举
namespace BlackDawn
{

    #region 枚举空间
    /// <summary>英雄称号系统</summary>
    public enum PlayerTitle
    {
        Default,


    }
    /// <summary>伤害触发类型枚举，用于在文字渲染系统中进行计算</summary>
    public enum DamageTriggerType
    {

        // 基础攻击类型
        NormalAttack,   // Q键 - 普通攻击（类型0）
        // 单一效果类型
        Vulnerable,     // W键 - 易伤（类型1）
        CriticalStrike, // E键 - 暴击（类型2）
        // 双效果组合
        VulnerableCritical, // R键 - 易伤暴击（类型3）
        Suppression,      // T键 - 压制（类型4）
        SuppressionVulnerable, // Y键 - 压制易伤（类型5）
        SuppressionCritical,  // U键 - 压制暴击（类型6）
        // 三效果组合
        SuppressionVulnCrit,    // I键 - 压制易伤暴击（类型7）
        DotDamage,//dot伤害
        Miss,//闪避
        Block,//格挡


    }



    /// <summary>被动特效类型枚举</summary>
    public enum PassiveEffectType
    {
        None = 0,
        DamageBoostOnHit,
        ChanceToPoison,
        LightningStack,
        // …根据需求继续扩展…
    }

    /// <summary>终极特效类型枚举</summary>
    public enum UltimateEffectType
    {
        None = 0,
        GaleStorm,
        ToxicExplosion,
        ThunderFury,
        // …根据需求继续扩展…
    }

    /// <summary>道具类型枚举</summary>
    public enum ItemType
    {
        Basic,
        Attack,
        Defense,
        Accessory,
        Upgrade,
        Legendary,
        Potion
    }

    /// <summary>英雄属性对应道具枚举</summary>
    public enum HeroItemAttributes
    {
        // —— 基础属性 ——  
        Intelligence,
        Strength,
        Agility,
        Willpower,
        Title,

        // —— 防御属性 ——  
        OriginalHp,
        Hp,
        DamageReduction,
        Energy,
        Armor,
        FrostResistance,
        LightningResistance,
        PoisonResistance,
        ShadowResistance,
        FireResistance,
        Dodge,
        MoveSpeed,
        Block,
        RNGState,

        // —— 元素伤害 ——  （对应 ElementDamage）  
        FrostDamage,
        LightningDamage,
        PoisonDamage,
        ShadowDamage,
        FireDamage,

        // —— 持续性伤害触发几率 ——  （对应 DOTProcChance）  
        BleedChance,
        FrostChance,
        LightningChance,
        PoisonChance,
        ShadowChance,
        FireChance,

        // —— 攻击属性 ——  
        AttackPower,
        AttackSpeed,
        ArmorPenetration,
        ElementalPenetration,
        ArmorBreak,
        ElementalBreak,
        ProjectilePenetration,
        Damage,
        PhysicalCritChance,
        CritDamage,
        VulnerabilityDamage,
        VulnerabilityChance,
        SuppressionDamage,
        SuppressionChance,
        LuckyStrikeChance,
        CooldownReduction,
        ElementalCritChance,
        ElementalCritDamage,
        DotDamage,
        DotCritChance,
        DotCritDamage,
        ExtraDamage,

        // —— 增益属性 ——  
        AtkRange,
        SkillRange,
        ExplosionRange,
        SkillDuration,
        HpRegen,
        EnergyRegen,

        // —— 控制能力 ——  
        Stun,
        Slow,
        Root,
        Fear,
        Freeze,
        Knockback,

        // —— 武器属性 ——  
        PropSpeed,
        ItemCapacity,
        ReloadTime,
        BaseAttackSpeed,
        Level,
        PelletCount,
        SpecialAttribute,
        MagazineCapacityDelta,
        BaseAttackSpeedDelta,
        PelletCountDelta,
        SpecialDelta,
        SectorAngle
    }




    /// <summary>所有怪物的标识</summary>
    public enum MonsterName
    {
        Zombie,        // 僵尸
        Albono,       //恶犬
        AlbonoUpper,  //恶龙升空者
        Skeleton,      // 骷髅
        Vampire,       // 吸血鬼
        Werewolf,      // 狼人
        Ghoul,         // 食尸鬼
                       // …根据项目再补充…
    }

    /// <summary>怪物分类</summary>
    public enum MonsterType
    {
        Normal = 0,    // 普通
        Elite = 1,    // 精英
        Boss = 2     // Boss
    }

    /// <summary>怪物攻击方式</summary>
    public enum MonsterAttackType
    {
        Melee = 0,    // 近战
        Ranged = 1,    // 远程
        Hybrid = 2     // 混合
    }


    /// <summary>
    /// 玩家基础飞行道具
    /// </summary>
    public enum HeroFlightPropID
    { DefaultProp,


    }
    /// <summary>
    /// 敌人飞行道具
    /// </summary>
    public enum MonsterFlightPropID
    {
        FrostLightningBall,

    }

    /// <summary>
    /// 场内shader或VFX特效枚举
    /// </summary>
    public enum ShaderEffectsID
    {
        //点燃效果
        BurningEffect,

    }
    /// <summary>
    /// 场内shader或VFX特效枚举
    /// </summary>
    public enum VFXEffectsID
    {
        //点燃效果
        DefaultEffexts,

    }



    /// <summary>
    /// 场内粒子特效
    /// </summary>
    public enum ParticleEffectsID
    {
        //点燃效果
        DefaultEffexts,

    }



    #endregion



    #region 基础属性结构空间
    /// <summary>
    /// 怪物基础属性
    /// </summary>
    [Serializable]
    public struct MonsterBaseAttribute : IComponentData
    {
        /// <summary>怪物标识（枚举）</summary>
        public MonsterName name;

        /// <summary>怪物分类（普通/精英/Boss）</summary>
        public MonsterType type;

        /// <summary>怪物等级</summary>
        public int level;

        /// <summary>攻击方式（近战/远程/混合）</summary>
        public MonsterAttackType attackType;
    }


    /// <summary>
    /// 基础属性
    /// </summary>
    [Serializable]
    public struct BaseAttribute
    {
        /// <summary>
        /// 智力
        /// </summary>
        public float intelligence;

        /// <summary>
        /// 力量
        /// </summary>
        public float strength;

        /// <summary>
        /// 敏捷
        /// </summary>
        public float agility;

        /// <summary>
        /// 意力
        /// </summary>
        public float willpower;

        /// <summary>
        /// 称号
        /// </summary>
        public PlayerTitle title;
        /// <summary>
        /// 基础存放blob 的引用
        /// </summary>
       // public BlobAssetReference<OriginalHeroAttribute> blob;

    }
    /// <summary>
    /// 各元素抗性
    /// </summary>
    [Serializable]
    public struct Resistances
    {
        /// <summary>冰霜抗性</summary>
        public float frost;
        /// <summary>闪电抗性</summary>
        public float lightning;
        /// <summary>毒素抗性</summary>
        public float poison;
        /// <summary>暗影抗性</summary>
        public float shadow;
        /// <summary>火焰抗性</summary>
        public float fire;
    }
    /// <summary>
    /// 怪物防御属性
    /// </summary>
    [Serializable]
    public struct MonsterDefenseAttribute : IComponentData
    {
        /// <summary>原始生命值</summary>
        public float originalHp;

        /// <summary>生命值</summary>
        public float hp;

        /// <summary>伤害减免</summary>
        public float damageReduction;

        /// <summary>精力</summary>
        public float energy;

        /// <summary>护甲</summary>
        public float armor;

        /// <summary>各元素抗性</summary>
        public Resistances resistances;

        /// <summary>闪避率</summary>
        public float dodge;

        /// <summary>移动速度</summary>
        public float moveSpeed;

        /// <summary>格挡率</summary>
        public float block;

        /// <summary>RNGState,随机数据状态，为ECS系统生成随机数</summary>
        public uint rngState;

        /// <summary>死亡后剩余存活时间</summary>
        public float survivalTime;

        /// <summary>死亡判定，json无值默认false</summary>
        public bool death;

        /// <summary>持续伤害记数器</summary>
        public float overTimeDamageCount;

    }

    /// <summary>
    /// 临时伤害组件，用于伤害字体渲染，升级为可关闭组件，减少查询消耗
    /// </summary>
    [Serializable]
    public struct MonsterTempDamageText : IComponentData, IEnableableComponent

    {
        //这里只要受到伤害就判断一次重写，然后刷新剩余时间
        public bool underAttack;
        //目前这里集成到GPU端完成了
        public float survivalTime;
        public float hurtVlue;

        //判断几种状态，从道具和技能结构体中传过来
        public DamageTriggerType damageTriggerType;

    }

    /// <summary>
    /// 临时DOT伤害组件，用于伤害字体渲染，升级为可关闭组件，减少查询消耗
    /// </summary>
    [Serializable]
    public struct MonsterTempDotDamageText : IComponentData, IEnableableComponent

    {
        //这里只要受到伤害就判断一次重写，然后刷新剩余时间
        public bool underAttack;
        //目前这里集成到GPU端完成了
        public float survivalTime;
        public float hurtVlue;

        //判断几种状态，从道具和技能结构体中传过来
        public DamageTriggerType damageTriggerType;

    }




    /// <summary>
    /// 防御属性
    /// </summary>
    [Serializable]
    public struct DefenseAttribute : IComponentData
    {
        /// <summary>原始生命值</summary>
        public float originalHp;

        /// <summary>生命值</summary>
        public float hp;

        /// <summary>伤害减免</summary>
        public float damageReduction;

        /// <summary>精力</summary>
        public float energy;

        /// <summary>护甲</summary>
        public float armor;

        /// <summary>各元素抗性</summary>
        public Resistances resistances;

        /// <summary>闪避率</summary>
        public float dodge;

        /// <summary>移动速度</summary>
        public float moveSpeed;

        /// <summary>格挡率</summary>
        public float block;
        /// <summary>屏障</summary>
        public float universalBarrier;

        ///<summary>寒冰屏障</summary>
        public float frostBarrier;

        /// <summary>RNGState,随机数据状态，为ECS系统生成随机数</summary>
        public uint rngState;

        /// <summary>死亡后剩余存活时间</summary>
        public float survivalTime;

        /// <summary>死亡判定，json无值默认false</summary>
        public bool death;
        //临时减伤
        public TempDefenseAttribute  tempDefense;
    }


    /// <summary>
    /// 临时防预属性，用于英雄的临时减伤计算
    /// </summary>
    [Serializable]
    public struct TempDefenseAttribute
    {
        //进击减伤
        public float advanceDamageReduction;
        //元素护盾减伤
        public float elmentShieldDamageReduction;
        //幻影步3阶段减伤
        public float PhantomStepC;

    }

    /// <summary>
    /// 各元素伤害
    /// </summary>
    [Serializable]
    public struct ElementDamage
    {
        /// <summary>冰霜伤害</summary>
        public float frostDamage;
        /// <summary>闪电伤害</summary>
        public float lightningDamage;
        /// <summary>毒素伤害</summary>
        public float poisonDamage;
        /// <summary>暗影伤害</summary>
        public float shadowDamage;
        /// <summary>火焰伤害</summary>
        public float fireDamage;
    }

    /// <summary>
    /// 各元素持续性伤害触发几率
    /// </summary>
    [Serializable]
    public struct DOTProcChance
    {
        /// <summary>流血伤害触发几率 [0-1]</summary>
        public float bleedChance;

        /// <summary>冰霜伤害触发几率 [0-1]</summary>
        public float frostChance;

        /// <summary>闪电伤害触发几率 [0-1]</summary>
        public float lightningChance;

        /// <summary>毒素伤害触发几率 [0-1]</summary>
        public float poisonChance;

        /// <summary>暗影伤害触发几率 [0-1]</summary>
        public float shadowChance;

        /// <summary>火焰伤害触发几率 [0-1]</summary>
        public float fireChance;
    }

    /// <summary>
    /// 怪物攻击属性--后期可以简化
    /// </summary>
    [Serializable]
    public struct MonsterAttackAttribute : IComponentData
    {
        /// <summary>攻击力</summary>
        public float attackPower;
        /// <summary>攻击速度</summary>
        public float attackSpeed;
        /// <summary>武器攻击速度</summary>
        public float weaponAttackSpeed;
        /// <summary>护甲穿透</summary>
        public float armorPenetration;
        /// <summary>元素穿透</summary>
        public float elementalPenetration;
        /// <summary>护甲削弱</summary>
        public float armorBreak;
        /// <summary>元素削弱</summary>
        public float elementalBreak;
        /// <summary>飞行道具穿透</summary>
        public float projectilePenetration;
        /// <summary>伤害</summary>
        public float damage;
        /// <summary>物理暴击率</summary>
        public float physicalCritChance;
        /// <summary>暴击伤害</summary>
        public float critDamage;
        /// <summary>易伤伤害</summary>
        public float vulnerabilityDamage;
        /// <summary>易伤几率</summary>
        public float vulnerabilityChance;
        /// <summary>压制伤害</summary>
        public float suppressionDamage;
        /// <summary>元素伤害</summary>
        public ElementDamage elementalDamage;
        /// <summary>DOT触发几率</summary>
        public DOTProcChance dotProcChance;
        /// <summary>幸运一击几率</summary>
        public float luckyStrikeChance;
        /// <summary>冷却缩减</summary>
        public float cooldownReduction;
        /// <summary>压制几率</summary>
        public float suppressionChance;
        /// <summary>元素暴击率</summary>
        public float elementalCritChance;
        /// <summary>元素暴伤</summary>
        public float elementalCritDamage;
        /// <summary>持续性伤害</summary>
        public float dotDamage;
        /// <summary>持续性伤害暴击几率</summary>
        public float dotCritChance;
        /// <summary>持续性暴伤</summary>
        public float dotCritDamage;
        /// <summary>额外伤害</summary>
        public float extraDamage;
    }

    /// <summary>
    /// 攻击属性
    /// </summary>
    [Serializable]
    public struct AttackAttribute : IComponentData
    {
        /// <summary>攻击力</summary>
        public float attackPower;
        /// <summary>攻击速度</summary>
        public float attackSpeed;
        /// <summary>武器攻击速度</summary>
        public float weaponAttackSpeed;
        /// <summary>护甲穿透</summary>
        public float armorPenetration;
        /// <summary>元素穿透</summary>
        public float elementalPenetration;
        /// <summary>护甲削弱</summary>
        public float armorBreak;
        /// <summary>元素削弱</summary>
        public float elementalBreak;
        /// <summary>飞行道具穿透</summary>
        public float projectilePenetration;
        /// <summary>伤害</summary>
        public float damage;
        /// <summary>物理暴击率</summary>
        public float physicalCritChance;
        /// <summary>暴击伤害</summary>
        public float critDamage;
        /// <summary>易伤伤害</summary>
        public float vulnerabilityDamage;
        /// <summary>易伤几率</summary>
        public float vulnerabilityChance;
        /// <summary>压制伤害</summary>
        public float suppressionDamage;
        /// <summary>元素伤害</summary>
        public ElementDamage elementalDamage;
        /// <summary>DOT触发几率</summary>
        public DOTProcChance dotProcChance;
        /// <summary>幸运一击几率</summary>
        public float luckyStrikeChance;
        /// <summary>冷却缩减</summary>
        public float cooldownReduction;
        /// <summary>压制几率</summary>
        public float suppressionChance;
        /// <summary>元素暴击率</summary>
        public float elementalCritChance;
        /// <summary>元素暴伤</summary>
        public float elementalCritDamage;
        /// <summary>持续性伤害</summary>
        public float dotDamage;
        /// <summary>持续性伤害暴击几率</summary>
        public float dotCritChance;
        /// <summary>持续性暴伤</summary>
        public float dotCritDamage;
        /// <summary>额外伤害</summary>
        public float extraDamage;
        //动态伤害结构体
        public HeroDynamicalAttackAttribute heroDynamicalAttack;
    }
    // 动态伤害结构体,加在外面
    [Serializable]
    public struct HeroDynamicalAttackAttribute : IComponentData
    {
        //大于1通用累计
        public float tempMasterDamagePar;
        //小于1形式累计
        public float tempAdvanceDamagePar;
        //幻影步3阶段
        public float tempPhantomStepCpar;


    }
    /// <summary>
    /// 技能伤害结构体  
    /// 包含技能等级、基础伤害、各伤害因子，以及可选的额外攻击属性  
    /// 这里普通伤害型技能造成的伤害1 基础属性 武器等级 技能等级  2. 加成影响(开放式编码 攻击力 幸运一击 等等)
    /// 这里还存储灵能结构体
    /// </summary>
    [Serializable]
    public struct SkillDamageAttribute
    {
        /// <summary>技能名称,这里在json文件里面为了方便读取，直接采用  int 数字转换</summary>
        public HeroSkillID skillName;
        /// <summary>技能等级</summary>
        public int skillLevel;
        /// <summary>技能基础伤害</summary>
        public float baseDamage;
        /// <summary>物理伤害因子</summary>
        public float physicalFactor;
        /// <summary>冰霜伤害因子</summary>
        public float frostFactor;
        /// <summary>闪电伤害因子</summary>
        public float lightningFactor;
        /// <summary>毒素伤害因子</summary>
        public float poisonFactor;
        /// <summary>暗影伤害因子</summary>
        public float shadowFactor;
        /// <summary>火焰伤害因子</summary>
        public float fireFactor;
        /// <summary>技能测试参数，后面可以根据灵能插槽进行计算</summary>
        public int skillPsionicType;
        /// <summary>技能释放时间，根据不同的技能的动画点读取</summary>
        public float skillRelaseTime;

        /// <summary>DOT触发几率，这里增加dot的技能临时触发描述</summary>
        public DOTProcChance dotSkillProcChance;

        /// <summary>属性加成因子1</summary>
        public SkillBonus skillBonus1;
        /// <summary>属性加成因子2</summary>
        public SkillBonus skillBonus2;
        /// <summary>属性加成因子3</summary>
        public SkillBonus skillBonus3;

        /// <summary>
        /// 默认4个灵能插槽，根据技能的属性分配
        /// </summary>
        public Psionic psionic1;
        public Psionic psionic2;
        public Psionic psionic3;
        public Psionic psionic4;
    }
   
    /// 单个加成的键值对  
    /// </summary>
    [Serializable]
    public struct SkillBonus 
    {
        /// <summary>加成键名（FixedString 最多可存 64 字节）这里恰好可以和道具的属性增益通用</summary>
        public HeroItemAttributes bonusKey;
        /// <summary>加成数值</summary>
        public float bonusValue;
    }
    /// <summary>
    /// 灵能 确定所属技能，2 灵能枚举 3 灵能等级
    /// </summary>
    public struct Psionic
    {
        public HeroSkillID skillName;
        public PsionicsID psionicID;
        public int level;
    }


    /// <summary>
    /// 控制效果触发时的伤害加成，第二阶段增加，仅套在英雄身上
    /// </summary>
    [Serializable]
    public struct ControlDamageAttribute
    {
        /// <summary>昏迷 额外伤害（+）</summary>
        public float StunBonus;
        /// <summary>昏迷 伤害倍率（x）</summary>
        public float StunMultiplier;

        /// <summary>减速 额外伤害（+）</summary>
        public float SlowBonus;
        /// <summary>减速 伤害倍率（x）</summary>
        public float SlowMultiplier;

        /// <summary>定身 额外伤害（+）</summary>
        public float RootBonus;
        /// <summary>定身 伤害倍率（x）</summary>
        public float RootMultiplier;

        /// <summary>恐惧 额外伤害（+）</summary>
        public float FearBonus;
        /// <summary>恐惧 伤害倍率（x）</summary>
        public float FearMultiplier;

        /// <summary>冻结 额外伤害（+）</summary>
        public float FreezeBonus;
        /// <summary>冻结 伤害倍率（x）</summary>
        public float FreezeMultiplier;

        /// <summary>击退 额外伤害（+）</summary>
        public float KnockbackBonus;
        /// <summary>击退 伤害倍率（x）</summary>
        public float KnockbackMultiplier;

        /// <summary>牵引 额外伤害（+）</summary>
        public float PullBonus;
        /// <summary>牵引 伤害倍率（x）</summary>
        public float PullMultiplier;

        /// <summary>爆炸 额外伤害（+）</summary>
        public float ExplosionBonus;
        /// <summary>爆炸 伤害倍率（x）</summary>
        public float ExplosionMultiplier;
    }

    /// <summary>
    /// DOT 触发时的伤害加成，第二阶段增加，仅仅套在英雄身上
    /// </summary>
    [Serializable]
    public struct DotDamageAttribute
    {
        /// <summary>冰霜 额外伤害（+）</summary>
        public float FrostBonus;
        /// <summary>冰霜 伤害倍率（x）</summary>
        public float FrostMultiplier;

        /// <summary>闪电 额外伤害（+）</summary>
        public float LightningBonus;
        /// <summary>闪电 伤害倍率（x）</summary>
        public float LightningMultiplier;

        /// <summary>毒素 额外伤害（+）</summary>
        public float PoisonBonus;
        /// <summary>毒素 伤害倍率（x）</summary>
        public float PoisonMultiplier;

        /// <summary>暗影 额外伤害（+）</summary>
        public float ShadowBonus;
        /// <summary>暗影 伤害倍率（x）</summary>
        public float ShadowMultiplier;

        /// <summary>火焰 额外伤害（+）</summary>
        public float FireBonus;
        /// <summary>火焰 伤害倍率（x）</summary>
        public float FireMultiplier;

        /// <summary>流血 额外伤害（+）</summary>
        public float BleedBonus;
        /// <summary>流血 伤害倍率（x）</summary>
        public float BleedMultiplier;
    }

    /// <summary>
    /// 怪物增益属性
    /// </summary>
    [Serializable]
    public struct MonsterGainAttribute :IComponentData
    {
        /// <summary>攻击范围</summary>
        public float atkRange;

        /// <summary>技能范围</summary>
        public float skillRange;

        /// <summary>爆炸范围</summary>
        public float explosionRange;

        /// <summary>技能持续时间</summary>
        public float skillDuration;

        /// <summary>生命恢复</summary>
        public float hpRegen;

        /// <summary>精力恢复</summary>
        public float energyRegen;
    }
    /// <summary>
    /// 增益属性
    /// </summary>
    [Serializable]
    public struct GainAttribute : IComponentData
    {
        /// <summary>攻击范围</summary>
        public float atkRange;

        /// <summary>技能范围</summary>
        public float skillRange;

        /// <summary>爆炸范围</summary>
        public float explosionRange;

        /// <summary>技能持续时间</summary>
        public float skillDuration;

        /// <summary>生命恢复</summary>
        public float hpRegen;

        /// <summary>精力恢复</summary>
        public float energyRegen;
        
        /// <summary>冷却缩减</summary>
        public float  cooldownReduction;
        /// <summary>动态冷却缩减</summary>
        public DymicalCooldownReduction dymicalCooldownReduction;
    }


    /// <summary>
    /// 动态冷却缩减 结构体
    /// </summary>
    [Serializable]
    public struct DymicalCooldownReduction
    {
        //进击A阶段提供的冷却缩减
        public float advanceACooldownReduction;


    }
    /// <summary>
    /// 各元素抗性削弱
    /// </summary>
    [Serializable]
    public struct ResistanceReduction
    {
        /// <summary>冰霜抗性削弱</summary>
        public float frost;
        /// <summary>闪电抗性削弱</summary>
        public float lightning;
        /// <summary>毒素抗性削弱</summary>
        public float poison;
        /// <summary>暗影抗性削弱</summary>
        public float shadow;
        /// <summary>火焰抗性削弱</summary>
        public float fire;
    }

    /// <summary>
    /// 持续性伤害减益
    /// </summary>
    [Serializable]
    public struct DotDebuff
    {
        /// <summary>冰霜持续性伤害</summary>
        public float frost;
        /// <summary>闪电持续性伤害</summary>
        public float lightning;
        /// <summary>毒素持续性伤害</summary>
        public float poison;
        /// <summary>暗影持续性伤害</summary>
        public float shadow;
        /// <summary>火焰持续性伤害</summary>
        public float fire;
        /// <summary>流血持续性伤害</summary>
        public float bleed;
    }


    /// <summary>
    /// 怪物减益属性--后期可根据需求更改,这里更多的用于怪物减益情形对伤害计算的影响
    /// </summary>
    [Serializable]
    public struct MonsterDebuffAttribute : IComponentData
    {
        /// <summary>伤害加深（目标受到更多伤害的倍率）</summary>
        public float damageAmplification;

        /// <summary>护甲削弱</summary>
        public float armorReduction;

        /// <summary>抗性削弱</summary>
        public ResistanceReduction resistanceReduction;

        /// <summary>移速削弱</summary>
        public float moveSpeedReduction;

        /// <summary>生命削弱</summary>
        public float healthReduction;

        /// <summary>持续性伤害</summary>
        public DotDebuff dotDebuff;

        /// <summary>生命抑制（最大生命恢复速率降低）</summary>
        public float healthSuppression;

        /// <summary>精力抑制（最大精力恢复速率降低）</summary>
        public float energySuppression;
        /// <summary>整合dot伤害，默认颜色灰色</summary>
        public float totalDotDamage;
        /// <summary>DOT 的累加记时器</summary>
        public float dotTimer;
        //---将部分技能的控制缓存，交给debuff组件来处理
        public float3 thunderPosition; //雷霆之握的目标位置
        public float thunderGripEndTimer;//雷霆之握timer
        //饥渴吞噬的buffer以及特效处理
        public bool toClearDotBuffer;
        public float Devourtimer;
        //黑炎的状态阶段,1为开启A阶段，渲染系统直接进行 黑炎的伤害计算
        public float blackFrameActiveA;
        
    }

    /// <summary>
    /// 减益属性
    /// </summary>
    [Serializable]
    public struct DebuffAttribute :IComponentData
    {
        /// <summary>伤害加深（目标受到更多伤害的倍率）</summary>
        public float damageAmplification;

        /// <summary>护甲削弱</summary>
        public float armorReduction;

        /// <summary>抗性削弱</summary>
        public ResistanceReduction resistanceReduction;

        /// <summary>移速削弱</summary>
        public float moveSpeedReduction;

        /// <summary>生命削弱</summary>
        public float healthReduction;

        /// <summary>持续性伤害</summary>
        public DotDebuff dotDebuff;

        /// <summary>生命抑制（最大生命上限降低）</summary>
        public float healthSuppression;

        /// <summary>精力抑制（最大精力上限降低）</summary>
        public float energySuppression;

        /// <summary>整合dot伤害，默认颜色灰色</summary>
        public float totalDotDamage;
    }

    /// <summary>
    /// 怪物伤害池属性,这里激活标志用于代表true
    /// </summary>
    [Serializable]
    public struct MonsterLossPoolAttribute : IComponentData
    {
        /// <summary>
        /// 攻击计时器
        /// </summary>
        public float attackTimer;

        // ——— 冰霜池 ———
        /// <summary>冰霜伤害池</summary>
        public float frostPool;
        /// <summary>冰霜池计时器</summary>
        public float frostTimer;
        /// <summary>冰霜池激活标志</summary>
        public float frostActive;

        // ——— 火焰池 ———
        /// <summary>火焰伤害池</summary>
        public float firePool;
        /// <summary>火焰池计时器</summary>
        public float fireTimer;
        /// <summary>火焰池激活标志</summary>
        public float fireActive;

        // ——— 毒素池 ———
        /// <summary>毒素伤害池</summary>
        public float poisonPool;
        /// <summary>毒素池计时器</summary>
        public float poisonTimer;
        /// <summary>毒素池激活标志</summary>
        public float poisonActive;

        // ——— 闪电池 ———
        /// <summary>闪电伤害池</summary>
        public float lightningPool;
        /// <summary>闪电池计时器</summary>
        public float lightningTimer;
        /// <summary>闪电池激活标志</summary>
        public float lightningActive;

        // ——— 暗影池 ———
        /// <summary>暗影伤害池</summary>
        public float shadowPool;
        /// <summary>暗影池计时器</summary>
        public float shadowTimer;
        /// <summary>暗影池激活标志</summary>
        public float shadowActive;

        // ——— 流血池 ———
        /// <summary>流血伤害池</summary>
        public float bleedPool;
        /// <summary>流血池计时器</summary>
        public float bleedTimer;
        /// <summary>流血池激活标志</summary>
        public float bleedActive;

        //--黑炎状态,永久激活
        public float blackFrameActive;
        public float blackFrameTimer;
    }
    /// <summary>
    /// 伤害池属性
    /// </summary>
    [Serializable]
    public struct LossPoolAttribute :IComponentData
    {
        /// <summary>
        /// 攻击计时器
        /// </summary>
        public float attackTimer;

        // ——— 冰霜池 ———
        /// <summary>冰霜伤害池</summary>
        public float frostPool;
        /// <summary>冰霜池计时器</summary>
        public float frostTimer;
        /// <summary>冰霜池激活标志</summary>
        public float frostActive;

        // ——— 火焰池 ———
        /// <summary>火焰伤害池</summary>
        public float firePool;
        /// <summary>火焰池计时器</summary>
        public float fireTimer;
        /// <summary>火焰池激活标志</summary>
        public float fireActive;

        // ——— 毒素池 ———
        /// <summary>毒素伤害池</summary>
        public float poisonPool;
        /// <summary>毒素池计时器</summary>
        public float poisonTimer;
        /// <summary>毒素池激活标志</summary>
        public float poisonActive;

        // ——— 闪电池 ———
        /// <summary>闪电伤害池</summary>
        public float lightningPool;
        /// <summary>闪电池计时器</summary>
        public float lightningTimer;
        /// <summary>闪电池激活标志</summary>
        public float lightningActive;

        // ——— 暗影池 ———
        /// <summary>暗影伤害池</summary>
        public float shadowPool;
        /// <summary>暗影池计时器</summary>
        public float shadowTimer;
        /// <summary>暗影池激活标志</summary>
        public float shadowActive;

        // ——— 流血池 ———
        /// <summary>流血伤害池</summary>
        public float bleedPool;
        /// <summary>流血池计时器</summary>
        public float bleedTimer;
        /// <summary>流血池激活标志</summary>
        public float bleedActive;
    }

    /// <summary>
    /// 控制能力属性
    /// </summary>
    [Serializable]
    public struct ControlAbilityAttribute : IComponentData
    {
        /// <summary>昏迷</summary>
        public float stun;

        /// <summary>减速</summary>
        public float slow;

        /// <summary>定身（定身/束缚）</summary>
        public float root;

        /// <summary>恐惧</summary>
        public float fear;

        /// <summary>冻结</summary>
        public float freeze;

        /// <summary>击退</summary>
        public float knockback;

        /// <summary>牵引</summary>
        public float pull;

        /// <summary>爆炸</summary>
        public float explosion;

        ///// <summary>昏迷</summary>
        //public float tempStun;

        ///// <summary>定身（定身/束缚）</summary>
        //public float tempRoot;

        ///// <summary>恐惧</summary>
        //public float tempFear;

        ///// <summary>冻结</summary>
        //public float tempFreeze;



    }
    /// <summary>
    /// 怪物被控制效果属性
    /// </summary>
    [Serializable]
    public struct MonsterControlledEffectAttribute : IComponentData
    {
        /// <summary>昏迷</summary>
        public float stun;
        /// <summary>昏迷计时器</summary>
        public float stunTimer;
        /// <summary>昏迷是否生效</summary>
        public bool stunActive;

        /// <summary>减速</summary>
        public float slow;
        /// <summary>减速计时器</summary>
        public float slowTimer;
        /// <summary>减速是否生效</summary>
        public bool slowActive;

        /// <summary>定身（定身/束缚）</summary>
        public float root;
        /// <summary>定身计时器</summary>
        public float rootTimer;
        /// <summary>定身是否生效</summary>
        public bool rootActive;

        /// <summary>恐惧</summary>
        public float fear;
        /// <summary>恐惧计时器</summary>
        public float fearTimer;
        /// <summary>恐惧是否生效</summary>
        public bool fearActive;

        /// <summary>冻结</summary>
        public float freeze;
        /// <summary>冻结计时器</summary>
        public float freezeTimer;
        /// <summary>冻结是否生效</summary>
        public bool freezeActive;

        /// <summary>击退</summary>
        public float knockback;
        /// <summary>击退计时器</summary>
        public float knockbackTimer;
        /// <summary>击退是否生效</summary>
        public bool knockbackActive;

        /// <summary>牵引</summary>
        public float pull;
        /// <summary>牵引计时器</summary>
        public float pullTimer;
        /// <summary>牵引中心</summary>
        public float3 pullCenter;
        /// <summary>牵引是否生效</summary>
        public bool pullActive;

        /// <summary>爆炸</summary>
        public float explosion;
        /// <summary>爆炸计时器</summary>
        public float explosionTimer;
        /// <summary>爆炸中心</summary>
        public float3 explosionCenter;
        /// <summary>爆炸是否生效</summary>
        public bool explosionActive;
    }


    /// <summary>
    /// 控制能力属性
    /// </summary>
    [Serializable]
    public struct MonsterControlAbilityAttribute :IComponentData
    {
        /// <summary>昏迷</summary>
        public float stun;

        /// <summary>减速</summary>
        public float slow;

        /// <summary>定身（定身/束缚）</summary>
        public float root;

        /// <summary>恐惧</summary>
        public float fear;

        /// <summary>冻结</summary>
        public float freeze;

        /// <summary>击退</summary>
        public float knockback;

        /// <summary>牵引</summary>
        public float pull;

        /// <summary>爆炸</summary>
        public float explosion;
    }
    /// <summary>
    /// 被控制效果属性
    /// </summary>
    [Serializable]
    public struct ControlledEffectAttribute :IComponentData
    {
        /// <summary>昏迷</summary>
        public float stun;
        /// <summary>昏迷计时器</summary>
        public float stunTimer;
        /// <summary>昏迷是否生效</summary>
        public bool stunActive;

        /// <summary>减速</summary>
        public float slow;
        /// <summary>减速计时器</summary>
        public float slowTimer;
        /// <summary>减速是否生效</summary>
        public bool slowActive;

        /// <summary>定身（定身/束缚）</summary>
        public float root;
        /// <summary>定身计时器</summary>
        public float rootTimer;
        /// <summary>定身是否生效</summary>
        public bool rootActive;

        /// <summary>恐惧</summary>
        public float fear;
        /// <summary>恐惧计时器</summary>
        public float fearTimer;
        /// <summary>恐惧是否生效</summary>
        public bool fearActive;

        /// <summary>冻结</summary>
        public float freeze;
        /// <summary>冻结计时器</summary>
        public float freezeTimer;
        /// <summary>冻结是否生效</summary>
        public bool freezeActive;

        /// <summary>击退</summary>
        public float knockback;
        /// <summary>击退计时器</summary>
        public float knockbackTimer;
        /// <summary>击退是否生效</summary>
        public bool knockbackActive;

        /// <summary>牵引</summary>
        public float pull;
        /// <summary>牵引计时器</summary>
        public float pullTimer;
        /// <summary>牵引中心</summary>
        public float3 pullCenter;
        /// <summary>牵引是否生效</summary>
        public bool pullActive;

        /// <summary>爆炸</summary>
        public float explosion;
        /// <summary>爆炸计时器</summary>
        public float explosionTimer;
        /// <summary>爆炸中心</summary>
        public float3 explosionCenter;
        /// <summary>爆炸是否生效</summary>
        public bool explosionActive;
    }


    /// <summary>
    /// 武器属性（含升级增量与特效）
    /// </summary>
    [Serializable]
    public struct WeaponAttribute
    {
        /// <summary>飞行道具飞行速度</summary>
        public float propSpeed;
        /// <summary>初始弹匣容量</summary>
        public int itemCapacity;
        /// <summary>装填时间（秒）</summary>
        public float reloadTime;
        /// <summary>基础攻速（次/秒，不含升级增量）</summary>
        public float baseAttackSpeed;
        /// <summary>当前等级,这里还可用于技能伤害计算</summary>
        public int level;

        /// <summary>弹片数量</summary>
        public int pelletCount;
        /// <summary>特殊数量</summary>
        public int specialAttribute;

        // —— 升级增量字段 —— 
        /// <summary>每级弹匣容量增量</summary>
        public int magazineCapacityDelta;
        /// <summary>每级基础攻速增量</summary>
        public float baseAttackSpeedDelta;
        /// <summary>每级弹片数增量（散弹等多发武器用）</summary>
        public int pelletCountDelta;
        /// <summary>每级特殊属性增量（例如额外伤害、范围等复合值）</summary>
        public int specialDelta;
        /// <summary>扇形角</summary>
        public float sectorAngle;
        /// <summary>被动特效类型</summary>
        public PassiveEffectType passiveEffect;
        /// <summary>终极特效类型</summary>
        public UltimateEffectType ultimateEffect;
    }


    #endregion


    #region 其他
    //---

    //----------------------关于设计的buffHandel的部分-----------------------------------

    /// <summary>
    /// 元素伤害类型
    /// </summary>
    public enum ElementType
    {
        None,
        Ice, //冰霜
        Lightning, //闪电
        Poison, //毒素
        Shadow, //暗影
        Fire //火焰
    }

    public struct Buff
    {
        /// buff的基本信息

        //TODO:可能需要buff标识
        //public enum e;

        /// <summary>
        /// buff名字
        /// <summary>

        /// <summary>
        /// 是否永久
        /// </summary>
        public bool isForever;
        /// <summary>
        /// 持续时间
        /// </summary>
        public float duration;
        /// <summary>
        /// 间隔时间
        /// </summary>
        public float interval;
        /// <summary>
        /// 元素伤害类型
        /// </summary>
        public ElementType elementType;
        /// <summary>
        /// buff层数
        /// </summary>
        public int level;
        /// <summary>
        /// buff的属性信息
        /// </summary>
        public MonsterAttributeCmpt MonsterAttribute;

        public HeroAttributeCmpt heroAttribute;

        /// <summary>
        /// buff的各回调点
        /// <summary>

    }
    #endregion
}
