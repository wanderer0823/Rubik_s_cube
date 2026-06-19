using UnityEngine;
using static InitCubeSlot;

/// <summary>
/// ����ħ����С����Ӿ�λ�á�
/// С��ʼ����Ϊ��ǰ CubePiece �������塣
/// ֻ������������Ż�����ʱ���¶�λ��
/// </summary>
public class BallVisualController : MonoBehaviour
{
    [Header("�Ӿ�ƫ�ƣ�ħ�������ȣ�")]
    public float surfaceOffset = 0.008f;
    public Transform playerTrans;

    private InitCubeSlot cubeData;
    private Vector3 currentWorldNormal = Vector3.up;

    void Start()
    {
        cubeData = ViewModeManager.Instance?.cubeData;
        // ��ʼ��λ
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
    /// ��С���ƶ���ָ�������Ӧ�� Piece ��
    /// </summary>
    void PositionBall(int roomID)
    {
        if (cubeData == null) return;
        if (roomID < 0) return;

        CubeSurface_s surface = FindSurfaceByRoomID(roomID);
        if (surface == null)
        {
            /*__DEBUGTOOL_START__*/Debug.LogWarning($"BallVisual: �Ҳ��� RoomID={roomID} ��Ӧ�� Surface");/*__DEBUGTOOL_END__*/
            return;
        }

        GameObject pieceObj = cubeData.GetPieceGameObjectByRoomID(roomID);
        if (pieceObj == null)
        {
            /*__DEBUGTOOL_START__*/Debug.LogWarning($"BallVisual: �Ҳ��� RoomID={roomID} ��Ӧ�� PieceObj");/*__DEBUGTOOL_END__*/
            return;
        }

        transform.SetParent(pieceObj.transform, false);

        // �߼����� �� ħ������ϵ���緽�� �� Piece���ط���
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
        // 只更新旋转，不改变位置
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
            // 没有玩家时只对齐法线
            transform.up = currentWorldNormal;
        }
    }
    
    // 把原来PositionBall里的旋转逻辑抽取成ApplyRotation，避免重复代码
    private void ApplyRotation()
    {
        // 使用currentWorldNormal
        Transform playerTrans = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTrans != null)
        {
            Quaternion baseRotation = Quaternion.FromToRotation(Vector3.up, currentWorldNormal);
            transform.rotation = baseRotation * playerTrans.rotation;
        }
        else
        {
            transform.up = currentWorldNormal;
        }
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
    /// ��ȡָ������ Surface ������ռ�ĳ��ⷽ�򣨹��ⲿʹ�ã�
    /// </summary>
    public static Vector3 GetSurfaceWorldDirection(int roomID)
    {
        var cubeData = ViewModeManager.Instance?.cubeData;
        var cubeRoot = ViewModeManager.Instance?.cubeRoot;
        if (cubeData == null || cubeRoot == null) return Vector3.up;

        // �� Surface
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
