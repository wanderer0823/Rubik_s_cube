using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("背包系统")]
    [SerializeField] private GameObject backpackPanel;
    [SerializeField] private Button backpackButton;   // UI上的背包开关按钮（可选，和Tab键功能相同）

    [Header("准星（仅View3）")]
    [SerializeField] private GameObject crosshairUI;

    [Header("视角切换按钮")]
    [SerializeField] private Button[] viewSwitchButtons;

    [Header("箭头按钮列表")]
    [SerializeField] private List<Button> arrowsButtons;

    [Header("不同视角对应的UI面板")]
    [SerializeField] private GameObject[] viewPanels;

    [Header("不同视角对应的摄像机")]
    [SerializeField] private Camera[] viewCameras;

    void Awake()
    {
        // 默认启用 View3 摄像机
        viewCameras[1].gameObject.SetActive(true);

        // 初始化 View3 面板
        InitView3Panel();

        // ===== 新增：初始化背包 =====
        if (backpackPanel != null)
            backpackPanel.SetActive(false);

        if (backpackButton != null)
            backpackButton.onClick.AddListener(OnBackpackButtonClick);

        // 绑定视角切换按钮
        BindViewSwitchButtons();

        // 箭头按钮绑定（由外部初始化后调用）
        // BindArrowsButtons();

        Instance = this;
    }

    void OnEnable()
    {
        GameEvents.OnViewSwitchExecute += UpdatePanels;
        // ===== 新增 =====
        GameEvents.OnBagOpenExecute += OnBagOpen;
        GameEvents.OnBagCloseExecute += OnBagClose;
    }

    void OnDisable()
    {
        GameEvents.OnViewSwitchExecute -= UpdatePanels;
        // ===== 新增 =====
        GameEvents.OnBagOpenExecute -= OnBagOpen;
        GameEvents.OnBagCloseExecute -= OnBagClose;
    }

    #region ================= View3 Panel 初始化 =================
    void InitView3Panel()
    {
        if (crosshairUI != null)
            crosshairUI.SetActive(true);

        if (viewPanels == null || viewPanels.Length < 2)
        {
            /*__DEBUGTOOL_START__*/Debug.LogWarning("ViewPanels 未正确配置");/*__DEBUGTOOL_END__*/
            return;
        }

        for (int i = 0; i < viewPanels.Length; i++)
        {
            viewPanels[i].SetActive(false);
        }

        // 默认开启 View3
        viewPanels[1].SetActive(true);
    }
    #endregion

    #region ================= 视角切换 =================

    void UpdatePanels(ViewMode mode)
    {
        for (int i = 0; i < viewPanels.Length; i++)
        {
            bool isTarget = (i == (int)mode);

            viewPanels[i].SetActive(isTarget);
            viewCameras[i].gameObject.SetActive(isTarget);
        }

        /*if (mode == ViewMode.View1)
            GameEvents.isView1Now();*/

        if (mode == ViewMode.View3)
        {
            // ===== 新增：显示准星 =====
            if (crosshairUI != null)
                crosshairUI.SetActive(true);
        }
        else
        {
            // ===== 新增：非View3隐藏准星 =====
            if (crosshairUI != null)
                crosshairUI.SetActive(false);
        }
    }

    private void BindViewSwitchButtons()
    {
        if (viewSwitchButtons == null || viewSwitchButtons.Length < 3)
        {
            /*__DEBUGTOOL_START__*/Debug.LogWarning("ViewSwitchButtons 未正确配置");/*__DEBUGTOOL_END__*/
            return;
        }

        /*viewSwitchButtons[0].onClick.AddListener(() => OnViewButtonClick(ViewMode.View1));*/
        viewSwitchButtons[1].onClick.AddListener(() => OnViewButtonClick(ViewMode.View2));
        viewSwitchButtons[2].onClick.AddListener(() => OnViewButtonClick(ViewMode.View3));
    }

    private void OnViewButtonClick(ViewMode targetMode)
    {

        // 发送视角切换请求
        GameEvents.onDirectViewSwitchRequest(targetMode);
    }

    #endregion

    #region ================= 箭头按钮 =================

    public void BindArrowsButtons()
    {
        if (arrowsButtons == null || arrowsButtons.Count < 3)
        {
            /*__DEBUGTOOL_START__*/Debug.LogWarning("箭头按钮未正确初始化");/*__DEBUGTOOL_END__*/
            return;
        }

        for (int i = 0; i < arrowsButtons.Count; i++)
        {
            int index = i; // 防闭包问题

            arrowsButtons[i].onClick.AddListener(() => OnArrowsButtonClick(index));
        }
    }

    public void AddArrowButton(Button button)
    {
        arrowsButtons.Add(button);
    }

    private void OnArrowsButtonClick(int num)
    {

        // 发送箭头点击事件
        GameEvents.onArrowsClickRequest(num);
    }

    #endregion

    #region ================= 背包系统 =================
    private void OnBackpackButtonClick()
    {
        // UI按钮和Tab键走同一条路径
        GameEvents.onTabRequest();
    }

    private void OnBagOpen()
    {
        if (backpackPanel == null)
        {
        }
        else
        {
            backpackPanel.SetActive(true);
        }
    }

    private void OnBagClose()
    {
        if (backpackPanel != null)
            backpackPanel.SetActive(false);
    }

    #endregion

    #region ================= 工具 =================

    private void DisableAllCameras()
    {
        for (int i = 0; i < viewCameras.Length; i++)
        {
            viewCameras[i].gameObject.SetActive(false);
        }
    }

    #endregion
}