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


//分管怪物的所有动作JOB逻辑，与monoSystem 配合
namespace BlackDawn.DOTS
{
    /// <summary>
    /// Watcher_A行为System
    /// </summary>
    [BurstCompile]
    [UpdateAfter(typeof(DetectionSystem))]
    [UpdateInGroup(typeof(ActionSystemGroup))]
    public partial struct ActionSystem : ISystem, ISystemStartStop
    {
        ComponentLookup<LocalTransform> m_transform;
        ComponentLookup<AgentBody> m_PhysicsVelocity;
        ComponentLookup<HeroEntityBranchTag> _heroEntityBranchTagLookup;
        ScenePrefabsSingleton m_Prefabs;
        //一次建立 永久使用
        EntityQuery _heroBranchQuery;

        float timer;
        bool IsOpenAction;
        int updateActionCount;
        Entity _heroEntity;


        public void OnCreate(ref SystemState state)
        {
            //关闭系统 ，手动控制，由英雄角色初始化操控,ECS的OnCreate的Mono Awake 之前
            state.Enabled = false;
            //场景双向控制
            state.RequireForUpdate<EnableActionSystemTag>();
            Debug.Log("ECS Action 初始化");

        }
        public void OnStartRunning(ref SystemState state)
        {

            m_Prefabs = SystemAPI.GetSingleton<ScenePrefabsSingleton>();
            m_transform = SystemAPI.GetComponentLookup<LocalTransform>(true);
            m_PhysicsVelocity = SystemAPI.GetComponentLookup<AgentBody>(true);
            _heroEntityBranchTagLookup = SystemAPI.GetComponentLookup<HeroEntityBranchTag>(true);
            //不用更新
            _heroBranchQuery = state.EntityManager.CreateEntityQuery(typeof(HeroEntityBranchTag), typeof(LocalTransform));
            IsOpenAction = true;
            _heroEntity = Hero.instance.heroEntity;
        }
        void UpdateAllComponentLookup(ref SystemState state)
        {
            m_transform.Update(ref state);
            m_PhysicsVelocity.Update(ref state);
            _heroEntityBranchTagLookup.Update(ref state);

        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //state.Dependency.Complete();
            timer += SystemAPI.Time.DeltaTime;
            UpdateAllComponentLookup(ref state);
            //agent 插件的空间分布单例
            var spatial = SystemAPI.GetSingleton<AgentSpatialPartitioningSystem.Singleton>();

            //英雄位置
            float3 heroPositon = m_transform[_heroEntity].Position;         
            var branchTransforms = _heroBranchQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            //设置全局的entity的目标为英雄
            foreach (var (body, lum, live, transform) in SystemAPI.Query<RefRW<AgentBody>, RefRW<AgentLocomotion>, RefRW<LiveMonster>, RefRW<LocalTransform>>())
            {
                if (branchTransforms.Length > 0)
                {

                    // 计算与每个分支的距离，找到最近的
                    float3 selfPos = transform.ValueRO.Position;
                    int closestIdx = 0;
                    float minDistSq = math.lengthsq(selfPos - branchTransforms[0].Position);

                    for (int i = 1; i < branchTransforms.Length; i++)
                    {
                        float distSq = math.lengthsq(selfPos - branchTransforms[i].Position);
                        if (distSq < minDistSq)
                        {
                            minDistSq = distSq;
                            closestIdx = i;
                        }
                    }
                    if (minDistSq < 100)
                        body.ValueRW.SetDestination(branchTransforms[closestIdx].Position);
                    else
                        body.ValueRW.SetDestination(heroPositon);
                }
                else
                {
                    body.ValueRW.SetDestination(heroPositon);
                }

            }
            branchTransforms.Dispose();

            //近战job
            // var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            var parallelECB = ecb.AsParallelWriter();

            // Step 1: 调度 Melee Job
            state.Dependency = new ActionMelee_Job
            {
                ECB = parallelECB,
                Time = SystemAPI.Time.DeltaTime,
                TransformLookup = m_transform,
            }.ScheduleParallel(state.Dependency);

            // Step 2: 调度 Ranged Job，依赖 Melee 完成
            state.Dependency = new ActionRanged_Job
            {
                ECB = parallelECB,
                Time = SystemAPI.Time.DeltaTime,
                Prefabs = m_Prefabs,
                HeroPosition = heroPositon,
                TransformLookup = m_transform,
            }.ScheduleParallel(state.Dependency); // 注意依赖 meleeHandle！





        }



        public void OnStopRunning(ref SystemState state)
        {

        }


        void CalPropDamageAndPropDamage()
        {

            //var damageCal = new EnemyFlightProDamageCalPar();
            ////物理伤害赋值
            //damageCal.instantPhysicalDamage = cmpt.attackAttribute.attackPower;
            ////元素伤害赋值
            //damageCal.frostDamage = cmpt.attackAttribute.elementalDamage.frostDamage;
            //damageCal.fireDamage = cmpt.attackAttribute.elementalDamage.fireDamage;
            //damageCal.shadowDamage = cmpt.attackAttribute.elementalDamage.shadowDamage;
            //damageCal.poisonDamage = cmpt.attackAttribute.elementalDamage.poisonDamage;
            //damageCal.lightningDamage = cmpt.attackAttribute.elementalDamage.lightningDamage;

            //var rng = cmpt.defenseAttribute.rngState;

            ////dot 伤害赋值
            //var dotBaseDamage = cmpt.attackAttribute.attackPower;



        }

    }
    /// <summary>
    /// 无锚点近战怪逻辑
    /// </summary>
    [BurstCompile]
    public partial struct ActionMelee_Job : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        public float Time;

        /// <summary>
        /// ！ASPECT 底层已经使用ref 进行封装,空组件标签也不能使用ref 因为没必要，注意插件中本身的isStopped的判断在job中貌似并不准确,
        /// 针对无锚点的近战怪
        /// 1.4 使用EnabledRefRO<LiveMonster> 来直接筛选失活组件
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="agentBody"></param>
        /// <param name="transform"></param>
        /// <param name="animatorAspect"></param>
        /// <param name="index"></param>
        public void Execute(Entity entity, EnabledRefRO<LiveMonster> live, in MonsterGainAttribute gainAttribute, ref AgentBody agentBody, ref AgentLocomotion agentLocomotion, AtMelee atMelee,
            ref AnimationControllerData animation, ref DynamicBuffer<GpuEcsAnimatorEventBufferElement> eventBuffer,
            in LocalTransform transform, GpuEcsAnimatorAspect animatorAspect, [ChunkIndexInQuery] int index)
        {

            // 0. 计算 delta 和距离
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
                        // DevDebug.Log("动画播放开始---");
                        break;
                    case 1:
                        animation.isAttack = false;
                        break;
                    //s DevDebug.Log("动画播放结束---");
                    case 2:
                        animation.isAttack = true;
                        // DevDebug.Log("动画播放开始---");
                        break;
                    case 3:
                        animation.isAttack = false;
                        break;
                        // …其它事件                   
                }
            }
            //每帧清空buffer
            eventBuffer.Clear();
            // 2. 如果到达攻击范围，切换到 Attack 状态
            if (distSqr <= rangeSqr)
            {
                //开启攻击模式
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

                ////停止移动之后，进行手动转向
                //float3 dir = math.normalize(delta);
                //// 生成仅围绕 Y 轴的旋转
                //float yaw = math.atan2(dir.x, dir.z);
                //quaternion rot = quaternion.AxisAngle(math.up(), yaw);
                //transform.Rotation = rot;

                //动画事件修正？动画事件未触发？目前没有找到原因
                if (agentBody.RemainingDistance > 10)
                {
                    animation.isAttack = false;

                }

            }

        }
    }

    /// <summary>
    /// 有锚点远程怪逻辑
    /// </summary>
    [BurstCompile]
    public partial struct ActionRanged_Job : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        public float Time;
        public ScenePrefabsSingleton Prefabs;
        public float3 HeroPosition;

        public void Execute(Entity entity, EnabledRefRO<LiveMonster> live, in MonsterGainAttribute gainAttribute, ref AgentBody agentBody, ref AgentLocomotion agentLocomotion, AtRanged atRanged,
            ref AnimationControllerData animation, ref DynamicBuffer<GpuEcsAnimatorEventBufferElement> eventBuffer,
            in LocalTransform transform, GpuEcsAnimatorAspect animatorAspect, ref DynamicBuffer<GpuEcsCurrentAttachmentAnchorBufferElement> anchorBuffer,
            [ChunkIndexInQuery] int index)
        {

            float3 currentPos = transform.Position;
            float3 destPos = agentBody.Destination;  // AgentBody 中的目标位置
            float3 delta = destPos - currentPos;
            delta.y = 0;
            float distSqr = math.lengthsq(delta);
            //这里可以根据怪物的攻击范围属性定义
            float attackRange = gainAttribute.atkRange;
            float rangeSqr = attackRange * attackRange;


            foreach (var evt in eventBuffer)
            {
                switch (evt.eventId)
                {
                    //这里就是远程怪的fire

                    case 0:
                        animation.isAttack = true;
                        // DevDebug.Log("动画播放开始---");
                        break;
                    case 1:
                        animation.isAttack = false;
                        break;

                    case 2:
                        animation.isAttack = true;
                        break;
                    case 3:
                        //开火
                        Fire(index, transform, anchorBuffer, gainAttribute, entity);
                        break;
                    case 4:
                        animation.isAttack = false;
                        break;
                    case 5:
                        break;

                }
            }
            //每帧清空buffer
            eventBuffer.Clear();
            // 2. 如果到达攻击范围，切换到 Attack 状态
            if (distSqr <= rangeSqr)
            {
                //开启攻击模式
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
                //停止移动之后，进行手动转向
                //float3 dir = math.normalize(delta);
                //// 生成仅围绕 Y 轴的旋转
                //float yaw = math.atan2(dir.x, dir.z);
                //quaternion rot = quaternion.AxisAngle(math.up(), yaw);
                //transform.Rotation = rot;

            }

        }

        void Fire(int index, in LocalTransform transform, DynamicBuffer<GpuEcsCurrentAttachmentAnchorBufferElement> anchorBuffer, in MonsterGainAttribute gainAttribute, Entity entity)
        {
            var prob = ECB.Instantiate(index, Prefabs.MonsterFlightProp_FrostLightningBall);

            // 取第一个挂件锚点
            var anchor = anchorBuffer[0];

            float4x4 worldM = math.mul(transform.ToMatrix(), anchor.currentTransform);

            ECB.SetComponent(index, prob, new LocalTransform());

            // 拆位置
            float3 pos = worldM.c3.xyz;
            // 拆旋转（forward=col2, up=col1）
            quaternion rot = quaternion.LookRotationSafe(worldM.c2.xyz, worldM.c1.xyz);
            var scale = 1;


            // 写回到新实体的 LocalTransform
            ECB.SetComponent(index,
                prob,
                new LocalTransform
                {
                    Position = pos,
                    Rotation = rot,
                    Scale = scale
                });


            float3 diro = HeroPosition - transform.Position;      // 方向向量
            diro = math.normalize(diro);              // 单位化

            ECB.AddComponent(index, prob, new EnemyFlightProp { speed = 20, survivalTime = 5, dir = diro, monsterRef = entity });
            //添加记录buffer
            var hits = ECB.AddBuffer<HitRecord>(index, prob);
            hits.Capacity = 5; //更新容量为5
            //暂时不管其他伤害计算，后期怪物攻击属性也许会缩减，目前就用基础伤害代替
            //

        }

        /// <summary>
        ///伤害计算,有没有必要给怪设计暴击等属性？直接使用简单属性或许更好，这里进行属性传递可以免查询计算，直接使用池化控制shader参数且触发dot？
        ///传入控制参数？
        ///使用boss或者精英怪构建偶然性
        ///可以保留，因为怪物伤害计算，是检测到碰撞之后再计算，那么直接的伤害计算就可以找到怪本身进行计算了？
        /// </summary>
        //EnemyFlightProDamageCalPar CalDamage(MonsterAttributeCmpt monsterAttributeCmpt)
        //{
        //    var calDamage = new EnemyFlightProDamageCalPar();

        //    var at = monsterAttributeCmpt.attackAttribute;
        //    //物理 火焰 冰霜 毒素 闪电 暗影
        //    calDamage.instantPhysicalDamage = at.attackPower;
        //    calDamage.fireDamage = at.elementalDamage.fireDamage;
        //    calDamage.frostDamage=at.elementalDamage.frostDamage;
        //    calDamage.poisonDamage = at.elementalDamage.poisonDamage;
        //    calDamage.lightningDamage = at.elementalDamage.lightningDamage;
        //    calDamage.shadowDamage = at.elementalDamage.shadowDamage;
        //    return calDamage;
        //}




    }

/// <summary>
/// 查找附近的 英雄分支,就是一个结构体 封装方法， 非JOB
/// </summary>
    [BurstCompile]
    public struct FindClosestHeroBranch : ISpatialQueryEntity
    {
        public float3 SelfPos;
        public ComponentLookup<HeroEntityBranchTag> HeroEntityBranchTagLookup;
        public Entity ClosestEntity;
        public float3 ClosestPos;
        public float MinDistSq;

        public void Execute(Entity entity, AgentBody body, AgentShape shape, LocalTransform transform)
        {
            // 必须是Branch
            if (!HeroEntityBranchTagLookup.HasComponent(entity))
                return;

            float distSq = math.lengthsq(SelfPos - transform.Position);
            if (distSq <=MinDistSq)
            {
                MinDistSq = distSq;
                ClosestEntity = entity;
                ClosestPos = transform.Position;
            }
        }

    }
}