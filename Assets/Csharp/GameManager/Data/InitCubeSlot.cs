using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InitCubeSlot : MonoBehaviour
{
    public GameObject LogicCube;                //逻辑魔方
    public GameObject csP;                      //测试一键加载prefb

    //用于整体管理魔方结构
    public List<Slot> slots;                    //槽位，内含方块（CubePiece）类和面（CubeSurface_s）类
    public List<Room> rooms;                    //房间列表
    public GameObject CurrentRoom;              //房间刷新点（放预制体的）

    Dictionary<int, CubePiece> pieceMap;        //用方块id调用对应方块的字典，因为用slot来调用有点冗长
    Dictionary<int, CubeSurface_s> surfaceMap;  //用面id调用对应面的字典
    Dictionary<Vector3Int, CubeSurface_s> surfaceCoordMap;//用坐标调用对应面，和上面的区别只是调用媒介不一样，调用方法在下面
    Dictionary<Vector3Int, CubePiece> PieceCoordMap;//用坐标调用对应方块

    //静态数据
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
    //逻辑坐标范围还是-3，0，3！
    public enum Axis { X, Y, Z }//旋转轴标识

    //面朝向，用在面（CubeSurface_s）类
    public enum FaceDir
    {
        Up, Down,
        Left, Right,
        Front, Back
    }

    #region 槽位，方块，面相关定义
    //槽位（总入口）：引用方块（CubePiece）类
    [System.Serializable]
    public class Slot
    {
        [Header("Static")]          //静态数据（不变）
        public Vector3Int coord;        // 逻辑坐标，固定不变
        public Transform indexCube;     // 其世界坐标与coord绑定，旋转后occupant更新至其世界坐标

        [Header("Dynamic")]         //动态状态（变化）
        public CubePiece occupant;      // 当前占据者（变化）

        //初始化
        public Slot(Vector3Int coord, Transform indexCube, CubePiece occupant)
        {
            this.coord = coord;
            this.indexCube = indexCube;
            this.occupant = occupant;
        }

        //更新当前占据方块时调用(仅更新坐标)
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

    //方块（显示层）：被槽位（Slot）引用，引用了面（CubeSurface_s）类的List
    [System.Serializable]
    public class CubePiece
    {
        [Header("Static")]          //静态数据（不变）
        public int id;                              //方块id（固定）
        public Transform indexCube;                 //视觉实体（固定）
        public List<CubeSurface_s> surfaces;        //每个方块带的外表面（固定，但外表面属性可能有变化）

        [Header("Dynamic")]         //动态状态（变化）
        public Vector3Int coord;                 //方块当前的逻辑坐标（变化）

        //初始化
        public CubePiece() { }
    }

    //槽位的外表面：被方块（CubePiece）引用
    [System.Serializable]
    public class CubeSurface_s
    {
        [Header("Static")]          //静态数据（不变）
        public int id;                      //小面ID（固定）
        public int roomID;                  //此面对应的房间ID（固定）

        [Header("Dynamic")]         //动态状态（变化）
        public FaceDir dir;                 //外表面的方向，用于坐标计算（变化）
        public Vector3Int coord;            //外表面的逻辑坐标，旋转后变化

        //初始化
        public CubeSurface_s() { }

        //更新面坐标
        public void UpdatePosition(Vector3Int pieceCoord)
        {
            coord = pieceCoord + FaceOffset[dir];
        }
    }
    #endregion

    #region 房间相关定义
    [System.Serializable]
    //房间class 
    public class Room
    {
        [Header("Static")]          //静态数据（不变）
        public int roomID;                      //0到53
        public Vector3Int orRotation;           //初始旋转参数
        //public int RoomPerfabID;              //因为是预制体，所以是十多个，需要的时候再解锁吧
        public Vector3 spawnPoint;              //房间生成的坐标
        public GameObject RoomPerfab;           //房间预制体

        [Header("Dynamic")]         //动态状态（变化）
        public FaceState[] faces;
        public FaceDir[] dirMap;                //数字对应固定墙面，矢量要随旋转变化！

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
                dirMap[i] = (FaceDir)i; //初始化六个方向
            }
        }

        //根据方向获取该方向的门状态
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
    public class FaceState  //每个方向的数据
    {
        public bool HasDoor = true;   //房间的这一面是否有门
        public bool isPassable; //是否可通行
    }
    #endregion

    //初始化函数
    private void Awake()
    {
        InitSlots();                // 初始化slots列表
        BuildSurfaceMap();          //调用方法：CubeSurface_s s=SurfaceMap[id]
        BuildSurfaceCoordMap();     //调用方法：CubeSurface_s s=SurfaceCoordMap[position]
        BuildPieceMap();            //调用方法：CubePiece p = pieceMap[id];
        BuildPieceCoordMap();       //调用方法：CubeSurface_s s=SurfaceCoordMap[position]
        InitRooms();                // 初始化房间列表
    }

    private void Start()
    {
        GameEvents.calculateNeighbors();
    }

    #region 列表初始化
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
            ) * 2; //初始化逻辑坐标

            if (slot.indexCube == null)
                Debug.LogError($"Slot at {slot.coord} missing indexCube");

            //初始化方块和面的一些数据
            if (slot.occupant != null)
            {
                slot.occupant.coord = slot.coord;                                   //初始化槽位现在对应的方块的逻辑坐标
                slot.occupant.indexCube.position = slot.indexCube.position;         //初始化槽位现在对应的方块的世界坐标
                foreach (var element in slot.occupant.surfaces)
                {
                    element.id = i;         //初始化面id
                    element.roomID = i;     //初始化面对应的房间id
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
            //初始化每个房间的共性
            room.Init();

            //初始化房间的差异性
            room.roomID = i;
            i++;
            room.RoomPerfab = csP;
        }

        //改到start里
        //GameEvents.calculateNeighbors();
    }
    #endregion

    #region 字典初始化
    //用id调用SurfaceMap
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

    //用坐标调用SurfaceMap
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

    //用id调用pieceMap
    void BuildPieceMap()
    {
        pieceMap = new();
        foreach (var slot in slots)
        {
            if (slot.occupant == null) continue;
            pieceMap[slot.occupant.id] = slot.occupant;
        }
    }

    //用坐标调用PieceMap
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

    #region 张奕忻添加面访问接口
    public CubeSurface_s GetSurfaceByCoord(Vector3Int coord)
    {
        if (surfaceCoordMap == null)
            return null;

        if (surfaceCoordMap.TryGetValue(coord, out var surface))
            return surface;

        return null;
    }

    public CubeSurface_s GetSurfaceByRoomID(int roomID)
    {
        foreach (var slot in slots)
        {
            if (slot.occupant == null) continue;
            foreach (var surface in slot.occupant.surfaces)
            {
                if (surface.roomID == roomID)
                    return surface;
            }
        }

        return null;
    }

    /// <summary> 表面坐标每轴有效值（与 piece*2 + offset 一致） </summary>
    const int SurfaceCoordMax = 3;

    /// <summary> 是否在魔方表面坐标范围内（每轴取 -3,-1,1,3，即奇数且绝对值≤3） </summary>
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
            case FaceDir.Down: return 1;  // Y
            case FaceDir.Left:
            case FaceDir.Right: return 0; // X
            case FaceDir.Front:
            case FaceDir.Back: return 2;  // Z
        }
        return 1;
    }

    // 使用 GetBallFaceDirByPos 找周围面
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

    /// <summary> FaceDir 的反方向 </summary>
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

    #region 张天姿添加：获取指定轴、指定坐标值的所有方块（即某一层的9个方块）  
    public List<CubePiece> GetPiecesInLayer(Axis axis, int coordValue)
    {
        var result = new List<CubePiece>();
        foreach (var slot in slots)
        {
            if (slot.occupant == null) continue;
            // 修复问题A：应使用 piece.coord（随旋转变化），而非 slot.coord（固定不变）
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

    /// <summary>
    /// 修复问题B：拧动魔方后重建 surfaceCoordMap，使新坐标可以被正确查询到。
    /// 应在每次拧动完成后调用。
    /// </summary>
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

    #region 根据当前房间ID获取对应的方块 
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
