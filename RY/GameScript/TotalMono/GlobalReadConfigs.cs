using BlackDawn.DOTS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
//读取、加载、储存配置流文件以及玩家存档文件等，StreamingAssets 全局加载基本配置，存档路径，全局加载存档配置
namespace BlackDawn
{

    /// <summary>
    /// 读取怪物json文件参数
    /// </summary>
    public class GlobalReadConfigs : MonoBehaviour
    {
        // Start is called before the first frame update

        //英雄参数模块
        [HideInInspector]public HeroAttributeCmpt attributeCmpt; //英雄属性继承Icomponent接口
        //参数读取单例
        public static GlobalReadConfigs instance { get { return _configsInstance; } }
        private static GlobalReadConfigs _configsInstance;

        void Awake()
        {

            _configsInstance = this;

            //加载json文件
          LoadParametersFromJson();
            //测试储存
            HeroAttributes.GetInstance().SaveHeroData();
           // WeaponAttributes.GetInstance().SaveWeaponData();



        }


        void Start()
        {

            //读取加载的基本参数
            ReadBaseParameters();
           
            //测试阶段先清空存档
            ResetArchive();
            //加载道具、武器、威能
            AddItemAndWeaponAndWeiNeng();
            //计算道具增益
            ApplyItemAttributesToHero();
            //计算基于意力初始属性收益
            ApplyBaseAttributeScaling();
            //加载武器
            ApplyWeapon("GalePistol");
            //计算道具增益

            //加载技能，应用技能伤害
            ApplySkill();

        }




        void LoadParametersFromJson()
        {
            // 获取怪物属性单例
            MonsterAttributes monsterAttributes = MonsterAttributes.GetInstance();

            // 加载 JSON 文件并将数据添加到字典

            string jsonFilePath = Path.Combine(Application.streamingAssetsPath, "Configs/MonsterConfigs.json"); // 这里的路径可以根据需要修改
            monsterAttributes.LoadMonsterDataFromJson(jsonFilePath);

            // 打印出字典中的怪物信息
            foreach (var monster in monsterAttributes.monserDic)
            {
                DevDebug.Log($"Monster Name: {monster.Key}, Strength: {monster.Value.attackAttribute.attackPower}, HP: {monster.Value.defenseAttribute.hp}");
            }


            // 获取英雄属性单例
            HeroAttributes heroAttributes = HeroAttributes.GetInstance();

            // 加载 JSON 文件并将数据添加到字典
            string json1FilePath = Path.Combine(Application.streamingAssetsPath, "Configs/HeroConfigs.json"); // 这里的路径可以根据需要修改

            heroAttributes.LoadHeroDataFromJson(json1FilePath);

            // 打印出字典中的怪物信息
            foreach (var hero in heroAttributes.heroDic)
            {
                DevDebug.Log($"Monster Name: {hero.Key}, Strength: {hero.Value.attackAttribute.attackPower}, HP: {hero.Value.defenseAttribute.hp}");
            }



            // 获取武器属性单例
            var weaponAttributes = WeaponAttributes.GetInstance();

            // 加载 JSON 文件并将数据添加到字典
            string json2FilePath = Path.Combine(Application.streamingAssetsPath, "Configs/WeaponConfigs.json"); // 这里的路径可以根据需要修改

            weaponAttributes.LoadWeaponDataFromJson(json2FilePath);

            // 打印出字典中的武器信息
            foreach (var weapon in weaponAttributes.weaponDic)
            {
                DevDebug.Log($"Weapon Name: {weapon.Key} string{weapon.Value.name} ");
            }


            //获取道具属性单例
            var itemAttributes = ItemAttributes.GetInstance();

            // 加载 JSON 文件并将数据添加到字典
            string json3FilePath = Path.Combine(Application.streamingAssetsPath, "Configs/ItemConfigs.json"); // 这里的路径可以根据需要修改

            itemAttributes.LoadItemDataFromJson(json3FilePath);

            // 打印出字典中的道具信息
            foreach (var item in itemAttributes.itemsByType)
            {
                DevDebug.Log($"Item Name: {item.Key}");
            }

        }




        /// <summary>
        ///读取基本参数转入entity
        /// </summary>
        void ReadBaseParameters()
        {
            var par = HeroAttributes.GetInstance().heroDic["Hero"];
            attributeCmpt.weaponAttribute = par.weaponAttribute;
            attributeCmpt.debuffAttribute = par.debuffAttribute;
            attributeCmpt.baseAttribute = par.baseAttribute;
            attributeCmpt.attackAttribute = par.attackAttribute;
            attributeCmpt.gainAttribute = par.gainAttribute;
            attributeCmpt.controlAbilityAttribute = par.controlAbilityAttribute;
            attributeCmpt.controlledEffectAttribute = par.controlledEffectAttribute;
            attributeCmpt.defenseAttribute = par.defenseAttribute;
            attributeCmpt.controlDamageAttribute = par.controlDamageAttribute;
            attributeCmpt.dotDamageAttribute=par.dotDamageAttribute;
            attributeCmpt.skillDamageAttribute = par.skillDamageAttribute;




        }
        /// <summary>
        /// ！！！！！！测试的加载技能伤害，测试类 后期更改
        /// </summary>

        void ApplySkill()
        {
            // 1. 先把整个 SkillDamageAttribute 结构体读出来
            var skillAttr = attributeCmpt.skillDamageAttribute;

            // 2. 在这个局部副本上修改
            skillAttr.baseDamage = 100f * skillAttr.skillLevel
                * (1
                   + attributeCmpt.weaponAttribute.level * 0.05f
                   + attributeCmpt.baseAttribute.strength * 0.005f
                   + attributeCmpt.baseAttribute.agility * 0.0025f
                   + attributeCmpt.baseAttribute.intelligence * 0.0025f
                  );
              
            // 3. 再把修改后的副本写回去
            attributeCmpt.skillDamageAttribute = skillAttr;

        }

        /// <summary>
        /// 加载并应用指定武器的等级加成到 attributeCmpt  
        /// – weaponAttribute 中的字段依旧按之前逻辑叠加  
        /// – attackAttribute 中的所有字段都视为“每级增加值”，按 (level-1) × perLevel 加到英雄的原始属性上
        /// </summary>
        public void ApplyWeapon(string weaponName)
        {
            // 1. 取配置
            var w = WeaponAttributes.GetInstance().weaponDic[weaponName];
            var wa = w.weaponAttribute;   // 包含 level 和武器参数
            var perLevel = w.attackAttribute;   // 每级增加的攻击属性
            //获取武器等级配置
            int lvl = (int)MathF.Max(1, WeaponAttributes.GetInstance().ownedWeapons[weaponName]);


            // 2. 叠加 WeaponAttribute 里的增量
           // int lvl = Mathf.Max(1, wa.level);
            wa.itemCapacity += wa.magazineCapacityDelta * (lvl );
            wa.baseAttackSpeed += wa.baseAttackSpeedDelta * (lvl );
            wa.pelletCount += wa.pelletCountDelta * (lvl );
            wa.specialAttribute += wa.specialDelta * (lvl );
            attributeCmpt.weaponAttribute = wa;

            // 3. 将 perLevel 属性累加到英雄组件的 attackAttribute
            ref var aa = ref attributeCmpt.attackAttribute;
            int times = lvl;

            aa.attackPower += perLevel.attackPower * times;
            //----攻速参数模块 ---///        
        
            //这里可以保留，允许武器每级提升攻击速度的属性
            aa.attackSpeed += perLevel.attackSpeed * times;
            //武器攻速等于加成后的基础武器攻速* 攻击速度值！！！，基础伤害以武器攻速进行计算
            //武器攻速的增加 基于武器类的基础攻速乘以 攻击类的 攻击速度， 这样可以有效的动态分离
            aa.weaponAttackSpeed = wa.baseAttackSpeed * (aa.attackSpeed);

            //----攻速参数模块 ---///
            aa.armorPenetration += perLevel.armorPenetration * times;
            aa.elementalPenetration += perLevel.elementalPenetration * times;
            aa.projectilePenetration += perLevel.projectilePenetration * times;
            aa.damage += perLevel.damage * times;
            aa.physicalCritChance += perLevel.physicalCritChance * times;
            aa.critDamage += perLevel.critDamage * times;
            aa.vulnerabilityDamage += perLevel.vulnerabilityDamage * times;
            aa.vulnerabilityChance += perLevel.vulnerabilityChance * times;
            aa.suppressionDamage += perLevel.suppressionDamage * times;
            aa.suppressionChance += perLevel.suppressionChance * times;

            // 元素伤害
            aa.elementalDamage.frostDamage += perLevel.elementalDamage.frostDamage * times;
            aa.elementalDamage.fireDamage += perLevel.elementalDamage.fireDamage * times;
            aa.elementalDamage.poisonDamage += perLevel.elementalDamage.poisonDamage * times;
            aa.elementalDamage.lightningDamage += perLevel.elementalDamage.lightningDamage * times;
            aa.elementalDamage.shadowDamage += perLevel.elementalDamage.shadowDamage * times;
            // DOT触发几率累加（与元素伤害完全一致的格式和命名）
            aa.dotProcChance.bleedChance += perLevel.dotProcChance.bleedChance * times;
            aa.dotProcChance.frostChance += perLevel.dotProcChance.frostChance * times;
            aa.dotProcChance.lightningChance += perLevel.dotProcChance.lightningChance * times;
            aa.dotProcChance.poisonChance += perLevel.dotProcChance.poisonChance * times;
            aa.dotProcChance.shadowChance += perLevel.dotProcChance.shadowChance * times;
            aa.dotProcChance.fireChance += perLevel.dotProcChance.fireChance * times;

            aa.luckyStrikeChance += perLevel.luckyStrikeChance * times;
            aa.cooldownReduction += perLevel.cooldownReduction * times;
            aa.elementalCritChance += perLevel.elementalCritChance * times;
            aa.elementalCritDamage += perLevel.elementalCritDamage * times;
            aa.dotDamage += perLevel.dotDamage * times;
            aa.dotCritChance += perLevel.dotCritChance * times;
            aa.dotCritDamage += perLevel.dotCritDamage * times;
            aa.extraDamage += perLevel.extraDamage * times;

            // 4. 写回组件（如果 using ref 不需要额外赋值）
            attributeCmpt.attackAttribute = aa;

            DevDebug.Log("冰霜dot原始触发几率：" + perLevel.dotProcChance.frostChance + "冰霜元素原始伤害：" + perLevel.elementalDamage.frostDamage);
            DevDebug.Log("冰霜dot触发几率：" + attributeCmpt.attackAttribute.dotProcChance.frostChance + "冰霜元素伤害：" + attributeCmpt.attackAttribute.elementalDamage.frostDamage);
        }


        /// <summary>
        /// 根据表格规则，就地更新 _attributeCmpt:
        /// 1. 意力提升基础属性  
        /// 2. 各基础属性对攻防的专属加成  
        /// 3. 意力对所有战斗属性的增益（包含各种触发几率、攻速、闪避等）  
        /// </summary>
        public void ApplyBaseAttributeScaling()
        {
            // 取出当前基础与战斗属性
            var b = attributeCmpt.baseAttribute;
            var a = attributeCmpt.attackAttribute;
            var d = attributeCmpt.defenseAttribute;

            // —— 1. 意力对基础属性的增益 —— 
            float baseAttrMul = 1f + b.willpower * 0.01f;
            b.intelligence *= baseAttrMul;
            b.strength *= baseAttrMul;
            b.agility *= baseAttrMul;
            attributeCmpt.baseAttribute = b;

            // —— 2. 各基础属性专属加成 —— 

            // 智力：+1 Energy；+0.1 元素暴击率；+0.03 元素暴击伤害；+0.1 各元素抗性
            d.energy += b.intelligence;
            a.elementalCritChance += b.intelligence * 0.1f;
            a.elementalCritDamage += b.intelligence * 0.03f;
            d.resistances.frost += b.intelligence * 0.1f;
            d.resistances.lightning += b.intelligence * 0.1f;
            d.resistances.poison += b.intelligence * 0.1f;
            d.resistances.shadow += b.intelligence * 0.1f;
            d.resistances.fire += b.intelligence * 0.1f;

            // 力量：+1 AttackPower；+1 Armor
            a.attackPower += b.strength;
            d.armor += b.strength;

            // 敏捷：+0.3 物理暴击率；+0.1 闪避率；+1 攻击速度；+0.1 暴击伤害
            a.physicalCritChance += b.agility * 0.3f;
            d.dodge += b.agility * 0.001f;
            //注意攻击速度是乘0.01
            a.attackSpeed += b.agility * 0.01f;
            a.critDamage += b.agility * 0.1f;

            // —— 3. 意力对战斗属性的整体增益 —— 
            // 意力每点 +1% 所有战斗属性
            //这里不用重写乘， 这里已经计算了乘的属性
            float gainMul = 1f + b.willpower * 0.01f;

            // 伤害相关,没有元素伤害加成
            a.attackPower *= gainMul;
            a.damage *= gainMul;
            a.extraDamage *= gainMul;
            a.dotDamage *= gainMul;
            a.critDamage *= gainMul;


            // 各种几率
            a.physicalCritChance *= gainMul;
            a.vulnerabilityChance *= gainMul;
            a.suppressionChance *= gainMul;
            a.elementalCritChance *= gainMul;
            a.dotCritChance *= gainMul;
            a.luckyStrikeChance *= gainMul;

            // 攻击速度、闪避、格挡
            a.attackSpeed *= gainMul;
            d.dodge *= gainMul;
            d.block *= gainMul;

            // 生命、护甲、减伤、抗性
            d.hp *= gainMul;
            d.energy *= gainMul;
            d.armor *= gainMul;
            d.damageReduction *= gainMul;
            d.resistances.frost *= gainMul;
            d.resistances.lightning *= gainMul;
            d.resistances.poison *= gainMul;
            d.resistances.shadow *= gainMul;
            d.resistances.fire *= gainMul;


            
            // 移速
            d.moveSpeed *= gainMul;

            // 意力额外加成：压制伤害 = 5 + 0.05*MaxHP + 0.2*意力 + 0.02*护甲
            a.suppressionDamage =
                5f
              + d.hp * 0.05f
              + b.willpower * 0.2f
              + d.armor * 0.02f;

            //重新赋予武器攻速！！
            a.weaponAttackSpeed = a.attackSpeed;

            // 写回修改后的组件
            attributeCmpt.attackAttribute = a;
            attributeCmpt.defenseAttribute = d;

            DevDebug.Log($" Strength: {attributeCmpt.attackAttribute.attackPower} 闪避：{attributeCmpt.defenseAttribute.dodge} 敏捷：{attributeCmpt.baseAttribute.agility}");

        }

        /// <summary>加载道具,武器，威能，技能</summary>
        public void AddItemAndWeaponAndWeiNeng()
        {

            var itemInstance = ItemAttributes.GetInstance();

            //读取道具
            itemInstance.LoadOwnerItems();
            itemInstance.AddItem("FlameCompensator", 1);
           // itemInstance.AddItem("FlameCompensator", 1);



            var weaponInstance = WeaponAttributes.GetInstance();    

            weaponInstance.LoadOwnedWeapons();
            weaponInstance.AddOrUpgradeWeapon("GalePistol");

        }


        /// <summary>
        ///重置存档
        /// </summary>
        public void ResetArchive()
        {
            ItemAttributes.GetInstance().ResetItem();
            WeaponAttributes.GetInstance().ResetWeapon();

        }


        /// <summary>将所有道具增量应用到英雄属性组件</summary>
        public void ApplyItemAttributesToHero()
        {
            var deltas = ItemAttributes.GetInstance().ComputeTotalDeltas();
            foreach (var kv in deltas)
            {
                switch (kv.Key)
                {
                    // —— 基础属性 ——  
                    case HeroItemAttributes.Intelligence:
                        attributeCmpt.baseAttribute.intelligence += kv.Value;
                        break;
                    case HeroItemAttributes.Strength:
                        attributeCmpt.baseAttribute.strength += kv.Value;
                        break;
                    case HeroItemAttributes.Agility:
                        attributeCmpt.baseAttribute.agility += kv.Value;
                        break;
                    case HeroItemAttributes.Willpower:
                        attributeCmpt.baseAttribute.willpower += kv.Value;
                        break;
                    case HeroItemAttributes.Title:
                        // 无数值映射，可在此处处理称号逻辑
                        break;

                    // —— 防御属性 ——  
                    case HeroItemAttributes.OriginalHp:
                        attributeCmpt.defenseAttribute.originalHp += kv.Value;
                        break;
                    case HeroItemAttributes.Hp:
                        attributeCmpt.defenseAttribute.hp += kv.Value;
                        break;
                    case HeroItemAttributes.DamageReduction:
                        attributeCmpt.defenseAttribute.damageReduction += kv.Value;
                        break;
                    case HeroItemAttributes.Energy:
                        attributeCmpt.defenseAttribute.energy += kv.Value;
                        break;
                    case HeroItemAttributes.Armor:
                        attributeCmpt.defenseAttribute.armor += kv.Value;
                        break;
                    case HeroItemAttributes.FrostResistance:
                        attributeCmpt.defenseAttribute.resistances.frost += kv.Value;
                        break;
                    case HeroItemAttributes.LightningResistance:
                        attributeCmpt.defenseAttribute.resistances.lightning += kv.Value;
                        break;
                    case HeroItemAttributes.PoisonResistance:
                        attributeCmpt.defenseAttribute.resistances.poison += kv.Value;
                        break;
                    case HeroItemAttributes.ShadowResistance:
                        attributeCmpt.defenseAttribute.resistances.shadow += kv.Value;
                        break;
                    case HeroItemAttributes.FireResistance:
                        attributeCmpt.defenseAttribute.resistances.fire += kv.Value;
                        break;
                    case HeroItemAttributes.Dodge:
                        attributeCmpt.defenseAttribute.dodge += kv.Value;
                        break;
                    case HeroItemAttributes.MoveSpeed:
                        attributeCmpt.defenseAttribute.moveSpeed += kv.Value;
                        break;
                    case HeroItemAttributes.Block:
                        attributeCmpt.defenseAttribute.block += kv.Value;
                        break;
                    case HeroItemAttributes.RNGState:
                        // RNGState 非增量值，一般不在道具中修改
                        break;

                    // —— 元素伤害 ——  
                    case HeroItemAttributes.FrostDamage:
                        attributeCmpt.attackAttribute.elementalDamage.frostDamage += kv.Value;
                        break;
                    case HeroItemAttributes.LightningDamage:
                        attributeCmpt.attackAttribute.elementalDamage.lightningDamage += kv.Value;
                        break;
                    case HeroItemAttributes.PoisonDamage:
                        attributeCmpt.attackAttribute.elementalDamage.poisonDamage += kv.Value;
                        break;
                    case HeroItemAttributes.ShadowDamage:
                        attributeCmpt.attackAttribute.elementalDamage.shadowDamage += kv.Value;
                        break;
                    case HeroItemAttributes.FireDamage:
                        attributeCmpt.attackAttribute.elementalDamage.fireDamage += kv.Value;
                        break;

                    // —— 持续性伤害触发几率 ——  
                    case HeroItemAttributes.BleedChance:
                        attributeCmpt.attackAttribute.dotProcChance.bleedChance += kv.Value;
                        break;
                    case HeroItemAttributes.FrostChance:
                        attributeCmpt.attackAttribute.dotProcChance.frostChance += kv.Value;
                        break;
                    case HeroItemAttributes.LightningChance:
                        attributeCmpt.attackAttribute.dotProcChance.lightningChance += kv.Value;
                        break;
                    case HeroItemAttributes.PoisonChance:
                        attributeCmpt.attackAttribute.dotProcChance.poisonChance += kv.Value;
                        break;
                    case HeroItemAttributes.ShadowChance:
                        attributeCmpt.attackAttribute.dotProcChance.shadowChance += kv.Value;
                        break;
                    case HeroItemAttributes.FireChance:
                        attributeCmpt.attackAttribute.dotProcChance.fireChance += kv.Value;
                        break;

                    // —— 攻击属性 ——  
                    case HeroItemAttributes.AttackPower:
                        attributeCmpt.attackAttribute.attackPower += kv.Value;
                        break;
                    case HeroItemAttributes.AttackSpeed:
                        attributeCmpt.attackAttribute.attackSpeed += kv.Value;
                        break;
                    case HeroItemAttributes.ArmorPenetration:
                        attributeCmpt.attackAttribute.armorPenetration += kv.Value;
                        break;
                    case HeroItemAttributes.ElementalPenetration:
                        attributeCmpt.attackAttribute.elementalPenetration += kv.Value;
                        break;
                    case HeroItemAttributes.ArmorBreak:
                        attributeCmpt.attackAttribute.armorBreak += kv.Value;
                        break;
                    case HeroItemAttributes.ElementalBreak:
                        attributeCmpt.attackAttribute.elementalBreak += kv.Value;
                        break;
                    case HeroItemAttributes.ProjectilePenetration:
                        attributeCmpt.attackAttribute.projectilePenetration += kv.Value;
                        break;
                    case HeroItemAttributes.Damage:
                        attributeCmpt.attackAttribute.damage += kv.Value;
                        break;
                    case HeroItemAttributes.PhysicalCritChance:
                        attributeCmpt.attackAttribute.physicalCritChance += kv.Value;
                        break;
                    case HeroItemAttributes.CritDamage:
                        attributeCmpt.attackAttribute.critDamage += kv.Value;
                        break;
                    case HeroItemAttributes.VulnerabilityDamage:
                        attributeCmpt.attackAttribute.vulnerabilityDamage += kv.Value;
                        break;
                    case HeroItemAttributes.VulnerabilityChance:
                        attributeCmpt.attackAttribute.vulnerabilityChance += kv.Value;
                        break;
                    case HeroItemAttributes.SuppressionDamage:
                        attributeCmpt.attackAttribute.suppressionDamage += kv.Value;
                        break;
                    case HeroItemAttributes.SuppressionChance:
                        attributeCmpt.attackAttribute.suppressionChance += kv.Value;
                        break;
                    case HeroItemAttributes.LuckyStrikeChance:
                        attributeCmpt.attackAttribute.luckyStrikeChance += kv.Value;
                        break;
                    case HeroItemAttributes.CooldownReduction:
                        attributeCmpt.attackAttribute.cooldownReduction += kv.Value;
                        break;
                    case HeroItemAttributes.ElementalCritChance:
                        attributeCmpt.attackAttribute.elementalCritChance += kv.Value;
                        break;
                    case HeroItemAttributes.ElementalCritDamage:
                        attributeCmpt.attackAttribute.elementalCritDamage += kv.Value;
                        break;
                    case HeroItemAttributes.DotDamage:
                        attributeCmpt.attackAttribute.dotDamage += kv.Value;
                        break;
                    case HeroItemAttributes.DotCritChance:
                        attributeCmpt.attackAttribute.dotCritChance += kv.Value;
                        break;
                    case HeroItemAttributes.DotCritDamage:
                        attributeCmpt.attackAttribute.dotCritDamage += kv.Value;
                        break;
                    case HeroItemAttributes.ExtraDamage:
                        attributeCmpt.attackAttribute.extraDamage += kv.Value;
                        break;

                    // —— 增益属性 ——  
                    case HeroItemAttributes.AtkRange:
                        attributeCmpt.gainAttribute.atkRange += kv.Value;
                        break;
                    case HeroItemAttributes.SkillRange:
                        attributeCmpt.gainAttribute.skillRange += kv.Value;
                        break;
                    case HeroItemAttributes.ExplosionRange:
                        attributeCmpt.gainAttribute.explosionRange += kv.Value;
                        break;
                    case HeroItemAttributes.SkillDuration:
                        attributeCmpt.gainAttribute.skillDuration += kv.Value;
                        break;
                    case HeroItemAttributes.HpRegen:
                        attributeCmpt.gainAttribute.hpRegen += kv.Value;
                        break;
                    case HeroItemAttributes.EnergyRegen:
                        attributeCmpt.gainAttribute.energyRegen += kv.Value;
                        break;

                    // —— 控制能力 ——  
                    case HeroItemAttributes.Stun:
                        attributeCmpt.controlAbilityAttribute.stun += kv.Value;
                        break;
                    case HeroItemAttributes.Slow:
                        attributeCmpt.controlAbilityAttribute.slow += kv.Value;
                        break;
                    case HeroItemAttributes.Root:
                        attributeCmpt.controlAbilityAttribute.root += kv.Value;
                        break;
                    case HeroItemAttributes.Fear:
                        attributeCmpt.controlAbilityAttribute.fear += kv.Value;
                        break;
                    case HeroItemAttributes.Freeze:
                        attributeCmpt.controlAbilityAttribute.freeze += kv.Value;
                        break;
                    case HeroItemAttributes.Knockback:
                        attributeCmpt.controlAbilityAttribute.knockback += kv.Value;
                        break;

                    // —— 武器属性 ——  
                    case HeroItemAttributes.PropSpeed:
                        attributeCmpt.weaponAttribute.propSpeed += kv.Value;
                        break;
                    case HeroItemAttributes.ItemCapacity:
                        attributeCmpt.weaponAttribute.itemCapacity += (int)kv.Value;
                        break;
                    case HeroItemAttributes.ReloadTime:
                        attributeCmpt.weaponAttribute.reloadTime += kv.Value;
                        break;
                    case HeroItemAttributes.BaseAttackSpeed:
                        attributeCmpt.weaponAttribute.baseAttackSpeed += kv.Value;
                        break;
                    case HeroItemAttributes.Level:
                        attributeCmpt.weaponAttribute.level += (int)kv.Value;
                        break;
                    case HeroItemAttributes.PelletCount:
                        attributeCmpt.weaponAttribute.pelletCount += (int)kv.Value;
                        break;
                    case HeroItemAttributes.SpecialAttribute:
                        attributeCmpt.weaponAttribute.specialAttribute += (int)kv.Value;
                        break;
                    case HeroItemAttributes.MagazineCapacityDelta:
                        attributeCmpt.weaponAttribute.magazineCapacityDelta += (int)kv.Value;
                        break;
                    case HeroItemAttributes.BaseAttackSpeedDelta:
                        attributeCmpt.weaponAttribute.baseAttackSpeedDelta += kv.Value;
                        break;
                    case HeroItemAttributes.PelletCountDelta:
                        attributeCmpt.weaponAttribute.pelletCountDelta += (int)kv.Value;
                        break;
                    case HeroItemAttributes.SpecialDelta:
                        attributeCmpt.weaponAttribute.specialDelta += (int)kv.Value;
                        break;
                    case HeroItemAttributes.SectorAngle:
                        attributeCmpt.weaponAttribute.sectorAngle += kv.Value;
                        break;

                    default:
                        DevDebug.LogWarning($"Unhandled item attribute: {kv.Key}");
                        break;
                }
            }
        }

    }
}
