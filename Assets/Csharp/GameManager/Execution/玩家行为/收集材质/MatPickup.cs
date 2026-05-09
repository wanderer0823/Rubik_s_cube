using UnityEngine;

/// <summary>
/// 材质拾取：玩家在 View3 靠近后按 E 获取材质到背包。
/// 挂在场景中材质道具 prefab 上。
/// </summary>
public class MatPickup : MonoBehaviour
{
    [Header("材质类型")]
    public PlayerMatState matType;

    [Header("背包显示信息")]
    public string displayName;
    [TextArea(2, 4)]
    public string description;
    public Sprite detailImage;

    [Header("拾取设置")]
    public float pickupRange = 3f;

    private bool isPickedUp = false;

    void OnEnable()
    {
        GameEvents.OnInteractExecute += TryPickup;
    }

    void OnDisable()
    {
        GameEvents.OnInteractExecute -= TryPickup;
    }

    void TryPickup()
    {
        if (isPickedUp) return;

        Transform ball = ViewModeManager.Instance?.ball;
        if (ball == null) return;

        float dist = Vector3.Distance(ball.position, transform.position);
        if (dist > pickupRange) return;

        isPickedUp = true;

        // 添加到背包
        var backpack = FindObjectOfType<BackpackSystem>();
        if (backpack != null)
        {
            backpack.AddMat(matType, displayName, description, detailImage);
        }

        Debug.Log($"拾取材质：{displayName}");
        gameObject.SetActive(false);
    }
}
