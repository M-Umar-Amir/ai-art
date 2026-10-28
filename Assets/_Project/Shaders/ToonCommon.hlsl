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
CBUFFER_END

float3 CurveWorld(float3 positionWS)
{
    float dz = positionWS.z - _WorldSpaceCameraPos.z;
    float d2 = max(dz, 0) * max(dz, 0);
    positionWS.y -= d2 * _CurveParams.x;
    positionWS.x += d2 * _CurveParams.y;
    return positionWS;
}

#endif
