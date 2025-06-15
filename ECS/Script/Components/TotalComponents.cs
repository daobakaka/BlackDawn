using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using System;
using Unity.Physics;

namespace BlackDawn.DOTS
{


    /// ����״̬
    /// </summary>
    public struct LiveMonster : IComponentData, IEnableableComponent { }

    /// <summary>
    /// ��Ϊ��ǩ
    /// </summary>
    public enum EActionType
    {
        Idle,
        Run,
        Attack,
        Die

    }

    /// <summary>
    /// ���ڴ洢�������¼�����
    /// </summary>
    [Serializable]
    public struct TriggerPairData
    {
        public Entity EntityA;
        public Entity EntityB;

  
    }
    #region �����ǩ
    /// <summary>
    /// ��ǩ����������������Լ����Ŷ�����
    /// ɥʬ
    /// </summary>
    public struct MoZombieCmp : IComponentData, IEnableableComponent { }

    /// <summary>
    ///��Ȯ
    /// </summary>
    public struct MoAlbonoCmp : IComponentData, IEnableableComponent { }


    /// <summary>
    /// ��Ȯԭʼ��Ч�߶Ȳ���
    /// </summary>
    public struct MoAlbonoEffectsCmp : IComponentData
    {
        //����ԭʼ�ߴ�
       public float3 fireOringinalScale;
    
    
    }

    /// <summary>
    /// ��ֵ��������ڲ�ͬ��chunk�У� ��֤������ʱ������ٶ�
    /// </summary>
    public struct TTTTTTTTSSSSSSSS : ISharedComponentData { public int value; }
    /// <summary>
    ///����������
    /// </summary>
    public struct MoAlbonoUpperCmp : IComponentData, IEnableableComponent { }

    /// <summary>
    /// ����������ԭʼ��Ч�߶Ȳ���
    /// </summary>
    public struct MoAlbononUpperEffectsCmp : IComponentData
    {
        //����ԭʼ�ߴ�
       public float3 fireOringinalScale;

    }


    #endregion

    #region ��̬buffer����
    /// <summary>
    /// Buffer Ԫ�أ���¼һ�δ�����ײ�ġ���һ��ʵ�� + ����ƽ����
    /// </summary>
    [Serializable]
    [InternalBufferCapacity(100)]
    public struct NearbyHit : IBufferElementData
    {
        public Entity other;
        public float sqrDist;
    }


    /// <summary>
    /// �����¼
    /// </summary>
    [Serializable]
    [InternalBufferCapacity(50)] //Ĭ������50
    public struct HitRecord : IBufferElementData
    {
        public Entity other;
        //�¼���ǩ,�������
        public float timer;
        //ͨ���ж�,�ж� ��Ӱ���� �ȸ��ִ���Ч��
        public bool universalJudgment;
    }
    /// <summary>
    /// �Ӽ������ǩ
    /// </summary>
    public struct DetectorTag:IComponentData { };

    /// <summary>
    /// Ӣ�����˴����¼
    /// </summary>
    [Serializable]
    [InternalBufferCapacity(50)]
    public struct HeroHitRecord : IBufferElementData
    {
        public Entity other;
        //�¼���ǩ,�������
        public float timer;
    }



    #endregion


    /// <summary>
    /// ���ϵͳ
    /// </summary>
    public struct Detection_DefaultCmpt : IComponentData,IEnableableComponent
    {
        public Entity bufferOwner; // ����д�� NearbyHit ��ʵ��
    }

    public struct HeroAttackTarget : IComponentData
    {
        public Entity attackTarget;//����Ŀ��
    }

    /// <summary>
    /// Ӣ�������ʶ ȫ��Ψһ
    /// </summary>
    public struct HeroEntityMasterTag :IComponentData { }

    /// <summary>
    /// Ӣ�۷�֧��ʶ ��Ҫ����Ӣ�۷���
    /// </summary>
    public struct HeroEntityBrachTag : IComponentData { }
    /// <summary>
    /// ����������Ϣ
    /// </summary>
    public struct AnimationControllerData : IComponentData
    {
        public bool isAttack;
        public bool isFire;    
    }
    #region �������ͱ�ǩ
    /// <summary>
    /// ��ս����
    /// </summary>
    public struct AtMelee : IComponentData { };

    /// <summary>
    /// Զ�̹���
    /// </summary>
    public struct AtRanged : IComponentData { };
    /// <summary>
    /// ��Ϲ���
    /// </summary>
    public struct AtHybrid : IComponentData { };
    #endregion








    #region Ӣ������BUFF ��ǩ
    /// <summary>
    /// ��ʹ���ۺ��Եı�ǩ�� ����������SIMD ָ��Ľṹ
    /// </summary>
    [Serializable]
    public struct HeroIntgratedNoImmunityState : IComponentData
    {
        //���Ʒ�����
        public float controlNoImmunity;
        //�����˺������ߣ��� �����ܣ�
        public float inlineDamageNoImmunity;
        //dot�˺�������
        public float dotNoImmunity;
        // �����˺�������
        public float physicalDamageNoImmunity;
        //Ԫ���˺�������
        public float elementDamageNoImmunity;

        /// <summary>
        /// ���ã� ���ṩ�޲ι��캯��



    }
    
    
    
    
    
    /// <summary>
    /// ״̬��ʶ������־
    /// </summary>
    public struct HeroStateWillUnchained : IComponentData, IEnableableComponent { };


    /// <summary>
    /// ʥĸ���٣� ���������˺�
    /// </summary>
    public struct HeroStateDivineDescent : IComponentData, IEnableableComponent { };


    /// <summary>
    /// ��Ĭ���� ��������DOT���˺�
    /// </summary>

    public struct HeroStateSilentDomain : IComponentData, IEnableableComponent { };

    /// <summary>
    /// ���ܷ籩  �����ͷŲ����������������Լ��ܲ��������ľ���
    /// </summary>
    public struct HeroStatePsionicSurge : IComponentData, IEnableableComponent { };

    /// <summary>
    /// ��������  ��������ֱ�������˺�
    /// </summary>

    public struct HeroStateTitaniumShell : IComponentData, IEnableableComponent { };
    /// <summary>
    /// ���Ϲ���  ��������ֱ�ӷ����˺�
    /// </summary>

    public struct HeroStateForbiddenResonance : IComponentData, IEnableableComponent { };
    #endregion
}
