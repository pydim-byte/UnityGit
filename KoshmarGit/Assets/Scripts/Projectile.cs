using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Damage")]
    public float damage = 10f;

    [Header("Lifetime")]
    public float lifeTime = 5f;

    [Header("Collision")]
    public LayerMask hitMask; // Optional (Player layer recommended)

    private void Start()
    {
        // Auto destroy in case it never hits anything
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Deal damage only if player
        if (collision.gameObject.CompareTag("Player"))
        {
            HealthBar health = collision.gameObject.GetComponent<HealthBar>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
        }

        // Destroy projectile on ANY collision
        Destroy(gameObject);
    }
}
