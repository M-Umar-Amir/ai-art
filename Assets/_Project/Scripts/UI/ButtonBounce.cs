using DallasDash.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace DallasDash.UI
{
    public class ButtonBounce : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Tween press;
        public void OnPointerDown(PointerEventData e)
        {
            var button = GetComponent<Button>();
            if (button && button.interactable) Animate(PlayerProfile.ReduceMotion ? 1f : 0.96f, 0.08f);
        }
        public void OnPointerUp(PointerEventData e) => Animate(1f, 0.18f);
        public void OnPointerExit(PointerEventData e) => Animate(1f, 0.18f);
        void Animate(float scale, float duration)
        {
            press?.Kill();
            press = transform.DOScale(scale, duration).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }
        void OnDisable() { press?.Kill(); transform.localScale = Vector3.one; }
    }
}
