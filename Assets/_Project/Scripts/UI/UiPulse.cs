using UnityEngine;

namespace DallasDash.UI
{
    /// Gentle breathing scale and optional bob for call-to-action buttons and the logo. Unscaled time.
    public class UiPulse : MonoBehaviour
    {
        public float scale = 0.035f;
        public float speed = 2.4f;
        public float bob;
        public float tilt;
        Vector2 home;
        RectTransform rt;

        void Awake()
        {
            rt = (RectTransform)transform;
            home = rt.anchoredPosition;
        }

        void Update()
        {
            float t = Time.unscaledTime * speed;
            transform.localScale = Vector3.one * (1f + Mathf.Sin(t) * scale);
            rt.anchoredPosition = home + new Vector2(0f, Mathf.Sin(t * 0.5f) * bob);
            if (tilt != 0f) transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.5f + 1f) * tilt);
        }
    }
}
