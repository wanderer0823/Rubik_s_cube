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
    #region === 脚本引用 ===
    [Header("Input Systems")]
    private PlayerInputManager playerInputManager;
    [Header("Data Systems")]
    private InitCubeSlot initCubeSlot;
    //private InitRoomDoors initRoomDoors;
    [Header("Execution Systems")]
    private ViewModeManager viewModeManager;
    //public CubeTurnController cubeTurnController;
    public CubeRotateController cubeRotateController;
    public RoomPreloadController roomPreloadSystem;
    [Header("Presentation Systems���ֲ�")]
    public ArrowsButton ArrowsButton;
    public MusicAudioManager musicAudioManager;
    #endregion

    public static GameManager Instance;
    
    #region === 字段 ===
    public int currentRoomIndex { get; private set; }
    #endregion

    #region === 脚本生命 ===
    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        playerInputManager = new PlayerInputManager(this);
    }

    void Update()
    {
        playerInputManager.Update(); // ֻ������
    }

    private void OnDestroy()
    {

    }
    #endregion

    #region ======================================================
    #region === PIM 输入请求===
    // ===== 新增 Request 方法 =====
    public void RequestInteract()
    {
        //Debug.Log("GM:玩家按E交互请求");
        GameEvents.onInteractRequest();
    }

    public void RequestScroll(float delta)
    {
        //Debug.Log("GM:滚轮请求");
        GameEvents.onScrollRequest(delta);
    }

    public void RequestTab()
    {
        //Debug.Log("Tab Pressed����");
        GameEvents.onTabRequest();  
    }

    public void RequestViewSwitch()
    {
        //Debug.Log("ViewSwitch����");
        GameEvents.onViewSwitchRequest(); 
    }

    public void RequestMove(Vector3 moveDir)
    {
        //Debug.Log("Player Move����");
        GameEvents.onMoveRequest(moveDir); 
    }

    public void RequestOpenDoor()
    {
        //Debug.Log("Try Open Door����");
        GameEvents.onOpenDoorRequest(); 
    }

    public void RequestLeftRotate()
    {
        //Debug.Log("LeftRotate����");
        GameEvents.onRotateRequest(RotateType.Left); 
    }

    public void RequestRightRotate()
    {
        //Debug.Log("RightRotate����");
        GameEvents.onRotateRequest(RotateType.Right); 
    }

    public void RequestLeftRotateFinish()
    {
        //Debug.Log("LeftRotateFinish����");
        GameEvents.onRotateFinishRequest(RotateType.Left);
    }

    public void RequestRightRotateFinish()
    {
        //Debug.Log("RightRotateFinish����");
        GameEvents.onRotateFinishRequest(RotateType.Right);
    }

    public void RequestMouseMove(Vector2 mouseMove)//欧：鼠标移动检测
    {
        GameEvents.onMouseLookRequest(mouseMove);
    }
    #endregion
    #endregion
}
