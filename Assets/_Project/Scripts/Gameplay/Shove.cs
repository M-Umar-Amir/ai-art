using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// A heavy vehicle struck head on is shoved forward by the impact, faster than the rider at first and then
    /// sliding to a stop, so the rider deflecting into the next lane never passes through its body.
    public class Shove : MonoBehaviour
    {
        Vector3 velocity;
        float life;

        void OnEnable()
        {
            float pace = GameManager.Instance ? GameManager.Instance.Speed : 20f;
            float side = transform.position.x >= 0f ? 0.6f : -0.6f;
            velocity = new Vector3(side, 0f, pace + 6f);
            life = 1.1f;
        }

        void Update()
        {
            if (GameManager.Instance && GameManager.Instance.Paused) return;
            transform.position += velocity * Time.deltaTime;
            // Tyres scrub off the shove: about 70% of it is gone within a second.
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, (velocity.magnitude * 1.2f + 4f) * Time.deltaTime);
            if ((life -= Time.deltaTime) <= 0f) enabled = false;
        }
    }
}
