using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

using InitCubeSlotFaceDir = InitCubeSlot.FaceDir;
using InitCubeSlotAxis = InitCubeSlot.Axis;
using ArrowSide = ArrowsButton.ArrowSide;
using System.Net.NetworkInformation;

public class CubeTurnController1 : MonoBehaviour
{
    [SerializeField] private InitCubeSlot initCubeSlot;
    [SerializeField] private float doubleClickThreshold = 0.25f;
    [SerializeField] private float clickThresholdPixels = 8f;
    [SerializeField] private float turnAnimationDuration = 0.5f;

    private Transform view2CameraTransform;
    private Camera view2Camera;
    private Vector3 currentSignedLocalAxis = Vector3.right;
    private Vector3 lockedLocalUp = Vector3.up;
    private Vector3 lockedLocalRight = Vector3.right;
    private bool isTrackingClick;
    private bool isTurnAnimating;
    private Vector3 clickStartMousePos;
    private float lastClickTime;
    private InitCubeSlot.CubePiece lastClickedPiece;
    private InitCubeSlotFaceDir lastClickedFace;
    private View2LayerSelectionMode selectedLayerMode;
    private InitCubeSlotFaceDir selectedFace;
    private ArrowSide selectedArrowSide;
    private int selectedLayerIndex;
    private const int CoordScale = 2;
    public List<InitCubeSlot.CubePiece> currentCubePiece = new List<InitCubeSlot.CubePiece>();
    public InitCubeSlotAxis currentAxis;

    //欧：材质替换
    public Material M_suliao;
    public Material M_Outline;

    private enum View2LayerSelectionMode
    {
        None,
        Vertical,
        Horizontal
    }

    private struct TurnAnimationState
    {
        public InitCubeSlot.CubePiece Piece;
        public Vector3 StartPosition;
        public Vector3 TargetPosition;
        public Quaternion StartRotation;
        public Quaternion TargetRotation;
    }

    private void Awake()
    {
        if (initCubeSlot == null)
            initCubeSlot = FindObjectOfType<InitCubeSlot>();
        TryResolveView2CameraTransform();
    }

    private void OnDisable()
    {
        ClearSelection();
    }

    private void Update()
    {
        TrackLayerClick();
        HandleLayerTurnInput();
    }

    private void TrackLayerClick()
    {
        if (GameState.Instance == null || GameState.Instance.CurrentView != ViewMode.View2)
        {
            isTrackingClick = false;
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(0))
        {
            isTrackingClick = true;
            clickStartMousePos = Input.mousePosition;
        }

        if (!Input.GetMouseButtonUp(0))
            return;

        if (!isTrackingClick)
            return;

        isTrackingClick = false;

        Vector3 upPos = Input.mousePosition;
        float moveSq = (upPos - clickStartMousePos).sqrMagnitude;
        if (moveSq > clickThresholdPixels * clickThresholdPixels)
            return;

        if (!TryBuildClickSelection(
                out InitCubeSlot.CubePiece piece,
                out InitCubeSlotFaceDir face,
                out Vector3 localUp,
                out Vector3 localRight,
                out int verticalLayerIndex,
                out int horizontalLayerIndex))
            return;

        float now = Time.unscaledTime;
        bool isDoubleClick =
            now - lastClickTime <= Mathf.Max(0.01f, doubleClickThreshold) &&
            lastClickedPiece == piece &&
            lastClickedFace == face;

        if (isDoubleClick)
        {
            selectedLayerMode = View2LayerSelectionMode.Horizontal;
            selectedLayerIndex = horizontalLayerIndex;
            selectedArrowSide = ArrowSide.Left;
        }
        else
        {
            selectedLayerMode = View2LayerSelectionMode.Vertical;
            selectedLayerIndex = verticalLayerIndex;
            selectedArrowSide = ArrowSide.Up;
        }

        if (!TryCacheSelectedLayer(face, localUp, localRight, selectedArrowSide, selectedLayerIndex))
        {
            ClearSelection();
            return;
        }

        lastClickTime = now;
        lastClickedPiece = piece;
        lastClickedFace = face;
    }

    private void HandleLayerTurnInput()
    {
        if (GameState.Instance == null || GameState.Instance.CurrentView != ViewMode.View2)
            return;

        if (selectedLayerMode == View2LayerSelectionMode.None || isTurnAnimating)
            return;

        bool rotated = false;

        if (selectedLayerMode == View2LayerSelectionMode.Vertical)
        {
            if (Input.GetKeyDown(KeyCode.W))
                rotated = TryRotateLayer(selectedFace, ArrowSide.Up, selectedLayerIndex, false);
            else if (Input.GetKeyDown(KeyCode.S))
                rotated = TryRotateLayer(selectedFace, ArrowSide.Up, selectedLayerIndex, true);
        }
        else if (selectedLayerMode == View2LayerSelectionMode.Horizontal)
        {
            if (Input.GetKeyDown(KeyCode.A))
                rotated = TryRotateLayer(selectedFace, ArrowSide.Left, selectedLayerIndex, false);
            else if (Input.GetKeyDown(KeyCode.D))
                rotated = TryRotateLayer(selectedFace, ArrowSide.Left, selectedLayerIndex, true);
        }

    }

    public bool TryRotateLayer(
        InitCubeSlotFaceDir faceDir,
        ArrowSide side,
        int index,
        bool reverse)
    {
        if (isTurnAnimating || initCubeSlot == null)
            return false;

        bool useCachedSelection =
            selectedFace == faceDir &&
            selectedArrowSide == side &&
            selectedLayerIndex == index &&
            currentCubePiece.Count > 0;

        if (!useCachedSelection)
        {
            GetCurrentBasis(out Vector3 localUp, out Vector3 localRight);
            if (!TryCacheSelectedLayer(faceDir, localUp, localRight, side, index))
                return false;
        }

        float angle = reverse ? -90f : 90f;
        Transform cubeRoot = ViewModeManager.Instance?.cubeRoot;
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

        if (GameState.Instance != null)
            GameState.Instance.SetPlayerState(PlayerState.isTurning);

        StartCoroutine(AnimateTurn(animationStates, isCW));
        return true;
    }

    private void ClearSelection()
    {
        isTrackingClick = false;
        selectedLayerMode = View2LayerSelectionMode.None;
        selectedArrowSide = ArrowSide.Up;
        selectedLayerIndex = 0;
        selectedFace = InitCubeSlotFaceDir.Up;
        currentCubePiece.Clear();
    }

    private IEnumerator AnimateTurn(List<TurnAnimationState> animationStates, bool isCW)
    {
        isTurnAnimating = true;
        MusicAudioManager.Instance.PlaySfx("mofang");

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
        if (GameState.Instance != null)
            GameState.Instance.SetPlayerState(PlayerState.rotatingFinished);
    }

    private void ApplyTurnResult(bool isCW)
    {
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
            }
        }

        initCubeSlot.RebuildSurfaceCoordMap();
    }

    private bool TryBuildClickSelection(
        out InitCubeSlot.CubePiece piece,
        out InitCubeSlotFaceDir face,
        out Vector3 localUp,
        out Vector3 localRight,
        out int verticalLayerIndex,
        out int horizontalLayerIndex)
    {
        piece = null;
        face = InitCubeSlotFaceDir.Up;
        localUp = Vector3.up;
        localRight = Vector3.right;
        verticalLayerIndex = 0;
        horizontalLayerIndex = 0;

        if (!TryResolveView2CameraTransform() || view2Camera == null)
            return false;

        if (!TryGetView2FaceAndBasis(
                out face,
                out localUp,
                out localRight))
            return false;

        Ray ray = view2Camera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return false;

        piece = ResolvePieceByHitTransform(hit.transform);
        if (piece == null)
        {
            piece = ResolveFacePieceByHitPoint(hit.point, face, localUp, localRight);
        }

        if (piece == null)
            return false;

        Vector3 localNormal = GetFaceNormal(face);
        int faceDepth = Mathf.RoundToInt(Vector3.Dot(piece.coord, localNormal));
        if (faceDepth != CoordScale)
            return false;

        int columnCoord = Mathf.RoundToInt(Vector3.Dot(piece.coord, localRight));
        int rowCoord = Mathf.RoundToInt(Vector3.Dot(piece.coord, localUp));

        verticalLayerIndex = CoordToColumnIndex(columnCoord);
        horizontalLayerIndex = CoordToRowIndex(rowCoord);
        return true;
    }

    private bool TryGetView2FaceAndBasis(
        out InitCubeSlotFaceDir face,
        out Vector3 localUp,
        out Vector3 localRight)
    {
        face = InitCubeSlotFaceDir.Front;
        localUp = Vector3.up;
        localRight = Vector3.right;

        Transform cubeRoot = ViewModeManager.Instance?.cubeRoot;
        if (cubeRoot == null || !TryResolveView2CameraTransform() || view2CameraTransform == null)
            return false;

        Vector3 cameraLocalForward = cubeRoot.InverseTransformDirection(view2CameraTransform.forward);
        Vector3 cameraLocalUp = cubeRoot.InverseTransformDirection(view2CameraTransform.up);
        Vector3 cameraLocalRight = cubeRoot.InverseTransformDirection(view2CameraTransform.right);

        Vector3 snappedFaceNormal = SnapToPrimaryAxis(-cameraLocalForward);
        lockedLocalUp = SnapToPrimaryAxis(cameraLocalUp);
        lockedLocalRight = SnapToPrimaryAxis(cameraLocalRight);
        localUp = lockedLocalUp;
        localRight = lockedLocalRight;

        face = VectorToFaceDir(snappedFaceNormal);
        return true;
    }

    private void GetCurrentBasis(out Vector3 localUp, out Vector3 localRight)
    {
        localUp = lockedLocalUp;
        localRight = lockedLocalRight;
    }

    private bool TryCacheSelectedLayer(
        InitCubeSlotFaceDir faceDir,
        Vector3 localUp,
        Vector3 localRight,
        ArrowSide side,
        int index)
    {
        if (initCubeSlot == null)
            return false;

        (Vector3 signedAxis, int coordValue) = GetLayerFilter(localUp, localRight, side, index);

        if(currentCubePiece!=null)
            RestoreSelectionMaterials();
        // 清空旧列表
        currentCubePiece.Clear();

        currentSignedLocalAxis = signedAxis;
        currentAxis = VectorToAxis(signedAxis);
        currentCubePiece = initCubeSlot.GetPiecesInLayer(currentAxis, coordValue);

        if (currentCubePiece.Count == 0)
            return false;

        //更新材质
        foreach (InitCubeSlot.CubePiece piece in currentCubePiece)
        {
            if (piece?.indexCube == null) continue;
            var meshRenderer = piece.indexCube.GetChild(0).GetComponent<MeshRenderer>();
            if (meshRenderer != null)
                meshRenderer.material = M_Outline;

        }

        selectedFace = faceDir;
        selectedArrowSide = side;
        selectedLayerIndex = index;
        return true;
    }

    private (Vector3 signedAxis, int coordValue) GetLayerFilter(
        Vector3 localUp,
        Vector3 localRight,
        ArrowSide side,
        int index)
    {
        int val = IndexToCoordValue(Mathf.Clamp(index, 0, 2));

        Vector3 signedAxis = side == ArrowSide.Up ? localRight : localUp;
        int axisSign = GetAxisSign(signedAxis);
        int coordValue = side == ArrowSide.Up
            ? axisSign * val
            : axisSign * -val;

        return (signedAxis, coordValue);
    }

    private InitCubeSlot.CubePiece ResolvePieceByHitTransform(Transform hitTransform)
    {
        if (hitTransform == null || initCubeSlot == null)
            return null;

        foreach (var slot in initCubeSlot.slots)
        {
            var piece = slot.occupant;
            if (piece?.indexCube == null)
                continue;

            if (hitTransform == piece.indexCube || hitTransform.IsChildOf(piece.indexCube))
                return piece;
        }

        return null;
    }

    private InitCubeSlot.CubePiece ResolveFacePieceByHitPoint(
        Vector3 hitPoint,
        InitCubeSlotFaceDir face,
        Vector3 localUp,
        Vector3 localRight)
    {
        if (initCubeSlot == null)
            return null;

        Transform cubeRoot = ViewModeManager.Instance?.cubeRoot;
        if (cubeRoot == null)
            return null;

        Vector3 localNormal = GetFaceNormal(face);
        Vector3 localHitPoint = cubeRoot.InverseTransformPoint(hitPoint);

        InitCubeSlot.CubePiece bestPiece = null;
        float bestDistanceSq = float.PositiveInfinity;

        foreach (var slot in initCubeSlot.slots)
        {
            var piece = slot.occupant;
            if (piece?.indexCube == null)
                continue;

            int faceDepth = Mathf.RoundToInt(Vector3.Dot(piece.coord, localNormal));
            if (faceDepth != CoordScale)
                continue;

            Vector3 localPiecePos = cubeRoot.InverseTransformPoint(piece.indexCube.position);
            float dx = Vector3.Dot(localHitPoint - localPiecePos, localRight);
            float dy = Vector3.Dot(localHitPoint - localPiecePos, localUp);
            float distanceSq = dx * dx + dy * dy;

            if (distanceSq < bestDistanceSq)
            {
                bestDistanceSq = distanceSq;
                bestPiece = piece;
            }
        }

        return bestPiece;
    }

    private bool TryResolveView2CameraTransform()
    {
        if (view2CameraTransform != null)
        {
            if (view2Camera == null)
                view2Camera = view2CameraTransform.GetComponent<Camera>();
            return true;
        }

        var activeCameraController = FindObjectOfType<CameraRotateController>();
        if (activeCameraController != null)
        {
            view2CameraTransform = activeCameraController.transform;
            view2Camera = activeCameraController.GetComponent<Camera>();
            return true;
        }

        foreach (var candidate in Resources.FindObjectsOfTypeAll<CameraRotateController>())
        {
            if (!candidate.gameObject.scene.IsValid())
                continue;

            view2CameraTransform = candidate.transform;
            view2Camera = candidate.GetComponent<Camera>();
            return true;
        }

        return false;
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

    private static InitCubeSlotFaceDir VectorToFaceDir(Vector3 axis)
    {
        if (Mathf.Abs(axis.x) > 0.5f)
            return axis.x > 0f ? InitCubeSlotFaceDir.Right : InitCubeSlotFaceDir.Left;

        if (Mathf.Abs(axis.y) > 0.5f)
            return axis.y > 0f ? InitCubeSlotFaceDir.Up : InitCubeSlotFaceDir.Down;

        return axis.z > 0f ? InitCubeSlotFaceDir.Front : InitCubeSlotFaceDir.Back;
    }

    private static int CoordToColumnIndex(int coordValue)
    {
        return Mathf.Clamp((coordValue / CoordScale) + 1, 0, 2);
    }

    private static int CoordToRowIndex(int coordValue)
    {
        return Mathf.Clamp(1 - (coordValue / CoordScale), 0, 2);
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

    private static int IndexToCoordValue(int index)
    {
        return (index - 1) * CoordScale;
    }
    //欧：恢复材质
    private void RestoreSelectionMaterials()
    {
        if (initCubeSlot == null) return;
        foreach (InitCubeSlot.CubePiece piece in currentCubePiece)
        {
            if (piece?.indexCube == null) continue;
            var meshRenderer = piece.indexCube.GetChild(0).GetComponent<MeshRenderer>();
            if (meshRenderer != null)
                meshRenderer.material = M_suliao;
        }
    }
    private int GetAxisComponent(InitCubeSlotAxis axis)
    {
        if (currentCubePiece.Count == 0) return 0;
        Vector3Int coord = currentCubePiece[0].coord;
        switch (axis)
        {
            case InitCubeSlotAxis.X: return coord.x;
            case InitCubeSlotAxis.Y: return coord.y;
            case InitCubeSlotAxis.Z: return coord.z;
        }
        return 0;
    }
}
