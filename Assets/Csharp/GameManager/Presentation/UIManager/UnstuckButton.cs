using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UnstuckButton : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button unstuckButton;

    [Header("Teleport Target")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform roomOriginOverride;
    [SerializeField] private Vector3 roomLocalOffset = Vector3.zero;

    private Rigidbody playerRigidbody;
    private ContinuousCollisionDetector3D continuousCollisionDetector;

    private void Awake()
    {
        if (unstuckButton == null)
            unstuckButton = GetComponent<Button>();

        ResolvePlayer();
    }

    private void OnEnable()
    {
        if (unstuckButton != null)
            unstuckButton.onClick.AddListener(TeleportPlayerToCurrentRoomOrigin);
    }

    private void OnDisable()
    {
        if (unstuckButton != null)
            unstuckButton.onClick.RemoveListener(TeleportPlayerToCurrentRoomOrigin);
    }

    public void TeleportPlayerToCurrentRoomOrigin()
    {
        ResolvePlayer();

        if (player == null)
        {
            Debug.LogWarning("UnstuckButton: player is not assigned and could not be found.");
            return;
        }

        Transform roomOrigin = ResolveCurrentRoomOrigin();
        if (roomOrigin == null)
        {
            Debug.LogWarning("UnstuckButton: current room origin could not be found.");
            return;
        }

        Vector3 targetPosition = roomOrigin.TransformPoint(roomLocalOffset);
        TeleportPlayer(targetPosition);
    }

    private void ResolvePlayer()
    {
        if (player == null)
        {
            PlayerAction playerAction = FindObjectOfType<PlayerAction>();
            if (playerAction != null)
                player = playerAction.transform;
        }

        if (player == null)
            return;

        if (playerRigidbody == null)
            playerRigidbody = player.GetComponent<Rigidbody>();

        if (continuousCollisionDetector == null)
            continuousCollisionDetector = player.GetComponent<ContinuousCollisionDetector3D>();
    }

    private Transform ResolveCurrentRoomOrigin()
    {
        if (roomOriginOverride != null)
            return roomOriginOverride;

        RoomInstanceManager roomInstanceManager = FindObjectOfType<RoomInstanceManager>();
        if (roomInstanceManager != null && roomInstanceManager.CurrentRoom != null)
            return roomInstanceManager.CurrentRoom.transform;

        RoomGameObjectManager roomGameObjectManager = RoomGameObjectManager.Instance;
        if (roomGameObjectManager != null)
        {
            GameState gameState = GameState.Instance;
            if (gameState != null)
            {
                GameObject roomObject = roomGameObjectManager.GetRoomGameObject(gameState.CurrentRoomID);
                if (roomObject != null)
                    return roomObject.transform;
            }

            if (roomGameObjectManager.CurrentRoom != null)
                return roomGameObjectManager.CurrentRoom.transform;
        }

        return null;
    }

    private void TeleportPlayer(Vector3 targetPosition)
    {
        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.position = targetPosition;
        }

        player.position = targetPosition;

        if (continuousCollisionDetector != null)
            continuousCollisionDetector.OnTeleport(targetPosition);
    }
}
