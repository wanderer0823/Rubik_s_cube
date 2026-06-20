using UnityEngine;
using TMPro;
using System;

/// <summary>
/// 专门用于房间45的交互提示：
/// 检测名字含"球"或"珠"的物体，按 E 进入下一步，按左键完成并销毁。
/// </summary>
public class Room45Hint : MonoBehaviour
{
    [Header("步骤文字")]
    public string step0Message = "检测到球体，按 E 开始";
    public string step1Message = "按左键确认";

    [Header("过滤关键词（包含任意一个即匹配）")]
    public string[] targetKeywords = new string[] { "球", "珠" };

    [Header("调试")]
    public bool enableDebugLog = true;

    private TextMeshProUGUI tmpText;
    private GrabSystem grabSystem;
    private int currentStep = -1;    // -1=未激活, 0=步骤0, 1=步骤1
    private bool isActive = false;
    private Grabbable currentTarget; // 缓存目标，后续不再更新

    void Start()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
        if (tmpText == null)
        {
            Debug.LogError("Room45Hint: 需要 TextMeshProUGUI 组件");
            return;
        }

        tmpText.text = "";
        tmpText.enabled = false;      // 初始隐藏，GameObject 保持激活
        if (enableDebugLog) Debug.Log("Room45Hint: 初始化完成，等待检测...");
    }

    void Update()
    {
        // ----- 未激活时：检测目标 -----
        if (!isActive)
        {
            // 获取 GrabSystem
            if (grabSystem == null)
            {
                grabSystem = FindObjectOfType<GrabSystem>();
                if (grabSystem == null) return;
            }

            // 获取当前准星目标
            Grabbable target = GetCurrentTarget(grabSystem);
            if (target != null && IsTargetValid(target))
            {
                // 满足条件，进入步骤0
                currentTarget = target;
                isActive = true;
                currentStep = 0;
                UpdateText(step0Message);
                if (enableDebugLog) Debug.Log($"Room45Hint: 检测到目标 [{target.name}]，进入步骤0");
            }
            // 否则不显示，继续等待
            return;
        }

        // ----- 已激活：处理输入（不再依赖 GrabSystem） -----
        if (currentStep == 0 && Input.GetKeyDown(KeyCode.E))
        {
            currentStep = 1;
            UpdateText(step1Message);
            if (enableDebugLog) Debug.Log("Room45Hint: 按下 E，进入步骤1");
        }
        else if (currentStep == 1 && Input.GetMouseButtonDown(0))
        {
            if (enableDebugLog) Debug.Log("Room45Hint: 按下左键，执行最终动作并销毁");

            // 在此处添加你需要的自定义逻辑（例如触发物体方法）
            // currentTarget?.SendMessage("OnConfirm", SendMessageOptions.DontRequireReceiver);

            // 销毁自身（提示消失）
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 验证目标是否有效：房间45 + 名称包含任一关键词
    /// </summary>
    bool IsTargetValid(Grabbable target)
    {
        // 房间判断
        if (GameState.Instance == null || GameState.Instance.CurrentRoomID != 45)
            return false;

        // 名称关键词匹配
        string name = target.gameObject.name;
        foreach (string keyword in targetKeywords)
        {
            if (name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    void UpdateText(string message)
    {
        if (tmpText != null)
        {
            tmpText.text = message;
            tmpText.enabled = true;   // 显示文字
        }
    }

    /// <summary>
    /// 获取 GrabSystem 当前检测到的目标（支持公共属性或反射）
    /// </summary>
    Grabbable GetCurrentTarget(GrabSystem gs)
    {
        // 优先使用公共属性（如已添加 public Grabbable CurrentTarget => currentTarget;）
        var prop = typeof(GrabSystem).GetProperty("CurrentTarget");
        if (prop != null)
            return prop.GetValue(gs) as Grabbable;

        // 降级使用反射字段
        var field = typeof(GrabSystem).GetField("currentTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            return field.GetValue(gs) as Grabbable;

        Debug.LogWarning("Room45Hint: 无法获取 currentTarget，请检查 GrabSystem 是否暴露该字段");
        return null;
    }
}