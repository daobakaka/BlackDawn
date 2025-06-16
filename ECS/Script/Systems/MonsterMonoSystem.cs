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
    [UpdateAfter(typeof(AttackRecordBufferSystem))]
    [UpdateInGroup(typeof(ActionSystemGroup))]
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
            //ecb ������ʱ����
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            _timer = SystemAPI.Time.DeltaTime;

            //entiy ���ٻص����߳�,���һ�ֱ�ǩ������job�жϣ�ģ��״̬��,ò��1.4 ֻ��ִ��һ�Σ�1
            foreach (var (attrRW,collider,agent,agentShape,liveMonster,animatorAspect, entity) in SystemAPI.Query<RefRW<MonsterDefenseAttribute>,RefRW<PhysicsCollider>,
                RefRW<AgentBody>, RefRW<AgentShape>,
                RefRW<LiveMonster>,GpuEcsAnimatorAspect> ().WithEntityAccess())
            {
                if (attrRW.ValueRW.hp <= 0f)
                //�̶���������
                {
                    if (!attrRW.ValueRW.death)
                    {
                        //ʧ��liveMonster���������job����
                        ecb.SetComponentEnabled<LiveMonster>(entity, false);

                        // 2) �Ƴ���ײ�������������Ͳ����ټ����
                        ecb.RemoveComponent<PhysicsCollider>(entity);

                        //�Ƴ������������
                        ecb.RemoveComponent<AgentBody>(entity);
                        //�Ƴ�����·��
                        ecb.RemoveComponent<AgentShape>(entity);

                        //ֻ��һ�Σ�Ĭ�϶���4�����������о�Ӣ�ּ���ҲĬ��4
                        animatorAspect.RunAnimation(4, 0, 1);
                       // DevDebug.Log("������������");
                        attrRW.ValueRW.death = true;   
                    }
                   
                }
  
            }

            //���������߼���ֻ�ֿܷ���
            foreach (var (attrRW, entity) in SystemAPI.Query<RefRW<MonsterDefenseAttribute>>().WithEntityAccess())
            {

                if (attrRW.ValueRW.death)
                {
                
                    attrRW.ValueRW.survivalTime -= _timer;
                    //���ʱ�����֮������
                    if (attrRW.ValueRO.survivalTime <= 0)
                        ecb.DestroyEntity(entity);

                }        
            
            
            }

            //�����¼������������¼�δ������Ŀǰû���ҵ�ԭ��






            ecb.Playback(state.EntityManager);
            ecb.Dispose();

        }
        void OnDestroy(ref SystemState state) { }
    }
}