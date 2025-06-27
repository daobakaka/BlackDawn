using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

namespace BlackDawn.DOTS
{
    [RequireMatchingQueriesForUpdate]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
   // [UpdateInGroup(typeof(ActionSystemGroup))]
    [BurstCompile]
    public partial struct OverlapDetectionSystem : ISystem
    {
        // ������������
        private NativeQueue<TriggerPairData> _detectionOverlapMonster;
        private NativeQueue<TriggerPairData> _monsterOverlapPlayer;
        private NativeQueue<TriggerPairData> _skillOverlapMonster;

        // ����ֻ������
        public NativeArray<TriggerPairData> detectionOverlapMonsterArray;
        public NativeArray<TriggerPairData> monsterOverlapPlayerArray;
        public NativeArray<TriggerPairData> skillOverlapMonsterArray;

        // ��ѯ���
        private EntityQuery _overlapQuery;
        private ComponentLookup<Detection_DefaultCmpt> _detectionTagLookup;
        private ComponentLookup<LiveMonster> _monsterTagLookup;
        private ComponentLookup<SkillsOverTimeDamageCalPar> _skillTagLookup;


        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            //���ⲿͳһ����
            state.RequireForUpdate<EnableOverlapDetectionSystemTag>();

            _detectionOverlapMonster = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _monsterOverlapPlayer = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _skillOverlapMonster = new NativeQueue<TriggerPairData>(Allocator.Persistent);

            detectionOverlapMonsterArray = default;
            monsterOverlapPlayerArray = default;
            skillOverlapMonsterArray = default;

            _overlapQuery = state.GetEntityQuery(ComponentType.ReadOnly<OverlapQueryCenter>());

            // ����������� PlayerTag��MonsterTag��SkillTag
            _detectionTagLookup = state.GetComponentLookup<Detection_DefaultCmpt>(true);
            _monsterTagLookup = state.GetComponentLookup<LiveMonster>(true);
            _skillTagLookup = state.GetComponentLookup<SkillsOverTimeDamageCalPar>(true);
        }



        private void DisposeOverLapArrays()
        {
            if (detectionOverlapMonsterArray.IsCreated) detectionOverlapMonsterArray.Dispose();
            if (monsterOverlapPlayerArray.IsCreated) monsterOverlapPlayerArray.Dispose();
            if (skillOverlapMonsterArray.IsCreated) skillOverlapMonsterArray.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            DisposeOverLapArrays();
            _detectionOverlapMonster.Clear();
            _monsterOverlapPlayer.Clear();
            _skillOverlapMonster.Clear();

            // ÿ֡ͬ��Lookup����ֹʧЧ
            _detectionTagLookup.Update(ref state);
            _monsterTagLookup.Update(ref state);
            _skillTagLookup.Update(ref state);

            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

            var entities = _overlapQuery.ToEntityArray(state.WorldUpdateAllocator);
            var centers = _overlapQuery.ToComponentDataArray<OverlapQueryCenter>(state.WorldUpdateAllocator);


            var sim = SystemAPI.GetSingleton<SimulationSingleton>();

            //sim.Dependency.Complete();
            // ������ PhysicsSystemGroup.Dependency


            // JOB׼����ÿ֡�������
            state.Dependency = new OverlapDetectionJob
            {
                PhysicsWorld = physicsWorld,
                Entities = entities,
                Centers = centers,
                DetectionTagLookup = _detectionTagLookup,
                MonsterTagLookup = _monsterTagLookup,
                SkillTagLookup = _skillTagLookup,
                DetectionOverlapMonsterQueue = _detectionOverlapMonster.AsParallelWriter(),
                MonsterOverlapPlayerQueue = _monsterOverlapPlayer.AsParallelWriter(),
                SkillOverlapMonsterQueue = _skillOverlapMonster.AsParallelWriter(),
            }.Schedule(entities.Length, 64,state.Dependency);

            state.Dependency.Complete();
            // JOB�󣺶���ת���飬���Ⱪ¶
            detectionOverlapMonsterArray = _detectionOverlapMonster.ToArray(Allocator.Persistent);
            monsterOverlapPlayerArray = _monsterOverlapPlayer.ToArray(Allocator.Persistent);
            skillOverlapMonsterArray = _skillOverlapMonster.ToArray(Allocator.Persistent);


           // DevDebug.Log("��⵽������" + detectionOverlapMonsterArray.Length);
        }

        public void OnDestroy(ref SystemState state)
        {
            _detectionOverlapMonster.Dispose();
            _monsterOverlapPlayer.Dispose();
            _skillOverlapMonster.Dispose();

            DisposeOverLapArrays();
        }

    }
    [BurstCompile]
     struct OverlapDetectionJob : IJobParallelFor
    {
        [ReadOnly] public PhysicsWorld PhysicsWorld;
        [ReadOnly] public NativeArray<Entity> Entities;
        [ReadOnly] public NativeArray<OverlapQueryCenter> Centers;
        [ReadOnly] public ComponentLookup<Detection_DefaultCmpt> DetectionTagLookup;
        [ReadOnly] public ComponentLookup<LiveMonster> MonsterTagLookup;
        [ReadOnly] public ComponentLookup<SkillsOverTimeDamageCalPar> SkillTagLookup;

        public NativeQueue<TriggerPairData>.ParallelWriter DetectionOverlapMonsterQueue;
        public NativeQueue<TriggerPairData>.ParallelWriter MonsterOverlapPlayerQueue;
        public NativeQueue<TriggerPairData>.ParallelWriter SkillOverlapMonsterQueue;

        public void Execute(int index)
        {
            var entity = Entities[index];
            var center = Centers[index].Center;
            var radius = Centers[index].Radius;
            var filter = Centers[index].Filter;

            var hits = new NativeList<DistanceHit>(Allocator.Temp);
            var input = new PointDistanceInput
            {
                Position = center,
                MaxDistance = radius,
                Filter = filter
            };
            PhysicsWorld.CalculateDistance(input, ref hits);

            for (int j = 0; j < hits.Length; j++)
            {
                var targetEntity = PhysicsWorld.Bodies[hits[j].RigidBodyIndex].Entity;
                if (targetEntity == entity) continue;

                // �������ֻ��ComponentLookup�ж����ͼ���
                bool isPlayer = DetectionTagLookup.HasComponent(entity);
                bool isMonster = MonsterTagLookup.HasComponent(entity);
                bool isSkill = SkillTagLookup.HasComponent(entity);

                bool isTargetPlayer = DetectionTagLookup.HasComponent(targetEntity);
                bool isTargetMonster = MonsterTagLookup.HasComponent(targetEntity);

                if (isPlayer && isTargetMonster)
                    DetectionOverlapMonsterQueue.Enqueue(new TriggerPairData { EntityA = entity, EntityB = targetEntity });
                else if (isMonster && isTargetPlayer)
                    MonsterOverlapPlayerQueue.Enqueue(new TriggerPairData { EntityA = entity, EntityB = targetEntity });
                else if (isSkill && isTargetMonster)
                    SkillOverlapMonsterQueue.Enqueue(new TriggerPairData { EntityA = entity, EntityB = targetEntity });
            }
            hits.Dispose();
        }
    }
}
