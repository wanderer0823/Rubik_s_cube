using UnityEngine;

/// <summary>
/// 可抓取标记。挂在场景中可被准星举起的物体上。
/// 物体需要有 Collider（用于射线检测）。
/// 如果需要释放后自由下落，还需要 Rigidbody。
/// </summary>
public class Grabbable : MonoBehaviour
{
    [Header("说明（仅编辑器提示）")]
    [Tooltip("此物体可被玩家准星举起")]
    public string description = "可交互物体";
}
