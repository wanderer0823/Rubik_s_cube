using System.Collections;
using UnityEngine;


public class GroundLandingDetector : MonoBehaviour
{
    [Header("落地检测")]
    public float velocityThreshold = 0.05f;     
    public float angularThreshold = 0.05f;      
    public float maxWaitTime = 5f;              
    public float rayDistance = 0.2f;            
    public float minStableTime = 0.2f;          
    public float normalDotThreshold = 0.9f;     

    private Rigidbody rb;
    private Collider col;
    private LayerMask wallLayer;
    private bool started = false;

    
    public void Begin(LayerMask walls)
    {
        if (started) return;
        started = true;
        wallLayer = walls;

        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        if (rb == null || col == null)
        {
            /*__DEBUGTOOL_START__*/Debug.LogWarning($"GroundLandingDetector: {gameObject.name} 缺失 Rigidbody 或 Collider");/*__DEBUGTOOL_END__*/
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

                // 
                if (stableTime >= minStableTime)
                {
                    if (CheckGround())
                    {
                        rb.isKinematic = true;
                        Destroy(this);
                        yield break;
                    }
                    // 
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

        Destroy(this);
    }

    
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

        return dot >= normalDotThreshold;
    }
}
