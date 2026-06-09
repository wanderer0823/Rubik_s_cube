using UnityEngine;

[ExecuteAlways]
public class PixelatePostProcess : MonoBehaviour
{
    [Range(1, 16)]
    public int pixelSize = 4;

    [Range(0, 1)]
    public float blurStrength = 0.25f;

    private void Update()
    {
        if (PixelateRenderFeature.instance == null)
            return;

        PixelateRenderFeature.instance.settings.pixelSize = pixelSize;
        PixelateRenderFeature.instance.settings.blurStrength = blurStrength;
    }
}