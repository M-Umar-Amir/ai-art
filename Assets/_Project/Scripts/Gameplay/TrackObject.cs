using UnityEngine;

namespace DallasDash.Gameplay
{
    public enum TrackKind
    {
        Pickup,
        /// Low barrier: jump over it.
        Low,
        /// Fills the lane (roadworks, a stopped van, another rider): change lane.
        Block,
        Overhead,
        Gap,
        /// A walkable rooftop/ledge in the Skypark building-parkour route. Support comes from its
        /// DashPlatform, never from a hit test.
        Platform
    }

    /// Anything placed on the straight track. Hit tests are lane + distance, like the web sim, so no physics is needed.
    public class TrackObject : MonoBehaviour
    {
        public TrackKind kind;
        public int lane;
        public RunSurface surface = RunSurface.Road;
        public float halfLength = 0.5f;
        public float halfWidth = 0.85f;
        public float clearance = 1.4f;
        /// Top of the obstacle above the road: a hop has to carry the bike's wheels higher than this.
        public float height = 0.8f;
        /// Height of the surface this object stands on (a rooftop hurdle sits 3.4 m up). Hit tests are
        /// measured from here, so route objects resolve against a rider on the route, not on the road.
        public float baseHeight;
        public int points;
        public bool energy;
        /// Oncoming traffic moves toward the rider at this speed (world units/s).
        public float oncoming;
        /// Traffic riding the same way (other couriers): the fraction of the rider's pace it moves at. It sits
        /// exactly on meetZ when the rider reaches it, so a row's open lane stays open (TrackSpawner.MoveTraffic).
        public float drift;
        public float meetZ;
        /// Vans, cars and roadworks: the rider bounces off instead of shoving them over.
        public bool heavy;
        /// A power-up token (PowerUp index) rather than a burger; -1 for everything else.
        public int powerUp = -1;

        [HideInInspector] public bool consumed;
        [HideInInspector] public bool nearMiss;
        [HideInInspector] public bool passed;
        [HideInInspector] public bool struck;
    }
}
