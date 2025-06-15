using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;


namespace BlackDawn.DOTS
{
    //需要引用外部单例
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial class GameControllerSystemBase : SystemBase,IECSSyncedMono
    {
        ComponentLookup<LocalTransform> m_transform;
        ComponentLookup<LocalToWorld> m_localtoWorld;
        MonsterAttributes _monsterAttributes;
        Hero _heroManager;
        public  ScenePrefabsSingleton prefabs;
        bool kk;

        public bool Enable { get ; set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            //默认禁用，由MONO场景管理开启
            base.Enabled = false;
            // 要等 PrefabsComponentData 存在才能执行
            RequireForUpdate<ScenePrefabsSingleton>();

        }
        public void OnSceneEcsReady()
        {

            if (!Enable)
            {
                m_transform = GetComponentLookup<LocalTransform>(true);
                m_localtoWorld = GetComponentLookup<LocalToWorld>(true);
                prefabs = SystemAPI.GetSingleton<ScenePrefabsSingleton>();
               // _heroManager = Hero.instance;
                _monsterAttributes = MonsterAttributes.GetInstance();
                Enable = true;
                Debug.Log("初始化ECS场景管理");
            }
        }
        [BurstCompile]
        void UpDataComponentLookup(SystemBase system)
        {
            m_transform.Update(system);
            m_localtoWorld.Update(system);
        }
        [BurstCompile]
        protected override void OnUpdate()
        {
           // Debug.Log("base 系统开始更新");
            if (!GameManager.instance.Enable) return;
            OnSceneEcsReady();             
            UpDataComponentLookup(this);

            //if (Input.GetKeyDown(KeyCode.Alpha1))
            //{
            //    InstantiateMonster("Zombie_GpuEcsAnimator",prefabs.Zombie_level1,MonsterTypes.ZombieLevel1);

            //}
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                //InstantiateMonster(MonsterName.Zombie, prefabs.Zombie_level1Agnet);

            }



        }
        [BurstCompile]
        /// <summary>
        /// 根据“圆分布”策略实例化一个实体：
        /// 在以 center 为圆心、radius 为半径的圆周上，
        /// 第 index/ count 个位置，rotation 会朝向圆心
        /// </summary>
        Entity EntityInstantiateOnCircle(
            Entity prefab,
            float3 center,
            float radius,
            int index,
            int count)
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            // 1) 克隆实体
            var ent = em.Instantiate(prefab);

            // 2) 计算圆周上的位置
            //    平均分成 count 份，每份角度 = 2π / count
            float angleStep = math.PI * 2f / count;
            float angle = index * angleStep;

            // 3) 求出该角度对应的点（XZ 平面）
            float3 pos = center + new float3(
                math.cos(angle) * radius,
                center.y,                      // y = 圆心的 y
                math.sin(angle) * radius);

            // 4) 让实体面朝圆心（可按需反向）
            float3 toCenter = math.normalizesafe(center - pos);
            quaternion rot = quaternion.LookRotationSafe(toCenter, math.up());

            // 5) 应用 LocalTransform
            var tsf = em.GetComponentData<LocalTransform>(ent);
            tsf.Position = pos;
            tsf.Rotation = rot;
            em.SetComponentData(ent, tsf);

            // 6) 如果带刚体，禁止它在 XZ 以外的转动
            if (em.HasComponent<Unity.Physics.PhysicsMass>(ent))
            {
                var pm = em.GetComponentData<Unity.Physics.PhysicsMass>(ent);
                pm.InverseInertia = new float3(0, pm.InverseInertia.y, 0);
                em.SetComponentData(ent, pm);
            }

            return ent;
        }
        [BurstCompile]
        public void InstantiateMonster(MonsterName name,Entity entityPrefab)
        {

            //var entityarr = EntityManager.Instantiate(prefabs.Watcher_A, 100, Allocator.Temp);
            int total = GameManager.instance.testCount;
            for (int i = 0; i < total; ++i)
            {
                var monster = EntityInstantiateOnCircle(entityPrefab, new float3(0, 0, 0), 50,i,total);
                //添加怪物标签 锁轴？
                EntityManager.AddComponentData(monster, new MonsterComponent());

                //添加怪物动态buffer缓冲， 后续用于其他控制
                EntityManager.AddBuffer<BuffHandlerBuffer>(monster);
                //添加怪物特定标签
                switch (name)
                {
                    case MonsterName.Zombie:
                        EntityManager.AddComponentData(monster, new MoZombieCmp());
                        break;
                             
                
                
                }
                //从单例读取
                MonsterAttributeCmpt attributeCmpt = new MonsterAttributeCmpt();
                var attributeGet = _monsterAttributes.monserDic[name];
                // 逐个赋值
                attributeCmpt.attackAttribute.attackPower = attributeGet.attackAttribute.attackPower;
                attributeCmpt.defenseAttribute.hp = attributeGet.defenseAttribute.hp;
                attributeCmpt.defenseAttribute.originalHp = attributeGet.defenseAttribute.originalHp;
                //rngState 的随机值
                attributeCmpt.defenseAttribute.rngState = (uint)UnityEngine.Random.Range(1, int.MaxValue);
                attributeCmpt.gainAttribute.atkRange = attributeGet.gainAttribute.atkRange;
                attributeCmpt.lossPoolAttribute = attributeGet.lossPoolAttribute;
                attributeCmpt.debuffAttribute = attributeGet.debuffAttribute;
                attributeCmpt.controlledEffectAttribute = attributeGet.controlledEffectAttribute;

                // 添加到实体
                EntityManager.AddComponentData(monster, attributeCmpt);

                // EntityManager.AddComponentData(monster, new Idle());
                // EntityManager.SetComponentEnabled<Idle>(monster, true);
                EntityManager.AddComponentData(monster, new Run());
                EntityManager.SetComponentEnabled<Run>(monster, true);


                //为子entity动态加载相关buffer
                var ecb = new EntityCommandBuffer(Allocator.Temp);
                // 1) 读取 LinkedEntityGroup,连续递归，读取完毕
                var linked = EntityManager.GetBuffer<LinkedEntityGroup>(monster);
                ////需要手动添加， baker会自动添加
              //  var childern = EntityManager.GetBuffer<Child>(monster);
               
                    var child = linked[1].Value;
                    var childFire = linked[2].Value;

                    // 2) 只给有渲染的子实体加覆盖组件
                    if (EntityManager.HasComponent<MaterialMeshInfo>(child)
                     && !EntityManager.HasComponent<UnderAttackColor>(child))
                    {
                        // 受攻击
                        ecb.AddComponent(child, new UnderAttackColor
                        {
                            Value = float4.zero
                        });
                        //溶解
                        ecb.AddComponent(child, new DissolveEffect());
                        //菲涅尔
                        ecb.AddComponent(child, new FresnelColor());
                        //冰冻
                        ecb.AddComponent(child, new FrostIntensity());
                        //冰锥体
                        ecb.AddComponent(child, new ConeStrength() { Value = 0.2f });
                        //火焰
                        ecb.AddComponent(child, new FireIntensity());
                        //毒素
                        ecb.AddComponent(child, new PoisoningIntensity());
                        //闪电
                        ecb.AddComponent(child, new LightningIntensity());
                        //暗影
                        ecb.AddComponent(child, new DarkShadowIntensity());
                        //Alpha
                        ecb.AddComponent(child, new AlphaIntensity() { Value = 1 });

                        //失活火焰特效
                        ecb.AddComponent<Disabled>(childFire);

                    }
                

                // 3) 立即执行所有记录的操作
                ecb.Playback(EntityManager);
                ecb.Dispose();
            }


        }


       

     
    }



}

