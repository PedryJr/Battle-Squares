Shader "*MyShaders/SuperFluid"
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

        _Sample1Mul("Sample1Mul", Float) = 1
        _Sample2Mul("Sample2Mul", Float) = 1

        _DistortionNoise("Distortion Noise", 2D) = "white" {}
        _DistortionStrength("Distortion Strength", Float) = 1


        [Header(Fractal Settings)]
        _Octaves ("Octaves", Integer) = 4
        _Lacunarity ("Lacunarity", Float) = 2.0
        _Persistence ("Persistence", Float) = 0.5

        [Header(Caustic Settings)]
        _CausticSharpness ("Caustic Sharpness", Float) = 3.0
        _DomainWarpStrength ("Domain Warp Strength", Float) = 0.3
        [Toggle] _UseF2MinusF1 ("Use F2 Minus F1", Float) = 0

        [Header(Animation)]
        _AnimationSpeed ("Animation Speed", Float) = 1.0

        [Header(Output)]
        _Contrast ("Contrast", Float) = 1.0
        _Brightness ("Brightness", Float) = 1.0
        [Toggle] _Invert ("Invert", Float) = 0

        [Header(Feature Points)]
        _FeaturePointsTex ("Feature Points Texture", 2D) = "white" {}
        _FracTiling("Fractal Tiling amount", Float) = 1
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Back
        ZWrite [_ZWrite]
        //ZTest LEqual
        

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

            TEXTURE2D(_DistortionNoise);
            SAMPLER(sampler_DistortionNoise);

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(_MainTex);

            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _DistortionNoise_ST;
            CBUFFER_END

            float _Tiling;
            float _ColorToEffect;
            float _ColorStrength;
            float _ExponentialNoise;
            float _ArtificialLight;

            float _Sample1Mul;
            float _Sample2Mul;
            float _DistortionStrength;

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

            //#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"
            #include "Assets/BattleSquares2/Scripts/ProximityPixelationSystem/SampleProximityColorBuffer.hlsl"

            int _Octaves;
            float _Lacunarity;
            float _Persistence;
            float _CausticSharpness;
            float _DomainWarpStrength;
            float _UseF2MinusF1;
            float _AnimationSpeed;
            float _Contrast;
            float _Brightness;
            float _Invert;
            sampler2D _FeaturePointsTex;
                float _FracTiling = 0.0;
            float FractalCaustic(
    float2 uv,
    int octaves,
    float lacunarity,
    float persistence,
    float causticSharpness,
    float domainWarpStrength,
    bool useF2MinusF1,
    float time,
    float animationSpeed,
    float contrast,
    float brightness,
    bool invert,
    sampler2D featurePointsTex) // or pass as a structured buffer
{
    float t = time * animationSpeed;

    float amplitude = 1.0;
    float frequency = 1.0;
    float sum = 0.0;
    float maxSum = 0.0;

    for (int i = 0; i < octaves; i++)
    {
        float uf = uv.x * frequency;
        float vf = uv.y * frequency;
        float warp = sin((uf + t) * 3.14159265 * 2.0) * cos((vf + t) * 3.14159265 * 2.0);

        float warpedU = frac(uf + warp * domainWarpStrength);
        float warpedV = frac(vf + warp * domainWarpStrength);

        float min1 = 1e38;
        float min2 = 1e38;

        int cellCount = 1;

        float scaledU = warpedU * cellCount;
        float scaledV = warpedV * cellCount;

        for (int iy = 0; iy < cellCount; iy++)
        {
            for (int ix = 0; ix < cellCount; ix++)
            {
                // Sample feature point from texture (packed as RG)
                float2 p = tex2D(featurePointsTex, float2(
                    (ix + 0.5) / cellCount,
                    (iy + 0.5) / cellCount)).rg;

                float dx = abs(scaledU - p.x * cellCount);
                float dy = abs(scaledV - p.y * cellCount);

                dx = min(dx, cellCount - dx);
                dy = min(dy, cellCount - dy);

                float dist = dx * dx + dy * dy;

                if (dist < min1)
                {
                    min2 = min1;
                    min1 = dist;
                }
                else if (dist < min2)
                {
                    min2 = dist;
                }
            }
        }

        float value = useF2MinusF1 ? sqrt(min2) - sqrt(min1) : sqrt(min1);
        value = exp(-value * causticSharpness);
        sum += value * amplitude;
        maxSum += amplitude;

        amplitude *= persistence;
        frequency *= lacunarity;
    }

    float pixel = pow(sum / maxSum, contrast) * brightness;
    pixel = saturate(pixel);

    if (invert) pixel = 1.0 - pixel;

    return pixel;
}

            float4 SampleFromEnergy(float2 uv)
            {

                    float caustic = FractalCaustic(
                            uv/_Tiling * _FracTiling,
                        _Octaves, _Lacunarity, _Persistence,
                        _CausticSharpness, _DomainWarpStrength,
                        _UseF2MinusF1,
                        _Time.y, _AnimationSpeed,
                        _Contrast, _Brightness, _Invert,
                        _FeaturePointsTex);

                        return float4(caustic, caustic, caustic, 1);
            }

            float4 CombinedShapeLightFragment(Varyings i) : SV_Target
            {

                

                float colorWeight = _ColorToEffect;
                float effectWeight = 1 - colorWeight;

                const float4 energyColor1 = SampleFromEnergy(i.uv);
                const float4 main = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                const float4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv);
                //SurfaceData2D surfaceData;
                //InputData2D inputData;

                //InitializeSurfaceData(main.rgb, main.a, mask, surfaceData);
                //InitializeInputData(i.uv, i.lightingUV, inputData);

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
