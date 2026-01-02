using UnityEngine;
using TMPro;

public class PlayerMoney : MonoBehaviour
{
    public static PlayerMoney Instance;

    [SerializeField] private int money = 0;
    [SerializeField] private TMP_Text moneyText;
    public int CurrentMoney => money;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        UpdateUI();
    }

    public void AddMoney(int amount)
    {
        money += amount;
        UpdateUI();
    }

    public bool SpendMoney(int amount)
    {
        if (money < amount)
            return false;

        money -= amount;
        UpdateUI();
        return true;
    }

    private void UpdateUI()
    {
        moneyText.text = money.ToString("000");
    }
}
