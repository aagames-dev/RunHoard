using UnityEngine;

public class BoxingGlove : MonoBehaviour
{
    public float bounceSpeed = 5f;
    public float bounceHeight = 1.2f;

    private Transform spring;
    private Transform glove;
    private Vector3 initialGlovePos;

    private void Start()
    {
        spring = transform.Find("Spring");
        glove = transform.Find("GloveBody");
        if (glove != null)
        {
            initialGlovePos = glove.localPosition;
        }
    }

    private void Update()
    {
        float bounceFactor = (Mathf.Sin(Time.time * bounceSpeed) + 1f) * 0.5f; // scale 0 to 1
        float currentOffset = bounceFactor * bounceHeight;

        if (glove != null)
        {
            glove.localPosition = initialGlovePos + new Vector3(0f, currentOffset, 0f);
        }

        if (spring != null)
        {
            // Squash and stretch the spring scale along Y to match the glove position
            Vector3 scale = spring.localScale;
            scale.y = 0.5f + (bounceFactor * 1.0f); // spring expands with glove
            spring.localScale = scale;
            
            // Adjust position slightly so it remains anchored to the base
            Vector3 pos = spring.localPosition;
            pos.y = scale.y;
            spring.localPosition = pos;
        }
    }
}
