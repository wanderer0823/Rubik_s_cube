using UnityEngine;
using static InitCubeSlot;

public static class BallLocationService
{
    /// <summary>
    /// ����С��ǰ���ڵ�����棨���䣩
    /// </summary>
    /// <param name="cubeRoot">ħ��������</param>
    /// <param name="cubeData">InitCubeSlot ����</param>
    /// <param name="ballWorldPos">С����������</param>
    public static CubeSurface_s CalculateSurface(
        Transform cubeRoot,
        InitCubeSlot cubeData,
        Vector3 ballWorldPos)
    {
        // 1 ת����ħ�����ؿռ�
        Vector3 localPos = cubeRoot.InverseTransformPoint(ballWorldPos);
            //Debug.Log("С�򱾵�λ�ã�" + localPos);

        // 2 ȡ����߼����꣨-3,0,3��
        Vector3Int nearestPieceCoord = new Vector3Int(
            RoundToLogic(localPos.x),
            RoundToLogic(localPos.y),
            RoundToLogic(localPos.z)
        );
            //Debug.Log("ȡ�������λ�ã�" + nearestPieceCoord);

        // 3 �ж����ĸ��棨��������жϣ�
        FaceDir faceDir = GetBallFaceDirByPos(localPos);
            //Debug.Log("С�����棺" + faceDir);

        // 4 ����ñ�����߼�����
        Vector3Int surfaceCoord =
            nearestPieceCoord +
            FaceOffset[faceDir];
        Debug.Log("С���ڱ��������Σ�" + surfaceCoord);

        // 5 ͨ�� surfaceCoordMap �����
        var surface = cubeData.GetSurfaceByCoord(surfaceCoord);
        
        if (surface != null)
        {
            //Debug.Log("303");
            return surface;
        }
        return null;
        
    }

    // =========================
    // �ڲ����߷���
    // =========================

    public static int RoundToLogic(float value)
    {
        return Mathf.RoundToInt(value / 2f) * 2;//����ż��������ȡ�����ż��
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

    //����С��λ�û�ȡ�������泯��
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