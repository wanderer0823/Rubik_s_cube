using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class StaticBatchSearchTool : EditorWindow
{
    GameObject[] roots = new GameObject[0];

    Dictionary<Material, List<GameObject>> materialGroups = new Dictionary<Material, List<GameObject>>();

    Vector2 scrollPos;

    const int threshold = 5;

    [MenuItem("Tools/Hierarchy/静态合批搜索")]
    static void Open()
    {
        GetWindow<StaticBatchSearchTool>("Static Batch Tool");
    }

    void OnGUI()
    {
        GUILayout.Label("静态合批材质工具（自动Batching）", EditorStyles.boldLabel);

        DrawRootsList();

        GUILayout.Space(10);

        if (GUILayout.Button("扫描材质分组并默认合批"))
        {
            ScanAndAutoBatch();
        }

        GUILayout.Space(10);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach (var kv in materialGroups)
        {
            Material mat = kv.Key;
            List<GameObject> gos = kv.Value;

            if (mat == null) continue;

            if (gos.Count <= threshold) continue;

            GUILayout.BeginVertical("box");

            GUILayout.BeginHorizontal();

            if (GUILayout.Button($"{mat.name} | Count: {gos.Count}"))
            {
                Selection.objects = gos.ToArray();

                if (SceneView.lastActiveSceneView != null)
                    SceneView.lastActiveSceneView.FrameSelected();
            }

            if (GUILayout.Button("取消合批", GUILayout.Width(80)))
            {
                UnbatchMaterialGroup(gos);
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawRootsList()
    {
        GUILayout.Label("父节点列表", EditorStyles.boldLabel);

        int newSize = Mathf.Max(0, EditorGUILayout.IntField("数量", roots.Length));

        if (newSize != roots.Length)
            System.Array.Resize(ref roots, newSize);

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

    // =========================
    // 扫描 + 自动合批
    // =========================
    void ScanAndAutoBatch()
    {
        materialGroups.Clear();

        foreach (var root in roots)
        {
            if (root == null) continue;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

            foreach (var r in renderers)
            {
                if (r == null) continue;

                if (r is SkinnedMeshRenderer) continue;

                if (r.GetComponent<MeshFilter>() == null) continue;

                Material mat = r.sharedMaterial;
                if (mat == null) continue;

                if (!materialGroups.ContainsKey(mat))
                    materialGroups[mat] = new List<GameObject>();

                materialGroups[mat].Add(r.gameObject);
            }
        }

        // ✔ 自动合批
        foreach (var kv in materialGroups)
        {
            if (kv.Value.Count <= threshold) continue;

            foreach (var go in kv.Value)
            {
                if (go == null) continue;

                Undo.RecordObject(go, "Enable Batching Static");

                // ❗只影响当前物体，不递归子物体
                GameObjectUtility.SetStaticEditorFlags(
                    go,
                    GameObjectUtility.GetStaticEditorFlags(go) | StaticEditorFlags.BatchingStatic
                );
            }
        }

        Debug.Log($"扫描完成并自动合批：材质 {materialGroups.Count}");
    }

    // =========================
    // 取消合批（按材质组）
    // =========================
    void UnbatchMaterialGroup(List<GameObject> gos)
    {
        foreach (var go in gos)
        {
            if (go == null) continue;

            Undo.RecordObject(go, "Disable Batching Static");

            var flags = GameObjectUtility.GetStaticEditorFlags(go);

            flags &= ~StaticEditorFlags.BatchingStatic;

            GameObjectUtility.SetStaticEditorFlags(go, flags);
        }

        Debug.Log("已取消该材质组的静态合批");
    }
}