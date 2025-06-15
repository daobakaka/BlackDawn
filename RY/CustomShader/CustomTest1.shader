Shader "Unlit/URPUnlitInstance"
{
    Properties
    {
       _BaseColor ("Color", Color) = (0.5,1,1,1)
    }

    SubShader
    {
        // �̶��� Opaque ��Ⱦ����
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
            "Queue"="Geometry"
        }

        //=============================
        // 1) ShadowCaster Pass����ӰͶ�䣩
        //=============================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            HLSLPROGRAM
            #pragma multi_compile_instancing
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #ifndef LerpWhiteTo
            real3 LerpWhiteTo(real3 b, real t)
            {
                return lerp(real3(1.0,1.0,1.0), b, t);
            }
            #endif

           // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct VertexInput
            {
                float4 positionOS : POSITION;
            };

            struct VertexOutput
            {
                float4 positionCS : SV_POSITION;
            };

            VertexOutput ShadowVert(VertexInput input)
            {
                VertexOutput output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            float4 ShadowFrag(VertexOutput input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }


       Pass
{
    Name "CustomUniversal"
    Tags {"LightMode"="UniversalForward" }

    // ���û��ģʽ��ʵ��͸��Ч��
    // Blend SrcAlpha OneMinusSrcAlpha
    // ZWrite Off  // �������д�루��������ȷ����������Ҳ�ܿ�����

    HLSLPROGRAM
    #pragma multi_compile_instancing
    #pragma vertex ForwardVert
    #pragma fragment ForwardFrag
    #pragma target 4.5
   // #include "Assets/Scripts/CustomShader/LitInput.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

    uniform float4 _BaseColor;

      // ���� GPU Instancing ���Ի����������ڴ���ÿ��ʵ���� _BaseColorInstance
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColorInstance)
            UNITY_INSTANCING_BUFFER_END(Props)


    struct Attributes
    {
        float4 positionOS : POSITION;
        float2 uv         : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID

    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 uv         : TEXCOORD0;
        float3 positionWS : TEXCOORD1;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    Varyings ForwardVert(Attributes input)
    {
        Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
        float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
        output.positionCS = TransformWorldToHClip(positionWS);
        output.uv = input.uv;
        output.positionWS = positionWS;
        return output;
    }
    float4 ForwardFrag(Varyings input) : SV_Target
    {
       
         UNITY_SETUP_INSTANCE_ID(input);
      //  float4 texColor = float4(1,1,1,1);
      //        return texColor;
       // float4 finalColor = _BaseColor;
        // float4 finalColor = float4(1,1,1,1);
               // ��ʵ���������л�ȡÿ��ʵ���� _BaseColor
        float4 finalColor = UNITY_ACCESS_INSTANCED_PROP(Props, _BaseColorInstance);
                return finalColor;


    }
    ENDHLSL
}       
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
