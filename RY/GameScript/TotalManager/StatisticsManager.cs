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
            public int enemiesKilled;   // ��ɱ��������
            public float damageDealt;     // ������˺�
            public int goldCollected;   // ʰȡ�������
            public int itemsCollected;  // ��õ�������
            public float runDuration;     // ����ս��ʱ�����룩

            /// <summary>ս����ʼʱ���ã����������ֶβ���¼��ʼʱ��</summary>
            [NonSerialized] public float _startTime;
        }

        [Serializable]
        class StatsSaveData
        {
            public List<RunStats> runs = new List<RunStats>();
        }

        private const string SAVE_FILE = "run_stats.json";

        /// <summary>��ǰս����ͳ������</summary>
        public RunStats currentRun;

        /// <summary>��ʷ����ս����ͳ���б�</summary>
        public List<RunStats> history = new List<RunStats>();

        private StatisticsManager()
        {
            LoadHistory();
        }

        /// <summary>����ս����ʼǰ���ã����� currentRun</summary>
        public void StartRun()
        {
            currentRun = new RunStats();
            currentRun._startTime = Time.realtimeSinceStartup;
            Debug.Log("[Statistics] New run started");
        }

        /// <summary>ս���У���¼һ�λ�ɱ</summary>
        public void RecordKill(int count = 1)
        {
            currentRun.enemiesKilled += count;
        }

        /// <summary>ս���У���¼һ���˺�</summary>
        public void RecordDamage(float dmg)
        {
            currentRun.damageDealt += dmg;
        }

        /// <summary>ս���У���¼ʰȡ���</summary>
        public void RecordGold(int amount)
        {
            currentRun.goldCollected += amount;
        }

        /// <summary>ս���У���¼��õ���</summary>
        public void RecordItem(int count = 1)
        {
            currentRun.itemsCollected += count;
        }

        /// <summary>
        /// ս������ʱ���ã����� runDuration��������ʷ���־û�������
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

        #region �� ���س־û� ��

        /// <summary>�� history ���浽 Application.persistentDataPath/SAVE_FILE</summary>
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
            // ���ı���
            string debugPath = Path.Combine(Application.persistentDataPath, "run_stats_debug.json");
            File.WriteAllText(debugPath, json);
            Debug.Log($"[Statistics] Debug JSON to {debugPath}");
#endif
        }

        /// <summary>�ӱ��ض�ȡ���ָ���ʷ run �б�</summary>
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