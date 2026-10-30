using DallasDash.Core;
using DG.Tweening;
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
        public float fovSlow = 60f, fovFast = 72f;
        Camera output;
        CinemachineBrain brain;
        CinemachineCamera run, title, result, story;
        CinemachineImpulseSource impulse;
        CinemachineImpulseListener listener;
        Transform rigs, tracking, runAim, titleAim, resultAim;
        bool storyActive;
        public bool MovieFraming;
        float kick;
        Tween kickTween;
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
            brain.IgnoreTimeScale = false;
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.55f);
            rigs = new GameObject("Dash Riders / Cinemachine shots").transform;
            tracking = Anchor("Rider tracking"); runAim = Anchor("Run composition");
            titleAim = Anchor("Title composition"); resultAim = Anchor("Result composition");
            run = Shot("Delivery follow", offset, runAim, fovSlow);
            title = Shot("Crew hero", new Vector3(0, 2.1f, 6.8f), titleAim, 48f);
            result = Shot("Run result", new Vector3(1.4f, 2.1f, 6.2f), resultAim, 48f);
            story = new GameObject("Delivery cinematic").AddComponent<CinemachineCamera>();
            story.transform.SetParent(rigs, false); story.Priority = 0;
            impulse = GetComponent<CinemachineImpulseSource>() ?? gameObject.AddComponent<CinemachineImpulseSource>();
            impulse.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
            impulse.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            impulse.ImpulseDefinition.ImpulseDuration = 0.22f;
            impulse.ImpulseDefinition.ImpulseChannel = 1;
            listener = run.gameObject.AddComponent<CinemachineImpulseListener>();
            listener.ChannelMask = 1; listener.Gain = 0.4f; listener.UseCameraSpace = true;
        }
        public void Shake(float amount)
        {
            if (PlayerProfile.ReduceMotion) return;
            Ensure(); impulse.GenerateImpulseWithVelocity(new Vector3(amount * 0.3f, -amount * 0.45f, 0));
        }
        public void Kick(float amount)
        {
            if (PlayerProfile.ReduceMotion) return;
            kickTween?.Kill(); kick = Mathf.Max(kick, amount);
            kickTween = DOTween.To(() => kick, v => kick = v, 0f, 0.35f).SetEase(Ease.OutCubic).SetLink(gameObject);
        }
        void LateUpdate()
        {
            if (!target) return;
            Ensure();
            var gm = GameManager.Instance; var p = target.position;
            var rider = target.GetComponent<RiderController>();
            Vector3 normal = rider ? rider.SurfaceNormal : Vector3.up;
            float onWall = 1f - Mathf.Abs(normal.y);
            float followX = Mathf.Lerp(p.x * lateralFollow, p.x, onWall);
            if (rider && rider.Lane == Tuning.RouteLane) followX = p.x;
            tracking.position = new Vector3(followX, p.y, p.z);
            // Keep the lens inside the traversable volume. A fixed positive Y offset put it above
            // the ceiling, obscuring the entire rider whenever gravity was flipped.
            Vector3 followOffset = normal * 3.2f + Vector3.up + Vector3.back * 7.5f;
            if (MovieFraming && DashRun.District==1 && gm && gm.Distance<100f)
            {
                float orbit = 1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(55f,100f,gm.Distance));
                followOffset = Vector3.Lerp(followOffset,new Vector3(-11f,10f,-13f),orbit);
            }
            run.GetComponent<CinemachineFollow>().FollowOffset = followOffset;
            runAim.position = new Vector3(followX, p.y, p.z + Mathf.Lerp(lookAhead, 1.5f, onWall)) + normal * 0.9f;
            titleAim.position = p + Vector3.up * 0.8f; resultAim.position = p + new Vector3(-0.7f, 1.1f, 0);
            run.Priority = !storyActive && gm && gm.State == RunState.Running ? 20 : 0;
            title.Priority = !storyActive && gm && gm.State == RunState.Ready ? 20 : 0;
            result.Priority = !storyActive && gm && gm.State == RunState.Busted ? 20 : 0;
            story.Priority = storyActive ? 100 : 0;
            float pace = gm ? Mathf.InverseLerp(Tuning.SpeedBase, Tuning.SpeedMax, gm.Speed) : 0f;
            run.Lens.FieldOfView = Mathf.Lerp(fovSlow, fovFast, pace) + kick * 4f;
            listener.Gain = PlayerProfile.ReduceMotion ? 0f : 0.4f;
            brain.ManualUpdate();
        }
        void OnDestroy()
        {
            kickTween?.Kill();
            if (rigs) Destroy(rigs.gameObject);
        }
    }
}
