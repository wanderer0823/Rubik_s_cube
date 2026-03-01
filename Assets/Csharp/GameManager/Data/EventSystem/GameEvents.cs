using System;
using UnityEngine;

public static class GameEvents
{
    #region === 请求事件定义 ===
    // GM请求VMM监听
    public static event Action OnTabRequest;
    public static event Action OnViewSwitchRequest;
    public static event Action<Vector3> OnMoveRequest;
    public static event Action OnOpenDoorRequest;
    public static event Action<RotateType> OnRotateRequest;
    public static event Action<RotateType> OnRotateFinishRequest;
    public static event Action<Vector2> OnMouseLookRequest;//欧
    // UIM请求VMM监听
    public static event Action<ViewMode> OnDirectViewSwitchRequest;
    public static event Action<int> OnArrowsClickRequest; //张天姿
    // CRC请求VMM监听
    public static event Action<Vector3> OnBallSpaceUpdateRequest;
    #endregion

    #region === 执行事件定义 ===
    // PC监听VMM
    public static event Action OnTabExecute;
    public static event Action<Vector3> OnMoveExecute;
    public static event Action OnOpenDoorExecute;
    public static event Action<Vector2> OnMouseLookExecute;//欧
    // CRC监听VMM
    public static event Action OnCubeRotateExecute;
    public static event Action OnCubeRotateFinishExecute;
    // CARC监听VMM
    public static event Action OnCameraRotateExecute;
    public static event Action OnCameraRotateFinishExecute;
    // UIM监听VMM
    public static event Action<ViewMode> OnViewSwitchExecute;
    // 张天姿：CTC监听VMM
    public static event Action<int> OnArrowsExecute;
    #endregion

    #region === 请求事件广播方法 ===
    // PIM请求GM，VMM监听
    public static void onTabRequest() => OnTabRequest?.Invoke();
    public static void onViewSwitchRequest() => OnViewSwitchRequest?.Invoke();
    public static void onMoveRequest(Vector3 moveDir) => OnMoveRequest?.Invoke(moveDir);
    public static void onOpenDoorRequest() => OnOpenDoorRequest?.Invoke();
    public static void onRotateRequest(RotateType type) => OnRotateRequest?.Invoke(type);
    public static void onRotateFinishRequest(RotateType type) => OnRotateFinishRequest ?.Invoke(type);
    public static void onMouseLookRequest(Vector2 mouseMove)=>OnMouseLookRequest?.Invoke(mouseMove);//欧
    // UIM请求VMM监听
    public static void onDirectViewSwitchRequest(ViewMode mode) => OnDirectViewSwitchRequest?.Invoke(mode);

    public static void onArrowsClickRequest(int number) => OnArrowsClickRequest?.Invoke(number);//张天姿
    // CRC请求VMM监听
    public static void onBallSpaceUpdateRequest(Vector3 ballPos)=> OnBallSpaceUpdateRequest?.Invoke(ballPos);
    #endregion

    #region === 执行事件广播方法 ===
    // PC监听VMM
    public static void onTabExecute() => OnTabExecute?.Invoke();
    public static void onMoveExecute(Vector3 moveDir) => OnMoveExecute?.Invoke(moveDir);
    public static void onOpenDoorExecute() => OnOpenDoorExecute?.Invoke();
    public static void onMouseLookExecute(Vector2 mouseMove) => OnMouseLookExecute?.Invoke(mouseMove);//欧
    // UIM监听VMM
    public static void onViewSwitchExecute(ViewMode mode) => OnViewSwitchExecute?.Invoke(mode);
    // CRC监听VMM
    public static void onCubeRotateStart() => OnCubeRotateExecute?.Invoke();
    public static void onCubeRotateEnd()=> OnCubeRotateFinishExecute?.Invoke();
    // CARC监听VMM
    public static void onCameraRotateStart() => OnCameraRotateExecute?.Invoke();
    public static void onCameraRotateEnd() => OnCameraRotateFinishExecute?.Invoke();
    // 张天姿：CTC监听VMM
    public static void onArrowsExecute(int number) => OnArrowsExecute?.Invoke(number);
    #endregion
}