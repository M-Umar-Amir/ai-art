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
        /// For poses only: jumping, or mid-hop on the rooftop parkour line.
        public bool InAir => !grounded || Traversal.Hopping;
        public float JumpHeight => Mathf.Max(0f, y - groundHeight);
        public float LandingImpact { get; private set; }
        public SurfaceTraversal Traversal { get; } = new SurfaceTraversal();
        public Vector3 SurfaceNormal => Traversal.Normal;
        bool jumpPending;
        bool grounded = true;
        float groundHeight;
        TrackSpawner track;
        float lastGroundedAt = -100f;

        /// Where the wheels are now in the street cross-section (lane offset and jump height included).
        Vector3 Contact => Traversal.Origin + Traversal.Rotation * new Vector3(x, y, 0f);

        public void AttachSurface(RunSurface surface)
        {
            if (Traversal.Surface == surface) return;
            Traversal.Change(surface, Contact, Tuning.LaneX[1]);
            Settle(1); // local centre of the new wall/ceiling; no residual road-lane displacement
        }

        /// Up onto the roofs over the truck and the fire-escape landing (RooftopCourse steps), arriving in the
        /// street-side roof lane. The same line on foot or on the e-bike: real things to jump onto.
        public void AscendToRooftops()
        {
            var gm = GameManager.Instance;
            Traversal.Ascend(Contact, Tuning.LaneX[0], gm.Speed, RooftopCourse.ZoneStartAt(gm.Distance), gm.Distance);
            Settle(0);
        }

        /// The planned way down off the last roof (onto the skip, then the street), or a fall into an alley gap.
        public void DropToStreet(bool fell)
        {
            var gm = GameManager.Instance;
            if (fell) Traversal.Drop(Contact, Tuning.LaneX[RooftopCourse.EntryLane]);
            else Traversal.Descend(Contact, Tuning.LaneX[RooftopCourse.EntryLane], gm.Speed, RooftopCourse.ZoneStartAt(gm.Distance), gm.Distance);
            Settle(RooftopCourse.EntryLane);
            if (fell) { wobble = 1f; gm.Bonk(); }
        }

        /// Fell past the roof line into a gap between buildings.
        public bool Fallen => Traversal.Surface == RunSurface.Rooftop && !Traversal.Moving && y < -1.2f;

        void Settle(int lane)
        {
            Lane = lane; x = Tuning.LaneX[lane]; lateralVelocity = 0f;
            y = vy = jumpBuffer = groundHeight = 0f; jumpPending = false; grounded = true;
        }

        public event Action Jumped, Dropped, Landed, LaneChanged;

        /// Ground speed outside a run (the story intro): 0 while parked at the curb, so the bike stands still
        /// instead of pedalling in place. During a run the pace comes from GameManager.Speed.
        [NonSerialized] public float cruiseSpeed;

        float x, y, vy, squash, lean, wobble, pedal, jumpBuffer, lateralVelocity, slideLay;

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
            int lane = Lane;
            float dt = Time.deltaTime;
            bool hasGround = true;
            float nextGround = 0f;
            if (track && Traversal.Surface == RunSurface.Road && !Traversal.Moving)
                hasGround = track.GroundSupport(x, gm.Distance, y, out nextGround);
            // On the roofs the ground is the block row itself: alley gaps have none, except on a skybridge.
            if (Traversal.Surface == RunSurface.Rooftop && !Traversal.Moving)
                hasGround = RooftopCourse.Supported(gm.Distance, x);
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
                case RiderInput.Action.Right:
                    if (!Traversal.Moving) Steer(action == RiderInput.Action.Left ? -1 : 1);
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
                DashRun.Instance?.TrickAir(DashRun.Instance.Board ? 3 : 0);
                Missions.Add(MissionStat.Jumps, 1);
            }
            jumpBuffer = Mathf.Max(0f, jumpBuffer - dt);

            var dynamics = VehicleDynamics.Current;
            float remaining = Tuning.LaneX[Lane] - x;
            float desired = Mathf.Sign(remaining) * Mathf.Min(dynamics.steering * handling, Mathf.Sqrt(2f * dynamics.acceleration * Mathf.Abs(remaining)));
            lateralVelocity = Mathf.MoveTowards(lateralVelocity, desired, dynamics.acceleration * dt);
            // A reversal can carry residual velocity away from the target; clamp only overshoot.
            float dx = lateralVelocity * dt;
            if (dx * remaining >= 0f && Mathf.Abs(dx) >= Mathf.Abs(remaining))
            { dx = remaining; lateralVelocity = 0f; }
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
                    DashRun.Instance?.LandTrick();
                    jumpPending = false;
                }
                if (y < -4f && Traversal.Surface != RunSurface.Rooftop)
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

            if (Traversal.Surface == RunSurface.Rooftop && y < -3f) { y = -3f; vy = Mathf.Min(vy, 0f); }
            bool wasMoving = Traversal.Moving;
            Traversal.Tick(dt);
            if (wasMoving && !Traversal.Moving && Traversal.Surface is RunSurface.LeftWall or RunSurface.RightWall or RunSurface.Ceiling)
                DashRun.Instance?.TrickAir(DashRun.GravityZone(gm.Distance) ? 4 : 5);
            transform.SetPositionAndRotation(Traversal.Position(x, y, gm.Distance), Traversal.Rotation);
            Pose(dt, dt > 0f ? dx / dt : 0f);
        }

        /// A left/right swipe. In the street it changes lane; from an outer lane, steering on into the building
        /// starts a wall run along its facade (DashRun decides whether this rider may). On a wall the rider keeps
        /// one running line: steering away from the wall drops back to the street, steering into it does nothing.
        void Steer(int dir)
        {
            var dash = DashRun.Instance;
            var surface = Traversal.Surface;
            if (surface is RunSurface.LeftWall or RunSurface.RightWall)
            {
                int wallSide = surface == RunSurface.LeftWall ? -1 : 1;
                if (dir != wallSide) dash?.LeaveWall();
                return;
            }
            int edge = dir < 0 ? 0 : Tuning.Lanes - 1;
            if (surface == RunSurface.Road && Lane == edge)
            {
                dash?.TryWallRun(dir);
                return;
            }
            Lane = Mathf.Clamp(Lane + dir, 0, Tuning.Lanes - 1);
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
            float pace = gm && gm.State == RunState.Running && !gm.Paused ? gm.Speed : gm && gm.State == RunState.Intro ? cruiseSpeed : 0f;
            bool robot = VehicleDynamics.Current.kind == CourierVehicle.MonoWheel;
            bool board = VehicleDynamics.Current.kind is CourierVehicle.ElectricBoard or CourierVehicle.GlideBoard;
            bool bike = VehicleDynamics.Current.kind == CourierVehicle.Bicycle;
            bool riding = pace > 0.5f && !Airborne;
            pedal += dt * pace * 0.55f;
            // Ease the pedalling motion in as the rider pulls away rather than snapping to full sway.
            float amount = Mathf.Clamp01(pace / 8f);
            float bob = riding && !robot ? (board ? Mathf.Sin(pedal * 0.55f) * 0.012f : Mathf.Abs(Mathf.Sin(pedal)) * 0.025f) * amount : 0f;
            float sway = riding ? Mathf.Sin(pedal) * (board ? 0.45f : 1.1f) * amount : 0f;
            wobble = Mathf.MoveTowards(wobble, 0f, dt * 1.6f);
            float wob = Mathf.Sin(Time.time * 28f) * 14f * wobble;
            squash = Mathf.MoveTowards(squash, 0f, dt * 2.2f);
            float s = Mathf.Sin(squash * Mathf.PI * 2f) * (robot ? 0.007f : 0.012f);
            var scale = Vector3.Scale(visualScale, new Vector3(1f + s, 1f - s, 1f + s));
            visual.localScale = scale;
            var dash = DashRun.Instance;
            bool slide = dash && dash.Slide > 0f;
            if (slide && !robot && !bike) scale.y *= 0.56f;
            visual.localScale = scale;
            // Under a gate the e-bike goes into a low layback slide: laid over on its side, skidding sideways.
            slideLay = Mathf.MoveTowards(slideLay, slide && bike ? 1f : 0f, dt * 6f);
            float lay = Mathf.SmoothStep(0f, 1f, slideLay);
            // Running up a facade the rider turns to face up the wall (the frame's lane axis is world up there).
            const float climb = 0f;
            visual.localRotation = Quaternion.Euler(slide && !bike ? -12f : pitch * (board ? .45f : 1f) * (1f - lay),
                (board ? lateralSpeed * 0.45f : 0f) + wob * 0.4f - lay * 22f + climb,
                Mathf.Clamp(lean, board ? -12f : -18f, board ? 12f : 18f) * (1f - lay) + sway + wob + lay * 68f);
            visual.localPosition = visualPos + Vector3.up * bob + Vector3.right * lay * 0.35f;
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
