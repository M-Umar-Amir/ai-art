using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Ace and Nova on their courier e-bikes. The rider mesh carries the pedal stroke as baked morph targets
    /// (models/pose_cyclist.py: Pedal60..Pedal300 around the crank, Stand for out of the saddle), so the legs,
    /// crank and wheels stay in step without a skeleton. In the air the cranks level off and the rider stands.
    public class CyclistMotion : MonoBehaviour
    {
        public SkinnedMeshRenderer body;
        public Transform crank, frontWheel, rearWheel;
        public float wheelRadius = 0.4f;

        /// glTFast adds each morph target as a blend shape frame at weight 1 (not Unity's usual 100).
        const float FrameWeight = 1f;
        static readonly string[] PedalShapes = { "Pedal60", "Pedal120", "Pedal180", "Pedal240", "Pedal300" };
        readonly int[] pedal = new int[5];
        int stand = -1;
        float crankAngle, wheelAngle, standing;
        Quaternion crankRest, frontRest, rearRest;
        RiderController rider;

        void Awake()
        {
            rider = GetComponentInParent<RiderController>();
            if (crank) crankRest = crank.localRotation;
            if (frontWheel) frontRest = frontWheel.localRotation;
            if (rearWheel) rearRest = rearWheel.localRotation;
            var mesh = body ? body.sharedMesh : null;
            for (int i = 0; i < pedal.Length; i++) pedal[i] = mesh ? mesh.GetBlendShapeIndex(PedalShapes[i]) : -1;
            stand = mesh ? mesh.GetBlendShapeIndex("Stand") : -1;
        }

        void LateUpdate()
        {
            var gm = GameManager.Instance;
            float dt = Time.deltaTime;
            float speed = 0f;
            if (gm && !gm.Paused)
                speed = gm.State == RunState.Running ? gm.Speed : rider ? rider.cruiseSpeed : 0f;
            bool air = rider && rider.InAir && gm && gm.State == RunState.Running;

            wheelAngle = Mathf.Repeat(wheelAngle + speed / wheelRadius * Mathf.Rad2Deg * dt, 360f);
            if (air || speed < 0.5f)
            {
                // Coast with the cranks level: the stand pose is authored at phase 0.
                float target = Mathf.Round(crankAngle / 360f) * 360f;
                crankAngle = Mathf.MoveTowards(crankAngle, target, 540f * dt);
            }
            else
            {
                // The run is far faster than real cycling, so cadence is styled rather than geared.
                crankAngle += (240f + speed * 11f) * dt;
            }
            crankAngle = Mathf.Repeat(crankAngle + 360f, 720f) - 360f;
            standing = Mathf.MoveTowards(standing, air ? 1f : 0f, dt * 7f);

            if (crank) crank.localRotation = crankRest * Quaternion.Euler(crankAngle, 0f, 0f);
            if (frontWheel) frontWheel.localRotation = frontRest * Quaternion.Euler(wheelAngle, 0f, 0f);
            if (rearWheel) rearWheel.localRotation = rearRest * Quaternion.Euler(wheelAngle, 0f, 0f);
            if (!body) return;

            // Two neighbouring keys are blended around the stroke; the basis is phase 0.
            float p = Mathf.Repeat(crankAngle, 360f) / 60f;
            int key = Mathf.FloorToInt(p) % 6;
            float f = p - Mathf.Floor(p);
            float seated = 1f - standing;
            for (int i = 0; i < pedal.Length; i++)
            {
                if (pedal[i] < 0) continue;
                int phase = i + 1;
                float w = phase == key ? 1f - f : phase == (key + 1) % 6 ? f : 0f;
                body.SetBlendShapeWeight(pedal[i], w * seated * FrameWeight);
            }
            if (stand >= 0) body.SetBlendShapeWeight(stand, standing * FrameWeight);
        }
    }
}
