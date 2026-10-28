using UnityEngine;
using UnityEngine.EventSystems;

namespace DallasDash.UI
{
    /// Chunky press feel: the button squashes while held and springs back on release.
    public class ButtonBounce : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        float target = 1f, scale = 1f, velocity;

        public void OnPointerDown(PointerEventData e) => target = 0.92f;
        public void OnPointerUp(PointerEventData e) => target = 1f;
        public void OnPointerExit(PointerEventData e) => target = 1f;

        void Update()
        {
            // Critically damped spring with a little bounce, on unscaled time for the pause menu.
            float dt = Time.unscaledDeltaTime;
            velocity += ((target - scale) * 900f - velocity * 28f) * dt;
            scale += velocity * dt;
            transform.localScale = Vector3.one * scale;
        }
    }
}
