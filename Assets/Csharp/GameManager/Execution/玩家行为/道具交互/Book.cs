using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Book : MonoBehaviour
{
    [Header("检测设置")]
    [Tooltip("风扇触发器的标签，默认 'Wind'")]
    public string fanTag = "Wind";

    [Header("动画设置")]
    [Tooltip("要触发的 Trigger 名称，默认 'play'")]
    public string triggerName = "play";

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
            Debug.LogWarning($"{gameObject.name} 上未找到 Animator 组件，无法触发动画。");
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(fanTag))
            return;

        if (animator != null)
        {
            animator.SetTrigger(triggerName);
            Debug.Log($"{gameObject.name} 进入风扇，触发自身 Animator Trigger: {triggerName}");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(fanTag))
            return;

        if (animator != null)
        {
            animator.ResetTrigger(triggerName);
            Debug.Log($"{gameObject.name} 离开风扇，重置 Trigger: {triggerName}");
        }
    }
}
