using UnityEngine;
using TMPro;

public class EnemyCastle : MonoBehaviour
{
    public static EnemyCastle Instance { get; private set; }

    [Header("Castle Settings")]
    public int maxHP = 100;
    public TextMeshPro HPText;

    [Header("World Space UI Components")]
    public GameObject visualRoot;
    public UnityEngine.UI.Slider worldSpaceHPSlider;
    public TextMeshProUGUI worldSpaceHPText;

    [Header("Door Open Effect")]
    public float doorSlideDistance = 3f;
    public float doorSlideDuration = 0.7f;

    private int currentHP;
    private bool isLevelCompleted = false;

    private void Start()
    {
        Instance = this;
        currentHP = maxHP;
        UpdateHPText();
        ApplyVibrantColors();
        ShrinkTriggerNearDoor();

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

    private void ShrinkTriggerNearDoor()
    {
        // The prefab's trigger box is 12 units deep, spanning well before the actual door plane
        // (LeftDoor/RightDoor sit around local Z 4.2). Depending on approach speed, the leader could
        // freeze anywhere in that whole span the instant it first touches the trigger - sometimes far
        // enough from the door that the camera (which follows the leader closely) never framed it well
        // when it opened, making the door-open look inconsistent even though it always actually ran.
        // Re-centering the trigger tightly around the door plane guarantees the leader always freezes
        // right next to it.
        BoxCollider triggerCollider = GetComponent<BoxCollider>();
        if (triggerCollider == null) return;

        Vector3 center = triggerCollider.center;
        Vector3 size = triggerCollider.size;
        center.z = 4f;
        size.z = 3f;
        triggerCollider.center = center;
        triggerCollider.size = size;
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

            if (objName.Contains("Wall") || objName.Contains("Tower") || objName.Contains("Battlement"))
            {
                // Main Walls/Towers: sleek vibrant indigo-violet
                RendererColorUtils.SetEmissiveColor(r, wallColor, 0.5f);
            }
            else if (objName.Contains("Door") || objName.Contains("Drawbridge"))
            {
                // Doors/Drawbridge: bright glowing golden-yellow for excellent contrast
                Color accentColor = new Color(1.0f, 0.64f, 0.0f); // Bright Cyber Gold/Orange
                RendererColorUtils.SetEmissiveColor(r, accentColor, 1.5f);
            }
            else if (objName.Contains("LeftChain") || objName.Contains("LeftTowerCap") || objName.Contains("NeonArchLeft") || objName.Contains("NeonArchTop"))
            {
                // Left neon accents: match left neon theme color
                RendererColorUtils.SetEmissiveColor(r, primaryColor, 4.0f);
            }
            else if (objName.Contains("RightChain") || objName.Contains("RightTowerCap") || objName.Contains("NeonArchRight"))
            {
                // Right neon accents: match right neon theme color
                RendererColorUtils.SetEmissiveColor(r, secondaryColor, 4.0f);
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
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        // Once the castle has fallen, crowd members should just walk on through rather than dealing
        // further (pointless) damage - but the leader-arrival check below must still run even after
        // isLevelCompleted is true, otherwise a leader who reaches the door after the crowd already
        // finished the castle off would never get IsAtEndBase set, and would run straight through the
        // open doors and off the end of the generated track instead of stopping here.
        if (!isLevelCompleted)
        {
            StickmanController stickman = TriggerUtils.GetStickman(other);
            if (stickman != null)
            {
                TriggerUtils.DisableAllColliders(stickman);
                TakeDamage(1);
                Destroy(stickman.gameObject);
            }
        }

        PlayerController player = TriggerUtils.GetLeader(other);
        if (player == null || player.IsAtEndBase) return;

        if (currentHP <= 0)
        {
            // Castle's already been beaten down (by the crowd, in TakeDamage) - possibly even before
            // the leader physically reached the door. Still mark arrival so the leader stops here.
            player.IsAtEndBase = true;
            if (!isLevelCompleted)
            {
                isLevelCompleted = true; // Lock state
                Win();
            }
        }
        else
        {
            // Castle still standing - hold the leader at the door. Setting IsAtEndBase also
            // switches the trailing crowd (StickmanController) to charge forward at full speed
            // instead of trailing the leader, so they keep piling into the castle rather than
            // idling behind a leader who stopped advancing.
            player.IsAtEndBase = true;

            // The leader's own collider is solid, not a trigger - left enabled, a stationary leader
            // standing right in the doorway physically blocks the trailing crowd from ever reaching
            // the castle's damage trigger behind them (worse, cohesion pulls the crowd toward the
            // leader's exact spot). Disable it so the crowd can pass through and keep attacking.
            // Nothing ahead needs the leader's collider anymore - this is the end of the track.
            other.enabled = false;

            // No crowd left (all lost to hazards en route) - rather than an instant, no-warning
            // Game Over the moment the leader touches the door, let the lone leader finish the
            // castle off themselves immediately (see EnsureSoloLeaderAttack).
            if (CrowdManager.Instance == null || CrowdManager.Instance.ActiveCrowdCount == 0)
            {
                EnsureSoloLeaderAttack();
            }
        }
    }

    public void EnsureSoloLeaderAttack()
    {
        if (isLevelCompleted) return;

        // No crowd left to wear the castle down over time - rather than a forced multi-second grind
        // (or an automatic Game Over), the leader finishes the castle off themselves immediately.
        // Reaching the door with nobody left should resolve right away, not stall the run.
        int finishingBlow = currentHP;

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.TriggerSoloAttackPunch();
        }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPopSFX();
        }
        if (finishingBlow > 0)
        {
            FloatingText.Spawn(transform.position + Vector3.up * 2f, $"-{finishingBlow}", Color.cyan);
        }

        TakeDamage(finishingBlow);
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

            // Slide the doors open like supermarket doors instead of the castle just vanishing -
            // the crowd survives the whole fight and walks through the opening once it's done.
            OpenCastleDoors();
            HideHPDisplays();

            Win();
        }
        else if (!isLevelCompleted)
        {
            UpdateHPText();
        }
    }

    private void OpenCastleDoors()
    {
        if (visualRoot == null) return;

        Transform leftDoor = FindDeepChild(visualRoot.transform, "LeftDoor");
        Transform rightDoor = FindDeepChild(visualRoot.transform, "RightDoor");

        // Slide each door outward along the castle's own local left/right axis, like automatic doors
        if (leftDoor != null)
        {
            StartCoroutine(SlideDoor(leftDoor, Vector3.left * doorSlideDistance));
        }
        if (rightDoor != null)
        {
            StartCoroutine(SlideDoor(rightDoor, Vector3.right * doorSlideDistance));
        }
    }

    private Transform FindDeepChild(Transform root, string name)
    {
        if (root.name.Contains(name)) return root;
        foreach (Transform child in root)
        {
            Transform found = FindDeepChild(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private System.Collections.IEnumerator SlideDoor(Transform door, Vector3 localOffset)
    {
        Vector3 startPos = door.localPosition;
        Vector3 endPos = startPos + localOffset;
        float elapsed = 0f;

        while (elapsed < doorSlideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / doorSlideDuration);
            door.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        door.localPosition = endPos;
    }

    private void HideHPDisplays()
    {
        // Hide just the HP readouts, not the castle itself - the crowd survives to walk through
        // the now-open doorway, so the castle should stay standing rather than vanish.
        if (HPText != null) HPText.gameObject.SetActive(false);
        if (worldSpaceHPText != null) worldSpaceHPText.gameObject.SetActive(false);
        if (worldSpaceHPSlider != null) worldSpaceHPSlider.gameObject.SetActive(false);
    }

    private void Win()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.VictoryState();
        }
    }
}
