using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static InitCubeSlot;

public class View1CameraManager : MonoBehaviour
{
    public Transform cubeCenter;
    public float View1CameraDist = 10.0f;

    private void OnEnable()
    {
        //     л  ӽ 1
        GameEvents.IsView1Now += TransCamera;
    }

    private void OnDisable()
    {
        //ȡ       л  ӽ 1
        GameEvents.IsView1Now -= TransCamera;
    }

    //Yiu：注释掉旧玩法的计算方法，并使用BallVisualController的静态方法。
    /*private void TransCamera()
    {
        FaceDir face = GameState.Instance.CurrentPlayerFace;

        // 玩家所在面的本地法向量（不变）
        //Vector3 localDir = (Vector3)FaceOffset[face];

        // 转换到世界坐标，考虑魔方在 View2 中被旋转后的实际朝向
        Vector3 worldDir = cubeCenter.TransformDirection(localDir);
        Debug.Log("V1CM:玩家所在面的本地法向量：" + localDir);

        // 相机放在该面外侧
        transform.position = cubeCenter.position + worldDir * View1CameraDist;
        Debug.Log("V1CM:玩家所在面的本地法向量：" + localDir);

        // 计算屏幕"上"方向（魔方本地坐标系），避免 Up/Down 面 LookAt 退化
        Vector3 localUp = GetLocalUp(face);
        Vector3 worldUp = cubeCenter.TransformDirection(localUp);
        Debug.Log("V1CM:玩家所在面的本地法向量：" + localDir);

        transform.LookAt(cubeCenter.position, worldUp);
    }*/
    private void TransCamera()
    {
        int roomID = GameState.Instance.CurrentRoomID;

        // 用 BallVisualController 的静态方法获取世界方向
        Vector3 worldDir = BallVisualController.GetSurfaceWorldDirection(roomID);

        // 相机放在该面外侧
        transform.position = cubeCenter.position + worldDir * View1CameraDist;

        // 计算屏幕"上"方向
        FaceDir face = GameState.Instance.CurrentPlayerFace;
        Vector3 localUp = GetLocalUp(face);
        Vector3 worldUp = cubeCenter.TransformDirection(localUp);

        transform.LookAt(cubeCenter.position, worldUp);
    }


    /// <summary>
    /// 根据展开图（Up-Front-Down-Back 纵列，Left-Front-Right 横排）推导：
    /// 展开图中每个面折叠后，其"上边缘"对应的魔方本地方向即为 worldUp。
    /// Up   面在 Front 上方 → 折叠后屏幕上 = Z-（Back 方向）
    /// Down 面在 Front 下方 → 折叠后屏幕上 = Z+（Front 方向）
    /// 四侧面屏幕上 = 魔方本地 Y+
    /// </summary>
    private Vector3 GetLocalUp(FaceDir face)
    {
        return face switch
        {
            FaceDir.Up => Vector3.back,
            FaceDir.Down => Vector3.forward,
            _ => Vector3.up
        };
    }
}
