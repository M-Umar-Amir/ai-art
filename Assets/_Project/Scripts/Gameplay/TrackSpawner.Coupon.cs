using DallasDash.Core;
using DallasDash.UI;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// The on-track 10% coupon (client round, 4 Nov 2026). The Skypark portal shortcut that used to share this
    /// file was removed on 11 Nov 2026 (the game is Downtown only); it is archived in
    /// _archive/portal-skypark-20261111.
    ///
    /// Once 100,000 burgers have been collected (PlayerProfile.CouponProgress) a large gold 10% OFF coupon is put
    /// in a free lane ahead. Riding into it issues the coupon; passing it resets the progress.
    public partial class TrackSpawner
    {
        TrackObject coupon;
        /// Lane reservation for the coupon: no hazard spawns in its lane around it.
        int reservedLane = -1;
        float reservedFrom = -1f, reservedTo = -1f;
        bool couponPending;
        /// The coupon ticket artwork (models/make_coupon.py), set by the bootstrap.
        public Texture2D couponTexture;
        Material couponMat;

        /// The live coupon (HUD arrow, bots); null when there is none ahead.
        public TrackObject Coupon => coupon && !coupon.consumed && coupon.gameObject.activeSelf ? coupon : null;
        public bool CouponWaiting => couponPending;

        bool LaneReserved(int lane, float z) => lane == reservedLane && z > reservedFrom && z < reservedTo;

        void ResetSpecials(float d)
        {
            coupon = null;
            reservedLane = -1;
            couponPending = PlayerProfile.CouponProgress >= BurgerPrices.CouponThreshold;
        }

        void OnCouponReady() => couponPending = true;

        /// The coupon is placed far ahead, before the hazard rows reach its spot.
        void BuildSpecials(float d)
        {
            var gm = GameManager.Instance;
            if (!gm || gm.State != RunState.Running) return;
            if (couponPending && !Coupon)
            {
                float z = Mathf.Max(d + 130f, nextRow + 4f);
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

        void Reserve(int lane, float z)
        {
            // Hazards already standing in the lane around the spot are cleared, so the way in is always open.
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var o = live[i];
                if (!o || o.kind == TrackKind.Pickup || o.lane != lane || o.lateral) continue;
                if (Mathf.Abs(o.transform.position.z - z) > 18f) continue;
                Pool.Release(o.gameObject); live.RemoveAt(i);
            }
            reservedLane = lane; reservedFrom = z - 18f; reservedTo = z + 10f;
        }

        void SpawnCoupon(float z, int lane)
        {
            Reserve(lane, z);
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
            reservedLane = -1;
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
