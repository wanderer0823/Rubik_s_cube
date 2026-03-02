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
    public static CubeSurface_s CalculateSurface(
        Transform cubeRoot,
        InitCubeSlot cubeData,
        Vector3 ballWorldPos)
    {
        // 1 转换到魔方本地空间
        Vector3 localPos = cubeRoot.InverseTransformPoint(ballWorldPos);
        Debug.Log("小球本地位置：" + localPos);

        // 2 取最近逻辑坐标（-3,0,3）
        Vector3Int nearestPieceCoord = new Vector3Int(
            RoundToLogic(localPos.x),
            RoundToLogic(localPos.y),
            RoundToLogic(localPos.z)
        );
        Debug.Log("取本地最近位置：" + nearestPieceCoord);

        // 3 判断在哪个面（用最大轴判断）
        FaceDir faceDir = GetBallFaceDirByPos(localPos);
        Debug.Log("小球在面：" + faceDir);

        // 4 计算该表面的逻辑坐标
        Vector3Int surfaceCoord =
            nearestPieceCoord +
            FaceOffset[faceDir];
        Debug.Log("小球在表面正方形：" + surfaceCoord);

        // 5 通过 surfaceCoordMap 查表面
        var surface = cubeData.GetSurfaceByCoord(surfaceCoord);
        
        if (surface != null)
        {
            //Debug.Log("303");
            return surface;
        }
        return null;
        
    }

    // =========================
    // 内部工具方法
    // =========================

    public static int RoundToLogic(float value)
    {
        return Mathf.RoundToInt(value / 2f) * 2;//保留偶数或向上取整获得偶数
        //return Mathf.FloorToInt(value*2);
    }

    public static FaceDir GetBallFaceDirByWorldPos(Transform ballWorldPos)
    {
        Vector3 localPos = ViewModeManager.Instance.cubeRoot.InverseTransformPoint(ballWorldPos.position);
            
        Vector3Int nearestPieceCoord = new Vector3Int(
            RoundToLogic(localPos.x),
            RoundToLogic(localPos.y),
            RoundToLogic(localPos.z)
        );
            
        FaceDir faceDir = BallLocationService.GetBallFaceDirByPos(localPos);
        return faceDir;
    }

    //根据小球位置获取其所在面朝向
    public static FaceDir GetBallFaceDirByPos(Vector3 localPos)
    {
        float absX = Mathf.Abs(localPos.x);
        float absY = Mathf.Abs(localPos.y);
        float absZ = Mathf.Abs(localPos.z);

        if (absY >= absX && absY >= absZ)
            return localPos.y > 0 ?
                FaceDir.Up :
                FaceDir.Down;

        if (absX >= absY && absX >= absZ)
            return localPos.x > 0 ?
                FaceDir.Right :
                FaceDir.Left;

        return localPos.z > 0 ?
            FaceDir.Front :
            FaceDir.Back;
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