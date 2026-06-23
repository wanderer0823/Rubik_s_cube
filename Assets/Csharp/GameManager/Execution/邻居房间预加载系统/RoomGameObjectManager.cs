using System.Collections.Generic;
using UnityEngine;

public class RoomGameObjectManager : MonoBehaviour
{
    public static RoomGameObjectManager Instance { get; private set; }
    private static readonly int[] TaskDoorRoomIds = { 45 };
    
    [Header("始终激活的特殊物体（切换房间时不关闭）")]
    public List<GameObject> alwaysActiveObjects = new List<GameObject>();

    [System.Serializable]
    public class RoomGameObjectEntry
    {
        public int roomID;
        public GameObject roomObject;
    }

    private GameObject _currentRoomObject;

    public GameObject CurrentRoom;
    [SerializeField] private Vector3 worldLiftOffset = Vector3.zero;
    public Vector3 WorldLiftOffset => worldLiftOffset;
    public List<RoomGameObjectEntry> roomGameObjects = new List<RoomGameObjectEntry>();

    private readonly Dictionary<int, GameObject> _roomGameObjectMap = new Dictionary<int, GameObject>();
    private readonly HashSet<int> _positionAdjustedRoomIds = new HashSet<int>();
    private bool _isRoomGameObjectMapBuilt;
    private Vector3 _currentRoomBasePosition;
    private bool _hasCurrentRoomBasePosition;

    void Awake()
    {
        BuildRoomGameObjectMap();
        Instance = this;
    }

    void OnEnable()
    {
        GameEvents.OnRoomTransitionExecute += OnRoomTransition;
    }

    void Start()
    {
        ApplyCurrentRoomPositionToAll();
        ApplyCurrentRoomLift();
        //LoadCurrentRoomGameObject();
    }

    void OnDisable()
    {
        GameEvents.OnRoomTransitionExecute -= OnRoomTransition;
    }

    public void LoadCurrentRoomGameObject()
    {
        var gameState = GameState.Instance;
        if (gameState == null)
            return;

        LoadRoomGameObject(gameState, gameState.CurrentRoomID);
        TryCompleteDoorTask(gameState);
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
            /*__DEBUGTOOL_START__*/Debug.LogWarning($"RoomGameObjectManager: room object not found for room={currentRoomId}");/*__DEBUGTOOL_END__*/
            return;
        }

        SetAllRoomsInactive();

        //ApplyCurrentRoomPosition(roomObject, currentRoomId);
        roomObject.SetActive(true);
        _currentRoomObject = roomObject;
        TryCompleteDoorTask(gameState);
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

            // ★ 新增：如果该物体在“始终激活”列表中，跳过
            if (alwaysActiveObjects.Contains(entry.roomObject))
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

            ApplyCurrentRoomPosition(entry.roomObject, entry.roomID);
        }
    }

    private void ApplyCurrentRoomPosition(GameObject roomObject, int roomID)
    {
        if (roomObject == null || CurrentRoom == null)
            return;

        if (_positionAdjustedRoomIds.Contains(roomID))
            return;

        roomObject.transform.position += CurrentRoom.transform.position;
        _positionAdjustedRoomIds.Add(roomID);
    }

    public void ApplyCurrentRoomLift()
    {
        if (CurrentRoom == null)
            return;

        if (!_hasCurrentRoomBasePosition)
        {
            _currentRoomBasePosition = CurrentRoom.transform.position;
            _hasCurrentRoomBasePosition = true;
        }

        CurrentRoom.transform.position = _currentRoomBasePosition + worldLiftOffset;
    }

    private void OnRoomTransition(int roomID)
    {
        var gameState = GameState.Instance;
        if (gameState == null)
            return;

        LoadRoomGameObject(gameState, roomID);
    }

    private void TryCompleteDoorTask(GameState gameState)
    {
        if (gameState == null || gameState.CurrentRoomID != 45)
            return;

        if (TaskSystem.Instance == null)
            return;

        foreach (int roomId in TaskDoorRoomIds)
        {
            GameObject roomObject = GetRoomGameObject(roomId);
            if (roomObject == null)
                return;

            DoorController[] doors = roomObject.GetComponentsInChildren<DoorController>(true);
            bool hasOpenedDoor = false;
            foreach (DoorController door in doors)
            {
                if (door != null && door.IsOpened)
                {
                    hasOpenedDoor = true;
                    break;
                }
            }

            if (!hasOpenedDoor)
                return;
        }

        TaskSystem.Instance.CompleteTask(0);
    }
}
