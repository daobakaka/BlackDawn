using System;

namespace GameFrame.Fsm
{
    /// <summary>
    /// 有限状态机基类
    /// </summary>
    public abstract class FsmBase
    {
        /// <summary>
        /// 状态机名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 状态机的完整名字
        /// </summary>
        /// <value></value>
        public string FullName => $"{OwnerType.FullName}.{Name}";

        /// <summary>
        /// 状态机的持有者类型
        /// </summary>
        /// <value></value>
        public abstract Type OwnerType { get; }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        /// <value></value>
        public abstract bool IsRunning { get; }
        
        /// <summary>
        /// 是否被销毁
        /// </summary>
        /// <value></value>
        public abstract bool IsDestroyed { get; }

        /// <summary>
        /// 当前状态机名称
        /// </summary>
        /// <value></value>
        public abstract string CurrentStateName { get; }

        /// <summary>
        /// 当前状态机持续时间
        /// </summary>
        /// <value></value>
        public abstract float CurrentStateTime { get; }

        /// <summary>
        /// 轮询状态机
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝的时间</param>
        /// <param name="realElapseSeconds">真实流逝的时间</param>
        public abstract void Update(float elapseSeconds, float realElapseSeconds);

        /// <summary>
        /// 关闭状态机
        /// </summary>
        public abstract void Shutdown();
    }
}