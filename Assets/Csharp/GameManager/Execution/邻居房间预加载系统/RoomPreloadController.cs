using System;
using System;
using System.Collections.Generic;
using UnityEngine;
using static InitCubeSlot;

/// <summary>
/// 邻居房间预加载：维护逻辑邻居列表（可重新生成/销毁），计算当前房间与前后左右 4 邻房间内门的“能否形成通道”。
/// 接口：调用 ExecutePreload() 触发；订阅 OnPreloadComplete 获取结果。
/// </summary>
public class RoomPreloadController : MonoBehaviour
{
    // 当前逻辑邻居房间 ID 集合（每次执行时清空并重新生成）
    private HashSet<int> _logicalNeighborRoomIds = new HashSet<int>();
    // 最近一次预加载结果，供销毁/实例化使用
    private NeighborPreloadPayload _lastPayload;

    /// <summary> 预加载完成时触发，参数为 NeighborPreloadPayload </summary>
    public static event Action<NeighborPreloadPayload> OnPreloadComplete;

    /// <summary> 当前逻辑邻居房间 ID 集合（只读） </summary>
    public IReadOnlyCollection<int> LogicalNeighborRoomIds => _logicalNeighborRoomIds;

    /// <summary> 最近一次预加载结果 </summary>
    public NeighborPreloadPayload LastPayload => _lastPayload;

    #region 张奕忻：订阅广播事件，计算一次邻居房间所有信息
    private void OnEnable()
    {
        GameEvents.CalculateNeighbors += ExecutePreload;
    }

    private void OnDisable()
    {
        GameEvents.CalculateNeighbors += ExecutePreload;
    }
    #endregion

    /// <summary>
    /// 执行预加载：获取玩家空间位置 → 当前+前后左右 5 房间 → 计算门通道 → 更新逻辑邻居 → 触发 OnPreloadComplete。
    /// </summary>
    public void ExecutePreload()
    {
        var vmm = ViewModeManager.Instance;
        if (vmm == null || vmm.cubeRoot == null || vmm.cubeData == null || vmm.ball == null)
        {
            Debug.LogWarning("RoomPreloadController: 缺少 cubeRoot/cubeData/ball 引用，跳过预加载");
            return;
        }

        Transform cubeRoot = vmm.cubeRoot;
        InitCubeSlot cubeData = vmm.cubeData;
        Vector3 ballWorldPos = vmm.ball.position;

        CubeSurface_s currentSurface = BallLocationService.CalculateSurface(cubeRoot, cubeData, ballWorldPos);
        if (currentSurface == null)
        {
            Debug.LogWarning("RoomPreloadController: 无法解析玩家当前表面，跳过预加载");
            return;
        }

        // 1) 5 个表面坐标：自身 + 前后左右 4 个同面邻居
        List<Vector3Int> fiveCoords = new List<Vector3Int>(5) { currentSurface.coord };
        fiveCoords.AddRange(InitCubeSlot.GetNeighborSurfaceCoords(currentSurface.coord));
        var fiveCoordSet = new HashSet<Vector3Int>(fiveCoords);

        // 2) 取 5 个表面对应的房间，并重置 CanFormPassage
        var fiveSurfaces = new List<CubeSurface_s>(5);
        foreach (var coord in fiveCoords)
        {
            var s = cubeData.GetSurfaceByCoord(coord);
            if (s != null)
            {
                fiveSurfaces.Add(s);
                var room = rooms[s.roomID];
                if (room != null) room.ResetIsPassible();
            }
        }

        // 3) 对 5 个房间的每个门方向：若邻格在 5 内且双方都有门，则标记可形成通道
        foreach (var surface in fiveSurfaces)
        {
            var room = rooms[surface.roomID];
            if (room == null) continue;

            for (int d = 0; d < 6; d++)
            {
                FaceDir dir = (FaceDir)d;
                Vector3Int neighborCoord = surface.coord + FaceOffset[dir];
                if (!fiveCoordSet.Contains(neighborCoord)) continue;

                var neighborSurface = cubeData.GetSurfaceByCoord(neighborCoord);
                if (neighborSurface == null) continue;

                var neighborRoom = rooms[neighborSurface.roomID];
                if (neighborRoom == null) continue;

                bool curHasDoor = room.GetFace(dir) != null && room.GetFace(dir).HasDoor;
                bool neighborHasDoor = neighborRoom.GetFace(InitCubeSlot.OppositeFace(dir)) != null
                    && neighborRoom.GetFace(InitCubeSlot.OppositeFace(dir)).HasDoor;
                if (curHasDoor && neighborHasDoor)
                {
                    room.SetIsPassible(dir, true);
                    neighborRoom.SetIsPassible(InitCubeSlot.OppositeFace(dir), true);
                }
            }
        }

        // 4) 逻辑邻居 = 当前房间所有“可形成通道”方向对应的邻室
        _logicalNeighborRoomIds.Clear();
        var currentRoom = rooms[currentSurface.roomID];
        if (currentRoom != null)
        {
            for (int d = 0; d < 6; d++)
            {
                FaceDir dir = (FaceDir)d;
                var face = currentRoom.GetFace(dir);
                if (face == null || !face.isPassable) continue;
                Vector3Int neighborCoord = currentSurface.coord + FaceOffset[dir];
                var neighborSurface = cubeData.GetSurfaceByCoord(neighborCoord);
                if (neighborSurface != null)
                    _logicalNeighborRoomIds.Add(neighborSurface.roomID);
            }
        }

        // 5) 组装 payload：当前房间 + 逻辑邻居的重力方向与各门通道 bool
        var payload = new NeighborPreloadPayload
        {
            CurrentRoomID = currentSurface.roomID,
            LogicalNeighborRoomIds = new HashSet<int>(_logicalNeighborRoomIds)
        };

        var roomIdsToReport = new HashSet<int>(payload.LogicalNeighborRoomIds) { currentSurface.roomID };
        foreach (int roomId in roomIdsToReport)
        {
            if (roomId < 0 || roomId >= rooms.Count) continue;
            var r = rooms[roomId];
            if (r == null) continue;

            // 该房间对应的表面：从当前+四邻中任取一个属于该 roomID 的表面（用于重力方向）
            // 不确定需不需要反转oppsiteFace
            CubeSurface_s anySurfaceForRoom = null;
            foreach (var s in fiveSurfaces)
                if (s.roomID == roomId) { anySurfaceForRoom = s; break; }
            if (anySurfaceForRoom == null) continue;

            FaceDir gravityFace = InitCubeSlot.OppositeFace(anySurfaceForRoom.dir);
            var loadData = new RoomLoadData { GravityFace = gravityFace };
            for (int d = 0; d < 6; d++)
            {
                FaceDir dir = (FaceDir)d;
                loadData.CanFormPassageByDoor[dir] = r.GetFace(dir) != null && r.GetFace(dir).isPassable;
            }
            payload.RoomDataByRoomId[roomId] = loadData;
        }

        _lastPayload = payload;
        OnPreloadComplete?.Invoke(payload);
        Debug.Log($"RoomPreloadController: 预加载完成 CurrentRoom={payload.CurrentRoomID}, 逻辑邻居数={payload.LogicalNeighborRoomIds.Count}");
    }
}
