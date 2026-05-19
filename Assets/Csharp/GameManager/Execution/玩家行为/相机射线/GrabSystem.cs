using UnityEngine;

/// <summary>
///      View3     
/// </summary>
public class GrabSystem : MonoBehaviour
{
    [Header("可移动物体范围")]
    public float maxGrabDistance = 10f;
    public LayerMask grabbableLayer;
    public LayerMask wallLayer;

    [Header("托举设置")]
    public float liftSpeed = 5f;
    public float holdHeightOffset = 1f;

    [Header("旋转设置")]
    public float rotateStepAngle = 90f;     // 每次旋转的角度
    public float rotateCooldown = 0.15f;    // 两次旋转之间的最小间隔，防误触

    private float lastRotateTime;

    [Header("高亮设置")]
    public UnityEngine.UI.Image crosshairImage;
    public Color normalColor = Color.white;
    public Color interactColor = Color.green;

    [Header("性能")]
    public float detectInterval = 0.02f;   // 检测间隔

    private float lastDetectTime;

    private Camera cam;
    private GameState gs;

    private Grabbable currentTarget;
    private Grabbable heldObject;
    private float grabDistance;
    private bool isHolding = false;
    private bool isGravityAllowed = false;

    private Vector3 rotateAxis = new(1, 0, 0);
    private Transform currentPivot;          // 当前抓取物体的旋转中心

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
            if (Time.unscaledTime - lastDetectTime >= detectInterval)
            {
                DetectTarget();
                lastDetectTime = Time.unscaledTime;
            }

            // 输入响应仍每帧检测，保留点击灵敏度
            if (currentTarget != null && Input.GetMouseButtonDown(0))
            {
                if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    return;
                if (!isGravityAllowed)
                    return;

                GrabObject(currentTarget);
            }
        }
        else
        {
            HoldObject();

            if (Input.GetMouseButtonDown(0))
            {
                ReleaseObject();
            }
        }
    }

    void DetectTarget()
    {
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
        Ray ray = cam.ScreenPointToRay(screenCenter);

        isGravityAllowed = false;

        if (Physics.Raycast(ray, out RaycastHit hit, maxGrabDistance, grabbableLayer))
        {
            Grabbable g = hit.collider.GetComponent<Grabbable>();
            if (g == null) g = hit.collider.GetComponentInParent<Grabbable>();

            if (g != null && g.IsGravityAligned())
            {
                isGravityAllowed = true;
                rotateAxis = g.GetRotateWorldAxis();

                if (currentTarget != g)
                {
                    DisableOutline(currentTarget);
                    currentTarget = g;
                    EnableOutline(currentTarget);
                }
                if (crosshairImage != null) crosshairImage.color = interactColor;
                return;
            }
        }

        if (currentTarget != null)
        {
            DisableOutline(currentTarget);
            currentTarget = null;
        }
        if (crosshairImage != null) crosshairImage.color = normalColor;
    }

    /// <summary>
    /// 物体自身朝向是否与世界重力相反方向一致
    /// </summary>
    
    void EnableOutline(Grabbable target)
    {
        if (target == null) return;
        MinimalOutline outline = target.GetComponent<MinimalOutline>();
        if (outline != null)
            outline.SetEnabled(true);
    }

    void DisableOutline(Grabbable target)
    {
        if (target == null) return;
        MinimalOutline outline = target.GetComponent<MinimalOutline>();
        if (outline != null)
            outline.SetEnabled(false);
    }

    void GrabObject(Grabbable obj)
    {
        heldObject = obj;
        grabDistance = Vector3.Distance(cam.transform.position, obj.transform.position);
        isHolding = true;

        // 缓存 pivot：优先用 Grabbable 上指定的子物体，否则退回自身
        currentPivot = obj.rotatePivot != null ? obj.rotatePivot : obj.transform;

        Rigidbody objRb = obj.GetComponent<Rigidbody>();
        if (objRb != null)
        {
            objRb.isKinematic = true;
        }

        gs.SetPlayerState(PlayerState.isGrabbing);
        Debug.Log($"GrabSystem: 抓取 {obj.gameObject.name}, pivot={currentPivot.name}");
    }

    void HoldObject()
    {
        if (heldObject == null) return;

        Vector3 targetPos = cam.transform.position + cam.transform.forward * grabDistance + Vector3.up * holdHeightOffset;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, grabDistance, wallLayer))
        {
            float safeDistance = hit.distance - 0.3f;
            if (safeDistance < 0.5f) safeDistance = 0.5f;
            targetPos = cam.transform.position + cam.transform.forward * safeDistance;
        }

        heldObject.transform.position = Vector3.Lerp(
            heldObject.transform.position,
            targetPos,
            Time.deltaTime * liftSpeed
        );
    }

    void ReleaseObject()
    {
        if (heldObject == null) return;

        DisableOutline(heldObject);

        Rigidbody objRb = heldObject.GetComponent<Rigidbody>();
        if (objRb != null)
        {
            objRb.isKinematic = false;

            // 挂落地检测器：下落只与 Walls 碰撞，落到水平面后 0.2s 冻结
            var detector = heldObject.gameObject.GetComponent<GroundLandingDetector>();
            if (detector == null)
                detector = heldObject.gameObject.AddComponent<GroundLandingDetector>();
            detector.Begin(wallLayer);
        }

        heldObject = null;
        currentTarget = null;
        currentPivot = null;
        isHolding = false;

        gs.SetPlayerState(PlayerState.isMoving);

        if (crosshairImage != null)
            crosshairImage.color = normalColor;
    }

    /// <summary>
    ///         VMM    OnGrabRotateExecute 
    /// </summary>
    void OnScrollRotate(float delta)
    {
        if (!isHolding || heldObject == null) return;
        if (Mathf.Abs(delta) < 0.01f) return;
        if (Time.unscaledTime - lastRotateTime < rotateCooldown) return;

        float sign = delta > 0f ? 1f : -1f;

        // 每次旋转时重新获取物体当前的旋转轴世界方向
        Vector3 currentRotateAxis = heldObject.GetRotateWorldAxis();

        Vector3 pivotPos = currentPivot != null ? currentPivot.position : heldObject.transform.position;

        heldObject.transform.RotateAround(pivotPos, currentRotateAxis, sign * rotateStepAngle);
        lastRotateTime = Time.unscaledTime;

        Debug.Log($"GrabSystem: 绕 {currentRotateAxis} 旋转 {sign * rotateStepAngle}°");
    }

    //  
    float NormalizeAngle(float angle)
    {
        return Mathf.DeltaAngle(0f, angle);
    }

    Vector3 NormalizeEuler(Vector3 v)
    {
        return new Vector3(
            NormalizeAngle(v.x),
            NormalizeAngle(v.y),
            NormalizeAngle(v.z)
        );
    }

    bool SameAngle(float a, float b)
    {
        return Mathf.Abs(Mathf.DeltaAngle(a, b)) < 0.1f;
    }

    bool SameRotation(Vector3 a, Vector3 b)
    {
        return SameAngle(a.x, b.x) &&
               SameAngle(a.y, b.y) &&
               SameAngle(a.z, b.z);
    }
}
