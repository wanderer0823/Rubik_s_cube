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
        rb = ball_p.GetComponent<Rigidbody>();
    }
    public void OnEnable()
    {
        //订阅GM请求事件
        GameEvents.OnTabRequest += CheckTab;
        GameEvents.OnMoveRequest += CheckMove;
        GameEvents.OnViewSwitchRequest += CheckViewSwitch;
        GameEvents.OnOpenDoorRequest += CheckOpenDoor;
        GameEvents.OnRotateRequest += CheckRotate;
        GameEvents.OnRotateFinishRequest += CheckRotateFinish;
        GameEvents.OnMouseLookRequest += CheckMouseMove; //欧
        //订阅UIM请求事件
        GameEvents.OnDirectViewSwitchRequest += CheckDirectViewSwitch;
        GameEvents.OnArrowsClickRequest += CheckArrowsClick;  //张天姿
        //订阅CRC请求事件
        GameEvents.OnBallSpaceUpdateRequest += CheckBallSpaceUpdate;

        Debug.Log("VMM:初始化完成。");
    }

    public void OnDisable()
    {
        //取消订阅
        GameEvents.OnTabRequest -= CheckTab;
        GameEvents.OnMoveRequest -= CheckMove;
        GameEvents.OnViewSwitchRequest -= CheckViewSwitch;
        GameEvents.OnOpenDoorRequest -= CheckOpenDoor;
        GameEvents.OnRotateRequest -= CheckRotate;
        GameEvents.OnRotateFinishRequest -= CheckRotateFinish;
        GameEvents.OnMouseLookRequest -= CheckMouseMove; //欧
        //UIM请求事件
        GameEvents.OnDirectViewSwitchRequest -= CheckDirectViewSwitch;
        GameEvents.OnArrowsClickRequest -= CheckArrowsClick;  //张天姿
        //订阅CRC请求事件
        GameEvents.OnBallSpaceUpdateRequest -= CheckBallSpaceUpdate;
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
        GameEvents.onTabExecute();
    }

    void CheckMove(Vector3 moveDir)
    {
        if (!CheckViewMode(ViewMode.View3)
            ||!CheckPlayerState(PlayerState.isMoving) )
            return;
        GameEvents.onMoveExecute(moveDir); 
    }

    void CheckViewSwitch()//F
    {
        if (!CheckPlayerState(PlayerState.rotatingFinished)
            && !CheckPlayerState(PlayerState.turningFinished)
            && !CheckPlayerState(PlayerState.isMoving))
            return;
        //更新view mode
        gs.FSetView();

        GameEvents.onViewSwitchExecute(gs.CurrentView);
    }

    void CheckOpenDoor()//E
    {
        if (!CheckViewMode(ViewMode.View3)
            || !CheckPlayerState(PlayerState.isMoving))
            return;
        gs.SetPlayerState(PlayerState.isWaiting);
        GameEvents.onOpenDoorExecute();
    }

    void CheckDirectViewSwitch(ViewMode targetMode)
    {
        if (!CheckPlayerState(PlayerState.rotatingFinished)
            && !CheckPlayerState(PlayerState.turningFinished)
            && !CheckPlayerState(PlayerState.isMoving))
            return;
        //更新view mode
        gs.SetView(targetMode);

        GameEvents.onViewSwitchExecute(gs.CurrentView);
    }

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
            gs.SetBallPhysics(BallPhysics.On);
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
        if (!CheckViewMode(ViewMode.View3)
            || !CheckPlayerState(PlayerState.isMoving))
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


    #endregion
    #endregion

    #region ============================================
    #region 控制小球物理状态

    //订阅CRC请求事件
    void CheckBallSpaceUpdate(Vector3 ballPos)
    {
        //等待小球速度降低直到低于阈值才计算其空间位置
        StartCoroutine(CheckSpeed(minBallSpeed, (isOk) =>
        {
            Debug.Log("检测结果：" + isOk);
            if (isOk == false)
            {
                Debug.Log("VMM尝试计算失败。");
                return;
            }
            //锁定小球物理状态
            gs.SetBallPhysics(BallPhysics.Off);

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
        }));

        
    }

    IEnumerator CheckSpeed(float threshold, System.Action<bool> result)
    {
        while (rb.velocity.magnitude > threshold)
        {
            yield return null;
        }

        result?.Invoke(true);
    }

    #endregion
    #endregion
}