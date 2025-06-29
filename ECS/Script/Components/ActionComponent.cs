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

    public struct OtherActionsGroup<T> where T : struct, IEnableableComponent
    {

        public readonly void SwitchAction(EntityCommandBuffer.ParallelWriter ecb, int chunkIndex, Entity entity, EActionType actionType)// where U : struct, IEnableableComponent
        {

            ecb.SetComponentEnabled<T>(chunkIndex, entity, false);

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