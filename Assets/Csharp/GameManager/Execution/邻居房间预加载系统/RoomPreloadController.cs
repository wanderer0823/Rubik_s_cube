using System;
using System.Collections.Generic;
using UnityEngine;
using static InitCubeSlot;

/// <summary>
/// 閭诲眳鎴块棿棰勫姞杞芥帶鍒跺櫒锛?
/// 缁存姢閫昏緫閭诲眳鍒楄〃锛屽苟璁＄畻褰撳墠鎴块棿鍜屽悓涓€澶ч潰鍓嶅悗宸﹀彸閭绘埧闂寸殑闂ㄦ槸鍚﹁兘褰㈡垚閫氶亾銆?
/// </summary>
public class RoomPreloadController : MonoBehaviour
{
    private readonly HashSet<int> _logicalNeighborRoomIds = new();
    private NeighborPreloadPayload _lastPayload;

    public static event Action<NeighborPreloadPayload> OnPreloadComplete;

    public IReadOnlyCollection<int> LogicalNeighborRoomIds => _logicalNeighborRoomIds;
    public NeighborPreloadPayload LastPayload => _lastPayload;

    private void OnEnable()
    {
        GameEvents.CalculateNeighbors += ExecutePreload;
    }

    private void OnDisable()
    {
        GameEvents.CalculateNeighbors -= ExecutePreload;
    }

    public void ExecutePreload()
    {
        var vmm = ViewModeManager.Instance;
        var gs = GameState.Instance;
        if (vmm == null || vmm.cubeRoot == null || vmm.cubeData == null || vmm.ball == null)
        {
            Debug.LogWarning("RoomPreloadController: missing cubeRoot/cubeData/ball, skip preload");
            return;
        }

        Transform cubeRoot = vmm.cubeRoot;
        InitCubeSlot cubeData = vmm.cubeData;
        Vector3 ballWorldPos = vmm.ball.position;

        CubeSurface_s currentSurface = null;

        // Temporarily resolve the active surface directly from CurrentRoomID.
        if (gs != null)
        {
            currentSurface = cubeData.GetSurfaceByRoomID(gs.CurrentRoomID);
        }

        // Previous logic kept here for rollback:
        // CubeSurface_s currentSurface = BallLocationService.CalculateSurface(cubeRoot, cubeData, ballWorldPos);
        // if (currentSurface == null && gs != null)
        // {
        //     currentSurface = gs.CurrentSurface ?? cubeData.GetSurfaceByRoomID(gs.CurrentRoomID);
        //     if (currentSurface != null)
        //     {
        //         Debug.LogWarning(
        //             $"RoomPreloadController: fallback to CurrentRoomID={gs.CurrentRoomID} because ball surface was not resolved yet");
        //     }
        // }

        if (currentSurface == null)
        {
            Debug.LogWarning("RoomPreloadController: failed to resolve current surface, skip preload");
            return;
        }

        List<Vector3Int> fiveCoords = new(5) { currentSurface.coord };
        fiveCoords.AddRange(InitCubeSlot.GetNeighborSurfaceCoords(currentSurface.coord));
        var fiveCoordSet = new HashSet<Vector3Int>(fiveCoords);

        var fiveSurfaces = new List<CubeSurface_s>(5);
        foreach (var coord in fiveCoords)
        {
            var surface = cubeData.GetSurfaceByCoord(coord);
            if (surface == null)
                continue;

            fiveSurfaces.Add(surface);
            var room = cubeData.rooms[surface.roomID];
            room?.ResetIsPassible();
        }

        foreach (var surface in fiveSurfaces)
        {
            var room = cubeData.rooms[surface.roomID];
            if (room == null)
                continue;

            for (int d = 0; d < 6; d++)
            {
                FaceDir dir = (FaceDir)d;
                if (!InitCubeSlot.TryGetSameFaceNeighborSurfaceCoord(surface.coord, dir, out var neighborCoord))
                    continue;

                if (!fiveCoordSet.Contains(neighborCoord))
                    continue;

                var neighborSurface = cubeData.GetSurfaceByCoord(neighborCoord);
                if (neighborSurface == null)
                    continue;

                var neighborRoom = cubeData.rooms[neighborSurface.roomID];
                if (neighborRoom == null)
                    continue;

                bool curHasDoor = room.GetFace(dir) != null && room.GetFace(dir).HasDoor;
                FaceDir oppositeDir = InitCubeSlot.OppositeFace(dir);
                bool neighborHasDoor = neighborRoom.GetFace(oppositeDir) != null
                    && neighborRoom.GetFace(oppositeDir).HasDoor;

                if (!curHasDoor || !neighborHasDoor)
                    continue;

                room.SetIsPassible(dir, true);
                neighborRoom.SetIsPassible(oppositeDir, true);
            }
        }

        _logicalNeighborRoomIds.Clear();
        var currentRoom = cubeData.rooms[currentSurface.roomID];
        if (currentRoom != null)
        {
            for (int d = 0; d < 6; d++)
            {
                FaceDir dir = (FaceDir)d;
                var face = currentRoom.GetFace(dir);
                if (face == null || !face.isPassable)
                    continue;

                if (!InitCubeSlot.TryGetSameFaceNeighborSurfaceCoord(currentSurface.coord, dir, out var neighborCoord))
                    continue;

                var neighborSurface = cubeData.GetSurfaceByCoord(neighborCoord);
                if (neighborSurface != null)
                    _logicalNeighborRoomIds.Add(neighborSurface.roomID);
            }
        }

        var payload = new NeighborPreloadPayload
        {
            LogicalNeighborRoomIds = new HashSet<int>(_logicalNeighborRoomIds)
        };

        var roomIdsToReport = new HashSet<int>(payload.LogicalNeighborRoomIds) { currentSurface.roomID };
        foreach (int roomId in roomIdsToReport)
        {
            if (roomId < 0 || roomId >= cubeData.rooms.Count)
                continue;

            var room = cubeData.rooms[roomId];
            if (room == null)
                continue;

            CubeSurface_s anySurfaceForRoom = null;
            foreach (var surface in fiveSurfaces)
            {
                if (surface.roomID == roomId)
                {
                    anySurfaceForRoom = surface;
                    break;
                }
            }

            if (anySurfaceForRoom == null)
                continue;

            FaceDir gravityFace = InitCubeSlot.OppositeFace(anySurfaceForRoom.dir);
            var loadData = new RoomLoadData { GravityFace = gravityFace };
            for (int d = 0; d < 6; d++)
            {
                FaceDir dir = (FaceDir)d;
                loadData.CanFormPassageByDoor[dir] =
                    room.GetFace(dir) != null && room.GetFace(dir).isPassable;
            }
            payload.RoomDataByRoomId[roomId] = loadData;
        }

        _lastPayload = payload;
        OnPreloadComplete?.Invoke(payload);
    }
}
