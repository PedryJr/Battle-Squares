Shader "*MyShaders/ShapeShadow"
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
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
         _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.5)
        _ShadowIntensity ("Shadow Intensity", Range(0, 1)) = 1
    }

    SubShader
    {
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

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

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            uniform float3 _LightPos;
            uniform float4x4 _ShadowModelMatrix;
            uniform float4x4 _ShadowModelInvMatrix;
            uniform float3 _ShadowModelScale;
            uniform float  _ShadowRadius;
            uniform float  _ShadowContractionDistance;
            uniform float  _SoftShadowAngle;
            
            float4 _ShadowColor;
            float _ShadowIntensity;

                        #define ToFloat(x) x
            #define Deg2Rad(x) (x * 3.14159265359f / 180)

            struct Attributes
            {
                float3 vertex  : POSITION;
                float4 packed0 : TANGENT;
                uint vid : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 vertex      : SV_POSITION;
                float2 shadow      : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"


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


            float AngleFromDir(float3 dir)
            {
                float angle = acos(dir.x);
                float gt180 = ceil(saturate(-dir.y));
                return gt180 * -angle + (1 - gt180) * angle;
            }

            float3 DirFromAngle(float angle)
            {
                return float3(cos(angle), sin(angle), 0);
            }

            float2 CalculateShadowValue(float shadowType)
            {
                float isLeft = ToFloat(shadowType == 1);
                float isRight = ToFloat(shadowType == 3);
                return float2(-isLeft + isRight, isLeft + isRight);
            }

            float3 SoftShadowDir(float3 lightDir, float3 vertex0, float3 vertex1, float angleOp, float softShadowAngle)
            {
                float lightAngle = AngleFromDir(lightDir);
                float edgeAngle = AngleFromDir(normalize(vertex1 - vertex0));
                float softAngle = lightAngle + angleOp * softShadowAngle;
                return DirFromAngle(softAngle);
            }

            float4 ProjectShadowVertexToWS(float2 vertex, float2 otherEndPt, float2 contractDir, float shadowType, float3 lightPos, float3 shadowModelScale, float4x4 shadowModelMatrix, float4x4 shadowModelInvMatrix, float shadowContractionDistance, float shadowRadius, float softShadowAngle)
            {
                float3 vertexOS0 = float3(vertex.x * shadowModelScale.x, vertex.y * shadowModelScale.y, 0);
                float3 vertexOS1 = float3(otherEndPt.x * shadowModelScale.x, otherEndPt.y * shadowModelScale.y, 0);
                float3 lightPosOS = float3(mul(shadowModelInvMatrix, float4(lightPos.x, lightPos.y, lightPos.z, 1)).xy, 0);

                float3 unnormalizedLightDir0 = vertexOS0 - lightPosOS;
                float3 unnormalizedLightDir1 = vertexOS1 - lightPosOS;

                float3 lightDir0 = normalize(unnormalizedLightDir0);
                float3 lightDir1 = normalize(unnormalizedLightDir1);
                float3 avgLightDir = normalize(lightDir0 + lightDir1);

                float isSoftShadow = ToFloat(shadowType >= 1);
                float isHardShadow = ToFloat(shadowType == 0);
                float isShadowVertex = saturate(isSoftShadow + isHardShadow);

                float3 softShadowDir = SoftShadowDir(lightDir0, vertexOS0, vertexOS1, shadowType - 2, softShadowAngle);
                float3 hardShadowDir = lightDir0;
                float3 shadowDir = isSoftShadow * softShadowDir + isHardShadow * hardShadowDir;

                float lightDistance = length(unnormalizedLightDir0);
                float hardShadowLength = max(shadowRadius / dot(lightDir0, avgLightDir), lightDistance);
                float softShadowLength = shadowRadius * (1 / cos(softShadowAngle));

                float3 shadowOffset = (isSoftShadow * softShadowLength + isHardShadow * hardShadowLength) * shadowDir;
                float3 contractedVertexPos = vertexOS0 + float3(shadowContractionDistance * contractDir.xy, 0);

                float3 finalVertexOS = isShadowVertex * (lightPosOS + shadowOffset) + (1 - isShadowVertex) * contractedVertexPos;

                return mul(shadowModelMatrix, float4(finalVertexOS, 1));
            }

            Varyings ProjectShadow(Attributes v)
            {
                Varyings o;

                float2 contractDir = v.packed0.xy;
                float2 otherEndPt = v.packed0.zw;
                float  shadowType = v.vertex.z;
                float2 position = v.vertex.xy;
                float  softShadowAngle = _SoftShadowAngle;

                float4 positionWS = ProjectShadowVertexToWS(position, otherEndPt, contractDir, shadowType, _LightPos, _ShadowModelScale, _ShadowModelMatrix, _ShadowModelInvMatrix, _ShadowContractionDistance, _ShadowRadius, softShadowAngle);
                o.vertex = mul(UNITY_MATRIX_VP, positionWS);
                o.shadow = CalculateShadowValue(shadowType);
                return o;
            }

            float _EnableColorOverride;
            half4 _ColorOverride;

            float4 GetInstancedVertex(Attributes v)
            {

                float4 localPos;
                if(v.vid == 0) localPos = UNITY_ACCESS_INSTANCED_PROP(Props, _Pos0);
                if(v.vid == 1) localPos = UNITY_ACCESS_INSTANCED_PROP(Props, _Pos1);
                if(v.vid == 2) localPos = UNITY_ACCESS_INSTANCED_PROP(Props, _Pos2);
                if(v.vid == 3) localPos = UNITY_ACCESS_INSTANCED_PROP(Props, _Pos3);
                if(v.vid == 4) localPos = UNITY_ACCESS_INSTANCED_PROP(Props, _Pos4);
                if(v.vid == 5) localPos = UNITY_ACCESS_INSTANCED_PROP(Props, _Pos5);
                if(v.vid == 6) localPos = UNITY_ACCESS_INSTANCED_PROP(Props, _Pos6);
                if(v.vid == 7) localPos = UNITY_ACCESS_INSTANCED_PROP(Props, _Pos7);
                return localPos;
            }

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_SKINNED_VERTEX_COMPUTE(v);

                
                v.vertex = GetInstancedVertex(v).xyz;

                o = ProjectShadow(v);


                //o.vertex = TransformObjectToHClip(v.vertex.xyz);

                return o;
            }


            half4 frag(Varyings i) : SV_Target0
            {

                                // Setup instancing
                UNITY_SETUP_INSTANCE_ID(i);
                
                // i.shadow.y contains whether this is a shadow vertex (0 or non-shadow, >0 for shadow)
                float shadowStrength = saturate(i.shadow.y) * _ShadowIntensity;
                
                // Output shadow color with alpha based on shadow strength
                return float4(_ShadowColor.rgb, _ShadowColor.a * shadowStrength);
                return 1;
            }
            ENDHLSL
        }
    }
}