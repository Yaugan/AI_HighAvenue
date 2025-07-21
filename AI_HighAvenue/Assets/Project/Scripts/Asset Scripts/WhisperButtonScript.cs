using UnityEngine;
using TMPro;

public class WhisperButtonScript : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI buttonText; // Drag the button's label here in the Inspector

    private bool isRecording = false;

    public void OnRecordButtonClick()
    {
        if (VoiceToWhisper.Instance == null)
        {
            Debug.LogError("❌ VoiceToWhisper instance is missing.");
            return;
        }

        if (!isRecording)
        {
            VoiceToWhisper.Instance.StartRecording();
            buttonText.text = "Stop Recording";
            isRecording = true;
        }
        else
        {
            VoiceToWhisper.Instance.StopRecordingAndSave();
            buttonText.text = "Start Recording";
            isRecording = false;
        }
    }
}
