using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class PlateDoorBindingWindow : EditorWindow
{
    private const string DoorPrefabFolder = "Assets/Source/关卡";

    private readonly List<PlateEntry> plateEntries = new List<PlateEntry>();
    private readonly List<DoorEntry> doorEntries = new List<DoorEntry>();
    private Vector2 scrollPosition;

    [MenuItem("Tools/Rooms/Plate-Door 绑定面板")]
    private static void Open()
    {
        GetWindow<PlateDoorBindingWindow>("Plate-Door 绑定");
    }

    private void OnEnable()
    {
        ScanAll();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Plate-Door 绑定面板", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("刷新", GUILayout.Width(90)))
                ScanAll();

            if (GUILayout.Button("自动生成缺失 DoorId", GUILayout.Width(160)))
                GenerateMissingDoorIds();

            if (GUILayout.Button("保存当前场景", GUILayout.Width(120)))
                EditorSceneManager.SaveOpenScenes();
        }

        EditorGUILayout.HelpBox(
            "策划只需要在“绑定Door”里选择目标门。面板会把选择转换成 linkedDoorId 保存到 Plate，运行时再用 ID 找动态生成的门。",
            MessageType.Info
        );

        EditorGUILayout.Space(8);
        DrawSummary();
        EditorGUILayout.Space(6);
        DrawPlateTable();
    }

    private void DrawSummary()
    {
        EditorGUILayout.LabelField($"场景 Plate 数量：{plateEntries.Count}    关卡 Door 数量：{doorEntries.Count}");
    }

    private void DrawPlateTable()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("房间名", GUILayout.Width(160));
            GUILayout.Label("Plate名", GUILayout.Width(180));
            GUILayout.Label("linkedDoorId", GUILayout.Width(220));
            GUILayout.Label("绑定Door");
        }

        string[] doorOptions = BuildDoorOptions();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        foreach (PlateEntry plateEntry in plateEntries)
        {
            if (plateEntry.Plate == null)
                continue;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(plateEntry.RoomName, GUILayout.Width(160));

                if (GUILayout.Button(plateEntry.Plate.name, EditorStyles.linkLabel, GUILayout.Width(180)))
                {
                    Selection.activeObject = plateEntry.Plate.gameObject;
                    EditorGUIUtility.PingObject(plateEntry.Plate.gameObject);
                }

                EditorGUILayout.LabelField(plateEntry.Plate.linkedDoorId, GUILayout.Width(220));

                int currentIndex = FindDoorOptionIndex(plateEntry.Plate.linkedDoorId);
                EditorGUI.BeginChangeCheck();
                int selectedIndex = EditorGUILayout.Popup(currentIndex, doorOptions);
                if (EditorGUI.EndChangeCheck())
                    ApplyDoorSelection(plateEntry.Plate, selectedIndex);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private string[] BuildDoorOptions()
    {
        string[] options = new string[doorEntries.Count + 1];
        options[0] = "未绑定";

        for (int i = 0; i < doorEntries.Count; i++)
            options[i + 1] = doorEntries[i].DisplayName;

        return options;
    }

    private int FindDoorOptionIndex(string linkedDoorId)
    {
        if (string.IsNullOrEmpty(linkedDoorId))
            return 0;

        for (int i = 0; i < doorEntries.Count; i++)
        {
            if (doorEntries[i].DoorId == linkedDoorId)
                return i + 1;
        }

        return 0;
    }

    private void ApplyDoorSelection(Plate plate, int selectedIndex)
    {
        Undo.RecordObject(plate, "Bind Plate Door");

        if (selectedIndex <= 0)
        {
            plate.linkedDoorId = string.Empty;
            plate.linkedDoor = null;
        }
        else
        {
            DoorEntry doorEntry = doorEntries[selectedIndex - 1];
            string doorId = EnsureDoorId(doorEntry);
            plate.linkedDoorId = doorId;
            plate.linkedDoor = null;
        }

        EditorUtility.SetDirty(plate);
        EditorSceneManager.MarkSceneDirty(plate.gameObject.scene);
        ScanAll();
    }

    private void GenerateMissingDoorIds()
    {
        int generatedCount = 0;

        foreach (DoorEntry doorEntry in doorEntries.ToArray())
        {
            if (!string.IsNullOrEmpty(doorEntry.DoorId))
                continue;

            EnsureDoorId(doorEntry);
            generatedCount++;
        }

        ScanAll();
        Debug.Log($"Plate-Door 绑定面板：已生成 {generatedCount} 个 DoorId");
    }

    private string EnsureDoorId(DoorEntry doorEntry)
    {
        if (!string.IsNullOrEmpty(doorEntry.DoorId))
            return doorEntry.DoorId;

        string generatedDoorId = GenerateDoorId(doorEntry);
        SetDoorIdInPrefab(doorEntry, generatedDoorId);
        doorEntry.DoorId = generatedDoorId;
        return generatedDoorId;
    }

    private string GenerateDoorId(DoorEntry doorEntry)
    {
        string prefabName = Path.GetFileNameWithoutExtension(doorEntry.PrefabPath);
        string rawId = prefabName + "_" + doorEntry.TransformPath;
        string baseDoorId = SanitizeId(rawId);
        string doorId = baseDoorId;
        int suffix = 2;

        while (IsDoorIdUsedByOtherEntry(doorId, doorEntry))
        {
            doorId = baseDoorId + "_" + suffix;
            suffix++;
        }

        return doorId;
    }

    private bool IsDoorIdUsedByOtherEntry(string doorId, DoorEntry currentEntry)
    {
        foreach (DoorEntry doorEntry in doorEntries)
        {
            if (doorEntry == currentEntry)
                continue;

            if (doorEntry.DoorId == doorId)
                return true;
        }

        return false;
    }

    private string SanitizeId(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "door";

        char[] chars = value.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            char c = chars[i];
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
                continue;

            chars[i] = '_';
        }

        return new string(chars);
    }

    private void SetDoorIdInPrefab(DoorEntry doorEntry, string doorId)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(doorEntry.PrefabPath);
        try
        {
            Transform doorTransform = FindTransformByPath(prefabRoot.transform, doorEntry.TransformPath);
            if (doorTransform == null)
            {
                Debug.LogWarning($"未能在预制体中找到门：{doorEntry.PrefabPath} / {doorEntry.TransformPath}");
                return;
            }

            DoorBindingTarget bindingTarget = doorTransform.GetComponent<DoorBindingTarget>();
            if (bindingTarget == null)
                bindingTarget = doorTransform.gameObject.AddComponent<DoorBindingTarget>();

            bindingTarget.doorId = doorId;
            EditorUtility.SetDirty(bindingTarget);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, doorEntry.PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private void ScanAll()
    {
        ScanPlates();
        ScanDoors();
        Repaint();
    }

    private void ScanPlates()
    {
        plateEntries.Clear();
        Plate[] plates = Resources.FindObjectsOfTypeAll<Plate>();

        foreach (Plate plate in plates)
        {
            if (plate == null || EditorUtility.IsPersistent(plate))
                continue;

            if (!plate.gameObject.scene.IsValid())
                continue;

            plateEntries.Add(new PlateEntry
            {
                Plate = plate,
                RoomName = ResolveRoomName(plate.transform)
            });
        }

        plateEntries.Sort((a, b) =>
        {
            int roomCompare = string.Compare(a.RoomName, b.RoomName, System.StringComparison.Ordinal);
            if (roomCompare != 0)
                return roomCompare;

            return string.Compare(a.Plate.name, b.Plate.name, System.StringComparison.Ordinal);
        });
    }

    private void ScanDoors()
    {
        doorEntries.Clear();

        if (!AssetDatabase.IsValidFolder(DoorPrefabFolder))
            return;

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { DoorPrefabFolder });
        foreach (string guid in guids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                DoorController[] doors = prefabRoot.GetComponentsInChildren<DoorController>(true);
                foreach (DoorController door in doors)
                {
                    DoorBindingTarget bindingTarget = door.GetComponent<DoorBindingTarget>();
                    doorEntries.Add(new DoorEntry
                    {
                        PrefabPath = prefabPath,
                        PrefabName = Path.GetFileNameWithoutExtension(prefabPath),
                        DoorName = door.name,
                        TransformPath = GetTransformPath(prefabRoot.transform, door.transform),
                        DoorId = bindingTarget != null ? bindingTarget.doorId : string.Empty
                    });
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        doorEntries.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, System.StringComparison.Ordinal));
    }

    private string ResolveRoomName(Transform plateTransform)
    {
        Transform roomTransform = plateTransform.parent != null ? plateTransform.parent.parent : null;
        return roomTransform != null ? roomTransform.name : "未找到父级房间";
    }

    private string GetTransformPath(Transform root, Transform target)
    {
        if (target == root)
            return root.name;

        List<string> names = new List<string>();
        Transform current = target;
        while (current != null)
        {
            names.Add(current.name);
            if (current == root)
                break;

            current = current.parent;
        }

        names.Reverse();
        return string.Join("/", names.ToArray());
    }

    private Transform FindTransformByPath(Transform root, string path)
    {
        string[] names = path.Split('/');
        if (names.Length == 0 || names[0] != root.name)
            return null;

        Transform current = root;
        for (int i = 1; i < names.Length; i++)
        {
            current = FindDirectChild(current, names[i]);
            if (current == null)
                return null;
        }

        return current;
    }

    private Transform FindDirectChild(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;
        }

        return null;
    }

    private class PlateEntry
    {
        public Plate Plate;
        public string RoomName;
    }

    private class DoorEntry
    {
        public string PrefabPath;
        public string PrefabName;
        public string DoorName;
        public string TransformPath;
        public string DoorId;

        public string DisplayName => string.IsNullOrEmpty(DoorId)
            ? $"{PrefabName} / {DoorName}  (未生成ID)"
            : $"{PrefabName} / {DoorName}";
    }
}
