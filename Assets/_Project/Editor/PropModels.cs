using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DallasDash.EditorTools
{
    /// Authored street props, built from lofted superellipse forms instead of AI photogrammetry
    /// blobs: the low hurdles the rider hops, the cargo gate they slide under, the delivery pod,
    /// and the lamp and planter that dress the pavement. Each prop is merged down to one mesh per
    /// material, so a hurdle costs a handful of draw calls and can be pooled by the hundred.
    public static class PropModels
    {
        const string PrefabDir = "Assets/_Project/Prefabs/Props";
        const string MeshDir = "Assets/_Project/Art/DashRiders/Props";
        const string MatDir = "Assets/_Project/Materials/Props";

        static string variant = "prop";

        // ================= materials =================

        static readonly Dictionary<string, Material> cache = new();

        static Material M(string id, Color color, float glow = 0f, float smoothness = 0.35f, float metallic = 0.05f)
        {
            if (cache.TryGetValue(id, out var cached) && cached) return cached;
            System.IO.Directory.CreateDirectory(MatDir);
            string path = MatDir + "/" + id + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find("FoodInc/ToonCurved")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", color);
            m.SetColor("_EmissionColor", glow > 0f ? color * glow : Color.black);
            m.SetColor("_ShadeColor", new Color(0.74f, 0.78f, 0.94f));
            m.SetFloat("_OutlineWidth", 0.006f);
            m.SetFloat("_SurfaceStyle", 0f);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_RimStrength", 0.4f);
            m.SetFloat("_RimPower", 3.2f);
            m.SetFloat("_AmbientStrength", 0.4f);
            m.SetFloat("_ShadowSoftness", 0.2f);
            m.SetShaderPassEnabled("Outline", true);
            EditorUtility.SetDirty(m);
            cache[id] = m;
            return m;
        }

        static Material Ink => M("propInk", new Color(0.05f, 0.055f, 0.075f));
        static Material Steel => M("propSteel", new Color(0.6f, 0.65f, 0.72f), 0f, 0.55f, 0.55f);
        static Material SteelDark => M("propSteelDark", new Color(0.28f, 0.32f, 0.38f), 0f, 0.45f, 0.4f);
        static Material Orange => M("propOrange", new Color(1f, 0.33f, 0.04f));
        static Material OrangeDark => M("propOrangeDark", new Color(0.62f, 0.16f, 0.02f));
        static Material White => M("propWhite", new Color(0.96f, 0.97f, 1f));
        static Material Cyan => M("propCyan", new Color(0.1f, 0.9f, 1f), 1.7f);
        static Material Amber => M("propAmber", new Color(1f, 0.62f, 0.07f), 1.5f);
        static Material Magenta => M("propMagenta", new Color(1f, 0.1f, 0.5f), 1.1f);
        static Material Lime => M("propLime", new Color(0.55f, 0.95f, 0.2f));
        static Material Glass => M("propGlass", new Color(0.04f, 0.09f, 0.15f), 0.1f, 0.8f, 0.25f);
        static Material Leaf => M("propLeaf", new Color(0.16f, 0.55f, 0.28f));
        static Material LeafLight => M("propLeafLight", new Color(0.34f, 0.78f, 0.34f));
        static Material Soil => M("propSoil", new Color(0.14f, 0.1f, 0.07f));

        // ================= mesh toolkit =================

        /// One superellipse cross-section: 2 is a circle, 4 a soft rectangle, 8 almost a box.
        public struct Ring
        {
            public float y, rx, rz, square;
            public Ring(float y, float rx, float rz, float square = 2f)
            {
                this.y = y; this.rx = rx; this.rz = rz; this.square = square;
            }
        }

        static Mesh SaveMesh(string key, Mesh mesh)
        {
            System.IO.Directory.CreateDirectory(MeshDir);
            string path = MeshDir + "/" + key + ".asset";
            // A fresh asset every run: CopySerialized does not carry a Mesh's native vertex data over,
            // which left zero-bounds meshes behind. The GUID churn is harmless because every prop is
            // built before any prefab that embeds it is saved (see BuildStreetHurdles).
            AssetDatabase.DeleteAsset(path);
            mesh.name = key;
            AssetDatabase.CreateAsset(mesh, path);
            // Import straight away: a prefab saved in the same session keeps a null mesh reference
            // until the freshly created mesh asset has been imported once.
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Mesh>(path);
        }

        static Mesh Loft(string key, Ring[] rings, int segments = 20, bool caps = true)
        {
            int n = rings.Length;
            var verts = new List<Vector3>(n * segments + 2);
            var uvs = new List<Vector2>(n * segments + 2);
            var tris = new List<int>((n - 1) * segments * 6 + segments * 6);
            for (int i = 0; i < n; i++)
            {
                var r = rings[i];
                float e = 2f / Mathf.Max(r.square, 1.2f);
                for (int s = 0; s < segments; s++)
                {
                    float t = Mathf.PI * 2f * s / segments;
                    float cx = Mathf.Cos(t), sz = Mathf.Sin(t);
                    float x = Mathf.Sign(cx) * Mathf.Pow(Mathf.Abs(cx), e) * r.rx;
                    float z = Mathf.Sign(sz) * Mathf.Pow(Mathf.Abs(sz), e) * r.rz;
                    verts.Add(new Vector3(x, r.y, z));
                    uvs.Add(new Vector2(s / (float)segments, i / Mathf.Max(1f, n - 1f)));
                }
            }
            for (int i = 0; i < n - 1; i++)
            for (int s = 0; s < segments; s++)
            {
                int a = i * segments + s, b = i * segments + (s + 1) % segments;
                int c = (i + 1) * segments + s, d = (i + 1) * segments + (s + 1) % segments;
                tris.Add(a); tris.Add(c); tris.Add(d);
                tris.Add(a); tris.Add(d); tris.Add(b);
            }
            if (caps)
            {
                int bottom = verts.Count;
                verts.Add(new Vector3(0f, rings[0].y, 0f)); uvs.Add(new Vector2(0.5f, 0f));
                int top = verts.Count;
                verts.Add(new Vector3(0f, rings[n - 1].y, 0f)); uvs.Add(new Vector2(0.5f, 1f));
                for (int s = 0; s < segments; s++)
                {
                    int a = s, b = (s + 1) % segments;
                    int c = (n - 1) * segments + s, d = (n - 1) * segments + (s + 1) % segments;
                    tris.Add(bottom); tris.Add(a); tris.Add(b);
                    tris.Add(top); tris.Add(d); tris.Add(c);
                }
            }
            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return SaveMesh(variant + "_" + key, mesh);
        }

        /// Rounded box, bevel on every edge.
        static Mesh Box(string key, Vector3 size, float bevel = 0.015f, float square = 5f)
        {
            float h = size.y * 0.5f;
            float b = Mathf.Min(bevel, Mathf.Min(size.x, size.z) * 0.42f);
            float bt = Mathf.Min(bevel, h * 0.7f);
            return Loft(key, new[]
            {
                new Ring(-h, size.x * 0.5f - b, size.z * 0.5f - b, square),
                new Ring(-h + bt, size.x * 0.5f, size.z * 0.5f, square),
                new Ring(h - bt, size.x * 0.5f, size.z * 0.5f, square),
                new Ring(h, size.x * 0.5f - b, size.z * 0.5f - b, square)
            }, 20, true);
        }

        /// Tapered tube along Y, centred on its middle.
        static Mesh Tube(string key, float top, float bottom, float height, float square = 2f, int segments = 16)
        {
            return Loft(key, new[]
            {
                new Ring(-height * 0.5f, bottom, bottom, square),
                new Ring(height * 0.5f, top, top, square)
            }, segments, true);
        }

        // ================= scene helpers =================

        static GameObject Part(Transform parent, string name, Mesh mesh, Material mat, Vector3 at,
            Vector3 euler = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go;
        }

        static GameObject Shape(Transform parent, string name, PrimitiveType type, Vector3 at, Vector3 scale,
            Material mat, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        /// Merge every child renderer into one mesh per material, so a prop is a few draw calls
        /// and a single prefab with no hierarchy to walk at spawn time.
        static void Merge(GameObject root, string id)
        {
            var groups = new Dictionary<Material, List<CombineInstance>>();
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if (!filter.sharedMesh) continue;
                var renderer = filter.GetComponent<MeshRenderer>();
                if (!renderer || !renderer.sharedMaterial) continue;
                if (!groups.TryGetValue(renderer.sharedMaterial, out var list))
                    groups[renderer.sharedMaterial] = list = new List<CombineInstance>();
                list.Add(new CombineInstance
                {
                    mesh = filter.sharedMesh,
                    transform = root.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix
                });
            }
            foreach (var child in root.GetComponentsInChildren<Transform>())
                if (child != root.transform) UnityEngine.Object.DestroyImmediate(child.gameObject);

            int index = 0;
            foreach (var pair in groups)
            {
                var mesh = new Mesh { name = id + "-" + index, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(pair.Value.ToArray(), true, true);
                mesh.RecalculateBounds();
                mesh = SaveMesh(id + "-" + index++, mesh);
                var go = new GameObject(pair.Key.name);
                go.transform.SetParent(root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = pair.Key;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        static GameObject Save(GameObject root, string id)
        {
            Merge(root, id);
            System.IO.Directory.CreateDirectory(PrefabDir);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/" + id + ".prefab");
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log("[DallasDash] Prop model " + id);
            return prefab;
        }

        /// Builds each prop once per run: the bootstrap asks for the cone from the cone row and again
        /// from the roadworks, and a second build would rewrite the meshes under the first prefab.
        static readonly Dictionary<string, GameObject> built = new();
        static GameObject Cached(string id, Func<GameObject> build)
        {
            if (built.TryGetValue(id, out var go) && go) return go;
            var made = build();
            built[id] = made;
            return made;
        }

        // ================= hurdles =================

        /// A low hurdle: a chevron-striped road barricade on splayed legs, with a lit top rail and
        /// two warning lamps. 1.9 m across, 0.95 m tall, so a bunny hop clears it cleanly.
        public static GameObject Barrier(bool neon) =>
            Cached(neon ? "neonBarrier" : "barricade", () => BuildBarrier(neon));

        static GameObject BuildBarrier(bool neon)
        {
            variant = neon ? "neonBarrier" : "barricade";
            var root = new GameObject(variant);
            var t = root.transform;
            var body = neon ? Orange : White;
            var stripe = neon ? White : Orange;
            var edge = neon ? Cyan : Amber;

            foreach (float side in new[] { -1f, 1f })
            {
                Part(t, "Foot", Box("foot", new Vector3(0.2f, 0.07f, 0.56f), 0.02f), Ink,
                    new Vector3(side * 0.79f, 0.035f, 0f));
                foreach (float z in new[] { -1f, 1f })
                    Part(t, "Leg", Box("leg", new Vector3(0.075f, 0.84f, 0.075f), 0.015f), SteelDark,
                        new Vector3(side * 0.79f, 0.42f, z * 0.17f), new Vector3(-z * 13f, 0f, 0f));
                Part(t, "Post cap", Box("cap", new Vector3(0.09f, 0.1f, 0.09f), 0.02f), edge,
                    new Vector3(side * 0.79f, 0.87f, 0f));
            }

            // The panel and its diagonal hazard stripes.
            Part(t, "Panel", Box("panel", new Vector3(1.66f, 0.38f, 0.08f), 0.02f), body,
                new Vector3(0f, 0.6f, 0f));
            for (int i = -2; i <= 2; i++)
                Part(t, "Stripe", Box("stripe", new Vector3(0.17f, 0.44f, 0.012f), 0.005f), stripe,
                    new Vector3(i * 0.32f, 0.6f, 0.048f), new Vector3(0f, 0f, 24f));
            Part(t, "Top rail", Box("topRail", new Vector3(1.74f, 0.06f, 0.1f), 0.015f), SteelDark,
                new Vector3(0f, 0.8f, 0f));
            Part(t, "Neon rail", Box("neonRail", new Vector3(1.66f, 0.028f, 0.11f), 0.008f), edge,
                new Vector3(0f, 0.828f, 0f));
            Part(t, "Bottom rail", Box("bottomRail", new Vector3(1.7f, 0.05f, 0.09f), 0.012f), SteelDark,
                new Vector3(0f, 0.4f, 0f));

            foreach (float x in new[] { -0.56f, 0.56f })
            {
                Shape(t, "Lamp", PrimitiveType.Sphere, new Vector3(x, 0.88f, 0.02f), Vector3.one * 0.1f, Amber);
                Part(t, "Lamp hood", Box("lampHood", new Vector3(0.13f, 0.04f, 0.11f), 0.01f), Ink,
                    new Vector3(x, 0.925f, 0.02f));
            }
            return Save(root, variant);
        }

        /// The overhead cargo gate: two striped posts carrying a beam and a suspended container
        /// whose underside sits at 1.98 m, exactly the slide clearance the spawner uses.
        public static GameObject CargoGate()
        {
            variant = "cargoGate";
            var root = new GameObject(variant);
            var t = root.transform;
            const float clearance = 1.98f;

            foreach (float side in new[] { -1f, 1f })
            {
                Part(t, "Base plate", Box("base", new Vector3(0.34f, 0.08f, 0.6f), 0.02f), Ink,
                    new Vector3(side * 0.9f, 0.04f, 0f));
                Part(t, "Post", Box("post", new Vector3(0.13f, 2.36f, 0.17f), 0.02f), SteelDark,
                    new Vector3(side * 0.9f, 1.18f, 0f));
                for (int i = 0; i < 4; i++)
                    Part(t, "Hazard band", Box("band", new Vector3(0.145f, 0.16f, 0.185f), 0.012f),
                        i % 2 == 0 ? Orange : White, new Vector3(side * 0.9f, 0.32f + i * 0.22f, 0f));
                Part(t, "Post light", Box("postLight", new Vector3(0.04f, 1.5f, 0.04f), 0.008f), Cyan,
                    new Vector3(side * 0.96f, 1.3f, 0f));
            }

            Part(t, "Beam", Box("beam", new Vector3(2f, 0.18f, 0.26f), 0.025f), SteelDark, new Vector3(0f, 2.31f, 0f));
            Part(t, "Beam light", Box("beamLight", new Vector3(1.9f, 0.035f, 0.28f), 0.008f), Cyan,
                new Vector3(0f, 2.2f, 0f));
            foreach (float x in new[] { -0.62f, 0.62f })
                Part(t, "Beacon", Box("beacon", new Vector3(0.16f, 0.08f, 0.18f), 0.015f), Amber,
                    new Vector3(x, 2.44f, 0f));

            // The suspended container: corrugated sides, lit underside, magenta cargo badge.
            float crateMid = (clearance + 2.16f) * 0.5f;
            Part(t, "Container", Box("container", new Vector3(1.5f, 2.16f - clearance, 0.72f), 0.03f), Steel,
                new Vector3(0f, crateMid, 0f));
            for (int i = -2; i <= 2; i++)
                Part(t, "Rib", Box("rib", new Vector3(0.06f, 0.16f, 0.74f), 0.01f), SteelDark,
                    new Vector3(i * 0.3f, crateMid, 0f));
            Part(t, "Cargo badge", Box("badge", new Vector3(0.5f, 0.14f, 0.02f), 0.01f), Magenta,
                new Vector3(0f, crateMid, 0.37f));
            Part(t, "Underside light", Box("underLight", new Vector3(1.4f, 0.025f, 0.6f), 0.006f), Cyan,
                new Vector3(0f, clearance + 0.012f, 0f));
            foreach (float x in new[] { -0.55f, 0.55f })
                Part(t, "Cable", Box("cable", new Vector3(0.035f, 0.14f, 0.035f), 0.005f), Ink,
                    new Vector3(x, 2.23f, 0f));
            return Save(root, variant);
        }

        /// A traffic cone: rubber base, tapered body, two reflective collars.
        public static GameObject Cone() => Cached("trafficCone", BuildCone);

        static GameObject BuildCone()
        {
            variant = "trafficCone";
            var root = new GameObject(variant);
            var t = root.transform;
            Part(t, "Base", Box("coneBase", new Vector3(0.4f, 0.045f, 0.4f), 0.012f), Ink,
                new Vector3(0f, 0.0225f, 0f));
            Part(t, "Body", Loft("coneBody", new[]
            {
                new Ring(0.04f, 0.145f, 0.145f, 2.2f),
                new Ring(0.16f, 0.118f, 0.118f, 2.1f),
                new Ring(0.34f, 0.083f, 0.083f, 2f),
                new Ring(0.56f, 0.05f, 0.05f, 2f),
                new Ring(0.73f, 0.018f, 0.018f, 2f)
            }, 18), Orange, Vector3.zero);
            Part(t, "Collar", Loft("coneCollar", new[]
            {
                new Ring(0.22f, 0.113f, 0.113f, 2.1f),
                new Ring(0.3f, 0.099f, 0.099f, 2.1f)
            }, 18, false), White, Vector3.zero);
            Part(t, "Collar 2", Loft("coneCollar2", new[]
            {
                new Ring(0.4f, 0.077f, 0.077f, 2f),
                new Ring(0.46f, 0.066f, 0.066f, 2f)
            }, 18, false), White, Vector3.zero);
            return Save(root, variant);
        }

        /// Dash City's autonomous delivery pod: a rounded cargo body on four wheels, a dark canopy
        /// band, cyan running lights and a magenta hatch. 3.6 m long, 1.85 m wide, 2.1 m tall.
        public static GameObject Pod()
        {
            variant = "deliveryPod";
            var root = new GameObject(variant);
            var t = root.transform;

            // Body lofted along Y then turned to run down the street (+Z is the nose): the ring's
            // rz becomes the body's half height, so the centre sits one half-height above the road.
            const float halfHeight = 1.02f;
            Part(t, "Body", Loft("podBody", new[]
            {
                new Ring(-1.8f, 0.7f, 0.66f, 3.4f),
                new Ring(-1.66f, 0.86f, 0.92f, 3.2f),
                new Ring(-0.9f, 0.9f, 1f, 3f),
                new Ring(0.4f, 0.92f, halfHeight, 3f),
                new Ring(1.3f, 0.9f, 0.97f, 3.1f),
                new Ring(1.7f, 0.8f, 0.84f, 3.3f),
                new Ring(1.8f, 0.62f, 0.66f, 3.4f)
            }, 26), White, new Vector3(0f, halfHeight, 0f), new Vector3(90f, 0f, 0f));

            // Canopy: a dark glass band standing just proud of the upper body.
            Part(t, "Canopy", Loft("podCanopy", new[]
            {
                new Ring(-1.05f, 0.9f, 0.2f, 3.1f),
                new Ring(0.05f, 0.95f, 0.22f, 3f),
                new Ring(1.05f, 0.9f, 0.2f, 3.1f)
            }, 24), Glass, new Vector3(0f, 1.46f, 0f), new Vector3(90f, 0f, 0f));

            // Rounded roof slab over the cargo bay.
            Part(t, "Roof", Loft("podRoof", new[]
            {
                new Ring(-1.55f, 0.6f, 0.07f, 3.4f),
                new Ring(-1.35f, 0.72f, 0.1f, 3.2f),
                new Ring(1.35f, 0.72f, 0.1f, 3.2f),
                new Ring(1.55f, 0.6f, 0.07f, 3.4f)
            }, 24), White, new Vector3(0f, 2.06f, 0f), new Vector3(90f, 0f, 0f));
            Part(t, "Roof light", Box("podRoofLight", new Vector3(0.44f, 0.04f, 0.14f), 0.01f), Cyan,
                new Vector3(0f, 2.14f, 0.42f));
            Part(t, "Sensor", Loft("podSensor", new[]
            {
                new Ring(0f, 0.12f, 0.12f, 2f),
                new Ring(0.16f, 0.1f, 0.1f, 2f),
                new Ring(0.24f, 0.045f, 0.045f, 2f)
            }, 14), SteelDark, new Vector3(0f, 2.02f, -0.2f));

            // Wheels and arches.
            foreach (float side in new[] { -1f, 1f })
            foreach (float z in new[] { -1.12f, 1.12f })
            {
                Shape(t, "Tyre", PrimitiveType.Cylinder, new Vector3(side * 0.86f, 0.34f, z),
                    new Vector3(0.66f, 0.11f, 0.66f), Ink, new Vector3(0f, 0f, 90f));
                Shape(t, "Hub", PrimitiveType.Cylinder, new Vector3(side * 0.93f, 0.34f, z),
                    new Vector3(0.3f, 0.1f, 0.3f), Steel, new Vector3(0f, 0f, 90f));
                Part(t, "Arch", Box("arch", new Vector3(0.1f, 0.12f, 0.9f), 0.02f), SteelDark,
                    new Vector3(side * 0.88f, 0.72f, z));
            }

            // Nose lights, rear hatch and bumpers.
            Part(t, "Headlight bar", Box("headBar", new Vector3(1.1f, 0.09f, 0.05f), 0.012f), Cyan,
                new Vector3(0f, 0.95f, 1.78f));
            foreach (float side in new[] { -1f, 1f })
                Part(t, "Headlight", Box("headlight", new Vector3(0.2f, 0.16f, 0.05f), 0.012f), Cyan,
                    new Vector3(side * 0.5f, 0.78f, 1.8f));
            Part(t, "Front bumper", Box("frontBumper", new Vector3(1.5f, 0.16f, 0.14f), 0.02f), SteelDark,
                new Vector3(0f, 0.44f, 1.8f));
            Part(t, "Rear hatch", Box("hatch", new Vector3(1.2f, 0.8f, 0.06f), 0.02f), Magenta,
                new Vector3(0f, 1.18f, -1.8f));
            Part(t, "Hatch handle", Box("handle", new Vector3(0.4f, 0.06f, 0.04f), 0.008f), White,
                new Vector3(0f, 1.5f, -1.84f));
            Part(t, "Rear bumper", Box("rearBumper", new Vector3(1.5f, 0.16f, 0.14f), 0.02f), SteelDark,
                new Vector3(0f, 0.44f, -1.8f));
            foreach (float side in new[] { -1f, 1f })
                Part(t, "Tail light", Box("tailLight", new Vector3(0.24f, 0.08f, 0.04f), 0.01f), Magenta,
                    new Vector3(side * 0.62f, 0.78f, -1.82f));
            Part(t, "Side stripe", Box("sideStripe", new Vector3(0.03f, 0.06f, 2.6f), 0.008f), Cyan,
                new Vector3(0.94f, 1.02f, 0f));
            Part(t, "Side stripe", Box("sideStripe", new Vector3(0.03f, 0.06f, 2.6f), 0.008f), Cyan,
                new Vector3(-0.94f, 1.02f, 0f));
            return Save(root, variant);
        }

        // ================= street dressing =================

        /// A clean contemporary lamp: plinth, tapered pole, arm over the road and a lit head bar.
        /// The arm points along -X, so the caller turns it to face the street.
        public static GameObject Lamp()
        {
            variant = "streetLamp";
            var root = new GameObject(variant);
            var t = root.transform;
            Part(t, "Plinth", Loft("lampPlinth", new[]
            {
                new Ring(0f, 0.22f, 0.22f, 3.6f),
                new Ring(0.12f, 0.24f, 0.24f, 3.6f),
                new Ring(0.18f, 0.18f, 0.18f, 3f)
            }, 18), SteelDark, Vector3.zero);
            Part(t, "Pole", Loft("lampPole", new[]
            {
                new Ring(0.14f, 0.085f, 0.085f, 2.2f),
                new Ring(2.4f, 0.06f, 0.06f, 2f),
                new Ring(4.75f, 0.048f, 0.048f, 2f)
            }, 16), Steel, Vector3.zero);
            Part(t, "Collar", Loft("lampCollar", new[]
            {
                new Ring(0.3f, 0.1f, 0.1f, 2.2f),
                new Ring(0.36f, 0.09f, 0.09f, 2.2f)
            }, 16, false), Cyan, Vector3.zero);
            Part(t, "Arm", Box("lampArm", new Vector3(1.3f, 0.085f, 0.1f), 0.015f), Steel,
                new Vector3(-0.62f, 4.82f, 0f), new Vector3(0f, 0f, 3f));
            Part(t, "Head", Box("lampHead", new Vector3(0.86f, 0.11f, 0.24f), 0.02f), SteelDark,
                new Vector3(-1.28f, 4.7f, 0f));
            Part(t, "Lens", Box("lampLens", new Vector3(0.78f, 0.03f, 0.2f), 0.008f), Amber,
                new Vector3(-1.28f, 4.635f, 0f));
            Part(t, "Halo", Box("lampHalo", new Vector3(0.7f, 0.02f, 0.16f), 0.005f), M("propLampHalo", new Color(1f, 0.85f, 0.6f), 2.2f),
                new Vector3(-1.28f, 4.62f, 0f));
            return Save(root, variant);
        }

        /// A pavement planter: plinth, soil bed, three clipped shrubs and a lit edge.
        public static GameObject Planter()
        {
            variant = "streetPlanter";
            var root = new GameObject(variant);
            var t = root.transform;
            Part(t, "Plinth", Box("planterBox", new Vector3(1.5f, 0.5f, 2.7f), 0.05f, 6f), SteelDark,
                new Vector3(0f, 0.25f, 0f));
            Part(t, "Rim", Box("planterRim", new Vector3(1.58f, 0.08f, 2.78f), 0.02f, 6f), Steel,
                new Vector3(0f, 0.52f, 0f));
            Part(t, "Soil", Box("planterSoil", new Vector3(1.34f, 0.06f, 2.54f), 0.02f, 6f), Soil,
                new Vector3(0f, 0.55f, 0f));
            for (int i = 0; i < 3; i++)
            {
                float z = -0.8f + i * 0.8f;
                Part(t, "Shrub", Loft("shrub", new[]
                {
                    new Ring(0.56f, 0.42f, 0.42f, 2.6f),
                    new Ring(0.78f, 0.52f, 0.5f, 2.4f),
                    new Ring(1f, 0.42f, 0.4f, 2.2f),
                    new Ring(1.16f, 0.2f, 0.2f, 2f)
                }, 18), Leaf, new Vector3(0f, 0f, z));
                Part(t, "Shrub top", Loft("shrubTop", new[]
                {
                    new Ring(0.98f, 0.3f, 0.29f, 2.3f),
                    new Ring(1.12f, 0.24f, 0.23f, 2.2f),
                    new Ring(1.3f, 0.06f, 0.06f, 2f)
                }, 14), LeafLight, new Vector3(0.04f, 0f, z + 0.05f));
            }
            Part(t, "Edge light", Box("planterLight", new Vector3(1.52f, 0.025f, 2.72f), 0.006f), Cyan,
                new Vector3(0f, 0.545f, 0f));
            return Save(root, variant);
        }
    }
}
