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
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Bounds b = default; bool any = false;
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
            {
                if (!r.enabled) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
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
            Check(ledger.Issue(now) == null, "cannot issue without earned badges");
            ledger.badges = 6000;
            var codes = new HashSet<string>();
            for (int i = 0; i < 1000; i++)
            {
                var c = ledger.Issue(now);
                Check(c != null && codes.Add(c.code), "unique code per issue");
                Check(c.expiresUtcTicks - c.issuedUtcTicks == TimeSpan.TicksPerDay, "exact 24-hour lifetime");
            }
            Check(ledger.badges == 3000, "badge debit matches issued coupons");
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
            Check(reloaded.badges == ledger.badges, "balance survives restart");
            Check(reloaded.coupons.Count == 1001, "all issued history survives restart");
            foreach (var p in (PowerUp[])Enum.GetValues(typeof(PowerUp)))
            {
                Check((int)p < PowerUps.Count && PowerUps.Names[(int)p] != null && PowerUps.IconKeys[(int)p] != null, "every power-up has HUD/shop metadata");
                Check(PowerUps.Duration(p) > 0, "positive power-up duration");
            }
            Check(!DashRun.WallZone(299) && DashRun.WallZone(300) && !DashRun.WallZone(390), "wall section bounds");
            Check(!DashRun.GravityZone(599) && DashRun.GravityZone(600) && !DashRun.GravityZone(690), "gravity section bounds");
            var traversal = new SurfaceTraversal();
            foreach (RunSurface surface in Enum.GetValues(typeof(RunSurface)))
            {
                traversal.Change(surface);
                Vector3 before = traversal.Origin;
                traversal.Tick(0.01f);
                Check(Vector3.Distance(traversal.Origin, before) < 0.05f, "surface transfer begins continuously: " + surface);
                for (int frame = 0; frame < 30; frame++) traversal.Tick(1f / 60f);
                Check(!traversal.Moving && Vector3.Distance(traversal.Origin, SurfaceTraversal.Anchor(surface)) < 0.001f, "surface contact completes: " + surface);
                Vector3 jump = traversal.Position(0f, 1f, 50f) - traversal.Position(0f, 0f, 50f);
                Check(Vector3.Dot(jump, traversal.Normal) > 0.999f, "jump travels away from contact surface: " + surface);
                Check(Mathf.Abs(Vector3.Dot(traversal.Normal, traversal.Rotation * Vector3.right)) < 0.001f, "surface lane axis remains perpendicular: " + surface);
            }
            traversal.Change(RunSurface.Ceiling); traversal.Tick(0.1f);
            Vector3 interrupted = traversal.Origin;
            traversal.Change(RunSurface.Road);
            Check(traversal.Origin == interrupted, "zone exit does not teleport mid-transition");
            traversal.Tick(1f); Check(traversal.Origin == Vector3.zero, "zone exit returns to road");
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
            // Authored crew models: real articulated geometry, human scale, and a riding pose whose feet
            // rest on the board deck rather than the AI meshes' standing A-pose.
            foreach (string name in new[] { "Ace", "Nova", "Bolt" })
            {
                var standing = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Crew/" + name + ".prefab");
                Check(standing && standing.GetComponentInChildren<MeshFilter>(), "authored standing crew model: " + name);
                var sb = RenderBounds(standing);
                Check(sb.size.y > 1.5f && sb.size.y < 2.05f, "standing crew is human scale: " + name + " = " + sb.size.y.ToString("0.00") + " m");
                Check(sb.min.y > -0.06f && sb.min.y < 0.14f, "standing crew stands on the ground: " + name + " = " + sb.min.y.ToString("0.00") + " m");
                if (name == "Bolt") continue;
                var riding = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Crew/" + name + "-Ride.prefab");
                Check(riding && riding.GetComponentInChildren<MeshFilter>(), "authored riding crew model: " + name);
                var rb = RenderBounds(riding);
                Check(rb.size.y > 1.1f && rb.size.y < 1.8f, "riding crew is crouched: " + name + " = " + rb.size.y.ToString("0.00") + " m");
                Check(rb.min.y > 0.02f && rb.min.y < 0.18f, "riding crew feet rest on the deck: " + name + " = " + rb.min.y.ToString("0.00") + " m");
            }
            // Authored hurdles and street dressing: the AI photogrammetry meshes are gone from the
            // obstacles, and every prop is merged to a handful of renderers so it can be pooled.
            var barrier = Prop("neonBarrier");
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
                int renderers = prop.GetComponentsInChildren<MeshRenderer>().Length;
                Check(renderers > 0 && renderers <= 8, "prop is merged into a few draw calls: " + prop.name + " = " + renderers);
            }
            Debug.Log($"[DashRiders] PASS: {assertions} assertions (coupon uniqueness, expiry, one-use, save/reload, movement windows, power-ups)");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
