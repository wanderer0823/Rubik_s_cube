using UnityEngine;
using static InitCubeSlot;
using System.Collections;
using Unity.VisualScripting;

/// <summary>
/// 玩家与道具（Spring/Wind/Plate）的碰撞交互。
/// 挂在玩家物体上（带 Rigidbody + Collider）。
/// </summary>
public class ItemInteractionController : MonoBehaviour
{
    [Header("魔方数据引用")]
    public InitCubeSlot cubeData;

    [Header("Spring 设置")]
    public float springForce = 15f;

    [Header("Wind 设置")]
    public float windForce = 10f;

    [Header("门碰撞回弹力度")]
    public float doorBounceForce = 2f;

    public int groundLayer = 0;

    private Rigidbody rb;
    private GameState gs;
    public Vector3 windAddVelocity;

    private PlayerAction playerAction;
    public Animator animator;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        gs = GameState.Instance;
        playerAction = GetComponent<PlayerAction>();
        groundLayer = LayerMask.NameToLayer("Walls");
        
        if (cubeData == null)
        {
            var vmm = ViewModeManager.Instance;
            if (vmm != null)
                cubeData = vmm.cubeData;
            else
                /*__DEBUGTOOL_START__*/Debug.LogError("ItemInteractionController: 无法获取 ViewModeManager");/*__DEBUGTOOL_END__*/
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (gs == null) return;
        var mat = gs.CurrentMatState;

        if (other.CompareTag("Plate"))
        {
            if (mat == PlayerMatState.Steel || mat == PlayerMatState.Bounce)
            {
                HandlePlate(other);
            }
            else if (mat == PlayerMatState.Glass)
            {
                /*__DEBUGTOOL_START__*/Debug.Log("Glass + Plate: 无效果");/*__DEBUGTOOL_END__*/
            }
            return;
        }

        if (other.CompareTag("Spring"))
        {
            if ((mat == PlayerMatState.Glass || mat == PlayerMatState.Bounce )
                && gs.CurrentPlayerState!=PlayerState.isGrabbing)//添加握持时不可被弹起与播放动画
            {
                HandleSpring(other);
            }
            else if (mat == PlayerMatState.Steel)
            {
                /*__DEBUGTOOL_START__*/Debug.Log("Steel + Spring: 无效果");/*__DEBUGTOOL_END__*/
            }
            return;
        }

        if (other.gameObject.layer == groundLayer)
        {
            if (mat == PlayerMatState.Bounce)
            {
                MusicAudioManager.Instance.PlaySfx("bounce");
            }
            else if (mat == PlayerMatState.Steel)
            {
                MusicAudioManager.Instance.PlaySfx("steel");
            }
            else if (mat == PlayerMatState.Glass)
            {
                MusicAudioManager.Instance.PlaySfx("glass");
            }
        }

        if (other.CompareTag("Door"))
        {
            HandleDoorCollision(other);
        }

        if (other.CompareTag("Door2"))
        {
            ExecuteDoorTransition(other);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (gs == null) return;
        var mat = gs.CurrentMatState;

        if (!other.CompareTag("Wind")||gs.CurrentMatState==PlayerMatState.Steel || gs.CurrentPlayerState == PlayerState.isGrabbing)
            return;

        //if (mat == PlayerMatState.Glass || mat == PlayerMatState.Bounce)
        //{
        //    Transform fanModel = other.transform.parent;
        //    Vector3 windDir = fanModel.TransformDirection(Vector3.up).normalized;
        //    rb.AddForce(windDir * windForce * Time.fixedDeltaTime, ForceMode.Force);
        //}
        HandleWind(other);
    }
    //离开风扇范围加速度恢复
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Wind"))
            return;
        playerAction.moveAcceleration = 20f;
        windAddVelocity = Vector3.zero;
    }

    void HandlePlate(Collider plateCollider)
    {
        float minPressSpeed = playerAction != null ? playerAction.stopBounceYSpeed : 0f;
        if (rb == null || Mathf.Abs(rb.velocity.y) <= minPressSpeed)
            return;

        Plate plateLink = ResolvePlateLink(plateCollider);
        if (plateLink == null)
        {
            /*__DEBUGTOOL_START__*/Debug.LogWarning("Plate 缺少 PlateLink");/*__DEBUGTOOL_END__*/
            return;
        }

        plateLink.AddCount();
    }

    void HandleSpring(Collider springCollider)
    {
        Debug.Log($"IIC：执行HandleSpring（），玩家当前状态：{gs.CurrentPlayerState}");
        Transform springModel = springCollider.transform.parent;
        Vector3 launchDir = springModel.TransformDirection(Vector3.up).normalized;

        rb.AddForce(launchDir * springForce, ForceMode.Impulse);
        Animator anim = springModel.gameObject.GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetBool("Jump",true);
            MusicAudioManager.Instance.PlaySfx("banhuang");
            StartCoroutine(ResetJumpAfterDelay(anim, 1f));
        }

        /*__DEBUGTOOL_START__*/Debug.Log($"{gs.CurrentMatState} + Spring: 弹起，方向 {launchDir}, 力 {springForce}");/*__DEBUGTOOL_END__*/
    }
    IEnumerator ResetJumpAfterDelay(Animator anim, float delay)
    {
        yield return new WaitForSeconds(delay);
        anim.SetBool("Jump", false);
    }

    void HandleWind(Collider windCollider)
    {
        Transform fanModel = windCollider.transform.parent;
        Vector3 windDir = fanModel.TransformDirection(Vector3.forward).normalized;
        windAddVelocity += windDir * windForce * Time.fixedDeltaTime; // 增量添加
        if (playerAction != null)
        {
            Vector3 playerMoveDir = rb.velocity; // 归一化的移动输入方向
            float dot = Vector3.Dot(playerMoveDir, windDir);

            // 顺风：dot > 0.2 （夹角约78度以内）→ 增加加速度
            // 逆风：dot < -0.2 → 减少加速度
            // 侧风：中间范围 → 不变或缓慢恢复默认值

            float accelChange = 0f;
            if (dot > 0.2f)
            {
                accelChange = dot * 2f;   // 最大顺风时 +2（可调）
            }
            else if (dot < -0.2f)
            {
                accelChange = dot * 10f;   // dot为负，accelChange为负（如-0.5 → -1）
            }
            else if (dot > -0.2f && dot < 0.2f)
            {
                windAddVelocity += windDir * windForce *0.1f;
            }

                playerAction.moveAcceleration += accelChange * Time.fixedDeltaTime;
            playerAction.moveAcceleration = Mathf.Clamp(playerAction.moveAcceleration, 5f, 20f);
        }
    }

    void HandleDoorCollision(Collider doorCollider)
    {

        DoorController doorCtrl = doorCollider.GetComponentInParent<DoorController>();
        if (doorCtrl == null)
        {
            /*__DEBUGTOOL_START__*/Debug.Log("Door碰撞：未找到DoorController");/*__DEBUGTOOL_END__*/
            return;
        }
        
        var mat = gs.CurrentMatState;
        float playerSpeed = rb.velocity.magnitude;
        bool isPassable = doorCtrl.GetIsPassable();
        string doorMatName = doorCtrl.doorMat.ToString();
        string passStr = isPassable ? "1" : "0";
        string openStr = doorCtrl.IsOpened ? "1" : "0";

        if (!isPassable)
        {
            if (doorCtrl.targetRoomID < cubeData.rooms.Count && doorCtrl.targetRoomID > 0)
            {
                Debug.Log("你是强制传送,继续逻辑");

            }
            else
            {
                Debug.Log($"{doorMatName}, isPassable={passStr}, isOpened={openStr}, 玩家不可以通过，由于通道未连通");
                BounceBackFromDoor(doorCollider);
                return;
            }
        }

        //普通门
        if (doorCtrl.doorMat == DoorController.DoorMat.normal)
        {
            ExecuteDoorTransition(doorCollider);
        }
        //敞开的门
        if (doorCtrl.doorMat == DoorController.DoorMat.Open)
        {
            Debug.Log("玩家触发敞开的门");
            ExecuteDoorTransition(doorCollider);
        }

        if (mat == PlayerMatState.Steel)
        {
            if (doorCtrl.doorMat == DoorController.DoorMat.Soft)
            {
                /*__DEBUGTOOL_START__*/Debug.Log($"{doorMatName}, isPassable={passStr}, isOpened={openStr}, 玩家不可以通过，由于钢铁球无法撞碎软门");/*__DEBUGTOOL_END__*/
                BounceBackFromDoor(doorCollider);
                return;
            }

            if (doorCtrl.doorMat == DoorController.DoorMat.Hard && doorCtrl.IsOpened)
            {
                /*__DEBUGTOOL_START__*/Debug.Log($"{doorMatName}, isPassable={passStr}, isOpened={openStr}, 玩家可以通过");/*__DEBUGTOOL_END__*/
                ExecuteDoorTransition(doorCollider);
            }
            else
            {
                /*__DEBUGTOOL_START__*/Debug.Log($"{doorMatName}, isPassable={passStr}, isOpened={openStr}, 玩家不可以通过，由于硬门未被压力板打开");/*__DEBUGTOOL_END__*/
                BounceBackFromDoor(doorCollider);
            }

            return;
        }

        if (mat == PlayerMatState.Glass || mat == PlayerMatState.Bounce)
        {
            if (doorCtrl.doorMat == DoorController.DoorMat.Soft)
            {
                if (playerSpeed >= doorCtrl.softDoorHitSpeed)
                {
                    doorCtrl.Open();
                    /*__DEBUGTOOL_START__*/Debug.Log($"{doorMatName}, isPassable={passStr}, isOpened=1, 玩家可以通过，软门被撞碎(速度={playerSpeed:F1})");/*__DEBUGTOOL_END__*/
                    Animator animator=doorCollider.GetComponent<Animator>();
                    animator.SetBool("isBreaking", true);
                    MusicAudioManager.Instance.PlaySfx("wooddoor");
                    BounceBackFromDoor(doorCollider);
                    StartCoroutine(WaitForBreaking(1.0f, doorCollider.gameObject));

                }
                else
                {
                    /*__DEBUGTOOL_START__*/Debug.Log($"{doorMatName}, isPassable={passStr}, isOpened={openStr}, 玩家不可以通过，由于速度不足({playerSpeed:F1}<{doorCtrl.softDoorHitSpeed})");/*__DEBUGTOOL_END__*/
                    BounceBackFromDoor(doorCollider);
                }
                return;
            }

            if (doorCtrl.doorMat == DoorController.DoorMat.Hard && doorCtrl.IsOpened)
            {
                /*__DEBUGTOOL_START__*/Debug.Log($"{doorMatName}, isPassable={passStr}, isOpened={openStr}, 玩家可以通过");/*__DEBUGTOOL_END__*/
                ExecuteDoorTransition(doorCollider);
            }
            else
            {
                /*__DEBUGTOOL_START__*/Debug.Log($"{doorMatName}, isPassable={passStr}, isOpened={openStr}, 玩家不可以通过，由于硬门未被压力板打开");/*__DEBUGTOOL_END__*/
                BounceBackFromDoor(doorCollider);
            }

           
        }
       
    }

    void BounceBackFromDoor(Collider doorCollider)
    {
        Vector3 bounceDir = (transform.position - doorCollider.transform.position).normalized;
        bounceDir.y = 0;
        rb.velocity = Vector3.zero;
        rb.AddForce(bounceDir * doorBounceForce, ForceMode.Impulse);
    }

    void ExecuteDoorTransition(Collider doorCollider)
    {
        DoorController doorCtrl = doorCollider.GetComponentInParent<DoorController>();
        if (doorCtrl == null) return;

        var gsLocal = GameState.Instance;
        if (gsLocal == null) return;

        if (TryExecuteFixedRoomTransition(doorCtrl))
            return;

        int id = gsLocal.CurrentRoomID;
        Vector3Int doorDir = Vector3Int.RoundToInt(doorCtrl.DoorinRoomVector);
        Vector3Int oppositeDir = -doorDir;

        for (int i = 0; i < cubeData.rooms[id].dirMap.Length; i++)
        {
            if (doorDir != FaceOffset[cubeData.rooms[id].dirMap[i]])
                continue;

            FaceState face = cubeData.rooms[id].GetFace(cubeData.rooms[id].dirMap[i]);
            if (!face.isPassable)
                continue;

            RoomInstanceManager roomInstanceManager = FindObjectOfType<RoomInstanceManager>();
            foreach (var roomId in roomInstanceManager.GetNeighborRoomIds())
            {
                if (roomId == id)
                    continue;

                TryFindTrueNeighborRoom(roomId, oppositeDir);
                /*__DEBUGTOOL_START__*/Debug.Log("NeighborRoomID是——" + roomId);/*__DEBUGTOOL_END__*/
            }

            /*__DEBUGTOOL_START__*/Debug.Log("开门成功，传送到" + GameState.Instance.CurrentRoomID);/*__DEBUGTOOL_END__*/
            if (playerAction != null)
                playerAction.ResetToStartPosition();
            GameEvents.onRoomTransitionExecute(GameState.Instance.CurrentRoomID);
            GameEvents.calculateNeighbors();
            RoomGameObjectManager.Instance.LoadCurrentRoomGameObject();
            break;
        }
    }

    private bool TryExecuteFixedRoomTransition(DoorController doorCtrl)
    {
        if (doorCtrl == null || doorCtrl.targetRoomID < 0)
            return false;

        if (cubeData == null || doorCtrl.targetRoomID >= cubeData.rooms.Count)
        {
            /*__DEBUGTOOL_START__*/Debug.LogWarning($"门 {doorCtrl.gameObject.name} 的固定传送房间ID {doorCtrl.targetRoomID} 无效（超出范围或 cubeData 为空），继续执行原有传送逻辑");/*__DEBUGTOOL_END__*/
            return false;
        }

        var targetRoom = cubeData.rooms[doorCtrl.targetRoomID];
        if (targetRoom == null)
        {
            /*__DEBUGTOOL_START__*/Debug.LogWarning($"门 {doorCtrl.gameObject.name} 的固定传送房间ID {doorCtrl.targetRoomID} 对应的房间数据为 null，继续执行原有传送逻辑");/*__DEBUGTOOL_END__*/
            return false;
        }

        GameState.Instance.CurrentRoomID = doorCtrl.targetRoomID;
        GameState.Instance.RefreshCurrentSurfaceFromRoomID();
        /*__DEBUGTOOL_START__*/Debug.Log($"门 {doorCtrl.gameObject.name} 在门逻辑执行完成后传送到固定房间 {doorCtrl.targetRoomID}");/*__DEBUGTOOL_END__*/

        if (playerAction != null)
            playerAction.ResetToStartPosition();
        GameEvents.onRoomTransitionExecute(GameState.Instance.CurrentRoomID);
        GameEvents.calculateNeighbors();
        return true;
    }

    private void TryFindTrueNeighborRoom(int id, Vector3Int oppositeDoorDir)
    {
        for (int i = 0; i < cubeData.rooms[id].dirMap.Length; i++)
        {
            if (oppositeDoorDir != FaceOffset[cubeData.rooms[id].dirMap[i]])
                continue;

            FaceState face = cubeData.rooms[id].GetFace(cubeData.rooms[id].dirMap[i]);
            if (face.isPassable)
            {
                GameState.Instance.CurrentRoomID = id;
                GameState.Instance.RefreshCurrentSurfaceFromRoomID();
            }
            else
            {
                /*__DEBUGTOOL_START__*/Debug.Log("开门失败");/*__DEBUGTOOL_END__*/
            }
        }
    }

    Plate ResolvePlateLink(Collider plateCollider)
    {
        return plateCollider.GetComponentInParent<Plate>();
    }
    private IEnumerator WaitForBreaking(float delay, GameObject door)
    {
        yield return new WaitForSeconds(delay);
        door.transform.parent.GetChild(0).gameObject.SetActive(false);  //隐藏门的形
        door.transform.parent.GetChild(1).gameObject.SetActive(true);//触发开门的真正门
    }
}
