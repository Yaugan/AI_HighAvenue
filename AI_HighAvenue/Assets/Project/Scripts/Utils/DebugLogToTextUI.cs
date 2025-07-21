using TMPro;
using UnityEngine;

public class DebugLogToTextUI : MonoBehaviour
{
    public TextMeshProUGUI logText;
    public int maxLines = 30;

    private string logOutput = "";

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Only handle standard logs and errors
        if (type == LogType.Log)
        {
            logOutput += $"📘 {logString}\n";
        }
        else if (type == LogType.Error)
        {
            logOutput += $"❌ {logString}\n";
        }
        else
        {
            return; // Ignore Warnings, Exceptions, and Asserts
        }

        // Trim to last maxLines lines
        string[] lines = logOutput.Split('\n');
        if (lines.Length > maxLines)
        {
            logOutput = string.Join("\n", lines[^maxLines..]);
        }

        logText.text = logOutput;
    }
}
