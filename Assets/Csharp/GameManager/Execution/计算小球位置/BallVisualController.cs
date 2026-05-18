using UnityEngine;
using static InitCubeSlot;

/// <summary>
/// 控制魔方内小球的视觉位置。
/// 小球始终作为当前 CubePiece 的子物体。
/// 只在玩家真正过门换房间时重新定位。
/// </summary>
public class BallVisualController : MonoBehaviour
{
    [Header("视觉偏移（魔方插槽深度）")]
    public float surfaceOffset = 0.008f;

    private InitCubeSlot cubeData;

    void Start()
    {
        cubeData = ViewModeManager.Instance?.cubeData;
        // 初始定位
        PositionBall(GameState.Instance.CurrentRoomID);
    }

    void OnEnable()
    {
        GameEvents.OnRoomTransitionExecute += PositionBall;
    }

    void OnDisable()
    {
        GameEvents.OnRoomTransitionExecute -= PositionBall;
    }

    /// <summary>
    /// 将小球移动到指定房间对应的 Piece 下
    /// </summary>
    void PositionBall(int roomID)
    {
        if (cubeData == null) return;
        if (roomID < 0) return;

        CubeSurface_s surface = FindSurfaceByRoomID(roomID);
        if (surface == null)
        {
            Debug.LogWarning($"BallVisual: 找不到 RoomID={roomID} 对应的 Surface");
            return;
        }

        GameObject pieceObj = cubeData.GetPieceGameObjectByRoomID(roomID);
        if (pieceObj == null)
        {
            Debug.LogWarning($"BallVisual: 找不到 RoomID={roomID} 对应的 PieceObj");
            return;
        }

        transform.SetParent(pieceObj.transform, false);

        // 逻辑方向 → 魔方坐标系世界方向 → Piece本地方向
        Transform cubeRoot = ViewModeManager.Instance.cubeRoot;
        Vector3 logicDir = FaceDirToLocalVector(surface.dir);
        Vector3 worldDir = cubeRoot.TransformDirection(logicDir);
        Vector3 pieceLocalDir = pieceObj.transform.InverseTransformDirection(worldDir).normalized;

        transform.localPosition = pieceLocalDir * surfaceOffset;

    }

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

    /// <summary>
    /// 获取指定房间 Surface 在世界空间的朝外方向（供外部使用）
    /// </summary>
    public static Vector3 GetSurfaceWorldDirection(int roomID)
    {
        var cubeData = ViewModeManager.Instance?.cubeData;
        var cubeRoot = ViewModeManager.Instance?.cubeRoot;
        if (cubeData == null || cubeRoot == null) return Vector3.up;

        // 找 Surface
        CubeSurface_s surface = null;
        foreach (var slot in cubeData.slots)
        {
            if (slot.occupant == null) continue;
            foreach (var s in slot.occupant.surfaces)
            {
                if (s.roomID == roomID)
                {
                    surface = s;
                    break;
                }
            }
            if (surface != null) break;
        }

        if (surface == null) return Vector3.up;

        Vector3 logicDir = surface.dir switch
        {
            FaceDir.Up => Vector3.up,
            FaceDir.Down => Vector3.down,
            FaceDir.Left => Vector3.left,
            FaceDir.Right => Vector3.right,
            FaceDir.Front => Vector3.forward,
            FaceDir.Back => Vector3.back,
            _ => Vector3.up
        };

        return cubeRoot.TransformDirection(logicDir).normalized;
    }

}
