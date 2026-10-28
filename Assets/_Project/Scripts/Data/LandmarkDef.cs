using UnityEngine;

namespace DallasDash.Data
{
    /// The five Dallas landmarks shown on the skyline beside the track.
    [CreateAssetMenu(menuName = "Dallas Dash/Landmark", fileName = "Landmark")]
    public class LandmarkDef : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        /// Distance along the run (world units) where this landmark is placed on the skyline.
        public float atDistance;
        public float side = 1f;
        /// Street shown in the banner as the rider reaches it (scene 17: Singleton Blvd under the bridge).
        public string street;
        public Texture2D referenceSheet;
        public GameObject prefab;
    }
}
