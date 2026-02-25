using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//using static UnityEngine.Rendering.DebugUI;

public class UIManager : MonoBehaviour
{
    [Header("视角转换按钮")]
    [SerializeField] private Button[] viewSwitchButtons;
    [Header("拧魔方箭头按钮")]
    [SerializeField] private Button[] arrowsButtons;
    [Header("View面板（空物体）")]
    [SerializeField] private GameObject[] viewPanels;
    void Awake()
    {
        BindViewSwitchButtons();
    }
    void OnEnable()
    {
        GameEvents.OnViewSwitchExecute += UpdatePanels;
    }

    void OnDisable()
    {
        GameEvents.OnViewSwitchExecute -= UpdatePanels;
    }

    void UpdatePanels(ViewMode mode)
    {
        for (int i = 0; i < viewPanels.Length; i++)
        {
            viewPanels[i].SetActive(i == (int)mode);
        }
    }
    private void BindViewSwitchButtons()
    {
        if (viewSwitchButtons == null || viewSwitchButtons.Length < 3)
        {
            Debug.LogWarning("视角按钮数量不足！");
            return;
        }

        viewSwitchButtons[0].onClick.AddListener(() => OnViewButtonClick(ViewMode.View1));
        viewSwitchButtons[1].onClick.AddListener(() => OnViewButtonClick(ViewMode.View2));
        viewSwitchButtons[2].onClick.AddListener(() => OnViewButtonClick(ViewMode.View3));
    }

    private void OnViewButtonClick(ViewMode targetMode)
    {
        Debug.Log($"按钮请求切换到 {targetMode}");

        // 向逻辑层发请求
        GameEvents.onDirectViewSwitchRequest(targetMode);
    }
}