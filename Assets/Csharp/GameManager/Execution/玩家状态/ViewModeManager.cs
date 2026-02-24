using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ViewMode
{
    View1,
    View2,
    View3
}

//"ÕÊº“ ‰»Î◊¥Ã¨"
public enum PlayerState
{
    isTurning,
    isRotating,
    isWaiting,
    turningFinished,
    rotatingFinished,
    isMoving
}

public class ViewModeManager : MonoBehaviour
{
    public ViewMode currentViewMode;
    public PlayerState currentPlayerState;

    public void Initialize()
    {
        GameEvents.OnTabRequest += CheckTab;
        GameEvents.OnMoveRequest += CheckMove;
        GameEvents.OnViewSwitchRequest += CheckViewSwitch;
        GameEvents.OnOpenDoorRequest += CheckOpenDoor;
        GameEvents.OnRotateRequest += CheckRotate;
    }

    public void Dispose()
    {
        GameEvents.OnTabRequest -= CheckTab;
        GameEvents.OnMoveRequest -= CheckMove;
        GameEvents.OnViewSwitchRequest -= CheckViewSwitch;
        GameEvents.OnOpenDoorRequest -= CheckOpenDoor;
        GameEvents.OnRotateRequest -= CheckRotate;
    }

    bool CheckViewMode(ViewMode mode)
    {
        if (currentViewMode == mode)
            return true;
        else return false;
    }

    bool CheckPlayerState(PlayerState playerState)
    {
        if (currentPlayerState==playerState)
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
        GameEvents.onViewSwitchExecute(currentViewMode);
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

    ViewMode NextViewMode(ViewMode mode)
    {
        return (ViewMode)(((int)mode + 1) % 3);
    }
}