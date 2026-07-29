using UnityEngine;

public static class RendererColorUtils
{
    // Instantiates the renderer's material (via .material) and sets a flat color plus a matching
    // emissive glow, the recurring pattern behind most of this project's neon/vibrant recoloring.
    public static void SetEmissiveColor(Renderer r, Color color, float emissionMultiplier)
    {
        Material mat = r.material;
        mat.color = color;
        mat.SetColor("_EmissionColor", color * emissionMultiplier);
    }
}
