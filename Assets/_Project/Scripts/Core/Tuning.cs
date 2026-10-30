namespace DallasDash.Core
{
    /// Run tuning carried over from the web build's sim.js, so the Unity version keeps the pace the client already approved.
    public static class Tuning
    {
        public const int Lanes = 3;
        /// The fourth lane is the side route: an express deck in shortcut zones, building rooftops in
        /// Skypark. It sits on the right-hand sidewalk, outside the three traffic lanes.
        public const int RouteLane = 3;
        public static readonly float[] LaneX = { -2.3f, 0f, 2.3f, 5.7f };

        public const float JumpV = 9.4f;
        public const float DiveV = -22f;
        public const float Gravity = 31f;
        /// A jump swiped just before landing still fires on touchdown.
        public const float JumpBufferSeconds = 0.15f;
        public const float LaneSlewPerSecond = 22f;

        public const float SpeedBase = 20f;
        public const float SpeedMax = 46f;
        public const float SpeedRamp = 0.0072f; // speed gained per unit run

        public const int PointsPerUnit = 2;
        public const int MaxMult = 6;
        /// Distance for each step up the score multiplier.
        public const float MultEvery = 600f;

        /// A second stumble inside this window means the police catch the rider.
        public const float ChaseWindowSeconds = 300f / 60f;

        public const float FarEdge = 230f;
        public const float PlayerHalf = 0.35f;
    }
}
