using System;
using System.ComponentModel;

namespace GameFrame
{
    /// <summary>
    /// 变量。
    /// </summary>
    public abstract class Variable 
    {
        /// <summary>
        /// 获取变量类型。
        /// </summary>
        public abstract Type Type
        {
            get;
        }

        /// <summary>
        /// 获取变量值。
        /// </summary>
        /// <returns>变量值。</returns>
        public abstract TDate GetValue<TDate>();

        /// <summary>
        /// 设置变量值。
        /// </summary>
        /// <param name="value">变量值。</param>
        public abstract void SetValue<TDate>(TDate value);

        /// <summary>
        /// 重置变量值。
        /// </summary>
        public abstract void Reset();

        public abstract void Clear();

    }
}