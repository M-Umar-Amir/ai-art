using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DallasDash.EditorTools
{
    /// Reports which models are actually textured: UV presence per mesh, material slots,
    /// base map assignment and texture size. Run headless:
    ///   -executeMethod DallasDash.EditorTools.TextureAudit.Run
    public static class TextureAudit
    {
        static readonly string[] Roots = { "Assets/_Project" };

        public static void Run()
        {
            var lines = new List<string> { "Dash Riders texture audit", "" };
            var prefabs = new List<string>();
            foreach (var root in Roots)
            {
                prefabs.AddRange(AssetDatabase.FindAssets("t:Prefab", new[] { root }).Select(AssetDatabase.GUIDToAssetPath));
                prefabs.AddRange(AssetDatabase.FindAssets("t:Model", new[] { root }).Select(AssetDatabase.GUIDToAssetPath));
            }
            prefabs = prefabs.Distinct().OrderBy(p => p).ToList();

            int noUvMeshes = 0, untexturedSlots = 0, slots = 0, smallTextures = 0, nonTextureSlots = 0, meshlessRenderers = 0, fontAtlases = 0;
            var untexturedMaterials = new SortedSet<string>();
            var noUvNames = new SortedSet<string>();

            foreach (var path in prefabs)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!prefab) continue;
                var linesForPrefab = new List<string>();
                foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    // Only real meshes can carry UVs; particles/trails/line renderers have no mesh.
                    Mesh mesh = renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh
                        : renderer.GetComponent<MeshFilter>() ? renderer.GetComponent<MeshFilter>().sharedMesh : null;
                    if (mesh)
                    {
                        bool hasUv = mesh.uv.Length == mesh.vertexCount && mesh.vertexCount > 0;
                        if (!hasUv)
                        {
                            noUvMeshes++;
                            noUvNames.Add(path.Replace("Assets/_Project/", "") + " / " + renderer.name + " (" + mesh.name + ")");
                            linesForPrefab.Add("    NO UVs   " + renderer.name + " (" + mesh.name + ")");
                        }
                    }
                    else if (renderer is MeshRenderer or SkinnedMeshRenderer) meshlessRenderers++;
                    else meshlessRenderers++; // particle/trail/line renderers are expected to have no mesh

                    foreach (var mat in renderer.sharedMaterials)
                    {
                        bool hasBaseMapProperty = mat && (mat.HasProperty("_BaseMap") || mat.HasProperty("_MainTex"));
                        if (!mat || !hasBaseMapProperty) { nonTextureSlots++; continue; }
                        slots++;
                        Texture tex = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null;
                        if (!tex && mat.HasProperty("_MainTex")) tex = mat.GetTexture("_MainTex");
                        if (!tex)
                        {
                            untexturedSlots++;
                            untexturedMaterials.Add(mat.name);
                            linesForPrefab.Add("    MISSING  " + mat.name + "  on " + renderer.name);
                        }
                        else if (tex.name == "Font Texture" || tex.width == 0 || tex.height == 0)
                        {
                            // TextMeshPro builds its atlas at runtime; a font material has no baked texture.
                            fontAtlases++;
                        }
                        else if (tex.width < 256 || tex.height < 256)
                        {
                            smallTextures++;
                            linesForPrefab.Add("    SMALL    " + tex.name + " " + tex.width + "x" + tex.height + "  (" + mat.name + ")");
                        }
                    }
                }
                if (linesForPrefab.Count == 0) continue;
                lines.Add(path.Replace("Assets/_Project/", ""));
                lines.AddRange(linesForPrefab);
            }

            var flat = new SortedSet<string>();
            int textureMaterials = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Project" }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (!m || !m.HasProperty("_BaseMap")) continue;
                textureMaterials++;
                Texture t = m.GetTexture("_BaseMap");
                if (!t && m.HasProperty("_MainTex")) t = m.GetTexture("_MainTex");
                if (!t) flat.Add(p.Replace("Assets/_Project/", ""));
            }

            lines.Add("");
            lines.Add("prefabs=" + prefabs.Count + "  materialSlots=" + slots + "  untexturedSlots=" + untexturedSlots +
                      "  noUvMeshes=" + noUvMeshes + "  smallTextures=" + smallTextures +
                      "  fontAtlases=" + fontAtlases +
                      "  nonTextureSlots=" + nonTextureSlots + "  meshlessRenderers=" + meshlessRenderers);
            lines.Add("untextured materials: " + (untexturedMaterials.Count == 0 ? "none" : string.Join(", ", untexturedMaterials)));
            lines.Add("");
            lines.Add("base-map materials: " + textureMaterials + "  without a base map: " + flat.Count);
            foreach (var f in flat) lines.Add("  " + f);
            lines.Add("");
            lines.Add("meshes without UVs: " + (noUvNames.Count == 0 ? "none" : ""));
            foreach (var n in noUvNames) lines.Add("  " + n);
            lines.Add("");
            lines.Add("meshless renderers (particles/trails/decals, expected): " + meshlessRenderers);

            var project = Path.GetFullPath(".");
            var logPath = Path.Combine(project, "Logs", "texture-audit.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));
            File.WriteAllLines(logPath, lines);

            var stamp = System.DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var evidence = Path.Combine("D:/ai-art/ai-art/dash-riders-overhaul/evidence", stamp);
            Directory.CreateDirectory(evidence);
            File.WriteAllLines(Path.Combine(evidence, "texture-audit.txt"), lines);

            Debug.Log("[TextureAudit] prefabs=" + prefabs.Count + " slots=" + slots + " untextured=" + untexturedSlots +
                      " noUV=" + noUvMeshes + " small=" + smallTextures + " flatMaterials=" + flat.Count +
                      " report=" + logPath);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
