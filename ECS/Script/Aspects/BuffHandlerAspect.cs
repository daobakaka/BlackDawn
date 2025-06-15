using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;


namespace BlackDawn.DOTS
{
    /// <summary>
    /// BuffHandler和属性的集合
    /// </summary>
    public readonly partial struct BuffHandlerAspect : IAspect
    {
        // 拿到 DynamicBuffer<BuffHandlerBuffer>
        public readonly DynamicBuffer<BuffHandlerBuffer> buffHandler;
        // 拿到 AttributeCmpt，可读写
        public readonly RefRO<MonsterAttributeCmpt> attribute;

        #region BuffHandler功能
        public void AddBuff()
        {

        }

        #endregion


        #region 攻击属性
        /// <summary>
        /// 所有装备 技能 buff 基础属性的攻击力
        /// </summary>
        /// <value></value>
        public float at
        {
            get
            {
                float att = attribute.ValueRO.attackAttribute.attackPower;
                for (int i = 0; i < buffHandler.Length; ++i)
                    att += buffHandler[i].buff.MonsterAttribute.attackAttribute.attackPower;
                return att;
            }
        }
        #endregion
        #region 防御属性
        /// <summary>
        /// 所有装备 技能 buff 基础属性的生命值
        /// </summary>
        /// <value></value>
        public float hp
        {
            get
            {
                float hpp = attribute.ValueRO.defenseAttribute.hp;
                for (int i = 0; i < buffHandler.Length; ++i)
                    hpp += buffHandler[i].buff.MonsterAttribute.defenseAttribute.hp;
                return hpp;
            }
        }
        #endregion


    }
}