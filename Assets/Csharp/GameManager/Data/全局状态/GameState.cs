//using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
//using System.Runtime.CompilerServices;
using UnityEngine;
using static InitCubeSlot;
//视角状态
public enum ViewMode
{
    View1,
    View2,
    View3
}

//玩家状态
public enum PlayerState
{
    isTurning,
    isRotating,
    isWaiting,
    turningFinished,
    rotatingFinished,
    isMoving,
    isOpeningBag,    // 新增：打开背包
    isGrabbing       // 新增：举起物体
}

//玩家材质状态
public enum PlayerMatState
{
    Steel,//钢铁
    Glass,//玻璃
    Bounce,//弹力
    None
}
//可拾取道具
public enum ItemType
{
    Spring,//弹簧
    Wind,//风
    Plate//压板
}
public class GameState
{
    public static GameState Instance;
    private GameObject ball;
    private Rigidbody rb;
    public PlayerMatState CurrentMatState { get; private set; } = PlayerMatState.None;
    // 背包：记住打开前的状态
    private PlayerState stateBeforeBag;
    public bool[] TaskFinished { get; private set; } = new bool[5];
    // 线索收集
    private HashSet<string> collectedClueIDs = new HashSet<string>();


    public GameState()
    {
        Instance = this;
        InitGameState();
    }

    public ViewMode CurrentView { get; private set; }
    public PlayerState CurrentPlayerState { get; private set; }
    public FaceDir CurrentPlayerFace { get; private set; }
    public InitCubeSlot.CubeSurface_s CurrentSurface { get; private set; }// 当前小球所在的表面
    public int CurrentRoomID=43;// 当前房间ID，这里是显示的初始房间
    public InitCubeSlot.FaceDir CurrentGravityFace { get; private set; } // 当前重力面


    // 构造函数..初始化默认状态
    public void InitGameState()
    {
        ball = ViewModeManager.Instance.ball_p;
        CurrentView = ViewMode.View3;
        CurrentPlayerState = PlayerState.isMoving;
        CurrentSurface = new InitCubeSlot.CubeSurface_s();
    }

    #region ====================================
    #region 修改视角方向
    // 修改视角
    public void SetView(ViewMode mode)
    {
        // 切视角前自动关背包
        if (IsBagOpen)
            CloseBag();
        CurrentView = mode;
        Debug.Log("切换为视角：" + CurrentView);
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

        GameEvents.onViewSwitchExecute(CurrentView);
    }
    //顺序切换视角
    public void FSetView()
    {
        if (IsBagOpen)
            CloseBag();
        CurrentView = (ViewMode)(((int)CurrentView + 1) % System.Enum.GetValues(typeof(ViewMode)).Length);
        Debug.Log("F切换为视角：" + CurrentView);
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

        GameEvents.onViewSwitchExecute(CurrentView);
    }
#endregion
#endregion

#region ===========================================
#region 修改玩家状态
// 修改玩家状态
public void SetPlayerState(PlayerState state)
    {
        CurrentPlayerState = state;
        Debug.Log("更新为玩家状态：" + CurrentPlayerState);
    }
    #endregion
    #endregion

    #region ===========================================
    #region 控制小球物理状态
    
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
        Debug.Log("更新重力方向" + CurrentGravityFace);
    }
    #endregion
    #endregion

    #region ===========================================
    #region 材质切换
    public void SetMatState(PlayerMatState state)
    {
        CurrentMatState = state;
        Debug.Log("切换材质：" + CurrentMatState);
    }
    #endregion
    #endregion

    #region ===========================================
    #region 背包开关
    public void OpenBag()
    {
        stateBeforeBag = CurrentPlayerState;
        SetPlayerState(PlayerState.isOpeningBag);
        Debug.Log("背包打开，记住之前状态：" + stateBeforeBag);
    }

    public void CloseBag()
    {
        SetPlayerState(stateBeforeBag);
        Debug.Log("背包关闭，恢复状态：" + stateBeforeBag);
    }

    public bool IsBagOpen => CurrentPlayerState == PlayerState.isOpeningBag;
    #endregion
    #endregion

    #region ===========================================
    #region 任务系统
    public bool FinishTask(int index)
    {
        if (index < 0 || index >= 4 || TaskFinished[index]) return false;
        TaskFinished[index] = true;
        Debug.Log($"任务 {index} 完成");
        return true;
    }

    public bool AllTasksFinished()
    {
        foreach (var t in TaskFinished)
            if (!t) return false;
        return true;
    }
    #endregion
    #endregion

    #region ===========================================
    #region 线索收集
    public bool CollectClue(string clueID)
    {
        bool added = collectedClueIDs.Add(clueID);
        if (added) Debug.Log($"收集线索：{clueID}");
        return added;
    }

    public bool HasClue(string clueID) => collectedClueIDs.Contains(clueID);
    public HashSet<string> GetAllClues() => collectedClueIDs;
    #endregion
    #endregion

}