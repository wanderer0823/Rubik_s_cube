using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InitCubeSlot : MonoBehaviour
{
    public GameObject LogicCube;
    public GameObject csP;//测试一键加载prefb

    public List<Slot> slots;
    public List<Room> rooms;
    public GameObject CurrentRoom;

    Dictionary<int, CubePiece> pieceMap;
    Dictionary<int, CubeSurface_s> surfaceMap;
    Dictionary<Vector3Int, CubeSurface_s> surfaceCoordMap;
    Dictionary<Vector3Int, CubePiece> PieceCoordMap;

    public static readonly Dictionary<FaceDir, Vector3Int> FaceOffset =
        new()
        {
            { FaceDir.Up,    new(0,  1,  0) },
            { FaceDir.Down,  new(0, -1,  0) },
            { FaceDir.Left,  new(-1, 0,  0) },
            { FaceDir.Right, new( 1, 0,  0) },
            { FaceDir.Front, new(0,  0,  1) },
            { FaceDir.Back,  new(0,  0, -1) }
        };

    public enum Axis { X, Y, Z }

    public enum FaceDir
    {
        Up, Down,
        Left, Right,
        Front, Back
    }

    #region Slot / Piece / Surface
    [System.Serializable]
    public class Slot
    {
        [Header("Static")]
        public Vector3Int coord;
        public Transform indexCube;

        [Header("Dynamic")]
        public CubePiece occupant;

        public Slot(Vector3Int coord, Transform indexCube, CubePiece occupant)
        {
            this.coord = coord;
            this.indexCube = indexCube;
            this.occupant = occupant;
        }

        public void SetOccupant(CubePiece piece)
        {
            occupant = piece;
            if (piece != null)
            {
                piece.coord = coord;
                piece.indexCube.position = indexCube.position;
            }
        }
    }

    [System.Serializable]
    public class CubePiece
    {
        [Header("Static")]
        public int id;
        public Transform indexCube;
        public List<CubeSurface_s> surfaces;

        [Header("Dynamic")]
        public Vector3Int coord;

        public CubePiece() { }
    }

    [System.Serializable]
    public class CubeSurface_s
    {
        [Header("Static")]
        public int id;
        public int roomID;

        [Header("Dynamic")]
        public FaceDir dir;
        public Vector3Int coord;

        public CubeSurface_s() { }

        public void UpdatePosition(Vector3Int pieceCoord)
        {
            coord = pieceCoord + FaceOffset[dir];
        }
    }
    #endregion

    #region Room
    [System.Serializable]
    public class Room
    {
        [Header("Static")]
        public int roomID;
        public Vector3 spawnPoint;
        public GameObject RoomPerfab;

        [Header("Dynamic")]
        public FaceState[] faces;
        public FaceDir[] dirMap;

        public void Init()
        {
            if (faces == null || faces.Length != 6)
                faces = new FaceState[6];

            for (int i = 0; i < faces.Length; i++)
            {
                faces[i] ??= new FaceState();
            }

            dirMap = new FaceDir[6];

            spawnPoint = new Vector3(0, 40, 0);
            for (int i = 0; i < dirMap.Length; i++)
            {
                dirMap[i] = (FaceDir)i;
            }
        }

        public FaceState GetFace(FaceDir dir)
        {
            FaceDir originalDir = dirMap[(int)dir];
            return faces[(int)originalDir];
        }

        public void SetIsPassible(FaceDir dir, bool value)
        {
            var fs = GetFace(dir);
            if (fs != null) fs.isPassable = value;
        }

        public void ResetIsPassible()
        {
            if (faces == null) return;
            foreach (var f in faces)
            {
                if (f != null) f.isPassable = false;
            }
        }
    }

    [System.Serializable]
    public class FaceState
    {
        public bool HasDoor = true;
        public bool isPassable;
    }
    #endregion

    private void Awake()
    {
        InitSlots();
        BuildSurfaceMap();
        BuildSurfaceCoordMap();
        BuildPieceMap();
        BuildPieceCoordMap();
        InitRooms();

    }

    private void Start()
    {
        GameEvents.calculateNeighbors();
    }

    #region Init
    private void InitSlots()
    {
        LogicCube.transform.position = Vector3.zero;
        int i = 0;
        foreach (var slot in slots)
        {
            Vector3 vec3 = slot.indexCube.position;
            slot.coord = new Vector3Int(
                Mathf.RoundToInt(vec3.x),
                Mathf.RoundToInt(vec3.y),
                Mathf.RoundToInt(vec3.z)
            ) * 2;

            if (slot.indexCube == null)
                Debug.LogError($"Slot at {slot.coord} missing indexCube");

            if (slot.occupant != null)
            {
                slot.occupant.coord = slot.coord;
                slot.occupant.indexCube.position = slot.indexCube.position;
                foreach (var element in slot.occupant.surfaces)
                {
                    element.id = i;
                    element.roomID = i;
                    i++;
                }
            }
        }
    }

    private void InitRooms()
    {
        int i = 0;
        foreach (var room in rooms)
        {
            room.Init();
            room.roomID = i;
            room.RoomPerfab = csP;
            i++;
        }
        
        //改到start里
        //GameEvents.calculateNeighbors();
    }
    #endregion

    #region Maps
    void BuildSurfaceMap()
    {
        surfaceMap = new();
        foreach (var slot in slots)
        {
            if (slot.occupant == null) continue;
            foreach (var s in slot.occupant.surfaces)
            {
                surfaceMap[s.id] = s;
            }
        }
    }

    void BuildSurfaceCoordMap()
    {
        surfaceCoordMap = new();
        foreach (var slot in slots)
        {
            if (slot.occupant == null) continue;
            foreach (var s in slot.occupant.surfaces)
            {
                s.UpdatePosition(slot.coord);
                surfaceCoordMap[s.coord] = s;
            }
        }
    }

    void BuildPieceMap()
    {
        pieceMap = new();
        foreach (var slot in slots)
        {
            if (slot.occupant == null) continue;
            pieceMap[slot.occupant.id] = slot.occupant;
        }
    }

    void BuildPieceCoordMap()
    {
        PieceCoordMap = new();
        foreach (var slot in slots)
        {
            if (slot.occupant == null) continue;
            PieceCoordMap[slot.occupant.coord] = slot.occupant;
        }
    }
    #endregion

    #region Surface helpers
    public CubeSurface_s GetSurfaceByCoord(Vector3Int coord)
    {
        if (surfaceCoordMap == null)
            return null;

        if (surfaceCoordMap.TryGetValue(coord, out var surface))
            return surface;

        return null;
    }

    const int SurfaceCoordMax = 3;

    // A valid surface coord has exactly one axis on the shell (+/-3)
    // and the other two axes on the face grid (-2/0/2).
    public static bool IsValidSurfaceCoord(Vector3Int c)
    {
        int shellAxisCount = 0;
        int[] axes = { c.x, c.y, c.z };

        foreach (int axis in axes)
        {
            int abs = Mathf.Abs(axis);
            if (abs == SurfaceCoordMax)
            {
                shellAxisCount++;
                continue;
            }

            if (abs == 0 || abs == 2)
                continue;

            return false;
        }

        return shellAxisCount == 1;
    }

    static int FaceDirToNormalAxis(FaceDir dir)
    {
        switch (dir)
        {
            case FaceDir.Up:
            case FaceDir.Down: return 1;
            case FaceDir.Left:
            case FaceDir.Right: return 0;
            case FaceDir.Front:
            case FaceDir.Back: return 2;
        }
        return 1;
    }

    public static bool TryGetSameFaceNeighborSurfaceCoord(
        Vector3Int surfaceCoord,
        FaceDir dir,
        out Vector3Int neighborCoord)
    {
        neighborCoord = surfaceCoord;

        FaceDir faceDir = BallLocationService.GetBallFaceDirByPos(
            new Vector3(surfaceCoord.x, surfaceCoord.y, surfaceCoord.z));
        int normalAxis = FaceDirToNormalAxis(faceDir);
        int dirAxis = FaceDirToNormalAxis(dir);

        if (dirAxis == normalAxis)
            return false;

        neighborCoord = surfaceCoord + FaceOffset[dir] * 2;
        return IsValidSurfaceCoord(neighborCoord);
    }

    public static List<Vector3Int> GetNeighborSurfaceCoords(Vector3Int surfaceCoord)
    {
        var list = new List<Vector3Int>(4);
        for (int d = 0; d < 6; d++)
        {
            FaceDir dir = (FaceDir)d;
            if (TryGetSameFaceNeighborSurfaceCoord(surfaceCoord, dir, out var neighbor))
                list.Add(neighbor);
        }
        return list;
    }

    public static FaceDir OppositeFace(FaceDir dir)
    {
        switch (dir)
        {
            case FaceDir.Up: return FaceDir.Down;
            case FaceDir.Down: return FaceDir.Up;
            case FaceDir.Left: return FaceDir.Right;
            case FaceDir.Right: return FaceDir.Left;
            case FaceDir.Front: return FaceDir.Back;
            case FaceDir.Back: return FaceDir.Front;
        }
        return dir;
    }
    #endregion

    #region Layer helpers
    public List<CubePiece> GetPiecesInLayer(Axis axis, int coordValue)
    {
        var result = new List<CubePiece>();
        foreach (var slot in slots)
        {
            if (slot.occupant == null) continue;

            int val = axis switch
            {
                Axis.X => slot.occupant.coord.x,
                Axis.Y => slot.occupant.coord.y,
                Axis.Z => slot.occupant.coord.z,
                _ => 0
            };

            if (val == coordValue)
                result.Add(slot.occupant);
        }
        return result;
    }

    public void RebuildSurfaceCoordMap()
    {
        surfaceCoordMap = new Dictionary<Vector3Int, CubeSurface_s>();
        foreach (var slot in slots)
        {
            if (slot.occupant == null) continue;
            foreach (var s in slot.occupant.surfaces)
            {
                surfaceCoordMap[s.coord] = s;
            }
        }
    }
    #endregion

    #region Room lookup
    public GameObject GetPieceGameObjectByRoomID(int roomID)
    {
        foreach (var slot in slots)
        {
            if (slot.occupant == null) continue;
            foreach (var surface in slot.occupant.surfaces)
            {
                if (surface.roomID == roomID)
                    return slot.occupant.indexCube.gameObject;
            }
        }
        return null;
    }
    #endregion
}
