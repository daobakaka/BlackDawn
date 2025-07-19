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
using GameFrame.BaseClass;
using BlackDawn.DOTS;
using ProjectDawn.Entities;
using ProjectDawn.Navigation;
using Random = Unity.Mathematics.Random;
using System.Threading;



namespace BlackDawn
{
    public  sealed class SpawnCollection :  Singleton<SpawnCollection>
    {
        public ScenePrefabsSingleton prefabs;

       
       // public EntityManager entityManager;
       //怪物属性
        private MonsterAttributes _monsterAttributes;
        //英雄属性
        private HeroAttributeCmpt _heroAttributesCmpt; 

        /// <summary>
        /// 通过反射查找
        /// </summary>
        private SpawnCollection() 
                
        {
             //怪物属性类内部储存一个从json文件里面加载的怪物链表
            _monsterAttributes = MonsterAttributes.GetInstance();
            //获取英雄属性
            _heroAttributesCmpt = GlobalReadConfigs.instance.attributeCmpt;
            //获取entity管理器
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;


          
            //获取场景预制对象烘焙
            var entityPrefabQuery = entityManager.CreateEntityQuery(typeof(ScenePrefabsSingleton));
            if (entityPrefabQuery.TryGetSingleton<ScenePrefabsSingleton>(out var prefab))
            {

                prefabs = prefab;
            
            }


        }
        /// <summary>
        /// 英雄生成
        /// </summary>
        /// <returns></returns>
        public Entity InstantiateHero()
        {

            // 拿取system 世界的 单例进行控制
            var heroSystem = World
              .DefaultGameObjectInjectionWorld
              .Unmanaged;
            heroSystem.GetExistingSystemState<HeroSystem>().Enabled = true;

            //开启怪物操作系统
            var heroActionSystem = World.DefaultGameObjectInjectionWorld.Unmanaged;
            heroActionSystem.GetExistingSystemState<ActionSystem>().Enabled = true;

            //开启渲染系统
            var renderSystem = World.DefaultGameObjectInjectionWorld.Unmanaged;
            renderSystem.GetExistingSystemState<RenderEffectSystem>().Enabled = true;

            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            var ecb = new EntityCommandBuffer(Allocator.Temp);


            //从场景里那个“管理实体”上读出来 PrefabsComponentData
            // var query = entityManager.CreateEntityQuery(typeof(PrefabsComponentData));
            //var prefabsdata = query.GetSingleton<PrefabsComponentData>();


            var heroEntity = entityManager.Instantiate(prefabs.Hero);
            //构建英雄侦察器
            var detectionEntity = entityManager.Instantiate(prefabs.HeroDetector);
            

                if (entityManager.HasComponent<LocalTransform>(heroEntity))
                {
                    entityManager.SetComponentData(heroEntity, new LocalTransform
                    {
                        Position = new float3(0,0,0),
                        //  Position = new float3(0, 10, 0),
                        Rotation = quaternion.identity,
                        Scale = 1f
                    });
                }
              //英雄真身 唯一标签
              entityManager.AddComponentData(heroEntity, new HeroEntityMasterTag());
            //添加攻击目标组件
            entityManager.AddComponentData(heroEntity, new HeroAttackTarget());
                //后续规定除渲染类的烘焙，其余类型的组件均执行动态添加
                entityManager.AddComponentData(heroEntity, new HeroAttributeCmpt(_heroAttributesCmpt));



            //储存伤害锁定的hits,在专门的伤害锁定系统里更新,英雄受伤独立检查
            var heroHits = entityManager.AddBuffer<HeroHitRecord>(heroEntity);

            //添加动态距离侦察buffer,默认分配100 个槽位,这里使用标签的方式
            var hits = entityManager.AddBuffer<NearbyHit>(heroEntity);
            //添加用于计算减益效果的伤害计算
            var dotHits = entityManager.AddBuffer<HeroDotDamageBuffer>(heroEntity);


            //添加侦擦器 --------------------,添加bufferOwner的引用            
            entityManager.AddComponentData(detectionEntity, new Detection_DefaultCmpt() { bufferOwner =heroEntity,originalRadius=20});

            var filter = new CollisionFilter
            {
                BelongsTo = 1u << 8,
                CollidesWith = 1u << 6,
                GroupIndex = 0
            };
            //添加范围侦测的标签
            entityManager.AddComponentData(detectionEntity, new OverlapOverTimeQueryCenter { center = Hero.instance.transform.position, radius = 20, filter = filter });



            //未免疫状态标识，默认构造为1
            entityManager.AddComponentData(heroEntity, new HeroIntgratedNoImmunityState
            {
                controlNoImmunity = 1f,
                inlineDamageNoImmunity = 1f,
                dotNoImmunity = 1f,
                physicalDamageNoImmunity = 1f,
                elementDamageNoImmunity = 1f
            });
            //为子组件添加相关标识
            // 1) 读取 LinkedEntityGroup,连续递归，读取完毕
            var linked = entityManager.GetBuffer<LinkedEntityGroup>(heroEntity);
  
            //链接渲染体， 专门处理英雄的链接渲染特效
            var childLinkedEffects = linked[1].Value;         
            //添加结构， 失活
            ecb.AddComponent(childLinkedEffects, new HeroEffectsLinked());
            ecb.SetComponentEnabled<HeroEffectsLinked>(childLinkedEffects, false); // 设置为禁用

            //加载动态伤害组件,用于设计技能回调
           // ecb.AddComponent(heroEntity, new HeroDynamicalAttackAttribute() { });

            //元素护盾预加载组件
            ecb.AddComponent(heroEntity, new SkillElementShieldTag_Hero() { });

            //冰霜护盾预加载组件            
            ecb.AddComponent(heroEntity, new SkillFrostShieldTag_Hero() { });

            //进击 预加载组件
            ecb.AddComponent(heroEntity,new SkillAdvanceTag_Hero(){ });
            
            ecb.Playback(entityManager);
            ecb.Dispose();

            DevDebug.LogError("初始化英雄entity,初始化侦擦器" + heroEntity.Index);




            return heroEntity;

      
        }
        /// <summary>
        /// 用于某些技能生成英雄残影
        /// </summary>
        /// <returns></returns>
        public Entity InstantiateHeroShadow(float3 positon,quaternion quaternion)
        {

  
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            var ecb = new EntityCommandBuffer(Allocator.Temp);


            var heroEntity = entityManager.CreateEntity();
                         
           entityManager.AddComponentData(heroEntity, new LocalTransform
                    {
                        Position = positon,
                        //  Position = new float3(0, 10, 0),
                        Rotation = quaternion,
                        Scale = 1f
                    });
                
              //英雄分身 残影等 标签
              entityManager.AddComponentData(heroEntity, new  HeroEntityBranchTag());

            
            ecb.Playback(entityManager);
            ecb.Dispose();

            DevDebug.LogError("生成英雄残影");




            return heroEntity;

      
        }

           
        /// <summary>
        /// 怪物生成
        /// </summary>
        /// <param name="name"></param>
        /// <param name="entityPrefab"></param>
        /// <param name="types"></param>
     public  Entity InstantiateMonster(MonsterName name, Entity entityPrefab)
        {

            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var attributeGet = _monsterAttributes.monserDic[name];
            //为子entity动态加载相关buffer
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            //为子entity动态加载相关buffer
            int total = GameManager.instance.testCount;
            for (int i = 0; i < total; ++i)
            {
                var monster = EntityInstantiateOnCircle(entityPrefab, Hero.instance.transform.position, 70, i, total);


                //var filter = new CollisionFilter
                //{
                //    BelongsTo = 1u << 6,
                //    CollidesWith = 1u << 8,
                //    GroupIndex = 0
                //};
                ////添加范围侦测的标签
                //ecb.AddComponent(monster ,new OverlapQueryCenter { Center = Hero.instance.transform.position, Radius = 1, Filter = filter });


                //添加怪物标签 锁轴？
                ecb.AddComponent(monster, new MonsterComponent());
                //添加怪物激活标签
                ecb.AddComponent(monster, new LiveMonster());
                //添加动画控制标签
                ecb.AddComponent(monster, new AnimationControllerData());
                //添加伤害飞行道具buffer缓冲， 初始容量10
                ecb.AddBuffer<FlightPropAccumulateData>(monster);
                //添加伤害英雄技能buffer缓冲， 初始容量5
                ecb.AddBuffer<HeroSkillPropAccumulateData>(monster);
                //添加DOT计算BUFFER缓冲， 初始容量15
                ecb.AddBuffer<MonsterDotDamageBuffer>(monster);


                //--技能预定义标签区域 -- 雷霆之握
                ecb.AddComponent(monster, new PreDefineHeroSkillThunderGripTag());
                ecb.SetComponentEnabled<PreDefineHeroSkillThunderGripTag>(monster, false);

                //--技能预定义标签区域 -- 黑炎
                ecb.AddComponent(monster, new PreDefineHeroSkillBlackFrameATag());
                ecb.SetComponentEnabled<PreDefineHeroSkillBlackFrameATag>(monster, false);
                ecb.AddComponent(monster, new PreDefineHeroSkillBlackFrameBTag());
                ecb.SetComponentEnabled<PreDefineHeroSkillBlackFrameBTag>(monster, false);

                

                // 基础属性赋值
                var monsterBaseAttribute = attributeGet.baseAttribute;
                // 攻击属性赋值
                var attackAttribute = attributeGet.attackAttribute;
                // 防御属性赋值
                var defenseAttribute = attributeGet.defenseAttribute;
                // rngState 的随机值
                defenseAttribute.rngState = (uint)UnityEngine.Random.Range(1, int.MaxValue);
                var rng = new Random(defenseAttribute.rngState);
            
                // 增益属性赋值
                var gainAttribute = attributeGet.gainAttribute;
                // 减益池赋值
                var lossPoolAttribute = attributeGet.lossPoolAttribute;           
                // debuff 赋值
                var debuffAttribute = attributeGet.debuffAttribute;
                debuffAttribute.dotTimer = rng.NextFloat(0, 1);//这里给一个随机时间，避免DOT中所有怪出现同一时间掉DOY
                // 控制力赋值
                var controlledEffectAttribute = attributeGet.controlledEffectAttribute;

                // 添加到实体（所有 AddComponentData ➔ ecb.AddComponent）
                ecb.AddComponent(monster, monsterBaseAttribute);
                ecb.AddComponent(monster, attackAttribute);
                ecb.AddComponent(monster, defenseAttribute);
                ecb.AddComponent(monster, gainAttribute);
                ecb.AddComponent(monster, lossPoolAttribute);
                ecb.AddComponent(monster, debuffAttribute);
                ecb.AddComponent(monster, controlledEffectAttribute);
                //加入临时标记组件
               // ecb.AddComponent(monster, new MonsterTempDamageText());

                DevDebug.Log("生成怪物");
                //设置移动速度和攻击范围，这里就可以通过插件直接计算，免去外部计算的消耗,注意基础属性是从上面加载,但测试之后消耗增加了？？
                //这里已经事先烘焙好了
                var agentLocomotion = entityManager.GetComponentData<AgentLocomotion>(monster);
               // agentLocomotion.StoppingDistance = attributeCmpt.gainAttribute.atkRange;
                agentLocomotion.Speed = defenseAttribute.moveSpeed;
                ecb.SetComponent(monster, agentLocomotion);
       
                //三种怪物攻击类型，近战、远程、混合
                switch (monsterBaseAttribute.attackType)
                {
                    case MonsterAttackType.Melee:
                        ecb.AddComponent(monster, new AtMelee());
                        break;
                    case MonsterAttackType.Ranged:
                        ecb.AddComponent(monster, new AtRanged());
                        break;
                    case MonsterAttackType.Hybrid:
                        ecb.AddComponent(monster, new AtHybrid());
                        break;

                }

                // 1) 读取 LinkedEntityGroup,连续递归，读取完毕
                var linked = entityManager.GetBuffer<LinkedEntityGroup>(monster);
                ////需要手动添加， baker会自动添加
                //  var childern = EntityManager.GetBuffer<Child>(monster);
                //基础渲染体
                var child = linked[1].Value;
                //伤害飘字预制体
                var childText = linked[2].Value;
                //dot伤害飘字预制体
                var childDotText = linked[3].Value; 
                //火焰预制体
                var childFire = linked[4].Value;
                //暗影预制体
                var childShadow = linked[5].Value;
                //闪电预制体
                var childLightning = linked[6].Value;
                //毒素预制体
                var childPoison =linked[7].Value;
                //流血预制体
                var childBleed = linked[8].Value;
                //黑炎预制体
                var childBlackFrame = linked[9].Value;

           

                // 2) 只给有渲染的子实体加覆盖组件
                if (entityManager.HasComponent<MaterialMeshInfo>(child)
                 && !entityManager.HasComponent<UnderAttackColor>(child))
                {
                    // 受攻击
                    ecb.AddComponent(child, new UnderAttackColor
                    {
                        Value = float4.zero
                    });
                    //溶解
                    ecb.AddComponent(child, new DissolveEffect());
                    //菲涅尔
                    ecb.AddComponent(child, new EmissionColor());
                    //冰锥体
                    ecb.AddComponent(child, new ConeStrength() { Value = 0.2f });
                    //冰冻
                    ecb.AddComponent(child, new FrostIntensity());
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
                    //非固定化设置，每次特效效果，都可以不一样，配套的随机值和rng
                    ecb.AddComponent(child, new RenderRngState() { rngState = defenseAttribute.rngState });
                    //池化偏移，防止过度效果， 这里直接传入初始化值
                    ecb.AddComponent(child, new RandomOffset() { Value = new float4(rng.NextFloat(-1, 1), rng.NextFloat(-1, 1), 0, 0) });

                    //ecb.AddComponent<Disabled>(childFire);
                    //texCOlor 模块 放在第二个子物体身上
                    // 添加 TextColor 
                    ecb.AddComponent(childText, new TextColor { Value = new float4(1, 1, 1, 1) });

                    // 添加 Char1UVRect
                    ecb.AddComponent(childText, new Char1UVRect { Value = new float4(0, 0, 0, 0) });

                    // 添加 Char2UVRect
                    ecb.AddComponent(childText, new Char2UVRect { Value = new float4(0, 0, 0, 0) });

                    // 添加 Char3UVRect
                    ecb.AddComponent(childText, new Char3UVRect { Value = new float4(0, 0, 0, 0) });

                    // 添加 Char4UVRect
                    ecb.AddComponent(childText, new Char4UVRect { Value = new float4(0, 0, 0, 0) });

                    // 添加 Char5UVRect
                    ecb.AddComponent(childText, new Char5UVRect { Value = new float4(0, 0, 0, 0) });

                    // 添加 Char6UVRect
                    ecb.AddComponent(childText, new Char6UVRect { Value = new float4(0, 0, 0, 0) });

                    // 添加 Offset
                    ecb.AddComponent(childText, new TextOffset { Value = new float2(0, 0) });

                    // 添加 Scale，这里就是默认为0了
                    ecb.AddComponent(childText, new TextScale { Value = 0 });
                    //添加开始时间
                    ecb.AddComponent(childText, new TextStartTime { Value = 0 });
                    //加入临时标记组件
                    ecb.AddComponent(childText, new MonsterTempDamageText());
                    //直接设置disable ，避免初始化时出现0
                    ecb.SetComponentEnabled<MonsterTempDamageText>(childText, false);

                    //添加随机偏移
                    ecb.AddComponent(childText, new RenderRngState() { rngState = defenseAttribute.rngState });
                    ecb.AddComponent(childText, new RandomOffset());

                    //可能第三个子物体专门用于渲染DOT伤害,主要向下飘字
                    // 添加 TextColor 
                    ecb.AddComponent(childDotText, new TextColor { Value = new float4(1, 1, 1, 1) });

                    // 添加 Char1UVRect
                    ecb.AddComponent(childDotText, new Char1UVRect { Value = new float4(0, 0, 0, 0) });

                    // 添加 Char2UVRect
                    ecb.AddComponent(childDotText, new Char2UVRect { Value = new float4(0, 0, 0, 0) });

                    // 添加 Char3UVRect
                    ecb.AddComponent(childDotText, new Char3UVRect { Value = new float4(0, 0, 0, 0) });

                    // 添加 Char4UVRect
                    ecb.AddComponent(childDotText, new Char4UVRect { Value = new float4(0, 0, 0, 0) });

                    // 添加 Char5UVRect
                    ecb.AddComponent(childDotText, new Char5UVRect { Value = new float4(0, 0, 0, 0) });

                    // 添加 Char6UVRect
                    ecb.AddComponent(childDotText, new Char6UVRect { Value = new float4(0, 0, 0, 0) });

                    // 添加 Offset
                    ecb.AddComponent(childDotText, new TextOffset { Value = new float2(0, 0) });

                    // 添加 Scale，这里就是默认为0了
                    ecb.AddComponent(childDotText, new TextScale { Value = 0 });
                    //添加开始时间
                    ecb.AddComponent(childDotText, new TextStartTime { Value = 0 });
                    //加入临时标记组件
                    ecb.AddComponent(childDotText, new MonsterTempDotDamageText());
                    //直接设置disable ，避免初始化时出现0
                    ecb.SetComponentEnabled<MonsterTempDotDamageText>(childDotText, false);

                    //添加随机偏移
                    ecb.AddComponent(childDotText, new RenderRngState() { rngState = defenseAttribute.rngState });
                    ecb.AddComponent(childDotText, new RandomOffset());

                    //可能会有第四个子物体专门用于渲染DOT伤害

                }

                //添加怪物特定标签

                //添加火焰预制体的随机标签
           
                //添加火焰预制体的随机标签,这里采用统一的四个偏移值，传入都为（-1，1）,再加上时间参数进行随机化
                ecb.AddComponent(childFire, new FireRandomOffset() { Value = new float4(rng.NextFloat4(-1, 1))});
                //添加暗影预制体的随机标签
                ecb.AddComponent(childShadow, new DarkShadowRandomOffset() { Value = new float4(rng.NextFloat4(-1, 1))});
                //添加闪电预制体的随机标签
                ecb.AddComponent(childLightning, new LightningRandomOffset { Value = new float4(rng.NextFloat4(-1, 1)) });
                //添加毒素预制体的随机标签
                ecb.AddComponent(childPoison, new PoisoningRandomOffset { Value = new float4(rng.NextFloat4(-1, 1)) });
                //添加流血预制体的随机标签
                ecb.AddComponent(childBleed, new BleedRandomOffset { Value = new float4(rng.NextFloat4(-1, 1)) });
                //添加黑炎预制体的随机标签
                ecb.AddComponent(childBlackFrame, new BlackFrameRandomOffset { Value = new float4(rng.NextFloat4(-1, 1)) });
               
                switch (name)
                {


                    case MonsterName.Zombie:
                        ecb.AddComponent(monster, new MoZombieCmp());
                        break;
                    case MonsterName.Albono:
                        ecb.AddComponent(monster, new MoAlbonoCmp());
                        break;
                    case MonsterName.AlbonoUpper:
                        ecb.AddComponent(monster, new MoAlbonoUpperCmp());

                        break;

                }
            }
            // 3) 立即执行所有记录的操作
            ecb.Playback(entityManager);
            ecb.Dispose();
            return entityPrefab;
        }

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


    }
}