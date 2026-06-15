using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using InitCubeSlotAxis = InitCubeSlot.Axis;
using InitCubeSlotFaceDir = InitCubeSlot.FaceDir;
using ArrowSide = ArrowsButton.ArrowSide;

public class CubeTurnController : MonoBehaviour
{
    [Tooltip("Arrow button prefab")]
    [SerializeField] private GameObject arrowButtonPrefab;

    [SerializeField] private List<RectTransform> ButtonsRectTranform = new List<RectTransform>();
    [SerializeField] private InitCubeSlot initCubeSlot;
    [SerializeField] private Transform view1CameraTransform;
    [SerializeField] private InitCubeSlotFaceDir currentFaceDir = InitCubeSlotFaceDir.Up;
    [SerializeField] private float turnAnimationDuration = 0.5f;

    public List<InitCubeSlot.CubePiece> currentCubePiece = new List<InitCubeSlot.CubePiece>();
    public InitCubeSlotAxis currentAxis;

    private Camera view1Camera;
    private Vector3 currentSignedLocalAxis = Vector3.right;
    private Vector3 lockedLocalUp = Vector3.up;
    private Vector3 lockedLocalRight = Vector3.right;
    private bool isTurnAnimating;
    private bool hasLockedScreenBasis;
    private bool isView1FaceLocked;
    private int coordScale = 2;

    private struct TurnAnimationState
    {
        public InitCubeSlot.CubePiece Piece;
        public Vector3 StartPosition;
        public Vector3 TargetPosition;
        public Quaternion StartRotation;
        public Quaternion TargetRotation;
    }

    private int IndexToCoordValue(int index)
    {
        return (index - 1) * coordScale;
    }

    private void Awake()
    {
        if (initCubeSlot == null)
            initCubeSlot = FindObjectOfType<InitCubeSlot>();

        TryResolveView1CameraTransform();
    }

    private void Start()
    {
        InitArrowsButton();
    }

    private void OnEnable()
    {
        GameEvents.OnArrowsExecute += RotateByCurrentArrow;
        GameEvents.OnViewSwitchExecute += OnViewSwitch;
    }

    private void OnDisable()
    {
        GameEvents.OnArrowsExecute -= RotateByCurrentArrow;
        GameEvents.OnViewSwitchExecute -= OnViewSwitch;
    }

    public void SetCurrentFace(InitCubeSlotFaceDir face) => currentFaceDir = face;

    public void InitArrowsButton()
    {
        if (!arrowButtonPrefab.TryGetComponent<ArrowsButton>(out _))
        {
            Debug.LogError("Missing ArrowsButton component on prefab");
            return;
        }

        int index = 0;

        foreach (RectTransform rect in ButtonsRectTranform)
        {
            GameObject go = Instantiate(arrowButtonPrefab);
            RectTransform goRect = go.GetComponent<RectTransform>();

            goRect.SetParent(transform, false);
            goRect.anchorMin = rect.anchorMin;
            goRect.anchorMax = rect.anchorMax;
            goRect.anchoredPosition = rect.anchoredPosition;
            goRect.sizeDelta = rect.sizeDelta;

            ArrowsButton arrowButton = go.GetComponent<ArrowsButton>();
            Button button = go.GetComponent<Button>();

            UIManager.Instance.AddArrowButton(button);

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

    public (Vector3 signedAxis, int coordValue) GetLayerFilter(
        InitCubeSlotFaceDir faceDir,
        ArrowSide side,
        int index)
    {
        int val = IndexToCoordValue(Mathf.Clamp(index, 0, 2));
        GetCurrentBasis(faceDir, out Vector3 localUp, out Vector3 localRight);

        Vector3 signedAxis = side == ArrowSide.Up ? localRight : localUp;
        int axisSign = GetAxisSign(signedAxis);
        int coordValue = side == ArrowSide.Up
            ? axisSign * val
            : axisSign * -val;

        return (signedAxis, coordValue);
    }

    public void RotateByCurrentArrow(int arrowIndex)
    {
        if (isTurnAnimating)
            return;

        LockCurrentFaceForView1();
        DecomposeArrowIndex(arrowIndex, out ArrowSide side, out int normalizedIndex);
        TryRotateLayer(currentFaceDir, side, normalizedIndex, false);
    }

    public bool TryRotateLayer(
        InitCubeSlotFaceDir faceDir,
        ArrowSide side,
        int index,
        bool reverse)
    {
        if (isTurnAnimating)
            return false;

        GetPiecesForArrowInternal(faceDir, side, index);

        float angle = reverse ? -90f : 90f;
        Transform cubeRoot = ViewModeManager.Instance != null
            ? ViewModeManager.Instance.cubeRoot
            : null;
        Quaternion localRotation = Quaternion.AngleAxis(angle, currentSignedLocalAxis);
        Quaternion worldRotation = cubeRoot != null
            ? cubeRoot.rotation * localRotation * Quaternion.Inverse(cubeRoot.rotation)
            : localRotation;

        bool isCW = angle * GetAxisSign(currentSignedLocalAxis) > 0f;
        List<TurnAnimationState> animationStates = new List<TurnAnimationState>(currentCubePiece.Count);

        foreach (InitCubeSlot.CubePiece piece in currentCubePiece)
        {
            Vector3 targetPosition;

            if (cubeRoot != null)
            {
                Vector3 localPos = cubeRoot.InverseTransformPoint(piece.indexCube.position);
                targetPosition = cubeRoot.TransformPoint(localRotation * localPos);
            }
            else
            {
                targetPosition = localRotation * piece.indexCube.position;
            }

            animationStates.Add(new TurnAnimationState
            {
                Piece = piece,
                StartPosition = piece.indexCube.position,
                TargetPosition = targetPosition,
                StartRotation = piece.indexCube.rotation,
                TargetRotation = worldRotation * piece.indexCube.rotation
            });
        }

        StartCoroutine(AnimateTurn(animationStates, isCW));
        return true;
    }

    private IEnumerator AnimateTurn(List<TurnAnimationState> animationStates, bool isCW)
    {
        isTurnAnimating = true;

        float duration = Mathf.Max(0.01f, turnAnimationDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            foreach (TurnAnimationState state in animationStates)
            {
                state.Piece.indexCube.position = Vector3.LerpUnclamped(state.StartPosition, state.TargetPosition, t);
                state.Piece.indexCube.rotation = Quaternion.Slerp(state.StartRotation, state.TargetRotation, t);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (TurnAnimationState state in animationStates)
        {
            state.Piece.indexCube.position = state.TargetPosition;
            state.Piece.indexCube.rotation = state.TargetRotation;
        }

        ApplyTurnResult(isCW);

        isTurnAnimating = false;
        GameState.Instance.SetPlayerState(PlayerState.turningFinished);
    }

    private void ApplyTurnResult(bool isCW)
    {
        HashSet<int> rotatedRoomIds = new HashSet<int>();

        foreach (InitCubeSlot.CubePiece piece in currentCubePiece)
        {
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

                if (surface.roomID >= 0
                    && surface.roomID < initCubeSlot.rooms.Count
                    && rotatedRoomIds.Add(surface.roomID))
                {
                    initCubeSlot.rooms[surface.roomID]?.RotateDirMap(currentAxis, isCW);
                }
            }
        }

        initCubeSlot.RebuildSurfaceCoordMap();
        GameEvents.calculateNeighbors();
    }

    private void OnViewSwitch(ViewMode mode)
    {
        hasLockedScreenBasis = false;
        isView1FaceLocked = false;
    }

    private void EnsureView1FaceLocked()
    {
        if (isView1FaceLocked)
            return;

        LockCurrentFaceForView1();
    }

    private void LockCurrentFaceForView1()
    {
        GameState.Instance.RefreshCurrentSurfaceFromRoomID();
        currentFaceDir = (InitCubeSlotFaceDir)GameState.Instance.CurrentPlayerFace;
        CacheScreenBasisForView1();
        isView1FaceLocked = true;
    }

    private void GetCurrentBasis(
        InitCubeSlotFaceDir face,
        out Vector3 localUp,
        out Vector3 localRight)
    {
        if (hasLockedScreenBasis)
        {
            localUp = lockedLocalUp;
            localRight = lockedLocalRight;
            return;
        }

        GetFaceBasis(face, out _, out localUp, out localRight);
    }

    private void CacheScreenBasisForView1()
    {
        hasLockedScreenBasis = false;
        TryResolveView1CameraTransform();

        Transform cubeRoot = ViewModeManager.Instance?.cubeRoot;
        if (cubeRoot == null || view1CameraTransform == null)
            return;

        Vector3 cameraLocalUp = cubeRoot.InverseTransformDirection(view1CameraTransform.up);
        Vector3 cameraLocalRight = cubeRoot.InverseTransformDirection(view1CameraTransform.right);

        lockedLocalUp = SnapToPrimaryAxis(cameraLocalUp);
        lockedLocalRight = SnapToPrimaryAxis(cameraLocalRight);

        if (lockedLocalUp == Vector3.zero || lockedLocalRight == Vector3.zero)
            return;

        hasLockedScreenBasis = true;
    }

    private bool TryResolveView1CameraTransform()
    {
        if (view1CameraTransform != null)
        {
            if (view1Camera == null)
                view1Camera = view1CameraTransform.GetComponent<Camera>();
            return true;
        }

        var activeCameraManager = FindObjectOfType<View1CameraManager>();
        if (activeCameraManager != null)
        {
            view1CameraTransform = activeCameraManager.transform;
            view1Camera = activeCameraManager.GetComponent<Camera>();
            return true;
        }

        foreach (var candidate in Resources.FindObjectsOfTypeAll<View1CameraManager>())
        {
            if (!candidate.gameObject.scene.IsValid())
                continue;

            view1CameraTransform = candidate.transform;
            view1Camera = candidate.GetComponent<Camera>();
            return true;
        }

        return false;
    }

    private void DecomposeArrowIndex(int arrowIndex, out ArrowSide side, out int normalizedIndex)
    {
        if (arrowIndex < 3)
        {
            side = ArrowSide.Up;
            normalizedIndex = arrowIndex;
        }
        else
        {
            side = ArrowSide.Left;
            normalizedIndex = arrowIndex - 3;
        }
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
        localRight = Vector3.Cross(-localNormal, localUp);
    }

    private static Vector3 SnapToPrimaryAxis(Vector3 v)
    {
        float absX = Mathf.Abs(v.x);
        float absY = Mathf.Abs(v.y);
        float absZ = Mathf.Abs(v.z);

        if (absX >= absY && absX >= absZ)
            return v.x >= 0f ? Vector3.right : Vector3.left;

        if (absY >= absX && absY >= absZ)
            return v.y >= 0f ? Vector3.up : Vector3.down;

        return v.z >= 0f ? Vector3.forward : Vector3.back;
    }
}
