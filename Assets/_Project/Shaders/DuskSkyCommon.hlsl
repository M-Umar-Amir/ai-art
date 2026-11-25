#ifndef DASH_DUSK_SKY_COMMON
#define DASH_DUSK_SKY_COMMON

// The Dash Riders dusk sky as a function of view direction, shared by DuskSky.shader (the skybox itself) and
// ToonCommon.hlsl (26 Nov 2026: the street's far end melts into exactly this colour, so past the Toronto skyline
// there is only sky, with no seam, no dissolve pattern and no pink fogged buildings).
// Indigo zenith, violet band, hot-pink to amber glow on the horizon the rider is heading for, a procedural far city
// on the horizon. `dip` (sine) drops the horizon to the road's crest; `stars` adds the sparse stars overhead.

struct DuskSkyParams
{
    half3 zenith, band, horizon, sunset, ground;
    float3 sunDir;
    half exposure;
    float dip;
};

float DuskHash(float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }

// A procedural skyline on the horizon (11 Nov 2026, PDF cover): the end of the street must never be an empty
// gradient. Two layers of towers: a hazy far row and a darker, taller near row, with lit windows and stepped
// crowns, fading at their feet into the glow of the sky behind them.
half3 DuskSkyline(DuskSkyParams s, float az, float h, half3 c)
{
    half3 haze = s.horizon * 0.75 + s.sunset * 0.15;
    [unroll] for (int L = 0; L < 2; L++)
    {
        float scale = L == 0 ? 40.0 : 21.0;
        float x = az * scale + L * 17.31;
        float cell = floor(x), fx = frac(x);
        float r = DuskHash(float3(cell, L, 3.1)), r2 = DuskHash(float3(cell, L, 7.7)), r3 = DuskHash(float3(cell, L, 1.9));
        float width = lerp(0.5, 0.94, r2);
        float half_w = width * 0.5, off = abs(fx - 0.5);
        if (off > half_w) continue;
        // Toronto (25 Nov 2026): a low, even far city, so the CN Tower and the Financial District (TorontoSkyline,
        // real geometry in front of this) stand well clear of it.
        float top = (L == 0 ? 0.018 : 0.028) + r * r * (L == 0 ? 0.045 : 0.065);
        float crown = off < half_w * 0.55 && r2 > 0.45 ? top * 0.12 : 0.0;
        float spire = off < 0.02 && r3 > 0.93 ? top * 0.25 : 0.0;
        float H = top + crown + spire;
        if (h > H || h < -0.25) continue;
        half3 body = L == 0 ? lerp(haze, half3(0.2, 0.1, 0.3), 0.5) : half3(0.075, 0.05, 0.15);
        // Windows: a column/row grid inside the tower body (not the spire), some warm, some cyan.
        float cols = floor(lerp(5.0, 11.0, r3));
        float u = (fx - (0.5 - half_w)) / width;
        float col = floor(u * cols), row = floor(h * (L == 0 ? 260.0 : 170.0));
        float inCol = step(0.22, frac(u * cols)) * step(frac(u * cols), 0.78);
        float inRow = step(0.3, frac(h * (L == 0 ? 260.0 : 170.0)));
        float lit = step(0.7, DuskHash(float3(cell * 13.0 + col, row, L + 5.0))) * inCol * inRow * step(h, top);
        half3 window = DuskHash(float3(cell, col, row + 9.0)) > 0.3 ? half3(1.0, 0.68, 0.38) : half3(0.82, 0.86, 0.92);
        body += window * lit * (L == 0 ? 0.45 : 0.9);
        // (No neon roof lines: a real Toronto skyline, client 25 Nov 2026. Red aviation lights instead.)
        if (r > 0.6 && off < 0.03 && abs(h - H) < 0.0015) body = half3(1.0, 0.15, 0.1);
        // Haze at the feet of the towers, thicker on the far row: they fade into the glow of the sky behind them
        // (26 Nov 2026: no hot-pink band under the Toronto skyline).
        float feet = saturate(1.0 - h / (L == 0 ? 0.07 : 0.045));
        body = lerp(body, lerp(c, haze, 0.25), saturate(feet * (L == 0 ? 0.6 : 0.35)));
        c = body;
    }
    return c;
}

half3 DuskSkyColour(DuskSkyParams s, float3 d, bool stars)
{
    float h = d.y + s.dip;
    float3 sun = normalize(s.sunDir);
    float toward = saturate(dot(normalize(float3(d.x, 0, d.z)), normalize(float3(sun.x, 0, sun.z))));
    // Above the horizon: glow -> violet band -> zenith.
    half3 c = lerp(s.horizon, s.band, smoothstep(0.0, 0.28, h));
    c = lerp(c, s.zenith, smoothstep(0.22, 0.75, h));
    // The sunset core sits on the horizon ahead and fades round the sky.
    float core = pow(toward, 6.0) * exp(-max(h, 0.0) * 9.0);
    c = lerp(c, s.sunset, saturate(core * 1.3));
    c += s.horizon * 0.35 * pow(toward, 2.0) * exp(-abs(h) * 14.0);
    // Below the horizon: the haze over the city floor.
    c = lerp(c, s.ground, smoothstep(0.0, -0.12, h));
    c = DuskSkyline(s, atan2(d.x, d.z), h, c);
    if (stars)
    {
        // Sparse stars high up.
        float3 cell = floor(d * 220.0);
        c += step(0.9965, DuskHash(cell)) * smoothstep(0.35, 0.8, h) * 0.55;
    }
    return c * s.exposure;
}

#endif
