using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InitCubeSlot : MonoBehaviour
{
    
    //用于整体管理魔方结构
    public List<Slot> slots;
    Dictionary<int, CubePiece> pieceMap;
    Dictionary<int, CubeSurface_s> surfaceMap;
    Dictionary<Vector3Int, CubeSurface_s> surfaceCoordMap;
    Dictionary<Vector3Int, CubePiece> PieceCoordMap;

    //静态数据
    static readonly Dictionary<FaceDir, Vector3Int> FaceOffset =
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
    public enum Axis { X, Y, Z }


    //面朝向
    public enum FaceDir
    {
        Up, Down,
        Left, Right,
        Front, Back
    }

    //槽位（入口）
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



    //方块（显示层）（被Slot引用）
    [System.Serializable]
    public class CubePiece
    {
        [Header("静态数据（不变）")]
        public int id;                              //方块id（固定）
        public Transform indexCube;                 //视觉实体（固定）
        public List<CubeSurface_s> surfaces;        //每个方块带的外表面（固定）

        [Header("动态状态（变化）")]
        public Vector3Int coord;                 //方块当前的逻辑坐标（变化）

        //初始化
        public CubePiece() { }
    }

    //槽位的外表面（被CubePiece引用）
    [System.Serializable]
    public class CubeSurface_s
    {
        [Header("静态数据（不变）")]
        public int id;                      //小面ID（固定）
        public int roomID;                  //此面对应的房间ID（固定）

        [Header("动态状态（变化）")]
        public FaceDir dir;                 //外表面的方向，用于坐标计算（变化）
        public Vector3Int coord;         //外表面的逻辑坐标，旋转后变化

        //初始化
        public CubeSurface_s() { }

        //更新面坐标
        public void UpdatePosition(Vector3Int pieceCoord)
        {
            coord = pieceCoord + FaceOffset[dir];
        }
    }

    //初始化函数
    private void Awake()
    {
        InitData();// 验证数据完整性

        //初始化字典，方便以后单独引用调用面和方块
        BuildSurfaceMap();
        BuildSurfaceCoordMap();
        BuildPieceMap();
        BuildPieceCoordMap();
    }
    

    private void InitData()
    {
        int i = 0;
        foreach (var slot in slots)
        {
            Vector3 vec3 = slot.indexCube.position;
            slot.coord= new Vector3Int(
            Mathf.RoundToInt(vec3.x),
            Mathf.RoundToInt(vec3.y),
            Mathf.RoundToInt(vec3.z)
        )*2;
            if (slot.indexCube == null)
                Debug.LogError($"Slot at {slot.coord} 缺少 indexCube");

            if (slot.occupant != null)
            {
                // 确保棋子位置正确
                slot.occupant.coord = slot.coord;
                slot.occupant.indexCube.position = slot.indexCube.position;
                foreach(var element in slot.occupant.surfaces)
                {
                    element.id = i;
                    element.roomID = i;
                    i++;
                }
            }
        }
    }

    //用id调用SurfaceMap
    void BuildSurfaceMap()
    {
        //调用方法：CubeSurface_s s=SurfaceMap[id]
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
        //调用方法：CubeSurface_s s=SurfaceCoordMap[position]
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
        //调用方法：CubePiece p = pieceMap[id];
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
        //调用方法：CubeSurface_s s=SurfaceCoordMap[position]
        PieceCoordMap = new();

        foreach (var slot in slots)
        {
            if (slot.occupant == null) continue;
            PieceCoordMap[slot.occupant.coord] = slot.occupant;
        }
    }
}
