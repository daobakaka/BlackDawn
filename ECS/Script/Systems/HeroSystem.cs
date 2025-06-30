using System.Collections;
using System.Collections.Generic;
using BlackDawn;
using BlackDawn.DOTS;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using ProjectDawn.Navigation;
using UnityEditor.Search;
using ProjectDawn.ContinuumCrowds;
using Unity.Physics;
using Unity.Collections;
//英雄系统为渲染前的最后一个系统
namespace BlackDawn.DOTS
{
    [BurstCompile]
    //renderEFects 处理所有渲染效果包括文字,在渲染系统之前执行
    [UpdateInGroup(typeof(MainThreadSystemGroup))]
    [UpdateAfter(typeof(MonsterMonoSystem))]
    public partial struct HeroSystem : ISystem, ISystemStartStop
    {
        ComponentLookup<LocalTransform> m_transform;
        ComponentLookup<LocalToWorld> m_localToWorld;
        ComponentLookup<Detection_DefaultCmpt> m_detection_DefaultCmpt;
        Entity _heroEntity;
        public float3 targetPosition;//向Mono世界传输
        ProjectDawn.Navigation.Sample.Crowd.Spawner _crowdSpawner;

        private SystemHandle _detectionSystemHandle;

        private SystemHandle _overlapDetectionSystemHandle;
        public void OnCreate(ref SystemState state)
        {
            //先失活，在mono中激活，便于控制流程，由英雄初始化开启
            state.Enabled = false;
            m_transform = state.GetComponentLookup<LocalTransform>(true);
            m_localToWorld = state.GetComponentLookup<LocalToWorld>(true);
            m_detection_DefaultCmpt = state.GetComponentLookup<Detection_DefaultCmpt>(true);

           

        }
        /// <summary>
        /// 需要继承接口，这个方法才能生效
        /// </summary>
        /// <param name="state"></param>
        public void OnStartRunning(ref SystemState state)
        {


            _heroEntity = Hero.instance.heroEntity;
            //传输目标位置
            targetPosition = float3.zero;

            //---    侦察系统
            _detectionSystemHandle = state.WorldUnmanaged.GetExistingUnmanagedSystem<DetectionSystem>();
            _overlapDetectionSystemHandle = state.WorldUnmanaged.GetExistingUnmanagedSystem<OverlapDetectionSystem>();
            Debug.Log("重启系统英雄");
        }

        public void OnStopRunning(ref SystemState state)
        {



            Debug.Log("关闭系统英雄");
        }
        [BurstCompile]
        void UpDataAllComponentLookup(ref SystemState state)
        {
            m_transform.Update(ref state);
            m_localToWorld.Update(ref state);
            m_detection_DefaultCmpt.Update(ref state);
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //！！！注意，这里直接拿取如heroEntity = Hero.instance.heroEntity;，会有非常不友好的延迟，必须避免，引用在初始化完成，更新在updat中进行！！！
            var timer = SystemAPI.Time.DeltaTime;

            UpDataAllComponentLookup(ref state);

            targetPosition = m_transform[_heroEntity].Position;
            //获取收集世界单例
            var detectionSystem = state.WorldUnmanaged.GetUnsafeSystemRef<DetectionSystem>(_detectionSystemHandle);
            var arcanelCorcleHitsArray = detectionSystem.arcaneCircleHitHeroArray;

            //主动范围检测系统单例
            var overlapSystem = state.WorldUnmanaged.GetUnsafeSystemRef<OverlapDetectionSystem>(_overlapDetectionSystemHandle);
            var detectionHitsOverTimeArray = overlapSystem.detectionOverlapMonsterArray;

            //英雄系统， 对所有怪
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            //销毁 所有 基础飞行道具，英雄技能等
            foreach (var (directFlightProp, flightPro, entity) in SystemAPI.Query<RefRW<DirectFlightPropCmpt>, RefRW<FlightPropDamageCalPar>>().WithEntityAccess())
            {
                if (directFlightProp.ValueRO.originalSurvivalTime <= 0)
                {
                    ecb.DestroyEntity(entity);
                }
                else if (flightPro.ValueRO.destory == true)
                {

                    ecb.DestroyEntity(entity);
                }

            }

            //最开始的全局飞行技能消除
            foreach (var (skillCal, entity) in SystemAPI.Query<RefRO<SkillsDamageCalPar>>().WithEntityAccess())
            {

                if (skillCal.ValueRO.destory == true)

                    ecb.DestroyEntity(entity);

            }
            //最开始的全局持续性技能消除
            foreach (var (skillCal, entity) in SystemAPI.Query<RefRO<SkillsOverTimeDamageCalPar>>().WithEntityAccess())
            {

                if (skillCal.ValueRO.destory == true)

                    ecb.DestroyEntity(entity);

            }


            //处理英雄自身的增益恢复效果
            foreach (var (transform, heroAttr, stateNoImmunity) in SystemAPI.Query<RefRW<HeroEntityMasterTag>, RefRW<HeroAttributeCmpt>, RefRW<HeroIntgratedNoImmunityState>>())

            {
                //精力恢复,这里要添加原始精力
                heroAttr.ValueRW.defenseAttribute.energy += (heroAttr.ValueRW.gainAttribute.energyRegen+10) * timer;

                heroAttr.ValueRW.defenseAttribute.hp += heroAttr.ValueRW.gainAttribute.hpRegen * timer;

            }
            //技能法阵的英雄自身伤害、DOT减免、控制抵消的判定
            HeroSkillArcanelCorcleDeal(ref state,ecb, arcanelCorcleHitsArray.Length, timer);



            if (!SystemAPI.HasSingleton<ProjectDawn.Navigation.Sample.Crowd.Spawner>())
                return;

        }
        /// <summary>
        /// 分开写，法阵技能的状态处理，增加分层逻辑
        /// </summary>
        /// <param name="state"></param>
        void HeroSkillArcanelCorcleDeal(ref SystemState state,EntityCommandBuffer ecb ,int length, float timer)
        {


            foreach (var (transform, heroAttr, stateNoImmunity,linkedGroup) in SystemAPI.Query<RefRW<HeroEntityMasterTag>, RefRW<HeroAttributeCmpt>, RefRW<HeroIntgratedNoImmunityState>,DynamicBuffer<LinkedEntityGroup>>())
            {

                //拿取碰撞对，英雄处于碰撞对中，则按照帧掉血？应该也可以

                //二阶法阵中生命值的扣除逻辑，有基础法阵标签才执行的逻辑
                if (SystemAPI.TryGetSingleton<SkillArcaneCircleTag>(out var tag))
                {
                    if (length > 0)
                    {
                        //一阶段生命恢复标识,1000% 总元素伤害系数的恢复， 100% 元素伤害，则每秒回复10点
                        var attackPar = heroAttr.ValueRO.attackAttribute.elementalDamage;
                        var recoverPar = (attackPar.fireDamage + attackPar.frostDamage + attackPar.shadowDamage + attackPar.lightningDamage + attackPar.poisonDamage) * 100;
                        heroAttr.ValueRW.defenseAttribute.hp += recoverPar * timer;


                        //1+等级乘以系数，默认等级为1？二阶技能开启标识,英雄掉血逻辑这里自动判断
                        if (tag.enableSecondA == true)
                        {
                            //s DevDebug.LogError("自己扣血"+ stateNoImmunity.ValueRW.inlineDamageNoImmunity);
                            heroAttr.ValueRW.defenseAttribute.hp -= (((heroAttr.ValueRW.defenseAttribute.originalHp / 100) * (5 + (tag.level - 1) * 0.5f)) * timer * stateNoImmunity.ValueRW.inlineDamageNoImmunity);

                            //开启链接特效
                            ecb.SetComponentEnabled<HeroEffectsLinked>(linkedGroup[1].Value, true);

                        }
                        //英雄法阵内伤害免疫逻辑
                        if (tag.enableSecondB == true)
                        {
                            //DevDebug.LogError("自己扣血三阶");
                            // stateNoImmunity.ValueRW.inlineDamageNoImmunity = 0;
                            //免疫dot伤害
                            stateNoImmunity.ValueRW.dotNoImmunity = 0;
                            //免疫控制
                            stateNoImmunity.ValueRW.controlNoImmunity = 0;

                        }

                    }

                    else
                    {

                        //1+等级乘以系数，默认等级为1？二阶技能开启标识,英雄掉血逻辑这里自动判断
                        if (tag.enableSecondA == true)
                        {
                            //关闭链接特效
                            ecb.SetComponentEnabled<HeroEffectsLinked>(linkedGroup[1].Value, false);

                            // DevDebug.Log("初始执行关闭链接");
                        }


                        if (tag.enableSecondB == true)

                        {
                            // stateNoImmunity.ValueRW.inlineDamageNoImmunity = 1;
                            //免疫dot伤害
                            stateNoImmunity.ValueRW.dotNoImmunity = 1;
                            //免疫控制
                            stateNoImmunity.ValueRW.controlNoImmunity = 1;
                        }

                    }
                }
                else
                {

                    //关闭链接特效
                    ecb.SetComponentEnabled<HeroEffectsLinked>(linkedGroup[1].Value, false);

                }


            }

        }
    }
}