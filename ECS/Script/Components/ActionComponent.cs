using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.VisualScripting;

namespace BlackDawn.DOTS
{
    /// <summary>
    /// 行为组件组ComponentLookup，用于Job使用
    /// 带切换功能，让各行为在Job切换时更便捷、高效、合法
    /// </summary>
    /// /// 
    public struct OtherActionsGroup<T> where T : struct, IEnableableComponent
    {
        /// <summary>
        /// 切换行为
        /// </summary>
        public readonly void SwitchAction(EntityCommandBuffer.ParallelWriter ecb, int chunkIndex, Entity entity, EActionType actionType)// where U : struct, IEnableableComponent
        {
            //关闭当前行为
            ecb.SetComponentEnabled<T>(chunkIndex, entity, false);
            //开启下一个行为
            switch (actionType)
            {
                case EActionType.Idle : ecb.SetComponentEnabled<Idle>(chunkIndex, entity, true); break;
                case EActionType.Run : ecb.SetComponentEnabled<Run>(chunkIndex, entity, true); break;
                case EActionType.Attack : ecb.SetComponentEnabled<Attack>(chunkIndex, entity, true); break;
                default: break;
            }

        }

    }

    public struct Idle : IComponentData, IEnableableComponent { }
    public struct Run : IComponentData, IEnableableComponent { }
    public struct Attack : IComponentData, IEnableableComponent { }
    public struct Die : IComponentData, IEnableableComponent { }



}