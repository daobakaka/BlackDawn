using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


namespace GameFrame.BaseClass
{
    /// <summary>
    /// ͨ�õ������࣬ʹ�� GetInstance ������ȡʵ����
    /// Ҫ�� T ����һ���� public ���޲ι��캯����protected �� private����
    /// </summary>
    public abstract class Singleton<T> where T : class
    {
        // �ӳ١��̰߳�ȫ��ʼ��
        private static readonly Lazy<T> _instance = new Lazy<T>(CreateInstance);

        /// <summary>
        /// ��ȡ����ʵ��
        /// </summary>
        public static T GetInstance()
        {
            return _instance.Value;
        }

        
        private static T CreateInstance()
        {
            // �ֶ��������з� public �Ĺ��캯��
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

            // ���÷� public ���캯������ʵ��
            return (T)parameterlessCtor.Invoke(null);
        }

        // ��ֹ������ⲿͨ��������ʽʵ����
        protected Singleton() { }
    }
}