using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GroundLandingDetector : MonoBehaviour
{
    Rigidbody rb;
    LayerMask wallLayer;
    LayerMask originalExcludeLayers;
    bool armed;
    float startTime;

    // 安全超时：万一卡住超过这个时间，强制落地
    const float SAFETY_TIMEOUT = 5f;
    // 落地法线判定阈值（dot > 0.8 ≈ 与重力反方向夹角 < 37°）
    const float GROUND_DOT = 0.8f;
    // 落地后延迟冻结
    const float FREEZE_DELAY = 0.2f;

    public void Begin(LayerMask wallLayerMask)
    {
        rb = GetComponent<Rigidbody>();
        wallLayer = wallLayerMask;

        // 下落期间只跟 Walls 层发生物理碰撞，无视家具/装饰等所有非墙物体
        originalExcludeLayers = rb.excludeLayers;
        rb.excludeLayers = ~wallLayerMask.value;   // 排除"非Walls"的所有层

        armed = true;
        startTime = Time.time;
    }

    void Update()
    {
        if (!armed) return;

        // 兜底：万一物理卡住（墙角夹住），超时强制落地
        if (Time.time - startTime > SAFETY_TIMEOUT)
        {
            armed = false;
            StartCoroutine(FreezeDelayed(0f));
        }
    }

    void OnCollisionEnter(Collision c) { TryDetectGround(c); }
    void OnCollisionStay(Collision c) { TryDetectGround(c); }

    void TryDetectGround(Collision c)
    {
        if (!armed) return;
        if (((1 << c.gameObject.layer) & wallLayer) == 0) return;

        Vector3 upDir = -Physics.gravity.normalized;

        foreach (ContactPoint cp in c.contacts)
        {
            if (Vector3.Dot(cp.normal, upDir) > GROUND_DOT)
            {
                armed = false;
                StartCoroutine(FreezeDelayed(FREEZE_DELAY));
                return;
            }
        }
    }

    IEnumerator FreezeDelayed(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.excludeLayers = originalExcludeLayers;   // 恢复碰撞过滤
        }
        Destroy(this);
    }
}
