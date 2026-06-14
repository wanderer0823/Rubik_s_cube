using UnityEngine;

[DisallowMultipleComponent]
public class DoorBindingTarget : MonoBehaviour
{
    [Tooltip("供 Plate 运行时查找此门的稳定 ID，由 Plate-Door 绑定面板维护")]
    public string doorId;

    private DoorController doorController;

    private void Awake()
    {
        doorController = GetComponent<DoorController>();
    }

    private void OnEnable()
    {
        Register();
    }

    private void OnDisable()
    {
        if (doorController == null)
            doorController = GetComponent<DoorController>();

        DoorRegistry.Unregister(doorId, doorController);
    }

    private void Register()
    {
        if (doorController == null)
            doorController = GetComponent<DoorController>();

        DoorRegistry.Register(doorId, doorController);
    }
}
