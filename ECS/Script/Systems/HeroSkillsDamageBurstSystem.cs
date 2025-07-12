using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Profiling;
using Unity.Transforms;
using UnityEngine;



namespace BlackDawn.DOTS
{


    /// <summary>
    /// 处理爆发性技能，技能标签由初始伤害标签继承，增加独立的特定标签计算
    /// </summary>
    [RequireMatchingQueriesForUpdate]   // 若所有 Query 都匹配不到实体，则系统永不执行
    [UpdateInGroup(typeof(ActionSystemGroup))]
    [UpdateAfter(typeof(HeroSkillsDamageOverTimeSystem))]
    [BurstCompile]
    public partial struct HeroSkillsDamageBurstSystem : ISystem
    {
        //外部控制job 元素爆发第二阶段的job开启   
        public bool enableHeroSkillElementBurstB;
     
        private ComponentLookup<LiveMonster> _liveMonster;
        private ComponentLookup<MonsterDefenseAttribute> _monsterDefenseAttrLookup;
        private ComponentLookup<MonsterLossPoolAttribute> _monsterLossPoolAttrLookup;
        private ComponentLookup<MonsterControlledEffectAttribute> _monsterControlledEffectAttrLookup;
        private ComponentLookup<HeroAttributeCmpt> _heroAttrLookup;
        private ComponentLookup<SkillsBurstDamageCalPar> _skillBurstDamage;
        private BufferLookup<HitRecord> _hitRecordBufferLookup;
        private ComponentLookup<LocalTransform> _transform;
        private BufferLookup<LinkedEntityGroup> _linkedLookup;
        private ComponentLookup<MonsterTempDamageText> _monsterTempDamageTextLookup;
        private ComponentLookup<MonsterTempDotDamageText> _monsterTempDotDamageTextLookup;
        //debuffer伤害组件查询
        private ComponentLookup<MonsterDebuffAttribute> _monsterDebuffAttrLookup;
        //dot组件查询
        private BufferLookup<MonsterDotDamageBuffer> _monsterDotDamageBufferLookup;
        //元素爆发技能标签查询
        private ComponentLookup<SkillElementBurstTag> _skillElementBurstLookup;
        //侦测系统缓存
        private SystemHandle _detectionSystemHandle;
        //持续性overLap检测系统缓存
        private SystemHandle _overlapDetectionSystemHandle;



        void OnCreate(ref SystemState state)

        {
            //外部控制更新
            state.RequireForUpdate<EnableHeroSkillsDamageBurstSystemTag>();


            _liveMonster = SystemAPI.GetComponentLookup<LiveMonster>(true);
            _monsterDefenseAttrLookup = SystemAPI.GetComponentLookup<MonsterDefenseAttribute>(true);
            _monsterLossPoolAttrLookup = SystemAPI.GetComponentLookup<MonsterLossPoolAttribute>(true);
            _monsterControlledEffectAttrLookup = SystemAPI.GetComponentLookup<MonsterControlledEffectAttribute>(true);
            _heroAttrLookup = SystemAPI.GetComponentLookup<HeroAttributeCmpt>(true);
            _hitRecordBufferLookup = SystemAPI.GetBufferLookup<HitRecord>(true);
            _skillBurstDamage = SystemAPI.GetComponentLookup<SkillsBurstDamageCalPar>(true);
            _linkedLookup = SystemAPI.GetBufferLookup<LinkedEntityGroup>(true);
            _transform = SystemAPI.GetComponentLookup<LocalTransform>(true);
            _monsterTempDamageTextLookup = SystemAPI.GetComponentLookup<MonsterTempDamageText>(true);
            _monsterTempDotDamageTextLookup = SystemAPI.GetComponentLookup<MonsterTempDotDamageText>(false);
            _monsterDebuffAttrLookup = SystemAPI.GetComponentLookup<MonsterDebuffAttribute>(true);
            _monsterDotDamageBufferLookup = SystemAPI.GetBufferLookup<MonsterDotDamageBuffer>(true);
            _skillElementBurstLookup = SystemAPI.GetComponentLookup<SkillElementBurstTag>(true);


            _detectionSystemHandle = state.WorldUnmanaged.GetExistingUnmanagedSystem<DetectionSystem>();
            _overlapDetectionSystemHandle = state.WorldUnmanaged.GetExistingUnmanagedSystem<OverlapDetectionSystem>();

        }
        [BurstCompile]

        void OnUpdate(ref SystemState state)
        {
            //更新相关参数
            _monsterDefenseAttrLookup.Update(ref state);
            _liveMonster.Update(ref state);
            _monsterControlledEffectAttrLookup.Update(ref state);
            _monsterLossPoolAttrLookup.Update(ref state);
            _heroAttrLookup.Update(ref state);
            _hitRecordBufferLookup.Update(ref state);
            _skillBurstDamage.Update(ref state);
            _transform.Update(ref state);
            _linkedLookup.Update(ref state);
            _monsterTempDamageTextLookup.Update(ref state);
            _monsterTempDotDamageTextLookup.Update(ref state);
            _monsterDebuffAttrLookup.Update(ref state);
            _monsterDotDamageBufferLookup.Update(ref state);
            _skillElementBurstLookup.Update(ref state);

            //获取收集世界单例
            var detectionSystem = state.WorldUnmanaged.GetUnsafeSystemRef<DetectionSystem>(_detectionSystemHandle);
            var hitsArray = detectionSystem.skillOverTimeHitMonsterArray;


            var overlapSystem = state.WorldUnmanaged.GetUnsafeSystemRef<OverlapDetectionSystem>(_overlapDetectionSystemHandle);
            var hitsBurstArray = overlapSystem.skillBurstOverlapMonsterArray;
            var deltaTime = SystemAPI.Time.DeltaTime;

            // DevDebug.LogError("hitaary length     :" + hitsArray.Length);
            // 2. 并行应用伤害 技能& 标记技能结束？,重要job 结构
            //var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var ecbWriter = ecb.AsParallelWriter();
            state.Dependency = new ApplySkillDamageBurstJob
            {
                ECB = ecbWriter,
                DamageParLookup = _skillBurstDamage,
                DefenseAttrLookup = _monsterDefenseAttrLookup,
                LossPoolAttrLookup = _monsterLossPoolAttrLookup,
                ElementBurstLookup =_skillElementBurstLookup,
                ControlledEffectAttrLookup = _monsterControlledEffectAttrLookup,
                LinkedLookup = _linkedLookup,
                HitArray = hitsBurstArray,
                HeroAttrLookup = _heroAttrLookup,
                Transform = _transform,
                TempDamageText = _monsterTempDamageTextLookup,
                TempDotDamageText = _monsterTempDotDamageTextLookup,
                DebufferAttrLookup = _monsterDebuffAttrLookup,
                DotDamageBufferLookup = _monsterDotDamageBufferLookup,
                DeltaTime = deltaTime,
            }.ScheduleParallel(hitsBurstArray.Length, 64, state.Dependency);
            
            //元素爆发第二阶段 池化逻辑，这种写入方式貌似会有覆盖现象
            if (enableHeroSkillElementBurstB)//由技能释放系统外部开启job控制
                state.Dependency = new ApplySkillDamageBurst_ElementBurstBJob
                {
                    ECB = ecbWriter,
                    DamageParLookup = _skillBurstDamage,
                    ElementBurstLookup = _skillElementBurstLookup,
                    HitArray = hitsBurstArray,
                    DeltaTime = deltaTime,
                    Transform = _transform,
                    LossPoolAttrLookup = _monsterLossPoolAttrLookup,

                }.ScheduleParallel(hitsBurstArray.Length, 64, state.Dependency);

            //state.Dependency.Complete();

        }

        void OnDestroy(ref SystemState state) { }

    }


    /// <summary>
    /// 技能的持续性伤害，防止锁定buffer的无序扩张， 持续性伤害是都需要新的爆发性技能 ，执行周期由技能标签控制
    /// </summary>
    [BurstCompile]
    struct ApplySkillDamageBurstJob : IJobFor
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<SkillsBurstDamageCalPar> DamageParLookup;
        [ReadOnly] public ComponentLookup<SkillElementBurstTag> ElementBurstLookup;
        [ReadOnly] public ComponentLookup<MonsterDefenseAttribute> DefenseAttrLookup;
        [ReadOnly] public ComponentLookup<MonsterControlledEffectAttribute> ControlledEffectAttrLookup;
        [ReadOnly] public ComponentLookup<MonsterLossPoolAttribute> LossPoolAttrLookup;
        [ReadOnly] public BufferLookup<LinkedEntityGroup> LinkedLookup;
        [ReadOnly] public NativeArray<TriggerPairData> HitArray;
        [ReadOnly] public ComponentLookup<HeroAttributeCmpt> HeroAttrLookup;
        //技能位置信息
        [ReadOnly] public ComponentLookup<LocalTransform> Transform;
        [ReadOnly] public float DeltaTime;

        //伤害飘字
        [ReadOnly] public ComponentLookup<MonsterTempDamageText> TempDamageText;
        //Dot伤害飘字的定义
        [ReadOnly] public ComponentLookup<MonsterTempDotDamageText> TempDotDamageText;
        //debuffer 效果
        [ReadOnly] public ComponentLookup<MonsterDebuffAttribute> DebufferAttrLookup;
        //buffer累加
        [ReadOnly] public BufferLookup<MonsterDotDamageBuffer> DotDamageBufferLookup;


        static readonly ProfilerMarker mPB_Execute =
new ProfilerMarker("SkillBurstDamageJob.Execute");

        public void Execute(int i)
        {
            using (mPB_Execute.Auto())
            {

                // 1) 拿到碰撞实体对
                var pair = HitArray[i];
                Entity skill = pair.EntityA;
                Entity target = pair.EntityB;
                //DevDebug.Log("进入：" );


                // 2) 读取组件 & 随机数
                var d = DamageParLookup[skill];


                // DevDebug.Log("进入");

                //超过1帧直接退回， 进行单次伤害计算,这里burstTIme 要在主线程里更新
                if (d.burstTime > DeltaTime)
                {
                    // DevDebug.Log("返回");
                    return;
                }
                else
                {

                    // DevDebug.Log("计算");
                    var a = DefenseAttrLookup[target];
                    var c = ControlledEffectAttrLookup[target];
                    var l = LossPoolAttrLookup[target];
                    var h = HeroAttrLookup[d.heroRef];
                    var db = DebufferAttrLookup[target];
                    var dbd = DotDamageBufferLookup[target];
                    //这里注意拿取的时候和添加的时候不一样， 添加的时候烘焙完了之后，貌似又没有动？
                    var textRenderEntity = LinkedLookup[target][2].Value;
                    //这里拿取DOT的伤害飘字
                    var textDotRenderEntity = LinkedLookup[target][3].Value;
                    var tempText = TempDamageText[textRenderEntity];
                    var tempDotText = TempDotDamageText[textDotRenderEntity];
                    var rnd = new Unity.Mathematics.Random(a.rngState);
                    var e = ElementBurstLookup[skill];
                    //补充元素护盾二阶段独立增伤值
                    var elementShieldBAddDamagepar = h.attackAttribute.heroDynamicalAttack.tempMasterDamagePar;


                    // 3) 闪避判定-这里应该展现闪避字体
                    if (rnd.NextFloat() < a.dodge)
                    {
                        // 9) 保存新的 RNG 状态
                        a.rngState = rnd.state;
                        ECB.SetComponent(i, target, a);
                        tempText.underAttack = true;
                        tempText.damageTriggerType = DamageTriggerType.Miss;
                        //写回闪避字样
                        ECB.SetComponentEnabled<MonsterTempDamageText>(i, textRenderEntity, true);
                        ECB.SetComponent(i, textRenderEntity, tempText);
                        return;
                    }

                    //3+0）
                    //刷新击中时间,更新击中判定
                    d.hitSurvivalTime = 1;
                    d.hit = true;

                    //3+1) 控制系统参数，传入相关的控制参数， 达到阈值之后产生控制效果
                    //控制系统是以叠加周期的方式进行，而不是概率方式进行
                    //常规控制可以每次击打刷新，而强力控制必须是周期性累加

                    //减速， 这个可以是为常规控制，常规控制击中则清零
                    c.slow += h.controlAbilityAttribute.slow + d.tempSlow;
                    c.slowTimer = 0;
                    //击退,不进行叠加
                    c.knockback = h.controlAbilityAttribute.knockback;
                    c.knockbackTimer = 0;


                    //状态控制，后者可覆盖前者状态，但控制状态标识依旧在，用于计算伤害
                    //恐惧
                    c.fear += h.controlAbilityAttribute.fear + d.tempFear;
                    //定身
                    c.root += h.controlAbilityAttribute.root + d.tempRoot;
                    //昏迷
                    c.stun += h.controlAbilityAttribute.stun + d.tempStun;
                    //冻结
                    c.freeze += h.controlAbilityAttribute.freeze + d.tempFreeze;

                    //由技能标签的加载决定牵引状态,通常可以获得道具或者技能可以获得临时或者永久牵引或者爆炸值
                    //牵引或者爆炸有1秒间隔，因为有buffer的间隔，所以这里判断并不能和执行
                    //牵引或者爆炸是直接执行，不由累加进行触发
                    if (d.enablePull)
                    {
                        c.pull = h.controlAbilityAttribute.pull + d.tempPull;
                        c.pullCenter = Transform[skill].Position;

                    }
                    //爆炸
                    if (d.enableExplosion)
                    {
                        c.explosion = h.controlAbilityAttribute.explosion + d.tempExplosion;
                        c.explosionCenter = Transform[skill].Position;
                        //牵引或者爆炸有两秒间隔，因为有buffer的间隔，所以这里判断并不能和执行            
                    }
                    //-- 控制区域
                    // 4) 计算缩减后的“瞬时伤害”和“DOT伤害”
                    float instTotal = 0f, dotTotal = 0f;
                    {
                        // 物理子计算
                        float CalcPhysicalSub(float armor, float breakVal, float pen)
                        {
                            float eff = armor - (breakVal + math.max(0f, 1f - pen) * armor);
                            return eff / (eff + 100f);
                        }

                        // 元素子计算
                        float CalcElementSub(float res, float breakVal, float pen)
                        {
                            float eff = res - (breakVal + math.max(0f, 1f - pen) * res);
                            return eff / (eff + 50f);
                        }

                        // 物理,加上减益效果的护甲削弱,原本的怪的减益于效果的设计（保留），这里可以配合类似鱼人电灯类的技能（设计）
                        float physSub = CalcPhysicalSub(a.armor - db.armorReduction,
                                                        h.attackAttribute.armorBreak,
                                                        h.attackAttribute.armorPenetration);
                        instTotal += d.instantPhysicalDamage * (1f - physSub);
                        dotTotal += d.bleedDotDamage * (1f - physSub);

                        // 火
                        float fireSub = CalcElementSub(a.resistances.fire - db.resistanceReduction.fire,
                                                       h.attackAttribute.elementalBreak,
                                                       h.attackAttribute.elementalPenetration);
                        instTotal += d.fireDamage * (1f - fireSub);
                        dotTotal += d.fireDotDamage * (1f - fireSub);

                        // 冰
                        float frostSub = CalcElementSub(a.resistances.frost - db.resistanceReduction.frost,
                                                        h.attackAttribute.elementalBreak,
                                                        h.attackAttribute.elementalPenetration);
                        instTotal += d.frostDamage * (1f - frostSub);
                        dotTotal += d.frostDotDamage * (1f - frostSub);

                        // 闪电
                        float lightSub = CalcElementSub(a.resistances.lightning - db.resistanceReduction.lightning,
                                                        h.attackAttribute.elementalBreak,
                                                        h.attackAttribute.elementalPenetration);
                        instTotal += d.lightningDamage * (1f - lightSub);
                        dotTotal += d.lightningDotDamage * (1f - lightSub);

                        // 毒素
                        float poisonSub = CalcElementSub(a.resistances.poison - db.resistanceReduction.poison,
                                                         h.attackAttribute.elementalBreak,
                                                         h.attackAttribute.elementalPenetration);
                        instTotal += d.poisonDamage * (1f - poisonSub);
                        dotTotal += d.poisonDotDamage * (1f - poisonSub);

                        // 暗影
                        float shadowSub = CalcElementSub(a.resistances.shadow - db.resistanceReduction.shadow,
                                                         h.attackAttribute.elementalBreak,
                                                         h.attackAttribute.elementalPenetration);
                        instTotal += d.shadowDamage * (1f - shadowSub);
                        dotTotal += d.shadowDotDamage * (1f - shadowSub);
                    }


                    // 5) 池化反应（基于 raw dmg，不受任何减免，已闪避过滤，控制增加池化值？）
                    l.attackTimer = 0.07f;

                    float origHp = a.originalHp;
                    const float mult = 20f, cap = 200f;
                    //池化反应也要乘对应的伤害change参数,10% 伤害对应200点池化值，且不受任何减免影响
                    float Gain(float raw, float dot) => math.min(((raw + dot) * (d.damageChangePar) / origHp) * 100f * mult, cap);
                    var addFirePool = Gain(d.fireDamage, d.fireDotDamage);
                    var addFrostPool = Gain(d.frostDamage, d.frostDotDamage);
                    var addLightningPool = Gain(d.lightningDamage, d.lightningDotDamage);
                    var addPoisonPool = Gain(d.poisonDamage, d.poisonDotDamage);
                    var addShadowPool = Gain(d.shadowDamage, d.shadowDotDamage);
                    var addBleedPool = Gain(d.instantPhysicalDamage, d.bleedDotDamage) * 0.25f;


                    l.firePool = math.min(l.firePool + addFirePool, cap);
                    l.frostPool = math.min(l.frostPool + addFrostPool, cap);
                    l.lightningPool = math.min(l.lightningPool + addLightningPool, cap);
                    l.poisonPool = math.min(l.poisonPool + addPoisonPool, cap);
                    l.shadowPool = math.min(l.shadowPool + addShadowPool, cap);
                    //流血池只使用25%物理伤害计算
                   l.bleedPool = math.min(l.bleedPool + addBleedPool, cap);

                    //5-1）防止写回覆盖这里增加 元素爆发的第二阶段池化逻辑！！！，不能以通用ECB 写回同一个组件，否则会发生叠加？
                    //这里直接覆盖

                      if (e.enableSecondB)
                    {

                        // 掩码判定（damage > 0f 则为1，否则为0）
                        float fireMask = math.step(1e-6f, d.fireDotDamage);
                        float frostMask = math.step(1e-6f, d.frostDotDamage);
                        float lightningMask = math.step(1e-6f, d.lightningDotDamage);
                        float poisonMask = math.step(1e-6f, d.poisonDotDamage);
                        float shadowMask = math.step(1e-6f, d.shadowDotDamage);
                        float bleedMask = math.step(1e-6f, d.bleedDotDamage);

                        // 每种元素的池化增量（你可以自定义增长公式，比如基础100+等级*10，也可以按伤害比例加）
                        addFirePool = fireMask * (100f + e.level * 10f);
                        addFrostPool= frostMask * (100f + e.level * 10f);
                        addLightningPool  = lightningMask * (100f + e.level * 10f);
                        addPoisonPool = poisonMask * (100f + e.level * 10f);
                        addShadowPool = shadowMask * (100f + e.level * 10f);
                        addBleedPool = bleedMask * (100f + e.level * 10);

                        // 原有池化值 + 增量，并钳制到cap
                        l.firePool = math.min(l.firePool + addFirePool, cap);
                        l.frostPool = math.min(l.frostPool + addFrostPool, cap);
                        l.lightningPool = math.min(l.lightningPool + addLightningPool, cap);
                        l.poisonPool = math.min(l.poisonPool + addPoisonPool, cap);
                        l.shadowPool = math.min(l.shadowPool + addShadowPool, cap);
                        l.bleedPool = math.min(l.bleedPool + addBleedPool, cap);

                    }

                    // 6) 格挡判定（仅对瞬时）,随机减免20%-80%伤害
                    var tempBlock = false;
                    if (rnd.NextFloat() < a.block)
                    {
                        float br = math.lerp(0.2f, 0.8f, rnd.NextFloat());
                        instTotal *= (1f - br);
                        tempBlock = true;
                    }

                    // 7) 固定减伤（对瞬时+DOT，0-50%的固定随机减伤，用于控制数字跳动),这里的DOT伤害是计算过暴击和抗性之后,补充上伤害加深的debuffer
                    //这里乘以伤害变化参数
                    var rd = math.lerp(0.0f, 0.5f, rnd.NextFloat());//固定随机减伤
                    float finalDamage = (instTotal + dotTotal) * (1f - a.damageReduction) * (1 - rd) * (1 + db.damageAmplification) * d.damageChangePar*elementShieldBAddDamagepar;
                    //这里分离dot伤害
                    float finalDotDamage = (dotTotal) * (1f - a.damageReduction) * (1 - rd) * (1 + db.damageAmplification) * d.damageChangePar*elementShieldBAddDamagepar;


                    //（7-1）写回dot伤害的扣血总量,采用同样的buffer累加方式
                    db.totalDotDamage += finalDotDamage;



                    // 8) 应用扣血 & 写回
                    a.hp = math.max(0f, a.hp - finalDamage);
                    // DevDebug.Log("伤害：" + finalDamage);

                    //8-1) 伤害数字传入
                    //确认收到攻击
                    tempText.underAttack = true;
                    //传入伤害数字
                    tempText.hurtVlue = finalDamage;
                    //传入瞬时伤害类型,将子弹的伤害枚举类型传入临时结构体中，持续伤害会覆盖
                    tempText.damageTriggerType = d.damageTriggerType;
                    //写回格挡,大多数情况下三元判断能被编译为分支的 csel 指令
                    tempText.damageTriggerType = tempBlock ? DamageTriggerType.Block : tempText.damageTriggerType;

                    //-- DOT类型传入,这里用来测试


                    //8-2 这里保留原本的写入， 这样在后续的代码中仅需要遍历长度>=2的buffer，同时保留原结构不变
                    var dd = new HeroSkillPropAccumulateData();
                    dd.damage = finalDamage;//合并使用伤害数字，到这个job之后更新

                    // 各池化增量
                    dd.firePool = addFirePool;
                    dd.frostPool = addFrostPool;
                    dd.lightningPool = addLightningPool;
                    dd.poisonPool = addPoisonPool;
                    dd.shadowPool = addShadowPool;
                    dd.bleedPool = addBleedPool;

                    //8-3 写入动态Dotbuffer,最终计算的dot总伤害，剩余时间6秒
                    //这里貌似只能这样写 无法SIMD优化
                    if (finalDotDamage > 0)
                    {
                        var tdbd = new MonsterDotDamageBuffer();
                        tdbd.dotDamage = finalDotDamage;
                        tdbd.survivalTime = 6;
                        //累加怪物受到的buffer
                        ECB.AppendToBuffer(i, target, tdbd);
                    }


                    //攻击颜色变化状态
                    // 9) 受击高亮，SIMD 指令优化
                    {
                        // 先统一一个本帧赋  3f 值的常量
                        const float newTimerValue = 3f;

                        // 对每个元素都做同样的掩码写回，job 中采用无分支实现？！！！这点很重要
                        // math.step(0, x) == (x > 0 ? 1f : 0f) 无分支实现
                        float frostMask = math.step(1e-6f, d.frostDamage);
                        float fireMask = math.step(1e-6f, d.fireDamage);
                        float poisonMask = math.step(1e-6f, d.poisonDamage);
                        float lightningMask = math.step(1e-6f, d.lightningDamage);
                        float shadowMask = math.step(1e-6f, d.shadowDamage);
                        float bleedMask = math.step(1e-6f, d.instantPhysicalDamage);

                        // 然后一次性写回，全部都是单条算式，没有 if
                        l.frostTimer = frostMask * newTimerValue + (1f - frostMask) * l.frostTimer;
                        l.fireTimer = fireMask * newTimerValue + (1f - fireMask) * l.fireTimer;
                        l.poisonTimer = poisonMask * newTimerValue + (1f - poisonMask) * l.poisonTimer;
                        l.lightningTimer = lightningMask * newTimerValue + (1f - lightningMask) * l.lightningTimer;
                        l.shadowTimer = shadowMask * newTimerValue + (1f - shadowMask) * l.shadowTimer;
                        l.bleedTimer = bleedMask * newTimerValue + (1f - bleedMask) * l.bleedTimer;

                        // 2) DOT 活动开关：如果对应 dotDamage>0 则设为 6，否则保持原值
                        float frostDotMask = math.step(1e-6f, d.frostDotDamage);
                        float fireDotMask = math.step(1e-6f, d.fireDotDamage);
                        float poisonDotMask = math.step(1e-6f, d.poisonDotDamage);
                        float lightningDotMask = math.step(1e-6f, d.lightningDotDamage);
                        float shadowDotMask = math.step(1e-6f, d.shadowDotDamage);
                        float bleedDotMask = math.step(1e-6f, d.bleedDotDamage);

                        // 当 mask==1 时设置为 6f；mask==0 时保持之前的值
                        l.frostActive = frostDotMask * 6f + (1f - frostDotMask) * l.frostActive;
                        l.fireActive = fireDotMask * 6f + (1f - fireDotMask) * l.fireActive;
                        l.poisonActive = poisonDotMask * 6f + (1f - poisonDotMask) * l.poisonActive;
                        l.lightningActive = lightningDotMask * 6f + (1f - lightningDotMask) * l.lightningActive;
                        l.shadowActive = shadowDotMask * 6f + (1f - shadowDotMask) * l.shadowActive;
                        l.bleedActive = bleedDotMask * 6f + (1f - bleedDotMask) * l.bleedActive;
                    }

                    // 9) 保存新的 RNG 状态
                    a.rngState = rnd.state;
                    ECB.SetComponent(i, target, a);
                    ECB.SetComponent(i, target, l);
                    ECB.SetComponent(i, target, c);
                    //激活临时瞬时伤害表
                    ECB.SetComponentEnabled<MonsterTempDamageText>(i, textRenderEntity, true);
                    ECB.SetComponent(i, textRenderEntity, tempText);
                    //写回的debuffer记录的dot伤害，以及一些触发的抑制效果
                    ECB.SetComponent(i, target, db);
                    //添加累加的buffer 数据，解决同帧数据并行写入 覆盖的问题
                    ECB.AppendToBuffer(i, target, dd);

                }
            }
        }
    }


    struct ApplySkillDamageBurst_ElementBurstBJob : IJobFor
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<SkillsBurstDamageCalPar> DamageParLookup;
        [ReadOnly] public ComponentLookup<SkillElementBurstTag> ElementBurstLookup;
        [ReadOnly] public ComponentLookup<MonsterLossPoolAttribute> LossPoolAttrLookup;
        [ReadOnly] public NativeArray<TriggerPairData> HitArray;
        [ReadOnly] public ComponentLookup<LocalTransform> Transform;
        [ReadOnly] public float DeltaTime;


        static readonly ProfilerMarker mPBE_Execute =
new ProfilerMarker("SkillBurstDamage_ElementBurstBJob.Execute");

        public void Execute(int i)
        {
            using (mPBE_Execute.Auto())
            {
                var pair = HitArray[i];
                Entity skill = pair.EntityA;
                Entity target = pair.EntityB;

                // 2) 读取组件 & 随机数
                var d = DamageParLookup[skill];

                //超过1帧直接退回， 进行单次伤害计算,这里burstTIme 要在主线程里更新
                if (d.burstTime > DeltaTime)
                {
                    // DevDebug.Log("返回");
                    return;
                }
                else
                {


                    var e = ElementBurstLookup[skill];
                    var l = LossPoolAttrLookup[target]; 

                    if (e.enableSecondB)
                    {
                        // 钳制上限
                        const float cap = 200f;
                        // 掩码判定（damage > 0f 则为1，否则为0）
                        float fireMask = math.step(1e-6f, d.fireDotDamage);
                        float frostMask = math.step(1e-6f, d.frostDotDamage);
                        float lightningMask = math.step(1e-6f, d.lightningDotDamage);
                        float poisonMask = math.step(1e-6f, d.poisonDotDamage);
                        float shadowMask = math.step(1e-6f, d.shadowDotDamage);
                        float bleedMask = math.step(1e-6f, d.bleedDotDamage);

                        // 每种元素的池化增量（你可以自定义增长公式，比如基础100+等级*10，也可以按伤害比例加）
                        float firePoolAdd = fireMask * (100f + e.level * 10f);
                        float frostPoolAdd = frostMask * (100f + e.level * 10f);
                        float lightningPoolAdd = lightningMask * (100f + e.level * 10f);
                        float poisonPoolAdd = poisonMask * (100f + e.level * 10f);
                        float shadowPoolAdd = shadowMask * (100f + e.level * 10f);
                        float bleedMaskAdd = bleedMask * (100f + e.level * 10);

                        // 原有池化值 + 增量，并钳制到cap
                        l.firePool = math.min(l.firePool + firePoolAdd, cap);
                        l.frostPool = math.min(l.frostPool + frostPoolAdd, cap);
                        l.lightningPool = math.min(l.lightningPool + lightningPoolAdd, cap);
                        l.poisonPool = math.min(l.poisonPool + poisonPoolAdd, cap);
                        l.shadowPool = math.min(l.shadowPool + shadowPoolAdd, cap);
                        l.bleedPool = math.min(l.bleedPool + bleedMaskAdd, cap);

                    }

                    // 记得写回
                    ECB.SetComponent(i, target, l);

                }
            }


        }

    }
}