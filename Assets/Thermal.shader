Shader "Custom/Thermal"
{
    Properties
    {
        _DistortionMap1("Distortion Texture 1", 2D) = "white" {}
        _DistortionMap2("Distortion Texture 2", 2D) = "white" {}
        _DistortionExponential("Expo", Float) = 1
        _Multiplier("Multiplier", Range(0.0, 50.0)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 positionWS : ADD0;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_DistortionMap1);
            SAMPLER(sampler_DistortionMap1);

            TEXTURE2D(_DistortionMap2);
            SAMPLER(sampler_DistortionMap2);

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _DistortionStrength)
            UNITY_INSTANCING_BUFFER_END(Props)

            float DistortionStrength()
            {
                return UNITY_ACCESS_INSTANCED_PROP(Props, _DistortionStrength);
            }

            float _DistortionExponential;
            float _Multiplier;

            Varyings vert(Attributes v)
            {
                Varyings o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz).xy;
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float2 tiledUv = i.positionWS / 10.0;

                half4 distortion = SAMPLE_TEXTURE2D(_DistortionMap1, sampler_DistortionMap1, tiledUv).r * DistortionStrength() * _Multiplier;

                float fallof = pow(SAMPLE_TEXTURE2D(_DistortionMap2, sampler_DistortionMap2, i.uv).r, _DistortionExponential);

                distortion *= fallof;

                return distortion;
            }
            ENDHLSL
        }
    }
}
