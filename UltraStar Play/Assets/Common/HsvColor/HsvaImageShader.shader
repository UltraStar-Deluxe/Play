// NOTE: Changed the original implementation to set the HSV color instead of shifting it

/*
MIT License

Copyright 2015, Gregg Tavares.
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are
met:

    * Redistributions of source code must retain the above copyright
notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above
copyright notice, this list of conditions and the following disclaimer
in the documentation and/or other materials provided with the
distribution.
    * Neither the name of Gregg Tavares. nor the names of its
contributors may be used to endorse or promote products derived from
this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
Shader "Custom/HsvaImageShader"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _ColorMask("Color Mask", Float) = 15
    }
        SubShader
        {
            Tags
            {
                "RenderType" = "Transparent"
                "Queue" = "Transparent"
            }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest[unity_GUIZTestMode]
            Cull Off

            Stencil
            {
                Ref[_Stencil]
                Comp[_StencilComp]
                Pass[_StencilOp]
                ReadMask[_StencilReadMask]
                WriteMask[_StencilWriteMask]
            }
            ColorMask[_ColorMask]

            Pass
            {
                Cull Off
                ZWrite Off
                Blend SrcAlpha OneMinusSrcAlpha

                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile DUMMY PIXELSNAP_ON

                sampler2D _MainTex;

                struct Vertex
                {
                    float4 vertex : POSITION;
                    float2 uv_MainTex : TEXCOORD0;
                    // Color from the UI Image
                    float4 color    : COLOR;
                };

                struct Fragment
                {
                    float4 vertex : POSITION;
                    float2 uv_MainTex : TEXCOORD0;
                    float4 color    : COLOR;
                };

                Fragment vert(Vertex v)
                {
                    Fragment o;

                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv_MainTex = v.uv_MainTex;
                    o.color = v.color;

                    return o;
                }

                // See https://www.laurivan.com/rgb-to-hsv-to-rgb-for-shaders/
                float3 rgb2hsv(float3 c) {
                    // Values must be in range [0..1]
                    c = float3(clamp(c.rgb, 0.0, 1.0));

                    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

                    float d = q.x - min(q.w, q.y);
                    // Tiny delta to avoid divide-by-zero
                    float e = 1.0e-10;
                    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
                }

                float3 hsv2rgb(float3 c) {
                    // Values must be in range [0..1]
                    c = float3(clamp(c.xyz, 0.0, 1.0));

                    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                    float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                    return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
                }

                float4 frag(Fragment IN) : COLOR
                {
                    float4 texRgba = tex2D(_MainTex, IN.uv_MainTex);
                    // Combine colors in HSV world
                    float3 colorHsv = rgb2hsv(IN.color.rgb);
                    float3 texHsv = rgb2hsv(texRgba.rgb);
                    float3 resultHsv = float3(colorHsv.x, colorHsv.y * texHsv.y, colorHsv.z * texHsv.z);
                    // Convert back to RGB
                    float3 resultRgb = hsv2rgb(resultHsv);
                    float4 resultRgba = float4(resultRgb, texRgba.a * IN.color.a);
                    return resultRgba;
                }
                ENDCG
            }
        }
}