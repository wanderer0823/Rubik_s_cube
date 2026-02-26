using UnityEngine;
using System.Collections;
using static InitCubeSlot;

public class CubeRotateController : MonoBehaviour
{
    bool isDragging;
    Vector3 lastMousePos;

    // ============================
    // 空间计算引用
    // ============================
    public Transform ball;
    public InitCubeSlot cubeData;
    public Transform cubeRoot;

    // ============================
    // 生命周期
    // ============================

    void OnEnable()
    {
        GameEvents.OnCubeRotateExecute += StartRotate;
        GameEvents.OnCubeRotateFinishExecute += StopRotate;
    }

    void OnDisable()
    {
        GameEvents.OnCubeRotateExecute -= StartRotate;
        GameEvents.OnCubeRotateFinishExecute -= StopRotate;
    }

    void Update()
    {
        if (!isDragging) return;

        Vector3 delta = Input.mousePosition - lastMousePos;
        lastMousePos = Input.mousePosition;

        RotateCubeFree(delta);
    }

    // ============================
    // 旋转控制
    // ============================

    void StartRotate()
    {
        isDragging = true;
        lastMousePos = Input.mousePosition;
    }

    void StopRotate()
    {
        isDragging = false;
        AutoSnapToNearestFace();
    }

    void RotateCubeFree(Vector3 delta)
    {
        float sensitivity = 0.2f;

        float rotX = delta.y * sensitivity;
        float rotY = -delta.x * sensitivity;

        transform.Rotate(Vector3.right, rotX, Space.World);
        transform.Rotate(Vector3.up, rotY, Space.World);
    }

    // ============================
    // 回正 + 空间计算核心
    // ============================

    FaceDir GetClosestFaceToGround()
    {
        float maxDot = -999f;
        FaceDir closest = FaceDir.Up;

        foreach (FaceDir dir in System.Enum.GetValues(typeof(FaceDir)))
        {
            Vector3 localDir = DirToVector(dir);
            Vector3 worldDir = transform.rotation * localDir;

            float dot = Vector3.Dot(worldDir, Vector3.down);

            if (dot > maxDot)
            {
                maxDot = dot;
                closest = dir;
            }
        }

        return closest;
    }

    Vector3 DirToVector(FaceDir dir)
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

    // ============================
    // 回正结束 → 空间计算入口
    // ============================

    void AutoSnapToNearestFace()
    {
        FaceDir face = GetClosestFaceToGround();

        Vector3 currentNormal = transform.rotation * DirToVector(face);

        Quaternion targetRot =
            Quaternion.FromToRotation(currentNormal, Vector3.down)
            * transform.rotation;

        StartCoroutine(SmoothRotate(targetRot, face));
    }

    IEnumerator SmoothRotate(Quaternion target, FaceDir gravityFace)
    {
        Quaternion start = transform.rotation;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            transform.rotation = Quaternion.Slerp(start, target, t);
            yield return null;
        }

        transform.rotation = target;

        // 回正完成后计算空间状态
        CalculateBallSpaceState();
    }

    // ============================
    // 空间状态计算（新增核心）
    // ============================

    void CalculateBallSpaceState()
    {
        Debug.Log("CRC 空间计算请求触发");
        if (ball == null || cubeData == null || cubeRoot == null)
            return;
        GameEvents.onBallSpaceUpdateRequest(ball.position);
    }
}