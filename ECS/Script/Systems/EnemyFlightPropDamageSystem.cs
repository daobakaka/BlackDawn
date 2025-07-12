using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Mathematics;
namespace BlackDawn.DOTS
{
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    //敌人道具伤害计算在基础伤害计算之后
    [UpdateAfter(typeof(EnemyBaseDamageSystem))]
    [UpdateInGroup(typeof(ActionSystemGroup))]
    public partial struct EnemyFlightPropDamageSystem : ISystem
    {
        // —— 只读查找组件 —— 
        private ComponentLookup<EnemyFlightProp> _damageParLookup;
        //怪物组件，用于查找计算怪物的其他属性
        private ComponentLookup<MonsterAttackAttribute> _monsterAttrLookup;

        private ComponentLookup<LiveMonster> _liveMonserLookup;
        //子组件，用于计算shader参数变化,传统mono控制方式发生变化
       // private BufferLookup<LinkedEntityGroup> linkedLookup;
        //部分参数用于寻找到英雄结构体进行计算
        private ComponentLookup<HeroAttributeCmpt> _heroAttrLookup;
        //buffer记录
        private BufferLookup<HitRecord> _recordBufferLookup;
        private ComponentLookup<HeroIntgratedNoImmunityState> _heroIntgrateNoImmunityStateLookup;

        //侦测系统缓存
        private SystemHandle _detectionSystemHandle;
        public void OnCreate(ref SystemState state)
        {
            //敌人飞行道具伤害计算标识
            state.RequireForUpdate<EnableEnemyPropDamageSystemTag>();

            _liveMonserLookup = SystemAPI.GetComponentLookup<LiveMonster>(true);
            _damageParLookup = SystemAPI.GetComponentLookup<EnemyFlightProp>(true);
            _monsterAttrLookup = SystemAPI.GetComponentLookup<MonsterAttackAttribute>(true);           
            _heroAttrLookup = SystemAPI.GetComponentLookup<HeroAttributeCmpt>(true);
            _recordBufferLookup =SystemAPI.GetBufferLookup<HitRecord>(false);
            _heroIntgrateNoImmunityStateLookup = SystemAPI.GetComponentLookup<HeroIntgratedNoImmunityState>(true);


            _detectionSystemHandle = state.WorldUnmanaged.GetExistingUnmanagedSystem<DetectionSystem>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //状态更新
            _liveMonserLookup.Update(ref state);
            _damageParLookup.Update(ref state);
            _monsterAttrLookup.Update(ref state);
            _heroAttrLookup.Update(ref state);
            _recordBufferLookup.Update(ref state);
            _heroIntgrateNoImmunityStateLookup.Update(ref state);

            //获取收集世界单例
            var detectionSystem = state.WorldUnmanaged.GetUnsafeSystemRef<DetectionSystem>(_detectionSystemHandle);
            var hitsArray = detectionSystem.enemyFlightHitHeroArray;
            SkillElementShieldTag_Hero heroShieldReduction;
            float tempReduciton = 0;

            if (SystemAPI.TryGetSingleton<SkillElementShieldTag_Hero>(out heroShieldReduction))
            {

             tempReduciton = heroShieldReduction.damageReduction > 0 ? heroShieldReduction.damageReduction : 0;

            }



          

            // 2. 并行应用伤害 & 销毁敌人道具,传入记录buffer，用一个额外的类专门计算buffer的持续时间，初始默认1秒
            // var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var ecbWriter = ecb.AsParallelWriter();
            state.Dependency = new ApplyEnemyPropDamageJob
            {
                ECB = ecbWriter,
                DamageParLookup = _damageParLookup,
                AttrLookup = _heroAttrLookup,
                HitArray = hitsArray,
                MonsterAttrLookup = _monsterAttrLookup,
                RecordBufferLookup = _recordBufferLookup,
                IntgratedNoImmunityStateLookup = _heroIntgrateNoImmunityStateLookup,
                ElementShieldReduction=tempReduciton,
            }
            .ScheduleParallel(hitsArray.Length, 64, state.Dependency);


           // state.Dependency.Complete();

            //// 3. 回放并清理
            //ecb.Playback(state.EntityManager);
            //ecb.Dispose();










        }


        public void OnDestroy(ref SystemState state)
        {

        }
    }






    /// <summary>
    /// 对每个碰撞对，读取伤害参数、扣血、销毁道具
    /// 这里怪物这样设计，可以增加怪物的属性参数调试，包括 物理穿透 护甲穿透 元素穿透，暂时舍弃压制 暴伤 易伤，保留DOT和池化
    /// 怪物的伤害计算 就用简单的attackPower和元素伤害来代替
    /// 怪物的dot计算这里直接使用原始伤害*几率*30%固定值来计算
    /// JjobParallerFor 并行写入，支持原生容器和只读限制
    /// </summary>
    [BurstCompile]
    struct ApplyEnemyPropDamageJob : IJobFor
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<EnemyFlightProp> DamageParLookup;
        //这里就是计算英雄自身扣血逻辑
        [ReadOnly] public ComponentLookup<HeroAttributeCmpt> AttrLookup;
        [ReadOnly] public ComponentLookup<HeroIntgratedNoImmunityState> IntgratedNoImmunityStateLookup;
        // [ReadOnly] public BufferLookup<LinkedEntityGroup> LinkedLookup;
        [ReadOnly] public NativeArray<TriggerPairData> HitArray;
        [ReadOnly] public ComponentLookup<MonsterAttackAttribute> MonsterAttrLookup;
        [ReadOnly] public BufferLookup<HitRecord> RecordBufferLookup;
    //元素护盾减伤
        [ReadOnly] public float ElementShieldReduction;

        public void Execute(int i)
        {
            // 1) 拿到碰撞实体对
            var pair = HitArray[i];
            Entity prop = pair.EntityA;
            Entity target = pair.EntityB;


            if (!DamageParLookup.HasComponent(prop))
            {
                prop = pair.EntityB;
                target = pair.EntityA;
            }
            var p = DamageParLookup[prop].monsterRef;



            //判断是否有组件
            if (!MonsterAttrLookup.HasComponent(p)) return;

            // 拿到道具的记录缓冲
            var buffer = RecordBufferLookup[prop];

            // 先检查是否已经记录过这个 target
            for (int j = 0; j < buffer.Length; j++)
            {
                if (buffer[j].other == target)
                {
                    return;
                }
            }
            // 只有没记录过，才加进来,这里要注意并行写入限制，使用并行写入方法         
            ECB.AppendToBuffer(i, prop, new HitRecord { other = target });



            // 2) 读取组件 & 随机数
            var h = MonsterAttrLookup[DamageParLookup[prop].monsterRef];
            var a = AttrLookup[target];
            var d = h;
            var inim = IntgratedNoImmunityStateLookup[target];
            var rnd = new Unity.Mathematics.Random(a.defenseAttribute.rngState);

            // 3) 闪避判定
            if (rnd.NextFloat() < a.defenseAttribute.dodge)
            {
                // DevDebug.Log("随机数"+rnd.NextFloat()+"闪避率"+a.defenseAttribute.dodge);
                a.defenseAttribute.rngState = rnd.state;
                ECB.SetComponent(i, target, a);
                return;
            }
            // DevDebug.Log("执行多线程2");

            // 4) 计算缩减后的“瞬时伤害”和“DOT伤害”
            float instTotal = 0f, dotTotal = 0f;

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

            // 物理
            float physSub = CalcPhysicalSub(
                a.defenseAttribute.armor,
                h.armorBreak,
                h.armorPenetration);
            instTotal += d.attackPower * (1f - physSub) * inim.physicalDamageNoImmunity;

            var dotPhy = rnd.NextFloat() < d.dotProcChance.bleedChance
                ? d.attackPower * (1f - physSub) * 0.3f
                : 0f;
            dotTotal += dotPhy * inim.dotNoImmunity;

            // 火
            float fireSub = CalcElementSub(
                a.defenseAttribute.resistances.fire,
                h.elementalBreak,
                h.elementalPenetration);
            instTotal += d.elementalDamage.fireDamage * (1f - fireSub) * inim.elementDamageNoImmunity;

            var dotFire = rnd.NextFloat() < d.dotProcChance.fireChance
                ? d.elementalDamage.fireDamage * (1f - fireSub) * 0.3f
                : 0f;
            dotTotal += dotFire * inim.dotNoImmunity;

            // 冰
            float frostSub = CalcElementSub(
                a.defenseAttribute.resistances.frost,
                h.elementalBreak,
                h.elementalPenetration);
            instTotal += d.elementalDamage.frostDamage * (1f - frostSub) * inim.elementDamageNoImmunity;

            var dotFrost = rnd.NextFloat() < d.dotProcChance.frostChance
                ? d.elementalDamage.frostDamage * (1f - frostSub) * 0.3f
                : 0f;
            dotTotal += dotFrost * inim.dotNoImmunity;

            // 闪电
            float lightSub = CalcElementSub(
                a.defenseAttribute.resistances.lightning,
                h.elementalBreak,
                h.elementalPenetration);
            instTotal += d.elementalDamage.lightningDamage * (1f - lightSub) * inim.elementDamageNoImmunity;

            var dotLightning = rnd.NextFloat() < d.dotProcChance.lightningChance
                ? d.elementalDamage.lightningDamage * (1f - lightSub) * 0.3f
                : 0f;
            dotTotal += dotLightning * inim.dotNoImmunity;

            // 毒素
            float poisonSub = CalcElementSub(
                a.defenseAttribute.resistances.poison,
                h.elementalBreak,
                h.elementalPenetration);
            instTotal += d.elementalDamage.poisonDamage * (1f - poisonSub) * inim.elementDamageNoImmunity;

            var dotPoison = rnd.NextFloat() < d.dotProcChance.poisonChance
                ? d.elementalDamage.poisonDamage * (1f - poisonSub) * 0.3f
                : 0f;
            dotTotal += dotPoison * inim.dotNoImmunity;

            // 暗影
            float shadowSub = CalcElementSub(
                a.defenseAttribute.resistances.shadow,
                h.elementalBreak,
                h.elementalPenetration);
            instTotal += d.elementalDamage.shadowDamage * (1f - shadowSub) * inim.elementDamageNoImmunity;

            var dotShadow = rnd.NextFloat() < d.dotProcChance.shadowChance
                ? d.elementalDamage.shadowDamage * (1f - shadowSub) * 0.3f
                : 0f;
            dotTotal += dotShadow * inim.dotNoImmunity;



            // 5) 池化反应（基于 raw dmg，不受任何减免，已闪避过滤）
            //dot dot伤害也能堆叠反应池

            float origHp = a.defenseAttribute.originalHp;
            const float mult = 5f, cap = 200f;
            float Gain(float raw, float dot) => math.min(((raw + dot) / origHp) * 100f * mult, cap);

            a.lossPoolAttribute.firePool = math.min(a.lossPoolAttribute.firePool + Gain(h.elementalDamage.fireDamage, dotFire), cap);
            a.lossPoolAttribute.frostPool = math.min(a.lossPoolAttribute.frostPool + Gain(h.elementalDamage.frostDamage, dotFrost), cap);
            a.lossPoolAttribute.lightningPool = math.min(a.lossPoolAttribute.lightningPool + Gain(h.elementalDamage.lightningDamage, dotFrost), cap);
            a.lossPoolAttribute.poisonPool = math.min(a.lossPoolAttribute.poisonPool + Gain(h.elementalDamage.poisonDamage, dotPoison), cap);
            a.lossPoolAttribute.shadowPool = math.min(a.lossPoolAttribute.shadowPool + Gain(h.elementalDamage.shadowDamage, dotShadow), cap);
            //流血池只使用25%物理伤害计算
            a.lossPoolAttribute.bleedPool = math.min(a.lossPoolAttribute.bleedPool + Gain(h.attackPower, dotPhy), cap) * 0.25f;


            // 6) 格挡判定（仅对瞬时）,随机减免20%-80%伤害
            if (rnd.NextFloat() < a.defenseAttribute.block)
            {
                float br = math.lerp(0.2f, 0.8f, rnd.NextFloat());
                instTotal *= (1f - br);
            }

            // 7) 固定减伤（对瞬时+DOT）
            var rd = math.lerp(0.0f, 0.5f, rnd.NextFloat());//固定随机减伤,0-50的固定随机减伤，模拟伤害波动
            float finalDamage = (instTotal) * (1f - a.defenseAttribute.damageReduction) * (1 - rd)*(1-ElementShieldReduction);
            float finalDotDamage = (dotTotal) * (1f - a.defenseAttribute.damageReduction) * (1 - rd)*(1-ElementShieldReduction);
            // 8) 应用扣血 & 写回
            if(ElementShieldReduction<=0)           
            a.defenseAttribute.hp = math.max(0f, a.defenseAttribute.hp - finalDamage);
            else
            a.defenseAttribute.energy =math.max(0f, a.defenseAttribute.energy - finalDamage/100);

            //攻击颜色变化状态
            // 9) 受击高亮
            if (true)
            {
                //var under = LinkedLookup[target][1].Value;
                a.lossPoolAttribute.attackTimer = 0.1f;
                //  ECB.SetComponent(i, under, new UnderAttackColor { Value = new float4(1f, 1f, 1f, 1f) });

            }

            // 9) 保存新的 RNG 状态
            a.defenseAttribute.rngState = rnd.state;
            ECB.SetComponent(i, target, a);



            // —— 10 标记删除，在mono中删除
            var pp = DamageParLookup[prop];
            pp.destory = true;
            //  ECB.SetComponent(i, prop, pp);


        }
    }

}