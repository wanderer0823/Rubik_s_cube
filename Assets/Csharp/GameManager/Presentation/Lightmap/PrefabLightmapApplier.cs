using System.Collections.Generic;
using UnityEngine;

public static class PrefabLightmapApplier
{
    public static void Apply(GameObject instanceRoot, PrefabLightmapData data)
    {
        if (instanceRoot == null || data == null)
            return;

        int baseIndex = EnsureLightmaps(data);

        if (data.renderers == null)
            return;

        foreach (PrefabLightmapData.RendererLightmapInfo info in data.renderers)
        {
            if (info == null)
                continue;

            Renderer renderer = FindRenderer(instanceRoot.transform, info.rendererPath);
            if (renderer == null)
            {
                Debug.LogWarning($"PrefabLightmapApplier: Renderer not found at path '{info.rendererPath}' under '{instanceRoot.name}'.");
                continue;
            }

            renderer.lightmapIndex = RemapIndex(info.lightmapIndex, baseIndex);
            renderer.lightmapScaleOffset = info.lightmapScaleOffset;
            renderer.realtimeLightmapIndex = RemapIndex(info.realtimeLightmapIndex, baseIndex);
            renderer.realtimeLightmapScaleOffset = info.realtimeLightmapScaleOffset;
        }
    }

    private static int EnsureLightmaps(PrefabLightmapData data)
    {
        int lightmapCount = data.LightmapCount;
        if (lightmapCount <= 0)
            return 0;

        LightmapData[] existing = LightmapSettings.lightmaps ?? new LightmapData[0];
        int existingBase = FindExistingSequence(existing, data, lightmapCount);
        if (existingBase >= 0)
            return existingBase;

        var merged = new List<LightmapData>(existing.Length + lightmapCount);
        merged.AddRange(existing);

        for (int i = 0; i < lightmapCount; i++)
        {
            merged.Add(new LightmapData
            {
                lightmapColor = Get(data.lightmapColor, i),
                lightmapDir = Get(data.lightmapDir, i),
                shadowMask = Get(data.shadowMask, i)
            });
        }

        LightmapSettings.lightmaps = merged.ToArray();
        return existing.Length;
    }

    private static int FindExistingSequence(LightmapData[] existing, PrefabLightmapData data, int lightmapCount)
    {
        for (int start = 0; start <= existing.Length - lightmapCount; start++)
        {
            bool allMatch = true;
            for (int i = 0; i < lightmapCount; i++)
            {
                LightmapData current = existing[start + i];
                if (current.lightmapColor != Get(data.lightmapColor, i)
                    || current.lightmapDir != Get(data.lightmapDir, i)
                    || current.shadowMask != Get(data.shadowMask, i))
                {
                    allMatch = false;
                    break;
                }
            }

            if (allMatch)
                return start;
        }

        return -1;
    }

    private static Texture2D Get(Texture2D[] textures, int index)
    {
        return textures != null && index >= 0 && index < textures.Length ? textures[index] : null;
    }

    private static int RemapIndex(int savedIndex, int baseIndex)
    {
        return savedIndex >= 0 ? savedIndex + baseIndex : -1;
    }

    private static Renderer FindRenderer(Transform root, string path)
    {
        Transform target = string.IsNullOrEmpty(path) ? root : root.Find(path);
        return target != null ? target.GetComponent<Renderer>() : null;
    }
}
