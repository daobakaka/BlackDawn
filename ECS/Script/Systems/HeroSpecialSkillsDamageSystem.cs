using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;


namespace BlackDawn.DOTS
{
    /// <summary>
    /// 在技能伤害检测系统之后运行
    /// 特殊技能执行， 如法阵虹吸 效果
    /// </summary>
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    [UpdateInGroup(typeof(ActionSystemGroup))]
    [UpdateAfter(typeof(HeroSkillsDamageSystem))]
    public partial struct HeroSpecialSkillsDamageSystem : ISystem
    {
        ComponentLookup<LocalTransform> m_transform;
        private ComponentLookup<MonsterDefenseAttribute> _monsterDefenseAttrLookup;
        private ComponentLookup<MonsterLossPoolAttribute> _monsterLossPoolAttrLookip;
        private ComponentLookup<MonsterControlledEffectAttribute> _monsterControlledEffectAttrLookup;
        private ComponentLookup<HeroAttributeCmpt> _heroAttrLookup;
        /// <summary>
        /// 法阵特殊技能造成的伤害表现为DOT伤害
        /// </summary>
       // private BufferLookup<MonsterDotDamageBuffer> _monsterDotDamageBufferLookup;

        //带有技能等级
        private  ComponentLookup<SkillArcaneCircleTag> _skillArcaneCircleTagLookup;
        //buffer用于收集进阶后的dot情况
        public BufferLookup<SkillArcaneCircleSecondBufferTag> _skillArcaneCircleSecondBufferLookup;
        //侦测系统缓存
        private SystemHandle _detectionSystemHandle;

        //公开区域
        public NativeArray<float3> arcaneCircleLinkenBuffer;
        public void OnCreate(ref SystemState state)
        {
            //外部控制
            state.RequireForUpdate<EnableHeroSpecialSkillsDamageSystemTag>();
            
            m_transform = SystemAPI.GetComponentLookup<LocalTransform>(true);

            _detectionSystemHandle = state.WorldUnmanaged.GetExistingUnmanagedSystem<DetectionSystem>();

            _monsterDefenseAttrLookup = SystemAPI.GetComponentLookup<MonsterDefenseAttribute>(true);
            _monsterLossPoolAttrLookip = SystemAPI.GetComponentLookup<MonsterLossPoolAttribute>(true);
            _monsterControlledEffectAttrLookup = SystemAPI.GetComponentLookup<MonsterControlledEffectAttribute>(true);
            _heroAttrLookup = SystemAPI.GetComponentLookup<HeroAttributeCmpt>(true);
            _skillArcaneCircleSecondBufferLookup = SystemAPI.GetBufferLookup<SkillArcaneCircleSecondBufferTag>(true);
            _skillArcaneCircleTagLookup = SystemAPI.GetComponentLookup<SkillArcaneCircleTag>(true);

        }
        void UpDataAllComponentLookup(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            // 或者用 GetSingletonBuffer 方式（需 DOTS 1.4 以上）



        }
        public void OnUpdate(ref SystemState state)
        {
            m_transform.Update(ref state);
            _monsterDefenseAttrLookup.Update(ref state);
            _monsterControlledEffectAttrLookup.Update(ref state);
            _monsterLossPoolAttrLookip.Update(ref state);
            _heroAttrLookup.Update(ref state);

            _skillArcaneCircleTagLookup.Update(ref state);
            _skillArcaneCircleSecondBufferLookup.Update(ref state);

            if (arcaneCircleLinkenBuffer.IsCreated)
                arcaneCircleLinkenBuffer.Dispose();
           // var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            //获取收集世界单例
            var detectionSystem = state.WorldUnmanaged.GetUnsafeSystemRef<DetectionSystem>(_detectionSystemHandle);
            var arcanelCorcleHitsArray = detectionSystem.arcaneCircleHitMonsterArray;


            // 为虹吸特效提供buffer，遍历BUFFer  生成特效
            var damageJobHandle = new ApplySpecialSkillDamageJob
            {
                ECB = ecb.AsParallelWriter(),
                DamageParLookup = _skillArcaneCircleTagLookup,
                DefenseAttrLookup = _monsterDefenseAttrLookup,
                SkillArcaneCirelBufferLookup = _skillArcaneCircleSecondBufferLookup,
                //DotDamageBufferLookup=_monsterDotDamageBufferLookup,
                HitArray = arcanelCorcleHitsArray,

            }.Schedule(arcanelCorcleHitsArray.Length, 64, state.Dependency);
  
    
            var collectedPositions = new NativeList<float3>(5000,Allocator.TempJob);

            // 3. 创建 Job
            var collectJobHandle = new CollectArcaneCircleLinkJob
            {
                TargetTransformLookup = m_transform,
                OutputPositions = collectedPositions.AsParallelWriter()
            }.ScheduleParallel(damageJobHandle);

            state.Dependency = collectJobHandle;
            //这里转换回主线程，获取数组
            state.Dependency.Complete();
            arcaneCircleLinkenBuffer = collectedPositions.ToArray(Allocator.Persistent);
            collectedPositions.Dispose(); 





        }
        public void OnDestroy(ref SystemState state)
        {

        }


    }

    /// <summary>
    /// 对每个特殊碰撞对
    /// </summary>
    [BurstCompile]
    struct ApplySpecialSkillDamageJob : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<SkillArcaneCircleTag> DamageParLookup;
        [ReadOnly] public ComponentLookup<MonsterDefenseAttribute> DefenseAttrLookup;
        [ReadOnly] public BufferLookup<SkillArcaneCircleSecondBufferTag> SkillArcaneCirelBufferLookup;
        [ReadOnly] public NativeArray<TriggerPairData> HitArray;//这里收集的是第二种 添加了特定标签之后的碰撞队
       // [ReadOnly] public ComponentLookup<HeroAttributeCmpt> HeroAttrLookup;
        //攻击记录buffer,道具的buffer是加在飞行道具身上
       // [ReadOnly] public BufferLookup<HitRecord> RecordBufferLookup;
        //技能位置信息
       // [ReadOnly] public ComponentLookup<LocalTransform> Transform;
        //debuffer 效果
       // [ReadOnly] public ComponentLookup<MonsterDebuffAttribute> DebufferAttrLookup;
        //buffer累加
       // [ReadOnly] public BufferLookup<MonsterDotDamageBuffer> DotDamageBufferLookup;

        public void Execute(int i)
        {
            // 1) 拿到碰撞实体对
            var pair = HitArray[i];
            Entity skill = pair.EntityA;
            Entity target = pair.EntityB;
            if (!DamageParLookup.HasComponent(skill))
            {
                skill = pair.EntityB;
                target = pair.EntityA;
            }

            //这里是特殊技能的buffer,用于专门判断,这里不用进行判断， 因为传进来的碰撞对 本质上带有二阶技能标识
            var arcaneCircleBuffer = SkillArcaneCirelBufferLookup[skill];
            // 先检查是否已经记录过这个 target
            for (int j = 0; j < arcaneCircleBuffer.Length; j++)
            {
                //DevDebug.Log("buffer：--"+j +"   "+ buffer[j].timer);
                if (arcaneCircleBuffer[j].target == target)
                {
                    // DevDebug.Log("有重复拒绝计算");
                    return;
                }
            }
            // 2) 读取组件 & 随机数
            var d = DamageParLookup[skill];//这里需要取出来等级
            var a = DefenseAttrLookup[target];//这里取出生命变化
           // var db = DotDamageBufferLookup[target];//这里是dot伤害总值

            var newBuffer = new SkillArcaneCircleSecondBufferTag();
            //将怪物的位置加给buffer,这里做一个链接特效？上千个链接？性能如何解决 时间预定义6秒,和buffer时间统一
            newBuffer.target = target;
            newBuffer.tagSurvivalTime = 6;
            // 只有没记录过，才加进来,这里要注意并行写入限制，使用并行写入方法         
            ECB.AppendToBuffer(i, skill, newBuffer);

            var dotBuffer = new MonsterDotDamageBuffer();
            //全局只有一个 不存在累加buffer的情况，6秒累加一次，一次性加出6秒的伤害
            //（7-1）当前血量的1%,等级+0.2%的血量，这里以DOT伤害来表达进行累加
            dotBuffer.dotDamage = ((a.hp / 100) * (1 + 0.2f * d.level) * 6);
            dotBuffer.survivalTime = 6;

            ECB.AppendToBuffer(i, target, dotBuffer);



        }
    }

    /// <summary>
    /// 收集法阵链接
    /// </summary>

    [BurstCompile]
    public partial struct CollectArcaneCircleLinkJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> TargetTransformLookup;
        public NativeList<float3>.ParallelWriter OutputPositions;

        void Execute(in DynamicBuffer<SkillArcaneCircleSecondBufferTag> bufferElement)
        {

            for (int i = 0; i < bufferElement.Length; i++)
            {

                float3 pos = TargetTransformLookup[bufferElement[i].target].Position;
              //强制钳制到5000
                if(bufferElement.Length<5000)
                OutputPositions.AddNoResize(pos); // 或者根据情况 Add()
            }
        }
    }


}