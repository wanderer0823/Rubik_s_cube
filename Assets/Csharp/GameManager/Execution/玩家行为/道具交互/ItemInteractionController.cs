using UnityEngine;
using static InitCubeSlot;
using System.Collections;
using Unity.VisualScripting;

/// <summary>
/// 鐜╁涓庨亾鍏凤紙Spring/Wind/Plate锛夌殑纰版挒浜や簰銆?
/// 鎸傚湪鐜╁鐗╀綋涓婏紙甯?Rigidbody + Collider锛夈€?
/// </summary>
public class ItemInteractionController : MonoBehaviour
{
    [Header("InitCubeSlot")]
    public InitCubeSlot cubeData;

    [Header("Spring Force")]
    public float springForce = 15f;

    [Header("Wind Force")]
    public float windForce = 10f;

    [Header("Door Force")]
    public float doorBounceForce = 2f;

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
            }
            return;
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

        if (!other.CompareTag("Wind"))
            return;

        //if (mat == PlayerMatState.Glass || mat == PlayerMatState.Bounce)
        //{
        //    Transform fanModel = other.transform.parent;
        //    Vector3 windDir = fanModel.TransformDirection(Vector3.up).normalized;
        //    rb.AddForce(windDir * windForce * Time.fixedDeltaTime, ForceMode.Force);
        //}
        HandleWind(other);
    }
    //绂诲紑椋庢墖鑼冨洿鍔犻€熷害鎭㈠
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
            Debug.LogWarning("Plate 缂哄皯 PlateLink");
            return;
        }

        plateLink.AddCount();
    }

    void HandleSpring(Collider springCollider)
    {
        Transform springModel = springCollider.transform.parent;
        Vector3 launchDir = springModel.TransformDirection(Vector3.up).normalized;

        rb.AddForce(launchDir * springForce, ForceMode.Impulse);
        Animator anim = springModel.gameObject.GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetBool("Jump",true);
            StartCoroutine(ResetJumpAfterDelay(anim, 1f));
        }

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
        windAddVelocity += windDir * windForce * Time.fixedDeltaTime; // 澧為噺娣诲姞
        if (playerAction != null)
        {
            Vector3 playerMoveDir = rb.velocity; // 褰掍竴鍖栫殑绉诲姩杈撳叆鏂瑰悜
            float dot = Vector3.Dot(playerMoveDir, windDir);

            // 椤洪锛歞ot > 0.2 锛堝す瑙掔害78搴︿互鍐咃級鈫?澧炲姞鍔犻€熷害
            // 閫嗛锛歞ot < -0.2 鈫?鍑忓皯鍔犻€熷害
            // 渚ч锛氫腑闂磋寖鍥?鈫?涓嶅彉鎴栫紦鎱㈡仮澶嶉粯璁ゅ€?

            float accelChange = 0f;
            if (dot > 0.2f)
            {
                accelChange = dot * 2f;   // 鏈€澶ч『椋庢椂 +2锛堝彲璋冿級
            }
            else if (dot < -0.2f)
            {
                accelChange = dot * 10f;   // dot涓鸿礋锛宎ccelChange涓鸿礋锛堝-0.5 鈫?-1锛?
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
            BounceBackFromDoor(doorCollider);
            return;
        }

        if (mat == PlayerMatState.Steel)
        {
            if (doorCtrl.doorMat == DoorController.DoorMat.Soft)
            {
                BounceBackFromDoor(doorCollider);
                return;
            }

            if (doorCtrl.doorMat == DoorController.DoorMat.Hard && doorCtrl.IsOpened)
            {
                ExecuteDoorTransition(doorCollider);
            }
            else
            {
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
                    Animator animator=doorCollider.GetComponent<Animator>();
                    animator.SetBool("isBreaking", true);
                    BounceBackFromDoor(doorCollider);
                    StartCoroutine(WaitForBreaking(1.0f, doorCollider.gameObject));

                }
                else
                {
                    BounceBackFromDoor(doorCollider);
                }
                return;
            }

            if (doorCtrl.doorMat == DoorController.DoorMat.Hard && doorCtrl.IsOpened)
            {
                ExecuteDoorTransition(doorCollider);
            }
            else
            {
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
            }

            if (playerAction != null)
                playerAction.ResetToStartPosition();
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
        door.transform.parent.GetChild(0).gameObject.SetActive(false);  //闅愯棌闂ㄧ殑褰?
        door.transform.parent.GetChild(1).gameObject.SetActive(true);//瑙﹀彂寮€闂ㄧ殑鐪熸闂?
    }
}
