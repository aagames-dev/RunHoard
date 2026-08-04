using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindFirstObjectByType<GameManager>();
            }
            return _instance;
        }
    }

    public enum GameState { Start, Playing, Victory, GameOver, Paused }
    public GameState CurrentState { get; private set; } = GameState.Start;
    public static int currentLevelIndex = 1;

    private GameObject pausePanel;

    // Tap-to-pause/resume input tracking (no dedicated button - a quick, low-movement tap toggles it).
    private const float TapMaxDuration = 0.35f;
    private const float TapMaxDragDistance = 30f;
    private Vector2 tapStartPos;
    private float tapStartTime;
    private bool isTapTracking = false;

    // Survives scene reloads (retry/next level) but resets to false on every fresh app process launch,
    // so it reliably distinguishes "just opened the app" from "reloaded the scene mid-session".
    private static bool hasResetForThisAppLaunch = false;

    [Header("UI Panels")]
    public GameObject startPanel;
    public GameObject victoryPanel;
    public GameObject gameOverPanel;
    public GameObject gameplayUI;
    public GameObject shopPanel;

    [Header("UI Texts")]
    public TextMeshProUGUI crowdCountText;
    public TextMeshProUGUI victoryScoreText;
    public TextMeshProUGUI levelText;

    [Header("Coin UI")]
    public TextMeshProUGUI coinCountText;
    public RectTransform coinIconRect;

    public int TotalCoins { get; private set; } = 0;
    private Coroutine punchScaleCoroutine;
    private int displayedCoins = 0;
    private Coroutine coinAnimateCoroutine;

    private Coroutine crowdPunchScaleCoroutine;
    private int lastDisplayedCrowdCount = -1;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        HandleTapToPauseInput();
    }

    private void Start()
    {
        // Find every single particle system instance loaded in the active scene and instantly wipe them out
        // Destroying the ParticleSystem component itself rather than the entire GameObject ensures that
        // essential game structures, background meshes, and UI elements are preserved while completely neutralizing lag.
        ParticleSystem[] allParticles = FindObjectsOfType<ParticleSystem>();
        foreach (ParticleSystem ps in allParticles)
        {
            if (ps != null)
            {
                Destroy(ps);
            }
        }

        // Economy Initialization: Reset starting state on very first launch to fix starting coins bug.
        if (!PlayerPrefs.HasKey("HasPlayedBefore"))
        {
            PlayerPrefs.SetInt("TotalCoins", 0);
            PlayerPrefs.SetInt("StartingArmyLevel", 0);
            PlayerPrefs.SetInt("StartingStickmenCount", 0);
            PlayerPrefs.SetInt("HasPremiumHat", 0);
            PlayerPrefs.SetInt("EquippedHat", 0);
            PlayerPrefs.SetInt("HasPlayedBefore", 1);
            PlayerPrefs.Save();
        }

        // Always begin at level 1 with 0 coins and no purchased starting-army upgrade when the app itself
        // is (re)launched. This flag is a static, so it survives the scene reloads RestartGame()/AdvanceLevel()
        // perform mid-session (letting normal level progression and coin earnings keep working within one
        // sitting), but resets to false on every new app process.
        if (!hasResetForThisAppLaunch)
        {
            hasResetForThisAppLaunch = true;
            PlayerPrefs.SetInt("CurrentLevel", 1);
            PlayerPrefs.SetInt("TotalCoins", 0);
            PlayerPrefs.SetInt("StartingArmyLevel", 0);
            PlayerPrefs.SetInt("StartingStickmenCount", 0);
            PlayerPrefs.Save();
        }

        currentLevelIndex = LevelManager.CurrentLevel;
        TotalCoins = PlayerPrefs.GetInt("TotalCoins", 0);
        displayedCoins = TotalCoins;
        UpdateCoinUI();
        ShowStartScreen();
        WireUpButtons();
        UpdateLevelText();

        // Apply custom retro-cyberpunk styling to coin and level HUD elements
        StyleCoinText();
        StyleLevelText();
        StyleVictoryPanel();
        StyleCrowdText();
        BuildPauseUI();
    }

    public void AddCoins(int amount)
    {
        TotalCoins += amount;
        if (TotalCoins < 0) TotalCoins = 0; // Prevent negative coins
        PlayerPrefs.SetInt("TotalCoins", TotalCoins);
        PlayerPrefs.Save();
        
        // Start sequential tick down/up countdown animation
        if (coinAnimateCoroutine != null) StopCoroutine(coinAnimateCoroutine);
        coinAnimateCoroutine = StartCoroutine(AnimateCoinCounterRoutine());

        // Trigger satisfying UI squash and stretch scale bounce animation
        if (coinIconRect != null)
        {
            if (punchScaleCoroutine != null) StopCoroutine(punchScaleCoroutine);
            punchScaleCoroutine = StartCoroutine(PunchScale(coinIconRect));
        }
    }

    private void UpdateCoinUI()
    {
        if (coinCountText != null)
        {
            coinCountText.text = displayedCoins.ToString("N0");
        }
    }

    private System.Collections.IEnumerator AnimateCoinCounterRoutine()
    {
        while (displayedCoins != TotalCoins)
        {
            int difference = Mathf.Abs(TotalCoins - displayedCoins);
            int step = 1;
            
            // If the difference is huge, count in larger increments but still animate sequentially
            if (difference > 50)
            {
                step = difference / 15; // Complete animation in roughly 15 frames
                if (step < 1) step = 1;
            }

            if (displayedCoins < TotalCoins)
            {
                displayedCoins += step;
                if (displayedCoins > TotalCoins) displayedCoins = TotalCoins;
            }
            else
            {
                displayedCoins -= step;
                if (displayedCoins < TotalCoins) displayedCoins = TotalCoins;
            }

            if (coinCountText != null)
            {
                coinCountText.text = displayedCoins.ToString("N0");
            }

            // Trigger a mini-punch on each tick
            if (coinIconRect != null)
            {
                // A very subtle scale bounce on individual ticks
                coinIconRect.localScale = Vector3.one * 1.15f;
            }

            yield return new WaitForSeconds(0.03f);
        }

        // Return coin icon back to normal scale
        if (coinIconRect != null)
        {
            coinIconRect.localScale = Vector3.one;
        }

        coinAnimateCoroutine = null;
    }

    private System.Collections.IEnumerator PunchScale(RectTransform target)
    {
        // Smooth sine bounce profile
        yield return ScaleTween.Run(target, Vector3.one, t => 1f + Mathf.Sin(t * Mathf.PI) * 0.35f, 0.25f, true);
    }

    private void StyleCoinText()
    {
        if (coinCountText != null)
        {
            TMP_FontAsset oswald = Resources.Load<TMP_FontAsset>("Fonts & Materials/Oswald Bold SDF");
            if (oswald != null)
            {
                coinCountText.font = oswald;
            }
            coinCountText.fontSize = 48f;
            
            TMP_ColorGradient yellowOrange = Resources.Load<TMP_ColorGradient>("Color Gradient Presets/Yellow to Orange - Vertical");
            if (yellowOrange != null)
            {
                coinCountText.enableVertexGradient = true;
                coinCountText.colorGradientPreset = yellowOrange;
            }
        }
    }

    private void StyleLevelText()
    {
        if (levelText != null)
        {
            TMP_FontAsset oswald = Resources.Load<TMP_FontAsset>("Fonts & Materials/Oswald Bold SDF");
            if (oswald != null)
            {
                levelText.font = oswald;
            }
            levelText.fontSize = 36f;
            levelText.alignment = TextAlignmentOptions.Right;
            
            TMP_ColorGradient bluePurple = Resources.Load<TMP_ColorGradient>("Color Gradient Presets/Blue to Purple - Vertical");
            if (bluePurple != null)
            {
                levelText.enableVertexGradient = true;
                levelText.colorGradientPreset = bluePurple;
            }
        }
    }

    private void StyleVictoryPanel()
    {
        if (victoryPanel == null) return;

        // Replace the old flat full-screen color wash with a dim translucent backdrop
        UnityEngine.UI.Image backdrop = victoryPanel.GetComponent<UnityEngine.UI.Image>();
        if (backdrop != null)
        {
            backdrop.color = new Color(0.02f, 0.02f, 0.06f, 0.78f);
        }

        if (victoryPanel.transform.Find("VictoryWindow_Border") != null) return; // Already built

        // Tie the window's neon frame color to the current level's theme for visual consistency
        Color accentA = Color.cyan;
        Color accentB = new Color(0.8f, 0f, 1f);
        if (LevelManager.Instance != null)
        {
            accentA = LevelManager.Instance.currentNeonLeft;
            accentB = LevelManager.Instance.currentNeonRight;
        }

        // Neon-colored outer border rect
        RectTransform border = CreateCenteredUIRect(victoryPanel.transform, "VictoryWindow_Border", new Vector2(680f, 420f), Vector2.zero);
        UnityEngine.UI.Image borderImg = border.gameObject.AddComponent<UnityEngine.UI.Image>();
        borderImg.color = accentA;
        borderImg.raycastTarget = false;
        border.SetAsFirstSibling();

        // Dark indigo card sitting on top of the border, leaving a clean glowing frame
        RectTransform panel = CreateCenteredUIRect(victoryPanel.transform, "VictoryWindow_Panel", new Vector2(656f, 396f), Vector2.zero);
        UnityEngine.UI.Image panelImg = panel.gameObject.AddComponent<UnityEngine.UI.Image>();
        panelImg.color = new Color(0.07f, 0.05f, 0.18f, 0.97f);
        panelImg.raycastTarget = false;
        panel.SetSiblingIndex(1);

        // Thin accent divider between the title and the score line
        RectTransform accentBar = CreateCenteredUIRect(victoryPanel.transform, "VictoryWindow_AccentBar", new Vector2(520f, 4f), new Vector2(0f, 55f));
        UnityEngine.UI.Image accentImg = accentBar.gameObject.AddComponent<UnityEngine.UI.Image>();
        accentImg.color = accentB;
        accentImg.raycastTarget = false;
        accentBar.SetSiblingIndex(2);

        // Give the "Next Level" button a warm gold call-to-action look plus the game's standard button pulse juice
        UnityEngine.UI.Button btn = victoryPanel.GetComponentInChildren<UnityEngine.UI.Button>();
        if (btn != null)
        {
            UnityEngine.UI.Image btnImg = btn.GetComponent<UnityEngine.UI.Image>();
            if (btnImg != null)
            {
                btnImg.color = new Color(1f, 0.78f, 0.1f);
            }
            TextMeshProUGUI btnLabel = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnLabel != null)
            {
                btnLabel.color = new Color(0.08f, 0.05f, 0.02f);
                btnLabel.fontStyle = FontStyles.Bold;
            }
            if (btn.GetComponent<UIButtonJuice>() == null)
            {
                btn.gameObject.AddComponent<UIButtonJuice>();
            }
        }
    }

    private RectTransform CreateCenteredUIRect(Transform parent, string name, Vector2 size, Vector2 anchoredPosition)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPosition;
        return rt;
    }

    private void StyleCrowdText()
    {
        if (crowdCountText == null || coinCountText == null) return;

        // Merge the crowd count into the existing coin status panel as a second row, reusing its
        // pill-shaped background, instead of building a separate standalone widget.
        Transform coinPanel = coinCountText.transform.parent;
        if (coinPanel == null) return;
        if (crowdCountText.transform.parent == coinPanel) return; // Already merged

        RectTransform coinPanelRT = coinPanel.GetComponent<RectTransform>();
        if (coinPanelRT != null)
        {
            // Grow the panel downward to fit the extra row (pivot is top-left, so this extends the bottom edge)
            Vector2 size = coinPanelRT.sizeDelta;
            coinPanelRT.sizeDelta = new Vector2(size.x, size.y + 40f);
        }

        // The coin text was originally anchored to the panel's vertical *center* (anchorMin/Max.y = 0.5),
        // which was fine while it was the panel's only content. But that center point shifts downward as
        // the panel grows taller to fit the new row below, dragging the coin number down into the crowd
        // row and overlapping it. Re-anchor it to the top instead so its position is fixed regardless of
        // how tall the panel is, and pin the crowd row to the top too, directly below it.
        RectTransform coinRT = coinCountText.rectTransform;
        coinRT.anchorMin = new Vector2(coinRT.anchorMin.x, 1f);
        coinRT.anchorMax = new Vector2(coinRT.anchorMax.x, 1f);
        coinRT.pivot = new Vector2(coinRT.pivot.x, 1f);
        coinRT.anchoredPosition = new Vector2(coinRT.anchoredPosition.x, -10f);

        RectTransform textRT = crowdCountText.rectTransform;
        textRT.SetParent(coinPanel, false);
        textRT.anchorMin = new Vector2(0f, 1f);
        textRT.anchorMax = new Vector2(1f, 1f);
        textRT.pivot = new Vector2(0f, 1f);
        textRT.anchoredPosition = new Vector2(95f, -74f); // same left offset as the coin row, one row below it
        textRT.sizeDelta = new Vector2(-115f, 36f);

        TMP_FontAsset oswald = Resources.Load<TMP_FontAsset>("Fonts & Materials/Oswald Bold SDF");
        if (oswald != null)
        {
            crowdCountText.font = oswald;
        }
        crowdCountText.fontSize = 28f;
        crowdCountText.alignment = TextAlignmentOptions.Left;

        TMP_ColorGradient green = Resources.Load<TMP_ColorGradient>("Color Gradient Presets/Light to Dark Green - Vertical");
        if (green != null)
        {
            crowdCountText.enableVertexGradient = true;
            crowdCountText.colorGradientPreset = green;
        }
    }

    private void UpdateLevelText()
    {
        if (levelText)
        {
            levelText.text = $"LEVEL {LevelManager.CurrentLevel}";
        }
    }

    private void BuildPauseUI()
    {
        if (gameplayUI == null) return;
        Transform canvasParent = gameplayUI.transform.parent;
        if (canvasParent == null) return;

        TMP_FontAsset oswald = Resources.Load<TMP_FontAsset>("Fonts & Materials/Oswald Bold SDF");

        // Purely informational overlay - no button. Pausing/resuming both happen from a plain tap
        // anywhere on screen, handled centrally in HandleTapToPauseInput().
        GameObject panelGo = new GameObject("PausePanel", typeof(RectTransform));
        panelGo.transform.SetParent(canvasParent, false);
        RectTransform panelRT = panelGo.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        UnityEngine.UI.Image backdrop = panelGo.AddComponent<UnityEngine.UI.Image>();
        backdrop.color = new Color(0.02f, 0.02f, 0.06f, 0.78f);
        backdrop.raycastTarget = false; // Never intercept the tap - GameManager reads raw Pointer input directly

        RectTransform titleRT = CreateCenteredUIRect(panelGo.transform, "PausedTitle", new Vector2(500f, 80f), new Vector2(0f, 40f));
        TextMeshProUGUI titleText = titleRT.gameObject.AddComponent<TextMeshProUGUI>();
        titleText.text = "PAUSED";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 56f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;
        titleText.raycastTarget = false;
        if (oswald != null) titleText.font = oswald;

        RectTransform subtitleRT = CreateCenteredUIRect(panelGo.transform, "PausedSubtitle", new Vector2(500f, 60f), new Vector2(0f, -30f));
        TextMeshProUGUI subtitleText = subtitleRT.gameObject.AddComponent<TextMeshProUGUI>();
        subtitleText.text = "Tap anywhere to resume";
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.fontSize = 30f;
        subtitleText.color = new Color(1f, 1f, 1f, 0.8f);
        subtitleText.raycastTarget = false;
        if (oswald != null) subtitleText.font = oswald;

        pausePanel = panelGo;
        pausePanel.SetActive(false);
    }

    private void HandleTapToPauseInput()
    {
        if (CurrentState != GameState.Playing && CurrentState != GameState.Paused) return;
        if (Pointer.current == null) return;

        if (Pointer.current.press.wasPressedThisFrame)
        {
            tapStartPos = Pointer.current.position.ReadValue();
            tapStartTime = Time.unscaledTime;
            isTapTracking = true;
        }
        else if (Pointer.current.press.wasReleasedThisFrame && isTapTracking)
        {
            isTapTracking = false;

            // Only counts as a tap-to-pause if it was quick and didn't drag far - a real lane-change
            // drag or jump/duck swipe moves well past this distance, so there's no ambiguity between
            // the two gestures.
            float heldDuration = Time.unscaledTime - tapStartTime;
            float dragDistance = Vector2.Distance(Pointer.current.position.ReadValue(), tapStartPos);
            if (heldDuration <= TapMaxDuration && dragDistance <= TapMaxDragDistance)
            {
                if (CurrentState == GameState.Playing)
                {
                    PauseGame();
                }
                else if (CurrentState == GameState.Paused)
                {
                    ResumeGame();
                }
            }
        }
    }

    private void WireUpButtons()
    {
        if (startPanel)
        {
            UnityEngine.UI.Button btn = startPanel.GetComponentInChildren<UnityEngine.UI.Button>();
            if (btn) btn.onClick.AddListener(StartGame);
        }
        if (victoryPanel)
        {
            UnityEngine.UI.Button btn = victoryPanel.GetComponentInChildren<UnityEngine.UI.Button>();
            if (btn) btn.onClick.AddListener(AdvanceLevel);
        }
        if (gameOverPanel)
        {
            UnityEngine.UI.Button btn = gameOverPanel.GetComponentInChildren<UnityEngine.UI.Button>();
            if (btn) btn.onClick.AddListener(RestartGame);
        }
    }

    public void AdvanceLevel()
    {
        int nextLevel = LevelManager.CurrentLevel + 1;
        PlayerPrefs.SetInt("CurrentLevel", nextLevel);
        PlayerPrefs.Save();
        currentLevelIndex = nextLevel; // Sync global variable
        RestartGame();
    }

    public void StartGame()
    {
        CurrentState = GameState.Playing;
        if (startPanel) startPanel.SetActive(false);
        if (victoryPanel) victoryPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (gameplayUI) gameplayUI.SetActive(true);
        Time.timeScale = 1f;
    }

    public void PauseGame()
    {
        // Only pausable mid-run - every gameplay script already freezes whenever CurrentState isn't
        // Playing, so reusing that same mechanism (rather than a separate pause flag) needs no extra
        // guards anywhere else in the codebase.
        if (CurrentState != GameState.Playing) return;
        CurrentState = GameState.Paused;
        Time.timeScale = 0f;
        if (pausePanel) pausePanel.SetActive(true);
        if (AudioManager.Instance != null) AudioManager.Instance.PauseMusic();
    }

    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
        if (pausePanel) pausePanel.SetActive(false);
        if (AudioManager.Instance != null) AudioManager.Instance.ResumeMusic();
    }

    public void VictoryState()
    {
        if (CurrentState == GameState.Victory) return; // Guard against duplicate calls
        CurrentState = GameState.Victory;
        
        // Keep gameplay timescale at 1f so player models can dance/celebrate excitedly!
        Time.timeScale = 1f;

        if (gameplayUI) gameplayUI.SetActive(false);

        // Give the castle's wall-break a beat to actually play out before the victory panel
        // covers the screen, instead of popping up instantly over it.
        StartCoroutine(ShowVictoryPanelDelayed());

        if (victoryScoreText && CrowdManager.Instance != null)
        {
            // Includes leader (+ 1)
            victoryScoreText.text = $"You won with {CrowdManager.Instance.ActiveCrowdCount + 1} Stickmen!";
        }
        if (BossHealthBar.Instance != null)
        {
            BossHealthBar.Instance.HideBar();
        }

        // Advancing now waits for the player to press "NEXT LEVEL" on the victory window (wired in WireUpButtons).
    }

    private System.Collections.IEnumerator ShowVictoryPanelDelayed()
    {
        // Wait for the castle's door-slide animation to fully finish before darkening the screen with
        // the victory backdrop. This used to be a hardcoded 0.5s beat, shorter than EnemyCastle's own
        // 0.7s door-slide duration - so the backdrop started covering the screen before the door had
        // finished opening, cutting the animation off early/making it look like it never opened.
        float delay = 0.5f;
        if (EnemyCastle.Instance != null)
        {
            delay = EnemyCastle.Instance.doorSlideDuration + 0.15f;
        }
        yield return new WaitForSeconds(delay);

        if (victoryPanel)
        {
            victoryPanel.SetActive(true);
            Transform titleTrans = victoryPanel.transform.Find("Title");
            if (titleTrans != null)
            {
                // Prominent overlay text says "LEVEL CLEAR!"
                TextMeshProUGUI titleText = titleTrans.GetComponent<TextMeshProUGUI>();
                if (titleText != null)
                {
                    titleText.text = "LEVEL CLEAR!";
                }
                StartCoroutine(AnimateVictoryTextBounce(titleTrans));
            }
        }
    }

    private System.Collections.IEnumerator AnimateVictoryTextBounce(Transform textTransform)
    {
        textTransform.localScale = Vector3.zero;

        // Simple robust elastic-bounce out formula for unscaled scale bounce
        yield return ScaleTween.Run(textTransform, Vector3.one, t =>
        {
            if (t < 0.45f) return Mathf.Lerp(0f, 1.35f, t / 0.45f); // Climb quickly up to 1.35x
            if (t < 0.75f) return Mathf.Lerp(1.35f, 1.15f, (t - 0.45f) / 0.3f); // Descend to 1.15x
            return Mathf.Lerp(1.15f, 1.2f, (t - 0.75f) / 0.25f); // Settle on 1.2x
        }, 0.5f, true);
    }

    public void GameOver()
    {
        if (CurrentState == GameState.GameOver) return; // Guard against duplicate calls
        CurrentState = GameState.GameOver;
        Time.timeScale = 0f;
        if (gameplayUI) gameplayUI.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (BossHealthBar.Instance != null)
        {
            BossHealthBar.Instance.HideBar();
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        // Perform thorough manual cleanup of all dynamic instances before scene load
        // to prevent memory leaks, residual physics triggers, or frame-drops on transition.
        CleanupDynamicRuntimeInstances();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void CleanupDynamicRuntimeInstances()
    {
        // 1. Destroy all spawned stickmen
        var stickmen = Object.FindObjectsByType<StickmanController>(FindObjectsSortMode.None);
        foreach (var sm in stickmen)
        {
            if (sm != null) Destroy(sm.gameObject);
        }

        // 2. Destroy all red enemy stickmen and squads
        var enemySquads = Object.FindObjectsByType<EnemySquad>(FindObjectsSortMode.None);
        foreach (var es in enemySquads)
        {
            if (es != null) Destroy(es.gameObject);
        }
        var enemyStickmen = Object.FindObjectsByType<EnemyStickman>(FindObjectsSortMode.None);
        foreach (var es in enemyStickmen)
        {
            if (es != null) Destroy(es.gameObject);
        }

        // 3. Destroy all spinning and collectible coins
        var coins = Object.FindObjectsByType<CoinCollector>(FindObjectsSortMode.None);
        foreach (var coin in coins)
        {
            if (coin != null) Destroy(coin.gameObject);
        }

        // 4. Destroy all hazards/obstacles
        var obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (var obs in obstacles)
        {
            if (obs != null) Destroy(obs);
        }

        // 5. Destroy all dynamic particle systems and effects left in the scene
        var particleSystems = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        foreach (var ps in particleSystems)
        {
            // Do not destroy the ones attached to persistent managers or camera, only dynamic instances
            if (ps != null && ps.transform.parent == null)
            {
                Destroy(ps.gameObject);
            }
        }

        // 6. Explicitly clear static crowd manager lists to free references immediately
        if (CrowdManager.Instance != null)
        {
            var activeList = CrowdManager.Instance.GetActiveStickmen();
            if (activeList != null)
            {
                activeList.Clear();
            }
        }

        // 7. Force garbage collection to instantly release memory heap before reload
        System.GC.Collect();
    }

    private void ShowStartScreen()
    {
        CurrentState = GameState.Start;
        Time.timeScale = 1f;
        if (startPanel) startPanel.SetActive(true);
        if (victoryPanel) victoryPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (gameplayUI) gameplayUI.SetActive(false);
        // The shop panel was left active in the scene by default, permanently covering the start
        // panel's Play button since nothing ever hid it - there's currently no button that opens it either.
        if (shopPanel) shopPanel.SetActive(false);
    }

    public void UpdateCrowdCount(int count)
    {
        if (crowdCountText)
        {
            // Displays followers count + 1 (the main character leader) as the total army size
            crowdCountText.text = $"Crowd: {count + 1}";
        }

        // Punch-bounce the crowd row whenever the crowd size actually changes (gate pickup/loss),
        // matching the same satisfying feedback the coin row gets on AddCoins().
        if (count != lastDisplayedCrowdCount)
        {
            lastDisplayedCrowdCount = count;
            if (crowdCountText != null)
            {
                if (crowdPunchScaleCoroutine != null) StopCoroutine(crowdPunchScaleCoroutine);
                crowdPunchScaleCoroutine = StartCoroutine(PunchScale(crowdCountText.rectTransform));
            }
        }
    }
}
