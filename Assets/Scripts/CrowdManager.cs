using UnityEngine;
using System.Collections.Generic;

public class CrowdManager : MonoBehaviour
{
    public static CrowdManager Instance { get; private set; }

    [Header("Stickman Prefab & Settings")]
    public GameObject stickmanPrefab;
    public float stickmanSpeed = 12f;
    
    [Header("Flocking Settings")]
    public float cohesionWeight = 1f;
    public float separationWeight = 1.5f;
    public float separationDistance = 0.6f;

    private List<StickmanController> activeStickmen = new List<StickmanController>();

    public int ActiveCrowdCount => activeStickmen.Count;

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

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateCrowdCount(activeStickmen.Count);
        }
    }

    public void RegisterStickman(StickmanController stickman)
    {
        if (!activeStickmen.Contains(stickman))
        {
            activeStickmen.Add(stickman);
        }
    }

    public void UnregisterStickman(StickmanController stickman)
    {
        if (activeStickmen.Contains(stickman))
        {
            activeStickmen.Remove(stickman);
        }

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            if (activeStickmen.Count == 0 && PlayerController.Instance != null && PlayerController.Instance.IsAtEndBase)
            {
                // Reaching the castle gate should successfully clear the level, never trigger a game failure
                GameManager.Instance.VictoryState();
            }
        }
    }

    public void SpawnStickman(Vector3 position)
    {
        if (stickmanPrefab == null) return;
        GameObject go = Instantiate(stickmanPrefab, position, Quaternion.identity);
        StickmanController stickman = go.GetComponent<StickmanController>();
        if (stickman != null)
        {
            RegisterStickman(stickman);
        }
    }

    public void MultiplyStickmen(Vector3 spawnPosition, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-0.5f, 0.5f), 0.1f, Random.Range(-0.5f, 0.5f));
            SpawnStickman(spawnPosition + offset);
        }
    }

    public List<StickmanController> GetActiveStickmen()
    {
        return activeStickmen;
    }
}
