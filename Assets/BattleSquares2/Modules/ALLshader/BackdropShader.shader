Shader "*MyShaders/BackdropShader"
{
    Properties
    {

        _ArtificialLight("Artificial Light", Float) = 0.3
        _ExponentialNoise("Exponential Noise", Float) = 1
        _ColorStrength("Color strength", Float) = 1
        _ColorToEffect("Color or effect weight", Float) = 1
        _Tiling("Tiling amount", Float) = 1
        _fallofExponential("Fallof Exponential", Float) = 1
        _maxIntensity("Max Intensity", Float) = 1

        _EnergyTexture("Energy Tex", 2D) = "white" {}

        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        // Legacy properties. They're here so that materials using this shader can gracefully fallback to the legacy sprite shader.
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Back
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
            #pragma target 4.5
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
                float4  positionCS  : SV_POSITION;
                float4   color       : COLOR;
                float2  uv          : TEXCOORD0;
                float2   lightingUV  : TEXCOORD1;
                float3  positionWS  : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"

            TEXTURE2D(_EnergyTexture);
            SAMPLER(sampler_EnergyTexture);

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(_MainTex);

            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
            CBUFFER_END

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

            float _Tiling;
            float _ColorToEffect;
            float _ColorStrength;
            float _ExponentialNoise;
            float _ArtificialLight;

            Varyings CombinedShapeLightVertex(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_SKINNED_VERTEX_COMPUTE(v);

                SetUpSpriteInstanceProperties();
                v.positionOS = UnityFlipSprite(v.positionOS, unity_SpriteProps.xy);
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.positionWS = TransformObjectToWorld(v.positionOS);
                o.uv = v.uv * _Tiling;
                o.lightingUV = float2(ComputeScreenPos(o.positionCS / o.positionCS.w).xy);

                o.color = v.color * _Color * unity_SpriteColor;
                return o;
            }

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"
            #include "Assets/BattleSquares2/Scripts/ProximityPixelationSystem/SampleProximityColorBuffer.hlsl"

            float4 SampleFromEnergy(float2 uv)
            {
                return 
                (
                    pow(SAMPLE_TEXTURE2D(_EnergyTexture, sampler_EnergyTexture, uv + float2(_SinTime.x + 0.1, _CosTime.x) / 7), _ExponentialNoise) * 
                    pow(SAMPLE_TEXTURE2D(_EnergyTexture, sampler_EnergyTexture, uv + float2(_SinTime.y, _CosTime.y  + 0.1) / 7), _ExponentialNoise)  * 
                    pow(SAMPLE_TEXTURE2D(_EnergyTexture, sampler_EnergyTexture, uv + float2(_SinTime.z  + 0.1, _CosTime.z) / 7), _ExponentialNoise)  * 
                    pow(SAMPLE_TEXTURE2D(_EnergyTexture, sampler_EnergyTexture, uv + float2(_SinTime.w, _CosTime.w  + 0.1) / 7), _ExponentialNoise) 
                );
            }

            float4 CombinedShapeLightFragment(Varyings i) : SV_Target
            {


                float colorWeight = _ColorToEffect;
                float effectWeight = 1 - colorWeight;

                const float4 energyColor1 = SampleFromEnergy(i.uv);
                const float4 main = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                const float4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv);
                SurfaceData2D surfaceData;
                InputData2D inputData;

                InitializeSurfaceData(main.rgb, main.a, mask, surfaceData);
                InitializeInputData(i.uv, i.lightingUV, inputData);

                SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, i.positionWS, i.positionCS, _MainTex);


                //float4 spriteColor = CombinedShapeLightShared(surfaceData, inputData);
                //float4 spriteColor = CombinedShapeLightShared(surfaceData, inputData);
                float4 spriteColor = float4(_ArtificialLight, _ArtificialLight, _ArtificialLight, 1);
                //return energyColor1 * spriteColor;
                spriteColor.xyz = SampleProximityColor(spriteColor.xyz, i.positionWS.xy);
                //spriteColor.w = 1;

                return (spriteColor * colorWeight * _ColorStrength) + (spriteColor * energyColor1 * effectWeight);
            }
            ENDHLSL
        }

    }
}
