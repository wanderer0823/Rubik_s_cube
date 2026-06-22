using System;
using Unity.Mathematics;
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
    public static event Action OnGameExitRequest;
    public static event Action<Vector2> OnMouseLookRequest;//欧
    // UIM请求VMM监听
    public static event Action<ViewMode> OnDirectViewSwitchRequest;
    public static event Action<int> OnArrowsClickRequest; //张天姿
    // CRC请求VMM监听
    //public static event Action<Vector3> OnBallSpaceUpdateRequest;
    // PIM→GM→VMM：E键交互
    public static event Action OnInteractRequest;
    // PIM→GM→VMM：滚轮
    public static event Action<float> OnScrollRequest;
    // BackpackUI→VMM：材质切换
    public static event Action<PlayerMatState> OnMatChangeRequest;
    #endregion

    #region === 执行事件定义 ===
    // PC监听VMM
    public static event Action OnTabExecute;
    public static event Action<Vector3> OnMoveExecute;
    public static event Action OnOpenDoorExecute;
    public static event Action<Vector2> OnMouseLookExecute;//欧
    // CRC监听VMM
    public static event Action OnLeftMouseDragCompletedExecute;
    public static event Action OnRightMouseDragCompletedExecute;
    public static event Action OnCubeRotateExecute;
    public static event Action OnCubeRotateFinishExecute;
    public static event Action OnCubeRotateSettledExecute;
    // CARC监听VMM
    public static event Action OnCameraRotateExecute;
    public static event Action OnCameraRotateFinishExecute;
    // UIM+VMM监听GS
    public static event Action<ViewMode> OnViewSwitchExecute;
    // 张天姿：CTC监听VMM
    public static event Action<int> OnArrowsExecute;
    // V1CM监听UIM
    public static event Action IsView1Now;
    // RPC监听UIM和PA
    public static event Action CalculateNeighbors;
    // ===== 新增执行事件 =====
    // UIManager监听VMM：背包开关
    public static event Action OnBagOpenExecute;
    public static event Action OnBagCloseExecute;
    // CluePickup/可交互物体 监听VMM：E键交互
    public static event Action OnInteractExecute;
    // BackpackSystem监听VMM：背包滚动
    public static event Action<float> OnBagScrollExecute;
    // GrabSystem监听VMM：举起物体旋转
    public static event Action<float> OnGrabRotateExecute;
    // PlayerAction监听VMM：材质切换
    public static event Action<PlayerMatState> OnMatChangeExecute;
    // TaskSystem/UIManager监听：任务完成
    public static event Action<int> OnTaskFinished;
    // UIManager监听：通关
    public static event Action OnGameWin;
    // 过门成功，小球需要移动
    public static event Action<int> OnRoomTransitionExecute;
    // StartUI监听VMM：eac重新打开开始界面
    public static event Action OnGameStartExecute;
    public static event Action OnBackStartUI;
    // CA监听PA，相机走路晃动
    public static event Action<Vector3> OnWalkMovement;
    public static event Action OnStopMovement;
    #endregion

    #region === 请求事件广播方法 ===
    // PIM请求GM，VMM监听
    public static void onTabRequest() => OnTabRequest?.Invoke();
    public static void onViewSwitchRequest() => OnViewSwitchRequest?.Invoke();
    public static void onMoveRequest(Vector3 moveDir) => OnMoveRequest?.Invoke(moveDir);
    public static void onOpenDoorRequest() => OnOpenDoorRequest?.Invoke();
    public static void onRotateRequest(RotateType type) => OnRotateRequest?.Invoke(type);
    public static void onRotateFinishRequest(RotateType type) => OnRotateFinishRequest ?.Invoke(type);
    public static void onGameExitRequest() => OnGameExitRequest?.Invoke();
    public static void onMouseLookRequest(Vector2 mouseMove)=>OnMouseLookRequest?.Invoke(mouseMove);//欧
    // UIM请求VMM监听
    public static void onDirectViewSwitchRequest(ViewMode mode) => OnDirectViewSwitchRequest?.Invoke(mode);

    public static void onArrowsClickRequest(int number) => OnArrowsClickRequest?.Invoke(number);//张天姿
    // CRC请求VMM监听
    //public static void onBallSpaceUpdateRequest(Vector3 ballPos)=> OnBallSpaceUpdateRequest?.Invoke(ballPos);
    // ===== 新增请求广播 =====
    public static void onInteractRequest() => OnInteractRequest?.Invoke();
    public static void onScrollRequest(float delta) => OnScrollRequest?.Invoke(delta);
    public static void onMatChangeRequest(PlayerMatState s) => OnMatChangeRequest?.Invoke(s);
    #endregion

    #region === 执行事件广播方法 ===
    // PC监听VMM
    public static void onTabExecute() => OnTabExecute?.Invoke();
    public static void onMoveExecute(Vector3 moveDir) => OnMoveExecute?.Invoke(moveDir);
    public static void onOpenDoorExecute() => OnOpenDoorExecute?.Invoke();
    public static void onMouseLookExecute(Vector2 mouseMove) => OnMouseLookExecute?.Invoke(mouseMove);//欧
    // UIM+VMM监听GS
    public static void onLeftMouseDragCompletedExecute() => OnLeftMouseDragCompletedExecute?.Invoke();
    public static void onRightMouseDragCompletedExecute() => OnRightMouseDragCompletedExecute?.Invoke();
    public static void onViewSwitchExecute(ViewMode mode) => OnViewSwitchExecute?.Invoke(mode);
    // CRC监听VMM
    public static void onCubeRotateStart() => OnCubeRotateExecute?.Invoke();
    public static void onCubeRotateEnd()=> OnCubeRotateFinishExecute?.Invoke();
    public static void onCubeRotateSettled() => OnCubeRotateSettledExecute?.Invoke();
    // CARC监听VMM
    public static void onCameraRotateStart() => OnCameraRotateExecute?.Invoke();
    public static void onCameraRotateEnd() => OnCameraRotateFinishExecute?.Invoke();
    // 张天姿：CTC监听VMM
    public static void onArrowsExecute(int number) => OnArrowsExecute?.Invoke(number);
    // V1CM监听UIM
    public static void isView1Now() => IsView1Now?.Invoke();
    // RPC监听UIM和PA
    public static void calculateNeighbors() =>CalculateNeighbors?.Invoke();
    // StartUI监听VMM
    public static void onGameStartExecute() => OnGameStartExecute?.Invoke();
    public static void onBackStartUI() => OnBackStartUI?.Invoke();
    // ===== 新增执行广播 =====
    public static void onBagOpenExecute() => OnBagOpenExecute?.Invoke();
    public static void onBagCloseExecute() => OnBagCloseExecute?.Invoke();
    public static void onInteractExecute() => OnInteractExecute?.Invoke();
    public static void onBagScrollExecute(float d) => OnBagScrollExecute?.Invoke(d);
    public static void onGrabRotateExecute(float d) => OnGrabRotateExecute?.Invoke(d);
    public static void onMatChangeExecute(PlayerMatState s) => OnMatChangeExecute?.Invoke(s);
    public static void onTaskFinished(int i) => OnTaskFinished?.Invoke(i);
    public static void onGameWin() => OnGameWin?.Invoke();
    public static void onRoomTransitionExecute(int newRoomID) => OnRoomTransitionExecute?.Invoke(newRoomID);

    public static void onWalkMovement(Vector3 v) => OnWalkMovement?.Invoke(v);
    public static void onStopMovement() => OnStopMovement?.Invoke();
    #endregion
}
