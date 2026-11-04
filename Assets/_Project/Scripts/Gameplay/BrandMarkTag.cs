using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Registers a logo face on a prefab (Ace's courier box) when it is shown.
    public class BrandMarkTag : MonoBehaviour
    {
        public bool rider = true;
        bool registered;
        void OnEnable() { if (!registered && TryGetComponent<Renderer>(out var r)) { BrandVisibility.Live(r, rider); registered = true; } }
    }
}
