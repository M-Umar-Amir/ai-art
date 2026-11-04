using System.Collections.Generic;
using DallasDash.Core;
using DallasDash.UI;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Roadside fuel-and-food stops. Stations sit every 520 m from 300 m. A rider inside the pit
    /// window can stop the run, spend burgers and charge the tank; the delivery clock continues while
    /// the pit crew works. The geometry is built by this component rather than TrackSpawner so a
    /// station can never be mistaken for a hurdle and never blocks a lane.
    public class FuelStops : MonoBehaviour
    {
        public static FuelStops Instance { get; private set; }

        public const float FirstStation = 380f;
        public const float StationSpacing = 480f;
        /// How close the rider must be to the station centre to open the pit.
        public const float PitRadius = 12f;
        public const float StationLength = 14f;
        public const float BuildAhead = 170f;
        public const float CullBehind = 60f;

        public bool Pitting { get; private set; }
        public bool NearStation { get; private set; }
        public int StationIndex { get; private set; } = -1;
        public float DistanceToStation { get; private set; } = 999f;
        public int StationsVisited { get; private set; }
        public int EmergencySplashes { get; private set; }
        public bool EmergencyAvailable => StationIndex >= 0 && !emergencyStations.Contains(StationIndex);
        public float PitProgress => chargeDuration <= 0f ? 0f : Mathf.Clamp01(chargeTimer / chargeDuration);
        /// Fuel the current (or last) stop will add, for the HUD label.
        public float PitFuel { get; private set; }
        public int PitBurgers { get; private set; }

        TrackSpawner track;
        readonly Dictionary<int, GameObject> built = new();
        readonly List<int> stale = new();
        int lastDistrict = -1;
        float chargeTimer, chargeDuration, chargeGiven;
        readonly HashSet<int> emergencyStations = new();
        int lastStateFrame = -1;

        public static float StationAt(int index) => FirstStation + index * StationSpacing;

        /// Index of the station at or before this distance, or -1 before the first station.
        public static int IndexAt(float distance) =>
            distance < FirstStation ? -1 : Mathf.FloorToInt((distance - FirstStation) / StationSpacing);

        public static float NextStationDistance(float distance)
        {
            int i = IndexAt(distance);
            float next = StationAt(i + 1);
            return next;
        }

        /// True when the rider is inside the pit window of the station nearest to this distance.
        public static bool InPitWindow(float distance)
        {
            return WindowIndex(distance) >= 0;
        }

        /// Closest station index within the window, or -1.
        public static int WindowIndex(float distance)
        {
            int i = Mathf.Max(0, Mathf.RoundToInt((distance - FirstStation) / StationSpacing));
            return Mathf.Abs(distance - StationAt(i)) <= PitRadius ? i : -1;
        }

        /// A kiosk stands on the right-hand kerb around this distance (its pad, sign and canopy).
        public static bool KioskNear(float distance, float margin = 6f)
        {
            int i = Mathf.Max(0, Mathf.RoundToInt((distance - FirstStation) / StationSpacing));
            return Mathf.Abs(distance - StationAt(i)) <= StationLength * 0.5f + margin;
        }

        void Awake() => Instance = this;

        void Start()
        {
            track = FindFirstObjectByType<TrackSpawner>();
            if (GameManager.Instance)
            {
                GameManager.Instance.StateChanged += OnState;
                lastStateFrame = Time.frameCount;
            }
        }

        void OnDestroy()
        {
            if (GameManager.Instance) GameManager.Instance.StateChanged -= OnState;
            if (Instance == this) Instance = null;
        }

        void OnState(RunState state)
        {
            if (state == RunState.Running) { ResetRun(); return; }
            CancelPit(false);
            NearStation = false;
            StationIndex = -1;
        }

        public void ResetRun()
        {
            CancelPit(false);
            StationsVisited = 0;
            EmergencySplashes = 0;
            emergencyStations.Clear();
            NearStation = false;
            StationIndex = -1;
            DistanceToStation = 999f;
            PitFuel = 0f;
            PitBurgers = 0;
            if (DashRun.District != lastDistrict) Rebuild();
        }

        void Rebuild()
        {
            foreach (var pair in built) if (pair.Value) Destroy(pair.Value);
            built.Clear();
            lastDistrict = DashRun.District;
        }

        void Update()
        {
            if (!track) track = FindFirstObjectByType<TrackSpawner>();
            if (!track || !GameManager.Instance) return;
            if (DashRun.District != lastDistrict) Rebuild();

            var gm = GameManager.Instance;
            float distance = gm.Distance;

            // Build stations just ahead of the rider and drop the ones far behind. Stations are
            // static, so this is a handful of objects rather than a per-frame allocation.
            int ahead = IndexAt(distance + BuildAhead);
            int behind = Mathf.Max(0, Mathf.CeilToInt((distance - CullBehind - FirstStation) / StationSpacing));
            for (int i = behind; i <= ahead; i++)
                if (!built.ContainsKey(i)) built[i] = BuildStation(i);

            stale.Clear();
            foreach (var pair in built)
                if (StationAt(pair.Key) < distance - CullBehind) stale.Add(pair.Key);
            foreach (int i in stale)
            {
                if (built[i]) Destroy(built[i]);
                built.Remove(i);
            }

            if (Pitting)
            {
                TickPit();
                return;
            }

            bool running = gm.State == RunState.Running;
            int window = running ? WindowIndex(distance) : -1;
            StationIndex = window;
            NearStation = window >= 0;
            DistanceToStation = window >= 0 ? Mathf.Abs(distance - StationAt(window)) : 999f;

        }

        /// Starts a pit stop if the rider is in the window and the run is live. Returns false with a
        /// message when there is nothing to buy.
        public bool TryStartPit(out string message)
        {
            message = "";
            var gm = GameManager.Instance;
            if (gm == null || gm.State != RunState.Running || gm.Paused) { message = "Not while riding."; return false; }
            if (Pitting) { message = "Already refuelling."; return false; }
            int window = WindowIndex(gm.Distance);
            if (window < 0) { message = "Pull up to a fuel stop first."; return false; }

            float missing = 1f - RidersEconomy.Fuel;
            if (missing <= 0.01f) { message = "Tank is already full."; return false; }

            var plan = RidersEconomy.PitPlan(missing, RidersEconomy.Burgers);
            if (plan.cost <= 0)
            {
                // No burgers: one free emergency splash per station keeps a dry rider moving.
                if (emergencyStations.Contains(window)) { message = "No burgers to spend. Collect burgers on the road."; return false; }
                emergencyStations.Add(window);
                EmergencySplashes++;
                RidersEconomy.AddFuel(RidersEconomy.EmergencySplash);
                MenuUI.Instance?.Shout("EMERGENCY SPLASH");
                Haptics.Play(Haptics.Strength.Light);
                message = "Free emergency splash. Collect burgers for a full charge.";
                return true;
            }

            if (!RidersEconomy.TrySpendBurgers(plan.cost)) { message = "Not enough burgers."; return false; }
            PitBurgers = plan.cost;
            PitFuel = plan.added;
            chargeGiven = 0f;
            chargeTimer = 0f;
            chargeDuration = RidersEconomy.ChargeSeconds(plan.cost);
            Pitting = true;
            StationsVisited++;
            gm.SetPaused(true);
            MenuUI.Instance?.Shout($"PIT STOP  /  -{plan.cost} BURGERS");
            Haptics.Play(Haptics.Strength.Medium);
            message = $"Charging {plan.added * 100f:0}% fuel.";
            return true;
        }

        public void CancelPit(bool refund)
        {
            if (!Pitting) return;
            Pitting = false;
            if (GameManager.Instance && GameManager.Instance.Paused && GameManager.Instance.State == RunState.Running)
                GameManager.Instance.SetPaused(false);
            // Fuel already delivered remains purchased; only unspent whole burgers are refunded.
            if (refund && PitBurgers > 0)
                RidersEconomy.Burgers += Mathf.Max(0, PitBurgers - Mathf.CeilToInt(chargeGiven * RidersEconomy.BurgersPerTank));
            PitBurgers = 0;
            chargeTimer = chargeDuration = 0f;
        }

        void TickPit()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != RunState.Running) { CancelPit(true); return; }
            chargeTimer += Time.unscaledDeltaTime;
            float target = PitFuel * Mathf.Clamp01(chargeTimer / Mathf.Max(0.01f, chargeDuration));
            float step = Mathf.Max(0f, target - chargeGiven);
            if (step > 0f) { RidersEconomy.AddFuel(step); chargeGiven += step; }
            if (chargeTimer < chargeDuration) return;

            Pitting = false;
            chargeTimer = chargeDuration = 0f;
            gm.SetPaused(false);
            MenuUI.Instance?.Shout($"REFUELLED  /  {RidersEconomy.FuelPercent * 100f:0}%");
            Haptics.Play(Haptics.Strength.Medium);
            PitBurgers = 0;
        }

        // ---- Geometry

        static void PlaceKiosk(GameObject prefab, Transform parent, string name, Vector3 at, float height)
        {
            var wrapper = new GameObject(name).transform;
            wrapper.SetParent(parent, false); wrapper.position = at;
            var model = Instantiate(prefab, wrapper, false);
            model.SetActive(true);
            foreach (var node in model.GetComponentsInChildren<Transform>(true)) node.gameObject.SetActive(true);
            foreach (var collider in model.GetComponentsInChildren<Collider>(true)) Destroy(collider);
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new System.InvalidOperationException(name + " has no visible mesh.");
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) { renderer.enabled = true; bounds.Encapsulate(renderer.bounds); }
            model.transform.localScale *= Mathf.Min(height / Mathf.Max(.01f, bounds.size.y), 3.8f / Mathf.Max(.01f, bounds.size.x));
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            model.transform.position += at - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        GameObject BuildStation(int index)
        {
            float z = StationAt(index);
            var root = new GameObject("Fuel stop " + (index + 1));
            root.transform.SetParent(transform, false);
            var t = root.transform;
            bool roof = DashRun.District == 1;

            var catalogue = Resources.Load<RidersCityAssets>("RidersCityAssets");
            if (!catalogue || !catalogue.districtCharger || !catalogue.districtFood)
                throw new System.InvalidOperationException("District food and battery kiosk models are missing.");
            float x = roof ? 5.5f : 5.8f;
            var pad = DashCity.Mat(track, "pitPad", new Color(.055f, .075f, .12f));
            var trim = DashCity.Mat(track, "pitTrim", new Color(1f, .62f, .1f), .9f);
            // A supported terrace keeps both kiosks outside all three playable lanes.
            DashCity.Part(t, new Vector3(x, -.16f, z), new Vector3(4.4f, .32f, 12f), pad);
            if (roof)
                DashCity.Part(t, new Vector3(x, -20f, z), new Vector3(4.4f, 40f, 12f), pad);
            PlaceKiosk(catalogue.districtCharger, t, "Battery charging kiosk", new Vector3(x, 0, z - 2.8f), 2.5f);
            PlaceKiosk(catalogue.districtFood, t, "Food kiosk", new Vector3(x, 0, z + 1f), 2.8f);
            DashCity.Text(track, t, "BURGER BAR\nBATTERY CHARGING", new Vector3(x, 3.4f, z), .04f, Color.white);
            DashCity.Part(t, new Vector3(3.8f, .045f, z), new Vector3(.16f, .03f, 10f), trim);
            return root;
        }
    }
}

