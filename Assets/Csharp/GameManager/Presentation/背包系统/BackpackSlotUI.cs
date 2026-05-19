using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 背包槽位组件：支持材质和线索两种类型
/// 悬停时通知 BackpackSystem 显示详情弹窗。
/// 
/// 层级结构（示例）：
/// SlotRoot (挂载本脚本)
///   └─ Icon (Image)
///   └─ NameText (Text)
/// </summary>
public class BackpackSlotUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    public enum SlotType { Material, Clue }

    [Header("UI子对象引用")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image iconImage;

    [Header("选中高亮")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color selectedColor = new Color(0.3f, 0.6f, 1f, 0.9f);
    [SerializeField] private Color hoverColor = new Color(0.4f, 0.4f, 0.4f, 0.9f);

    // 初始化时存储的数据（Init 方法调用）
    private SlotType slotType;
    private string displayName;
    private string description;
    private Sprite detailSprite;     // 详情图片
    private Action onClickCallback;
    private bool isSelected = false;
    private PlayerMatState? linkedMatState = null;   // 仅材质槽位关联的材质类型

    // 缓存 BackpackSystem 引用
    private BackpackSystem backpackSystem;

    /// <summary>
    /// 初始化槽位
    /// </summary>
    public void Init(BackpackSystem system, SlotType type, string name, string desc,
                     Action onClick = null, Sprite detail = null, PlayerMatState? matState = null,
                     Sprite icon = null, Color? iconColor = null, TMP_FontAsset fontAsset = null)
    {
        backpackSystem = system;
        slotType = type;
        displayName = name;
        description = desc;
        detailSprite = detail;
        onClickCallback = onClick;
        linkedMatState = matState;

        // 设置名称
        if (nameText != null)
            nameText.text = displayName;

        // 设置字体（若传入有效字体）
        if (fontAsset != null && nameText != null)
            nameText.font = fontAsset;

        // 设置图标
        if (iconImage != null)
        {
            if (icon != null)
                iconImage.sprite = icon;
            // 关键：重置颜色为白色（或指定颜色），防止预制体残留的蓝色
            iconImage.color = iconColor ?? Color.white;
        }

        // 背景初始化
        if (backgroundImage != null)
            backgroundImage.color = normalColor;
    }

    public PlayerMatState? LinkedMatState => linkedMatState;

    // ===== 鼠标事件 =====
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 通知背包系统显示详情
        if (backpackSystem != null)
            backpackSystem.ShowDetail(displayName, description, detailSprite);

        if (!isSelected && backgroundImage != null)
            backgroundImage.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 通知背包系统隐藏详情
        if (backpackSystem != null)
            backpackSystem.HideDetail();

        if (!isSelected && backgroundImage != null)
            backgroundImage.color = normalColor;
    }

    // ===== 点击逻辑 =====
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (slotType == SlotType.Material && onClickCallback != null)
        {
            onClickCallback.Invoke();
            /*__DEBUGTOOL_START__*/
            Debug.Log($"BackpackSlotUI: 点击材质 [{displayName}]");/*__DEBUGTOOL_END__*/
        }
    }

    // ===== 选中高亮（供外部调用，用于材质高亮）=====
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (backgroundImage != null)
            backgroundImage.color = selected ? selectedColor : normalColor;
    }
}