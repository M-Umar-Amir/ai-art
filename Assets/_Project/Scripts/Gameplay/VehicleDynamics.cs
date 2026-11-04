using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    public enum CourierVehicle { Running, ElectricBoard, GlideBoard, MonoWheel, Bicycle }

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
        /// On foot after a crash: slower than the e-bike, nimbler, a higher jump.
        public static readonly VehicleProfile Runner = new(CourierVehicle.Running, 0.82f, 26f, 200f, 9.9f, 31f, 0.27f, 0.3f, 1.95f, 0.85f);
        public static readonly VehicleProfile Electric = new(CourierVehicle.ElectricBoard, 1.02f, 22f, 145f, 9.4f, 31f, 0.36f, 0.7f, 2.15f, 1.05f);
        public static readonly VehicleProfile Glide = new(CourierVehicle.GlideBoard, 1.04f, 24f, 210f, 9.8f, 31f, 0.34f, 0.62f, 2.15f, 1.0f);
        public static readonly VehicleProfile Wheel = new(CourierVehicle.MonoWheel, 0.99f, 20f, 110f, 9.1f, 32f, 0.52f, 0.65f, 2.15f, 1.45f);
        /// Ace and Nova's courier e-bike: a 1.7 m wheelbase, quick but weighty steering, a bunny-hop jump.
        public static readonly VehicleProfile Bike = new(CourierVehicle.Bicycle, 1.03f, 23f, 170f, 9.6f, 31f, 0.34f, 0.85f, 2.2f, 1.25f);
        public static VehicleProfile Current => PlayerProfile.Character == 2 ? Wheel : DashRun.Instance && DashRun.Instance.OnFoot ? Runner : Bike;
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
