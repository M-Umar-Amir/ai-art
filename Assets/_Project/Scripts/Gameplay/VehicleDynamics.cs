using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    public enum CourierVehicle { Running, ElectricBoard, GlideBoard, MonoWheel }

    public readonly struct VehicleProfile
    {
        public readonly CourierVehicle kind;
        public readonly float speed, steering, acceleration, jump, gravity, radius, length, standing, crouching;
        public VehicleProfile(CourierVehicle kind, float speed, float steering, float acceleration, float jump,
            float gravity, float radius, float length, float standing, float crouching)
        {
            this.kind = kind; this.speed = speed; this.steering = steering; this.acceleration = acceleration;
            this.jump = jump; this.gravity = gravity; this.radius = radius; this.length = length;
            this.standing = standing; this.crouching = crouching;
        }
    }

    // Arcade kinematic bodies: deterministic lane controls with vehicle-specific inertia and clearance.
    public static class VehicleDynamics
    {
        public static readonly VehicleProfile Runner = new(CourierVehicle.Running, 1f, 24f, 180f, 9.8f, 31f, 0.27f, 0.3f, 1.95f, 0.85f);
        public static readonly VehicleProfile Electric = new(CourierVehicle.ElectricBoard, 1.02f, 22f, 145f, 9.4f, 31f, 0.36f, 0.7f, 2.15f, 1.05f);
        public static readonly VehicleProfile Glide = new(CourierVehicle.GlideBoard, 1.05f, 21f, 125f, 9.2f, 28f, 0.4f, 0.75f, 2.15f, 1.1f);
        public static readonly VehicleProfile Wheel = new(CourierVehicle.MonoWheel, 0.99f, 20f, 110f, 9.1f, 32f, 0.52f, 0.65f, 2.15f, 1.45f);
        static int cachedFrame = -1;
        static VehicleProfile cached;
        public static VehicleProfile Current
        {
            get
            {
                if (cachedFrame == Time.frameCount) return cached;
                cachedFrame = Time.frameCount;
                bool upgraded = PlayerProfile.Ride == 1 || DashRun.Instance && DashRun.Instance.Board;
                cached = PlayerProfile.Character == 2 ? Wheel : upgraded ? Glide : PlayerProfile.Character == 1 ? Runner : Electric;
                return cached;
            }
        }
        public static float BodyHeight => DashRun.Instance && DashRun.Instance.Slide > 0f ? Current.crouching : Current.standing;

        // Swept body/AABB test: expanded obstacle bounds prevent tunnelling during a frame hitch,
        // rapid lane change or long vehicle passing a thin barrier.
        public static bool Sweep(Vector3 from, Vector3 to, Bounds obstacle, float radius, float halfLength, float height)
        {
            Vector3 min = obstacle.min - new Vector3(radius, height, halfLength);
            Vector3 max = obstacle.max + new Vector3(radius, 0f, halfLength);
            Vector3 delta = to - from;
            float enter = 0f, exit = 1f;
            for (int axis = 0; axis < 3; axis++)
            {
                if (Mathf.Abs(delta[axis]) < 0.00001f)
                { if (from[axis] < min[axis] || from[axis] > max[axis]) return false; continue; }
                float a = (min[axis] - from[axis]) / delta[axis], b = (max[axis] - from[axis]) / delta[axis];
                if (a > b) (a, b) = (b, a);
                enter = Mathf.Max(enter, a); exit = Mathf.Min(exit, b);
                if (enter > exit) return false;
            }
            return true;
        }
    }
}
