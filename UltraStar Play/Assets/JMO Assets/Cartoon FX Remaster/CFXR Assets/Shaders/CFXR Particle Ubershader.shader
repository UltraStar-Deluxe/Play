//--------------------------------------------------------------------------------------------------------------------------------
// Cartoon FX
// (c) 2012-2020 Jean Moreno
//--------------------------------------------------------------------------------------------------------------------------------

Shader "Cartoon FX/Remaster/Particle Ubershader"
{
	Properties
	{
	//# Blending
	//#
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Blend Source", Float) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Blend Destination", Float) = 10
		[KeywordEnumNoPrefix(Alpha Blending, _ALPHABLEND_ON, Alpha Blending Premultiplied, _ALPHAPREMULTIPLY_ON, Multiplicative, _ALPHAMODULATE_ON, Additive, _CFXR_ADDITIVE)] _BlendingType ("Blending Type", Float) = 0

	//# 
		[ToggleNoKeyword] _ZWrite ("Depth Write", Float) = 0
		[Toggle(_ALPHATEST_ON)] _UseAlphaClip ("Alpha Clipping (Cutout)", Float) = 0
	//# IF_KEYWORD _ALPHATEST_ON
		_Cutoff ("Cutoff Threshold", Range(0.001,1)) = 0.1
	//# END_IF
	
	//# --------------------------------------------------------
	
		[Toggle(_FADING_ON)] _UseSP ("Soft Particles", Float) = 0
	//# IF_KEYWORD _FADING_ON
		_SoftParticlesFadeDistanceNear ("Near Fade", Float) = 0
		_SoftParticlesFadeDistanceFar ("Far Fade", Float) = 1
	//# END_IF

	//# 

		[Toggle(_CFXR_EDGE_FADING)] _UseEF ("Edge Fade", Float) = 0
	//# IF_KEYWORD _CFXR_EDGE_FADING
		_EdgeFadePow ("Edge Fade Power", Float) = 1
	//# END_IF

	//# 

	//# ========================================================

	//# Effects
	//#

		[Toggle(_CFXR_DISSOLVE)] _UseDissolve ("Enable Dissolve", Float) = 0
	//# IF_KEYWORD _CFXR_DISSOLVE
		_DissolveTex ("Dissolve Texture", 2D) = "gray" {}
		_DissolveSmooth ("Dissolve Smoothing", Range(0.0001,0.5)) = 0.1
		[ToggleNoKeyword] _InvertDissolveTex ("Invert Dissolve Texture", Float) = 0
		[ToggleNoKeyword] _DoubleDissolve ("Double Dissolve", Float) = 0
		[Toggle(_CFXR_DISSOLVE_ALONG_UV_X)] _UseDissolveOffsetUV ("Dissolve offset along X", Float) = 0
	//# IF_KEYWORD _CFXR_DISSOLVE_ALONG_UV_X
		_DissolveScroll ("UV Scrolling", Vector) = (0,0,0,0)
	//# END_IF
	//# END_IF

	//# --------------------------------------------------------

		[Toggle(_CFXR_UV_DISTORTION)] _UseUVDistortion ("Enable UV Distortion", Float) = 0
	//# IF_KEYWORD _CFXR_UV_DISTORTION
		
		[NoScaleOffset] _DistortTex ("Distortion Texture", 2D) = "gray" {}
		_DistortScrolling ("Scroll (XY) Tile (ZW)", Vector) = (0,0,1,1)
		[Toggle(_CFXR_UV2_DISTORTION)] _UseUV2Distortion ("Use UV2", Float) = 0
		_Distort ("Distortion Strength", Range(0,2.0)) = 0.1
		[ToggleNoKeyword] _FadeAlongU ("Fade along Y", Float) = 0
		[Toggle(_CFXR_UV_DISTORTION_ADD)] _UVDistortionAdd ("Add to base UV", Float) = 0
	//# END_IF

	//# ========================================================

	//# Colors
	//#

		[NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
		[Toggle(_CFXR_SINGLE_CHANNEL)] _SingleChannel ("Single Channel Texture", Float) = 0

	//# --------------------------------------------------------

		[KeywordEnum(Off,1x,2x)] _CFXR_OVERLAYTEX ("Enable Overlay Texture", Float) = 0
	//# IF_KEYWORD _CFXR_OVERLAYTEX_1X || _CFXR_OVERLAYTEX_2X
		[KeywordEnum(RGBA,RGB,A)] _CFXR_OVERLAYBLEND ("Overlay Blend Channels", Float) = 0
		[NoScaleOffset] _OverlayTex ("Overlay Texture", 2D) = "white" {}
		_OverlayTex_Scroll ("Overlay Scrolling / Scale", Vector) = (0.1,0.1,1,1)
	//# END_IF

	//# --------------------------------------------------------

		[Toggle(_FLIPBOOK_BLENDING)] _UseFB ("Flipbook Blending", Float) = 0

	//# --------------------------------------------------------

		[Toggle(_CFXR_SECONDCOLOR_LERP)] _UseSecondColor ("Secondary Vertex Color (TEXCOORD2)", Float) = 0
	//# IF_KEYWORD _CFXR_SECONDCOLOR_LERP
		[NoScaleOffset] _SecondColorTex ("Second Color Map", 2D) = "black" {}
		_SecondColorSmooth ("Second Color Smoothing", Range(0.0001,0.5)) = 0.2
	//# END_IF

	//# --------------------------------------------------------

		[Toggle(_CFXR_FONT_COLORS)] _UseFontColor ("Use Font Colors", Float) = 0

//	//# --------------------------------------------------------
//
//	[Toggle(_CFXR_GRADIENTMAP)] _UseGradientMap ("Gradient Map", Float) = 0
//	//# IF_KEYWORD _CFXR_GRADIENTMAP
//		[NoScaleOffset] _GradientMap ("Gradient Map", 2D) = "black" {}
//	//# END_IF

	//# --------------------------------------------------------

		[Toggle(_CFXR_HDR_BOOST)] _HdrBoost ("Enable HDR Multiplier", Float) = 0
	//# IF_KEYWORD _CFXR_HDR_BOOST
		 _HdrMultiply ("HDR Multiplier", Float) = 2
	//# END_IF

	//# --------------------------------------------------------
	
	//# Lighting
	//#

		[KeywordEnumNoPrefix(Off, _, Direct, _CFXR_LIGHTING_DIRECT, Indirect, _CFXR_LIGHTING_INDIRECT, Both, _CFXR_LIGHTING_ALL)] _UseLighting ("Mode", Float) = 0
	//# IF_KEYWORD _CFXR_LIGHTING_DIRECT || _CFXR_LIGHTING_ALL
		_DirectLightingRamp ("Direct Lighting Ramp", Range(0,1)) = 1.0
	//# END_IF
	//# 
	//# IF_KEYWORD _CFXR_LIGHTING_DIRECT || _CFXR_LIGHTING_INDIRECT || _CFXR_LIGHTING_ALL
		[Toggle(_NORMALMAP)] _UseNormalMap ("Enable Normal Map", Float) = 0
	//# IF_KEYWORD _NORMALMAP
		[NoScaleOffset] _BumpMap ("Normal Map", 2D) = "bump" {}
		_BumpScale ("Normal Scale", Range(-1, 1)) = 1.0
	//# END_IF
	//# 
		[Toggle(_EMISSION)] _UseEmission ("Enable Emission (TEXCOORD2)", Float) = 0
	//# 
		[Toggle(_CFXR_LIGHTING_WPOS_OFFSET)] _UseLightingWorldPosOffset ("Enable World Pos. Offset", Float) = 0
	//# IF_KEYWORD _CFXR_LIGHTING_WPOS_OFFSET
		_LightingWorldPosStrength ("Offset Strength", Range(0,1)) = 0.2
	//# END_IF
	//# 
		[Toggle(_CFXR_LIGHTING_BACK)] _UseBackLighting ("Enable Backlighting", Float) = 0
	//# IF_KEYWORD _CFXR_LIGHTING_BACK
		_DirLightScreenAtten ("Dir. Light Screen Attenuation", Range(0, 5)) = 1.0
		_BacklightTransmittance ("Backlight Transmittance", Range(0, 2)) = 1.0
	//# END_IF
	//# 
	//# IF_KEYWORD _CFXR_LIGHTING_INDIRECT || _CFXR_LIGHTING_ALL
		_IndirectLightingMix ("Indirect Lighting Mix", Range(0,1)) = 0.5
	//# END_IF
		_ShadowColor ("Shadow Color", Color) = (0,0,0,1)
	//# 
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

//		_ReceivedShadowsStrength ("Received Shadows Strength", Range(0,1)) = 0.5
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
		ZWrite [_ZWrite]
		Cull  Off

		//====================================================================================================================================
		// Universal Rendering Pipeline

		Subshader
		{
			Pass
			{
				Name "BASE_URP"
				Tags { "LightMode"="UniversalForward" }

				CGPROGRAM

				#pragma vertex vertex_program
				#pragma fragment fragment_program
				
				#pragma target 2.0
				
				// #pragma multi_compile_instancing
				// #pragma instancing_options procedural:ParticleInstancingSetup

				#pragma multi_compile_fog
				//#pragma multi_compile_fwdbase
				//#pragma multi_compile SHADOWS_SCREEN

				#pragma shader_feature_local _ _CFXR_SINGLE_CHANNEL
				#pragma shader_feature_local _ _CFXR_DISSOLVE
				#pragma shader_feature_local _ _CFXR_DISSOLVE_ALONG_UV_X
				#pragma shader_feature_local _ _CFXR_UV_DISTORTION
				#pragma shader_feature_local _ _CFXR_UV2_DISTORTION
				#pragma shader_feature_local _ _CFXR_UV_DISTORTION_ADD
				// #pragma shader_feature_local _ _CFXR_GRADIENTMAP
				#pragma shader_feature_local _ _CFXR_SECONDCOLOR_LERP _CFXR_FONT_COLORS
				#pragma shader_feature_local _ _CFXR_OVERLAYTEX_1X _CFXR_OVERLAYTEX_2X
				#pragma shader_feature_local _ _CFXR_OVERLAYBLEND_A _CFXR_OVERLAYBLEND_RGB
				#pragma shader_feature_local _ _CFXR_HDR_BOOST
				#pragma shader_feature_local _ _CFXR_EDGE_FADING
				#pragma shader_feature_local _ _CFXR_LIGHTING_DIRECT _CFXR_LIGHTING_INDIRECT _CFXR_LIGHTING_ALL
				#pragma shader_feature_local _ _CFXR_LIGHTING_WPOS_OFFSET
				#pragma shader_feature_local _ _CFXR_LIGHTING_BACK

				// Using the same keywords as Unity's Standard Particle shader to minimize project-wide keyword usage
				#pragma shader_feature_local _ _NORMALMAP
				#pragma shader_feature_local _ _EMISSION
				#pragma shader_feature_local _ _FLIPBOOK_BLENDING
				#pragma shader_feature_local _ _FADING_ON
				#pragma shader_feature_local _ _ALPHATEST_ON
				#pragma shader_feature_local _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON _CFXR_ADDITIVE

				#define CFXR_URP
				#define CFXR_UBERSHADER
				#include "CFXR_PASSES.cginc"

				ENDCG
			}
			
			// Same as above with 'Universal2D' instead and DISABLE_SOFT_PARTICLES keyword
			Pass
			{
				Name "BASE_URP"
				Tags { "LightMode"="Universal2D" }

				CGPROGRAM

				#pragma vertex vertex_program
				#pragma fragment fragment_program
				
				#pragma target 2.0
				
				// #pragma multi_compile_instancing
				// #pragma instancing_options procedural:ParticleInstancingSetup

				#pragma multi_compile_fog
				//#pragma multi_compile_fwdbase
				//#pragma multi_compile SHADOWS_SCREEN

				#pragma shader_feature_local _ _CFXR_SINGLE_CHANNEL
				#pragma shader_feature_local _ _CFXR_DISSOLVE
				#pragma shader_feature_local _ _CFXR_DISSOLVE_ALONG_UV_X
				#pragma shader_feature_local _ _CFXR_UV_DISTORTION
				#pragma shader_feature_local _ _CFXR_UV2_DISTORTION
				#pragma shader_feature_local _ _CFXR_UV_DISTORTION_ADD
				// #pragma shader_feature_local _ _CFXR_GRADIENTMAP
				#pragma shader_feature_local _ _CFXR_SECONDCOLOR_LERP _CFXR_FONT_COLORS
				#pragma shader_feature_local _ _CFXR_OVERLAYTEX_1X _CFXR_OVERLAYTEX_2X
				#pragma shader_feature_local _ _CFXR_OVERLAYBLEND_A _CFXR_OVERLAYBLEND_RGB
				#pragma shader_feature_local _ _CFXR_HDR_BOOST
				#pragma shader_feature_local _ _CFXR_EDGE_FADING
				#pragma shader_feature_local _ _CFXR_LIGHTING_DIRECT _CFXR_LIGHTING_INDIRECT _CFXR_LIGHTING_ALL
				#pragma shader_feature_local _ _CFXR_LIGHTING_WPOS_OFFSET
				#pragma shader_feature_local _ _CFXR_LIGHTING_BACK

				// Using the same keywords as Unity's Standard Particle shader to minimize project-wide keyword usage
				#pragma shader_feature_local _ _NORMALMAP
				#pragma shader_feature_local _ _EMISSION
				#pragma shader_feature_local _ _FLIPBOOK_BLENDING
				#pragma shader_feature_local _ _FADING_ON
				#pragma shader_feature_local _ _ALPHATEST_ON
				#pragma shader_feature_local _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON _CFXR_ADDITIVE

				#define CFXR_UPR
				#define DISABLE_SOFT_PARTICLES
				#define CFXR_UBERSHADER
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

				#pragma shader_feature_local _ _CFXR_SINGLE_CHANNEL
				#pragma shader_feature_local _ _CFXR_DISSOLVE
				#pragma shader_feature_local _ _CFXR_DISSOLVE_ALONG_UV_X
				#pragma shader_feature_local _ _CFXR_UV_DISTORTION
				#pragma shader_feature_local _ _CFXR_UV2_DISTORTION
				#pragma shader_feature_local _ _CFXR_UV_DISTORTION_ADD
				#pragma shader_feature_local _ _CFXR_OVERLAYTEX_1X _CFXR_OVERLAYTEX_2X
				#pragma shader_feature_local _ _CFXR_OVERLAYBLEND_A _CFXR_OVERLAYBLEND_RGB
				#pragma shader_feature_local _ _FLIPBOOK_BLENDING

				#pragma shader_feature_local _ _ALPHATEST_ON
				#pragma shader_feature_local _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON _CFXR_ADDITIVE

				#pragma multi_compile_shadowcaster
				#pragma shader_feature_local _ _CFXR_DITHERED_SHADOWS_ON _CFXR_DITHERED_SHADOWS_CUSTOMTEXTURE

			#if (_CFXR_DITHERED_SHADOWS_ON || _CFXR_DITHERED_SHADOWS_CUSTOMTEXTURE) && !defined(SHADER_API_GLES)
				#pragma target 3.0		//needed for VPOS
			#endif

				#define CFXR_UPR
				#define PASS_SHADOW_CASTER
				#define CFXR_UBERSHADER
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
				//#pragma multi_compile_fwdbase
				//#pragma multi_compile SHADOWS_SCREEN

				#pragma shader_feature_local _ _CFXR_SINGLE_CHANNEL
				#pragma shader_feature_local _ _CFXR_DISSOLVE
				#pragma shader_feature_local _ _CFXR_DISSOLVE_ALONG_UV_X
				#pragma shader_feature_local _ _CFXR_UV_DISTORTION
				#pragma shader_feature_local _ _CFXR_UV2_DISTORTION
				#pragma shader_feature_local _ _CFXR_UV_DISTORTION_ADD
				// #pragma shader_feature_local _ _CFXR_GRADIENTMAP
				#pragma shader_feature_local _ _CFXR_SECONDCOLOR_LERP _CFXR_FONT_COLORS
				#pragma shader_feature_local _ _CFXR_OVERLAYTEX_1X _CFXR_OVERLAYTEX_2X
				#pragma shader_feature_local _ _CFXR_OVERLAYBLEND_A _CFXR_OVERLAYBLEND_RGB
				#pragma shader_feature_local _ _CFXR_HDR_BOOST
				#pragma shader_feature_local _ _CFXR_EDGE_FADING
				#pragma shader_feature_local _ _CFXR_LIGHTING_DIRECT _CFXR_LIGHTING_INDIRECT _CFXR_LIGHTING_ALL
				#pragma shader_feature_local _ _CFXR_LIGHTING_WPOS_OFFSET
				#pragma shader_feature_local _ _CFXR_LIGHTING_BACK

				// Using the same keywords as Unity's Standard Particle shader to minimize project-wide keyword usage
				#pragma shader_feature_local _ _NORMALMAP
				#pragma shader_feature_local _ _EMISSION
				#pragma shader_feature_local _ _FLIPBOOK_BLENDING
				#pragma shader_feature_local _ _FADING_ON
				#pragma shader_feature_local _ _ALPHATEST_ON
				#pragma shader_feature_local _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON _CFXR_ADDITIVE

				#include "UnityStandardParticleInstancing.cginc"
				#define CFXR_UBERSHADER
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

				#pragma shader_feature_local _ _CFXR_SINGLE_CHANNEL
				#pragma shader_feature_local _ _CFXR_DISSOLVE
				#pragma shader_feature_local _ _CFXR_DISSOLVE_ALONG_UV_X
				#pragma shader_feature_local _ _CFXR_UV_DISTORTION
				#pragma shader_feature_local _ _CFXR_UV2_DISTORTION
				#pragma shader_feature_local _ _CFXR_UV_DISTORTION_ADD
				#pragma shader_feature_local _ _CFXR_OVERLAYTEX_1X _CFXR_OVERLAYTEX_2X
				#pragma shader_feature_local _ _CFXR_OVERLAYBLEND_A _CFXR_OVERLAYBLEND_RGB
				#pragma shader_feature_local _ _FLIPBOOK_BLENDING

				#pragma shader_feature_local _ _ALPHATEST_ON
				#pragma shader_feature_local _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON _CFXR_ADDITIVE

				#pragma multi_compile_shadowcaster
				#pragma shader_feature_local _ _CFXR_DITHERED_SHADOWS_ON _CFXR_DITHERED_SHADOWS_CUSTOMTEXTURE

			#if (_CFXR_DITHERED_SHADOWS_ON || _CFXR_DITHERED_SHADOWS_CUSTOMTEXTURE) && !defined(SHADER_API_GLES)
				#pragma target 3.0		//needed for VPOS
			#endif

				#include "UnityStandardParticleInstancing.cginc"

				#define PASS_SHADOW_CASTER
				#define CFXR_UBERSHADER
				#include "CFXR_PASSES.cginc"

				ENDCG
			}
		}
	}
	
	CustomEditor "CartoonFX.MaterialInspector"
}

