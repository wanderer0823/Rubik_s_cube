using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static InitCubeSlot;

public class ViewModeManager : MonoBehaviour
{
    public static ViewModeManager Instance;
    private GameState gs;
    [Header("小球")]
    public GameObject ball_p;
    public Transform ball;
    public float minBallSpeed = 0.02f;
    private Rigidbody rb;
    [Header("空间系统引用")]
    public Transform cubeRoot;
    public InitCubeSlot cubeData;

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
            Debug.Log("VMM: 背包关闭");
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
            Debug.Log("VMM: 背包打开");
        }
    }

    void CheckMove(Vector3 moveDir)
    {
        if (!CheckViewMode(ViewMode.View3))
            return;

        // 移动时自动关背包
        if (CheckPlayerState(PlayerState.isOpeningBag))
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

        if (!CheckPlayerState(PlayerState.rotatingFinished)
            && !CheckPlayerState(PlayerState.turningFinished)
            && !CheckPlayerState(PlayerState.isMoving))
            return;

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
    private void RotateCurrentRoom()
    {
        Quaternion rotation_R =
            Quaternion.FromToRotation(
                CubeRotateController.CurrentGDirinMF,
                new Vector3(0, -1, 0));
        Debug.Log("地球："+ CubeRotateController.CurrentGDirinMF);

        Quaternion qStart = Quaternion.Euler(270, 0, 0);

        Transform cubeRoot = ViewModeManager.Instance.cubeRoot;

        GameObject pieceObj =
            cubeData.GetPieceGameObjectByRoomID(gs.CurrentRoomID);

        Debug.Log($"旋转计算: CurrentGDirinMF={CubeRotateController.CurrentGDirinMF}, RoomID={gs.CurrentRoomID}, pieceObj={pieceObj?.name}");

        Quaternion qEnd =
            cubeData.GetPieceGameObjectByRoomID(gs.CurrentRoomID)
            .transform.localRotation;

        if (pieceObj.transform.parent == cubeRoot)
        {
            qEnd = pieceObj.transform.localRotation;
        }

        Debug.Log("可恶这是什么" + qEnd.eulerAngles);

        Quaternion rotation_T =
            qEnd * Quaternion.Inverse(qStart);

        Quaternion rotation =
            rotation_R * rotation_T;

        GameObject currentRoom = cubeData.CurrentRoom;

        Debug.Log($"旋转计算: currentRoom={currentRoom?.name}, rotation_R={rotation_R.eulerAngles}, rotation_T={rotation_T.eulerAngles}, final={rotation.eulerAngles}");

        Debug.Log($"旋转前: currentRoom.rotation={currentRoom.transform.rotation.eulerAngles}");

        currentRoom.transform.rotation = rotation;

        //currentRoom.transform.GetChild(0).localRotation =
            Quaternion.Euler(cubeData.rooms[gs.CurrentRoomID].orRotation);
    }
    #endregion

    void CheckRotate(RotateType type)//left right
    {
        //Debug.Log("100");
        if (!CheckViewMode(ViewMode.View2)
            || !CheckPlayerState(PlayerState.rotatingFinished))
            return;
        //Debug.Log("101");
        gs.SetPlayerState(PlayerState.isRotating);
        if(type==RotateType.Left)
        {
            //Debug.Log("102");
            GameEvents.onCubeRotateStart();
        }
        if (type == RotateType.Right)
        {
            //Debug.Log("103");
            GameEvents.onCameraRotateStart();
        }
    }

    void CheckRotateFinish(RotateType type)
    {
        //Debug.Log("200");
        if (!CheckViewMode(ViewMode.View2)
            || !CheckPlayerState(PlayerState.isRotating))
            return;
        //Debug.Log("201");
        gs.SetPlayerState(PlayerState.rotatingFinished);
        if (type == RotateType.Left)
        {
           // Debug.Log("202");
            GameEvents.onCubeRotateEnd();
        }
        if (type == RotateType.Right)
        {
            //Debug.Log("203");
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
            || !CheckViewMode(ViewMode.View1))
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
        Debug.Log("VMM: E键交互执行");
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
        Debug.Log("VMM: 材质切换为 " + targetMat);
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