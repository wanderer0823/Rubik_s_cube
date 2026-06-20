using UnityEngine;

/// <summary>
/// 准星举起/放下/旋转系统。
/// 挂在 View3 相机上。
/// </summary>
public class GrabSystem : MonoBehaviour
{
    [Header("可移动物体范围")]
    public float maxGrabDistance = 10f;
    public LayerMask grabbableLayer;
    public LayerMask wallLayer;
    public float GW_distance = 0.6f;

    [Header("托举设置")]
    public float liftSpeed = 5f;
    public float holdHeightOffset = 1f;

    [Header("旋转设置")]
    public float rotateStepAngle = 90f;
    public float rotateCooldown = 0.15f;

    private float lastRotateTime;

    [Header("准星设置")]
    public UnityEngine.UI.Image crosshairImage; // 显示准星的 Image 组件
    public Sprite crosshairSprite1; // 默认准星
    public Sprite crosshairSprite2; // 瞄准准星
    public Sprite crosshairSprite3; // 抓取准星

    [Header("性能")]
    public float detectInterval = 0.02f;

    private float lastDetectTime;
    
    public bool blockRelease = false;

    private Camera cam;
    private GameState gs;

    public Grabbable CurrentTarget => currentTarget;   // 只读属性
    public Grabbable currentTarget;
    private Grabbable heldObject;
    private float grabDistance;
    private bool isHolding = false;
    private bool isGravityAllowed = false;

    private Vector3 rotateAxis = Vector3.up;
    private Transform currentPivot;

    void Start()
    {
        cam = GetComponent<Camera>();
        gs = GameState.Instance;
        UpdateCrosshair();
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
        if (Input.GetMouseButtonDown(0))
        {
            if (blockRelease) return;   // 若被阻止，忽略左键
            ReleaseObject();
        }

        if (!isHolding)
        {
            if (Time.unscaledTime - lastDetectTime >= detectInterval)
            {
                DetectTarget();
                lastDetectTime = Time.unscaledTime;
            }

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
                UpdateCrosshair();
                return;
            }
        }

        if (currentTarget != null)
        {
            DisableOutline(currentTarget);
            currentTarget = null;
        }
        UpdateCrosshair();
    }

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

    public void GrabObject(Grabbable obj)
    {
        heldObject = obj;
        grabDistance = Vector3.Distance(cam.transform.position, obj.transform.position);
        isHolding = true;
        /*__DEBUGTOOL_START__*/Debug.Log("GrabS-1:isHolding");/*__DEBUGTOOL_END__*/
        UpdateCrosshair();

        currentPivot = obj.rotatePivot != null ? obj.rotatePivot : obj.transform;

        Rigidbody objRb = obj.GetComponent<Rigidbody>();
        if (objRb != null)
        {
            objRb.isKinematic = true;
        }

        gs.SetPlayerState(PlayerState.isGrabbing);
        /*__DEBUGTOOL_START__*/Debug.Log($"GrabSystem: 抓取 {obj.gameObject.name}, pivot={currentPivot.name}");/*__DEBUGTOOL_END__*/
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Grabbable"), true); //抓取物体时不和拾取物品产生碰撞，否则会变成永动机desu
    }

    void HoldObject()
    {
        if (heldObject == null) return;

        Vector3 targetPos = cam.transform.position + cam.transform.forward * grabDistance + Vector3.up * holdHeightOffset;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, grabDistance, wallLayer))
        {
            float safeDistance = hit.distance - GW_distance;
            if (safeDistance < 0.5f) safeDistance = 0.5f;
            targetPos = cam.transform.position + cam.transform.forward * safeDistance;
        }

        heldObject.transform.position = Vector3.Lerp(
            heldObject.transform.position,
            targetPos,
            Time.deltaTime * liftSpeed
        );
    }

    public void ReleaseObject()
    {
        if (heldObject == null) return;

        /*__DEBUGTOOL_START__*/Debug.Log($"GrabSystem: 释放 {heldObject.gameObject.name}");/*__DEBUGTOOL_END__*/

        DisableOutline(heldObject);

        Rigidbody objRb = heldObject.GetComponent<Rigidbody>();
        if (objRb != null)
        {
            objRb.isKinematic = false;

            // 挂载落地检测器，落到地面后自动冻结
            var detector = heldObject.gameObject.GetComponent<GroundLandingDetector>();
            if (detector == null)
                detector = heldObject.gameObject.AddComponent<GroundLandingDetector>();
            detector.Begin(wallLayer);
        }

        heldObject = null;
        currentTarget = null;
        currentPivot = null;
        isHolding = false;
        /*__DEBUGTOOL_START__*/Debug.Log("GrabS-2:!isHolding");/*__DEBUGTOOL_END__*/

        gs.SetPlayerState(PlayerState.isMoving);
        Physics.IgnoreLayerCollision( LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Grabbable"), false);

        UpdateCrosshair();
    }

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

        /*__DEBUGTOOL_START__*/Debug.Log($"GrabSystem: 绕 {currentRotateAxis} 旋转 {sign * rotateStepAngle}°");/*__DEBUGTOOL_END__*/
    }

    void UpdateCrosshair()
    {
        if (crosshairImage == null) return;

        if (isHolding)
        {
            // 抓取中 - 显示准星3
            /*__DEBUGTOOL_START__*/Debug.Log("GrabS-3:isHolding");/*__DEBUGTOOL_END__*/
            crosshairImage.sprite = crosshairSprite3;
        }
        else if (currentTarget != null && isGravityAllowed)
        {
            // 瞄准中 - 显示准星2
            crosshairImage.sprite = crosshairSprite2;
        }
        else
        {
            // 默认 - 显示准星1
            crosshairImage.sprite = crosshairSprite1;
        }
    }

}
