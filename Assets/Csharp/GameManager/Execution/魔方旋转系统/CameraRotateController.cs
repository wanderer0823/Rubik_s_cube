using UnityEngine;
using System.Collections;

public class CameraRotateController : MonoBehaviour
{
    bool isDragging;
    Vector3 lastMousePos;
    Coroutine snapCoroutine;

    public Transform cubeCenter;
    [SerializeField] private float snapSpeed = 3f;

    void OnEnable()
    {
        GameEvents.OnCameraRotateExecute += StartRotate;
        GameEvents.OnCameraRotateFinishExecute += StopRotate;
    }

    void OnDisable()
    {
        GameEvents.OnCameraRotateExecute -= StartRotate;
        GameEvents.OnCameraRotateFinishExecute -= StopRotate;
    }

    void Update()
    {
        if (!isDragging) return;

        Vector3 delta = Input.mousePosition - lastMousePos;
        lastMousePos = Input.mousePosition;

        RotateCamera(delta);
    }

    void StartRotate()
    {
        if (snapCoroutine != null)
        {
            StopCoroutine(snapCoroutine);
            snapCoroutine = null;
        }

        isDragging = true;
        lastMousePos = Input.mousePosition;
    }

    void StopRotate()
    {
        isDragging = false;
        AutoSnapToFacingFace();
    }

    void RotateCamera(Vector3 delta)
    {
        float sensitivity = 0.2f;

        transform.RotateAround(
            cubeCenter.position,
            Vector3.up,
            delta.x * sensitivity
        );

        transform.RotateAround(
            cubeCenter.position,
            transform.right,
            -delta.y * sensitivity
        );
    }

    void AutoSnapToFacingFace()
    {
        if (cubeCenter == null)
            return;

        Vector3 offset = transform.position - cubeCenter.position;
        float radius = offset.magnitude;
        if (radius <= Mathf.Epsilon)
            return;

        Vector3 localForward = cubeCenter.InverseTransformDirection(transform.forward).normalized;
        Vector3 snappedLocalForward = SnapToPrimaryAxis(localForward);
        Vector3 localFaceNormal = -snappedLocalForward;
        Vector3 targetWorldDir = cubeCenter.TransformDirection(localFaceNormal).normalized;
        Vector3 targetPosition = cubeCenter.position + targetWorldDir * radius;
        Vector3 targetForward = (cubeCenter.position - targetPosition).normalized;
        Vector3 targetUp = Vector3.ProjectOnPlane(transform.up, targetForward).normalized;
        if (targetUp.sqrMagnitude < 0.0001f)
            targetUp = cubeCenter.TransformDirection(GetLocalUp(localFaceNormal)).normalized;
        targetUp = GetQuantizedFaceUp(localFaceNormal, targetUp);

        Quaternion targetRotation = Quaternion.LookRotation(cubeCenter.position - targetPosition, targetUp);

        snapCoroutine = StartCoroutine(SmoothSnap(targetPosition, targetRotation));
    }

    IEnumerator SmoothSnap(Vector3 targetPosition, Quaternion targetRotation)
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * Mathf.Max(0.01f, snapSpeed);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;
        snapCoroutine = null;
    }

    static Vector3 SnapToPrimaryAxis(Vector3 v)
    {
        float absX = Mathf.Abs(v.x);
        float absY = Mathf.Abs(v.y);
        float absZ = Mathf.Abs(v.z);

        if (absX >= absY && absX >= absZ)
            return v.x >= 0f ? Vector3.right : Vector3.left;

        if (absY >= absX && absY >= absZ)
            return v.y >= 0f ? Vector3.up : Vector3.down;

        return v.z >= 0f ? Vector3.forward : Vector3.back;
    }

    static Vector3 GetLocalUp(Vector3 localDir)
    {
        if (localDir == Vector3.up)
            return Vector3.back;

        if (localDir == Vector3.down)
            return Vector3.forward;

        return Vector3.up;
    }

    Vector3 GetQuantizedFaceUp(Vector3 localFaceNormal, Vector3 currentWorldUp)
    {
        Vector3 referenceLocalUp = GetLocalUp(localFaceNormal);
        Vector3 referenceLocalRight = Vector3.Cross(-localFaceNormal, referenceLocalUp);
        Vector3 currentLocalUp = cubeCenter.InverseTransformDirection(currentWorldUp).normalized;

        Vector3[] candidates =
        {
            referenceLocalUp,
            referenceLocalRight,
            -referenceLocalUp,
            -referenceLocalRight
        };

        Vector3 bestLocalUp = referenceLocalUp;
        float bestDot = float.NegativeInfinity;

        foreach (Vector3 candidate in candidates)
        {
            float dot = Vector3.Dot(currentLocalUp, candidate);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestLocalUp = candidate;
            }
        }

        return cubeCenter.TransformDirection(bestLocalUp).normalized;
    }
}
