using UnityEngine;

/// <summary>
/// 通关任务管理。监听任务完成事件，全部完成时触发通关。
/// 挂在场景管理空物体上，单例。
/// </summary>
public class TaskSystem : MonoBehaviour
{
    public static TaskSystem Instance { get; private set; }

    [Header("通关UI面板")]
    [SerializeField] private GameObject winPanel;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        GameEvents.OnTaskFinished += OnTaskFinished;
        GameEvents.OnGameWin += OnGameWin;
    }

    void OnDisable()
    {
        GameEvents.OnTaskFinished -= OnTaskFinished;
        GameEvents.OnGameWin -= OnGameWin;
    }

    void Start()
    {
        if (winPanel != null)
            winPanel.SetActive(false);
    }

    /// <summary>
    /// 外部调用：完成指定任务
    /// </summary>
    public void CompleteTask(int taskIndex)
    {
        var gs = GameState.Instance;
        if (gs == null) return;

        if (gs.FinishTask(taskIndex))
        {
            GameEvents.onTaskFinished(taskIndex);

            if (gs.AllTasksFinished())
            {
                GameEvents.onGameWin();
            }
        }
    }

    void OnTaskFinished(int taskIndex)
    {
        Debug.Log($"TaskSystem: 任务 {taskIndex} 完成，" +
                  $"进度 {GetCompletedCount()}/4");
    }

    void OnGameWin()
    {
        Debug.Log("TaskSystem: 全部任务完成，游戏通关！");
        if (winPanel != null)
            winPanel.SetActive(true);

        // TODO: 暂停游戏 / 播放通关动画
    }

    int GetCompletedCount()
    {
        var gs = GameState.Instance;
        if (gs == null) return 0;
        int count = 0;
        foreach (var t in gs.TaskFinished)
            if (t) count++;
        return count;
    }
}
