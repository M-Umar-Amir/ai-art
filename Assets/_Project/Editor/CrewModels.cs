using System.Collections.Generic;
using DallasDash.Gameplay;
using UnityEditor;
using UnityEngine;

namespace DallasDash.EditorTools
{
    /// Authored crew models: clean, toon-shaded, articulated characters built from lofted meshes
    /// instead of AI photogrammetry blobs. Each character has a standing variant for the crew screen
    /// and a riding variant posed on the board (Bolt on his own monowheel). Limbs are real joints, so
    /// DashPose animates them in game.
    public static class CrewModels
    {
        const string PrefabDir = "Assets/_Project/Prefabs/Crew";
        const string MeshDir = "Assets/_Project/Art/DashRiders/Crew";
        const string MatDir = "Assets/_Project/Materials/Crew";

        static string variant = "Ace";

        public static GameObject Build(int character, bool riding)
        {
            character = Mathf.Clamp(character, 0, 2);
            string name = character == 0 ? "Ace" : character == 1 ? "Nova" : "Bolt";
            string id = name + (riding && character != 2 ? "-Ride" : "");
            variant = id;
            var root = new GameObject(id);
            if (character == 2) Bolt(root.transform);
            else Human(root.transform, character, riding);
            System.IO.Directory.CreateDirectory(PrefabDir);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/" + id + ".prefab");
            Object.DestroyImmediate(root);
            Debug.Log("[DallasDash] Crew model " + id);
            return prefab;
        }

        // ================= materials =================

        static readonly Dictionary<string, Material> cache = new();

        static Material M(string id, Color color, float glow = 0f, float smoothness = 0.3f, float metallic = 0.03f)
        {
            if (cache.TryGetValue(id, out var cached) && cached) return cached;
            System.IO.Directory.CreateDirectory(MatDir);
            string path = MatDir + "/" + id + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find("FoodInc/ToonCurved")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", color);
            m.SetColor("_EmissionColor", glow > 0f ? color * glow : Color.black);
            m.SetColor("_ShadeColor", new Color(0.72f, 0.76f, 0.94f));
            m.SetFloat("_OutlineWidth", 0.007f);
            m.SetFloat("_SurfaceStyle", 0f);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_RimStrength", 0.55f);
            m.SetFloat("_RimPower", 3.4f);
            m.SetFloat("_AmbientStrength", 0.42f);
            m.SetFloat("_ShadowSoftness", 0.2f);
            m.SetShaderPassEnabled("Outline", true);
            EditorUtility.SetDirty(m);
            cache[id] = m;
            return m;
        }

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
            AssetDatabase.DeleteAsset(path);
            mesh.name = key;
            AssetDatabase.CreateAsset(mesh, path);
            return AssetDatabase.LoadAssetAtPath<Mesh>(path);
        }

        /// A lofted solid: rings of superellipse sections stacked up Y with smooth normals, so limbs
        /// and torsos read as sculpted forms rather than stacked primitives.
        static Mesh Loft(string key, Ring[] rings, int segments = 24, bool caps = true)
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
        static Mesh Box(string key, Vector3 size, float bevel = 0.02f, float square = 5f)
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
            }, 24, true);
        }

        /// Tapered tube along Y, centred on its middle.
        static Mesh Tube(string key, float top, float bottom, float height, float square = 2f, int segments = 18)
        {
            return Loft(key, new[]
            {
                new Ring(-height * 0.5f, bottom, bottom, square),
                new Ring(height * 0.5f, top, top, square)
            }, segments, true);
        }

        // ================= scene helpers =================

        static Transform Joint(Transform parent, string name, Vector3 at, Vector3 euler = default)
        {
            var joint = new GameObject(name).transform;
            joint.SetParent(parent, false);
            joint.localPosition = at;
            joint.localRotation = Quaternion.Euler(euler);
            return joint;
        }

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

        static GameObject Blob(Transform parent, string name, Vector3 at, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static GameObject Disc(Transform parent, string name, Vector3 at, float diameter, float thickness,
            Material mat, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = new Vector3(diameter, thickness * 0.5f, diameter);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        // ================= the two humans =================

        static void Human(Transform root, int c, bool riding)
        {
            bool ace = c == 0;
            var k = new Palette
            {
                jacket = M(ace ? "aceJacket" : "novaJacket", ace ? new Color(1f, 0.36f, 0.04f) : new Color(0.58f, 0.14f, 0.88f)),
                jacketDark = M(ace ? "aceJacketDark" : "novaJacketDark", ace ? new Color(0.46f, 0.12f, 0.02f) : new Color(0.31f, 0.05f, 0.5f)),
                pants = M(ace ? "acePants" : "novaPants", ace ? new Color(0.07f, 0.075f, 0.1f) : new Color(0.16f, 0.1f, 0.26f)),
                pantsDark = M("crewPantsDark", new Color(0.035f, 0.04f, 0.055f)),
                shoe = M(ace ? "aceShoe" : "novaShoe", ace ? new Color(0.9f, 0.36f, 0.06f) : new Color(0.62f, 0.18f, 0.92f)),
                sole = M("crewSole", new Color(0.95f, 0.96f, 1f)),
                skin = M(ace ? "aceSkin" : "novaSkin", ace ? new Color(0.66f, 0.41f, 0.26f) : new Color(0.86f, 0.6f, 0.44f)),
                hair = M(ace ? "aceHair" : "novaHair", ace ? new Color(0.05f, 0.04f, 0.06f) : new Color(0.42f, 0.14f, 0.76f)),
                accent = M(ace ? "aceAccent" : "novaAccent", ace ? new Color(1f, 0.73f, 0.16f) : new Color(0.42f, 0.95f, 0.36f)),
                white = M("crewWhite", new Color(0.96f, 0.97f, 0.99f)),
                dark = M("crewDark", new Color(0.04f, 0.045f, 0.065f)),
                cyan = M("crewCyan", new Color(0.14f, 0.92f, 1f), 1.4f),
                metal = M("crewMetal", new Color(0.62f, 0.67f, 0.74f), 0f, 0.55f, 0.6f),
                glass = M("crewGlass", new Color(0.05f, 0.1f, 0.16f), 0.12f, 0.72f, 0.2f)
            };

            var pose = root.gameObject.AddComponent<DashPose>();
            pose.riding = riding;

            // Seated crouch on the board: hips 0.86 m up, feet on the deck at 0.10 m.
            // Standing: hips 0.92 m, feet on the pavement at 0 m.
            float hipY = riding ? 0.86f : 0.92f;
            float thighAngle = riding ? -52f : 0f;
            float kneeAngle = riding ? 62f : 0f;
            float footAngle = riding ? -10f : 0f;

            var hips = Joint(root, "Hips", new Vector3(0f, hipY, 0f));
            Part(hips, "Pelvis", Box("pelvis", new Vector3(0.27f, 0.21f, 0.2f), 0.05f), k.pants, new Vector3(0f, -0.01f, 0f));
            Part(hips, "Belt", Box("belt", new Vector3(0.285f, 0.05f, 0.205f), 0.018f), k.accent, new Vector3(0f, 0.085f, 0f));

            var torso = Joint(hips, "Torso", Vector3.zero, new Vector3(riding ? 18f : 0f, 0f, 0f));

            // The jacket: a lofted shell with a V taper and a puffed chest, cropped for Nova.
            float hem = ace ? -0.06f : 0.06f;
            Part(torso, "Jacket", Loft("jacket", new[]
            {
                new Ring(hem, 0.155f, 0.105f, 3f),
                new Ring(hem + 0.05f, 0.172f, 0.117f, 3f),
                new Ring(0.19f, 0.184f, 0.124f, 3.1f),
                new Ring(0.32f, 0.203f, 0.134f, 3.3f),
                new Ring(0.42f, 0.208f, 0.13f, 3.4f),
                new Ring(0.475f, 0.146f, 0.098f, 2.6f)
            }, 26), k.jacket, Vector3.zero);
            Part(torso, "Jacket hem", Loft("hem", new[]
            {
                new Ring(hem - 0.03f, 0.157f, 0.107f, 3f),
                new Ring(hem + 0.035f, 0.162f, 0.11f, 3f)
            }, 26), k.jacketDark, Vector3.zero);
            if (!ace)
                Part(torso, "Crop top", Loft("cropTop", new[]
                {
                    new Ring(-0.04f, 0.148f, 0.1f, 3f),
                    new Ring(0.12f, 0.158f, 0.107f, 3f)
                }, 24), k.dark, Vector3.zero);

            // Zip, chest pockets, collar and shoulder stripes.
            Part(torso, "Zip", Box("zip", new Vector3(0.018f, ace ? 0.44f : 0.3f, 0.014f), 0.005f), k.metal,
                new Vector3(0f, ace ? 0.22f : 0.28f, 0.126f));
            foreach (float side in new[] { -1f, 1f })
            {
                Part(torso, "Chest pocket", Box("pocket", new Vector3(0.064f, 0.052f, 0.016f), 0.01f), k.jacketDark,
                    new Vector3(side * 0.106f, 0.3f, 0.122f), new Vector3(0f, 0f, side * 6f));
                Part(torso, "Shoulder stripe", Box("stripe", new Vector3(0.08f, 0.026f, 0.12f), 0.008f), k.accent,
                    new Vector3(side * 0.188f, 0.405f, 0f), new Vector3(0f, 0f, side * 10f));
            }
            Part(torso, "Collar", Loft("collar", new[]
            {
                new Ring(0.455f, 0.128f, 0.106f, 2.4f),
                new Ring(0.515f, 0.144f, 0.12f, 2.4f)
            }, 22), k.jacketDark, Vector3.zero);
            Part(torso, "Neck", Loft("neck", new[]
            {
                new Ring(0.47f, 0.058f, 0.058f, 2f),
                new Ring(0.56f, 0.052f, 0.052f, 2f)
            }, 16), k.skin, Vector3.zero);

            if (ace)
            {
                // Charcoal scarf, one end trailing behind.
                Part(torso, "Scarf", Loft("scarf", new[]
                {
                    new Ring(0.465f, 0.132f, 0.11f, 2.4f),
                    new Ring(0.525f, 0.14f, 0.118f, 2.4f)
                }, 22), k.dark, Vector3.zero);
                Part(torso, "Scarf tail", Box("scarfTail", new Vector3(0.1f, 0.24f, 0.03f), 0.012f), k.dark,
                    new Vector3(0.06f, 0.36f, -0.118f), new Vector3(-12f, 0f, 4f));
            }
            else
            {
                Part(torso, "Zip pull", Box("zipPull", new Vector3(0.032f, 0.06f, 0.02f), 0.008f), k.accent,
                    new Vector3(0f, 0.36f, 0.128f));
                Part(torso, "Hem stripe", Loft("hemStripe", new[]
                {
                    new Ring(0.03f, 0.161f, 0.111f, 3f),
                    new Ring(0.06f, 0.163f, 0.113f, 3f)
                }, 24), k.accent, Vector3.zero);
            }

            // Delivery pack: rounded compartment, badge, straps.
            float packY = 0.29f, packZ = -0.19f;
            Part(torso, "Delivery pack", Box("pack", new Vector3(0.3f, 0.35f, 0.18f), 0.04f), k.dark,
                new Vector3(0f, packY, packZ));
            Part(torso, "Pack badge", Box("packBadge", new Vector3(0.19f, 0.17f, 0.022f), 0.012f), k.accent,
                new Vector3(0f, packY + 0.02f, packZ - 0.094f));
            Part(torso, "Pack stripe", Box("packStripe", new Vector3(0.042f, 0.13f, 0.014f), 0.005f), k.white,
                new Vector3(0f, packY + 0.02f, packZ - 0.107f));
            foreach (float side in new[] { -1f, 1f })
                Part(torso, "Pack strap", Box("strap", new Vector3(0.048f, 0.28f, 0.03f), 0.01f), k.jacketDark,
                    new Vector3(side * 0.1f, 0.32f, 0.095f), new Vector3(8f, 0f, 0f));

            // ---- head ----
            var head = Joint(torso, "Head", new Vector3(0f, 0.55f, 0f), new Vector3(riding ? -14f : 0f, 0f, 0f));
            Part(head, "Skull", Loft("skull", new[]
            {
                new Ring(0f, 0.052f, 0.058f, 2.2f),
                new Ring(0.03f, 0.084f, 0.09f, 2.2f),
                new Ring(0.08f, 0.103f, 0.108f, 2.1f),
                new Ring(0.15f, 0.111f, 0.116f, 2.1f),
                new Ring(0.21f, 0.1f, 0.105f, 2.2f),
                new Ring(0.25f, 0.062f, 0.066f, 2.2f),
                new Ring(0.275f, 0.02f, 0.022f, 2.2f)
            }, 26), k.skin, Vector3.zero);
            foreach (float side in new[] { -1f, 1f })
            {
                Blob(head, "Ear", new Vector3(side * 0.107f, 0.155f, 0f), new Vector3(0.038f, 0.054f, 0.032f), k.skin);
                Blob(head, "Eye white", new Vector3(side * 0.043f, 0.16f, 0.093f), new Vector3(0.04f, 0.044f, 0.026f), k.white);
                Blob(head, "Iris", new Vector3(side * 0.043f, 0.158f, 0.107f), new Vector3(0.022f, 0.024f, 0.013f),
                    ace ? M("aceEye", new Color(0.32f, 0.16f, 0.06f)) : M("novaEye", new Color(0.2f, 0.52f, 0.26f)));
                Blob(head, "Pupil", new Vector3(side * 0.043f, 0.157f, 0.114f), new Vector3(0.011f, 0.012f, 0.008f), k.dark);
                Part(head, "Brow", Box("brow", new Vector3(0.054f, 0.013f, 0.015f), 0.004f), k.hair,
                    new Vector3(side * 0.044f, 0.194f, 0.1f), new Vector3(0f, 0f, side * -7f));
            }
            Blob(head, "Nose", new Vector3(0f, 0.13f, 0.11f), new Vector3(0.027f, 0.036f, 0.034f), k.skin);
            Part(head, "Mouth", Box("mouth", new Vector3(0.048f, 0.01f, 0.014f), 0.003f), k.dark,
                new Vector3(0f, 0.084f, 0.104f));

            // Hair: cap over the skull, a swept fringe, sideburns.
            Part(head, "Hair cap", Loft("hairCap", new[]
            {
                new Ring(0.135f, 0.117f, 0.122f, 2.2f),
                new Ring(0.19f, 0.12f, 0.125f, 2.1f),
                new Ring(0.235f, 0.105f, 0.109f, 2.2f),
                new Ring(0.27f, 0.062f, 0.066f, 2.3f)
            }, 24), k.hair, Vector3.zero);
            for (int i = 0; i < 5; i++)
            {
                float x = -0.085f + i * 0.043f;
                float lift = 0.018f * Mathf.Sin(i * 1.3f);
                Part(head, "Fringe", Box("fringe", new Vector3(0.052f, 0.078f, 0.046f), 0.014f), k.hair,
                    new Vector3(x, 0.24f + lift, 0.078f), new Vector3(-28f, i * 9f - 18f, 0f));
            }
            foreach (float side in new[] { -1f, 1f })
                Part(head, "Sideburn", Box("sideburn", new Vector3(0.024f, 0.09f, 0.052f), 0.01f), k.hair,
                    new Vector3(side * 0.105f, 0.145f, 0.012f), new Vector3(0f, 0f, side * 4f));

            if (ace)
            {
                // Goggles pushed up on the forehead: strap, metal rims, amber lenses.
                Part(head, "Goggle strap", Loft("goggleStrap", new[]
                {
                    new Ring(0.2f, 0.119f, 0.124f, 2.2f),
                    new Ring(0.238f, 0.121f, 0.126f, 2.2f)
                }, 24), k.dark, Vector3.zero);
                foreach (float side in new[] { -1f, 1f })
                {
                    Disc(head, "Goggle rim", new Vector3(side * 0.052f, 0.224f, 0.1f), 0.088f, 0.03f, k.metal,
                        new Vector3(90f, 0f, 0f));
                    Disc(head, "Goggle lens", new Vector3(side * 0.052f, 0.224f, 0.114f), 0.07f, 0.012f,
                        M("aceLens", new Color(1f, 0.6f, 0.08f), 0.9f, 0.72f, 0.3f), new Vector3(90f, 0f, 0f));
                }
            }
            else
            {
                // High ponytail with a tie and a green streak.
                var tail = Joint(head, "Ponytail", new Vector3(0f, 0.23f, -0.09f), new Vector3(-104f, 0f, 0f));
                Part(tail, "Ponytail", Loft("ponytail", new[]
                {
                    new Ring(0f, 0.05f, 0.05f, 2f),
                    new Ring(0.1f, 0.058f, 0.058f, 2f),
                    new Ring(0.26f, 0.032f, 0.032f, 2f),
                    new Ring(0.34f, 0.009f, 0.009f, 2f)
                }, 16), k.hair, Vector3.zero);
                Disc(head, "Hair tie", new Vector3(0f, 0.24f, -0.086f), 0.072f, 0.042f, k.accent,
                    new Vector3(80f, 0f, 0f));
                Part(head, "Green streak", Box("streak", new Vector3(0.032f, 0.17f, 0.032f), 0.01f), k.accent,
                    new Vector3(0.076f, 0.228f, 0.06f), new Vector3(-25f, 0f, 12f));
                foreach (float side in new[] { -1f, 1f })
                    Disc(head, "Headset cup", new Vector3(side * 0.124f, 0.155f, 0f), 0.078f, 0.042f, k.dark,
                        new Vector3(0f, 0f, 90f));
                Part(head, "Headset band", Loft("headsetBand", new[]
                {
                    new Ring(0.2f, 0.129f, 0.133f, 2.2f),
                    new Ring(0.228f, 0.131f, 0.135f, 2.2f)
                }, 24), k.dark, Vector3.zero);
            }

            // ---- arms ----
            float armLift = riding ? -32f : 0f, armOut = riding ? 34f : 8f;
            var armL = Joint(torso, "Left arm", new Vector3(-0.212f, 0.42f, 0f), new Vector3(armLift, 0f, armOut));
            var armR = Joint(torso, "Right arm", new Vector3(0.212f, 0.42f, 0f), new Vector3(armLift, 0f, -armOut));
            foreach (var arm in new[] { armL, armR })
            {
                float side = arm == armL ? -1f : 1f;
                Part(arm, "Upper sleeve", Tube("sleeve", 0.073f, 0.062f, 0.26f), k.jacket, new Vector3(0f, -0.13f, 0f));
                var elbow = Joint(arm, side < 0 ? "Left elbow" : "Right elbow", new Vector3(0f, -0.26f, 0f),
                    new Vector3(riding ? -26f : -6f, 0f, 0f));
                Part(elbow, "Forearm sleeve", Tube("forearm", 0.059f, 0.05f, 0.23f), k.jacket,
                    new Vector3(0f, -0.115f, 0f));
                Part(elbow, "Cuff", Loft("cuff", new[]
                {
                    new Ring(-0.238f, 0.057f, 0.057f, 2f),
                    new Ring(-0.212f, 0.063f, 0.063f, 2f)
                }, 16), k.accent, Vector3.zero);
                Blob(elbow, "Glove", new Vector3(0f, -0.272f, 0.014f), new Vector3(0.076f, 0.092f, 0.076f), k.dark);
                Part(elbow, "Thumb", Box("thumb", new Vector3(0.03f, 0.052f, 0.034f), 0.01f), k.dark,
                    new Vector3(side * 0.046f, -0.266f, 0.032f), new Vector3(0f, 0f, side * 25f));
                if (side < 0) { pose.leftArm = arm; pose.leftElbow = elbow; }
                else { pose.rightArm = arm; pose.rightElbow = elbow; }
            }

            // ---- legs ----
            var legL = Joint(hips, "Left leg", new Vector3(-0.088f, -0.02f, 0f), new Vector3(thighAngle, 0f, riding ? 2f : 0f));
            var legR = Joint(hips, "Right leg", new Vector3(0.088f, -0.02f, 0f), new Vector3(thighAngle, 0f, riding ? -2f : 0f));
            foreach (var leg in new[] { legL, legR })
            {
                float side = leg == legL ? -1f : 1f;
                Part(leg, "Thigh", Tube("thigh", 0.08f, 0.068f, 0.42f, 2.2f), k.pants, new Vector3(0f, -0.21f, 0f));
                Part(leg, "Cargo pocket", Box("cargo", new Vector3(0.056f, 0.115f, 0.088f), 0.016f), k.pantsDark,
                    new Vector3(side * 0.064f, -0.2f, 0.014f));
                var knee = Joint(leg, side < 0 ? "Left knee" : "Right knee", new Vector3(0f, -0.42f, 0f),
                    new Vector3(kneeAngle, 0f, 0f));
                Part(knee, "Knee pad", Box("kneePad", new Vector3(0.104f, 0.115f, 0.078f), 0.022f), k.accent,
                    new Vector3(0f, -0.012f, 0.06f));
                Part(knee, "Shin", Tube("shin", 0.067f, 0.053f, 0.4f, 2.2f), k.pants, new Vector3(0f, -0.2f, 0f));
                var foot = Joint(knee, side < 0 ? "Left foot" : "Right foot", new Vector3(0f, -0.4f, 0f),
                    new Vector3(footAngle, 0f, 0f));
                Part(foot, "Sneaker", Box("shoe", new Vector3(0.118f, 0.09f, 0.275f), 0.032f), k.shoe,
                    new Vector3(0f, -0.045f, 0.06f));
                Part(foot, "Toe cap", Box("toe", new Vector3(0.108f, 0.058f, 0.085f), 0.024f), k.pantsDark,
                    new Vector3(0f, -0.058f, 0.196f));
                Part(foot, "Luminous sole", Box("sole", new Vector3(0.124f, 0.032f, 0.29f), 0.012f), k.cyan,
                    new Vector3(0f, -0.088f, 0.06f));
                Part(foot, "Heel tab", Box("heel", new Vector3(0.052f, 0.072f, 0.022f), 0.008f), k.accent,
                    new Vector3(0f, -0.008f, -0.078f));
                if (side < 0) { pose.leftLeg = leg; pose.leftKnee = knee; }
                else { pose.rightLeg = leg; pose.rightKnee = knee; }
            }

            // Outfit token: swap the jacket colour at runtime.
            var outfit = root.gameObject.AddComponent<CrewOutfit>();
            outfit.character = c;
            outfit.baseJacket = k.jacket;
            outfit.altJacket = M(ace ? "aceJacketAlt" : "novaJacketAlt",
                ace ? new Color(0.06f, 0.72f, 0.95f) : new Color(1f, 0.12f, 0.52f));
        }

        // ================= Bolt =================

        static void Bolt(Transform root)
        {
            var silver = M("boltSilver", new Color(0.68f, 0.73f, 0.8f), 0f, 0.5f, 0.55f);
            var silverDark = M("boltSilverDark", new Color(0.4f, 0.45f, 0.53f), 0f, 0.45f, 0.4f);
            var magenta = M("boltMagenta", new Color(0.93f, 0.08f, 0.46f));
            var magentaDark = M("boltMagentaDark", new Color(0.5f, 0.03f, 0.24f));
            var graphite = M("boltGraphite", new Color(0.07f, 0.08f, 0.11f));
            var lime = M("boltLime", new Color(0.62f, 0.93f, 0.16f));
            var cyan = M("boltCyan", new Color(0.12f, 0.92f, 1f), 1.6f);
            var visor = M("boltVisor", new Color(0.02f, 0.03f, 0.05f), 0.1f, 0.78f, 0.2f);

            var pose = root.gameObject.AddComponent<DashPose>();
            pose.robot = true;
            pose.riding = true;

            // One thick wheel, touching the road at y = 0. WheelSpin turns it with the run.
            var wheel = Joint(root, "Monowheel", new Vector3(0f, 0.33f, 0f));
            Disc(wheel, "Tire", Vector3.zero, 0.66f, 0.26f, graphite, new Vector3(0f, 0f, 90f));
            Disc(wheel, "Rim", Vector3.zero, 0.44f, 0.28f, silver, new Vector3(0f, 0f, 90f));
            Disc(wheel, "Hub", Vector3.zero, 0.16f, 0.3f, magenta, new Vector3(0f, 0f, 90f));
            Disc(wheel, "Hub light", new Vector3(0f, 0f, 0.153f), 0.09f, 0.02f, cyan, new Vector3(0f, 0f, 90f));
            Disc(wheel, "Hub light", new Vector3(0f, 0f, -0.153f), 0.09f, 0.02f, cyan, new Vector3(0f, 0f, 90f));
            wheel.gameObject.AddComponent<WheelSpin>();

            // Fairings either side of the wheel, then the chassis that carries the rider.
            foreach (float side in new[] { -1f, 1f })
            {
                Part(root, "Side fairing", Loft("fairing", new[]
                {
                    new Ring(0.16f, 0.1f, 0.24f, 3f),
                    new Ring(0.42f, 0.115f, 0.3f, 3f),
                    new Ring(0.7f, 0.09f, 0.24f, 3f)
                }, 20), magenta, new Vector3(side * 0.16f, 0f, 0f), new Vector3(0f, 0f, side * 6f));
                Part(root, "Fairing light", Box("fairingLight", new Vector3(0.03f, 0.26f, 0.05f), 0.01f), cyan,
                    new Vector3(side * 0.216f, 0.42f, 0.14f), new Vector3(0f, 0f, side * 6f));
                Part(root, "Footpeg", Box("peg", new Vector3(0.14f, 0.038f, 0.2f), 0.01f), graphite,
                    new Vector3(side * 0.22f, 0.4f, 0.06f));
            }
            Part(root, "Chassis", Loft("chassis", new[]
            {
                new Ring(0.3f, 0.19f, 0.2f, 3f),
                new Ring(0.52f, 0.22f, 0.24f, 3f),
                new Ring(0.74f, 0.2f, 0.22f, 3f),
                new Ring(0.82f, 0.16f, 0.18f, 3f)
            }, 24), silver, Vector3.zero);
            Part(root, "Chassis plate", Box("plate", new Vector3(0.26f, 0.2f, 0.032f), 0.012f), magenta,
                new Vector3(0f, 0.6f, 0.206f));
            Part(root, "Charge display", Box("display", new Vector3(0.16f, 0.07f, 0.014f), 0.006f), cyan,
                new Vector3(0f, 0.62f, 0.224f));
            Part(root, "Seat", Loft("seat", new[]
            {
                new Ring(0.78f, 0.16f, 0.19f, 3f),
                new Ring(0.84f, 0.17f, 0.2f, 3f)
            }, 22), graphite, Vector3.zero);

            // Rider torso: a compact chassis with a jacket-like magenta shell.
            var torso = Joint(root, "Torso", new Vector3(0f, 0.84f, 0f), new Vector3(10f, 0f, 0f));
            Part(torso, "Chest", Loft("chest", new[]
            {
                new Ring(0f, 0.16f, 0.13f, 3f),
                new Ring(0.1f, 0.19f, 0.15f, 3f),
                new Ring(0.26f, 0.2f, 0.15f, 3.2f),
                new Ring(0.34f, 0.13f, 0.11f, 2.6f)
            }, 24), magenta, Vector3.zero);
            Part(torso, "Chest panel", Box("chestPanel", new Vector3(0.2f, 0.2f, 0.032f), 0.012f), silver,
                new Vector3(0f, 0.18f, 0.142f));
            Part(torso, "Chest light", Box("chestLight", new Vector3(0.12f, 0.05f, 0.014f), 0.005f), cyan,
                new Vector3(0f, 0.2f, 0.16f));
            Part(torso, "Collar", Loft("collar", new[]
            {
                new Ring(0.33f, 0.13f, 0.115f, 2.6f),
                new Ring(0.39f, 0.115f, 0.1f, 2.6f)
            }, 22), graphite, Vector3.zero);

            // Helmet: magenta shell, black visor band, cyan LED eyes and a smile.
            var head = Joint(torso, "Head", new Vector3(0f, 0.4f, 0f), new Vector3(-6f, 0f, 0f));
            Part(head, "Helmet", Loft("helmet", new[]
            {
                new Ring(0f, 0.115f, 0.12f, 2.6f),
                new Ring(0.05f, 0.145f, 0.15f, 2.4f),
                new Ring(0.16f, 0.155f, 0.16f, 2.3f),
                new Ring(0.24f, 0.135f, 0.14f, 2.4f),
                new Ring(0.29f, 0.07f, 0.075f, 2.5f)
            }, 26), magenta, Vector3.zero);
            Part(head, "Visor band", Box("visorBand", new Vector3(0.27f, 0.13f, 0.06f), 0.03f), visor,
                new Vector3(0f, 0.15f, 0.112f));
            foreach (float side in new[] { -1f, 1f })
            {
                Part(head, "LED eye", Box("eye", new Vector3(0.055f, 0.05f, 0.02f), 0.012f), cyan,
                    new Vector3(side * 0.062f, 0.17f, 0.147f));
                Part(head, "Helmet fin", Box("fin", new Vector3(0.03f, 0.1f, 0.16f), 0.01f), silver,
                    new Vector3(side * 0.15f, 0.2f, 0f), new Vector3(0f, 0f, side * 12f));
            }
            Part(head, "LED smile", Box("smile", new Vector3(0.1f, 0.018f, 0.014f), 0.005f), cyan,
                new Vector3(0f, 0.1f, 0.144f));
            Part(head, "Antenna", Loft("antenna", new[]
            {
                new Ring(0f, 0.014f, 0.014f, 2f),
                new Ring(0.16f, 0.009f, 0.009f, 2f)
            }, 10), silver, new Vector3(0.09f, 0.29f, -0.02f), new Vector3(0f, 0f, 12f));
            Blob(head, "Antenna light", new Vector3(0.106f, 0.45f, -0.02f), Vector3.one * 0.046f, cyan);

            // Handlebar and arms reaching for it.
            Part(root, "Handlebar", Box("bar", new Vector3(0.5f, 0.042f, 0.052f), 0.012f), graphite,
                new Vector3(0f, 1.03f, 0.27f));
            foreach (float side in new[] { -1f, 1f })
                Disc(root, "Grip", new Vector3(side * 0.24f, 1.03f, 0.27f), 0.058f, 0.13f, magentaDark,
                    new Vector3(0f, 0f, 90f));
            var armL = Joint(torso, "Left arm", new Vector3(-0.2f, 0.26f, 0f), new Vector3(-52f, 0f, 14f));
            var armR = Joint(torso, "Right arm", new Vector3(0.2f, 0.26f, 0f), new Vector3(-52f, 0f, -14f));
            foreach (var arm in new[] { armL, armR })
            {
                float side = arm == armL ? -1f : 1f;
                Part(arm, "Upper arm", Tube("upperArm", 0.058f, 0.05f, 0.22f), magenta, new Vector3(0f, -0.11f, 0f));
                var elbow = Joint(arm, side < 0 ? "Left elbow" : "Right elbow", new Vector3(0f, -0.22f, 0f),
                    new Vector3(18f, 0f, 0f));
                Part(elbow, "Forearm", Tube("forearm", 0.048f, 0.042f, 0.19f), silver, new Vector3(0f, -0.095f, 0f));
                Blob(elbow, "Glove", new Vector3(0f, -0.2f, 0.006f), new Vector3(0.07f, 0.076f, 0.07f), graphite);
                if (side < 0) { pose.leftArm = arm; pose.leftElbow = elbow; }
                else { pose.rightArm = arm; pose.rightElbow = elbow; }
            }

            // Legs tucked onto the footpegs.
            var legL = Joint(root, "Left leg", new Vector3(-0.1f, 0.78f, 0f), new Vector3(-55f, 0f, 4f));
            var legR = Joint(root, "Right leg", new Vector3(0.1f, 0.78f, 0f), new Vector3(-55f, 0f, -4f));
            foreach (var leg in new[] { legL, legR })
            {
                float side = leg == legL ? -1f : 1f;
                Part(leg, "Thigh", Tube("thigh", 0.062f, 0.055f, 0.26f), graphite, new Vector3(0f, -0.13f, 0f));
                var knee = Joint(leg, side < 0 ? "Left knee" : "Right knee", new Vector3(0f, -0.26f, 0f),
                    new Vector3(82f, 0f, 0f));
                Part(knee, "Shin", Tube("shin", 0.052f, 0.045f, 0.24f), graphite, new Vector3(0f, -0.12f, 0f));
                var foot = Joint(knee, side < 0 ? "Left foot" : "Right foot", new Vector3(0f, -0.24f, 0f),
                    new Vector3(-30f, 0f, 0f));
                Part(foot, "Boot", Box("boot", new Vector3(0.1f, 0.078f, 0.22f), 0.022f), graphite,
                    new Vector3(0f, -0.03f, 0.05f));
                Part(foot, "Boot trim", Box("bootTrim", new Vector3(0.105f, 0.026f, 0.2f), 0.01f), lime,
                    new Vector3(0f, -0.064f, 0.05f));
                if (side < 0) { pose.leftLeg = leg; pose.leftKnee = knee; }
                else { pose.rightLeg = leg; pose.rightKnee = knee; }
            }

            // Rear delivery trunk and panniers.
            Part(root, "Cargo trunk", Box("trunk", new Vector3(0.38f, 0.34f, 0.26f), 0.035f), silver,
                new Vector3(0f, 1.0f, -0.34f));
            Part(root, "Trunk lid", Box("lid", new Vector3(0.4f, 0.06f, 0.28f), 0.018f), magenta,
                new Vector3(0f, 1.185f, -0.34f));
            Part(root, "Trunk badge", Box("badge", new Vector3(0.2f, 0.16f, 0.022f), 0.01f), magenta,
                new Vector3(0f, 1.0f, -0.477f));
            Part(root, "Trunk stripe", Box("badgeStripe", new Vector3(0.045f, 0.12f, 0.014f), 0.005f), lime,
                new Vector3(0f, 1.0f, -0.489f));
            foreach (float side in new[] { -1f, 1f })
            {
                Part(root, "Pannier", Box("pannier", new Vector3(0.17f, 0.26f, 0.24f), 0.03f), magenta,
                    new Vector3(side * 0.26f, 0.86f, -0.3f));
                Part(root, "Pannier light", Box("pannierLight", new Vector3(0.02f, 0.12f, 0.16f), 0.008f), cyan,
                    new Vector3(side * 0.352f, 0.86f, -0.3f));
            }
            Part(root, "Tail light", Box("tail", new Vector3(0.24f, 0.05f, 0.022f), 0.008f), cyan,
                new Vector3(0f, 0.93f, -0.48f));

            var outfit = root.gameObject.AddComponent<CrewOutfit>();
            outfit.character = 2;
            outfit.baseJacket = magenta;
            outfit.altJacket = M("boltMagentaAlt", new Color(0.1f, 0.78f, 0.98f));
        }

        sealed class Palette
        {
            public Material jacket, jacketDark, pants, pantsDark, shoe, sole, skin, hair, accent;
            public Material white, dark, cyan, metal, glass;
        }
    }
}
