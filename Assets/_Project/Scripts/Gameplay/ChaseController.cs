using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// The police chase (client round, 4 Nov 2026). The officer is a courier-cop in the game's own style,
    /// modelled on Ace (same build, police navy and white, helmet with a red/blue light visor) on a police
    /// e-bike with a light bar. DashRun owns the rules (PoliceGap); this places and animates him.
    ///
    /// The follow camera sits 7.5 m behind Ace, so the officer is drawn at a compressed distance: at the far
    /// edge of the chase he trails just inside the bottom of the frame, and he closes onto Ace's back wheel as
    /// the gap runs out. He is absent in the Skypark (the portal shook him off).
    public class ChaseController : MonoBehaviour
    {
        public RiderController rider;
        /// The officer on his police e-bike (DashAssets.Police), instantiated under this object.
        public GameObject officerPrefab;
        public Transform officer;
        Renderer[] red, blue;
        Light beacon;
        MaterialPropertyBlock block;
        float x, bob;
        /// Gap (m) under which the officer is drawn behind Ace.
        public const float ShowGap = 11f;

        /// Close enough for the siren to swell and the music to duck.
        public bool Close => DashRun.Instance && DashRun.Instance.PoliceActive && DashRun.Instance.Heat > 0.55f
            && GameManager.Instance && GameManager.Instance.State == RunState.Running;

        /// Kept for older callers: hits are applied to the gap by DashRun.Crash.
        public void Stumble() { }
        public void SirenBurst() { }
        public void Place(Vector3 p) { if (officer) officer.position = p; }

        void Start()
        {
            if (!rider) rider = FindFirstObjectByType<RiderController>();
            if (!officer && officerPrefab)
            {
                officer = Instantiate(officerPrefab, transform).transform;
                officer.name = "Police officer";
            }
            if (!officer) { enabled = false; return; }
            var reds = new System.Collections.Generic.List<Renderer>();
            var blues = new System.Collections.Generic.List<Renderer>();
            // The light bar lamps are their own nodes on the police e-bike (models/make_ebike.py police).
            foreach (var r in officer.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.gameObject.name;
                if (n.Contains("Light bar red")) reds.Add(r);
                else if (n.Contains("Light bar blue")) blues.Add(r);
            }
            red = reds.ToArray(); blue = blues.ToArray();
            block = new MaterialPropertyBlock();
            var go = new GameObject("Light bar glow");
            go.transform.SetParent(officer, false);
            go.transform.localPosition = new Vector3(0f, 1.9f, -0.2f);
            beacon = go.AddComponent<Light>();
            beacon.type = LightType.Point; beacon.range = 5.5f; beacon.intensity = 0f;
            beacon.shadows = LightShadows.None;
            officer.gameObject.SetActive(false);
        }

        void Update()
        {
            var gm = GameManager.Instance;
            var run = DashRun.Instance;
            if (!gm || !run || !officer || !rider) return;
            // He rides into view only when he is closing in (gap under ShowGap); further back, the HUD meter and
            // the siren carry the chase so he never blocks the road ahead.
            bool show = (gm.State == RunState.Running && run.PoliceGap < ShowGap || gm.State == RunState.Busted) && run.PoliceActive;
            if (officer.gameObject.activeSelf != show) officer.gameObject.SetActive(show);
            if (!show) return;
            float dt = gm.Paused ? 0f : Time.deltaTime;
            Vector3 r = rider.transform.position;
            float back;
            Vector3 want;
            if (gm.State == RunState.Busted && DashRun.EndReason.StartsWith("BUSTED"))
            {
                // Caught: he pulls alongside, light bar going.
                want = new Vector3(r.x + (r.x > 0f ? -1.3f : 1.3f), 0f, r.z + 0.2f);
                x = Mathf.Lerp(x, want.x, 1f - Mathf.Exp(-6f * Time.deltaTime));
                want.x = x;
            }
            else
            {
                // Compressed screen distance: 2.2 m behind when the gap is gone, ~6 m (bottom of frame) at its widest.
                back = Mathf.Lerp(2.0f, 4.6f, Mathf.Clamp01(run.PoliceGap / ShowGap));
                // He follows Ace's line a beat late, the way a pursuer does.
                x = Mathf.Lerp(x, r.x * 0.85f, 1f - Mathf.Exp(-2.5f * dt));
                bob += dt * 9f;
                want = new Vector3(x, 0f, gm.Distance - back);
            }
            officer.position = want;
            officer.rotation = Quaternion.Euler(0f, 0f, Mathf.Clamp((r.x - x) * -6f, -12f, 12f));
            FlashLights();
        }

        void FlashLights()
        {
            bool phase = Mathf.Repeat(Time.time * 3.2f, 1f) < 0.5f;
            Paint(red, phase ? new Color(1f, 0.08f, 0.08f) * 4f : new Color(0.25f, 0f, 0f));
            Paint(blue, phase ? new Color(0f, 0.1f, 0.25f) : new Color(0.1f, 0.35f, 1f) * 4f);
            if (beacon)
            {
                beacon.color = phase ? new Color(1f, 0.15f, 0.12f) : new Color(0.2f, 0.4f, 1f);
                beacon.intensity = 1.1f;
            }
        }

        void Paint(Renderer[] renderers, Color emission)
        {
            if (renderers == null) return;
            foreach (var r in renderers)
            {
                r.GetPropertyBlock(block);
                block.SetColor("_EmissionColor", emission);
                r.SetPropertyBlock(block);
            }
        }
    }
}
