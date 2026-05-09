using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 背包系统：管理背包内格子 + 右侧公共详情面板。
/// 挂在 BackpackPanel 上。
/// </summary>
public class BackpackSystem : MonoBehaviour
{
    [Header("ScrollRect")]
    [SerializeField] private ScrollRect scrollRect;

    [Header("材质格子（固定3个，按Steel/Glass/Bounce顺序）")]
    [SerializeField] private List<BackpackSlotUI> matSlots;

    [Header("材质详情图片")]
    [SerializeField] private Sprite steelDetailSprite;
    [SerializeField] private Sprite glassDetailSprite;
    [SerializeField] private Sprite bounceDetailSprite;

    [Header("线索格子容器（ClueSection）")]
    [SerializeField] private Transform clueSection;    // 改名，指向 ClueSection

    [Header("线索格子预制体")]
    [SerializeField] private GameObject clueSlotPrefab;

    [Header("公共详情面板（在BackpackPanel外部）")]
    [SerializeField] private RectTransform detailPopupPanel;
    [SerializeField] private TMP_Text detailNameText;
    [SerializeField] private TMP_Text detailDescText;
    [SerializeField] private Image detailImage;

    [Header("详情面板动画")]
    [SerializeField] private float animDuration = 0.25f;
    [SerializeField] private float hideOffsetY = -200f;   // 隐藏时相对显示位置的Y偏移（屏幕下方）

    // 详情面板的显示位置（由初始位置决定）
    private Vector2 detailShowPos;
    private Vector2 detailHidePos;
    private Coroutine currentAnim;
    private CanvasGroup detailCanvasGroup;

    // 已创建的线索格子
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
        InitMatSlots();
        InitDetailPanel();
    }

    // ===== 初始化 =====

    private void InitMatSlots()
    {
        if (matSlots == null || matSlots.Count < 3)
        {
            Debug.LogWarning("BackpackSystem: matSlots 未配置满3个");
            return;
        }

        matSlots[0].Init(this,
            BackpackSlotUI.SlotType.Material,
            "Steel",
            "钢铁球：质量大，可踩压力板开门",
            () => GameEvents.onMatChangeRequest(PlayerMatState.Steel),
            steelDetailSprite       // 传入图片
);

        matSlots[1].Init(this,
            BackpackSlotUI.SlotType.Material,
            "Glass",
            "玻璃球：可被弹簧弹起，可被风吹动，高速可撞碎软门",
            () => GameEvents.onMatChangeRequest(PlayerMatState.Glass),
            glassDetailSprite
        );

        matSlots[2].Init(this,
            BackpackSlotUI.SlotType.Material,
            "Bounce",
            "弹力球：拥有玻璃球全部能力，且可在所有表面反弹，反复踩压力板也可开门",
            () => GameEvents.onMatChangeRequest(PlayerMatState.Bounce),
            bounceDetailSprite
        );
    }

    private void InitDetailPanel()
    {
        if (detailPopupPanel == null) return;

        // 记录显示位置
        detailShowPos = detailPopupPanel.anchoredPosition;
        detailHidePos = detailShowPos + new Vector2(0, hideOffsetY);

        // 确保有 CanvasGroup 用于淡入淡出
        detailCanvasGroup = detailPopupPanel.GetComponent<CanvasGroup>();
        if (detailCanvasGroup == null)
            detailCanvasGroup = detailPopupPanel.gameObject.AddComponent<CanvasGroup>();

        // 初始隐藏
        detailCanvasGroup.alpha = 0f;
        detailPopupPanel.anchoredPosition = detailHidePos;
        detailPopupPanel.gameObject.SetActive(false);
    }

    // ===== 公共详情面板控制（由 BackpackSlotUI 调用）=====

    /// <summary>
    /// 显示详情面板：从下方渐显浮上
    /// </summary>
    public void ShowDetail(string itemName, string description, Sprite image)
    {
        if (detailPopupPanel == null) return;

        // 填充内容
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

        // 每次悬浮都从底部重新浮上
        if (currentAnim != null)
            StopCoroutine(currentAnim);

        detailPopupPanel.gameObject.SetActive(true);
        // 重置到底部起始位置
        detailPopupPanel.anchoredPosition = detailHidePos;
        detailCanvasGroup.alpha = 0f;

        currentAnim = StartCoroutine(AnimateDetail(true));
    }

    /// <summary>
    /// 隐藏详情面板：渐隐退下
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

    // ===== 线索管理 =====

    public void AddClue(string clueID, string clueName, string description, Sprite detailImg = null)
    {
        if (clueSlotMap.ContainsKey(clueID)) return;

        if (clueSlotPrefab == null || clueSection == null)    // clueContent → clueSection
        {
            Debug.LogWarning("BackpackSystem: 线索预制体或容器未配置");
            return;
        }

        GameObject go = Instantiate(clueSlotPrefab, clueSection);    // clueContent → clueSection
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
            null,
            detailImg
        );

        clueSlotMap[clueID] = slot;
        Debug.Log($"BackpackSystem: 添加线索 [{clueID}] {clueName}");
    }

    // ===== 背包事件响应 =====

    private void OnBagOpen()
    {
        RefreshMatHighlight();
    }

    private void OnBagClose()
    {
        // 关闭背包时立即隐藏详情面板（不播动画）
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
        if (matSlots == null) return;
        var currentMat = GameState.Instance.CurrentMatState;

        for (int i = 0; i < matSlots.Count; i++)
        {
            bool isSelected = (i == (int)currentMat);
            matSlots[i].SetSelected(isSelected);
        }
    }
    private void OnMatChanged(PlayerMatState newMat)
    {
        RefreshMatHighlight();
    }
}
