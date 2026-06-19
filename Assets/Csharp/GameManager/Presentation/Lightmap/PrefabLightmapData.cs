using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PrefabLightmapData", menuName = "Rendering/Prefab Lightmap Data")]
public class PrefabLightmapData : ScriptableObject
{
    [Serializable]
    public class RendererLightmapInfo
    {
        public string rendererPath;
        public int lightmapIndex = -1;
        public Vector4 lightmapScaleOffset = new Vector4(1f, 1f, 0f, 0f);
        public int realtimeLightmapIndex = -1;
        public Vector4 realtimeLightmapScaleOffset = new Vector4(1f, 1f, 0f, 0f);
    }

    public Texture2D[] lightmapColor = Array.Empty<Texture2D>();
    public Texture2D[] lightmapDir = Array.Empty<Texture2D>();
    public Texture2D[] shadowMask = Array.Empty<Texture2D>();
    public RendererLightmapInfo[] renderers = Array.Empty<RendererLightmapInfo>();

    public int LightmapCount
    {
        get
        {
            return Mathf.Max(
                lightmapColor != null ? lightmapColor.Length : 0,
                lightmapDir != null ? lightmapDir.Length : 0,
                shadowMask != null ? shadowMask.Length : 0
            );
        }
    }
}
