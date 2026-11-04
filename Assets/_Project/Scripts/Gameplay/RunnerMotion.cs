using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Ace and Nova on foot, after a crash has knocked them off the e-bike: the stride is baked as morph targets
    /// (models/pose_cyclist.py run mode: Run60..Run300 around the stride, Air for jumps and vaults).
    public class RunnerMotion : MonoBehaviour
    {
        public SkinnedMeshRenderer body;

        /// glTFast adds each morph target as a blend shape frame at weight 1.
        const float FrameWeight = 1f;
        static readonly string[] Stride = { "Run60", "Run120", "Run180", "Run240", "Run300" };
        readonly int[] keys = new int[5];
        int air = -1;
        float phase, airborne;
        RiderController rider;

        void Awake()
        {
            rider = GetComponentInParent<RiderController>();
            var mesh = body ? body.sharedMesh : null;
            for (int i = 0; i < keys.Length; i++) keys[i] = mesh ? mesh.GetBlendShapeIndex(Stride[i]) : -1;
            air = mesh ? mesh.GetBlendShapeIndex("Air") : -1;
        }

        void LateUpdate()
        {
            if (!body) return;
            var gm = GameManager.Instance;
            float dt = Time.deltaTime;
            bool running = gm && gm.State == RunState.Running && !gm.Paused;
            bool inAir = running && rider && rider.InAir;
            // A sprinting cadence, styled to the run's speed (the game is far faster than a real sprint).
            if (running && !inAir) phase = Mathf.Repeat(phase + (300f + gm.Speed * 9f) * dt, 360f);
            airborne = Mathf.MoveTowards(airborne, inAir ? 1f : 0f, dt * 8f);
            float p = phase / 60f;
            int key = Mathf.FloorToInt(p) % 6;
            float f = p - Mathf.Floor(p);
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i] < 0) continue;
                int k = i + 1;
                float w = k == key ? 1f - f : k == (key + 1) % 6 ? f : 0f;
                body.SetBlendShapeWeight(keys[i], w * (1f - airborne) * FrameWeight);
            }
            if (air >= 0) body.SetBlendShapeWeight(air, airborne * FrameWeight);
        }
    }
}
