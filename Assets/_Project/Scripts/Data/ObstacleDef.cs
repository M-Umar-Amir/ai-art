using UnityEngine;

namespace DallasDash.Data
{
    /// Seasonal lane obstacles. Low = jump it, High = roll under it, Block = change lane.
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
    }
}
