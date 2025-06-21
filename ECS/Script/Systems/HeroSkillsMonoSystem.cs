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
    [UpdateAfter(typeof(HeroSpecialSkillsDamageSystem))]
    [UpdateInGroup(typeof(ActionSystemGroup))]
    public partial struct HeroSkillsMonoSystem : ISystem,ISystemStartStop
    {
        ComponentLookup<LocalTransform> _transform;
        ComponentLookup<MonsterLossPoolAttribute> _monsterLossPoolAttrLookup;
        ComponentLookup<HeroAttributeCmpt> _heroAttribute;
        //��ȡ���ܵ����ϵ�buffer������ʵ�ְ�Ӱ������Ч��
        BufferLookup<HitRecord> _hitBuffer;
        float3 _heroPosition;
        Entity _heroEntity;

      public  void OnCreate(ref SystemState state) 
        {

           // state.Enabled = false;
            //���ⲿ����
           state.RequireForUpdate<EnableHeroSkillsMonoSystemTag>();
           state.Enabled = false;

          _transform= state.GetComponentLookup<LocalTransform>(true);
          _monsterLossPoolAttrLookup = state.GetComponentLookup<MonsterLossPoolAttribute>(false);
            _heroAttribute = state.GetComponentLookup<HeroAttributeCmpt>(true);   
         // _hitBuffer = state.GetBufferLookup<HitRecord>(true);   
        
        }

        public void OnStartRunning(ref SystemState state)
        {
            _heroEntity = SystemAPI.GetSingletonEntity<HeroEntityMasterTag>();
        

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
     


         
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            
        
                
           


            var timer = SystemAPI.Time.DeltaTime;
            //������Ҫ����,��ѯӢ�۵�λ��
           // var heroEntity = SystemAPI.GetSingletonEntity<HeroEntityMasterTag>();
            quaternion rot = _transform[_heroEntity].Rotation;
            _heroPosition = _transform[_heroEntity].Position;
            //��ȡӢ������
            var heroPar = _heroAttribute[_heroEntity];
            //��ȡӢ��װ�صļ��ܵȼ�
            var level = _heroAttribute[_heroEntity].skillDamageAttribute.skillLevel;

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

                       // collider.ValueRW.Value.Value.SetCollisionFilter(new CollisionFilter());
                      //collider.ValueRW = new PhysicsCollider();
                      //  ecb.RemoveComponent<SkillPulseTag>(entity);
                        ecb.DestroyEntity(entity);     
                     // ecb.SetComponentEnabled<PhysicsCollider>(entity, false);

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
                                    state.EntityManager.SetComponentData(buffer[i].other, monsterAttr);
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
                  
                      
                        ecb.DestroyEntity(entity);
                 
                    
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
                   
                        ecb.DestroyEntity(entity);
                    
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


                    ecb.DestroyEntity(entity);
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
                        ecb.DestroyEntity(entity);
                    }
                    ecb.SetComponent(_heroEntity, heroPar);
                }
                //���ڵڶ����ͷ��ֶ��ر�
                if(skillTag.ValueRO.closed)
                    ecb.DestroyEntity(entity);

            }
         
            //������MonoЧ��
            SkillMonoFrost(ref state,ecb);



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

                    ecb.DestroyEntity(entity);
                }



            }


        }



    }
}