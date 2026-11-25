Shader "VuonTra/Spike3DHalo"
{
    Properties
    {
        _BaseColor("Color", Color) = (1, 0.784, 0.4, 1)
        _BaseMap("Falloff", 2D) = "white" {}
        _Intensity("Intensity", Float) = 0.6
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex HaloVert
            #pragma fragment HaloFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _Intensity;
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

            Varyings HaloVert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 HaloFrag(Varyings i) : SV_Target
            {
                half falloff = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a;
                return half4(_BaseColor.rgb * falloff * _Intensity, 1);
            }
            ENDHLSL
        }
    }
}
