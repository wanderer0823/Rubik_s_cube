using UnityEngine;

/// <summary>
/// ͨ����������������������¼���ȫ�����ʱ����ͨ�ء�
/// ���ڳ�������������ϣ�������
/// </summary>
public class TaskSystem : MonoBehaviour
{
    public static TaskSystem Instance { get; private set; }

    [Header("ͨ��UI���")]
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
    /// �ⲿ���ã����ָ������
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
    }

    void OnGameWin()
    {
        if (winPanel != null)
            winPanel.SetActive(true);

        // TODO: ��ͣ��Ϸ / ����ͨ�ض���
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
