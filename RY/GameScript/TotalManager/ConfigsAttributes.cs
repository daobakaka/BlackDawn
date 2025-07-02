using UnityEngine;
using System.Collections.Generic;
using System.IO;
using GameFrame.BaseClass;
using System;
using System.Linq;
using BlackDawn.DOTS;
//整个属性、威能、道具、武器、的josn解析系统
namespace BlackDawn
{
    public class MonsterAttributes : Singleton<MonsterAttributes>
    {

        private MonsterAttributes()
        {
            monserDic = new Dictionary<MonsterName, MonsterPro>();
        }

        // 怪物字典
        public Dictionary<MonsterName, MonsterPro> monserDic;

        // 怪物属性结构体
        public struct MonsterPro
        {
            public MonsterBaseAttribute baseAttribute;
            public MonsterAttackAttribute attackAttribute;
            public MonsterDefenseAttribute defenseAttribute;
            public MonsterGainAttribute gainAttribute;
            public MonsterLossPoolAttribute lossPoolAttribute;
            public MonsterDebuffAttribute debuffAttribute;
            public MonsterControlAbilityAttribute controlAbilityAttribute;
            public MonsterControlledEffectAttribute controlledEffectAttribute;
        }

        // 从 JSON 文件读取数据并填充字典
        public void LoadMonsterDataFromJson(string jsonFilePath)
        {
            // 确保文件存在
            if (!File.Exists(jsonFilePath))
            {
                DevDebug.LogError("JSON file not found!");
                return;
            }

            // 读取 JSON 文件内容
            string jsonData = File.ReadAllText(jsonFilePath);
            MonsterData monsterData = JsonUtility.FromJson<MonsterData>(jsonData);

            foreach (var monster in monsterData.monsters)
            {
                MonsterPro monsterPro = new MonsterPro
                {
                    baseAttribute = monster.baseAttribute,
                    attackAttribute = monster.attackAttribute,
                    defenseAttribute = monster.defenseAttribute,
                    gainAttribute = monster.gainAttribute,
                    lossPoolAttribute = monster.lossPoolAttribute,
                    debuffAttribute = monster.debuffAttribute,
                    controlAbilityAttribute = monster.controlAbilityAttribute,
                    controlledEffectAttribute = monster.controlledEffectAttribute,
                };

                // 将怪物属性加入字典
                monserDic[monster.baseAttribute.name] = monsterPro;
            }

            DevDebug.Log("Monsters data loaded successfully.");
        }

        // 用于解析 JSON 数据结构的类
        [System.Serializable]
        public class MonsterData
        {
            public List<MonsterJson> monsters;
        }

        [System.Serializable]
        public class MonsterJson
        {
            public MonsterBaseAttribute baseAttribute;
            public MonsterAttackAttribute attackAttribute;
            public MonsterDefenseAttribute defenseAttribute;
            public MonsterGainAttribute gainAttribute;
            public MonsterLossPoolAttribute lossPoolAttribute;
            public MonsterDebuffAttribute debuffAttribute;
            public MonsterControlAbilityAttribute controlAbilityAttribute;
            public MonsterControlledEffectAttribute controlledEffectAttribute;
        }
    }


    /// <summary>
    /// 英雄属性管理（单例）
    /// </summary>
    public class HeroAttributes : Singleton<HeroAttributes>
    {
        private const string HERO_SAVE = "heroes.dat";

        private HeroAttributes()
        {
            heroDic = new Dictionary<string, HeroPro>();
        }


        /// <summary>英雄字典：Key=英雄名，Value=属性结构</summary>
        public Dictionary<string, HeroPro> heroDic;

        /// <summary>英雄属性结构体</summary>
         [Serializable]
        public struct HeroPro
        {
            public string name;
            public BaseAttribute baseAttribute;
            public WeaponAttribute weaponAttribute;
            public AttackAttribute attackAttribute;
            public DefenseAttribute defenseAttribute;
            public GainAttribute gainAttribute;
            public LossPoolAttribute lossPoolAttribute;
            public DebuffAttribute debuffAttribute;
            public ControlAbilityAttribute controlAbilityAttribute;
            public ControlledEffectAttribute controlledEffectAttribute;
            public ControlDamageAttribute controlDamageAttribute;
            public DotDamageAttribute dotDamageAttribute;
            public SkillDamageAttribute skillDamageAttribute;

        }

        /// <summary>
        /// 从 JSON 文件读取所有英雄数据并填充字典
        /// </summary>
        public void LoadHeroDataFromJson(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
            {
                DevDebug.LogError($"JSON file not found: {jsonFilePath}");
                return;
            }

            string jsonData = File.ReadAllText(jsonFilePath);
            HeroData heroData = JsonUtility.FromJson<HeroData>(jsonData);
            if (heroData?.heroes == null)
            {
                DevDebug.LogError("HeroData parse failed or 'heroes' is null.");
                return;
            }

            foreach (var h in heroData.heroes)
            {
                var pro = new HeroPro
                {
                    baseAttribute = h.baseAttribute,
                    weaponAttribute = h.weaponAttribute,
                    attackAttribute = h.attackAttribute,
                    defenseAttribute = h.defenseAttribute,
                    gainAttribute = h.gainAttribute,
                    lossPoolAttribute = h.lossPoolAttribute,
                    debuffAttribute = h.debuffAttribute,
                    controlAbilityAttribute = h.controlAbilityAttribute,
                    controlledEffectAttribute = h.controlledEffectAttribute,
                    controlDamageAttribute = h.controlDamageAttribute,
                    dotDamageAttribute = h.dotDamageAttribute,
                    skillDamageAttribute = h.skillDamageAttribute,
                };
                heroDic[h.name] = pro;
            }

            DevDebug.Log("Hero data loaded successfully.");
        }

        // 用于 JSON 反序列化
        [System.Serializable]
        public class HeroData
        {
            public List<HeroJson> heroes;
        }

        [System.Serializable]
        public class HeroJson
        {
            public string name;
            public BaseAttribute baseAttribute;
            public WeaponAttribute weaponAttribute;
            public AttackAttribute attackAttribute;
            public DefenseAttribute defenseAttribute;
            public GainAttribute gainAttribute;
            public LossPoolAttribute lossPoolAttribute;
            public DebuffAttribute debuffAttribute;
            public ControlAbilityAttribute controlAbilityAttribute;
            public ControlledEffectAttribute controlledEffectAttribute;
            public ControlDamageAttribute controlDamageAttribute;
            public DotDamageAttribute dotDamageAttribute;
            public SkillDamageAttribute skillDamageAttribute;
        }

        [Serializable]
        private struct HeroEntry
        {
            public string name;
            public HeroPro data;
        }

        [Serializable]
        private class HeroSaveData
        {
            public List<HeroEntry> heroes = new List<HeroEntry>();
        }
        /// <summary>
        /// 这里后续应该是 读取 角色ID，角色密码，【角色名称：英文 支持简体中文/繁体中文 日文 西班牙文 韩文 】称号、技能、威能、关卡状态、存档等，
        /// </summary>
        public void SaveHeroData()
        {
            // 1) 构造要存的数据
            var save = new HeroSaveData();
            foreach (var kv in heroDic)
                save.heroes.Add(new HeroEntry { name = kv.Key, data = kv.Value });

            // 2) 序列化
            string wrappedJson = JsonUtility.ToJson(save, true);

            // 3) 写盘
            string pathDat = Path.Combine(Application.persistentDataPath, HERO_SAVE);
            File.WriteAllText(pathDat, wrappedJson);
            DevDebug.Log($"[SaveHeroData] saved encrypted to {pathDat}");

            // —— 编辑器／开发环境下，输出明文 JSON ——  
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string pathJson = Path.Combine(Application.persistentDataPath, "heroes_debug.json");
            File.WriteAllText(pathJson, wrappedJson);
            DevDebug.Log($"[SaveHeroData] debug JSON to {pathJson}");
#endif
        }

        public void LoadHeroData()
        {
            string pathDat = Path.Combine(Application.persistentDataPath, HERO_SAVE);
            if (!File.Exists(pathDat)) return;

            string wrappedJson = File.ReadAllText(pathDat);
            var save = JsonUtility.FromJson<HeroSaveData>(wrappedJson);
            heroDic.Clear();
            foreach (var e in save.heroes)
                heroDic[e.name] = e.data;
            DevDebug.Log($"[LoadHeroData] loaded {heroDic.Count} heroes");
        }

    }


    /// <summary>
    /// 武器属性管理（单例），从 JSON 中加载所有武器配置
    /// </summary>
    public class WeaponAttributes : Singleton<WeaponAttributes>
    {
        private const string OWNED_WEAPONS_SAVE = "owned_weapons.dat";

        private WeaponAttributes()
        {
            weaponDic = new Dictionary<string, WeaponPro>();
            ownedWeapons = new Dictionary<string, int>();

        }

        /// <summary>Key=武器名称, Value=属性</summary>
        public Dictionary<string, WeaponPro> weaponDic;

        /// <summary>玩家当前拥有武器的等级：Key=武器名, Value=等级</summary>
        public Dictionary<string, int> ownedWeapons;


        /// <summary>武器属性结构体：名称+武器数值+攻击数值</summary>
        [System.Serializable]
        public struct WeaponPro
        {
            public string name;
            public WeaponAttribute weaponAttribute;
            public AttackAttribute attackAttribute;
        }

        /// <summary>
        /// 从 JSON 文件读取所有武器数据并填充 weaponDic
        /// </summary>
        public void LoadWeaponDataFromJson(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
            {
                DevDebug.LogError($"Weapon JSON not found: {jsonFilePath}");
                return;
            }

            var text = File.ReadAllText(jsonFilePath);
            var data = JsonUtility.FromJson<WeaponData>(text);
            if (data?.weapons == null)
            {
                DevDebug.LogError("Failed to parse WeaponData or 'weapons' is null.");
                return;
            }

            foreach (var w in data.weapons)
            {
                var pro = new WeaponPro
                {
                    name = w.name,
                    weaponAttribute = w.weaponAttribute,
                    attackAttribute = w.attackAttribute
                };
                weaponDic[w.name] = pro;
            }

            DevDebug.Log("Weapon data loaded successfully.");
        }

        // --- 用于 JsonUtility 的反序列化容器 ---
        [System.Serializable]
        private class WeaponData
        {
            public List<WeaponJson> weapons;
        }

        [System.Serializable]
        private class WeaponJson
        {
            public string name;
            public WeaponAttribute weaponAttribute;
            public AttackAttribute attackAttribute;

        }
        [Serializable]
        private struct OwnedEntry { public string name; public int level; }

        [Serializable]
        private class OwnedSaveData { public List<OwnedEntry> weapons = new(); }

        /// <summary>
        /// 给玩家添加或升级武器：
        /// • 如果没有该武器，则等级 = 1  
        /// • 否则在现有等级上 +1  
        /// 并同步写盘
        /// </summary>
        public void AddOrUpgradeWeapon(string weaponName)
        {
            if (!weaponDic.ContainsKey(weaponName))
            {
                DevDebug.LogWarning($"[AddOrUpgradeWeapon] 未知武器 '{weaponName}'");
                return;
            }

            int newLevel = ownedWeapons.ContainsKey(weaponName)
                ? ownedWeapons[weaponName] + 1
                : 1;

            ownedWeapons[weaponName] = newLevel;

            SaveOwnedWeapons();
            DevDebug.Log($"[AddOrUpgradeWeapon] '{weaponName}' 等级更新为 {newLevel}");
        }

        /// <summary>获取指定武器的当前等级（不存在返回 0）</summary>
        public int GetWeaponLevel(string weaponName) =>
            ownedWeapons.TryGetValue(weaponName, out var lvl) ? lvl : 0;

        /// <summary>移除指定武器（或 -count），若等级 ≤ 0 则从字典中删掉</summary>
        public void RemoveOwnedWeapon(string weaponName, int count = 1)
        {
            if (!ownedWeapons.ContainsKey(weaponName)) return;

            ownedWeapons[weaponName] -= count;
            if (ownedWeapons[weaponName] <= 0)
                ownedWeapons.Remove(weaponName);
            SaveOwnedWeapons();
        }
        /// <summary>
        /// 重置武器
        /// </summary>
        public void ResetWeapon()
        {

            ownedWeapons.Clear();

            SaveOwnedWeapons();
           
        }


        #region  加密解密 区域

        // 包装载体：先校验 CRC 再存原 JSON
        [Serializable]
        private class WrappedData
        {
            public uint crc;
            public string payload;
        }


        /// <summary>将 ownedWeapons 写到本地磁盘</summary>
        /// <summary>保存玩家拥有武器（名称→等级）</summary>
        public void SaveOwnedWeapons()
        {
            // 1) 构造 payload 对象
            var data = new OwnedSaveData();
            foreach (var kv in ownedWeapons)
                data.weapons.Add(new OwnedEntry { name = kv.Key, level = kv.Value });

            // 2) JSON 序列化
            string payloadJson = JsonUtility.ToJson(data, true);

            // 3) CRC32 计算
            uint crc = Crc32Helper.Compute(payloadJson);

            // 4) 包装
            var wrapped = new WrappedData { crc = crc, payload = payloadJson };
            string wrappedJson = JsonUtility.ToJson(wrapped, true);

            // 5) AES 加密
            byte[] cipher = AesHelper.Encrypt(wrappedJson);

            // 6) 写入二进制文件
            string path = Path.Combine(Application.persistentDataPath, OWNED_WEAPONS_SAVE);
            try
            {
                File.WriteAllBytes(path, cipher);
                DevDebug.Log($"[SaveOwnedWeapons] encrypted save to {path}");
            }
            catch (Exception ex)
            {
                DevDebug.LogError($"[SaveOwnedWeapons] write failed: {ex}");
            }

            // —— EDITOR/DEV 额外写一份明文 JSON 方便调试 —— 
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            try
            {
                string debugPath = Path.Combine(Application.persistentDataPath, "owned_weapons_plain.json");
                File.WriteAllText(debugPath, payloadJson);
                DevDebug.Log($"[SaveOwnedWeapons] debug plain payload JSON to {debugPath}");
            }
            catch (Exception ex)
            {
                DevDebug.LogError($"[SaveOwnedWeapons] write plain payload JSON failed: {ex}");
            }
#endif
        }

        /// <summary>加载玩家拥有武器（解密→CRC校验→反序列化）</summary>
        public void LoadOwnedWeapons()
        {
            string path = Path.Combine(Application.persistentDataPath, OWNED_WEAPONS_SAVE);
            if (!File.Exists(path))
            {
                DevDebug.Log($"[LoadOwnedWeapons] no save file at {path}");
                return;
            }

            try
            {
                // 1) 读二进制并 AES 解密
                byte[] cipher = File.ReadAllBytes(path);
                string wrappedJson = AesHelper.Decrypt(cipher);

                // 2) 解包
                var wrapped = JsonUtility.FromJson<WrappedData>(wrappedJson);

                // 3) CRC 校验
                uint actual = Crc32Helper.Compute(wrapped.payload);
                if (actual != wrapped.crc)
                {
                    DevDebug.LogError($"[LoadOwnedWeapons] CRC mismatch: expected {wrapped.crc}, got {actual}");
                    ownedWeapons.Clear();
                    return;
                }

                // 4) 最终反序列化
                var data = JsonUtility.FromJson<OwnedSaveData>(wrapped.payload);
                ownedWeapons.Clear();
                foreach (var e in data.weapons)
                    ownedWeapons[e.name] = e.level;

                DevDebug.Log($"[LoadOwnedWeapons] loaded {ownedWeapons.Count} entries from {path}");
            }
            catch (Exception ex)
            {
                DevDebug.LogError($"[LoadOwnedWeapons] failed: {ex}");
                ownedWeapons.Clear();
            }
        }

#endregion

    }


    /// <summary>道具属性管理（单例），按类型分组存储道具列表</summary>
    public class ItemAttributes : Singleton<ItemAttributes>
    {
        /// <summary>单个道具的信息：名称 + 类型 + 属性增量映射</summary>
        [Serializable]
        public struct ItemPro
        {
            public string name;
            public ItemType type;
            public Dictionary<HeroItemAttributes, float> deltas;
        }
        private ItemAttributes()
        {
            itemsByType = new Dictionary<ItemType, List<ItemPro>>();
            foreach (ItemType t in Enum.GetValues(typeof(ItemType)))
                itemsByType[t] = new List<ItemPro>();

            ownerItem = new Dictionary<string, int>();
        }

        /// <summary>
        /// Key = 道具类型, Value = 该类型下所有 ItemPro 列表
        /// </summary>
        public Dictionary<ItemType, List<ItemPro>> itemsByType;

        /// <summary>玩家当前拥有的道具及其数量</summary>
        public Dictionary<string, int> ownerItem;

        #region JSON 解析用类型

        [Serializable]
        private class ItemData { public List<ItemJson> items; }

        [Serializable]
        private class ItemJson
        {
            public string name;
            public string type;
            public List<DeltaEntry> deltas;
        }

        [Serializable]
        private struct DeltaEntry
        {
            public string key;    // 改为 string
            public float value;
        }

        #endregion
        /// <summary>从 JSON 加载所有道具，按类型填充 itemsByType</summary>
        public void LoadItemDataFromJson(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
            {
                DevDebug.LogError($"Item JSON not found: {jsonFilePath}");
                return;
            }

            string json = File.ReadAllText(jsonFilePath);
            var data = JsonUtility.FromJson<ItemData>(json);
            if (data?.items == null)
            {
                DevDebug.LogError("Failed to parse item JSON or 'items' is null.");
                return;
            }

            // 清空每种类型的列表
            foreach (var list in itemsByType.Values)
                list.Clear();

            // 逐条解析
            foreach (var j in data.items)
            {
                // 1. 解析类型
                if (!Enum.TryParse<ItemType>(j.type, true, out var itype))
                    itype = ItemType.Basic;

                // 2. 打印原始条目，帮助调试
                DevDebug.Log($"Parsing deltas for item '{j.name}':");
                foreach (var entry in j.deltas)
                    DevDebug.Log($"  rawKey = '{entry.key}', value = {entry.value}");

                // 3. 把 string->enum，并检查重复/错误
                var map = new Dictionary<HeroItemAttributes, float>();
                foreach (var entry in j.deltas)
                {
                    if (!Enum.TryParse<HeroItemAttributes>(entry.key, true, out var parsedKey))
                    {
                        DevDebug.LogError($"Unknown attribute key '{entry.key}' in item '{j.name}'");
                        continue;
                    }
                    if (map.ContainsKey(parsedKey))
                    {
                        DevDebug.LogError($"Duplicate attribute '{parsedKey}' in item '{j.name}'");
                        continue;
                    }
                    map[parsedKey] = entry.value;
                }

                // 4. 构造 ItemPro 并分组
                var pro = new ItemPro { name = j.name, type = itype, deltas = map };
                itemsByType[itype].Add(pro);
            }

            // 加载结果日志
            DevDebug.Log("Loaded items by type:");
            foreach (var kv in itemsByType)
                DevDebug.Log($"  {kv.Key}: {kv.Value.Count} items");
        }


        /// <summary>按类型获取所有道具</summary>
        public List<ItemPro> GetByType(ItemType type)
        {
            return itemsByType.TryGetValue(type, out var list) ? list : new List<ItemPro>();
        }

        /// <summary>按名称查找单个道具</summary>
        public ItemPro? GetByName(string name)
        {
            foreach (var list in itemsByType.Values)
            {
                var pro = list.FirstOrDefault(i => i.name == name);
                if (pro.name != null)
                    return pro;
            }
            return null;
        }

        /// <summary>给玩家添加一件道具（数量 +1）</summary>
        public void AddItem(string itemName, int count = 1)
        {

            if (ownerItem.ContainsKey(itemName))
                ownerItem[itemName] += count;
            else
                ownerItem[itemName] = count;

            SaveOwnerItems(); 

        }
        /// <summary>从玩家身上移除一件道具（数量 -1，若到 0 则删掉这条记录）</summary>
        public void RemoveItem(string itemName)
        {
            if (!ownerItem.ContainsKey(itemName))
                return;

            ownerItem[itemName]--;
            if (ownerItem[itemName] <= 0)
                ownerItem.Remove(itemName);

            SaveOwnerItems();
        }
        /// <summary>
        /// 清空所有道具
        /// </summary>
        public void ResetItem()
        {
            ownerItem.Clear();

            SaveOwnerItems();

        }







        /// <summary>获取玩家一件道具的持有数量（不存在则返回 0）</summary>
        public int GetItemCount(string itemName)
        {
            return ownerItem.TryGetValue(itemName, out var cnt) ? cnt : 0;
        }

        /// <summary>计算玩家所有已拥有道具的总属性增量</summary>
        public Dictionary<HeroItemAttributes, float> ComputeTotalDeltas()
        {
            var total = new Dictionary<HeroItemAttributes, float>();
            foreach (var kv in ownerItem)
            {
                if (kv.Value <= 0) continue;
                var maybePro = GetByName(kv.Key);
                if (maybePro == null) continue;

                var pro = maybePro.Value;
                foreach (var d in pro.deltas)
                {
                    float add = d.Value * kv.Value;
                    if (total.ContainsKey(d.Key))
                        total[d.Key] += add;
                    else
                        total[d.Key] = add;
                }
            }
            return total;
        }


        #region 本地存盘与读盘

        [Serializable]
        private struct ItemEntry
        {
            public string name;
            public int count;
        }
        [Serializable]
        private class OwnerItemData
        {
            public List<ItemEntry> items = new List<ItemEntry>();
        }
        // 包装载体：先校验 CRC 再存原 JSON
        [Serializable]
        private class WrappedData
        {
            public uint crc;
            public string payload;
        }
        //本地文件名称
        private const string SAVE_FILE = "owner_items.dat";

        /// <summary>将 ownerItem 写入磁盘：先 JSON → CRC → 包装 → AES 加密 → 写文件</summary>
        public void SaveOwnerItems()
        {
            // 1) 把字典转 JSON
            var data = new OwnerItemData();
            foreach (var kv in ownerItem)
                data.items.Add(new ItemEntry { name = kv.Key, count = kv.Value });
            string payloadJson = JsonUtility.ToJson(data, true);

            // 2) 计算 CRC32
            uint crc = Crc32Helper.Compute(payloadJson);

            // 3) 包装
            var wrapped = new WrappedData { crc = crc, payload = payloadJson };
            string wrappedJson = JsonUtility.ToJson(wrapped, true);

            // 4) AES 加密
            byte[] cipher = AesHelper.Encrypt(wrappedJson);

            // 5) 写文件
            string path = Path.Combine(Application.persistentDataPath, SAVE_FILE);
            try
            {
                File.WriteAllBytes(path, cipher);
                DevDebug.Log($"[SaveOwnerItems] encrypted save to {path}");
            }
            catch (Exception ex)
            {
                DevDebug.LogError($"[SaveOwnerItems] write failed: {ex}");
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            try
            {
                // 直接把 payloadJson 写到 .json 文件
                string pathJson = Path.Combine(Application.persistentDataPath, "owner_items_plain.json");
                File.WriteAllText(pathJson, payloadJson);
                DevDebug.Log($"[SaveOwnerItems] debug plain payload JSON to {pathJson}");
            }
            catch (Exception ex)
            {
                DevDebug.LogError($"[SaveOwnerItems] write plain payload JSON failed: {ex}");
            }
#endif
        }

        /// <summary>从磁盘读取：AES 解密 → 解包 → CRC 校验 → JSON → 恢复字典</summary>
        public void LoadOwnerItems()
        {
            string path = Path.Combine(Application.persistentDataPath, SAVE_FILE);
            if (!File.Exists(path))
            {
                DevDebug.Log($"[LoadOwnerItems] no save file at {path}");
                return;
            }

            try
            {
                // 1) 读加密字节并解密
                byte[] cipher = File.ReadAllBytes(path);
                string wrappedJson = AesHelper.Decrypt(cipher);

                // 2) 解包
                var wrapped = JsonUtility.FromJson<WrappedData>(wrappedJson);

                // 3) CRC 校验
                uint actual = Crc32Helper.Compute(wrapped.payload);
                if (actual != wrapped.crc)
                {
                    DevDebug.LogError($"[LoadOwnerItems] CRC mismatch: expected {wrapped.crc}, got {actual}");
                    ownerItem.Clear();
                    return;
                }

                // 4) 最终反序列化 payload
                var data = JsonUtility.FromJson<OwnerItemData>(wrapped.payload);
                ownerItem.Clear();
                foreach (var e in data.items)
                    ownerItem[e.name] = e.count;

                DevDebug.Log($"[LoadOwnerItems] loaded {ownerItem.Count} entries from {path}");
            }
            catch (Exception ex)
            {
                DevDebug.LogError($"[LoadOwnerItems] failed: {ex}");
                ownerItem.Clear();
            }
        }

        #endregion


    }



}
