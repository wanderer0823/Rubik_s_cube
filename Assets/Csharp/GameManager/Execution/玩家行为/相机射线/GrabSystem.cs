using UnityEngine;

/// <summary>
/// 准星举起/放下/旋转系统。
/// 挂在 View3 相机上。
/// 长按左键举起，松开放下，滚轮旋转。
/// </summary>
public class GrabSystem : MonoBehaviour
{
    [Header("检测设置")]
    public float maxGrabDistance = 10f;
    public LayerMask grabbableLayer;
    public LayerMask wallLayer;

    [Header("举起设置")]
    public float liftSpeed = 5f;
    public float holdHeightOffset = 1f;

    [Header("旋转设置")]
    public float rotateSpeed = 90f;

    private Camera cam;
    private GameState gs;

    private Grabbable currentTarget;
    private Grabbable heldObject;
    private float grabDistance;
    private bool isHolding = false;

    void Start()
    {
        cam = GetComponent<Camera>();
        gs = GameState.Instance;
    }

    void OnEnable()
    {
        GameEvents.OnGrabRotateExecute += OnScrollRotate;
    }

    void OnDisable()
    {
        GameEvents.OnGrabRotateExecute -= OnScrollRotate;
    }

    void Update()
    {
        if (gs == null) return;
        if (gs.CurrentView != ViewMode.View3) return;
        if (gs.CurrentPlayerState == PlayerState.isOpeningBag) return;

        if (!isHolding)
        {
            DetectTarget();

            // 长按左键 + 有目标 → 抓起
            if (currentTarget != null && Input.GetMouseButtonDown(0))
            {
                // 确认不在UI上
                if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    return;

                GrabObject(currentTarget);
            }
        }
        else
        {
            HoldObject();

            // 松开左键 → 释放
            if (Input.GetMouseButtonUp(0))
            {
                ReleaseObject();
            }
        }
    }

    void DetectTarget()
    {
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
        Ray ray = cam.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out RaycastHit hit, maxGrabDistance, grabbableLayer))
        {
            Grabbable g = hit.collider.GetComponent<Grabbable>();
            if (g == null)
                g = hit.collider.GetComponentInParent<Grabbable>();

            if (g != null)
            {
                currentTarget = g;
                // TODO: 准星变色提示可交互
                return;
            }
        }

        currentTarget = null;
    }

    void GrabObject(Grabbable obj)
    {
        heldObject = obj;
        grabDistance = Vector3.Distance(cam.transform.position, obj.transform.position);
        isHolding = true;

        // 物体变为运动学，跟随准星
        Rigidbody objRb = obj.GetComponent<Rigidbody>();
        if (objRb != null)
        {
            objRb.isKinematic = true;
        }

        // 切换玩家状态
        gs.SetPlayerState(PlayerState.isGrabbing);
        Debug.Log($"GrabSystem: 举起 {obj.gameObject.name}");
    }

    void HoldObject()
    {
        if (heldObject == null) return;

        // 目标位置：相机前方 grabDistance 距离
        Vector3 targetPos = cam.transform.position + cam.transform.forward * grabDistance;

        // 防穿墙：射线检测相机到目标位置之间是否有墙
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, grabDistance, wallLayer))
        {
            float safeDistance = hit.distance - 0.3f;
            if (safeDistance < 0.5f) safeDistance = 0.5f;
            targetPos = cam.transform.position + cam.transform.forward * safeDistance;
        }

        // 平滑移动到目标位置
        heldObject.transform.position = Vector3.Lerp(
            heldObject.transform.position,
            targetPos,
            Time.deltaTime * liftSpeed
        );
    }

    void ReleaseObject()
    {
        if (heldObject == null) return;

        Debug.Log($"GrabSystem: 释放 {heldObject.gameObject.name}");

        // 恢复物理
        Rigidbody objRb = heldObject.GetComponent<Rigidbody>();
        if (objRb != null)
        {
            objRb.isKinematic = false;
        }

        heldObject = null;
        currentTarget = null;
        isHolding = false;

        // 恢复玩家状态
        gs.SetPlayerState(PlayerState.isMoving);
    }

    /// <summary>
    /// 滚轮旋转物体（由 VMM → OnGrabRotateExecute 调用）
    /// </summary>
    void OnScrollRotate(float delta)
    {
        if (!isHolding || heldObject == null) return;

        // 绕物体自身Y轴旋转，delta>0顺时针，<0逆时针
        float angle = delta * rotateSpeed;
        heldObject.transform.Rotate(Vector3.up, angle, Space.Self);
        Debug.Log($"GrabSystem: 旋转物体 角度={angle:F1}");
    }
}
