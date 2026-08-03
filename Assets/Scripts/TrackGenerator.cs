using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class TrackGenerator : MonoBehaviour
{
    public static TrackGenerator Instance { get; private set; }

    [Header("Track Prefabs & Settings")]
    public GameObject trackSegmentPrefab;
    public float segmentLength = 30f;
    public int initialSegmentsCount = 6;
    public float drawAheadDistance = 150f;

    [Header("Obstacles & Gates")]
    public GameObject gatePrefab;
    public GameObject obstaclePrefab;

    [Header("Coin Prefab")]
    public GameObject coinPrefab;

    [Header("Hazard Prefabs (Stylized Obstacles)")]
    public GameObject[] hazardPrefabs;

    [Header("Red Enemy Squad Prefab")]
    public GameObject enemySquadPrefab;

    [Header("Materials (Assigned programmatically if needed)")]
    public Material obstacleMaterial;
    public Material gateMaterial;

    public struct SegmentInfo
    {
        public Vector3 position;
        public Quaternion rotation;
        public float startZ;
    }

    private List<GameObject> activeSegments = new List<GameObject>();
    private List<SegmentInfo> segmentHistory = new List<SegmentInfo>();
    private float nextSpawnZ = 0f;
    private float lastHazardZ = -999f;
    private float lastSegmentAngle = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Clean up any pre-existing track segments or clones under the manager transform at runtime
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.Contains("TrackSegmentPrefab") || child.name.Contains("SplitTrack") || child.name.Contains("Clone"))
            {
                Destroy(child.gameObject);
            }
        }

        segmentHistory.Clear();
        lastSegmentAngle = 0f;
        nextSpawnZ = 0f;
        lastHazardZ = -999f;
    }

    private void Start()
    {
        for (int i = 0; i < initialSegmentsCount; i++)
        {
            SpawnSegment(i == 0);
        }
    }

    public void EnsureTrackGeneratedTo(float targetZ)
    {
        while (nextSpawnZ < targetZ)
        {
            SpawnSegment(nextSpawnZ == 0f);
        }
    }

    public float GetDivergenceOffset(float z)
    {
        return 0f;
    }

    private void Update()
    {
        if (PlayerController.Instance == null) return;

        float playerZ = PlayerController.Instance.transform.position.z;
        if (playerZ + drawAheadDistance > nextSpawnZ)
        {
            float maxZ = LevelManager.Instance != null ? LevelManager.Instance.levelLength + segmentLength : 200f;
            if (nextSpawnZ < maxZ)
            {
                SpawnSegment(false);
            }
        }

        if (activeSegments.Count > 0)
        {
            GameObject oldest = activeSegments[0];
            if (oldest != null && playerZ - oldest.transform.position.z > segmentLength * 1.5f)
            {
                activeSegments.RemoveAt(0);
                Destroy(oldest);
            }
        }
    }

    private void SpawnSegment(bool isFirst)
    {
        if (trackSegmentPrefab == null) return;

        // Calculate rotation using smooth continuous angle change (max 2 degrees step default)
        float targetAngle = 0f;
        int currentLevel = LevelManager.CurrentLevel;
        float amplitude = 0.15f;
        float frequency = 0.015f;
        float maxStep = 2f;

        if (currentLevel >= 2)
        {
            amplitude = 0.35f; // Sharper track curvature!
            frequency = 0.035f; // Alternate faster between left and right curves!
            maxStep = 4f; // Allow segment angles to bend sharper
        }

        if (nextSpawnZ >= 40f)
        {
            // Analytical target angle from sine curve derivative
            float dx = amplitude * Mathf.Cos((nextSpawnZ - 40f) * frequency);
            targetAngle = Mathf.Atan2(dx, 1f) * Mathf.Rad2Deg;
        }

        float deltaAngle = Mathf.DeltaAngle(lastSegmentAngle, targetAngle);
        deltaAngle = Mathf.Clamp(deltaAngle, -maxStep, maxStep); // Limit step dynamically
        float segmentAngle = lastSegmentAngle + deltaAngle;
        lastSegmentAngle = segmentAngle;

        Quaternion rot = Quaternion.Euler(0f, segmentAngle, 0f);

        // Calculate pivot-to-tip seamless position alignment
        Vector3 pos;
        if (isFirst || segmentHistory.Count == 0)
        {
            pos = Vector3.zero;
        }
        else
        {
            // Previous segment exit point is exactly its center position + its rotation * (0, 0, segmentLength / 2)
            SegmentInfo prev = segmentHistory[segmentHistory.Count - 1];
            Vector3 prevExit = prev.position + prev.rotation * new Vector3(0f, 0f, segmentLength * 0.5f);
            
            // Current segment center position must be entrance position + its rotation * (0, 0, segmentLength / 2)
            // Entrance position must snap exactly to previous segment's exit!
            pos = prevExit + rot * new Vector3(0f, 0f, segmentLength * 0.5f);
        }

        GameObject segment = Instantiate(trackSegmentPrefab, pos, rot, transform);
        SpawnLaneDividers(segment);
        activeSegments.Add(segment);

        // Record segment info in history
        SegmentInfo info = new SegmentInfo();
        info.position = pos;
        info.rotation = rot;
        info.startZ = nextSpawnZ;
        segmentHistory.Add(info);

        ApplyLevelColors(segment);

        if (!isFirst && LevelManager.Instance != null && nextSpawnZ < LevelManager.Instance.levelLength - segmentLength)
        {
            SpawnObjectsOnSegment(nextSpawnZ, segment.transform);
        }

        nextSpawnZ += segmentLength;
    }

    public Vector3 GetTrackPosition(float z)
    {
        if (segmentHistory.Count == 0)
        {
            return new Vector3(0f, 0f, z);
        }

        // Find the segment info for the given z
        int index = Mathf.FloorToInt(z / segmentLength);
        index = Mathf.Clamp(index, 0, segmentHistory.Count - 1);
        SegmentInfo info = segmentHistory[index];

        // Interpolate along the segment length
        float localZ = z - info.startZ;
        // Segment center is info.position, so entrance is info.position - info.rotation * (0, 0, 15)
        Vector3 entrance = info.position - info.rotation * new Vector3(0f, 0f, segmentLength * 0.5f);
        Vector3 dir = info.rotation * Vector3.forward;
        return entrance + dir * localZ;
    }

    public Vector3 GetTrackDirection(float z)
    {
        if (segmentHistory.Count == 0) return Vector3.forward;
        int index = Mathf.FloorToInt(z / segmentLength);
        index = Mathf.Clamp(index, 0, segmentHistory.Count - 1);
        return segmentHistory[index].rotation * Vector3.forward;
    }

    public Vector3 GetTrackRight(float z)
    {
        if (segmentHistory.Count == 0) return Vector3.right;
        int index = Mathf.FloorToInt(z / segmentLength);
        index = Mathf.Clamp(index, 0, segmentHistory.Count - 1);
        return segmentHistory[index].rotation * Vector3.right;
    }

    public float GetLocalX(Vector3 worldPos)
    {
        Vector3 center = GetTrackPosition(worldPos.z);
        Vector3 offset = worldPos - center;
        Vector3 right = GetTrackRight(worldPos.z);
        return Vector3.Dot(offset, right);
    }

    public Vector3 GetWorldPositionAt(float zGlobal, float xLocal = 0f, float yLocal = 0f)
    {
        Vector3 center = GetTrackPosition(zGlobal);
        Quaternion rot = GetWorldRotationAt(zGlobal);
        return center + rot * new Vector3(xLocal, yLocal, 0f);
    }

    public Quaternion GetWorldRotationAt(float zGlobal, float extraYRotation = 0f)
    {
        if (segmentHistory.Count == 0) return Quaternion.Euler(0f, extraYRotation, 0f);
        int index = Mathf.FloorToInt(zGlobal / segmentLength);
        index = Mathf.Clamp(index, 0, segmentHistory.Count - 1);
        return segmentHistory[index].rotation * Quaternion.Euler(0f, extraYRotation, 0f);
    }

    private void SpawnLaneDividers(GameObject segment)
    {
        if (gateMaterial == null) return;

        // Position dashes at x = -1.25 and x = 1.25 relative to track center
        float[] xDividers = { -1.25f, 1.25f };
        float[] zOffsets = { -12f, -6f, 0f, 6f, 12f };

        foreach (float x in xDividers)
        {
            foreach (float z in zOffsets)
            {
                GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dash.name = "LaneDivider_Dash";
                dash.transform.SetParent(segment.transform);
                dash.transform.localPosition = new Vector3(x, 0.02f, z);
                dash.transform.localScale = new Vector3(0.1f, 0.01f, 2f);
                
                // Remove collider to optimize physics
                if (dash.GetComponent<BoxCollider>())
                {
                    Destroy(dash.GetComponent<BoxCollider>());
                }

                Renderer r = dash.GetComponent<Renderer>();
                if (r != null)
                {
                    r.sharedMaterial = gateMaterial; // Use glowing gate material for lane dividers
                }
            }
        }
    }

    private void ApplyLevelColors(GameObject segment)
    {
        if (LevelManager.Instance == null) return;

        Color leftColor = LevelManager.Instance.currentNeonLeft;
        Color rightColor = LevelManager.Instance.currentNeonRight;

        Renderer[] renderers = segment.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            if (r.sharedMaterial == null) continue;

            string matName = r.sharedMaterial.name;
            if (matName.Contains("NeonCyanMat") || r.gameObject.name.Contains("LeftNeonLine") || (r.transform.parent != null && r.transform.parent.name.Contains("LeftPillar")))
            {
                RendererColorUtils.SetEmissiveColor(r, leftColor, 3.5f);
            }
            else if (matName.Contains("NeonPurpleMat") || r.gameObject.name.Contains("RightNeonLine") || (r.transform.parent != null && r.transform.parent.name.Contains("RightPillar")))
            {
                RendererColorUtils.SetEmissiveColor(r, rightColor, 3.5f);
            }
        }
    }

    private void SpawnObjectsOnSegment(float startZ, Transform segmentParent)
    {
        int currentLevel = LevelManager.CurrentLevel;

        // --- STANDARD SINGLE-TRACK GENERATION ---
        // 1. Spawning Gates (Always spawn gates on each segment)
        float gateZ = startZ + segmentLength * 0.5f;
        SpawnGateGroup(gateZ, currentLevel, segmentParent);

        // 2. Dynamic Obstacle spawning chance based on level with strict spacing rule
        float obstacleChance = 0.35f + (currentLevel - 1) * 0.20f; // Level 1: 35%, Level 2: 55%, Level 3: 75%, Level 4: 95%, Level 5+: 100%
        obstacleChance = Mathf.Clamp01(obstacleChance);

        // We can try to spawn two obstacles per segment to make the gameplay feel much fuller!
        // First potential obstacle position at 20% of segment length
        float firstObstacleZ = startZ + segmentLength * 0.2f;
        if (firstObstacleZ - lastHazardZ >= 15f && Random.value <= obstacleChance)
        {
            SpawnObstacle(firstObstacleZ, currentLevel, segmentParent);
            lastHazardZ = firstObstacleZ;
        }

        // Second potential obstacle position at 80% of segment length
        float secondObstacleZ = startZ + segmentLength * 0.8f;
        if (secondObstacleZ - lastHazardZ >= 15f && Random.value <= obstacleChance)
        {
            SpawnObstacle(secondObstacleZ, currentLevel, segmentParent);
            lastHazardZ = secondObstacleZ;
        }
    }

    private void SpawnGateGroup(float zPosition, int level, Transform parent)
    {
        if (gatePrefab == null) return;

        // Choose from 5 different dynamic gate and transition mechanics
        int gateType = level == 1 ? Random.Range(0, 3) : Random.Range(0, 5);

        if (gateType == 0)
        {
            // Standard Split-Choice Gates
            SpawnStandardSplitGates(zPosition, level, parent);
        }
        else if (gateType == 1)
        {
            // Horizontal Sliding Gates
            SpawnSlidingGates(zPosition, level, parent);
        }
        else if (gateType == 2)
        {
            // Alternating Flashing Gates
            SpawnAlternatingGates(zPosition, level, parent);
        }
        else if (gateType == 3)
        {
            // Single Center High-Reward Gate with Obstacles
            SpawnSingleCenterGate(zPosition, level, parent);
        }
        else if (gateType == 4)
        {
            // Diagonal Staggered Gates
            SpawnDiagonalGates(zPosition, level, parent);
        }
    }

    // Shared shape behind the split/sliding/alternating gate layouts: spawn one gate per lane position,
    // let the caller apply any per-gate customization (movement, alternation, etc.), then drop a coin
    // cluster behind any positive-reward gate.
    private void SpawnGatePair(float zPosition, float[] xPositions, string[] operations, Transform parent, System.Action<GameObject, int> customize = null)
    {
        for (int i = 0; i < xPositions.Length; i++)
        {
            GameObject gateGo = SpawnSingleGate(zPosition, xPositions[i], operations[i], parent);
            customize?.Invoke(gateGo, i);
            if (operations[i].Contains("+"))
            {
                SpawnCoinCluster(zPosition + 2.5f, xPositions[i], parent);
            }
        }
    }

    private void SpawnStandardSplitGates(float zPosition, int level, Transform parent)
    {
        SpawnGatePair(zPosition, GetRandomLanes(level), GetRandomOperations(level), parent);
    }

    private void SpawnSlidingGates(float zPosition, int level, Transform parent)
    {
        float[] xPositions = { -2f, 2f };
        SpawnGatePair(zPosition, xPositions, GetRandomOperations(level), parent, (gateGo, i) =>
        {
            GateTrigger gt = gateGo.GetComponent<GateTrigger>();
            if (gt != null)
            {
                gt.isMoving = true;
                gt.moveSpeed = Random.Range(1.5f, 3.0f);
                gt.moveRange = Random.Range(1.0f, 1.8f);
            }
        });
    }

    private void SpawnAlternatingGates(float zPosition, int level, Transform parent)
    {
        string[] ops = GetRandomOperations(level);
        SpawnGatePair(zPosition, GetRandomLanes(level), ops, parent, (gateGo, i) =>
        {
            GateTrigger gt = gateGo.GetComponent<GateTrigger>();
            if (gt != null && i == 0) // Make at least one gate dynamic/alternating
            {
                gt.isAlternating = true;
                gt.firstOp = ops[0];
                gt.secondOp = (ops[0].StartsWith("-")) ? "+10" : "-5";
                gt.alternateInterval = Random.Range(1.2f, 2.0f);
                gt.operationText = gt.firstOp;
            }
        });
    }

    private void SpawnSingleCenterGate(float zPosition, int level, Transform parent)
    {
        // High-reward gate in the center
        string reward = level == 1 ? "+15" : (Random.value < 0.5f ? "+30" : "+20");
        GameObject gateGo = SpawnSingleGate(zPosition, 0f, reward, parent);
        SpawnCoinCluster(zPosition + 2.5f, 0f, parent);

        // Guarded by left and right static obstacles
        if (obstaclePrefab != null)
        {
            Vector3 leftObstaclePos = GetWorldPositionAt(zPosition, -2.2f, 0.75f); // Grounded (Y=0.75f for 1.5 scale cube)
            Vector3 rightObstaclePos = GetWorldPositionAt(zPosition, 2.2f, 0.75f); // Grounded (Y=0.75f for 1.5 scale cube)
            Quaternion rot = GetWorldRotationAt(zPosition);
            Instantiate(obstaclePrefab, leftObstaclePos, rot, parent);
            Instantiate(obstaclePrefab, rightObstaclePos, rot, parent);
        }
    }

    private void SpawnDiagonalGates(float zPosition, int level, Transform parent)
    {
        // One gate on left further back, one gate on right further forward
        string[] operations = GetRandomOperations(level);
        
        GameObject gateLeft = SpawnSingleGate(zPosition - 4f, -2f, operations[0], parent);
        GameObject gateRight = SpawnSingleGate(zPosition + 4f, 2f, operations[1], parent);

        if (operations[0].Contains("+"))
            SpawnCoinCluster(zPosition - 1.5f, -2f, parent);
        if (operations[1].Contains("+"))
            SpawnCoinCluster(zPosition + 6.5f, 2f, parent);
    }

    private GameObject SpawnSingleGate(float zPos, float xPos, string opText, Transform parent)
    {
        Vector3 worldPos = GetWorldPositionAt(zPos, xPos, 0f);
        Quaternion worldRot = GetWorldRotationAt(zPos);
        GameObject gateGo = Instantiate(gatePrefab, worldPos, worldRot, parent);
        GateTrigger gateTrigger = gateGo.GetComponent<GateTrigger>();
        if (gateTrigger != null)
        {
            gateTrigger.operationText = opText;
        }
        return gateGo;
    }

    private float[] GetRandomLanes(int level)
    {
        if (level < 2)
        {
            return new float[] { -2f, 2f };
        }
        int combo = Random.Range(0, 3);
        if (combo == 0) return new float[] { -2f, 0f };
        if (combo == 1) return new float[] { 0f, 2f };
        return new float[] { -2f, 2f };
    }

    private string[] GetRandomOperations(int level)
    {
        string leftOp = "+5";
        string rightOp = "+10";

        if (level == 1)
        {
            string[] rewards = { "+10", "+15", "+20" };
            leftOp = rewards[Random.Range(0, rewards.Length)];
            rightOp = rewards[Random.Range(0, rewards.Length)];
            while (leftOp == rightOp)
            {
                rightOp = rewards[Random.Range(0, rewards.Length)];
            }
        }
        else if (level >= 2 && level <= 5)
        {
            string[] positiveRewards = { "+10", "+15", "+20" };
            string[] negativePenalties = { "-5", "-10" };

            if (Random.value < 0.5f)
            {
                leftOp = positiveRewards[Random.Range(0, positiveRewards.Length)];
                rightOp = negativePenalties[Random.Range(0, negativePenalties.Length)];
            }
            else
            {
                leftOp = negativePenalties[Random.Range(0, negativePenalties.Length)];
                rightOp = positiveRewards[Random.Range(0, positiveRewards.Length)];
            }
        }
        else
        {
            // Guarantee one positive gate and one penalty gate per pair, same as the level 2-5 tier -
            // picking both sides independently from one mixed pool could give two penalty gates in a
            // row with no safe option at all, right at the point the game is already hardest.
            string[] positiveRewards = { "+20", "+30", "+40" };
            string[] negativePenalties = { "-15", "-10", "-20", "-5" };

            if (Random.value < 0.5f)
            {
                leftOp = positiveRewards[Random.Range(0, positiveRewards.Length)];
                rightOp = negativePenalties[Random.Range(0, negativePenalties.Length)];
            }
            else
            {
                leftOp = negativePenalties[Random.Range(0, negativePenalties.Length)];
                rightOp = positiveRewards[Random.Range(0, positiveRewards.Length)];
            }
        }

        return new string[] { leftOp, rightOp };
    }

    private void SpawnCoinCluster(float startZ, float laneX, Transform parent)
    {
        if (coinPrefab == null) return;

        // Single-file straight line: coins are arranged one after another along the track,
        // staying on the same lane (constant laneX) with a clean, consistent gap between each coin.
        int count = 5;
        float spacing = 1.6f; // consistent physical gap between each individual coin

        for (int j = 0; j < count; j++)
        {
            float offsetZ = j * spacing;
            Vector3 pos = GetWorldPositionAt(startZ + offsetZ, laneX, 1.35f);
            Quaternion rot = GetWorldRotationAt(startZ + offsetZ);
            Instantiate(coinPrefab, pos, rot, parent);
        }
    }

    private void SpawnObstacle(float zPosition, int level, Transform parent)
    {
        GameObject selectedPrefab = null;

        // 35% chance to spawn a charging Red Enemy Squad instead of a mechanical hazard
        if (enemySquadPrefab != null && Random.value < 0.35f)
        {
            selectedPrefab = enemySquadPrefab;
        }
        else if (hazardPrefabs != null && hazardPrefabs.Length > 0)
        {
            selectedPrefab = hazardPrefabs[Random.Range(0, hazardPrefabs.Length)];
        }
        else
        {
            selectedPrefab = obstaclePrefab;
        }

        if (selectedPrefab == null) return;

        // Ensure that when a hazard spawns, it picks only ONE random lane (Left, Center, or Right)
        float[] lanes = { -2f, 0f, 2f };
        float laneX = lanes[Random.Range(0, lanes.Length)];
        
        // Ground the hazard perfectly depending on its physical structure
        float yLocal = 0f;
        if (selectedPrefab.name.Contains("SpikedRoller"))
        {
            yLocal = 0.2f; // Spiked Roller bottom is at -0.2 relative to parent
        }
        else if (selectedPrefab.name.Contains("ObstaclePrefab") || selectedPrefab.name.Contains("Cube"))
        {
            yLocal = 0.75f; // Static obstacle block center is at 0, half-height is 0.75 (scale 1.5)
        }
        else
        {
            yLocal = 0f; // Enemy squads, boxing gloves, and pendulums have pivots perfectly at the bottom (y=0)
        }

        Vector3 pos = GetWorldPositionAt(zPosition, laneX, yLocal);
        Quaternion rot = GetWorldRotationAt(zPosition);
        Instantiate(selectedPrefab, pos, rot, parent);
    }
}
