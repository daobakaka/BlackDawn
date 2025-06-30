using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using UnityEngine;
//用于管理技能的生命周期及状态
namespace BlackDawn.DOTS
{
    /// <summary>
    /// 技能管理类,在特殊技能类之后进行更新
    /// </summary>
    //先伤害计算，再更新状态
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(MainThreadSystemGroup))]
    public partial struct HeroSkillsMonoSystem : ISystem,ISystemStartStop
    {
        ComponentLookup<LocalTransform> _transform;
        ComponentLookup<MonsterLossPoolAttribute> _monsterLossPoolAttrLookup;
        ComponentLookup<HeroAttributeCmpt> _heroAttribute;
        //获取技能道具上的buffer，用于实现暗影增吞噬效果
        BufferLookup<HitRecord> _hitBuffer;
        float3 _heroPosition;
        Entity _heroEntity;
        EntityManager _entityManager;
        HeroAttributeCmpt _heroAttributeCmptOriginal;

      public  void OnCreate(ref SystemState state) 
        {

           // state.Enabled = false;
            //由外部控制
           state.RequireForUpdate<EnableHeroSkillsMonoSystemTag>();
           state.Enabled = false;

          _transform= state.GetComponentLookup<LocalTransform>(true);
          _monsterLossPoolAttrLookup = state.GetComponentLookup<MonsterLossPoolAttribute>(false);
            _heroAttribute = state.GetComponentLookup<HeroAttributeCmpt>(true);
            _entityManager = state.EntityManager;
         // _hitBuffer = state.GetBufferLookup<HitRecord>(true);   
        
        }

        public void OnStartRunning(ref SystemState state)
        {
            _heroEntity = SystemAPI.GetSingletonEntity<HeroEntityMasterTag>();
            _heroAttributeCmptOriginal = Hero.instance.attributeCmpt;

            DevDebug.Log("重启SkillMono系统");
        }



        [BurstCompile]
      public  void OnUpdate(ref SystemState state) 
        
        {
            //更新位置
            _transform.Update(ref state);
            _monsterLossPoolAttrLookup.Update(ref state);
            _heroAttribute.Update(ref state);
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
            var prefab =SystemAPI.GetSingleton<ScenePrefabsSingleton>();


            //脉冲技能处理
            foreach (var (skillTag ,skillCal,transform,collider,entity)
                  in SystemAPI.Query<RefRW<SkillPulseTag> ,RefRW<SkillsDamageCalPar>,RefRW<LocalTransform>,RefRW<PhysicsCollider>>().WithEntityAccess())
            {
                //更新标签的技能伤害参数，这里有动态的变化再更新
              //  skillCal.ValueRW.damageChangePar = skillTag.ValueRW.skillDamageChangeParTag;
                // 2) 计算“前向”世界向量
                float3 forward = math.mul(transform.ValueRO.Rotation, new float3(0f, 0f, 1f));
                // 3) 沿着前向移动
                transform.ValueRW.Position += forward *skillTag.ValueRW.speed * timer;

                skillTag.ValueRW.tagSurvivalTime -= timer; 

                //满足时间大于3秒，oncheck关闭，且允许开启第二阶段 ，则添加第二阶段爆炸需求标签,取消销毁，留在爆炸渲染逻辑销毁
                if (skillTag.ValueRW.tagSurvivalTime <=0)
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
            foreach (var (skillTag,skillCal, transform, collider,entity)
                 in SystemAPI.Query<RefRW<SkillDarkEnergyTag>,RefRW<SkillsDamageCalPar>, RefRW<LocalTransform>, RefRW<PhysicsCollider>>().WithEntityAccess())
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
                                var monsterAttr =  _monsterLossPoolAttrLookup[buffer[i].other];
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

                 foreach (var (skillTag,skillCal, transform, collider,entity)
                 in SystemAPI.Query<RefRW<SkillIceFireTag>,RefRW<SkillsDamageCalPar>, RefRW<LocalTransform>, RefRW<PhysicsCollider>>().WithEntityAccess())
            {
                //更新标签的技能伤害参数这里有动态的变化再更新
               // skillCal.ValueRW.damageChangePar =skillTag.ValueRW.skillDamageChangeParTag;              
                float radius = skillTag.ValueRO.radius;
               ref float angle = ref skillTag.ValueRW.currentAngle;

                //如果开启第二阶段标识，且开启特殊效果,4秒执行一次爆炸判断
                if (skillTag.ValueRO.enableSecond&&skillTag.ValueRO.secondSurvivalTime<0)
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
                            ecb.SetComponentEnabled<SkillIceFireSecondExplosionRequestTag>(entity,true);        
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
                if(skillTag.ValueRO.closed)
                    skillCal.ValueRW.destory = true;
               // ecb.DestroyEntity(entity);

            }
         
            //寒冰的Mono效果
            SkillMonoFrost(ref state,ecb);
            //元素共鸣Mono效果
            SkillMonoElementResonance(ref state,ecb);
            //技能静电牢笼
            SkillMonoElectroCage(ref state,ecb,prefab);
            //暗影洪流B阶段，瞬时伤害特效控制
            SkillMineBlastMono( ref state);






        }


         public   void OnDestroy(ref SystemState state) { }

        public void OnStopRunning(ref SystemState state)
        {
          
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
                if (skillTag.ValueRO.tagSurvivalTime==10)
                {
                    skillTag.ValueRW.originalPosition = transform.ValueRO.Position;
                }
                // 2. 增加存活时间
                skillTag.ValueRW.tagSurvivalTime -= SystemAPI.Time.DeltaTime;
                // 3. 读取 origin、t
                float3 origin = skillTag.ValueRO.originalPosition;
                float t = 10-skillTag.ValueRO.tagSurvivalTime;

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
                if(skillCal.ValueRW.hit ==true&& skillTag.ValueRW.hitCount>0)
                    {                        
                        skillCal.ValueRW.hit = false;
                        var prefab = SystemAPI.GetSingleton<ScenePrefabsSingleton>();

                        //激发一次寒冰碎片的计算

                        skillTag.ValueRW.hitCount--;

                        for (int i = 0; i < skillTag.ValueRO.shrapnelCount; i++)
                        {

                           var fragIce= ecb.Instantiate(prefab.HeroSkillAssistive_Frost);

                            var trs = transform.ValueRW;
                            trs.Scale = 1;
                            float angleDeg = 360f / skillTag.ValueRO.shrapnelCount * i;
                            float angleRad = math.radians(angleDeg);

                            // 3. 生成绕 Y 轴的四元数，并赋给 trs.Rotation
                            trs.Rotation = quaternion.EulerXYZ(0f, angleRad, 0f);
                            ecb.AddComponent(fragIce, trs);                           
                            //添加寒冰碎片的标签,赋予伤害标签的伤害值
                            ecb.AddComponent(fragIce , new SkillFrostShrapneTag() { speed =20,tagSurvivalTime =1});
                            var newCal = skillCal.ValueRW;
                            // 2. 修改字段，寒冰碎片继承20%冻结值
                            newCal.damageChangePar = skillTag.ValueRO.skillDamageChangeParTag;
                            if(skillTag.ValueRO.enableSpecialEffect)
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

            foreach (var (skillTag,skillCal,transform, collider, entity)
            in SystemAPI.Query<RefRW<SkillElementResonanceTag>, RefRW<SkillsDamageCalPar>,RefRW<LocalTransform>, RefRW<PhysicsCollider>>().WithEntityAccess())
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
        void SkillMonoElectroCage(ref SystemState state, EntityCommandBuffer ecb,ScenePrefabsSingleton prefabs)
        {
            var rng = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, int.MaxValue));

            foreach (var (skillTag,damagePar ,transform, collider, entity)
            in SystemAPI.Query<RefRW<SkillElectroCageTag>,RefRW<SkillsDamageCalPar>, RefRW<LocalTransform>, RefRW<PhysicsCollider>>().WithEntityAccess())
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
                        var arcEntity = ecb.Instantiate(prefabs.HeroSkillAssistive_ElectroCage_Lightning);

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
                        if (random <=( 0.5-skillTag.ValueRO.StackCount*0.05f))
                        {
                            float3 Offset = rng.NextFloat3(-15f, 15f);
                            //增加一次传导次数
                            skillTag.ValueRW.StackCount += 1;
                            float3 newPosition = transform.ValueRO.Position + new float3(Offset.x, 0, Offset.z);

                          var entityElectroCage =  DamageSkillsECSRelaseProp(ecb, prefabs.HeroSkill_ElectroCage, damagePar.ValueRO, newPosition, quaternion.identity);
                            int nextStackCount = skillTag.ValueRO.StackCount + 1;
                            if (skillTag.ValueRO.enableSecondA)
                            {
                                ecb.AddComponent(entityElectroCage, new SkillElectroCageTag() { tagSurvivalTime = 4, enableSecondA = true, enableSecondB = true, skillDamageChangeParTag = 2, intervalTimer = 0.2f ,StackCount=nextStackCount});
                            }
                            else
                            {
                                ecb.AddComponent(entityElectroCage, new SkillElectroCageTag() { tagSurvivalTime = 4, enableSecondB = true,StackCount=nextStackCount});

                            }
                        
                        }


                    }

                }



            }




            //雷暴消除逻辑
            foreach (var (skillTag,skillCal,entity)
          in SystemAPI.Query<RefRW<SkillElectroCageScoendTag>,RefRW<SkillsDamageCalPar>>().WithEntityAccess())
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

        void SkillMineBlastMono(ref SystemState state)
        {
            foreach (var (skillTag, skillCal, entity)
             in SystemAPI.Query<RefRW<SkillShadowTideBTag>, RefRW<SkillsDamageCalPar>>().WithEntityAccess())
            { 
               skillTag.ValueRW.tagSurvivalTime -=SystemAPI.Time.DeltaTime;

                if (skillTag.ValueRW.tagSurvivalTime <= 0)
                    skillCal.ValueRW.destory = true;
            
            }



        }

        /// <summary>
        /// 暗影洪流第二阶段
        /// </summary>
        /// <param name="state"></param>
        /// <param name="ecb"></param>
        void SkillShadowTideBMono(ref SystemState state, EntityCommandBuffer ecb)
        {



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