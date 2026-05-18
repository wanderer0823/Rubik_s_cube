// MeshChannelScanner.cs  放 Editor 目录
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

public static class MeshChannelScanner
{
    // 上次扫描出的问题物体，用于"清理 Static"按钮使用
    static List<GameObject> lastProblems = new List<GameObject>();

    [MenuItem("Tools/诊断/扫描场景 Mesh 通道")]
    static void Scan()
    {
        lastProblems.Clear();

        var filters = Object.FindObjectsOfType<MeshFilter>(true);
        int problems = 0;
        var sb = new StringBuilder();

        foreach (var mf in filters)
        {
            var m = mf.sharedMesh;
            string path = GetPath(mf.transform);

            if (m == null)
            {
                sb.AppendLine($"[空Mesh]  {path}");
                lastProblems.Add(mf.gameObject);
                problems++;
                continue;
            }

            bool hasUV2 = m.uv2 != null && m.uv2.Length > 0;
            bool hasNormals = m.normals != null && m.normals.Length > 0;
            bool isCombined = m.name.Contains("Combined") || m.name.StartsWith("Combined Mesh");
            bool isReadable = m.isReadable;

            if (!hasUV2 || !hasNormals || isCombined || !isReadable)
            {
                sb.AppendLine(
                    $"[问题] {path}  Mesh={m.name}  " +
                    $"UV2={hasUV2}  Normals={hasNormals}  Combined={isCombined}  Readable={isReadable}");
                lastProblems.Add(mf.gameObject);
                problems++;
                EditorGUIUtility.PingObject(mf.gameObject);
            }
        }

        Debug.Log($"扫描完成。问题数：{problems}\n已缓存到内存，可使用「取消问题物体的 Occluder/Occludee Static」按钮一键清理。\n{sb}");
    }

    [MenuItem("Tools/诊断/取消上次扫描问题物体的 Occluder+Occludee Static")]
    static void ClearOcclusionStaticOnLastProblems()
    {
        if (lastProblems == null || lastProblems.Count == 0)
        {
            Debug.LogWarning("没有缓存的问题物体。请先点「扫描场景 Mesh 通道」。");
            return;
        }

        const StaticEditorFlags MASK =
            StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic;

        int affected = 0;
        int alive = 0;

        foreach (var go in lastProblems)
        {
            if (go == null) continue;
            alive++;

            var flags = GameObjectUtility.GetStaticEditorFlags(go);
            if ((flags & MASK) == 0) continue;

            Undo.RecordObject(go, "Clear Occluder/Occludee Static on Problem");
            GameObjectUtility.SetStaticEditorFlags(go, flags & ~MASK);
            EditorUtility.SetDirty(go);
            affected++;
        }

        Debug.Log($"完成：缓存物体 {lastProblems.Count} 个（仍存在 {alive} 个），其中 {affected} 个被取消了 Occluder/Occludee Static。");
    }

    [MenuItem("Tools/诊断/选中上次扫描问题物体")]
    static void SelectLastProblems()
    {
        if (lastProblems == null || lastProblems.Count == 0)
        {
            Debug.LogWarning("没有缓存的问题物体。请先点「扫描场景 Mesh 通道」。");
            return;
        }

        var alive = new List<Object>();
        foreach (var go in lastProblems)
            if (go != null) alive.Add(go);

        Selection.objects = alive.ToArray();
        Debug.Log($"已选中 {alive.Count} 个问题物体。");
    }

    static string GetPath(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }
}
