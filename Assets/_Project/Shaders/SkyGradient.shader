// Cartoon daytime sky: deep blue overhead fading to a bright horizon, a soft sun glow, and drifting
// toon cumulus clouds (procedural noise projected onto a sky dome: crisp edges, white sunlit tops,
// blue-grey shaded bellies). Used as the skybox material; no textures needed.
Shader "FoodInc/SkyGradient"
{
    Properties
    {
        _Top ("Top", Color) = (0.08,0.3,0.78,1)
        _Horizon ("Horizon", Color) = (0.5,0.75,1,1)
        _Bottom ("Bottom", Color) = (0.75,0.86,1,1)
        _SunColor ("Sun Glow", Color) = (1,0.95,0.8,1)
        _SunDir ("Sun Direction", Vector) = (0.3,0.35,1,0)
        _CloudColor ("Cloud Lit", Color) = (1,1,1,1)
        _CloudShade ("Cloud Shade", Color) = (0.66,0.76,0.92,1)
        _CloudCover ("Cloud Cover", Range(0,1)) = 0.48
        _CloudSpeed ("Cloud Drift", Float) = 0.006
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            half4 _Top, _Horizon, _Bottom, _SunColor, _CloudColor, _CloudShade;
            float4 _SunDir;
            float _CloudCover, _CloudSpeed;
            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };
            V vert(A v) { V o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.dir = v.positionOS.xyz; return o; }

            float hash(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), f.x), lerp(hash(i + float2(0, 1)), hash(i + 1), f.x), f.y);
            }
            float fbm(float2 p)
            {
                float v = 0, a = 0.5;
                for (int k = 0; k < 4; k++) { v += a * noise(p); p = p * 2.03 + 17.1; a *= 0.5; }
                return v;
            }

            half4 frag(V i) : SV_Target
            {
                float3 d = normalize(i.dir);
                half3 c = d.y > 0 ? lerp(_Horizon.rgb, _Top.rgb, pow(saturate(d.y), 0.5)) : lerp(_Horizon.rgb, _Bottom.rgb, saturate(-d.y * 4));
                float3 sd = normalize(_SunDir.xyz);
                float sunDot = saturate(dot(d, sd));
                c += _SunColor.rgb * (pow(sunDot, 32) * 0.5 + pow(sunDot, 6) * 0.12);

                if (d.y > 0.0)
                {
                    // Project onto a flat cloud deck; distance squashes clouds into flat bands near the horizon.
                    float2 uv = d.xz / (d.y + 0.12) * 0.55 + float2(_Time.y * _CloudSpeed, _Time.y * _CloudSpeed * 0.3);
                    float n = fbm(uv * 1.6);
                    // Puffy shapes: big lobes from the noise, cut off with a soft-but-crisp edge (toon).
                    float cover = 1 - _CloudCover;
                    float body = smoothstep(cover, cover + 0.05, n);
                    // Shade the belly: sample the noise slightly toward the sun; denser toward the light reads as a lit top.
                    float lit = smoothstep(cover - 0.02, cover + 0.22, fbm((uv + sd.xz * 0.035) * 1.6));
                    half3 cloud = lerp(_CloudShade.rgb, _CloudColor.rgb, saturate(lit * 0.85 + 0.15));
                    cloud += _SunColor.rgb * pow(sunDot, 8) * 0.25 * (1 - lit);
                    float fade = smoothstep(0.02, 0.22, d.y); // clouds melt into the horizon haze
                    c = lerp(c, cloud, body * fade);
                }
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
