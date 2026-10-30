using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// An obstacle the rider runs into: shoved ahead faster than the (now slowed) rider and tipped over onto
    /// its back, sliding to a stop off to the side. So a hit lands physically instead of the rider ghosting
    /// through it.
    public class Topple : MonoBehaviour
    {
        Vector3 velocity;
        Quaternion from, to;
        float t;
        float baseY;

        void OnEnable()
        {
            t = 0f;
            var gm = GameManager.Instance;
            float side = transform.position.x > 0.5f ? 1f : transform.position.x < -0.5f ? -1f : (Random.value < 0.5f ? -1f : 1f);
            velocity = new Vector3(side * 2.2f, 3.2f, (gm ? gm.Speed : 20f) * 0.9f + 4f);
            from = transform.rotation;
            to = Quaternion.Euler(80f, Random.Range(-25f, 25f), side * -20f) * from;
            // Overhead banners drop to the road; everything else stays on the ground it stood on.
            baseY = transform.position.y > 1f ? 0.25f : transform.position.y;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            t += dt;
            velocity.y -= 24f * dt;
            var p = transform.position + velocity * dt;
            if (p.y < baseY)
            {
                // Landed: scrape along and slow down.
                p.y = baseY;
                velocity.y = 0f;
                velocity.x *= 1f - 3f * dt;
                velocity.z *= 1f - 2.2f * dt;
            }
            transform.position = p;
            transform.rotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t / 0.45f));
            if (t > 2.5f) gameObject.SetActive(false);
        }
    }
}
