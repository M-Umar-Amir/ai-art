Shader "FoodInc/HologramCurved"
{
    Properties { _BaseMap("Image",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "ToonCommon.hlsl"
            struct A {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct V {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;half fog:TEXCOORD1;};
            V vert(A v){V o;o.pos=TransformWorldToHClip(CurveWorld(TransformObjectToWorld(v.vertex.xyz)));o.uv=v.uv;o.fog=ComputeFogFactor(o.pos.z);return o;}
            half4 frag(V i):SV_Target
            {
                half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;
                half scan=0.94+0.06*sin((i.uv.y+_Time.y*0.025)*1100);
                c.rgb*=scan*1.25;
                c.rgb=MixFog(c.rgb,i.fog);
                c.a*=0.94;
                return c;
            }
            ENDHLSL
        }
    }
}
