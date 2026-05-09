using UnityEngine;

/// <summary>
/// 线索拾取：挂在场景中线索 prefab 上。
/// 玩家在 View3 靠近后按 E 拾取，永久保存到背包。
/// </summary>
public class CluePickup : MonoBehaviour
{
    [Header("线索信息")]
    public string clueID;           // 唯一标识
    public string clueName;         // 显示名称
    [TextArea(2, 5)]
    public string description;      // 描述文本
    public Sprite detailImage;      // 详情图片（可选）

    [Header("拾取设置")]
    public float pickupRange = 3f;  // 拾取距离

    private bool isPickedUp = false;

    void OnEnable()
    {
        GameEvents.OnInteractExecute += TryPickup;
    }

    void OnDisable()
    {
        GameEvents.OnInteractExecute -= TryPickup;
    }

    private void TryPickup()
    {
        if (isPickedUp) return;

        // 检查玩家是否在拾取范围内
        Transform ball = ViewModeManager.Instance?.ball;
        if (ball == null) return;

        float dist = Vector3.Distance(ball.position, transform.position);
        if (dist > pickupRange) return;

        // 检查是否已收集过
        var gs = GameState.Instance;
        if (gs == null || gs.HasClue(clueID)) return;

        // 收集线索
        gs.CollectClue(clueID);
        isPickedUp = true;

        // 添加到背包UI
        var backpack = FindObjectOfType<BackpackSystem>();
        if (backpack != null)
        {
            backpack.AddClue(clueID, clueName, description, detailImage);
        }

        Debug.Log($"拾取线索：{clueName}");

        // 拾取后隐藏物体（不销毁，因为房间不会被销毁）
        gameObject.SetActive(false);
    }
}
