using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    public static TutorialController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogueUI dialogueUI;

    [Header("JSON")]
    [SerializeField] private string tutorialFolderName = "TutorialDialogues";
    [SerializeField] private string roomFilePrefix = "room_";

    private const string LockWasdEventId = "LockWASD";
    private const string UnlockWasdEventId = "UnlockWASD";
    private const string OpenBagEventId = "OpenBag";
    private const string EnterCubeEventId = "EnterCube";
    private const string EnterCubeAgainEventId = "EnterCubeAgain";
    private const string ExitCubeEventId = "ExitCube";
    private const string RightDragCompletedEventId = "RightDragCompleted";
    private const string LeftDragCompletedEventId = "LeftDragCompleted";

    private readonly HashSet<int> triggeredRoomIds = new HashSet<int>();
    private string currentWaitingEventId;
    private bool hasGameStarted;
    private bool isSubscribedToManager;
    private bool isSubscribedToUI;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dialogueManager == null)
            dialogueManager = DialogueManager.Instance;
    }

    private void OnEnable()
    {
        Subscribe();
        SubscribeGameplayEvents();
    }

    private void Start()
    {
        if (dialogueManager == null)
            dialogueManager = DialogueManager.Instance;

        Subscribe();

        if (GameState.Instance != null && GameState.Instance.CurrentPlayerState != PlayerState.isStartUI)
        {
            hasGameStarted = true;
            TryStartRoomDialogue(GameState.Instance.CurrentRoomID);
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
        UnsubscribeGameplayEvents();
    }

    private void OnDialogueStepStarted(DialogueStepData step)
    {
        if (step == null || step.ParsedStepType != DialogueStepType.WaitForEvent)
            currentWaitingEventId = string.Empty;

        if (step != null && step.ParsedStepType == DialogueStepType.WaitForEvent && string.IsNullOrWhiteSpace(step.text))
            return;

        if (dialogueUI != null)
            dialogueUI.Show(step);
    }

    private void OnDialogueFinished()
    {
        currentWaitingEventId = string.Empty;

        if (dialogueUI != null)
            dialogueUI.Hide();
    }

    private void OnDialogueUIClickNext()
    {
        if (dialogueManager != null)
            dialogueManager.ResumeDialogue();
    }

    private void OnWaitForEventStarted(string eventId)
    {
        currentWaitingEventId = eventId;

        switch (eventId)
        {
            case LockWasdEventId:
                LockWASD();
                CompleteWaitEvent(eventId);
                break;
            case UnlockWasdEventId:
                UnlockWASD();
                CompleteWaitEvent(eventId);
                break;
            case OpenBagEventId:
                TryCompleteOpenBagIfAlreadyDone();
                break;
            case EnterCubeEventId:
            case EnterCubeAgainEventId:
                TryCompleteEnterCubeIfAlreadyDone(eventId);
                break;
            case ExitCubeEventId:
                TryCompleteExitCubeIfAlreadyDone();
                break;
            default:
                Debug.Log($"Tutorial wait event started: {eventId}");
                break;
        }
    }

    public void CompleteTutorialEvent(string eventId)
    {
        CompleteWaitEvent(eventId);
    }

    private void CompleteWaitEvent(string eventId)
    {
        if (dialogueManager != null)
            dialogueManager.CompleteWaitEvent(eventId);

        if (currentWaitingEventId == eventId)
            currentWaitingEventId = string.Empty;
    }

    private void LockWASD()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetMovementInputLocked(true);
    }

    private void UnlockWASD()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetMovementInputLocked(false);
    }

    public bool TryStartRoomDialogue(int roomId)
    {
        if (!hasGameStarted)
            return false;

        if (dialogueManager == null)
            dialogueManager = DialogueManager.Instance;

        if (dialogueManager == null || triggeredRoomIds.Contains(roomId))
            return false;

        DialogueJsonFile dialogueJsonFile = LoadRoomDialogue(roomId);
        if (dialogueJsonFile == null || dialogueJsonFile.steps == null || dialogueJsonFile.steps.Length == 0)
            return false;

        if (dialogueManager.TryStartDialogue(dialogueJsonFile.steps))
        {
            triggeredRoomIds.Add(roomId);
            return true;
        }

        return false;
    }

    public void ClearTriggeredRooms()
    {
        triggeredRoomIds.Clear();
    }

    private DialogueJsonFile LoadRoomDialogue(int roomId)
    {
        string filePath = GetRoomDialogueFilePath(roomId);
        if (!File.Exists(filePath))
        {
            filePath = GetRoomDialogueFallbackFilePath(roomId);
            if (!File.Exists(filePath))
                return null;
        }

        string json = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning($"Tutorial dialogue JSON is empty: {filePath}");
            return null;
        }

        try
        {
            DialogueJsonFile dialogueJsonFile = JsonUtility.FromJson<DialogueJsonFile>(json);
            if (dialogueJsonFile == null)
            {
                Debug.LogWarning($"Tutorial dialogue JSON parse failed: {filePath}");
                return null;
            }

            if (dialogueJsonFile.roomId != 0 && dialogueJsonFile.roomId != roomId)
                Debug.LogWarning($"Tutorial dialogue JSON roomId mismatch. File room={roomId}, json room={dialogueJsonFile.roomId}");

            return dialogueJsonFile;
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"Tutorial dialogue JSON parse exception: {filePath}\n{exception.Message}");
            return null;
        }
    }

    private string GetRoomDialogueFilePath(int roomId)
    {
        string fileName = $"{roomFilePrefix}{roomId}.json";
        return Path.Combine(Application.streamingAssetsPath, tutorialFolderName, fileName);
    }

    private string GetRoomDialogueFallbackFilePath(int roomId)
    {
        string fileName = $"{roomFilePrefix}{roomId}.js";
        return Path.Combine(Application.streamingAssetsPath, tutorialFolderName, fileName);
    }

    private void Subscribe()
    {
        if (!isSubscribedToManager && dialogueManager != null)
        {
            dialogueManager.OnDialogueStepStarted += OnDialogueStepStarted;
            dialogueManager.OnDialogueFinished += OnDialogueFinished;
            dialogueManager.OnWaitForEventStarted += OnWaitForEventStarted;
            isSubscribedToManager = true;
        }

        if (!isSubscribedToUI && dialogueUI != null)
        {
            dialogueUI.OnClickNext += OnDialogueUIClickNext;
            isSubscribedToUI = true;
        }
    }

    private void SubscribeGameplayEvents()
    {
        GameEvents.OnGameStartExecute += OnGameStartExecute;
        GameEvents.OnBagOpenExecute += OnBagOpenExecute;
        GameEvents.OnViewSwitchExecute += OnViewSwitchExecute;
        GameEvents.OnCameraRotateFinishExecute += OnCameraRotateFinishExecute;
        GameEvents.OnCubeRotateSettledExecute += OnCubeRotateSettledExecute;
    }

    private void UnsubscribeGameplayEvents()
    {
        GameEvents.OnGameStartExecute -= OnGameStartExecute;
        GameEvents.OnBagOpenExecute -= OnBagOpenExecute;
        GameEvents.OnViewSwitchExecute -= OnViewSwitchExecute;
        GameEvents.OnCameraRotateFinishExecute -= OnCameraRotateFinishExecute;
        GameEvents.OnCubeRotateSettledExecute -= OnCubeRotateSettledExecute;
    }

    private void OnGameStartExecute()
    {
        hasGameStarted = true;

        if (GameState.Instance != null)
            TryStartRoomDialogue(GameState.Instance.CurrentRoomID);
    }

    private void OnBagOpenExecute()
    {
        TryCompleteWaitingEvent(OpenBagEventId);
    }

    private void OnViewSwitchExecute(ViewMode mode)
    {
        if (mode == ViewMode.View2)
        {
            if (IsWaitingFor(EnterCubeEventId))
                CompleteWaitEvent(EnterCubeEventId);
            else if (IsWaitingFor(EnterCubeAgainEventId))
                CompleteWaitEvent(EnterCubeAgainEventId);
        }
        else if (mode == ViewMode.View3)
        {
            TryCompleteWaitingEvent(ExitCubeEventId);
        }
    }

    private void OnCameraRotateFinishExecute()
    {
        TryCompleteWaitingEvent(RightDragCompletedEventId);
    }

    private void OnCubeRotateSettledExecute()
    {
        TryCompleteWaitingEvent(LeftDragCompletedEventId);
    }

    private void TryCompleteOpenBagIfAlreadyDone()
    {
        if (GameState.Instance != null && GameState.Instance.IsBagOpen)
            TryCompleteWaitingEvent(OpenBagEventId);
    }

    private void TryCompleteEnterCubeIfAlreadyDone(string eventId)
    {
        if (GameState.Instance != null && GameState.Instance.CurrentView == ViewMode.View2)
            TryCompleteWaitingEvent(eventId);
    }

    private void TryCompleteExitCubeIfAlreadyDone()
    {
        if (GameState.Instance != null && GameState.Instance.CurrentView == ViewMode.View3)
            TryCompleteWaitingEvent(ExitCubeEventId);
    }

    private bool IsWaitingFor(string eventId)
    {
        return currentWaitingEventId == eventId;
    }

    private void TryCompleteWaitingEvent(string eventId)
    {
        if (IsWaitingFor(eventId))
            CompleteWaitEvent(eventId);
    }

    private void Unsubscribe()
    {
        if (isSubscribedToManager && dialogueManager != null)
        {
            dialogueManager.OnDialogueStepStarted -= OnDialogueStepStarted;
            dialogueManager.OnDialogueFinished -= OnDialogueFinished;
            dialogueManager.OnWaitForEventStarted -= OnWaitForEventStarted;
        }

        if (isSubscribedToUI && dialogueUI != null)
            dialogueUI.OnClickNext -= OnDialogueUIClickNext;

        isSubscribedToManager = false;
        isSubscribedToUI = false;
    }
}
