using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    public event Action<DialogueStepData> OnDialogueStepStarted;
    public event Action OnDialogueFinished;
    public event Action<DialogueStepData> OnWaitForEventStepStarted;
    public event Action<string> OnWaitForEventStarted;

    private readonly Queue<DialogueStepData> dialogueQueue = new Queue<DialogueStepData>();
    private readonly HashSet<string> flags = new HashSet<string>();

    private DialogueStepData currentStep;
    private Coroutine delayCoroutine;
    private bool isRunning;
    private bool isWaitingForEvent;
    private string waitingEventId;

    public bool IsRunning => isRunning;
    public bool IsWaitingForEvent => isWaitingForEvent;
    public DialogueStepData CurrentStep => currentStep;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool TryStartDialogue(IEnumerable<DialogueStepData> dialogues)
    {
        if (isRunning || dialogues == null)
            return false;

        List<DialogueStepData> steps = new List<DialogueStepData>();

        foreach (DialogueStepData dialogue in dialogues)
        {
            if (dialogue == null)
                continue;

            if (!IsRequiredFlagSatisfied(dialogue.requiredFlag))
                return false;

            steps.Add(dialogue);
        }

        if (steps.Count == 0)
            return false;

        dialogueQueue.Clear();

        for (int i = 0; i < steps.Count; i++)
            dialogueQueue.Enqueue(steps[i]);

        isRunning = true;
        ProcessNextStep();
        return true;
    }

    public void ResumeDialogue()
    {
        if (!isRunning || isWaitingForEvent)
            return;

        CompleteCurrentStep();
        ProcessNextStep();
    }

    public void CompleteWaitEvent(string eventId)
    {
        if (!isRunning || !isWaitingForEvent)
            return;

        if (!string.IsNullOrEmpty(waitingEventId) && waitingEventId != eventId)
            return;

        isWaitingForEvent = false;
        waitingEventId = string.Empty;
        CompleteCurrentStep();
        ProcessNextStep();
    }

    public void SetFlag(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag))
            return;

        flags.Add(flag);
    }

    public bool HasFlag(string flag)
    {
        return string.IsNullOrWhiteSpace(flag) || flags.Contains(flag);
    }

    public void ClearFlags()
    {
        flags.Clear();
    }

    private void ProcessNextStep()
    {
        StopDelayCoroutine();

        if (dialogueQueue.Count == 0)
        {
            FinishDialogue();
            return;
        }

        currentStep = dialogueQueue.Dequeue();
        OnDialogueStepStarted?.Invoke(currentStep);

        switch (currentStep.ParsedStepType)
        {
            case DialogueStepType.WaitForEvent:
                StartWaitForEvent(currentStep);
                break;
            case DialogueStepType.Delay:
                delayCoroutine = StartCoroutine(DelayThenResume(currentStep.delayTime));
                break;
            case DialogueStepType.Text:
            case DialogueStepType.ShowTips:
            default:
                break;
        }
    }

    private void StartWaitForEvent(DialogueStepData step)
    {
        isWaitingForEvent = true;
        waitingEventId = step.eventId;
        OnWaitForEventStepStarted?.Invoke(step);
        OnWaitForEventStarted?.Invoke(waitingEventId);
    }

    private IEnumerator DelayThenResume(float delayTime)
    {
        if (delayTime > 0f)
            yield return new WaitForSeconds(delayTime);

        delayCoroutine = null;
        CompleteCurrentStep();
        ProcessNextStep();
    }

    private void CompleteCurrentStep()
    {
        if (currentStep == null)
            return;

        SetFlag(currentStep.completeFlag);
        currentStep = null;
    }

    private bool IsRequiredFlagSatisfied(string requiredFlag)
    {
        return string.IsNullOrWhiteSpace(requiredFlag) || HasFlag(requiredFlag);
    }

    private void FinishDialogue()
    {
        currentStep = null;
        isRunning = false;
        isWaitingForEvent = false;
        waitingEventId = string.Empty;
        OnDialogueFinished?.Invoke();
    }

    private void StopDelayCoroutine()
    {
        if (delayCoroutine == null)
            return;

        StopCoroutine(delayCoroutine);
        delayCoroutine = null;
    }
}
