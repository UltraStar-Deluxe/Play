// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Hovl/Particles/Blend_TwoSides"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_MainTex("Main Tex", 2D) = "white" {}
		_Mask("Mask", 2D) = "white" {}
		_Noise("Noise", 2D) = "white" {}
		_SpeedMainTexUVNoiseZW("Speed MainTex U/V + Noise Z/W", Vector) = (0,0,0,0)
		_Emission("Emission", Float) = 2
		[Toggle]_UseFresnel("Use Fresnel?", Float) = 1
		[Toggle]_Usesmoothcorners("Use smooth corners?", Float) = 0
		_Fresnel("Fresnel", Float) = 1
		_FresnelEmission("Fresnel Emission", Float) = 1
		[Toggle]_SeparateFresnel("SeparateFresnel", Float) = 0
		_SeparateEmission("Separate Emission", Float) = 2
		_FresnelColor("Fresnel Color", Color) = (0.3568628,0.08627451,0.08627451,1)
		_FrontFacesColor("Front Faces Color", Color) = (0,0.2313726,1,1)
		_BackFacesColor("Back Faces Color", Color) = (0,0.02397324,0.509434,1)
		_BackFresnelColor("Back Fresnel Color", Color) = (0.3568628,0.08627451,0.08627451,1)
		[Toggle]_UseBackFresnel("Use Back Fresnel?", Float) = 1
		_BackFresnel("Back Fresnel", Float) = -2
		_BackFresnelEmission("Back Fresnel Emission", Float) = 1
		[Toggle]_UseCustomData("Use Custom Data?", Float) = 0
		[Toggle]_Sideopacity("Side opacity", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "Transparent+0" "IsEmissive" = "true"  "PreviewType"="Plane" }
		Cull Off
		Blend SrcAlpha OneMinusSrcAlpha
		
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Unlit keepalpha noshadow 
		#undef TRANSFORM_TEX
		#define TRANSFORM_TEX(tex,name) float4(tex.xy * name##_ST.xy + name##_ST.zw, tex.z, tex.w)
		struct Input
		{
			float3 worldPos;
			float3 worldNormal;
			float4 uv_texcoord;
			float3 viewDir;
			float4 vertexColor : COLOR;
		};

		uniform float _SeparateFresnel;
		uniform float _UseFresnel;
		uniform float4 _FrontFacesColor;
		uniform float _Fresnel;
		uniform float _Usesmoothcorners;
		uniform sampler2D _Mask;
		uniform float4 _Mask_ST;
		uniform sampler2D _Noise;
		uniform float4 _Noise_ST;
		uniform float4 _SpeedMainTexUVNoiseZW;
		uniform float _UseCustomData;
		uniform float _FresnelEmission;
		uniform float4 _FresnelColor;
		uniform float4 _BackFacesColor;
		uniform float _UseBackFresnel;
		uniform float _BackFresnel;
		uniform float _BackFresnelEmission;
		uniform float4 _BackFresnelColor;
		uniform float _Emission;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform float _SeparateEmission;
		uniform float _Sideopacity;
		uniform float _Cutoff = 0.5;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float3 ase_worldNormal = i.worldNormal;
			float fresnelNdotV95 = dot( ase_worldNormal, ase_worldViewDir );
			float fresnelNode95 = ( 0.0 + 1.0 * pow( 1.0 - fresnelNdotV95, _Fresnel ) );
			float2 uv_Mask = i.uv_texcoord * _Mask_ST.xy + _Mask_ST.zw;
			float4 uvs_Noise = i.uv_texcoord;
			uvs_Noise.xy = i.uv_texcoord.xy * _Noise_ST.xy + _Noise_ST.zw;
			float2 appendResult22 = (float2(_SpeedMainTexUVNoiseZW.z , _SpeedMainTexUVNoiseZW.w));
			float4 temp_output_70_0 = ( tex2D( _Mask, uv_Mask ) * tex2D( _Noise, ( (uvs_Noise).xy + ( _Time.y * appendResult22 ) + uvs_Noise.w ) ) * (( _UseCustomData )?( uvs_Noise.z ):( 1.0 )) );
			float4 Noise156 = saturate( (float4(-1,-1,-1,-1) + (temp_output_70_0 - float4( 0,0,0,0 )) * (float4(2,2,2,2) - float4(-1,-1,-1,-1)) / (float4( 1,1,1,1 ) - float4( 0,0,0,0 ))) );
			float4 temp_cast_2 = (fresnelNode95).xxxx;
			float fresnelNdotV146 = dot( ase_worldNormal, ase_worldViewDir );
			float fresnelNode146 = ( 0.0 + 1.0 * pow( 1.0 - fresnelNdotV146, _BackFresnel ) );
			float dotResult87 = dot( ase_worldNormal , i.viewDir );
			float4 lerpResult91 = lerp( (( _UseFresnel )?( ( ( _FrontFacesColor * ( 1.0 - fresnelNode95 ) * (( _Usesmoothcorners )?( Noise156 ):( float4( 1,1,1,1 ) )) ) + ( _FresnelEmission * _FresnelColor * (( _Usesmoothcorners )?( saturate( ( fresnelNode95 + ( 1.0 - Noise156 ) ) ) ):( temp_cast_2 )) ) ) ):( _FrontFacesColor )) , (( _Usesmoothcorners )?( (( _UseBackFresnel )?( ( ( _BackFacesColor * ( 1.0 - fresnelNode146 ) * Noise156 ) + ( _BackFresnelEmission * _BackFresnelColor * saturate( ( fresnelNode146 + ( 1.0 - Noise156 ) ) ) ) ) ):( _BackFacesColor )) ):( _BackFacesColor )) , (1.0 + (sign( dotResult87 ) - -1.0) * (0.0 - 1.0) / (1.0 - -1.0)));
			float4 uvs_MainTex = i.uv_texcoord;
			uvs_MainTex.xy = i.uv_texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;
			float2 appendResult21 = (float2(_SpeedMainTexUVNoiseZW.x , _SpeedMainTexUVNoiseZW.y));
			float4 tex2DNode105 = tex2D( _MainTex, ( uvs_MainTex.xy + ( appendResult21 * _Time.y ) ) );
			o.Emission = (( _SeparateFresnel )?( ( ( lerpResult91 + ( _FresnelColor * tex2DNode105 * _SeparateEmission ) ) * _Emission * i.vertexColor ) ):( ( lerpResult91 * _Emission * i.vertexColor * tex2DNode105 ) )).rgb;
			float4 temp_cast_4 = (i.vertexColor.a).xxxx;
			float4 temp_cast_5 = (1.0).xxxx;
			o.Alpha = (( _Sideopacity )?( ( i.vertexColor.a * saturate( ( ( temp_output_70_0 - temp_cast_5 ) + (( _UseCustomData )?( uvs_Noise.z ):( 1.0 )) ) ) ) ):( temp_cast_4 )).r;
			clip( temp_output_70_0.r - _Cutoff );
		}

		ENDCG
	}
}
/*ASEBEGIN
Version=18933
0;0;1920;1019;3842.416;1109.631;3.132044;True;False
Node;AmplifyShaderEditor.Vector4Node;15;-1416.635,615.4911;Float;False;Property;_SpeedMainTexUVNoiseZW;Speed MainTex U/V + Noise Z/W;4;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TimeNode;17;-1085.113,644.0539;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;29;-842.2249,645.2516;Inherit;False;0;14;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;22;-1052.689,775.7031;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode;59;-589.8,683.1187;Inherit;False;True;True;False;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;23;-836.114,821.5425;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;104;-265.9941,869.9862;Float;False;Constant;_Float0;Float 0;13;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;27;-264.7057,745.7369;Inherit;False;3;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;14;-117.4146,709.8865;Inherit;True;Property;_Noise;Noise;3;0;Create;True;0;0;0;False;0;False;-1;3584f2bf4afb5284d91edb6a29126e62;3584f2bf4afb5284d91edb6a29126e62;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ToggleSwitchNode;130;-98.24423,989.1903;Float;False;Property;_UseCustomData;Use Custom Data?;19;0;Create;True;0;0;0;False;0;False;0;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;13;-117.0276,524.4487;Inherit;True;Property;_Mask;Mask;2;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node;185;323.0389,838.1842;Inherit;False;Constant;_Vector1;Vector 1;19;0;Create;True;0;0;0;False;0;False;-1,-1,-1,-1;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;70;333.5393,643.4429;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.Vector4Node;183;327.1047,1004.301;Inherit;False;Constant;_Vector0;Vector 0;19;0;Create;True;0;0;0;False;0;False;2,2,2,2;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCRemapNode;182;575.0549,783.0135;Inherit;True;5;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;1,1,1,1;False;3;COLOR;0,0,0,0;False;4;COLOR;1,1,1,1;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;181;884.5331,774.2887;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;156;1042.584,770.8239;Inherit;False;Noise;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;96;-2482.361,-1158.667;Float;False;Property;_Fresnel;Fresnel;8;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;157;-2428.146,-981.2784;Inherit;False;156;Noise;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;164;-2127.536,-307.3619;Inherit;False;156;Noise;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;145;-2227.076,-478.5881;Float;False;Property;_BackFresnel;Back Fresnel;17;0;Create;True;0;0;0;False;0;False;-2;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;159;-2247.889,-1090.836;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;174;-1965.128,-408.0617;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FresnelNode;95;-2327.869,-1331.04;Inherit;False;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode;146;-2046.818,-576.0375;Inherit;False;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;173;-1777.637,-571.7398;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;162;-2064.735,-1248.1;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;152;-1804.272,-845.4792;Float;False;Property;_BackFresnelEmission;Back Fresnel Emission;18;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;186;-1793.878,-757.5932;Float;False;Property;_BackFresnelColor;Back Fresnel Color;15;0;Create;True;0;0;0;False;0;False;0.3568628,0.08627451,0.08627451,1;0.5,0.5,0.5,1;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WireNode;177;-1379.361,-336.8337;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;163;-1926.881,-1234.441;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;92;-1499.365,-880.7869;Float;False;Property;_BackFacesColor;Back Faces Color;14;0;Create;True;0;0;0;False;0;False;0,0.02397324,0.509434,1;0.5,0.5,0.5,1;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;148;-1745.033,-475.8595;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;167;-1648.358,-571.1368;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;150;-1486.227,-653.3383;Inherit;False;3;3;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;126;-1917.39,-1163.28;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;85;-834.8215,-91.70955;Float;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;149;-1245.457,-727.8528;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;93;-1781.583,-1498.553;Float;False;Property;_FresnelColor;Fresnel Color;12;0;Create;True;0;0;0;False;0;False;0.3568628,0.08627451,0.08627451,1;0.5,0.5,0.5,1;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;31;-1536.367,-1610.658;Float;False;Property;_FrontFacesColor;Front Faces Color;13;0;Create;True;0;0;0;False;0;False;0,0.2313726,1,1;0.5,0.5,0.5,1;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ToggleSwitchNode;189;-1775.258,-1326.313;Float;False;Property;_Usesmoothcorners;Use smooth corners?;7;0;Create;True;0;0;0;False;0;False;0;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DynamicAppendNode;21;-1055.23,553.4234;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ToggleSwitchNode;188;-1588.763,-1155.405;Float;False;Property;_Usesmoothcorners;Use smooth corners?;7;0;Create;True;0;0;0;False;0;False;0;True;2;0;COLOR;1,1,1,1;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.WorldNormalVector;86;-843.9027,-243.9981;Inherit;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;98;-1787.089,-1581.61;Float;False;Property;_FresnelEmission;Fresnel Emission;9;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;151;-1083.601,-667.8624;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;106;-612.1661,450.9655;Inherit;False;0;105;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;97;-1504.551,-1440.158;Inherit;False;3;3;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DotProductOpNode;87;-585.6503,-148.8772;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;24;-838.5569,554.7645;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;127;-1282.876,-1502.273;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SignOpNode;88;-418.097,-137.2645;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;153;-864.8242,-683.6946;Float;False;Property;_UseBackFresnel;Use Back Fresnel?;16;0;Create;True;0;0;0;False;0;False;1;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;193;113.2455,436.0645;Inherit;False;Constant;_Float1;Float 1;23;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;123;-1123.542,-1455.929;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;26;-312.7807,532.4018;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WireNode;143;-1100.856,-39.73738;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TFHCRemapNode;89;-255.5934,-155.4252;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;-1;False;2;FLOAT;1;False;3;FLOAT;1;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;158;-358.488,105.6721;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;142;-94.93386,351.3196;Float;False;Property;_SeparateEmission;Separate Emission;11;0;Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;187;-614.2866,-830.5384;Float;False;Property;_Usesmoothcorners;Use smooth corners?;7;0;Create;True;0;0;0;False;0;False;0;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;191;321.6437,455.2592;Inherit;False;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;105;-176.5121,163.5146;Inherit;True;Property;_MainTex;Main Tex;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ToggleSwitchNode;133;-753.2562,-1551.58;Float;False;Property;_UseFresnel;Use Fresnel?;6;0;Create;True;0;0;0;False;0;False;1;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;141;252.8173,97.90491;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;91;-25.98829,-276.804;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;192;497.1369,458.0011;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;194;646.5795,463.4853;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;136;411.426,-123.3316;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;52;-16.89706,-122.9107;Float;False;Property;_Emission;Emission;5;0;Create;True;0;0;0;False;0;False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;32;-46.08871,-45.01809;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;140;566.1781,-39.63671;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;51;274.6301,239.1062;Inherit;False;4;4;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;195;789.2271,419.2154;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ToggleSwitchNode;134;765.065,222.8378;Float;False;Property;_SeparateFresnel;SeparateFresnel;10;0;Create;True;0;0;0;False;0;False;0;True;2;0;COLOR;0,0,0,0;False;1;COLOR;1,1,1,1;False;1;COLOR;0
Node;AmplifyShaderEditor.ToggleSwitchNode;190;965.2324,385.8944;Inherit;False;Property;_Sideopacity;Side opacity;20;0;Create;True;0;0;0;False;0;False;0;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;129;1190.268,240.309;Float;False;True;-1;2;;0;0;Unlit;Hovl/Particles/Blend_TwoSides;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Off;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0.5;True;False;0;True;TransparentCutout;;Transparent;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;0;-1;-1;-1;1;PreviewType=Plane;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;22;0;15;3
WireConnection;22;1;15;4
WireConnection;59;0;29;0
WireConnection;23;0;17;2
WireConnection;23;1;22;0
WireConnection;27;0;59;0
WireConnection;27;1;23;0
WireConnection;27;2;29;4
WireConnection;14;1;27;0
WireConnection;130;0;104;0
WireConnection;130;1;29;3
WireConnection;70;0;13;0
WireConnection;70;1;14;0
WireConnection;70;2;130;0
WireConnection;182;0;70;0
WireConnection;182;3;185;0
WireConnection;182;4;183;0
WireConnection;181;0;182;0
WireConnection;156;0;181;0
WireConnection;159;0;157;0
WireConnection;174;0;164;0
WireConnection;95;3;96;0
WireConnection;146;3;145;0
WireConnection;173;0;146;0
WireConnection;173;1;174;0
WireConnection;162;0;95;0
WireConnection;162;1;159;0
WireConnection;177;0;164;0
WireConnection;163;0;162;0
WireConnection;148;0;146;0
WireConnection;167;0;173;0
WireConnection;150;0;152;0
WireConnection;150;1;186;0
WireConnection;150;2;167;0
WireConnection;126;0;95;0
WireConnection;149;0;92;0
WireConnection;149;1;148;0
WireConnection;149;2;177;0
WireConnection;189;0;95;0
WireConnection;189;1;163;0
WireConnection;21;0;15;1
WireConnection;21;1;15;2
WireConnection;188;1;157;0
WireConnection;151;0;149;0
WireConnection;151;1;150;0
WireConnection;97;0;98;0
WireConnection;97;1;93;0
WireConnection;97;2;189;0
WireConnection;87;0;86;0
WireConnection;87;1;85;0
WireConnection;24;0;21;0
WireConnection;24;1;17;2
WireConnection;127;0;31;0
WireConnection;127;1;126;0
WireConnection;127;2;188;0
WireConnection;88;0;87;0
WireConnection;153;0;92;0
WireConnection;153;1;151;0
WireConnection;123;0;127;0
WireConnection;123;1;97;0
WireConnection;26;0;106;0
WireConnection;26;1;24;0
WireConnection;143;0;93;0
WireConnection;89;0;88;0
WireConnection;158;0;143;0
WireConnection;187;0;92;0
WireConnection;187;1;153;0
WireConnection;191;0;70;0
WireConnection;191;1;193;0
WireConnection;105;1;26;0
WireConnection;133;0;31;0
WireConnection;133;1;123;0
WireConnection;141;0;158;0
WireConnection;141;1;105;0
WireConnection;141;2;142;0
WireConnection;91;0;133;0
WireConnection;91;1;187;0
WireConnection;91;2;89;0
WireConnection;192;0;191;0
WireConnection;192;1;130;0
WireConnection;194;0;192;0
WireConnection;136;0;91;0
WireConnection;136;1;141;0
WireConnection;140;0;136;0
WireConnection;140;1;52;0
WireConnection;140;2;32;0
WireConnection;51;0;91;0
WireConnection;51;1;52;0
WireConnection;51;2;32;0
WireConnection;51;3;105;0
WireConnection;195;0;32;4
WireConnection;195;1;194;0
WireConnection;134;0;51;0
WireConnection;134;1;140;0
WireConnection;190;0;32;4
WireConnection;190;1;195;0
WireConnection;129;2;134;0
WireConnection;129;9;190;0
WireConnection;129;10;70;0
ASEEND*/
//CHKSM=595482D8985B5AE96CA2254E8CDD809861312A0C