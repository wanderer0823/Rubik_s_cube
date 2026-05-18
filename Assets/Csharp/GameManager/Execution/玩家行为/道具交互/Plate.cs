using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挂在 Plate 根物体上，负责记录触发计数、切换材质，并在达到阈值后压板开门。
/// </summary>
public class Plate : MonoBehaviour
{
    private struct MaterialSlotBinding
    {
        public Renderer Renderer;
        public int SlotIndex;

        public MaterialSlotBinding(Renderer renderer, int slotIndex)
        {
            Renderer = renderer;
            SlotIndex = slotIndex;
        }
    }

    [Tooltip("达到该次数后触发压力板")]
    public int maxCount = 1;

    [Tooltip("当前累计次数")]
    public int currentCount = 0;

    [Header("Plate 设置")]
    public float plateMoveDistance = 1f;
    public float plateMoveSpeed = 2f;
    public Renderer targetRenderer;
    public List<Material> countMaterials = new List<Material>();

    [Tooltip("此压力板触发后打开的门")]
    public DoorController linkedDoor;

    [HideInInspector]
    public bool isPressed = false;
    private Vector3 initialPosition;
    private readonly List<MaterialSlotBinding> materialBindings = new List<MaterialSlotBinding>();

    private void Awake()
    {
        CacheMaterialBindings();

        initialPosition = transform.position;
        RefreshMaterial();
    }

    public void AddCount()
    {
        if (isPressed)
            return;

        currentCount++;
        //Debug.Log($"Plate: {name} count {currentCount}/{maxCount}");
        RefreshMaterial();

        if (currentCount >= maxCount)
        {
            TriggerPlate();
        }
    }

    private void TriggerPlate()
    {
        if (isPressed)
            return;

        isPressed = true;
        MusicAudioManager.Instance?.PlaySfx("plate");
        RefreshMaterial();
        StartCoroutine(MovePlate(Vector3.down * plateMoveDistance));

        if (linkedDoor != null)
        {
            linkedDoor.Open();
        }
    }

    private System.Collections.IEnumerator MovePlate(Vector3 offset)
    {
        Vector3 start = transform.position;
        Vector3 end = start + offset;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * plateMoveSpeed;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        transform.position = end;
    }

    private void RefreshMaterial()
    {
        Material targetMaterial = ResolveCurrentCountMaterial();
        if (targetMaterial == null)
            return;

        if (materialBindings.Count == 0)
            CacheMaterialBindings();

        foreach (MaterialSlotBinding binding in materialBindings)
        {
            if (binding.Renderer == null)
                continue;

            Material[] materials = binding.Renderer.sharedMaterials;
            if (materials == null || binding.SlotIndex < 0 || binding.SlotIndex >= materials.Length)
                continue;

            if (materials[binding.SlotIndex] == targetMaterial)
                continue;

            materials[binding.SlotIndex] = targetMaterial;
            binding.Renderer.sharedMaterials = materials;
        }
    }

    public void ResetPlate()
    {
        currentCount = 0;
        isPressed = false;
        StopAllCoroutines();
        transform.position = initialPosition;
        RefreshMaterial();
    }

    private Material ResolveCurrentCountMaterial()
    {
        if (countMaterials == null || countMaterials.Count == 0)
            return null;

        int remainingCount = Mathf.Max(0, maxCount - currentCount);
        int materialIndex = Mathf.Clamp(remainingCount, 0, countMaterials.Count - 1);
        return countMaterials[materialIndex];
    }

    private void CacheMaterialBindings()
    {
        materialBindings.Clear();

        if (targetRenderer != null)
        {
            AddFallbackBinding(targetRenderer);
            return;
        }

        Material expectedMaterial = ResolveCurrentCountMaterial();
        if (expectedMaterial != null)
        {
            Renderer[] childRenderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in childRenderers)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == expectedMaterial)
                        materialBindings.Add(new MaterialSlotBinding(renderer, i));
                }
            }
        }

        if (materialBindings.Count > 0)
            return;

        targetRenderer = GetComponentInChildren<Renderer>();
        AddFallbackBinding(targetRenderer);
    }

    private void AddFallbackBinding(Renderer renderer)
    {
        if (renderer == null)
            return;

        Material[] materials = renderer.sharedMaterials;
        if (materials == null || materials.Length == 0)
            return;

        materialBindings.Add(new MaterialSlotBinding(renderer, 0));
    }
}
