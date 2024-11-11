//--------------------------------------------------------------------------------------------------------------------------------
// Cartoon FX
// (c) 2012-2020 Jean Moreno
//--------------------------------------------------------------------------------------------------------------------------------

#if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
	#pragma exclude_renderers gles
#endif

#if defined(GLOBAL_DISABLE_SOFT_PARTICLES) && !defined(DISABLE_SOFT_PARTICLES)
	#define DISABLE_SOFT_PARTICLES
#endif

#if defined(CFXR_URP)
	float LinearEyeDepthURP(float depth, float4 zBufferParam)
	{
		return 1.0 / (zBufferParam.z * depth + zBufferParam.w);
	}

	float SoftParticles(float near, float far, float4 projection)
	{
		float sceneZ = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(projection)).r;

	#if defined(SOFT_PARTICLES_ORTHOGRAPHIC)
		// orthographic camera
		#if defined(UNITY_REVERSED_Z)
			sceneZ = 1.0f - sceneZ;
		#endif
		sceneZ = (sceneZ * _ProjectionParams.z) + _ProjectionParams.y;
	#else
		// perspective camera
		sceneZ = LinearEyeDepthURP(sceneZ, _ZBufferParams);
	#endif

		float fade = saturate (far * ((sceneZ - near) - projection.z));
		return fade;
	}
#else
	float SoftParticles(float near, float far, float4 projection)
	{
		float sceneZ = (SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(projection)));
	#if defined(SOFT_PARTICLES_ORTHOGRAPHIC)
		// orthographic camera
		#if defined(UNITY_REVERSED_Z)
			sceneZ = 1.0f - sceneZ;
		#endif
		sceneZ = (sceneZ * _ProjectionParams.z) + _ProjectionParams.y;
	#else
		// perspective camera
		sceneZ = LinearEyeDepth(sceneZ);
	#endif

		float fade = saturate (far * ((sceneZ - near) - projection.z));
		return fade;
	}
#endif

		float LinearToGammaSpaceApprox(float value)
		{
			return max(1.055h * pow(value, 0.416666667h) - 0.055h, 0.h);
		}
		
		// Same as UnityStandardUtils.cginc, but without the SHADER_TARGET limitation
		half3 UnpackScaleNormal_CFXR(half4 packednormal, half bumpScale)
		{
			#if defined(UNITY_NO_DXT5nm)
				half3 normal = packednormal.xyz * 2 - 1;
				// #if (SHADER_TARGET >= 30)
					// SM2.0: instruction count limitation
					// SM2.0: normal scaler is not supported
					normal.xy *= bumpScale;
				// #endif
				return normal;
			#else
				// This do the trick
				packednormal.x *= packednormal.w;

				half3 normal;
				normal.xy = (packednormal.xy * 2 - 1);
				// #if (SHADER_TARGET >= 30)
					// SM2.0: instruction count limitation
					// SM2.0: normal scaler is not supported
					normal.xy *= bumpScale;
				// #endif
				normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
				return normal;
			#endif
		}

		//Macros

		// Project Position
	#if !defined(PASS_SHADOW_CASTER) && !defined(GLOBAL_DISABLE_SOFT_PARTICLES) && !defined(DISABLE_SOFT_PARTICLES) && ( (defined(SOFTPARTICLES_ON) || defined(CFXR_URP) || defined(SOFT_PARTICLES_ORTHOGRAPHIC)) && defined(_FADING_ON) )
		#define vertProjPos(o, clipPos) \
			o.projPos = ComputeScreenPos(clipPos); \
			COMPUTE_EYEDEPTH(o.projPos.z);
	#else
		#define vertProjPos(o, clipPos)
	#endif

		// Soft Particles
	#if !defined(PASS_SHADOW_CASTER) && !defined(GLOBAL_DISABLE_SOFT_PARTICLES) && !defined(DISABLE_SOFT_PARTICLES) && ((defined(SOFTPARTICLES_ON) || defined(CFXR_URP) || defined(SOFT_PARTICLES_ORTHOGRAPHIC)) && defined(_FADING_ON))
		#define fragSoftParticlesFade(i, color) \
			color *= SoftParticles(_SoftParticlesFadeDistanceNear, _SoftParticlesFadeDistanceFar, i.projPos);
	#else
		#define fragSoftParticlesFade(i, color)
	#endif

		// Edge fade (note: particle meshes are already in world space)
	#if !defined(PASS_SHADOW_CASTER) && defined(_CFXR_EDGE_FADING)
		#define vertEdgeFade(v, color) \
			float3 viewDir = UnityWorldSpaceViewDir(v.vertex); \
			float ndv = abs(dot(normalize(viewDir), v.normal.xyz)); \
			color *= saturate(pow(ndv, _EdgeFadePow));
	#else
		#define vertEdgeFade(v, color)
	#endif

		// Fog
	#if _ALPHABLEND_ON
		#define applyFog(i, color, alpha)	UNITY_APPLY_FOG_COLOR(i.fogCoord, color, unity_FogColor);
	#elif _ALPHAPREMULTIPLY_ON
		#define applyFog(i, color, alpha)	UNITY_APPLY_FOG_COLOR(i.fogCoord, color, alpha * unity_FogColor);
	#elif _CFXR_ADDITIVE
		#define applyFog(i, color, alpha)	UNITY_APPLY_FOG_COLOR(i.fogCoord, color, half4(0, 0, 0, 0));
	#elif _ALPHAMODULATE_ON
		#define applyFog(i, color, alpha)	UNITY_APPLY_FOG_COLOR(i.fogCoord, color, half4(1, 1, 1, 1));
	#else
		#define applyFog(i, color, alpha)	UNITY_APPLY_FOG_COLOR(i.fogCoord, color, unity_FogColor);
	#endif

		// Vertex program
	#if defined(PASS_SHADOW_CASTER)
		void vert(appdata v, v2f_shadowCaster o, out float4 opos)
	#else
		v2f vert(appdata v, v2f o)
	#endif
		{
			UNITY_TRANSFER_FOG(o, o.pos);
			vertProjPos(o, o.pos);
			vertEdgeFade(v, o.color.a);

	#if defined(PASS_SHADOW_CASTER)
			TRANSFER_SHADOW_CASTER_NOPOS(o, opos);
	#else
			return o;
	#endif
		}

		// Fragment program
	#if defined(PASS_SHADOW_CASTER)
		float4 frag(v2f_shadowCaster i, UNITY_VPOS_TYPE vpos, half3 particleColor, half particleAlpha, half dissolve, half dissolveTime, half doubleDissolveWidth) : SV_Target
	#else
		half4 frag(v2f i, half3 particleColor, half particleAlpha, half dissolve, half dissolveTime, half doubleDissolveWidth) : SV_Target
	#endif
		{
		#if _CFXR_DISSOLVE
			// Dissolve
			half time = lerp(-_DissolveSmooth, 1+_DissolveSmooth, dissolveTime);
			particleAlpha *= smoothstep(dissolve - _DissolveSmooth, dissolve + _DissolveSmooth, time);
			if (doubleDissolveWidth > 0)
			{
				half dissolveSubtract = smoothstep(dissolve - _DissolveSmooth, dissolve + _DissolveSmooth, time - doubleDissolveWidth);
				particleAlpha = saturate(particleAlpha - dissolveSubtract);
			}
		#endif

			//Blending
		#if _ALPHAPREMULTIPLY_ON
			particleColor *= particleAlpha;
		#endif
		#if _ALPHAMODULATE_ON
			particleColor.rgb = lerp(float3(1,1,1), particleColor.rgb, particleAlpha);
		#endif

		#if _ALPHATEST_ON
			clip(particleAlpha - _Cutoff);
		#endif

		#if !defined(PASS_SHADOW_CASTER)
			// Fog & Soft Particles
			applyFog(i, particleColor, particleAlpha);
			fragSoftParticlesFade(i, particleAlpha);
		#endif

			// Prevent alpha from exceeding 1
			particleAlpha = min(particleAlpha, 1.0);

		#if !defined(PASS_SHADOW_CASTER)
			return float4(particleColor, particleAlpha);
		#else

			//--------------------------------------------------------------------------------------------------------------------------------
			// Shadow Caster Pass

		#if _CFXR_ADDITIVE
			half alpha = max(particleColor.r, max(particleColor.g, particleColor.b)) * particleAlpha;
		#else
			half alpha = particleAlpha;
		#endif

		#if (_CFXR_DITHERED_SHADOWS_ON || _CFXR_DITHERED_SHADOWS_CUSTOMTEXTURE) && !defined(SHADER_API_GLES)
			alpha = min(alpha, _ShadowStrength);
			// Use dither mask for alpha blended shadows, based on pixel position xy
			// and alpha level. Our dither texture is 4x4x16.
			#if _CFXR_DITHERED_SHADOWS_CUSTOMTEXTURE
			half texSize = _DitherCustom_TexelSize.z;
			alpha = tex3D(_DitherCustom, float3(vpos.xy*(1 / texSize), alpha*(1 - (1 / (texSize*texSize))))).a;
			#else
			alpha = tex3D(_DitherMaskLOD, float3(vpos.xy*0.25, alpha*0.9375)).a;
			#endif
		#endif
			clip(alpha - 0.01);
			SHADOW_CASTER_FRAGMENT(i)
		#endif
		}

	// ================================================================================================================================
	// ParticlesInstancing.hlsl
	// ================================================================================================================================

#if defined(CFXR_URP)
	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) && !defined(SHADER_TARGET_SURFACE_ANALYSIS)
		#define UNITY_PARTICLE_INSTANCING_ENABLED
	#endif

	#if defined(UNITY_PARTICLE_INSTANCING_ENABLED)

		#ifndef UNITY_PARTICLE_INSTANCE_DATA
			#define UNITY_PARTICLE_INSTANCE_DATA DefaultParticleInstanceData
		#endif

		struct DefaultParticleInstanceData
		{
			float3x4 transform;
			uint color;
			float animFrame;
		};

		StructuredBuffer<UNITY_PARTICLE_INSTANCE_DATA> unity_ParticleInstanceData;
		float4 unity_ParticleUVShiftData;
		float unity_ParticleUseMeshColors;

		void ParticleInstancingMatrices(out float4x4 objectToWorld, out float4x4 worldToObject)
		{
			UNITY_PARTICLE_INSTANCE_DATA data = unity_ParticleInstanceData[unity_InstanceID];

			// transform matrix
			objectToWorld._11_21_31_41 = float4(data.transform._11_21_31, 0.0f);
			objectToWorld._12_22_32_42 = float4(data.transform._12_22_32, 0.0f);
			objectToWorld._13_23_33_43 = float4(data.transform._13_23_33, 0.0f);
			objectToWorld._14_24_34_44 = float4(data.transform._14_24_34, 1.0f);

			// inverse transform matrix (TODO: replace with a library implementation if/when available)
			float3x3 worldToObject3x3;
			worldToObject3x3[0] = objectToWorld[1].yzx * objectToWorld[2].zxy - objectToWorld[1].zxy * objectToWorld[2].yzx;
			worldToObject3x3[1] = objectToWorld[0].zxy * objectToWorld[2].yzx - objectToWorld[0].yzx * objectToWorld[2].zxy;
			worldToObject3x3[2] = objectToWorld[0].yzx * objectToWorld[1].zxy - objectToWorld[0].zxy * objectToWorld[1].yzx;

			float det = dot(objectToWorld[0].xyz, worldToObject3x3[0]);

			worldToObject3x3 = transpose(worldToObject3x3);

			worldToObject3x3 *= rcp(det);

			float3 worldToObjectPosition = mul(worldToObject3x3, -objectToWorld._14_24_34);

			worldToObject._11_21_31_41 = float4(worldToObject3x3._11_21_31, 0.0f);
			worldToObject._12_22_32_42 = float4(worldToObject3x3._12_22_32, 0.0f);
			worldToObject._13_23_33_43 = float4(worldToObject3x3._13_23_33, 0.0f);
			worldToObject._14_24_34_44 = float4(worldToObjectPosition, 1.0f);
		}

		void ParticleInstancingSetup()
		{
			ParticleInstancingMatrices(unity_ObjectToWorld, unity_WorldToObject);
		}

	#else

		void ParticleInstancingSetup() {}

	#endif
#endif

	// ================================================================================================================================
	// Instancing functions
	// ================================================================================================================================

	float4 UnpackFromR8G8B8A8(uint rgba)
	{
		return float4(rgba & 255, (rgba >> 8) & 255, (rgba >> 16) & 255, (rgba >> 24) & 255) * (1.0 / 255);
	}

	half4 GetParticleColor(half4 color)
	{
		#if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
			#if !defined(UNITY_PARTICLE_INSTANCE_DATA_NO_COLOR)
				UNITY_PARTICLE_INSTANCE_DATA data = unity_ParticleInstanceData[unity_InstanceID];
				color = lerp(half4(1.0, 1.0, 1.0, 1.0), color, unity_ParticleUseMeshColors);
				color *= UnpackFromR8G8B8A8(data.color);
			#endif
		#endif
		return color;
	}

	void GetParticleTexcoords(out float2 outputTexcoord, out float2 outputTexcoord2, out float outputBlend, in float4 inputTexcoords, in float inputBlend)
	{
		#if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
			if (unity_ParticleUVShiftData.x != 0.0)
			{
				UNITY_PARTICLE_INSTANCE_DATA data = unity_ParticleInstanceData[unity_InstanceID];

				float numTilesX = unity_ParticleUVShiftData.y;
				float2 animScale = unity_ParticleUVShiftData.zw;
				#ifdef UNITY_PARTICLE_INSTANCE_DATA_NO_ANIM_FRAME
					float sheetIndex = 0.0;
				#else
					float sheetIndex = data.animFrame;
				#endif

				float index0 = floor(sheetIndex);
				float vIdx0 = floor(index0 / numTilesX);
				float uIdx0 = floor(index0 - vIdx0 * numTilesX);
				float2 offset0 = float2(uIdx0 * animScale.x, (1.0 - animScale.y) - vIdx0 * animScale.y); // Copied from built-in as is and it looks like upside-down flip

				outputTexcoord = inputTexcoords.xy * animScale.xy + offset0.xy;

				#ifdef _FLIPBOOKBLENDING_ON
					float index1 = floor(sheetIndex + 1.0);
					float vIdx1 = floor(index1 / numTilesX);
					float uIdx1 = floor(index1 - vIdx1 * numTilesX);
					float2 offset1 = float2(uIdx1 * animScale.x, (1.0 - animScale.y) - vIdx1 * animScale.y);

					outputTexcoord2.xy = inputTexcoords.xy * animScale.xy + offset1.xy;
					outputBlend = frac(sheetIndex);
				#endif
			}
			else
		#endif
			{
				outputTexcoord = inputTexcoords.xy;
				#ifdef _FLIPBOOKBLENDING_ON
					outputTexcoord2.xy = inputTexcoords.zw;
					outputBlend = inputBlend;
				#endif
			}

		#ifndef _FLIPBOOKBLENDING_ON
			outputTexcoord2.xy = inputTexcoords.xy;
			outputBlend = 0.5;
		#endif
	}

	void GetParticleTexcoords(out float2 outputTexcoord, in float2 inputTexcoord)
	{
		float2 dummyTexcoord2 = 0.0;
		float dummyBlend = 0.0;
		GetParticleTexcoords(outputTexcoord, dummyTexcoord2, dummyBlend, inputTexcoord.xyxy, 0.0);
	}
