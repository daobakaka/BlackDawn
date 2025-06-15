using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BlackDawn.DOTS
{
    public struct BuffHandlerUtility
    {
        [ReadOnly] public ComponentLookup<MonsterAttributeCmpt> attrLookup;
        [ReadOnly] public BufferLookup<BuffHandlerBuffer> buffLookup;
        [ReadOnly] public BufferLookup<LinkedEntityGroup> linkedLookup;  // 新增
        Entity entity;
        public BuffHandlerUtility(ComponentLookup<MonsterAttributeCmpt> attrLookup, BufferLookup<BuffHandlerBuffer> buffLookup, BufferLookup<LinkedEntityGroup> linkedLookup)
        {
            entity = Entity.Null;
            this.attrLookup = attrLookup;
            this.buffLookup = buffLookup;
            this.linkedLookup = linkedLookup;
        }


        #region


        #endregion

        // public BuffHandlerUtility ByEntity(Entity entity)
        // {
        //     this.entity = entity;
        //     return this;
        // }
        #region BuffHandlerBuffer功能
        public void AddBuff(Entity entity, BuffHandlerBuffer buffer)
        {
            buffLookup[entity].Add(buffer);
        }


        #endregion

        #region 攻击属性

        // public float at
        // {
        //     get
        //     {
        //         float att = attrLookup[entity].attackAttribute.at;
        //         for (int i = 0; i < buffLookup[entity].Length; ++i)
        //             att += buffLookup[entity][i].buffInfo.buff.attribute.attackAttribute.at;
        //         return att;
        //     }
        // }
        public float Get_AttackAttribute_AT(Entity entity)
        {
            float at = attrLookup[entity].attackAttribute.attackPower;
            for (int i = 0; i < buffLookup[entity].Length; ++i)
                at += buffLookup[entity][i].buff.MonsterAttribute.attackAttribute.attackPower;
            return at;
        }
        #endregion

        #region 防御属性
        public float Get_DefenseAttribute_HP(Entity entity)
        {
            float hp = attrLookup[entity].defenseAttribute.hp;
            for (int i = 0; i < buffLookup[entity].Length; ++i)
            {
                hp += buffLookup[entity][i].buff.MonsterAttribute.defenseAttribute.hp;
            }
            return hp;
        }
        #endregion

    }
    public struct BuffHandlerBuffer : IBufferElementData
    {
        /// <summary>
        /// buff信息
        /// </summary>
        public Buff buff;
        /// <summary>
        /// 创造者
        /// </summary>
        public Entity creator;
        /// <summary>
        /// 目标
        /// </summary>
        public Entity target;


    }


    #region 多重攻击同时命中的并行处理逻辑

    /// <summary>
    /// 基础飞行子弹的多重命中，累加计算值，这里可以直接采用标签确定初始化容量
    /// </summary>
    [Serializable]
    [InternalBufferCapacity(10)]
    public struct FlightPropAccumulateData : IBufferElementData
    {
        public float damage, dotDamage;
        public float slow, fear, root, stun, freeze;
        public float firePool, frostPool, lightningPool, poisonPool, shadowPool, bleedPool;
    }



    /// <summary>
    /// 英雄技能的buffer，累加计算值，这里可以直接采用标签确定初始化容量
    /// </summary>
    [Serializable]
    [InternalBufferCapacity(5)]
    public struct HeroSkillPropAccumulateData : IBufferElementData
    {
        public float damage, dotDamage;
        public float slow, fear, root, stun, freeze;
        public float firePool, frostPool, lightningPool, poisonPool, shadowPool, bleedPool;
    }


    /// <summary>
    /// 怪物的buffer伤害列表，储存每次施加的总DOT以及剩余时间
    /// 初始容量15
    /// </summary>
    [Serializable]
    [InternalBufferCapacity(15)]
    public struct MonsterDotDamageBuffer : IBufferElementData
    {
        public float dotDamage, survivalTime;
    }

    /// <summary>
    /// 英雄的debuffer状态
    /// </summary>
    [Serializable]
    [InternalBufferCapacity(50)]
    public struct HeroDotDamageBuffer : IBufferElementData
    {

        public float dotDamage, survivalTime;
    }


    #endregion


}