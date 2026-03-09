// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "*MyShaders/UIBUTTON"
{
	Properties
	{
        _MainTex            ("Sprite Texture", 2D) = "white" {}
		_Color              ("Tint", Color) = (1,1,1,1)

		_StencilComp        ("Stencil Comparison", Float) = 8
		_Stencil            ("Stencil ID", Float) = 0
		_StencilOp          ("Stencil Operation", Float) = 0
		_StencilWriteMask   ("Stencil Write Mask", Float) = 255
		_StencilReadMask    ("Stencil Read Mask", Float) = 255

		_CullMode           ("Cull Mode", Float) = 0
		_ColorMask          ("Color Mask", Float) = 15
		_ClipRect           ("Clip Rect", vector) = (-32767, -32767, 32767, 32767)

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
		[Toggle(UV_SAMPLE_FROM_CENTER)] _SAMPLEM ("Use Alpha Clip", Float) = 0
	}

	SubShader
	{
		Tags
		{
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Cull [_CullMode]
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		Pass
		{
            Name "Default"
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
            #pragma target 2.0

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			#define KERNEL_CHANNEL_RED
			#include "../../../Objects/Rendering/Shaders/Includes/KernelSampling.hlsl"
			#include "../../../Objects/Rendering/Shaders/Includes/ColorSpaceConversion.hlsl"

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP
            #pragma multi_compile __ UV_SAMPLE_FROM_CENTER

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex            : SV_POSITION;
				fixed4 color             : COLOR;
				float2 texcoord          : TEXCOORD0;
				float4 worldPosition     : TEXCOORD1;
				float4 mask              : TEXCOORD2;
				float2 ssPosPixel            : TEXCOORD3;
				float2 ssPosObj            : TEXCOORD4;
				UNITY_VERTEX_OUTPUT_STEREO
			};


            sampler2D _MainTex;
			fixed4 _Color;
			fixed4 _TextureSampleAdd;
			float4 _ClipRect;
            float4 _MainTex_ST;
		    float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;
            int _UIVertexColorAlwaysGammaSpace;


            v2f vert(appdata_t v)
			{
				v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				float4 vPosition = UnityObjectToClipPos(v.vertex);
				OUT.worldPosition = v.vertex;
				OUT.vertex = vPosition;
				float2 ndc;
				// clip -> ndc
				ndc = vPosition.xy / vPosition.w;
				// ndc [-1..1] -> uv [0..1]
				OUT.ssPosPixel = ndc * 0.5 + 0.5;

            	float2 pixelSize = vPosition.w;
                pixelSize /= abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

				float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.mask = half4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

                if (_UIVertexColorAlwaysGammaSpace && !IsGammaSpace())
                {
                    v.color.rgb = UIGammaToLinear(v.color.rgb);
                }
                OUT.color = v.color * _Color;


				return OUT;
			}

			//Transform with component streams parameters to material.
			float4x4 modelMatrix;
			float _TilingScale;
			float _Pow;

			float4x4 lightEmitter1;
			float emitterBrightness1;
			float emitterRadius1;


			float4x4 lightEmitter2;
			float emitterBrightness2;
			float emitterRadius2;

			float4x4 lightEmitter3;
			float emitterBrightness3;
			float emitterRadius3;

			float4x4 lightEmitter4;
			float emitterBrightness4;
			float emitterRadius4;

			float4x4 lightEmitter5;
			float emitterBrightness5;
			float emitterRadius5;

			float4x4 lightEmitter6;
			float emitterBrightness6;
			float emitterRadius6;


			float EmitterAttenuation(float4x4 emitter, float brightness, float radius, float2 fragPos)
			{
				float4 objPos = mul(emitter, float4(0, 0, 0, 1));
				float4 clipPos = mul(UNITY_MATRIX_VP, objPos);
				float2 ndc = clipPos.xy / clipPos.w;
				float2 emitterScreenPos = ndc * 0.5 + 0.5;
    
				float dist = distance(emitterScreenPos, fragPos);
    
				float normalizedDist = dist / radius;
    
				float attenuation = 1.0 - saturate(normalizedDist);
    
				attenuation = pow(attenuation, _Pow) * brightness;
    
				return attenuation;
			}

			float GetLighting(float2 fragPos)
			{
				float atten = 0;

				atten += EmitterAttenuation(lightEmitter1, emitterBrightness1, emitterRadius1, fragPos);
				atten += EmitterAttenuation(lightEmitter2, emitterBrightness2, emitterRadius2, fragPos);
				atten += EmitterAttenuation(lightEmitter3, emitterBrightness3, emitterRadius3, fragPos);
				atten += EmitterAttenuation(lightEmitter4, emitterBrightness4, emitterRadius4, fragPos);
				atten += EmitterAttenuation(lightEmitter5, emitterBrightness5, emitterRadius5, fragPos);
				atten += EmitterAttenuation(lightEmitter6, emitterBrightness6, emitterRadius6, fragPos);

				return atten;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				//Generated UV -> Removes stretching caused by UI system and gives me full controll
				float4 objPos = mul(modelMatrix, float4(0, 0, 0, 1));
				float4 clipPos = mul(UNITY_MATRIX_VP, objPos);
				float2 ndc = clipPos.xy / clipPos.w;
				IN.ssPosObj = ndc * 0.5 + 0.5;

				float2 objUV = IN.ssPosObj.xy;
				float2 pixelUV = IN.ssPosPixel.xy;

				float2 uvDelta = abs(objUV - pixelUV);
				uvDelta *= _TilingScale;

				float4 color = tex2D(_MainTex, uvDelta);

				float edgeDetection = LaplacianOfGaussian9x9(_MainTex, uvDelta, float2(1, 1));
				color.a = edgeDetection > 0.1 ? 1 : 0;


				color *= GetLighting(IN.ssPosPixel.xy);

				#if UNITY_UI_CLIP_RECT
					half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
					color *= m.x * m.y;
				#endif

				#ifdef UNITY_UI_ALPHACLIP
					clip(color.a - 0.001);
				#endif

				return color;
			}
		    ENDCG
		}
	}
}
