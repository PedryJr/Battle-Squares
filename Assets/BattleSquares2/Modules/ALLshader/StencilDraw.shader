Shader "*MyShaders/StencilDraw"
{
    Properties
    {
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

        Cull Off
        ZWrite [_ZWrite]

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

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"

            TEXTURE2D(_StencilGroup);
            SAMPLER(sampler_StencilGroup);

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

                o.myOut = i.stencilID;
                //o.myOut = 0.5f;

                return o;
            }
            ENDHLSL
        }

        Pass
        {
            ZWrite Off

            Tags { "LightMode" = "NormalsRendering"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex NormalsRenderingVertex
            #pragma fragment NormalsRenderingFragment

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                float4 tangent      : TANGENT;
                uint vid : SV_VertexID;
                UNITY_SKINNED_VERTEX_INPUTS
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS      : SV_POSITION;
                half4   color           : COLOR;
                float2  uv              : TEXCOORD0;
                half3   normalWS        : TEXCOORD1;
                half3   tangentWS       : TEXCOORD2;
                half3   bitangentWS     : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START( UnityPerMaterial )
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
                UNITY_DEFINE_INSTANCED_PROP(float4, _MyColor)
            UNITY_INSTANCING_BUFFER_END(Props)

            Varyings NormalsRenderingVertex(Attributes attributes)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(attributes);

                float4 localPos;

                if(attributes.vid == 0) localPos = UNITY_ACCESS_INSTANCED_PROP(Props, _Pos0);
                if(attributes.vid == 1) localPos = UNITY_ACCESS_INSTANCED_PROP(Props, _Pos1);
                if(attributes.vid == 2) localPos = UNITY_ACCESS_INSTANCED_PROP(Props, _Pos2);
                if(attributes.vid == 3) localPos = UNITY_ACCESS_INSTANCED_PROP(Props, _Pos3);
                if(attributes.vid == 4) localPos = UNITY_ACCESS_INSTANCED_PROP(Props, _Pos4);
                if(attributes.vid == 5) localPos = UNITY_ACCESS_INSTANCED_PROP(Props, _Pos5);
                if(attributes.vid == 6) localPos = UNITY_ACCESS_INSTANCED_PROP(Props, _Pos6);
                if(attributes.vid == 7) localPos = UNITY_ACCESS_INSTANCED_PROP(Props, _Pos7);

                localPos.z = -2.91;

                attributes.positionOS = localPos.xyz;

                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_SKINNED_VERTEX_COMPUTE(attributes);

                SetUpSpriteInstanceProperties();
                attributes.positionOS = UnityFlipSprite(attributes.positionOS, unity_SpriteProps.xy);
                o.positionCS = TransformObjectToHClip(attributes.positionOS);
                o.uv = attributes.uv;
                o.color = attributes.color * _Color * unity_SpriteColor;
                o.normalWS = -GetViewForwardDir();
                o.tangentWS = TransformObjectToWorldDir(attributes.tangent.xyz);
                o.bitangentWS = cross(o.normalWS, o.tangentWS) * attributes.tangent.w;
                return o;
            }

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl"

            half4 NormalsRenderingFragment(Varyings i) : SV_Target
            {
                const half4 mainTex = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                const half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv));

                return NormalsRenderingShared(mainTex, normalTS, i.tangentWS.xyz, i.bitangentWS.xyz, i.normalWS.xyz);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue"="Transparent" "RenderType"="Transparent"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
            #if defined(DEBUG_DISPLAY)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"
            #endif

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_SKINNED_VERTEX_INPUTS
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS      : SV_POSITION;
                float4  color           : COLOR;
                float2  uv              : TEXCOORD0;
                #if defined(DEBUG_DISPLAY)
                float3  positionWS  : TEXCOORD2;
                #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(_MainTex);

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START( UnityPerMaterial )
                half4 _Color;
            CBUFFER_END

            Varyings UnlitVertex(Attributes attributes)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(attributes);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_SKINNED_VERTEX_COMPUTE(attributes);

                SetUpSpriteInstanceProperties();
                attributes.positionOS = UnityFlipSprite( attributes.positionOS, unity_SpriteProps.xy);
                o.positionCS = TransformObjectToHClip(attributes.positionOS);
                #if defined(DEBUG_DISPLAY)
                o.positionWS = TransformObjectToWorld(attributes.positionOS);
                #endif
                o.uv = attributes.uv;
                o.color = attributes.color * _Color * unity_SpriteColor;
                return o;
            }

            float4 UnlitFragment(Varyings i) : SV_Target
            {
                float4 mainTex = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                #if defined(DEBUG_DISPLAY)
                SurfaceData2D surfaceData;
                InputData2D inputData;
                half4 debugColor = 0;

                InitializeSurfaceData(mainTex.rgb, mainTex.a, surfaceData);
                InitializeInputData(i.uv, inputData);
                SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, i.positionWS, i.positionCS, _MainTex);

                if(CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
                {
                    return debugColor;
                }
                #endif

                return mainTex;
            }
            ENDHLSL
        }
    }
}
