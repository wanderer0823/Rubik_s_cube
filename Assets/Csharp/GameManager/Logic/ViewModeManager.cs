using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static InitCubeSlot;

public class ViewModeManager : MonoBehaviour
{
    public static ViewModeManager Instance;
    private GameState gs;
    public GameObject player;
    [Header("小球")]
    public GameObject ball_p;
    public Transform ball;
    public float minBallSpeed = 0.02f;
    private Rigidbody rb;
    [Header("空间系统引用")]
    public Transform cubeRoot;
    public InitCubeSlot cubeData;
    [Header("房间旋转用的时间")]
    [SerializeField] private float RotationTime=5f;


    //欧添加：在更新旋转后重置小球位置

    private Quaternion newRotation = Quaternion.Euler(360, 360, 360);
    private Quaternion lastRoomRotation = Quaternion.Euler(360, 360, 360);

    private void ResetPlayerToStartPosition()
    {
        if (player == null)
            return;

        PlayerAction playerAction = player.GetComponent<PlayerAction>();
        if (playerAction == null)
            return;

        playerAction.ResetToStartPosition();
    }

    void Awake()
    {
        Instance = this;
        if (GameState.Instance == null)
            new GameState();

        gs = GameState.Instance;
        //rb = ball_p.GetComponent<Rigidbody>();
    }
    public void OnEnable()
    {
        //订阅GM请求事件
        GameEvents.OnTabRequest += CheckTab;
        GameEvents.OnMoveRequest += CheckMove;
        GameEvents.OnViewSwitchRequest += CheckViewSwitch;
        //GameEvents.OnOpenDoorRequest += CheckOpenDoor;
        GameEvents.OnRotateRequest += CheckRotate;
        GameEvents.OnRotateFinishRequest += CheckRotateFinish;
        GameEvents.OnGameExitRequest += CheckExitGame;
        GameEvents.OnMouseLookRequest += CheckMouseMove; //欧
        //订阅UIM请求事件
        GameEvents.OnDirectViewSwitchRequest += CheckDirectViewSwitch;
        GameEvents.OnArrowsClickRequest += CheckArrowsClick;  //张天姿
        //订阅CRC请求事件
        //GameEvents.OnBallSpaceUpdateRequest += CheckBallSpaceUpdate;
        //新增
        GameEvents.OnInteractRequest += CheckInteract;
        GameEvents.OnScrollRequest += CheckScroll;
        GameEvents.OnMatChangeRequest += CheckMatChange;
        GameEvents.OnViewSwitchExecute += OnViewSwitch;
    }

    public void OnDisable()
    {
        //取消订阅
        GameEvents.OnTabRequest -= CheckTab;
        GameEvents.OnMoveRequest -= CheckMove;
        GameEvents.OnViewSwitchRequest -= CheckViewSwitch;
        //GameEvents.OnOpenDoorRequest -= CheckOpenDoor;
        GameEvents.OnRotateRequest -= CheckRotate;
        GameEvents.OnRotateFinishRequest -= CheckRotateFinish;
        GameEvents.OnGameExitRequest -= CheckExitGame;
        GameEvents.OnMouseLookRequest -= CheckMouseMove; //欧
        //UIM请求事件
        GameEvents.OnDirectViewSwitchRequest -= CheckDirectViewSwitch;
        GameEvents.OnArrowsClickRequest -= CheckArrowsClick;  //张天姿
        //订阅CRC请求事件
        //GameEvents.OnBallSpaceUpdateRequest -= CheckBallSpaceUpdate;
        //新增
        GameEvents.OnInteractRequest -= CheckInteract;
        GameEvents.OnScrollRequest -= CheckScroll;
        GameEvents.OnMatChangeRequest -= CheckMatChange;
        GameEvents.OnViewSwitchExecute -= OnViewSwitch;
    }

    /// <summary> 邻居预加载接口：在 View3 切换或开门转场时调用 RoomPreloadController.ExecutePreload() </summary>
    public void RequestNeighborPreload()
    {
        var rpc = GameManager.Instance?.roomPreloadSystem;
        if (rpc != null) rpc.ExecutePreload();
    }

    #region 用GS检查当前全局状态
    bool CheckViewMode(ViewMode mode)
    {
        if (gs.CurrentView == mode)
            return true;
        else return false;
    }

    bool CheckPlayerState(PlayerState playerState)
    {
        if (gs.CurrentPlayerState ==playerState)
            return true;
        else return false;
    }
    #endregion

    #region ============================================
    #region 监听订阅函数
    void CheckTab()
    {
        if (gs.IsBagOpen)
        {
            // 背包已开 → 关闭
            gs.CloseBag();
            GameEvents.onBagCloseExecute();
            /*__DEBUGTOOL_START__*/Debug.Log("VMM: 背包关闭");/*__DEBUGTOOL_END__*/
        }
        else
        {
            // 背包未开 → 检查是否允许打开
            if (!CheckPlayerState(PlayerState.isMoving)
                && !CheckPlayerState(PlayerState.turningFinished)
                && !CheckPlayerState(PlayerState.rotatingFinished))
                return;

            gs.OpenBag();
            GameEvents.onBagOpenExecute();
            /*__DEBUGTOOL_START__*/Debug.Log("VMM: 背包打开");/*__DEBUGTOOL_END__*/
        }
    }

    void CheckMove(Vector3 moveDir)
    {
        if (!CheckViewMode(ViewMode.View3))
            return;
        // 移动时自动关背包
        if (CheckPlayerState(PlayerState.isOpeningBag) && moveDir != Vector3.zero)
        {
            gs.CloseBag();
            GameEvents.onBagCloseExecute();
        }

        if (!CheckPlayerState(PlayerState.isMoving)&&!CheckPlayerState(PlayerState.isGrabbing))
            return;

        GameEvents.onMoveExecute(moveDir);
    }

    void CheckViewSwitch()//F
    {
        // 背包打开时先关背包再切视角
        if (CheckPlayerState(PlayerState.isOpeningBag))
        {
            gs.CloseBag();
            GameEvents.onBagCloseExecute();
        }

        /*__DEBUGTOOL_START__*/Debug.Log("VMM：开始检查F状态");/*__DEBUGTOOL_END__*/
        if (!CheckPlayerState(PlayerState.rotatingFinished)
            && !CheckPlayerState(PlayerState.turningFinished)
            && !CheckPlayerState(PlayerState.isMoving))
        {
            /*__DEBUGTOOL_START__*/Debug.Log("VMM：PlayerState状态不能切换视角！");/*__DEBUGTOOL_END__*/
            return;
        }
        gs.FSetView();
    }

    void CheckDirectViewSwitch(ViewMode targetMode)
    {
        // 背包打开时先关背包再切视角
        if (CheckPlayerState(PlayerState.isOpeningBag))
        {
            gs.CloseBag();
            GameEvents.onBagCloseExecute();
        }
        if (!CheckPlayerState(PlayerState.rotatingFinished)
            && !CheckPlayerState(PlayerState.turningFinished)
            && !CheckPlayerState(PlayerState.isMoving))
            return;
        //更新view mode
        gs.SetView(targetMode);
    }
    #region 封装旋转CurrentRoom方法
    private void OnViewSwitch(ViewMode mode)
    {
        if (mode != ViewMode.View3)
            return;
        RotateCurrentRoom();
    }
    private bool isRotating = false;  // 防止旋转过程中再次触发

private void RotateCurrentRoom()
{
    if (isRotating) return;  // 正在旋转，忽略本次调用

    GameObject currentRoom = cubeData.CurrentRoom;

        // 1. 计算目标旋转（与原来相同）
    Quaternion rotation_R = Quaternion.FromToRotation(
    CubeRotateController.CurrentGDirinMF,
    new Vector3(0, -1, 0));
    Quaternion qStart = Quaternion.Euler(270, 0, 0);
    GameObject pieceObj = cubeData.GetPieceGameObjectByRoomID(gs.CurrentRoomID);
    Quaternion qEnd = pieceObj.transform.localRotation;
    if (pieceObj.transform.parent == cubeRoot)
    {
        qEnd = pieceObj.transform.localRotation;
    }
    Quaternion rotation_T = qEnd * Quaternion.Inverse(qStart);
    Quaternion targetRotation = rotation_R * rotation_T;

    // 2. 启动平滑旋转协程
    StartCoroutine(RotateOverTime(currentRoom.transform, targetRotation, RotationTime)); // 0.5秒完成旋转
}

private IEnumerator RotateOverTime(Transform targetTransform, Quaternion targetRotation, float duration)
{
    isRotating = true;
    Quaternion startRotation = targetTransform.rotation;
    float elapsed = 0f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;  // 0→1
        // 使用Slerp保证旋转路径最短
        targetTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
        yield return null;
    }

    // 确保最终精确到达目标旋转
    targetTransform.rotation = targetRotation;
    isRotating = false;

    // 旋转完成后的处理（原来写在RotateCurrentRoom末尾的逻辑）
    newRotation = targetTransform.rotation;
    if (Quaternion.Angle(lastRoomRotation, newRotation) > 0f)
    {
        //ResetPlayerToStartPosition();
        lastRoomRotation = newRotation;
    }
}
    #endregion

    void CheckRotate(RotateType type)//left right
    {
        if (!CheckViewMode(ViewMode.View2)
            || !CheckPlayerState(PlayerState.rotatingFinished))
            return;
        gs.SetPlayerState(PlayerState.isRotating);
        if(type==RotateType.Left)
        {
            GameEvents.onCubeRotateStart();
        }
        if (type == RotateType.Right)
        {
            GameEvents.onCameraRotateStart();
        }
    }

    void CheckRotateFinish(RotateType type)
    {
        if (!CheckViewMode(ViewMode.View2)
            || !CheckPlayerState(PlayerState.isRotating))
            return;
        gs.SetPlayerState(PlayerState.rotatingFinished);
        if (type == RotateType.Left)
        {
            GameEvents.onCubeRotateEnd();
        }
        if (type == RotateType.Right)
        {
            GameEvents.onCameraRotateEnd();
        }
    }

    //欧：订阅GM的鼠标移动请求事件
    void CheckMouseMove(Vector2 mouseMove)
    {
        if (!CheckViewMode(ViewMode.View3))
            return;
        if(!CheckPlayerState(PlayerState.isMoving)
            &&!CheckPlayerState(PlayerState.isGrabbing))
            return;
        GameEvents.onMouseLookExecute(mouseMove);
    }

    //张天姿：订阅UIM的箭头请求事件
    void CheckArrowsClick(int number)
    {
        if (!CheckPlayerState(PlayerState.turningFinished)
            || !CheckViewMode(ViewMode.View2))
            return;
        //更新view mode
        gs.SetPlayerState(PlayerState.isTurning);
        GameEvents.onArrowsExecute(number);
    }
    // ===== yiu新增：E键交互 =====
    void CheckInteract()
    {
        // 仅 View3 + isMoving 时允许E交互
        if (!CheckViewMode(ViewMode.View3)
            || !CheckPlayerState(PlayerState.isMoving))
            return;
        GameEvents.onInteractExecute();
        /*__DEBUGTOOL_START__*/Debug.Log("VMM: E键交互执行");/*__DEBUGTOOL_END__*/
    }

    //esc退出游戏
    void CheckExitGame()
    {
        if(CheckPlayerState(PlayerState.isStartUI))
        {
            Application.Quit();
        }
        if (gs.IsBagOpen)
        {
            // 退出打开的背包
            gs.CloseBag();
            GameEvents.onBagCloseExecute();
            /*__DEBUGTOOL_START__*/Debug.Log("VMM: 背包关闭");/*__DEBUGTOOL_END__*/
        }
        else
        {
            gs.SetPlayerState(PlayerState.isStartUI);
            GameEvents.onBackStartUI();
        }
    }

    // ===== 新增：滚轮分流 =====
    void CheckScroll(float delta)
    {
        if (CheckPlayerState(PlayerState.isOpeningBag))
        {
            // 背包打开时 → 背包滚动
            GameEvents.onBagScrollExecute(delta);
        }
        else if (CheckPlayerState(PlayerState.isGrabbing)
                 && CheckViewMode(ViewMode.View3))
        {
            // 举起物体时 → 旋转物体
            GameEvents.onGrabRotateExecute(delta);
        }
        // 其他状态下滚轮无效
    }

    // ===== 新增：背包内材质切换 =====
    void CheckMatChange(PlayerMatState targetMat)
    {
        if (!CheckPlayerState(PlayerState.isOpeningBag))
            return;
        gs.SetMatState(targetMat);
        GameEvents.onMatChangeExecute(targetMat);
        /*__DEBUGTOOL_START__*/Debug.Log("VMM: 材质切换为 " + targetMat);/*__DEBUGTOOL_END__*/
    }

    #endregion
    #endregion

    #region ============================================
    #region 控制小球物理状态（删除）
    //订阅CRC请求事件
    /*void CheckBallSpaceUpdate(Vector3 ballPos)
    {
            //计算并更新小球空间位置的全局状态
            //Debug.Log("301");
            var surface =
                BallLocationService.CalculateSurface(
                    cubeRoot,
                    cubeData,
                    ballPos
                );
            if (surface == null)
                return;
            //Debug.Log("302");
            gs.SetCurrentSurface(surface);
            Vector3 localDown =
                cubeRoot.InverseTransformDirection(Vector3.down);

            //改变新重力方向在相对坐标系中的矢量
            FaceDir gravityFace =
                BallLocationService.CalculateGravityFace(localDown);
            gs.SetGravityFace(gravityFace);

            Debug.Log($"VMM更新空间 → Room:{surface.roomID}");
    }*/
    #endregion
    #endregion
}
