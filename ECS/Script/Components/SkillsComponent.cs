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

        // ���� ��ʱ�ԣ��������ԣ� ����
        /// <summary>��ʱ�Կ�������</summary>
        public float tempFreeze;
        public float tempStun;
        public float tempFear;
        public float tempRoot;

        /// <summary>���ڼ��ܱ仯�������˺��Σ����￴�����ڳػ��н��м���ģ�ҲӦ���ڼ��ܱ��������ӣ�Ĭ��ֵΪ1</summary>
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
        //���к���ʱ�䣬���ڹ�����ը������������(�����)
        public float hitSurvivalTime;
        public bool hit;
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
    /// ������ħ��
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
    /// ���� ��ħ��
    /// </summary>
    public struct SkillFrostTag : IComponentData, IEnableableComponent
    {
        public float3 originalPosition;
        public float tagSurvivalTime;
        public float speed;
        //�α����
        public float scaleChangePar;
        //����ڶ��׶�
        public bool enableSecond;
        //��������ִ���߼�
        public bool enableSpecialEffect;
        //�˺��仯����,Ĭ��Ϊ0���ⲿ��ֵ1+
        public float skillDamageChangeParTag;
        //���м��ʱ��
        public float hitIntervalTime;
        //���д���
        public int hitCount;
        //��Ƭ����
        public int shrapnelCount;


    }
    /// <summary>
    /// ������Ƭ��ǩ
    /// </summary>
    public struct SkillFrostShrapneTag : IComponentData
    {
        public float tagSurvivalTime;
        public float speed;
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
    /// ����Ŀ���ʱ�� ����dot���䣿6��,����̫���ᳬ��16KB�޶���Χ
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
    /// ������׼�����Ⱦ��ǩ
    /// </summary>
    public struct SkillArcaneCircleSecondRenderTag : IComponentData, IEnableableComponent { }
    /// <summary>
    /// ���׷����ǩ
    /// </summary>
    public struct SkillArcaneCircleThirdTag : IComponentData, IEnableableComponent { }


    /// <summary>
    /// Ԫ�ع���
    /// </summary>
    public struct SkillElementResonanceTag : IComponentData 
    {
        public float tagSurvivalTime;

        public bool enableSecondA;
        //���ر仯���ӣ����ܳ�ʼ��ʱ����
        public float secondDamagePar;

        public bool enableSecondB;
        //���ر仯���ӣ����ܳ�ʼ��ʱ����
        public float thridDamagePar;

    }
    /// <summary>
    /// ��������
    /// </summary>
    public struct SkillElectroCageTag : IComponentData
    {


        public float tagSurvivalTime;
        //�α����
        public float scaleChangePar;
        public bool enableSecondA;
        public bool enableSecondB;
        //���ʱ��
        public float intervalTimer;
        //�ڲ����ʱ��
        public float timerA;
        public float timerB;
        //�˺��仯����,Ĭ��Ϊ0���ⲿ��ֵ1+
        public float skillDamageChangeParTag;
        //������ӵȼ���ǩ�����׼��ܿ۳�����ֵ���ȼ��й�
        public int level;
        //��������
        public int StackCount;

    }
    /// <summary>
    /// ��������2���ױ���1��
    /// </summary>
    public struct SkillElectroCageScoendTag : IComponentData
    {

        public float tagSurvivalTime;

    }
    /// <summary>
    /// ��������,���Թرգ��Ϳ���ȡ����ײ���ռ������򿪶��׶εı�ըЧ�������¸�ֵ�˺�
    /// </summary>
    public struct SkillMineBlastTag : IComponentData,IEnableableComponent
    {


        public float tagSurvivalTime;
        //�α����
        public float scaleChangePar;

        //�˺��仯����,Ĭ��Ϊ0���ⲿ��ֵ1+
        public float skillDamageChangeParTag;
        //������ӵȼ���ǩ�����׼��ܿ۳�����ֵ���ȼ��й�
        public int level;
  
    }

    /// <summary>
    /// ��������ը��ı�ǩ�� �����Ƴ� ԭʼ��ǩ�ĵ�����ײ���Ч��
    /// </summary>
    public struct SkillMineBlastExplosionTag : IComponentData, IEnableableComponent
    {

        public float tagSurvivalTime;
        //��ը����ʱ��
        public float tagSurvivalTimeSecond;
        //�α����
        public float scaleChangePar;
        public bool enableSecondA;
        public bool enableSecondB;
        public bool enableSecondC;
        //�˺��仯����,Ĭ��Ϊ0���ⲿ��ֵ1+
        public float skillDamageChangeParTag;
        //��ʼ�ڶ��׶�
        public bool startSecondA;
        //�ȼ�
        public int level;
    }



}



