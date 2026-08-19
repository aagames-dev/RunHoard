using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UpgradeShop : MonoBehaviour
{
    [Header("Starting Army Upgrade")]
    public Button armyButton;
    public TextMeshProUGUI armyLevelText;
    public TextMeshProUGUI armyCostText;

    [Header("Hat Shop Upgrade")]
    public Button hatButton;
    public TextMeshProUGUI hatCostText;

    private void Start()
    {
        UpdateShopUI();

        if (armyButton != null) armyButton.onClick.AddListener(BuyStartingArmy);
        if (hatButton != null) hatButton.onClick.AddListener(ToggleHatShop);
    }

    private void OnEnable()
    {
        UpdateShopUI();
    }

    public void UpdateShopUI()
    {
        // 1. Starting Army Upgrade
        int armyLevel = PlayerPrefs.GetInt("StartingArmyLevel", 0);
        int armyCost = 200 + (armyLevel * 100);
        int stickmenCount = armyLevel * 5;

        if (armyLevelText != null)
        {
            armyLevelText.text = $"STARTING ARMY: +{stickmenCount} STICKMEN (Lvl {armyLevel})";
        }

        if (armyCostText != null)
        {
            armyCostText.text = $"{armyCost} COINS";
        }

        // 2. Hat Shop Upgrade
        int hasPremiumHat = PlayerPrefs.GetInt("HasPremiumHat", 0); // 0 = Locked, 1 = Unlocked
        int equippedHat = PlayerPrefs.GetInt("EquippedHat", 0); // 0 = Explorer, 1 = Golden Crown

        if (hatCostText != null)
        {
            if (hasPremiumHat == 0)
            {
                hatCostText.text = "BUY CROWN: 50 COINS";
            }
            else
            {
                hatCostText.text = equippedHat == 0 ? "EQUIP GOLDEN CROWN" : "EQUIP EXPLORER HAT";
            }
        }

        // Apply interactive button states based on coins
        int totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);
        if (armyButton != null)
        {
            armyButton.interactable = totalCoins >= armyCost;
        }

        if (hatButton != null)
        {
            if (hasPremiumHat == 0)
            {
                hatButton.interactable = totalCoins >= 50;
            }
            else
            {
                hatButton.interactable = true; // Equipping is free
            }
        }
    }

    private void BuyStartingArmy()
    {
        int armyLevel = PlayerPrefs.GetInt("StartingArmyLevel", 0);
        int armyCost = 200 + (armyLevel * 100);

        if (GameManager.Instance != null && GameManager.Instance.TotalCoins >= armyCost)
        {
            // Deduct coins
            GameManager.Instance.AddCoins(-armyCost);

            // Increment level and army count
            armyLevel++;
            int stickmenCount = armyLevel * 5;

            PlayerPrefs.SetInt("StartingArmyLevel", armyLevel);
            PlayerPrefs.SetInt("StartingStickmenCount", stickmenCount);
            PlayerPrefs.Save();

            // Play satisfying pop sound
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPopSFX();
            }

            UpdateShopUI();
        }
    }

    private void ToggleHatShop()
    {
        int hasPremiumHat = PlayerPrefs.GetInt("HasPremiumHat", 0);
        int equippedHat = PlayerPrefs.GetInt("EquippedHat", 0);

        if (hasPremiumHat == 0)
        {
            // Try to buy premium hat
            if (GameManager.Instance != null && GameManager.Instance.TotalCoins >= 50)
            {
                GameManager.Instance.AddCoins(-50);

                PlayerPrefs.SetInt("HasPremiumHat", 1);
                PlayerPrefs.SetInt("EquippedHat", 1); // Automatically equip on purchase
                PlayerPrefs.Save();

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayPopSFX();
                }

                // Update Hat-Man real-time appearance if spawned
                if (PlayerController.Instance != null)
                {
                    PlayerController.Instance.ApplyEquippedHat();
                }

                UpdateShopUI();
            }
        }
        else
        {
            // Toggle Equipped state
            equippedHat = equippedHat == 0 ? 1 : 0;
            PlayerPrefs.SetInt("EquippedHat", equippedHat);
            PlayerPrefs.Save();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPopSFX();
            }

            // Update Hat-Man real-time appearance if spawned
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.ApplyEquippedHat();
            }

            UpdateShopUI();
        }
    }
}
