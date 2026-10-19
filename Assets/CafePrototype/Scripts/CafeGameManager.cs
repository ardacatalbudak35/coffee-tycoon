using UnityEngine;
using UnityEngine.UI;

public class CafeGameManager : MonoBehaviour
{
    public static CafeGameManager Instance { get; private set; }
    public int Money { get; private set; } = 100;
    public Text moneyText;
    public Text messageText;
    public int upgradeLevel = 1;
    private float messageTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Money = PlayerPrefs.GetInt("CafeMoney", 100);
        upgradeLevel = PlayerPrefs.GetInt("CafeUpgrade", 1);
    }

    private void Update()
    {
        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0f && messageText != null) messageText.text = string.Empty;
        }
        RefreshMoney();
    }

    public bool TrySpend(int amount)
    {
        if (Money < amount) { ShowMessage("Yeterli paran yok!"); return false; }
        Money -= amount;
        return true;
    }

    public void AddMoney(int amount)
    {
        Money += amount;
        ShowMessage("+" + amount + " coin kazandın!");
        Save();
    }

    public void SetUpgradeLevel(int value)
    {
        upgradeLevel = value;
        Save();
    }

    public void Save()
    {
        PlayerPrefs.SetInt("CafeMoney", Money);
        PlayerPrefs.SetInt("CafeUpgrade", upgradeLevel);
        PlayerPrefs.Save();
    }

    public void ShowMessage(string message)
    {
        if (messageText == null) return;
        messageText.text = message;
        messageTimer = 2.5f;
    }

    private void RefreshMoney()
    {
        if (moneyText != null) moneyText.text = "☕ " + Money;
    }
}
