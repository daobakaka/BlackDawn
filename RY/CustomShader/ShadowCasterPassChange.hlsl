#ifndef UNIVERSAL_SHADOW_CASTER_PASS_INCLUDED
#define UNIVERSAL_SHADOW_CASTER_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

// 由 Unity 填充：用于计算阴影偏移
float3 _LightDirection;
float3 _LightPosition;

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 texcoord     : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    // 始终传递 UV 以采样透明度
    float2 uv           : TEXCOORD0;
    float4 positionCS   : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// 将物体空间位置转换为剪裁空间位置，同时应用阴影偏移
float4 GetShadowPositionHClip(Attributes input)
{
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

#if _CASTING_PUNCTUAL_LIGHT_SHADOW
    // 对于点光和聚光灯，使用从光源位置到顶点的方向
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    // 定向光直接使用全局光照方向
    float3 lightDirectionWS = _LightDirection;
#endif

    // 计算偏移后位置并转换到剪裁空间
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

#if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif

    return positionCS;
}

Varyings ShadowPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    // 直接传递 UV 用于采样 _BaseMap 的 alpha
    output.uv = input.texcoord;
    output.positionCS = GetShadowPositionHClip(input);
    return output;
}

// 修改后的片元着色器：
// 采样 _BaseMap 的 alpha 值，并直接输出一个遮挡因子（灰度值）
// 这样，即使物体透明也不会被 clip 掉，而是输出一个与 alpha 成比例的值
half4 ShadowPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);

    // 如果希望用 _BaseColor.a 来控制阴影，直接使用它：
    float alpha = _BaseColor.a;

    // 或者结合 _BaseMap 的采样结果，例如乘积：
    // float alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;

    half shadowFactor = saturate(alpha);

#if defined(LOD_FADE_CROSSFADE)
        LODFadeCrossFade(input.positionCS);
#endif

    return half4(shadowFactor, shadowFactor, shadowFactor, 1);
}

#endif
