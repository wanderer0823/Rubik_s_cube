using UnityEngine;

/// <summary>
/// ����ʰȡ�����ڳ��������� prefab �ϡ�
/// ����� View3 ������ E ʰȡ�����ñ��浽������
/// </summary>
public class CluePickup : MonoBehaviour
{
    [Header("线索信息")]
    public string clueID;
    public string clueName;
    [TextArea(2, 5)]
    public string description;
    public Sprite detailImage;
    public Sprite iconSprite;    // 新增：背包内小图标

    [Header("ʰȡ����")]
    public float pickupRange = 3f;  // ʰȡ����

    private bool isPickedUp = false;

    void OnEnable()
    {
        GameEvents.OnInteractExecute += TryPickup;
    }

    void OnDisable()
    {
        GameEvents.OnInteractExecute -= TryPickup;
    }

    private void TryPickup()
    {
        if (isPickedUp) return;

        // �����λ�ö�����ball
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        float dist = Vector3.Distance(player.transform.position, transform.position);
        if (dist > pickupRange) return;

        var gs = GameState.Instance;
        if (gs == null || gs.HasClue(clueID)) return;

        gs.CollectClue(clueID);
        isPickedUp = true;

        var backpack = FindObjectOfType<BackpackSystem>();
        if (backpack != null)
        {
            backpack.AddClue(clueID, clueName, description, detailImage, iconSprite);
        }

        /*__DEBUGTOOL_START__*/
        Debug.Log($"ʰȡ������{clueName}");/*__DEBUGTOOL_END__*/
        gameObject.SetActive(false);
    }
}
