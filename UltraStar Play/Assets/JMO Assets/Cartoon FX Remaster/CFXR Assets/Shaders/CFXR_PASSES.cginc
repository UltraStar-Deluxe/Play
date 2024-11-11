//--------------------------------------------------------------------------------------------------------------------------------
// Cartoon FX
// (c) 2012-2020 Jean Moreno
//--------------------------------------------------------------------------------------------------------------------------------


//--------------------------------------------------------------------------------------------------------------------------------
// UBERSHADER
//--------------------------------------------------------------------------------------------------------------------------------

#if defined(CFXR_UBERSHADER)

		#if defined(UNITY_SAMPLE_FULL_SH_PER_PIXEL)
			#undef UNITY_SAMPLE_FULL_SH_PER_PIXEL
		#endif
		#define UNITY_SAMPLE_FULL_SH_PER_PIXEL defined(_NORMALMAP)

		#include "UnityCG.cginc"
		#include "UnityStandardUtils.cginc"
		#include "AutoLight.cginc"

		#if defined(CFXR_URP)
			#include "CFXR_URP.cginc"
		#else
			#include "UnityLightingCommon.cginc"
		#endif

		#if defined(_CFXR_LIGHTING_INDIRECT) || defined(_CFXR_LIGHTING_DIRECT) || defined(_CFXR_LIGHTING_ALL)
			#define LIGHTING
		#endif
		#if defined(_CFXR_LIGHTING_DIRECT) || defined(_CFXR_LIGHTING_ALL)
			#define LIGHTING_DIRECT
		#endif
		#if defined(_CFXR_LIGHTING_INDIRECT) || defined(_CFXR_LIGHTING_ALL)
			#define LIGHTING_INDIRECT
		#endif
		#if (defined(_CFXR_LIGHTING_DIRECT) || defined(_CFXR_LIGHTING_ALL)) && defined(_CFXR_LIGHTING_BACK)
			#define LIGHTING_BACK
		#endif

		#include "CFXR_SETTINGS.cginc"

		// --------------------------------

		CBUFFER_START(UnityPerMaterial)

		float4 _OverlayTex_Scroll;

		half _BumpScale;

		float4 _GameObjectWorldPosition;
		half _LightingWorldPosStrength;
		half _IndirectLightingMix;
		half4 _ShadowColor;
		half _DirectLightingRamp;
		half _DirLightScreenAtten;
		half _BacklightTransmittance;

		half _InvertDissolveTex;
		half _DoubleDissolve;
		half2 _DissolveScroll;
		half _DissolveSmooth;

		half4 _DistortScrolling;
		half _Distort;
		half _FadeAlongU;

		half _SecondColorSmooth;

		half _HdrMultiply;

		half _ReceivedShadowsStrength;

		half _Cutoff;

		half _SoftParticlesFadeDistanceNear;
		half _SoftParticlesFadeDistanceFar;
		half _EdgeFadePow;

		half4 _DissolveTex_ST;

	#if !defined(SHADER_API_GLES)
		float _ShadowStrength;
		float4 _DitherCustom_TexelSize;
	#endif

		CBUFFER_END

		sampler2D _MainTex;
		sampler2D _OverlayTex;
		sampler2D _BumpMap;
		sampler2D _DissolveTex;
		sampler2D _DistortTex;
		sampler2D _SecondColorTex;
		// sampler2D _GradientMap;
		UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
	#if !defined(SHADER_API_GLES)
		sampler3D _DitherMaskLOD;
		sampler3D _DitherCustom;
	#endif

		// --------------------------------
		// Input/output

		struct appdata
		{
			float4 vertex		: POSITION;
			half4 color			: COLOR;
			float4 texcoord		: TEXCOORD0;	//xy = uv, zw = random
			float4 texcoord1	: TEXCOORD1;	//additional particle data: x = dissolve, y = animFrame
			float4 texcoord2	: TEXCOORD2;	//additional particle data: second color
	#if defined(PASS_SHADOW_CASTER) || _CFXR_EDGE_FADING || defined(LIGHTING)
			float3 normal       : NORMAL;
	#endif
	#if defined(LIGHTING) && _NORMALMAP
			float4 tangent : TANGENT;
	#endif
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		// vertex to fragment
		struct v2f
		{
			float4 pos				: SV_POSITION;
			half4 color				: COLOR;
			float4 uv_random		: TEXCOORD0;	//uv + particle data
			float4 custom1			: TEXCOORD1;	//additional particle data
	#if _CFXR_SECONDCOLOR_LERP || _CFXR_FONT_COLORS || (defined(LIGHTING) && _EMISSION)
			float4 secondColor		: TEXCOORD2;
	#endif
	#if !defined(GLOBAL_DISABLE_SOFT_PARTICLES) && ((defined(SOFTPARTICLES_ON) || defined(CFXR_URP) || defined(SOFT_PARTICLES_ORTHOGRAPHIC)) && defined(_FADING_ON))
			float4 projPos			: TEXCOORD3;
	#endif
	#if defined(LIGHTING_DIRECT) || (defined(LIGHTING_INDIRECT) && _NORMALMAP)
			float3 worldPos : TEXCOORD4;
	#endif
	#if defined(LIGHTING_INDIRECT) && !_NORMALMAP
			float3 shColor : TEXCOORD5;
	#endif
	#if defined(LIGHTING_DIRECT) && defined(LIGHTING_BACK)
			float3 viewDirWS : TEXCOORD6;
	#endif
	#if defined(_CFXR_LIGHTING_WPOS_OFFSET) && (defined(LIGHTING_DIRECT) || defined(LIGHTING_INDIRECT))
			float3 worldPosDiff : TEXCOORD7;
	#endif
	#if !defined(PASS_SHADOW_CASTER)
			UNITY_FOG_COORDS(8)		//note: does nothing if fog is not enabled
			// SHADOW_COORDS(8)
	#endif
	#if defined(LIGHTING)
			float3 normalWS : NORMAL;
		#if _NORMALMAP
			float4 tangentWS : TANGENT;
		#endif
	#endif
			UNITY_VERTEX_INPUT_INSTANCE_ID
			UNITY_VERTEX_OUTPUT_STEREO
		};

		// vertex to fragment (shadow caster)
		struct v2f_shadowCaster
		{
			V2F_SHADOW_CASTER_NOPOS
			half4 color				: COLOR;
			float4 uv_random		: TEXCOORD1;	//uv + particle data
			float4 custom1			: TEXCOORD2;	//additional particle data
			UNITY_VERTEX_INPUT_INSTANCE_ID
			UNITY_VERTEX_OUTPUT_STEREO
		};

		// --------------------------------

		#include "CFXR.cginc"

		// --------------------------------
		// Vertex

	#if defined(PASS_SHADOW_CASTER)
		void vertex_program (appdata v, out v2f_shadowCaster o, out float4 opos : SV_POSITION)
	#else
		v2f vertex_program (appdata v)
	#endif
		{
	#if !defined(PASS_SHADOW_CASTER)
			v2f o = (v2f)0;
			#if defined(CFXR_URP)
				o = (v2f)0;
			#else
				UNITY_INITIALIZE_OUTPUT(v2f, o);
			#endif
	#else
			o = (v2f_shadowCaster)0;
	#endif

			UNITY_SETUP_INSTANCE_ID(v);
			UNITY_TRANSFER_INSTANCE_ID(v, o);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	#if !defined(PASS_SHADOW_CASTER)
			o.pos = UnityObjectToClipPos(v.vertex);

		#if defined(LIGHTING_DIRECT) || (defined(LIGHTING_INDIRECT) && _NORMALMAP)
			// Particle Systems already output their vertex position in world space
			o.worldPos = v.vertex.xyz;

			#if defined(LIGHTING_BACK)
				o.viewDirWS = normalize(_WorldSpaceCameraPos.xyz - o.worldPos);
				//o.viewDirWS = normalize(mul((float3x3)unity_ObjectToWorld, v.vertex.xyz));
			#endif
		#endif

		#if defined(LIGHTING) && defined(_CFXR_LIGHTING_WPOS_OFFSET)
			o.worldPosDiff = _GameObjectWorldPosition.xyz - v.vertex.xyz;
		#endif
	#endif

			o.color = GetParticleColor(v.color);
			o.custom1 = v.texcoord1;
			GetParticleTexcoords(o.uv_random.xy, o.uv_random.zw, o.custom1.y, v.texcoord, v.texcoord1.y);
			//o.uv_random = v.texcoord;

	#if _CFXR_SECONDCOLOR_LERP || _CFXR_FONT_COLORS || (defined(LIGHTING) && _EMISSION)
			o.secondColor = v.texcoord2;
	#endif
	#if defined(LIGHTING)
			o.normalWS = v.normal.xyz;
		#if _NORMALMAP
			o.tangentWS = v.tangent;
		#endif
	#endif

			// Ambient Lighting / Light Probes, per-vertex if no normal map
	#if defined(LIGHTING_INDIRECT) && !_NORMALMAP
			half3 shColor = ShadeSHPerVertex(v.normal.xyz, half3(0,0,0));
			o.shColor = shColor;
	#endif

			/*
	#if !defined(PASS_SHADOW_CASTER)
			// Shadows Receiving
			TRANSFER_SHADOW(o);
	#endif
			*/

	#if defined(PASS_SHADOW_CASTER)
			vert(v, o, opos);
	#else
			return vert(v, o);
	#endif
		}

		// --------------------------------
		// Shade4PointLights Custom

		float3 CFXR_Shade4PointLights (
		float4 lightPosX, float4 lightPosY, float4 lightPosZ,
		float3 lightColor0, float3 lightColor1, float3 lightColor2, float3 lightColor3,
		float4 lightAttenSq,
		float3 pos, float3 normal)
		{
			// to light vectors
			float4 toLightX = lightPosX - pos.x;
			float4 toLightY = lightPosY - pos.y;
			float4 toLightZ = lightPosZ - pos.z;
			// squared lengths
			float4 lengthSq = 0;
			lengthSq += toLightX * toLightX;
			lengthSq += toLightY * toLightY;
			lengthSq += toLightZ * toLightZ;
			// don't produce NaNs if some vertex position overlaps with the light
			lengthSq = max(lengthSq, 0.000001);

			// NdotL
			float4 ndotl = 0;
			ndotl += toLightX * normal.x;
			ndotl += toLightY * normal.y;
			ndotl += toLightZ * normal.z;
			ndotl = smoothstep(0.5 - _DirectLightingRamp * 0.5, 0.5 + _DirectLightingRamp * 0.5, ndotl * 0.5 + 0.5);

			// correct NdotL
			float4 corr = rsqrt(lengthSq);
			ndotl = max (float4(0,0,0,0), ndotl * corr);

			// attenuation
			float4 atten = 1.0 / (1.0 + lengthSq * lightAttenSq);
			float4 diff = ndotl * atten;
			// final color
			float3 col = 0;
			col += lightColor0 * diff.x;
			col += lightColor1 * diff.y;
			col += lightColor2 * diff.z;
			col += lightColor3 * diff.w;
			return col;
		}

		// --------------------------------
		// Fragment

	#if defined(PASS_SHADOW_CASTER)
		float4 fragment_program (v2f_shadowCaster i, UNITY_VPOS_TYPE vpos : VPOS) : SV_Target
	#else
		half4 fragment_program (v2f i) : SV_Target
	#endif
		{
			UNITY_SETUP_INSTANCE_ID(i);
			UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

			// ================================================================
			// UV Distortion

		#if _CFXR_UV_DISTORTION
			#if _CFXR_UV2_DISTORTION
				float2 uvDistortion = tex2D(_DistortTex, i.custom1.xy * _DistortScrolling.zw + i.uv_random.zw + frac(_DistortScrolling.xy * _Time.yy)).rg;
			#else
				float2 uvDistortion = tex2D(_DistortTex, i.uv_random.xy * _DistortScrolling.zw + i.uv_random.zw + frac(_DistortScrolling.xy * _Time.yy)).rg;
			#endif

			#if _CFXR_UV_DISTORTION_ADD
				uvDistortion = i.uv_random.xy + (uvDistortion * 2.0 - 1.0) * _Distort;
			#else
				uvDistortion = lerp(i.uv_random.xy, uvDistortion, _Distort);
			#endif

			if (_FadeAlongU > 0)
			{
				uvDistortion = lerp(i.uv_random.xy, uvDistortion, i.uv_random.y * 0.5);
			}

			#define main_uv uvDistortion
		#else
			#define main_uv i.uv_random
		#endif

			// ================================================================
			// Color & Alpha

		#if _CFXR_SINGLE_CHANNEL
			half4 mainTex = half4(1, 1, 1, tex2D(_MainTex, main_uv.xy).r);
		#else
			half4 mainTex = tex2D(_MainTex, main_uv.xy);
		#endif

		#ifdef _FLIPBOOK_BLENDING
			#if _CFXR_SINGLE_CHANNEL
				half4 mainTex2 = tex2D(_MainTex, i.uv_random.zw).r;
			#else
				half4 mainTex2 = tex2D(_MainTex, i.uv_random.zw);
			#endif
				mainTex = lerp(mainTex, mainTex2, i.custom1.y);
		#endif

		#if _CFXR_OVERLAYTEX_1X
			float2 timeOffset = frac(_Time.yy * _OverlayTex_Scroll.xy);
			float2 overlayUv = ((i.uv_random.xy + i.uv_random.zw) * _OverlayTex_Scroll.zz) + timeOffset;
			half4 overlay = tex2D(_OverlayTex, overlayUv).r;
		#elif _CFXR_OVERLAYTEX_2X
			float2 timeOffset = frac(_Time.yy * _OverlayTex_Scroll.xy);
			float2 overlayUv = ((i.uv_random.xy + i.uv_random.zw) * _OverlayTex_Scroll.zz) + timeOffset;
			half4 overlay = tex2D(_OverlayTex, overlayUv).r;

			overlayUv = ((i.uv_random.xy + i.uv_random.wz) * _OverlayTex_Scroll.ww) + timeOffset;
			half4 overlay2 = tex2D(_OverlayTex, overlayUv).r;
			mainTex.a *= (overlay.r + overlay2.r) / 2.0;
		#endif

		#if _CFXR_OVERLAYTEX_1X || _CFXR_OVERLAYTEX_2X
			#if _CFXR_OVERLAYBLEND_A
			mainTex.a *= overlay.r;
			#elif _CFXR_OVERLAYBLEND_RGB
			mainTex.rgb *= overlay.rgb;
			#else
			mainTex.rgba *= overlay.rgba;
			#endif
		#endif

			/*
		#if _CFXR_GRADIENTMAP
			mainTex.rgb = tex2D(_GradientMap, mainTex.a).rgb;
		#endif
			*/

		#if _CFXR_FONT_COLORS
			half3 particleColor = mainTex.b * i.color.rgb + mainTex.g * i.custom1.rgb + mainTex.r * i.secondColor.rgb;
			half particleAlpha = mainTex.a * i.color.a;
		#else
			half3 particleColor = mainTex.rgb * i.color.rgb;
			half particleAlpha = mainTex.a * i.color.a;
		#endif

		#if _CFXR_SECONDCOLOR_LERP
			half secondColorMap = tex2D(_SecondColorTex, i.uv_random.xy).r;
			float time = lerp(-_SecondColorSmooth, 1+_SecondColorSmooth, i.secondColor.a);
			secondColorMap = smoothstep(secondColorMap - _SecondColorSmooth, secondColorMap + _SecondColorSmooth, time);
			particleColor.rgb += i.secondColor.rgb * secondColorMap;
		#endif

		#if _CFXR_HDR_BOOST
			#ifdef UNITY_COLORSPACE_GAMMA
				_HdrMultiply = LinearToGammaSpaceApprox(_HdrMultiply);
			#endif
			particleColor.rgb *= _HdrMultiply * GLOBAL_HDR_MULTIPLIER;
		#endif

			/*
		#if !defined(PASS_SHADOW_CASTER)
			// Shadows Receiving
			half shadows = SHADOW_ATTENUATION(i);
			particleColor.rgb *= saturate(shadows + (1.0 - _ReceivedShadowsStrength));
		#endif
			*/

			// ================================================================
			// Lighting

	#if !defined(PASS_SHADOW_CASTER)

		#if defined(LIGHTING)
			half3 particleLighting = half3(0, 0, 0);

			#if defined(CFXR_URP)
				#define UNPACK_SCALE_NORMAL UnpackNormalScale
				float3 mainLightDir = _MainLightPosition.xyz;
				float3 mainLightColor = _MainLightColor.rgb;
			#else
				#define UNPACK_SCALE_NORMAL UnpackScaleNormal
				float3 mainLightDir = _WorldSpaceLightPos0.xyz;
				float3 mainLightColor = _LightColor0.rgb;
			#endif

			#if _NORMALMAP
				half3 normalTS = UnpackScaleNormal_CFXR(tex2D(_BumpMap, main_uv.xy), _BumpScale);
				// tangent space to world space:
				float sgn = i.tangentWS.w;      // should be either +1 or -1
				float3 bitangent = sgn * cross(i.normalWS.xyz, i.tangentWS.xyz);
				float3 normalWS = mul(normalTS, half3x3(i.tangentWS.xyz, bitangent.xyz, i.normalWS.xyz));
			#else
				half3 normalWS = i.normalWS;
			#endif
			#if defined(_CFXR_LIGHTING_WPOS_OFFSET)
				normalWS = normalize(normalWS.xyz - i.worldPosDiff.xyz * _LightingWorldPosStrength);
			#endif
		#endif

			// - Direct
		#if defined(LIGHTING_DIRECT)
			// Main Directional
			half ndl = dot(normalWS, mainLightDir);
			ndl = smoothstep(0.5 - _DirectLightingRamp * 0.5, 0.5 + _DirectLightingRamp * 0.5, ndl * 0.5 + 0.5);
			half mainDirectLighting = max(0, ndl);

			#if defined(LIGHTING_BACK)
				half viewAtten = length(mainLightDir  + i.viewDirWS);
				viewAtten = 1 - smoothstep(0, _DirLightScreenAtten, viewAtten);
				mainDirectLighting += (viewAtten * viewAtten) * _BacklightTransmittance;
			#endif

			// particleColor.rgb = lerp(_ShadowColor.rgb, particleColor.rgb * mainLightColor.rgb, mainDirectLighting);
			particleColor.rgb *= lerp(_ShadowColor.rgb, mainLightColor.rgb, mainDirectLighting);

			// Point Lights
			#if defined(ENABLE_POINT_LIGHTS)
				#if defined(CFXR_URP)
					uint additionalLightsCount = GetAdditionalLightsCount();
					for (uint lightIndex = 0u; lightIndex < additionalLightsCount; ++lightIndex)
					{
						Light light = GetAdditionalLight(lightIndex, i.worldPos);
						half ndl = dot(normalWS, light.direction);
						ndl = smoothstep(0.5 - _DirectLightingRamp * 0.5, 0.5 + _DirectLightingRamp * 0.5, ndl * 0.5 + 0.5);
						ndl = max(0, ndl);

					#if defined(LIGHTING_BACK)
						half backNdl = dot(-normalWS, light.direction);
						backNdl = smoothstep(0.5 - _DirectLightingRamp * 0.5, 0.5 + _DirectLightingRamp * 0.5, backNdl * 0.5 + 0.5);
						ndl += max(0, backNdl) * _BacklightTransmittance;
					#endif

						particleColor.rgb += ndl * light.color.rgb * light.distanceAttenuation;
					}
				#else
					half3 pointLights = CFXR_Shade4PointLights(
						unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
						unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
						unity_4LightAtten0, i.worldPos, normalWS);

					#if defined(LIGHTING_BACK)
						// TODO Tweak CFXR_Shade4PointLights to handle both front & back lights more efficiently?
						pointLights += CFXR_Shade4PointLights(
						unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
						unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
						unity_4LightAtten0, i.worldPos, -normalWS) * _BacklightTransmittance;
					#endif

					particleColor.rgb += pointLights;
				#endif
			#endif
		#endif

			// - Indirect
		#if defined(LIGHTING_INDIRECT)
			#if _NORMALMAP
					half3 shColor = ShadeSHPerPixel(normalWS, half3(0,0,0), float3(0,0,0));
					particleColor.rgb += shColor.rgb * _IndirectLightingMix;
			#else
				particleColor.rgb += i.shColor.rgb * _IndirectLightingMix;
			#endif
		#endif

		#if defined(LIGHTING) && _EMISSION
			particleColor.rgb += i.secondColor.rgb;
		#endif

	#endif

			// ================================================================
			// Dissolve

		#if _CFXR_DISSOLVE
			#if _CFXR_DISSOLVE_ALONG_UV_X
				half dissolveOffset = tex2D(_DissolveTex, i.uv_random.xy * _DissolveTex_ST.xy + _DissolveTex_ST.zw + frac(_Time.yy * _DissolveScroll.xy)).r * 2.0 - 1.0;
				half dissolveTex = i.uv_random.x + dissolveOffset * i.custom1.z;
			#else
				half dissolveTex = tex2D(_DissolveTex, i.uv_random.xy).r;
			#endif
			dissolveTex = _InvertDissolveTex <= 0 ? 1 - dissolveTex : dissolveTex;
			half dissolveTime = i.custom1.x;
			half doubleDissolveWidth = 0;
			if (_DoubleDissolve > 0) doubleDissolveWidth = i.custom1.y;
		#else
			half dissolveTex = 0;
			half dissolveTime = 0;
			half doubleDissolveWidth = 0;
		#endif

			// ================================================================
			//

		#if defined(PASS_SHADOW_CASTER)
			return frag(i, vpos, particleColor, particleAlpha, dissolveTex, dissolveTime, doubleDissolveWidth);
		#else
			return frag(i, particleColor, particleAlpha, dissolveTex, dissolveTime, doubleDissolveWidth);
		#endif
		}

#endif

//--------------------------------------------------------------------------------------------------------------------------------
// PROCEDURAL GLOW
//--------------------------------------------------------------------------------------------------------------------------------

#if defined(CFXR_GLOW_SHADER)

			#include "UnityCG.cginc"
			#include "UnityStandardUtils.cginc"

			// --------------------------------

			#include "CFXR_SETTINGS.cginc"

			// --------------------------------

			CBUFFER_START(UnityPerMaterial)

			half _GlowMin;
			half _GlowMax;
			half _MaxValue;

			half _InvertDissolveTex;
			half _DissolveSmooth;

			half _HdrMultiply;

			half _Cutoff;

			half _SoftParticlesFadeDistanceNear;
			half _SoftParticlesFadeDistanceFar;
			half _EdgeFadePow;

		#if !defined(SHADER_API_GLES)
			float _ShadowStrength;
			float4 _DitherCustom_TexelSize;
		#endif

			CBUFFER_END

			sampler2D _DissolveTex;
			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
		#if !defined(SHADER_API_GLES)
			sampler3D _DitherMaskLOD;
			sampler3D _DitherCustom;
		#endif

			// --------------------------------
			// Input/Output

			struct appdata
			{
				float4 vertex		: POSITION;
				half4 color			: COLOR;
				float4 texcoord		: TEXCOORD0;	//xy = uv, zw = random
				float4 texcoord1	: TEXCOORD1;	//additional particle data: x = dissolve
		#if defined(PASS_SHADOW_CASTER)
				float3 normal : NORMAL;
		#endif
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			// vertex to fragment
			struct v2f
			{
				float4 pos				: SV_POSITION;
				half4 color				: COLOR;
				float4 uv_random		: TEXCOORD0;	//uv + particle data
				float4 custom1			: TEXCOORD1;	//additional particle data
		#if !defined(GLOBAL_DISABLE_SOFT_PARTICLES) && ((defined(SOFTPARTICLES_ON) || defined(CFXR_URP) || defined(SOFT_PARTICLES_ORTHOGRAPHIC)) && defined(_FADING_ON))
				float4 projPos			: TEXCOORD2;
		#endif
				UNITY_FOG_COORDS(3)		//note: does nothing if fog is not enabled
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			// vertex to fragment (shadow caster)
			struct v2f_shadowCaster
			{
				V2F_SHADOW_CASTER_NOPOS
				half4 color				: COLOR;
				float4 uv_random		: TEXCOORD1;	//uv + particle data
				float4 custom1			: TEXCOORD2;	//additional particle data
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			// --------------------------------

			#include "CFXR.cginc"

			// --------------------------------
			// Vertex

		#if defined(PASS_SHADOW_CASTER)
			void vertex_program (appdata v, out v2f_shadowCaster o, out float4 opos : SV_POSITION)
		#else
			v2f vertex_program (appdata v)
		#endif
			{
		#if !defined(PASS_SHADOW_CASTER)
				v2f o = (v2f)0;
				#if defined(CFXR_URP)
					o = (v2f)0;
				#else
					UNITY_INITIALIZE_OUTPUT(v2f, o);
				#endif
		#else
				o = (v2f_shadowCaster)0;
		#endif

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

		#if !defined(PASS_SHADOW_CASTER)
				o.pos = UnityObjectToClipPos(v.vertex);
		#endif

				o.color = GetParticleColor(v.color);
				o.custom1 = v.texcoord1;
				GetParticleTexcoords(o.uv_random.xy, o.uv_random.zw, o.custom1.y, v.texcoord, v.texcoord1.y);
				//o.uv_random = v.texcoord;

		#if defined(PASS_SHADOW_CASTER)
				vert(v, o, opos);
		#else
				return vert(v, o);
		#endif
			}

			// --------------------------------
			// Fragment

		#if defined(PASS_SHADOW_CASTER)
			float4 fragment_program (v2f_shadowCaster i, UNITY_VPOS_TYPE vpos : VPOS) : SV_Target
		#else
			half4 fragment_program (v2f i) : SV_Target
		#endif
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				// ================================================================
				// Color & Alpha

				//--------------------------------
				// procedural glow
				float2 uvm = i.uv_random.xy - 0.5;
				half glow = saturate(1 - ((dot(uvm, uvm) * 4)));
			#if _CFXR_GLOW_POW_P2
				glow = pow(glow, 2);
			#elif _CFXR_GLOW_POW_P4
				glow = pow(glow, 4);
			#elif _CFXR_GLOW_POW_P8
				glow = pow(glow, 8);
			#endif
				glow = clamp(lerp(_GlowMin, _GlowMax, glow), 0, _MaxValue) * saturate(glow * 30);
				half4 mainTex = half4(1, 1, 1, glow);
				//--------------------------------

			#if _CFXR_HDR_BOOST
				#ifdef UNITY_COLORSPACE_GAMMA
					_HdrMultiply = LinearToGammaSpaceApprox(_HdrMultiply);
				#endif
				mainTex.rgb *= _HdrMultiply * GLOBAL_HDR_MULTIPLIER;
			#endif

				half3 particleColor = mainTex.rgb * i.color.rgb;
				half particleAlpha = mainTex.a * i.color.a;

				// ================================================================
				// Dissolve

			#if _CFXR_DISSOLVE
				half dissolveTex = tex2D(_DissolveTex, i.uv_random.xy).r;
				dissolveTex = _InvertDissolveTex <= 0 ? 1 - dissolveTex : dissolveTex;
				half dissolveTime = i.custom1.x;
			#else
				half dissolveTex = 0;
				half dissolveTime = 0;
			#endif

				// ================================================================
				//

			#if defined(PASS_SHADOW_CASTER)
				return frag(i, vpos, particleColor, particleAlpha, dissolveTex, dissolveTime, 0.0);
			#else
				return frag(i, particleColor, particleAlpha, dissolveTex, dissolveTime, 0.0);
			#endif
			}

#endif

//--------------------------------------------------------------------------------------------------------------------------------
// SCREEN DISTORTION SHADER
//--------------------------------------------------------------------------------------------------------------------------------

#if defined(CFXR_SCREEN_DISTORTION_SHADER)

			#include "UnityCG.cginc"

			#if defined(CFXR_URP)
				#include "CFXR_URP.cginc"
			#endif

			#include "CFXR_SETTINGS.cginc"

			// --------------------------------

			CBUFFER_START(UnityPerMaterial)

			half _ScreenDistortionScale;

			half _Cutoff;

			half _SoftParticlesFadeDistanceNear;
			half _SoftParticlesFadeDistanceFar;
			half _EdgeFadePow;

			CBUFFER_END

			sampler2D _ScreenDistortionTex;
			#if defined(CFXR_URP)
				sampler2D _CameraOpaqueTexture;
				#define SampleScreenTexture(uv) tex2Dproj(_CameraOpaqueTexture, uv)
			#else
				sampler2D _GrabTexture;
				#define SampleScreenTexture(uv) tex2Dproj(_GrabTexture, uv)
			#endif
			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

			// --------------------------------
			// Input/output

			struct appdata
			{
				float4 vertex		: POSITION;
				half4 color			: COLOR;
				float4 texcoord		: TEXCOORD0;	//xy = uv, zw = random
				float4 texcoord1	: TEXCOORD1;	//additional particle data: x = dissolve, y = animFrame
				float4 texcoord2	: TEXCOORD2;	//additional particle data: second color
		#if _CFXR_EDGE_FADING
				float3 normal       : NORMAL;
		#endif
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			// vertex to fragment
			struct v2f
			{
				float4 pos				: SV_POSITION;
				half4 color				: COLOR;
				float4 uv_random		: TEXCOORD0;	//uv + particle data
				float4 custom1			: TEXCOORD1;	//additional particle data
				float4 grabPassPosition	: TEXCOORD2;
		#if !defined(GLOBAL_DISABLE_SOFT_PARTICLES) && ((defined(SOFTPARTICLES_ON) || defined(CFXR_URP) || defined(SOFT_PARTICLES_ORTHOGRAPHIC)) && defined(_FADING_ON))
				float4 projPos			: TEXCOORD3;
		#endif
		#if !defined(PASS_SHADOW_CASTER)
				UNITY_FOG_COORDS(4)		//note: does nothing if fog is not enabled
		#endif
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			// --------------------------------

			#include "CFXR.cginc"

			// --------------------------------
			// Vertex

			v2f vertex_program (appdata v)
			{
		#if !defined(PASS_SHADOW_CASTER)
				v2f o = (v2f)0;
				#if defined(CFXR_URP)
					o = (v2f)0;
				#else
					UNITY_INITIALIZE_OUTPUT(v2f, o);
				#endif
		#else
				o = (v2f_shadowCaster)0;
		#endif

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

		#if !defined(PASS_SHADOW_CASTER)
				o.pos = UnityObjectToClipPos(v.vertex);
				o.grabPassPosition = ComputeGrabScreenPos(o.pos);
		#endif

				o.color = GetParticleColor(v.color);
				o.custom1 = v.texcoord1;
				GetParticleTexcoords(o.uv_random.xy, o.uv_random.zw, o.custom1.y, v.texcoord, v.texcoord1.y);
				//o.uv_random = v.texcoord;

				return vert(v, o);
			}

			// --------------------------------
			// Fragment

			half4 fragment_program (v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);


				// ================================================================
				// Screen Distortion

				half4 distortionTex = tex2D(_ScreenDistortionTex, i.uv_random.xy);
				half particleAlpha = i.color.a * distortionTex.b;
				half2 screenDistortion = (distortionTex.rg * 2.0 - 1.0) * _ScreenDistortionScale * particleAlpha;
				float4 grabPosUV = i.grabPassPosition;
				grabPosUV.xy += screenDistortion;
				half3 particleColor = SampleScreenTexture(grabPosUV).rgb;

				#if defined(_DEBUG_VISUALIZE_DISTORTION)
					return half4(distortionTex.rg, 0, particleAlpha);
				#endif

			#if defined(PASS_SHADOW_CASTER)
				return frag(i, vpos, particleColor, particleAlpha, dissolveTex, dissolveTime, 0.0);
			#else
				return frag(i, particleColor, particleAlpha, 0, 0, 0.0);
			#endif
			}

#endif

//--------------------------------------------------------------------------------------------------------------------------------
// PROCEDURAL RING SHADER
//--------------------------------------------------------------------------------------------------------------------------------

#if defined(CFXR_PROCEDURAL_RING_SHADER)

			#include "UnityCG.cginc"
			#include "UnityStandardUtils.cginc"

			// --------------------------------

			#include "CFXR_SETTINGS.cginc"

			// --------------------------------

			CBUFFER_START(UnityPerMaterial)

			float4 _MainTex_ST;

			half _InvertDissolveTex;
			half _DissolveSmooth;

			half _HdrMultiply;

			float _RingTopOffset;

			half _Cutoff;

			half _SoftParticlesFadeDistanceNear;
			half _SoftParticlesFadeDistanceFar;
			half _EdgeFadePow;

		#if !defined(SHADER_API_GLES)
			float _ShadowStrength;
			float4 _DitherCustom_TexelSize;
		#endif

			CBUFFER_END

			sampler2D _MainTex;
			sampler2D _DissolveTex;
			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
		#if !defined(SHADER_API_GLES)
			sampler3D _DitherMaskLOD;
			sampler3D _DitherCustom;
		#endif

			// --------------------------------
			// Input/Output

			struct appdata
			{
				float4 vertex		: POSITION;
				half4 color			: COLOR;
				float4 texcoord		: TEXCOORD0;	//uv + particle data
				float4 texcoord1	: TEXCOORD1;	//additional particle data
				float4 texcoord2    : TEXCOORD2;    //procedural ring data: x = width, y = smooth, z = rotation, w = particle size
		#if defined(PASS_SHADOW_CASTER) || _CFXR_WORLD_SPACE_RING
				float3 normal : NORMAL;
		#endif
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			// vertex to fragment
			struct v2f
			{
				float4 pos					: SV_POSITION;
				half4 color					: COLOR;
				float4 uv_uv2				: TEXCOORD0;	//uv + particle data
				float4 ringData				: TEXCOORD1;    //procedural ring data
				float4 uvRing_uvCartesian	: TEXCOORD2;
		#if !defined(GLOBAL_DISABLE_SOFT_PARTICLES) && ((defined(SOFTPARTICLES_ON) || defined(CFXR_URP) || defined(SOFT_PARTICLES_ORTHOGRAPHIC)) && defined(_FADING_ON))
				float4 projPos				: TEXCOORD3;
		#endif
				UNITY_FOG_COORDS(4)		//note: does nothing if fog is not enabled
		#if _CFXR_DISSOLVE
				float4 custom1				: TEXCOORD5;
		#endif
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			// vertex to fragment (shadow caster)
			struct v2f_shadowCaster
			{
				V2F_SHADOW_CASTER_NOPOS
				half4 color					: COLOR;
				float4 uv_uv2				: TEXCOORD1;	//uv + particle data
				float4 ringData				: TEXCOORD2;    //procedural ring data
				float4 uvRing_uvCartesian	: TEXCOORD3;
		#if _CFXR_DISSOLVE
				float4 custom1				: TEXCOORD4;
		#endif
			};

			// --------------------------------

			#include "CFXR.cginc"

			// --------------------------------
			// Vertex

		#if defined(PASS_SHADOW_CASTER)
			void vertex_program (appdata v, out v2f_shadowCaster o, out float4 opos : SV_POSITION)
		#else
			v2f vertex_program (appdata v)
		#endif
			{
		#if !defined(PASS_SHADOW_CASTER)
				v2f o = (v2f)0;
				#if defined(CFXR_URP)
					o = (v2f)0;
				#else
					UNITY_INITIALIZE_OUTPUT(v2f, o);
				#endif
		#else
				o = (v2f_shadowCaster)0;
		#endif

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				//--------------------------------
				// procedural ring

				float ringWidth = v.texcoord2.x;
				float ringSmooth = v.texcoord2.y;
				float ringRotation = v.texcoord2.z;
				float particleSize = v.texcoord2.w;

				// avoid artifacts when vertex are pushed too much
				ringWidth = min(particleSize, ringWidth);

				// constants calculated per vertex
				o.ringData.x = pow(1 - ringWidth / particleSize, 2);
				o.ringData.y = 1 - _RingTopOffset;
				o.ringData.z = ringSmooth / particleSize; // smoothing depends on particle size
				o.ringData.w = ringRotation;

				// regular ring UVs
				float2 uv = v.texcoord.xy + float2(ringRotation, 0);
				o.uvRing_uvCartesian.xy = 1 - TRANSFORM_TEX(uv, _MainTex);


			#if _CFXR_WORLD_SPACE_RING
					// to clip space with width offset
					v.vertex.xyz = v.vertex.xyz - v.normal.xyz * v.texcoord.y * ringWidth;
				#if !defined(PASS_SHADOW_CASTER)
					o.pos = UnityObjectToClipPos(v.vertex);
				#endif
			#else
					// to clip space with width offset
					float4 m = mul(UNITY_MATRIX_V, v.vertex);
					m.xy += -v.texcoord.zw * v.texcoord.y * ringWidth;
				#if !defined(PASS_SHADOW_CASTER)
					o.pos = mul(UNITY_MATRIX_P, m);
				#endif
			#endif

				//------------------------------------------
				/*
				//v.vertex.xy += -v.texcoord.zw * v.texcoord.y * ringWidth;
				v.vertex.xy += v.texcoord.zw * v.texcoord.y * 0.5;
			#if !defined(PASS_SHADOW_CASTER)
				o.pos = UnityObjectToClipPos(v.vertex);
			#endif
				*/
				//------------------------------------------

				// calculate cartesian UVs to accurately calculate ring in fragment shader
				o.uvRing_uvCartesian.zw = v.texcoord.zw - v.texcoord.zw * v.texcoord.y * ringWidth / particleSize;

				//--------------------------------

				o.color = v.color;
				o.uv_uv2 = v.texcoord;

				//--------------------------------

		#if _CFXR_DISSOLVE
				o.custom1 = v.texcoord1;
		#endif

		#if defined(PASS_SHADOW_CASTER)
				vert(v, o, opos);
		#else
				return vert(v, o);
		#endif
			}

			// --------------------------------
			// Fragment

		#if defined(PASS_SHADOW_CASTER)
			float4 fragment_program (v2f_shadowCaster i, UNITY_VPOS_TYPE vpos : VPOS) : SV_Target
		#else
			half4 fragment_program (v2f i) : SV_Target
		#endif
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				// ================================================================
				// Color & Alpha

				//--------------------------------
				// procedural ring

				float b = i.ringData.x; // bottom
				float t = i.ringData.y; // top
				float smooth = i.ringData.z; // smoothing
				float gradient = dot(i.uvRing_uvCartesian.zw, i.uvRing_uvCartesian.zw);
				float ring = saturate( smoothstep(b, b + smooth, gradient) - smoothstep(t - smooth, t, gradient) );

			#if _CFXR_RADIAL_UV
				// approximate polar coordinates
				float2 radialUv = float2
				(
					(atan2(i.uvRing_uvCartesian.z, i.uvRing_uvCartesian.w) / UNITY_PI) * 0.5 + 0.5 + 0.23 - i.ringData.w,
					(gradient * (1.0 / (t - b)) - (b / (t - b))) * 0.9 - 0.92 + 1
				);
				radialUv.xy = radialUv.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				float dx = ddx(i.uvRing_uvCartesian.x);
				//float dy = ddx(i.uvRing_uvCartesian.x);
				#define TEX2D_MAIN_TEXCOORD(sampler) tex2Dgrad(sampler, radialUv, dx, dx)
			#else
				#define TEX2D_MAIN_TEXCOORD(sampler) tex2D(sampler, i.uvRing_uvCartesian.xy)
			#endif

				#if _CFXR_SINGLE_CHANNEL
				half4 mainTex = half4(1, 1, 1, TEX2D_MAIN_TEXCOORD(_MainTex).r);
				#else
				half4 mainTex = TEX2D_MAIN_TEXCOORD(_MainTex);
				#endif

				mainTex *= ring;

				//--------------------------------

				half3 particleColor = mainTex.rgb * i.color.rgb;
				half particleAlpha = mainTex.a * i.color.a;

			#if _CFXR_HDR_BOOST
				#ifdef UNITY_COLORSPACE_GAMMA
					_HdrMultiply = LinearToGammaSpaceApprox(_HdrMultiply);
				#endif
				particleColor.rgb *= _HdrMultiply * GLOBAL_HDR_MULTIPLIER;
			#endif

				// ================================================================
				// Dissolve

			#if _CFXR_DISSOLVE
				half dissolveTex = TEX2D_MAIN_TEXCOORD(_DissolveTex).r;
				dissolveTex = _InvertDissolveTex <= 0 ? 1 - dissolveTex : dissolveTex;
				half dissolveTime = i.custom1.x;
			#else
				half dissolveTex = 0;
				half dissolveTime = 0;
			#endif

				// ================================================================
				//

			#if defined(PASS_SHADOW_CASTER)
				return frag(i, vpos, particleColor, particleAlpha, dissolveTex, dissolveTime, 0.0);
			#else
				return frag(i, particleColor, particleAlpha, dissolveTex, dissolveTime, 0.0);
			#endif
			}

#endif