using DallasDash.Core;
using DallasDash.Data;
using UnityEngine;

namespace DallasDash.Gameplay
{
    // Real cross streets (client): the block rows break for a junction with a cross road, corner kerbs, zebra
    // crossings, signals on mast arms and street-name blades. Cross traffic (taxis and cars from the hurdle
    // set) drives through the junction, timed so it is crossing the lanes as the rider arrives.
    public partial class TrackSpawner
    {
        /// Junction segments in each 960 m cycle: clear of both rooftop shortcuts (100-340 m and 580-820 m with
        /// their approaches and landings) and of the kiosks at 380 and 860 m.
        static readonly float[] JunctionOffsets = { 60f, 480f, 900f };
        const float CrossRoadHalf = 7f, CrossLength = 70f;

        /// True for the 20 m segment starting at z when it is a junction (street districts only).
        public static bool Junction(float segmentStart)
        {
            if (DashRun.District == 1 || segmentStart < 40f) return false;
            float local = Mathf.Repeat(segmentStart, 960f);
            foreach (float j in JunctionOffsets) if (Mathf.Abs(local - j) < 0.5f) return true;
            return false;
        }

        /// The junction's footprint (with its approach): street hurdles stay out so cross traffic is the test.
        public static bool NearJunction(float z)
        {
            float seg = Mathf.Floor(z / 20f) * 20f;
            return Junction(seg) || Junction(seg - 20f) && Mathf.Repeat(z, 20f) < 4f || Junction(seg + 20f) && Mathf.Repeat(z, 20f) > 12f;
        }

        void BuildJunction(Transform t, float z)
        {
            float mid = z + 10f;
            var paint = DashCity.Mat(this, "junctionPaint", new Color(0.94f, 0.93f, 0.86f), 0.05f);
            var steel = DashCity.Mat(this, "junctionSteel", new Color(0.2f, 0.22f, 0.25f));
            var housing = DashCity.Mat(this, "junctionHousing", new Color(0.05f, 0.05f, 0.06f));
            var red = DashCity.Mat(this, "signalRed", new Color(1f, 0.12f, 0.1f), 2.6f);
            var green = DashCity.Mat(this, "signalGreen", new Color(0.15f, 1f, 0.45f), 2.6f);
            var unlit = DashCity.Mat(this, "signalOff", new Color(0.12f, 0.12f, 0.12f));
            var blade = DashCity.Mat(this, "streetBlade", new Color(0.05f, 0.4f, 0.22f));
            // Cross road out to both sides, its centre line, and corner kerbs.
            foreach (float side in new[] { -1f, 1f })
            {
                Block(t, new Vector3(side * (4f + CrossLength * 0.5f), -0.1f, mid), new Vector3(CrossLength, 0.2f, CrossRoadHalf * 2f), roadMaterial);
                for (float x = 9f; x < CrossLength; x += 5f)
                    Block(t, new Vector3(side * x, 0.005f, mid), new Vector3(2.5f, 0.02f, 0.12f), laneLineMaterial);
                foreach (float cz in new[] { z + 1.5f, z + 18.5f })
                {
                    var corner = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Destroy(corner.GetComponent<Collider>());
                    corner.transform.SetParent(t);
                    corner.transform.position = new Vector3(side * (4f + CrossLength * 0.5f), 0f, cz);
                    corner.transform.localScale = new Vector3(CrossLength, 0.4f, 3f);
                    Paint(corner, sidewalkMaterial);
                }
                // Zebra crossing over the cross road, along the pavement line.
                for (int i = 0; i < 7; i++)
                    Block(t, new Vector3(side * 5.6f, 0.012f, z + 4f + i * 2f), new Vector3(2.4f, 0.012f, 0.9f), paint);
            }
            // Zebra crossings over the main street on both sides of the junction, with stop lines.
            foreach (float cz in new[] { z + 1.6f, z + 18.4f })
                for (int i = 0; i < 8; i++)
                    Block(t, new Vector3(-3.65f + i * 1.04f, 0.012f, cz), new Vector3(0.58f, 0.012f, 2.4f), paint);
            Block(t, new Vector3(0f, 0.013f, z - 0.2f), new Vector3(8f, 0.012f, 0.3f), paint);
            // Signals: a pole on each corner with a mast arm over the lanes. The rider's heads are red (this
            // is a courier running the light); the cross street's are green.
            foreach (float side in new[] { -1f, 1f })
                foreach (float cz in new[] { z + 0.6f, z + 19.4f })
                {
                    bool near = cz < mid;
                    Block(t, new Vector3(side * 4.55f, 2.6f, cz), new Vector3(0.18f, 5.2f, 0.18f), steel);
                    Block(t, new Vector3(side * 2.6f, 5.05f, cz), new Vector3(3.9f, 0.14f, 0.14f), steel);
                    float hx = side * 1.2f;
                    Block(t, new Vector3(hx, 4.45f, cz), new Vector3(0.38f, 1.05f, 0.32f), housing);
                    DashCity.Part(t, new Vector3(hx, 4.8f, cz - (near ? 0.17f : -0.17f)), Vector3.one * 0.2f, near ? red : unlit, PrimitiveType.Sphere);
                    DashCity.Part(t, new Vector3(hx, 4.45f, cz - (near ? 0.17f : -0.17f)), Vector3.one * 0.2f, unlit, PrimitiveType.Sphere);
                    DashCity.Part(t, new Vector3(hx, 4.1f, cz - (near ? 0.17f : -0.17f)), Vector3.one * 0.2f, unlit, PrimitiveType.Sphere);
                    // The cross street's own head on the pole, facing along it.
                    Block(t, new Vector3(side * 4.55f, 3.4f, cz + (near ? 0.25f : -0.25f)), new Vector3(0.32f, 0.9f, 0.3f), housing);
                    DashCity.Part(t, new Vector3(side * 4.55f + side * -0.17f, 3.1f, cz + (near ? 0.25f : -0.25f)), Vector3.one * 0.18f, green, PrimitiveType.Sphere);
                    // Street-name blades.
                    Block(t, new Vector3(side * 4.55f, 2.9f, cz + (near ? -0.5f : 0.5f)), new Vector3(1.3f, 0.3f, 0.05f), blade);
                }
            string[] streets = { "MARKET ST", "5TH AVE", "BAYVIEW BLVD", "UNION ST", "ELM ST", "HARBOR WAY" };
            string name = streets[Mathf.Abs(Mathf.RoundToInt(z / 20f)) % streets.Length];
            foreach (float side in new[] { -1f, 1f })
                DashCity.Text(this, t, name, new Vector3(side * 4.55f, 2.9f, z + 0.03f), 0.022f, Color.white);
        }

        PeopleCatalogue junctionPeople;

        /// The crew's own e-bikes (Ace orange, Nova purple), for parking after a crash; set by the bootstrap.
        public GameObject[] parkedBikes;
        /// The stretch kept clear for the parked bike: no new hurdles spawn in its lane there.
        public float BikeReservedFrom { get; private set; } = -1f;
        public float BikeReservedTo { get; private set; } = -1f;
        public int BikeReservedLane { get; private set; } = -1;
        GameObject parkedBike;

        /// A lane with nothing in it for 12 m either side of z (and no cross traffic, no moving couriers near).
        public bool LaneFree(int lane, float z)
        {
            foreach (var o in live)
            {
                if (!o || !o.gameObject.activeInHierarchy || o.consumed || o.kind == TrackKind.Pickup || o.surface != RunSurface.Road) continue;
                float dz = Mathf.Abs(o.transform.position.z - z);
                if (o.lateral && dz < 12f) return false;
                if (o.drift > 0f && o.lane == lane && dz < 30f) return false;
                if (o.lane == lane && dz < 12f + o.halfLength) return false;
            }
            return true;
        }

        /// After a crash the rider's e-bike stands parked on its kickstand in a free lane, under a tall light
        /// beacon so it reads from far off. Riding into it grabs it.
        public void ParkBike(float z, int lane)
        {
            if (parkedBike) Destroy(parkedBike);
            int character = Mathf.Clamp(PlayerProfile.Character, 0, 1);
            var model = parkedBikes != null && parkedBikes.Length > character ? parkedBikes[character] : null;
            if (!model) return;
            var root = new GameObject("Parked e-bike / grab it");
            root.transform.SetParent(transform);
            root.transform.position = new Vector3(Tuning.LaneX[lane], 0f, z);
            var bike = Instantiate(model, root.transform);
            bike.transform.localPosition = Vector3.zero;
            bike.transform.localRotation = Quaternion.Euler(0f, 0f, 8f); // leaning on its kickstand
            var cyan = DashCity.Mat(this, "roofNeonCyan", new Color(0.1f, 0.85f, 1f), 2.2f);
            var beam = DashCity.Mat(this, "bikeBeacon", new Color(0.2f, 0.95f, 1f), 3.5f);
            DashCity.Part(root.transform, root.transform.position + new Vector3(0f, 0.02f, 0f), new Vector3(2.1f, 0.02f, 2.6f), cyan, PrimitiveType.Cylinder).name = "Pickup glow";
            DashCity.Part(root.transform, root.transform.position + new Vector3(0f, 4.2f, 0f), new Vector3(0.18f, 4f, 0.18f), beam, PrimitiveType.Cylinder).name = "Beacon";
            DashCity.Text(this, root.transform, "YOUR E-BIKE", root.transform.position + new Vector3(0f, 2.4f, 0f), 0.05f, Color.white);
            Register(root, TrackKind.Pickup, lane, 1.1f, 0, 0f);
            live[live.Count - 1].bike = true;
            parkedBike = root;
            BikeReservedLane = lane; BikeReservedFrom = z - 14f; BikeReservedTo = z + 14f;
        }

        public void ClearBikeReservation() { BikeReservedLane = -1; BikeReservedFrom = BikeReservedTo = -1f; }

        /// People crossing on the zebras over the cross road, and a few waiting on the corners.
        void JunctionPeople(Transform t, float z)
        {
            if (!junctionPeople) junctionPeople = Resources.Load<PeopleCatalogue>("PeopleCatalogue");
            if (!junctionPeople || junctionPeople.bodies == null || junctionPeople.bodies.Length == 0) return;
            int seed = Mathf.Abs(Mathf.RoundToInt(z));
            for (int i = 0; i < 6; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                bool crossing = i < 4;
                var person = RentPerson(junctionPeople.bodies[(seed + i) % junctionPeople.bodies.Length], t);
                float pz = crossing ? z + 4f + (i * 3.1f) % 12f : z + (i == 4 ? 1.2f : 18.8f);
                float px = crossing ? side * (5.1f + (i / 2) * 0.9f) : side * 5.6f;
                person.transform.position = new Vector3(px, 0f, pz);
                person.transform.rotation = Quaternion.Euler(0f, crossing ? (i / 2 == 0 ? 0f : 180f) : side < 0 ? 90f : -90f, 0f);
                person.Dress(junctionPeople, seed * 7 + i, crossing ? Pedestrian.Walk : Pedestrian.Chat);
                if (crossing) person.gameObject.AddComponent<LifeStroller>().Set(z + 3.5f, z + 16.5f, 0.11f + i * 0.01f);
            }
            // People crossing the main street on its zebras, kerb to kerb, clearing the lanes for riders.
            for (int i = 0; i < 4; i++)
            {
                var person = RentPerson(junctionPeople.bodies[(seed + i + 1) % junctionPeople.bodies.Length], t);
                float cz = i < 2 ? z + 1.6f : z + 18.4f;
                float side = i % 2 == 0 ? -1f : 1f;
                person.transform.position = new Vector3(side * (4.6f + i * 0.3f), 0f, cz + (i % 2) * 0.5f);
                person.Dress(junctionPeople, seed * 11 + i, Pedestrian.Walk);
                person.gameObject.AddComponent<StreetCrosser>().Begin(-side, (seed + i) % 3 * 1.3f);
            }
        }

        /// Two cars through the junction, one each way, timed to reach a lane as the rider gets there.
        void SpawnCrossTraffic(float z)
        {
            if (obstacles == null) return;
            ObstacleDef car = null, taxi = null;
            foreach (var def in obstacles)
            {
                if (!def || !def.prefab) continue;
                if (def.id == "slow-car") car = def;
                if (def.id == "taxi") taxi = def;
            }
            int seed = Mathf.Abs(Mathf.RoundToInt(z / 20f));
            for (int k = 0; k < 2; k++)
            {
                var def = k == 0 ? (taxi ? taxi : car) : (car ? car : taxi);
                if (!def) return;
                float dir = k == 0 ? -1f : 1f;           // first from the right, second from the left
                float laneZ = z + 10f + (k == 0 ? -3.2f : 3.2f);
                var go = Pool.Rent("cross-" + def.GetInstanceID(), () => Instantiate(def.prefab), new Vector3(-dir * 45f, 0f, laneZ), Quaternion.Euler(0f, dir * 90f, 0f));
                go.name = "Cross traffic / " + def.displayName;
                Register(go, TrackKind.Block, 1, 1.0f, 0, 0f);
                var o = live[live.Count - 1];
                o.lateral = true; o.heavy = true; o.height = def.size.y;
                o.halfWidth = def.size.z * 0.5f;
                var drive = go.GetComponent<CrossTraffic>() ?? go.AddComponent<CrossTraffic>();
                drive.Setup(o, dir, 13f, Tuning.LaneX[(seed + k * 2) % Tuning.Lanes], laneZ);
            }
        }
    }

    /// A pedestrian crossing the main street: steps off the kerb when the way is clear, walks across, waits a
    /// while on the far kerb and crosses back; if a rider comes close while they are in the road they hurry on.
    public class StreetCrosser : MonoBehaviour
    {
        float dir, wait;
        Animator animator;
        const float Kerb = 4.6f;

        public void Begin(float direction, float delay) { dir = direction; wait = delay; animator = GetComponentInChildren<Animator>(); }

        void Update()
        {
            var gm = GameManager.Instance;
            if (!gm || gm.Paused) return;
            var p = transform.position;
            float ahead = p.z - gm.Distance;
            bool inRoad = Mathf.Abs(p.x) < Kerb - 0.05f;
            bool riderClose = gm.State == RunState.Running && ahead > -2f && ahead < 30f;
            if (!inRoad && (wait -= Time.deltaTime) > 0f) { Face(-Mathf.Sign(p.x)); return; }
            if (!inRoad && riderClose) { Face(-Mathf.Sign(p.x)); return; }
            float pace = inRoad && riderClose ? 3.2f : 1.35f;
            if (animator) animator.speed = pace > 2f ? 1.8f : 1f;
            p.x += dir * pace * Time.deltaTime;
            transform.position = p;
            Face(dir);
            if (Mathf.Abs(p.x) >= Kerb && Mathf.Sign(p.x) == dir) { dir = -dir; wait = 2.5f + Mathf.Abs(p.z) % 3f; }
        }

        void Face(float d) => transform.rotation = Quaternion.Euler(0f, d > 0f ? 90f : -90f, 0f);
    }

    /// Drives a cross-traffic car along the cross street. It waits off-screen and pulls away so that it is in
    /// `targetX` (a lane) just as the rider reaches its line, then carries on through.
    public class CrossTraffic : MonoBehaviour
    {
        TrackObject body;
        float dir, speed, targetX, lineZ;
        bool rolling;

        public void Setup(TrackObject o, float direction, float pace, float meetX, float z)
        {
            body = o; dir = direction; speed = pace; targetX = meetX; lineZ = z; rolling = false;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (!gm || gm.Paused || body == null) return;
            var p = transform.position;
            if (!rolling && gm.State == RunState.Running)
            {
                float riderEta = (lineZ - gm.Distance) / Mathf.Max(1f, gm.Speed);
                float carEta = Mathf.Abs(targetX - p.x) / speed;
                if (riderEta <= carEta) rolling = true;
            }
            if (rolling) p.x += dir * speed * Time.deltaTime;
            transform.position = p;
            // Lane bookkeeping for near misses, swerves and the bots; the hit box follows the real x.
            float best = float.MaxValue;
            for (int i = 0; i < Tuning.Lanes; i++)
                if (Mathf.Abs(Tuning.LaneX[i] - p.x) < best) { best = Mathf.Abs(Tuning.LaneX[i] - p.x); body.lane = i; }
        }
    }
}
