using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    public event Action OnClickNext;

    [Header("References")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button clickButton;

    [Header("Typing")]
    [Min(1f)]
    [SerializeField] private float charactersPerSecond = 35f;

    private Coroutine typingCoroutine;
    private string currentText = string.Empty;
    private bool isTyping;

    private void Awake()
    {
        if (root == null)
            root = gameObject;
    }

    private void OnEnable()
    {
        if (clickButton != null)
            clickButton.onClick.AddListener(SkipTypingOrRequestNext);
    }

    private void OnDisable()
    {
        if (clickButton != null)
            clickButton.onClick.RemoveListener(SkipTypingOrRequestNext);
    }

    public void Show(DialogueStepData step)
    {
        if (step == null)
        {
            Hide();
            return;
        }

        if (root != null)
            root.SetActive(true);

        currentText = step.text ?? string.Empty;

        if (speakerText != null)
        {
            speakerText.text = step.speaker ?? string.Empty;
            speakerText.color = step.ParsedColor;
        }

        if (dialogueText != null)
        {
            dialogueText.color = step.ParsedColor;
            StartTyping(currentText);
        }
    }

    public void Hide()
    {
        StopTyping();

        if (speakerText != null)
            speakerText.text = string.Empty;

        if (dialogueText != null)
            dialogueText.text = string.Empty;

        if (root != null)
            root.SetActive(false);
    }

    public void SkipTypingOrRequestNext()
    {
        if (isTyping)
        {
            CompleteTyping();
            return;
        }

        OnClickNext?.Invoke();
    }

    private void StartTyping(string text)
    {
        StopTyping();
        typingCoroutine = StartCoroutine(TypeText(text));
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = string.Empty;

        float interval = 1f / charactersPerSecond;

        for (int i = 0; i < text.Length; i++)
        {
            dialogueText.text += text[i];
            yield return new WaitForSeconds(interval);
        }

        typingCoroutine = null;
        isTyping = false;
    }

    private void CompleteTyping()
    {
        StopTyping();

        if (dialogueText != null)
            dialogueText.text = currentText;
    }

    private void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
    }
}
