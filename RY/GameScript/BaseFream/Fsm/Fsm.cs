using System;
using System.Collections.Generic;
using System.Linq;
using GameFrame;



namespace GameFrame.Fsm
{
    /// <summary>
    /// 有限状态机。
    /// </summary>
    /// <typeparam name="T">有限状态机持有者类型。</typeparam>
    public class Fsm<T> : FsmBase, IFsm<T> where T : class
    {   
        /// <summary>
        /// 状态机状态
        /// </summary>
        readonly Dictionary<Type, FsmState<T>> _states;

        /// <summary>
        /// 状态机黑板
        /// </summary>
        readonly Dictionary<string, Variable> _blackboard;

        // 当前状态
        private FsmState<T> _currentState;

        // 当前状态时间
        private float _currentStateTime;

        // 是否销毁
        private bool _isDestroyed;

        private T _owner;

        public Fsm()
        {
            _owner = null;
            _states = new();
            _blackboard = new();
            _currentState = null;
            _currentStateTime = 0f;
            _isDestroyed = true;
        }

        /// <summary>
        /// 持有者
        /// </summary>
        /// <returns></returns>
        public T Owner => _owner;

        /// <summary>
        /// 持有者类型
        /// </summary>
        /// <returns></returns>
        public override Type OwnerType => typeof(T);

        /// <summary>
        /// 当前状态
        /// </summary>
        /// <returns></returns>
        public FsmState<T> CurrentState => _currentState;

        /// <summary>
        /// 状态的数量。
        /// </summary>
        public int FsmStateCount => _states.Count;

        /// <summary>
        /// 是否正在运行。
        /// </summary>
        /// <returns></returns>
        public override bool IsRunning => _currentState != null;

        /// <summary>
        /// 是否被销毁。
        /// </summary>
        public override bool IsDestroyed => _isDestroyed;

        /// <summary>
        /// 当前状态持续时间
        /// </summary>
        public override float CurrentStateTime => _currentStateTime;

        /// <summary>
        /// 当前状态名称。
        /// </summary>
        public override string CurrentStateName => _currentState?.GetType()?.FullName ?? string.Empty;

        /// <summary>
        /// 创建有限状态机
        /// </summary>
        /// <param name="name">状态机名称</param>
        /// <param name="owner">拥有者</param>
        /// <param name="states">状态</param>
        /// <returns></returns>
        public static Fsm<T> Create(string name,T owner,params FsmState<T>[] states)
        {
            if (null == owner)
                throw new Exception("FSM owner is invalid.");
            
            if (states == null || states.Length == 0)
                throw new Exception("FSM states is invalid.");

            // Fsm<T> fsm = ReferencePool.Acquire<Fsm<T>>();
            // {
            //     fsm.Name = name;
            //     fsm._owner = owner;
            //     fsm._isDestroyed = false;
            // }

            Fsm<T> fsm = new()
            {
                Name = name,
                _owner = owner,
                _isDestroyed = false
            };


            foreach (var state in states)
            {
                if (null == state)
                    throw new Exception("FSM state is null.");
                
                Type stateType = state.GetType();
                if (!fsm._states.TryAdd(stateType, state))
                    throw new Exception($"FSM state type: {stateType} is duplicated.");

                state.OnInit(fsm);
            }

            return fsm;
        }

        /// <summary>
        /// 创建有限状态机
        /// </summary>
        /// <param name="name">状态机名称</param>
        /// <param name="owner">拥有者</param>
        /// <param name="states">状态</param>
        /// <returns></returns>
        public static Fsm<T> Create(string name, T owner, List<FsmState<T>> states)
        {
            if (null == owner)
                throw new Exception("FSM owner is invalid.");

            if (states == null || states.Count == 0)
                throw new Exception("FSM states is invalid.");

            Fsm<T> fsm = new()
            {
                Name = name,
                _owner = owner,
                _isDestroyed = false
            };

            foreach (var state in states)
            {
                if (null == state)
                    throw new Exception("FSM state is null.");

                Type stateType = state.GetType();
                if (!fsm._states.TryAdd(stateType, state))
                    throw new Exception($"FSM state type: {stateType} is duplicated.");

                state.OnInit(fsm);
            }

            return fsm;
        }

        /// <summary>
        /// 清理状态机
        /// </summary>
        public void Clear()
        {
            _currentState?.OnExit(this, true);

            foreach (var state in _states.Values)
            {
                state.OnDestroy(this);
            }

            // foreach (var val in _blackboard.Values)
            // {
            //     ReferencePool.Release(val);
            // }

            Name = null;
            _owner = null;
            _states.Clear();
            _blackboard.Clear();
            _currentState = null;
            _currentStateTime = 0f;
            _isDestroyed = true;
        }

        /// <summary>
        /// 启动状态机
        /// </summary>
        /// <typeparam name="TState">启动状态</typeparam>
        public void Start<TState>() where TState : FsmState<T>
        {
            if (_isDestroyed)
                throw new Exception("FSM is destroyed.");

            if (IsRunning)
                throw new Exception("FSM is already running.");
            
            var state = GetState<TState>() ??
                throw new Exception($"FSM state type: {typeof(TState)} is not found.");
            
            _currentState = state;
            _currentStateTime = 0f;
            _currentState.OnEnter(this);
        }

        /// <summary>
        /// 启动状态机
        /// </summary>
        public void Start(Type stateType)
        {
            if (_isDestroyed)
                throw new Exception("FSM is destroyed.");

            if (IsRunning)
                throw new Exception("FSM is already running.");

            var state = GetState(stateType) ??
                throw new Exception($"FSM state type: {stateType} is not found.");

            _currentState = state;
            _currentStateTime = 0f;
            _currentState.OnEnter(this);
        }

        /// <summary>
        /// 黑板是否存在数据
        /// </summary>
        /// <param name="name">数据名称</param>
        /// <returns></returns>
        public bool HasData(string name)
        {
            return _blackboard.ContainsKey(name);
        }

        /// <summary>
        /// 状态是否存在
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <returns></returns>
        public bool HasState<TState>() where TState : FsmState<T>
        {
            return _states.ContainsKey(typeof(TState));
        }

        /// <summary>
        /// 状态是否存在
        /// </summary>
        public bool HasState(Type stateType)
        {
            return _states.ContainsKey(stateType);
        }
        
        /// <summary>
        /// 获取状态
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <returns></returns>
        public TState GetState<TState>() where TState : FsmState<T>
        {
            return _states.TryGetValue(typeof(TState), out var state) ? (TState)state : null;
        }

        /// <summary>
        /// 获取状态
        /// </summary>
        public FsmState<T> GetState(Type stateType)
        {
            return _states.TryGetValue(stateType, out var state) ? state : null;
        }

        /// <summary>
        /// 获取所有状态。
        /// </summary>
        public FsmState<T>[] GetAllStates()
        {
            return _states.Values.ToArray();
        }
        
        /// <summary>
        /// 获取所有状态类型。
        /// </summary>
        /// <returns></returns>
        Type[] IFsm<T>.GetAllStatesType()
        {
            return _states.Keys.ToArray();
        }

        /// <summary>
        /// 获取所有状态。
        /// </summary>
        public void GetAllStates(List<FsmState<T>> results)
        {
           if (results == null)
                throw new Exception("Results is null.");

           results.Clear();
           results.AddRange(_states.Values);
        }

        /// <summary>
        /// 获取黑板数据
        /// </summary>
        public Variable GetVariable(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new Exception("Data name is invalid.");
            
            return _blackboard.TryGetValue(name, out var data) ? data : null;
        }

        /// <summary>
        /// 获取黑板数据中的值
        /// </summary>
        public TDate GetValue<TDate>(string name)
        {
            if(string.IsNullOrEmpty(name))
                throw new Exception("Data name is invalid.");

            if (_blackboard.TryGetValue(name, out var data))
                return data.GetValue<TDate>() ?? default;

            throw new Exception($"Data is not found : {name}");
        }


        /// <summary>
        /// 设置黑板数据中的值
        /// </summary>
        public void SetValue<TDate>(string name, TDate data)
        {
            if (string.IsNullOrEmpty(name))
                throw new Exception("Data name is invalid.");

            if (_blackboard.TryGetValue(name, out var val))
                val.SetValue(data);
            else
            {
                //var variable = ReferencePool.Acquire<Variable<TDate>>();
                var variable = new Variable<TDate>();
                variable.SetValue(data);
                _blackboard[name] = variable;
            }
        }


        /// <summary>
        /// 移除黑板数据
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool RemoveData(string name)
        {
            return _blackboard.Remove(name);    
        }

        /// <summary>
        /// 轮询状态机
        /// </summary>
        /// <param name="elapseSeconds"></param>
        /// <param name="realElapseSeconds"></param>
        public override void Update(float elapseSeconds = 0, float realElapseSeconds = 0)
        {
            if (null == _currentState)
                return;

            _currentStateTime += elapseSeconds;
            _currentState.OnUpdate(this, elapseSeconds, realElapseSeconds); 
        }

        /// <summary>
        /// 关闭状态机
        /// </summary>
        public override void Shutdown()
        {
            //ReferencePool.Release(this);
        }

        /// <summary>
        /// 切换状态机
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        public void ChangeState<TState>() where TState : FsmState<T>
        {
            ChangeState(typeof(TState));
        }

        /// <summary>
        /// 切换状态机
        /// </summary>
        public void ChangeState(Type stateType)
        {
            if (null == _currentState)
                throw new Exception("FSM current state is null.");
            
            var state = GetState(stateType) ?? 
                throw new Exception($"FSM state type: {stateType} is not found.");
            
            _currentState.OnExit(this, false);
            _currentState = state;
            _currentStateTime = 0f;
            _currentState.OnEnter(this);
        }

       
    }
}