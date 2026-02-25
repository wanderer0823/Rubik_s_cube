using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerAction
{
    static PlayerAction()
    {
        Debug.Log("PlayerController 初始化");
    }
    public static void Initialize()
    {
        GameEvents.OnTabExecute += OnTabPressed;
        GameEvents.OnMoveExecute += Move;
        GameEvents.OnOpenDoorExecute += TryOpenDoor;
        GameEvents.OnRotateExecute += RotateCube;
        Debug.Log("PlayerController 事件订阅完成");
    }

    public static void Cleanup()
    {
        GameEvents.OnTabExecute -= OnTabPressed;
        GameEvents.OnMoveExecute -= Move;
        GameEvents.OnOpenDoorExecute -= TryOpenDoor;
        GameEvents.OnRotateExecute -= RotateCube;
    }



    //玩家打开/关闭背包系统的UI
    static void OnTabPressed()
    {
        Debug.Log("打开/关闭背包系统。");
    }
    //玩家wasd移动
    static void Move()
    {
        Debug.Log("移动中");
    }
    //按e尝试开门
    static void TryOpenDoor()
    {
        Debug.Log("正在尝试开门");
    }
    static void RotateCube(RotateType type)
    {
        Debug.Log("执行魔方旋转 " + type);
    }

}
