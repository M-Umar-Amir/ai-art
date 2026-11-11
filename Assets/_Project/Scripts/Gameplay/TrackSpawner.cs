using System;
using System.Collections.Generic;
using DallasDash.Core;
using DallasDash.Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DallasDash.Gameplay
{
    /// Streams a straight downtown street ahead of the rider and fills it with rows of everyday hurdles
    /// (roadworks, stopped vans, other couriers, cones and barriers) and burgers.
    public partial class TrackSpawner : MonoBehaviour
    {
        public RiderController rider;
        public ChaseController chase;
        public PickupDef[] pickups;
        public GameObject[] powerUpModels;
        public GameObject routeHurdleModel;
        public GameObject movingDeliveryPlatform;
        public GameObject roofVentModel, roofTankModel, roofChimneyModel, festivalStageModel;
        public Texture2D asphaltTexture, pavingTexture, asphaltNormal, pavingNormal, masonryTexture;
        public Texture2D asphaltRoughness, pavingRoughness, masonryNormal, masonryRoughness;

        public GameObject PlaceAsset(GameObject prefab, Transform parent, Vector3 at, float size, float yaw = 0f)
        {
            if (!prefab) throw new InvalidOperationException("Required environment model is missing");
            var model = Instantiate(prefab, at, Quaternion.Euler(0, yaw, 0), parent);
            model.transform.localScale = Vector3.one * size;
            return model;
        }
        public ObstacleDef[] obstacles;
        public LandmarkDef[] landmarks;

        public float segmentLength = 20f;
        public float rowSpacing = 14f;
        public float firstRowAt = 60f; // 11 Nov 2026 QoL: a clear 60 m to settle in before the first hurdle row
        public Material roadMaterial, sidewalkMaterial, landmarkMaterial;
        /// Storefront dressing: brick and painted facades; bulbMaterial lights the fallback lamp heads.
        public Material[] facadeMaterials;
        public Material bulbMaterial;
        /// Shared URP/Lit base for placeholder obstacles and pickups, tinted per object.
        /// Runtime primitives otherwise get the built-in default material, which is stripped from URP builds (renders magenta).
        public Material tintMaterial;
        public Material laneLineMaterial, curbMaterial, trimMaterial;
        public Material[] windowMaterials, awningMaterials;
        public Material lampPostMaterial, foliageMaterial, haloMaterial;
        public Material[] skylineMaterials;
        public Material doorMaterial, plazaMaterial;
        public Font signFont;
        /// FoodInc/TextCurved: lettering that bends with the street and sits on its board.
        public Material signTextMaterial;
        public GameObject[] dashBuildings;
        public GameObject dashBridge;
        public GameObject rooftopBuilding;
        /// Authored street dressing, instantiated by DashCity: a lit lamp and a planted terrace box.
        public GameObject dashLamp, dashPlanter;
        public Texture2D[] hologramTextures;
        public Material hologramMaterial;
        [Header("Modular facades (Quaternius Downtown City MegaKit, CC0)")]
        /// 2 m shop-window modules for the ground floor.
        public GameObject[] kitGround;
        public GameObject kitDoorFrame;
        /// 2 m upper-floor window modules per facade style: red brick, painted plaster, metal and glass.
        public GameObject[] kitBrickWindows, kitTrimWindows, kitMetalWindows;
        public GameObject kitBrickPlain, kitTrimPlain, kitMetalPlain;
        public GameObject kitBrickCornice, kitTrimCornice, kitMetalCornice;
        /// The kit's cream render material and its repainted variants (KitImport.Paints).
        public Material kitTrim;
        public Material[] kitPaints;
        /// The kit's glass, and the warm lit-shop glass the ground floor swaps it for.
        public Material kitGlass, kitShopGlass;
        /// The kit's dark metal frame material; shop fronts repaint it in their awning colour.
        public Material kitFrame;
        /// Body colour behind each style, for the building sides and roof.
        public Material kitBrickBody, kitMetalBody;

        [Header("Street props (CC0: Quaternius trees, Poly Haven furniture)")]
        public GameObject[] kitTrees;
        public GameObject kitLamp, kitTrash, kitUtility, kitBush, kitTrafficLight;

        /// Sidewalk dressing (planter boxes, benches, crates outside the shops): spawned without hit tests.
        public GameObject[] decorPrefabs;

        /// Street stretches kept free of shops so a landmark can rise beside the road: side, from z, to z.
        readonly List<Vector3> plazas = new();
        /// Raised for game feel (particles, sound, camera).
        public static event Action<TrackObject> Collected, Struck, NearMiss, Smashed;
        public static event Action<LandmarkDef> LandmarkAhead;
        public const int NearMissBonus = 50;

        RunState lastState;
        int lastLane = 1, laneFrom = -1, announceIndex;
        float previousDistance;
        Vector3 previousRiderPosition;
        float laneChangedAt = -10f;

        /// Invented outlets only (pack, 24 Oct): real chain marks need a licence.
        static readonly string[] Outlets = { "CLUCK & CO", "SLICE HOUSE", "BURGER BARN", "DONUT DEPOT", "TAQUERIA SOL" };

        /// Client presentation stills only (AutoCapture `-branded`): shop signs carry the client's restaurant
        /// brands, Yum! Brands and the Ampex Brands portfolio, as wordmarks in brand colours. Never on in the
        /// shipped game, which keeps the invented outlets above.
        public static bool BrandedPreview;
        static readonly (string name, Color board, Color ink)[] Brands =
        {
            ("KFC", new Color(0.89f, 0.1f, 0.17f), Color.white),
            ("TACO BELL", new Color(0.44f, 0.13f, 0.51f), Color.white),
            ("PIZZA HUT", new Color(0.93f, 0.19f, 0.14f), Color.white),
            ("THE HABIT", new Color(0.12f, 0.12f, 0.14f), new Color(1f, 0.6f, 0.12f)),
            ("BELLAGREEN", new Color(0.36f, 0.58f, 0.24f), Color.white),
            ("AU BON PAIN", new Color(0.55f, 0.08f, 0.16f), new Color(1f, 0.95f, 0.85f)),
            ("LONG JOHN SILVER'S", new Color(0.05f, 0.24f, 0.5f), new Color(1f, 0.82f, 0.1f)),
            ("TIM HORTONS", new Color(0.78f, 0.06f, 0.18f), Color.white),
        };
        int brandIndex;
        readonly Dictionary<Color, Material> brandMats = new();

        readonly List<TrackObject> live = new();
        TrackPool objectPool;
        TrackPool Pool => objectPool ??= new TrackPool(transform);
        public int PooledCreated => objectPool?.Created ?? 0;
        public int PooledReused => objectPool?.Reused ?? 0;
        void OnDestroy() => objectPool?.Dispose();
        public IReadOnlyList<TrackObject> Live => live;
        public bool GroundSupport(float x, float z, out float height) => GroundSupport(x, z, 0f, out height);
        /// feetY is the rider's current height above the road: a rooftop platform only counts as ground
        /// once the rider has jumped high enough to reach it.
        public bool GroundSupport(float x, float z, float feetY, out float height)
        {
            height = 0f;
            float best = float.NegativeInfinity;
            foreach (var o in live)
            {
                if (!o || !o.gameObject.activeSelf) continue;
                if (o.kind == TrackKind.Platform)
                {
                    var platform = o.GetComponent<DashPlatform>();
                    if (platform && platform.Support(x, z, out float top) && top <= feetY + 0.6f) best = Mathf.Max(best, top);
                    continue;
                }
                if (o.kind != TrackKind.Gap) continue;
                if (Mathf.Abs(x - o.transform.position.x) > 1.12f || Mathf.Abs(z - o.transform.position.z) >= o.halfLength) continue;
                var gapPlatform = o.GetComponent<DashPlatform>();
                if (gapPlatform && gapPlatform.Support(x, z, out float gapTop)) { best = Mathf.Max(best, gapTop); continue; }
                return false;
            }
            // The rooftop kicker ramp in the right-hand lane is a real slope the bike rolls up.
            if (RooftopCourse.OnRamp(z) && Mathf.Abs(x - Tuning.LaneX[RooftopCourse.EntryLane]) < 1.05f)
                best = Mathf.Max(best, RampHeightAt(z));
            if (best > float.NegativeInfinity) height = best;
            return true;
        }
        readonly List<GameObject> segments = new();
        float builtTo, nextRow, nextPowerUp;
        int landmarkIndex, segmentIndex;

        void Start()
        {
            // The font atlas is dynamic: keep the curved text material pointed at it as glyphs get added.
            if (signTextMaterial && signFont)
            {
                signTextMaterial.mainTexture = signFont.material.mainTexture;
                Font.textureRebuilt += f => { if (f == signFont && signTextMaterial) signTextMaterial.mainTexture = f.material.mainTexture; };
            }

            // A fresh street for the title and for each intro; the run carries straight on from the intro.
            GameManager.Instance.StateChanged += s =>
            {
                if (s == RunState.Ready || s == RunState.Intro || (s == RunState.Running && lastState != RunState.Intro)) ResetTrack();
                if (s == RunState.Busted) ClearForBustShot();
                lastState = s;
            };
            GameManager.Instance.CouponReady += OnCouponReady;
            ResetTrack();
        }

        public void RefreshDistrict() => ResetTrack();

        void ResetTrack()
        {
            staticCohorts.Clear();
            DashCity.ApplyLighting(this);
            pending.Clear();
            building = null;
            foreach (var o in live) if (o) Pool.Release(o.gameObject);
            foreach (var s in segments) if (s) Destroy(s);
            live.Clear();
            segments.Clear();
            builtTo = -segmentLength * 12f; // deep city behind the title camera, as well as ahead of the run
            announceIndex = 0; previousDistance = 0f;
            plazas.Clear();
            nextRow = firstRowAt;
            nextPowerUp = FirstPowerUpAt;
            brandIndex = 0;
            lastOpen = 1;
            builtRooftops.Clear();
            ResetSpecials(0f);
            landmarkIndex = 0;
            segmentIndex = 0;
            Build(0f);
            while (Pump()) { }
        }

        // ---- Frame-sliced street building. A modular block is ~100 kit pieces plus a mesh merge; built in one
        // frame it cost 20-60 ms, and at top speed two blocks a second made the late run stutter. Each block is
        // now built over about five frames (road, left side, its merge, right side, its merge), queued well
        // beyond view distance so nothing pops in.

        readonly Queue<System.Collections.IEnumerator> pending = new();
        System.Collections.IEnumerator building;

        /// Advances the queued street work by one step. False when there is nothing left to do.
        bool Pump()
        {
            if (building == null)
            {
                if (pending.Count == 0) return false;
                building = pending.Dequeue();
            }
            if (!building.MoveNext()) building = null;
            return true;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            float d = gm.Distance;
            Build(d);
            // One step a frame; catch up faster if the queue backs up.
            Pump();
            if (pending.Count > 3) Pump();
            if (gm.State == RunState.Running)
            {
                if (rider.Lane != lastLane) { laneFrom = lastLane; laneChangedAt = Time.time; lastLane = rider.Lane; }
                MoveTraffic(d);
                Resolve(d);
                PedestrianContacts();
                previousRiderPosition = rider.transform.position;
                previousDistance = d;
                Announce(d);
                CheckMissedSpecials(d);
                Cull(d);
            }
            else lastLane = rider.Lane;
        }

        void Build(float d)
        {
            while (DashRun.District == 0 && landmarkIndex < (landmarks?.Length ?? 0) && landmarks[landmarkIndex].atDistance < d + Tuning.FarEdge * 2f)
            {
                if (landmarks[landmarkIndex].atDistance > d - 20f) SpawnLandmark(landmarks[landmarkIndex]);
                landmarkIndex++;
            }
            BuildRooftops(d);
            BuildSpecials(d);
            while (builtTo < d + Tuning.FarEdge + segmentLength * 2f)
            {
                var root = new GameObject("Segment");
                root.transform.SetParent(transform);
                // Root sits at the segment start so Cull() sees its real distance.
                root.transform.position = new Vector3(0f, 0f, builtTo);
                segments.Add(root);
                pending.Enqueue(SegmentSteps(root, builtTo));
                builtTo += segmentLength;
            }
            while (nextRow < d + Tuning.FarEdge)
            {
                // Rows with a long hurdle (a 6 m van or roadworks) get its length added to the gap after them,
                // so the clear road between rows is the same whatever was spawned.
                float extra = SpawnRow(nextRow);
                float densitySpeed = GameManager.Instance.Speed;
                nextRow += rowSpacing * Mathf.Lerp(1f, 1.6f, Mathf.InverseLerp(Tuning.SpeedBase, Tuning.SpeedMax, densitySpeed)) + extra;
            }
        }

        System.Collections.IEnumerator SegmentSteps(GameObject root, float z)
        {
            if (!root) yield break;
            var t = root.transform;
            int gapLane = DashRun.RoofGapLane(z);
            if (gapLane < 0)
                Block(t, new Vector3(0, -0.1f, z + segmentLength / 2f), new Vector3(8f, 0.2f, segmentLength), roadMaterial);
            else
            {
                for (int lane = 0; lane < 3; lane++)
                {
                    if (lane != gapLane)
                        Block(t, new Vector3(Tuning.LaneX[lane], -0.1f, z + 10f), new Vector3(2.3f, 0.2f, 20f), roadMaterial);
                    else
                    {
                        foreach (float dz in new[] { 3.25f, 16.75f })
                            Block(t, new Vector3(Tuning.LaneX[lane], -0.1f, z + dz), new Vector3(2.3f, 0.2f, 6.5f), roadMaterial);
                        SpawnRoofGap(lane, z + 10f);
                    }
                }
            }
            bool junction = Junction(z);
            if (junction) BuildJunction(t, z);
            foreach (float side in junction ? new float[0] : new[] { -1f, 1f })
            {
                var walk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                walk.transform.SetParent(t);
                walk.transform.localScale = new Vector3(3f, 0.4f, segmentLength);
                walk.transform.position = new Vector3(side * 5.5f, 0f, z + segmentLength / 2f);
                Paint(walk, sidewalkMaterial);
                Block(t, new Vector3(side * 4.05f, 0.12f, z + segmentLength / 2f), new Vector3(0.18f, 0.3f, segmentLength), curbMaterial);
            }
            // Dashed lane lines between the three lanes.
            foreach (float lx in new[] { -1.15f, 1.15f })
                for (float dz = 1f; dz < segmentLength; dz += 5f)
                    if ((gapLane < 0 || dz < 5f || dz > 13f) && !junction)
                        Block(t, new Vector3(lx, 0.005f, z + dz + 1.25f), new Vector3(0.12f, 0.02f, 2.5f), laneLineMaterial);
            DashCity.Signature(this, t, z);
            segmentIndex++;
            Combine(root);
            yield return null;

            foreach (float side in junction ? new float[0] : new[] { -1f, 1f })
            {
                if (!root) yield break;
                // Each side gets its own batching root, so the merge cost is split across frames too.
                var sideRoot = new GameObject(side < 0f ? "Left" : "Right").transform;
                sideRoot.SetParent(t, false);
                DashCity.Side(this, sideRoot, side, z);
                if (DashRun.District == 0 && !DashCity.OpenEdge(side, z, segmentLength)) StreetTree(sideRoot, side, z + 10f);
                yield return null;
                if (!root) yield break;
                Combine(sideRoot.gameObject);
                yield return null;
            }
            if (junction)
            {
                // People and cross traffic come a frame or two after the junction's geometry.
                yield return null;
                if (!root) yield break;
                JunctionPeople(t, z);
                yield return null;
                if (!root) yield break;
                SpawnCrossTraffic(z);
            }
            BatchCohort(root, z);
        }

        readonly Dictionary<int,List<GameObject>> staticCohorts = new();
        void BatchCohort(GameObject root, float z)
        {
            int key = Mathf.FloorToInt(z / (segmentLength * 3f));
            if (!staticCohorts.TryGetValue(key, out var members)) staticCohorts[key] = members = new List<GameObject>(3);
            members.Add(root);
            if (members.Count < 3) return;
            var objects = new List<GameObject>();
            foreach (var member in members) if (member)
                foreach (var renderer in member.GetComponentsInChildren<MeshRenderer>())
                {
                    var filter = renderer.GetComponent<MeshFilter>();
                    if (!renderer.enabled || renderer.isPartOfStaticBatch || renderer.HasPropertyBlock() || !filter || !filter.sharedMesh || !filter.sharedMesh.isReadable) continue;
                    if (renderer.GetComponent<TextMesh>() || renderer.GetComponentInParent<Pedestrian>() || renderer.GetComponentInParent<LODGroup>() || renderer.GetComponentInParent<Billboard>() || renderer.GetComponentInParent<Bobber>() || renderer.GetComponentInParent<Blinker>() || renderer.GetComponentInParent<TrafficLight>()) continue;
                    objects.Add(renderer.gameObject);
                }
            if (objects.Count > 1) StaticBatchingUtility.Combine(objects.ToArray(), gameObject);
            staticCohorts.Remove(key);
        }

        Material patchMaterial;

        /// Markings and wear that make the asphalt read as a real downtown street: solid edge lines, a zebra
        /// crossing with stop line every fourth block, manhole covers and darker patch repairs.
        void RoadDetail(Transform root, float z)
        {
            float mid = z + segmentLength / 2f;
            foreach (float side in new[] { -1f, 1f })
                Block(root, new Vector3(side * 3.62f, 0.006f, mid), new Vector3(0.14f, 0.02f, segmentLength), laneLineMaterial);
            if (segmentIndex % 4 == 1)
            {
                for (float x = -3.1f; x <= 3.11f; x += 0.78f)
                    Block(root, new Vector3(x, 0.008f, z + 2.2f), new Vector3(0.44f, 0.02f, 3f), laneLineMaterial);
                Block(root, new Vector3(0f, 0.008f, z + 0.25f), new Vector3(7.2f, 0.02f, 0.3f), laneLineMaterial);
                // Signals on both corners of the crossing.
                if (kitTrafficLight)
                    foreach (float side in new[] { -1f, 1f })
                        Prop(root, kitTrafficLight, new Vector3(side * 4.5f, SidewalkTop, z + 0.9f), 180f, 1.1f);
            }
            if (!patchMaterial && roadMaterial)
            {
                patchMaterial = new Material(roadMaterial) { name = "Road patch" };
                patchMaterial.SetColor("_BaseColor", roadMaterial.GetColor("_BaseColor") * 0.92f);
            }
            if (Random.value < 0.55f)
                Block(root, new Vector3(Random.Range(-2.6f, 2.6f), 0.004f, z + Random.Range(5f, 15f)),
                      new Vector3(Random.Range(0.7f, 1.4f), 0.02f, Random.Range(1f, 2.4f)), patchMaterial);
            if (Random.value < 0.4f)
            {
                var cover = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(cover.GetComponent<Collider>());
                cover.transform.SetParent(root);
                cover.transform.position = new Vector3(Tuning.LaneX[Random.Range(0, Tuning.Lanes)] + Random.Range(-0.5f, 0.5f), 0.01f, z + Random.Range(4f, 16f));
                cover.transform.localScale = new Vector3(0.9f, 0.01f, 0.9f);
                Paint(cover, lampPostMaterial);
            }
        }

        /// Street furniture between the shopfronts: a red hydrant or a bench, near the curb.
        void Furniture(Transform root, float side, float z)
        {
            float r = Random.value;
            if (r < 0.3f && awningMaterials != null && awningMaterials.Length > 0)
            {
                // Between the lamp posts (z+5, z+15); the sidewalk top is at 0.2.
                float hz = z + Random.Range(8f, 12f);
                var red = awningMaterials[0];
                Block(root, new Vector3(side * 4.55f, 0.45f, hz), new Vector3(0.26f, 0.5f, 0.26f), red);
                Block(root, new Vector3(side * 4.55f, 0.74f, hz), new Vector3(0.2f, 0.1f, 0.2f), red);
                Block(root, new Vector3(side * 4.55f, 0.5f, hz), new Vector3(0.42f, 0.1f, 0.12f), red);
            }
            else if (r < 0.42f && kitTrash)
                Prop(root, kitTrash, new Vector3(side * 6.55f, SidewalkTop, z + Random.Range(6f, 14f)), side > 0 ? -90f : 90f, 0.9f);
            else if (r < 0.5f && kitUtility)
                Prop(root, kitUtility, new Vector3(side * 6.6f, SidewalkTop, z + Random.Range(6f, 14f)), side > 0 ? -90f : 90f, 1f);
            else if (r < 0.62f && doorMaterial)
            {
                float bz = z + Random.Range(3f, 17f);
                float bx = side * 6.4f;
                Block(root, new Vector3(bx, 0.62f, bz), new Vector3(0.5f, 0.08f, 1.8f), doorMaterial);
                Block(root, new Vector3(bx + side * 0.22f, 0.95f, bz), new Vector3(0.08f, 0.5f, 1.8f), doorMaterial);
                foreach (float lz in new[] { -0.75f, 0.75f })
                    Block(root, new Vector3(bx, 0.4f, bz + lz), new Vector3(0.46f, 0.4f, 0.08f), lampPostMaterial);
            }
        }

        int lastOpen = 1;

        /// Returns how much longer than a short hurdle the row's longest one is.
        float SpawnRow(float z)
        {
            SpawnWallBurgers(z);
            // One lane is always left open so every row is passable, and it is never more than one lane over
            // from the last row's, so the way through is always one swipe away.
            int open = Mathf.Clamp(lastOpen + Random.Range(-1, 2), 0, Tuning.Lanes - 1);
            lastOpen = open;
            float roofZ = Mathf.Floor(z / 20f) * 20f;
            if (DashRun.RoofGapLane(roofZ) >= 0 || DashRun.RoofGapLane(roofZ - 20f) >= 0 || DashRun.RoofGapLane(roofZ + 20f) >= 0)
            {
                // Keep the complete approach and landing clear; a neighbouring lane always remains walkable.
                return 0f;
            }
            float longest = 0f;
            bool token = z >= nextPowerUp;
            if (token) nextPowerUp = z + Random.Range(PowerUpGap * 0.75f, PowerUpGap * 1.25f);

            // The rooftop shortcut runs above the right-hand pavement; the three traffic lanes keep their
            // hazards, so the roofs are a real bypass rather than a cleared lane.
            // At a junction the cross traffic is the hazard; the street rows there only carry food.
            if (NearJunction(z)) { for (int lane = 0; lane < Tuning.Lanes; lane++) SpawnPickupLine(lane, z); return 0f; }
            bool rampLane = RooftopCourse.KeepLaneClear(z);
            int bikeLane = z > BikeReservedFrom && z < BikeReservedTo ? BikeReservedLane : -1;
            for (int l = 0; l < Tuning.Lanes; l++) if (LaneReserved(l, z)) { open = l; lastOpen = l; } // the coupon lane is the way through
            if (rampLane) { open = RooftopCourse.EntryLane; lastOpen = open; }
            for (int lane = 0; lane < Tuning.Lanes; lane++)
            {
                if (lane == bikeLane) continue;
                if (LaneReserved(lane, z)) { SpawnPickupLine(lane, z); continue; }
                if (rampLane && lane == RooftopCourse.EntryLane) { if (!RooftopCourse.OnRamp(z) && !RooftopCourse.OnRamp(z + 5f)) SpawnPickupLine(lane, z); continue; }
                if (token && lane == open) { SpawnPowerUp((PowerUp)Random.Range(0, PowerUps.Count), lane, z); continue; }
                if (lane == open || Random.value < 0.45f || obstacles == null || obstacles.Length == 0)
                {
                    SpawnPickupLine(lane, z);
                    continue;
                }
                var def = SpawnObstacle(WeightedObstacle(), lane, z);
                if (def && def.drift <= 0f) longest = Mathf.Max(longest, def.size.z);
            }
            return Mathf.Max(0f, longest - 1f);
        }

        /// Wall runs can start anywhere (client, 3 Nov), so some street rows put a line of burgers along one
        /// facade at the wall-run height: a reward for running the wall, out of reach from the road.
        void SpawnWallBurgers(float z)
        {
            if (!DashRun.CanRideWalls) return; // wall running removed (4 Nov 2026)
            int row = Mathf.Abs(Mathf.FloorToInt(z / rowSpacing));
            if (row % 5 != 2) return;
            int side = (row / 5) % 2 == 0 ? -1 : 1;
            if (!DashRun.WallAvailable(side, z) || !DashRun.WallAvailable(side, z + 10f)) return;
            var surface = side < 0 ? RunSurface.LeftWall : RunSurface.RightWall;
            for (int j = 0; j < 4; j++)
            {
                var at = SurfaceTraversal.Point(surface, 0f, 0.5f) + Vector3.forward * (z + j * 2.5f);
                var token = RentBurger("wall-burger", at, SurfaceTraversal.Orientation(surface), false);
                token.name = "Wall-run burger / " + surface;
                token.transform.rotation = SurfaceTraversal.Orientation(surface);
                Register(token, TrackKind.Pickup, 1, 0.4f, 25, 0);
                live[live.Count - 1].surface = surface;
            }
        }

        void SpawnRoofGap(int lane, float z)
        {
            var go = new GameObject("Skypark roof gap"); go.transform.SetParent(transform);
            go.transform.position = new Vector3(Tuning.LaneX[lane], 0, z);
            var edge = DashCity.Mat(this, "gapEdge", new Color(1f, 0.5f, 0.12f), 0.4f);
            foreach (float dz in new[] { -3.5f, 3.5f })
                DashCity.Part(go.transform, go.transform.position + new Vector3(0, 0.03f, dz), new Vector3(2.25f, 0.06f, 0.12f), edge);
            Register(go, TrackKind.Gap, lane, 3.5f, 0, 0);
            // Every second gap includes a delivery platform moving vertically through the opening.
            if (Mathf.FloorToInt(z / 80f) % 2 == 0)
            {
                if (!movingDeliveryPlatform) throw new InvalidOperationException("Missing moving platform mesh");
                var platform = new GameObject("Moving delivery platform");
                platform.transform.SetParent(go.transform, false);
                platform.transform.localPosition = Vector3.down * .5f;
                var visual = Instantiate(movingDeliveryPlatform, platform.transform);
                var renderers = visual.GetComponentsInChildren<Renderer>();
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                visual.transform.localScale = new Vector3(2.05f / bounds.size.x, .22f / bounds.size.y, 6f / bounds.size.z);
                visual.transform.localPosition = Vector3.down * .11f;
                go.AddComponent<DashPlatform>().platform = platform.transform;
            }
        }

        /// Signature street hurdles, all real objects: construction scaffolding over a lane to slide under,
        /// or in Festivals a stage lighting truss whose light bar drops and lifts on the music's beat.
        void SpawnSignatureObstacle(int lane, float z)
        {
            float x = Tuning.LaneX[lane];
            if (DashRun.District != 2)
            {
                ObstacleDef scaffold = null;
                foreach (var def in obstacles) if (def && def.id == "cargo-slide-gate") scaffold = def;
                if (scaffold) { SpawnObstacle(scaffold, lane, z); return; }
            }
            var root = new GameObject("Festival lighting truss / beat gate");
            root.transform.SetParent(transform);
            root.transform.position = new Vector3(x, 0, z);
            var truss = DashCity.Mat(this, "trussAluminium", new Color(0.72f, 0.74f, 0.78f));
            var housing = DashCity.Mat(this, "trussLampHousing", new Color(0.05f, 0.05f, 0.06f));
            var warm = DashCity.Mat(this, "trussLampWarm", new Color(1f, 0.72f, 0.35f), 2.4f);
            var pink = DashCity.Mat(this, "roofNeonPink", new Color(1f, 0.1f, 0.55f), 2.2f);
            // Square box-truss towers with cross bracing, and a header truss across the lane.
            foreach (float side in new[] { -1f, 1f })
            {
                foreach (float dx in new[] { -0.15f, 0.15f })
                    foreach (float dz in new[] { -0.15f, 0.15f })
                        DashCity.Part(root.transform, new Vector3(x + side * 1.12f + dx, 2f, z + dz), new Vector3(0.05f, 4f, 0.05f), truss);
                for (float y = 0.4f; y < 4f; y += 0.5f)
                    DashCity.Part(root.transform, new Vector3(x + side * 1.12f, y, z - 0.15f), new Vector3(0.32f, 0.03f, 0.03f), truss);
                DashCity.Part(root.transform, new Vector3(x + side * 1.12f, 0.03f, z), new Vector3(0.6f, 0.06f, 0.6f), housing);
            }
            foreach (float dy in new[] { 3.85f, 4.15f })
                DashCity.Part(root.transform, new Vector3(x, dy, z), new Vector3(2.6f, 0.05f, 0.05f), truss);
            // The moving light bar: a row of stage spots on a pipe, dropped to riding height on the beat.
            var bar = new GameObject("Light bar").transform;
            bar.SetParent(root.transform, false);
            bar.position = new Vector3(x, 1.75f, z);
            DashCity.Part(bar, new Vector3(x, 1.75f, z), new Vector3(2.1f, 0.12f, 0.12f), truss);
            for (int i = -2; i <= 2; i++)
            {
                DashCity.Part(bar, new Vector3(x + i * 0.42f, 1.55f, z), new Vector3(0.24f, 0.3f, 0.24f), housing);
                DashCity.Part(bar, new Vector3(x + i * 0.42f, 1.39f, z - 0.02f), new Vector3(0.17f, 0.03f, 0.17f), warm);
            }
            DashCity.Part(bar, new Vector3(x, 1.86f, z - 0.07f), new Vector3(2f, 0.04f, 0.02f), pink);
            root.AddComponent<DashRhythmGate>().beam = bar;
            Register(root, TrackKind.Overhead, lane, 0.45f, 0, 0);
            // Closed, the bar's spots hang at 1.3 m: slide under them, or ride through on the lift.
            var gate = live[live.Count - 1]; gate.clearance = 1.3f; gate.height = 1.95f;
        }

        void SpawnPickupLine(int lane, float z)
        {
            if (pickups == null || pickups.Length == 0 || Random.value < 0.4f) return;
            var def = WeightedPickup();
            for (int i = 0; i < 3; i++)
            {
                var go = Pool.Rent("food-" + def.GetInstanceID(), () =>
                {
                    if (!def.prefab) throw new InvalidOperationException("Missing food pickup model: " + def.id);
                    var item = Instantiate(def.prefab);
                    if (!def.prefab) { item.transform.localScale = Vector3.one * 0.7f; Tint(item, def.placeholderColor); }
                    item.AddComponent<PickupSpin>();
                    if (haloMaterial) Halo(item.transform);
                    return item;
                }, new Vector3(Tuning.LaneX[lane], def.prefab ? 0.45f : 0.8f, z + i * 2.5f), Quaternion.identity);
                go.name = def.displayName;
                go.transform.SetParent(transform);
                go.transform.position = new Vector3(Tuning.LaneX[lane], def.prefab ? 0.45f : 0.8f, z + i * 2.5f);
                if (!def.prefab) { go.transform.localScale = Vector3.one * 0.7f; Tint(go, def.placeholderColor); }
                Register(go, TrackKind.Pickup, lane, 0.4f, def.points, 0f);
            }
        }

        /// First power-up token comes early so new players meet one; then roughly every PowerUpGap metres.
        const float FirstPowerUpAt = 140f, PowerUpGap = 360f;

        /// A glowing token in the power-up's colour with its emblem on the front: a horseshoe magnet,
        /// a lightning bolt, or "2X". It floats and bobs (no spin, so the emblem keeps facing the rider).
        void SpawnPowerUp(PowerUp kind, int lane, float z)
        {
            var root = Pool.Rent("powerup-" + kind, () => CreatePowerUp(kind), new Vector3(Tuning.LaneX[lane], 1.05f, z), Quaternion.identity);
            Register(root, TrackKind.Pickup, lane, 0.6f, 0, 0f);
            live[live.Count - 1].powerUp = (int)kind;
        }

        GameObject CreatePowerUp(PowerUp kind)
        {
            int index = (int)kind;
            if (powerUpModels == null || index >= powerUpModels.Length || !powerUpModels[index])
                throw new InvalidOperationException("Missing power-up model: " + kind);
            var root = Instantiate(powerUpModels[index], transform);
            root.name = PowerUps.Names[index];
            root.transform.localPosition = Vector3.zero;
            root.transform.localScale *= .8f;
            var spin = root.AddComponent<PickupSpin>();
            spin.degreesPerSecond = 0f;
            spin.bob = 0.12f;
            if (haloMaterial) Halo(root.transform);
            return root;
        }

        /// A tinted, slightly glowing primitive part (power-up tokens).
        GameObject Shape(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Color c, float glow)
        {
            if (WorldPalette.IsCoolNeon(c)) { c = WorldPalette.Surface(c); glow = 0; }
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            if (tintMaterial) r.sharedMaterial = tintMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_BaseColor", c);
            mpb.SetColor("_Color", c);
            mpb.SetColor("_EmissionColor", c * glow);
            r.SetPropertyBlock(mpb);
            return go;
        }

        /// Places an obstacle (or a standing substitute for a courier with no room); returns what was placed.
        ObstacleDef SpawnObstacle(ObstacleDef def, int lane, float z)
        {
            // Another courier rides toward its meeting point from behind it, so it needs its lane clear for that
            // stretch; otherwise put something that stands still there instead.
            float path = def.drift * (z - GameManager.Instance.Distance) + def.size.z + 2f;
            if (def.drift > 0f && !LaneClearBehind(lane, z, path))
            {
                for (int tries = 0; tries < 6 && def.drift > 0f; tries++) def = WeightedObstacle();
                if (def.drift > 0f) return null;
            }
            // Burgers it would ride through on its way are taken off the street (they're far out in the haze).
            if (def.drift > 0f) ClearPickupsBehind(lane, z, path);
            var chosen = def;
            var go = Pool.Rent("obstacle-" + def.GetInstanceID(), () =>
            {
                if (!chosen.prefab) throw new InvalidOperationException("Missing obstacle model: " + chosen.id);
                var item = Instantiate(chosen.prefab);
                if (!chosen.prefab) { item.transform.localScale = chosen.size; Tint(item, chosen.placeholderColor); }
                return item;
            }, new Vector3(Tuning.LaneX[lane], def.prefab ? 0f : def.size.y / 2f, z), Quaternion.identity);
            go.name = def.displayName;
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(Tuning.LaneX[lane], def.prefab ? 0f : def.size.y / 2f, z);
            BrandVehicle(go, def.id);
            if (!def.prefab)
            {
                go.transform.localScale = def.size;
                Tint(go, def.placeholderColor);
            }
            Register(go, def.kind, lane, def.size.z / 2f, 0, 0f);
            var t = live[live.Count - 1];
            t.heavy = def.heavy;
            t.height = def.size.y;
            t.halfWidth = Mathf.Min(1.1f, def.size.x * 0.5f);
            if (def.kind == TrackKind.Overhead) t.clearance = def.id == "cargo-slide-gate" ? 1.98f : 1.4f;
            t.drift = def.drift;
            t.meetZ = z;
            if (t.drift > 0f) PlaceTraffic(t, GameManager.Instance.Distance);
            return def;
        }

        /// No obstacle in this lane over the stretch a courier rides through before the rider meets it.
        bool LaneClearBehind(int lane, float z, float length)
        {
            foreach (var o in live)
                if (o && o.kind != TrackKind.Pickup && o.lane == lane && o.transform.position.z + o.halfLength > z - length && o.transform.position.z < z) return false;
            return true;
        }

        void ClearPickupsBehind(int lane, float z, float length)
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var o = live[i];
                if (!o || o.kind != TrackKind.Pickup || o.lane != lane) continue;
                float pz = o.transform.position.z;
                if (pz < z - length || pz > z + 1f) continue;
                Pool.Release(o.gameObject);
                live.RemoveAt(i);
            }
        }

        /// Other couriers ride the same way at a fraction of the rider's pace, arriving on their meeting point
        /// exactly when the rider does: z = meet + drift * (d - meet). Once passed they drop back behind.
        static void PlaceTraffic(TrackObject o, float d)
        {
            var p = o.transform.position;
            p.z = o.meetZ + o.drift * (d - o.meetZ);
            o.transform.position = p;
        }

        void MoveTraffic(float d)
        {
            foreach (var o in live)
            {
                if (!o || o.drift <= 0f || o.consumed) continue;
                PlaceTraffic(o, d);
                if (!o.passed && o.transform.position.z + o.halfLength + Tuning.PlayerHalf < d)
                {
                    o.passed = true;
                    Missions.Add(MissionStat.Overtakes, 1);
                }
            }
        }

        /// One shopfront per side per segment: brick facade, cream cornice, rows of upper windows (some lit),
        /// a lit shop window under a striped awning, a framed door, and a sign board with an outlet name.
        void Storefront(Transform parent, float side, float z)
        {
            float h = Random.Range(8f, 15f);
            float mid = z + segmentLength / 2f;
            float len = segmentLength - 0.6f;
            float face = side * 7f; // street-facing wall plane
            if (kitGround != null && kitGround.Length > 0)
            {
                KitStorefront(parent, side, z, mid, len, face);
                return;
            }
            var brick = Pick(facadeMaterials);
            Block(parent, new Vector3(side * 10f, h / 2f, mid), new Vector3(6f, h, len), brick).name = "Storefront";
            Block(parent, new Vector3(face, h + 0.25f, mid), new Vector3(0.5f, 0.5f, len + 0.2f), trimMaterial);
            Block(parent, new Vector3(face, 4.35f, mid), new Vector3(0.3f, 0.18f, len + 0.1f), trimMaterial);

            // Upper floors: a row of windows every 3 m of height, cream sills, about a third lit warm.
            for (float wy = 5.5f; wy < h - 1.2f; wy += 3f)
                for (float wz = -len / 2f + 2f; wz < len / 2f - 1f; wz += 3.2f)
                {
                    bool lit = Random.value < 0.35f && windowMaterials != null && windowMaterials.Length > 1;
                    Block(parent, new Vector3(face, wy, mid + wz), new Vector3(0.12f, 1.6f, 1.3f), lit ? windowMaterials[1] : windowMaterials[0]);
                    Block(parent, new Vector3(face, wy - 0.88f, mid + wz), new Vector3(0.22f, 0.12f, 1.5f), trimMaterial);
                }

            // Ground floor: big warm shop window with frame, awning, framed door.
            Block(parent, new Vector3(face, 1.6f, mid - 2f), new Vector3(0.12f, 2.2f, len * 0.55f), windowMaterials != null && windowMaterials.Length > 1 ? windowMaterials[1] : null);
            Block(parent, new Vector3(face, 0.42f, mid - 2f), new Vector3(0.2f, 0.25f, len * 0.57f), trimMaterial);
            WindowDisplay(parent, side, face, mid - 2f, len * 0.55f);
            var awning = Block(parent, new Vector3(side * 6.6f, 3.15f, mid - 2f), new Vector3(1.3f, 0.12f, len * 0.6f), Pick(awningMaterials));
            awning.transform.rotation = Quaternion.Euler(0f, 0f, side * 18f);
            float doorZ = mid + len * 0.33f;
            Block(parent, new Vector3(face, 1.35f, doorZ), new Vector3(0.16f, 2.7f, 1.6f), trimMaterial);
            Block(parent, new Vector3(face - side * 0.04f, 1.25f, doorZ), new Vector3(0.12f, 2.4f, 1.2f), doorMaterial);
            Block(parent, new Vector3(face - side * 0.06f, 1.9f, doorZ), new Vector3(0.1f, 0.8f, 0.8f), windowMaterials != null && windowMaterials.Length > 1 ? windowMaterials[1] : null);

            BuildingDetail(parent, side, mid, len, h, face, brick);

            SignBoard(parent, side, mid - 2f, 5.15f);
        }

        /// A sign board in one of the pack's styles (red with cream letters, cream with red, green with cream)
        /// with an invented outlet name, centred at cz along the street and at height y.
        void SignBoard(Transform parent, float side, float cz, float y)
        {
            // Every shop has a sign board: red with cream letters, cream with red, or green with cream (as in the pack).
            int style = Random.Range(0, 3);
            var boardMat = style == 0 ? Pick(awningMaterials, 0) : style == 1 ? trimMaterial : Pick(awningMaterials, 1);
            Color ink = style == 1 ? new Color(0.78f, 0.1f, 0.12f) : new Color(1f, 0.95f, 0.85f);
            string name = Outlets[Random.Range(0, Outlets.Length)];
            // Yum! Brands storefronts (client, 4 Nov 2026), cycled so every block shows a spread of them;
            // the presentation flag adds the wider client portfolio.
            {
                var b = BrandedPreview ? Brands[brandIndex++ % Brands.Length] : YumBrands[brandIndex++ % YumBrands.Length];
                name = b.name;
                ink = b.ink;
                boardMat = BrandMaterial(b.board);
            }
            Block(parent, new Vector3(side * 6.86f, y, cz), new Vector3(0.16f, 1.3f, 6.4f), trimMaterial).name = "SignFrame";
            Block(parent, new Vector3(side * 6.8f, y, cz), new Vector3(0.12f, 1.1f, 6.1f), boardMat).name = "SignBoard";
            var sign = new GameObject("Sign", typeof(TextMesh));
            sign.transform.SetParent(parent);
            sign.transform.position = new Vector3(side * 6.72f, y, cz);
            sign.transform.rotation = Quaternion.Euler(0f, side > 0 ? 90f : -90f, 0f);
            var tm = sign.GetComponent<TextMesh>();
            tm.text = name;
            // Long names shrink to fit the 6 m board.
            if (name.Length > 11) tm.characterSize = 0.2f * 11f / name.Length;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.characterSize = 0.2f;
            tm.fontSize = 56;
            tm.color = ink;
            if (signFont)
            {
                tm.font = signFont;
                sign.GetComponent<MeshRenderer>().sharedMaterial = signTextMaterial ? signTextMaterial : signFont.material;
            }
            else tm.fontStyle = FontStyle.Bold;
        }

        Material BrandMaterial(Color c)
        {
            if (brandMats.TryGetValue(c, out var m) && m) return m;
            m = new Material(trimMaterial) { name = "Brand board" };
            m.SetColor("_BaseColor", c);
            // A lit lightbox sign, the way the chains' boards read at dusk.
            m.SetColor("_EmissionColor", c * 0.85f);
            m.EnableKeyword("_EMISSION");
            return brandMats[c] = m;
        }

        /// A shopfront assembled from the modular kit on a 2 m grid: nine columns of lit shop windows and a glazed
        /// door on the ground floor, a sign band, two to four upper floors of windows alternating with plain wall,
        /// and a cornice, in one of three styles (red brick, painted plaster, metal and glass). The awning, sign
        /// board, pilasters and fire escape are layered on as before.
        void KitStorefront(Transform parent, float side, float z, float mid, float len, float face)
        {
            const int cols = 9;
            const float band = 0.9f;
            int style = Random.Range(0, 3);
            int floors = Random.Range(2, 5);
            float top = 3f + band + floors * 3f;
            float h = top + 1f;
            float yaw = side > 0 ? -90f : 90f; // kit fronts face +Z; turn them to face the street
            Material paint = style == 1 && kitPaints != null && kitPaints.Length > 0 ? Pick(kitPaints) : null;
            // Flat colours on the body box behind the facade (a kit texture would stretch across 6 x 15 m).
            Material body = style == 0 ? kitBrickBody : style == 1 ? trimMaterial : kitMetalBody;
            Block(parent, new Vector3(side * 10.1f, h / 2f, mid), new Vector3(6f, h, len), body ? body : Pick(facadeMaterials)).name = "Storefront";

            var ground = kitGround[Random.Range(0, kitGround.Length)];
            var windows = style == 0 ? kitBrickWindows : style == 1 ? kitTrimWindows : kitMetalWindows;
            var window = windows != null && windows.Length > 0 ? windows[Random.Range(0, windows.Length)] : null;
            var plain = style == 0 ? kitBrickPlain : style == 1 ? kitTrimPlain : kitMetalPlain;
            var cornice = style == 0 ? kitBrickCornice : style == 1 ? kitTrimCornice : kitMetalCornice;
            int doorCol = Random.value < 0.5f ? 6 : 2;
            // Shop fronts painted in the awning colour (the kit's near-black frames read as bars).
            var awningMat = Pick(awningMaterials);
            for (int c = 0; c < cols; c++)
            {
                float cz = mid + (c - (cols - 1) / 2f) * 2f;
                Kit(parent, c == doorCol && kitDoorFrame ? kitDoorFrame : ground, new Vector3(face, 0f, cz), yaw, kitGlass, kitShopGlass, paint, awningMat);
                for (int f = 0; f < floors; f++)
                    Kit(parent, c % 2 == 0 ? window : plain, new Vector3(face, 3f + band + f * 3f, cz), yaw, null, null, paint);
                Kit(parent, cornice, new Vector3(face, top, cz), yaw, null, null, paint);
            }
            // Sign band between the shop floor and the flats above.
            Block(parent, new Vector3(face + side * 0.02f, 3f + band / 2f, mid), new Vector3(0.3f, band, cols * 2f), paint ? paint : trimMaterial);

            var awning = Block(parent, new Vector3(side * 6.6f, 2.95f, mid - 2f), new Vector3(1.3f, 0.12f, len * 0.6f), awningMat);
            awning.transform.rotation = Quaternion.Euler(0f, 0f, side * 18f);
            foreach (float end in new[] { -1f, 1f })
                Block(parent, new Vector3(face - side * 0.02f, h / 2f, mid + end * (cols + 0.35f)), new Vector3(0.4f, h, 0.7f), paint ? paint : trimMaterial);
            RoofClutter(parent, side, mid, len, h);
            if (h > 11f && Random.value < 0.35f) FireEscape(parent, side, mid, h, face);
            SignBoard(parent, side, mid - 2f, 3f + band / 2f);
        }

        /// Instantiates a kit piece as a direct child of the segment (so it static-batches), optionally swapping
        /// one material (shop glass) and repainting the cream trim.
        void Kit(Transform parent, GameObject piece, Vector3 pos, float yaw, Material from, Material to, Material paint, Material frame = null)
        {
            if (!piece) return;
            var go = Instantiate(piece, pos, Quaternion.Euler(0f, yaw, 0f), parent);
            if ((from && to) || paint || frame)
            {
                var r = go.GetComponent<MeshRenderer>();
                var ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++)
                {
                    if (from && to && ms[i] == from) ms[i] = to;
                    else if (frame && ms[i] == kitFrame) ms[i] = frame;
                    else if (paint && ms[i] == kitTrim) ms[i] = paint;
                }
                r.sharedMaterials = ms;
            }
        }

        /// The lit shop window was a blank glowing slab. Glazing bars split it into panes, a cream sill runs along
        /// the bottom, and everyday goods sit on it: stacked boxes, potted plants, a small menu board.
        void WindowDisplay(Transform parent, float side, float face, float cz, float width)
        {
            float glass = face - side * 0.08f; // street side of the window block
            int panes = Mathf.Max(3, Mathf.RoundToInt(width / 1.8f));
            for (int i = 1; i < panes; i++)
                Block(parent, new Vector3(glass, 1.6f, cz - width / 2f + width * i / panes), new Vector3(0.08f, 2.2f, 0.1f), trimMaterial);
            Block(parent, new Vector3(glass, 2.35f, cz), new Vector3(0.08f, 0.08f, width), trimMaterial);
            Block(parent, new Vector3(face - side * 0.26f, 0.66f, cz), new Vector3(0.4f, 0.1f, width), trimMaterial);
            float itemX = face - side * 0.28f;
            for (float zz = cz - width / 2f + 0.5f; zz < cz + width / 2f - 0.4f; zz += Random.Range(0.55f, 0.95f))
            {
                float r = Random.value;
                if (r < 0.4f)
                {
                    // Two stacked boxes.
                    Block(parent, new Vector3(itemX, 0.86f, zz), new Vector3(0.36f, 0.3f, 0.36f), trimMaterial);
                    Block(parent, new Vector3(itemX, 1.12f, zz), new Vector3(0.24f, 0.22f, 0.24f), Pick(awningMaterials));
                }
                else if (r < 0.75f && foliageMaterial)
                {
                    // A potted plant.
                    Block(parent, new Vector3(itemX, 0.82f, zz), new Vector3(0.22f, 0.22f, 0.22f), doorMaterial);
                    Block(parent, new Vector3(itemX, 1.07f, zz), new Vector3(0.36f, 0.3f, 0.36f), foliageMaterial).transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                }
                else
                {
                    // A chalk menu board on a little easel.
                    Block(parent, new Vector3(itemX, 1.05f, zz), new Vector3(0.06f, 0.6f, 0.42f), lampPostMaterial).transform.rotation = Quaternion.Euler(0f, 0f, side * 8f);
                    Block(parent, new Vector3(itemX - side * 0.035f, 1.05f, zz), new Vector3(0.02f, 0.5f, 0.34f), trimMaterial).transform.rotation = Quaternion.Euler(0f, 0f, side * 8f);
                }
            }
        }

        /// Breaks up the box: corner pilasters, a band course, rooftop clutter (AC units, a water tower or
        /// chimney) and, on some buildings, a black iron fire escape zig-zagging up the upper floors.
        void BuildingDetail(Transform parent, float side, float mid, float len, float h, float face, Material brick)
        {
            foreach (float end in new[] { -1f, 1f })
                Block(parent, new Vector3(face + side * 0.05f, h / 2f, mid + end * (len / 2f - 0.35f)), new Vector3(0.35f, h, 0.7f), trimMaterial);
            if (h > 10f) Block(parent, new Vector3(face, 7.9f, mid), new Vector3(0.24f, 0.22f, len), trimMaterial);

            RoofClutter(parent, side, mid, len, h, brick);
            if (Random.value < 0.35f && h > 10f) FireEscape(parent, side, mid, h, face);
        }

        /// Rooftop clutter: AC units, sometimes a wooden water tower on legs or a chimney.
        void RoofClutter(Transform parent, float side, float mid, float len, float h, Material chimney = null)
        {
            float roofX = side * 9.4f;
            int units = Random.Range(1, 3);
            for (int i = 0; i < units; i++)
                PlaceAsset(roofVentModel, parent, new Vector3(roofX + Random.Range(-1f, 1f), h, mid + Random.Range(-len * .35f, len * .35f)), 1.4f);
            float choice = Random.value;
            if (choice < .25f)
                PlaceAsset(roofTankModel, parent, new Vector3(roofX, h, mid + Random.Range(-3f, 3f)), 3.1f);
            else if (choice < .55f)
                PlaceAsset(roofChimneyModel, parent, new Vector3(side * 8.2f, h, mid + Random.Range(-len * .4f, len * .4f)), 2f);
        }
        /// Black iron fire escape on the far half of the facade: a landing per floor, rails, and stairs between.
        void FireEscape(Transform parent, float side, float mid, float h, float face)
        {
            float len = segmentLength - 0.6f;
            // Fire escape on the far half of the facade: a landing per floor, rails, and ladders between.
            float fz = mid + len * 0.22f;
            float fx = face - side * 0.55f;
            for (float fy = 5.6f; fy < h - 1.5f; fy += 3f)
            {
                Block(parent, new Vector3(fx, fy - 0.95f, fz), new Vector3(1f, 0.06f, 3.6f), lampPostMaterial);
                Block(parent, new Vector3(fx - side * 0.48f, fy - 0.45f, fz), new Vector3(0.05f, 0.05f, 3.6f), lampPostMaterial);
                foreach (float pz in new[] { -1.75f, 0f, 1.75f })
                    Block(parent, new Vector3(fx - side * 0.48f, fy - 0.7f, fz + pz), new Vector3(0.05f, 0.5f, 0.05f), lampPostMaterial);
                if (fy + 3f < h - 1.5f)
                {
                    // Stair from this landing up to the next: 3 m rise over about 3.2 m.
                    var ladder = Block(parent, new Vector3(fx, fy + 0.55f, fz), new Vector3(0.5f, 0.06f, 4.4f), lampPostMaterial);
                    ladder.transform.rotation = Quaternion.Euler(-43f, 0f, 0f);
                }
            }
        
        }

        static Material Pick(Material[] set, int index) => set != null && set.Length > index ? set[index] : null;

        /// Planter boxes, benches and crates against the shopfronts: set dressing only (not obstacles).
        void SidewalkDecor(Transform parent, float side, float z)
        {
            if (decorPrefabs == null || decorPrefabs.Length == 0 || Random.value > 0.5f) return;
            var prefab = decorPrefabs[Random.Range(0, decorPrefabs.Length)];
            if (!prefab) return;
            // Between the lamp posts, backed against the building line and turned to face the street.
            Prop(parent, prefab, new Vector3(side * 6.3f, SidewalkTop, z + Random.Range(6.5f, 13.5f)), side > 0 ? -90f : 90f, 1f);
        }

        /// Black cast-iron street lamp.
        void LampPost(Transform parent, float side, float z)
        {
            float x = side * 4.6f;
            if (kitLamp)
            {
                // Cast-iron street lamp, its arm reaching out over the road.
                Prop(parent, kitLamp, new Vector3(x, SidewalkTop, z), side > 0 ? 90f : -90f, 1.4f);
                return;
            }
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(pole.GetComponent<Collider>());
            pole.transform.SetParent(parent);
            pole.transform.position = new Vector3(x, 2.6f, z);
            pole.transform.localScale = new Vector3(0.14f, 2.6f, 0.14f);
            Paint(pole, lampPostMaterial);
            Block(parent, new Vector3(x, 5.35f, z), new Vector3(0.42f, 0.55f, 0.42f), lampPostMaterial);
            var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(lamp.GetComponent<Collider>());
            lamp.transform.SetParent(parent);
            lamp.transform.position = new Vector3(x, 5.3f, z);
            lamp.transform.localScale = Vector3.one * 0.36f;
            Paint(lamp, bulbMaterial);
        }

        const float SidewalkTop = 0.2f;

        /// A street tree in a dark iron grate near the curb, between the lamp posts, on most blocks.
        void StreetTree(Transform parent, float side, float z)
        {
            if (kitTrees == null || kitTrees.Length == 0 || Random.value < 0.3f || InPlaza(side, z - 10f)) return;
            float x = side * 5.05f;
            Block(parent, new Vector3(x, SidewalkTop + 0.01f, z), new Vector3(1.3f, 0.03f, 1.3f), lampPostMaterial);
            Prop(parent, kitTrees[Random.Range(0, kitTrees.Length)], new Vector3(x, SidewalkTop, z), Random.Range(0f, 360f), Random.Range(0.5f, 0.6f));
        }

        /// Places a kit prop as a direct child of the segment (so it batches), standing on y with its real base.
        void Prop(Transform parent, GameObject prefab, Vector3 at, float yaw, float scale)
        {
            var go = Instantiate(prefab, at, Quaternion.Euler(0f, yaw, 0f), parent);
            go.transform.localScale = Vector3.one * scale;
            var mf = go.GetComponent<MeshFilter>();
            if (mf && mf.sharedMesh) go.transform.position += Vector3.up * (-mf.sharedMesh.bounds.min.y * scale);
        }

        /// Far downtown towers above the shop roofs, tinted toward the fog so they sit back.
        void Skyline(Transform parent, float side, float z)
        {
            if (skylineMaterials == null || skylineMaterials.Length == 0 || Random.value < 0.35f) return;
            float h = Random.Range(25f, 60f);
            float w = Random.Range(8f, 14f);
            Block(parent, new Vector3(side * Random.Range(34f, 60f), h / 2f, z + Random.Range(0f, segmentLength)), new Vector3(w, h, w), Pick(skylineMaterials));
        }

        /// Soft gold glow behind a burger, always facing the camera.
        void Halo(Transform burger)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = "Halo";
            q.transform.SetParent(burger, false);
            q.transform.localPosition = new Vector3(0f, 0.4f, 0.15f);
            q.transform.localScale = Vector3.one * 1.6f;
            q.GetComponent<Renderer>().sharedMaterial = haloMaterial;
            q.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            q.AddComponent<Billboard>();
        }

        static Material Pick(Material[] set) => set != null && set.Length > 0 ? set[Random.Range(0, set.Length)] : null;

        /// Riding into someone on the e-bike knocks them over (and costs the rider a wobble and some speed);
        /// on foot they shrug it off. People are scenery, never a crash.
        void PedestrianContacts()
        {
            if (!rider || rider.Traversal.Moving || rider.JumpHeight > 1.2f) return;
            var at = rider.transform.position;
            bool onFoot = DashRun.Instance && DashRun.Instance.OnFoot;
            bool bike = !onFoot; // the e-bike, or Bolt's magnetic wheel
            var list = Pedestrian.Active;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var person = list[i];
                if (!person || person.Reacting) continue;
                var p = person.transform.position;
                if (Mathf.Abs(p.z - at.z) > 0.9f || Mathf.Abs(p.x - at.x) > 0.6f || Mathf.Abs(p.y - at.y) > 1.2f) continue;
                if (bike) { person.KnockDown(at); rider.Wobble(); GameManager.Instance.Bonk(); }
                else person.ShrugOff(at);
            }
        }

        /// Residents for junctions and roofs come from the district's pool and go back to it when culled.
        Pedestrian RentPerson(Pedestrian template, Transform parent)
        {
            if (DistrictLife.Instance) return DistrictLife.Instance.Take(template, parent);
            var made = Instantiate(template, parent); made.template = template; return made;
        }

        static GameObject Block(Transform parent, Vector3 pos, Vector3 scale, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent);
            go.transform.localScale = scale;
            go.transform.position = pos;
            Paint(go, m);
            return go;
        }

        void SpawnLandmark(LandmarkDef def)
        {
            var go = def.prefab ? Instantiate(def.prefab) : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = def.displayName;
            go.transform.SetParent(transform);
            if (!def.prefab)
            {
                go.transform.localScale = new Vector3(6f, 30f, 6f);
                Paint(go, landmarkMaterial);
            }
            if (!def.prefab)
            {
                go.transform.position = new Vector3(def.side * 30f, 30f, def.atDistance);
                segments.Add(go);
                return;
            }
            // Wide landmarks (the bridge) run alongside the street; towers turn to face the oncoming rider.
            var size = RenderBounds(go).size;
            bool wide = size.x > size.z * 1.5f;
            go.transform.rotation = Quaternion.Euler(0f, wide ? 90f : 180f + def.side * 25f, 0f);
            var b = RenderBounds(go);
            // Rise from an open plaza right beside the sidewalk, with the shops cleared for its length.
            float x = def.side * (8.5f + b.extents.x);
            go.transform.position = new Vector3(x - (b.center.x - go.transform.position.x), 0f, def.atDistance);
            plazas.Add(new Vector3(def.side, def.atDistance - b.extents.z - 14f, def.atDistance + b.extents.z + 14f));
            segments.Add(go);
        }

        /// A segment is a couple of hundred primitives that never move: merge them by material into a few
        /// meshes so the street costs a handful of draw calls. Imported models (decor) are left as they are.
        static readonly Dictionary<Material, List<CombineInstance>> combineGroups = new();
        static readonly List<GameObject> combineBuffer = new();
        static void Combine(GameObject root)
        {
            combineGroups.Clear();
            combineBuffer.Clear();
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
            {
                if (!mf.sharedMesh || !mf.sharedMesh.isReadable || mf.sharedMesh.subMeshCount != 1) continue;
                var renderer = mf.GetComponent<MeshRenderer>();
                if (!renderer || !renderer.enabled || !renderer.sharedMaterial || renderer.HasPropertyBlock() || mf.GetComponent<TextMesh>()) continue;
                if (mf.GetComponentInParent<Pedestrian>() || mf.GetComponentInParent<LODGroup>() || mf.GetComponentInParent<Billboard>() || mf.GetComponentInParent<Bobber>() || mf.GetComponentInParent<Blinker>() || mf.GetComponentInParent<TrafficLight>()) continue;
                if (!combineGroups.TryGetValue(renderer.sharedMaterial, out var pieces))
                    combineGroups[renderer.sharedMaterial] = pieces = new List<CombineInstance>();
                pieces.Add(new CombineInstance { mesh = mf.sharedMesh, transform = root.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix });
                combineBuffer.Add(mf.gameObject);
            }
            if (combineBuffer.Count < 2) return;
            var lifetime = root.GetComponent<StreamedMeshLifetime>() ?? root.AddComponent<StreamedMeshLifetime>();
            foreach (var group in combineGroups)
            {
                int vertices = 0; foreach (var piece in group.Value) vertices += piece.mesh.vertexCount;
                var mesh = new Mesh { name = "Streamed city batch", indexFormat = vertices > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
                mesh.CombineMeshes(group.Value.ToArray(), true, true); mesh.RecalculateBounds();
                var batch = new GameObject("City batch / " + group.Key.name); batch.transform.SetParent(root.transform, false);
                batch.AddComponent<MeshFilter>().sharedMesh = mesh;
                batch.AddComponent<MeshRenderer>().sharedMaterial = group.Key;
                lifetime.meshes.Add(mesh);
            }
            foreach (var original in combineBuffer)
            {
                // Keep nested transforms and any children (signs, lights, animation anchors).
                var renderer = original.GetComponent<MeshRenderer>();
                if (renderer) { renderer.enabled = false; Destroy(renderer); }
                Destroy(original.GetComponent<MeshFilter>());
            }
        }

        bool InPlaza(float side, float z)
        {
            foreach (var p in plazas)
                if (p.x == side && z + segmentLength > p.y && z < p.z) return true;
            return false;
        }

        /// Open paved square with low planters in place of a shopfront, so the landmark behind reads clearly.
        void Plaza(Transform parent, float side, float z)
        {
            float mid = z + segmentLength / 2f;
            Block(parent, new Vector3(side * 30f, 0.05f, mid), new Vector3(46f, 0.3f, segmentLength), plazaMaterial);
            // Stone planters with rounded bushes along the plaza edge.
            for (float pz = 3f; pz < segmentLength; pz += 7f)
            {
                Block(parent, new Vector3(side * 8.2f, 0.45f, z + pz), new Vector3(1.4f, 0.55f, 2.4f), trimMaterial);
                foreach (float bz in new[] { -0.55f, 0.55f })
                {
                    var bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Destroy(bush.GetComponent<Collider>());
                    bush.transform.SetParent(parent);
                    bush.transform.position = new Vector3(side * 8.2f, 0.95f, z + pz + bz);
                    bush.transform.localScale = new Vector3(1.25f, 0.85f, 1.25f);
                    Paint(bush, foliageMaterial);
                }
            }
        }

        static Bounds RenderBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        void Register(GameObject go, TrackKind k, int lane, float half, int points, float oncoming)
        {
            var col = go.GetComponent<Collider>();
            if (col) Destroy(col);
            var t = go.GetComponent<TrackObject>() ?? go.AddComponent<TrackObject>();
            t.consumed = t.nearMiss = t.passed = t.heavy = t.struck = false;
            t.surface = RunSurface.Road; t.powerUp = -1; t.drift = t.meetZ = 0f; t.height = 0.8f;
            t.baseHeight = 0f;
            t.halfWidth = 0.85f; t.clearance = 1.4f;
            t.lateral = false; t.bike = false; t.coupon = false;
            t.kind = k;
            t.lane = lane;
            t.halfLength = half;
            t.points = points;
            t.oncoming = oncoming;
            live.Add(t);
        }

        /// Lane + distance overlap test against the rider, mirroring sim.js.
        void Resolve(float d)
        {
            bool magnet = PowerUps.Active(PowerUp.Magnet);
            if (GameManager.Instance.Paused) return;
            foreach (var o in live)
            {
                if (magnet && o && !o.consumed && !o.bike && !o.coupon && o.kind == TrackKind.Pickup && o.powerUp < 0 && Pull(o, d)) continue;
                if (!o || o.consumed || o.struck) continue;
                var contact = SurfaceTraversal.Point(o.surface, Tuning.LaneX[o.lane], 0f);
                // Route objects stand on a deck or a roof: measure the rider against that surface.
                if (o.surface == RunSurface.Road) contact.y = o.baseHeight;
                if (o.lateral) contact.x = o.transform.position.x;
                var localRider = Quaternion.Inverse(SurfaceTraversal.Orientation(o.surface)) * (rider.transform.position - contact);
                // Resolve in the object's contact frame. A ceiling chip cannot be collected from the road;
                // floor barriers do not mysteriously hit a rider on the opposite wall.
                bool hazard = o.kind == TrackKind.Low || o.kind == TrackKind.Block || o.kind == TrackKind.Overhead;
                if (hazard)
                {
                    float bottom = o.kind == TrackKind.Overhead ? o.clearance : 0f;
                    var box = new Bounds(new Vector3(0f, (bottom + o.height) * 0.5f, o.transform.position.z),
                        new Vector3(o.halfWidth * 2f, Mathf.Max(0.1f, o.height - bottom), o.halfLength * 2f));
                    Vector3 from = localRider;
                    if (d > 1f && Mathf.Abs(d - previousDistance) < GameManager.Instance.Speed * Time.deltaTime * 3f + 5f)
                        from = Quaternion.Inverse(SurfaceTraversal.Orientation(o.surface)) * (previousRiderPosition - contact);
                    var body = VehicleDynamics.Current;
                    if (!VehicleDynamics.Sweep(from, localRider, box, body.radius, body.length, VehicleDynamics.BodyHeight))
                    {
                        if (o.kind == TrackKind.Overhead && !o.passed && DashRun.Instance && DashRun.Instance.Slide > 0f && previousDistance <= o.transform.position.z && d > o.transform.position.z)
                        { o.passed = true; DashRun.Instance.Trick(1); }
                        if (o.surface == RunSurface.Road) CheckNearMiss(o, d); continue;
                    }
                }
                else if (localRider.y < -0.25f || localRider.y > 2.8f || Mathf.Abs(localRider.x) > 0.85f) continue;
                float halfBody = hazard ? VehicleDynamics.Current.length : Tuning.PlayerHalf;
                float near = Mathf.Min(previousDistance, d) - halfBody;
                float far = Mathf.Max(previousDistance, d) + halfBody;
                if (o.transform.position.z + o.halfLength < near || o.transform.position.z - o.halfLength > far) continue;

                switch (o.kind)
                {
                    case TrackKind.Pickup:
                        float pickupHeight = (Quaternion.Inverse(SurfaceTraversal.Orientation(o.surface)) * (o.transform.position - contact)).y;
                        if (Mathf.Abs(localRider.y + 0.8f - pickupHeight) < 1.15f) Collect(o);
                        break;
                    case TrackKind.Low:
                        // The bike's wheels have to be above the top of the cones or barrier.
                        Hit(o);
                        break;
                    case TrackKind.Overhead:
                        var rhythm = o.GetComponent<DashRhythmGate>();
                        if (rhythm && rhythm.Clear)
                        {
                            if (!o.passed) { o.passed = true; GameManager.Instance.AddBonus(40); DashRun.Instance?.Trick(6); }
                            break;
                        }
                        Hit(o);
                        break;
                    case TrackKind.Gap:
                        // Missing ground and moving platform tops are resolved by RiderController.
                        // A hole is not a fence that can be brushed aside by a one-hit shield.
                        break;
                    case TrackKind.Platform:
                        // Rooftop/ledge support only: RiderController decides when the rider is high
                        // enough to stand on it. Never a hit.
                        break;
                    case TrackKind.Block:
                        Hit(o);
                        break;
                }
            }
        }

        void Collect(TrackObject o)
        {
            o.consumed = true;
            if (o.bike) { DashRun.Instance?.Remount(); ClearBikeReservation(); o.gameObject.SetActive(false); return; }
            if (o.coupon) { TakeCoupon(o); return; }
            if (o.powerUp >= 0) PowerUps.Activate((PowerUp)o.powerUp);
            else
            {
                GameManager.Instance.AddPickup(o.points);
                Collected?.Invoke(o);
            }
            o.gameObject.SetActive(false);
        }

        const float MagnetReach = 16f;

        /// Magnet: burgers within reach ahead, in any lane, fly to the rider. True once handled here.
        bool Pull(TrackObject o, float d)
        {
            var p = o.transform.position;
            float ahead = p.z - d;
            if (ahead > MagnetReach || ahead < -1.5f) return false;
            if (o.surface != rider.Traversal.Surface || rider.Traversal.Moving) return false;
            var target = rider.transform.position;
            float step = (GameManager.Instance.Speed + 25f) * Time.deltaTime;
            p = Vector3.MoveTowards(p, target + rider.SurfaceNormal * 0.8f + Vector3.forward * 0.3f, step);
            o.transform.position = p;
            if (Vector3.Distance(p, target + rider.SurfaceNormal * 0.8f + Vector3.forward * 0.3f) < 0.6f) Collect(o);
            return true;
        }

        /// Dodging out of a lane just before its obstacle reaches you (scene 16).
        void CheckNearMiss(TrackObject o, float d)
        {
            if (o.kind == TrackKind.Pickup || o.kind == TrackKind.Platform || o.kind == TrackKind.Gap || o.consumed || o.nearMiss || o.lane != laneFrom) return;
            if (Time.time - laneChangedAt > 0.45f) return;
            if (Mathf.Abs(o.transform.position.z - d) > o.halfLength + Tuning.PlayerHalf) return;
            o.nearMiss = true;
            GameManager.Instance.AddNearMiss(NearMissBonus);
            Missions.Add(MissionStat.NearMisses, 1);
            NearMiss?.Invoke(o);
        }

        /// The Busted two-shot puts the camera a few metres ahead of the rider: hide whatever stands there.
        void ClearForBustShot()
        {
            float d = GameManager.Instance.Distance;
            foreach (var o in live)
                if (o && o.kind != TrackKind.Pickup && o.transform.position.z > d - 1f && o.transform.position.z < d + 10f)
                    o.gameObject.SetActive(false);
        }

        void Announce(float d)
        {
            if (landmarks == null || announceIndex >= landmarks.Length) return;
            if (landmarks[announceIndex].atDistance - d > 150f) return;
            LandmarkAhead?.Invoke(landmarks[announceIndex++]);
        }

        void Hit(TrackObject o)
        {
            // A physical lane change takes several frames. The same fence must not count as
            // another stumble before the bounce has carried the rider clear of its bounds.
            if (o.struck) return;
            o.struck = true;
            o.consumed = true;
            // Endless rules count only head-on impacts: the rider centred on the thing it hit. Clipping a
            // vehicle's side mid lane change is a bounce and a speed loss, never a wipe-out.
            bool headOn = Mathf.Abs(rider.transform.position.x - o.transform.position.x) < 0.85f;
            if (DashRun.Instance && DashRun.Instance.AbsorbHit())
            {
                if (o.heavy)
                {
                    o.consumed = false;
                    rider.BounceBack(SwerveLane(o));
                    GameManager.Instance.Bonk();
                }
                else o.gameObject.SetActive(false);
                Smashed?.Invoke(o);
                return;
            }
            if (PowerUps.Active(PowerUp.Rush))
            {
                // Sugar Rush: the obstacle goes flying and the run carries on.
                var thrown = o.GetComponent<Knockback>();
                if (thrown) thrown.enabled = true; else o.gameObject.AddComponent<Knockback>();
                Smashed?.Invoke(o);
                return;
            }
            // Swerving into the side of something that fills the lane: bounce back to the lane you came from
            // and leave it standing (only a stumble). Head on: it gets shoved over and the rider bonks.
            bool sideSwipe = o.kind == TrackKind.Block && laneFrom >= 0 && laneFrom != o.lane && Time.time - laneChangedAt < 0.3f;
            if (sideSwipe)
            {
                o.consumed = false;
                rider.BounceBack(laneFrom);
                lastLane = laneFrom;
            }
            else if (o.heavy)
            {
                // A van or a roadworks fence doesn't fall over: the rider clips it and swerves into the
                // clearest lane beside it, losing speed.
                int to = SwerveLane(o);
                // Never leave the rider inside a van: with both sides blocked, knock them out to the side anyway.
                if (to < 0) to = o.lane == 1 ? (rider.transform.position.x < 0f ? 0 : 2) : 1;
                o.consumed = false;
                if (to >= 0) { rider.BounceBack(to); lastLane = to; laneFrom = o.lane; }
                var shove = o.GetComponent<Shove>();
                if (shove) { shove.enabled = false; shove.enabled = true; } else o.gameObject.AddComponent<Shove>();
                GameManager.Instance.Bonk();
            }
            else
            {
                var topple = o.GetComponent<Topple>();
                if (topple) topple.enabled = true; else o.gameObject.AddComponent<Topple>();
                GameManager.Instance.Bonk();
            }
            GameManager.Instance.BreakCombo();
            Struck?.Invoke(o);
            rider.Wobble();
            // Endless run: the first crash knocks the rider off (or staggers Bolt); a second one before
            // recovering ends the run (DashRun.Crash). Side swipes only bounce.
            DashRun.LastStruck = $"{o.name} {o.kind} lane{o.lane} heavy={o.heavy} z={o.transform.position.z:0.0} headOn={headOn}";
            if (!headOn) Debug.Log("[DashCrash] side contact only: " + DashRun.LastStruck);
            if (!sideSwipe && headOn) DashRun.Instance?.Crash();
            if (chase) chase.Stumble();
        }

        /// The neighbouring lane with the most room ahead, or -1 when both are blocked right here.
        int SwerveLane(TrackObject from)
        {
            int best = -1;
            float bestRoom = 1.5f;
            float d = GameManager.Instance.Distance;
            foreach (int lane in new[] { from.lane - 1, from.lane + 1 })
            {
                if (lane < 0 || lane >= Tuning.Lanes) continue;
                float room = 60f;
                foreach (var o in live)
                {
                    if (!o || o.consumed || o.kind == TrackKind.Pickup || o.kind == TrackKind.Platform || o.lane != lane) continue;
                    float near = o.transform.position.z - o.halfLength - d;
                    float far = o.transform.position.z + o.halfLength - d;
                    if (far < -Tuning.PlayerHalf) continue;
                    room = Mathf.Min(room, Mathf.Max(0f, near));
                }
                if (room > bestRoom) { bestRoom = room; best = lane; }
            }
            return best;
        }

        void Cull(float d)
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                if (live[i] && live[i].transform.position.z > d - 15f) continue;
                if (live[i]) Pool.Release(live[i].gameObject);
                live.RemoveAt(i);
            }
            for (int i = segments.Count - 1; i >= 0; i--)
            {
                if (segments[i] && segments[i].transform.position.z > d - 60f) continue;
                if (segments[i]) { DistrictLife.Instance?.Return(segments[i]); Destroy(segments[i]); }
                segments.RemoveAt(i);
            }
        }

        ObstacleDef WeightedObstacle()
        {
            int total = 0;
            // Weight 0 retires a hurdle from the street mix (the slide-under scaffold, 11 Nov 2026: no ducking).
            foreach (var o in obstacles) total += Mathf.Max(0, o.weight);
            if (total <= 0) return obstacles[0];
            int r = Random.Range(0, total);
            foreach (var o in obstacles)
            {
                r -= Mathf.Max(0, o.weight);
                if (r < 0) return o;
            }
            return obstacles[0];
        }

        PickupDef WeightedPickup()
        {
            int total = 0;
            foreach (var p in pickups) total += Mathf.Max(1, p.weight);
            int r = Random.Range(0, total);
            foreach (var p in pickups)
            {
                r -= Mathf.Max(1, p.weight);
                if (r < 0) return p;
            }
            return pickups[0];
        }

        static void Paint(GameObject go, Material m)
        {
            if (m) go.GetComponent<Renderer>().sharedMaterial = m;
        }

        void Tint(GameObject go, Color c)
        {
            var r = go.GetComponent<Renderer>();
            if (tintMaterial) r.sharedMaterial = tintMaterial;
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_BaseColor", c);
            mpb.SetColor("_Color", c);
            r.SetPropertyBlock(mpb);
        }
    }
}
