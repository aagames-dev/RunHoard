using UnityEngine;
using System.Collections.Generic;

public class CloudGenerator : MonoBehaviour
{
    public static CloudGenerator Instance { get; private set; }

    [Header("Cloud Pool Settings")]
    public int cloudCount = 20;
    public float minHeight = 25f;
    public float maxHeight = 40f;
    public float minWidth = -120f;
    public float maxWidth = 120f;
    public float spawnAheadDistance = 250f;
    public float despawnBehindDistance = 60f;

    [Header("Movement Settings")]
    public float minDriftSpeed = 0.5f;
    public float maxDriftSpeed = 2f;

    private Material cloudMaterial;
    private Transform playerTrans;

    private struct CloudInfo
    {
        public GameObject gameObject;
        public float driftSpeed;
    }
    private List<CloudInfo> cloudList = new List<CloudInfo>();

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
    }

    private void Start()
    {
        // 1. Create a beautiful stylized soft unlit/simple lit material for low-poly clouds
        cloudMaterial = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
        cloudMaterial.color = new Color(0.95f, 0.95f, 1.0f, 0.85f); // Beautiful soft pastel white
        cloudMaterial.SetFloat("_Smoothness", 0.1f);
        cloudMaterial.SetFloat("_Metallic", 0f);

        // Find player transform to snap/wrap clouds relative to forward progress
        if (PlayerController.Instance != null)
        {
            playerTrans = PlayerController.Instance.transform;
        }
        else
        {
            GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null) playerTrans = playerGo.transform;
        }

        // Initialize the clouds spread out ahead and around the starting zone
        float playerZ = playerTrans != null ? playerTrans.position.z : 0f;
        for (int i = 0; i < cloudCount; i++)
        {
            float randomZ = Random.Range(playerZ - despawnBehindDistance, playerZ + spawnAheadDistance);
            SpawnSingleCloud(randomZ);
        }
    }

    private void Update()
    {
        if (playerTrans == null)
        {
            if (PlayerController.Instance != null) playerTrans = PlayerController.Instance.transform;
            return;
        }

        float playerZ = playerTrans.position.z;

        // Move clouds slowly and check for wrapping behind the player
        for (int i = 0; i < cloudList.Count; i++)
        {
            CloudInfo info = cloudList[i];
            if (info.gameObject == null) continue;

            // Horizontal slow drift
            info.gameObject.transform.Translate(Vector3.right * info.driftSpeed * Time.deltaTime, Space.World);

            // Wrap Z-position: If player has run past the cloud, teleport it ahead
            if (info.gameObject.transform.position.z < playerZ - despawnBehindDistance)
            {
                Vector3 currentPos = info.gameObject.transform.position;
                float newZ = playerZ + spawnAheadDistance + Random.Range(-20f, 40f);
                float newX = Random.Range(minWidth, maxWidth);
                float newY = Random.Range(minHeight, maxHeight);
                
                info.gameObject.transform.position = new Vector3(newX, newY, newZ);
                
                // Randomize scale gently to keep it varied and fresh
                float baseScale = Random.Range(4f, 9f);
                info.gameObject.transform.localScale = new Vector3(baseScale * Random.Range(1.2f, 1.8f), baseScale * Random.Range(0.6f, 1f), baseScale * Random.Range(1f, 1.4f));
            }
        }
    }

    private void SpawnSingleCloud(float zPos)
    {
        // A single cloud consists of a parent object holding 3-5 intersecting low-poly spheres
        GameObject cloudParent = new GameObject("ProceduralCloud");
        cloudParent.transform.SetParent(transform);

        float xPos = Random.Range(minWidth, maxWidth);
        float yPos = Random.Range(minHeight, maxHeight);
        cloudParent.transform.position = new Vector3(xPos, yPos, zPos);

        int spheresCount = Random.Range(3, 6);
        for (int i = 0; i < spheresCount; i++)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(cloudParent.transform, false);

            // Disable sphere collider to prevent raycast/physics collision interference
            if (sphere.GetComponent<SphereCollider>())
            {
                DestroyImmediate(sphere.GetComponent<SphereCollider>());
            }

            // Assign cloud material
            Renderer r = sphere.GetComponent<Renderer>();
            if (r != null)
            {
                r.sharedMaterial = cloudMaterial;
            }

            // Fluffy overlapping low-poly offsets
            float localX = i == 0 ? 0f : Random.Range(-1.2f, 1.2f);
            float localY = i == 0 ? 0f : Random.Range(-0.4f, 0.4f);
            float localZ = i == 0 ? 0f : Random.Range(-1.0f, 1.0f);
            sphere.transform.localPosition = new Vector3(localX, localY, localZ);

            // Center sphere is largest, side spheres are smaller
            float size = i == 0 ? Random.Range(1f, 1.5f) : Random.Range(0.6f, 1.1f);
            sphere.transform.localScale = new Vector3(size, size * Random.Range(0.8f, 1.2f), size);
        }

        // Randomize parent overall low-poly cloud scale
        float baseScale = Random.Range(4f, 9f);
        cloudParent.transform.localScale = new Vector3(baseScale * Random.Range(1.2f, 1.8f), baseScale * Random.Range(0.6f, 1f), baseScale * Random.Range(1f, 1.4f));

        // Create CloudInfo to track drift
        CloudInfo info = new CloudInfo();
        info.gameObject = cloudParent;
        // Half drift left, half drift right
        float direction = Random.value < 0.5f ? -1f : 1f;
        info.driftSpeed = direction * Random.Range(minDriftSpeed, maxDriftSpeed);

        cloudList.Add(info);
    }
}