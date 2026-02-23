using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeRotateController
{
    private Camera view2Camera;
    private Transform cubeCenter;

    private float rotateSpeed = 5f;
    private float distance;

    private float currentYaw;
    private float currentPitch;

    private bool isRotating = false;

    public CubeRotateController(Camera cam, Transform cubeTransform)
    {
        view2Camera = cam;
        cubeCenter = cubeTransform;

        // 计算初始距离
        distance = Vector3.Distance(view2Camera.transform.position, cubeCenter.position);

        Vector3 angles = view2Camera.transform.eulerAngles;
        currentYaw = angles.y;
        currentPitch = angles.x;
    }

    // 由 GM 的 Update 调用
    public void Tick()
    {
        HandleMouseInput();

        if (isRotating)
        {
            RotateCamera();
        }
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            OnMouseDown();
        }

        if (Input.GetMouseButtonUp(0))
        {
            OnMouseUp();
        }
    }

    // 鼠标按下
    public void OnMouseDown()
    {
        isRotating = true;
    }

    // 鼠标松开
    public void OnMouseUp()
    {
        isRotating = false;
    }

    private void RotateCamera()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        currentYaw += mouseX * rotateSpeed;
        currentPitch -= mouseY * rotateSpeed;

        currentPitch = Mathf.Clamp(currentPitch, -80f, 80f);

        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);

        Vector3 offset = rotation * new Vector3(0, 0, -distance);

        view2Camera.transform.position = cubeCenter.position + offset;
        view2Camera.transform.LookAt(cubeCenter.position);
    }
}
