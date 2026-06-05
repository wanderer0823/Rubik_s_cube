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
    public float stopBounceYSpeed = 0.2f;

    [Header("移动设置")]
    public float moveSpeed = 5f;

    [Header("生成设置")]
    public bool ignoreFixedSpawnPosition = false;
    public Transform startPositionOverride;
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
    private bool hasActiveBounceJump = false;
    // 当前生效的 profile
    private PlayerPhysicsProfile currentProfile;

    private ItemInteractionController IIC;
    private float cameraShakeSpeed = 1.0f;
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
        IIC= GetComponent<ItemInteractionController>();
    }

    void Start()///YIu
    {
        gs = GameState.Instance;
        ApplyProfile(steelProfile);

        ResetToStartPosition();
    }
    void FixedUpdate()//Yiu
    {
        if (gs == null)
            return;

        if (gs.CurrentMatState != PlayerMatState.Bounce)
        {
            hasActiveBounceJump = false;
            return;
        }

        float absYSpeed = Mathf.Abs(rb.velocity.y);
        isBouncing = absYSpeed > stopBounceYSpeed;

        if (absYSpeed >= stopBounceYSpeed)
            hasActiveBounceJump = true;

        // 只要已经进入过一次跳跃过程，后续 y 速度跌破阈值就立刻重置
        if (hasActiveBounceJump && absYSpeed < stopBounceYSpeed && transform.position.y < 38f)
        {
            hasActiveBounceJump = false;
            isBouncing = false;
            ResetUnpressedPlates();
            /*__DEBUGTOOL_START__*/Debug.Log("Bounce跳跃结束，已重置压力板");/*__DEBUGTOOL_END__*/
        }
    }

    //玩家打开/关闭背包系统的UI
    void OnTabPressed()
    {
        /*__DEBUGTOOL_START__*/Debug.Log("打开/关闭背包系统。");/*__DEBUGTOOL_END__*/
    }
    //玩家wasd移动
    // 可调参数：移动加速度（控制响应速度）
    public float moveAcceleration = 20f;
    // 可调参数：地面/空中刹车减速度
    public float brakeDeceleration = 30f;
    public Vector3 deltaHorVelocity;
    void Move(Vector3 moveDir)
    {
        // 获取输入方向（本地转世界）
        moveDir = transform.right * moveDir.x + transform.forward * moveDir.z;
        float targetSpeed = moveSpeed;

        // 期望的水平速度方向
        Vector3 targetHorVelocity = moveDir.normalized * targetSpeed;
        Vector3 currentHorVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);

        
        if (moveDir.magnitude > 0.1f)
        {
            // 有输入：向目标速度加速
            deltaHorVelocity = targetHorVelocity - currentHorVelocity;
            // 限制单帧最大加速度
            float maxDelta = moveAcceleration * Time.fixedDeltaTime;
            deltaHorVelocity = Vector3.ClampMagnitude(deltaHorVelocity, maxDelta);
            // Yiu: 新增镜头摇晃
            if (!isBouncing)
            {
                GameEvents.onWalkMovement(rb.velocity);//由view3 的CA监听
            }
        }
        else
        {
            // 无输入：刹车减速
            float brake = brakeDeceleration * Time.fixedDeltaTime;
            float currentSpeed = currentHorVelocity.magnitude;
            float decel = Mathf.Min(currentSpeed, brake);
            deltaHorVelocity = -currentHorVelocity.normalized * decel;
            if (float.IsNaN(deltaHorVelocity.x))
                deltaHorVelocity = Vector3.zero;
            //Yiu:镜头控制
            GameEvents.onStopMovement();
        }

        // 叠加到速度上（保留 Y 轴）
        Vector3 newVelocity = rb.velocity;
        newVelocity.x += deltaHorVelocity.x;
        newVelocity.z += deltaHorVelocity.z;
        rb.velocity = newVelocity+IIC.windAddVelocity;
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
                    transform.position = GetResolvedStartPosition();
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
        /*__DEBUGTOOL_START__*/Debug.Log($"PlayerAction: 材质切换为 {newMat}");/*__DEBUGTOOL_END__*/

        // 物理参数切换
        PlayerPhysicsProfile profile = GetProfileForMat(newMat);
        ApplyProfile(profile);

        // 切换材质时取消反弹状态
        isBouncing = false;
        hasActiveBounceJump = false;

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
        /*__DEBUGTOOL_START__*/Debug.Log($"ApplyProfile: mass={profile.mass}, drag={profile.drag}, " +
                  $"bounce={profile.bounciness}, friction={profile.friction}, speed={profile.moveSpeed}");/*__DEBUGTOOL_END__*/
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
    private Vector3 externalAcceleration = Vector3.zero;

    public void AddExternalAcceleration(Vector3 deltaVelocity)
    {
        externalAcceleration += deltaVelocity;
    }

    private Vector3 GetResolvedStartPosition()
    {
        if (startPositionOverride == null)
            return transform.position;

        return startPositionOverride.position;
    }

    public void ResetToStartPosition()
    {
        if (ignoreFixedSpawnPosition)
            return;

        transform.position = GetResolvedStartPosition();

        if (rb == null)
            return;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void ResetUnpressedPlates()
    {
        foreach (Plate plate in FindObjectsOfType<Plate>())
        {
            if (plate != null && !plate.isPressed)
                plate.ResetPlate();
        }
    }

}
