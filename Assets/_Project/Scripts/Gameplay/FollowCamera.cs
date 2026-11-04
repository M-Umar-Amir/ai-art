using DallasDash.Core;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

namespace DallasDash.Gameplay
{
    // Cinemachine owns positioning, damping, shot blends and impulses. StoryDirector only authors
    // the cinematic rig's pose, so it no longer competes with the actual output camera.
    public class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new(0, 4.2f, -7.5f);
        public float lookAhead = 8f, lateralFollow = 0.5f, smoothing = 10f;
        public float fovSlow = 60f, fovFast = 66f;
        Camera output;
        CinemachineBrain brain;
        CinemachineCamera run, title, result, story;
        Transform rigs, tracking, runAim, titleAim, resultAim;
        bool storyActive;
        public bool MovieFraming;
        float stableX, stableY, xVelocity, yVelocity, stableFov;
        bool trackingReady;
        public bool cinematic
        {
            get => storyActive;
            set
            {
                Ensure();
                if (value && !storyActive)
                {
                    story.transform.SetPositionAndRotation(transform.position, transform.rotation);
                    story.Lens.FieldOfView = output.fieldOfView;
                }
                storyActive = value;
            }
        }
        public Transform CinematicRig { get { Ensure(); return story.transform; } }
        public bool UsesCinemachine => brain && run;
        void Start() => Ensure();

        Transform Anchor(string name)
        {
            var t = new GameObject(name).transform; t.SetParent(rigs, false); return t;
        }
        CinemachineCamera Shot(string name, Vector3 followOffset, Transform aim, float fov)
        {
            var camera = new GameObject(name).AddComponent<CinemachineCamera>();
            camera.transform.SetParent(rigs, false);
            camera.Follow = tracking; camera.LookAt = aim; camera.Lens.FieldOfView = fov;
            var follow = camera.gameObject.AddComponent<CinemachineFollow>();
            follow.FollowOffset = followOffset;
            follow.TrackerSettings.BindingMode = BindingMode.WorldSpace;
            follow.TrackerSettings.PositionDamping = new Vector3(0.14f, 0.18f, 0f);
            camera.gameObject.AddComponent<CinemachineHardLookAt>();
            camera.Priority = 0; return camera;
        }
        void Ensure()
        {
            if (brain) return;
            output = GetComponent<Camera>();
            brain = GetComponent<CinemachineBrain>() ?? gameObject.AddComponent<CinemachineBrain>();
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
            brain.IgnoreTimeScale = true;
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.55f);
            rigs = new GameObject("Dash Riders / Cinemachine shots").transform;
            tracking = Anchor("Rider tracking"); runAim = Anchor("Run composition");
            titleAim = Anchor("Title composition"); resultAim = Anchor("Result composition");
            run = Shot("Delivery follow", offset, runAim, fovSlow);
            run.GetComponent<CinemachineFollow>().TrackerSettings.PositionDamping = Vector3.zero;
            title = Shot("Crew hero", new Vector3(0, 2.1f, 6.8f), titleAim, 48f);
            result = Shot("Run result", new Vector3(1.4f, 2.1f, 6.2f), resultAim, 48f);
            story = new GameObject("Delivery cinematic").AddComponent<CinemachineCamera>();
            story.transform.SetParent(rigs, false); story.Priority = 0;

        }
        public void Shake(float amount)
        {
            // Gameplay feedback must never disturb the viewing frame.
        }
        public void Kick(float amount) { }
        void LateUpdate()
        {
            if (!target) return;
            Ensure();
            var gm = GameManager.Instance; var p = target.position;
            var rider = target.GetComponent<RiderController>();
            Vector3 normal = rider ? rider.SurfaceNormal : Vector3.up;
            float onWall = 1f - Mathf.Abs(normal.y);
            Vector3 support = p - (rider ? normal * rider.JumpHeight * .9f : Vector3.zero);
            // Lateral follow is relative to the surface frame, so on the roofs the camera frames the roof
            // lanes rather than the street below.
            float frameX = rider ? rider.Traversal.Origin.x : 0f;
            // The roof course is wider than the street and its outer lane sat on the frame edge: follow further.
            float lateral = rider && rider.Traversal.Surface == RunSurface.Rooftop ? Mathf.Max(lateralFollow, .72f) : lateralFollow;
            float followX = Mathf.Lerp(frameX + (support.x - frameX) * lateral, support.x, onWall);
            if (MovieFraming) followX = support.x;
            const bool expressFollow = false;
            float baseY = support.y;
            if (!trackingReady || gm && gm.RunTime < .05f)
            { stableX = followX; stableY = baseY; xVelocity = yVelocity = 0f; stableFov = fovSlow; trackingReady = true; }
            if (Time.deltaTime > 0f)
            {
                bool transfer = rider && (rider.Traversal.Surface != RunSurface.Road || rider.Traversal.Moving);
                stableX = Mathf.SmoothDamp(stableX, followX, ref xVelocity, transfer ? .1f : expressFollow ? .12f : MovieFraming ? .16f : .22f, Mathf.Infinity, Time.deltaTime);
                stableY = Mathf.SmoothDamp(stableY, baseY, ref yVelocity, transfer ? .1f : .28f, Mathf.Infinity, Time.deltaTime);
            }
            tracking.position = new Vector3(stableX, stableY, p.z);
            // Keep the lens inside the traversable volume. A fixed positive Y offset put it above
            // the ceiling, obscuring the entire rider whenever gravity was flipped.
            Vector3 followOffset = normal * 3.2f + Vector3.up + Vector3.back * 7.5f;
            if (MovieFraming && DashRun.District==1 && gm && gm.Distance<100f)
            {
                float orbit = 1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(55f,100f,gm.Distance));
                followOffset = Vector3.Lerp(followOffset,new Vector3(-11f,10f,-13f),orbit);
            }
            run.GetComponent<CinemachineFollow>().FollowOffset = followOffset;
            runAim.position = new Vector3(stableX, stableY, p.z + Mathf.Lerp(lookAhead, 1.5f, onWall)) + normal * 0.9f;
            titleAim.position = p + Vector3.up * 0.8f; resultAim.position = p + new Vector3(-0.7f, 1.1f, 0);
            run.Priority = !storyActive && gm && gm.State == RunState.Running ? 20 : 0;
            title.Priority = !storyActive && gm && gm.State == RunState.Ready ? 20 : 0;
            result.Priority = !storyActive && gm && gm.State == RunState.Busted ? 20 : 0;
            story.Priority = storyActive ? 100 : 0;
            float pace = gm ? Mathf.InverseLerp(Tuning.SpeedBase, Tuning.SpeedMax, gm.Speed) : 0f;
            stableFov = Mathf.Lerp(stableFov, Mathf.Lerp(fovSlow, Mathf.Min(fovFast, 66f), pace), 1f - Mathf.Exp(-2f * Time.deltaTime));
            run.Lens.FieldOfView = stableFov;
            brain.ManualUpdate();
        }
        void OnDestroy()
        {
            if (rigs) Destroy(rigs.gameObject);
        }
    }
}
