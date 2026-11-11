using DallasDash.Gameplay;
using DallasDash.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DallasDash.Core
{
    /// The run's rules (client round, 4 Nov 2026):
    /// - One map, Neon Downtown, and one rider, Ace on his e-bike.
    /// - A POLICE CHASE replaces the delivery clock: an officer rides behind; crashes and running on foot let him
    ///   close in, riding clean on the bike opens the gap again. When he reaches Ace the run is BUSTED.
    /// - No shortcut (client, 11 Nov 2026): the Skypark portal was removed; the whole run is Downtown.
    /// - A busted or wiped-out run can be revived on the spot with burgers (BurgerPrices.Revive).
    /// - No kiosks, no fuel, no delivery timer.
    public class DashRun : MonoBehaviour
    {
        public static DashRun Instance { get; private set; }
        public static readonly string[] CrewNames = { "ACE" };
        public static readonly string[] AbilityNames = { "SPEED BOOST" };
        public static readonly string[] DistrictNames = { "NEON DOWNTOWN" };

        /// The only world: Downtown (0). Older builders still branch on District for the retired Skypark (1) and
        /// Festivals (2); those branches are dead and are being removed file by file.
        public static int District => 0;

        public float Battery { get; private set; } = 1f;
        public float Boost { get; private set; }
        public float FloatTime { get; private set; }
        public bool Shield { get; private set; }
        /// Retired hoverboard power-up state; always false.
        public bool Board => false;
        /// Knocked off the e-bike by a crash: on foot until the bike is back.
        public bool OnFoot { get; private set; }
        /// Where the parked e-bike waits (distance and lane) while the rider is on foot in Downtown.
        public float BikeAt { get; private set; }
        public int BikeLane { get; private set; } = 1;
        /// Wall running was removed (client, 4 Nov 2026, later the same day). Kept so older call sites read naturally.
        public static bool CanRideWalls => false;
        /// The old ramp-up rooftop course is retired, and there is no shortcut at all now.
        public static bool CanTakeRooftops => false;
        bool warnedWall, parkPending;
        public int WallSide => Surface == RunSurface.LeftWall ? -1 : Surface == RunSurface.RightWall ? 1 : 0;
        public RunSurface Surface => rider ? rider.Traversal.Surface : RunSurface.Road;
        public bool Flipped => Surface == RunSurface.Ceiling;
        public bool WallRunning => Surface == RunSurface.LeftWall || Surface == RunSurface.RightWall;
        public float DashTime { get; private set; }
        public float DashCooldown { get; private set; }
        public float Slide { get; private set; }
        public int TrickCombo { get; private set; }
        /// Why the last run ended, shown on the results card.
        public static string EndReason { get; private set; } = "";
        public int WallRuns { get; private set; }

        // ---------- Police chase
        /// Metres between Ace and the officer. 0 = caught.
        public float PoliceGap { get; private set; } = StartGap;
        public const float StartGap = 14f, MaxGap = 24f;
        /// The officer's bike outpaces a runner; the e-bike pulls away from him.
        /// 11 Nov 2026: with no Skypark to shake him off, clean riding must win back a crash in about five seconds.
        public const float GainOnFoot = 1.7f, LoseOnBike = 1.0f;
        public const float CrashOnBike = 5f, CrashOnFoot = 9f, MissedLanding = 6f;
        /// 1 when the officer is right behind, 0 when he is at the far edge (HUD meter, siren loudness).
        public float Heat => Mathf.Clamp01(1f - PoliceGap / MaxGap);
        /// The officer is always on the rider's tail (no shortcut shakes him off any more).
        public bool PoliceActive => true;

        // ---------- Revive
        public int Revives { get; private set; }
        public int ReviveCost => BurgerPrices.Revive(Revives);
        /// Seconds of protection after a revive: hits bounce off, the officer can't close in.
        public float ReviveGrace { get; private set; }
        /// True while the revive offer is on screen (the run is frozen).
        public bool AwaitingRevive { get; private set; }
        /// Review and bot modes: decline revives automatically unless a test asks for one.
        public static bool AutoDeclineRevive;

        public static bool WallZone(float d) => false;

        /// A building face lines this side of the street at z: not across a junction's cross road.
        public static bool WallAvailable(int side, float z)
        {
            if (side == 0 || z < 20f || TrackSpawner.NearJunction(z)) return false;
            return true;
        }
        public float WallRunSeconds => PowerUps.Active(PowerUp.Boots) ? 4f : 2.6f;
        public static int RoofGapLane(float z) => -1;
        public static bool GravityZone(float d) => false;
        public static float GravityZoneStart(float d) => d - Mathf.Repeat(d - 700f, 960f);

        public bool OnRooftops => Surface == RunSurface.Rooftop;
        public float SurfaceHeight => rider ? rider.Traversal.Origin.y : 0f;
        public float JumpGravityFactor => FloatTime > 0f || PowerUps.Active(PowerUp.Boots) ? 0.68f : 1f;
        float trickTime, wallTime;
        int lastTrick = -1;
        int pendingTrickKind = -1;
        RiderController rider;
        TrackSpawner track;

        void Awake() { Instance = this; }
        void Start()
        {
            rider = FindFirstObjectByType<RiderController>();
            track = FindFirstObjectByType<TrackSpawner>();
            GameManager.Instance.StateChanged += State;
        }
        void OnDestroy() { if (GameManager.Instance) GameManager.Instance.StateChanged -= State; }

        void State(RunState s)
        {
            if (s == RunState.Running)
            {
                Battery = 1f; Boost = FloatTime = Slide = wallTime = trickTime = 0f;
                Shield = false;
                OnFoot = warnedWall = false; BikeAt = 0f;
                FindFirstObjectByType<RiderLook>()?.Apply(0, true);
                DashTime = DashCooldown = 0f;
                TrickCombo = 0; lastTrick = -1; pendingTrickKind = -1;
                WallRuns = 0;
                PoliceGap = StartGap; Revives = 0; ReviveGrace = 0f; AwaitingRevive = false;
                EndReason = "";
            }
            if (s == RunState.Ready || s == RunState.Intro)
            {
                AwaitingRevive = false;
            }
            if (s != RunState.Running) { rider?.Traversal.Reset(); Slide = 0f; }
        }
        public bool TryDeliveryDash()
        {
            if (!CanAct() || DashCooldown > 0f) return false;
            DashTime = 2.2f; DashCooldown = 12f;
            MenuUI.Instance?.Shout("DASH!");
            Haptics.Play(Haptics.Strength.Medium);
            return true;
        }

        public void TryAbility()
        {
            if (!CanAct() || Battery < 0.999f) return;
            Battery = 0f;
            Boost = 4.5f;
            MenuUI.Instance?.Shout(AbilityNames[0]);
            Haptics.Play(Haptics.Strength.Medium);
        }

        public bool AbsorbHit()
        {
            if (Boost > 0f || ReviveGrace > 0f) return true;
            if (Shield || PowerUps.Active(PowerUp.Shield))
            {
                Shield = false; PowerUps.Consume(PowerUp.Shield); return true;
            }
            return false;
        }
        public void Recharge() => Battery = Mathf.Min(1f, Battery + 0.4f);

        /// Kept for older callers (a missed landing): the officer gains ground instead of a time penalty.
        public void PenalizeTime(float seconds) => PoliceGains(seconds * 1.5f, "MISSED LANDING");

        /// The officer closes in by `metres`; at zero the run is busted.
        public void PoliceGains(float metres, string why = null)
        {
            if (!CanAct() || !PoliceActive || ReviveGrace > 0f) return;
            PoliceGap = Mathf.Max(0f, PoliceGap - Mathf.Max(0f, metres));
            if (PoliceGap <= 0f) { End("BUSTED!"); return; }
            if (!string.IsNullOrEmpty(why) && PoliceGap < 8f) MenuUI.Instance?.Shout(why + "  /  COP CLOSING IN!");
        }

        bool CanAct() => GameManager.Instance && GameManager.Instance.State == RunState.Running && !GameManager.Instance.Paused && !AwaitingRevive;
        public bool JumpMove()
        {
            Slide = 0f;
            if (!CanAct() || !rider) return false;
            if (OnRooftops) return false;
            if (rider.Traversal.Moving) return true;
            if (WallRunning)
            {
                rider.AttachSurface(RunSurface.Road);
                return true;
            }
            return false;
        }

        /// Steering into the building beside an outer lane starts a wall run along its facade (on foot only).
        public bool TryWallRun(int side)
        {
            // Wall running is gone: steering into a building is simply ignored.
            if (!CanRideWalls || !CanAct() || !rider || rider.Traversal.Moving || Surface != RunSurface.Road) return false;
            float d = GameManager.Instance.Distance;
            float reach = d + GameManager.Instance.Speed * (SurfaceTraversal.TransitionSeconds + 0.4f);
            if (!WallAvailable(side, d) || !WallAvailable(side, reach)) { MenuUI.Instance?.Shout("NO WALL HERE"); return false; }
            rider.AttachSurface(side < 0 ? RunSurface.LeftWall : RunSurface.RightWall);
            wallTime = WallRunSeconds;
            WallRuns++;
            MenuUI.Instance?.Shout("WALL RUN");
            Haptics.Play(Haptics.Strength.Light);
            return true;
        }

        public void LeaveWall() { if (WallRunning && rider && !rider.Traversal.Moving) rider.AttachSurface(RunSurface.Road); }
        public void StartSlide() { if (CanAct()) Slide = 0.85f; }

        /// A head-on crash: on the bike it knocks Ace off (the officer gains); on foot the officer gains a lot.
        public static string LastStruck = "";
        public void Crash()
        {
            if (!CanAct() || !rider) return;
            if (Surface != RunSurface.Road && Surface != RunSurface.Rooftop) return;
            Debug.Log($"[DashCrash] at {GameManager.Instance.Distance:0.0} lane={rider.Lane} surface={Surface} onFoot={OnFoot} gap={PoliceGap:0.0} object={LastStruck}");
            if (OnFoot) { PoliceGains(CrashOnFoot, "CRASHED"); Haptics.Play(Haptics.Strength.Medium); return; }
            KnockOffBike();
            PoliceGains(CrashOnBike, "KNOCKED OFF");
        }

        /// The run is over for `reason` unless the player revives with burgers.
        public void End(string reason)
        {
            if (!GameManager.Instance || GameManager.Instance.State != RunState.Running || AwaitingRevive) return;
            EndReason = reason;
            MenuUI.Instance?.Shout(reason);
            bool offered = false;
            if (!AutoDeclineRevive && MenuUI.Instance)
            {
                // Never let a UI fault leave the run stuck at zero gap: no offer, the run just ends.
                try { offered = MenuUI.Instance.OfferRevive(reason, ReviveCost); }
                catch (System.Exception e) { Debug.LogException(e); offered = false; }
            }
            if (offered)
            {
                AwaitingRevive = true;
                GameManager.Instance.SetPaused(true);
                return;
            }
            FinishRun();
        }

        /// The revive offer was declined or ran out: the run ends here.
        public void FinishRun()
        {
            AwaitingRevive = false;
            if (!GameManager.Instance || GameManager.Instance.State != RunState.Running) return;
            GameManager.Instance.SetPaused(false);
            GameManager.Instance.Bust();
        }

        /// Pays the revive price and carries on: back on the bike, the officer pushed back, the road ahead cleared.
        public bool Revive()
        {
            var gm = GameManager.Instance;
            if (!gm || gm.State != RunState.Running) return false;
            if (!PlayerProfile.TrySpend(ReviveCost)) return false;
            Revives++;
            AwaitingRevive = false;
            gm.SetPaused(false);
            EndReason = "";
            PoliceGap = MaxGap;
            ReviveGrace = 2.5f;
            rider?.ResetRider();
            if (OnFoot) { Remount(); track?.ClearBikeReservation(); track?.RemoveParkedBike(); }
            track?.ClearAhead(gm.Distance, 45f);
            MenuUI.Instance?.Caption("REVIVED!", "-" + BurgerPrices.Revive(Revives - 1).ToString("N0") + " burgers  /  keep riding", 1.6f);
            Haptics.Play(Haptics.Strength.Heavy);
            return true;
        }

        /// A crash throws Ace off the e-bike: the bike is left lying in the road and he runs on.
        public void KnockOffBike()
        {
            if (!CanAct() || OnFoot || !rider) return;
            if (Surface != RunSurface.Road || rider.Traversal.Moving) return;
            OnFoot = true;
            MenuUI.Instance?.Shout("KNOCKED OFF! GRAB THE BIKE");
            parkPending = true; // parked next frame: this runs inside the track's hit-test loop
            var look = FindFirstObjectByType<RiderLook>();
            look?.DropBike(0, rider.transform.position);
            look?.Apply(0, false);
            Haptics.Play(Haptics.Strength.Medium);
        }

        /// Puts the e-bike in a lane a few seconds ahead, never in a junction.
        void ParkBikeAhead(float d)
        {
            float z = d + Mathf.Max(60f, GameManager.Instance.Speed * 3.5f);
            for (int guard = 0; guard < 8; guard++)
            {
                if (TrackSpawner.NearJunction(z) || TrackSpawner.NearJunction(z + 8f)) { z += 30f; continue; }
                break;
            }
            int lane = -1;
            for (int tries = 0; tries < 24 && track; tries++, z += 6f)
            {
                if (TrackSpawner.NearJunction(z)) continue;
                int first = rider ? rider.Lane : 1;
                for (int k = 0; k < Tuning.Lanes && lane < 0; k++)
                {
                    int candidate = (first + k) % Tuning.Lanes;
                    if (track.LaneFree(candidate, z)) lane = candidate;
                }
                if (lane >= 0) break;
            }
            BikeAt = z;
            BikeLane = lane < 0 ? 1 : lane;
            track?.ParkBike(z, BikeLane);
        }

        public void Remount()
        {
            if (!OnFoot) return;
            OnFoot = false; warnedWall = false;
            FindFirstObjectByType<RiderLook>()?.Apply(0, true);
            MenuUI.Instance?.Shout("BACK ON THE E-BIKE");
        }

        public void Trick(int kind)
        {
            if (!CanAct() || lastTrick == kind && trickTime > 0f) return;
            TrickCombo = trickTime > 0f ? TrickCombo + 1 : 1;
            lastTrick = kind; trickTime = 3f;
            GameManager.Instance.AddBonus(15 * Mathf.Min(TrickCombo, 8));
            if (TrickCombo >= 3) MenuUI.Instance?.Shout("TRICK CHAIN x" + TrickCombo);
        }

        public void TrickAir(int kind)
        {
            if (!CanAct() || pendingTrickKind >= 0) return;
            pendingTrickKind = kind;
        }

        public void LandTrick()
        {
            if (pendingTrickKind < 0) return;
            int kind = pendingTrickKind;
            pendingTrickKind = -1;
            Trick(kind);
        }

        public void BreakTricks() { TrickCombo = 0; trickTime = 0f; lastTrick = -1; pendingTrickKind = -1; }

        void Update()
        {
            if (!CanAct()) return;
            var gm = GameManager.Instance;
            float dt = Time.deltaTime;
            DashTime = Mathf.Max(0f, DashTime - dt); DashCooldown = Mathf.Max(0f, DashCooldown - dt);
            ReviveGrace = Mathf.Max(0f, ReviveGrace - dt);
            Battery = Mathf.Min(1f, Battery + dt / 28f);
            Boost = Mathf.Max(0, Boost - dt); FloatTime = Mathf.Max(0, FloatTime - dt);
            Slide = Mathf.Max(0, Slide - dt); trickTime = Mathf.Max(0, trickTime - dt);
            if (trickTime <= 0f) { TrickCombo = 0; lastTrick = -1; }

            // The chase: the officer gains on a runner and drops back from a rider on the bike.
            if (PoliceActive && ReviveGrace <= 0f)
            {
                float before = PoliceGap;
                if (OnFoot) PoliceGap -= GainOnFoot * dt;
                else PoliceGap = Mathf.Min(MaxGap, PoliceGap + LoseOnBike * dt * (Boost > 0f || DashTime > 0f ? 3f : 1f));
                if (before > 6f && PoliceGap <= 6f) { MenuUI.Instance?.Shout("COP RIGHT BEHIND YOU!"); Haptics.Play(Haptics.Strength.Medium); }
                if (PoliceGap <= 0f) { PoliceGap = 0f; End("BUSTED!"); return; }
            }

            float exitDistance = gm.Distance + gm.Speed * SurfaceTraversal.TransitionSeconds;
            if (OnFoot && (parkPending || gm.Distance > BikeAt + 4f)) { parkPending = false; ParkBikeAhead(gm.Distance); }
            if (Surface != RunSurface.Road && Surface != RunSurface.Rooftop && !rider.Traversal.Moving)
            {
                if (!WallRunning || (wallTime -= dt) <= 0f || !WallAvailable(WallSide, exitDistance)) rider.AttachSurface(RunSurface.Road);
            }
            if (Keyboard.current?.eKey.wasPressedThisFrame == true) TryAbility();
            if (Keyboard.current?.qKey.wasPressedThisFrame == true) TryDeliveryDash();
        }
    }
}
