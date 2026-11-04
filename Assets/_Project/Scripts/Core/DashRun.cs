using DallasDash.Gameplay;
using DallasDash.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DallasDash.Core
{
    // Delivery progress, signature moves and character abilities belong to the current run.
    public class DashRun : MonoBehaviour
    {
        public static DashRun Instance { get; private set; }
        public static readonly string[] CrewNames = { "ACE", "NOVA", "BOLT" };
        public static readonly string[] AbilityNames = { "SPEED BOOST", "AIR TIME", "SHIELD DRONE" };
        public static readonly string[] DistrictNames = { "NEON DOWNTOWN", "SKYPARK DISTRICT", "FESTIVALS" };
        /// The selected district; locked districts are bought with burgers (BurgerPrices.District).
        public static int District
        {
            get { int d = Mathf.Clamp(PlayerPrefs.GetInt("dash_district", 0), 0, 2); return BurgerPrices.DistrictOpen(d) ? d : 0; }
            set { if (value >= 0 && value < 3 && BurgerPrices.DistrictOpen(value)) { PlayerPrefs.SetInt("dash_district", value); PlayerPrefs.Save(); } }
        }
        public static bool BuyDistrict(int d) => d <= 0 || d < 3 && PlayerProfile.Buy(BurgerPrices.DistrictItem(d), BurgerPrices.District[d]);
        public float Battery { get; private set; } = 1f;
        public float Boost { get; private set; }
        public float FloatTime { get; private set; }
        public bool Shield { get; private set; }
        public bool Board { get; private set; }
        /// Knocked off the e-bike by a crash: on foot until the rider reaches the parked bike again.
        /// Client rules (3 Nov): Ace and Nova wall-run ONLY on foot and take the rooftops ONLY on the bike;
        /// Bolt's magnetic monowheel does both.
        public bool OnFoot { get; private set; }
        /// Where the parked e-bike waits (distance and lane) while the rider is on foot.
        public float BikeAt { get; private set; }
        public int BikeLane { get; private set; } = 1;
        public static bool CanRideWalls => PlayerProfile.Character == 2 || Instance && Instance.OnFoot;
        public static bool CanTakeRooftops => PlayerProfile.Character == 2 || !(Instance && Instance.OnFoot);
        bool warnedWall, warnedRoof, parkPending;
        /// -1 on the left facade, +1 on the right, 0 off the walls.
        public int WallSide => Surface == RunSurface.LeftWall ? -1 : Surface == RunSurface.RightWall ? 1 : 0;
        public RunSurface Surface => rider ? rider.Traversal.Surface : RunSurface.Road;
        public bool Flipped => Surface == RunSurface.Ceiling;
        public bool WallRunning => Surface == RunSurface.LeftWall || Surface == RunSurface.RightWall;
        public float DashTime { get; private set; }
        public float DashCooldown { get; private set; }
        public float Slide { get; private set; }
        public int Deliveries { get; private set; }
        bool warned;
        /// Shortcuts the player actually took this run.
        public int Shortcuts { get; private set; }
        /// Seconds of the shortcut surge still running.
        public float ShortcutTime { get; private set; }
        public int TrickCombo { get; private set; }
        /// Endless run (client, 2 Nov): deliveries never end the run. This is the on-time window for the
        /// current drop: arrive before it reaches zero for the full bonus; late drops still count.
        public float DeliveryTime { get; private set; } = 28f;
        public int OnTimeDeliveries { get; private set; }
        /// Why the last run ended, shown on the results card.
        public static string EndReason { get; private set; } = "";
        /// Seconds the rider has been coasting on a dry tank; the run ends at DryLimit.
        public float DrySeconds { get; private set; }
        public const float DryLimit = 6f;
        /// Bolt has no bike to lose: a second head-on hit inside this window wipes him out.
        float stumbleWindow;
        public float DeliveryFraction => Mathf.Clamp01((GameManager.Instance.Distance - Deliveries * 450f) / 450f);
        /// Wall runs are no longer zoned (client, 3 Nov): any street facade can be run, see WallAvailable.
        /// Kept as a predicate (always false) so the older zone call sites read naturally.
        public static bool WallZone(float d) => false;

        /// A building face lines this side of the street at z: not across a junction's cross road, not over
        /// the rooftop course's own block row (its alley gaps), and not along a kiosk on the right-hand kerb.
        public static bool WallAvailable(int side, float z)
        {
            if (side == 0 || z < 20f || TrackSpawner.NearJunction(z)) return false;
            if (side > 0 && (RooftopCourse.OwnsRightSide(z) || FuelStops.KioskNear(z))) return false;
            return true;
        }
        /// Seconds a wall run lasts before the rider drops back to the street.
        public float WallRunSeconds => PowerUps.Active(PowerUp.Boots) ? 4f : 2.6f;
        public static int RoofGapLane(float z) => District == 1 && z >= 160f && Mathf.FloorToInt(z / 20f) % 4 == 0 ? Mathf.FloorToInt(z / 80f) % 3 : -1;
        /// The gravity flip (floor > wall > ceiling > wall) was removed at the client's request: real-world
        /// physics only. Kept as a predicate so older call sites read naturally.
        public static bool GravityZone(float d) => false;
        public static float GravityZoneStart(float d) => d - Mathf.Repeat(d - 700f, 960f);

        /// The rooftop course keeps a speed surge alive for its whole length: running the roofs pays for the
        /// climb even against a perfect street line.
        public const float RouteSpeedFactor = 1.35f;

        /// True while the rider is up on the roofs; counts the shortcut once per climb and keeps the surge alive.
        float lastRouteStart = -1f;
        public bool OnRoute { get; private set; }
        public bool OnRooftops => Surface == RunSurface.Rooftop;
        public float SurfaceHeight => rider ? rider.Traversal.Origin.y : 0f;
        public float JumpGravityFactor => FloatTime > 0f || PowerUps.Active(PowerUp.Boots) ? 0.68f : 1f;
        float boardTime, trickTime, wallTime;
        int lastTrick = -1;
        int pendingTrickKind = -1;
        RiderController rider;
        void Awake() => Instance = this;
        void Start()
        {
            rider = FindFirstObjectByType<RiderController>();
            GameManager.Instance.StateChanged += State;
        }
        void OnDestroy() { if (GameManager.Instance) GameManager.Instance.StateChanged -= State; }
        void State(RunState s)
        {
            if (s == RunState.Running)
            {
                Battery = 1f; Boost = FloatTime = Slide = boardTime = wallTime = trickTime = 0f;
                Shield = Board = false;
                OnFoot = warnedWall = warnedRoof = false; BikeAt = 0f;
                FindFirstObjectByType<RiderLook>()?.Apply(PlayerProfile.Character, true);
                DashTime = DashCooldown = 0f;
                Deliveries = TrickCombo = 0; lastTrick = -1; pendingTrickKind = -1;
                Shortcuts = 0; WallRuns = 0; ShortcutTime = 0f; OnRoute = false; lastRouteStart = -1f;
                DeliveryTime = 28f;
                OnTimeDeliveries = 0; DrySeconds = 0f; stumbleWindow = 0f; EndReason = "";
                warned = false;
            }
            if (s != RunState.Running) { rider?.Traversal.Reset(); Slide = 0f; }
        }
        public bool TryDeliveryDash()
        {
            if (!CanAct() || DashCooldown > 0f) return false;
            DashTime = 2.2f; DashCooldown = 12f;
            MenuUI.Instance?.Shout("DELIVERY DASH");
            Haptics.Play(Haptics.Strength.Medium);
            return true;
        }
        /// The player rode onto the side route: counted once per entry, and the surge is refreshed
        /// every frame the rider stays on it.
        public void RideRoute(bool on)
        {
            float routeStart = RooftopCourse.ZoneStartAt(GameManager.Instance.Distance);
            if (on && routeStart != lastRouteStart && CanAct())
            {
                lastRouteStart = routeStart;
                Shortcuts++;
                ShortcutTime = 3f;
                GameManager.Instance.AddBonus(150);
                MenuUI.Instance?.Shout(Shortcuts > 1 ? "ROOFTOP SHORTCUT! x" + Shortcuts : "ROOFTOP SHORTCUT!");
                Haptics.Play(Haptics.Strength.Medium);
            }
            OnRoute = on;
            if (on) ShortcutTime = Mathf.Max(ShortcutTime, 0.4f);
        }
        public void TryAbility()
        {
            if (!CanAct() || Battery < 0.999f) return;
            Battery = 0f;
            switch (PlayerProfile.Character)
            {
                case 0: Boost = 4.5f; break;
                case 1: FloatTime = 7f; break;
                default: Shield = true; break;
            }
            MenuUI.Instance?.Shout(AbilityNames[Mathf.Clamp(PlayerProfile.Character, 0, 2)]);
            Haptics.Play(Haptics.Strength.Medium);
        }
        public bool AbsorbHit()
        {
            if (Boost > 0f) return true;
            if (Shield || PowerUps.Active(PowerUp.Shield))
            {
                Shield = false; PowerUps.Consume(PowerUp.Shield); return true;
            }
            if (!Board) return false;
            Board = false; boardTime = 0f;
            FindFirstObjectByType<RiderLook>()?.Apply(PlayerProfile.Character, true);
            return true;
        }
        public void Recharge() => Battery = Mathf.Min(1f, Battery + 0.4f);
        public void PenalizeTime(float seconds)
        {
            if (CanAct()) DeliveryTime = Mathf.Max(0f, DeliveryTime - Mathf.Max(0f, seconds));
        }
        bool CanAct() => GameManager.Instance && GameManager.Instance.State == RunState.Running && !GameManager.Instance.Paused;
        public bool JumpMove()
        {
            Slide = 0f;
            if (!CanAct() || !rider) return false;
            // On the roofs a jump is just a jump (over the alley gaps): wall and ceiling moves belong to the street.
            if (OnRooftops) return false;
            if (rider.Traversal.Moving) return true;
            if (WallRunning)
            {
                rider.AttachSurface(RunSurface.Road);
                return true; // up on a wall kicks off it, back down into the street
            }
            return false;
        }

        /// Steering into the building beside an outer lane starts a wall run along its facade, anywhere on the
        /// street where a facade stands. Returns false when the move is not allowed, so the steer is ignored.
        public bool TryWallRun(int side)
        {
            if (!CanAct() || !rider || rider.Traversal.Moving || Surface != RunSurface.Road) return false;
            if (!CanRideWalls)
            {
                if (!warnedWall) { warnedWall = true; MenuUI.Instance?.Shout("WALL RUN: ON FOOT ONLY"); }
                return false;
            }
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
        /// Wall runs started this run (results card, bot checks).
        public int WallRuns { get; private set; }

        /// Steering away from the wall ends the wall run.
        public void LeaveWall() { if (WallRunning && rider && !rider.Traversal.Moving) rider.AttachSurface(RunSurface.Road); }
        public void StartSlide() { if (CanAct()) Slide = 0.85f; }

        /// Endless-runner fail rule: the first head-on crash knocks Ace or Nova off the e-bike (Bolt
        /// staggers); crashing again before getting back on (or, for Bolt, within 6 s) ends the run.
        public static string LastStruck = "";
        public void Crash()
        {
            if (!CanAct() || !rider) return;
            // A wall run is already an on-foot move: a facade hazard only bumps the rider off the wall.
            if (Surface != RunSurface.Road && Surface != RunSurface.Rooftop) return;
            bool secondHit = PlayerProfile.Character == 2 ? stumbleWindow > 0f : OnFoot;
            Debug.Log($"[DashCrash] at {GameManager.Instance.Distance:0.0} lane={rider.Lane} surface={Surface} onFoot={OnFoot} second={secondHit} object={LastStruck}");
            if (secondHit) { End("WIPED OUT"); return; }
            if (PlayerProfile.Character == 2)
            {
                stumbleWindow = 6f;
                MenuUI.Instance?.Shout("STAGGERED! ONE MORE HIT ENDS THE RUN");
                Haptics.Play(Haptics.Strength.Medium);
                return;
            }
            KnockOffBike();
        }

        public void End(string reason)
        {
            if (!GameManager.Instance || GameManager.Instance.State != RunState.Running) return;
            EndReason = reason;
            if (FuelStops.Instance && FuelStops.Instance.Pitting) FuelStops.Instance.CancelPit(true);
            MenuUI.Instance?.Shout(reason);
            GameManager.Instance.Bust();
        }

        /// A crash throws Ace or Nova off the e-bike: the bike is left lying in the road and they run on.
        public void KnockOffBike()
        {
            if (!CanAct() || OnFoot || PlayerProfile.Character == 2 || !rider) return;
            if (Surface != RunSurface.Road || rider.Traversal.Moving) return;
            OnFoot = true;
            MenuUI.Instance?.Shout("KNOCKED OFF! ON FOOT: RUN THE WALLS");
            parkPending = true; // parked next frame: this runs inside the track's hit-test loop
            var look = FindFirstObjectByType<RiderLook>();
            look?.DropBike(PlayerProfile.Character, rider.transform.position);
            look?.Apply(PlayerProfile.Character, false);
            Haptics.Play(Haptics.Strength.Medium);
        }

        /// Puts the e-bike in a lane a few seconds ahead, never inside the facade run, the rooftop line or a
        /// junction, so there is always a fair chance to grab it.
        void ParkBikeAhead(float d)
        {
            float z = d + Mathf.Max(70f, GameManager.Instance.Speed * 4f);
            for (int guard = 0; guard < 8; guard++)
            {
                if (RooftopCourse.Near(z, 40f, RooftopCourse.StreetLanding - RooftopCourse.Length + 15f)) { z = RooftopCourse.ZoneStartAt(z) + RooftopCourse.StreetLanding + 25f; continue; }
                if (TrackSpawner.NearJunction(z) || TrackSpawner.NearJunction(z + 8f)) { z += 30f; continue; }
                // Skypark: never over (or just before) a gap between towers, where the rider is mid-jump.
                if (SkyparkGapNear(z)) { z += 12f; continue; }
                break;
            }
            // Free space only: walk forward until a lane is clear for 12 m either side of the spot.
            var track = FindFirstObjectByType<TrackSpawner>();
            int lane = -1;
            for (int tries = 0; tries < 24 && track; tries++, z += 6f)
            {
                if (RooftopCourse.Near(z, 20f, 20f) || TrackSpawner.NearJunction(z) || SkyparkGapNear(z)) continue;
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

        static bool SkyparkGapNear(float z)
        {
            for (float dz = -10f; dz <= 6f; dz += 2f) if (TrackSpawner.SkyparkVoid(z + dz)) return true;
            return false;
        }

        public void Remount()
        {
            if (!OnFoot) return;
            OnFoot = false; warnedWall = false; warnedRoof = false;
            FindFirstObjectByType<RiderLook>()?.Apply(PlayerProfile.Character, true);
            MenuUI.Instance?.Shout("BACK ON THE E-BIKE");
        }

        /// The rooftop shortcut: ride the kicker ramp in the right-hand lane and the rider climbs the facade
        /// onto the roofs; at the end of the block row they drop back into the street. Missing an alley gap
        /// drops them to the street too, at a time cost, instead of ending the run.
        void Rooftops(float d)
        {
            if (!rider || rider.Traversal.Moving) return;
            if (Surface == RunSurface.Road)
            {
                if (rider.Lane == RooftopCourse.EntryLane && RooftopCourse.EntryWindow(d) && rider.JumpHeight < 1.6f)
                {
                    // The kicker is a bike ramp: on foot, Ace and Nova run over it and stay in the street.
                    if (!CanTakeRooftops)
                    {
                        if (!warnedRoof) { warnedRoof = true; MenuUI.Instance?.Shout("ROOFTOPS: BIKE ONLY"); }
                        return;
                    }
                    // A parkour line over real things: truck roof, fire-escape landing, parapet.
                    rider.AscendToRooftops();
                    Trick(7);
                    Haptics.Play(Haptics.Strength.Medium);
                }
                return;
            }
            if (Surface != RunSurface.Rooftop) return;
            if (RooftopCourse.PastDrop(d))
            {
                rider.DropToStreet(false);
                RideRoute(false);
                MenuUI.Instance?.Shout("ROOF DROP!");
                Trick(8);
                return;
            }
            if (rider.Fallen)
            {
                rider.DropToStreet(true);
                RideRoute(false);
                PenalizeTime(2f);
                BreakTricks();
                MenuUI.Instance?.Shout("MISSED THE ROOF / -2s");
                return;
            }
            RideRoute(true);
        }
        public void Trick(int kind)
        {
            if (!CanAct() || lastTrick == kind && trickTime > 0f) return;
            TrickCombo = trickTime > 0f ? TrickCombo + 1 : 1;
            lastTrick = kind; trickTime = 3f;
            GameManager.Instance.AddBonus(15 * Mathf.Min(TrickCombo, 8));
            if (TrickCombo >= 3) MenuUI.Instance?.Shout("TRICK CHAIN x" + TrickCombo);
        }

        /// An airborne trick is only banked on landing; a hit or a missed landing loses it.
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
            // Pit stops freeze movement, never the delivery deadline. User pauses still pause it.
            if (GameManager.Instance && GameManager.Instance.State == RunState.Running && FuelStops.Instance && FuelStops.Instance.Pitting)
            {
                DeliveryTime = Mathf.Max(0f, DeliveryTime - Time.unscaledDeltaTime);
                DrySeconds = 0f;
                return;
            }
            if (!CanAct()) return;
            float dt = Time.deltaTime;
            DashTime = Mathf.Max(0f, DashTime - dt); DashCooldown = Mathf.Max(0f, DashCooldown - dt);
            ShortcutTime = Mathf.Max(0f, ShortcutTime - dt);
            DeliveryTime = Mathf.Max(0f, DeliveryTime - dt);
            stumbleWindow = Mathf.Max(0f, stumbleWindow - dt);
            // Out of charge: the rider coasts at reduced pace; reach a kiosk (Food, or the free emergency
            // splash) inside DryLimit seconds or the run is over.
            if (RidersEconomy.Empty)
            {
                float before = DrySeconds;
                DrySeconds += dt;
                if (before <= 0f) { MenuUI.Instance?.Shout("OUT OF CHARGE! REACH A KIOSK"); Haptics.Play(Haptics.Strength.Medium); }
                if (DrySeconds >= DryLimit) { End("OUT OF CHARGE"); return; }
            }
            else DrySeconds = 0f;
            Battery = Mathf.Min(1f, Battery + dt / 28f);
            Boost = Mathf.Max(0, Boost - dt); FloatTime = Mathf.Max(0, FloatTime - dt);
            Slide = Mathf.Max(0, Slide - dt); trickTime = Mathf.Max(0, trickTime - dt);
            if (trickTime <= 0f) { TrickCombo = 0; lastTrick = -1; }
            if (Board && (boardTime -= dt) <= 0f)
            { Board = false; FindFirstObjectByType<RiderLook>()?.Apply(PlayerProfile.Character, true); }
            float exitDistance = GameManager.Instance.Distance + GameManager.Instance.Speed * SurfaceTraversal.TransitionSeconds;
            Rooftops(GameManager.Instance.Distance);
            // Parked after the crash, and again further on if the rider runs past it.
            if (OnFoot && (parkPending || GameManager.Instance.Distance > BikeAt + 4f)) { parkPending = false; ParkBikeAhead(GameManager.Instance.Distance); }
            // A wall run ends on its timer, or before the facade does (a cross street, the rooftop block row, a kiosk).
            if (Surface != RunSurface.Road && Surface != RunSurface.Rooftop && !rider.Traversal.Moving)
            {
                if (!WallRunning || (wallTime -= dt) <= 0f || !WallAvailable(WallSide, exitDistance)) rider.AttachSurface(RunSurface.Road);
            }
            if (Keyboard.current?.eKey.wasPressedThisFrame == true) TryAbility();
            if (Keyboard.current?.qKey.wasPressedThisFrame == true) TryDeliveryDash();
            if (GameManager.Instance.Distance >= (Deliveries + 1) * 450f)
            {
                bool onTime = DeliveryTime > 0f;
                Deliveries++;
                warned = false;
                int tip = DashInventory.Delivery(onTime);
                GameManager.Instance.TipsEarned += tip;
                DeliveryTime = Mathf.Max(16f, 28f - Deliveries * 0.7f);
                DashCooldown = 0f;
                if (onTime) OnTimeDeliveries++;
                GameManager.Instance.AddBonus(onTime ? 250 : 60);
                MenuUI.Instance?.Shout(onTime ? $"ON TIME!  +{tip} BURGERS" : $"DELIVERED LATE  /  +{tip} BURGERS");
            }
            // Warning at 10 seconds remaining (quality of life)
            else if (DeliveryTime <= 6f && DeliveryTime > 0f && !warned)
            {
                warned = true;
                MenuUI.Instance?.Shout("ON-TIME BONUS ENDING!");
                Haptics.Play(Haptics.Strength.Medium);
            }
        }
    }
}
