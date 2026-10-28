using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Keeps a quad turned to the camera (burger halos).
    public class Billboard : MonoBehaviour
    {
        static Transform cam;

        void LateUpdate()
        {
            if (!cam) cam = Camera.main ? Camera.main.transform : null;
            if (cam) transform.rotation = cam.rotation;
        }
    }
}
