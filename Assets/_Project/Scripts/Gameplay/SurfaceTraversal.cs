using UnityEngine;

namespace DallasDash.Gameplay
{
    public enum RunSurface { Road, LeftWall, Ceiling, RightWall }

    // Contact frame shared by movement, rendered routes and obstacle tests. Units are metres.
    // A transition interpolates from the current frame, including when interrupted at a zone exit.
    public sealed class SurfaceTraversal
    {
        public const float HalfWidth = 3.55f, CeilingHeight = 6.2f, TransitionSeconds = 0.42f;
        public RunSurface Surface { get; private set; }
        public Vector3 Origin { get; private set; }
        public Quaternion Rotation { get; private set; } = Quaternion.identity;
        public Vector3 Normal => Rotation * Vector3.up;
        public bool Moving => elapsed < TransitionSeconds;
        Vector3 from;
        Quaternion fromRotation = Quaternion.identity;
        float elapsed = TransitionSeconds;

        public static Vector3 Anchor(RunSurface surface) => surface switch
        {
            RunSurface.LeftWall => new Vector3(-HalfWidth, CeilingHeight * 0.5f, 0),
            RunSurface.Ceiling => new Vector3(0, CeilingHeight, 0),
            RunSurface.RightWall => new Vector3(HalfWidth, CeilingHeight * 0.5f, 0),
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
        public void Change(RunSurface surface)
        {
            if (surface == Surface) return;
            from = Origin; fromRotation = Rotation; Surface = surface; elapsed = 0f;
        }
        public void Tick(float dt)
        {
            elapsed = Mathf.Min(TransitionSeconds, elapsed + Mathf.Max(0f, dt));
            float t = Mathf.SmoothStep(0, 1, elapsed / TransitionSeconds);
            Origin = Vector3.Lerp(from, Anchor(Surface), t);
            Rotation = Quaternion.Slerp(fromRotation, Orientation(Surface), t);
        }
        public void Reset()
        {
            Surface = RunSurface.Road; Origin = from = Vector3.zero;
            Rotation = fromRotation = Quaternion.identity; elapsed = TransitionSeconds;
        }
    }
}
