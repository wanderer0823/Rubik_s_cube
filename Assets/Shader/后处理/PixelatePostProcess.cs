using UnityEngine;

[ExecuteAlways]
public class PixelatePostProcess : MonoBehaviour
{
    [SerializeField][Range(1f, 100f)] private float pixelSize = 1f;

    private void OnValidate()
    {
        if (PixelateRenderFeature.instance != null)
        {
            PixelateRenderFeature.instance.settings.pixelSize = pixelSize;
        }
    }
}
