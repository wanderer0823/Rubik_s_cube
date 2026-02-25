using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewModeManager : MonoBehaviour
{
    private GameState gs;
    public void OnEnable()
    {
        //初始化静态类
        gs=new GameState();            //纯数据，自动初始化
        PlayerAction.Initialize(); //有事件订阅，需要手动管理

        //订阅GM请求事件
        GameEvents.OnTabRequest += CheckTab;
        GameEvents.OnMoveRequest += CheckMove;
        GameEvents.OnViewSwitchRequest += CheckViewSwitch;
        GameEvents.OnOpenDoorRequest += CheckOpenDoor;
        GameEvents.OnRotateRequest += CheckRotate;
        //订阅UIM请求事件
        GameEvents.OnDirectViewSwitchRequest += CheckDirectViewSwitch;

        Debug.Log("VMM:初始化完成。");
    }

    public void OnDisable()
    {
        //清理
        PlayerAction.Cleanup();

        //取消订阅
        GameEvents.OnTabRequest -= CheckTab;
        GameEvents.OnMoveRequest -= CheckMove;
        GameEvents.OnViewSwitchRequest -= CheckViewSwitch;
        GameEvents.OnOpenDoorRequest -= CheckOpenDoor;
        GameEvents.OnRotateRequest -= CheckRotate;
        //UIM请求事件
        GameEvents.OnDirectViewSwitchRequest -= CheckDirectViewSwitch;

    }

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
        GameEvents.onOpenDoorExecute(); 
    }

    void CheckRotate(RotateType type)//left right
    {
        if (!CheckViewMode(ViewMode.View2)
            || !CheckPlayerState(PlayerState.rotatingFinished))
            return;
        GameEvents.onRotateExecute(type);  
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
}