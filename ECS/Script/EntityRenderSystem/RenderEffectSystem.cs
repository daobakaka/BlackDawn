using BlackDawn;
using BlackDawn.DOTS;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Profiling;
using Random = Unity.Mathematics.Random;
using static Pathfinding.TargetMover;
using Unity.Jobs;
//用于shader 渲染 材质变更  伤害飘字等逻辑,在英雄技能回调系统之后运行
namespace BlackDawn.DOTS
{/// <summary>
/// 
/// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(RenderSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial struct RenderEffectSystem : ISystem, ISystemStartStop
    {
        private ComponentLookup<MonsterLossPoolAttribute> _monsterLossPoolattrLookup;
        private ComponentLookup<MonsterDefenseAttribute> _monsterDefenseAttrLookup;
        private ComponentLookup<MonsterTempDamageText> _monsterTempDamageTextLookup;
        private ComponentLookup<LocalTransform> _transform;
        private BufferLookup<LinkedEntityGroup> _linkedEntityGroupLookup;
        private ComponentLookup<FireRandomOffset> _fireRandomOffsetLookup;
        private ComponentLookup<MonsterControlledEffectAttribute> _monsterControlledEffectAttributeLookup;
        private EntityManager _entityManager;
        private NativeArray<char> _UVchar;
        private NativeArray<float4> _UVTable;
        // 持有 EndSimulationEntityCommandBufferSystem 的引用
        //  private EndSimulationEntityCommandBufferSystem _ecbSystem;

        public void OnCreate(ref SystemState state)
        {
            //由外部开启，目前暂时由英雄类开启,这里应该等场景加载的过程进行确认，或者回调型确认
            state.RequireForUpdate<EnableRenderEffectSystemTag>();
            state.Enabled = false;
            //  prefabs=SystemAPI.GetSingleton<PrefabsComponentData>();
            _monsterLossPoolattrLookup = SystemAPI.GetComponentLookup<MonsterLossPoolAttribute>(true);
            _monsterDefenseAttrLookup = SystemAPI.GetComponentLookup<MonsterDefenseAttribute>(true);
            _monsterTempDamageTextLookup = SystemAPI.GetComponentLookup<MonsterTempDamageText>(true);
            _transform = SystemAPI.GetComponentLookup<LocalTransform>(true);
            _linkedEntityGroupLookup = SystemAPI.GetBufferLookup<LinkedEntityGroup>(true);
            _fireRandomOffsetLookup = SystemAPI.GetComponentLookup<FireRandomOffset>(true);
            _monsterControlledEffectAttributeLookup = SystemAPI.GetComponentLookup<MonsterControlledEffectAttribute>(true);

            _entityManager = state.EntityManager;

            //_ecbSystem = state.World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();

        }
        /// <summary>
        /// 在外部控制初始化
        /// </summary>
        /// <param name="state"></param>
        public void OnStartRunning(ref SystemState state)
        {

            DevDebug.LogError("开启渲染系统");
            //数组初始化问题
            int length = DamageTextUVLookup.CharTable.Length;
            //分配原生字符串
            _UVchar = new NativeArray<char>(length, Allocator.Persistent);
            //分配原生数组
            _UVTable = new NativeArray<float4>(length, Allocator.Persistent);


            for (int i = 0; i < _UVTable.Length; i++)
            {
                _UVchar[i] = DamageTextUVLookup.CharTable[i];
                _UVTable[i] = (float4)DamageTextUVLookup.UVTable[i];
            }


        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

            _monsterLossPoolattrLookup.Update(ref state);
            _monsterDefenseAttrLookup.Update(ref state);
            _monsterTempDamageTextLookup.Update(ref state);
            _transform.Update(ref state);
            _linkedEntityGroupLookup.Update(ref state);
            _fireRandomOffsetLookup.Update(ref state);
            _monsterControlledEffectAttributeLookup.Update(ref state);
            float dt = SystemAPI.Time.DeltaTime;
            // var ecb = new EntityCommandBuffer(Allocator.TempJob);
            //渲染动态ECB,等同于end，写回必须在系统运行顺序之后进行才可以
            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var ecb1 = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var ecb2 = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var currentTime = SystemAPI.Time.ElapsedTime;
            //等待action主线程跑完
            state.Dependency.Complete();


            //var _ecb = _ecbSystem.CreateCommandBuffer();    
            // —— 3) 构造一个 Query，它一定包含 Parent、RenderParameterAspect，
            //      也要包含你会通过 ComponentLookup 访问的那两个组件类型

            //伤害shader特效的渲染,这种方式顺滑依赖回收
            //注意 像这种有额外查询写入的job,必须要有单独的声明hanndel处理依赖关系 进行 complete 才可以， 但有可能造成卡顿
            state.Dependency = new RenderEffectsJob
            {
                LossPoolLookup = _monsterLossPoolattrLookup,
                DefenseLookup = _monsterDefenseAttrLookup,
                LtLookup = _transform,
                Ecb = ecb.AsParallelWriter(),
                DeltaTime = SystemAPI.Time.DeltaTime,
                LinderGroupLookup = _linkedEntityGroupLookup,
                MonsterControlledEffectAttrLookup = _monsterControlledEffectAttributeLookup,
            }.ScheduleParallel(state.Dependency);

          // state.Dependency.Complete();
            //   renderEffectsJob.Complete();


            //伤害漂字的JobECS 渲染
            state.Dependency = new RenderTextJob
            {
                UVTable = _UVTable,
                UVchar = _UVchar,
                CurrentTime = SystemAPI.Time.ElapsedTime,
                Ecb = ecb1.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
            // state.Dependency.Complete();

            //伤害漂字的 Dot JobECS 渲染
            state.Dependency = new RenderDotTextJob
            {
                UVTable = _UVTable,
                UVchar = _UVchar,
                CurrentTime = SystemAPI.Time.ElapsedTime,
                Ecb = ecb2.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
            //  state.Dependency.Complete();





            //ecb.Playback(_entityManager);
            //ecb.Dispose();
        }

        public void OnDestroy(ref SystemState state)
        {
            //释放 UV表 UV字符索引
            _UVTable.Dispose();
            _UVchar.Dispose();


        }

        #region 普通方法接口查询区域
        /// <summary>
        /// 这里使用结构体数据
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private FixedString64Bytes FormatDamageValue(float value)
        {
            FixedString64Bytes result = default;

            if (value >= 1_000_000_000_000f)
            {
                float v = value * 0.000000000001f;
                int intPart = (int)v;
                result.Append(intPart);
                result.Append('.');
                result.Append((int)((v - intPart) * 10));
                result.Append('T');
            }
            else if (value >= 1_000_000_000f)
            {
                float v = value * 0.000000001f;
                int intPart = (int)v;
                result.Append(intPart);
                result.Append('.');
                result.Append((int)((v - intPart) * 10));
                result.Append('G');
            }
            else if (value >= 1_000_000f)
            {
                float v = value * 0.000001f;
                int intPart = (int)v;
                result.Append(intPart);
                result.Append('.');
                result.Append((int)((v - intPart) * 10));
                result.Append('M');
            }
            else if (value >= 1_000f)
            {
                float v = value * 0.001f;
                int intPart = (int)v;
                result.Append(intPart);
                result.Append('.');
                result.Append((int)((v - intPart) * 10));
                result.Append('K');
            }
            // 其他同理
            else
            {
                int intValue = (int)value;
                result.Append(intValue);
            }

            return result;
        }

        // 高性能接口：查 index
        private int GetCharIndex(char c)
        {
            for (int i = 0; i < _UVchar.Length; i++)
            {
                if (_UVchar[i] == c)
                    return i;
            }
            return 0; // fallback '_'
        }

        // 高性能接口：直接查 UV
        private float4 GetUVByIndex(int index)
        {
            if (index >= 0 && index < _UVTable.Length)
                return _UVTable[index];
            return _UVTable[0];
        }

        void ApplyAnimParams(
    ref DamageTextMaterialAspect mat,
    float4 textColor,
    float scale,
    float2 offset,
    float startTime
)
        {
            mat.TextColor.ValueRW.Value = textColor;
            mat.Scale.ValueRW.Value = scale;
            mat.Offset.ValueRW.Value = offset;
            mat.StartTime.ValueRW.Value = startTime;

        }


        #endregion
        public void OnStopRunning(ref SystemState state)
        {

        }


    }


    /// <summary>
    /// 特效渲染的JOB
    /// </summary>
    [BurstCompile]
  
    public partial struct RenderEffectsJob : IJobEntity
    {
        // ComponentLookups for accessing components by entity
        //外部的查询必须只读，或者提供并行不安全的可写标记
        
      [ReadOnly] public ComponentLookup<MonsterLossPoolAttribute> LossPoolLookup;
      [ReadOnly] public ComponentLookup<MonsterDefenseAttribute> DefenseLookup;
      [ReadOnly] public ComponentLookup<LocalTransform> LtLookup;
      [ReadOnly] public BufferLookup<LinkedEntityGroup> LinderGroupLookup;
      [ReadOnly] public ComponentLookup<MonsterControlledEffectAttribute> MonsterControlledEffectAttrLookup;

        // Parallel ECB for structural changes
        public EntityCommandBuffer.ParallelWriter Ecb;
        //查看调度专业的标识
        static readonly ProfilerMarker m_Execute =
    new ProfilerMarker("RenderEffectsJob.Execute");

        // DeltaTime for timers
        public float DeltaTime;

        void Execute(
            Entity entity,
            [EntityIndexInQuery] int sortKey,
            in Parent parentRO,        
            RenderParameterAspect mat)
        {
            using (m_Execute.Auto())
            {
                // DevDebug.Log("进入渲染JOB"); 
                var monster = parentRO.Value;
                var linkedGroup = LinderGroupLookup[monster];
                //获取控制组件
                var control = MonsterControlledEffectAttrLookup[monster];
                // 先拷贝出来，再修改，最后 write-back
                var pools = LossPoolLookup[monster];
                var defense = DefenseLookup[monster];
                //生成全局的RNGstate
                var rng = new Random(mat.RngState.ValueRW.rngState);
                //这里应该只在初始化的时候写入
               // mat.RandomOffset.ValueRW.Value = new float4(rng.NextFloat(-1, 1), rng.NextFloat(-1, 1), 0, 0);
    

                // 1) 受击高亮
                pools.attackTimer = math.max(0f, pools.attackTimer - DeltaTime);
                mat.UnderAttack.ValueRW.Value =
                    pools.attackTimer < 0.01f
                        ? float4.zero
                        : new float4(1f, 1f, 1f, 1f);

                //同一时间dot激活时间减少，池化标签的功能目前是激活DOT
                pools.fireActive = math.max(0f, pools.fireActive - DeltaTime);
                pools.frostActive = math.max(0f, pools.frostActive - DeltaTime);
                pools.poisonActive = math.max(0f, pools.poisonActive - DeltaTime);
                pools.lightningActive = math.max(0f, pools.lightningActive - DeltaTime);
                pools.shadowActive = math.max(0f, pools.shadowActive - DeltaTime);
                pools.bleedActive = math.max(0f, pools.bleedActive - DeltaTime);

                // 1) 冰霜


                //冰霜直接走emmision通道，这里用一个DEbuffer冻伤，冻结是控制效果，冻伤给与0.1浮点数的效果表现
                // 判断是否冻伤激活（产生 1 或 0 的掩码）
                // 1) 掩码计算（冻伤必须 active>0 且 pool > 0 才显示效果）
                float maskFrost = math.select(0f, 1f, pools.frostActive > 0f && pools.frostPool > 0f);
                float invMaskFrost = 1f - maskFrost;

                // 2) 正常 frostPool 控制值（范围 0~1）,池化效果在大于100时显示
                float frostPoolVal = math.saturate((pools.frostPool-100) / 100f);

                // 3) 冻伤目标值
                float currentFrost = mat.Frost.ValueRW.Value;
                float frostFrozenVal = math.lerp(currentFrost, 1.1f, DeltaTime * 2f); // 冻伤特效


                // 4) 合并结果（冻伤优先），增加冻结冻伤 1.1  冻结参数1.3
                float freezeMask = math.select(0f, 1f, control.freezeActive);

                //    首先按 frost 逻辑合并
                float frostFinalBase = frostFrozenVal * maskFrost + frostPoolVal * invMaskFrost;

                //    再用 select 在一条指令里覆盖冻结效果（值为 1.3f）
                float frostFinal = math.select(frostFinalBase, 1.3f, control.freezeActive);
                mat.Frost.ValueRW.Value = frostFinal;



                // 5) 计时器更新
                pools.frostTimer = math.max(0f, pools.frostTimer - DeltaTime);

                // 6) frostPool 衰减逻辑（保持原样）
                pools.frostPool = math.max(
                    0f,
                    pools.frostPool - 100f * DeltaTime * math.step(pools.frostTimer+pools.frostActive, 0f)
                );




                // 2) 火焰
                pools.fireTimer = math.max(0f, pools.fireTimer - DeltaTime);
                var firet = math.saturate((pools.firePool-100) / 100f);
                mat.Fire.ValueRW.Value = firet;
                pools.firePool = math.max(
                    0f,
                    pools.firePool - 100f * DeltaTime * math.step(pools.fireTimer, 0f)
                );
                // 当 pools.fireActive >= 1f 时 mask = 1， 否则 mask = 0   // 计算增量：mask==1 → +DeltaTime， mask==0 → –DeltaTime  
                // 更新 scale 并钳制到 [0,1]   // 无论哪条路径都写回 Component
                //SIMD 优化式掩码写法
                var childFire = linkedGroup[4].Value;
                var ltFire = LtLookup[childFire];
                float maskFire = math.step(1f, pools.fireActive);
                float deltaFire = DeltaTime * (maskFire * 1f + (1f - maskFire) * -1f);
                ltFire.Scale = math.saturate(ltFire.Scale + deltaFire);
                Ecb.SetComponent(sortKey, childFire, ltFire);


                // 3) 暗影
                pools.shadowTimer = math.max(0f, pools.shadowTimer - DeltaTime);
               var  shadowt = math.saturate((pools.shadowPool-100) / 100f);
                mat.DarkShadow.ValueRW.Value = shadowt;
                pools.shadowPool = math.max(
                    0f,
                    pools.shadowPool - 100f * DeltaTime * math.step(pools.shadowTimer, 0f)
                );

                var childShadow = linkedGroup[5].Value;
                var ltShadow = LtLookup[childShadow];
                float maskShadow = math.step(1f, pools.shadowActive);
                float deltaShadow = DeltaTime * (maskShadow * 1f + (1f - maskShadow) * -1f);
                ltShadow.Scale = math.saturate(ltShadow.Scale + deltaShadow);
                Ecb.SetComponent(sortKey, childShadow, ltShadow);



                // 4) 闪电
                pools.lightningTimer = math.max(0f, pools.lightningTimer - DeltaTime);
               var lightningt = math.saturate((pools.lightningPool-100) / 100f);
                mat.Lighting.ValueRW.Value = lightningt;
                pools.lightningPool = math.max(
                    0f,
                    pools.lightningPool - 100f * DeltaTime * math.step(pools.lightningTimer, 0f)
                );
                var childLightning = linkedGroup[6].Value;
                var ltLightning = LtLookup[childLightning];
                float maskLightning = math.step(1f, pools.lightningActive);
                float deltaLightning = DeltaTime * (maskLightning * 1f + (1f - maskLightning) * -1f);
                ltLightning.Scale = math.saturate(ltLightning.Scale + deltaLightning);
                Ecb.SetComponent(sortKey, childLightning, ltLightning);
              
                
                // 5) 毒素
                pools.poisonTimer = math.max(0f, pools.poisonTimer - DeltaTime);
                var poisont = math.saturate((pools.poisonPool-100) / 100f);
                mat.Poisoning.ValueRW.Value = poisont;
                pools.poisonPool = math.max(
                    0f,
                    pools.poisonPool - 100f * DeltaTime * math.step(pools.poisonTimer, 0f)
                );

                var childPoison = linkedGroup[7].Value;
                var ltPoison = LtLookup[childPoison];
                float maskPoison = math.step(1f, pools.poisonActive);
                float deltaPoison = DeltaTime * (maskPoison * 1f + (1f - maskPoison) * -1f);
                ltPoison.Scale = math.saturate(ltPoison.Scale + deltaPoison);
                Ecb.SetComponent(sortKey, childPoison, ltPoison);


                // 6) 流血
                if (false)
                {
                    pools.bleedTimer = math.max(0f, pools.bleedTimer - DeltaTime);
                    var bleedt = math.saturate(pools.bleedPool / 100f);
                    mat.Alpha.ValueRW.Value = bleedt;
                    pools.bleedPool = math.max(
                        0f,
                        pools.bleedPool - 100f * DeltaTime * math.step(pools.bleedTimer, 0f)
                    );
                }

                // 8) 默认死亡溶解,这里采用三目化，旨在编译时的无跳转
                // 计算当 survivalTime 在 [0,2] 时的目标值
                float target = 1f - defense.survivalTime / 2f;
                // 用三元/Branchless 选择：如果 survivalTime ≤ 2，则采用 target，否则保留旧值
                // Burst 通常会把下面这行编译成无分支的 csel/select
                mat.Dissolve.ValueRW.Value =
                    defense.survivalTime <= 2f
                        ? target
                        : mat.Dissolve.ValueRW.Value;


                // 9) 写回更新后的属性
                Ecb.SetComponent<MonsterDefenseAttribute>(sortKey, monster, defense);
                Ecb.SetComponent(sortKey, monster, pools);

                //生成之后写回，最后写回
                mat.RngState.ValueRW.rngState = rng.state;
                //这种方式更加轻量化
                //LossPoolLookup[monster] = pools;
                //DefenseLookup[monster] = defense;
            }
        }
    }

    /// <summary>
    /// 文字渲染的Job,非常强悍 几乎没有花销
    /// </summary>
    [BurstCompile]
    partial struct RenderTextJob : IJobEntity
    {

        // 只读 UV 表和字符表
        [ReadOnly] public NativeArray<float4> UVTable;
        [ReadOnly] public NativeArray<char> UVchar;

        // 当前时间
        public double CurrentTime;

        // 并行 ECB
        public EntityCommandBuffer.ParallelWriter Ecb;


        // Execute 方法签名：Entity + 组件引用 + Aspect
        void Execute(Entity entity,
                     RefRW<MonsterTempDamageText> textM,
                   DamageTextMaterialAspect mat, [EntityIndexInQuery] int sortKey)
        {
          
            {
                //DevDebug.Log("进入渲染TEXT");
                // 1. 取出伤害值
              
                ref var text = ref textM.ValueRW;
                float damageValue = text.hurtVlue;

                // 2. 格式化为 FixedString64Bytes
                FixedString64Bytes fs = default;

                if (text.damageTriggerType == DamageTriggerType.Miss) // 假设MIss类型
                {                 
                    fs.Append('M');
                    fs.Append('I');
                    fs.Append('S');
                    fs.Append('S');
                }
                 else if (damageValue >= 1_000_000_000_000f)
                {
                    float v = damageValue * 1e-12f; int ip = (int)v;
                    fs.Append(ip); fs.Append('.'); fs.Append((int)((v - ip) * 10)); fs.Append('T');
                }
                else if (damageValue >= 1_000_000_000f)
                {
                    float v = damageValue * 1e-9f; int ip = (int)v;
                    fs.Append(ip); fs.Append('.'); fs.Append((int)((v - ip) * 10)); fs.Append('G');
                }
                else if (damageValue >= 1_000_000f)
                {
                    float v = damageValue * 1e-6f; int ip = (int)v;
                    fs.Append(ip); fs.Append('.'); fs.Append((int)((v - ip) * 10)); fs.Append('M');
                }
                else if (damageValue >= 1_000f)
                {
                    float v = damageValue * 1e-3f; int ip = (int)v;
                    fs.Append(ip); fs.Append('.'); fs.Append((int)((v - ip) * 10)); fs.Append('K');
                }
                else
                {
                    fs.Append((int)damageValue);
                }

                // 3. 写入 6 个字符对应的 UV
                int len = fs.Length;
                int start = math.max(0, 6 - len);
                for (int i = 0; i < 6; i++)
                {
                    char c = i < start ? '_' : (char)fs[i - start];
                    int idx = 0;
                    for (int k = 0; k < UVchar.Length; k++)
                        if ((char)UVchar[k] == c) { idx = k; break; }
                    float4 uv = UVTable[idx];

                    switch (i)
                    {
                        case 0: mat.Char1.ValueRW.Value = uv; break;
                        case 1: mat.Char2.ValueRW.Value = uv; break;
                        case 2: mat.Char3.ValueRW.Value = uv; break;
                        case 3: mat.Char4.ValueRW.Value = uv; break;
                        case 4: mat.Char5.ValueRW.Value = uv; break;
                        case 5: mat.Char6.ValueRW.Value = uv; break;
                    }
                }

                // 4. 原样 switch 写入其余参数
                switch (text.damageTriggerType)
                {
                    case DamageTriggerType.NormalAttack:
                        mat.TextColor.ValueRW.Value = new float4(1f, 1f, 1f, 1f);
                        mat.Offset.ValueRW.Value = new float2(0f, 5f);
                        mat.StartTime.ValueRW.Value = (float)CurrentTime;
                        mat.Scale.ValueRW.Value = 0.1f;
                        break;

                    case DamageTriggerType.Vulnerable:
                        mat.TextColor.ValueRW.Value = new float4(0f, 1f, 1f, 1f);
                        mat.Offset.ValueRW.Value = new float2(0f, 5f);
                        mat.StartTime.ValueRW.Value = (float)CurrentTime;
                        mat.Scale.ValueRW.Value = 0.25f;
                        break;

                    case DamageTriggerType.CriticalStrike:
                        mat.TextColor.ValueRW.Value = new float4(1f, 1f, 1f, 1f);
                        mat.Offset.ValueRW.Value = new float2(0f, 2.5f);
                        mat.StartTime.ValueRW.Value = (float)CurrentTime;
                        mat.Scale.ValueRW.Value = 0.8f;
                        break;

                    case DamageTriggerType.VulnerableCritical:
                        mat.TextColor.ValueRW.Value = new float4(0f, 1f, 1f, 1f);
                        mat.Offset.ValueRW.Value = new float2(0.02f, 2.5f);
                        mat.StartTime.ValueRW.Value = (float)CurrentTime;
                        mat.Scale.ValueRW.Value = 1.0f;
                        break;

                    case DamageTriggerType.Suppression:
                        mat.TextColor.ValueRW.Value = new float4(1f, 0.85f, 0f, 1f);
                        mat.Offset.ValueRW.Value = new float2(0f, 6.5f);
                        mat.StartTime.ValueRW.Value = (float)CurrentTime;
                        mat.Scale.ValueRW.Value = 0.5f;
                        break;

                    case DamageTriggerType.SuppressionVulnerable:
                        mat.TextColor.ValueRW.Value = new float4(0f, 1f, 1f, 1f);
                        mat.Offset.ValueRW.Value = new float2(0f, 6f);
                        mat.StartTime.ValueRW.Value = (float)CurrentTime;
                        mat.Scale.ValueRW.Value = 0.5f;
                        break;

                    case DamageTriggerType.SuppressionCritical:
                        mat.TextColor.ValueRW.Value = new float4(1f, 0.85f, 0f, 1f);
                        mat.Offset.ValueRW.Value = new float2(0.02f, 3.5f);
                        mat.StartTime.ValueRW.Value = (float)CurrentTime;
                        mat.Scale.ValueRW.Value = 1f;
                        break;

                    case DamageTriggerType.SuppressionVulnCrit:
                        mat.TextColor.ValueRW.Value = new float4(1f, 0.5f, 0f, 1f);
                        mat.Offset.ValueRW.Value = new float2(0.1f, 2f);
                        mat.StartTime.ValueRW.Value = (float)CurrentTime;
                        mat.Scale.ValueRW.Value = 2f;
                        break;
                    case DamageTriggerType.Block:
                        mat.TextColor.ValueRW.Value = new float4(0f, 0f, 0f, 0f);
                        mat.Offset.ValueRW.Value = new float2(0f, 3f);
                        mat.StartTime.ValueRW.Value = (float)CurrentTime;
                        mat.Scale.ValueRW.Value = 0.0f;
                        break;
                    case DamageTriggerType.Miss:
                        mat.TextColor.ValueRW.Value = new float4(0f, 0f, 0f, 0f);
                        mat.Offset.ValueRW.Value = new float2(0.1f, 3f);
                        mat.StartTime.ValueRW.Value = (float)CurrentTime;
                        mat.Scale.ValueRW.Value = 0.0f;
                        break;
                }

                // 5. 禁用该组件，避免下帧再处理
                Ecb.SetComponentEnabled<MonsterTempDamageText>(sortKey, entity, false);
            }
        }
    }




    /// <summary>
    /// Dot伤害文字渲染的Job,非常强悍 几乎没有花销,这里就是专门针对DOT伤害的文字渲染
    /// </summary>
    [BurstCompile]
    partial struct RenderDotTextJob : IJobEntity
    {

        // 只读 UV 表和字符表
        [ReadOnly] public NativeArray<float4> UVTable;
        [ReadOnly] public NativeArray<char> UVchar;

        // 当前时间
        public double CurrentTime;

        // 并行 ECB
        public EntityCommandBuffer.ParallelWriter Ecb;


        // Execute 方法签名：Entity + 组件引用 + Aspect
        void Execute(Entity entity,
                     RefRW<MonsterTempDotDamageText> textM,
                   DamageTextMaterialAspect mat, [EntityIndexInQuery] int sortKey)
        {

            {
                //DevDebug.Log("进入渲染TEXT");
                // 1. 取出伤害值

                ref var text = ref textM.ValueRW;
                float damageValue = text.hurtVlue;

                // 2. 格式化为 FixedString64Bytes
                FixedString64Bytes fs = default;
                if (damageValue >= 1_000_000_000_000f)
                {
                    float v = damageValue * 1e-12f; int ip = (int)v;
                    fs.Append(ip); fs.Append('.'); fs.Append((int)((v - ip) * 10)); fs.Append('T');
                }
                else if (damageValue >= 1_000_000_000f)
                {
                    float v = damageValue * 1e-9f; int ip = (int)v;
                    fs.Append(ip); fs.Append('.'); fs.Append((int)((v - ip) * 10)); fs.Append('G');
                }
                else if (damageValue >= 1_000_000f)
                {
                    float v = damageValue * 1e-6f; int ip = (int)v;
                    fs.Append(ip); fs.Append('.'); fs.Append((int)((v - ip) * 10)); fs.Append('M');
                }
                else if (damageValue >= 1_000f)
                {
                    float v = damageValue * 1e-3f; int ip = (int)v;
                    fs.Append(ip); fs.Append('.'); fs.Append((int)((v - ip) * 10)); fs.Append('K');
                }
                else
                {
                    fs.Append((int)damageValue);
                }

                // 3. 写入 6 个字符对应的 UV
                int len = fs.Length;
                int start = math.max(0, 6 - len);
                for (int i = 0; i < 6; i++)
                {
                    char c = i < start ? '_' : (char)fs[i - start];
                    int idx = 0;
                    for (int k = 0; k < UVchar.Length; k++)
                        if ((char)UVchar[k] == c) { idx = k; break; }
                    float4 uv = UVTable[idx];

                    switch (i)
                    {
                        case 0: mat.Char1.ValueRW.Value = uv; break;
                        case 1: mat.Char2.ValueRW.Value = uv; break;
                        case 2: mat.Char3.ValueRW.Value = uv; break;
                        case 3: mat.Char4.ValueRW.Value = uv; break;
                        case 4: mat.Char5.ValueRW.Value = uv; break;
                        case 5: mat.Char6.ValueRW.Value = uv; break;
                    }
                }

                // 4. 原样 Dot 目前暂定一种情况 灰色向下漂移
                switch (text.damageTriggerType)
                {
                    case DamageTriggerType.DotDamage:
                        mat.TextColor.ValueRW.Value = new float4(1f, 1f, 1f, 1f)*0.7f;
                        mat.Offset.ValueRW.Value = new float2(0f, -7f);
                        mat.StartTime.ValueRW.Value = (float)CurrentTime;
                        mat.Scale.ValueRW.Value = -0.5f;
                        break;                   
                }
                // 5. 禁用该组件，避免下帧再处理
                Ecb.SetComponentEnabled<MonsterTempDotDamageText>(sortKey, entity, false);
            }
        }
    }
}