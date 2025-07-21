using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TTSPlaybackController : MonoBehaviour
{
    public TextMeshProUGUI buttonText;

    private void Start()
    {
        UpdateButtonText();
    }

    public void OnClickToggle()
    {
        if (AudioPlaybackManager.Instance.IsAudioPlaying())
        {
            AudioPlaybackManager.Instance.PauseAudio();
        }
        else
        {
            AudioPlaybackManager.Instance.PlayAudio();
        }

        UpdateButtonText();
    }

    private void UpdateButtonText()
    {
        buttonText.text = AudioPlaybackManager.Instance.IsAudioPlaying() ? "Pause" : "Play";
    }
}
