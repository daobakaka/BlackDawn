using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BlackDawn.DOTS
{
    /// <summary>
    /// 飞行道具System
    /// </summary>
    [BurstCompile]
    [UpdateAfter(typeof(HeroSkillsMonoSystem))]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(ActionSystemGroup))]
    partial struct EnemyFlightPropMonoSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            //外部控制
            state.RequireForUpdate<EnableEnemyPropMonoSystemTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {


            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);


            state.Dependency = new EnemyFlightPropJob
            {
                Time = SystemAPI.Time.DeltaTime,
                ECB = ecb.AsParallelWriter(),

            }.ScheduleParallel(state.Dependency);


        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {


        }
    }
    /// <summary>
    /// 依赖管理良好版本，所有变更通过ECB记录 统一改回
    /// </summary>
    [BurstCompile]
    partial struct EnemyFlightPropJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        public float Time;

        void Execute(Entity entity,
                     in LocalTransform transform, // 只读访问
                     ref EnemyFlightProp enemyFlightProp,
                     [EntityIndexInQuery] int sortKey)
        {
            // 存活时间处理
            enemyFlightProp.survivalTime -= Time;

            if (enemyFlightProp.destory == true)
                return;


            if (enemyFlightProp.survivalTime <= 0f)
            {
                enemyFlightProp.destory = true;
                return;
            }

            // 计算新位置和旋转
            float3 newPos = transform.Position + enemyFlightProp.speed * Time * enemyFlightProp.dir;
            newPos.y = 1; // 强制 Y 高度为 1

            quaternion newRot = quaternion.LookRotationSafe(enemyFlightProp.dir, math.up());

            // 写回 LocalTransform
            ECB.SetComponent(sortKey, entity, new LocalTransform
            {
                Position = newPos,
                Rotation = newRot,
                Scale = transform.Scale // 保持原始缩放
            });
        }
    }

}
