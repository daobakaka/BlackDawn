// 方法一：给 UnityEngine.Debug 起个别名，把 System.Diagnostics.Debug 隐藏掉
using UnityEngine;
using System.Diagnostics;
using UDebug = UnityEngine.Debug;

public static class DevDebug
{
    // 只有在 Editor 模式或 Development Build 时，这个方法的调用才会被保留
    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message)
    {
        UDebug.Log(message);
    }

    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(object message)
    {
        UDebug.LogWarning(message);
    }

    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(object message)
    {
        UDebug.LogError(message);
    }
}
