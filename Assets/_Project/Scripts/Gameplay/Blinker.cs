using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Hazard lights on a stopped van, the lamp on a roadworks barrier: switches its renderer on and off.
    public class Blinker : MonoBehaviour
    {
        public float perSecond = 1.4f;

        Renderer lamp;
        float phase;

        void Start()
        {
            lamp = GetComponent<Renderer>();
            phase = Random.value;
        }

        void Update()
        {
            if (lamp) lamp.enabled = Mathf.Repeat(Time.time * perSecond + phase, 1f) < 0.55f;
        }
    }
}
