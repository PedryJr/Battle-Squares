Shader "Custom/Thermal"
{
    Properties
    {
        _DistortionMap1("Distortion Texture 1", 2D) = "white" {}
        _DistortionMap2("Distortion Texture 2", 2D) = "white" {}
        _DistortionStrength("Distortion strength", Range(0.0, 1.0)) = 0
        _DistortionExponential("Expo", Float) = 1
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

            TEXTURE2D(_DistortionMap1);
            SAMPLER(sampler_DistortionMap1);

            TEXTURE2D(_DistortionMap2);
            SAMPLER(sampler_DistortionMap2);

            float _DistortionStrength;
            float _DistortionExponential;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {

                float2 tiledUv = IN.uv + _Time.y;

                

                half4 distortion = SAMPLE_TEXTURE2D(_DistortionMap1, sampler_DistortionMap1, tiledUv).r * _DistortionStrength;

                float fallof = pow(SAMPLE_TEXTURE2D(_DistortionMap2, sampler_DistortionMap2, IN.uv).r, _DistortionExponential);

                distortion *= fallof;

                return distortion;
            }
            ENDHLSL
        }
    }
}
