// Dash Riders dusk sky (PDF cover): indigo zenith, violet band, hot-pink to amber glow on the horizon the
// rider is heading for, faint stars overhead. Procedural, so it costs one full-screen gradient and no texture.
// The maths lives in DuskSkyCommon.hlsl, shared with the street's far fade (ToonCommon.hlsl).
Shader "DashRiders/DuskSky"
{
    Properties
    {
        _Zenith("Zenith", Color) = (0.07, 0.06, 0.24, 1)
        _Band("Upper band", Color) = (0.33, 0.14, 0.52, 1)
        _Horizon("Horizon glow", Color) = (1.0, 0.36, 0.55, 1)
        _Sunset("Sunset core", Color) = (1.0, 0.62, 0.32, 1)
        _Ground("Below horizon", Color) = (0.12, 0.06, 0.16, 1)
        _SunDir("Sunset direction", Vector) = (0, 0.06, 1, 0)
        _Exposure("Exposure", Float) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Zenith, _Band, _Horizon, _Sunset, _Ground;
                float4 _SunDir;
                half _Exposure;
            CBUFFER_END
            // Set by TorontoSkyline.cs (26 Nov 2026): the sine of the angle the visible horizon lies below the true
            // horizontal. Ahead in the run the curved world drops the street away, so its end (the road's crest)
            // lies about 7 degrees down; the glow and the far city sit on that crest, under the lowered skyline.
            float _HorizonDip;
            struct A { float4 pos : POSITION; };
            struct V { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };
            V vert(A i)
            {
                V o;
                o.pos = TransformObjectToHClip(i.pos.xyz);
                o.dir = i.pos.xyz;
                return o;
            }
            #include "DuskSkyCommon.hlsl"
            half4 frag(V i) : SV_Target
            {
                DuskSkyParams s;
                s.zenith = _Zenith.rgb; s.band = _Band.rgb; s.horizon = _Horizon.rgb; s.sunset = _Sunset.rgb; s.ground = _Ground.rgb;
                s.sunDir = _SunDir.xyz; s.exposure = _Exposure; s.dip = _HorizonDip;
                return half4(DuskSkyColour(s, normalize(i.dir), true), 1);
            }
            ENDHLSL
        }
    }
}
