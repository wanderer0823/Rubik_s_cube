using System.Collections.Generic;
using UnityEngine;
using static InitCubeSlot;

// 维护当前逻辑邻居房间ID（不实例化邻居房间）
// 仅实例化当前房间

public class RoomInstanceManager : MonoBehaviour
{
    // 当前逻辑邻居房间ID
    public List<int> _neighborRoomIds = new List<int>();

    // 当前房间实例
    private GameObject _currentRoomInstance;

    public GameObject CurrentRoom;

    void OnEnable()
    {
        RoomPreloadController.OnPreloadComplete += OnPreloadComplete;
    }

    void OnDisable()
    {
        RoomPreloadController.OnPreloadComplete -= OnPreloadComplete;
    }

    private void OnPreloadComplete(NeighborPreloadPayload payload)
    {
        if (payload == null) return;

        var vmm = ViewModeManager.Instance;
        var gs = GameState.Instance;

        if (vmm == null || vmm.cubeData == null || gs == null)
            return;

        InitCubeSlot cubeData = vmm.cubeData;

        // 更新逻辑邻居房间列表
        _neighborRoomIds.Clear();
        _neighborRoomIds.AddRange(payload.LogicalNeighborRoomIds);

        int currentRoomId = gs.CurrentRoomID;

        // 当前房间合法性检查
        if (currentRoomId < 0 || currentRoomId >= cubeData.rooms.Count)
            return;

        var room = cubeData.rooms[currentRoomId];

        if (room == null || room.RoomPerfab == null)
            return;

        // 销毁旧当前房间
        if (_currentRoomInstance != null)
        {
            Destroy(_currentRoomInstance);
        }

        // 仅实例化当前房间
        _currentRoomInstance = Instantiate(
            room.RoomPerfab,
            room.spawnPoint,
            Quaternion.Euler(room.orRotation)
        );

        _currentRoomInstance.transform.SetParent(CurrentRoom.transform, true);

        Debug.Log(
            $"RoomInstanceManager: 当前房间={currentRoomId} 邻居房间数={_neighborRoomIds.Count}"
        );
    }

    // 获取当前逻辑邻居房间ID列表
    public List<int> GetNeighborRoomIds()
    {
        return _neighborRoomIds;
    }
}