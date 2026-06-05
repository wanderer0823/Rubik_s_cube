using System.Collections;
using UnityEngine;

/// <summary>
/// ��ؼ�����������ͷź�ȴ���أ��䵽���棨Walls �㣩�󶳽ᡣ
/// �� GrabSystem ���ͷ�����ʱ��̬���ص������ϡ�
/// </summary>
public class GroundLandingDetector : MonoBehaviour
{
    [Header("������")]
    public float velocityThreshold = 0.05f;     // �ٶȵ��ڴ�ֵ��Ϊ��ֹ
    public float angularThreshold = 0.05f;      // ���ٶȵ��ڴ�ֵ��Ϊ��ֹ
    public float maxWaitTime = 5f;              // ��ȴ�ʱ��
    public float rayDistance = 0.2f;            // �ײ����߼�����
    public float minStableTime = 0.2f;          // ��ֹ����ʱ�䣬���ⶶ������
    public float normalDotThreshold = 0.9f;     // ������worldUp�ĵ����ֵ��Լ25���ݲ

    private Rigidbody rb;
    private Collider col;
    private LayerMask wallLayer;
    private bool started = false;

    /// <summary>
    /// GrabSystem �ͷ�����ʱ���ã������ؼ��
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
            /*__DEBUGTOOL_START__*/Debug.LogWarning($"GroundLandingDetector: {gameObject.name} ȱ�� Rigidbody �� Collider");/*__DEBUGTOOL_END__*/
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

                // ������ֹ������ֵ�ż��
                if (stableTime >= minStableTime)
                {
                    if (CheckGround())
                    {
                        rb.isKinematic = true;
                        /*__DEBUGTOOL_START__*/Debug.Log($"{gameObject.name} ��أ��Ѷ���");/*__DEBUGTOOL_END__*/
                        Destroy(this);
                        yield break;
                    }
                    // ���ʧ��˵�����忨��ǽ�ϵ�λ�ã������ȶ���ʱ������
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

        /*__DEBUGTOOL_START__*/Debug.LogWarning($"{gameObject.name} �ȴ���س�ʱ����������");/*__DEBUGTOOL_END__*/
        Destroy(this);
    }

    /// <summary>
    /// �ײ�������߼�⣬ȷ���Ƿ����ڵ��棨���߳��ϣ�
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

        /*__DEBUGTOOL_START__*/Debug.Log($"{gameObject.name} ��ؼ�⣺{hitCount}/{samplePoints.Length} ����, " +
                  $"ƽ������={avgNormal}, dot={dot:F2}");/*__DEBUGTOOL_END__*/

        return dot >= normalDotThreshold;
    }
}
