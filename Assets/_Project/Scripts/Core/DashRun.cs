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
        public static readonly int[] DistrictPoints = { 0, 10000, 25000 };
        public static int District
        {
            get { int d = Mathf.Clamp(PlayerPrefs.GetInt("dash_district", 0), 0, 2); return PlayerProfile.TotalPoints >= DistrictPoints[d] ? d : 0; }
            set { if (value >= 0 && value < 3 && PlayerProfile.TotalPoints >= DistrictPoints[value]) { PlayerPrefs.SetInt("dash_district", value); PlayerPrefs.Save(); } }
        }
        public float Battery { get; private set; } = 1f;
        public float Boost { get; private set; }
        public float FloatTime { get; private set; }
        public bool Shield { get; private set; }
        public bool Board { get; private set; }
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
        public float DeliveryTime { get; private set; } = 28f;
        public float DeliveryFraction => Mathf.Clamp01((GameManager.Instance.Distance - Deliveries * 450f) / 450f);
        public static bool WallZone(float d) => d >= 300f && Mathf.Repeat(d - 300f, 960f) < 90f;
        public static int RoofGapLane(float z) => District == 1 && z >= 160f && Mathf.FloorToInt(z / 20f) % 4 == 0 ? Mathf.FloorToInt(z / 80f) % 3 : -1;
        public static bool GravityZone(float d) => d >= 600f && Mathf.Repeat(d - 600f, 960f) < 90f;

        // ---- The side route: a real fourth lane on the right-hand side of the street.
        // In a shortcut zone it is a raised express deck that runs the whole zone at a higher speed;
        // in Skypark's parkour zones it is the rooftops of the low-rise blocks, with gaps to hop.
        // Either way it is a genuine bypass: the road below keeps its hazards, the route does not.
        public const float ShortcutStart = 200f, ShortcutEvery = 960f, ShortcutLength = 90f;
        public static bool ShortcutZone(float d) => d >= ShortcutStart && Mathf.Repeat(d - ShortcutStart, ShortcutEvery) < ShortcutLength;
        public static float ShortcutZoneStart(float d) => d - Mathf.Repeat(d - ShortcutStart, ShortcutEvery);

        public const float ParkourStart = 420f, ParkourEvery = 960f, ParkourLength = 90f;
        public static bool ParkourZone(float d) => District == 1 && d >= ParkourStart && Mathf.Repeat(d - ParkourStart, ParkourEvery) < ParkourLength;
        public static float ParkourZoneStart(float d) => d - Mathf.Repeat(d - ParkourStart, ParkourEvery);

        /// The route geometry exists here (express deck or rooftops).
        public static bool RouteZone(float d) => ShortcutZone(d) || ParkourZone(d);
        /// The route can be entered here: the entry ramp starts a few metres ahead, so the swipe onto it
        /// lands on the ramp instead of mid-air.
        public static bool RouteOpen(float d) => RouteZone(d) || RouteZone(d + EntryLookAhead);
        public const float EntryLookAhead = 7f;
        /// The first metres of every route are a flat lead-in at kerb height, so a rider can join it
        /// anywhere in that window without clipping through the raised deck.
        public const float RouteLeadIn = 12f;
        /// Swiping right puts the rider on the route only while it is open and still in its lead-in.
        public static bool RouteEntry(float d) =>
            RouteOpen(d) && (!RouteZone(d) || d - RouteZoneStart(d) <= 5f);
        /// 1 = express deck, 2 = rooftop parkour, 0 = no route.
        public static int RouteKind(float d) => ParkourZone(d) ? 2 : ShortcutZone(d) ? 1 : 0;
        public static float RouteZoneStart(float d) => ParkourZone(d) ? ParkourZoneStart(d) : ShortcutZoneStart(d);
        public static float RouteZoneLength(float d) => ParkourZone(d) ? ParkourLength : ShortcutLength;
        /// Deck height of the express route, and the roof height of the parkour blocks.
        public const float ExpressHeight = 1.15f;
        public const float ParkourRoofHeight = 3.4f;
        /// The low kerb-height lead-in at the start and end of every route.
        public const float RouteLeadHeight = 0.25f;
        public static float RouteHeight(float d) => ParkourZone(d) ? ParkourRoofHeight : ExpressHeight;
        /// The route keeps a speed surge alive for its whole length. 35% over the base pace is enough
        /// to make a clean 90 m bypass pay for itself even against a perfect road line.
        public const float RouteSpeedFactor = 1.35f;

        /// True while the rider is on the side route; counts the shortcut once per entry and keeps the
        /// speed surge alive for as long as the route lasts.
        float lastRouteStart = -1f;
        public bool OnRoute { get; private set; }
        public float SurfaceHeight => rider ? rider.Traversal.Origin.y : 0f;
        public float JumpGravityFactor => FloatTime > 0f || PowerUps.Active(PowerUp.Boots) ? 0.68f : 1f;
        float boardTime, trickTime, wallTime;
        int lastTrick = -1;
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
                DashTime = DashCooldown = 0f;
                Deliveries = TrickCombo = 0; lastTrick = -1;
                Shortcuts = 0; ShortcutTime = 0f; OnRoute = false; lastRouteStart = -1f;
                DeliveryTime = 28f;
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
            float routeStart = RouteZoneStart(GameManager.Instance.Distance);
            if (on && routeStart != lastRouteStart && CanAct())
            {
                lastRouteStart = routeStart;
                Shortcuts++;
                ShortcutTime = 3f;
                GameManager.Instance.AddBonus(150);
                MenuUI.Instance?.Shout(Shortcuts > 1 ? "SHORTCUT! x" + Shortcuts : "SHORTCUT!");
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
        public void ToggleBoard()
        {
            if (!CanAct() || Board || !Missions.ScooterEarned || PlayerProfile.Coins < 50) return;
            PlayerProfile.Coins -= 50; Board = true; boardTime = 25f;
            FindFirstObjectByType<RiderLook>()?.Apply(PlayerProfile.Character, true);
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
            // On the side route a jump is just a jump: the wall/ceiling teaching moves belong to the road.
            if (OnRoute) return false;
            if (rider.Traversal.Moving) return true;
            if (GravityZone(GameManager.Instance.Distance) && !rider.Airborne)
            {
                rider.AttachSurface((RunSurface)(((int)Surface + 1) % 4));
                MenuUI.Instance?.Shout("GRAVITY / " + Surface.ToString().ToUpperInvariant());
                return true;
            }
            if (WallRunning)
            {
                rider.AttachSurface(RunSurface.Road);
                return false; // a second up input kicks back into a road jump
            }
            if (WallZone(GameManager.Instance.Distance) && rider.Lane != 1 && !rider.Airborne)
            {
                rider.AttachSurface(rider.Lane == 0 ? RunSurface.LeftWall : RunSurface.RightWall);
                wallTime = PowerUps.Active(PowerUp.Boots) ? 4f : 2.6f;
                MenuUI.Instance?.Shout("WALL RUN");
                return true;
            }
            return false;
        }
        public void StartSlide() { if (CanAct()) Slide = 0.85f; }
        public void Trick(int kind)
        {
            if (!CanAct() || lastTrick == kind && trickTime > 0f) return;
            TrickCombo = trickTime > 0f ? TrickCombo + 1 : 1;
            lastTrick = kind; trickTime = 3f;
            GameManager.Instance.AddBonus(15 * Mathf.Min(TrickCombo, 8));
            if (TrickCombo >= 3) MenuUI.Instance?.Shout("TRICK CHAIN x" + TrickCombo);
        }
        public void BreakTricks() { TrickCombo = 0; trickTime = 0f; lastTrick = -1; }
        void Update()
        {
            if (!CanAct()) return;
            float dt = Time.deltaTime;
            DashTime = Mathf.Max(0f, DashTime - dt); DashCooldown = Mathf.Max(0f, DashCooldown - dt);
            ShortcutTime = Mathf.Max(0f, ShortcutTime - dt);
            DeliveryTime = Mathf.Max(0f, DeliveryTime - dt);
            Battery = Mathf.Min(1f, Battery + dt / 28f);
            Boost = Mathf.Max(0, Boost - dt); FloatTime = Mathf.Max(0, FloatTime - dt);
            Slide = Mathf.Max(0, Slide - dt); trickTime = Mathf.Max(0, trickTime - dt);
            if (trickTime <= 0f) { TrickCombo = 0; lastTrick = -1; }
            if (Board && (boardTime -= dt) <= 0f)
            { Board = false; FindFirstObjectByType<RiderLook>()?.Apply(PlayerProfile.Character, true); }
            float exitDistance = GameManager.Instance.Distance + GameManager.Instance.Speed * SurfaceTraversal.TransitionSeconds;
            if (!GravityZone(GameManager.Instance.Distance) && Surface != RunSurface.Road)
            {
                if (!WallRunning || (wallTime -= dt) <= 0f || !WallZone(exitDistance)) rider.AttachSurface(RunSurface.Road);
            }
            if (GravityZone(GameManager.Instance.Distance) && !GravityZone(exitDistance)) rider.AttachSurface(RunSurface.Road);
            if (Keyboard.current?.eKey.wasPressedThisFrame == true) TryAbility();
            if (Keyboard.current?.bKey.wasPressedThisFrame == true) ToggleBoard();
            if (Keyboard.current?.qKey.wasPressedThisFrame == true) TryDeliveryDash();
            if (GameManager.Instance.Distance >= (Deliveries + 1) * 450f)
            {
                Deliveries++; DemoRewards.EarnBadge();
                warned = false;
                DashInventory.Delivery();
                DeliveryTime = Mathf.Max(16f, 28f - Deliveries * 0.7f);
                DashCooldown = 0f;
                GameManager.Instance.AddBonus(250);
                MenuUI.Instance?.Shout("DELIVERED! +1 BADGE");
            }
            else if (DeliveryTime <= 0f)
            {
                MenuUI.Instance?.Shout("DELIVERY WINDOW CLOSED");
                GameManager.Instance.Bust();
            }
            // Warning at 10 seconds remaining (quality of life)
            else if (DeliveryTime <= 10f && !warned)
            {
                warned = true;
                MenuUI.Instance?.Shout("TIME RUNNING LOW!");
                Haptics.Play(Haptics.Strength.Medium);
            }
        }
    }
}
