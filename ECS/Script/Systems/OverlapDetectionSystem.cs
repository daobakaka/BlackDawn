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
        // ������������
        private NativeQueue<TriggerPairData> _detectionOverlapMonster;
        private NativeQueue<TriggerPairData> _skillOverlapMonster;

        // ����ֻ������
        public NativeArray<TriggerPairData> detectionOverlapMonsterArray;
        public NativeArray<TriggerPairData> skillOverlapMonsterArray;

        // ��ѯ���
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
            //���ⲿͳһ����
            state.RequireForUpdate<EnableOverlapDetectionSystemTag>();

            _detectionOverlapMonster = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _skillOverlapMonster = new NativeQueue<TriggerPairData>(Allocator.Persistent);

            detectionOverlapMonsterArray = default;
            skillOverlapMonsterArray = default;

            _overlapQuery = state.GetEntityQuery(ComponentType.ReadOnly<OverlapQueryCenter>());

            // ����������� PlayerTag��MonsterTag��SkillTag
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

            // ÿ֡ͬ��Lookup����ֹʧЧ
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


            // JOB׼����ÿ֡�������
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

            // 2. �������������buffer��⣬�ߵ�һ�ĵ���
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


            // 2. ����ɸѡ������ÿ��ʵ��� buffer��ѡ�����Ŀ�겢��� buffer������Ҫ������
            state.Dependency = new ApplyNearestJob 
            {DetectionTagLookup=_detectionTagLookup,
            OverlapQueryLookup = _overlapQueryCenterLookup,
            Detector    =detectionEntiy,                 
            }
            .ScheduleParallel(state.Dependency);

            // JOB�󣺶���ת���飬���Ⱪ¶
            skillOverlapMonsterArray = _skillOverlapMonster.ToArray(Allocator.Persistent);


            //if (skillOverlapMonsterArray.Length > 0)
            //    DevDebug.Log("��⵽������" + skillOverlapMonsterArray.Length);
        }

        public void OnDestroy(ref SystemState state)
        {
            _detectionOverlapMonster.Dispose();
            _skillOverlapMonster.Dispose();

            DisposeOverLapArrays();
        }

    }
    /// <summary>
    ///�����Լ��ܼ��
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
    /// ��������
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
                overlap.Radius = math.max(overlap.Radius - 1f, 5f); // ��С5
            else if (hits.Length <= 0)

            {
                overlap.Radius = math.min(overlap.Radius + 1f, detection_DefaultCmpt.originalRadius); // ���originalRadius
                det.attackTarget = Entity.Null;
                OverlapQueryLookup[Detector] = overlap;
                return;
            }

            // ������� buffer���ҵ�����ƽ����С��ʵ��
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

            // ����Ա���һ֡
            hits.Clear();
        }
    }

}
