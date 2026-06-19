using UnityEngine;

[DisallowMultipleComponent]
public class PrefabLightmapBinding : MonoBehaviour
{
    public PrefabLightmapData lightmapData;
    public bool applyOnEnable = true;
    public bool applyOncePerInstance = true;

    private bool hasApplied;

    private void Awake()
    {
        if (!applyOnEnable)
            ApplyNow();
    }

    private void OnEnable()
    {
        if (applyOnEnable)
            ApplyNow();
    }

    public void ApplyNow()
    {
        if (applyOncePerInstance && hasApplied)
            return;

        if (lightmapData == null)
        {
            Debug.LogWarning($"PrefabLightmapBinding: Missing lightmap data on '{name}'.", this);
            return;
        }

        PrefabLightmapApplier.Apply(gameObject, lightmapData);
        hasApplied = true;
    }
}
