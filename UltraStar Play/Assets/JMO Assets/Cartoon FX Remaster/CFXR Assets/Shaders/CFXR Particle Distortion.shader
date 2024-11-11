//--------------------------------------------------------------------------------------------------------------------------------
// Cartoon FX
// (c) 2012-2020 Jean Moreno
//--------------------------------------------------------------------------------------------------------------------------------

Shader "Cartoon FX/Remaster/Particle Screen Distortion"
{
	Properties
	{ 
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

	//# ========================================================
	//# Texture
	//#
		[NoScaleOffset] _ScreenDistortionTex ("Distortion Texture", 2D) = "bump" {}
		_ScreenDistortionScale ("Distortion Scale", Range(-0.5, 0.5)) = 0.1
		
	//# ========================================================
	//# Debug
	//# 
		
		[Toggle(_DEBUG_VISUALIZE_DISTORTION)] _DebugVisualize ("Visualize Distortion Particles", Float) = 0 
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

		Blend SrcAlpha OneMinusSrcAlpha, One One
		ZWrite Off
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

				// Using the same keywords as Unity's Standard Particle shader to minimize project-wide keyword usage
				#pragma shader_feature_local _ _FADING_ON
				#pragma shader_feature_local _ _ALPHATEST_ON
				#pragma shader_feature_local _ _DEBUG_VISUALIZE_DISTORTION

				#define CFXR_URP
				#define CFXR_SCREEN_DISTORTION_SHADER
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

				// Using the same keywords as Unity's Standard Particle shader to minimize project-wide keyword usage
				#pragma shader_feature_local _ _FADING_ON
				#pragma shader_feature_local _ _ALPHATEST_ON
				#pragma shader_feature_local _ _DEBUG_VISUALIZE_DISTORTION

				#define CFXR_URP
				#define DISABLE_SOFT_PARTICLES
				#define CFXR_SCREEN_DISTORTION_SHADER
				#include "CFXR_PASSES.cginc"

				ENDCG
			}
		}

		//====================================================================================================================================
		// Built-in Rendering Pipeline

		SubShader
		{
			GrabPass
			{
				Tags { "LightMode" = "Always" }
				"_GrabTexture"
			}
			
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
				
				// Using the same keywords as Unity's Standard Particle shader to minimize project-wide keyword usage
				#pragma shader_feature_local _ _FADING_ON
				#pragma shader_feature_local _ _ALPHATEST_ON
				#pragma shader_feature_local _ _DEBUG_VISUALIZE_DISTORTION

				#include "UnityStandardParticleInstancing.cginc"

				#define CFXR_SCREEN_DISTORTION_SHADER
				#include "CFXR_PASSES.cginc"

				ENDCG
			}
		}
	}
	
	CustomEditor "CartoonFX.MaterialInspector"
}

