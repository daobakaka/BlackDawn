using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using UnityEngine;
using Unity.VisualScripting;
using System.Linq;
//用于管理技能的生命周期及状态
//可以进行burstCompile 的无引用技能
namespace BlackDawn.DOTS
{
    /// <summary>
    /// 技能管理类,在特殊技能类之后进行更新
    /// </summary>
    //先伤害计算，再更新状态
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(MainThreadSystemGroup))]
    public partial struct HeroSkillsMonoSystem : ISystem, ISystemStartStop
    {
        //侦测系统缓存
        private SystemHandle _detectionSystemHandle;
        ComponentLookup<LocalTransform> _transform;
        ComponentLookup<MonsterLossPoolAttribute> _monsterLossPoolAttrLookup;
        ComponentLookup<HeroAttributeCmpt> _heroAttribute;
        ComponentLookup<SkillBlackFrameTag> _skillBlackFrameTagLookup;
        //获取技能道具上的buffer，用于实现暗影增吞噬效果
        BufferLookup<HitRecord> _hitBuffer;
        float3 _heroPosition;
        Entity _heroEntity;
        EntityManager _entityManager;
        HeroAttributeCmpt _heroAttributeCmptOriginal;

        ScenePrefabsSingleton _prefabs;

        public void OnCreate(ref SystemState state)
        {

            // state.Enabled = false;
            //由外部控制
            state.RequireForUpdate<EnableHeroSkillsMonoSystemTag>();
            state.Enabled = false;

            _transform = state.GetComponentLookup<LocalTransform>(true);
            _monsterLossPoolAttrLookup = state.GetComponentLookup<MonsterLossPoolAttribute>(false);
            _skillBlackFrameTagLookup = state.GetComponentLookup<SkillBlackFrameTag>(true);
            _heroAttribute = state.GetComponentLookup<HeroAttributeCmpt>(true);
            _entityManager = state.EntityManager;
            _detectionSystemHandle = state.WorldUnmanaged.GetExistingUnmanagedSystem<DetectionSystem>();

            // _hitBuffer = state.GetBufferLookup<HitRecord>(true);   

        }

        public void OnStartRunning(ref SystemState state)
        {
            _heroEntity = SystemAPI.GetSingletonEntity<HeroEntityMasterTag>();
            _heroAttributeCmptOriginal = Hero.instance.attributeCmpt;


            DevDebug.Log("重启SkillMono系统");
        }



        [BurstCompile]
        public void OnUpdate(ref SystemState state)

        {
            //更新位置
            _transform.Update(ref state);
            _monsterLossPoolAttrLookup.Update(ref state);
            _heroAttribute.Update(ref state);
            _skillBlackFrameTagLookup.Update(ref state);
            // _hitBuffer.Update(ref state);



            //主线成逻辑采用开头写
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var timer = SystemAPI.Time.DeltaTime;
            //后续需要更改,查询英雄的位置
            // var heroEntity = SystemAPI.GetSingletonEntity<HeroEntityMasterTag>();
            quaternion rot = _transform[_heroEntity].Rotation;
            _heroPosition = _transform[_heroEntity].Position;
            //获取英雄属性
            var heroPar = _heroAttribute[_heroEntity];
            //获取英雄装载的技能等级
            var level = _heroAttribute[_heroEntity].skillDamageAttribute.skillLevel;
            _prefabs = SystemAPI.GetSingleton<ScenePrefabsSingleton>();
            //获取收集世界单例
            var detectionSystem = state.WorldUnmanaged.GetUnsafeSystemRef<DetectionSystem>(_detectionSystemHandle);
            var thunderGripHitMonsterArray = detectionSystem.thunderGripHitMonsterArray;



            //脉冲技能处理
            foreach (var (skillTag, skillCal, transform, collider, entity)
                  in SystemAPI.Query<RefRW<SkillPulseTag>, RefRW<SkillsDamageCalPar>, RefRW<LocalTransform>, RefRW<PhysicsCollider>>().WithEntityAccess())
            {
                //更新标签的技能伤害参数，这里有动态的变化再更新
                //  skillCal.ValueRW.damageChangePar = skillTag.ValueRW.skillDamageChangeParTag;
                // 2) 计算“前向”世界向量
                float3 forward = math.mul(transform.ValueRO.Rotation, new float3(0f, 0f, 1f));
                // 3) 沿着前向移动
                transform.ValueRW.Position += forward * skillTag.ValueRW.speed * timer;

                skillTag.ValueRW.tagSurvivalTime -= timer;

                //满足时间大于3秒，oncheck关闭，且允许开启第二阶段 ，则添加第二阶段爆炸需求标签,取消销毁，留在爆炸渲染逻辑销毁
                if (skillTag.ValueRW.tagSurvivalTime <= 0)
                {
                    if (skillTag.ValueRW.enableSecond)
                        //直接开关标签，避免结构性改变
                        ecb.SetComponentEnabled<SkillPulseSecondExplosionRequestTag>(entity, true);
                    else
                    {


                        //ecb.DestroyEntity(entity);
                        skillCal.ValueRW.destory = true;

                    }
                }

            }
            //暗能技能处理,DymicalBuffer<...>这样只能拿到只读的，做更改需要在方法内部使用显示的SystemAPI 来执行
            //拆分组件以获得性能优势
            foreach (var (skillTag, skillCal, transform, collider, entity)
                 in SystemAPI.Query<RefRW<SkillDarkEnergyTag>, RefRW<SkillsDamageCalPar>, RefRW<LocalTransform>, RefRW<PhysicsCollider>>().WithEntityAccess())
            {

                // 2) 计算“前向”世界向量
                float3 forward = math.mul(transform.ValueRO.Rotation, new float3(0f, 0f, 1f));
                // 3) 沿着前向移动
                transform.ValueRW.Position += forward * skillTag.ValueRW.speed * timer;

                skillTag.ValueRW.tagSurvivalTime -= timer;


                // 5) 如果需要触发特殊效果，就遍历 hitBuffer，给每个元素的 universalJudgment 赋值
                if (skillTag.ValueRO.enableSpecialEffect)
                {
                    // 1) 每次循环先显式地用 entity 拿缓冲
                    var buffer = SystemAPI.GetBuffer<HitRecord>(entity);
                    // 2) 需要用一个临时变量进行更改之后再写回
                    if (skillTag.ValueRO.enableSpecialEffect)
                    {
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            if (!buffer[i].universalJudgment)
                            {
                                //这里只有没有判断过，就在下一次才能判断，节省不必要的开销，也可以累加暗影池
                                var monsterAttr = _monsterLossPoolAttrLookup[buffer[i].other];
                                HitRecord temp = buffer[i];
                                temp.universalJudgment = true;
                                //暗影值>50时吞取
                                if (monsterAttr.shadowPool > 50)
                                {
                                    //增加一次伤害参数
                                    skillCal.ValueRW.damageChangePar *= (1 + (monsterAttr.shadowPool * level / 10000));
                                    //设置怪物对应的暗影池的值为0
                                    monsterAttr.shadowPool = 0;
                                    //这里写回 仅修改一条， 后期考虑组件拆分
                                    ecb.SetComponent(buffer[i].other, monsterAttr);
                                    //这里播放暗影吞噬特效？                     
                                }
                                buffer[i] = temp;
                            }
                        }
                    }
                }
                //满足时间大于3秒，oncheck关闭，且允许开启第二阶段 ，则添加第二阶段爆炸需求标签,取消销毁，留在爆炸渲染逻辑销毁
                if (skillTag.ValueRW.tagSurvivalTime <= 0)
                {


                    // ecb.DestroyEntity(entity);
                    skillCal.ValueRW.destory = true;


                }

            }

            //冰火技能处理旋转
            foreach (var (skillTag, skillCal, transform, collider, entity)
            in SystemAPI.Query<RefRW<SkillIceFireTag>, RefRW<SkillsDamageCalPar>, RefRW<LocalTransform>, RefRW<PhysicsCollider>>().WithEntityAccess())
            {
                //更新标签的技能伤害参数这里有动态的变化再更新
                // skillCal.ValueRW.damageChangePar =skillTag.ValueRW.skillDamageChangeParTag;              
                float radius = skillTag.ValueRO.radius;
                ref float angle = ref skillTag.ValueRW.currentAngle;

                //如果开启第二阶段标识，且开启特殊效果,4秒执行一次爆炸判断
                if (skillTag.ValueRO.enableSecond && skillTag.ValueRO.secondSurvivalTime < 0)
                {

                    var buffer = SystemAPI.GetBuffer<HitRecord>(entity);
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        if (!buffer[i].universalJudgment)
                        {
                            //取消判断效果
                            HitRecord temp = buffer[i];
                            temp.universalJudgment = true;
                            buffer[i] = temp;
                            //设置爆炸效果
                            ecb.SetComponentEnabled<SkillIceFireSecondExplosionRequestTag>(entity, true);
                            //跳出for循环
                            break;
                        }
                    }

                }

                // 2) 计算角度增量（speed 为弧度/秒）
                float deltaAngle = skillTag.ValueRW.speed * timer;
                angle += deltaAngle;
                if (angle > math.PI * 2f) angle -= math.PI * 2f;

                // 3) 只在 XZ 平面计算新偏移
                float x = math.cos(angle) * radius;
                float z = math.sin(angle) * radius;

                // 4) 原来的 Y 不变
                float y = transform.ValueRO.Position.y;

                // 5) 把实体位置设为：英雄位置 + (x, 0, z)，再加上自身 Y
                transform.ValueRW.Position = new float3(
                    _heroPosition.x + x,
                    y,
                    _heroPosition.z + z
                );

                // 6) 持续减少存活时间并处理销毁或第二阶段逻辑
                skillTag.ValueRW.tagSurvivalTime -= timer;
                if (skillTag.ValueRW.tagSurvivalTime <= 0f)
                {

                    //  ecb.DestroyEntity(entity);
                    skillCal.ValueRW.destory = true;


                }
            }

            //落雷技能处理
            //这里到时间就消失
            foreach (var (skillTag, skillCal, transform, collider, entity)
                 in SystemAPI.Query<RefRW<SkillThunderStrikeTag>, RefRW<SkillsDamageCalPar>, RefRW<LocalTransform>, RefRW<PhysicsCollider>>().WithEntityAccess())
            {

                skillTag.ValueRW.tagSurvivalTime -= timer;
                if (skillTag.ValueRW.tagSurvivalTime < 0)
                {


                    //ecb.DestroyEntity(entity);
                    skillCal.ValueRW.destory = true;
                }


            }

            //法阵技能，可以手动关闭
            //二阶段遍历buffer,构建虹吸链接，链接根据动态效果改变长短？持续6秒自动消失，重新生成，还是按照buffer状态定义消失或者生成,这段逻辑在特殊技能类里面处理
            foreach (var (skillTag, skillCal, transform, collider, entity)
       in SystemAPI.Query<RefRW<SkillArcaneCircleTag>, RefRW<SkillsDamageCalPar>, RefRW<LocalTransform>, RefRW<PhysicsCollider>>().WithEntityAccess())
            {
                //三秒之后开始掉能量
                skillTag.ValueRW.tagSurvivalTime -= timer;
                if (skillTag.ValueRW.tagSurvivalTime <= 0)
                {
                    //钳制到0
                    heroPar.defenseAttribute.energy = math.max(0, heroPar.defenseAttribute.energy - 3 * timer);

                    if (heroPar.defenseAttribute.energy <= 0)
                    {
                        // ecb.DestroyEntity(entity);
                        skillCal.ValueRW.destory = true;
                    }
                    ecb.SetComponent(_heroEntity, heroPar);
                }
                //存在第二次释放手动关闭
                if (skillTag.ValueRO.closed)
                    skillCal.ValueRW.destory = true;
                // ecb.DestroyEntity(entity);

            }



            //进击的Mono效果
            SkillMonoAdvance(ref state, timer);
            //寒冰的Mono效果
            SkillMonoFrost(ref state, ecb);
            //黑炎的Mono效果-- 通过enbale 组件进行控制过滤
            SkillMonoBlackFrameA(ref state, timer);
            //元素共鸣Mono效果
            SkillMonoElementResonance(ref state, ecb);
            //技能静电牢笼
            SkillMonoElectroCage(ref state, ecb);

            // 暗影步 12
            SkillMonoShadowStep(ref state, ecb, timer);
            //暗影洪流15 B阶段，瞬时伤害特效控制
            SkillMonoMineBlastB(ref state);
            //时间缓速 16
            SKillMonoTimeSlow(ref state, timer);
            //连锁吞噬
            SkillMonoChainDevour(ref state, ecb);
            //雷霆之握
            SkillMonoThunderGrip(ref state, ecb);
            //时空扭曲B阶段 27
            SkillMonoChronoTwistB(ref state, timer);
            //烈焰爆发B阶段 28
            SkillFlameBurstB(ref state, timer);
            //暗影之刺 31
            SkillMonoShadowStap(ref state, timer, ecb);
            //幻影步 34
            SkillMonoPhantomStep(ref state, ecb, timer);







        }


        public void OnDestroy(ref SystemState state) { }

        public void OnStopRunning(ref SystemState state)
        {

        }

        /// <summary>
        /// 技能进击，通过mono中进行传统shader参数进行读取
        /// </summary>
        /// <param name="state"></param>
        /// <param name="ecb"></param>
        void SkillMonoAdvance(ref SystemState state, float timer)
        {
            foreach (var (skillTag, heroPar, transform, entity)
             in SystemAPI.Query<RefRW<SkillAdvanceTag_Hero>, RefRW<HeroAttributeCmpt>, RefRW<LocalTransform>>().WithEntityAccess())
            {
                //进击技能开启时 的掉能量的状态
                if (skillTag.ValueRW.active)
                {
                    skillTag.ValueRW.tagSurvivalTime -= timer;
                    //伤害增加30%
                    heroPar.ValueRW.defenseAttribute.tempDefense.advanceDamageReduction = 0.3f;
                    //每秒恢复300%元素系数生命值
                    heroPar.ValueRW.defenseAttribute.hp += (heroPar.ValueRW.attackAttribute.elementalDamage.frostDamage +
                    heroPar.ValueRW.attackAttribute.elementalDamage.fireDamage +
                    heroPar.ValueRW.attackAttribute.elementalDamage.poisonDamage +
                    heroPar.ValueRW.attackAttribute.elementalDamage.lightningDamage +
                    heroPar.ValueRW.attackAttribute.elementalDamage.shadowDamage) * 3;

                    if (skillTag.ValueRW.tagSurvivalTime <= 0)
                    {
                        //每秒降低三点能量
                        heroPar.ValueRW.defenseAttribute.energy = math.max(0, heroPar.ValueRW.defenseAttribute.energy - 10 * timer);

                        // 能量为0 则关闭进击，恢复伤害减免
                        if (heroPar.ValueRW.defenseAttribute.energy <= 0)
                        {
                            skillTag.ValueRW.active = false;
                            heroPar.ValueRW.defenseAttribute.tempDefense.advanceDamageReduction = 0;
                        }

                    }
                    //开启A阶段后， 增加冷却缩减以及伤害
                    if (skillTag.ValueRW.enableSecondA)
                    {
                        heroPar.ValueRW.gainAttribute.dymicalCooldownReduction.advanceACooldownReduction = 0.3f;

                        heroPar.ValueRW.attackAttribute.heroDynamicalAttack.tempAdvanceADamagePar = 0.2f;
                    }


                }
                else
                {
                    heroPar.ValueRW.defenseAttribute.tempDefense.advanceDamageReduction = 0;

                    if (skillTag.ValueRW.enableSecondA)
                    {
                        heroPar.ValueRW.gainAttribute.dymicalCooldownReduction.advanceACooldownReduction = 0.0f;

                        heroPar.ValueRW.attackAttribute.heroDynamicalAttack.tempAdvanceADamagePar = 0.0f;
                    }

                }

            }



        }
        //技能时间缓速
        void SKillMonoTimeSlow(ref SystemState state, float timer)
        {
            foreach (var (skillTag, heroPar, transform, entity)
                        in SystemAPI.Query<RefRW<SkillTimeSlowTag_Hero>, RefRW<HeroAttributeCmpt>, RefRW<LocalTransform>>().WithEntityAccess())
            {

                skillTag.ValueRW.tagSurvivalTime -= timer;
                if (skillTag.ValueRW.active)
                {
                    if (skillTag.ValueRW.tagSurvivalTime <= 0)
                    {
                        //每秒降低5点灵能
                        heroPar.ValueRW.defenseAttribute.energy -= 10 * timer;
                    }
                    //单次初始化的结果
                    if (!skillTag.ValueRW.initialized)
                    {
                        skillTag.ValueRW.initialized = true;
                        heroPar.ValueRW.gainAttribute.energyRegen *= 1.5f;
                        heroPar.ValueRW.gainAttribute.hpRegen *= 2f;
                        heroPar.ValueRW.defenseAttribute.moveSpeed *= 2f;
                        if (skillTag.ValueRW.enableSecondA)
                        {
                            //这里提供的冷却缩减是基础冷却缩减，非进击式的动态冷却缩减--这种加成应该更高，属于1类加成
                            heroPar.ValueRW.gainAttribute.cooldownReduction += (0.35f + 0.05f * skillTag.ValueRW.level);
                        }
                        if (skillTag.ValueRW.enableSecondB)
                        {
                            //提升的是基础攻速-这种加成应该更高，属于一类加成
                            heroPar.ValueRW.attackAttribute.attackSpeed += (0.5f + 0.2f * skillTag.ValueRW.level);
                        }
                    }
                }
                else
                {
                    //非active 状态即重置 skillTag 
                    skillTag.ValueRW.initialized = false;
                    heroPar.ValueRW.gainAttribute.energyRegen = _heroAttributeCmptOriginal.gainAttribute.energyRegen;
                    heroPar.ValueRW.gainAttribute.hpRegen = _heroAttributeCmptOriginal.gainAttribute.hpRegen;
                    heroPar.ValueRW.defenseAttribute.moveSpeed = _heroAttributeCmptOriginal.defenseAttribute.moveSpeed;
                    heroPar.ValueRW.attackAttribute.attackSpeed = _heroAttributeCmptOriginal.attackAttribute.attackSpeed;
                    heroPar.ValueRW.gainAttribute.cooldownReduction = _heroAttributeCmptOriginal.gainAttribute.cooldownReduction;

                }
                if (heroPar.ValueRW.defenseAttribute.energy <= 0.01f)
                {

                    skillTag.ValueRW.active = false;

                }



            }


        }
        /// <summary>
        /// 烈焰爆发B
        /// </summary>
        /// <param name="state"></param>
        /// <param name="timer"></param>
        void SkillFlameBurstB(ref SystemState state, float timer)
        {

            foreach (var (skillTag, heroPar, transform, entity)
            in SystemAPI.Query<RefRW<SkillFlameBurst_Hero>, RefRW<HeroAttributeCmpt>, RefRW<LocalTransform>>().WithEntityAccess())

            {
                skillTag.ValueRW.tagSurvivalTime -= timer;
                if (skillTag.ValueRW.tagSurvivalTime <= 0)
                    heroPar.ValueRW.attackAttribute.heroDynamicalAttack.tempFlameBurstBDamagePar = 0;//持续时间标记，小于0 则伤害增强清零


            }


        }

        //技能时空扭曲B阶段 瞬时伤害 ,暂定持续0.5秒销毁
        void SkillMonoChronoTwistB(ref SystemState state, float timer)
        {

            foreach (var (skillTag, skillPar, transform, entity)
            in SystemAPI.Query<RefRW<SkillChronoTwistBTag>, RefRW<SkillsDamageCalPar>, RefRW<LocalTransform>>().WithEntityAccess())
            {
                skillTag.ValueRW.tagSurvivalTime -= timer;
                if (skillTag.ValueRW.tagSurvivalTime <= 0)
                    skillPar.ValueRW.destory = true;

                float3 forward = math.mul(transform.ValueRO.Rotation, new float3(0, 0, 1)); // Z轴为前
                transform.ValueRW.Position += forward * skillTag.ValueRO.speed * timer;

            }


        }

        //暗影之刺 31 分裂在特殊类里面执行？支线程or主线程OR并行线程
        void SkillMonoShadowStap(ref SystemState state, float timer, EntityCommandBuffer ecb)
        {
            foreach (var (skillTag, skillPar, transform, entity)
              in SystemAPI.Query<RefRW<SkillShadowStabTag>, RefRW<SkillsDamageCalPar>, RefRW<LocalTransform>>().WithEntityAccess())
            {

                skillTag.ValueRW.tagSurvivalTime -= timer;
                if (skillTag.ValueRW.tagSurvivalTime <= 0)
                    skillPar.ValueRW.destory = true;
                var forward = math.mul(transform.ValueRO.Rotation, new float3(0, 0, 1));
                transform.ValueRW.Position += forward * timer * skillTag.ValueRO.speed;
                skillPar.ValueRW.timer += timer;


                //命中 且未初始化时
                if (skillPar.ValueRO.hit && !skillTag.ValueRW.initialized)
                {
                    skillPar.ValueRW.hit = false;
                    skillTag.ValueRW.initialized = true;
                    for (int j = 0; j < 2; j++)
                    {

                        var entityShadowStab = ecb.Instantiate(_prefabs.HeroSkill_ShadowStab);

                        var trs = transform.ValueRW;
                        trs.Scale = 1;
                        var baseRot = transform.ValueRW.Rotation;

                        // 左右各30度偏转（j=0: -30°, j=1: +30°）
                        float offsetDeg = -30 + (j * 60);
                        quaternion offsetRot = quaternion.RotateY(math.radians(offsetDeg));

                        // 最终旋转：当前朝向 * 偏移量
                        trs.Rotation = math.mul(baseRot, offsetRot);
                        ecb.SetComponent(entityShadowStab, trs);
                        //添加暗影之刺的标签,赋予伤害标签的伤害值
                        ecb.AddComponent(entityShadowStab, new SkillShadowStabTag() { speed = 20, tagSurvivalTime = 4, splitCount = 0, initialized = true,enableSecondA=skillTag.ValueRO.enableSecondA,
                        enableSecondB=skillTag.ValueRO.enableSecondB,skillDamageChangeParTag=skillTag.ValueRO.skillDamageChangeParTag,secondAChance=skillTag.ValueRO.secondAChance});
                        var newCal = skillPar.ValueRW;
                        ecb.AddComponent(entityShadowStab, newCal);

                        var hits = ecb.AddBuffer<HitRecord>(entityShadowStab);
                        ecb.AddBuffer<HitElementResonanceRecord>(entityShadowStab);

                    }
                }

                //开启A阶段之后，没有开始次级分裂的道具，进行分裂判断，判断结果为1
                if (skillTag.ValueRO.enableSecondA)
                {
                    if (skillPar.ValueRO.hit && skillTag.ValueRW.splitCount <= 0&&skillPar.ValueRW.timer > 0.5f)
                    {
                        skillPar.ValueRW.hit = false;
                        skillPar.ValueRW.timer = 0;
                        skillTag.ValueRW.splitCount += 1;
                        if (UnityEngine.Random.Range(0, 1f) <= skillTag.ValueRW.secondAChance)
                            for (int j = 0; j < 2; j++)
                            {

                                var entityShadowStab = ecb.Instantiate(_prefabs.HeroSkill_ShadowStab);
                                var trs = transform.ValueRW;
                                trs.Scale = 1;
                                var baseRot = transform.ValueRW.Rotation;

                                // 左右各30度偏转（j=0: -30°, j=1: +30°）
                                float offsetDeg = -30 + (j * 60);
                                quaternion offsetRot = quaternion.RotateY(math.radians(offsetDeg));

                                // 最终旋转：当前朝向 * 偏移量
                                trs.Rotation = math.mul(baseRot, offsetRot);
                                ecb.SetComponent(entityShadowStab, trs);
                                //添加暗影之刺的标签,赋予伤害标签的伤害值
                        ecb.AddComponent(entityShadowStab, new SkillShadowStabTag() { speed = 20, tagSurvivalTime = 4, splitCount = skillTag.ValueRW.splitCount, initialized = true,enableSecondA=skillTag.ValueRO.enableSecondA,
                        enableSecondB=skillTag.ValueRO.enableSecondB,skillDamageChangeParTag=skillTag.ValueRO.skillDamageChangeParTag,secondAChance=skillTag.ValueRO.secondAChance});
                                var newCal = skillPar.ValueRW;
                                ecb.AddComponent(entityShadowStab, newCal);

                                var hits = ecb.AddBuffer<HitRecord>(entityShadowStab);
                                ecb.AddBuffer<HitElementResonanceRecord>(entityShadowStab);

                            }
                    }

                }

                //开启B阶段之后，针对已经已经过次级分裂，没有开始次次级分裂的道具，进行分裂判断，判断结果为2，B阶段的值为0.5f
                if (skillTag.ValueRO.enableSecondB)
                {
                    if (skillPar.ValueRO.hit && skillTag.ValueRW.splitCount <= 1 && skillTag.ValueRW.splitCount > 0&&skillPar.ValueRW.timer > 0.5f)
                    {
                        skillPar.ValueRW.hit = false;
                        skillPar.ValueRW.timer = 0;
                        skillTag.ValueRW.splitCount += 1;
                        if (UnityEngine.Random.Range(0, 1f) <= 0.5f)//次次级分裂概率50%
                            for (int j = 0; j < 2; j++)
                            {

                                var entityShadowStab = ecb.Instantiate(_prefabs.HeroSkill_ShadowStab);
                                var trs = transform.ValueRW;
                                trs.Scale = 1;
                                var baseRot = transform.ValueRW.Rotation;

                                // 左右各30度偏转（j=0: -30°, j=1: +30°）
                                float offsetDeg = -30 + (j * 60);
                                quaternion offsetRot = quaternion.RotateY(math.radians(offsetDeg));

                                // 最终旋转：当前朝向 * 偏移量
                                trs.Rotation = math.mul(baseRot, offsetRot);
                                ecb.SetComponent(entityShadowStab, trs);
                                //添加暗影之刺的标签,赋予伤害标签的伤害值
                                ecb.AddComponent(entityShadowStab, new SkillShadowStabTag() { speed = 20, tagSurvivalTime = 4, splitCount = skillTag.ValueRW.splitCount, initialized = true,enableSecondA=skillTag.ValueRO.enableSecondA,
                        enableSecondB=skillTag.ValueRO.enableSecondB,skillDamageChangeParTag=skillTag.ValueRO.skillDamageChangeParTag,secondAChance=skillTag.ValueRO.secondAChance});
                                var newCal = skillPar.ValueRW;
                                newCal.damageChangePar *= skillTag.ValueRO.skillDamageChangeParTag;
                                ecb.AddComponent(entityShadowStab, newCal);
                                var hits = ecb.AddBuffer<HitRecord>(entityShadowStab);
                                ecb.AddBuffer<HitElementResonanceRecord>(entityShadowStab);

                            }
                    }
                    //次次级之后的分裂，由幸运一击触发，幸运一击可以溢出,判断内置0.5fCD,由幸运一击产生的暗影之刺最多分裂5次
                    if (skillPar.ValueRO.hit && skillTag.ValueRW.splitCount >= 2 && skillPar.ValueRW.timer > 0.5f&&skillTag.ValueRW.splitCount<7)
                    {
                        skillPar.ValueRW.hit = false;
                        skillPar.ValueRW.timer = 0;
                        skillTag.ValueRW.splitCount += 1;
                        if (UnityEngine.Random.Range(0, 1f) <= 0.5f * 0.25 * _heroAttribute[_heroEntity].attackAttribute.luckyStrikeChance)//0.5幸运一击*0.25幸运一击命中*幸运一击几率
                            for (int j = 0; j < 2; j++)
                            {

                                var entityShadowStab = ecb.Instantiate(_prefabs.HeroSkill_ShadowStab);
                                var trs = transform.ValueRW;
                                trs.Scale = 1;
                                var baseRot = transform.ValueRW.Rotation;

                                // 左右各30度偏转（j=0: -30°, j=1: +30°）
                                float offsetDeg = -30 + (j * 60);
                                quaternion offsetRot = quaternion.RotateY(math.radians(offsetDeg));

                                // 最终旋转：当前朝向 * 偏移量
                                trs.Rotation = math.mul(baseRot, offsetRot);
                                ecb.SetComponent(entityShadowStab, trs);

                                skillTag.ValueRW.skillDamageChangeParTag *= 0.9f;//次次级分裂伤害递减10%
                                //添加暗影之刺的标签,赋予伤害标签的伤害值
                                ecb.AddComponent(entityShadowStab, new SkillShadowStabTag() { speed = 20, tagSurvivalTime = 4, splitCount = skillTag.ValueRW.splitCount, initialized = true,enableSecondA=skillTag.ValueRO.enableSecondA,
                        enableSecondB=skillTag.ValueRO.enableSecondB,skillDamageChangeParTag=skillTag.ValueRO.skillDamageChangeParTag,secondAChance=skillTag.ValueRO.secondAChance});
                                var newCal = skillPar.ValueRW;
                                newCal.damageChangePar *= skillTag.ValueRO.skillDamageChangeParTag;
                                ecb.AddComponent(entityShadowStab, newCal);

                                var hits = ecb.AddBuffer<HitRecord>(entityShadowStab);
                                ecb.AddBuffer<HitElementResonanceRecord>(entityShadowStab);

                            }
                    }


                }


            }

        }

        //特殊， 检查激活A阶段怪物身上带有的黑炎标签， 进行其抗性持降低的计算
        //检查激活B阶段怪物身上带有的黑炎标签，进行其伤害加深的计算
        void SkillMonoBlackFrameA(ref SystemState state, float timer)
        {
            int level = 0;

            foreach (var skillTag in SystemAPI.Query<RefRO<SkillBlackFrameTag>>())
            {
                level = skillTag.ValueRO.level;
                return;
            }

            foreach (var (preSkillTag, defense, transform, entity)
            in SystemAPI.Query<RefRW<PreDefineHeroSkillBlackFrameATag>, RefRW<MonsterDefenseAttribute>, RefRW<LocalTransform>>().WithEntityAccess())
            {

                defense.ValueRW.resistances.fire -= timer * (1 + level * 0.01f);
                defense.ValueRW.resistances.frost -= timer * (1 + level * 0.01f);
                defense.ValueRW.resistances.lightning -= timer * (1 + level * 0.01f);
                defense.ValueRW.resistances.poison -= timer * (1 + level * 0.01f);
                defense.ValueRW.resistances.shadow -= timer * (1 + level * 0.01f);

            }
            foreach (var (preSkillTag, debuff, transform, entity)
           in SystemAPI.Query<RefRW<PreDefineHeroSkillBlackFrameBTag>, RefRW<MonsterDebuffAttribute>, RefRW<LocalTransform>>().WithEntityAccess())
            {

                debuff.ValueRW.damageAmplification += timer * 0.001f;

            }

        }
        /// <summary>
        /// 寒冰
        /// </summary>
        /// <param name="state"></param>
        /// <param name="ecb"></param>
        void SkillMonoFrost(ref SystemState state, EntityCommandBuffer ecb)
        {


            //一阶性状控制
            foreach (var (skillTag, skillCal, transform, collider, entity)
              in SystemAPI.Query<RefRW<SkillFrostTag>, RefRW<SkillsDamageCalPar>, RefRW<LocalTransform>, RefRW<PhysicsCollider>>().WithEntityAccess())
            {
                // 1. 缓存发射时的原点和开始时间（只在第一次执行时写入）
                if (skillTag.ValueRO.tagSurvivalTime == 10)
                {
                    skillTag.ValueRW.originalPosition = transform.ValueRO.Position;
                }
                // 2. 增加存活时间
                skillTag.ValueRW.tagSurvivalTime -= SystemAPI.Time.DeltaTime;
                // 3. 读取 origin、t
                float3 origin = skillTag.ValueRO.originalPosition;
                float t = 10 - skillTag.ValueRO.tagSurvivalTime;

                // 5. 用 speed 直接作为线速度，半径 r = speed * t
                float r = skillTag.ValueRO.speed * t;
                // 6. 角速度保持不变（按需修改）
                float angularSpeed = math.radians(90f); // 90°/s
                float theta = angularSpeed * t;

                // 7. 在 XZ 平面计算螺旋偏移（Y 不变）
                float3 offset = new float3(
                    r * math.cos(theta),
                    0f,
                    r * math.sin(theta)
                );
                // 7. 更新位置
                transform.ValueRW.Position = origin + offset;

                // 8. 超时销毁
                if (t >= 10f)
                {
                    ecb.DestroyEntity(entity);
                }

                //开启第二阶段，寒冰碎片能力
                if (skillTag.ValueRO.enableSecond)
                {
                    //生成 不同数量寒冰碎片
                    if (skillCal.ValueRW.hit == true && skillTag.ValueRW.hitCount > 0)
                    {
                        skillCal.ValueRW.hit = false;

                        //激发一次寒冰碎片的计算

                        skillTag.ValueRW.hitCount--;

                        for (int i = 0; i < skillTag.ValueRO.shrapnelCount; i++)
                        {

                            var fragIce = ecb.Instantiate(_prefabs.HeroSkillAssistive_Frost);

                            var trs = transform.ValueRW;
                            trs.Scale = 1;
                            float angleDeg = 360f / skillTag.ValueRO.shrapnelCount * i;
                            float angleRad = math.radians(angleDeg);

                            // 3. 生成绕 Y 轴的四元数，并赋给 trs.Rotation
                            trs.Rotation = quaternion.EulerXYZ(0f, angleRad, 0f);
                            ecb.AddComponent(fragIce, trs);
                            //添加寒冰碎片的标签,赋予伤害标签的伤害值
                            ecb.AddComponent(fragIce, new SkillFrostShrapneTag() { speed = 20, tagSurvivalTime = 1 });
                            var newCal = skillCal.ValueRW;
                            // 2. 修改字段，寒冰碎片继承20%冻结值
                            newCal.damageChangePar = skillTag.ValueRO.skillDamageChangeParTag;
                            if (skillTag.ValueRO.enableSpecialEffect)
                                newCal.tempFreeze = 20;

                            // 3. 把改好的组件整包给实体  
                            ecb.AddComponent(fragIce, newCal);

                            var hits = ecb.AddBuffer<HitRecord>(fragIce);
                            hits.Capacity = 10;
                            ecb.AddBuffer<HitElementResonanceRecord>(fragIce);

                        }

                    }

                }

            }


            //二阶性状控制
            foreach (var (skillTag, skillCal, transform, collider, entity)
           in SystemAPI.Query<RefRW<SkillFrostShrapneTag>, RefRW<SkillsDamageCalPar>, RefRW<LocalTransform>, RefRW<PhysicsCollider>>().WithEntityAccess())
            {

                // 2) 计算“前向”世界向量
                float3 forward = math.mul(transform.ValueRO.Rotation, new float3(0f, 0f, 1f));
                // 3) 沿着前向移动
                transform.ValueRW.Position += forward * skillTag.ValueRW.speed * SystemAPI.Time.DeltaTime;

                skillTag.ValueRW.tagSurvivalTime -= SystemAPI.Time.DeltaTime;

                if (skillTag.ValueRO.tagSurvivalTime <= 0)
                {

                    // ecb.DestroyEntity(entity);
                    skillCal.ValueRW.destory = true;
                }



            }


        }



        /// <summary>
        /// 元素共鸣
        /// </summary>
        /// <param name="state"></param>
        /// <param name="ecb"></param>
        void SkillMonoElementResonance(ref SystemState state, EntityCommandBuffer ecb)
        {

            foreach (var (skillTag, skillCal, transform, collider, entity)
            in SystemAPI.Query<RefRW<SkillElementResonanceTag>, RefRW<SkillsDamageCalPar>, RefRW<LocalTransform>, RefRW<PhysicsCollider>>().WithEntityAccess())
            {

                skillTag.ValueRW.tagSurvivalTime -= SystemAPI.Time.DeltaTime;

                if (skillTag.ValueRW.tagSurvivalTime <= 0)
                    // ecb.DestroyEntity(entity);
                    skillCal.ValueRW.destory = true;

            }


        }

        /// <summary>
        /// 静电牢笼，持续4秒？
        /// </summary>
        /// <param name="state"></param>
        /// <param name="ecb"></param>
        void SkillMonoElectroCage(ref SystemState state, EntityCommandBuffer ecb)
        {
            var rng = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, int.MaxValue));

            foreach (var (skillTag, damagePar, transform, collider, entity)
            in SystemAPI.Query<RefRW<SkillElectroCageTag>, RefRW<SkillsDamageCalPar>, RefRW<LocalTransform>, RefRW<PhysicsCollider>>().WithEntityAccess())
            {

                skillTag.ValueRW.tagSurvivalTime -= SystemAPI.Time.DeltaTime;


                if (skillTag.ValueRW.tagSurvivalTime <= 0)
                {
                    // ecb.DestroyEntity(entity);
                    damagePar.ValueRW.destory = true;
                    continue;
                }
                //第二阶段雷暴牢笼
                if (skillTag.ValueRO.enableSecondA)
                {
                    skillTag.ValueRW.timerA += SystemAPI.Time.DeltaTime;

                    if (skillTag.ValueRW.timerA >= skillTag.ValueRW.intervalTimer)
                    {
                        skillTag.ValueRW.timerA = 0;

                        //  1.实例化新牢笼电弧
                        var arcEntity = ecb.Instantiate(_prefabs.HeroSkillAssistive_ElectroCage_Lightning);

                        // 2. LocalTransform 随机偏移 XZ ±10
                        var newTransform = transform.ValueRO;
                        float xOffset = rng.NextFloat(-7f, 7f);
                        float zOffset = rng.NextFloat(-7f, 7f);
                        newTransform.Position.x += xOffset;
                        newTransform.Position.z += zOffset;
                        ecb.SetComponent(arcEntity, newTransform);

                        // 3. 构造伤害参数（复制+定制）
                        var newSkillPar = damagePar.ValueRO;
                        //第二阶段进行雷暴增伤
                        newSkillPar.damageChangePar = skillTag.ValueRW.skillDamageChangeParTag;

                        ecb.AddComponent(arcEntity, newSkillPar);
                        //添加雷暴存活印记
                        ecb.AddComponent(arcEntity, new SkillElectroCageScoendTag() { tagSurvivalTime = 1 });

                        // 4. 添加碰撞记录缓冲区
                        ecb.AddBuffer<HitRecord>(arcEntity);
                        ecb.AddBuffer<HitElementResonanceRecord>(arcEntity);


                    }


                }
                //第三阶段导电牢笼,两秒一次的概率判断
                if (skillTag.ValueRO.enableSecondB)
                {
                    skillTag.ValueRW.timerB += SystemAPI.Time.DeltaTime;


                    if (skillTag.ValueRW.timerB >= 1.99)
                    {
                        skillTag.ValueRW.timerB = 0;

                        var random = rng.NextFloat(0, 1);
                        //概率每次降低20%
                        if (random <= (0.5 - skillTag.ValueRO.StackCount * 0.05f))
                        {
                            float3 Offset = rng.NextFloat3(-15f, 15f);
                            //增加一次传导次数
                            skillTag.ValueRW.StackCount += 1;
                            float3 newPosition = transform.ValueRO.Position + new float3(Offset.x, 0, Offset.z);

                            var entityElectroCage = DamageSkillsECSRelaseProp(ecb, _prefabs.HeroSkill_ElectroCage, damagePar.ValueRO, newPosition, quaternion.identity);
                            int nextStackCount = skillTag.ValueRO.StackCount + 1;
                            if (skillTag.ValueRO.enableSecondA)
                            {
                                ecb.AddComponent(entityElectroCage, new SkillElectroCageTag() { tagSurvivalTime = 4, enableSecondA = true, enableSecondB = true, skillDamageChangeParTag = 2, intervalTimer = 0.2f, StackCount = nextStackCount });
                            }
                            else
                            {
                                ecb.AddComponent(entityElectroCage, new SkillElectroCageTag() { tagSurvivalTime = 4, enableSecondB = true, StackCount = nextStackCount });
                            }
                        }
                    }
                }

            }

            //雷暴消除逻辑
            foreach (var (skillTag, skillCal, entity)
          in SystemAPI.Query<RefRW<SkillElectroCageScoendTag>, RefRW<SkillsDamageCalPar>>().WithEntityAccess())
            {

                skillTag.ValueRW.tagSurvivalTime -= SystemAPI.Time.DeltaTime;
                if (skillTag.ValueRO.tagSurvivalTime <= 0)
                {
                    skillCal.ValueRW.destory = true;
                    //ecb.DestroyEntity(entity);

                    continue;

                }

            }

        }

        /// <summary>
        ///毒爆地雷的Mono 控制？或许直接写在回调类里面？爆炸之后重新更新时间？
        /// </summary>
        /// <param name="state"></param>
        /// <param name="ecb"></param>

        void SkillMonoMineBlastB(ref SystemState state)
        {
            foreach (var (skillTag, skillCal, entity)
             in SystemAPI.Query<RefRW<SkillShadowTideBTag>, RefRW<SkillsBurstDamageCalPar>>().WithEntityAccess())
            {
                skillTag.ValueRW.tagSurvivalTime -= SystemAPI.Time.DeltaTime;
                skillCal.ValueRW.burstTime += SystemAPI.Time.DeltaTime;

                if (skillTag.ValueRW.tagSurvivalTime <= 0)
                    skillCal.ValueRW.destory = true;


            }



        }

        /// <summary>
        /// 暗影洪流第二阶段
        /// </summary>
        /// <param name="state"></param>
        /// <param name="ecb"></param>
        void SkillShadowTideBMono(ref SystemState state, EntityCommandBuffer ecb, float3 heroPosition)
        {



        }

        /// <summary>
        /// 雷霆之握
        /// </summary>
        /// <param name="state"></param>
        /// <param name="ecb"></param>
        void SkillMonoThunderGrip(ref SystemState state, EntityCommandBuffer ecb)
        {

            //遍历雷霆之握技能
            foreach (var (skillTag, skillCal, transform, entity)
                in SystemAPI.Query<RefRW<SkillThunderGripTag>, RefRW<SkillsDamageCalPar>, RefRW<LocalTransform>>().WithEntityAccess())
            {
                // 1) 更新存活时间
                skillTag.ValueRW.tagSurvivalTime -= SystemAPI.Time.DeltaTime;

                // 2) 如果存活时间小于等于0，销毁实体
                if (skillTag.ValueRW.tagSurvivalTime <= 0f)
                {
                    // ecb.DestroyEntity(entity);
                    skillCal.ValueRW.destory = true;
                    continue;
                }
            }

            //雷霆之握控制怪物移动
            foreach (var (liveMonster, transform, debuff, preDefineSkillTag, entity)
                in SystemAPI.Query<RefRW<LiveMonster>, RefRW<LocalTransform>, RefRW<MonsterDebuffAttribute>, RefRW<PreDefineHeroSkillThunderGripTag>>().WithEntityAccess())
            {

                debuff.ValueRW.thunderGripEndTimer += SystemAPI.Time.DeltaTime;
                float3 monsterPos = transform.ValueRO.Position;
                float3 targetPos = debuff.ValueRO.thunderPosition;
                float3 toTarget = targetPos - monsterPos;  // 应该是目标位置-怪物当前位置
                float distSq = math.lengthsq(new float2(toTarget.x, toTarget.z));
                // 计算 XZ 平面方向并归一化
                float3 dir = math.normalize(new float3(toTarget.x, 0, toTarget.z));

                if (distSq < 10f) // 距离目标点足够近就禁用标签
                {
                    if (debuff.ValueRW.thunderGripEndTimer >= 0.3f)
                        ecb.SetComponentEnabled<PreDefineHeroSkillThunderGripTag>(entity, false);

                }
                else
                {

                    transform.ValueRW.Position += dir * 100f * SystemAPI.Time.DeltaTime;


                }
            }





        }

        /// <summary>
        /// 连锁吞噬，瞬时技能标签 寻址技能通用标签 连锁吞噬 专用标签
        /// </summary>
        /// <param name="state"></param>
        /// <param name="ecb"></param>
        void SkillMonoChainDevour(ref SystemState state, EntityCommandBuffer ecb)
        {
            foreach (var (transform, skillDamageCal, trackingSkillTag, SkillTag, overlapTracking, entity)
              in SystemAPI.Query<RefRW<LocalTransform>, RefRW<SkillsDamageCalPar>, RefRW<SkillsTrackingCalPar>, RefRW<SkillChainDevourTag>, RefRW<OverlapTrackingQueryCenter>>().WithEntityAccess())
            {
                SkillTag.ValueRW.tagSurvivalTime -= SystemAPI.Time.DeltaTime;
                overlapTracking.ValueRW.center = transform.ValueRO.Position;
                // float3 forward = math.mul(transform.ValueRO.Rotation, new float3(0f, 0f, 1f));
                //这里由初始化值进行赋值
                float3 forward = trackingSkillTag.ValueRO.currentDir;
                transform.ValueRW.Position += SkillTag.ValueRO.speed * forward * SystemAPI.Time.DeltaTime;
                //主线程控制时间？等于true情况下为0.1秒钟的间隔，可以针对不同的技能标签进行分离控制,三帧
                if (trackingSkillTag.ValueRW.enbaleChangeTarget == true && trackingSkillTag.ValueRO.runCount > 0)
                {
                    trackingSkillTag.ValueRW.timer += SystemAPI.Time.DeltaTime;

                    if (trackingSkillTag.ValueRW.timer > 0.05f)
                        trackingSkillTag.ValueRW.enbaleChangeTarget = false;
                }


                if (SkillTag.ValueRW.tagSurvivalTime <= 0)
                    skillDamageCal.ValueRW.destory = true;

            }


        }


        /// <summary>
        /// 技能 幻影步
        /// </summary>
        /// <param name="state"></param>
        /// <param name="ecb"></param>
        /// <param name="timer"></param>
        void SkillMonoPhantomStep(ref SystemState state, EntityCommandBuffer ecb, float timer)
        {

            foreach (var (transform, skillDamageCal, SkillTag, entity)
             in SystemAPI.Query<RefRW<LocalTransform>, RefRW<SkillsDamageCalPar>, RefRW<SkillPhantomStepTag>>().WithEntityAccess())
            {
                SkillTag.ValueRW.tagSurvivalTime -= timer;
                if (SkillTag.ValueRW.tagSurvivalTime <= 0)
                {
                    skillDamageCal.ValueRW.destory = true;
                }
            }

        }
        /// <summary>
        /// 技能暗影步
        /// </summary>
        /// <param name="state"></param>
        /// <param name="ecb"></param>
        /// <param name="timer"></param>
        void SkillMonoShadowStep(ref SystemState state, EntityCommandBuffer ecb, float timer)
        {
            foreach (var (transform, skillDamageCal, SkillTag, entity)
           in SystemAPI.Query<RefRW<LocalTransform>, RefRW<SkillsDamageCalPar>, RefRW<SkillShadowStepTag>>().WithEntityAccess())
            {
                SkillTag.ValueRW.tagSurvivalTime -= timer;
                if (SkillTag.ValueRW.tagSurvivalTime <= 0)
                {
                    skillDamageCal.ValueRW.destory = true;
                }
            }

        }


        /// <summary>
        /// 英雄技能ECS 释放系统(静电牢笼B变种)
        /// </summary>
        /// <param name="ecb"></param>
        /// <param name="prefab"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="damageChangePar"></param>
        /// <param name="positionOffset"></param>
        /// <param name="rotationOffsetEuler"></param>
        /// <param name="scaleFactor"></param>
        /// <param name="enablePull"></param>
        /// <param name="enableExplosion"></param>
        /// <returns></returns>
        public Entity DamageSkillsECSRelaseProp(
         EntityCommandBuffer ecb,
         Entity prefab,
         SkillsDamageCalPar skillsDamageCal,
         float3 position,
         quaternion rotation,
         float damageChangePar = 1,//默认伤害参数为1
         float3 positionOffset = default,
         float3 rotationOffsetEuler = default,
         float scaleFactor = 1f,
         bool enablePull = false,
         bool enableExplosion = false)
        {
            // 1) 延迟实例化
            var entity = ecb.Instantiate(prefab);

            // 2) 读取预制体上已有的 LocalTransform，仅读取操作可以直接用 EntityManager
            var prefabTransform = _entityManager.GetComponentData<LocalTransform>(prefab);
            float baseScale = prefabTransform.Scale;

            // 3) 计算新的变换
            quaternion offsetQuat = quaternion.EulerXYZ(math.radians(rotationOffsetEuler));
            LocalTransform newTransform = new LocalTransform
            {
                Position = position + math.mul(rotation, positionOffset),
                Rotation = math.mul(rotation, offsetQuat),
                //这里由技能范围决定技能的影响因子
                Scale = baseScale * scaleFactor * (1 + _heroAttributeCmptOriginal.gainAttribute.skillRange)
            };

            // 4) 写入新实体
            ecb.SetComponent(entity, newTransform);

            // 5) 添加并初始化伤害参数组件，沿用快照机制
            ecb.AddComponent(entity, skillsDamageCal);

            // 6) 添加碰撞记录缓冲区
            var hits = ecb.AddBuffer<HitRecord>(entity);
            ecb.AddBuffer<HitElementResonanceRecord>(entity);

            //写回
            return entity;
        }



    }
}