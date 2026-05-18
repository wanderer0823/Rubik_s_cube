using System.Collections.Generic;
using UnityEngine;

public class RoomGameObjectManager : MonoBehaviour
{
    [System.Serializable]
    public class RoomGameObjectEntry
    {
        public int roomID;
        public GameObject roomObject;
    }

    private GameObject _currentRoomObject;

    public GameObject CurrentRoom;
    public List<RoomGameObjectEntry> roomGameObjects = new List<RoomGameObjectEntry>();

    private readonly Dictionary<int, GameObject> _roomGameObjectMap = new Dictionary<int, GameObject>();
    private readonly HashSet<int> _positionAdjustedRoomIds = new HashSet<int>();
    private bool _isRoomGameObjectMapBuilt;

    void Awake()
    {
        BuildRoomGameObjectMap();
    }

    void Start()
    {
        ApplyCurrentRoomPositionToAll();
    }

    public void LoadCurrentRoomGameObject()
    {
        var gameState = GameState.Instance;
        if (gameState == null)
            return;

        LoadRoomGameObject(gameState, gameState.CurrentRoomID);
    }

    public void LoadRoomGameObject(GameState gameState, int currentRoomId)
    {
        var vmm = ViewModeManager.Instance;
        if (gameState == null || vmm == null || vmm.cubeData == null)
            return;

        InitCubeSlot cubeData = vmm.cubeData;
        if (currentRoomId < 0 || currentRoomId >= cubeData.rooms.Count)
            return;

        var room = cubeData.rooms[currentRoomId];
        if (room == null)
            return;

        GameObject roomObject = GetRoomGameObject(currentRoomId);
        if (roomObject == null && room.RoomPerfab != null && room.RoomPerfab.scene.IsValid())
        {
            roomObject = room.RoomPerfab;
        }

        if (roomObject == null)
        {
            Debug.LogWarning($"RoomGameObjectManager: room object not found for room={currentRoomId}");
            return;
        }

        SetAllRoomsInactive();

        roomObject.SetActive(true);
        _currentRoomObject = roomObject;

    }

    private void BuildRoomGameObjectMap()
    {
        if (_isRoomGameObjectMapBuilt)
            return;

        _roomGameObjectMap.Clear();
        _positionAdjustedRoomIds.Clear();

        foreach (var entry in roomGameObjects)
        {
            if (entry == null || entry.roomObject == null)
                continue;

            _roomGameObjectMap[entry.roomID] = entry.roomObject;
        }

        _isRoomGameObjectMapBuilt = true;
    }

    public GameObject GetRoomGameObject(int roomID)
    {
        if (_roomGameObjectMap.TryGetValue(roomID, out var roomObject))
            return roomObject;

        return null;
    }

    private void SetAllRoomsInactive()
    {
        foreach (var entry in roomGameObjects)
        {
            if (entry == null || entry.roomObject == null)
                continue;

            entry.roomObject.SetActive(false);
        }

        _currentRoomObject = null;
    }

    private void ApplyCurrentRoomPositionToAll()
    {
        if (CurrentRoom == null)
            return;

        foreach (var entry in roomGameObjects)
        {
            if (entry == null || entry.roomObject == null)
                continue;

            if (_positionAdjustedRoomIds.Contains(entry.roomID))
                continue;

            entry.roomObject.transform.position += CurrentRoom.transform.position;
            _positionAdjustedRoomIds.Add(entry.roomID);
        }
    }
}
