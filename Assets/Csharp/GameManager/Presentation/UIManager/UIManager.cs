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
    [Header("每个视角的相机")]
    [SerializeField] private Camera[] viewCameras;

    void Awake()
    {
        //初始化相机状态
        viewCameras[2].gameObject.SetActive(true);
        //为视角切换按钮添加点击广播
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

    #region ===================================================
    #region 按钮点击事件，发送请求
    void UpdatePanels(ViewMode mode)
    {
        for (int i = 0; i < viewPanels.Length; i++)
        {
            viewPanels[i].SetActive(i == (int)mode);
            viewCameras[i].gameObject.SetActive(i == (int)mode);
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
    #endregion
    #endregion

    #region ===================================================
    #region 相机启动管理
    private void DisableAllCameras()
    {
        for (int i = 0; i < viewCameras.Length; i++)
        {
            viewCameras[i].gameObject.SetActive(false);
        }
    }
    #endregion
    #endregion
}