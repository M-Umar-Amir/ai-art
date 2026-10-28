using UnityEngine;

namespace DallasDash.UI
{
    /// Menu panels pop in with a small overshoot each time they open. Runs on unscaled time so it works while paused.
    public class PanelPop : MonoBehaviour
    {
        float t;

        void OnEnable()
        {
            t = 0f;
            transform.localScale = Vector3.one * 0.8f;
        }

        void Update()
        {
            if (t >= 1f) return;
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / 0.22f);
            // Ease-out-back: overshoots to about 1.06, then settles.
            float k = t - 1f;
            float s = 1f + k * k * ((1.7f + 1f) * k + 1.7f);
            transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, s);
        }
    }
}
