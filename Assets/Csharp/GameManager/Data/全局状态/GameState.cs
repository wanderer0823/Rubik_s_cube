//using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
//using System.Runtime.CompilerServices;
using UnityEngine;
using static InitCubeSlot;
//瑙嗚鐘舵€?
public enum ViewMode
{
    //View1,
    View2,
    View3
}

//鐜╁鐘舵€?
public enum PlayerState
{
    isTurning,
    isRotating,
    isWaiting,
    turningFinished,
    rotatingFinished,
    isMoving,
    isOpeningBag,    // 鏂板锛氭墦寮€鑳屽寘
    isGrabbing       // 鏂板锛氫妇璧风墿浣?
}

//鐜╁鏉愯川鐘舵€?
public enum PlayerMatState
{
    Steel,//閽㈤搧
    Glass,//鐜荤拑
    Bounce,//寮瑰姏
    None
}
//鍙嬀鍙栭亾鍏?
public enum ItemType
{
    Spring,//寮圭哀
    Wind,//椋?
    Plate//鍘嬫澘
}
public class GameState
{
    public static GameState Instance;
    private GameObject ball;
    private Rigidbody rb;
    public PlayerMatState CurrentMatState { get; private set; } = PlayerMatState.None;
    // 鑳屽寘锛氳浣忔墦寮€鍓嶇殑鐘舵€?
    private PlayerState stateBeforeBag;
    public bool[] TaskFinished { get; private set; } = new bool[5];
    // 绾跨储鏀堕泦
    private HashSet<string> collectedClueIDs = new HashSet<string>();


    public GameState()
    {
        Instance = this;
        InitGameState();
    }

    public ViewMode CurrentView { get; private set; }
    public PlayerState CurrentPlayerState { get; private set; }
    public FaceDir CurrentPlayerFace { get; private set; }
    public InitCubeSlot.CubeSurface_s CurrentSurface { get; private set; }// 褰撳墠灏忕悆鎵€鍦ㄧ殑琛ㄩ潰
    public int CurrentRoomID=43;// 褰撳墠鎴块棿ID锛岃繖閲屾槸鏄剧ず鐨勫垵濮嬫埧闂?
    public InitCubeSlot.FaceDir CurrentGravityFace { get; private set; } // 褰撳墠閲嶅姏闈?


    // 鏋勯€犲嚱鏁?.鍒濆鍖栭粯璁ょ姸鎬?
    public void InitGameState()
    {
        ball = ViewModeManager.Instance.ball_p;
        CurrentView = ViewMode.View3;
        CurrentPlayerState = PlayerState.isMoving;
        CurrentSurface = new InitCubeSlot.CubeSurface_s();
        RefreshCurrentSurfaceFromRoomID();
    }

    #region ====================================
    #region 淇敼瑙嗚鏂瑰悜
    // 淇敼瑙嗚
    public void SetView(ViewMode mode)
    {
        // 鍒囪瑙掑墠鑷姩鍏宠儗鍖?
        if (IsBagOpen)
            CloseBag();
        CurrentView = mode;
        /*if(mode==ViewMode.View1)
        {
            SetPlayerState(PlayerState.turningFinished);
        }*/
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
    //椤哄簭鍒囨崲瑙嗚
    public void FSetView()
    {
        if (IsBagOpen)
            CloseBag();
        CurrentView = (ViewMode)(((int)CurrentView + 1) % System.Enum.GetValues(typeof(ViewMode)).Length);
        /*if (CurrentView == ViewMode.View1)
        {
            SetPlayerState(PlayerState.turningFinished);
        }*/
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
#region 淇敼鐜╁鐘舵€?
// 淇敼鐜╁鐘舵€?
public void SetPlayerState(PlayerState state)
    {
        CurrentPlayerState = state;
    }
    #endregion
    #endregion

    #region ===========================================
    #region 鎺у埗灏忕悆鐗╃悊鐘舵€?
    
    #endregion
    #endregion

    #region ===========================================
    #region 鏇存柊灏忕悆浣嶇疆鐘舵€?
    public void SetCurrentSurface(InitCubeSlot.CubeSurface_s surface)
    {
        CurrentSurface = surface;

        if (surface != null)
        {
            CurrentRoomID = surface.roomID;
            CurrentPlayerFace = surface.dir;
        }

    }

    public bool RefreshCurrentSurfaceFromRoomID()
    {
        var cubeData = ViewModeManager.Instance?.cubeData;
        if (cubeData == null)
            return false;

        var surface = cubeData.GetSurfaceByRoomID(CurrentRoomID);
        if (surface == null)
            return false;

        SetCurrentSurface(surface);
        return true;
    }
    public void SetGravityFace(InitCubeSlot.FaceDir face)
    {
        CurrentGravityFace = face;
    }
    #endregion
    #endregion

    #region ===========================================
    #region 鏉愯川鍒囨崲
    public void SetMatState(PlayerMatState state)
    {
        CurrentMatState = state;
    }
    #endregion
    #endregion

    #region ===========================================
    #region 鑳屽寘寮€鍏?
    public void OpenBag()
    {
        stateBeforeBag = CurrentPlayerState;
        SetPlayerState(PlayerState.isOpeningBag);
    }

    public void CloseBag()
    {
        SetPlayerState(stateBeforeBag);
    }

    public bool IsBagOpen => CurrentPlayerState == PlayerState.isOpeningBag;
    #endregion
    #endregion

    #region ===========================================
    #region 浠诲姟绯荤粺
    public bool FinishTask(int index)
    {
        if (index < 0 || index >= 4 || TaskFinished[index]) return false;
        TaskFinished[index] = true;
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
    #region 绾跨储鏀堕泦
    public bool CollectClue(string clueID)
    {
        bool added = collectedClueIDs.Add(clueID);
        return added;
    }

    public bool HasClue(string clueID) => collectedClueIDs.Contains(clueID);
    public HashSet<string> GetAllClues() => collectedClueIDs;
    #endregion
    #endregion

}
