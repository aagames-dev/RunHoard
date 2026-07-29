using UnityEngine;

public static class ToneSynth
{
    // Shared procedural tone generator: a single sine sweep from startFreq to endFreq over duration,
    // shaped by an envelope function of normalized progress [0,1]. Used for short synthesized SFX
    // (pop/chime style sounds) so each caller only needs to supply its own pitch range and envelope.
    public static AudioClip CreateSweepClip(string name, int sampleRate, float duration, float startFreq, float endFreq, System.Func<float, float> envelope, float amplitude = 1f)
    {
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float progress = t / duration;
            float freq = Mathf.Lerp(startFreq, endFreq, progress);
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope(progress) * amplitude;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
