using DallasDash.Gameplay;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DallasDash.UI
{
    /// Live 3D portraits for the crew screen. Copies of the standing crew models sit on a turntable far below
    /// the street on their own layer; a camera only sees that layer and renders into a transparent texture
    /// that the crew card shows. Rendering runs only while the crew screen is open.
    public class CrewStudio : MonoBehaviour
    {
        const int Layer = 31;
        static readonly Vector3 Origin = new(0f, -400f, -400f);

        public RenderTexture Texture { get; private set; }
        public int Shown { get; private set; }

        Camera cam;
        Transform turntable;
        GameObject[] models;
        Bounds[] bounds;
        float spin;

        public static CrewStudio Create(Transform parent)
        {
            var look = FindFirstObjectByType<RiderLook>();
            if (!look || look.standing == null || look.standing.Length == 0) return null;
            var studio = new GameObject("Crew Studio").AddComponent<CrewStudio>();
            studio.transform.SetParent(parent, false);
            studio.Build(look.standing);
            return studio;
        }

        void Build(GameObject[] crew)
        {
            Texture = new RenderTexture(720, 900, 24, RenderTextureFormat.ARGB32) { name = "Crew portrait", antiAliasing = 4 };
            turntable = new GameObject("Turntable").transform;
            turntable.position = Origin;
            models = new GameObject[crew.Length];
            bounds = new Bounds[crew.Length];
            for (int i = 0; i < crew.Length; i++)
            {
                if (!crew[i]) continue;
                var m = Instantiate(crew[i], turntable);
                m.name = crew[i].name + " (portrait)";
                m.transform.localPosition = Vector3.zero;
                m.transform.localRotation = Quaternion.identity;
                m.SetActive(true);
                foreach (var t in m.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layer;
                var rs = m.GetComponentsInChildren<Renderer>();
                var b = rs.Length > 0 ? rs[0].bounds : new Bounds(m.transform.position, Vector3.one * 1.6f);
                foreach (var r in rs) b.Encapsulate(r.bounds);
                bounds[i] = b;
                m.SetActive(false);
                models[i] = m;
            }

            cam = new GameObject("Crew Camera").AddComponent<Camera>();
            cam.transform.SetParent(transform, false);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.cullingMask = 1 << Layer;
            cam.fieldOfView = 26f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            cam.targetTexture = Texture;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false;
            data.renderShadows = false;
            cam.enabled = false;

            // The street camera must not see the studio.
            foreach (var c in Camera.allCameras) if (c != cam) c.cullingMask &= ~(1 << Layer);
            Show(0);
        }

        public void Show(int index)
        {
            if (models == null) return;
            Shown = Mathf.Clamp(index, 0, models.Length - 1);
            for (int i = 0; i < models.Length; i++) if (models[i]) models[i].SetActive(i == Shown);
            spin = 0f;
            // Frame the whole figure, a little headroom, looking slightly down from chest height.
            var b = bounds[Shown];
            float h = Mathf.Max(b.size.y, b.size.x * 0.8f);
            float dist = h * 0.62f / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            var center = new Vector3(Origin.x, b.min.y + b.size.y * 0.5f, Origin.z);
            cam.transform.position = center + new Vector3(0f, h * 0.12f, -dist);
            cam.transform.LookAt(center + Vector3.up * h * 0.02f);
        }

        public void SetLive(bool live)
        {
            if (cam) cam.enabled = live;
        }

        void Update()
        {
            if (!cam || !cam.enabled) return;
            // Showroom sway around a front three-quarter view (a full turn showed the back half the time),
            // with a gentle idle bob. The models face +Z; the camera looks at them from -Z.
            spin += Time.unscaledDeltaTime;
            turntable.rotation = Quaternion.Euler(0f, 180f + 18f + Mathf.Sin(spin * 0.9f) * 35f, 0f);
            turntable.position = Origin + Vector3.up * Mathf.Sin(Time.unscaledTime * 2f) * 0.02f;
        }
    }
}
