using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DallasDash.EditorTools
{
    /// Textures every material in the project. Colours stay authoritative: each map is a near-white
    /// grayscale detail that ToonCurved multiplies by _BaseColor. Maps are generated procedurally,
    /// tileable (periodic value noise + seams), and only materials without a _BaseMap are touched,
    /// so hand-authored and baked maps are never overwritten.
    ///   -executeMethod DallasDash.EditorTools.TextureKit.Run
    public static class TextureKit
    {
        const string Folder = "Assets/_Project/Art/Textures/Materials";
        const int Size = 256;
        const string Version = "3";

        static readonly string[] Kinds =
        {
            "fabric", "denim", "skin", "hair", "metal", "plastic", "led", "grip", "circuit",
            "bun", "sauce", "lettuce", "cheese", "concrete", "brick", "panel", "glass", "asphalt",
            "rubber", "wood", "soil", "leaf", "cardboard", "paint", "stone", "tile", "glow"
        };

        static string AbsFolder => Path.Combine(Application.dataPath, "_Project/Art/Textures/Materials");

        // Material-name tokens, most specific first. A token matches if the material name contains it.
        static readonly (string token, string kind, float tiling)[] Rules =
        {
            ("aceskin", "skin", 1f), ("novaskin", "skin", 1f), ("boltskin", "skin", 1f),
            ("skin", "skin", 1f),
            ("hair", "hair", 1f),
            ("eye", "glass", 1f), ("lens", "glass", 1f), ("visor", "glass", 1f),
            ("shoe", "rubber", 2f), ("sole", "rubber", 2.5f),
            ("pants", "denim", 2.2f), ("denim", "denim", 2.2f),
            ("jacket", "fabric", 1.8f), ("accent", "fabric", 1.8f), ("hivis", "fabric", 1.8f),
            ("fabric", "fabric", 1.8f),
            ("silver", "metal", 2f), ("graphite", "metal", 2f), ("steel", "metal", 2f),
            ("metal", "metal", 2f), ("trim", "metal", 2f), ("frame", "metal", 2f),
            ("charcoal", "metal", 2f), ("door", "metal", 2f), ("post", "metal", 2f),
            ("boltcyan", "led", 1f), ("cyan", "led", 1f), ("lime", "led", 1f),
            ("magenta", "led", 1f), ("amber", "led", 1f), ("led", "led", 1f),
            ("bulb", "led", 1f), ("lamp", "led", 1f), ("halo", "led", 1f),
            ("neon", "led", 2f),
            ("brand", "paint", 1.5f), ("tint", "paint", 1.5f), ("paint", "paint", 1.5f),
            ("awning", "fabric", 1.5f), ("sign", "paint", 1.5f),
            ("foodgold", "bun", 1f), ("bun", "bun", 1f),
            ("foodcream", "cheese", 1f), ("cheese", "cheese", 1f),
            ("foodgreen", "lettuce", 1f), ("lettuce", "lettuce", 1f),
            ("foodred", "sauce", 1f), ("sauce", "sauce", 1f),
            ("glass", "glass", 2f),
            ("leaf", "leaf", 1.5f), ("foliage", "leaf", 1.5f), ("soil", "soil", 2f),
            ("brick", "brick", 2f), ("concrete", "concrete", 2f), ("stone", "stone", 2f),
            ("plaza", "stone", 2f), ("tile", "tile", 2f), ("curb", "concrete", 2f),
            ("wood", "wood", 2f), ("timber", "wood", 2f), ("bark", "wood", 2f), ("crate", "wood", 2f),
            ("kraft", "cardboard", 2f), ("cardboard", "cardboard", 2f),
            ("asphalt", "asphalt", 2f), ("road", "asphalt", 2f), ("rubber", "rubber", 2.5f),
            ("circuit", "circuit", 1f), ("grip", "grip", 3f),
            ("panel", "panel", 2f), ("window", "glass", 2f), ("skyline", "panel", 2f),
            ("facade", "panel", 2f), ("plastic", "plastic", 1f), ("propink", "plastic", 1.5f)
        };

        public static void Run()
        {
            Apply();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// Assigns missing base maps. Called by the bootstrap as well, so every rebuild keeps textures.
        public static void Apply()
        {
            Directory.CreateDirectory(AbsFolder);
            var textures = LoadOrBuild();

            int assigned = 0, kept = 0, skipped = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Project" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!mat) continue;
                // Shaders without a base map (sky, text, particles) are intentionally untextured.
                if (!mat.HasProperty("_BaseMap")) { skipped++; continue; }
                if (mat.GetTexture("_BaseMap")) { kept++; continue; }

                var pick = Pick(mat.name.ToLowerInvariant());
                mat.SetTexture("_BaseMap", textures[pick.kind]);
                mat.SetTextureScale("_BaseMap", new Vector2(pick.tiling, pick.tiling));
                EditorUtility.SetDirty(mat);
                assigned++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[TextureKit] assigned=" + assigned + " kept=" + kept + " noBaseMapProperty=" + skipped +
                      " textures=" + textures.Count);
        }

        static Dictionary<string, Texture2D> LoadOrBuild()
        {
            var marker = Path.Combine(AbsFolder, "texturekit.version");
            bool current = File.Exists(marker) && File.ReadAllText(marker).Trim() == Version;
            var map = new Dictionary<string, Texture2D>();
            if (current)
            {
                foreach (var kind in Kinds)
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/" + kind + ".png");
                    if (!tex) { current = false; break; }
                    map[kind] = tex;
                }
            }
            if (current) return map;

            map.Clear();
            foreach (var kind in Kinds) map[kind] = Build(kind);
            File.WriteAllText(marker, Version);
            AssetDatabase.ImportAsset(Folder + "/texturekit.version", ImportAssetOptions.ForceUpdate);
            return map;
        }

        static (string kind, float tiling) Pick(string n)
        {
            // Per-slot model families: alternate surface kinds so a model does not read as one flat block.
            if (n.StartsWith("neon-block-"))
            {
                int i = Index(n);
                return ((i % 5 == 0 ? "glass" : i % 3 == 1 ? "concrete" : "panel"), 2f);
            }
            if (n.StartsWith("neon-hurdle-")) return ("metal", 2f);
            if (n.StartsWith("rooftop-apartment-"))
            {
                int i = Index(n);
                return ((i % 4 == 0 ? "glass" : i % 2 == 0 ? "brick" : "concrete"), 2f);
            }
            if (n.StartsWith("skybridge-"))
            {
                int i = Index(n);
                return ((i % 4 == 3 ? "glass" : i % 2 == 0 ? "panel" : "concrete"), 2f);
            }
            if (n.StartsWith("skypark-tower-"))
            {
                int i = Index(n);
                return ((i % 3 == 0 ? "glass" : "panel"), 2f);
            }
            if (n.StartsWith("electric-board-") || n.StartsWith("glide-board-"))
                return (Index(n) % 2 == 0 ? "circuit" : "metal", 1f);
            if (n.StartsWith("electric-pod-")) return (Index(n) % 3 == 0 ? "circuit" : "metal", 1f);
            if (n.StartsWith("cargo-slide-gate-")) return ("metal", 2f);
            if (n.StartsWith("bolt-")) return (Index(n) % 6 == 0 ? "led" : "metal", 2f);
            if (n.EndsWith("-brand")) return ("paint", 1.5f);

            foreach (var rule in Rules)
                if (n.Contains(rule.token)) return (rule.kind, rule.tiling);

            if (n.Contains("white") || n.Contains("orange")) return ("plastic", 1.5f);
            return ("concrete", 2f);
        }

        static int Index(string n)
        {
            int dash = n.LastIndexOf('-');
            return dash >= 0 && int.TryParse(n.Substring(dash + 1), out var i) ? i : 0;
        }

        // ---------------- procedural, tileable maps ----------------

        static Texture2D Build(string kind)
        {
            var path = Path.Combine(AbsFolder, kind + ".png");
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var rng = new System.Random(StableHash(kind));
            float[,] broad = Fractal(rng, 6), mid = Fractal(rng, 24), fine = Fractal(rng, 96);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float n = broad[x, y], m = mid[x, y], f = fine[x, y], v;
                    switch (kind)
                    {
                        case "fabric":
                            v = 0.94f + ((((x / 2) + (y / 2)) & 1) == 0 ? 0.025f : -0.02f) + (n - 0.5f) * 0.05f + (f - 0.5f) * 0.02f;
                            break;
                        case "denim":
                            v = 0.91f + (((x + y) % 4 == 0) ? -0.03f : 0.012f) + (n - 0.5f) * 0.05f + (f - 0.5f) * 0.03f;
                            break;
                        case "skin":
                            v = 0.975f + (m - 0.5f) * 0.015f + (f - 0.5f) * 0.012f;
                            break;
                        case "hair":
                            v = 0.86f + Mathf.Sin(x * 0.9f + n * 6f) * 0.05f + (f - 0.5f) * 0.06f;
                            break;
                        case "metal":
                            v = 0.94f + Mathf.Sin(y * 1.7f + n * 3f) * 0.018f + (f - 0.5f) * 0.025f;
                            break;
                        case "plastic":
                            v = 0.985f + (m - 0.5f) * 0.012f;
                            break;
                        case "led":
                            v = 0.98f - ((x % 32 < 2 || y % 32 < 2) ? 0.07f : 0f) + (f - 0.5f) * 0.015f;
                            break;
                        case "grip":
                            v = 0.8f + (f > 0.76f ? 0.13f : 0f) + (n - 0.5f) * 0.06f + (m - 0.5f) * 0.04f;
                            break;
                        case "circuit":
                            v = 0.95f - ((x % 48 < 2 || y % 64 < 2) ? 0.24f : 0f)
                                     + ((x % 48 < 6 && y % 64 < 6) ? 0.05f : 0f) + (f - 0.5f) * 0.015f;
                            break;
                        case "bun":
                            v = 0.96f - (f > 0.945f ? 0.11f : 0f) + (m - 0.5f) * 0.03f;
                            break;
                        case "sauce":
                            v = 0.93f + (n - 0.5f) * 0.1f + (f > 0.86f ? -0.05f : 0f) + (m - 0.5f) * 0.04f;
                            break;
                        case "lettuce":
                            v = 0.88f + Mathf.Sin((x + y) * 0.25f + n * 7f) * 0.05f + (f - 0.5f) * 0.1f;
                            break;
                        case "cheese":
                            v = 0.97f - (f > 0.965f ? 0.12f : 0f) + (m - 0.5f) * 0.02f;
                            break;
                        case "concrete":
                            v = 0.9f + (n - 0.5f) * 0.1f + (m - 0.5f) * 0.05f + (f > 0.972f ? -0.07f : 0f);
                            break;
                        case "brick":
                            v = (y % 32 < 3 || (x + ((y / 32) % 2) * 24) % 48 < 3)
                                ? 0.78f : 0.93f + (n - 0.5f) * 0.09f + (f - 0.5f) * 0.04f;
                            break;
                        case "panel":
                            v = (x % 64 < 2 || y % 64 < 2) ? 0.82f
                                : ((x % 64 < 7 && y % 64 < 7) ? 0.86f : 0.96f + (n - 0.5f) * 0.035f + (f - 0.5f) * 0.015f);
                            break;
                        case "glass":
                            v = 0.97f + Mathf.Sin((x + y) * 0.05f) * 0.015f + (m - 0.5f) * 0.012f;
                            break;
                        case "asphalt":
                            v = 0.74f + (n - 0.5f) * 0.16f + (m - 0.5f) * 0.1f + (f - 0.5f) * 0.12f;
                            break;
                        case "rubber":
                            v = 0.82f + (f - 0.5f) * 0.09f + (m - 0.5f) * 0.06f;
                            break;
                        case "wood":
                            v = 0.89f + Mathf.Sin(x * 0.32f + n * 8f) * 0.06f + (f - 0.5f) * 0.03f;
                            break;
                        case "soil":
                            v = 0.72f + (n - 0.5f) * 0.16f + (m - 0.5f) * 0.1f + (f > 0.9f ? 0.06f : 0f);
                            break;
                        case "leaf":
                            v = 0.85f + Mathf.Sin(x * 0.6f + y * 0.2f + n * 8f) * 0.06f + (f - 0.5f) * 0.06f;
                            break;
                        case "cardboard":
                            v = 0.9f + (y % 8 < 1 ? -0.045f : 0.01f) + (n - 0.5f) * 0.02f;
                            break;
                        case "paint":
                            v = 0.975f + (m - 0.5f) * 0.015f + (f - 0.5f) * 0.01f;
                            break;
                        case "stone":
                            v = 0.88f + (n - 0.5f) * 0.14f + (m - 0.5f) * 0.05f + (f - 0.5f) * 0.04f;
                            break;
                        case "tile":
                            v = (x % 32 < 2 || y % 32 < 2) ? 0.86f : 0.95f + (n - 0.5f) * 0.04f;
                            break;
                        default: // glow
                            float dx = (x - Size * 0.5f) / (Size * 0.5f), dy = (y - Size * 0.5f) / (Size * 0.5f);
                            v = 1f - 0.04f * (dx * dx + dy * dy);
                            break;
                    }
                    v = Mathf.Clamp01(v);
                    tex.SetPixel(x, y, new Color(v, v, v, 1f));
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(Folder + "/" + kind + ".png", ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(Folder + "/" + kind + ".png") as TextureImporter;
            if (importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = 512;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/" + kind + ".png");
        }

        /// Periodic value noise so every map tiles without a seam.
        static float[,] Fractal(System.Random rng, int cells)
        {
            var result = new float[Size, Size];
            float amplitude = 1f, total = 0f;
            for (int octave = 0; octave < 3; octave++)
            {
                int grid = cells << octave;
                var lattice = new float[grid, grid];
                for (int y = 0; y < grid; y++)
                    for (int x = 0; x < grid; x++)
                        lattice[x, y] = (float)rng.NextDouble();
                for (int y = 0; y < Size; y++)
                {
                    for (int x = 0; x < Size; x++)
                    {
                        float fx = (float)x / Size * grid, fy = (float)y / Size * grid;
                        int x0 = (int)fx % grid, y0 = (int)fy % grid;
                        int x1 = (x0 + 1) % grid, y1 = (y0 + 1) % grid;
                        float tx = fx - (int)fx, ty = fy - (int)fy;
                        tx = tx * tx * (3f - 2f * tx);
                        ty = ty * ty * (3f - 2f * ty);
                        float a = Mathf.Lerp(lattice[x0, y0], lattice[x1, y0], tx);
                        float b = Mathf.Lerp(lattice[x0, y1], lattice[x1, y1], tx);
                        result[x, y] += Mathf.Lerp(a, b, ty) * amplitude;
                    }
                }
                total += amplitude;
                amplitude *= 0.5f;
            }
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    result[x, y] /= total;
            return result;
        }

        static int StableHash(string s)
        {
            unchecked
            {
                uint h = 2166136261;
                foreach (var c in s) h = (h ^ c) * 16777619;
                return (int)(h & 0x7fffffff);
            }
        }
    }
}
