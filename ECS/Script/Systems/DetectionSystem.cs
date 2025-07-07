using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
#if UNITY_EDITOR
using UnityEditor.Search;
#endif

namespace BlackDawn.DOTS
{
    /// <summary>
    /// 负责 Detection_DefaultCmpt 的触发检测，
    /// 并为每个实体从其 NearbyHit buffer 中选出最近的目标
    /// 侦测系统最先运行
    /// </summary>
    [RequireMatchingQueriesForUpdate]
    //在行动系统之前进行侦测
    [UpdateBefore(typeof(ActionSystem))]
    [UpdateInGroup(typeof(ActionSystemGroup))]
    [BurstCompile]
    public partial struct DetectionSystem : ISystem
    {
        private int batchSize;
        //雷霆之握 技能标签
        public bool enableSpecialSkillThunderGrip;
        //连锁吞噬 技能标签
        public bool enableSpecialSkillChainDevour;



        // 每帧要更新的查找
        private ComponentLookup<Detection_DefaultCmpt> _detectionLookup;
        private ComponentLookup<LiveMonster> _liveMonsterLookup;
        private ComponentLookup<LocalTransform> _transformLookup;
        private BufferLookup<NearbyHit> _hitBufferLookup;
        //额外添加的碰撞对查找
        private ComponentLookup<EnemyFlightProp> _enemyFlightPropLookup;
        private ComponentLookup<FlightPropDamageCalPar> _flightPropDamageCalParLookup;
        private ComponentLookup<SkillsDamageCalPar> _skillsDamageCalParLookup;
        private ComponentLookup<SkillsOverTimeDamageCalPar> _skillsOverTimeDamageCalParLookup;
        private ComponentLookup<HeroEntityMasterTag> _heroEntityMasterTagLookup;
        private ComponentLookup<SkillArcaneCircleSecondTag> _skillArcaneCircleSecondTagLookup;
        private ComponentLookup<SkillArcaneCircleTag> _skillArcaneCircleTagLookup;
        private ComponentLookup<SkillElementResonanceTag> _skillElementResonanceTagLookup;
        private ComponentLookup<SkillMineBlastTag> _skillMineBlastTagLookup;
        private ComponentLookup<SkillMineBlastExplosionTag> _skillMineExplosionTagLookup;
        private ComponentLookup<SkillPoisonRainATag> _skillPoisonRainATagLookup;
        private ComponentLookup<SkillThunderGripTag> _skillThunderGripTagLookup;
        private ComponentLookup<SkillChainDevourTag> _skillChainDevourTagLookup;


        // 所有用于分类的碰撞对容器
        private NativeQueue<TriggerPairData> _heroHitMonster;
        private NativeQueue<TriggerPairData> _enemyFlightHitHero;
        private NativeQueue<TriggerPairData> _flightHitMonster;
        private NativeQueue<TriggerPairData> _skillHitMonster;
        private NativeQueue<TriggerPairData> _skillOverTimeHitMonster;
        private NativeQueue<TriggerPairData> _arcaneCircleHitMonster;
        private NativeQueue<TriggerPairData> _arcaneCircleHitHero;
        private NativeQueue<TriggerPairData> _skillElementResonance;
        private NativeQueue<TriggerPairData> _basePropElementResonance;
        private NativeQueue<TriggerPairData> _combinedElementResonance;
        private NativeQueue<TriggerPairData> _mineBlastHitMonster;
        private NativeQueue<TriggerPairData> _mineBlastExplosionHitMonster;
        private NativeQueue<TriggerPairData> _poisonRainAHitMonster;

        private NativeQueue<TriggerPairData> _thunderGripHitMonster; // 雷霆之握技能碰撞对
        private NativeQueue<TriggerPairData> _chainDevourHitMonster;//连锁吞噬技能碰撞对


        //用于在job中并行的array
        public NativeArray<TriggerPairData> heroHitMonsterArray;
        public NativeArray<TriggerPairData> enemyFlightHitHeroArray;
        public NativeArray<TriggerPairData> flightHitMonsterArray;
        public NativeArray<TriggerPairData> skillHitMonsterArray;
        public NativeArray<TriggerPairData> skillOverTimeHitMonsterArray;
        public NativeArray<TriggerPairData> arcaneCircleHitMonsterArray;
        public NativeArray<TriggerPairData> arcaneCircleHitHeroArray;
        public NativeArray<TriggerPairData> skillElementResonanceArray;
        public NativeArray<TriggerPairData> basePropElementResonanceArray;
        public NativeArray<TriggerPairData> combinedElementResonanceArray; // 如需后续使用
        private NativeParallelHashMap<Entity, byte> _resonanceMap;
        public NativeArray<TriggerPairData> mineBlastHitMonsterArray;
        public NativeArray<TriggerPairData> mineBlastExplosionHitMonsterArray;
        public NativeArray<TriggerPairData> posionRainAHitMonsterArray;
        
        public NativeArray<TriggerPairData> thunderGripHitMonsterArray; // 雷霆之握技能碰撞对数组

        public NativeArray<TriggerPairData> chainDevourHitMonsterArray;// 连锁吞噬技能碰撞对数组


        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableDetectionSystemTag>();

            _detectionLookup = SystemAPI.GetComponentLookup<Detection_DefaultCmpt>(true);
            _liveMonsterLookup = SystemAPI.GetComponentLookup<LiveMonster>(true);
            _transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            _hitBufferLookup = SystemAPI.GetBufferLookup<NearbyHit>(false);

            //额外添加的碰撞对查找
            _enemyFlightPropLookup = SystemAPI.GetComponentLookup<EnemyFlightProp>(true);
            _flightPropDamageCalParLookup = SystemAPI.GetComponentLookup<FlightPropDamageCalPar>(true);
            _skillsDamageCalParLookup = SystemAPI.GetComponentLookup<SkillsDamageCalPar>(true);
            _skillsOverTimeDamageCalParLookup = SystemAPI.GetComponentLookup<SkillsOverTimeDamageCalPar>(true);

            _heroEntityMasterTagLookup = SystemAPI.GetComponentLookup<HeroEntityMasterTag>(true);
            _skillArcaneCircleSecondTagLookup = SystemAPI.GetComponentLookup<SkillArcaneCircleSecondTag>(true);
            _skillArcaneCircleTagLookup = SystemAPI.GetComponentLookup<SkillArcaneCircleTag>(true);
            _skillElementResonanceTagLookup = SystemAPI.GetComponentLookup<SkillElementResonanceTag>(true);
            _skillMineBlastTagLookup = SystemAPI.GetComponentLookup<SkillMineBlastTag>(true);
            _skillMineExplosionTagLookup = SystemAPI.GetComponentLookup<SkillMineBlastExplosionTag>(true);
            _skillPoisonRainATagLookup = SystemAPI.GetComponentLookup<SkillPoisonRainATag>(true);
            //雷霆之握技能标签
            _skillThunderGripTagLookup = SystemAPI.GetComponentLookup<SkillThunderGripTag>(true);
            //连锁吞噬技能标签
            _skillChainDevourTagLookup = SystemAPI.GetComponentLookup<SkillChainDevourTag>(true);


            batchSize = UnityEngine.SystemInfo.processorCount > 8 ? 64 : 32;

            _heroHitMonster = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _enemyFlightHitHero = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _flightHitMonster = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _skillHitMonster = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _skillOverTimeHitMonster = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _arcaneCircleHitMonster = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _arcaneCircleHitHero = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _skillElementResonance = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _basePropElementResonance = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _combinedElementResonance = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _resonanceMap = new NativeParallelHashMap<Entity, byte>(1024, Allocator.Persistent); // 容量可根据场景调整
            _mineBlastHitMonster = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _mineBlastExplosionHitMonster = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _poisonRainAHitMonster = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _thunderGripHitMonster = new NativeQueue<TriggerPairData>(Allocator.Persistent);
            _chainDevourHitMonster = new NativeQueue<TriggerPairData>(Allocator.Persistent);


             // enableSpecialSkillThunderGrip =true;
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

            //  DetectionTriggerJob._enqueueCounter = 0;


            // 更新查找
            _detectionLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _liveMonsterLookup.Update(ref state);
            _hitBufferLookup.Update(ref state);

            _enemyFlightPropLookup.Update(ref state);
            _flightPropDamageCalParLookup.Update(ref state);
            _skillsDamageCalParLookup.Update(ref state);
            _skillsOverTimeDamageCalParLookup.Update(ref state);
            _heroEntityMasterTagLookup.Update(ref state);
            _skillArcaneCircleSecondTagLookup.Update(ref state);
            _skillArcaneCircleTagLookup.Update(ref state);
            _skillElementResonanceTagLookup.Update(ref state);
            _skillMineBlastTagLookup.Update(ref state);
            _skillMineExplosionTagLookup.Update(ref state);
            _skillPoisonRainATagLookup.Update(ref state);
            _skillThunderGripTagLookup.Update(ref state);
            _skillChainDevourTagLookup.Update(ref state);
            //清空区
            _heroHitMonster.Clear();
            _enemyFlightHitHero.Clear();
            _flightHitMonster.Clear();
            _skillHitMonster.Clear();
            _skillOverTimeHitMonster.Clear();
            //法阵技能二阶
            _arcaneCircleHitMonster.Clear();
            _arcaneCircleHitHero.Clear();
            //元素共鸣,下面两个使用的新收集方法
            _skillElementResonance.Clear();
            _basePropElementResonance.Clear();
            _combinedElementResonance.Clear();
            _resonanceMap.Clear();
            //毒爆地雷
            _mineBlastHitMonster.Clear();
            _mineBlastExplosionHitMonster.Clear();
            //毒雨A
            _poisonRainAHitMonster.Clear();
            //雷霆之握 原始队列
            _thunderGripHitMonster.Clear();
            //连锁吞噬 原始队列
            _chainDevourHitMonster.Clear();

            //释放所有碰撞数组内存
            DisposeArrayForCollison();

            var heroHitMonsterQueue = _heroHitMonster.AsParallelWriter();
            var enemyFlightHitHeroQueue = _enemyFlightHitHero.AsParallelWriter();
            var flightHitMonsterQueue = _flightHitMonster.AsParallelWriter();
            var skillHitMonsterQueue = _skillHitMonster.AsParallelWriter();
            var skillOverTimeHitMonsterQueue = _skillOverTimeHitMonster.AsParallelWriter();
            var arcaneCircleHitMonsterQueue = _arcaneCircleHitMonster.AsParallelWriter();
            var arcaneCircleHitHeroQueue = _arcaneCircleHitHero.AsParallelWriter();
            var skillElementResonanceQueue = _skillElementResonance.AsParallelWriter();
            var baseFlightElementResonanceQueue = _basePropElementResonance.AsParallelWriter();
            var combinedElementResonanceQueue = _combinedElementResonance.AsParallelWriter();
            var mineBlastHitMonsterQueue = _mineBlastHitMonster.AsParallelWriter();
            var mineBlastExplosionHitMonsterQueue = _mineBlastExplosionHitMonster.AsParallelWriter();
            var poisonRainAHitMonsterQueue = _poisonRainAHitMonster.AsParallelWriter();
            var thunderGripHitMonsterQueue = _thunderGripHitMonster.AsParallelWriter(); // 雷霆之握技能碰撞对
            var chainDevourHitMonsterQueue = _chainDevourHitMonster.AsParallelWriter(); //连锁吞噬技能碰撞对


            // 1. 收集触发：把所有碰撞写入自己实体的 buffer,收集碰撞对的标准并行方式
            var sim = SystemAPI.GetSingleton<SimulationSingleton>();
            state.Dependency = new DetectionTriggerJob
            {
                DetectionLookup = _detectionLookup,
                TransformLookup = _transformLookup,
                HitBufferLookup = _hitBufferLookup,
                LiveMonsterLookup = _liveMonsterLookup,
                EnemyFlightPropLookup = _enemyFlightPropLookup,
                FlightPropDamageCalParLookup = _flightPropDamageCalParLookup,
                SkillPropDamageCalParLookup = _skillsDamageCalParLookup,
                SkillOverTimePropDamageCalParLookup = _skillsOverTimeDamageCalParLookup,
                HeroEntityMasterTagLookup = _heroEntityMasterTagLookup,

                //特殊瞬时碰撞技能区域
                EnableSpecialSkillThunderGrip = enableSpecialSkillThunderGrip,
                SkillThunderGripTagLookup = _skillThunderGripTagLookup, // 雷霆之握技能标签
                ThunderGripHitMonsterQueue = thunderGripHitMonsterQueue, // 雷霆之握技能碰撞对

                EnableSpecialSkillChainDevour = enableSpecialSkillChainDevour,
                SkillChainDevourTagLookup = _skillChainDevourTagLookup,// 连锁吞噬 技能标签
                ChainDevourHitMonsterQueue = chainDevourHitMonsterQueue, //连锁吞噬技能碰撞对

                //end




                SkillArcaneCircleSecondTagLookup = _skillArcaneCircleSecondTagLookup,
                SkillArcaneCircleTagLookup = _skillArcaneCircleTagLookup,
                SkillElementResonanceTagLookup = _skillElementResonanceTagLookup,
                SkillMineBlastTagLookup = _skillMineBlastTagLookup,
                SkillMineBlastExplosionTagLookup = _skillMineExplosionTagLookup,
                SkillPoisonRainATaglookup = _skillPoisonRainATagLookup,

                HeroHitMonsterQueue = heroHitMonsterQueue,
                EnemyFlightHitHeroQueue = enemyFlightHitHeroQueue,
                FlightHitMonsterQueue = flightHitMonsterQueue,
                SkillHitMonsterQueue = skillHitMonsterQueue,
                SkillOverTimeHitMonsterQueue = skillOverTimeHitMonsterQueue,
                ArcaneCircleHitMonsterQueue = arcaneCircleHitMonsterQueue,
                ArcaneCircleHitHeroQueue = arcaneCircleHitHeroQueue,
                SkillElementResonanceQueue = skillElementResonanceQueue,
                BaseFlightElementResonanceQueue = baseFlightElementResonanceQueue,
                CombinedElementResonanceQueue = combinedElementResonanceQueue,
                ResonanceMap = _resonanceMap,
                //传入毒爆地雷
                MineBlastHitMonsterQueue = mineBlastHitMonsterQueue,
                MineBlastExplosionHitMonsterQueue = mineBlastExplosionHitMonsterQueue,
                //传入毒雨A阶段
                PoisonRainAHitMonsterQueue = poisonRainAHitMonsterQueue,




            }.Schedule(sim, state.Dependency);

            // 等待收集完成，这里收集原始碰撞数据，要等待完成
            state.Dependency.Complete();
            //这里会分配新的内存， 所以需要在开始释放
            heroHitMonsterArray = _heroHitMonster.ToArray(Allocator.Persistent);
            enemyFlightHitHeroArray = _enemyFlightHitHero.ToArray(Allocator.Persistent);
            flightHitMonsterArray = _flightHitMonster.ToArray(Allocator.Persistent);
            skillHitMonsterArray = _skillHitMonster.ToArray(Allocator.Persistent);
            skillOverTimeHitMonsterArray = _skillOverTimeHitMonster.ToArray(Allocator.Persistent);
            arcaneCircleHitMonsterArray = _arcaneCircleHitMonster.ToArray(Allocator.Persistent);
            arcaneCircleHitHeroArray = _arcaneCircleHitHero.ToArray(Allocator.Persistent);
            skillElementResonanceArray = _skillElementResonance.ToArray(Allocator.Persistent);
            basePropElementResonanceArray = _basePropElementResonance.ToArray(Allocator.Persistent);
            combinedElementResonanceArray = _combinedElementResonance.ToArray(Allocator.Persistent);
            mineBlastHitMonsterArray = _mineBlastHitMonster.ToArray(Allocator.Persistent);
            mineBlastExplosionHitMonsterArray = _mineBlastExplosionHitMonster.ToArray(Allocator.Persistent);
            posionRainAHitMonsterArray = _poisonRainAHitMonster.ToArray(Allocator.Persistent);
            thunderGripHitMonsterArray = _thunderGripHitMonster.ToArray(Allocator.Persistent); // 雷霆之握技能碰撞对数组
            chainDevourHitMonsterArray = _chainDevourHitMonster.ToArray(Allocator.Persistent);// 连锁吞噬技能碰撞对数组


            //   if(chainDevourHitMonsterArray.Length>0)
            // DevDebug.LogError("连锁吞噬碰撞对长度" + chainDevourHitMonsterArray.Length);
            //if (heroHitMonsterArray.Length > 0)
            //    DevDebug.Log("event 英雄碰到怪物数量" + heroHitMonsterArray.Length);

            // DevDebug.LogError("元素共鸣结构长度" + combinedElementResonanceArray.Length +"命中共鸣体的基础子弹长度"+basePropElementResonanceArray.Length);

            // DevDebug.LogError(arcaneCircleHitMonsterArray.Length);
            //  CheckNumberOfDetection(ref state);



        }



        void CheckNumberOfDetection(ref SystemState state)
        {

            state.Dependency.Complete();
            {
                // 创建一个只包含 Detection_DefaultCmpt + NearbyHit buffer 的查询
                var query = state.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<HeroAttackTarget>(),
                    ComponentType.ReadOnly<NearbyHit>()
                );

                // 拿到所有符合条件的实体
                using var entities = query.ToEntityArray(Allocator.Temp);
                // hitBufferLookup 已经在前面 Update 过了

                foreach (var e in entities)
                {
                    // 直接通过 BufferLookup 取出 buffer，再读 Length
                    var buf = _hitBufferLookup[e];
                    UnityEngine.Debug.Log($"实体 {e.Index} 本帧命中次数：{buf.Length}");
                }
            }
        }
        public void OnDestroy(ref SystemState state)
        {
            //释放所有队列内存
            _heroHitMonster.Dispose();
            _enemyFlightHitHero.Dispose();
            _flightHitMonster.Dispose();
            _skillHitMonster.Dispose();
            _arcaneCircleHitMonster.Dispose();
            _arcaneCircleHitHero.Dispose();
            _skillElementResonance.Dispose();
            _basePropElementResonance.Dispose();
            _combinedElementResonance.Dispose();
            _mineBlastHitMonster.Dispose();
            _thunderGripHitMonster.Dispose();
            _chainDevourHitMonster.Dispose();
            



        }


        void DisposeArrayForCollison()
        {
            //施放所有碰撞数组内存
            if (heroHitMonsterArray.IsCreated) heroHitMonsterArray.Dispose();
            if (enemyFlightHitHeroArray.IsCreated) enemyFlightHitHeroArray.Dispose();
            if (flightHitMonsterArray.IsCreated) flightHitMonsterArray.Dispose();
            if (skillHitMonsterArray.IsCreated) skillHitMonsterArray.Dispose();
            if (skillOverTimeHitMonsterArray.IsCreated) skillOverTimeHitMonsterArray.Dispose();
            if (arcaneCircleHitMonsterArray.IsCreated) arcaneCircleHitMonsterArray.Dispose();
            if (arcaneCircleHitHeroArray.IsCreated) arcaneCircleHitHeroArray.Dispose();
            if (skillElementResonanceArray.IsCreated) skillElementResonanceArray.Dispose();
            if (basePropElementResonanceArray.IsCreated) basePropElementResonanceArray.Dispose();
            if (combinedElementResonanceArray.IsCreated) combinedElementResonanceArray.Dispose();
            if (mineBlastHitMonsterArray.IsCreated) mineBlastHitMonsterArray.Dispose();
            if (mineBlastExplosionHitMonsterArray.IsCreated) mineBlastExplosionHitMonsterArray.Dispose();
            if (posionRainAHitMonsterArray.IsCreated) posionRainAHitMonsterArray.Dispose();
            if (thunderGripHitMonsterArray.IsCreated) thunderGripHitMonsterArray.Dispose();
            if (chainDevourHitMonsterArray.IsCreated) chainDevourHitMonsterArray.Dispose();



        }
    }
    /// <summary>
    /// 综合碰撞对收集
    /// </summary>
    [BurstCompile]
  public  struct DetectionTriggerJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<Detection_DefaultCmpt> DetectionLookup; // 基础检测组件
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup; // 用于计算距离
        [ReadOnly] public ComponentLookup<LiveMonster> LiveMonsterLookup; // 怪物存活状态

        [ReadOnly] public ComponentLookup<EnemyFlightProp> EnemyFlightPropLookup; // 怪物飞行道具
        [ReadOnly] public ComponentLookup<FlightPropDamageCalPar> FlightPropDamageCalParLookup; // 基础飞行道具
        [ReadOnly] public ComponentLookup<SkillsDamageCalPar> SkillPropDamageCalParLookup; // 技能飞行道具
        [ReadOnly] public ComponentLookup<SkillsOverTimeDamageCalPar> SkillOverTimePropDamageCalParLookup; // 技能飞行道具
        [ReadOnly] public ComponentLookup<HeroEntityMasterTag> HeroEntityMasterTagLookup; // 英雄主体
        [ReadOnly] public ComponentLookup<SkillArcaneCircleTag> SkillArcaneCircleTagLookup; // 技能法阵第一阶段
        [ReadOnly] public ComponentLookup<SkillArcaneCircleSecondTag> SkillArcaneCircleSecondTagLookup; // 技能法阵第二阶段
        [ReadOnly] public ComponentLookup<SkillElementResonanceTag> SkillElementResonanceTagLookup;//元素共鸣体
        [ReadOnly] public ComponentLookup<SkillMineBlastTag> SkillMineBlastTagLookup;//毒爆地雷
        [ReadOnly] public ComponentLookup<SkillMineBlastExplosionTag> SkillMineBlastExplosionTagLookup;//爆炸后的瘟疫地雷
        [ReadOnly] public ComponentLookup<SkillPoisonRainATag> SkillPoisonRainATaglookup;//毒雨造成的伤害加深
       
        [ReadOnly] public bool EnableSpecialSkillThunderGrip; // 是否启用雷霆之握技能
        [ReadOnly] public ComponentLookup<SkillThunderGripTag> SkillThunderGripTagLookup; // 雷霆之握技能标签

        [ReadOnly] public bool EnableSpecialSkillChainDevour;//是否启用连锁吞噬技能
        [ReadOnly] public ComponentLookup<SkillChainDevourTag> SkillChainDevourTagLookup;//联锁吞噬技能标签
        
        public BufferLookup<NearbyHit> HitBufferLookup; // 基础检测系统
        

        public NativeQueue<TriggerPairData>.ParallelWriter HeroHitMonsterQueue;
        public NativeQueue<TriggerPairData>.ParallelWriter EnemyFlightHitHeroQueue;
        public NativeQueue<TriggerPairData>.ParallelWriter FlightHitMonsterQueue;
        public NativeQueue<TriggerPairData>.ParallelWriter SkillHitMonsterQueue;
        public NativeQueue<TriggerPairData>.ParallelWriter SkillOverTimeHitMonsterQueue;
        public NativeQueue<TriggerPairData>.ParallelWriter ArcaneCircleHitMonsterQueue;
        public NativeQueue<TriggerPairData>.ParallelWriter ArcaneCircleHitHeroQueue;
        public NativeQueue<TriggerPairData>.ParallelWriter SkillElementResonanceQueue;
        public NativeQueue<TriggerPairData>.ParallelWriter BaseFlightElementResonanceQueue;

        // 新增：单一综合队列
        // 收集：基础飞行道具 & 技能，同步撞到怪物并撞到元素共鸣体
        public NativeQueue<TriggerPairData>.ParallelWriter CombinedElementResonanceQueue;

        // 新增：并行标记表，标记已撞元素共鸣体的攻击者
        public NativeParallelHashMap<Entity, byte> ResonanceMap;

        //毒爆地雷
        public NativeQueue<TriggerPairData>.ParallelWriter MineBlastHitMonsterQueue;
        //毒爆地雷爆炸后的瘟疫地雷
        public NativeQueue<TriggerPairData>.ParallelWriter MineBlastExplosionHitMonsterQueue;
        //毒雨 
        public NativeQueue<TriggerPairData>.ParallelWriter PoisonRainAHitMonsterQueue;

        //雷霆之握技能碰撞对
        public NativeQueue<TriggerPairData>.ParallelWriter ThunderGripHitMonsterQueue; // 雷霆之握技能碰撞对 
        //连锁吞噬技能碰撞对
        public NativeQueue<TriggerPairData>.ParallelWriter ChainDevourHitMonsterQueue;// 连锁吞噬技能碰撞对


        public void Execute(TriggerEvent triggerEvent)
        {
            var a = triggerEvent.EntityA;
            var b = triggerEvent.EntityB;

             //DevDebug.LogError($"enttyA:{a.Index} entityB{b.Index} ");


            // // 2) 处理元素共鸣同时碰撞的效果
            //ProcessCombined(a, b);

            // // 分类写入碰撞对容器
            AddIfMatch(a, b, HeroEntityMasterTagLookup, LiveMonsterLookup, HeroHitMonsterQueue, true);
            // AddIfMatch(b, a, HeroEntityMasterTagLookup, LiveMonsterLookup, HeroHitMonsterQueue, true);

            AddIfMatch(a, b, EnemyFlightPropLookup, HeroEntityMasterTagLookup, EnemyFlightHitHeroQueue, false);
            AddIfMatch(b, a, EnemyFlightPropLookup, HeroEntityMasterTagLookup, EnemyFlightHitHeroQueue, false);

            AddIfMatch(a, b, FlightPropDamageCalParLookup, LiveMonsterLookup, FlightHitMonsterQueue, true);
            // AddIfMatch(b, a, FlightPropDamageCalParLookup, LiveMonsterLookup, FlightHitMonsterQueue, true);

            //瞬时类技能
            AddIfMatch(a, b, SkillPropDamageCalParLookup, LiveMonsterLookup, SkillHitMonsterQueue, true);
            // AddIfMatch(b, a, SkillPropDamageCalParLookup, LiveMonsterLookup, SkillHitMonsterQueue, true);



            //雷霆之握（捕捉活体),技能系统转入控制标签
            if (EnableSpecialSkillThunderGrip)
                AddIfMatch(a, b, SkillThunderGripTagLookup, LiveMonsterLookup, ThunderGripHitMonsterQueue, true);
            //连锁吞噬 技能传入 控制标签，后期后话
            if (EnableSpecialSkillChainDevour)
                AddIfMatch(a, b, SkillChainDevourTagLookup, LiveMonsterLookup, ChainDevourHitMonsterQueue, true);







            //持续性技能
            // AddIfMatch(a, b, SkillOverTimePropDamageCalParLookup, LiveMonsterLookup, SkillOverTimeHitMonsterQueue, true);
            // AddIfMatch(b, a, SkillPropDamageCalParLookup, LiveMonsterLookup, SkillHitMonsterQueue, true);

                // //怪物与法阵碰撞
                // AddIfMatch(a, b, SkillArcaneCircleSecondTagLookup, LiveMonsterLookup, ArcaneCircleHitMonsterQueue, true);
                // AddIfMatch(b, a, SkillArcaneCircleSecondTagLookup, LiveMonsterLookup, ArcaneCircleHitMonsterQueue, true);

                // //英雄与法阵本体碰撞,英雄可以在阶段内自行判断
                // AddIfMatchSimple(a, b, HeroEntityMasterTagLookup, SkillArcaneCircleTagLookup, ArcaneCircleHitHeroQueue);
                // AddIfMatchSimple(b, a, HeroEntityMasterTagLookup, SkillArcaneCircleTagLookup, ArcaneCircleHitHeroQueue);

                // //技能碰撞到元素共鸣体,元素共鸣体自身不检测自己
                // AddIfMatchSimple(a, b, SkillElementResonanceTagLookup, SkillPropDamageCalParLookup, SkillElementResonanceQueue);
                // AddIfMatchSimple(b, a, SkillElementResonanceTagLookup, SkillPropDamageCalParLookup, SkillElementResonanceQueue);

                // //基础飞行道具碰撞到元素共鸣体
                // AddIfMatchSimple(a, b, SkillElementResonanceTagLookup, FlightPropDamageCalParLookup, BaseFlightElementResonanceQueue);
                // AddIfMatchSimple(b, a, SkillElementResonanceTagLookup, FlightPropDamageCalParLookup, BaseFlightElementResonanceQueue);

                // //怪物与毒爆地雷碰撞
                // AddIfMatch(a, b, SkillMineBlastTagLookup, LiveMonsterLookup, MineBlastHitMonsterQueue, true);
                // AddIfMatch(b, a, SkillMineBlastTagLookup, LiveMonsterLookup, MineBlastHitMonsterQueue, true);
                // //怪物与毒爆地雷爆炸后的碰撞对，用于计算B效果
                // AddIfMatch(a, b, SkillMineBlastExplosionTagLookup, LiveMonsterLookup, MineBlastExplosionHitMonsterQueue, true);
                // AddIfMatch(b, a, SkillMineBlastExplosionTagLookup, LiveMonsterLookup, MineBlastExplosionHitMonsterQueue, true);
                // //毒雨A阶段
                // AddIfMatch(a, b, SkillPoisonRainATaglookup, LiveMonsterLookup, PoisonRainAHitMonsterQueue, true);
                // AddIfMatch(b, a, SkillPoisonRainATaglookup, LiveMonsterLookup, PoisonRainAHitMonsterQueue, true);


                // AddIfMatchSingle(a, b, SkillPoisonRainATaglookup, LiveMonsterLookup, PoisonRainAHitMonsterQueue, true);


        }

        private void CheckAndAddNearbyHit(Entity detector, Entity other)
        {
            if (!DetectionLookup.HasComponent(detector) ||
                !TransformLookup.HasComponent(detector) ||
                !TransformLookup.HasComponent(other) ||
                !LiveMonsterLookup.HasComponent(other) ||
                !LiveMonsterLookup.IsComponentEnabled(other))
                return;

            float d = math.distancesq(
                TransformLookup[detector].Position,
                TransformLookup[other].Position);

            var detection = DetectionLookup[detector];
            Entity bufferTarget = detection.bufferOwner;

            if (HitBufferLookup.HasBuffer(bufferTarget))
            {
                var buf = HitBufferLookup[bufferTarget];
                if (buf.Length < buf.Capacity)
                    buf.Add(new NearbyHit { other = other, sqrDist = d });
            }
        }

      // public static int _enqueueCounter = 0;

        private void AddIfMatch<TA, TB>(
            Entity a, Entity b,
            ComponentLookup<TA> lookupA,
            ComponentLookup<TB> lookupB,
            NativeQueue<TriggerPairData>.ParallelWriter queue,
            bool checkLiveMonster)
            where TA : unmanaged, IComponentData
            where TB : unmanaged, IComponentData
        {
           
            if (lookupA.HasComponent(a) && lookupB.HasComponent(b))
            {
               
                if (!checkLiveMonster || LiveMonsterLookup.IsComponentEnabled(b))
                {
                    //int currentIdx = System.Threading.Interlocked.Increment(ref _enqueueCounter);
                    // DevDebug.LogError($"入队第次：a={a.Index}, b={b.Index}");
                   // DevDebug.LogError($"加入内部碰撞enttyA:{a.Index} entityB{b.Index} ");
                    queue.Enqueue(new TriggerPairData { EntityA = a, EntityB = b });
                }
            }
        }
        private void AddIfMatchSingle<TA, TB>(
    Entity a, Entity b,
    ComponentLookup<TA> lookupA,
    ComponentLookup<TB> lookupB,
    NativeQueue<TriggerPairData>.ParallelWriter queue,
    bool checkLiveMonster)
    where TA : unmanaged, IComponentData
    where TB : unmanaged, IComponentData
        {
            // 检查 a→b 或 b→a 哪一种是我们要的组合
            bool isAB = lookupA.HasComponent(a) && lookupB.HasComponent(b);
            bool isBA = lookupA.HasComponent(b) && lookupB.HasComponent(a);
            if (!isAB && !isBA)
                return; // 两种情况都不符合，直接跳过

            // 统一“攻击者→目标”的顺序
            Entity attacker = isAB ? a : b;
            Entity target = isAB ? b : a;

            // 如果需要判断存活状态，放到这里
            if (checkLiveMonster && !LiveMonsterLookup.IsComponentEnabled(target))
                return;

            // 最终只入一次队
            queue.Enqueue(new TriggerPairData { EntityA = attacker, EntityB = target });
        }

        private void AddIfMatchSimple<TA, TB>(
    Entity a, Entity b,
    ComponentLookup<TA> lookupA,
    ComponentLookup<TB> lookupB,
    NativeQueue<TriggerPairData>.ParallelWriter queue)
    where TA : unmanaged, IComponentData
    where TB : unmanaged, IComponentData
        {
            if (lookupA.HasComponent(a) && lookupB.HasComponent(b))
            {
                queue.Enqueue(new TriggerPairData { EntityA = a, EntityB = b });
            }
        }

        // 将标记与分类+综合输出封装
        void ProcessCombined(Entity a, Entity b)
        {
            // 标记：当道具或技能与元素共鸣体碰撞时
            TryMark(a, b);
            TryMark(b, a);

            // 飞行道具 ↔ 怪物
            TryEnqueueAndCombine(a, b,
                FlightPropDamageCalParLookup,
                FlightHitMonsterQueue);
            TryEnqueueAndCombine(b, a,
                FlightPropDamageCalParLookup,
                FlightHitMonsterQueue);

            // 技能道具 ↔ 怪物
            TryEnqueueAndCombine(a, b,
                SkillPropDamageCalParLookup,
                SkillHitMonsterQueue);
            TryEnqueueAndCombine(b, a,
                SkillPropDamageCalParLookup,
                SkillHitMonsterQueue);
        }

        // 如果 a 是共鸣体、b 是道具或技能，则标记 b
        void TryMark(Entity a, Entity b)
        {
            if (!SkillElementResonanceTagLookup.HasComponent(a))
                return;
            if (FlightPropDamageCalParLookup.HasComponent(b) || SkillPropDamageCalParLookup.HasComponent(b))
                ResonanceMap.TryAdd(b, 1);
        }

        // 分类写入原始队列，并在已标记时写入综合队列
        void TryEnqueueAndCombine<T>(
            Entity a,
            Entity b,
            ComponentLookup<T> lookup,
            NativeQueue<TriggerPairData>.ParallelWriter queue)
            where T : unmanaged, IComponentData
        {
            if (lookup.HasComponent(a)
                && LiveMonsterLookup.HasComponent(b)
                && LiveMonsterLookup.IsComponentEnabled(b))
            {
                var pair = new TriggerPairData { EntityA = a, EntityB = b };
                //这里的幽灵BUG
               // queue.Enqueue(pair);
                if (ResonanceMap.ContainsKey(a))
                    CombinedElementResonanceQueue.Enqueue(pair);
            }
        }






    }


    





    
}
