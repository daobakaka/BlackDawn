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
    [InternalBufferCapacity(3)]
    public struct FlightPropAccumulateData : IBufferElementData
    {
        public float damage, dotDamage;
      //  public float slow, fear, root, stun, freeze;
        public float firePool, frostPool, lightningPool, poisonPool, shadowPool, bleedPool;
    }



    /// <summary>
    /// 英雄技能的buffer，累加计算值，这里可以直接采用标签确定初始化容量
    /// </summary>
    [Serializable]
    [InternalBufferCapacity(1)]
    public struct HeroSkillPropAccumulateData : IBufferElementData
    {
        public float damage, dotDamage;
      //  public float slow, fear, root, stun, freeze;
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
    #region 动态buffer区域
    /// <summary>
    /// Buffer 元素：记录一次触发碰撞的“另一方实体 + 距离平方”
    /// </summary>
    [Serializable]
    [InternalBufferCapacity(50)]
    public struct NearbyHit : IBufferElementData
    {
        public Entity other;
        public float sqrDist;
    }


    /// <summary>
    /// 打击记录
    /// </summary>
    [Serializable]
    [InternalBufferCapacity(10)] //默认容量10
    public struct HitRecord : IBufferElementData
    {
        public Entity other;
        //事件标签,攻击间隔
        public float timer;
        //通用判定,判定 暗影吞噬 等各种触发效果
        public bool universalJudgment;
    }


    /// <summary>
    /// 用于记录元素共鸣的记录条，后面可根据选择的中途休息选择的技能进行配置
    /// </summary>
    [Serializable]
    [InternalBufferCapacity(10)] //默认容量50
    public struct HitElementResonanceRecord : IBufferElementData
    {
        public Entity other;
        //事件标签,攻击间隔
        public float timer;
    }



    /// <summary>
    /// 英雄受伤打击记录
    /// </summary>
    [Serializable]
    [InternalBufferCapacity(50)]
    public struct HeroHitRecord : IBufferElementData
    {
        public Entity other;
        //事件标签,攻击间隔
        public float timer;
    }



    #endregion

}