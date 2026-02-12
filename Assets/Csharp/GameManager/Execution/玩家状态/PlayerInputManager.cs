using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputManager : MonoBehaviour
{
    public static PlayerInputManager Instance { get; private set; }

    //[Header("玩家输入状态")]
    public enum PlayerState
    {
        isTurning,
        isRotating,
        isWaiting,
        turningFinished,
        rotatingFinished,
        isMoving
    }
    public PlayerState currentPlayerState;

    private void Awake()
    {
        // 单例初始化
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 如果想跨场景保留
    }
}
