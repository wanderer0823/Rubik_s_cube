using UnityEngine;
using System.Collections.Generic;

public class MinimalOutline : MonoBehaviour
{
    [Header("描边设置")]
    public Material outlineMaterial = null;

    [Range(0.01f, 0.1f)]
    public float outlineWidth = 0.03f;


    // 所有描边对象
    private List<GameObject> outlineObjects = new List<GameObject>();

    void Awake()
    {
        CreateOutline();
        SetEnabled(false);
    }

    void CreateOutline()
    {
        // 获取所有子 MeshFilter
        MeshFilter[] meshFilters =
            GetComponentsInChildren<MeshFilter>(true);

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.sharedMesh == null)
                continue;

            MeshRenderer sourceRenderer =
                mf.GetComponent<MeshRenderer>();

            if (sourceRenderer == null)
                continue;

            // 创建描边对象
            GameObject outlineObj =
                new GameObject(mf.gameObject.name + "_Outline");

            outlineObj.transform.SetParent(mf.transform, false);

            outlineObj.transform.localPosition = Vector3.zero;
            outlineObj.transform.localRotation = Quaternion.identity;

            outlineObj.transform.localScale =
                Vector3.one * (1f + outlineWidth);

            // Mesh
            MeshFilter outlineMF =
                outlineObj.AddComponent<MeshFilter>();

            outlineMF.sharedMesh = mf.sharedMesh;

            // Renderer
            MeshRenderer outlineMR =
                outlineObj.AddComponent<MeshRenderer>();

            Material outlineMat =outlineMaterial;

            outlineMR.material = outlineMat;

            //outlineMR.shadowCastingMode =
            //    UnityEngine.Rendering.ShadowCastingMode.Off;

            //outlineMR.receiveShadows = false;

            // 防止描边被原模型挡住
            outlineMR.sortingOrder = -1;

            outlineObj.SetActive(false);

            outlineObjects.Add(outlineObj);
        }
    }

    public void SetEnabled(bool enabled)
    {
        foreach (GameObject obj in outlineObjects)
        {
            if (obj != null)
                obj.SetActive(enabled);
        }
    }



    void OnDestroy()
    {
        foreach (GameObject obj in outlineObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
    }
}