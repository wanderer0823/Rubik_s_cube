using UnityEngine;
using static InitCubeSlot;

public static class BallLocationService
{
    /// <summary>
    /// 计算小球当前所在的外表面（房间）
    /// </summary>
    /// <param name="cubeRoot">魔方根物体</param>
    /// <param name="cubeData">InitCubeSlot 引用</param>
    /// <param name="ballWorldPos">小球世界坐标</param>
    public static InitCubeSlot.CubeSurface_s CalculateSurface(
        Transform cubeRoot,
        InitCubeSlot cubeData,
        Vector3 ballWorldPos)
    {
        // 1 转换到魔方本地空间
        Vector3 localPos = cubeRoot.InverseTransformPoint(ballWorldPos);

        // 2 取最近逻辑坐标（-3,0,3）
        Vector3Int nearestPieceCoord = new Vector3Int(
            RoundToLogic(localPos.x),
            RoundToLogic(localPos.y),
            RoundToLogic(localPos.z)
        );

        // 3 判断在哪个面（用最大轴判断）
        InitCubeSlot.FaceDir faceDir = GetFaceDir(localPos);

        // 4 计算该表面的逻辑坐标
        Vector3Int surfaceCoord =
            nearestPieceCoord +
            FaceOffset[faceDir];

        // 5 通过 surfaceCoordMap 查表面
        var surface = cubeData.GetSurfaceByCoord(surfaceCoord);

        if (surface != null)
        {
            return surface;
        }
        return null;
    }

    // =========================
    // 内部工具方法
    // =========================

    static int RoundToLogic(float value)
    {
        return Mathf.RoundToInt(value / 2f) * 2;
    }

    static InitCubeSlot.FaceDir GetFaceDir(Vector3 localPos)
    {
        float absX = Mathf.Abs(localPos.x);
        float absY = Mathf.Abs(localPos.y);
        float absZ = Mathf.Abs(localPos.z);

        if (absY >= absX && absY >= absZ)
            return localPos.y > 0 ?
                InitCubeSlot.FaceDir.Up :
                InitCubeSlot.FaceDir.Down;

        if (absX >= absY && absX >= absZ)
            return localPos.x > 0 ?
                InitCubeSlot.FaceDir.Right :
                InitCubeSlot.FaceDir.Left;

        return localPos.z > 0 ?
            InitCubeSlot.FaceDir.Front :
            InitCubeSlot.FaceDir.Back;
    }

   

    public static FaceDir CalculateGravityFace(Vector3 localDown)
    {
        float maxDot = -999f;
        FaceDir result = FaceDir.Up;

        foreach (FaceDir dir in System.Enum.GetValues(typeof(FaceDir)))
        {
            Vector3 v = DirToVectorStatic(dir);

            float dot = Vector3.Dot(v, localDown);

            if (dot > maxDot)
            {
                maxDot = dot;
                result = dir;
            }
        }

        return result;
    }

    static Vector3 DirToVectorStatic(FaceDir dir)
    {
        switch (dir)
        {
            case FaceDir.Up: return Vector3.up;
            case FaceDir.Down: return Vector3.down;
            case FaceDir.Left: return Vector3.left;
            case FaceDir.Right: return Vector3.right;
            case FaceDir.Front: return Vector3.forward;
            case FaceDir.Back: return Vector3.back;
        }

        return Vector3.up;
    }
}