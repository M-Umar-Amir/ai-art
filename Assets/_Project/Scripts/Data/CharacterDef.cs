using UnityEngine;

namespace DallasDash.Data
{
    /// Finn, Faye and Fizz-11 from the Food Inc Game asset pack (24 Oct 2026).
    [CreateAssetMenu(menuName = "Dallas Dash/Character", fileName = "Character")]
    public class CharacterDef : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Unlock unlock = Unlock.Free;
        public int unlockPoints;
        /// Fizz-11 rolls on his own tire and never mounts a ride; he needs the rolling-base rig, not a biped.
        public bool rollingBase;
        public Texture2D referenceSheet;
        public GameObject prefab;
    }
}
