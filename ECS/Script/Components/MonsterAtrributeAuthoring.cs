using System;
using Unity.Entities;
using UnityEngine;

namespace BlackDawn.DOTS
{
    /// <summary>
    /// 怪物继承数值属性
    /// </summary>
    public struct MonsterAttributeCmpt : IComponentData
    {

        public MonsterBaseAttribute monsterBaseAttribute;
        public MonsterAttackAttribute attackAttribute;
        public MonsterDefenseAttribute defenseAttribute;
        public MonsterGainAttribute gainAttribute;
        public MonsterLossPoolAttribute lossPoolAttribute;
        public MonsterDebuffAttribute debuffAttribute;
        public MonsterControlAbilityAttribute controlAbilityAttribute;
        public MonsterControlledEffectAttribute controlledEffectAttribute;


        public MonsterAttributeCmpt(MonsterAttributeCmpt other)
        {
            monsterBaseAttribute = other.monsterBaseAttribute;
            attackAttribute = other.attackAttribute;
            defenseAttribute = other.defenseAttribute;
            gainAttribute = other.gainAttribute;
            lossPoolAttribute = other.lossPoolAttribute;
            debuffAttribute = other.debuffAttribute;
            controlAbilityAttribute = other.controlAbilityAttribute;
            controlledEffectAttribute = other.controlledEffectAttribute;

        }
    }
  
}