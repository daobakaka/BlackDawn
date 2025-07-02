using System.Collections.Generic;
using System;
using UnityEngine;

namespace GameFrame.Runtime
{
    using HoldAction = Action<float,float>;
    
    internal partial class InputOperate : IInputOperate
    {
        /// <summary>
        /// 触发事件
        /// </summary>
        public Dictionary<InputCode, Action> TriggerActions { get; } =  new();
        
        /// <summary>
        /// 抬起事件
        /// </summary>
        public Dictionary<InputCode, Action> TriggerActionsCancel { get; } =  new();
        
        /// <summary>
        /// 持续按下事件
        /// </summary>
        public Dictionary<InputCode, HoldAction> HoldActions { get; } =  new();
        

        /// <summary>
        /// 是否忽略时缩放
        /// </summary>
        public bool IsIgnoreTimescale { get; private set;} =  false;
        
        /// <summary>
        /// 操作标记
        /// </summary>
        public int Flag { get; private set;} = -1;

        /// <summary>
        /// 是否为主操作
        /// </summary>
        public bool IsMain { get; } = false;
        
        /// <summary>
        /// 是否为页面根节点
        /// </summary>
        public bool IsPageRoot { get; private set;} =  false;
        
        /// <summary>
        /// 是否继承上次操作
        /// </summary>
        public bool ContinueOperate { get; private set;} =  false;

        /// <summary>
        /// 持续按下事件间隔
        /// </summary>
        public Dictionary<InputCode, float> Intervals { get; } = new();

        /// <summary>
        /// 开启的持续按下事件
        /// </summary>
        private Dictionary<InputCode,float> HoldEnables { get; } =  new ();
        
        /// <summary>
        /// 组合按键
        /// </summary>
        private List<CombAction> Combinations { get; } =  new();
    }
}