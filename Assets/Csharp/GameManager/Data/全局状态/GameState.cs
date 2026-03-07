using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static InitCubeSlot;
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
//小球重力锁定状态
public enum BallPhysics
{
    On,
    Off
}

public class GameState
{
    public static GameState Instance;
    private GameObject ball;
    private Rigidbody rb;

    public GameState()
    {
        Instance = this;
        InitGameState();
    }

    public ViewMode CurrentView { get; private set; }
    public PlayerState CurrentPlayerState { get; private set; }
    public BallPhysics CurrentBallPhysics { get; private set; }
    //玩家所在位置
    public FaceDir CurrentPlayerFace { get; private set; }
    public InitCubeSlot.CubeSurface_s CurrentSurface { get; private set; }// 当前小球所在外表面
    public int CurrentRoomID { get; private set; }// 当前房间ID
    public InitCubeSlot.FaceDir CurrentGravityFace { get; private set; } // 当前重力面


    // 构造函数..初始化默认状态
    public void InitGameState()
    {
        ball = ViewModeManager.Instance.ball_p;
        CurrentView = ViewMode.View3;
        CurrentPlayerState = PlayerState.isMoving;
        SetBallPhysics( BallPhysics.Off);
        CurrentSurface = new InitCubeSlot.CubeSurface_s();
    }

    #region ====================================
    #region 更新视角方法
    // 修改视角
    public void SetView(ViewMode mode)
    {
        CurrentView = mode;
        Debug.Log("按键更新为视角：" + CurrentView);
        if(mode==ViewMode.View1)
        {
            SetPlayerState(PlayerState.turningFinished);
        }
        if(mode==ViewMode.View2)
        {
            SetPlayerState(PlayerState.rotatingFinished);
        }
        if (mode == ViewMode.View3)
        {
            SetPlayerState(PlayerState.isMoving);
        }
    }
    //按顺序切换视角
    public void FSetView()
    {
        CurrentView = (ViewMode)(((int)CurrentView + 1) % System.Enum.GetValues(typeof(ViewMode)).Length);
        Debug.Log("F更新为视角：" + CurrentView);
        if (CurrentView == ViewMode.View1)
        {
            SetPlayerState(PlayerState.turningFinished);
        }
        if (CurrentView == ViewMode.View2)
        {
            SetPlayerState(PlayerState.rotatingFinished);
        }
        if (CurrentView == ViewMode.View3)
        {
            SetPlayerState(PlayerState.isMoving);
        }
    }
    #endregion
    #endregion

    #region ===========================================
    #region 更新玩家输入状态
    // 修改玩家状态
    public void SetPlayerState(PlayerState state)
    {
        CurrentPlayerState = state;
        Debug.Log("更新为输入状态：" + CurrentPlayerState);
    }
    #endregion
    #endregion

    #region ===========================================
    #region 更新小球物理状态
    private void GetBallRigidBody()
    {
        rb = ball.GetComponent<Rigidbody>();
    }
    //锁定小球物理
    public void SetBallPhysics(BallPhysics bp)
    {
        CurrentBallPhysics = bp;
        Debug.Log("更新小球物理：" + CurrentBallPhysics);
        if(bp==BallPhysics.On)
        {
            UnlockBallPhysics();
        }
        if (bp == BallPhysics.Off)
        {
            LockBallPhysics();
        }
    }
    void UnlockBallPhysics()
    {
        Debug.Log("解锁小球物理。");
        GetBallRigidBody();
        rb.isKinematic = false;
    }

    void LockBallPhysics()
    {
        Debug.Log("锁定小球物理。");
        GetBallRigidBody();
        rb.isKinematic = true;
    }
    #endregion
    #endregion

    #region ===========================================
    #region 更新小球位置状态
    public void SetCurrentSurface(InitCubeSlot.CubeSurface_s surface)
    {
        CurrentSurface = surface;

        if (surface != null)
        {
            CurrentRoomID = surface.roomID;
            CurrentPlayerFace = surface.dir;
        }

        Debug.Log($"更新空间信息 Room:{CurrentRoomID} Face:{CurrentPlayerFace}");
    }
    public void SetGravityFace(InitCubeSlot.FaceDir face)
    {
        CurrentGravityFace = face;
        Debug.Log("更新重力方向：" + CurrentGravityFace);
    }
    #endregion
    #endregion
}