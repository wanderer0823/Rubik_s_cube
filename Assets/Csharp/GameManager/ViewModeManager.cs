using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static InitCubeSlot;

public class ViewModeManager : MonoBehaviour
{
    private GameState gs;
    [Header("空间系统引用")]
    public Transform cubeRoot;
    public InitCubeSlot cubeData;

    void Awake()
    {
        if (GameState.Instance == null)
            new GameState();

        gs = GameState.Instance;
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
        //订阅UIM请求事件
        GameEvents.OnDirectViewSwitchRequest += CheckDirectViewSwitch;
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
        //UIM请求事件
        GameEvents.OnDirectViewSwitchRequest -= CheckDirectViewSwitch;
        //订阅CRC请求事件
        GameEvents.OnBallSpaceUpdateRequest -= CheckBallSpaceUpdate;
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

    void CheckMove()
    {
        if (!CheckViewMode(ViewMode.View3)
            ||!CheckPlayerState(PlayerState.isMoving) )
            return;
        GameEvents.onMoveExecute(); 
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

    //订阅CRC请求事件
    void CheckBallSpaceUpdate(Vector3 ballPos)
    {
        Debug.Log("301");
        var surface =
            BallLocationService.CalculateSurface(
                cubeRoot,
                cubeData,
                ballPos
            );

        if (surface == null)
            return;
        Debug.Log("302");
        // 更新 GS
        gs.SetCurrentSurface(surface);
        Debug.Log("303");
        Vector3 localDown =
            cubeRoot.InverseTransformDirection(Vector3.down);

        FaceDir gravityFace =
            BallLocationService.CalculateGravityFace(localDown);

        gs.SetGravityFace(gravityFace);

        Debug.Log($"VMM更新空间 → Room:{surface.roomID}");
    }
    #endregion
    #endregion
}