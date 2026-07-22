using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    private TextMeshPro textMesh;
    private float lifetime = 1.2f;
    private float timer = 0f;
    private Vector3 moveDirection;
    private Color startColor;
    private float floatSpeed = 4f;

    public static void Spawn(Vector3 worldPosition, string text, Color color)
    {
        GameObject go = new GameObject("FloatingText_" + text);
        // Position slightly offset towards camera or up so it doesn't clip with ground/player
        go.transform.position = worldPosition + Vector3.up * 1.5f;
        
        FloatingText ft = go.AddComponent<FloatingText>();
        ft.Initialize(text, color);
    }

    private void Initialize(string text, Color color)
    {
        textMesh = gameObject.AddComponent<TextMeshPro>();
        textMesh.text = text;
        textMesh.fontSize = 8;
        textMesh.alignment = TextAlignmentOptions.Center;
        
        // Apply Oswald Bold font if available in resources, otherwise default
        TMP_FontAsset oswald = Resources.Load<TMP_FontAsset>("Fonts & Materials/Oswald Bold SDF");
        if (oswald != null)
        {
            textMesh.font = oswald;
        }
        else
        {
            textMesh.fontStyle = FontStyles.Bold;
        }

        startColor = color;
        textMesh.color = startColor;

        // Apply a cool dark shadow outline
        textMesh.outlineWidth = 0.25f;
        textMesh.outlineColor = new Color(0.05f, 0.05f, 0.1f, 1f);

        // Gentle random horizontal offset for the drift direction
        moveDirection = new Vector3(Random.Range(-1.5f, 1.5f), 1f, Random.Range(-0.5f, 0.5f)).normalized;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / lifetime;

        if (progress >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        // Float up like a balloon (gently slow down or drift upwards)
        transform.position += moveDirection * floatSpeed * Time.deltaTime;
        
        // Add a tiny bit of horizontal sway
        transform.position += transform.right * Mathf.Sin(timer * 5f) * 0.3f * Time.deltaTime;

        // Gently fade out alpha
        Color curColor = startColor;
        curColor.a = Mathf.Lerp(1f, 0f, progress);
        textMesh.color = curColor;

        // Face the camera
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }
    }
}