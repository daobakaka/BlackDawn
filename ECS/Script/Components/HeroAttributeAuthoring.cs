using System;
using Unity.Entities;
using UnityEngine;

namespace BlackDawn.DOTS
{
    /// <summary>
    /// 英雄继承数值属性
    /// </summary>
    public struct HeroAttributeCmpt : IComponentData
    {
        public BaseAttribute baseAttribute;
        public AttackAttribute attackAttribute;
        public DefenseAttribute defenseAttribute;
        public GainAttribute gainAttribute;
        public LossPoolAttribute lossPoolAttribute;
        public DebuffAttribute debuffAttribute;
        public ControlAbilityAttribute controlAbilityAttribute;
        public ControlledEffectAttribute controlledEffectAttribute;

        // 新增武器属性
        public WeaponAttribute weaponAttribute;
        //新增控制伤害加成属性
        public ControlDamageAttribute controlDamageAttribute;
        //新增DOT影响伤害成属性
        public DotDamageAttribute dotDamageAttribute;
        //新增技能伤害结构体
        public SkillDamageAttribute skillDamageAttribute;
        //新增动态伤害结构体
        
    

        public HeroAttributeCmpt(HeroAttributeCmpt other)
        {
            baseAttribute = other.baseAttribute;
            attackAttribute = other.attackAttribute;
            defenseAttribute = other.defenseAttribute;
            gainAttribute = other.gainAttribute;
            lossPoolAttribute = other.lossPoolAttribute;
            debuffAttribute = other.debuffAttribute;
            controlAbilityAttribute = other.controlAbilityAttribute;
            controlledEffectAttribute = other.controlledEffectAttribute;
            weaponAttribute = other.weaponAttribute;
            controlDamageAttribute = other.controlDamageAttribute;
            dotDamageAttribute = other.dotDamageAttribute;
            skillDamageAttribute = other.skillDamageAttribute;
        }
    }

}