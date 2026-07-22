using UnityEngine;
using TMPro;

public class EnemyCastle : MonoBehaviour
{
    [Header("Castle Settings")]
    public int maxHP = 100;
    public TextMeshPro HPText;

    [Header("World Space UI Components")]
    public GameObject visualRoot;
    public UnityEngine.UI.Slider worldSpaceHPSlider;
    public TextMeshProUGUI worldSpaceHPText;

    private int currentHP;
    private bool isLevelCompleted = false;

    private void Start()
    {
        currentHP = maxHP;
        UpdateHPText();
        ApplyVibrantColors();

        // Initialize the top Screen-Space Boss Health Bar
        if (BossHealthBar.Instance != null)
        {
            string baseName = LevelManager.Instance != null ? LevelManager.Instance.currentLevelName : "Enemy Base";
            BossHealthBar.Instance.InitializeBar(baseName.ToUpper(), maxHP);
        }

        // Initialize the World Space floating HP Slider above the castle
        if (worldSpaceHPSlider != null)
        {
            worldSpaceHPSlider.maxValue = maxHP;
            worldSpaceHPSlider.value = maxHP;
        }
    }

    private void ApplyVibrantColors()
    {
        if (LevelManager.Instance == null) return;

        Color primaryColor = LevelManager.Instance.currentNeonLeft;
        Color secondaryColor = LevelManager.Instance.currentNeonRight;

        // Create a beautiful, bright, vibrant material for the main walls
        // Instead of black, let's make it a gorgeous bright purple/magenta or stylized violet
        Color wallColor = new Color(0.2f, 0.1f, 0.4f); // Sleek bright neon-indigo/violet
        
        // Find all Renderers on the castle
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            string objName = r.gameObject.name;

            // Instantiate material for runtime customization
            Material mat = r.material;

            if (objName.Contains("Wall") || objName.Contains("Tower") || objName.Contains("Battlement"))
            {
                // Main Walls/Towers: sleek vibrant indigo-violet
                mat.color = wallColor;
                mat.SetColor("_EmissionColor", wallColor * 0.5f);
            }
            else if (objName.Contains("Door") || objName.Contains("Drawbridge"))
            {
                // Doors/Drawbridge: bright glowing golden-yellow for excellent contrast
                Color accentColor = new Color(1.0f, 0.64f, 0.0f); // Bright Cyber Gold/Orange
                mat.color = accentColor;
                mat.SetColor("_EmissionColor", accentColor * 1.5f);
            }
            else if (objName.Contains("LeftChain") || objName.Contains("LeftTowerCap") || objName.Contains("NeonArchLeft") || objName.Contains("NeonArchTop"))
            {
                // Left neon accents: match left neon theme color
                mat.color = primaryColor;
                mat.SetColor("_EmissionColor", primaryColor * 4.0f);
            }
            else if (objName.Contains("RightChain") || objName.Contains("RightTowerCap") || objName.Contains("NeonArchRight"))
            {
                // Right neon accents: match right neon theme color
                mat.color = secondaryColor;
                mat.SetColor("_EmissionColor", secondaryColor * 4.0f);
            }
        }
    }

    private void UpdateHPText()
    {
        if (HPText != null)
        {
            HPText.text = $"HP: {currentHP}";
        }

        if (worldSpaceHPText != null)
        {
            worldSpaceHPText.text = $"HP: {currentHP} / {maxHP}";
        }

        if (worldSpaceHPSlider != null)
        {
            worldSpaceHPSlider.value = currentHP;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ProcessTrigger(other);
    }

    private void OnTriggerStay(Collider other)
    {
        ProcessTrigger(other);
    }

    private void ProcessTrigger(Collider other)
    {
        if (isLevelCompleted) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        StickmanController stickman = other.GetComponent<StickmanController>();
        if (stickman != null)
        {
            // Disable the collider immediately (synchronous) rather than relying only on Destroy(),
            // which doesn't take effect until end of frame - on a slow frame, multiple FixedUpdate
            // physics ticks (default 50Hz) can re-fire OnTriggerStay for this same stickman before
            // it's actually removed, dealing double castle damage for one physical pass-through.
            other.enabled = false;
            TakeDamage(1);
            Destroy(stickman.gameObject);
        }

        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            player.IsAtEndBase = true;
            isLevelCompleted = true; // Lock state
            Win();
        }
    }

    private void TakeDamage(int amount)
    {
        if (isLevelCompleted) return;

        currentHP -= amount;
        
        if (BossHealthBar.Instance != null)
        {
            BossHealthBar.Instance.UpdateHP(currentHP);
        }

        if (currentHP <= 0 && !isLevelCompleted)
        {
            isLevelCompleted = true; // Lock the state immediately!
            currentHP = 0;
            UpdateHPText();

            // Play heavy, booming explosion sound effect
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayExplosionSFX();
            }

            // Deactivate the main castle mesh components instantly on victory
            if (visualRoot != null)
            {
                visualRoot.SetActive(false);
            }

            Win();
        }
        else if (!isLevelCompleted)
        {
            UpdateHPText();
        }
    }

    private void Win()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.VictoryState();
        }
    }
}
