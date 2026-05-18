using UnityEngine;

/// <summary>
/// ����ʰȡ�����ڳ��������� prefab �ϡ�
/// ����� View3 ������ E ʰȡ�����ñ��浽������
/// </summary>
public class CluePickup : MonoBehaviour
{
    [Header("������Ϣ")]
    public string clueID;           // Ψһ��ʶ
    public string clueName;         // ��ʾ����
    [TextArea(2, 5)]
    public string description;      // �����ı�
    public Sprite detailImage;      // ����ͼƬ����ѡ��

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
            backpack.AddClue(clueID, clueName, description, detailImage);
        }

        /*__DEBUGTOOL_START__*/Debug.Log($"ʰȡ������{clueName}");/*__DEBUGTOOL_END__*/
        gameObject.SetActive(false);
    }
}
