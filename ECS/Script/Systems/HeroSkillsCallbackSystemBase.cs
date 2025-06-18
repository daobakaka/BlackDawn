using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.VFX;

//用于处理技能释放的回调，非burstCompile
namespace BlackDawn.DOTS
{/// <summary>
/// 由英雄mono开启,在渲染系统之后进行,回调系统， 涉及到传统class交互， 设计在最后进行
/// </summary>
    [RequireMatchingQueriesForUpdate]
    [UpdateAfter(typeof(RenderEffectSystem))]
    [UpdateInGroup(typeof(ActionSystemGroup))]
    public partial class HeroSkillsCallbackSystemBase : SystemBase,IOneStepFun
    {
        public bool Done { get ; set ; }
        HeroSkills _heroSkills;
        ScenePrefabsSingleton _prefabs;

        //侦测系统缓存
        private SystemHandle _specialSkillsDamageSystemHandle;


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

        }

        protected override void OnStartRunning()
        {
           //获取英雄技能单例
            _heroSkills = HeroSkills.GetInstance();

            _prefabs = SystemAPI.GetSingleton<ScenePrefabsSingleton>();

        }

        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var timer = SystemAPI.Time.DeltaTime;


            var specialDanafeSystem = World.Unmanaged.GetUnsafeSystemRef<HeroSpecialSkillsDamageSystem>(_specialSkillsDamageSystemHandle);
            //渲染链接
            var arcaneCircleLinkedArray = specialDanafeSystem.arcaneCircleLinkenBuffer; 



            // **遍历所有打了请求标记的实体**,这里需要为方法传入ECB，这样可以在foreach里面同一帧使用
            //这是针对粒子特效的方法
            if (false)
            Entities
                .WithName("SkillPulseSceondExplosionCallback") //程序底层打签名，用于标记
                .WithAll<SkillPulseSecondExplosionRequestTag>() //匹配ABC 所有组件,默认匹配没有被disnable的组件
                //in 只读，需要放到ref 后面
                .ForEach((Entity e,ref SkillsDamageCalPar damageCalPar,ref SkillPulseTag pulseTag,in LocalTransform t ) =>   
                {
                   // 调用 Mono 层的爆炸逻辑，继续设连锁阶段
                 var entity=   _heroSkills.DamageSkillsExplosionProp(
                        ecb,
                        _prefabs.ParticleEffect_DefaultEffexts, //爆炸特效                        
                        t.Position,
                        t.Rotation,
                        1,
                        0, 0, pulseTag.scaleChangePar, false, true
                    );
                    ecb.AddComponent(entity, new SkillPulseTag() { tagSurvivalTime = 2 ,scaleChangePar=pulseTag.scaleChangePar});//为二阶段技能生成存活标签,这里传入形变参数,持续两秒
                    //这种方式不会形成结构改变               
                    ecb.SetComponentEnabled<SkillPulseSecondExplosionRequestTag>(e,false);
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
                    t.Scale *= (1+pulseTag.scaleChangePar);
                    //取消第二阶段状态，2秒后销毁
                    pulseTag.tagSurvivalTime = 2;
                    pulseTag.enableSecond=false;
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
                    t.Scale *= ( 1+skillTag.scaleChangePar);
                    //增加爆炸伤害,这里直接增加，因为是进入乘法区，直接加就可以
                    damageCalPar.damageChangePar += skillTag.skillDamageChangeParTag;
                    //取消第二阶段状态，2秒后销毁
                    skillTag.secondSurvivalTime = 4;
                    //允许特殊效果
                    skillTag.enableSpecialEffect=false;
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
                    ref LocalTransform transform)=>
                {
                 
                    skillTag.secondSurvivalTime -= timer;
                    //两秒执行一次恢复判断，必须播放爆炸特效2秒之后进行
                    if (!skillTag.enableSpecialEffect&&skillTag.secondSurvivalTime<3)
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
                .WithoutBurst() .Run();


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

            //法阵的链接特效,传入buffer数组
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




            // 播放并清理
            ecb.Playback(base.EntityManager);
            ecb.Dispose();


        }




        protected override void OnDestroy()
        {


            //法阵buffer的效果 ，需要在外面清除
            if (_arcaneCirclegraphicsBuffer != null)
            {
                _arcaneCirclegraphicsBuffer.Dispose(); 
            }
        }
    }
}