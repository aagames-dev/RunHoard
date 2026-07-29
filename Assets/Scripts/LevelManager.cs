using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public static int CurrentLevel => PlayerPrefs.GetInt("CurrentLevel", 1);

    [Header("Current Runtime Settings")]
    public string currentLevelName;
    public float levelLength = 300f;
    public int castleMaxHP = 100;
    public float forwardSpeed = 10f;
    public Color currentNeonLeft = Color.cyan;
    public Color currentNeonRight = new Color(0.8f, 0f, 1f);

    [Header("Prefabs")]
    public GameObject enemyCastlePrefab;

    private GameObject spawnedCastle;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadLevelConfig();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadLevelConfig()
    {
        int currentLevel = CurrentLevel;

        // 1. Global Level Progression & Dynamic Timing Math
        if (currentLevel == 1)
        {
            // Level 1: Onboarding Win, 300 units length (takes exactly 30 seconds at 10f speed)
            levelLength = 300f;
            forwardSpeed = 10f;
            castleMaxHP = 100;
            currentLevelName = "The Neon Plains";
            currentNeonLeft = Color.cyan;
            currentNeonRight = Color.green;
        }
        else if (currentLevel >= 2 && currentLevel <= 5)
        {
            // Level 2 to 5: Core Loop, formula: 300 + (Level * 25).
            // Duration is roughly 33s (Lvl 2: 350 units / 10.5 speed) to 35s (Lvl 5: 425 units / 12 speed) -
            // length and speed now both scale up, so duration stays fairly flat instead of stretching out.
            levelLength = 300f + (currentLevel * 25f);
            forwardSpeed = 10f + (currentLevel - 1) * 0.5f; // Gentle ramp so pacing itself becomes part of the difficulty curve
            castleMaxHP = 100 + (currentLevel - 1) * 50; // HP scales dynamically
            currentLevelName = "Grid Highway " + currentLevel;
            currentNeonLeft = Color.yellow;
            currentNeonRight = Color.red;
        }
        else
        {
            // Level 6 and Beyond: Engagement Cap, capped at a maximum of 450 units.
            // Speed keeps ramping until it hits its own cap of 16 (around level 13), after which
            // duration settles at a brisk ~28s (450 units / 16 speed) for all further levels.
            levelLength = Mathf.Min(300f + (currentLevel * 25f), 450f);
            forwardSpeed = Mathf.Min(10f + (currentLevel - 1) * 0.5f, 16f); // Same gentle ramp, capped so late levels stay playable
            castleMaxHP = 100 + (currentLevel - 1) * 75; // Boss HP scales dynamically
            currentLevelName = "Challenger Cap " + currentLevel;
            currentNeonLeft = Color.magenta;
            currentNeonRight = Color.cyan;
        }

        // Propagate forward speed to PlayerController if instantiated
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.forwardSpeed = forwardSpeed;
        }
    }

    private void Start()
    {
        if (TrackGenerator.Instance != null)
        {
            TrackGenerator.Instance.EnsureTrackGeneratedTo(levelLength + TrackGenerator.Instance.segmentLength);
        }
        SpawnEndCastle();
    }

    public void SpawnEndCastle()
    {
        if (enemyCastlePrefab != null)
        {
            Vector3 spawnPos = new Vector3(0f, 0f, levelLength);
            Quaternion spawnRot = Quaternion.identity;

            if (TrackGenerator.Instance != null)
            {
                spawnPos = TrackGenerator.Instance.GetWorldPositionAt(levelLength);
                spawnRot = TrackGenerator.Instance.GetWorldRotationAt(levelLength);
            }

            spawnedCastle = Instantiate(enemyCastlePrefab, spawnPos, spawnRot);
            EnemyCastle castle = spawnedCastle.GetComponent<EnemyCastle>();
            if (castle != null)
            {
                castle.maxHP = castleMaxHP;
            }
        }
    }
}


