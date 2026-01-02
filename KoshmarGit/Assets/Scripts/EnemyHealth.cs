using UnityEngine;
using System;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    // 🔴 Notify on death
    public Action OnDeath;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        PlayerMoney.Instance.AddMoney(1);

        // 🔴 Notify spawner / other systems
        OnDeath?.Invoke();

        Destroy(gameObject);
    }
}
