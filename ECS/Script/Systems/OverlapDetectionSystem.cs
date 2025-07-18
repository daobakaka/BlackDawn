using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Profiling;
using Unity.Transforms;
#if UNITY_EDITOR
using UnityEditor.Search;
#endif

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
        private NativeQueue<TriggerPairData> _skillOverTimeOverlapMonster;
        private NativeQueue<TriggerPairData> _skillBurstOverlapMonster;
        private NativeQueue<TriggerPairData> _skillTrackingOverlapMonster;
        // 对外只读数组
        public NativeArray<TriggerPairData> detectionOverlapMonsterArray;
        public NativeArray<TriggerPairData> skillOverTimeOverlapMonsterArray;
        public NativeArray<TriggerPairData> skillBurstOverlapMonsterArray;
        public NativeArray<TriggerPairData> skillTrackingOverlapMonsterArray;

        // 查询句柄
        private EntityQuery _overlapOverTimeQuery;
        private EntityQuery _overlapBurstQuery;
        private EntityQuery _overlapTrackingQuery;

        private ComponentLookup<Detection_DefaultCmpt> _detectionTagLookup;
        private ComponentLookup<LiveMonster> _monsterTagLookup;
        private ComponentLookup<SkillsOverTimeDamageCalPar> _skillOverTimeTagLookup;
        private ComponentLookup<SkillsBurstDamageCalPar> _skillBurstTagLookup;
        private ComponentLookup<SkillsTrackingCalPar> _skillTrackingTagLookup;


        private BufferLookup<TrackingRecord> _trackingBufferLookup;

        private ComponentLookup<LocalTransform> _transformLookup;
        private BufferLookup<NearbyHit> _hitBufferLookup;
        private ComponentLookup<OverlapOverTimeQueryCenter> _overlapOverTimeQueryCenterLookup;
        private ComponentLookup<LocalToWorld> _localToWorldLookup;





        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            //有外部统一开启
            state.RequireForUpdate<EnableOverlapDetectionSystemTag>();

            _detectionOverlapMonster = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _skillOverTimeOverlapMonster = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _skillBurstOverlapMonster = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _skillTrackingOverlapMonster = new NativeQueue<TriggerPairData>(Allocator.Persistent);


            detectionOverlapMonsterArray = default;
            skillOverTimeOverlapMonsterArray = default;
            skillBurstOverlapMonsterArray = default;
            skillTrackingOverlapMonsterArray = default;

            _overlapOverTimeQuery = state.GetEntityQuery(ComponentType.ReadOnly<OverlapOverTimeQueryCenter>());
            _overlapBurstQuery = state.GetEntityQuery(ComponentType.ReadOnly<OverlapBurstQueryCenter>());
            _overlapTrackingQuery = state.GetEntityQuery(ComponentType.ReadOnly<OverlapTrackingQueryCenter>());

            // 有 PlayerTag、MonsterTag、SkillTag
            _detectionTagLookup = state.GetComponentLookup<Detection_DefaultCmpt>(true);
            _monsterTagLookup = state.GetComponentLookup<LiveMonster>(true);
            _skillOverTimeTagLookup = state.GetComponentLookup<SkillsOverTimeDamageCalPar>(true);
            _skillBurstTagLookup = state.GetComponentLookup<SkillsBurstDamageCalPar>(true);
            _skillTrackingTagLookup = state.GetComponentLookup<SkillsTrackingCalPar>(true);

            _transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            _hitBufferLookup = SystemAPI.GetBufferLookup<NearbyHit>(false);
            _trackingBufferLookup = SystemAPI.GetBufferLookup<TrackingRecord>(false);
            _overlapOverTimeQueryCenterLookup = SystemAPI.GetComponentLookup<OverlapOverTimeQueryCenter>(false);
            _localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);

        }



        private void DisposeOverLapArrays()
        {
            if (skillOverTimeOverlapMonsterArray.IsCreated) skillOverTimeOverlapMonsterArray.Dispose();
            if (skillBurstOverlapMonsterArray.IsCreated) skillBurstOverlapMonsterArray.Dispose();
            if (skillTrackingOverlapMonsterArray.IsCreated) skillTrackingOverlapMonsterArray.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

            state.Dependency.Complete();
            DisposeOverLapArrays();
            _skillOverTimeOverlapMonster.Clear();
            _skillBurstOverlapMonster.Clear();
            _skillTrackingOverlapMonster.Clear();

            // 每帧同步Lookup，防止失效
            _detectionTagLookup.Update(ref state);
            _monsterTagLookup.Update(ref state);
            _skillOverTimeTagLookup.Update(ref state);
            _skillBurstTagLookup.Update(ref state);
            _skillTrackingTagLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _hitBufferLookup.Update(ref state);
            _trackingBufferLookup.Update(ref state);
            _overlapOverTimeQueryCenterLookup.Update(ref state);
            _localToWorldLookup.Update(ref state);
            //侦察系统采用 物理模系统初始化ECB 便于跨帧检测
            var ecb = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
 
                    

            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

            var entitiesOverTime = _overlapOverTimeQuery.ToEntityArray(state.WorldUpdateAllocator);
            var centersOverTime = _overlapOverTimeQuery.ToComponentDataArray<OverlapOverTimeQueryCenter>(state.WorldUpdateAllocator);

            var entitiesBurst = _overlapBurstQuery.ToEntityArray(state.WorldUpdateAllocator);
            var centersBurst = _overlapBurstQuery.ToComponentDataArray<OverlapBurstQueryCenter>(state.WorldUpdateAllocator);


            var entitiesTracking = _overlapTrackingQuery.ToEntityArray(state.WorldUpdateAllocator);
            var centersTracking = _overlapTrackingQuery.ToComponentDataArray<OverlapTrackingQueryCenter>(state.WorldUpdateAllocator);

            Entity detectionEntiy = Entity.Null;
            Entity heroEntity = Entity.Null;
            if (SystemAPI.HasSingleton<Detection_DefaultCmpt>())
                detectionEntiy = SystemAPI.GetSingletonEntity<Detection_DefaultCmpt>();
            if (SystemAPI.HasSingleton<HeroEntityMasterTag>())
                heroEntity = SystemAPI.GetSingletonEntity<HeroEntityMasterTag>();

            // 侦测型技能销毁 --闪电链！！这里会引起侦察系统报错，暂时不知为什么，目前认为 快照Ijobfor 要使用同样的ecb或者之前的ECB写入才不会报错！！
            //临时ECB 写回 也不行， 必须是ECB 周期之前的ECB ，与脚本的运行顺序似乎无关     
            foreach (var (skillTrackingCal, entity) in SystemAPI.Query<RefRO<SkillsTrackingCalPar>>().WithEntityAccess())
            {
                if (skillTrackingCal.ValueRO.destory == true)
                    ecb.DestroyEntity(entity);

            }
     


            // 1:通用持续性技能的检测
            state.Dependency = new SkillOverTimeOverlapDetectionJob
            {
                PhysicsWorld = physicsWorld,
                Entities = entitiesOverTime,
                Centers = centersOverTime,
                MonsterTagLookup = _monsterTagLookup,
                SkillTagLookup = _skillOverTimeTagLookup,
                LocalToWorldLookup = _localToWorldLookup,

                SkillOverlapMonsterQueue = _skillOverTimeOverlapMonster.AsParallelWriter(),
            }.ScheduleParallel(entitiesOverTime.Length, 1, state.Dependency);
            state.Dependency.Complete();
            //2.通用爆发性技能碰撞检测
            state.Dependency = new SkillBurstOverlapDetectionJob
            {
                PhysicsWorld = physicsWorld,
                Entities = entitiesBurst,
                Centers = centersBurst,
                MonsterTagLookup = _monsterTagLookup,
                SkillTagLookup = _skillBurstTagLookup,
                LocalToWorldLookup = _localToWorldLookup,
                SkillOverlapMonsterQueue = _skillBurstOverlapMonster.AsParallelWriter(),
            }.ScheduleParallel(entitiesBurst.Length, 1, state.Dependency);
            state.Dependency.Complete();

            //3.通用追踪型技能碰撞检测，收集碰撞对，但不计算伤害，用于寻踪,这里8个一个批次
            //这里暂时不收集对列，不涉及伤害计算， 应该直接走buffer系统来进行目标确定
            //对于技能体，是否需要改变目标的回调，还需要通过碰撞事件触发

            state.Dependency = new SkillTrackingOverlapDetectionJob

            {
                PhysicsWorld = physicsWorld,
                Entities = entitiesTracking,
                Centers = centersTracking,
                ECB = ecb.AsParallelWriter(),
                TrackingRecordBufferLookup = _trackingBufferLookup,
                Transform = _transformLookup,
                MonsterTagLookup = _monsterTagLookup,
                SkillTagLookup = _skillTrackingTagLookup,
                LocalToWorldLookup = _localToWorldLookup,
                SkillOverlapMonsterQueue = _skillTrackingOverlapMonster.AsParallelWriter(),
            }.ScheduleParallel(entitiesTracking.Length, 1, state.Dependency);
            //连锁吞噬的特殊技能回调，使用同一个ecb 这里也需要阻塞？
            state.Dependency.Complete();
            //3-1 依赖追踪范围检测的job 处理，这里使用不同job进行处理
            // state.Dependency = new TrackingBufferDealJob
            // {
            //     Time = (float)SystemAPI.Time.ElapsedTime,
            //     DeltaTime = SystemAPI.Time.DeltaTime,

            // }.ScheduleParallel(state.Dependency);

            // 4. 单独的侦测器的buffer检测，走单一的调度
            state.Dependency = new DetectionBufferWriteJob
            {
                PhysicsWorld = physicsWorld,
                Detector = detectionEntiy,
                DetectionTagLookup = _detectionTagLookup,
                MonsterTagLookup = _monsterTagLookup,
                TransformTagLookup = _transformLookup,
                HitBufferLookup = _hitBufferLookup,
                OverlapQueryLookup = _overlapOverTimeQueryCenterLookup,

            }.Schedule();


            // 4-1. 并行筛选：遍历每个实体的 buffer，选最近的目标并清空 buffer，不需要被依赖
            state.Dependency = new ApplyNearestJob
            {
                DetectionTagLookup = _detectionTagLookup,
                OverlapQueryLookup = _overlapOverTimeQueryCenterLookup,
                Detector = detectionEntiy,
            }
            .ScheduleParallel(state.Dependency);

            // JOB后：队列转数组，对外暴露
            skillOverTimeOverlapMonsterArray = _skillOverTimeOverlapMonster.ToArray(Allocator.Persistent);
            skillBurstOverlapMonsterArray = _skillBurstOverlapMonster.ToArray(Allocator.Persistent);
            skillTrackingOverlapMonsterArray = _skillTrackingOverlapMonster.ToArray(Allocator.Persistent);
            //if (skillOverlapMonsterArray.Length > 0)
            //    DevDebug.Log("检测到的数量" + skillOverlapMonsterArray.Length);

            // if (skillTrackingOverlapMonsterArray.Length > 0)
            //    DevDebug.Log("检测到的数量" + skillTrackingOverlapMonsterArray.Length);
            

        }

        public void OnDestroy(ref SystemState state)
        {
            _detectionOverlapMonster.Dispose();
            _skillOverTimeOverlapMonster.Dispose();
            _skillBurstOverlapMonster.Dispose();
            _skillTrackingOverlapMonster.Dispose();

            DisposeOverLapArrays();

        }

    }
    /// <summary>
    ///持续性技能检测,球体，长方体
    /// </summary>

    [BurstCompile]
    struct SkillOverTimeOverlapDetectionJob : IJobFor
    {
        [ReadOnly] public PhysicsWorld PhysicsWorld;
        [ReadOnly] public NativeArray<Entity> Entities;
        [ReadOnly] public NativeArray<OverlapOverTimeQueryCenter> Centers;
        [ReadOnly] public ComponentLookup<SkillsOverTimeDamageCalPar> SkillTagLookup;
        [ReadOnly] public ComponentLookup<LiveMonster> MonsterTagLookup;
        [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup; // ✅ 新增世界变换组件
        //通用队列
        public NativeQueue<TriggerPairData>.ParallelWriter SkillOverlapMonsterQueue;
        //后面通过 技能读取类的条件判断，引入新的队列，以筛选新的标识？

        public void Execute(int index)
        {
            var entity = Entities[index];
            var shape = Centers[index].shape;
            var center = Centers[index].center;
            var offset = Centers[index].offset;
            var radius = Centers[index].radius;
            var box = Centers[index].box;
            var filter = Centers[index].filter;
            var rotation = Centers[index].rotaion;

            // ✅ 获取 LocalToWorld 变换（用于将 offset 从本地变为世界空间）
            var ltw = LocalToWorldLookup[entity];
            var worldOffset = math.transform(ltw.Value, offset);
            var actualCenter = center + worldOffset;

            switch (shape)
            {
                case OverLapShape.Sphere:
                    {
                        // ✅ 将旋转 float4 转 quaternion
                        quaternion rotationQuat = new quaternion(rotation);

                        // ✅ 使用 rotation * offset 模拟碰撞体偏移
                        float3 rotatedOffset = math.mul(rotationQuat, offset);

                        // ✅ 实体世界中心 + 旋转后的偏移，得到球心
                        float3 actuallCirclCenter = LocalToWorldLookup[entity].Position + rotatedOffset;
                        var hits = new NativeList<DistanceHit>(Allocator.Temp);
                        var input = new PointDistanceInput
                        {
                            Position = actuallCirclCenter,
                            MaxDistance = radius,
                            Filter = filter
                        };
                        PhysicsWorld.CalculateDistance(input, ref hits);

                        for (int j = 0; j < hits.Length; j++)
                        {
                            //  DevDebug.Log("进入最终筛选");
                            var targetEntity = PhysicsWorld.Bodies[hits[j].RigidBodyIndex].Entity;
                            if (targetEntity == entity) continue;

                            bool isSkill = SkillTagLookup.HasComponent(entity);
                            bool isTargetMonster = MonsterTagLookup.HasComponent(targetEntity);

                            if (isSkill && isTargetMonster)
                            {
                                //  DevDebug.Log("加入队列");
                                SkillOverlapMonsterQueue.Enqueue(new TriggerPairData { EntityA = entity, EntityB = targetEntity });
                            }
                        }

                        hits.Dispose();
                        break;
                    }

                case OverLapShape.Box:
                    {
                        var hitsBox = new NativeList<DistanceHit>(Allocator.Temp);
                        float3 halfExtents = box * 0.5f;

                        quaternion rotationQuat = new quaternion(rotation);

                        float3 rotatedOffset = math.mul(rotationQuat, offset);
                        float3 actualBoxCenter = center + rotatedOffset;


                        PhysicsWorld.OverlapBox(
                            actualBoxCenter,
                            rotationQuat,
                            halfExtents,
                            ref hitsBox,
                            filter
                        );

                        for (int j = 0; j < hitsBox.Length; j++)
                        {
                            var targetEntity = PhysicsWorld.Bodies[hitsBox[j].RigidBodyIndex].Entity;
                            if (targetEntity == entity) continue;

                            if (SkillTagLookup.HasComponent(entity) && MonsterTagLookup.HasComponent(targetEntity))
                            {
                                SkillOverlapMonsterQueue.Enqueue(new TriggerPairData
                                {
                                    EntityA = entity,
                                    EntityB = targetEntity
                                });
                            }
                        }

                        hitsBox.Dispose();
                        break;
                    }

            }
        }

        /// <summary>
        /// job 内的方法， 使用静态方法内联， 符合 burstCompile的编译规则
        /// </summary>
        /// <param name="center"></param>
        /// <param name="halfExtents"></param>
        /// <param name="yRotationInRadians"></param>
        /// <returns></returns>
        private static Aabb GetRotatedBoxApproximateAABB(float3 center, float3 halfExtents, float yRotationInRadians)
        {
            // 计算旋转后的包围盒范围
            float cos = math.abs(math.cos(yRotationInRadians));
            float sin = math.abs(math.sin(yRotationInRadians));

            // 旋转后 XZ 方向的最大投影长度（用来计算 AABB）
            float newHalfX = halfExtents.x * cos + halfExtents.z * sin;
            float newHalfZ = halfExtents.x * sin + halfExtents.z * cos;

            // Y 不变（不考虑垂直旋转）
            float3 newHalfExtents = new float3(newHalfX, halfExtents.y, newHalfZ);

            return new Aabb
            {
                Min = center - newHalfExtents,
                Max = center + newHalfExtents
            };
        }

    }


    [BurstCompile]
    struct SkillBurstOverlapDetectionJob : IJobFor
    {
        [ReadOnly] public PhysicsWorld PhysicsWorld;
        [ReadOnly] public NativeArray<Entity> Entities;
        [ReadOnly] public NativeArray<OverlapBurstQueryCenter> Centers;
        [ReadOnly] public ComponentLookup<SkillsBurstDamageCalPar> SkillTagLookup;
        [ReadOnly] public ComponentLookup<LiveMonster> MonsterTagLookup;
        [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup; // ✅ 新增世界变换组件
        //通用队列
        public NativeQueue<TriggerPairData>.ParallelWriter SkillOverlapMonsterQueue;
        //后面通过 技能读取类的条件判断，引入新的队列，以筛选新的标识？

        public void Execute(int index)
        {
            //  DevDebug.Log("进入收集");
            var entity = Entities[index];
            var shape = Centers[index].shape;
            var center = Centers[index].center;
            var offset = Centers[index].offset;
            var radius = Centers[index].radius;
            var box = Centers[index].box;
            var filter = Centers[index].filter;
            var rotation = Centers[index].rotaion;

            // ✅ 获取 LocalToWorld 变换（用于将 offset 从本地变为世界空间）
            var ltw = LocalToWorldLookup[entity];
            var worldOffset = math.transform(ltw.Value, offset);
            var actualCenter = center + worldOffset;

            switch (shape)
            {
                case OverLapShape.Sphere:
                    {
                        // DevDebug.Log("进入筛选");


                        // ✅ 将旋转 float4 转 quaternion
                        quaternion rotationQuat = new quaternion(rotation);

                        // ✅ 使用 rotation * offset 模拟碰撞体偏移
                        float3 rotatedOffset = math.mul(rotationQuat, offset);

                        // ✅ 实体世界中心 + 旋转后的偏移，得到球心
                        float3 actuallCirclCenter = LocalToWorldLookup[entity].Position + rotatedOffset;
                        var hits = new NativeList<DistanceHit>(Allocator.Temp);
                        var input = new PointDistanceInput
                        {
                            Position = actuallCirclCenter,
                            MaxDistance = radius,
                            Filter = filter
                        };
                        PhysicsWorld.CalculateDistance(input, ref hits);

                        for (int j = 0; j < hits.Length; j++)
                        {
                            //  DevDebug.Log("进入最终筛选");
                            var targetEntity = PhysicsWorld.Bodies[hits[j].RigidBodyIndex].Entity;
                            if (targetEntity == entity) continue;

                            bool isSkill = SkillTagLookup.HasComponent(entity);
                            bool isTargetMonster = MonsterTagLookup.HasComponent(targetEntity);

                            if (isSkill && isTargetMonster)
                            {
                                //  DevDebug.Log("加入队列");
                                SkillOverlapMonsterQueue.Enqueue(new TriggerPairData { EntityA = entity, EntityB = targetEntity });
                            }
                        }

                        hits.Dispose();
                        break;
                    }

                case OverLapShape.Box:
                    {
                        var hitsBox = new NativeList<DistanceHit>(Allocator.Temp);
                        float3 halfExtents = box * 0.5f;

                        quaternion rotationQuat = new quaternion(rotation);

                        float3 rotatedOffset = math.mul(rotationQuat, offset);
                        float3 actualBoxCenter = center + rotatedOffset;


                        PhysicsWorld.OverlapBox(
                            actualBoxCenter,
                            rotationQuat,
                            halfExtents,
                            ref hitsBox,
                            filter
                        );

                        for (int j = 0; j < hitsBox.Length; j++)
                        {
                            var targetEntity = PhysicsWorld.Bodies[hitsBox[j].RigidBodyIndex].Entity;
                            if (targetEntity == entity) continue;

                            if (SkillTagLookup.HasComponent(entity) && MonsterTagLookup.HasComponent(targetEntity))
                            {
                                SkillOverlapMonsterQueue.Enqueue(new TriggerPairData
                                {
                                    EntityA = entity,
                                    EntityB = targetEntity
                                });
                            }
                        }

                        hitsBox.Dispose();
                        break;
                    }

            }
        }

        /// <summary>
        /// job 内的方法， 使用静态方法内联， 符合 burstCompile的编译规则
        /// </summary>
        /// <param name="center"></param>
        /// <param name="halfExtents"></param>
        /// <param name="yRotationInRadians"></param>
        /// <returns></returns>
        private static Aabb GetRotatedBoxApproximateAABB(float3 center, float3 halfExtents, float yRotationInRadians)
        {
            // 计算旋转后的包围盒范围
            float cos = math.abs(math.cos(yRotationInRadians));
            float sin = math.abs(math.sin(yRotationInRadians));

            // 旋转后 XZ 方向的最大投影长度（用来计算 AABB）
            float newHalfX = halfExtents.x * cos + halfExtents.z * sin;
            float newHalfZ = halfExtents.x * sin + halfExtents.z * cos;

            // Y 不变（不考虑垂直旋转）
            float3 newHalfExtents = new float3(newHalfX, halfExtents.y, newHalfZ);

            return new Aabb
            {
                Min = center - newHalfExtents,
                Max = center + newHalfExtents
            };
        }

    }

    /// <summary>
    /// 寻址技能 相关检测处理的专用jobs
    /// </summary>
    [BurstCompile]
    struct SkillTrackingOverlapDetectionJob : IJobFor
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public PhysicsWorld PhysicsWorld;
        [ReadOnly] public NativeArray<Entity> Entities;
        [ReadOnly] public NativeArray<OverlapTrackingQueryCenter> Centers;
        [ReadOnly] public ComponentLookup<SkillsTrackingCalPar> SkillTagLookup;
        [ReadOnly] public ComponentLookup<LiveMonster> MonsterTagLookup;
        [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup; // ✅ 新增世界变换组件
        [ReadOnly] public ComponentLookup<LocalTransform> Transform;//位置变换
        [ReadOnly] public BufferLookup<TrackingRecord> TrackingRecordBufferLookup;//追踪buffer查询
        //通用队列
        public NativeQueue<TriggerPairData>.ParallelWriter SkillOverlapMonsterQueue;
        //后面通过 技能读取类的条件判断，引入新的队列，以筛选新的标识？

        public void Execute(int index)

        {
            // DevDebug.Log("进入收集");
            var entity = Entities[index];
            var shape = Centers[index].shape;
            var center = Centers[index].center;
            var offset = Centers[index].offset;
            var radius = Centers[index].radius;
            var box = Centers[index].box;
            var filter = Centers[index].filter;
            var rotation = Centers[index].rotaion;
            //定义buffer变量
            var buffer = TrackingRecordBufferLookup[entity];
            var skillTrackingCal = SkillTagLookup[entity];


            // ✅ 获取 LocalToWorld 变换（用于将 offset 从本地变为世界空间）
            var ltw = LocalToWorldLookup[entity];
            var worldOffset = math.transform(ltw.Value, offset);
            var actualCenter = center + worldOffset;

            switch (shape)
            {
                case OverLapShape.Sphere:
                    {
                         //DevDebug.Log("进入筛选");


                        // ✅ 将旋转 float4 转 quaternion
                        quaternion rotationQuat = new quaternion(rotation);

                        // ✅ 使用 rotation * offset 模拟碰撞体偏移
                        float3 rotatedOffset = math.mul(rotationQuat, offset);

                        // ✅ 实体世界中心 + 旋转后的偏移，得到球心
                        float3 actuallCirclCenter = LocalToWorldLookup[entity].Position + rotatedOffset;
                        var hits = new NativeList<DistanceHit>(Allocator.Temp);
                        var input = new PointDistanceInput
                        {
                            Position = actuallCirclCenter,
                            MaxDistance = radius,
                            Filter = filter
                        };
                        PhysicsWorld.CalculateDistance(input, ref hits);

                        for (int j = 0; j < hits.Length; j++)
                        {
                            //  DevDebug.Log("进入最终筛选");
                            var targetEntity = PhysicsWorld.Bodies[hits[j].RigidBodyIndex].Entity;
                            if (targetEntity == entity) continue;

                            bool isSkill = SkillTagLookup.HasComponent(entity);
                            bool isTargetMonster = MonsterTagLookup.HasComponent(targetEntity);

                            if (isSkill && isTargetMonster)
                            {
                                  //DevDebug.Log("加入队列");
                                 SkillOverlapMonsterQueue.Enqueue(new TriggerPairData { EntityA = entity, EntityB = targetEntity });
                                //- 在此处调用符合要求的方法， 直接将目标的位置 和 entity 的引用，添加到buffer中

                                float3 targetPos = Transform[targetEntity].Position;
                                //这里并行写入 貌似不能控制长度-待考察,不能控制buffer 的添加，只能控制队列，这里是添加到各自的buffer
                                
                                    if (!skillTrackingCal.destory&&TrackingRecordBufferLookup.HasBuffer(entity))
                                                    
                                    AddToTrackingBuffer(entity, targetEntity, targetPos,index,ECB);
                                

                            }
                        }

                        hits.Dispose();
                        break;
                    }

                case OverLapShape.Box:
                    {
                        var hitsBox = new NativeList<DistanceHit>(Allocator.Temp);
                        float3 halfExtents = box * 0.5f;

                        quaternion rotationQuat = new quaternion(rotation);

                        float3 rotatedOffset = math.mul(rotationQuat, offset);
                        float3 actualBoxCenter = center + rotatedOffset;


                        PhysicsWorld.OverlapBox(
                            actualBoxCenter,
                            rotationQuat,
                            halfExtents,
                            ref hitsBox,
                            filter
                        );

                        for (int j = 0; j < hitsBox.Length; j++)
                        {
                            var targetEntity = PhysicsWorld.Bodies[hitsBox[j].RigidBodyIndex].Entity;
                            if (targetEntity == entity) continue;

                            if (SkillTagLookup.HasComponent(entity) && MonsterTagLookup.HasComponent(targetEntity))
                            {
                                // SkillOverlapMonsterQueue.Enqueue(new TriggerPairData
                                // {
                                //     EntityA = entity,
                                //     EntityB = targetEntity
                                // });
                                float3 targetPos = Transform[targetEntity].Position;

                                if (buffer.Length < buffer.Capacity)

                                {
                                        if (!skillTrackingCal.destory)
                                      AddToTrackingBuffer(entity, targetEntity, targetPos,index,ECB);
                                }

                            }
                        }

                        hitsBox.Dispose();
                        break;
                    }

            }
        }
        private static void AddToTrackingBuffer(
           Entity skillEntity,
           Entity targetEntity,
           float3 targetPosition,
           int softKey,
           EntityCommandBuffer.ParallelWriter ECB )
        {

            ECB.AppendToBuffer(softKey,skillEntity,new TrackingRecord
            {
                refTarget = targetEntity,
                postion = targetPosition
            });

        }
    }

    /// <summary>
    /// 侦测器检测,默认球形
    /// </summary>
    struct DetectionBufferWriteJob : IJob
    {
        [ReadOnly] public PhysicsWorld PhysicsWorld;
        [ReadOnly] public Entity Detector;
        [ReadOnly] public ComponentLookup<Detection_DefaultCmpt> DetectionTagLookup;
        [ReadOnly] public ComponentLookup<LiveMonster> MonsterTagLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformTagLookup;
        [ReadOnly] public ComponentLookup<OverlapOverTimeQueryCenter> OverlapQueryLookup;
        public BufferLookup<NearbyHit> HitBufferLookup;

        public void Execute()
        {
            //if (!DetectionTagLookup.HasComponent(Detector))
            //    return;
            if (!OverlapQueryLookup.HasComponent(Detector))
                return;

            var overlap = OverlapQueryLookup[Detector];
            var center = overlap.center;
            var offset = overlap.offset;
            var radius = overlap.radius;
            var filter = overlap.filter;

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

                // if (!MonsterTagLookup.HasComponent(other)) continue;
                if (!MonsterTagLookup.IsComponentEnabled(other)) continue;
                //  if (!TransformTagLookup.HasComponent(other) || !TransformTagLookup.HasComponent(Detector)) continue;

                float d = math.distancesq(
                    TransformTagLookup[Detector].Position,
                    TransformTagLookup[other].Position);

                var detection = DetectionTagLookup[Detector];
                Entity bufferTarget = detection.bufferOwner;

                if (HitBufferLookup.HasBuffer(bufferTarget))
                {
                    var buf = HitBufferLookup[bufferTarget];
                    if (buf.Length < buf.Capacity)///这里单线程才有用？多线程会竞态
                        buf.Add(new NearbyHit { other = other, sqrDist = d });
                }
            }
            hits.Dispose();
        }
    }
    /// <summary>
    /// 侦测器buffer的处理单独调度处理
    /// </summary>
    [BurstCompile]
    partial struct ApplyNearestJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<Detection_DefaultCmpt> DetectionTagLookup;
        [ReadOnly] public Entity Detector;
        [NativeDisableParallelForRestriction]
        public ComponentLookup<OverlapOverTimeQueryCenter> OverlapQueryLookup;
        public void Execute(ref HeroAttackTarget det, in LocalTransform transform, HeroEntityMasterTag masterTag, DynamicBuffer<NearbyHit> hits)
        {

            var detection_DefaultCmpt = DetectionTagLookup[Detector];
            var overlap = OverlapQueryLookup[Detector];

            overlap.center = transform.Position;
            //if(hits.Length>0)
            //DevDebug.Log("当前侦测的长度：" + hits.Length);
            if (hits.Length > 20)
                overlap.radius = math.max(overlap.radius - 1f, 2f); // 最小2
            else if (hits.Length <= 0)

            {
                overlap.radius = math.min(overlap.radius + 1f, detection_DefaultCmpt.originalRadius); // 最大originalRadius
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
