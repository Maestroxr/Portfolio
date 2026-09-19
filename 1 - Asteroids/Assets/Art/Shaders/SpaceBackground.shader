// Deep space behind the Asteroids playfield: two slowly drifting samples of a nebula noise texture coloured per
// sector (clouds, bright filaments, dark dust lanes) and three layers of procedural, anti-aliased, twinkling stars.
// SpaceBackdrop sets the colours per sector through a property block.
Shader "Portfolio/Asteroids/SpaceBackground"
{
    Properties
    {
        _NebulaTex ("Nebula (R clouds, G filaments, B dust, A detail)", 2D) = "black" {}
        _SpaceColor ("Space", Color) = (0.01, 0.012, 0.03, 1)
        [HDR] _NebulaColor ("Nebula", Color) = (0.1, 0.3, 0.6, 1)
        [HDR] _HighlightColor ("Highlight", Color) = (0.4, 0.9, 1, 1)
        [HDR] _DustColor ("Dust", Color) = (0.05, 0.02, 0.08, 1)
        _StarDensity ("Stars", Range(0, 2)) = 1
        _Scroll ("Drift (xy near layer, zw far layer)", Vector) = (0.0015, 0.004, 0.0008, 0.002)
        _Tiling ("Nebula Tiling", Float) = 0.45
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Unlit"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_NebulaTex);
            SAMPLER(sampler_NebulaTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _NebulaTex_ST;
                half4 _SpaceColor;
                half4 _NebulaColor;
                half4 _HighlightColor;
                half4 _DustColor;
                float _StarDensity;
                float4 _Scroll;
                float _Tiling;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 plane : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                // World units on the far plane, divided so that the whole view spans a few units.
                output.plane = positionWS.xy * 0.01;
                return output;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(233.34, 851.73));
                p += dot(p, p + 23.45);
                return frac(p.x * p.y);
            }

            float2 Hash22(float2 p)
            {
                float h = Hash21(p);
                return float2(h, Hash21(p + h * 17.0));
            }

            // One layer of stars: at most one star per cell, a share of cells lit, anti-aliased by the pixel size.
            float3 Stars(float2 p, float cells, float lit, float size, float time)
            {
                float2 grid = p * cells;
                float2 id = floor(grid);
                float2 local = frac(grid) - 0.5;
                float h = Hash21(id);
                float on = step(1.0 - lit, h);
                float2 offset = (Hash22(id + 3.1) - 0.5) * 0.6;
                float d = length(local - offset);
                float aa = max(fwidth(d), 0.0001);
                float star = 1.0 - smoothstep(size - aa, size + aa, d);
                float halo = exp(-d * d / (size * size * 9.0)) * 0.45;
                float twinkle = 0.7 + 0.3 * sin(time * (1.3 + h * 3.7) + h * 40.0);
                float brightness = (star + halo) * twinkle * on * (0.35 + 0.65 * frac(h * 13.7));
                float warm = frac(h * 7.3);
                float3 tint = lerp(float3(0.75, 0.85, 1.0), float3(1.0, 0.86, 0.72), warm);
                return tint * brightness;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float time = _Time.y;
                float2 uv = input.plane * _Tiling;
                float4 nearLayer = SAMPLE_TEXTURE2D(_NebulaTex, sampler_NebulaTex, uv + time * _Scroll.xy);
                float4 farLayer = SAMPLE_TEXTURE2D(_NebulaTex, sampler_NebulaTex, uv * 0.55 + float2(0.37, 0.61) + time * _Scroll.zw);

                float clouds = nearLayer.r * 0.7 + farLayer.r * 0.5;
                float filaments = saturate(nearLayer.g * (0.4 + farLayer.r));
                float dust = saturate(nearLayer.b * 1.3 - 0.25) * saturate(farLayer.a + 0.4);

                float3 color = _SpaceColor.rgb;
                color += _NebulaColor.rgb * clouds * clouds * 1.4;
                color += _HighlightColor.rgb * pow(filaments, 1.8) * (0.4 + nearLayer.a);
                color = lerp(color, _DustColor.rgb, dust * 0.7);

                float2 starPlane = input.plane * 3.0;
                float3 stars = Stars(starPlane + time * float2(0.0012, 0.003), 26.0, 0.45, 0.07, time) * 0.7;
                stars += Stars(starPlane * 0.7 + 11.3 + time * float2(0.0024, 0.006), 13.0, 0.3, 0.06, time) * 1.3;
                stars += Stars(starPlane * 0.45 + 5.7 + time * float2(0.0004, 0.001), 6.0, 0.18, 0.05, time) * 2.4;
                color += stars * _StarDensity * (1.0 - dust * 0.85);

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
