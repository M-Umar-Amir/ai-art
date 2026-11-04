using System;
using System.Collections.Generic;
using DallasDash.Core;
using DallasDash.Gameplay;
using UnityEditor;
using UnityEngine;

namespace DallasDash.EditorTools
{
    public static class DashValidation
    {
        static int assertions;
        static void Check(bool ok, string why) { assertions++; if (!ok) throw new Exception("Dash validation: " + why); }

        /// World-space bounds of a prefab's renderers, instanced the same way the bootstrap measures hurdles.
        static Bounds RenderBounds(GameObject prefab)
        {
            var go = PrefabUtility.IsPartOfPrefabAsset(prefab) ? (GameObject)PrefabUtility.InstantiatePrefab(prefab) : UnityEngine.Object.Instantiate(prefab);
            go.SetActive(true);
            Bounds b = default; bool any = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled) continue;
                var measured = r is SkinnedMeshRenderer skin && skin.sharedMesh
                    ? new Bounds(skin.transform.TransformPoint(skin.sharedMesh.bounds.center), Vector3.Scale(skin.sharedMesh.bounds.size, skin.transform.lossyScale)) : r.bounds;
                if (!any) { b = measured; any = true; } else b.Encapsulate(measured);
            }
            UnityEngine.Object.DestroyImmediate(go);
            return b;
        }
        /// A prop prefab built by PropModels, loaded the way the bootstrap references it.
        static GameObject Prop(string name) =>
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Props/" + name + ".prefab");

        [MenuItem("Dash Riders/Validate coupons and progression")]
        public static void Run()
        {
            var now = new DateTime(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc);
            var thinBarrier = new Bounds(new Vector3(0f, 0.5f, 10f), new Vector3(1.8f, 1f, 0.1f));
            Check(VehicleDynamics.Sweep(new Vector3(0, 0, 0), new Vector3(0, 0, 20), thinBarrier, 0.3f, 0.5f, 2f), "high-speed body cannot tunnel through a thin hurdle");
            Check(!VehicleDynamics.Sweep(new Vector3(2.3f, 0, 0), new Vector3(2.3f, 0, 20), thinBarrier, 0.3f, 0.5f, 2f), "adjacent clear lane does not collide");
            Check(!VehicleDynamics.Sweep(new Vector3(0, 1.2f, 0), new Vector3(0, 1.2f, 20), thinBarrier, 0.3f, 0.5f, 2f), "feet above the hurdle clear it");
            var beam = new Bounds(new Vector3(0, 2f, 10), new Vector3(2f, 0.8f, 0.3f));
            Check(VehicleDynamics.Sweep(Vector3.zero, new Vector3(0, 0, 20), beam, 0.3f, 0.5f, 2f), "standing body hits the overhead beam");
            Check(!VehicleDynamics.Sweep(Vector3.zero, new Vector3(0, 0, 20), beam, 0.3f, 0.5f, 1f), "crouched body clears the overhead beam");
            Check(VehicleDynamics.Runner.acceleration > VehicleDynamics.Wheel.acceleration && VehicleDynamics.Wheel.radius > VehicleDynamics.Runner.radius, "vehicle inertia and collision bodies are distinct");
            var ledger = new CouponLedger();
            // Burgers only: every price is positive burgers, the free district is free, payouts are in burgers.
            Check(BurgerPrices.District[0] == 0 && BurgerPrices.District[1] > 0 && BurgerPrices.District[2] > BurgerPrices.District[1], "district unlocks are burger purchases");
            Check(BurgerPrices.Bolt > 0 && BurgerPrices.Outfit > 0 && BurgerPrices.Coupon > 0, "crew, outfit and coupon prices are burgers");
            Check(PowerUps.UpgradeCost.Length == PowerUps.MaxLevel && PlayerProfile.DailyRewards.Length == 7, "upgrade and daily tables are burger tables");
            Check(BurgerPrices.OnTimeTip > BurgerPrices.LateTip && BurgerPrices.LateTip > 0, "deliveries tip burgers, more when on time");
            Check(RidersEconomy.PitPlan(1f, 100).cost == 4 && RidersEconomy.PitPlan(1f, 2).cost == 2 && RidersEconomy.PitPlan(0.1f, 0).cost == 0, "kiosk charge is priced in burgers and never overspends");
            var codes = new HashSet<string>();
            for (int i = 0; i < 1000; i++)
            {
                var c = ledger.Issue(now);
                Check(c != null && codes.Add(c.code), "unique code per issue");
                Check(c.expiresUtcTicks - c.issuedUtcTicks == TimeSpan.TicksPerDay, "exact 24-hour lifetime");
            }
            Check(ledger.lastSerial == "RS", "1000th expanding serial persisted");
            Check(CouponLedger.IncrementSerial("Z") == "10", "counter grows after Z");
            Check(CouponLedger.IncrementSerial(new string('Z', 100)) == "1" + new string('0', 100), "counter grows beyond fixed integer sizes");
            Check(CouponLedger.ValidFormat(ledger.coupons[0].code), "versioned format and checksum");
            string altered = ledger.coupons[0].code.Substring(0, ledger.coupons[0].code.Length - 1) + (ledger.coupons[0].code.EndsWith("0") ? "1" : "0");
            Check(!CouponLedger.ValidFormat(altered) && ledger.Redeem(altered, now) == "INVALID", "mistyped code rejected");
            var pruned = JsonUtility.FromJson<CouponLedger>(JsonUtility.ToJson(ledger));
            string serialBeforePrune = pruned.lastSerial;
            pruned.coupons.Clear();
            var afterPrune = pruned.Issue(now);
            Check(pruned.lastSerial == CouponLedger.IncrementSerial(serialBeforePrune) && !codes.Contains(afterPrune.code), "archiving history does not recycle coupon identities");
            var first = ledger.coupons[0];
            Check(ledger.Status(first, now.AddHours(23).AddMinutes(59)) == "ACTIVE", "valid before deadline");
            Check(ledger.Status(first, now.AddHours(24)) == "EXPIRED", "expires at the exact deadline");
            Check(ledger.Redeem(first.code, now.AddHours(24)) == "EXPIRED", "expired code cannot redeem");
            Check(ledger.Status(first, now.AddHours(-1)) == "EXPIRED", "clock rollback cannot revive observed expiry");
            Check(ledger.Redeem("MADE-UP", now) == "UNKNOWN", "unknown code rejected");
            Check(ledger.Redeem(null, now) == "UNKNOWN", "null code rejected");
            var fresh = ledger.Issue(now.AddDays(1));
            Check(ledger.Redeem("  " + fresh.code.ToLowerInvariant() + "  ", now.AddDays(1).AddSeconds(1)) == "REDEEMED", "first use succeeds and normalizes input");
            Check(ledger.Redeem(fresh.code, now.AddDays(1).AddSeconds(1)) == "USED", "immediate second use rejected");
            string json = JsonUtility.ToJson(ledger);
            var reloaded = JsonUtility.FromJson<CouponLedger>(json);
            Check(reloaded.Redeem(fresh.code, now.AddDays(1).AddHours(2)) == "USED", "used state survives restart");
            Check(reloaded.Status(reloaded.coupons.Find(c => c.code == first.code), now) == "EXPIRED", "expiry watermark survives restart");
            Check(reloaded.coupons.Count == 1001, "all issued history survives restart");
            foreach (var p in (PowerUp[])Enum.GetValues(typeof(PowerUp)))
            {
                Check((int)p < PowerUps.Count && PowerUps.Names[(int)p] != null && PowerUps.IconKeys[(int)p] != null, "every power-up has HUD/shop metadata");
                Check(PowerUps.Duration(p) > 0, "positive power-up duration");
            }
            // Wall runs anywhere a facade stands (client, 3 Nov); never across a junction, the rooftop block row or a kiosk.
            Check(!DashRun.WallZone(420) && DashRun.WallAvailable(-1, 430f) && DashRun.WallAvailable(1, 430f), "wall runs are not zoned");
            Check(!DashRun.WallAvailable(-1, 70f) && !DashRun.WallAvailable(1, 490f), "no wall across a cross street");
            Check(!DashRun.WallAvailable(1, RooftopCourse.Start + 40f) && DashRun.WallAvailable(-1, RooftopCourse.Start + 40f), "right wall belongs to the rooftop block row on the shortcut");
            Check(!DashRun.WallAvailable(1, FuelStops.FirstStation) && DashRun.WallAvailable(-1, FuelStops.FirstStation), "no right wall along a kiosk");
            Check(SurfaceTraversal.Anchor(RunSurface.LeftWall).y - 0.4f > 3.2f && SurfaceTraversal.Anchor(RunSurface.LeftWall).y + 0.4f < 6.85f, "wall-run line clears awnings and terraces");
            // Two rooftop shortcuts per 960 m, each clear of junctions and kiosks.
            Check(Mathf.Approximately(RooftopCourse.ZoneStart(1) - RooftopCourse.ZoneStart(0), 480f), "rooftop shortcut every 480 m");
            for (int roofZone = 0; roofZone < 8; roofZone++)
                for (float z = RooftopCourse.ZoneStart(roofZone) - 60f; z <= RooftopCourse.ZoneStart(roofZone) + RooftopCourse.StreetLanding + 12f; z += 2f)
                {
                    Check(!TrackSpawner.NearJunction(z) || DashRun.District == 1, "no junction inside a rooftop shortcut");
                    Check(!FuelStops.KioskNear(z, 0f), "no kiosk inside a rooftop shortcut");
                }
            Check(!DashRun.GravityZone(700) && !DashRun.GravityZone(745), "gravity flip removed (real-world physics)");
            Check(DashRun.District == 1 || TrackSpawner.Junction(480f) && !TrackSpawner.Junction(500f) && !TrackSpawner.Junction(RooftopCourse.Start), "junctions sit on their own segments, clear of the rooftop ramp");
            Check(!DashCity.FacadeZone(RooftopCourse.Start - 20f, 20f) && !DashRun.WallZone(RooftopCourse.Start + RooftopCourse.Length + 40f), "rooftop shortcut and wall ride never overlap");
            var traversal = new SurfaceTraversal();
            foreach (RunSurface surface in Enum.GetValues(typeof(RunSurface)))
            {
                Vector3 contact = traversal.Origin;
                traversal.Change(surface, contact, 0f);
                Vector3 before = traversal.Origin;
                traversal.Tick(0.01f);
                Check(Vector3.Distance(traversal.Origin, before) < 0.05f, "surface transfer begins continuously: " + surface);
                for (int frame = 0; frame < 30; frame++) traversal.Tick(1f / 60f);
                Check(!traversal.Moving && Vector3.Distance(traversal.Origin, SurfaceTraversal.Anchor(surface)) < 0.001f, "surface contact completes: " + surface);
                Vector3 jump = traversal.Position(0f, 1f, 50f) - traversal.Position(0f, 0f, 50f);
                Check(Vector3.Dot(jump, traversal.Normal) > 0.999f, "jump travels away from contact surface: " + surface);
                Check(Mathf.Abs(Vector3.Dot(traversal.Normal, traversal.Rotation * Vector3.right)) < 0.001f, "surface lane axis remains perpendicular: " + surface);
            }
            traversal.Reset();
            traversal.Change(RunSurface.Ceiling, Vector3.zero, 0f); traversal.Tick(0.1f);
            Vector3 interrupted = traversal.Origin;
            traversal.Change(RunSurface.Road, interrupted, 0f);
            Check(traversal.Origin == interrupted, "zone exit does not teleport mid-transition");
            traversal.Tick(1f); Check(traversal.Origin == Vector3.zero, "zone exit returns to road");
            // Rooftop shortcut: from the right-hand lane, up the facade (wheels on the wall), onto the roofs.
            traversal.Reset();
            var start = new Vector3(Tuning.LaneX[2], 0f, 0f);
            // Rooftop parkour: every hop lands on a real object (truck roof, fire-escape landing, roof).
            const float speed = 25f, zone = RooftopCourse.Start;
            traversal.Ascend(start, Tuning.LaneX[0], speed, zone, zone);
            Check(Vector3.Distance(traversal.Position(Tuning.LaneX[0], 0f, 0f), start) < 0.01f, "ascent starts where the rider is");
            bool onTruck = false, onLanding = false; float clock = 0f;
            for (int frame = 0; frame < 120 && traversal.Moving; frame++)
            {
                traversal.Tick(1f / 60f); clock += 1f / 60f;
                var at = traversal.Position(Tuning.LaneX[0], 0, 0); float z = zone + clock * speed;
                if (Mathf.Abs(at.y - RooftopCourse.Truck.top) < 0.15f && Mathf.Abs(at.x - RooftopCourse.Truck.x) < 0.3f && z > zone + RooftopCourse.Truck.z0 && z < zone + RooftopCourse.Truck.z1) onTruck = true;
                if (Mathf.Abs(at.y - RooftopCourse.Landing.top) < 0.15f && Mathf.Abs(at.x - RooftopCourse.Landing.x) < 0.3f && z > zone + RooftopCourse.Landing.z0 && z < zone + RooftopCourse.Landing.z1) onLanding = true;
            }
            Check(onTruck, "parkour line lands on the parked truck's roof");
            Check(onLanding, "parkour line lands on the fire-escape landing");
            Check(!traversal.Moving && traversal.Surface == RunSurface.Rooftop && Mathf.Abs(traversal.Position(Tuning.LaneX[0], 0, 0).y - SurfaceTraversal.RoofHeight) < 0.01f, "parkour line lands on the roofs");
            traversal.Descend(traversal.Position(Tuning.LaneX[1], 0, 0), Tuning.LaneX[2], speed, zone, zone + RooftopCourse.DropAt);
            for (int frame = 0; frame < 120; frame++) traversal.Tick(1f / 60f);
            Check(traversal.Surface == RunSurface.Road && Vector3.Distance(traversal.Position(Tuning.LaneX[2], 0, 0), start) < 0.01f, "way down lands in the street's right-hand lane");
            var blocks = RooftopCourse.Blocks(0);
            Check(blocks.Count >= 3 && blocks[0].z0 < RooftopCourse.Start - 1f && blocks[0].z1 - RooftopCourse.Start > 26f, "first roof takes the whole climb");
            for (int i = 1; i < blocks.Count; i++) Check(blocks[i].z0 - blocks[i - 1].z1 < 7f && blocks[i].z0 > blocks[i - 1].z1, "alley gaps are jumpable");
            Check(RooftopCourse.Supported(blocks[0].z1 - 1f, 0f) && (blocks[1].bridgeLane == 1 || !RooftopCourse.Supported(blocks[0].z1 + 1f, 0f)), "alley gaps have no roof except skybridges");
            traversal.Reset(); Check(traversal.Normal == Vector3.up && !traversal.Moving, "retry resets contact frame");
            foreach (string id in new[] { "ace", "nova", "bolt", "neon-block-a", "neon-block-b", "skybridge", "electric-board", "electric-pod" })
            {
                var prefab = ModelImport.Get(id);
                Check(prefab && prefab.GetComponentInChildren<MeshFilter>(), "real model imported: " + id);
            }
            var city = ModelImport.Get("neon-block-a"); bool hasDarkMaterial = false;
            foreach (var renderer in city.GetComponentsInChildren<Renderer>()) foreach (var material in renderer.sharedMaterials)
                if (material.HasProperty("_BaseColor"))
                {
                    var color = material.GetColor("_BaseColor");
                    if ((color.r + color.g + color.b) / 3f < 0.7f) hasDarkMaterial = true;
                }
            Check(hasDarkMaterial, "architecture retains source colours instead of becoming white");
            // The client's original meshes must remain the live crew, rather than replacement people.
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity");
            var liveLook = UnityEngine.Object.FindFirstObjectByType<RiderLook>(FindObjectsInactive.Include);
            Check(liveLook && liveLook.seated.Length == 3 && liveLook.standing.Length == 3, "all original crew slots exist");
            var liveTrack = UnityEngine.Object.FindFirstObjectByType<TrackSpawner>(FindObjectsInactive.Include);
            Check(liveTrack && liveTrack.powerUpModels != null && liveTrack.powerUpModels.Length == 6, "all six power-up models assigned");
            foreach (var model in liveTrack.powerUpModels)
                Check(model && model.GetComponentsInChildren<Renderer>(true).Length > 0, "power-up contains model renderers");
            foreach (var pickup in liveTrack.pickups)
                Check(pickup && pickup.prefab && pickup.prefab.GetComponentsInChildren<Renderer>(true).Length > 0, "pickup has a real model: " + pickup.name);
            foreach (var obstacle in liveTrack.obstacles)
                Check(obstacle && obstacle.prefab && obstacle.prefab.GetComponentsInChildren<Renderer>(true).Length > 0, "obstacle has a real model: " + obstacle.name);
            Check(liveTrack.routeHurdleModel, "traversal hurdle model assigned");
            Check(liveTrack.pickups.Length == 1 && liveTrack.pickups[0].id == "burger", "burgers are the only pickup");
            for (int character = 0; character < 3; character++)
            {
                string id = character == 0 ? "ace" : character == 1 ? "nova" : "bolt";
                var original = ModelImport.Get(id);
                Check(original, "original crew prefab exists: " + id);
                var bounds = RenderBounds(original);
                Check(bounds.size.y > 1.8f && bounds.size.y < 2.3f, "original crew scale: " + id);
                Check(Mathf.Abs(bounds.min.y) < .14f, "original crew is grounded: " + id);
                var meshes = new System.Collections.Generic.HashSet<Mesh>();
                foreach (var filter in original.GetComponentsInChildren<MeshFilter>(true))
                {
                    Check(filter.sharedMesh && filter.sharedMesh.vertexCount > 0, "original crew mesh is populated: " + id);
                    meshes.Add(filter.sharedMesh);
                }
                Check(meshes.Count > 0, "original crew geometry exists: " + id);
                if (character < 2)
                {
                    // Ace and Nova ride e-bikes: the seated slot is the original mesh re-posed with pedal morphs.
                    var slot = liveLook.seated[character];
                    Check(slot, "live cyclist slot exists: " + id);
                    var skin = slot.GetComponentInChildren<SkinnedMeshRenderer>(true);
                    int originalVertices = 0;
                    foreach (var mesh in meshes) originalVertices += mesh.vertexCount;
                    Check(skin && skin.sharedMesh && skin.sharedMesh.vertexCount == originalVertices, "cyclist is the original mesh re-posed: " + id);
                    Check(skin && skin.sharedMesh.GetBlendShapeIndex("Pedal180") >= 0 && skin.sharedMesh.GetBlendShapeIndex("Stand") >= 0, "cyclist has pedal and stand morphs: " + id);
                    var motion = slot.GetComponentInChildren<CyclistMotion>(true);
                    Check(motion && motion.crank && motion.frontWheel && motion.rearWheel, "e-bike crank and wheels are animated: " + id);
                    var ride = RenderBounds(slot);
                    Check(ride.min.y > -0.05f && ride.min.y < 0.06f && ride.size.z > 1.6f && ride.size.z < 2.3f, "e-bike stands on the road at bike length: " + ride.size.ToString("0.00"));
                }
                if (character < 2)
                {
                    var runner = liveLook.standing[character];
                    var stride = runner ? runner.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
                    int originalVertices = 0;
                    foreach (var mesh in meshes) originalVertices += mesh.vertexCount;
                    Check(stride && stride.sharedMesh.vertexCount == originalVertices && stride.sharedMesh.GetBlendShapeIndex("Run180") >= 0, "on-foot slot is the original mesh with a running stride: " + id);
                    Check(runner.GetComponentInChildren<RunnerMotion>(true), "on-foot stride is animated: " + id);
                }
                foreach (var slot in character < 2 ? new GameObject[0] : new[] { liveLook.seated[character], liveLook.standing[character] })
                {
                    Check(slot, "live crew slot exists: " + id);
                    bool usesOriginal = false;
                    foreach (var filter in slot.GetComponentsInChildren<MeshFilter>(true))
                        if (meshes.Contains(filter.sharedMesh)) usesOriginal = true;
                    Check(usesOriginal, "live crew slot uses original mesh: " + id);
                }
            }
            // Real people (client): rigged pedestrians with outfits, hair and street clips that bind to their bones.
            var people = Resources.Load<PeopleCatalogue>("PeopleCatalogue");
            Check(people && people.bodies.Length == 2 && people.maleOutfits.Length >= 4 && people.femaleOutfits.Length >= 4, "people catalogue has bodies and outfits");
            foreach (var person in people.bodies)
            {
                Check(person.animator && person.animator.runtimeAnimatorController && person.body && person.hair.Length >= 2, "pedestrian is rigged and dressed: " + person.name);
                var clips = person.animator.runtimeAnimatorController.animationClips;
                Check(clips.Length >= 5, "pedestrian has street clips: " + person.name);
                foreach (var clip in clips)
                {
                    int bound = 0, total = 0;
                    foreach (var b in AnimationUtility.GetCurveBindings(clip)) { total++; if (AnimationUtility.GetAnimatedObject(person.animator.gameObject, b)) bound++; }
                    Check(total > 0 && bound >= total * 0.9f, $"clip {clip.name} binds to {person.name}'s bones: {bound}/{total}");
                }
                // Sample every clip on the real skeleton: a broken clip (it happened: meshopt-compressed source
                // data) throws the bones to infinity and the crowd silently vanishes.
                var probe = UnityEngine.Object.Instantiate(person.gameObject);
                var probePerson = probe.GetComponent<Pedestrian>();
                foreach (var clip in clips)
                {
                    clip.SampleAnimation(probePerson.animator.gameObject, clip.length * 0.37f);
                    var baked = new Mesh(); probePerson.body.BakeMesh(baked);
                    var posed = baked.bounds;
                    Check(posed.size.y > 1.3f && posed.size.y < 2.3f && posed.size.x < 2.2f && Mathf.Abs(posed.center.x) < 1f && Mathf.Abs(posed.center.z) < 1f && posed.center.y > 0.4f && posed.center.y < 1.4f,
                        $"clip {clip.name} poses {person.name} in place at person size: {posed.size:0.00}");
                    UnityEngine.Object.DestroyImmediate(baked);
                }
                UnityEngine.Object.DestroyImmediate(probe);
                var personBounds = RenderBounds(person.gameObject);
                Check(personBounds.size.y > 1.6f && personBounds.size.y < 2.0f && Mathf.Abs(personBounds.min.y) < 0.08f, "pedestrian is person height and grounded: " + personBounds.size.ToString("0.00"));
            }
            Check(UnityEngine.Object.FindFirstObjectByType<DistrictLife>(FindObjectsInactive.Include), "district life (residents) is in the scene");
            // Real-life hurdles only: no abstract gates or player-crew clones on the road.
            foreach (var obstacle in liveTrack.obstacles) Check(obstacle.id != "delivery-bot", "no crew clone used as a hurdle");
            // Authored hurdles and street dressing: the AI photogrammetry meshes are gone from the
            // obstacles, and every prop is merged to a handful of renderers so it can be pooled.
            var barrier = Prop("barricade");
            Check(barrier, "authored low hurdle exists");
            var bb = RenderBounds(barrier);
            Check(bb.size.x > 1.7f && bb.size.x < 2.1f, "low hurdle spans the lane: " + bb.size.x.ToString("0.00") + " m");
            Check(bb.max.y > 0.8f && bb.max.y < 1.05f, "low hurdle stays hoppable: " + bb.max.y.ToString("0.00") + " m");
            Check(bb.min.y > -0.02f && bb.min.y < 0.06f, "low hurdle stands on the road: " + bb.min.y.ToString("0.00") + " m");
            var gate = Prop("cargoGate");
            Check(gate, "authored cargo gate exists");
            var gb = RenderBounds(gate);
            Check(gb.size.x > 1.8f && gb.max.y > 2.3f && gb.max.y < 2.65f,
                "cargo gate clears a sliding rider and blocks a standing one: " + gb.max.y.ToString("0.00") + " m");
            var pod = Prop("deliveryPod");
            Check(pod, "authored delivery pod exists");
            var pb = RenderBounds(pod);
            Check(pb.size.x > 1.7f && pb.size.x < 2.1f && pb.max.y > 1.9f && pb.max.y < 2.4f && pb.size.z > 3.2f && pb.size.z < 4f,
                "delivery pod fills a lane at vehicle scale: " + pb.size.ToString("0.00"));
            Check(pb.min.y > -0.02f && pb.min.y < 0.06f, "delivery pod wheels touch the road");
            var lamp = Prop("streetLamp");
            Check(lamp, "authored street lamp exists");
            var lb = RenderBounds(lamp);
            Check(lb.max.y > 4.5f && lb.max.y < 5.4f, "street lamp is street height: " + lb.max.y.ToString("0.00") + " m");
            var planter = Prop("streetPlanter");
            Check(planter, "authored planter exists");
            var plb = RenderBounds(planter);
            Check(plb.max.y > 1f && plb.max.y < 1.6f, "planter shrubs rise above the box: " + plb.max.y.ToString("0.00") + " m");
            foreach (var prop in new[] { barrier, gate, pod, lamp, planter })
            {
                int renderers = prop.GetComponentsInChildren<Renderer>().Length;
                Check(renderers > 0 && renderers <= 8, "prop is merged into a few draw calls: " + prop.name + " = " + renderers);
            }
            Debug.Log($"[DashRiders] PASS: {assertions} assertions (coupon uniqueness, expiry, one-use, save/reload, movement windows, power-ups)");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
