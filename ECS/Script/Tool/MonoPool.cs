using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace GameFrame.Runtime
{
    /// <summary>
    /// 对象池中可重置的接口
    /// </summary>
    public interface IResettable
    {
        void Reset();
        
        public static T Fetch<T>() where T : IResettable, new()
        {
            return MonoPool.Instance.Fetch<T>();
        }
    }

    /// <summary>
    /// 简单的 LRU 缓存实现
    /// </summary>
    public class LRUCache<TKey, TValue>
    {
        private int capacity;
        public LRUCache(int capacity)
        {
            this.capacity = capacity;
        }

        private readonly Dictionary<TKey, LinkedListNode<(TKey key, TValue value)>> _cache = new();
        private readonly LinkedList<(TKey key, TValue value)> _lruList = new();

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                // 移动到队首（最新使用）
                _lruList.Remove(node);
                _lruList.AddFirst(node);
                value = node.Value.value;
                return true;
            }
            value = default;
            return false;
        }

        public void Set(TKey key, TValue value)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                _lruList.Remove(node);
            }
            else if (_cache.Count >= capacity)
            {
                // 淘汰最近最少使用的
                var last = _lruList.Last;
                if (last != null)
                {
                    _cache.Remove(last.Value.key);
                    _lruList.RemoveLast();
                }
            }
            var newNode = new LinkedListNode<(TKey, TValue)>((key, value));
            _lruList.AddFirst(newNode);
            _cache[key] = newNode;
        }
    }

    public sealed class MonoPool : IDisposable
    {
        // 池使用并发字典管理，每个类型对应一个并发队列
        private readonly ConcurrentDictionary<Type, ConcurrentQueue<object>> _pool = new();
        private readonly ConcurrentDictionary<Type, int> _typeMaxPoolSizes = new();

        // 缓存构造器委托，减少反射调用开销
        private static readonly ConcurrentDictionary<Type, Func<object>> _constructorCache = new();

        // 延迟线程安全单例模式
        public static readonly Lazy<MonoPool> LazyInstance = new(() => new MonoPool());
        public static MonoPool Instance => LazyInstance.Value;

        private MonoPool() { }

        /// <summary>
        /// 设置指定类型池最大缓存数量
        /// </summary>
        public void SetMaxPoolSize(Type type, int maxSize)
        {
            if (maxSize < 0) 
                throw new ArgumentOutOfRangeException(nameof(maxSize));
            _typeMaxPoolSizes.AddOrUpdate(type, maxSize, (_, _) => maxSize);
        }

        /// <summary>
        /// 泛型版本获取对象，如果池中没有则新建
        /// </summary>
        public T Fetch<T>() where T : new()
        {
            var type = typeof(T);
            if (_pool.TryGetValue(type, out var queue) && queue.TryDequeue(out var obj))
                return (T)obj;
            // 使用缓存构造器创建对象
            return new T();
        }

        /// <summary>
        /// 根据类型获取对象
        /// </summary>
        public object Fetch(Type type)
        {
            if (_pool.TryGetValue(type, out var queue) && queue.TryDequeue(out var obj))
                return obj;

            return CreateInstance(type);
        }

        /// <summary>
        /// 回收对象到池中
        /// </summary>
        public void Recycle(object obj)
        {
            if (obj == null) 
                throw new ArgumentNullException(nameof(obj));

            var type = obj.GetType();
            var queue = _pool.GetOrAdd(type, _ => new ConcurrentQueue<object>());

            // 检查池容量限制，超出限制时直接舍弃对象
            if (_typeMaxPoolSizes.TryGetValue(type, out var maxSize) && queue.Count >= maxSize)
            {
                return;
            }
            
            if (queue.Contains(obj))
                return;
            
            queue.Enqueue(obj);
        }

        /// <summary>
        /// 清理池中所有对象，如果对象实现了 IDisposable，则调用 Dispose
        /// </summary>
        public void Dispose()
        {
            foreach (var queue in _pool.Values)
            {
                while (queue.TryDequeue(out var obj))
                {
                    if (obj is IDisposable disposable)
                        disposable.Dispose();
                }
            }
            _pool.Clear();
            _typeMaxPoolSizes.Clear();
        }

        /// <summary>
        /// 创建对象，并利用缓存的构造器提高性能
        /// </summary>
        private static object CreateInstance(Type type)
        {
            var activator = _constructorCache.GetOrAdd(type, t =>
            {
                try
                {
                    // 使用表达式树生成无参构造函数
                    var newExp = System.Linq.Expressions.Expression.New(t);
                    var lambda = System.Linq.Expressions.Expression.Lambda<Func<object>>(
                        System.Linq.Expressions.Expression.Convert(newExp, typeof(object))
                    );
                    return lambda.Compile();
                }
                catch (MissingMethodException ex)
                {
                    throw new InvalidOperationException($"Type {t} must have a parameterless constructor", ex);
                }
            });

            return activator();
        }
    }
}
