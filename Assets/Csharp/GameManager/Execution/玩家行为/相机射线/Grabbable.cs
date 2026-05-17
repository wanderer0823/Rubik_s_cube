using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;


public enum Angle90
{
    Neg90 = -90,
    Zero = 0,
    Pos90 = 90
}

[System.Serializable]
public class Angle90Vector3
{
    public Angle90 x;
    public Angle90 y;
    public Angle90 z;

    public Vector3 ToVector3()
    {
        return new Vector3((int)x, (int)y, (int)z);
    }
}

[CustomPropertyDrawer(typeof(Angle90Vector3))]
public class Angle90Vector3Drawer : PropertyDrawer
{
    private readonly string[] options = { "-90", "0", "90" };
    private readonly int[] values = { -90, 0, 90 };

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        position = EditorGUI.PrefixLabel(position, label);

        float width = position.width / 3f;

        DrawPopup(
            new Rect(position.x, position.y, width - 4, position.height),
            property.FindPropertyRelative("x")
        );

        DrawPopup(
            new Rect(position.x + width, position.y, width - 4, position.height),
            property.FindPropertyRelative("y")
        );

        DrawPopup(
            new Rect(position.x + width * 2, position.y, width - 4, position.height),
            property.FindPropertyRelative("z")
        );

        EditorGUI.EndProperty();
    }

    private void DrawPopup(Rect rect, SerializedProperty prop)
    {
        int current = System.Array.IndexOf(values, prop.intValue);

        if (current < 0)
            current = 1;

        current = EditorGUI.Popup(rect, current, options);

        prop.intValue = values[current];
    }
}

#endif

/// <summary>
/// 可抓取标记。挂在场景中可被准星举起的物体上。
/// 物体需要有 Collider（用于射线检测）。
/// 如果需要释放后自由下落，还需要 Rigidbody。
/// </summary>
public class Grabbable : MonoBehaviour
{
    [Header("说明（仅编辑器提示）")]
    [Tooltip("此物体可被玩家准星举起")]
    public string description = "可交互物体";

    [Header("限制同重力才可移动道具")]
    public Angle90Vector3 allowedParentRotate;
}