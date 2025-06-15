using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using GameFrame.Runtime;
using Unity.Collections;

namespace GameFrame.Runtime
{

    public static class Extend
    {

        public static void Recycle(this IResettable self)
        {
            self.Reset();
            MonoPool.Instance.Recycle(self);
        }

    }



    public static class List
    {
        private static ConcurrentDictionary<Type, object> empty = new();


        // 获取一个空的List
        public static List<T>? Empty<T>()
        {
            var list = empty.GetOrAdd(typeof(T), _ => new List<T>()) as List<T>;
            list?.Clear();
            return list;
        }

        // 获取一个List
        public static List<T> Fetch<T>()
        {
            return MonoPool.Instance.Fetch<List<T>>();
        }

        // 回收一个List
        public static void Recycle<T>(this List<T> list)
        {
            list.Clear();
            MonoPool.Instance.Recycle(list);
        }
    }

    public static class Dictionary
    {
        public static Dictionary<TKey, TValue> Fetch<TKey, TValue>()
        {
            return MonoPool.Instance.Fetch<Dictionary<TKey, TValue>>();
        }

        public static void Recycle<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
        {
            dictionary.Clear();
            MonoPool.Instance.Recycle(dictionary);
        }
    }

}