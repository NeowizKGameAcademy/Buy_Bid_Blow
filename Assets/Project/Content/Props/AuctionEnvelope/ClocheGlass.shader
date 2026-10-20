Shader "BuyBidBlow/DisplayClocheGlass" {
 Properties { _BaseColor("Glass Tint",Color)=(.72,.9,.95,1) _CenterOpacity("Center Opacity",Range(0,1))=.035 _RimOpacity("Rim Opacity",Range(0,1))=.38 _Smoothness("Smoothness",Float)=.94 }
 SubShader {
 Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
 Pass {
 Blend SrcAlpha OneMinusSrcAlpha
 ZWrite Off
 Cull Back
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
 struct V {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float3 positionWS:TEXCOORD1;};
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseColor;float _CenterOpacity;float _RimOpacity;float _Smoothness;
 CBUFFER_END
 V vert(A i){V o;o.positionWS=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(i.normalOS);return o;}
 half4 frag(V i):SV_Target{float3 n=normalize(i.normalWS),v=normalize(GetWorldSpaceViewDir(i.positionWS));float rim=pow(1-saturate(dot(n,v)),3);float highlight=pow(saturate(dot(reflect(-normalize(float3(-.5,1,.5)),n),v)),50);float alpha=saturate(_CenterOpacity+rim*_RimOpacity+highlight*.25);return half4(lerp(_BaseColor.rgb,float3(1,1,1),saturate(rim*.6+highlight)),alpha);}
 ENDHLSL
 }
 }
}
