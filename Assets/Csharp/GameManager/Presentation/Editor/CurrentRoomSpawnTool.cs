using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[FilePath("ProjectSettings/CurrentRoomSpawnToolState.asset", FilePathAttribute.Location.ProjectFolder)]
public class CurrentRoomSpawnToolState : ScriptableSingleton<CurrentRoomSpawnToolState>
{
    public string roomId = "43";
    public List<PrefabSpawnEntry> entries = new();

    public void SaveState()
    {
        Save(true);
    }
}

[Serializable]
public class PrefabSpawnEntry
{
    public GameObject prefab;
    public int count = 1;
}

public class CurrentRoomSpawnTool : EditorWindow
{
    [SerializeField] private GameObject currentRoomOverride;

    private Vector2 scrollPosition;

    [MenuItem("Tools/Rooms/CurrentRoom Spawn Tool")]
    private static void Open()
    {
        GetWindow<CurrentRoomSpawnTool>("Room Spawn");
    }

    private void OnGUI()
    {
        CurrentRoomSpawnToolState state = CurrentRoomSpawnToolState.instance;

        EditorGUILayout.LabelField("CurrentRoom 房间生成工具", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        DrawCurrentRoomSection();
        EditorGUILayout.Space(6);
        DrawRoomSection(state);
        EditorGUILayout.Space(6);
        DrawPrefabSection(state);
        EditorGUILayout.Space(10);

        using (new EditorGUI.DisabledScope(!CanGenerate(state)))
        {
            if (GUILayout.Button("生成到 CurrentRoom", GUILayout.Height(32)))
            {
                Generate(state);
            }
        }
    }

    private void DrawCurrentRoomSection()
    {
        EditorGUILayout.LabelField("CurrentRoom", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        currentRoomOverride = (GameObject)EditorGUILayout.ObjectField(
            "手动指定",
            currentRoomOverride,
            typeof(GameObject),
            true
        );
        if (EditorGUI.EndChangeCheck())
        {
            Repaint();
        }

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("当前使用", ResolveCurrentRoom(), typeof(GameObject), true);
        }

        EditorGUILayout.HelpBox(
            "优先使用手动指定对象；未指定时自动读取 InitCubeSlot.CurrentRoom，最后回退到场景里名为 CurrentRoom 的对象。",
            MessageType.Info
        );
    }

    private void DrawRoomSection(CurrentRoomSpawnToolState state)
    {
        EditorGUILayout.LabelField("房间信息", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        state.roomId = EditorGUILayout.TextField("Room ID", state.roomId ?? string.Empty);
        if (EditorGUI.EndChangeCheck())
        {
            state.SaveState();
        }
    }

    private void DrawPrefabSection(CurrentRoomSpawnToolState state)
    {
        EditorGUILayout.LabelField("预制体列表", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("添加一行"))
            {
                state.entries.Add(new PrefabSpawnEntry());
                state.SaveState();
            }

            if (GUILayout.Button("添加当前选中的预制体"))
            {
                AddSelectedPrefabs(state);
            }

            if (GUILayout.Button("清空列表"))
            {
                if (EditorUtility.DisplayDialog("清空预制体列表", "确定清空当前预制体列表吗？", "确定", "取消"))
                {
                    state.entries.Clear();
                    state.SaveState();
                }
            }
        }

        if (state.entries.Count == 0)
        {
            EditorGUILayout.HelpBox("先把要生成的预制体加进列表，再设置每个预制体的数量。", MessageType.None);
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MinHeight(180));

        for (int i = 0; i < state.entries.Count; i++)
        {
            PrefabSpawnEntry entry = state.entries[i];
            if (entry == null)
            {
                entry = new PrefabSpawnEntry();
                state.entries[i] = entry;
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUI.BeginChangeCheck();
                entry.prefab = (GameObject)EditorGUILayout.ObjectField(
                    $"Prefab {i + 1}",
                    entry.prefab,
                    typeof(GameObject),
                    false
                );
                entry.count = Mathf.Max(0, EditorGUILayout.IntField("数量", entry.count));

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("删除", GUILayout.Width(70)))
                    {
                        state.entries.RemoveAt(i);
                        state.SaveState();
                        GUIUtility.ExitGUI();
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    state.SaveState();
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void AddSelectedPrefabs(CurrentRoomSpawnToolState state)
    {
        UnityEngine.Object[] selectedObjects = Selection.objects;
        int addedCount = 0;

        foreach (UnityEngine.Object selectedObject in selectedObjects)
        {
            if (selectedObject is not GameObject prefab)
                continue;

            if (!PrefabUtility.IsPartOfPrefabAsset(prefab))
                continue;

            if (ContainsPrefab(state.entries, prefab))
                continue;

            state.entries.Add(new PrefabSpawnEntry
            {
                prefab = prefab,
                count = 1
            });
            addedCount++;
        }

        if (addedCount > 0)
        {
            state.SaveState();
        }
        else
        {
            EditorUtility.DisplayDialog("添加预制体", "当前选择里没有可添加的新预制体资源。", "确定");
        }
    }

    private bool ContainsPrefab(List<PrefabSpawnEntry> entries, GameObject prefab)
    {
        foreach (PrefabSpawnEntry entry in entries)
        {
            if (entry != null && entry.prefab == prefab)
                return true;
        }

        return false;
    }

    private bool CanGenerate(CurrentRoomSpawnToolState state)
    {
        if (ResolveCurrentRoom() == null)
            return false;

        if (string.IsNullOrWhiteSpace(state.roomId))
            return false;

        foreach (PrefabSpawnEntry entry in state.entries)
        {
            if (entry != null && entry.prefab != null && entry.count > 0)
                return true;
        }

        return false;
    }

    private GameObject ResolveCurrentRoom()
    {
        if (currentRoomOverride != null)
            return currentRoomOverride;

        InitCubeSlot cubeSlot = FindObjectOfType<InitCubeSlot>();
        if (cubeSlot != null && cubeSlot.CurrentRoom != null)
            return cubeSlot.CurrentRoom;

        return GameObject.Find("CurrentRoom");
    }

    private void Generate(CurrentRoomSpawnToolState state)
    {
        GameObject currentRoom = ResolveCurrentRoom();
        if (currentRoom == null)
        {
            EditorUtility.DisplayDialog("生成失败", "没有找到 CurrentRoom。请先手动指定，或确保场景里存在 InitCubeSlot.CurrentRoom。", "确定");
            return;
        }

        string roomId = (state.roomId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(roomId))
        {
            EditorUtility.DisplayDialog("生成失败", "Room ID 不能为空。", "确定");
            return;
        }

        bool hasValidEntry = false;
        foreach (PrefabSpawnEntry entry in state.entries)
        {
            if (entry != null && entry.prefab != null && entry.count > 0)
            {
                hasValidEntry = true;
                break;
            }
        }

        if (!hasValidEntry)
        {
            EditorUtility.DisplayDialog("生成失败", "至少要有一个数量大于 0 的预制体。", "确定");
            return;
        }

        Transform roomRoot = GetTargetRoomRoot(currentRoom.transform, roomId);
        if (roomRoot == null)
            return;

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Generate CurrentRoom Prefabs");

        int createdCount = 0;
        foreach (PrefabSpawnEntry entry in state.entries)
        {
            if (entry == null || entry.prefab == null || entry.count <= 0)
                continue;

            for (int i = 0; i < entry.count; i++)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab);
                if (instance == null)
                    continue;

                instance.transform.SetParent(roomRoot, false);
                Undo.RegisterCreatedObjectUndo(instance, "Create Prefab Instance");
                createdCount++;
            }
        }

        EditorUtility.SetDirty(currentRoom);
        EditorUtility.SetDirty(roomRoot.gameObject);
        UpdateRoomGameObjectManager(roomId, roomRoot.gameObject);
        EditorSceneManager.MarkSceneDirty(currentRoom.scene);
        Undo.CollapseUndoOperations(undoGroup);

        Selection.activeGameObject = roomRoot.gameObject;
        EditorGUIUtility.PingObject(roomRoot.gameObject);

        EditorUtility.DisplayDialog("生成完成", $"已在 {currentRoom.name}/{roomRoot.name} 下生成 {createdCount} 个物体。", "确定");
    }

    private void UpdateRoomGameObjectManager(string roomIdText, GameObject roomObject)
    {
        if (!int.TryParse(roomIdText, out int roomId))
            return;

        RoomGameObjectManager manager = FindObjectOfType<RoomGameObjectManager>();
        if (manager == null || roomObject == null)
            return;

        Undo.RecordObject(manager, "Update RoomGameObjectManager");

        RoomGameObjectManager.RoomGameObjectEntry targetEntry = null;
        foreach (RoomGameObjectManager.RoomGameObjectEntry entry in manager.roomGameObjects)
        {
            if (entry != null && entry.roomID == roomId)
            {
                targetEntry = entry;
                break;
            }
        }

        if (targetEntry == null)
        {
            targetEntry = new RoomGameObjectManager.RoomGameObjectEntry
            {
                roomID = roomId,
                roomObject = roomObject
            };
            manager.roomGameObjects.Add(targetEntry);
        }
        else
        {
            targetEntry.roomObject = roomObject;
        }

        EditorUtility.SetDirty(manager);
    }

    private Transform GetTargetRoomRoot(Transform currentRoom, string roomId)
    {
        Transform existing = currentRoom.Find(roomId);
        if (existing == null)
        {
            return CreateRoomRoot(currentRoom, roomId);
        }

        int option = EditorUtility.DisplayDialogComplex(
            "Room 节点已存在",
            $"CurrentRoom 下已经存在名为 {roomId} 的子物体。",
            "使用现有节点",
            "取消",
            "新建重名节点"
        );

        if (option == 0)
            return existing;

        if (option == 2)
            return CreateRoomRoot(currentRoom, roomId);

        return null;
    }

    private Transform CreateRoomRoot(Transform parent, string roomId)
    {
        GameObject roomRoot = new GameObject(roomId);
        Undo.RegisterCreatedObjectUndo(roomRoot, "Create Room Root");
        roomRoot.transform.SetParent(parent, false);
        roomRoot.transform.localPosition = Vector3.zero;
        roomRoot.transform.localRotation = Quaternion.identity;
        roomRoot.transform.localScale = Vector3.one;
        return roomRoot.transform;
    }
}
