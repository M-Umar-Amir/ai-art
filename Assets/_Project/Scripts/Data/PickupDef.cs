using UnityEngine;

namespace DallasDash.Data
{
    /// Track pickups. The pack has one only: the Bellagreen-style Wagyu Bacon Burger.
    [CreateAssetMenu(menuName = "Dallas Dash/Pickup", fileName = "Pickup")]
    public class PickupDef : ScriptableObject
    {
        public string id;
        public string displayName;
        public int points = 10;
        public int weight = 1;
        public Color placeholderColor = Color.white;
        public GameObject prefab;
    }
}
