using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GateTrigger : MonoBehaviour
{
    [Header("Gate Configuration")]
    public string operationText = "x2";
    public TextMeshPro textMesh;

    [Header("Dynamic Movement")]
    public bool isMoving = false;
    public float moveSpeed = 2f;
    public float moveRange = 1.5f;
    private float startX;

    [Header("Dynamic Alternating")]
    public bool isAlternating = false;
    public float alternateInterval = 1.5f;
    public string firstOp = "+10";
    public string secondOp = "-5";
    private float lastAlternateTime;

    private bool isAdditionTriggered = false;
    private bool isGateCoinsApplied = false;
    private bool isMultiplyStickmenApplied = false;
    private HashSet<int> processedStickmen = new HashSet<int>();
    private Coroutine textPulseCoroutine;

    private void Start()
    {
        UpdateGateVisuals();
        startX = transform.localPosition.x;
        lastAlternateTime = Time.time;
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        if (isMoving)
        {
            float newX = startX + Mathf.Sin(Time.time * moveSpeed) * moveRange;
            transform.localPosition = new Vector3(newX, transform.localPosition.y, transform.localPosition.z);
        }

        if (isAlternating)
        {
            if (Time.time - lastAlternateTime > alternateInterval)
            {
                lastAlternateTime = Time.time;
                operationText = (operationText == firstOp) ? secondOp : firstOp;
                UpdateGateVisuals();
            }
        }
    }

    private void UpdateGateVisuals()
    {
        if (textMesh != null)
        {
            textMesh.text = operationText;
            
            // Text styling: thick, bold white font with a dark outline for high legibility
            textMesh.fontStyle = FontStyles.Bold;
            textMesh.color = Color.white;
            textMesh.outlineWidth = 0.22f; // clean thick outline
            textMesh.outlineColor = new Color(0.05f, 0.05f, 0.1f); // dark slate/black outline
        }

        // Dynamically color the shield and side pillars
        Transform shield = transform.Find("EnergyShield");
        Transform frameTrans = transform.Find("Frame");

        Color gateColor = Color.grey;
        Color frameColor = new Color(0.4f, 0.4f, 0.45f); // Neutral gray
        float emissionIntensity = 3.5f;

        if (operationText.StartsWith("+") || operationText.ToLower().StartsWith("x"))
        {
            // Positive gate: Glowing, semi-transparent Emerald Green
            gateColor = new Color(0.0f, 1.0f, 0.45f, 0.35f); 
            frameColor = new Color(0.0f, 0.6f, 0.15f); // Deep Metallic Green
        }
        else if (operationText.StartsWith("-"))
        {
            // Negative/Obstacle gate: Glowing, semi-transparent Bright Orange-Red
            gateColor = new Color(1.0f, 0.25f, 0.0f, 0.35f);
            frameColor = new Color(0.7f, 0.05f, 0.05f); // Dark Metallic Red
        }
        else if (operationText == "USED")
        {
            gateColor = new Color(0.2f, 0.2f, 0.2f, 0.1f);
            frameColor = new Color(0.2f, 0.2f, 0.22f);
            emissionIntensity = 0f;
        }

        if (shield != null)
        {
            Renderer r = shield.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = gateColor;
                if (emissionIntensity > 0)
                {
                    r.material.EnableKeyword("_EMISSION");
                    r.material.SetColor("_EmissionColor", new Color(gateColor.r, gateColor.g, gateColor.b) * emissionIntensity);
                }
                else
                {
                    r.material.DisableKeyword("_EMISSION");
                }
            }
        }

        if (frameTrans != null)
        {
            Renderer[] frameRends = frameTrans.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in frameRends)
            {
                if (r == null) continue;
                Material m = r.material;
                m.color = frameColor;
                m.SetFloat("_Metallic", 0.9f);
                m.SetFloat("_Smoothness", 0.85f);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        StickmanController stickman = other.GetComponent<StickmanController>();
        if (stickman != null)
        {
            int instanceId = stickman.gameObject.GetHashCode();
            if (processedStickmen.Contains(instanceId))
            {
                return;
            }
            processedStickmen.Add(instanceId);

            ParseAndApplyOperation(other.transform.position);
            return;
        }

        // The leader must also be able to trigger gates directly - otherwise a crowd of 0 starting
        // followers could never grow, since only an existing follower could pass through a gate.
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            int instanceId = player.gameObject.GetHashCode();
            if (processedStickmen.Contains(instanceId))
            {
                return;
            }
            processedStickmen.Add(instanceId);

            ParseAndApplyOperation(other.transform.position);
        }
    }

    private void ParseAndApplyOperation(Vector3 contactPosition)
    {
        if (string.IsNullOrEmpty(operationText)) return;

        if (VisualEffectManager.Instance != null)
        {
            VisualEffectManager.Instance.TriggerGateCross(transform.position + Vector3.up * 1f);
        }

        // Play juicy pop sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPopSFX();
        }

        // Trigger satisfying text pop pulse animation
        TriggerTextPulse();

        char op = operationText[0];
        string numStr = operationText.Substring(1);
        if (int.TryParse(numStr, out int val))
        {
            if (op == 'x' || op == 'X')
            {
                // Multiplier Gate Coin Reward: grants a flat bonus scaled by the gate's multiplier value.
                // Previously this multiplied the player's entire persistent coin balance, which compounded
                // exponentially across the many "x" gates scattered through a level (each gate multiplying
                // an already-inflated total from the previous one), making the coin counter look like it
                // climbed forever. A flat bonus keeps the reward bounded no matter how many gates are crossed.
                if (!isGateCoinsApplied)
                {
                    isGateCoinsApplied = true;
                    if (GameManager.Instance != null)
                    {
                        int bonus = (val - 1) * 20;
                        if (bonus > 0)
                        {
                            GameManager.Instance.AddCoins(bonus);
                            FloatingText.Spawn(contactPosition, $"+{bonus}", Color.yellow);
                        }
                    }
                }

                // Guarded so this only ever fires once per gate - otherwise each newly-spawned stickman
                // immediately overlaps this same trigger and re-fires it, causing an exponential spawn cascade.
                if (val > 1 && CrowdManager.Instance != null && !isMultiplyStickmenApplied)
                {
                    isMultiplyStickmenApplied = true;
                    CrowdManager.Instance.MultiplyStickmen(contactPosition, val - 1);
                }
            }
            else if (op == '+')
            {
                if (!isAdditionTriggered && CrowdManager.Instance != null)
                {
                    isAdditionTriggered = true;
                    CrowdManager.Instance.MultiplyStickmen(transform.position, val);
                    operationText = "USED";
                    UpdateGateVisuals();
                }
            }
            else if (op == '-')
            {
                // Subtraction Gate Coin Calculation (Deducts the corresponding number immediately)
                if (!isGateCoinsApplied)
                {
                    isGateCoinsApplied = true;
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.AddCoins(-val);
                        FloatingText.Spawn(contactPosition, $"-{val}", Color.red);
                    }
                }

                if (val > 0)
                {
                    val--;
                    operationText = val > 0 ? $"-{val}" : "USED";
                    UpdateGateVisuals();
                }
            }
        }
    }

    private void TriggerTextPulse()
    {
        if (textPulseCoroutine != null) StopCoroutine(textPulseCoroutine);
        textPulseCoroutine = StartCoroutine(AnimateTextPulse());
    }

    private IEnumerator AnimateTextPulse()
    {
        if (textMesh == null) yield break;

        float duration = 0.15f;
        float elapsed = 0f;
        Vector3 baseScale = Vector3.one;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Quick pulse up to 1.4x and return back to 1.0x over 0.15 seconds using Lerp
            float targetScaleFactor = t < 0.5f ? Mathf.Lerp(1.0f, 1.4f, t / 0.5f) : Mathf.Lerp(1.4f, 1.0f, (t - 0.5f) / 0.5f);
            textMesh.transform.localScale = baseScale * targetScaleFactor;
            yield return null;
        }

        textMesh.transform.localScale = baseScale;
        textPulseCoroutine = null;
    }

    private void OnTriggerStay(Collider other)
    {
        if (operationText.StartsWith("-"))
        {
            StickmanController stickman = other.GetComponent<StickmanController>();
            if (stickman != null)
            {
                string numStr = operationText.Substring(1);
                if (int.TryParse(numStr, out int val) && val > 0)
                {
                    // Disable the collider immediately (synchronous) rather than relying only on
                    // Destroy(), which doesn't take effect until end of frame - on a slow frame,
                    // multiple FixedUpdate physics ticks can re-fire OnTriggerStay for this same
                    // stickman before it's actually removed, burning two counts off the gate for
                    // one physical pass-through.
                    other.enabled = false;
                    val--;
                    operationText = val > 0 ? $"-{val}" : "USED";
                    UpdateGateVisuals();
                    Destroy(stickman.gameObject);
                }
            }
        }
    }
}
