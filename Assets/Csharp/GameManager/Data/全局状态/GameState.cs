using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//视角状态
public enum ViewMode
{
    View1,
    View2,
    View3
}

//玩家输入状态
public enum PlayerState
{
    isTurning,
    isRotating,
    isWaiting,
    turningFinished,
    rotatingFinished,
    isMoving
}

public class GameState
{
    public ViewMode CurrentView { get; private set; }
    public PlayerState CurrentPlayerState { get; private set; }


    // 构造函数..初始化默认状态
    public GameState()
    {
        CurrentView = ViewMode.View3;
        CurrentPlayerState = PlayerState.isMoving;
    }


    // 修改视角
    public void SetView(ViewMode mode)
    {
        CurrentView = mode;
    }


    // 修改玩家状态
    public void SetPlayerState(PlayerState state)
    {
        CurrentPlayerState = state;
    }

    //按顺序切换视角
    public void FSetView()
    {
        CurrentView = (ViewMode)(((int)CurrentView + 1) % System.Enum.GetValues(typeof(ViewMode)).Length);
    }
}