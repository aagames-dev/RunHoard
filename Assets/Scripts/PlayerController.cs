using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Movement Settings")]
    public float forwardSpeed = 10f;
    public float horizontalSpeed = 4.5f;
    public float laneWidth = 2.0f;

    [Header("Spawning (Inspiration) Settings")]
    public float spawnRate = 5f;
    private float spawnCooldown;

    [Header("Jump Settings (Physical Rigidbody)")]
    public float jumpForce = 11f;
    public float gravityForce = -22f; // Custom gravity multiplier for snappy feel
    private bool isGrounded = true;
    private float groundY = 0.5f;

    [Header("Duck Settings")]
    public float duckDuration = 0.8f;
    private bool isDucking = false;
    private float duckTimer = 0f;

    [Header("Solo Castle Attack Animation")]
    public float soloAttackAnimDuration = 0.25f;
    private bool isSoloAttacking = false;
    private float soloAttackTimer = 0f;

    [Header("Visual Container Reference")]
    public Transform visualContainer;

    [Header("Juice & Procedural Animations")]
    public float runBobSpeed = 12f;
    public float runBobAmount = 0.08f;
    private Vector3 originalVisualScale = Vector3.one;

    private CapsuleCollider capsuleCollider;
    private Rigidbody rb;
    private float originalColliderHeight = 1.7f;
    private Vector3 originalColliderCenter = new Vector3(0f, 0.85f, 0f);

    private float targetX = 0f;
    public bool IsAtEndBase { get; set; } = false;

    private float trackDistance = 0f;
    private float verticalVelocity = 0f;
    private float currentLocalX = 0f;

    // Mobile swipe tracking
    private Vector2 touchStartPos;
    private bool isSwipeTracking = false;
    private const float MinSwipeDistance = 40f;

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

        capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider != null)
        {
            originalColliderHeight = capsuleCollider.height;
            originalColliderCenter = capsuleCollider.center;
        }

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Configure physical Rigidbody
        rb.useGravity = false; // We will apply custom snappy gravity manually to have tight control
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

        if (visualContainer != null)
        {
            originalVisualScale = visualContainer.localScale;
        }
    }

    private void Start()
    {
        laneWidth = 4.5f;
        if (LevelManager.Instance != null)
        {
            forwardSpeed = LevelManager.Instance.forwardSpeed;
        }
        spawnCooldown = 1f / spawnRate;
        currentLocalX = 0f;
        targetX = 0f;
        groundY = transform.position.y;
        IsAtEndBase = false;

        // Initialize track distance to match world Z = 0 (which is 15m from segment 0's entrance)
        trackDistance = 15f;

        // Apply selected hat configuration
        ApplyEquippedHat();

        // Spawn Starting Army if unlocked
        SpawnStartingArmy();
    }

    public void ApplyEquippedHat()
    {
        if (visualContainer == null) return;
        Transform head = visualContainer.Find("Head");
        if (head == null) return;

        Transform brim = head.Find("Hat_Brim");
        Transform crown = head.Find("Hat_Crown");
        Transform premiumCrown = head.Find("PremiumCrown");

        int equippedHat = PlayerPrefs.GetInt("EquippedHat", 0); // 0 = Explorer, 1 = Premium Crown

        if (equippedHat == 0)
        {
            // Activate default explorer hat
            if (brim != null) brim.gameObject.SetActive(true);
            if (crown != null) crown.gameObject.SetActive(true);
            if (premiumCrown != null) premiumCrown.gameObject.SetActive(false);
        }
        else
        {
            // Deactivate default explorer hat
            if (brim != null) brim.gameObject.SetActive(false);
            if (crown != null) crown.gameObject.SetActive(false);

            // Construct or activate Premium Golden Crown
            if (premiumCrown == null)
            {
                premiumCrown = CreateProceduralCrown(head);
            }
            premiumCrown.gameObject.SetActive(true);
        }
    }

    private Transform CreateProceduralCrown(Transform head)
    {
        GameObject crownGo = new GameObject("PremiumCrown");
        crownGo.transform.SetParent(head, false);
        // Position on top of head
        crownGo.transform.localPosition = new Vector3(0f, 0.42f, 0f);
        crownGo.transform.localRotation = Quaternion.identity;
        crownGo.transform.localScale = Vector3.one;

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Standard");

        // Rich Gold material
        Material goldMat = new Material(litShader);
        goldMat.color = new Color(1f, 0.85f, 0f);
        goldMat.SetFloat("_Metallic", 0.9f);
        goldMat.SetFloat("_Smoothness", 0.85f);
        goldMat.EnableKeyword("_EMISSION");
        goldMat.SetColor("_EmissionColor", new Color(1f, 0.7f, 0f) * 1.5f);

        // Crown Base Ring
        GameObject baseRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseRing.name = "BaseRing";
        baseRing.transform.SetParent(crownGo.transform, false);
        baseRing.transform.localPosition = Vector3.zero;
        baseRing.transform.localScale = new Vector3(1.1f, 0.08f, 1.1f);
        baseRing.GetComponent<Renderer>().sharedMaterial = goldMat;
        RemovePrimitiveCollider(baseRing);

        // Spikes on the crown (6 cones pointing up around the ring)
        int spikeCount = 6;
        float radius = 0.5f;
        for (int i = 0; i < spikeCount; i++)
        {
            float angle = (i * 360f / spikeCount) * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spike.name = "Spike_" + i;
            spike.transform.SetParent(crownGo.transform, false);
            spike.transform.localPosition = new Vector3(x, 0.22f, z);
            spike.transform.localRotation = Quaternion.identity;
            spike.transform.localScale = new Vector3(0.12f, 0.22f, 0.12f);
            spike.GetComponent<Renderer>().sharedMaterial = goldMat;
            RemovePrimitiveCollider(spike);

            // Add a small ruby jewel sphere at the tip of each spike
            GameObject gem = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gem.name = "RubyGem";
            gem.transform.SetParent(spike.transform, false);
            gem.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            gem.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            
            Material rubyMat = new Material(litShader);
            rubyMat.color = Color.red;
            rubyMat.EnableKeyword("_EMISSION");
            rubyMat.SetColor("_EmissionColor", Color.red * 1.5f);
            gem.GetComponent<Renderer>().sharedMaterial = rubyMat;
            RemovePrimitiveCollider(gem);
        }

        return crownGo.transform;
    }

    // GameObject.CreatePrimitive() always attaches a real physics Collider. Destroy() doesn't take
    // effect until end of frame, so without this, these crown pieces (parented under the player's own
    // Head/root hierarchy) would briefly become part of the player's compound Rigidbody collider shape -
    // for one frame, but enough to visibly pop the player up off the track. Disabling immediately closes
    // that window; Destroy() still runs afterward to clean the now-unused component up properly.
    private void RemovePrimitiveCollider(GameObject primitive)
    {
        Collider col = primitive.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
            Destroy(col);
        }
    }

    private void SpawnStartingArmy()
    {
        int startingCount = PlayerPrefs.GetInt("StartingStickmenCount", 0);
        if (startingCount > 0 && CrowdManager.Instance != null)
        {
            for (int i = 0; i < startingCount; i++)
            {
                // Spawn slightly trailing the Hat-Man leader to build the swarm instantly
                Vector3 spawnPos = transform.position - transform.forward * (1.2f + (i * 0.35f)) + new Vector3(Random.Range(-0.4f, 0.4f), 0.1f, Random.Range(-0.2f, 0.2f));
                CrowdManager.Instance.SpawnStickman(spawnPos);
            }
        }
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true; // Lock position perfectly against any physics forces, inertia, or crowd collisions
            return;
        }

        rb.isKinematic = false;

        // 1. Increment forward progress distance along the curving track spline
        // This locks the forward movement calculation directly to the forward directional vector of the curving track centerline
        if (!IsAtEndBase)
        {
            trackDistance += forwardSpeed * Time.fixedDeltaTime;
        }

        // 2. Horizontal slide local position relative to the curved track centerline
        // Uses the persistent member variable currentLocalX directly to eliminate jitter caused by reading back transform position
        currentLocalX = Mathf.MoveTowards(currentLocalX, targetX, horizontalSpeed * Time.fixedDeltaTime);
        
        // Standard lane width clamping
        currentLocalX = Mathf.Clamp(currentLocalX, -laneWidth, laneWidth);

        // 3. Integrate vertical jumping velocity and snappy custom gravity
        // Locks the player's global Y-position using the ground-snapping raycast, keeping them strictly on the track surface
        float targetY = groundY;
        if (!isGrounded)
        {
            verticalVelocity += gravityForce * Time.fixedDeltaTime;
            targetY = transform.position.y + verticalVelocity * Time.fixedDeltaTime;

            // Smooth landing check: if we fall below or touch the surface height
            if (targetY <= groundY)
            {
                targetY = groundY;
                isGrounded = true;
                verticalVelocity = 0f;
                ResetColliderFromJump();
            }
        }
        else
        {
            verticalVelocity = 0f;
        }

        // 4. Snap player position and rotation precisely to the computed 3D track centerline coordinates
        // This ensures the player's object orientation (transform.forward) always matches the angle of the track curve instantly!
        if (TrackGenerator.Instance != null)
        {
            Vector3 targetWorldPos = TrackGenerator.Instance.GetWorldPositionAt(trackDistance, currentLocalX, targetY);
            rb.position = targetWorldPos;
            rb.rotation = TrackGenerator.Instance.GetWorldRotationAt(trackDistance);
        }

        // Zero out linear velocity to prevent any physics-based drift/fight
        rb.linearVelocity = Vector3.zero;
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        // Ground check: raycast down to detect if we're near the track surface
        GroundCheck();

        // Horizontal sliding input updates
        HandleHorizontalInput();

        // Ducking timer countdown & scale interpolation
        HandleDuckingTimer();

        // Solo castle-attack punch animation timer countdown
        HandleSoloAttackTimer();

        // Read swipe and vertical keyboard triggers
        HandleVerticalInput();

        // Running bobbing & cartoon animations
        HandleProceduralAnimations();
    }

    private void GroundCheck()
    {
        float currentSurfaceY = 0f;
        RaycastHit hit;
        bool hitFloor = false;

        // Use a short down-facing Raycast from slightly above player's feet (hips level) to detect track collider floor
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 1.5f))
        {
            // Crowd stickmen use solid (non-trigger) colliders, so when one ends up directly under the
            // leader (bunching/cohesion), this raycast would otherwise mistake the stickman's collider for
            // the track floor and snap groundY up to its height, making the player visibly float.
            if (!hit.collider.isTrigger && hit.collider.transform.root != transform.root && hit.collider.GetComponentInParent<StickmanController>() == null)
            {
                currentSurfaceY = hit.point.y;
                hitFloor = true;
            }
        }

        if (hitFloor)
        {
            // Glues player base grounded position precisely to the track surface
            groundY = currentSurfaceY + 0.5f;
        }

        float yTolerance = 0.15f;
        if (transform.position.y <= groundY + yTolerance && rb.linearVelocity.y <= 0.1f)
        {
            if (!isGrounded)
            {
                isGrounded = true;
                ResetColliderFromJump();
            }
        }
        else
        {
            isGrounded = false;
        }
    }

    private void HandleHorizontalInput()
    {
        float keyboardInput = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                keyboardInput = -1f;
            else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                keyboardInput = 1f;
        }

        if (Mathf.Abs(keyboardInput) > 0.1f)
        {
            targetX += keyboardInput * horizontalSpeed * Time.deltaTime;
        }
        else
        {
            if (Pointer.current != null)
            {
                if (Pointer.current.press.isPressed)
                {
                    float deltaX = Pointer.current.delta.x.ReadValue();
                    targetX += deltaX * 0.015f * horizontalSpeed;
                }
            }
        }

        targetX = Mathf.Clamp(targetX, -laneWidth, laneWidth);
    }

    private void HandleVerticalInput()
    {
        // Keyboard bindings (Up / Down / W / S)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
            {
                TriggerJump();
            }
            if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
            {
                TriggerDuck();
            }
        }

        // Touchscreen swipe gesture detection
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            Vector2 currentTouchPos = Touchscreen.current.primaryTouch.position.ReadValue();
            if (!isSwipeTracking)
            {
                touchStartPos = currentTouchPos;
                isSwipeTracking = true;
            }
            else
            {
                Vector2 delta = currentTouchPos - touchStartPos;
                if (delta.magnitude > MinSwipeDistance)
                {
                    if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
                    {
                        if (delta.y > 0)
                        {
                            TriggerJump();
                        }
                        else
                        {
                            TriggerDuck();
                        }
                    }
                    isSwipeTracking = false;
                }
            }
        }
        else
        {
            isSwipeTracking = false;
        }
    }

    public void TriggerJump()
    {
        if (!isGrounded) return;
        if (isDucking) EndDuck(); // Interrupted ducking

        isGrounded = false;
        verticalVelocity = jumpForce;

        // Temporarily raise collider center to easily clear low Spiked Rollers
        if (capsuleCollider != null)
        {
            capsuleCollider.center = new Vector3(originalColliderCenter.x, originalColliderCenter.y + 0.4f, originalColliderCenter.z);
        }
    }

    private void ResetColliderFromJump()
    {
        if (capsuleCollider != null)
        {
            capsuleCollider.center = originalColliderCenter;
        }
    }

    public void TriggerDuck()
    {
        if (isDucking)
        {
            duckTimer = duckDuration; // Refresh timer
            return;
        }

        isDucking = true;
        duckTimer = duckDuration;

        // Shrink Capsule Collider by half
        if (capsuleCollider != null)
        {
            capsuleCollider.height = originalColliderHeight * 0.5f;
            capsuleCollider.center = new Vector3(originalColliderCenter.x, originalColliderCenter.y * 0.5f, originalColliderCenter.z);
        }
    }

    private void HandleDuckingTimer()
    {
        if (isDucking)
        {
            duckTimer -= Time.deltaTime;
            if (duckTimer <= 0f)
            {
                EndDuck();
            }
        }
    }

    // Called by EnemyCastle each solo-attack tick so a lone leader waiting out a castle's HP visibly
    // does something each hit, instead of standing frozen while an easy-to-miss number ticks down.
    public void TriggerSoloAttackPunch()
    {
        isSoloAttacking = true;
        soloAttackTimer = soloAttackAnimDuration;
    }

    private void HandleSoloAttackTimer()
    {
        if (isSoloAttacking)
        {
            soloAttackTimer -= Time.deltaTime;
            if (soloAttackTimer <= 0f)
            {
                isSoloAttacking = false;
            }
        }
    }

    private void EndDuck()
    {
        isDucking = false;

        // Restore collider scale
        if (capsuleCollider != null)
        {
            capsuleCollider.height = originalColliderHeight;
            capsuleCollider.center = originalColliderCenter;
        }
    }

    private void HandleProceduralAnimations()
    {
        if (visualContainer == null) return;

        // Quick forward punch/lunge each time the lone leader lands a solo hit on the castle -
        // takes priority so it's clearly visible even while frozen at the door (IsAtEndBase).
        if (isSoloAttacking)
        {
            float t = 1f - Mathf.Clamp01(soloAttackTimer / soloAttackAnimDuration);
            float lunge = Mathf.Sin(t * Mathf.PI) * 0.35f;
            visualContainer.localPosition = new Vector3(0f, 0f, lunge);
            visualContainer.localScale = new Vector3(
                originalVisualScale.x * (1.0f + lunge * 0.15f),
                originalVisualScale.y * (1.0f - lunge * 0.1f),
                originalVisualScale.z * (1.0f + lunge * 0.15f)
            );
            return;
        }

        // Bouncy vertical jumping victory pose/dance at level clear
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Victory)
        {
            float victoryBob = Mathf.Abs(Mathf.Sin(Time.time * 16f)) * 0.35f;
            visualContainer.localPosition = new Vector3(0f, victoryBob, 0f);
            visualContainer.localScale = new Vector3(
                originalVisualScale.x * 1.1f, 
                originalVisualScale.y * (1.0f + victoryBob * 0.3f), 
                originalVisualScale.z * 1.1f
            );
            return;
        }

        if (isDucking)
        {
            // Squash and stretch: smoothly scale Y to 0.5f and X/Z to 1.3f
            Vector3 targetDuckScale = new Vector3(originalVisualScale.x * 1.3f, originalVisualScale.y * 0.5f, originalVisualScale.z * 1.3f);
            visualContainer.localScale = Vector3.Lerp(visualContainer.localScale, targetDuckScale, 15f * Time.deltaTime);
            visualContainer.localPosition = Vector3.Lerp(visualContainer.localPosition, new Vector3(0f, -0.3f, 0f), 15f * Time.deltaTime);
        }
        else if (!isGrounded)
        {
            // Stretch when jumping up
            float jumpFactor = rb.linearVelocity.y / jumpForce; // positive when going up, negative down
            float targetYScale = originalVisualScale.y * (1.0f + jumpFactor * 0.15f);
            float targetXZScale = originalVisualScale.x * (1.0f - jumpFactor * 0.08f);
            visualContainer.localScale = Vector3.Lerp(visualContainer.localScale, new Vector3(targetXZScale, targetYScale, targetXZScale), 10f * Time.deltaTime);
            visualContainer.localPosition = Vector3.Lerp(visualContainer.localPosition, Vector3.zero, 10f * Time.deltaTime);
        }
        else
        {
            // Running Bobbing cycle: Slight vertical bobbing and bounciness simulation
            float bob = Mathf.Sin(Time.time * runBobSpeed) * runBobAmount;
            
            // Apply slight vertical offset and scale bobbing
            visualContainer.localPosition = new Vector3(0f, bob, 0f);
            
            Vector3 runScale = new Vector3(
                originalVisualScale.x * (1.0f - bob * 0.1f), 
                originalVisualScale.y * (1.0f + bob * 0.15f), 
                originalVisualScale.z * (1.0f - bob * 0.1f)
            );
            visualContainer.localScale = Vector3.Lerp(visualContainer.localScale, runScale, 20f * Time.deltaTime);
        }
    }

    private void HandleSpawning()
    {
        spawnCooldown -= Time.deltaTime;
        if (spawnCooldown <= 0f)
        {
            spawnCooldown = 1f / spawnRate;
            SpawnFollowerStickman();
        }
    }

    private void SpawnFollowerStickman()
    {
        if (CrowdManager.Instance != null)
        {
            Vector3 spawnPos = transform.position - transform.forward * 1.5f + new Vector3(Random.Range(-0.4f, 0.4f), 0.1f, Random.Range(-0.2f, 0.2f));
            CrowdManager.Instance.SpawnStickman(spawnPos);

            if (VisualEffectManager.Instance != null)
            {
                VisualEffectManager.Instance.TriggerMuzzleFlash(spawnPos);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            if (other.name.Contains("ObstaclePrefab") || other.name.Contains("Cube"))
            {
                // Red Cube Currency Penalty (No Game Over)
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddCoins(-15);
                    FloatingText.Spawn(transform.position, "-15", Color.red);
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayPopSFX();
                    }
                }
                Destroy(other.gameObject);
            }
            else
            {
                // Mechanical hazards (spikes/pendulum/glove) now behave like enemy squad collisions:
                // they cost a crowd follower first and only end the run once the crowd is empty.
                // Previously these were unconditional instant-death for the leader regardless of
                // crowd size, which made the entire crowd-building loop (shop upgrades, + gates)
                // meaningless against the most common obstacle type on the track.
                StickmanController sacrificed = CrowdManager.Instance != null ? CrowdManager.Instance.PeekFrontStickman() : null;

                if (sacrificed != null)
                {
                    if (VisualEffectManager.Instance != null)
                    {
                        VisualEffectManager.Instance.TriggerStickmanDeath(sacrificed.transform.position);
                    }
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayPopSFX();
                    }
                    Destroy(sacrificed.gameObject);
                }
                else if (GameManager.Instance != null)
                {
                    GameManager.Instance.GameOver();
                }
            }
        }
    }
}
