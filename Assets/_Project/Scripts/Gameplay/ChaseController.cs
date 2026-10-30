using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// DISABLED: Officer chasing removed per client request.
    /// Game is now time-based course only - complete deliveries before timer runs out.
    /// This component remains for scene compatibility but is inactive.
    public class ChaseController : MonoBehaviour
    {
        public RiderController rider;
        public Transform[] officers;
        public Light[] lightBars;
        public GameObject ridingVisual, standingVisual;

        public float farGap = 14f;
        public float closeGap = 3.5f;
        public float officerSpread = 1.6f;

        float chaseTimer;
        /// Always false - chase system disabled
        public bool Close => false;
        float sirenTimer, pedal;
        Vector3[] lastPos;

        /// Intro control: put the officer somewhere directly (StoryDirector).
        public void Place(Vector3 p)
        {
            foreach (var o in officers) o.position = p;
        }

        /// Siren on for a few seconds without counting as a stumble (The Pursuit).
        public void SirenBurst() => sirenTimer = 4f;
        float gap;

        void Start()
        {
            gap = farGap;
            if (standingVisual) standingVisual.SetActive(false);
            // Hide all officer visuals - chase system disabled
            if (ridingVisual) ridingVisual.SetActive(false);
            if (standingVisual) standingVisual.SetActive(false);
            foreach (var officer in officers) if (officer) officer.gameObject.SetActive(false);

            GameManager.Instance.StateChanged += s =>
            {
                // Chase logic disabled - game is time-based only
                if (s == RunState.Running) { chaseTimer = 0f; gap = farGap; }
                if (s == RunState.Busted) gap = 1.2f;
                // Keep officers hidden
                bool standing = s == RunState.Busted && standingVisual;
                if (ridingVisual) ridingVisual.SetActive(false);
                if (standingVisual) standingVisual.SetActive(false);
            };
        }

        /// DISABLED: Stumble no longer triggers chase - game ends only on delivery timeout
        public void Stumble()
        {
            // Chase system disabled - no officer chasing
            // Game ends only when delivery timer runs out (DashRun.cs)
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            // Keep officers inactive - chase system disabled
            foreach (var officer in officers) if (officer) officer.gameObject.SetActive(false);
            sirenTimer = Mathf.Max(0f, sirenTimer - Time.deltaTime);
            if (gm.State == RunState.Intro) { FlashLights(); return; }

            // Minimal update for scene compatibility - officers remain hidden
            if (gm.State == RunState.Running)
            {
                chaseTimer = Mathf.Max(0f, chaseTimer - Time.deltaTime);
                gap = farGap; // Always keep officers far away
            }

            // Position officers far away but hidden
            Vector3 r = rider.transform.position;
            for (int i = 0; i < officers.Length; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                Vector3 want = new(r.x * 0.6f + side * officerSpread, 0f, r.z - gap - i * 0.8f);
                if (gm.State == RunState.Busted) want = new Vector3(r.x - 1.5f, 0f, r.z + 0.3f);
                if (gm.State == RunState.Ready) want = new Vector3(r.x - 1.6f, 0f, r.z - 3.2f);
                officers[i].position = want;
            }
            FlashLights();
        }

        void Animate()
        {
            // Animation disabled - officers hidden
        }

        void FlashLights()
        {
            // Lights disabled - officers hidden
            bool on = Mathf.Repeat(Time.time * 6f, 1f) < 0.5f;
            for (int i = 0; i < lightBars.Length; i++)
            {
                if (!lightBars[i]) continue;
                lightBars[i].enabled = false; // Disable lights
            }
        }
    }
}
