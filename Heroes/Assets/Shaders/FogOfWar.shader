// The unexplored land of the map, drawn over the whole screen after the scene: every pixel is looked up by the world
// position under it (from the depth texture) in a texture of what the player has explored, so trees, mountains and
// towns in the dark are hidden where they stand, whatever their height.
Shader "Heroes/FogOfWar"
{
    Properties
    {
        _FogTex ("Explored (0 seen, 1 hidden)", 2D) = "black" {}
        _NoiseTex ("Clouds", 2D) = "gray" {}
        _FogColor ("Fog", Color) = (0.015, 0.02, 0.035, 1)
        _EdgeColor ("Edge", Color) = (0.16, 0.19, 0.28, 1)
        _FogRect ("Map rect (x, z, 1/width, 1/depth)", Vector) = (0, 0, 0.01, 0.01)
        _Outside ("Darkness beyond the map", Range(0, 1)) = 0.55
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+100" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Fog"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_FogTex);
            SAMPLER(sampler_FogTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float4 _EdgeColor;
                float4 _FogRect;
                float _Outside;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                // The quad's corners go straight to the corners of the screen, wherever the camera looks.
                output.positionCS = float4(input.uv * 2.0 - 1.0, UNITY_NEAR_CLIP_VALUE, 1.0);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.positionCS.xy / _ScaledScreenParams.xy;
                #if UNITY_REVERSED_Z
                    float depth = SampleSceneDepth(screenUV);
                #else
                    float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(screenUV));
                #endif
                float3 world = ComputeWorldSpacePosition(screenUV, depth, UNITY_MATRIX_I_VP);
                float2 fogUV = (world.xz - _FogRect.xy) * _FogRect.zw;
                float hidden = SAMPLE_TEXTURE2D(_FogTex, sampler_FogTex, fogUV).r;
                float outside = (fogUV.x < 0.0 || fogUV.y < 0.0 || fogUV.x > 1.0 || fogUV.y > 1.0) ? 1.0 : 0.0;
                hidden = max(hidden, outside * _Outside);
                float2 drift = float2(_Time.y * 0.012, _Time.y * 0.007);
                float clouds = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, world.xz * 0.035 + drift).r;
                float wisps = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, world.xz * 0.09 - drift * 1.7).r;
                float edge = saturate((1.0 - hidden) * 2.5);
                float3 color = lerp(_FogColor.rgb, _EdgeColor.rgb, saturate(edge * 0.8 + clouds * 0.35));
                color += (wisps - 0.5) * 0.04;
                float alpha = saturate(hidden * (0.9 + clouds * 0.1));
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
