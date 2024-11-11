//--------------------------------------------------------------------------------------------------------------------------------
// Cartoon FX
// (c) 2012-2020 Jean Moreno
//--------------------------------------------------------------------------------------------------------------------------------

Shader "Cartoon FX/Remaster/Particle Procedural Glow"
{
	Properties
	{
	//# Blending
	//#

		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Blend Source", Float) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Blend Destination", Float) = 10
	
	//# --------------------------------------------------------

		[Toggle(_CFXR_DISSOLVE)] _UseDissolve ("Enable Dissolve", Float) = 0
	//# IF_KEYWORD _CFXR_DISSOLVE
		[NoScaleOffset] _DissolveTex ("Dissolve Texture", 2D) = "gray" {}
		_DissolveSmooth ("Dissolve Smoothing", Range(0.0001,0.5)) = 0.1
		[ToggleNoKeyword] _InvertDissolveTex ("Invert Dissolve Texture", Float) = 0
	//# END_IF

	//# --------------------------------------------------------

	//# Procedural Circle
	//#

		[KeywordEnum(P0, P2, P4, P8)] _CFXR_GLOW_POW ("Apply Power of", Float) = 0
		_GlowMin ("Circle Min", Float) = 0
		_GlowMax ("Circle Max", Float) = 1
	//#
		_MaxValue ("Max Value", Float) = 10

	//# --------------------------------------------------------

		[Toggle(_CFXR_HDR_BOOST)] _HdrBoost ("Enable HDR Multiplier", Float) = 0
	//# IF_KEYWORD _CFXR_HDR_BOOST
		_HdrMultiply ("HDR Multiplier", Float) = 2
	//# END_IF

	//# --------------------------------------------------------
	
		[Toggle(_FADING_ON)] _UseSP ("Soft Particles", Float) = 0
	//# IF_KEYWORD _FADING_ON
		_SoftParticlesFadeDistanceNear ("Near Fade", Float) = 0
		_SoftParticlesFadeDistanceFar ("Far Fade", Float) = 1
	//# END_IF

	//# ========================================================
	//# Shadows
	//#

		[KeywordEnum(Off,On,CustomTexture)] _CFXR_DITHERED_SHADOWS ("Dithered Shadows", Float) = 0
	//# IF_KEYWORD _CFXR_DITHERED_SHADOWS_ON || _CFXR_DITHERED_SHADOWS_CUSTOMTEXTURE
		_ShadowStrength		("Shadows Strength Max", Range(0,1)) = 1.0
		//#	IF_KEYWORD _CFXR_DITHERED_SHADOWS_CUSTOMTEXTURE
		_DitherCustom		("Dithering 3D Texture", 3D) = "black" {}
		//#	END_IF
	//# END_IF
	}
	
	Category
	{
		Tags
		{
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
			"PreviewType"="Plane"
		}
		Blend [_SrcBlend] [_DstBlend], One One
		Cull  Off
		ZWrite Off

		//====================================================================================================================================
		// Universal Rendering Pipeline

		SubShader
		{
			Pass
			{
				Name "BASE"
				Tags { "LightMode"="UniversalForward" }

				CGPROGRAM

				#pragma vertex vertex_program
				#pragma fragment fragment_program
				
				#pragma target 2.0
				
				// #pragma multi_compile_instancing
				// #pragma instancing_options procedural:ParticleInstancingSetup
				#pragma multi_compile_fog

				#pragma shader_feature_local _ _CFXR_GLOW_POW_P2 _CFXR_GLOW_POW_P4 _CFXR_GLOW_POW_P8
				#pragma shader_feature_local _ _CFXR_HDR_BOOST
				#pragma shader_feature_local _ _CFXR_DISSOLVE

				#pragma shader_feature_local _FADING_ON
				#pragma shader_feature_local _ _ALPHATEST_ON
				#pragma shader_feature_local _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON _CFXR_ADDITIVE

				#define CFXR_URP
				#define CFXR_GLOW_SHADER
				#include "CFXR_PASSES.cginc"

				ENDCG
			}
			
			// Same as above with 'Universal2D' instead and DISABLE_SOFT_PARTICLES keyword
			Pass
			{
				Name "BASE"
				Tags { "LightMode"="Universal2D" }

				CGPROGRAM

				#pragma vertex vertex_program
				#pragma fragment fragment_program
				
				#pragma target 2.0
				
				// #pragma multi_compile_instancing
				// #pragma instancing_options procedural:ParticleInstancingSetup
				#pragma multi_compile_fog

				#pragma shader_feature_local _ _CFXR_GLOW_POW_P2 _CFXR_GLOW_POW_P4 _CFXR_GLOW_POW_P8
				#pragma shader_feature_local _ _CFXR_HDR_BOOST
				#pragma shader_feature_local _ _CFXR_DISSOLVE

				#pragma shader_feature_local _FADING_ON
				#pragma shader_feature_local _ _ALPHATEST_ON
				#pragma shader_feature_local _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON _CFXR_ADDITIVE

				#define CFXR_URP
				#define DISABLE_SOFT_PARTICLES
				#define CFXR_GLOW_SHADER
				#include "CFXR_PASSES.cginc"

				ENDCG
			}

			//--------------------------------------------------------------------------------------------------------------------------------

			Pass
			{
				Name "ShadowCaster"
				Tags { "LightMode" = "ShadowCaster" }

				BlendOp Add
				Blend One Zero
				ZWrite On
				Cull Off
			
				CGPROGRAM

				#pragma vertex vertex_program
				#pragma fragment fragment_program

				#pragma shader_feature_local _ _CFXR_GLOW_POW_P2 _CFXR_GLOW_POW_P4 _CFXR_GLOW_POW_P8
				#pragma shader_feature_local _ _CFXR_DISSOLVE

				#pragma shader_feature_local _FADING_ON
				#pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON

				#pragma multi_compile_shadowcaster
				#pragma shader_feature_local _ _CFXR_DITHERED_SHADOWS_ON _CFXR_DITHERED_SHADOWS_CUSTOMTEXTURE

			#if (_CFXR_DITHERED_SHADOWS_ON || _CFXR_DITHERED_SHADOWS_CUSTOMTEXTURE) && !defined(SHADER_API_GLES)
				#pragma target 3.0		//needed for VPOS
			#endif

				#define CFXR_URP
				#define PASS_SHADOW_CASTER
				#define CFXR_GLOW_SHADER
				#include "CFXR_PASSES.cginc"

				ENDCG
			}
		}

		//====================================================================================================================================
		// Built-in Rendering Pipeline

		SubShader
		{
			Pass
			{
				Name "BASE"
				Tags { "LightMode"="ForwardBase" }

				CGPROGRAM

				#pragma vertex vertex_program
				#pragma fragment fragment_program
				
				//vertInstancingSetup writes to global, not allowed with DXC
				// #pragma never_use_dxc
				// #pragma target 2.5
				// #pragma multi_compile_instancing
				// #pragma instancing_options procedural:vertInstancingSetup

				#pragma multi_compile_particles
				#pragma multi_compile_fog
				
				#pragma shader_feature_local _ _CFXR_GLOW_POW_P2 _CFXR_GLOW_POW_P4 _CFXR_GLOW_POW_P8
				#pragma shader_feature_local _ _CFXR_HDR_BOOST
				#pragma shader_feature_local _ _CFXR_DISSOLVE

				#pragma shader_feature_local _FADING_ON
				#pragma shader_feature_local _ _ALPHATEST_ON
				#pragma shader_feature_local _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON _CFXR_ADDITIVE

				#include "UnityStandardParticleInstancing.cginc"

				#define CFXR_GLOW_SHADER
				#include "CFXR_PASSES.cginc"

				ENDCG
			}

			//--------------------------------------------------------------------------------------------------------------------------------

			Pass
			{
				Name "ShadowCaster"
				Tags { "LightMode" = "ShadowCaster" }

				BlendOp Add
				Blend One Zero
				ZWrite On
				Cull Off
			
				CGPROGRAM

				#pragma vertex vertex_program
				#pragma fragment fragment_program

				//vertInstancingSetup writes to global, not allowed with DXC
				// #pragma never_use_dxc
				// #pragma target 2.5
				// #pragma multi_compile_instancing
				// #pragma instancing_options procedural:vertInstancingSetup

				#pragma shader_feature_local _ _CFXR_GLOW_POW_P2 _CFXR_GLOW_POW_P4 _CFXR_GLOW_POW_P8
				#pragma shader_feature_local _ _CFXR_DISSOLVE

				#pragma shader_feature_local _FADING_ON
				#pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON

				#pragma multi_compile_shadowcaster
				#pragma shader_feature_local _ _CFXR_DITHERED_SHADOWS_ON _CFXR_DITHERED_SHADOWS_CUSTOMTEXTURE

			#if (_CFXR_DITHERED_SHADOWS_ON || _CFXR_DITHERED_SHADOWS_CUSTOMTEXTURE) && !defined(SHADER_API_GLES)
				#pragma target 3.0		//needed for VPOS
			#endif

				#include "UnityStandardParticleInstancing.cginc"

				#define PASS_SHADOW_CASTER
				#define CFXR_GLOW_SHADER
				#include "CFXR_PASSES.cginc"

				ENDCG
			}
		}
	}
	
	CustomEditor "CartoonFX.MaterialInspector"
}

