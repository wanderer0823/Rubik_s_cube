using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputManager
{
    private GameManager gm;

    public PlayerInputManager(GameManager gameManager)
    {
        gm = gameManager;
    }

    public void Update()
    {
        HandleKeyboard();
        HandleMouse();
    }

    private void HandleKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            gm.RequestTab();

        if (Input.GetKeyDown(KeyCode.F))
            gm.RequestViewSwitch();

        if (Input.GetKeyDown(KeyCode.E))
            gm.RequestOpenDoor();

        if (Input.GetKeyDown(KeyCode.W) ||
            Input.GetKeyDown(KeyCode.A) ||
            Input.GetKeyDown(KeyCode.S) ||
            Input.GetKeyDown(KeyCode.D))
            gm.RequestMove();
    }

    private void HandleMouse()
    {
        if (Input.GetMouseButton(0))
            gm.RequestLeftRotate();

        if (Input.GetMouseButton(1))
            gm.RequestRightRotate();
    }
}

/*
public class PlayerInputManager
{
    private PlayerController playerController;
    private ArrowsButtonManager arrowsButtonManager;
    private ViewSwitchManager viewSwitchManager;
    private CubeRotateController cubeRotateController;

    private Camera View2Camera;
    //监听事件，判断当前状态 左键能否进行3种交互。
    public static event Action<bool> OnViewSwitchAvailabilityChanged;//视角切换button
    public static event Action<bool> OnArrowsAvailabilityChanged;//视角1拧动魔方的箭头
    public static event Action<bool> OnRotateDragAvailabilityChanged;//视角2长按左键/右键 旋转魔方

    
    public PlayerState currentPlayerState;

    public void ProcessInput(PlayerState state)
    {
        switch(state)
        {
            case PlayerState.isTurning:
                PlayerIsTurning();
                break;
            case PlayerState.isRotating:
                PlayerIsRotating();
                break;
            case PlayerState.isWaiting:
                PlayerIsWaiting();
                break;
            case PlayerState.turningFinished:
                PlayerIsTurningFinished();
                break;
            case PlayerState.rotatingFinished:
                PlayerIsRotatingFinished();
                break;
            case PlayerState.isMoving:
                PlayerIsMoving();
                break;
        }
    }

    #region 设置状态方法
    public void SetPlayerInputState(PlayerState newState)
    {
        currentPlayerState = newState;

        UpdateMouseAvailability();
    }
    #endregion

    //获取视角2摄像机
    public PlayerInputManager(Camera cam)
    {
        View2Camera = cam;
    }

    #region 各状态下按键交互禁用状态，不包含鼠标点击按钮
    private void PlayerIsTurning()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            playerController.OnTabPressed();
        }
    }

    private void PlayerIsRotating()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            playerController.OnTabPressed();
        }
    }

    private void PlayerIsWaiting()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            playerController.OnTabPressed();
        }
    }

    private void PlayerIsTurningFinished()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            playerController.OnTabPressed();
        }
        if(Input.GetKeyDown(KeyCode.F))
        {
            viewSwitchManager.OnKeySwitch();
        }
    }

    private void PlayerIsRotatingFinished()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            playerController.OnTabPressed();
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            viewSwitchManager.OnKeySwitch();
        }
        // 长按左键旋转
        if (Input.GetMouseButton(0))
            cubeRotateController.LeftRotate();
        // 长按右键旋转
        if (Input.GetMouseButton(1))
            cubeRotateController.RightRotate();
    }

    private void PlayerIsMoving()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            playerController.OnTabPressed();
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            viewSwitchManager.OnKeySwitch();
        }
        if(Input.GetKeyDown(KeyCode.A)||
            Input.GetKeyDown(KeyCode.W) ||
            Input.GetKeyDown(KeyCode.S) ||
            Input.GetKeyDown(KeyCode.D) )
        {
            playerController.Move();
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            playerController.TryOpenDoor();
        }
    }
    #endregion

    #region 左键 视角转换 按钮禁用状态事件
    private void UpdateMouseAvailability()
    {
        bool enableSwitchViewButton =
            currentPlayerState != PlayerState.isTurning &&
            currentPlayerState != PlayerState.isRotating &&
            currentPlayerState != PlayerState.isWaiting;
        bool enableArrowsButton = currentPlayerState == PlayerState.turningFinished;
        bool enableRotateDrag = currentPlayerState == PlayerState.rotatingFinished;

        OnViewSwitchAvailabilityChanged?.Invoke(enableSwitchViewButton);
        OnArrowsAvailabilityChanged?.Invoke(enableArrowsButton);
        OnRotateDragAvailabilityChanged?.Invoke(enableRotateDrag);
    }
    #endregion

}*/
