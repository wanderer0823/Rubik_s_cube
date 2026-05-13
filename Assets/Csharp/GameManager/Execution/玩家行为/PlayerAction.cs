using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using static InitCubeSlot;

public class PlayerAction : MonoBehaviour
{
    public InitCubeSlot cubeData;

    [Header("物理参数配置（从Project面板拖入）")]
    public PlayerPhysicsProfile steelProfile;
    public PlayerPhysicsProfile glassProfile;
    public PlayerPhysicsProfile bounceProfile;

    [Header("反弹控制（仅Bounce状态）")]
    public float minBounceSpeed = 1.5f;

    [Header("移动设置")]
    public float moveSpeed = 5f;
    /*public float smoothTime = 0.1f;     // 移动平滑时间
    public float gravity = -15f;*/

    [Header("检测设置")]
    public float interactRange=3.0f;

    [Header("小球材质显示（View1/2可见的Sphere）")]
    public Renderer ballRenderer;
    public Material steelMaterial;
    public Material glassMaterial;
    public Material bounceMaterial;

    /*private CharacterController controller;
    private Vector3 CurrentMoveVelocity;
    private Vector3 FinalMoveVelocity;
    private Vector3 moveSmoothVelocity;
    private Vector3 velocity = Vector3.zero;*/
    private Rigidbody rb;
    private Collider col;
    private GameState gs;
    private bool isBouncing = false;
    // 当前生效的 profile
    private PlayerPhysicsProfile currentProfile;

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
        //controller = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();///Yiu
        col = GetComponent<Collider>();
    }

    void Start()///YIu
    {
        gs = GameState.Instance;
        ApplyProfile(steelProfile);
    }
    void FixedUpdate()//Yiu
    {
        // Bounce 状态：速度低于阈值时停止反弹，恢复正常移动
        if (gs != null && gs.CurrentMatState == PlayerMatState.Bounce && isBouncing)
        {
            if (rb.velocity.magnitude < minBounceSpeed)
            {
                isBouncing = false;
                Debug.Log("Bounce反弹结束，恢复正常移动");
            }
        }
    }

    //玩家打开/关闭背包系统的UI
    void OnTabPressed()
    {
        Debug.Log("打开/关闭背包系统。");
    }
    //玩家wasd移动
    void Move(Vector3 moveDir)
    {
        ///Yiu
        // Bounce 反弹中不接受移动输入
        if (isBouncing) return;
        moveDir = transform.right * moveDir.x + transform.forward * moveDir.z;
        float speed = currentProfile != null ? currentProfile.moveSpeed : 5f;
        if (moveDir.magnitude > 0.1f)
        {
            Vector3 targetVel = moveDir.normalized * speed;
            targetVel.y = rb.velocity.y;
            rb.velocity = targetVel;
        }
        else
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }
        #region （已注释） 欧的旧移动CC
        /*Debug.Log("移动中");
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
        controller.Move(finalVelocity * Time.deltaTime);*/
        #endregion
    }

    ///Yiu：注释掉TryOpenDoor_()
    #region （已注释） 欧的旧尝试开门
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
                    controller.enabled = false;     // 临时禁用控制器
                    transform.position = new Vector3(0, 40, 0);
                    controller.enabled = true;      // 重新启用
                    RoomPreloadController innn = FindObjectOfType<RoomPreloadController>();
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
    
    */
    #endregion

    // ===== 新增：材质切换响应 =====
    void OnMatChanged(PlayerMatState newMat)
    {
        Debug.Log($"PlayerAction: 材质切换为 {newMat}");

        // 物理参数切换
        PlayerPhysicsProfile profile = GetProfileForMat(newMat);
        ApplyProfile(profile);

        // 切换材质时取消反弹状态
        isBouncing = false;

        // 更新小球视觉材质
        if (ballRenderer != null)
        {
            Material targetMat = newMat switch
            {
                PlayerMatState.Steel => steelMaterial,
                PlayerMatState.Glass => glassMaterial,
                PlayerMatState.Bounce => bounceMaterial,
                _ => steelMaterial
            };

            if (targetMat != null)
                ballRenderer.material = targetMat;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Bounce 状态：碰撞速度够就进入反弹模式
        if (gs != null && gs.CurrentMatState == PlayerMatState.Bounce)
        {
            if (rb.velocity.magnitude >= minBounceSpeed)
            {
                isBouncing = true;
            }
        }
    }
    /// <summary>
    /// 应用物理参数到 Rigidbody 和 Collider
    /// </summary>
    void ApplyProfile(PlayerPhysicsProfile profile)
    {
        if (profile == null) return;
        currentProfile = profile;
        rb.mass = profile.mass;
        rb.drag = profile.drag;
        rb.angularDrag = profile.angularDrag;
        if (col != null)
        {
            PhysicMaterial pm = col.sharedMaterial;
            if (pm == null)
            {
                pm = new PhysicMaterial("PlayerPhysMat");
                col.material = pm;
            }
            pm.bounciness = profile.bounciness;
            pm.dynamicFriction = profile.friction;
            pm.staticFriction = profile.friction;
            pm.bounceCombine = PhysicMaterialCombine.Maximum;
            pm.frictionCombine = PhysicMaterialCombine.Average;
        }
        Debug.Log($"ApplyProfile: mass={profile.mass}, drag={profile.drag}, " +
                  $"bounce={profile.bounciness}, friction={profile.friction}, speed={profile.moveSpeed}");
    }
    PlayerPhysicsProfile GetProfileForMat(PlayerMatState mat)
    {
        return mat switch
        {
            PlayerMatState.Steel => steelProfile,
            PlayerMatState.Glass => glassProfile,
            PlayerMatState.Bounce => bounceProfile,
            _ => steelProfile
        };
    }

}
