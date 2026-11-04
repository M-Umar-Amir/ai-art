using System.Collections.Generic;
using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// The Festivals district as a snowy Christmas street (user, 3 Nov: "make it snowy, snowman and more
    /// Christmas hurdles, decoration related to Christmas"), built from real, properly modelled objects instead
    /// of primitive blobs: decorated firs with snow-tipped tiers and a star, wooden market chalets with snowy
    /// pitched roofs and eave lights, string lights and garlands with wreaths across the street, a MERRY
    /// CHRISTMAS truss arch, candy canes, snow on the pavements and kerbs. The hurdles are ChristmasHurdles.
    ///
    /// Meshes are built once and shared; the streamed segment combiner batches the instances by material.
    /// Clearances: nothing below 3 m inside |x| < 3.6 (the lanes), nothing inside the wall-run envelope
    /// (|x| 6.0-8.6, y 4.4-6.2), overhead lines above 8.4 m.
    public static class FestivalDecor
    {
        internal static readonly Color Red = new(.72f, .06f, .07f), Green = new(.06f, .3f, .14f), Gold = new(1f, .78f, .3f),
            Snow = new(.93f, .95f, 1f), Warm = new(1f, .82f, .55f);

        static Mesh bulb, firTier, star;

        /// A small glass bulb for string lights (lathe, 17 cm).
        internal static Mesh Bulb()
        {
            if (bulb) return bulb;
            bulb = Lathe("Christmas light bulb", new[] { (0f, .03f), (.025f, .045f), (.06f, .05f), (.11f, .035f), (.15f, .012f), (.17f, 0f) }, 12);
            return bulb;
        }

        /// One drooping, jagged fir tier, 1 m radius, 1 m tall: a cone whose skirt zig-zags like branch tips.
        internal static Mesh FirTier()
        {
            if (firTier) return firTier;
            const int around = 24;
            var v = new List<Vector3> { Vector3.up };
            for (int a = 0; a < around; a++)
            {
                float ang = a * Mathf.PI * 2f / around;
                float tip = (a & 1) == 0 ? 1f : .82f; // branch tips and the gaps between them
                v.Add(new Vector3(Mathf.Cos(ang) * .55f * tip, .45f, Mathf.Sin(ang) * .55f * tip));
                v.Add(new Vector3(Mathf.Cos(ang) * tip, (a & 1) == 0 ? -.05f : .06f, Mathf.Sin(ang) * tip));
            }
            v.Add(Vector3.up * .05f);
            var t = new List<int>();
            for (int a = 0; a < around; a++)
            {
                int m0 = 1 + a * 2, o0 = m0 + 1, m1 = 1 + (a + 1) % around * 2, o1 = m1 + 1;
                t.AddRange(new[] { 0, m1, m0, m0, m1, o1, m0, o1, o0, v.Count - 1, o0, o1 });
            }
            firTier = new Mesh { name = "Fir tier" };
            firTier.SetVertices(v); firTier.SetTriangles(t, 0); firTier.RecalculateNormals(); firTier.RecalculateBounds();
            return firTier;
        }

        /// An extruded five-point star, 0.5 m across, 8 cm deep.
        internal static Mesh Star()
        {
            if (star) return star;
            var v = new List<Vector3>(); var t = new List<int>();
            for (int face = 0; face < 2; face++)
            {
                float zf = face == 0 ? -.04f : .04f;
                v.Add(new Vector3(0, 0, zf));
                for (int i = 0; i < 10; i++)
                {
                    float a = Mathf.PI / 2f + i * Mathf.PI / 5f, r = (i & 1) == 0 ? .25f : .1f;
                    v.Add(new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, zf));
                }
            }
            for (int i = 0; i < 10; i++)
            {
                int a = 1 + i, b = 1 + (i + 1) % 10;
                t.AddRange(new[] { 0, b, a });
                t.AddRange(new[] { 11, 11 + a, 11 + b });
                t.AddRange(new[] { a, b, 11 + b, a, 11 + b, 11 + a });
            }
            star = new Mesh { name = "Tree star" };
            star.SetVertices(v); star.SetTriangles(t, 0); star.RecalculateNormals(); star.RecalculateBounds();
            return star;
        }

        /// Revolves a (y, radius) profile round the y axis.
        internal static Mesh Lathe(string name, (float y, float r)[] profile, int around)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            foreach (var p in profile)
                for (int a = 0; a < around; a++)
                {
                    float ang = a * Mathf.PI * 2f / around;
                    v.Add(new Vector3(Mathf.Cos(ang) * p.r, p.y, Mathf.Sin(ang) * p.r));
                }
            for (int r = 0; r < profile.Length - 1; r++)
                for (int a = 0; a < around; a++)
                {
                    int i0 = r * around + a, i1 = r * around + (a + 1) % around, i2 = i0 + around, i3 = i1 + around;
                    t.AddRange(new[] { i0, i2, i1, i1, i2, i3 });
                }
            var m = new Mesh { name = name };
            m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        internal static GameObject Mesh(Transform parent, Mesh mesh, Vector3 at, Quaternion rotation, Vector3 scale, Material material, string name)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(at, rotation);
            go.transform.localScale = scale;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        internal static GameObject Box(Transform parent, Vector3 at, Vector3 size, Material material, PrimitiveType type = PrimitiveType.Cube) => DashCity.Part(parent, at, size, material, type);

        /// A thin member from a to b (truss chords, cables, candy-cane segments).
        internal static void Member(Transform parent, Vector3 a, Vector3 b, float thickness, Material material)
        {
            var go = Box(parent, (a + b) * .5f, new Vector3(thickness, thickness, Vector3.Distance(a, b)), material);
            go.transform.rotation = Quaternion.LookRotation(b - a, Mathf.Abs(Vector3.Dot((b - a).normalized, Vector3.up)) > .99f ? Vector3.forward : Vector3.up);
        }

        internal static Material M(TrackSpawner track, string key, Color c, float glow = 0f, float smooth = .42f, float metal = .08f)
        {
            var m = DashCity.Mat(track, key, c, glow);
            m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
            return m;
        }

        internal static Material[] Lights(TrackSpawner track) => new[] {
            M(track, "xmasBulbRed", new Color(1f, .12f, .1f), 3f), M(track, "xmasBulbGreen", new Color(.15f, 1f, .3f), 3f),
            M(track, "xmasBulbGold", Gold, 3f), M(track, "xmasBulbWarm", Warm, 3f), M(track, "xmasBulbBlue", new Color(.25f, .5f, 1f), 3f) };

        public static void Build(TrackSpawner track, Transform parent, float z)
        {
            int block = Mathf.FloorToInt(z / track.segmentLength);
            var snow = M(track, "xmasSnow", Snow, .05f, .55f, 0f);
            var pine = M(track, "xmasPine", Green, 0f, .2f, 0f);
            var wire = M(track, "xmasWire", new Color(.05f, .07f, .05f));
            var red = M(track, "xmasRed", Red, .05f, .5f);
            var gold = M(track, "xmasGold", Gold, .6f, .8f, .8f);
            var lights = Lights(track);

            // Snow: a thick layer over both pavements, banks heaped against the kerbs, and darker slush ruts worn
            // into the packed-snow road along each lane (where the traffic runs), so the street reads as snowbound.
            var slush = M(track, "xmasSlush", new Color(.5f, .54f, .62f), 0f, .75f, 0f);
            foreach (float side in new[] { -1f, 1f })
            {
                if (side > 0 && RooftopCourse.OwnsRightSide(z + 10f)) continue;
                Box(parent, new Vector3(side * 6.3f, .07f, z + 10f), new Vector3(4.6f, .16f, 20f), snow);
                for (int k = 0; k < 4; k++)
                    Box(parent, new Vector3(side * 4.2f, .12f, z + 2.5f + k * 5f), new Vector3(.9f, .42f, 4.6f), snow, PrimitiveType.Sphere).name = "Kerb snowbank";
            }
            foreach (float lane in Tuning.LaneX)
                foreach (float rut in new[] { -.36f, .36f })
                    Box(parent, new Vector3(lane + rut, .004f, z + 10f), new Vector3(.26f, .006f, 20f), slush).name = "Slush rut";

            // String lights across the street, and a garland hung with wreaths further on.
            Catenary(parent, z + 6f, 9.4f, .8f, wire, (at, i) =>
                Mesh(parent, Bulb(), at + Vector3.down * .2f, Quaternion.Euler(180f, 0, 0), Vector3.one, lights[i % lights.Length], "String light bulb"), 24);
            Catenary(parent, z + 15f, 10.2f, .6f, pine, (at, i) =>
            {
                if (i % 3 == 1) Wreath(parent, at + Vector3.down * .55f, .38f, pine, red, lights[3]);
            }, 9, .09f);

            foreach (float side in new[] { -1f, 1f })
            {
                float center = z + 10f;
                bool kiosk = side > 0 && FuelStops.KioskNear(center, 12f);
                bool roofCourse = side > 0 && RooftopCourse.OwnsRightSide(center);
                if (kiosk || roofCourse || TrackSpawner.NearJunction(center)) continue;
                if (block % 2 == 0) Tree(parent, new Vector3(side * 5.2f, 0f, z + 5f), 4.6f, pine, snow, red, gold, lights);
                else Chalet(track, parent, side, center, block, snow, red, lights[3]);
                CandyCane(track, parent, new Vector3(side * 4.45f, 0f, z + 16.5f), side);
            }

            // Every fourth block a MERRY CHRISTMAS arch spans the course.
            if (block % 4 == 2 && !TrackSpawner.NearJunction(z + 10f) && !RooftopCourse.OwnsRightSide(z + 10f))
                Arch(track, parent, z + 10f, pine, red, snow, lights);
        }

        /// Places `count` items along a cable hung between the two facades at height y, sagging by `sag`.
        static void Catenary(Transform parent, float z, float y, float sag, Material cable, System.Action<Vector3, int> each, int count, float thick = .02f)
        {
            const float span = 8.3f; // facade to facade, clear of the 8.5 m building line
            Vector3 Point(float k) => new(Mathf.Lerp(-span, span, k), y - Mathf.Sin(k * Mathf.PI) * sag, z);
            const int pieces = 12;
            for (int i = 0; i < pieces; i++) Member(parent, Point(i / (float)pieces), Point((i + 1) / (float)pieces), thick, cable);
            for (int i = 0; i < count; i++) each(Point((i + .5f) / count), i);
        }

        /// A wreath: a ring of fir bunches with a red bow and a few lights, facing the rider.
        internal static void Wreath(Transform parent, Vector3 at, float radius, Material pine, Material red, Material light)
        {
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6f;
                var p = at + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0);
                Mesh(parent, FirTier(), p, Quaternion.Euler(-90f, 0, a * Mathf.Rad2Deg), new Vector3(.16f, .12f, .16f) * (radius / .38f + .3f) / 1.3f, pine, "Wreath bunch");
                if (i % 3 == 0) Mesh(parent, Bulb(), p + Vector3.back * .08f, Quaternion.identity, Vector3.one * .6f, light, "Wreath light");
            }
            foreach (float s in new[] { -1f, 1f })
            {
                var loop = Box(parent, at + new Vector3(s * .11f, -radius, -.06f), new Vector3(.18f, .1f, .04f), red);
                loop.transform.rotation = Quaternion.Euler(0, 0, s * 25f);
                Box(parent, at + new Vector3(s * .06f, -radius - .17f, -.05f), new Vector3(.05f, .24f, .02f), red).transform.rotation = Quaternion.Euler(0, 0, s * 12f);
            }
        }

        /// A decorated fir: five snow-tipped tiers, baubles, a spiral of lights and a star on top.
        internal static void Tree(Transform parent, Vector3 basePos, float height, Material pine, Material snow, Material red, Material gold, Material[] lights)
        {
            var trunk = DashCity.Part(parent, basePos + Vector3.up * .3f, new Vector3(.22f, .3f, .22f), pine, PrimitiveType.Cylinder);
            trunk.name = "Fir trunk";
            const int tiers = 5;
            for (int k = 0; k < tiers; k++)
            {
                float f = k / (float)(tiers - 1);
                float r = Mathf.Lerp(1.05f, .3f, f) * height / 4.6f, y = .45f + f * (height - 1.3f), th = Mathf.Lerp(1.1f, .7f, f) * height / 4.6f;
                Mesh(parent, FirTier(), basePos + Vector3.up * y, Quaternion.Euler(0, k * 17f, 0), new Vector3(r, th, r), pine, "Fir tier");
                Mesh(parent, FirTier(), basePos + Vector3.up * (y + th * .22f), Quaternion.Euler(0, k * 17f + 7f, 0), new Vector3(r * .78f, th * .35f, r * .78f), snow, "Snow on the branches");
                for (int b = 0; b < 4; b++)
                {
                    float a = b * Mathf.PI / 2f + k * .8f;
                    var p = basePos + new Vector3(Mathf.Cos(a) * r * .86f, y + .02f, Mathf.Sin(a) * r * .86f);
                    Box(parent, p, Vector3.one * .13f * height / 4.6f, b % 2 == 0 ? red : gold, PrimitiveType.Sphere).name = "Bauble";
                }
            }
            for (int i = 0; i < 26; i++)
            {
                float f = i / 26f, a = f * Mathf.PI * 7f;
                float r = Mathf.Lerp(1.0f, .25f, f) * .92f * height / 4.6f;
                Mesh(parent, Bulb(), basePos + new Vector3(Mathf.Cos(a) * r, .5f + f * (height - 1.4f), Mathf.Sin(a) * r), Quaternion.Euler(180, 0, 0), Vector3.one * .6f, lights[i % lights.Length], "Tree light");
            }
            Mesh(parent, Star(), basePos + Vector3.up * (height - .3f), Quaternion.identity, Vector3.one * height / 4.6f, lights[2], "Tree star");
        }

        /// A wooden Christmas-market chalet: plank walls, a counter under a red awning, a steep snow-covered
        /// roof, lights along the eaves and a wreath. 1.9 m deep, 3 m to the ridge, below the wall-run line.
        static void Chalet(TrackSpawner track, Transform parent, float side, float center, int block, Material snow, Material red, Material light)
        {
            var wood = M(track, "xmasChaletWood", new Color(.42f, .25f, .13f), 0f, .25f, 0f);
            var dark = M(track, "xmasChaletTrim", new Color(.2f, .11f, .05f), 0f, .25f, 0f);
            var pine = M(track, "xmasPine", Green, 0f, .2f, 0f);
            float x = side * 5.7f;
            Box(parent, new Vector3(x, 1.15f, center), new Vector3(1.9f, 2.3f, 2.8f), wood);
            for (int p = 0; p < 7; p++) Box(parent, new Vector3(x - side * .955f, .2f + p * .33f, center), new Vector3(.02f, .03f, 2.82f), dark);
            Box(parent, new Vector3(x - side * .97f, 1.3f, center), new Vector3(.04f, .9f, 2.1f), dark);
            Box(parent, new Vector3(x - side * 1.12f, 1.0f, center), new Vector3(.35f, .06f, 2.2f), wood);
            var awning = Box(parent, new Vector3(x - side * 1.12f, 1.95f, center), new Vector3(.5f, .04f, 2.4f), red);
            awning.transform.rotation = Quaternion.Euler(0, 0, side * -18f);
            foreach (float s in new[] { -1f, 1f })
            {
                var slope = Box(parent, new Vector3(x + s * .58f, 2.68f, center), new Vector3(1.4f, .08f, 3.2f), dark);
                slope.transform.rotation = Quaternion.Euler(0, 0, -s * 38f);
                var cap = Box(parent, new Vector3(x + s * .56f, 2.76f, center), new Vector3(1.36f, .12f, 3.24f), snow);
                cap.transform.rotation = Quaternion.Euler(0, 0, -s * 38f);
            }
            for (int i = 0; i < 9; i++)
                Mesh(parent, Bulb(), new Vector3(x - side * 1.05f, 2.18f, center - 1.5f + i * .375f), Quaternion.Euler(180, 0, 0), Vector3.one * .6f, light, "Eave light");
            Wreath(parent, new Vector3(x - side * .99f, 2.5f, center), .2f, pine, red, light);
            string[] names = { "MULLED CIDER", "GINGERBREAD", "ROAST CHESTNUTS", "XMAS BURGERS" };
            var label = new GameObject("Chalet sign");
            label.transform.SetParent(parent, false);
            label.transform.position = new Vector3(x - side * 1.0f, 2.05f, center);
            label.transform.rotation = Quaternion.Euler(0, side > 0 ? -90 : 90, 0);
            var tm = label.AddComponent<TextMesh>(); tm.text = names[Mathf.Abs(block) % names.Length]; tm.fontSize = 64;
            tm.characterSize = .026f; tm.anchor = TextAnchor.MiddleCenter; tm.color = Color.white;
            if (track.signFont) tm.font = track.signFont;
            label.GetComponent<MeshRenderer>().sharedMaterial = track.signTextMaterial;
        }

        /// A candy cane: red and white segments up the shaft and round the hook. `height` is the shaft.
        internal static void CandyCane(TrackSpawner track, Transform parent, Vector3 at, float side, float height = 1.12f, float thick = .075f)
        {
            var red = M(track, "xmasCaneRed", new Color(.85f, .05f, .08f), .1f, .7f);
            var white = M(track, "xmasCaneWhite", new Color(.97f, .96f, .93f), .05f, .7f);
            var pts = new List<Vector3>();
            for (int i = 0; i <= 8; i++) pts.Add(at + Vector3.up * (i * height / 8f));
            float hook = height * .14f;
            for (int i = 1; i <= 6; i++)
            {
                float a = i * Mathf.PI / 6f;
                pts.Add(at + new Vector3(-side * (hook - Mathf.Cos(a) * hook), height + Mathf.Sin(a) * hook, 0));
            }
            for (int i = 0; i < pts.Count - 1; i++) Member(parent, pts[i], pts[i + 1], thick, i % 2 == 0 ? red : white);
        }

        /// MERRY CHRISTMAS arch over the course: garland-wrapped truss legs, a snowy beam, a lit sign board,
        /// lights along the top and a wreath under the beam.
        static void Arch(TrackSpawner track, Transform parent, float z, Material pine, Material red, Material snow, Material[] lights)
        {
            var truss = M(track, "xmasArchTruss", new Color(.62f, .64f, .68f), 0f, .55f, .7f);
            var board = M(track, "xmasArchBoard", new Color(.08f, .03f, .05f), .2f);
            const float leg = 4.95f, beam = 8.1f, w = .3f;
            foreach (float side in new[] { -1f, 1f })
            {
                float x = side * leg;
                foreach (var c in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) })
                    Member(parent, new Vector3(x + c.x * w * .5f, .1f, z + c.y * w * .5f), new Vector3(x + c.x * w * .5f, beam, z + c.y * w * .5f), .035f, truss);
                int n = 0;
                for (float y = .4f; y < beam; y += .55f, n++)
                {
                    Mesh(parent, FirTier(), new Vector3(x, y, z), Quaternion.Euler(0, y * 40f, 0), new Vector3(.34f, .3f, .34f), pine, "Garland on the arch leg");
                    if (n % 2 == 0)
                        Mesh(parent, Bulb(), new Vector3(x - side * .3f, y, z - .1f), Quaternion.Euler(180, 0, 0), Vector3.one * .7f, lights[n % lights.Length], "Arch light");
                }
            }
            foreach (var c in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) })
                Member(parent, new Vector3(-leg, beam + c.x * w * .5f, z + c.y * w * .5f), new Vector3(leg, beam + c.x * w * .5f, z + c.y * w * .5f), .035f, truss);
            Box(parent, new Vector3(0, beam + .2f, z), new Vector3(2 * leg + .3f, .1f, .42f), snow);
            Box(parent, new Vector3(0, beam + 1.25f, z), new Vector3(6.2f, 1.9f, .14f), board);
            for (int i = 0; i < 14; i++)
                Mesh(parent, Bulb(), new Vector3(Mathf.Lerp(-3f, 3f, i / 13f), beam + 2.25f, z - .1f), Quaternion.Euler(180, 0, 0), Vector3.one * .7f, lights[i % lights.Length], "Sign light");
            var label = new GameObject("Arch sign");
            label.transform.SetParent(parent, false);
            label.transform.position = new Vector3(0, beam + 1.25f, z - .1f);
            var tm = label.AddComponent<TextMesh>(); tm.text = "MERRY CHRISTMAS"; tm.fontSize = 64; tm.characterSize = .08f;
            tm.anchor = TextAnchor.MiddleCenter; tm.color = new Color(1f, .85f, .45f);
            if (track.signFont) tm.font = track.signFont;
            label.GetComponent<MeshRenderer>().sharedMaterial = track.signTextMaterial;
            Wreath(parent, new Vector3(0, beam - .55f, z - .2f), .42f, pine, red, lights[3]);
        }
    }

    /// Falling snow over the Festivals district: one world-space particle system that follows the camera.
    public sealed class ChristmasSnow : MonoBehaviour
    {
        static ChristmasSnow instance;
        ParticleSystem system;
        Material flake;

        /// On in the Festivals district only. `flakeMaterial` is a game material (always in the build).
        public static void Ensure(bool on, Material flakeMaterial)
        {
            if (!instance)
            {
                if (!on) return;
                var go = new GameObject("Falling snow");
                go.SetActive(false);
                instance = go.AddComponent<ChristmasSnow>();
                instance.flake = flakeMaterial;
                go.SetActive(true);
            }
            instance.gameObject.SetActive(on);
        }

        static Material flakeMaterial;
        /// Round, soft white flakes: the burger halo's glow shader and soft-dot texture, recoloured white.
        public static Material FlakeMaterial(TrackSpawner track)
        {
            if (flakeMaterial) return flakeMaterial;
            if (track.haloMaterial)
            {
                flakeMaterial = new Material(track.haloMaterial) { name = "Snowflake" };
                flakeMaterial.SetColor("_BaseColor", new Color(1f, 1f, 1f, .95f));
            }
            else flakeMaterial = DashCity.Mat(track, "snowflake", Color.white, 1.4f);
            return flakeMaterial;
        }

        void Awake()
        {
            system = gameObject.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = true; main.playOnAwake = true;
            main.startLifetime = 7f; main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(.12f, .26f);
            main.startColor = new Color(1f, 1f, 1f, .9f);
            main.gravityModifier = .06f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            // Low graphics keeps a light flurry; High is a proper snowfall.
            main.maxParticles = PlayerProfile.Graphics == 0 ? 600 : PlayerProfile.Graphics == 1 ? 1100 : 1800;
            var emission = system.emission; emission.rateOverTime = main.maxParticles / 7f;
            var shape = system.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(22f, 1f, 70f);
            var noise = system.noise; noise.enabled = true; noise.strength = .35f; noise.frequency = .25f;
            var velocity = system.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-.3f, .3f); velocity.y = new ParticleSystem.MinMaxCurve(-1.4f, -.8f); velocity.z = new ParticleSystem.MinMaxCurve(-.2f, .2f);
            var renderer = GetComponent<ParticleSystemRenderer>();
            if (flake) renderer.sharedMaterial = flake;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            system.Play();
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (!cam) return;
            // Emit from a slab above and ahead of the camera, so the flakes fall through the view.
            transform.position = cam.transform.position + Vector3.up * 8f + Vector3.forward * 30f;
        }
    }
}
