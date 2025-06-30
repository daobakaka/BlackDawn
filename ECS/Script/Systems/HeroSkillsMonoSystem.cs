using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using UnityEngine;
//���ڹ����ܵ��������ڼ�״̬
namespace BlackDawn.DOTS
{
    /// <summary>
    /// ���ܹ�����,�����⼼����֮����и���
    /// </summary>
    //���˺����㣬�ٸ���״̬
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(MainThreadSystemGroup))]
    public partial struct HeroSkillsMonoSystem : ISystem,ISystemStartStop
    {
        ComponentLookup<LocalTransform> _transform;
        ComponentLookup<MonsterLossPoolAttribute> _monsterLossPoolAttrLookup;
        ComponentLookup<HeroAttributeCmpt> _heroAttribute;
        //��ȡ���ܵ����ϵ�buffer������ʵ�ְ�Ӱ������Ч��
        BufferLookup<HitRecord> _hitBuffer;
        float3 _heroPosition;
        Entity _heroEntity;
        EntityManager _entityManager;
        HeroAttributeCmpt _heroAttributeCmptOriginal;

      public  void OnCreate(ref SystemState state) 
        {

           // state.Enabled = false;
            //���ⲿ����
           state.RequireForUpdate<EnableHeroSkillsMonoSystemTag>();
           state.Enabled = false;

          _transform= state.GetComponentLookup<LocalTransform>(true);
          _monsterLossPoolAttrLookup = state.GetComponentLookup<MonsterLossPoolAttribute>(false);
            _heroAttribute = state.GetComponentLookup<HeroAttributeCmpt>(true);
            _entityManager = state.EntityManager;
         // _hitBuffer = state.GetBufferLookup<HitRecord>(true);   
        
        }

        public void OnStartRunning(ref SystemState state)
        {
            _heroEntity = SystemAPI.GetSingletonEntity<HeroEntityMasterTag>();
            _heroAttributeCmptOriginal = Hero.instance.attributeCmpt;

            DevDebug.Log("����SkillMonoϵͳ");
        }



        [BurstCompile]
      public  void OnUpdate(ref SystemState state) 
        
        {
            //����λ��
            _transform.Update(ref state);
            _monsterLossPoolAttrLookup.Update(ref state);
            _heroAttribute.Update(ref state);
            // _hitBuffer.Update(ref state);
     


         //���߳��߼����ÿ�ͷд
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            
        
                
           


            var timer = SystemAPI.Time.DeltaTime;
            //������Ҫ����,��ѯӢ�۵�λ��
           // var heroEntity = SystemAPI.GetSingletonEntity<HeroEntityMasterTag>();
            quaternion rot = _transform[_heroEntity].Rotation;
            _heroPosition = _transform[_heroEntity].Position;
            //��ȡӢ������
            var heroPar = _heroAttribute[_heroEntity];
            //��ȡӢ��װ�صļ��ܵȼ�
            var level = _heroAttribute[_heroEntity].skillDamageAttribute.skillLevel;
            var prefab =SystemAPI.GetSingleton<ScenePrefabsSingleton>();


            //���弼�ܴ���
            foreach (var (skillTag ,skillCal,transform,collider,entity)
                  in SystemAPI.Query<RefRW<SkillPulseTag> ,RefRW<SkillsDamageCalPar>,RefRW<LocalTransform>,RefRW<PhysicsCollider>>().WithEntityAccess())
            {
                //���±�ǩ�ļ����˺������������ж�̬�ı仯�ٸ���
              //  skillCal.ValueRW.damageChangePar = skillTag.ValueRW.skillDamageChangeParTag;
                // 2) ���㡰ǰ����������
                float3 forward = math.mul(transform.ValueRO.Rotation, new float3(0f, 0f, 1f));
                // 3) ����ǰ���ƶ�
                transform.ValueRW.Position += forward *skillTag.ValueRW.speed * timer;

                skillTag.ValueRW.tagSurvivalTime -= timer; 

                //����ʱ�����3�룬oncheck�رգ����������ڶ��׶� ������ӵڶ��׶α�ը�����ǩ,ȡ�����٣����ڱ�ը��Ⱦ�߼�����
                if (skillTag.ValueRW.tagSurvivalTime <=0)
                {
                    if (skillTag.ValueRW.enableSecond)
                        //ֱ�ӿ��ر�ǩ������ṹ�Ըı�
                    ecb.SetComponentEnabled<SkillPulseSecondExplosionRequestTag>(entity, true);
                    else
                    {

    
                        //ecb.DestroyEntity(entity);
                        skillCal.ValueRW.destory = true;    

                    }                
                }

            }
            //���ܼ��ܴ���,DymicalBuffer<...>����ֻ���õ�ֻ���ģ���������Ҫ�ڷ����ڲ�ʹ����ʾ��SystemAPI ��ִ��
            //�������Ի����������
            foreach (var (skillTag,skillCal, transform, collider,entity)
                 in SystemAPI.Query<RefRW<SkillDarkEnergyTag>,RefRW<SkillsDamageCalPar>, RefRW<LocalTransform>, RefRW<PhysicsCollider>>().WithEntityAccess())
            {
     
                // 2) ���㡰ǰ����������
                float3 forward = math.mul(transform.ValueRO.Rotation, new float3(0f, 0f, 1f));
                // 3) ����ǰ���ƶ�
                transform.ValueRW.Position += forward * skillTag.ValueRW.speed * timer;

                skillTag.ValueRW.tagSurvivalTime -= timer;


                // 5) �����Ҫ��������Ч�����ͱ��� hitBuffer����ÿ��Ԫ�ص� universalJudgment ��ֵ
                if (skillTag.ValueRO.enableSpecialEffect)
                {
                    // 1) ÿ��ѭ������ʽ���� entity �û���
                    var buffer = SystemAPI.GetBuffer<HitRecord>(entity);
                    // 2) ��Ҫ��һ����ʱ�������и���֮����д��
                    if (skillTag.ValueRO.enableSpecialEffect)
                    {
                        for (int i = 0; i < buffer.Length; i++)
                        {          
                            if (!buffer[i].universalJudgment)
                            {
                                //����ֻ��û���жϹ���������һ�β����жϣ���ʡ����Ҫ�Ŀ�����Ҳ�����ۼӰ�Ӱ��
                                var monsterAttr =  _monsterLossPoolAttrLookup[buffer[i].other];
                                HitRecord temp = buffer[i];
                                temp.universalJudgment = true;
                                //��Ӱֵ>50ʱ��ȡ
                                if (monsterAttr.shadowPool > 50)
                                {                                 
                                    //����һ���˺�����
                                    skillCal.ValueRW.damageChangePar *= (1 + (monsterAttr.shadowPool * level / 10000));
                                    //���ù����Ӧ�İ�Ӱ�ص�ֵΪ0
                                    monsterAttr.shadowPool = 0;
                                    //����д�� ���޸�һ���� ���ڿ���������
                                    ecb.SetComponent(buffer[i].other, monsterAttr);
                                    //���ﲥ�Ű�Ӱ������Ч��                     
                                }
                                buffer[i] = temp;
                            }
                        }
                    }
                }
                //����ʱ�����3�룬oncheck�رգ����������ڶ��׶� ������ӵڶ��׶α�ը�����ǩ,ȡ�����٣����ڱ�ը��Ⱦ�߼�����
                if (skillTag.ValueRW.tagSurvivalTime <= 0)
                {
                  
                      
                       // ecb.DestroyEntity(entity);
                    skillCal.ValueRW.destory = true;
                 
                    
                }

            }

            //�����ܴ�����ת

                 foreach (var (skillTag,skillCal, transform, collider,entity)
                 in SystemAPI.Query<RefRW<SkillIceFireTag>,RefRW<SkillsDamageCalPar>, RefRW<LocalTransform>, RefRW<PhysicsCollider>>().WithEntityAccess())
            {
                //���±�ǩ�ļ����˺����������ж�̬�ı仯�ٸ���
               // skillCal.ValueRW.damageChangePar =skillTag.ValueRW.skillDamageChangeParTag;              
                float radius = skillTag.ValueRO.radius;
               ref float angle = ref skillTag.ValueRW.currentAngle;

                //��������ڶ��׶α�ʶ���ҿ�������Ч��,4��ִ��һ�α�ը�ж�
                if (skillTag.ValueRO.enableSecond&&skillTag.ValueRO.secondSurvivalTime<0)
                {
                                   
                    var buffer = SystemAPI.GetBuffer<HitRecord>(entity);
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        if (!buffer[i].universalJudgment)
                        {
                            //ȡ���ж�Ч��
                            HitRecord temp = buffer[i];
                            temp.universalJudgment = true;
                            buffer[i] = temp;
                            //���ñ�ըЧ��
                            ecb.SetComponentEnabled<SkillIceFireSecondExplosionRequestTag>(entity,true);        
                            //����forѭ��
                            break;
                        }
                    }

                }
            
                // 2) ����Ƕ�������speed Ϊ����/�룩
                float deltaAngle = skillTag.ValueRW.speed * timer;
                angle += deltaAngle;
                if (angle > math.PI * 2f) angle -= math.PI * 2f;

                // 3) ֻ�� XZ ƽ�������ƫ��
                float x = math.cos(angle) * radius;
                float z = math.sin(angle) * radius;

                // 4) ԭ���� Y ����
                float y = transform.ValueRO.Position.y;

                // 5) ��ʵ��λ����Ϊ��Ӣ��λ�� + (x, 0, z)���ټ������� Y
                transform.ValueRW.Position = new float3(
                    _heroPosition.x + x,
                    y,
                    _heroPosition.z + z
                );

                // 6) �������ٴ��ʱ�䲢�������ٻ�ڶ��׶��߼�
                skillTag.ValueRW.tagSurvivalTime -= timer;
                if (skillTag.ValueRW.tagSurvivalTime <= 0f)
                {

                    //  ecb.DestroyEntity(entity);
                    skillCal.ValueRW.destory = true;

                    
                }
            }

            //���׼��ܴ���
            //���ﵽʱ�����ʧ
            foreach (var (skillTag, skillCal, transform, collider, entity)
                 in SystemAPI.Query<RefRW<SkillThunderStrikeTag>, RefRW<SkillsDamageCalPar>, RefRW<LocalTransform>, RefRW<PhysicsCollider>>().WithEntityAccess())
            {

                skillTag.ValueRW.tagSurvivalTime -= timer;
                if (skillTag.ValueRW.tagSurvivalTime < 0)
                {


                    //ecb.DestroyEntity(entity);
                    skillCal.ValueRW.destory = true;
                }


            }

            //�����ܣ������ֶ��ر�
            //���׶α���buffer,�����������ӣ����Ӹ��ݶ�̬Ч���ı䳤�̣�����6���Զ���ʧ���������ɣ����ǰ���buffer״̬������ʧ��������,����߼������⼼�������洦��
            foreach (var (skillTag, skillCal, transform, collider, entity)
       in SystemAPI.Query<RefRW<SkillArcaneCircleTag>, RefRW<SkillsDamageCalPar>, RefRW<LocalTransform>, RefRW<PhysicsCollider>>().WithEntityAccess())
            {         
                //����֮��ʼ������
                skillTag.ValueRW.tagSurvivalTime -= timer;
                if (skillTag.ValueRW.tagSurvivalTime <= 0)
                {
                    //ǯ�Ƶ�0
                    heroPar.defenseAttribute.energy = math.max(0, heroPar.defenseAttribute.energy - 3 * timer);

                    if (heroPar.defenseAttribute.energy <= 0)
                    {
                       // ecb.DestroyEntity(entity);
                        skillCal.ValueRW.destory = true;
                    }
                    ecb.SetComponent(_heroEntity, heroPar);
                }
                //���ڵڶ����ͷ��ֶ��ر�
                if(skillTag.ValueRO.closed)
                    skillCal.ValueRW.destory = true;
               // ecb.DestroyEntity(entity);

            }
         
            //������MonoЧ��
            SkillMonoFrost(ref state,ecb);
            //Ԫ�ع���MonoЧ��
            SkillMonoElementResonance(ref state,ecb);
            //���ܾ�������
            SkillMonoElectroCage(ref state,ecb,prefab);
            //��Ӱ����B�׶Σ�˲ʱ�˺���Ч����
            SkillMineBlastMono( ref state);






        }


         public   void OnDestroy(ref SystemState state) { }

        public void OnStopRunning(ref SystemState state)
        {
          
        }

        /// <summary>
        /// ����
        /// </summary>
        /// <param name="state"></param>
        /// <param name="ecb"></param>
        void SkillMonoFrost(ref SystemState state, EntityCommandBuffer ecb)
        {


            //һ����״����
            foreach (var (skillTag, skillCal, transform, collider, entity)
              in SystemAPI.Query<RefRW<SkillFrostTag>, RefRW<SkillsDamageCalPar>, RefRW<LocalTransform>, RefRW<PhysicsCollider>>().WithEntityAccess())
             {
                // 1. ���淢��ʱ��ԭ��Ϳ�ʼʱ�䣨ֻ�ڵ�һ��ִ��ʱд�룩
                if (skillTag.ValueRO.tagSurvivalTime==10)
                {
                    skillTag.ValueRW.originalPosition = transform.ValueRO.Position;
                }
                // 2. ���Ӵ��ʱ��
                skillTag.ValueRW.tagSurvivalTime -= SystemAPI.Time.DeltaTime;
                // 3. ��ȡ origin��t
                float3 origin = skillTag.ValueRO.originalPosition;
                float t = 10-skillTag.ValueRO.tagSurvivalTime;

                // 5. �� speed ֱ����Ϊ���ٶȣ��뾶 r = speed * t
                float r = skillTag.ValueRO.speed * t;
                // 6. ���ٶȱ��ֲ��䣨�����޸ģ�
                float angularSpeed = math.radians(90f); // 90��/s
                float theta = angularSpeed * t;

                // 7. �� XZ ƽ���������ƫ�ƣ�Y ���䣩
                float3 offset = new float3(
                    r * math.cos(theta),
                    0f,
                    r * math.sin(theta)
                );
                // 7. ����λ��
                transform.ValueRW.Position = origin + offset;

                // 8. ��ʱ����
                if (t >= 10f)
                {
                    ecb.DestroyEntity(entity);
                }

                //�����ڶ��׶Σ�������Ƭ����
                if (skillTag.ValueRO.enableSecond)
                {                 
                //���� ��ͬ����������Ƭ
                if(skillCal.ValueRW.hit ==true&& skillTag.ValueRW.hitCount>0)
                    {                        
                        skillCal.ValueRW.hit = false;
                        var prefab = SystemAPI.GetSingleton<ScenePrefabsSingleton>();

                        //����һ�κ�����Ƭ�ļ���

                        skillTag.ValueRW.hitCount--;

                        for (int i = 0; i < skillTag.ValueRO.shrapnelCount; i++)
                        {

                           var fragIce= ecb.Instantiate(prefab.HeroSkillAssistive_Frost);

                            var trs = transform.ValueRW;
                            trs.Scale = 1;
                            float angleDeg = 360f / skillTag.ValueRO.shrapnelCount * i;
                            float angleRad = math.radians(angleDeg);

                            // 3. ������ Y �����Ԫ���������� trs.Rotation
                            trs.Rotation = quaternion.EulerXYZ(0f, angleRad, 0f);
                            ecb.AddComponent(fragIce, trs);                           
                            //��Ӻ�����Ƭ�ı�ǩ,�����˺���ǩ���˺�ֵ
                            ecb.AddComponent(fragIce , new SkillFrostShrapneTag() { speed =20,tagSurvivalTime =1});
                            var newCal = skillCal.ValueRW;
                            // 2. �޸��ֶΣ�������Ƭ�̳�20%����ֵ
                            newCal.damageChangePar = skillTag.ValueRO.skillDamageChangeParTag;
                            if(skillTag.ValueRO.enableSpecialEffect)
                            newCal.tempFreeze = 20;

                            // 3. �Ѹĺõ����������ʵ��  
                            ecb.AddComponent(fragIce, newCal);

                            var hits = ecb.AddBuffer<HitRecord>(fragIce);
                            hits.Capacity = 10;
                            ecb.AddBuffer<HitElementResonanceRecord>(fragIce);

                        }

                    }

                }

            }


            //������״����
            foreach (var (skillTag, skillCal, transform, collider, entity)
           in SystemAPI.Query<RefRW<SkillFrostShrapneTag>, RefRW<SkillsDamageCalPar>, RefRW<LocalTransform>, RefRW<PhysicsCollider>>().WithEntityAccess())
            {

                // 2) ���㡰ǰ����������
                float3 forward = math.mul(transform.ValueRO.Rotation, new float3(0f, 0f, 1f));
                // 3) ����ǰ���ƶ�
                transform.ValueRW.Position += forward * skillTag.ValueRW.speed * SystemAPI.Time.DeltaTime;

                skillTag.ValueRW.tagSurvivalTime -= SystemAPI.Time.DeltaTime;

                if (skillTag.ValueRO.tagSurvivalTime <= 0)
                {

                   // ecb.DestroyEntity(entity);
                    skillCal.ValueRW.destory = true;
                }



            }


        }



        /// <summary>
        /// Ԫ�ع���
        /// </summary>
        /// <param name="state"></param>
        /// <param name="ecb"></param>
        void SkillMonoElementResonance(ref SystemState state, EntityCommandBuffer ecb)
        {

            foreach (var (skillTag,skillCal,transform, collider, entity)
            in SystemAPI.Query<RefRW<SkillElementResonanceTag>, RefRW<SkillsDamageCalPar>,RefRW<LocalTransform>, RefRW<PhysicsCollider>>().WithEntityAccess())
            {

                skillTag.ValueRW.tagSurvivalTime -= SystemAPI.Time.DeltaTime;

                if (skillTag.ValueRW.tagSurvivalTime <= 0)
                   // ecb.DestroyEntity(entity);
                skillCal.ValueRW.destory = true;

            }


        }

        /// <summary>
        /// ��������������4�룿
        /// </summary>
        /// <param name="state"></param>
        /// <param name="ecb"></param>
        void SkillMonoElectroCage(ref SystemState state, EntityCommandBuffer ecb,ScenePrefabsSingleton prefabs)
        {
            var rng = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, int.MaxValue));

            foreach (var (skillTag,damagePar ,transform, collider, entity)
            in SystemAPI.Query<RefRW<SkillElectroCageTag>,RefRW<SkillsDamageCalPar>, RefRW<LocalTransform>, RefRW<PhysicsCollider>>().WithEntityAccess())
            {

                skillTag.ValueRW.tagSurvivalTime -= SystemAPI.Time.DeltaTime;


                if (skillTag.ValueRW.tagSurvivalTime <= 0)
                {
                    // ecb.DestroyEntity(entity);
                    damagePar.ValueRW.destory = true;
                    continue;
                }
                //�ڶ��׶��ױ�����
                if (skillTag.ValueRO.enableSecondA)
                {
                    skillTag.ValueRW.timerA += SystemAPI.Time.DeltaTime;

                    if (skillTag.ValueRW.timerA >= skillTag.ValueRW.intervalTimer)
                    {
                        skillTag.ValueRW.timerA = 0;

                      //  1.ʵ�����������绡
                        var arcEntity = ecb.Instantiate(prefabs.HeroSkillAssistive_ElectroCage_Lightning);

                        // 2. LocalTransform ���ƫ�� XZ ��10
                        var newTransform = transform.ValueRO;
                        float xOffset = rng.NextFloat(-7f, 7f);
                        float zOffset = rng.NextFloat(-7f, 7f);
                        newTransform.Position.x += xOffset;
                        newTransform.Position.z += zOffset;
                        ecb.SetComponent(arcEntity, newTransform);

                        // 3. �����˺�����������+���ƣ�
                        var newSkillPar = damagePar.ValueRO;
                        //�ڶ��׶ν����ױ�����
                        newSkillPar.damageChangePar = skillTag.ValueRW.skillDamageChangeParTag;

                        ecb.AddComponent(arcEntity, newSkillPar);
                        //����ױ����ӡ��
                        ecb.AddComponent(arcEntity, new SkillElectroCageScoendTag() { tagSurvivalTime = 1 });

                        // 4. �����ײ��¼������
                        ecb.AddBuffer<HitRecord>(arcEntity);
                        ecb.AddBuffer<HitElementResonanceRecord>(arcEntity);


                    }


                }
                //�����׶ε�������,����һ�εĸ����ж�
                if (skillTag.ValueRO.enableSecondB)
                {
                    skillTag.ValueRW.timerB += SystemAPI.Time.DeltaTime;


                    if (skillTag.ValueRW.timerB >= 1.99)
                    {
                        skillTag.ValueRW.timerB = 0;

                        var random = rng.NextFloat(0, 1);
                        //����ÿ�ν���20%
                        if (random <=( 0.5-skillTag.ValueRO.StackCount*0.05f))
                        {
                            float3 Offset = rng.NextFloat3(-15f, 15f);
                            //����һ�δ�������
                            skillTag.ValueRW.StackCount += 1;
                            float3 newPosition = transform.ValueRO.Position + new float3(Offset.x, 0, Offset.z);

                          var entityElectroCage =  DamageSkillsECSRelaseProp(ecb, prefabs.HeroSkill_ElectroCage, damagePar.ValueRO, newPosition, quaternion.identity);
                            int nextStackCount = skillTag.ValueRO.StackCount + 1;
                            if (skillTag.ValueRO.enableSecondA)
                            {
                                ecb.AddComponent(entityElectroCage, new SkillElectroCageTag() { tagSurvivalTime = 4, enableSecondA = true, enableSecondB = true, skillDamageChangeParTag = 2, intervalTimer = 0.2f ,StackCount=nextStackCount});
                            }
                            else
                            {
                                ecb.AddComponent(entityElectroCage, new SkillElectroCageTag() { tagSurvivalTime = 4, enableSecondB = true,StackCount=nextStackCount});

                            }
                        
                        }


                    }

                }



            }




            //�ױ������߼�
            foreach (var (skillTag,skillCal,entity)
          in SystemAPI.Query<RefRW<SkillElectroCageScoendTag>,RefRW<SkillsDamageCalPar>>().WithEntityAccess())
            {

                skillTag.ValueRW.tagSurvivalTime -= SystemAPI.Time.DeltaTime;
                if (skillTag.ValueRO.tagSurvivalTime <= 0)
                {
                    skillCal.ValueRW.destory = true;
                    //ecb.DestroyEntity(entity);
                    
                    continue;
                
                }

            }

        }

        /// <summary>
        ///�������׵�Mono ���ƣ�����ֱ��д�ڻص������棿��ը֮�����¸���ʱ�䣿
        /// </summary>
        /// <param name="state"></param>
        /// <param name="ecb"></param>

        void SkillMineBlastMono(ref SystemState state)
        {
            foreach (var (skillTag, skillCal, entity)
             in SystemAPI.Query<RefRW<SkillShadowTideBTag>, RefRW<SkillsDamageCalPar>>().WithEntityAccess())
            { 
               skillTag.ValueRW.tagSurvivalTime -=SystemAPI.Time.DeltaTime;

                if (skillTag.ValueRW.tagSurvivalTime <= 0)
                    skillCal.ValueRW.destory = true;
            
            }



        }

        /// <summary>
        /// ��Ӱ�����ڶ��׶�
        /// </summary>
        /// <param name="state"></param>
        /// <param name="ecb"></param>
        void SkillShadowTideBMono(ref SystemState state, EntityCommandBuffer ecb)
        {



        }



        /// <summary>
        /// Ӣ�ۼ���ECS �ͷ�ϵͳ(��������B����)
        /// </summary>
        /// <param name="ecb"></param>
        /// <param name="prefab"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="damageChangePar"></param>
        /// <param name="positionOffset"></param>
        /// <param name="rotationOffsetEuler"></param>
        /// <param name="scaleFactor"></param>
        /// <param name="enablePull"></param>
        /// <param name="enableExplosion"></param>
        /// <returns></returns>
        public Entity DamageSkillsECSRelaseProp(
         EntityCommandBuffer ecb,
         Entity prefab,
         SkillsDamageCalPar skillsDamageCal,
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
                Scale = baseScale * scaleFactor * (1 + _heroAttributeCmptOriginal.gainAttribute.skillRange)
            };

            // 4) д����ʵ��
            ecb.SetComponent(entity, newTransform);

            // 5) ��Ӳ���ʼ���˺�������������ÿ��ջ���
            ecb.AddComponent(entity, skillsDamageCal);

            // 6) �����ײ��¼������
            var hits = ecb.AddBuffer<HitRecord>(entity);
            ecb.AddBuffer<HitElementResonanceRecord>(entity);

            //д��
            return entity;
        }



    }
}