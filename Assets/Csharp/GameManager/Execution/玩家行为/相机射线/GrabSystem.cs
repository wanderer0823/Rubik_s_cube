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

    [Header("托举设置")]
    public float liftSpeed = 5f;
    public float holdHeightOffset = 1f;

    [Header("旋转设置")]
    public float rotateStepAngle = 90f;
    public float rotateCooldown = 0.15f;

    private float lastRotateTime;

    [Header("高亮设置")]
    public UnityEngine.UI.Image crosshairImage;
    public Color normalColor = Color.white;
    public Color interactColor = Color.green;

    [Header("性能")]
    public float detectInterval = 0.02f;

    private float lastDetectTime;

    private Camera cam;
    private GameState gs;

    private Grabbable currentTarget;
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

        Debug.Log($"GrabSystem: 释放 {heldObject.gameObject.name}");

        DisableOutline(heldObject);

        Rigidbody objRb = heldObject.GetComponent<Rigidbody>();
        if (objRb != null)
        {
            objRb.isKinematic = false;
        }

        heldObject = null;
        currentTarget = null;
        currentPivot = null;
        isHolding = false;

        gs.SetPlayerState(PlayerState.isMoving);

        if (crosshairImage != null)
            crosshairImage.color = normalColor;
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

        Debug.Log($"GrabSystem: 绕 {currentRotateAxis} 旋转 {sign * rotateStepAngle}°");
    }
}
