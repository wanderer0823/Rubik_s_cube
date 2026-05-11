using UnityEngine;
using static InitCubeSlot;

/// <summary>
/// 玩家与道具（Spring/Wind/Plate）的碰撞交互。
/// 挂在玩家物体上（带 Rigidbody + Collider）。
/// 不走事件总线，直接用 OnTriggerEnter/Stay/Exit。
/// </summary>
public class ItemInteractionController : MonoBehaviour
{
    [Header("魔方数据引用")]
    public InitCubeSlot cubeData;

    [Header("Plate 设置")]
    public float plateMoveDistance = 1f;
    public float plateMoveSpeed = 2f;

    [Header("Spring 设置")]
    public float springForce = 15f;

    [Header("Wind 设置")]
    public float windForce = 10f;

    [Header("Bounce + Plate 设置")]
    public int bounceCountRequired = 3;

    [Header("门碰撞回弹力度")]
    public float doorBounceForce = 2f;        // 撞门弹回力度

    private Rigidbody rb;
    private GameState gs;
    // Bounce + Plate 计数
    private int bounceCountOnPlate = 0;
    private Transform currentPlateRoot = null;
    private int activePlateContactCount = 0;
    private Coroutine plateResetCoroutine = null;

    [Header("Bounce + Plate 归零延迟")]
    public float plateResetDelay = 2f;               // 新增：离开后多久归零

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        gs = GameState.Instance;
    }

    // ==================== Trigger 进入 ====================
    void OnTriggerEnter(Collider other)
    {
        if (gs == null) return;
        var mat = gs.CurrentMatState;

        // ---------- Plate ----------
        if (other.CompareTag("Plate"))
        {
            if (mat == PlayerMatState.Steel)
            {
                HandleSteelPlate(other);
            }
            else if (mat == PlayerMatState.Bounce)
            {
                HandleBouncePlateEnter(other);
            }
            else if(mat == PlayerMatState.Glass)
            {
                Debug.Log("Glass + Plate: 无效果");
            }
            else
            return;
        }

        // ---------- Spring ----------
        if (other.CompareTag("Spring"))
        {
            if (mat == PlayerMatState.Glass || mat == PlayerMatState.Bounce)
            {
                HandleSpring(other);
            }
            else if (mat == PlayerMatState.Steel)
            {
                Debug.Log("Steel + Spring: 无效果");
            }
            else
            return;
        }

        // ---------- Wind ----------
        if (other.CompareTag("Wind"))
        {
            if (mat == PlayerMatState.Glass || mat == PlayerMatState.Bounce)
            {
                HandleWind(other);
            }
            else if (mat == PlayerMatState.Steel)
            {
                Debug.Log("Steel + Wind: 无效果");
            }
            else return;
        }
        // ---------- Door ----------
        if (other.CompareTag("Door"))
        {
            HandleDoorCollision(other);
            return;
        }
    }

    // ==================== Trigger 持续（Wind 持续吹）====================
    void OnTriggerStay(Collider other)
    {
        if (gs == null) return;
        var mat = gs.CurrentMatState;

        if (other.CompareTag("Wind"))
        {
            if (mat == PlayerMatState.Glass || mat == PlayerMatState.Bounce)
            {
                // 持续施加风力
                Transform fanModel = other.transform.parent;
                Vector3 windDir = fanModel.TransformDirection(Vector3.up).normalized;
                rb.AddForce(windDir * windForce * Time.fixedDeltaTime, ForceMode.Force);
            }
        }
    }

    // ==================== Trigger 离开 ====================
    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Plate"))
            return;

        Transform plateRoot = ResolvePlateTransform(other);
        if (plateRoot != currentPlateRoot)
            return;

        activePlateContactCount = Mathf.Max(0, activePlateContactCount - 1);
        if (activePlateContactCount > 0)
            return;

        // 延迟归零，给弹力球时间弹回来
        if (plateResetCoroutine != null)
            StopCoroutine(plateResetCoroutine);
        plateResetCoroutine = StartCoroutine(DelayedPlateReset());
    }

    void HandleBouncePlateEnter(Collider plateCollider)
    {
        Transform plateRoot = ResolvePlateTransform(plateCollider);

        // 在超时前弹回同一块板时，保留当前累计次数。
        if (plateResetCoroutine != null)
        {
            StopCoroutine(plateResetCoroutine);
            plateResetCoroutine = null;
        }

        if (currentPlateRoot != null && currentPlateRoot != plateRoot)
            ResetBouncePlateTracking(clearBounceProgress: true);

        currentPlateRoot = plateRoot;

        bool wasOutsidePlate = activePlateContactCount == 0;
        activePlateContactCount++;

        // 同一块 Plate 就算有多个触发器，也只在一次落板的首次接触计数。
        if (!wasOutsidePlate)
            return;

        // 只在下落接触 Plate 时计数。
        if (rb == null || rb.velocity.y >= 0f)
            return;

        bounceCountOnPlate++;
        Debug.Log($"Bounce踩Plate 次数: {bounceCountOnPlate}/{bounceCountRequired}");

        if (bounceCountOnPlate >= bounceCountRequired)
            HandleBouncePlate(plateCollider);
    }

    System.Collections.IEnumerator DelayedPlateReset()
    {
        yield return new WaitForSeconds(plateResetDelay);
        Debug.Log("Plate计数超时归零");
        ResetBouncePlateTracking(clearBounceProgress: true);
        plateResetCoroutine = null;
    }

    // ==================== 交互处理 ====================

    void HandleSteelPlate(Collider plateCollider)
    {
        Transform plateModel = ResolvePlateTransform(plateCollider);
        PlateLink link = ResolvePlateLink(plateCollider);

        // 已下压过则跳过
        if (link != null && link.isPressed)
        {
            Debug.Log("Plate 已下压，跳过");
            return;
        }

        Debug.Log("Steel + Plate: 压力板触发！");
        StartCoroutine(MovePlate(plateModel, Vector3.down * plateMoveDistance));

        // 标记已下压
        if (link != null)
        {
            link.isPressed = true;
            if (link.linkedDoor != null)
            {
                link.linkedDoor.Open();
                Debug.Log($"关联门 [{link.linkedDoor.gameObject.name}] 已打开");
            }
        }
    }

    void HandleBouncePlate(Collider plateCollider)
    {
        Transform plateModel = ResolvePlateTransform(plateCollider);
        PlateLink link = ResolvePlateLink(plateCollider);

        if (link != null && link.isPressed)
        {
            Debug.Log("Plate 已下压，跳过");
            return;
        }

        Debug.Log($"Bounce + Plate: 踩满{bounceCountRequired}次，压力板触发！");
        StartCoroutine(MovePlate(plateModel, Vector3.down * plateMoveDistance));

        if (link != null)
        {
            link.isPressed = true;
            if (link.linkedDoor != null)
            {
                link.linkedDoor.Open();
            }
        }

        ResetBouncePlateTracking(clearBounceProgress: true);
    }

    void HandleSpring(Collider springCollider)
    {
        Transform springModel = springCollider.transform.parent;

        // 弹起方向 = 弹簧模型本地+Y转世界方向
        Vector3 launchDir = springModel.TransformDirection(Vector3.up).normalized;

        rb.AddForce(launchDir * springForce, ForceMode.Impulse);
        Debug.Log($"{gs.CurrentMatState} + Spring: 弹起！方向={launchDir}, 力={springForce}");

        // TODO: 播放弹簧压缩动画
    }

    void HandleWind(Collider windCollider)
    {
        Transform fanModel = windCollider.transform.parent;

        // 瞬时风力 = 风扇模型本地+Y转世界方向
        Vector3 windDir = fanModel.TransformDirection(Vector3.up).normalized;

        rb.AddForce(windDir * windForce, ForceMode.Impulse);
        Debug.Log($"{gs.CurrentMatState} + Wind: 风力！方向={windDir}, 力={windForce}");

        // TODO: 禁止 fan -Y 方向移动（后续在 PlayerAction.Move 里过滤）
    }

    // ==================== 门碰撞 ====================

    void HandleDoorCollision(Collider doorCollider)
    {
        DoorController doorCtrl = doorCollider.GetComponentInParent<DoorController>();
        if (doorCtrl == null)
        {
            Debug.Log("Door碰撞：未找到DoorController");
            return;
        }

        var mat = gs.CurrentMatState;
        float playerSpeed = rb.velocity.magnitude;
        bool isPassable = doorCtrl.GetIsPassable();
        string doorMatName = doorCtrl.doorMat.ToString();
        string passStr = isPassable ? "1" : "0";
        string openStr = doorCtrl.IsOpened ? "1" : "0";

        // 1. 通道未连通
        if (!isPassable)
        {
            Debug.Log($"{doorMatName}, isPassable={passStr}, isOpened={openStr}, 玩家不可以通过，由于通道未连通");
            BounceBackFromDoor(doorCollider);
            // TODO: 广播UI提示
            return;
        }

        // 2. Steel
        if (mat == PlayerMatState.Steel)
        {
            if (doorCtrl.doorMat == DoorController.DoorMat.Soft)
            {
                Debug.Log($"{doorMatName}, isPassable={passStr}, isOpened={openStr}, 玩家不可以通过，由于钢铁球无法撞碎软门");
                BounceBackFromDoor(doorCollider);
                return;
            }

            if (doorCtrl.doorMat == DoorController.DoorMat.Hard && doorCtrl.IsOpened)
            {
                Debug.Log($"{doorMatName}, isPassable={passStr}, isOpened={openStr}, 玩家可以通过");
                ExecuteDoorTransition(doorCollider);
            }
            else
            {
                Debug.Log($"{doorMatName}, isPassable={passStr}, isOpened={openStr}, 玩家不可以通过，由于硬门未被压力板打开");
                BounceBackFromDoor(doorCollider);
            }
            return;
        }

        // 3. Glass / Bounce
        if (mat == PlayerMatState.Glass || mat == PlayerMatState.Bounce)
        {
            if (doorCtrl.doorMat == DoorController.DoorMat.Soft)
            {
                if (playerSpeed >= doorCtrl.softDoorHitSpeed)
                {
                    doorCtrl.Open();
                    Debug.Log($"{doorMatName}, isPassable={passStr}, isOpened=1, 玩家可以通过，软门被撞碎(速度={playerSpeed:F1})");
                    ExecuteDoorTransition(doorCollider);
                }
                else
                {
                    Debug.Log($"{doorMatName}, isPassable={passStr}, isOpened={openStr}, 玩家不可以通过，由于速度不足({playerSpeed:F1}<{doorCtrl.softDoorHitSpeed})");
                    BounceBackFromDoor(doorCollider);
                }
                return;
            }

            if (doorCtrl.doorMat == DoorController.DoorMat.Hard && doorCtrl.IsOpened)
            {
                Debug.Log($"{doorMatName}, isPassable={passStr}, isOpened={openStr}, 玩家可以通过");
                ExecuteDoorTransition(doorCollider);
            }
            else
            {
                Debug.Log($"{doorMatName}, isPassable={passStr}, isOpened={openStr}, 玩家不可以通过，由于硬门未被压力板打开");
                BounceBackFromDoor(doorCollider);
            }
            return;
        }
    }

    /// <summary>
    /// 撞门弹回
    /// </summary>
    void BounceBackFromDoor(Collider doorCollider)
    {
        Vector3 bounceDir = (transform.position - doorCollider.transform.position).normalized;
        bounceDir.y = 0; // 只水平弹回
        rb.velocity = Vector3.zero;
        rb.AddForce(bounceDir * doorBounceForce, ForceMode.Impulse);
    }

    /// <summary>
    /// 执行过门传送（调用原有逻辑）
    /// </summary>
    void ExecuteDoorTransition(Collider doorCollider)
    {
        DoorController doorCtrl = doorCollider.GetComponentInParent<DoorController>();
        if (doorCtrl == null) return;

        var gs_local = GameState.Instance;
        if (gs_local == null) return;

        int id = gs_local.CurrentRoomID;
        Vector3Int DoorDir = Vector3Int.RoundToInt(doorCtrl.DoorinRoomVector);
        Vector3Int oppositeDir = -DoorDir;

        for (int i = 0; i < cubeData.rooms[id].dirMap.Length; i++)
        {
            if (DoorDir == FaceOffset[cubeData.rooms[id].dirMap[i]])
            {
                FaceState face = cubeData.rooms[id].GetFace(cubeData.rooms[id].dirMap[i]);
                if (face.isPassable)
                {
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
                    Debug.Log("开门成功，传送到" + GameState.Instance.CurrentRoomID);
                    RoomPreloadController rpc = FindObjectOfType<RoomPreloadController>();
                    transform.position = new Vector3(0, 40, 0);
                    rpc.TriggerPreloadComplete();
                    // ������֪ͨС���ƶ�
                    GameEvents.onRoomTransitionExecute(GameState.Instance.CurrentRoomID);

                    break;
                }
            }
        }
    }

    private void TryFindTrueNeighborRoom(int id, Vector3Int ODoorDir)
    {
        for (int i = 0; i < cubeData.rooms[id].dirMap.Length; i++)
        {
            if (ODoorDir == FaceOffset[cubeData.rooms[id].dirMap[i]])
            {
                FaceState face = cubeData.rooms[id].GetFace(cubeData.rooms[id].dirMap[i]);
                if (face.isPassable)
                {
                    GameState.Instance.CurrentRoomID = id;
                }
                else
                {
                    Debug.Log("开门失败2");
                }
            }
        }
    }


    // ==================== 工具 ====================

    Transform ResolvePlateTransform(Collider plateCollider)
    {
        PlateLink link = ResolvePlateLink(plateCollider);
        if (link != null)
            return link.transform;

        if (plateCollider.transform.parent != null)
            return plateCollider.transform.parent;

        return plateCollider.transform;
    }

    PlateLink ResolvePlateLink(Collider plateCollider)
    {
        return plateCollider.GetComponentInParent<PlateLink>();
    }

    void ResetBouncePlateTracking(bool clearBounceProgress)
    {
        activePlateContactCount = 0;
        currentPlateRoot = null;

        if (clearBounceProgress)
            bounceCountOnPlate = 0;
    }

    System.Collections.IEnumerator MovePlate(Transform plate, Vector3 offset)
    {
        Vector3 start = plate.position;
        Vector3 end = start + offset;
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * plateMoveSpeed;
            plate.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
        plate.position = end;
    }
}
