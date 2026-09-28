// Stylised sky of the Endless Runner: a three colour gradient with a sun (or moon), drifting clouds, stars and two
// ridges of mountains along the horizon, or (with _Skyline on, the Night Shift theme's sky material) two rows of city
// blocks with lit windows in their place. The ThemeController sets the colours per world.
Shader "Portfolio/EndlessRunner/Sky"
{
    Properties
    {
        _TopColor ("Top", Color) = (0.25, 0.55, 0.95, 1)
        _HorizonColor ("Horizon", Color) = (0.75, 0.9, 1, 1)
        _BottomColor ("Bottom", Color) = (0.55, 0.7, 0.6, 1)
        _SunColor ("Sun", Color) = (1, 0.95, 0.8, 1)
        _SunDirection ("Direction To Sun", Vector) = (0.3, 0.5, 0.8, 0)
        _SunSize ("Sun Size", Range(0.005, 0.2)) = 0.04
        _StarDensity ("Stars", Range(0, 1)) = 0
        _CloudColor ("Clouds", Color) = (1, 1, 1, 1)
        _CloudCoverage ("Cloud Coverage", Range(0, 1)) = 0.45
        _MountainFar ("Far Mountains", Color) = (0.55, 0.7, 0.85, 1)
        _MountainNear ("Near Mountains", Color) = (0.4, 0.6, 0.5, 1)
        _MountainHeight ("Mountain Height", Range(0, 0.3)) = 0.09
        _Skyline ("City Skyline", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _HorizonColor;
                half4 _BottomColor;
                half4 _SunColor;
                float4 _SunDirection;
                float _SunSize;
                float _StarDensity;
                half4 _CloudColor;
                float _CloudCoverage;
                half4 _MountainFar;
                half4 _MountainNear;
                float _MountainHeight;
                float _Skyline;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            float Noise2(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                for (int k = 0; k < 4; k++)
                {
                    value += amplitude * Noise2(p);
                    p = p * 2.03 + 17.1;
                    amplitude *= 0.5;
                }
                return value;
            }

            // Height of a mountain ridge around the horizon, sampled on a circle so it wraps without a seam.
            float Ridge(float azimuth, float scale, float seed)
            {
                float2 p = float2(cos(azimuth), sin(azimuth)) * scale + seed;
                float height = 0.0;
                float amplitude = 0.6;
                for (int k = 0; k < 3; k++)
                {
                    float n = Noise2(p);
                    height += amplitude * (1.0 - abs(n * 2.0 - 1.0));
                    p = p * 2.1 + 3.7;
                    amplitude *= 0.45;
                }
                return height;
            }

            // Height of a row of city blocks around the horizon: flat roofs of hashed heights, sampled so it wraps.
            float Blocks(float azimuth, float count, float seed)
            {
                float a = (azimuth + 3.14159265) / 6.2831853 * count;
                float cell = floor(a);
                float h = Hash21(float2(cell, seed));
                float wide = Hash21(float2(cell, seed + 7.0));
                float inset = step(0.06, frac(a)) * step(frac(a), 0.94 + wide * 0.06);
                return (0.15 + 0.85 * h * h) * inset;
            }

            // A sprinkle of lit windows on the face of a row of blocks.
            float Windows(float azimuth, float y, float count, float seed)
            {
                float2 grid = float2((azimuth + 3.14159265) / 6.2831853 * count * 9.0, y * 220.0);
                float2 cell = floor(grid);
                float2 f = frac(grid);
                float lit = step(0.72, Hash21(cell + seed));
                float inside = step(0.25, f.x) * step(f.x, 0.7) * step(0.3, f.y) * step(f.y, 0.75);
                return lit * inside;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 d = normalize(input.direction);
                float y = d.y;

                float3 color = y >= 0.0
                    ? lerp(_HorizonColor.rgb, _TopColor.rgb, pow(saturate(y), 0.55))
                    : lerp(_HorizonColor.rgb, _BottomColor.rgb, saturate(-y * 5.0));

                if (_StarDensity > 0.001 && y > 0.0)
                {
                    // One candidate star per small cell of directions, drawn as a soft point inside its cell.
                    float3 grid = d * 360.0;
                    float3 cell = floor(grid);
                    float h = Hash31(cell);
                    if (h > 1.0 - _StarDensity * 0.03)
                    {
                        float3 center = cell + 0.5 + (float3(Hash31(cell + 11.0), Hash31(cell + 23.0), Hash31(cell + 37.0)) - 0.5) * 0.5;
                        float starShape = smoothstep(0.32, 0.0, length(grid - center));
                        float twinkle = 0.55 + 0.45 * sin(_Time.y * 2.5 + h * 60.0);
                        color += starShape * twinkle * saturate(y * 4.0) * (0.6 + 0.6 * frac(h * 13.0));
                    }
                }

                float3 sunDirection = normalize(_SunDirection.xyz);
                float sunDot = dot(d, sunDirection);
                float glow = pow(saturate(sunDot), 60.0) * 0.6 + pow(saturate(sunDot), 8.0) * 0.18;
                color += _SunColor.rgb * glow;
                float disc = smoothstep(cos(_SunSize * 1.15), cos(_SunSize), sunDot);
                color = lerp(color, _SunColor.rgb * 1.2, disc);

                if (y > 0.0)
                {
                    float2 uv = d.xz / (y + 0.12) * 0.9 + _Time.y * float2(0.004, 0.012);
                    float n = Fbm(uv * 1.3);
                    float cover = saturate((n - (1.0 - _CloudCoverage) * 0.8) * 3.0) * smoothstep(0.0, 0.22, y);
                    float3 cloud = lerp(_CloudColor.rgb * 0.82, _CloudColor.rgb, saturate(n * 1.3)) + _SunColor.rgb * glow * 0.4;
                    color = lerp(color, cloud, cover * _CloudColor.a);
                }

                float azimuth = atan2(d.x, d.z);
                float enabled = step(0.001, _MountainHeight);
                float yy = y + 0.004;
                float city = step(0.5, _Skyline);
                float farHeight = _MountainHeight * lerp(0.35 + 0.9 * Ridge(azimuth, 1.6, 3.1), 0.2 + 1.1 * Blocks(azimuth, 90.0, 3.1), city);
                float farMask = smoothstep(farHeight + 0.002, farHeight - 0.002, yy) * step(-0.08, yy) * enabled;
                float3 farColor = lerp(lerp(_MountainFar.rgb, _HorizonColor.rgb, 0.55), _MountainFar.rgb, saturate(yy / max(farHeight, 0.001)));
                farColor += _HorizonColor.rgb * Windows(azimuth, yy, 90.0, 3.1) * 0.8 * city * step(0.0, yy);
                color = lerp(color, farColor, farMask * _MountainFar.a);

                float nearHeight = _MountainHeight * lerp((0.1 + 0.75 * Ridge(azimuth, 2.7, 11.3)) * 0.8, 0.1 + 0.7 * Blocks(azimuth, 48.0, 11.3), city);
                float nearMask = smoothstep(nearHeight + 0.002, nearHeight - 0.002, yy) * step(-0.08, yy) * enabled;
                float3 nearColor = lerp(lerp(_MountainNear.rgb, _HorizonColor.rgb, 0.45), _MountainNear.rgb, saturate(yy / max(nearHeight, 0.001)));
                nearColor += _HorizonColor.rgb * Windows(azimuth, yy, 48.0, 11.3) * 0.9 * city * step(0.0, yy);
                color = lerp(color, nearColor, nearMask * _MountainNear.a);

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
