// ����һ���� UnityEngine.Debug ����������� System.Diagnostics.Debug ���ص�
using UnityEngine;
using System.Diagnostics;
using UDebug = UnityEngine.Debug;

public static class DevDebug
{
    // ֻ���� Editor ģʽ�� Development Build ʱ����������ĵ��òŻᱻ����
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
