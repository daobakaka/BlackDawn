using System;
using Unity.Entities;
using Unity.Mathematics;



namespace BlackDawn.DOTS
{
    /// <summary>
    /// 基础飞行道具
    /// </summary>
    public struct DirectFlightPropCmpt : IComponentData, IEnableableComponent
    {
        /// <summary>
        /// 飞行方向
        /// </summary>
        public float3 dir;
        /// <summary>
        /// 飞行速度
        /// </summary>
        public float speed;
        /// <summary>
        /// 存活时间
        /// </summary>
        public float originalSurvivalTime;
  

    }
    /// <summary>
    /// 飞行道具基本参数
    /// </summary>
    public struct FlightPropCmpt : IComponentData, IEnableableComponent
    {
        /// <summary>
        /// 攻击力
        /// </summary>
        public float atk;
        /// <summary>
        /// 射速
        /// </summary>
        public float rof;
        /// <summary>
        /// 弹容量
        /// </summary>
        public int magSize;
        /// <summary>
        /// 换弹时间
        /// </summary>
        public float reloadTime;
        /// <summary>
        /// 武器等级
        /// </summary>
        public int level;

    }
    /// <summary>
    /// 飞行道具伤害参数封装，包含物理瞬时伤害、5 种元素瞬时伤害和 6 种持续性伤害
    /// </summary>
    [Serializable]
    public struct FlightPropDamageCalPar:IComponentData, IEnableableComponent
    {

        public Entity heroRef;
        /// <summary>物理瞬时带暴击伤害</summary>
        public float instantPhysicalDamage;

        // —— 元素瞬时伤害 ——
        /// <summary>冰霜瞬时伤害</summary>
        public float frostDamage;
        /// <summary>闪电瞬时伤害</summary>
        public float lightningDamage;
        /// <summary>毒素瞬时伤害</summary>
        public float poisonDamage;
        /// <summary>暗影瞬时伤害</summary>
        public float shadowDamage;
        /// <summary>火焰瞬时伤害</summary>
        public float fireDamage;

        // —— 持续性（DOT）伤害 ——
        /// <summary>冰霜持续性伤害</summary>
        public float frostDotDamage;
        /// <summary>闪电持续性伤害</summary>
        public float lightningDotDamage;
        /// <summary>毒素持续性伤害</summary>
        public float poisonDotDamage;
        /// <summary>暗影持续性伤害</summary>
        public float shadowDotDamage;
        /// <summary>火焰持续性伤害</summary>
        public float fireDotDamage;
        /// <summary>流血持续性伤害，由物理伤害触发</summary>
        public float bleedDotDamage;
        /// <summary>
        ///判定几种暴击状态，用于生成不同颜色字体，和字体跳动动画,暂时用于伤害的后续处理，这里添加一个枚举用于英雄MONO类直接进行计算管理
        /// </summary>
        public bool critTriggered;
        public bool vulTriggered;
        public bool supTriggered;
        public bool dotCritTriggered;
        public bool elemCritTriggered;
        //伤害类型枚举
        public DamageTriggerType damageTriggerType;

        public float hitSurvivalTime;
        public bool  destory;

    }

    #region 敌人飞行道具标签
    /// <summary>
    /// 速度从怪物json属性中读取，或者干脆自定义
    /// </summary>
    public struct EnemyFlightProp : IComponentData
    {
        public float speed;
        public float3 dir;
        public float survivalTime;
        public bool destory;
        public Entity monsterRef;

    }

    #endregion



}