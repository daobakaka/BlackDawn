using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;


namespace BlackDawn.DOTS
{
    /// <summary>
    /// Ӣ�����
    /// </summary>
    public struct HeroParameters : IComponentData
    {



    }
    /// <summary>
    /// ����������
    /// </summary>
    public class MaterialPropertyBlockComponent : IComponentData
    {
        public MeshRenderer Renderer;
        public MaterialPropertyBlock MPB;
    }

    /// <summary>
    /// �˺�������Ⱦ���
    /// </summary>

    [Serializable]
    public struct DamageTextComponent : IComponentData
    {
        public float StartTime;
        public float ActiveDuration;
        public float TotalDuration;

        public float ScaleMultiplier;
        //Y��ƫ���ٶ�
        public float OffsetSpeed;
        //X�ᶶ�����
        public float ShakeAmountX;

        public float4 TextColor;

        public float4 Char1UVRect;
        public float4 Char2UVRect;
        public float4 Char3UVRect;
        public float4 Char4UVRect;
        public float4 Char5UVRect;
        public float4 Char6UVRect;
    }


    /// <summary>
    /// ������ɫ
    /// </summary>
    [MaterialProperty("_UnderAttackColor")]
    public struct UnderAttackColor : IComponentData
    {
        public float4 Value;
    }
    /// <summary>
    /// ��������ɫ
    /// </summary>
    [MaterialProperty("_FresnelColor")]
    public struct FresnelColor : IComponentData
    {
        public float4 Value;
    }

    /// <summary>
    /// �Է�����ɫ
    /// </summary>
    [MaterialProperty("_EmissionColor")]
    public struct EmissionColor : IComponentData
    {
        public float4 Value;
    }
    /// <summary>
    /// ����ǿ��
    /// </summary>

    [MaterialProperty("_FireIntensity")]
    public struct FireIntensity : IComponentData
    {
        public float Value;
    }
    /// <summary>
    /// �ܽ�Ч��
    /// </summary>
    [MaterialProperty("_DissolveEffect")]
    public struct DissolveEffect : IComponentData
    {
        public float Value;
    }
    /// <summary>
    /// ��������
    /// </summary>

    [MaterialProperty("_FrostIntensity")]
    public struct FrostIntensity : IComponentData
    {
        public float Value;
    }
    /// <summary>
    /// ׶��ǿ��
    /// </summary>
    [MaterialProperty("_ConeStrength")]
    public struct ConeStrength : IComponentData
    {
        public float Value;
    }
    /// <summary>
    /// ����ǿ��
    /// </summary>

    [MaterialProperty("_LightningIntensity")]
    public struct LightningIntensity : IComponentData
    {
        public float Value;
    }
    /// <summary>
    ///�ж�ǿ��
    /// </summary>
    [MaterialProperty("_PoisoningIntensity")]
    public struct PoisoningIntensity : IComponentData
    {
        public float Value;
    }
    /// <summary>
    /// ��Ӱǿ��
    /// </summary>
    [MaterialProperty("_DarkShadowIntensity")]
    public struct DarkShadowIntensity : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// ͸����
    /// </summary>
    [MaterialProperty("_AlphaIntensity")]
    public struct AlphaIntensity : IComponentData
    {
        public float Value;
    }

    public struct FireEffects : IComponentData
    {
        public float survivalTime;
        public bool destory;
    }



    #region  �˺�Ʈ�ִ���ģ��
    /// <summary>
    /// �ı���ɫ
    /// </summary>
    [MaterialProperty("_TextColor")]
    public struct TextColor : IComponentData
    {
        public float4 Value; // RGBA
    }


    /// <summary>
    /// �ַ�1 UV Rect
    /// </summary>
    [MaterialProperty("_Char1UVRect")]
    public struct Char1UVRect : IComponentData
    {
        public float4 Value; // uMin, vMin, uMax, vMax
    }

    /// <summary>
    /// �ַ�2 UV Rect
    /// </summary>
    [MaterialProperty("_Char2UVRect")]
    public struct Char2UVRect : IComponentData
    {
        public float4 Value;
    }

    /// <summary>
    /// �ַ�3 UV Rect
    /// </summary>
    [MaterialProperty("_Char3UVRect")]
    public struct Char3UVRect : IComponentData
    {
        public float4 Value;
    }

    /// <summary>
    /// �ַ�4 UV Rect
    /// </summary>
    [MaterialProperty("_Char4UVRect")]
    public struct Char4UVRect : IComponentData
    {
        public float4 Value;
    }

    /// <summary>
    /// �ַ�5 UV Rect
    /// </summary>
    [MaterialProperty("_Char5UVRect")]
    public struct Char5UVRect : IComponentData
    {
        public float4 Value;
    }

    /// <summary>
    /// �ַ�6 UV Rect
    /// </summary>
    [MaterialProperty("_Char6UVRect")]
    public struct Char6UVRect : IComponentData
    {
        public float4 Value;
    }

    /// <summary>
    /// UV ƫ��
    /// </summary>
    [MaterialProperty("_Offset")]
    public struct TextOffset : IComponentData
    {
        public float2 Value;
    }

    /// <summary>
    /// ����
    /// </summary>
    [MaterialProperty("_Scale")]
    public struct TextScale : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// ��shaderGrapha ���ۼӵĳ���ʱ��Buffer������ʵ��ЧGPU�Զ�����
    /// </summary>
    [MaterialProperty("_StartTime")]
    public struct TextStartTime : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// ͳһ���ƫ�Ʊ���
    /// </summary>
    [MaterialProperty("_RandomOffset")]
    public struct RandomOffset : IComponentData
    {
        public float4 Value;

    }


    /// <summary>
    /// ����ƫ�Ʊ��������Ӧ���������ò���ҵĹ������ɫ����֧�֣�
    /// </summary>
    [MaterialProperty("_RandomOffset")]
    public struct FireRandomOffset : IComponentData
    {
        public float4 Value;

    }




    /// <summary>
    /// ��˪ƫ�Ʊ����
    /// </summary>
    [MaterialProperty("_RandomOffset")]
    public struct FrostRandomOffset : IComponentData
    {
        public float4 Value;

    }

    /// <summary>
    /// ����ƫ�Ʊ��������Ӧ�������
    /// </summary>
    [MaterialProperty("_RandomOffset")]
    public struct LightningRandomOffset : IComponentData
    {
        public float4 Value;

    }

    /// <summary>
    /// �ж�ƫ�Ʊ����
    /// </summary>
    [MaterialProperty("_RandomOffset")]
    public struct PoisoningRandomOffset : IComponentData
    {
        public float4 Value;

    }

    /// <summary>
    /// ��Ӱƫ�Ʊ���
    /// </summary>
    [MaterialProperty("_RandomOffset")]
    public struct DarkShadowRandomOffset : IComponentData
    {
        public float4 Value;

    }

    /// <summary>
    /// ��Ѫƫ�Ʊ����
    /// </summary>
    [MaterialProperty("_RandomOffset")]
    public struct BleedRandomOffset : IComponentData
    {
        public float4 Value;

    }

    /// <summary>
    ///��Ⱦϵͳר�õ�rng����
    /// </summary>
    public struct RenderRngState : IComponentData
    {
        /// <summary>RNGState,�������ר��Ϊ��Ⱦ����</summary>
        public uint rngState;
    }


    /// <summary>
    /// �����ж��Ƿ���������Ⱦ
    /// </summary>
    public struct EnableTextRender : IComponentData, IEnableableComponent
    { }
    #endregion




}