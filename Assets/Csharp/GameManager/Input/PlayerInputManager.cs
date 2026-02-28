using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputManager
{
    private GameManager gm;
    private float holdTime=0.0f;
    private float maxHoverTime = 0.1f;
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

        //检测移动方向
        Vector3 moveDir=Vector3.zero;
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        moveDir = new Vector3(x,0,z);

        if (moveDir!=Vector3.zero)
        {
            holdTime += Time.deltaTime;
            
            if(holdTime>maxHoverTime)
            {
                holdTime = 0.0f;
                gm.RequestMove(moveDir.normalized);
            }
        }
    }

    private void HandleMouse()
    {
        //优先UI输入
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;
        if (Input.GetMouseButtonDown(0))
        {
            gm.RequestLeftRotate();
        }
        if (Input.GetMouseButtonDown(1))
        {
            gm.RequestRightRotate();
        }
        //检测鼠标抬起
        if(Input.GetMouseButtonUp(0))
        {
            gm.RequestLeftRotateFinish();
        }
        if(Input.GetMouseButtonUp(1))
        {
            gm.RequestRightRotateFinish();
        }
    }
}

