using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShopUIController : MonoBehaviour
{
    [Header("References")]
    public TPSController player;                // existing player reference
    public HealthBar playerHealthBar;           // assign the player's HealthBar in Inspector

    [Header("Stat Text")]
    public TextMeshProUGUI damageText;
    public TextMeshProUGUI critChanceText;
    public TextMeshProUGUI armorText;

    [Header("Cost Text")]
    public TextMeshProUGUI damageCostText;
    public TextMeshProUGUI critCostText;
    public TextMeshProUGUI armorCostText;

    [Header("Buttons")]
    public Button damageButton;
    public Button critButton;
    public Button armorButton;

    [Header("Damage Upgrade Settings")]
    public int damageCost = 100;                // cost to upgrade damage
    public float damageUpgradeAmount = 1f;      // how much damage is added

    [Header("Crit Chance Upgrade Settings")]
    public int critChanceCost = 150;            // cost to upgrade crit chance
    public float critChanceUpgradeAmount = 5f;  // how much crit % is added

    [Header("Armor Purchase Settings")]
    public int armorCost = 50;                  // cost to buy armor
    public float armorAmountOnBuy = 20f;        // how much armor is given

    private void Update()
    {
        if (gameObject.activeSelf)
            UpdateStats();
    }

    public void UpdateStats()
    {
        if (player == null) return;

        // Display current stats
        damageText.text = player.WeaponDamage.ToString("000");
        critChanceText.text = player.WeaponCritChance.ToString("000");

        // Display costs (now from ShopUIController)
        damageCostText.text = damageCost.ToString("000");
        critCostText.text = critChanceCost.ToString("000");

        // Armor info (if HealthBar assigned)
        if (playerHealthBar != null)
        {
            float cur = playerHealthBar.CurrentArmor;
            float max = playerHealthBar.MaxArmor;
            int percent = Mathf.RoundToInt((max > 0f) ? (cur / max * 100f) : 0f);
            armorText.text = $"{cur:000}";
        }
        else
        {
            armorText.text = "N/A";
        }
        armorCostText.text = armorCost.ToString("000");

        UpdateButtons();
    }

    void UpdateButtons()
    {
        if (PlayerMoney.Instance == null) return;

        int money = PlayerMoney.Instance.CurrentMoney;

        // Enable/disable buttons based on affordability
        damageButton.interactable = money >= damageCost;
        critButton.interactable = money >= critChanceCost;

        // Armor button also checks if armor is not already maxed
        bool canBuyArmor = money >= armorCost;
        if (playerHealthBar != null)
            canBuyArmor &= (playerHealthBar.CurrentArmor < playerHealthBar.MaxArmor);
        armorButton.interactable = canBuyArmor;
    }

    public void BuyDamage()
    {
        if (!CanAfford(damageCost)) return;

        PlayerMoney.Instance.SpendMoney(damageCost);
        player.UpgradeDamage(damageUpgradeAmount);
        UpdateStats();
    }

    public void BuyCritChance()
    {
        if (!CanAfford(critChanceCost)) return;

        PlayerMoney.Instance.SpendMoney(critChanceCost);
        player.UpgradeCritChance(critChanceUpgradeAmount);
        UpdateStats();
    }

    public void BuyArmor()
    {
        if (!CanAfford(armorCost)) return;

        if (playerHealthBar == null)
        {
            Debug.LogWarning("ShopUIController: playerHealthBar is not set. Can't add armor.");
            return;
        }

        // Check if armor is already maxed
        if (playerHealthBar.CurrentArmor >= playerHealthBar.MaxArmor)
        {
            Debug.Log("Armor already at maximum!");
            return;
        }

        PlayerMoney.Instance.SpendMoney(armorCost);
        playerHealthBar.AddArmor(armorAmountOnBuy);
        UpdateStats();
    }

    bool CanAfford(int cost)
    {
        return PlayerMoney.Instance != null && PlayerMoney.Instance.CurrentMoney >= cost;
    }
}