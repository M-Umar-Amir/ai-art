using DallasDash.Core;

namespace DallasDash.Gameplay
{
    /// Bot rider for screenshot and showcase modes: looks a short way down its lane and dodges whatever is there.
    public static class Autopilot
    {
        public static RiderInput.Action Steer(TrackSpawner track, RiderController rider)
        {
            if (!track || !rider) return RiderInput.Action.None;
            float d = GameManager.Instance.Distance;
            TrackObject next = null;
            foreach (var o in track.Live)
            {
                if (!o || o.consumed || o.kind == TrackKind.Pickup || o.lane != rider.Lane) continue;
                float ahead = o.transform.position.z - d;
                if (ahead > 0f && ahead < 9f && (next == null || ahead < next.transform.position.z - d)) next = o;
            }
            if (next == null) return RiderInput.Action.None;
            // Jumps and rolls go by time to impact, not distance: a jump is clear of a low obstacle only from
            // about 0.1 s to 0.5 s after take-off, so going at 9 m (half a second early at speed) lands on it.
            float eta = (next.transform.position.z - next.halfLength - d) / UnityEngine.Mathf.Max(GameManager.Instance.Speed, 1f);
            switch (next.kind)
            {
                case TrackKind.Low: return rider.Airborne || eta > 0.25f ? RiderInput.Action.None : RiderInput.Action.Jump;
                case TrackKind.High: return rider.Rolling || eta > 0.25f ? RiderInput.Action.None : RiderInput.Action.Roll;
                default:
                    foreach (int lane in new[] { rider.Lane - 1, rider.Lane + 1 })
                        if (lane >= 0 && lane < Tuning.Lanes && LaneClear(track, lane, d))
                            return lane < rider.Lane ? RiderInput.Action.Left : RiderInput.Action.Right;
                    return RiderInput.Action.None;
            }
        }

        static bool LaneClear(TrackSpawner track, int lane, float d)
        {
            foreach (var o in track.Live)
            {
                if (!o || o.consumed || o.kind == TrackKind.Pickup || o.lane != lane) continue;
                float ahead = o.transform.position.z - d;
                if (ahead > -2f && ahead < 10f) return false;
            }
            return true;
        }
    }
}
