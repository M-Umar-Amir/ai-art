using UnityEngine;

namespace DallasDash.Gameplay
{
    /// An obstacle the Sugar Rush rider ploughs through: it tumbles up and off to the side, then goes away.
    public class Knockback : MonoBehaviour
    {
        Vector3 velocity, spin;
        float life = 1.2f;

        void OnEnable()
        {
            life = 1.2f;
            float side = transform.position.x >= 0f ? 1f : -1f;
            // Thrown ahead faster than the rider is going, so the rider never drives through what they hit.
            float pace = DallasDash.Core.GameManager.Instance ? DallasDash.Core.GameManager.Instance.Speed : 20f;
            velocity = new Vector3(side * Random.Range(5f, 8f), Random.Range(6f, 9f), pace + Random.Range(5f, 9f));
            spin = Random.insideUnitSphere * 720f;
        }

        void Update()
        {
            velocity.y -= 22f * Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
            transform.Rotate(spin * Time.deltaTime, Space.World);
            life -= Time.deltaTime;
            if (life <= 0f) gameObject.SetActive(false);
        }
    }
}
