using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Burgers turn and bob so they read as collectibles from far down the street.
    public class PickupSpin : MonoBehaviour
    {
        public float degreesPerSecond = 140f;
        public float bob = 0.15f;

        float baseY, phase;

        void OnEnable()
        {
            baseY = transform.position.y;
            phase = transform.position.z * 0.7f;
        }

        void Update()
        {
            transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World);
            var p = transform.position;
            p.y = baseY + Mathf.Sin(Time.time * 4f + phase) * bob;
            transform.position = p;
        }
    }
}
