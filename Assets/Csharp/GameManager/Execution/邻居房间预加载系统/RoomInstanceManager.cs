using System.Collections.Generic;
using UnityEngine;
using static InitCubeSlot;

// 订阅 OnNeighborPreloadExecute，维护当前应加载的房间集合。

public class RoomInstanceManager : MonoBehaviour
{
    private Dictionary<int, GameObject> _instantiatedRooms = new Dictionary<int, GameObject>();

    void OnEnable()
    {
        RoomPreloadController.OnPreloadComplete += OnPreloadComplete;
    }

    void OnDisable()
    {
        RoomPreloadController.OnPreloadComplete -= OnPreloadComplete;
    }

    // TODO: 这里是关键预加载函数
    private void OnPreloadComplete(NeighborPreloadPayload payload)
    {
        if (payload == null) return;

        var vmm = ViewModeManager.Instance;
        var gs = GameState.Instance;
        if (vmm == null || vmm.cubeData == null || gs == null) return;

        InitCubeSlot cubeData = vmm.cubeData;
        var roomsToKeep = new HashSet<int>(payload.LogicalNeighborRoomIds) { gs.CurrentRoomID };

        // 销毁：已实例化但不在本次“当前+逻辑邻居”集合中的房间
        var toRemove = new List<int>();
        foreach (var kv in _instantiatedRooms)
        {
            if (!roomsToKeep.Contains(kv.Key))
            {
                if (kv.Value != null) Destroy(kv.Value);
                toRemove.Add(kv.Key);
            }
        }
        foreach (var id in toRemove) _instantiatedRooms.Remove(id);

        // 实例化：本次集合中尚未实例化的房间（使用 payload 中的重力方向等子属性可在此或后续应用到实例上）
        foreach (int roomId in roomsToKeep)
        {
            if (_instantiatedRooms.ContainsKey(roomId)) continue;
            if (roomId < 0 || roomId >= cubeData.rooms.Count) continue;

            var room = cubeData.rooms[roomId];
            if (room == null || room.RoomPerfab == null) continue;

            Quaternion rotation = Quaternion.identity;
            if (payload.RoomDataByRoomId != null && payload.RoomDataByRoomId.TryGetValue(roomId, out var loadData))
            {
                rotation = FaceDirToRotation(loadData.GravityFace);
            }

            GameObject instance = Instantiate(room.RoomPerfab, room.spawnPoint, rotation);
            _instantiatedRooms[roomId] = instance;
        }

        Debug.Log($"RoomInstanceManager: 当前加载房间数={_instantiatedRooms.Count}（当前+逻辑邻居）");
    }

    private static Quaternion FaceDirToRotation(FaceDir gravityFace)
    {
        // 重力朝下时房间“地面”朝向；可根据实际坐标系调整
        switch (gravityFace)
        {
            case FaceDir.Down: return Quaternion.identity;
            case FaceDir.Up: return Quaternion.Euler(180f, 0f, 0f);
            case FaceDir.Back: return Quaternion.Euler(90f, 0f, 0f);
            case FaceDir.Front: return Quaternion.Euler(-90f, 0f, 0f);
            case FaceDir.Left: return Quaternion.Euler(0f, 90f, 0f);
            case FaceDir.Right: return Quaternion.Euler(0f, -90f, 0f);
        }
        return Quaternion.identity;
    }
}
