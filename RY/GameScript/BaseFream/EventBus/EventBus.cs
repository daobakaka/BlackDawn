using System;
using System.Collections.Generic;
using GameFrame.BaseClass;  

namespace GameFrame.EventBus
{
    /// <summary>
    /// 类型安全的事件总线，单例模式实现
    /// 通过 EventBus.GetInstance() 调用订阅/发布/退订方法
    /// </summary>
    public class EventBus : Singleton<EventBus>
    {
        // 存储各事件类型对应的处理器列表
        private readonly Dictionary<Type, List<Delegate>> _handlers
            = new Dictionary<Type, List<Delegate>>();



        private EventBus() { }
        /// <summary>
        /// 订阅某种事件
        /// </summary>
        public void Subscribe<T>(Action<T> handler)
        {
            var key = typeof(T);
            if (!_handlers.TryGetValue(key, out var list))
            {
                list = new List<Delegate>();
                _handlers[key] = list;
            }

            if (!list.Contains(handler))
                list.Add(handler);
        }

        /// <summary>
        /// 退订某种事件
        /// </summary>
        public void Unsubscribe<T>(Action<T> handler)
        {
            var key = typeof(T);
            if (_handlers.TryGetValue(key, out var list))
            {
                list.Remove(handler);
                if (list.Count == 0)
                    _handlers.Remove(key);
            }
        }

        /// <summary>
        /// 发布某种事件，所有订阅者都会收到
        /// </summary>
        public void Publish<T>(T evt)
        {
            var key = typeof(T);
            if (_handlers.TryGetValue(key, out var list))
            {
                // 复制一份，防止回调中修改列表导致错误
                var invocationList = list.ToArray();
                foreach (var del in invocationList)
                {
                    try
                    {
                        ((Action<T>)del)?.Invoke(evt);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError(
                            $"EventBus.Publish<{key.Name}> handler threw: {ex}");
                    }
                }
            }
        }
    }
}
