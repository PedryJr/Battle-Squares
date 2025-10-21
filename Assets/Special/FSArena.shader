Shader "Lit/Simple Diffuse"
{
    Properties
    {
        _ArenaStencil ("Texture", 2D) = "white" {}
    }
    SubShader
    {

        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }

        Pass
        {

        
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc" // for UnityObjectToWorldNormal
            #include "UnityLightingCommon.cginc" // for _LightColor0

            float4 _ColorOverride;
            struct v2f
            {
                float2 uv : TEXCOORD0;
                fixed4 diff : COLOR0; // diffuse lighting color
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                // get vertex normal in world space
                half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                // dot product between normal and light direction for
                // standard diffuse (Lambert) lighting
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
                // factor in the light color
                o.diff = nl * _LightColor0;
                return o;
            }
            
            sampler2D _ArenaStencil;

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_ArenaStencil, i.uv);

                if(col.r > 0)
                {
                    col = _ColorOverride;
                }
                else
                {
                    col.a *= 0;
                }

                return col;
            }
            ENDHLSL
        }
    }
}
