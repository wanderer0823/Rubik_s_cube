using UnityEngine;

/// <summary>
/// 压力板关联门。挂在 Plate 模型物体上。
/// Inspector 里拖入对应的 DoorController。
/// </summary>
public class PlateLink : MonoBehaviour
{
    [Tooltip("此压力板触发后打开的门")]
    public DoorController linkedDoor;

    [HideInInspector]
    public bool isPressed = false;
}
