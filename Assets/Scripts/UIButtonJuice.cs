using UnityEngine;

public class UIButtonJuice : MonoBehaviour
{
    public float pulseSpeed = 4f;
    public float pulseMagnitude = 0.05f;

    private Vector3 initialScale;

    private void Start()
    {
        initialScale = transform.localScale;
    }

    private void Update()
    {
        // Smooth scale-pulsing tween using the exact clean sine wave formula requested
        float currentScale = 1.0f + Mathf.Sin(Time.time * pulseSpeed) * pulseMagnitude;
        transform.localScale = initialScale * currentScale;
    }
}
