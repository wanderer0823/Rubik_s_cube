using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputManager
{
    private GameManager gm;
    private float holdTime=0.0f;
    private float maxHoverTime = 0.01f;
    private bool isView2LeftRotateActive;
    [Header("鼠标灵敏度")]
    [SerializeField] private float mouseSensitivity = 2f;

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
            gm.RequestInteract();

        //欧：检测移动方向
        // 每帧获取移动输入（可能为零）
        Vector3 moveDir = Vector3.zero;
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        moveDir = new Vector3(x, 0, z).normalized;

        // 无论是否为零都发送，让 PlayerAction 每帧执行移动/刹车逻辑
        gm.RequestMove(moveDir);

    }

    private void HandleMouse()
    {
        //优先UI输入
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        bool isView2 = GameState.Instance != null && GameState.Instance.CurrentView == ViewMode.View2;

        if (Input.GetMouseButtonDown(0))
        {
            if (isView2)
            {
                if (IsMouseOnView2Cube())
                {
                }
                else
                {
                    isView2LeftRotateActive = true;
                    gm.RequestLeftRotate();
                }
            }
            else
            {
                gm.RequestLeftRotate();
            }
        }
        if (Input.GetMouseButtonDown(1))
        {
            gm.RequestRightRotate();
        }
        //检测鼠标抬起
        if(Input.GetMouseButtonUp(0))
        {
            if (isView2)
            {
                if (isView2LeftRotateActive)
                {
                    isView2LeftRotateActive = false;
                    gm.RequestLeftRotateFinish();
                }
                else
                {
                }
            }
            else
            {
                gm.RequestLeftRotateFinish();
            }
        }
        if(Input.GetMouseButtonUp(1))
        {
            gm.RequestRightRotateFinish();
        }
        //欧：检测鼠标移动
        if(TryGetMouseMoveInput(out Vector2 mouseMove))
        {
            gm.RequestMouseMove(mouseMove);
        }
        // ===== 新增：滚轮检测 =====
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollDelta) > 0.01f)
        {
            gm.RequestScroll(scrollDelta);
        }
    }


    #region ======================================================
    #region === 欧：上面两个主要函数使用的封装函数===


    //欧：检测视角移动操作输入的函数
    private bool TryGetMouseMoveInput(out Vector2 mouseMove)
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        mouseMove = new Vector2(mouseX, mouseY);
        return mouseMove != Vector2.zero;
    }

    private bool IsMouseOnView2Cube()
    {
        if (GameState.Instance == null || GameState.Instance.CurrentView != ViewMode.View2)
            return false;

        var cubeRoot = ViewModeManager.Instance?.cubeRoot;
        if (cubeRoot == null)
            return false;

        var cameraController = UnityEngine.Object.FindObjectOfType<CameraRotateController>();
        if (cameraController == null)
            return false;

        Camera cam = cameraController.GetComponent<Camera>();
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return false;

        return hit.transform == cubeRoot || hit.transform.IsChildOf(cubeRoot);
    }

    #endregion
    #endregion
}

