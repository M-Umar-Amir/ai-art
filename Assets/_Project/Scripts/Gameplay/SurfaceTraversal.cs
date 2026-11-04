using UnityEngine;

namespace DallasDash.Gameplay
{
    public enum RunSurface { Road, LeftWall, Ceiling, RightWall, Rooftop }

    // Contact frame shared by movement, rendered routes and obstacle tests. Units are metres.
    // The walls are the real building facades lining the street, the ceiling is the soffit of the building
    // passage the gravity section rides through, and the rooftop is the roof line of the right-hand block.
    // A transition interpolates from the rider's current frame, optionally through waypoints (the climb up a
    // facade onto the roofs), so a transfer never teleports.
    public sealed class SurfaceTraversal
    {
        /// The rooftop block row's street face (RooftopCourse steps and the roof builder line up on it).
        public const float FacadeX = 7.0f, CeilingHeight = 7.4f, TransitionSeconds = 0.42f;
        /// Wall runs use the ordinary street buildings (NaturalArchitecture fronts at 8.5 m, window sills
        /// standing 0.5 m proud), so the running plane sits just off the sills. The running line is 5.2 m up:
        /// above the shop awnings (3.1 m) and below the first-floor terraces (6.85 m), in every district.
        public const float WallRunX = 7.98f, WallMid = 5.2f;
        /// The rooftop course: three lanes across the roof of the right-hand block, ten metres up.
        public const float RoofX = 11.6f, RoofHeight = 8f;
        public const float DropSeconds = 0.75f;

        public RunSurface Surface { get; private set; }
        public Vector3 Origin { get; private set; }
        public Quaternion Rotation { get; private set; } = Quaternion.identity;
        public Vector3 Normal => Rotation * Vector3.up;
        public bool Moving => elapsed < duration;
        /// 0..1 through the current transfer (1 when settled).
        public float Progress => duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

        Vector3[] keys = { Vector3.zero };
        Quaternion[] turns = { Quaternion.identity };
        float elapsed, duration, arc;

        public static Vector3 Anchor(RunSurface surface) => surface switch
        {
            RunSurface.LeftWall => new Vector3(-WallRunX, WallMid, 0),
            RunSurface.Ceiling => new Vector3(0, CeilingHeight, 0),
            RunSurface.RightWall => new Vector3(WallRunX, WallMid, 0),
            RunSurface.Rooftop => new Vector3(RoofX, RoofHeight, 0),
            _ => Vector3.zero
        };
        public static Quaternion Orientation(RunSurface surface) => Quaternion.Euler(0, 0, surface switch
        {
            RunSurface.LeftWall => -90f,
            RunSurface.Ceiling => -180f,
            RunSurface.RightWall => -270f,
            _ => 0f
        });
        public static Vector3 Point(RunSurface surface, float lane, float height) =>
            Anchor(surface) + Orientation(surface) * new Vector3(lane, height, 0);
        public Vector3 Position(float lane, float height, float distance) =>
            Origin + Rotation * new Vector3(lane, height, 0) + Vector3.forward * distance;

        /// Straight transfer from the rider's current contact point (lane offset and jump height included) to
        /// `lane` on the new surface.
        public void Change(RunSurface surface, Vector3 contact, float lane) =>
            Path(surface, new[] { contact, Point(surface, lane, 0f) }, new[] { Rotation, Orientation(surface) }, TransitionSeconds, 0f, lane, false);

        /// Up onto the roofs by parkour over real things (RooftopCourse steps): from the kicker ramp onto the
        /// parked truck's roof, up onto the fire-escape landing, then over the parapet into `lane`. Every hop is
        /// a ballistic jump timed by the rider's speed, so each landing happens on its object, upright.
        public void Ascend(Vector3 contact, float lane, float speed, float zoneStart, float distance)
        {
            var truck = RooftopCourse.Truck; var landing = RooftopCourse.Landing;
            var points = new[] { contact, new Vector3(truck.x, truck.top, 0f), new Vector3(landing.x, landing.top, 0f), Point(RunSurface.Rooftop, lane, 0f) };
            float[] z = { distance, zoneStart + truck.Mid, zoneStart + landing.Mid, zoneStart + RooftopCourse.RoofLanding };
            Hops(RunSurface.Rooftop, points, z, new[] { 1.1f, 1.5f, 1.2f }, speed, lane);
        }

        /// Off the last roof onto the skip on the kerb, then down into `lane` on the street.
        public void Descend(Vector3 contact, float lane, float speed, float zoneStart, float distance)
        {
            var skip = RooftopCourse.Skip;
            var points = new[] { contact, new Vector3(skip.x, skip.top, 0f), Point(RunSurface.Road, lane, 0f) };
            float[] z = { distance, zoneStart + skip.Mid, zoneStart + RooftopCourse.StreetLanding };
            Hops(RunSurface.Road, points, z, new[] { 0.8f, 0.9f }, speed, lane);
        }

        /// Into an alley gap: straight down to the street's right-hand lane.
        public void Drop(Vector3 contact, float lane) =>
            Path(RunSurface.Road, new[] { contact, Point(RunSurface.Road, lane, 0f) }, new[] { Rotation, Quaternion.identity }, DropSeconds, 0.6f, lane, true);

        /// Always false now: the facade climb was replaced by the stepped parkour line.
        public bool Climbing { get; private set; }

        float[] hopTimes, hopArcs;

        void Hops(RunSurface surface, Vector3[] contacts, float[] atZ, float[] arcs, float speed, float lane)
        {
            var rotations = new Quaternion[contacts.Length];
            for (int k = 0; k < rotations.Length; k++) rotations[k] = k == 0 ? Rotation : Quaternion.identity;
            Path(surface, contacts, rotations, 0f, 0f, lane, true);
            hopTimes = new float[contacts.Length];
            float v = Mathf.Max(8f, speed), t = 0f;
            for (int k = 1; k < contacts.Length; k++) { t += Mathf.Max(0.18f, (atZ[k] - atZ[k - 1]) / v); hopTimes[k] = t; }
            hopArcs = arcs; duration = t;
        }

        // Contacts are where the rider's wheels or feet are; the frame origin at each key is the contact minus
        // the rider's lane offset in that key's orientation, so Position() follows the contacts exactly.
        void Path(RunSurface surface, Vector3[] contacts, Quaternion[] rotations, float seconds, float hop, float lane, bool ballistic)
        {
            keys = new Vector3[contacts.Length];
            for (int k = 0; k < contacts.Length; k++) keys[k] = contacts[k] - rotations[k] * new Vector3(lane, 0f, 0f);
            Surface = surface; turns = rotations; duration = seconds; elapsed = 0f; arc = hop; this.ballistic = ballistic;
            Origin = keys[0]; Rotation = rotations[0]; Climbing = false; hopTimes = null; hopArcs = null;
        }

        bool ballistic;

        /// True for a moment after each hop lands (the rider touches the truck, the landing).
        public bool JustLanded { get; private set; }
        /// Mid-flight on a parkour hop (between the truck, the landing and the roof).
        public bool Hopping => hopTimes != null && Moving && !JustLanded;

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }

        public void Tick(float dt)
        {
            if (!Moving) return;
            elapsed = Mathf.Min(duration, elapsed + Mathf.Max(0f, dt));
            float u = elapsed / duration;
            int spans = keys.Length - 1;
            if (hopTimes != null)
            {
                // A chain of real jumps: find the current hop, fly it as a parabola above the straight line.
                float now = elapsed;
                int i = 1;
                while (i < hopTimes.Length - 1 && now > hopTimes[i]) i++;
                float span = hopTimes[i] - hopTimes[i - 1];
                float f = Mathf.Clamp01((now - hopTimes[i - 1]) / Mathf.Max(1e-4f, span));
                Origin = Vector3.Lerp(keys[i - 1], keys[i], f) + Vector3.up * (hopArcs[i - 1] * 4f * f * (1f - f));
                Rotation = Quaternion.Slerp(turns[i - 1], turns[i], Mathf.SmoothStep(0f, 1f, f));
                JustLanded = f > 0.97f;
            }
            else if (ballistic)
            {
                // Horizontal travel at constant pace, a parabola on top: what a jump actually looks like.
                Origin = Vector3.Lerp(keys[0], keys[spans], u) + Vector3.up * (arc * 4f * u * (1f - u));
                Rotation = Quaternion.Slerp(turns[0], turns[spans], Mathf.SmoothStep(0f, 1f, u));
            }
            else
            {
                float eased = spans > 1 ? Mathf.SmoothStep(0f, 1f, u) * 0.5f + u * 0.5f : Mathf.SmoothStep(0f, 1f, u);
                float s = Mathf.Clamp(eased * spans, 0f, spans - 1e-4f);
                int i = Mathf.FloorToInt(s);
                float f = s - i;
                var p0 = keys[Mathf.Max(0, i - 1)]; var p3 = keys[Mathf.Min(spans, i + 2)];
                Origin = CatmullRom(p0, keys[i], keys[i + 1], p3, f);
                Rotation = Quaternion.Slerp(turns[i], turns[i + 1], Mathf.SmoothStep(0f, 1f, f));
            }
            if (!Moving) { Origin = keys[spans]; Rotation = turns[spans]; Climbing = false; }
        }

        public void Reset()
        {
            Surface = RunSurface.Road;
            keys = new[] { Vector3.zero }; turns = new[] { Quaternion.identity };
            Origin = Vector3.zero; Rotation = Quaternion.identity; elapsed = duration = arc = 0f; Climbing = ballistic = false; hopTimes = null;
        }
    }
}
