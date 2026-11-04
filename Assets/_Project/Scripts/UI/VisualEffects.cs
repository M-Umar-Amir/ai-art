using DallasDash.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DallasDash.UI
{
    /// Enhanced visual effects for UI elements: glows, particles, and sophisticated animations
    public class VisualEffects : MonoBehaviour
    {
        public enum EffectType
        {
            PulseGlow,
            Shimmer,
            Bounce,
            Rotate,
            Float
        }

        public EffectType effect;
        public float intensity = 1f;
        public float speed = 1f;
        public Color glowColor = Color.white;

        private Image image;
        private CanvasGroup canvasGroup;
        private Tween tween;
        private RectTransform rect;
        private Vector2 originalPosition;
        private Quaternion originalRotation;
        private bool initialized, reduced;

        void Awake()
        {
            image = GetComponent<Image>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            rect = (RectTransform)transform;
            originalPosition = rect.anchoredPosition;
            originalRotation = transform.localRotation;
        }

        void Start()
        {
            initialized = true;
            Begin();
        }

        void OnEnable()
        {
            if (initialized) Begin();
        }

        void Update()
        {
            if (reduced != PlayerProfile.ReduceMotion) Begin();
        }

        void Begin()
        {
            tween?.Kill();
            reduced = PlayerProfile.ReduceMotion;

            // Reset to original state
            rect.anchoredPosition = originalPosition;
            transform.localRotation = originalRotation;
            transform.localScale = Vector3.one;
            if (image) image.color = glowColor;
            if (canvasGroup) canvasGroup.alpha = 1f;

            if (reduced) return;

            switch (effect)
            {
                case EffectType.PulseGlow:
                    StartPulseGlow();
                    break;
                case EffectType.Shimmer:
                    StartShimmer();
                    break;
                case EffectType.Bounce:
                    StartBounce();
                    break;
                case EffectType.Rotate:
                    StartRotate();
                    break;
                case EffectType.Float:
                    StartFloat();
                    break;
            }
        }

        void StartPulseGlow()
        {
            if (canvasGroup == null) return;
            float duration = 2f / speed;
            tween = canvasGroup.DOFade(0.6f, duration * 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        void StartShimmer()
        {
            if (image == null) return;
            float duration = 1.5f / speed;
            Color targetColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0.5f);
            tween = image.DOColor(targetColor, duration * 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        void StartBounce()
        {
            float duration = 0.8f / speed;
            float bounceAmount = 20f * intensity;
            tween = rect.DOAnchorPosY(originalPosition.y + bounceAmount, duration * 0.5f)
                .SetEase(Ease.OutQuad)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        void StartRotate()
        {
            float duration = 4f / speed;
            tween = transform.DORotate(new Vector3(0, 0, 15f * intensity), duration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        void StartFloat()
        {
            float duration = 3f / speed;
            float floatAmount = 15f * intensity;
            tween = rect.DOAnchorPosY(originalPosition.y + floatAmount, duration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        void OnDisable()
        {
            tween?.Kill();
            rect.anchoredPosition = originalPosition;
            transform.localRotation = originalRotation;
            transform.localScale = Vector3.one;
            if (image) image.color = glowColor;
            if (canvasGroup) canvasGroup.alpha = 1f;
        }

        /// Public method to trigger a one-time effect
        public void TriggerEffect(EffectType type, float duration = 0.5f)
        {
            tween?.Kill();
            reduced = PlayerProfile.ReduceMotion;
            if (reduced) return;

            switch (type)
            {
                case EffectType.PulseGlow:
                    if (canvasGroup)
                        tween = canvasGroup.DOFade(0.3f, duration)
                            .SetLoops(2, LoopType.Yoyo)
                            .OnComplete(() => canvasGroup.alpha = 1f);
                    break;
                case EffectType.Bounce:
                    tween = rect.DOAnchorPosY(originalPosition.y + 30f * intensity, duration * 0.5f)
                        .SetEase(Ease.OutQuad)
                        .SetLoops(2, LoopType.Yoyo)
                        .OnComplete(() => rect.anchoredPosition = originalPosition);
                    break;
            }
        }
    }
}
