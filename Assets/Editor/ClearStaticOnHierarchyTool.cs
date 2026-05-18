using UnityEngine;
using UnityEditor;

public static class ClearStaticOnHierarchyTool
{
    // ===================================================
    // 清除 Batching Static
    // ===================================================
    [MenuItem("GameObject/Static工具/取消所有子孙的 Batching Static", false, 0)]
    static void ClearBatchingOnContextMenu(MenuCommand cmd)
    {
        // 多选场景下 Unity 会对每个对象触发一次，这里直接处理 context 即可
        var go = cmd.context as GameObject;
        if (go == null) return;
        ClearRecursive(go, StaticEditorFlags.BatchingStatic, "BatchingStatic", removeCache: true);
    }

    [MenuItem("Tools/Hierarchy/【删除选中对象的所有合批状态】取消hierarchy里选中物体的子级所有Batching Static")]
    static void ClearBatchingOnSelection()
    {
        var selected = Selection.gameObjects;
        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("没有选中任何 GameObject。");
            return;
        }

        foreach (var go in selected)
            ClearRecursive(go, StaticEditorFlags.BatchingStatic, "BatchingStatic", removeCache: true);
    }

    // ===================================================
    // 清除 Contribute GI（Lightmap Static）
    // ===================================================
    [MenuItem("GameObject/Static工具/取消所有子孙的 Contribute GI", false, 1)]
    static void ClearContributeOnContextMenu(MenuCommand cmd)
    {
        var go = cmd.context as GameObject;
        if (go == null) return;
        ClearRecursive(go, StaticEditorFlags.ContributeGI, "ContributeGI", removeCache: false);
    }

    [MenuItem("Tools/Hierarchy/【删除选中对象的所有待烘焙状态】取消hierarchy里选中物体的子级所有Contribute GI")]
    static void ClearContributeOnSelection()
    {
        var selected = Selection.gameObjects;
        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("没有选中任何 GameObject。");
            return;
        }

        foreach (var go in selected)
            ClearRecursive(go, StaticEditorFlags.ContributeGI, "ContributeGI", removeCache: false);
    }

    // ===================================================
    // 通用递归清除
    // ===================================================
    static void ClearRecursive(GameObject root, StaticEditorFlags mask, string label, bool removeCache)
    {
        var all = root.GetComponentsInChildren<Transform>(true);
        int affected = 0;

        foreach (var t in all)
        {
            var go = t.gameObject;
            var flags = GameObjectUtility.GetStaticEditorFlags(go);

            if ((flags & mask) == 0) continue;

            Undo.RecordObject(go, $"Clear {label}");
            GameObjectUtility.SetStaticEditorFlags(go, flags & ~mask);
            EditorUtility.SetDirty(go);
            affected++;
        }

        // 只有清除 BatchingStatic 时才顺手删除缓存组件
        if (removeCache)
        {
            var cache = root.GetComponent<RoomBatchingCache>();
            if (cache != null)
                Undo.DestroyObjectImmediate(cache);
        }

        Debug.Log($"[{root.name}] 已取消 {affected} 个子孙节点的 {label} 标记。");
    }
}
