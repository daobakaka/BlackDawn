using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.VFX;

//���ڴ������ͷŵĻص�����burstCompile
namespace BlackDawn.DOTS
{/// <summary>
/// ��Ӣ��mono����,����Ⱦϵͳ֮�����,�ص�ϵͳ�� �漰����ͳclass������ �����������
/// </summary>
    [RequireMatchingQueriesForUpdate]
    [UpdateAfter(typeof(RenderEffectSystem))]
    [UpdateInGroup(typeof(ActionSystemGroup))]
    public partial class HeroSkillsCallbackSystemBase : SystemBase,IOneStepFun
    {
        public bool Done { get ; set ; }
        HeroSkills _heroSkills;
        ScenePrefabsSingleton _prefabs;

        //���ϵͳ����
        private SystemHandle _specialSkillsDamageSystemHandle;


        //�����ܵ�GPUbuffer
        GraphicsBuffer _arcaneCirclegraphicsBuffer;
        private bool resetVFXPartical;
        protected override void OnCreate()
        {
            base.OnCreate();
            //��Ӣ�۳�ʼ��ʱ����
            base.Enabled = false;

            _specialSkillsDamageSystemHandle = World.Unmanaged.GetExistingUnmanagedSystem<HeroSpecialSkillsDamageSystem>();
            _arcaneCirclegraphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 5000, sizeof(float) * 3);

        }

        protected override void OnStartRunning()
        {
           //��ȡӢ�ۼ��ܵ���
            _heroSkills = HeroSkills.GetInstance();

            _prefabs = SystemAPI.GetSingleton<ScenePrefabsSingleton>();

        }

        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var timer = SystemAPI.Time.DeltaTime;


            var specialDanafeSystem = World.Unmanaged.GetUnsafeSystemRef<HeroSpecialSkillsDamageSystem>(_specialSkillsDamageSystemHandle);
            //��Ⱦ����
            var arcaneCircleLinkedArray = specialDanafeSystem.arcaneCircleLinkenBuffer; 



            // **�������д��������ǵ�ʵ��**,������ҪΪ��������ECB������������foreach����ͬһ֡ʹ��
            //�������������Ч�ķ���
            if (false)
            Entities
                .WithName("SkillPulseSceondExplosionCallback") //����ײ��ǩ�������ڱ��
                .WithAll<SkillPulseSecondExplosionRequestTag>() //ƥ��ABC �������,Ĭ��ƥ��û�б�disnable�����
                //in ֻ������Ҫ�ŵ�ref ����
                .ForEach((Entity e,ref SkillsDamageCalPar damageCalPar,ref SkillPulseTag pulseTag,in LocalTransform t ) =>   
                {
                   // ���� Mono ��ı�ը�߼��������������׶�
                 var entity=   _heroSkills.DamageSkillsExplosionProp(
                        ecb,
                        _prefabs.ParticleEffect_DefaultEffexts, //��ը��Ч                        
                        t.Position,
                        t.Rotation,
                        1,
                        0, 0, pulseTag.scaleChangePar, false, true
                    );
                    ecb.AddComponent(entity, new SkillPulseTag() { tagSurvivalTime = 2 ,scaleChangePar=pulseTag.scaleChangePar});//Ϊ���׶μ������ɴ���ǩ,���ﴫ���α����,��������
                    //���ַ�ʽ�����γɽṹ�ı�               
                    ecb.SetComponentEnabled<SkillPulseSecondExplosionRequestTag>(e,false);
                    //���٣���ʱ�Ѿ������˶��׶μ��ܣ����׶μ���û�б�ǩ�����ص���һ�׶ν�������
                    ecb.DestroyEntity(e);
                })                
                .WithoutBurst()   // ����ر� Burst�����ܵ����κ� UnityEngine/Mono ����
                .Run();


            //���崦��
            Entities
                .WithName("SkillPulseSceondVFXExplosionCallback") //����ֱ�ӱ�������VFX�ص���ʶ
                .WithAll<SkillPulseSecondExplosionRequestTag>() //ƥ��ABC �������,Ĭ��ƥ��û�б�disnable�����
                                                                //in ֻ������Ҫ�ŵ�ref ����
                .ForEach((Entity e, VisualEffect vfx, ref SkillsDamageCalPar damageCalPar, ref SkillPulseTag pulseTag, ref LocalTransform t) =>
                {

                    vfx.SendEvent("hit");            
                    ecb.SetComponentEnabled<SkillPulseSecondExplosionRequestTag>(e, false);
                    //�л������ͱ�ը״̬
                    damageCalPar.enableExplosion = true;
                    damageCalPar.enablePull = false;
                    t.Scale *= (1+pulseTag.scaleChangePar);
                    //ȡ���ڶ��׶�״̬��2�������
                    pulseTag.tagSurvivalTime = 2;
                    pulseTag.enableSecond=false;
                    //���׶�ֹͣ�ƶ�
                    pulseTag.speed = 0;
              
                })
                .WithoutBurst()   // ����ر� Burst�����ܵ����κ� UnityEngine/Mono ����
                .Run();

            //��������
            Entities
                .WithName("SkillIceFireSceondVFXExplosionCallback") //����ֱ�ӱ�Ǳ������VFX�ص���ʶ
                .WithAll<SkillIceFireSecondExplosionRequestTag>() //ƥ��ABC �������,Ĭ��ƥ��û�б�disnable�����
                //in ֻ������Ҫ�ŵ�ref ����
                .ForEach((Entity e, VisualEffect vfx, ref SkillsDamageCalPar damageCalPar, ref SkillIceFireTag skillTag, ref LocalTransform t) =>
                {
                    //���ű�ը����
                    vfx.SendEvent("hit");
                    ecb.SetComponentEnabled<SkillIceFireSecondExplosionRequestTag>(e, false);

                    //�л������ͱ�ը״̬
                    damageCalPar.enableExplosion = false;
                    //��ը��������Ч��
                    damageCalPar.enablePull = true;
                    //��ը�������
                    t.Scale *= ( 1+skillTag.scaleChangePar);
                    //���ӱ�ը�˺�,����ֱ�����ӣ���Ϊ�ǽ���˷�����ֱ�ӼӾͿ���
                    damageCalPar.damageChangePar += skillTag.skillDamageChangeParTag;
                    //ȡ���ڶ��׶�״̬��2�������
                    skillTag.secondSurvivalTime = 4;
                    //��������Ч��
                    skillTag.enableSpecialEffect=false;
                    //���׶�ֹͣ�ƶ�
                   // skillTag.speed = 0;

                })
                .WithoutBurst()   // ����ر� Burst�����ܵ����κ� UnityEngine/Mono ����
                .Run();

            //�������ظ������
            Entities
                .WithName("Disabled_SkillIceFireSecondExplosion")
                .WithDisabled<SkillIceFireSecondExplosionRequestTag>()
                .ForEach((Entity entity, VisualEffect vfx,
                    ref SkillIceFireTag skillTag,
                    ref SkillsDamageCalPar damageCalPar,
                    ref LocalTransform transform)=>
                {
                 
                    skillTag.secondSurvivalTime -= timer;
                    //����ִ��һ�λָ��жϣ����벥�ű�ը��Ч2��֮�����
                    if (!skillTag.enableSpecialEffect&&skillTag.secondSurvivalTime<3)
                    {
                        vfx.SendEvent("create");
                        //�ָ��ߴ�
                        transform.Scale = skillTag.originalScale;
                        //�ָ���ʱ�����
                        damageCalPar.damageChangePar = 1;
                        //�ָ�����
                        damageCalPar.enablePull = false;
                        skillTag.enableSpecialEffect = true;                    
                    }
                             
                
                })
                .WithoutBurst() .Run();


            //����buffer��Ч�� ����Ҫ���������,Ŀǰ��ʱʹ�����ַ�ʽ���о�������ʽ��BUG,�ر��ǹ���buffer��������鷳
            if (_arcaneCirclegraphicsBuffer != null)
            {
                _arcaneCirclegraphicsBuffer.Dispose();
            }
        

        //�����������Ч,����buffer����
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
                        // ���� VFX
                        vfx.SetGraphicsBuffer("_LinkedTargets", _arcaneCirclegraphicsBuffer);  // �� VFX ��ƥ��

                        //��������׼�����֮�󣬽���buffer���
                        vfx.SendEvent("Custom5");
                        // buffer.Dispose();
                        resetVFXPartical = false;
                    }
                })
                .WithoutBurst().Run();

            //�����������Ч,����buffer����
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




            // ���Ų�����
            ecb.Playback(base.EntityManager);
            ecb.Dispose();


        }




        protected override void OnDestroy()
        {


            //����buffer��Ч�� ����Ҫ���������
            if (_arcaneCirclegraphicsBuffer != null)
            {
                _arcaneCirclegraphicsBuffer.Dispose(); 
            }
        }
    }
}