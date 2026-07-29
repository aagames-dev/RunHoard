using UnityEngine;
using System.Collections.Generic;

public class EnemySquad : MonoBehaviour
{
    public float triggerDistance = 15f;
    public float chargeSpeed = 6f;
    private bool isCharging = false;
    private List<Transform> enemyMembers = new List<Transform>();

    private Vector3 startLocalPosition;

    private void Start()
    {
        startLocalPosition = transform.localPosition;

        // Gather all child red stickmen
        foreach (Transform child in transform)
        {
            if (child.name.Contains("EnemyMember"))
            {
                enemyMembers.Add(child);
            }
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        if (PlayerController.Instance == null)
            return;

        float distance = transform.position.z - PlayerController.Instance.transform.position.z;

        // If player is close enough, start charging forward
        if (!isCharging && distance > 0f && distance <= triggerDistance)
        {
            isCharging = true;
        }

        if (isCharging)
        {
            // Charge down the lane (-Z direction)
            transform.Translate(Vector3.back * chargeSpeed * Time.deltaTime, Space.World);

            // Animate members with high-speed bouncy cartoon running bobbing
            float bobSpeed = 22f;
            float bobAmount = 0.15f;
            foreach (var member in enemyMembers)
            {
                if (member == null) continue;
                Transform visual = member.Find("Visuals");
                if (visual != null)
                {
                    float bob = Mathf.Abs(Mathf.Sin(Time.time * bobSpeed)) * bobAmount;
                    visual.localPosition = new Vector3(visual.localPosition.x, bob, visual.localPosition.y);
                    
                    // Simple cartoon running tilt
                    visual.localRotation = Quaternion.Euler(15f, 180f, Mathf.Sin(Time.time * bobSpeed) * 10f);
                }
            }
        }

        // Sideways walking logic for Level >= 2: Oscillate smoothly across lanes relative to start X position
        int currentLevel = LevelManager.CurrentLevel;
        if (currentLevel >= 2)
        {
            float sideRange = 1.6f;
            float sideSpeed = 2.5f;
            float offset = Mathf.Sin(Time.time * sideSpeed) * sideRange;
            transform.localPosition = new Vector3(startLocalPosition.x + offset, transform.localPosition.y, transform.localPosition.z);
        }
    }
}
