using UnityEngine;

public class EnemyStickman : MonoBehaviour
{
    private bool resolved = false;

    private void OnTriggerEnter(Collider other)
    {
        if (resolved) return;

        StickmanController friendly = other.GetComponent<StickmanController>();
        if (friendly != null)
        {
            resolved = true;
            ResolveCombat(friendly);
            return;
        }

        // Also check if collision is with the Hat-Man leader directly to hurt the crowd/leader
        PlayerController leader = other.GetComponent<PlayerController>();
        if (leader != null)
        {
            resolved = true;
            ResolveLeaderCollision();
        }
    }

    private void ResolveCombat(StickmanController friendly)
    {
        Vector3 combatPos = (transform.position + friendly.transform.position) * 0.5f;

        // Play visual smoke puff combat effect
        if (VisualEffectManager.Instance != null)
        {
            VisualEffectManager.Instance.TriggerMuzzleFlash(combatPos);
        }

        // Play juicy pop sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPopSFX();
        }

        // Red enemy financial penalty: deduct coins and spawn floating text (exact same mechanics as red cubes)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCoins(-15);
        }
        FloatingText.Spawn(combatPos, "-15", Color.red);

        // Instantly destroy/neutralize both stickmen (1:1 combat sacrifice)
        Destroy(friendly.gameObject);
        Destroy(gameObject);
    }

    private void ResolveLeaderCollision()
    {
        // If they crash into the main player directly and player has active crowd, sacrifice a follower
        if (CrowdManager.Instance != null && CrowdManager.Instance.ActiveCrowdCount > 0)
        {
            var activeList = CrowdManager.Instance.GetActiveStickmen();
            if (activeList != null && activeList.Count > 0)
            {
                StickmanController targetStickman = activeList[0];
                if (targetStickman != null)
                {
                    ResolveCombat(targetStickman);
                    return;
                }
            }
        }

        // Otherwise, if no crowd followers exist, trigger game over immediately!
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
        
        Destroy(gameObject);
    }
}
