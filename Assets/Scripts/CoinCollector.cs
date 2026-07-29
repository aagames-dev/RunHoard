using UnityEngine;

public class CoinCollector : MonoBehaviour
{
    private bool collected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        // Check if collision is with friendly active stickman or Player Cannon
        if (TriggerUtils.GetStickman(other) != null || TriggerUtils.GetLeader(other) != null)
        {
            collected = true;
            CollectCoin();
        }
    }

    private void CollectCoin()
    {
        // Disable collider immediately to prevent double collection during animation
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Add coin to global score tracker
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCoins(1);
        }

        // Play the golden coin visual pop particle system burst
        if (VisualEffectManager.Instance != null)
        {
            VisualEffectManager.Instance.TriggerCoinPop(transform.position);
        }

        // Play juicy pop sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPopSFX();
        }

        // Play the highly satisfying high-pitched synthesis chime
        PlaySatisfyingCollectSound();

        // Trigger highly tactile squash/pop scale-down animation before destroying
        StartCoroutine(TactilePopAndDestroy());
    }

    private System.Collections.IEnumerator TactilePopAndDestroy()
    {
        Vector3 baseScale = transform.localScale;

        // Pop up to 1.45f quickly, then rapidly deflate to 0f
        yield return ScaleTween.Run(transform, baseScale,
            t => t < 0.35f ? Mathf.Lerp(1.0f, 1.45f, t / 0.35f) : Mathf.Lerp(1.45f, 0.0f, (t - 0.35f) / 0.65f),
            0.12f, false);

        Destroy(gameObject);
    }

    private void PlaySatisfyingCollectSound()
    {
        GameObject audioGo = new GameObject("CoinAudio");
        audioGo.transform.position = transform.position;
        AudioSource audioSource = audioGo.AddComponent<AudioSource>();
        audioSource.clip = CreateSatisfyingPitchClip();
        audioSource.volume = 0.35f;
        audioSource.pitch = Random.Range(1.05f, 1.2f);
        audioSource.Play();
        Destroy(audioGo, 1.0f);
    }

    private AudioClip CreateSatisfyingPitchClip()
    {
        // Sweep frequency up rapidly from 950Hz to 1450Hz for maximum dopamine
        return ToneSynth.CreateSweepClip("CoinBeep", 44100, 0.12f, 950f, 1450f, progress => 1f - progress);
    }
}
