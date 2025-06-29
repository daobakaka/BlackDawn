using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Profiling;
using Unity.Transforms;

namespace BlackDawn.DOTS
{
    [RequireMatchingQueriesForUpdate]
    [UpdateBefore(typeof(DetectionSystem))]
    [UpdateInGroup(typeof(ActionSystemGroup))]
    [BurstCompile]
    public partial struct OverlapDetectionSystem : ISystem
    {
        // 检测结果分类队列
        private NativeQueue<TriggerPairData> _detectionOverlapMonster;
        private NativeQueue<TriggerPairData> _skillOverlapMonster;

        // 对外只读数组
        public NativeArray<TriggerPairData> detectionOverlapMonsterArray;
        public NativeArray<TriggerPairData> skillOverlapMonsterArray;

        // 查询句柄
        private EntityQuery _overlapQuery;
        private ComponentLookup<Detection_DefaultCmpt> _detectionTagLookup;
        private ComponentLookup<LiveMonster> _monsterTagLookup;
        private ComponentLookup<SkillsOverTimeDamageCalPar> _skillTagLookup;
        private ComponentLookup<LocalTransform> _transformLookup;
        private BufferLookup<NearbyHit> _hitBufferLookup;
        private ComponentLookup<OverlapQueryCenter> _overlapQueryCenterLookup;

       



        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            //有外部统一开启
            state.RequireForUpdate<EnableOverlapDetectionSystemTag>();

            _detectionOverlapMonster = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _skillOverlapMonster = new NativeQueue<TriggerPairData>(Allocator.Persistent);

            detectionOverlapMonsterArray = default;
            skillOverlapMonsterArray = default;

            _overlapQuery = state.GetEntityQuery(ComponentType.ReadOnly<OverlapQueryCenter>());

            // 这里假设你有 PlayerTag、MonsterTag、SkillTag
            _detectionTagLookup = state.GetComponentLookup<Detection_DefaultCmpt>(true);
            _monsterTagLookup = state.GetComponentLookup<LiveMonster>(true);
            _skillTagLookup = state.GetComponentLookup<SkillsOverTimeDamageCalPar>(true);

            _transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            _hitBufferLookup = SystemAPI.GetBufferLookup<NearbyHit>(false);
            _overlapQueryCenterLookup =SystemAPI.GetComponentLookup<OverlapQueryCenter>(false);
        }



        private void DisposeOverLapArrays()
        {
            if (skillOverlapMonsterArray.IsCreated) skillOverlapMonsterArray.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

            state.Dependency.Complete();
            DisposeOverLapArrays();
            _skillOverlapMonster.Clear();

            // 每帧同步Lookup，防止失效
            _detectionTagLookup.Update(ref state);
            _monsterTagLookup.Update(ref state);
            _skillTagLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _hitBufferLookup.Update(ref state);
            _overlapQueryCenterLookup.Update(ref state);


            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

            var entities = _overlapQuery.ToEntityArray(state.WorldUpdateAllocator);
            var centers = _overlapQuery.ToComponentDataArray<OverlapQueryCenter>(state.WorldUpdateAllocator);

            Entity detectionEntiy = Entity.Null;
            Entity heroEntity = Entity.Null;
            if (SystemAPI.HasSingleton<Detection_DefaultCmpt>())
             detectionEntiy = SystemAPI.GetSingletonEntity<Detection_DefaultCmpt>();
            if (SystemAPI.HasSingleton<HeroEntityMasterTag>())
              heroEntity = SystemAPI.GetSingletonEntity<HeroEntityMasterTag>();


            // JOB准备：每帧检测点参数
            state.Dependency= new SkillOverlapDetectionJob
            {
                PhysicsWorld = physicsWorld,
                Entities = entities,
                Centers = centers,
                MonsterTagLookup = _monsterTagLookup,
                SkillTagLookup = _skillTagLookup,

                SkillOverlapMonsterQueue = _skillOverlapMonster.AsParallelWriter(),
            }.ScheduleParallel(entities.Length, 1, state.Dependency);
            state.Dependency.Complete();

            // 2. 单独的侦测器的buffer检测，走单一的调度
            state.Dependency = new DetectionBufferWriteJob
            {
                PhysicsWorld = physicsWorld,
                Detector = detectionEntiy,
                DetectionTagLookup = _detectionTagLookup,
                MonsterTagLookup = _monsterTagLookup,
                TransformTagLookup = _transformLookup,
                HitBufferLookup = _hitBufferLookup,
                OverlapQueryLookup=_overlapQueryCenterLookup,
            }.Schedule();


            // 2. 并行筛选：遍历每个实体的 buffer，选最近的目标并清空 buffer，不需要被依赖
            state.Dependency = new ApplyNearestJob 
            {DetectionTagLookup=_detectionTagLookup,
            OverlapQueryLookup = _overlapQueryCenterLookup,
            Detector    =detectionEntiy,                 
            }
            .ScheduleParallel(state.Dependency);

            // JOB后：队列转数组，对外暴露
            skillOverlapMonsterArray = _skillOverlapMonster.ToArray(Allocator.Persistent);


            //if (skillOverlapMonsterArray.Length > 0)
            //    DevDebug.Log("检测到的数量" + skillOverlapMonsterArray.Length);
        }

        public void OnDestroy(ref SystemState state)
        {
            _detectionOverlapMonster.Dispose();
            _skillOverlapMonster.Dispose();

            DisposeOverLapArrays();
        }

    }
    /// <summary>
    ///持续性技能检测
    /// </summary>
    [BurstCompile]
    struct SkillOverlapDetectionJob : IJobFor
    {
        [ReadOnly] public PhysicsWorld PhysicsWorld;
        [ReadOnly] public NativeArray<Entity> Entities;
        [ReadOnly] public NativeArray<OverlapQueryCenter> Centers;
        [ReadOnly] public ComponentLookup<SkillsOverTimeDamageCalPar> SkillTagLookup;
        [ReadOnly] public ComponentLookup<LiveMonster> MonsterTagLookup;

        public NativeQueue<TriggerPairData>.ParallelWriter SkillOverlapMonsterQueue;

        public void Execute(int index)
        {
            var entity = Entities[index];
            var center = Centers[index].Center;
            var offset = Centers[index].offset;
            var radius = Centers[index].Radius;
            var filter = Centers[index].Filter;

            var hits = new NativeList<DistanceHit>(Allocator.Temp);
            var input = new PointDistanceInput
            {
                Position = center + offset,
                MaxDistance = radius,
                Filter = filter
            };
            PhysicsWorld.CalculateDistance(input, ref hits);

            for (int j = 0; j < hits.Length; j++)
            {
                var targetEntity = PhysicsWorld.Bodies[hits[j].RigidBodyIndex].Entity;
                if (targetEntity == entity) continue;

                bool isSkill = SkillTagLookup.HasComponent(entity);
                bool isTargetMonster = MonsterTagLookup.HasComponent(targetEntity);

                if (isSkill && isTargetMonster)
                {
                    SkillOverlapMonsterQueue.Enqueue(new TriggerPairData { EntityA = entity, EntityB = targetEntity });
                }
            }
            hits.Dispose();
        }
    }




    /// <summary>
    /// 侦测器检测
    /// </summary>
    struct DetectionBufferWriteJob : IJob
    {
        [ReadOnly] public PhysicsWorld PhysicsWorld;
        [ReadOnly] public Entity Detector;
        [ReadOnly] public ComponentLookup<Detection_DefaultCmpt> DetectionTagLookup;
        [ReadOnly] public ComponentLookup<LiveMonster> MonsterTagLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformTagLookup;
        [ReadOnly] public ComponentLookup<OverlapQueryCenter> OverlapQueryLookup;
        public BufferLookup<NearbyHit> HitBufferLookup;

        public void Execute()
        {
            if (!DetectionTagLookup.HasComponent(Detector))
                return;
            if (!OverlapQueryLookup.HasComponent(Detector))
                return;

            var overlap = OverlapQueryLookup[Detector];
            var center = overlap.Center;
            var offset = overlap.offset;
            var radius = overlap.Radius;
            var filter = overlap.Filter;

            var hits = new NativeList<DistanceHit>(Allocator.Temp);
            var input = new PointDistanceInput
            {
                Position = center + offset,
                MaxDistance = radius,
                Filter = filter
            };
            PhysicsWorld.CalculateDistance(input, ref hits);

            for (int j = 0; j < hits.Length; j++)
            {
                var other = PhysicsWorld.Bodies[hits[j].RigidBodyIndex].Entity;
                if (other == Detector) continue;

                if (!MonsterTagLookup.HasComponent(other)) continue;
                if (!MonsterTagLookup.IsComponentEnabled(other)) continue;
                if (!TransformTagLookup.HasComponent(other) || !TransformTagLookup.HasComponent(Detector)) continue;

                float d = math.distancesq(
                    TransformTagLookup[Detector].Position,
                    TransformTagLookup[other].Position);

                var detection = DetectionTagLookup[Detector];
                Entity bufferTarget = detection.bufferOwner;

                if (HitBufferLookup.HasBuffer(bufferTarget))
                {
                    var buf = HitBufferLookup[bufferTarget];
                    if (buf.Length < buf.Capacity)
                        buf.Add(new NearbyHit { other = other, sqrDist = d });
                }
            }
            hits.Dispose();
        }
    }

    [BurstCompile]
    partial struct ApplyNearestJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<Detection_DefaultCmpt> DetectionTagLookup;
        [ReadOnly] public Entity Detector;
        [NativeDisableParallelForRestriction]
         public ComponentLookup<OverlapQueryCenter> OverlapQueryLookup;
        public void Execute(ref HeroAttackTarget det,in LocalTransform  transform ,HeroEntityMasterTag  masterTag, DynamicBuffer<NearbyHit> hits)
        {

            var detection_DefaultCmpt = DetectionTagLookup[Detector];
            var overlap = OverlapQueryLookup[Detector];

            overlap.Center = transform.Position;

            if (hits.Length > 20)
                overlap.Radius = math.max(overlap.Radius - 1f, 5f); // 最小5
            else if (hits.Length <= 0)

            {
                overlap.Radius = math.min(overlap.Radius + 1f, detection_DefaultCmpt.originalRadius); // 最大originalRadius
                det.attackTarget = Entity.Null;
                OverlapQueryLookup[Detector] = overlap;
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
            OverlapQueryLookup[Detector] = overlap;

            // 清空以备下一帧
            hits.Clear();
        }
    }

}
