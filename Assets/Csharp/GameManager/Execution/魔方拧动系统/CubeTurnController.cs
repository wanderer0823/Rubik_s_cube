using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using InitCubeSlotAxis = InitCubeSlot.Axis;
using InitCubeSlotFaceDir = InitCubeSlot.FaceDir;
using ArrowSide = ArrowsButton.ArrowSide;

public class CubeTurnController : MonoBehaviour
{
    // 箭头按钮相关
    [Tooltip("箭头按钮预制体")]
    [SerializeField] private GameObject arrowButtonPrefab;

    [SerializeField] private List<RectTransform> ButtonsRectTranform = new List<RectTransform>();

    // 魔方数据
    [SerializeField] private InitCubeSlot initCubeSlot;
    [SerializeField] private InitCubeSlotFaceDir currentFaceDir = InitCubeSlotFaceDir.Up;

    public List<InitCubeSlot.CubePiece> currentCubePiece = new List<InitCubeSlot.CubePiece>();
    public InitCubeSlotAxis currentAxis;

    // 坐标缩放（逻辑坐标：-2 / 0 / 2）
    private int coordScale = 2;

    // index 0/1/2 → -2 / 0 / +2
    int IndexToCoordValue(int index)
    {
        return (index - 1) * coordScale;
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
            Debug.LogError("预制体缺少 ArrowsButton 脚本");
            return;
        }

        int index = 0;

        foreach (RectTransform rect in ButtonsRectTranform)
        {
            GameObject go = Instantiate(arrowButtonPrefab);
            RectTransform goRect = go.GetComponent<RectTransform>();

            // 挂到当前脚本物体下
            goRect.SetParent(transform, false);

            // 对齐到目标 rect
            goRect.anchorMin = rect.anchorMin;
            goRect.anchorMax = rect.anchorMax;
            goRect.anchoredPosition = rect.anchoredPosition;
            goRect.sizeDelta = rect.sizeDelta;

            ArrowsButton arrowButton = go.GetComponent<ArrowsButton>();
            Button b = go.GetComponent<Button>();

            UIManager.Instance.AddArrowButton(b);

            if (index < 3)
            {
                arrowButton.SetArrowSide(ArrowSide.Up);
                arrowButton.SetArrowIndex(index++);
            }
            else
            {
                arrowButton.SetArrowSide(ArrowSide.Left);
                int overindex = (index++) % 3;
                arrowButton.SetArrowIndex(overindex);
            }
        }

        UIManager.Instance.BindArrowsButtons();
    }

    #region 获取层并执行旋转

    // 根据箭头索引筛选对应层
    public void GetPiecesForArrow(int index)
    {
        var face = currentFaceDir;

        ArrowSide side;
        int normalizedIndex;

        if (index < 3)
        {
            side = ArrowSide.Up;
            normalizedIndex = index;
        }
        else
        {
            side = ArrowSide.Left;
            normalizedIndex = index - 3;
        }

        GetPiecesForArrowInternal(face, side, normalizedIndex);
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

    // 根据当前面和箭头位置，确定筛选层（轴 + 坐标）
    public (InitCubeSlotAxis axis, int coordValue) GetLayerFilter(
        InitCubeSlotFaceDir faceDir,
        ArrowSide side,
        int index)
    {
        int val = IndexToCoordValue(Mathf.Clamp(index, 0, 2));

        switch (faceDir)
        {
            case InitCubeSlotFaceDir.Up:
                return side == ArrowSide.Up
                    ? (InitCubeSlotAxis.X, val)
                    : (InitCubeSlotAxis.Z, val);

            case InitCubeSlotFaceDir.Down:
                return side == ArrowSide.Up
                    ? (InitCubeSlotAxis.X, val)
                    : (InitCubeSlotAxis.Z, -val);

            case InitCubeSlotFaceDir.Left:
                return side == ArrowSide.Up
                    ? (InitCubeSlotAxis.Z, val)
                    : (InitCubeSlotAxis.Y, val);

            case InitCubeSlotFaceDir.Right:
                return side == ArrowSide.Up
                    ? (InitCubeSlotAxis.Z, -val)
                    : (InitCubeSlotAxis.Y, val);

            case InitCubeSlotFaceDir.Front:
                return side == ArrowSide.Up
                    ? (InitCubeSlotAxis.X, val)
                    : (InitCubeSlotAxis.Y, val);

            case InitCubeSlotFaceDir.Back:
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

        ArrowSide side = arrowIndex < 3 ? ArrowSide.Up : ArrowSide.Left;

        float angle = GetRotationAngle(currentFaceDir, side);
        Transform cubeRoot = ViewModeManager.Instance != null
            ? ViewModeManager.Instance.cubeRoot
            : null;
        Vector3 localAxis = AxisToVector(currentAxis);
        Quaternion localRotation = Quaternion.AngleAxis(angle, localAxis);
        Quaternion worldRotation = cubeRoot != null
            ? cubeRoot.rotation * localRotation * Quaternion.Inverse(cubeRoot.rotation)
            : localRotation;

        bool isCW = angle > 0f;

        foreach (InitCubeSlot.CubePiece piece in currentCubePiece)
        {
            if (cubeRoot != null)
            {
                Vector3 localPos = cubeRoot.InverseTransformPoint(piece.indexCube.position);
                piece.indexCube.position = cubeRoot.TransformPoint(localRotation * localPos);
            }
            else
            {
                piece.indexCube.position = localRotation * piece.indexCube.position;
            }

            piece.indexCube.rotation = worldRotation * piece.indexCube.rotation;

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

            foreach (var surface in piece.surfaces)
            {
                surface.dir = DirRotator.Rotate(surface.dir, currentAxis, isCW);
                surface.UpdatePosition(piece.coord);
            }
        }

        initCubeSlot.RebuildSurfaceCoordMap();

        GameState.Instance.SetPlayerState(PlayerState.turningFinished);

        Debug.Log("CubeTurnController：旋转完成");
    }

    private float GetRotationAngle(InitCubeSlotFaceDir face, ArrowSide side)
    {
        if (side == ArrowSide.Up)
            return 90f;

        if (face == InitCubeSlotFaceDir.Down)
            return 90f;

        return -90f;
    }

    private static Vector3 AxisToVector(InitCubeSlotAxis axis)
    {
        return axis switch
        {
            InitCubeSlotAxis.X => Vector3.right,
            InitCubeSlotAxis.Y => Vector3.up,
            InitCubeSlotAxis.Z => Vector3.forward,
            _ => Vector3.up
        };
    }

    #endregion
}
