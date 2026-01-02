using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItem : MonoBehaviour
{
    public enum UpgradeType { Damage, CritChance, Armor }

    [Header("This item")]
    public UpgradeType upgradeType;

    [Header("References (assign in Inspector)")]
    public Button button;
    public TextMeshProUGUI costText;          // displays the cost
    public ShopUIController shopUIController; // REQUIRED: holds all prices and upgrade values
    public TPSController player;              // used for applying upgrades
    public HealthBar playerHealthBar;         // used for armor upgrades

    void Start()
    {
        if (button == null) button = GetComponent<Button>();
        if (shopUIController == null) shopUIController = FindObjectOfType<ShopUIController>();
        if (player == null) player = FindObjectOfType<TPSController>();
        if (playerHealthBar == null) playerHealthBar = FindObjectOfType<HealthBar>();

        if (shopUIController == null)
        {
            Debug.LogError("ShopItem: ShopUIController not found! This item will not work properly.");
        }
    }

    void Update()
    {
        if (shopUIController == null) return;

        int currentPrice = GetCurrentPrice();

        // Update cost text if assigned
        if (costText != null)
            costText.text = currentPrice.ToString("000");

        // Check if player can afford this item
        bool canAfford = (PlayerMoney.Instance != null && PlayerMoney.Instance.CurrentMoney >= currentPrice);

        // For armor, also check if not already maxed
        if (upgradeType == UpgradeType.Armor && playerHealthBar != null)
            canAfford &= (playerHealthBar.CurrentArmor < playerHealthBar.MaxArmor);

        if (button != null)
            button.interactable = canAfford;
    }

    int GetCurrentPrice()
    {
        if (shopUIController == null) return 0;

        switch (upgradeType)
        {
            case UpgradeType.Damage:
                return shopUIController.damageCost;

            case UpgradeType.CritChance:
                return shopUIController.critChanceCost;

            case UpgradeType.Armor:
                return shopUIController.armorCost;

            default:
                return 0;
        }
    }

    public void Buy()
    {
        if (PlayerMoney.Instance == null)
        {
            Debug.LogWarning("No PlayerMoney instance found.");
            return;
        }

        if (shopUIController == null)
        {
            Debug.LogWarning("No ShopUIController assigned to ShopItem.");
            return;
        }

        int priceToSpend = GetCurrentPrice();

        // Attempt to spend the current price
        if (!PlayerMoney.Instance.SpendMoney(priceToSpend))
        {
            Debug.Log($"Can't afford {upgradeType}: price {priceToSpend}, have {PlayerMoney.Instance.CurrentMoney}");
            return;
        }

        // Apply the upgrade
        switch (upgradeType)
        {
            case UpgradeType.Damage:
                if (player != null)
                {
                    player.UpgradeDamage(shopUIController.damageUpgradeAmount);
                }
                break;

            case UpgradeType.CritChance:
                if (player != null)
                {
                    player.UpgradeCritChance(shopUIController.critChanceUpgradeAmount);
                }
                break;

            case UpgradeType.Armor:
                if (playerHealthBar != null)
                {
                    // Check if armor is already maxed
                    if (playerHealthBar.CurrentArmor >= playerHealthBar.MaxArmor)
                    {
                        Debug.Log("Armor already at maximum!");
                        // Refund the money since we couldn't apply the upgrade
                        PlayerMoney.Instance.AddMoney(priceToSpend);
                        return;
                    }
                    playerHealthBar.AddArmor(shopUIController.armorAmountOnBuy);
                }
                break;
        }

        Debug.Log($"Bought {upgradeType} for {priceToSpend}. New money: {PlayerMoney.Instance.CurrentMoney}");
    }
}