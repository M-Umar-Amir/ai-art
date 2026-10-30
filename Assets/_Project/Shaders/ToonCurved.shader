// Dash Riders: enhanced toon lighting with soft shadows, rim light, fog, and curved-world bend.
// Improved graphics: better light falloff, enhanced rim, ambient occlusion, and color grading.
// Outline pass is drawn only when _OutlineWidth > 0 (characters, rides, pickups).
Shader "FoodInc/ToonCurved"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _ShadeColor ("Shade Tint", Color) = (0.62,0.66,0.86,1)
        [HDR] _EmissionColor ("Emission", Color) = (0,0,0,0)
        _RimStrength ("Rim", Range(0,1)) = 0.42
        _RimPower ("Rim Power", Range(1,8)) = 4.5
        _SurfaceStyle ("Physically lit surface", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.52
        _Metallic ("Metallic", Range(0,1)) = 0.18
        _AmbientStrength ("Ambient Strength", Range(0,1)) = 0.35
        _ShadowSoftness ("Shadow Softness", Range(0,0.5)) = 0.18
        [HideInInspector] _Portrait("Portrait",Float)=0
        [HideInInspector] _GaitPhase("Gait phase",Float)=0
        [HideInInspector] _GaitAmount("Gait amount",Float)=0
        [HideInInspector] _GaitMin("Gait minimum",Float)=0
        [HideInInspector] _GaitHeight("Gait height",Float)=0
        [HideInInspector] _GaitCenter("Gait center",Float)=0
        [HideInInspector] _Outfit("Outfit",Float)=0
        [HideInInspector] _CrewIndex("Crew index",Float)=0
        [HideInInspector] _WardrobeMin("Wardrobe min",Float)=0
        [HideInInspector] _WardrobeHeight("Wardrobe height",Float)=0
        _OutlineWidth ("Outline Width", Range(0,0.05)) = 0
        _OutlineColor ("Outline Color", Color) = (0.12,0.1,0.16,1)
        // Alpha-tested cut-outs (tree leaves): 0 keeps everything. Leaves also render both faces.
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "ToonCommon.hlsl"
        ENDHLSL

        Pass
        {
            Name "ForwardToon"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ALPHATEST_ON

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half fog : TEXCOORD3;
                float localY : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 ws = TransformObjectToWorld(DashPoseOS(v.positionOS.xyz));
                o.positionWS = ws; // unbent position for lighting and shadow lookups
                o.localY = v.positionOS.y;
                o.positionCS = TransformWorldToHClip(CurveWorld(ws));
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                if (_Outfit > 0.5)
                {
                    float h = (i.localY - _WardrobeMin) / max(_WardrobeHeight, 0.001);
                    // Clothing hues identify the jacket or robot trim while preserving texture detail.
                    bool orange = albedo.r > albedo.g*1.25 && albedo.r > albedo.b*1.4 && albedo.r > 0.22;
                    bool purple = albedo.b > albedo.r*1.15 && albedo.b > albedo.g*1.2 && albedo.b > 0.2;
                    bool gold = albedo.r > 0.3 && albedo.g > 0.25 && albedo.b < albedo.g*0.65;
                    bool jacket = h > 0.44 && h < 0.8;
                    if ((_CrewIndex < 0.5 && orange && jacket) || (_CrewIndex > 0.5 && _CrewIndex < 1.5 && purple && jacket) || (_CrewIndex > 1.5 && gold))
                    {
                        half brightness = max(albedo.r,max(albedo.g,albedo.b));
                        albedo.rgb = brightness * (_CrewIndex > 1.5 ? half3(1,0.12,0.45) : half3(0.06,0.85,1));
                    }
                }
                #ifdef _ALPHATEST_ON
                clip(albedo.a - _Cutoff);
                #endif
                half3 n = normalize(i.normalWS);
                Light main = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                // Enhanced cel lighting with softer shadow transition
                half ndl = dot(n, main.direction);
                half shadowSoft = 1.0 - _ShadowSoftness;
                half lit = smoothstep(0.0, 0.18, ndl) * lerp(shadowSoft, 1.0, main.shadowAttenuation);
                half3 ambient = SampleSH(n) * _AmbientStrength;
                half3 shade = albedo.rgb * _ShadeColor.rgb * (0.5 + 0.5 * ambient) * half3(0.88, 0.94, 1.12);
                // Improved light calculation with better color response
                half3 light = albedo.rgb * (main.color * 0.72 + ambient * 0.22);
                half3 c = lerp(shade, light, lit);
                // Enhanced ambient occlusion and canyon effect
                half vertical = 1 - abs(n.y);
                half contact = lerp(0.58, 1, smoothstep(0.0, 1.5, i.positionWS.y));
                half canyon = lerp(0.91, 1, saturate(i.positionWS.y / 12.0));
                if (_Portrait > 0.5) { contact = 1; canyon = 1; }
                c *= lerp(1, contact * canyon, vertical);
                // Enhanced rim light with configurable power
                half3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                half rim = pow(1 - saturate(dot(n, v)), _RimPower) * _RimStrength * (0.35 + 0.65 * lit);
                c += rim * main.color * 0.75;
                c += _EmissionColor.rgb;
                if (_SurfaceStyle > 0.001)
                {
                    InputData data = (InputData)0;
                    data.positionWS = i.positionWS;
                    data.normalWS = n;
                    data.viewDirectionWS = v;
                    data.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                    data.bakedGI = SampleSH(n);
                    data.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                    data.shadowMask = half4(1,1,1,1);
                    SurfaceData surface = (SurfaceData)0;
                    surface.albedo = albedo.rgb;
                    surface.alpha = albedo.a;
                    surface.metallic = _Metallic;
                    surface.smoothness = _Smoothness;
                    surface.normalTS = half3(0,0,1);
                    surface.occlusion = 1;
                    surface.emission = _EmissionColor.rgb;
                    c = lerp(c, UniversalFragmentPBR(data, surface).rgb * lerp(1, contact * canyon, vertical), _SurfaceStyle);
                }
                c = MixFog(c, i.fog);
                return half4(c, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; half fog : TEXCOORD0; };
            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                float3 ws = TransformObjectToWorld(DashPoseOS(v.positionOS.xyz));
                float3 nws = normalize(TransformObjectToWorldNormal(v.normalOS));
                // Width in world units, scaled by distance a little so far objects keep a readable line.
                float dist = distance(ws, _WorldSpaceCameraPos);
                ws += nws * _OutlineWidth * (1 + dist * 0.04);
                o.positionCS = TransformWorldToHClip(CurveWorld(ws));
                if (_OutlineWidth <= 0) o.positionCS = float4(0, 0, -2, 1); // clipped: no outline
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 frag(Varyings i) : SV_Target { return half4(MixFog(_OutlineColor.rgb, i.fog), 1); }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma shader_feature_local _ALPHATEST_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct ShadowV { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            ShadowV vert(Attributes v)
            {
                ShadowV o;
                UNITY_SETUP_INSTANCE_ID(v);
                float3 ws = TransformObjectToWorld(DashPoseOS(v.positionOS.xyz));
                float3 nws = TransformObjectToWorldNormal(v.normalOS);
                float4 cs = TransformWorldToHClip(ApplyShadowBias(ws, nws, _LightDirection));
                #if UNITY_REVERSED_Z
                    cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = cs;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }
            half4 frag(ShadowV i) : SV_Target
            {
                // Leaf cut-outs cast leafy shadows, not squares.
                #ifdef _ALPHATEST_ON
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a * _BaseColor.a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ALPHATEST_ON
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct DepthV { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            DepthV vert(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                DepthV o;
                o.positionCS = TransformWorldToHClip(CurveWorld(TransformObjectToWorld(DashPoseOS(v.positionOS.xyz))));
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }
            half4 frag(DepthV i) : SV_Target
            {
                #ifdef _ALPHATEST_ON
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a * _BaseColor.a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }
    }
}
