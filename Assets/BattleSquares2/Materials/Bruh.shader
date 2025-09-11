Shader "Custom/BruhFragVert"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 5.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            StructuredBuffer<float4x4> modelMatrices;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(int, MyId)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            float4 Getch(float3 localPosition)
            {
                float4x4 model = modelMatrices[UNITY_ACCESS_INSTANCED_PROP(Props, MyId)];
                float4 localPos4 = float4(localPosition, 1.0);
                float4 worldPos4 = mul(model, localPos4);
                return worldPos4;
            }

            v2f vert(appdata v)
            {
                v2f o;
                int id = UNITY_ACCESS_INSTANCED_PROP(Props, MyId);
                float4x4 model = modelMatrices[id];
                float4 worldPos = Getch(v.vertex.xyz);
                o.pos = UnityWorldToClipPos(worldPos);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }

            ENDCG
        }
        
        //UsePass "Universal Render Pipeline/2D/Sprite-Lit Default/ShadowCaster"
    }

    FallBack "Diffuse"
}
