using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    public class DashRhythmGate : MonoBehaviour
    {
        public Transform beam;
        public bool Open => GameManager.Instance && Mathf.Repeat(GameManager.Instance.RunTime * (126f * 1.13f / 60f), 4f) < 1.4f;
        Vector3 closed;
        void Start() { if (beam) closed = beam.localPosition; }
        void Update()
        {
            if (!beam || !GameManager.Instance || GameManager.Instance.Paused) return;
            var target = closed + Vector3.up * (Open ? 2.2f : 0f);
            beam.localPosition = Vector3.MoveTowards(beam.localPosition, target, Time.deltaTime * 14f);
        }
        public bool Clear => beam && beam.localPosition.y > closed.y + 1.2f;
    }
}
