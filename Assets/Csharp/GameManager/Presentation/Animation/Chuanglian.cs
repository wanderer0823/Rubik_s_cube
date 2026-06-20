using UnityEngine;

public class AnimPingPong_Percentage : MonoBehaviour
{
    [Header("动画设置")]
    [SerializeField] private string clipName = "";
    [SerializeField] private float speed = 1f;

    [Header("切换阈值（0~1）")]
    [Range(0f, 1f)]
    [SerializeField] private float forwardEndPercent = 0.95f;   // 正放到95%就切倒放
    [Range(0f, 1f)]
    [SerializeField] private float backwardEndPercent = 0f;     // 倒放到0%就切正放（你也可以设为0.05f停在5%）

    [Header("停顿时间（秒）")]
    [SerializeField] private float pauseDuration = 0.1f;

    private Animation anim;
    private AnimationState animState;
    private float length;

    private enum PlayState { Forward, PauseAfterForward, Backward, PauseAfterBackward }
    private PlayState state = PlayState.Forward;
    private float pauseTimer = 0f;

    // 锁定变换
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;
    private Renderer[] renderers;

    void Start()
    {
        // 记录变换
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        initialScale = transform.localScale;
        renderers = GetComponentsInChildren<Renderer>(true);

        anim = GetComponent<Animation>();
        if (string.IsNullOrEmpty(clipName) && anim.clip != null)
            clipName = anim.clip.name;

        if (string.IsNullOrEmpty(clipName))
        {
            Debug.LogError("请指定动画名称或在 Animation 组件中拖入默认动画！");
            return;
        }

        anim.Play(clipName);
        animState = anim[clipName];
        if (animState == null)
        {
            Debug.LogError($"找不到动画 '{clipName}'");
            return;
        }

        length = animState.length;
        animState.wrapMode = WrapMode.ClampForever;
        animState.speed = 0f;
        animState.time = 0f;
        state = PlayState.Forward;
        anim.Sample();
    }

    void Update()
    {
        if (animState == null) return;

        // 强制锁定 Transform 和渲染器
        transform.localPosition = initialPosition;
        transform.localRotation = initialRotation;
        transform.localScale = initialScale;
        foreach (var r in renderers) if (r != null) r.enabled = true;

        // 计算当前百分比（0~1）
        float currentPercent = animState.time / length;

        switch (state)
        {
            case PlayState.Forward:
                animState.time += Time.deltaTime * speed;
                // 到达设定的正放结束百分比 → 切停顿
                if (currentPercent >= forwardEndPercent)
                {
                    animState.time = length * forwardEndPercent; // 精确停在该百分比
                    state = PlayState.PauseAfterForward;
                    pauseTimer = 0f;
                }
                break;

            case PlayState.PauseAfterForward:
                pauseTimer += Time.deltaTime;
                if (pauseTimer >= pauseDuration)
                {
                    state = PlayState.Backward;
                }
                break;

            case PlayState.Backward:
                animState.time -= Time.deltaTime * speed;
                currentPercent = animState.time / length;
                // 到达设定的倒放结束百分比 → 切停顿
                if (currentPercent <= backwardEndPercent)
                {
                    animState.time = length * backwardEndPercent;
                    state = PlayState.PauseAfterBackward;
                    pauseTimer = 0f;
                }
                break;

            case PlayState.PauseAfterBackward:
                pauseTimer += Time.deltaTime;
                if (pauseTimer >= pauseDuration)
                {
                    state = PlayState.Forward;
                }
                break;
        }

        // 安全钳制
        animState.time = Mathf.Clamp(animState.time, 0f, length);
        anim.Sample();
    }
}