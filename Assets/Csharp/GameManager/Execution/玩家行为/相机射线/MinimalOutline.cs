using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 轻量描边：通过复制 Mesh 并放大渲染纯色实现。
/// 挂在 Grabbable 物体上，默认禁用，GrabSystem 控制开关。
/// </summary>
[RequireComponent(typeof(Renderer))]
public class MinimalOutline : MonoBehaviour
{
    [Header("描边设置")]
    public Color outlineColor = Color.yellow;
    [Range(0.01f, 0.1f)]
    public float outlineWidth = 0.03f;

    private GameObject outlineObj;
    private Renderer sourceRenderer;

    void Awake()
    {
        sourceRenderer = GetComponent<Renderer>();
        CreateOutline();
        SetEnabled(false);
    }

    void CreateOutline()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        outlineObj = new GameObject("_Outline");
        outlineObj.transform.SetParent(transform, false);
        outlineObj.transform.localPosition = Vector3.zero;
        outlineObj.transform.localRotation = Quaternion.identity;
        outlineObj.transform.localScale = Vector3.one * (1f + outlineWidth);

        MeshFilter outlineMF = outlineObj.AddComponent<MeshFilter>();
        outlineMF.sharedMesh = mf.sharedMesh;

        MeshRenderer outlineMR = outlineObj.AddComponent<MeshRenderer>();
        Material outlineMat = new Material(Shader.Find("Unlit/Color"));
        outlineMat.color = outlineColor;
        outlineMR.material = outlineMat;
        outlineMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        outlineMR.receiveShadows = false;

        outlineObj.SetActive(false);
    }

    public void SetEnabled(bool enabled)
    {
        if (outlineObj != null)
            outlineObj.SetActive(enabled);
    }

    public void SetColor(Color color)
    {
        outlineColor = color;
        if (outlineObj != null)
        {
            var mr = outlineObj.GetComponent<MeshRenderer>();
            if (mr != null)
                mr.material.color = color;
        }
    }

    void OnDestroy()
    {
        if (outlineObj != null)
            Destroy(outlineObj);
    }
}
