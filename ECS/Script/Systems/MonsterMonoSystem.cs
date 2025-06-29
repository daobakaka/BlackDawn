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
/// �ֹܹ��������ű����������������Ч�����������¼��߼���������Ч�� renderEffects ���
/// </summary>
namespace BlackDawn.DOTS
{
    //����Ⱦ֮��
    [BurstCompile]
    [UpdateAfter(typeof(HeroSkillsCallbackSystemBase))]
    [UpdateInGroup(typeof(MainThreadSystemGroup))]
    public partial struct  MonsterMonoSystem : ISystem
{

        float _timer;
        void OnCreate(ref SystemState state) 
        {

            //�ⲿ����
            state.RequireForUpdate<EnableEnemyPropMonoSystemTag>();



        }
        [BurstCompile]
        void OnUpdate(ref SystemState state)

        {
          //�ȴ������ط�job��ɣ����ִ������
           // state.Dependency.Complete();

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            //ecb ������ʱ����
          //  var ecb = new EntityCommandBuffer(Allocator.Temp);
            _timer = SystemAPI.Time.DeltaTime;


            //���ٵ��˷��е��ߡ����ܵ�
            foreach (var (enemyFlightProp,trans,entity) in SystemAPI.Query<RefRW<EnemyFlightProp>, RefRW<LocalTransform>>().WithEntityAccess())
            {
                if (enemyFlightProp.ValueRO.destory == true)
                {
                    ecb.DestroyEntity(entity);
                
                }
            }







            //entiy ���ٻص����߳�,���һ�ֱ�ǩ������job�жϣ�ģ��״̬��,ò��1.4 ֻ��ִ��һ�Σ�1
            foreach (var(attrRW,collider,agent,agentShape,liveMonster,animatorAspect, entity) in SystemAPI.Query<RefRW<MonsterDefenseAttribute>,RefRW<PhysicsCollider>,
                RefRW<AgentBody>, RefRW<AgentShape>,
                RefRW<LiveMonster>,GpuEcsAnimatorAspect> ().WithEntityAccess())
            {
         
                if (attrRW.ValueRW.hp <= 0.00f)
                //�̶���������
                {
                    if (!attrRW.ValueRW.death)
                    {
                        // DevDebug.Log("������������");
                        //������ڶ��̻߳��ƣ�ͬһ֡����д�п��ܲ������У������ǳ���Ҫ���ǣ�
                        attrRW.ValueRW.death = true;
                        //ʧ��liveMonster���������job����
                        ecb.SetComponentEnabled<LiveMonster>(entity, false);
                        //ֻ��һ�Σ�Ĭ�϶���4�����������о�Ӣ�ּ���ҲĬ��4
                        animatorAspect.RunAnimation(4, 0, 1);

                        // 2) �Ƴ���ײ�������������Ͳ����ټ����
                        ecb.RemoveComponent<PhysicsCollider>(entity);

                        //�Ƴ������������
                        ecb.RemoveComponent<AgentBody>(entity);
                        //�Ƴ�����·��
                        ecb.RemoveComponent<AgentShape>(entity);
                    }
                   
                }
  
            }

            //���������߼���ֻ�ֿܷ���,��������� death ��ǩ�����޷���Ч���ã� ���߳̾�������һ�������õ���ֵ������������Ҫ  state.EntityManager.CompleteAllTrackedJobs();��
            foreach (var (attrRW, entity) in SystemAPI.Query<RefRW<MonsterDefenseAttribute>>().WithDisabled<LiveMonster>().WithEntityAccess())
            {
               
                
                    attrRW.ValueRW.survivalTime -= _timer;
                    //���ʱ�����֮������
                    if (attrRW.ValueRO.survivalTime <= 0)
                        ecb.DestroyEntity(entity);

                                   
            
            }

            //�����¼������������¼�δ������Ŀǰû���ҵ�ԭ��






            //ecb.Playback(state.EntityManager);
            //ecb.Dispose();

        }
        void OnDestroy(ref SystemState state) { }
    }
}