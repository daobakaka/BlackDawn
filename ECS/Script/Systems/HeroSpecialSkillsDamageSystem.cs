using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.VisualScripting;

#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
#endif


//执行特殊技能的伤害处理
namespace BlackDawn.DOTS
{
    /// <summary>
    /// 在技能伤害检测系统之后运行
    /// 特殊技能执行， 如法阵虹吸 效果
    /// </summary>
    [RequireMatchingQueriesForUpdate]
    // [BurstCompile]
    [UpdateInGroup(typeof(ActionSystemGroup))]
    [UpdateBefore(typeof(HeroSkillsDamageSystem))]//-- 注意这里在技能伤害计算系统之前运行， 确保出伤害数字，好像还是未渲染出来？
    public partial struct HeroSpecialSkillsDamageSystem : ISystem
    {
        ComponentLookup<LocalTransform> m_transform;
        private ComponentLookup<MonsterDefenseAttribute> _monsterDefenseAttrLookup;
        private ComponentLookup<MonsterLossPoolAttribute> _monsterLossPoolAttrLookup;
        private ComponentLookup<MonsterControlledEffectAttribute> _monsterControlledEffectAttrLookup;
        private ComponentLookup<MonsterDebuffAttribute> _monsterDebufferAttributeLookup;
        private ComponentLookup<HeroAttributeCmpt> _heroAttrLookup;
        private BufferLookup<HitElementResonanceRecord> _hitElementResonanceRecordBufferLookup;
        private BufferLookup<LinkedEntityGroup> _linkedEntityGroupLookup;
        private BufferLookup<MonsterDotDamageBuffer> _monsterDotDamafeBufferLookup;
        private ComponentLookup<FlightPropDamageCalPar> _flightPropDamageCalParLookup;
        private ComponentLookup<SkillsDamageCalPar> _skillsDamageCalParLookup;
        private ComponentLookup<SkillMineBlastExplosionTag> _skillMineBlastExplosionTagLookup;
        private ComponentLookup<SkillPoisonRainATag> _skillPoisonRainATagLookup;
        private ComponentLookup<SkillThunderGripTag> _skillThunderGripTagLookup;
        private ComponentLookup<SkillChainDevourTag> _skillChainDevourTagLookup;
        private ComponentLookup<PreDefineHeroSkillThunderGripTag> _preDefineHeroSkillThunderGripTagLookup;
        private ComponentLookup<SkillsTrackingCalPar> _skillsTrackingCalParLookup;//寻址类技能通用计算标签
        /// <summary>
        /// 法阵特殊技能造成的伤害表现为DOT伤害
        /// </summary>
        // private BufferLookup<MonsterDotDamageBuffer> _monsterDotDamageBufferLookup;

        //带有技能等级
        private ComponentLookup<SkillArcaneCircleTag> _skillArcaneCircleTagLookup;
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
            _monsterLossPoolAttrLookup = SystemAPI.GetComponentLookup<MonsterLossPoolAttribute>(true);
            _monsterControlledEffectAttrLookup = SystemAPI.GetComponentLookup<MonsterControlledEffectAttribute>(true);
            _heroAttrLookup = SystemAPI.GetComponentLookup<HeroAttributeCmpt>(true);
            _skillArcaneCircleSecondBufferLookup = SystemAPI.GetBufferLookup<SkillArcaneCircleSecondBufferTag>(true);
            _skillArcaneCircleTagLookup = SystemAPI.GetComponentLookup<SkillArcaneCircleTag>(true);
            _hitElementResonanceRecordBufferLookup = SystemAPI.GetBufferLookup<HitElementResonanceRecord>(true);
            _linkedEntityGroupLookup = SystemAPI.GetBufferLookup<LinkedEntityGroup>(true);
            _monsterDotDamafeBufferLookup = SystemAPI.GetBufferLookup<MonsterDotDamageBuffer>(true);
            _flightPropDamageCalParLookup = SystemAPI.GetComponentLookup<FlightPropDamageCalPar>(true);
            _skillsDamageCalParLookup = SystemAPI.GetComponentLookup<SkillsDamageCalPar>(true);
            _skillMineBlastExplosionTagLookup = SystemAPI.GetComponentLookup<SkillMineBlastExplosionTag>(true);
            _monsterDebufferAttributeLookup = SystemAPI.GetComponentLookup<MonsterDebuffAttribute>(true);
            _skillPoisonRainATagLookup = SystemAPI.GetComponentLookup<SkillPoisonRainATag>(true);
            _skillThunderGripTagLookup = SystemAPI.GetComponentLookup<SkillThunderGripTag>(true);
            _preDefineHeroSkillThunderGripTagLookup = SystemAPI.GetComponentLookup<PreDefineHeroSkillThunderGripTag>(true);
            _skillChainDevourTagLookup = SystemAPI.GetComponentLookup<SkillChainDevourTag>(true);
            _skillsTrackingCalParLookup = SystemAPI.GetComponentLookup<SkillsTrackingCalPar>(true);

        }
        public void OnUpdate(ref SystemState state)
        {
            m_transform.Update(ref state);
            _monsterDefenseAttrLookup.Update(ref state);
            _monsterControlledEffectAttrLookup.Update(ref state);
            _monsterLossPoolAttrLookup.Update(ref state);
            _heroAttrLookup.Update(ref state);
            _monsterDebufferAttributeLookup.Update(ref state);
            _monsterDotDamafeBufferLookup.Update(ref state);

            _skillArcaneCircleTagLookup.Update(ref state);
            _skillArcaneCircleSecondBufferLookup.Update(ref state);

            _hitElementResonanceRecordBufferLookup.Update(ref state);
            _linkedEntityGroupLookup.Update(ref state);

            _flightPropDamageCalParLookup.Update(ref state);
            _skillsDamageCalParLookup.Update(ref state);

            //毒爆地雷爆炸标签
            _skillMineBlastExplosionTagLookup.Update(ref state);
            //毒雨 二阶段增伤标签
            _skillPoisonRainATagLookup.Update(ref state);
            //雷霆之握，技能标签,预定义标签
            _skillThunderGripTagLookup.Update(ref state);
            _preDefineHeroSkillThunderGripTagLookup.Update(ref state);
            //连锁吞噬 技能标签，通用寻址类技能标签
            _skillChainDevourTagLookup.Update(ref state);
            _skillsTrackingCalParLookup.Update(ref state);

            var deltaTime = SystemAPI.Time.DeltaTime;
            if (arcaneCircleLinkenBuffer.IsCreated)
                arcaneCircleLinkenBuffer.Dispose();
            // var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            //获取收集世界单例
            var detectionSystem = state.WorldUnmanaged.GetUnsafeSystemRef<DetectionSystem>(_detectionSystemHandle);
            //获取法阵碰撞对
            var arcanelCorcleHitsArray = detectionSystem.arcaneCircleHitMonsterArray;
            //获取元素共鸣碰撞对
            var elementResonanceHitArray = detectionSystem.combinedElementResonanceArray;
            //获取毒爆地雷碰撞对
            var mineBlastExplosionHitMonsterArray = detectionSystem.mineBlastExplosionHitMonsterArray;
            //获取毒雨碰撞对
            var poisonRainAHitMonterArray = detectionSystem.posionRainAHitMonsterArray;
            //获取雷霆之握碰撞对,进行一个标记，采取预标记策略，考虑到变化阶段的问题，这里还是在多线程里计算
            var thunderGripHitMonsterArray = detectionSystem.thunderGripHitMonsterArray;
            //获取连锁吞噬碰撞对
            var chainDevourHitMonsterArray = detectionSystem.chainDevourHitMonsterArray;
            // if(thunderGripHitMonsterArray.Length>0)
            // DevDebug.LogError("雷霆之握碰撞对长度" + thunderGripHitMonsterArray.Length);
            //雷霆之握技能处理,由侦测系统开关控制
            if (detectionSystem.enableSpecialSkillThunderGrip)
                state.Dependency = new ApplySpecialSkillThunderGripDamageJob
                {
                    ECB = ecb.AsParallelWriter(),
                    DefenseAttrLookup = _monsterDefenseAttrLookup,
                    DebuffAttrLookup = _monsterDebufferAttributeLookup,
                    SkillTagLookup = _skillThunderGripTagLookup,
                    TransformLookup = m_transform,
                    SkillDamageCalLookup = _skillsDamageCalParLookup,
                    PreDefineHeroSkillThunderGripTagLookup = _preDefineHeroSkillThunderGripTagLookup,
                    HitArray = thunderGripHitMonsterArray
                }.ScheduleParallel(thunderGripHitMonsterArray.Length, 64, state.Dependency);


            //连锁吞噬技能处理，由侦测系统开关控制
            if (detectionSystem.enableSpecialSkillChainDevour&&chainDevourHitMonsterArray.Length>0)
                state.Dependency = new ApplySpecialSkillChainDevourDamageJob
                {
                    ECB = ecb.AsParallelWriter(),
                    DefenseAttrLookup = _monsterDefenseAttrLookup,
                    DebuffAttrLookup = _monsterDebufferAttributeLookup,
                    LossPoolAttrLookup = _monsterLossPoolAttrLookup,
                    DotDamageBufferLookup = _monsterDotDamafeBufferLookup,
                    SkillTagLookup = _skillChainDevourTagLookup,
                    TransformLookup = m_transform,
                    SkillDamageCalLookup = _skillsDamageCalParLookup,
                    SkillsTrackingCalParLookup = _skillsTrackingCalParLookup,
                    HitArray = chainDevourHitMonsterArray
                }
                .ScheduleParallel(chainDevourHitMonsterArray.Length, 64, state.Dependency);
            //Schedule(chainDevourHitMonsterArray.Length, state.Dependency);
               
                // if (chainDevourHitMonsterArray.Length > 0)
                // DevDebug.Log("chian长度" + chainDevourHitMonsterArray.Length);
               //.ScheduleParallel(chainDevourHitMonsterArray.Length, 64, state.Dependency);



            if (false)
            {
                // 为虹吸特效提供buffer，遍历BUFFer  生成特效
                var damageJobHandle = new ApplySpecialSkillArcaneCircleDamageJob
                {
                    ECB = ecb.AsParallelWriter(),
                    DamageParLookup = _skillArcaneCircleTagLookup,
                    DefenseAttrLookup = _monsterDefenseAttrLookup,
                    SkillArcaneCirelBufferLookup = _skillArcaneCircleSecondBufferLookup,
                    //DotDamageBufferLookup=_monsterDotDamageBufferLookup,
                    HitArray = arcanelCorcleHitsArray,

                }.Schedule(arcanelCorcleHitsArray.Length, 64, state.Dependency);


                var collectedPositions = new NativeList<float3>(5000, Allocator.TempJob);

                // 3. 创建这里是为了  法阵的链解特效做的代码块
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

                //元素共鸣的相关计算
                var elementResonanceEnableSecond = false;
                var elementResonanceEnableThird = false;
                float elementResonanceSecondPar = 0;
                float elementRespnanceThridPar = 0;

                foreach (var (skillTagE, entity) in SystemAPI.Query<RefRW<SkillElementResonanceTag>>().WithEntityAccess())
                {
                    elementResonanceEnableSecond = skillTagE.ValueRO.enableSecondA;
                    elementResonanceEnableThird = skillTagE.ValueRO.enableSecondB;
                    elementResonanceSecondPar = skillTagE.ValueRO.secondDamagePar;
                    elementRespnanceThridPar = skillTagE.ValueRO.thridDamagePar;
                    break;
                }

                state.Dependency = new ApplySpecialSkillElementResonanceDamageJob
                {
                    DefenseAttrLookup = _monsterDefenseAttrLookup,
                    ECB = ecb.AsParallelWriter(),
                    FlightPropDamageCalParLookup = _flightPropDamageCalParLookup,
                    SkillDamageCalParLookup = _skillsDamageCalParLookup,
                    LossPoolAttrLookup = _monsterLossPoolAttrLookup,
                    HitArray = elementResonanceHitArray,
                    LinkedLookup = _linkedEntityGroupLookup,
                    RecordElementResonanceBufferLookup = _hitElementResonanceRecordBufferLookup,
                    EnableSecond = elementResonanceEnableSecond,
                    EnableThrid = elementResonanceEnableThird,
                    SecondDamagePar = elementResonanceSecondPar,
                    ThridDamagePar = elementRespnanceThridPar,

                }.Schedule(elementResonanceHitArray.Length, 64, state.Dependency);

                // 毒爆地雷B阶段的，毒伤状态计算

                // DevDebug.LogError("毒伤碰撞对长度" + mineBlastExplosionHitMonsterArray.Length);

                state.Dependency = new ApplySpecialMineBlastExplosionPosionDamageJob
                {
                    DefenseAttrLookup = _monsterDefenseAttrLookup,
                    ECB = ecb.AsParallelWriter(),
                    SkillTagLookup = _skillMineBlastExplosionTagLookup,
                    HitArray = mineBlastExplosionHitMonsterArray,
                    DeltaTime = deltaTime,
                    DebufferAttrLookup = _monsterDebufferAttributeLookup,

                }.Schedule(mineBlastExplosionHitMonsterArray.Length, 64, state.Dependency);



                // DevDebug.LogError("毒雨碰撞对长度" + poisonRainAHitMonterArray.Length);
                //毒雨 伤害加深的buff
                state.Dependency = new ApplySpecialPoisonRainDamageAJob
                {
                    DefenseAttrLookup = _monsterDefenseAttrLookup,
                    ECB = ecb.AsParallelWriter(),
                    HitArray = poisonRainAHitMonterArray,
                    DeltaTime = deltaTime,
                    DebuffAttrLookup = _monsterDebufferAttributeLookup,
                    SkillTagLookup = _skillPoisonRainATagLookup,

                }.Schedule(poisonRainAHitMonterArray.Length, 64, state.Dependency);


            }





        }
        public void OnDestroy(ref SystemState state)
        {

        }








    }

    /// <summary>
    /// 对每个特殊碰撞对
    /// </summary>
    [BurstCompile]
    struct ApplySpecialSkillArcaneCircleDamageJob : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<SkillArcaneCircleTag> DamageParLookup;
        [ReadOnly] public ComponentLookup<MonsterDefenseAttribute> DefenseAttrLookup;
        [ReadOnly] public BufferLookup<SkillArcaneCircleSecondBufferTag> SkillArcaneCirelBufferLookup;
        [ReadOnly] public NativeArray<TriggerPairData> HitArray;//这里收集的是第二种 添加了特定标签之后的碰撞队

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
                if (bufferElement.Length < 5000)
                    OutputPositions.AddNoResize(pos); // 或者根据情况 Add()
            }
        }
    }

    /// <summary>
    /// 元素共鸣的伤害计算
    /// </summary>

    [BurstCompile]
    struct ApplySpecialSkillElementResonanceDamageJob : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<MonsterDefenseAttribute> DefenseAttrLookup;
        [ReadOnly] public ComponentLookup<SkillsDamageCalPar> SkillDamageCalParLookup;
        [ReadOnly] public ComponentLookup<FlightPropDamageCalPar> FlightPropDamageCalParLookup;
        [ReadOnly] public ComponentLookup<MonsterLossPoolAttribute> LossPoolAttrLookup;
        [ReadOnly] public BufferLookup<HitElementResonanceRecord> RecordElementResonanceBufferLookup;
        [ReadOnly] public BufferLookup<LinkedEntityGroup> LinkedLookup;
        [ReadOnly] public NativeArray<TriggerPairData> HitArray;//收集产生元素共鸣的碰撞对，合并元素共鸣/暗影吞噬类特效,可以使用广告牌做激发特效

        public bool EnableSecond;
        public bool EnableThrid;
        public float SecondDamagePar;
        public float ThridDamagePar;

        public void Execute(int i)
        {
            var pair = HitArray[i];
            Entity damage = pair.EntityA;
            Entity target = pair.EntityB; // target = 怪物

            // 取出怪物属性
            var d = DefenseAttrLookup[target];
            var l = LossPoolAttrLookup[target];
            var textRenderEntity = LinkedLookup[target][2].Value;
            var buffer = RecordElementResonanceBufferLookup[damage];

            // 防止重复处理
            bool alreadyProcessed = false;
            for (int j = 0; j < buffer.Length; j++)
            {
                alreadyProcessed |= buffer[j].other == target;
            }
            if (alreadyProcessed) return;
            // 添加元素共鸣buffer,添加一个判断， 后期整改
            if (!RecordElementResonanceBufferLookup.HasBuffer(damage))
                return;
            ECB.AppendToBuffer(i, damage, new HitElementResonanceRecord { other = target });

            // 计算dotNum: 分支变掩码
            float dotCount = l.fireActive + l.frostActive + l.lightningActive + l.poisonActive + l.shadowActive;
            float mask2 = math.select(0f, 1f, EnableSecond && dotCount >= 12 && dotCount < 20);
            float mask3 = math.select(0f, 1f, EnableThrid && dotCount >= 20);
            float dotNum = 1f + mask2 * SecondDamagePar + mask3 * ThridDamagePar;

            // 取 skill/flight 属性、统一混合处理（SIMD友好关键）
            bool isSkill = SkillDamageCalParLookup.HasComponent(damage);
            bool isFlight = FlightPropDamageCalParLookup.HasComponent(damage);

            float frost = 0, fire = 0, lightning = 0, poison = 0, shadow = 0;

            if (isSkill)
            {
                var skill = SkillDamageCalParLookup[damage];
                frost = skill.frostDamage;
                fire = skill.fireDamage;
                lightning = skill.lightningDamage;
                poison = skill.poisonDamage;
                shadow = skill.shadowDamage;
            }
            else if (isFlight)
            {
                var flight = FlightPropDamageCalParLookup[damage];
                frost = flight.frostDamage;
                fire = flight.fireDamage;
                lightning = flight.lightningDamage;
                poison = flight.poisonDamage;
                shadow = flight.shadowDamage;
            }

            float totalDamage = (frost + fire + lightning + poison + shadow) * (1 - d.damageReduction) * dotNum;

            // DevDebug.LogError("开始共鸣伤害计算"+totalDamage);
            // 用掩码方式、全部都写，最后只激活一种
            int skillMask = isSkill ? 1 : 0;
            int flightMask = isFlight ? 1 : 0;

            // 写入技能/道具专属数据（这样Burst能合并分支）
            FlightPropAccumulateData skillData = default;
            skillData.damage = totalDamage * skillMask;

            HeroSkillPropAccumulateData flightData = default;
            flightData.damage = totalDamage * flightMask;

            // SIMD友好写法，写回只激活一种
            if (isSkill)
                ECB.AppendToBuffer(i, target, skillData);
            if (isFlight)
                ECB.AppendToBuffer(i, target, flightData);

            // 激活一次伤害飘字
            ECB.SetComponentEnabled<MonsterTempDamageText>(i, textRenderEntity, true);

            //测试共鸣效果,而
            //l.fireActive = 6;
            //ECB.SetComponent(i, target, l);
        }
    }

    /// <summary>
    /// 毒爆地雷B 阶段毒伤地雷 计算,持续性的技能可以计算秒变/不用buffer?
    /// </summary>
    struct ApplySpecialMineBlastExplosionPosionDamageJob : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<MonsterDefenseAttribute> DefenseAttrLookup;
        [ReadOnly] public ComponentLookup<MonsterDebuffAttribute> DebufferAttrLookup;
        [ReadOnly] public ComponentLookup<SkillMineBlastExplosionTag> SkillTagLookup;
        [ReadOnly] public NativeArray<TriggerPairData> HitArray;//收集毒伤地雷碰撞对
        public float DeltaTime;

        public void Execute(int i)
        {

            // 1) 拿到碰撞实体对
            var pair = HitArray[i];
            Entity skill = pair.EntityA;
            Entity target = pair.EntityB;
            if (!SkillTagLookup.HasComponent(skill))
            {
                skill = pair.EntityB;
                target = pair.EntityA;
            }

            var d = DefenseAttrLookup[target];
            var db = DebufferAttrLookup[target];
            var st = SkillTagLookup[skill];
            //DevDebug.Log("进入计算");

            if (st.enableSecondB)
            {
                //  DevDebug.Log("计算减少值");
                db.armorReduction -= (20 + st.level * 2) * DeltaTime;
                db.resistanceReduction.poison -= (10 + st.level * 1) * DeltaTime;

            }
            //制造掩码
            float maskPoison = st.enableSecondB ? 1 : 0;

            if (st.enableSecondC)
            {
                //  DevDebug.Log("计算减少值");
                db.resistanceReduction.frost -= (st.level * 0.5f + 10 * maskPoison) * DeltaTime;
                db.resistanceReduction.fire -= (st.level * 0.5f + 10 * maskPoison) * DeltaTime;
                db.resistanceReduction.lightning -= (st.level * 0.5f + 10 * maskPoison) * DeltaTime;
                db.resistanceReduction.shadow -= (st.level * 0.5f + 10 * maskPoison) * DeltaTime;
                db.resistanceReduction.poison -= (st.level * 0.5f) * DeltaTime;
                db.armorReduction -= (st.level * 1) * DeltaTime; ;

            }

            //写回计算数据
            ECB.SetComponent(i, target, db);
        }


    }



    /// <summary>
    /// 毒雨A阶段，伤害加深
    /// </summary>
    struct ApplySpecialPoisonRainDamageAJob : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<MonsterDefenseAttribute> DefenseAttrLookup;
        [ReadOnly] public ComponentLookup<MonsterDebuffAttribute> DebuffAttrLookup;
        [ReadOnly] public ComponentLookup<SkillPoisonRainATag> SkillTagLookup;
        [ReadOnly] public NativeArray<TriggerPairData> HitArray;//收集毒雨碰撞对
        public float DeltaTime;

        public void Execute(int i)
        {

            // 1) 拿到碰撞实体对
            var pair = HitArray[i];
            Entity skill = pair.EntityA;
            Entity target = pair.EntityB;
            if (!SkillTagLookup.HasComponent(skill))
            {
                skill = pair.EntityB;
                target = pair.EntityA;
            }

            var d = DefenseAttrLookup[target];
            var db = DebuffAttrLookup[target];
            var st = SkillTagLookup[skill];

            db.damageAmplification -= DeltaTime * (st.level + 1);

            //写回计算数据
            ECB.SetComponent(i, target, db);
        }
    }

    /// <summary>
    /// 雷霆之握技能伤害计算
    /// </summary>
    struct ApplySpecialSkillThunderGripDamageJob : IJobFor
    {

        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<MonsterDefenseAttribute> DefenseAttrLookup;
        [ReadOnly] public ComponentLookup<MonsterDebuffAttribute> DebuffAttrLookup;
        [ReadOnly] public ComponentLookup<SkillsDamageCalPar> SkillDamageCalLookup;
        [ReadOnly] public ComponentLookup<SkillThunderGripTag> SkillTagLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        //传入预定义标签进行单次判断
        [ReadOnly] public ComponentLookup<PreDefineHeroSkillThunderGripTag> PreDefineHeroSkillThunderGripTagLookup;
        [ReadOnly] public NativeArray<TriggerPairData> HitArray;//收集雷霆之握碰撞对

        public void Execute(int i)
        {

            var pair = HitArray[i];
            Entity skill = pair.EntityA;
            Entity target = pair.EntityB;

            // 检查目标是否有 PreDefineHeroSkillThunderGripTag
            if (PreDefineHeroSkillThunderGripTagLookup.HasComponent(target) && PreDefineHeroSkillThunderGripTagLookup.IsComponentEnabled(target))

                return;


            var debuff = DebuffAttrLookup[target];
            var skillTag = SkillTagLookup[skill];
            var skillDamageCal = SkillDamageCalLookup[skill];
            var preDefineSkillTag = PreDefineHeroSkillThunderGripTagLookup[target];
            var transform = TransformLookup[target];
            var heroTransform = TransformLookup[skillDamageCal.heroRef];

            // 英雄正面方向
            float3 heroForward = math.mul(heroTransform.Rotation, new float3(0, 0, 1));
            // 目标点为英雄位置前方一定距离（如5米）
            float3 targetPos = heroTransform.Position + heroForward * 5f;
            debuff.thunderPosition = targetPos;
            //重置叠加时间
            debuff.thunderGripEndTimer = 0;


            ECB.SetComponentEnabled<PreDefineHeroSkillThunderGripTag>(i, target, true);

            if (skillTag.enableSecondA)
            {
                debuff.damageAmplification -= (0.2f + skillTag.level * 0.01f);
            }


            ECB.SetComponent(i, target, debuff);


        }
    }
/// <summary>
/// 连锁吞噬回调计算
/// </summary>
    struct ApplySpecialSkillChainDevourDamageJob : IJobFor
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<MonsterDefenseAttribute> DefenseAttrLookup;
        [ReadOnly] public ComponentLookup<MonsterLossPoolAttribute> LossPoolAttrLookup;
        [ReadOnly] public ComponentLookup<MonsterDebuffAttribute> DebuffAttrLookup;
        [ReadOnly] public ComponentLookup<SkillsDamageCalPar> SkillDamageCalLookup;
        [ReadOnly] public ComponentLookup<SkillChainDevourTag> SkillTagLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        [ReadOnly] public BufferLookup<MonsterDotDamageBuffer> DotDamageBufferLookup;
        //传入预定义标签进行单次判断
        [ReadOnly] public ComponentLookup<SkillsTrackingCalPar> SkillsTrackingCalParLookup;//寻踪类技能专用标签
        [ReadOnly] public NativeArray<TriggerPairData> HitArray;//收集连锁吞噬碰撞对，这里因此涉及到多个，似乎无法有效控制监测器大小，需要降低道具飞行速度，降低监测范围，以适应开发需求

        public void Execute(int index)
        {

            var pair = HitArray[index];
            Entity skill = pair.EntityA;
            Entity target = pair.EntityB;

            // 检查目标是否有 
            if (!SkillsTrackingCalParLookup.HasComponent(skill))

                return;


            var debuff = DebuffAttrLookup[target];
            var skillTag = SkillTagLookup[skill];
            var skillDamageCal = SkillDamageCalLookup[skill];
            var trackingCalParSkillTag = SkillsTrackingCalParLookup[skill];
            //dot的buffer情况，用于遍历
            var dotBuffer = DotDamageBufferLookup[target];
            //池 用于关闭渲染dot特效
            var monsterPool = LossPoolAttrLookup[target];
            if (!trackingCalParSkillTag.enbaleChangeTarget)
            {//碰撞到之后回调确认,同时碰撞次数-1,时间约束器归0，可以重新计算碰撞
                trackingCalParSkillTag.enbaleChangeTarget = true;
                trackingCalParSkillTag.runCount -= 1;
                trackingCalParSkillTag.timer = 0;
                //写回回调,回调确认之后，这里放到 overlapDetectionSystem 进行处理 
                ECB.SetComponent(index, skill, trackingCalParSkillTag);

                skillDamageCal.damageChangePar += 0.2f;

                //写回伤害变化，碰撞一次增伤20%
                ECB.SetComponent(index, skill, skillDamageCal);
            }
            //开启第二阶段连锁吞噬处理，这里几乎不需要处理多个组件碰撞的聚合问题
            //这里通过标签控制进行单次计算
            if (skillTag.enableSecondB && !debuff.toClearDotBuffer)
            {

                float devourDamage = 0;
                for (int i = 0; i < dotBuffer.Length; i++)
                {

                    devourDamage += dotBuffer[i].dotDamage;

                }

                //直减还是添加到累计buffer里面？--暂时用累计buffer的方式--这个系统是运行在 通用伤害参数计算系统之后
                //后面伤害飘字的显示，可能会优化为使用广告牌预制体的方式生成
                //这里是直接累加到buffer里面通用表达
                devourDamage *= (10 + trackingCalParSkillTag.originalCount);
                //吞噬伤害不计入池化值，写回吞噬，伤害，apeend 伤害系统去降低血量-- 原本的末位减1逻辑是否应更改，目前增加到首位第二开始
                //或者干脆取消直接写入，统一在通用类中采用buffer写入
                var dd = new HeroSkillPropAccumulateData();
                dd.damage = devourDamage;
                // DevDebug.LogError("吞噬伤害"+devourDamage);
                ECB.AppendToBuffer<HeroSkillPropAccumulateData>(index, target, dd);

                //写回清除指令
                debuff.toClearDotBuffer = true;
                ECB.SetComponent(index, target, debuff);
                //写回pool 特效处理，《这里可以添加吞噬效果》,1、关闭现有的池化效果 2、后续开启吞噬效果,这里是管用的
                //但是目前伤害的触发 很难观察
                monsterPool.fireActive = 0;
                monsterPool.frostActive = 0;
                monsterPool.lightningActive = 0;
                monsterPool.shadowActive = 0;
                monsterPool.poisonActive = 0;
                monsterPool.bleedActive = 0;
                ECB.SetComponent(index, target, monsterPool);

            }
         
        }
    }

}