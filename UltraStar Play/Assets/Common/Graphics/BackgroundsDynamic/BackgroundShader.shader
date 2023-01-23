Shader "UltraStar Play/Background Shader"
{
    Properties
    {
        [NoScaleOffset][HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        [NoScaleOffset][HideInInspector] _UiTex ("UI Texture", 2D) = "white" {}
        [NoScaleOffset][HideInInspector] _TransitionTex ("Transition Texture", 2D) = "white" {}
        [NoScaleOffset] _ColorRampTex ("Gradient Ramp", 2D) = "gray" {}
        _ColorRampScrolling ("Gradient Scrolling", Float) = 0
        [Space]
        [Header(Pattern)]
        [Space]
        _PatternTex ("Pattern Texture", 2D) = "white" {}
        _PatternColor ("Pattern Color", Color) = (1,1,1,1)
        [Header(Gradient)]
        [Space]
        [Enum(Radial,0,Linear,1,Reflected,2,Repeated,3,Radial Repeated,4)] _Gradient ("Gradient Type", Float) = 0
        _Scale ("Gradient Scale", Range(0.1,10)) = 1
        _Smoothness ("Gradient Smoothness", Float) = 1
        _Angle ("Gradient Angle", Range(0,360)) = 0
        [ToggleUI] _EnableGradientAnimation ("Enable Gradient Sine Animation", Float) = 0
        _GradientAnimSpeed ("Animation Speed", Float) = 1
        _GradientAnimAmp ("Animation Amplitude", Float) = 0.1
        [Toggle(_DITHERING)] _EnableDithering ("Enable Gradient Dithering", Float) = 0
        [NoScaleOffset] _NoiseTex ("Dithering Noise", 2D) = "gray" {}
        [Header(UI Effects)]
        [Space]
        _UiShadowOpacity ("Shadow Opacity", Float) = 0
        _UiShadowOffset ("Shadow Offset", Vector) = (0,0,0,0)
        [Header(Animation)]
        [Space]
        _TransitionTime ("Transition Time", Range(0,1)) = 0
    }
    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #pragma multi_compile_fragment _ _DITHERING
            #pragma multi_compile_fragment _ _UI_SHADOW
            #pragma multi_compile_fragment _ _UI_TRANSITION_ANIM

            half _Gradient;
            half _EnableGradientAnimation;

            sampler2D _MainTex;
            sampler2D _UiTex;
            sampler2D _TransitionTex;
            sampler2D _ColorRampTex;
            sampler2D _PatternTex;
            sampler2D _NoiseTex;
            half4 _NoiseTex_TexelSize;
            half4 _PatternTex_ST;
            half4 _PatternTex_TexelSize;
            half _ColorRampScrolling;
            half _GradientAnimSpeed;
            half _GradientAnimAmp;
            half _Scale;
            half _Smoothness;
            half _Angle;
            half4 _PatternColor;
            half _UiShadowOpacity;
            float4 _UiShadowOffset;
            float _TransitionTime;

            float _TimeApplication;

            // ----------------------------------------------------------------
            // The MIT License
            // https://www.youtube.com/c/InigoQuilez
            // https://iquilezles.org/
            // Copyright © 2015 Inigo Quilez
            // Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
            half3 proceduralPalette(in float t, in half3 a, in half3 b, in half3 c, in half3 d)
            {
                return a + b * cos(UNITY_PI* 2.0 * (c * t + d));
            }
            // ----------------------------------------------------------------

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 texcoord0 : TEXCOORD0;
            };

            struct Varyings
            {
                float4 texcoord0 : TEXCOORD0;
                float2 patternTexcoord : TEXCOORD2;
                float4 screenCoord : TEXCOORD1;
                float4 positionCS : SV_POSITION;
            };

            Varyings vert (Attributes input)
            {
                Varyings output;
                UNITY_INITIALIZE_OUTPUT(Varyings, output)

                output.positionCS = UnityObjectToClipPos(input.positionOS);
                output.texcoord0.xy = input.texcoord0.xy;
                output.texcoord0.zw = input.texcoord0.xy * _Scale - (_Scale - 1.0) / 2.0;
                float2 patternRatio = float2(_PatternTex_TexelSize.x / _PatternTex_TexelSize.y, 1);
                output.patternTexcoord.xy = input.texcoord0.xy * _PatternTex_ST.xy * patternRatio + frac(_TimeApplication.xx * _PatternTex_ST.zw);
                output.screenCoord = ComputeScreenPos(output.positionCS);

                #if !defined(_GRADIENT_RADIAL)
                    half rad = _Angle * (UNITY_PI * 2) / 360;
                    float s = sin(rad);
                    float c = cos(rad);
                    const float2x2 rotationMatrix = float2x2(c, -s, s, c);
                    output.texcoord0.zw = mul(rotationMatrix, output.texcoord0.zw - 0.5) + 0.5;
                #endif

                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                // Sine animation
                if (_EnableGradientAnimation > 0)
                {
                    half time = _TimeApplication * _GradientAnimSpeed;
                    input.texcoord0.z += sin(input.texcoord0.w * 3.2 + time * 0.2)
                            * sin(input.texcoord0.w * 0.7 + time * 0.7)
                            * sin(input.texcoord0.w * 2.1 + time * 0.5)
                            * _GradientAnimAmp;
                }

                half gradient;
                switch (_Gradient)
                {
                    // Radial
                    default:
                    case 0:
                    {
                        half2 offsetUv = (input.texcoord0.zw - 0.5);
                        // offsetUv.x *= _ScreenParams.x / _ScreenParams.y;
                        gradient = saturate(dot(offsetUv, offsetUv));
                        break;
                    }

                    // Radial Repeated
                    case 1:
                    {
                        half2 offsetUv = (input.texcoord0.zw - 0.5);
                        // offsetUv.x *= _ScreenParams.x / _ScreenParams.y;
                        gradient = abs(frac(dot(offsetUv, offsetUv)) * 2.0 - 1.0);
                        break;
                    }


                    // Linear
                    case 2:
                        gradient = input.texcoord0.zw;
                        break;

                    // Reflected
                    case 3:
                        gradient = abs(input.texcoord0.zw * 2.0 - 1.0);
                        break;

                    // Repeated
                    case 4:
                        gradient = abs(frac(input.texcoord0.zw) * 2.0 - 1.0);
                        break;
                }

                half smooth = _Smoothness / 2.0;
                if (_Smoothness <= 0)
                {
                    // make sure the line is antialiased if 0
                    smooth = fwidth(input.texcoord0.x) * 10.0;
                }
                gradient = smoothstep(0.5 - smooth, 0.5 + smooth, gradient);

                // If not radial (radial looks better in gamma empirically)
                if (_Gradient > 1)
                {
                    gradient = GammaToLinearSpaceExact(gradient);
                }
                gradient = smoothstep(0.0, 1.0, 1.0 - gradient);

                #ifdef _DITHERING
                    // Dithering
                    float2 screenCoords = input.screenCoord.xy / input.screenCoord.w * _ScreenParams.xy * _NoiseTex_TexelSize.xy;
                    half ditheringNoise = tex2D(_NoiseTex, screenCoords).r * 2.0 - 1.0;
                    gradient += ditheringNoise * 0.03;
                #endif

                // Gather the grayscale version of the background scene with particles, etc.
                half sceneGrayscale = tex2D(_MainTex, input.texcoord0.xy).r;
                // and remap colors using the selected gradient texture
                half coords =  lerp(gradient, 1 - gradient, sceneGrayscale);
                half3 color = tex2Dgrad(_ColorRampTex, coords.xx + frac(_TimeApplication.xx * _ColorRampScrolling), 0, 0).rgb;

                // Pattern
                half4 pattern = tex2D(_PatternTex, input.patternTexcoord.xy) * _PatternColor;
                color.rgb = lerp(color.rgb, pattern.rgb, pattern.a);

                half4 uiColors = half4(0, 0, 0, 0);

                #if defined(_UI_SHADOW)
                    // UIToolkit shadow
                    float2 shadowOffset = _UiShadowOffset / _ScreenParams.xy;
                    half4 uiShadow = tex2D(_UiTex, input.texcoord0.xy + shadowOffset);
                    uiColors.a += uiShadow.a * _UiShadowOpacity;
                #endif

                #if !defined(_UI_TRANSITION_ANIM)
                    // Blend UIToolkit interface
                    half4 uiTexture = tex2D(_UiTex, input.texcoord0.xy);
                    uiColors = saturate(uiColors + uiTexture);
                    color.rgb = uiColors.rgb + color.rgb * (1.0 - uiColors.a);
                #else
                    // Transition animations
                    float4 transitionCoords = input.texcoord0.xyxy - 0.5;
                    transitionCoords.xy *= lerp(1.25, 1.0, _TransitionTime).xx;
                    transitionCoords.zw *= lerp(1.0, 0.4, _TransitionTime).xx;
                    transitionCoords += 0.5;

                    // Transition the new screen in by scaling it up (1.0/1.25 -> 1.0) and fading it in
                    half4 uiTexture = tex2D(_UiTex, transitionCoords.xy);
                    uiColors = saturate(uiColors + uiTexture);
                    color.rgb = uiColors.rgb * _TransitionTime + color.rgb * (1.0 - uiColors.a * _TransitionTime);

                    // Transition the old screen out by scaling it up (1.0 -> 1.0/0.4) and fading it out
                    half4 transitionTexture = tex2D(_TransitionTex, transitionCoords.zw);
                    #if defined(_UI_SHADOW)
                        // Reapply the shadow to the fading out UI
                        half4 uiShadowTransition = tex2D(_TransitionTex, transitionCoords.zw + shadowOffset);
                        transitionTexture.a += saturate(uiShadowTransition.a - transitionTexture.a) * _UiShadowOpacity;
                    #endif
                    float timeFade = (1.0 - _TransitionTime);
                    color.rgb = transitionTexture.rgb * timeFade + color.rgb * (1.0 - transitionTexture.a * timeFade);
                #endif

                return half4(color, 1.0);
            }
            ENDCG
        }
    }
}