Shader "*MyShaders/ShapeSimple"
{
    Properties
    {
        [MaterialToggle] _EnableColorOverride("Enable color override", Float) = 1
        _ColorOverride("ColorOverride", Color) = (1, 1, 1, 1)
        _Stencil("Stencil", Int) = 1
        _StencilGroup("Stencil Texture", 2D) = "white" {}
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

        // Alpha blend (standard)
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

        // Additive
        //Blend SrcAlpha One

        // Premultiplied alpha
        //Blend One OneMinusSrcAlpha

        // Multiply
        //Blend DstColor Zero

        // Screen
        //Blend One OneMinusSrcColor
        Cull Off
        ZWrite [_ZWrite]

        // Stencil 
        // {

        //     Ref [_Stencil]
        //     Comp Always
        //     Pass Replace

        // }
        Stencil
        {
            Ref 69
            ReadMask 255
            WriteMask 255
            Comp NotEqual
            Pass Replace
        }


        Pass
        {
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
                uint vid : SV_VertexID;
                UNITY_SKINNED_VERTEX_INPUTS
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"
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
            UNITY_INSTANCING_BUFFER_END(Props)

            #if USE_SHAPE_LIGHT_TYPE_0
            SHAPE_LIGHT(0)
            #endif

            #if USE_SHAPE_LIGHT_TYPE_1
            SHAPE_LIGHT(1)
            #endif

            #if USE_SHAPE_LIGHT_TYPE_2
            SHAPE_LIGHT(2)
            #endif

            #if USE_SHAPE_LIGHT_TYPE_3
            SHAPE_LIGHT(3)
            #endif

            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);



            float _EnableColorOverride;
            half4 _ColorOverride;

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

                o.positionCS = TransformObjectToHClip(localPos.xyz);

                return o;
            }


            half4 CombinedShapeLightFragment(Varyings i) : SV_Target0
            {
                return _ColorOverride;
            }
            ENDHLSL
        }
    }
}
