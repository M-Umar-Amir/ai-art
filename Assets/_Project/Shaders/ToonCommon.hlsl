#ifndef FOODINC_TOON_COMMON
#define FOODINC_TOON_COMMON

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// Set once per frame by CurvedWorld.cs: the street bends down and sideways away from the camera.
float4 _CurveParams; // x: downward bend, y: sideways bend

// Set by TorontoSkyline.cs while the CN Tower skyline is shown (26 Nov 2026). Past the skyline the street's own
// far end, fogged pink, used to show under and between the CN Tower and the Financial District. Now:
//  - the skyline is drawn as a thin card: each vertex slides along its own view ray (the picture is unchanged) to a
//    distance between _BackdropCard.x and about 3 m further, so all the street nearer than that draws over it and
//    all of it further away is hidden behind it;
//  - the street fades into the exact colour of the dusk sky (DuskSkyCommon.hlsl) between _FarFade.x and
//    _FarFade.x + 1 / _FarFade.y metres along the street, so above and beside the skyline it melts into the sky;
//  - hurdles, burgers and the coupon (renderer user value bit 0, TrackSpawner.Register) never fade, and past the
//    card they slide toward the camera to just in front of it, so they still show coming over the crest.
float4 _FarFade;      // x: fade start (m), y: 1 / fade length, z: 1 on
float4 _BackdropCard; // x: card distance (m), y: model's nearest distance (m), z: depth squash, w: 1 on
// The skyline's dusk haze colour (TorontoSkyline); alpha 0 falls back to the fog colour.
half4 _BackdropHazeColor;
// The dusk sky's colours (copied from the DuskSky skybox material) and its horizon dip.
half4 _DuskZenith, _DuskBand, _DuskHorizon, _DuskSunset, _DuskGround;
float4 _DuskSunDir;
float _DuskExposure;
float _HorizonDip;

#include "DuskSkyCommon.hlsl"

/// Hurdles, burgers and the coupon (TrackSpawner.Register sets the renderer's shader user value).
bool IsTrackObject() { return (unity_RendererUserValue & 1u) != 0; }

half FarFadeAmount(float3 positionWS)
{
    if (_FarFade.z <= 0 || IsTrackObject()) return 0;
    return smoothstep(0, 1, saturate((abs(positionWS.z - _WorldSpaceCameraPos.z) - _FarFade.x) * _FarFade.y));
}

DuskSkyParams DuskSkyGlobals()
{
    DuskSkyParams s;
    s.zenith = _DuskZenith.rgb; s.band = _DuskBand.rgb; s.horizon = _DuskHorizon.rgb; s.sunset = _DuskSunset.rgb;
    s.ground = _DuskGround.rgb; s.sunDir = _DuskSunDir.xyz; s.exposure = _DuskExposure; s.dip = _HorizonDip;
    return s;
}

TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
TEXTURE2D(_RoughnessMap); SAMPLER(sampler_RoughnessMap);
// Lit-window atlas (DowntownArchitecture): emission comes from this map when _EmissionMapOn > 0.5, so stone
// and frames stay unlit while only the windows glow.
TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
// Vehicle livery (HurdleBrand, 11 Nov 2026): the official logo projected into the vehicle's own paint.
TEXTURE2D(_LiveryMap); SAMPLER(sampler_LiveryMap);

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
    half _BumpScale;
    half _RoughnessStrength;
    half _Wetness;
    half _EmissionUsesAlbedo;
    half _WindowInterior;
    half _EmissionMapOn;
    float4x4 _LiveryObjectToBody; // mesh object space -> vehicle body space (x across, y up from the road, z nose)
    float4 _LiveryRect;           // side logo: centre z, centre y, width (along z), height
    float4 _LiveryRear;           // rear logo: centre x, centre y, width, height (width 0 = none)
    float4 _LiveryStripe;         // brand stripe: y from, y to
    half4 _LiveryColor;
    half _LiveryOn;
    half _AmbientStrength;
    half _RimPower;
    half _ShadowSoftness;
    float _WorldTextureScale;
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
    // Skyline backdrop (TorontoSkyline, 25 Nov 2026): > 0 marks a far landmark (the CN Tower and the Financial
    // District) built at reduced scale inside the draw distance. It is not bent by the curved world, ignores the
    // distance fog and is hazed by this fixed amount instead, thicker towards its feet over _BackdropFeet metres
    // of object height.
    half _Backdrop;
    float _BackdropFeet;
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

/// Slides a point along its view ray to `distance` from the camera: same pixel, new depth.
float3 AlongViewRay(float3 positionWS, float distance)
{
    float3 v = positionWS - _WorldSpaceCameraPos;
    return _WorldSpaceCameraPos + v * (distance / max(length(v), 1e-3));
}

float3 CurveWorld(float3 positionWS)
{
    if (_Backdrop > 0)
    {
        // The backdrop sits on the horizon, it does not drop away; it is drawn as a thin card (see _BackdropCard).
        if (_BackdropCard.w <= 0) return positionWS;
        float d = length(positionWS - _WorldSpaceCameraPos);
        return AlongViewRay(positionWS, _BackdropCard.x + max(d - _BackdropCard.y, 0) * _BackdropCard.z);
    }
    float dz = positionWS.z - _WorldSpaceCameraPos.z;
    float d2 = max(dz, 0) * max(dz, 0);
    positionWS.y -= d2 * _CurveParams.x;
    positionWS.x += d2 * _CurveParams.y;
    if (_BackdropCard.w > 0 && IsTrackObject())
    {
        // A hurdle or burger past the card draws just in front of it, in the same order as before.
        float d = length(positionWS - _WorldSpaceCameraPos), front = _BackdropCard.x - 5;
        if (d > front) positionWS = AlongViewRay(positionWS, front + (d - front) * 0.02);
    }
    return positionWS;
}

/// The street's far end melts into the dusk sky behind it (TorontoSkyline); the skyline itself never does.
half3 FarSkyFade(half3 c, float3 positionWS)
{
    half f = FarFadeAmount(positionWS);
    if (f <= 0) return c;
    float3 d = normalize(CurveWorld(positionWS) - _WorldSpaceCameraPos);
    return lerp(c, DuskSkyColour(DuskSkyGlobals(), d, false), f);
}

#endif
