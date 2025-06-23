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


//控制DOTS对象受控制时的行为,在渲染系统之前进行
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
           
            //由外部控制开启
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
            //这里不需要写回，可直接由伤害系统写回
            var rnd = new Unity.Mathematics.Random(defenseAttribute.rngState);


            
            //时间标签默认增加，重新碰撞时清零, 默认阈值保留2秒，2秒后直接清零，需重新累计
            ref var ce= ref controlledEffectAttribute;
            //读取原始速度
            var daSpeed = defenseAttribute.moveSpeed;
            //持续性刷新控制效果timer标签由外部更新
            ce.slowTimer += Time;     
            ce.knockbackTimer += Time;
           //--重点
            



            #region 可独立区域 减速 击退，位于嵌套区域之后，控制速度在下一帧恢复

            //减速 - 已测试通过，最高50%
            //减速 - 持续性触发,减速之后，池归零
            //减速独立触发
            //加入减速标签
            //---标签拟采用广告牌条形片来设计， 每有一个控制效果，激活怪物头像上方的条形牌，没有则透明-------后期设计
            if (ce.slow > 0 && ce.slowTimer <= 1)
            {
                agentLocomotion.Speed = daSpeed * math.max(0.5f, (100f - ce.slow) / 100);
                ce.slowActive = true;
            }
            else
            {
                ce.slowActive = false;//减速状态恢复
                ce.slow = 0;
                agentLocomotion.Speed = daSpeed;//这里可以恢复所有的移动速度

            }

            //击退--表现行为 往原前进方向 反方向后退,这里可以进行单帧判断，可以落到 攻击恢复系统进行恢复
            //击退 是持续性触发 ，所以归零方式不一样
            //默认击退附带 0.2秒的效果,击退多乘以0.1f与其他参数统一
            //加入击退标签
            if (ce.knockback > 0 && ce.knockbackTimer <= 0.2f)
            {

                ce.knockbackActive = true;
                // 1) 计算当前前向向量（在 XZ 平面）
                float3 forward = math.normalize(new float3(
                    0f, 0f, 1f));
                // 如果 transform.Rotation 里 Y 轴朝向有用，则：
                forward = math.mul(transform.Rotation, new float3(0f, 0f, 1f));
                forward.y = 0f;
                forward = math.normalize(forward);

                // 2) 反方向的冲击位移
                float3 knockbackOffset = -forward * ce.knockback * Time * 0.1f;
                // 3) 应用到位置上
                transform.Position += knockbackOffset;

            }
            else
            {
                ce.knockbackActive = false;
                ce.knockback = 0;
            }



            #endregion

            #region 可嵌套区域 定身 恐惧 昏迷 冻结  非持续性更新 需要状态确认

            // 定身 恐惧 昏迷 冻结 三种控制效果，表现形式不叠加  冻结可覆盖昏迷，昏迷可覆盖恐惧，待设计
            //恐惧 -表现行为 慌乱逃串，1、播放2倍速跑动动画， 基本实现， 后面要考虑控制效果叠加的问题
            //这里可以加入恐惧控制标签
            if (ce.fear > 100)
            {

                float fearDuration = 2f + (ce.fear - 100) / 100f;
                if (ce.fearTimer <= fearDuration)
                {
                    ce.fearActive = true;
                    ce.fearTimer += Time;//增长恐惧时间标签
                                         //二倍速度播放
                    animatorAspect.RunAnimation(0, 0, 2);
                    //慌乱逃串定义为设定一样的目标，因为行为系统是在动作系统更新之后， 所以这里可以直接采用覆盖方法
                    // 2) 在 [-10, +10] 范围内取两个随机值
                    float dx = rnd.NextFloat(-5f, +5f);
                    float dz = rnd.NextFloat(-5f, +5f);
                    agentBody.SetDestination(transform.Position);
                    // 3) 新目标 = 当前 pos + 偏移
                    float3 newDest = transform.Position + new float3(dx, 0f, dz);
                    agentBody.SetDestination(newDest);
                }
                else
                    ce.fear = 0;//归零恐惧标签
            }
            else
            {
                ce.fearActive = false;
                ce.fearTimer = 0;//清空恐惧时间标签
            }

            //定身 速度为0,保留攻击旋转播放动画,可以实现旋转，应增加动画clip 以及动画事件 idel？
            //这里定身可以覆盖恐惧的状态
            //这里可以加入定身控制标签
            if (ce.root > 100 )
            {
                float rootDuration = 2f + (ce.root - 100) / 100f;
                if (ce.rootTimer <= rootDuration)
                {
                    ce.rootActive = true;//确认定身状态
                    ce.rootTimer += Time;
                    //如果定身控制池满足则定身两秒
                    agentLocomotion.Speed = 0;
                    //暂时不播放动画看看效果
                    // animatorAspect.RunAnimation(1, 0, 1);
                }
                else
                    ce.root = 0;        
            }
            else
            {
                ce.rootActive = false;//定身状态取消
                ce.rootTimer = 0;//恢复定身时间

            }


            //昏迷  - 播放昏迷动画 - 速度降低为0，假设攻击动画2为昏迷动画,确认昏迷状态，不用添加昏迷状态， 用动画事件可以处理？
            if (ce.stun > 100)
            {

                float stunDuration = 2f + (ce.stun - 100) / 100f;
                if (ce.stunTimer <= stunDuration)
                {
                    ce.stunActive = true;//昏迷状态确认，便于伤害计算
                    ce.stunTimer += Time;//昏迷值大于100 产生昏迷控制，大于2秒时退出循环，更新昏迷值

                    agentLocomotion.Speed = 0;
                    //播放昏迷动画， 停止旋转，这里使用IDLE 动画代替
                    animatorAspect.RunAnimation(3, 0, 1);
                }
                else
                {
                    ce.stun = 0;//清空昏迷状态值
                    animation.isAttack = false;//返回昏迷状态之后
                    
                }
            }
            else
            {
                ce.stunActive = false;//昏迷状态取消
                ce.stunTimer = 0;//归零昏迷时间标签

            }

            //冻结  - 停止当前动画  - 速度为0 ，以冰霜池控制？特殊属性+冰霜控制 ，测试可行，所以因设计冰霜伤害有额外的控制效果，因此冰霜伤害应该设计更低
            //这里如果昏迷和冻结同时触发，冻结可以覆盖昏迷，注意脚本先后顺序
            if ( ce.freeze > 100 )
            {
                //这里可以增加冻结值 来制造技能的冻结效果
                float freezeDuration = 2f + (ce.freeze - 100) / 100f;

                if (ce.freezeTimer <= freezeDuration)
                {
                    ce.freezeActive = true;
                    ce.freezeTimer += Time;//达到触发条件更新timer值
                                           //停止速度
                    agentLocomotion.Speed = 0;
                    //停止动画
                    animatorAspect.StopAnimation();
                   // DevDebug.LogError(index + "  被冻结");
                }
                else
                {
                    ce.freeze = 0;//清空冻伤值
                    animation.isAttack = false;//返回冻结状态之后，由外部的action来进行控制

                }
            }
            else
            {
                ce.freezeActive = false;//非冻结
                ce.freezeTimer = 0;//归零时间标签
     
            }

            #endregion

            #region 额外强力控制区域 引力和爆炸，需有控制中心,最终确认可叠加上诉表现性状,更多的是在技能上碰撞，直接达到牵引或者爆炸阈值，开启爆炸

            ///额外的两种控制效果，牵引和爆炸 应该由碰撞检测实现，这是在碰撞的时候即时发生的， 或者需要储存牵引中心和爆炸中心的位置
            ///牵引和爆炸效果的 实现基数是300，持续时间是1秒，属于加强版的控制，伤害数值上受更多加成

            ////牵引状态,
         
            if (ce.pull > 100)
            {
                if (ce.pullTimer <= 1)
                {
                    ce.pullActive = true;//确认牵引状态
                    ce.pullTimer += Time;

                    float3 dir = ce.pullCenter - transform.Position;
                    dir.y = 0f;
                    dir = math.normalize(dir);
                    // 2) 根据 ce.pull 强度和时间推进
                    float3 pullOffset = dir * ce.pull * Time * 0.05f;
                    // 3) 应用到位置
                    transform.Position += pullOffset;

                }
                else
                    ce.pull = 0;
            }
            else
            {
                ce.pullActive = false;//牵引状态取消
                ce.pullTimer = 0;//恢复牵引时间

            }


            //爆炸状态
            if (ce.explosion >100)
            {
                if (ce.explosionTimer <= 0.3f)
                {
                    ce.explosionActive = true;//确认爆炸状态
                    ce.explosionTimer += Time;

                    // 1) 计算从爆炸中心指向怪物位置的向量（XZ 平面）
                    float3 dir = transform.Position - ce.explosionCenter;
                    dir.y = 0f;
                    dir = math.normalize(dir);

                    // 2) 根据 ce.explosion 强度和时间推进
                    float3 explosionOffset = dir * ce.explosion * Time * 0.2f;
                    // 3) 应用到位置
                    transform.Position += explosionOffset;

                }
                else
                    ce.explosion = 0;
            }
            else
            {
                ce.explosionActive = false;//爆炸状态取消
              //ce.explosionTimer = 0;//这里不恢复爆炸时间内爆炸 只持续一次
            }

            #endregion


        }





    }
}