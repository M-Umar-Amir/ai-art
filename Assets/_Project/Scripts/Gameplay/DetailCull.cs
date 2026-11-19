using System.Collections.Generic;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Near-only street detail (11 Nov 2026 perf pass). Shop interiors, terrace planters, umbrellas and other small
    /// parts are merged per material like the rest of a street block, but only drawn while the block is within
    /// `Range` metres of the camera along the street; beyond that the fascia, canopy, logos and tower facades carry
    /// the look. Detail and neon materials never cast shadows. Benchmark before: ~2,550 batches, 15 fps (HD 620).
    public class DetailCull : MonoBehaviour
    {
        /// Materials that are near-only detail (culled with distance, no shadows).
        public static readonly HashSet<Material> Detail = new();
        /// Materials that stay visible at any distance but never cast shadows (neon lines, glass rails, light panels).
        public static readonly HashSet<Material> NoShadow = new();
        public const float Range = 70f;

        public readonly List<Renderer> renderers = new();
        float centreZ;
        bool shown = true;
        int phase;

        public static Material MarkDetail(Material m) { if (m) Detail.Add(m); return m; }
        public static Material MarkNoShadow(Material m) { if (m) NoShadow.Add(m); return m; }

        /// Called by TrackSpawner.Combine for every merged batch it creates.
        public static void Classify(GameObject root, Renderer batch, Material m)
        {
            if (!m) return;
            bool detail = Detail.Contains(m);
            if (detail || NoShadow.Contains(m)) batch.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (!detail) return;
            var cull = root.GetComponent<DetailCull>() ?? root.AddComponent<DetailCull>();
            cull.renderers.Add(batch);
            cull.centreZ = root.transform.position.z + 10f;
        }

        void Start() { phase = Random.Range(0, 8); centreZ = transform.position.z + 10f; }

        void Update()
        {
            if ((Time.frameCount + phase) % 8 != 0) return;
            var cam = Camera.main;
            if (!cam) return;
            bool want = Mathf.Abs(centreZ - cam.transform.position.z) < Range;
            if (want == shown) return;
            shown = want;
            foreach (var r in renderers) if (r) r.enabled = want;
        }
    }
}
