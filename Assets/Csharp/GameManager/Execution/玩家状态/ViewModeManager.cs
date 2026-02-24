using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewModeManager : MonoBehaviour
{
    private GameState gs;
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
        //¸üÐÂview mode
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
}