Shader "Custom/loll"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _BaseMap("Base Map", 2D) = "white" {}
        _Blurr ("Blurr", Float) = 0.0
        _Steps ("Steps", Range(0.0, 255.0)) = 0.0
        _Quality ("Quality", Range(1.0, 64.0)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Objects/Rendering/Shaders/Includes/KernelSampling.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _BaseMap;

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            float _Blurr;
            float _Steps;
            int _Quality;

            void Unity_Posterize_float4(float4 In, float4 Steps, out float4 Out)
            {
                Out = floor(In / (1 / Steps)) * (1 / Steps);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                _Blurr /= _ScreenParams.x;
                half4 color = KernelAverage9x9(_BaseMap, IN.uv, float2(_Blurr, _Blurr)) * _BaseColor;
                Unity_Posterize_float4(color, _Steps, color);
                return color;
            }
            ENDHLSL
        }
    }
}
