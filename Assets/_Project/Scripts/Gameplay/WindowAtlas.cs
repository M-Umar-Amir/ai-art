using System.Collections.Generic;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Procedural facade textures for the Downtown towers (11 Nov 2026, PDF cover: glass towers lit from inside).
    /// One tile is 4 bays x 4 floors of 3.2 m (12.8 m square), mapped in world space by FoodInc/ToonCurved
    /// (_WorldTextureScale), so a whole facade is one box with one material instead of hundreds of window cubes.
    /// Each kind has an albedo map (glass, frames, stone, spandrels) and an emission map that holds only the lit
    /// windows (_EmissionMap, _EmissionMapOn), so stone and frames never glow. Built once per session, lazily.
    public static class WindowAtlas
    {
        /// Curtain..PunchedTerracotta are generated here; Loft..BuffPlain are the brick atlases from
        /// models/kfc-street/make_brick_facades.py (Bishop Arts red and buff brick, Distillery warehouse brick), set on
        /// TrackSpawner by the bootstrap, with the same 12.8 m tile of 4 bays x 4 floors.
        /// Condo and Mies (25 Nov 2026, downtown Toronto): the city's glass condo towers (teal window wall, white slab
        /// edges, frosted balcony glass) and TD Centre-style black steel with bronze glass, generated here too.
        public enum Kind { Curtain = 0, Ribbon = 1, PunchedCream = 2, PunchedTerracotta = 3, Loft = 4, Warehouse = 5, Buff = 6, LoftPlain = 7, WarehousePlain = 8, BuffPlain = 9, Condo = 10, Mies = 11 }
        static bool IsBrick(Kind k) => k >= Kind.Loft && k <= Kind.BuffPlain;
        /// The same brick without windows (shop piers, party walls).
        public static Kind Plain(Kind k) => k switch { Kind.Loft => Kind.LoftPlain, Kind.Warehouse => Kind.WarehousePlain, Kind.Buff => Kind.BuffPlain, _ => k };
        public const float Period = 12.8f;
        const int Size = 512, Cell = Size / 4;

        static readonly Texture2D[] albedo = new Texture2D[12], emission = new Texture2D[12];
        static readonly Dictionary<string, Material> mats = new();

        /// A facade material of `kind`, tinted (glass hue variants) and with `glow` window brightness.
        public static Material Facade(TrackSpawner track, Kind kind, Color tint, float glow = 1.5f)
        {
            string key = $"{(int)kind}-{ColorUtility.ToHtmlStringRGB(tint)}-{glow:0.0}";
            if (mats.TryGetValue(key, out var m) && m) return m;
            if (IsBrick(kind)) return mats[key] = Brick(track, kind, tint, glow);
            int k = (int)kind;
            if (!albedo[k]) Generate(kind);
            m = new Material(track.tintMaterial) { name = "Facade atlas " + kind + " " + key };
            m.SetTexture("_BaseMap", albedo[k]);
            m.SetTextureScale("_BaseMap", Vector2.one); m.SetTextureOffset("_BaseMap", Vector2.zero);
            m.SetColor("_BaseColor", tint);
            m.SetTexture("_EmissionMap", emission[k]);
            m.SetFloat("_EmissionMapOn", 1f);
            m.SetColor("_EmissionColor", Color.white * glow);
            m.SetFloat("_EmissionUsesAlbedo", 0f);
            m.SetFloat("_WorldTextureScale", 1f / Period);
            m.SetFloat("_SurfaceStyle", 1f); m.SetFloat("_RimStrength", 0f);
            bool glass = kind == Kind.Curtain || kind == Kind.Condo || kind == Kind.Mies;
            m.SetFloat("_Smoothness", glass ? 0.86f : kind == Kind.Ribbon ? 0.62f : 0.28f);
            m.SetFloat("_Metallic", glass ? 0.5f : kind == Kind.Ribbon ? 0.3f : 0.04f);
            m.SetTexture("_BumpMap", null); m.SetFloat("_BumpScale", 0f);
            m.SetTexture("_RoughnessMap", null); m.SetFloat("_RoughnessStrength", 0f);
            m.SetFloat("_Wetness", 0f); m.SetFloat("_WindowInterior", 0f);
            m.EnableKeyword("_EMISSION");
            return mats[key] = m;
        }

        /// A brick atlas material: albedo, brick relief (normal map) and, for the windowed kinds, the lit rooms.
        static Material Brick(TrackSpawner track, Kind kind, Color tint, float glow)
        {
            int i = kind - Kind.Loft;
            Texture2D At(Texture2D[] set) => set != null && i < set.Length ? set[i] : null;
            var albedoMap = At(track.facadeAlbedo);
            if (!albedoMap) // missing art: keep the street standing on the generated brick
                return Facade(track, kind == Kind.Buff || kind == Kind.BuffPlain ? Kind.PunchedCream : Kind.PunchedTerracotta, tint, glow);
            var m = new Material(track.tintMaterial) { name = "Brick facade " + kind };
            m.SetTexture("_BaseMap", albedoMap);
            m.SetTextureScale("_BaseMap", Vector2.one); m.SetTextureOffset("_BaseMap", Vector2.zero);
            m.SetColor("_BaseColor", tint);
            var lit = At(track.facadeEmission);
            m.SetTexture("_EmissionMap", lit);
            m.SetFloat("_EmissionMapOn", lit ? 1f : 0f);
            m.SetColor("_EmissionColor", lit ? Color.white * glow : Color.black);
            m.SetFloat("_EmissionUsesAlbedo", 0f);
            m.SetFloat("_WorldTextureScale", 1f / Period);
            m.SetFloat("_SurfaceStyle", 1f); m.SetFloat("_RimStrength", 0f);
            m.SetFloat("_Smoothness", 0.2f); m.SetFloat("_Metallic", 0f);
            var relief = At(track.facadeNormal);
            m.SetTexture("_BumpMap", relief); m.SetFloat("_BumpScale", relief ? 0.9f : 0f);
            m.SetTexture("_RoughnessMap", null); m.SetFloat("_RoughnessStrength", 0f);
            m.SetFloat("_Wetness", 0f); m.SetFloat("_WindowInterior", 0f);
            if (lit) m.EnableKeyword("_EMISSION");
            return m;
        }

        static Color32 C(float r, float g, float b) => new((byte)(Mathf.Clamp01(r) * 255f), (byte)(Mathf.Clamp01(g) * 255f), (byte)(Mathf.Clamp01(b) * 255f), 255);

        static void Generate(Kind kind)
        {
            var a = new Color32[Size * Size];
            var e = new Color32[Size * Size];
            var rng = new System.Random(4211 + (int)kind * 977);
            for (int fy = 0; fy < 4; fy++)
                for (int bx = 0; bx < 4; bx++)
                {
                    // Per-window state: lit or not, warm or cool, how bright, blinds, an interior divider.
                    double litChance = kind switch { Kind.Curtain => 0.42, Kind.Ribbon => 0.55, Kind.Condo => 0.5, Kind.Mies => 0.38, _ => 0.4 };
                    bool lit = rng.NextDouble() < litChance;
                    bool warm = rng.NextDouble() < kind switch { Kind.Curtain or Kind.Ribbon => 0.45, Kind.Mies => 0.25, _ => 0.82 };
                    // Condo bays: some have a balcony (frosted glass rail), some an opaque spandrel panel up top.
                    bool balcony = kind == Kind.Condo && rng.NextDouble() < 0.55;
                    bool panel = kind == Kind.Condo && !balcony && rng.NextDouble() < 0.35;
                    float level = 0.55f + (float)rng.NextDouble() * 0.45f;
                    float blinds = rng.NextDouble() < 0.3 ? 0.35f + (float)rng.NextDouble() * 0.4f : 0f;
                    int divider = rng.Next(30, 98);
                    var lamp = warm ? new Vector3(1f, 0.68f, 0.38f) : new Vector3(0.62f, 0.86f, 1f);
                    for (int y = 0; y < Cell; y++)
                        for (int x = 0; x < Cell; x++)
                        {
                            int px = bx * Cell + x, py = fy * Cell + y, i = py * Size + px;
                            float u = x / (float)Cell, v = y / (float)Cell;
                            Color32 albedoPx; Vector3 glow = Vector3.zero;
                            bool inGlass;
                            float gx = u, gy = v; // position inside the glass, 0..1
                            float frost = 0f; // condo balcony glass in front of the window wall
                            switch (kind)
                            {
                                case Kind.Curtain:
                                {
                                    bool mullion = x < 4 || x >= Cell - 3;
                                    bool spandrel = y < 16;
                                    inGlass = !mullion && !spandrel;
                                    gy = (y - 16) / (float)(Cell - 16);
                                    albedoPx = mullion ? C(.05f, .055f, .075f) : spandrel ? C(.09f, .085f, .13f) : default;
                                    break;
                                }
                                case Kind.Ribbon:
                                {
                                    bool spandrel = y < 46;
                                    bool mullion = !spandrel && (x % 32 < 3);
                                    inGlass = !spandrel && !mullion;
                                    gy = (y - 46) / (float)(Cell - 46);
                                    float line = spandrel && (y == 44 || y == 45 || y == 2 || y == 3) ? 0.6f : 1f;
                                    albedoPx = spandrel ? C(.8f * line, .8f * line, .84f * line) : mullion ? C(.12f, .13f, .16f) : default;
                                    break;
                                }
                                case Kind.Condo:
                                {
                                    // Toronto condo window wall: white slab edge, frosted balcony rail on some bays,
                                    // floor-to-ceiling teal glass with slim mullions every 1.6 m.
                                    bool slab = y < 12;
                                    bool mullion = !slab && (x % 64 < 3);
                                    bool top = panel && y >= 104;
                                    inGlass = !slab && !mullion && !top;
                                    gy = (y - 12) / (float)(Cell - 12);
                                    if (balcony && y >= 12 && y < 46) frost = 0.55f;
                                    albedoPx = slab ? C(.86f, .87f, .87f) : mullion ? C(.2f, .22f, .23f) : top ? C(.16f, .19f, .2f) : default;
                                    break;
                                }
                                case Kind.Mies:
                                {
                                    // TD Centre: black steel I-beam mullions every 1.6 m, black spandrels, bronze glass.
                                    bool spandrel = y < 26;
                                    bool mullion = x % 64 < 6;
                                    inGlass = !spandrel && !mullion;
                                    gy = (y - 26) / (float)(Cell - 26);
                                    float edge = (mullion && x % 64 == 5) || (spandrel && y == 25) ? 2.2f : 1f;
                                    albedoPx = mullion || spandrel ? C(.032f * edge, .032f * edge, .036f * edge) : default;
                                    break;
                                }
                                default:
                                {
                                    bool cream = kind == Kind.PunchedCream;
                                    var wall = cream ? new Vector3(.8f, .74f, .64f) : new Vector3(.62f, .35f, .26f);
                                    bool opening = x >= 28 && x < 100 && y >= 26 && y < 112;
                                    bool sill = !opening && x >= 22 && x < 106 && y >= 18 && y < 26;
                                    bool reveal = opening && (x < 34 || y >= 106);
                                    inGlass = opening && !reveal;
                                    gx = (x - 34) / 66f; gy = (y - 26) / 80f;
                                    // A little brick/stone course texture so the wall is not flat paint.
                                    float course = (y % 16 == 0) || ((x + (y / 16 % 2) * 16) % 32 == 0) ? 0.86f : 1f;
                                    float grain = 0.95f + 0.05f * Mathf.Sin(px * 0.37f + py * 0.91f);
                                    albedoPx = sill ? C(.88f, .85f, .78f) : reveal ? C(wall.x * .45f, wall.y * .45f, wall.z * .48f)
                                        : opening ? default : C(wall.x * course * grain, wall.y * course * grain, wall.z * course * grain);
                                    break;
                                }
                            }
                            if (inGlass)
                            {
                                // Dusk glass: violet-blue reflection, lighter towards the top, with a diagonal sheen.
                                float sheen = Mathf.Repeat(px + py * 0.6f, 160f) < 14f ? 0.08f : 0f;
                                var glass = kind switch
                                {
                                    Kind.Condo => Vector3.Lerp(new Vector3(.07f, .15f, .16f), new Vector3(.26f, .36f, .4f), gy), // teal-green
                                    Kind.Mies => Vector3.Lerp(new Vector3(.06f, .05f, .045f), new Vector3(.17f, .14f, .12f), gy), // bronze
                                    _ => Vector3.Lerp(new Vector3(.08f, .1f, .2f), new Vector3(.3f, .24f, .46f), gy),
                                } + Vector3.one * sheen;
                                if (lit)
                                {
                                    // Lit room: bright ceiling strip, mid room, darker desks/furniture low down, a divider.
                                    float room = gy > 0.86f ? 1.6f : gy < 0.32f ? 0.32f : 0.85f;
                                    if (Mathf.Abs(gx * Cell - divider) < 2f && gy < 0.8f) room *= 0.4f;
                                    if (blinds > 0f && gy > 1f - blinds && (y / 5) % 2 == 0) room *= 0.45f;
                                    glow = lamp * level * room;
                                    glass = Vector3.Lerp(glass, new Vector3(.32f, .26f, .2f), 0.6f);
                                }
                                if (frost > 0f)
                                {
                                    glass = Vector3.Lerp(glass, new Vector3(.62f, .7f, .72f), frost);
                                    glow *= 0.35f;
                                }
                                albedoPx = C(glass.x, glass.y, glass.z);
                            }
                            a[i] = albedoPx;
                            e[i] = C(glow.x, glow.y, glow.z);
                        }
                }
            albedo[(int)kind] = Make(a, "Facade albedo " + kind);
            emission[(int)kind] = Make(e, "Facade emission " + kind);
        }

        static Texture2D Make(Color32[] pixels, string name)
        {
            var t = new Texture2D(Size, Size, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            t.SetPixels32(pixels);
            t.Apply(true, true); // mips, then free the CPU copy
            return t;
        }
    }
}
