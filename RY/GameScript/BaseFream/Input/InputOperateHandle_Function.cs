using System;
using System.Collections.Generic;
//using GameFrame.Save;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameFrame.Runtime
{
    public partial class InputOperateHandle : MonoBehaviour
    {
        protected void Awake()
        {
            
            _inputCodeMap = new()
            {
                // WASD 的按键初始映射
                { "/Keyboard/w", InputCode.Up },
                { "/Keyboard/s", InputCode.Down },
                { "/Keyboard/a", InputCode.Left },
                { "/Keyboard/d", InputCode.Right },
                { "/Keyboard/upArrow", InputCode.Up },
                { "/Keyboard/downArrow", InputCode.Down },
                { "/Keyboard/leftArrow", InputCode.Left },
                { "/Keyboard/rightArrow", InputCode.Right },
                { "/Keyboard/Space", InputCode.Right },
            
                // { "Back", InputCode.Back },
                // { "Touch", InputCode.Touch },
                { "/Mouse/leftButton", InputCode.Mouse0 },
                { "/Mouse/rightButton", InputCode.Mouse1 },
                { "/Mouse/scroll",InputCode.MouseWheel}
            };

            _actions = new()
            {
                { InputCode.Up, null },
                { InputCode.Down, null },
                { InputCode.Left, null },
                { InputCode.Right, null },
                { InputCode.Mouse0, null },
                { InputCode.Mouse1, null },
                { InputCode.Shift, null },
                { InputCode.Sure, null },
                { InputCode.Cancel, null },
                { InputCode.Back, null },
                { InputCode.Touch, null },
                { InputCode.MouseWheel,null},
                { InputCode.Space,null},
            };

            _actionsCancel = new()
            {
                { InputCode.Up, null },
                { InputCode.Down, null },
                { InputCode.Left, null },
                { InputCode.Right, null },
                { InputCode.Mouse0, null },
                { InputCode.Mouse1, null },
                { InputCode.Shift, null },
                { InputCode.Sure, null },
                { InputCode.Cancel, null },
                { InputCode.Back, null },
                { InputCode.Touch, null },
                { InputCode.MouseWheel,null},
                { InputCode.Space,null},
            };

            // 初始化输入轴
            _inputInfo.axis = Vector2.zero;

            _input = GetComponent<PlayerInput>();
            
            // 操作栈
            _operateStack = new Stack<InputOperate>();
            
            // 获取名为InputHandle的Input Action Map
            var inputMap = _input.actions.FindActionMap("InputHandle");
            
            // 获取名为Move的Input Action
            _move = inputMap.FindAction("Move");
            
            // 获取名为W的Input Action
            _w = inputMap.FindAction("W");
            
            // 获取名为A的Input Action
            _a = inputMap.FindAction("A");
            
            // 获取名为S的Input Action
            _s = inputMap.FindAction("S");

            // 获取名为D的Input Action
            _d = inputMap.FindAction("D");
            
            // 获取名为Shift的Input Action
            _shift = inputMap.FindAction("Shift");
            
            // 获取名为Sure的Input Action
            _sure = inputMap.FindAction("Sure");
            
            // 获取名为Cancel的Input Action
            _cancel = inputMap.FindAction("Cancel");
            
            // 获取名为Click的Input Action
            _click = inputMap.FindAction("Click");
            
            // 获取名为Touch的Input Action
            _touch = inputMap.FindAction("Touch");

            //获取名为MouseWheel 的input Action
            _mouseWheel = inputMap.FindAction("MouseWheel");

            //获取名为 Space的键盘Input Action
            _space = inputMap.FindAction("Space");



            _new = inputMap.FindAction("按键检测");
            
            _recordBinding = new()
            {
                { _w.name, new() },
                { _a.name, new() },
                { _s.name, new() },
                { _d.name, new() },
                { _shift.name, new() },
                { _sure.name, new() },
                { _cancel.name, new() },
                { _click.name, new() },
                { _space.name, new() },
            };

            // 监听移动事件
            _move.performed += OnMoveStart;
            _move.canceled += OnMoveCancel;
            
            // 监听W事件
            _w.started += OnWasdStarted;
            _w.canceled += OnWasdCanceled;

            // 监听A事件
            _a.started += OnWasdStarted;
            _a.canceled += OnWasdCanceled;
            
            // 监听AD事件
            _s.started += OnWasdStarted;
            _s.canceled += OnWasdCanceled;
            
            // 监听AD事件
            _d.started += OnWasdStarted;
            _d.canceled += OnWasdCanceled;
            
            // 监听Shift事件
            _shift.started += OnShiftStarted;
            _shift.canceled += OnShiftCancel;
            
            // 监听Click事件
            _click.started += OnClickStarted;
            _click.canceled += OnClickCanceled;
            
            // 监听Sure事件
            _sure.started += OnSureStarted;
            _sure.canceled += OnSureCanceled;
            
            // 监听Cancel事件
            _cancel.started += OnCanceledStarted;
            _cancel.canceled += OnCanceled;
            
            // 监听Touch事件
            _touch.started += OnTouchStarted;
            _touch.canceled += OnTouchCanceled;
            //监听鼠标滚轮？
            _mouseWheel.performed += OnMouseWheel;

            //监听Space 事件
            _space.started += OnSpaceStarted;
            _space.canceled += OnSpaceCancel;
            // _new.started += (input) => {
            //     if (null == _operate) return;
            //     Invoke(InputCode.检测);
            //  };
            // _new.performed += (input) => { };
            // _new.canceled += (input) => { };




            // 监听Any事件
            Keyboard.current.onTextInput += OnAnyInput;
            DontDestroyOnLoad(this);
            // 监听SaveLoadSuccess事件
            //GEvent.AddListener<ISaveManager>("SaveLoadSuccess",OnSaveLoadSuccess);
        }
        

        private void Update()
        { 
            if (null == _operate || _inputInfo.code == InputCode.Invalid) 
                return;
            
            IntervalTriggering();

            HoldTriggering();
        }

        private void HoldTriggering()
        {
            var holds = _inputInfo.holds;

            for (var code = InputCode.Up; code < InputCode.Max; code++)
            {
                var hold = holds[(int)code];
                float holdTime = _operate.GetHoldTime(code);
                if (!hold.started || holdTime <= 0) continue;
                
                float time = _operate.IsIgnoreTimescale ? Time.unscaledDeltaTime : Time.deltaTime;
                _inputInfo.timer += time;

                hold.timer = Mathf.Clamp(hold.timer + time,0f,holdTime);
                
                // 触发长按事件
                _operate.Hold(code)?.Invoke(hold.timer / holdTime, hold.timer);

                // 触发完成事件
                if (hold.timer >= holdTime)
                {
                    _operate.Invoke(code);
                    // 重置(无效化)
                    hold.Reset();
                }
            }
        }
        
        /// <summary>
        /// 间隔触发
        /// </summary>
        private void IntervalTriggering()
        {
            var interval = _operate.GetInInterval(_inputInfo.code);

            if (interval < 0) return;
           
            if (_inputInfo.holds[(int)_inputInfo.code].timer > 0) return;
            
            float time = _operate.IsIgnoreTimescale ? Time.unscaledDeltaTime : Time.deltaTime;
            _inputInfo.timer += time;

            if (_inputInfo.timer >= interval)
            {
               _inputInfo.timer = 0; _new.performed += (input) => { };
               _operate.Invoke(_inputInfo.code);
            }
        }

        /// <summary>
        /// 创建输入操作
        /// </summary>
        /// <param name="isMain"></param>
        /// <returns></returns>
        public static IInputOperate CreateOperate(bool isMain = false)
        {
            return new InputOperate(isMain);
        }

        
        // TODO : 等待实现
        /// <summary>
        /// 重置输入按键
        /// </summary>
        public void ResetInputKey()
        {
            
        }
        
        
        /// <summary>
        /// 重新绑定操作按键
        /// </summary>
        /// <param name="input">输入事件</param>
        /// <param name="index">修改下标</param>
        /// <param name="ignores">忽略设备</param>
        private void OperateReset(InputAction input, int index,string[] ignores = null)
        {
            input.Disable();
         
            // 如果是移动时间 path 后面添加 _move ： oldPath += "_move";
            var oldPath = input.controls[index - 1].path;
            var oldCode = InputCode.Invalid;
            if (_inputCodeMap.TryGetValue(oldPath, out var value))
                oldCode = value;
            
#if UNITY_EDITOR
            Debug.Log($"开始重新绑定绑定，当前：{oldPath}");;
#endif
                
            // 禁用输入操作
            input.Disable();
     
            // 开始重绑定
            _operateRest = input.PerformInteractiveRebinding()
                .WithTargetBinding(index) // 指定要重绑定的控件的索引
                .OnComplete(operate =>
                {
                    // 记录绑定
                    _recordBinding[input.name][index] = input.bindings[index].path;
                    
                    var newPath = input.controls[index - 1].path;

#if UNITY_EDITOR
                    Debug.Log($"重绑定完成，新绑定路径：{newPath}");;
#endif

                    if (oldCode != InputCode.Invalid)
                    {
                        // 如果是移动时间 path 后面添加 _move ： newPath += "_move";
                        _inputCodeMap[newPath] = oldCode;

                        // 移除旧映射
                        _inputCodeMap.Remove(oldPath);
                    }

                    // 完成后清理重绑定操作对象
                    operate.Dispose();

                    // 重绑定完成后重新启用
                    input.Enable();

                    SaveInputCodeMap(input);
                });
            
            // 排除不需要的控件
            if (ignores != null)
            {
                //可筛选的控件范围：只要控件在 Input System 中有定义并能被匹配到
                //（如 "Mouse"、"Keyboard"、"Gamepad"、"Joystick"、"Touchscreen"、"XRController" 等），
                //都可以通过 WithControlsExcluding 排除。
                foreach (var ignore in ignores)
                    _operateRest.WithControlsExcluding(ignore);
            }
            
            _operateRest.Start();
        }
        
        /// <summary>
        /// 取消重绑定
        /// </summary>
        public static void OperateResetCancle()
        {
            _operateRest?.Cancel();
#if UNITY_EDITOR
            Debug.Log("取消重新重绑定按键");
#endif
        }
        
        /// <summary>
        /// 保存输入操作码映射
        /// </summary>
        /// <param name="input"></param>
        private void SaveInputCodeMap(InputAction input)
        {
            // var save = GameEntry.GetComponent<SaveComponent>();
            // save.SetObject("InputCodeMap", _inputCodeMap);
            // save.SetObject(input.name, input);
        }

        /// <summary>
        /// 入栈输入操作
        /// </summary>
        /// <param name="operate"></param>
        public static void PushOperate(IInputOperate operate)
        {
            if (operate is not InputOperate inputOperate)
                throw new Exception("operate is null");

            if (_operate != null && _operate != operate)
                _operateStack.Push(_operate);
            
            _operate = inputOperate;
            
            if (_operate.ContinueOperate)
            {
                if (_inputInfo.axis != Vector2.zero)
                    _operate.onMove?.Invoke(_inputInfo.axis);

                foreach (var code in _inputInfo.codeStack)
                    _operate.Invoke(code);
            }

            // 重置输入信息
            _inputInfo.Reset();
        }
        
        /// <summary>
        /// 出栈输入操作
        /// </summary>
        /// <param name="closePage"></param>
        public static void PopOperate(bool closePage = false)
        {
            // 如果是主操作，则不执行
            if (null == _operate || _operate.IsMain) return;

            if (closePage)
                BackPageRoot();
            else
                _inputInfo.Reset();
            
            _operate = _operateStack.Count > 0 ? _operateStack.Pop() : null;
        }
        
        /// <summary>
        /// 返回页面根节点
        /// </summary>
        public static void BackPageRoot()
        {
            if (null == _operate || _operate.IsMain)
            {
                Debug.LogWarning("当前操作为主操作，无法返回!");
                return;
            }

            while (!_operate.IsPageRoot && _operateStack.Count > 0)
                _operate = _operateStack.Pop();
            
            // 重置输入信息
            _inputInfo.Reset();
        }
        
        /// <summary>
        /// 返回主操作
        /// </summary>
        public static void BackToMain()
        {
            while (_operateStack.Count > 0)
                _operate = _operateStack.Pop();
            
            // 重置输入信息
            _inputInfo.Reset();
        }
        
        /// <summary>
        /// 触发输入操作
        /// </summary>
        /// <param name="code"></param>
        void Invoke(InputCode code)
        {
// #if UNITY_EDITOR
//             Log.Debug($"{code} 按下");
// #endif
            if (code == InputCode.Invalid) return;

            _inputInfo.code = code;
            _inputInfo.timer = 0;
            
            if (!_inputInfo.codeStack.Contains(code))
                _inputInfo.codeStack.Add(code);
            
            if (_operate.GetHoldTime(code) > 0)
            {
                _inputInfo.HoldBegin(code);
                return;
            }

            _actions.TryGetValue(code,  out var action);
            action?.Invoke();

            _operate.Invoke(code);
        }
        
        /// <summary>
        /// 释放输入操作
        /// </summary>
        /// <param name="code"></param>
        void Release(InputCode code)
        {
// #if UNITY_EDITOR
//             Log.Debug($"{code} 抬起");
// #endif
            _inputInfo.HoldClear(code);
            _inputInfo.timer = 0;
            
            if (_inputInfo.codeStack.Contains(code))
                _inputInfo.codeStack.Remove(code);

            if (_inputInfo.codeStack.Count == 0)
                _inputInfo.code = InputCode.Invalid;
        }
        
        /// <summary>
        /// 点击开始
        /// </summary>
        /// <param name="context"></param>
        private void OnClickStarted(InputAction.CallbackContext context)
        {
            if (_operate == null) return;
            
            if (!_inputCodeMap.TryGetValue(context.control.path, out var code))
                return;
            
            Invoke(code);
        }
        
        /// <summary>
        /// 点击取消
        /// </summary>
        /// <param name="context"></param>
        private void OnClickCanceled(InputAction.CallbackContext context)
        {
            //if (GameEntry.IsMobile) return;

            if (_operate == null) return;

            if (!_inputCodeMap.TryGetValue(context.control.path, out var code))
                return;
            
            Release(code);
            _actionsCancel.TryGetValue(code,  out var action);
            action?.Invoke();
            _operate.InvokeCancel(code);

        }
        
        /// <summary>
        /// 触摸开始
        /// </summary>
        /// <param name="context"></param>
        private void OnTouchStarted(InputAction.CallbackContext context)
        {
            Invoke(InputCode.Touch);
        }
        /// <summary>
        ///执行鼠标滚轮滚动
        /// </summary>
        /// <param name="ctx"></param>
        private void OnMouseWheel(InputAction.CallbackContext ctx)
        {
            //if (null == _operate) return;
            //Invoke(InputCode.MouseWheel);


            if (_operate == null) return;

            Vector2 scroll = ctx.ReadValue<Vector2>();
            float delta = scroll.y;
            if (Mathf.Approximately(delta, 0f)) return;

            // 触发“开始滚动”回调
            _operate.onMouseWheel?.Invoke(delta);

        }

        /// <summary>
        /// 触摸取消
        /// </summary>
        /// <param name="context"></param>
        private void OnTouchCanceled(InputAction.CallbackContext context)
        {
            Release(InputCode.Touch);
        }

        private InputCode WASD(Vector2 val)
        {
            InputCode code = InputCode.Invalid;
            
            if (val.x > 0)
                code = InputCode.Right;
            else if (val.x < 0)
                code = InputCode.Left;
            else if (val.y > 0)
                code = InputCode.Up;
            else if (val.y < 0)
                code = InputCode.Down;
            
            return code;
        }
        
        /// <summary>
        /// WASD按下
        /// </summary>
        /// <param name="context"></param>
        private void OnWasdStarted(InputAction.CallbackContext context)
        {
            if (null == _operate)
                return;
            
            if (!_inputCodeMap.TryGetValue(context.control.path, out var code))
                return;
            
            Invoke(code);
        }
        
        
        /// <summary>
        /// WASD取消
        /// </summary>
        /// <param name="context"></param>
        private void OnWasdCanceled(InputAction.CallbackContext context)
        {
            if (null == _operate)
                return;
           
            if (!_inputCodeMap.TryGetValue(context.control.path, out var code))
                return;
            
            Release(code);
            _actionsCancel.TryGetValue(code,  out var action);
            action?.Invoke();
            _operate.InvokeCancel(code);
        }
        
        /// <summary>
        /// 移动开始
        /// </summary>
        /// <param name="context"></param>
        private void OnMoveStart(InputAction.CallbackContext context)
        {
            if (null == _operate) return;

            _inputInfo.axis = context.ReadValue<Vector2>();

            _operate.onMove?.Invoke(_inputInfo.axis);
        }
        
        /// <summary>
        /// 移动取消
        /// </summary>
        /// <param name="context"></param>
        private void OnMoveCancel(InputAction.CallbackContext context)
        {
            if (null == _operate) return;

            _inputInfo.axis = Vector2.zero;

            _operate.onMove?.Invoke(_inputInfo.axis);
        }


        /// <summary>
        /// 确定事件
        /// </summary>
        /// <param name="context"></param>
        private void OnSureStarted(InputAction.CallbackContext context)
        {
            if (null == _operate) return;
            
            Invoke(InputCode.Sure);
        }
        
        /// <summary>
        /// 确定事件取消
        /// </summary>
        /// <param name="context"></param>
        private void OnSureCanceled(InputAction.CallbackContext context)
        {
            if (null == _operate) return;
            
            Release(InputCode.Sure);
            _actionsCancel.TryGetValue(InputCode.Sure,  out var action);
            action?.Invoke();
            _operate.InvokeCancel(InputCode.Sure);
        }
        
        /// <summary>
        /// 取消事件
        /// </summary>
        private void OnCanceledStarted(InputAction.CallbackContext context)
        {
            if (null == _operate) return;
            
            Invoke(InputCode.Cancel);
        }
        
        /// <summary>
        /// 取消事件取消
        /// </summary>
        private void OnCanceled(InputAction.CallbackContext context)
        {
            if (null == _operate) return;
            
            Release(InputCode.Cancel);
            _actionsCancel.TryGetValue(InputCode.Cancel,  out var action);
            action?.Invoke();
            _operate.InvokeCancel(InputCode.Cancel);
        }
        
        /// <summary>
        /// Shift事件
        /// </summary>
        private void OnShiftStarted(InputAction.CallbackContext context)
        {
            if (null == _operate) return;
            
            Invoke(InputCode.Shift);
        }
        
        /// <summary>
        /// 取消Shift事件
        /// </summary>
        private void OnShiftCancel(InputAction.CallbackContext context)
        {
            if (null == _operate) return;
            
            Release(InputCode.Shift);
            _actionsCancel.TryGetValue(InputCode.Shift,  out var action);
            action?.Invoke();
            _operate.InvokeCancel(InputCode.Shift);
        }


        /// <summary>
        /// Space事件
        /// </summary>
        private void OnSpaceStarted(InputAction.CallbackContext context)
        {
            if (null == _operate) return;

            Invoke(InputCode.Space);
        }

        /// <summary>
        /// 取消Shift事件
        /// </summary>
        private void OnSpaceCancel(InputAction.CallbackContext context)
        {
            if (null == _operate) return;

            Release(InputCode.Space);
            _actionsCancel.TryGetValue(InputCode.Space, out var action);
            action?.Invoke();
            _operate.InvokeCancel(InputCode.Space);
        }

        /// <summary>
        /// 添加全局输入事件
        /// </summary>
        /// <param name="code"></param>
        /// <param name="action"></param>
        public static void TriggerListener(InputCode code, Action action)
        {
            if (_actions.ContainsKey(code))
                _actions[code] += action;
            else
                Debug.LogWarning($"{code}事件未注册");
        }
    
        /// <summary>
        /// 移除全局输入事件
        /// </summary>
        /// <param name="code"></param>
        /// <param name="action"></param>
        public static void TriggerRemove(InputCode code, Action action)
        {
            if (_actions.ContainsKey(code))
                _actions[code] -= action;
        }
        
        /// <summary>
        /// 添加抬起全局输入事件
        /// </summary>
        /// <param name="code"></param>
        /// <param name="action"></param>
        public static void CancelListener(InputCode code, Action action)
        {
            if (_actionsCancel.ContainsKey(code))
                _actionsCancel[code] += action;
            else
                Debug.LogWarning($"{code}事件未注册");
        }
        
        /// <summary>
        /// 移除抬起全局输入事件
        /// </summary>
        /// <param name="code"></param>
        /// <param name="action"></param>
        public static void CancelRemove(InputCode code, Action action)
        {
            if (_actionsCancel.ContainsKey(code))
                _actionsCancel[code] -= action;
        }
        
        /// <summary>
        /// 判断检测的是输入是否全部持续按住
        /// </summary>
        /// <param name="codes"></param>
        /// <returns></returns>
        public static bool IsHoldDown(params InputCode[] codes)
        {
            foreach (var code in codes)
            {
                if (!_inputInfo.codeStack.Contains(code))
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 判断检测的是输入是否有任何一个持续按住
        /// </summary>
        /// <param name="codes"></param>
        /// <returns></returns>
        public static bool AnyHoldDown(params InputCode[] codes)
        {
            foreach (var code in codes)
            {
                if (_inputInfo.codeStack.Contains(code))
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Any事件
        /// </summary>
        private void OnAnyInput(char inputChar)
        {
            if (null == _operate) return;

            _operate.TryInvokeCombination(inputChar);
        }
        
        /// <summary>
        /// 游戏存档数据加成成功时重置按键映射
        /// </summary>
        /// <param name="saveManager"></param>
        // private void OnSaveLoadSuccess(ISaveManager saveManager)
        // {
        //     _inputCodeMap = saveManager.GetObject("InputCodeMap", _inputCodeMap);
        //     _recordBinding = saveManager.GetObject("InputRecordBinding", _recordBinding);

        //     var list = new List<InputAction>()
        //     {
        //         _w,
        //         _a,
        //         _s,
        //         _d,
        //         _shift,
        //         _sure,
        //         _cancel,
        //         _click
        //     };
            
        //     // 重置按键绑定
        //     foreach (var inputAction in _recordBinding)
        //     {
        //         var input = list.Find(x => x.name == inputAction.Key);
        //         if (input == null)
        //             return;

        //         foreach (var bind in inputAction.Value)
        //             input.ApplyBindingOverride(bind.Key, bind.Value);
        //     }
        // }
        
        // public static void TestSave()
        // {
        //     // // 测试
        //     // _inputCodeMap.Remove(_w.controls[0].path);
        //     // _inputCodeMap.Remove(_w.controls[2].path);
        //     // _recordBinding[_w.name][1] = "<Keyboard>/x";
        //     // _recordBinding[_w.name][3] = "<Keyboard>/z";
        //     // _inputCodeMap[_w.controls[0].path] = InputCode.Up;
        //     // _inputCodeMap[_w.controls[2].path] = InputCode.Down;
            
        //     var save = GameEntry.GetComponent<SaveComponent>();
        //     save.SetObject("InputCodeMap", _inputCodeMap);
        //     save.SetObject("InputRecordBinding", _recordBinding);
            
        // }
    }
}