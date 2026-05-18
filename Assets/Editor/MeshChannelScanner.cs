// MeshChannelScanner.cs  放 Editor 目录
using UnityEngine;
using UnityEditor;
using System.Text;

public static class MeshChannelScanner
{
    [MenuItem("Tools/诊断/扫描场景 Mesh 通道")]
    static void Scan()
    {
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
                problems++;
                continue;
            }

            bool hasUV2 = m.uv2 != null && m.uv2.Length > 0;
            bool hasNormals = m.normals != null && m.normals.Length > 0;
            bool isCombined = m.name.Contains("Combined") || m.name.StartsWith("Combined Mesh");
            bool isReadable = m.isReadable;

            // 命中烘焙报错的常见组合
            if (!hasUV2 || !hasNormals || isCombined || !isReadable)
            {
                sb.AppendLine(
                    $"[问题] {path}  Mesh={m.name}  " +
                    $"UV2={hasUV2}  Normals={hasNormals}  Combined={isCombined}  Readable={isReadable}");
                problems++;
                EditorGUIUtility.PingObject(mf.gameObject);
            }
        }

        Debug.Log($"扫描完成。问题数：{problems}\n{sb}");
    }

    static string GetPath(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }
}
