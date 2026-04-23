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

    private void TransCamera()
    {
        FaceDir face = GameState.Instance.CurrentPlayerFace;

        // 玩家所在面的本地法向量
        Vector3 localDir = (Vector3)FaceOffset[face];
        // 转换到世界坐标，考虑魔方在 View2 中被旋转后的实际朝向
        Vector3 worldDir = cubeCenter.TransformDirection(localDir);

        // 相机放在该面外侧
        transform.position = cubeCenter.position + worldDir * View1CameraDist;

        // 计算屏幕"上"方向（魔方本地坐标系），避免 Up/Down 面 LookAt 退化
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
        switch (face)
        {
            case FaceDir.Up: return Vector3.back;    // 顶面：屏幕上 = 魔方本地 Z-
            case FaceDir.Down: return Vector3.forward; // 底面：屏幕上 = 魔方本地 Z+
            default: return Vector3.up;      // 四侧面：屏幕上 = 魔方本地 Y+
        }
    }
}
