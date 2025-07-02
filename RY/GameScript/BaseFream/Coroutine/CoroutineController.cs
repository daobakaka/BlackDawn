using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局协程控制器，支持启动、暂停、恢复、取消，并可按标签批量管理。
/// </summary>
public class CoroutineController : MonoBehaviour
{
    private class TaskInfo
    {
        public int Id;
        public IEnumerator Routine;
        public string Tag;
        public bool IsPaused;
        public bool IsCompleted;
        public Action OnComplete;
        public Coroutine CoroutineRef; // 协程句柄
    }

    private static CoroutineController _instance;
    public static CoroutineController instance => _instance;

    private Dictionary<int, TaskInfo> _tasks = new Dictionary<int, TaskInfo>();
    private int _nextId = 1;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    /// <summary>
    /// 启动一个受控制的协程
    /// </summary>
    /// <param name="routine">Coroutine 方法</param>
    /// <param name="tag">用于批量管理的标签（可选）</param>
    /// <param name="onComplete">完成回调（可选）</param>
    /// <returns>任务 ID</returns>
    public int StartRoutine(IEnumerator routine, string tag = null, Action onComplete = null)
    {
        int id = _nextId++;

        TaskInfo info = new TaskInfo
        {
            Id = id,
            Routine = routine,
            Tag = tag,
            OnComplete = onComplete,
            IsPaused = false,
            IsCompleted = false
        };

        // 包装器负责控制暂停逻辑与完成回调
        IEnumerator Wrapper()
        {
            while (!info.IsCompleted)
            {
                if (info.IsPaused)
                {
                    yield return null;
                    continue;
                }

                bool hasNext = false;
                try
                {
                    hasNext = info.Routine.MoveNext();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CoroutineController] Coroutine[{info.Id}] ({info.Tag}) error: {ex}");
                    break;
                }

                if (!hasNext)
                    break;

                yield return info.Routine.Current;
            }

            info.IsCompleted = true;
            info.OnComplete?.Invoke();
            _tasks.Remove(id);
        }

        info.CoroutineRef = StartCoroutine(Wrapper());
        _tasks[id] = info;
        Debug.Log($"[CoroutineController] Start Routine ID={id} Tag={tag}");
        return id;
    }

    /// <summary>暂停指定任务</summary>
    public void Pause(int id)
    {
        if (_tasks.TryGetValue(id, out var task) && !task.IsCompleted)
        {
            task.IsPaused = true;
            Debug.Log($"[CoroutineController] Pause ID={id}");
        }
    }

    /// <summary>恢复指定任务</summary>
    public void Resume(int id)
    {
        if (_tasks.TryGetValue(id, out var task) && !task.IsCompleted)
        {
            task.IsPaused = false;
            Debug.Log($"[CoroutineController] Resume ID={id}");
        }
    }

    /// <summary>停止并移除指定任务</summary>
    public void Stop(int id)
    {
        if (_tasks.TryGetValue(id, out var task))
        {
            if (task.CoroutineRef != null)
            {
                StopCoroutine(task.CoroutineRef);
            }
            _tasks.Remove(id);
            Debug.Log($"[CoroutineController] Stop ID={id}");
        }
    }

    /// <summary>停止所有具有相同标签的任务</summary>
    public void StopAllByTag(string tag)
    {
        var toRemove = new List<int>();
        foreach (var kv in _tasks)
        {
            if (kv.Value.Tag == tag)
                toRemove.Add(kv.Key);
        }

        foreach (var id in toRemove)
        {
            if (_tasks.TryGetValue(id, out var task))
            {
                if (task.CoroutineRef != null)
                {
                    StopCoroutine(task.CoroutineRef); // ✅ 停止协程
                }
                _tasks.Remove(id);
                Debug.Log($"[CoroutineController] StopByTag Tag={tag} ID={id}");
            }
        }
    }

    /// <summary>获取任务状态</summary>
    public string GetStatus(int id)
    {
        if (_tasks.TryGetValue(id, out var t))
        {
            return t.IsCompleted ? "Completed"
                 : t.IsPaused ? "Paused"
                               : "Running";
        }
        return "NotFound";
    }
}

// 使用示例：
// int rollId = CoroutineController.instance.StartRoutine(hero.MoveForwardRoutine(10f, 0.4f), "HeroRoll", () => Debug.Log("Roll Finished"));
// CoroutineController.instance.Pause(rollId);
// CoroutineController.instance.Resume(rollId);
// CoroutineController.instance.Stop(rollId);
// CoroutineController.instance.StopAllByTag("HeroRoll");
