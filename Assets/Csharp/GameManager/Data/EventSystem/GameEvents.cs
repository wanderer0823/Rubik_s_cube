using System;
using UnityEngine;

public static class GameEvents
{
    #region === 请求事件定义 ===
    // PIM请求GM，VMM监听
    public static event Action OnTabRequest;
    public static event Action OnViewSwitchRequest;
    public static event Action OnMoveRequest;
    public static event Action OnOpenDoorRequest;
    public static event Action<RotateType> OnRotateRequest;
    public static event Action<RotateType> OnRotateFinishRequest;
    // UIM请求VMM监听
    public static event Action<ViewMode> OnDirectViewSwitchRequest;
    #endregion

    #region === 执行事件定义 ===
    // PC监听VMM
    public static event Action OnTabExecute;
    public static event Action OnMoveExecute;
    public static event Action OnOpenDoorExecute;
    // CRC监听VMM
    public static event Action OnCubeRotateExecute;
    public static event Action OnCubeRotateFinishExecute;
    // CARC监听VMM
    public static event Action OnCameraRotateExecute;
    public static event Action OnCameraRotateFinishExecute;
    // UIM监听VMM
    public static event Action<ViewMode> OnViewSwitchExecute;
    #endregion

    #region === 请求事件广播方法 ===
    // PIM请求GM，VMM监听
    public static void onTabRequest() => OnTabRequest?.Invoke();
    public static void onViewSwitchRequest() => OnViewSwitchRequest?.Invoke();
    public static void onMoveRequest() => OnMoveRequest?.Invoke();
    public static void onOpenDoorRequest() => OnOpenDoorRequest?.Invoke();
    public static void onRotateRequest(RotateType type) => OnRotateRequest?.Invoke(type);
    public static void onRotateFinishRequest(RotateType type) => OnRotateFinishRequest ?.Invoke(type);
    // UIM请求VMM监听
    public static void onDirectViewSwitchRequest(ViewMode mode) => OnDirectViewSwitchRequest?.Invoke(mode);
    #endregion

    #region === 执行事件广播方法 ===
    // PC监听VMM
    public static void onTabExecute() => OnTabExecute?.Invoke();
    public static void onMoveExecute() => OnMoveExecute?.Invoke();
    public static void onOpenDoorExecute() => OnOpenDoorExecute?.Invoke();
    // UIM监听VMM
    public static void onViewSwitchExecute(ViewMode mode) => OnViewSwitchExecute?.Invoke(mode);
    // CRC监听VMM
    public static void onCubeRotateStart() => OnCubeRotateExecute?.Invoke();
    public static void onCubeRotateEnd()=> OnCubeRotateFinishExecute?.Invoke();
    // CARC监听VMM
    public static void onCameraRotateStart() => OnCameraRotateExecute?.Invoke();
    public static void onCameraRotateEnd() => OnCameraRotateFinishExecute?.Invoke();

    #endregion
}