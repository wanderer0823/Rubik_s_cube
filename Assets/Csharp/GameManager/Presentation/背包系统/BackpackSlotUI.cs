using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// �����������ӣ����������͵����
/// ����ʱ֪ͨ BackpackSystem ��ʾ����������塣
/// 
/// �㼶�ṹ���������ӣ���
/// SlotRoot (�˽ű�)
///   ������ Icon (Image)
///   ������ NameText (Text)
/// </summary>
public class BackpackSlotUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    public enum SlotType { Material, Clue }

    [Header("UI�������")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image iconImage;

    [Header("ѡ�и���")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color selectedColor = new Color(0.3f, 0.6f, 1f, 0.9f);
    [SerializeField] private Color hoverColor = new Color(0.4f, 0.4f, 0.4f, 0.9f);

    // ����ʱ���ݣ��� Init ���ã�
    private SlotType slotType;
    private string displayName;
    private string description;
    private Sprite detailSprite;     // ����ͼƬ
    private Action onClickCallback;
    private bool isSelected = false;
    private PlayerMatState? linkedMatState = null;   // �����������Ĳ�������

    // ����� BackpackSystem ����
    private BackpackSystem backpackSystem;

    /// <summary>
    /// ��ʼ������
    /// </summary>
    public void Init(BackpackSystem system, SlotType type, string name, string desc,
                     Action onClick = null, Sprite detail = null,PlayerMatState? matState = null)
    {
        backpackSystem = system;    // ֱ�Ӹ�ֵ������ GetComponentInParent
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

    // ===== ������� =====
    public void OnPointerEnter(PointerEventData eventData)
    {
        // ֪ͨ�������������ʾ
        if (backpackSystem != null)
            backpackSystem.ShowDetail(displayName, description, detailSprite);

        if (!isSelected && backgroundImage != null)
            backgroundImage.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // ֪ͨ���������������
        if (backpackSystem != null)
            backpackSystem.HideDetail();

        if (!isSelected && backgroundImage != null)
            backgroundImage.color = normalColor;
    }

    // ===== ����� =====
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (slotType == SlotType.Material && onClickCallback != null)
        {
            onClickCallback.Invoke();
            /*__DEBUGTOOL_START__*/Debug.Log($"BackpackSlotUI: ������� [{displayName}]");/*__DEBUGTOOL_END__*/
        }
    }

    // ===== ѡ�и��������ʸ����ã� =====
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (backgroundImage != null)
            backgroundImage.color = selected ? selectedColor : normalColor;
    }
}
