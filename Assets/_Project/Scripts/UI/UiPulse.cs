using DallasDash.Core;
using DG.Tweening;
using UnityEngine;
namespace DallasDash.UI
{
    public class UiPulse : MonoBehaviour
    {
        public float scale = 0.018f, speed = 2.4f, bob, tilt;
        RectTransform rect;
        Vector2 home;
        Tween pulse;
        bool initialized, reduced;
        void Awake() { rect = (RectTransform)transform; home = rect.anchoredPosition; }
        void Start() { initialized = true; Begin(); }
        void OnEnable() { if (initialized) Begin(); }
        void Update() { if (reduced != PlayerProfile.ReduceMotion) Begin(); }
        void Begin()
        {
            pulse?.Kill(); reduced = PlayerProfile.ReduceMotion;
            transform.localScale = Vector3.one; rect.anchoredPosition = home; transform.localRotation = Quaternion.identity;
            if (reduced) return;
            float phase = 0f;
            pulse = DOTween.To(() => phase, x =>
            {
                phase = x; transform.localScale = Vector3.one * (1f + Mathf.Sin(x) * scale);
                rect.anchoredPosition = home + Vector2.up * Mathf.Sin(x * 0.5f) * bob;
                if (tilt != 0f) transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(x * 0.5f) * tilt);
            }, Mathf.PI * 4f, Mathf.PI * 4f / Mathf.Max(0.1f, speed))
                .SetEase(Ease.Linear).SetLoops(-1).SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }
        void OnDisable()
        {
            pulse?.Kill(); transform.localScale = Vector3.one;
            if (rect) rect.anchoredPosition = home;
            transform.localRotation = Quaternion.identity;
        }
    }
}
