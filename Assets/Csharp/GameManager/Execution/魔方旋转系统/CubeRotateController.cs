using System;
using UnityEngine;
using System.Collections;
using static InitCubeSlot;

public class CubeRotateController : MonoBehaviour
{
    bool isDragging;
    bool hasStartedActualDrag;
    bool isSnappingAfterActualDrag;
    Vector3 lastMousePos;
    Vector3 dragStartMousePos;
    Transform view2CameraTransform;
    [SerializeField] private float dragStartThresholdPixels = 8f;

    // ============================
    // 空间计算引用
    // ============================
    // public Transform ball;
    //Transform ball;
    public InitCubeSlot cubeData;
    public Transform cubeRoot;

    public static Vector3 CurrentGDirinMF; //欧：当前重力在魔方坐标系下的矢量方向
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

        Vector3 currentMousePos = Input.mousePosition;
        if (!hasStartedActualDrag)
        {
            if ((currentMousePos - dragStartMousePos).sqrMagnitude <
                dragStartThresholdPixels * dragStartThresholdPixels)
            {
                lastMousePos = currentMousePos;
                return;
            }

            hasStartedActualDrag = true;
            lastMousePos = currentMousePos;
            return;
        }

        Vector3 delta = currentMousePos - lastMousePos;
        lastMousePos = currentMousePos;


        RotateCubeFree(delta);
    }

    // ============================
    // 旋转控制
    // ============================

    void StartRotate()
    {
        isDragging = true;
        hasStartedActualDrag = false;
        isSnappingAfterActualDrag = false;
        dragStartMousePos = Input.mousePosition;
        lastMousePos = dragStartMousePos;
    }

    void StopRotate()
    {
        isDragging = false;
        if (hasStartedActualDrag)
        {
            isSnappingAfterActualDrag = true;
            AutoSnapToNearestFace();
        }
    }

    void RotateCubeFree(Vector3 delta)
    {
        float sensitivity = 0.2f;

        float rotX = delta.y * sensitivity;
        float rotY = -delta.x * sensitivity;
        TryResolveView2CameraTransform();

        Vector3 horizontalAxis = Vector3.up;
        Vector3 verticalAxis = Vector3.right;

        if (view2CameraTransform != null)
        {
            horizontalAxis = view2CameraTransform.up;
            verticalAxis = view2CameraTransform.right;
        }

        transform.Rotate(verticalAxis, rotX, Space.World);
        transform.Rotate(horizontalAxis, rotY, Space.World);
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

        CurrentGDirinMF = FaceOffset[face];     //欧：添加

        Vector3 currentNormal = transform.rotation * DirToVector(face);

        Quaternion alignToGround =
            Quaternion.FromToRotation(currentNormal, Vector3.down)
            * transform.rotation;

        Vector3 twistReferenceAxis = GetTwistReferenceAxis(face);
        Vector3 leveledForward = Vector3.ProjectOnPlane(alignToGround * twistReferenceAxis, Vector3.up);
        if (leveledForward.sqrMagnitude > 0.0001f)
        {
            Vector3 snappedForward = SnapHorizontalAxis(leveledForward.normalized);
            Quaternion yawSnap = Quaternion.FromToRotation(leveledForward.normalized, snappedForward);
            alignToGround = yawSnap * alignToGround;
        }

        StartCoroutine(SmoothRotate(alignToGround, face));
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
        //CalculateBallSpaceState();
        if (isSnappingAfterActualDrag)
        {
            isSnappingAfterActualDrag = false;
            GameEvents.onCubeRotateSettled();
        }
    }

    Vector3 SnapHorizontalAxis(Vector3 direction)
    {
        Vector3[] candidates =
        {
            Vector3.forward,
            Vector3.right,
            Vector3.back,
            Vector3.left
        };

        Vector3 best = Vector3.forward;
        float bestDot = float.NegativeInfinity;

        foreach (Vector3 candidate in candidates)
        {
            float dot = Vector3.Dot(direction, candidate);
            if (dot > bestDot)
            {
                bestDot = dot;
                best = candidate;
            }
        }

        return best;
    }

    Vector3 GetTwistReferenceAxis(FaceDir downFace)
    {
        switch (downFace)
        {
            case FaceDir.Front:
            case FaceDir.Back:
                return Vector3.up;
            default:
                return Vector3.forward;
        }
    }

    bool TryResolveView2CameraTransform()
    {
        if (view2CameraTransform != null)
            return true;

        var activeCameraController = FindObjectOfType<CameraRotateController>();
        if (activeCameraController != null)
        {
            view2CameraTransform = activeCameraController.transform;
            return true;
        }

        foreach (var candidate in Resources.FindObjectsOfTypeAll<CameraRotateController>())
        {
            if (!candidate.gameObject.scene.IsValid())
                continue;

            view2CameraTransform = candidate.transform;
            return true;
        }

        return false;
    }

    // ============================
    // 空间状态计算（新增核心）
    // ============================

    /*void CalculateBallSpaceState()
    {
        Debug.Log("CRC 空间计算请求触发");
        if (ball == null || cubeData == null || cubeRoot == null)
            return;
        GameEvents.onBallSpaceUpdateRequest(ball.position);
    }*/
}
