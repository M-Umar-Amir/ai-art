using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DallasDash.EditorTools
{
    /// Flattens the Quaternius Downtown City MegaKit pieces (Art/Models/Kit/city.glb, CC0, converted from the web
    /// build by tools/gltf/unpack-for-unity.mjs) into one readable mesh per piece with a submesh per material,
    /// and shares one toon curved-world material per kit material. Spawned pieces then bend with the street and
    /// static-batch with the rest of a segment.
    public static class KitImport
    {
        const string Src = "Assets/_Project/Art/Models/Kit/city.glb";
        /// Street props and trees: Quaternius Stylized Nature trees, Poly Haven street furniture (CC0).
        const string PropSrc = "Assets/_Project/Art/Models/Kit/props.glb";
        const string MeshDir = "Assets/_Project/Art/Kit/Meshes";
        const string PrefabDir = "Assets/_Project/Prefabs/Kit";
        const string MatDir = "Assets/_Project/Materials/Kit";

        /// Painted-plaster colours for the trim (cream render) material: teal, mint, pink, butter, sky, cream.
        public static readonly Color[] Paints =
        {
            new(0.42f, 0.78f, 0.8f), new(0.7f, 0.92f, 0.78f), new(1f, 0.72f, 0.74f),
            new(1f, 0.9f, 0.6f), new(0.66f, 0.8f, 1f), new(1f, 0.96f, 0.88f),
        };

        static readonly Dictionary<string, GameObject> cache = new();
        static readonly Dictionary<string, Material> mats = new();

        public static bool Available => AssetDatabase.LoadAssetAtPath<GameObject>(Src);

        /// Prefab for one kit piece by its node name (e.g. "Brick_Window_Square_Single"), or null.
        public static GameObject Get(string piece) => Get(Src, piece);

        /// Prefab for one street prop or tree from props.glb, or null.
        public static GameObject GetProp(string piece) => Get(PropSrc, piece);

        /// The .glb a piece is being flattened from, so Toon() can bake its textures out of it.
        static string currentSrc;

        static GameObject Get(string srcPath, string piece)
        {
            currentSrc = srcPath;
            if (cache.TryGetValue(piece, out var hit) && hit) return hit;
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(srcPath);
            if (!src) return null;
            var node = Find(src.transform, piece);
            if (!node) { Debug.LogWarning("[DallasDash] Kit piece missing: " + piece); return null; }
            var prefab = Build(piece, node);
            cache[piece] = prefab;
            return prefab;
        }

        public static Material Material(string kitMaterial) => mats.TryGetValue(kitMaterial, out var m) ? m : null;

        /// The trim material repainted, one per Paints entry, for plaster-fronted buildings.
        public static Material[] PaintVariants()
        {
            var baseMat = Material("MI_Trim");
            if (!baseMat) return new Material[0];
            var list = new Material[Paints.Length];
            for (int i = 0; i < Paints.Length; i++)
            {
                string path = $"{MatDir}/MI_Trim_Paint{i}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!m) { m = new Material(baseMat); AssetDatabase.CreateAsset(m, path); }
                m.CopyPropertiesFromMaterial(baseMat);
                m.SetColor("_BaseColor", Paints[i]);
                EditorUtility.SetDirty(m);
                list[i] = m;
            }
            return list;
        }

        public const string FrameMaterial = "MI_Trim_MetalConcrete";

        /// Lit shop-window glass for the ground floor: warm interior glow instead of the cool upstairs glass.
        public static Material ShopGlass()
        {
            var glass = Material("MI_Glass");
            if (!glass) return null;
            string path = $"{MatDir}/MI_ShopGlass.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(glass); AssetDatabase.CreateAsset(m, path); }
            m.CopyPropertiesFromMaterial(glass);
            m.SetColor("_BaseColor", new Color(1f, 0.88f, 0.62f));
            m.SetColor("_EmissionColor", new Color(0.32f, 0.24f, 0.12f));
            m.SetFloat("_RimStrength", 0.5f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Transform Find(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t)
            {
                var f = Find(c, name);
                if (f) return f;
            }
            return null;
        }

        static GameObject Build(string piece, Transform node)
        {
            Directory.CreateDirectory(MeshDir);
            Directory.CreateDirectory(PrefabDir);
            Directory.CreateDirectory(MatDir);

            // Group every sub-mesh under the node by material, in the node's own space.
            var groups = new Dictionary<Material, List<CombineInstance>>();
            var order = new List<Material>();
            var toLocal = node.worldToLocalMatrix;
            foreach (var r in node.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mf = r.GetComponent<MeshFilter>();
                if (!mf || !mf.sharedMesh) continue;
                var shared = r.sharedMaterials;
                for (int s = 0; s < mf.sharedMesh.subMeshCount; s++)
                {
                    var src = Toon(s < shared.Length ? shared[s] : null);
                    if (!groups.TryGetValue(src, out var list)) { groups[src] = list = new List<CombineInstance>(); order.Add(src); }
                    list.Add(new CombineInstance { mesh = mf.sharedMesh, subMeshIndex = s, transform = toLocal * r.transform.localToWorldMatrix });
                }
            }

            var parts = new List<CombineInstance>();
            foreach (var m in order)
            {
                var g = new Mesh();
                g.CombineMeshes(groups[m].ToArray(), true, true);
                parts.Add(new CombineInstance { mesh = g, transform = Matrix4x4.identity });
            }
            var mesh = new Mesh { name = piece };
            mesh.CombineMeshes(parts.ToArray(), false, false);
            mesh.RecalculateBounds();
            foreach (var p in parts) Object.DestroyImmediate(p.mesh);
            string meshPath = $"{MeshDir}/{piece}.asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (old) { EditorUtility.CopySerialized(mesh, old); mesh = old; }
            else AssetDatabase.CreateAsset(mesh, meshPath);

            var go = new GameObject(piece, typeof(MeshFilter), typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterials = order.ToArray();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{piece}.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// One shared toon material per kit material, keeping its texture and base colour.
        static Material Toon(Material src)
        {
            string name = src ? src.name.Replace(" (Instance)", "") : "MI_Default";
            if (mats.TryGetValue(name, out var have) && have) return have;
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find("FoodInc/ToonCurved")); AssetDatabase.CreateAsset(m, path); }
            m.shader = Shader.Find("FoodInc/ToonCurved");
            Texture tex = null;
            if (src)
                foreach (var p in new[] { "baseColorTexture", "_BaseMap", "_MainTex", "_BaseColorMap" })
                    if (src.HasProperty(p) && src.GetTexture(p)) { tex = src.GetTexture(p); break; }
            // Compressed copy of the kit texture (TextureBake) in place of glTFast's uncompressed one.
            if (tex && currentSrc != null)
            {
                var baked = TextureBake.FromGlb(currentSrc, name, "kit-" + name, 1024);
                if (baked) tex = baked;
            }
            m.SetTexture("_BaseMap", tex);
            var c = src && src.HasProperty("baseColorFactor") ? src.GetColor("baseColorFactor") : src && src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : Color.white;
            // Opaque toon glass: a cool sky-blue that reads as reflective in daylight (the kit's is near-black, 58% alpha).
            if (name == "MI_Glass") c = new Color(0.45f, 0.62f, 0.8f);
            if (name == "MI_FakeInterior") c = new Color(0.95f, 0.86f, 0.68f);
            // The kit's brick reads dark brown under our toon light; lift it to a warm red-orange brick.
            if (name == "MI_RedBrick") c = new Color(1.45f, 1.25f, 1.15f);
            if (name == "MI_Roof_Slate") c = new Color(1.2f, 1.25f, 1.35f);
            c.a = 1f;
            m.SetColor("_BaseColor", c);
            m.SetColor("_ShadeColor", new Color(0.72f, 0.76f, 0.9f));
            m.SetColor("_EmissionColor", name == "MI_FakeInterior" ? new Color(0.25f, 0.2f, 0.12f) : Color.black);
            m.SetFloat("_OutlineWidth", 0f);
            // Tree leaves and bark are alpha-tested, double-sided cards (glTF MASK, cutoff 0.2).
            bool foliage = name.StartsWith("Leaves") || name.StartsWith("Bark");
            m.SetFloat("_Cutoff", foliage ? 0.3f : 0f);
            m.SetFloat("_Cull", foliage ? 0f : 2f);
            if (foliage) m.EnableKeyword("_ALPHATEST_ON"); else m.DisableKeyword("_ALPHATEST_ON");
            m.SetFloat("_RimStrength", name == "MI_Glass" ? 0.8f : 0.12f);
            EditorUtility.SetDirty(m);
            mats[name] = m;
            return m;
        }
    }
}
