using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Arrow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject arrowObject; // 箭头UI对象

    void Start()
    {
        if (arrowObject != null)
            arrowObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (arrowObject != null)
            arrowObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (arrowObject != null)
            arrowObject.SetActive(false);
    }
}