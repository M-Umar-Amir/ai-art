using System.Collections.Generic;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Marks a batch StreetMesh already merged, so TrackSpawner.Combine leaves it alone.
    public class StreetMeshBatch : MonoBehaviour { }

    /// Builds the street's static parts straight into merged meshes, one per material, without a GameObject per part.
    /// 25 Nov 2026: the brick street (balconies, fire escapes, three furnished shops per block, the KFC outlet) has a
    /// few hundred parts per block side; creating each as a primitive GameObject and merging it afterwards cost 45-117
    /// ms frames while a block streamed in. Here a part is a matrix in a list, and End() builds one mesh per material.
    /// The batches get the same shadow/near-only treatment as TrackSpawner.Combine's (DetailCull.Classify) and are
    /// freed with the street segment (StreamedMeshLifetime).
    public static class StreetMesh
    {
        static readonly Dictionary<PrimitiveType, Mesh> shapes = new();
        static readonly Dictionary<Material, List<CombineInstance>> groups = new();
        static readonly List<Mesh> temporary = new();
        static Transform owner;
        static int depth;

        public static bool Active => owner;

        static Mesh Shape(PrimitiveType type)
        {
            if (shapes.TryGetValue(type, out var mesh) && mesh) return mesh;
            var go = GameObject.CreatePrimitive(type);
            mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(go);
            return shapes[type] = mesh;
        }

        /// Start collecting parts for `parent` (the street side's batching root). Nested calls share the outer batch.
        public static void Begin(Transform parent)
        {
            if (depth++ > 0) return;
            owner = parent;
            foreach (var g in groups.Values) g.Clear();
        }

        public static void Add(PrimitiveType type, Vector3 position, Quaternion rotation, Vector3 scale, Material m) =>
            Add(Shape(type), Matrix4x4.TRS(position, rotation, scale), m);

        /// A world-space mesh placement. `ownedTemporary` meshes (built for this one placement) are destroyed at End.
        public static void Add(Mesh mesh, Matrix4x4 world, Material m, bool ownedTemporary = false)
        {
            if (!m || !mesh) return;
            if (!owner) { Debug.LogWarning("[StreetMesh] Add outside Begin/End"); return; }
            if (!groups.TryGetValue(m, out var list)) groups[m] = list = new List<CombineInstance>();
            list.Add(new CombineInstance { mesh = mesh, transform = owner.worldToLocalMatrix * world });
            if (ownedTemporary) temporary.Add(mesh);
        }

        /// World bounds of a unit quad (or cube face) placed with `world`, for brand-visibility marks.
        public static Bounds QuadBounds(Matrix4x4 world)
        {
            var b = new Bounds(world.MultiplyPoint3x4(new Vector3(-.5f, -.5f, 0f)), Vector3.zero);
            b.Encapsulate(world.MultiplyPoint3x4(new Vector3(.5f, -.5f, 0f)));
            b.Encapsulate(world.MultiplyPoint3x4(new Vector3(-.5f, .5f, 0f)));
            b.Encapsulate(world.MultiplyPoint3x4(new Vector3(.5f, .5f, 0f)));
            return b;
        }

        public static void End()
        {
            if (--depth > 0) return;
            depth = 0;
            if (!owner) { Clear(); return; }
            var lifetime = owner.GetComponent<StreamedMeshLifetime>();
            if (!lifetime) lifetime = owner.gameObject.AddComponent<StreamedMeshLifetime>();
            foreach (var group in groups)
            {
                var list = group.Value;
                if (list.Count == 0) continue;
                int vertices = 0;
                foreach (var piece in list) vertices += piece.mesh.vertexCount;
                var mesh = new Mesh { name = "Street parts", indexFormat = vertices > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
                mesh.CombineMeshes(list.ToArray(), true, true);
                mesh.RecalculateBounds();
                var go = new GameObject("Street parts / " + group.Key.name, typeof(MeshFilter), typeof(MeshRenderer), typeof(StreetMeshBatch));
                go.transform.SetParent(owner, false);
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.GetComponent<MeshRenderer>();
                r.sharedMaterial = group.Key;
                DetailCull.Classify(owner.gameObject, r, group.Key);
                lifetime.meshes.Add(mesh);
                list.Clear();
            }
            Clear();
        }

        static void Clear()
        {
            foreach (var m in temporary) if (m) Object.Destroy(m);
            temporary.Clear();
            owner = null;
        }
    }
}
