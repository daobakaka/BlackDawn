using System.Collections;
using System.Collections.Generic;
using BlackDawn.DOTS;
using Unity.Entities;
using UnityEngine;

namespace BlackDawn
{
    public class Gun : MonoBehaviour
    {
        protected Buff buff;
        protected Entity bulletPrefab;
        /// <summary>
        /// 枪的等级
        /// </summary>
        protected int Gunlevel = 0;
        /// <summary>
        /// 枪的标识
        /// </summary>
        //protected enum Gun

        // Start is called before the first frame update
        public virtual void Start()
        {
            // var entiMager = World.DefaultGameObjectInjectionWorld.EntityManager;
            // var query = entiMager.CreateEntityQuery(typeof(PrefabsComponentData));
            // if (query.TryGetSingleton<PrefabsComponentData>(out var prefabs))
            //     bulletPrefab = prefabs.FlightProp;
        }

        // Update is called once per frame
        public virtual void Update()
        {

        }
        /// <summary>
        /// 初始化
        /// </summary>
        public void InitGun()
        {
            buff = new Buff
            {
                //可能需要buff标识
                //public enum e;
                /// <summary>
                /// 是否永久
                /// </summary>
                isForever = true,

                /// <summary>
                /// 持续时间
                /// </summary>
                duration = 0f,

                /// <summary>
                /// 间隔时间
                /// </summary>
                interval = 0f,

                /// <summary>
                /// 元素伤害类型
                /// </summary>
                elementType = ElementType.None,
                /// <summary>

                level = 0,

                /// <summary>
                /// buff的各回调点
                /// <summary>

                /// buff基础属性
                /// </summary>
                /// <value></value>
                heroAttribute = new HeroAttributeCmpt
                {
                    baseAttribute = new BaseAttribute
                    {
                        agility = 2f + Gunlevel * 0.1f,
                        //magSize = 12 + Gunlevel * 2,
                    },
                    attackAttribute = new AttackAttribute
                    {
                        attackPower = 5f + Gunlevel * 1,
                        //...
                    },
                    defenseAttribute = new DefenseAttribute
                    {

                    },
                    gainAttribute = new GainAttribute
                    {

                    },
                    lossPoolAttribute = new LossPoolAttribute
                    {

                    },

                }



            };
        }
        /// <summary>
        /// 枪的展示效果
        /// </summary>
        public virtual void DisplayEffect()
        {

        }
        /// <summary>
        /// 枪的射击逻辑
        /// </summary>
        public virtual void ShootFunction()
        {

        }
        /// <summary>
        /// 攻击特效
        /// </summary>
        public virtual void AttackEffect()
        {

        }
        /// <summary>
        /// 终极特效
        /// </summary>
        public virtual void UltimateEffect()
        {

        }
    
    }
}
