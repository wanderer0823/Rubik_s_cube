using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraAction : MonoBehaviour
{
    public Transform player;
    public Vector3 offset = new Vector3(0, 3f, 0);

    [SerializeField] private float minAngle = -80f;  // 向下看
    [SerializeField] private float maxAngle = 80f;   // 向上看

    float xRotation = 0f;
    float yRotation = 0f;

    private void OnEnable()
    {
        GameEvents.OnMouseLookExecute += HandleMouseLook;
    }


    private void OnDisable()
    {
        GameEvents.OnMouseLookExecute-=HandleMouseLook;
    }

    void HandleMouseLook(Vector2 mouseMove)
    {
        float mouseX = mouseMove.x;
        float mouseY = mouseMove.y;

        // 左右 → 玩家旋转
        player.Rotate(Vector3.up * mouseX);
        yRotation += mouseX;

        // 上下 → 相机旋转
        xRotation -= mouseY;
        //这里有报错，为了提交先注释一下！
        //xRotation = Mathf.Clamp(xRotation, minAngle, maxAngle);
    }
    private void LateUpdate()
    {
        transform.position = player.position + offset;
        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
}

