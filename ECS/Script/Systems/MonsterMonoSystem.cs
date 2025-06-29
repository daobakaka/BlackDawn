using GPUECSAnimationBaker.Engine.AnimatorSystem;
using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine.Scripting;
/// <summary>
/// 分管怪物死亡脚本，后续添加死亡特效、动画、或事件逻辑，死亡特效与 renderEffects 配合
/// </summary>
namespace BlackDawn.DOTS
{
    //在渲染之后
    [BurstCompile]
    [UpdateAfter(typeof(HeroSkillsCallbackSystemBase))]
    [UpdateInGroup(typeof(MainThreadSystemGroup))]
    public partial struct  MonsterMonoSystem : ISystem
{

        float _timer;
        void OnCreate(ref SystemState state) 
        {

            //外部控制
            state.RequireForUpdate<EnableEnemyPropMonoSystemTag>();



        }
        [BurstCompile]
        void OnUpdate(ref SystemState state)

        {
          //等待其他地方job完成，最后执行清理
           // state.Dependency.Complete();

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            //ecb 都是临时定义
          //  var ecb = new EntityCommandBuffer(Allocator.Temp);
            _timer = SystemAPI.Time.DeltaTime;


            //销毁敌人飞行道具、技能等
            foreach (var (enemyFlightProp,trans,entity) in SystemAPI.Query<RefRW<EnemyFlightProp>, RefRW<LocalTransform>>().WithEntityAccess())
            {
                if (enemyFlightProp.ValueRO.destory == true)
                {
                    ecb.DestroyEntity(entity);
                
                }
            }







            //entiy 销毁回到主线程,查找活怪标签，减少job判断，模拟状态机,貌似1.4 只能执行一次？1
            foreach (var(attrRW,collider,agent,agentShape,liveMonster,animatorAspect, entity) in SystemAPI.Query<RefRW<MonsterDefenseAttribute>,RefRW<PhysicsCollider>,
                RefRW<AgentBody>, RefRW<AgentShape>,
                RefRW<LiveMonster>,GpuEcsAnimatorAspect> ().WithEntityAccess())
            {
         
                if (attrRW.ValueRW.hp <= 0.00f)
                //固定死亡动画
                {
                    if (!attrRW.ValueRW.death)
                    {
                        // DevDebug.Log("播放死亡动画");
                        //这里存在多线程机制？同一帧单次写有可能不被命中！！（非常重要谨记）
                        attrRW.ValueRW.death = true;
                        //失活liveMonster组件，避免job计算
                        ecb.SetComponentEnabled<LiveMonster>(entity, false);
                        //只播一次，默认动画4死亡，后续有精英怪加入也默认4
                        animatorAspect.RunAnimation(4, 0, 1);

                        // 2) 移除碰撞组件，物理引擎就不会再检测它
                        ecb.RemoveComponent<PhysicsCollider>(entity);

                        //移除导航基础组件
                        ecb.RemoveComponent<AgentBody>(entity);
                        //移除导航路障
                        ecb.RemoveComponent<AgentShape>(entity);
                    }
                   
                }
  
            }

            //死亡销毁逻辑，只能分开？,而且上面的 death 标签经常无法有效设置？ 有线程竞争，有一定概率拿到老值，所以这里需要  state.EntityManager.CompleteAllTrackedJobs();？
            foreach (var (attrRW, entity) in SystemAPI.Query<RefRW<MonsterDefenseAttribute>>().WithDisabled<LiveMonster>().WithEntityAccess())
            {
               
                
                    attrRW.ValueRW.survivalTime -= _timer;
                    //存活时间完毕之后销毁
                    if (attrRW.ValueRO.survivalTime <= 0)
                        ecb.DestroyEntity(entity);

                                   
            
            }

            //动画事件修正？动画事件未触发？目前没有找到原因






            //ecb.Playback(state.EntityManager);
            //ecb.Dispose();

        }
        void OnDestroy(ref SystemState state) { }
    }
}