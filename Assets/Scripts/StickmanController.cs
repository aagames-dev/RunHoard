using UnityEngine;

public class StickmanController : MonoBehaviour
{
    private Rigidbody rb;
    private Transform visualChild;
    private Transform engineLeft;
    private Transform engineRight;

    [Header("Hover Bobbing Animation")]
    public float hoverSpeed = 6f;
    public float hoverAmount = 0.15f;
    private float hoverOffset;

    [Header("Banking Rotation")]
    public float maxBankingAngle = 20f;

    private TrailRenderer trail;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.useGravity = false; // Hover drones don't need gravity dragging them down rigidly
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

        // Cache visual components for animations
        visualChild = transform.Find("Body");
        if (visualChild != null)
        {
            engineLeft = visualChild.Find("HoverJet_L");
            engineRight = visualChild.Find("HoverJet_R");
        }

        // Randomize hover phase so drones don't bob in perfect unison
        hoverOffset = Random.Range(0f, 100f);

        SetupDynamicTrail();
    }

    private void SetupDynamicTrail()
    {
        // Add a gorgeous glowing TrailRenderer dynamically at runtime
        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.4f;
        trail.startWidth = 0.25f;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.1f;

        // Get URP particle/unlit material or create a high-quality neon trail material
        Shader trailShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (trailShader == null) trailShader = Shader.Find("Particles/Standard Unlit");
        
        Material trailMat = new Material(trailShader);
        
        Color themeColor = Color.cyan;
        if (LevelManager.Instance != null)
        {
            themeColor = LevelManager.Instance.currentNeonLeft;
        }

        trailMat.color = themeColor;
        trailMat.EnableKeyword("_EMISSION");
        trailMat.SetColor("_EmissionColor", themeColor * 4f);
        trail.sharedMaterial = trailMat;

        // Define a beautiful fading gradient
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(themeColor, 0f), new GradientColorKey(themeColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        trail.colorGradient = gradient;
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 velocity = rb.linearVelocity;
        Vector3 forwardDir = Vector3.forward;
        Vector3 rightDir = Vector3.right;

        if (TrackGenerator.Instance != null)
        {
            forwardDir = TrackGenerator.Instance.GetTrackDirection(transform.position.z);
            rightDir = TrackGenerator.Instance.GetTrackRight(transform.position.z);
        }

        // 1. Calculate trailing forward speed along track curve based on Leader position
        float leaderSpeed = PlayerController.Instance != null ? PlayerController.Instance.forwardSpeed : 10f;
        float forwardSpeed = leaderSpeed;

        if (PlayerController.Instance != null)
        {
            if (!PlayerController.Instance.IsAtEndBase)
            {
                float targetZ = PlayerController.Instance.transform.position.z - 1.5f;
                float zDiff = targetZ - transform.position.z;
                forwardSpeed = leaderSpeed + zDiff * 4.5f;
            }
            else
            {
                forwardSpeed = CrowdManager.Instance != null ? CrowdManager.Instance.stickmanSpeed : 12f;
            }
        }

        // 2. Calculate horizontal flocking speed projected along track right vector
        float targetLocalX = 0f;
        if (PlayerController.Instance != null && TrackGenerator.Instance != null)
        {
            targetLocalX = TrackGenerator.Instance.GetLocalX(PlayerController.Instance.transform.position);
        }

        float stickmanLocalX = 0f;
        if (TrackGenerator.Instance != null)
        {
            stickmanLocalX = TrackGenerator.Instance.GetLocalX(transform.position);
        }

        float xDiff = targetLocalX - stickmanLocalX;
        float cohesionForce = xDiff * 4.0f;

        float separationForceX = 0f;
        if (CrowdManager.Instance != null)
        {
            var stickmen = CrowdManager.Instance.GetActiveStickmen();
            float sepDist = CrowdManager.Instance.separationDistance;
            int count = 0;
            foreach (var other in stickmen)
            {
                if (other == this || other == null) continue;
                float dist = Vector3.Distance(transform.position, other.transform.position);
                if (dist < sepDist && dist > 0.01f)
                {
                    float force = (sepDist - dist) / sepDist;
                    
                    // project neighbor direction onto the track's right vector
                    Vector3 toOther = other.transform.position - transform.position;
                    float localXOffset = Vector3.Dot(toOther, rightDir);
                    separationForceX += -localXOffset * force * 10f;
                    count++;
                }
            }
            if (count > 0)
            {
                separationForceX /= count;
            }
        }

        float rightSpeed = cohesionForce + separationForceX;

        // Keep local position clamped within the lane borders
        float clampX = 4.5f; // laneWidth of player is 4.5f
        if (stickmanLocalX < -clampX && rightSpeed < 0f) rightSpeed = 0f;
        if (stickmanLocalX > clampX && rightSpeed > 0f) rightSpeed = 0f;

        // Assemble the full velocity vector in world space
        velocity = forwardDir * forwardSpeed + rightDir * rightSpeed;

        // Maintain float height
        float targetY = 0.5f;
        float yDiff = targetY - transform.position.y;
        velocity.y = yDiff * 5f;

        rb.linearVelocity = velocity;

        // Banking rotation based on local right speed and track forward direction
        float bankingTarget = -rightSpeed * 2.5f;
        bankingTarget = Mathf.Clamp(bankingTarget, -maxBankingAngle, maxBankingAngle);
        transform.rotation = Quaternion.LookRotation(forwardDir) * Quaternion.Euler(0f, 0f, bankingTarget);
    }

    private void Update()
    {
        bool isVictory = GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Victory;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing && !isVictory)
            return;

        // 2. Animate local hover bobbing of the drone body (bouncier during victory!)
        if (visualChild != null)
        {
            float speedMult = isVictory ? 2.5f : 1f;
            float amountMult = isVictory ? 2.0f : 1f;
            float newY = Mathf.Sin(Time.time * hoverSpeed * speedMult + hoverOffset) * hoverAmount * amountMult;
            Vector3 pos = visualChild.localPosition;
            pos.y = newY;
            visualChild.localPosition = pos;
        }

        // 3. Spin drone hover engine rotors (twice as fast during victory!)
        float spinSpeed = (isVictory ? 1440f : 720f) * Time.deltaTime;
        if (engineLeft != null) engineLeft.Rotate(Vector3.up, spinSpeed, Space.Self);
        if (engineRight != null) engineRight.Rotate(Vector3.up, -spinSpeed, Space.Self);
    }

    private void OnDestroy()
    {
        if (CrowdManager.Instance != null)
        {
            CrowdManager.Instance.UnregisterStickman(this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            if (VisualEffectManager.Instance != null)
            {
                VisualEffectManager.Instance.TriggerStickmanDeath(transform.position);
            }
            Destroy(gameObject);
        }
    }
}
