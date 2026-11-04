using System.Collections.Generic;
using DallasDash.Gameplay;
using UnityEditor;
using UnityEngine;

namespace DallasDash.EditorTools
{
    public static class DashAssets
    {
        const string Root = "Assets/_Project/Art/DashRiders";
        static readonly Dictionary<string, Material> mats = new();
        public static void ImportVisuals()
        {
            AssetDatabase.Refresh();
            string path = Root + "/Visuals/dash-riders-logo.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true; importer.maxTextureSize = 2048; importer.SaveAndReimport();
            }
        }
        public static Material HologramMaterial()
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Hologram.mat");
            if (!m) { m = new Material(Shader.Find("FoodInc/HologramCurved")); AssetDatabase.CreateAsset(m, Root + "/Hologram.mat"); }
            m.shader = Shader.Find("FoodInc/HologramCurved"); m.SetColor("_BaseColor", Color.white); EditorUtility.SetDirty(m); return m;
        }
        static Material Mat(string id, Color color, float glow = 0f)
        {
            if (glow > 0f && WorldPalette.IsCoolNeon(color)) { color = WorldPalette.Surface(color); glow = 0f; }
            if (mats.TryGetValue(id, out var m) && m) return m;
            System.IO.Directory.CreateDirectory(Root);
            m = AssetDatabase.LoadAssetAtPath<Material>(Root + "/" + id + ".mat");
            if (!m) { m = new Material(Shader.Find("FoodInc/ToonCurved")); AssetDatabase.CreateAsset(m, Root + "/" + id + ".mat"); }
            m.SetColor("_BaseColor", color); m.SetColor("_EmissionColor", color * glow);
            m.SetFloat("_OutlineWidth", 0.008f); m.SetShaderPassEnabled("Outline", false);
            EditorUtility.SetDirty(m); mats[id] = m; return m;
        }
        static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 at, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(type); Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = at; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat; return go;
        }
        static Transform Joint(Transform parent, string name, Vector3 at)
        {
            var joint = new GameObject(name).transform; joint.SetParent(parent, false); joint.localPosition = at; return joint;
        }
        static GameObject Save(GameObject root, string name)
        {
            System.IO.Directory.CreateDirectory(Root);
            var result = PrefabUtility.SaveAsPrefabAsset(root, Root + "/" + name + ".prefab");
            Object.DestroyImmediate(root); return result;
        }
        /// Lettering for the courier boxes (set by the bootstrap: the sign font and its curved text material).
        public static Font BrandFont;
        public static Material BrandText;

        /// Ace on his heavy hero e-bike (client, 4 Nov 2026): the original mesh re-posed, the hero kit posed with
        /// the same morphs, and a KFC courier box.
        public static GameObject Crew(int character) => Cyclist("ace", "ebike", "ace-gear-ride", "KFC", new Color(0.89f, 0.1f, 0.17f), "ace-cyclist");

        /// The officer for the chase: Ace's build in police colours and kit, on the police e-bike.
        public static GameObject Police() => Cyclist("police", "ebike-police", "police-gear-ride", "POLICE", new Color(0.06f, 0.12f, 0.42f), "police-cyclist");

        /// Ace or Nova on foot (after a crash): the original mesh in a running stride with morph keys.
        public static GameObject Runner(int character)
        {
            string id = "ace";
            var model = ModelImport.Get(id + "-run");
            if (!model) throw new System.InvalidOperationException("Required runner model missing: " + id);
            var root = new GameObject(id + " on foot");
            var body = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            body.name = "Rider";
            var motion = root.AddComponent<DallasDash.Gameplay.RunnerMotion>();
            motion.body = body.GetComponentInChildren<SkinnedMeshRenderer>();
            motion.gear = new[] { Gear(root, id + "-gear-run", "Run180") };
            if (!motion.body || motion.body.sharedMesh.GetBlendShapeIndex("Run180") < 0)
                throw new System.InvalidOperationException("Runner mesh has no stride morphs: " + id);
            return Save(root, id + "-runner");
        }

        /// The rider seated on a courier e-bike (PDF page 2): both meshes share bike space, ground at y = 0.
        /// The hero kit as its own skinned mesh beside the rider (same bike space, same morph names).
        static SkinnedMeshRenderer Gear(GameObject root, string gearId, string morph)
        {
            var gearPrefab = ModelImport.Get(gearId);
            if (!gearPrefab) throw new System.InvalidOperationException("Required gear model missing: " + gearId);
            var gear = (GameObject)PrefabUtility.InstantiatePrefab(gearPrefab, root.transform);
            gear.name = "Hero gear";
            var skin = gear.GetComponentInChildren<SkinnedMeshRenderer>();
            if (!skin || skin.sharedMesh.GetBlendShapeIndex(morph) < 0)
                throw new System.InvalidOperationException("Gear mesh has no pose morphs: " + gearId);
            return skin;
        }

        /// The brand on the courier box's sides and back (box centre/size from models/make_ebike.py heavy): the
        /// official logo for KFC (4 Nov 2026: logos, not text), plain lettering for the police box.
        static void BoxBrand(GameObject root, string word, Color ink)
        {
            if (word == "KFC") { BoxLogo(root); return; }
            if (!BrandFont || !BrandText) throw new System.InvalidOperationException("Box lettering needs the sign font");
            var box = new Vector3(0f, 1.245f, -0.66f);
            var half = new Vector3(0.3f, 0.25f, 0.28f);
            var faces = new (Vector3 at, float yaw, float size)[]
            {
                (box + new Vector3(half.x + 0.012f, 0f, 0f), -90f, 0.0165f),
                (box + new Vector3(-half.x - 0.012f, 0f, 0f), 90f, 0.0165f),
                (box + new Vector3(0f, 0f, -half.z - 0.012f), 0f, 0.0165f),
            };
            foreach (var f in faces)
            {
                var go = new GameObject("Box brand " + word);
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = f.at;
                go.transform.localRotation = Quaternion.Euler(0f, f.yaw, 0f);
                var tm = go.AddComponent<TextMesh>();
                tm.text = word; tm.font = BrandFont; tm.fontSize = 96; tm.characterSize = f.size * (word.Length > 4 ? 0.62f : 1f);
                tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = Color.white;
                go.GetComponent<MeshRenderer>().sharedMaterial = BrandText;
                go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        static void BoxLogo(GameObject root)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Brands/kfc-square.png");
            if (!tex) throw new System.InvalidOperationException("Missing KFC logo sign texture");
            var m = AssetDatabase.LoadAssetAtPath<Material>(Root + "/BrandBoxKFC.mat");
            if (!m) { m = new Material(Shader.Find("FoodInc/ToonCurved")); AssetDatabase.CreateAsset(m, Root + "/BrandBoxKFC.mat"); }
            DallasDash.Gameplay.BrandVisibility.Backlit(m, tex);
            m.SetFloat("_OutlineWidth", 0f);
            m.SetShaderPassEnabled("Outline", false);
            EditorUtility.SetDirty(m);
            var box = new Vector3(0f, 1.245f, -0.66f);
            var half = new Vector3(0.3f, 0.25f, 0.28f);
            var faces = new (Vector3 at, float yaw)[]
            {
                (box + new Vector3(half.x + 0.012f, -0.01f, 0f), -90f),
                (box + new Vector3(-half.x - 0.012f, -0.01f, 0f), 90f),
                (box + new Vector3(0f, -0.01f, -half.z - 0.012f), 0f),
            };
            foreach (var f in faces)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(q.GetComponent<Collider>());
                q.name = "Box brand KFC logo";
                q.transform.SetParent(root.transform, false);
                q.transform.localPosition = f.at;
                q.transform.localRotation = Quaternion.Euler(0f, f.yaw, 0f);
                q.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
                var r = q.GetComponent<MeshRenderer>();
                r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                q.AddComponent<DallasDash.Gameplay.BrandMarkTag>().rider = true;
            }
        }

        static GameObject Cyclist(string riderId, string bikeId, string gearId, string brand, Color ink, string prefabName)
        {
            var riderPrefab = ModelImport.Get(riderId + "-ride");
            var bikePrefab = ModelImport.Get(bikeId);
            if (!riderPrefab || !bikePrefab) throw new System.InvalidOperationException("Required cyclist model missing: " + riderId);
            var root = new GameObject(riderId + " on e-bike");
            var bike = (GameObject)PrefabUtility.InstantiatePrefab(bikePrefab, root.transform);
            bike.name = "E-bike";
            var rider = (GameObject)PrefabUtility.InstantiatePrefab(riderPrefab, root.transform);
            rider.name = "Rider";
            var motion = root.AddComponent<DallasDash.Gameplay.CyclistMotion>();
            motion.body = rider.GetComponentInChildren<SkinnedMeshRenderer>();
            motion.gear = new[] { Gear(root, gearId, "Pedal180") };
            BoxBrand(root, brand, ink);
            Transform Find(string node)
            {
                foreach (var t in bike.GetComponentsInChildren<Transform>(true)) if (t.name == node) return t;
                throw new System.InvalidOperationException("E-bike node missing: " + node);
            }
            motion.crank = Find("Crank");
            motion.frontWheel = Find("Front wheel");
            motion.rearWheel = Find("Rear wheel");
            if (!motion.body || motion.body.sharedMesh.GetBlendShapeIndex("Pedal180") < 0)
                throw new System.InvalidOperationException("Cyclist mesh has no pedal morphs: " + riderId);
            return Save(root, prefabName);
        }
        public static GameObject Board(bool upgraded = false)
        {
            var root = new GameObject("Electric board");
            var dark = Mat("cargo", new Color(0.055f, 0.065f, 0.1f));
            var led = Mat("led", new Color(0.12f, 0.9f, 1f), 1.1f);
            Part(root.transform, "Deck", PrimitiveType.Cube, new Vector3(0, 0.04f, 0), new Vector3(0.72f, 0.12f, 1.25f), dark);
            foreach (float tip in new[] { -.54f, .54f })
                Part(root.transform, "Rounded deck tip", PrimitiveType.Sphere, new Vector3(0,.04f,tip), new Vector3(.72f,.12f,.34f), dark);
            Part(root.transform, "Grip pad", PrimitiveType.Cube, new Vector3(0,.108f,0), new Vector3(.55f,.018f,.9f), Mat("boardGrip", new Color(.16f,.20f,.27f)));
            foreach (float side in new[] { -1f, 1f })
                Part(root.transform, "Edge glow", PrimitiveType.Cube, new Vector3(side * 0.35f, 0.035f, 0), new Vector3(0.035f, 0.1f, 1.18f), led);
            foreach (float z in new[] { -0.4f, 0.4f })
                Part(root.transform, "Hover engine", PrimitiveType.Cube, new Vector3(0, -0.045f, z), new Vector3(0.54f, 0.1f, 0.18f), led);
            return Save(root, upgraded ? "GlideBoard" : "ElectricBoard");
        }
        public static GameObject Pickup(string id)
        {
            string model = id == "energy-chip" ? "courier-chip" : id == "soft-drink" ? "food-drink" : "food-" + id;
            var prefab = ModelImport.Get("free-" + model);
            if (!prefab) throw new System.InvalidOperationException("Required pickup model missing: " + id);
            var root = new GameObject(id);
            var visual = Object.Instantiate(prefab, root.transform);
            visual.transform.localScale *= id == "energy-chip" ? .6f : .72f;
            return Save(root, id);
        }
    }
}
