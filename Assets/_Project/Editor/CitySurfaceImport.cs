using UnityEditor;
using UnityEngine;

namespace DallasDash.EditorTools
{
    public static class CitySurfaceImport
    {
        const string Root = "Assets/_Project/Art/Textures/FreeSurfaces/";
        public static Texture2D Texture(string id, bool normal = false)
        {
            string path = Root + id + (normal ? "_nor_gl_1k.jpg" : "_diff_1k.jpg");
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new System.InvalidOperationException("Required surface texture missing: " + path);
            var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (importer.textureType != type || importer.maxTextureSize != 1024 || importer.wrapMode != TextureWrapMode.Repeat)
            {
                importer.textureType = type;
                importer.maxTextureSize = 1024;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        public static Material Material(string id)
        {
            string path = Root + id + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.SetTexture("_BaseMap", Texture(id));
            material.SetTexture("_BumpMap", Texture(id, true));
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_BumpScale", .4f);
            material.SetFloat("_Smoothness", .2f);
            material.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(material);
            return material;
        }
        public static Texture2D Roughness(string id)
        {
            string path = Root + id + "_rough_1k.jpg";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new System.InvalidOperationException("Missing roughness: " + path);
            if (importer.sRGBTexture || importer.wrapMode != TextureWrapMode.Repeat) {
                importer.sRGBTexture = false; importer.wrapMode = TextureWrapMode.Repeat;
                importer.maxTextureSize = 1024; importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        public static void NightEnvironment()
        {
            string path = Root + "shanghai_bund_1k.hdr";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new System.InvalidOperationException("Night HDR environment is missing.");
            if (importer.textureShape != TextureImporterShape.TextureCube) {
                importer.textureShape = TextureImporterShape.TextureCube;
                importer.generateCubemap = TextureImporterGenerateCubemap.AutoCubemap;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.cubemapConvolution = TextureImporterCubemapConvolution.Specular;
                importer.SetTextureSettings(settings);
                importer.sRGBTexture = false; importer.mipmapEnabled = true;
                importer.maxTextureSize = 1024; importer.SaveAndReimport();
            }
            const string skyPath = "Assets/_Project/Resources/NightSky.mat";
            System.IO.Directory.CreateDirectory("Assets/_Project/Resources");
            var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (!sky) { sky = new Material(Shader.Find("Skybox/Cubemap")); AssetDatabase.CreateAsset(sky,skyPath); }
            sky.SetTexture("_Tex",AssetDatabase.LoadAssetAtPath<Cubemap>(path));
            sky.SetFloat("_Exposure",.32f); sky.SetFloat("_Rotation",115f);
            EditorUtility.SetDirty(sky);
            // The visible sky is the PDF cover's dusk gradient; the city HDR above stays the reflection source.
            const string duskPath = "Assets/_Project/Resources/DuskSky.mat";
            var dusk = AssetDatabase.LoadAssetAtPath<Material>(duskPath);
            var duskShader = Shader.Find("DashRiders/DuskSky");
            if (!duskShader) throw new System.InvalidOperationException("DuskSky shader is missing.");
            if (!dusk) { dusk = new Material(duskShader); AssetDatabase.CreateAsset(dusk, duskPath); }
            dusk.shader = duskShader;
            EditorUtility.SetDirty(dusk);
        }
    }
}
