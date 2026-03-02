using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [Header("视角转换按钮")]
    [SerializeField] private Button[] viewSwitchButtons;
    [Header("拧魔方箭头按钮")]
    [SerializeField] private List<Button> arrowsButtons;
    [Header("View面板（空物体）")]
    [SerializeField] private GameObject[] viewPanels;
    [Header("每个视角的相机")]
    [SerializeField] private Camera[] viewCameras;

    void Awake()
    {
        //初始化相机状态
        viewCameras[2].gameObject.SetActive(true);
        //初始化只有View3 Panel
        InitView3Panel();
        //为视角切换按钮添加点击广播
        BindViewSwitchButtons();
        //为箭头拧动按钮添加点击广播，已修改：迁移至初始化后执行
        // BindArrowsButtons();
        Instance = this;
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
    #region 初始化面板View3 Panel
    void InitView3Panel()
    {
        if (viewPanels == null || viewPanels.Length < 3)
        {
            Debug.LogWarning("视角面板数量不足！");
            return;
        }
        for (int i=0;i<viewPanels.Length;i++)
        {
            viewPanels[i].gameObject.SetActive(false);
        }
        viewPanels[2].gameObject.SetActive(true);
    }
    #endregion
    #endregion

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
    #region 张天姿：箭头按钮点击事件监听
    public void BindArrowsButtons()
    {
        if (arrowsButtons == null || arrowsButtons.Count < 3)
        {
            Debug.LogWarning("箭头按钮数量不足！");
            return;
        }
        for(int i=0;i<arrowsButtons.Count;i++)
        {
            int index = i;
            arrowsButtons[i].onClick.AddListener(() => OnArrowsButtonClick(index));
        }
    }

    public void AddArrowButton(Button button)
    {
        arrowsButtons.Add(button);
    }
    
    private void OnArrowsButtonClick(int num)
    {
        Debug.Log($"箭头按钮{num}请求点击");

        // 向逻辑层发请求
        GameEvents.onArrowsClickRequest(num);
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