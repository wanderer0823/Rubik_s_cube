using UnityEngine;

/// <summary>
/// 玩家与道具（Spring/Wind/Plate）的碰撞交互。
/// 挂在玩家物体上（带 Rigidbody + Collider）。
/// 不走事件总线，直接用 OnTriggerEnter/Stay/Exit。
/// </summary>
public class ItemInteractionController : MonoBehaviour
{
    [Header("Plate 设置")]
    public float plateMoveDistance = 1f;
    public float plateMoveSpeed = 2f;

    [Header("Spring 设置")]
    public float springForce = 15f;

    [Header("Wind 设置")]
    public float windForce = 10f;

    [Header("Bounce + Plate 设置")]
    public int bounceCountRequired = 3;

    private Rigidbody rb;
    private GameState gs;

    // Bounce + Plate 计数
    private int bounceCountOnPlate = 0;
    private Collider currentPlateCollider = null;
    private Coroutine plateResetCoroutine = null;    // 新增
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
                // 踩回来时取消归零计时
                if (plateResetCoroutine != null)
                {
                    StopCoroutine(plateResetCoroutine);
                    plateResetCoroutine = null;
                }

                if (currentPlateCollider != other)
                {
                    currentPlateCollider = other;
                    bounceCountOnPlate = 0;
                }
                bounceCountOnPlate++;
                Debug.Log($"Bounce踩Plate 次数: {bounceCountOnPlate}/{bounceCountRequired}");

                if (bounceCountOnPlate >= bounceCountRequired)
                {
                    HandleBouncePlate(other);
                }
            }
            else
            {
                Debug.Log("Glass + Plate: 无效果");
            }
            return;
        }

        // ---------- Spring ----------
        if (other.CompareTag("Spring"))
        {
            if (mat == PlayerMatState.Glass || mat == PlayerMatState.Bounce)
            {
                HandleSpring(other);
            }
            else
            {
                Debug.Log("Steel + Spring: 无效果");
            }
            return;
        }

        // ---------- Wind ----------
        if (other.CompareTag("Wind"))
        {
            if (mat == PlayerMatState.Glass || mat == PlayerMatState.Bounce)
            {
                HandleWind(other);
            }
            else
            {
                Debug.Log("Steel + Wind: 无效果");
            }
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
        if (other.CompareTag("Plate") && other == currentPlateCollider)
        {
            // 延迟归零，给弹力球时间弹回来
            if (plateResetCoroutine != null)
                StopCoroutine(plateResetCoroutine);
            plateResetCoroutine = StartCoroutine(DelayedPlateReset());
        }
    }

    System.Collections.IEnumerator DelayedPlateReset()
    {
        yield return new WaitForSeconds(plateResetDelay);
        Debug.Log("Plate计数超时归零");
        bounceCountOnPlate = 0;
        currentPlateCollider = null;
        plateResetCoroutine = null;
    }

    // ==================== 交互处理 ====================

    void HandleSteelPlate(Collider plateCollider)
    {
        Transform plateModel = plateCollider.transform.parent;
        PlateLink link = plateModel.GetComponent<PlateLink>();
        if (link == null)
            link = plateModel.GetComponentInParent<PlateLink>();

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
        Transform plateModel = plateCollider.transform.parent;
        PlateLink link = plateModel.GetComponent<PlateLink>();
        if (link == null)
            link = plateModel.GetComponentInParent<PlateLink>();

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

        bounceCountOnPlate = 0;
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

    // ==================== 工具 ====================

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
