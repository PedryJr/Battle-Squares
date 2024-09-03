Shader "Custom/2DMotionBlurShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurStrength ("Blur Strength", Range(0, 1)) = 0.5
        _Color ("Tint Color", Color) = (1,1,1,1)
        _Velocity ("Velocity", Vector) = (0,0,0,0)
    }
    
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            sampler2D _MainTex;
            float _BlurStrength;
            float4 _Color;
            float4 _Velocity;
            
            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            float4 frag (v2f i) : SV_Target
            {
                float2 blurOffset = _BlurStrength * _Velocity.xy * float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);
                
                // Sample multiple points to create the blur effect
                float4 blurredColor = float4(0, 0, 0, 0);
                blurredColor += tex2D(_MainTex, i.uv - 2.0 * blurOffset) * 0.05;
                blurredColor += tex2D(_MainTex, i.uv - blurOffset) * 0.15;
                blurredColor += tex2D(_MainTex, i.uv) * 0.40;
                blurredColor += tex2D(_MainTex, i.uv + blurOffset) * 0.15;
                blurredColor += tex2D(_MainTex, i.uv + 2.0 * blurOffset) * 0.05;
                
                return blurredColor * _Color;
            }
            ENDCG
        }
    }
    
    FallBack "Diffuse"
}
