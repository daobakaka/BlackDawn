using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
///专门用于处理 碰撞体的伤害锁定
namespace BlackDawn.DOTS
{
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    //这里落到控制表现系统之后进行处理
    [UpdateAfter(typeof(BehaviorControlSystem))]
    [UpdateInGroup(typeof(ActionSystemGroup))]
    partial struct AttackRecordBufferSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            //外部控制
            state.RequireForUpdate<EnableAttackRecordBufferSystemTag>();


        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var ecbP = ecb.AsParallelWriter();
            var timer = SystemAPI.Time.DeltaTime;

            state.Dependency = new HitRecordBufferDealJob
            {
                DeltaTime = timer,
                // ECB= ecbP,
            }.ScheduleParallel(state.Dependency);


            //用于记录怪物与英雄的基础攻击的记录器
            state.Dependency = new HeroHitRecordBufferDealJob
            {

                DeltaTime = timer,

            }.ScheduleParallel(state.Dependency);


            //用于记录连锁吞噬寻址技能的处理JOB
            state.Dependency = new TrackingBufferChainDevourDealJob
            {
                Time = (float)SystemAPI.Time.ElapsedTime,
                DeltaTime = SystemAPI.Time.DeltaTime,

            }.ScheduleParallel(state.Dependency);
            //用于记录闪电链寻址技能的处理JOB
            // state.Dependency = new TrackingBufferLightningChainDealJob
            // {
            //     Time = (float)SystemAPI.Time.ElapsedTime,
            //     DeltaTime = SystemAPI.Time.DeltaTime,

            // }.ScheduleParallel(state.Dependency);

            //state.Dependency.Complete();
            //计算元素共鸣的 buffer
            state.Dependency = new HitElementResonanceRecordBufferDealJob
            {

                DeltaTime = timer,
            }.ScheduleParallel(state.Dependency);


            //用于记录法阵技能的相关buffer
            state.Dependency = new SpecialSkillArcaneCircleSecondBufferDealJob
            {

                DeltaTime = timer,

            }.ScheduleParallel(state.Dependency);
            
            //清空聚合buffer,取消在原本的通用伤害计算类的清空，便于后续的如连锁吞噬类的效果计算
            state.Dependency = new AccumulateDataBufferClear { }.ScheduleParallel(state.Dependency);

            //清空怪物的两种聚合buffer,放到最后
            //最后阻塞
            // state.Dependency.Complete();

        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }


    /// <summary>
    /// 用于计算帧伤害锁定拟定1秒，1秒内多次碰撞只能计算1次伤害
    /// </summary>

    [BurstCompile]
    partial struct HitRecordBufferDealJob : IJobEntity
    {
        // public EntityCommandBuffer.ParallelWriter ECB;
        public float DeltaTime;
        void Execute(Entity entity, ref DynamicBuffer<HitRecord> hitRecord, [EntityIndexInQuery] int sortKey)
        {

            for (int i = 0; i < hitRecord.Length; i++)
            {
                var record = hitRecord[i];
                record.timer += DeltaTime;

                if (record.timer > 1f)
                {
                    hitRecord.RemoveAtSwapBack(i);
                    // 由于 SwapBack，把最后一个元素放到了当前索引，为了不漏掉要再检查新元素
                    i--;
                }
                else
                {
                    hitRecord[i] = record;
                }
            }

        }

    }




    /// <summary>
    /// 用于计算帧伤害锁定拟定0.5秒，0.5秒内多次碰撞只能计算1次伤害
    /// </summary>

    [BurstCompile]
    partial struct HeroHitRecordBufferDealJob : IJobEntity
    {
        public float DeltaTime;
        void Execute(Entity entity, ref DynamicBuffer<HeroHitRecord> heroHitRecord, [EntityIndexInQuery] int sortKey)
        {

            for (int i = 0; i < heroHitRecord.Length; i++)
            {
                var record = heroHitRecord[i];
                record.timer += DeltaTime;

                if (record.timer > 0.5f)
                {
                    heroHitRecord.RemoveAtSwapBack(i);
                    // 由于 SwapBack，把最后一个元素放到了当前索引，为了不漏掉要再检查新元素
                    i--;
                }
                else
                {
                    heroHitRecord[i] = record;
                }
            }

        }
    }



    /// <summary>
    /// 用于计算元素共鸣的伤害效果，伤害频次0.5f
    /// </summary>

    [BurstCompile]
    partial struct HitElementResonanceRecordBufferDealJob : IJobEntity
    {
        public float DeltaTime;
        void Execute(Entity entity, ref DynamicBuffer<HitElementResonanceRecord> heroHitRecord, [EntityIndexInQuery] int sortKey)
        {

            for (int i = 0; i < heroHitRecord.Length; i++)
            {
                var record = heroHitRecord[i];
                record.timer += DeltaTime;

                if (record.timer > 0.5f)
                {
                    heroHitRecord.RemoveAtSwapBack(i);
                    // 由于 SwapBack，把最后一个元素放到了当前索引，为了不漏掉要再检查新元素
                    i--;
                }
                else
                {
                    heroHitRecord[i] = record;
                }
            }

        }
    }

    /// <summary>
    /// 特殊技能，法阵第二阶段 生命虹吸开启的标签,这里直接移除，便于在碰撞job中重新添加
    /// </summary>
    [BurstCompile]
    partial struct SpecialSkillArcaneCircleSecondBufferDealJob : IJobEntity
    {
        public float DeltaTime;
        void Execute(Entity entity, ref DynamicBuffer<SkillArcaneCircleSecondBufferTag> bufferRecord, [EntityIndexInQuery] int sortKey)
        {

            for (int i = 0; i < bufferRecord.Length; i++)
            {
                var record = bufferRecord[i];
                record.tagSurvivalTime -= DeltaTime;

                if (record.tagSurvivalTime <= 0.0f)
                {
                    bufferRecord.RemoveAtSwapBack(i);
                    // 由于 SwapBack，把最后一个元素放到了当前索引，为了不漏掉要再检查新元素
                    i--;
                }
                else
                {
                    bufferRecord[i] = record;
                }
            }

        }
    }



    /// <summary>
    /// 用于计算 寻址技能的相关参数-- 这里的回调由特殊技能系统开启,连锁吞噬的buffer 处理
    /// </summary>
    [BurstCompile]
    partial struct TrackingBufferChainDevourDealJob : IJobEntity
    {
        // public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public float Time;
        public float DeltaTime;
        void Execute(Entity entity, in LocalTransform transform,in SkillChainDevourTag skilltag ,ref DynamicBuffer<TrackingRecord> hitRecord, ref SkillsTrackingCalPar trackingCalPar, [EntityIndexInQuery] int sortKey)
        {
            //添加约束条件
            if (trackingCalPar.enbaleChangeTarget == true && trackingCalPar.runCount > 0 && trackingCalPar.timer >= 0f && trackingCalPar.timer < DeltaTime)
            {
                int count = math.min(10, hitRecord.Length);
                if (count == 0)
                    return;
                uint seed = (uint)(Time) + (uint)sortKey;
                var rand = new Unity.Mathematics.Random(seed);
                int randIndex = rand.NextInt(0, count);
                var rec = hitRecord[randIndex];
                float3 targetDir = math.normalize(rec.postion - transform.Position);
                trackingCalPar.currentDir = targetDir;
                trackingCalPar.targetRef = rec.refTarget;

            }
            //每帧清空，防止无序扩张，此时记录的信息 已经通过trackingCalPar写回主线程了 所以可以清除             
            hitRecord.Clear();
        }

    }

    /// <summary>
    /// 用于计算 寻址技能的相关参数-- 这里的回调由特殊技能系统开启,闪电链的buffer 处理--- 目前放在主线程中了
    /// </summary>
    [BurstCompile]
    partial struct TrackingBufferLightningChainDealJob : IJobEntity
    {
        // public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public float Time;
        public float DeltaTime;
        void Execute(Entity entity, in LocalTransform transform,ref SkillLightningChainTag skilltag ,ref DynamicBuffer<TrackingRecord> hitRecord, ref SkillsTrackingCalPar trackingCalPar, [EntityIndexInQuery] int sortKey)
        {
            // DevDebug.LogError("进入闪电链buffer处理");
            if (!skilltag.bufferChecked)
            {
              // DevDebug.LogError($"进入闪电链buffer处理 真实进入, Entity:{entity.Index}, Version:{entity.Version}");
                int count = math.min(10, hitRecord.Length);
                if (count == 0)
                    return;
                uint seed = (uint)(Time) + (uint)sortKey;
                var rand = new Unity.Mathematics.Random(seed);

                int randIndex2 = rand.NextInt(0, count);
                int randIndex3 = rand.NextInt(0, count);
                int randIndex4 = rand.NextInt(0, count);

                var rec2 = hitRecord[randIndex2];
                var rec3 = hitRecord[randIndex3];
                var rec4 = hitRecord[randIndex4];

                trackingCalPar.pos2Ref = rec2.refTarget;
                trackingCalPar.pos3Ref = rec3.refTarget;
                trackingCalPar.pos4Ref = rec4.refTarget;
     
                skilltag.bufferChecked = true;
            }

            //每帧清空，防止无序扩张，此时记录的信息 已经通过trackingCalPar写回主线程了 所以可以清除             
            hitRecord.Clear();
        }

    }



        /// <summary>
    /// 聚合buffer清空
    /// </summary>
    [BurstCompile]
    partial struct AccumulateDataBufferClear : IJobEntity
    {


        void Execute(Entity entity, ref DynamicBuffer<FlightPropAccumulateData> flightRecord, ref DynamicBuffer<HeroSkillPropAccumulateData> skillRecord)
        {

            flightRecord.Clear();
            skillRecord.Clear();

         }

    }

}
