using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DallasDash.EditorTools
{
    // District surfaces bend relative to the camera. Every grounded model must use
    // that same shader; a straight URP/Lit mesh otherwise floats above the bent road.
    public static class DistrictModelBuilder
    {
        const string Root = "Assets/_Project/Art/RidersCity";
        static readonly Dictionary<Material, Material> Converted = new();
        public static GameObject Build(string name, GameObject source)
        {
            Directory.CreateDirectory(Root + "/DistrictMaterials");
            var instance = Object.Instantiate(source); instance.name = name;
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var original = materials[i];
                    if (!original) continue;
                    if (!Converted.TryGetValue(original, out var material))
                    {
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original, out string guid, out long id);
                        string path = Root + "/DistrictMaterials/" + guid + "_" + id + ".mat";
                        material = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (!material) { material = new Material(Shader.Find("FoodInc/ToonCurved")); AssetDatabase.CreateAsset(material, path); }
                        material.shader = Shader.Find("FoodInc/ToonCurved");
                        material.SetColor("_BaseColor", original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") : Color.white);
                        material.SetTexture("_BaseMap", original.HasProperty("_BaseMap") ? original.GetTexture("_BaseMap") : null);
                        material.SetColor("_EmissionColor", Color.black);
                        material.SetColor("_ShadeColor", new Color(.82f, .82f, .79f));
                        material.SetFloat("_SurfaceStyle", 1f); material.SetFloat("_Smoothness", .3f);
                        material.SetFloat("_RimStrength", 0f); material.SetFloat("_OutlineWidth", 0);
                        material.SetShaderPassEnabled("Outline", false);
                        EditorUtility.SetDirty(material); Converted[original] = material;
                    }
                    materials[i] = material;
                }
                renderer.sharedMaterials = materials;
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, Root + "/Prefabs/" + name + ".prefab");
            Object.DestroyImmediate(instance); return prefab;
        }
    }
}
