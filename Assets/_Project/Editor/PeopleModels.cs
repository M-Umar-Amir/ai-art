using System.IO;
using System.Linq;
using DallasDash.Gameplay;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DallasDash.EditorTools
{
    /// Pedestrian prefabs from models/people-*.glb (one skeleton, all hair pieces, street clips) with the outfit
    /// textures from models/bake_outfits.py, collected in Resources/PeopleCatalogue for DistrictLife.
    public static class PeopleModels
    {
        const string ModelDir = "Assets/_Project/Art/Models/DashRiders";
        const string TextureDir = "Assets/_Project/Art/Textures/People";
        const string OutDir = "Assets/_Project/Art/People";
        const string CataloguePath = "Assets/_Project/Resources/PeopleCatalogue.asset";
        static readonly string[] HairNames = { "Hair_Buzzed", "Hair_SimpleParted", "Hair_Beard", "Hair_Buns", "Hair_Long" };

        public static PeopleCatalogue Build()
        {
            Directory.CreateDirectory(OutDir);
            Directory.CreateDirectory("Assets/_Project/Resources");
            var catalogue = AssetDatabase.LoadAssetAtPath<PeopleCatalogue>(CataloguePath);
            if (!catalogue) { catalogue = ScriptableObject.CreateInstance<PeopleCatalogue>(); AssetDatabase.CreateAsset(catalogue, CataloguePath); }
            catalogue.bodies = new[] { Body("male", false), Body("female", true) };
            catalogue.maleOutfits = Outfits("male", catalogue.bodies[0]);
            catalogue.femaleOutfits = Outfits("female", catalogue.bodies[1]);
            EditorUtility.SetDirty(catalogue);
            AssetDatabase.SaveAssets();
            return catalogue;
        }

        static Pedestrian Body(string id, bool female)
        {
            string glb = $"{ModelDir}/people-{id}.glb";
            var prefab = ModelImport.Get("people-" + id);
            if (!prefab) throw new System.InvalidOperationException("Pedestrian model missing: " + glb);
            var clips = AssetDatabase.LoadAllAssetsAtPath(glb).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToArray();
            if (clips.Length < 5) throw new System.InvalidOperationException($"Pedestrian clips missing in {glb}: {clips.Length}");

            string controllerPath = $"{OutDir}/people-{id}.controller";
            AssetDatabase.DeleteAsset(controllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            foreach (var clip in clips)
            {
                var state = machine.AddState(clip.name);
                state.motion = clip;
                if (clip.name == Pedestrian.Walk) machine.defaultState = state;
            }

            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = "Pedestrian " + id;
            // The clips bind by path: put the Animator on the transform their bone paths resolve from.
            var binding = AnimationUtility.GetCurveBindings(clips[0]).First(b => b.path.Contains("pelvis"));
            Transform host = null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.Find(binding.path)) { host = t; break; }
            if (!host) throw new System.InvalidOperationException("Pedestrian clip paths do not resolve: " + binding.path);
            var animator = host.GetComponent<Animator>() ?? host.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            var person = root.AddComponent<Pedestrian>();
            person.female = female;
            person.animator = animator;
            person.body = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name.ToLowerInvariant().Contains("superhero"));
            person.hair = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r => HairNames.Contains(r.name)).ToArray<Renderer>();
            foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                r.updateWhenOffscreen = false;
                r.shadowCastingMode = r == person.body ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            string summary = $"[DashRiders] Pedestrian {id}: {clips.Length} clips on {host.name}, {person.hair.Length} hair pieces";
            var saved = PrefabUtility.SaveAsPrefabAsset(root, $"{OutDir}/pedestrian-{id}.prefab");
            Object.DestroyImmediate(root);
            Debug.Log(summary);
            return saved.GetComponent<Pedestrian>();
        }

        static Material[] Outfits(string id, Pedestrian person)
        {
            var source = person.body.sharedMaterial;
            var list = new System.Collections.Generic.List<Material>();
            for (int k = 0; ; k++)
            {
                string path = $"{TextureDir}/people-{id}-{k}.png";
                if (!File.Exists(path)) break;
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer && (importer.maxTextureSize != 1024 || importer.textureCompression != TextureImporterCompression.Compressed))
                {
                    importer.maxTextureSize = 1024; importer.textureCompression = TextureImporterCompression.Compressed;
                    importer.mipmapEnabled = true; importer.SaveAndReimport();
                }
                string matPath = $"{OutDir}/outfit-{id}-{k}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (!m) { m = new Material(source); AssetDatabase.CreateAsset(m, matPath); }
                else m.CopyPropertiesFromMaterial(source);
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
                m.SetColor("_BaseColor", Color.white);
                EditorUtility.SetDirty(m);
                list.Add(m);
            }
            if (list.Count == 0) throw new System.InvalidOperationException("No outfit textures for " + id);
            return list.ToArray();
        }
    }
}
