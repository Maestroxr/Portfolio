// The vector-arcade look of the Arcade 80s theme: an unlit dark hull with bright neon edges. The edges are the
// triangle edges of the mesh (drawn from barycentric coordinates the art builder bakes into the vertex colours of its
// wire copies, alpha 0 marking them, a constant width on screen; the second UV set holds how bright each edge is, full
// on the feature edges and dim on the others), the latitude and longitude lines of a globe (_Grid, for a mesh without
// barycentrics such as the planet spheres), and a fresnel rim. _EmissionColor is added on top so the hit flash of a
// Shootable (a material property block) still shows.
Shader "Portfolio/Asteroids/NeonSurface"
{
    Properties
    {
        [HDR] _EdgeColor ("Edge", Color) = (0.1, 2.4, 2.8, 1)
        _FillColor ("Fill", Color) = (0.02, 0.01, 0.06, 1)
        _EdgeWidth ("Edge Width (pixels)", Range(0.5, 4)) = 1.4
        _Wire ("Triangle Edges", Range(0, 1)) = 1
        _Rim ("Rim", Range(0, 2)) = 0.35
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _Grid ("Globe Grid", Range(0, 1)) = 0
        _GridScale ("Grid Lines (u, v)", Vector) = (24, 12, 0, 0)
        _Pulse ("Pulse", Range(0, 1)) = 0.2
        [HDR] _EmissionColor ("Emission (hit flash)", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _EdgeColor;
            half4 _FillColor;
            float _EdgeWidth;
            float _Wire;
            float _Rim;
            float _RimPower;
            float _Grid;
            float4 _GridScale;
            float _Pulse;
            half4 _EmissionColor;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float3 strength : TEXCOORD1;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 strength : TEXCOORD3;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.strength = input.strength;
                output.color = input.color;
                return output;
            }

            // A line where a value crosses zero, _EdgeWidth pixels wide, anti-aliased by the value's screen derivative.
            float Line(float value)
            {
                float width = max(fwidth(value), 1e-5);
                return 1.0 - smoothstep(width * (_EdgeWidth - 0.5), width * (_EdgeWidth + 0.5), value);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 b = input.color.rgb;
                // The drawn edges: a barycentric coordinate is zero along an edge. A mesh whose colours are not coordinates has
                // alpha 1 (the derivatives are taken outside any branch).
                float wireOn = _Wire > 0.5 && input.color.a < 0.5 ? 1.0 : 0.0;
                float3 s = input.strength;
                float wire = max(Line(b.r) * s.x, max(Line(b.g) * s.y, Line(b.b) * s.z)) * wireOn;
                float2 cells = input.uv * _GridScale.xy;
                float2 away = abs(frac(cells) - 0.5);
                float2 width = max(fwidth(cells), 1e-5) * _EdgeWidth;
                float2 lines = smoothstep(0.5 - width * 1.5, 0.5 - width * 0.5, away);
                float grid = max(lines.x, lines.y) * (_Grid > 0.5 ? 1.0 : 0.0);
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                float facing = saturate(dot(normalize(input.normalWS), viewDir));
                float rim = pow(1.0 - facing, _RimPower) * _Rim;
                float pulse = 1.0 + _Pulse * sin(_Time.y * 2.5 + input.positionWS.y * 0.8 + input.positionWS.x * 0.3);
                float edge = saturate(max(wire, grid));
                half3 color = lerp(_FillColor.rgb, _EdgeColor.rgb * pulse, edge);
                color += _EdgeColor.rgb * rim * (1.0 - edge) * 0.8;
                color += _FillColor.rgb * 0.5 * (1.0 - facing);
                // The hit flash lights the edges and only tints the hull, so a boss under fire does not turn into a white block.
                color += _EmissionColor.rgb * lerp(0.12, 0.6, edge);
                return half4(color, 1.0);
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
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half DepthFrag(DepthVaryings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
