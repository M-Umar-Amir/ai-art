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
            velocity = new Vector3(side * Random.Range(5f, 8f), Random.Range(6f, 9f), Random.Range(8f, 14f));
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
