Shader "RenderFeatures/ThermalReadAndApply"
{
    Properties
    {
        _DistortionTexture("Distortion Tex", 2D) = "white" {}
        _DistortStrength("Distortion Strength", Range(0,1)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Cull Back 
        ZWrite On
        ZTest Always

        Pass
        {
            Name "FULLSCREEN"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            sampler2D _BlitTexture;
            sampler2D _DistortionTexture;
            sampler2D _CameraDepthTexture;

            float _DistortStrength;

            struct appdata
            {
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            v2f Vert(appdata i)
            {
                v2f o;
                o.pos = GetFullScreenTriangleVertexPosition(i.vertexID);
                o.uv = GetFullScreenTriangleTexCoord(i.vertexID);
                return o;
            }

            float4 Frag(v2f i) : SV_Target
            {

                float2 uv = i.uv;

                float2 distortedUv = 0;
                distortedUv += tex2D(_DistortionTexture, uv).g;
                distortedUv *= _DistortStrength;

                distortedUv += uv;
                float4 col = tex2D(_BlitTexture, distortedUv);
                return col;
            }

            ENDHLSL
        } // Pass
    } // SubShader

    FallBack Off
}
