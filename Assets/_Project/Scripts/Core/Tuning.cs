namespace DallasDash.Core
{
    /// Run tuning carried over from the web build's sim.js, so the Unity version keeps the pace the client already approved.
    public static class Tuning
    {
        public const int Lanes = 3;
        public static readonly float[] LaneX = { -2.3f, 0f, 2.3f };

        public const float JumpV = 9.4f;
        public const float DiveV = -22f;
        public const float Gravity = 31f;
        public const float SlideSeconds = 32f / 60f;
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
