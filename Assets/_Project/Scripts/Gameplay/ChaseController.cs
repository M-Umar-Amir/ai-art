using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// The one officer on a police bicycle behind the rider (pack, 24 Oct: a bicycle, so the chase stays even).
    /// A stumble pulls him in close; a second stumble inside the chase window means busted.
    /// Clean riding lets him drop back out of frame.
    public class ChaseController : MonoBehaviour
    {
        public RiderController rider;
        public Transform[] officers;
        public Light[] lightBars;
        /// The officer on his bike for the chase, and standing beside it once the rider is busted (scene 19).
        public GameObject ridingVisual, standingVisual;

        public float farGap = 14f;
        public float closeGap = 3.5f;
        public float officerSpread = 1.6f;

        float chaseTimer;
        /// True while the officer is on the rider's tail after a stumble.
        public bool Close => chaseTimer > 0f || sirenTimer > 0f;
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
            GameManager.Instance.StateChanged += s =>
            {
                if (s == RunState.Running) { chaseTimer = 0f; gap = closeGap; }
                if (s == RunState.Busted) gap = 1.2f;
                bool standing = s == RunState.Busted && standingVisual;
                if (ridingVisual) ridingVisual.SetActive(!standing);
                if (standingVisual) standingVisual.SetActive(standing);
            };
        }

        public void Stumble()
        {
            if (chaseTimer > 0f) { GameManager.Instance.Bust(); return; }
            chaseTimer = Tuning.ChaseWindowSeconds;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            sirenTimer = Mathf.Max(0f, sirenTimer - Time.deltaTime);
            if (gm.State == RunState.Intro) { FlashLights(); return; }

            if (gm.State == RunState.Running)
            {
                chaseTimer = Mathf.Max(0f, chaseTimer - Time.deltaTime);
                // Run starts with them on his tail (he just ran the red), then they fall back unless he stumbles.
                float target = chaseTimer > 0f ? closeGap : farGap;
                gap = Mathf.MoveTowards(gap, target, (target < gap ? 10f : 1.5f) * Time.deltaTime);
            }

            Vector3 r = rider.transform.position;
            for (int i = 0; i < officers.Length; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                Vector3 want = new(r.x * 0.6f + side * officerSpread, 0f, r.z - gap - i * 0.8f);
                // 19 Busted: he pulls up alongside on the rider's left. 01 Cover: close behind at the start line.
                if (gm.State == RunState.Busted) want = new Vector3(r.x - 1.5f, 0f, r.z + 0.3f);
                if (gm.State == RunState.Ready) want = new Vector3(r.x - 1.6f, 0f, r.z - 3.2f);
                officers[i].position = gm.State == RunState.Running ? want : Vector3.Lerp(officers[i].position, want, 1f - Mathf.Exp(-4f * Time.deltaTime));
            }
            Animate();
            FlashLights();
        }

        void Animate()
        {
            lastPos ??= new Vector3[officers.Length];
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            pedal += Time.deltaTime * 11f;
            for (int i = 0; i < officers.Length; i++)
            {
                float vx = (officers[i].position.x - lastPos[i].x) / dt;
                lastPos[i] = officers[i].position;
                bool pedalling = GameManager.Instance.State == RunState.Running;
                float lean = Mathf.Clamp(-vx * 3f, -18f, 18f) + (pedalling ? Mathf.Sin(pedal + i) * 2f : 0f);
                officers[i].rotation = Quaternion.Slerp(officers[i].rotation, Quaternion.Euler(0f, 0f, lean), 1f - Mathf.Exp(-10f * dt));
            }
        }

        void FlashLights()
        {
            bool on = Mathf.Repeat(Time.time * 6f, 1f) < 0.5f;
            for (int i = 0; i < lightBars.Length; i++)
            {
                if (!lightBars[i]) continue;
                lightBars[i].color = (on ^ (i % 2 == 1)) ? new Color(1f, 0.15f, 0.15f) : new Color(0.2f, 0.4f, 1f);
            }
        }
    }
}
