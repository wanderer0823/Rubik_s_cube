using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挂在 Plate 根物体上，负责记录触发计数、切换材质，并在达到阈值后压板开门。
/// </summary>
public class Plate : MonoBehaviour
{
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

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

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
        if (targetRenderer == null || countMaterials == null || countMaterials.Count == 0)
            return;

        int remainingCount = Mathf.Max(0, maxCount - currentCount);
        int materialIndex = Mathf.Clamp(remainingCount, 0, countMaterials.Count - 1);
        Material targetMaterial = countMaterials[materialIndex];

        if (targetMaterial != null)
            targetRenderer.material = targetMaterial;
    }

    public void ResetPlate()
    {
        currentCount = 0;
        isPressed = false;
        StopAllCoroutines();
        transform.position = initialPosition;
        RefreshMaterial();
    }
}
