// The drop shadow of the strike mode's aircraft: a flat, unlit, alpha-blended colour. Every pixel is darkened once
// per frame at most (a stencil bit marks it), so the overlapping faces of a flattened model, and shadows that overlap
// each other, never darken twice.
Shader "Portfolio/Asteroids/StrikeShadow"
{
    Properties
    {
        _BaseColor ("Color", Color) = (0, 0, 0, 0.45)
        _StencilBit ("Stencil Bit", Float) = 64
    }

    SubShader
    {
        Tags { "Queue" = "Transparent-50" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Stencil
        {
            Ref [_StencilBit]
            ReadMask [_StencilBit]
            WriteMask [_StencilBit]
            Comp NotEqual
            Pass Replace
        }

        Pass
        {
            Name "Shadow"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _StencilBit;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return _BaseColor;
            }
            ENDHLSL
        }
    }
}
