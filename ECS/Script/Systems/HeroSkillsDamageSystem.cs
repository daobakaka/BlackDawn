using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using TMPro;

//����Ӣ�ۼ��ܵļ���Լ��˺�����,���ϵͳ�ڻ����˺�ϵͳ֮����и���
namespace BlackDawn.DOTS
{

    [RequireMatchingQueriesForUpdate]
    [UpdateAfter(typeof(EnemyFlightPropMonoSystem))]
    [UpdateInGroup(typeof(ActionSystemGroup))]
    [BurstCompile]
    public partial struct HeroSkillsDamageSystem : ISystem
    {
        private ComponentLookup<LiveMonster> _liveMonster;
        private ComponentLookup<MonsterDefenseAttribute> _monsterDefenseAttrLookup;
        private ComponentLookup<MonsterLossPoolAttribute> _monsterLossPoolAttrLookip;
        private ComponentLookup<MonsterControlledEffectAttribute> _monsterControlledEffectAttrLookup;
        private ComponentLookup<HeroAttributeCmpt> _heroAttrLookup;
        private ComponentLookup<SkillsDamageCalPar> _skillDamage;
        private BufferLookup<HitRecord> _hitRecordBufferLookup;
        private ComponentLookup<LocalTransform> _transform;
        private BufferLookup<LinkedEntityGroup> _linkedLookup;
        private ComponentLookup<MonsterTempDamageText> _monsterTempDamageTextLookup;
        private ComponentLookup<MonsterTempDotDamageText> _monsterTempDotDamageTextLookup;
        //debuffer�˺������ѯ
        private ComponentLookup<MonsterDebuffAttribute> _monsterDebuffAttrLookup;
        //dot�����ѯ
        private BufferLookup<MonsterDotDamageBuffer> _monsterDotDamageBufferLookup;
        //���ϵͳ����
        private SystemHandle _detectionSystemHandle;



        void OnCreate(ref SystemState state)
        
        {
            //�ⲿ���Ƹ���
            state.RequireForUpdate<EnableHeroSkillsDamageSystemTag>();
            _liveMonster = SystemAPI.GetComponentLookup<LiveMonster>(true);
            _monsterDefenseAttrLookup = SystemAPI.GetComponentLookup<MonsterDefenseAttribute>(true);
            _monsterLossPoolAttrLookip = SystemAPI.GetComponentLookup<MonsterLossPoolAttribute>(true);
            _monsterControlledEffectAttrLookup = SystemAPI.GetComponentLookup<MonsterControlledEffectAttribute>(true);
            _heroAttrLookup = SystemAPI.GetComponentLookup<HeroAttributeCmpt>(true);
            _hitRecordBufferLookup = SystemAPI.GetBufferLookup<HitRecord>(true);
            _skillDamage = SystemAPI.GetComponentLookup<SkillsDamageCalPar>(true);
            _linkedLookup = SystemAPI.GetBufferLookup<LinkedEntityGroup>(true);
            _transform = SystemAPI.GetComponentLookup<LocalTransform>(true);
            _monsterTempDamageTextLookup = SystemAPI.GetComponentLookup<MonsterTempDamageText>(false);
            _monsterTempDotDamageTextLookup = SystemAPI.GetComponentLookup<MonsterTempDotDamageText>(false);
            _monsterDebuffAttrLookup = SystemAPI.GetComponentLookup<MonsterDebuffAttribute>(true);
            _monsterDotDamageBufferLookup = SystemAPI.GetBufferLookup<MonsterDotDamageBuffer>(true);


            _detectionSystemHandle = state.WorldUnmanaged.GetExistingUnmanagedSystem<DetectionSystem>();

        }
        [BurstCompile]   
        
        void OnUpdate(ref SystemState state)       
        {
            //������ز���
            _monsterDefenseAttrLookup.Update(ref state);
            _liveMonster.Update(ref state);
            _monsterControlledEffectAttrLookup.Update(ref state);
            _monsterLossPoolAttrLookip.Update(ref state);
            _heroAttrLookup.Update(ref state);
            _hitRecordBufferLookup.Update(ref state);
            _skillDamage.Update(ref state);
            _transform.Update(ref state);
            _linkedLookup.Update(ref state);
            _monsterTempDamageTextLookup.Update(ref state);
            _monsterTempDotDamageTextLookup.Update(ref state);
            _monsterDebuffAttrLookup.Update(ref state);
            _monsterDotDamageBufferLookup.Update(ref state);

            //��ȡ�ռ����絥��
            var detectionSystem = state.WorldUnmanaged.GetUnsafeSystemRef<DetectionSystem>(_detectionSystemHandle);
            var hitsArray = detectionSystem.skillHitMonsterArray;


            // 2. ����Ӧ���˺� ����& ��Ǽ��ܽ�����,��Ҫjob �ṹ
          // var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var ecbWriter = ecb.AsParallelWriter();
            state.Dependency = new ApplySkillDamageJob
            {
                ECB = ecbWriter,
                DamageParLookup = _skillDamage,
                DefenseAttrLookup = _monsterDefenseAttrLookup,
                LossPoolAttrLookup =_monsterLossPoolAttrLookip,
                ControlledEffectAttrLookup =_monsterControlledEffectAttrLookup,
                LinkedLookup = _linkedLookup,
                HitArray = hitsArray,
                HeroAttrLookup = _heroAttrLookup,
                RecordBufferLookup = _hitRecordBufferLookup,
                Transform = _transform,
                TempDamageText =_monsterTempDamageTextLookup,
                TempDotDamageText = _monsterTempDotDamageTextLookup,
                DebufferAttrLookup = _monsterDebuffAttrLookup,
                DotDamageBufferLookup = _monsterDotDamageBufferLookup
            }
            .Schedule(hitsArray.Length, 64, state.Dependency);

          //  state.Dependency.Complete();

            // 3. �طŲ�����
            //ecb.Playback(state.EntityManager);
            //ecb.Dispose();

            //4. ��ʱ�Լ��ܸĶ�
            //��ʱ����һ��
            state.Dependency = new ApplyHeroSkillPropBufferAggregatesJob
            {

                DamageTextLookop = _monsterTempDamageTextLookup,
                DamageDotTextLookop = _monsterTempDotDamageTextLookup,


            }.ScheduleParallel(state.Dependency);



        }

        void OnDestroy(ref SystemState state) { }

    }
   

    /// <summary>
    /// ��ÿ����ײ�ԣ���ȡ�˺���������Ѫ�����ٵ���
    /// </summary>
    [BurstCompile]
    struct ApplySkillDamageJob : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<SkillsDamageCalPar> DamageParLookup;
        [ReadOnly] public ComponentLookup<MonsterDefenseAttribute> DefenseAttrLookup;
        [ReadOnly] public ComponentLookup<MonsterControlledEffectAttribute> ControlledEffectAttrLookup;
        [ReadOnly] public ComponentLookup<MonsterLossPoolAttribute> LossPoolAttrLookup;
        [ReadOnly] public BufferLookup<LinkedEntityGroup> LinkedLookup;
        [ReadOnly] public NativeArray<TriggerPairData> HitArray;
        [ReadOnly] public ComponentLookup<HeroAttributeCmpt> HeroAttrLookup;
        //������¼buffer,���ߵ�buffer�Ǽ��ڷ��е�������
        [ReadOnly] public BufferLookup<HitRecord> RecordBufferLookup;
        //����λ����Ϣ
        [ReadOnly] public ComponentLookup<LocalTransform> Transform;
   
        //�˺�Ʈ��
        [ReadOnly] public ComponentLookup<MonsterTempDamageText> TempDamageText;
        //Dot�˺�Ʈ�ֵĶ���
        [ReadOnly] public ComponentLookup<MonsterTempDotDamageText> TempDotDamageText;
        //debuffer Ч��
        [ReadOnly] public ComponentLookup<MonsterDebuffAttribute> DebufferAttrLookup;
        //buffer�ۼ�
        [ReadOnly] public BufferLookup<MonsterDotDamageBuffer> DotDamageBufferLookup;

        public void Execute(int i)
        {
            // 1) �õ���ײʵ���
            var pair = HitArray[i];
            Entity skill = pair.EntityA;
            Entity target = pair.EntityB;
            if (!DamageParLookup.HasComponent(skill))
            {
                skill = pair.EntityB;
                target = pair.EntityA;
            }


            // �õ����ߵļ�¼����
            var buffer = RecordBufferLookup[skill];

            // �ȼ���Ƿ��Ѿ���¼����� target
            for (int j = 0; j < buffer.Length; j++)
            {
                //DevDebug.Log("buffer��--"+j +"   "+ buffer[j].timer);
                if (buffer[j].other == target)
                {
                    // DevDebug.Log("���ظ��ܾ�����");
                    return;
                }
            }
            // ֻ��û��¼�����żӽ���,����Ҫע�Ⲣ��д�����ƣ�ʹ�ò���д�뷽��         
            ECB.AppendToBuffer(i, skill, new HitRecord { other = target });

            // 2) ��ȡ��� & �����
            var d = DamageParLookup[skill];
            var a = DefenseAttrLookup[target];
            var c = ControlledEffectAttrLookup[target];
            var l = LossPoolAttrLookup[target];
            var h = HeroAttrLookup[d.heroRef];
            var db = DebufferAttrLookup[target];
            var dbd = DotDamageBufferLookup[target];
            //����ע����ȡ��ʱ�����ӵ�ʱ��һ���� ��ӵ�ʱ��決����֮��ò����û�ж���
            var textRenderEntity = LinkedLookup[target][2].Value;
            //������ȡDOT���˺�Ʈ��
            var textDotRenderEntity = LinkedLookup[target][3].Value;
            var tempText = TempDamageText[textRenderEntity];
            var tempDotText = TempDotDamageText[textDotRenderEntity];
            var rnd = new Unity.Mathematics.Random(a.rngState);

            // 3) �����ж�-����Ӧ��չ����������
            if (rnd.NextFloat() < a.dodge)
            {
                // 9) �����µ� RNG ״̬
                a.rngState = rnd.state;
                ECB.SetComponent(i, target, a);
                tempText.underAttack = true;
                tempText.damageTriggerType = DamageTriggerType.Miss;
                //д����������
                ECB.SetComponentEnabled<MonsterTempDamageText>(i, textRenderEntity, true);
                ECB.SetComponent(i, textRenderEntity, tempText);
                return;
            }


            //3+1) ����ϵͳ������������صĿ��Ʋ����� �ﵽ��ֵ֮���������Ч��
            //����ϵͳ���Ե������ڵķ�ʽ���У������Ǹ��ʷ�ʽ����
            //������ƿ���ÿ�λ���ˢ�£���ǿ�����Ʊ������������ۼ�

            //���٣� ���������Ϊ������ƣ�������ƻ���������
             c.slow += h.controlAbilityAttribute.slow;
             c.slowTimer = 0;
            //����,�����е���
            c.knockback = h.controlAbilityAttribute.knockback;
            c.knockbackTimer = 0;


            //״̬���ƣ����߿ɸ���ǰ��״̬��������״̬��ʶ�����ڣ����ڼ����˺�
            //�־�
           c.fear += h.controlAbilityAttribute.fear;
            //����
            c.root += h.controlAbilityAttribute.root;
            //����
            c.stun += h.controlAbilityAttribute.stun;
            //����
            c.freeze += h.controlAbilityAttribute.freeze;

            //�ɼ��ܱ�ǩ�ļ��ؾ���ǣ��״̬,ͨ�����Ի�õ��߻��߼��ܿ��Ի����ʱ��������ǣ�����߱�ըֵ
            //ǣ�����߱�ը��1��������Ϊ��buffer�ļ�������������жϲ����ܺ�ִ��
            //ǣ�����߱�ը��ֱ��ִ�У������ۼӽ��д���
            if (d.enablePull)
            {
                c.pull = h.controlAbilityAttribute.pull;
                 c.pullCenter = Transform[skill].Position;
         
            }
            //��ը
            if (d.enableExplosion)
            {
                c.explosion = h.controlAbilityAttribute.explosion;
                c.explosionCenter = Transform[skill].Position;
                //ǣ�����߱�ը������������Ϊ��buffer�ļ�������������жϲ����ܺ�ִ��            
            }
            //-- ��������
            // 4) ����������ġ�˲ʱ�˺����͡�DOT�˺���
            float instTotal = 0f, dotTotal = 0f;
            {
                // �����Ӽ���
                float CalcPhysicalSub(float armor, float breakVal, float pen)
                {
                    float eff = armor - (breakVal + math.max(0f, 1f - pen) * armor);
                    return eff / (eff + 100f);
                }

                // Ԫ���Ӽ���
                float CalcElementSub(float res, float breakVal, float pen)
                {
                    float eff = res - (breakVal + math.max(0f, 1f - pen) * res);
                    return eff / (eff + 50f);
                }

                // ����,���ϼ���Ч���Ļ�������,ԭ���Ĺֵļ�����Ч������ƣ��������������������������˵����ļ��ܣ���ƣ�
                float physSub = CalcPhysicalSub(a.armor - db.armorReduction,
                                                h.attackAttribute.armorBreak,
                                                h.attackAttribute.armorPenetration);
                instTotal += d.instantPhysicalDamage * (1f - physSub);
                dotTotal += d.bleedDotDamage * (1f - physSub);

                // ��
                float fireSub = CalcElementSub(a.resistances.fire - db.resistanceReduction.fire,
                                               h.attackAttribute.elementalBreak,
                                               h.attackAttribute.elementalPenetration);
                instTotal += d.fireDamage * (1f - fireSub);
                dotTotal += d.fireDotDamage * (1f - fireSub);

                // ��
                float frostSub = CalcElementSub(a.resistances.frost - db.resistanceReduction.frost,
                                                h.attackAttribute.elementalBreak,
                                                h.attackAttribute.elementalPenetration);
                instTotal += d.frostDamage * (1f - frostSub);
                dotTotal += d.frostDotDamage * (1f - frostSub);

                // ����
                float lightSub = CalcElementSub(a.resistances.lightning - db.resistanceReduction.lightning,
                                                h.attackAttribute.elementalBreak,
                                                h.attackAttribute.elementalPenetration);
                instTotal += d.lightningDamage * (1f - lightSub);
                dotTotal += d.lightningDotDamage * (1f - lightSub);

                // ����
                float poisonSub = CalcElementSub(a.resistances.poison - db.resistanceReduction.poison,
                                                 h.attackAttribute.elementalBreak,
                                                 h.attackAttribute.elementalPenetration);
                instTotal += d.poisonDamage * (1f - poisonSub);
                dotTotal += d.poisonDotDamage * (1f - poisonSub);

                // ��Ӱ
                float shadowSub = CalcElementSub(a.resistances.shadow - db.resistanceReduction.shadow,
                                                 h.attackAttribute.elementalBreak,
                                                 h.attackAttribute.elementalPenetration);
                instTotal += d.shadowDamage * (1f - shadowSub);
                dotTotal += d.shadowDotDamage * (1f - shadowSub);
            }


            // 5) �ػ���Ӧ������ raw dmg�������κμ��⣬�����ܹ��ˣ��������ӳػ�ֵ����
            l.attackTimer = 0.07f;

            float origHp = a.originalHp;
                const float mult = 20f, cap = 200f;
                //�ػ���ӦҲҪ�˶�Ӧ���˺�change����,10% �˺���Ӧ200��ػ�ֵ���Ҳ����κμ���Ӱ��
                float Gain(float raw, float dot) => math.min(((raw + dot)*(d.damageChangePar) / origHp) * 100f * mult, cap);
            var addFirePool = Gain(d.fireDamage, d.fireDotDamage);
            var addFrostPool = Gain(d.frostDamage, d.fireDotDamage);
            var addLightningPool = Gain(d.lightningDamage, d.lightningDotDamage);
            var addPosionPool = Gain(d.poisonDamage, d.poisonDotDamage);
            var addShadowPool = Gain(d.shadowDamage, d.shadowDotDamage);
            var addBleedPool = Gain(d.instantPhysicalDamage, d.bleedDotDamage) * 0.25f;


            l.firePool = math.min(l.firePool + addFrostPool, cap);
            l.frostPool = math.min(l.frostPool + addFirePool, cap);
            l.lightningPool = math.min(l.lightningPool + addLightningPool, cap);
            l.poisonPool = math.min(l.poisonPool + addPosionPool, cap);
            l.shadowPool = math.min(l.shadowPool + addShadowPool, cap);
            //��Ѫ��ֻʹ��25%�����˺�����
            l.bleedPool = math.min(l.bleedPool + addBleedPool, cap) * 0.25f;



            // 6) ���ж�������˲ʱ��,�������20%-80%�˺�
            var tempBlock = false;
            if (rnd.NextFloat() < a.block)
            {
                float br = math.lerp(0.2f, 0.8f, rnd.NextFloat());
                instTotal *= (1f - br);
                tempBlock = true;
            }

            // 7) �̶����ˣ���˲ʱ+DOT��0-50%�Ĺ̶�������ˣ����ڿ�����������),�����DOT�˺��Ǽ���������Ϳ���֮��,�������˺������debuffer
            var rd = math.lerp(0.0f, 0.5f, rnd.NextFloat());//�̶��������
            float finalDamage = (instTotal + dotTotal) * (1f - a.damageReduction) * (1 - rd) * (1 + db.damageAmplification);
            //�������dot�˺�
            float finalDotDamage = (dotTotal) * (1f - a.damageReduction) * (1 - rd) * (1 + db.damageAmplification);


            //��7-1��д��dot�˺��Ŀ�Ѫ����,����ͬ����buffer�ۼӷ�ʽ
            db.totalDotDamage += finalDotDamage;



            // 8) Ӧ�ÿ�Ѫ & д��
            a.hp = math.max(0f, a.hp - finalDamage);


            //8-1) �˺����ִ���
            //ȷ���յ�����
            tempText.underAttack = true;
            //�����˺�����
            tempText.hurtVlue = finalDamage;
            //����˲ʱ�˺�����,���ӵ����˺�ö�����ʹ�����ʱ�ṹ���У������˺��Ḳ��
            tempText.damageTriggerType = d.damageTriggerType;
            //д�ظ�,������������Ԫ�ж��ܱ�����Ϊ��֧�� csel ָ��
            tempText.damageTriggerType = tempBlock ? DamageTriggerType.Block : tempText.damageTriggerType;

            //-- DOT���ʹ���,������������


            //8-2 ���ﱣ��ԭ����д�룬 �����ں����Ĵ����н���Ҫ��������>=2��buffer��ͬʱ����ԭ�ṹ����
            var dd = new HeroSkillPropAccumulateData();
            dd.damage = finalDamage;//�ϲ�ʹ���˺����֣������job֮�����
            dd.slow = h.controlAbilityAttribute.slow;
            dd.fear = h.controlAbilityAttribute.fear;
            dd.root = h.controlAbilityAttribute.root;
            dd.stun = h.controlAbilityAttribute.stun;
            dd.freeze = h.controlAbilityAttribute.freeze;

            // ���ػ�����
            dd.firePool = addFirePool;
            dd.frostPool = addFrostPool;
            dd.lightningPool = addLightningPool;
            dd.poisonPool = addPosionPool;
            dd.shadowPool = addShadowPool;
            dd.bleedPool = addBleedPool;

            //8-3 д�붯̬Dotbuffer,���ռ����dot���˺���ʣ��ʱ��6��
            //����ò��ֻ������д �޷�SIMD�Ż�
            if (finalDotDamage > 0)
            {
                var tdbd = new MonsterDotDamageBuffer();
                tdbd.dotDamage = finalDotDamage;
                tdbd.survivalTime = 6;
                //�ۼӹ����ܵ���buffer
                ECB.AppendToBuffer(i, target, tdbd);
            }


            //������ɫ�仯״̬
            // 9) �ܻ�������SIMD ָ���Ż�
            {
                // ��ͳһһ����֡��  3f ֵ�ĳ���
                const float newTimerValue = 3f;

                // ��ÿ��Ԫ�ض���ͬ��������д�أ�job �в����޷�֧ʵ�֣�������������Ҫ
                // math.step(0, x) == (x > 0 ? 1f : 0f) �޷�֧ʵ��
                float frostMask = math.step(1e-6f, d.frostDamage);
                float fireMask = math.step(1e-6f, d.fireDamage);
                float poisonMask = math.step(1e-6f, d.poisonDamage);
                float lightningMask = math.step(1e-6f, d.lightningDamage);
                float shadowMask = math.step(1e-6f, d.shadowDamage);
                float bleedMask = math.step(1e-6f, d.instantPhysicalDamage);

                // Ȼ��һ����д�أ�ȫ�����ǵ�����ʽ��û�� if
                l.frostTimer = frostMask * newTimerValue + (1f - frostMask) * l.frostTimer;
                l.fireTimer = fireMask * newTimerValue + (1f - fireMask) * l.fireTimer;
                l.poisonTimer = poisonMask * newTimerValue + (1f - poisonMask) * l.poisonTimer;
                l.lightningTimer = lightningMask * newTimerValue + (1f - lightningMask) * l.lightningTimer;
                l.shadowTimer = shadowMask * newTimerValue + (1f - shadowMask) * l.shadowTimer;
                l.bleedTimer = bleedMask * newTimerValue + (1f - bleedMask) * l.bleedTimer;

                // 2) DOT ����أ������Ӧ dotDamage>0 ����Ϊ 6�����򱣳�ԭֵ
                float frostDotMask = math.step(1e-6f, d.frostDotDamage);
                float fireDotMask = math.step(1e-6f, d.fireDotDamage);
                float poisonDotMask = math.step(1e-6f, d.poisonDotDamage);
                float lightningDotMask = math.step(1e-6f, d.lightningDotDamage);
                float shadowDotMask = math.step(1e-6f, d.shadowDotDamage);
                float bleedDotMask = math.step(1e-6f, d.bleedDotDamage);

                // �� mask==1 ʱ����Ϊ 6f��mask==0 ʱ����֮ǰ��ֵ
                l.frostActive = frostDotMask * 6f + (1f - frostDotMask) * l.frostActive;
                l.fireActive = fireDotMask * 6f + (1f - fireDotMask) * l.fireActive;
                l.poisonActive = poisonDotMask * 6f + (1f - poisonDotMask) * l.poisonActive;
                l.lightningActive = lightningDotMask * 6f + (1f - lightningDotMask) * l.lightningActive;
                l.shadowActive = shadowDotMask * 6f + (1f - shadowDotMask) * l.shadowActive;
                l.bleedActive = bleedDotMask * 6f + (1f - bleedDotMask) * l.bleedActive;
            }

            // 9) �����µ� RNG ״̬
            a.rngState = rnd.state;
            ECB.SetComponent(i, target, a);
            ECB.SetComponent(i, target, l);
            ECB.SetComponent(i, target, c);
            //������ʱ˲ʱ�˺���
            ECB.SetComponentEnabled<MonsterTempDamageText>(i, textRenderEntity, true);
            ECB.SetComponent(i, textRenderEntity, tempText);
            //д�ص�debuffer��¼��dot�˺����Լ�һЩ����������Ч��
            ECB.SetComponent(i, target, db);
            //����ۼӵ�buffer ���ݣ����ͬ֡���ݲ���д�� ���ǵ�����
            ECB.AppendToBuffer(i, target, dd);


            //10) ��ǵ������٣������Ϳ���ִ�д�͸�߼��������س������
            {
                var pd = d;
                pd.destory = true;
                ECB.SetComponent(i, skill, pd);
            }

        }
    }


    /// <summary>
    /// ����ͬʱ������е�����ײ�������˺��Լ��ۼӳػ���������
    /// </summary>
    [BurstCompile]
    partial struct ApplyHeroSkillPropBufferAggregatesJob : IJobEntity
    {
        //����������������ȫ���������������������������Ҫʹ��ǰ����ʱ����һ��
        [NativeDisableParallelForRestriction]
        public ComponentLookup<MonsterTempDamageText> DamageTextLookop;
        [NativeDisableParallelForRestriction]
        public ComponentLookup<MonsterTempDotDamageText> DamageDotTextLookop;
        void Execute(
            Entity e,
            EnabledRefRO<LiveMonster> live,
            ref MonsterDefenseAttribute def,
            ref MonsterControlledEffectAttribute ctl,
            ref MonsterLossPoolAttribute pool,
            ref MonsterDebuffAttribute dot,
            ref DynamicBuffer<HeroSkillPropAccumulateData> accBuf,
            DynamicBuffer<LinkedEntityGroup> linkedEntity)
        {
            //����С��2��ʱ���Ѿ�д�벻��Ҫ�ۺ�
            if (accBuf.Length < 2)
            {
                //����֮ǰ���buffer
                accBuf.Clear();
                return;
            }

            // 1) �ۺ�
            var sum = new FlightPropAccumulateData();
            for (int i = 0; i < accBuf.Length - 1; i++)
            {
                var d = accBuf[i];
                sum.damage += d.damage;
                sum.dotDamage += sum.dotDamage;
                sum.slow += d.slow;
                sum.fear += d.fear;
                sum.root += d.root;
                sum.stun += d.stun;
                sum.freeze += d.freeze;
                sum.firePool += d.firePool;
                sum.frostPool += d.frostPool;
                sum.lightningPool += d.lightningPool;
                sum.poisonPool += d.poisonPool;
                sum.shadowPool += d.shadowPool;
                sum.bleedPool += d.bleedPool;
            }

            // 2) д��Ѫ��
            def.hp = math.max(0f, def.hp - sum.damage);



            // 3) д�ؿ���
            ctl.slow += sum.slow;
            ctl.fear += sum.fear;
            ctl.root += sum.root;
            ctl.stun += sum.stun;
            ctl.freeze += sum.freeze;


            // 4) д�سػ�ֵ
            if (true)
            {
                pool.firePool = math.min(pool.firePool + sum.firePool, 200);
                pool.frostPool = math.min(pool.frostPool + sum.frostPool, 200);
                pool.lightningPool = math.min(pool.lightningPool + sum.lightningPool, 200);
                pool.poisonPool = math.min(pool.poisonPool + sum.poisonPool, 200);
                pool.shadowPool = math.min(pool.shadowPool + sum.shadowPool, 200);
                pool.bleedPool = math.min(pool.bleedPool + sum.bleedPool, 200);
            }
            //5)д��dot���˺�
            dot.totalDotDamage = sum.dotDamage;


            //��������ȥ�������岢�Ҹ���
            var damageText = DamageTextLookop[linkedEntity[2].Value];
            //д���˺�
            damageText.hurtVlue += sum.damage;

            DamageTextLookop[linkedEntity[2].Value] = damageText;


            // 5) ��� buffer��Ϊ��һ֡����
            accBuf.Clear();

            // DevDebug.Log("���ۼӲ�����Լ���buffer");

        }
    }
}