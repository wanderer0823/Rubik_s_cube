using UnityEngine;
using TMPro;
using System;

public class GrabDetectStepHint : MonoBehaviour
{
    [Header("步骤文字")]
    public string[] stepMessages = new string[]
    {
        "检测到风扇，按左键调整",
        "滚轮调整角度",
        "再按左键确认"
    };

    [Header("过滤条件")]
    public string requiredNameSubstring = "风扇";

    [Header("调试")]
    public bool enableDebugLog = true;

    private TextMeshProUGUI tmpText;
    private GrabSystem grabSystem;
    private int currentStep = -1;
    private bool isActive = false;

    void Start()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
        if (tmpText == null)
        {
            Debug.LogError("GrabDetectStepHint: 需要 TextMeshProUGUI 组件");
            return;
        }

        // 关键修改：不要关闭自身 GameObject，仅隐藏文字
        tmpText.text = "";
        tmpText.enabled = false;   // 禁用组件，保持 GameObject 激活

        if (enableDebugLog) Debug.Log("GrabDetectStepHint: 初始化完成，等待检测...");
    }

    void Update()
    {
        // 1. 获取 GrabSystem 实例
        if (grabSystem == null)
        {
            grabSystem = FindObjectOfType<GrabSystem>();
            if (grabSystem == null)
            {
                if (enableDebugLog) Debug.LogWarning("GrabDetectStepHint: 未找到 GrabSystem");
                return;
            }
            else
            {
                if (enableDebugLog) Debug.Log("GrabDetectStepHint: 找到 GrabSystem");
            }
        }

        // 2. 获取当前目标（优先使用公共属性，若无则用反射）
        Grabbable currentTarget = GetCurrentTarget(grabSystem);
        if (enableDebugLog && currentTarget != null)
            Debug.Log($"GrabDetectStepHint: 检测到目标 {currentTarget.name}");

        // 3. 验证目标有效性
        bool isValid = IsTargetValid(currentTarget);
        if (enableDebugLog)
            Debug.Log($"GrabDetectStepHint: isValid={isValid}, isActive={isActive}");

        // 4. 状态转换
        if (isValid && !isActive)
        {
            if (enableDebugLog) Debug.Log("GrabDetectStepHint: 有效目标，进入步骤0");
            StartSteps();
        }
        else if (!isValid && isActive)
        {
            if (enableDebugLog) Debug.Log("GrabDetectStepHint: 目标无效，隐藏提示");
            HideHint();
            return;
        }

        if (!isActive) return;

        // 5. 步骤输入逻辑
        if (currentStep == 0 && Input.GetMouseButtonDown(0))
        {
            if (enableDebugLog) Debug.Log("步骤0 → 左键，进入步骤1");
            currentStep = 1;
            UpdateText(stepMessages[1]);
        }
        else if (currentStep == 1 && Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.01f)
        {
            if (enableDebugLog) Debug.Log("步骤1 → 滚轮，进入步骤2");
            currentStep = 2;
            UpdateText(stepMessages[2]);
        }
        else if (currentStep == 2 && Input.GetMouseButtonDown(0))
        {
            if (enableDebugLog) Debug.Log("步骤2 → 左键，执行动作并销毁");
            // 执行你的最终动作（例如触发物体方法）
            // currentTarget.SendMessage("OnConfirm", SendMessageOptions.DontRequireReceiver);
            Destroy(gameObject);   // 现在销毁自身是安全的
        }
    }

    bool IsTargetValid(Grabbable target)
    {
        if (target == null) return false;

        // 房间判断
        if (GameState.Instance == null || GameState.Instance.CurrentRoomID != 44)
            return false;

        // 名称过滤（不区分大小写）
        bool nameContains = target.gameObject.name.IndexOf(requiredNameSubstring, StringComparison.OrdinalIgnoreCase) >= 0;
        return nameContains;
    }

    void StartSteps()
    {
        isActive = true;
        currentStep = 0;
        UpdateText(stepMessages[0]);
        if (enableDebugLog) Debug.Log($"步骤0激活，文字: {stepMessages[0]}");
    }

    void UpdateText(string message)
    {
        if (tmpText != null)
        {
            tmpText.text = message;
            tmpText.enabled = true;      // 显示文字
            if (enableDebugLog) Debug.Log($"更新文字: {message}");
        }
    }

    void HideHint()
    {
        isActive = false;
        if (tmpText != null)
        {
            tmpText.enabled = false;
            tmpText.text = "";
        }
        // 若希望销毁而非隐藏，可改为 Destroy(gameObject);
        // 但隐藏后重新检测到目标会再次显示（因为 isActive 已重置）
    }

    Grabbable GetCurrentTarget(GrabSystem gs)
    {
        // 优先使用公共属性（如已添加）
        var prop = typeof(GrabSystem).GetProperty("CurrentTarget");
        if (prop != null)
            return prop.GetValue(gs) as Grabbable;

        // 降级用反射字段
        var field = typeof(GrabSystem).GetField("currentTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            return field.GetValue(gs) as Grabbable;

        Debug.LogWarning("GrabDetectStepHint: 无法获取 currentTarget，请检查 GrabSystem 是否暴露该字段");
        return null;
    }
}