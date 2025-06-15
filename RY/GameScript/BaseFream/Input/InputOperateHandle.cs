using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameFrame.Runtime
{
    using BindingKey = Dictionary<string, Dictionary<int,string>>;
    using ResetOperate = InputActionRebindingExtensions.RebindingOperation;
    
    public partial class InputOperateHandle : MonoBehaviour
    {
        private static PlayerInput _input;

        /// <summary>
        /// 操作按键类型映射
        /// </summary>
        private static Dictionary<string, InputCode> _inputCodeMap;
        
        /// <summary>
        /// 操作按键类型绑定记录
        /// </summary>
        private static BindingKey _recordBinding;
        
        /// <summary>
        /// 移动
        /// </summary>
        private static InputAction _move;

        /// <summary>
        /// W
        /// </summary>
        private static InputAction _w;
        
        /// <summary>
        /// A
        /// </summary>
        private static InputAction _a;
        
        /// <summary>
        /// S
        /// </summary>
        private static InputAction _s;
        
        /// <summary>
        /// D
        /// </summary>
        private static InputAction _d;
        
        /// <summary>
        /// Shift
        /// </summary>
        private static InputAction _shift;

        /// <summary>
        /// 确认
        /// </summary>
        private static InputAction _sure;

        /// <summary>
        /// 取消
        /// </summary>
        private static InputAction _cancel;

        /// <summary>
        /// 鼠标点击
        /// </summary>
        private static InputAction _click;
        
        /// <summary>
        /// 触摸
        /// </summary>
        private static InputAction _touch;

        private static InputAction _new;
        /// <summary>
        /// 鼠标滚轮滚动
        /// </summary>        
        private static InputAction _mouseWheel;

        /// <summary>
        /// 键盘Space
        /// </summary>        
        private static InputAction _space;

        /// <summary>
        /// 当前操作
        /// </summary>
        private static InputOperate _operate;
        
        /// <summary>
        /// 重绑定操作
        /// </summary>
        private static ResetOperate _operateRest;
        
        /// <summary>
        /// 操作栈
        /// </summary>
        private static Stack<InputOperate> _operateStack;
        
        /// <summary>
        /// 输入信息
        /// </summary>
        private  static  InputInfo _inputInfo = new();

        /// <summary>
        /// 当前的输入轴
        /// </summary>
        public static Vector2 InputAxis => _inputInfo.axis;

        /// <summary>
        /// 全局输入操作
        /// </summary>
        private static Dictionary<InputCode, Action> _actions;
        
        /// <summary>
        /// 全局输入取消
        /// </summary>
        private static Dictionary<InputCode, Action> _actionsCancel;
        
        
        class InputInfo
        {
            /// <summary>
            /// 键按下的时间
            /// </summary>
            public float timer;
            
            /// <summary>
            /// 当前的键
            /// </summary>
            public InputCode code;
            
            /// <summary>
            /// 操作记录
            /// </summary>
            public List<InputCode> codeStack = new ();
            
            /// <summary>
            /// 输入轴信息
            /// </summary>
            public Vector2 axis = Vector2.zero;
            
            /// <summary>
            /// 持续按下的时间
            /// </summary>
            public HodeInfo[] holds = new HodeInfo[(int)InputCode.Max];

            public InputInfo()
            {
                for (int i = 0; i < holds.Length; i++)
                    holds[i] = new HodeInfo();
            }
            
            public void HoldBegin(InputCode code)
            {
                var hold = holds[(int)code];
                hold.started = true;
                hold.timer = 0;
            }
            
            public void HoldClear(InputCode code)
            {
                var hold = holds[(int)code];
                hold.started = false;
                hold.timer = 0;
            }

            public void Reset()
            {
                timer = 0;
                
                foreach (var hold in this.holds)
                    hold.Reset(); 
            }
        }
        
        class HodeInfo
        {
            public bool started;
            public float timer;
            
            public void Reset()
            {
                started = false;
                timer = 0;
            }
        }
        
    }
}