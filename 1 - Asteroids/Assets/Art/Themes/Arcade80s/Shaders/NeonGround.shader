// The ground of the Arcade 80s theme's strike world: a dark plane ruled with a neon grid. The layout of a tile comes
// from the same mask texture the ground shader uses (R paved, G liquid, B rock, A alt, signed distances mapped by
// _Ranges): paving turns the cells violet with a magenta rule around it, water is a deep pool with scan lines running
// across it and a bright shore, rock brightens the cells, fields tint them. The grid is drawn in tile metres.
Shader "Portfolio/Asteroids/NeonGround"
{
    Properties
    {
        [NoScaleOffset] _Masks ("Masks (R paved, G liquid, B rock, A alt)", 2D) = "black" {}
        _Ranges ("Mask Ranges (m): paved, liquid, rock, alt", Vector) = (1.5, 6, 3, 3)
        _TileSize ("Tile Size (m)", Vector) = (60, 20, 0, 0)
        _Cell ("Grid Cell (m)", Float) = 2.5
        _FillColor ("Ground", Color) = (0.015, 0.008, 0.05, 1)
        [HDR] _LineColor ("Grid", Color) = (0.08, 1.3, 1.6, 1)
        [HDR] _MajorColor ("Major Grid", Color) = (0.15, 2.2, 2.6, 1)
        _PavedColor ("Paved Ground", Color) = (0.12, 0.03, 0.2, 1)
        [HDR] _PavedLine ("Paved Rule", Color) = (2.6, 0.3, 2.2, 1)
        _LiquidColor ("Liquid", Color) = (0.0, 0.05, 0.16, 1)
        [HDR] _ShoreColor ("Shore", Color) = (0.2, 2.4, 2.8, 1)
        _RockColor ("Rock Cells", Color) = (0.1, 0.06, 0.2, 1)
        _AltColor ("Field Cells", Color) = (0.06, 0.02, 0.14, 1)
        _LineWidth ("Line Width (pixels)", Range(0.5, 4)) = 1.3
    }

    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_Masks);
            SAMPLER(sampler_Masks);

            CBUFFER_START(UnityPerMaterial)
                float4 _Ranges;
                float4 _TileSize;
                float _Cell;
                half4 _FillColor;
                half4 _LineColor;
                half4 _MajorColor;
                half4 _PavedColor;
                half4 _PavedLine;
                half4 _LiquidColor;
                half4 _ShoreColor;
                half4 _RockColor;
                half4 _AltColor;
                float _LineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            // Coverage of a mask field (positive inside), anti-aliased by the mask's screen derivative.
            float Cover(float mask, float range)
            {
                float inside = (mask - 0.5) * 2.0 * range;
                float width = max(fwidth(mask) * 2.0 * range, 0.02);
                return saturate(inside / width + 0.5);
            }

            // A line where the mask field crosses its edge, about _LineWidth pixels wide.
            float Outline(float mask, float range)
            {
                float inside = (mask - 0.5) * 2.0 * range;
                float width = max(fwidth(mask) * 2.0 * range, 0.02) * _LineWidth;
                return 1.0 - smoothstep(0.0, width, abs(inside));
            }

            float GridLine(float value)
            {
                float away = abs(frac(value) - 0.5);
                float aa = max(fwidth(value), 1e-5);
                return smoothstep(0.5 - aa * (_LineWidth + 1.0), 0.5 - aa * _LineWidth, away);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float4 masks = SAMPLE_TEXTURE2D(_Masks, sampler_Masks, input.uv);
                float paved = Cover(masks.r, _Ranges.x);
                float liquid = Cover(masks.g, _Ranges.y);
                float rock = Cover(masks.b, _Ranges.z);
                float alt = Cover(masks.a, _Ranges.w);

                float2 metres = input.uv * _TileSize.xy;
                float2 cells = metres / max(_Cell, 0.1);
                float minor = max(GridLine(cells.x), GridLine(cells.y));
                float major = max(GridLine(cells.x * 0.25), GridLine(cells.y * 0.25));

                half3 ground = _FillColor.rgb;
                ground = lerp(ground, _AltColor.rgb, alt);
                ground = lerp(ground, _RockColor.rgb, rock);
                // Rock cells carry a dot at their centre.
                float2 centre = abs(frac(cells) - 0.5);
                float dot = 1.0 - smoothstep(0.05, 0.12, max(centre.x, centre.y));
                ground += _LineColor.rgb * dot * rock * 0.5;
                ground = lerp(ground, _PavedColor.rgb, paved);

                half3 lines = lerp(_LineColor.rgb, _MajorColor.rgb, major) * max(minor, major);
                lines = lerp(lines, _PavedLine.rgb * max(minor, major) * 0.8, paved);
                half3 color = ground + lines * 0.55;
                color += _PavedLine.rgb * Outline(masks.r, _Ranges.x);

                // Water: a deep pool, scan lines drifting down it, a bright shore.
                float scan = 0.5 + 0.5 * sin((metres.y + _Time.y * 1.6) * 6.28318 / 1.25);
                half3 pool = _LiquidColor.rgb + _ShoreColor.rgb * pow(scan, 8.0) * 0.18;
                pool += _LineColor.rgb * GridLine(cells.x) * 0.2;
                color = lerp(color, pool, liquid);
                color += _ShoreColor.rgb * Outline(masks.g, _Ranges.y);

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
