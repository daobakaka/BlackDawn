using System;

namespace GameFrame.Fsm
{
    /// <summary>
    /// 有限状态机-状态基类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class FsmState<T> where T : class
    {
        /// <summary>
        /// 初始化有限状态机状态，这里在创建状态机的时候引用
        /// </summary>
        /// <param name="fsm">有限状态机的引用</param>
        protected internal virtual void OnInit(IFsm<T> fsm) { }

        /// <summary>
        /// 有限状态机状态进入时调用。
        /// </summary>
        protected internal virtual void OnEnter(IFsm<T> fsm) { }

        /// <summary>
        /// 有限状态机状态轮询时调用。
        /// </summary>
        /// <param name="fsm">有限状态机引用。</param>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        protected internal virtual void OnUpdate(IFsm<T> fsm, float elapseSeconds, float realElapseSeconds) { }

        /// <summary>
        /// 有限状态机状态离开时调用。
        /// </summary>
        /// <param name="fsm">有限状态机引用。</param>
        /// <param name="isShutdown">是否是关闭有限状态机时触发。</param>
        protected internal virtual void OnExit(IFsm<T> fsm, bool isShutdown) { }

        /// <summary>
        /// 有限状态机状态销毁时调用。
        /// </summary>
        protected internal virtual void OnDestroy(IFsm<T> fsm) { }

        protected internal void ChangeState<TState>(IFsm<T> fsm) where TState : FsmState<T> => fsm.ChangeState<TState>();
     
        /// <summary>
        /// 切换当前有限状态机状态。
        /// </summary>
        /// <param name="fsm">有限状态机引用。</param>
        /// <param name="stateType">要切换到的有限状态机状态类型。</param>
        protected void ChangeState(IFsm<T> fsm, Type stateType) => fsm.ChangeState(stateType);
    }
}