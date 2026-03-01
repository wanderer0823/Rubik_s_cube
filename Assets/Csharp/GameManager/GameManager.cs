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
    #region === ��ϵͳ���� ===
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
    #endregion

    
    #region === ȫ��״̬ ===
    public int currentRoomIndex { get; private set; }
    #endregion

    #region === �������� ===
    void Start()
    {
        // �������������
        playerInputManager = new PlayerInputManager(this);
    }

    void Update()
    {
        //������
        playerInputManager.Update(); // ֻ������
    }

    private void OnDestroy()
    {

    }
    #endregion

    #region ======================================================
    #region === ��������ӿڣ�PIM ���ã�===
    public void RequestTab()
    {
        Debug.Log("Tab Pressed����");
        GameEvents.onTabRequest();  
    }

    public void RequestViewSwitch()
    {
        Debug.Log("ViewSwitch����");
        GameEvents.onViewSwitchRequest(); 
    }

    public void RequestMove(Vector3 moveDir)
    {
        Debug.Log("Player Move����");
        GameEvents.onMoveRequest(moveDir); 
    }

    public void RequestOpenDoor()
    {
        Debug.Log("Try Open Door����");
        GameEvents.onOpenDoorRequest(); 
    }

    public void RequestLeftRotate()
    {
        Debug.Log("LeftRotate����");
        GameEvents.onRotateRequest(RotateType.Left); 
    }

    public void RequestRightRotate()
    {
        Debug.Log("RightRotate����");
        GameEvents.onRotateRequest(RotateType.Right); 
    }

    public void RequestLeftRotateFinish()
    {
        Debug.Log("LeftRotateFinish����");
        GameEvents.onRotateFinishRequest(RotateType.Left);
    }

    public void RequestRightRotateFinish()
    {
        Debug.Log("RightRotateFinish����");
        GameEvents.onRotateFinishRequest(RotateType.Right);
    }
    #endregion
    #endregion

    #region ======================================================
    #region === ��������ӿڣ� ���ã�===
    #endregion
    #endregion

    #region ======================================================
    #region === ��Ϊ�жϲ� ===

    #endregion
    #endregion


    #region ======================================================
    #region === ��Ϊִ�в� ===

    #endregion
    #endregion


    #region ======================================================
    #region === UI ���� ===
    #endregion
    #endregion

}