// The space of the Arcade 80s theme, drawn on the far quad in place of the nebula: a night sky fading into a glow at
// the horizon, a striped low sun, a perspective grid rolling towards the viewer and a field of stars. It answers to
// the same properties as the SpaceBackground shader, which the sector themes set through SpaceBackdrop: _SpaceColor
// is the sky, _DustColor the horizon glow and the lower half of the sun, _HighlightColor the sun, _NebulaColor the
// grid lines and _StarDensity the stars. The quad stands 420 m behind the playfield; the horizon and the sun are
// placed in its world units, where the view spans about 330 m of height.
Shader "Portfolio/Asteroids/SynthwaveBackground"
{
    Properties
    {
        _NebulaTex ("Unused (kept for the backdrop)", 2D) = "black" {}
        _SpaceColor ("Sky", Color) = (0.02, 0.01, 0.08, 1)
        [HDR] _NebulaColor ("Grid", Color) = (0.1, 1.8, 2.2, 1)
        [HDR] _HighlightColor ("Sun", Color) = (2.5, 1.4, 0.3, 1)
        [HDR] _DustColor ("Horizon Glow", Color) = (0.9, 0.12, 0.7, 1)
        _StarDensity ("Stars", Range(0, 2)) = 1
        _Horizon ("Horizon (world y)", Float) = -60
        _SunCenter ("Sun (world x, y, radius)", Vector) = (0, -14, 44, 0)
        _GridSpeed ("Grid Speed", Float) = 0.35
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
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _NebulaTex_ST;
                half4 _SpaceColor;
                half4 _NebulaColor;
                half4 _HighlightColor;
                half4 _DustColor;
                float _StarDensity;
                float _Horizon;
                float4 _SunCenter;
                float _GridSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 world : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.world = positionWS.xy;
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
                float3 tint = lerp(float3(0.8, 0.9, 1.0), float3(1.0, 0.7, 0.95), frac(h * 7.3));
                return tint * brightness;
            }

            // A line where a repeating value crosses a whole number, widened by the screen derivative.
            float GridLine(float value, float width)
            {
                float away = abs(frac(value) - 0.5);
                float aa = max(fwidth(value), 1e-5);
                return smoothstep(0.5 - aa * (width + 1.0), 0.5 - aa * width, away);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float time = _Time.y;
                float2 p = input.world;
                float above = p.y - _Horizon;
                float3 sky = _SpaceColor.rgb;
                // The glow of the horizon: strongest at the horizon, gone 120 m above it.
                float glow = exp(-max(above, 0.0) / 45.0);
                sky = lerp(sky, _DustColor.rgb * 0.55, glow * 0.8);

                float3 color = sky;
                if (above >= 0.0)
                {
                    // Stars over the sky, thinning near the horizon.
                    float2 starPlane = p * 0.012;
                    float3 stars = Stars(starPlane + time * float2(0.0012, 0.003), 26.0, 0.42, 0.07, time) * 0.7;
                    stars += Stars(starPlane * 0.7 + 11.3, 13.0, 0.3, 0.06, time) * 1.3;
                    stars += Stars(starPlane * 0.45 + 5.7, 6.0, 0.18, 0.05, time) * 2.2;
                    color += stars * _StarDensity * saturate(above / 40.0);

                    // The sun: a disc with dark stripes across its lower half, from the sun colour above to the glow colour below.
                    float2 toSun = p - _SunCenter.xy;
                    float radius = max(_SunCenter.z, 1.0);
                    float d = length(toSun) / radius;
                    float aa = max(fwidth(d), 0.002);
                    float disc = 1.0 - smoothstep(1.0 - aa, 1.0 + aa, d);
                    float height = saturate(toSun.y / radius * 0.5 + 0.5);
                    float stripe = 1.0;
                    if (height < 0.55)
                    {
                        float bands = frac(height * 11.0 - time * 0.15);
                        float cut = lerp(0.65, 0.15, height / 0.55);
                        stripe = smoothstep(cut - 0.04, cut + 0.04, bands);
                    }
                    float3 sunColor = lerp(_DustColor.rgb * 1.6, _HighlightColor.rgb, height);
                    color = lerp(color, sunColor, disc * stripe);
                    // The haze around the sun.
                    color += _DustColor.rgb * exp(-d * d * 1.6) * 0.35 * (1.0 - disc);
                }
                else
                {
                    // The floor: a grid in perspective, rows racing towards the viewer, fading into the horizon glow.
                    float depth = 1.0 / max(-above, 0.5);
                    float rows = 28.0 * depth * 12.0 + time * _GridSpeed;
                    float columns = p.x * depth * 0.28;
                    // Lines closer together than a few pixels (near the horizon) fade out instead of melting into a band.
                    float lineRows = GridLine(rows, 1.2) * saturate(1.5 - fwidth(rows) * 4.0);
                    float lineColumns = GridLine(columns, 1.2) * saturate(1.5 - fwidth(columns) * 4.0);
                    float lines = max(lineRows, lineColumns);
                    float fade = saturate(-above / 30.0);
                    float3 floor = lerp(_DustColor.rgb * 0.28, _SpaceColor.rgb, saturate(-above / 120.0));
                    float3 grid = _NebulaColor.rgb * lines * fade;
                    color = floor + grid;
                    // The horizon line itself.
                    color += _NebulaColor.rgb * exp(-(-above) / 2.0) * 0.7;
                }
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
