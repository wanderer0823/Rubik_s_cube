using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("脚本引用")]
    private PlayerInputManager playerInputManager;
    private BallPhysicsManager ballPhysicsManager;

    [Header("视角转换按钮")]
    [SerializeField] private Button[] viewSwitchButtons;
    [Header("拧魔方箭头按钮")]
    [SerializeField] private Button[] ArrowsButtons;
    [Header("摄像机")]
    [SerializeField] private Camera[] View1Camera;
    [SerializeField] private Camera View2Camera;
    [SerializeField] private Camera View3Camera;

    void Start()
    {
        #region 张奕忻
        //给引用脚本创造实例
        playerInputManager = new PlayerInputManager(View2Camera);
        ballPhysicsManager = new BallPhysicsManager();
        //设置初始状态
        playerInputManager.currentPlayerState = PlayerState.isMoving;
        ballPhysicsManager.LockBallPhysics();


        #endregion
    }


    void Update()
    {
        #region 张奕忻
        //检测玩家输入状态---->PlayerInputManager.cs
        playerInputManager.ProcessInput(playerInputManager.currentPlayerState);
        #endregion

        #region 欧熙凝


        #endregion

        #region 张天姿


        #endregion
    }

    #region 张奕忻
    //监听PlayerInputManager.cs--------
    private void OnMouseEnable()
    {
        PlayerInputManager.OnViewSwitchAvailabilityChanged += HandleViewSwitchAvailability;
        PlayerInputManager.OnArrowsAvailabilityChanged += HandleArrowsAvailability;
        PlayerInputManager.OnRotateDragAvailabilityChanged += HandleRotateDragAvailability;
    }

    private void OnMouseDisable()
    {
        PlayerInputManager.OnViewSwitchAvailabilityChanged-= HandleViewSwitchAvailability;
        PlayerInputManager.OnArrowsAvailabilityChanged -= HandleArrowsAvailability;
        PlayerInputManager.OnRotateDragAvailabilityChanged -= HandleRotateDragAvailability;
    }
    # region 1
    //管理视角转换按钮能否有效点击
    private void HandleViewSwitchAvailability(bool canSwitch)
    {
        foreach(var button in viewSwitchButtons)
        {
            button.interactable = canSwitch;
        }
    }
    private void HandleArrowsAvailability(bool canTurnArrows)
    {
        foreach (var button in ArrowsButtons)
        {
            button.interactable = canTurnArrows;
        }
    }
    private void HandleRotateDragAvailability(bool canRotateDrag)
    {

    }
    #endregion

    #endregion
}
