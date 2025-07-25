using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
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

        private float3 _heroPositon;
        
        //英雄属性
        private ComponentLookup<HeroAttributeCmpt> _heroAttribute;

        private HeroAttributeCmpt _orignalHeroAttributeCmp;

        private EntityManager _entityManager;


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
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        }

        protected override void OnStartRunning()
        {
            //获取英雄技能单例
            _heroSkills = HeroSkills.GetInstance();

            _prefabs = SystemAPI.GetSingleton<ScenePrefabsSingleton>();
            //获取英雄entity
            _heroEntity = Hero.instance.heroEntity;
            _orignalHeroAttributeCmp = Hero.instance.attributeCmpt;

        }

        protected override void OnUpdate()
        {
            //base系统更新
            _transformLookup.Update(this);
            //hero属性更新
            _heroAttribute.Update(this);
            _heroPositon = _transformLookup[_heroEntity].Position;

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



            //自定义绘制
            CustomOverlapDarw();
            //技能脉冲 0
            SkillCallBack_Pulse(timer, ecb);
            //技能冰火 2
            SkillCallBack_iceFire(timer, ecb);
            //技能法阵 4
            SkillCallBack_ArcaneCircle(timer, ecb, arcaneCircleLinkedArray);
            //技能黑炎 7
            SkillCallBack_BlackFrame(timer, ecb);
            //技能横扫 8
            SkillCallBack_Sweep(timer, ecb);
            //技能毒池 9
            SkillCallBack_PoisonPool(timer, ecb);
            //技能毒爆地雷 14
            SkillCallBack_MineBlast(timer, ecb, mineBlastArray);
            //技能 暗影洪流 15,引导，跟随，旋转
            SkillCallBack_ShadowTide(timer, ecb, heroPar);
            //技能 烈焰冲锋 17
            SkillCallBack_FlameCharge(timer, ecb);
            //通用-冰霜护盾18 
            SkillCallBack_FrostShieldDeal(timer,_prefabs,ecb);
            //技能 冰霜新星 22
            SkillCallBack_FrostNova(timer, ecb, _prefabs);
            // 元素护盾 25
            SkillCallBack_ElementShieldDeal();
            //暗影之拥 23 瞬时/持续 
            SkillCallBack_ShadowEmbrace(timer, ecb);
            //瘟疫蔓延 24 辅助/增强
            SkillCallBack_PlagueSpread(timer);
            // 元素护盾 25
            SkillCallBack_ElementShieldDeal();
            //技能 烈焰灵刃 26
            SkillCallBack_FlameSpiritBlade(timer, ecb);
            //技能 时空扭曲 27
            SkillCallBack_ChronoTwist(timer, ecb);
            //技能 烈焰爆发 28
            SKillCakkBack_FlameBurst(timer, ecb);
            //技能闪电链 30
            SkillCallBack_LightningChain(timer, ecb, _prefabs);
            //技能毒雨 32
            SkillCallBack_PoisonRain(timer);
            //元素爆发 33
            SkillCallBack_ElementBurst(timer, ecb);

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
        /// <summary>
        /// 自定义重叠碰撞体绘制
        /// </summary>
        void CustomOverlapDarw()
        { 

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


            Entities
            .WithName("DrawTrackingOverlapCollider")
            .ForEach((in OverlapTrackingQueryCenter overlap,in LocalToWorld localToWorld) =>
            {

               if (overlap.shape == OverLapShape.Sphere)
                   DebugDrawSphere(overlap.center, overlap.offset, overlap.rotaion,overlap.radius, Color.yellow, 0.02f);
               else if (overlap.shape == OverLapShape.Box)
               {
                   quaternion rot = new quaternion(overlap.rotaion); // 四元数
                   DebugDrawBox(overlap.center, overlap.offset, overlap.box, rot, Color.green, 0.02f);
               }

            }).WithoutBurst().Run();


        }



        //技能脉冲 回调阶段
        void SkillCallBack_Pulse(float timer,EntityCommandBuffer ecb)
        { 
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


        }

        //技能 冰火 回调阶段
        void SkillCallBack_iceFire(float timer, EntityCommandBuffer ecb)
        {
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


        }
      
       //黑炎  - 一种特殊的永不消失的dot?命中的敌人一直衰减？
       void SkillCallBack_BlackFrame(float timer, EntityCommandBuffer ecb)
        {

            Entities
                    .WithName("SkillBlackFrame")
                    .ForEach((Entity entiy, VisualEffect vfx ,ref SkillBlackFrameTag skillTag,ref SkillsDamageCalPar skillCal) =>
                    {

                        skillTag.tagSurvivalTime -= timer;
                        if (skillTag.tagSurvivalTime <= 3f && skillTag.tagSurvivalTime > 3f - timer)
                        {

                            vfx.Stop();

                        }
                        if (skillTag.tagSurvivalTime <= 0)
                        {

                            skillCal.destory = true;
                            
                        }

                    }).WithoutBurst().Run();


        }
        //横扫 回调阶段
        void SkillCallBack_Sweep(float timer, EntityCommandBuffer ecb)
        {
              
   
        //基础阶段渲染
          Entities
                .WithName("SkillSweepRender")
                .ForEach((Entity entity, VisualEffect vfx, ref SkillSweepRenderTag skillTag)=>
                    {
                        skillTag.tagSurvivalTime -= timer;
                        if (skillTag.tagSurvivalTime <= 0)
                        {
                            skillTag.destory = true;
                        }   
                        
                    }).WithoutBurst().Run();

            //横扫的碰撞
            Entities
                 .WithName("SkillSweepCollider")
                 .ForEach((Entity entity, ref SkillSweepTag skillTag, ref SkillsDamageCalPar skillDamgeCal, ref LocalTransform transform)=>
                    {
                        skillTag.tagSurvivalTime = math.max(0, skillTag.tagSurvivalTime - timer);
                        transform.Position = _heroPositon;
                        // 计算t
                        float t = 1f - math.saturate(skillTag.tagSurvivalTime / skillTag.rotationTotalTime);

                        // 起点角 -90
                        float startAngle = -90f;
                        float totalAngle = skillTag.enableSecondA ? 360f : 180f;
                        float angle = startAngle + totalAngle * t;

                        transform.Rotation = quaternion.RotateY(math.radians(angle));

                        if (skillTag.tagSurvivalTime <= 0)
                            skillDamgeCal.destory = true;

                        if (skillTag.enableSecondB)
                        {
                            // 每0.2秒生成一次
                            skillTag.spawnTimer -= timer;
                            if (skillTag.spawnTimer <= 0f)
                            {
                                skillTag.spawnTimer += skillTag.interval; // 重置间隔，防止跳帧漏刷
                                var skillB = ecb.Instantiate(_prefabs.HeroSkillAssistive_SweepB);
                                // 使用当前的旋转
                                ecb.SetComponent<LocalTransform>(skillB, transform);

                                var skillDamageCalB = skillDamgeCal;
                                skillDamageCalB.damageChangePar = skillTag.skillDamageChangeParTag;
                                ecb.AddComponent<SkillsDamageCalPar>(skillB, skillDamageCalB);
                                ecb.AddComponent(skillB, new SkillSweepBTag() { tagSurvivalTime = 5, speed = skillTag.speed });
                                ecb.AddBuffer<HitRecord>(skillB);
                                ecb.AddBuffer<HitElementResonanceRecord>(skillB);


                            }
                        }
                            

                    }).WithoutBurst().Run();

          
          
          
          
          
          
          
            //横扫技能B阶段 余震的运动状态
            Entities
                    .WithName("SkillSweepB")
                    .ForEach((Entity entity, VisualEffect vfx, ref SkillSweepBTag skillTag, ref SkillsDamageCalPar SkillCal,ref LocalTransform transform)=>
                    {
                             skillTag.tagSurvivalTime -= timer;                 
                            float3 forward = math.mul(transform.Rotation, new float3(0, 0, 1));
                            transform.Position += forward * skillTag.speed * timer;
                            
                        
                        if (skillTag.tagSurvivalTime < 1 && skillTag.tagSurvivalTime >= 1 - timer)
                        {
                            vfx.Stop();
                        }
                        if (skillTag.tagSurvivalTime <= 0)
                        {
                            SkillCal.destory = true;
                        }


                    }).WithoutBurst().Run();




        }
      
      
      
        //毒池 回调阶段
        void SkillCallBack_PoisonPool(float timer, EntityCommandBuffer ecb)
        {

            Entities
                    .WithName("SkillPosionPool")
                    .ForEach((Entity entiy, VisualEffect vfx ,ref SkillPoisonPoolTag skillTag,ref SkillsDamageCalPar skillCal) =>
                    {

                        skillTag.tagSurvivalTime -= timer;
                        if (skillTag.tagSurvivalTime <= 3f && skillTag.tagSurvivalTime > 3f - timer)
                        {

                            vfx.Stop();

                        }
                        if (skillTag.tagSurvivalTime <= 0)
                        {

                            skillCal.destory = true;
                            
                        }

                    }).WithoutBurst().Run();






        }
        //技能 法阵
        void SkillCallBack_ArcaneCircle(float timer, EntityCommandBuffer ecb,NativeArray<float3> arcaneCircleLinkedArray)
        {   
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

            
        }

        //技能毒爆地雷
        void SkillCallBack_MineBlast(float timer,EntityCommandBuffer ecb,NativeArray<TriggerPairData> mineBlastArray)
        {
            
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
            
            
        }




        //技能 毒雨
        void SkillCallBack_PoisonRain(float timer)
        { 
            
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


        }

        //技能 元素爆发
        void SkillCallBack_ElementBurst(float timer, EntityCommandBuffer ecb)
        {
            Entities
            .WithName("SkillElementBurst")
            .ForEach((Entity entity, VisualEffect vfx, ref SkillElementBurstTag skillTag,ref OverlapBurstQueryCenter overlap, ref SkillsBurstDamageCalPar skillDamageCal, ref LocalTransform transform)=>
            {
                skillTag.tagSurvivalTime -= timer;
                if (skillTag.tagSurvivalTime <= 0)
                    skillDamageCal.destory = true;
                //更新碰撞体中心位置
                overlap.center = transform.Position;
                //单次判断缩小半径重叠区域
                if (skillTag.tagSurvivalTime < 1f && skillTag.tagSurvivalTime >= 1f - timer)
                {

                    overlap.radius = 0.01f;
                }

                

            }).WithoutBurst().Run();   





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




        /// <summary>
        /// 技能回调 烈焰灵刃
        /// </summary>
        /// <param name="timer"></param>
        /// <param name="ecb"></param>/
        void SkillCallBack_FlameSpiritBlade(float timer, EntityCommandBuffer ecb)
        {

            Entities.WithName("HeroSkillFlameSpiritBlade")
                    .ForEach((VisualEffect vfx,ref SkillFlameSpiritBladeTag skillTag,ref SkillsDamageCalPar skillsDamageCal,ref LocalTransform transform) =>
                    {
                        skillTag.tagSurvivalTime -= timer;
                        if (skillTag.tagSurvivalTime <= 0)
                            skillsDamageCal.destory = true;
                        if (skillTag.tagSurvivalTime <= 1 && skillTag.tagSurvivalTime > 1 - timer)
                        {
                            vfx.SendEvent("end");
                        }
                         float3 forward = math.mul(transform.Rotation, new float3(0, 0, 1));

                          if (skillTag.enableSecondB)
                        {
                            float t = 8 - skillTag.tagSurvivalTime; // 已经经过的时间
                            float phaseTime = 4f;

                            if (t < phaseTime)
                            {
                                // 第一阶段：直线飞出
                                transform.Position += forward * skillTag.speed*timer;
                            }
                            else
                            {
                                if (t > phaseTime && t <= phaseTime + timer)
                                      transform.Rotation = math.mul( quaternion.RotateY(math.radians(180f)), transform.Rotation);//沿着Y 调转180度
                                //取消击退值
                                    skillsDamageCal.tempknockback = 0;
                                skillsDamageCal.tempStun = 300;
                                transform.Position += forward * skillTag.speed * 2f*timer;

                            }
                        }
                        else
                        {

                            transform.Position += forward * timer * skillTag.speed; // 速度可调
                        }
                        //开启烈焰吞噬的变化
                        if (skillTag.enableSecondA)
                        {
                            skillsDamageCal.timer += timer;//增加timer
                            skillsDamageCal.hitSurvivalTime -= timer;
                            if (skillsDamageCal.hitSurvivalTime <= 0)
                                skillsDamageCal.hit = false;
                            if (skillsDamageCal.hit)
                            {
                                if (skillsDamageCal.timer > 0.5f)
                                {
                                    skillsDamageCal.damageChangePar += 0.2f * (1+skillTag.level * 0.01f);//内置的timer去执行公共CD
                                    skillsDamageCal.timer = 0;
                                    vfx.SendEvent("hit");
                                }
                            }
                        }
                   

            }).WithoutBurst().Run();

        }
        /// <summary>
        /// 技能回调 时空扭曲
        /// </summary>
        /// <param name="timer"></param>
        /// <param name="ecb"></param>

        void SkillCallBack_ChronoTwist(float timer, EntityCommandBuffer ecb)
        {

            Entities.WithName("HeroSkillChronoTwist")
            .ForEach(( VisualEffect vfx,ref OverlapOverTimeQueryCenter overlap,ref SkillChronoTwistTag skillTag,ref SkillsOverTimeDamageCalPar skillOverTimeCal,ref LocalTransform transform) =>
            {
                skillTag.tagSurvivalTime -= timer;
                overlap.center = transform.Position;
                if (skillTag.tagSurvivalTime <= 0)
                    skillOverTimeCal.destory = true;              
                //单帧判断  转换爆炸参数
                if (skillTag.tagSurvivalTime < skillTag.stratExplosionTime && skillTag.tagSurvivalTime >= skillTag.stratExplosionTime - timer)
                {
                    vfx.SendEvent("hit");

                }
                if (skillTag.tagSurvivalTime < 1 && skillTag.tagSurvivalTime >= 1 - timer)
                {

                    overlap.radius *= 1.3f;
                    transform.Scale = 2.5f;
                    //取消牵引 重新爆炸
                    skillOverTimeCal.enablePull = false;
                    skillOverTimeCal.enableExplosion = true;
                    skillOverTimeCal.tempExplosion = 500f;
                    //爆炸的时候进行伤害变化计算
                    skillOverTimeCal.damageChangePar *= skillTag.skillDamageChangeParTag;

                    if (skillTag.enableSecondB)
                    {
                        int totalCount = 10 + skillTag.level;
                        float angleStep = 360f / totalCount;  // 每个之间的角度
                        var skillDamageCal = new SkillsDamageCalPar();

                        for (int j = 0; j < totalCount; j++)
                        {
                            var entiyBullet = ecb.Instantiate(_prefabs.HeroSkillAssistive_ChronoTwistB);
                            var tras = transform;
                            tras.Scale = 1.2f;
                            tras.Rotation = transform.Rotation * Quaternion.Euler(0,angleStep*j , 0);
                            ecb.SetComponent(entiyBullet, tras);
                            //添加时空碎片
                            ecb.AddComponent(entiyBullet, new SkillChronoTwistBTag { tagSurvivalTime = 0.5f, speed = 50 });
                            //选取英雄的武器类型 生成对应的值,这里假设物理
                            skillDamageCal.instantPhysicalDamage = skillOverTimeCal.instantPhysicalDamage*(0.2f+0.01f*skillTag.level);//每个造成10伤害加等级成长
                            skillDamageCal.heroRef = _heroEntity;//传递引用，进行动态伤害计算
                            skillDamageCal.damageChangePar = skillOverTimeCal.damageChangePar;
                            skillDamageCal.damageTriggerType = skillOverTimeCal.damageTriggerType;//持续性伤害无法造成压制？
                            //对应的dot伤害
                            skillDamageCal.bleedDotDamage = UnityEngine.Random.Range(0, 1f) < 0.3f ? 1 : 0 * skillDamageCal.instantPhysicalDamage;
                            ecb.AddComponent(entiyBullet, skillDamageCal);
                            //添加节能检测的buffer
                            ecb.AddBuffer<HitRecord>(entiyBullet);
                            ecb.AddBuffer<HitElementResonanceRecord>(entiyBullet);
                            


                        }

                    }
                 }
        

            }).WithoutBurst().Run();




        }


        /// <summary>
        /// 烈焰爆发 技能
        /// </summary>
        /// <param name="timer"></param>
        /// <param name="ecb"></param>
        void SKillCakkBack_FlameBurst(float timer, EntityCommandBuffer ecb)
        {
            Entities.
                    ForEach((VisualEffect vfx,ref SkillFlameBurstTag skillTag,ref SkillsBurstDamageCalPar skillsBurstDamage, ref OverlapBurstQueryCenter overlap ,ref LocalTransform transform) =>
                    {
                        skillTag.tagSurvivalTime -= timer;
                        if (skillTag.tagSurvivalTime <= 0)
                            skillsBurstDamage.destory = true;
                        overlap.center = _heroPositon;
                        transform.Position = _heroPositon;
                                                      
                    }).WithoutBurst().Run();
        }

        //技能  闪电链,这里 应该是先投掷检测球， 根据检测球的检测结果，输出闪电链 即可
        void SkillCallBack_LightningChain(float timer, EntityCommandBuffer ecb, ScenePrefabsSingleton prefab)
        {

            Entities
           .WithName("SkillLightningChainDeteciton")
           .ForEach((Entity entity,
               ref SkillLightningChainTag skillTag,
               ref OverlapTrackingQueryCenter overlapTracking,
               ref SkillsTrackingCalPar trackingCalPar,
               ref LocalTransform transform,
               ref DynamicBuffer<TrackingRecord> hitRecord) =>
           {

               skillTag.tagSurvivalTime -= timer;
               if (skillTag.tagSurvivalTime <= 0)
                   trackingCalPar.destory = true;
               //第一帧判断检测          
               if (!skillTag.bufferChecked)

               {
                   int count = math.min(20, hitRecord.Length);
                   if (count < 5)
                       return;
                   uint seed = (uint)(SystemAPI.Time.ElapsedTime) + (uint)entity.Index;
                   var rand = new Unity.Mathematics.Random(seed);

                   trackingCalPar.pos2Ref = hitRecord[rand.NextInt(0, count)].refTarget;
                   trackingCalPar.pos3Ref = hitRecord[rand.NextInt(0, count)].refTarget;
                   trackingCalPar.pos4Ref = hitRecord[rand.NextInt(0, count)].refTarget;
                   //允许第二阶段释放第二段电弧
                   if (skillTag.enableSecondB)
                   {
                       trackingCalPar.pos5Ref = hitRecord[rand.NextInt(0, count)].refTarget;
                       trackingCalPar.pos6Ref = hitRecord[rand.NextInt(0, count)].refTarget;
                       trackingCalPar.pos7Ref = hitRecord[rand.NextInt(0, count)].refTarget;
                       trackingCalPar.pos8Ref = hitRecord[rand.NextInt(0, count)].refTarget;
                   }

                   //这里为空检测判断

                   skillTag.bufferChecked = true;
               }
               //第二帧判断 初始化，检测到了再初始化，并且失活
               if (!skillTag.initialized && skillTag.bufferChecked)//生成闪电链的渲染，并且赋予位置
               {
                   skillTag.initialized = true;
                   var renderVFXEntity = ecb.Instantiate(prefab.HeroSkillAssistive_LightningChainRendering);

                   //这里应该为闪电链的相关参数赋值
                   ecb.AddComponent(renderVFXEntity, trackingCalPar);
                   ecb.AddComponent(renderVFXEntity, new SkillLightningChainRenderTag() { tagSurvivalTime = skillTag.laterTagSurvivalTime, colliderRef = skillTag.colliderRef, enableSecondA = skillTag.enableSecondA, enableSecondB = skillTag.enableSecondB });

                   //开启第二阶段新增加4个节点
                   if (skillTag.enableSecondB)
                   {

                       var renderVFXEntityB = ecb.Instantiate(prefab.HeroSkillAssistive_LightningChainRendering);

                       //这里应该为闪电链的相关参数赋值
                       ecb.AddComponent(renderVFXEntityB, trackingCalPar);
                       ecb.AddComponent(renderVFXEntityB, new SkillLightningChainRenderBTag() { tagSurvivalTime = skillTag.laterTagSurvivalTime, colliderRef = skillTag.colliderRef, enableSecondA = skillTag.enableSecondA, enableSecondB = skillTag.enableSecondB });

                   }

                   trackingCalPar.destory = true;

               }
               hitRecord.Clear();

           }).WithoutBurst().Run();

           //渲染闪电链
         Entities
        .WithName("SkillLightningChainRender")
        .ForEach((Entity entity, VisualEffect vfx,
            ref SkillLightningChainRenderTag skillTag,
            ref SkillsTrackingCalPar trackingCalPar,
            ref LocalTransform transform) =>
        {
            skillTag.tagSurvivalTime -= timer;
            if (skillTag.tagSurvivalTime <= 0)
                trackingCalPar.destory = true;
            if (!skillTag.initialized)
            {
                if (!skillTag.enableSecondA)
                    skillTag.initialized = true;

                // ==== 计算目标位置 ====
                float3 pos2 = _heroPositon + new float3(0, 0, 5);
                float3 pos3 = _heroPositon + new float3(0, 0, 10);
                float3 pos4 = _heroPositon + new float3(0, 0, 15);

                if (_transformLookup.HasComponent(trackingCalPar.pos2Ref))
                    pos2 = _transformLookup[trackingCalPar.pos2Ref].Position;
                if (_transformLookup.HasComponent(trackingCalPar.pos3Ref))
                    pos3 = _transformLookup[trackingCalPar.pos3Ref].Position;
                if (_transformLookup.HasComponent(trackingCalPar.pos4Ref))
                    pos4 = _transformLookup[trackingCalPar.pos4Ref].Position;

                // ==== 更新Collider ====
                if (_transformLookup.HasComponent(skillTag.colliderRef.collider1))
                {
                    var tra2 = _transformLookup[skillTag.colliderRef.collider1];
                    tra2.Position = pos2;
                    ecb.SetComponent<LocalTransform>(skillTag.colliderRef.collider1, tra2);
                    ecb.SetComponentEnabled<skillLightningChianColliderTag>(skillTag.colliderRef.collider1, true);
                }
                if (_transformLookup.HasComponent(skillTag.colliderRef.collider2))
                {
                    var tra3 = _transformLookup[skillTag.colliderRef.collider2];
                    tra3.Position = pos3;
                    ecb.SetComponent<LocalTransform>(skillTag.colliderRef.collider2, tra3);
                    ecb.SetComponentEnabled<skillLightningChianColliderTag>(skillTag.colliderRef.collider2, true);
                }
                if (_transformLookup.HasComponent(skillTag.colliderRef.collider3))
                {
                    var tra4 = _transformLookup[skillTag.colliderRef.collider3];
                    tra4.Position = pos4;
                    ecb.SetComponent<LocalTransform>(skillTag.colliderRef.collider3, tra4);
                    ecb.SetComponentEnabled<skillLightningChianColliderTag>(skillTag.colliderRef.collider3, true);
                }

                // ==== VFX 位置 ====
                vfx.SetVector3("Pos1", _heroPositon);
                vfx.SetVector3("Pos2", pos2);
                vfx.SetVector3("Pos3", pos3);
                vfx.SetVector3("Pos4", pos4);
            }
        }).WithoutBurst().Run();

            //B阶段，增加渲染数量
        Entities
        .WithName("SkillLightningChainRenderB")
        .ForEach((Entity entity, VisualEffect vfx,
            ref SkillLightningChainRenderBTag skillTag,
            ref SkillsTrackingCalPar trackingCalPar,
            ref LocalTransform transform) =>
        {
            skillTag.tagSurvivalTime -= timer;
            if (skillTag.tagSurvivalTime <= 0)
                trackingCalPar.destory = true;
            if (!skillTag.initialized)
            {
                if (!skillTag.enableSecondA)
                    skillTag.initialized = true;

                float3 pos4 = _heroPositon + new float3(0, 0, 20);
                float3 pos5 = _heroPositon + new float3(0, 0, 25);
                float3 pos6 = _heroPositon + new float3(0, 0, 30);
                float3 pos7 = _heroPositon + new float3(0, 0, 35);

                if (_transformLookup.HasComponent(trackingCalPar.pos4Ref))
                    pos4 = _transformLookup[trackingCalPar.pos4Ref].Position;
                if (_transformLookup.HasComponent(trackingCalPar.pos5Ref))
                    pos5 = _transformLookup[trackingCalPar.pos5Ref].Position;
                if (_transformLookup.HasComponent(trackingCalPar.pos6Ref))
                    pos6 = _transformLookup[trackingCalPar.pos6Ref].Position;
                if (_transformLookup.HasComponent(trackingCalPar.pos7Ref))
                    pos7 = _transformLookup[trackingCalPar.pos7Ref].Position;

                // ==== 更新Collider ====
                if (_transformLookup.HasComponent(skillTag.colliderRef.collider4))
                {
                    var tra4 = _transformLookup[skillTag.colliderRef.collider4];
                    tra4.Position = pos4;
                    ecb.SetComponent<LocalTransform>(skillTag.colliderRef.collider4, tra4);
                    ecb.SetComponentEnabled<skillLightningChianColliderTag>(skillTag.colliderRef.collider4, true);
                }
                if (_transformLookup.HasComponent(skillTag.colliderRef.collider5))
                {
                    var tra5 = _transformLookup[skillTag.colliderRef.collider5];
                    tra5.Position = pos5;
                    ecb.SetComponent<LocalTransform>(skillTag.colliderRef.collider5, tra5);
                    ecb.SetComponentEnabled<skillLightningChianColliderTag>(skillTag.colliderRef.collider5, true);
                }
                if (_transformLookup.HasComponent(skillTag.colliderRef.collider6))
                {
                    var tra6 = _transformLookup[skillTag.colliderRef.collider6];
                    tra6.Position = pos6;
                    ecb.SetComponent<LocalTransform>(skillTag.colliderRef.collider6, tra6);
                    ecb.SetComponentEnabled<skillLightningChianColliderTag>(skillTag.colliderRef.collider6, true);
                }
                if (_transformLookup.HasComponent(skillTag.colliderRef.collider7))
                {
                    var tra7 = _transformLookup[skillTag.colliderRef.collider7];
                    tra7.Position = pos7;
                    ecb.SetComponent<LocalTransform>(skillTag.colliderRef.collider7, tra7);
                    ecb.SetComponentEnabled<skillLightningChianColliderTag>(skillTag.colliderRef.collider7, true);
                }

                // ==== VFX 位置 ====
                vfx.SetVector3("Pos1", pos4);
                vfx.SetVector3("Pos2", pos5);
                vfx.SetVector3("Pos3", pos6);
                vfx.SetVector3("Pos4", pos7);
            }
        }).WithoutBurst().Run();

           

            Entities
            .WithName("SkillLightningChinaCollider")            
            .ForEach((Entity entiy, ref skillLightningChianColliderTag skillTag,ref SkillsDamageCalPar skillDamageCal) =>

            {
                skillTag.tagSurvivalTime -= timer;
                if (skillTag.tagSurvivalTime <= 0)
                    skillDamageCal.destory = true;

            }).WithoutBurst().Run();



        }
        /// <summary>
        ///  处理英雄元素护盾系统-目前是以动态计算的方式进行，其实可以通过拷贝值进行单次计算即可
        /// 后期考虑英雄防御值的动态变化，这里采取事实计算的模式
        /// </summary>
        void SkillCallBack_ElementShieldDeal()
        { 
             Entities
            .WithName("HeroSkillElementShieldDeal")
            .ForEach((
                ref HeroEntityMasterTag masterTag,
                ref HeroAttributeCmpt heroAttr,
                ref HeroIntgratedNoImmunityState stateNoImmunity,
                ref SkillElementShieldTag_Hero skillElementShieldTag
                ) =>
            {
                if (!skillElementShieldTag.active)
                {
                    skillElementShieldTag.damageReduction = 0;
                    skillElementShieldTag.damageAmplification = 0;
                    heroAttr.attackAttribute.heroDynamicalAttack.tempMasterDamagePar = 1;
                }
                else
                {
                    skillElementShieldTag.damageReduction = 0.2f + 0.01f * skillElementShieldTag.level;

                    // 元素护盾失效条件
                    if (heroAttr.defenseAttribute.energy < 0.001f)
                    {
                        skillElementShieldTag.active = false;
                        _heroSkills.SkillSetActiveElementShield(false);
                        return;
                        
                    }

                    // 1. 元素护盾减伤（基于抗性，每种不超过 5% + 升级加成）
                    if (skillElementShieldTag.enableSecondA)
                    {
                        var resist = heroAttr.defenseAttribute.resistances;
                        var lvl = skillElementShieldTag.level;
                        float maxResistBonus = 0.05f + 0.01f * lvl;
                        float frostDR = math.min(resist.frost * 0.2f / 1f * 0.01f, maxResistBonus);
                        float lightningDR = math.min(resist.lightning * 0.2f / 1f * 0.01f, maxResistBonus);
                        float poisonDR = math.min(resist.poison * 0.2f / 1f * 0.01f, maxResistBonus);
                        float shadowDR = math.min(resist.shadow * 0.2f / 1f * 0.01f, maxResistBonus);
                        float fireDR = math.min(resist.fire * 0.2f / 1f * 0.01f, maxResistBonus);
                        float totalReduction = 0.2f + 0.01f * lvl + frostDR + lightningDR + poisonDR + shadowDR + fireDR;
                        skillElementShieldTag.damageReduction = totalReduction;
                    }

                    // 2. 元素护盾期间伤害提升
                    if (skillElementShieldTag.enableSecondB)
                    {
                        var elementDmg = heroAttr.attackAttribute.elementalDamage;
                        var lvl = skillElementShieldTag.level;
                        float maxAmp = 0.10f + 0.005f * lvl;
                        float ampFromElement = (
                            elementDmg.frostDamage +
                            elementDmg.lightningDamage +
                            elementDmg.poisonDamage +
                            elementDmg.shadowDamage +
                            elementDmg.fireDamage
                        ) * 0.1f * 0.01f;
                        float totalAmp = math.min(0.20f + ampFromElement, maxAmp);

                        skillElementShieldTag.damageAmplification = totalAmp;
                        heroAttr.attackAttribute.heroDynamicalAttack.tempMasterDamagePar = 1 + totalAmp;
                    }
                }
            })
            .WithoutBurst().Run();
        }

        /// <summary>
        /// 暗影之拥 处理
        /// </summary>
        /// <param name="timer"></param>
        /// <param name="prefab"></param>
        /// <param name="ecb"></param>
        void SkillCallBack_ShadowEmbrace(float timer, EntityCommandBuffer ecb)
        {

            //潜行状态处理
            Entities
                    .WithName("HeroSkillShadowEmbraceDeal")                  
                    .ForEach((ref HeroEntityMasterTag masterTag, ref HeroAttributeCmpt heroAttr, ref SkillShadowEmbrace_Hero skillHeroTag, ref LocalTransform transform) =>
                    {
                        skillHeroTag.tagSurvivalTime -= timer;
                        if (skillHeroTag.tagSurvivalTime <= 0)
                            skillHeroTag.active = false;

                        if (!skillHeroTag.active && !skillHeroTag.initialized)
                        {
                            skillHeroTag.initialized = true;

                            heroAttr.defenseAttribute.moveSpeed = _orignalHeroAttributeCmp.defenseAttribute.moveSpeed;
                            heroAttr.gainAttribute.energyRegen = _orignalHeroAttributeCmp.gainAttribute.energyRegen;
                            heroAttr.gainAttribute.hpRegen = _orignalHeroAttributeCmp.gainAttribute.hpRegen;
                            Hero.instance.skillAttackPar.stealth = false;//时间到期被动释放
                            var entiyShadowEmbrace = ecb.Instantiate(_prefabs.HeroSkill_ShadowEmbrace);
                            ecb.SetComponent(entiyShadowEmbrace, transform);
                            //手动计算暴击
                            var skillCalParOverrride = Hero.instance.CalculateBaseSkillDamage(1);//必定触发暴击
                            if (skillHeroTag.enableSecondB)
                            {
                                skillCalParOverrride = Hero.instance.CalculateBaseSkillDamage(1, 0, (0.15f * skillHeroTag.shadowTime * (1 + 0.01f * skillHeroTag.level)));
                            }
                            //写回暴击参数,压制
                            skillCalParOverrride.shadowDotDamage = skillCalParOverrride.shadowDamage;//触发暗蚀
                            ecb.AddComponent(entiyShadowEmbrace, skillCalParOverrride);
                            Hero.instance.CalculateBaseSkillDamage();//再重新计算一次以手动更新，避免其他技能受影响
                            ecb.AddComponent(entiyShadowEmbrace, new SkillShadowEmbraceTag { tagSurvivalTime = 0.5f });
                            ecb.AddBuffer<HitRecord>(entiyShadowEmbrace);
                            ecb.AddBuffer<HitElementResonanceRecord>(entiyShadowEmbrace);
                            //释放技能清零暗影藏匿计时器
                            skillHeroTag.shadowTime = 0;
                        }
                        if (skillHeroTag.active && skillHeroTag.enableSecondB)
                        {
                            skillHeroTag.shadowTime += timer;

                        }
                        if (skillHeroTag.active)
                        {

                        }                      

                    }).WithoutBurst().WithStructuralChanges().Run();

            //基本状态处理攻击效果
            Entities
                    .ForEach((ref SkillShadowEmbraceTag skillTag, ref SkillsDamageCalPar skillCal) =>
                    {

                        skillTag.tagSurvivalTime -= timer;
                        if (skillTag.tagSurvivalTime <= 0)
                            skillCal.destory = true;

                    }).WithoutBurst().Run();

            //开启A阶段效果
            Entities.
                    ForEach((VisualEffect vfx,ref SkillShadowEmbraceAOverTimeTag skillTag,ref SkillsOverTimeDamageCalPar skillsOver,ref OverlapOverTimeQueryCenter overlapOverTime,ref LocalTransform transform) =>
                    {
                        overlapOverTime.center = _heroPositon;
                        transform.Position = _heroPositon;                                       
                         if (!SystemAPI.GetComponent<SkillShadowEmbrace_Hero>(_heroEntity).active)
                            skillsOver.destory = true;

                    }).WithoutBurst().Run();     

        }

        //瘟疫蔓延处理
        void SkillCallBack_PlagueSpread(float timer)
        {
            Entities
            .WithName("HeroSkillPlagueSpreadDeal")
            .ForEach((ref HeroEntityMasterTag masterTag, ref HeroAttributeCmpt heroAttr, ref SkillPlagueSpread_Hero skillTag) =>
            {
                skillTag.tagSurvivalTime -= timer;
                if (skillTag.active)
                {
                    if (skillTag.tagSurvivalTime <= 0)
                    {
                        //配置读取可以不用硬编码了
                        heroAttr.defenseAttribute.energy -= skillTag.energyCost * timer;
                    }
                    if (heroAttr.defenseAttribute.energy <= -0.1f)
                    {
                        skillTag.active = false;

                    }
                    if (!skillTag.initialized)
                    {
                        skillTag.initialized = true;

                        heroAttr.attackAttribute.dotProcChance.poisonChance += 0.35f+(skillTag.level * 0.015f);

                        //持续性伤害 加成最高80%
                        if (skillTag.enableSecondA)
                        {
                            heroAttr.attackAttribute.dotDamage += 0.6f + (skillTag.level * 0.02f);
                        }
                        //持续性伤害暴击伤害 加成最高50%
                        if (skillTag.enableSecondB)
                        {
                            heroAttr.attackAttribute.dotCritDamage += 0.35f + (skillTag.level * 0.015f);

                        }

                    }
                }
                else
                {
                    if (skillTag.initialized)
                    {
                        skillTag.initialized = false;

                        heroAttr.attackAttribute.dotProcChance.poisonChance = _orignalHeroAttributeCmp.attackAttribute.dotProcChance.poisonChance;

                        //持续性伤害 加成最高80%
                        if (skillTag.enableSecondA)
                        {
                            heroAttr.attackAttribute.dotDamage = _orignalHeroAttributeCmp.attackAttribute.dotDamage;
                        }
                        //持续性伤害暴击伤害 加成最高50%
                        if (skillTag.enableSecondB)
                        {
                            heroAttr.attackAttribute.dotCritDamage = _orignalHeroAttributeCmp.attackAttribute.dotCritDamage;
                        }
                    }
                }
            }).WithoutBurst().Run();
        }
        /// <summary>
        /// 烈焰冲锋
        /// </summary>
        /// <param name="timer"></param>
        /// <param name="ecb"></param>
        void SkillCallBack_FlameCharge(float timer, EntityCommandBuffer ecb)
        {
            //烈焰痕迹 燃烧的消失
            Entities
            .ForEach((VisualEffect vfx, ref SkillFlameChargeTag skillTag, ref SkillsOverTimeDamageCalPar skillCal) =>
            {
                skillTag.tagSurvivalTime -= timer;
                if (skillTag.tagSurvivalTime <= 0)
                    skillCal.destory = true;
                //提前1秒关闭燃烧效果
                if (skillTag.tagSurvivalTime <= 1 && skillTag.tagSurvivalTime > 1 - timer)
                    vfx.Stop();

            }).WithoutBurst().Run();

             //烈焰爆冲 瞬时
            Entities
            .ForEach((VisualEffect vfx, ref SkillFlameChargeATag skillTag,ref SkillsDamageCalPar skillCal,ref LocalTransform transform) =>
            {
                skillTag.tagSurvivalTime -= timer;
                transform.Position = _heroPositon;
                
                if (skillTag.tagSurvivalTime <= 0)
                    skillCal.destory = true;
            
            }).WithoutBurst().Run();

       }
        //冰霜护盾 处理,寒冰持续时间60秒？
        void SkillCallBack_FrostShieldDeal(float timer, ScenePrefabsSingleton prefab, EntityCommandBuffer ecb)
        {
            Entities
            .WithName("HeroSkillFrostShieldDeal")
            .ForEach((ref HeroEntityMasterTag masterTag,
                ref HeroAttributeCmpt heroAttr,
                ref HeroIntgratedNoImmunityState stateNoImmunity,
                ref SkillFrostShieldTag_Hero skillFrostShieldTag
                ) =>
                {
                    skillFrostShieldTag.tagSurvivalTime -= timer;
                    //这里也可以执行单次检测？
                    if (skillFrostShieldTag.relaseSkill && heroAttr.defenseAttribute.frostBarrier <= 0)
                    {
                        //DevDebug.LogError("冰霜护盾关闭");
                        skillFrostShieldTag.relaseSkill = false;
                        _heroSkills.SkillSetActiveFrostShield(false);
                        if (skillFrostShieldTag.enableSecondA)
                        {
                            //DevDebug.LogError("释放冰刺");   
                            skillFrostShieldTag.enableSecondA = false;
                            var iceCone = ecb.Instantiate(prefab.HeroSkillAssistive_FrostShieldA);
                            var iceConeTras = _transformLookup[_heroEntity];
                            iceConeTras.Scale = 3;
                            ecb.SetComponent(iceCone, iceConeTras);
                            //单次伤害采用爆发伤害计算
                            var skillBurstDamageCal = new SkillsBurstDamageCalPar() { };
                            //赋值冰刺伤害,赋英雄引用，赋冻结值,物理，触发冻伤
                            skillBurstDamageCal.frostDamage = skillFrostShieldTag.iceConeDamage;
                            skillBurstDamageCal.instantPhysicalDamage = skillFrostShieldTag.iceConeDamage;
                            skillBurstDamageCal.frostDotDamage = skillFrostShieldTag.iceConeDamage;
                            skillBurstDamageCal.damageChangePar = 1;//默认变化参数为1
                            skillBurstDamageCal.heroRef = _heroEntity;
                            skillBurstDamageCal.tempFreeze = 300;
                            ecb.AddComponent(iceCone, skillBurstDamageCal);
                            var filter = new CollisionFilter
                            {
                                //属于道具层
                                BelongsTo = 1u << 10,
                                //检测敌人
                                CollidesWith = 1u << 6,
                                GroupIndex = 0
                            };
                            var overlapBurst = new OverlapBurstQueryCenter { center = _transformLookup[_heroEntity].Position, radius = 7, filter = filter, offset = new float3(0, 0, 0), shape = OverLapShape.Sphere };
                            //添加瞬时伤害碰撞体
                            ecb.AddComponent(iceCone, overlapBurst);
                            //添加A阶段控制标签
                            ecb.AddComponent(iceCone, new SkillFrostShieldTagA() { tagSurvivalTime = 3 });

                        }


                    }
                    //通过增量时间量度 进行单次判断
                    if (skillFrostShieldTag.tagSurvivalTime <= 0 && skillFrostShieldTag.tagSurvivalTime > -timer)
                    {
                        _heroSkills.SkillSetActiveFrostShield(false);
                        heroAttr.defenseAttribute.frostBarrier = 0;
                    }

                }).WithoutBurst().Run();

            Entities
            .ForEach((ref SkillFrostShieldTagA skillTag,ref SkillsBurstDamageCalPar skillCalPar,ref OverlapBurstQueryCenter burstQueryCenter)=>
            {

                skillTag.tagSurvivalTime -= timer;
                //缩小碰撞避免持续检测
                if (skillTag.tagSurvivalTime <= 2.5f && skillTag.tagSurvivalTime > 2.5f - timer)
                    burstQueryCenter.radius = 0.01f;
                if (skillTag.tagSurvivalTime <= 0)
                        skillCalPar.destory = true;

            }).WithoutBurst().Run();



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