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
    /// 英雄组件
    /// </summary>
    public struct HeroParameters : IComponentData
    {



    }
    /// <summary>
    /// 测试引用类
    /// </summary>
    public class MaterialPropertyBlockComponent : IComponentData
    {
        public MeshRenderer Renderer;
        public MaterialPropertyBlock MPB;
    }

    /// <summary>
    /// 伤害数字渲染组件
    /// </summary>

    [Serializable]
    public struct DamageTextComponent : IComponentData
    {
        public float StartTime;
        public float ActiveDuration;
        public float TotalDuration;

        public float ScaleMultiplier;
        //Y轴偏移速度
        public float OffsetSpeed;
        //X轴抖动振幅
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
    /// 攻击变色
    /// </summary>
    [MaterialProperty("_UnderAttackColor")]
    public struct UnderAttackColor : IComponentData
    {
        public float4 Value;
    }
    /// <summary>
    /// 菲涅尔颜色
    /// </summary>
    [MaterialProperty("_FresnelColor")]
    public struct FresnelColor : IComponentData
    {
        public float4 Value;
    }

    /// <summary>
    /// 自发光颜色
    /// </summary>
    [MaterialProperty("_EmissionColor")]
    public struct EmissionColor : IComponentData
    {
        public float4 Value;
    }
    /// <summary>
    /// 火焰强度
    /// </summary>

    [MaterialProperty("_FireIntensity")]
    public struct FireIntensity : IComponentData
    {
        public float Value;
    }
    /// <summary>
    /// 溶解效果
    /// </summary>
    [MaterialProperty("_DissolveEffect")]
    public struct DissolveEffect : IComponentData
    {
        public float Value;
    }
    /// <summary>
    /// 冰冻参数
    /// </summary>

    [MaterialProperty("_FrostIntensity")]
    public struct FrostIntensity : IComponentData
    {
        public float Value;
    }
    /// <summary>
    /// 锥体强度
    /// </summary>
    [MaterialProperty("_ConeStrength")]
    public struct ConeStrength : IComponentData
    {
        public float Value;
    }
    /// <summary>
    /// 闪电强度
    /// </summary>

    [MaterialProperty("_LightningIntensity")]
    public struct LightningIntensity : IComponentData
    {
        public float Value;
    }
    /// <summary>
    ///中毒强度
    /// </summary>
    [MaterialProperty("_PoisoningIntensity")]
    public struct PoisoningIntensity : IComponentData
    {
        public float Value;
    }
    /// <summary>
    /// 暗影强度
    /// </summary>
    [MaterialProperty("_DarkShadowIntensity")]
    public struct DarkShadowIntensity : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// 透明度
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



    #region  伤害飘字处理模块
    /// <summary>
    /// 文本颜色
    /// </summary>
    [MaterialProperty("_TextColor")]
    public struct TextColor : IComponentData
    {
        public float4 Value; // RGBA
    }


    /// <summary>
    /// 字符1 UV Rect
    /// </summary>
    [MaterialProperty("_Char1UVRect")]
    public struct Char1UVRect : IComponentData
    {
        public float4 Value; // uMin, vMin, uMax, vMax
    }

    /// <summary>
    /// 字符2 UV Rect
    /// </summary>
    [MaterialProperty("_Char2UVRect")]
    public struct Char2UVRect : IComponentData
    {
        public float4 Value;
    }

    /// <summary>
    /// 字符3 UV Rect
    /// </summary>
    [MaterialProperty("_Char3UVRect")]
    public struct Char3UVRect : IComponentData
    {
        public float4 Value;
    }

    /// <summary>
    /// 字符4 UV Rect
    /// </summary>
    [MaterialProperty("_Char4UVRect")]
    public struct Char4UVRect : IComponentData
    {
        public float4 Value;
    }

    /// <summary>
    /// 字符5 UV Rect
    /// </summary>
    [MaterialProperty("_Char5UVRect")]
    public struct Char5UVRect : IComponentData
    {
        public float4 Value;
    }

    /// <summary>
    /// 字符6 UV Rect
    /// </summary>
    [MaterialProperty("_Char6UVRect")]
    public struct Char6UVRect : IComponentData
    {
        public float4 Value;
    }

    /// <summary>
    /// UV 偏移
    /// </summary>
    [MaterialProperty("_Offset")]
    public struct TextOffset : IComponentData
    {
        public float2 Value;
    }

    /// <summary>
    /// 缩放
    /// </summary>
    [MaterialProperty("_Scale")]
    public struct TextScale : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// 与shaderGrapha 中累加的程序时间Buffer变量，实现效GPU自动计算
    /// </summary>
    [MaterialProperty("_StartTime")]
    public struct TextStartTime : IComponentData
    {
        public float Value;
    }

    /// <summary>
    /// 统一随机偏移变量
    /// </summary>
    [MaterialProperty("_RandomOffset")]
    public struct RandomOffset : IComponentData
    {
        public float4 Value;

    }


    /// <summary>
    /// 火焰偏移便变量，对应火焰组件，貌似我的广告牌着色器不支持？
    /// </summary>
    [MaterialProperty("_RandomOffset")]
    public struct FireRandomOffset : IComponentData
    {
        public float4 Value;

    }




    /// <summary>
    /// 冰霜偏移便变量
    /// </summary>
    [MaterialProperty("_RandomOffset")]
    public struct FrostRandomOffset : IComponentData
    {
        public float4 Value;

    }

    /// <summary>
    /// 闪电偏移便变量，对应闪电组件
    /// </summary>
    [MaterialProperty("_RandomOffset")]
    public struct LightningRandomOffset : IComponentData
    {
        public float4 Value;

    }

    /// <summary>
    /// 中毒偏移便变量
    /// </summary>
    [MaterialProperty("_RandomOffset")]
    public struct PoisoningRandomOffset : IComponentData
    {
        public float4 Value;

    }

    
    /// <summary>
    /// 黑炎偏移便变量
    /// </summary>
    [MaterialProperty("_RandomOffset")]
    public struct BlackFrameRandomOffset : IComponentData
    {
        public float4 Value;

    }      
    /// <summary>
    /// 暗影偏移变量
    /// </summary>
    [MaterialProperty("_RandomOffset")]
    public struct DarkShadowRandomOffset : IComponentData
    {
        public float4 Value;

    }

    /// <summary>
    /// 流血偏移便变量
    /// </summary>
    [MaterialProperty("_RandomOffset")]
    public struct BleedRandomOffset : IComponentData
    {
        public float4 Value;

    }

    /// <summary>
    ///渲染系统专用的rng数据
    /// </summary>
    public struct RenderRngState : IComponentData
    {
        /// <summary>RNGState,单独组件专门为渲染定制</summary>
        public uint rngState;
    }


    /// <summary>
    /// 用于判断是否开启文字渲染
    /// </summary>
    public struct EnableTextRender : IComponentData, IEnableableComponent
    { }
    #endregion




}