using System.Text;
using UnityEditor;
using UnityEngine;
using static InitCubeSlot;

public class View2VisibleFaceRoomsTool : EditorWindow
{
    private const int CoordScale = 2;
    private const int FaceShellCoord = 3;

    private readonly int[,] roomGrid = new int[3, 3];
    private readonly bool[,] hasRoom = new bool[3, 3];
    private bool autoRefresh = true;
    private string status = "Waiting for Play Mode.";
    private FaceDir facingFace = FaceDir.Front;

    [MenuItem("Tools/Rooms/View2 Visible Face Rooms")]
    private static void Open()
    {
        GetWindow<View2VisibleFaceRoomsTool>("View2 Rooms");
    }

    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
        Refresh();
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        if (!autoRefresh || !EditorApplication.isPlaying)
            return;

        Refresh();
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("View2 Visible Face Rooms", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        using (new EditorGUILayout.HorizontalScope())
        {
            autoRefresh = EditorGUILayout.ToggleLeft("Auto Refresh", autoRefresh, GUILayout.Width(120));

            if (GUILayout.Button("Refresh", GUILayout.Width(90)))
                Refresh();

            using (new EditorGUI.DisabledScope(!HasAnyRoom()))
            {
                if (GUILayout.Button("Copy", GUILayout.Width(70)))
                    EditorGUIUtility.systemCopyBuffer = BuildGridText();
            }
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Status", status);
        EditorGUILayout.LabelField("Facing Face", facingFace.ToString());
        EditorGUILayout.Space(8);

        DrawGrid();
    }

    private void DrawGrid()
    {
        for (int row = 0; row < 3; row++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int col = 0; col < 3; col++)
                {
                    string label = hasRoom[row, col] ? roomGrid[row, col].ToString() : "-";
                    GUILayout.Label(label, EditorStyles.helpBox, GUILayout.Width(64), GUILayout.Height(32));
                }
            }
        }
    }

    private void Refresh()
    {
        ClearGrid();

        if (!EditorApplication.isPlaying)
        {
            status = "Enter Play Mode to read runtime View2 data.";
            return;
        }

        if (GameState.Instance == null)
        {
            status = "GameState.Instance was not found.";
            return;
        }

        if (GameState.Instance.CurrentView != ViewMode.View2)
        {
            status = $"Current view is {GameState.Instance.CurrentView}, not View2.";
            return;
        }

        ViewModeManager viewModeManager = ViewModeManager.Instance;
        if (viewModeManager == null || viewModeManager.cubeRoot == null || viewModeManager.cubeData == null)
        {
            status = "ViewModeManager, cubeRoot, or cubeData was not found.";
            return;
        }

        Camera view2Camera = ResolveView2Camera();
        if (view2Camera == null)
        {
            status = "View2 camera was not found.";
            return;
        }

        if (!TryGetFaceBasis(
                viewModeManager.cubeRoot,
                view2Camera.transform,
                out facingFace,
                out Vector3 localNormal,
                out Vector3 localUp,
                out Vector3 localRight))
        {
            status = "Could not resolve the View2 camera basis.";
            return;
        }

        int found = FillGrid(viewModeManager.cubeData, localNormal, localUp, localRight);
        status = found == 9
            ? "Showing the 3x3 room IDs currently facing the View2 camera."
            : $"Found {found}/9 rooms on the visible face.";
    }

    private int FillGrid(InitCubeSlot cubeData, Vector3 localNormal, Vector3 localUp, Vector3 localRight)
    {
        int found = 0;

        foreach (Slot slot in cubeData.slots)
        {
            if (slot?.occupant?.surfaces == null)
                continue;

            foreach (CubeSurface_s surface in slot.occupant.surfaces)
            {
                Vector3 coord = new Vector3(surface.coord.x, surface.coord.y, surface.coord.z);
                int depth = Mathf.RoundToInt(Vector3.Dot(coord, localNormal));
                if (depth != FaceShellCoord)
                    continue;

                int row = CoordToRowIndex(Mathf.RoundToInt(Vector3.Dot(coord, localUp)));
                int col = CoordToColumnIndex(Mathf.RoundToInt(Vector3.Dot(coord, localRight)));
                if (row < 0 || row >= 3 || col < 0 || col >= 3)
                    continue;

                if (!hasRoom[row, col])
                    found++;

                roomGrid[row, col] = surface.roomID;
                hasRoom[row, col] = true;
            }
        }

        return found;
    }

    private static Camera ResolveView2Camera()
    {
        CameraRotateController controller = FindObjectOfType<CameraRotateController>();
        if (controller != null)
            return controller.GetComponent<Camera>();

        foreach (CameraRotateController candidate in Resources.FindObjectsOfTypeAll<CameraRotateController>())
        {
            if (candidate == null || !candidate.gameObject.scene.IsValid())
                continue;

            return candidate.GetComponent<Camera>();
        }

        return null;
    }

    private static bool TryGetFaceBasis(
        Transform cubeRoot,
        Transform cameraTransform,
        out FaceDir face,
        out Vector3 localNormal,
        out Vector3 localUp,
        out Vector3 localRight)
    {
        face = FaceDir.Front;
        localNormal = Vector3.forward;
        localUp = Vector3.up;
        localRight = Vector3.right;

        if (cubeRoot == null || cameraTransform == null)
            return false;

        Vector3 cameraLocalForward = cubeRoot.InverseTransformDirection(cameraTransform.forward);
        Vector3 cameraLocalUp = cubeRoot.InverseTransformDirection(cameraTransform.up);
        Vector3 cameraLocalRight = cubeRoot.InverseTransformDirection(cameraTransform.right);

        localNormal = SnapToPrimaryAxis(-cameraLocalForward);
        face = VectorToFaceDir(localNormal);
        localUp = SnapToPrimaryAxis(cameraLocalUp);
        localRight = SnapToPrimaryAxis(cameraLocalRight);

        if (!IsPerpendicular(localNormal, localUp) || !IsPerpendicular(localNormal, localRight) || !IsPerpendicular(localUp, localRight))
            GetDefaultFaceBasis(face, out localNormal, out localUp, out localRight);

        return localNormal != Vector3.zero && localUp != Vector3.zero && localRight != Vector3.zero;
    }

    private static bool IsPerpendicular(Vector3 a, Vector3 b)
    {
        return Mathf.Abs(Vector3.Dot(a, b)) < 0.5f;
    }

    private static void GetDefaultFaceBasis(FaceDir face, out Vector3 localNormal, out Vector3 localUp, out Vector3 localRight)
    {
        localNormal = FaceToVector(face);
        localUp = face == FaceDir.Up
            ? Vector3.back
            : face == FaceDir.Down
                ? Vector3.forward
                : Vector3.up;
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

    private static FaceDir VectorToFaceDir(Vector3 axis)
    {
        if (Mathf.Abs(axis.x) > 0.5f)
            return axis.x > 0f ? FaceDir.Right : FaceDir.Left;

        if (Mathf.Abs(axis.y) > 0.5f)
            return axis.y > 0f ? FaceDir.Up : FaceDir.Down;

        return axis.z > 0f ? FaceDir.Front : FaceDir.Back;
    }

    private static Vector3 FaceToVector(FaceDir face)
    {
        switch (face)
        {
            case FaceDir.Up: return Vector3.up;
            case FaceDir.Down: return Vector3.down;
            case FaceDir.Left: return Vector3.left;
            case FaceDir.Right: return Vector3.right;
            case FaceDir.Front: return Vector3.forward;
            case FaceDir.Back: return Vector3.back;
            default: return Vector3.forward;
        }
    }

    private static int CoordToColumnIndex(int coordValue)
    {
        if (coordValue < -CoordScale / 2)
            return 0;

        if (coordValue > CoordScale / 2)
            return 2;

        return 1;
    }

    private static int CoordToRowIndex(int coordValue)
    {
        if (coordValue > CoordScale / 2)
            return 0;

        if (coordValue < -CoordScale / 2)
            return 2;

        return 1;
    }

    private void ClearGrid()
    {
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                roomGrid[row, col] = -1;
                hasRoom[row, col] = false;
            }
        }
    }

    private bool HasAnyRoom()
    {
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                if (hasRoom[row, col])
                    return true;
            }
        }

        return false;
    }

    private string BuildGridText()
    {
        StringBuilder builder = new StringBuilder();

        for (int row = 0; row < 3; row++)
        {
            if (row > 0)
                builder.AppendLine();

            for (int col = 0; col < 3; col++)
            {
                if (col > 0)
                    builder.Append('\t');

                builder.Append(hasRoom[row, col] ? roomGrid[row, col].ToString() : "-");
            }
        }

        return builder.ToString();
    }
}
