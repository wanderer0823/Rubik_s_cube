using System;
using UnityEngine;
using UnityEngine.UI;

public enum RotateType
{
    Left,
    Right
}

public class GameManager : MonoBehaviour
{
    #region === 子系统引用 ===
    [Header("Input Systems")]
    private PlayerInputManager playerInputManager;
    [Header("Data Systems")]
    private InitCubeSlot initCubeSlot;
    //private InitRoomDoors initRoomDoors;
    [Header("Execution Systems")]
    private ViewModeManager viewModeManager;
    public PlayerController playerController;
    //public CubeTurnController cubeTurnController;
    public CubeRotateController cubeRotateController;
    public RoomPreloadController roomPreloadSystem;
    [Header("Presentation Systems表现层")]
    public CameraManager cameraManager;
    public ArrowsButtonManager arrowsButtonManager;
    public ViewSwitchManager viewSwitchManager;
    #endregion

    
    #region === 全局状态 ===
    public int currentRoomIndex { get; private set; }
    public ViewMode currentView { get; private set; }
    public PlayerState currentPlayerState { get; private set; }
    #endregion

    #region === UI 引用 ===
    [Header("视角转换按钮")]
    [SerializeField] private Button[] viewSwitchButtons;
    [Header("拧魔方箭头按钮")]
    [SerializeField] private Button[] arrowsButtons;
    #endregion

    #region === 生命周期 ===
    void Start()
    {
        // 创建输入管理器
        playerInputManager = new PlayerInputManager(this);

        // 初始状态
        currentView = ViewMode.View3;
        currentPlayerState = PlayerState.isMoving;

        UpdateUIState();
    }

    void Update()
    {
        playerInputManager.Update(); // 只读输入
    }
    #endregion

    #region ======================================================
    #region === 输入请求接口（PIM 调用）===
    public void RequestTab()
    {
        Debug.Log("Tab Pressed请求");
        GameEvents.onTabRequest();  
    }

    public void RequestViewSwitch()
    {
        Debug.Log("ViewSwitch请求");
        GameEvents.onViewSwitchRequest(); 
    }

    public void RequestMove()
    {
        Debug.Log("Player Move请求");
        GameEvents.onMoveRequest(); 
    }

    public void RequestOpenDoor()
    {
        Debug.Log("Try Open Door请求");
        GameEvents.onOpenDoorRequest(); 
    }

    public void RequestLeftRotate()
    {
        Debug.Log("LeftRotate请求");
        GameEvents.onRotateRequest(RotateType.Left); 
    }

    public void RequestRightRotate()
    {
        Debug.Log("RightRotate请求");
        GameEvents.onRotateRequest(RotateType.Right); 
    }
    #endregion
    #endregion

    #region ======================================================
    #region === 输入请求接口（PlayerController 调用）===
    #endregion
    #endregion

    #region ======================================================
    #region === 输入请求接口（ViewSwitchManager 调用）===
    #endregion
    #endregion

    #region ======================================================
    #region === 行为判断层 ===

    #endregion
    #endregion


    #region ======================================================
    #region === 行为执行层 ===
    public void SwitchToView(ViewMode newView)
    {
        if (currentView == newView) return;

        currentView = newView;
        GameEvents.OnViewChangedInvoke(currentView);  // 广播
    }
    #endregion
    #endregion


    #region ======================================================
    #region === UI 更新 ===
    private void UpdateUIState()
    {
        bool canSwitch = true;// CanSwitchView();
        bool canRotate = false;// CanRotateCube();

        foreach (var btn in viewSwitchButtons)
            btn.interactable = canSwitch;

        foreach (var btn in arrowsButtons)
            btn.interactable = canRotate;
    }
    #endregion
    #endregion

}