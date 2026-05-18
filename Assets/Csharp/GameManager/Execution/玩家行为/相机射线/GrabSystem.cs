using UnityEngine;

/// <summary>
/// ׼�Ǿ���/����/��תϵͳ��
/// ���� View3 ����ϡ�
/// ������������ɿ����£�������ת��
/// </summary>
public class GrabSystem : MonoBehaviour
{
    [Header("�������")]
    public float maxGrabDistance = 10f;
    public LayerMask grabbableLayer;
    public LayerMask wallLayer;

    [Header("��������")]
    public float liftSpeed = 5f;
    public float holdHeightOffset = 1f;

    [Header("��ת����")]
    public float rotateSpeed = 90f;

    [Header("׼����ʾ")]
    public UnityEngine.UI.Image crosshairImage;
    public Color normalColor = Color.white;
    public Color interactColor = Color.green;

    private Camera cam;
    private GameState gs;

    private Grabbable currentTarget;
    private Grabbable heldObject;
    private float grabDistance;
    private bool isHolding = false;
    private bool isGravityAllowed = false;
    private bool isWaitingGroundFreeze = false;

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

            // ������� + ��Ŀ�� �� ץ��
            if (currentTarget != null && Input.GetMouseButtonDown(0))
            {
                // ȷ�ϲ���UI��
                if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    return;
                //ȷ����ͬһ��������ϵ
                if (!isGravityAllowed)
                    return;

                GrabObject(currentTarget);
            }
        }
        else
        {
            HoldObject();

            // �ɿ���� �� �ͷ�
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

        if (Physics.Raycast(ray, out RaycastHit hit, maxGrabDistance, grabbableLayer))
        {
            Grabbable g = hit.collider.GetComponent<Grabbable>();
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (g == null)
                g = hit.collider.GetComponentInParent<Grabbable>();

            //ȷ����ͬһ��������ϵ��
            Vector3 allowedRotation = g.allowedParentRotate.ToVector3();
            Transform t = g.transform;

            for (int i = 0; i < 2; i++)
            {
                if (t.parent == null)
                {
                    Debug.LogError("���㼶���㣺i="+i);
                    return;
                }

                t = t.parent;
            }

            Vector3 parentRotation = t.eulerAngles;
            if (allowedRotation == parentRotation)
            {
                isGravityAllowed = true;
            }
            else return;

            if (g != null)
            {
                // �л�����Ŀ��
                if (currentTarget != g)
                {
                    DisableOutline(currentTarget);
                    currentTarget = g;
                    EnableOutline(currentTarget);
                }

                if (crosshairImage != null)
                    crosshairImage.color = interactColor;
                return;
            }
        }

        // û��Ŀ��
        if (currentTarget != null)
        {
            DisableOutline(currentTarget);
            currentTarget = null;
        }

        if (crosshairImage != null)
            crosshairImage.color = normalColor;
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

        // �����Ϊ�˶�ѧ������׼��
        Rigidbody objRb = obj.GetComponent<Rigidbody>();
        if (objRb != null)
        {
            objRb.isKinematic = true;
        }

        // �л����״̬
        gs.SetPlayerState(PlayerState.isGrabbing);
        /*__DEBUGTOOL_START__*/Debug.Log($"GrabSystem: ���� {obj.gameObject.name}");/*__DEBUGTOOL_END__*/
    }

    void HoldObject()
    {
        if (heldObject == null) return;

        // Ŀ��λ�ã����ǰ�� grabDistance ����
        Vector3 targetPos = cam.transform.position + cam.transform.forward * grabDistance + Vector3.up *holdHeightOffset;

        // ����ǽ�����߼�������Ŀ��λ��֮���Ƿ���ǽ
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, grabDistance, wallLayer))
        {
            float safeDistance = hit.distance - 0.3f;
            if (safeDistance < 0.5f) safeDistance = 0.5f;
            targetPos = cam.transform.position + cam.transform.forward * safeDistance;
        }

        // ƽ���ƶ���Ŀ��λ��
        heldObject.transform.position = Vector3.Lerp(
            heldObject.transform.position,
            targetPos,
            Time.deltaTime * liftSpeed
        );
    }

    void ReleaseObject()
    {
        if (heldObject == null) return;

        /*__DEBUGTOOL_START__*/Debug.Log($"GrabSystem: �ͷ� {heldObject.gameObject.name}");/*__DEBUGTOOL_END__*/

        DisableOutline(heldObject);

        Rigidbody objRb = heldObject.GetComponent<Rigidbody>();
        if (objRb != null)
        {
            objRb.isKinematic = false;

            // ������ؼ��
            isWaitingGroundFreeze = true;
        }

        currentTarget = null;
        isHolding = false;

        gs.SetPlayerState(PlayerState.isMoving);

        if (crosshairImage != null)
            crosshairImage.color = normalColor;
    }

    /// <summary>
    /// ������ת���壨�� VMM �� OnGrabRotateExecute ���ã�
    /// </summary>
    void OnScrollRotate(float delta)
    {
        if (!isHolding || heldObject == null) return;

        // ����������Y����ת��delta>0˳ʱ�룬<0��ʱ��
        float angle = delta * rotateSpeed;
        heldObject.transform.Rotate(Vector3.up, angle, Space.Self);
        /*__DEBUGTOOL_START__*/Debug.Log($"GrabSystem: ��ת���� �Ƕ�={angle:F1}");/*__DEBUGTOOL_END__*/
    }

    //������ײ������˶�ѧ
    void OnCollisionEnter(Collision collision)
    {
        if (!isWaitingGroundFreeze) return;
        if (heldObject == null) return;

        // ������ Walls Layer
        if (((1 << collision.gameObject.layer) & wallLayer) == 0)
            return;

        // Trigger ������
        Collider col = collision.collider;
        if (col.isTrigger)
            return;

        // �������������򣨵��淨�߷���
        Vector3 upDir = -Physics.gravity.normalized;

        foreach (ContactPoint contact in collision.contacts)
        {
            // ����������������ӽ�
            float dot = Vector3.Dot(contact.normal, upDir);

            // 0.9 �� 25������
            if (dot > 0.8f)
            {
                StartCoroutine(FreezeAfterDelay());
                break;
            }
        }
    }

    System.Collections.IEnumerator FreezeAfterDelay()
    {
        isWaitingGroundFreeze = false;

        yield return new WaitForSeconds(0.2f);

        if (heldObject == null)
            yield break;

        Rigidbody rb = heldObject.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        heldObject = null;
    }
}
