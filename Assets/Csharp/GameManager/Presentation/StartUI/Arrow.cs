using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class Arrow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject arrowObject;
    public float scaleDuration = 0.3f;
    public float targetScale = 1.2f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 自定义缓动曲线

    private RectTransform arrowRectTransform;
    private Vector3 originalScale;

    void Start()
    {
        if (arrowObject != null)
        {
            arrowObject.SetActive(false);
            arrowRectTransform = GetComponent<RectTransform>();
            if (arrowRectTransform != null)
            {
                originalScale = arrowRectTransform.localScale;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (arrowObject != null && arrowRectTransform != null)
        {
            arrowObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(ScaleSlowly(true));
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (arrowObject != null && arrowRectTransform != null)
        {
            StopAllCoroutines();
            StartCoroutine(ScaleSlowly(false));
        }
    }
    
    // 统一的缩放协程，isGrowing决定是变大还是变小
    private IEnumerator ScaleSlowly(bool isGrowing)
    {
        Vector3 startScale = arrowRectTransform.localScale;
        Vector3 endScale = isGrowing ? originalScale * targetScale : originalScale;
        float elapsedTime = 0f;
        
        while (elapsedTime < scaleDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / scaleDuration;
            
            // 使用AnimationCurve提供更丰富的缓动效果
            float curveValue = scaleCurve.Evaluate(t);
            
            arrowRectTransform.localScale = Vector3.Lerp(startScale, endScale, curveValue);
            yield return null;
        }
        
        arrowRectTransform.localScale = endScale;
        
        // 变小完成后隐藏对象
        if (!isGrowing && arrowObject != null)
        {
            arrowObject.SetActive(false);
        }
    }
}