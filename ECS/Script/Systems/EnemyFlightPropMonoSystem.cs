using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BlackDawn.DOTS
{
    /// <summary>
    /// ���е���System
    /// </summary>
    [BurstCompile]
    [UpdateAfter(typeof(EnemyFlightPropDamageSystem))]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(ActionSystemGroup))]
    partial struct EnemyFlightPropMonoSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            //�ⲿ����
            state.RequireForUpdate<EnableEnemyPropMonoSystemTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ECBParallel = ecb.AsParallelWriter();

            state.Dependency = new EnemyFlightPropJob
            {
                Time = SystemAPI.Time.DeltaTime,
                ECB = ECBParallel,

            }.ScheduleParallel(state.Dependency);

            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {


        }
    }

    [BurstCompile]
    partial struct EnemyFlightPropJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        public float Time;

        void Execute(Entity entity,
                     ref LocalTransform transform,
                     ref EnemyFlightProp enemyFlightProp,
             
                    [EntityIndexInQuery] int sortKey)
        {
            // ��ͨ����ʱ��������ɾ�����
            enemyFlightProp.survivalTime -= Time;
            if (enemyFlightProp.survivalTime <= 0f|| enemyFlightProp.destory == true)
            {
                ECB.DestroyEntity(sortKey, entity);
                return;
            }

          
            // �����߼�����
            transform.Position += enemyFlightProp.speed * Time * enemyFlightProp.dir;
            transform.Position.y = 1;
            transform.Rotation = quaternion.LookRotationSafe(enemyFlightProp.dir, math.up());
        }
    }
}
