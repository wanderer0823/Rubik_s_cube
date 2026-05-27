using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class AllButtonSubscribe : MonoBehaviour
{
    private static List<Button> subscribedButtons = new List<Button>();
    private static bool isSubscribed = false;
    
    [RuntimeInitializeOnLoadMethod]
    static void InitializeOnLoad()
    {
        // 游戏启动时自动订阅（可选）
        SubscribeAllButtonsOnce();
    }
    
    [ContextMenu("订阅所有按钮")]
    public static void SubscribeAllButtonsOnce()
    {
        if (isSubscribed) return;
        
        Button[] allButtons = FindObjectsOfType<Button>(true);
        
        foreach (Button btn in allButtons)
        {
            if (!subscribedButtons.Contains(btn))
            {
                btn.onClick.AddListener(GlobalButtonHandler);
                subscribedButtons.Add(btn);
            }
        }
        
        isSubscribed = true;
        /*__DEBUGTOOL_START__*/Debug.Log($"已订阅 {subscribedButtons.Count} 个按钮");/*__DEBUGTOOL_END__*/
    }
    
    static void GlobalButtonHandler()
    {
        // 获取当前点击的按钮（通过UI事件系统）
        GameObject currentButton = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
        
        if (currentButton != null)
        {
            ///*__DEBUGTOOL_START__*/Debug.Log($"全局点击：{currentButton.name}");/*__DEBUGTOOL_END__*/
            
            // 这里执行所有按钮的通用逻辑
            MusicAudioManager.Instance.PlaySfx("ui");
        }
    }
    
    [ContextMenu("清除订阅记录")]
    public static void ClearSubscription()
    {
        foreach (var btn in subscribedButtons)
        {
            if (btn != null)
                btn.onClick.RemoveListener(GlobalButtonHandler);
        }
        subscribedButtons.Clear();
        isSubscribed = false;
        /*__DEBUGTOOL_START__*/Debug.Log("已清除所有按钮订阅");/*__DEBUGTOOL_END__*/
    }
}