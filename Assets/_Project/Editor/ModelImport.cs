using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DallasDash.EditorTools
{
    /// Turns the generated asset-pack meshes (Art/Models/v4/*.glb, Hunyuan3D-2 + sheet projection) into game prefabs:
    /// sized to real-world metres, grounded at y=0, faced along the street, with the toon curved-world material.
    public static class ModelImport
    {
        const string ModelDir = "Assets/_Project/Art/Models/v4";
        const string PrefabDir = "Assets/_Project/Prefabs/Models";
        const string MatDir = "Assets/_Project/Materials/Models";

        /// Keep: the mesh is already in metres with its own pivot (posed riders: seat at the origin).
        public enum Fit { Height, Longest, Keep }

        public struct Spec
        {
            public float size;
            public Fit fit;
            /// Yaw applied after import. The models face +Z (away from the chase camera).
            public float yaw;
            /// Side-view sheets (rides) come in lengthwise along X; turn them to run along the street.
            public bool alongZ;
            public float outline;
            /// Stretched across the street to this width after fitting (0 = keep proportions).
            public float stretchX;
        }

        static readonly Dictionary<string, Spec> Specs = new()
        {
            ["finn"] = new Spec { size = 1.7f, outline = 0.012f },
            ["finn-ride"] = new Spec { fit = Fit.Keep, outline = 0.012f },
            ["faye-ride"] = new Spec { fit = Fit.Keep, outline = 0.012f },
            // Generated from a back view, so the mesh's front is his back: turn him to ride along +Z.
            ["officer-riding"] = new Spec { size = 1.8f, fit = Fit.Longest, alongZ = true, yaw = 180f, outline = 0.012f },
            ["officer-standing"] = new Spec { size = 1.85f, outline = 0.012f },
            ["faye"] = new Spec { size = 1.65f, outline = 0.012f },
            ["fizz-11"] = new Spec { size = 1.5f, outline = 0.012f },
            ["bicycle"] = new Spec { size = 1.8f, fit = Fit.Longest, alongZ = true, yaw = 180f, outline = 0.01f },
            ["scooter"] = new Spec { size = 1.8f, fit = Fit.Longest, alongZ = true, yaw = 180f, outline = 0.01f },
            ["police-bicycle"] = new Spec { size = 1.8f, fit = Fit.Longest, alongZ = true, yaw = 180f, outline = 0.01f },
            ["wagyu-bacon-burger"] = new Spec { size = 0.9f, fit = Fit.Longest, yaw = 180f, outline = 0.008f },
            ["presents"] = new Spec { size = 1.0f, yaw = 180f, outline = 0.01f },
            ["cone"] = new Spec { size = 0.75f, yaw = 180f, outline = 0.01f },
            // A low A-frame barrier spanning the lane, so a hop clears it and the whole lane is visibly closed.
            ["roadworks-barrier"] = new Spec { size = 0.9f, yaw = 180f, outline = 0.01f, stretchX = 1.9f },
            ["tree-planter"] = new Spec { size = 3.2f, yaw = 180f, outline = 0.012f },
            ["shopper"] = new Spec { size = 1.7f, yaw = 180f, outline = 0.012f },
            ["reunion-tower"] = new Spec { size = 70f },
            ["margaret-hunt-hill-bridge"] = new Spec { size = 90f, fit = Fit.Longest },
            ["bank-of-america-plaza"] = new Spec { size = 85f },
            ["fountain-place"] = new Spec { size = 75f },
            ["omni-dallas"] = new Spec { size = 55f },
        };

        /// Prefab for a generated model, or null when that mesh isn't in the project yet.
        public static GameObject Get(string id)
        {
            string prefabPath = $"{PrefabDir}/{id}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var src = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelDir}/{id}.glb");
            if (!src) return existing;
            var newest = File.GetLastWriteTimeUtc($"{ModelDir}/{id}.glb");
            var self = File.GetLastWriteTimeUtc("Assets/_Project/Editor/ModelImport.cs");
            if (self > newest) newest = self;
            if (existing && File.GetLastWriteTimeUtc(prefabPath) > newest) return existing;
            return Build(id, src, Specs.TryGetValue(id, out var s) ? s : new Spec { size = 1f });
        }

        static GameObject Build(string id, GameObject src, Spec spec)
        {
            Directory.CreateDirectory(PrefabDir);
            Directory.CreateDirectory(MatDir);
            var toon = Shader.Find("FoodInc/ToonCurved");

            var root = new GameObject(id);
            var model = (GameObject)Object.Instantiate(src, root.transform);
            model.name = "Model";
            model.transform.localRotation = Quaternion.Euler(0f, spec.yaw, 0f);

            var b = Bounds(model);
            float k = 1f;
            if (spec.fit != Fit.Keep)
            {
                if (spec.alongZ && b.size.x > b.size.z)
                {
                    model.transform.localRotation = Quaternion.Euler(0f, spec.yaw + 90f, 0f);
                    b = Bounds(model);
                }
                float measure = spec.fit == Fit.Height ? b.size.y : Mathf.Max(b.size.x, b.size.y, b.size.z);
                k = spec.size / Mathf.Max(measure, 1e-4f);
                model.transform.localScale *= k;
                b = Bounds(model);
                if (spec.stretchX > 0f)
                {
                    var sc = model.transform.localScale;
                    sc.x *= spec.stretchX / Mathf.Max(b.size.x, 1e-4f);
                    model.transform.localScale = sc;
                    b = Bounds(model);
                }
                model.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);
            }

            int mi = 0;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    string path = $"{MatDir}/{id}-{mi++}.mat";
                    var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (!m) { m = new Material(toon); AssetDatabase.CreateAsset(m, path); }
                    m.shader = toon;
                    var tex = BaseTexture(mats[i]);
                    m.SetTexture("_BaseMap", tex);
                    m.SetColor("_BaseColor", tex ? Color.white : (mats[i] ? mats[i].color : Color.white));
                    m.SetFloat("_OutlineWidth", spec.outline);
                    m.SetFloat("_RimStrength", spec.size > 20f ? 0.15f : 0.35f);
                    EditorUtility.SetDirty(m);
                    mats[i] = m;
                }
                r.sharedMaterials = mats;
                r.shadowCastingMode = spec.size > 20f ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/{id}.prefab");
            Object.DestroyImmediate(root);
            Debug.Log($"[DallasDash] Model prefab {id}: scale {k:F3}, size {b.size}");
            return prefab;
        }

        static Texture BaseTexture(Material m)
        {
            if (!m) return null;
            foreach (var p in new[] { "baseColorTexture", "_BaseMap", "_MainTex", "_BaseColorMap" })
                if (m.HasProperty(p) && m.GetTexture(p)) return m.GetTexture(p);
            return null;
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
