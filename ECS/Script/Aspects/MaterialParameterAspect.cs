using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


namespace BlackDawn.DOTS
{
    /// <summary>
    /// 着色器参数调整集,注意调整shader的signal精度， half 不可使用
    /// </summary>
    public readonly partial struct RenderParameterAspect : IAspect
    {
        public readonly RefRW<UnderAttackColor> UnderAttack;
        public readonly RefRW<EmissionColor> Emission;
        public readonly RefRW<FireIntensity> Fire;
        public readonly RefRW<DissolveEffect> Dissolve;
        public readonly RefRW<FrostIntensity> Frost;
        public readonly RefRW<ConeStrength> Cone;
        public readonly RefRW<LightningIntensity> Lighting;
        public readonly RefRW<PoisoningIntensity> Poisoning;
        public readonly RefRW<DarkShadowIntensity> DarkShadow;
        public readonly RefRW<AlphaIntensity> Alpha;
        public readonly RefRW<RandomOffset> RandomOffset;

        //渲染随机数,这是单独的非材质暴露参数，因此用原英意标识
        public readonly RefRW<RenderRngState> RngState;
        

        /// <summary>
        /// 将所有通道重置为 0／初始值
        /// </summary>
        public void ResetAll()
        {
            UnderAttack.ValueRW.Value = float4.zero;
            Emission.ValueRW.Value = float4.zero;
            Fire.ValueRW.Value = 0f;
            Dissolve.ValueRW.Value = 0f;
            Frost.ValueRW.Value = 0f;
            Cone.ValueRW.Value = 0.2f;
            Lighting.ValueRW.Value = 0f;
            Poisoning.ValueRW.Value = 0f;
            DarkShadow.ValueRW.Value = 0f;
            Alpha.ValueRW.Value = 1f;
            RandomOffset.ValueRW.Value = float4.zero;
            RngState.ValueRW.rngState = 999;
        }
    }


    /// <summary>
    /// 飘字 Shader 参数调整集
    /// </summary>
    public readonly partial struct DamageTextMaterialAspect : IAspect
    {
        public readonly RefRW<TextColor> TextColor;
        public readonly RefRW<Char1UVRect> Char1;
        public readonly RefRW<Char2UVRect> Char2;
        public readonly RefRW<Char3UVRect> Char3;
        public readonly RefRW<Char4UVRect> Char4;
        public readonly RefRW<Char5UVRect> Char5;
        public readonly RefRW<Char6UVRect> Char6;

        public readonly RefRW<TextOffset> Offset;
        public readonly RefRW<TextScale> Scale;
        public readonly RefRW<TextStartTime> StartTime;

        //通用偏移变量，封装在Aspect中
        public readonly RefRW<RandomOffset> RandomOffset;

        //渲染随机数,这是单独的非材质暴露参数，因此用原英意标识
        public readonly RefRW<RenderRngState> RngState;
        /// <summary>
        /// 将所有通道重置为默认值
        /// </summary>
        public void ResetAll()
        {
            TextColor.ValueRW.Value = new float4(1, 1, 1, 1);

            Char1.ValueRW.Value = float4.zero;
            Char2.ValueRW.Value = float4.zero;
            Char3.ValueRW.Value = float4.zero;
            Char4.ValueRW.Value = float4.zero;
            Char5.ValueRW.Value = float4.zero;
            Char6.ValueRW.Value = float4.zero;

            Offset.ValueRW.Value = float2.zero;
            Scale.ValueRW.Value = 0f;
            StartTime.ValueRW.Value = 0f;
            RandomOffset.ValueRW.Value = float4.zero;
            RngState.ValueRW.rngState = 999;
        }
    }


    /// <summary>
    /// 火焰dot
    /// </summary>
    //public readonly partial struct DotFireAspect : IAspect
    //{
    //    public readonly RefRW<FireXRandomOffset> OffsetX;
    //    public readonly RefRW<FireYRandomOffset> OffsetY;


    //}

}