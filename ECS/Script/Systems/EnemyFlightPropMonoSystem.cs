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
    [UpdateAfter(typeof(HeroSkillsMonoSystem))]
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
    /// �����������ð汾�����б��ͨ��ECB��¼ ͳһ�Ļ�
    /// </summary>
    [BurstCompile]
    partial struct EnemyFlightPropJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        public float Time;

        void Execute(Entity entity,
                     in LocalTransform transform, // ֻ������
                     ref EnemyFlightProp enemyFlightProp,
                     [EntityIndexInQuery] int sortKey)
        {
            // ���ʱ�䴦��
            enemyFlightProp.survivalTime -= Time;

            if (enemyFlightProp.destory == true)
                return;


            if (enemyFlightProp.survivalTime <= 0f)
            {
                enemyFlightProp.destory = true;
                return;
            }

            // ������λ�ú���ת
            float3 newPos = transform.Position + enemyFlightProp.speed * Time * enemyFlightProp.dir;
            newPos.y = 1; // ǿ�� Y �߶�Ϊ 1

            quaternion newRot = quaternion.LookRotationSafe(enemyFlightProp.dir, math.up());

            // д�� LocalTransform
            ECB.SetComponent(sortKey, entity, new LocalTransform
            {
                Position = newPos,
                Rotation = newRot,
                Scale = transform.Scale // ����ԭʼ����
            });
        }
    }

}
