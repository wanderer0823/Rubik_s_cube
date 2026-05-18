using System.Collections.Generic;
using UnityEngine;
using static InitCubeSlot;

// 缁存姢褰撳墠閫昏緫閭诲眳鎴块棿ID锛堜笉瀹炰緥鍖栭偦灞呮埧闂达級
// 浠呭疄渚嬪寲褰撳墠鎴块棿

public class RoomInstanceManager : MonoBehaviour
{
    // 褰撳墠閫昏緫閭诲眳鎴块棿ID
    public List<int> _neighborRoomIds = new List<int>();

    // 褰撳墠鎴块棿瀹炰緥
    private GameObject _currentRoomInstance;

    public GameObject CurrentRoom;

    void OnEnable()
    {
        RoomPreloadController.OnPreloadComplete += OnPreloadComplete;
    }

    void Start()
    {
        EnsureCurrentRoomLoaded();
    }
    
    void OnDisable()
    {
        RoomPreloadController.OnPreloadComplete -= OnPreloadComplete;
    }

    private void EnsureCurrentRoomLoaded()
    {
        var preloadController = GameManager.Instance?.roomPreloadSystem;
        if (preloadController == null)
        {
            preloadController = FindObjectOfType<RoomPreloadController>();
        }

        if (preloadController == null)
            return;

        if (preloadController.LastPayload != null)
        {
            OnPreloadComplete(preloadController.LastPayload);
            return;
        }

        preloadController.ExecutePreload();
    }

    private void OnPreloadComplete(NeighborPreloadPayload payload)
    {
        if (payload == null) return;

        var vmm = ViewModeManager.Instance;
        var gs = GameState.Instance;

        if (vmm == null || vmm.cubeData == null || gs == null)
            return;

        InitCubeSlot cubeData = vmm.cubeData;

        // 鏇存柊閫昏緫閭诲眳鎴块棿鍒楄〃
        _neighborRoomIds.Clear();
        _neighborRoomIds.AddRange(payload.LogicalNeighborRoomIds);

        int currentRoomId = gs.CurrentRoomID;

        // 褰撳墠鎴块棿鍚堟硶鎬ф鏌?
        if (currentRoomId < 0 || currentRoomId >= cubeData.rooms.Count)
            return;

        var room = cubeData.rooms[currentRoomId];

        if (room == null || room.RoomPerfab == null)
            return;

        // 閿€姣佹棫褰撳墠鎴块棿
        if (_currentRoomInstance != null)
        {
            Destroy(_currentRoomInstance);
        }
        
        // 浠呭疄渚嬪寲褰撳墠鎴块棿
        _currentRoomInstance = Instantiate(room.RoomPerfab, CurrentRoom.transform, false);
        _currentRoomInstance.transform.localPosition = Vector3.zero;
        _currentRoomInstance.transform.localRotation = Quaternion.Euler(room.orRotation);

        #region 寮犲蹇讳慨鏀癸紒锛侊紒锛侀槻姝㈠厠闅嗛潤鎬佺墿浣撳け璐?
        // 鎭㈠鍘熷Mesh骞舵竻鎺夌儤鐒欏厜鐓у紩鐢紝璁╁厠闅嗕綋鍙互鑷敱鏃嬭浆
        var batchingCache = _currentRoomInstance.GetComponent<RoomBatchingCache>();
        if (batchingCache != null) batchingCache.RestoreForClone(clearLightmap: true);
        #endregion


    }

    // 鑾峰彇褰撳墠閫昏緫閭诲眳鎴块棿ID鍒楄〃
    public List<int> GetNeighborRoomIds()
    {
        return _neighborRoomIds;
    }
}
