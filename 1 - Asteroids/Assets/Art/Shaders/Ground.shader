// Ground of the strike mode: one material per terrain tile. The tile's mask texture holds four signed distance fields
// (0.5 on an edge, more inside): R paved, G liquid, B rock, A alt ground; its colour map holds the tile's large-scale
// tint and the baked shadows of its props (RGB, x2) and which paving a paved spot uses (A: 0 asphalt, 1 concrete).
// Six tiling detail layers of the theme (RGB colour with the lighting of the default sun baked in, A height) are laid
// over each other along the fields: the height roughens every edge, so a low-resolution mask still gives crisp,
// organic edges at any zoom. Detail coordinates are the tile's own metres, and every repeat divides 20 m, so tiles of
// a theme join without a seam. Liquid is water with depth, a shore line and glints, or glowing lava.
Shader "Portfolio/Asteroids/Ground"
{
    Properties
    {
        [NoScaleOffset] _Masks ("Masks (R paved, G liquid, B rock, A alt)", 2D) = "black" {}
        [NoScaleOffset] _ColorMap ("Colour Map (RGB tint x2, A concrete)", 2D) = "gray" {}
        [NoScaleOffset] _BaseTex ("Base Layer (RGB, A height)", 2D) = "gray" {}
        [NoScaleOffset] _AltTex ("Alt Layer", 2D) = "gray" {}
        [NoScaleOffset] _RockTex ("Rock Layer", 2D) = "gray" {}
        [NoScaleOffset] _RoadTex ("Asphalt Layer", 2D) = "gray" {}
        [NoScaleOffset] _PavedTex ("Concrete Layer", 2D) = "gray" {}
        [NoScaleOffset] _LiquidTex ("Liquid Pattern (A)", 2D) = "gray" {}
        _ShallowColor ("Shallow Liquid", Color) = (0.2, 0.5, 0.55, 1)
        _DeepColor ("Deep Liquid", Color) = (0.03, 0.14, 0.24, 1)
        _FoamColor ("Shore Line (alpha = amount)", Color) = (0.85, 0.9, 0.85, 0.6)
        [HDR] _LiquidEmission ("Liquid Glow (lava)", Color) = (0, 0, 0, 1)
        [HDR] _CrackEmission ("Rock Crack Glow", Color) = (0, 0, 0, 1)
        _Ranges ("Mask Ranges (m): paved, liquid, rock, alt", Vector) = (1.5, 6, 3, 3)
        _Edges ("Edge Roughness (m): paved, liquid, rock, alt", Vector) = (0.06, 0.25, 0.7, 0.6)
        _DetailSize ("Detail Repeat (m, divides 20)", Float) = 5
        _Waves ("Liquid Motion", Float) = 1
        _Gloss ("Liquid Glints", Float) = 0.6
    }

    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _ShallowColor;
            half4 _DeepColor;
            half4 _FoamColor;
            half4 _LiquidEmission;
            half4 _CrackEmission;
            float4 _Ranges;
            float4 _Edges;
            float _DetailSize;
            float _Waves;
            float _Gloss;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_Masks); SAMPLER(sampler_Masks);
            TEXTURE2D(_ColorMap); SAMPLER(sampler_ColorMap);
            TEXTURE2D(_BaseTex); SAMPLER(sampler_BaseTex);
            TEXTURE2D(_AltTex); SAMPLER(sampler_AltTex);
            TEXTURE2D(_RockTex); SAMPLER(sampler_RockTex);
            TEXTURE2D(_RoadTex); SAMPLER(sampler_RoadTex);
            TEXTURE2D(_PavedTex); SAMPLER(sampler_PavedTex);
            TEXTURE2D(_LiquidTex); SAMPLER(sampler_LiquidTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 meters : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.meters = input.positionOS.xy;
                return output;
            }

            // How much of a layer covers this spot: its field in metres (positive inside), roughened by the layer's
            // height, with an edge one pixel wide.
            float Cover(float mask, float height, float range, float roughness)
            {
                float inside = (mask - 0.5) * 2.0 * range;
                float width = max(fwidth(mask) * 2.0 * range, 0.015);
                inside += (height - 0.5) * 2.0 * roughness;
                return saturate(inside / width + 0.5);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.meters;
                float2 detail = p / _DetailSize;
                float4 masks = SAMPLE_TEXTURE2D(_Masks, sampler_Masks, input.uv);
                half4 tint = SAMPLE_TEXTURE2D(_ColorMap, sampler_ColorMap, input.uv);

                // Two scales of the base layer (5 m and 20 / 3 m), mixed by a blurred sample repeating every 20 m.
                half macro = SAMPLE_TEXTURE2D_BIAS(_BaseTex, sampler_BaseTex, p / 20.0 + 0.31, 5.0).a;
                half4 baseA = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, detail);
                half4 baseB = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, p * (3.0 / 20.0) + 0.5);
                half4 ground = lerp(baseA, baseB, smoothstep(0.42, 0.58, macro));
                half4 alt = SAMPLE_TEXTURE2D(_AltTex, sampler_AltTex, detail);
                half4 rock = SAMPLE_TEXTURE2D(_RockTex, sampler_RockTex, detail * 0.5);

                float wAlt = Cover(masks.a, alt.a, _Ranges.w, _Edges.w);
                float wRock = Cover(masks.b, rock.a, _Ranges.z, _Edges.z);
                half3 color = lerp(ground.rgb, alt.rgb, wAlt);
                color = lerp(color, rock.rgb, wRock);
                half3 emission = _CrackEmission.rgb * wRock * pow(saturate(1.0 - rock.a * 1.9), 2.2);

                [branch] if (masks.r > 0.3)
                {
                    half4 asphalt = SAMPLE_TEXTURE2D(_RoadTex, sampler_RoadTex, detail);
                    half4 concrete = SAMPLE_TEXTURE2D(_PavedTex, sampler_PavedTex, detail);
                    half4 paved = lerp(asphalt, concrete, saturate(tint.a * 1.5 - 0.25));
                    float wPaved = Cover(masks.r, paved.a, _Ranges.x, _Edges.x);
                    color = lerp(color, paved.rgb, wPaved);
                    emission *= 1.0 - wPaved;
                }
                color *= tint.rgb * 2.0;

                float3 normal = normalize(input.normalWS);
                Light light = GetMainLight();
                float3 ambient = SampleSH(normal);
                float sun = saturate(dot(normal, light.direction));
                half3 lit = color * (ambient + light.color * sun);

                [branch] if (masks.g > 0.3)
                {
                    float t = _Time.y * _Waves;
                    half r1 = SAMPLE_TEXTURE2D(_LiquidTex, sampler_LiquidTex, detail * 0.5 + t * float2(0.021, 0.013)).a;
                    half r2 = SAMPLE_TEXTURE2D(_LiquidTex, sampler_LiquidTex, detail * 0.25 + t * float2(-0.016, 0.022) + 0.43).a;
                    half ripple = (r1 + r2) * 0.5;
                    float depth = saturate((masks.g - 0.5) * 2.0);
                    float wLiquid = Cover(masks.g, ripple * 0.5 + 0.25, _Ranges.y, _Edges.y);
                    half3 water = lerp(_ShallowColor.rgb, _DeepColor.rgb, smoothstep(0.0, 0.85, depth));
                    water *= 0.88 + 0.24 * ripple;
                    float shore = saturate(1.0 - depth * 9.0);
                    float foam = shore * smoothstep(0.35, 0.65, ripple + shore * 0.35) * _FoamColor.a;
                    water = lerp(water, _FoamColor.rgb, foam);
                    half3 waterLit = water * (ambient + light.color * sun);
                    float3 rippled = normalize(float3((r1 - r2) * 0.55, (r1 + r2 - 1.0) * 0.55, 0.0) + normal);
                    float3 view = normalize(GetWorldSpaceViewDir(input.positionWS));
                    float3 halfway = normalize(light.direction + view);
                    float glint = pow(saturate(dot(rippled, halfway)), 48.0) * _Gloss;
                    waterLit += light.color * glint * (1.0 - foam);
                    half3 glow = _LiquidEmission.rgb * pow(saturate(1.15 - ripple), 2.5) * (0.4 + 0.6 * depth);
                    lit = lerp(lit, waterLit, wLiquid);
                    emission = lerp(emission, glow, wLiquid);
                }
                return half4(lit + emission, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half DepthFrag(DepthVaryings input) : SV_Target
            {
                return input.positionCS.z;
            }
            ENDHLSL
        }
    }
}
