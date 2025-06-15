using System;

namespace GameFrame
{
    /// <summary>
    /// 变量。
    /// </summary>
    /// <typeparam name="T">变量类型。</typeparam>
    public class Variable<T> : Variable
    {
        private T _value;

        public Variable()
        {
        }

        /// <summary>
        /// 获取变量类型。
        /// </summary>
        public override Type Type => typeof(T);
    

        /// <summary>
        /// 获取或设置变量值。
        /// </summary>
        public T Value
        {
            get => _value;
            
            set => _value = value;
        }

        /// <summary>
        /// 获取变量值。
        /// </summary>
        /// <returns>变量值。</returns>
        public override TDate GetValue<TDate>()
        {
            if (Value is TDate val)
                return val;
            if (null == Value)
                return default;

            throw new Exception("Variable value is not of type " + typeof(TDate).Name);
        } 
       

        /// <summary>
        /// 设置变量值。
        /// </summary>
        /// <param name="value">变量值。</param>
        public override void SetValue<TDate>(TDate value)
        {
            if (value is T val)
                _value = val;
        }

        /// <summary>
        /// 重置变量值。
        /// </summary>
        public override void Reset()
        {
            _value = default(T);
        }

        /// <summary>
        /// 获取变量字符串。
        /// </summary>
        /// <returns>变量字符串。</returns>
        public override string ToString()
        {
            return (_value != null) ? _value.ToString() : "<Null>";
        }

        public override void Clear()
        {
            _value = default;
        }

    }
}
