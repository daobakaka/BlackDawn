using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.VFX;
using static BlackDawn.HeroAttributes;

//用于处理技能释放的回调，非burstCompile
namespace BlackDawn.DOTS
{/// <summary>
/// 由英雄mono开启,在渲染系统之后进行,回调系统， 涉及到传统class交互， 设计在最后进行
/// </summary>
    [RequireMatchingQueriesForUpdate]
    [UpdateAfter(typeof(HeroSkillsMonoSystem))]
    [UpdateInGroup(typeof(MainThreadSystemGroup))]
    public partial class HeroSkillsCallbackSystemBase : SystemBase, IOneStepFun
    {
        public bool Done { get; set; }
        HeroSkills _heroSkills;
        ScenePrefabsSingleton _prefabs;

        //特殊技能系统（法阵的虹吸特效）缓存
        private SystemHandle _specialSkillsDamageSystemHandle;

        private SystemHandle _detectionSystemHandle;

        private Entity _heroEntity;
        //直接引用mono的单例位置，会卡顿
        private ComponentLookup<LocalTransform> _transformLookup;
        
        //英雄属性
        private ComponentLookup<HeroAttributeCmpt> _heroAttribute;


        //法阵技能的GPUbuffer
        GraphicsBuffer _arcaneCirclegraphicsBuffer;
        private bool resetVFXPartical;
        protected override void OnCreate()
        {
            base.OnCreate();
            //由英雄初始化时开启
            base.Enabled = false;

            _specialSkillsDamageSystemHandle = World.Unmanaged.GetExistingUnmanagedSystem<HeroSpecialSkillsDamageSystem>();
            _arcaneCirclegraphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 5000, sizeof(float) * 3);
            _detectionSystemHandle = World.Unmanaged.GetExistingUnmanagedSystem<DetectionSystem>();
            _transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            _heroAttribute = SystemAPI.GetComponentLookup<HeroAttributeCmpt>(true);

        }

        protected override void OnStartRunning()
        {
            //获取英雄技能单例
            _heroSkills = HeroSkills.GetInstance();

            _prefabs = SystemAPI.GetSingleton<ScenePrefabsSingleton>();
            //获取英雄entity
            _heroEntity = Hero.instance.heroEntity;

        }

        protected override void OnUpdate()
        {
            //base系统更新
            _transformLookup.Update(this);
            //hero属性更新
            _heroAttribute.Update(this);

            //获取英雄属性
            var heroPar = _heroAttribute[_heroEntity];

            var ecb = World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer();

            var timer = SystemAPI.Time.DeltaTime;

            var specialDanafeSystem = World.Unmanaged.GetUnsafeSystemRef<HeroSpecialSkillsDamageSystem>(_specialSkillsDamageSystemHandle);

            var detctionSystem = World.Unmanaged.GetUnsafeSystemRef<DetectionSystem>(_detectionSystemHandle);
            //渲染链接
            var arcaneCircleLinkedArray = specialDanafeSystem.arcaneCircleLinkenBuffer;
            //获取侦测系统的  碰撞对
            var mineBlastArray = detctionSystem.mineBlastHitMonsterArray;

            //测试性绘制
            Entities
           .WithName("DrawOverlapCollider")
           .ForEach((in OverlapOverTimeQueryCenter overlap,in LocalToWorld localToWorld) =>
           {

               if (overlap.shape == OverLapShape.Sphere)
                   DebugDrawSphere(overlap.center, overlap.offset,overlap.rotaion ,overlap.radius, Color.yellow, 0.02f);
               else if (overlap.shape == OverLapShape.Box)
               {
                   quaternion rot = new quaternion(overlap.rotaion); // 四元数
                   DebugDrawBox(overlap.center, overlap.offset, overlap.box, rot, Color.green, 0.02f);
               }

           }).WithoutBurst().Run();

            Entities
            .WithName("DrawBurstOverlapCollider")
            .ForEach((in OverlapBurstQueryCenter overlap,in LocalToWorld localToWorld) =>
            {

               if (overlap.shape == OverLapShape.Sphere)
                   DebugDrawSphere(overlap.center, overlap.offset, overlap.rotaion,overlap.radius, Color.yellow, 0.02f);
               else if (overlap.shape == OverLapShape.Box)
               {
                   quaternion rot = new quaternion(overlap.rotaion); // 四元数
                   DebugDrawBox(overlap.center, overlap.offset, overlap.box, rot, Color.green, 0.02f);
               }

            }).WithoutBurst().Run();

            // **遍历所有打了请求标记的实体**,这里需要为方法传入ECB，这样可以在foreach里面同一帧使用
            //这是针对粒子特效的方法
            if (false)
                Entities
                    .WithName("SkillPulseSceondExplosionCallback") //程序底层打签名，用于标记
                    .WithAll<SkillPulseSecondExplosionRequestTag>() //匹配ABC 所有组件,默认匹配没有被disnable的组件
                                                                    //in 只读，需要放到ref 后面
                    .ForEach((Entity e, ref SkillsDamageCalPar damageCalPar, ref SkillPulseTag pulseTag, in LocalTransform t) =>
                    {
                        // 调用 Mono 层的爆炸逻辑，继续设连锁阶段
                        var entity = _heroSkills.DamageSkillsExplosionProp(
                           ecb,
                           _prefabs.ParticleEffect_DefaultEffexts, //爆炸特效                        
                           t.Position,
                           t.Rotation,
                           1,
                           0, 0, pulseTag.scaleChangePar, false, true
                       );
                        ecb.AddComponent(entity, new SkillPulseTag() { tagSurvivalTime = 2, scaleChangePar = pulseTag.scaleChangePar });//为二阶段技能生成存活标签,这里传入形变参数,持续两秒
                                                                                                                                        //这种方式不会形成结构改变               
                        ecb.SetComponentEnabled<SkillPulseSecondExplosionRequestTag>(e, false);
                        //销毁，此时已经生成了二阶段技能，二阶段技能没有标签，返回到第一阶段进行销毁
                        ecb.DestroyEntity(e);
                    })
                    .WithoutBurst()   // 必须关闭 Burst，才能调用任何 UnityEngine/Mono 代码
                    .Run();


            //脉冲处理
            Entities
                .WithName("SkillPulseSceondVFXExplosionCallback") //这里直接标记脉冲的VFX回调标识
                .WithAll<SkillPulseSecondExplosionRequestTag>() //匹配ABC 所有组件,默认匹配没有被disnable的组件
                                                                //in 只读，需要放到ref 后面
                .ForEach((Entity e, VisualEffect vfx, ref SkillsDamageCalPar damageCalPar, ref SkillPulseTag pulseTag, ref LocalTransform t) =>
                {

                    vfx.SendEvent("hit");
                    ecb.SetComponentEnabled<SkillPulseSecondExplosionRequestTag>(e, false);
                    //切换引力和爆炸状态
                    damageCalPar.enableExplosion = true;
                    damageCalPar.enablePull = false;
                    t.Scale *= (1 + pulseTag.scaleChangePar);
                    //取消第二阶段状态，2秒后销毁
                    pulseTag.tagSurvivalTime = 2;
                    pulseTag.enableSecond = false;
                    //二阶段停止移动
                    pulseTag.speed = 0;

                })
                .WithoutBurst()   // 必须关闭 Burst，才能调用任何 UnityEngine/Mono 代码
                .Run();

            //冰火球处理
            Entities
                .WithName("SkillIceFireSceondVFXExplosionCallback") //这里直接标记冰火球的VFX回调标识
                .WithAll<SkillIceFireSecondExplosionRequestTag>() //匹配ABC 所有组件,默认匹配没有被disnable的组件
                                                                  //in 只读，需要放到ref 后面
                .ForEach((Entity e, VisualEffect vfx, ref SkillsDamageCalPar damageCalPar, ref SkillIceFireTag skillTag, ref LocalTransform t) =>
                {
                    //播放爆炸动画
                    vfx.SendEvent("hit");
                    ecb.SetComponentEnabled<SkillIceFireSecondExplosionRequestTag>(e, false);

                    //切换引力和爆炸状态
                    damageCalPar.enableExplosion = false;
                    //爆炸产生引力效果
                    damageCalPar.enablePull = true;
                    //爆炸增加体积
                    t.Scale *= (1 + skillTag.scaleChangePar);
                    //增加爆炸伤害,这里直接增加，因为是进入乘法区，直接加就可以
                    damageCalPar.damageChangePar += skillTag.skillDamageChangeParTag;
                    //取消第二阶段状态，2秒后销毁
                    skillTag.secondSurvivalTime = 4;
                    //允许特殊效果
                    skillTag.enableSpecialEffect = false;
                    //二阶段停止移动
                    // skillTag.speed = 0;

                })
                .WithoutBurst()   // 必须关闭 Burst，才能调用任何 UnityEngine/Mono 代码
                .Run();

            //冰火球重复激活处理
            Entities
                .WithName("Disabled_SkillIceFireSecondExplosion")
                .WithDisabled<SkillIceFireSecondExplosionRequestTag>()
                .ForEach((Entity entity, VisualEffect vfx,
                    ref SkillIceFireTag skillTag,
                    ref SkillsDamageCalPar damageCalPar,
                    ref LocalTransform transform) =>
                {

                    skillTag.secondSurvivalTime -= timer;
                    //两秒执行一次恢复判断，必须播放爆炸特效2秒之后进行
                    if (!skillTag.enableSpecialEffect && skillTag.secondSurvivalTime < 3)
                    {
                        vfx.SendEvent("create");
                        //恢复尺寸
                        transform.Scale = skillTag.originalScale;
                        //恢复的时候减回
                        damageCalPar.damageChangePar = 1;
                        //恢复引力
                        damageCalPar.enablePull = false;
                        skillTag.enableSpecialEffect = true;
                    }


                })
                .WithoutBurst().Run();


            //法阵buffer的效果 ，需要在外面清除,目前暂时使用这种方式，感觉其他方式有BUG,特比是关于buffer的清除比麻烦
            if (_arcaneCirclegraphicsBuffer != null)
            {
                _arcaneCirclegraphicsBuffer.Dispose();
            }


            //法阵的链接特效,传入buffer数组
            Entities
                    .WithName("Enable_ArcaneCircleSecond")
                    .WithAll<HeroEffectsLinked>()
                    .ForEach((Entity entity, VisualEffect vfx
                      ) =>
                    {

                        if (arcaneCircleLinkedArray.Length > 0)
                        {
                            var targetPositions = arcaneCircleLinkedArray;

                            _arcaneCirclegraphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, targetPositions.Length, sizeof(float) * 3);
                            vfx.SetInt("_LinkedTargetsCount", targetPositions.Length);

                            _arcaneCirclegraphicsBuffer.SetData(targetPositions);
                            // 传入 VFX
                            vfx.SetGraphicsBuffer("_LinkedTargets", _arcaneCirclegraphicsBuffer);  // 与 VFX 中匹配

                            //缓冲数据准备完毕之后，进行buffer清除
                            vfx.SendEvent("Custom5");
                            // buffer.Dispose();
                            resetVFXPartical = false;
                        }
                    })
                    .WithoutBurst().Run();

            //法阵的链接特效,这里是恢复
            Entities
                .WithName("DisEnable_ArcaneCircleSecond")
                .WithDisabled<HeroEffectsLinked>()
                .ForEach((Entity entity, VisualEffect vfx
                  ) =>
                {
                    if (!resetVFXPartical)
                    {


                        vfx.Reinit();
                        resetVFXPartical = true;
                    }

                })
                .WithoutBurst().Run();



            //毒爆地雷一级阶段处理
            Entities
                .WithName("SkillMineBlast")
                .ForEach((Entity entity, VisualEffect vfx,
                    ref SkillMineBlastTag skillTag,
                    ref SkillsDamageCalPar damageCalPar,
                    ref LocalTransform transform) =>
                {

                    skillTag.tagSurvivalTime -= timer;
                    //百分百数字命中，不用添加新变量
                    if (skillTag.tagSurvivalTime <= 1f && skillTag.tagSurvivalTime > 1f - timer)
                    {
                        vfx.SendEvent("stop");

                    }
                    if (skillTag.tagSurvivalTime <= 0)
                    {
                        // ecb.DestroyEntity(entity);
                        damageCalPar.destory = true;
                        return;

                    }
                    //保持检测，因为要计算毒爆之后的毒伤效果,这里跑起来的碰撞对太多了，应当分离开

                    for (int i = 0; i < mineBlastArray.Length; i++)
                    {

                        if (mineBlastArray[i].EntityA == entity || mineBlastArray[i].EntityB == entity)

                        {
                            vfx.SendEvent("hit");
                            ecb.SetComponentEnabled<SkillMineBlastTag>(entity, false);
                            //开启爆炸
                            ecb.SetComponentEnabled<SkillMineBlastExplosionTag>(entity, true);
                            //赋予新的伤害
                            damageCalPar.damageChangePar = skillTag.skillDamageChangeParTag;
                            //赋予爆炸效果
                            damageCalPar.enableExplosion = true;
                            //范围增大
                            transform.Scale = skillTag.scaleChangePar;
                            //200点恐惧效果， 恐惧3秒
                            damageCalPar.tempFear = 200;
                            break;
                        }

                    }

                })
                .WithoutBurst().Run();

            //毒爆地雷一阶爆炸效果处理
            Entities
                .WithName("DisableSkillMineBlast")
                .WithDisabled<SkillMineBlastTag>()
                .ForEach((Entity entity, VisualEffect vfx,
                    ref SkillMineBlastExplosionTag skillTag,
                    ref SkillsDamageCalPar damageCalPar,
                    ref LocalTransform transform) =>
                {
                    skillTag.tagSurvivalTime -= timer;


                    if (skillTag.tagSurvivalTime <= 0)
                    {

                        if (!skillTag.enableSecondA)
                        {
                            //ecb.DestroyEntity(entity);
                            damageCalPar.destory = true;
                            return;
                        }
                        else //允许第一阶段 变化情况
                        {
                            skillTag.tagSurvivalTimeSecond -= timer;
                            if (!skillTag.startSecondA)
                            {
                                skillTag.startSecondA = true;
                                vfx.SendEvent("buildup");
                                //传入变化参数
                                transform.Scale = skillTag.scaleChangePar;
                                //传入伤害参数
                                damageCalPar.damageChangePar = skillTag.skillDamageChangeParTag;
                                //修正恐惧值
                                damageCalPar.tempFear = 0;
                                //修正爆炸值
                                damageCalPar.enableExplosion = false;
                                //增加吸引值
                                damageCalPar.enablePull = true;
                            }
                            if (skillTag.tagSurvivalTimeSecond <= 0)
                            {
                                //ecb.DestroyEntity(entity);
                                damageCalPar.destory = true;
                                return;
                            }

                        }
                    }
                    //允许第二阶段变化，写在特殊技能job 里面？               

                })
                .WithoutBurst().Run();

            //技能毒雨
            Entities
           .WithName("SkillOverTimePoisonRain")
           .ForEach((Entity entity, VisualEffect vfx,
               ref SkillPoisonRainTag skillTag,
               ref SkillsOverTimeDamageCalPar damageCalPar,
               ref LocalTransform transform) =>
           {
               skillTag.tagSurvivalTime -= timer;

               if (skillTag.tagSurvivalTime <= 2f && skillTag.tagSurvivalTime > 2f - timer)
               {
                   vfx.Stop();


               }
               if (skillTag.tagSurvivalTime <= 0)
                   damageCalPar.destory = true;


           }).WithoutBurst().Run();


            //技能 暗影洪流,引导，跟随，旋转
            SkillCallBack_ShadowTide(timer, ecb, heroPar);
            //技能 冰霜新星
            SkillCallBack_FrostNova(timer, ecb, _prefabs);

            // 播放并清理
            //ecb.Playback(base.EntityManager);
            //ecb.Dispose();


        }




        protected override void OnDestroy()
        {


            //法阵buffer的效果 ，需要在外面清除
            if (_arcaneCirclegraphicsBuffer != null)
            {
                _arcaneCirclegraphicsBuffer.Dispose();
            }
        }

        //技能 暗影洪流,引导，跟随，旋转
        void SkillCallBack_ShadowTide(float timer,EntityCommandBuffer ecb,HeroAttributeCmpt heroPar)
        {

            //技能 暗影洪流,引导，跟随，旋转
            Entities
            .WithName("SkillOverTimeShadowTide")
            .ForEach((Entity entity, VisualEffect vfx,
               ref SkillShadowTideTag skillTag,
               ref OverlapOverTimeQueryCenter overlap,
               ref SkillsOverTimeDamageCalPar damageCalPar,
               ref LocalTransform transform) =>
            {
                skillTag.tagSurvivalTime -= timer;

                //同步碰撞体
                overlap.center = _transformLookup[_heroEntity].Position;
                overlap.rotaion = _transformLookup[_heroEntity].Rotation.value;
                //偏移Y1的距离
                transform.Position = _transformLookup[_heroEntity].Position + new float3(0, 1, 0);
                transform.Rotation = _transformLookup[_heroEntity].Rotation;
                if (skillTag.tagSurvivalTime <= 0)
                {
                    //钳制到0 每秒消耗5点
                    heroPar.defenseAttribute.energy = math.max(0, heroPar.defenseAttribute.energy - 5 * timer);

                    if (heroPar.defenseAttribute.energy <= 0)
                    {

                        skillTag.closed = true;
                    }
                    ecb.SetComponent(_heroEntity, heroPar);

                }
                if (skillTag.closed == true)
                {
                    vfx.Stop();
                    skillTag.effectDissolveTime += timer;
                    if (skillTag.effectDissolveTime >= 1)
                        damageCalPar.destory = true;
                }
                if (skillTag.enableSecondB)
                {
                    skillTag.secondBTimer += timer;
                    //3秒一次的进入
                    if (skillTag.secondBTimer <= 3f && skillTag.secondBTimer > 3f - timer)
                    {
                        skillTag.secondBTimer = 0;
                        //进入清零，进行30%概率随机判断，成功则释放技能,转化型伤害 默认白色
                        if (UnityEngine.Random.Range(0, 1f) < 1f)

                        {  //释放技能b
                            var shadowTideB = ecb.Instantiate(_prefabs.HeroSkillAssistive_ShadowTideB);
                            var currentTransform = _transformLookup[entity];

                            var skillsBurstDamageCalPar = new SkillsBurstDamageCalPar();
                            //继承2倍暗影伤害
                            skillsBurstDamageCalPar.fireDamage = damageCalPar.shadowDamage;
                            //造成等量的DOT 伤害
                            skillsBurstDamageCalPar.fireDotDamage = damageCalPar.shadowDamage;
                            skillsBurstDamageCalPar.damageChangePar = skillTag.skillDamageChangeParTag * 2 * (1 + 0.1f * skillTag.level);
                            skillsBurstDamageCalPar.heroRef = _heroEntity;

                            ecb.AddComponent(shadowTideB, skillsBurstDamageCalPar);
                            //添加专属技能标签,持续3秒？
                            ecb.AddComponent(shadowTideB, new SkillShadowTideBTag() { tagSurvivalTime = 1.0f });

                            ecb.SetComponent(shadowTideB, new LocalTransform
                            {
                                Position = currentTransform.Position,
                                Rotation = currentTransform.Rotation,
                                Scale = 1f
                            });
                            //前方球形区域的生成
                            var filter = new CollisionFilter
                            {
                                BelongsTo = 1u << 10,
                                CollidesWith = 1u << 6,
                                GroupIndex = 0
                            };
                            var collider = new OverlapBurstQueryCenter { center = transform.Position, radius = 20,rotaion=transform.Rotation.value, filter = filter, offset = new float3(0, 0, 20),shape=OverLapShape.Sphere };

                            ecb.AddComponent(shadowTideB, collider);

                            // 6) 添加碰撞记录缓冲区,爆发型技能无法元素共鸣
                            //var hits = ecb.AddBuffer<HitRecord>(shadowTideB);
                            //ecb.AddBuffer<HitElementResonanceRecord>(shadowTideB);
                        }

                    }

                }


            }).WithoutBurst().Run();




        }


        // 技能 冰霜新星
        void SkillCallBack_FrostNova(float timer, EntityCommandBuffer ecb, ScenePrefabsSingleton prefab)
        {
            Entities
                .WithName("SkillTimeFrostNova")
                .ForEach((Entity entity, VisualEffect vfx,
                    ref SkillFrostNovaTag skillTag,
                    ref SkillsDamageCalPar damageCalPar,
                    ref LocalTransform transform) =>
                {
                    skillTag.tagSurvivalTime -= timer;
                    if (skillTag.tagSurvivalTime <= 0)
                    {
                        damageCalPar.destory = true;
                        return;
                    }

                    // 冷却进入最后4.5秒时触发
                    if (skillTag.tagSurvivalTime <= 2.5f && skillTag.tagSurvivalTime > 2.5f - timer)
                    {
                        if (skillTag.enableSecondB)
                        {
                            for (int i = 0; i < 2; i++)
                            {
                                // 实例第二阶段实体
                                var frostWave = ecb.Instantiate(prefab.HeroSkillAssistive_FrostNovaB);

                                // 设置旋转角度，i=0时为+90度，i=1时为-90度，传递体积参数
                                var trsCopy = transform;
                                float angle = (i == 0) ? math.radians(90f) : math.radians(-90f);
                                trsCopy.Rotation = math.mul(trsCopy.Rotation, quaternion.EulerXYZ(0f, angle, 0f)); // 累加旋转
                                                                                                                   // 这里还传递了体积值
                                ecb.SetComponent(frostWave, trsCopy); // 设置

                                // 添加第二阶段标签，并赋予速度和生存时间
                                ecb.AddComponent(frostWave, new SkillFrostNovaBTag { tagSurvivalTime = 3 });

                                // 继承伤害参数和冻结值,传递等级，这里实际上只需要传递一个冻结值就可以了
                                var newCal = damageCalPar;
                                newCal.damageChangePar = damageCalPar.damageChangePar * (1 + skillTag.level * 0.05f);

                                ecb.AddComponent(frostWave, newCal);

                                // 添加命中记录Buffer
                                ecb.AddBuffer<HitRecord>(frostWave);
                                ecb.AddBuffer<HitElementResonanceRecord>(frostWave);
                            }
                        }
                    }
                })
                .WithoutBurst().Run();
            //两个阶段写在一个系统里面便于调式
              Entities
                .WithName("SkillTimeFrostNovaB")
                .ForEach((Entity entity, VisualEffect vfx,
                    ref SkillFrostNovaBTag skillTag,
                    ref SkillsDamageCalPar damageCalPar,
                    ref LocalTransform transform) =>
                {
                    skillTag.tagSurvivalTime -= timer;
                    if (skillTag.tagSurvivalTime <= 0)
                    {
                        damageCalPar.destory = true;
                        return;
                    }                  
                })
                .WithoutBurst().Run();

}



        void DebugDrawSphere(float3 entityPosition, float3 offset, quaternion rotation, float radius, Color color, float duration)
        {
            // ✅ 偏移应用旋转
            float3 rotatedOffset = math.mul(rotation, offset);

            // ✅ 计算最终中心
            float3 center = entityPosition + rotatedOffset;

            for (int i = 0; i < 12; i++)
            {
                float angle1 = math.radians(i * 30f);
                float angle2 = math.radians((i + 1) * 30f);

                // xy 平面
                Debug.DrawLine(
                    (Vector3)(center + new float3(math.cos(angle1), math.sin(angle1), 0) * radius),
                    (Vector3)(center + new float3(math.cos(angle2), math.sin(angle2), 0) * radius),
                    color, duration);

                // xz 平面
                Debug.DrawLine(
                    (Vector3)(center + new float3(math.cos(angle1), 0, math.sin(angle1)) * radius),
                    (Vector3)(center + new float3(math.cos(angle2), 0, math.sin(angle2)) * radius),
                    color, duration);

                // yz 平面
                Debug.DrawLine(
                    (Vector3)(center + new float3(0, math.cos(angle1), math.sin(angle1)) * radius),
                    (Vector3)(center + new float3(0, math.cos(angle2), math.sin(angle2)) * radius),
                    color, duration);
            }
        }

        void DebugDrawBox(float3 center, float3 offset, float3 boxSize, quaternion rotation, Color color, float duration)
        {
            float3 halfExtents = boxSize * 0.5f;

            // 旋转 offset
            float3 rotatedOffset = math.mul(rotation, offset);

            // 实际中心
            float3 boxCenter = center + rotatedOffset;

            // 8 个角点（在本地坐标下）
            float3[] localCorners = new float3[8]
            {
                new float3(-1, -1, -1),
                new float3( 1, -1, -1),
                new float3( 1,  1, -1),
                new float3(-1,  1, -1),
                new float3(-1, -1,  1),
                new float3( 1, -1,  1),
                new float3( 1,  1,  1),
                new float3(-1,  1,  1),
            };

            // 应用缩放和旋转，得到世界空间角点
            for (int i = 0; i < 8; i++)
            {
                localCorners[i] *= halfExtents;
                localCorners[i] = math.mul(rotation, localCorners[i]) + boxCenter;
            }

            // 画边线（连接 12 条边）
            int3[] edges = new int3[12]
            {
                new int3(0,1,0), new int3(1,2,0), new int3(2,3,0), new int3(3,0,0),
                new int3(4,5,0), new int3(5,6,0), new int3(6,7,0), new int3(7,4,0),
                new int3(0,4,0), new int3(1,5,0), new int3(2,6,0), new int3(3,7,0),
            };

            for (int i = 0; i < 12; i++)
            {
                Debug.DrawLine(localCorners[edges[i].x], localCorners[edges[i].y], color, duration);
            }
        }

    }
}