using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Game camera behind and above the rider, as framed in the asset pack scenes.
    /// Widens its field of view with speed, shakes on hits, and swings round to face the rider when busted.
    public class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new(0f, 4.2f, -7.5f);
        public float lookAhead = 8f;
        public float lateralFollow = 0.5f;
        public float smoothing = 10f;
        public float fovSlow = 60f, fovFast = 72f;
        /// StoryDirector is moving the camera itself.
        public bool cinematic;

        Camera cam;
        float shake, kick, bustT;

        void Awake() => cam = GetComponent<Camera>();

        // Reduce Motion (settings) turns off the shake and the field-of-view kick.
        public void Shake(float amount) { if (!PlayerProfile.ReduceMotion) shake = Mathf.Max(shake, amount); }
        public void Kick(float amount) { if (!PlayerProfile.ReduceMotion) kick = Mathf.Max(kick, amount); }

        void LateUpdate()
        {
            if (!target || cinematic) return;
            var gm = GameManager.Instance;
            float dt = Time.deltaTime;
            Vector3 t = target.position;

            // Title (01 Cover) and Busted (19) both use the front two-shot of rider and officer.
            bool busted = gm && (gm.State == RunState.Busted || gm.State == RunState.Ready);
            if (gm && gm.State == RunState.Ready) bustT = 1f;
            else if (gm && gm.State == RunState.Intro) bustT = 0f; // the intro hands over already behind the rider
            else bustT = Mathf.MoveTowards(bustT, busted ? 1f : 0f, dt * 1.2f);
            float e = bustT * bustT * (3f - 2f * bustT);

            Vector3 behind = new(t.x * lateralFollow + offset.x, offset.y + t.y * 0.25f, t.z + offset.z);
            Vector3 front = new(t.x + 1.4f, 2.1f, t.z + 6.2f);
            Vector3 want = Vector3.Lerp(behind, front, e);
            Vector3 pos = Vector3.Lerp(transform.position, want, 1f - Mathf.Exp(-smoothing * dt));
            if (e < 0.01f) pos.z = want.z;

            shake = Mathf.MoveTowards(shake, 0f, dt * 1.5f);
            pos += Random.insideUnitSphere * shake * 0.35f;
            transform.position = pos;

            Vector3 look = Vector3.Lerp(new Vector3(t.x * lateralFollow, 1f, t.z + lookAhead), t + new Vector3(-0.7f, 1.1f, 0f), e);
            transform.LookAt(look);

            if (cam && gm)
            {
                kick = Mathf.MoveTowards(kick, 0f, dt * 3f);
                float speed01 = Mathf.InverseLerp(Tuning.SpeedBase, Tuning.SpeedMax, gm.Speed);
                float fov = Mathf.Lerp(fovSlow, fovFast, gm.State == RunState.Running ? speed01 : 0f) + kick * 4f - e * 12f;
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, 1f - Mathf.Exp(-6f * dt));
            }
        }
    }
}
