// Energy shield of the ship and the bosses, and the glowing atmosphere of the backdrop planets: an additive fresnel
// rim with an optional hexagon grid and a bright patch where the last hit landed.
Shader "Portfolio/Asteroids/Shield"
{
    Properties
    {
        [HDR] _BaseColor ("Color (alpha scales everything)", Color) = (0.35, 0.8, 1.6, 1)
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 2.5
        _Hex ("Hex Grid", Range(0, 1)) = 1
        _HexScale ("Hex Scale", Float) = 5
        _Core ("Face Glow", Range(0, 1)) = 0.06
        _Hit ("Hit (direction xyz, strength w)", Vector) = (0, 1, 0, 0)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Back

        Pass
        {
            Name "Unlit"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _FresnelPower;
                float _Hex;
                float _HexScale;
                float _Core;
                float4 _Hit;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewWS = GetWorldSpaceViewDir(positionWS);
                output.positionOS = input.positionOS.xyz;
                return output;
            }

            // Distance to the edge of the hexagon cell containing p: 0.5 on the edges.
            float HexEdge(float2 p)
            {
                const float2 s = float2(1.0, 1.7320508);
                float4 centers = floor(float4(p, p - float2(0.5, 1.0)) / s.xyxy) + 0.5;
                float4 offsets = float4(p - centers.xy * s, p - (centers.zw + 0.5) * s);
                float2 q = dot(offsets.xy, offsets.xy) < dot(offsets.zw, offsets.zw) ? offsets.xy : offsets.zw;
                q = abs(q);
                return max(dot(q, s * 0.5), q.x);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 view = normalize(input.viewWS);
                float rim = pow(1.0 - saturate(dot(normal, view)), _FresnelPower);
                float edge = HexEdge(input.positionOS.xy * _HexScale);
                float aa = max(fwidth(edge), 0.0001);
                float lines = smoothstep(0.46 - aa, 0.5, edge) * _Hex;
                float3 local = normalize(input.positionOS);
                float hit = pow(saturate(dot(local, normalize(_Hit.xyz + 0.0001))), 5.0) * _Hit.w;
                float glow = rim + lines * (0.25 + rim) + _Core + hit * (1.2 + lines * 2.0);
                return half4(_BaseColor.rgb * glow * _BaseColor.a, 1.0);
            }
            ENDHLSL
        }
    }
}
