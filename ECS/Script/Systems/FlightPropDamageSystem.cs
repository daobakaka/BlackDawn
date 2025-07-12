using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using System.Globalization;

namespace BlackDawn.DOTS
{
    /// <summary>
    /// 处理所有带 FlightProDamageCalPar 的飞行道具与目标的碰撞伤害
    /// </summary>
    [RequireMatchingQueriesForUpdate]
    [UpdateAfter(typeof(ActionSystem))]
    [UpdateInGroup(typeof(ActionSystemGroup))]
    [BurstCompile]
    public partial struct FlightPropDamageSystem : ISystem
    {
        // —— 只读查找组件 —— 
        private ComponentLookup<FlightPropDamageCalPar> _damageParLookup;
        private ComponentLookup<LiveMonster> _liveMonsterLooup;
        private ComponentLookup<MonsterDefenseAttribute> _monsterDefenseAttrLookup;
        private ComponentLookup<MonsterLossPoolAttribute> _monsterLossPoolAttrLookup;
        private ComponentLookup<MonsterControlledEffectAttribute> _monsterContorlldEfffectAttrLookup;
        private ComponentLookup<MonsterTempDamageText> _monsterTempDamageTextLookup;
        private ComponentLookup<MonsterTempDotDamageText> _monsterTempDotDamageTextLookup;
        //子组件，用于计算shader参数变化
        private BufferLookup<LinkedEntityGroup> _linkedLookup;
        //部分参数用于寻找到英雄结构体进行计算
        private ComponentLookup<HeroAttributeCmpt> _heroAttrLookup;
        //攻击伤害锁定间隔
        private BufferLookup<HitRecord> _recordBufferLookup;
        //位置参数
        private ComponentLookup<LocalTransform> _transform;
        //debuffer伤害组件查询
        private ComponentLookup<MonsterDebuffAttribute> _monsterDebuffAttrLookup;
        //dot组件查询
        private BufferLookup<MonsterDotDamageBuffer> _monsterDotDamageBufferLookup;
        //侦测系统缓存
        private SystemHandle _detectionSystemHandle;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnablePropDamageSystemTag>();

            _damageParLookup = SystemAPI.GetComponentLookup<FlightPropDamageCalPar>(true);
            _liveMonsterLooup = SystemAPI.GetComponentLookup<LiveMonster>(true);
            _monsterDefenseAttrLookup = SystemAPI.GetComponentLookup<MonsterDefenseAttribute>(true);
            _monsterContorlldEfffectAttrLookup = SystemAPI.GetComponentLookup<MonsterControlledEffectAttribute>(true);
            _monsterLossPoolAttrLookup = SystemAPI.GetComponentLookup<MonsterLossPoolAttribute>(true);
            _linkedLookup = SystemAPI.GetBufferLookup<LinkedEntityGroup>(true);
            _heroAttrLookup = SystemAPI.GetComponentLookup<HeroAttributeCmpt>(true);
            _recordBufferLookup = SystemAPI.GetBufferLookup<HitRecord>(true);
            _transform = SystemAPI.GetComponentLookup<LocalTransform>(true);
            _monsterTempDamageTextLookup =SystemAPI.GetComponentLookup<MonsterTempDamageText>(true);
            _monsterTempDotDamageTextLookup = SystemAPI.GetComponentLookup<MonsterTempDotDamageText>(false);
            _monsterDebuffAttrLookup = SystemAPI.GetComponentLookup<MonsterDebuffAttribute>(true);
            _monsterDotDamageBufferLookup = SystemAPI.GetBufferLookup<MonsterDotDamageBuffer>(true);

            _detectionSystemHandle = state.WorldUnmanaged.GetExistingUnmanagedSystem<DetectionSystem>();

        }

        public void OnUpdate(ref SystemState state)
        {
          // state.Dependency.Complete();
            
            // 更新所有 lookup
            _damageParLookup.Update(ref state);
            _liveMonsterLooup.Update(ref state);
            _monsterDefenseAttrLookup.Update(ref state);
            _monsterContorlldEfffectAttrLookup.Update(ref state);
            _monsterLossPoolAttrLookup.Update(ref state);
            _linkedLookup.Update(ref state);
            _heroAttrLookup.Update(ref state);
            _recordBufferLookup.Update(ref state);
            _transform.Update(ref state);
            _monsterTempDamageTextLookup.Update(ref state);
            _monsterTempDotDamageTextLookup.Update(ref state);
            _monsterDebuffAttrLookup.Update(ref state);
            _monsterDotDamageBufferLookup.Update(ref state);

            //获取收集世界单例
            var detectionSystem = state.WorldUnmanaged.GetUnsafeSystemRef<DetectionSystem>(_detectionSystemHandle);
            var hitsArray = detectionSystem.flightHitMonsterArray;



            // 2. 并行应用伤害 & 销毁道具,重要job 结构

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            //自定义ECB buffer ,位于RenderEffects 之前运行
            var ecb1 = SystemAPI.GetSingleton<CustomEndActionECBSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);


            state.Dependency = new ApplyPropDamageJob
            {
                ECB = ecb.AsParallelWriter(),
                DamageParLookup = _damageParLookup,
                DefenseAttrLookup = _monsterDefenseAttrLookup,
                LossPoolLookup =_monsterLossPoolAttrLookup,
                ControlledEffectAttrLookup =_monsterContorlldEfffectAttrLookup,
                LinkedLookup = _linkedLookup,
                HitArray = hitsArray,
                HeroAttrLookup = _heroAttrLookup,
                RecordBufferLookup = _recordBufferLookup,
                Transform = _transform,
                TempDamageText =_monsterTempDamageTextLookup,
                TempDotDamageText=_monsterTempDotDamageTextLookup,
                DebufferAttrLookup=_monsterDebuffAttrLookup,
                DotDamageBufferLookup=_monsterDotDamageBufferLookup
            }
            .ScheduleParallel(hitsArray.Length, 64, state.Dependency);
            // state.Dependency.Complete();

            state.Dependency = new ApplyFlightPropBufferAggregatesJob
            {
                DamageTextLookop = _monsterTempDamageTextLookup,
                ECB = ecb1.AsParallelWriter(),
            }.ScheduleParallel(state.Dependency);

          //  state.Dependency.Complete();  // 等Job完成再操作ECB

        }

        public void OnDestroy(ref SystemState state) { }

     

    }

  

    /// <summary>
    /// 对每个碰撞对，读取伤害参数、扣血、销毁道具
    /// </summary>
    [BurstCompile]
    struct ApplyPropDamageJob : IJobFor
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<FlightPropDamageCalPar> DamageParLookup;
        [ReadOnly] public ComponentLookup<MonsterDefenseAttribute> DefenseAttrLookup;
        [ReadOnly] public ComponentLookup<MonsterControlledEffectAttribute> ControlledEffectAttrLookup;
        [ReadOnly] public ComponentLookup<MonsterLossPoolAttribute> LossPoolLookup;
        [ReadOnly] public BufferLookup<LinkedEntityGroup> LinkedLookup;
        [ReadOnly] public NativeArray<TriggerPairData> HitArray;
        [ReadOnly] public ComponentLookup<HeroAttributeCmpt> HeroAttrLookup;
        //攻击记录buffer,道具的buffer是加在飞行道具身上
        [ReadOnly] public BufferLookup<HitRecord> RecordBufferLookup;
        //飞行道具位置信息
        [ReadOnly] public ComponentLookup<LocalTransform> Transform;
        //伤害飘字的定义
        [ReadOnly] public ComponentLookup<MonsterTempDamageText> TempDamageText;
        //Dot伤害飘字的定义
        [ReadOnly] public ComponentLookup<MonsterTempDotDamageText> TempDotDamageText;
        //debuffer 效果
        [ReadOnly] public ComponentLookup<MonsterDebuffAttribute> DebufferAttrLookup;
        //buffer累加
        [ReadOnly] public BufferLookup<MonsterDotDamageBuffer> DotDamageBufferLookup;

        public void Execute(int i)
        {
            // 1) 拿到碰撞实体对
            var pair = HitArray[i];
            Entity prop = pair.EntityA;
            Entity target = pair.EntityB;
            //if (!DamageParLookup.HasComponent(prop))
            //{
            //    prop = pair.EntityB;
            //    target = pair.EntityA;
            //}

            if (!RecordBufferLookup.HasBuffer(prop))
                return;
                // 拿到道具的记录缓冲
                var buffer = RecordBufferLookup[prop];

            // 先检查是否已经记录过这个 target
            for (int j = 0; j < buffer.Length; j++)
            {
                //DevDebug.Log("buffer：--"+j +"   "+ buffer[j].timer);
                if (buffer[j].other == target)
                {
                
                    return;
                }
            }
            // 只有没记录过，才加进来,这里要注意并行写入限制，使用并行写入方法         
            ECB.AppendToBuffer(i, prop, new HitRecord { other = target });

            // 2) 读取组件 & 随机数
            var d = DamageParLookup[prop];
            var a = DefenseAttrLookup[target];
            var c = ControlledEffectAttrLookup[target];
            var l = LossPoolLookup[target];
            var h = HeroAttrLookup[d.heroRef];
            var db = DebufferAttrLookup[target];
            var dbd = DotDamageBufferLookup[target];
            //这里注意拿取的时候和添加的时候不一样， 添加的时候烘焙完了之后，貌似又没有动？
            var textRenderEntity = LinkedLookup[target][2].Value;
            //这里拿取DOT的伤害飘字
            var textDotRenderEntity = LinkedLookup[target][3].Value;
            var tempText = TempDamageText[textRenderEntity];
            var tempDotText =TempDotDamageText[textDotRenderEntity];
            var rnd = new Unity.Mathematics.Random(a.rngState);

            //补充元素护盾二阶段独立增伤值
            var elementShieldBAddDamagepar = h.attackAttribute.heroDynamicalAttack.tempMasterDamagePar;

            // 3) 闪避判定
            if (rnd.NextFloat() < a.dodge)
            {
                // 9) 保存新的 RNG 状态
                a.rngState = rnd.state;
                ECB.SetComponent(i, target, a);
                //通知文字miss
                tempText.underAttack = true;
                tempText.damageTriggerType = DamageTriggerType.Miss;
                //写回闪避字样
                ECB.SetComponentEnabled<MonsterTempDamageText>(i, textRenderEntity, true);
                ECB.SetComponent(i, textRenderEntity, tempText);

                return;
            }

            //3+1) 控制系统参数，传入相关的控制参数， 达到阈值之后产生控制效果
            //控制系统是以叠加周期的方式进行，而不是概率方式进行
            //常规控制可以每次击打刷新，而强力控制必须是周期性累加

            //减速， 这个可以是为常规控制，常规控制击中则清零
            c.slow += h.controlAbilityAttribute.slow;
            c.slowTimer = 0;
            //击退,不进行叠加
            c.knockback = h.controlAbilityAttribute.knockback;
            c.knockbackTimer = 0;


            //状态控制，后者可覆盖前者状态，但控制状态标识依旧在，用于计算伤害
            //恐惧
            c.fear += h.controlAbilityAttribute.fear;
            //定身
            c.root += h.controlAbilityAttribute.root;
            //昏迷
            c.stun += h.controlAbilityAttribute.stun;
            //冻结
            c.freeze += h.controlAbilityAttribute.freeze;



            //牵引和爆炸属于特殊控制效果，初步设计由技能或者特殊效果触发，技能开启期间， 设置人物的牵引值和爆炸值，检测方式应该迁移至特殊技能模块


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
                float physSub = CalcPhysicalSub(a.armor-db.armorReduction,
                                                h.attackAttribute.armorBreak,
                                                h.attackAttribute.armorPenetration);
                instTotal += d.instantPhysicalDamage * (1f - physSub);
                dotTotal += d.bleedDotDamage * (1f - physSub);

                // 火
                float fireSub = CalcElementSub(a.resistances.fire-db.resistanceReduction.fire,
                                               h.attackAttribute.elementalBreak,
                                               h.attackAttribute.elementalPenetration);
                instTotal += d.fireDamage * (1f - fireSub);
                dotTotal += d.fireDotDamage * (1f - fireSub);

                // 冰
                float frostSub = CalcElementSub(a.resistances.frost-db.resistanceReduction.frost,
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
                float Gain(float raw, float dot) => math.min(((raw + dot) / origHp) * 100f * mult, cap);

                var addFirePool = Gain(d.fireDamage, d.fireDotDamage);
                var addFrostPool = Gain(d.frostDamage, d.fireDotDamage);
                var addLightningPool =Gain(d.lightningDamage, d.lightningDotDamage);
                var addPosionPool = Gain(d.poisonDamage, d.poisonDotDamage);
                var addShadowPool =Gain(d.shadowDamage, d.shadowDotDamage);
                var addBleedPool = Gain(d.instantPhysicalDamage, d.bleedDotDamage)*0.25f;    
 

                l.firePool = math.min(l.firePool + addFrostPool, cap);
                l.frostPool = math.min(l.frostPool + addFirePool, cap);
                l.lightningPool = math.min(l.lightningPool + addLightningPool, cap);
                l.poisonPool = math.min(l.poisonPool + addPosionPool, cap);
                l.shadowPool = math.min(l.shadowPool + addShadowPool, cap);
                //流血池只使用25%物理伤害计算
                l.bleedPool = math.min(l.bleedPool + addBleedPool, cap) * 0.25f;




            // 6) 格挡判定（仅对瞬时）,随机减免20%-80%伤害
            var tempBlock = false;
            if (rnd.NextFloat() < a.block)
            {
                float br = math.lerp(0.2f, 0.8f, rnd.NextFloat());
                instTotal *= (1f - br);
                tempBlock = true;
                //通知文字格挡

            }

            // 7) 固定减伤（对瞬时+DOT，0-50%的固定随机减伤，用于控制数字跳动),这里的DOT伤害是计算过暴击和抗性之后,补充上伤害加深的debuffer
            var rd = math.lerp(0.0f, 0.5f, rnd.NextFloat());//固定随机减伤
            float finalDamage = (instTotal + dotTotal) * (1f - a.damageReduction) * (1 - rd)*(1+db.damageAmplification)*elementShieldBAddDamagepar;
            //这里分离dot伤害
            float finalDotDamage = (dotTotal) * (1f-a.damageReduction) * (1-rd) * (1 + db.damageAmplification)*elementShieldBAddDamagepar;


            //（7-1）写回dot伤害的扣血总量,采用同样的buffer累加方式
            db.totalDotDamage += finalDotDamage;

            // 8) 应用扣血 & 写回
            a.hp = math.max(0f, a.hp - finalDamage);
            //测试
            //  a.hp = math.max(0f, a.hp - 1);
           
            //8-1) 伤害数字传入
            //确认收到攻击
            tempText.underAttack = true;
            //传入伤害数字
            tempText.hurtVlue = finalDamage;
            //传入瞬时伤害类型,将子弹的伤害枚举类型传入临时结构体中，持续伤害会覆盖
            tempText.damageTriggerType = d.damageTriggerType;
            //写回格挡
            tempText.damageTriggerType = tempBlock? DamageTriggerType.Block: tempText.damageTriggerType;

            //-- DOT类型传入,这里用来测试     


            //8-2 这里保留原本的写入， 这样在后续的代码中仅需要遍历长度>=2的buffer，同时保留原结构不变
            var dd = new FlightPropAccumulateData();
            dd.damage = finalDamage;//合并使用伤害数字，到这个job之后更新
            dd.dotDamage = finalDotDamage;//写入dot伤害的总量

            // 各池化增量
            dd.firePool = addFirePool;
            dd.frostPool = addFrostPool;
            dd.lightningPool = addLightningPool;
            dd.poisonPool = addPosionPool;
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
            // 9) 受击高亮  SIMD 指令优化
            {
                var under = LinkedLookup[target][1].Value;
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

            // 9) 保存新的 RNG 状态，拆分之后需要重新写回
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

            //10) 标记道具销毁，这样就可以执行穿透逻辑，而不必持续检测
            {

                d.destory = true;
                ECB.SetComponent(i, prop, d);
            }

        }
    }

    /// <summary>
    /// 修正同时多个飞行道具碰撞产生的伤害以及累加池化计算问题
    /// </summary>
    [BurstCompile]
   partial struct ApplyFlightPropBufferAggregatesJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<MonsterTempDamageText> DamageTextLookop;
        public EntityCommandBuffer.ParallelWriter ECB;
        void Execute(
            Entity e,
             [EntityIndexInQuery] int sortKey, // 新增 sortKey（并发安全）
            EnabledRefRO<LiveMonster> live ,
            ref MonsterDefenseAttribute def,
            ref MonsterLossPoolAttribute pool, 
            ref MonsterDebuffAttribute dot,
             DynamicBuffer<FlightPropAccumulateData> accBuf,
            DynamicBuffer<LinkedEntityGroup>  linkedEntity)
        {
            //长度小于2的时候已经写入不需要聚合
            if (accBuf.Length < 2)
            {
                //返回之前清空buffer
                accBuf.Clear();
                return;
            }

            // 1) 聚合
            var sum = new FlightPropAccumulateData();
            for (int i = 0; i < accBuf.Length - 1; i++)
            {
                var d = accBuf[i];
                sum.damage += d.damage;
                sum.firePool += d.firePool;
                sum.frostPool += d.frostPool;
                sum.lightningPool += d.lightningPool;
                sum.poisonPool += d.poisonPool;
                sum.shadowPool += d.shadowPool;
                sum.bleedPool += d.bleedPool;
            }

            // 2) 写回血量
            def.hp = math.max(0f, def.hp - sum.damage);
           




            // 4) 写回池化值
            if (true)
            {
                pool.firePool = math.min(pool.firePool + sum.firePool, 200);
                pool.frostPool = math.min(pool.frostPool + sum.frostPool, 200);
                pool.lightningPool = math.min(pool.lightningPool + sum.lightningPool, 200);
                pool.poisonPool = math.min(pool.poisonPool + sum.poisonPool, 200);
                pool.shadowPool = math.min(pool.shadowPool + sum.shadowPool, 200);
                pool.bleedPool = math.min(pool.bleedPool + sum.bleedPool, 200);
            }
            //5)写回dot总伤害
            dot.totalDotDamage =sum.dotDamage;


            //这两条是去查找字体并且更改
            var damageText = DamageTextLookop[linkedEntity[2].Value];
            //写回伤害
            damageText.hurtVlue +=sum.damage;

           // DamageTextLookop[linkedEntity[2].Value] = damageText;

            ECB.SetComponent(sortKey, linkedEntity[2].Value, damageText);

            // 5) 清空 buffer，为下一帧重用
           // accBuf.Clear();

           // DevDebug.Log("已累加并清空自己的buffer");

        }
    }
}
