using UnityEngine;

public class VisualEffectManager : MonoBehaviour
{
    public static VisualEffectManager Instance { get; private set; }

    [Header("Particle Prefabs")]
    public GameObject muzzleFlashPrefab;
    public GameObject gateCrossPrefab;
    public GameObject stickmanDeathPrefab;
    public GameObject baseDamagePrefab;
    public GameObject confettiPrefab;
    public GameObject coinPopPrefab;

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

    private void Start()
    {
        CreateDefaultParticlesIfMissing();
    }

    private void CreateDefaultParticlesIfMissing()
    {
        // Programmatically create beautiful procedural particle system prefabs if none are assigned
        if (muzzleFlashPrefab == null) muzzleFlashPrefab = CreateParticleSystemPrefab("MuzzleFlash", Color.cyan, 0.4f, 20f);
        if (gateCrossPrefab == null) gateCrossPrefab = CreateParticleSystemPrefab("GateCross", Color.green, 0.6f, 30f);
        if (stickmanDeathPrefab == null) stickmanDeathPrefab = CreateParticleSystemPrefab("StickmanDeath", Color.cyan, 0.5f, 15f);
        if (baseDamagePrefab == null) baseDamagePrefab = CreateParticleSystemPrefab("BaseDamage", Color.red, 0.8f, 40f);
        // Confetti particles completely disabled to maximize performance and frames-per-second
        // if (confettiPrefab == null) confettiPrefab = CreateConfettiPrefab();
        if (coinPopPrefab == null) coinPopPrefab = CreateParticleSystemPrefab("CoinPop", new Color(1f, 0.85f, 0f), 0.5f, 10f);
    }

    private GameObject CreateConfettiPrefab()
    {
        GameObject go = new GameObject("Confetti_Particles");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        var main = ps.main;
        main.duration = 2.0f;
        main.loop = false;
        main.startLifetime = 2.0f;
        main.startSpeed = 12f;
        main.startSize = 0.35f;
        main.gravityModifier = 0.8f; // pull down dynamically like real paper flakes
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 150) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 1.5f;

        var colorModule = ps.colorOverLifetime;
        colorModule.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.red, 0.0f), 
                new GradientColorKey(Color.yellow, 0.25f), 
                new GradientColorKey(Color.green, 0.5f), 
                new GradientColorKey(Color.cyan, 0.75f), 
                new GradientColorKey(Color.magenta, 1.0f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1.0f, 0.0f), 
                new GradientAlphaKey(0.0f, 1.0f) 
            }
        );
        colorModule.color = grad;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Lit");
        if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
        Material mat = new Material(particleShader);
        mat.color = Color.white;
        renderer.sharedMaterial = mat;

        go.transform.SetParent(transform);
        go.SetActive(false);
        return go;
    }

    private GameObject CreateParticleSystemPrefab(string name, Color color, float duration, float speed)
    {
        GameObject go = new GameObject(name + "_Particles");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        var main = ps.main;
        main.duration = duration;
        main.loop = false;
        main.startLifetime = duration;
        main.startSpeed = speed;
        main.startSize = 0.25f;
        main.startColor = color;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 25) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        var colorModule = ps.colorOverLifetime;
        colorModule.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(color, 0.0f), new GradientColorKey(Color.white, 0.5f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorModule.color = grad;

        // Set Material to glowing
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Lit");
        if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
        Material mat = new Material(particleShader);
        mat.color = color;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * 4f);
        renderer.sharedMaterial = mat;

        // Keep a copy in scene or return it to be instantiated
        go.transform.SetParent(transform);
        go.SetActive(false);
        return go;
    }

    public void PlayEffect(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return;
        GameObject instance = Instantiate(prefab, position, Quaternion.identity);
        instance.SetActive(true);
        ParticleSystem ps = instance.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
            Destroy(instance, ps.main.duration + 0.2f);
        }
        else
        {
            Destroy(instance, 1f);
        }
    }

    public void TriggerMuzzleFlash(Vector3 position)
    {
        PlayEffect(muzzleFlashPrefab, position);
    }

    public void TriggerGateCross(Vector3 position)
    {
        PlayEffect(gateCrossPrefab, position);
    }

    public void TriggerStickmanDeath(Vector3 position)
    {
        PlayEffect(stickmanDeathPrefab, position);
    }

    public void TriggerBaseDamage(Vector3 position)
    {
        // Particles completely disabled to prevent mobile performance issues and lag spikes
    }

    public void TriggerConfetti(Vector3 position)
    {
        // Confetti completely disabled for lightweight chiptune feel and optimal performance
    }

    public void TriggerCoinPop(Vector3 position)
    {
        PlayEffect(coinPopPrefab, position);
    }
}
