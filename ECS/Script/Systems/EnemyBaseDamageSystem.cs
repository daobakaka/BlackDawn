using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
//用于计算 怪物碰撞及基础攻击的buffer，英雄身上也添加了<hitRecord>默认周期为1秒
//这里基础攻击过滤掉了 detection 的检测，只检测liveMnster
namespace BlackDawn.DOTS
{
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateAfter(typeof(HeroSpecialSkillsDamageSystem))]
    [UpdateInGroup(typeof(ActionSystemGroup))]
    public partial struct EnemyBaseDamageSystem : ISystem
    {
        private ComponentLookup<LiveMonster> _liveMonsterLookup;
        private ComponentLookup<MonsterAttackAttribute> _monsterAttack;
        private ComponentLookup<HeroAttributeCmpt> _heroAttrLookup;
        private BufferLookup<HeroHitRecord> _recordBufferLookup;
        private ComponentLookup<PhysicsCollider> _physicsCollider;
        private ComponentLookup<HeroIntgratedNoImmunityState> _heroIntgrateNoImmunityStateLookup;    
        //侦测系统缓存
        private SystemHandle _detectionSystemHandle;
        void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableEnemyBaseDamageSystemTag>();
           // state.Enabled = false;
            _liveMonsterLookup = SystemAPI.GetComponentLookup<LiveMonster>(true);
            _heroAttrLookup = SystemAPI.GetComponentLookup<HeroAttributeCmpt>(true);
            _recordBufferLookup = SystemAPI.GetBufferLookup<HeroHitRecord>(true);
            _physicsCollider = SystemAPI.GetComponentLookup<PhysicsCollider>(true);
            _monsterAttack = SystemAPI.GetComponentLookup<MonsterAttackAttribute>(true);
            _heroIntgrateNoImmunityStateLookup = SystemAPI.GetComponentLookup<HeroIntgratedNoImmunityState>(true);

            _detectionSystemHandle = state.WorldUnmanaged.GetExistingUnmanagedSystem<DetectionSystem>();
        }

        [BurstCompile]
        void OnUpdate(ref SystemState state)
        {
            _liveMonsterLookup.Update(ref state);
            _heroAttrLookup.Update(ref state);
            _recordBufferLookup.Update(ref state);
            _physicsCollider.Update(ref state);
            _monsterAttack.Update(ref state);
            _heroIntgrateNoImmunityStateLookup.Update(ref state);

            //获取收集世界单例
            var detectionSystem = state.WorldUnmanaged.GetUnsafeSystemRef<DetectionSystem>(_detectionSystemHandle);
           
            var hitsArray = detectionSystem.heroHitMonsterArray;

            //var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            //var ecbWriter = ecb.AsParallelWriter();
            state.Dependency = new ApplyEnemyBaseDamageJob
            {
                ECB = ecb.AsParallelWriter(),
                AttrLookup = _heroAttrLookup,
                HitArray = hitsArray,
                MonsterAttrLookup = _monsterAttack,
                RecordBufferLookup = _recordBufferLookup,
                IntgratedNoImmunityStateLookup =_heroIntgrateNoImmunityStateLookup
               
            }.Schedule(hitsArray.Length, 64, state.Dependency);

            //state.Dependency.Complete();
            //ecb.Playback(state.EntityManager);
            //ecb.Dispose();
        }

        void OnDestroy(ref SystemState state) { }
    }


 






    /// <summary>
    /// 敌人近战伤害与英雄的碰撞计算，这里简化逻辑，近战、远程怪，碰到就会掉血
    /// </summary>
    [BurstCompile]
    struct ApplyEnemyBaseDamageJob : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        //这里就是计算英雄自身扣血逻辑
        [ReadOnly] public ComponentLookup<HeroAttributeCmpt> AttrLookup;
        //英雄的相关免疫计算
        [ReadOnly] public ComponentLookup<HeroIntgratedNoImmunityState> IntgratedNoImmunityStateLookup;
        // [ReadOnly] public BufferLookup<LinkedEntityGroup> LinkedLookup;
        [ReadOnly] public NativeArray<TriggerPairData> HitArray;
        [ReadOnly] public ComponentLookup<MonsterAttackAttribute> MonsterAttrLookup;
        [ReadOnly] public BufferLookup<HeroHitRecord> RecordBufferLookup;

        public void Execute(int i)
        {
            // 1) 拿到碰撞实体对
            var pair = HitArray[i];
            Entity monster = pair.EntityA;
            Entity hero = pair.EntityB;


            if (!MonsterAttrLookup.HasComponent(monster))
            {
                monster = pair.EntityB;
                hero = pair.EntityA;
            }

            // 拿到伤害的记录缓冲
            var buffer = RecordBufferLookup[hero];

            // 先检查是否已经记录过这个 target
            for (int j = 0; j < buffer.Length; j++)
            {
                if (buffer[j].other == monster)
                {
                    return;
                }
            }
            // 只有没记录过，才加进来,这里要注意并行写入限制，使用并行写入方法         
            ECB.AppendToBuffer(i, hero, new HeroHitRecord { other = monster });



            // 2) 读取组件 & 随机数
            var h = MonsterAttrLookup[monster];
            var a = AttrLookup[hero];
            var d = h;
            var inim = IntgratedNoImmunityStateLookup[hero];
            var rnd = new Unity.Mathematics.Random(a.defenseAttribute.rngState);

            // 3) 闪避判定
            if (rnd.NextFloat() < a.defenseAttribute.dodge)
            {
                // DevDebug.Log("随机数"+rnd.NextFloat()+"闪避率"+a.defenseAttribute.dodge);
                a.defenseAttribute.rngState = rnd.state;
                ECB.SetComponent(i, hero, a);
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
            instTotal += d.attackPower * (1f - physSub)*inim.physicalDamageNoImmunity;

            var dotPhy = rnd.NextFloat() < d.dotProcChance.bleedChance
                ? d.attackPower * (1f - physSub) * 0.3f
                : 0f;
            dotTotal += dotPhy*inim.dotNoImmunity;

            // 火
            float fireSub = CalcElementSub(
                a.defenseAttribute.resistances.fire,
                h.elementalBreak,
                h.elementalPenetration);
            instTotal += d.elementalDamage.fireDamage * (1f - fireSub)*inim.elementDamageNoImmunity;

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

            a.lossPoolAttribute.firePool = math.min(a.lossPoolAttribute.firePool + Gain(d.elementalDamage.fireDamage, dotFire), cap);
            a.lossPoolAttribute.frostPool = math.min(a.lossPoolAttribute.frostPool + Gain(d.elementalDamage.frostDamage, dotFrost), cap);
            a.lossPoolAttribute.lightningPool = math.min(a.lossPoolAttribute.lightningPool + Gain(d.elementalDamage.lightningDamage, dotFrost), cap);
            a.lossPoolAttribute.poisonPool = math.min(a.lossPoolAttribute.poisonPool + Gain(d.elementalDamage.poisonDamage, dotPoison), cap);
            a.lossPoolAttribute.shadowPool = math.min(a.lossPoolAttribute.shadowPool + Gain(d.elementalDamage.shadowDamage, dotShadow), cap);
            //流血池只使用25%物理伤害计算
            a.lossPoolAttribute.bleedPool = math.min(a.lossPoolAttribute.bleedPool + Gain(d.attackPower, dotPhy), cap) * 0.25f;


            // 6) 格挡判定（仅对瞬时）,随机减免20%-80%伤害
            if (rnd.NextFloat() < a.defenseAttribute.block)
            {
                float br = math.lerp(0.2f, 0.8f, rnd.NextFloat());
                instTotal *= (1f - br);
            }

            // 7) 固定减伤（对瞬时+DOT）
            var rd = math.lerp(0.0f, 0.5f, rnd.NextFloat());//固定随机减伤,0-50的固定随机减伤，模拟伤害波动
            float finalDamage = (instTotal ) * (1f - a.defenseAttribute.damageReduction) * (1 - rd);
            float finalDotDamage = (dotTotal) * (1f - a.defenseAttribute.damageReduction) * (1 - rd);
            // 8) 应用扣血 & 写回
            a.defenseAttribute.hp = math.max(0f, a.defenseAttribute.hp - finalDamage);

            //攻击颜色变化状态
            // 9) 受击高亮
            if (true)
            {
                //var under = LinkedLookup[target][1].Value;
                a.lossPoolAttribute.attackTimer = 0.1f;
                //  ECB.SetComponent(i, under, new UnderAttackColor { Value = new float4(1f, 1f, 1f, 1f) });
                // 2) 重置所有特效激活标志
                // 先统一一个本帧赋  3f 值的常量         

            }

            // 9) 保存新的 RNG 状态
            a.defenseAttribute.rngState = rnd.state;
            ECB.SetComponent(i, hero, a);




        }


    }
}