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

