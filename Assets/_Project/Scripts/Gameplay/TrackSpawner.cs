using System;
using System.Collections.Generic;
using DallasDash.Core;
using DallasDash.Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DallasDash.Gameplay
{
    /// Streams a straight, junction-free Christmas street ahead of the rider and fills it with rows of
    /// seasonal obstacles and burgers. Placeholder primitives stand in until the meshes from the asset pack are built.
    public class TrackSpawner : MonoBehaviour
    {
        public RiderController rider;
        public ChaseController chase;
        public PickupDef[] pickups;
        public ObstacleDef[] obstacles;
        public LandmarkDef[] landmarks;

        public float segmentLength = 20f;
        public float rowSpacing = 14f;
        public float firstRowAt = 40f;
        public Material roadMaterial, sidewalkMaterial, landmarkMaterial;
        /// Storefront dressing: brick facades, garland, string-light bulbs.
        public Material[] facadeMaterials;
        public Material garlandMaterial, bulbMaterial;
        /// Shared URP/Lit base for placeholder obstacles and pickups, tinted per object.
        /// Runtime primitives otherwise get the built-in default material, which is stripped from URP builds (renders magenta).
        public Material tintMaterial;
        public Material laneLineMaterial, curbMaterial, trimMaterial;
        public Material[] windowMaterials, awningMaterials;
        public Material lampPostMaterial, wreathMaterial, haloMaterial;
        public Material[] skylineMaterials;
        public Material doorMaterial, plazaMaterial;
        public Font signFont;
        /// FoodInc/TextCurved: lettering that bends with the street and sits on its board.
        public Material signTextMaterial;
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
        public GameObject kitLamp, kitTrash, kitUtility, kitBush;

        /// Sidewalk dressing (small trees, presents): spawned without hit tests.
        public GameObject[] decorPrefabs;

        /// Street stretches kept free of shops so a landmark can rise beside the road: side, from z, to z.
        readonly List<Vector3> plazas = new();
        public Color[] bulbColors;
        Material[] bulbMats;

        Material BulbMaterial(int i)
        {
            if (bulbColors == null || bulbColors.Length == 0 || !bulbMaterial) return bulbMaterial;
            if (bulbMats == null)
            {
                bulbMats = new Material[bulbColors.Length];
                for (int k = 0; k < bulbColors.Length; k++)
                {
                    bulbMats[k] = new Material(bulbMaterial) { name = "Bulb " + k };
                    bulbMats[k].SetColor("_BaseColor", bulbColors[k]);
                    bulbMats[k].SetColor("_EmissionColor", bulbColors[k] * 2.6f);
                }
            }
            return bulbMats[i % bulbMats.Length];
        }

        /// Raised for game feel (particles, sound, camera).
        public static event Action<TrackObject> Collected, Struck, NearMiss, Smashed;
        public static event Action<LandmarkDef> LandmarkAhead;
        public const int NearMissBonus = 50;

        RunState lastState;
        int lastLane = 1, laneFrom = -1, announceIndex;
        float laneChangedAt = -10f;

        /// Invented outlets only (pack, 24 Oct): real chain marks need a licence.
        static readonly string[] Outlets = { "CLUCK & CO", "SLICE HOUSE", "BURGER BARN", "DONUT DEPOT", "TAQUERIA SOL" };

        readonly List<TrackObject> live = new();
        public IReadOnlyList<TrackObject> Live => live;
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
            ResetTrack();
        }

        void ResetTrack()
        {
            pending.Clear();
            building = null;
            foreach (var o in live) if (o) Destroy(o.gameObject);
            foreach (var s in segments) if (s) Destroy(s);
            live.Clear();
            segments.Clear();
            builtTo = -segmentLength * 4f; // the intro starts back at the curb (StoryDirector.CurbZ)
            announceIndex = 0;
            plazas.Clear();
            nextRow = firstRowAt;
            nextPowerUp = FirstPowerUpAt;
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
                Resolve(d);
                Announce(d);
                Cull(d);
            }
            else lastLane = rider.Lane;
        }

        void Build(float d)
        {
            while (landmarkIndex < (landmarks?.Length ?? 0) && landmarks[landmarkIndex].atDistance < d + Tuning.FarEdge * 2f)
            {
                SpawnLandmark(landmarks[landmarkIndex++]);
            }
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
                SpawnRow(nextRow);
                nextRow += rowSpacing * Mathf.Lerp(1f, 1.6f, Mathf.InverseLerp(Tuning.SpeedBase, Tuning.SpeedMax, GameManager.Instance.Speed));
            }
        }

        System.Collections.IEnumerator SegmentSteps(GameObject root, float z)
        {
            var t = root.transform;
            var road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.transform.SetParent(t);
            road.transform.localScale = new Vector3(8f, 0.2f, segmentLength);
            road.transform.position = new Vector3(0f, -0.1f, z + segmentLength / 2f);
            Paint(road, roadMaterial);
            foreach (float side in new[] { -1f, 1f })
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
                    Block(t, new Vector3(lx, 0.005f, z + dz + 1.25f), new Vector3(0.12f, 0.02f, 2.5f), laneLineMaterial);
            RoadDetail(t, z);
            // String lights across the street every other segment.
            if (segmentIndex++ % 2 == 0) StringLights(t, z + segmentLength / 2f);
            Combine(root);
            yield return null;

            foreach (float side in new[] { -1f, 1f })
            {
                if (!root) yield break;
                // Each side gets its own batching root, so the merge cost is split across frames too.
                var sideRoot = new GameObject(side < 0f ? "Left" : "Right").transform;
                sideRoot.SetParent(t, false);
                if (InPlaza(side, z)) Plaza(sideRoot, side, z);
                else
                {
                    Storefront(sideRoot, side, z);
                    Skyline(sideRoot, side, z);
                }
                LampPost(sideRoot, side, z + 5f);
                LampPost(sideRoot, side, z + 15f);
                StreetTree(sideRoot, side, z + 10f);
                SidewalkDecor(sideRoot, side, z);
                Furniture(sideRoot, side, z);
                yield return null;
                if (!root) yield break;
                Combine(sideRoot.gameObject);
                yield return null;
            }
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

        void SpawnRow(float z)
        {
            // One lane is always left open so every row is passable.
            int open = Random.Range(0, Tuning.Lanes);
            bool token = z >= nextPowerUp;
            if (token) nextPowerUp = z + Random.Range(PowerUpGap * 0.75f, PowerUpGap * 1.25f);
            for (int lane = 0; lane < Tuning.Lanes; lane++)
            {
                if (token && lane == open) { SpawnPowerUp((PowerUp)Random.Range(0, PowerUps.Count), lane, z); continue; }
                if (lane == open || Random.value < 0.45f || obstacles == null || obstacles.Length == 0) { SpawnPickupLine(lane, z); continue; }
                SpawnObstacle(WeightedObstacle(), lane, z);
            }
        }

        void SpawnPickupLine(int lane, float z)
        {
            if (pickups == null || pickups.Length == 0 || Random.value < 0.4f) return;
            var def = WeightedPickup();
            for (int i = 0; i < 3; i++)
            {
                var go = def.prefab ? Instantiate(def.prefab) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = def.displayName;
                go.transform.SetParent(transform);
                go.transform.position = new Vector3(Tuning.LaneX[lane], def.prefab ? 0.45f : 0.8f, z + i * 2.5f);
                if (!def.prefab) { go.transform.localScale = Vector3.one * 0.7f; Tint(go, def.placeholderColor); }
                go.AddComponent<PickupSpin>();
                if (haloMaterial) Halo(go.transform);
                Register(go, TrackKind.Pickup, lane, 0.4f, def.points, 0f);
            }
        }

        /// First power-up token comes early so new players meet one; then roughly every PowerUpGap metres.
        const float FirstPowerUpAt = 140f, PowerUpGap = 360f;

        /// A glowing token in the power-up's colour with its emblem on the front: a horseshoe magnet,
        /// a lightning bolt, or "2X". It floats and bobs (no spin, so the emblem keeps facing the rider).
        void SpawnPowerUp(PowerUp kind, int lane, float z)
        {
            var root = new GameObject(PowerUps.Names[(int)kind]);
            root.transform.SetParent(transform);
            root.transform.position = new Vector3(Tuning.LaneX[lane], 1.05f, z);
            var c = PowerUps.Colors[(int)kind];
            var rim = Shape(root.transform, PrimitiveType.Cylinder, Vector3.zero, new Vector3(1.1f, 0.06f, 1.1f), new Color(1f, 0.85f, 0.3f), 0.6f);
            rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var disc = Shape(root.transform, PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.035f), new Vector3(0.92f, 0.06f, 0.92f), c, 0.8f);
            disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var white = new Color(1f, 0.97f, 0.9f);
            var steel = new Color(0.75f, 0.8f, 0.9f);
            var gold = new Color(1f, 0.88f, 0.25f);
            const float f = -0.1f; // the emblem stands proud of the disc, toward the rider
            switch (kind)
            {
                case PowerUp.Magnet:
                    Shape(root.transform, PrimitiveType.Cube, new Vector3(-0.17f, 0.04f, f), new Vector3(0.12f, 0.36f, 0.08f), white, 0.4f);
                    Shape(root.transform, PrimitiveType.Cube, new Vector3(0.17f, 0.04f, f), new Vector3(0.12f, 0.36f, 0.08f), white, 0.4f);
                    Shape(root.transform, PrimitiveType.Cube, new Vector3(0f, -0.17f, f), new Vector3(0.46f, 0.12f, 0.08f), white, 0.4f);
                    Shape(root.transform, PrimitiveType.Cube, new Vector3(-0.17f, 0.25f, f), new Vector3(0.12f, 0.1f, 0.09f), steel, 0.2f);
                    Shape(root.transform, PrimitiveType.Cube, new Vector3(0.17f, 0.25f, f), new Vector3(0.12f, 0.1f, 0.09f), steel, 0.2f);
                    break;
                case PowerUp.Rush:
                    Shape(root.transform, PrimitiveType.Cube, new Vector3(0.05f, 0.14f, f), new Vector3(0.1f, 0.34f, 0.08f), gold, 0.6f).transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
                    Shape(root.transform, PrimitiveType.Cube, new Vector3(0f, 0f, f), new Vector3(0.3f, 0.09f, 0.08f), gold, 0.6f).transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
                    Shape(root.transform, PrimitiveType.Cube, new Vector3(-0.05f, -0.15f, f), new Vector3(0.1f, 0.34f, 0.08f), gold, 0.6f).transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
                    break;
                default:
                    var label = new GameObject("2X", typeof(TextMesh));
                    label.transform.SetParent(root.transform, false);
                    label.transform.localPosition = new Vector3(0f, 0.02f, f);
                    var tm = label.GetComponent<TextMesh>();
                    tm.text = "2X";
                    tm.anchor = TextAnchor.MiddleCenter;
                    tm.characterSize = 0.075f;
                    tm.fontSize = 64;
                    tm.color = white;
                    if (signFont)
                    {
                        tm.font = signFont;
                        label.GetComponent<MeshRenderer>().sharedMaterial = signTextMaterial ? signTextMaterial : signFont.material;
                    }
                    break;
            }
            var spin = root.AddComponent<PickupSpin>();
            spin.degreesPerSecond = 0f;
            spin.bob = 0.12f;
            if (haloMaterial) Halo(root.transform);
            Register(root, TrackKind.Pickup, lane, 0.6f, 0, 0f);
            live[live.Count - 1].powerUp = (int)kind;
        }

        /// A tinted, slightly glowing primitive part (power-up tokens).
        GameObject Shape(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Color c, float glow)
        {
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

        void SpawnObstacle(ObstacleDef def, int lane, float z)
        {
            var go = def.prefab ? Instantiate(def.prefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = def.displayName;
            go.transform.SetParent(transform);
            // High obstacles hang overhead with a roll-under gap; everything else sits on the road.
            float y = def.kind == TrackKind.High ? 1.6f : (def.prefab ? 0f : def.size.y / 2f);
            go.transform.position = new Vector3(Tuning.LaneX[lane], y, z);
            if (!def.prefab)
            {
                go.transform.localScale = def.size;
                Tint(go, def.placeholderColor);
            }
            if (def.id == "shopper") go.AddComponent<Bobber>();
            Register(go, def.kind, lane, def.size.z / 2f, 0, 0f);
        }

        /// One shopfront per side per segment: brick facade, cream cornice, two rows of upper windows,
        /// a lit shop window under a striped awning, garland, and an invented outlet sign on some.
        /// One shopfront per side per segment: brick facade, cream cornice, rows of upper windows (some lit),
        /// a lit shop window under a striped awning, a framed door, garland, and a sign board in one of the
        /// pack's styles with an invented outlet name.
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

            // Garland along the shopfront.
            Block(parent, new Vector3(side * 6.9f, 3.9f, mid), new Vector3(0.4f, 0.35f, len - 0.4f), garlandMaterial);

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
            Block(parent, new Vector3(side * 6.86f, y, cz), new Vector3(0.16f, 1.3f, 6.4f), trimMaterial).name = "SignFrame";
            Block(parent, new Vector3(side * 6.8f, y, cz), new Vector3(0.12f, 1.1f, 6.1f), boardMat).name = "SignBoard";
            var sign = new GameObject("Sign", typeof(TextMesh));
            sign.transform.SetParent(parent);
            sign.transform.position = new Vector3(side * 6.72f, y, cz);
            sign.transform.rotation = Quaternion.Euler(0f, side > 0 ? 90f : -90f, 0f);
            var tm = sign.GetComponent<TextMesh>();
            tm.text = Outlets[Random.Range(0, Outlets.Length)];
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

        /// A shopfront assembled from the modular kit on a 2 m grid: nine columns of lit shop windows and a glazed
        /// door on the ground floor, a sign band, two to four upper floors of windows alternating with plain wall,
        /// and a cornice, in one of three styles (red brick, painted plaster, metal and glass). The awning, sign
        /// board, garland, pilasters and fire escape are layered on as before.
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
            Block(parent, new Vector3(side * 6.86f, 3.62f, mid), new Vector3(0.3f, 0.3f, len - 0.4f), garlandMaterial);
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
        /// the bottom, and a row of seasonal goods sits on it: wrapped presents, stacked boxes, a little tree, baubles.
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
                if (r < 0.45f)
                {
                    // A wrapped present with a ribbon band.
                    float sz = Random.Range(0.22f, 0.4f);
                    var box = Block(parent, new Vector3(itemX, 0.71f + sz / 2f, zz), new Vector3(sz, sz, sz), Pick(awningMaterials));
                    box.transform.rotation = Quaternion.Euler(0f, Random.Range(-20f, 20f), 0f);
                    Block(parent, new Vector3(itemX, 0.71f + sz / 2f, zz), new Vector3(sz + 0.02f, sz + 0.02f, 0.06f), trimMaterial);
                }
                else if (r < 0.7f)
                {
                    // Two stacked boxes.
                    Block(parent, new Vector3(itemX, 0.86f, zz), new Vector3(0.36f, 0.3f, 0.36f), trimMaterial);
                    Block(parent, new Vector3(itemX, 1.12f, zz), new Vector3(0.24f, 0.22f, 0.24f), Pick(awningMaterials));
                }
                else if (r < 0.85f && wreathMaterial)
                {
                    // A little potted tree.
                    Block(parent, new Vector3(itemX, 0.8f, zz), new Vector3(0.18f, 0.18f, 0.18f), Pick(awningMaterials, 0));
                    Block(parent, new Vector3(itemX, 1.02f, zz), new Vector3(0.34f, 0.26f, 0.34f), wreathMaterial).transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                    Block(parent, new Vector3(itemX, 1.24f, zz), new Vector3(0.24f, 0.22f, 0.24f), wreathMaterial).transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                    Block(parent, new Vector3(itemX, 1.42f, zz), new Vector3(0.12f, 0.16f, 0.12f), wreathMaterial).transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                }
                else
                {
                    // Baubles.
                    for (int k = 0; k < 3; k++)
                    {
                        var b = Block(parent, new Vector3(itemX, 0.83f, zz + (k - 1) * 0.18f), Vector3.one * 0.13f, BulbMaterial(Random.Range(0, 8)));
                        b.transform.rotation = Quaternion.Euler(45f, 0f, 45f);
                    }
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
            if (!chimney) chimney = Pick(facadeMaterials);
            float roofX = side * 9.4f;
            int units = Random.Range(1, 3);
            for (int i = 0; i < units; i++)
            {
                float uz = mid + Random.Range(-len * 0.35f, len * 0.35f);
                Block(parent, new Vector3(roofX + Random.Range(-1f, 1f), h + 0.5f, uz), new Vector3(1.3f, 0.9f, 1.1f), trimMaterial);
                Block(parent, new Vector3(roofX, h + 0.98f, uz), new Vector3(0.9f, 0.08f, 0.9f), lampPostMaterial);
            }
            float r = Random.value;
            if (r < 0.25f && doorMaterial)
            {
                // Wooden water tower on legs, the downtown-roof classic.
                float tz = mid + Random.Range(-3f, 3f);
                foreach (float lx in new[] { -0.6f, 0.6f })
                foreach (float lz in new[] { -0.6f, 0.6f })
                    Block(parent, new Vector3(roofX + lx, h + 0.9f, tz + lz), new Vector3(0.12f, 1.8f, 0.12f), lampPostMaterial);
                var tank = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(tank.GetComponent<Collider>());
                tank.transform.SetParent(parent);
                tank.transform.position = new Vector3(roofX, h + 2.7f, tz);
                tank.transform.localScale = new Vector3(1.9f, 1f, 1.9f);
                Paint(tank, doorMaterial);
                var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(cap.GetComponent<Collider>());
                cap.transform.SetParent(parent);
                cap.transform.position = new Vector3(roofX, h + 3.7f, tz);
                cap.transform.localScale = new Vector3(2f, 0.9f, 2f);
                Paint(cap, lampPostMaterial);
            }
            else if (r < 0.55f)
                Block(parent, new Vector3(side * 8.2f, h + 1f, mid + Random.Range(-len * 0.4f, len * 0.4f)), new Vector3(0.8f, 2f, 0.8f), chimney);

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

        /// Christmas trees and stacked presents on the sidewalk, set dressing only (not obstacles).
        void SidewalkDecor(Transform parent, float side, float z)
        {
            if (decorPrefabs == null || decorPrefabs.Length == 0 || Random.value > 0.45f) return;
            var prefab = decorPrefabs[Random.Range(0, decorPrefabs.Length)];
            if (!prefab) return;
            var go = Instantiate(prefab, parent);
            go.transform.position = new Vector3(side * 5.9f, 0.2f, z + Random.Range(3f, segmentLength - 3f));
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            go.transform.localScale = Vector3.one * Random.Range(0.55f, 0.75f);
        }

        /// Black Victorian-style street lamp with a wreath and red bow, as along the pack's Christmas street.
        void LampPost(Transform parent, float side, float z)
        {
            float x = side * 4.6f;
            if (kitLamp)
            {
                // Cast-iron street lamp, its arm reaching out over the road.
                Prop(parent, kitLamp, new Vector3(x, SidewalkTop, z), side > 0 ? 90f : -90f, 1.4f);
                LampWreath(parent, side, x, z);
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
            LampWreath(parent, side, x, z);
        }

        /// Wreath on the lamp post: a green disc facing the street, with a red bow.
        void LampWreath(Transform parent, float side, float x, float z)
        {
            var wreath = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(wreath.GetComponent<Collider>());
            wreath.transform.SetParent(parent);
            wreath.transform.position = new Vector3(x - side * 0.12f, 3.6f, z);
            wreath.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            wreath.transform.localScale = new Vector3(0.75f, 0.06f, 0.75f);
            Paint(wreath, wreathMaterial);
            Block(parent, new Vector3(x - side * 0.2f, 3.32f, z), new Vector3(0.12f, 0.18f, 0.34f), awningMaterials != null && awningMaterials.Length > 0 ? awningMaterials[0] : null);
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

        void StringLights(Transform parent, float z)
        {
            // A dark wire sagging between the facades, small bulbs hanging just under it. Without the wire the
            // bloomed bulbs read as loose glowing balls floating over the street.
            const int bulbs = 15;
            Vector3 Wire(float t) => new(Mathf.Lerp(-6.9f, 6.9f, t), 6.3f - 4f * t * (1f - t) * 1.1f, z);
            const int links = 12;
            for (int i = 0; i < links; i++)
            {
                Vector3 a = Wire(i / (float)links), c = Wire((i + 1) / (float)links);
                var w = Block(parent, (a + c) / 2f, new Vector3(0.035f, 0.035f, Vector3.Distance(a, c) + 0.02f), lampPostMaterial);
                w.transform.rotation = Quaternion.LookRotation(c - a);
            }
            for (int i = 1; i < bulbs - 1; i++)
            {
                float t = i / (float)(bulbs - 1);
                // A tiny elongated diamond (8 triangles) reads as a glowing bulb up close and far away;
                // spheres cost 768 triangles each, and cubes looked like boxes when the camera passed under.
                var b = new GameObject("Bulb", typeof(MeshFilter), typeof(MeshRenderer));
                b.GetComponent<MeshFilter>().sharedMesh = BulbMesh;
                b.transform.SetParent(parent);
                b.transform.position = Wire(t) + Vector3.down * 0.12f;
                b.transform.localScale = new Vector3(0.11f, 0.17f, 0.11f);
                // Alternate red, green, gold and warm white. One shared material per colour (not property blocks),
                // so the bulbs batch with each other.
                Paint(b, BulbMaterial(i));
            }
        }

        static Mesh bulbMesh;

        /// Octahedron, unit size, pointed top and bottom: the string-light bulb.
        static Mesh BulbMesh
        {
            get
            {
                if (bulbMesh) return bulbMesh;
                var v = new[] { Vector3.up * 0.5f, Vector3.down * 0.5f, new Vector3(0.5f, 0f, 0f), new Vector3(0f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0f), new Vector3(0f, 0f, -0.5f) };
                var tris = new[] { 0, 3, 2, 0, 4, 3, 0, 5, 4, 0, 2, 5, 1, 2, 3, 1, 3, 4, 1, 4, 5, 1, 5, 2 };
                bulbMesh = new Mesh { name = "Bulb", vertices = v, triangles = tris };
                bulbMesh.RecalculateNormals();
                bulbMesh.RecalculateBounds();
                return bulbMesh;
            }
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
        static readonly List<GameObject> combineBuffer = new();
        static void Combine(GameObject root)
        {
            combineBuffer.Clear();
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
                if (mf.sharedMesh && mf.sharedMesh.isReadable && mf.transform.parent == root.transform) combineBuffer.Add(mf.gameObject);
            if (combineBuffer.Count > 1) StaticBatchingUtility.Combine(combineBuffer.ToArray(), root);
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
                    Paint(bush, wreathMaterial ? wreathMaterial : garlandMaterial);
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
            var t = go.AddComponent<TrackObject>();
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
            foreach (var o in live)
            {
                if (magnet && o && !o.consumed && o.kind == TrackKind.Pickup && o.powerUp < 0 && Pull(o, d)) continue;
                if (o && o.lane != rider.Lane) { CheckNearMiss(o, d); continue; }
                if (!o || o.consumed) continue;
                float dz = Mathf.Abs(o.transform.position.z - d);
                if (dz > o.halfLength + Tuning.PlayerHalf) continue;

                switch (o.kind)
                {
                    case TrackKind.Pickup:
                        Collect(o);
                        break;
                    case TrackKind.Low:
                        if (rider.transform.position.y < 0.8f) Hit(o);
                        break;
                    case TrackKind.High:
                        if (!rider.Rolling) Hit(o);
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
            var target = rider.transform.position;
            float step = (GameManager.Instance.Speed + 25f) * Time.deltaTime;
            p.x = Mathf.MoveTowards(p.x, target.x, step * 0.6f);
            p.z = Mathf.MoveTowards(p.z, target.z + 0.3f, step);
            o.transform.position = p;
            if (Mathf.Abs(p.x - target.x) < 0.5f && Mathf.Abs(p.z - target.z - 0.3f) < 0.6f) Collect(o);
            return true;
        }

        /// Dodging out of a lane just before its obstacle reaches you (scene 16).
        void CheckNearMiss(TrackObject o, float d)
        {
            if (o.kind == TrackKind.Pickup || o.consumed || o.nearMiss || o.lane != laneFrom) return;
            if (Time.time - laneChangedAt > 0.45f) return;
            if (Mathf.Abs(o.transform.position.z - d) > o.halfLength + Tuning.PlayerHalf) return;
            o.nearMiss = true;
            GameManager.Instance.AddBonus(NearMissBonus);
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
            o.consumed = true;
            if (PowerUps.Active(PowerUp.Rush))
            {
                // Sugar Rush: the obstacle goes flying and the run carries on.
                o.gameObject.AddComponent<Knockback>();
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
            else
            {
                o.gameObject.AddComponent<Topple>();
                GameManager.Instance.Bonk();
            }
            Struck?.Invoke(o);
            rider.Wobble();
            if (chase) chase.Stumble();
            else GameManager.Instance.Bust();
        }

        void Cull(float d)
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                if (live[i] && live[i].transform.position.z > d - 15f) continue;
                if (live[i]) Destroy(live[i].gameObject);
                live.RemoveAt(i);
            }
            for (int i = segments.Count - 1; i >= 0; i--)
            {
                if (segments[i] && segments[i].transform.position.z > d - 60f) continue;
                if (segments[i]) Destroy(segments[i]);
                segments.RemoveAt(i);
            }
        }

        ObstacleDef WeightedObstacle()
        {
            int total = 0;
            foreach (var o in obstacles) total += Mathf.Max(1, o.weight);
            int r = Random.Range(0, total);
            foreach (var o in obstacles)
            {
                r -= Mathf.Max(1, o.weight);
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
