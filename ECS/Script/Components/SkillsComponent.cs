using System;
using Unity.Entities;
using Unity.Mathematics;

namespace BlackDawn.DOTS
{
    /// <summary>
    /// �����˺�������װ����������˲ʱ�˺���5 ��Ԫ��˲ʱ�˺��� 6 �ֳ������˺�
    /// </summary>
    [Serializable]
    public struct SkillsDamageCalPar : IComponentData
    {

        public Entity heroRef;
        /// <summary>����˲ʱ�������˺�</summary>
        public float instantPhysicalDamage;

        // ���� Ԫ��˲ʱ�˺� ����
        /// <summary>��˪˲ʱ�˺�</summary>
        public float frostDamage;
        /// <summary>����˲ʱ�˺�</summary>
        public float lightningDamage;
        /// <summary>����˲ʱ�˺�</summary>
        public float poisonDamage;
        /// <summary>��Ӱ˲ʱ�˺�</summary>
        public float shadowDamage;
        /// <summary>����˲ʱ�˺�</summary>
        public float fireDamage;

        // ���� �����ԣ�DOT���˺� ����
        /// <summary>��˪�������˺�</summary>
        public float frostDotDamage;
        /// <summary>����������˺�</summary>
        public float lightningDotDamage;
        /// <summary>���س������˺�</summary>
        public float poisonDotDamage;
        /// <summary>��Ӱ�������˺�</summary>
        public float shadowDotDamage;
        /// <summary>����������˺�</summary>
        public float fireDotDamage;
        /// <summary>��Ѫ�������˺����������˺�����</summary>
        public float bleedDotDamage;


        /// <summary>���ڼ��ܱ仯�������˺�����</summary>
        public float damageChangePar;

        /// <summary>
        ///�ж����ֱ���״̬���������ɲ�ͬ��ɫ���壬��������������
        /// </summary>
        public bool critTriggered;
        public bool vulTriggered;
        public bool supTriggered;
        public bool dotCritTriggered;
        public bool elemCritTriggered;

        //�˺�����ö��
        public DamageTriggerType damageTriggerType;
        //���к���ʱ�䣬���ڹ�����ը������������
        public float hitSurvivalTime;
        //ԭʼ���ʱ��
        public float originalSurvivalTime;
        //���ٱ�ʶ
        public bool  destory;
 
        ///���������ǣ���ͱ�ը���Ա�ǩ
        public bool enablePull;
        public bool enableExplosion;


    }
    /// <summary>
    /// �ɹرձ�ǩ�����ǩ,��ʼ��һ��ʱ�䣬���ڴ���ж�,����ʹ�ñ�ǩ������ �ٶ�  ���� ���ʱ��
    /// </summary>
    public struct SkillPulseTag : IComponentData, IEnableableComponent
    { public float tagSurvivalTime;
      public float speed;
      //�α����
      public float scaleChangePar;
      public bool enableSecond;
        //�˺��仯����,Ĭ��Ϊ0���ⲿ��ֵ1+
     public float skillDamageChangeParTag;

    }

    /// <summary>
    /// �ɹر�������׶α�ը�����ǩ,���̳�IComponentData���Ϸ�
    /// </summary>
    public struct SkillPulseSecondExplosionRequestTag : IComponentData, IEnableableComponent { }



    /// <summary>
    /// ������
    /// </summary>
    public struct SkillDarkEnergyTag : IComponentData, IEnableableComponent
    {
        public float tagSurvivalTime;
        public float speed;
        //�α����
        public float scaleChangePar;
        public bool enableSecond;
        //��������ִ���߼�
        public bool enableSpecialEffect;
        //�˺��仯����,Ĭ��Ϊ0���ⲿ��ֵ1+
        public float skillDamageChangeParTag;

    }

    /// <summary>
    /// ������ ��������ת ģ��
    /// </summary>
    public struct SkillIceFireTag : IComponentData, IEnableableComponent
    {
        public float tagSurvivalTime;
        public float speed;
        public float radius;
        public float currentAngle;      // ��ǰ�Ƕȣ���λ������
        //�α��������һ��ڶ��׶μ������ڶ�̬�ı�Ĳ���
        public float originalScale;
        public float scaleChangePar;
        public bool enableSecond;
        //����״̬����ʱ�䣬 ����һ��״̬���������
        public float secondSurvivalTime;
        //��������ִ���߼�
        public bool enableSpecialEffect;
        //�˺��仯����,Ĭ��Ϊ0���ⲿ��ֵ1+
        public float skillDamageChangeParTag;

    }


    /// <summary>
    /// �ɹرձ�������׶α�ը�����ǩ
    /// </summary>
    public struct SkillIceFireSecondExplosionRequestTag : IComponentData, IEnableableComponent { }


    /// <summary>
    /// ���׿����Ϊ�ٶ�Ϊ0 �ľ�̬���е��ߣ�������һ�������ͷ�Ч������Ӧ���ı��߼�����Я�̴���
    /// </summary>
    public struct SkillThunderStrikeTag : IComponentData, IEnableableComponent
    {
        public float tagSurvivalTime;
        //�α����
        public float scaleChangePar;
        //�˺��仯����,Ĭ��Ϊ0���ⲿ��ֵ1+
        public float skillDamageChangeParTag;

    }

    /// <summary>
    /// �����ǳ�������������ߣ�ͬһʱ��ֻ�ܴ���һ����������ƻص���clsss�е�����ȡ ���ӿɹرձ�ǩ
    /// </summary>
    public struct SkillArcaneCircleTag : IComponentData
    {
        //����Ҫ��Ϊ�������ӵ�
        public Entity heroRef;

        public float tagSurvivalTime;
        //�α����
        public float scaleChangePar;
        public bool enableSecondA;
        public bool enableSecondB;

        //�˺��仯����,Ĭ��Ϊ0���ⲿ��ֵ1+
        public float skillDamageChangeParTag;
        //������ӵȼ���ǩ�����׼��ܿ۳�����ֵ���ȼ��й�
        public int level;

        //�ֶ��ر�
        public bool closed;

    }
    /// <summary>
    /// �������ܷ����ǩ������Ӧ����һ��buffer�����buffer������Ӳ�ͬ��link�����ڰ���صĲ�������
    /// ����Ŀ���ʱ�� ����dot���䣿6��
    /// </summary>
    [InternalBufferCapacity(1000)]
    public struct SkillArcaneCircleSecondBufferTag : IBufferElementData 
    
    {
        public Entity target;
        public float tagSurvivalTime;
        
    }
    /// <summary>
    /// ���������ռ���ײ��
    /// </summary>
    public struct SkillArcaneCircleSecondTag : IComponentData { }
    /// <summary>
    /// ���׷����ǩ
    /// </summary>
    public struct SkillArcaneCircleThirdTag : IComponentData, IEnableableComponent { }
}

