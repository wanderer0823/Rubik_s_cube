using UnityEngine;
using static InitCubeSlot;

/// <summary>
/// 控制小球在 Cube 表面上的位置和旋转
/// 由 CubePiece 驱动，并随玩家朝向旋转
/// </summary>
public class BallVisualController : MonoBehaviour
{
    [Header("小球表面偏移量")]
    public float surfaceOffset = 0.008f;
    public Transform playerTrans;

    private InitCubeSlot cubeData;
    private Vector3 currentWorldNormal = Vector3.up;
    private int currentRoomID = -1;  // ★ 新增：记录当前房间ID

    void Start()
    {
        cubeData = ViewModeManager.Instance?.cubeData;
        // 初始化位置
        PositionBall(GameState.Instance.CurrentRoomID);
    }

    void OnEnable()
    {
        GameEvents.OnRoomTransitionExecute += PositionBall;
        GameEvents.OnViewSwitchExecute += OnViewSwitched;
    }

    void OnDisable()
    {
        GameEvents.OnRoomTransitionExecute -= PositionBall;
        GameEvents.OnViewSwitchExecute -= OnViewSwitched;
    }

    /// <summary>
    /// 将小球定位到指定房间所在的 Piece 表面
    /// </summary>
    void PositionBall(int roomID)
    {
        currentRoomID = roomID;  // ★ 新增：记录当前房间ID

        if (cubeData == null) return;
        if (roomID < 0) return;

        CubeSurface_s surface = FindSurfaceByRoomID(roomID);
        if (surface == null)
        {
            /*__DEBUGTOOL_START__*/Debug.LogWarning($"BallVisual: 找不到 RoomID={roomID} 的 Surface");/*__DEBUGTOOL_END__*/
            return;
        }

        GameObject pieceObj = cubeData.GetPieceGameObjectByRoomID(roomID);
        if (pieceObj == null)
        {
            /*__DEBUGTOOL_START__*/Debug.LogWarning($"BallVisual: 找不到 RoomID={roomID} 的 PieceObj");/*__DEBUGTOOL_END__*/
            return;
        }

        transform.SetParent(pieceObj.transform, false);

        // 计算表面法线的本地方向（相对于 Piece）
        Transform cubeRoot = ViewModeManager.Instance.cubeRoot;
        Vector3 logicDir = FaceDirToLocalVector(surface.dir);
        Vector3 worldDir = cubeRoot.TransformDirection(logicDir);
        Vector3 pieceLocalDir = pieceObj.transform.InverseTransformDirection(worldDir).normalized;

        transform.localPosition = pieceLocalDir * surfaceOffset;

        /*__DEBUGTOOL_START__*/Debug.Log($"BallVisual: Room={roomID}, Piece={pieceObj.name}, " +
                  $"FaceDir={surface.dir}, pieceLocalDir={pieceLocalDir}, localPos={transform.localPosition}");/*__DEBUGTOOL_END__*/

        currentWorldNormal = worldDir.normalized;
        // 应用旋转
        ApplyRotation();
    }
    
    void OnViewSwitched(ViewMode mode)
    {
        // 重新从当前的 surface 获取方向并更新 currentWorldNormal
        CubeSurface_s surface = FindSurfaceByRoomID(currentRoomID);
        if (surface != null)
        {
            Transform cubeRoot = ViewModeManager.Instance.cubeRoot;
            Vector3 logicDir = FaceDirToLocalVector(surface.dir);
            Vector3 worldDir = cubeRoot.TransformDirection(logicDir);
            currentWorldNormal = worldDir.normalized;
        }
        UpdateRotationOnly();
    }

    public void UpdateRotationOnly()
    {
        if (currentWorldNormal == Vector3.zero) return; // 未初始化

        Transform playerTrans = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTrans != null)
        {
            Quaternion baseRotation = Quaternion.FromToRotation(Vector3.up, currentWorldNormal);
            transform.rotation = baseRotation * playerTrans.rotation;
        }
        else
        {
            // 无玩家时只对齐法线
            transform.up = currentWorldNormal;
        }
    }
    
    // 应用旋转（调用 UpdateRotationOnly 避免重复代码）
    private void ApplyRotation()
    {
        UpdateRotationOnly();
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
    /// 获取某个房间所在表面的世界法线方向（静态方法，供外部调用）
    /// </summary>
    public static Vector3 GetSurfaceWorldDirection(int roomID)
    {
        var cubeData = ViewModeManager.Instance?.cubeData;
        var cubeRoot = ViewModeManager.Instance?.cubeRoot;
        if (cubeData == null || cubeRoot == null) return Vector3.up;

        // 查找 Surface
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