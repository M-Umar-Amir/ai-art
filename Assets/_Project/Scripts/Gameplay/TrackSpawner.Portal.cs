using DallasDash.Core;
using DallasDash.UI;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// The Skypark portal shortcut and the on-track 10% coupon (client round, 4 Nov 2026).
    ///
    /// Shortcut: a portal stands in one street lane, built like the "grab your bike" marker (glow pad and light
    /// beacon) with a ring gate, and a kerbside sign GO THROUGH HERE FOR SHORTCUT. Riding through it teleports
    /// Ace to the Skypark rooftops on foot (DashRun.EnterSkypark); a full-width return portal at the end of the
    /// Skypark is compulsory and sends him back to Downtown on his bike (DashRun.ExitSkypark).
    ///
    /// Coupon: once 100,000 burgers have been collected (PlayerProfile.CouponProgress) a large gold 10% OFF
    /// coupon is put in a free lane ahead. Riding into it issues the coupon; passing it resets the progress.
    public partial class TrackSpawner
    {
        /// First portal of a run, and the spacing between portals (measured from the last return to Downtown).
        public const float FirstPortalAt = 450f, PortalEvery = 900f;
        float nextPortal = FirstPortalAt;
        TrackObject portal, coupon;
        /// Lane reservations: [0] the portal, [1] the coupon.
        readonly int[] reservedLane = { -1, -1 };
        readonly float[] reservedFrom = { -1f, -1f }, reservedTo = { -1f, -1f };
        bool couponPending, portalPending;
        /// The coupon ticket artwork (models/make_coupon.py), set by the bootstrap.
        public Texture2D couponTexture;
        Material couponMat;

        /// The live shortcut portal and coupon (HUD arrows, bots); null when there is none ahead.
        public TrackObject Portal => portal && !portal.consumed && portal.gameObject.activeSelf ? portal : null;
        public TrackObject Coupon => coupon && !coupon.consumed && coupon.gameObject.activeSelf ? coupon : null;
        public bool CouponWaiting => couponPending;

        /// Hazards never spawn in the portal's or coupon's lane around it.
        bool LaneReserved(int lane, float z)
        {
            for (int i = 0; i < 2; i++) if (lane == reservedLane[i] && z > reservedFrom[i] && z < reservedTo[i]) return true;
            return false;
        }

        void ResetSpecials(float d)
        {
            nextPortal = d + FirstPortalAt;
            portal = coupon = null;
            reservedLane[0] = reservedLane[1] = -1;
            couponPending = PlayerProfile.CouponProgress >= BurgerPrices.CouponThreshold;
        }

        void OnCouponReady() => couponPending = true;

        /// Portals and the coupon are placed far ahead, before the hazard rows reach their spot.
        void BuildSpecials(float d)
        {
            var gm = GameManager.Instance;
            if (!gm || gm.State != RunState.Running || DashRun.District != 0) return;
            float horizon = d + Tuning.FarEdge + 30f;
            if (!Portal && nextPortal < horizon)
            {
                float z = nextPortal;
                for (int guard = 0; guard < 8 && (NearJunction(z) || NearJunction(z + 10f) || NearJunction(z - 10f)); guard++) z += 25f;
                if (z > d + 60f) SpawnPortal(z, PickLane(z));
                nextPortal = z + PortalEvery;
            }
            if (couponPending && !Coupon)
            {
                float z = Mathf.Max(d + 130f, nextRow + 4f);
                if (Portal && Mathf.Abs(z - portal.transform.position.z) < 40f) z = portal.transform.position.z + 45f;
                for (int guard = 0; guard < 6 && NearJunction(z); guard++) z += 20f;
                SpawnCoupon(z, PickLane(z));
                couponPending = false;
            }
        }

        int PickLane(float z)
        {
            int start = Mathf.Abs(Mathf.RoundToInt(z / 37f)) % Tuning.Lanes;
            for (int k = 0; k < Tuning.Lanes; k++)
            {
                int lane = (start + k) % Tuning.Lanes;
                if (LaneFree(lane, z)) return lane;
            }
            return 1;
        }

        void Reserve(int slot, int lane, float z)
        {
            // Hazards already standing in the lane around the spot are cleared, so the way in is always open.
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var o = live[i];
                if (!o || o.kind == TrackKind.Pickup || o.lane != lane || o.lateral) continue;
                if (Mathf.Abs(o.transform.position.z - z) > 18f) continue;
                Pool.Release(o.gameObject); live.RemoveAt(i);
            }
            reservedLane[slot] = lane; reservedFrom[slot] = z - 18f; reservedTo[slot] = z + 10f;
        }

        // ---------- Portal

        void SpawnPortal(float z, int lane)
        {
            Reserve(0, lane, z);
            float x = Tuning.LaneX[lane];
            var root = new GameObject("Skypark shortcut portal / go through");
            root.transform.SetParent(transform);
            root.transform.position = new Vector3(x, 1f, z);
            var ring = DashCity.Mat(this, "portalRing", new Color(0.25f, 0.95f, 1f), 3.2f);
            var frame = DashCity.Mat(this, "portalFrame", new Color(0.07f, 0.08f, 0.12f));
            var glow = DashCity.Mat(this, "roofNeonCyan", new Color(0.1f, 0.85f, 1f), 2.2f);
            var beam = DashCity.Mat(this, "bikeBeacon", new Color(0.2f, 0.95f, 1f), 3.5f);
            var core = DashCity.Mat(this, "portalEnergy", new Color(0.03f, 0.22f, 0.32f), 0.9f);
            var coreRing = DashCity.Mat(this, "portalEnergyRing", new Color(0.2f, 0.85f, 1f), 1.6f);

            // The same language as the grab-your-bike marker: a glowing pad and a tall light beacon.
            DashCity.Part(root.transform, new Vector3(x, 0.02f, z), new Vector3(2.2f, 0.02f, 2.6f), glow, PrimitiveType.Cylinder).name = "Pad glow";
            DashCity.Part(root.transform, new Vector3(x, 6.2f, z), new Vector3(0.2f, 3.2f, 0.2f), beam, PrimitiveType.Cylinder).name = "Beacon";

            // The ring gate: a dark frame with a bright inner ring, built from short chord segments.
            var gate = new GameObject("Ring gate").transform;
            gate.SetParent(root.transform, false);
            gate.position = new Vector3(x, 1.5f, z);
            const int segments = 28;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                var p = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                var outer = DashCity.Part(gate, gate.position + p * 1.32f, new Vector3(0.34f, 0.3f, 0.34f), frame);
                outer.transform.rotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
                var inner = DashCity.Part(gate, gate.position + p * 1.14f + Vector3.back * 0.12f, new Vector3(0.28f, 0.1f, 0.08f), ring);
                inner.transform.rotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg + 90f);
            }
            // Feet on the road so the gate reads as standing, not floating.
            foreach (float s in new[] { -1f, 1f })
                DashCity.Part(gate, new Vector3(x + s * 0.95f, 0.15f, z), new Vector3(0.35f, 0.3f, 0.7f), frame);
            var swirl = DashCity.Part(gate, gate.position, new Vector3(2.15f, 0.02f, 2.15f), core, PrimitiveType.Cylinder);
            swirl.name = "Portal swirl";
            swirl.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            gate.gameObject.AddComponent<PortalSpin>().swirl = swirl.transform;

            DashCity.Text(this, root.transform, "SHORTCUT", new Vector3(x, 3.35f, z - 0.05f), 0.055f, Color.white);

            // The sign: an overhead gantry 26 m before the portal, its board hanging over the portal's lane with
            // an arrow down at it: GO THROUGH HERE FOR SHORTCUT. Seen from far down the street, never clipped.
            float sz = z - 26f;
            var post = DashCity.Mat(this, "signPost", new Color(0.32f, 0.34f, 0.38f));
            var board = DashCity.Mat(this, "shortcutBoard", new Color(0.03f, 0.2f, 0.5f), 0.25f);
            var edge = DashCity.Mat(this, "shortcutEdge", new Color(1f, 0.86f, 0.1f), 1.6f);
            foreach (float s in new[] { -1f, 1f })
                DashCity.Part(root.transform, new Vector3(s * 4.4f, 2.9f, sz), new Vector3(0.2f, 5.8f, 0.2f), post);
            DashCity.Part(root.transform, new Vector3(0f, 5.75f, sz), new Vector3(9f, 0.22f, 0.22f), post);
            DashCity.Part(root.transform, new Vector3(x, 4.55f, sz), new Vector3(3.3f, 1.75f, 0.1f), board).name = "Shortcut sign";
            DashCity.Part(root.transform, new Vector3(x, 4.55f, sz + 0.04f), new Vector3(3.44f, 1.89f, 0.04f), edge);
            DashCity.Text(this, root.transform, "GO THROUGH HERE", new Vector3(x, 5.0f, sz - 0.07f), 0.04f, Color.white);
            DashCity.Text(this, root.transform, "FOR SHORTCUT", new Vector3(x, 4.5f, sz - 0.07f), 0.04f, new Color(1f, 0.86f, 0.1f));
            DashCity.Text(this, root.transform, "V   SKYPARK   V", new Vector3(x, 4.0f, sz - 0.07f), 0.03f, new Color(0.55f, 0.95f, 1f));

            Register(root, TrackKind.Pickup, lane, 1.2f, 0, 0f);
            portal = live[live.Count - 1];
            portal.portal = true;
        }

        // ---------- Coupon

        void SpawnCoupon(float z, int lane)
        {
            Reserve(1, lane, z);
            float x = Tuning.LaneX[lane];
            var root = new GameObject("10% coupon / grab it");
            root.transform.SetParent(transform);
            root.transform.position = new Vector3(x, 1f, z);
            var gold = DashCity.Mat(this, "couponGold", new Color(1f, 0.74f, 0.12f), 2.4f);
            var frame = DashCity.Mat(this, "couponFrame", new Color(0.35f, 0.22f, 0.04f));
            DashCity.Part(root.transform, new Vector3(x, 0.02f, z), new Vector3(2.4f, 0.02f, 2.8f), gold, PrimitiveType.Cylinder).name = "Coupon pad";
            DashCity.Part(root.transform, new Vector3(x, 7.5f, z), new Vector3(0.32f, 6f, 0.32f), gold, PrimitiveType.Cylinder).name = "Coupon beacon";
            var ticket = new GameObject("Coupon ticket").transform;
            ticket.SetParent(root.transform, false);
            ticket.position = new Vector3(x, 1.75f, z);
            const float tw = 2.3f, th = tw * 576f / 1024f;
            DashCity.Part(ticket, ticket.position, new Vector3(tw + 0.12f, th + 0.12f, 0.08f), frame).name = "Ticket frame";
            if (!couponTexture) throw new System.InvalidOperationException("Missing coupon ticket texture");
            if (!couponMat) { couponMat = new Material(tintMaterial) { name = "Coupon ticket" }; BrandVisibility.Backlit(couponMat, couponTexture); }
            foreach (float face in new[] { -1f, 1f })
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(q.GetComponent<Collider>());
                q.name = "Coupon ticket face";
                q.transform.SetParent(ticket, false);
                q.transform.position = ticket.position + Vector3.forward * face * 0.045f;
                q.transform.rotation = face < 0 ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
                q.transform.localScale = new Vector3(tw, th, 1f);
                var r = q.GetComponent<Renderer>(); r.sharedMaterial = couponMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            // A gold halo ring behind the ticket, so it reads as a prize from far down the street.
            for (int i = 0; i < 24; i++)
            {
                float a2 = i * Mathf.PI * 2f / 24f;
                var seg = DashCity.Part(ticket, ticket.position + new Vector3(Mathf.Cos(a2) * 1.55f, Mathf.Sin(a2) * 1.55f, 0.12f), new Vector3(0.36f, 0.09f, 0.05f), gold);
                seg.transform.rotation = Quaternion.Euler(0f, 0f, a2 * Mathf.Rad2Deg + 90f);
            }
            ticket.gameObject.AddComponent<CouponSpin>();
            Register(root, TrackKind.Pickup, lane, 1.3f, 0, 0f);
            coupon = live[live.Count - 1];
            coupon.coupon = true;
            MenuUI.Instance?.Caption("10% COUPON AHEAD!", "100,000 burgers collected  /  ride into it", 2.4f);
            Haptics.Play(Haptics.Strength.Medium);
        }

        /// Riding into the coupon issues it; called from Collect.
        void TakeCoupon(TrackObject o)
        {
            o.gameObject.SetActive(false);
            PlayerProfile.CouponProgress = Mathf.Max(0, PlayerProfile.CouponProgress - BurgerPrices.CouponThreshold);
            var issued = DemoRewards.Award();
            PlayerProfile.NotifyChanged();
            MenuUI.Instance?.CouponWon(issued);
            reservedLane[1] = -1;
        }

        /// A coupon left behind is lost: the progress starts again from zero.
        void CheckMissedSpecials(float d)
        {
            var c = Coupon;
            if (c && c.transform.position.z + c.halfLength < d - 2f)
            {
                c.consumed = true;
                PlayerProfile.CouponProgress = 0;
                PlayerProfile.NotifyChanged();
                MenuUI.Instance?.Caption("COUPON MISSED!", "Collect 100,000 more burgers for a new one", 2.6f);
                Haptics.Play(Haptics.Strength.Medium);
                coupon = null;
            }
            var p = Portal;
            if (p && p.transform.position.z + p.halfLength < d - 2f) { p.consumed = true; portal = null; }
        }

        // ---------- World switch (portal teleports)

        /// Rebuilds the world around distance d for the current DashRun.District, without resetting the run.
        /// Called under the teleport flash; the build runs to completion in this frame.
        public void SwitchWorld(float d)
        {
            bool couponWasLive = Coupon;
            staticCohorts.Clear();
            DashCity.ApplyLighting(this);
            pending.Clear();
            building = null;
            foreach (var o in live) if (o) Pool.Release(o.gameObject);
            foreach (var s in segments) if (s) { DistrictLife.Instance?.Return(s); Destroy(s); }
            live.Clear();
            segments.Clear();
            plazas.Clear();
            builtRooftops.Clear();
            if (parkedBike) Destroy(parkedBike);
            parkedBike = null;
            ClearBikeReservation();
            portal = coupon = null;
            reservedLane[0] = reservedLane[1] = -1;
            if (couponWasLive) couponPending = true; // not missed: it comes back in Downtown
            if (DashRun.District == 0) nextPortal = d + PortalEvery * 0.75f;
            builtTo = Mathf.Floor(d / segmentLength) * segmentLength - segmentLength * 3f;
            nextRow = d + 30f;
            nextPowerUp = d + 90f;
            lastOpen = 1;
            previousDistance = d;
            previousRiderPosition = rider ? rider.transform.position : new Vector3(0f, 0f, d);
            Build(d);
            while (Pump()) { }
        }

        /// The return portal across the whole roof at the end of the Skypark stretch.
        void SkyparkReturnPortal(Transform parent, float z)
        {
            var ring = DashCity.Mat(this, "returnRing", new Color(1f, 0.55f, 0.12f), 3f);
            var frame = DashCity.Mat(this, "portalFrame", new Color(0.07f, 0.08f, 0.12f));
            var core = DashCity.Mat(this, "returnEnergy", new Color(0.3f, 0.12f, 0.02f), 0.9f);
            var root = new GameObject("Return portal / back to Downtown").transform;
            root.SetParent(parent, false);
            foreach (float s in new[] { -1f, 1f })
            {
                DashCity.Part(root, new Vector3(s * 3.75f, 2.2f, z), new Vector3(0.5f, 4.4f, 0.5f), frame);
                DashCity.Part(root, new Vector3(s * 3.5f, 2.2f, z - 0.27f), new Vector3(0.08f, 4.2f, 0.06f), ring);
            }
            DashCity.Part(root, new Vector3(0f, 4.55f, z), new Vector3(8f, 0.5f, 0.5f), frame);
            DashCity.Part(root, new Vector3(0f, 4.3f, z - 0.27f), new Vector3(7f, 0.08f, 0.06f), ring);
            var sheet = DashCity.Part(root, new Vector3(0f, 2.15f, z + 0.05f), new Vector3(7f, 4.1f, 0.02f), core);
            sheet.name = "Return portal sheet";
            sheet.AddComponent<PortalSpin>().pulseOnly = true;
            DashCity.Text(this, root, "RETURN PORTAL  /  DOWNTOWN", new Vector3(0f, 5.2f, z - 0.3f), 0.05f, Color.white);
        }

        /// Removes hazards (not pickups) from the road just ahead: used by revives.
        public void ClearAhead(float d, float length)
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var o = live[i];
                if (!o || o.kind == TrackKind.Pickup || o.kind == TrackKind.Gap || o.kind == TrackKind.Platform) continue;
                float z = o.transform.position.z;
                if (z < d - 3f || z > d + length) continue;
                Pool.Release(o.gameObject); live.RemoveAt(i);
            }
        }

        public void RemoveParkedBike()
        {
            if (!parkedBike) return;
            var o = parkedBike.GetComponent<TrackObject>();
            if (o) { o.consumed = true; live.Remove(o); }
            Destroy(parkedBike);
            parkedBike = null;
        }
    }

    /// The portal's swirl turns and breathes so the gate reads as live from far down the street.
    public class PortalSpin : MonoBehaviour
    {
        public Transform swirl;
        public bool pulseOnly;
        Vector3 baseScale;
        void Start() { baseScale = (swirl ? swirl : transform).localScale; }
        void Update()
        {
            var t = swirl ? swirl : transform;
            float pulse = 1f + 0.05f * Mathf.Sin(Time.time * 5f);
            if (!pulseOnly) t.Rotate(0f, 140f * Time.deltaTime, 0f, Space.Self);
            t.localScale = new Vector3(baseScale.x * pulse, baseScale.y, baseScale.z * (pulseOnly ? 1f : pulse));
        }
    }

    /// The coupon ticket swings to show both faces and bobs above its pad.
    public class CouponSpin : MonoBehaviour
    {
        Vector3 basePos;
        void Start() => basePos = transform.position;
        void Update()
        {
            transform.rotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 1.6f) * 35f, Mathf.Sin(Time.time * 2.3f) * 4f);
            transform.position = basePos + Vector3.up * (0.12f * Mathf.Sin(Time.time * 3f));
        }
    }
}
