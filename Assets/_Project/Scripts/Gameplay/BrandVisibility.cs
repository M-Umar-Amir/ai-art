using System.Collections.Generic;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// The official Yum! Brands logos on signs, billboards, vans and Ace's box (client, 4 Nov 2026), and the count
    /// of how many are in view ("make sure at least one brand is visible every frame of the run": the playtest
    /// checks it every frame). Street signs are merged into the street meshes, so they are registered with their
    /// world bounds and the segment that owns them; moving ones (vans, the rider's box) register their renderer.
    public static class BrandVisibility
    {
        struct Mark { public Renderer renderer; public Transform owner; public Bounds bounds; public Vector3 normal; public bool rider; }
        static readonly List<Mark> marks = new();
        static readonly Plane[] planes = new Plane[6];

        /// Textures by brand (TrackSpawner.YumBrands order) and format, set by TrackSpawner from its inspector fields.
        public static Texture2D[] Square, Wide, Board;
        static readonly Dictionary<string, Material> mats = new();

        /// A white backlit lightbox: the face shows the logo texture exactly once (1x1 tiling, no world mapping),
        /// lit almost entirely from behind (emission), so the dusk sun and the grade don't tint it pink.
        public static void Backlit(Material m, Texture texture)
        {
            const float lit = 0.12f, glow = 1.2f; // white reads white after ACES, with a soft backlit halo
            m.SetTexture("_BaseMap", texture);
            m.SetTextureScale("_BaseMap", Vector2.one); m.SetTextureOffset("_BaseMap", Vector2.zero);
            m.mainTextureScale = Vector2.one; m.mainTextureOffset = Vector2.zero;
            m.SetColor("_BaseColor", new Color(lit, lit, lit, 1f));
            // Emission is modulated by the albedo (texture x base colour), so scale it back up to the texture.
            m.SetColor("_EmissionColor", Color.white * (glow / lit));
            m.SetFloat("_EmissionUsesAlbedo", 1f);
            m.SetFloat("_WorldTextureScale", 0f);
            // The lit (PBR) path: only there is the emission modulated by the texture (the toon path adds flat emission,
            // which turned every sign plain white).
            m.SetFloat("_SurfaceStyle", 1f); m.SetFloat("_RimStrength", 0f);
            if (m.HasProperty("_RoughnessStrength")) m.SetFloat("_RoughnessStrength", 0f);
            m.SetFloat("_Smoothness", 0f); m.SetFloat("_Metallic", 0f);
            if (m.HasProperty("_WindowInterior")) m.SetFloat("_WindowInterior", 0f);
            if (m.HasProperty("_Wetness")) m.SetFloat("_Wetness", 0f);
            if (m.HasProperty("_BumpScale")) m.SetFloat("_BumpScale", 0f);
            m.SetTexture("_BumpMap", null);
            if (m.HasProperty("_RoughnessMap")) m.SetTexture("_RoughnessMap", null);
            m.EnableKeyword("_EMISSION");
        }

        /// Width / height of each sign format's texture, so every logo face keeps its exact aspect ratio.
        public static float Aspect(string format) => format == "wide" ? 1024f / 320f : format == "board" ? 1024f / 640f : 1f;

        public static Material Logo(TrackSpawner track, int brand, string format)
        {
            var set = format == "wide" ? Wide : format == "board" ? Board : Square;
            if (set == null || brand < 0 || brand >= set.Length || !set[brand]) throw new System.InvalidOperationException("Missing brand logo " + brand + " " + format);
            string key = format + brand;
            if (mats.TryGetValue(key, out var m) && m) return m;
            m = new Material(track.tintMaterial) { name = "Brand logo " + key };
            Backlit(m, set[brand]);
            return mats[key] = m;
        }

        /// A logo face: a quad (visible from its -Z side) sized w x h, facing `facing`.
        public static GameObject Face(TrackSpawner track, Transform parent, int brand, string format, Vector3 at, Quaternion facing, float w, float h, bool moving = false, bool rider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "Brand logo " + TrackSpawner.YumBrands[brand].name;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(at, facing);
            // Height follows the width at the texture's own aspect ratio: never stretched, never cropped.
            go.transform.localScale = new Vector3(w, w / Aspect(format), 1f);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = Logo(track, brand, format);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (moving) Live(r, rider);
            else marks.Add(new Mark { owner = parent, bounds = r.bounds, normal = -go.transform.forward });
            return go;
        }

        public static void Live(Renderer r, bool rider) => marks.Add(new Mark { renderer = r, rider = rider });

        public static (int street, int rider) InView(Camera cam, float maxDistance = 170f)
        {
            if (!cam) return (0, 0);
            GeometryUtility.CalculateFrustumPlanes(cam, planes);
            var eye = cam.transform.position;
            int street = 0, rider = 0;
            for (int i = marks.Count - 1; i >= 0; i--)
            {
                var m = marks[i];
                Bounds b; Vector3 n; bool active;
                if (m.renderer != null || !ReferenceEquals(m.renderer, null))
                {
                    if (!m.renderer) { marks.RemoveAt(i); continue; }
                    b = m.renderer.bounds; n = -m.renderer.transform.forward;
                    active = m.renderer.enabled && m.renderer.gameObject.activeInHierarchy;
                }
                else
                {
                    if (!m.owner) { marks.RemoveAt(i); continue; }
                    b = m.bounds; n = m.normal; active = m.owner.gameObject.activeInHierarchy;
                }
                if (!active || (b.center - eye).sqrMagnitude > maxDistance * maxDistance) continue;
                if (Vector3.Dot(n, eye - b.center) <= 0f) continue; // seen from behind
                if (!GeometryUtility.TestPlanesAABB(planes, b)) continue;
                if (m.rider) rider++; else street++;
            }
            return (street, rider);
        }
    }
}
