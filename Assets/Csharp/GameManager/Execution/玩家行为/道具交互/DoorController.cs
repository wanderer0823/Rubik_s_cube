using System;
using UnityEngine;
using System.Collections;

public class DoorController : MonoBehaviour
{
    // ==================== 门属性 ====================

    public enum DoorMat { Hard, Soft, normal,Open}

    [Header("初始化设定（不变）")]
    public DoorMat doorMat = DoorMat.Hard;
    public float softDoorHitSpeed = 5f;
    [Header("运行时状态")]
    [SerializeField] private bool _isOpened = false;
    [SerializeField] private int needPlateNum = 1;
    private int currentPlateNum = 0;

    [Header("开门动画设置（仅Hard门）")]
    [SerializeField] private float doorOpenAngle = 90f;
    [SerializeField] private float doorAnimSpeed = 2f;

    // 动画状态
    private Coroutine doorAnimCoroutine;

    public bool IsOpened => _isOpened;

    private void Awake()
    {
        _isOpened = needPlateNum == 0;
        //transform.GetChild(1).gameObject.SetActive(false);
    }

    public void Open()
    {
        if (_isOpened) return;
        currentPlateNum++;
        if (currentPlateNum >= needPlateNum)
        {
            _isOpened = true;
        }
        /*__DEBUGTOOL_START__*/Debug.Log($"门 {gameObject.name} 已打开（isOpened=true）");/*__DEBUGTOOL_END__*/
        // TODO: 播放开门动画
    }

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

    /// <summary>
    /// 打印当前门的完整状态
    /// </summary>
    public void LogDoorStatus()
    {
        /*__DEBUGTOOL_START__*/Debug.Log($"[门状态] {gameObject.name} | " +
                  $"DoorMat={doorMat} | " +
                  $"isOpened={_isOpened} | " +
                  $"isPassable={GetIsPassable()} | " +
                  $"DoorVector={DoorinRoomVector}");/*__DEBUGTOOL_END__*/
    }

    // ==================== 门向量计算（原 DoorVectorReturn 逻辑）====================

    [Header("门向量（运行时自动计算）")]
    public Vector3 DoorinRoomVector;
    //public Vector3 GinMF;

    void OnEnable()
    {
        GameEvents.OnInteractExecute += OpenNormalDoor;
    }

    void OnDisable()
    {
        GameEvents.OnInteractExecute -= OpenNormalDoor;
    }

    void Update()
    {
        ReturnDoorVector();
        CheckDoorVisualState();
    }

    void ReturnDoorVector()
    {
        if (transform.parent == null)
        {
            //Debug.Log("场景中有没挂好的门");
            return;

        }
        Vector3 parentPos = Vector3.zero;
        // dirReference = dir_door/left，其 localPosition = (-5,0,0)
        // dirReference.parent = dir_door
        // dirReference.parent.parent = prefab根节点
        Vector3 dir = transform.parent.localPosition;
        parentPos = transform.root.rotation * dir;

        Quaternion rotation = Quaternion.FromToRotation(
            new Vector3(0, -1, 0),
            CubeRotateController.CurrentGDirinMF
        );
        parentPos = rotation * parentPos.normalized;
        //GinMF = CubeRotateController.CurrentGDirinMF;

        float epsilon = 0.1f;
        if (Mathf.Abs(parentPos.x) < epsilon) parentPos.x = 0;
        if (Mathf.Abs(parentPos.y) < epsilon) parentPos.y = 0;
        if (Mathf.Abs(parentPos.z) < epsilon) parentPos.z = 0;

        DoorinRoomVector = parentPos;
    }

    void CheckDoorVisualState()
    {
        // 只有 Hard 门 + 已被压力板打开 才有开关动画
        if (doorMat != DoorMat.Hard || !_isOpened ) return;

        bool shouldBeOpen = GetIsPassable();

        //if (shouldBeOpen )
        {
            // 旋转打开
            MusicAudioManager.Instance.PlaySfx("opendoor");
            Animator animator = transform.GetComponentInChildren<Animator>();
            animator.SetBool("isOpen", true);
            StartCoroutine(WaitForOpening(1.0f));
        }

    }


    //欧
    void OpenNormalDoor()
    {
        if (doorMat != DoorMat.normal) return;
        Animator animator = transform.GetComponentInChildren<Animator>();
        animator.SetBool("isOpen", true);
        MusicAudioManager.Instance.PlaySfx("opendoor");
        StartCoroutine(WaitForOpening(1.0f));
    }
    private IEnumerator WaitForOpening(float delay)
    {
        yield return new WaitForSeconds(delay);
        transform.GetChild(0).gameObject.SetActive(true);  //开完后才出现传送碰撞体
    }
}
