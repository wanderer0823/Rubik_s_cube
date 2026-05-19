using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ����ϵͳ����������ڸ��� + �Ҳ๫��������塣
/// ���� BackpackPanel �ϡ�
/// </summary>
public class BackpackSystem : MonoBehaviour
{
    [Header("ScrollRect")]
    [SerializeField] private ScrollRect scrollRect;

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
    [SerializeField] private Transform clueSection;    // ������ָ�� ClueSection

    [Header("线索的背包UI单位")]
    [SerializeField] private GameObject clueSlotPrefab;

    [Header("详情面板引用BackpackPanel")]
    [SerializeField] private RectTransform detailPopupPanel;
    [SerializeField] private TMP_Text detailNameText;
    [SerializeField] private TMP_Text detailDescText;
    [SerializeField] private Image detailImage;

    [Header("详情面板淡入淡出")]
    [SerializeField] private float animDuration = 0.25f;
    [SerializeField] private float hideOffsetY = -200f;   // ����ʱ�����ʾλ�õ�Yƫ�ƣ���Ļ�·���

    // ����������ʾλ�ã��ɳ�ʼλ�þ�����
    private Vector2 detailShowPos;
    private Vector2 detailHidePos;
    private Coroutine currentAnim;
    private CanvasGroup detailCanvasGroup;

    // �Ѵ�������������
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
    }

    // ===== ��ʼ�� =====
    private void InitDetailPanel()
    {
        if (detailPopupPanel == null) return;

        // ��¼��ʾλ��
        detailShowPos = detailPopupPanel.anchoredPosition;
        detailHidePos = detailShowPos + new Vector2(0, hideOffsetY);

        // ȷ���� CanvasGroup ���ڵ��뵭��
        detailCanvasGroup = detailPopupPanel.GetComponent<CanvasGroup>();
        if (detailCanvasGroup == null)
            detailCanvasGroup = detailPopupPanel.gameObject.AddComponent<CanvasGroup>();

        // ��ʼ����
        detailCanvasGroup.alpha = 0f;
        detailPopupPanel.anchoredPosition = detailHidePos;
        detailPopupPanel.gameObject.SetActive(false);
    }

    // ===== �������������ƣ��� BackpackSlotUI ���ã�=====

    /// <summary>
    /// ��ʾ������壺���·����Ը���
    /// </summary>
    public void ShowDetail(string itemName, string description, Sprite image)
    {
        if (detailPopupPanel == null) return;

        // �������
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

        // ÿ���������ӵײ����¸���
        if (currentAnim != null)
            StopCoroutine(currentAnim);

        detailPopupPanel.gameObject.SetActive(true);
        // ���õ��ײ���ʼλ��
        detailPopupPanel.anchoredPosition = detailHidePos;
        detailCanvasGroup.alpha = 0f;

        currentAnim = StartCoroutine(AnimateDetail(true));
    }

    /// <summary>
    /// ����������壺��������
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

    // ��������
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

    // ===== �������� =====

    public void AddClue(string clueID, string clueName, string description, Sprite detailImg = null, Sprite icon = null)
    {
        if (clueSlotMap.ContainsKey(clueID)) return;

        if (clueSlotPrefab == null || clueSection == null)    // clueContent �� clueSection
        {
            /*__DEBUGTOOL_START__*/Debug.LogWarning("BackpackSystem: ����Ԥ���������δ����");/*__DEBUGTOOL_END__*/
            return;
        }

        GameObject go = Instantiate(clueSlotPrefab, clueSection);    // clueContent �� clueSection
        BackpackSlotUI slot = go.GetComponent<BackpackSlotUI>();

        if (slot == null)
        {
            Debug.LogError("BackpackSystem: ����Ԥ����ȱ�� BackpackSlotUI ���");
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
        /*__DEBUGTOOL_START__*/Debug.Log($"BackpackSystem: ������� [{clueID}] {clueName}");/*__DEBUGTOOL_END__*/
    }

    // ===== ���ʹ��� =====
    public void AddMat(PlayerMatState matType, string matName, string desc, Sprite detail = null)
    {
        // ����Ƿ����иò���
        foreach (var existingSlot in matSlots)
        {
            if (existingSlot != null && existingSlot.gameObject.name == matName)
                return;
        }

        if (matSlotPrefab == null || matSection == null)
        {
            Debug.LogError("BackpackSystem: ����Ԥ���������δ����");
            return;
        }

        GameObject go = Instantiate(matSlotPrefab, matSection);
        go.name = matName;
        BackpackSlotUI newSlot = go.GetComponent<BackpackSlotUI>();

        if (newSlot == null)
        {
            Debug.LogError("BackpackSystem: ����Ԥ����ȱ�� BackpackSlotUI");
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
            iconSprite    // 新增
        );


        matSlots.Add(newSlot);
        /*__DEBUGTOOL_START__*/Debug.Log($"BackpackSystem: ��Ӳ��� [{matName}]");/*__DEBUGTOOL_END__*/
    }

    // ===== �����¼���Ӧ =====

    private void OnBagOpen()
    {
        RefreshMatHighlight();
    }

    private void OnBagClose()
    {
        // �رձ���ʱ��������������壨����������
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

        // ��̬�б��޷��� index ��Ӧ enum����������ƥ��
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
