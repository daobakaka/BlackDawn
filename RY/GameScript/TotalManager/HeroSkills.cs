using System.Collections;
using UnityEngine;
using GameFrame.BaseClass;
using BlackDawn.DOTS;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;
using Unity.Collections;
using Unity.Physics;
using TMPro;
using System.Collections.Generic;
using Pathfinding;




namespace BlackDawn

{/// <summary>
/// 主管英雄技能的核心类，在英雄Mono脚本中初始化之后，通过获取单例从构造函数进行初始化
/// </summary>
    public class HeroSkills : Singleton<HeroSkills>
    {
        ScenePrefabsSingleton _skillPrefabs;
        EntityManager _entityManager;
        //生成集合器
        SpawnCollection _spawnCollection;
        GameObject[] _monoPrefabs;
        //技能位置
        Transform _transform;
        //英雄属性,这里的属性，基本只能用于只读，执行过程中，应该采用查询属性
        [ReadOnly] HeroAttributeCmpt _heroAttributeCmptOriginal;
        CoroutineController _coroutineController;

        Entity _heroEntity;
        //技能查询模块,法阵，唯一
        EntityQuery _arcaneCircleQuery;
        //暗影洪流，引导唯一
        EntityQuery _shadowTideQuery;
        //元素护盾 ，唯一 加载到英雄身上，这里可以通过英雄entity 直接拿取
        EntityQuery _ElementShieldQuery;

        //动态英雄结构查询模块
        EntityQuery _heroRealTimeAttr;


        private HeroSkills()
        {


            //获取变换
            _transform = Hero.instance.skillTransforms[0];

            //获取entityManager 管理器
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            //获取预制体,Mono中需要使用entity 查询进行相关的转换
            _skillPrefabs = _entityManager.CreateEntityQuery(typeof(ScenePrefabsSingleton)).GetSingleton<ScenePrefabsSingleton>();

            //英雄属性
            _heroAttributeCmptOriginal = Hero.instance.attributeCmpt;

            //获取全局协程控制器
            _coroutineController = Hero.instance.coroutineController;


            //获取英雄entity
            _heroEntity = Hero.instance.heroEntity;
            //建立查询
            _arcaneCircleQuery = _entityManager.CreateEntityQuery(typeof(SkillArcaneCircleTag));
            //暗影洪流
            _shadowTideQuery = _entityManager.CreateEntityQuery(typeof(SkillShadowTideTag));
            //实时英雄组件查询
            _heroRealTimeAttr = _entityManager.CreateEntityQuery(typeof(HeroAttributeCmpt), typeof(HeroEntityMasterTag));

            //生成集合器
            _spawnCollection = SpawnCollection.GetInstance();
            _monoPrefabs = GameManager.instance.gameObjects;


        }

        /// <summary>
        /// 传入技能ID 这里有7种灵能变化类型，默认都为基础型，辅助或者位移技能1种变化，核心技能3种变化，终极技能6种变化
        ///飞行道具释放伤害类技能 Pulse脉冲， 
        ///
        /// </summary>
        /// <param name="iD"></param>
        /// <param name="psionicType"></param>

        public void RelasesHeroSkill(HeroSkillID iD, HeroSkillPsionicType psionicType = HeroSkillPsionicType.Basic)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var detectionSystemHandle = World.DefaultGameObjectInjectionWorld.Unmanaged.GetExistingUnmanagedSystem<DetectionSystem>();
            ref var detectionSystem = ref World.DefaultGameObjectInjectionWorld.Unmanaged.GetUnsafeSystemRef<DetectionSystem>(detectionSystemHandle);

            var dotDamageSystemHandle = World.DefaultGameObjectInjectionWorld.Unmanaged.GetExistingUnmanagedSystem<DotDamageSystem>();
            ref var dotDamageSystem = ref World.DefaultGameObjectInjectionWorld.Unmanaged.GetUnsafeSystemRef<DotDamageSystem>(dotDamageSystemHandle);

            var runTimeHeroCmp = _entityManager.GetComponentData<HeroAttributeCmpt>(_heroEntity);
            var runtimeStateNoImmunity = _entityManager.GetComponentData<HeroIntgratedNoImmunityState>(_heroEntity);
            switch (iD)

            {
                //脉冲 0，瞬时
                case HeroSkillID.Pulse:
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:
                            var entity = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_Pulse, _transform.position, Hero.instance.transform.rotation, 1, 0, 0, 1, true, false);
                            //添加技能专用标签用于检测等运动等    
                            _entityManager.AddComponentData(entity, new SkillPulseTag() { tagSurvivalTime = 3, speed = 5 });
                            break;
                        case HeroSkillPsionicType.PsionicA:
                            //开启二阶段效果
                            var entity1 = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_Pulse, _transform.position, Hero.instance.transform.rotation, 1, 0, 0, 1, true, false);
                            //添加技能专用标签用于检测等运动等    
                            _entityManager.AddComponentData(entity1, new SkillPulseTag() { tagSurvivalTime = 3, speed = 5, enableSecond = true, scaleChangePar = 1f });
                            _entityManager.AddComponentData(entity1, new SkillPulseSecondExplosionRequestTag { });
                            _entityManager.SetComponentEnabled<SkillPulseSecondExplosionRequestTag>(entity1, false);
                            break;
                        case HeroSkillPsionicType.PsionicB:

                            //生成3个能量体
                            for (int i = 0; i < 3; i++)
                            {
                                var entity2 = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_Pulse, _transform.position, Hero.instance.transform.rotation, 1.4f, float3.zero, new float3(0, -30 + i * 30, 0), 0.5f, true, false);

                                _entityManager.AddComponentData(entity2, new SkillPulseTag() { tagSurvivalTime = 3, speed = 5, scaleChangePar = 0.5f });
                            }
                            break;
                        case HeroSkillPsionicType.PsionicAB:

                            //生成3个能量体,开启二阶状态
                            for (int i = 0; i < 3; i++)
                            {
                                var entity3 = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_Pulse, _transform.position, Hero.instance.transform.rotation, 1.4f, float3.zero, new float3(0, -30 + i * 30, 0), 0.5f, true, false);

                                _entityManager.AddComponentData(entity3, new SkillPulseTag() { tagSurvivalTime = 3, speed = 5, enableSecond = true, scaleChangePar = 0.5f });
                                _entityManager.AddComponentData(entity3, new SkillPulseSecondExplosionRequestTag { });
                                _entityManager.SetComponentEnabled<SkillPulseSecondExplosionRequestTag>(entity3, false);
                            }
                            break;

                    }
                    break;
                //暗能1，瞬时
                case HeroSkillID.DarkEnergy:
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:
                            WeaponEnchantmentSkillDarkEnergy(5);
                            break;
                        //增加两次充能，随着技能等级成长，增加充能次数
                        case HeroSkillPsionicType.PsionicA:
                            WeaponEnchantmentSkillDarkEnergy(7);
                            break;
                        //暗影吞噬的技能，这里应该增加一个新标签
                        case HeroSkillPsionicType.PsionicB:
                            WeaponEnchantmentSkillDarkEnergy(5);
                            break;
                        //暗影吞噬的技能，这里应该增加一个新标签
                        case HeroSkillPsionicType.PsionicAB:
                            Hero.instance.skillAttackPar.enableSpecialEffect = true;
                            WeaponEnchantmentSkillDarkEnergy(7);
                            break;
                    }

                    break;
                //冰火2，瞬时？
                case HeroSkillID.IceFire:
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:
                            var entityIce = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_IceFire, _transform.position, Hero.instance.transform.rotation, 1, new float3(0, 0.3f, 0), 0, 1, false, false);
                            //添加技能专用标签用于检测等运动等    
                            _entityManager.AddComponentData(entityIce, new SkillIceFireTag() { tagSurvivalTime = 20, speed = 3, radius = 5, currentAngle = 1.72f });
                            var entityFire = DamageSkillsFlightProp(_skillPrefabs.HeroSkillAssistive_IceFireFire, _transform.position, Hero.instance.transform.rotation, 1, new float3(0, 0.3f, 0), 0, 1, false, false);
                            //添加技能专用标签用于检测等运动等    
                            _entityManager.AddComponentData(entityFire, new SkillIceFireTag() { tagSurvivalTime = 20, speed = 3, radius = 5, currentAngle = -1.72f });
                            break;
                        //二阶冰火技能增加伤害、半径、体积、速度、持续时间
                        case HeroSkillPsionicType.PsionicA:
                            var entityIce1 = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_IceFire, _transform.position, Hero.instance.transform.rotation, 1.3f, new float3(0, 0.5f, 0), 0, 1.5f, false, false);
                            //添加技能专用标签用于检测等运动等    
                            _entityManager.AddComponentData(entityIce1, new SkillIceFireTag() { tagSurvivalTime = 25, speed = 4, radius = 7, currentAngle = 1.72f, originalScale = 2.6f });
                            var entityFire1 = DamageSkillsFlightProp(_skillPrefabs.HeroSkillAssistive_IceFireFire, _transform.position, Hero.instance.transform.rotation, 1.3f, new float3(0, 0.5f, 0), 0, 1.3f, false, false);
                            //添加技能专用标签用于检测等运动等    
                            _entityManager.AddComponentData(entityFire1, new SkillIceFireTag() { tagSurvivalTime = 25, speed = 4, radius = 7, currentAngle = -1.72f, originalScale = 2.6f });
                            break;
                        case HeroSkillPsionicType.PsionicB:
                            var entityIce2 = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_IceFire, _transform.position, Hero.instance.transform.rotation, 1, new float3(0, 0.3f, 0), 0, 1, false, false);
                            //添加技能专用标签用于检测等运动等    
                            _entityManager.AddComponentData(entityIce2, new SkillIceFireTag() { tagSurvivalTime = 25, speed = 4, radius = 7, currentAngle = 1.72f, enableSecond = true, scaleChangePar = 1, skillDamageChangeParTag = 1, originalScale = 2f });
                            _entityManager.AddComponentData(entityIce2, new SkillIceFireSecondExplosionRequestTag { });
                            _entityManager.SetComponentEnabled<SkillIceFireSecondExplosionRequestTag>(entityIce2, false);
                            var entityFire2 = DamageSkillsFlightProp(_skillPrefabs.HeroSkillAssistive_IceFireFire, _transform.position, Hero.instance.transform.rotation, 1, new float3(0, 0.3f, 0), 0, 1, false, false);
                            //添加技能专用标签用于检测等运动等    
                            _entityManager.AddComponentData(entityFire2, new SkillIceFireTag() { tagSurvivalTime = 25, speed = 4, radius = 7, currentAngle = -1.72f, enableSecond = true, scaleChangePar = 1, skillDamageChangeParTag = 1, originalScale = 2f });
                            _entityManager.AddComponentData(entityFire2, new SkillIceFireSecondExplosionRequestTag { });
                            _entityManager.SetComponentEnabled<SkillIceFireSecondExplosionRequestTag>(entityFire2, false);
                            break;
                        case HeroSkillPsionicType.PsionicAB:

                            var entityIce3 = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_IceFire, _transform.position, Hero.instance.transform.rotation, 0.3f, new float3(0, 1.3f, 0), 0, 1.5f, false, false);
                            //添加技能专用标签用于检测等运动等    
                            _entityManager.AddComponentData(entityIce3, new SkillIceFireTag() { tagSurvivalTime = 30, speed = 6f, radius = 9, currentAngle = -1.72f, enableSecond = true, scaleChangePar = 1, skillDamageChangeParTag = 1, originalScale = 2.6f });
                            _entityManager.AddComponentData(entityIce3, new SkillIceFireSecondExplosionRequestTag { });
                            _entityManager.SetComponentEnabled<SkillIceFireSecondExplosionRequestTag>(entityIce3, false);
                            var entityFire3 = DamageSkillsFlightProp(_skillPrefabs.HeroSkillAssistive_IceFireFire, _transform.position, Hero.instance.transform.rotation, 0.3f, new float3(0, 1.3f, 0), 0, 1.5f, false, false);
                            //添加技能专用标签用于检测等运动等    
                            _entityManager.AddComponentData(entityFire3, new SkillIceFireTag() { tagSurvivalTime = 30, speed = 6f, radius = 9, currentAngle = 1.72f, enableSecond = true, scaleChangePar = 1, skillDamageChangeParTag = 1, originalScale = 2.6f });
                            _entityManager.AddComponentData(entityFire3, new SkillIceFireSecondExplosionRequestTag { });
                            _entityManager.SetComponentEnabled<SkillIceFireSecondExplosionRequestTag>(entityFire3, false);
                            break;
                    }
                    break;
                //落雷3，瞬时
                case HeroSkillID.ThunderStrike:

                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:

                            //释放初级落雷技能,测试跟随性落雷
                            DamageSkillsFlightPropConsecutiveCasting<SkillThunderStrikeTag>(_skillPrefabs.HeroSkill_ThunderStrike, new SkillThunderStrikeTag() { tagSurvivalTime = 0.5f }, 12, 1, _transform.position,
                                Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false, false);

                            break;
                        case HeroSkillPsionicType.PsionicA:
                            //释放初级落雷技能,这里的行为可以配置,跟随英雄数量+1
                            DamageSkillsFlightPropConsecutiveCasting<SkillThunderStrikeTag>(_skillPrefabs.HeroSkill_ThunderStrike, new SkillThunderStrikeTag() { tagSurvivalTime = 0.5f }, 16, 1, _transform.position,
                                 Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false, true);

                            break;


                        case HeroSkillPsionicType.PsionicB:
                            //释放初级落雷技能,这里的行为可以配置
                            DamageSkillsFlightPropConsecutiveCasting<SkillThunderStrikeTag>(_skillPrefabs.HeroSkill_ThunderStrike, new SkillThunderStrikeTag() { tagSurvivalTime = 1f }, 12, 0.5f, _transform.position,
                                 Hero.instance.transform.rotation, 1.3f, float3.zero, float3.zero, 1, false, false, false);
                            break;

                        case HeroSkillPsionicType.PsionicAB:

                            //释放初级落雷技能,这里的行为可以配置
                            DamageSkillsFlightPropConsecutiveCasting<SkillThunderStrikeTag>(_skillPrefabs.HeroSkill_ThunderStrike, new SkillThunderStrikeTag() { tagSurvivalTime = 1f }, 16, 0.5f, _transform.position,
                                 Hero.instance.transform.rotation, 1.3f, float3.zero, float3.zero, 1, false, false, true);


                            break;

                    }


                    break;
                //法阵 4，持续
                case HeroSkillID.ArcaneCircle:

                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:

                            //基础能量大于20 放置法阵，可否自由关闭？
                            bool hasArcaneCircle = _arcaneCircleQuery.CalculateEntityCount() > 0;

                            //这里获取世界中的事实entity
                            var realAttr = _entityManager.GetComponentData<HeroAttributeCmpt>(_heroEntity);
                            if (!hasArcaneCircle)
                            {
                                if (realAttr.defenseAttribute.energy >= 20)
                                {
                                    //开启则降低10点能量持续3秒
                                    realAttr.defenseAttribute.energy -= 20;
                                    _entityManager.SetComponentData(_heroEntity, realAttr);
                                    var entityArcaneCircle = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ArcaneCircle, _transform.position, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                                    _entityManager.AddComponentData(entityArcaneCircle, new SkillArcaneCircleTag() { tagSurvivalTime = 3 });
                                }
                            }
                            else
                            {
                                //再次点击手动关闭
                                var arcaneCircleEntity = _arcaneCircleQuery.GetSingletonRW<SkillArcaneCircleTag>();

                                arcaneCircleEntity.ValueRW.closed = true;

                            }
                            break;
                        case HeroSkillPsionicType.PsionicA:


                            //基础能量大于20 放置法阵，可否自由关闭？
                            bool hasArcaneCircleA = _arcaneCircleQuery.CalculateEntityCount() > 0;

                            //这里获取世界中的事实entity
                            var realAttrA = _entityManager.GetComponentData<HeroAttributeCmpt>(_heroEntity);
                            if (!hasArcaneCircleA)
                            {
                                if (realAttrA.defenseAttribute.energy >= 20)
                                {
                                    //开启则降低10点能量持续3秒
                                    realAttrA.defenseAttribute.energy -= 20;
                                    _entityManager.SetComponentData(_heroEntity, realAttrA);
                                    var entityArcaneCircle = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ArcaneCircle, _transform.position, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                                    _entityManager.AddComponentData(entityArcaneCircle, new SkillArcaneCircleTag() { tagSurvivalTime = 3, enableSecondA = true });
                                    _entityManager.AddComponentData(entityArcaneCircle, new SkillArcaneCircleSecondTag());//添加标识，用于收集碰撞对
                                    _entityManager.AddBuffer<SkillArcaneCircleSecondBufferTag>(entityArcaneCircle);//添加技能专属buffer标签，用于构建虹吸效果的基础数据
                                                                                                                   //添加链接效果渲染标签
                                                                                                                   //  ecb.AddComponent(_entityManager.GetBuffer<LinkedEntityGroup>(_heroEntity)[1].Value, new HeroEffectsLinked());
                                }
                            }
                            else
                            {
                                //再次点击手动关闭
                                var arcaneCircleEntity = _arcaneCircleQuery.GetSingletonRW<SkillArcaneCircleTag>();
                                arcaneCircleEntity.ValueRW.closed = true;
                                //关闭法阵时，需要设置链接为0
                                // ecb.SetComponentEnabled<HeroEffectsLinked>(_entityManager.GetBuffer<LinkedEntityGroup>(_heroEntity)[1].Value, false);
                            }

                            break;


                        case HeroSkillPsionicType.PsionicB:

                            //基础能量大于20 放置法阵，可否自由关闭？
                            bool hasArcaneCircleB = _arcaneCircleQuery.CalculateEntityCount() > 0;

                            //这里获取世界中的事实entity
                            var realAttrB = _entityManager.GetComponentData<HeroAttributeCmpt>(_heroEntity);
                            if (!hasArcaneCircleB)
                            {
                                if (realAttrB.defenseAttribute.energy >= 20)
                                {
                                    //开启则降低10点能量持续3秒
                                    realAttrB.defenseAttribute.energy -= 20;
                                    _entityManager.SetComponentData(_heroEntity, realAttrB);
                                    //体积加大50%
                                    var entityArcaneCircle = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ArcaneCircle, _transform.position, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1.5f, false, false);
                                    _entityManager.AddComponentData(entityArcaneCircle, new SkillArcaneCircleTag() { tagSurvivalTime = 3, enableSecondB = true });
                                }
                            }
                            else
                            {
                                //再次点击手动关闭
                                var arcaneCircleEntity = _arcaneCircleQuery.GetSingletonRW<SkillArcaneCircleTag>();
                                arcaneCircleEntity.ValueRW.closed = true;

                            }

                            break;

                        case HeroSkillPsionicType.PsionicAB:

                            //基础能量大于20 放置法阵，可否自由关闭？
                            bool hasArcaneCircleAB = _arcaneCircleQuery.CalculateEntityCount() > 0;
                            //这里获取世界中的事实entity
                            var realAttrAB = _entityManager.GetComponentData<HeroAttributeCmpt>(_heroEntity);
                            if (!hasArcaneCircleAB)
                            {
                                if (realAttrAB.defenseAttribute.energy >= 20)
                                {
                                    //开启则降低10点能量持续3秒
                                    realAttrAB.defenseAttribute.energy -= 20;
                                    _entityManager.SetComponentData(_heroEntity, realAttrAB);
                                    var entityArcaneCircle = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ArcaneCircle, _transform.position, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1.5f, false, false);
                                    _entityManager.AddComponentData(entityArcaneCircle, new SkillArcaneCircleTag() { tagSurvivalTime = 3, enableSecondA = true, enableSecondB = true });
                                    _entityManager.AddComponentData(entityArcaneCircle, new SkillArcaneCircleSecondTag());//添加标识，用于收集碰撞对
                                    _entityManager.AddBuffer<SkillArcaneCircleSecondBufferTag>(entityArcaneCircle);//添加技能专属buffer标签，用于构建虹吸效果的基础数据

                                }
                            }
                            else
                            {
                                //再次点击手动关闭
                                var arcaneCircleEntity = _arcaneCircleQuery.GetSingletonRW<SkillArcaneCircleTag>();
                                arcaneCircleEntity.ValueRW.closed = true;
                                //关闭法阵时，需要设置链接为0
                                //   ecb.SetComponentEnabled<HeroEffectsLinked>(_entityManager.GetBuffer<LinkedEntityGroup>(_heroEntity)[1].Value, false);
                            }


                            break;

                    }


                    break;
                //进击5 保护 辅助
                case HeroSkillID.Advance:
                    //开启进击标签
                    _entityManager.SetComponentEnabled<SkillAdvanceTag_Hero>(_heroEntity, true);
                    switch (psionicType)
                    {

                        case HeroSkillPsionicType.Basic:

                            var skillAdvance = _entityManager.GetComponentData<SkillAdvanceTag_Hero>(_heroEntity);

                            if (skillAdvance.active)
                            { skillAdvance.active = false; }

                            else
                            {
                                if (runTimeHeroCmp.defenseAttribute.energy > 20)
                                {
                                    runTimeHeroCmp.defenseAttribute.energy -= 20;

                                    //设置初始时间激活
                                    skillAdvance.tagSurvivalTime = 3f;
                                    skillAdvance.active = true;
                                }
                            }
                            _entityManager.SetComponentData(_heroEntity, skillAdvance);
                            _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                            break;

                        case HeroSkillPsionicType.PsionicA:

                            var skillAdvanceA = _entityManager.GetComponentData<SkillAdvanceTag_Hero>(_heroEntity);

                            if (skillAdvanceA.active)
                            { skillAdvanceA.active = false; }
                            else
                            {
                                if (runTimeHeroCmp.defenseAttribute.energy > 20)
                                {
                                    runTimeHeroCmp.defenseAttribute.energy -= 20;

                                    //设置初始时间激活
                                    skillAdvanceA.tagSurvivalTime = 3f;
                                    skillAdvanceA.active = true;
                                    skillAdvanceA.enableSecondA = true;
                                }
                            }
                            _entityManager.SetComponentData(_heroEntity, skillAdvanceA);
                            _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                            break;


                    }

                    break;
                //寒冰 6，瞬时    
                case HeroSkillID.Frost:
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:
                            WeaponEnchantmentSkillFrost();
                            break;
                        //增加分裂功能
                        case HeroSkillPsionicType.PsionicA:
                            WeaponEnchantmentSkillFrost(true, 5, 5);
                            break;
                        //增加碎片次数,不能分裂，但是可以冻结
                        case HeroSkillPsionicType.PsionicB:
                            WeaponEnchantmentSkillFrost(false, 5, 2);
                            Hero.instance.skillAttackPar.tempFreeze = 101;
                            Hero.instance.skillAttackPar.enableSpecialEffect = true;
                            break;
                        //分裂和碎片都增加
                        case HeroSkillPsionicType.PsionicAB:
                            WeaponEnchantmentSkillFrost(true, 15, 17, 0.1f);
                            Hero.instance.skillAttackPar.tempFreeze = 101;
                            Hero.instance.skillAttackPar.enableSpecialEffect = true;
                            break;
                    }

                    break;
                //黑炎 7，瞬时/辅助
                case HeroSkillID.BlackFlame:
                    detectionSystem.enableSpecialSkillBlcakFrame = true;
                    switch (psionicType)
                    {
                        //黑炎设计为特殊技能，打一个特殊技能标签，只要碰撞到了之后， 怪物就永久 减益，并且渲染出黑炎
                        case HeroSkillPsionicType.Basic:
                            //skillTag 这里在技能配置里进行读取，开启侦测系统进行侦测

                            var entityBlackFrame = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_BlackFlame, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            var skillPar = _entityManager.GetComponentData<SkillsDamageCalPar>(entityBlackFrame);
                            _entityManager.SetComponentData(entityBlackFrame, skillPar);
                            _entityManager.AddComponentData(entityBlackFrame, new SkillBlackFrameTag() { tagSurvivalTime = 10 });

                            break;
                        case HeroSkillPsionicType.PsionicA:

                            var entityBlackFrameA = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_BlackFlame, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            var skillParA = _entityManager.GetComponentData<SkillsDamageCalPar>(entityBlackFrameA);

                            _entityManager.SetComponentData(entityBlackFrameA, skillParA);
                            _entityManager.AddComponentData(entityBlackFrameA, new SkillBlackFrameTag() { tagSurvivalTime = 10, enableSecondA = true });

                            break;
                        case HeroSkillPsionicType.PsionicB:

                            var entityBlackFrameB = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_BlackFlame, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            var skillParB = _entityManager.GetComponentData<SkillsDamageCalPar>(entityBlackFrameB);
                            _entityManager.SetComponentData(entityBlackFrameB, skillParB);
                            _entityManager.AddComponentData(entityBlackFrameB, new SkillBlackFrameTag() { tagSurvivalTime = 10, enableSecondB = true });

                            break;

                        case HeroSkillPsionicType.PsionicC:
                            var entityBlackFrameC = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_BlackFlame, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            var skillParC = _entityManager.GetComponentData<SkillsDamageCalPar>(entityBlackFrameC);
                            skillParC.fireDotDamage = 10 * skillParC.instantPhysicalDamage;//物理伤害可以压制
                            _entityManager.SetComponentData(entityBlackFrameC, skillParC);
                            _entityManager.AddComponentData(entityBlackFrameC, new SkillBlackFrameTag() { tagSurvivalTime = 10, enableSecondC = true });

                            break;
                        case HeroSkillPsionicType.PsionicAB:

                            var entityBlackFrameAB = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_BlackFlame, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            var skillParAB = _entityManager.GetComponentData<SkillsDamageCalPar>(entityBlackFrameAB);
                            _entityManager.SetComponentData(entityBlackFrameAB, skillParAB);
                            _entityManager.AddComponentData(entityBlackFrameAB, new SkillBlackFrameTag() { tagSurvivalTime = 10, enableSecondB = true, enableSecondA = true });

                            break;
                        case HeroSkillPsionicType.PsionicAC:
                            var entityBlackFrameAC = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_BlackFlame, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            var skillParAC = _entityManager.GetComponentData<SkillsDamageCalPar>(entityBlackFrameAC);
                            skillParAC.fireDotDamage = 10 * skillParAC.instantPhysicalDamage;//物理伤害可以压制
                            _entityManager.SetComponentData(entityBlackFrameAC, skillParAC);
                            _entityManager.AddComponentData(entityBlackFrameAC, new SkillBlackFrameTag() { tagSurvivalTime = 10, enableSecondC = true, enableSecondA = true });
                            break;
                        case HeroSkillPsionicType.PsionicBC:
                            var entityBlackFrameBC = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_BlackFlame, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            var skillParBC = _entityManager.GetComponentData<SkillsDamageCalPar>(entityBlackFrameBC);
                            skillParBC.fireDotDamage = 10 * skillParBC.instantPhysicalDamage;//物理伤害可以压制
                            _entityManager.SetComponentData(entityBlackFrameBC, skillParBC);
                            _entityManager.AddComponentData(entityBlackFrameBC, new SkillBlackFrameTag() { tagSurvivalTime = 10, enableSecondC = true, enableSecondB = true });

                            break;
                        case HeroSkillPsionicType.PsionicABC:
                            var entityBlackFrameABC = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_BlackFlame, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            var skillParABC = _entityManager.GetComponentData<SkillsDamageCalPar>(entityBlackFrameABC);
                            skillParABC.fireDotDamage = 10 * skillParABC.instantPhysicalDamage;//物理伤害可以压制
                            _entityManager.SetComponentData(entityBlackFrameABC, skillParABC);
                            _entityManager.AddComponentData(entityBlackFrameABC, new SkillBlackFrameTag() { tagSurvivalTime = 10, enableSecondB = true, enableSecondA = true, enableSecondC = true });


                            break;





                    }
                    break;
                //横扫 瞬时 8
                case HeroSkillID.Sweep:
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:
                            var entitySweepRender = DamageSkillsFlightPropNoneDamage(_skillPrefabs.HeroSkill_Sweep, Hero.instance.transform.position, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            _entityManager.AddComponentData(entitySweepRender, new SkillSweepRenderTag { tagSurvivalTime = 1 });

                            var entitySweep = DamageSkillsFlightProp(_skillPrefabs.HeroSkillAssistive_SweepCollider, Hero.instance.transform.position, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            var skillPar = _entityManager.GetComponentData<SkillsDamageCalPar>(entitySweep);
                            _entityManager.SetComponentData(entitySweep, skillPar);
                            _entityManager.AddComponentData(entitySweep, new SkillSweepTag() { tagSurvivalTime = 1, rotationTotalTime = 1 });
                            break;
                        case HeroSkillPsionicType.PsionicA:
                            var entitySweepRenderA = DamageSkillsFlightPropNoneDamage(_skillPrefabs.HeroSkill_Sweep, Hero.instance.transform.position, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            _entityManager.AddComponentData(entitySweepRenderA, new SkillSweepRenderTag { tagSurvivalTime = 1 });

                            var entitySweepA = DamageSkillsFlightProp(_skillPrefabs.HeroSkillAssistive_SweepCollider, Hero.instance.transform.position, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            var skillParA = _entityManager.GetComponentData<SkillsDamageCalPar>(entitySweepA);
                            _entityManager.SetComponentData(entitySweepA, skillParA);
                            _entityManager.AddComponentData(entitySweepA, new SkillSweepTag() { tagSurvivalTime = 1, enableSecondA = true, rotationTotalTime = 1 });
                            break;
                        case HeroSkillPsionicType.PsionicB:
                            var entitySweepRenderB = DamageSkillsFlightPropNoneDamage(_skillPrefabs.HeroSkill_Sweep, Hero.instance.transform.position, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            _entityManager.AddComponentData(entitySweepRenderB, new SkillSweepRenderTag { tagSurvivalTime = 1 });


                            var entitySweepB = DamageSkillsFlightProp(_skillPrefabs.HeroSkillAssistive_SweepCollider, Hero.instance.transform.position, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            var skillParB = _entityManager.GetComponentData<SkillsDamageCalPar>(entitySweepB);
                            _entityManager.SetComponentData(entitySweepB, skillParB);
                            _entityManager.AddComponentData(entitySweepB, new SkillSweepTag() { tagSurvivalTime = 1, enableSecondB = true, rotationTotalTime = 1, speed = 10, spawnTimer = UnityEngine.Random.Range(0, 0.15f), interval = 0.3f, skillDamageChangeParTag = 0.5f });
                            break;
                        case HeroSkillPsionicType.PsionicAB:
                            var entitySweepRenderAB = DamageSkillsFlightPropNoneDamage(_skillPrefabs.HeroSkill_Sweep, Hero.instance.transform.position, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            _entityManager.AddComponentData(entitySweepRenderAB, new SkillSweepRenderTag { tagSurvivalTime = 1 });

                            var entitySweepAB = DamageSkillsFlightProp(_skillPrefabs.HeroSkillAssistive_SweepCollider, Hero.instance.transform.position, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            var skillParAB = _entityManager.GetComponentData<SkillsDamageCalPar>(entitySweepAB);
                            _entityManager.SetComponentData(entitySweepAB, skillParAB);
                            _entityManager.AddComponentData(entitySweepAB, new SkillSweepTag() { tagSurvivalTime = 1, enableSecondB = true, enableSecondA = true, rotationTotalTime = 1, speed = 10, spawnTimer = UnityEngine.Random.Range(0, 0.15f), interval = 0.15f, skillDamageChangeParTag = 0.5f });
                            break;
                    }
                    break;
                //毒池 9 ，瞬时/辅助
                case HeroSkillID.PoisonPool:
                    switch (psionicType)
                    {

                        case HeroSkillPsionicType.Basic:
                            //skillTag 这里在技能配置里进行读取
                            var entityPoisonPool = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_PoisonPool, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            var skillPar = _entityManager.GetComponentData<SkillsDamageCalPar>(entityPoisonPool);
                            _entityManager.SetComponentData(entityPoisonPool, skillPar);
                            _entityManager.AddComponentData(entityPoisonPool, new SkillPoisonPoolTag() { tagSurvivalTime = 10 });

                            break;
                        case HeroSkillPsionicType.PsionicA:
                            break;





                    }
                    break;
                //相位 10 ，保护/辅助
                case HeroSkillID.Phase:
                    switch (psionicType)
                    {
                        //激活直接伤害物理免疫和元素伤害免疫的 两种状态,免疫时间，升级外部读取 临时储存
                        case HeroSkillPsionicType.Basic:
                            var heroNoImmunityState = _entityManager.GetComponentData<HeroIntgratedNoImmunityState>(_heroEntity);

                            heroNoImmunityState.physicalDamageNoImmunityTimer = 3f;
                            heroNoImmunityState.elementDamageNoImmunityTimer = 3f;
                            _entityManager.SetComponentData(_heroEntity, heroNoImmunityState);
                            if (runTimeHeroCmp.defenseAttribute.energy > 51)
                            {
                                var _rollCoroutineId = _coroutineController.StartRoutine(
                                IEPhaseSkill(3, runTimeHeroCmp),
                                tag: "PhaseSkill",
                                onComplete: () =>
                                {
                                    DevDebug.Log("相位释放完成");
                                }
                                );
                            }
                            break;
                        case HeroSkillPsionicType.PsionicA:
                            break;
                    }

                    break;
                //元素共鸣 11,持续？，非技能标签，不会造成伤害,可以读取等级展示伤害
                case HeroSkillID.ElementResonance:
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:

                            var entityElementResonance = DamageSkillsFlightPropNoneDamage(_skillPrefabs.HeroSkill_ElementResonance, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            _entityManager.AddComponentData(entityElementResonance, new SkillElementResonanceTag() { tagSurvivalTime = 8 });
                            break;
                        case HeroSkillPsionicType.PsionicA:
                            var entityElementResonanceA = DamageSkillsFlightPropNoneDamage(_skillPrefabs.HeroSkill_ElementResonance, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            _entityManager.AddComponentData(entityElementResonanceA, new SkillElementResonanceTag() { tagSurvivalTime = 8, enableSecondA = true, secondDamagePar = 1 });
                            break;
                        case HeroSkillPsionicType.PsionicB:
                            var entityElementResonanceB = DamageSkillsFlightPropNoneDamage(_skillPrefabs.HeroSkill_ElementResonance, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            _entityManager.AddComponentData(entityElementResonanceB, new SkillElementResonanceTag() { tagSurvivalTime = 8, enableSecondB = true, thridDamagePar = 2 });
                            break;

                        case HeroSkillPsionicType.PsionicAB:
                            var entityElementResonanceAB = DamageSkillsFlightPropNoneDamage(_skillPrefabs.HeroSkill_ElementResonance, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            _entityManager.AddComponentData(entityElementResonanceAB, new SkillElementResonanceTag() { tagSurvivalTime = 8, enableSecondA = true, secondDamagePar = 2, enableSecondB = true, thridDamagePar = 2 });
                            break;

                    }
                    break;
                //暗影步 12 瞬时，辅助，保护
                case HeroSkillID.ShadowStep:
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:
                            if (runTimeHeroCmp.defenseAttribute.energy > 30)
                            {
                                runTimeHeroCmp.defenseAttribute.energy -= 30;
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                Hero.instance.transform.position = Hero.instance.skillTargetPositon;
                            }

                            break;
                        case HeroSkillPsionicType.PsionicA:
                            //获得状态圣母降临
                            if (runTimeHeroCmp.defenseAttribute.energy > 30)
                            {
                                runTimeHeroCmp.defenseAttribute.energy -= 30;
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                runtimeStateNoImmunity.controlNoImmunityTimer = 1;
                                runtimeStateNoImmunity.dotNoImmunityTimer = 1;
                                runtimeStateNoImmunity.elementDamageNoImmunityTimer = 1;
                                runtimeStateNoImmunity.inlineDamageNoImmunityTimer = 1;
                                runtimeStateNoImmunity.physicalDamageNoImmunityTimer = 1;
                                _entityManager.SetComponentData(_heroEntity, runtimeStateNoImmunity);
                                Hero.instance.transform.position = Hero.instance.skillTargetPositon;

                            }

                            break;
                        case HeroSkillPsionicType.PsionicB:
                            if (runTimeHeroCmp.defenseAttribute.energy > 30)
                            {
                                runTimeHeroCmp.defenseAttribute.energy -= 30;
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                Hero.instance.transform.position = Hero.instance.skillTargetPositon;
                                var entityShadowStep = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ShadowStep, Hero.instance.transform.position, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                                var skillPar = _entityManager.GetComponentData<SkillsDamageCalPar>(entityShadowStep);
                                skillPar.tempStun = 200;
                                _entityManager.SetComponentData(entityShadowStep, skillPar);
                                _entityManager.AddComponentData(entityShadowStep, new SkillShadowStepTag() { tagSurvivalTime = 0.5f });
                            }


                            break;
                        case HeroSkillPsionicType.PsionicAB:

                            if (runTimeHeroCmp.defenseAttribute.energy > 30)
                            {
                                runTimeHeroCmp.defenseAttribute.energy -= 30;
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                Hero.instance.transform.position = Hero.instance.skillTargetPositon;
                                runtimeStateNoImmunity.controlNoImmunityTimer = 1;
                                runtimeStateNoImmunity.dotNoImmunityTimer = 1;
                                runtimeStateNoImmunity.elementDamageNoImmunityTimer = 1;
                                runtimeStateNoImmunity.inlineDamageNoImmunityTimer = 1;
                                runtimeStateNoImmunity.physicalDamageNoImmunityTimer = 1;
                                _entityManager.SetComponentData(_heroEntity, runtimeStateNoImmunity);
                                var entityShadowStep = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ShadowStep, Hero.instance.transform.position, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                                var skillPar = _entityManager.GetComponentData<SkillsDamageCalPar>(entityShadowStep);
                                skillPar.tempStun = 200;
                                _entityManager.SetComponentData(entityShadowStep, skillPar);
                                _entityManager.AddComponentData(entityShadowStep, new SkillShadowStepTag() { tagSurvivalTime = 0.5f });
                            }

                            break;
                    }
                    break;
                //静电牢笼 13，瞬时，持续
                case HeroSkillID.ElectroCage:

                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:

                            var entityElectroCage = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ElectroCage, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            //构造定身效果
                            var skillPar = _entityManager.GetComponentData<SkillsDamageCalPar>(entityElectroCage);
                            skillPar.tempRoot = 101;
                            skillPar.tempStun = 101;
                            _entityManager.SetComponentData(entityElectroCage, skillPar);
                            _entityManager.AddComponentData(entityElectroCage, new SkillElectroCageTag() { tagSurvivalTime = 4 });
                            break;

                        //开启二阶段，增伤100%
                        case HeroSkillPsionicType.PsionicA:

                            var entityElectroCageA = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ElectroCage, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            //构造定身效果
                            var skillParA = _entityManager.GetComponentData<SkillsDamageCalPar>(entityElectroCageA);
                            skillParA.tempRoot = 101;
                            skillParA.tempStun = 101;
                            _entityManager.SetComponentData(entityElectroCageA, skillParA);
                            _entityManager.AddComponentData(entityElectroCageA, new SkillElectroCageTag() { tagSurvivalTime = 4, enableSecondA = true, skillDamageChangeParTag = 2, intervalTimer = 0.2f });
                            break;
                        //开启第三阶段，静电传导
                        case HeroSkillPsionicType.PsionicB:

                            var entityElectroCageB = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ElectroCage, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            //构造定身效果
                            var skillParB = _entityManager.GetComponentData<SkillsDamageCalPar>(entityElectroCageB);
                            skillParB.tempRoot = 101;
                            skillParB.tempStun = 101;
                            _entityManager.SetComponentData(entityElectroCageB, skillParB);
                            _entityManager.AddComponentData(entityElectroCageB, new SkillElectroCageTag() { tagSurvivalTime = 4, enableSecondB = true });
                            break;

                        //开启二阶段，增伤100%,开启静电传导标识
                        case HeroSkillPsionicType.PsionicAB:

                            var entityElectroCageAB = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ElectroCage, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            //构造定身效果
                            var skillParAB = _entityManager.GetComponentData<SkillsDamageCalPar>(entityElectroCageAB);
                            skillParAB.tempRoot = 101;
                            skillParAB.tempStun = 101;
                            _entityManager.SetComponentData(entityElectroCageAB, skillParAB);
                            _entityManager.AddComponentData(entityElectroCageAB, new SkillElectroCageTag() { tagSurvivalTime = 4, enableSecondA = true, enableSecondB = true, skillDamageChangeParTag = 2, intervalTimer = 0.2f });
                            break;


                    }

                    break;
                //毒爆地雷 14,瞬时，持续，第一个3阶变化技能
                case HeroSkillID.MineBlast:
                    switch (psionicType)
                    {
                        //布置3颗毒爆雷  是否有必要，布置方式，是否可提供旋转操作？暂不提供效果， 原生技能自带2阶爆炸效果,添加爆炸效果标签，用于爆炸持续时间,300%爆炸伤害传递
                        case HeroSkillPsionicType.Basic:

                            for (int i = 0; i < 3; i++)
                            {
                                var entityMineBlast = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_MineBlast, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, new float3(10 * i, 0, 0), float3.zero, 1, false, false);
                                _entityManager.AddComponentData(entityMineBlast, new SkillMineBlastTag() { tagSurvivalTime = 20, scaleChangePar = 2, skillDamageChangeParTag = 3 });
                                _entityManager.AddComponentData(entityMineBlast, new SkillMineBlastExplosionTag() { tagSurvivalTime = 1 });
                            }
                            break;
                        case HeroSkillPsionicType.PsionicA:
                            for (int i = 0; i < 3; i++)
                            {
                                var entityMineBlastA = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_MineBlast, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, new float3(10 * i, 0, 0), float3.zero, 1, false, false);
                                _entityManager.AddComponentData(entityMineBlastA, new SkillMineBlastTag() { tagSurvivalTime = 20, scaleChangePar = 2, skillDamageChangeParTag = 3 });
                                _entityManager.AddComponentData(entityMineBlastA, new SkillMineBlastExplosionTag() { tagSurvivalTime = 1, skillDamageChangeParTag = 1.5f, enableSecondA = true, tagSurvivalTimeSecond = 5, scaleChangePar = 4 });
                            }
                            break;
                        case HeroSkillPsionicType.PsionicB:
                            for (int i = 0; i < 3; i++)
                            {
                                var entityMineBlastB = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_MineBlast, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, new float3(10 * i, 0, 0), float3.zero, 1, false, false);
                                _entityManager.AddComponentData(entityMineBlastB, new SkillMineBlastTag() { tagSurvivalTime = 20, scaleChangePar = 2, skillDamageChangeParTag = 3 });
                                _entityManager.AddComponentData(entityMineBlastB, new SkillMineBlastExplosionTag() { tagSurvivalTime = 1, enableSecondB = true });
                            }
                            break;
                        //十字地雷
                        case HeroSkillPsionicType.PsionicC:
                            for (int i = 0; i < 3; i++)
                            {
                                var entityMineBlastC = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_MineBlast, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, new float3(10 * i, 0, 0), float3.zero, 1, false, false);
                                _entityManager.AddComponentData(entityMineBlastC, new SkillMineBlastTag() { tagSurvivalTime = 20, scaleChangePar = 2, skillDamageChangeParTag = 3 });
                                _entityManager.AddComponentData(entityMineBlastC, new SkillMineBlastExplosionTag() { tagSurvivalTime = 1, enableSecondC = true, level = 1 });
                            }
                            for (int i = 0; i < 2; i++)
                            {
                                var par = 1 - 2 * i;
                                var entityMineBlastC = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_MineBlast, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, new float3(10, 0, 10 * par), float3.zero, 1, false, false);
                                _entityManager.AddComponentData(entityMineBlastC, new SkillMineBlastTag() { tagSurvivalTime = 20, scaleChangePar = 2, skillDamageChangeParTag = 3 });
                                _entityManager.AddComponentData(entityMineBlastC, new SkillMineBlastExplosionTag() { tagSurvivalTime = 1, enableSecondC = true, level = 1 });
                            }
                            break;
                        case HeroSkillPsionicType.PsionicAB:

                            for (int i = 0; i < 3; i++)
                            {
                                var entityMineBlastAB = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_MineBlast, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, new float3(10 * i, 0, 0), float3.zero, 1, false, false);
                                _entityManager.AddComponentData(entityMineBlastAB, new SkillMineBlastTag() { tagSurvivalTime = 20, scaleChangePar = 2, skillDamageChangeParTag = 3 });
                                _entityManager.AddComponentData(entityMineBlastAB, new SkillMineBlastExplosionTag() { tagSurvivalTime = 1, skillDamageChangeParTag = 1.5f, enableSecondA = true, tagSurvivalTimeSecond = 5, scaleChangePar = 4, enableSecondB = true });
                            }

                            break;
                        //十字地雷加后续遗留
                        case HeroSkillPsionicType.PsionicAC:
                            for (int i = 0; i < 3; i++)
                            {
                                var entityMineBlastAC = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_MineBlast, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, new float3(10 * i, 0, 0), float3.zero, 1, false, false);
                                _entityManager.AddComponentData(entityMineBlastAC, new SkillMineBlastTag() { tagSurvivalTime = 20, scaleChangePar = 2, skillDamageChangeParTag = 3 });
                                _entityManager.AddComponentData(entityMineBlastAC, new SkillMineBlastExplosionTag() { tagSurvivalTime = 1, skillDamageChangeParTag = 1.5f, enableSecondA = true, tagSurvivalTimeSecond = 5, scaleChangePar = 4, enableSecondC = true, level = 1 });
                            }
                            for (int i = 0; i < 2; i++)
                            {
                                var par = 1 - 2 * i;
                                var entityMineBlastAC = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_MineBlast, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, new float3(10, 0, 10 * par), float3.zero, 1, false, false);
                                _entityManager.AddComponentData(entityMineBlastAC, new SkillMineBlastTag() { tagSurvivalTime = 20, scaleChangePar = 2, skillDamageChangeParTag = 3 });
                                _entityManager.AddComponentData(entityMineBlastAC, new SkillMineBlastExplosionTag() { tagSurvivalTime = 1, skillDamageChangeParTag = 1.5f, enableSecondA = true, tagSurvivalTimeSecond = 5, scaleChangePar = 4, enableSecondC = true, level = 1 });
                            }
                            break;
                        case HeroSkillPsionicType.PsionicBC:
                            for (int i = 0; i < 3; i++)
                            {
                                var entityMineBlastBC = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_MineBlast, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, new float3(10 * i, 0, 0), float3.zero, 1, false, false);
                                _entityManager.AddComponentData(entityMineBlastBC, new SkillMineBlastTag() { tagSurvivalTime = 20, scaleChangePar = 2, skillDamageChangeParTag = 3 });
                                _entityManager.AddComponentData(entityMineBlastBC, new SkillMineBlastExplosionTag() { tagSurvivalTime = 1, enableSecondB = true, enableSecondC = true, level = 1 });
                            }
                            for (int i = 0; i < 2; i++)
                            {
                                var par = 1 - 2 * i;
                                var entityMineBlastBC = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_MineBlast, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, new float3(10, 0, 10 * par), float3.zero, 1, false, false);
                                _entityManager.AddComponentData(entityMineBlastBC, new SkillMineBlastTag() { tagSurvivalTime = 20, scaleChangePar = 2, skillDamageChangeParTag = 3 });
                                _entityManager.AddComponentData(entityMineBlastBC, new SkillMineBlastExplosionTag() { tagSurvivalTime = 1, enableSecondB = true, enableSecondC = true, level = 1 });
                            }
                            break;
                        //终极三灵能效果
                        case HeroSkillPsionicType.PsionicABC:
                            for (int i = 0; i < 3; i++)
                            {
                                var entityMineBlastABC = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_MineBlast, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, new float3(10 * i, 0, 0), float3.zero, 1, false, false);
                                _entityManager.AddComponentData(entityMineBlastABC, new SkillMineBlastTag() { tagSurvivalTime = 20, scaleChangePar = 2, skillDamageChangeParTag = 3 });
                                _entityManager.AddComponentData(entityMineBlastABC, new SkillMineBlastExplosionTag() { tagSurvivalTime = 1, skillDamageChangeParTag = 1.5f, enableSecondA = true, enableSecondB = true, tagSurvivalTimeSecond = 5, scaleChangePar = 4, enableSecondC = true, level = 1 });
                            }
                            for (int i = 0; i < 2; i++)
                            {
                                var par = 1 - 2 * i;
                                var entityMineBlastABC = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_MineBlast, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, new float3(10, 0, 10 * par), float3.zero, 1, false, false);
                                _entityManager.AddComponentData(entityMineBlastABC, new SkillMineBlastTag() { tagSurvivalTime = 20, scaleChangePar = 2, skillDamageChangeParTag = 3 });
                                _entityManager.AddComponentData(entityMineBlastABC, new SkillMineBlastExplosionTag() { tagSurvivalTime = 1, skillDamageChangeParTag = 1.5f, enableSecondA = true, enableSecondB = true, tagSurvivalTimeSecond = 5, scaleChangePar = 4, enableSecondC = true, level = 1 });
                            }
                            break;

                    }
                    break;
                //暗影洪流 15，持续，引导
                case HeroSkillID.ShadowTide:
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:

                            //基础能量大于
                            bool hasShadowTide = _shadowTideQuery.CalculateEntityCount() > 0;

                            //这里获取世界中的事实entity
                            var realAttr = _entityManager.GetComponentData<HeroAttributeCmpt>(_heroEntity);
                            if (!hasShadowTide)
                            {
                                if (realAttr.defenseAttribute.energy >= 20)
                                {
                                    realAttr.defenseAttribute.energy -= 20;
                                    _entityManager.SetComponentData(_heroEntity, realAttr);
                                    var filter = new CollisionFilter
                                    {
                                        //属于道具层
                                        BelongsTo = 1u << 10,
                                        //检测敌人
                                        CollidesWith = 1u << 6,
                                        GroupIndex = 0
                                    };
                                    var overlap = new OverlapOverTimeQueryCenter { center = Hero.instance.transform.position, radius = 1, filter = filter, offset = new float3(0, 0, 15), box = new float3(6, 6, 40), shape = OverLapShape.Box };
                                    var entityShadowTide = DamageSkillsOverTimeProp(_skillPrefabs.HeroSkill_ShadowTide, overlap, Hero.instance.transform.position, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                                    _entityManager.AddComponentData(entityShadowTide, new SkillShadowTideTag { tagSurvivalTime = 10, level = 1 });
                                    var skillPar = _entityManager.GetComponentData<SkillsOverTimeDamageCalPar>(entityShadowTide);
                                    _entityManager.SetComponentData(entityShadowTide, skillPar);
                                }
                            }
                            else
                            {
                                //再次点击手动关闭
                                var shadowTideEntity = _shadowTideQuery.GetSingletonRW<SkillShadowTideTag>();

                                shadowTideEntity.ValueRW.closed = true;

                            }
                            break;
                        case HeroSkillPsionicType.PsionicA:

                            //基础能量大于20 
                            bool hasShadowTideA = _shadowTideQuery.CalculateEntityCount() > 0;

                            //这里获取世界中的事实entity
                            var realAttrA = _entityManager.GetComponentData<HeroAttributeCmpt>(_heroEntity);
                            if (!hasShadowTideA)
                            {
                                if (realAttrA.defenseAttribute.energy >= 20)
                                {
                                    realAttrA.defenseAttribute.energy -= 20;
                                    _entityManager.SetComponentData(_heroEntity, realAttrA);
                                    var filter = new CollisionFilter
                                    {
                                        //属于道具层
                                        BelongsTo = 1u << 10,
                                        //检测敌人
                                        CollidesWith = 1u << 6,
                                        GroupIndex = 0
                                    };
                                    var overlapA = new OverlapOverTimeQueryCenter { center = Hero.instance.transform.position, radius = 1, filter = filter, offset = new float3(0, 0, 15), box = new float3(3, 3, 40), shape = OverLapShape.Box };
                                    var entityShadowTideA = DamageSkillsOverTimeProp(_skillPrefabs.HeroSkillAssistive_ShadowTideA, overlapA, Hero.instance.transform.position, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                                    _entityManager.AddComponentData(entityShadowTideA, new SkillShadowTideTag { tagSurvivalTime = 3, level = 1, skillDamageChangeParTag = 2 });
                                    var skillParA = _entityManager.GetComponentData<SkillsOverTimeDamageCalPar>(entityShadowTideA);
                                    //添加引力特效
                                    skillParA.enablePull = true;
                                    //伤害翻倍
                                    skillParA.damageChangePar = 2;
                                    _entityManager.SetComponentData(entityShadowTideA, skillParA);
                                }
                            }
                            else
                            {
                                //再次点击手动关闭
                                var shadowTideEntity = _shadowTideQuery.GetSingletonRW<SkillShadowTideTag>();

                                shadowTideEntity.ValueRW.closed = true;

                            }

                            break;

                        case HeroSkillPsionicType.PsionicB:

                            //基础能量大于20 
                            bool hasShadowTideB = _shadowTideQuery.CalculateEntityCount() > 0;

                            //这里获取世界中的事实entity
                            var realAttrB = _entityManager.GetComponentData<HeroAttributeCmpt>(_heroEntity);
                            if (!hasShadowTideB)
                            {
                                if (realAttrB.defenseAttribute.energy >= 20)
                                {
                                    realAttrB.defenseAttribute.energy -= 20;
                                    _entityManager.SetComponentData(_heroEntity, realAttrB);
                                    var filter = new CollisionFilter
                                    {
                                        //属于道具层
                                        BelongsTo = 1u << 10,
                                        //检测敌人
                                        CollidesWith = 1u << 6,
                                        GroupIndex = 0
                                    };
                                    var overlapB = new OverlapOverTimeQueryCenter { center = Hero.instance.transform.position, radius = 1, filter = filter, offset = new float3(0, 0, 15), box = new float3(3, 3, 40), shape = OverLapShape.Box };
                                    var entityShadowTideB = DamageSkillsOverTimeProp(_skillPrefabs.HeroSkill_ShadowTide, overlapB, Hero.instance.transform.position, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                                    _entityManager.AddComponentData(entityShadowTideB, new SkillShadowTideTag { tagSurvivalTime = 3, level = 1, skillDamageChangeParTag = 1, enableSecondB = true });
                                    var skillParB = _entityManager.GetComponentData<SkillsOverTimeDamageCalPar>(entityShadowTideB);
                                    _entityManager.SetComponentData(entityShadowTideB, skillParB);
                                }
                            }
                            else
                            {
                                //再次点击手动关闭
                                var shadowTideEntity = _shadowTideQuery.GetSingletonRW<SkillShadowTideTag>();

                                shadowTideEntity.ValueRW.closed = true;

                            }

                            break;


                        case HeroSkillPsionicType.PsionicAB:

                            //基础能量大于20
                            bool hasShadowTideAB = _shadowTideQuery.CalculateEntityCount() > 0;

                            //这里获取世界中的事实entity
                            var realAttrAB = _entityManager.GetComponentData<HeroAttributeCmpt>(_heroEntity);
                            if (!hasShadowTideAB)
                            {
                                if (realAttrAB.defenseAttribute.energy >= 20)
                                {
                                    realAttrAB.defenseAttribute.energy -= 20;
                                    _entityManager.SetComponentData(_heroEntity, realAttrAB);
                                    var filter = new CollisionFilter
                                    {
                                        //属于道具层
                                        BelongsTo = 1u << 10,
                                        //检测敌人
                                        CollidesWith = 1u << 6,
                                        GroupIndex = 0
                                    };
                                    var overlapAB = new OverlapOverTimeQueryCenter { center = Hero.instance.transform.position, radius = 1, filter = filter, offset = new float3(0, 0, 15), box = new float3(3, 3, 40), shape = OverLapShape.Box };
                                    var entityShadowTideAB = DamageSkillsOverTimeProp(_skillPrefabs.HeroSkillAssistive_ShadowTideA, overlapAB, Hero.instance.transform.position, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                                    _entityManager.AddComponentData(entityShadowTideAB, new SkillShadowTideTag { tagSurvivalTime = 3, level = 1, skillDamageChangeParTag = 2, enableSecondB = true });
                                    var skillParAB = _entityManager.GetComponentData<SkillsOverTimeDamageCalPar>(entityShadowTideAB);
                                    //添加引力特效
                                    skillParAB.enablePull = true;
                                    //伤害翻倍
                                    skillParAB.damageChangePar = 2;
                                    _entityManager.SetComponentData(entityShadowTideAB, skillParAB);
                                }
                            }
                            else
                            {
                                //再次点击手动关闭
                                var shadowTideEntity = _shadowTideQuery.GetSingletonRW<SkillShadowTideTag>();

                                shadowTideEntity.ValueRW.closed = true;

                            }

                            break;
                    }

                    break;
                //时间缓速 16 消耗/持续，增益,每秒降低5点
                case HeroSkillID.TimeSlow:
                    _entityManager.SetComponentEnabled<SkillTimeSlowTag_Hero>(_heroEntity, true);
                    var skillTimeslowCmp = _entityManager.GetComponentData<SkillTimeSlowTag_Hero>(_heroEntity);
                    if (skillTimeslowCmp.active)
                    {
                        //开关节点，关闭之后重置初始化
                        skillTimeslowCmp.active = false;

                        skillTimeslowCmp.initialized = false;

                        _entityManager.SetComponentData(_heroEntity, skillTimeslowCmp);
                    }
                    else
                    {
                        switch (psionicType)
                        {
                            case HeroSkillPsionicType.Basic:

                                if (runTimeHeroCmp.defenseAttribute.energy > 20)
                                {
                                    runTimeHeroCmp.defenseAttribute.energy -= 20;
                                    skillTimeslowCmp.active = true;
                                    skillTimeslowCmp.tagSurvivalTime = 3;
                                    _entityManager.SetComponentData(_heroEntity, skillTimeslowCmp);
                                    _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                }

                                break;
                            case HeroSkillPsionicType.PsionicA:
                                if (runTimeHeroCmp.defenseAttribute.energy > 20)
                                {
                                    runTimeHeroCmp.defenseAttribute.energy -= 20;
                                    skillTimeslowCmp.active = true;
                                    skillTimeslowCmp.tagSurvivalTime = 3;
                                    skillTimeslowCmp.enableSecondA = true;
                                    _entityManager.SetComponentData(_heroEntity, skillTimeslowCmp);
                                    _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                }
                                break;
                            case HeroSkillPsionicType.PsionicB:
                                if (runTimeHeroCmp.defenseAttribute.energy > 20)
                                {
                                    runTimeHeroCmp.defenseAttribute.energy -= 20;
                                    skillTimeslowCmp.active = true;
                                    skillTimeslowCmp.tagSurvivalTime = 3;
                                    skillTimeslowCmp.enableSecondB = true;
                                    _entityManager.SetComponentData(_heroEntity, skillTimeslowCmp);
                                    _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                }
                                break;
                            case HeroSkillPsionicType.PsionicAB:
                                if (runTimeHeroCmp.defenseAttribute.energy > 20)
                                {
                                    runTimeHeroCmp.defenseAttribute.energy -= 20;
                                    skillTimeslowCmp.active = true;
                                    skillTimeslowCmp.tagSurvivalTime = 3;
                                    skillTimeslowCmp.enableSecondB = true;
                                    skillTimeslowCmp.enableSecondA = true;
                                    _entityManager.SetComponentData(_heroEntity, skillTimeslowCmp);
                                    _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                }
                                break;



                        }
                    }
                    break;
                //烈焰冲锋 17 位移/持续/保护/控制 -- 这里应该是开启一个0.5秒的位移携程
                case HeroSkillID.FlameCharge:
                    switch (psionicType)
                    {   //
                        case HeroSkillPsionicType.Basic:
                            if (runTimeHeroCmp.defenseAttribute.energy > 40)
                            {

                                var _rollCoroutineId = _coroutineController.StartRoutine(
                                IEFlameCharge(runTimeHeroCmp, runtimeStateNoImmunity),
                                tag: "FlameCharge",
                                onComplete: () =>
                                {
                                    DevDebug.Log("烈焰冲锋释放完毕");
                                }
                                );
                            }

                            break;
                        case HeroSkillPsionicType.PsionicA:
                            if (runTimeHeroCmp.defenseAttribute.energy > 40)
                            {
                            int preLevel = 10; 
                            var entityFlameCharge= DamageSkillsFlightProp(_skillPrefabs.HeroSkillAssistive_FlameChargeA, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1.0f, new float3(0, 0.0f, 0), 0, 1, false, false);
                            //获得昏迷值
                            var skillDamageCal = _entityManager.GetComponentData<SkillsDamageCalPar>(entityFlameCharge); 
                                skillDamageCal = Hero.instance.CalculateBaseSkillDamage(1, 1);//伤100% 易伤100%
                                skillDamageCal.tempknockback = 500;//附带500击退值
                                skillDamageCal.damageChangePar +=10+0.5f*(preLevel);//10倍+0.5倍
                                _entityManager.SetComponentData(entityFlameCharge, skillDamageCal);
                                Hero.instance.CalculateBaseSkillDamage();//重新计算爆伤易伤
                                //设置总体存活时间， 设置爆发时间，应该是到了爆发时间动态扩大烈焰爆发的技能检测范围
                                _entityManager.AddComponentData(entityFlameCharge, new SkillFlameChargeATag() { tagSurvivalTime = 0.5f, level = preLevel });
                                var _rollCoroutineId = _coroutineController.StartRoutine(
                                IEFlameCharge(runTimeHeroCmp, runtimeStateNoImmunity,preLevel),
                                tag: "FlameCharge",
                                onComplete: () =>
                                {
                                    DevDebug.Log("烈焰冲锋释放完毕");
                                }
                                );
                            }

                            break;
                        case HeroSkillPsionicType.PsionicB:
                             if (runTimeHeroCmp.defenseAttribute.energy > 40)
                            {
                                
                                var _rollCoroutineId = _coroutineController.StartRoutine(
                                IEFlameCharge(runTimeHeroCmp, runtimeStateNoImmunity,10,true),
                                tag: "FlameCharge",
                                onComplete: () =>
                                {
                                    DevDebug.Log("烈焰冲锋释放完毕");
                                }
                                );
                            }
                            break;
                        case HeroSkillPsionicType.PsionicAB:
                         if (runTimeHeroCmp.defenseAttribute.energy > 40)
                            {
                            int preLevel = 10; 
                            var entityFlameCharge= DamageSkillsFlightProp(_skillPrefabs.HeroSkillAssistive_FlameChargeA, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1.0f, new float3(0, 0.0f, 0), 0, 1, false, false);
                            //获得昏迷值
                            var skillDamageCal = _entityManager.GetComponentData<SkillsDamageCalPar>(entityFlameCharge); 
                                skillDamageCal = Hero.instance.CalculateBaseSkillDamage(1, 1);//伤100% 易伤100%
                                skillDamageCal.tempknockback = 500;//附带500击退值
                                skillDamageCal.damageChangePar +=10+0.5f*(preLevel);//10倍+0.5倍
                                if (UnityEngine.Random.Range(0, 1f) <= runTimeHeroCmp.attackAttribute.luckyStrikeChance * 0.5f * (0.25f + (0.015f * preLevel)))
                                {
                                    skillDamageCal.damageChangePar *= 3f;//三倍基础乘伤
                                    skillDamageCal.fireDotDamage += skillDamageCal.fireDamage;//dot伤害
                                    DevDebug.LogError("幸运增伤");
                                }
                                _entityManager.SetComponentData(entityFlameCharge, skillDamageCal);
                                Hero.instance.CalculateBaseSkillDamage();//重新计算爆伤易伤,切回伤害参数
                                //设置总体存活时间
                                _entityManager.AddComponentData(entityFlameCharge, new SkillFlameChargeATag() { tagSurvivalTime = 0.5f, level = preLevel });
                                var _rollCoroutineId = _coroutineController.StartRoutine(
                                IEFlameCharge(runTimeHeroCmp, runtimeStateNoImmunity,preLevel,true),
                                tag: "FlameCharge",
                                onComplete: () =>
                                {
                                    DevDebug.Log("烈焰冲锋释放完毕");
                                }
                                );
                            }
                            break;
                    }
                    break;
                //冰霜护盾 18
                case HeroSkillID.FrostShield:

                    var skillFrostShieldCmp = _entityManager.GetComponentData<SkillFrostShieldTag_Hero>(_heroEntity);
                    _entityManager.SetComponentEnabled<SkillFrostShieldTag_Hero>(_heroEntity, true);
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:
                            if (runTimeHeroCmp.defenseAttribute.energy > 100)
                            {
                                //冰霜护盾吸收公式
                                runTimeHeroCmp.defenseAttribute.frostBarrier = 3000 + (runTimeHeroCmp.defenseAttribute.energy) * (1 + runTimeHeroCmp.attackAttribute.attackPower / 100) * (1 + runTimeHeroCmp.attackAttribute.elementalDamage.frostDamage);
                                runTimeHeroCmp.defenseAttribute.energy = 0;
                                skillFrostShieldCmp.active = true;
                                skillFrostShieldCmp.relaseSkill = true;
                                //初始化护盾存在时间为60秒
                                skillFrostShieldCmp.tagSurvivalTime = 60;
                                _entityManager.SetComponentData(_heroEntity, skillFrostShieldCmp);
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                SkillSetActiveFrostShield(true);
                            }

                            break;
                        case HeroSkillPsionicType.PsionicA:
                            if (runTimeHeroCmp.defenseAttribute.energy > 100)
                            {
                                //冰霜护盾吸收公式
                                runTimeHeroCmp.defenseAttribute.frostBarrier = 3000 + (runTimeHeroCmp.defenseAttribute.energy) * (1 + runTimeHeroCmp.attackAttribute.attackPower / 100) * (1 + runTimeHeroCmp.attackAttribute.elementalDamage.frostDamage);
                                runTimeHeroCmp.defenseAttribute.energy = 0;
                                skillFrostShieldCmp.active = true;
                                //初始化护盾存在时间为60秒
                                skillFrostShieldCmp.tagSurvivalTime = 60;
                                skillFrostShieldCmp.relaseSkill = true;
                                //释放冰刺
                                skillFrostShieldCmp.enableSecondA = true;
                                //储存阶段冰刺伤害
                                skillFrostShieldCmp.iceConeDamage = runTimeHeroCmp.defenseAttribute.frostBarrier;
                                //开启A阶段控制标识
                                _entityManager.SetComponentData(_heroEntity, skillFrostShieldCmp);
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);

                                SkillSetActiveFrostShield(true);
                            }
                            break;
                        case HeroSkillPsionicType.PsionicB:
                            break;
                        case HeroSkillPsionicType.PsionicAB:
                            break;

                    }

                    break;
                //连锁吞噬 ：瞬时 19
                case HeroSkillID.ChainDevour:
                    //开启连锁吞噬碰撞，
                    detectionSystem.enableSpecialSkillChainDevour = true;
                    //DevDebug.LogError("是否开启连锁吞噬" + detectionSystem.enableSpecialSkillChainDevour);

                    switch (psionicType)
                    {

                        case HeroSkillPsionicType.Basic:
                            var filter = new CollisionFilter
                            {
                                //属于道具层
                                BelongsTo = 1u << 10,
                                //检测敌人
                                CollidesWith = 1u << 6,
                                GroupIndex = 0
                            };
                            var overlap = new OverlapTrackingQueryCenter { center = Hero.instance.transform.position, radius = 5, filter = filter, offset = new float3(0, 0, 0), shape = OverLapShape.Sphere };
                            //这里添加寻踪类技能专属标签
                            var entityChainDevour = DamageSkillsTrackingProp(_skillPrefabs.HeroSkill_ChainDevour, overlap, Hero.instance.transform.position, Hero.instance.transform.rotation, 1.0f, new float3(0, 0.0f, 0), 0, 1, false, false);
                            _entityManager.AddComponentData(entityChainDevour, new SkillChainDevourTag() { tagSurvivalTime = 10f, speed = 20 });

                            //可能的参数设置
                            var skillDamageChainDevour = _entityManager.GetComponentData<SkillsDamageCalPar>(entityChainDevour);
                            _entityManager.SetComponentData(entityChainDevour, skillDamageChainDevour);
                            //寻址技能参数变化配置
                            var skillsTrackingCalPar = _entityManager.GetComponentData<SkillsTrackingCalPar>(entityChainDevour);
                            _entityManager.SetComponentData(entityChainDevour, skillsTrackingCalPar);

                            break;
                        case HeroSkillPsionicType.PsionicA:

                            var filterA = new CollisionFilter
                            {
                                //属于道具层
                                BelongsTo = 1u << 10,
                                //检测敌人
                                CollidesWith = 1u << 6,
                                GroupIndex = 0
                            };
                            var overlapA = new OverlapTrackingQueryCenter { center = Hero.instance.transform.position, radius = 5, filter = filterA, offset = new float3(0, 0, 0), shape = OverLapShape.Sphere };
                            //这里添加寻踪类技能专属标签
                            var entityChainDevourA = DamageSkillsTrackingProp(_skillPrefabs.HeroSkill_ChainDevour, overlapA, Hero.instance.transform.position, Hero.instance.transform.rotation, 1.0f, new float3(0, 0.0f, 0), 0, 1, false, false);
                            _entityManager.AddComponentData(entityChainDevourA, new SkillChainDevourTag() { tagSurvivalTime = 10f, speed = 20 });
                            var tagA = _entityManager.GetComponentData<SkillChainDevourTag>(entityChainDevourA);
                            //可能的参数设置
                            var skillDamageChainDevourA = _entityManager.GetComponentData<SkillsDamageCalPar>(entityChainDevourA);
                            _entityManager.SetComponentData(entityChainDevourA, skillDamageChainDevourA);
                            //寻址技能参数变化配置,A阶变化技能添加碰撞次数
                            var skillsTrackingCalParA = _entityManager.GetComponentData<SkillsTrackingCalPar>(entityChainDevourA);
                            skillsTrackingCalParA.runCount += (tagA.level + 3);

                            _entityManager.SetComponentData(entityChainDevourA, skillsTrackingCalParA);


                            break;
                        case HeroSkillPsionicType.PsionicB:
                            //dotDamageSystem 里面关于连锁吞噬效果的处理（含内置1秒CD）
                            dotDamageSystem.EnbaleChainDecourSkillSecondB = true;
                            var filterB = new CollisionFilter
                            {
                                //属于道具层
                                BelongsTo = 1u << 10,
                                //检测敌人
                                CollidesWith = 1u << 6,
                                GroupIndex = 0
                            };
                            var overlapB = new OverlapTrackingQueryCenter { center = Hero.instance.transform.position, radius = 5, filter = filterB, offset = new float3(0, 0, 0), shape = OverLapShape.Sphere };
                            //这里添加寻踪类技能专属标签
                            var entityChainDevourB = DamageSkillsTrackingProp(_skillPrefabs.HeroSkill_ChainDevour, overlapB, Hero.instance.transform.position, Hero.instance.transform.rotation, 1.0f, new float3(0, 0.0f, 0), 0, 1, false, false);
                            _entityManager.AddComponentData(entityChainDevourB, new SkillChainDevourTag() { tagSurvivalTime = 10f, speed = 20, enableSecondB = true });

                            //可能的参数设置
                            var skillDamageChainDevourB = _entityManager.GetComponentData<SkillsDamageCalPar>(entityChainDevourB);
                            _entityManager.SetComponentData(entityChainDevourB, skillDamageChainDevourB);
                            //寻址技能参数变化配置
                            var skillsTrackingCalParB = _entityManager.GetComponentData<SkillsTrackingCalPar>(entityChainDevourB);
                            _entityManager.SetComponentData(entityChainDevourB, skillsTrackingCalParB);



                            break;
                        case HeroSkillPsionicType.PsionicAB:
                            var filterAB = new CollisionFilter
                            {
                                //属于道具层
                                BelongsTo = 1u << 10,
                                //检测敌人
                                CollidesWith = 1u << 6,
                                GroupIndex = 0
                            };
                            var overlapAB = new OverlapTrackingQueryCenter { center = Hero.instance.transform.position, radius = 5, filter = filterAB, offset = new float3(0, 0, 0), shape = OverLapShape.Sphere };
                            //这里添加寻踪类技能专属标签
                            var entityChainDevourAB = DamageSkillsTrackingProp(_skillPrefabs.HeroSkill_ChainDevour, overlapAB, Hero.instance.transform.position, Hero.instance.transform.rotation, 1.0f, new float3(0, 0.0f, 0), 0, 1, false, false);
                            _entityManager.AddComponentData(entityChainDevourAB, new SkillChainDevourTag() { tagSurvivalTime = 10f, speed = 20, enableSecondB = true });
                            var tagAB = _entityManager.GetComponentData<SkillChainDevourTag>(entityChainDevourAB);
                            //可能的参数设置
                            var skillDamageChainDevourAB = _entityManager.GetComponentData<SkillsDamageCalPar>(entityChainDevourAB);
                            _entityManager.SetComponentData(entityChainDevourAB, skillDamageChainDevourAB);
                            //寻址技能参数变化配置,A阶变化技能添加碰撞次数
                            var skillsTrackingCalParAB = _entityManager.GetComponentData<SkillsTrackingCalPar>(entityChainDevourAB);
                            skillsTrackingCalParAB.runCount += (tagAB.level + 3);

                            _entityManager.SetComponentData(entityChainDevourAB, skillsTrackingCalParAB);

                            break;

                    }
                    break;
                //雷霆之握：瞬时,标记 20    
                case HeroSkillID.ThunderGrip:

                    //开启特殊碰撞对确认系统,后期可以根据装载代码来进行处理
                    detectionSystem.enableSpecialSkillThunderGrip = true;
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:
                            var entityThunderGrip = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ThunderGrip, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1.0f, new float3(0, 0.0f, 0), 0, 1, false, false);
                            _entityManager.AddComponentData(entityThunderGrip, new SkillThunderGripTag() { tagSurvivalTime = 0.3f });
                            //获得昏迷值
                            var skillDamageCalTG = _entityManager.GetComponentData<SkillsDamageCalPar>(entityThunderGrip);
                            skillDamageCalTG.tempStun = 301;
                            _entityManager.SetComponentData(entityThunderGrip, skillDamageCalTG);

                            break;
                        case HeroSkillPsionicType.PsionicA:

                            var entityThunderGripA = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ThunderGrip, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1.0f, new float3(0, 0.0f, 0), 0, 1, false, false);
                            _entityManager.AddComponentData(entityThunderGripA, new SkillThunderGripTag() { tagSurvivalTime = 0.3f, enableSecondA = true });
                            ////获得昏迷值
                            var skillDamageCalTGA = _entityManager.GetComponentData<SkillsDamageCalPar>(entityThunderGripA);
                            skillDamageCalTGA.tempStun = 301;
                            _entityManager.SetComponentData(entityThunderGripA, skillDamageCalTGA);

                            break;
                        //爆发雷霆得参数通过 配置表进行更改，附加2倍的DOT伤害
                        case HeroSkillPsionicType.PsionicB:

                            var entityThunderGripB = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ThunderGrip, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1.0f, new float3(0, 0.0f, 0), 0, 1, false, false);
                            _entityManager.AddComponentData(entityThunderGripB, new SkillThunderGripTag() { tagSurvivalTime = 0.3f });
                            //获得昏迷值，获得冻结值
                            var skillDamageCalTGB = _entityManager.GetComponentData<SkillsDamageCalPar>(entityThunderGripB);
                            skillDamageCalTGB.tempStun = 301;
                            skillDamageCalTGB.tempFreeze = 301;
                            skillDamageCalTGB.frostDotDamage += 2 * skillDamageCalTGB.instantPhysicalDamage;
                            skillDamageCalTGB.fireDotDamage += 2 * skillDamageCalTGB.instantPhysicalDamage;
                            skillDamageCalTGB.shadowDotDamage += 2 * skillDamageCalTGB.instantPhysicalDamage;
                            skillDamageCalTGB.lightningDotDamage += 2 * skillDamageCalTGB.instantPhysicalDamage;
                            skillDamageCalTGB.poisonDotDamage += 2 * skillDamageCalTGB.instantPhysicalDamage;
                            skillDamageCalTGB.bleedDotDamage += 2 * skillDamageCalTGB.instantPhysicalDamage;
                            _entityManager.SetComponentData(entityThunderGripB, skillDamageCalTGB);
                            break;

                        case HeroSkillPsionicType.PsionicAB:

                            var entityThunderGripAB = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ThunderGrip, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1.0f, new float3(0, 0.0f, 0), 0, 1, false, false);
                            _entityManager.AddComponentData(entityThunderGripAB, new SkillThunderGripTag() { tagSurvivalTime = 0.3f });
                            //获得昏迷值，获得冻结值
                            var skillDamageCalTGAB = _entityManager.GetComponentData<SkillsDamageCalPar>(entityThunderGripAB);
                            skillDamageCalTGAB.tempStun = 301;
                            skillDamageCalTGAB.tempFreeze = 301;
                            skillDamageCalTGAB.frostDotDamage += 2 * skillDamageCalTGAB.instantPhysicalDamage;
                            skillDamageCalTGAB.fireDotDamage += 2 * skillDamageCalTGAB.instantPhysicalDamage;
                            skillDamageCalTGAB.shadowDotDamage += 2 * skillDamageCalTGAB.instantPhysicalDamage;
                            skillDamageCalTGAB.lightningDotDamage += 2 * skillDamageCalTGAB.instantPhysicalDamage;
                            skillDamageCalTGAB.poisonDotDamage += 2 * skillDamageCalTGAB.instantPhysicalDamage;
                            skillDamageCalTGAB.bleedDotDamage += 2 * skillDamageCalTGAB.instantPhysicalDamage;
                            _entityManager.SetComponentData(entityThunderGripAB, skillDamageCalTGAB);
                            break;

                    }
                    break;
                //炽炎烙印 21，标记 瞬时
                case HeroSkillID.ScorchMark:
                    //开启其炽炎烙印碰撞对收集
                    detectionSystem.enableSpecialSkillScorchMark = true;
                    switch (psionicType)
                    {
                        //基础技能添加炽炎烙印 标记
                        case HeroSkillPsionicType.Basic:
                            if (runTimeHeroCmp.defenseAttribute.energy > 20)
                            {
                                runTimeHeroCmp.defenseAttribute.energy -= 20;
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                var entityScorchMark = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ScorchMark, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1.0f, new float3(0, 0.0f, 0), 0, 1, false, false);
                                _entityManager.AddComponentData(entityScorchMark, new SkillScorchMarkTag { tagSurvivalTime = 0.3f });
                                //获得恐惧值
                                var skillDamageCal = _entityManager.GetComponentData<SkillsDamageCalPar>(entityScorchMark);
                                skillDamageCal.tempFear = 300;
                                _entityManager.SetComponentData(entityScorchMark, skillDamageCal);
                            }
                            break;
                        case HeroSkillPsionicType.PsionicA:
                            if (runTimeHeroCmp.defenseAttribute.energy > 20)
                            {
                                runTimeHeroCmp.defenseAttribute.energy -= 20;
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                var entityScorchMark = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ScorchMark, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1.0f, new float3(0, 0.0f, 0), 0, 1, false, false);
                                _entityManager.AddComponentData(entityScorchMark, new SkillScorchMarkTag { tagSurvivalTime = 0.3f, enableSecondA = true });
                                //获得恐惧值
                                var skillDamageCal = _entityManager.GetComponentData<SkillsDamageCalPar>(entityScorchMark);
                                skillDamageCal.tempFear = 300;
                                _entityManager.SetComponentData(entityScorchMark, skillDamageCal);
                            }
                            break;
                        case HeroSkillPsionicType.PsionicB:
                            break;
                        case HeroSkillPsionicType.PsionicAB:
                            break;

                    }
                    break;
                //寒霜新星, 瞬时 22    
                case HeroSkillID.FrostNova:
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:

                            var entityFrostNova = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_FrostNova, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, new float3(0, 0.0f, 0), 0, 1, false, false);
                            //添加技能专用标签用于检测等运动等    
                            _entityManager.AddComponentData(entityFrostNova, new SkillFrostNovaTag() { tagSurvivalTime = 3 });
                            var skillDamageCal = _entityManager.GetComponentData<SkillsDamageCalPar>(entityFrostNova);

                            break;
                        case HeroSkillPsionicType.PsionicA:
                            var entityFrostNovaA = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_FrostNova, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1.3f, new float3(0, 0.0f, 0), 0, 1, false, false);
                            _entityManager.AddComponentData(entityFrostNovaA, new SkillFrostNovaTag() { tagSurvivalTime = 3 });
                            //获得冻结值，成长20
                            var skillDamageCalA = _entityManager.GetComponentData<SkillsDamageCalPar>(entityFrostNovaA);
                            skillDamageCalA.tempFreeze = 301;
                            _entityManager.SetComponentData(entityFrostNovaA, skillDamageCalA);


                            break;
                        case HeroSkillPsionicType.PsionicB:
                            var entityFrostNovaB = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_FrostNova, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, new float3(0, 0.0f, 0), 0, 1, false, false);
                            //二阶段增加
                            _entityManager.AddComponentData(entityFrostNovaB, new SkillFrostNovaTag() { tagSurvivalTime = 3, enableSecondB = true });

                            break;
                        //开启第二阶段增加体积值，增加冻结值，获取控制标签
                        case HeroSkillPsionicType.PsionicAB:
                            var entityFrostNovaAB = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_FrostNova, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1.3f, new float3(0, 0.0f, 0), 0, 1, false, false);
                            _entityManager.AddComponentData(entityFrostNovaAB, new SkillFrostNovaTag() { tagSurvivalTime = 3, enableSecondB = true });
                            //获得冻结值，成长20
                            var skillDamageCalAB = _entityManager.GetComponentData<SkillsDamageCalPar>(entityFrostNovaAB);
                            skillDamageCalAB.tempFreeze = 301;
                            _entityManager.SetComponentData(entityFrostNovaAB, skillDamageCalAB);

                            break;
                    }
                    break;
                //暗影之拥 23 瞬时
                case HeroSkillID.ShadowEmbrace:
                    //激活暗影之拥
                    _entityManager.SetComponentEnabled<SkillShadowEmbrace_Hero>(_heroEntity, true);
                    var skillShadowEmbraceCmp = _entityManager.GetComponentData<SkillShadowEmbrace_Hero>(_heroEntity);
                    // Hero.instance.fsm.ChangeState<Hero_Stealth>();

                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:
                            if (!skillShadowEmbraceCmp.active)
                            {
                                if (runTimeHeroCmp.defenseAttribute.energy > 80)
                                {
                                    runTimeHeroCmp.defenseAttribute.energy -= 80;
                                    runTimeHeroCmp.defenseAttribute.moveSpeed *= 0.7f;//移速降低30%
                                    runTimeHeroCmp.gainAttribute.energyRegen *= 0.3f;//能量回复速度降低70%
                                    runTimeHeroCmp.gainAttribute.hpRegen *= 2f;//生命回复速度提升100%
                                    skillShadowEmbraceCmp.active = true;
                                    skillShadowEmbraceCmp.initialized = false;
                                    skillShadowEmbraceCmp.tagSurvivalTime = 7;
                                    skillShadowEmbraceCmp.shadowTime = 0;
                                    _entityManager.SetComponentData(_heroEntity, skillShadowEmbraceCmp);
                                    _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                    Hero.instance.skillAttackPar.stealth = true;
                                }
                            }
                            else
                            {
                                Hero.instance.skillAttackPar.stealth = false;
                                //这里会释放技能,且改变英雄的渲染状态,破隐时释放的暗影切割
                                skillShadowEmbraceCmp.active = false;
                                skillShadowEmbraceCmp.initialized = true;
                                skillShadowEmbraceCmp.shadowTime = 0;
                                //这里有可能出现其他参数对增加的增加！-后期更改
                                runTimeHeroCmp.defenseAttribute.moveSpeed = _heroAttributeCmptOriginal.defenseAttribute.moveSpeed;
                                runTimeHeroCmp.gainAttribute.energyRegen = _heroAttributeCmptOriginal.gainAttribute.energyRegen;
                                runTimeHeroCmp.gainAttribute.hpRegen = _heroAttributeCmptOriginal.gainAttribute.hpRegen;
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                //在英雄正前方生成一次暗影切割
                                var entiyShadowEmbrace = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ShadowEmbrace, Hero.instance.transform.position, Hero.instance.transform.rotation, 1, float3.zero, new float3(0, 0, 0), 1, false, false);
                                _entityManager.SetComponentData(_heroEntity, skillShadowEmbraceCmp);
                                //计算暴击
                                var skillCalParOverrride = Hero.instance.CalculateBaseSkillDamage(1);//必定触发暴击
                                skillCalParOverrride.shadowDotDamage = skillCalParOverrride.shadowDamage;//必定触发暗蚀
                                //写回暴击参数
                                _entityManager.SetComponentData(entiyShadowEmbrace, skillCalParOverrride);
                                Hero.instance.CalculateBaseSkillDamage();//再重新计算一次以手动更新，避免其他技能受影响

                                //暗影之拥抱攻击技能标签
                                _entityManager.AddComponentData(entiyShadowEmbrace, new SkillShadowEmbraceTag { tagSurvivalTime = 0.5f });
                                // Hero.instance.fsm.ChangeState<Hero_Idle>();
                            }

                            break;
                        //A阶段要添加暗影辉耀的持续伤害碰撞体
                        case HeroSkillPsionicType.PsionicA:
                            if (!skillShadowEmbraceCmp.active)
                            {
                                if (runTimeHeroCmp.defenseAttribute.energy > 80)
                                {
                                    runTimeHeroCmp.defenseAttribute.energy -= 80;
                                    runTimeHeroCmp.defenseAttribute.moveSpeed *= 0.7f;//移速降低30%
                                    runTimeHeroCmp.gainAttribute.energyRegen *= 0.3f;//能量回复速度降低70%
                                    runTimeHeroCmp.gainAttribute.hpRegen *= 2f;//生命回复速度提升100%
                                    skillShadowEmbraceCmp.active = true;
                                    skillShadowEmbraceCmp.initialized = false;
                                    //开启A阶段， 外部更新相关的参数
                                    skillShadowEmbraceCmp.enableSecondA = true;
                                    skillShadowEmbraceCmp.tagSurvivalTime = 7;
                                    skillShadowEmbraceCmp.shadowTime = 0;
                                    _entityManager.SetComponentData(_heroEntity, skillShadowEmbraceCmp);
                                    _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                    Hero.instance.skillAttackPar.stealth = true;

                                    var filter = new CollisionFilter
                                    {
                                        //属于道具层
                                        BelongsTo = 1u << 10,
                                        //检测敌人
                                        CollidesWith = 1u << 6,
                                        GroupIndex = 0
                                    };
                                    var overlapAB = new OverlapOverTimeQueryCenter { center = Hero.instance.transform.position, radius = 15, filter = filter, shape = OverLapShape.Sphere };
                                    //0.5f的 持续性伤害
                                    var entityShadowEmbraceA = DamageSkillsOverTimeProp(_skillPrefabs.HeroSkillAssistive_ShadowEmbraceA, overlapAB, Hero.instance.transform.position, Hero.instance.transform.rotation, 0.5f, float3.zero, new float3(0, 0, 0), 1, false, false);
                                    _entityManager.AddComponentData(entityShadowEmbraceA, new SkillShadowEmbraceAOverTimeTag { tagSurvivalTime = 7 });


                                }
                            }
                            else
                            {
                                Hero.instance.skillAttackPar.stealth = false;
                                //这里会释放技能,且改变英雄的渲染状态,破隐时释放的暗影切割
                                skillShadowEmbraceCmp.active = false;
                                skillShadowEmbraceCmp.initialized = true;
                                skillShadowEmbraceCmp.shadowTime = 0;
                                //这里有可能出现其他参数对增加的增加！-后期更改
                                runTimeHeroCmp.defenseAttribute.moveSpeed = _heroAttributeCmptOriginal.defenseAttribute.moveSpeed;
                                runTimeHeroCmp.gainAttribute.energyRegen = _heroAttributeCmptOriginal.gainAttribute.energyRegen;
                                runTimeHeroCmp.gainAttribute.hpRegen = _heroAttributeCmptOriginal.gainAttribute.hpRegen;
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                //在英雄正前方生成一次暗影切割
                                var entiyShadowEmbrace = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ShadowEmbrace, Hero.instance.transform.position, Hero.instance.transform.rotation, 1, float3.zero, new float3(0, 0, 0), 1, false, false);
                                _entityManager.SetComponentData(_heroEntity, skillShadowEmbraceCmp);
                                //计算暴击
                                var skillCalParOverrride = Hero.instance.CalculateBaseSkillDamage(1);//必定触发暴击
                                skillCalParOverrride.shadowDotDamage = skillCalParOverrride.shadowDamage;//必定触发暗蚀
                                //写回暴击参数
                                _entityManager.SetComponentData(entiyShadowEmbrace, skillCalParOverrride);
                                Hero.instance.CalculateBaseSkillDamage();//再重新计算一次以手动更新，避免其他技能受影响
                                //暗影之拥抱攻击技能标签
                                _entityManager.AddComponentData(entiyShadowEmbrace, new SkillShadowEmbraceTag { tagSurvivalTime = 0.5f });
                                // Hero.instance.fsm.ChangeState<Hero_Idle>();
                            }

                            break;
                        case HeroSkillPsionicType.PsionicB:

                            if (!skillShadowEmbraceCmp.active)
                            {
                                if (runTimeHeroCmp.defenseAttribute.energy > 80)
                                {
                                    runTimeHeroCmp.defenseAttribute.energy -= 80;
                                    runTimeHeroCmp.defenseAttribute.moveSpeed *= 0.7f;//移速降低30%
                                    runTimeHeroCmp.gainAttribute.energyRegen *= 0.3f;//能量回复速度降低70%
                                    runTimeHeroCmp.gainAttribute.hpRegen *= 2f;//生命回复速度提升100%
                                    skillShadowEmbraceCmp.active = true;
                                    skillShadowEmbraceCmp.initialized = false;
                                    //开启B阶段，外部调试相关的参数
                                    skillShadowEmbraceCmp.enableSecondB = true;
                                    skillShadowEmbraceCmp.tagSurvivalTime = 7;
                                    skillShadowEmbraceCmp.shadowTime = 0;
                                    _entityManager.SetComponentData(_heroEntity, skillShadowEmbraceCmp);
                                    _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                    Hero.instance.skillAttackPar.stealth = true;
                                }
                            }
                            else
                            {
                                Hero.instance.skillAttackPar.stealth = false;
                                //这里会释放技能,且改变英雄的渲染状态,破隐时释放的暗影切割
                                skillShadowEmbraceCmp.active = false;
                                skillShadowEmbraceCmp.initialized = true;
                                skillShadowEmbraceCmp.shadowTime = 0;
                                //这里有可能出现其他参数对增加的增加！-后期更改
                                runTimeHeroCmp.defenseAttribute.moveSpeed = _heroAttributeCmptOriginal.defenseAttribute.moveSpeed;
                                runTimeHeroCmp.gainAttribute.energyRegen = _heroAttributeCmptOriginal.gainAttribute.energyRegen;
                                runTimeHeroCmp.gainAttribute.hpRegen = _heroAttributeCmptOriginal.gainAttribute.hpRegen;
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                //在英雄正前方生成一次暗影切割
                                var entiyShadowEmbrace = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ShadowEmbrace, Hero.instance.transform.position, Hero.instance.transform.rotation, 1, float3.zero, new float3(0, 0, 0), 1, false, false);
                                _entityManager.SetComponentData(_heroEntity, skillShadowEmbraceCmp);
                                //计算暴击
                                var skillCalParOverrride = Hero.instance.CalculateBaseSkillDamage(1);//必定触发暴击
                                                                                                     //写回暴击参数
                                skillCalParOverrride.shadowDotDamage = skillCalParOverrride.shadowDamage;//必定触发暗蚀
                                _entityManager.SetComponentData(entiyShadowEmbrace, skillCalParOverrride);
                                Hero.instance.CalculateBaseSkillDamage();//再重新计算一次以手动更新，避免其他技能受影响
                                //暗影之拥抱攻击技能标签
                                _entityManager.AddComponentData(entiyShadowEmbrace, new SkillShadowEmbraceTag { tagSurvivalTime = 0.5f });
                                // Hero.instance.fsm.ChangeState<Hero_Idle>();
                            }
                            break;
                        case HeroSkillPsionicType.PsionicAB:
                            if (!skillShadowEmbraceCmp.active)
                            {
                                if (runTimeHeroCmp.defenseAttribute.energy > 80)
                                {
                                    runTimeHeroCmp.defenseAttribute.energy -= 80;
                                    runTimeHeroCmp.defenseAttribute.moveSpeed *= 0.7f;//移速降低30%
                                    runTimeHeroCmp.gainAttribute.energyRegen *= 0.3f;//能量回复速度降低70%
                                    runTimeHeroCmp.gainAttribute.hpRegen *= 2f;//生命回复速度提升100%
                                    skillShadowEmbraceCmp.active = true;
                                    skillShadowEmbraceCmp.initialized = false;
                                    //开启A阶段， 外部更新相关的参数
                                    skillShadowEmbraceCmp.enableSecondA = true;
                                    //开启B阶段， 外部更新相关的参数
                                    skillShadowEmbraceCmp.enableSecondB = true;
                                    skillShadowEmbraceCmp.tagSurvivalTime = 7;
                                    skillShadowEmbraceCmp.shadowTime = 0;
                                    _entityManager.SetComponentData(_heroEntity, skillShadowEmbraceCmp);
                                    _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                    Hero.instance.skillAttackPar.stealth = true;

                                    var filter = new CollisionFilter
                                    {
                                        //属于道具层
                                        BelongsTo = 1u << 10,
                                        //检测敌人
                                        CollidesWith = 1u << 6,
                                        GroupIndex = 0
                                    };
                                    var overlapAB = new OverlapOverTimeQueryCenter { center = Hero.instance.transform.position, radius = 15, filter = filter, shape = OverLapShape.Sphere };
                                    //0.5f的 持续性伤害
                                    var entityShadowEmbraceA = DamageSkillsOverTimeProp(_skillPrefabs.HeroSkillAssistive_ShadowEmbraceA, overlapAB, Hero.instance.transform.position, Hero.instance.transform.rotation, 0.5f, float3.zero, new float3(0, 0, 0), 1, false, false);
                                    _entityManager.AddComponentData(entityShadowEmbraceA, new SkillShadowEmbraceAOverTimeTag { tagSurvivalTime = 7 });

                                }
                            }
                            else
                            {
                                Hero.instance.skillAttackPar.stealth = false;
                                //这里会释放技能,且改变英雄的渲染状态,破隐时释放的暗影切割
                                skillShadowEmbraceCmp.active = false;
                                skillShadowEmbraceCmp.initialized = true;
                                skillShadowEmbraceCmp.shadowTime = 0;
                                //这里有可能出现其他参数对增加的增加！-后期更改
                                runTimeHeroCmp.defenseAttribute.moveSpeed = _heroAttributeCmptOriginal.defenseAttribute.moveSpeed;
                                runTimeHeroCmp.gainAttribute.energyRegen = _heroAttributeCmptOriginal.gainAttribute.energyRegen;
                                runTimeHeroCmp.gainAttribute.hpRegen = _heroAttributeCmptOriginal.gainAttribute.hpRegen;
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                //在英雄正前方生成一次暗影切割
                                var entiyShadowEmbrace = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ShadowEmbrace, Hero.instance.transform.position, Hero.instance.transform.rotation, 1, float3.zero, new float3(0, 0, 0), 1, false, false);
                                _entityManager.SetComponentData(_heroEntity, skillShadowEmbraceCmp);
                                //计算暴击
                                var skillCalParOverrride = Hero.instance.CalculateBaseSkillDamage(1);//必定触发暴击
                                                                                                     //写回暴击参数
                                skillCalParOverrride.shadowDotDamage = skillCalParOverrride.shadowDamage;//必定触发暗蚀
                                _entityManager.SetComponentData(entiyShadowEmbrace, skillCalParOverrride);
                                Hero.instance.CalculateBaseSkillDamage();//再重新计算一次以手动更新，避免其他技能受影响
                                //暗影之拥抱攻击技能标签
                                _entityManager.AddComponentData(entiyShadowEmbrace, new SkillShadowEmbraceTag { tagSurvivalTime = 0.5f });
                                //  Hero.instance.fsm.ChangeState<Hero_Idle>();
                            }

                            break;
                    }
                    break;
                //瘟疫蔓延 24  消耗 辅助 核心
                case HeroSkillID.PlagueSpread:
                    //开启激活遍历
                    _entityManager.SetComponentEnabled<SkillPlagueSpread_Hero>(_heroEntity, true);
                    var skillPlagueSpreadCmp = _entityManager.GetComponentData<SkillPlagueSpread_Hero>(_heroEntity);
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:
                            if (!skillPlagueSpreadCmp.active)
                            {
                                if (runTimeHeroCmp.defenseAttribute.energy > 20)
                                {
                                    runTimeHeroCmp.defenseAttribute.energy -= 20;
                                    skillPlagueSpreadCmp.active = true;
                                    skillPlagueSpreadCmp.tagSurvivalTime = 3;
                                    skillPlagueSpreadCmp.energyCost = 5;//能量消耗， 这样可以配置了
                                    _entityManager.SetComponentData(_heroEntity, skillPlagueSpreadCmp);
                                    _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                }
                            }
                            else
                            {

                                skillPlagueSpreadCmp.active = false;
                                _entityManager.SetComponentData(_heroEntity, skillPlagueSpreadCmp);
                            }
                            break;
                        case HeroSkillPsionicType.PsionicA:
                            if (!skillPlagueSpreadCmp.active)
                            {
                                if (runTimeHeroCmp.defenseAttribute.energy > 20)
                                {
                                    runTimeHeroCmp.defenseAttribute.energy -= 20;
                                    skillPlagueSpreadCmp.active = true;
                                    skillPlagueSpreadCmp.tagSurvivalTime = 3;
                                    skillPlagueSpreadCmp.enableSecondA = true;
                                    skillPlagueSpreadCmp.energyCost = 5;//能量消耗， 这样可以配置了
                                    _entityManager.SetComponentData(_heroEntity, skillPlagueSpreadCmp);
                                    _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                }
                            }
                            else
                            {

                                skillPlagueSpreadCmp.active = false;
                                _entityManager.SetComponentData(_heroEntity, skillPlagueSpreadCmp);
                            }
                            break;
                        case HeroSkillPsionicType.PsionicB:
                            if (!skillPlagueSpreadCmp.active)
                            {
                                if (runTimeHeroCmp.defenseAttribute.energy > 20)
                                {
                                    runTimeHeroCmp.defenseAttribute.energy -= 20;
                                    skillPlagueSpreadCmp.active = true;
                                    skillPlagueSpreadCmp.tagSurvivalTime = 3;
                                    skillPlagueSpreadCmp.enableSecondB = true;
                                    skillPlagueSpreadCmp.energyCost = 5;//能量消耗， 这样可以配置了
                                    _entityManager.SetComponentData(_heroEntity, skillPlagueSpreadCmp);
                                    _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                }
                            }
                            else
                            {

                                skillPlagueSpreadCmp.active = false;
                                _entityManager.SetComponentData(_heroEntity, skillPlagueSpreadCmp);
                            }
                            break;
                        case HeroSkillPsionicType.PsionicAB:
                            if (!skillPlagueSpreadCmp.active)
                            {
                                if (runTimeHeroCmp.defenseAttribute.energy > 20)
                                {
                                    runTimeHeroCmp.defenseAttribute.energy -= 20;
                                    skillPlagueSpreadCmp.active = true;
                                    skillPlagueSpreadCmp.tagSurvivalTime = 3;
                                    skillPlagueSpreadCmp.enableSecondA = true;
                                    skillPlagueSpreadCmp.enableSecondB = true;
                                    skillPlagueSpreadCmp.energyCost = 5;//能量消耗， 这样可以配置了
                                    _entityManager.SetComponentData(_heroEntity, skillPlagueSpreadCmp);
                                    _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                }
                            }
                            else
                            {

                                skillPlagueSpreadCmp.active = false;
                                _entityManager.SetComponentData(_heroEntity, skillPlagueSpreadCmp);
                            }
                            break;
                    }
                    break;
                //元素护盾 25 保护/唯一，保护技能25号元素可以仅添加渲染即可
                case HeroSkillID.ElementShield:
                    var skillElementShieldCmp = _entityManager.GetComponentData<SkillElementShieldTag_Hero>(_heroEntity);
                    _entityManager.SetComponentEnabled<SkillElementShieldTag_Hero>(_heroEntity, true);
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:
                            if (!skillElementShieldCmp.active)
                            {
                                if (runTimeHeroCmp.defenseAttribute.energy > 20)
                                {
                                    runTimeHeroCmp.defenseAttribute.energy -= 20;
                                    skillElementShieldCmp.active = true;
                                    _entityManager.SetComponentData(_heroEntity, skillElementShieldCmp);
                                    _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                    SkillSetActiveElementShield(true);
                                }
                            }
                            else
                            {

                                skillElementShieldCmp.active = false;
                                _entityManager.SetComponentData(_heroEntity, skillElementShieldCmp);
                                SkillSetActiveElementShield(false);
                            }

                            break;
                        case HeroSkillPsionicType.PsionicA:
                            if (!skillElementShieldCmp.active)
                            {
                                if (runTimeHeroCmp.defenseAttribute.energy > 20)
                                {
                                    runTimeHeroCmp.defenseAttribute.energy -= 20;
                                    skillElementShieldCmp.active = true;
                                    skillElementShieldCmp.enableSecondA = true;
                                    _entityManager.SetComponentData(_heroEntity, skillElementShieldCmp);
                                    _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                    SkillSetActiveElementShield(true);
                                }
                            }
                            else
                            {

                                skillElementShieldCmp.active = false;
                                _entityManager.SetComponentData(_heroEntity, skillElementShieldCmp);
                                SkillSetActiveElementShield(false);
                            }

                            break;
                        case HeroSkillPsionicType.PsionicB:
                            if (!skillElementShieldCmp.active)
                            {
                                if (runTimeHeroCmp.defenseAttribute.energy > 20)
                                {
                                    runTimeHeroCmp.defenseAttribute.energy -= 20;
                                    skillElementShieldCmp.active = true;
                                    skillElementShieldCmp.enableSecondB = true;
                                    _entityManager.SetComponentData(_heroEntity, skillElementShieldCmp);
                                    _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                    SkillSetActiveElementShield(true);
                                }
                            }
                            else
                            {

                                skillElementShieldCmp.active = false;
                                _entityManager.SetComponentData(_heroEntity, skillElementShieldCmp);
                                SkillSetActiveElementShield(false);
                            }
                            break;
                        case HeroSkillPsionicType.PsionicAB:
                            if (!skillElementShieldCmp.active)
                            {
                                if (runTimeHeroCmp.defenseAttribute.energy > 20)
                                {
                                    runTimeHeroCmp.defenseAttribute.energy -= 20;
                                    skillElementShieldCmp.active = true;
                                    skillElementShieldCmp.enableSecondA = true;
                                    skillElementShieldCmp.enableSecondB = true;
                                    _entityManager.SetComponentData(_heroEntity, skillElementShieldCmp);
                                    _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                    SkillSetActiveElementShield(true);
                                }
                            }
                            else
                            {

                                skillElementShieldCmp.active = false;
                                _entityManager.SetComponentData(_heroEntity, skillElementShieldCmp);
                                SkillSetActiveElementShield(false);
                            }
                            break;
                    }
                    break;
                //烈焰灵刃 26 瞬发
                case HeroSkillID.FlameSpiritBlade:
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:
                            if (runTimeHeroCmp.defenseAttribute.energy > 25)
                            {
                                runTimeHeroCmp.defenseAttribute.energy -= 25;
                                var entityFlameSpiritBlade = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_FlameSpiritBlade, Hero.instance.targetPosition, Hero.instance.transform.rotation, 1.0f, new float3(0, 0.0f, 0), 0, 1, false, false);
                                _entityManager.AddComponentData(entityFlameSpiritBlade, new SkillFlameSpiritBladeTag() { tagSurvivalTime = 8f, speed = 10, startPosition = Hero.instance.targetPosition });
                                //取出攻击参数，写回击退值
                                var skillCal = _entityManager.GetComponentData<SkillsDamageCalPar>(entityFlameSpiritBlade);
                                skillCal.tempknockback = 300;
                                _entityManager.SetComponentData(entityFlameSpiritBlade, skillCal);
                            }

                            break;
                        case HeroSkillPsionicType.PsionicA:
                            if (runTimeHeroCmp.defenseAttribute.energy > 25)
                            {
                                runTimeHeroCmp.defenseAttribute.energy -= 25;
                                var entityFlameSpiritBlade = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_FlameSpiritBlade, Hero.instance.targetPosition, Hero.instance.transform.rotation, 1.0f, new float3(0, 0.0f, 0), 0, 1, false, false);
                                _entityManager.AddComponentData(entityFlameSpiritBlade, new SkillFlameSpiritBladeTag() { tagSurvivalTime = 8f, speed = 10, startPosition = Hero.instance.targetPosition, enableSecondA = true });
                                //取出攻击参数，写回击退值
                                var skillCal = _entityManager.GetComponentData<SkillsDamageCalPar>(entityFlameSpiritBlade);
                                skillCal.tempknockback = 300;
                                _entityManager.SetComponentData(entityFlameSpiritBlade, skillCal);
                            }

                            break;
                        case HeroSkillPsionicType.PsionicB:
                            if (runTimeHeroCmp.defenseAttribute.energy > 25)
                            {
                                runTimeHeroCmp.defenseAttribute.energy -= 25;
                                var entityFlameSpiritBlade = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_FlameSpiritBlade, Hero.instance.targetPosition, Hero.instance.transform.rotation, 1.0f, new float3(0, 0.0f, 0), 0, 1, false, false);
                                _entityManager.AddComponentData(entityFlameSpiritBlade, new SkillFlameSpiritBladeTag() { tagSurvivalTime = 8f, speed = 10, enableSecondB = true, startPosition = Hero.instance.targetPosition });
                                //取出攻击参数，写回击退值
                                var skillCal = _entityManager.GetComponentData<SkillsDamageCalPar>(entityFlameSpiritBlade);
                                skillCal.tempknockback = 300;
                                skillCal.damageChangePar += _heroAttributeCmptOriginal.attackAttribute.luckyStrikeChance;
                                _entityManager.SetComponentData(entityFlameSpiritBlade, skillCal);
                            }


                            break;
                        case HeroSkillPsionicType.PsionicAB:
                            if (runTimeHeroCmp.defenseAttribute.energy > 25)
                            {
                                runTimeHeroCmp.defenseAttribute.energy -= 25;
                                var entityFlameSpiritBlade = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_FlameSpiritBlade, Hero.instance.targetPosition, Hero.instance.transform.rotation, 1.0f, new float3(0, 0.0f, 0), 0, 1, false, false);
                                _entityManager.AddComponentData(entityFlameSpiritBlade, new SkillFlameSpiritBladeTag() { tagSurvivalTime = 8f, speed = 10, enableSecondB = true, enableSecondA = true, startPosition = Hero.instance.targetPosition });
                                //取出攻击参数，写回击退值
                                var skillCal = _entityManager.GetComponentData<SkillsDamageCalPar>(entityFlameSpiritBlade);
                                skillCal.tempknockback = 300;
                                skillCal.damageChangePar += _heroAttributeCmptOriginal.attackAttribute.luckyStrikeChance;
                                _entityManager.SetComponentData(entityFlameSpiritBlade, skillCal);
                            }

                            break;
                    }
                    break;
                //时空扭曲27 第一步  生成时空奇点 第二步 随时间变化 形态， 第三步 爆炸生成时空碎片
                case HeroSkillID.ChronoTwist:

                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:

                            if (runTimeHeroCmp.defenseAttribute.energy > 40)
                            {
                                runTimeHeroCmp.defenseAttribute.energy -= 40;
                                var damagePar = runTimeHeroCmp.defenseAttribute.energy / 100;//每1点灵力提升1点爆炸伤害，100点灵力提升30%的牵引伤害，提升500%的爆炸伤害
                                runTimeHeroCmp.defenseAttribute.energy = 0; //灵力归零
                                //写回能量扣减
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                var filter = new CollisionFilter
                                {
                                    BelongsTo = 1u << 10,
                                    CollidesWith = 1u << 6,
                                    GroupIndex = 0
                                };
                                var overlap = new OverlapOverTimeQueryCenter { center = Hero.instance.skillTargetPositon, radius = 15, filter = filter, offset = new float3(0, 0, 0) };
                                var enityChronoTwist = DamageSkillsOverTimeProp(_skillPrefabs.HeroSkill_ChronoTwist, overlap, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, new float3(0, 2, 0), float3.zero, 1, true, false);
                                _entityManager.AddComponentData(enityChronoTwist, new SkillChronoTwistTag { tagSurvivalTime = 6f + damagePar * 3f, level = 10, stratExplosionTime = 2f, skillDamageChangeParTag = 1 + damagePar * 5f });
                                var skillPar = _entityManager.GetComponentData<SkillsOverTimeDamageCalPar>(enityChronoTwist);
                                skillPar.tempSlow = 100;
                                skillPar.tempPull = 200;
                                skillPar.damageChangePar += damagePar * 0.3f;
                                _entityManager.SetComponentData(enityChronoTwist, skillPar);

                            }
                            break;
                        case HeroSkillPsionicType.PsionicA:
                            if (runTimeHeroCmp.defenseAttribute.energy > 40)
                            {
                                runTimeHeroCmp.defenseAttribute.energy -= 40;
                                var damagePar = runTimeHeroCmp.defenseAttribute.energy / 100;//每1点灵力提升1点爆炸伤害，100点灵力提升30%的牵引伤害，提升500%的爆炸伤害
                                runTimeHeroCmp.defenseAttribute.energy = 0; //灵力归零
                                //写回能量扣减
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                var filter = new CollisionFilter
                                {
                                    BelongsTo = 1u << 10,
                                    CollidesWith = 1u << 6,
                                    GroupIndex = 0
                                };
                                var overlap = new OverlapOverTimeQueryCenter { center = Hero.instance.skillTargetPositon, radius = 15, filter = filter, offset = new float3(0, 0, 0) };
                                var enityChronoTwist = DamageSkillsOverTimeProp(_skillPrefabs.HeroSkill_ChronoTwist, overlap, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, new float3(0, 2, 0), float3.zero, 1, true, false);
                                _entityManager.AddComponentData(enityChronoTwist, new SkillChronoTwistTag { tagSurvivalTime = 6f + damagePar * 3f, level = 10, stratExplosionTime = 2f, skillDamageChangeParTag = 1 + damagePar * 5f });
                                var skillPar = _entityManager.GetComponentData<SkillsOverTimeDamageCalPar>(enityChronoTwist);
                                skillPar.tempSlow = 100;
                                skillPar.tempPull = 200;
                                skillPar.damageChangePar += damagePar * 0.3f;
                                skillPar.damageChangePar *= 1.5f;//基础伤害提升50%，在加成蓄力加成之后提升，在乘以等级系数
                                _entityManager.SetComponentData(enityChronoTwist, skillPar);

                            }
                            break;
                        case HeroSkillPsionicType.PsionicB:
                            if (runTimeHeroCmp.defenseAttribute.energy > 40)
                            {
                                runTimeHeroCmp.defenseAttribute.energy -= 40;
                                var damagePar = runTimeHeroCmp.defenseAttribute.energy / 100;//每1点灵力提升1点爆炸伤害，100点灵力提升30%的牵引伤害，提升500%的爆炸伤害
                                runTimeHeroCmp.defenseAttribute.energy = 0; //灵力归零
                                //写回能量扣减
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                var filter = new CollisionFilter
                                {
                                    BelongsTo = 1u << 10,
                                    CollidesWith = 1u << 6,
                                    GroupIndex = 0
                                };
                                var overlap = new OverlapOverTimeQueryCenter { center = Hero.instance.skillTargetPositon, radius = 15, filter = filter, offset = new float3(0, 0, 0) };
                                var enityChronoTwist = DamageSkillsOverTimeProp(_skillPrefabs.HeroSkill_ChronoTwist, overlap, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, new float3(0, 2, 0), float3.zero, 1, true, false);
                                _entityManager.AddComponentData(enityChronoTwist, new SkillChronoTwistTag { tagSurvivalTime = 6f + damagePar * 3f, level = 10, stratExplosionTime = 2f, skillDamageChangeParTag = 1 + damagePar * 5f, enableSecondB = true });
                                var skillPar = _entityManager.GetComponentData<SkillsOverTimeDamageCalPar>(enityChronoTwist);
                                skillPar.tempSlow = 100;
                                skillPar.tempPull = 200;
                                skillPar.damageChangePar += damagePar * 0.3f;
                                _entityManager.SetComponentData(enityChronoTwist, skillPar);
                            }
                            break;
                        case HeroSkillPsionicType.PsionicAB:
                            if (runTimeHeroCmp.defenseAttribute.energy > 40)
                            {
                                runTimeHeroCmp.defenseAttribute.energy -= 40;
                                var damagePar = runTimeHeroCmp.defenseAttribute.energy / 100;//每1点灵力提升1点爆炸伤害，100点灵力提升30%的牵引伤害，提升500%的爆炸伤害
                                runTimeHeroCmp.defenseAttribute.energy = 0; //灵力归零
                                //写回能量扣减
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                var filter = new CollisionFilter
                                {
                                    BelongsTo = 1u << 10,
                                    CollidesWith = 1u << 6,
                                    GroupIndex = 0
                                };
                                var overlap = new OverlapOverTimeQueryCenter { center = Hero.instance.skillTargetPositon, radius = 15, filter = filter, offset = new float3(0, 0, 0) };
                                var enityChronoTwist = DamageSkillsOverTimeProp(_skillPrefabs.HeroSkill_ChronoTwist, overlap, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, new float3(0, 2, 0), float3.zero, 1, true, false);
                                _entityManager.AddComponentData(enityChronoTwist, new SkillChronoTwistTag { tagSurvivalTime = 6f + damagePar * 3f, level = 10, stratExplosionTime = 2f, skillDamageChangeParTag = 1 + damagePar * 5f, enableSecondB = true });
                                var skillPar = _entityManager.GetComponentData<SkillsOverTimeDamageCalPar>(enityChronoTwist);
                                skillPar.tempSlow = 100;
                                skillPar.tempPull = 200;
                                skillPar.damageChangePar += damagePar * 0.3f;
                                skillPar.damageChangePar *= 1.5f;//基础伤害提升50%，在加成蓄力加成之后提升，在乘以等级系数
                                _entityManager.SetComponentData(enityChronoTwist, skillPar);
                            }
                            break;
                    }
                    break;
                //烈焰爆发 28 爆发
                case HeroSkillID.FlameBurst:
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:

                            // DevDebug.LogError("进入烈焰爆发");
                            if (runTimeHeroCmp.defenseAttribute.energy > 40)
                            {
                                int preLevel = 10;
                                runTimeHeroCmp.defenseAttribute.energy -= 40;
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                var filter = new CollisionFilter
                                {
                                    //属于道具层
                                    BelongsTo = 1u << 10,
                                    //检测敌人
                                    CollidesWith = 1u << 6,
                                    GroupIndex = 0
                                };
                                var overlap = new OverlapBurstQueryCenter { center = Hero.instance.transform.position, radius = 12f, filter = filter, offset = new float3(0, 0, 0), shape = OverLapShape.Sphere };
                                var flameBurstEntity = DamageSkillsBrustProp(_skillPrefabs.HeroSkill_FlameBurst, overlap, Hero.instance.transform.position, Hero.instance.transform.rotation,
                                1, float3.zero, float3.zero, 1, false, true);
                                var skillBurstDamageCal = _entityManager.GetComponentData<SkillsBurstDamageCalPar>(flameBurstEntity);
                                skillBurstDamageCal.tempknockback = 300;//附带300击退值
                                _entityManager.SetComponentData(flameBurstEntity, skillBurstDamageCal);
                                //设置总体存活时间， 设置爆发时间，应该是到了爆发时间动态扩大烈焰爆发的技能检测范围
                                _entityManager.AddComponentData(flameBurstEntity, new SkillFlameBurstTag() { tagSurvivalTime = 0.5f, level = preLevel });
                            }
                            break;
                        case HeroSkillPsionicType.PsionicA:
                            if (runTimeHeroCmp.defenseAttribute.energy > 40)
                            {
                                int preLevel = 10;
                                runTimeHeroCmp.defenseAttribute.energy -= 40;
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                var filter = new CollisionFilter
                                {
                                    //属于道具层
                                    BelongsTo = 1u << 10,
                                    //检测敌人
                                    CollidesWith = 1u << 6,
                                    GroupIndex = 0
                                };
                                var overlap = new OverlapBurstQueryCenter { center = Hero.instance.transform.position, radius = 12f, filter = filter, offset = new float3(0, 0, 0), shape = OverLapShape.Sphere };
                                var flameBurstEntity = DamageSkillsBrustProp(_skillPrefabs.HeroSkill_FlameBurst, overlap, Hero.instance.transform.position, Hero.instance.transform.rotation,
                                1, float3.zero, float3.zero, 1, false, true);
                                var skillBurstDamageCal = _entityManager.GetComponentData<SkillsBurstDamageCalPar>(flameBurstEntity);
                                //新增爆燃冲击的压制逻辑
                                var tempCount = Hero.instance.skillAttackPar.flameBurstRelasecount += 1;
                                if (tempCount >= (11 - preLevel * 0.5f))
                                {
                                    DevDebug.LogError("压制");
                                    skillBurstDamageCal = Hero.instance.CalculateBurstSkillDamage(0, 0, 1);//产生压制
                                    Hero.instance.skillAttackPar.flameBurstRelasecount = 0;//清零计数器
                                }

                                skillBurstDamageCal.tempknockback = 300;//附带300击退值
                                _entityManager.SetComponentData(flameBurstEntity, skillBurstDamageCal);
                                //设置总体存活时间， 设置爆发时间，应该是到了爆发时间动态扩大烈焰爆发的技能检测范围
                                _entityManager.AddComponentData(flameBurstEntity, new SkillFlameBurstTag() { tagSurvivalTime = 0.5f, level = preLevel });

                            }
                            break;
                        case HeroSkillPsionicType.PsionicB:
                            _entityManager.SetComponentEnabled<SkillFlameBurst_Hero>(_heroEntity, true);//开启烈焰爆发B阶段加载标签
                            if (runTimeHeroCmp.defenseAttribute.energy > 40)
                            {
                                int preLevel = 10;

                                runTimeHeroCmp.defenseAttribute.energy -= 40;
                                //烈焰爆发独立增伤
                                if (runTimeHeroCmp.attackAttribute.heroDynamicalAttack.tempFlameBurstBDamagePar < (8 * (0.15f + 0.01f * preLevel)))
                                    runTimeHeroCmp.attackAttribute.heroDynamicalAttack.tempFlameBurstBDamagePar += 0.15f + 0.01f * preLevel;
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                var flameBurstStength = _entityManager.GetComponentData<SkillFlameBurst_Hero>(_heroEntity);
                                flameBurstStength.tagSurvivalTime = 6;//重置6秒的加成时间
                                _entityManager.SetComponentData(_heroEntity, flameBurstStength);//写回-由外部检测加成时间                              
                                var filter = new CollisionFilter
                                {
                                    //属于道具层
                                    BelongsTo = 1u << 10,
                                    //检测敌人
                                    CollidesWith = 1u << 6,
                                    GroupIndex = 0
                                };
                                var overlap = new OverlapBurstQueryCenter { center = Hero.instance.transform.position, radius = 12f, filter = filter, offset = new float3(0, 0, 0), shape = OverLapShape.Sphere };
                                var flameBurstEntity = DamageSkillsBrustProp(_skillPrefabs.HeroSkill_FlameBurst, overlap, Hero.instance.transform.position, Hero.instance.transform.rotation,
                                1, float3.zero, float3.zero, 1, false, true);
                                var skillBurstDamageCal = _entityManager.GetComponentData<SkillsBurstDamageCalPar>(flameBurstEntity);
                                skillBurstDamageCal.tempknockback = 300;//附带300击退值
                                _entityManager.SetComponentData(flameBurstEntity, skillBurstDamageCal);
                                //设置总体存活时间， 设置爆发时间，应该是到了爆发时间动态扩大烈焰爆发的技能检测范围
                                _entityManager.AddComponentData(flameBurstEntity, new SkillFlameBurstTag() { tagSurvivalTime = 0.5f, level = preLevel });
                            }
                            break;
                        case HeroSkillPsionicType.PsionicC:

                            if (runTimeHeroCmp.defenseAttribute.energy > 40)
                            {
                                int preLevel = 10;
                                runTimeHeroCmp.defenseAttribute.energy -= 40;
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                var filter = new CollisionFilter
                                {
                                    //属于道具层
                                    BelongsTo = 1u << 10,
                                    //检测敌人
                                    CollidesWith = 1u << 6,
                                    GroupIndex = 0
                                };
                                var overlap = new OverlapBurstQueryCenter { center = Hero.instance.transform.position, radius = 12f, filter = filter, offset = new float3(0, 0, 0), shape = OverLapShape.Sphere };
                                var flameBurstEntity = DamageSkillsBrustProp(_skillPrefabs.HeroSkill_FlameBurst, overlap, Hero.instance.transform.position, Hero.instance.transform.rotation,
                                1, float3.zero, float3.zero, 1, false, true);
                                var skillBurstDamageCal = _entityManager.GetComponentData<SkillsBurstDamageCalPar>(flameBurstEntity);
                                //C阶段的幸运判定
                                var enbaleSup = UnityEngine.Random.Range(0, 1f) <= (0.25 + preLevel * 0.015) * 0.5f * runTimeHeroCmp.attackAttribute.luckyStrikeChance;
                                if (enbaleSup)
                                {
                                    DevDebug.LogError("压制");
                                    skillBurstDamageCal = Hero.instance.CalculateBurstSkillDamage(0, 0, 1);//产生压制
                                                                                                           // Hero.instance.skillAttackPar.flameBurstRelasecount = 0;//清零计数器
                                }

                                skillBurstDamageCal.tempknockback = 300;//附带300击退值
                                _entityManager.SetComponentData(flameBurstEntity, skillBurstDamageCal);
                                //设置总体存活时间， 设置爆发时间，应该是到了爆发时间动态扩大烈焰爆发的技能检测范围
                                _entityManager.AddComponentData(flameBurstEntity, new SkillFlameBurstTag() { tagSurvivalTime = 0.5f, level = preLevel });

                            }
                            break;
                        case HeroSkillPsionicType.PsionicAB:
                            _entityManager.SetComponentEnabled<SkillFlameBurst_Hero>(_heroEntity, true);//开启烈焰爆发B阶段加载标签
                            if (runTimeHeroCmp.defenseAttribute.energy > 40)
                            {
                                int preLevel = 10;

                                runTimeHeroCmp.defenseAttribute.energy -= 40;

                                //烈焰爆发独立增伤
                                if (runTimeHeroCmp.attackAttribute.heroDynamicalAttack.tempFlameBurstBDamagePar < (8 * (0.15f + 0.01f * preLevel)))
                                    runTimeHeroCmp.attackAttribute.heroDynamicalAttack.tempFlameBurstBDamagePar += 0.15f + 0.01f * preLevel;
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                var flameBurstStength = _entityManager.GetComponentData<SkillFlameBurst_Hero>(_heroEntity);
                                flameBurstStength.tagSurvivalTime = 6;//重置6秒的加成时间
                                _entityManager.SetComponentData(_heroEntity, flameBurstStength);//写回-由外部检测加成时间                              
                                var filter = new CollisionFilter
                                {
                                    //属于道具层
                                    BelongsTo = 1u << 10,
                                    //检测敌人
                                    CollidesWith = 1u << 6,
                                    GroupIndex = 0
                                };
                                var overlap = new OverlapBurstQueryCenter { center = Hero.instance.transform.position, radius = 12f, filter = filter, offset = new float3(0, 0, 0), shape = OverLapShape.Sphere };
                                var flameBurstEntity = DamageSkillsBrustProp(_skillPrefabs.HeroSkill_FlameBurst, overlap, Hero.instance.transform.position, Hero.instance.transform.rotation,
                                1, float3.zero, float3.zero, 1, false, true);
                                var skillBurstDamageCal = _entityManager.GetComponentData<SkillsBurstDamageCalPar>(flameBurstEntity);
                                //新增爆燃冲击A的压制逻辑
                                var tempCount = Hero.instance.skillAttackPar.flameBurstRelasecount += 1;
                                if (tempCount >= (11 - preLevel * 0.5f))
                                {
                                    DevDebug.LogError("压制");
                                    skillBurstDamageCal = Hero.instance.CalculateBurstSkillDamage(0, 0, 1);//产生压制
                                    Hero.instance.skillAttackPar.flameBurstRelasecount = 0;//清零计数器
                                }

                                skillBurstDamageCal.tempknockback = 300;//附带300击退值
                                _entityManager.SetComponentData(flameBurstEntity, skillBurstDamageCal);
                                //设置总体存活时间， 设置爆发时间，应该是到了爆发时间动态扩大烈焰爆发的技能检测范围
                                _entityManager.AddComponentData(flameBurstEntity, new SkillFlameBurstTag() { tagSurvivalTime = 0.5f, level = preLevel });
                            }
                            break;
                        case HeroSkillPsionicType.PsionicAC:
                            if (runTimeHeroCmp.defenseAttribute.energy > 40)
                            {
                                int preLevel = 10;
                                runTimeHeroCmp.defenseAttribute.energy -= 40;
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                var filter = new CollisionFilter
                                {
                                    //属于道具层
                                    BelongsTo = 1u << 10,
                                    //检测敌人
                                    CollidesWith = 1u << 6,
                                    GroupIndex = 0
                                };
                                var overlap = new OverlapBurstQueryCenter { center = Hero.instance.transform.position, radius = 12f, filter = filter, offset = new float3(0, 0, 0), shape = OverLapShape.Sphere };
                                var flameBurstEntity = DamageSkillsBrustProp(_skillPrefabs.HeroSkill_FlameBurst, overlap, Hero.instance.transform.position, Hero.instance.transform.rotation,
                                1, float3.zero, float3.zero, 1, false, true);
                                var skillBurstDamageCal = _entityManager.GetComponentData<SkillsBurstDamageCalPar>(flameBurstEntity);

                                //新增爆燃冲击A的压制逻辑
                                var tempCount = Hero.instance.skillAttackPar.flameBurstRelasecount += 1;
                                if (tempCount >= (11 - preLevel * 0.5f))
                                {
                                    DevDebug.LogError("压制");
                                    skillBurstDamageCal = Hero.instance.CalculateBurstSkillDamage(0, 0, 1);//产生压制
                                    Hero.instance.skillAttackPar.flameBurstRelasecount = 0;//清零计数器
                                }
                                //C阶段的幸运判定
                                var enbaleSup = UnityEngine.Random.Range(0, 1f) <= (0.25 + preLevel * 0.015) * 0.5f * runTimeHeroCmp.attackAttribute.luckyStrikeChance;
                                if (enbaleSup)
                                {
                                    DevDebug.LogError("压制");
                                    skillBurstDamageCal = Hero.instance.CalculateBurstSkillDamage(0, 0, 1);//产生压制
                                                                                                           // Hero.instance.skillAttackPar.flameBurstRelasecount = 0;//清零计数器
                                }
                                skillBurstDamageCal.tempknockback = 300;//附带300击退值
                                _entityManager.SetComponentData(flameBurstEntity, skillBurstDamageCal);
                                //设置总体存活时间， 设置爆发时间，应该是到了爆发时间动态扩大烈焰爆发的技能检测范围
                                _entityManager.AddComponentData(flameBurstEntity, new SkillFlameBurstTag() { tagSurvivalTime = 0.5f, level = preLevel });

                            }

                            break;
                        case HeroSkillPsionicType.PsionicBC:
                            _entityManager.SetComponentEnabled<SkillFlameBurst_Hero>(_heroEntity, true);//开启烈焰爆发B阶段加载标签
                            if (runTimeHeroCmp.defenseAttribute.energy > 40)
                            {
                                int preLevel = 10;

                                runTimeHeroCmp.defenseAttribute.energy -= 40;
                                //烈焰爆发独立增伤
                                if (runTimeHeroCmp.attackAttribute.heroDynamicalAttack.tempFlameBurstBDamagePar < (8 * (0.15f + 0.01f * preLevel)))
                                    runTimeHeroCmp.attackAttribute.heroDynamicalAttack.tempFlameBurstBDamagePar += 0.15f + 0.01f * preLevel;
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                var flameBurstStength = _entityManager.GetComponentData<SkillFlameBurst_Hero>(_heroEntity);
                                flameBurstStength.tagSurvivalTime = 6;//重置6秒的加成时间
                                _entityManager.SetComponentData(_heroEntity, flameBurstStength);//写回-由外部检测加成时间                              
                                var filter = new CollisionFilter
                                {
                                    //属于道具层
                                    BelongsTo = 1u << 10,
                                    //检测敌人
                                    CollidesWith = 1u << 6,
                                    GroupIndex = 0
                                };
                                var overlap = new OverlapBurstQueryCenter { center = Hero.instance.transform.position, radius = 12f, filter = filter, offset = new float3(0, 0, 0), shape = OverLapShape.Sphere };
                                var flameBurstEntity = DamageSkillsBrustProp(_skillPrefabs.HeroSkill_FlameBurst, overlap, Hero.instance.transform.position, Hero.instance.transform.rotation,
                                1, float3.zero, float3.zero, 1, false, true);
                                var skillBurstDamageCal = _entityManager.GetComponentData<SkillsBurstDamageCalPar>(flameBurstEntity);
                                //C阶段的幸运判定
                                var enbaleSup = UnityEngine.Random.Range(0, 1f) <= (0.25 + preLevel * 0.015) * 0.5f * runTimeHeroCmp.attackAttribute.luckyStrikeChance;
                                if (enbaleSup)
                                {
                                    DevDebug.LogError("压制");
                                    skillBurstDamageCal = Hero.instance.CalculateBurstSkillDamage(0, 0, 1);//产生压制
                                                                                                           // Hero.instance.skillAttackPar.flameBurstRelasecount = 0;//清零计数器
                                }
                                skillBurstDamageCal.tempknockback = 300;//附带300击退值
                                _entityManager.SetComponentData(flameBurstEntity, skillBurstDamageCal);
                                //设置总体存活时间， 设置爆发时间，应该是到了爆发时间动态扩大烈焰爆发的技能检测范围
                                _entityManager.AddComponentData(flameBurstEntity, new SkillFlameBurstTag() { tagSurvivalTime = 0.5f, level = preLevel });
                            }

                            break;
                        case HeroSkillPsionicType.PsionicABC:
                            _entityManager.SetComponentEnabled<SkillFlameBurst_Hero>(_heroEntity, true);//开启烈焰爆发B阶段加载标签
                            if (runTimeHeroCmp.defenseAttribute.energy > 40)
                            {
                                int preLevel = 10;

                                runTimeHeroCmp.defenseAttribute.energy -= 40;
                                //烈焰爆发独立增伤
                                if (runTimeHeroCmp.attackAttribute.heroDynamicalAttack.tempFlameBurstBDamagePar < (8 * (0.15f + 0.01f * preLevel)))
                                    runTimeHeroCmp.attackAttribute.heroDynamicalAttack.tempFlameBurstBDamagePar += 0.15f + 0.01f * preLevel;
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                var flameBurstStength = _entityManager.GetComponentData<SkillFlameBurst_Hero>(_heroEntity);
                                flameBurstStength.tagSurvivalTime = 6;//重置6秒的加成时间
                                _entityManager.SetComponentData(_heroEntity, flameBurstStength);//写回-由外部检测加成时间                              
                                var filter = new CollisionFilter
                                {
                                    //属于道具层
                                    BelongsTo = 1u << 10,
                                    //检测敌人
                                    CollidesWith = 1u << 6,
                                    GroupIndex = 0
                                };
                                var overlap = new OverlapBurstQueryCenter { center = Hero.instance.transform.position, radius = 12f, filter = filter, offset = new float3(0, 0, 0), shape = OverLapShape.Sphere };
                                var flameBurstEntity = DamageSkillsBrustProp(_skillPrefabs.HeroSkill_FlameBurst, overlap, Hero.instance.transform.position, Hero.instance.transform.rotation,
                                1, float3.zero, float3.zero, 1, false, true);
                                var skillBurstDamageCal = _entityManager.GetComponentData<SkillsBurstDamageCalPar>(flameBurstEntity);
                                //新增爆燃冲击A的压制逻辑
                                var tempCount = Hero.instance.skillAttackPar.flameBurstRelasecount += 1;
                                if (tempCount >= (11 - preLevel * 0.5f))
                                {
                                    DevDebug.LogError("压制");
                                    skillBurstDamageCal = Hero.instance.CalculateBurstSkillDamage(0, 0, 1);//产生压制
                                    Hero.instance.skillAttackPar.flameBurstRelasecount = 0;//清零计数器
                                }
                                //C阶段的幸运判定
                                var enbaleSup = UnityEngine.Random.Range(0, 1f) <= (0.25 + preLevel * 0.015) * 0.5f * runTimeHeroCmp.attackAttribute.luckyStrikeChance;
                                if (enbaleSup)
                                {
                                    DevDebug.LogError("压制");
                                    skillBurstDamageCal = Hero.instance.CalculateBurstSkillDamage(0, 0, 1);//产生压制
                                                                                                           // Hero.instance.skillAttackPar.flameBurstRelasecount = 0;//清零计数器
                                }
                                skillBurstDamageCal.tempknockback = 300;//附带300击退值
                                _entityManager.SetComponentData(flameBurstEntity, skillBurstDamageCal);
                                //设置总体存活时间， 设置爆发时间，应该是到了爆发时间动态扩大烈焰爆发的技能检测范围
                                _entityManager.AddComponentData(flameBurstEntity, new SkillFlameBurstTag() { tagSurvivalTime = 0.5f, level = preLevel });
                            }
                            break;
                    }
                    break;
                //闪电链 30， 瞬时寻址
                case HeroSkillID.LightningChain:
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:
                            var filter = new CollisionFilter
                            {
                                //属于道具层
                                BelongsTo = 1u << 10,
                                //检测敌人
                                CollidesWith = 1u << 6,
                                GroupIndex = 0
                            };
                            var overlap = new OverlapTrackingQueryCenter { center = Hero.instance.skillTargetPositon, radius = 10, filter = filter, offset = new float3(0, 0, 0), shape = OverLapShape.Sphere };
                            //这里添加寻踪类技能专属标签
                            var entityLightningChain = DamageSkillsTrackingPropNoneDamage(_skillPrefabs.HeroSkill_LightningChain, overlap, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1.0f, new float3(0, 3.0f, 0), 0, 1, false, false);
                            //添加通用侦察器
                            _entityManager.AddComponentData(entityLightningChain, new SkillLightningChainTag() { tagSurvivalTime = 0.5f, laterTagSurvivalTime = 1f, speed = 20, targetPostion = Hero.instance.skillTargetPositon });
                            //寻址技能参数变化配置
                            var skillsTrackingCalPar = _entityManager.GetComponentData<SkillsTrackingCalPar>(entityLightningChain);
                            skillsTrackingCalPar.runCount = 3;//默认弹射三次， 生成 skillsDamageCalPar的 伤害碰撞检测体，透明，用于闪电链的定点检测
                            _entityManager.SetComponentData(entityLightningChain, skillsTrackingCalPar);


                            for (int i = 0; i < skillsTrackingCalPar.runCount; i++)
                            {
                                //默认放在原始 siglotenPrefab 的位置
                                var lightningChainColliderEntity = _entityManager.Instantiate(_skillPrefabs.HeroSkillAssistive_LightningChainCollider);
                                _entityManager.AddComponentData(lightningChainColliderEntity, Hero.instance.skillsDamageCalPar);
                                var trs = _entityManager.GetComponentData<LocalTransform>(lightningChainColliderEntity);
                                trs.Position.y = -100;
                                _entityManager.SetComponentData(lightningChainColliderEntity, trs);

                                var skillPar = _entityManager.GetComponentData<SkillsDamageCalPar>(lightningChainColliderEntity);
                                skillPar.enablePull = false;
                                skillPar.enableExplosion = false;
                                skillPar.damageChangePar -= skillsTrackingCalPar.runCount * 0.1f; //原始伤害递减10% 
                                //写回伤害递减  
                                _entityManager.SetComponentData(lightningChainColliderEntity, skillPar);
                                //添加碰撞记录
                                var hits = _entityManager.AddBuffer<HitRecord>(lightningChainColliderEntity);
                                _entityManager.AddBuffer<HitElementResonanceRecord>(lightningChainColliderEntity);

                                var skillLightningChianTag = _entityManager.GetComponentData<SkillLightningChainTag>(entityLightningChain);
                                var refSkillTag = SetLightningChainCollider(skillLightningChianTag, i, lightningChainColliderEntity);
                                _entityManager.SetComponentData(entityLightningChain, refSkillTag);
                                //添加碰撞器控制标识
                                _entityManager.AddComponentData(lightningChainColliderEntity, new skillLightningChianColliderTag() { tagSurvivalTime = 1 });
                                _entityManager.SetComponentEnabled<skillLightningChianColliderTag>(lightningChainColliderEntity, false);

                            }

                            break;
                        case HeroSkillPsionicType.PsionicA:
                            var filterA = new CollisionFilter
                            {
                                //属于道具层
                                BelongsTo = 1u << 10,
                                //检测敌人
                                CollidesWith = 1u << 6,
                                GroupIndex = 0
                            };
                            var overlapA = new OverlapTrackingQueryCenter { center = Hero.instance.skillTargetPositon, radius = 10, filter = filterA, offset = new float3(0, 0, 0), shape = OverLapShape.Sphere };
                            //这里添加寻踪类技能专属标签
                            var entityLightningChainA = DamageSkillsTrackingPropNoneDamage(_skillPrefabs.HeroSkill_LightningChain, overlapA, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1.0f, new float3(0, 3.0f, 0), 0, 1, false, false);
                            //添加通用侦察器
                            _entityManager.AddComponentData(entityLightningChainA, new SkillLightningChainTag() { tagSurvivalTime = 0.5f, laterTagSurvivalTime = 3f, speed = 20, targetPostion = Hero.instance.skillTargetPositon, enableSecondA = true });
                            //寻址技能参数变化配置
                            var skillsTrackingCalParA = _entityManager.GetComponentData<SkillsTrackingCalPar>(entityLightningChainA);
                            skillsTrackingCalPar.runCount = 3;//默认弹射三次， 生成 skillsDamageCalPar的 伤害碰撞检测体，透明，用于闪电链的定点检测
                            _entityManager.SetComponentData(entityLightningChainA, skillsTrackingCalParA);


                            for (int i = 0; i < skillsTrackingCalPar.runCount; i++)
                            {
                                //默认放在原始 siglotenPrefab 的位置
                                var lightningChainColliderEntity = _entityManager.Instantiate(_skillPrefabs.HeroSkillAssistive_LightningChainCollider);
                                _entityManager.AddComponentData(lightningChainColliderEntity, Hero.instance.skillsDamageCalPar);
                                var trs = _entityManager.GetComponentData<LocalTransform>(lightningChainColliderEntity);
                                trs.Position.y = -100;
                                _entityManager.SetComponentData(lightningChainColliderEntity, trs);

                                var skillPar = _entityManager.GetComponentData<SkillsDamageCalPar>(lightningChainColliderEntity);
                                skillPar.enablePull = false;
                                skillPar.enableExplosion = false;
                                skillPar.damageChangePar -= (skillsTrackingCalPar.runCount * 0.1f * 1.3f); //原始伤害递减10% ,增加伤害30%，随等级提升
                                //写回伤害递减  
                                _entityManager.SetComponentData(lightningChainColliderEntity, skillPar);
                                //添加碰撞记录
                                var hits = _entityManager.AddBuffer<HitRecord>(lightningChainColliderEntity);
                                _entityManager.AddBuffer<HitElementResonanceRecord>(lightningChainColliderEntity);

                                var skillLightningChianTag = _entityManager.GetComponentData<SkillLightningChainTag>(entityLightningChainA);
                                var refSkillTag = SetLightningChainCollider(skillLightningChianTag, i, lightningChainColliderEntity);
                                _entityManager.SetComponentData(entityLightningChainA, refSkillTag);
                                //添加碰撞器控制标识
                                _entityManager.AddComponentData(lightningChainColliderEntity, new skillLightningChianColliderTag() { tagSurvivalTime = 3 });
                                _entityManager.SetComponentEnabled<skillLightningChianColliderTag>(lightningChainColliderEntity, false);

                            }
                            break;
                        case HeroSkillPsionicType.PsionicB:
                            var filterB = new CollisionFilter
                            {
                                //属于道具层
                                BelongsTo = 1u << 10,
                                //检测敌人
                                CollidesWith = 1u << 6,
                                GroupIndex = 0
                            };
                            var overlapB = new OverlapTrackingQueryCenter { center = Hero.instance.skillTargetPositon, radius = 10, filter = filterB, offset = new float3(0, 0, 0), shape = OverLapShape.Sphere };
                            //这里添加寻踪类技能专属标签
                            var entityLightningChainB = DamageSkillsTrackingPropNoneDamage(_skillPrefabs.HeroSkill_LightningChain, overlapB, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1.0f, new float3(0, 3.0f, 0), 0, 1, false, false);
                            //添加通用侦察器
                            _entityManager.AddComponentData(entityLightningChainB, new SkillLightningChainTag() { tagSurvivalTime = 0.5f, laterTagSurvivalTime = 1f, speed = 20, targetPostion = Hero.instance.skillTargetPositon, enableSecondB = true });
                            //寻址技能参数变化配置
                            var skillsTrackingCalParB = _entityManager.GetComponentData<SkillsTrackingCalPar>(entityLightningChainB);
                            skillsTrackingCalPar.runCount = 3;//默认弹射三次， 生成 skillsDamageCalPar的 伤害碰撞检测体，透明，用于闪电链的定点检测
                            _entityManager.SetComponentData(entityLightningChainB, skillsTrackingCalParB);
                            for (int i = 0; i < skillsTrackingCalPar.runCount; i++)
                            {
                                //默认放在原始 siglotenPrefab 的位置
                                var lightningChainColliderEntity = _entityManager.Instantiate(_skillPrefabs.HeroSkillAssistive_LightningChainCollider);
                                _entityManager.AddComponentData(lightningChainColliderEntity, Hero.instance.skillsDamageCalPar);
                                var trs = _entityManager.GetComponentData<LocalTransform>(lightningChainColliderEntity);
                                trs.Position.y = -100;
                                trs.Scale = 1.5f;//增大导电体范围，随等级提升
                                _entityManager.SetComponentData(lightningChainColliderEntity, trs);

                                var skillPar = _entityManager.GetComponentData<SkillsDamageCalPar>(lightningChainColliderEntity);
                                skillPar.enablePull = false;
                                skillPar.enableExplosion = false;
                                skillPar.damageChangePar -= (skillsTrackingCalPar.runCount * 0.1f * 1.3f); //原始伤害递减10% ,增加伤害30%，随等级提升    
                                //写回伤害递减  
                                _entityManager.SetComponentData(lightningChainColliderEntity, skillPar);
                                //添加碰撞记录
                                var hits = _entityManager.AddBuffer<HitRecord>(lightningChainColliderEntity);
                                _entityManager.AddBuffer<HitElementResonanceRecord>(lightningChainColliderEntity);

                                var skillLightningChianTag = _entityManager.GetComponentData<SkillLightningChainTag>(entityLightningChainB);
                                var refSkillTag = SetLightningChainCollider(skillLightningChianTag, i, lightningChainColliderEntity);
                                _entityManager.SetComponentData(entityLightningChainB, refSkillTag);
                                //添加碰撞器控制标识
                                _entityManager.AddComponentData(lightningChainColliderEntity, new skillLightningChianColliderTag() { tagSurvivalTime = 1 });
                                _entityManager.SetComponentEnabled<skillLightningChianColliderTag>(lightningChainColliderEntity, false);
                            }
                            break;
                        case HeroSkillPsionicType.PsionicAB:
                            var filterAB = new CollisionFilter
                            {
                                //属于道具层
                                BelongsTo = 1u << 10,
                                //检测敌人
                                CollidesWith = 1u << 6,
                                GroupIndex = 0
                            };
                            var overlapAB = new OverlapTrackingQueryCenter { center = Hero.instance.skillTargetPositon, radius = 10, filter = filterAB, offset = new float3(0, 0, 0), shape = OverLapShape.Sphere };
                            //这里添加寻踪类技能专属标签
                            var entityLightningChainAB = DamageSkillsTrackingPropNoneDamage(_skillPrefabs.HeroSkill_LightningChain, overlapAB, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1.0f, new float3(0, 3.0f, 0), 0, 1, false, false);
                            //添加通用侦察器
                            _entityManager.AddComponentData(entityLightningChainAB, new SkillLightningChainTag() { tagSurvivalTime = 0.5f, laterTagSurvivalTime = 3f, speed = 20, targetPostion = Hero.instance.skillTargetPositon, enableSecondA = true, enableSecondB = true });
                            //寻址技能参数变化配置
                            var skillsTrackingCalParAB = _entityManager.GetComponentData<SkillsTrackingCalPar>(entityLightningChainAB);
                            skillsTrackingCalPar.runCount = 7;//默认弹射三次， 生成 skillsDamageCalPar的 伤害碰撞检测体，透明，用于闪电链的定点检测
                            _entityManager.SetComponentData(entityLightningChainAB, skillsTrackingCalParAB);
                            for (int i = 0; i < skillsTrackingCalPar.runCount; i++)
                            {
                                //默认放在原始 siglotenPrefab 的位置
                                var lightningChainColliderEntity = _entityManager.Instantiate(_skillPrefabs.HeroSkillAssistive_LightningChainCollider);
                                _entityManager.AddComponentData(lightningChainColliderEntity, Hero.instance.skillsDamageCalPar);
                                var trs = _entityManager.GetComponentData<LocalTransform>(lightningChainColliderEntity);
                                trs.Position.y = -100;
                                trs.Scale = 1.5f;
                                _entityManager.SetComponentData(lightningChainColliderEntity, trs);

                                var skillPar = _entityManager.GetComponentData<SkillsDamageCalPar>(lightningChainColliderEntity);
                                skillPar.enablePull = false;
                                skillPar.enableExplosion = false;
                                skillPar.damageChangePar -= (skillsTrackingCalPar.runCount * 0.1f * 1.6f); //原始伤害递减10% ,增加伤害30%，分别随等级提升        
                                //写回伤害递减  
                                _entityManager.SetComponentData(lightningChainColliderEntity, skillPar);
                                //添加碰撞记录
                                var hits = _entityManager.AddBuffer<HitRecord>(lightningChainColliderEntity);
                                _entityManager.AddBuffer<HitElementResonanceRecord>(lightningChainColliderEntity);

                                var skillLightningChianTag = _entityManager.GetComponentData<SkillLightningChainTag>(entityLightningChainAB);
                                var refSkillTag = SetLightningChainCollider(skillLightningChianTag, i, lightningChainColliderEntity);
                                _entityManager.SetComponentData(entityLightningChainAB, refSkillTag);
                                //添加碰撞器控制标识
                                _entityManager.AddComponentData(lightningChainColliderEntity, new skillLightningChianColliderTag() { tagSurvivalTime = 3 });
                                _entityManager.SetComponentEnabled<skillLightningChianColliderTag>(lightningChainColliderEntity, false);
                            }
                            break;


                    }
                    break;
                //暗影之刺  31 瞬时，分裂
                case HeroSkillID.ShadowStab:

                    switch (psionicType)
                    {   //能量消耗等， 都可以进行配置
                        case HeroSkillPsionicType.Basic:
                            if (runTimeHeroCmp.defenseAttribute.energy > 40)
                            {
                                runTimeHeroCmp.defenseAttribute.energy -= 40;
                                var entityShadowStab = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ShadowStab, Hero.instance.transform.position, Hero.instance.transform.rotation, 1.0f, new float3(0, 0.0f, 0), 0, 1, false, false);
                                _entityManager.AddComponentData(entityShadowStab, new SkillShadowStabTag() { tagSurvivalTime = 4f, speed = 20, skillDamageChangeParTag = 1f });
                                //取出攻击参数，写回击退值
                                var skillCal = _entityManager.GetComponentData<SkillsDamageCalPar>(entityShadowStab);
                                skillCal.tempknockback = 100;
                                _entityManager.SetComponentData(entityShadowStab, skillCal);
                            }

                            break;
                        case HeroSkillPsionicType.PsionicA:
                            if (runTimeHeroCmp.defenseAttribute.energy > 40)
                            {
                                runTimeHeroCmp.defenseAttribute.energy -= 40;
                                int level = 10;
                                var entityShadowStab = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ShadowStab, Hero.instance.transform.position, Hero.instance.transform.rotation, 1.0f, new float3(0, 0.0f, 0), 0, 1, false, false);
                                _entityManager.AddComponentData(entityShadowStab, new SkillShadowStabTag() { tagSurvivalTime = 4f, speed = 20, enableSecondA = true, secondAChance = 0.5f + (level * 0.05f), skillDamageChangeParTag = 1f });
                                //取出攻击参数，写回击退值
                                var skillCal = _entityManager.GetComponentData<SkillsDamageCalPar>(entityShadowStab);
                                skillCal.tempknockback = 100;
                                _entityManager.SetComponentData(entityShadowStab, skillCal);
                            }
                            break;
                        case HeroSkillPsionicType.PsionicB:
                            if (runTimeHeroCmp.defenseAttribute.energy > 40)
                            {
                                int level = 10;
                                runTimeHeroCmp.defenseAttribute.energy -= 40;
                                var entityShadowStab = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ShadowStab, Hero.instance.transform.position, Hero.instance.transform.rotation, 1.3f, new float3(0, 0.0f, 0), 0, 1, false, false);
                                _entityManager.AddComponentData(entityShadowStab, new SkillShadowStabTag() { tagSurvivalTime = 4f, speed = 20, enableSecondB = true, skillDamageChangeParTag = 0.5f + (level * 0.02f) });
                                //取出攻击参数，写回击退值
                                var skillCal = _entityManager.GetComponentData<SkillsDamageCalPar>(entityShadowStab);
                                skillCal.tempknockback = 100;
                                _entityManager.SetComponentData(entityShadowStab, skillCal);
                            }
                            break;
                        case HeroSkillPsionicType.PsionicAB:
                            if (runTimeHeroCmp.defenseAttribute.energy > 40)
                            {
                                int level = 10;
                                runTimeHeroCmp.defenseAttribute.energy -= 40;
                                var entityShadowStab = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ShadowStab, Hero.instance.transform.position, Hero.instance.transform.rotation, 1.3f, new float3(0, 0.0f, 0), 0, 1, false, false);
                                _entityManager.AddComponentData(entityShadowStab, new SkillShadowStabTag() { tagSurvivalTime = 4f, speed = 20, enableSecondA = true, skillDamageChangeParTag = 0.5f + (level * 0.02f), enableSecondB = true, secondAChance = 0.5f + (level * 0.05f) });
                                //取出攻击参数，写回击退值
                                var skillCal = _entityManager.GetComponentData<SkillsDamageCalPar>(entityShadowStab);
                                skillCal.tempknockback = 100;
                                _entityManager.SetComponentData(entityShadowStab, skillCal);
                            }
                            break;
                    }
                    break;
                //毒雨 32 ,持续,技能附带的控制参数， 可以通过配置表进行配置
                case HeroSkillID.PoisonRain:
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:
                            var filter = new CollisionFilter
                            {
                                BelongsTo = 1u << 10,
                                CollidesWith = 1u << 6,
                                GroupIndex = 0
                            };
                            var overlap = new OverlapOverTimeQueryCenter { center = Hero.instance.skillTargetPositon, radius = 30, filter = filter, offset = new float3(0, 0, 0) };
                            var entityPoisonRain = DamageSkillsOverTimeProp(_skillPrefabs.HeroSkill_PoisonRain, overlap, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            _entityManager.AddComponentData(entityPoisonRain, new SkillPoisonRainTag { tagSurvivalTime = 15, level = 1 });
                            var skillPar = _entityManager.GetComponentData<SkillsOverTimeDamageCalPar>(entityPoisonRain);
                            skillPar.tempSlow = 30;
                            _entityManager.SetComponentData(entityPoisonRain, skillPar);
                            break;
                        case HeroSkillPsionicType.PsionicA:
                            var filterA = new CollisionFilter
                            {
                                BelongsTo = 1u << 10,
                                CollidesWith = 1u << 6,
                                GroupIndex = 0
                            };
                            var overlapA = new OverlapOverTimeQueryCenter { center = Hero.instance.skillTargetPositon, radius = 30, filter = filterA, offset = new float3(0, 0, 0) };
                            var entityPoisonRainA = DamageSkillsOverTimeProp(_skillPrefabs.HeroSkill_PoisonRain, overlapA, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            _entityManager.AddComponentData(entityPoisonRainA, new SkillPoisonRainTag { tagSurvivalTime = 15, level = 1 });
                            var skillParA = _entityManager.GetComponentData<SkillsOverTimeDamageCalPar>(entityPoisonRainA);
                            skillParA.tempSlow = 30;
                            _entityManager.SetComponentData(entityPoisonRainA, skillParA);
                            //添加A阶段标签，用于收集判断，非buffer的处理结构？或用于持续性计算
                            _entityManager.AddComponentData(entityPoisonRainA, new SkillPoisonRainATag { level = 1 });
                            break;
                        //进行 B技能触发，火焰效果
                        case HeroSkillPsionicType.PsionicB:
                            var filterB = new CollisionFilter
                            {
                                BelongsTo = 1u << 10,
                                CollidesWith = 1u << 6,
                                GroupIndex = 0
                            };
                            var overlapB = new OverlapOverTimeQueryCenter { center = Hero.instance.skillTargetPositon, radius = 30, filter = filterB, offset = new float3(0, 0, 0) };
                            var entityPoisonRainB = DamageSkillsOverTimeProp(_skillPrefabs.HeroSkill_PoisonRain, overlapB, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            _entityManager.AddComponentData(entityPoisonRainB, new SkillPoisonRainTag { tagSurvivalTime = 15, level = 1 });
                            int level = 3;
                            var skillParB = _entityManager.GetComponentData<SkillsOverTimeDamageCalPar>(entityPoisonRainB);
                            skillParB.tempSlow = 30;
                            //添加昏迷值
                            skillParB.tempStun = 200;
                            //添加火焰参数
                            skillParB.fireDamage += skillParB.poisonDamage * (1 + level * 0.2f);
                            skillParB.fireDotDamage += skillParB.poisonDotDamage * (1 + level * 0.2f);
                            _entityManager.SetComponentData(entityPoisonRainB, skillParB);

                            //--火焰雨,仅仅增加一个效果，无实际计算
                            var entityPoisonRainBFire = DamageSkillsOverTimeProp(_skillPrefabs.HeroSkillAssistive_PoisonRainB, new OverlapOverTimeQueryCenter(), Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            _entityManager.AddComponentData(entityPoisonRainBFire, new SkillPoisonRainTag { tagSurvivalTime = 15, level = 1 });

                            break;
                        //进行 B技能触发，混合终极效果
                        case HeroSkillPsionicType.PsionicAB:
                            var filterAB = new CollisionFilter
                            {
                                BelongsTo = 1u << 10,
                                CollidesWith = 1u << 6,
                                GroupIndex = 0
                            };
                            var overlapAB = new OverlapOverTimeQueryCenter { center = Hero.instance.skillTargetPositon, radius = 30, filter = filterAB, offset = new float3(0, 0, 0) };
                            var entityPoisonRainAB = DamageSkillsOverTimeProp(_skillPrefabs.HeroSkill_PoisonRain, overlapAB, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            _entityManager.AddComponentData(entityPoisonRainAB, new SkillPoisonRainTag { tagSurvivalTime = 15, level = 1 });
                            //添加A阶段标签，用于收集判断，非buffer的处理结构？或用于持续性计算
                            _entityManager.AddComponentData(entityPoisonRainAB, new SkillPoisonRainATag { level = 1 });
                            int levelAB = 3;
                            var skillParAB = _entityManager.GetComponentData<SkillsOverTimeDamageCalPar>(entityPoisonRainAB);
                            skillParAB.tempSlow = 30;
                            //添加昏迷值
                            skillParAB.tempStun = 200;
                            //添加火焰参数
                            skillParAB.fireDamage += skillParAB.poisonDamage * (1 + levelAB * 0.2f);
                            skillParAB.fireDotDamage += skillParAB.poisonDotDamage * (1 + levelAB * 0.2f);
                            _entityManager.SetComponentData(entityPoisonRainAB, skillParAB);

                            //--火焰雨,仅仅增加一个效果，无实际计算
                            var entityPoisonRainABFire = DamageSkillsOverTimeProp(_skillPrefabs.HeroSkillAssistive_PoisonRainB, new OverlapOverTimeQueryCenter(), Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            _entityManager.AddComponentData(entityPoisonRainABFire, new SkillPoisonRainTag { tagSurvivalTime = 15, level = 1 });

                            break;

                    }

                    break;
                //元素爆发33 瞬发,爆发， 这里走元素爆发类技能
                case HeroSkillID.ElementBurst:
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:

                            var filter = new CollisionFilter
                            {
                                //属于道具层
                                BelongsTo = 1u << 10,
                                //检测敌人
                                CollidesWith = 1u << 6,
                                GroupIndex = 0
                            };
                            var overlap = new OverlapBurstQueryCenter { center = Hero.instance.skillTargetPositon, radius = 8, filter = filter, offset = new float3(0, 0, 0), shape = OverLapShape.Sphere };
                            var elementBurstEntity = DamageSkillsBrustProp(_skillPrefabs.HeroSkill_ElementBurst, overlap, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation,
                            1, float3.zero, float3.zero, 1, false, true);
                            var skillBurstDamageCal = _entityManager.GetComponentData<SkillsBurstDamageCalPar>(elementBurstEntity);
                            skillBurstDamageCal.tempExplosion = 200;
                            _entityManager.SetComponentData(elementBurstEntity, skillBurstDamageCal);
                            //设置总体存活时间， 设置爆炸时间
                            _entityManager.AddComponentData(elementBurstEntity, new SkillElementBurstTag() { tagSurvivalTime = 1.5f, startBurstTime = 0.5f });
                            //元素伤害由配置文件阶段读取并且配置

                            break;
                        //减少范围 降低伤害，可以享受范围值的加成
                        case HeroSkillPsionicType.PsionicA:

                            var filterA = new CollisionFilter
                            {
                                //属于道具层
                                BelongsTo = 1u << 10,
                                //检测敌人
                                CollidesWith = 1u << 6,
                                GroupIndex = 0
                            };
                            //检测体积 范围随等级成长
                            var overlapA = new OverlapBurstQueryCenter { center = Hero.instance.skillTargetPositon, radius = 4, filter = filterA, offset = new float3(0, 0, 0), shape = OverLapShape.Sphere };
                            //特效体积 范围随等级成长
                            var elementBurstEntityA = DamageSkillsBrustProp(_skillPrefabs.HeroSkill_ElementBurst, overlapA, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation,
                            1, float3.zero, float3.zero, 0.5f, false, true);
                            var skillBurstDamageCalA = _entityManager.GetComponentData<SkillsBurstDamageCalPar>(elementBurstEntityA);
                            skillBurstDamageCalA.tempExplosion = 200;
                            skillBurstDamageCalA.damageChangePar += 5;//随等级成长
                            _entityManager.SetComponentData(elementBurstEntityA, skillBurstDamageCalA);
                            //设置总体存活时间， 设置爆炸时间
                            _entityManager.AddComponentData(elementBurstEntityA, new SkillElementBurstTag() { tagSurvivalTime = 1.5f, startBurstTime = 0.5f });


                            break;
                        //元素爆发第二阶段
                        case HeroSkillPsionicType.PsionicB:
                            var filterB = new CollisionFilter
                            {
                                //属于道具层
                                BelongsTo = 1u << 10,
                                //检测敌人
                                CollidesWith = 1u << 6,
                                GroupIndex = 0
                            };
                            //检测体积 范围随等级成长
                            var overlapB = new OverlapBurstQueryCenter { center = Hero.instance.skillTargetPositon, radius = 8, filter = filterB, offset = new float3(0, 0, 0), shape = OverLapShape.Sphere };
                            //特效体积 范围随等级成长
                            var elementBurstEntityB = DamageSkillsBrustProp(_skillPrefabs.HeroSkill_ElementBurst, overlapB, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation,
                            1, float3.zero, float3.zero, 1f, false, true);
                            var skillBurstDamageCalB = _entityManager.GetComponentData<SkillsBurstDamageCalPar>(elementBurstEntityB);
                            skillBurstDamageCalB.tempExplosion = 200;
                            var skillBurstDamageCalBProcess = DealDamageOfElementBurstB(skillBurstDamageCalB, true);
                            _entityManager.SetComponentData(elementBurstEntityB, skillBurstDamageCalBProcess);
                            //设置总体存活时间， 设置爆炸时间
                            _entityManager.AddComponentData(elementBurstEntityB, new SkillElementBurstTag() { tagSurvivalTime = 1.5f, startBurstTime = 0.5f, enableSecondB = true });
                            break;
                        //元素爆发混合阶段
                        case HeroSkillPsionicType.PsionicAB:
                            var filterAB = new CollisionFilter
                            {
                                //属于道具层
                                BelongsTo = 1u << 10,
                                //检测敌人
                                CollidesWith = 1u << 6,
                                GroupIndex = 0
                            };
                            //检测体积 范围随等级成长
                            var overlapAB = new OverlapBurstQueryCenter { center = Hero.instance.skillTargetPositon, radius = 4, filter = filterAB, offset = new float3(0, 0, 0), shape = OverLapShape.Sphere };
                            //特效体积 范围随等级成长
                            var elementBurstEntityAB = DamageSkillsBrustProp(_skillPrefabs.HeroSkill_ElementBurst, overlapAB, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation,
                            1, float3.zero, float3.zero, 0.5f, false, true);
                            var skillBurstDamageCalAB = _entityManager.GetComponentData<SkillsBurstDamageCalPar>(elementBurstEntityAB);
                            skillBurstDamageCalAB.tempExplosion = 200;
                            skillBurstDamageCalAB.damageChangePar += 5;
                            var skillBurstDamageCalABProcess = DealDamageOfElementBurstB(skillBurstDamageCalAB, true);
                            _entityManager.SetComponentData(elementBurstEntityAB, skillBurstDamageCalABProcess);
                            //设置总体存活时间， 设置爆炸时间，开启池化标识
                            _entityManager.AddComponentData(elementBurstEntityAB, new SkillElementBurstTag() { tagSurvivalTime = 1.5f, startBurstTime = 0.5f, enableSecondB = true });


                            break;
                    }

                    break;
                //幻影步 34
                case HeroSkillID.PhantomStep:
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:

                            if (runTimeHeroCmp.defenseAttribute.energy > 20)
                            {
                                runTimeHeroCmp.defenseAttribute.energy -= 20;
                                var tempDeltaTime = runTimeHeroCmp.defenseAttribute.energy * 0.05f;
                                runTimeHeroCmp.defenseAttribute.energy = 0;
                                //写回能量扣减
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                //残影
                                var heroShadow = GameObject.Instantiate(_monoPrefabs[1].gameObject, Hero.instance.transform.position, Hero.instance.transform.rotation);
                                heroShadow.TryGetComponent<HeroBranchDeal>(out HeroBranchDeal component);
                                //最新的持续时间
                                component.originalSurvivalTime += tempDeltaTime;
                                //生成残影entity
                                // var heroShadowEntity = _entityManager.CreateEntity();
                                var heroShadowEntity = _entityManager.Instantiate(_skillPrefabs.HeroBrach);
                                _entityManager.AddComponentData(heroShadowEntity, new LocalTransform { Position = Hero.instance.transform.position, Rotation = Hero.instance.transform.rotation, Scale = 1 });
                                _entityManager.AddComponentData(heroShadowEntity, new HeroEntityBranchTag { });
                                //持续5秒
                                _entityManager.AddComponentData(heroShadowEntity, new SkillPhantomStepTag { tagSurvivalTime = 5 + tempDeltaTime });
                                _entityManager.AddComponentData(heroShadowEntity, Hero.instance.skillsDamageCalPar);
                                //传送
                                Hero.instance.transform.position = Hero.instance.skillTargetPositon;

                            }
                            break;
                        case HeroSkillPsionicType.PsionicA:
                            if (runTimeHeroCmp.defenseAttribute.energy > 50)
                            {
                                runTimeHeroCmp.defenseAttribute.energy -= 50;
                                var tempDeltaTime = runTimeHeroCmp.defenseAttribute.energy * 0.05f;
                                runTimeHeroCmp.defenseAttribute.energy = 0;
                                //写回能量扣减
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                //残影
                                var heroShadow = GameObject.Instantiate(_monoPrefabs[1].gameObject, Hero.instance.transform.position, Hero.instance.transform.rotation);
                                heroShadow.TryGetComponent<HeroBranchDeal>(out HeroBranchDeal component);
                                //最新的持续时间
                                component.originalSurvivalTime += tempDeltaTime;
                                //生成带碰撞体的残影
                                var heroShadowEntity = _entityManager.Instantiate(_skillPrefabs.HeroBrachWithCollider);
                                _entityManager.AddComponentData(heroShadowEntity, new LocalTransform { Position = Hero.instance.transform.position, Rotation = Hero.instance.transform.rotation, Scale = 1 });
                                _entityManager.AddComponentData(heroShadowEntity, new HeroEntityBranchTag { });
                                //持续5秒
                                _entityManager.AddComponentData(heroShadowEntity, new SkillPhantomStepTag { tagSurvivalTime = 5 + tempDeltaTime, enableSecondA = true });
                                _entityManager.AddComponentData(heroShadowEntity, Hero.instance.skillsDamageCalPar);
                                _entityManager.AddBuffer<HitRecord>(heroShadowEntity);
                                _entityManager.AddBuffer<HitElementResonanceRecord>(heroShadowEntity);
                                //传送
                                Hero.instance.transform.position = Hero.instance.skillTargetPositon;

                            }
                            break;
                        case HeroSkillPsionicType.PsionicB:
                            if (runTimeHeroCmp.defenseAttribute.energy > 50)
                            {
                                runTimeHeroCmp.defenseAttribute.energy -= 50;
                                var tempDeltaTime = runTimeHeroCmp.defenseAttribute.energy * 0.05f;
                                runTimeHeroCmp.defenseAttribute.energy = 0;
                                //写回能量扣减
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                //残影 ,这里设置残影的 初始化信息
                                var heroShadow = GameObject.Instantiate(_monoPrefabs[1].gameObject, Hero.instance.transform.position, Hero.instance.transform.rotation);
                                heroShadow.TryGetComponent<HeroBranchDeal>(out HeroBranchDeal component);
                                component.spawnChance = 0.2f;
                                //最新的持续时间
                                component.originalSurvivalTime += tempDeltaTime;
                                //生成残影entity
                                // var heroShadowEntity = _entityManager.CreateEntity();
                                var heroShadowEntity = _entityManager.Instantiate(_skillPrefabs.HeroBrach);
                                _entityManager.AddComponentData(heroShadowEntity, new LocalTransform { Position = Hero.instance.transform.position, Rotation = Hero.instance.transform.rotation, Scale = 1 });
                                _entityManager.AddComponentData(heroShadowEntity, new HeroEntityBranchTag { });
                                //持续5秒
                                _entityManager.AddComponentData(heroShadowEntity, new SkillPhantomStepTag { tagSurvivalTime = 5 + tempDeltaTime, enableSecondB = true });
                                _entityManager.AddComponentData(heroShadowEntity, Hero.instance.skillsDamageCalPar);
                                //传送
                                Hero.instance.transform.position = Hero.instance.skillTargetPositon;

                            }
                            break;
                        case HeroSkillPsionicType.PsionicC:
                            if (runTimeHeroCmp.defenseAttribute.energy > 25)
                            {
                                runTimeHeroCmp.defenseAttribute.energy -= 25;
                                var tempDeltaTime = runTimeHeroCmp.defenseAttribute.energy * 0.05f;
                                runTimeHeroCmp.defenseAttribute.energy = 0;
                                //写回能量扣减
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                //残影 ,这里设置残影的 初始化信息
                                var heroShadow = GameObject.Instantiate(_monoPrefabs[1].gameObject, Hero.instance.transform.position, Hero.instance.transform.rotation);
                                heroShadow.TryGetComponent<HeroBranchDeal>(out HeroBranchDeal component);
                                //最新的持续时间
                                component.originalSurvivalTime += tempDeltaTime;
                                //生成残影entity
                                // var heroShadowEntity = _entityManager.CreateEntity();
                                var heroShadowEntity = _entityManager.Instantiate(_skillPrefabs.HeroBrach);
                                _entityManager.AddComponentData(heroShadowEntity, new LocalTransform { Position = Hero.instance.transform.position, Rotation = Hero.instance.transform.rotation, Scale = 1 });
                                _entityManager.AddComponentData(heroShadowEntity, new HeroEntityBranchTag { });
                                //持续5秒
                                _entityManager.AddComponentData(heroShadowEntity, new SkillPhantomStepTag { tagSurvivalTime = 5 + tempDeltaTime, enableSecondC = true });
                                _entityManager.AddComponentData(heroShadowEntity, Hero.instance.skillsDamageCalPar);
                                //传送
                                Hero.instance.transform.position = Hero.instance.skillTargetPositon;

                            }
                            break;
                        case HeroSkillPsionicType.PsionicAB:

                            if (runTimeHeroCmp.defenseAttribute.energy > 50)
                            {
                                runTimeHeroCmp.defenseAttribute.energy -= 50;
                                var tempDeltaTime = runTimeHeroCmp.defenseAttribute.energy * 0.05f;
                                runTimeHeroCmp.defenseAttribute.energy = 0;
                                //写回能量扣减
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                //残影 ,这里设置残影的 初始化信息
                                var heroShadow = GameObject.Instantiate(_monoPrefabs[1].gameObject, Hero.instance.transform.position, Hero.instance.transform.rotation);
                                heroShadow.TryGetComponent<HeroBranchDeal>(out HeroBranchDeal component);
                                component.spawnChance = 0.2f;
                                component.enableSecondA = true;
                                //最新的持续时间
                                component.originalSurvivalTime += tempDeltaTime;
                                //生成残影entity
                                // var heroShadowEntity = _entityManager.CreateEntity();
                                var heroShadowEntity = _entityManager.Instantiate(_skillPrefabs.HeroBrachWithCollider);
                                _entityManager.AddComponentData(heroShadowEntity, new LocalTransform { Position = Hero.instance.transform.position, Rotation = Hero.instance.transform.rotation, Scale = 1 });
                                _entityManager.AddComponentData(heroShadowEntity, new HeroEntityBranchTag { });
                                //持续5秒
                                _entityManager.AddComponentData(heroShadowEntity, new SkillPhantomStepTag { tagSurvivalTime = 5 + tempDeltaTime, enableSecondB = true });
                                _entityManager.AddComponentData(heroShadowEntity, Hero.instance.skillsDamageCalPar);
                                //传送
                                _entityManager.AddBuffer<HitRecord>(heroShadowEntity);
                                _entityManager.AddBuffer<HitElementResonanceRecord>(heroShadowEntity);
                                Hero.instance.transform.position = Hero.instance.skillTargetPositon;

                            }

                            break;
                        case HeroSkillPsionicType.PsionicAC:
                            if (runTimeHeroCmp.defenseAttribute.energy > 20)
                            {
                                runTimeHeroCmp.defenseAttribute.energy -= 20;
                                var tempDeltaTime = runTimeHeroCmp.defenseAttribute.energy * 0.05f;
                                runTimeHeroCmp.defenseAttribute.energy = 0;
                                //写回能量扣减
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                //残影
                                var heroShadow = GameObject.Instantiate(_monoPrefabs[1].gameObject, Hero.instance.transform.position, Hero.instance.transform.rotation);
                                heroShadow.TryGetComponent<HeroBranchDeal>(out HeroBranchDeal component);
                                //最新的持续时间
                                component.originalSurvivalTime += tempDeltaTime;
                                //生成带碰撞体的残影
                                var heroShadowEntity = _entityManager.Instantiate(_skillPrefabs.HeroBrachWithCollider);
                                _entityManager.AddComponentData(heroShadowEntity, new LocalTransform { Position = Hero.instance.transform.position, Rotation = Hero.instance.transform.rotation, Scale = 1 });
                                _entityManager.AddComponentData(heroShadowEntity, new HeroEntityBranchTag { });
                                //持续5秒
                                _entityManager.AddComponentData(heroShadowEntity, new SkillPhantomStepTag { tagSurvivalTime = 5 + tempDeltaTime, enableSecondA = true, enableSecondC = true });
                                _entityManager.AddComponentData(heroShadowEntity, Hero.instance.skillsDamageCalPar);
                                _entityManager.AddBuffer<HitRecord>(heroShadowEntity);
                                _entityManager.AddBuffer<HitElementResonanceRecord>(heroShadowEntity);
                                //传送
                                Hero.instance.transform.position = Hero.instance.skillTargetPositon;

                            }

                            break;
                        case HeroSkillPsionicType.PsionicBC:
                            if (runTimeHeroCmp.defenseAttribute.energy > 20)
                            {
                                runTimeHeroCmp.defenseAttribute.energy -= 20;
                                var tempDeltaTime = runTimeHeroCmp.defenseAttribute.energy * 0.05f;
                                runTimeHeroCmp.defenseAttribute.energy = 0;
                                //写回能量扣减
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                //残影 ,这里设置残影的 初始化信息
                                var heroShadow = GameObject.Instantiate(_monoPrefabs[1].gameObject, Hero.instance.transform.position, Hero.instance.transform.rotation);
                                heroShadow.TryGetComponent<HeroBranchDeal>(out HeroBranchDeal component);
                                component.spawnChance = 0.2f;
                                component.enableSecondC = true;
                                //最新的持续时间
                                component.originalSurvivalTime += tempDeltaTime;
                                //生成残影entity
                                // var heroShadowEntity = _entityManager.CreateEntity();
                                var heroShadowEntity = _entityManager.Instantiate(_skillPrefabs.HeroBrach);
                                _entityManager.AddComponentData(heroShadowEntity, new LocalTransform { Position = Hero.instance.transform.position, Rotation = Hero.instance.transform.rotation, Scale = 1 });
                                _entityManager.AddComponentData(heroShadowEntity, new HeroEntityBranchTag { });
                                //持续5秒
                                _entityManager.AddComponentData(heroShadowEntity, new SkillPhantomStepTag { tagSurvivalTime = 5 + tempDeltaTime, enableSecondB = true, enableSecondC = true });
                                _entityManager.AddComponentData(heroShadowEntity, Hero.instance.skillsDamageCalPar);
                                //传送
                                Hero.instance.transform.position = Hero.instance.skillTargetPositon;

                            }

                            break;
                        case HeroSkillPsionicType.PsionicABC:
                            if (runTimeHeroCmp.defenseAttribute.energy > 20)
                            {
                                runTimeHeroCmp.defenseAttribute.energy -= 25;
                                var tempDeltaTime = runTimeHeroCmp.defenseAttribute.energy * 0.05f;
                                runTimeHeroCmp.defenseAttribute.energy = 0;
                                //写回能量扣减
                                _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
                                //残影 ,这里设置残影的 初始化信息
                                var heroShadow = GameObject.Instantiate(_monoPrefabs[1].gameObject, Hero.instance.transform.position, Hero.instance.transform.rotation);
                                heroShadow.TryGetComponent<HeroBranchDeal>(out HeroBranchDeal component);
                                component.spawnChance = 0.2f;
                                component.enableSecondA = true;
                                component.enableSecondC = true;
                                //最新的持续时间
                                component.originalSurvivalTime += tempDeltaTime;
                                //生成残影entity
                                // var heroShadowEntity = _entityManager.CreateEntity();
                                var heroShadowEntity = _entityManager.Instantiate(_skillPrefabs.HeroBrachWithCollider);
                                _entityManager.AddComponentData(heroShadowEntity, new LocalTransform { Position = Hero.instance.transform.position, Rotation = Hero.instance.transform.rotation, Scale = 1 });
                                _entityManager.AddComponentData(heroShadowEntity, new HeroEntityBranchTag { });
                                //持续5秒
                                _entityManager.AddComponentData(heroShadowEntity, new SkillPhantomStepTag { tagSurvivalTime = 5 + tempDeltaTime, enableSecondB = true, enableSecondA = true, enableSecondC = true });
                                _entityManager.AddComponentData(heroShadowEntity, Hero.instance.skillsDamageCalPar);
                                _entityManager.AddBuffer<HitRecord>(heroShadowEntity);
                                _entityManager.AddBuffer<HitElementResonanceRecord>(heroShadowEntity);
                                //传送
                                Hero.instance.transform.position = Hero.instance.skillTargetPositon;

                            }

                            break;
                    }
                    break;
            }
            ;
            ecb.Playback(_entityManager);
            ecb.Dispose();

        }
        /// <summary>
        /// 实例化并初始化一个伤害型飞行技能实体（Pulse,PulseB）-
        /// 支持位置、旋转、缩放偏移，以及可选的引力/斥力效果。
        /// </summary>
        /// <param name="prefab">技能预制实体。</param>
        /// <param name="positionOffset">世界空间位置偏移，默认 <c>float3.zero</c>。</param>
        /// <param name="rotationOffsetEuler">相对英雄的欧拉角旋转偏移（度），默认 <c>float3.zero</c>。</param>
        /// <param name="scaleFactor">缩放因子，默认 <c>1f</c>。</param>
        /// <param name="enablePull">是否开启引力效果，默认 <c>false</c>。</param>
        /// <param name="enableExplosion">是否开启斥力/爆炸效果，默认 <c>fasle</c>。</param>
        /// <param name="enableSecond">是否开启二阶段 <c>fasle</c>。</param>
        /// <returns>返回新实例化的实体。</returns>
        public Entity DamageSkillsFlightProp(
            Entity prefab,
            float3 posion,
            quaternion quaternion,
            float damageChangePar = 1,//默认伤害参数为1
            float3 positionOffset = default,
            float3 rotationOffsetEuler = default,  // 传入度数
            float scaleFactor = 1f, bool enablePull = false, bool enableExplosion = false)
        {
            DevDebug.Log("释放伤害型飞行技能");

            // 1) 实例化
            var entity = _entityManager.Instantiate(prefab);

            // 2) 取出可变的 LocalTransform
            var transform = _entityManager.GetComponentData<LocalTransform>(entity);


            // 3) 从英雄获取基础位置/旋转/缩放
            float3 heroPos = posion;
            quaternion heroRot = quaternion;
            float baseScale = transform.Scale; // 保留预制体的原始 scale

            // 4) 计算欧拉偏移的四元数
            //    math.radians 将度数转为弧度
            quaternion eulerOffsetQuat = quaternion.EulerXYZ(
                math.radians(rotationOffsetEuler)
            );

            // 5) 叠加偏移
            transform.Position = heroPos
                                + math.mul(heroRot, positionOffset);
            //计算整合旋转
            var combineRotation = math.mul(heroRot, eulerOffsetQuat);
            //叠加本体旋转
            transform.Rotation = math.mul(transform.Rotation, combineRotation);
            transform.Scale = baseScale * scaleFactor * (1 + _heroAttributeCmptOriginal.gainAttribute.skillRange);

            // 6) 写回组件
            _entityManager.SetComponentData(entity, transform);

            // 7) 添加伤害参数
            _entityManager.AddComponentData(entity, Hero.instance.skillsDamageCalPar);

            var skillPar = _entityManager.GetComponentData<SkillsDamageCalPar>(entity);

            skillPar.enablePull = enablePull;
            skillPar.enableExplosion = enableExplosion;
            skillPar.damageChangePar = damageChangePar;
            _entityManager.SetComponentData(entity, skillPar);

            // 8) 添加碰撞记录缓冲区
            var hits = _entityManager.AddBuffer<HitRecord>(entity);
            _entityManager.AddBuffer<HitElementResonanceRecord>(entity);

            return entity;
        }
        /// <summary>
        /// 持续性技能，取消hitRecord 的buffer遍历，且在system中进行更新，不享受快照机制（毒雨）
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="posion"></param>
        /// <param name="quaternion"></param>
        /// <param name="damageChangePar"></param>
        /// <param name="positionOffset"></param>
        /// <param name="rotationOffsetEuler"></param>
        /// <param name="scaleFactor"></param>
        /// <param name="enablePull"></param>
        /// <param name="enableExplosion"></param>
        /// <returns></returns>
        public Entity DamageSkillsOverTimeProp(
         Entity prefab,
         OverlapOverTimeQueryCenter queryCenter,
         float3 posion,
         quaternion quaternion,
         float damageChangePar = 1,//默认伤害参数为1
         float3 positionOffset = default,
         float3 rotationOffsetEuler = default,  // 传入度数
         float scaleFactor = 1f, bool enablePull = false, bool enableExplosion = false)
        {
            DevDebug.Log("释放持续性技能");

            // 1) 实例化
            var entity = _entityManager.Instantiate(prefab);

            // 2) 取出可变的 LocalTransform
            var transform = _entityManager.GetComponentData<LocalTransform>(entity);


            // 3) 从英雄获取基础位置/旋转/缩放
            float3 heroPos = posion;
            quaternion heroRot = quaternion;
            float baseScale = transform.Scale; // 保留预制体的原始 scale

            // 4) 计算欧拉偏移的四元数
            //    math.radians 将度数转为弧度
            quaternion eulerOffsetQuat = quaternion.EulerXYZ(
                math.radians(rotationOffsetEuler)
            );

            // 5) 叠加偏移
            transform.Position = heroPos
                                + math.mul(heroRot, positionOffset);
            //计算整合旋转
            var combineRotation = math.mul(heroRot, eulerOffsetQuat);
            //叠加本体旋转
            transform.Rotation = math.mul(transform.Rotation, combineRotation);
            transform.Scale = baseScale * scaleFactor * (1 + _heroAttributeCmptOriginal.gainAttribute.skillRange);

            // 6) 写回组件
            _entityManager.SetComponentData(entity, transform);

            // 7) 添加持续伤害参数
            _entityManager.AddComponentData(entity, Hero.instance.skillsOverTimeDamageCalPar);

            //8)添加持续性伤害overlap检测
            if (queryCenter.radius != 0)
                _entityManager.AddComponentData(entity, queryCenter);

            var skillPar = _entityManager.GetComponentData<SkillsOverTimeDamageCalPar>(entity);

            skillPar.enablePull = enablePull;
            skillPar.enableExplosion = enableExplosion;
            skillPar.damageChangePar = damageChangePar;
            _entityManager.SetComponentData(entity, skillPar);

            _entityManager.AddBuffer<HitElementResonanceRecord>(entity);

            return entity;
        }
        /// <summary>
        /// 爆发类技能 暗影洪流第二阶段 、 元素爆发
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="queryCenter"></param>
        /// <param name="posion"></param>
        /// <param name="quaternion"></param>
        /// <param name="damageChangePar"></param>
        /// <param name="positionOffset"></param>
        /// <param name="rotationOffsetEuler"></param>
        /// <param name="scaleFactor"></param>
        /// <param name="enablePull"></param>
        /// <param name="enableExplosion"></param>
        /// <returns></returns>
        public Entity DamageSkillsBrustProp(
       Entity prefab,
       OverlapBurstQueryCenter queryCenter,
       float3 posion,
       quaternion quaternion,
       float damageChangePar = 1,//默认伤害参数为1
       float3 positionOffset = default,
       float3 rotationOffsetEuler = default,  // 传入度数
       float scaleFactor = 1f, bool enablePull = false, bool enableExplosion = false)
        {
            DevDebug.Log("释放爆发性技能");

            // 1) 实例化
            var entity = _entityManager.Instantiate(prefab);

            // 2) 取出可变的 LocalTransform
            var transform = _entityManager.GetComponentData<LocalTransform>(entity);


            // 3) 从英雄获取基础位置/旋转/缩放
            float3 heroPos = posion;
            quaternion heroRot = quaternion;
            float baseScale = transform.Scale; // 保留预制体的原始 scale

            // 4) 计算欧拉偏移的四元数
            //    math.radians 将度数转为弧度
            quaternion eulerOffsetQuat = quaternion.EulerXYZ(
                math.radians(rotationOffsetEuler)
            );

            // 5) 叠加偏移
            transform.Position = heroPos
                                + math.mul(heroRot, positionOffset);
            //计算整合旋转
            var combineRotation = math.mul(heroRot, eulerOffsetQuat);
            //叠加本体旋转
            transform.Rotation = math.mul(transform.Rotation, combineRotation);
            transform.Scale = baseScale * scaleFactor * (1 + _heroAttributeCmptOriginal.gainAttribute.skillRange);

            // 6) 写回组件
            _entityManager.SetComponentData(entity, transform);
            //



            // 7) 添加爆发技能伤害参数
            _entityManager.AddComponentData(entity, Hero.instance.skillsBurstDamageCalPar);

            //8)添加爆发性伤害overlap检测
            if (queryCenter.radius != 0)
                _entityManager.AddComponentData(entity, queryCenter);

            var skillPar = _entityManager.GetComponentData<SkillsBurstDamageCalPar>(entity);

            skillPar.enablePull = enablePull;
            skillPar.enableExplosion = enableExplosion;
            skillPar.damageChangePar = damageChangePar;
            _entityManager.SetComponentData(entity, skillPar);
            //爆发类技能无元素共鸣
            // _entityManager.AddBuffer<HitElementResonanceRecord>(entity);

            return entity;
        }

        /// <summary>
        /// 释放 寻踪类技能
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="queryCenter"></param>
        /// <param name="posion"></param>
        /// <param name="quaternion"></param>
        /// <param name="damageChangePar"></param>
        /// <param name="positionOffset"></param>
        /// <param name="rotationOffsetEuler"></param>
        /// <param name="scaleFactor"></param>
        /// <param name="enablePull"></param>
        /// <param name="enableExplosion"></param>
        /// <returns></returns>
        public Entity DamageSkillsTrackingProp(
      Entity prefab,
      OverlapTrackingQueryCenter queryCenter,
      float3 posion,
      quaternion quaternion,
      float damageChangePar = 1,//默认伤害参数为1
      float3 positionOffset = default,
      float3 rotationOffsetEuler = default,  // 传入度数
      float scaleFactor = 1f, bool enablePull = false, bool enableExplosion = false)
        {
            DevDebug.Log("释放寻址类技能");

            // 1) 实例化
            var entity = _entityManager.Instantiate(prefab);

            // 2) 取出可变的 LocalTransform
            var transform = _entityManager.GetComponentData<LocalTransform>(entity);


            // 3) 从英雄获取基础位置/旋转/缩放
            float3 heroPos = posion;
            quaternion heroRot = quaternion;
            float baseScale = transform.Scale; // 保留预制体的原始 scale

            // 4) 计算欧拉偏移的四元数
            //    math.radians 将度数转为弧度
            quaternion eulerOffsetQuat = quaternion.EulerXYZ(
                math.radians(rotationOffsetEuler)
            );

            // 5) 叠加偏移
            transform.Position = heroPos
                                + math.mul(heroRot, positionOffset);
            //计算整合旋转
            var combineRotation = math.mul(heroRot, eulerOffsetQuat);
            //叠加本体旋转
            transform.Rotation = math.mul(transform.Rotation, combineRotation);
            transform.Scale = baseScale * scaleFactor * (1 + _heroAttributeCmptOriginal.gainAttribute.skillRange);

            // 6) 写回组件
            _entityManager.SetComponentData(entity, transform);

            // 7) 添加持续伤害参数
            _entityManager.AddComponentData(entity, Hero.instance.skillsDamageCalPar);



            var skillPar = _entityManager.GetComponentData<SkillsDamageCalPar>(entity);

            skillPar.enablePull = enablePull;
            skillPar.enableExplosion = enableExplosion;
            skillPar.damageChangePar = damageChangePar;
            _entityManager.SetComponentData(entity, skillPar);


            //8)添加Trackingoverlap检测
            if (queryCenter.radius != 0)
                _entityManager.AddComponentData(entity, queryCenter);

            // 8-1) 添加瞬时技能碰撞缓冲区
            var hits = _entityManager.AddBuffer<HitRecord>(entity);
            //9）添加寻踪技能的专属标签,默认寻址次数为5,初始方向为传入的原始旋转方向
            _entityManager.AddComponentData(entity, new SkillsTrackingCalPar() { runCount = 5, currentDir = math.mul(quaternion, new float3(0f, 0f, 1f)) });

            //10)添加寻址技能的专属buffer
            _entityManager.AddBuffer<TrackingRecord>(entity);

            _entityManager.AddBuffer<HitElementResonanceRecord>(entity);

            return entity;
        }

        /// <summary>
        /// 无伤害的 寻址类技能
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="queryCenter"></param>
        /// <param name="posion"></param>
        /// <param name="quaternion"></param>
        /// <param name="damageChangePar"></param>
        /// <param name="positionOffset"></param>
        /// <param name="rotationOffsetEuler"></param>
        /// <param name="scaleFactor"></param>
        /// <param name="enablePull"></param>
        /// <param name="enableExplosion"></param>
        /// <returns></returns>
        public Entity DamageSkillsTrackingPropNoneDamage(
        Entity prefab,
        OverlapTrackingQueryCenter queryCenter,
        float3 posion,
        quaternion quaternion,
        float damageChangePar = 1,//默认伤害参数为1
        float3 positionOffset = default,
        float3 rotationOffsetEuler = default,  // 传入度数
        float scaleFactor = 1f, bool enablePull = false, bool enableExplosion = false)
        {
            DevDebug.Log("释放无伤害的寻址类技能侦测器");

            // 1) 实例化
            var entity = _entityManager.Instantiate(prefab);

            // 2) 取出可变的 LocalTransform
            var transform = _entityManager.GetComponentData<LocalTransform>(entity);


            // 3) 从英雄获取基础位置/旋转/缩放
            float3 heroPos = posion;
            quaternion heroRot = quaternion;
            float baseScale = transform.Scale; // 保留预制体的原始 scale

            // 4) 计算欧拉偏移的四元数
            //    math.radians 将度数转为弧度
            quaternion eulerOffsetQuat = quaternion.EulerXYZ(
                math.radians(rotationOffsetEuler)
            );

            // 5) 叠加偏移
            transform.Position = heroPos
                                + math.mul(heroRot, positionOffset);
            //计算整合旋转
            var combineRotation = math.mul(heroRot, eulerOffsetQuat);
            //叠加本体旋转
            transform.Rotation = math.mul(transform.Rotation, combineRotation);
            transform.Scale = baseScale * scaleFactor * (1 + _heroAttributeCmptOriginal.gainAttribute.skillRange);

            // 6) 写回组件
            _entityManager.SetComponentData(entity, transform);

            //8)添加Trackingoverlap检测
            if (queryCenter.radius != 0)
                _entityManager.AddComponentData(entity, queryCenter);

            //9）添加寻踪技能的专属标签,默认寻址次数为5,初始方向为传入的原始旋转方向
            _entityManager.AddComponentData(entity, new SkillsTrackingCalPar() { runCount = 5, currentDir = math.mul(quaternion, new float3(0f, 0f, 1f)) });

            //10)添加寻址技能的专属buffer
            _entityManager.AddBuffer<TrackingRecord>(entity);


            return entity;
        }

        /// <summary>
        /// 无伤害类型增益 技能道具，如元素共鸣体
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="posion"></param>
        /// <param name="quaternion"></param>
        /// <param name="damageChangePar"></param>
        /// <param name="positionOffset"></param>
        /// <param name="rotationOffsetEuler"></param>
        /// <param name="scaleFactor"></param>
        /// <param name="enablePull"></param>
        /// <param name="enableExplosion"></param>
        /// <returns></returns>
        public Entity DamageSkillsFlightPropNoneDamage(
        Entity prefab,
        float3 posion,
        quaternion quaternion,
        float damageChangePar = 1,//默认伤害参数为1
        float3 positionOffset = default,
        float3 rotationOffsetEuler = default,  // 传入度数
        float scaleFactor = 1f, bool enablePull = false, bool enableExplosion = false)
        {
            DevDebug.Log("释放无伤害型技能");

            // 1) 实例化
            var entity = _entityManager.Instantiate(prefab);

            // 2) 取出可变的 LocalTransform
            var transform = _entityManager.GetComponentData<LocalTransform>(entity);


            // 3) 从英雄获取基础位置/旋转/缩放
            float3 heroPos = posion;
            quaternion heroRot = quaternion;
            float baseScale = transform.Scale; // 保留预制体的原始 scale

            // 4) 计算欧拉偏移的四元数
            //    math.radians 将度数转为弧度
            quaternion eulerOffsetQuat = quaternion.EulerXYZ(
                math.radians(rotationOffsetEuler)
            );

            // 5) 叠加偏移
            transform.Position = heroPos
                                + math.mul(heroRot, positionOffset);
            //计算整合旋转
            var combineRotation = math.mul(heroRot, eulerOffsetQuat);
            //叠加本体旋转
            transform.Rotation = math.mul(transform.Rotation, combineRotation);
            transform.Scale = baseScale * scaleFactor * (1 + _heroAttributeCmptOriginal.gainAttribute.skillRange);

            // 6) 写回组件
            _entityManager.SetComponentData(entity, transform);


            return entity;
        }

        /// <summary>
        /// 爆炸类通用技能
        /// 实例化并初始化一个伤害型飞行技能实体（PulseC）-
        /// 支持位置、旋转、缩放偏移，以及可选的引力/斥力效果。
        /// </summary>
        /// <param name="prefab">技能预制体的实体</param>
        /// <param name="position">英雄当前位置</param>
        /// <param name="rotation">英雄朝向四元数</param>
        /// <param name="positionOffset">相对于英雄位置的偏移（世界空间）</param>
        /// <param name="rotationOffsetEuler">相对于英雄朝向的欧拉角偏移（度）</param>
        /// <param name="scaleFactor">缩放因子（相对于预制体原始 Scale）</param>
        /// <param name="enablePull">是否开启吸引效果</param>
        /// <param name="enableExplosion">是否开启爆炸效果</param>
        /// <returns>新实例化出的实体（仍在命令缓冲区中，待 Playback）</returns>
        public Entity SpawnDamageFlightSkillImmediate(
            Entity prefab,
            float3 position,
            quaternion rotation,
            float damageChangePar = 0,//默认伤害参数为1
            float3 positionOffset = default,
            float3 rotationOffsetEuler = default,
            float scaleFactor = 1f,
            bool enablePull = false,
            bool enableExplosion = false
            )
        {
            // 1) 立刻 Instantiate
            var entity = _entityManager.Instantiate(prefab);

            // 2) 拿 prefab 原始 Transform
            var prefabTransform = _entityManager.GetComponentData<LocalTransform>(prefab);
            float baseScale = prefabTransform.Scale;

            // 3) 计算新 Transform
            quaternion offsetQuat = quaternion.EulerXYZ(math.radians(rotationOffsetEuler));
            var newTransform = new LocalTransform
            {
                Position = position + math.mul(rotation, positionOffset),
                Rotation = math.mul(rotation, offsetQuat),
                Scale = baseScale * scaleFactor * (1 + _heroAttributeCmptOriginal.gainAttribute.skillRange)
            };

            // 4) 写回
            _entityManager.SetComponentData(entity, newTransform);

            // 5) 添加伤害参数
            var damagePar = Hero.instance.skillsDamageCalPar;
            damagePar.enablePull = enablePull;
            damagePar.enableExplosion = enableExplosion;
            damagePar.damageChangePar = damageChangePar;
            _entityManager.AddComponentData(entity, damagePar);
            // 6) 添加缓冲区
            var hits = _entityManager.AddBuffer<HitRecord>(entity);
            _entityManager.AddBuffer<HitElementResonanceRecord>(entity);


            return entity;

        }


        /// ECB 版本 用于 回调BASE系统，在外部调用的引用,用于粒子系统或者专用的用于销毁再初始化的系统的处理
        public Entity DamageSkillsCallBackExplosionProp(
            EntityCommandBuffer ecb,
            Entity prefab,
            float3 position,
            quaternion rotation,
            float damageChangePar = 1,//默认伤害参数为1
            float3 positionOffset = default,
            float3 rotationOffsetEuler = default,
            float scaleFactor = 1f,
            bool enablePull = false,
            bool enableExplosion = false)
        {
            // 1) 延迟实例化
            var entity = ecb.Instantiate(prefab);

            // 2) 读取预制体上已有的 LocalTransform，仅读取操作可以直接用 EntityManager
            var prefabTransform = _entityManager.GetComponentData<LocalTransform>(prefab);
            float baseScale = prefabTransform.Scale;

            // 3) 计算新的变换
            quaternion offsetQuat = quaternion.EulerXYZ(math.radians(rotationOffsetEuler));
            LocalTransform newTransform = new LocalTransform
            {
                Position = position + math.mul(rotation, positionOffset),
                Rotation = math.mul(rotation, offsetQuat),
                //这里由技能范围决定技能的影响因子
                Scale = baseScale * scaleFactor * (1 + _heroAttributeCmptOriginal.gainAttribute.skillRange)
            };

            // 4) 写入新实体
            ecb.SetComponent(entity, newTransform);

            // 5) 添加并初始化伤害参数组件
            var damagePar = new SkillsDamageCalPar { };
            damagePar = Hero.instance.skillsDamageCalPar;
            damagePar.enablePull = enablePull;
            damagePar.enableExplosion = enableExplosion;
            damagePar.damageChangePar = damageChangePar;
            ecb.AddComponent(entity, damagePar);

            // 6) 添加碰撞记录缓冲区
            var hits = ecb.AddBuffer<HitRecord>(entity);
            ecb.AddBuffer<HitElementResonanceRecord>(entity);

            //写回
            return entity;
        }


        /// <summary>
        /// 武器附魔类技能,附魔，暗能，并补充附魔时间，附魔类技能的特效？
        /// </summary>
        public void WeaponEnchantmentSkillDarkEnergy(int darkEnergyCount)
        {

            Hero.instance.skillAttackPar.darkEnergyCapacity = darkEnergyCount;

            Hero.instance.skillAttackPar.darkEnergyEnhantmentTimer = 15;


        }

        /// <summary>
        /// 武器附魔类技能， 寒冰
        /// </summary>
        public void WeaponEnchantmentSkillFrost(bool enableFrostScenod = false, int frostSplittingCount = 5, int frostShardCount = 5, float skillDamageChangePar = 0.1f)
        {

            Hero.instance.skillAttackPar.frostCapacity = 1;
            Hero.instance.skillAttackPar.frostEnchantmentTimer = 15;
            if (enableFrostScenod)
            {
                Hero.instance.skillAttackPar.enableFrostSecond = true;
                Hero.instance.skillAttackPar.frostSplittingCount = frostSplittingCount;
                Hero.instance.skillAttackPar.frostShardCount = frostShardCount;
                Hero.instance.skillAttackPar.frostSkillChangePar = skillDamageChangePar;
            }

        }

        #region 带连续释放的触发携程类的技能


        /// <summary>
        ///连续释放技能，因为携程的返回值问题，所以直接传入1阶段技能标签
        ///这里以连续的落雷技能开始
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="prefab"></param>
        /// <param name="componentData"></param>
        /// <param name="posion"></param>
        /// <param name="quaternion"></param>
        /// <param name="damageChangePar"></param>
        /// <param name="positionOffset"></param>
        /// <param name="rotationOffsetEuler"></param>
        /// <param name="scaleFactor"></param>
        /// <param name="enablePull"></param>
        /// <param name="enableExplosion"></param>
        public void DamageSkillsFlightPropConsecutiveCasting<T>(
    Entity prefab,
    T componentData,
    int castCount,//释放总次数
    float interval,//间隔
    float3 posion,
    quaternion quaternion,
    float damageChangePar = 0,//默认伤害参数为0
    float3 positionOffset = default,
    float3 rotationOffsetEuler = default,  // 传入度数
    float scaleFactor = 1f, bool enablePull = false, bool enableExplosion = false, bool fllow = false) where T : unmanaged, IComponentData


        {

            var _rollCoroutineId = _coroutineController.StartRoutine(
                    IEThunderStrikeSkill<T>(prefab, componentData, castCount, interval, posion, quaternion, damageChangePar, positionOffset, rotationOffsetEuler
                    , scaleFactor, enablePull, enableExplosion, fllow),
                    tag: "ThunderStrikeSkill",
                    onComplete: () =>
                    {
                        DevDebug.Log("持续型连续技能释放完成");
                    }
                );

        }
        #endregion

        #region 携程区域
        /// <summary>
        /// ICommonetData 必须加上 unmanaged 的泛型约束，这里是范围释放性技能， 攻击范围增加技能的释放范围
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="componentData"></param>
        /// <param name="posion"></param>
        /// <param name="quaternion"></param>
        /// <param name="damageChangePar"></param>
        /// <param name="positionOffset"></param>
        /// <param name="rotationOffsetEuler"></param>
        /// <param name="scaleFactor"></param>
        /// <param name="enablePull"></param>
        /// <param name="enableExplosion"></param>
        /// <returns></returns>
        IEnumerator IEThunderStrikeSkill<T>(Entity prefab,
         T componentData,
         int castCount,//释放总次数
         float interval,//间隔
         float3 posion,
         quaternion quaternion,
        float damageChangePar = 0,//默认伤害参数为1
        float3 positionOffset = default,
        float3 rotationOffsetEuler = default,  // 传入度数
        float scaleFactor = 1f, bool enablePull = false, bool enableExplosion = false, bool fllow = false) where T : unmanaged, IComponentData
        {


            for (int i = 0; i < castCount; i++)
            {
                // 1) 实例化
                var entity = _entityManager.Instantiate(prefab);

                // 2) 取出可变的 LocalTransform
                var transform = _entityManager.GetComponentData<LocalTransform>(entity);


                // 3) 从英雄获取基础位置/旋转/缩放

                float3 heroPos = posion;
                quaternion heroRot = quaternion;


                //跟随情况下，持续性释放的飞行道具的位置 跟着英雄移动，旋转也是一样的
                if (fllow)
                {
                    heroPos = Hero.instance.transform.position;
                    heroRot = Hero.instance.transform.rotation;
                }
                float baseScale = transform.Scale; // 保留预制体的原始 scale

                // 4) 计算欧拉偏移的四元数
                //    math.radians 将度数转为弧度
                quaternion eulerOffsetQuat = quaternion.EulerXYZ(
                    math.radians(rotationOffsetEuler)
                );

                //4-1) 范围位置随机化
                float2 randomInCircle = UnityEngine.Random.insideUnitCircle * 10f * (1 + _heroAttributeCmptOriginal.gainAttribute.skillRange);
                heroPos += new float3(randomInCircle.x, 0, randomInCircle.y);//随机范围内进行相关的参数处理这里是400平方米

                // 5) 叠加偏移
                transform.Position = heroPos
                                    + math.mul(heroRot, positionOffset);
                //计算整合旋转
                var combineRotation = math.mul(heroRot, eulerOffsetQuat);
                //叠加本体旋转
                transform.Rotation = math.mul(transform.Rotation, combineRotation);
                transform.Scale = baseScale * scaleFactor * (1 + _heroAttributeCmptOriginal.gainAttribute.skillRange);

                // 6) 写回组件
                _entityManager.SetComponentData(entity, transform);

                // 7) 添加伤害参数
                _entityManager.AddComponentData(entity, Hero.instance.skillsDamageCalPar);

                var skillPar = _entityManager.GetComponentData<SkillsDamageCalPar>(entity);

                skillPar.enablePull = enablePull;
                skillPar.enableExplosion = enableExplosion;
                skillPar.damageChangePar = damageChangePar;
                //  skillPar.lightningDotDamage += 1;//这样就能持续感电

                _entityManager.SetComponentData(entity, skillPar);

                // 8) 添加碰撞记录缓冲区
                var hits = _entityManager.AddBuffer<HitRecord>(entity);
                _entityManager.AddBuffer<HitElementResonanceRecord>(entity);

                //携程内直接添加技能标签
                _entityManager.AddComponentData<T>(entity, componentData);

                // DevDebug.Log("第  " + i + " 次释放技能");
                yield return new WaitForSeconds(interval);
            }


        }
        /// <summary>
        /// 技能相位三秒后回复生命值
        /// </summary>
        /// <param name="interval"></param>
        /// <returns></returns>
        IEnumerator IEPhaseSkill(float interval, HeroAttributeCmpt realAttr)
        {

            realAttr.defenseAttribute.energy -= 50;
            _entityManager.SetComponentData(_heroEntity, realAttr);


            //时间间隔完毕之后恢复生命值 （？外部表现特征）
            yield return new WaitForSeconds(interval);

            realAttr.defenseAttribute.hp += (realAttr.defenseAttribute.originalHp / 10);
            _entityManager.SetComponentData(_heroEntity, realAttr);

        }

        //烈焰冲锋携程
        IEnumerator IEFlameCharge(HeroAttributeCmpt runTimeHeroCmp, HeroIntgratedNoImmunityState runtimeStateNoImmunity,int level=0,bool enableSecondB=false)
        {
            Hero.instance.skillAttackPar.flameCharge = true;//激活烈焰冲锋状态判断，在idel状态机中行使的状态
           // Hero.instance.animator.SetBool("Charge", true);
            // 消耗能量&无敌状态写回
            runTimeHeroCmp.defenseAttribute.energy -= 40;
            _entityManager.SetComponentData(_heroEntity, runTimeHeroCmp);
            runtimeStateNoImmunity.controlNoImmunityTimer = 1f;
            runtimeStateNoImmunity.dotNoImmunityTimer = 1f;
            runtimeStateNoImmunity.elementDamageNoImmunityTimer = 1f;
            runtimeStateNoImmunity.inlineDamageNoImmunityTimer = 1f;
            runtimeStateNoImmunity.physicalDamageNoImmunityTimer = 1f;
            _entityManager.SetComponentData(_heroEntity, runtimeStateNoImmunity);

            // 1. 冲锋参数
            float duration = 0.5f; // 持续0.5秒
            float timer = 0f;
            float speed = 50f; // 每秒移动的速度
            Vector3 startPosition = Hero.instance.transform.position;

            // 2. 冲锋协程
            while (timer < duration)
            {
                Vector3 forward = Hero.instance.transform.forward;
                Hero.instance.transform.position += forward * speed * Time.deltaTime;
                timer += Time.deltaTime;
                yield return null;
            }

            // 3. 计算最终判定区域
            Vector3 endPosition = Hero.instance.transform.position;
            Vector3 direction = (endPosition - startPosition).normalized;
            float length = Vector3.Distance(startPosition, endPosition);
            Vector3 center = (startPosition + endPosition) * 0.5f;
            Vector3 box = new Vector3(8, 3, length); // Z为路径长度
            quaternion rotation = quaternion.LookRotationSafe(direction, math.up()); // Box朝向

            // 4. 构造 OverlapOverTimeQueryCenter
            var filter = new CollisionFilter
            {
                BelongsTo = 1u << 10,
                CollidesWith = 1u << 6,
                GroupIndex = 0
            };

            var overlap = new OverlapOverTimeQueryCenter
            {
                shape = OverLapShape.Box,
                box = box,
                center = center,
                radius = 0.1f, // 用 box
                offset = float3.zero, // 不需要额外偏移
                rotaion = rotation.value,
                filter = filter
            };

            // 5. 生成判定实体
            var entityFlameCharge = DamageSkillsOverTimeProp(
                _skillPrefabs.HeroSkill_FlameCharge,
                overlap,
                center, // 注意这里位置填 center，rotation 填 rotation
                rotation,
                1, float3.zero, float3.zero, 1, false, false);

            _entityManager.AddComponentData(entityFlameCharge, new SkillFlameChargeTag { tagSurvivalTime = 6f, level = level});
            var skillPar = _entityManager.GetComponentData<SkillsOverTimeDamageCalPar>(entityFlameCharge);
            if (enableSecondB && UnityEngine.Random.Range(0, 1f) <= runTimeHeroCmp.attackAttribute.luckyStrikeChance * 0.5f * (0.25f + (0.015f * level)))
            {
                skillPar.damageChangePar *= 3f;//三倍基础乘伤
                skillPar.fireDotDamage += skillPar.fireDamage;//dot伤害
                DevDebug.LogError("烈焰尾迹 幸运增伤");
            }
            //skillPar.tempExplosion = 300;
            _entityManager.SetComponentData(entityFlameCharge, skillPar);


            // Hero.instance.animator.SetBool("Charge", false); //跳出冲锋动画
            // Hero.instance.animator.SetTrigger("Idle"); //跳转到Idel 动画
            yield return Hero.instance.skillAttackPar.flameCharge = false; //返回烈焰冲锋状态结束

        }

        #endregion

        #region 辅助方法区域
        /// <summary>
        /// 设置闪电链的碰撞体索引
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private SkillLightningChainTag SetLightningChainCollider(
    SkillLightningChainTag tag, int index, Entity value)
        {

            switch (index)
            {
                case 0: tag.colliderRef.collider1 = value; break;
                case 1: tag.colliderRef.collider2 = value; break;
                case 2: tag.colliderRef.collider3 = value; break;
                case 3: tag.colliderRef.collider4 = value; break;
                case 4: tag.colliderRef.collider5 = value; break;
                case 5: tag.colliderRef.collider6 = value; break;
                case 6: tag.colliderRef.collider7 = value; break;
                case 7: tag.colliderRef.collider8 = value; break;
                    // case 8: tag.colliderRef.collider9 = value; break;
                    // case 9: tag.colliderRef.collider10 = value; break;
                    // case 10: tag.colliderRef.collider11 = value; break;
                    // case 11: tag.colliderRef.collider12 = value; break;
                    // case 12: tag.colliderRef.collider13 = value; break;
                    // case 13: tag.colliderRef.collider14 = value; break;
                    // case 14: tag.colliderRef.collider15 = value; break;

            }
            return tag;
        }
        /// <summary>
        ///爆发类技能是独立计算还是快照转移？
        /// 
        /// </summary>
        /// <param name="skillsDamageCalPar"></param>
        /// <returns></returns>
        private SkillsBurstDamageCalPar SetSkillsBurstDamageCalPar(SkillsDamageCalPar skillsDamageCalPar)
        {
            var skillBurstDamageCalPar = new SkillsBurstDamageCalPar();


            return skillBurstDamageCalPar;
        }

        /// <summary>
        /// 处理爆发类技能 元素, 增加另外两种的全量DOT 伤害
        /// 这里！！  注意一个隐形BUG IjobFor 的快照机制，有时候IJOBFOR 拿到的是取出更新前的参数
        /// </summary>
        /// <param name="skillsBurstDamageCalPar"></param>
        /// <returns></returns>
        private SkillsBurstDamageCalPar DealDamageOfElementBurstB(SkillsBurstDamageCalPar skillsBurstDamageCalPar, bool enableChange = true)
        {
            if (!enableChange)
                return skillsBurstDamageCalPar;

            // 1. 整合所有 dot（物理+元素5种），总共6种
            float[] dots = new float[]
            {
                    skillsBurstDamageCalPar.bleedDotDamage,   // 0 物理
                    skillsBurstDamageCalPar.fireDotDamage,    // 1
                    skillsBurstDamageCalPar.frostDotDamage,   // 2
                    skillsBurstDamageCalPar.lightningDotDamage, // 3
                    skillsBurstDamageCalPar.shadowDotDamage,  // 4
                    skillsBurstDamageCalPar.poisonDotDamage   // 5
            };

            // 2. 找到首个非0 dot为主爆发类型
            int mainIdx = -1;
            for (int i = 0; i < dots.Length; i++)
            {
                if (dots[i] > 0)
                {
                    mainIdx = i;
                    break;
                }
            }

            if (mainIdx == -1)
            {
                // 全部为0，全部清零返回
                skillsBurstDamageCalPar.bleedDotDamage = 0;
                skillsBurstDamageCalPar.fireDotDamage = 0;
                skillsBurstDamageCalPar.frostDotDamage = 0;
                skillsBurstDamageCalPar.lightningDotDamage = 0;
                skillsBurstDamageCalPar.shadowDotDamage = 0;
                skillsBurstDamageCalPar.poisonDotDamage = 0;
                return skillsBurstDamageCalPar;
            }

            // 3. 随机选2种其余类型
            List<int> otherIdx = new List<int>() { 0, 1, 2, 3, 4, 5 };
            otherIdx.Remove(mainIdx);
            int randA = otherIdx[UnityEngine.Random.Range(0, otherIdx.Count)];
            otherIdx.Remove(randA);
            int randB = otherIdx[UnityEngine.Random.Range(0, otherIdx.Count)];

            // 4. 三项赋值，基本元素伤害再加上本来的瞬时物理伤害
            float val = dots[mainIdx] + skillsBurstDamageCalPar.instantPhysicalDamage;
            for (int i = 0; i < dots.Length; i++)
            {
                float setVal = (i == mainIdx || i == randA || i == randB) ? val : 0f;
                switch (i)
                {
                    case 0: skillsBurstDamageCalPar.bleedDotDamage = setVal; break;
                    case 1: skillsBurstDamageCalPar.fireDotDamage = setVal; break;
                    case 2: skillsBurstDamageCalPar.frostDotDamage = setVal; break;
                    case 3: skillsBurstDamageCalPar.lightningDotDamage = setVal; break;
                    case 4: skillsBurstDamageCalPar.shadowDotDamage = setVal; break;
                    case 5: skillsBurstDamageCalPar.poisonDotDamage = setVal; break;
                }
            }
            return skillsBurstDamageCalPar;




        }

        //元素护盾技能设置,outer
        public void SkillSetActiveElementShield(bool active)
        {

            Hero.instance.skillTransforms[3].gameObject.SetActive(active);

        }
        //冰霜护盾技能设置,outer
        public void SkillSetActiveFrostShield(bool active)
        {

            Hero.instance.skillTransforms[4].gameObject.SetActive(active);

        }

        #endregion
    }
}
