using System;
using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Surface-relative lane movement, jumping and slides. Forward travel follows GameManager.Distance.
    public class RiderController : MonoBehaviour
    {
        public float handling = 1f;
        public Transform visual;

        public int Lane { get; private set; } = 1;
        public bool Airborne => !grounded;
        public float JumpHeight => Mathf.Max(0f, y - groundHeight);
        public float LandingImpact { get; private set; }
        public SurfaceTraversal Traversal { get; } = new SurfaceTraversal();
        public Vector3 SurfaceNormal => Traversal.Normal;
        bool jumpPending;
        bool grounded = true;
        float groundHeight;
        TrackSpawner track;
        float lastGroundedAt = -100f;

        public void AttachSurface(RunSurface surface)
        {
            if (Traversal.Surface == surface) return;
            Traversal.Change(surface);
            Lane = 1; // local centre of the new wall/ceiling; no residual road-lane displacement
            y = vy = jumpBuffer = groundHeight = 0f; jumpPending = false; grounded = true;
        }

        public event Action Jumped, Dropped, Landed, LaneChanged;

        /// Ground speed outside a run (the story intro): 0 while parked at the curb, so the bike stands still
        /// instead of pedalling in place. During a run the pace comes from GameManager.Speed.
        [NonSerialized] public float cruiseSpeed;

        float x, y, vy, squash, lean, wobble, pedal, jumpBuffer, lateralVelocity;

        /// Knocked off balance by an obstacle: a quick side-to-side wobble (TrackSpawner.Struck).
        public void Wobble() => wobble = 1f;

        /// Swiped the side of a lane-filling obstacle: knocked back into the lane the rider came from.
        public void BounceBack(int lane)
        {
            Lane = Mathf.Clamp(lane, 0, Tuning.Lanes - 1);
            wobble = 1f;
            squash = 0.25f;
        }
        Vector3 visualScale, visualPos;

        void Start()
        {
            track = FindFirstObjectByType<TrackSpawner>();
            if (visual) { visualScale = visual.localScale; visualPos = visual.localPosition; }
            GameManager.Instance.StateChanged += s => { if (s == RunState.Running) ResetRider(); };
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != RunState.Running || gm.Paused) { Pose(Time.deltaTime, 0f); return; }

            var action = Time.frameCount == gm.StartFrame ? RiderInput.Action.None : RiderInput.Poll();
            if (Lane == Tuning.RouteLane && !DashRun.RouteOpen(gm.Distance)) Lane = Tuning.Lanes - 1;
            int lane = Lane;
            float dt = Time.deltaTime;
            bool hasGround = true;
            float nextGround = 0f;
            if (track && Traversal.Surface == RunSurface.Road && !Traversal.Moving)
                hasGround = track.GroundSupport(x, gm.Distance, y, out nextGround);
            if (grounded && hasGround)
            {
                if (Mathf.Abs(nextGround - groundHeight) < 0.6f) y += nextGround - groundHeight;
                else grounded = false;
            }
            if (!hasGround) grounded = false;
            groundHeight = nextGround;
            if (grounded && hasGround) lastGroundedAt = gm.RunTime;
            switch (action)
            {
                case RiderInput.Action.Left:
                    // The route is a commitment: there is no lane to the left of it, only a drop.
                    if (Lane != Tuning.RouteLane) Lane = Mathf.Max(0, Lane - 1);
                    RiderInput.ClearBuffer();
                    break;
                case RiderInput.Action.Right:
                    // The fourth lane opens only at a route's lead-in, so the rider always joins it on
                    // the flat kerb-height approach instead of clipping through a raised deck.
                    if (Lane < Tuning.Lanes - 1) Lane++;
                    else if (Lane == Tuning.Lanes - 1 && DashRun.RouteEntry(gm.Distance)) Lane = Tuning.RouteLane;
                    RiderInput.ClearBuffer();
                    break;
                case RiderInput.Action.Jump:
                    if (!(DashRun.Instance && DashRun.Instance.JumpMove())) jumpBuffer = Tuning.JumpBufferSeconds;
                    RiderInput.ClearBuffer();
                    break;
                case RiderInput.Action.Down:
                    DashRun.Instance?.StartSlide();
                    // Bunny-hopped too early: pull the bike back down to the road. Does nothing on the ground.
                    if (Airborne && vy > Tuning.DiveV) { vy = Tuning.DiveV; jumpBuffer = 0f; Dropped?.Invoke(); }
                    RiderInput.ClearBuffer();
                    break;
            }
            if (lane != Lane) LaneChanged?.Invoke();
            if (jumpBuffer > 0f && (grounded || gm.RunTime - lastGroundedAt < 0.09f) && vy <= 0f)
            {
                jumpBuffer = 0f;
                lastGroundedAt = -100f;
                vy = VehicleDynamics.Current.jump;
                grounded = false;
                squash = -0.25f;
                Jumped?.Invoke();
                jumpPending = true;
                Missions.Add(MissionStat.Jumps, 1);
            }
            jumpBuffer = Mathf.Max(0f, jumpBuffer - dt);

            var dynamics = VehicleDynamics.Current;
            float remaining = Tuning.LaneX[Lane] - x;
            float desired = Mathf.Sign(remaining) * Mathf.Min(dynamics.steering * handling, Mathf.Sqrt(2f * dynamics.acceleration * Mathf.Abs(remaining)));
            lateralVelocity = Mathf.MoveTowards(lateralVelocity, desired, dynamics.acceleration * dt);
            float dx = Mathf.Clamp(lateralVelocity * dt, -Mathf.Abs(remaining), Mathf.Abs(remaining));
            x += dx;

            if (Airborne || vy > 0f)
            {
                float previousFeetY = y;
                float gravity = dynamics.gravity * (DashRun.Instance ? DashRun.Instance.JumpGravityFactor : 1f);
                y += vy * dt - 0.5f * gravity * dt * dt;
                vy -= gravity * dt;
                if (hasGround && previousFeetY >= groundHeight - .1f && y <= groundHeight && vy <= 0f)
                {
                    LandingImpact = Mathf.Abs(vy);
                    y = groundHeight; vy = 0f; grounded = true; squash = Mathf.Clamp(LandingImpact / 40f, .1f, .35f); Landed?.Invoke();
                    if (jumpPending) DashRun.Instance?.Trick(DashRun.Instance.Board ? 3 : 0);
                    jumpPending = false;
                }
                if (y < -4f)
                {
                    DashRun.Instance?.PenalizeTime(4f); gm.Bonk();
                    ResetRider();
                    for (int safe = 0; safe < Tuning.Lanes; safe++)
                        if (track.GroundSupport(Tuning.LaneX[safe], gm.Distance, out float top))
                        { Lane = safe; x = Tuning.LaneX[safe]; y = groundHeight = top; break; }
                    DallasDash.UI.MenuUI.Instance?.Shout("MISSED LANDING / -4s");
                    transform.position = new Vector3(x, y, gm.Distance);
                    return;
                }
            }

            // Counted once per entry and refreshed while the rider stays out on the route. Being airborne
            // over a rooftop hop must not break the run, so this deliberately ignores the grounded flag.
            DashRun.Instance?.RideRoute(Lane == Tuning.RouteLane && x > 4.5f && DashRun.RouteZone(gm.Distance));
            bool wasMoving = Traversal.Moving;
            Traversal.Tick(dt);
            if (wasMoving && !Traversal.Moving && Traversal.Surface != RunSurface.Road)
                DashRun.Instance?.Trick(DashRun.GravityZone(gm.Distance) ? 4 : 5);
            transform.SetPositionAndRotation(Traversal.Position(x, y, gm.Distance), Traversal.Rotation);
            Pose(dt, dt > 0f ? dx / dt : 0f);
        }

        /// Lean into lane changes, pitch with the jump arc, squash on take-off and landing.
        void Pose(float dt, float lateralSpeed)
        {
            if (!visual) return;
            lean = Mathf.Lerp(lean, -lateralSpeed * 1.4f, 1f - Mathf.Exp(-12f * dt));
            float pitch = Airborne ? Mathf.Clamp(-vy * 1.6f, -18f, 18f) : 0f;
            // Pedalling: cadence follows speed; a small bob and alternating sway while on the ground.
            var gm = GameManager.Instance;
            // Only while the bike is actually moving: in the Loading Up scene it is parked at the curb, and the
            // run's base speed used to make it sway and bob on the spot (the client saw it shaking).
            float pace = gm && gm.State == RunState.Running ? gm.Speed : gm && gm.State == RunState.Intro ? cruiseSpeed : 0f;
            bool riding = pace > 0.5f && !Airborne;
            pedal += dt * pace * 0.55f;
            // Ease the pedalling motion in as the rider pulls away rather than snapping to full sway.
            float amount = Mathf.Clamp01(pace / 8f);
            float bob = riding ? Mathf.Abs(Mathf.Sin(pedal)) * 0.035f * amount : 0f;
            float sway = riding ? Mathf.Sin(pedal) * 2.2f * amount : 0f;
            wobble = Mathf.MoveTowards(wobble, 0f, dt * 1.6f);
            float wob = Mathf.Sin(Time.time * 28f) * 14f * wobble;
            squash = Mathf.MoveTowards(squash, 0f, dt * 2.2f);
            float s = Mathf.Sin(squash * Mathf.PI * 2f) * (VehicleDynamics.Current.kind == CourierVehicle.MonoWheel ? 0.015f : 0.06f);
            var scale = Vector3.Scale(visualScale, new Vector3(1f + s, 1f - s, 1f + s));
            visual.localScale = scale;
            var dash = DashRun.Instance;
            bool slide = dash && dash.Slide > 0f;
            if (slide && VehicleDynamics.Current.kind != CourierVehicle.MonoWheel) scale.y *= 0.56f;
            visual.localScale = scale;
            visual.localRotation = Quaternion.Euler(slide ? -12f : pitch, wob * 0.4f, Mathf.Clamp(lean, -22f, 22f) + sway + wob);
            visual.localPosition = visualPos + Vector3.up * bob;
        }

        public void ResetRider()
        {
            Lane = 1;
            x = y = vy = squash = lean = wobble = jumpBuffer = lateralVelocity = 0f;
            groundHeight = 0f; grounded = true;
            lastGroundedAt = -100f; LandingImpact = 0f;
            jumpPending = false; Traversal.Reset(); transform.rotation = Quaternion.identity;
        }
    }
}
