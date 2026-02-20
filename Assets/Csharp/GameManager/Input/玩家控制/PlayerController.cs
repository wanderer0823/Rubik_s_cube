using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController
{
    //玩家行为方法

    //玩家wasd移动
    public void Move()
    {
        Debug.Log("移动中");
    }

    //按e尝试开门
    public void TryOpenDoor()
    {
        Debug.Log("正在尝试开门");
    }


    //玩家打开/关闭背包系统的UI
    public void OnTabPressed()
    {
        
        Debug.Log("打开/关闭背包系统。");
    }
}
