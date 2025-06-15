using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
//之前的碰撞对处理系统，默认关闭
namespace BlackDawn.DOTS
{
    /// <summary>
    /// 用于处理所有Trigger碰撞的系统
    /// </summary>
    [RequireMatchingQueriesForUpdate]   // 若所有 Query 都匹配不到实体，则系统永不执行
    [UpdateBefore(typeof(ActionSystemGroup))]
    [BurstCompile]
    public partial struct TriggerSystem : ISystem,ISystemStartStop
    {
        int batchSize;

        public ComponentLookup<MonsterAttributeCmpt> attrLookup;
        public BufferLookup<BuffHandlerBuffer> buffHandlerBuffer;
        public BufferLookup<LinkedEntityGroup> linkedLookup;
        public ComponentLookup<LocalTransform> monsterTrasform;
        public void UpdateAllLookup(ref SystemState state)
        {
            attrLookup.Update(ref state);
            buffHandlerBuffer.Update(ref state);
            linkedLookup.Update(ref state);
            monsterTrasform.Update(ref state);
        }

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnableTriggerSystemTag>();
          
            attrLookup = SystemAPI.GetComponentLookup<MonsterAttributeCmpt>(true);
            buffHandlerBuffer = SystemAPI.GetBufferLookup<BuffHandlerBuffer>(true);
            linkedLookup = SystemAPI.GetBufferLookup<LinkedEntityGroup>(true);
            monsterTrasform = SystemAPI.GetComponentLookup<LocalTransform>(true);
            // 假设核心数较多时可使用更大批处理大小
            batchSize = SystemInfo.processorCount > 8 ? 64 : 32;

        }
        public void OnDestroy(ref SystemState state)
        {

        }

        public void OnUpdate(ref SystemState state)
        {
            

    


          

        }

        public void OnStartRunning(ref SystemState state)
        {
            Debug.Log("triggrt 系统开");
        }
            public void OnStopRunning(ref SystemState state)
        {
            
        }
        //并行所有的Job，但是要确保所有数据安全            //: 感觉所有TriggerJob可以使用并行
        //JobHandle.CombineDependencies()
    }



    /// <summary>
    /// 用于区分各个Trigger，
    /// 区分完后的由各TriggerJob做具体的逻辑
    /// </summary>
   
    


}