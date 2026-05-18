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

        if (GUILayout.Button("执行优化（在 Hierarchy 多选根节点）"))
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

        var roots = Selection.gameObjects;
        if (roots == null || roots.Length == 0)
        {
            Debug.LogError("请先在 Hierarchy 中选中至少一个根物体（支持多选）。");
            return;
        }

        var processedRenderers = new HashSet<Renderer>();

        foreach (var root in roots)
        {
            if (root == null) continue;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                if (!processedRenderers.Add(r)) continue;

                float size = r.bounds.size.magnitude;
                bool isSmall = size < sizeThreshold;

                if (isSmall)
                {
                    if (r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off
                        || r.receiveShadows)
                    {
                        Undo.RecordObject(r, "Small Object Shadow Off");

                        if (r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off)
                        {
                            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                            affectedObjects.Add(r);
                        }
                        if (r.receiveShadows)
                            r.receiveShadows = false;

                        EditorUtility.SetDirty(r);
                    }
                }
                else
                {
                    // 仅恢复"被本工具改过、且 size 上来了"的物体
                    // 这里依然不强制 On，避免覆盖人工设置
                    if (r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off
                        && !r.receiveShadows)
                    {
                        Undo.RecordObject(r, "Small Object Shadow Restore");
                        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                        r.receiveShadows = true;
                        EditorUtility.SetDirty(r);
                    }
                }
            }
        }

        AssetDatabase.SaveAssets();

        Debug.Log($"优化完成：选中根 {roots.Length} 个，处理小物体 {affectedObjects.Count} 个");
    }

    void PrintList()
    {
        Debug.Log("===== 被关闭阴影的小物体列表 =====");

        foreach (var r in affectedObjects)
        {
            if (r == null) continue;

            string path = GetHierarchyPath(r.transform);
            Debug.Log($"[Shadow Off] {path}", r.gameObject);
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
