using System.Collections.Generic;
using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// One US festival observance on the Festivals course. The name and date are shown on the
    /// overhead banner, and the motif index selects a small decoration set.
    public struct FestivalDef
    {
        public readonly string name;
        public readonly string date;
        public readonly string motif;
        public readonly Color color;
        public readonly Color accent;

        public FestivalDef(string name, string date, string motif, Color color, Color accent)
        {
            this.name = name;
            this.date = date;
            this.motif = motif;
            this.color = color;
            this.accent = accent;
        }
    }

    /// Ambient city life, built in forty-metre chunks independently of TrackSpawner. Nothing here
    /// has a collider: these are residents, storefronts and festival dressing on the pavement, not
    /// hazards. Keeping the system separate means a new chunk of people can never change the course
    /// or the object pools the run depends on.
    ///
    /// District 0: intersections, pedestrians and corner stores.
    /// District 1: rooftop diners, a kids' football pitch and a cricket match.
    /// District 2: an expandable catalogue of US observances, one per chunk, with decorations and crowds.
    public class DistrictLife : MonoBehaviour
    {
        PeopleCatalogue people;
        int diagnostics = 6;

        // Residents are pooled: a chunk streaming in reuses people instead of instantiating rigged characters
        // (nine of them in one frame was the mid-run stutter), and the pool is filled at start, two a frame.
        readonly Dictionary<Pedestrian, Stack<Pedestrian>> pool = new();
        Transform poolRoot;

        public Pedestrian Take(Pedestrian template, Transform parent)
        {
            if (pool.TryGetValue(template, out var stack) && stack.Count > 0)
            {
                var reused = stack.Pop();
                reused.transform.SetParent(parent, false);
                reused.gameObject.SetActive(true);
                return reused;
            }
            var made = Instantiate(template, parent);
            made.template = template;
            return made;
        }

        /// A person inside a building (shop customers and staff, terrace diners): pooled like the street residents and
        /// returned when the street segment that owns them is culled (TrackSpawner.Cull calls Return).
        public Transform Occupant(Transform parent, Vector3 world, float yaw, bool chatting, int variant)
        {
            if (!track) track = FindFirstObjectByType<TrackSpawner>();
            var local = parent.InverseTransformPoint(world);
            return Person(parent, local, yaw, ShirtTones[Mathf.Abs(variant) % ShirtTones.Length], chatting ? LifeMotion.Wave : LifeMotion.Idle, false, variant);
        }

        public void Return(GameObject chunk)
        {
            if (!chunk) return;
            foreach (var person in chunk.GetComponentsInChildren<Pedestrian>(true))
            {
                if (!person.template) continue;
                foreach (var stroll in person.GetComponents<LifeStroller>()) Destroy(stroll);
                foreach (var crosser in person.GetComponents<StreetCrosser>()) Destroy(crosser);
                person.gameObject.SetActive(false);
                person.transform.SetParent(poolRoot, false);
                if (!pool.TryGetValue(person.template, out var stack)) pool[person.template] = stack = new Stack<Pedestrian>();
                stack.Push(person);
            }
        }

        System.Collections.IEnumerator Prefill()
        {
            if (!people) people = Resources.Load<PeopleCatalogue>("PeopleCatalogue");
            if (!people || people.bodies == null) yield break;
            for (int i = 0; i < 52; i++) // street residents plus shop and terrace occupants (11 Nov 2026)
            {
                var template = people.bodies[i % people.bodies.Length];
                var made = Instantiate(template, poolRoot);
                made.template = template;
                made.gameObject.SetActive(false);
                if (!pool.TryGetValue(template, out var stack)) pool[template] = stack = new Stack<Pedestrian>();
                stack.Push(made);
                if (i % 2 == 1) yield return null;
            }
        }
        public static DistrictLife Instance { get; private set; }

        public const float ChunkLength = 40f;
        public const float BuildAhead = 150f;
        public const float CullBehind = 70f;

        /// Eighteen observances, in calendar order. A run of 720 m passes every one of them, so the
        /// Festivals district is a tour of the US holiday year rather than a single decorated street.
        public static readonly FestivalDef[] Festivals =
        {
            new("NEW YEAR'S DAY", "JAN 1", "midnight ball and confetti",
                new Color(1f, 0.78f, 0.18f), new Color(1f, 0.95f, 0.7f)),
            new("MARTIN LUTHER KING JR. DAY", "3RD MON JAN", "peace ring and service banners",
                new Color(0.32f, 0.55f, 0.95f), new Color(0.92f, 0.96f, 1f)),
            new("VALENTINE'S DAY", "FEB 14", "hearts and rose lights",
                new Color(1f, 0.2f, 0.45f), new Color(1f, 0.72f, 0.82f)),
            new("PRESIDENTS' DAY", "3RD MON FEB", "columns, flags and stars",
                new Color(0.2f, 0.3f, 0.62f), new Color(0.95f, 0.95f, 1f)),
            new("ST. PATRICK'S DAY", "MAR 17", "shamrocks and pots of gold",
                new Color(0.12f, 0.78f, 0.36f), new Color(1f, 0.86f, 0.28f)),
            new("EASTER", "SPRING", "pastel eggs and bunny ears",
                new Color(0.72f, 0.55f, 0.95f), new Color(1f, 0.86f, 0.94f)),
            new("CINCO DE MAYO", "MAY 5", "papel picado and sombreros",
                new Color(0.98f, 0.35f, 0.18f), new Color(0.18f, 0.78f, 0.52f)),
            new("MEMORIAL DAY", "LAST MON MAY", "half-mast flags and wreaths",
                new Color(0.24f, 0.34f, 0.62f), new Color(0.95f, 0.96f, 1f)),
            new("JUNETEENTH", "JUN 19", "red, black and green freedom banners",
                new Color(0.72f, 0.1f, 0.16f), new Color(0.1f, 0.62f, 0.3f)),
            new("INDEPENDENCE DAY", "JUL 4", "flags, bunting and fireworks",
                new Color(0.15f, 0.28f, 0.72f), new Color(1f, 0.24f, 0.28f)),
            new("LABOR DAY", "1ST MON SEP", "picnic tables and grills",
                new Color(0.9f, 0.55f, 0.18f), new Color(0.95f, 0.95f, 0.9f)),
            new("HISPANIC HERITAGE MONTH", "SEP 15 - OCT 15", "maracas, flags and lanterns",
                new Color(0.95f, 0.45f, 0.12f), new Color(0.2f, 0.7f, 0.85f)),
            new("HALLOWEEN", "OCT 31", "pumpkins, ghosts and bats",
                new Color(0.98f, 0.45f, 0.08f), new Color(0.55f, 0.25f, 0.85f)),
            new("VETERANS DAY", "NOV 11", "flags, stars and thank-you signs",
                new Color(0.16f, 0.3f, 0.66f), new Color(0.9f, 0.92f, 1f)),
            new("THANKSGIVING", "4TH THU NOV", "turkeys, corn and autumn leaves",
                new Color(0.85f, 0.42f, 0.12f), new Color(0.55f, 0.32f, 0.12f)),
            new("HANUKKAH", "WINTER", "menorah and blue-white lights",
                new Color(0.12f, 0.38f, 0.9f), new Color(0.95f, 0.96f, 1f)),
            new("CHRISTMAS", "DEC 25", "tree, gifts and warm lights",
                new Color(0.12f, 0.65f, 0.32f), new Color(0.95f, 0.2f, 0.22f)),
            new("NEW YEAR'S EVE", "DEC 31", "countdown clock and fireworks",
                new Color(0.55f, 0.28f, 0.95f), new Color(1f, 0.82f, 0.25f)),
            new("LUNAR NEW YEAR", "LUNAR CALENDAR", "red lanterns and reunion tables", new Color(0.9f, 0.12f, 0.1f), new Color(1f, 0.78f, 0.15f)),
            new("DIWALI", "LUNISOLAR CALENDAR", "rows of lamps and light patterns", new Color(0.95f, 0.45f, 0.1f), new Color(0.75f, 0.25f, 0.8f)),
            new("EID AL-FITR", "ISLAMIC CALENDAR", "lanterns and community meal tables", new Color(0.12f, 0.6f, 0.4f), new Color(1f, 0.82f, 0.3f)),
            new("EID AL-ADHA", "ISLAMIC CALENDAR", "community tables and giving banners", new Color(0.1f, 0.45f, 0.7f), new Color(1f, 0.82f, 0.3f)),
            new("HOLI", "LUNISOLAR CALENDAR", "bright colour ribbons and floral patterns", new Color(0.95f, 0.2f, 0.55f), new Color(0.2f, 0.85f, 0.8f)),
            new("KWANZAA", "DEC 26 - JAN 1", "seven lights and community table", new Color(0.75f, 0.1f, 0.15f), new Color(0.1f, 0.6f, 0.25f)),
            new("PRIDE", "JUNE", "rainbow banners and community celebrations", new Color(0.9f, 0.2f, 0.5f), new Color(0.2f, 0.65f, 0.95f)),
            new("INDIGENOUS PEOPLES' DAY", "2ND MON OCT", "community art and learning banners", new Color(0.3f, 0.65f, 0.65f), new Color(0.95f, 0.7f, 0.35f)),
        };

        static readonly Color[] SkinTones =
        {
            new(0.96f, 0.78f, 0.63f), new(0.82f, 0.6f, 0.43f),
            new(0.6f, 0.4f, 0.27f), new(0.42f, 0.27f, 0.18f),
        };

        static readonly Color[] HairTones =
        {
            new(0.1f, 0.08f, 0.07f), new(0.35f, 0.2f, 0.1f), new(0.62f, 0.45f, 0.2f),
        };

        static readonly Color[] PantsTones =
        {
            new(0.13f, 0.16f, 0.24f), new(0.22f, 0.22f, 0.26f), new(0.38f, 0.26f, 0.18f),
        };

        static readonly Color[] ShirtTones =
        {
            new(0.15f, 0.72f, 0.82f), new(0.95f, 0.35f, 0.28f), new(0.95f, 0.72f, 0.18f),
            new(0.36f, 0.78f, 0.42f), new(0.62f, 0.42f, 0.9f), new(0.94f, 0.94f, 0.9f),
        };

        public int ChunksBuilt => built.Count;
        public int DistrictBuilt { get; private set; } = -1;

        TrackSpawner track;
        readonly Dictionary<int, GameObject> built = new();
        readonly List<int> stale = new();
        int lastDistrict = -1;

        public static int FestivalForChunk(int chunk) => 16; // colour set for the festival crowd; the dressing is FestivalDecor

        /// The observance decorating the chunk that contains this distance.
        public static FestivalDef FestivalAt(float distance) =>
            Festivals[FestivalForChunk(Mathf.FloorToInt(distance / ChunkLength))];

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            poolRoot = new GameObject("Resident pool").transform;
            poolRoot.SetParent(transform, false);
            StartCoroutine(Prefill());
            track = FindFirstObjectByType<TrackSpawner>();
        }

        void Update()
        {
            if (!track) track = FindFirstObjectByType<TrackSpawner>();
            if (!track || !GameManager.Instance) return;

            int district = DashRun.District;
            if (district != lastDistrict) Rebuild(district);

            float distance = GameManager.Instance ? GameManager.Instance.Distance : 0f;
            int first = Mathf.FloorToInt((distance - CullBehind) / ChunkLength);
            int last = Mathf.FloorToInt((distance + BuildAhead) / ChunkLength);
            for (int i = first; i <= last; i++)
                if (!built.ContainsKey(i)) { built[i] = BuildChunk(i, district); break; } // one chunk a frame

            stale.Clear();
            foreach (var pair in built)
                if ((pair.Key + 1) * ChunkLength < distance - CullBehind) stale.Add(pair.Key);
            foreach (int i in stale)
            {
                Return(built[i]);
                if (built[i]) Destroy(built[i]);
                built.Remove(i);
            }
        }

        void Rebuild(int district)
        {
            foreach (var pair in built) { Return(pair.Value); if (pair.Value) Destroy(pair.Value); }
            built.Clear();
            lastDistrict = district;
            DistrictBuilt = district;
        }

        GameObject BuildChunk(int chunk, int district)
        {
            var root = new GameObject("District life " + district + "/" + chunk);
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, 0f, chunk * ChunkLength);
            var rng = new System.Random(unchecked(chunk * 73856093 ^ district * 19349663));
            // The wall ride and the gravity passage run along open kerbs; the crowd stays off them.
            if (DashCity.FacadeZone(chunk * ChunkLength, ChunkLength)) return root;
            if (district == 1) BuildRooftop(root.transform, chunk, rng);
            else if (district == 2) BuildFestival(root.transform, chunk, rng);
            else BuildStreet(root.transform, chunk, rng);
            if (diagnostics-- > 0)
                Debug.Log($"[DistrictLife] chunk {chunk} district {district}: {root.GetComponentsInChildren<Pedestrian>(true).Length} pedestrians, catalogue {(people ? people.bodies?.Length ?? -1 : -2)}, active {root.activeInHierarchy}");
            return root;
        }

        // ---------------------------------------------------------------- street district

        void BuildStreet(Transform root, int chunk, System.Random rng)
        {
            float baseZ = chunk * ChunkLength;
            // Real junctions are built by the track (TrackSpawner.Intersections); keep life off the cross road.
            const bool intersection = false;
            if (TrackSpawner.NearJunction(baseZ + 10f) || TrackSpawner.NearJunction(baseZ + 30f) || TrackSpawner.Junction(baseZ) || TrackSpawner.Junction(baseZ + 20f)) return;

            var asphalt = DashCity.Mat(track, "lifeAsphalt", new Color(0.06f, 0.07f, 0.11f));
            var paint = DashCity.Mat(track, "lifePaint", new Color(0.94f, 0.94f, 0.88f), 0.08f);
            var steel = DashCity.Mat(track, "lifeSteel", new Color(0.24f, 0.3f, 0.36f));
            var warm = DashCity.Mat(track, "lifeWarm", new Color(1f, 0.72f, 0.34f), 0.8f);
            var glass = DashCity.Mat(track, "lifeGlass", new Color(0.18f, 0.55f, 0.62f), 0.25f);
            var green = DashCity.Mat(track, "lifeGreen", new Color(0.18f, 0.5f, 0.3f));
            var red = DashCity.Mat(track, "lifeRed", new Color(1f, 0.2f, 0.2f), 0.7f);
            var amber = DashCity.Mat(track, "lifeAmber", new Color(1f, 0.62f, 0.12f), 0.7f);
            var lime = DashCity.Mat(track, "lifeLime", new Color(0.25f, 0.95f, 0.5f), 0.7f);

            if (intersection)
            {
                // A zebra crossing and a stop line. These are paint, not geometry.
                for (int i = 0; i < 8; i++)
                    Block(root, new Vector3(-3.65f + i * 1.04f, 0.012f, 6.5f), new Vector3(0.58f, 0.012f, 3.2f), paint);
                Block(root, new Vector3(0f, 0.013f, 3.9f), new Vector3(8.1f, 0.012f, 0.22f), paint);

                foreach (float side in new[] { -1f, 1f })
                {
                    if (side > 0f && RightBlocked(baseZ + 8f)) continue;
                    // Signal pole, arm over the kerb and a three-lamp head.
                    Block(root, new Vector3(side * 4.55f, 1.6f, 8.6f), new Vector3(0.14f, 3.2f, 0.14f), steel);
                    Block(root, new Vector3(side * 4.15f, 3.15f, 8.6f), new Vector3(0.8f, 0.12f, 0.12f), steel);
                    Block(root, new Vector3(side * 3.78f, 3.0f, 8.6f), new Vector3(0.34f, 0.9f, 0.3f), asphalt);
                    Block(root, new Vector3(side * 3.78f, 3.28f, 8.47f), Vector3.one * 0.15f, red, PrimitiveType.Sphere);
                    Block(root, new Vector3(side * 3.78f, 3.02f, 8.47f), Vector3.one * 0.15f, amber, PrimitiveType.Sphere);
                    Block(root, new Vector3(side * 3.78f, 2.76f, 8.47f), Vector3.one * 0.15f, lime, PrimitiveType.Sphere);

                    // Street name blades on the corner.
                    Block(root, new Vector3(side * 4.7f, 1.4f, 3.1f), new Vector3(0.1f, 2.8f, 0.1f), steel);
                    Block(root, new Vector3(side * 4.45f, 2.75f, 3.1f), new Vector3(0.7f, 0.36f, 0.08f), paint);
                    DashCity.Text(track, root, "DASH AVE", root.position + new Vector3(side * 4.45f, 2.75f, 3.04f), 0.042f, new Color(0.08f, 0.09f, 0.14f));

                    // Corner storefront: awning, lit window, door, crates and a sign.
                    float sx = side * 6.15f;
                    Block(root, new Vector3(sx, 1.55f, 15.5f), new Vector3(2.3f, 3.1f, 5.2f), asphalt);
                    Block(root, new Vector3(sx - side * 0.05f, 1.55f, 15.5f), new Vector3(2.25f, 2.9f, 5.1f), glass);
                    Block(root, new Vector3(sx - side * 0.15f, 2.72f, 15.5f), new Vector3(1.4f, 0.14f, 5.5f), side < 0 ? warm : red);
                    Block(root, new Vector3(sx - side * 0.15f, 3.15f, 15.5f), new Vector3(2.5f, 0.12f, 5.7f), asphalt);
                    Block(root, new Vector3(sx - side * 0.05f, 1.15f, 13.3f), new Vector3(0.12f, 2.2f, 1.1f), warm);
                    Block(root, new Vector3(sx - side * 0.15f, 3.55f, 15.5f), new Vector3(2.1f, 0.62f, 4.2f), asphalt);
                    DashCity.Text(track, root, "CORNER MART", root.position + new Vector3(sx - side * 0.22f, 3.55f, 15.5f), 0.07f, new Color(1f, 0.92f, 0.74f));
                    for (int c = 0; c < 3; c++)
                    {
                        Block(root, new Vector3(sx - side * 0.55f, 0.25f + c * 0.42f, 18.1f), new Vector3(0.55f, 0.36f, 0.55f), warm);
                        Block(root, new Vector3(sx - side * 0.55f, 0.44f + c * 0.42f, 18.1f), new Vector3(0.58f, 0.06f, 0.58f), green);
                    }

                    // People waiting at the kerb, plus one crossing the paint.
                    Person(root, new Vector3(side * 5.0f, 0f, 4.4f), side < 0 ? 0f : 180f, ShirtTones[Mathf.Abs(chunk + (int)side) % ShirtTones.Length], LifeMotion.Wave, false, Mathf.Abs(chunk) + 1);
                    if (!(side > 0f && RightBlocked(baseZ + 6f)))
                        Person(root, new Vector3(side * 1.9f, 0f, 6.4f), 90f, ShirtTones[Mathf.Abs(chunk + 3) % ShirtTones.Length], LifeMotion.Walk, false, Mathf.Abs(chunk) + 4);
                }
            }

            // Pedestrians on both pavements. The right pavement is left clear wherever the fourth-lane
            // route or a fuel stop occupies it.
            float pz = 8f + (float)rng.NextDouble() * 8f;
            var walker = Person(root, new Vector3(-5.25f, 0f, pz), 0f, ShirtTones[Mathf.Abs(chunk) % ShirtTones.Length], LifeMotion.Walk, false, Mathf.Abs(chunk) + 2);
            Stroll(walker, pz, pz + 12f, 0.1f + (float)rng.NextDouble() * 0.08f);
            if (!RightBlocked(baseZ + 26f))
            {
                float pz2 = 22f + (float)rng.NextDouble() * 8f;
                var walker2 = Person(root, new Vector3(5.25f, 0f, pz2), 180f, ShirtTones[Mathf.Abs(chunk + 2) % ShirtTones.Length], LifeMotion.Walk, false, Mathf.Abs(chunk) + 5);
                Stroll(walker2, pz2, pz2 - 11f, 0.1f + (float)rng.NextDouble() * 0.08f);
            }
            if (Mathf.Abs(chunk) % 3 == 1)
                Person(root, new Vector3(-5.6f, 0f, 33f), 12f, ShirtTones[Mathf.Abs(chunk + 1) % ShirtTones.Length], LifeMotion.Cheer, false, Mathf.Abs(chunk) + 3);
            // Friends chatting outside the shops, and someone heading the other way, on both pavements.
            foreach (float side in new[] { -1f, 1f })
            {
                if (side > 0f && RightBlocked(baseZ + 20f)) continue;
                float gz = 18f + (float)rng.NextDouble() * 6f;
                Person(root, new Vector3(side * 6.35f, 0f, gz), side < 0 ? 110f : -110f, ShirtTones[0], LifeMotion.Idle, false, Mathf.Abs(chunk) * 3 + (side < 0 ? 7 : 11));
                Person(root, new Vector3(side * 6.1f, 0f, gz + 0.95f), side < 0 ? 200f : 160f, ShirtTones[0], LifeMotion.Wave, false, Mathf.Abs(chunk) * 3 + (side < 0 ? 8 : 12));
                float wz = 2f + (float)rng.NextDouble() * 6f;
                var passer = Person(root, new Vector3(side * 4.85f, 0f, wz), side < 0 ? 180f : 0f, ShirtTones[0], LifeMotion.Walk, false, Mathf.Abs(chunk) * 5 + (side < 0 ? 2 : 9));
                Stroll(passer, wz, wz + 10f, 0.12f + (float)rng.NextDouble() * 0.06f);
            }

            // A small vendor cart on alternating sides, clear of the existing market stalls.
            float cartSide = Mathf.Abs(chunk) % 2 == 0 ? -1f : 1f;
            if (!(cartSide > 0f && RightBlocked(baseZ + 31f)))
                VendorCart(root, cartSide, 31f, chunk, warm, red, glass, green);

            // Benches and bins keep the pavement readable between the bigger set pieces.
            foreach (float side in new[] { -1f, 1f })
            {
                if (side > 0f && RightBlocked(baseZ + 12f)) continue;
                Block(root, new Vector3(side * 5.85f, 0.42f, 12f), new Vector3(0.62f, 0.1f, 1.9f), steel);
                Block(root, new Vector3(side * 6.05f, 0.66f, 12f), new Vector3(0.1f, 0.5f, 1.9f), steel);
                Block(root, new Vector3(side * 6.55f, 0.45f, 12f), new Vector3(0.45f, 0.8f, 0.45f), asphalt);
            }
        }

        void VendorCart(Transform root, float side, float z, int chunk, Material warm, Material red, Material glass, Material green)
        {
            var body = DashCity.Mat(track, "lifeCartBody", new Color(0.82f, 0.42f, 0.16f), 0.15f);
            var cream = DashCity.Mat(track, "lifeCartCream", new Color(0.95f, 0.9f, 0.78f));
            float x = side * 5.95f;
            Block(root, new Vector3(x, 0.62f, z), new Vector3(1.2f, 1.1f, 2.4f), body);
            Block(root, new Vector3(x, 1.2f, z), new Vector3(1.28f, 0.1f, 2.5f), cream);
            Block(root, new Vector3(x, 1.98f, z), new Vector3(1.7f, 0.12f, 2.9f), red);
            Block(root, new Vector3(x, 1.35f, z - 1.32f), new Vector3(1.5f, 0.75f, 0.08f), glass);
            foreach (float dz in new[] { -1.05f, 1.05f })
                Block(root, new Vector3(x, 0.98f, z + dz), new Vector3(0.08f, 1.9f, 0.08f), cream);
            for (int i = 0; i < 4; i++)
                Block(root, new Vector3(x - 0.36f + i * 0.24f, 1.48f, z - 1.4f), Vector3.one * 0.16f, i % 2 == 0 ? green : warm, PrimitiveType.Sphere);
            string[] names = { "TACO CART", "NOODLE BAR", "DASH DELI", "JUICE STOP" };
            DashCity.Text(track, root, names[Mathf.Abs(chunk) % names.Length], root.position + new Vector3(x, 2.2f, z - 1.52f), 0.055f, new Color(1f, 0.95f, 0.82f));
            Person(root, new Vector3(x + side * 0.72f, 0f, z + 0.3f), side < 0 ? 90f : -90f, ShirtTones[Mathf.Abs(chunk + 2) % ShirtTones.Length], LifeMotion.Wave, false, Mathf.Abs(chunk) + 6);
        }

        // ---------------------------------------------------------------- rooftop district

        void BuildRooftop(Transform root, int chunk, System.Random rng)
        {
            float baseZ = chunk * ChunkLength;
            int kind = Mathf.Abs(chunk) % 3;
            if (kind == 0) RooftopCafe(root, baseZ, rng);
            else if (kind == 1) RooftopSoccer(root, baseZ);
            else RooftopCricket(root, baseZ);
        }

        void RooftopCafe(Transform root, float baseZ, System.Random rng)
        {
            if (Void(baseZ + 8f) || Void(baseZ + 18f)) return;
            var deck = DashCity.Mat(track, "lifeRoofDeck", new Color(0.28f, 0.22f, 0.18f));
            var pole = DashCity.Mat(track, "lifeCafePole", new Color(0.16f, 0.2f, 0.22f));
            var cream = DashCity.Mat(track, "lifeCafeCream", new Color(0.96f, 0.92f, 0.84f));
            var warm = DashCity.Mat(track, "lifeCafeWarm", new Color(1f, 0.72f, 0.34f), 0.9f);
            var awning = DashCity.Mat(track, "lifeCafeAwning", new Color(0.95f, 0.36f, 0.22f), 0.15f);
            float x = -3.15f;

            Block(root, new Vector3(x, 0.035f, 13f), new Vector3(1.55f, 0.07f, 13.5f), deck);
            foreach (float z in new[] { 8f, 18f })
            {
                // Table, two plates, two chairs and a diner on each side.
                Block(root, new Vector3(x, 0.72f, z), new Vector3(0.62f, 0.06f, 0.62f), cream, PrimitiveType.Cylinder);
                Block(root, new Vector3(x, 0.36f, z), new Vector3(0.08f, 0.36f, 0.08f), pole, PrimitiveType.Cylinder);
                Block(root, new Vector3(x, 0.78f, z - 0.18f), new Vector3(0.24f, 0.03f, 0.24f), cream, PrimitiveType.Cylinder);
                Block(root, new Vector3(x, 0.78f, z + 0.18f), new Vector3(0.24f, 0.03f, 0.24f), cream, PrimitiveType.Cylinder);
                Block(root, new Vector3(x, 1.95f, z), new Vector3(0.06f, 1.9f, 0.06f), pole, PrimitiveType.Cylinder);
                Block(root, new Vector3(x, 2.45f, z), new Vector3(1.25f, 0.06f, 1.25f), awning, PrimitiveType.Cylinder);
                Person(root, new Vector3(x - 0.58f, 0f, z), 90f, ShirtTones[Mathf.Abs((int)z) % ShirtTones.Length], LifeMotion.Dine, true, Mathf.Abs((int)z) + 1);
                Person(root, new Vector3(x + 0.58f, 0f, z), -90f, ShirtTones[Mathf.Abs((int)z + 2) % ShirtTones.Length], LifeMotion.Dine, true, Mathf.Abs((int)z) + 3);
            }

            // String lights sagging between two poles, and a tiny service counter.
            foreach (float z in new[] { 5.5f, 21.5f })
                Block(root, new Vector3(x - 0.65f, 1.6f, z), new Vector3(0.08f, 3.2f, 0.08f), pole, PrimitiveType.Cylinder);
            for (int i = 0; i < 9; i++)
            {
                float k = i / 8f;
                float sag = Mathf.Sin(k * Mathf.PI) * 0.28f;
                Block(root, new Vector3(x - 0.65f, 3.1f - sag, Mathf.Lerp(5.5f, 21.5f, k)), Vector3.one * 0.1f, warm, PrimitiveType.Sphere);
            }
            Block(root, new Vector3(x + 0.35f, 0.55f, 25.2f), new Vector3(0.9f, 1.0f, 1.8f), pole);
            Block(root, new Vector3(x + 0.35f, 1.1f, 25.2f), new Vector3(0.96f, 0.08f, 1.86f), warm);
            DashCity.Text(track, root, "ROOFTOP CAFE", root.position + new Vector3(x, 2.85f, 24.2f), 0.05f, new Color(1f, 0.94f, 0.82f));
        }

        void RooftopSoccer(Transform root, float baseZ)
        {
            if (Void(baseZ + 9f) || Void(baseZ + 21f)) return;
            var grass = DashCity.Mat(track, "lifePitch", new Color(0.16f, 0.5f, 0.26f));
            var line = DashCity.Mat(track, "lifePitchLine", new Color(0.9f, 0.95f, 0.88f), 0.1f);
            var net = DashCity.Mat(track, "lifeGoalNet", new Color(0.85f, 0.9f, 0.95f), 0.1f);
            var ball = DashCity.Mat(track, "lifeBall", new Color(0.96f, 0.96f, 0.94f), 0.1f);
            float x = 3.0f;

            Block(root, new Vector3(x, 0.03f, 15f), new Vector3(1.35f, 0.03f, 13f), grass);
            Block(root, new Vector3(x, 0.045f, 15f), new Vector3(0.06f, 0.012f, 12.6f), line);
            foreach (float z in new[] { 9f, 21f })
            {
                foreach (float dx in new[] { -0.52f, 0.52f })
                    Block(root, new Vector3(x + dx, 0.55f, z), new Vector3(0.07f, 1.1f, 0.07f), net);
                Block(root, new Vector3(x, 1.1f, z), new Vector3(1.12f, 0.07f, 0.07f), net);
            }

            var kicker = Person(root, new Vector3(x, 0f, 13.4f), 90f, ShirtTones[Mathf.Abs(Mathf.RoundToInt(baseZ / ChunkLength)) % ShirtTones.Length], LifeMotion.Kick, false, 4);
            var actor = kicker.GetComponentInChildren<LifeActor>();
            if (actor)
            {
                actor.ball = Block(actor.transform, new Vector3(-0.2f, 0.16f, 0.1f), Vector3.one * 0.2f, ball, PrimitiveType.Sphere).transform;
                actor.ballTravel = 3.4f;
            }
            Person(root, new Vector3(x, 0f, 10.2f), 0f, ShirtTones[1], LifeMotion.Idle, false, 2);
            Person(root, new Vector3(x - 0.7f, 0f, 17.2f), 180f, ShirtTones[2], LifeMotion.Cheer, false, 3);
            Person(root, new Vector3(x - 0.75f, 0f, 19.2f), 180f, ShirtTones[4], LifeMotion.Cheer, false, 5);
            DashCity.Text(track, root, "ROOF LEAGUE", root.position + new Vector3(x, 1.65f, 8.2f), 0.045f, new Color(1f, 0.95f, 0.8f));
        }

        void RooftopCricket(Transform root, float baseZ)
        {
            if (Void(baseZ + 11f) || Void(baseZ + 19f)) return;
            var pitch = DashCity.Mat(track, "lifeCricketPitch", new Color(0.55f, 0.42f, 0.24f));
            var line = DashCity.Mat(track, "lifeCricketLine", new Color(0.95f, 0.94f, 0.86f), 0.1f);
            var stump = DashCity.Mat(track, "lifeStump", new Color(0.85f, 0.72f, 0.46f));
            var batMat = DashCity.Mat(track, "lifeBat", new Color(0.9f, 0.7f, 0.36f));
            float x = -3.0f;

            Block(root, new Vector3(x, 0.03f, 15f), new Vector3(1.25f, 0.03f, 14f), pitch);
            Block(root, new Vector3(x, 0.045f, 11.5f), new Vector3(1.0f, 0.012f, 0.08f), line);
            Block(root, new Vector3(x, 0.045f, 18.5f), new Vector3(1.0f, 0.012f, 0.08f), line);

            // Stumps and bails behind the batter.
            foreach (float dx in new[] { -0.16f, 0f, 0.16f })
                Block(root, new Vector3(x + dx, 0.46f, 18.9f), new Vector3(0.035f, 0.92f, 0.035f), stump, PrimitiveType.Cylinder);
            Block(root, new Vector3(x, 0.94f, 18.9f), new Vector3(0.4f, 0.035f, 0.05f), stump);

            var bowler = Person(root, new Vector3(x, 0f, 11.2f), 0f, ShirtTones[3], LifeMotion.Bowl, false, 1);
            var bowlActor = bowler.GetComponentInChildren<LifeActor>();
            if (bowlActor)
            {
                bowlActor.ball = Block(bowlActor.transform, new Vector3(0f, 1.25f, 0.2f), Vector3.one * 0.15f, batMat, PrimitiveType.Sphere).transform;
            }

            var batter = Person(root, new Vector3(x, 0f, 17.7f), 180f, ShirtTones[0], LifeMotion.Bat, false, 2);
            var batActor = batter.GetComponentInChildren<LifeActor>();
            if (batActor)
            {
                var hand = Joint(batActor.transform, new Vector3(0.28f, 1.05f, 0.12f));
                Block(hand, new Vector3(0f, -0.46f, 0f), new Vector3(0.09f, 0.95f, 0.16f), batMat);
                batActor.bat = hand;
            }
            Person(root, new Vector3(x, 0f, 20.2f), 0f, ShirtTones[4], LifeMotion.Idle, false, 5);
            Person(root, new Vector3(x - 0.8f, 0f, 14.2f), 180f, ShirtTones[2], LifeMotion.Cheer, false, 6);
            Person(root, new Vector3(x - 0.85f, 0f, 16.4f), 180f, ShirtTones[1], LifeMotion.Cheer, false, 7);
            DashCity.Text(track, root, "ROOF CRICKET", root.position + new Vector3(x, 1.65f, 10.1f), 0.045f, new Color(1f, 0.95f, 0.8f));
        }

        // ---------------------------------------------------------------- festivals district

        void BuildFestival(Transform root, int chunk, System.Random rng)
        {
            int index = FestivalForChunk(chunk);
            var f = Festivals[index];
            float baseZ = chunk * ChunkLength;
            var fest = DashCity.Mat(track, "fest" + index, f.color, 0.7f);
            var accent = DashCity.Mat(track, "festAccent" + index, f.accent, 0.8f);
            var ink = DashCity.Mat(track, "festInk", new Color(0.055f, 0.06f, 0.12f));
            var cream = DashCity.Mat(track, "festCream", new Color(0.96f, 0.94f, 0.88f));
            var warm = DashCity.Mat(track, "festWarm", new Color(1f, 0.74f, 0.34f), 0.9f);
            track.PlaceAsset(track.festivalStageModel, root, root.position + new Vector3(-9f, 0f, 18f), 6f, 90f);

            // Overhead banner: every chunk announces its observance, so the year is readable at speed.
            foreach (float side in new[] { -1f, 1f })
                Block(root, new Vector3(side * 4.45f, 3.2f, 2.2f), new Vector3(0.12f, 6.4f, 0.12f), ink);
            Block(root, new Vector3(0f, 6.35f, 2.2f), new Vector3(9f, 0.16f, 0.2f), ink);
            Block(root, new Vector3(0f, 6.1f, 2.1f), new Vector3(8.4f, 0.8f, 0.08f), fest);
            DashCity.Text(track, root, "MERRY CHRISTMAS   /   DEC 25", root.position + new Vector3(0f, 6.12f, 2.02f), 0.058f, Color.white);

            // Stalls, lanterns, bunting, light towers and stage gantries come from FestivalDecor (client, 3 Nov:
            // real decorations). The old per-chunk booths, low bunting and primitive motifs are retired: the
            // motifs were sphere stacks, and the bunting crossed the wall-run line.

            // A small crowd in the festival colours. Cheering, waving, and one person just watching.
            for (int i = 0; i < 3; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                if (side > 0f && RightBlocked(baseZ + 20f)) continue;
                float z = 8f + i * 8f;
                var color = i == 1 ? f.accent : i == 2 ? cream.GetColor("_BaseColor") : f.color;
                Person(root, new Vector3(side * 6.4f, 0f, z), side < 0 ? 90f : -90f, color,
                    i == 2 ? LifeMotion.Wave : LifeMotion.Cheer, false, index + i);
            }
        }

        void FestivalBooth(Transform root, float side, float baseZ, FestivalDef f, Material fest, Material accent, Material ink, Material cream, Material warm)
        {
            float x = side * 6.2f;
            float z = 15.5f;
            Block(root, new Vector3(x, 0.58f, z), new Vector3(1.5f, 1.16f, 3.4f), ink);
            Block(root, new Vector3(x, 1.2f, z), new Vector3(1.58f, 0.12f, 3.5f), fest);
            Block(root, new Vector3(x - side * 0.08f, 1.55f, z), new Vector3(1.42f, 0.7f, 3.2f), accent);
            foreach (float dz in new[] { -1.45f, 1.45f })
                Block(root, new Vector3(x, 1.55f, z + dz), new Vector3(0.1f, 3.1f, 0.1f), ink);
            Block(root, new Vector3(x, 3.05f, z), new Vector3(1.85f, 0.14f, 3.9f), fest);
            Block(root, new Vector3(x, 2.85f, z), new Vector3(1.7f, 0.08f, 3.7f), warm);
            Block(root, new Vector3(x - side * 0.1f, 3.45f, z), new Vector3(1.8f, 0.62f, 3.4f), ink);
            DashCity.Text(track, root, f.name, root.position + new Vector3(x - side * 0.18f, 3.45f, z), 0.05f, Color.white);
            // A counter of small goods: cups, boxes and a till.
            for (int i = 0; i < 4; i++)
                Block(root, new Vector3(x - side * 0.35f, 1.45f, z - 1.1f + i * 0.65f), new Vector3(0.22f, 0.24f, 0.22f), i % 2 == 0 ? cream : accent);
            Block(root, new Vector3(x + side * 0.2f, 1.5f, z + 1.2f), new Vector3(0.32f, 0.4f, 0.32f), ink);
            Person(root, new Vector3(x + side * 0.55f, 0f, z + 0.4f), side < 0 ? 90f : -90f,
                ShirtTones[Mathf.Abs(Mathf.RoundToInt(baseZ / ChunkLength) + 2) % ShirtTones.Length], LifeMotion.Wave, false, 2);
        }

        void Motif(Transform root, int index, float x, float z, FestivalDef f, Material fest, Material accent, Material cream, Material warm)
        {
            Material ink = DashCity.Mat(track, "festivalInk", new Color(0.04f, 0.06f, 0.1f));
            switch (index)
            {
                case 0: // New Year's Day: a midnight ball, confetti and a year sign.
                    Block(root, new Vector3(x, 3.4f, z), new Vector3(0.05f, 6.8f, 0.05f), cream, PrimitiveType.Cylinder);
                    Block(root, new Vector3(x, 4.3f, z), Vector3.one * 0.62f, accent, PrimitiveType.Sphere);
                    for (int i = 0; i < 10; i++)
                        Block(root, new Vector3(x + Mathf.Cos(i * 2.1f) * 0.8f, 1.4f + i * 0.28f, z + Mathf.Sin(i * 1.7f) * 0.5f), Vector3.one * 0.12f, i % 2 == 0 ? fest : accent);
                    DashCity.Text(track, root, System.DateTime.UtcNow.Year.ToString(), root.position + new Vector3(x, 1.9f, z - 0.4f), 0.09f, Color.white);
                    break;
                case 1: // MLK Day: a peace ring and service banners.
                    for (int i = 0; i < 14; i++)
                    {
                        float a = i / 14f * Mathf.PI * 2f;
                        Block(root, new Vector3(x + Mathf.Cos(a) * 0.8f, 2.4f + Mathf.Sin(a) * 0.8f, z), Vector3.one * 0.16f, i % 3 == 0 ? accent : fest);
                    }
                    Block(root, new Vector3(x, 2.4f, z), new Vector3(0.16f, 1.6f, 0.16f), accent);
                    Block(root, new Vector3(x, 1.9f, z), new Vector3(1.0f, 0.16f, 0.16f), accent);
                    DashCity.Text(track, root, "DAY OF SERVICE", root.position + new Vector3(x, 1.1f, z - 0.3f), 0.055f, Color.white);
                    break;
                case 2: // Valentine's: a heart built from two spheres and a tilted cube.
                    Block(root, new Vector3(x - 0.24f, 2.45f, z), Vector3.one * 0.56f, fest, PrimitiveType.Sphere);
                    Block(root, new Vector3(x + 0.24f, 2.45f, z), Vector3.one * 0.56f, fest, PrimitiveType.Sphere);
                    var tip = Block(root, new Vector3(x, 2.12f, z), new Vector3(0.56f, 0.56f, 0.56f), fest);
                    tip.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    Block(root, new Vector3(x, 1.35f, z), new Vector3(0.08f, 1.4f, 0.08f), accent);
                    DashCity.Text(track, root, "BE MINE", root.position + new Vector3(x, 0.9f, z - 0.3f), 0.06f, Color.white);
                    break;
                case 3: // Presidents' Day: columns, a pediment and stars.
                    for (int i = -1; i <= 1; i++)
                    {
                        Block(root, new Vector3(x + i * 0.55f, 1.15f, z), new Vector3(0.2f, 2.3f, 0.2f), cream, PrimitiveType.Cylinder);
                        Block(root, new Vector3(x + i * 0.55f, 2.35f, z), new Vector3(0.3f, 0.12f, 0.3f), accent, PrimitiveType.Cylinder);
                    }
                    Block(root, new Vector3(x, 2.55f, z), new Vector3(1.8f, 0.18f, 0.5f), accent);
                    for (int i = 0; i < 5; i++)
                        Block(root, new Vector3(x - 0.6f + i * 0.3f, 2.85f, z), Vector3.one * 0.12f, cream, PrimitiveType.Sphere);
                    break;
                case 4: // St Patrick's: shamrocks and a pot of gold.
                    for (int i = 0; i < 3; i++)
                    {
                        float a = i / 3f * Mathf.PI * 2f;
                        Block(root, new Vector3(x + Mathf.Cos(a) * 0.3f, 2.3f + Mathf.Sin(a) * 0.3f, z), Vector3.one * 0.42f, fest, PrimitiveType.Sphere);
                    }
                    Block(root, new Vector3(x, 1.75f, z), new Vector3(0.12f, 0.9f, 0.12f), fest, PrimitiveType.Cylinder);
                    Block(root, new Vector3(x, 0.35f, z + 0.8f), new Vector3(0.55f, 0.55f, 0.55f), warm, PrimitiveType.Cylinder);
                    for (int i = 0; i < 4; i++)
                        Block(root, new Vector3(x - 0.18f + i * 0.12f, 0.7f, z + 0.8f), Vector3.one * 0.12f, accent, PrimitiveType.Sphere);
                    break;
                case 5: // Easter: pastel eggs and bunny ears.
                    for (int i = 0; i < 5; i++)
                        Block(root, new Vector3(x - 0.6f + i * 0.3f, 0.45f + i % 2 * 0.2f, z + 0.5f), new Vector3(0.24f, 0.36f, 0.24f), i % 2 == 0 ? fest : accent, PrimitiveType.Sphere);
                    Block(root, new Vector3(x, 1.8f, z), Vector3.one * 0.38f, cream, PrimitiveType.Sphere);
                    Block(root, new Vector3(x - 0.14f, 2.2f, z), new Vector3(0.1f, 0.55f, 0.1f), fest, PrimitiveType.Capsule);
                    Block(root, new Vector3(x + 0.14f, 2.2f, z), new Vector3(0.1f, 0.55f, 0.1f), fest, PrimitiveType.Capsule);
                    break;
                case 6: // Cinco de Mayo: papel picado and a sombrero.
                    for (int i = 0; i < 9; i++)
                    {
                        float k = i / 8f;
                        Block(root, new Vector3(x, 3.5f - Mathf.Sin(k * Mathf.PI) * 0.3f, Mathf.Lerp(z - 1.4f, z + 1.4f, k)),
                            new Vector3(0.34f, 0.42f, 0.04f), i % 3 == 0 ? accent : i % 3 == 1 ? fest : cream);
                    }
                    Block(root, new Vector3(x, 0.95f, z), new Vector3(0.95f, 0.12f, 0.95f), fest, PrimitiveType.Cylinder);
                    Block(root, new Vector3(x, 1.25f, z), new Vector3(0.55f, 0.55f, 0.55f), accent, PrimitiveType.Sphere);
                    break;
                case 7: // Memorial Day: half-mast flag and a wreath.
                    Block(root, new Vector3(x, 1.8f, z), new Vector3(0.06f, 3.6f, 0.06f), cream, PrimitiveType.Cylinder);
                    Block(root, new Vector3(x + 0.42f, 2.85f, z), new Vector3(0.8f, 0.5f, 0.04f), fest);
                    for (int i = 0; i < 10; i++)
                    {
                        float a = i / 10f * Mathf.PI * 2f;
                        Block(root, new Vector3(x + Mathf.Cos(a) * 0.45f, 0.75f + Mathf.Sin(a) * 0.45f, z + 0.6f), Vector3.one * 0.16f, i % 2 == 0 ? accent : cream);
                    }
                    break;
                case 8: // Juneteenth: red, black and green bands with a freedom star.
                    Block(root, new Vector3(x, 2.7f, z), new Vector3(1.3f, 0.4f, 0.06f), fest);
                    Block(root, new Vector3(x, 2.3f, z), new Vector3(1.3f, 0.4f, 0.06f), cream);
                    Block(root, new Vector3(x, 1.9f, z), new Vector3(1.3f, 0.4f, 0.06f), accent);
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i / 5f * Mathf.PI * 2f - Mathf.PI / 2f;
                        Block(root, new Vector3(x + Mathf.Cos(a) * 0.45f, 1.1f + Mathf.Sin(a) * 0.45f, z + 0.4f), Vector3.one * 0.13f, accent, PrimitiveType.Sphere);
                    }
                    break;
                case 9: // Independence Day: flags, bunting and a firework burst.
                    Block(root, new Vector3(x, 2.2f, z), new Vector3(0.07f, 4.4f, 0.07f), cream, PrimitiveType.Cylinder);
                    Block(root, new Vector3(x + 0.55f, 3.55f, z), new Vector3(1.0f, 0.6f, 0.05f), fest);
                    for (int i = 0; i < 12; i++)
                    {
                        float a = i / 12f * Mathf.PI * 2f;
                        Block(root, new Vector3(x + Mathf.Cos(a) * (0.7f + i % 2 * 0.3f), 4.4f + Mathf.Sin(a) * (0.7f + i % 2 * 0.3f), z), Vector3.one * 0.12f, i % 3 == 0 ? accent : i % 3 == 1 ? fest : cream, PrimitiveType.Sphere);
                    }
                    for (int i = 0; i < 6; i++)
                        Block(root, new Vector3(x - 1.0f + i * 0.4f, 1.0f + i % 2 * 0.2f, z + 0.7f), new Vector3(0.22f, 0.3f, 0.05f), i % 2 == 0 ? fest : accent);
                    break;
                case 10: // Labor Day: picnic table and grill.
                    Block(root, new Vector3(x, 0.75f, z), new Vector3(1.3f, 0.12f, 0.9f), cream);
                    foreach (float dx in new[] { -0.5f, 0.5f })
                        Block(root, new Vector3(x + dx, 0.38f, z), new Vector3(0.12f, 0.75f, 0.12f), ink);
                    foreach (float dz in new[] { -0.55f, 0.55f })
                        Block(root, new Vector3(x, 0.42f, z + dz), new Vector3(1.3f, 0.1f, 0.28f), fest);
                    Block(root, new Vector3(x, 0.55f, z + 1.25f), new Vector3(0.5f, 0.5f, 0.5f), ink, PrimitiveType.Cylinder);
                    Block(root, new Vector3(x, 0.85f, z + 1.25f), new Vector3(0.55f, 0.1f, 0.55f), accent, PrimitiveType.Cylinder);
                    break;
                case 11: // Hispanic Heritage Month: maracas, flags and lanterns.
                    foreach (float dz in new[] { -0.5f, 0.5f })
                    {
                        Block(root, new Vector3(x + dz, 1.6f, z), new Vector3(0.22f, 0.5f, 0.22f), fest, PrimitiveType.Sphere);
                        Block(root, new Vector3(x + dz, 1.1f, z), new Vector3(0.08f, 0.55f, 0.08f), cream, PrimitiveType.Cylinder);
                    }
                    for (int i = 0; i < 5; i++)
                        Block(root, new Vector3(x - 0.8f + i * 0.4f, 2.7f, z), new Vector3(0.3f, 0.4f, 0.04f), i % 2 == 0 ? accent : fest);
                    Block(root, new Vector3(x, 0.5f, z + 0.7f), new Vector3(0.4f, 0.4f, 0.4f), warm, PrimitiveType.Sphere);
                    break;
                case 12: // Halloween: pumpkins, a ghost and bats.
                    for (int i = 0; i < 3; i++)
                    {
                        float px = x - 0.6f + i * 0.55f;
                        Block(root, new Vector3(px, 0.35f + i % 2 * 0.1f, z + 0.4f), new Vector3(0.5f, 0.42f, 0.5f), fest, PrimitiveType.Sphere);
                        Block(root, new Vector3(px, 0.62f + i % 2 * 0.1f, z + 0.4f), new Vector3(0.08f, 0.2f, 0.08f), accent, PrimitiveType.Cylinder);
                    }
                    Block(root, new Vector3(x, 2.3f, z), new Vector3(0.55f, 0.8f, 0.5f), cream, PrimitiveType.Sphere);
                    Block(root, new Vector3(x, 1.85f, z), new Vector3(0.6f, 0.3f, 0.55f), cream);
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i / 4f * Mathf.PI * 2f;
                        Block(root, new Vector3(x + Mathf.Cos(a) * 0.9f, 3.4f + Mathf.Sin(a) * 0.4f, z), new Vector3(0.35f, 0.12f, 0.06f), accent);
                    }
                    break;
                case 13: // Veterans Day: flags, stars and a thank-you sign.
                    foreach (float dz in new[] { -0.6f, 0.6f })
                    {
                        Block(root, new Vector3(x + dz, 1.8f, z), new Vector3(0.06f, 3.6f, 0.06f), cream, PrimitiveType.Cylinder);
                        Block(root, new Vector3(x + dz + 0.38f, 3.0f, z), new Vector3(0.75f, 0.5f, 0.04f), fest);
                    }
                    for (int i = 0; i < 6; i++)
                        Block(root, new Vector3(x - 0.7f + i * 0.28f, 1.1f, z + 0.6f), Vector3.one * 0.12f, accent, PrimitiveType.Sphere);
                    DashCity.Text(track, root, "THANK YOU", root.position + new Vector3(x, 0.75f, z - 0.3f), 0.05f, Color.white);
                    break;
                case 14: // Thanksgiving: a turkey with a fan tail, corn and leaves.
                    Block(root, new Vector3(x, 0.75f, z), new Vector3(0.7f, 0.7f, 0.7f), warm, PrimitiveType.Sphere);
                    for (int i = 0; i < 7; i++)
                    {
                        float a = Mathf.Lerp(-1.1f, 1.1f, i / 6f);
                        Block(root, new Vector3(x + Mathf.Sin(a) * 0.8f, 1.1f + Mathf.Cos(a) * 0.7f, z + 0.25f), new Vector3(0.18f, 0.55f, 0.1f), i % 2 == 0 ? fest : accent);
                    }
                    Block(root, new Vector3(x, 1.05f, z - 0.4f), new Vector3(0.3f, 0.3f, 0.3f), cream, PrimitiveType.Sphere);
                    foreach (float dx in new[] { -0.75f, 0.75f })
                        Block(root, new Vector3(x + dx, 0.3f, z + 0.6f), new Vector3(0.18f, 0.6f, 0.18f), fest, PrimitiveType.Cylinder);
                    break;
                case 15: // Hanukkah: a menorah with nine lit candles.
                    Block(root, new Vector3(x, 0.35f, z), new Vector3(1.1f, 0.18f, 0.35f), accent, PrimitiveType.Cylinder);
                    Block(root, new Vector3(x, 0.65f, z), new Vector3(0.12f, 0.6f, 0.12f), accent, PrimitiveType.Cylinder);
                    for (int i = 0; i < 9; i++)
                    {
                        float cx = x - 0.7f + i * 0.175f;
                        float lift = i == 4 ? 0.55f : 0f;
                        Block(root, new Vector3(cx, 0.95f + lift, z), new Vector3(0.06f, 0.65f, 0.06f), cream, PrimitiveType.Cylinder);
                        Block(root, new Vector3(cx, 1.33f + lift, z), Vector3.one * 0.1f, warm, PrimitiveType.Sphere);
                    }
                    break;
                case 16: // Christmas: a stacked tree, a star and gifts.
                    for (int i = 0; i < 3; i++)
                        Block(root, new Vector3(x, 0.6f + i * 0.65f, z), new Vector3(1.5f - i * 0.35f, 0.8f, 1.5f - i * 0.35f), fest, PrimitiveType.Cylinder);
                    Block(root, new Vector3(x, 2.55f, z), Vector3.one * 0.35f, accent, PrimitiveType.Sphere);
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i / 8f * Mathf.PI * 2f;
                        Block(root, new Vector3(x + Mathf.Cos(a) * 0.65f, 0.9f + i % 3 * 0.35f, z + Mathf.Sin(a) * 0.65f), Vector3.one * 0.1f, i % 2 == 0 ? accent : cream, PrimitiveType.Sphere);
                    }
                    foreach (float dx in new[] { -0.65f, 0.65f })
                        Block(root, new Vector3(x + dx, 0.28f, z + 0.7f), new Vector3(0.45f, 0.4f, 0.45f), dx < 0f ? accent : cream);
                    break;
                case 17: // New Year's Eve: a countdown clock and fireworks.
                    Block(root, new Vector3(x, 2.2f, z), new Vector3(1.2f, 1.2f, 0.16f), cream, PrimitiveType.Cylinder);
                    Block(root, new Vector3(x, 2.2f, z - 0.1f), new Vector3(0.08f, 0.45f, 0.06f), ink);
                    Block(root, new Vector3(x + 0.2f, 2.2f, z - 0.1f), new Vector3(0.4f, 0.08f, 0.06f), ink);
                    for (int i = 0; i < 14; i++)
                    {
                        float a = i / 14f * Mathf.PI * 2f;
                        Block(root, new Vector3(x + Mathf.Cos(a) * (0.8f + i % 2 * 0.35f), 4.2f + Mathf.Sin(a) * (0.8f + i % 2 * 0.35f), z), Vector3.one * 0.11f, i % 3 == 0 ? accent : fest, PrimitiveType.Sphere);
                    }
                    DashCity.Text(track, root, "COUNTDOWN", root.position + new Vector3(x, 0.8f, z - 0.3f), 0.05f, Color.white);
                    break;
                default:
                    // New catalogue entries have their own visual vocabulary; no generic fireworks
                    // on religious or commemorative observances.
                    if (index == 18) // Lanterns for Lunar New Year.
                    {
                        for (int i = -1; i <= 1; i++)
                        {
                            Block(root, new Vector3(x + i * 0.65f, 2.6f, z), new Vector3(0.42f, 0.65f, 0.42f), fest, PrimitiveType.Sphere);
                            Block(root, new Vector3(x + i * 0.65f, 2.15f, z), new Vector3(0.06f, 0.3f, 0.06f), accent);
                        }
                    }
                    else if (index == 19 || index == 23) // Diwali lamps or seven Kwanzaa lights.
                    {
                        int count = index == 23 ? 7 : 5;
                        for (int i = 0; i < count; i++)
                        {
                            float dx = (i - (count - 1) * 0.5f) * 0.25f;
                            Block(root, new Vector3(x + dx, 1f, z), new Vector3(0.13f, index == 23 ? 0.6f : 0.13f, 0.13f), i % 2 == 0 ? fest : accent);
                            Block(root, new Vector3(x + dx, index == 23 ? 1.38f : 1.15f, z), Vector3.one * 0.09f, warm, PrimitiveType.Sphere);
                        }
                    }
                    else if (index == 20 || index == 21) // Community meal and giving.
                    {
                        Block(root, new Vector3(x, 0.85f, z), new Vector3(1.8f, 0.12f, 0.8f), cream);
                        for (int i = -1; i <= 1; i++)
                            Block(root, new Vector3(x + i * 0.5f, 0.95f, z), new Vector3(0.3f, 0.08f, 0.3f), accent, PrimitiveType.Cylinder);
                        DashCity.Text(track, root, index == 20 ? "COMMUNITY MEAL" : "SHARE AND GIVE", root.position + new Vector3(x, 2f, z), 0.045f, Color.white);
                    }
                    else if (index == 22 || index == 24) // Colour ribbons / Pride rainbow.
                    {
                        Color[] rainbow = { Color.red, new Color(1f, 0.5f, 0f), Color.yellow, Color.green, Color.blue, new Color(0.6f, 0.2f, 0.8f) };
                        for (int i = 0; i < rainbow.Length; i++)
                            Block(root, new Vector3(x, 1.3f + i * 0.2f, z), new Vector3(1.6f, 0.18f, 0.05f), DashCity.Mat(track, "lifeRainbow" + i, rainbow[i]));
                    }
                    else
                    {
                        Block(root, new Vector3(x, 1.3f, z), new Vector3(1.8f, 1.4f, 0.1f), fest);
                        DashCity.Text(track, root, "COMMUNITY ART\nLEARN AND LISTEN", root.position + new Vector3(x, 1.3f, z - 0.1f), 0.045f, Color.white);
                    }
                    break;
            }
        }

        // ---------------------------------------------------------------- shared helpers

        static bool Void(float worldZ) => false;

        /// The rooftop shortcut's ramp and the fuel-stop shoulder both own the right-hand pavement. Life
        /// steps aside there so it never stands inside the express deck or the pit canopy.
        static bool RightBlocked(float worldZ)
        {
            return RooftopCourse.OwnsRightSide(worldZ) || DashCity.FacadeZone(worldZ, 1f);
        }

        static GameObject Block(Transform parent, Vector3 pos, Vector3 scale, Material material, PrimitiveType type = PrimitiveType.Cube)
        {
            var go = GameObject.CreatePrimitive(type);
            var collider = go.GetComponent<Collider>();
            if (collider) Object.Destroy(collider);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        static Transform Joint(Transform parent, Vector3 pos)
        {
            var go = new GameObject("joint");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            return go.transform;
        }

        /// A resident built from blocks with real limb pivots, so LifeActor can walk, kick, bowl,
        /// bat, dine, cheer or wave without a skeletal rig.
        Transform Person(Transform parent, Vector3 pos, float yaw, Color shirtColor, LifeMotion motion, bool seated, int variant)
        {
            // Real rigged residents (PeopleCatalogue) wherever the build has them; the block figure is the fallback.
            if (!people) people = Resources.Load<PeopleCatalogue>("PeopleCatalogue");
            if (people && people.bodies != null && people.bodies.Length > 0)
            {
                var template = people.bodies[Mathf.Abs(variant * 5 + Mathf.RoundToInt(pos.z * 3f)) % people.bodies.Length];
                var person = Take(template, parent);
                person.name = (seated ? "Diner" : "Resident") + " / " + motion;
                person.transform.localPosition = pos;
                person.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                string clip = motion switch
                {
                    LifeMotion.Walk => Pedestrian.Walk,
                    LifeMotion.Cheer => Pedestrian.Dance,
                    LifeMotion.Kick or LifeMotion.Bowl or LifeMotion.Bat => Pedestrian.Jog,
                    LifeMotion.Wave => Pedestrian.Chat,
                    _ => variant % 2 == 0 ? Pedestrian.Idle : Pedestrian.Chat
                };
                person.Dress(people, variant + Mathf.RoundToInt(pos.z * 7f), clip);
                if (motion is LifeMotion.Kick or LifeMotion.Bowl or LifeMotion.Bat) person.gameObject.AddComponent<LifeStroller>().Set(pos.z - 1.5f, pos.z + 1.5f, 0.35f);
                return person.transform;
            }
            var root = new GameObject(seated ? "Diner" : "Resident").transform;
            root.SetParent(parent, false);
            root.localPosition = pos;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var body = new GameObject("Body").transform;
            body.SetParent(root, false);

            var skin = DashCity.Mat(track, "lifeSkin" + (variant % SkinTones.Length), SkinTones[variant % SkinTones.Length]);
            var hair = DashCity.Mat(track, "lifeHair" + (variant % HairTones.Length), HairTones[variant % HairTones.Length]);
            var pants = DashCity.Mat(track, "lifePants" + (variant % PantsTones.Length), PantsTones[variant % PantsTones.Length]);
            var shoe = DashCity.Mat(track, "lifeShoe", new Color(0.08f, 0.09f, 0.12f));
            var shirt = DashCity.Mat(track, "lifeShirt" + ColorUtility.ToHtmlStringRGB(shirtColor), shirtColor);
            var eye = DashCity.Mat(track, "lifeEye", new Color(0.04f, 0.04f, 0.06f));

            float torsoY = seated ? 0.82f : 1.12f;
            float headY = seated ? 1.28f : 1.64f;
            Block(body, new Vector3(0f, torsoY, 0f), new Vector3(0.42f, 0.62f, 0.26f), shirt);
            Block(body, new Vector3(0f, headY, 0f), Vector3.one * 0.3f, skin, PrimitiveType.Sphere);
            Block(body, new Vector3(0f, headY + 0.12f, -0.02f), new Vector3(0.31f, 0.12f, 0.3f), hair, PrimitiveType.Sphere);
            Block(body, new Vector3(-0.07f, headY + 0.02f, 0.145f), new Vector3(0.045f, 0.045f, 0.02f), eye, PrimitiveType.Sphere);
            Block(body, new Vector3(0.07f, headY + 0.02f, 0.145f), new Vector3(0.045f, 0.045f, 0.02f), eye, PrimitiveType.Sphere);

            var leftLeg = Joint(body, new Vector3(-0.11f, seated ? 0.6f : 0.8f, 0f));
            var rightLeg = Joint(body, new Vector3(0.11f, seated ? 0.6f : 0.8f, 0f));
            foreach (var leg in new[] { leftLeg, rightLeg })
            {
                Block(leg, new Vector3(0f, -0.32f, 0f), new Vector3(0.14f, 0.62f, 0.16f), pants);
                Block(leg, new Vector3(0f, -0.64f, 0.03f), new Vector3(0.15f, 0.1f, 0.24f), shoe);
            }
            if (seated)
            {
                leftLeg.localRotation = Quaternion.Euler(76f, 0f, 0f);
                rightLeg.localRotation = Quaternion.Euler(76f, 0f, 0f);
            }

            var leftArm = Joint(body, new Vector3(-0.27f, seated ? 1.04f : 1.38f, 0f));
            var rightArm = Joint(body, new Vector3(0.27f, seated ? 1.04f : 1.38f, 0f));
            foreach (var arm in new[] { leftArm, rightArm })
            {
                Block(arm, new Vector3(0f, -0.26f, 0f), new Vector3(0.1f, 0.5f, 0.12f), shirt);
                Block(arm, new Vector3(0f, -0.54f, 0f), new Vector3(0.1f, 0.1f, 0.12f), skin);
            }

            var actor = body.gameObject.AddComponent<LifeActor>();
            actor.motion = motion;
            actor.leftLeg = leftLeg;
            actor.rightLeg = rightLeg;
            actor.leftArm = leftArm;
            actor.rightArm = rightArm;
            actor.head = body;
            actor.speed = 0.85f + (variant % 4) * 0.08f;
            return root;
        }

        static void Stroll(Transform root, float fromZ, float toZ, float speed)
        {
            var walker = root.gameObject.AddComponent<LifeStroller>();
            walker.fromZ = fromZ;
            walker.toZ = toZ;
            walker.speed = speed;
            walker.phase = Random.value;
        }
    }

    /// Walks a resident up and down a short stretch of pavement. The body keeps its own bob and
    /// limb swing in LifeActor; this only moves the parent and turns it at each end.
    public class LifeStroller : MonoBehaviour
    {
        public float fromZ, toZ, speed = 0.12f, phase;
        float t;

        public void Set(float from, float to, float pace) { fromZ = from; toZ = to; speed = pace; phase = Random.value; }

        void Start() => t = phase;

        void Update()
        {
            t += Time.deltaTime * speed;
            float k = Mathf.PingPong(t, 1f);
            var p = transform.localPosition;
            p.z = Mathf.Lerp(fromZ, toZ, k);
            transform.localPosition = p;
            transform.localRotation = Quaternion.Euler(0f, k < 0.5f ? 0f : 180f, 0f);
        }
    }
}
