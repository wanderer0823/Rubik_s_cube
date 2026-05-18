using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SmallObjectShadowTool : EditorWindow
{
    float sizeThreshold = 6.0f;

    List<Renderer> affectedObjects = new List<Renderer>();

    [MenuItem("Tools/优化/小物体关闭阴影")]
    static void Open()
    {
        GetWindow<SmallObjectShadowTool>("小物体阴影优化");
    }

    void OnGUI()
    {
        GUILayout.Label("小物体阴影一键优化（可追踪版）", EditorStyles.boldLabel);

        sizeThreshold = EditorGUILayout.FloatField("体积阈值", sizeThreshold);

        GUILayout.Space(10);

        if (GUILayout.Button("执行优化"))
        {
            Optimize();
        }

        if (affectedObjects.Count > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label($"已处理物体：{affectedObjects.Count}");

            if (GUILayout.Button("在Console输出列表"))
            {
                PrintList();
            }
        }
    }

    void Optimize()
    {
        affectedObjects.Clear();

        GameObject root = Selection.activeGameObject;
        if (root == null)
        {
            Debug.LogError("请先选中一个根物体");
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in renderers)
        {
            if (r == null) continue;

            float size = r.bounds.size.magnitude;
            bool isSmall = size < sizeThreshold;

            // ?只在状态不同的时候修改
            if (isSmall)
            {
                if (r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off)
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    affectedObjects.Add(r);
                }

                if (r.receiveShadows)
                {
                    r.receiveShadows = false;
                }
            }
            else
            {
                // ?关键：不要强制 On
                // 只恢复“被你改过的小物体”
                if (r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off)
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                }

                if (!r.receiveShadows)
                {
                    r.receiveShadows = true;
                }
            }
        }

        Debug.Log($"优化完成：处理小物体 {affectedObjects.Count} 个");
    }
    void PrintList()
    {
        Debug.Log("===== 被关闭阴影的小物体列表 =====");

        foreach (var r in affectedObjects)
        {
            if (r == null) continue;

            string path = GetHierarchyPath(r.transform);

            Debug.Log($"[Shadow Off] {path}", r.gameObject);

            // 在Hierarchy高亮
            EditorGUIUtility.PingObject(r.gameObject);
        }
    }

    string GetHierarchyPath(Transform t)
    {
        string path = t.name;

        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }

        return path;
    }
}