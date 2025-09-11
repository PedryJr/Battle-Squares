Shader "Custom/2DShadowReadyShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {



        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "PreviewType"="Plane" 
            "RenderPipeline"="UniversalPipeline" 
        }

        Pass
        {
            Name "Lit"
            Tags { "LightMode" = "Universal2D" }

            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            
              

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #pragma multi_compile_fragment _ _LIGHT_TEXTURE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceInput.hlsl"

            StructuredBuffer<float4x4> modelMatrices;

                        UNITY_INSTANCING_BUFFER_START(InstancingProps)
                UNITY_DEFINE_INSTANCED_PROP(int, MyId)
            UNITY_INSTANCING_BUFFER_END(InstancingProps)


            void Getch_float(float3 localPosition, out float3 worldPosition)
            {
                float4x4 model = modelMatrices[UNITY_ACCESS_INSTANCED_PROP(InstancingProps, MyId)];
                float4 localPos4 = float4(localPosition, 1.0);
                float4 worldPos4 = mul(model, localPos4);
                worldPosition = worldPos4.xyz;
            }


            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 worldPos : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float2 lightingUV : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _Color;

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float3 worldPosition;
                Getch_float(v.positionOS.xyz, out worldPosition);

                o.positionHCS = TransformWorldToHClip(worldPosition);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                o.worldPos = worldPosition;
                o.normalWS = float3(0, 0, -1);
                o.lightingUV = ComputeLightingUV(float4(worldPosition, 1.0));
                return o;
            }


            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;

                float4 lit = SampleSpriteLighting(i.lightingUV, i.normalWS, i.worldPos, texColor);

                return lit;
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            Cull Off
            ZWrite On
            ColorMask 0

            HLSLPROGRAM

            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            UNITY_INSTANCING_BUFFER_START(InstancingProps)
                UNITY_DEFINE_INSTANCED_PROP(int, MyId)
            UNITY_INSTANCING_BUFFER_END(InstancingProps)

            StructuredBuffer<float4x4> modelMatrices;

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            void Getch_float(float3 localPosition, out float3 worldPosition)
            {
                float4x4 model = modelMatrices[UNITY_ACCESS_INSTANCED_PROP(InstancingProps, MyId)];
                float4 localPos4 = float4(localPosition, 1.0);
                float4 worldPos4 = mul(model, localPos4);
                worldPosition = worldPos4.xyz;
            }

            Varyings ShadowVert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);

                float3 worldPosition;
                Getch_float(v.positionOS.xyz, out worldPosition);
                o.positionHCS = TransformWorldToHClip(worldPosition);
                return o;
            }

            float4 ShadowFrag(Varyings i) : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }


        UsePass "Universal Render Pipeline/2D/Sprite-Lit Default/ShadowCaster"
    }

    FallBack "Sprites/Default"
}
