using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraAction : MonoBehaviour
{
    public Transform player;
    public Vector3 offset = new Vector3(0, 1f, 0);

    [SerializeField] private float minAngle = -80f;
    [SerializeField] private float maxAngle = 80f;

    float xRotation = 0f;
    float yRotation = 0f;

    #region 摇晃参数
    [Header("摇晃参数")]
    private float bobTimer = 0f;
    private float bobFrequency = 4.2f;
    private float bobAmplitude = 0.05f;
    [SerializeField] private float verticalShakeInstence = 1.7f;
    [SerializeField] private float horizontalShakeInstance = 1.7f;
    private float landBobDamping = 0.9f;
    #endregion

    private bool canFollow = false;
    private Coroutine delayCoroutine;

    private static bool s_isFirstActivation = true;

    private void OnEnable()
    {
        GameEvents.OnMouseLookExecute += HandleMouseLook;
        GameEvents.OnWalkMovement += Bob;

        if (s_isFirstActivation)
        {
            s_isFirstActivation = false;

            // 设定初始旋转（相机和玩家）
            transform.rotation = Quaternion.Euler(0, 90, 0);
            xRotation = 0f;
            yRotation = 90f;
            if (player != null)
                player.rotation = Quaternion.Euler(0, 90, 0);

            // 启动延迟跟随
            if (delayCoroutine != null) StopCoroutine(delayCoroutine);
            delayCoroutine = StartCoroutine(DelayFollow(1f));
        }
        else
        {
            // 后续激活：立即跟随，不重置旋转
            if (delayCoroutine != null)
            {
                StopCoroutine(delayCoroutine);
                delayCoroutine = null;
            }
            canFollow = true;
        }
    }

    private void OnDisable()
    {
        GameEvents.OnMouseLookExecute -= HandleMouseLook;
        GameEvents.OnWalkMovement -= Bob;

        if (delayCoroutine != null)
        {
            StopCoroutine(delayCoroutine);
            delayCoroutine = null;
        }
        canFollow = false;
        StopAllCoroutines();
    }

    private IEnumerator DelayFollow(float delay)
    {
        canFollow = false;
        yield return new WaitForSeconds(delay);
        canFollow = true;
        delayCoroutine = null;
    }

    private void LateUpdate()
    {
        transform.position = player.position + offset;

        if (canFollow)
        {
            transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        }
    }

    void HandleMouseLook(Vector2 mouseMove)
    {
        // 延迟期间不允许鼠标旋转
        if (!canFollow) return;

        float mouseX = mouseMove.x;
        float mouseY = mouseMove.y;

        player.Rotate(Vector3.up * mouseX);
        yRotation += mouseX;

        xRotation -= mouseY;
        // xRotation = Mathf.Clamp(xRotation, minAngle, maxAngle);
    }

    void Bob(Vector3 moveDir)
    {
        float speed = moveDir.magnitude;
        bobTimer += Time.deltaTime * bobFrequency;

        float speedFactor = Mathf.Clamp01(speed / 5f);
        float dynamicAmplitude = bobAmplitude * speedFactor;

        float verticalBob = verticalShakeInstence * Mathf.Sin(bobTimer * Mathf.PI) * dynamicAmplitude;
        float horizontalBob = horizontalShakeInstance * Mathf.Cos(bobTimer * Mathf.PI * 0.5f) * (dynamicAmplitude * 0.5f);

        offset.y = 1.7f + verticalBob;
        offset.x = horizontalBob;
    }

    void Stop()
    {
        bobTimer = 0f;
        offset.y = Mathf.Lerp(offset.y, 3f, Time.deltaTime * landBobDamping);
        offset.x = Mathf.Lerp(offset.x, 0f, Time.deltaTime * landBobDamping);
    }
}