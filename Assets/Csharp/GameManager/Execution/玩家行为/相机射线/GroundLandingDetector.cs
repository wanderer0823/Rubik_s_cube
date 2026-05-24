using System.Collections;
using UnityEngine;

/// <summary>
/// 落地检测器：物体释放后等待落地，落到地面（Walls 层）后冻结。
/// 由 GrabSystem 在释放物体时动态挂载到物体上。
/// </summary>
public class GroundLandingDetector : MonoBehaviour
{
    [Header("检测参数")]
    public float velocityThreshold = 0.05f;     // 速度低于此值视为静止
    public float angularThreshold = 0.05f;      // 角速度低于此值视为静止
    public float maxWaitTime = 5f;              // 最长等待时间
    public float rayDistance = 0.2f;            // 底部射线检测距离
    public float minStableTime = 0.2f;          // 静止持续时间，避免抖动误判
    public float normalDotThreshold = 0.9f;     // 法线与worldUp的点积阈值（约25度容差）

    private Rigidbody rb;
    private Collider col;
    private LayerMask wallLayer;
    private bool started = false;

    /// <summary>
    /// GrabSystem 释放物体时调用，启动落地检测
    /// </summary>
    public void Begin(LayerMask walls)
    {
        if (started) return;
        started = true;
        wallLayer = walls;

        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        if (rb == null || col == null)
        {
            Debug.LogWarning($"GroundLandingDetector: {gameObject.name} 缺少 Rigidbody 或 Collider");
            Destroy(this);
            return;
        }

        StartCoroutine(WaitForLanding());
    }

    IEnumerator WaitForLanding()
    {
        float t = 0f;
        float stableTime = 0f;

        while (t < maxWaitTime)
        {
            bool isStable = rb.velocity.magnitude < velocityThreshold
                            && rb.angularVelocity.magnitude < angularThreshold;

            if (isStable)
            {
                stableTime += Time.deltaTime;

                // 持续静止超过阈值才检测
                if (stableTime >= minStableTime)
                {
                    if (CheckGround())
                    {
                        rb.isKinematic = true;
                        Debug.Log($"{gameObject.name} 落地，已冻结");
                        Destroy(this);
                        yield break;
                    }
                    // 检测失败说明物体卡在墙上等位置，重置稳定计时继续等
                    stableTime = 0f;
                }
            }
            else
            {
                stableTime = 0f;
            }

            t += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning($"{gameObject.name} 等待落地超时，跳过冻结");
        Destroy(this);
    }

    /// <summary>
    /// 底部多点射线检测，确认是否落在地面（法线朝上）
    /// </summary>
    bool CheckGround()
    {
        Bounds bounds = col.bounds;
        Vector3 worldUp = -Physics.gravity.normalized;
        Vector3 worldDown = Physics.gravity.normalized;

        Vector3 bottomCenter = bounds.center + worldDown * bounds.extents.y;

        Vector3[] samplePoints = new Vector3[]
        {
            bottomCenter,
            bottomCenter + transform.right * bounds.extents.x * 0.7f,
            bottomCenter - transform.right * bounds.extents.x * 0.7f,
            bottomCenter + transform.forward * bounds.extents.z * 0.7f,
            bottomCenter - transform.forward * bounds.extents.z * 0.7f,
        };

        Vector3 sumNormal = Vector3.zero;
        int hitCount = 0;

        foreach (var point in samplePoints)
        {
            Vector3 rayStart = point - worldDown * 0.05f;

            if (Physics.Raycast(rayStart, worldDown, out RaycastHit hit,
                rayDistance, wallLayer, QueryTriggerInteraction.Ignore))
            {
                sumNormal += hit.normal;
                hitCount++;
            }
        }

        if (hitCount == 0) return false;

        Vector3 avgNormal = (sumNormal / hitCount).normalized;
        float dot = Vector3.Dot(avgNormal, worldUp);

        Debug.Log($"{gameObject.name} 落地检测：{hitCount}/{samplePoints.Length} 命中, " +
                  $"平均法线={avgNormal}, dot={dot:F2}");

        return dot >= normalDotThreshold;
    }
}
