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
        public static GameObject Crew(int character)
        {
            if (character == 2) return CrewModels.Build(character, true);
            return Cyclist(character == 0 ? "ace" : "nova", character == 0 ? "ebike" : "ebike-nova");
        }

        /// Ace or Nova on foot (after a crash): the original mesh in a running stride with morph keys.
        public static GameObject Runner(int character)
        {
            string id = character == 0 ? "ace" : "nova";
            var model = ModelImport.Get(id + "-run");
            if (!model) throw new System.InvalidOperationException("Required runner model missing: " + id);
            var root = new GameObject(id + " on foot");
            var body = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            body.name = "Rider";
            var motion = root.AddComponent<DallasDash.Gameplay.RunnerMotion>();
            motion.body = body.GetComponentInChildren<SkinnedMeshRenderer>();
            if (!motion.body || motion.body.sharedMesh.GetBlendShapeIndex("Run180") < 0)
                throw new System.InvalidOperationException("Runner mesh has no stride morphs: " + id);
            return Save(root, id + "-runner");
        }

        /// The rider seated on a courier e-bike (PDF page 2): both meshes share bike space, ground at y = 0.
        static GameObject Cyclist(string riderId, string bikeId)
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
            return Save(root, riderId + "-cyclist");
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
