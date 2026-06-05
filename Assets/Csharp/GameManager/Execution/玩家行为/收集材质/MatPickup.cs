using UnityEngine;
using UnityEngine.UI;
public class MatPickup : MonoBehaviour
{
    [Header("材质类型")]
    public PlayerMatState matType;

    [Header("背包详情文字设置")]
    public string displayName;
    [TextArea(2, 4)]
    public string description;
    public Sprite detailImage;
    public Sprite iconSprite;

    [Header("可拾取距离")]
    public float pickupRange = 3f;

    private bool isPickedUp = false;

    void OnEnable()
    {
        GameEvents.OnInteractExecute += TryPickup;
    }

    void OnDisable()
    {
        GameEvents.OnInteractExecute -= TryPickup;
    }

    void TryPickup()
    {
        if (isPickedUp) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        float dist = Vector3.Distance(player.transform.position, transform.position);
        if (dist > pickupRange) return;

        isPickedUp = true;

        var backpack = FindObjectOfType<BackpackSystem>();
        if (backpack != null)
        {
            backpack.AddMat(matType, displayName, description, detailImage);
        }

        /*__DEBUGTOOL_START__*/Debug.Log($"拿到{displayName}");/*__DEBUGTOOL_END__*/
        gameObject.SetActive(false);
    }
}
