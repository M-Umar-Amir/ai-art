#ifndef FOODINC_TOON_COMMON
#define FOODINC_TOON_COMMON

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// Set once per frame by CurvedWorld.cs: the street bends down and sideways away from the camera.
float4 _CurveParams; // x: downward bend, y: sideways bend

TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half4 _ShadeColor;
    half4 _EmissionColor;
    half _RimStrength;
    half _OutlineWidth;
    half4 _OutlineColor;
    half _Cutoff;
    half _SurfaceStyle;
    half _Smoothness;
    half _Metallic;
    half _AmbientStrength;
    half _RimPower;
    half _ShadowSoftness;
    float _GaitPhase;
    float _GaitAmount;
    float _GaitMin;
    float _GaitHeight;
    float _GaitCenter;
    float _Portrait;
    float _Outfit;
    float _CrewIndex;
    float _WardrobeMin;
    float _WardrobeHeight;
CBUFFER_END

float3 DashPoseOS(float3 p)
{
    if (_GaitAmount < 0.001 || _GaitHeight < 0.001) return p;
    float h = saturate((p.y - _GaitMin) / _GaitHeight);
    float side = p.x < _GaitCenter ? -1 : 1;
    float swing = sin(_GaitPhase + (side < 0 ? 0 : 3.14159265));
    float leg = (1 - smoothstep(0.40,0.54,h));
    float arm = smoothstep(0.57,0.65,h) * (1-smoothstep(0.8,0.87,h)) * smoothstep(0.10,0.20,abs(p.x-_GaitCenter)/_GaitHeight);
    float angle = swing * (leg * 0.36 - arm * 0.14) * _GaitAmount;
    float pivot = _GaitMin + _GaitHeight * (arm > leg ? 0.75 : 0.49);
    float y = p.y - pivot;
    p.y = pivot + y*cos(angle) - p.z*sin(angle);
    p.z = y*sin(angle) + p.z*cos(angle);
    return p;
}

float3 CurveWorld(float3 positionWS)
{
    float dz = positionWS.z - _WorldSpaceCameraPos.z;
    float d2 = max(dz, 0) * max(dz, 0);
    positionWS.y -= d2 * _CurveParams.x;
    positionWS.x += d2 * _CurveParams.y;
    return positionWS;
}

#endif
