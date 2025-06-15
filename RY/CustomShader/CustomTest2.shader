Shader "Unlit/URPUnlitInstance_Indirect"
{
    Properties
    {
        // Ĭ����ɫ�����ڱ༭����Ԥ��ʹ�ã���ʵ�����ݰ󶨺���Ҫ�� GPU ʵ�����ݿ���
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
            "Queue" = "Geometry"
        }

        //=============================
        // 1) ShadowCaster Pass����ӰͶ�䣩
        //=============================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

        //=============================
        // 2) CustomUniversal Pass��������ͨ����֧�ּ�ӻ��ƣ�
        //=============================
        Pass
        {
            Name "CustomUniversal"
            Tags { "LightMode"="UniversalForward" }

            // ��ע�⣺��ӻ���ͨ�����ⲿ ComputeBuffer �������ƻ��Ʋ�����
            // �˴�����ʹ�� Blend �� ZWrite ���ƣ��������ÿɸ������������

            HLSLPROGRAM
            #pragma vertex ForwardVert
            #pragma fragment ForwardFrag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // �����ṹ�������������ڴ���ÿ��ʵ���� object-to-world ����,����indrectDraw �ķ�ʽ��Ҫά��һ�����߶��StructuredBuffer�Ľṹ����������
            StructuredBuffer<float4x4> unity_ObjectToWorldBuffer;
            // �����ṹ�������������ڴ���ÿ��ʵ������ɫ
            StructuredBuffer<float4> unity_InstanceColorBuffer;

            // ��������ṹ
            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            // ��������ṹ��ͬʱ���� SV_InstanceID��ʵ��������
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                uint instanceID   : SV_InstanceID;
            };

            // ������ɫ�������� SV_InstanceID ��ʵ�����ݻ������ж�ȡ��Ӧ�� object-to-world ����
            Varyings ForwardVert(Attributes input, uint instanceID : SV_InstanceID)
            {
                Varyings output;
                // ��ʵ�����ݻ������ж�ȡ��ǰʵ���� object-to-world ����
                float4x4 obj2world = unity_ObjectToWorldBuffer[instanceID];
                // ������ÿռ�λ��
                output.positionCS = TransformWorldToHClip(mul(obj2world, input.vertex).xyz);
                output.uv = input.uv;
                // ��ȡ����ռ�λ�ã����ڿ��ܵĺ������㣩
                output.positionWS = mul(obj2world, input.vertex).xyz;
                output.instanceID = instanceID;
                return output;
            }

            // ƬԪ��ɫ������ʵ�����ݻ������ж�ȡÿ��ʵ����Ӧ����ɫ
            float4 ForwardFrag(Varyings input) : SV_Target
            {
                // ͨ�� SV_InstanceID ��ȡ��ǰʵ������ɫ
                float4 finalColor = unity_InstanceColorBuffer[input.instanceID];
                return finalColor;
            }
            ENDHLSL
        }
    }
    // Fallback ���ڵ���ǰ SRP ��֧��ʱ��ʾ������Ϣ
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
