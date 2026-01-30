Shader "*MyShaders/StencilDraw"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        [MaterialToggle] _ZWrite("ZWrite", Float) = 1

        // Legacy properties. They're here so that materials using this shader can gracefully fallback to the legacy sprite shader.
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Cull Off
            ZWrite [_ZWrite]
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex CombinedShapeLightVertex
            #pragma fragment CombinedShapeLightFragment

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                uint vid : SV_VertexID;
                UNITY_SKINNED_VERTEX_INPUTS
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                float4   color       : COLOR;
                float2  uv          : TEXCOORD0;
                half2   lightingUV  : TEXCOORD1;
                float3  positionWS  : TEXCOORD2;
                float4 stencilID    : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct FragOutput
            {
                float4 myOut        : SV_Target0;
            };

            //#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(_MainTex);

            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Pos0)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Pos1)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Pos2)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Pos3)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Pos4)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Pos5)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Pos6)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Pos7)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Stencil)
            UNITY_INSTANCING_BUFFER_END(Props)

            Varyings CombinedShapeLightVertex(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                float4 localPos;

                if(v.vid == 0) localPos = UNITY_ACCESS_INSTANCED_PROP(Props, _Pos0);
                if(v.vid == 1) localPos = UNITY_ACCESS_INSTANCED_PROP(Props, _Pos1);
                if(v.vid == 2) localPos = UNITY_ACCESS_INSTANCED_PROP(Props, _Pos2);
                if(v.vid == 3) localPos = UNITY_ACCESS_INSTANCED_PROP(Props, _Pos3);
                if(v.vid == 4) localPos = UNITY_ACCESS_INSTANCED_PROP(Props, _Pos4);
                if(v.vid == 5) localPos = UNITY_ACCESS_INSTANCED_PROP(Props, _Pos5);
                if(v.vid == 6) localPos = UNITY_ACCESS_INSTANCED_PROP(Props, _Pos6);
                if(v.vid == 7) localPos = UNITY_ACCESS_INSTANCED_PROP(Props, _Pos7);

                localPos.z = -2.91;

                v.positionOS = localPos.xyz;

                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_SKINNED_VERTEX_COMPUTE(v);

                SetUpSpriteInstanceProperties();
                v.positionOS = UnityFlipSprite(v.positionOS, unity_SpriteProps.xy);
                o.positionCS = TransformObjectToHClip(localPos.xyz);
                o.positionWS = TransformObjectToWorld(localPos.xyz);
                o.uv = v.uv;
                o.lightingUV = half2(ComputeScreenPos(o.positionCS / o.positionCS.w).xy);
                o.stencilID = UNITY_ACCESS_INSTANCED_PROP(Props, _Stencil);
                o.color = v.color * unity_SpriteColor;
                return o;
            }

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            FragOutput CombinedShapeLightFragment(Varyings i)
            {

                FragOutput o;

                o.myOut.r = i.stencilID;
                //o.myOut = i.stencilID * 2048 / 2;
                //o.myOut = 0.5f;
                o.myOut.a = 1;

                return o;
            }
            ENDHLSL
        }
    }
}
