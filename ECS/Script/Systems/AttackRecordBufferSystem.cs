using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
///ר�����ڴ��� ��ײ����˺�����
namespace BlackDawn.DOTS
{
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    //�����䵽���Ʊ���ϵͳ֮����д���
    [UpdateAfter(typeof(RenderEffectSystem))]
    [UpdateInGroup(typeof(ActionSystemGroup))]
    partial struct AttackRecordBufferSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            //�ⲿ����
            state.RequireForUpdate<EnableAttackRecordBufferSystemTag>();


        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecbP= ecb.AsParallelWriter();
            var timer = SystemAPI.Time.DeltaTime;

            state.Dependency = new HitRecordBufferDealJob
            {
                DeltaTime = timer,
               // ECB= ecbP,
            }.ScheduleParallel(state.Dependency);

          //  state.Dependency.Complete();

            //���ڼ�¼������Ӣ�۵Ļ��������ļ�¼��
            state.Dependency = new HeroHitRecordBufferDealJob
            {

                DeltaTime=timer,

            }.ScheduleParallel(state.Dependency);

            //state.Dependency.Complete();


            state.Dependency = new SpecialSkillArcaneCircleSecondBufferDealJob
            {

                DeltaTime = timer,

            }.ScheduleParallel(state.Dependency);




            //������ECB ʵ���ϾͲ���Ҫд��
            ecb.Playback(state.EntityManager);
            ecb.Dispose();

        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }

    /// <summary>
    /// ���ڼ���֡�˺������ⶨ1�룬1���ڶ����ײֻ�ܼ���1���˺�
    /// </summary>

    [BurstCompile]
    partial struct HitRecordBufferDealJob : IJobEntity
    {
       // public EntityCommandBuffer.ParallelWriter ECB;
        public float DeltaTime;
        void Execute(Entity entity, ref DynamicBuffer<HitRecord> hitRecord,[EntityIndexInQuery] int sortKey)
        {

            for (int i = 0; i< hitRecord.Length; i++)
            { 
                var record= hitRecord[i];
                record.timer += DeltaTime;

                if (record.timer > 1f)
                {
                    hitRecord.RemoveAtSwapBack(i);
                    // ���� SwapBack�������һ��Ԫ�طŵ��˵�ǰ������Ϊ�˲�©��Ҫ�ټ����Ԫ��
                    i--;
                }
                else
                {
                    hitRecord[i] = record;
                }
            }
                            
        }
        
    }

    /// <summary>
    /// ���ڼ���֡�˺������ⶨ0.5�룬0.5���ڶ����ײֻ�ܼ���1���˺�
    /// </summary>

    [BurstCompile]
    partial struct HeroHitRecordBufferDealJob : IJobEntity
    {
        public float DeltaTime;
        void Execute(Entity entity, ref DynamicBuffer<HeroHitRecord> heroHitRecord, [EntityIndexInQuery] int sortKey)
        {

            for (int i = 0; i < heroHitRecord.Length; i++)
            {
                var record = heroHitRecord[i];
                record.timer += DeltaTime;

                if (record.timer > 0.5f)
                {
                    heroHitRecord.RemoveAtSwapBack(i);
                    // ���� SwapBack�������һ��Ԫ�طŵ��˵�ǰ������Ϊ�˲�©��Ҫ�ټ����Ԫ��
                    i--;
                }
                else
                {
                    heroHitRecord[i] = record;
                }
            }

        }
    }

    /// <summary>
    /// ���⼼�ܣ�����ڶ��׶� �������������ı�ǩ,����ֱ���Ƴ�����������ײjob���������
    /// </summary>
    [BurstCompile]
    partial struct SpecialSkillArcaneCircleSecondBufferDealJob : IJobEntity
    {
        public float DeltaTime;
        void Execute(Entity entity, ref DynamicBuffer<SkillArcaneCircleSecondBufferTag> bufferRecord, [EntityIndexInQuery] int sortKey)
        {

            for (int i = 0; i < bufferRecord.Length; i++)
            {
                var record = bufferRecord[i];
                record.tagSurvivalTime -= DeltaTime;

                if (record.tagSurvivalTime <= 0.0f)
                {
                    bufferRecord.RemoveAtSwapBack(i);
                    // ���� SwapBack�������һ��Ԫ�طŵ��˵�ǰ������Ϊ�˲�©��Ҫ�ټ����Ԫ��
                    i--;
                }
                else
                {
                    bufferRecord[i] = record;
                }
            }

        }
    }

}
