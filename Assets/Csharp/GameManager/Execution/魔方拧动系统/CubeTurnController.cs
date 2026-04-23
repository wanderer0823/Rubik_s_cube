using System;
using System.Collections.Generic;
using UnityEngine;
using InitCubeSlotAxis = InitCubeSlot.Axis;
using InitCubeSlotFaceDir = InitCubeSlot.FaceDir;
using ArrowSide = ArrowsButton.ArrowSide;
using UnityEngine.UI;

public class CubeTurnController : MonoBehaviour
{
    // 箭头按钮容器  
    [Tooltip("箭头预制体")]
    [SerializeField] private GameObject arrowButtonPrefab;
    [SerializeField] private List<RectTransform> ButtonsRectTranform = new List<RectTransform>();
    // 旋转数据  
    [SerializeField] private InitCubeSlot initCubeSlot;
    [SerializeField] private InitCubeSlotFaceDir currentFaceDir = InitCubeSlotFaceDir.Up;
    public List<InitCubeSlot.CubePiece> currentCubePiece = new List<InitCubeSlot.CubePiece>();
    public InitCubeSlotAxis currentAxis;

    // 逻辑坐标步长为2  
    private int coordScale = 2;

    // index 0/1/2 → coord 值 -2, 0, +2
    int IndexToCoordValue(int index)
    {
        return (index - 1) * coordScale;  // 0→-2, 1→0, 2→+2
    }

    private void Awake()
    {
        if (initCubeSlot == null)
            initCubeSlot = FindObjectOfType<InitCubeSlot>();
    }

    void Start()
    {
        InitArrowsButton();
    }

    private void OnEnable()
    {
        GameEvents.OnArrowsExecute += RotateByCurrentArrow;
    }
    private void OnDisable()
    {
        GameEvents.OnArrowsExecute -= RotateByCurrentArrow;
    }

    public void SetCurrentFace(InitCubeSlotFaceDir face) => currentFaceDir = face;

    public void InitArrowsButton()
    {
        if (!arrowButtonPrefab.TryGetComponent<ArrowsButton>(out var button))
        {
            Debug.LogError("预制体缺少ArrowsButton脚本");
            return;
        }
        int index = 0;
        foreach (RectTransform rect in ButtonsRectTranform)
        {
            GameObject go = Instantiate(arrowButtonPrefab, rect);
            ArrowsButton arrowButton = go.GetComponent<ArrowsButton>();
            Button b = go.GetComponent<Button>();
            UIManager.Instance.AddArrowButton(b);
            if (index < 3) arrowButton.SetArrowSide(ArrowSide.Up);
            else arrowButton.SetArrowSide(ArrowSide.Left);
            arrowButton.SetArrowIndex(index++);
        }
        UIManager.Instance.BindArrowsButtons();
    }

    #region 获取层级方块并旋转

    // 根据当前朝向面和箭头序号，筛选出对应一层的立方体。  
    public void GetPiecesForArrow(int index)
    {
        var face = currentFaceDir;
        ArrowSide s;
        int normalizedIndex;
        if (index < 3)
        {
            s = ArrowSide.Up;
            normalizedIndex = index;        // Up 侧：0/1/2 直接使用
        }
        else
        {
            s = ArrowSide.Left;
            normalizedIndex = index - 3;    // Left 侧：3/4/5 → 0/1/2
        }

        GetPiecesForArrowInternal(face, s, normalizedIndex);
    }

    private void GetPiecesForArrowInternal(
        InitCubeSlotFaceDir faceDir,
        ArrowSide side,
        int index)
    {
        (InitCubeSlotAxis axis, int coordValue) = GetLayerFilter(faceDir, side, index);
        currentCubePiece.Clear();
        currentAxis = axis;
        currentCubePiece = initCubeSlot.GetPiecesInLayer(axis, coordValue);
    }

    // 根据朝向面和箭头位置，得到筛选条件(轴, 坐标值)。  
    // 例如 face=Front, side=Up, index=0 → (X, -2) 表示最左列。  
    public (InitCubeSlotAxis axis, int coordValue) GetLayerFilter(
        InitCubeSlotFaceDir faceDir,
        ArrowSide side,
        int index)
    {
        int val = IndexToCoordValue(Mathf.Clamp(index, 0, 2));

        // Up  侧箭头 = 面上边缘那排，控制列方向
        // Left 侧箭头 = 面左边缘那排，控制行方向
        switch (faceDir)
        {
            case InitCubeSlotFaceDir.Up:    // Y+ 面，平面为 XZ
                return side == ArrowSide.Up
                    ? (InitCubeSlotAxis.X, val)   // Up 侧箭头 → 选 X 列
                    : (InitCubeSlotAxis.Z, val);  // Left 侧箭头 → 选 Z 行

            case InitCubeSlotFaceDir.Down:  // Y- 面，平面为 XZ
                return side == ArrowSide.Up
                    ? (InitCubeSlotAxis.X, val)
                    : (InitCubeSlotAxis.Z, -val);

            case InitCubeSlotFaceDir.Left:  // X- 面，平面为 YZ
                return side == ArrowSide.Up
                    ? (InitCubeSlotAxis.Z, val)
                    : (InitCubeSlotAxis.Y, val);

            case InitCubeSlotFaceDir.Right: // X+ 面，平面为 YZ
                return side == ArrowSide.Up
                    ? (InitCubeSlotAxis.Z, -val)
                    : (InitCubeSlotAxis.Y, val);

            case InitCubeSlotFaceDir.Front: // Z+ 面，平面为 XY
                return side == ArrowSide.Up
                    ? (InitCubeSlotAxis.X, val)
                    : (InitCubeSlotAxis.Y, val);

            case InitCubeSlotFaceDir.Back:  // Z- 面，平面为 XY
                return side == ArrowSide.Up
                    ? (InitCubeSlotAxis.X, -val)
                    : (InitCubeSlotAxis.Y, val);
        }
        return (InitCubeSlotAxis.X, val);
    }

    public void RotateByCurrentArrow(int arrowIndex)
    {
        currentFaceDir = BallLocationService.GetBallFaceDirByWorldPos(ViewModeManager.Instance.ball);
        GetPiecesForArrow(arrowIndex);

        // Axis 映射为旋转轴方向向量（世界空间单位向量）
        Vector3 axisVec = AxisToVector(currentAxis);

        // 根据箭头方向和所在面决定旋转角度：
        //   Up 侧（index 0~2）：箭头朝↑，对应 +90°（列向上走）
        //   Left 侧（index 3~5）：箭头朝←，对应 -90°（行向左走）
        //   例外：Down 面因相机视角镜像，Left 侧改为 +90°
        ArrowSide side = arrowIndex < 3 ? ArrowSide.Up : ArrowSide.Left;
        float angle = GetRotationAngle(currentFaceDir, side);
        Quaternion rotation = Quaternion.AngleAxis(angle, axisVec);
        bool isCW = angle > 0f;

        foreach (InitCubeSlot.CubePiece piece in currentCubePiece)
        {
            // TODO: 添加 DOTween 动画
            // position 绕魔方中心（原点）公转
            piece.indexCube.position = rotation * piece.indexCube.position;
            // 世界空间旋转放左边，避免本地坐标轴漂移
            piece.indexCube.rotation = rotation * piece.indexCube.rotation;

            // 更新逻辑坐标（与旋转角度方向一致）
            Vector3Int coord = piece.coord;
            if (isCW)
            {
                switch (currentAxis)
                {
                    case InitCubeSlot.Axis.X:
                        piece.coord = new Vector3Int(coord.x, -coord.z, coord.y);
                        break;
                    case InitCubeSlot.Axis.Y:
                        piece.coord = new Vector3Int(coord.z, coord.y, -coord.x);
                        break;
                    case InitCubeSlot.Axis.Z:
                        piece.coord = new Vector3Int(-coord.y, coord.x, coord.z);
                        break;
                }
            }
            else
            {
                // -90° = CW 的逆变换
                switch (currentAxis)
                {
                    case InitCubeSlot.Axis.X:
                        piece.coord = new Vector3Int(coord.x, coord.z, -coord.y);
                        break;
                    case InitCubeSlot.Axis.Y:
                        piece.coord = new Vector3Int(-coord.z, coord.y, coord.x);
                        break;
                    case InitCubeSlot.Axis.Z:
                        piece.coord = new Vector3Int(coord.y, -coord.x, coord.z);
                        break;
                }
            }

            // 用 DirRotator 同步更新每个面的 dir，并重新计算面坐标
            foreach (var surface in piece.surfaces)
            {
                surface.dir = DirRotator.Rotate(surface.dir, currentAxis, isCW);
                surface.UpdatePosition(piece.coord);
            }
        }

        // 重建 surfaceCoordMap，确保新坐标可被 BallLocationService 正确查询
        initCubeSlot.RebuildSurfaceCoordMap();
        // 邻居房间重算不在此处触发：玩家切换到 View3 时 UIManager 会自动广播 calculateNeighbors

        // 旋转完成后将状态改回 turningFinished，解除操作锁定
        GameState.Instance.SetPlayerState(PlayerState.turningFinished);
        Debug.Log("CTC: 拧动完成，状态已恢复为 turningFinished");
    }

    /// <summary>
    /// 根据所在面和箭头方向决定旋转角度：
    /// Up 侧：+90°（列向上）
    /// Left 侧：-90°（行向左），Down 面例外用 +90°（镜像翻转）
    /// </summary>
    private float GetRotationAngle(InitCubeSlotFaceDir face, ArrowSide side)
    {
        if (side == ArrowSide.Up)
            return 90f;

        // Left 侧：Down 面镜像，其余一律 -90°
        if (face == InitCubeSlotFaceDir.Down)
            return 90f;

        return -90f;
    }

    // Axis -> 世界空间单位方向向量，用于 Quaternion.AngleAxis
    private static Vector3 AxisToVector(InitCubeSlotAxis axis)
    {
        return axis switch
        {
            InitCubeSlotAxis.X => Vector3.right,   // (1, 0, 0)
            InitCubeSlotAxis.Y => Vector3.up,      // (0, 1, 0)
            InitCubeSlotAxis.Z => Vector3.forward, // (0, 0, 1)
            _ => Vector3.up
        };
    }

    #endregion
}
