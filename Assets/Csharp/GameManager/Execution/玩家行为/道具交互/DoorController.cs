using System;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    // ==================== 门属性 ====================

    public enum DoorMat { Hard, Soft }

    [Header("初始化设定（不变）")]
    public DoorMat doorMat = DoorMat.Hard;
    public float softDoorHitSpeed = 5f;

    [Header("方向引用（从dir_door下拖入对应方向物体，如left）")]
    public Transform dirReference;

    [Header("运行时状态")]
    [SerializeField] private bool _isOpened = false;
    [SerializeField] private int needPlateNum = 1;
    private int currentPlateNum = 0;

    [Header("开门动画设置（仅Hard门）")]
    [SerializeField] private float doorOpenAngle = 90f;
    [SerializeField] private float doorAnimSpeed = 2f;
    [SerializeField] private Transform doorPivot;       // 门旋转轴物体（门模型本身或一个空父物体）

    // 动画状态
    private bool isVisuallyOpen = false;
    private Coroutine doorAnimCoroutine;

    public bool IsOpened => _isOpened;

    private void Awake()
    {
        _isOpened = needPlateNum == 0;
        transform.GetChild(1).gameObject.SetActive(false);
    }

    public void Open()
    {
        if (_isOpened) return;
        currentPlateNum++;
        if (currentPlateNum >= needPlateNum)
        {
            _isOpened = true;
        }
        Debug.Log($"门 {gameObject.name} 已打开（isOpened=true）");
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
        Debug.Log($"[门状态] {gameObject.name} | " +
                  $"DoorMat={doorMat} | " +
                  $"isOpened={_isOpened} | " +
                  $"isPassable={GetIsPassable()} | " +
                  $"DoorVector={DoorinRoomVector}");
    }

    // ==================== 门向量计算（原 DoorVectorReturn 逻辑）====================

    [Header("门向量（运行时自动计算）")]
    public Vector3 DoorinRoomVector;
    public Vector3 GinMF;

    void Update()
    {
        ReturnDoorVector();
        CheckDoorVisualState();
    }

    void ReturnDoorVector()
    {
        // 安全检查
        if (dirReference == null) return;
        if (dirReference.parent == null) return;

        // dirReference = dir_door/left，其 localPosition = (-5,0,0)
        // dirReference.parent = dir_door
        // dirReference.parent.parent = prefab根节点
        Vector3 dir = dirReference.localPosition;
        Vector3 parentPos = dirReference.parent.parent.rotation * dir;

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

    void CheckDoorVisualState()
    {
        // 只有 Hard 门 + 已被压力板打开 才有开关动画
        if (doorMat != DoorMat.Hard || !_isOpened || doorPivot == null) return;

        bool shouldBeOpen = GetIsPassable();

        if (shouldBeOpen && !isVisuallyOpen)
        {
            // 旋转打开
            isVisuallyOpen = true;
            if (doorAnimCoroutine != null) StopCoroutine(doorAnimCoroutine);
            doorAnimCoroutine = StartCoroutine(AnimateDoor(doorOpenAngle));
        }
        else if (!shouldBeOpen && isVisuallyOpen)
        {
            // 旋转关闭
            isVisuallyOpen = false;
            if (doorAnimCoroutine != null) StopCoroutine(doorAnimCoroutine);
            doorAnimCoroutine = StartCoroutine(AnimateDoor(0f));
        }
    }

    System.Collections.IEnumerator AnimateDoor(float targetAngle)
    {
        float currentAngle = doorPivot.localEulerAngles.y;
        // 处理角度环绕
        if (currentAngle > 180f) currentAngle -= 360f;

        while (Mathf.Abs(currentAngle - targetAngle) > 0.5f)
        {
            currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * doorAnimSpeed);
            doorPivot.localEulerAngles = new Vector3(0, currentAngle, 0);
            yield return null;
        }

        doorPivot.localEulerAngles = new Vector3(0, targetAngle, 0);
        doorAnimCoroutine = null;
    }

}
