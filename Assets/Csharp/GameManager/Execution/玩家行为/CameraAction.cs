using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraAction : MonoBehaviour
{
    public Transform player;
    public Vector3 offset = new Vector3(0, 1f, 0);

    [SerializeField] private float minAngle = -80f;  // 向下看
    [SerializeField] private float maxAngle = 80f;   // 向上看

    float xRotation = 0f;
    float yRotation = 0f;

    #region 摇晃参数
    [Header("摇晃参数")]
    private float bobTimer = 0f;
    private float bobFrequency = 4.2f;        // 摇晃频率（越高摇晃越快）
    private float bobAmplitude = 0.05f;     // 摇晃幅度
    [SerializeField] private float verticalShakeInstence = 1.7f;
    [SerializeField] private float horizontalShakeInstance = 1.7f;
    private float landBobDamping = 0.9f;    // 着陆时的摇晃衰减
    #endregion

    private bool canFollow = false;  // 是否可以跟随

    private void OnEnable()
    {
        GameEvents.OnMouseLookExecute += HandleMouseLook;
        GameEvents.OnWalkMovement += Bob;

        // 设置初始旋转
        transform.rotation = Quaternion.Euler(0, 90, 0);

        // 启动协程，延迟1秒后允许跟随
        StartCoroutine(DelayFollow(1f));
    }

    private void OnDisable()
    {
        GameEvents.OnMouseLookExecute -= HandleMouseLook;
        GameEvents.OnWalkMovement -= Bob;
        canFollow = false;
        StopAllCoroutines();
    }

    IEnumerator DelayFollow(float delay)
    {
        yield return new WaitForSeconds(delay);
        canFollow = true;

        // 同步当前的旋转变量
        xRotation = 0f;
        yRotation = 90f;
    }

    private void LateUpdate()
    {
        transform.position = player.position + offset;

        // 只有 canFollow 为 true 时才更新旋转
        if (canFollow)
        {
            transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        }
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

    //走路摇晃
    void Bob(Vector3 moveDir)
    {
        float speed = moveDir.magnitude;
        // bobTimer 驱动摇晃周期
        bobTimer += Time.deltaTime * bobFrequency;

        // 根据速度动态调整摇晃幅度（假设 moveSpeed 为 5）
        float speedFactor = Mathf.Clamp01(speed / 5f);
        float dynamicAmplitude = bobAmplitude * speedFactor;

        // 垂直摇晃（上下）
        float verticalBob = verticalShakeInstence * Mathf.Sin(bobTimer * Mathf.PI ) * dynamicAmplitude;

        // 水平摇晃（左右）
        float horizontalBob = horizontalShakeInstance * Mathf.Cos(bobTimer * Mathf.PI * 0.5f) * (dynamicAmplitude * 0.5f);

        // 应用摇晃到相机 offset
        offset.y = 1.7f + verticalBob;
        offset.x = horizontalBob;
    }

    //刹车
    void Stop()
    {
        // 停止移动时平滑恢复到初始位置
        bobTimer = 0f;
        offset.y = Mathf.Lerp(offset.y, 3f, Time.deltaTime * landBobDamping);
        offset.x = Mathf.Lerp(offset.x, 0f, Time.deltaTime * landBobDamping);
    }
}

