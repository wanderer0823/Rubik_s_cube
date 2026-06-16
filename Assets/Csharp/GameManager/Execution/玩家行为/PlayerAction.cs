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

    [Header("检测设置")]
    public float interactRange = 3.0f;

    [Header("小球材质显示（View1/2可见的Sphere）")]
    public Renderer ballRenderer;
    public Material steelMaterial;
    public Material glassMaterial;
    public Material bounceMaterial;

    // ===== 新增：自动越障（台阶攀爬）参数 =====
    [Header("自动越障 (台阶攀爬)")]
    public bool enableAutoStep = true;
    public float stepHeight = 0.3f;          // 最大可跨越高度
    public float stepCheckDistance = 0.5f;   // 向前探测距离
    public float stepUpSpeed = 3f;           // 抬升速度（瞬时）
    public bool showDebugLines = true;       // 是否在Scene视图中显示检测线
    private bool isStepping = false;

    private Rigidbody rb;
    private Collider col;
    private GameState gs;
    private bool isBouncing = false;
    private bool hasActiveBounceJump = false;
    private PlayerPhysicsProfile currentProfile;

    private ItemInteractionController IIC;
    private float cameraShakeSpeed = 1.0f;

    // 移动平滑相关（原注释部分，保留）
    // private CharacterController controller;
    // private Vector3 CurrentMoveVelocity; ...

    void OnEnable()
    {
        GameEvents.OnTabExecute += OnTabPressed;
        GameEvents.OnMoveExecute += Move;
        //GameEvents.OnOpenDoorExecute += TryOpenDoor;
        GameEvents.OnMatChangeExecute += OnMatChanged;
    }

    void OnDisable()
    {
        GameEvents.OnTabExecute -= OnTabPressed;
        GameEvents.OnMoveExecute -= Move;
        //GameEvents.OnOpenDoorExecute -= TryOpenDoor;
        GameEvents.OnMatChangeExecute -= OnMatChanged;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        IIC = GetComponent<ItemInteractionController>();
    }

    void Start()
    {
        gs = GameState.Instance;
        ApplyProfile(steelProfile);
        ResetToStartPosition();
    }

    void FixedUpdate()
    {
        if (gs == null)
            return;
        
        // ===== 新增：自动台阶检测（仅当未弹跳且有水平移动时） =====
        if (enableAutoStep && !isBouncing)
        {
            TryAutoStep();
        }

        // ===== 原有的 Bounce 逻辑 =====
        if (gs.CurrentMatState != PlayerMatState.Bounce)
        {
            hasActiveBounceJump = false;
            return;
        }

        float absYSpeed = Mathf.Abs(rb.velocity.y);
        isBouncing = absYSpeed > stopBounceYSpeed;

        if (absYSpeed >= stopBounceYSpeed)
            hasActiveBounceJump = true;

        if (hasActiveBounceJump && absYSpeed < stopBounceYSpeed && transform.position.y < 38f)
        {
            hasActiveBounceJump = false;
            isBouncing = false;
            ResetUnpressedPlates();
            Debug.Log("Bounce跳跃结束，已重置压力板");
        }

        
    }

    // ===== 新增：自动台阶检测核心方法 =====
    private void TryAutoStep()
    {
        // ===== 1. 计算射线参数 =====
        Vector3 footPos = col.bounds.min;
        Vector3 origin = new Vector3(transform.position.x, footPos.y + 0.05f, transform.position.z);
        Vector3 direction = transform.forward;

        // 调试绘图
        if (showDebugLines)
        {
            Debug.DrawRay(origin, direction * stepCheckDistance, Color.green);
            Debug.DrawRay(origin, Vector3.up * 0.05f, Color.cyan);
        }

        // ===== 2. 地面检测 =====
        float footHeight = col.bounds.extents.y;
        Vector3 groundOrigin = transform.position + Vector3.down * (footHeight - 0.05f);
        bool isGrounded = Physics.Raycast(groundOrigin, Vector3.down, 0.1f);

        if (!isGrounded)
        {
            if (Time.frameCount % 60 == 0)
                Debug.Log("[AutoStep] 未着地");
            return;
        }

        // ===== 3. 脚部射线检测（忽略高度和斜坡） =====
        RaycastHit lowHit;
        bool hitLow = Physics.Raycast(origin, direction, out lowHit, stepCheckDistance);

        if (!hitLow)
        {
            if (Time.frameCount % 60 == 0)
                Debug.Log("[AutoStep] 前方无物");
            isStepping = false;
            return;
        }

        // 检测到任何物体
        Debug.Log($"[AutoStep] 检测到物体: {lowHit.collider.name}，距离 {lowHit.distance}");

        // ===== 4. 头顶空间检测 =====
        Vector3 upperOrigin = transform.position + Vector3.up * stepHeight;
        bool hitUpper = Physics.Raycast(upperOrigin, direction, stepCheckDistance);

        if (hitUpper)
        {
            Debug.LogWarning("[AutoStep] 头顶被挡住");
            return;
        }

        // ===== 5. 执行抬升 =====
        Debug.Log($"[AutoStep] 抬升！施加 {stepUpSpeed} m/s");
        rb.velocity += Vector3.up * stepUpSpeed;
        isStepping = true;
    }

    // ===== 原有方法：Tab、Move、材质切换等（无改动，保留） =====

    void OnTabPressed()
    {
        Debug.Log("打开/关闭背包系统。");
    }

    public float moveAcceleration = 20f;
    public float brakeDeceleration = 30f;
    public Vector3 deltaHorVelocity;

    void Move(Vector3 moveDir)
    {
        moveDir = transform.right * moveDir.x + transform.forward * moveDir.z;
        float targetSpeed = moveSpeed;

        Vector3 targetHorVelocity = moveDir.normalized * targetSpeed;
        Vector3 currentHorVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);

        if (moveDir.magnitude > 0.1f)
        {
            deltaHorVelocity = targetHorVelocity - currentHorVelocity;
            float maxDelta = moveAcceleration * Time.fixedDeltaTime;
            deltaHorVelocity = Vector3.ClampMagnitude(deltaHorVelocity, maxDelta);
            if (!isBouncing)
            {
                GameEvents.onWalkMovement(rb.velocity);
            }
        }
        else
        {
            float brake = brakeDeceleration * Time.fixedDeltaTime;
            float currentSpeed = currentHorVelocity.magnitude;
            float decel = Mathf.Min(currentSpeed, brake);
            deltaHorVelocity = -currentHorVelocity.normalized * decel;
            if (float.IsNaN(deltaHorVelocity.x))
                deltaHorVelocity = Vector3.zero;
            GameEvents.onStopMovement();
        }

        Vector3 newVelocity = rb.velocity;
        newVelocity.x += deltaHorVelocity.x;
        newVelocity.z += deltaHorVelocity.z;
        rb.velocity = newVelocity + IIC.windAddVelocity;
    }

    void OnMatChanged(PlayerMatState newMat)
    {
        Debug.Log($"PlayerAction: 材质切换为 {newMat}");

        PlayerPhysicsProfile profile = GetProfileForMat(newMat);
        ApplyProfile(profile);

        isBouncing = false;
        hasActiveBounceJump = false;

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

    void ApplyProfile(PlayerPhysicsProfile profile)
    {
        if (profile == null) return;
        currentProfile = profile;
        rb.mass = profile.mass;
        rb.drag = profile.drag;
        rb.angularDrag = profile.angularDrag;
        if (col != null)
        {
            PhysicMaterial pm = new PhysicMaterial("PlayerPhysMat");
            pm.bounciness = profile.bounciness;
            pm.dynamicFriction = profile.friction;
            pm.staticFriction = profile.friction;
            pm.bounceCombine = PhysicMaterialCombine.Maximum;
            pm.frictionCombine = PhysicMaterialCombine.Average;
            col.sharedMaterial = pm;
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
        ContinuousCollisionDetector3D ccd = gameObject.GetComponent<ContinuousCollisionDetector3D>();
        if (ccd != null)
            ccd.OnTeleport(transform.position);

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

    // ===== 可选：在 Scene 视图中绘制更多辅助信息（Gizmos） =====
    private void OnDrawGizmos()
    {
        if (!showDebugLines || !Application.isPlaying)
            return;

        // 绘制一个半透明的方块表示可检测的高度范围
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Vector3 center = transform.position + Vector3.up * (stepHeight * 0.5f);
        Vector3 size = new Vector3(0.2f, stepHeight, 0.2f);
        Gizmos.DrawCube(center, size);
    }
}