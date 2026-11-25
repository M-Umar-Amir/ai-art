// Additive soft glow that follows the curved-world bend (burger halos). Unlit, no depth write.
Shader "FoodInc/GlowCurved"
{
    Properties
    {
        _BaseMap ("Glow", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,0.78,0.25,0.9)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "ToonCommon.hlsl"
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; };
            V vert(A v)
            {
                V o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(CurveWorld(o.positionWS));
                o.uv = v.uv;
                return o;
            }
            half4 frag(V i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                c.a *= 1 - FarFadeAmount(i.positionWS); // fades with the far street (TorontoSkyline)
                return c;
            }
            ENDHLSL
        }
    }
}
