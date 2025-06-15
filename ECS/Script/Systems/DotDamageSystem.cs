using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
//ר���ڴ���DOT �˺���ϵͳ
namespace BlackDawn.DOTS
{
    [RequireMatchingQueriesForUpdate]
    //����Ⱦϵͳ֮ǰ����DOT�˺�����
    [UpdateBefore(typeof(RenderEffectSystem))]
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
            var ecb = new EntityCommandBuffer(Allocator.TempJob);


            //DevDebug.LogError("����DOT �˺�����");

            state.Dependency = new DealDotDamageJob
            {
                DamageDotTextLookup = _monsterTempDotDamage,
                ECB = ecb.AsParallelWriter(),
                DeltaTime = timer,


            }.ScheduleParallel(state.Dependency);

            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


        [BurstCompile]
        void OnDestroy(ref SystemState state)
        {



        }
    }

    [BurstCompile]
    partial struct DealDotDamageJob : IJobEntity
    {
        //�������ַ�ʽ��������������������������Ҫע��job����,ȡ��ֻ�� ���ֿ�д״̬,����������Ҫʹ��ECB ����DOT ״̬�� ���в���ECBд��
      // [NativeDisableParallelForRestriction]
        [ReadOnly] public ComponentLookup<MonsterTempDotDamageText> DamageDotTextLookup;
        public EntityCommandBuffer.ParallelWriter ECB;
        public float DeltaTime;

        public void Execute(Entity entity, [ChunkIndexInQuery] int index, ref MonsterDebuffAttribute monsterDebuff, ref MonsterDefenseAttribute monsterDefense

          , DynamicBuffer<MonsterDotDamageBuffer> monsterDotDamageBuffers, DynamicBuffer<LinkedEntityGroup> linkedEntityGroups)
        {
           // DevDebug.LogError("����DOT �˺�����");
            //��ʾ����д��,���if�������жϣ�ȡ��jump�Ļ����תָ��
            float hasHit = math.select(0f, 1f, monsterDotDamageBuffers.Length > 0);
            //�������
            float live = math.select(0f, 1f, monsterDefense.hp > 0);
            //��ʽ��Ŀ����д��
           // float hasHit = hits.Length > 0 ? 1f : 0f;
            // 1) ÿ�봥��һ�μ�ʱ��
            monsterDebuff.dotTimer += DeltaTime;
            float triggerMask = math.step(1f, monsterDebuff.dotTimer)*hasHit* live; // =1 when dotTimer>=1
            monsterDebuff.dotTimer -= triggerMask * 1f;                // ��ȥ1���0

            // 2) �������� dotDamage ���Ƴ��ѹ�����Ŀ��SwapBack ���
            float totalDamage = 0f;
            //SIMD�Ѻ�,length�ɲ�
            for (int i = 0; i < monsterDotDamageBuffers.Length; i++)
            {
                var d = monsterDotDamageBuffers[i];
                totalDamage += d.dotDamage;
                d.survivalTime -= DeltaTime;

                //���ִ�����ж����ڷ�֧Ԥ���Ѻ��ͣ�����Ҫ����SIMD�Ż�
                if (d.survivalTime >0)
                {
                    // δ���ڣ�д�ظ��º�ļ�¼
                    monsterDotDamageBuffers[i] = d;
                }
                else
                {
                    // �ѹ��ڣ��Ƴ��� SwapBack
                    monsterDotDamageBuffers.RemoveAtSwapBack(i);
                    i--; // �˻�һ�񣬼���ĩβ����������Ԫ��
                }
            }

            // 3) ������ʱ�������
            Entity dotTextEntity = linkedEntityGroups[3].Value;
            var damageDotText = DamageDotTextLookup[dotTextEntity];
            damageDotText.damageTriggerType = DamageTriggerType.DotDamage;
            damageDotText.hurtVlue = (totalDamage / 5f) * triggerMask;
            // 4) ��Ѫ��ֻ�� triggerMask==1 ʱ��������Ѫ
            monsterDefense.hp -= damageDotText.hurtVlue * triggerMask;

            ECB.SetComponent(index, dotTextEntity, damageDotText);
            ECB.SetComponentEnabled<MonsterTempDotDamageText>(
                index,
                dotTextEntity,
                triggerMask > 0f
            );

            // 4) д�� monsterDebuff��dotTimer �������£�
            ECB.SetComponent(index, entity, monsterDebuff);
            //5�� д�� �����仯
            ECB.SetComponent(index, entity, monsterDefense);

        }
    }
}