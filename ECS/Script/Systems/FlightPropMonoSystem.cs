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
    [RequireMatchingQueriesForUpdate]
    [UpdateAfter(typeof(FlightPropDamageSystem))]
    [UpdateInGroup(typeof(ActionSystemGroup))]
    partial struct FlightPropMonoSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            //外部控制
            state.RequireForUpdate<EnablePropMonoSystemTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {


            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);


            state.Dependency = new DirectFlightPropJob
            {
                time = SystemAPI.Time.DeltaTime,
                ECB = ecb.AsParallelWriter(),

            }.ScheduleParallel(state.Dependency);


        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {


        }
    }

    [BurstCompile]
    partial struct DirectFlightPropJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        public float time;

        void Execute(Entity entity,
                     ref LocalTransform transform,
                     ref DirectFlightPropCmpt directFlight,
                     ref FlightPropDamageCalPar damagePar,          // ← ref 拿到原组件
                    [EntityIndexInQuery] int sortKey)
        {
            // 普通存活倒计时
            directFlight.originalSurvivalTime -= time;
            if (directFlight.originalSurvivalTime <= 0f)
            {
                ECB.DestroyEntity(sortKey, entity);
                return;
            }

            // 命中直接销毁
            if (damagePar.destory)
            {
                
                  //  ECB.RemoveComponent<FlightPropDamageCalPar>(sortKey, entity);
                    ECB.DestroyEntity(sortKey, entity);
                    return;
                
            }

            // 飞行逻辑……
            transform.Position += directFlight.speed * time * directFlight.dir;
            transform.Position.y = 1;
            transform.Rotation = quaternion.LookRotationSafe(directFlight.dir, math.up());
        }
    }
}
