Shader "Unlit/URPUnlitInstance_Indirect"
{
    Properties
    {
        // 默认颜色，仅在编辑器中预览使用，当实例数据绑定后主要由 GPU 实例数据控制
        _BaseColor ("Color", Color) = (0.5,1,1,1)
    }

    SubShader
    {
        // 固定在 Opaque 渲染队列
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
            "Queue" = "Geometry"
        }

        //=============================
        // 1) ShadowCaster Pass（阴影投射）
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
        // 2) CustomUniversal Pass（主绘制通道，支持间接绘制）
        //=============================
        Pass
        {
            Name "CustomUniversal"
            Tags { "LightMode"="UniversalForward" }

            // 请注意：间接绘制通常由外部 ComputeBuffer 参数控制绘制参数，
            // 此处不再使用 Blend 和 ZWrite 控制，具体配置可根据需求调整。

            HLSLPROGRAM
            #pragma vertex ForwardVert
            #pragma fragment ForwardFrag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // 声明结构化缓冲区，用于传递每个实例的 object-to-world 矩阵,这种indrectDraw 的方式需要维护一个或者多个StructuredBuffer的结构缓冲区数组
            StructuredBuffer<float4x4> unity_ObjectToWorldBuffer;
            // 声明结构化缓冲区，用于传递每个实例的颜色
            StructuredBuffer<float4> unity_InstanceColorBuffer;

            // 顶点输入结构
            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            // 顶点输出结构，同时传递 SV_InstanceID（实例索引）
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                uint instanceID   : SV_InstanceID;
            };

            // 顶点着色器：利用 SV_InstanceID 从实例数据缓冲区中读取对应的 object-to-world 矩阵
            Varyings ForwardVert(Attributes input, uint instanceID : SV_InstanceID)
            {
                Varyings output;
                // 从实例数据缓冲区中读取当前实例的 object-to-world 矩阵
                float4x4 obj2world = unity_ObjectToWorldBuffer[instanceID];
                // 计算剪裁空间位置
                output.positionCS = TransformWorldToHClip(mul(obj2world, input.vertex).xyz);
                output.uv = input.uv;
                // 获取世界空间位置（用于可能的后续计算）
                output.positionWS = mul(obj2world, input.vertex).xyz;
                output.instanceID = instanceID;
                return output;
            }

            // 片元着色器：从实例数据缓冲区中读取每个实例对应的颜色
            float4 ForwardFrag(Varyings input) : SV_Target
            {
                // 通过 SV_InstanceID 获取当前实例的颜色
                float4 finalColor = unity_InstanceColorBuffer[input.instanceID];
                return finalColor;
            }
            ENDHLSL
        }
    }
    // Fallback 用于当当前 SRP 不支持时显示错误信息
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
