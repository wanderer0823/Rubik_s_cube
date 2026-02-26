using UnityEngine;

public class CameraRotateController : MonoBehaviour
{
    bool isDragging;
    Vector3 lastMousePos;

    public Transform cubeCenter;

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
        isDragging = true;
        lastMousePos = Input.mousePosition;
    }

    void StopRotate()
    {
        isDragging = false;
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
}