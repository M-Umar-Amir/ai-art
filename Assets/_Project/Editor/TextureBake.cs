using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DallasDash.EditorTools
{
    /// The textures glTFast imports from a .glb are uncompressed RGBA32 sub-assets with no import settings:
    /// 2048 x 2048 each, 22 MB apiece with mips, 368 MB across the models, which is most of the build and far too
    /// much GPU memory for a mid-range phone. This copies each embedded image out of the .glb as its own file
    /// (the bytes are already a PNG or JPEG, so it works headless with no GPU) and gives it real import settings:
    /// ASTC 6x6 on Android and iOS (about 0.6 MB at 1024 with mips), BC compression on desktop.
    public static class TextureBake
    {
        const string Dir = "Assets/_Project/Art/Textures/Baked";

        [Serializable] class Glb { public Image[] images; public View[] bufferViews; public Tex[] textures; public Mat[] materials; }
        [Serializable] class Image { public int bufferView = -1; public string mimeType; public string name; }
        [Serializable] class View { public int byteOffset; public int byteLength; }
        [Serializable] class Tex { public int source = -1; }
        [Serializable] class Mat { public string name; public Pbr pbrMetallicRoughness; }
        [Serializable] class Pbr { public TexRef baseColorTexture; }
        [Serializable] class TexRef { public int index = -1; }

        static readonly Dictionary<string, (Glb json, byte[] bytes, int bin)> cache = new();

        static (Glb json, byte[] bytes, int bin) Load(string glbPath)
        {
            if (cache.TryGetValue(glbPath, out var hit)) return hit;
            var bytes = File.ReadAllBytes(glbPath);
            int len = BitConverter.ToInt32(bytes, 12);
            var json = JsonUtility.FromJson<Glb>(Encoding.UTF8.GetString(bytes, 20, len));
            var r = (json, bytes, 20 + len + 8);
            cache[glbPath] = r;
            return r;
        }

        /// The embedded image used as the base colour of the glTF material called `material` (or the first image
        /// when there's no name match), as a compressed texture asset no larger than maxSize.
        public static Texture2D FromGlb(string glbPath, string material, string name, int maxSize)
        {
            if (!File.Exists(glbPath)) return null;
            var (g, bytes, bin) = Load(glbPath);
            if (g.images == null || g.images.Length == 0) return null;
            int image = 0;
            if (!string.IsNullOrEmpty(material) && g.materials != null)
                foreach (var m in g.materials)
                    if (m.name == material && m.pbrMetallicRoughness?.baseColorTexture != null && m.pbrMetallicRoughness.baseColorTexture.index >= 0)
                    {
                        int t = m.pbrMetallicRoughness.baseColorTexture.index;
                        if (g.textures != null && t < g.textures.Length && g.textures[t].source >= 0) image = g.textures[t].source;
                        break;
                    }
            var img = g.images[Mathf.Clamp(image, 0, g.images.Length - 1)];
            if (img.bufferView < 0) return null;
            var view = g.bufferViews[img.bufferView];
            string ext = img.mimeType == "image/jpeg" ? "jpg" : img.mimeType == "image/webp" ? null : "png";
            if (ext == null) { Debug.LogWarning($"[DallasDash] {glbPath}: WebP image, not baked"); return null; }

            Directory.CreateDirectory(Dir);
            string path = $"{Dir}/{name}.{ext}";
            if (!File.Exists(path))
            {
                var data = new byte[view.byteLength];
                Buffer.BlockCopy(bytes, bin + view.byteOffset, data, 0, view.byteLength);
                File.WriteAllBytes(path, data);
                AssetDatabase.ImportAsset(path);
            }
            Configure(path, maxSize);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void Configure(string path, int maxSize)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (!imp) return;
            bool dirty = imp.maxTextureSize != maxSize || imp.textureCompression != TextureImporterCompression.Compressed || imp.isReadable;
            imp.textureType = TextureImporterType.Default;
            imp.sRGBTexture = true;
            imp.mipmapEnabled = true;
            imp.isReadable = false;
            imp.maxTextureSize = maxSize;
            imp.textureCompression = TextureImporterCompression.Compressed;
            foreach (var platform in new[] { "Android", "iPhone" })
            {
                var s = imp.GetPlatformTextureSettings(platform);
                if (!s.overridden || s.format != TextureImporterFormat.ASTC_6x6 || s.maxTextureSize != maxSize) dirty = true;
                s.overridden = true;
                s.maxTextureSize = maxSize;
                s.format = TextureImporterFormat.ASTC_6x6;
                imp.SetPlatformTextureSettings(s);
            }
            if (dirty) imp.SaveAndReimport();
        }
    }
}
