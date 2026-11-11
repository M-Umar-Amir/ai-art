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
            // A procedural skyline on the horizon (11 Nov 2026, PDF cover): the end of the street must never be an
            // empty gradient, and real geometry that far out would be bent under the horizon by the curved world.
            // Two layers of towers: a hazy far row and a darker, taller near row, with lit windows, stepped crowns,
            // a few spires and neon roof lines, fading into pink haze at their feet.
            half3 Skyline(float az, float h, half3 c)
            {
                half3 haze = _Horizon.rgb * 0.75 + _Sunset.rgb * 0.15;
                [unroll] for (int L = 0; L < 2; L++)
                {
                    float scale = L == 0 ? 40.0 : 21.0;
                    float x = az * scale + L * 17.31;
                    float cell = floor(x), fx = frac(x);
                    float r = hash(float3(cell, L, 3.1)), r2 = hash(float3(cell, L, 7.7)), r3 = hash(float3(cell, L, 1.9));
                    float width = lerp(0.5, 0.94, r2);
                    float half_w = width * 0.5, off = abs(fx - 0.5);
                    if (off > half_w) continue;
                    float top = (L == 0 ? 0.03 : 0.05) + r * r * (L == 0 ? 0.11 : 0.2);
                    float crown = off < half_w * 0.55 && r2 > 0.45 ? top * 0.12 : 0.0;
                    float spire = off < 0.025 && r3 > 0.72 ? top * 0.45 : 0.0;
                    float H = top + crown + spire;
                    if (h > H || h < -0.25) continue;
                    half3 body = L == 0 ? lerp(haze, half3(0.2, 0.1, 0.3), 0.5) : half3(0.075, 0.05, 0.15);
                    // Windows: a column/row grid inside the tower body (not the spire), some warm, some cyan.
                    float cols = floor(lerp(5.0, 11.0, r3));
                    float u = (fx - (0.5 - half_w)) / width;
                    float col = floor(u * cols), row = floor(h * (L == 0 ? 260.0 : 170.0));
                    float inCol = step(0.22, frac(u * cols)) * step(frac(u * cols), 0.78);
                    float inRow = step(0.3, frac(h * (L == 0 ? 260.0 : 170.0)));
                    float lit = step(0.7, hash(float3(cell * 13.0 + col, row, L + 5.0))) * inCol * inRow * step(h, top);
                    half3 window = hash(float3(cell, col, row + 9.0)) > 0.35 ? half3(1.0, 0.66, 0.34) : half3(0.35, 0.85, 1.0);
                    body += window * lit * (L == 0 ? 0.45 : 0.9);
                    // A neon line on some roofs.
                    if (r3 > 0.55 && abs(h - top) < (L == 0 ? 0.0012 : 0.0018)) body = r > 0.5 ? half3(1.0, 0.25, 0.7) : half3(0.2, 0.9, 1.0);
                    // Haze at the feet of the towers, thicker on the far row.
                    float feet = saturate(1.0 - h / (L == 0 ? 0.07 : 0.045));
                    body = lerp(body, haze, saturate(feet * (L == 0 ? 0.85 : 0.6)));
                    c = body;
                }
                return c;
            }
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
                c = Skyline(atan2(d.x, d.z), h, c);
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
