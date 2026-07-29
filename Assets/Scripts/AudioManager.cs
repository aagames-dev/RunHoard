using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudio();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudio()
    {
        // 1. Create music AudioSource
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = 0.25f;

        // 2. Create SFX AudioSource
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.volume = 0.45f;

        // 3. Generate Upbeat Background Music loop procedurally
        musicSource.clip = CreateProceduralBackgroundMusic();
        musicSource.Play();
    }

    public void PauseMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }

    public void ResumeMusic()
    {
        if (musicSource != null)
        {
            musicSource.UnPause();
        }
    }

    private AudioClip CreateProceduralBackgroundMusic()
    {
        int sampleRate = 22050;
        float bpm = 125f;
        float beatDuration = 60f / bpm;
        float stepDuration = beatDuration / 4f; // 16th note steps

        // 8 measures of 16 steps = 128 steps total
        int totalSteps = 128;
        float duration = totalSteps * stepDuration;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        // Define a catchy pentatonic chord progression
        // C minor: C, Eb, F, G, Bb
        float[] bassNotes = { 65.41f, 77.78f, 87.31f, 98.00f }; // C2, Eb2, F2, G2
        float[] melodyNotes = { 261.63f, 311.13f, 349.23f, 392.00f, 466.16f, 523.25f }; // C4, Eb4, F4, G4, Bb4, C5

        // Simple step sequencers
        // Bassline: basic house/retro offbeat rhythm
        int[] bassPattern = { 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 1 };
        // Melody: catchy rhythmic arpeggios
        int[] melodyPattern = { 1, 0, 0, 1, 0, 1, 0, 0, 1, 0, 1, 0, 0, 1, 1, 0 };

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            int currentStep = Mathf.FloorToInt(t / stepDuration) % totalSteps;
            int measure = currentStep / 16;
            int stepInMeasure = currentStep % 16;

            // Determine chord root for the current measure
            float rootFreq = bassNotes[measure % bassNotes.Length];

            // 1. Synthesize Bass (Triangle Wave)
            float bassSample = 0f;
            if (bassPattern[stepInMeasure] == 1)
            {
                float stepStart = (currentStep) * stepDuration;
                float timeInStep = t - stepStart;
                
                // Classic triangle/square blend with decay envelope
                float envelope = Mathf.Max(0f, 1f - (timeInStep / stepDuration) * 1.5f);
                float angle = 2f * Mathf.PI * rootFreq * timeInStep;
                
                // Triangle wave calculation: 8/pi^2 * sum((sin((2k+1)x)/(2k+1)^2)) -> simplified approximation
                bassSample = Mathf.PingPong(angle / Mathf.PI, 1.0f) * 2f - 1.0f;
                bassSample *= envelope * 0.45f;
            }

            // 2. Synthesize Melody (Lead Square Wave)
            float melodySample = 0f;
            if (melodyPattern[stepInMeasure] == 1)
            {
                float stepStart = (currentStep) * stepDuration;
                float timeInStep = t - stepStart;
                
                float melodyEnvelope = Mathf.Max(0f, 1f - (timeInStep / (stepDuration * 1.2f)));
                
                // Pick melody note based on measure and step to keep it dynamic and fresh
                int noteIndex = (measure * 3 + stepInMeasure * 2) % melodyNotes.Length;
                float leadFreq = melodyNotes[noteIndex];

                // Add nice chiptune pulse/square wave
                float angle = 2f * Mathf.PI * leadFreq * timeInStep;
                melodySample = (Mathf.Sin(angle) >= 0f) ? 1.0f : -1.0f;
                
                // Soft lowpass filter equivalent for lead warmth
                melodySample *= melodyEnvelope * 0.18f;
            }

            // 3. Synthesize hi-hat/noise drum
            float noiseSample = 0f;
            if (stepInMeasure % 4 == 2) // offbeat hi-hat on every off-beat
            {
                float stepStart = (currentStep) * stepDuration;
                float timeInStep = t - stepStart;
                float drumEnvelope = Mathf.Max(0f, 1f - (timeInStep / 0.05f));
                
                // White noise representation
                noiseSample = (Random.value * 2f - 1f) * drumEnvelope * 0.06f;
            }

            // Combine tracks
            samples[i] = bassSample + melodySample + noiseSample;
        }

        AudioClip clip = AudioClip.Create("UpbeatRetroMusic", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public void PlayPopSFX()
    {
        if (sfxSource == null) return;

        // Procedural satisfying "pop" chime
        sfxSource.PlayOneShot(CreatePopClip());
    }

    public void PlayExplosionSFX()
    {
        if (sfxSource == null) return;

        // Procedural heavy, booming, cartoonish explosion
        sfxSource.PlayOneShot(CreateExplosionClip());
    }

    private AudioClip CreatePopClip()
    {
        // Sweep frequency up rapidly from 800Hz to 1600Hz
        return ToneSynth.CreateSweepClip("PopSFX", 22050, 0.15f, 800f, 1600f,
            progress => Mathf.Sin(progress * Mathf.PI) * (1f - progress), 0.8f);
    }

    private AudioClip CreateExplosionClip()
    {
        int sampleRate = 22050;
        float duration = 0.8f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float progress = t / duration;
            
            // Sweep frequency down from 150Hz to 20Hz (booming sub bass)
            float freq = Mathf.Lerp(150f, 20f, progress);
            
            // Generate rumble noise
            float rumble = Random.value * 2f - 1f;

            // Combine low frequency boom and rumble with exponential decay envelope
            float envelope = Mathf.Exp(-progress * 5f);
            samples[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f + rumble * 0.3f) * envelope;
        }

        AudioClip clip = AudioClip.Create("ExplosionSFX", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
