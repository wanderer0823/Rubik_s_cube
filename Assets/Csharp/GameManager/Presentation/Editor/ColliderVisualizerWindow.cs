using UnityEngine;
using UnityEditor;

public class ColliderVisualizerWindow : EditorWindow
{
    static bool enableVisualization = true;
    static bool drawFill = true;
    static bool drawOutline = true;

    static bool showBox = true;
    static bool showMesh = true;
    static bool showSphere = true;
    static bool showCapsule = true;
    static bool showTrigger = true;
    static int controlID;

    static Collider selectedCollider;

    static ColliderVisualizerWindow window;

    [MenuItem("Tools/碰撞体生成/碰撞体可视")]
    static void OpenWindow()
    {
        window = GetWindow<ColliderVisualizerWindow>();

        window.titleContent =
            new GUIContent("碰撞体可视");

        window.Show();
    }

    void OnEnable()
    {
        SceneView.duringSceneGui += DuringSceneGUI;
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    void OnGUI()
    {
        GUILayout.Space(10);

        GUILayout.Label(
            "碰撞体可视",
            EditorStyles.boldLabel);

        GUILayout.Space(5);

        enableVisualization =
            EditorGUILayout.Toggle(
                "启用",
                enableVisualization);

        drawFill =
            EditorGUILayout.Toggle(
                "填充",
                drawFill);

        drawOutline =
            EditorGUILayout.Toggle(
                "边框",
                drawOutline);

        GUILayout.Space(10);

        GUILayout.Space(10);

        GUILayout.Label("显示设置", EditorStyles.boldLabel);

        showBox = EditorGUILayout.Toggle("BoxCollider", showBox);
        showMesh = EditorGUILayout.Toggle("MeshCollider", showMesh);
        showSphere = EditorGUILayout.Toggle("SphereCollider", showSphere);
        showCapsule = EditorGUILayout.Toggle("CapsuleCollider", showCapsule);
        showTrigger = EditorGUILayout.Toggle("Trigger", showTrigger);

        GUILayout.Space(10);

        GUILayout.Space(10);

        GUILayout.Label("当前选中", EditorStyles.boldLabel);

        if (selectedCollider == null)
        {
            GUILayout.Label("无");
        }
        else
        {
            GUILayout.Label(selectedCollider.GetType().Name);
            GUILayout.Label(selectedCollider.gameObject.name);

            if (selectedCollider.isTrigger)
                GUILayout.Label("Trigger");
        }

        GUILayout.Label("颜色说明", EditorStyles.boldLabel);

        DrawColorLabel(Color.blue, "BoxCollider");
        DrawColorLabel(Color.green, "MeshCollider");
        DrawColorLabel(Color.yellow, "SphereCollider");
        DrawColorLabel(new Color(1f, 0f, 1f), "CapsuleCollider");
        DrawColorLabel(Color.red, "Trigger");
    }

    void DrawColorLabel(Color color,string text)
    {
        Rect r =
            EditorGUILayout.GetControlRect(false,20);

        EditorGUI.DrawRect(
            new Rect(r.x, r.y + 2, 20, 16), color);

        GUI.Label(
            new Rect(r.x + 30,r.y,200,20),text);
    }

    static void DuringSceneGUI(SceneView sceneView)
    {
        if (!enableVisualization)
            return;

        Event e = Event.current;

        controlID = GUIUtility.GetControlID(FocusType.Passive);

        HandleUtility.AddDefaultControl(controlID);

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            TrySelectCollider(e.mousePosition);
        }

        Collider[] colliders = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);

        foreach (Collider col in colliders)
        {
            if (col == null)
                continue;

            if (!ShouldDrawCollider(col))
                continue;

            if (col is BoxCollider)
                DrawBox(col as BoxCollider);

            else if (col is MeshCollider)
                DrawMesh(col as MeshCollider);

            else if (col is SphereCollider)
                DrawSphere(col as SphereCollider);

            else if (col is CapsuleCollider)
                DrawCapsule(col as CapsuleCollider);
        }

        SceneView.RepaintAll();
    }

    static void DrawBox( BoxCollider box)
    {
        Transform t =
            box.transform;

        Matrix4x4 old =
            Handles.matrix;

        Handles.matrix =
            Matrix4x4.TRS(
                t.TransformPoint(
                    box.center),
                t.rotation,
                t.lossyScale);
        Color fill;
        Color outline;

        GetColliderColor(box, out fill, out outline);

        if (box.isTrigger)
        {
            fill = new Color(1f, 0f, 0f, 0.2f);
            outline = Color.red;
        }
        else
        {
            fill = new Color(0f, 0.5f, 1f, 0.2f);
            outline = new Color(0f, 0.3f, 1f, 1f);
        }

        if (IsSelected(box))
        {
            fill.a = 0.8f;
            outline = Color.white;

            Handles.color = Color.white;

            Handles.DrawWireCube(Vector3.zero, box.size * 1.01f);
        }

        if (drawFill)
        {
            Handles.color = fill;

            Vector3 h = box.size * 0.5f;

            DrawFace(
                new Vector3(-h.x, -h.y, h.z),
                new Vector3(h.x, -h.y, h.z),
                new Vector3(h.x, h.y, h.z),
                new Vector3(-h.x, h.y, h.z));

            DrawFace(
                new Vector3(-h.x, -h.y, -h.z),
                new Vector3(h.x, -h.y, -h.z),
                new Vector3(h.x, h.y, -h.z),
                new Vector3(-h.x, h.y, -h.z));

            DrawFace(
                new Vector3(-h.x, -h.y, -h.z),
                new Vector3(-h.x, -h.y, h.z),
                new Vector3(-h.x, h.y, h.z),
                new Vector3(-h.x, h.y, -h.z));

            DrawFace(
                new Vector3(h.x, -h.y, -h.z),
                new Vector3(h.x, -h.y, h.z),
                new Vector3(h.x, h.y, h.z),
                new Vector3(h.x, h.y, -h.z));

            DrawFace(
                new Vector3(-h.x, h.y, -h.z),
                new Vector3(h.x, h.y, -h.z),
                new Vector3(h.x, h.y, h.z),
                new Vector3(-h.x, h.y, h.z));

            DrawFace(
                new Vector3(-h.x, -h.y, -h.z),
                new Vector3(h.x, -h.y, -h.z),
                new Vector3(h.x, -h.y, h.z),
                new Vector3(-h.x, -h.y, h.z));
        }

        if (drawOutline)
        {
            Handles.color =
                outline;

            Handles.DrawWireCube(
                Vector3.zero,
                box.size);
        }

        Handles.matrix = old;
    }

    static void DrawFace(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        Handles.DrawAAConvexPolygon( a, b, c, d);
    }

    static void TrySelectCollider(Vector2 mousePos)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);

        Collider nearest = null;
        float nearestDistance = float.MaxValue;

        Collider[] colliders = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);

        foreach (Collider col in colliders)
        {
            if (col == null)
                continue;

            if (!ShouldDrawCollider(col))
                continue;

            RaycastHit hit;

            if (!col.Raycast(ray, out hit, 100000f))
                continue;

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                nearest = col;
            }
        }

        selectedCollider = nearest;

        if (nearest == null)
        {
            Selection.activeObject = null;
        }
        else
        {
            Selection.activeObject = nearest;
        }

        SceneView.RepaintAll();
    }

    static bool IsSelected(Collider col)
    {
        if (selectedCollider == null)
            return false;

        return selectedCollider == col;
    }

    static bool ShouldDrawCollider(Collider col)
    {
        if (col == null)
            return false;

        if (col.isTrigger)
            return showTrigger;

        if (col is BoxCollider)
            return showBox;

        if (col is MeshCollider)
            return showMesh;

        if (col is SphereCollider)
            return showSphere;

        if (col is CapsuleCollider)
            return showCapsule;

        return false;
    }

    static void GetColliderColor(Collider col, out Color fill, out Color outline)
    {
        if (col.isTrigger)
        {
            fill = new Color(1f, 0f, 0f, 0.2f);
            outline = Color.red;
        }
        else if (col is BoxCollider)
        {
            fill = new Color(0f, 0.5f, 1f, 0.2f);
            outline = new Color(0f, 0.3f, 1f);
        }
        else if (col is MeshCollider)
        {
            fill = new Color(0f, 1f, 0f, 0.2f);
            outline = Color.green;
        }
        else if (col is SphereCollider)
        {
            fill = new Color(1f, 1f, 0f, 0.2f);
            outline = Color.yellow;
        }
        else
        {
            fill = new Color(1f, 0f, 1f, 0.2f);
            outline = Color.magenta;
        }

        if (IsSelected(col))
        {
            fill.a = 0.8f;
            outline = Color.white;
        }
    }

    static void DrawSphere(SphereCollider sphere)
    {
        Transform t = sphere.transform;

        Matrix4x4 old = Handles.matrix;

        Handles.matrix = Matrix4x4.TRS(
            t.TransformPoint(sphere.center),
            t.rotation,
            t.lossyScale);

        Color fill;
        Color outline;

        GetColliderColor(sphere, out fill, out outline);

        if (drawFill)
        {
            Handles.color = fill;
            Handles.SphereHandleCap(
                0,
                Vector3.zero,
                Quaternion.identity,
                sphere.radius * 2f,
                EventType.Repaint);
        }

        if (drawOutline)
        {
            Handles.color = outline;

            Handles.DrawWireDisc(
                Vector3.zero,
                Vector3.up,
                sphere.radius);

            Handles.DrawWireDisc(
                Vector3.zero,
                Vector3.right,
                sphere.radius);

            Handles.DrawWireDisc(
                Vector3.zero,
                Vector3.forward,
                sphere.radius);
        }

        Handles.matrix = old;
    }

    static void DrawMesh(MeshCollider mesh)
    {
        if (mesh.sharedMesh == null)
            return;

        Transform t = mesh.transform;

        Bounds b = mesh.sharedMesh.bounds;

        Matrix4x4 old = Handles.matrix;

        Handles.matrix = Matrix4x4.TRS(
            t.TransformPoint(b.center),
            t.rotation,
            t.lossyScale);

        Color fill;
        Color outline;

        GetColliderColor(mesh, out fill, out outline);

        if (drawFill)
        {
            Handles.color = fill;

            Vector3 h = b.size * 0.5f;

            DrawFace(
                new Vector3(-h.x, -h.y, h.z),
                new Vector3(h.x, -h.y, h.z),
                new Vector3(h.x, h.y, h.z),
                new Vector3(-h.x, h.y, h.z));
        }

        if (drawOutline)
        {
            Handles.color = outline;
            Handles.DrawWireCube(Vector3.zero, b.size);
        }

        Handles.matrix = old;
    }

    static void DrawCapsule(CapsuleCollider capsule)
    {
        Transform t = capsule.transform;

        Matrix4x4 old = Handles.matrix;

        Handles.matrix = Matrix4x4.TRS(
            t.TransformPoint(capsule.center),
            t.rotation,
            t.lossyScale);

        Color fill;
        Color outline;

        GetColliderColor(capsule, out fill, out outline);

        Handles.color = outline;

        float r = capsule.radius;
        float h = capsule.height;

        Handles.DrawWireDisc(Vector3.up * (h * 0.5f - r), Vector3.up, r);
        Handles.DrawWireDisc(Vector3.down * (h * 0.5f - r), Vector3.up, r);

        Handles.DrawLine(
            new Vector3(r, h * 0.5f - r, 0),
            new Vector3(r, -h * 0.5f + r, 0));

        Handles.DrawLine(
            new Vector3(-r, h * 0.5f - r, 0),
            new Vector3(-r, -h * 0.5f + r, 0));

        Handles.matrix = old;
    }
}