using GPUECSAnimationBaker.Engine.AnimatorSystem;
using ProjectDawn.Navigation;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using static BlackDawn.HeroAttributes;
using static Unity.Burst.Intrinsics.X86.Avx;


//�ֹܹ�������ж���JOB�߼�����monoSystem ���
namespace BlackDawn.DOTS
{
    /// <summary>
    /// Watcher_A��ΪSystem
    /// </summary>
    [BurstCompile]
    [UpdateAfter(typeof(DetectionSystem))]
    [UpdateInGroup(typeof(ActionSystemGroup))]
    public partial struct ActionSystem : ISystem, ISystemStartStop
    {
        ComponentLookup<LocalTransform> m_transform;
        ComponentLookup<AgentBody> m_PhysicsVelocity;
        ScenePrefabsSingleton m_Prefabs;

        float timer;
        bool IsOpenAction;
        int updateActionCount;
        Entity _heroEntity;


        public void OnCreate(ref SystemState state)
        {
            //�ر�ϵͳ ���ֶ����ƣ���Ӣ�۽�ɫ��ʼ���ٿ�,ECS��OnCreate��Mono Awake ֮ǰ
            state.Enabled = false;
            //����˫�����
            state.RequireForUpdate<EnableActionSystemTag>();
            Debug.Log("ECS Action ��ʼ��");

        }
        public void OnStartRunning(ref SystemState state)
        {

            m_Prefabs = SystemAPI.GetSingleton<ScenePrefabsSingleton>();
            m_transform = SystemAPI.GetComponentLookup<LocalTransform>(true);
            m_PhysicsVelocity = SystemAPI.GetComponentLookup<AgentBody>(true);
            IsOpenAction = true;
            _heroEntity = Hero.instance.heroEntity;
        }
        void UpdateAllComponentLookup(ref SystemState state)
        {
            m_transform.Update(ref state);
            m_PhysicsVelocity.Update(ref state);
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //state.Dependency.Complete();
            timer += SystemAPI.Time.DeltaTime;

            UpdateAllComponentLookup(ref state);


            //Ӣ��λ��
            float3 heroPositon = m_transform[_heroEntity].Position;

            //����ȫ�ֵ�entity��Ŀ��ΪӢ��
            foreach (var (body, lum) in SystemAPI.Query<RefRW<AgentBody>, RefRW<AgentLocomotion>>())
            {
                body.ValueRW.SetDestination(heroPositon);
                // lum.ValueRW.Speed = 10;

            }

            //��սjob
            // var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            var parallelECB = ecb.AsParallelWriter();

            // Step 1: ���� Melee Job
            state.Dependency = new ActionMelee_Job
            {
                ECB = parallelECB,
                Time = SystemAPI.Time.DeltaTime,
                TransformLookup = m_transform,
            }.ScheduleParallel(state.Dependency);

            // Step 2: ���� Ranged Job������ Melee ���
            state.Dependency = new ActionRanged_Job
            {
                ECB = parallelECB,
                Time = SystemAPI.Time.DeltaTime,
                Prefabs = m_Prefabs,
                HeroPosition = heroPositon,
                TransformLookup = m_transform,
            }.ScheduleParallel(state.Dependency); // ע������ meleeHandle��

     



        }



        public void OnStopRunning(ref SystemState state)
        {

        }


        void CalPropDamageAndPropDamage()
        {

            //var damageCal = new EnemyFlightProDamageCalPar();
            ////�����˺���ֵ
            //damageCal.instantPhysicalDamage = cmpt.attackAttribute.attackPower;
            ////Ԫ���˺���ֵ
            //damageCal.frostDamage = cmpt.attackAttribute.elementalDamage.frostDamage;
            //damageCal.fireDamage = cmpt.attackAttribute.elementalDamage.fireDamage;
            //damageCal.shadowDamage = cmpt.attackAttribute.elementalDamage.shadowDamage;
            //damageCal.poisonDamage = cmpt.attackAttribute.elementalDamage.poisonDamage;
            //damageCal.lightningDamage = cmpt.attackAttribute.elementalDamage.lightningDamage;

            //var rng = cmpt.defenseAttribute.rngState;

            ////dot �˺���ֵ
            //var dotBaseDamage = cmpt.attackAttribute.attackPower;



        }

    }
    /// <summary>
    /// ��ê���ս���߼�
    /// </summary>
    [BurstCompile]
    public partial struct ActionMelee_Job : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        public float Time;

        /// <summary>
        /// ��ASPECT �ײ��Ѿ�ʹ��ref ���з�װ,�������ǩҲ����ʹ��ref ��Ϊû��Ҫ��ע�����б����isStopped���ж���job��ò�Ʋ���׼ȷ,
        /// �����ê��Ľ�ս��
        /// 1.4 ʹ��EnabledRefRO<LiveMonster> ��ֱ��ɸѡʧ�����
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="agentBody"></param>
        /// <param name="transform"></param>
        /// <param name="animatorAspect"></param>
        /// <param name="index"></param>
        public void Execute(Entity entity, EnabledRefRO<LiveMonster> live,in MonsterGainAttribute gainAttribute, ref AgentBody agentBody, ref AgentLocomotion agentLocomotion, AtMelee atMelee,
            ref AnimationControllerData animation, ref DynamicBuffer<GpuEcsAnimatorEventBufferElement> eventBuffer,
            in LocalTransform transform, GpuEcsAnimatorAspect animatorAspect, [ChunkIndexInQuery] int index)
        {

            // 0. ���� delta �;���
            float3 currentPos = transform.Position;
            float3 destPos = agentBody.Destination;
            float3 delta = destPos - currentPos;
            delta.y = 0;
            float distSqr = math.lengthsq(delta);
            float attackRange = gainAttribute.atkRange;
            float rangeSqr = attackRange * attackRange;
            foreach (var evt in eventBuffer)
            {
                switch (evt.eventId)
                {
                    case 0:
                        animation.isAttack = true;
                        // DevDebug.Log("�������ſ�ʼ---");
                        break;
                    case 1:
                        animation.isAttack = false;
                        break;
                    //s DevDebug.Log("�������Ž���---");
                    case 2:
                        animation.isAttack = true;
                        // DevDebug.Log("�������ſ�ʼ---");
                        break;
                    case 3:
                        animation.isAttack = false;
                        break;
                        // �������¼�                   
                }
            }
            //ÿ֡���buffer
            eventBuffer.Clear();
            // 2. ������﹥����Χ���л��� Attack ״̬
            if (distSqr <= rangeSqr)
            {
                //��������ģʽ
                animatorAspect.RunAnimation(1, 0, 1f);

            }
            else
            {
                if (animation.isAttack == false)
                    animatorAspect.RunAnimation(0, 0, 1);

            }

            if (animation.isAttack == true)
            {
                agentLocomotion.Speed = 0;

                ////ֹͣ�ƶ�֮�󣬽����ֶ�ת��
                //float3 dir = math.normalize(delta);
                //// ���ɽ�Χ�� Y �����ת
                //float yaw = math.atan2(dir.x, dir.z);
                //quaternion rot = quaternion.AxisAngle(math.up(), yaw);
                //transform.Rotation = rot;

                //�����¼������������¼�δ������Ŀǰû���ҵ�ԭ��
                if (agentBody.RemainingDistance > 10)
                {
                    animation.isAttack = false;

                }

            }

        }
    }

    /// <summary>
    /// ��ê��Զ�̹��߼�
    /// </summary>
    [BurstCompile]
    public partial struct ActionRanged_Job : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        public float Time;
        public ScenePrefabsSingleton Prefabs;
        public float3 HeroPosition;

        public void Execute(Entity entity, EnabledRefRO<LiveMonster> live,in MonsterGainAttribute gainAttribute, ref AgentBody agentBody, ref AgentLocomotion agentLocomotion, AtRanged atRanged,
            ref AnimationControllerData animation, ref DynamicBuffer<GpuEcsAnimatorEventBufferElement> eventBuffer,
            in LocalTransform transform, GpuEcsAnimatorAspect animatorAspect, ref DynamicBuffer<GpuEcsCurrentAttachmentAnchorBufferElement> anchorBuffer,
            [ChunkIndexInQuery] int index)
        {

            float3 currentPos = transform.Position;
            float3 destPos = agentBody.Destination;  // AgentBody �е�Ŀ��λ��
            float3 delta = destPos - currentPos;
            delta.y = 0;
            float distSqr = math.lengthsq(delta);
            //������Ը��ݹ���Ĺ�����Χ���Զ���
            float attackRange = gainAttribute.atkRange;
            float rangeSqr = attackRange * attackRange;


            foreach (var evt in eventBuffer)
            {
                switch (evt.eventId)
                {
                    //�������Զ�ֵ̹�fire

                    case 0:
                        animation.isAttack = true;
                        // DevDebug.Log("�������ſ�ʼ---");
                        break;
                    case 1:
                        animation.isAttack = false;
                        break;

                    case 2:
                        animation.isAttack = true;
                        break;
                    case 3:
                        //����
                        Fire(index, transform, anchorBuffer, gainAttribute, entity);
                        break;
                    case 4:
                        animation.isAttack = false;
                        break;
                    case 5:
                        break;

                }
            }
            //ÿ֡���buffer
            eventBuffer.Clear();
            // 2. ������﹥����Χ���л��� Attack ״̬
            if (distSqr <= rangeSqr)
            {
                //��������ģʽ
                animatorAspect.RunAnimation(2, 0, 1f);

            }
            else
            {
                if (animation.isAttack == false)
                    animatorAspect.RunAnimation(0, 0, 1);

            }

            if (animation.isAttack == true)
            {
                agentLocomotion.Speed = 0; ;
                //ֹͣ�ƶ�֮�󣬽����ֶ�ת��
                //float3 dir = math.normalize(delta);
                //// ���ɽ�Χ�� Y �����ת
                //float yaw = math.atan2(dir.x, dir.z);
                //quaternion rot = quaternion.AxisAngle(math.up(), yaw);
                //transform.Rotation = rot;

            }

        }

        void Fire(int index, in LocalTransform transform, DynamicBuffer<GpuEcsCurrentAttachmentAnchorBufferElement> anchorBuffer, in MonsterGainAttribute gainAttribute, Entity entity)
        {
            var prob = ECB.Instantiate(index, Prefabs.MonsterFlightProp_FrostLightningBall);

            // ȡ��һ���Ҽ�ê��
            var anchor = anchorBuffer[0];

            float4x4 worldM = math.mul(transform.ToMatrix(), anchor.currentTransform);

            ECB.SetComponent(index, prob, new LocalTransform());

            // ��λ��
            float3 pos = worldM.c3.xyz;
            // ����ת��forward=col2, up=col1��
            quaternion rot = quaternion.LookRotationSafe(worldM.c2.xyz, worldM.c1.xyz);
            var scale = 1;


            // д�ص���ʵ��� LocalTransform
            ECB.SetComponent(index,
                prob,
                new LocalTransform
                {
                    Position = pos,
                    Rotation = rot,
                    Scale = scale
                });


            float3 diro = HeroPosition - transform.Position;      // ��������
            diro = math.normalize(diro);              // ��λ��

            ECB.AddComponent(index, prob, new EnemyFlightProp { speed = 20, survivalTime = 5, dir = diro, monsterRef = entity });
            //��Ӽ�¼buffer
            var hits = ECB.AddBuffer<HitRecord>(index, prob);
            hits.Capacity = 5; //��������Ϊ5
            //��ʱ���������˺����㣬���ڹ��﹥������Ҳ���������Ŀǰ���û����˺�����
            //

        }

        /// <summary>
        ///�˺�����,��û�б�Ҫ������Ʊ��������ԣ�ֱ��ʹ�ü����Ի�����ã�����������Դ��ݿ������ѯ���㣬ֱ��ʹ�óػ�����shader�����Ҵ���dot��
        ///������Ʋ�����
        ///ʹ��boss���߾�Ӣ�ֹ���żȻ��
        ///���Ա�������Ϊ�����˺����㣬�Ǽ�⵽��ײ֮���ټ��㣬��ôֱ�ӵ��˺�����Ϳ����ҵ��ֱ�����м����ˣ�
        /// </summary>
        //EnemyFlightProDamageCalPar CalDamage(MonsterAttributeCmpt monsterAttributeCmpt)
        //{
        //    var calDamage = new EnemyFlightProDamageCalPar();

        //    var at = monsterAttributeCmpt.attackAttribute;
        //    //���� ���� ��˪ ���� ���� ��Ӱ
        //    calDamage.instantPhysicalDamage = at.attackPower;
        //    calDamage.fireDamage = at.elementalDamage.fireDamage;
        //    calDamage.frostDamage=at.elementalDamage.frostDamage;
        //    calDamage.poisonDamage = at.elementalDamage.poisonDamage;
        //    calDamage.lightningDamage = at.elementalDamage.lightningDamage;
        //    calDamage.shadowDamage = at.elementalDamage.shadowDamage;
        //    return calDamage;
        //}




    }


}