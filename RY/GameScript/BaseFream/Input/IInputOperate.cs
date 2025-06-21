using System;
using UnityEngine;

namespace GameFrame.Runtime
{
    public interface IInputOperate
    {
        /// <summary>
        /// 上键事件
        /// </summary>
        Action onUp 
        {
            set;
        }
        
        /// <summary>
        /// 下键事件
        /// </summary>
        Action onDown
        {
            set;
        }
        
        /// <summary>
        /// 左键事件
        /// </summary>
        Action onLeft
        {
            set;
        }
        
        /// <summary>
        /// 右键事件
        /// </summary>
        Action onRight
        {
            set;
        }
        
        /// <summary>
        /// 按Shift
        /// </summary>
        Action onShift
        {
            set;
        }


        /// <summary>
        /// 按Spaces
        /// </summary>
        Action onSpace
        {
            set;
        }

        /// <summary>
        /// 确认事件
        /// </summary>
        Action onSure
        {
            set;
        }
        
        /// <summary>
        /// 取消事件
        /// </summary>
        Action onCancel
        {
            set;
        }
        
        /// <summary>
        /// 鼠标左键事件
        /// </summary>
        Action<Vector2> onMouse0
        {
            set;
        }
        
        /// <summary>
        /// 鼠标左键事件
        /// </summary>
        Action<Vector2> onMouse1
        {
            set;
        }
        
        /// <summary>
        /// 触摸事件
        /// </summary>
        Action<Vector2> onTouch
        {
            set;
        }
        
        
        /// <summary>
        /// 上键抬起事件
        /// </summary>
        Action onUpCancel
        {
            set;
        }
        
        /// <summary>
        /// 下键抬起事件
        /// </summary>
        Action onDownCancel
        {
            set;
        }
        
        /// <summary>
        /// 左键抬起事件
        /// </summary>
        Action onLeftCancel
        {
            set;
        }
        
        /// <summary>
        /// 右键抬起事件
        /// </summary>
        Action onRightCancel
        {
            set;
        }
        
        /// <summary>
        /// Shift抬起
        /// </summary>
        Action onShiftCancel
        {
            set;
        }
        
        /// <summary>
        /// 确认事件抬起
        /// </summary>
        Action onSureCancel
        {
            set;
        }
        
        /// <summary>
        /// 取消事件抬起
        /// </summary>
        Action onCancelCancel
        {
            set;
        }

        // Action on检测
        // {
        //     set;
        // }
        
        /// <summary>
        /// 鼠标左键抬起事件
        /// </summary>
        Action<Vector2> onMouse0Cancel
        {
            set;
        }
        
        /// <summary>
        /// 鼠标右键抬起事件
        /// </summary>
        Action<Vector2> onMouse1Cancel
        {
            set;
        }
        

        /// <summary>
        /// 长按上键事件
        /// </summary>
        Action<float, float> onUpHold
        {
            set;
        }
        
        /// <summary>
        /// 长按上键事件
        /// </summary>
        Action<float, float> onDownHold
        {
            set;
        }
        
        /// <summary>
        /// 长按左键事件
        /// </summary>
        Action<float, float> onLeftHold
        {
            set;
        }
        
        /// <summary>
        /// 长按右键事件
        /// </summary>
        Action<float, float> onRightHold
        {
            set;
        }

        /// <summary>
        /// 长按Shift
        /// </summary>
        Action<float, float> onShiftHold
        {
            set;
        }
        
        /// <summary>
        /// 长按确认事件
        /// </summary>
        Action<float, float> onSureHold
        {
            set;
        }
        
        /// <summary>
        /// 长按取消事件
        /// </summary>
        Action<float, float> onCancelHold
        {
            set;
        }
        
        /// <summary>
        /// 长按鼠标左键事件
        /// </summary>
        Action<float, float> onMouse0Hold
        {
            set;
        }
        
        /// <summary>
        /// 长按鼠标右键事件
        /// </summary>
        Action<float, float> onMouse1Hold
        {
            set;
        }
        
        /// <summary>
        /// 长按触摸事件
        /// </summary>
        Action<float, float> onTouchHold
        {
            set;
        }
        
        /// <summary>
        /// 移动事件
        /// </summary>
        Action<Vector2> onMove
        {
            set;
        }

        /// <summary>
        /// 移动事件
        /// </summary>
        Action<InputCode> onMoveCancel
        {
            set;
        }

        /// <summary>
        /// 鼠标滚轮滚动事件，上下滚动增量（正值向上，负值向下） 
        /// </summary>
        Action<float> onMouseWheel
        {
            set;
        }




        /// <summary>
        /// 设置间隔时间
        /// </summary>
        IInputOperate SetInterval(InputCode code, float interval);
        
        /// <summary>
        /// 设置长按的触发时间
        /// </summary>
        /// <param name="code"></param>
        /// <param name="holdTime"></param>
        /// <returns></returns>
        IInputOperate SetHoldTime(InputCode code, float holdTime);
        
        /// <summary>
        /// 设置操作标志
        /// </summary>
        IInputOperate SetFlag(int flag);
        
        /// <summary>
        /// 设置是否忽略时间缩放
        /// </summary>
        /// <param name="ignore"></param>
        /// <returns></returns>
        IInputOperate SetIgnoreTimescale(bool ignore);
        
        /// <summary>
        /// 设置是否是页面根节点
        /// </summary>
        /// <param name="isRoot"></param>
        /// <returns></returns>
        IInputOperate SetPageRoot(bool isRoot);
        
        /// <summary>
        /// 设置组合键
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="interval"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        IInputOperate SetCombination(string keys,float interval,Action action);
        
        /// <summary>
        /// 设置是否继承操作
        /// </summary>
        /// <param name="isContinue"></param>
        /// <returns></returns>
        IInputOperate SetContinueOperate(bool isContinue);
    }
}