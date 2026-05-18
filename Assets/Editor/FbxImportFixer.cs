using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class FbxImportFixer
{
    [MenuItem("Tools/诊断/批量修复 FBX 导入设置（Read/Write + Lightmap UV）")]
    static void FixAllFbx()
    {
        // 找到场景里所有 MeshFilter 引用的 Mesh，回溯到它们的 FBX 资产
        var filters = Object.FindObjectsOfType<MeshFilter>(true);
        var fbxPaths = new HashSet<string>();

        foreach (var mf in filters)
        {
            if (mf.sharedMesh == null) continue;
            string assetPath = AssetDatabase.GetAssetPath(mf.sharedMesh);
            if (string.IsNullOrEmpty(assetPath)) continue;
            // 只关心 FBX / 模型资产
            if (assetPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase) ||
                assetPath.EndsWith(".obj", System.StringComparison.OrdinalIgnoreCase) ||
                assetPath.EndsWith(".dae", System.StringComparison.OrdinalIgnoreCase))
            {
                fbxPaths.Add(assetPath);
            }
        }

        Debug.Log($"找到 {fbxPaths.Count} 个模型资产，开始修复...");

        int changed = 0;
        int total = fbxPaths.Count;
        int i = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (var path in fbxPaths)
            {
                i++;
                EditorUtility.DisplayProgressBar("修复 FBX 导入设置", path, (float)i / total);

                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;

                bool dirty = false;

                if (!importer.isReadable)
                {
                    importer.isReadable = true;
                    dirty = true;
                }

                if (!importer.generateSecondaryUV)
                {
                    importer.generateSecondaryUV = true;
                    dirty = true;
                }
                if (importer.importNormals == ModelImporterNormals.None)
                {
                    importer.importNormals = ModelImporterNormals.Calculate;
                    dirty = true;
                }

                if (dirty)
                {
                    importer.SaveAndReimport();
                    changed++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"修复完成。共扫描 {total} 个，修改并重导入 {changed} 个。");
    }
}
