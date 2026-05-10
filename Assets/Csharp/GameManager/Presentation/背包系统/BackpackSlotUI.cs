using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 单个背包格子：处理悬浮和点击。
/// 悬浮时通知 BackpackSystem 显示公共详情面板。
/// 
/// 层级结构（单个格子）：
/// SlotRoot (此脚本)
///   ├── Icon (Image)
///   └── NameText (Text)
/// </summary>
public class BackpackSlotUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    public enum SlotType { Material, Clue }

    [Header("UI组件引用")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image iconImage;

    [Header("选中高亮")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color selectedColor = new Color(0.3f, 0.6f, 1f, 0.9f);
    [SerializeField] private Color hoverColor = new Color(0.4f, 0.4f, 0.4f, 0.9f);

    // 运行时数据（由 Init 设置）
    private SlotType slotType;
    private string displayName;
    private string description;
    private Sprite detailSprite;     // 详情图片
    private Action onClickCallback;
    private bool isSelected = false;
    private PlayerMatState? linkedMatState = null;   // 新增：关联的材质类型

    // 所属的 BackpackSystem 引用
    private BackpackSystem backpackSystem;

    /// <summary>
    /// 初始化格子
    /// </summary>
    public void Init(BackpackSystem system, SlotType type, string name, string desc,
                     Action onClick = null, Sprite detail = null,PlayerMatState? matState = null)
    {
        backpackSystem = system;    // 直接赋值，不再 GetComponentInParent
        slotType = type;
        displayName = name;
        description = desc;
        detailSprite = detail;
        onClickCallback = onClick;
        linkedMatState = matState;

        if (nameText != null)
            nameText.text = displayName;

        if (backgroundImage != null)
            backgroundImage.color = normalColor;
    }

    public PlayerMatState? LinkedMatState => linkedMatState;

    // ===== 鼠标悬浮 =====
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 通知公共详情面板显示
        if (backpackSystem != null)
            backpackSystem.ShowDetail(displayName, description, detailSprite);

        if (!isSelected && backgroundImage != null)
            backgroundImage.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 通知公共详情面板隐藏
        if (backpackSystem != null)
            backpackSystem.HideDetail();

        if (!isSelected && backgroundImage != null)
            backgroundImage.color = normalColor;
    }

    // ===== 鼠标点击 =====
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (slotType == SlotType.Material && onClickCallback != null)
        {
            onClickCallback.Invoke();
            Debug.Log($"BackpackSlotUI: 点击材质 [{displayName}]");
        }
    }

    // ===== 选中高亮（材质格子用） =====
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (backgroundImage != null)
            backgroundImage.color = selected ? selectedColor : normalColor;
    }
}
