using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using static InitCubeSlot;

public class PlayerAction : MonoBehaviour
{
    public InitCubeSlot cubeData;

    [Header("移动设置")]
    public float moveSpeed = 5f;
    public float smoothTime = 0.1f;     // 移动平滑时间
    public float gravity = -15f;

    [Header("检测设置")]
    public float interactRange=3.0f;

    private CharacterController controller;
    private Vector3 CurrentMoveVelocity;
    private Vector3 FinalMoveVelocity;
    private Vector3 moveSmoothVelocity;
    private Vector3 velocity = Vector3.zero;

    void OnEnable()
    {
        GameEvents.OnTabExecute += OnTabPressed;
        GameEvents.OnMoveExecute += Move;
        //GameEvents.OnOpenDoorExecute += TryOpenDoor;
        // ===== 新增 =====
        GameEvents.OnMatChangeExecute += OnMatChanged;
    }

    void OnDisable()
    {
        GameEvents.OnTabExecute -= OnTabPressed;
        GameEvents.OnMoveExecute -= Move;
        //GameEvents.OnOpenDoorExecute -= TryOpenDoor;
        // ===== 新增 =====
        GameEvents.OnMatChangeExecute -= OnMatChanged;
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

    private void OnTriggerEnter(Collider hit)
    {
        if (hit.CompareTag("Door"))//带tag
        {
            Debug.Log("检测到门");
            ///Yiu：：注释掉TryOpenDoor_()的调用
            //TryOpenDoor_(hit);
        }
        else
        {
            return;
        }
    }

    ///Yiu：注释掉TryOpenDoor_()
    /*
    private void TryOpenDoor_(Collider hit)
    {
        DoorVectorReturn Door = hit.GetComponent<DoorVectorReturn>();
        var gs = GameState.Instance;
        if (gs == null)
            return;

        int id = gs.CurrentRoomID;
        Vector3Int DoorDir = Vector3Int.RoundToInt(Door.DoorinRoomVector);
        Vector3Int oppositeDir = -DoorDir;//门相对的方向
        for (int i = 0; i < cubeData. rooms[id].dirMap.Length; i++)//遍历现在房间的dirmap(六个方向墙面)
        {
            if (DoorDir == FaceOffset[cubeData.rooms[id].dirMap[i]])//找到门对应墙面
            {
                FaceState face = cubeData. rooms[id].GetFace(cubeData.rooms[id].dirMap[i]);//该方向的墙面状态
                if (face.isPassable)
                {
                    
                    // 玩家成功从View3开门切换房间了！！
                    RoomInstanceManager roomInstanceManager = FindObjectOfType<RoomInstanceManager>();
                    foreach (var roomId in roomInstanceManager.GetNeighborRoomIds())
                    {
                        
                        int NeighborRoomID = roomId;
                        if (NeighborRoomID != id)
                        {
                            TryFindTrueNeighborRoom(NeighborRoomID, oppositeDir);
                            Debug.Log("NeighborRoomID是——" + roomId);
                        }
                    }
                    //广播
                    Debug.Log("开门成功，传送到" + GameState.Instance.CurrentRoomID);
                    RoomPreloadController innn = FindObjectOfType<RoomPreloadController>();
                    transform.position = new Vector3(0, 40, 0);
                    innn.TriggerPreloadComplete();//触发跳转
                    break;
                }
                else
                {
                    Debug.Log("开门失败1,id="+id);
                  
                }
            }
        }
    }
    */
    private void TryFindTrueNeighborRoom(int id,Vector3Int ODoorDir)
    {
        for (int i = 0; i < cubeData.rooms[id].dirMap.Length; i++)//遍历现在房间的dirmap(六个方向墙面)
        {
            if (ODoorDir == FaceOffset[cubeData.rooms[id].dirMap[i]])//找到门对应矢量相对的墙面
            {
                FaceState face = cubeData.rooms[id].GetFace(cubeData.rooms[id].dirMap[i]);//该方向的墙面状态
                if (face.isPassable)
                {
                    NeighborPreloadPayload payload = new NeighborPreloadPayload();
                    GameState.Instance.CurrentRoomID = id;
                }
                else
                {
                    Debug.Log("开门失败2");
                    
                }
            }
        }
    }

    // ===== 新增：材质切换响应 =====
    void OnMatChanged(PlayerMatState newMat)
    {
        Debug.Log($"PlayerAction: 材质切换为 {newMat}");

        // TODO: 完整RB改造后在此更新物理参数
        // 目前先做标记，后续改CC→RB时补充：
        // rb.mass = GetProfileForMat(newMat).mass;
        // rb.drag = GetProfileForMat(newMat).drag;
        // collider.material.bounciness = GetProfileForMat(newMat).bounciness;

        // TODO: 更新小球视觉材质
        // var renderer = GetComponent<Renderer>();
        // renderer.material = matForState[newMat];
    }

}
