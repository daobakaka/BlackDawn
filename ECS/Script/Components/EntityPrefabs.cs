using System;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using System.Linq;
//集中到一个实例上，不词用buffer这样调度和生成更简单
namespace BlackDawn.DOTS
{
    public class EntityPrefabs : MonoBehaviour
    {
        [Header("英雄")]
        public GameObject heroObj;
        public GameObject heroDetector;

        [Header("英雄技能映射")]
        public HeroSkillPrefabPair[] skillPrefabs;

        [Header("英雄技能辅助映射")]
        public HeroSkillAssistivePrefabPair[] skillAssistivePrefabs;

        [Header("怪物技能映射")]
        public MonsterSkillPrefabPair[] monsterSkillPrefabs;

        [Header("怪物实体映射")]
        public MonsterPrefabPair[] monsterPrefabs;

        [Header("英雄飞行道具映射")]
        public HeroFlightPropPair[] flightPropPrefabs;

        [Header("敌人飞行道具映射")]
        public MonsterFlightPropPair[] enemyFlightPropPrefabs;

        [Header("Shader")]
        public ShaderEffectsPair[] shaderEffectsPrefabs;

        [Header("VFX 特效映射")]
        public VFXEffectsPair[] vfxEffectsPrefabs;

        [Header("粒子特效映射")]
        public ParticleEffectPair[] particlePrefabs;

        public class PrefabsSharedBaker : Baker<EntityPrefabs>
        {
            public override void Bake(EntityPrefabs auth)
            {

                var e = GetEntity(auth.gameObject, TransformUsageFlags.Dynamic);

                // 本地方法：安全地从 Pair 数组里根据枚举索引取 prefab 并转 Entity
                Entity GetSafe<TPair, TEnum>(TPair[] pairs, TEnum id)
                    where TEnum : Enum
                    where TPair : struct
                {
                    if (pairs == null) return Entity.Null;
                    int idx = Convert.ToInt32(id);
                    if (idx < 0 || idx >= pairs.Length) return Entity.Null;
                    // 反射读取字段 prefab
                    var go = (GameObject)typeof(TPair)
                        .GetField("prefab")
                        .GetValue(pairs[idx]);
                    return go != null ? GetEntity(go, TransformUsageFlags.Dynamic) : Entity.Null;
                }

                AddComponent(e, new ScenePrefabsSingleton
                {
                    Hero = auth.heroObj != null
                        ? GetEntity(auth.heroObj, TransformUsageFlags.Dynamic)
                        : Entity.Null,
                    HeroDetector = auth.heroDetector != null
                        ? GetEntity(auth.heroDetector, TransformUsageFlags.Dynamic)
                        : Entity.Null,
                    //英雄技能
                    HeroSkill_Pulse = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.Pulse),
                    HeroSkill_DarkEnergy = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.DarkEnergy),
                    HeroSkill_IceFire = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.IceFire),
                    HeroSkill_ThunderStrike = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.ThunderStrike),
                    HeroSkill_ArcaneCircle = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.ArcaneCircle),
                    HeroSkill_Advance = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.Advance),
                    HeroSkill_Frost = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.Frost),
                    HeroSkill_BlackFlame = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.BlackFlame),
                    HeroSkill_Sweep = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.Sweep),
                    HeroSkill_PoisonPool = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.PoisonPool),
                    HeroSkill_Phase = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.Phase),
                    HeroSkill_ElementResonance = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.ElementResonance),
                    HeroSkill_ShadowStep = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.ShadowStep),
                    HeroSkill_ElectroCage = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.ElectroCage),
                    HeroSkill_MineBlast = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.MineBlast),
                    HeroSkill_ShadowTide = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.ShadowTide),
                    HeroSkill_TimeSlow = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.TimeSlow),
                    HeroSkill_FlameCharge = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.FlameCharge),
                    HeroSkill_FrostShield = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.FrostShield),
                    HeroSkill_ChainDevour = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.ChainDevour),
                    HeroSkill_ThunderGrip = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.ThunderGrip),
                    HeroSkill_ScorchMark = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.ScorchMark),
                    HeroSkill_FrostNova = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.FrostNova),
                    HeroSkill_ShadowEmbrace = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.ShadowEmbrace),
                    HeroSkill_PlagueSpread = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.PlagueSpread),
                    HeroSkill_ElementShield = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.ElementShield),
                    HeroSkill_ArcanePulse = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.ArcanePulse),
                    HeroSkill_ChronoTwist = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.ChronoTwist),
                    HeroSkill_FlameBurst = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.FlameBurst),
                    HeroSkill_FrostTrail = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.FrostTrail),
                    HeroSkill_LightningChain = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.LightningChain),
                    HeroSkill_ShadowStab = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.ShadowStab),
                    HeroSkill_PoisonRain = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.PoisonRain),
                    HeroSkill_ElementBurst = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.ElementBurst),
                    HeroSkill_PhantomStep = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.PhantomStep),
                    HeroSkill_Shadowless = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.Shadowless),
                    HeroSkill_Fusion = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.Fusion),
                    HeroSkill_Mastery = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.Mastery),
                    HeroSkill_ElementAnnihilation = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.ElementAnnihilation),
                    HeroSkill_VoidDescend = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.VoidDescend),
                    HeroSkill_CelestialJudgment = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.CelestialJudgment),
                    HeroSkill_IceAge = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.IceAge),
                    HeroSkill_PurgatoryBlaze = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.PurgatoryBlaze),
                    HeroSkill_PlagueStorm = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.PlagueStorm),
                    HeroSkill_TimeRift = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.TimeRift),
                    HeroSkill_ShadowMirror = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.ShadowMirror),
                    HeroSkill_Starfall = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.Starfall),
                    HeroSkill_Doomsong = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.Doomsong),
                    HeroSkill_DoomsdayJudgment = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.DoomsdayJudgment),
                    HeroSkill_AbsoluteZero = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.AbsoluteZero),
                    HeroSkill_ThunderAnnihilation = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.ThunderAnnihilation),
                    HeroSkill_ShadowOnslaught = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.ShadowOnslaught),
                    HeroSkill_PlagueTide = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.PlagueTide),
                    HeroSkill_ElementStorm = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.ElementStorm),
                    HeroSkill_ReaperScythe = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.ReaperScythe),
                    HeroSkill_SeraphWrath = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.SeraphWrath),
                    HeroSkill_FrozenThrone = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.FrozenThrone),
                    HeroSkill_LightningAvatar = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.LightningAvatar),
                    HeroSkill_ShadowLegion = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.ShadowLegion),
                    HeroSkill_CatastrophicBlast = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.CatastrophicBlast),
                    HeroSkill_ElementFusion = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.ElementFusion),
                    HeroSkill_TwilightMoment = GetSafe<HeroSkillPrefabPair, HeroSkillID>(auth.skillPrefabs, HeroSkillID.TwilightMoment),

                    //英雄辅助技能
                    HeroSkillAssistive_IceFireFire =GetSafe(auth.skillAssistivePrefabs,HeroSkillAssistiveID.IceFireFire),
                    HeroSkillAssistive_Frost = GetSafe(auth.skillAssistivePrefabs, HeroSkillAssistiveID.Frost_Fragment),
                    HeroSkillAssistive_ElectroCage_Lightning =GetSafe(auth.skillAssistivePrefabs,HeroSkillAssistiveID.ElectroCage_Lightning),
                    HeroSkillAssistive_PoisonRainB =GetSafe(auth.skillAssistivePrefabs,HeroSkillAssistiveID.PoisonRainB),



                    // 怪物技能
                    MonsterSkill_DeathPulse = GetSafe<MonsterSkillPrefabPair, MonsterSkillID>(auth.monsterSkillPrefabs, MonsterSkillID.DeathPulse),

                    // 怪物
                    Monster_Zombie = GetSafe<MonsterPrefabPair, MonsterName>(auth.monsterPrefabs, MonsterName.Zombie),
                    Monster_Albono = GetSafe<MonsterPrefabPair, MonsterName>(auth.monsterPrefabs, MonsterName.Albono),
                    Monster_AlbonoUpper = GetSafe<MonsterPrefabPair, MonsterName>(auth.monsterPrefabs, MonsterName.AlbonoUpper),
                    Monster_Skeleton = GetSafe<MonsterPrefabPair, MonsterName>(auth.monsterPrefabs, MonsterName.Skeleton),
                    Monster_Vampire = GetSafe<MonsterPrefabPair, MonsterName>(auth.monsterPrefabs, MonsterName.Vampire),
                    Monster_Werewolf = GetSafe<MonsterPrefabPair, MonsterName>(auth.monsterPrefabs, MonsterName.Werewolf),
                    Monster_Ghoul = GetSafe<MonsterPrefabPair, MonsterName>(auth.monsterPrefabs, MonsterName.Ghoul),

                    // 英雄基础飞行道具
                    HeroFlightProp_DefaultProp = GetSafe<HeroFlightPropPair, HeroFlightPropID>(auth.flightPropPrefabs, HeroFlightPropID.DefaultProp),
                    //怪物飞行道具
                    MonsterFlightProp_FrostLightningBall = GetSafe<MonsterFlightPropPair, MonsterFlightPropID>(auth.enemyFlightPropPrefabs, MonsterFlightPropID.FrostLightningBall),

                    // shader 特效
                    Shader_BurningEffect = GetSafe<ShaderEffectsPair, ShaderEffectsID>(auth.shaderEffectsPrefabs, ShaderEffectsID.BurningEffect),
                    //VFX 特效
                    VFX_DefaultEffect = GetSafe<VFXEffectsPair,VFXEffectsID>(auth.vfxEffectsPrefabs,VFXEffectsID.DefaultEffexts),


                    //粒子特效
                    ParticleEffect_DefaultEffexts = GetSafe<ParticleEffectPair, ParticleEffectsID>(auth.particlePrefabs, ParticleEffectsID.DefaultEffexts),
                });


            }
        }

    }
    /// <summary>
    /// 英雄技能 → 预制体 对应结构,因为要暴露字段在外部设置，所以需要增加字段
    /// </summary>
    [Serializable]
    public struct HeroSkillPrefabPair : IPrefabPair<HeroSkillID>
    {
        // Inspector 可见的字段
        public HeroSkillID id;
        public GameObject prefab;

        // Baker 通过属性访问
        public HeroSkillID ID => id;
        public GameObject Prefab => prefab;
    }

    /// <summary>
    /// 英雄技能 → 预制体 这里针对某些技能的辅助映射如冰火的火球
    /// </summary>
    [Serializable]
    public struct HeroSkillAssistivePrefabPair : IPrefabPair<HeroSkillAssistiveID>
    {
        // Inspector 可见的字段
        public HeroSkillAssistiveID id;
        public GameObject prefab;

        // Baker 通过属性访问
        public HeroSkillAssistiveID ID => id;
        public GameObject Prefab => prefab;
    }
    /// <summary>
    /// 怪物技能 → 预制体 对应结构
    /// </summary>
    [Serializable]
    public struct MonsterSkillPrefabPair : IPrefabPair<MonsterSkillID>
    {
        public MonsterSkillID id;
        public GameObject prefab;

        public MonsterSkillID ID => id;
        public GameObject Prefab => prefab;
    }

    /// <summary>
    /// 怪物实体 → 预制体 对应结构
    /// </summary>
    [Serializable]
    public struct MonsterPrefabPair : IPrefabPair<MonsterName>
    {
        public MonsterName id;
        public GameObject prefab;

        public MonsterName ID => id;
        public GameObject Prefab => prefab;
    }

    /// <summary>
    /// 英雄飞行道具 → 预制体 对应结构
    /// </summary>
    [Serializable]
    public struct HeroFlightPropPair : IPrefabPair<HeroFlightPropID>
    {
        public HeroFlightPropID id;
        public GameObject prefab;

        public HeroFlightPropID ID => id;
        public GameObject Prefab => prefab;
    }

    /// <summary>
    /// 敌人飞行道具 → 预制体 对应结构
    /// </summary>
    [Serializable]
    public struct MonsterFlightPropPair : IPrefabPair<MonsterFlightPropID>
    {
        public MonsterFlightPropID id;
        public GameObject prefab;

        public MonsterFlightPropID ID => id;
        public GameObject Prefab => prefab;
    }

    /// <summary>
    /// Shader 特效 → 预制体 对应结构
    /// </summary>
    [Serializable]
    public struct ShaderEffectsPair : IPrefabPair<ShaderEffectsID>
    {
        public ShaderEffectsID id;
        public GameObject prefab;

        public ShaderEffectsID ID => id;
        public GameObject Prefab => prefab;
    }


    /// <summary>
    /// VFX 特效 → 预制体 对应结构
    /// </summary>
    [Serializable]
    public struct VFXEffectsPair : IPrefabPair<VFXEffectsID>
    {
        public VFXEffectsID id;
        public GameObject prefab;

        public VFXEffectsID ID => id;
        public GameObject Prefab => prefab;
    }

    /// <summary>
    /// 粒子特效 → 预制体 对应结构
    /// </summary>
    [Serializable]
    public struct ParticleEffectPair : IPrefabPair<ParticleEffectsID>
    {
        public ParticleEffectsID id;
        public GameObject prefab;

        public ParticleEffectsID ID => id;
        public GameObject Prefab => prefab;
    }

    /// <summary>硬编码的单例组件，包含所有 Entity 预制体引用</summary>
    public struct ScenePrefabsSingleton : IComponentData
        {
            public Entity Hero;
            public Entity HeroDetector;    
            // Hero Skills
            public Entity HeroSkill_Pulse;
            public Entity HeroSkill_DarkEnergy;
            public Entity HeroSkill_IceFire;
            public Entity HeroSkill_ThunderStrike;
            public Entity HeroSkill_ArcaneCircle;
            public Entity HeroSkill_Advance;
            public Entity HeroSkill_Frost;
            public Entity HeroSkill_BlackFlame;
            public Entity HeroSkill_Sweep;
            public Entity HeroSkill_PoisonPool;
            public Entity HeroSkill_Phase;
            public Entity HeroSkill_ElementResonance;
            public Entity HeroSkill_ShadowStep;
            public Entity HeroSkill_ElectroCage;
            public Entity HeroSkill_MineBlast;
            public Entity HeroSkill_ShadowTide;
            public Entity HeroSkill_TimeSlow;
            public Entity HeroSkill_FlameCharge;
            public Entity HeroSkill_FrostShield;
            public Entity HeroSkill_ChainDevour;
            public Entity HeroSkill_ThunderGrip;
            public Entity HeroSkill_ScorchMark;
            public Entity HeroSkill_FrostNova;
            public Entity HeroSkill_ShadowEmbrace;
            public Entity HeroSkill_PlagueSpread;
            public Entity HeroSkill_ElementShield;
            public Entity HeroSkill_ArcanePulse;
            public Entity HeroSkill_ChronoTwist;
            public Entity HeroSkill_FlameBurst;
            public Entity HeroSkill_FrostTrail;
            public Entity HeroSkill_LightningChain;
            public Entity HeroSkill_ShadowStab;
            public Entity HeroSkill_PoisonRain;
            public Entity HeroSkill_ElementBurst;
            public Entity HeroSkill_PhantomStep;
            public Entity HeroSkill_Shadowless;
            public Entity HeroSkill_Fusion;
            public Entity HeroSkill_Mastery;
            public Entity HeroSkill_ElementAnnihilation;
            public Entity HeroSkill_VoidDescend;
            public Entity HeroSkill_CelestialJudgment;
            public Entity HeroSkill_IceAge;
            public Entity HeroSkill_PurgatoryBlaze;
            public Entity HeroSkill_PlagueStorm;
            public Entity HeroSkill_TimeRift;
            public Entity HeroSkill_ShadowMirror;
            public Entity HeroSkill_Starfall;
            public Entity HeroSkill_Doomsong;
            public Entity HeroSkill_DoomsdayJudgment;
            public Entity HeroSkill_AbsoluteZero;
            public Entity HeroSkill_ThunderAnnihilation;
            public Entity HeroSkill_ShadowOnslaught;
            public Entity HeroSkill_PlagueTide;
            public Entity HeroSkill_ElementStorm;
            public Entity HeroSkill_ReaperScythe;
            public Entity HeroSkill_SeraphWrath;
            public Entity HeroSkill_FrozenThrone;
            public Entity HeroSkill_LightningAvatar;
            public Entity HeroSkill_ShadowLegion;
            public Entity HeroSkill_CatastrophicBlast;
            public Entity HeroSkill_ElementFusion;
            public Entity HeroSkill_TwilightMoment;
          //英雄辅助skill
            public Entity HeroSkillAssistive_IceFireFire;
            public Entity HeroSkillAssistive_Frost;
            public Entity HeroSkillAssistive_ElectroCage_Lightning;
            public Entity HeroSkillAssistive_PoisonRainB;    
            // Monster Skills
            public Entity MonsterSkill_DeathPulse;
            // Monster Entities
            public Entity Monster_Zombie;
            public Entity Monster_Albono;
            public Entity Monster_AlbonoUpper;
            public Entity Monster_Skeleton;
            public Entity Monster_Vampire;
            public Entity Monster_Werewolf;
            public Entity Monster_Ghoul;
            // Props
            public Entity HeroFlightProp_DefaultProp;
            public Entity MonsterFlightProp_FrostLightningBall;
            // Effects
            public Entity Shader_BurningEffect;
            //VFX Effects
            public Entity VFX_DefaultEffect;

            public Entity ParticleEffect_DefaultEffexts;
        }
    }
