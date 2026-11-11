using System;
using System.Collections.Generic;
using DallasDash.Core;
using DallasDash.Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DallasDash.Gameplay
{
    // The rooftop shortcut's buildings, ramp and roof hazards (layout: RooftopCourse). The right-hand side of
    // the street in a rooftop zone is this block row, so what the rider climbs is the same building seen from
    // the street: lit shopfronts, window bays, neon signs, then flat roofs with parapets, plant and billboards.
    public partial class TrackSpawner
    {
        readonly HashSet<int> builtRooftops = new();
        PeopleCatalogue people;
        const float RoofBack = 16.6f;

        static float RampHeightAt(float z)
        {
            float s = RooftopCourse.ZoneStartAt(z);
            return RooftopCourse.RampHeight * Mathf.Clamp01((z - (s - RooftopCourse.RampLength)) / RooftopCourse.RampLength);
        }

        void BuildRooftops(float d)
        {
            float ahead = d + Tuning.FarEdge + segmentLength * 2f;
            for (int i = Mathf.Max(0, RooftopCourse.Index(d - 40f)); RooftopCourse.ZoneStart(i) - 60f < ahead; i++)
                if (RooftopCourse.ZoneStart(i) + RooftopCourse.Length + 30f > d - 40f && builtRooftops.Add(i))
                    pending.Enqueue(BuildRooftop(i));
        }

        /// The one collectible: the burger (burgers are the only currency).
        PickupDef BurgerDef()
        {
            foreach (var pickup in pickups)
                if (pickup && pickup.id == "burger" && pickup.prefab) return pickup;
            throw new InvalidOperationException("Required burger pickup is missing");
        }

        /// A pooled burger for the off-street lines (rooftops and facades). `spin` only where "up" is world up.
        GameObject RentBurger(string pool, Vector3 at, Quaternion rotation, bool spin)
        {
            var def = BurgerDef();
            return Pool.Rent(pool, () =>
            {
                var item = Instantiate(def.prefab);
                if (spin) item.AddComponent<PickupSpin>();
                if (spin && haloMaterial) Halo(item.transform);
                return item;
            }, at, rotation);
        }

        System.Collections.IEnumerator BuildRooftop(int index)
        {
            float s = RooftopCourse.ZoneStart(index);
            var blocks = RooftopCourse.Blocks(index);
            var root = new GameObject("Rooftop shortcut " + index);
            root.transform.SetParent(transform);
            // Root at the far end: Cull() destroys segment roots 60 m behind the rider.
            root.transform.position = new Vector3(0f, 0f, s + RooftopCourse.Length + 30f);
            segments.Add(root);
            int d = DashRun.District;
            float baseY = 0f;
            float top = SurfaceTraversal.RoofHeight;

            var cyan = DashCity.Mat(this, "roofNeonCyan", new Color(0.1f, 0.85f, 1f), 2.2f);
            var pink = DashCity.Mat(this, "roofNeonPink", new Color(1f, 0.1f, 0.55f), 2.2f);
            var amber = DashCity.Mat(this, "roofNeonAmber", new Color(1f, 0.55f, 0.1f), 1.8f);
            // Festivals: the rooftop course is under snow too.
            bool snowy = DashRun.District == 2;
            var deck = snowy ? DashCity.Mat(this, "xmasSnow", new Color(.93f, .95f, 1f), .05f) : DashCity.Mat(this, "roofMembrane", new Color(0.2f, 0.22f, 0.27f));
            var coping = snowy ? DashCity.Mat(this, "xmasSnowCoping", new Color(.85f, .88f, .94f)) : DashCity.Mat(this, "roofCopingStone", new Color(0.55f, 0.53f, 0.5f));
            var steel = DashCity.Mat(this, "roofSteel", new Color(0.16f, 0.18f, 0.21f));
            var glass = DashCity.Mat(this, "roofBridgeGlass", new Color(0.35f, 0.75f, 0.95f), 0.25f);
            glass.SetFloat("_Smoothness", 0.95f);

            BuildRamp(root.transform, s, cyan, pink, steel);
            BuildParkourLine(root.transform, s, 0f, steel, cyan);
            yield return null;
            if (!root) yield break;

            for (int b = 0; b < blocks.Count; b++)
            {
                var block = blocks[b];
                var shell = new GameObject("Rooftop block " + b).transform;
                shell.SetParent(root.transform, false);
                RoofBuilding(shell, block.z0, block.z1, baseY, top, b + index * 7, b == 0 ? s + 32f : block.z0, deck, coping, steel, cyan, pink, amber);
                if (b > 0 && block.bridgeLane >= 0)
                    Skybridge(shell, blocks[b - 1].z1, block.z0, block.bridgeLane, top, glass, cyan, steel);
                Combine(shell.gameObject);
                yield return null;
                if (!root) yield break;
                RoofHazards(block, b == 0 ? s + 34f : block.z0 + 7f, b == blocks.Count - 1);
                yield return null;
                if (!root) yield break;
            }
        }

        /// A wedge kicker in the right-hand lane with lit rails, chevrons on the approach and a gantry sign.
        void BuildRamp(Transform root, float s, Material cyan, Material pink, Material steel)
        {
            float x = Tuning.LaneX[RooftopCourse.EntryLane];
            float len = RooftopCourse.RampLength, h = RooftopCourse.RampHeight;
            float slope = Mathf.Atan2(h, len) * Mathf.Rad2Deg;
            var ramp = DashCity.Part(root, new Vector3(x, h * 0.5f - 0.06f, s - len * 0.5f), new Vector3(1.9f, 0.12f, Mathf.Sqrt(len * len + h * h)), steel);
            ramp.transform.rotation = Quaternion.Euler(-slope, 0f, 0f);
            foreach (float side in new[] { -1f, 1f })
            {
                var rail = DashCity.Part(root, new Vector3(x + side * 0.92f, h * 0.5f + 0.02f, s - len * 0.5f), new Vector3(0.08f, 0.06f, len), cyan);
                rail.transform.rotation = Quaternion.Euler(-slope, 0f, 0f);
            }
            DashCity.Part(root, new Vector3(x, h * 0.5f, s), new Vector3(1.9f, h, 0.14f), steel);
            DashCity.Part(root, new Vector3(x, h + 0.01f, s - 0.1f), new Vector3(1.9f, 0.04f, 0.18f), pink);
            for (int i = 1; i <= 5; i++)
                foreach (float side in new[] { -1f, 1f })
                {
                    var v = DashCity.Part(root, new Vector3(x + side * 0.25f, 0.012f, s - len - i * 4f), new Vector3(0.6f, 0.012f, 0.12f), i % 2 == 0 ? pink : cyan);
                    v.transform.rotation = Quaternion.Euler(0f, side * 38f, 0f);
                }
            float gz = s - len - 22f;
            foreach (float side in new[] { -1f, 1f })
                DashCity.Part(root, new Vector3(x + side * 1.25f, 2.3f, gz), new Vector3(0.14f, 4.6f, 0.14f), steel);
            DashCity.Part(root, new Vector3(x, 4.55f, gz), new Vector3(2.9f, 0.9f, 0.2f), steel);
            DashCity.Part(root, new Vector3(x, 4.08f, gz - 0.12f), new Vector3(2.8f, 0.05f, 0.04f), pink);
            DashCity.Part(root, new Vector3(x, 5.02f, gz - 0.12f), new Vector3(2.8f, 0.05f, 0.04f), cyan);
            DashCity.Text(this, root, "ROOFTOP SHORTCUT", new Vector3(x, 4.66f, gz - 0.13f), 0.028f, Color.white);
            DashCity.Text(this, root, "RAMP  >  WALL  >  ROOFS", new Vector3(x, 4.34f, gz - 0.13f), 0.018f, new Color(0.4f, 0.95f, 1f));
        }

        /// The real things the parkour line uses (RooftopCourse steps), placed exactly where its hops land: a
        /// delivery truck parked on the kerb, a steel fire-escape landing on the facade, and at the far end a
        /// construction skip to drop onto on the way back down.
        void BuildParkourLine(Transform root, float s, float baseY, Material steel, Material cyan)
        {
            var grate = DashCity.Mat(this, "fireEscapeGrate", new Color(0.12f, 0.13f, 0.15f));
            var paint = DashCity.Mat(this, "fireEscapeRed", new Color(0.42f, 0.1f, 0.08f));
            const float kerb = 0.2f;
            // The truck: the hurdle set's delivery van, scaled to the landing height, with a flat roof rack.
            var truck = RooftopCourse.Truck;
            ObstacleDef van = null;
            foreach (var def in obstacles) if (def && def.id == "delivery-van" && def.prefab) van = def;
            if (van)
            {
                var body = Instantiate(van.prefab, root);
                float k = (truck.top - kerb - 0.08f) / Mathf.Max(0.5f, van.size.y);
                body.transform.localScale = Vector3.one * k;
                body.transform.position = new Vector3(truck.x, kerb, s + truck.Mid);
                body.name = "Parked delivery truck / step";
            }
            DashCity.Part(root, new Vector3(truck.x, truck.top - 0.04f, s + truck.Mid), new Vector3(1.8f, 0.08f, 3.8f), steel);
            foreach (float side in new[] { -1f, 1f })
                DashCity.Part(root, new Vector3(truck.x + side * 0.86f, truck.top + 0.06f, s + truck.Mid), new Vector3(0.06f, 0.12f, 3.8f), cyan);

            // The fire-escape landing: grating on brackets, a railing on the street side, a drop ladder
            // down toward the truck, and AC plant on the wall below it.
            var landing = RooftopCourse.Landing;
            float face = SurfaceTraversal.FacadeX, outer = face - 1.6f, lx = (face + outer) * 0.5f;
            float lz0 = s + landing.z0, lz1 = s + landing.z1, lzc = (lz0 + lz1) * 0.5f;
            DashCity.Part(root, new Vector3(lx, landing.top - 0.05f, lzc), new Vector3(face - outer, 0.1f, lz1 - lz0), grate);
            for (float z = lz0 + 0.5f; z < lz1; z += 1.3f)
                DashCity.Part(root, new Vector3(outer + 0.05f, landing.top + 0.5f, z), new Vector3(0.05f, 1f, 0.05f), paint);
            DashCity.Part(root, new Vector3(outer + 0.05f, landing.top + 1.0f, lzc), new Vector3(0.07f, 0.07f, lz1 - lz0), paint);
            DashCity.Part(root, new Vector3(outer + 0.05f, landing.top + 0.5f, lzc), new Vector3(0.04f, 0.04f, lz1 - lz0), paint);
            foreach (float z in new[] { lz0 + 0.6f, lzc, lz1 - 0.6f })
            {
                var brace = DashCity.Part(root, new Vector3(lx + 0.15f, landing.top - 0.75f, z), new Vector3(0.08f, 1.6f, 0.08f), paint);
                brace.transform.rotation = Quaternion.Euler(0f, 0f, -42f);
            }
            for (float y = truck.top + 0.2f; y < landing.top; y += 0.32f)
                DashCity.Part(root, new Vector3(outer + 0.35f, y, lz0 + 0.45f), new Vector3(0.55f, 0.04f, 0.05f), paint);
            foreach (float dx in new[] { 0.08f, 0.62f })
                DashCity.Part(root, new Vector3(outer + dx, (truck.top + landing.top) * 0.5f, lz0 + 0.45f), new Vector3(0.05f, landing.top - truck.top, 0.05f), paint);
            if (roofVentModel)
                foreach (float z in new[] { lz0 + 1.5f, lz1 - 1.6f })
                {
                    var ac = PlaceAsset(roofVentModel, root, new Vector3(face - 0.38f, baseY + 3.2f, z), 1.1f);
                    ac.name = "Facade AC unit";
                }

            // The skip on the kerb past the last roof: the step on the way back down.
            var skip = RooftopCourse.Skip;
            var yellow = DashCity.Mat(this, "skipYellow", new Color(0.95f, 0.72f, 0.08f));
            var rubble = DashCity.Mat(this, "skipRubble", new Color(0.4f, 0.36f, 0.32f));
            float sz = s + skip.Mid, height = skip.top - kerb;
            DashCity.Part(root, new Vector3(skip.x, kerb + height * 0.5f, sz), new Vector3(2.0f, height, skip.z1 - skip.z0), yellow);
            DashCity.Part(root, new Vector3(skip.x, skip.top - 0.03f, sz), new Vector3(2.1f, 0.06f, skip.z1 - skip.z0 + 0.1f), steel);
            for (int i = 0; i < 5; i++)
                DashCity.Part(root, new Vector3(skip.x - 0.5f + (i % 3) * 0.5f, skip.top + 0.05f, sz - 1.6f + i * 0.8f), new Vector3(0.45f, 0.18f, 0.5f), rubble);
            DashCity.Part(root, new Vector3(skip.x - 1.02f, kerb + height * 0.55f, sz), new Vector3(0.03f, 0.3f, 3f), DashCity.Mat(this, "skipStripe", new Color(0.1f, 0.1f, 0.1f)));
        }

        /// One occupied building of the shortcut row. Its street face is at the facade line the climb rides,
        /// its roof is the course. `clearFrom` keeps awnings off the face where the climb passes.
        void RoofBuilding(Transform parent, float z0, float z1, float baseY, float top, int seed, float clearFrom,
            Material deck, Material coping, Material steel, Material cyan, Material pink, Material amber)
        {
            float front = SurfaceTraversal.FacadeX, len = z1 - z0, zc = (z0 + z1) * 0.5f;
            int variant = Mathf.Abs(seed) % 5;
            Color[] tones = { new(.32f, .3f, .42f), new(.24f, .3f, .4f), new(.42f, .3f, .36f), new(.22f, .26f, .3f), new(.36f, .34f, .3f) };
            var body = DashCity.Mat(this, "roofBlock-" + variant, tones[variant]);
            if (masonryTexture && variant % 2 == 0)
            {
                body.SetTexture("_BaseMap", masonryTexture); body.SetFloat("_WorldTextureScale", .5f);
                body.SetTexture("_BumpMap", masonryNormal); body.SetFloat("_BumpScale", .6f);
            }
            var warm = DashCity.Mat(this, "occupied-window", new Color(1f, .65f, .3f), 1.1f);
            var cool = DashCity.Mat(this, "office-window", new Color(.35f, .65f, 1f), .65f);
            var dark = DashCity.Mat(this, "architectural-glass", new Color(.085f, .18f, .23f));
            var trim = DashCity.Mat(this, "limestone-trim", new Color(.59f, .56f, .49f));
            void Box(float x, float y, float z, float sx, float sy, float sz, Material m) =>
                DashCity.Part(parent, new Vector3(x, y, z), new Vector3(sx, sy, sz), m);

            float height = top - baseY;
            Box((front + RoofBack) * 0.5f, baseY + height * 0.5f - 0.1f, zc, RoofBack - front, height - 0.2f, len, body);
            // Roof: membrane deck flush with the course, coping on every edge, and lit warning strips at gaps.
            Box((front + RoofBack) * 0.5f, top - 0.06f, zc, RoofBack - front, 0.12f, len, deck);
            Box(front + 0.15f, top + 0.25f, zc, 0.3f, 0.5f, len, coping);
            Box(RoofBack - 0.15f, top + 0.25f, zc, 0.3f, 0.5f, len, coping);
            foreach (float ez in new[] { z0 + 0.08f, z1 - 0.08f })
                Box((front + RoofBack) * 0.5f, top + 0.015f, ez, RoofBack - front - 0.6f, 0.03f, 0.16f, amber);
            Box(front + 0.02f, top + 0.52f, zc, 0.06f, 0.05f, len, cyan);
            for (float seam = z0 + 4f; seam < z1 - 1f; seam += 4f)
                Box(SurfaceTraversal.RoofX, top + 0.004f, seam, 7.6f, 0.008f, 0.05f, steel);

            // Street face: ground-floor shopfront with a neon band, then window bays floor by floor.
            Box(front - 0.04f, baseY + 1.7f, zc, 0.08f, 2.6f, len - 1.2f, (seed & 1) == 0 ? warm : cool);
            Box(front - 0.06f, baseY + 3.25f, zc, 0.06f, 0.18f, len - 0.6f, (seed % 3) == 0 ? pink : cyan);
            if (z1 > clearFrom + 6f)
            {
                float az0 = Mathf.Max(z0 + 1f, clearFrom), az1 = z1 - 1f;
                var awning = DashCity.Part(parent, new Vector3(front - 0.7f, baseY + 3.05f, (az0 + az1) * 0.5f), new Vector3(1.4f, 0.1f, az1 - az0), steel);
                awning.transform.rotation = Quaternion.Euler(0f, 0f, -10f);
                Box(front - 1.38f, baseY + 2.92f, (az0 + az1) * 0.5f, 0.05f, 0.12f, az1 - az0, amber);
            }
            for (float y = baseY + 4.9f; y < top - 1.5f; y += 3.2f)
            {
                Box(front - 0.05f, y - 1.45f, zc, 0.2f, 0.14f, len, trim);
                int bays = Mathf.Max(2, Mathf.RoundToInt(len / 3.6f));
                float bay = len / bays;
                for (int k = 0; k < bays; k++)
                {
                    int lit = (Mathf.RoundToInt(y) * 7 + k * 5 + seed) % 7;
                    Box(front - 0.03f, y, z0 + bay * (k + 0.5f), 0.08f, 2.0f, bay - 0.9f, lit < 2 ? warm : lit == 2 ? cool : dark);
                }
            }
            // A vertical neon blade sign on the corner, and the wall-ride arrows up the facade at the climb.
            if (len > 14f)
            {
                Box(front - 0.5f, (baseY + 4f + top - 1f) * 0.5f, z1 - 1.2f, 0.12f, top - baseY - 6f, 0.9f, steel);
                Box(front - 0.57f, (baseY + 4f + top - 1f) * 0.5f, z1 - 1.2f, 0.02f, top - baseY - 6.4f, 0.7f, (seed % 2) == 0 ? pink : cyan);
            }

            // Residents up on their roof: a couple at the far parapet watching the city, out of the lanes.
            if (!people) people = Resources.Load<PeopleCatalogue>("PeopleCatalogue");
            if (people && people.bodies != null && people.bodies.Length > 0 && len > 14f)
                for (int k = 0; k < 2; k++)
                {
                    var person = RentPerson(people.bodies[(seed + k) & 1], parent);
                    person.transform.position = new Vector3(RoofBack - 0.75f, top, zc + (k == 0 ? -1.4f : -0.6f));
                    person.transform.rotation = Quaternion.Euler(0f, k == 0 ? -70f : -120f, 0f);
                    person.Dress(people, seed * 3 + k, k == 0 ? Pedestrian.Chat : Pedestrian.Idle);
                }
            // Roof dressing kept off the three lanes: plant, tanks and a lit billboard on the far edge.
            if (roofTankModel && len > 12f) PlaceAsset(roofTankModel, parent, new Vector3(RoofBack - 0.8f, top, z0 + len * 0.3f), 1.1f);
            if (dashPlanter) Instantiate(dashPlanter, new Vector3(RoofBack - 0.9f, top, z1 - 4f), Quaternion.identity, parent).name = "Roof garden planter";
            if (hologramTextures != null && hologramTextures.Length > 0 && len > 16f)
            {
                int ti = Mathf.Abs(seed) % hologramTextures.Length;
                var display = DashCity.Mat(this, "nightDisplay-" + ti, Color.white, 2.5f);
                display.SetTexture("_BaseMap", hologramTextures[ti]);
                display.SetFloat("_EmissionUsesAlbedo", 1);
                var board = new GameObject("Rooftop billboard").transform;
                board.SetParent(parent, false);
                board.position = new Vector3(RoofBack + 0.4f, top + 4.4f, zc);
                board.rotation = Quaternion.Euler(0f, -128f, 0f);
                var frame = DashCity.Part(board, Vector3.zero, new Vector3(7.2f, 4.2f, 0.3f), steel);
                frame.transform.localPosition = Vector3.zero; frame.transform.localRotation = Quaternion.identity;
                var face = DashCity.Part(board, Vector3.zero, new Vector3(6.8f, 3.8f, 0.04f), display);
                face.transform.localPosition = new Vector3(0f, 0f, -0.17f); face.transform.localRotation = Quaternion.identity;
                foreach (float lx in new[] { -2.6f, 2.6f })
                {
                    var leg = DashCity.Part(board, Vector3.zero, new Vector3(0.16f, 2.6f, 0.16f), steel);
                    leg.transform.localPosition = new Vector3(lx, -3.3f, 0.1f); leg.transform.localRotation = Quaternion.identity;
                }
            }
        }

        /// A glass skybridge across an alley gap in one rooftop lane: no jump needed if you pick its lane.
        void Skybridge(Transform parent, float z0, float z1, int lane, float top, Material glass, Material cyan, Material steel)
        {
            float x = SurfaceTraversal.RoofX + Tuning.LaneX[lane], zc = (z0 + z1) * 0.5f, len = z1 - z0 + 0.4f;
            DashCity.Part(parent, new Vector3(x, top - 0.08f, zc), new Vector3(2f, 0.1f, len), glass);
            foreach (float side in new[] { -1f, 1f })
            {
                DashCity.Part(parent, new Vector3(x + side * 1f, top + 0.55f, zc), new Vector3(0.05f, 1.1f, len), glass);
                DashCity.Part(parent, new Vector3(x + side * 1f, top + 1.12f, zc), new Vector3(0.08f, 0.05f, len), cyan);
                DashCity.Part(parent, new Vector3(x + side * 1f, top - 0.16f, zc), new Vector3(0.12f, 0.12f, len), steel);
            }
        }

        /// Rows of roof hazards on one building: hop the vent units, steer round the water tanks and plant
        /// housings, slide under the pipe gantries. One lane is always open and carries the burgers.
        void RoofHazards(RooftopCourse.Block block, float from, bool last)
        {
            float to = block.z1 - (last ? 14f : 6f);
            int open = 1;
            for (float z = Mathf.Max(from, block.z0 + 5f); z < to; z += 12.5f)
            {
                open = Mathf.Clamp(open + Random.Range(-1, 2), 0, Tuning.Lanes - 1);
                for (int lane = 0; lane < Tuning.Lanes; lane++)
                {
                    if (lane == open || Random.value < 0.3f) { RoofBurgers(lane, z); continue; }
                    float r = Random.value;
                    if (r < 0.45f) RoofVent(lane, z);
                    else if (r < 0.75f) RoofPipes(lane, z);
                    else RoofTank(lane, z);
                }
            }
        }

        Vector3 RoofPoint(int lane, float height, float z) =>
            SurfaceTraversal.Point(RunSurface.Rooftop, Tuning.LaneX[lane], height) + Vector3.forward * z;

        TrackObject RegisterRoof(GameObject go, TrackKind kind, int lane, float half)
        {
            Register(go, kind, lane, half, 0, 0f);
            var o = live[live.Count - 1];
            o.surface = RunSurface.Rooftop;
            return o;
        }

        void RoofVent(int lane, float z)
        {
            var go = Pool.Rent("roof-vent", () =>
            {
                var root = new GameObject("Roof ventilation unit / jump");
                PlaceAsset(roofVentModel, root.transform, Vector3.zero, 1.5f);
                return root;
            }, RoofPoint(lane, 0f, z), Quaternion.identity);
            var o = RegisterRoof(go, TrackKind.Low, lane, 0.75f);
            o.height = 0.74f; o.halfWidth = 0.675f;
        }

        void RoofTank(int lane, float z)
        {
            var go = Pool.Rent("roof-plant", () =>
            {
                var root = new GameObject("Roof plant housing / steer");
                var housing = DashCity.Mat(this, "roofPlantHousing", new Color(0.3f, 0.33f, 0.36f));
                var louvre = DashCity.Mat(this, "roofPlantLouvre", new Color(0.08f, 0.09f, 0.1f));
                var lamp = DashCity.Mat(this, "roofNeonAmber", new Color(1f, 0.55f, 0.1f), 1.8f);
                DashCity.Part(root.transform, new Vector3(0f, 1.05f, 0f), new Vector3(1.8f, 2.1f, 2.4f), housing);
                for (int i = 0; i < 6; i++)
                    DashCity.Part(root.transform, new Vector3(0f, 0.45f + i * 0.26f, -1.21f), new Vector3(1.6f, 0.06f, 0.03f), louvre);
                DashCity.Part(root.transform, new Vector3(0f, 2.13f, 0f), new Vector3(1.6f, 0.06f, 2.2f), louvre);
                DashCity.Part(root.transform, new Vector3(0.8f, 2.25f, -1.1f), new Vector3(0.12f, 0.12f, 0.12f), lamp);
                return root;
            }, RoofPoint(lane, 0f, z), Quaternion.identity);
            var o = RegisterRoof(go, TrackKind.Block, lane, 1.2f);
            o.height = 2.1f; o.halfWidth = 0.9f; o.heavy = true;
        }

        void RoofPipes(int lane, float z)
        {
            var go = Pool.Rent("roof-pipes", () =>
            {
                var root = new GameObject("Roof pipe gantry / slide");
                var steel = DashCity.Mat(this, "roofSteel", new Color(0.16f, 0.18f, 0.21f));
                var pipe = DashCity.Mat(this, "roofPipe", new Color(0.62f, 0.44f, 0.18f));
                var cyan = DashCity.Mat(this, "roofNeonCyan", new Color(0.1f, 0.85f, 1f), 2.2f);
                foreach (float side in new[] { -1f, 1f })
                    DashCity.Part(root.transform, new Vector3(side * 0.95f, 1.25f, 0f), new Vector3(0.12f, 2.5f, 0.12f), steel);
                foreach (float py in new[] { 2.12f, 2.38f })
                {
                    var p = DashCity.Part(root.transform, new Vector3(0f, py, 0f), new Vector3(0.22f, 1.05f, 0.22f), pipe, PrimitiveType.Cylinder);
                    p.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                }
                DashCity.Part(root.transform, new Vector3(0f, 1.98f, -0.13f), new Vector3(1.8f, 0.05f, 0.03f), cyan);
                return root;
            }, RoofPoint(lane, 0f, z), Quaternion.identity);
            var o = RegisterRoof(go, TrackKind.Overhead, lane, 0.3f);
            o.height = 2.5f; o.clearance = 1.98f; o.halfWidth = 1.0f;
        }

        void RoofBurgers(int lane, float z)
        {
            for (int i = 0; i < 4; i++)
            {
                var go = RentBurger("roof-burger", RoofPoint(lane, 0.45f, z + i * 2.5f), Quaternion.identity, true);
                go.name = "Rooftop burger";
                var o = RegisterRoof(go, TrackKind.Pickup, lane, 0.4f);
                o.points = 25; // roof lines score more than street burgers
            }
        }
    }
}
