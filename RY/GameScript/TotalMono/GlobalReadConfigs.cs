using BlackDawn.DOTS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
//��ȡ�����ء������������ļ��Լ���Ҵ浵�ļ��ȣ�StreamingAssets ȫ�ּ��ػ������ã��浵·����ȫ�ּ��ش浵����
namespace BlackDawn
{

    /// <summary>
    /// ��ȡ����json�ļ�����
    /// </summary>
    public class GlobalReadConfigs : MonoBehaviour
    {
        // Start is called before the first frame update

        //Ӣ�۲���ģ��
        [HideInInspector]public HeroAttributeCmpt attributeCmpt; //Ӣ�����Լ̳�Icomponent�ӿ�
        //������ȡ����
        public static GlobalReadConfigs instance { get { return _configsInstance; } }
        private static GlobalReadConfigs _configsInstance;

        void Awake()
        {

            _configsInstance = this;

            //����json�ļ�
          LoadParametersFromJson();
            //���Դ���
            HeroAttributes.GetInstance().SaveHeroData();
           // WeaponAttributes.GetInstance().SaveWeaponData();



        }


        void Start()
        {

            //��ȡ���صĻ�������
            ReadBaseParameters();
           
            //���Խ׶�����մ浵
            ResetArchive();
            //���ص��ߡ�����������
            AddItemAndWeaponAndWeiNeng();
            //�����������
            ApplyItemAttributesToHero();
            //�������������ʼ��������
            ApplyBaseAttributeScaling();
            //��������
            ApplyWeapon("GalePistol");
            //�����������

            //���ؼ��ܣ�Ӧ�ü����˺�
            ApplySkill();

        }




        void LoadParametersFromJson()
        {
            // ��ȡ�������Ե���
            MonsterAttributes monsterAttributes = MonsterAttributes.GetInstance();

            // ���� JSON �ļ�����������ӵ��ֵ�

            string jsonFilePath = Path.Combine(Application.streamingAssetsPath, "Configs/MonsterConfigs.json"); // �����·�����Ը�����Ҫ�޸�
            monsterAttributes.LoadMonsterDataFromJson(jsonFilePath);

            // ��ӡ���ֵ��еĹ�����Ϣ
            foreach (var monster in monsterAttributes.monserDic)
            {
                DevDebug.Log($"Monster Name: {monster.Key}, Strength: {monster.Value.attackAttribute.attackPower}, HP: {monster.Value.defenseAttribute.hp}");
            }


            // ��ȡӢ�����Ե���
            HeroAttributes heroAttributes = HeroAttributes.GetInstance();

            // ���� JSON �ļ�����������ӵ��ֵ�
            string json1FilePath = Path.Combine(Application.streamingAssetsPath, "Configs/HeroConfigs.json"); // �����·�����Ը�����Ҫ�޸�

            heroAttributes.LoadHeroDataFromJson(json1FilePath);

            // ��ӡ���ֵ��еĹ�����Ϣ
            foreach (var hero in heroAttributes.heroDic)
            {
                DevDebug.Log($"Monster Name: {hero.Key}, Strength: {hero.Value.attackAttribute.attackPower}, HP: {hero.Value.defenseAttribute.hp}");
            }



            // ��ȡ�������Ե���
            var weaponAttributes = WeaponAttributes.GetInstance();

            // ���� JSON �ļ�����������ӵ��ֵ�
            string json2FilePath = Path.Combine(Application.streamingAssetsPath, "Configs/WeaponConfigs.json"); // �����·�����Ը�����Ҫ�޸�

            weaponAttributes.LoadWeaponDataFromJson(json2FilePath);

            // ��ӡ���ֵ��е�������Ϣ
            foreach (var weapon in weaponAttributes.weaponDic)
            {
                DevDebug.Log($"Weapon Name: {weapon.Key} string{weapon.Value.name} ");
            }


            //��ȡ�������Ե���
            var itemAttributes = ItemAttributes.GetInstance();

            // ���� JSON �ļ�����������ӵ��ֵ�
            string json3FilePath = Path.Combine(Application.streamingAssetsPath, "Configs/ItemConfigs.json"); // �����·�����Ը�����Ҫ�޸�

            itemAttributes.LoadItemDataFromJson(json3FilePath);

            // ��ӡ���ֵ��еĵ�����Ϣ
            foreach (var item in itemAttributes.itemsByType)
            {
                DevDebug.Log($"Item Name: {item.Key}");
            }

        }




        /// <summary>
        ///��ȡ��������ת��entity
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
        /// ���������������Եļ��ؼ����˺��������� ���ڸ���
        /// </summary>

        void ApplySkill()
        {
            // 1. �Ȱ����� SkillDamageAttribute �ṹ�������
            var skillAttr = attributeCmpt.skillDamageAttribute;

            // 2. ������ֲ��������޸�
            skillAttr.baseDamage = 100f * skillAttr.skillLevel
                * (1
                   + attributeCmpt.weaponAttribute.level * 0.05f
                   + attributeCmpt.baseAttribute.strength * 0.005f
                   + attributeCmpt.baseAttribute.agility * 0.0025f
                   + attributeCmpt.baseAttribute.intelligence * 0.0025f
                  );
              
            // 3. �ٰ��޸ĺ�ĸ���д��ȥ
            attributeCmpt.skillDamageAttribute = skillAttr;

        }

        /// <summary>
        /// ���ز�Ӧ��ָ�������ĵȼ��ӳɵ� attributeCmpt  
        /// �C weaponAttribute �е��ֶ����ɰ�֮ǰ�߼�����  
        /// �C attackAttribute �е������ֶζ���Ϊ��ÿ������ֵ������ (level-1) �� perLevel �ӵ�Ӣ�۵�ԭʼ������
        /// </summary>
        public void ApplyWeapon(string weaponName)
        {
            // 1. ȡ����
            var w = WeaponAttributes.GetInstance().weaponDic[weaponName];
            var wa = w.weaponAttribute;   // ���� level ����������
            var perLevel = w.attackAttribute;   // ÿ�����ӵĹ�������
            //��ȡ�����ȼ�����
            int lvl = (int)MathF.Max(1, WeaponAttributes.GetInstance().ownedWeapons[weaponName]);


            // 2. ���� WeaponAttribute �������
           // int lvl = Mathf.Max(1, wa.level);
            wa.itemCapacity += wa.magazineCapacityDelta * (lvl );
            wa.baseAttackSpeed += wa.baseAttackSpeedDelta * (lvl );
            wa.pelletCount += wa.pelletCountDelta * (lvl );
            wa.specialAttribute += wa.specialDelta * (lvl );
            attributeCmpt.weaponAttribute = wa;

            // 3. �� perLevel �����ۼӵ�Ӣ������� attackAttribute
            ref var aa = ref attributeCmpt.attackAttribute;
            int times = lvl;

            aa.attackPower += perLevel.attackPower * times;
            //----���ٲ���ģ�� ---///        
        
            //������Ա�������������ÿ�����������ٶȵ�����
            aa.attackSpeed += perLevel.attackSpeed * times;
            //�������ٵ��ڼӳɺ�Ļ�����������* �����ٶ�ֵ�������������˺����������ٽ��м���
            //�������ٵ����� ����������Ļ������ٳ��� ������� �����ٶȣ� ����������Ч�Ķ�̬����
            aa.weaponAttackSpeed = wa.baseAttackSpeed * (aa.attackSpeed);

            //----���ٲ���ģ�� ---///
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

            // Ԫ���˺�
            aa.elementalDamage.frostDamage += perLevel.elementalDamage.frostDamage * times;
            aa.elementalDamage.fireDamage += perLevel.elementalDamage.fireDamage * times;
            aa.elementalDamage.poisonDamage += perLevel.elementalDamage.poisonDamage * times;
            aa.elementalDamage.lightningDamage += perLevel.elementalDamage.lightningDamage * times;
            aa.elementalDamage.shadowDamage += perLevel.elementalDamage.shadowDamage * times;
            // DOT���������ۼӣ���Ԫ���˺���ȫһ�µĸ�ʽ��������
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

            // 4. д���������� using ref ����Ҫ���⸳ֵ��
            attributeCmpt.attackAttribute = aa;

            DevDebug.Log("��˪dotԭʼ�������ʣ�" + perLevel.dotProcChance.frostChance + "��˪Ԫ��ԭʼ�˺���" + perLevel.elementalDamage.frostDamage);
            DevDebug.Log("��˪dot�������ʣ�" + attributeCmpt.attackAttribute.dotProcChance.frostChance + "��˪Ԫ���˺���" + attributeCmpt.attackAttribute.elementalDamage.frostDamage);
        }


        /// <summary>
        /// ���ݱ����򣬾͵ظ��� _attributeCmpt:
        /// 1. ����������������  
        /// 2. ���������ԶԹ�����ר���ӳ�  
        /// 3. ����������ս�����Ե����棨�������ִ������ʡ����١����ܵȣ�  
        /// </summary>
        public void ApplyBaseAttributeScaling()
        {
            // ȡ����ǰ������ս������
            var b = attributeCmpt.baseAttribute;
            var a = attributeCmpt.attackAttribute;
            var d = attributeCmpt.defenseAttribute;

            // ���� 1. �����Ի������Ե����� ���� 
            float baseAttrMul = 1f + b.willpower * 0.01f;
            b.intelligence *= baseAttrMul;
            b.strength *= baseAttrMul;
            b.agility *= baseAttrMul;
            attributeCmpt.baseAttribute = b;

            // ���� 2. ����������ר���ӳ� ���� 

            // ������+1 Energy��+0.1 Ԫ�ر����ʣ�+0.03 Ԫ�ر����˺���+0.1 ��Ԫ�ؿ���
            d.energy += b.intelligence;
            a.elementalCritChance += b.intelligence * 0.1f;
            a.elementalCritDamage += b.intelligence * 0.03f;
            d.resistances.frost += b.intelligence * 0.1f;
            d.resistances.lightning += b.intelligence * 0.1f;
            d.resistances.poison += b.intelligence * 0.1f;
            d.resistances.shadow += b.intelligence * 0.1f;
            d.resistances.fire += b.intelligence * 0.1f;

            // ������+1 AttackPower��+1 Armor
            a.attackPower += b.strength;
            d.armor += b.strength;

            // ���ݣ�+0.3 �������ʣ�+0.1 �����ʣ�+1 �����ٶȣ�+0.1 �����˺�
            a.physicalCritChance += b.agility * 0.3f;
            d.dodge += b.agility * 0.001f;
            //ע�⹥���ٶ��ǳ�0.01
            a.attackSpeed += b.agility * 0.01f;
            a.critDamage += b.agility * 0.1f;

            // ���� 3. ������ս�����Ե��������� ���� 
            // ����ÿ�� +1% ����ս������
            //���ﲻ����д�ˣ� �����Ѿ������˳˵�����
            float gainMul = 1f + b.willpower * 0.01f;

            // �˺����,û��Ԫ���˺��ӳ�
            a.attackPower *= gainMul;
            a.damage *= gainMul;
            a.extraDamage *= gainMul;
            a.dotDamage *= gainMul;
            a.critDamage *= gainMul;


            // ���ּ���
            a.physicalCritChance *= gainMul;
            a.vulnerabilityChance *= gainMul;
            a.suppressionChance *= gainMul;
            a.elementalCritChance *= gainMul;
            a.dotCritChance *= gainMul;
            a.luckyStrikeChance *= gainMul;

            // �����ٶȡ����ܡ���
            a.attackSpeed *= gainMul;
            d.dodge *= gainMul;
            d.block *= gainMul;

            // ���������ס����ˡ�����
            d.hp *= gainMul;
            d.energy *= gainMul;
            d.armor *= gainMul;
            d.damageReduction *= gainMul;
            d.resistances.frost *= gainMul;
            d.resistances.lightning *= gainMul;
            d.resistances.poison *= gainMul;
            d.resistances.shadow *= gainMul;
            d.resistances.fire *= gainMul;


            
            // ����
            d.moveSpeed *= gainMul;

            // ��������ӳɣ�ѹ���˺� = 5 + 0.05*MaxHP + 0.2*���� + 0.02*����
            a.suppressionDamage =
                5f
              + d.hp * 0.05f
              + b.willpower * 0.2f
              + d.armor * 0.02f;

            //���¸����������٣���
            a.weaponAttackSpeed = a.attackSpeed;

            // д���޸ĺ�����
            attributeCmpt.attackAttribute = a;
            attributeCmpt.defenseAttribute = d;

            DevDebug.Log($" Strength: {attributeCmpt.attackAttribute.attackPower} ���ܣ�{attributeCmpt.defenseAttribute.dodge} ���ݣ�{attributeCmpt.baseAttribute.agility}");

        }

        /// <summary>���ص���,���������ܣ�����</summary>
        public void AddItemAndWeaponAndWeiNeng()
        {

            var itemInstance = ItemAttributes.GetInstance();

            //��ȡ����
            itemInstance.LoadOwnerItems();
            itemInstance.AddItem("FlameCompensator", 1);
           // itemInstance.AddItem("FlameCompensator", 1);



            var weaponInstance = WeaponAttributes.GetInstance();    

            weaponInstance.LoadOwnedWeapons();
            weaponInstance.AddOrUpgradeWeapon("GalePistol");

        }


        /// <summary>
        ///���ô浵
        /// </summary>
        public void ResetArchive()
        {
            ItemAttributes.GetInstance().ResetItem();
            WeaponAttributes.GetInstance().ResetWeapon();

        }


        /// <summary>�����е�������Ӧ�õ�Ӣ���������</summary>
        public void ApplyItemAttributesToHero()
        {
            var deltas = ItemAttributes.GetInstance().ComputeTotalDeltas();
            foreach (var kv in deltas)
            {
                switch (kv.Key)
                {
                    // ���� �������� ����  
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
                        // ����ֵӳ�䣬���ڴ˴�����ƺ��߼�
                        break;

                    // ���� �������� ����  
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
                        // RNGState ������ֵ��һ�㲻�ڵ������޸�
                        break;

                    // ���� Ԫ���˺� ����  
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

                    // ���� �������˺��������� ����  
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

                    // ���� �������� ����  
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

                    // ���� �������� ����  
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

                    // ���� �������� ����  
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

                    // ���� �������� ����  
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
