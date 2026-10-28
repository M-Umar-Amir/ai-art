// 3D sign lettering (TextMesh) that follows the curved-world bend, so it stays glued to its board instead
// of floating off it down the street. Depth-tested like any surface, nudged toward the camera so it sits on
// the board face without z-fighting. Alpha comes from the font atlas, colour from the TextMesh vertex colour.
Shader "FoodInc/TextCurved"
{
    Properties
    {
        _MainTex ("Font Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off
        Offset -1, -4
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "ToonCommon.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            V vert(A v)
            {
                V o;
                o.positionCS = TransformWorldToHClip(CurveWorld(TransformObjectToWorld(v.positionOS.xyz)));
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }
            half4 frag(V i) : SV_Target
            {
                half4 c = i.color;
                c.a *= SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                clip(c.a - 0.02h);
                return c;
            }
            ENDHLSL
        }
    }
}
