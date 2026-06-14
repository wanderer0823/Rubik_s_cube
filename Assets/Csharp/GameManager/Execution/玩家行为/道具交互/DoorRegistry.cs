using System.Collections.Generic;

public static class DoorRegistry
{
    private static readonly Dictionary<string, DoorController> Doors = new Dictionary<string, DoorController>();

    public static void Register(string doorId, DoorController door)
    {
        if (string.IsNullOrEmpty(doorId) || door == null)
            return;

        Doors[doorId] = door;
    }

    public static void Unregister(string doorId, DoorController door)
    {
        if (string.IsNullOrEmpty(doorId) || door == null)
            return;

        if (Doors.TryGetValue(doorId, out DoorController registeredDoor) && registeredDoor == door)
            Doors.Remove(doorId);
    }

    public static bool TryGet(string doorId, out DoorController door)
    {
        if (string.IsNullOrEmpty(doorId))
        {
            door = null;
            return false;
        }

        return Doors.TryGetValue(doorId, out door) && door != null;
    }
}
