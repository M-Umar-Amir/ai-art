using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Walk-in-place for the jaywalking shopper: a step bounce with a side-to-side sway,
    /// so the unrigged mesh reads as walking across the street.
    public class Bobber : MonoBehaviour
    {
        public float stepsPerSecond = 2.2f;
        public float bounce = 0.06f;
        public float sway = 5f;

        Vector3 basePos;
        Quaternion baseRot;
        float phase;

        void Start()
        {
            basePos = transform.localPosition;
            baseRot = transform.localRotation;
            phase = Random.value * 10f;
        }

        void Update()
        {
            float p = (Time.time + phase) * stepsPerSecond * Mathf.PI;
            transform.localPosition = basePos + Vector3.up * Mathf.Abs(Mathf.Sin(p)) * bounce;
            transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, Mathf.Sin(p) * sway);
        }
    }
}
