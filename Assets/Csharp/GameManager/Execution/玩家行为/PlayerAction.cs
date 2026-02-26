using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAction:MonoBehaviour
{
    void OnEnable()
    {
        GameEvents.OnTabExecute += OnTabPressed;
        GameEvents.OnMoveExecute += Move;
        GameEvents.OnOpenDoorExecute += TryOpenDoor;
        Debug.Log("PlayerController 事件订阅完成");
    }

    void OnDisable()
    {
        GameEvents.OnTabExecute -= OnTabPressed;
        GameEvents.OnMoveExecute -= Move;
        GameEvents.OnOpenDoorExecute -= TryOpenDoor;
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
    //按e尝试开门
    void TryOpenDoor()
    {
        Debug.Log("正在尝试开门");
    }
}
