using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
//专用于处理DOT 伤害的系统
namespace BlackDawn.DOTS
{
    [RequireMatchingQueriesForUpdate]
    //在渲染系统之前进行DOT伤害计算
    [UpdateAfter(typeof(EnemyFlightPropDamageSystem))]
    [UpdateInGroup(typeof(ActionSystemGroup))]
    [BurstCompile]
    public partial struct DotDamageSystem : ISystem
    {
        private ComponentLookup<MonsterTempDotDamageText> _monsterTempDotDamage;

        void OnCreate(ref SystemState state)
        {

            state.RequireForUpdate<EnableDotDamageSystemTag>();

            _monsterTempDotDamage = state.GetComponentLookup<MonsterTempDotDamageText>(false);

        }

        [BurstCompile]
        void OnUpdate(ref SystemState state)
        {
            _monsterTempDotDamage.Update(ref state);
            var timer = SystemAPI.Time.DeltaTime;
            //var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);


            //DevDebug.LogError("更新DOT 伤害计算");

            state.Dependency = new DealDotDamageJob
            {
                DamageDotTextLookup = _monsterTempDotDamage,
                ECB = ecb.AsParallelWriter(),
                DeltaTime = timer,


            }.ScheduleParallel(state.Dependency);

            //state.Dependency.Complete();

            //ecb.Playback(state.EntityManager);
            //ecb.Dispose();
        }


        [BurstCompile]
        void OnDestroy(ref SystemState state)
        {



        }
    }

    [BurstCompile]
    partial struct DealDotDamageJob : IJobEntity
    {
        //采用这种方式，更轻量化，减少依赖，但是要注意job依赖,取消只读 保持可写状态,但是这里需要使用ECB 保持DOT 状态， 所有采用ECB写回
      // [NativeDisableParallelForRestriction]
        [ReadOnly] public ComponentLookup<MonsterTempDotDamageText> DamageDotTextLookup;
        public EntityCommandBuffer.ParallelWriter ECB;
        public float DeltaTime;

        public void Execute(Entity entity, [ChunkIndexInQuery] int index, ref MonsterDebuffAttribute monsterDebuff, ref MonsterDefenseAttribute monsterDefense

          , DynamicBuffer<MonsterDotDamageBuffer> monsterDotDamageBuffers, DynamicBuffer<LinkedEntityGroup> linkedEntityGroups)
        {
           // DevDebug.LogError("进入DOT 伤害计算");
            //显示掩码写法,替代if的条件判断，取消jump的汇编跳转指令
            float hasHit = math.select(0f, 1f, monsterDotDamageBuffers.Length > 0);
            //怪物活着
            float live = math.select(0f, 1f, monsterDefense.hp > 0);
            //隐式三目运算写法
           // float hasHit = hits.Length > 0 ? 1f : 0f;
            // 1) 每秒触发一次计时器
            monsterDebuff.dotTimer += DeltaTime;
            float triggerMask = math.step(1f, monsterDebuff.dotTimer)*hasHit* live; // =1 when dotTimer>=1
            monsterDebuff.dotTimer -= triggerMask * 1f;                // 减去1秒或0

            // 2) 汇总所有 dotDamage 并移除已过期条目（SwapBack 风格）
            float totalDamage = 0f;
            //SIMD友好,length可测
            for (int i = 0; i < monsterDotDamageBuffers.Length; i++)
            {
                var d = monsterDotDamageBuffers[i];
                totalDamage += d.dotDamage;
                d.survivalTime -= DeltaTime;

                //这种大概率判断属于分支预测友好型，不需要进行SIMD优化
                if (d.survivalTime >0)
                {
                    // 未过期，写回更新后的记录
                    monsterDotDamageBuffers[i] = d;
                }
                else
                {
                    // 已过期，移除并 SwapBack
                    monsterDotDamageBuffers.RemoveAtSwapBack(i);
                    i--; // 退回一格，检查从末尾换进来的新元素
                }
            }

            // 3) 更新临时文字组件
            Entity dotTextEntity = linkedEntityGroups[3].Value;
            var damageDotText = DamageDotTextLookup[dotTextEntity];
            damageDotText.damageTriggerType = DamageTriggerType.DotDamage;
            damageDotText.hurtVlue = (totalDamage / 5f) * triggerMask;
            // 4) 扣血：只在 triggerMask==1 时才真正减血
            monsterDefense.hp -= damageDotText.hurtVlue * triggerMask;

            ECB.SetComponent(index, dotTextEntity, damageDotText);
            ECB.SetComponentEnabled<MonsterTempDotDamageText>(
                index,
                dotTextEntity,
                triggerMask > 0f
            );

            //// 4) 写回 monsterDebuff（dotTimer 保持最新）
            //ECB.SetComponent(index, entity, monsterDebuff);
            ////5） 写回 生命变化
            //ECB.SetComponent(index, entity, monsterDefense);

        }
    }
}