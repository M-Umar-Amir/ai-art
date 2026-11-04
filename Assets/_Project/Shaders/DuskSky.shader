// Dash Riders dusk sky (PDF cover): indigo zenith, violet band, hot-pink to amber glow on the horizon the
// rider is heading for, faint stars overhead. Procedural, so it costs one full-screen gradient and no texture.
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
            struct A { float4 pos : POSITION; };
            struct V { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };
            V vert(A i)
            {
                V o;
                o.pos = TransformObjectToHClip(i.pos.xyz);
                o.dir = i.pos.xyz;
                return o;
            }
            float hash(float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }
            half4 frag(V i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = d.y;
                float3 sun = normalize(_SunDir.xyz);
                float toward = saturate(dot(normalize(float3(d.x, 0, d.z)), normalize(float3(sun.x, 0, sun.z))));
                // Above the horizon: glow -> violet band -> zenith.
                half3 c = lerp(_Horizon.rgb, _Band.rgb, smoothstep(0.0, 0.28, h));
                c = lerp(c, _Zenith.rgb, smoothstep(0.22, 0.75, h));
                // The sunset core sits on the horizon ahead and fades round the sky.
                float core = pow(toward, 6.0) * exp(-max(h, 0.0) * 9.0);
                c = lerp(c, _Sunset.rgb, saturate(core * 1.3));
                c += _Horizon.rgb * 0.35 * pow(toward, 2.0) * exp(-abs(h) * 14.0);
                // Below the horizon: the haze over the city floor.
                c = lerp(c, _Ground.rgb, smoothstep(0.0, -0.12, h));
                // Sparse stars high up.
                float3 cell = floor(d * 220.0);
                float star = step(0.9965, hash(cell)) * smoothstep(0.35, 0.8, h);
                c += star * 0.55;
                return half4(c * _Exposure, 1);
            }
            ENDHLSL
        }
    }
}
