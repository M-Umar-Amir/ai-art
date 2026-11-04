using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DallasDash.EditorTools
{
    /// Uses the imported Quaternius skinned people, preserving mesh bind poses and skin bones.
    public static class RidersKitCrewBuilder
    {
        const string CrewPath = "Assets/_Project/Art/Models/Kit/crew.glb";
        const string AnimationPath = "Assets/_Project/Art/Models/Kit/anims.glb";
        const string Output = "Assets/_Project/Art/RidersKitCrew";

        public static GameObject Build(string name, int index)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(CrewPath);
            if (!source) throw new InvalidOperationException("Imported rigged crew asset missing: " + CrewPath);
            var root = new GameObject(name);
            var model = Object.Instantiate(source, root.transform, false);
            model.name = "Imported courier";
            string bodyName = index == 1 ? "female" : "male";
            string hairName = index == 1 ? "Hair_Buns" : index == 2 ? "Hair_SimpleParted" : "Hair_Buzzed";
            Transform body = Find(model.transform, bodyName), hair = Find(model.transform, hairName);
            if (!body) { Object.DestroyImmediate(root); throw new InvalidOperationException("Imported body hierarchy missing: " + bodyName); }
            // Keep full body hierarchy intact; Instantiate has already remapped all its bone references.
            var keep = new HashSet<Transform> { body };
            if (hair) keep.Add(hair);
            foreach (var selected in keep) selected.SetParent(model.transform, true);
            var remove = new List<GameObject>();
            foreach (Transform child in model.transform)
            {
                bool needed = false;
                foreach (var selected in keep) if (selected == child || selected.IsChildOf(child)) needed = true;
                if (!needed) remove.Add(child.gameObject);
            }
            foreach (var unwanted in remove) Object.DestroyImmediate(unwanted);
            var bones = new Dictionary<string, Transform>();
            foreach (var bone in body.GetComponentsInChildren<Transform>(true)) if (!bones.ContainsKey(bone.name)) bones.Add(bone.name, bone);
            if (hair)
            {
                foreach (var skin in hair.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var mapped = skin.bones;
                    for (int i = 0; i < mapped.Length; i++) if (mapped[i] && bones.TryGetValue(mapped[i].name, out var target)) mapped[i] = target;
                    skin.bones = mapped;
                    if (skin.rootBone && bones.TryGetValue(skin.rootBone.name, out var mainRoot)) skin.rootBone = mainRoot;
                }
            }
            Directory.CreateDirectory(Output);
            bool first = true; var bounds = new Bounds();
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                skin.updateWhenOffscreen = true;
                skin.quality = SkinQuality.Bone4;
                if (first) { bounds = skin.bounds; first = false; } else bounds.Encapsulate(skin.bounds);
                var materials = skin.sharedMaterials;
                for (int m = 0; m < materials.Length; m++) materials[m] = LitMaterial(materials[m], index);
                skin.sharedMaterials = materials;
                if (skin.gameObject.name.IndexOf("superhero", StringComparison.OrdinalIgnoreCase) >= 0) Uniform(skin, index);
            }
            if (first || bounds.size.y < .01f) { Object.DestroyImmediate(root); throw new InvalidOperationException("Imported crew has no usable skinned bounds"); }
            float scale = 1.75f / bounds.size.y;
            model.transform.localScale *= scale;
            model.transform.localPosition = new Vector3(-bounds.center.x * scale, -bounds.min.y * scale, -bounds.center.z * scale);
            AttachAnimations(root, body, index);
            var animation = root.GetComponent<Animation>();
            Debug.Log("[Riders] Imported skinned courier " + name + ": " + bodyName + ", hair=" + hairName + ", renderers=" + model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length + ", idle=" + (animation && animation.clip));
            return root;
        }

        static Material LitMaterial(Material original, int index)
        {
            if (!original) return original;
            string id = original.name.Replace(" (Instance)", "").Replace('/', '_');
            string path = Output + "/" + index + "-" + id + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            Texture texture = null;
            foreach (string property in new[] { "baseColorTexture", "_BaseMap", "_MainTex" }) if (original.HasProperty(property) && original.GetTexture(property)) { texture = original.GetTexture(property); break; }
            Color color = original.HasProperty("baseColorFactor") ? original.GetColor("baseColorFactor") : original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") : Color.white;
            material.SetTexture("_BaseMap", texture); material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", .28f); material.SetFloat("_Metallic", 0);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void Uniform(SkinnedMeshRenderer skin, int index)
        {
            var original = skin.sharedMesh;
            if (!original || !original.isReadable) throw new InvalidOperationException("Courier uniform requires readable imported skinned mesh: " + skin.name);
            var mesh = Object.Instantiate(original);
            mesh.name = "Imported courier uniform " + index;
            var vertices = mesh.vertices;
            var partitions = new List<int>[5];
            for (int i = 0; i < partitions.Length; i++) partitions[i] = new List<int>();
            float height = mesh.bounds.size.y, bottom = mesh.bounds.min.y;
            for (int sub = 0; sub < original.subMeshCount; sub++)
            {
                var triangles = original.GetTriangles(sub);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 center = (vertices[triangles[i]] + vertices[triangles[i + 1]] + vertices[triangles[i + 2]]) / 3;
                    float h = (center.y - bottom) / height;
                    // Preserve textured face and hands. The original skinned body becomes a
                    // full courier jumpsuit by material assignment; no primitive clothing shells.
                    bool hand = Mathf.Abs(center.x - mesh.bounds.center.x) > height * .39f;
                    int part = h >= .845f || hand ? 0 : h < .105f ? 3 : h < .55f ? 2 : 1;
                    if (part == 2 && h >= .24f && h <= .265f) part = 4;
                    partitions[part].Add(triangles[i]); partitions[part].Add(triangles[i + 1]); partitions[part].Add(triangles[i + 2]);
                }
            }
            mesh.subMeshCount = partitions.Length;
            for (int i = 0; i < partitions.Length; i++) mesh.SetTriangles(partitions[i], i, false);
            mesh.RecalculateBounds();
            string path = Output + "/" + index + "-courier-uniform.asset";
            var stored = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (stored) { EditorUtility.CopySerialized(mesh, stored); Object.DestroyImmediate(mesh); mesh = stored; }
            else AssetDatabase.CreateAsset(mesh, path);
            skin.sharedMesh = mesh;
            var shirt = index == 1 ? new Color(.46f, .16f, .78f) : index == 2 ? new Color(.06f, .48f, .57f) : new Color(1, .26f, .045f);
            skin.sharedMaterials = new[]
            {
                skin.sharedMaterials[0], Flat("shirt-" + index, shirt),
                Flat("trousers", new Color(.055f, .075f, .11f)),
                Flat("shoes", new Color(.12f, .15f, .2f)),
                Flat("reflective", new Color(.82f, .9f, .88f))
            };
        }

        static Material Flat(string name, Color color)
        {
            string path = Output + "/uniform-" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.SetColor("_BaseColor", color); material.SetTexture("_BaseMap", null);
            material.SetFloat("_Smoothness", .18f); material.SetFloat("_Metallic", 0);
            EditorUtility.SetDirty(material); return material;
        }

        static void AttachAnimations(GameObject root, Transform body, int index)
        {
            Transform armature = Find(body, "Armature");
            if (!armature) return;
            string targetPrefix = AnimationUtility.CalculateTransformPath(armature, root.transform);
            Animation player = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AnimationPath))
            {
                if (!(asset is AnimationClip source) || !(source.name.Contains("Idle_Loop") || source.name.Contains("Walk_Loop") || source.name.Contains("Dance_Loop"))) continue;
                string clipName = source.name.Contains("Dance_Loop") ? "dance" : source.name.Contains("Walk_Loop") ? "walk" : "idle";
                var clip = new AnimationClip { name = clipName, legacy = true, wrapMode = WrapMode.Loop, frameRate = source.frameRate };
                int count = 0;
                foreach (var binding in AnimationUtility.GetCurveBindings(source))
                {
                    int marker = binding.path.IndexOf("Armature", StringComparison.Ordinal);
                    if (marker < 0 || binding.path.Contains("ual2/")) continue;
                    string suffix = binding.path.Substring(marker + "Armature".Length);
                    var target = binding; target.path = targetPrefix + suffix;
                    if (!root.transform.Find(target.path)) continue;
                    AnimationUtility.SetEditorCurve(clip, target, AnimationUtility.GetEditorCurve(source, binding)); count++;
                }
                if (count == 0) { Object.DestroyImmediate(clip); continue; }
                string path = Output + "/" + index + "-" + clipName + ".anim";
                var stored = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (stored) { EditorUtility.CopySerialized(clip, stored); Object.DestroyImmediate(clip); clip = stored; }
                else AssetDatabase.CreateAsset(clip, path);
                if (!player) player = root.AddComponent<Animation>();
                player.AddClip(clip, clipName);
                if (clipName == "idle") { player.clip = clip; player.playAutomatically = true; }
            }
            if (player) { player.wrapMode = WrapMode.Loop; player.cullingType = AnimationCullingType.AlwaysAnimate; }
        }

        static Transform Find(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent) { var found = Find(child, name); if (found) return found; }
            return null;
        }
    }
}
