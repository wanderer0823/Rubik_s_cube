using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController
{
    //玩家行为方法
    void OnEnable()
    {
        GameEvents.OnTabExecute += OnTabPressed;
        GameEvents.OnMoveExecute += Move;
        GameEvents.OnViewSwitchExecute += SwitchView;
        GameEvents.OnOpenDoorExecute += TryOpenDoor;
        GameEvents.OnRotateExecute += RotateCube;
    }

    void OnDisable()
    {
        GameEvents.OnTabExecute -= OnTabPressed;
        GameEvents.OnMoveExecute -= Move;
        GameEvents.OnViewSwitchExecute -= SwitchView;
        GameEvents.OnOpenDoorExecute -= TryOpenDoor;
        GameEvents.OnRotateExecute -= RotateCube;
    }


    
    //玩家打开/关闭背包系统的UI
    void OnTabPressed()
    {
        Debug.Log("打开/关闭背包系统。");
    }
    //玩家wasd移动
    void Move()
    {
        Debug.Log("移动中");
    }
    void SwitchView(ViewMode mode)
    {
        Debug.Log("执行视角切换 " + mode);
    }
    //按e尝试开门
    void TryOpenDoor()
    {
        Debug.Log("正在尝试开门");
    }
    void RotateCube(RotateType type)
    {
        Debug.Log("执行魔方旋转 " + type);
    }

}
