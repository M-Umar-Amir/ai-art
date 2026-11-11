using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Downtown street blocks to the PDF cover (client, 11 Nov 2026: "improve street design and buildings design ...
    /// as close to pdf style and high quality"; "branding shops should be detailed with their respective interior
    /// visible"). Each 20 m block side is:
    ///   - a two-storey podium: a ground-floor shop whose interior is modelled and lit behind an open glazed front,
    ///     under a dark fascia band (logos), a canopy and a blade sign;
    ///   - a tower above it, one of four kinds: glass curtain-wall tower with cantilevered planted terraces,
    ///     stepped residential terraces with roof gardens, ribbon-window office with fins, or a punched stone block;
    ///   - every tower has neon edge lines and a roofline feature; some carry a holographic screen or a brand board;
    ///   - a second, taller tower row behind (x 28-36 m), so the street reads as a canyon of a big city.
    /// One brand shop per block, alternating sides, in the KFC > Taco Bell > Pizza Hut > The Habit rotation; the other
    /// side is an invented cafe/arcade/mart with a lighter interior. Facades are world-mapped atlas boxes (WindowAtlas),
    /// so a tower costs a handful of parts; everything static is merged by TrackSpawner.Combine.
    public static class DowntownArchitecture
    {
        public const float Front = 8.5f, Depth = 14f, Podium = 6.4f, Floor = 3.2f;

        static int Hash(int a, int b)
        {
            unchecked
            {
                int h = a * 73856093 ^ b * 19349663;
                h ^= h >> 13; h *= 1274126177; h ^= h >> 16;
                return h & 0x7fffffff;
            }
        }

        static GameObject P(Transform parent, Vector3 at, Vector3 size, Material m, PrimitiveType type = PrimitiveType.Cube) => DashCity.Part(parent, at, size, m, type);

        static readonly Color[] GlassTints =
        {
            new(1f, 1f, 1f), new(.86f, .95f, 1.08f), new(1.05f, .9f, 1.06f), new(.9f, 1.04f, 1f), new(1.02f, .96f, .9f),
        };

        public static void Build(TrackSpawner track, Transform parent, float side, float z)
        {
            int block = Mathf.FloorToInt(z / 20f);
            int h = Hash(block, side > 0 ? 7 : 3);
            float face = side * Front;
            bool brandShop = ((block + (side > 0 ? 1 : 0)) & 1) == 0;
            int brand = TrackSpawner.BrandAt(block);

            var stone = DashCity.Mat(track, "podiumStone", new Color(.36f, .33f, .4f));
            var graphite = DashCity.Mat(track, "terraceGraphite", new Color(.045f, .06f, .08f));
            var paving = track.sidewalkMaterial;

            // Pavement fill up to the shopfronts, and the podium mass behind the shop and above it.
            P(parent, new Vector3(side * (Front - 0.8f), 0.1f, z + 10f), new Vector3(1.6f, 0.2f, 19.6f), paving);
            P(parent, new Vector3(face + side * (4.3f + (Depth - 4.3f) * 0.5f), 2.25f, z + 10f), new Vector3(Depth - 4.3f, 4.5f, 19.4f), stone);
            P(parent, new Vector3(face + side * Depth * 0.5f, 5.45f, z + 10f), new Vector3(Depth, 1.9f, 19.4f), graphite);
            // Stone piers frame the shop at both ends of the block, with a dark recess between neighbours.
            foreach (float pz in new[] { z + 0.75f, z + 19.25f })
                P(parent, new Vector3(face + side * 2.15f, 2.25f, pz), new Vector3(4.3f, 4.5f, 0.9f), stone);

            Shopfront(track, parent, side, z, face, brandShop ? brand : -1, h);

            int kind = h % 6;
            float top = kind switch
            {
                0 or 3 or 5 => GlassTower(track, parent, side, z, face, h),
                1 => Terraced(track, parent, side, z, face, h),
                2 => RibbonOffice(track, parent, side, z, face, h),
                _ => Punched(track, parent, side, z, face, h),
            };
            Screen(track, parent, side, z, face, h, block, brandShop, top);
            MidTower(track, parent, side, z, h);
        }

        // ------------------------------------------------------------------ towers

        static Material Neon(TrackSpawner track, int h) => (h & 1) == 0
            ? DashCity.Mat(track, "roofNeonCyan", new Color(0.1f, 0.85f, 1f), 2.2f)
            : DashCity.Mat(track, "roofNeonPink", new Color(1f, 0.1f, 0.55f), 2.2f);

        /// A facade body (atlas material) from y0 to y1, set back `back` m from the street line, with roof slab,
        /// a lit parapet line and neon corner strips. Returns the roof height.
        static float Body(TrackSpawner track, Transform parent, float side, float z, float face, float back, float depth, float y0, float y1, float z0, float z1, Material facade, Material neon, bool corners = true)
        {
            var roof = DashCity.Mat(track, "roofSlab", new Color(.16f, .15f, .2f));
            float len = z1 - z0, mid = (z0 + z1) * 0.5f, cx = face + side * (back + depth * 0.5f);
            P(parent, new Vector3(cx, (y0 + y1) * 0.5f, mid), new Vector3(depth, y1 - y0, len), facade);
            P(parent, new Vector3(cx, y1 + 0.18f, mid), new Vector3(depth + 0.3f, 0.36f, len + 0.3f), roof);
            P(parent, new Vector3(face + side * (back - 0.16f), y1 + 0.38f, mid), new Vector3(0.06f, 0.06f, len + 0.3f), neon);
            if (corners)
                foreach (float cz in new[] { z0 - 0.02f, z1 + 0.02f })
                    P(parent, new Vector3(face + side * (back - 0.03f), (y0 + y1) * 0.5f, cz), new Vector3(0.07f, y1 - y0, 0.07f), neon);
            return y1 + 0.36f;
        }

        static float GlassTower(TrackSpawner track, Transform parent, float side, float z, float face, int h)
        {
            int floors = 8 + h / 7 % 9;
            float back = (h / 3 & 1) == 0 ? 0f : 1.2f;
            float y1 = Podium + floors * Floor;
            var facade = WindowAtlas.Facade(track, WindowAtlas.Kind.Curtain, GlassTints[h / 11 % GlassTints.Length]);
            var neon = Neon(track, h);
            float top = Body(track, parent, side, z, face, back, 13f, Podium, y1, z + 0.6f, z + 19.4f, facade, neon);
            // Crown: a narrower lit top storey set back from the street, sometimes an antenna mast.
            var crown = WindowAtlas.Facade(track, WindowAtlas.Kind.Ribbon, new Color(.75f, .8f, .95f), 2.2f);
            Body(track, parent, side, z, face, back + 3f, 7f, top, top + 4.2f, z + 4f, z + 16f, crown, neon, false);
            if (h % 4 == 0)
            {
                var steel = DashCity.Mat(track, "roofSteel", new Color(0.16f, 0.18f, 0.21f));
                P(parent, new Vector3(face + side * (back + 6f), top + 4.2f + 4f, z + 10f), new Vector3(0.18f, 8f, 0.18f), steel);
                P(parent, new Vector3(face + side * (back + 6f), top + 12.3f, z + 10f), Vector3.one * 0.3f, DashCity.Mat(track, "beaconRed", new Color(1f, .15f, .1f), 3f));
            }
            // Cantilevered planted terraces on two floors, as on the cover's left-hand towers.
            foreach (int k in new[] { 2 + h % 3, 6 + h % 3 })
                if (k < floors - 1) Terrace(track, parent, side, z, face, back, Podium + k * Floor, h + k);
            return top;
        }

        static float Terraced(TrackSpawner track, Transform parent, float side, float z, float face, int h)
        {
            var facade = WindowAtlas.Facade(track, (h & 1) == 0 ? WindowAtlas.Kind.PunchedCream : WindowAtlas.Kind.PunchedTerracotta, Color.white, 1.4f);
            var neon = Neon(track, h);
            float y = Podium, back = 0f, top = y;
            int tiers = 3;
            for (int t = 0; t < tiers; t++)
            {
                int floors = 3 + (h >> (t + 2)) % 2;
                float y1 = y + floors * Floor;
                top = Body(track, parent, side, z, face, back, Depth - back, y, y1, z + 0.6f, z + 19.4f, facade, neon, t == 0);
                if (t < tiers - 1) RoofGarden(track, parent, side, z, face, back, top, 3f, h + t);
                y = top; back += 3f;
            }
            return top;
        }

        static float RibbonOffice(TrackSpawner track, Transform parent, float side, float z, float face, int h)
        {
            int floors = 6 + h / 5 % 7;
            float y1 = Podium + floors * Floor;
            var facade = WindowAtlas.Facade(track, WindowAtlas.Kind.Ribbon, GlassTints[h / 13 % GlassTints.Length], 1.6f);
            var neon = Neon(track, h);
            float top = Body(track, parent, side, z, face, 0.4f, Depth - 0.4f, Podium, y1, z + 0.6f, z + 19.4f, facade, neon);
            // Vertical fins give the elevation depth and catch the low sun.
            var fin = DashCity.Mat(track, "officeFin", new Color(.2f, .2f, .26f));
            for (float fz = z + 1.4f; fz < z + 19f; fz += 3.2f)
                P(parent, new Vector3(face + side * 0.2f, (Podium + y1) * 0.5f, fz), new Vector3(0.4f, y1 - Podium, 0.16f), fin);
            // An LED band round the top floor.
            P(parent, new Vector3(face + side * 0.36f, y1 - 0.5f, z + 10f), new Vector3(0.06f, 0.5f, 18.8f), neon);
            return top;
        }

        static float Punched(TrackSpawner track, Transform parent, float side, float z, float face, int h)
        {
            int floors = 4 + h / 3 % 5;
            float y1 = Podium + floors * Floor;
            var facade = WindowAtlas.Facade(track, (h & 2) == 0 ? WindowAtlas.Kind.PunchedTerracotta : WindowAtlas.Kind.PunchedCream, Color.white, 1.4f);
            var neon = Neon(track, h);
            float top = Body(track, parent, side, z, face, 0f, Depth, Podium, y1, z + 0.6f, z + 19.4f, facade, neon);
            // Balconies with glass rails on alternate floors and bays.
            var slab = DashCity.Mat(track, "balconySlab", new Color(.72f, .7f, .68f));
            var rail = DashCity.Mat(track, "terraceGlass", new Color(.45f, .7f, .85f), .25f);
            for (int f = 1; f < floors; f += 2)
                for (int b = (f / 2) % 2; b < 6; b += 2)
                {
                    float by = Podium + f * Floor, bz = z + 1.6f + b * 3.2f;
                    P(parent, new Vector3(face - side * 0.6f, by, bz), new Vector3(1.2f, 0.16f, 2.6f), slab);
                    P(parent, new Vector3(face - side * 1.18f, by + 0.55f, bz), new Vector3(0.04f, 0.95f, 2.6f), rail);
                }
            return top;
        }

        /// A cantilevered terrace off the tower face: slab, glass balustrade with a lit rail, planters, sometimes a
        /// small tree and a person looking out over the street.
        static void Terrace(TrackSpawner track, Transform parent, float side, float z, float face, float back, float y, int h)
        {
            var slab = DashCity.Mat(track, "terraceGraphite", new Color(.045f, .06f, .08f));
            var glass = DashCity.Mat(track, "terraceGlass", new Color(.45f, .7f, .85f), .25f);
            var cyan = DashCity.Mat(track, "architecturalCyan", new Color(.05f, .55f, .9f), 2f);
            var soil = DashCity.Mat(track, "planterBox", new Color(.22f, .2f, .24f));
            float len = 12f, mid = z + 4f + (h % 3) * 2f + len * 0.5f - 2f;
            float edge = face + side * (back - 2.3f);
            P(parent, new Vector3(face + side * (back - 1.15f), y, mid), new Vector3(2.3f, 0.28f, len), slab);
            P(parent, new Vector3(edge, y + 0.62f, mid), new Vector3(0.04f, 1.0f, len), glass);
            P(parent, new Vector3(edge, y + 1.14f, mid), new Vector3(0.06f, 0.05f, len), cyan);
            for (int i = 0; i < 3; i++)
            {
                float pz = mid - len * 0.35f + i * len * 0.35f;
                P(parent, new Vector3(face + side * (back - 0.5f), y + 0.42f, pz), new Vector3(0.8f, 0.55f, 1.8f), soil);
                P(parent, new Vector3(face + side * (back - 0.5f), y + 0.85f, pz), new Vector3(0.9f, 0.6f, 1.9f), track.foliageMaterial, PrimitiveType.Sphere);
            }
            if (h % 3 == 0 && track.kitTrees != null && track.kitTrees.Length > 0)
                Tree(track, parent, new Vector3(face + side * (back - 0.6f), y + 0.14f, mid + len * 0.3f), 0.42f, h);
            if (h % 2 == 0 && PlayerProfile.Graphics > 0)
                DistrictLife.Instance?.Occupant(parent, new Vector3(face + side * (back - 1.6f), y + 0.14f, mid - 1.5f), side > 0 ? -90f : 90f, (h & 4) != 0, h);
        }

        /// A planted roof garden on a terrace step: balustrade, planters, a tree, a cafe umbrella and table.
        static void RoofGarden(TrackSpawner track, Transform parent, float side, float z, float face, float back, float y, float width, int h)
        {
            var glass = DashCity.Mat(track, "terraceGlass", new Color(.45f, .7f, .85f), .25f);
            var warm = DashCity.Mat(track, "terraceWarm", new Color(1f, .7f, .4f), 1.6f);
            var soil = DashCity.Mat(track, "planterBox", new Color(.22f, .2f, .24f));
            var canvas = DashCity.Mat(track, "umbrella" + (h % 3), h % 3 == 0 ? new Color(.95f, .45f, .35f) : h % 3 == 1 ? new Color(.95f, .9f, .82f) : new Color(.3f, .62f, .7f));
            var steel = DashCity.Mat(track, "roofSteel", new Color(0.16f, 0.18f, 0.21f));
            float edge = face + side * (back + 0.1f);
            P(parent, new Vector3(edge, y + 0.5f, z + 10f), new Vector3(0.04f, 0.95f, 18.6f), glass);
            P(parent, new Vector3(edge, y + 0.99f, z + 10f), new Vector3(0.06f, 0.05f, 18.6f), warm);
            for (int i = 0; i < 4; i++)
            {
                float pz = z + 2.5f + i * 5f;
                P(parent, new Vector3(face + side * (back + 0.6f), y + 0.3f, pz), new Vector3(0.8f, 0.6f, 2.2f), soil);
                P(parent, new Vector3(face + side * (back + 0.6f), y + 0.75f, pz), new Vector3(0.9f, 0.7f, 2.3f), track.foliageMaterial, PrimitiveType.Sphere);
            }
            Tree(track, parent, new Vector3(face + side * (back + width * 0.6f), y, z + 4f + (h % 4) * 3f), 0.5f, h);
            float uz = z + 13f - (h % 3) * 2f, ux = face + side * (back + width * 0.55f);
            P(parent, new Vector3(ux, y + 1.2f, uz), new Vector3(0.06f, 2.4f, 0.06f), steel);
            P(parent, new Vector3(ux, y + 2.35f, uz), new Vector3(2.4f, 0.12f, 2.4f), canvas, PrimitiveType.Cylinder);
            P(parent, new Vector3(ux, y + 0.75f, uz), new Vector3(0.9f, 0.04f, 0.9f), steel, PrimitiveType.Cylinder);
            if (PlayerProfile.Graphics > 0)
                DistrictLife.Instance?.Occupant(parent, new Vector3(ux - side * 0.7f, y, uz - 0.4f), side > 0 ? -60f : 60f, true, h + 3);
        }

        static void Tree(TrackSpawner track, Transform parent, Vector3 at, float scale, int h)
        {
            if (track.kitTrees == null || track.kitTrees.Length == 0) return;
            var prefab = track.kitTrees[h % track.kitTrees.Length];
            if (!prefab) return;
            var tree = Object.Instantiate(prefab, at, Quaternion.Euler(0f, h % 360, 0f), parent);
            tree.name = "Terrace tree";
            tree.transform.localScale = Vector3.one * scale;
        }

        /// The taller second row behind the street buildings, visible above their roofs.
        static void MidTower(TrackSpawner track, Transform parent, float side, float z, int h)
        {
            float x = side * (29f + h % 7);
            float height = 42f + (h >> 4) % 56;
            float width = 12f + (h >> 2) % 6, len = 14f + (h >> 6) % 5;
            var kind = (h >> 5) % 3 == 0 ? WindowAtlas.Kind.Ribbon : WindowAtlas.Kind.Curtain;
            var facade = WindowAtlas.Facade(track, kind, GlassTints[(h >> 3) % GlassTints.Length], 1.3f);
            var neon = Neon(track, h >> 1);
            float mid = z + 10f;
            P(parent, new Vector3(x, height * 0.5f, mid), new Vector3(width, height, len), facade);
            P(parent, new Vector3(x, height + 0.2f, mid), new Vector3(width + 0.3f, 0.4f, len + 0.3f), DashCity.Mat(track, "roofSlab", new Color(.16f, .15f, .2f)));
            foreach (float cz in new[] { mid - len * 0.5f - 0.03f, mid + len * 0.5f + 0.03f })
                P(parent, new Vector3(x - side * width * 0.5f, height * 0.5f, cz), new Vector3(0.12f, height, 0.12f), neon);
            // Stepped crown with a spire on some.
            P(parent, new Vector3(x, height + 3f, mid), new Vector3(width * 0.6f, 6f, len * 0.6f), facade);
            P(parent, new Vector3(x - side * width * 0.3f, height + 6.2f, mid), new Vector3(0.1f, 0.12f, len * 0.6f), neon);
            if ((h >> 7) % 3 == 0)
                P(parent, new Vector3(x, height + 6f + 7f, mid), new Vector3(0.3f, 14f, 0.3f), neon);
            // A big holographic screen on some, angled down the street.
            if ((h >> 9) % 3 == 0 && track.hologramMaterial && track.hologramTextures != null && track.hologramTextures.Length > 0)
            {
                var screen = new GameObject("Tower hologram").transform;
                screen.SetParent(parent, false);
                screen.position = new Vector3(x - side * (width * 0.5f + 1f), height * 0.55f, mid - len * 0.2f);
                screen.rotation = Quaternion.Euler(0f, side > 0 ? 40f : -40f, 0f); // face the rider (not mirrored)
                Hologram(track, screen, (h >> 11) % track.hologramTextures.Length, 11f, 6.2f);
            }
        }

        // ------------------------------------------------------------------ screens

        static readonly System.Collections.Generic.Dictionary<int, Material> holograms = new();

        static void Hologram(TrackSpawner track, Transform screen, int texture, float w, float hgt)
        {
            if (!holograms.TryGetValue(texture, out var m) || !m)
            {
                m = new Material(track.hologramMaterial) { name = "Hologram " + texture };
                m.SetTexture("_BaseMap", track.hologramTextures[texture]);
                m.SetColor("_BaseColor", new Color(.85f, 1f, 1.1f, 1f));
                holograms[texture] = m;
            }
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(quad.GetComponent<Collider>());
            quad.name = "Hologram image";
            quad.transform.SetParent(screen, false);
            quad.transform.localPosition = Vector3.zero; quad.transform.localRotation = Quaternion.identity;
            quad.transform.localScale = new Vector3(w, hgt, 1f);
            var r = quad.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var cyan = DashCity.Mat(track, "roofNeonCyan", new Color(0.1f, 0.85f, 1f), 2.2f);
            foreach (float ry in new[] { -hgt * 0.5f - 0.08f, hgt * 0.5f + 0.08f })
            {
                var trim = DashCity.Part(screen, Vector3.zero, new Vector3(w + 0.3f, 0.07f, 0.07f), cyan);
                trim.transform.localPosition = new Vector3(0f, ry, 0f); trim.transform.localRotation = Quaternion.identity;
            }
        }

        /// On the street tower: a brand board on the non-brand side (so a logo is always ahead), otherwise a hologram.
        static void Screen(TrackSpawner track, Transform parent, float side, float z, float face, int h, int block, bool brandShop, float top)
        {
            if (top < 16f) return;
            var graphite = DashCity.Mat(track, "terraceGraphite", new Color(.045f, .06f, .08f));
            var screen = new GameObject("Street tower screen").transform;
            screen.SetParent(parent, false);
            screen.position = new Vector3(face - side * 1.6f, 12.5f + (h % 3), z + 10f);
            // The face (-Z of the quad) must point back down the street at the rider and in toward the road:
            // yaw 55 on the right, -55 on the left (the old ±125 showed every board its black back).
            screen.rotation = Quaternion.Euler(0f, side > 0 ? 55f : -55f, 0f);
            if (!brandShop)
            {
                var housing = DashCity.Part(screen, Vector3.zero, new Vector3(6.5f, 4.3f, 0.28f), graphite);
                housing.transform.localPosition = new Vector3(0f, 0f, 0.15f); housing.transform.localRotation = Quaternion.identity;
                int index = TrackSpawner.BrandAt(block + 1);
                BrandVisibility.Face(track, screen, index, "board", screen.TransformPoint(new Vector3(0, 0, -.01f)), screen.rotation, 6.1f, 3.85f);
                var trimMat = Neon(track, h);
                foreach (float ry in new[] { -2.1f, 2.1f })
                {
                    var trim = DashCity.Part(screen, Vector3.zero, new Vector3(6.5f, .05f, .08f), trimMat);
                    trim.transform.localPosition = new Vector3(0, ry, -.02f); trim.transform.localRotation = Quaternion.identity;
                }
            }
            else if (track.hologramMaterial && track.hologramTextures != null && track.hologramTextures.Length > 0)
                Hologram(track, screen, h % track.hologramTextures.Length, 6.4f, 3.6f);
        }

        // ------------------------------------------------------------------ shops

        struct Style
        {
            public Color wall, accent, floor, counter, canopy, menu;
            public bool stripes, wood;
            public string name;
        }

        static Style StyleFor(int brand, int h)
        {
            switch (brand)
            {
                case 0: return new Style { name = "KFC", wall = new(.78f, .07f, .11f), accent = new(.96f, .95f, .93f), floor = new(.78f, .76f, .74f), counter = new(.78f, .07f, .11f), canopy = new(.82f, .07f, .12f), menu = new(.16f, .03f, .05f), stripes = true };
                case 1: return new Style { name = "TACO BELL", wall = new(.34f, .11f, .48f), accent = new(.92f, .22f, .62f), floor = new(.2f, .17f, .25f), counter = new(.42f, .14f, .58f), canopy = new(.4f, .12f, .55f), menu = new(.1f, .03f, .16f) };
                case 2: return new Style { name = "PIZZA HUT", wall = new(.66f, .1f, .08f), accent = new(.95f, .75f, .3f), floor = new(.46f, .29f, .18f), counter = new(.5f, .3f, .17f), canopy = new(.75f, .1f, .08f), menu = new(.14f, .04f, .03f), wood = true };
                case 3: return new Style { name = "THE HABIT", wall = new(.12f, .12f, .13f), accent = new(.3f, .62f, .38f), floor = new(.36f, .33f, .3f), counter = new(.5f, .34f, .2f), canopy = new(.14f, .14f, .15f), menu = new(.05f, .05f, .06f), wood = true };
            }
            return (h % 4) switch
            {
                0 => new Style { name = "CLOUD CAFE", wall = new(.86f, .82f, .74f), accent = new(.2f, .65f, .62f), floor = new(.5f, .36f, .24f), counter = new(.45f, .3f, .2f), canopy = new(.2f, .5f, .5f), menu = new(.1f, .12f, .12f), wood = true },
                1 => new Style { name = "BYTE ARCADE", wall = new(.08f, .08f, .18f), accent = new(.2f, .9f, 1f), floor = new(.12f, .1f, .2f), counter = new(.12f, .12f, .26f), canopy = new(.1f, .1f, .22f), menu = new(.3f, .05f, .35f) },
                2 => new Style { name = "DASH MART", wall = new(.92f, .93f, .94f), accent = new(.25f, .7f, .35f), floor = new(.82f, .82f, .8f), counter = new(.85f, .86f, .88f), canopy = new(.22f, .55f, .3f), menu = new(.08f, .2f, .1f) },
                _ => new Style { name = "NEON NOODLE", wall = new(.55f, .22f, .1f), accent = new(1f, .55f, .2f), floor = new(.3f, .22f, .18f), counter = new(.35f, .2f, .12f), canopy = new(.6f, .2f, .08f), menu = new(.2f, .06f, .02f), wood = true },
            };
        }

        /// The ground-floor shop: an open glazed front onto a real, lit interior (counter, tills, menu boards,
        /// kitchen pass, high tables, pendant lights, staff and customers), canopy, fascia and blade sign.
        static void Shopfront(TrackSpawner track, Transform parent, float side, float z, float face, int brand, int h)
        {
            var s = StyleFor(brand, h);
            string k = brand >= 0 ? "brand" + brand : "shop" + (h % 4);
            Vector3 In(float depth, float y, float pz) => new(face + side * depth, y, pz);
            var wall = DashCity.Mat(track, k + "Wall", s.wall);
            var accent = DashCity.Mat(track, k + "Accent", s.accent, brand == 1 || brand < 0 && h % 4 == 1 ? 0.9f : 0f);
            var floor = DashCity.Mat(track, k + "Floor", s.floor);
            var counter = DashCity.Mat(track, k + "Counter", s.counter);
            var canopy = DashCity.Mat(track, k + "Canopy", s.canopy);
            var menu = DashCity.Mat(track, k + "Menu", s.menu, 0.6f);
            var steel = DashCity.Mat(track, "shopSteel", new Color(.72f, .74f, .78f));
            steel.SetFloat("_Metallic", .8f); steel.SetFloat("_Smoothness", .7f);
            var frame = DashCity.Mat(track, "shopFrame", new Color(.06f, .065f, .08f));
            var ceiling = DashCity.Mat(track, "shopCeiling", new Color(.82f, .8f, .78f), .12f);
            var panel = DashCity.Mat(track, "shopLightPanel", new Color(1f, .93f, .82f), 2.2f);
            var pendant = DashCity.Mat(track, "shopPendant", new Color(1f, .72f, .4f), 2.6f);
            var hot = DashCity.Mat(track, "shopHotLamp", new Color(1f, .55f, .2f), 1.8f);
            var fridge = DashCity.Mat(track, "shopFridge", new Color(.6f, .85f, 1f), 1.1f);
            var menuLine = DashCity.Mat(track, "shopMenuLine", new Color(1f, .96f, .88f), 1.4f);
            var photo = DashCity.Mat(track, "shopMenuPhoto", new Color(1f, .62f, .25f), 1.2f);
            var spill = DashCity.Mat(track, "shopSpill", new Color(1f, .78f, .5f), .35f);

            // Shell: floor, ceiling with light panels, back wall.
            P(parent, In(2.15f, 0.21f, z + 10f), new Vector3(4.3f, 0.04f, 17.6f), floor);
            P(parent, In(2.15f, 4.3f, z + 10f), new Vector3(4.3f, 0.12f, 17.6f), ceiling);
            // Recessed ceiling spotlights in two rows.
            for (int row = 0; row < 2; row++)
                for (float pz = z + 2.2f; pz < z + 18.5f; pz += 2.2f)
                    P(parent, In(0.9f + row * 1.9f, 4.23f, pz), new Vector3(0.22f, 0.02f, 0.22f), panel, PrimitiveType.Cylinder);
            P(parent, In(4.2f, 2.25f, z + 10f), new Vector3(0.1f, 4.1f, 17.6f), wall);
            if (s.stripes) // KFC's red-and-white stripes behind the counter
                for (int i = 0; i < 7; i++)
                    P(parent, In(4.13f, 2.25f, z + 4.8f + i * 1.0f), new Vector3(0.03f, 4.1f, 0.48f), accent);
            if (s.wood) // timber slat feature wall
                for (int i = 0; i < 9; i++)
                    P(parent, In(4.12f, 2.25f, z + 13.2f + i * 0.5f), new Vector3(0.05f, 4.1f, 0.22f), counter);

            // Menu boards over the counter and the brand logo on the back wall.
            foreach (float mz in new[] { z + 7.6f, z + 10f, z + 12.4f })
            {
                P(parent, In(4.12f, 3.3f, mz), new Vector3(0.06f, 1.0f, 2.2f), menu);
                P(parent, In(4.08f, 3.55f, mz - 0.45f), new Vector3(0.02f, 0.38f, 0.9f), photo);
                for (int l = 0; l < 3; l++)
                    P(parent, In(4.08f, 3.08f + l * 0.16f, mz + 0.55f), new Vector3(0.02f, 0.05f, 0.85f), menuLine);
            }
            var along = Quaternion.Euler(0f, side > 0 ? 90f : -90f, 0f);
            if (brand >= 0)
                BrandVisibility.Face(track, parent, brand, "wide", In(4.08f, 2.05f, z + 16.2f), along, 2.4f, 0f);

            // Counter, tills, drinks fridge, the kitchen pass with heat lamps.
            P(parent, In(3.0f, 0.75f, z + 10f), new Vector3(0.7f, 1.08f, 8.8f), counter);
            P(parent, In(3.0f, 1.32f, z + 10f), new Vector3(0.82f, 0.06f, 9f), steel);
            P(parent, In(2.64f, 0.6f, z + 10f), new Vector3(0.03f, 0.18f, 8.8f), accent);
            foreach (float tz in new[] { z + 8f, z + 12f })
                P(parent, In(3.0f, 1.5f, tz), new Vector3(0.3f, 0.26f, 0.38f), frame);
            P(parent, In(3.75f, 1.1f, z + 3.2f), new Vector3(0.7f, 1.8f, 1.6f), fridge);
            P(parent, In(4.14f, 1.7f, z + 10f), new Vector3(0.05f, 0.5f, 3.6f), hot);

            // High tables with pendant lights near the glass.
            var tableZ = new[] { z + 3.2f, z + 16.6f, z + 14.4f };
            for (int t = 0; t < tableZ.Length; t++)
            {
                float tz = tableZ[t];
                float tx = t == 2 ? 0.95f : 1.3f;
                P(parent, In(tx, 1.06f, tz), new Vector3(0.85f, 0.04f, 0.85f), s.wood ? counter : steel, PrimitiveType.Cylinder);
                P(parent, In(tx, 0.63f, tz), new Vector3(0.08f, 0.42f, 0.08f), frame, PrimitiveType.Cylinder);
                P(parent, In(tx, 3.75f, tz), new Vector3(0.02f, 1.1f, 0.02f), frame);
                P(parent, In(tx, 3.18f, tz), new Vector3(0.34f, 0.2f, 0.34f), pendant, PrimitiveType.Sphere);
                // Two bar stools at each high table.
                foreach (float sz in new[] { -0.62f, 0.62f })
                {
                    P(parent, In(tx, 0.78f, tz + sz), new Vector3(0.36f, 0.05f, 0.36f), s.wood ? counter : accent, PrimitiveType.Cylinder);
                    P(parent, In(tx, 0.5f, tz + sz), new Vector3(0.05f, 0.28f, 0.05f), frame, PrimitiveType.Cylinder);
                }
            }

            // Glazed front: slim mullions, transom, kick plates either side of an open door.
            for (float mz = z + 1.6f; mz < z + 19f; mz += 2.4f)
                if (Mathf.Abs(mz - (z + 10f)) > 1.1f) P(parent, In(0.03f, 2.25f, mz), new Vector3(0.07f, 4.1f, 0.07f), frame);
            P(parent, In(0.03f, 3.45f, z + 10f), new Vector3(0.08f, 0.07f, 17.4f), frame);
            foreach (float kz in new[] { z + 5f, z + 15f })
                P(parent, In(0.05f, 0.42f, kz), new Vector3(0.1f, 0.4f, 8f), frame);
            foreach (float dz in new[] { z + 8.95f, z + 11.05f })
                P(parent, In(0.05f, 1.75f, dz), new Vector3(0.14f, 3.1f, 0.14f), frame);
            P(parent, In(0.05f, 3.3f, z + 10f), new Vector3(0.14f, 0.14f, 2.2f), frame);

            // Canopy over the pavement with a warm light strip underneath, and the light spilling onto the paving.
            float canopyY = 4.62f;
            if (brand == 2)
            {
                // Pizza Hut's red roof form: two pitched planes.
                foreach (float tilt in new[] { -1f, 1f })
                {
                    var roofPart = P(parent, new Vector3(face - side * 0.9f, canopyY + 0.25f, z + 10f + tilt * 0.01f), new Vector3(1.9f, 0.14f, 17f), canopy);
                    roofPart.transform.rotation = Quaternion.Euler(0f, 0f, side * (tilt > 0 ? 14f : 14f));
                }
            }
            else
            {
                // A striped awning, sloping down to the street (KFC red/white, The Habit charcoal/green, others in
                // their own colours), in 1.4 m bands.
                var stripeB = brand == 0 ? DashCity.Mat(track, "awningWhite", new Color(.97f, .95f, .92f)) : brand == 3 ? DashCity.Mat(track, "awningGreen", new Color(.3f, .6f, .38f)) : accent;
                for (int band = 0; band < 12; band++)
                {
                    float az = z + 1.6f + band * 1.4f + 0.7f;
                    var strip = P(parent, new Vector3(face - side * 0.85f, canopyY, az), new Vector3(1.8f, 0.12f, 1.4f), (band & 1) == 0 ? canopy : stripeB);
                    strip.transform.rotation = Quaternion.Euler(0f, 0f, side * -14f);
                }
                P(parent, new Vector3(face - side * 1.72f, canopyY - 0.32f, z + 10f), new Vector3(0.04f, 0.26f, 16.8f), canopy);
            }
            P(parent, new Vector3(face - side * 1.66f, canopyY - 0.12f, z + 10f), new Vector3(0.06f, 0.06f, 17f), pendant);
            P(parent, new Vector3(face - side * 1.2f, 0.205f, z + 10f), new Vector3(2.2f, 0.01f, 15f), spill);

            // Fascia (the podium's dark band): logos for a brand shop, the shop's own lettering otherwise; and a
            // blade sign standing out from the facade, readable from far down the street.
            float bx = face - side * 1.45f, by = 7.3f, bz = z + 2.2f;
            P(parent, new Vector3(face - side * .45f, by + 1.3f, bz), new Vector3(1.1f, .14f, .14f), frame);
            P(parent, new Vector3(bx, by, bz), new Vector3(2.3f, 2.3f, .2f), frame);
            if (brand >= 0)
            {
                foreach (float wz in new[] { z + 5.5f, z + 14.5f })
                    BrandVisibility.Face(track, parent, brand, "wide", new Vector3(face - side * .03f, 5.45f, wz), along, 3.0f, 0f);
                BrandVisibility.Face(track, parent, brand, "square", new Vector3(bx, by, bz - .105f), Quaternion.identity, 2.18f, 2.18f);
                BrandVisibility.Face(track, parent, brand, "square", new Vector3(bx, by, bz + .105f), Quaternion.Euler(0f, 180f, 0f), 2.18f, 2.18f);
            }
            else
            {
                var signFace = DashCity.Mat(track, k + "Sign", s.canopy, 0.5f);
                P(parent, new Vector3(bx, by, bz - .11f), new Vector3(2.15f, 2.15f, .02f), signFace);
                DashCity.Text(track, parent, s.name.Replace(" ", "\n"), new Vector3(bx, by, bz - .13f), 0.07f, Color.white);
                P(parent, new Vector3(face - side * .03f, 5.45f, z + 10f), new Vector3(0.04f, 0.5f, 9f), accent);
            }

            // Pavement details: a menu A-board, and a courier waiting at the pickup window of a brand shop.
            P(parent, new Vector3(face - side * 1.05f, 0.65f, z + 3.4f), new Vector3(0.62f, 0.9f, 0.08f), canopy);
            P(parent, new Vector3(face - side * 1.05f, 0.72f, z + 3.35f), new Vector3(0.5f, 0.62f, 0.02f), menuLine);

            // People: staff behind the counter, customers at the tables and in the queue (fewer on Low).
            var life = DistrictLife.Instance;
            if (!life) return;
            float faceStreet = side > 0 ? -90f : 90f, faceBack = side > 0 ? 90f : -90f;
            life.Occupant(parent, In(3.55f, 0.23f, z + 9.2f), faceStreet, false, h);
            if (PlayerProfile.Graphics == 0) return;
            life.Occupant(parent, In(1.3f, 0.23f, z + 2.6f), 0f, true, h + 1);
            if (brand >= 0)
            {
                life.Occupant(parent, In(1.3f, 0.23f, z + 3.8f), 180f, true, h + 2);
                life.Occupant(parent, In(2.15f, 0.23f, z + 10.6f), faceBack, false, h + 4);
                life.Occupant(parent, In(0.95f, 0.23f, z + 15.1f), 200f, true, h + 5);
                life.Occupant(parent, new Vector3(face - side * 1.9f, 0.2f, z + 17.2f), faceBack, false, h + 6);
            }
        }
    }
}
