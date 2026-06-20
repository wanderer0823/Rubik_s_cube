using System;
using UnityEngine;

public enum DialogueStepType
{
    Text,
    WaitForEvent,
    Delay,
    ShowTips
}

[Serializable]
public class DialogueStepData
{
    public string speaker;
    public string text;
    public string color = "#FFFFFFFF";
    public string stepType = "Text";
    public string eventId;
    public float delayTime;
    public string requiredFlag;
    public string completeFlag;

    public Color ParsedColor
    {
        get
        {
            if (string.IsNullOrWhiteSpace(color))
                return Color.white;

            if (ColorUtility.TryParseHtmlString(color, out Color parsedColor))
                return parsedColor;

            Debug.LogWarning($"Tutorial dialogue color is invalid: {color}. Fallback to white.");
            return Color.white;
        }
    }

    public DialogueStepType ParsedStepType
    {
        get
        {
            if (string.IsNullOrWhiteSpace(stepType))
                return DialogueStepType.Text;

            if (Enum.TryParse(stepType, true, out DialogueStepType parsedStepType))
                return parsedStepType;

            Debug.LogWarning($"Tutorial dialogue stepType is invalid: {stepType}. Fallback to Text.");
            return DialogueStepType.Text;
        }
    }
}

[Serializable]
public class DialogueJsonFile
{
    public int roomId;
    public DialogueStepData[] steps;
}
