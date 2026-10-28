using UnityEngine;

namespace DallasDash.Data
{
    /// Bicycle (starter), the earnable scooter, and the police bicycle. The car and hatch were cut.
    [CreateAssetMenu(menuName = "Dallas Dash/Ride", fileName = "Ride")]
    public class RideDef : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Unlock unlock = Unlock.Objective;
        public int unlockPoints;
        public bool isPolice;
        /// Multiplier on lane-change response.
        [Range(0.5f, 1.5f)] public float handling = 1f;
        public Texture2D referenceSheet;
        public GameObject prefab;
    }
}
