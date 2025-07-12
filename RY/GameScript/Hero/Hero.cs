using System;
using System.Collections;
using System.Collections.Generic;
using BlackDawn.DOTS;
using GameFrame.EventBus;
using GameFrame.Fsm;
using GameFrame.Runtime;
using ProjectDawn.Entities;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using Random = UnityEngine.Random;
namespace BlackDawn
{
    /// <summary>
    /// 英雄类，主类，状态机初始化， 基础攻击释放，附魔技能释放，
    /// </summary>
    public class Hero : MonoBehaviour
    {
        [HideInInspector] public Animator animator;
        [HideInInspector] public Vector3 targetPosition; //点击的鼠标位置
        [HideInInspector] public Vector3 skillTargetPositon;//点击鼠标右键的位置
        [HideInInspector] public Vector3 skillElur;//技能欧拉角旋转
        [HideInInspector] public Vector3 cameraDistance;
        [HideInInspector] public Entity heroEntity;//英雄自己的Entity
        [HideInInspector] public HeroAttributeCmpt attributeCmpt; //英雄属性继承Icomponent接口
        [HideInInspector] public FlightPropDamageCalPar flightProDamageCalPar;//飞行道具伤害参数
        [HideInInspector] public SkillsDamageCalPar skillsDamageCalPar;//英雄技能伤害参数
        [HideInInspector] public SkillsOverTimeDamageCalPar skillsOverTimeDamageCalPar;//英雄持续性技能，降低buffer压力
        [HideInInspector] public SkillsBurstDamageCalPar skillsBurstDamageCalPar;//英雄爆发类技能伤害计算    
        [HideInInspector] public IInputOperate inputOperate; //按键输入类
        [HideInInspector] public bool enableOperate = true;
        [HideInInspector] public EventBusManager eventBusManager; //事件总线管理
        //全局控制状态机
        private IFsm<Hero> _fsm;
        //全局携程控制器
        [HideInInspector] public CoroutineController coroutineController;
        //base系统交互
        [HideInInspector] public GameControllerSystemBase gameControllerSystem;
        [HideInInspector] public EntityManager entityManager;
        [HideInInspector] public BeginSimulationEntityCommandBufferSystem ecbSystem;
        [HideInInspector] public EntityCommandBuffer entityCommandBuffer;
        //英雄主动技能引用
        [HideInInspector] public HeroSkills heroSkills;
        //1左手中指延伸 2右手中指 3.元素护盾 4. 冰霜护盾
        public Transform[] skillTransforms;

        


        //英雄实例
        public static Hero instance { get { return _heroInstance; } }
        private static Hero _heroInstance;
        //内部参数
        private float _timer;
        private TempBaseAttackPar _attackPar;
        //临时技能参数 需要其他类调取，暂时设置为共用
       [HideInInspector] public TempSkillAttackPar skillAttackPar;

        /// <summary>
        /// 临时基础攻击参数结构体
        /// </summary>
        private struct TempBaseAttackPar 
        {
           public float timer;
           public int capacity;
           public bool isReloading;    // 是否正在换弹
           public float reloadTimer;    // 换弹计时
        }

        /// <summary>
        /// 临时技能参数结构体，通常为附魔类技能使用
        /// </summary>
        public struct TempSkillAttackPar
        {
            //通用时间标识
            public float timer;
            //寒冰附魔时间
            public float frostEnchantmentTimer;
            //暗能附魔时间
            public float darkEnergyEnhantmentTimer;
            //暗能容量
            public int darkEnergyCapacity;
            //寒冰容量
            public int frostCapacity;
            public bool isReloading;    // 是否正在冷却
            public float reloadTimer;    // 冷却时间
            //-----
            public bool enableSpecialEffect; //是否触发特殊效果

            public bool enableFrostSecond;

            //寒冰分裂次数
            public int frostSplittingCount;
            //寒冰碎片数量
            public int frostShardCount;
            //寒冰状态切换伤害参数变化
            public float frostSkillChangePar;
            //寒冰的临时冻结值
            public float tempFreeze;
        }


        void Awake()
        {
            //hero实例
            _heroInstance = this;
            animator = GetComponent<Animator>();
            cameraDistance = new Vector3(0, 10, -10);
            enableOperate = true;
            //初始化注册事件
            inputOperate = InputOperateHandle.CreateOperate();
            //局内初始化
            InnerGameIns();


            //创建自己的状态机
            _fsm = FsmManager.Instans.CreateFsm(this, new List<FsmState<Hero>>()
            {
                new Hero_Idle(),
                new Hero_Run(),
                new Hero_Roll(),
                new Hero_Skill(),
            });
            _fsm.Start<Hero_Idle>();

        }
        private Hero() { }
        // Start is called before the first frame update
        void Start()
        {
       
            DevDebug.Log("物理系统+------------------------："+Physics.simulationMode);

   
            //测试
            //HeroAttack(1);
        }

        void InnerGameIns()
        {
            //携程控制器
            coroutineController = CoroutineController.instance;
            //获取全局EntityManager
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            //获取systemBase控制系统单例
            gameControllerSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<GameControllerSystemBase>();
            //获取ECB 组件创建引用
            ecbSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            //初始化英雄，ECS打包默认流式加载，这是个坑
            heroEntity = SpawnCollection.GetInstance().InstantiateHero();
            //获取英雄属性,这里已经计算过了
            attributeCmpt = GlobalReadConfigs.instance.attributeCmpt;
      
            //获取事件总线管理类
            eventBusManager = EventBusManager.GetInstance();
            //测试事件,订阅
            eventBusManager.eventBus.Subscribe<PlayerTestEvent>(eventBusManager.TestEvent);
            //初始化基础攻击容器，传入原始弹匣容量
            _attackPar = new TempBaseAttackPar() { capacity = attributeCmpt.weaponAttribute.itemCapacity ,reloadTimer=0, isReloading = false};
            //初始化技能参数，这里暂时使用测试值,增加灵能能，暂时为空
            skillAttackPar = new TempSkillAttackPar() {  };
                       
            //获取英雄技能单例,放到后面，便于前面初始化成功
            heroSkills = HeroSkills.GetInstance();


            //开启英雄技能回调系统，这里应该放在英雄技能类单例获取后面
            var heroSkillsCallbackSystemBase = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<HeroSkillsCallbackSystemBase>();
            heroSkillsCallbackSystemBase.Enabled = true;

            //开启技能动态系统
            var heroSkillMonoSystem = World.DefaultGameObjectInjectionWorld.Unmanaged;
            heroSkillMonoSystem.GetExistingSystemState<HeroSkillsMonoSystem>().Enabled = true;



        }

        // Update is called once per frame
        void Update()
        {
            if (!GameManager.instance.Enable) return;
            //变换同步
            SynchronizationTransform();
            //飞行道具伤害计算
            CalculateBaseFlightPropDamage();
            //技能伤害计算
            CalculateBaseSkillDamage();
            //爆发类技能伤害计算
            CalculateBurstSkillDamage();
            //持续性技能伤害计算
            CalculateBaseOverTimeSkillDamage();
            //技能攻速变化？可以一直受攻击状态或者buffer的加成影响
            CalculateSkillAttackSpeed();
            //附魔类技能的附魔时间计算
            CalcuateSkillEnhancementTimer();



            //   DevDebug.Log("Mono目标位置" + targetPosition);
        }

        void OnDestroy()
        {
            //销毁自身状态机
            FsmManager.DestroyFsm<List<FsmState<Hero>>>();
            //取消事件订阅
            eventBusManager.eventBus.Unsubscribe<PlayerTestEvent>(eventBusManager.TestEvent);
        }


        void LoadSkill()
        {


        }
        void LoadGameProp()
        {



        }
        /// <summary>
        /// 带burst Mono的优化方法,感觉差别不大
        /// </summary>
        /// <param name="attackInterval"></param>
        /// <param name="propNumber"></param>
        /// <param name="sectorAngle"></param>
        public void HeroAttackBurst()
        {

            //DevDebug.Log("开始射击");
            
            // 缓存属性
            var weaponAttr = attributeCmpt.weaponAttribute;
            var atkAttr = attributeCmpt.attackAttribute;
            float dt = Time.deltaTime;
            //这里切换为武器攻速
            float attackInterval = 1f / Mathf.Max(atkAttr.weaponAttackSpeed, 0.01f);
            // —— 1. 冷却累加 ——  
            _attackPar.timer += dt;

            // —— 2. 如果正在换弹 ——  
            if (_attackPar.isReloading)
            {
                // 换弹计时
                _attackPar.reloadTimer += dt;
                if (_attackPar.reloadTimer >= weaponAttr.reloadTime)
                {
                    // 换弹完成
                    _attackPar.isReloading = false;
                    _attackPar.capacity = weaponAttr.itemCapacity;
                    _attackPar.reloadTimer = 0f;
                }
                // 返还，等待完全换弹
                return;
            }

            // —— 3. 冷却未到，不能射击 ——  
            if (_attackPar.timer < attackInterval)
                return;
            _attackPar.timer = 0f;

            // —— 4. 弹匣空，触发换弹 ——  
            if (_attackPar.capacity <= 0)
            {
                _attackPar.isReloading = true;
                _attackPar.reloadTimer = 0f;
                return;
            }
            var det = entityManager.GetComponentData<HeroAttackTarget>(heroEntity);
            if (det.attackTarget == Entity.Null) return;


            // —— 5. 真正射击 ——  
  
            _attackPar.capacity--;

            // 只取 XZ 平面计算一次中心方向
            float3 heroPos = transform.position;
            float3 targetPos = entityManager.GetComponentData<LocalTransform>(det.attackTarget).Position;
            float3 rawDir = targetPos - heroPos;
            rawDir.y = 0f;
            float3 centerDir = math.normalize(rawDir);

            //新建查询
            var prefabs = entityManager.CreateEntityQuery(typeof(ScenePrefabsSingleton)).GetSingleton<ScenePrefabsSingleton>();

            var ecbParallel = ecbSystem.CreateCommandBuffer().AsParallelWriter();

            // 只要参数检查无误，就调度 propNumber 次的并行 Job
            var job = new HeroAttackParallelJob
            {
                HeroPosition = transform.position,
                CenterDir = centerDir,                            // 传入预计算的方向
                PropNumber = attributeCmpt.weaponAttribute.pelletCount,
                SectorAngle = attributeCmpt.weaponAttribute.sectorAngle,
                Prefabs = prefabs,
                PropSpeed = attributeCmpt.weaponAttribute.propSpeed,
                DamageCalData = flightProDamageCalPar,   // ← 把伤害数据传进来
                Ecb = ecbParallel
            };

            // propNumber 个并行实例，每个实例执行一次 Execute(index)
            JobHandle handle = job.Schedule(attributeCmpt.weaponAttribute.pelletCount, attributeCmpt.weaponAttribute.pelletCount /*batch count*/);

            ecbSystem.AddJobHandleForProducer(handle);

            //基础攻击变化1
            //这里增加附魔攻击判断
            if (skillAttackPar.darkEnergyCapacity > 0)
            {
                DevDebug.LogError("释放暗影能量体附魔技能");
                HeroSkillDarkEnergyAttackBurst();
            }
            //寒冰释放判断
            if (skillAttackPar.frostCapacity == 1 && _attackPar.capacity == 1)
            {

                DevDebug.LogError("释放寒冰球附魔技能");
                HeroSkillForestAttack();
            }
        }

        /// <summary>
        /// 技能暗能的释放逻辑，这里在技能状态机里面进行，这里是附魔类技能， 所以写在英雄类里面，对应的job生成的飞行道具类型不同
        /// 这里设计为与基础技能并行释放， 从外部根据初始化的TempSkillPar来控制技能的输出和输入，并改变玩家特效状态
        /// </summary>
        public void HeroSkillDarkEnergyAttackBurst()
        {
            skillAttackPar.darkEnergyCapacity--;
            var det = entityManager.GetComponentData<HeroAttackTarget>(heroEntity);
            // 只取 XZ 平面计算一次中心方向
            float3 heroPos = transform.position;
            float3 targetPos = entityManager.GetComponentData<LocalTransform>(det.attackTarget).Position;
            float3 rawDir = targetPos - heroPos;
            rawDir.y = 0f;
            float3 centerDir = math.normalize(rawDir);

            //新建查询
            var prefabs = entityManager.CreateEntityQuery(typeof(ScenePrefabsSingleton)).GetSingleton<ScenePrefabsSingleton>();

            var ecbParallel = ecbSystem.CreateCommandBuffer().AsParallelWriter();

            // 只要参数检查无误，就调度 propNumber 次的并行 Job, 这里是执行英雄类的技能逻辑
            var job = new HeroSkillDarkEnergyAttackParallelJob
            {
                HeroPosition = transform.position,
                CenterDir = centerDir,                            // 传入预计算的方向
                PropNumber = attributeCmpt.weaponAttribute.pelletCount,
                SectorAngle = attributeCmpt.weaponAttribute.sectorAngle,
                Prefabs = prefabs,
                PropSpeed = attributeCmpt.weaponAttribute.propSpeed,
                DamageCalData = skillsDamageCalPar,   // ← 把伤害数据传进来
                EnableSpecialEffect = skillAttackPar.enableSpecialEffect,//传入是否开启特殊效果
                Ecb = ecbParallel
            };

            // propNumber 个并行实例，每个实例执行一次 Execute(index)
            JobHandle handle = job.Schedule(attributeCmpt.weaponAttribute.pelletCount, attributeCmpt.weaponAttribute.pelletCount /*batch count*/);

            ecbSystem.AddJobHandleForProducer(handle);
        }

        /// <summary>
        /// 附魔类技能寒冰初始化逻辑
        /// </summary>
        public void HeroSkillForestAttack()
       
        {
            skillAttackPar.frostCapacity--;
            var det = entityManager.GetComponentData<HeroAttackTarget>(heroEntity);
            // 只取 XZ 平面计算一次中心方向
            float3 heroPos = transform.position;
            float3 targetPos = entityManager.GetComponentData<LocalTransform>(det.attackTarget).Position;
            float3 rawDir = targetPos - heroPos;
            rawDir.y = 0f;
            float3 centerDir = math.normalize(rawDir);

            //新建查询
            var prefabs = entityManager.CreateEntityQuery(typeof(ScenePrefabsSingleton)).GetSingleton<ScenePrefabsSingleton>();
            var ecb = ecbSystem.CreateCommandBuffer();

            //entityManager.Instantiate(prefabs.HeroSkill_Frost);

            Entity prop = ecb.Instantiate(prefabs.HeroSkill_Frost);

            ecb.SetComponent( prop, new LocalTransform
            {
                Position = transform.position,
                Rotation = quaternion.LookRotation(centerDir, math.up()),
                Scale = 1f

            });

            // 添加寒冰技能标识,这里技能道具的释放速度可以直接写死，后期可以根据道具设计，增加一个飞行速度值
            //传入起始位置
            ecb.AddComponent( prop, new SkillFrostTag
            {
                tagSurvivalTime = 10,
                speed = 5,
                originalPosition = transform.position,
                enableSecond =  skillAttackPar.enableFrostSecond,
                hitCount = skillAttackPar.frostSplittingCount,
                shrapnelCount =skillAttackPar.frostShardCount,
                //传入分裂伤害参数
                skillDamageChangeParTag = skillAttackPar.frostSkillChangePar,
                //传入碎片的冻结参数
                enableSpecialEffect =skillAttackPar.enableSpecialEffect,
               

            });


            var damagePar = skillsDamageCalPar;
            damagePar.tempFreeze = skillAttackPar.tempFreeze;
            // 添加伤害计算组件
            ecb.AddComponent( prop, damagePar);

            //默认命中容量50个，为道具添加命中参数
            var hits = ecb.AddBuffer<HitRecord>(prop);
            hits.Capacity = 50;
            ecb.AddBuffer<HitElementResonanceRecord>( prop);



        }




        /// <summary>
        /// 基础飞行道具伤害计算，结果写入 _flightProDamageCalPar 中
        /// 忽略“控制加成”和“怪物减免”
        /// </summary>
        public void CalculateBaseFlightPropDamage()
        {

            //0 基础参数计算,默认0.02秒销毁,传入英雄伤害索引
            flightProDamageCalPar.hitSurvivalTime = 0.02f;
            flightProDamageCalPar.heroRef = heroEntity;

            // 1. 取出当前的攻击属性
            var atkAttr = attributeCmpt.attackAttribute;

            // 2. 随机判定各触发状态
            bool critTriggered = Random.value <= atkAttr.physicalCritChance;
            bool vulTriggered = Random.value <= atkAttr.vulnerabilityChance;
            bool supTriggered = Random.value <= atkAttr.suppressionChance;
            bool dotCritTriggered = Random.value <= atkAttr.dotCritChance;//Dot暴击
            bool elemCritTriggered = Random.value <= atkAttr.elementalCritChance;  // 元素暴击判定

                flightProDamageCalPar.critTriggered = critTriggered;
                flightProDamageCalPar.vulTriggered = vulTriggered;
            flightProDamageCalPar.supTriggered = supTriggered;
            flightProDamageCalPar.elemCritTriggered = elemCritTriggered;
                flightProDamageCalPar.dotCritTriggered = dotCritTriggered;


            // 3. 各倍率：伤害加成永远生效，其他触发则加成，否则为1
            float baseMul = 1f + atkAttr.damage;
            // 物理暴击
            float critMul = critTriggered ? 1f + atkAttr.critDamage : 1f;
            // 易伤
            float vulMul = vulTriggered ? 1f + atkAttr.vulnerabilityDamage : 1f;
            // 压制
            float supMul = supTriggered ? 1f + atkAttr.suppressionDamage : 1f;
            // dot暴击
            float dotCritMul = dotCritTriggered ? 1f + atkAttr.dotCritDamage : 1f;
            // 元素暴击
            float elemCritMul = elemCritTriggered ? 1f + atkAttr.elementalCritDamage : 1f;
            float extra = atkAttr.extraDamage;

            // 4. 计算瞬时物理伤害（含暴击）
            flightProDamageCalPar.instantPhysicalDamage =
                atkAttr.attackPower
              * baseMul
              * critMul
              * vulMul
              * supMul
              + extra;

            // 5. 计算各元素瞬时伤害（不使用物理暴击，使用元素暴击判定）
            //    按公式：atk * (1 + damage + elementDamage) * vulMul * supMul * elemCritMul + extra
            //元素伤害不需要再加1，因为物理已经计算过
            float commonMul = baseMul * vulMul * supMul * elemCritMul;
            flightProDamageCalPar.frostDamage =
                atkAttr.attackPower
              * (atkAttr.elementalDamage.frostDamage)
              * commonMul
              + extra;
            flightProDamageCalPar.lightningDamage =
                atkAttr.attackPower
              * (atkAttr.elementalDamage.lightningDamage)
              * commonMul
              + extra;
             flightProDamageCalPar.poisonDamage =
                atkAttr.attackPower
              * (atkAttr.elementalDamage.poisonDamage)
              * commonMul
              + extra;
            flightProDamageCalPar.shadowDamage =
                atkAttr.attackPower
              * (atkAttr.elementalDamage.shadowDamage)
              * commonMul
              + extra;
            flightProDamageCalPar.fireDamage =
                atkAttr.attackPower
              * (atkAttr.elementalDamage.fireDamage)
              * commonMul
              + extra;

            // 6. 计算持续性DOT伤害（对所有类型相同公式）,不造成压制
            //    按公式：atk * (1 + damage) * dotCritMul + extra + dotDamage
            // 6. 计算持续性DOT伤害
            float dotBaseMul = baseMul * dotCritMul * vulMul;
            float dotExtra = extra + atkAttr.dotDamage;
            float potentialDotDamage = atkAttr.attackPower * dotBaseMul + dotExtra;
            //这里根据触发几率，写出各伤害
            flightProDamageCalPar.frostDotDamage = Random.value <= atkAttr.dotProcChance.frostChance ? potentialDotDamage : 0f;
            flightProDamageCalPar.lightningDotDamage = Random.value <= atkAttr.dotProcChance.lightningChance ? potentialDotDamage : 0f;
            flightProDamageCalPar.poisonDotDamage = Random.value <= atkAttr.dotProcChance.poisonChance ? potentialDotDamage : 0f;
            flightProDamageCalPar.shadowDotDamage = Random.value <= atkAttr.dotProcChance.shadowChance ? potentialDotDamage : 0f;
            flightProDamageCalPar.fireDotDamage = Random.value <= atkAttr.dotProcChance.fireChance ? potentialDotDamage : 0f;
            flightProDamageCalPar.bleedDotDamage = Random.value <= atkAttr.dotProcChance.bleedChance ? potentialDotDamage : 0f;

            //7 计算伤害类型触发枚举,计算完成之后，方便后字体渲染的方法进行获取
            flightProDamageCalPar.damageTriggerType = CalculateDamageTriggerType(critTriggered, vulTriggered, supTriggered, dotCritTriggered, elemCritTriggered);
            

            //这里计算完了英雄自己属性的伤害触发， 后面的DOT和控制状态判断，可由怪物自身触发
        }


        DamageTriggerType CalculateDamageTriggerType(bool critTriggered,bool vulTriggered, bool supTriggered,bool dotCritTriggered,bool elemCritTriggered)
        {

            if (critTriggered && vulTriggered && supTriggered)
                return DamageTriggerType.SuppressionVulnCrit;

            // 其次判断双效果组合
            else if (critTriggered && vulTriggered)
                return DamageTriggerType.VulnerableCritical;

            else if (critTriggered && supTriggered)
                return DamageTriggerType.SuppressionCritical;

            else if (vulTriggered && supTriggered)
                return DamageTriggerType.SuppressionVulnerable;

            // 再判断单效果
            else if (critTriggered)
                return DamageTriggerType.CriticalStrike;

            else if (vulTriggered)
                return DamageTriggerType.Vulnerable;

            else if (supTriggered)
                return DamageTriggerType.Suppression;

            else
                return DamageTriggerType.NormalAttack;
        }



        /// <summary>
        /// 同步Transform组件位置，内含同步技能伤害计算
        /// </summary>
        void SynchronizationTransform()
        {

            //同步自身的Entity
            var heroEntitytransform = entityManager.GetComponentData<LocalTransform>(heroEntity);
            heroEntitytransform.Position = transform.position;
            heroEntitytransform.Rotation = transform.rotation;
            entityManager.SetComponentData(heroEntity, heroEntitytransform);
            //传回位置即可
            // transform.position = heroEntitytransform.Position;
            //
            
      


        }
        /// <summary>
        ///技能基础伤害计算，暂定技能只能有一个
        /// </summary>
        public void CalculateBaseSkillDamage()
        {

            
            skillsDamageCalPar.heroRef = heroEntity;
            // 1. 取出当前的攻击属性
            var atkAttr = attributeCmpt.attackAttribute;
            var skillAttr = attributeCmpt.skillDamageAttribute;

            // 2. 随机判定各触发状态
            bool critTriggered = Random.value <= atkAttr.physicalCritChance;
            bool vulTriggered = Random.value <= atkAttr.vulnerabilityChance;
            bool supTriggered = Random.value <= atkAttr.suppressionChance;
            bool dotCritTriggered = Random.value <= atkAttr.dotCritChance;//Dot暴击
            bool elemCritTriggered = Random.value <= atkAttr.elementalCritChance;  // 元素暴击判定

            skillsDamageCalPar.critTriggered = critTriggered;
            skillsDamageCalPar.vulTriggered = vulTriggered;
            skillsDamageCalPar.supTriggered = supTriggered;
            skillsDamageCalPar.elemCritTriggered = elemCritTriggered;
            skillsDamageCalPar.dotCritTriggered = dotCritTriggered;

            // 3. 各倍率：伤害加成永远生效，其他触发则加成，否则为1
            float baseMul = 1f + atkAttr.damage;
            // 物理暴击
            float critMul = critTriggered ? 1f + atkAttr.critDamage : 1f;
            // 易伤
            float vulMul = vulTriggered ? 1f + atkAttr.vulnerabilityDamage : 1f;
            // 压制
            float supMul = supTriggered ? 1f + atkAttr.suppressionDamage : 1f;
            // dot暴击
            float dotCritMul = dotCritTriggered ? 1f + atkAttr.dotCritDamage : 1f;
            // 元素暴击
            float elemCritMul = elemCritTriggered ? 1f + atkAttr.elementalCritDamage : 1f;
            float extra = atkAttr.extraDamage;

            // 4. 计算瞬时物理伤害（含暴击）
            skillsDamageCalPar.instantPhysicalDamage =
                skillAttr.baseDamage
              * baseMul
              * critMul
              * vulMul
              * supMul*skillAttr.physicalFactor
              + extra;

            // 5. 计算各元素瞬时伤害（不使用物理暴击，使用元素暴击判定）
            //    按公式：atk * (1 + damage + elementDamage) * vulMul * supMul * elemCritMul + extra
            //这里的技能元素伤害需要+1 ，因为技能是基于基础伤害直接造成元素或者物理伤害，可以直接加成，并且要乘以加成因子，技能后面会有受某项属性加成的影响
            float commonMul = baseMul * vulMul * supMul * elemCritMul;
            skillsDamageCalPar.frostDamage =
               skillAttr.baseDamage
              * (1+atkAttr.elementalDamage.frostDamage)
              * commonMul*skillAttr.frostFactor
              + extra;
            skillsDamageCalPar.lightningDamage =
               skillAttr.baseDamage
              * (1+atkAttr.elementalDamage.lightningDamage)
              * commonMul*skillAttr.lightningFactor
              + extra;
            skillsDamageCalPar.poisonDamage =
               skillAttr.baseDamage
             * (1+atkAttr.elementalDamage.poisonDamage)
             * commonMul*skillAttr.poisonFactor
             + extra;
            skillsDamageCalPar.shadowDamage =
                skillAttr.baseDamage
              * (1+atkAttr.elementalDamage.shadowDamage)
              * commonMul*skillAttr.shadowFactor
              + extra;
            skillsDamageCalPar.fireDamage =
                skillAttr.baseDamage
              * (1+atkAttr.elementalDamage.fireDamage)
              * commonMul*skillAttr.fireFactor
              + extra;

            // 6. 计算持续性DOT伤害（对所有类型相同公式）,不造成压制
            //    按公式：atk * (1 + damage) * dotCritMul + extra + dotDamage
            // 6. 计算持续性DOT伤害
            float dotBaseMul = baseMul * dotCritMul * vulMul;
            float dotExtra = extra + atkAttr.dotDamage;
            float potentialDotDamage = skillAttr.baseDamage * dotBaseMul + dotExtra;

            //这里增加英雄技能造成dot伤害的概率
            // skillsDamageCalPar.frostDotDamage = Random.value <= atkAttr.dotProcChance.frostChance+skillAttr.dotSkillProcChance.frostChance ? potentialDotDamage : 0f;
            // skillsDamageCalPar.lightningDotDamage = Random.value <= atkAttr.dotProcChance.lightningChance+skillAttr.dotSkillProcChance.lightningChance ? potentialDotDamage : 0f;
            // skillsDamageCalPar.poisonDotDamage = Random.value <= atkAttr.dotProcChance.poisonChance +skillAttr.dotSkillProcChance.poisonChance? potentialDotDamage : 0f;
            // skillsDamageCalPar.shadowDotDamage = Random.value <= atkAttr.dotProcChance.shadowChance + skillAttr.dotSkillProcChance.shadowChance ? potentialDotDamage : 0f;
            // skillsDamageCalPar.fireDotDamage = Random.value <= atkAttr.dotProcChance.fireChance + skillAttr.dotSkillProcChance.fireChance ? potentialDotDamage : 0f;
            // skillsDamageCalPar.bleedDotDamage = Random.value <= atkAttr.dotProcChance.bleedChance + skillAttr.dotSkillProcChance.bleedChance ? potentialDotDamage: 0f;

            //这里增加一个用于技能分离触发的测试版本
            skillsDamageCalPar.frostDotDamage = Random.value <= skillAttr.dotSkillProcChance.frostChance ? potentialDotDamage : 0f;
            skillsDamageCalPar.lightningDotDamage = Random.value <= skillAttr.dotSkillProcChance.lightningChance ? potentialDotDamage : 0f;
            skillsDamageCalPar.poisonDotDamage = Random.value <= skillAttr.dotSkillProcChance.poisonChance? potentialDotDamage : 0f;
            skillsDamageCalPar.shadowDotDamage = Random.value <= skillAttr.dotSkillProcChance.shadowChance ? potentialDotDamage : 0f;
            skillsDamageCalPar.fireDotDamage = Random.value <= skillAttr.dotSkillProcChance.fireChance ? potentialDotDamage : 0f;
            skillsDamageCalPar.bleedDotDamage = Random.value <= skillAttr.dotSkillProcChance.bleedChance ? potentialDotDamage: 0f;
            
           
            //这里计算完了英雄自己属性的伤害触发， 后面的DOT和控制状态判断，可由怪物自身触发

            //增加 伤害变化参数,默认为1
            skillsDamageCalPar.damageChangePar = 1;
            //默认并行数量最高100
            skillsDamageCalPar.ParallelCount = 100;
            //传入 skillDamage 的伤害类型
            skillsDamageCalPar.damageTriggerType = CalculateDamageTriggerType(critTriggered, vulTriggered, supTriggered, dotCritTriggered, elemCritTriggered);
        }
        /// <summary>
        /// 爆发技能伤害计算
        /// </summary>
          public void CalculateBurstSkillDamage()
        {


            skillsBurstDamageCalPar.heroRef = heroEntity;
            // 1. 取出当前的攻击属性
            var atkAttr = attributeCmpt.attackAttribute;
            var skillAttr = attributeCmpt.skillDamageAttribute;

            // 2. 随机判定各触发状态
            bool critTriggered = Random.value <= atkAttr.physicalCritChance;
            bool vulTriggered = Random.value <= atkAttr.vulnerabilityChance;
            bool supTriggered = Random.value <= atkAttr.suppressionChance;
            bool dotCritTriggered = Random.value <= atkAttr.dotCritChance;//Dot暴击
            bool elemCritTriggered = Random.value <= atkAttr.elementalCritChance;  // 元素暴击判定

            skillsBurstDamageCalPar.critTriggered = critTriggered;
            skillsBurstDamageCalPar.vulTriggered = vulTriggered;
            skillsBurstDamageCalPar.supTriggered = supTriggered;
            skillsBurstDamageCalPar.elemCritTriggered = elemCritTriggered;
            skillsBurstDamageCalPar.dotCritTriggered = dotCritTriggered;

            // 3. 各倍率：伤害加成永远生效，其他触发则加成，否则为1
            float baseMul = 1f + atkAttr.damage;
            // 物理暴击
            float critMul = critTriggered ? 1f + atkAttr.critDamage : 1f;
            // 易伤
            float vulMul = vulTriggered ? 1f + atkAttr.vulnerabilityDamage : 1f;
            // 压制
            float supMul = supTriggered ? 1f + atkAttr.suppressionDamage : 1f;
            // dot暴击
            float dotCritMul = dotCritTriggered ? 1f + atkAttr.dotCritDamage : 1f;
            // 元素暴击
            float elemCritMul = elemCritTriggered ? 1f + atkAttr.elementalCritDamage : 1f;
            float extra = atkAttr.extraDamage;

            // 4. 计算瞬时物理伤害（含暴击）
            skillsBurstDamageCalPar.instantPhysicalDamage =
                skillAttr.baseDamage
              * baseMul
              * critMul
              * vulMul
              * supMul*skillAttr.physicalFactor
              + extra;

            // 5. 计算各元素瞬时伤害（不使用物理暴击，使用元素暴击判定）
            //    按公式：atk * (1 + damage + elementDamage) * vulMul * supMul * elemCritMul + extra
            //这里的技能元素伤害需要+1 ，因为技能是基于基础伤害直接造成元素或者物理伤害，可以直接加成，并且要乘以加成因子，技能后面会有受某项属性加成的影响
            float commonMul = baseMul * vulMul * supMul * elemCritMul;
            skillsBurstDamageCalPar.frostDamage =
               skillAttr.baseDamage
              * (1+atkAttr.elementalDamage.frostDamage)
              * commonMul*skillAttr.frostFactor
              + extra;
            skillsBurstDamageCalPar.lightningDamage =
               skillAttr.baseDamage
              * (1+atkAttr.elementalDamage.lightningDamage)
              * commonMul*skillAttr.lightningFactor
              + extra;
            skillsBurstDamageCalPar.poisonDamage =
               skillAttr.baseDamage
             * (1+atkAttr.elementalDamage.poisonDamage)
             * commonMul*skillAttr.poisonFactor
             + extra;
            skillsBurstDamageCalPar.shadowDamage =
                skillAttr.baseDamage
              * (1+atkAttr.elementalDamage.shadowDamage)
              * commonMul*skillAttr.shadowFactor
              + extra;
            skillsBurstDamageCalPar.fireDamage =
                skillAttr.baseDamage
              * (1+atkAttr.elementalDamage.fireDamage)
              * commonMul*skillAttr.fireFactor
              + extra;

            // 6. 计算持续性DOT伤害（对所有类型相同公式）,不造成压制
            //    按公式：atk * (1 + damage) * dotCritMul + extra + dotDamage
            // 6. 计算持续性DOT伤害
            float dotBaseMul = baseMul * dotCritMul * vulMul;
            float dotExtra = extra + atkAttr.dotDamage;
            float potentialDotDamage = skillAttr.baseDamage * dotBaseMul + dotExtra;

            //这里增加英雄技能造成dot伤害的概率
            // skillsDamageCalPar.frostDotDamage = Random.value <= atkAttr.dotProcChance.frostChance+skillAttr.dotSkillProcChance.frostChance ? potentialDotDamage : 0f;
            // skillsDamageCalPar.lightningDotDamage = Random.value <= atkAttr.dotProcChance.lightningChance+skillAttr.dotSkillProcChance.lightningChance ? potentialDotDamage : 0f;
            // skillsDamageCalPar.poisonDotDamage = Random.value <= atkAttr.dotProcChance.poisonChance +skillAttr.dotSkillProcChance.poisonChance? potentialDotDamage : 0f;
            // skillsDamageCalPar.shadowDotDamage = Random.value <= atkAttr.dotProcChance.shadowChance + skillAttr.dotSkillProcChance.shadowChance ? potentialDotDamage : 0f;
            // skillsDamageCalPar.fireDotDamage = Random.value <= atkAttr.dotProcChance.fireChance + skillAttr.dotSkillProcChance.fireChance ? potentialDotDamage : 0f;
            // skillsDamageCalPar.bleedDotDamage = Random.value <= atkAttr.dotProcChance.bleedChance + skillAttr.dotSkillProcChance.bleedChance ? potentialDotDamage: 0f;

            //这里增加一个用于技能分离触发的测试版本
            skillsBurstDamageCalPar.frostDotDamage = Random.value <= skillAttr.dotSkillProcChance.frostChance ? potentialDotDamage : 0f;
            skillsBurstDamageCalPar.lightningDotDamage = Random.value <= skillAttr.dotSkillProcChance.lightningChance ? potentialDotDamage : 0f;
            skillsBurstDamageCalPar.poisonDotDamage = Random.value <= skillAttr.dotSkillProcChance.poisonChance? potentialDotDamage : 0f;
            skillsBurstDamageCalPar.shadowDotDamage = Random.value <= skillAttr.dotSkillProcChance.shadowChance ? potentialDotDamage : 0f;
            skillsBurstDamageCalPar.fireDotDamage = Random.value <= skillAttr.dotSkillProcChance.fireChance ? potentialDotDamage : 0f;
            skillsBurstDamageCalPar.bleedDotDamage = Random.value <= skillAttr.dotSkillProcChance.bleedChance ? potentialDotDamage: 0f;
            
           
            //这里计算完了英雄自己属性的伤害触发， 后面的DOT和控制状态判断，可由怪物自身触发

            //增加 伤害变化参数,默认为1
            skillsBurstDamageCalPar.damageChangePar = 1;
            //默认并行数量最高100
            skillsBurstDamageCalPar.ParallelCount = 100;
            //传入 skillDamage 的伤害类型
            skillsBurstDamageCalPar.damageTriggerType = CalculateDamageTriggerType(critTriggered, vulTriggered, supTriggered, dotCritTriggered, elemCritTriggered);
        }


        /// <summary>
        ///持续性技能技能基础伤害计算，暂定技能只能有一个
        /// </summary>
        public void CalculateBaseOverTimeSkillDamage()
        {


            skillsOverTimeDamageCalPar.heroRef = heroEntity;
            // 1. 取出当前的攻击属性
            var atkAttr = attributeCmpt.attackAttribute;
            var skillAttr = attributeCmpt.skillDamageAttribute;

            // 2. 随机判定各触发状态
            bool critTriggered = Random.value <= atkAttr.physicalCritChance;
            bool vulTriggered = Random.value <= atkAttr.vulnerabilityChance;
            bool supTriggered = Random.value <= atkAttr.suppressionChance;
            bool dotCritTriggered = Random.value <= atkAttr.dotCritChance;//Dot暴击
            bool elemCritTriggered = Random.value <= atkAttr.elementalCritChance;  // 元素暴击判定

            skillsOverTimeDamageCalPar.critTriggered = critTriggered;
            skillsOverTimeDamageCalPar.vulTriggered = vulTriggered;
            skillsOverTimeDamageCalPar.supTriggered = supTriggered;
            skillsOverTimeDamageCalPar.elemCritTriggered = elemCritTriggered;
            skillsOverTimeDamageCalPar.dotCritTriggered = dotCritTriggered;

            // 3. 各倍率：伤害加成永远生效，其他触发则加成，否则为1
            float baseMul = 1f + atkAttr.damage;
            // 物理暴击
            float critMul = critTriggered ? 1f + atkAttr.critDamage : 1f;
            // 易伤
            float vulMul = vulTriggered ? 1f + atkAttr.vulnerabilityDamage : 1f;
            // 压制
            float supMul = supTriggered ? 1f + atkAttr.suppressionDamage : 1f;
            // dot暴击
            float dotCritMul = dotCritTriggered ? 1f + atkAttr.dotCritDamage : 1f;
            // 元素暴击
            float elemCritMul = elemCritTriggered ? 1f + atkAttr.elementalCritDamage : 1f;
            float extra = atkAttr.extraDamage;

            // 4. 计算瞬时物理伤害（含暴击）
            skillsOverTimeDamageCalPar.instantPhysicalDamage =
                skillAttr.baseDamage
              * baseMul
              * critMul
              * vulMul
              * supMul * skillAttr.physicalFactor
              + extra;

            // 5. 计算各元素瞬时伤害（不使用物理暴击，使用元素暴击判定）
            //    按公式：atk * (1 + damage + elementDamage) * vulMul * supMul * elemCritMul + extra
            //这里的技能元素伤害需要+1 ，因为技能是基于基础伤害直接造成元素或者物理伤害，可以直接加成，并且要乘以加成因子，技能后面会有受某项属性加成的影响
            float commonMul = baseMul * vulMul * supMul * elemCritMul;
            skillsOverTimeDamageCalPar.frostDamage =
               skillAttr.baseDamage
              * (1 + atkAttr.elementalDamage.frostDamage)
              * commonMul * skillAttr.frostFactor
              + extra;
            skillsOverTimeDamageCalPar.lightningDamage =
               skillAttr.baseDamage
              * (1 + atkAttr.elementalDamage.lightningDamage)
              * commonMul * skillAttr.lightningFactor
              + extra;
            skillsOverTimeDamageCalPar.poisonDamage =
               skillAttr.baseDamage
             * (1 + atkAttr.elementalDamage.poisonDamage)
             * commonMul * skillAttr.poisonFactor
             + extra;
            skillsOverTimeDamageCalPar.shadowDamage =
                skillAttr.baseDamage
              * (1 + atkAttr.elementalDamage.shadowDamage)
              * commonMul * skillAttr.shadowFactor
              + extra;
            skillsOverTimeDamageCalPar.fireDamage =
                skillAttr.baseDamage
              * (1 + atkAttr.elementalDamage.fireDamage)
              * commonMul * skillAttr.fireFactor
              + extra;

            // 6. 计算持续性DOT伤害（对所有类型相同公式）,不造成压制
            //    按公式：atk * (1 + damage) * dotCritMul + extra + dotDamage
            // 6. 计算持续性DOT伤害
            float dotBaseMul = baseMul * dotCritMul * vulMul;
            float dotExtra = extra + atkAttr.dotDamage;
            float potentialDotDamage = skillAttr.baseDamage * dotBaseMul + dotExtra;

            //这里增加英雄技能造成dot伤害的概率
            skillsOverTimeDamageCalPar.frostDotDamage = Random.value <= atkAttr.dotProcChance.frostChance + skillAttr.dotSkillProcChance.frostChance ? potentialDotDamage : 0f;
            skillsOverTimeDamageCalPar.lightningDotDamage = Random.value <= atkAttr.dotProcChance.lightningChance + skillAttr.dotSkillProcChance.lightningChance ? potentialDotDamage : 0f;
            skillsOverTimeDamageCalPar.poisonDotDamage = Random.value <= atkAttr.dotProcChance.poisonChance + skillAttr.dotSkillProcChance.poisonChance ? potentialDotDamage : 0f;
            skillsOverTimeDamageCalPar.shadowDotDamage = Random.value <= atkAttr.dotProcChance.shadowChance + skillAttr.dotSkillProcChance.shadowChance ? potentialDotDamage : 0f;
            skillsOverTimeDamageCalPar.fireDotDamage = Random.value <= atkAttr.dotProcChance.fireChance + skillAttr.dotSkillProcChance.fireChance ? potentialDotDamage : 0f;
            skillsOverTimeDamageCalPar.bleedDotDamage = Random.value <= atkAttr.dotProcChance.bleedChance + skillAttr.dotSkillProcChance.bleedChance ? potentialDotDamage : 0f;

            //这里计算完了英雄自己属性的伤害触发， 后面的DOT和控制状态判断，可由怪物自身触发

            //增加 伤害变化参数,默认为1
            skillsOverTimeDamageCalPar.damageChangePar = 1;
            //默认并行数量最高100
            skillsOverTimeDamageCalPar.ParallelCount = 100;
            //传入 skillDamage 的伤害类型
            skillsOverTimeDamageCalPar.damageTriggerType = CalculateDamageTriggerType(critTriggered, vulTriggered, supTriggered, dotCritTriggered, elemCritTriggered);
        }



        /// <summary>
        /// 技能攻击速度计算， 这里可以直接外部改变不同技能的攻击速度,与动画协同作用
        /// </summary>
        void CalculateSkillAttackSpeed()
        {

            animator.SetFloat("Skill1AttackSpeed", attributeCmpt.attackAttribute.attackSpeed + 1);
              
        }
        /// <summary>
        /// 附魔类技能的附魔时间计算
        /// </summary>
        void CalcuateSkillEnhancementTimer()
        {
            //暗能附魔时间小于0，则容量取消
            skillAttackPar.darkEnergyEnhantmentTimer -= Time.deltaTime;
            if (skillAttackPar.darkEnergyEnhantmentTimer <= 0)
                skillAttackPar.darkEnergyCapacity = 0;
            //寒冰附魔时间小于0，则容量取消
            skillAttackPar.frostEnchantmentTimer -= Time.deltaTime;
            if (skillAttackPar.frostEnchantmentTimer <= 0)
                skillAttackPar.frostCapacity = 0;
        
        }



        /// <summary>
        ///外部整体状态控制(摄像机）
        /// </summary>
        public void TotalListenController(float delta)
        {


            // 1. 取出当前的偏移向量
            Vector3 offset = cameraDistance;

            // 2. 计算当前距离
            float currentDistance = offset.magnitude;

            // 3. 根据滚轮增量算出新距离（向上滚放大：距离变小；向下滚缩小：距离变大）
            float newDistance = currentDistance - delta * 2;

            // 4. 限制到 [5,20]
            newDistance = Mathf.Clamp(newDistance, 8f, 25f);

            // 5. 按新的长度和原始方向重建偏移向量
            cameraDistance = offset.normalized * newDistance;


        }


        #region 携程区域
        /// <summary>
        /// 归内部状态机调用
        /// </summary>
        public void Inner_Roll()
        {
            if (!enableOperate) return;

            enableOperate = false;
            _fsm.ChangeState<Hero_Roll>();

            float distance = 10f;
            float duration = 0.4f;

            coroutineController.StopAllByTag("HeroRoll");
            // 启动新滚动协程
            var _rollCoroutineId = coroutineController.StartRoutine(
                  MoveForwardRoutine(distance, duration),
                  tag: "HeroRoll",
                  onComplete: () => {
                      _fsm.ChangeState<Hero_Idle>();
                      enableOperate = true;
                  }
              );


        }

        /// <summary>
        /// 英雄滚动效果
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        private IEnumerator MoveForwardRoutine(float distance, float duration)
        {
            Vector3 startPos = transform.position;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 endPos = startPos + forward * distance;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = endPos;
        }
        #endregion
    }


    [BurstCompile]
    public partial struct HeroAttackParallelJob : IJobParallelFor
    {
        // 输入数据
        public float3 HeroPosition;
        public float3 CenterDir;             // 由外部计算的中心方向
        public int PropNumber;
        public float SectorAngle;
        public ScenePrefabsSingleton Prefabs;
        public float PropSpeed;
        public FlightPropDamageCalPar DamageCalData;
        public EntityCommandBuffer.ParallelWriter Ecb;

        public void Execute(int index)
        {
            // 扇形散射：交替左右扩展，使用无分支计算
            int step = (index + 1) / 2;  // 0->0, 1->1/2->0, 2->3/2->1, 3->4/2->2...
            float dirSign = ((index & 1) == 1) ? -1f : 1f; // 奇数左(-), 偶数右(+)
            float offsetIndex = step * dirSign;

            float angleDeg = offsetIndex * SectorAngle;
            quaternion rot = quaternion.AxisAngle(math.up(), math.radians(angleDeg));
            float3 dir = math.normalize(math.mul(rot, CenterDir));

            // 实例化并设置 Transform
            Entity prop = Ecb.Instantiate(index, Prefabs.HeroFlightProp_DefaultProp);
            Ecb.SetComponent(index, prop, new LocalTransform
            {
                Position = HeroPosition,
                Rotation = quaternion.LookRotation(dir, math.up()),
                Scale = 0.3f
         
            });

            // 添加飞行组件
            Ecb.AddComponent(index, prop, new DirectFlightPropCmpt
            {
                speed = PropSpeed,
                dir = dir,
                originalSurvivalTime = 5f
            });

            // 添加伤害计算组件
            Ecb.AddComponent(index, prop, DamageCalData);

            //默认命中容量20个
            var hits=  Ecb.AddBuffer<HitRecord>(index, prop);
            hits.Capacity =10;
            Ecb.AddBuffer<HitElementResonanceRecord>(index, prop);
        }
    }

    /// <summary>
    /// 技能暗能的逻辑，这是附魔类技能，所以附加在基础攻击上执行
    /// </summary>
    [BurstCompile]
    public partial struct HeroSkillDarkEnergyAttackParallelJob : IJobParallelFor
    {
        // 输入数据
        public float3 HeroPosition;
        public float3 CenterDir;             // 由外部计算的中心方向
        public int PropNumber;
        public float SectorAngle;
        public ScenePrefabsSingleton Prefabs;
        public float PropSpeed;
        public SkillsDamageCalPar DamageCalData;
        public bool EnableSpecialEffect;
        public EntityCommandBuffer.ParallelWriter Ecb;

        public void Execute(int index)
        {
            // 扇形散射：交替左右扩展，使用无分支计算
            int step = (index + 1) / 2;  // 0->0, 1->1/2->0, 2->3/2->1, 3->4/2->2...
            float dirSign = ((index & 1) == 1) ? -1f : 1f; // 奇数左(-), 偶数右(+)
            float offsetIndex = step * dirSign;

            float angleDeg = offsetIndex * SectorAngle;
            quaternion rot = quaternion.AxisAngle(math.up(), math.radians(angleDeg));
            float3 dir = math.normalize(math.mul(rot, CenterDir));

            //这里释放暗能的逻辑
            Entity prop = Ecb.Instantiate(index, Prefabs.HeroSkill_DarkEnergy);
            Ecb.SetComponent(index, prop, new LocalTransform
            {
                Position = HeroPosition,
                Rotation = quaternion.LookRotation(dir, math.up()),
                Scale = 2f
              
            });

            // 添加暗能技能标识,这里技能道具的释放速度可以直接写死，后期可以根据道具设计，增加一个飞行速度值
            Ecb.AddComponent(index, prop, new SkillDarkEnergyTag 
            {tagSurvivalTime=5,
             speed =20,
             enableSpecialEffect=EnableSpecialEffect,

            });

            // 添加伤害计算组件
            Ecb.AddComponent(index, prop, DamageCalData);

            //默认命中容量50个，为道具添加命中参数
            var hits = Ecb.AddBuffer<HitRecord>(index, prop);
            hits.Capacity = 50;

            Ecb.AddBuffer<HitElementResonanceRecord>(index, prop);
        }
    }

}

