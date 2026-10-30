using DallasDash.Core;

namespace DallasDash.Gameplay
{
    /// Bot rider for screenshot and showcase modes: looks a short way down its lane and dodges whatever is there.
    public static class Autopilot
    {
        public static RiderInput.Action Steer(TrackSpawner track, RiderController rider)
        {
            if (!track || !rider) return RiderInput.Action.None;
            var gm = GameManager.Instance;
            float d = gm.Distance;
            float speed = UnityEngine.Mathf.Max(gm.Speed, 1f);
            // Looks about two thirds of a second ahead (vans and roadworks are 5-6 m long; 9 m was too late at speed).
            float look = UnityEngine.Mathf.Max(10f, speed * 0.7f);
            // The fourth lane is the faster, hazard-free line: be on the right-hand lane before the entry
            // window opens and swipe onto the route as soon as it does.
            if (rider.Lane != Tuning.RouteLane)
            {
                if (DashRun.RouteEntry(d)) return RiderInput.Action.Right;
                if (DashRun.RouteOpen(d + 45f) && rider.Lane < Tuning.Lanes - 1) return RiderInput.Action.Right;
            }
            else
            {
                // On the route there is no lane to dodge into, so a rooftop hurdle has to be hopped.
                foreach (var o in track.Live)
                {
                    if (!o || o.consumed || o.lane != Tuning.RouteLane || o.kind != TrackKind.Low && o.kind != TrackKind.Gap) continue;
                    float ahead = o.transform.position.z - o.halfLength - d;
                    if (ahead < -0.6f || ahead > look) continue;
                    return rider.Airborne || ahead / speed > 0.28f ? RiderInput.Action.None : RiderInput.Action.Jump;
                }
                return RiderInput.Action.None;
            }
            TrackObject next = null;
            float nextAhead = float.MaxValue;
            foreach (var o in track.Live)
            {
                if (!o || o.consumed || o.kind == TrackKind.Pickup || o.lane != rider.Lane) continue;
                float ahead = o.transform.position.z - o.halfLength - d;
                if (ahead > -o.halfLength * 2f && ahead < look && ahead < nextAhead) { next = o; nextAhead = ahead; }
            }
            if (next == null) return RiderInput.Action.None;
            // Jumps go by time to impact, not distance: a jump is clear of a low obstacle only from about
            // 0.1 s to 0.5 s after take-off, so going too early lands on it.
            float eta = nextAhead / speed;
            if (next.kind == TrackKind.Overhead && eta < 0.25f) return RiderInput.Action.Down;
            if (next.kind == TrackKind.Low || next.kind == TrackKind.Gap)
            {
                // Steer round it if a neighbouring lane is open for longer; otherwise hop it on time.
                int alt = BestLane(track, rider.Lane, d, look);
                if (alt != rider.Lane && Room(track, alt, d, look) > nextAhead + 6f) return alt < rider.Lane ? RiderInput.Action.Left : RiderInput.Action.Right;
                return rider.Airborne || eta > 0.25f ? RiderInput.Action.None : RiderInput.Action.Jump;
            }
            int lane = BestLane(track, rider.Lane, d, look);
            if (lane == rider.Lane) return RiderInput.Action.None;
            return lane < rider.Lane ? RiderInput.Action.Left : RiderInput.Action.Right;
        }

        /// The neighbouring lane (or this one) with the most open road ahead; low hurdles count as passable.
        static int BestLane(TrackSpawner track, int from, float d, float look)
        {
            int best = from;
            float bestRoom = Room(track, from, d, look);
            foreach (int lane in new[] { from - 1, from + 1 })
            {
                if (lane < 0 || lane >= Tuning.Lanes) continue;
                float room = Room(track, lane, d, look);
                if (room > bestRoom + 0.5f) { best = lane; bestRoom = room; }
            }
            return best;
        }

        /// Distance to the first lane-filling obstacle in a lane (0 if one is alongside right now).
        static float Room(TrackSpawner track, int lane, float d, float look)
        {
            float room = look * 3f;
            foreach (var o in track.Live)
            {
                if (!o || o.consumed || (o.kind != TrackKind.Block && o.kind != TrackKind.Gap && o.kind != TrackKind.Overhead) || o.lane != lane) continue;
                float near = o.transform.position.z - o.halfLength - d;
                float far = o.transform.position.z + o.halfLength - d;
                if (far < -Tuning.PlayerHalf - 0.3f) continue;
                room = UnityEngine.Mathf.Min(room, UnityEngine.Mathf.Max(0f, near));
            }
            return room;
        }
    }
}
