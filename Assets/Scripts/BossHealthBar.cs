using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthBar : MonoBehaviour
{
    public static BossHealthBar Instance { get; private set; }

    [Header("UI Components")]
    public Slider healthSlider;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI bossNameText;

    private int maxHP;
    private int currentHP;
    private float targetFillValue;
    public float fillSpeed = 5f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Hide on start until active
        gameObject.SetActive(false);
    }

    public void InitializeBar(string name, int maxHP)
    {
        gameObject.SetActive(true);
        this.maxHP = maxHP;
        this.currentHP = maxHP;
        
        if (bossNameText != null)
        {
            bossNameText.text = name;
        }

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHP;
            healthSlider.value = maxHP;
        }

        targetFillValue = maxHP;
        UpdateText();
    }

    public void UpdateHP(int newHP)
    {
        currentHP = Mathf.Clamp(newHP, 0, maxHP);
        targetFillValue = currentHP;
        UpdateText();
    }

    private void UpdateText()
    {
        if (hpText != null)
        {
            hpText.text = $"HP: {currentHP} / {maxHP}";
        }
    }

    private void Update()
    {
        if (healthSlider != null)
        {
            // Smoothly animate the health bar slide
            healthSlider.value = Mathf.MoveTowards(healthSlider.value, targetFillValue, fillSpeed * maxHP * Time.deltaTime);
        }
    }

    public void HideBar()
    {
        gameObject.SetActive(false);
    }
}
