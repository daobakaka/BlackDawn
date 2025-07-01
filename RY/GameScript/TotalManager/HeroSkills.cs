using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFrame.BaseClass;
using BlackDawn.DOTS;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;
using Unity.Collections;
using ProjectDawn.Entities;
using System;
using Unity.Entities.UniversalDelegates;
using Unity.Physics;


namespace BlackDawn

{/// <summary>
/// 主管英雄技能的核心类，在英雄Mono脚本中初始化之后，通过获取单例从构造函数进行初始化
/// </summary>
    public class HeroSkills : Singleton<HeroSkills>
    {
        ScenePrefabsSingleton _skillPrefabs;
        EntityManager _entityManager;
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
            _coroutineController =Hero.instance.coroutineController;


            //获取英雄entity
            _heroEntity = Hero.instance.heroEntity;
            //建立查询
            _arcaneCircleQuery = _entityManager.CreateEntityQuery(typeof(SkillArcaneCircleTag));
            //暗影洪流
            _shadowTideQuery = _entityManager.CreateEntityQuery(typeof(SkillShadowTideTag));
            //实时英雄组件查询
            _heroRealTimeAttr = _entityManager.CreateEntityQuery(typeof(HeroAttributeCmpt), typeof(HeroEntityMasterTag));
           

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
            switch (iD)
                
            {
                //脉冲，瞬时
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
                //暗能，瞬时
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
                //冰火，瞬时？
                case HeroSkillID.IceFire:
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:
                            var entityIce = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_IceFire, _transform.position, Hero.instance.transform.rotation, 1, new float3(0,0.3f,0), 0, 1, false, false);
                            //添加技能专用标签用于检测等运动等    
                            _entityManager.AddComponentData(entityIce, new SkillIceFireTag() { tagSurvivalTime =20, speed = 3,radius=5 ,currentAngle=1.72f});
                            var entityFire = DamageSkillsFlightProp(_skillPrefabs.HeroSkillAssistive_IceFireFire, _transform.position, Hero.instance.transform.rotation, 1, new float3(0, 0.3f, 0), 0, 1, false, false);
                            //添加技能专用标签用于检测等运动等    
                            _entityManager.AddComponentData(entityFire, new SkillIceFireTag() { tagSurvivalTime = 20, speed = 3, radius = 5, currentAngle = -1.72f });
                            break;
                            //二阶冰火技能增加伤害、半径、体积、速度、持续时间
                        case HeroSkillPsionicType.PsionicA:
                            var entityIce1 = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_IceFire, _transform.position, Hero.instance.transform.rotation, 1.3f, new float3(0, 0.5f, 0), 0, 1.5f, false, false);
                            //添加技能专用标签用于检测等运动等    
                            _entityManager.AddComponentData(entityIce1, new SkillIceFireTag() { tagSurvivalTime = 25, speed = 4, radius = 7, currentAngle = 1.72f ,originalScale=2.6f});
                            var entityFire1 = DamageSkillsFlightProp(_skillPrefabs.HeroSkillAssistive_IceFireFire, _transform.position, Hero.instance.transform.rotation, 1.3f, new float3(0, 0.5f, 0), 0, 1.3f, false, false);
                            //添加技能专用标签用于检测等运动等    
                            _entityManager.AddComponentData(entityFire1, new SkillIceFireTag() { tagSurvivalTime = 25, speed = 4, radius = 7, currentAngle = -1.72f , originalScale = 2.6f });
                            break;
                        case HeroSkillPsionicType.PsionicB:
                            var entityIce2 = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_IceFire, _transform.position, Hero.instance.transform.rotation, 1, new float3(0, 0.3f, 0), 0, 1, false, false);
                            //添加技能专用标签用于检测等运动等    
                            _entityManager.AddComponentData(entityIce2, new SkillIceFireTag() { tagSurvivalTime = 25, speed = 4, radius = 7, currentAngle = 1.72f ,enableSecond=true,scaleChangePar =1,skillDamageChangeParTag=1, originalScale = 2f });
                            _entityManager.AddComponentData(entityIce2, new SkillIceFireSecondExplosionRequestTag { });
                            _entityManager.SetComponentEnabled<SkillIceFireSecondExplosionRequestTag>(entityIce2, false);
                            var entityFire2 = DamageSkillsFlightProp(_skillPrefabs.HeroSkillAssistive_IceFireFire, _transform.position, Hero.instance.transform.rotation, 1, new float3(0, 0.3f, 0), 0, 1, false, false);
                            //添加技能专用标签用于检测等运动等    
                            _entityManager.AddComponentData(entityFire2, new SkillIceFireTag() { tagSurvivalTime = 25, speed = 4, radius = 7, currentAngle = -1.72f,enableSecond=true,scaleChangePar = 1, skillDamageChangeParTag = 1, originalScale = 2f });
                            _entityManager.AddComponentData(entityFire2, new SkillIceFireSecondExplosionRequestTag { });
                            _entityManager.SetComponentEnabled<SkillIceFireSecondExplosionRequestTag>(entityFire2, false);
                            break;
                        case HeroSkillPsionicType.PsionicAB:

                            var entityIce3 = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_IceFire, _transform.position, Hero.instance.transform.rotation, 0.3f, new float3(0, 1.3f, 0), 0, 1.5f, false, false);
                            //添加技能专用标签用于检测等运动等    
                            _entityManager.AddComponentData(entityIce3, new SkillIceFireTag() { tagSurvivalTime = 30, speed = 6f, radius = 9, currentAngle = -1.72f ,enableSecond = true, scaleChangePar = 1, skillDamageChangeParTag = 1 , originalScale = 2.6f });
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
               //落雷，瞬时
                case HeroSkillID.ThunderStrike:

                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:
                            
                            //释放初级落雷技能,测试跟随性落雷
                             DamageSkillsFlightPropConsecutiveCasting<SkillThunderStrikeTag>(_skillPrefabs.HeroSkill_ThunderStrike,new SkillThunderStrikeTag() {tagSurvivalTime=0.5f},12,1, _transform.position, 
                                 Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false,false);                      

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
                //法阵，持续
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
                                    _entityManager.AddComponentData(entityArcaneCircle, new SkillArcaneCircleTag() { tagSurvivalTime = 3 ,enableSecondA=true});
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
                                    _entityManager.AddComponentData(entityArcaneCircle, new SkillArcaneCircleTag() { tagSurvivalTime = 3,enableSecondB=true });
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
                                    _entityManager.AddComponentData(entityArcaneCircle, new SkillArcaneCircleTag() { tagSurvivalTime = 3, enableSecondA = true,enableSecondB=true });
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
                //寒冰，瞬时    
                case HeroSkillID.Frost:
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:
                            WeaponEnchantmentSkillFrost();
                            break;
                        //增加分裂功能
                        case HeroSkillPsionicType.PsionicA:
                            WeaponEnchantmentSkillFrost( true,5,5);
                            break;
                        //增加碎片次数,不能分裂，但是可以冻结
                        case HeroSkillPsionicType.PsionicB:
                            WeaponEnchantmentSkillFrost(false,5,2);
                            Hero.instance.skillAttackPar.tempFreeze = 101;
                            Hero.instance.skillAttackPar.enableSpecialEffect = true;
                            break;
                        //分裂和碎片都增加
                        case HeroSkillPsionicType.PsionicAB:
                            WeaponEnchantmentSkillFrost(true, 15, 17,0.1f);
                            Hero.instance.skillAttackPar.tempFreeze = 101;
                            Hero.instance.skillAttackPar.enableSpecialEffect = true;
                            break;
                    }

                    break;
                //元素共鸣,持续？，非技能标签，不会造成伤害,可以读取等级展示伤害
                case HeroSkillID.ElementResonance:
                    switch (psionicType)
                    { case HeroSkillPsionicType.Basic:

                            var entityElementResonance = DamageSkillsFlightPropNoneDamage(_skillPrefabs.HeroSkill_ElementResonance, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            _entityManager.AddComponentData(entityElementResonance, new SkillElementResonanceTag() { tagSurvivalTime = 8 });
                            break;
                        case HeroSkillPsionicType.PsionicA:
                            var entityElementResonanceA = DamageSkillsFlightPropNoneDamage(_skillPrefabs.HeroSkill_ElementResonance, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            _entityManager.AddComponentData(entityElementResonanceA, new SkillElementResonanceTag() { tagSurvivalTime = 8,enableSecondA=true,secondDamagePar=1 });
                            break;
                        case HeroSkillPsionicType.PsionicB:
                            var entityElementResonanceB = DamageSkillsFlightPropNoneDamage(_skillPrefabs.HeroSkill_ElementResonance, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            _entityManager.AddComponentData(entityElementResonanceB, new SkillElementResonanceTag() { tagSurvivalTime = 8, enableSecondB = true, thridDamagePar = 2 });
                            break;

                        case HeroSkillPsionicType.PsionicAB:
                            var entityElementResonanceAB = DamageSkillsFlightPropNoneDamage(_skillPrefabs.HeroSkill_ElementResonance, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            _entityManager.AddComponentData(entityElementResonanceAB, new SkillElementResonanceTag() { tagSurvivalTime = 8, enableSecondA = true, secondDamagePar = 2,enableSecondB=true,thridDamagePar=2 });
                            break;

                    }
                    break;
                //静电牢笼，瞬时，持续
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
                            _entityManager.AddComponentData(entityElectroCageA, new SkillElectroCageTag() { tagSurvivalTime = 4,enableSecondA =true,skillDamageChangeParTag=2,intervalTimer=0.2f });
                            break;
                            //开启第三阶段，静电传导
                        case HeroSkillPsionicType.PsionicB:

                            var entityElectroCageB = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ElectroCage, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            //构造定身效果
                            var skillParB = _entityManager.GetComponentData<SkillsDamageCalPar>(entityElectroCageB);
                            skillParB.tempRoot = 101;
                            skillParB.tempStun = 101;
                            _entityManager.SetComponentData(entityElectroCageB, skillParB);
                            _entityManager.AddComponentData(entityElectroCageB, new SkillElectroCageTag() { tagSurvivalTime = 4,enableSecondB=true });
                            break;

                        //开启二阶段，增伤100%,开启静电传导标识
                        case HeroSkillPsionicType.PsionicAB:

                            var entityElectroCageAB = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ElectroCage, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            //构造定身效果
                            var skillParAB = _entityManager.GetComponentData<SkillsDamageCalPar>(entityElectroCageAB);
                            skillParAB.tempRoot = 101;
                            skillParAB.tempStun = 101;
                            _entityManager.SetComponentData(entityElectroCageAB, skillParAB);
                            _entityManager.AddComponentData(entityElectroCageAB, new SkillElectroCageTag() { tagSurvivalTime = 4, enableSecondA = true,enableSecondB=true, skillDamageChangeParTag = 2, intervalTimer = 0.2f });
                            break;


                    }

                    break;
                //毒爆地雷,瞬时，持续，第一个3阶变化技能
                case HeroSkillID.MineBlast:
                    switch (psionicType)
                    {
                        //布置3颗毒爆雷  是否有必要，布置方式，是否可提供旋转操作？暂不提供效果， 原生技能自带2阶爆炸效果,添加爆炸效果标签，用于爆炸持续时间,300%爆炸伤害传递
                        case HeroSkillPsionicType.Basic:

                            for (int i = 0; i < 3; i++)
                            {
                                var entityMineBlast = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_MineBlast, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, new float3(10*i,0,0), float3.zero, 1, false, false);
                                _entityManager.AddComponentData(entityMineBlast, new SkillMineBlastTag() { tagSurvivalTime = 20, scaleChangePar = 2 ,skillDamageChangeParTag=3});
                                _entityManager.AddComponentData(entityMineBlast, new SkillMineBlastExplosionTag() { tagSurvivalTime = 1 });
                            }
                            break;
                        case HeroSkillPsionicType.PsionicA:
                            for (int i = 0; i < 3; i++)
                            {
                                var entityMineBlastA = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_MineBlast, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, new float3(10 * i, 0, 0), float3.zero, 1, false, false);
                                _entityManager.AddComponentData(entityMineBlastA, new SkillMineBlastTag() { tagSurvivalTime = 20, scaleChangePar = 2, skillDamageChangeParTag = 3 });
                                _entityManager.AddComponentData(entityMineBlastA, new SkillMineBlastExplosionTag() { tagSurvivalTime = 1, skillDamageChangeParTag = 1.5f, enableSecondA = true, tagSurvivalTimeSecond = 5,scaleChangePar=4 });
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
                                _entityManager.AddComponentData(entityMineBlastC, new SkillMineBlastExplosionTag() { tagSurvivalTime = 1, enableSecondC = true,level=1 });
                            }
                            for (int i = 0; i < 2; i++)
                            {
                                var par = 1 - 2 * i;
                                var entityMineBlastC = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_MineBlast, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, new float3(10 , 0, 10 * par), float3.zero, 1, false, false);
                                _entityManager.AddComponentData(entityMineBlastC, new SkillMineBlastTag() { tagSurvivalTime = 20, scaleChangePar = 2, skillDamageChangeParTag = 3 });
                                _entityManager.AddComponentData(entityMineBlastC, new SkillMineBlastExplosionTag() { tagSurvivalTime = 1, enableSecondC = true,level=1 });
                            }
                            break;
                        case HeroSkillPsionicType.PsionicAB:

                            for (int i = 0; i < 3; i++)
                            {
                                var entityMineBlastAB = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_MineBlast, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, new float3(10 * i, 0, 0), float3.zero, 1, false, false);
                                _entityManager.AddComponentData(entityMineBlastAB, new SkillMineBlastTag() { tagSurvivalTime = 20, scaleChangePar = 2, skillDamageChangeParTag = 3 });
                                _entityManager.AddComponentData(entityMineBlastAB, new SkillMineBlastExplosionTag() { tagSurvivalTime = 1, skillDamageChangeParTag = 1.5f, enableSecondA = true, tagSurvivalTimeSecond = 5, scaleChangePar = 4 ,enableSecondB=true});
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
                                _entityManager.AddComponentData(entityMineBlastABC, new SkillMineBlastExplosionTag() { tagSurvivalTime = 1, skillDamageChangeParTag = 1.5f, enableSecondA = true, enableSecondB=true,tagSurvivalTimeSecond = 5, scaleChangePar = 4, enableSecondC = true, level = 1 });
                            }
                            for (int i = 0; i < 2; i++)
                            {
                                var par = 1 - 2 * i;
                                var entityMineBlastABC = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_MineBlast, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, new float3(10, 0, 10 * par), float3.zero, 1, false, false);
                                _entityManager.AddComponentData(entityMineBlastABC, new SkillMineBlastTag() { tagSurvivalTime = 20, scaleChangePar = 2, skillDamageChangeParTag = 3 });
                                _entityManager.AddComponentData(entityMineBlastABC, new SkillMineBlastExplosionTag() { tagSurvivalTime = 1, skillDamageChangeParTag = 1.5f, enableSecondA = true,enableSecondB=true, tagSurvivalTimeSecond = 5, scaleChangePar = 4, enableSecondC = true, level = 1 });
                            }
                            break;

                    }
                    break;
                //暗影洪流，瞬时,持续，引导
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
                                    var overlap = new OverlapOverTimeQueryCenter { center = Hero.instance.transform.position,radius=1, filter = filter, offset = new float3(0, 0, 15), box = new float3(6, 6,40 ), shape = OverLapShape.Box };
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
                                    _entityManager.AddComponentData(entityShadowTideA, new SkillShadowTideTag { tagSurvivalTime = 3, level = 1 ,skillDamageChangeParTag=2});
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
                                    _entityManager.AddComponentData(entityShadowTideB, new SkillShadowTideTag { tagSurvivalTime =3, level = 1, skillDamageChangeParTag = 1,enableSecondB=true });
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
                                    _entityManager.AddComponentData(entityShadowTideAB, new SkillShadowTideTag { tagSurvivalTime = 3, level = 1, skillDamageChangeParTag = 2 ,enableSecondB=true});
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
                //毒雨,持续,技能附带的控制参数， 可以通过配置表进行配置
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
                            var entityPoisonRain= DamageSkillsOverTimeProp(_skillPrefabs.HeroSkill_PoisonRain, overlap,Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            _entityManager.AddComponentData(entityPoisonRain, new SkillPoisonRainTag { tagSurvivalTime = 15 ,level=1});
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
                            var entityPoisonRainA = DamageSkillsOverTimeProp(_skillPrefabs.HeroSkill_PoisonRain,overlapA ,Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            _entityManager.AddComponentData(entityPoisonRainA, new SkillPoisonRainTag { tagSurvivalTime = 15 ,level=1});
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
                            var overlapB = new OverlapOverTimeQueryCenter { center = Hero.instance.skillTargetPositon, radius = 30, filter = filterB,offset=new float3(0,0,0) };
                            var entityPoisonRainB = DamageSkillsOverTimeProp(_skillPrefabs.HeroSkill_PoisonRain,overlapB, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            _entityManager.AddComponentData(entityPoisonRainB, new SkillPoisonRainTag { tagSurvivalTime = 15, level = 1 });
                            int level = 3;
                            var skillParB = _entityManager.GetComponentData<SkillsOverTimeDamageCalPar>(entityPoisonRainB);
                            skillParB.tempSlow = 30;
                            //添加昏迷值
                            skillParB.tempStun = 200;
                            //添加火焰参数
                            skillParB.fireDamage += skillParB.poisonDamage*(1+level*0.2f);
                            skillParB.fireDotDamage += skillParB.poisonDotDamage* (1 + level * 0.2f); 
                            _entityManager.SetComponentData(entityPoisonRainB, skillParB);

                            //--火焰雨,仅仅增加一个效果，无实际计算
                            var entityPoisonRainBFire = DamageSkillsOverTimeProp(_skillPrefabs.HeroSkillAssistive_PoisonRainB,new OverlapOverTimeQueryCenter(), Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
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
                            var entityPoisonRainAB = DamageSkillsOverTimeProp(_skillPrefabs.HeroSkill_PoisonRain,overlapAB, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
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
                            var entityPoisonRainABFire = DamageSkillsOverTimeProp(_skillPrefabs.HeroSkillAssistive_PoisonRainB, new OverlapOverTimeQueryCenter(),Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            _entityManager.AddComponentData(entityPoisonRainABFire, new SkillPoisonRainTag { tagSurvivalTime = 15, level = 1 });
                        
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
      public  Entity DamageSkillsFlightProp(
          Entity prefab,
          float3 posion,
          quaternion quaternion,
          float damageChangePar =1,//默认伤害参数为1
          float3 positionOffset = default,
          float3 rotationOffsetEuler = default,  // 传入度数
          float scaleFactor = 1f,bool enablePull =false,bool enableExplosion =false)
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
            transform.Scale = baseScale * scaleFactor*(1+ _heroAttributeCmptOriginal.gainAttribute.skillRange);

            // 6) 写回组件
            _entityManager.SetComponentData(entity, transform);

            // 7) 添加伤害参数
            _entityManager.AddComponentData(entity, Hero.instance.skillsDamageCalPar);
       
            var skillPar = _entityManager.GetComponentData<SkillsDamageCalPar>(entity);
           
                skillPar.enablePull = enablePull;           
                skillPar.enableExplosion = enableExplosion;
                skillPar.damageChangePar= damageChangePar;         
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

            // 7) 添加持续伤害参数
            _entityManager.AddComponentData(entity, Hero.instance.skillsOverTimeDamageCalPar);

            //8)添加持续性伤害overlap检测
            if(queryCenter.radius!=0)
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
                Scale = baseScale * scaleFactor*(1 + _heroAttributeCmptOriginal.gainAttribute.skillRange)
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
        public Entity DamageSkillsExplosionProp(
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
                Scale = baseScale * scaleFactor*(1 + _heroAttributeCmptOriginal.gainAttribute.skillRange)
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
        public void WeaponEnchantmentSkillFrost( bool enableFrostScenod = false,int frostSplittingCount =5, int frostShardCount =5,float skillDamageChangePar=0.1f)
        {

            Hero.instance.skillAttackPar.frostCapacity = 1;
                Hero.instance.skillAttackPar.frostEnchantmentTimer = 15;
            if (enableFrostScenod)
            {
                Hero.instance.skillAttackPar.enableFrostSecond = true;
                Hero.instance.skillAttackPar.frostSplittingCount =frostSplittingCount;
                Hero.instance.skillAttackPar.frostShardCount =frostShardCount;
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
    float scaleFactor = 1f, bool enablePull = false, bool enableExplosion = false,bool fllow =false ) where T :unmanaged,IComponentData


        {

            var _rollCoroutineId = _coroutineController.StartRoutine(
                    ThunderStrikeSkill<T>(prefab,componentData, castCount, interval, posion,quaternion,damageChangePar,positionOffset,rotationOffsetEuler
                    ,scaleFactor,enablePull,enableExplosion,fllow),
                    tag: "ThunderStrikeSkill",
                    onComplete: () => {
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
        IEnumerator  ThunderStrikeSkill<T>( Entity prefab,
         T componentData,
         int castCount,//释放总次数
         float interval,//间隔
         float3 posion,
         quaternion quaternion,
        float damageChangePar = 0,//默认伤害参数为1
        float3 positionOffset = default,
        float3 rotationOffsetEuler = default,  // 传入度数
        float scaleFactor = 1f, bool enablePull = false, bool enableExplosion = false ,bool fllow=false)where T : unmanaged, IComponentData
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
                    heroRot =Hero.instance.transform.rotation;
                }
                float baseScale = transform.Scale; // 保留预制体的原始 scale

                // 4) 计算欧拉偏移的四元数
                //    math.radians 将度数转为弧度
                quaternion eulerOffsetQuat = quaternion.EulerXYZ(
                    math.radians(rotationOffsetEuler)
                );

                //4-1) 范围位置随机化
                float2 randomInCircle = UnityEngine.Random.insideUnitCircle * 10f*(1+ _heroAttributeCmptOriginal.gainAttribute.skillRange);
                heroPos += new float3(randomInCircle.x, 0,randomInCircle.y);//随机范围内进行相关的参数处理这里是400平方米

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



        #endregion

    }
}
