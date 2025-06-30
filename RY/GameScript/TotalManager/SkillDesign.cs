using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlackDawn
{

    /// <summary>
    /// 英雄技能枚举
    /// </summary>
    public enum HeroSkillID
    {
        #region 核心技能 (Core)
        //脉冲  0号技能
        Pulse,             // 脉冲：生成一枚可推动的脉冲能量体，造成大范围元素伤害，并造成引力效果，持续4秒  
                           // 释放方式：Release/Flight；伤害类型：闪电/火焰/冰霜
        //暗影能量 1号技能
        DarkEnergy,        // 暗能：发射可充能3次的暗影能量球，对途经敌人造成范围元素伤害，持续5秒  
                           // 释放方式：Release/Flight；伤害类型：暗影
        //冰火之歌 2号技能
        IceFire,           // 冰火：召唤冰火元素围绕自身旋转，对触碰的敌人造成元素伤害，持续20秒  
                           // 释放方式：Release；伤害类型：火焰/冰霜
        //落雷3号技能
        ThunderStrike,     // 落雷：每隔1秒随机召唤累计12枚落雷，造成范围元素伤害，持续12秒  
                           // 释放方式：Release；伤害类型：闪电
        //法阵4号技能
        ArcaneCircle,          // 恢复：召唤一个每秒可持续回复10%元素系数生命值的法阵，法阵中的敌人持续损失生命值，持续20秒  
                           // 释放方式：Release/Protect；伤害类型：无
        //进击5号技能
        Advance,           // 进击：受到伤害减免70%，造成伤害增加30%，每秒回复3%元素系数生命值，持续10秒  
                           // 释放方式：Protect；伤害类型：无
        //寒冰6号技能    
        Frost,             // 寒冰：飞行道具发射时附带凝聚水气的寒冰球，寒冰球会持续飞行，造成冰霜伤害，持续5秒  
                           // 释放方式：Attach；伤害类型：冰霜
       //黑炎     
        BlackFlame,        // 黑炎：召唤黑炎，通过黑炎的敌人会受到周期性伤害，直至死亡，持续20秒，无视抗性  
                           // 释放方式：Release；伤害类型：火焰
       //横扫 
        Sweep,             // 横扫：在目标面前生成一把刀，横扫敌人，造成范围伤害  
                           // 释放方式：Release；伤害类型：物理
        //毒池
        PoisonPool,        // 毒池：在指定位置生成一片毒池，敌人经过造成中毒，并造成10秒的持续性伤害  
                           // 释放方式：Release；伤害类型：毒素
        //相位
        Phase,             // 相位：自身处于短暂无敌状态，持续5秒，结束回复一定生命值  
                           // 释放方式：Protect；伤害类型：无
        //元素共鸣
        ElementResonance,  // 元素共鸣：生成一个持续8秒的元素领域，范围内所有攻击附加随机元素效果  
                           // 释放方式：Release/Buff；伤害类型：全元素
        //暗影步        
        ShadowStep,        // 暗影步：瞬移至目标位置，留下一个持续3秒的暗影残影（吸引敌人攻击）  
                           // 释放方式：Teleport；伤害类型：暗影
        //静电牢笼
        ElectroCage,       // 静电牢笼：禁锢目标敌人4秒，期间闪电伤害对其+100%  
                           // 释放方式：Release/Buff；伤害类型：闪电
        //毒爆地雷
        MineBlast,         // 毒爆地雷：布置3颗隐形毒雷，触发时造成范围中毒并降低敌人30%移动速度  
                           // 释放方式：Release/Summon；伤害类型：毒素
        //暗影洪流
        ShadowTide,        // 暗影洪流：引导1.5秒，释放一道持续3秒的暗影光束（穿透所有敌人，每秒造成120%伤害）  
                           // 释放方式：Channel；伤害类型：魔法

        TimeSlow,          // 时间缓速：范围内敌人攻速/移速降低60%，持续5秒  
                           // 释放方式：Release/Debuff；伤害类型：无

        FlameCharge,       // 烈焰冲锋：向前冲刺，路径上留下火焰轨迹（持续燃烧3秒）  
                           // 释放方式：Dash；伤害类型：火焰

        FrostShield,       // 冰霜护盾：生成一个吸收300%攻击力的冰盾，破裂时冻结周围敌人2秒  
                           // 释放方式：Protect；伤害类型：冰霜

        ChainDevour,       // 连锁吞噬：发射一颗暗影弹，每击杀1个敌人弹跳至下一个目标，伤害+20%（最多弹跳5次）  
                           // 释放方式：Release/Bounce；伤害类型：暗影

        ThunderGrip,       // 雷霆之握：将目标敌人拉至面前并眩晕2秒，闪电伤害对其+50%持续4秒  
                           // 释放方式：Release/Melee；伤害类型：闪电

        ScorchMark,        // 炽炎烙印：标记敌人，使其受到的下3次火焰伤害+40%  
                           // 释放方式：Mark；伤害类型：火焰

        FrostNova,         // 寒霜新星：释放一圈冰霜冲击波，冻结范围内敌人1.5秒  
                           // 释放方式：Control；伤害类型：冰霜

        ShadowEmbrace,     // 暗影之拥：进入潜行状态3秒，下次攻击必定暴击并附加暗影撕裂（持续伤害）  
                           // 释放方式：Buff/Teleport；伤害类型：暗影

        PlagueSpread,      // 瘟疫蔓延：使目标敌人的毒素效果扩散至附近3个敌人  
                           // 释放方式：Passive；伤害类型：毒素

        ElementShield,     // 元素护盾：生成一个护盾，按当前元素属性提供不同效果  
                           // 释放方式：Protect；伤害类型：全元素

        ArcanePulse,       // 奥术脉冲：发射一道穿透性奥术波，对路径上所有敌人造成伤害并降低其抗性10%（可叠加）  
                           // 释放方式：Release/Debuff；伤害类型：魔法

        ChronoTwist,       // 时空扭曲：短暂延迟后传送至目标位置，并在原地留下一个残影（吸引敌人攻击）  
                           // 释放方式：Teleport/Summon；伤害类型：无

        FlameBurst,        // 烈焰爆发：对周围敌人造成火焰伤害并击退，若敌人处于燃烧状态则伤害翻倍  
                           // 释放方式：Control/Buff；伤害类型：火焰

        FrostTrail,        // 冰霜路径：创造一条持续6秒的冰霜路径，友军移速+30%，敌军移速-30%  
                           // 释放方式：Buff；伤害类型：冰霜

        LightningChain,    // 闪电链：释放一道闪电链，跳跃5次，每次伤害递减20%  
                           // 释放方式：Release/Bounce；伤害类型：闪电

        ShadowStab,        // 暗影之刺：瞬间闪烁至目标身后，造成暗影伤害并使其昏迷2秒  
                           // 释放方式：Teleport/Control；伤害类型：暗影
       //毒雨 -技能名称更改    
        PoisonRain,           // 毒雾陷阱：降下一片毒雨，造成毒素伤害 
                           // 释放方式：Release/Summon；伤害类型：毒素

        ElementBurst,      // 元素爆发：根据当前武器元素类型释放对应范围爆炸  
                           // 释放方式：Release/Explosion；伤害类型：全元素

        PhantomStep,       // 幻影步：进入1秒无敌状态，并在原地留下一个幻影（幻影持续3秒，可嘲讽敌人）  
                           // 释放方式：Teleport/Protect；伤害类型：无

        #endregion

        #region 终结技能 (Ultimate)

        Shadowless,            // 绝影：召唤一个持续30秒的黑影，可继承角色大部分属性，并周期性释放元素技能  
                               // 释放方式：Summon；伤害类型：无

        Fusion,                // 聚变：召唤一枚能量核弹，吸引周围怪物，并形成6次大范围强力爆炸，无视抗性  
                               // 释放方式：Summon；伤害类型：无

        Mastery,               // 宗师：在成的元素伤害提升50%  
                               // 释放方式：Buff；伤害类型：无

        ElementAnnihilation,   // 元素湮灭：引爆范围内所有元素效果，造成混合爆炸（冰+火+雷+毒+暗影各自计算伤害）  
                               // 释放方式：Buff；伤害类型：全元素

        VoidDescend,           // 虚空降临：召唤一座虚空之门，持续15秒，每2秒生成1个虚空生物（继承玩家50%攻击力）  
                               // 释放方式：Summon；伤害类型：暗影

        CelestialJudgment,     // 天劫雷罚：召唤持续6秒的雷暴领域，每秒降下5道闪电（优先攻击精英敌人）  
                               // 释放方式：Release/Summon；伤害类型：闪电

        IceAge,                // 冰河世纪：冻结全场敌人5秒，冰冻结束后受到300%冰霜伤害  
                               // 释放方式：Control；伤害类型：冰霜

        PurgatoryBlaze,        // 炼狱焚城：将战场转化为熔岩之地，持续12秒，所有敌人每秒受到火焰伤害并减速  
                               // 释放方式：Area；伤害类型：火焰

        PlagueStorm,           // 瘟疫风暴：生成一片移动的毒云，持续8秒，对范围内敌人叠加毒素并降低治疗效果80%  
                               // 释放方式：Release/Debuff；伤害类型：毒素

        TimeRift,              // 时空裂隙：回溯5秒前的状态（生命/弹药/技能CD），并重置范围内敌人的位置  
                               // 释放方式：Protect；伤害类型：无

        ShadowMirror,          // 暗影镜像：召唤2个镜像分身，继承玩家100%属性（持续20秒），但承受300%伤害  
                               // 释放方式：Summon；伤害类型：暗影

        Starfall,              // 星辰坠落：召唤12颗追踪星辰，每颗造成200%范围伤害（优先锁定高血量目标）  
                               // 释放方式：Summon；伤害类型：物理

        Doomsong,              // 终焉之歌：标记1个敌人，5秒后直接斩杀（对BOSS造成最大生命值30%伤害）  
                               // 释放方式：Mark；伤害类型：暗影

        DoomsdayJudgment,      // 末日审判：召唤陨石雨轰击全场，每颗陨石造成200%火焰伤害并点燃地面  
                               // 释放方式：Release；伤害类型：火焰

        AbsoluteZero,          // 绝对零度：冻结全场敌人4秒，冰冻结束后造成300%冰霜伤害  
                               // 释放方式：Control；伤害类型：冰霜

        ThunderAnnihilation,   // 雷霆灭世：召唤持续8秒的雷暴领域，每秒降下3道闪电（每道附带感电）  
                               // 释放方式：Release；伤害类型：闪电

        ShadowOnslaught,       // 暗影降临：召唤暗影之王，继承玩家200%攻击力，持续20秒  
                               // 释放方式：Summon；伤害类型：暗影

        PlagueTide,            // 瘟疫之潮：释放一片移动毒云，持续10秒，对范围内敌人造成毒素伤害并降低治疗效果50%  
                               // 释放方式：Release；伤害类型：毒素

        ElementStorm,          // 元素风暴：生成持续5秒的元素龙卷风，吸附敌人并随机造成冰/火/雷/毒/暗影伤害  
                               // 释放方式：Release；伤害类型：全元素

        ReaperScythe,          // 死神之镰：对目标敌人发动即死攻击，对普通敌人直接斩杀，对BOSS造成最大生命值40%伤害  
                               // 释放方式：Mark；伤害类型：暗影

        SeraphWrath,           // 炽天使之怒：进入10秒炽天使形态，攻击附带神圣火焰（伤害+50%，范围+30%）  
                               // 释放方式：Protect/Buff；伤害类型：火焰

        FrozenThrone,          // 冰封王座：创造一座持续12秒的冰封王座，坐在王座上可无限释放寒冰弹（每发100%伤害）  
                               // 释放方式：Summon；伤害类型：冰霜

        LightningAvatar,       // 闪电化身：化身为闪电形态，移速+100%，攻击附带连锁闪电，持续15秒  
                               // 释放方式：Transform；伤害类型：闪电

        ShadowLegion,          // 暗影军团：召唤3个暗影战士，每个继承玩家80%属性，持续25秒  
                               // 释放方式：Summon；伤害类型：暗影

        CatastrophicBlast,     // 毒爆天灾：在目标区域生成持续8秒的毒爆领域，每秒对敌人造成毒素伤害并叠加层数  
                               // 释放方式：Release/Debuff；伤害类型：毒素

        ElementFusion,         // 元素融合：释放5元素融合爆炸，造成500%范围伤害并触发所有元素反应  
                               // 释放方式：Release/Explosion；伤害类型：全元素

        TwilightMoment         // 终焉时刻：标记范围内所有敌人，3秒后对其发动即死判定  
                               // 释放方式：Mark；伤害类型：暗影

        #endregion
    }
    /// <summary>
    /// 英雄技能辅助映射枚举
    /// </summary>
    public enum HeroSkillAssistiveID
    { 
    // 冰火-》火
     IceFireFire,
     Frost_Fragment,
     ElectroCage_Lightning,
     PoisonRainB,
     ShadowTideA,
     ShadowTideB,

    }
    /// <summary>
    /// 英雄技能灵能分类
    /// </summary>
    public enum HeroSkillPsionicType
    { 
        //基础
      Basic,
      //灵能1
      PsionicA,
      //灵能2
      PsionicB,
      //灵能3
      PsionicC,
      //灵能12
      PsionicAB,
      //灵能13
      PsionicAC,
      //灵能23
      PsionicBC,
      //灵能123
      PsionicABC,

    }


    /// <summary>
    /// 英雄技能类型
    /// </summary>
    public enum SkillType
    {
        Core,  //核心
        Ultimate //终极
    }
    /// <summary>
    /// 技能释放方式
    /// </summary>
    public enum SkillCastType
    {
        Release,    // 释放
        Flight,     // 飞行
        Summon,     // 召唤
        Protect,    // 保护/防护
        Buff,       // 增益
        Debuff,     // 削弱
        Control,    // 控制
        Bounce,     // 弹跳
        Dash,       // 冲锋/位移
        Enhance,    // 强化/增强
        Teleport,   // 瞬移/传送
                    // …根据需要再扩充
    }


    #region 怪物技能模块

    public enum MonsterSkillID
    {
        //死亡脉冲
        DeathPulse,


    }


    #endregion

}