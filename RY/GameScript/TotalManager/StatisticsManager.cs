using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFrame.BaseClass;
using System;
using System.IO;
namespace BlackDawn
{
    public sealed class StatisticsManager : Singleton<StatisticsManager>
    {
        [Serializable]
        public struct RunStats
        {
            public int enemiesKilled;   // 击杀怪物总数
            public float damageDealt;     // 造成总伤害
            public int goldCollected;   // 拾取金币总数
            public int itemsCollected;  // 获得道具总数
            public float runDuration;     // 本次战斗时长（秒）

            /// <summary>战斗开始时调用，重置所有字段并记录开始时间</summary>
            [NonSerialized] public float _startTime;
        }

        [Serializable]
        class StatsSaveData
        {
            public List<RunStats> runs = new List<RunStats>();
        }

        private const string SAVE_FILE = "run_stats.json";

        /// <summary>当前战斗的统计数据</summary>
        public RunStats currentRun;

        /// <summary>历史所有战斗的统计列表</summary>
        public List<RunStats> history = new List<RunStats>();

        private StatisticsManager()
        {
            LoadHistory();
        }

        /// <summary>在新战斗开始前调用，重置 currentRun</summary>
        public void StartRun()
        {
            currentRun = new RunStats();
            currentRun._startTime = Time.realtimeSinceStartup;
            Debug.Log("[Statistics] New run started");
        }

        /// <summary>战斗中：记录一次击杀</summary>
        public void RecordKill(int count = 1)
        {
            currentRun.enemiesKilled += count;
        }

        /// <summary>战斗中：记录一次伤害</summary>
        public void RecordDamage(float dmg)
        {
            currentRun.damageDealt += dmg;
        }

        /// <summary>战斗中：记录拾取金币</summary>
        public void RecordGold(int amount)
        {
            currentRun.goldCollected += amount;
        }

        /// <summary>战斗中：记录获得道具</summary>
        public void RecordItem(int count = 1)
        {
            currentRun.itemsCollected += count;
        }

        /// <summary>
        /// 战斗结束时调用：计算 runDuration，加入历史，持久化到本地
        /// </summary>
        public void EndRun()
        {
            currentRun.runDuration = Time.realtimeSinceStartup - currentRun._startTime;
            history.Add(currentRun);
            SaveHistory();
            Debug.Log($"[Statistics] Run ended: Killed={currentRun.enemiesKilled}, " +
                $"Damage={currentRun.damageDealt}, Gold={currentRun.goldCollected}, " +
                $"Items={currentRun.itemsCollected}, Duration={currentRun.runDuration:F1}s");
        }

        #region — 本地持久化 —

        /// <summary>将 history 保存到 Application.persistentDataPath/SAVE_FILE</summary>
        public void SaveHistory()
        {
            var data = new StatsSaveData { runs = history };
            string json = JsonUtility.ToJson(data, true);
            string path = Path.Combine(Application.persistentDataPath, SAVE_FILE);

            try
            {
                File.WriteAllText(path, json);
                Debug.Log($"[Statistics] Saved {history.Count} runs to {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Statistics] Save failed: {ex}");
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // 明文备份
            string debugPath = Path.Combine(Application.persistentDataPath, "run_stats_debug.json");
            File.WriteAllText(debugPath, json);
            Debug.Log($"[Statistics] Debug JSON to {debugPath}");
#endif
        }

        /// <summary>从本地读取并恢复历史 run 列表</summary>
        public void LoadHistory()
        {
            string path = Path.Combine(Application.persistentDataPath, SAVE_FILE);
            if (!File.Exists(path))
            {
                Debug.Log($"[Statistics] No history file at {path}");
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<StatsSaveData>(json);
                history = data?.runs ?? new List<RunStats>();
                Debug.Log($"[Statistics] Loaded {history.Count} runs from {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Statistics] Load failed: {ex}");
                history.Clear();
            }
        }

        #endregion



    }
}