using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DallasDash.EditorTools
{
    // Distant architecture keeps its silhouette and window rhythm in a single draw call.
    public static class CityLodBuilder
    {
        const string Folder = "Assets/_Project/Art/DashRiders/CityLOD";
        public static void Attach(GameObject root, GameObject detailed)
        {
            Directory.CreateDirectory(Folder);
            var renderers = detailed.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            var proxy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(proxy.GetComponent<Collider>());
            proxy.name = "Distant architectural silhouette";
            proxy.transform.SetParent(root.transform, false);
            proxy.transform.position = bounds.center;
            proxy.transform.localScale = new Vector3(bounds.size.x * .9f, bounds.size.y, bounds.size.z * .9f);
            var distant = proxy.GetComponent<MeshRenderer>(); distant.sharedMaterial = Material();
            distant.shadowCastingMode = ShadowCastingMode.Off;
            distant.receiveShadows = false;
            var group = root.AddComponent<LODGroup>();
            group.SetLODs(new[] { new LOD(.42f, renderers), new LOD(.025f, new Renderer[] { distant }) });
            group.RecalculateBounds();
        }
        static Material Material()
        {
            string materialPath = Folder + "/distant-city.mat", texturePath = Folder + "/windows.png";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material) return material;
            var texture = new Texture2D(256, 512, TextureFormat.RGB24, true);
            var pixels = new Color[256 * 512]; var rng = new System.Random(431);
            var rooms = new Color[16 * 32];
            for (int i = 0; i < rooms.Length; i++)
                rooms[i] = rng.NextDouble() < .13 ? new Color(.65f, .4f, .19f) : new Color(.07f, .18f + (float)rng.NextDouble() * .08f, .28f);
            for (int y = 0; y < 512; y++) for (int x = 0; x < 256; x++)
                pixels[y * 256 + x] = x % 16 < 3 || y % 16 < 4 ? new Color(.16f, .22f, .3f) : rooms[(y / 16) * 16 + x / 16];
            texture.SetPixels(pixels); texture.Apply(true);
            File.WriteAllBytes(texturePath, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(texturePath);
            material = new Material(Shader.Find("FoodInc/ToonCurved"));
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            material.SetColor("_BaseColor", Color.white); material.SetFloat("_SurfaceStyle", .4f);
            material.SetFloat("_RimStrength", .05f); material.enableInstancing = true;
            AssetDatabase.CreateAsset(material, materialPath);
            return material;
        }
    }
}
