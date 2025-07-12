using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities.UniversalDelegates;
using Unity.Rendering;

namespace BlackDawn.DOTS
{
    /// <summary>
    /// 行为系统组， 主要的伤害计算，以及行为变化
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class ActionSystemGroup : ComponentSystemGroup
    {

    }

    /// <summary>
    /// 渲染系统组
    /// </summary>
    [UpdateInGroup(typeof(DeformationsInPresentation))]
    public partial class RenderSystemGroup : ComponentSystemGroup
    {

    }


    /// <summary>
    /// 主线程系统组，结构性变化，以及轻数据的更新
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ActionSystemGroup))]
    public partial class MainThreadSystemGroup : ComponentSystemGroup
    {

    }



 

}
namespace BlackDawn.DOTS
{
    //自定义 ecb buffer
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(RenderEffectSystem))]
    public partial class CustomEndActionECBSystem : EntityCommandBufferSystem
    {
        // The singleton component data access pattern should be used to safely access
        // the command buffer system. This data will be stored in the derived ECB System's
        // system entity.

        public unsafe struct Singleton : IComponentData, IECBSingleton
        {
            internal UnsafeList<EntityCommandBuffer>* pendingBuffers;
            internal AllocatorManager.AllocatorHandle allocator;

            public EntityCommandBuffer CreateCommandBuffer(WorldUnmanaged world)
            {
                return EntityCommandBufferSystem
                    .CreateCommandBuffer(ref *pendingBuffers, allocator, world);
            }

            // Required by IECBSingleton
            public void SetPendingBufferList(ref UnsafeList<EntityCommandBuffer> buffers)
            {
                var ptr = UnsafeUtility.AddressOf(ref buffers);
                pendingBuffers = (UnsafeList<EntityCommandBuffer>*)ptr;
            }

            // Required by IECBSingleton
            public void SetAllocator(Allocator allocatorIn)
            {
                allocator = allocatorIn;
            }

            // Required by IECBSingleton
            public void SetAllocator(AllocatorManager.AllocatorHandle allocatorIn)
            {
                allocator = allocatorIn;
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            this.RegisterSingleton<Singleton>(ref PendingBuffers, World.Unmanaged);
        }
    }



    //自定义 初始化中途
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(EndInitializationEntityCommandBufferSystem))]
    public partial class CustomMiddleInitializationECBSystem : EntityCommandBufferSystem
    {
        // The singleton component data access pattern should be used to safely access
        // the command buffer system. This data will be stored in the derived ECB System's
        // system entity.

        public unsafe struct Singleton : IComponentData, IECBSingleton
        {
            internal UnsafeList<EntityCommandBuffer>* pendingBuffers;
            internal AllocatorManager.AllocatorHandle allocator;

            public EntityCommandBuffer CreateCommandBuffer(WorldUnmanaged world)
            {
                return EntityCommandBufferSystem
                    .CreateCommandBuffer(ref *pendingBuffers, allocator, world);
            }

            // Required by IECBSingleton
            public void SetPendingBufferList(ref UnsafeList<EntityCommandBuffer> buffers)
            {
                var ptr = UnsafeUtility.AddressOf(ref buffers);
                pendingBuffers = (UnsafeList<EntityCommandBuffer>*)ptr;
            }

            // Required by IECBSingleton
            public void SetAllocator(Allocator allocatorIn)
            {
                allocator = allocatorIn;
            }

            // Required by IECBSingleton
            public void SetAllocator(AllocatorManager.AllocatorHandle allocatorIn)
            {
                allocator = allocatorIn;
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            this.RegisterSingleton<Singleton>(ref PendingBuffers, World.Unmanaged);
        }
    }

      //自定义 渲染中途系统组
    [UpdateInGroup(typeof(UpdatePresentationSystemGroup))]

    public partial class CustomStartUpdatePresentationECBSystem : EntityCommandBufferSystem
    {
        // The singleton component data access pattern should be used to safely access
        // the command buffer system. This data will be stored in the derived ECB System's
        // system entity.

        public unsafe struct Singleton : IComponentData, IECBSingleton
        {
            internal UnsafeList<EntityCommandBuffer>* pendingBuffers;
            internal AllocatorManager.AllocatorHandle allocator;

            public EntityCommandBuffer CreateCommandBuffer(WorldUnmanaged world)
            {
                return EntityCommandBufferSystem
                    .CreateCommandBuffer(ref *pendingBuffers, allocator, world);
            }

            // Required by IECBSingleton
            public void SetPendingBufferList(ref UnsafeList<EntityCommandBuffer> buffers)
            {
                var ptr = UnsafeUtility.AddressOf(ref buffers);
                pendingBuffers = (UnsafeList<EntityCommandBuffer>*)ptr;
            }

            // Required by IECBSingleton
            public void SetAllocator(Allocator allocatorIn)
            {
                allocator = allocatorIn;
            }

            // Required by IECBSingleton
            public void SetAllocator(AllocatorManager.AllocatorHandle allocatorIn)
            {
                allocator = allocatorIn;
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            this.RegisterSingleton<Singleton>(ref PendingBuffers, World.Unmanaged);
        }
    }
        //自定义 渲染中途系统组
    [UpdateInGroup(typeof(UpdatePresentationSystemGroup))]
    [UpdateAfter(typeof(CustomStartUpdatePresentationECBSystem))]

    public partial class CustomMiddleUpdatePresentationECBSystem : EntityCommandBufferSystem
    {
        // The singleton component data access pattern should be used to safely access
        // the command buffer system. This data will be stored in the derived ECB System's
        // system entity.

        public unsafe struct Singleton : IComponentData, IECBSingleton
        {
            internal UnsafeList<EntityCommandBuffer>* pendingBuffers;
            internal AllocatorManager.AllocatorHandle allocator;

            public EntityCommandBuffer CreateCommandBuffer(WorldUnmanaged world)
            {
                return EntityCommandBufferSystem
                    .CreateCommandBuffer(ref *pendingBuffers, allocator, world);
            }

            // Required by IECBSingleton
            public void SetPendingBufferList(ref UnsafeList<EntityCommandBuffer> buffers)
            {
                var ptr = UnsafeUtility.AddressOf(ref buffers);
                pendingBuffers = (UnsafeList<EntityCommandBuffer>*)ptr;
            }

            // Required by IECBSingleton
            public void SetAllocator(Allocator allocatorIn)
            {
                allocator = allocatorIn;
            }

            // Required by IECBSingleton
            public void SetAllocator(AllocatorManager.AllocatorHandle allocatorIn)
            {
                allocator = allocatorIn;
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            this.RegisterSingleton<Singleton>(ref PendingBuffers, World.Unmanaged);
        }
    }
         //自定义 渲染中途系统组
    [UpdateInGroup(typeof(UpdatePresentationSystemGroup))]
    [UpdateAfter(typeof(CustomMiddleUpdatePresentationECBSystem))]

    public partial class CustomEndUpdatePresentationECBSystem : EntityCommandBufferSystem
    {
        // The singleton component data access pattern should be used to safely access
        // the command buffer system. This data will be stored in the derived ECB System's
        // system entity.

        public unsafe struct Singleton : IComponentData, IECBSingleton
        {
            internal UnsafeList<EntityCommandBuffer>* pendingBuffers;
            internal AllocatorManager.AllocatorHandle allocator;

            public EntityCommandBuffer CreateCommandBuffer(WorldUnmanaged world)
            {
                return EntityCommandBufferSystem
                    .CreateCommandBuffer(ref *pendingBuffers, allocator, world);
            }

            // Required by IECBSingleton
            public void SetPendingBufferList(ref UnsafeList<EntityCommandBuffer> buffers)
            {
                var ptr = UnsafeUtility.AddressOf(ref buffers);
                pendingBuffers = (UnsafeList<EntityCommandBuffer>*)ptr;
            }

            // Required by IECBSingleton
            public void SetAllocator(Allocator allocatorIn)
            {
                allocator = allocatorIn;
            }

            // Required by IECBSingleton
            public void SetAllocator(AllocatorManager.AllocatorHandle allocatorIn)
            {
                allocator = allocatorIn;
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            this.RegisterSingleton<Singleton>(ref PendingBuffers, World.Unmanaged);
        }
    }
}