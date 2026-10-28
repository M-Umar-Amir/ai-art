using UnityEngine;

namespace DallasDash.Data
{
    /// Everyday street hurdles. Low = hop it, Block = change lane.
    [CreateAssetMenu(menuName = "Dallas Dash/Obstacle", fileName = "Obstacle")]
    public class ObstacleDef : ScriptableObject
    {
        public string id;
        public string displayName;
        public Gameplay.TrackKind kind = Gameplay.TrackKind.Low;
        public Vector3 size = new(2f, 0.8f, 0.5f);
        public int weight = 1;
        public Color placeholderColor = Color.white;
        public GameObject prefab;
        /// Moves the same way as the rider at this fraction of their pace (another courier); 0 = standing.
        public float drift;
        /// Vehicles and roadworks: too heavy to knock over.
        public bool heavy;
    }
}
