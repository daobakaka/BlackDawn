namespace GameFrame.Runtime
{
    /// <summary>
    /// 输入代码
    /// </summary>
    public enum InputCode: byte
    {
        /// <summary>
        /// 上
        /// </summary>
        Up = 0,
        
        /// <summary>
        /// 下
        /// </summary>
        Down = 1,
        
        /// <summary>
        /// 左
        /// </summary>
        Left = 2,
        
        /// <summary>
        /// 右
        /// </summary>
        Right = 3,
        
        /// <summary>
        /// 确定
        /// </summary>
        Sure = 4,
        
        /// <summary>
        /// 取消
        /// </summary>
        Cancel = 5,
        
        /// <summary>
        /// 返回
        /// </summary>
        Back = 6,
        
        /// <summary>
        /// 触屏
        /// </summary>
        Touch = 7,
        
        /// <summary>
        /// 鼠标左键
        /// </summary>
        Mouse0 = 8,
        
        /// <summary>
        /// 鼠标右键
        /// </summary>
        Mouse1 = 9,
        
        /// <summary>
        /// Shift 键
        /// </summary>
        Shift,
        /// <summary>
        /// 滚轮
        /// </summary>
        MouseWheel,
        /// <summary>
        /// 空格
        /// </summary>
        Space,

        /// <summary>
        /// 最大
        /// </summary>
        Max,

        检测,
        
        /// <summary>
        /// 无效
        /// </summary>
        Invalid = 255
    }
}