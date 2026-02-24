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
    // UIM请求GM，VMM监听
    public static event Action OnDirectViewSwitchRequest;
    #endregion

    #region === 执行事件定义 ===
    // PC监听VMM
    public static event Action OnTabExecute;
    public static event Action OnMoveExecute;
    public static event Action<ViewMode> OnViewSwitchExecute;
    public static event Action OnOpenDoorExecute;
    public static event Action<RotateType> OnRotateExecute;
    // VSM监听VMM
    public static event Action OnDirectViewSwitchExecute;
    #endregion

    #region === 请求事件广播方法 ===
    // PIM请求GM，VMM监听
    public static void onTabRequest() => OnTabRequest?.Invoke();
    public static void onViewSwitchRequest() => OnViewSwitchRequest?.Invoke();
    public static void onMoveRequest() => OnMoveRequest?.Invoke();
    public static void onOpenDoorRequest() => OnOpenDoorRequest?.Invoke();
    public static void onRotateRequest(RotateType type) => OnRotateRequest?.Invoke(type);
    #endregion

    #region === 执行事件广播方法 ===
    // PC监听VMM
    public static void onTabExecute() => OnTabExecute?.Invoke();
    public static void onMoveExecute() => OnMoveExecute?.Invoke();
    public static void onViewSwitchExecute(ViewMode mode) => OnViewSwitchExecute?.Invoke(mode);
    public static void onOpenDoorExecute() => OnOpenDoorExecute?.Invoke();
    public static void onRotateExecute(RotateType type) => OnRotateExecute?.Invoke(type);
    #endregion
}