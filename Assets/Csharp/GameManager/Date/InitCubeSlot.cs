using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InitCubeSlot : MonoBehaviour
{
    //用于整体管理魔方结构
    public List<Slot> slots = new List<Slot>();

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
                piece.CurrentCoord = coord;
                piece.view.position = indexCube.position;
            }
        }
    }



    //方块（显示层）（被Slot引用）
    [System.Serializable]
    public class CubePiece
    {
        [Header("静态数据（不变）")]
        public int id;                              //方块id（固定）
        public Transform view;                      //视觉实体（固定）
        public List<CubeSurface_s> surfaces;        //每个方块带的外表面（固定）

        [Header("动态状态（变化）")]
        public Vector3Int CurrentCoord;             //方块当前坐标（变化）

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
        public Vector3Int position;         //外表面坐标信息，旋转后变化

        //初始化
        public CubeSurface_s() { }

        //更新面坐标
        public void UpdatePosition(Vector3Int pieceCoord)
        {
            position = pieceCoord + FaceOffset[dir];
        }
    }

    //初始化函数
    private void Awake()
    {
        InitData();// 验证数据完整性
    }

    private void InitData()
    {
        foreach (var slot in slots)
        {
            if (slot.indexCube == null)
                Debug.LogError($"Slot at {slot.coord} 缺少 indexCube");

            if (slot.occupant != null)
            {
                // 确保棋子位置正确
                slot.occupant.CurrentCoord = slot.coord;
                slot.occupant.view.position = slot.indexCube.position;
            }
        }
    }

}
