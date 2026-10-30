using DG.Tweening;
using UnityEngine;

namespace DallasDash.UI
{
    public static class TweenSetup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init() => DOTween.Init(false, true, LogBehaviour.ErrorsOnly).SetCapacity(512, 128);
    }
}
