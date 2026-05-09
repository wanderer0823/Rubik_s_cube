using UnityEngine;

/// <summary>
/// 门控制器：门属性 + 门向量计算（原 DoorVectorReturn 逻辑保留）。
/// 挂在每扇门的 GameObject 上。
/// </summary>
public class DoorController : MonoBehaviour
{
    // ==================== 门属性 ====================

    public enum DoorMat { Hard, Soft }

    [Header("初始化设定（不变）")]
    public DoorMat doorMat = DoorMat.Hard;
    public float softDoorHitSpeed = 5f;

    [Header("运行时状态")]
    [SerializeField] private bool _isOpened = false;

    public bool IsOpened => _isOpened;

    public void Open()
    {
        if (_isOpened) return;
        _isOpened = true;
        Debug.Log($"门 {gameObject.name} 已打开（isOpened=true）");
        // TODO: 播放开门动画
    }

    /// <summary>
    /// 查询此门方向的 isPassable（从 Room.FaceState 读取）
    /// </summary>
    public bool GetIsPassable()
    {
        var gs = GameState.Instance;
        if (gs == null) return false;

        int roomID = gs.CurrentRoomID;
        var cubeData = ViewModeManager.Instance?.cubeData;
        if (cubeData == null || roomID < 0 || roomID >= cubeData.rooms.Count)
            return false;

        var room = cubeData.rooms[roomID];
        Vector3Int doorDir = Vector3Int.RoundToInt(DoorinRoomVector);

        for (int i = 0; i < room.dirMap.Length; i++)
        {
            if (doorDir == InitCubeSlot.FaceOffset[room.dirMap[i]])
            {
                var face = room.GetFace(room.dirMap[i]);
                return face != null && face.isPassable;
            }
        }
        return false;
    }

    // ==================== 门向量计算（原 DoorVectorReturn，保留原逻辑）====================

    [Header("门向量（运行时自动计算）")]
    public Vector3 DoorinRoomVector;
    public Vector3 GinMF;

    void Update()
    {
        ReturnDoorVector();
    }

    /// <summary>
    /// 原作者逻辑，保留不变。
    /// </summary>
    void ReturnDoorVector()
    {
        // 安全检查：层级不够时跳过计算
        if (transform.parent == null
            || transform.parent.parent == null
            || transform.parent.parent.parent == null)
            return;

        Vector3 dir = transform.parent.localPosition;
        Vector3 parentPos = transform.parent.parent.parent.rotation * dir;

        Quaternion rotation = Quaternion.FromToRotation(
            new Vector3(0, -1, 0),
            CubeRotateController.CurrentGDirinMF
        );
        parentPos = rotation * parentPos.normalized;
        GinMF = CubeRotateController.CurrentGDirinMF;

        float epsilon = 0.1f;
        if (Mathf.Abs(parentPos.x) < epsilon) parentPos.x = 0;
        if (Mathf.Abs(parentPos.y) < epsilon) parentPos.y = 0;
        if (Mathf.Abs(parentPos.z) < epsilon) parentPos.z = 0;

        DoorinRoomVector = parentPos;
    }
}
