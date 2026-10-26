Shader "BuyBidBlow/Planar Floor Coating"
{
    Properties
    {
        _Strength("Reflection Strength", Range(0, 1)) = 0.65
        _BlurMip("Reflection Blur (mip)", Range(0, 6)) = 2.2
        _Tint("Reflection Tint", Color) = (1, 0.96, 0.9, 1)
        [HideInInspector] _PlanarTexture("Reflection", 2D) = "black" {}
        [HideInInspector] _PlanarValid("Valid", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-50" "RenderType"="Transparent" }
        Pass
        {
            Name "FloorCoating"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_PlanarTexture); SAMPLER(sampler_PlanarTexture);
            CBUFFER_START(UnityPerMaterial)
                float4x4 _PlanarVP;
                half4 _Tint;
                float _Strength, _BlurMip, _PlanarValid;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                clip(_PlanarValid - 0.5);
                float4 projected = mul(_PlanarVP, float4(input.positionWS, 1));
                clip(projected.w - 0.0001);
                float2 uv = projected.xy / projected.w * 0.5 + 0.5;
                #if UNITY_UV_STARTS_AT_TOP
                    uv.y = 1 - uv.y;
                #endif
                clip(min(min(uv.x, uv.y), min(1 - uv.x, 1 - uv.y)));
                half3 reflection = SAMPLE_TEXTURE2D_LOD(_PlanarTexture, sampler_PlanarTexture, uv, _BlurMip).rgb;
                float facing = saturate(normalize(GetWorldSpaceViewDir(input.positionWS)).y);
                float fresnel = 0.35 + 0.65 * pow(1 - facing, 3);
                float edge = saturate(min(min(uv.x, uv.y), min(1 - uv.x, 1 - uv.y)) * 40);
                // An art-directed thin coating over the existing Lit floor. It intentionally
                // preserves the original lightmap/material rather than replacing URP's BRDF.
                return half4(reflection * _Tint.rgb, saturate(_Strength * fresnel * edge));
            }
            ENDHLSL
        }
    }
}
