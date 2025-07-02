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
/// ����Ӣ�ۼ��ܵĺ����࣬��Ӣ��Mono�ű��г�ʼ��֮��ͨ����ȡ�����ӹ��캯�����г�ʼ��
/// </summary>
    public class HeroSkills : Singleton<HeroSkills>
    {
        ScenePrefabsSingleton _skillPrefabs;
        EntityManager _entityManager;
        //����λ��
        Transform _transform;
        //Ӣ������,��������ԣ�����ֻ������ֻ����ִ�й����У�Ӧ�ò��ò�ѯ����
       [ReadOnly] HeroAttributeCmpt _heroAttributeCmptOriginal;
        CoroutineController _coroutineController;

        Entity _heroEntity;
        //���ܲ�ѯģ��,����Ψһ
        EntityQuery _arcaneCircleQuery;
        //��Ӱ����������Ψһ
        EntityQuery _shadowTideQuery;

        //��̬Ӣ�۽ṹ��ѯģ��
        EntityQuery _heroRealTimeAttr;


        private HeroSkills()
        {


            //��ȡ�任
            _transform = Hero.instance.skillTransforms[0];

            //��ȡentityManager ������
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            //��ȡԤ����,Mono����Ҫʹ��entity ��ѯ������ص�ת��
            _skillPrefabs = _entityManager.CreateEntityQuery(typeof(ScenePrefabsSingleton)).GetSingleton<ScenePrefabsSingleton>();

            //Ӣ������
            _heroAttributeCmptOriginal = Hero.instance.attributeCmpt;

            //��ȡȫ��Э�̿�����
            _coroutineController =Hero.instance.coroutineController;


            //��ȡӢ��entity
            _heroEntity = Hero.instance.heroEntity;
            //������ѯ
            _arcaneCircleQuery = _entityManager.CreateEntityQuery(typeof(SkillArcaneCircleTag));
            //��Ӱ����
            _shadowTideQuery = _entityManager.CreateEntityQuery(typeof(SkillShadowTideTag));
            //ʵʱӢ�������ѯ
            _heroRealTimeAttr = _entityManager.CreateEntityQuery(typeof(HeroAttributeCmpt), typeof(HeroEntityMasterTag));
           

        }

        /// <summary>
        /// ���뼼��ID ������7�����ܱ仯���ͣ�Ĭ�϶�Ϊ�����ͣ���������λ�Ƽ���1�ֱ仯�����ļ���3�ֱ仯���ռ�����6�ֱ仯
        ///���е����ͷ��˺��༼�� Pulse���壬 
        ///
        /// </summary>
        /// <param name="iD"></param>
        /// <param name="psionicType"></param>

        public void RelasesHeroSkill(HeroSkillID iD, HeroSkillPsionicType psionicType = HeroSkillPsionicType.Basic)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            switch (iD)
                
            {
                //���壬˲ʱ
                case HeroSkillID.Pulse:
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:
                            var entity = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_Pulse, _transform.position, Hero.instance.transform.rotation, 1, 0, 0, 1, true, false);
                            //���Ӽ���ר�ñ�ǩ���ڼ����˶���    
                            _entityManager.AddComponentData(entity, new SkillPulseTag() { tagSurvivalTime = 3, speed = 5 });
                            break;
                        case HeroSkillPsionicType.PsionicA:
                            //�������׶�Ч��
                            var entity1 = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_Pulse, _transform.position, Hero.instance.transform.rotation, 1, 0, 0, 1, true, false);
                            //���Ӽ���ר�ñ�ǩ���ڼ����˶���    
                            _entityManager.AddComponentData(entity1, new SkillPulseTag() { tagSurvivalTime = 3, speed = 5, enableSecond = true, scaleChangePar = 1f });
                            _entityManager.AddComponentData(entity1, new SkillPulseSecondExplosionRequestTag { });
                            _entityManager.SetComponentEnabled<SkillPulseSecondExplosionRequestTag>(entity1, false);
                            break;
                        case HeroSkillPsionicType.PsionicB:

                            //����3��������
                            for (int i = 0; i < 3; i++)
                            {
                                var entity2 = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_Pulse, _transform.position, Hero.instance.transform.rotation, 1.4f, float3.zero, new float3(0, -30 + i * 30, 0), 0.5f, true, false);

                                _entityManager.AddComponentData(entity2, new SkillPulseTag() { tagSurvivalTime = 3, speed = 5, scaleChangePar = 0.5f });
                            }
                            break;
                        case HeroSkillPsionicType.PsionicAB:

                            //����3��������,��������״̬
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
                //���ܣ�˲ʱ
                case HeroSkillID.DarkEnergy:
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:
                            WeaponEnchantmentSkillDarkEnergy(5);
                            break;
                          //�������γ��ܣ����ż��ܵȼ��ɳ������ӳ��ܴ���
                        case HeroSkillPsionicType.PsionicA:
                            WeaponEnchantmentSkillDarkEnergy(7);
                            break;
                            //��Ӱ���ɵļ��ܣ�����Ӧ������һ���±�ǩ
                        case HeroSkillPsionicType.PsionicB:
                            WeaponEnchantmentSkillDarkEnergy(5);
                            break;
                        //��Ӱ���ɵļ��ܣ�����Ӧ������һ���±�ǩ
                        case HeroSkillPsionicType.PsionicAB:
                            Hero.instance.skillAttackPar.enableSpecialEffect = true;
                            WeaponEnchantmentSkillDarkEnergy(7);
                            break;
                    }

                    break;
                //����˲ʱ��
                case HeroSkillID.IceFire:
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:
                            var entityIce = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_IceFire, _transform.position, Hero.instance.transform.rotation, 1, new float3(0,0.3f,0), 0, 1, false, false);
                            //���Ӽ���ר�ñ�ǩ���ڼ����˶���    
                            _entityManager.AddComponentData(entityIce, new SkillIceFireTag() { tagSurvivalTime =20, speed = 3,radius=5 ,currentAngle=1.72f});
                            var entityFire = DamageSkillsFlightProp(_skillPrefabs.HeroSkillAssistive_IceFireFire, _transform.position, Hero.instance.transform.rotation, 1, new float3(0, 0.3f, 0), 0, 1, false, false);
                            //���Ӽ���ר�ñ�ǩ���ڼ����˶���    
                            _entityManager.AddComponentData(entityFire, new SkillIceFireTag() { tagSurvivalTime = 20, speed = 3, radius = 5, currentAngle = -1.72f });
                            break;
                            //���ױ����������˺����뾶��������ٶȡ�����ʱ��
                        case HeroSkillPsionicType.PsionicA:
                            var entityIce1 = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_IceFire, _transform.position, Hero.instance.transform.rotation, 1.3f, new float3(0, 0.5f, 0), 0, 1.5f, false, false);
                            //���Ӽ���ר�ñ�ǩ���ڼ����˶���    
                            _entityManager.AddComponentData(entityIce1, new SkillIceFireTag() { tagSurvivalTime = 25, speed = 4, radius = 7, currentAngle = 1.72f ,originalScale=2.6f});
                            var entityFire1 = DamageSkillsFlightProp(_skillPrefabs.HeroSkillAssistive_IceFireFire, _transform.position, Hero.instance.transform.rotation, 1.3f, new float3(0, 0.5f, 0), 0, 1.3f, false, false);
                            //���Ӽ���ר�ñ�ǩ���ڼ����˶���    
                            _entityManager.AddComponentData(entityFire1, new SkillIceFireTag() { tagSurvivalTime = 25, speed = 4, radius = 7, currentAngle = -1.72f , originalScale = 2.6f });
                            break;
                        case HeroSkillPsionicType.PsionicB:
                            var entityIce2 = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_IceFire, _transform.position, Hero.instance.transform.rotation, 1, new float3(0, 0.3f, 0), 0, 1, false, false);
                            //���Ӽ���ר�ñ�ǩ���ڼ����˶���    
                            _entityManager.AddComponentData(entityIce2, new SkillIceFireTag() { tagSurvivalTime = 25, speed = 4, radius = 7, currentAngle = 1.72f ,enableSecond=true,scaleChangePar =1,skillDamageChangeParTag=1, originalScale = 2f });
                            _entityManager.AddComponentData(entityIce2, new SkillIceFireSecondExplosionRequestTag { });
                            _entityManager.SetComponentEnabled<SkillIceFireSecondExplosionRequestTag>(entityIce2, false);
                            var entityFire2 = DamageSkillsFlightProp(_skillPrefabs.HeroSkillAssistive_IceFireFire, _transform.position, Hero.instance.transform.rotation, 1, new float3(0, 0.3f, 0), 0, 1, false, false);
                            //���Ӽ���ר�ñ�ǩ���ڼ����˶���    
                            _entityManager.AddComponentData(entityFire2, new SkillIceFireTag() { tagSurvivalTime = 25, speed = 4, radius = 7, currentAngle = -1.72f,enableSecond=true,scaleChangePar = 1, skillDamageChangeParTag = 1, originalScale = 2f });
                            _entityManager.AddComponentData(entityFire2, new SkillIceFireSecondExplosionRequestTag { });
                            _entityManager.SetComponentEnabled<SkillIceFireSecondExplosionRequestTag>(entityFire2, false);
                            break;
                        case HeroSkillPsionicType.PsionicAB:

                            var entityIce3 = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_IceFire, _transform.position, Hero.instance.transform.rotation, 0.3f, new float3(0, 1.3f, 0), 0, 1.5f, false, false);
                            //���Ӽ���ר�ñ�ǩ���ڼ����˶���    
                            _entityManager.AddComponentData(entityIce3, new SkillIceFireTag() { tagSurvivalTime = 30, speed = 6f, radius = 9, currentAngle = -1.72f ,enableSecond = true, scaleChangePar = 1, skillDamageChangeParTag = 1 , originalScale = 2.6f });
                            _entityManager.AddComponentData(entityIce3, new SkillIceFireSecondExplosionRequestTag { });
                            _entityManager.SetComponentEnabled<SkillIceFireSecondExplosionRequestTag>(entityIce3, false);
                            var entityFire3 = DamageSkillsFlightProp(_skillPrefabs.HeroSkillAssistive_IceFireFire, _transform.position, Hero.instance.transform.rotation, 0.3f, new float3(0, 1.3f, 0), 0, 1.5f, false, false);
                            //���Ӽ���ר�ñ�ǩ���ڼ����˶���    
                            _entityManager.AddComponentData(entityFire3, new SkillIceFireTag() { tagSurvivalTime = 30, speed = 6f, radius = 9, currentAngle = 1.72f, enableSecond = true, scaleChangePar = 1, skillDamageChangeParTag = 1, originalScale = 2.6f });
                            _entityManager.AddComponentData(entityFire3, new SkillIceFireSecondExplosionRequestTag { });
                            _entityManager.SetComponentEnabled<SkillIceFireSecondExplosionRequestTag>(entityFire3, false);
                            break;
                    }
                    break;

        
               //���ף�˲ʱ
                case HeroSkillID.ThunderStrike:

                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:
                            
                            //�ͷų������׼���,���Ը���������
                             DamageSkillsFlightPropConsecutiveCasting<SkillThunderStrikeTag>(_skillPrefabs.HeroSkill_ThunderStrike,new SkillThunderStrikeTag() {tagSurvivalTime=0.5f},12,1, _transform.position, 
                                 Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false,false);                      

                            break;
                        case HeroSkillPsionicType.PsionicA:
                            //�ͷų������׼���,�������Ϊ��������,����Ӣ������+1
                           DamageSkillsFlightPropConsecutiveCasting<SkillThunderStrikeTag>(_skillPrefabs.HeroSkill_ThunderStrike, new SkillThunderStrikeTag() { tagSurvivalTime = 0.5f }, 16, 1, _transform.position,
                                Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false, true);

                            break;


                        case HeroSkillPsionicType.PsionicB:
                            //�ͷų������׼���,�������Ϊ��������
                            DamageSkillsFlightPropConsecutiveCasting<SkillThunderStrikeTag>(_skillPrefabs.HeroSkill_ThunderStrike, new SkillThunderStrikeTag() { tagSurvivalTime = 1f }, 12, 0.5f, _transform.position,
                                 Hero.instance.transform.rotation, 1.3f, float3.zero, float3.zero, 1, false, false, false);
                            break;

                        case HeroSkillPsionicType.PsionicAB:

                            //�ͷų������׼���,�������Ϊ��������
                            DamageSkillsFlightPropConsecutiveCasting<SkillThunderStrikeTag>(_skillPrefabs.HeroSkill_ThunderStrike, new SkillThunderStrikeTag() { tagSurvivalTime = 1f }, 16, 0.5f, _transform.position,
                                 Hero.instance.transform.rotation, 1.3f, float3.zero, float3.zero, 1, false, false, true);
                 
 
                            break;

                    }


                    break;
                //���󣬳���
                case HeroSkillID.ArcaneCircle:

                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:

                            //������������20 ���÷��󣬿ɷ����ɹرգ�
                            bool hasArcaneCircle = _arcaneCircleQuery.CalculateEntityCount() > 0;
                            
                            //�����ȡ�����е���ʵentity
                            var realAttr = _entityManager.GetComponentData<HeroAttributeCmpt>(_heroEntity);
                            if (!hasArcaneCircle)
                            {
                                if (realAttr.defenseAttribute.energy >= 20)
                                {
                                   //�����򽵵�10����������3��
                                    realAttr.defenseAttribute.energy -= 20; 
                                    _entityManager.SetComponentData(_heroEntity, realAttr);
                                    var entityArcaneCircle = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ArcaneCircle, _transform.position, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                                    _entityManager.AddComponentData(entityArcaneCircle, new SkillArcaneCircleTag() { tagSurvivalTime = 3 });
                                }
                            }
                            else
                            {
                                //�ٴε���ֶ��ر�
                                var arcaneCircleEntity = _arcaneCircleQuery.GetSingletonRW<SkillArcaneCircleTag>();

                                arcaneCircleEntity.ValueRW.closed = true;
                            
                            }
                            break;
                        case HeroSkillPsionicType.PsionicA:


                            //������������20 ���÷��󣬿ɷ����ɹرգ�
                            bool hasArcaneCircleA = _arcaneCircleQuery.CalculateEntityCount() > 0;

                            //�����ȡ�����е���ʵentity
                            var realAttrA = _entityManager.GetComponentData<HeroAttributeCmpt>(_heroEntity);
                            if (!hasArcaneCircleA)
                            {
                                if (realAttrA.defenseAttribute.energy >= 20)
                                {
                                    //�����򽵵�10����������3��
                                    realAttrA.defenseAttribute.energy -= 20;
                                    _entityManager.SetComponentData(_heroEntity, realAttrA);
                                    var entityArcaneCircle = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ArcaneCircle, _transform.position, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                                    _entityManager.AddComponentData(entityArcaneCircle, new SkillArcaneCircleTag() { tagSurvivalTime = 3 ,enableSecondA=true});
                                    _entityManager.AddComponentData(entityArcaneCircle, new SkillArcaneCircleSecondTag());//���ӱ�ʶ�������ռ���ײ��
                                    _entityManager.AddBuffer<SkillArcaneCircleSecondBufferTag>(entityArcaneCircle);//���Ӽ���ר��buffer��ǩ�����ڹ�������Ч���Ļ�������
                                    //��������Ч����Ⱦ��ǩ
                                  //  ecb.AddComponent(_entityManager.GetBuffer<LinkedEntityGroup>(_heroEntity)[1].Value, new HeroEffectsLinked());
                                }
                            }
                            else
                            {
                                //�ٴε���ֶ��ر�
                                var arcaneCircleEntity = _arcaneCircleQuery.GetSingletonRW<SkillArcaneCircleTag>();
                                arcaneCircleEntity.ValueRW.closed = true;
                                //�رշ���ʱ����Ҫ��������Ϊ0
                               // ecb.SetComponentEnabled<HeroEffectsLinked>(_entityManager.GetBuffer<LinkedEntityGroup>(_heroEntity)[1].Value, false);
                            }

                            break;


                        case HeroSkillPsionicType.PsionicB:

                            //������������20 ���÷��󣬿ɷ����ɹرգ�
                            bool hasArcaneCircleB = _arcaneCircleQuery.CalculateEntityCount() > 0;

                            //�����ȡ�����е���ʵentity
                            var realAttrB = _entityManager.GetComponentData<HeroAttributeCmpt>(_heroEntity);
                            if (!hasArcaneCircleB)
                            {
                                if (realAttrB.defenseAttribute.energy >= 20)
                                {
                                    //�����򽵵�10����������3��
                                    realAttrB.defenseAttribute.energy -= 20;
                                    _entityManager.SetComponentData(_heroEntity, realAttrB);
                                    //����Ӵ�50%
                                    var entityArcaneCircle = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ArcaneCircle, _transform.position, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1.5f, false, false);
                                    _entityManager.AddComponentData(entityArcaneCircle, new SkillArcaneCircleTag() { tagSurvivalTime = 3,enableSecondB=true });
                                }
                            }
                            else
                            {
                                //�ٴε���ֶ��ر�
                                var arcaneCircleEntity = _arcaneCircleQuery.GetSingletonRW<SkillArcaneCircleTag>();
                                arcaneCircleEntity.ValueRW.closed = true;

                            }

                                break;

                        case HeroSkillPsionicType.PsionicAB:

                            //������������20 ���÷��󣬿ɷ����ɹرգ�
                            bool hasArcaneCircleAB = _arcaneCircleQuery.CalculateEntityCount() > 0;
                            //�����ȡ�����е���ʵentity
                            var realAttrAB = _entityManager.GetComponentData<HeroAttributeCmpt>(_heroEntity);
                            if (!hasArcaneCircleAB)
                            {
                                if (realAttrAB.defenseAttribute.energy >= 20)
                                {
                                    //�����򽵵�10����������3��
                                    realAttrAB.defenseAttribute.energy -= 20;
                                    _entityManager.SetComponentData(_heroEntity, realAttrAB);
                                    var entityArcaneCircle = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ArcaneCircle, _transform.position, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1.5f, false, false);
                                    _entityManager.AddComponentData(entityArcaneCircle, new SkillArcaneCircleTag() { tagSurvivalTime = 3, enableSecondA = true,enableSecondB=true });
                                    _entityManager.AddComponentData(entityArcaneCircle, new SkillArcaneCircleSecondTag());//���ӱ�ʶ�������ռ���ײ��
                                    _entityManager.AddBuffer<SkillArcaneCircleSecondBufferTag>(entityArcaneCircle);//���Ӽ���ר��buffer��ǩ�����ڹ�������Ч���Ļ�������
                         
                                }
                            }
                            else
                            {
                                //�ٴε���ֶ��ر�
                                var arcaneCircleEntity = _arcaneCircleQuery.GetSingletonRW<SkillArcaneCircleTag>();
                                arcaneCircleEntity.ValueRW.closed = true;
                                //�رշ���ʱ����Ҫ��������Ϊ0
                             //   ecb.SetComponentEnabled<HeroEffectsLinked>(_entityManager.GetBuffer<LinkedEntityGroup>(_heroEntity)[1].Value, false);
                            }


                            break;

                    }


                    break;
                //������˲ʱ    
                case HeroSkillID.Frost:
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:
                            WeaponEnchantmentSkillFrost();
                            break;
                        //���ӷ��ѹ���
                        case HeroSkillPsionicType.PsionicA:
                            WeaponEnchantmentSkillFrost( true,5,5);
                            break;
                        //������Ƭ����,���ܷ��ѣ����ǿ��Զ���
                        case HeroSkillPsionicType.PsionicB:
                            WeaponEnchantmentSkillFrost(false,5,2);
                            Hero.instance.skillAttackPar.tempFreeze = 101;
                            Hero.instance.skillAttackPar.enableSpecialEffect = true;
                            break;
                        //���Ѻ���Ƭ������
                        case HeroSkillPsionicType.PsionicAB:
                            WeaponEnchantmentSkillFrost(true, 15, 17,0.1f);
                            Hero.instance.skillAttackPar.tempFreeze = 101;
                            Hero.instance.skillAttackPar.enableSpecialEffect = true;
                            break;
                    }

                    break;
                //Ԫ�ع���,���������Ǽ��ܱ�ǩ����������˺�,���Զ�ȡ�ȼ�չʾ�˺�
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
                //����������˲ʱ������
                case HeroSkillID.ElectroCage:

                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:

                            var entityElectroCage = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ElectroCage, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            //���춨��Ч��
                            var skillPar = _entityManager.GetComponentData<SkillsDamageCalPar>(entityElectroCage);
                            skillPar.tempRoot = 101;
                            skillPar.tempStun = 101;
                            _entityManager.SetComponentData(entityElectroCage, skillPar);
                            _entityManager.AddComponentData(entityElectroCage, new SkillElectroCageTag() { tagSurvivalTime = 4 });
                            break;

                            //�������׶Σ�����100%
                        case HeroSkillPsionicType.PsionicA:

                            var entityElectroCageA = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ElectroCage, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            //���춨��Ч��
                            var skillParA = _entityManager.GetComponentData<SkillsDamageCalPar>(entityElectroCageA);
                            skillParA.tempRoot = 101;
                            skillParA.tempStun = 101;
                            _entityManager.SetComponentData(entityElectroCageA, skillParA);
                            _entityManager.AddComponentData(entityElectroCageA, new SkillElectroCageTag() { tagSurvivalTime = 4,enableSecondA =true,skillDamageChangeParTag=2,intervalTimer=0.2f });
                            break;
                            //���������׶Σ����紫��
                        case HeroSkillPsionicType.PsionicB:

                            var entityElectroCageB = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ElectroCage, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            //���춨��Ч��
                            var skillParB = _entityManager.GetComponentData<SkillsDamageCalPar>(entityElectroCageB);
                            skillParB.tempRoot = 101;
                            skillParB.tempStun = 101;
                            _entityManager.SetComponentData(entityElectroCageB, skillParB);
                            _entityManager.AddComponentData(entityElectroCageB, new SkillElectroCageTag() { tagSurvivalTime = 4,enableSecondB=true });
                            break;

                        //�������׶Σ�����100%,�������紫����ʶ
                        case HeroSkillPsionicType.PsionicAB:

                            var entityElectroCageAB = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_ElectroCage, Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            //���춨��Ч��
                            var skillParAB = _entityManager.GetComponentData<SkillsDamageCalPar>(entityElectroCageAB);
                            skillParAB.tempRoot = 101;
                            skillParAB.tempStun = 101;
                            _entityManager.SetComponentData(entityElectroCageAB, skillParAB);
                            _entityManager.AddComponentData(entityElectroCageAB, new SkillElectroCageTag() { tagSurvivalTime = 4, enableSecondA = true,enableSecondB=true, skillDamageChangeParTag = 2, intervalTimer = 0.2f });
                            break;


                    }

                    break;
                //��������,˲ʱ����������һ��3�ױ仯����
                case HeroSkillID.MineBlast:
                    switch (psionicType)
                    {
                        //����3�Ŷ�����  �Ƿ��б�Ҫ�����÷�ʽ���Ƿ���ṩ��ת�������ݲ��ṩЧ���� ԭ�������Դ�2�ױ�ըЧ��,���ӱ�ըЧ����ǩ�����ڱ�ը����ʱ��,300%��ը�˺�����
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
                            //ʮ�ֵ���
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
                        //ʮ�ֵ��׼Ӻ�������
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
                            //�ռ�������Ч��
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
                //��Ӱ������˲ʱ,����������
                case HeroSkillID.ShadowTide:
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:

                            //������������
                            bool hasShadowTide = _shadowTideQuery.CalculateEntityCount() > 0;

                            //�����ȡ�����е���ʵentity
                            var realAttr = _entityManager.GetComponentData<HeroAttributeCmpt>(_heroEntity);
                            if (!hasShadowTide)
                            {
                                if (realAttr.defenseAttribute.energy >= 20)
                                {
                                    realAttr.defenseAttribute.energy -= 20;
                                    _entityManager.SetComponentData(_heroEntity, realAttr);
                                    var filter = new CollisionFilter
                                    {
                                        //���ڵ��߲�
                                        BelongsTo = 1u << 10,
                                        //������
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
                                //�ٴε���ֶ��ر�
                                var shadowTideEntity = _shadowTideQuery.GetSingletonRW<SkillShadowTideTag>();

                                shadowTideEntity.ValueRW.closed = true;

                            }
                            break;
                        case HeroSkillPsionicType.PsionicA:

                            //������������20 
                            bool hasShadowTideA = _shadowTideQuery.CalculateEntityCount() > 0;

                            //�����ȡ�����е���ʵentity
                            var realAttrA = _entityManager.GetComponentData<HeroAttributeCmpt>(_heroEntity);
                            if (!hasShadowTideA)
                            {
                                if (realAttrA.defenseAttribute.energy >= 20)
                                {
                                    realAttrA.defenseAttribute.energy -= 20;
                                    _entityManager.SetComponentData(_heroEntity, realAttrA);
                                    var filter = new CollisionFilter
                                    {
                                        //���ڵ��߲�
                                        BelongsTo = 1u << 10,
                                        //������
                                        CollidesWith = 1u << 6,
                                        GroupIndex = 0
                                    };
                                    var overlapA = new OverlapOverTimeQueryCenter { center = Hero.instance.transform.position, radius = 1, filter = filter, offset = new float3(0, 0, 15), box = new float3(3, 3, 40), shape = OverLapShape.Box };
                                    var entityShadowTideA = DamageSkillsOverTimeProp(_skillPrefabs.HeroSkillAssistive_ShadowTideA, overlapA, Hero.instance.transform.position, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                                    _entityManager.AddComponentData(entityShadowTideA, new SkillShadowTideTag { tagSurvivalTime = 3, level = 1 ,skillDamageChangeParTag=2});
                                    var skillParA = _entityManager.GetComponentData<SkillsOverTimeDamageCalPar>(entityShadowTideA);
                                    //����������Ч
                                    skillParA.enablePull = true;
                                    //�˺�����
                                    skillParA.damageChangePar = 2;
                                    _entityManager.SetComponentData(entityShadowTideA, skillParA);
                                }
                            }
                            else
                            {
                                //�ٴε���ֶ��ر�
                                var shadowTideEntity = _shadowTideQuery.GetSingletonRW<SkillShadowTideTag>();

                                shadowTideEntity.ValueRW.closed = true;

                            }

                            break;

                        case HeroSkillPsionicType.PsionicB:

                            //������������20 
                            bool hasShadowTideB = _shadowTideQuery.CalculateEntityCount() > 0;

                            //�����ȡ�����е���ʵentity
                            var realAttrB = _entityManager.GetComponentData<HeroAttributeCmpt>(_heroEntity);
                            if (!hasShadowTideB)
                            {
                                if (realAttrB.defenseAttribute.energy >= 20)
                                {
                                    realAttrB.defenseAttribute.energy -= 20;
                                    _entityManager.SetComponentData(_heroEntity, realAttrB);
                                    var filter = new CollisionFilter
                                    {
                                        //���ڵ��߲�
                                        BelongsTo = 1u << 10,
                                        //������
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
                                //�ٴε���ֶ��ر�
                                var shadowTideEntity = _shadowTideQuery.GetSingletonRW<SkillShadowTideTag>();

                                shadowTideEntity.ValueRW.closed = true;

                            }

                            break;


                        case HeroSkillPsionicType.PsionicAB:

                            //������������20
                            bool hasShadowTideAB = _shadowTideQuery.CalculateEntityCount() > 0;

                            //�����ȡ�����е���ʵentity
                            var realAttrAB = _entityManager.GetComponentData<HeroAttributeCmpt>(_heroEntity);
                            if (!hasShadowTideAB)
                            {
                                if (realAttrAB.defenseAttribute.energy >= 20)
                                {
                                    realAttrAB.defenseAttribute.energy -= 20;
                                    _entityManager.SetComponentData(_heroEntity, realAttrAB);
                                    var filter = new CollisionFilter
                                    {
                                        //���ڵ��߲�
                                        BelongsTo = 1u << 10,
                                        //������
                                        CollidesWith = 1u << 6,
                                        GroupIndex = 0
                                    };
                                    var overlapAB = new OverlapOverTimeQueryCenter { center = Hero.instance.transform.position, radius = 1, filter = filter, offset = new float3(0, 0, 15), box = new float3(3, 3, 40), shape = OverLapShape.Box };
                                    var entityShadowTideAB = DamageSkillsOverTimeProp(_skillPrefabs.HeroSkillAssistive_ShadowTideA, overlapAB, Hero.instance.transform.position, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                                    _entityManager.AddComponentData(entityShadowTideAB, new SkillShadowTideTag { tagSurvivalTime = 3, level = 1, skillDamageChangeParTag = 2 ,enableSecondB=true});
                                    var skillParAB = _entityManager.GetComponentData<SkillsOverTimeDamageCalPar>(entityShadowTideAB);
                                    //����������Ч
                                    skillParAB.enablePull = true;
                                    //�˺�����
                                    skillParAB.damageChangePar = 2;
                                    _entityManager.SetComponentData(entityShadowTideAB, skillParAB);
                                }
                            }
                            else
                            {
                                //�ٴε���ֶ��ر�
                                var shadowTideEntity = _shadowTideQuery.GetSingletonRW<SkillShadowTideTag>();

                                shadowTideEntity.ValueRW.closed = true;

                            }

                            break;



                    }

                    break;
                //��˪���ǣ�˲ʱ    
                case HeroSkillID.FrostNova:
                    switch (psionicType)
                    {
                        case HeroSkillPsionicType.Basic:
                            var entityFrostNova = DamageSkillsFlightProp(_skillPrefabs.HeroSkill_FrostNova, _transform.position, Hero.instance.transform.rotation, 1, 0, 0, 1, true, false);
                            //���Ӽ���ר�ñ�ǩ���ڼ����˶���    
                            _entityManager.AddComponentData(entityFrostNova, new SkillPulseTag() { tagSurvivalTime = 3, speed = 5 });
                            


                            break;
                    }
                    break;
                //����,����,���ܸ����Ŀ��Ʋ����� ����ͨ�����ñ���������
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
                            //����A�׶α�ǩ�������ռ��жϣ���buffer�Ĵ����ṹ�������ڳ����Լ���
                            _entityManager.AddComponentData(entityPoisonRainA, new SkillPoisonRainATag { level = 1 });
                            break;
                            //���� B���ܴ���������Ч��
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
                            //���ӻ���ֵ
                            skillParB.tempStun = 200;
                            //���ӻ������
                            skillParB.fireDamage += skillParB.poisonDamage*(1+level*0.2f);
                            skillParB.fireDotDamage += skillParB.poisonDotDamage* (1 + level * 0.2f); 
                            _entityManager.SetComponentData(entityPoisonRainB, skillParB);

                            //--������,��������һ��Ч������ʵ�ʼ���
                            var entityPoisonRainBFire = DamageSkillsOverTimeProp(_skillPrefabs.HeroSkillAssistive_PoisonRainB,new OverlapOverTimeQueryCenter(), Hero.instance.skillTargetPositon, Hero.instance.transform.rotation, 1, float3.zero, float3.zero, 1, false, false);
                            _entityManager.AddComponentData(entityPoisonRainBFire, new SkillPoisonRainTag { tagSurvivalTime = 15, level = 1 });
                         
                            break;
                        //���� B���ܴ���������ռ�Ч��
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
                            //����A�׶α�ǩ�������ռ��жϣ���buffer�Ĵ����ṹ�������ڳ����Լ���
                            _entityManager.AddComponentData(entityPoisonRainAB, new SkillPoisonRainATag { level = 1 });
                            int levelAB = 3;
                            var skillParAB = _entityManager.GetComponentData<SkillsOverTimeDamageCalPar>(entityPoisonRainAB);
                            skillParAB.tempSlow = 30;
                            //���ӻ���ֵ
                            skillParAB.tempStun = 200;
                            //���ӻ������
                            skillParAB.fireDamage += skillParAB.poisonDamage * (1 + levelAB * 0.2f);
                            skillParAB.fireDotDamage += skillParAB.poisonDotDamage * (1 + levelAB * 0.2f);
                            _entityManager.SetComponentData(entityPoisonRainAB, skillParAB);

                            //--������,��������һ��Ч������ʵ�ʼ���
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
        /// ʵ��������ʼ��һ���˺��ͷ��м���ʵ�壨Pulse,PulseB��-
        /// ֧��λ�á���ת������ƫ�ƣ��Լ���ѡ������/����Ч����
        /// </summary>
        /// <param name="prefab">����Ԥ��ʵ�塣</param>
        /// <param name="positionOffset">����ռ�λ��ƫ�ƣ�Ĭ�� <c>float3.zero</c>��</param>
        /// <param name="rotationOffsetEuler">���Ӣ�۵�ŷ������תƫ�ƣ��ȣ���Ĭ�� <c>float3.zero</c>��</param>
        /// <param name="scaleFactor">�������ӣ�Ĭ�� <c>1f</c>��</param>
        /// <param name="enablePull">�Ƿ�������Ч����Ĭ�� <c>false</c>��</param>
        /// <param name="enableExplosion">�Ƿ�������/��ըЧ����Ĭ�� <c>fasle</c>��</param>
        /// <param name="enableSecond">�Ƿ������׶� <c>fasle</c>��</param>
        /// <returns>������ʵ������ʵ�塣</returns>
      public  Entity DamageSkillsFlightProp(
          Entity prefab,
          float3 posion,
          quaternion quaternion,
          float damageChangePar =1,//Ĭ���˺�����Ϊ1
          float3 positionOffset = default,
          float3 rotationOffsetEuler = default,  // �������
          float scaleFactor = 1f,bool enablePull =false,bool enableExplosion =false)
        {
            DevDebug.Log("�ͷ��˺��ͷ��м���");

            // 1) ʵ����
            var entity = _entityManager.Instantiate(prefab);

            // 2) ȡ���ɱ�� LocalTransform
            var transform = _entityManager.GetComponentData<LocalTransform>(entity);
            
    
            // 3) ��Ӣ�ۻ�ȡ����λ��/��ת/����
            float3 heroPos = posion;
            quaternion heroRot = quaternion;
            float baseScale = transform.Scale; // ����Ԥ�����ԭʼ scale

            // 4) ����ŷ��ƫ�Ƶ���Ԫ��
            //    math.radians ������תΪ����
            quaternion eulerOffsetQuat = quaternion.EulerXYZ(
                math.radians(rotationOffsetEuler)
            );

            // 5) ����ƫ��
            transform.Position = heroPos
                                + math.mul(heroRot, positionOffset);
            //����������ת
            var combineRotation = math.mul(heroRot, eulerOffsetQuat);
            //���ӱ�����ת
            transform.Rotation = math.mul(transform.Rotation, combineRotation);
            transform.Scale = baseScale * scaleFactor*(1+ _heroAttributeCmptOriginal.gainAttribute.skillRange);

            // 6) д�����
            _entityManager.SetComponentData(entity, transform);

            // 7) �����˺�����
            _entityManager.AddComponentData(entity, Hero.instance.skillsDamageCalPar);
       
            var skillPar = _entityManager.GetComponentData<SkillsDamageCalPar>(entity);
           
                skillPar.enablePull = enablePull;           
                skillPar.enableExplosion = enableExplosion;
                skillPar.damageChangePar= damageChangePar;         
            _entityManager.SetComponentData(entity, skillPar);

            // 8) ������ײ��¼������
            var hits = _entityManager.AddBuffer<HitRecord>(entity);
            _entityManager.AddBuffer<HitElementResonanceRecord>(entity);

            return entity;
        }
        /// <summary>
        /// �����Լ��ܣ�ȡ��hitRecord ��buffer����������system�н��и��£������ܿ��ջ��ƣ����꣩
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
         float damageChangePar = 1,//Ĭ���˺�����Ϊ1
         float3 positionOffset = default,
         float3 rotationOffsetEuler = default,  // �������
         float scaleFactor = 1f, bool enablePull = false, bool enableExplosion = false)
        {
            DevDebug.Log("�ͷ��˺��ͷ��м���");

            // 1) ʵ����
            var entity = _entityManager.Instantiate(prefab);

            // 2) ȡ���ɱ�� LocalTransform
            var transform = _entityManager.GetComponentData<LocalTransform>(entity);


            // 3) ��Ӣ�ۻ�ȡ����λ��/��ת/����
            float3 heroPos = posion;
            quaternion heroRot = quaternion;
            float baseScale = transform.Scale; // ����Ԥ�����ԭʼ scale

            // 4) ����ŷ��ƫ�Ƶ���Ԫ��
            //    math.radians ������תΪ����
            quaternion eulerOffsetQuat = quaternion.EulerXYZ(
                math.radians(rotationOffsetEuler)
            );

            // 5) ����ƫ��
            transform.Position = heroPos
                                + math.mul(heroRot, positionOffset);
            //����������ת
            var combineRotation = math.mul(heroRot, eulerOffsetQuat);
            //���ӱ�����ת
            transform.Rotation = math.mul(transform.Rotation, combineRotation);
            transform.Scale = baseScale * scaleFactor * (1 + _heroAttributeCmptOriginal.gainAttribute.skillRange);

            // 6) д�����
            _entityManager.SetComponentData(entity, transform);

            // 7) ���ӳ����˺�����
            _entityManager.AddComponentData(entity, Hero.instance.skillsOverTimeDamageCalPar);

            //8)���ӳ������˺�overlap���
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
        /// ���˺��������� ���ܵ��ߣ���Ԫ�ع�����
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
       float damageChangePar = 1,//Ĭ���˺�����Ϊ1
       float3 positionOffset = default,
       float3 rotationOffsetEuler = default,  // �������
       float scaleFactor = 1f, bool enablePull = false, bool enableExplosion = false)
        {
            DevDebug.Log("�ͷ��˺��ͷ��м���");

            // 1) ʵ����
            var entity = _entityManager.Instantiate(prefab);

            // 2) ȡ���ɱ�� LocalTransform
            var transform = _entityManager.GetComponentData<LocalTransform>(entity);


            // 3) ��Ӣ�ۻ�ȡ����λ��/��ת/����
            float3 heroPos = posion;
            quaternion heroRot = quaternion;
            float baseScale = transform.Scale; // ����Ԥ�����ԭʼ scale

            // 4) ����ŷ��ƫ�Ƶ���Ԫ��
            //    math.radians ������תΪ����
            quaternion eulerOffsetQuat = quaternion.EulerXYZ(
                math.radians(rotationOffsetEuler)
            );

            // 5) ����ƫ��
            transform.Position = heroPos
                                + math.mul(heroRot, positionOffset);
            //����������ת
            var combineRotation = math.mul(heroRot, eulerOffsetQuat);
            //���ӱ�����ת
            transform.Rotation = math.mul(transform.Rotation, combineRotation);
            transform.Scale = baseScale * scaleFactor * (1 + _heroAttributeCmptOriginal.gainAttribute.skillRange);

            // 6) д�����
            _entityManager.SetComponentData(entity, transform);


            return entity;
        }

        /// <summary>
        /// ��ը��ͨ�ü���
        /// ʵ��������ʼ��һ���˺��ͷ��м���ʵ�壨PulseC��-
        /// ֧��λ�á���ת������ƫ�ƣ��Լ���ѡ������/����Ч����
        /// </summary>
        /// <param name="prefab">����Ԥ�����ʵ��</param>
        /// <param name="position">Ӣ�۵�ǰλ��</param>
        /// <param name="rotation">Ӣ�۳�����Ԫ��</param>
        /// <param name="positionOffset">�����Ӣ��λ�õ�ƫ�ƣ�����ռ䣩</param>
        /// <param name="rotationOffsetEuler">�����Ӣ�۳����ŷ����ƫ�ƣ��ȣ�</param>
        /// <param name="scaleFactor">�������ӣ������Ԥ����ԭʼ Scale��</param>
        /// <param name="enablePull">�Ƿ�������Ч��</param>
        /// <param name="enableExplosion">�Ƿ�����ըЧ��</param>
        /// <returns>��ʵ��������ʵ�壨������������У��� Playback��</returns>
        public Entity SpawnDamageFlightSkillImmediate(
            Entity prefab,
            float3 position,
            quaternion rotation,
            float damageChangePar = 0,//Ĭ���˺�����Ϊ1
            float3 positionOffset = default,
            float3 rotationOffsetEuler = default,
            float scaleFactor = 1f,
            bool enablePull = false,
            bool enableExplosion = false
            )
        {
            // 1) ���� Instantiate
            var entity = _entityManager.Instantiate(prefab);

            // 2) �� prefab ԭʼ Transform
            var prefabTransform = _entityManager.GetComponentData<LocalTransform>(prefab);
            float baseScale = prefabTransform.Scale;

            // 3) ������ Transform
            quaternion offsetQuat = quaternion.EulerXYZ(math.radians(rotationOffsetEuler));
            var newTransform = new LocalTransform
            {
                Position = position + math.mul(rotation, positionOffset),
                Rotation = math.mul(rotation, offsetQuat),
                Scale = baseScale * scaleFactor*(1 + _heroAttributeCmptOriginal.gainAttribute.skillRange)
            };

            // 4) д��
            _entityManager.SetComponentData(entity, newTransform);

            // 5) �����˺�����
            var damagePar = Hero.instance.skillsDamageCalPar;
            damagePar.enablePull = enablePull;
            damagePar.enableExplosion = enableExplosion;
            damagePar.damageChangePar = damageChangePar;
            _entityManager.AddComponentData(entity, damagePar);
            // 6) ���ӻ�����
            var hits = _entityManager.AddBuffer<HitRecord>(entity);
            _entityManager.AddBuffer<HitElementResonanceRecord>(entity);


            return entity;
     
        }


        /// ECB �汾 ���� �ص�BASEϵͳ�����ⲿ���õ�����,��������ϵͳ����ר�õ����������ٳ�ʼ����ϵͳ�Ĵ���
        public Entity DamageSkillsExplosionProp(
            EntityCommandBuffer ecb,
            Entity prefab,
            float3 position,
            quaternion rotation,
            float damageChangePar = 1,//Ĭ���˺�����Ϊ1
            float3 positionOffset = default,
            float3 rotationOffsetEuler = default,
            float scaleFactor = 1f,
            bool enablePull = false,
            bool enableExplosion = false)
        {
            // 1) �ӳ�ʵ����
            var entity = ecb.Instantiate(prefab);

            // 2) ��ȡԤ���������е� LocalTransform������ȡ��������ֱ���� EntityManager
            var prefabTransform = _entityManager.GetComponentData<LocalTransform>(prefab);
            float baseScale = prefabTransform.Scale;

            // 3) �����µı任
            quaternion offsetQuat = quaternion.EulerXYZ(math.radians(rotationOffsetEuler));
            LocalTransform newTransform = new LocalTransform
            {
                Position = position + math.mul(rotation, positionOffset),
                Rotation = math.mul(rotation, offsetQuat),
                //�����ɼ��ܷ�Χ�������ܵ�Ӱ������
                Scale = baseScale * scaleFactor*(1 + _heroAttributeCmptOriginal.gainAttribute.skillRange)
            };

            // 4) д����ʵ��
            ecb.SetComponent(entity, newTransform);

            // 5) ���Ӳ���ʼ���˺��������
            var damagePar = new SkillsDamageCalPar { };
            damagePar = Hero.instance.skillsDamageCalPar;
            damagePar.enablePull = enablePull;
            damagePar.enableExplosion = enableExplosion;
            damagePar.damageChangePar = damageChangePar;
            ecb.AddComponent(entity, damagePar);

            // 6) ������ײ��¼������
            var hits = ecb.AddBuffer<HitRecord>(entity);
           ecb.AddBuffer<HitElementResonanceRecord>(entity);

            //д��
            return entity;
        }


        /// <summary>
        /// ������ħ�༼��,��ħ�����ܣ������丽ħʱ�䣬��ħ�༼�ܵ���Ч��
        /// </summary>
        public void WeaponEnchantmentSkillDarkEnergy(int darkEnergyCount)
        { 
                
            Hero.instance.skillAttackPar.darkEnergyCapacity = darkEnergyCount;

                Hero.instance.skillAttackPar.darkEnergyEnhantmentTimer = 15;

                                  
        }

        /// <summary>
        /// ������ħ�༼�ܣ� ����
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

        #region �������ͷŵĴ���Я����ļ���


        /// <summary>
        ///�����ͷż��ܣ���ΪЯ�̵ķ���ֵ���⣬����ֱ�Ӵ���1�׶μ��ܱ�ǩ
        ///���������������׼��ܿ�ʼ
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
    int castCount,//�ͷ��ܴ���
    float interval,//���
    float3 posion,
    quaternion quaternion,
    float damageChangePar = 0,//Ĭ���˺�����Ϊ0
    float3 positionOffset = default,
    float3 rotationOffsetEuler = default,  // �������
    float scaleFactor = 1f, bool enablePull = false, bool enableExplosion = false,bool fllow =false ) where T :unmanaged,IComponentData


        {

            var _rollCoroutineId = _coroutineController.StartRoutine(
                    ThunderStrikeSkill<T>(prefab,componentData, castCount, interval, posion,quaternion,damageChangePar,positionOffset,rotationOffsetEuler
                    ,scaleFactor,enablePull,enableExplosion,fllow),
                    tag: "ThunderStrikeSkill",
                    onComplete: () => {
                        DevDebug.Log("���������������ͷ����");
                    }
                );

        }
        #endregion

        #region Я������
        /// <summary>
        /// ICommonetData ������� unmanaged �ķ���Լ���������Ƿ�Χ�ͷ��Լ��ܣ� ������Χ���Ӽ��ܵ��ͷŷ�Χ
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
         int castCount,//�ͷ��ܴ���
         float interval,//���
         float3 posion,
         quaternion quaternion,
        float damageChangePar = 0,//Ĭ���˺�����Ϊ1
        float3 positionOffset = default,
        float3 rotationOffsetEuler = default,  // �������
        float scaleFactor = 1f, bool enablePull = false, bool enableExplosion = false ,bool fllow=false)where T : unmanaged, IComponentData
        {


            for (int i = 0; i < castCount; i++)
            {
                // 1) ʵ����
                var entity = _entityManager.Instantiate(prefab);

                // 2) ȡ���ɱ�� LocalTransform
                var transform = _entityManager.GetComponentData<LocalTransform>(entity);


                // 3) ��Ӣ�ۻ�ȡ����λ��/��ת/����
              
                float3 heroPos = posion;                                  
                quaternion heroRot = quaternion;


                //��������£��������ͷŵķ��е��ߵ�λ�� ����Ӣ���ƶ�����תҲ��һ����
                if (fllow)
                {
                    heroPos = Hero.instance.transform.position;
                    heroRot =Hero.instance.transform.rotation;
                }
                float baseScale = transform.Scale; // ����Ԥ�����ԭʼ scale

                // 4) ����ŷ��ƫ�Ƶ���Ԫ��
                //    math.radians ������תΪ����
                quaternion eulerOffsetQuat = quaternion.EulerXYZ(
                    math.radians(rotationOffsetEuler)
                );

                //4-1) ��Χλ�������
                float2 randomInCircle = UnityEngine.Random.insideUnitCircle * 10f*(1+ _heroAttributeCmptOriginal.gainAttribute.skillRange);
                heroPos += new float3(randomInCircle.x, 0,randomInCircle.y);//�����Χ�ڽ�����صĲ�������������400ƽ����

                // 5) ����ƫ��
                transform.Position = heroPos
                                    + math.mul(heroRot, positionOffset);
                //����������ת
                var combineRotation = math.mul(heroRot, eulerOffsetQuat);
                //���ӱ�����ת
                transform.Rotation = math.mul(transform.Rotation, combineRotation);
                transform.Scale = baseScale * scaleFactor * (1 + _heroAttributeCmptOriginal.gainAttribute.skillRange);

                // 6) д�����
                _entityManager.SetComponentData(entity, transform);

                // 7) �����˺�����
                _entityManager.AddComponentData(entity, Hero.instance.skillsDamageCalPar);

                var skillPar = _entityManager.GetComponentData<SkillsDamageCalPar>(entity);

                skillPar.enablePull = enablePull;
                skillPar.enableExplosion = enableExplosion;
                skillPar.damageChangePar = damageChangePar;
              //  skillPar.lightningDotDamage += 1;//�������ܳ����е�
                
                _entityManager.SetComponentData(entity, skillPar);

                // 8) ������ײ��¼������
                var hits = _entityManager.AddBuffer<HitRecord>(entity);
                _entityManager.AddBuffer<HitElementResonanceRecord>(entity);

                //Я����ֱ�����Ӽ��ܱ�ǩ
                _entityManager.AddComponentData<T>(entity, componentData);

               // DevDebug.Log("��  " + i + " ���ͷż���");
                yield return new WaitForSeconds(interval);
            }
        
        
        }



        #endregion

    }
}
