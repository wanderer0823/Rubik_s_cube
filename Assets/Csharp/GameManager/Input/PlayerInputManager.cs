using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputManager
{
    private GameManager gm;
    private float holdTime=0.0f;
    private float maxHoverTime = 0.01f;
    private const float MouseDragCompleteThresholdPixels = 8f;
    private bool isView2LeftRotateActive;
    private bool isRightRotateActive;
    private bool isMovementLocked;
    private bool isLeftMouseTrackingDrag;
    private bool isRightMouseTrackingDrag;
    private Vector3 leftMouseDownPosition;
    private Vector3 rightMouseDownPosition;
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

    public void SetMovementLocked(bool locked)
    {
        isMovementLocked = locked;
    }

    private void HandleKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            gm.RequestTab();

        if (Input.GetKeyDown(KeyCode.F))
            gm.RequestViewSwitch();

        if (Input.GetKeyDown(KeyCode.E))
            gm.RequestInteract();
        if (Input.GetKeyDown(KeyCode.Escape))
            gm.RequestExit();

        //欧：检测移动方向
        // 每帧获取移动输入（可能为零）
        Vector3 moveDir = Vector3.zero;
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        moveDir = new Vector3(x, 0, z).normalized;
        if (isMovementLocked)
            moveDir = Vector3.zero;

        // 无论是否为零都发送，让 PlayerAction 每帧执行移动/刹车逻辑
        gm.RequestMove(moveDir);

    }

    private void HandleMouse()
    {
        //优先UI输入
        bool isView2 = GameState.Instance != null && GameState.Instance.CurrentView == ViewMode.View2;
        bool isPointerOverUI = UnityEngine.EventSystems.EventSystem.current != null
            && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        if (Input.GetMouseButtonDown(0))
        {
            if (isView2)
                StartMouseDragTracking(0);

            if (isPointerOverUI)
                return;

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
            if (isView2)
                StartMouseDragTracking(1);

            if (isPointerOverUI)
                return;

            isRightRotateActive = true;
            gm.RequestRightRotate();
        }
        //检测鼠标抬起
        if(Input.GetMouseButtonUp(0))
        {
            CompleteMouseDragTracking(0);

            if (isPointerOverUI && !(isView2 && isView2LeftRotateActive))
                return;

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
            CompleteMouseDragTracking(1);
            if (isPointerOverUI && !isRightRotateActive)
                return;

            isRightRotateActive = false;
            gm.RequestRightRotateFinish();
        }
        //欧：检测鼠标移动
        if (isPointerOverUI)
            return;

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

    private void StartMouseDragTracking(int button)
    {
        if (button == 0)
        {
            isLeftMouseTrackingDrag = true;
            leftMouseDownPosition = Input.mousePosition;
        }
        else if (button == 1)
        {
            isRightMouseTrackingDrag = true;
            rightMouseDownPosition = Input.mousePosition;
        }
    }

    private void CompleteMouseDragTracking(int button)
    {
        Vector3 mouseUpPosition = Input.mousePosition;
        float thresholdSqr = MouseDragCompleteThresholdPixels * MouseDragCompleteThresholdPixels;

        if (button == 0)
        {
            if (isLeftMouseTrackingDrag && (mouseUpPosition - leftMouseDownPosition).sqrMagnitude >= thresholdSqr)
                gm.RequestLeftMouseDragCompleted();

            isLeftMouseTrackingDrag = false;
        }
        else if (button == 1)
        {
            if (isRightMouseTrackingDrag && (mouseUpPosition - rightMouseDownPosition).sqrMagnitude >= thresholdSqr)
                gm.RequestRightMouseDragCompleted();

            isRightMouseTrackingDrag = false;
        }
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

