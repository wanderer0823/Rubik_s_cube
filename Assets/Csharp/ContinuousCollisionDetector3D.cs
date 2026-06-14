using UnityEngine;

/// <summary>
/// 防止快速移动的玩家碰撞体穿透房间碰撞体。
/// 附加到拥有 Rigidbody 和 Collider 的玩家对象上。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class ContinuousCollisionDetector3D : MonoBehaviour
{
    public enum DetectTiming
    {
        FixedUpdate,
        LateUpdate
    }

    [Header("Cast")]
    [Tooltip("视为实心房间/墙壁碰撞体的层")]
    [SerializeField] private LayerMask obstacleLayers = ~0;

    [Tooltip("触发碰撞体通常不是墙壁，因此默认忽略是更安全的")]
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Tooltip("LateUpdate 可以捕获由 Update/协程驱动的房间旋转。如果所有移动都是物理驱动的，则使用 FixedUpdate")]
    [SerializeField] private DetectTiming detectTiming = DetectTiming.LateUpdate;

    [Tooltip("仅当移动距离超过碰撞体最小宽度乘以该值时，才执行扫描")]
    [SerializeField, Min(0.01f)] private float minWidthThresholdMultiplier = 1f;

    [Tooltip("额外扫描距离，避免因精度问题错过表面")]
    [SerializeField, Min(0f)] private float castPadding = 0.02f;

    [Header("Correction")]
    [Tooltip("修正后与碰撞表面保持的小间距")]
    [SerializeField, Min(0f)] private float skinWidth = 0.03f;

    [Tooltip("修正后移除指向碰撞表面的速度分量")]
    [SerializeField] private bool removeVelocityIntoSurface = true;

    [Tooltip("若为 true，则保留平行于墙壁的速度，而不是完全停止")]
    [SerializeField] private bool slideOnSurface = true;

    [Header("Debug")]
    [Tooltip("是否绘制调试线")]
    [SerializeField] private bool drawDebugLine;
    [Tooltip("调试线颜色")]
    [SerializeField] private Color debugLineColor = Color.cyan;

    private const int MaxHits = 16;

    private readonly RaycastHit[] hits = new RaycastHit[MaxHits];
    private Rigidbody rb;
    private Collider playerCollider;
    private Vector3 previousPosition;
    private Quaternion previousRotation;
    private bool hasPreviousPose;
    private bool skipNextDetection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
        ResetPreviousPose();
    }

    private void OnEnable()
    {
        ResetPreviousPose();
        skipNextDetection = true;
    }

    private void FixedUpdate()
    {
        if (detectTiming == DetectTiming.FixedUpdate)
            DetectAndCorrect();
    }

    private void LateUpdate()
    {
        if (detectTiming == DetectTiming.LateUpdate)
            DetectAndCorrect();
    }

    public void ResetPreviousPose()
    {
        previousPosition = rb != null ? rb.position : transform.position;
        previousRotation = transform.rotation;
        hasPreviousPose = true;
    }

    private void DetectAndCorrect()
    {
        if (!hasPreviousPose || rb == null || playerCollider == null)
        {
            ResetPreviousPose();
            return;
        }

        if (skipNextDetection)
        {
            skipNextDetection = false;
            ResetPreviousPose();
            return;
        }

        Vector3 currentPosition = rb.position;
        Vector3 movement = currentPosition - previousPosition;
        float distance = movement.magnitude;
        float minWidth = GetColliderMinWorldWidth();

        if (distance > minWidth * minWidthThresholdMultiplier)
        {
            Vector3 direction = movement / distance;
            if (TrySweepFromPreviousPose(direction, distance + castPadding, out RaycastHit hit))
                CorrectPosition(direction, hit);
        }

        ResetPreviousPose();
    }

    private bool TrySweepFromPreviousPose(Vector3 direction, float distance, out RaycastHit bestHit)
    {
        int hitCount = CastFromPreviousPose(direction, distance);
        bestHit = default;

        bool found = false;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.collider == playerCollider)
                continue;

            if (hit.rigidbody == rb)
                continue;

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                bestHit = hit;
                found = true;
            }
        }

        return found;
    }

    private int CastFromPreviousPose(Vector3 direction, float distance)
    {
        switch (playerCollider)
        {
            case SphereCollider sphere:
                {
                    Vector3 center = previousPosition + previousRotation * Vector3.Scale(sphere.center, transform.lossyScale);
                    float radius = GetScaledSphereRadius(sphere);
                    return Physics.SphereCastNonAlloc(center, radius, direction, hits, distance, obstacleLayers, triggerInteraction);
                }

            case CapsuleCollider capsule:
                {
                    GetCapsuleWorldPoints(capsule, previousPosition, previousRotation, out Vector3 point0, out Vector3 point1, out float radius);
                    return Physics.CapsuleCastNonAlloc(point0, point1, radius, direction, hits, distance, obstacleLayers, triggerInteraction);
                }

            case BoxCollider box:
                {
                    Vector3 center = previousPosition + previousRotation * Vector3.Scale(box.center, transform.lossyScale);
                    Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, Abs(transform.lossyScale));
                    return Physics.BoxCastNonAlloc(center, halfExtents, direction, hits, previousRotation, distance, obstacleLayers, triggerInteraction);
                }

            default:
                return Physics.RaycastNonAlloc(previousPosition, direction, hits, distance, obstacleLayers, triggerInteraction);
        }
    }

    private void CorrectPosition(Vector3 direction, RaycastHit hit)
    {
        float safeDistance = Mathf.Max(0f, hit.distance - skinWidth);
        Vector3 correctedPosition = previousPosition + direction * safeDistance;

        rb.position = correctedPosition;
        transform.position = correctedPosition;

        if (!removeVelocityIntoSurface)
        {
            OnPreventPassThrough(hit, correctedPosition);
            return;
        }

        Vector3 velocity = rb.velocity;
        float intoSurfaceSpeed = Vector3.Dot(velocity, hit.normal);

        if (intoSurfaceSpeed < 0f)
        {
            rb.velocity = slideOnSurface
                ? Vector3.ProjectOnPlane(velocity, hit.normal)
                : Vector3.zero;
        }

        OnPreventPassThrough(hit, correctedPosition);
    }

    protected virtual void OnPreventPassThrough(RaycastHit hit, Vector3 correctedPosition)
    {
    }

    public void OnTeleport(Vector3 newPosition)
    {
        // 直接将 previousPosition 设为新位置，这样下一帧计算的位移 ≈ 0
        previousPosition = newPosition;
        // 强制跳过下一次检测，避免刚刚传送完就做 sweep
        skipNextDetection = true;
        // 同步刚体位置（如果外部已经设置，这里只是确保记录一致）
        if (rb != null) rb.position = newPosition;
        transform.position = newPosition;
    }

    private float GetColliderMinWorldWidth()
    {
        switch (playerCollider)
        {
            case SphereCollider sphere:
                return GetScaledSphereRadius(sphere) * 2f;

            case CapsuleCollider capsule:
                return Mathf.Min(GetScaledCapsuleRadius(capsule) * 2f, GetScaledCapsuleHeight(capsule));

            case BoxCollider box:
                {
                    Vector3 size = Vector3.Scale(box.size, Abs(transform.lossyScale));
                    return Mathf.Min(size.x, size.y, size.z);
                }

            default:
                {
                    Vector3 size = playerCollider.bounds.size;
                    return Mathf.Min(size.x, size.y, size.z);
                }
        }
    }

    private static Vector3 Abs(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }

    private float GetScaledSphereRadius(SphereCollider sphere)
    {
        Vector3 scale = Abs(transform.lossyScale);
        return sphere.radius * Mathf.Max(scale.x, scale.y, scale.z);
    }

    private float GetScaledCapsuleRadius(CapsuleCollider capsule)
    {
        Vector3 scale = Abs(transform.lossyScale);
        return capsule.radius * GetCapsuleRadiusScale(capsule.direction, scale);
    }

    private float GetScaledCapsuleHeight(CapsuleCollider capsule)
    {
        Vector3 scale = Abs(transform.lossyScale);
        return capsule.height * GetAxisScale(capsule.direction, scale);
    }

    private void GetCapsuleWorldPoints(
        CapsuleCollider capsule,
        Vector3 position,
        Quaternion rotation,
        out Vector3 point0,
        out Vector3 point1,
        out float radius)
    {
        Vector3 scale = Abs(transform.lossyScale);
        radius = capsule.radius * GetCapsuleRadiusScale(capsule.direction, scale);
        float height = Mathf.Max(capsule.height * GetAxisScale(capsule.direction, scale), radius * 2f);
        Vector3 axis = GetLocalCapsuleAxis(capsule.direction);
        Vector3 center = position + rotation * Vector3.Scale(capsule.center, transform.lossyScale);
        Vector3 offset = rotation * axis * ((height * 0.5f) - radius);

        point0 = center + offset;
        point1 = center - offset;
    }

    private static Vector3 GetLocalCapsuleAxis(int direction)
    {
        switch (direction)
        {
            case 0:
                return Vector3.right;
            case 1:
                return Vector3.up;
            default:
                return Vector3.forward;
        }
    }

    private static float GetAxisScale(int direction, Vector3 scale)
    {
        switch (direction)
        {
            case 0:
                return scale.x;
            case 1:
                return scale.y;
            default:
                return scale.z;
        }
    }

    private static float GetCapsuleRadiusScale(int direction, Vector3 scale)
    {
        switch (direction)
        {
            case 0:
                return Mathf.Max(scale.y, scale.z);
            case 1:
                return Mathf.Max(scale.x, scale.z);
            default:
                return Mathf.Max(scale.x, scale.y);
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawDebugLine || !Application.isPlaying || !hasPreviousPose)
            return;

        Gizmos.color = debugLineColor;
        Gizmos.DrawLine(previousPosition, transform.position);
    }
}