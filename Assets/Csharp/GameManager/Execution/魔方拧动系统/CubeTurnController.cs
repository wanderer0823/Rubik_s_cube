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
    private Vector3 currentSignedLocalAxis = Vector3.right;

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
        (Vector3 signedAxis, int coordValue) = GetLayerFilter(faceDir, side, index);

        currentCubePiece.Clear();
        currentSignedLocalAxis = signedAxis;
        currentAxis = VectorToAxis(signedAxis);
        currentCubePiece = initCubeSlot.GetPiecesInLayer(currentAxis, coordValue);
    }

    // 根据当前面的屏幕朝向，动态决定按钮对应的轴和层
    public (Vector3 signedAxis, int coordValue) GetLayerFilter(
        InitCubeSlotFaceDir faceDir,
        ArrowSide side,
        int index)
    {
        int val = IndexToCoordValue(Mathf.Clamp(index, 0, 2));
        GetFaceBasis(faceDir, out _, out Vector3 localUp, out Vector3 localRight);

        Vector3 signedAxis = side == ArrowSide.Up ? localRight : localUp;
        int axisSign = GetAxisSign(signedAxis);
        int coordValue = side == ArrowSide.Up
            ? axisSign * val
            : axisSign * -val;

        return (signedAxis, coordValue);
    }

    public void RotateByCurrentArrow(int arrowIndex)
    {
        currentFaceDir = BallLocationService.GetBallFaceDirByWorldPos(ViewModeManager.Instance.ball);

        GetPiecesForArrow(arrowIndex);

        ArrowSide side = arrowIndex < 3 ? ArrowSide.Up : ArrowSide.Left;

        float angle = 90f;
        Transform cubeRoot = ViewModeManager.Instance != null
            ? ViewModeManager.Instance.cubeRoot
            : null;
        Quaternion localRotation = Quaternion.AngleAxis(angle, currentSignedLocalAxis);
        Quaternion worldRotation = cubeRoot != null
            ? cubeRoot.rotation * localRotation * Quaternion.Inverse(cubeRoot.rotation)
            : localRotation;

        bool isCW = GetAxisSign(currentSignedLocalAxis) > 0;

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

    private static int GetAxisSign(Vector3 axis)
    {
        if (Mathf.Abs(axis.x) > 0.5f) return axis.x > 0 ? 1 : -1;
        if (Mathf.Abs(axis.y) > 0.5f) return axis.y > 0 ? 1 : -1;
        return axis.z > 0 ? 1 : -1;
    }

    private static InitCubeSlotAxis VectorToAxis(Vector3 axis)
    {
        if (Mathf.Abs(axis.x) > 0.5f) return InitCubeSlotAxis.X;
        if (Mathf.Abs(axis.y) > 0.5f) return InitCubeSlotAxis.Y;
        return InitCubeSlotAxis.Z;
    }

    private static Vector3 GetFaceNormal(InitCubeSlotFaceDir face)
    {
        return face switch
        {
            InitCubeSlotFaceDir.Up => Vector3.up,
            InitCubeSlotFaceDir.Down => Vector3.down,
            InitCubeSlotFaceDir.Left => Vector3.left,
            InitCubeSlotFaceDir.Right => Vector3.right,
            InitCubeSlotFaceDir.Front => Vector3.forward,
            InitCubeSlotFaceDir.Back => Vector3.back,
            _ => Vector3.forward
        };
    }

    private static Vector3 GetLocalUp(InitCubeSlotFaceDir face)
    {
        return face switch
        {
            InitCubeSlotFaceDir.Up => Vector3.back,
            InitCubeSlotFaceDir.Down => Vector3.forward,
            _ => Vector3.up
        };
    }

    private static void GetFaceBasis(
        InitCubeSlotFaceDir face,
        out Vector3 localNormal,
        out Vector3 localUp,
        out Vector3 localRight)
    {
        localNormal = GetFaceNormal(face);
        localUp = GetLocalUp(face);
        localRight = Vector3.Cross(localUp, -localNormal);
    }

    #endregion
}
