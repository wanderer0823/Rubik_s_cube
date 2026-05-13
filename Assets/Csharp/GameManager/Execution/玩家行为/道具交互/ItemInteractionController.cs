using UnityEngine;
using static InitCubeSlot;

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

    private Rigidbody rb;
    private GameState gs;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        gs = GameState.Instance;
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
                Debug.Log("Glass + Plate: 无效果");
            }
            return;
        }

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
            return;
        }

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
            return;
        }

        if (other.CompareTag("Door"))
        {
            HandleDoorCollision(other);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (gs == null) return;
        var mat = gs.CurrentMatState;

        if (!other.CompareTag("Wind"))
            return;

        if (mat == PlayerMatState.Glass || mat == PlayerMatState.Bounce)
        {
            Transform fanModel = other.transform.parent;
            Vector3 windDir = fanModel.TransformDirection(Vector3.up).normalized;
            rb.AddForce(windDir * windForce * Time.fixedDeltaTime, ForceMode.Force);
        }
    }

    void HandlePlate(Collider plateCollider)
    {
        Plate plateLink = ResolvePlateLink(plateCollider);
        if (plateLink == null)
        {
            Debug.LogWarning("Plate 缺少 PlateLink");
            return;
        }

        plateLink.AddCount();
    }

    void HandleSpring(Collider springCollider)
    {
        Transform springModel = springCollider.transform.parent;
        Vector3 launchDir = springModel.TransformDirection(Vector3.up).normalized;

        rb.AddForce(launchDir * springForce, ForceMode.Impulse);
        Debug.Log($"{gs.CurrentMatState} + Spring: 弹起，方向 {launchDir}, 力 {springForce}");
    }

    void HandleWind(Collider windCollider)
    {
        Transform fanModel = windCollider.transform.parent;
        Vector3 windDir = fanModel.TransformDirection(Vector3.up).normalized;

        rb.AddForce(windDir * windForce, ForceMode.Impulse);
        Debug.Log($"{gs.CurrentMatState} + Wind: 风力，方向 {windDir}, 力 {windForce}");
    }

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

        if (!isPassable)
        {
            Debug.Log($"{doorMatName}, isPassable={passStr}, isOpened={openStr}, 玩家不可以通过，由于通道未连通");
            BounceBackFromDoor(doorCollider);
            return;
        }

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
                Debug.Log("NeighborRoomID是——" + roomId);
            }

            Debug.Log("开门成功，传送到" + GameState.Instance.CurrentRoomID);
            transform.position = new Vector3(0, 40, 0);
            GameEvents.onRoomTransitionExecute(GameState.Instance.CurrentRoomID);
            GameEvents.calculateNeighbors();
            break;
        }
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
                Debug.Log("开门失败");
            }
        }
    }

    Plate ResolvePlateLink(Collider plateCollider)
    {
        return plateCollider.GetComponentInParent<Plate>();
    }
}
