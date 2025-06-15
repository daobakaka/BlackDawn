using System;
using System.Collections.Generic;

namespace GameFrame.Fsm
{
    public interface IFsm<T> where T : class
    {
        /// <summary>
        /// 获取有限状态机名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 获取有限状态机完整名称
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// 获取有限状态机持有者
        /// </summary>
        T Owner { get; }

        /// <summary>
        /// 获取有限状态机中状态的数量
        /// </summary>
        int FsmStateCount { get; }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// 是否被销毁
        /// </summary>
        bool IsDestroyed { get; }

        /// <summary>
        /// 获取当前有限状态机状态
        /// </summary>
        FsmState<T> CurrentState { get; }

        /// <summary>
        /// 获取当前有限状态机状态名称
        /// </summary>
        float CurrentStateTime { get; }

        /// <summary>
        /// 开始有限状态机。
        /// </summary>
        /// <typeparam name="TState">要开始的有限状态机状态类型。</typeparam>
        void Start<TState>() where TState : FsmState<T>;

        /// <summary>
        /// 开始有限状态机。
        /// </summary>
        /// <param name="stateType">要开始的有限状态机状态类型。</param>
        void Start(Type stateType);

        /// <summary>
        /// 是否存在有限状态机状态。
        /// </summary>
        /// <typeparam name="TState">要检查的有限状态机状态类型。</typeparam>
        /// <returns>是否存在有限状态机状态。</returns>
        bool HasState<TState>() where TState : FsmState<T>;

        /// <summary>
        /// 是否存在有限状态机状态。
        /// </summary>
        /// <param name="stateType">要检查的有限状态机状态类型。</param>
        /// <returns>是否存在有限状态机状态。</returns>
        bool HasState(Type stateType);

        /// <summary>
        /// 获取有限状态机状态。
        /// </summary>
        /// <typeparam name="TState">要获取的有限状态机状态类型。</typeparam>
        /// <returns>要获取的有限状态机状态。</returns>
        TState GetState<TState>() where TState : FsmState<T>;

        /// <summary>
        /// 获取有限状态机状态。
        /// </summary>
        /// <param name="stateType">要获取的有限状态机状态类型。</param>
        /// <returns>要获取的有限状态机状态。</returns>
        FsmState<T> GetState(Type stateType);

        /// <summary>
        /// 获取有限状态机的所有状态。
        /// </summary>
        /// <returns>有限状态机的所有状态。</returns>
        FsmState<T>[] GetAllStates();
        
        /// <summary>
        /// 获取有限状态机的所有状态。
        /// </summary>
        /// <returns>有限状态机的所有状态。</returns>
        Type[] GetAllStatesType();

        /// <summary>
        /// 获取有限状态机的所有状态。
        /// </summary>
        /// <param name="results">有限状态机的所有状态。</param>
        void GetAllStates(List<FsmState<T>> results);

        /// <summary>
        /// 是否存在有限状态机数据。
        /// </summary>
        /// <param name="name">有限状态机数据名称。</param>
        /// <returns>有限状态机数据是否存在。</returns>
        bool HasData(string name);


        /// <summary>
        /// 获取有限状态机数据。
        /// </summary>
        /// <param name="name">有限状态机数据名称。</param>
        /// <returns>要获取的有限状态机数据。</returns>
        //Variable GetVariable(string name);


        /// <summary>
        /// 从黑板中获取有限状态机数据。
        /// </summary>
        /// <param name="name">有限状态机数据名称。</param>
        /// <returns>要获取的有限状态机数据。</returns>
        TDate GetValue<TDate>(string name);

        /// <summary>
        /// 设置有限状态机数据。
        /// </summary>
        /// <typeparam name="TDate">要设置的有限状态机数据的类型。</typeparam>
        /// <param name="name">有限状态机数据名称。</param>
        /// <param name="data">要设置的有限状态机数据。</param>
        void SetValue<TDate>(string name, TDate data);

        /// <summary>
        /// 移除有限状态机数据。
        /// </summary>
        /// <param name="name">有限状态机数据名称。</param>
        /// <returns>是否移除有限状态机数据成功。</returns>
        bool RemoveData(string name);

        //// <summary>
        /// 切换当前有限状态机状态。
        /// </summary>
        /// <typeparam name="TState">要切换到的有限状态机状态类型。</typeparam>
        void ChangeState<TState>() where TState : FsmState<T>;

        /// <summary>
        /// 切换当前有限状态机状态。
        /// </summary>
        /// <param name="stateType">要切换到的有限状态机状态类型。</param>
        void ChangeState(Type stateType);
    }
}