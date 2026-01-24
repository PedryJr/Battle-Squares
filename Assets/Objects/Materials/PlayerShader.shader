Shader "Custom/PlayerShader"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _Color ("Tint", Color) = (1,1,1,1)
        
        // Blend mode
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
        
        // Stencil properties for sprite masking
        _Stencil ("Stencil ID", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        _NoiseTex1("Noise 1", 2D) = "white"{}
        _Scale1("Scale 1", Range(0, 10)) = 1.0
        _NoiseTex2("Noise 2", 2D) = "white"{}
        _Scale2("Scale 2", Range(0, 10)) = 1.0
        _NoiseTex3("Noise 3", 2D) = "white"{}
        _Scale3("Scale 3", Range(0, 10)) = 1.0
        _NoiseTex4("Noise 4", 2D) = "white"{}
        _Scale4("Scale 4", Range(0, 10)) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
            "PreviewType" = "Plane"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Blend [_SrcBlend] [_DstBlend]
        ZWrite On
        Cull Back
        ColorMask [_ColorMask]

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
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_NoiseTex1);
            SAMPLER(sampler_NoiseTex1);

            TEXTURE2D(_NoiseTex2);
            SAMPLER(sampler_NoiseTex2);

            TEXTURE2D(_NoiseTex3);
            SAMPLER(sampler_NoiseTex3);

            TEXTURE2D(_NoiseTex4);
            SAMPLER(sampler_NoiseTex4);

            float _Scale1;
            float _Scale2;
            float _Scale3;
            float _Scale4;

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;


                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;
                return OUT;
            }

            #define PI 3.14159265358979323846

            float GetSquareFade(float2 uv, float size)
            {
                float2 p = abs(uv - 0.5) * 2.0 / size;
                float d = max(p.x, p.y);   // square distance (0 = center, 1 = edge)

                float fade = smoothstep(0.75, 1, d);
                return pow(fade, 10);
            }

            float SampleNoise1(float2 uv, float speed, float powr)
            {
                float timer = _Time.y * speed;

                uv *= _Scale1;

                uv.x += timer * 0.99;
                uv.y += timer;

                return pow(SAMPLE_TEXTURE2D(_NoiseTex1, sampler_NoiseTex1, uv ).r, powr);
            }

            float SampleNoise2(float2 uv, float speed, float powr)
            {
                float timer = _Time.y * speed;

                uv *= _Scale2;

                uv.x += timer * 0.99;
                uv.y += timer;

                return pow(SAMPLE_TEXTURE2D(_NoiseTex2, sampler_NoiseTex2, uv ).r, powr);
            }

            half4 frag(Varyings IN) : SV_Target
            {

                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 inputToGamma; 
                inputToGamma.rgb = pow(IN.color.rgb, 1/2.2);
                inputToGamma.a = IN.color.a;
                return inputToGamma;
                half3 producedColor = inputToGamma.rgb;
                half alpha = inputToGamma.a;
                half4 output;

                output.rgb = 0;

                float fade = GetSquareFade(IN.uv, 1);

                float l = 1.0;

                l *= SampleNoise1(IN.uv, 0.21, 1);
                l *= SampleNoise2(IN.uv, -0.2, 1);



                output.rgb = producedColor;
                output.a   = lerp(alpha, l, fade) * lerp(1, 0, fade);

                return output;
                //output.rgba = inputToGamma;

            }
            ENDHLSL
        }
    }
}