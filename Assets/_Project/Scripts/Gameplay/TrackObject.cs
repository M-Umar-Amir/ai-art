using UnityEngine;

namespace DallasDash.Gameplay
{
    public enum TrackKind
    {
        Pickup,
        /// Low barrier: jump over it.
        Low,
        /// Overhead bar: roll under it.
        High,
        /// Fills the lane (tree in a planter, food truck, shopper): change lane.
        Block
    }

    /// Anything placed on the straight track. Hit tests are lane + distance, like the web sim, so no physics is needed.
    public class TrackObject : MonoBehaviour
    {
        public TrackKind kind;
        public int lane;
        public float halfLength = 0.5f;
        public int points;
        /// Oncoming traffic moves toward the rider at this speed (world units/s).
        public float oncoming;
        /// A power-up token (PowerUp index) rather than a burger; -1 for everything else.
        public int powerUp = -1;

        [HideInInspector] public bool consumed;
        [HideInInspector] public bool nearMiss;
    }
}
