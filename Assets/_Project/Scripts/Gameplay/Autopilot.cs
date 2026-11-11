using DallasDash.Core;

namespace DallasDash.Gameplay
{
    /// Bot rider for screenshot and showcase modes: looks a short way down its lane and dodges whatever is there.
    public static class Autopilot
    {
        static float lastWallRun = -1000f;
        public static RiderInput.Action Steer(TrackSpawner track, RiderController rider)
        {
            if (!track || !rider) return RiderInput.Action.None;
            var gm = GameManager.Instance;
            float d = gm.Distance;
            float speed = UnityEngine.Mathf.Max(gm.Speed, 1f);
            // Looks about two thirds of a second ahead (vans and roadworks are 5-6 m long; 9 m was too late at speed).
            // A lane change needs about half a second to clear a van; look a full second ahead.
            float look = UnityEngine.Mathf.Max(12f, speed * 1.05f);
            var surface = rider.Traversal.Surface;
            var dash = DashRun.Instance;
            if (dash && dash.OnFoot && surface == RunSurface.Road && dash.BikeAt > d && dash.BikeAt - d < 60f && rider.Lane != dash.BikeLane && !rider.Traversal.Moving)
            {
                // Head for the parked bike one lane at a time, and only into a lane that is open for a while.
                int toward = rider.Lane < dash.BikeLane ? rider.Lane + 1 : rider.Lane - 1;
                if (Room(track, rider, toward, d, look) > UnityEngine.Mathf.Max(10f, speed * 0.6f))
                    return toward > rider.Lane ? RiderInput.Action.Right : RiderInput.Action.Left;
            }
            if (rider.Traversal.Moving) return RiderInput.Action.None;
            // Wall runs (client, 3 Nov: anywhere there is a facade; on foot for Ace and Nova, always for Bolt):
            // from an outer lane with a long clear facade ahead, steer into the building. Spaced out so the bot
            // still rides the street between runs.
            if (dash && surface == RunSurface.Road && DashRun.CanRideWalls && !rider.Airborne && d - lastWallRun > (dash.OnFoot ? 45f : 140f)
                && (rider.Lane == 0 || rider.Lane == Tuning.Lanes - 1) && !(dash.OnFoot && dash.BikeAt > d && dash.BikeAt - d < 70f))
            {
                int side = rider.Lane == 0 ? -1 : 1;
                float reach = speed * (dash.WallRunSeconds + 0.6f);
                if (DashRun.WallAvailable(side, d) && DashRun.WallAvailable(side, d + reach * 0.5f) && DashRun.WallAvailable(side, d + reach))
                { lastWallRun = d; return side < 0 ? RiderInput.Action.Left : RiderInput.Action.Right; }
            }
            if (d < lastWallRun) lastWallRun = -1000f; // a new run
            // The 10% coupon is ridden into: be in its lane well before it.
            foreach (var target in new[] { track.Coupon })
            {
                if (!target || surface != RunSurface.Road) continue;
                float ahead = target.transform.position.z - d;
                if (ahead < 0f || ahead > 70f) continue;
                if (rider.Lane != target.lane) return rider.Lane < target.lane ? RiderInput.Action.Right : RiderInput.Action.Left;
                if (ahead < 25f) return RiderInput.Action.None;
            }
            TrackObject next = null;
            float nextAhead = float.MaxValue;
            foreach (var o in track.Live)
            {
                if (!o || o.consumed || o.kind == TrackKind.Pickup || o.lane != rider.Lane || o.surface != surface) continue;
                float ahead = o.transform.position.z - o.halfLength - d;
                if (ahead > -o.halfLength * 2f && ahead < look && ahead < nextAhead) { next = o; nextAhead = ahead; }
            }
            if (next == null) return RiderInput.Action.None;
            // Jumps go by time to impact, not distance: a jump is clear of a low obstacle only from about
            // 0.1 s to 0.5 s after take-off, so going too early lands on it.
            float eta = nextAhead / speed;
            if (next.kind == TrackKind.Overhead && eta < 0.25f) return RiderInput.Action.Down;
            // A tall barrier (taller than a clean hop clears) is steered round like a block.
            if ((next.kind == TrackKind.Low && next.height <= 1.0f) || next.kind == TrackKind.Gap)
            {
                // Steer round it if a neighbouring lane is open for longer; otherwise hop it on time.
                int alt = BestLane(track, rider, rider.Lane, d, look);
                if (alt != rider.Lane && Room(track, rider, alt, d, look) > nextAhead + 6f) return alt < rider.Lane ? RiderInput.Action.Left : RiderInput.Action.Right;
                // Still airborne (just landed off a gap, say): ask anyway, the controller buffers it to landing.
                return eta > 0.3f ? RiderInput.Action.None : RiderInput.Action.Jump;
            }
            int lane = BestLane(track, rider, rider.Lane, d, look);
            if (lane == rider.Lane) return RiderInput.Action.None;
            if (surface != RunSurface.Road && surface != RunSurface.Rooftop) return RiderInput.Action.None;
            return lane < rider.Lane ? RiderInput.Action.Left : RiderInput.Action.Right;
        }

        /// The neighbouring lane (or this one) with the most open road ahead; low hurdles count as passable.
        static int BestLane(TrackSpawner track, RiderController rider, int from, float d, float look)
        {
            int best = from;
            float bestRoom = Room(track, rider, from, d, look);
            foreach (int lane in new[] { from - 1, from + 1 })
            {
                if (lane < 0 || lane >= Tuning.Lanes) continue;
                float room = Room(track, rider, lane, d, look);
                if (room > bestRoom + 0.5f) { best = lane; bestRoom = room; }
            }
            return best;
        }

        /// Distance to the first lane-filling obstacle in a lane (0 if one is alongside right now).
        static float Room(TrackSpawner track, RiderController rider, int lane, float d, float look)
        {
            float room = look * 3f;
            foreach (var o in track.Live)
            {
                if (!o) continue;
                bool tall = o.kind == TrackKind.Low && o.height > 1.0f;
                if (o.consumed || (o.kind != TrackKind.Block && o.kind != TrackKind.Gap && o.kind != TrackKind.Overhead && !tall) || o.lane != lane || o.surface != rider.Traversal.Surface) continue;
                float near = o.transform.position.z - o.halfLength - d;
                float far = o.transform.position.z + o.halfLength - d;
                if (far < -Tuning.PlayerHalf - 0.3f) continue;
                room = UnityEngine.Mathf.Min(room, UnityEngine.Mathf.Max(0f, near));
            }
            return room;
        }
    }
}
