using System.Collections.Generic;
using UnityEngine;
using static InitCubeSlot;

/// <summary>
/// 邻居房间预加载结果：当前房间、逻辑邻居列表、各房间重力与门通道信息。
/// 由 RoomPreloadController 计算后通过事件发给 VMM / RoomInstanceManager。
/// </summary>
public class NeighborPreloadPayload
{
    /// <summary> 玩家当前所在房间 ID </summary>
    public int CurrentRoomID { get; set; }

    /// <summary> 可与当前房间形成通道的逻辑邻居房间 ID 集合（用于实例化/销毁） </summary>
    public HashSet<int> LogicalNeighborRoomIds { get; set; }

    /// <summary> 各房间的加载属性：重力方向 + 各门能否形成通道 </summary>
    public Dictionary<int, RoomLoadData> RoomDataByRoomId { get; set; }

    public NeighborPreloadPayload()
    {
        LogicalNeighborRoomIds = new HashSet<int>();
        RoomDataByRoomId = new Dictionary<int, RoomLoadData>();
    }
}

/// <summary> 单个房间的预加载子属性 </summary>
public class RoomLoadData
{
    /// <summary> 房间内重力方向（大面朝向） </summary>
    public FaceDir GravityFace { get; set; }

    /// <summary> 每个方向的门能否形成通道 [FaceDir] -> bool </summary>
    public Dictionary<FaceDir, bool> CanFormPassageByDoor { get; set; }

    public RoomLoadData()
    {
        CanFormPassageByDoor = new Dictionary<FaceDir, bool>();
    }
}
