using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace BlackDawn.DOTS
{
    /// <summary>
    /// 负责 Detection_DefaultCmpt 的触发检测，
    /// 并为每个实体从其 NearbyHit buffer 中选出最近的目标
    /// 侦测系统最先运行
    /// </summary>
    [RequireMatchingQueriesForUpdate]
    //在行动系统之前进行侦测
    [UpdateBefore(typeof(ActionSystem))]
    [UpdateInGroup(typeof(ActionSystemGroup))]
    [BurstCompile]
    public partial struct DetectionSystem : ISystem
    {
        private int batchSize;

        // 每帧要更新的查找
        private ComponentLookup<Detection_DefaultCmpt> _detectionLookup;
        private ComponentLookup<LiveMonster> _liveMonsterLookup;
        private ComponentLookup<LocalTransform> _transformLookup;
        private BufferLookup<NearbyHit> _hitBufferLookup;
        //额外添加的碰撞对查找
        private ComponentLookup<EnemyFlightProp> _enemyFlightPropLookup;
        private ComponentLookup<FlightPropDamageCalPar> _flightPropDamageCalParLookup;
        private ComponentLookup<SkillsDamageCalPar> _skillsDamageCalParLookup;
        private ComponentLookup<HeroEntityMasterTag> _heroEntityMasterTagLookup;
        private ComponentLookup<SkillArcaneCircleSecondTag> _skillArcaneCircleSecondTagLookup;
        private ComponentLookup<SkillArcaneCircleTag> _skillArcaneCircleTagLookup;


        // 所有用于分类的碰撞对容器
        private NativeQueue<TriggerPairData> _heroHitMonster;
        private NativeQueue<TriggerPairData> _enemyFlightHitHero;
        private NativeQueue<TriggerPairData> _flightHitMonster;
        private NativeQueue<TriggerPairData> _skillHitMonster;
        private NativeQueue<TriggerPairData> _arcaneCircleHitMonster;
        private NativeQueue<TriggerPairData> _arcaneCircleHitHero;

        //用于在job中并行的array
        public NativeArray<TriggerPairData> heroHitMonsterArray;
        public NativeArray<TriggerPairData> enemyFlightHitHeroArray;
        public NativeArray<TriggerPairData> flightHitMonsterArray;
        public NativeArray<TriggerPairData> skillHitMonsterArray;
        public NativeArray<TriggerPairData> arcaneCircleHitMonsterArray;
        public NativeArray<TriggerPairData> arcaneCircleHitHeroArray;


        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableDetectionSystemTag>();

            _detectionLookup = SystemAPI.GetComponentLookup<Detection_DefaultCmpt>(true);
            _liveMonsterLookup = SystemAPI.GetComponentLookup<LiveMonster>(true);
            _transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            _hitBufferLookup = SystemAPI.GetBufferLookup<NearbyHit>(false);

            //额外添加的碰撞对查找
            _enemyFlightPropLookup = SystemAPI.GetComponentLookup<EnemyFlightProp>(true);
            _flightPropDamageCalParLookup = SystemAPI.GetComponentLookup<FlightPropDamageCalPar>(true);
            _skillsDamageCalParLookup = SystemAPI.GetComponentLookup<SkillsDamageCalPar>(true);
            _heroEntityMasterTagLookup = SystemAPI.GetComponentLookup<HeroEntityMasterTag>(true);
            _skillArcaneCircleSecondTagLookup = SystemAPI.GetComponentLookup<SkillArcaneCircleSecondTag>(true);
            _skillArcaneCircleTagLookup = SystemAPI.GetComponentLookup<SkillArcaneCircleTag>(true);


            batchSize = UnityEngine.SystemInfo.processorCount > 8 ? 64 : 32;

            _heroHitMonster = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _enemyFlightHitHero = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _flightHitMonster = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _skillHitMonster = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _arcaneCircleHitMonster = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _arcaneCircleHitHero = new NativeQueue<TriggerPairData>(Allocator.Persistent);
        }

        public void OnUpdate(ref SystemState state)
        {
            // 更新查找
            _detectionLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _liveMonsterLookup.Update(ref state);
            _hitBufferLookup.Update(ref state);

            _enemyFlightPropLookup.Update(ref state);
            _flightPropDamageCalParLookup.Update(ref state);
            _skillsDamageCalParLookup.Update(ref state);
            _heroEntityMasterTagLookup.Update(ref state);
            _skillArcaneCircleSecondTagLookup.Update(ref state);
            _skillArcaneCircleTagLookup.Update(ref state);
            //清空区
            _heroHitMonster.Clear();
            _enemyFlightHitHero.Clear();
            _flightHitMonster.Clear();
            _skillHitMonster.Clear();
            //法阵技能二阶
            _arcaneCircleHitMonster.Clear();
            _arcaneCircleHitHero.Clear();

            DisposeArrayForCollison();

            var heroHitMonsterQueue = _heroHitMonster.AsParallelWriter();
            var enemyFlightHitHeroQueue = _enemyFlightHitHero.AsParallelWriter();
            var flightHitMonsterQueue = _flightHitMonster.AsParallelWriter();
            var skillHitMonsterQueue = _skillHitMonster.AsParallelWriter();
            var arcaneCircleHitMonsterQueue = _arcaneCircleHitMonster.AsParallelWriter();
            var arcaneCircleHitHeroQueue = _arcaneCircleHitHero.AsParallelWriter();

            // 1. 收集触发：把所有碰撞写入自己实体的 buffer,收集碰撞对的标准并行方式
            var sim = SystemAPI.GetSingleton<SimulationSingleton>();
            state.Dependency = new DetectionTriggerJob
            {
                DetectionLookup = _detectionLookup,
                TransformLookup = _transformLookup,
                HitBufferLookup = _hitBufferLookup,
                LiveMonsterLookup = _liveMonsterLookup,

                EnemyFlightPropLookup = _enemyFlightPropLookup,
                FlightPropDamageCalPrrLookup = _flightPropDamageCalParLookup,
                FlighPropDamageCalParLookup = _skillsDamageCalParLookup,
                HeroEntityMasterTagLookup = _heroEntityMasterTagLookup,
                SkillArcaneCircleSecondTagLookup = _skillArcaneCircleSecondTagLookup,
                SkillArcaneCircleTagLookup = _skillArcaneCircleTagLookup,

                HeroHitMonsterQueue = heroHitMonsterQueue,
                EnemyFlightHitHeroQueue = enemyFlightHitHeroQueue,
                FlightHitMonsterQueue = flightHitMonsterQueue,
                SkillHitMonsterQueue = skillHitMonsterQueue,
                ArcaneCircleHitMonsterQueue = arcaneCircleHitMonsterQueue,
                ArcaneCircleHitHeroQueue =arcaneCircleHitHeroQueue,
                
                

            }
            .Schedule(sim, state.Dependency);
            // 等待收集完成，这里收集原始碰撞数据，要等待完成
             state.Dependency.Complete();
            //这里会分配新的内存， 所以需要在开始释放
            heroHitMonsterArray = _heroHitMonster.ToArray(Allocator.Persistent);
            enemyFlightHitHeroArray = _enemyFlightHitHero.ToArray(Allocator.Persistent);
            flightHitMonsterArray = _flightHitMonster.ToArray(Allocator.Persistent);
            skillHitMonsterArray = _skillHitMonster.ToArray(Allocator.Persistent);
            arcaneCircleHitMonsterArray = _arcaneCircleHitMonster.ToArray(Allocator.Persistent);
            arcaneCircleHitHeroArray = _arcaneCircleHitHero.ToArray(Allocator.Persistent);


            // DevDebug.LogError(arcaneCircleHitMonsterArray.Length);
            //  CheckNumberOfDetection(ref state);



            // 2. 并行筛选：遍历每个实体的 buffer，选最近的目标并清空 buffer，不需要被依赖
            state.Dependency = new ApplyNearestJob()
                .ScheduleParallel(state.Dependency);

        }



        void CheckNumberOfDetection(ref SystemState state)
        {

            state.Dependency.Complete();
            {
                // 创建一个只包含 Detection_DefaultCmpt + NearbyHit buffer 的查询
                var query = state.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<HeroAttackTarget>(),
                    ComponentType.ReadOnly<NearbyHit>()
                );

                // 拿到所有符合条件的实体
                using var entities = query.ToEntityArray(Allocator.Temp);
                // hitBufferLookup 已经在前面 Update 过了

                foreach (var e in entities)
                {
                    // 直接通过 BufferLookup 取出 buffer，再读 Length
                    var buf = _hitBufferLookup[e];
                    UnityEngine.Debug.Log($"实体 {e.Index} 本帧命中次数：{buf.Length}");
                }
            }
        }
        public void OnDestroy(ref SystemState state) 
        {
            //释放所有队列内存
            _heroHitMonster.Clear();
            _enemyFlightHitHero.Clear();
            _flightHitMonster.Clear();
            _skillHitMonster.Clear();
            _arcaneCircleHitMonster.Clear();
            _arcaneCircleHitHero.Clear();



        }


        void DisposeArrayForCollison()
        {
            //施放所有碰撞数组内存
            if (heroHitMonsterArray.IsCreated) heroHitMonsterArray.Dispose();
            if (enemyFlightHitHeroArray.IsCreated) enemyFlightHitHeroArray.Dispose();
            if (flightHitMonsterArray.IsCreated) flightHitMonsterArray.Dispose();
            if (skillHitMonsterArray.IsCreated) skillHitMonsterArray.Dispose();
            if (arcaneCircleHitMonsterArray.IsCreated) arcaneCircleHitMonsterArray.Dispose();
            if (arcaneCircleHitHeroArray.IsCreated) arcaneCircleHitHeroArray.Dispose();



        }
    }
    /// <summary>
    /// 综合碰撞对收集
    /// </summary>
    [BurstCompile]
    struct DetectionTriggerJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<Detection_DefaultCmpt> DetectionLookup; // 基础检测组件
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup; // 用于计算距离
        [ReadOnly] public ComponentLookup<LiveMonster> LiveMonsterLookup; // 怪物存活状态

        [ReadOnly] public ComponentLookup<EnemyFlightProp> EnemyFlightPropLookup; // 怪物飞行道具
        [ReadOnly] public ComponentLookup<FlightPropDamageCalPar> FlightPropDamageCalPrrLookup; // 基础飞行道具
        [ReadOnly] public ComponentLookup<SkillsDamageCalPar> FlighPropDamageCalParLookup; // 技能飞行道具
        [ReadOnly] public ComponentLookup<HeroEntityMasterTag> HeroEntityMasterTagLookup; // 英雄主体
        [ReadOnly] public ComponentLookup<SkillArcaneCircleTag> SkillArcaneCircleTagLookup; // 技能法阵第一阶段
        [ReadOnly] public ComponentLookup<SkillArcaneCircleSecondTag> SkillArcaneCircleSecondTagLookup; // 技能法阵第二阶段

        public BufferLookup<NearbyHit> HitBufferLookup; // 基础检测系统

        public NativeQueue<TriggerPairData>.ParallelWriter HeroHitMonsterQueue;
        public NativeQueue<TriggerPairData>.ParallelWriter EnemyFlightHitHeroQueue;
        public NativeQueue<TriggerPairData>.ParallelWriter FlightHitMonsterQueue;
        public NativeQueue<TriggerPairData>.ParallelWriter SkillHitMonsterQueue;
        public NativeQueue<TriggerPairData>.ParallelWriter ArcaneCircleHitMonsterQueue;
        public NativeQueue<TriggerPairData>.ParallelWriter ArcaneCircleHitHeroQueue;
   

        public void Execute(TriggerEvent triggerEvent)
        {
            var a = triggerEvent.EntityA;
            var b = triggerEvent.EntityB;

            

            // 检测周围实体（玩家→怪物）
            CheckAndAddNearbyHit(a, b);
            CheckAndAddNearbyHit(b, a);

            // 分类写入碰撞对容器
            AddIfMatch(a, b, HeroEntityMasterTagLookup, LiveMonsterLookup, HeroHitMonsterQueue, true);
            AddIfMatch(b, a, HeroEntityMasterTagLookup, LiveMonsterLookup, HeroHitMonsterQueue, true);

            AddIfMatch(a, b, EnemyFlightPropLookup, HeroEntityMasterTagLookup, EnemyFlightHitHeroQueue, false);
            AddIfMatch(b, a, EnemyFlightPropLookup, HeroEntityMasterTagLookup, EnemyFlightHitHeroQueue, false);

            AddIfMatch(a, b, FlightPropDamageCalPrrLookup, LiveMonsterLookup, FlightHitMonsterQueue, true);
            AddIfMatch(b, a, FlightPropDamageCalPrrLookup, LiveMonsterLookup, FlightHitMonsterQueue, true);

            AddIfMatch(a, b, FlighPropDamageCalParLookup, LiveMonsterLookup, SkillHitMonsterQueue, true);
            AddIfMatch(b, a, FlighPropDamageCalParLookup, LiveMonsterLookup, SkillHitMonsterQueue, true);
            //怪物与法阵碰撞
            AddIfMatch(a, b, SkillArcaneCircleSecondTagLookup, LiveMonsterLookup, ArcaneCircleHitMonsterQueue, true);
            AddIfMatch(b, a, SkillArcaneCircleSecondTagLookup, LiveMonsterLookup, ArcaneCircleHitMonsterQueue, true);

            //英雄与法阵本体碰撞,英雄可以在阶段内自行判断
            AddIfMatchSimple(a, b, HeroEntityMasterTagLookup, SkillArcaneCircleTagLookup, ArcaneCircleHitHeroQueue);
            AddIfMatchSimple(b, a, HeroEntityMasterTagLookup, SkillArcaneCircleTagLookup, ArcaneCircleHitHeroQueue);





        }

        private void CheckAndAddNearbyHit(Entity detector, Entity other)
        {
            if (!DetectionLookup.HasComponent(detector) ||
                !TransformLookup.HasComponent(detector) ||
                !TransformLookup.HasComponent(other) ||
                !LiveMonsterLookup.HasComponent(other) ||
                !LiveMonsterLookup.IsComponentEnabled(other))
                return;

            float d = math.distancesq(
                TransformLookup[detector].Position,
                TransformLookup[other].Position);

            var detection = DetectionLookup[detector];
            Entity bufferTarget = detection.bufferOwner;

            if (HitBufferLookup.HasBuffer(bufferTarget))
            {
                var buf = HitBufferLookup[bufferTarget];
                if (buf.Length < buf.Capacity)
                    buf.Add(new NearbyHit { other = other, sqrDist = d });
            }
        }

        private void AddIfMatch<TA, TB>(
            Entity a, Entity b,
            ComponentLookup<TA> lookupA,
            ComponentLookup<TB> lookupB,
            NativeQueue<TriggerPairData>.ParallelWriter queue,
            bool checkLiveMonster)
            where TA : unmanaged, IComponentData
            where TB : unmanaged, IComponentData
        {
            if (lookupA.HasComponent(a) && lookupB.HasComponent(b))
            {
                if (!checkLiveMonster || LiveMonsterLookup.IsComponentEnabled(b))
                {
                    queue.Enqueue(new TriggerPairData { EntityA = a, EntityB = b });
                }
            }
        }

        private void AddIfMatchSimple<TA, TB>(
    Entity a, Entity b,
    ComponentLookup<TA> lookupA,
    ComponentLookup<TB> lookupB,
    NativeQueue<TriggerPairData>.ParallelWriter queue)
    where TA : unmanaged, IComponentData
    where TB : unmanaged, IComponentData
        {
            if (lookupA.HasComponent(a) && lookupB.HasComponent(b))
            {
                queue.Enqueue(new TriggerPairData { EntityA = a, EntityB = b });
            }
        }
    }


    




        [BurstCompile]
        partial struct ApplyNearestJob : IJobEntity
        {
            public void Execute(ref HeroAttackTarget det, DynamicBuffer<NearbyHit> hits)
            {
                // 如果这一帧没有任何命中，就清空目标
                if (hits.Length == 0)
                {
                    det.attackTarget = Entity.Null;
                    return;
                }

                // 否则遍历 buffer，找到距离平方最小的实体
                float best = float.MaxValue;
                Entity target = Entity.Null;

                for (int i = 0; i < hits.Length; i++)
                {
                    var h = hits[i];
                    if (h.sqrDist < best)
                    {
                        best = h.sqrDist;
                        target = h.other;
                    }
                }

                det.attackTarget = target;

                // 清空以备下一帧
                hits.Clear();
            }
        }

    
}
