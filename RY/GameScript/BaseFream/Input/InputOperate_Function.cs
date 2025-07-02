using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFrame.Runtime
{
    using HoldAction = Action<float,float>;
    
    internal partial class InputOperate : IInputOperate
    {      // 私有字段用于保存用户赋值的回调
        private  Action<float> _onMouseWheel;

        private class CombAction
        {
            /// <summary>
            /// 绑定的事件
            /// </summary>
            private readonly Action _action;
            
            /// <summary>
            /// 最低按键间隔
            /// </summary>
            private readonly float _interval;
            
            /// <summary>
            /// 绑定的组合按键
            /// </summary>
            private readonly string _target;
            
            /// <summary>
            /// 输入组合的进度
            /// </summary>
            private string _progress = string.Empty;
            
            /// <summary>
            /// 上次按下的时间
            /// </summary>
            private float _lastTime = 0;

            public CombAction(string target, float interval, Action action)
            {
                _target = target.ToLower();
                _interval = interval;
                _action = action;
            }
            /// <summary>
            /// 输入组合
            /// </summary>
            /// <param name="key"></param>
            public bool Invoke(char key)
            {
                if (string.IsNullOrEmpty(_progress))
                {
                    _progress = key.ToString();
                    _lastTime = Time.time;
                }
                else
                {
                    var time = Time.time - _lastTime;
                    if (time > _interval)
                    {
                        _progress = string.Empty;
                        return false;
                    }
                    _lastTime = Time.time;
                    _progress += key;
                }
                
                if (!_target.StartsWith(_progress))
                {
                    _progress = string.Empty;
                    return false;
                }

                if (_progress.Equals(_target))
                {
                    _action?.Invoke();
                    _progress = string.Empty;
                    return true;
                }
                
                return false;
            }
        }
        
        public InputOperate(bool isMain)
        {
            this.IsMain = isMain;
        }
        
        public Action onUp
        {
            get => Trigger(InputCode.Up,TriggerActions); 
            set => TriggerActions[InputCode.Up] = value;
        }
        
        public Action onDown
        {
            get => Trigger(InputCode.Down,TriggerActions); 
            set => TriggerActions[InputCode.Down] = value;
        }

        public Action onLeft
        {
            get => Trigger(InputCode.Left,TriggerActions);
            set => TriggerActions[InputCode.Left] = value;
        }
        
        public Action onRight
        {
            get => Trigger(InputCode.Right,TriggerActions);
            set => TriggerActions[InputCode.Right] = value;
        }
        
        public Action onShift
        {
            get => Trigger(InputCode.Shift,TriggerActions);
            set => TriggerActions[InputCode.Shift] = value;
        }
        /// <summary>
        /// 新添加Space事件
        /// </summary>
        public Action onSpace
        {
            get => Trigger(InputCode.Space, TriggerActions);
            set => TriggerActions[InputCode.Space] = value;
        }
        public Action onSure
        {
            get => Trigger(InputCode.Sure,TriggerActions);
            set => TriggerActions[InputCode.Sure] = value;
        }


        // public Action on检测
        // {
        //     get => Trigger(InputCode.检测, TriggerActions);
        //     set => TriggerActions[InputCode.检测] = value;
        // }
        
        public Action onCancel
        {
            get => Trigger(InputCode.Cancel,TriggerActions);
            set => TriggerActions[InputCode.Cancel] = value;
        }
        
        
        
        public Action onUpCancel
        {
            get => Trigger(InputCode.Up,TriggerActionsCancel); 
            set => TriggerActionsCancel[InputCode.Up] = value;
        }
        
        public Action onDownCancel
        {
            get => Trigger(InputCode.Down,TriggerActionsCancel); 
            set => TriggerActionsCancel[InputCode.Down] = value;
        }

        public Action onLeftCancel
        {
            get => Trigger(InputCode.Left,TriggerActionsCancel);
            set => TriggerActionsCancel[InputCode.Left] = value;
        }
        
        public Action onRightCancel
        {
            get => Trigger(InputCode.Right,TriggerActionsCancel);
            set => TriggerActionsCancel[InputCode.Right] = value;
        }
        
        public Action onShiftCancel
        {
            get => Trigger(InputCode.Shift,TriggerActionsCancel);
            set => TriggerActionsCancel[InputCode.Shift] = value;
        }
        
        public Action onSureCancel
        {
            get => Trigger(InputCode.Sure,TriggerActionsCancel);
            set => TriggerActionsCancel[InputCode.Sure] = value;
        }
        
        public Action onCancelCancel
        {
            get => Trigger(InputCode.Cancel,TriggerActionsCancel);
            set => TriggerActionsCancel[InputCode.Cancel] = value;
        }
        
        public Action<Vector2> onMouse0
        {
            set
            {
                if (value == null)
                {
                    TriggerActions.Remove(InputCode.Mouse0);
                    return;
                }
                
                TriggerActions[InputCode.Mouse0] =
                    () => value.Invoke(Input.mousePosition);
            }
        }

        public Action<Vector2> onMouse1
        {
            set
            {
                if (value == null)
                {
                    TriggerActions.Remove(InputCode.Mouse1);
                    return;
                }
                
                TriggerActions[InputCode.Mouse1] =
                    () => value.Invoke(Input.mousePosition);
            }
        }
        
        public Action<Vector2> onMouse0Cancel
        {
            set
            {
                if (value == null)
                {
                    TriggerActionsCancel.Remove(InputCode.Mouse0);
                    return;
                }
                
                TriggerActionsCancel[InputCode.Mouse0] =
                    () => value.Invoke(Input.mousePosition);
            }
        }

        public Action<Vector2> onMouse1Cancel
        {
            set
            {
                if (value == null)
                {
                    TriggerActionsCancel.Remove(InputCode.Mouse1);
                    return;
                }
                
                TriggerActionsCancel[InputCode.Mouse1] =
                    () => value.Invoke(Input.mousePosition);
            }
        }

        public Action<Vector2> onTouch
        {
            set
            {
                if (value == null)
                {
                    TriggerActions.Remove(InputCode.Touch);
                    return;
                }
                
                TriggerActions[InputCode.Touch] =
                    () =>
                    {
                        var touch = Input.GetTouch(0);
                        value.Invoke(touch.position);
                    };
            }
        }
        
        public Action<Vector2> onTouchCancel
        {
            set
            {
                if (value == null)
                {
                    TriggerActionsCancel.Remove(InputCode.Touch);
                    return;
                }
                
                TriggerActionsCancel[InputCode.Touch] =
                    () =>
                    {
                        var touch = Input.GetTouch(0);
                        value.Invoke(touch.position);
                    };
            }
        }

        /// <summary>
        /// 鼠标滚轮滚动时触发，参数是滚动增量（正上为 +，正下为 -）
        /// </summary>
        public Action<float> onMouseWheel
        {
           get => _onMouseWheel;


            set
            {
                // 把用户的回调先存到字段里
                _onMouseWheel = value;
                if (value == null)
                {
                    TriggerActions.Remove(InputCode.MouseWheel);
                    return;
                }

                // 在触发列表中注册一个包装，将当前滚轮增量传给用户回调
                TriggerActions[InputCode.MouseWheel] = () =>
                {
                    // 只关心 y 轴增量
                    float delta = Input.mouseScrollDelta.y;
                    if (Mathf.Abs(delta) > 0f)
                        value.Invoke(delta);
                };
            }
        }

        public HoldAction onUpHold
        {
            get => Hold(InputCode.Up);
            set => HoldActions[InputCode.Up] = value;
        }

        // public HoldAction on检测Hold
        // {
        //     get => Hold(InputCode.检测);
        //     set => HoldActions[InputCode.检测] = value;
        // }

        public HoldAction onDownHold
        {
            get => Hold(InputCode.Down);
            set => HoldActions[InputCode.Down] = value;
        }

        public HoldAction onLeftHold
        {
            get => Hold(InputCode.Left);
            set => HoldActions[InputCode.Left] = value;
        }

        public HoldAction onRightHold
        {
            get => Hold(InputCode.Right);
            set => HoldActions[InputCode.Right] = value;
        }
        
        public HoldAction onShiftHold
        {
            get => Hold(InputCode.Shift);
            set => HoldActions[InputCode.Shift] = value;
        }
        
        public HoldAction onSureHold
        {
            get => Hold(InputCode.Sure);
            set => HoldActions[InputCode.Sure] = value;
        }
        
        public HoldAction onCancelHold
        {
            get => Hold(InputCode.Cancel);
            set => HoldActions[InputCode.Cancel] = value;
        }
        
        public HoldAction onMouse0Hold
        {
            get => Hold(InputCode.Mouse0);
            set => HoldActions[InputCode.Mouse0] = value;
        }
        
        public HoldAction onMouse1Hold
        {
            get => Hold(InputCode.Mouse1);
            set => HoldActions[InputCode.Mouse1] = value;
        }
        
        public HoldAction onTouchHold
        {
            get => Hold(InputCode.Touch);
            set => HoldActions[InputCode.Touch] = value;
        }

        public Action<Vector2> onMove { get; set; }
        public Action<InputCode> onMoveCancel{ get; set; }

        public IInputOperate SetInterval(InputCode code, float interval)
        {
            this.Intervals[code] = interval;
            return this;
        }

        public IInputOperate SetHoldTime(InputCode code, float holdTime)
        {
            HoldEnables[code] = holdTime;
            return this;
        }

        public IInputOperate SetFlag(int flag)
        {
            this.Flag = flag;
            return this;
        }

        public IInputOperate SetIgnoreTimescale(bool ignore)
        {
            this.IsIgnoreTimescale = ignore;
            return this;
        }

        public IInputOperate SetPageRoot(bool isRoot)
        {
            this.IsPageRoot = isRoot;
            return this;
        }

        public IInputOperate SetCombination(string keys, float interval, Action action)
        {
            Combinations.Add(new CombAction(keys, interval, action));
            return this;
        }

        public IInputOperate SetContinueOperate(bool isContinue)
        {
            ContinueOperate = isContinue;
            return this;
        }

        /// <summary>
        /// 尝试触发组合按键
        /// </summary>
        /// <param name="key"></param>
        public void TryInvokeCombination(char key)
        {
            foreach (var combination in Combinations)
            {
                if (combination.Invoke(key))
                    return;
            }
        }

        public float GetInInterval(InputCode code)
        {
            return Intervals.TryGetValue(code, out var holdTime) ? holdTime : -1;
        }

        public float GetHoldTime(InputCode code)
        {
            return HoldEnables.TryGetValue(code, out var holdTime) ? holdTime : -1;
        }

        public void Invoke(InputCode code)
        {
            TriggerActions.TryGetValue(code, out var action);
            action?.Invoke();
        }
        
        public void InvokeCancel(InputCode code)
        {
            TriggerActionsCancel.TryGetValue(code, out var action);
            action?.Invoke();
        }
        

        private Action Trigger(InputCode code, Dictionary<InputCode, Action> actions)
        {
            actions.TryGetValue(code, out var action);
            return action;
        }
        
        public HoldAction Hold(InputCode code)
        {
            HoldActions.TryGetValue(code, out var action);
            return action;
        }
    }
    
}