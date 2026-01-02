using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HealthBar : MonoBehaviour
{
    public AudioClip Ouch;

    [Header("Health Settings")]
    public Image healthBarFill;
    public float maxHealth = 100f;

    [Header("Armor Settings")]
    public Image armorBarFill;
    public float maxArmor = 100f;        // Maximum possible armor
    public float startingArmor = 20f;    // Armor at start

    private float currentHealth;
    private float currentArmor;
    private bool isDead = false;

    // Read-only properties so other scripts (shop, UI) can read values
    public float CurrentArmor => currentArmor;
    public float MaxArmor => maxArmor;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    void Start()
    {
        currentHealth = maxHealth;
        currentArmor = startingArmor;    // Start with partial armor
        UpdateBars();
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        if (Ouch != null)
            AudioSource.PlayClipAtPoint(Ouch, transform.position);

        // Armor absorbs damage first
        if (currentArmor > 0)
        {
            float armorDamage = Mathf.Min(damage, currentArmor);
            currentArmor -= armorDamage;
            damage -= armorDamage;
        }

        // Remaining damage affects health
        if (damage > 0)
        {
            currentHealth -= damage;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }

        UpdateBars();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void UpdateBars()
    {
        // Update health bar fill
        if (healthBarFill != null)
            healthBarFill.fillAmount = currentHealth / maxHealth;

        // Update armor bar fill
        if (armorBarFill != null)
            armorBarFill.fillAmount = currentArmor / maxArmor;
    }

    void Die()
    {
        isDead = true;

        // Optional: disable player movement here
        // GetComponent<CharacterController>().enabled = false;

        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Method to restore armor (e.g., pickup or shop)
    public void AddArmor(float amount)
    {
        currentArmor += amount;
        currentArmor = Mathf.Clamp(currentArmor, 0f, maxArmor);
        UpdateBars();
    }

    // Method to heal health
    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        UpdateBars();
    }
}
