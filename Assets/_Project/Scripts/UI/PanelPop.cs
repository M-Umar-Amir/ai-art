using DallasDash.Core;
using DG.Tweening;
using UnityEngine;
namespace DallasDash.UI
{
    public class PanelPop : MonoBehaviour
    {
        Tween scale, fade;
        CanvasGroup group;
        void OnEnable()
        {
            group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            scale?.Kill(); fade?.Kill(); group.alpha = 0f;
            transform.localScale = Vector3.one * (PlayerProfile.ReduceMotion ? 1f : 0.94f);
            fade = group.DOFade(1f, 0.18f).SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            if (!PlayerProfile.ReduceMotion)
                scale = transform.DOScale(1f, 0.28f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }
        void OnDisable() { scale?.Kill(); fade?.Kill(); }
    }
}
