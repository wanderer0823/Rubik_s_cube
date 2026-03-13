using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InitCubeSlot : MonoBehaviour
{
    public GameObject LogicCube;                //逻辑魔方

    //用于整体管理魔方结构
    public List<Slot> slots;                    //槽位，内含方块（CubePiece）类和面（CubeSurface_s）类
    public List<Room> rooms;                    //房间列表
    public int CurrentRoomID;                   //当前房间ID
    public GameObject CurrentRoom;              //房间刷新点（放预制体的）

    Dictionary<int, CubePiece> pieceMap;        //用方块id调用对应方块的字典，因为用slot来调用有点冗长
    Dictionary<int, CubeSurface_s> surfaceMap;  //用面id调用对应面的字典
    Dictionary<Vector3Int, CubeSurface_s> surfaceCoordMap;//用坐标调用对应面，和上面的区别只是调用媒介不一样，调用方法在下面
    Dictionary<Vector3Int, CubePiece> PieceCoordMap;//用坐标调用对应方块

    //静态数据
    public static readonly Dictionary<FaceDir, Vector3Int> FaceOffset =
new()
{
    {FaceDir.Up,    new(0,  1,  0)},
    {FaceDir.Down,  new(0, -1,  0)},
    {FaceDir.Left,  new(-1, 0,  0)},
    {FaceDir.Right, new( 1, 0,  0)},
    {FaceDir.Front, new(0,  0,  1)},
    {FaceDir.Back,  new(0,  0, -1)}
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
        [Header("静态数据（不变）")]
        public Vector3Int coord;        // 逻辑坐标，固定不变
        public Transform indexCube;     // 其世界坐标与coord绑定，旋转后occupant更新至其世界坐标

        [Header("动态状态（变化）")]
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
        [Header("静态数据（不变）")]
        public int id;                              //方块id（固定）
        public Transform indexCube;                 //视觉实体（固定）
        public List<CubeSurface_s> surfaces;        //每个方块带的外表面（固定，但外表面属性可能有变化）

        [Header("动态状态（变化）")]
        public Vector3Int coord;                 //方块当前的逻辑坐标（变化）

        //初始化
        public CubePiece() { }
    }

    //槽位的外表面：被方块（CubePiece）引用
    [System.Serializable]
    public class CubeSurface_s
    {
        [Header("静态数据（不变）")]
        public int id;                      //小面ID（固定）
        public int roomID;                  //此面对应的房间ID（固定）

        [Header("动态状态（变化）")]
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
        [Header("静态数据（不变）")]
        public int roomID;                      //0到53
        //public int RoomPerfabID;              //因为是预制体，所以是十多个，需要的时候再解锁吧
        public Vector3 spawnPoint;              //房间生成的坐标
        public GameObject RoomPerfab;           //房间预制体

        [Header("动态状态（变化）")]
        public FaceState[] faces; 
        public FaceDir[] dirMap;                //数字对应固定墙面，矢量要随旋转变化！

        
        public void Init()
        {
            faces = new FaceState[6];
            dirMap = new FaceDir[6];

            spawnPoint = new Vector3(0, 40, 0);
            for (int i=0;i<dirMap.Length;i++)
            {
                dirMap[i] = (FaceDir)i;//初始化六个方向
                GetFace(dirMap[i]);
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
        public bool HasDoor;   //房间的这一面是否有门
        public bool isPassable; //是否可通行
    }

    #endregion

    //初始化函数
    private void Awake()
    {
        CurrentRoomID = 0;
        InitSlots();                // 初始化slots列表
        InitRooms();                // 初始化房间列表

        //初始化字典，方便以后单独引用调用面和方块,看调用方法即可
        BuildSurfaceMap();          //调用方法：CubeSurface_s s=SurfaceMap[id]
        BuildSurfaceCoordMap();     //调用方法：CubeSurface_s s=SurfaceCoordMap[position]
        BuildPieceMap();            //调用方法：CubePiece p = pieceMap[id];
        BuildPieceCoordMap();       //调用方法：CubeSurface_s s=SurfaceCoordMap[position]
    }


    #region 列表初始化
    private void InitSlots()
    {
        LogicCube.transform.position = new Vector3(0, 0, 0);
        int i = 0;
        foreach (var slot in slots)
        {
            Vector3 vec3 = slot.indexCube.position;
            slot.coord= new Vector3Int(
            Mathf.RoundToInt(vec3.x),
            Mathf.RoundToInt(vec3.y),
            Mathf.RoundToInt(vec3.z)
        )*2; //初始化逻辑坐标

            if (slot.indexCube == null)
                Debug.LogError($"Slot at {slot.coord} 缺少 indexCube");

            //初始化方块和面的一些数据
            if (slot.occupant != null)
            {
                slot.occupant.coord = slot.coord;                            //初始化槽位现在对应的方块的逻辑坐标
                slot.occupant.indexCube.position = slot.indexCube.position;  //初始化槽位现在对应的方块的世界坐标
                foreach(var element in slot.occupant.surfaces)
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
        foreach(var room in rooms)
        {
            //初始化每个房间的共性
            room.Init();

            //初始化房间的差异性
            room.roomID = i;
            i++;
        }
        //初始：加载房间0
        CurrentRoom = rooms[0].RoomPerfab;
        Instantiate(CurrentRoom, rooms[0].spawnPoint, Quaternion.identity);
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

    /// <summary> 表面坐标每轴有效值（与 piece*2 + offset 一致） </summary>
    const int SurfaceCoordMin = -3, SurfaceCoordMax = 3;

    /// <summary> 是否在魔方表面坐标范围内（每轴取 -3,-1,1,3，即奇数且绝对值≤3） </summary>
    public static bool IsValidSurfaceCoord(Vector3Int c)
    {
        return IsValidSurfaceAxis(c.x) && IsValidSurfaceAxis(c.y) && IsValidSurfaceAxis(c.z);
    }
    static bool IsValidSurfaceAxis(int a)
    {
        int abs = Mathf.Abs(a);
        return abs <= SurfaceCoordMax && (abs == 1 || abs == 3);
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
    public static List<Vector3Int> GetNeighborSurfaceCoords(Vector3Int surfaceCoord)
    {
        var list = new List<Vector3Int>(4);
        Vector3 asVec3 = new Vector3(surfaceCoord.x, surfaceCoord.y, surfaceCoord.z);
        FaceDir faceDir = BallLocationService.GetBallFaceDirByPos(asVec3);
        int normalAxis = FaceDirToNormalAxis(faceDir);
        int t0 = (normalAxis + 1) % 3;
        int t1 = (normalAxis + 2) % 3;
        int[] v = { surfaceCoord.x, surfaceCoord.y, surfaceCoord.z };
        for (int d0 = -2; d0 <= 2; d0 += 2)
        for (int d1 = -2; d1 <= 2; d1 += 2)
        {
            if (d0 == 0 && d1 == 0) continue;
            if (d0 != 0 && d1 != 0) continue; // 只要前后左右，不要对角
            v[t0] += d0;
            v[t1] += d1;
            var neighbor = new Vector3Int(v[0], v[1], v[2]);
            v[t0] -= d0;
            v[t1] -= d1;
            if (IsValidSurfaceCoord(neighbor))
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

    //找大面朝向，方法在BallLocationService.GetBallFaceDirByPos

    #endregion

    # region 张天姿添加：获取指定轴、指定坐标值的所有方块（即某一层的9个方块）  
    public List<CubePiece> GetPiecesInLayer(Axis axis, int coordValue)  
    {  
        var result = new List<CubePiece>();  
        foreach (var slot in slots)  
        {        if (slot.occupant == null) continue;  
            int val = axis switch  
            {  
                Axis.X => slot.coord.x,  
                Axis.Y => slot.coord.y,  
                Axis.Z => slot.coord.z,  
                _ => 0  
            };  
            if (val == coordValue)  
                result.Add(slot.occupant);  
        }    return result;  
    }

    #endregion

    # region 实例化新的房间预制体，rotation是房间对应表面的当前旋转参数，在进入门（能通过）后加载对应房间预制体时调用
    public void spawnedRoom(int roomID, Quaternion rotation)
    {
        CurrentRoom = rooms[roomID].RoomPerfab;
        Instantiate(rooms[roomID].RoomPerfab, rooms[roomID].spawnPoint, rotation);
    }
    #endregion
}
