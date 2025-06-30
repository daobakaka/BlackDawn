using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.VFX;

//���ڴ������ͷŵĻص�����burstCompile
namespace BlackDawn.DOTS
{/// <summary>
/// ��Ӣ��mono����,����Ⱦϵͳ֮�����,�ص�ϵͳ�� �漰����ͳclass������ �����������
/// </summary>
    [RequireMatchingQueriesForUpdate]
    [UpdateAfter(typeof(HeroSkillsMonoSystem))]
    [UpdateInGroup(typeof(MainThreadSystemGroup))]
    public partial class HeroSkillsCallbackSystemBase : SystemBase, IOneStepFun
    {
        public bool Done { get; set; }
        HeroSkills _heroSkills;
        ScenePrefabsSingleton _prefabs;

        //���⼼��ϵͳ������ĺ�����Ч������
        private SystemHandle _specialSkillsDamageSystemHandle;

        private SystemHandle _detectionSystemHandle;

        private Entity _heroEntity;
        //ֱ������mono�ĵ���λ�ã��Ῠ��
        private ComponentLookup<LocalTransform> _transformLookup;
        //Ӣ������
        private ComponentLookup<HeroAttributeCmpt> _heroAttribute;


        //�����ܵ�GPUbuffer
        GraphicsBuffer _arcaneCirclegraphicsBuffer;
        private bool resetVFXPartical;
        protected override void OnCreate()
        {
            base.OnCreate();
            //��Ӣ�۳�ʼ��ʱ����
            base.Enabled = false;

            _specialSkillsDamageSystemHandle = World.Unmanaged.GetExistingUnmanagedSystem<HeroSpecialSkillsDamageSystem>();
            _arcaneCirclegraphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 5000, sizeof(float) * 3);
            _detectionSystemHandle = World.Unmanaged.GetExistingUnmanagedSystem<DetectionSystem>();
            _transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            _heroAttribute = SystemAPI.GetComponentLookup<HeroAttributeCmpt>(true);

        }

        protected override void OnStartRunning()
        {
            //��ȡӢ�ۼ��ܵ���
            _heroSkills = HeroSkills.GetInstance();

            _prefabs = SystemAPI.GetSingleton<ScenePrefabsSingleton>();
            //��ȡӢ��entity
            _heroEntity = Hero.instance.heroEntity;

        }

        protected override void OnUpdate()
        {
            //baseϵͳ����
            _transformLookup.Update(this);
            //hero���Ը���
            _heroAttribute.Update(this);

            //��ȡӢ������
            var heroPar = _heroAttribute[_heroEntity];

            var ecb = World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer();

            var timer = SystemAPI.Time.DeltaTime;

            var specialDanafeSystem = World.Unmanaged.GetUnsafeSystemRef<HeroSpecialSkillsDamageSystem>(_specialSkillsDamageSystemHandle);

            var detctionSystem = World.Unmanaged.GetUnsafeSystemRef<DetectionSystem>(_detectionSystemHandle);
            //��Ⱦ����
            var arcaneCircleLinkedArray = specialDanafeSystem.arcaneCircleLinkenBuffer;
            //��ȡ���ϵͳ��  ��ײ��
            var mineBlastArray = detctionSystem.mineBlastHitMonsterArray;

            //�����Ի���
            Entities
           .WithName("DrawOverlapSpheres")
           .ForEach((in OverlapQueryCenter overlap) =>
           {

               if (overlap.shape == OverLapShape.Sphere)
                   DebugDrawSphere(overlap.center, overlap.offset, overlap.radius, Color.yellow, 0.02f);
               else if (overlap.shape == OverLapShape.Box)
               {
                   quaternion rot = new quaternion(overlap.rotaion); // ��Ԫ��
                   DebugDrawBox(overlap.center, overlap.offset, overlap.box, rot, Color.green, 0.02f);
               }

           }).WithoutBurst().Run();



            // **�������д��������ǵ�ʵ��**,������ҪΪ��������ECB������������foreach����ͬһ֡ʹ��
            //�������������Ч�ķ���
            if (false)
                Entities
                    .WithName("SkillPulseSceondExplosionCallback") //����ײ��ǩ�������ڱ��
                    .WithAll<SkillPulseSecondExplosionRequestTag>() //ƥ��ABC �������,Ĭ��ƥ��û�б�disnable�����
                                                                    //in ֻ������Ҫ�ŵ�ref ����
                    .ForEach((Entity e, ref SkillsDamageCalPar damageCalPar, ref SkillPulseTag pulseTag, in LocalTransform t) =>
                    {
                        // ���� Mono ��ı�ը�߼��������������׶�
                        var entity = _heroSkills.DamageSkillsExplosionProp(
                           ecb,
                           _prefabs.ParticleEffect_DefaultEffexts, //��ը��Ч                        
                           t.Position,
                           t.Rotation,
                           1,
                           0, 0, pulseTag.scaleChangePar, false, true
                       );
                        ecb.AddComponent(entity, new SkillPulseTag() { tagSurvivalTime = 2, scaleChangePar = pulseTag.scaleChangePar });//Ϊ���׶μ������ɴ���ǩ,���ﴫ���α����,��������
                                                                                                                                        //���ַ�ʽ�����γɽṹ�ı�               
                        ecb.SetComponentEnabled<SkillPulseSecondExplosionRequestTag>(e, false);
                        //���٣���ʱ�Ѿ������˶��׶μ��ܣ����׶μ���û�б�ǩ�����ص���һ�׶ν�������
                        ecb.DestroyEntity(e);
                    })
                    .WithoutBurst()   // ����ر� Burst�����ܵ����κ� UnityEngine/Mono ����
                    .Run();


            //���崦��
            Entities
                .WithName("SkillPulseSceondVFXExplosionCallback") //����ֱ�ӱ�������VFX�ص���ʶ
                .WithAll<SkillPulseSecondExplosionRequestTag>() //ƥ��ABC �������,Ĭ��ƥ��û�б�disnable�����
                                                                //in ֻ������Ҫ�ŵ�ref ����
                .ForEach((Entity e, VisualEffect vfx, ref SkillsDamageCalPar damageCalPar, ref SkillPulseTag pulseTag, ref LocalTransform t) =>
                {

                    vfx.SendEvent("hit");
                    ecb.SetComponentEnabled<SkillPulseSecondExplosionRequestTag>(e, false);
                    //�л������ͱ�ը״̬
                    damageCalPar.enableExplosion = true;
                    damageCalPar.enablePull = false;
                    t.Scale *= (1 + pulseTag.scaleChangePar);
                    //ȡ���ڶ��׶�״̬��2�������
                    pulseTag.tagSurvivalTime = 2;
                    pulseTag.enableSecond = false;
                    //���׶�ֹͣ�ƶ�
                    pulseTag.speed = 0;

                })
                .WithoutBurst()   // ����ر� Burst�����ܵ����κ� UnityEngine/Mono ����
                .Run();

            //��������
            Entities
                .WithName("SkillIceFireSceondVFXExplosionCallback") //����ֱ�ӱ�Ǳ������VFX�ص���ʶ
                .WithAll<SkillIceFireSecondExplosionRequestTag>() //ƥ��ABC �������,Ĭ��ƥ��û�б�disnable�����
                                                                  //in ֻ������Ҫ�ŵ�ref ����
                .ForEach((Entity e, VisualEffect vfx, ref SkillsDamageCalPar damageCalPar, ref SkillIceFireTag skillTag, ref LocalTransform t) =>
                {
                    //���ű�ը����
                    vfx.SendEvent("hit");
                    ecb.SetComponentEnabled<SkillIceFireSecondExplosionRequestTag>(e, false);

                    //�л������ͱ�ը״̬
                    damageCalPar.enableExplosion = false;
                    //��ը��������Ч��
                    damageCalPar.enablePull = true;
                    //��ը�������
                    t.Scale *= (1 + skillTag.scaleChangePar);
                    //���ӱ�ը�˺�,����ֱ�����ӣ���Ϊ�ǽ���˷�����ֱ�ӼӾͿ���
                    damageCalPar.damageChangePar += skillTag.skillDamageChangeParTag;
                    //ȡ���ڶ��׶�״̬��2�������
                    skillTag.secondSurvivalTime = 4;
                    //��������Ч��
                    skillTag.enableSpecialEffect = false;
                    //���׶�ֹͣ�ƶ�
                    // skillTag.speed = 0;

                })
                .WithoutBurst()   // ����ر� Burst�����ܵ����κ� UnityEngine/Mono ����
                .Run();

            //�������ظ������
            Entities
                .WithName("Disabled_SkillIceFireSecondExplosion")
                .WithDisabled<SkillIceFireSecondExplosionRequestTag>()
                .ForEach((Entity entity, VisualEffect vfx,
                    ref SkillIceFireTag skillTag,
                    ref SkillsDamageCalPar damageCalPar,
                    ref LocalTransform transform) =>
                {

                    skillTag.secondSurvivalTime -= timer;
                    //����ִ��һ�λָ��жϣ����벥�ű�ը��Ч2��֮�����
                    if (!skillTag.enableSpecialEffect && skillTag.secondSurvivalTime < 3)
                    {
                        vfx.SendEvent("create");
                        //�ָ��ߴ�
                        transform.Scale = skillTag.originalScale;
                        //�ָ���ʱ�����
                        damageCalPar.damageChangePar = 1;
                        //�ָ�����
                        damageCalPar.enablePull = false;
                        skillTag.enableSpecialEffect = true;
                    }


                })
                .WithoutBurst().Run();


            //����buffer��Ч�� ����Ҫ���������,Ŀǰ��ʱʹ�����ַ�ʽ���о�������ʽ��BUG,�ر��ǹ���buffer��������鷳
            if (_arcaneCirclegraphicsBuffer != null)
            {
                _arcaneCirclegraphicsBuffer.Dispose();
            }


            //�����������Ч,����buffer����
            Entities
                    .WithName("Enable_ArcaneCircleSecond")
                    .WithAll<HeroEffectsLinked>()
                    .ForEach((Entity entity, VisualEffect vfx
                      ) =>
                    {

                        if (arcaneCircleLinkedArray.Length > 0)
                        {
                            var targetPositions = arcaneCircleLinkedArray;

                            _arcaneCirclegraphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, targetPositions.Length, sizeof(float) * 3);
                            vfx.SetInt("_LinkedTargetsCount", targetPositions.Length);

                            _arcaneCirclegraphicsBuffer.SetData(targetPositions);
                            // ���� VFX
                            vfx.SetGraphicsBuffer("_LinkedTargets", _arcaneCirclegraphicsBuffer);  // �� VFX ��ƥ��

                            //��������׼�����֮�󣬽���buffer���
                            vfx.SendEvent("Custom5");
                            // buffer.Dispose();
                            resetVFXPartical = false;
                        }
                    })
                    .WithoutBurst().Run();

            //�����������Ч,�����ǻָ�
            Entities
                .WithName("DisEnable_ArcaneCircleSecond")
                .WithDisabled<HeroEffectsLinked>()
                .ForEach((Entity entity, VisualEffect vfx
                  ) =>
                {
                    if (!resetVFXPartical)
                    {


                        vfx.Reinit();
                        resetVFXPartical = true;
                    }

                })
                .WithoutBurst().Run();



            //��������һ���׶δ���
            Entities
                .WithName("SkillMineBlast")
                .ForEach((Entity entity, VisualEffect vfx,
                    ref SkillMineBlastTag skillTag,
                    ref SkillsDamageCalPar damageCalPar,
                    ref LocalTransform transform) =>
                {

                    skillTag.tagSurvivalTime -= timer;
                    //�ٷְ��������У���������±���
                    if (skillTag.tagSurvivalTime <= 1f && skillTag.tagSurvivalTime > 1f - timer)
                    {
                        vfx.SendEvent("stop");

                    }
                    if (skillTag.tagSurvivalTime <= 0)
                    {
                        // ecb.DestroyEntity(entity);
                        damageCalPar.destory = true;
                        return;

                    }
                    //���ּ�⣬��ΪҪ���㶾��֮��Ķ���Ч��,��������������ײ��̫���ˣ�Ӧ�����뿪

                    for (int i = 0; i < mineBlastArray.Length; i++)
                    {

                        if (mineBlastArray[i].EntityA == entity || mineBlastArray[i].EntityB == entity)

                        {
                            vfx.SendEvent("hit");
                            ecb.SetComponentEnabled<SkillMineBlastTag>(entity, false);
                            //������ը
                            ecb.SetComponentEnabled<SkillMineBlastExplosionTag>(entity, true);
                            //�����µ��˺�
                            damageCalPar.damageChangePar = skillTag.skillDamageChangeParTag;
                            //���豬ըЧ��
                            damageCalPar.enableExplosion = true;
                            //��Χ����
                            transform.Scale = skillTag.scaleChangePar;
                            //200��־�Ч���� �־�3��
                            damageCalPar.tempFear = 200;
                            break;
                        }

                    }

                })
                .WithoutBurst().Run();

            //��������һ�ױ�ըЧ������
            Entities
                .WithName("DisableSkillMineBlast")
                .WithDisabled<SkillMineBlastTag>()
                .ForEach((Entity entity, VisualEffect vfx,
                    ref SkillMineBlastExplosionTag skillTag,
                    ref SkillsDamageCalPar damageCalPar,
                    ref LocalTransform transform) =>
                {
                    skillTag.tagSurvivalTime -= timer;


                    if (skillTag.tagSurvivalTime <= 0)
                    {

                        if (!skillTag.enableSecondA)
                        {
                            //ecb.DestroyEntity(entity);
                            damageCalPar.destory = true;
                            return;
                        }
                        else //�����һ�׶� �仯���
                        {
                            skillTag.tagSurvivalTimeSecond -= timer;
                            if (!skillTag.startSecondA)
                            {
                                skillTag.startSecondA = true;
                                vfx.SendEvent("buildup");
                                //����仯����
                                transform.Scale = skillTag.scaleChangePar;
                                //�����˺�����
                                damageCalPar.damageChangePar = skillTag.skillDamageChangeParTag;
                                //�����־�ֵ
                                damageCalPar.tempFear = 0;
                                //������ըֵ
                                damageCalPar.enableExplosion = false;
                                //��������ֵ
                                damageCalPar.enablePull = true;
                            }
                            if (skillTag.tagSurvivalTimeSecond <= 0)
                            {
                                //ecb.DestroyEntity(entity);
                                damageCalPar.destory = true;
                                return;
                            }

                        }
                    }
                    //����ڶ��׶α仯��д�����⼼��job ���棿               

                })
                .WithoutBurst().Run();

            //���ܶ���
            Entities
           .WithName("SkillOverTimePoisonRain")
           .ForEach((Entity entity, VisualEffect vfx,
               ref SkillPoisonRainTag skillTag,
               ref SkillsOverTimeDamageCalPar damageCalPar,
               ref LocalTransform transform) =>
           {
               skillTag.tagSurvivalTime -= timer;

               if (skillTag.tagSurvivalTime <= 2f && skillTag.tagSurvivalTime > 2f - timer)
               {
                   vfx.Stop();


               }
               if (skillTag.tagSurvivalTime <= 0)
                   damageCalPar.destory = true;


           }).WithoutBurst().Run();


            //���� ��Ӱ����,���������棬��ת
            Entities
            .WithName("SkillOverTimeShadowTide")
            .ForEach((Entity entity, VisualEffect vfx,
               ref SkillShadowTideTag skillTag,
               ref OverlapQueryCenter overlap,
               ref SkillsOverTimeDamageCalPar damageCalPar,
               ref LocalTransform transform) =>
            {
                skillTag.tagSurvivalTime -= timer;

                //ͬ����ײ��
                overlap.center = _transformLookup[_heroEntity].Position;
                overlap.rotaion = _transformLookup[_heroEntity].Rotation.value;
                //ƫ��Y1�ľ���
                transform.Position = _transformLookup[_heroEntity].Position+new float3(0,1,0);
                transform.Rotation = _transformLookup[_heroEntity].Rotation;
                 if (skillTag.tagSurvivalTime <= 0)
                    {
                        //ǯ�Ƶ�0 ÿ������5��
                        heroPar.defenseAttribute.energy = math.max(0, heroPar.defenseAttribute.energy - 5 * timer);

                        if (heroPar.defenseAttribute.energy <= 0)
                        {

                            skillTag.closed = true;
                        }
                        ecb.SetComponent(_heroEntity, heroPar);

                    }
                if (skillTag.closed == true)
                {
                    vfx.Stop();
                    skillTag.effectDissolveTime += timer;
                    if(skillTag.effectDissolveTime>=1)
                    damageCalPar.destory = true;
                }
                if (skillTag.enableSecondB)
                {
                    skillTag.secondBTimer += timer;
                    //5��һ�εĽ���
                    if (skillTag.secondBTimer <= 3f && skillTag.secondBTimer > 3f - timer)
                      {
                        skillTag.secondBTimer = 0;
                        //�������㣬����30%��������жϣ��ɹ����ͷż���,ת�����˺� Ĭ�ϰ�ɫ
                        if (UnityEngine.Random.Range(0, 1f) < 0.5f)

                        {  //�ͷż���b
                            var shadowTideB = ecb.Instantiate(_prefabs.HeroSkillAssistive_ShadowTideB);
                            var currentTransform = _transformLookup[entity]; 

                          var skillsDamageCalPar = new SkillsDamageCalPar();
                            //�̳�2����Ӱ�˺�
                            skillsDamageCalPar.fireDamage = damageCalPar.shadowDamage;
                            //��ɵ�����DOT �˺�
                            skillsDamageCalPar.fireDotDamage = damageCalPar.shadowDamage;
                            skillsDamageCalPar.damageChangePar = skillTag.skillDamageChangeParTag * 2 * (1 + 0.1f * skillTag.level);
                            skillsDamageCalPar.heroRef = _heroEntity;

                            ecb.AddComponent(shadowTideB, skillsDamageCalPar);
                            //���ר�����ܱ�ǩ,����3�룿
                            ecb.AddComponent(shadowTideB, new SkillShadowTideBTag() { tagSurvivalTime = 1.0f });

                            ecb.SetComponent(shadowTideB, new LocalTransform
                            {
                                Position = currentTransform.Position,
                                Rotation = currentTransform.Rotation,
                                Scale = 1f
                            });
                            // 6) �����ײ��¼������
                            var hits = ecb.AddBuffer<HitRecord>(shadowTideB);
                            ecb.AddBuffer<HitElementResonanceRecord>(shadowTideB);
                        }

                    }
                               
                }
   

            }).WithoutBurst().Run();






            // ���Ų�����
            //ecb.Playback(base.EntityManager);
            //ecb.Dispose();


        }




        protected override void OnDestroy()
        {


            //����buffer��Ч�� ����Ҫ���������
            if (_arcaneCirclegraphicsBuffer != null)
            {
                _arcaneCirclegraphicsBuffer.Dispose();
            }
        }

        void DebugDrawSphere(float3 center,float3 offset, float radius, Color color, float duration)
        {
            // ��12���߶ν���һ����
            for (int i = 0; i < 12; i++)
            {
                float angle1 = math.radians(i * 30);
                float angle2 = math.radians((i + 1) * 30);

                // xyƽ��
                Debug.DrawLine(
                    (Vector3)(center +offset+ new float3(math.cos(angle1), math.sin(angle1), 0) * radius),
                    (Vector3)(center +offset + new float3(math.cos(angle2), math.sin(angle2), 0) * radius),
                    color, duration);

                // xzƽ��
                Debug.DrawLine(
                    (Vector3)(center + offset + new float3(math.cos(angle1), 0, math.sin(angle1)) * radius),
                    (Vector3)(center + offset + new float3(math.cos(angle2), 0, math.sin(angle2)) * radius),
                    color, duration);

                // yzƽ��
                Debug.DrawLine(
                    (Vector3)(center + offset + new float3(0, math.cos(angle1), math.sin(angle1)) * radius),
                    (Vector3)(center + offset + new float3(0, math.cos(angle2), math.sin(angle2)) * radius),
                    color, duration);
            }
        }

        void DebugDrawBox(float3 center, float3 offset, float3 boxSize, quaternion rotation, Color color, float duration)
        {
            float3 halfExtents = boxSize * 0.5f;

            // ��ת offset
            float3 rotatedOffset = math.mul(rotation, offset);

            // ʵ������
            float3 boxCenter = center + rotatedOffset;

            // 8 ���ǵ㣨�ڱ��������£�
            float3[] localCorners = new float3[8]
            {
                new float3(-1, -1, -1),
                new float3( 1, -1, -1),
                new float3( 1,  1, -1),
                new float3(-1,  1, -1),
                new float3(-1, -1,  1),
                new float3( 1, -1,  1),
                new float3( 1,  1,  1),
                new float3(-1,  1,  1),
            };

            // Ӧ�����ź���ת���õ�����ռ�ǵ�
            for (int i = 0; i < 8; i++)
            {
                localCorners[i] *= halfExtents;
                localCorners[i] = math.mul(rotation, localCorners[i]) + boxCenter;
            }

            // �����ߣ����� 12 ���ߣ�
            int3[] edges = new int3[12]
            {
                new int3(0,1,0), new int3(1,2,0), new int3(2,3,0), new int3(3,0,0),
                new int3(4,5,0), new int3(5,6,0), new int3(6,7,0), new int3(7,4,0),
                new int3(0,4,0), new int3(1,5,0), new int3(2,6,0), new int3(3,7,0),
            };

            for (int i = 0; i < 12; i++)
            {
                Debug.DrawLine(localCorners[edges[i].x], localCorners[edges[i].y], color, duration);
            }
        }

    }
}