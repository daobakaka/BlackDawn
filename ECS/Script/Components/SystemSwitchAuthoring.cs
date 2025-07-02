using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace BlackDawn.DOTS
{
    public class SystemSwitchAuthoring : MonoBehaviour
    {
        public bool enableHeroSkillsDamageOverTimeSystem;
        public bool enableDetectionSystem;
        public bool enablePropDamageSystem;
        public bool enablePropMonoSystem;
        public bool enableRenderEffectSystem;
        public bool enableActionSystem;
        public bool enableEnemyPropDamageSystem;
        public bool enableEnemyPropMonoSystem;
        public bool enableAttackRecordBufferSystem;
        public bool enableMonsterMonoSystem;
        public bool enableEnemyBaseDamageSystem;
        public bool enableBehaviorControlSystem;
        public bool enableHeroSkillsDamageSystem;
        public bool enableHeroSkillsMonoSystem;
        public bool enableDotDamageSystem;
        public bool enableHeroSpecialSkillsDamageSystem;
        public bool enableOverlapDetectionSystem;
        public bool enableHeoSkillsDamageBurstSystem;
        public class SystemSwitchBaker : Baker<SystemSwitchAuthoring>
        {
            public override void Bake(SystemSwitchAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.None);
                
                if (authoring.enableHeroSkillsDamageOverTimeSystem)
                AddComponent(entity, new EnableHeroSkillsDamageOverTimeSystemTag());
                if (authoring.enableDetectionSystem)
                AddComponent(entity, new EnableDetectionSystemTag());
                if (authoring.enablePropDamageSystem)
                AddComponent(entity,new EnablePropDamageSystemTag());
                if (authoring.enablePropMonoSystem)
                    AddComponent(entity, new EnablePropMonoSystemTag());
                if (authoring.enableRenderEffectSystem)
                AddComponent(entity,new EnableRenderEffectSystemTag());
                if(authoring.enableActionSystem)
                AddComponent(entity,new EnableActionSystemTag());
                if (authoring.enableEnemyPropDamageSystem)
                AddComponent(entity, new EnableEnemyPropDamageSystemTag());
                if (authoring.enableEnemyPropDamageSystem)
                    AddComponent(entity, new EnableEnemyPropMonoSystemTag());
                if (authoring.enableAttackRecordBufferSystem)
                AddComponent(entity, new EnableAttackRecordBufferSystemTag());
                if (authoring.enableMonsterMonoSystem)
                    AddComponent(entity, new EnableMonsterMonoSystemTag());
                if (authoring.enableEnemyBaseDamageSystem)
                    AddComponent(entity, new EnableEnemyBaseDamageSystemTag());
                if(authoring.enableBehaviorControlSystem)
                    AddComponent(entity, new EnableBehaviorControlSystemTag());
                if (authoring.enableHeroSkillsDamageSystem)
                    AddComponent(entity, new EnableHeroSkillsDamageSystemTag());
                if (authoring.enableHeroSkillsMonoSystem)
                    AddComponent(entity, new EnableHeroSkillsMonoSystemTag());
                if(authoring.enableDotDamageSystem)
                    AddComponent(entity,new EnableDotDamageSystemTag());
                if (authoring.enableHeroSpecialSkillsDamageSystem)
                    AddComponent(entity, new EnableHeroSpecialSkillsDamageSystemTag());
                if (authoring.enableOverlapDetectionSystem)
                    AddComponent(entity, new EnableOverlapDetectionSystemTag());
                if (authoring.enableHeoSkillsDamageBurstSystem)
                    AddComponent(entity, new EnableHeroSkillsDamageBurstSystemTag());
            }
        }

    }
    /// <summary>
    /// 碰撞检测
    /// </summary>
    public struct EnableHeroSkillsDamageOverTimeSystemTag : IComponentData,IEnableableComponent { };
    /// <summary>
    /// 玩家侦察
    /// </summary>
    public struct EnableDetectionSystemTag:IComponentData, IEnableableComponent { };
    /// <summary>
    /// 飞行道具伤害
    /// </summary>
    public struct EnablePropDamageSystemTag : IComponentData, IEnableableComponent { };

    /// <summary>
    /// 飞行道具Mono
    /// </summary>
    public struct EnablePropMonoSystemTag :IComponentData, IEnableableComponent { };

    /// <summary>
    ///启动渲染交互
    /// </summary>
    public struct EnableRenderEffectSystemTag : IComponentData, IEnableableComponent { };

    /// <summary>
    /// 启动动作交互
    /// </summary>
    public struct EnableActionSystemTag : IComponentData, IEnableableComponent { };

    /// <summary>
    /// 敌人飞行道具伤害
    /// </summary>
    public struct EnableEnemyPropDamageSystemTag : IComponentData, IEnableableComponent { };

    /// <summary>
    /// 敌人飞行道具Mono
    /// </summary>
    public struct EnableEnemyPropMonoSystemTag : IComponentData, IEnableableComponent { };


    /// <summary>
    /// 碰撞伤害计算锁定
    /// </summary>
    public struct EnableAttackRecordBufferSystemTag: IComponentData, IEnableableComponent { };

    /// <summary>
    /// 怪物死亡状态逻辑
    /// </summary>
    public struct EnableMonsterMonoSystemTag : IComponentData, IEnableableComponent { };

    /// <summary>
    /// 怪物近战和基础碰撞伤害
    /// </summary>
    public struct EnableEnemyBaseDamageSystemTag : IComponentData, IEnableableComponent { };


    /// <summary>
    /// 控制系统
    /// </summary>
    public struct EnableBehaviorControlSystemTag :IComponentData,IEnableableComponent{ };

    /// <summary>
    /// 英雄技能检测和伤害计算系统
    /// </summary>
    public struct EnableHeroSkillsDamageSystemTag : IComponentData, IEnableableComponent { };


    /// <summary>
    /// 英雄技能Mono系统
    /// </summary>
    public struct EnableHeroSkillsMonoSystemTag : IComponentData, IEnableableComponent { };


    /// <summary>
    /// DoT伤害计算系统
    /// </summary>
    public struct EnableDotDamageSystemTag : IComponentData, IEnableableComponent { };

    /// <summary>
    /// 特别技能伤害 表现及检测系统（如法阵第二阶 虹吸,毒雨、毒爆地雷等）
    /// </summary>
    public struct EnableHeroSpecialSkillsDamageSystemTag : IComponentData, IEnableableComponent { };


    /// <summary>
    /// 范围高性能并行检测标签（侦测、 范围技能）
    /// </summary>
    public struct EnableOverlapDetectionSystemTag : IComponentData, IEnableableComponent { };

    /// <summary>
    /// 爆发性技能标签
    /// </summary>
    public struct EnableHeroSkillsDamageBurstSystemTag : IComponentData, IEnableableComponent { };

}