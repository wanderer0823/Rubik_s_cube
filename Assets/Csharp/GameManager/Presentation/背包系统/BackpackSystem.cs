using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 背包系统：控制背包整体 + 右侧详情弹窗。
/// 挂载在 BackpackPanel 上。
/// </summary>
public class BackpackSystem : MonoBehaviour
{
    [Header("ScrollRect")]
    [SerializeField] private ScrollRect scrollRect;

    [Header("测试用")] [SerializeField] private bool addMat = false;
    [Header("材质拾取物列表（拖入场景中的MatPickup物体）")]
    [SerializeField] private List<MatPickup> materialPickups = new List<MatPickup>();

    [Header("材质球Steel/Glass/Bounce的背包组件")]
    [SerializeField] private List<BackpackSlotUI> matSlots;

    [Header("材质球在背包的排列区域MatSection")]
    [SerializeField] private Transform matSection;

    [Header("材质的背包UI单位")]
    [SerializeField] private GameObject matSlotPrefab;

    [Header("材质球3个1：1小图标")]
    [SerializeField] private Sprite steelIconSprite;
    [SerializeField] private Sprite glassIconSprite;
    [SerializeField] private Sprite bounceIconSprite;


    [Header("线索在背包的排列区域ClueSection")]
    [SerializeField] private Transform clueSection;    // 注意：指向 ClueSection

    [Header("线索的背包UI单位")]
    [SerializeField] private GameObject clueSlotPrefab;

    [Header("详情面板引用BackpackPanel")]
    [SerializeField] private RectTransform detailPopupPanel;
    [SerializeField] private TMP_Text detailNameText;
    [SerializeField] private TMP_Text detailDescText;
    [SerializeField] private Image detailImage;

    [Header("详情面板淡入淡出")]
    [SerializeField] private float animDuration = 0.25f;
    [SerializeField] private float hideOffsetY = -200f;   // 隐藏时相对显示位置的Y偏移（屏幕下方）

    [Header("字体")]
    [SerializeField] private TMP_FontAsset defaultFont;

    // 记录详情面板显示/隐藏位置，由初始位置决定
    private Vector2 detailShowPos;
    private Vector2 detailHidePos;
    private Coroutine currentAnim;
    private CanvasGroup detailCanvasGroup;

    // 已添加的线索字典
    private Dictionary<string, BackpackSlotUI> clueSlotMap = new Dictionary<string, BackpackSlotUI>();

    void OnEnable()
    {
        GameEvents.OnBagOpenExecute += OnBagOpen;
        GameEvents.OnBagCloseExecute += OnBagClose;
        GameEvents.OnBagScrollExecute += OnBagScroll;
        GameEvents.OnMatChangeExecute += OnMatChanged;
    }

    void OnDisable()
    {
        GameEvents.OnBagOpenExecute -= OnBagOpen;
        GameEvents.OnBagCloseExecute -= OnBagClose;
        GameEvents.OnBagScrollExecute -= OnBagScroll;
        GameEvents.OnMatChangeExecute -= OnMatChanged;
    }

    void Start()
    {
        InitDetailPanel();
        if (addMat)
        {
            InitializeMaterialsFromPickups();
        }
    }
    
    private void InitializeMaterialsFromPickups()
    {
        if (materialPickups == null || materialPickups.Count == 0)
        {
            /*__DEBUGTOOL_START__*/
            Debug.Log("BackpackSystem: 材质拾取物列表为空，跳过初始化");
            /*__DEBUGTOOL_END__*/
            return;
        }

        foreach (var pickup in materialPickups)
        {
            if (pickup == null)
            {
                /*__DEBUGTOOL_START__*/
                Debug.LogWarning("BackpackSystem: 列表中存在空的拾取物引用，已跳过");
                /*__DEBUGTOOL_END__*/
                continue;
            }
            
            AddMatFromPickup(pickup);
        }
    }

    /// <summary>
    /// 从 MatPickup 读取配置并添加到背包（带去重）
    /// </summary>
    private void AddMatFromPickup(MatPickup pickup)
    {
        // 去重：检查是否已经添加过同类型材质
        foreach (var existingSlot in matSlots)
        {
            if (existingSlot != null && existingSlot.LinkedMatState == pickup.matType)
            {
                /*__DEBUGTOOL_START__*/
                Debug.Log($"BackpackSystem: 材质 [{pickup.displayName}] 已存在，跳过");
                /*__DEBUGTOOL_END__*/
                return;
            }
        }

        // 获取图标（优先使用 Pickup 上的自定义图标，否则使用默认）
        Sprite iconSprite = pickup.iconSprite != null ? pickup.iconSprite : pickup.matType switch
        {
            PlayerMatState.Steel => steelIconSprite,
            PlayerMatState.Glass => glassIconSprite,
            PlayerMatState.Bounce => bounceIconSprite,
            _ => null
        };

        // 实例化槽位
        GameObject go = Instantiate(matSlotPrefab, matSection);
        go.name = pickup.displayName;
        BackpackSlotUI newSlot = go.GetComponent<BackpackSlotUI>();

        if (newSlot == null)
        {
            Debug.LogError("BackpackSystem: 材质预制体缺少 BackpackSlotUI");
            Destroy(go);
            return;
        }

        newSlot.Init(
            this,
            BackpackSlotUI.SlotType.Material,
            pickup.displayName,
            pickup.description,
            () => GameEvents.onMatChangeRequest(pickup.matType),
            pickup.detailImage,
            pickup.matType,
            iconSprite,
            Color.white,
            defaultFont
        );

        matSlots.Add(newSlot);
        
        /*__DEBUGTOOL_START__*/
        Debug.Log($"BackpackSystem: 从拾取物添加材质 [{pickup.displayName}]");
        /*__DEBUGTOOL_END__*/
    }

    // ===== 初始化 =====
    private void InitDetailPanel()
    {
        if (detailPopupPanel == null) return;

        // 记录显示位置
        detailShowPos = detailPopupPanel.anchoredPosition;
        detailHidePos = detailShowPos + new Vector2(0, hideOffsetY);

        // 确保 CanvasGroup 存在，用于淡入淡出
        detailCanvasGroup = detailPopupPanel.GetComponent<CanvasGroup>();
        if (detailCanvasGroup == null)
            detailCanvasGroup = detailPopupPanel.gameObject.AddComponent<CanvasGroup>();

        // 初始状态
        detailCanvasGroup.alpha = 0f;
        detailPopupPanel.anchoredPosition = detailHidePos;
        detailPopupPanel.gameObject.SetActive(false);
    }

    // ===== 详情面板控制（供 BackpackSlotUI 调用）=====

    /// <summary>
    /// 显示详情面板：从底部向上滑入
    /// </summary>
    public void ShowDetail(string itemName, string description, Sprite image)
    {
        if (detailPopupPanel == null) return;

        // 填充数据
        if (detailNameText != null) detailNameText.text = itemName;
        if (detailDescText != null) detailDescText.text = description;

        if (detailImage != null)
        {
            if (image != null)
            {
                detailImage.sprite = image;
                detailImage.gameObject.SetActive(true);
            }
            else
            {
                detailImage.gameObject.SetActive(false);
            }
        }

        // 每次重新播放动画（从底部向上）
        if (currentAnim != null)
            StopCoroutine(currentAnim);

        detailPopupPanel.gameObject.SetActive(true);
        // 设置到底部起始位置
        detailPopupPanel.anchoredPosition = detailHidePos;
        detailCanvasGroup.alpha = 0f;

        currentAnim = StartCoroutine(AnimateDetail(true));
    }

    /// <summary>
    /// 隐藏详情面板：向下滑出
    /// </summary>
    public void HideDetail()
    {
        if (detailPopupPanel == null || !detailPopupPanel.gameObject.activeSelf) return;

        if (currentAnim != null)
            StopCoroutine(currentAnim);

        currentAnim = StartCoroutine(AnimateDetail(false));
    }

    private IEnumerator AnimateDetail(bool show)
    {
        Vector2 startPos = detailPopupPanel.anchoredPosition;
        Vector2 endPos = show ? detailShowPos : detailHidePos;
        float startAlpha = detailCanvasGroup.alpha;
        float endAlpha = show ? 1f : 0f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / animDuration;
            float eased = show ? EaseOutCubic(t) : EaseInCubic(t);

            detailPopupPanel.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
            detailCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, eased);
            yield return null;
        }

        detailPopupPanel.anchoredPosition = endPos;
        detailCanvasGroup.alpha = endAlpha;

        if (!show)
            detailPopupPanel.gameObject.SetActive(false);

        currentAnim = null;
    }

    // 缓动函数
    private float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private float EaseInCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t;
    }

    // ===== 添加物品 =====

    public void AddClue(string clueID, string clueName, string description, Sprite detailImg = null, Sprite icon = null)
    {
        if (clueSlotMap.ContainsKey(clueID)) return;

        if (clueSlotPrefab == null || clueSection == null)    // clueContent 即 clueSection
        {
            /*__DEBUGTOOL_START__*/
            Debug.LogWarning("BackpackSystem: 线索预制体或父物体未设置");/*__DEBUGTOOL_END__*/
            return;
        }

        GameObject go = Instantiate(clueSlotPrefab, clueSection);    // clueContent 即 clueSection
        BackpackSlotUI slot = go.GetComponent<BackpackSlotUI>();

        if (slot == null)
        {
            Debug.LogError("BackpackSystem: 线索预制体缺少 BackpackSlotUI 组件");
            Destroy(go);
            return;
        }

        slot.Init(
            this,
            BackpackSlotUI.SlotType.Clue,
            clueName,
            description,
            null,               // 线索没有点击切换逻辑
            detailImg,
            null,               // 无材质类型
            icon,               // 可能为 null
            Color.white,        // 图标颜色（若 icon 为 null 无关紧要）
            defaultFont         // 字体
        );

        clueSlotMap[clueID] = slot;
        /*__DEBUGTOOL_START__*/
        Debug.Log($"BackpackSystem: 添加线索 [{clueID}] {clueName}");/*__DEBUGTOOL_END__*/
    }

    // ===== 添加材质 =====
    public void AddMat(PlayerMatState matType, string matName, string desc, Sprite detail = null)
    {
        // 检查是否已有该材质
        foreach (var existingSlot in matSlots)
        {
            if (existingSlot != null && existingSlot.gameObject.name == matName)
                return;
        }

        if (matSlotPrefab == null || matSection == null)
        {
            Debug.LogError("BackpackSystem: 材质预制体或父物体未设置");
            return;
        }

        GameObject go = Instantiate(matSlotPrefab, matSection);
        go.name = matName;
        BackpackSlotUI newSlot = go.GetComponent<BackpackSlotUI>();

        if (newSlot == null)
        {
            Debug.LogError("BackpackSystem: 材质预制体缺少 BackpackSlotUI");
            Destroy(go);
            return;
        }

        Sprite iconSprite = matType switch
        {
            PlayerMatState.Steel => steelIconSprite,
            PlayerMatState.Glass => glassIconSprite,
            PlayerMatState.Bounce => bounceIconSprite,
            _ => null
        };

        newSlot.Init(this,
            BackpackSlotUI.SlotType.Material,
            matName,
            desc,
            () => GameEvents.onMatChangeRequest(matType),
            detail,
            matType,
            iconSprite,          // 小图标
            Color.white,         // 图标颜色纯白
            defaultFont          // 字体
        );


        matSlots.Add(newSlot);
        /*__DEBUGTOOL_START__*/
        Debug.Log($"BackpackSystem: 添加材质 [{matName}]");/*__DEBUGTOOL_END__*/
    }

    // ===== 背包事件响应 =====

    private void OnBagOpen()
    {
        RefreshMatHighlight();
    }

    private void OnBagClose()
    {
        // 关闭背包时，强制隐藏详情弹窗（如果有）
        if (detailPopupPanel != null)
        {
            if (currentAnim != null)
                StopCoroutine(currentAnim);
            detailCanvasGroup.alpha = 0f;
            detailPopupPanel.anchoredPosition = detailHidePos;
            detailPopupPanel.gameObject.SetActive(false);
            currentAnim = null;
        }
    }

    private void OnBagScroll(float delta)
    {
        if (scrollRect == null) return;
        scrollRect.verticalNormalizedPosition += delta * 0.1f;
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
    }

    public void RefreshMatHighlight()
    {
        if (matSlots == null || matSlots.Count == 0) return;
        var currentMat = GameState.Instance.CurrentMatState;

        // 动态列表无法用 index 对应 enum，因此按值匹配
        foreach (var slot in matSlots)
        {
            if (slot == null) continue;
            bool match = slot.LinkedMatState.HasValue && slot.LinkedMatState.Value == currentMat;
            slot.SetSelected(match);
        }
    }

    private void OnMatChanged(PlayerMatState newMat)
    {
        RefreshMatHighlight();
    }
}