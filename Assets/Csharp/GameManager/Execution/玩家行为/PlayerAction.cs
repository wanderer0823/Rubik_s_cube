using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAction : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;
    public float smoothTime = 0.1f;     // 移动平滑时间
    public float gravity = -15f;


    private CharacterController controller;
    private Vector3 CurrentMoveVelocity;
    private Vector3 FinalMoveVelocity;
    private Vector3 moveSmoothVelocity;
    private Vector3 velocity=Vector3.zero;

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

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    //玩家打开/关闭背包系统的UI
    void OnTabPressed()
    {
        Debug.Log("打开/关闭背包系统。");
    }
    //玩家wasd移动
    void Move(Vector3 moveDir)
    {
        Debug.Log("移动中");
        moveDir = transform.right * moveDir.x + transform.forward * moveDir.z;
        if (moveDir.magnitude > 0.1f)
        {
            //平滑移动
            CurrentMoveVelocity = Vector3.SmoothDamp(
                CurrentMoveVelocity,            //当前速度
                moveDir.normalized * moveSpeed, //目标速度
                ref moveSmoothVelocity,         //存储中间速度
                smoothTime                      //平滑时间
            );
        }
        else
        {
            // 停止时减速
            CurrentMoveVelocity = Vector3.SmoothDamp(
                CurrentMoveVelocity,
                Vector3.zero,
                ref moveSmoothVelocity,
                smoothTime
            );
        }

        //应用重力
        if (!controller.isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        else if (velocity.y < 0)
        {
            velocity.y = -2f;  // 轻微贴地
        }

        // 组合移动和重力
        Vector3 finalVelocity = CurrentMoveVelocity;
        finalVelocity.y = velocity.y;
        controller.Move(finalVelocity * Time.deltaTime);
    }
        
    //按e尝试开门
    void TryOpenDoor()
        {
            Debug.Log("正在尝试开门");
        #region 张奕忻注释
        //执行此函数时玩家已经按下E，写一个-----------------
        //如果 玩家碰撞体没有检测到Tag"Door"，则返回。
        //如果 检测到door，则获取这个门的isPassible=true?
        //if(isPassible==false):
        //debug"开门失败“后续添加失败特效。
        //if(isPassible==true):
        //玩家成功从View3开门切换房间了！！
        //广播到：VMM，（0） 广播进入异步转场（暂无脚本）：后台进行以下计算和广播：
        //             （1） 用GS方法更新GameState.CurrentPlayerxxx,,,
        //             （2） 更新完玩家新位置，RPC订阅VMM的广播，计算一次邻居房间信息。
        //             （3） 实例化房间perfab的脚本 订阅VMM广播，spawnRoom一次。
        #endregion
    }

}
