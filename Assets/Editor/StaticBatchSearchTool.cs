using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class StaticBatchSearchTool : EditorWindow
{
    GameObject[] roots = new GameObject[0];

    Dictionary<Material, List<GameObject>> materialGroups = new Dictionary<Material, List<GameObject>>();
    Dictionary<string, bool> groupFoldouts = new Dictionary<string, bool>();

    Vector2 scrollPos;

    const int threshold = 5;

    [MenuItem("Tools/Hierarchy/静态合批搜索")]
    static void Open()
    {
        GetWindow<StaticBatchSearchTool>("Static Batch Tool");
    }

    void OnGUI()
    {
        GUILayout.Label("【一键合批hierarchy中多选的房间】依次点击按钮，进度条加载完了再点下一个", EditorStyles.boldLabel);

        DrawRootsList();

        GUILayout.Space(10);

        if (GUILayout.Button("【1-一键合批】扫描材质分组并默认合批"))
        {
            ScanAndAutoBatch();
        }
        if (GUILayout.Button("【2-合批后更新状态】仅刷新缓存（不改变Static标记）"))
        {
            RefreshBatchingCacheOnly();
        }


        GUILayout.Space(10);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // 收集所有要显示的材质组
        var displayItems = new List<KeyValuePair<Material, List<GameObject>>>();
        foreach (var kv in materialGroups)
        {
            if (kv.Key == null) continue;
            if (kv.Value.Count <= threshold) continue;
            displayItems.Add(kv);
        }

        // 拆分：Count > 50 与 Count <= 50
        var highList = new List<KeyValuePair<Material, List<GameObject>>>();
        var lowList = new List<KeyValuePair<Material, List<GameObject>>>();
        foreach (var kv in displayItems)
        {
            if (kv.Value.Count > 50) highList.Add(kv);
            else lowList.Add(kv);
        }

        // 高量组：按 Count 降序
        highList.Sort((a, b) => b.Value.Count.CompareTo(a.Value.Count));

        // 低量组：按首字母分组（每3字母一组）
        var grouped = new SortedDictionary<string, List<KeyValuePair<Material, List<GameObject>>>>();
        foreach (var kv in lowList)
        {
            string g = GetGroupKey(kv.Key.name);
            if (!grouped.TryGetValue(g, out var list))
            {
                list = new List<KeyValuePair<Material, List<GameObject>>>();
                grouped[g] = list;
            }
            list.Add(kv);
        }

        // 渲染高量组
        if (highList.Count > 0)
        {
            GUILayout.Label($"Count > 50（按数量降序，共 {highList.Count} 个）", EditorStyles.boldLabel);
            foreach (var kv in highList)
                DrawMaterialRow(kv.Key, kv.Value);
            GUILayout.Space(8);
        }

        // 渲染低量组（折叠）
        if (grouped.Count > 0)
        {
            GUILayout.Label("Count ≤ 50（按首字母分组）", EditorStyles.boldLabel);
            foreach (var pair in grouped)
            {
                if (!groupFoldouts.TryGetValue(pair.Key, out bool open))
                    open = false;

                open = EditorGUILayout.Foldout(open, $"{pair.Key}  ({pair.Value.Count})", true);
                groupFoldouts[pair.Key] = open;

                if (open)
                {
                    EditorGUI.indentLevel++;
                    pair.Value.Sort((a, b) => string.Compare(a.Key.name, b.Key.name, System.StringComparison.OrdinalIgnoreCase));
                    foreach (var kv in pair.Value)
                        DrawMaterialRow(kv.Key, kv.Value);
                    EditorGUI.indentLevel--;
                }
            }
        }
        EditorGUILayout.EndScrollView();

    }

    void DrawRootsList()
    {
        GUILayout.Label("父节点列表（在 Hierarchy 中选中房间根节点，支持多选）", EditorStyles.boldLabel);

        var sel = Selection.gameObjects;
        int count = sel != null ? sel.Length : 0;
        EditorGUILayout.LabelField($"当前 Hierarchy 已选中: {count} 个 GameObject");

        if (count > 0)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < count && i < 30; i++)
                EditorGUILayout.LabelField($"- {sel[i].name}");
            if (count > 30)
                EditorGUILayout.LabelField($"... 还有 {count - 30} 个未显示");
            EditorGUI.indentLevel--;
        }

        if (roots != null && roots.Length > 0)
        {
            EditorGUILayout.HelpBox(
                $"上次扫描的 Roots 数量: {roots.Length}（点击下方按钮会用当前 Hierarchy 选中刷新）",
                MessageType.None);
        }
    }

    // =========================
    // 扫描 + 自动合批
    // =========================
    void ScanAndAutoBatch()
    {
        // 从 Hierarchy 选中读取 Roots
        if (Selection.gameObjects != null && Selection.gameObjects.Length > 0)
            roots = Selection.gameObjects;

        if (roots == null || roots.Length == 0)
        {
            Debug.LogWarning("请先在 Hierarchy 中选中房间根节点（支持多选）。");
            return;
        }

        materialGroups.Clear();


        // 每个 root 对应的待缓存条目
        var perRootEntries = new Dictionary<GameObject, List<RoomBatchingCache.Entry>>();

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

        // 自动合批 + 收集缓存条目
        foreach (var kv in materialGroups)
        {
            if (kv.Value.Count <= threshold) continue;

            foreach (var go in kv.Value)
            {
                if (go == null) continue;

                Undo.RecordObject(go, "Enable Batching Static");

                // 只影响当前物体，不递归子物体
                GameObjectUtility.SetStaticEditorFlags(
                    go,
                    GameObjectUtility.GetStaticEditorFlags(go) | StaticEditorFlags.BatchingStatic
                );

                // 找到所属的房间根
                var owningRoot = FindOwningRoot(go);
                if (owningRoot == null) continue;

                if (!perRootEntries.TryGetValue(owningRoot, out var list))
                {
                    list = new List<RoomBatchingCache.Entry>();
                    perRootEntries[owningRoot] = list;
                }

                var mf = go.GetComponent<MeshFilter>();
                var rd = go.GetComponent<Renderer>();
                list.Add(new RoomBatchingCache.Entry
                {
                    meshFilter = mf,
                    renderer = rd,
                    originalMesh = mf != null ? mf.sharedMesh : null,
                });
            }
        }

        // 写入每个 root 的 RoomBatchingCache（一根一个组件）
        foreach (var pair in perRootEntries)
        {
            var root = pair.Key;
            var cache = root.GetComponent<RoomBatchingCache>();
            if (cache == null)
                cache = Undo.AddComponent<RoomBatchingCache>(root);

            Undo.RecordObject(cache, "Write Batching Cache");
            cache.entries = pair.Value.ToArray();
            EditorUtility.SetDirty(cache);
            EditorUtility.SetDirty(root);
        }

        Debug.Log($"扫描完成，材质组 {materialGroups.Count}，已写入 {perRootEntries.Count} 个房间根缓存。");
    }

    GameObject FindOwningRoot(GameObject go)
    {
        GameObject topMost = null;
        Transform t = go.transform;
        while (t != null)
        {
            foreach (var root in roots)
            {
                if (root == null) continue;
                if (t == root.transform)
                {
                    topMost = root; // 继续向上找，看是否还有更高层的 root
                    break;
                }
            }
            t = t.parent;
        }
        return topMost;
    }

    void RefreshBatchingCacheOnly()
    {
        if (Selection.gameObjects != null && Selection.gameObjects.Length > 0)
            roots = Selection.gameObjects;

        if (roots == null || roots.Length == 0)
        {
            Debug.LogWarning("请先在 Hierarchy 中选中房间根节点（支持多选）。");
            return;
        }

        int rootHandled = 0;
        int entryCount = 0;

        foreach (var root in roots)
        {
            if (root == null) continue;

            var list = new List<RoomBatchingCache.Entry>();

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r == null) continue;
                if (r is SkinnedMeshRenderer) continue;

                var mf = r.GetComponent<MeshFilter>();
                if (mf == null) continue;

                var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
                if ((flags & StaticEditorFlags.BatchingStatic) == 0) continue;

                list.Add(new RoomBatchingCache.Entry
                {
                    meshFilter = mf,
                    renderer = r,
                    originalMesh = mf.sharedMesh,
                });
            }

            var cache = root.GetComponent<RoomBatchingCache>();
            if (list.Count == 0)
            {
                // 没有需要缓存的，顺手清掉旧组件
                if (cache != null)
                {
                    Undo.DestroyObjectImmediate(cache);
                }
                continue;
            }

            if (cache == null)
                cache = Undo.AddComponent<RoomBatchingCache>(root);

            Undo.RecordObject(cache, "Refresh Batching Cache");
            cache.entries = list.ToArray();
            EditorUtility.SetDirty(cache);
            EditorUtility.SetDirty(root);

            rootHandled++;
            entryCount += list.Count;
        }

        Debug.Log($"刷新缓存完成：{rootHandled} 个房间根，共 {entryCount} 条 Entry。");
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

    //新增
    void OnSelectionChange()
    {
        Repaint();
    }
    string GetGroupKey(string name)
    {
        if (string.IsNullOrEmpty(name)) return "其他";
        char c = char.ToUpper(name[0]);
        if (c < 'A' || c > 'Z') return "其他";
        int idx = (c - 'A') / 3;
        char start = (char)('A' + idx * 3);
        char end = (char)Mathf.Min(start + 2, 'Z');
        return $"{start}-{end}";
    }

    void DrawMaterialRow(Material mat, List<GameObject> gos)
    {
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

}