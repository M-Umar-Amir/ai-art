// Food Inc Game look: two-tone cel lighting, a soft rim, fog, and the curved-world bend.
// Outline pass is drawn only when _OutlineWidth > 0 (characters, rides, pickups).
Shader "FoodInc/ToonCurved"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _ShadeColor ("Shade Tint", Color) = (0.62,0.66,0.86,1)
        [HDR] _EmissionColor ("Emission", Color) = (0,0,0,0)
        _RimStrength ("Rim", Range(0,1)) = 0.35
        _OutlineWidth ("Outline Width", Range(0,0.05)) = 0
        _OutlineColor ("Outline Color", Color) = (0.12,0.1,0.16,1)
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
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half fog : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                o.positionWS = ws; // unbent position for lighting and shadow lookups
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
                half3 n = normalize(i.normalWS);
                Light main = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                // Cel step on the surface angle first, then the shadow on top. (Multiplying the shadow into
                // ndl before the step let a 0.75-strength shadow still clear the 0.12 threshold: no shadows.)
                half ndl = dot(n, main.direction);
                half lit = smoothstep(0.0, 0.12, ndl) * main.shadowAttenuation;
                half3 ambient = SampleSH(n);
                half3 shade = albedo.rgb * _ShadeColor.rgb * (0.55 + 0.45 * ambient) * half3(0.86, 0.92, 1.08);
                // Full sun lands near 1.1x albedo (sun 1.35 x 0.68 plus a little sky), so pale walls and sidewalks
                // keep their colour instead of washing out to cream under bloom.
                half3 light = albedo.rgb * (main.color * 0.68 + ambient * 0.18);
                half3 c = lerp(shade, light, lit);
                // Baked-look occlusion without lightmaps (the street is generated at runtime): walls and the sides
                // of props darken softly where they meet the ground, and lower floors get a little less sky than
                // upper ones in the street canyon. Only vertical surfaces; the road itself is untouched.
                half vertical = 1 - abs(n.y);
                half contact = lerp(0.62, 1, smoothstep(0.0, 1.3, i.positionWS.y));
                half canyon = lerp(0.93, 1, saturate(i.positionWS.y / 10.0));
                c *= lerp(1, contact * canyon, vertical);
                half3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                half rim = pow(1 - saturate(dot(n, v)), 3) * _RimStrength * (0.4 + 0.6 * lit);
                c += rim * main.color * 0.6;
                c += _EmissionColor.rgb;
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
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
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
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 vert(Attributes v) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(v);
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                float3 nws = TransformObjectToWorldNormal(v.normalOS);
                float4 cs = TransformWorldToHClip(ApplyShadowBias(ws, nws, _LightDirection));
                #if UNITY_REVERSED_Z
                    cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return cs;
            }
            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 vert(Attributes v) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(v);
                return TransformWorldToHClip(CurveWorld(TransformObjectToWorld(v.positionOS.xyz)));
            }
            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
