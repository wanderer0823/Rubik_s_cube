using UnityEngine;
using static InitCubeSlot;

/// <summary>
/// 控制魔方内小球的视觉位置。
/// 小球始终作为当前 CubePiece 的子物体，本地坐标由 Surface 方向决定。
/// </summary>
public class BallVisualController : MonoBehaviour
{
    [Header("视觉偏移（魔方插槽深度）")]
    public float surfaceOffset = 1.4f;

    private int lastRoomID = -1;
    private InitCubeSlot cubeData;

    void Start()
    {
        cubeData = ViewModeManager.Instance?.cubeData;
        // 初始定位
        UpdateBallPosition();
    }

    void OnEnable()
    {
        GameEvents.OnViewSwitchExecute += OnViewSwitch;
    }

    void OnDisable()
    {
        GameEvents.OnViewSwitchExecute -= OnViewSwitch;
    }

    void Update()
    {
        var gs = GameState.Instance;
        if (gs == null) return;

        // View1/2 下不重新定位（小球跟着Piece转就行）
        if (gs.CurrentView != ViewMode.View3) return;

        // 检测房间变化
        if (gs.CurrentRoomID != lastRoomID)
        {
            lastRoomID = gs.CurrentRoomID;
            UpdateBallPosition();
        }
    }

    void OnViewSwitch(ViewMode mode)
    {
        // 切视角时也刷新一次，确保位置正确
        //UpdateBallPosition();
    }

    /// <summary>
    /// 更新小球：设为对应 Piece 的子物体，本地坐标 = 面方向 × offset
    /// </summary>
    public void UpdateBallPosition()
    {
        var gs = GameState.Instance;
        if (gs == null || cubeData == null) return;

        int roomID = gs.CurrentRoomID;
        if (roomID < 0) return;

        // 找到该 roomID 对应的 Surface
        CubeSurface_s surface = FindSurfaceByRoomID(roomID);
        if (surface == null)
        {
            Debug.LogWarning($"BallVisualController: 找不到 RoomID={roomID} 对应的 Surface");
            return;
        }

        // 找到该 Surface 所属的 Piece 的 GameObject
        GameObject pieceObj = cubeData.GetPieceGameObjectByRoomID(roomID);
        if (pieceObj == null)
        {
            Debug.LogWarning($"BallVisualController: 找不到 RoomID={roomID} 对应的 PieceObj");
            return;
        }

        // 设为 Piece 子物体
        transform.SetParent(pieceObj.transform, false);

        // 本地坐标 = 面方向 × offset / 父物体缩放
        Vector3 localDir = FaceDirToLocalVector(surface.dir);
        float parentScale = pieceObj.transform.lossyScale.x; // 假设xyz等比缩放
        transform.localPosition = localDir * (surfaceOffset / parentScale);
    }

    /// <summary>
    /// 通过 roomID 找到对应的 CubeSurface_s
    /// </summary>
    CubeSurface_s FindSurfaceByRoomID(int roomID)
    {
        foreach (var slot in cubeData.slots)
        {
            if (slot.occupant == null) continue;
            foreach (var surface in slot.occupant.surfaces)
            {
                if (surface.roomID == roomID)
                    return surface;
            }
        }
        return null;
    }

    /// <summary>
    /// FaceDir 转本地方向向量
    /// </summary>
    Vector3 FaceDirToLocalVector(FaceDir dir)
    {
        return dir switch
        {
            FaceDir.Up => Vector3.up,
            FaceDir.Down => Vector3.down,
            FaceDir.Left => Vector3.left,
            FaceDir.Right => Vector3.right,
            FaceDir.Front => Vector3.forward,
            FaceDir.Back => Vector3.back,
            _ => Vector3.up
        };
    }
}
