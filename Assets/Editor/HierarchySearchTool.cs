using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class HierarchySearchTool : EditorWindow
{
    GameObject[] roots = new GameObject[0];
    string keyword = "h";

    List<GameObject> results = new List<GameObject>();

    [MenuItem("Tools/Hierarchy/多父节点子级搜索")]
    static void Open()
    {
        GetWindow<HierarchySearchTool>("MulSearch");
    }

    void OnGUI()
    {
        GUILayout.Label("多父节点子级搜索工具", EditorStyles.boldLabel);

        GUILayout.Space(5);

        DrawRootsList();

        keyword = EditorGUILayout.TextField("名称关键字", keyword);

        GUILayout.Space(10);

        if (GUILayout.Button("搜索"))
        {
            Search();
        }

        if (results.Count > 0)
        {
            GUILayout.Label($"命中数量：{results.Count}");

            if (GUILayout.Button("选中全部"))
            {
                Selection.objects = results.ToArray();
            }
        }
    }

    void DrawRootsList()
    {
        GUILayout.Label("父节点列表", EditorStyles.boldLabel);

        int newSize = Mathf.Max(0, EditorGUILayout.IntField("数量", roots.Length));

        if (newSize != roots.Length)
        {
            System.Array.Resize(ref roots, newSize);
        }

        for (int i = 0; i < roots.Length; i++)
        {
            roots[i] = (GameObject)EditorGUILayout.ObjectField(
                $"Root {i}",
                roots[i],
                typeof(GameObject),
                true
            );
        }
    }

    void Search()
    {
        results.Clear();

        if (roots == null || roots.Length == 0)
        {
            Debug.LogError("请先添加父节点");
            return;
        }

        foreach (var root in roots)
        {
            if (root == null) continue;

            Transform[] all = root.GetComponentsInChildren<Transform>(true);

            foreach (var t in all)
            {
                if (t == root.transform) continue;

                if (!string.IsNullOrEmpty(t.name) &&
                    t.name.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    results.Add(t.gameObject);
                    Debug.Log($"命中: {GetPath(t)}", t.gameObject);
                }
            }
        }
    }

    string GetPath(Transform t)
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