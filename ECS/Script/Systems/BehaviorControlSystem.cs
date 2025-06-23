using BlackDawn.DOTS;
using GPUECSAnimationBaker.Engine.AnimatorSystem;
using ProjectDawn.Navigation;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


//����DOTS�����ܿ���ʱ����Ϊ,����Ⱦϵͳ֮ǰ����
namespace BlackDawn
{

    [RequireMatchingQueriesForUpdate]
    [UpdateAfter(typeof(EnemyFlightPropMonoSystem))]
    [UpdateInGroup(typeof(ActionSystemGroup))]
    [BurstCompile]
    public partial struct BehaviorControlSystem : ISystem
    {
        void OnCreate(ref SystemState state) 
        
        {
           
            //���ⲿ���ƿ���
            state.RequireForUpdate<EnableBehaviorControlSystemTag>();
        
        
        }
        [BurstCompile]
        void OnUpdate(ref SystemState state) 
        {
            var time = SystemAPI.Time.DeltaTime;

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new BehaviorControlledJob()
            {
                Time = time,


            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
        
        
        
        
        }


        void OnDestroy(ref SystemState state) { }
    }




    [BurstCompile]
    partial struct BehaviorControlledJob : IJobEntity
    {

        public float Time;
        public void Execute(Entity entity, EnabledRefRO<LiveMonster> live,ref MonsterDefenseAttribute defenseAttribute,ref MonsterControlledEffectAttribute controlledEffectAttribute, ref MonsterLossPoolAttribute lossPoolAttribute,
         ref AgentBody agentBody,ref AgentLocomotion agentLocomotion,
         ref AnimationControllerData animation, ref DynamicBuffer<GpuEcsAnimatorEventBufferElement> eventBuffer,
         ref LocalTransform transform, GpuEcsAnimatorAspect animatorAspect, [ChunkIndexInQuery] int index)
        
        
        {
            //���ﲻ��Ҫд�أ���ֱ�����˺�ϵͳд��
            var rnd = new Unity.Mathematics.Random(defenseAttribute.rngState);


            
            //ʱ���ǩĬ�����ӣ�������ײʱ����, Ĭ����ֵ����2�룬2���ֱ�����㣬�������ۼ�
            ref var ce= ref controlledEffectAttribute;
            //��ȡԭʼ�ٶ�
            var daSpeed = defenseAttribute.moveSpeed;
            //������ˢ�¿���Ч��timer��ǩ���ⲿ����
            ce.slowTimer += Time;     
            ce.knockbackTimer += Time;
           //--�ص�
            



            #region �ɶ������� ���� ���ˣ�λ��Ƕ������֮�󣬿����ٶ�����һ֡�ָ�

            //���� - �Ѳ���ͨ�������50%
            //���� - �����Դ���,����֮�󣬳ع���
            //���ٶ�������
            //������ٱ�ǩ
            //---��ǩ����ù��������Ƭ����ƣ� ÿ��һ������Ч�����������ͷ���Ϸ��������ƣ�û����͸��-------�������
            if (ce.slow > 0 && ce.slowTimer <= 1)
            {
                agentLocomotion.Speed = daSpeed * math.max(0.5f, (100f - ce.slow) / 100);
                ce.slowActive = true;
            }
            else
            {
                ce.slowActive = false;//����״̬�ָ�
                ce.slow = 0;
                agentLocomotion.Speed = daSpeed;//������Իָ����е��ƶ��ٶ�

            }

            //����--������Ϊ ��ԭǰ������ ���������,������Խ��е�֡�жϣ������䵽 �����ָ�ϵͳ���лָ�
            //���� �ǳ����Դ��� �����Թ��㷽ʽ��һ��
            //Ĭ�ϻ��˸��� 0.2���Ч��,���˶����0.1f����������ͳһ
            //������˱�ǩ
            if (ce.knockback > 0 && ce.knockbackTimer <= 0.2f)
            {

                ce.knockbackActive = true;
                // 1) ���㵱ǰǰ���������� XZ ƽ�棩
                float3 forward = math.normalize(new float3(
                    0f, 0f, 1f));
                // ��� transform.Rotation �� Y �ᳯ�����ã���
                forward = math.mul(transform.Rotation, new float3(0f, 0f, 1f));
                forward.y = 0f;
                forward = math.normalize(forward);

                // 2) ������ĳ��λ��
                float3 knockbackOffset = -forward * ce.knockback * Time * 0.1f;
                // 3) Ӧ�õ�λ����
                transform.Position += knockbackOffset;

            }
            else
            {
                ce.knockbackActive = false;
                ce.knockback = 0;
            }



            #endregion

            #region ��Ƕ������ ���� �־� ���� ����  �ǳ����Ը��� ��Ҫ״̬ȷ��

            // ���� �־� ���� ���� ���ֿ���Ч����������ʽ������  ����ɸ��ǻ��ԣ����Կɸ��ǿ־壬�����
            //�־� -������Ϊ �����Ӵ���1������2�����ܶ������� ����ʵ�֣� ����Ҫ���ǿ���Ч�����ӵ�����
            //������Լ���־���Ʊ�ǩ
            if (ce.fear > 100)
            {

                float fearDuration = 2f + (ce.fear - 100) / 100f;
                if (ce.fearTimer <= fearDuration)
                {
                    ce.fearActive = true;
                    ce.fearTimer += Time;//�����־�ʱ���ǩ
                                         //�����ٶȲ���
                    animatorAspect.RunAnimation(0, 0, 2);
                    //�����Ӵ�����Ϊ�趨һ����Ŀ�꣬��Ϊ��Ϊϵͳ���ڶ���ϵͳ����֮�� �����������ֱ�Ӳ��ø��Ƿ���
                    // 2) �� [-10, +10] ��Χ��ȡ�������ֵ
                    float dx = rnd.NextFloat(-5f, +5f);
                    float dz = rnd.NextFloat(-5f, +5f);
                    agentBody.SetDestination(transform.Position);
                    // 3) ��Ŀ�� = ��ǰ pos + ƫ��
                    float3 newDest = transform.Position + new float3(dx, 0f, dz);
                    agentBody.SetDestination(newDest);
                }
                else
                    ce.fear = 0;//����־��ǩ
            }
            else
            {
                ce.fearActive = false;
                ce.fearTimer = 0;//��տ־�ʱ���ǩ
            }

            //���� �ٶ�Ϊ0,����������ת���Ŷ���,����ʵ����ת��Ӧ���Ӷ���clip �Լ������¼� idel��
            //���ﶨ����Ը��ǿ־��״̬
            //������Լ��붨����Ʊ�ǩ
            if (ce.root > 100 )
            {
                float rootDuration = 2f + (ce.root - 100) / 100f;
                if (ce.rootTimer <= rootDuration)
                {
                    ce.rootActive = true;//ȷ�϶���״̬
                    ce.rootTimer += Time;
                    //���������Ƴ�������������
                    agentLocomotion.Speed = 0;
                    //��ʱ�����Ŷ�������Ч��
                    // animatorAspect.RunAnimation(1, 0, 1);
                }
                else
                    ce.root = 0;        
            }
            else
            {
                ce.rootActive = false;//����״̬ȡ��
                ce.rootTimer = 0;//�ָ�����ʱ��

            }


            //����  - ���Ż��Զ��� - �ٶȽ���Ϊ0�����蹥������2Ϊ���Զ���,ȷ�ϻ���״̬��������ӻ���״̬�� �ö����¼����Դ���
            if (ce.stun > 100)
            {

                float stunDuration = 2f + (ce.stun - 100) / 100f;
                if (ce.stunTimer <= stunDuration)
                {
                    ce.stunActive = true;//����״̬ȷ�ϣ������˺�����
                    ce.stunTimer += Time;//����ֵ����100 �������Կ��ƣ�����2��ʱ�˳�ѭ�������»���ֵ

                    agentLocomotion.Speed = 0;
                    //���Ż��Զ����� ֹͣ��ת������ʹ��IDLE ��������
                    animatorAspect.RunAnimation(3, 0, 1);
                }
                else
                {
                    ce.stun = 0;//��ջ���״ֵ̬
                    animation.isAttack = false;//���ػ���״̬֮��
                    
                }
            }
            else
            {
                ce.stunActive = false;//����״̬ȡ��
                ce.stunTimer = 0;//�������ʱ���ǩ

            }

            //����  - ֹͣ��ǰ����  - �ٶ�Ϊ0 ���Ա�˪�ؿ��ƣ���������+��˪���� �����Կ��У���������Ʊ�˪�˺��ж���Ŀ���Ч������˱�˪�˺�Ӧ����Ƹ���
            //����������ԺͶ���ͬʱ������������Ը��ǻ��ԣ�ע��ű��Ⱥ�˳��
            if ( ce.freeze > 100 )
            {
                //����������Ӷ���ֵ �����켼�ܵĶ���Ч��
                float freezeDuration = 2f + (ce.freeze - 100) / 100f;

                if (ce.freezeTimer <= freezeDuration)
                {
                    ce.freezeActive = true;
                    ce.freezeTimer += Time;//�ﵽ������������timerֵ
                                           //ֹͣ�ٶ�
                    agentLocomotion.Speed = 0;
                    //ֹͣ����
                    animatorAspect.StopAnimation();
                   // DevDebug.LogError(index + "  ������");
                }
                else
                {
                    ce.freeze = 0;//��ն���ֵ
                    animation.isAttack = false;//���ض���״̬֮�����ⲿ��action�����п���

                }
            }
            else
            {
                ce.freezeActive = false;//�Ƕ���
                ce.freezeTimer = 0;//����ʱ���ǩ
     
            }

            #endregion

            #region ����ǿ���������� �����ͱ�ը�����п�������,����ȷ�Ͽɵ������߱�����״,��������ڼ�������ײ��ֱ�Ӵﵽǣ�����߱�ը��ֵ��������ը

            ///��������ֿ���Ч����ǣ���ͱ�ը Ӧ������ײ���ʵ�֣���������ײ��ʱ��ʱ�����ģ� ������Ҫ����ǣ�����ĺͱ�ը���ĵ�λ��
            ///ǣ���ͱ�ըЧ���� ʵ�ֻ�����300������ʱ����1�룬���ڼ�ǿ��Ŀ��ƣ��˺���ֵ���ܸ���ӳ�

            ////ǣ��״̬,
         
            if (ce.pull > 100)
            {
                if (ce.pullTimer <= 1)
                {
                    ce.pullActive = true;//ȷ��ǣ��״̬
                    ce.pullTimer += Time;

                    float3 dir = ce.pullCenter - transform.Position;
                    dir.y = 0f;
                    dir = math.normalize(dir);
                    // 2) ���� ce.pull ǿ�Ⱥ�ʱ���ƽ�
                    float3 pullOffset = dir * ce.pull * Time * 0.05f;
                    // 3) Ӧ�õ�λ��
                    transform.Position += pullOffset;

                }
                else
                    ce.pull = 0;
            }
            else
            {
                ce.pullActive = false;//ǣ��״̬ȡ��
                ce.pullTimer = 0;//�ָ�ǣ��ʱ��

            }


            //��ը״̬
            if (ce.explosion >100)
            {
                if (ce.explosionTimer <= 0.3f)
                {
                    ce.explosionActive = true;//ȷ�ϱ�ը״̬
                    ce.explosionTimer += Time;

                    // 1) ����ӱ�ը����ָ�����λ�õ�������XZ ƽ�棩
                    float3 dir = transform.Position - ce.explosionCenter;
                    dir.y = 0f;
                    dir = math.normalize(dir);

                    // 2) ���� ce.explosion ǿ�Ⱥ�ʱ���ƽ�
                    float3 explosionOffset = dir * ce.explosion * Time * 0.2f;
                    // 3) Ӧ�õ�λ��
                    transform.Position += explosionOffset;

                }
                else
                    ce.explosion = 0;
            }
            else
            {
                ce.explosionActive = false;//��ը״̬ȡ��
              //ce.explosionTimer = 0;//���ﲻ�ָ���ըʱ���ڱ�ը ֻ����һ��
            }

            #endregion


        }





    }
}