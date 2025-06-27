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
            }
        }

    }
    /// <summary>
    /// ��ײ���
    /// </summary>
    public struct EnableHeroSkillsDamageOverTimeSystemTag : IComponentData,IEnableableComponent { };
    /// <summary>
    /// ������
    /// </summary>
    public struct EnableDetectionSystemTag:IComponentData, IEnableableComponent { };
    /// <summary>
    /// ���е����˺�
    /// </summary>
    public struct EnablePropDamageSystemTag : IComponentData, IEnableableComponent { };

    /// <summary>
    /// ���е���Mono
    /// </summary>
    public struct EnablePropMonoSystemTag :IComponentData, IEnableableComponent { };

    /// <summary>
    ///������Ⱦ����
    /// </summary>
    public struct EnableRenderEffectSystemTag : IComponentData, IEnableableComponent { };

    /// <summary>
    /// ������������
    /// </summary>
    public struct EnableActionSystemTag : IComponentData, IEnableableComponent { };

    /// <summary>
    /// ���˷��е����˺�
    /// </summary>
    public struct EnableEnemyPropDamageSystemTag : IComponentData, IEnableableComponent { };

    /// <summary>
    /// ���˷��е���Mono
    /// </summary>
    public struct EnableEnemyPropMonoSystemTag : IComponentData, IEnableableComponent { };


    /// <summary>
    /// ��ײ�˺���������
    /// </summary>
    public struct EnableAttackRecordBufferSystemTag: IComponentData, IEnableableComponent { };

    /// <summary>
    /// ��������״̬�߼�
    /// </summary>
    public struct EnableMonsterMonoSystemTag : IComponentData, IEnableableComponent { };

    /// <summary>
    /// �����ս�ͻ�����ײ�˺�
    /// </summary>
    public struct EnableEnemyBaseDamageSystemTag : IComponentData, IEnableableComponent { };


    /// <summary>
    /// ����ϵͳ
    /// </summary>
    public struct EnableBehaviorControlSystemTag :IComponentData,IEnableableComponent{ };

    /// <summary>
    /// Ӣ�ۼ��ܼ����˺�����ϵͳ
    /// </summary>
    public struct EnableHeroSkillsDamageSystemTag : IComponentData, IEnableableComponent { };


    /// <summary>
    /// Ӣ�ۼ���Monoϵͳ
    /// </summary>
    public struct EnableHeroSkillsMonoSystemTag : IComponentData, IEnableableComponent { };


    /// <summary>
    /// DoT�˺�����ϵͳ
    /// </summary>
    public struct EnableDotDamageSystemTag : IComponentData, IEnableableComponent { };

    /// <summary>
    /// �ر����˺� ���ּ����ϵͳ���編��ڶ��� ����,���ꡢ�������׵ȣ�
    /// </summary>
    public struct EnableHeroSpecialSkillsDamageSystemTag : IComponentData, IEnableableComponent { };


    /// <summary>
    /// ��Χ�����ܲ��м���ǩ����⡢ ��Χ���ܣ�
    /// </summary>
    public struct EnableOverlapDetectionSystemTag : IComponentData, IEnableableComponent { };

}