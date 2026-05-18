using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ReplaceAllRoomPrefabsFromFirst
{
    [MenuItem("Tools/Rooms/Replace All RoomPrefabs From First Non-Empty")]
    private static void ReplaceAll()
    {
        var cubeSlot = Object.FindObjectOfType<InitCubeSlot>();
        if (cubeSlot == null)
        {
            EditorUtility.DisplayDialog("Replace RoomPrefabs", "Current scene has no InitCubeSlot.", "OK");
            return;
        }

        if (cubeSlot.rooms == null || cubeSlot.rooms.Count == 0)
        {
            EditorUtility.DisplayDialog("Replace RoomPrefabs", "InitCubeSlot.rooms is empty.", "OK");
            return;
        }

        GameObject sourcePrefab = null;
        for (int i = 0; i < cubeSlot.rooms.Count; i++)
        {
            var room = cubeSlot.rooms[i];
            if (room != null && room.RoomPerfab != null)
            {
                sourcePrefab = room.RoomPerfab;
                break;
            }
        }

        if (sourcePrefab == null)
        {
            EditorUtility.DisplayDialog("Replace RoomPrefabs", "No non-empty RoomPerfab was found in InitCubeSlot.rooms.", "OK");
            return;
        }

        Undo.RecordObject(cubeSlot, "Replace All RoomPrefabs");

        int replacedCount = 0;
        for (int i = 0; i < cubeSlot.rooms.Count; i++)
        {
            var room = cubeSlot.rooms[i];
            if (room == null)
                continue;

            if (room.RoomPerfab != sourcePrefab)
            {
                room.RoomPerfab = sourcePrefab;
                replacedCount++;
            }
        }

        EditorUtility.SetDirty(cubeSlot);
        EditorSceneManager.MarkSceneDirty(cubeSlot.gameObject.scene);
        EditorUtility.DisplayDialog("Replace RoomPrefabs", $"Replaced {replacedCount} room prefab references with '{sourcePrefab.name}'.", "OK");
    }
}
