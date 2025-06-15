using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


namespace GameFrame.BaseClass
{
    /// <summary>
    /// 通用单例基类，使用 GetInstance 方法获取实例；
    /// 要求 T 声明一个非 public 的无参构造函数（protected 或 private）。
    /// </summary>
    public abstract class Singleton<T> where T : class
    {
        // 延迟、线程安全初始化
        private static readonly Lazy<T> _instance = new Lazy<T>(CreateInstance);

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static T GetInstance()
        {
            return _instance.Value;
        }

        
        private static T CreateInstance()
        {
            // 手动查找所有非 public 的构造函数
            ConstructorInfo[] ctors = typeof(T)
                .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
            ConstructorInfo parameterlessCtor = null;

            foreach (var ctor in ctors)
            {
                if (ctor.GetParameters().Length == 0)
                {
                    parameterlessCtor = ctor;
                    break;
                }
            }

            if (parameterlessCtor == null)
                throw new InvalidOperationException(
                    $"Type {typeof(T)} must have a non-public parameterless constructor");

            // 调用非 public 构造函数创建实例
            return (T)parameterlessCtor.Invoke(null);
        }

        // 防止子类或外部通过其他方式实例化
        protected Singleton() { }
    }
}