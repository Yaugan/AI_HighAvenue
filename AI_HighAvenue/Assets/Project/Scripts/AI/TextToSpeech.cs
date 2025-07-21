using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using static Unity.VisualScripting.Member;

[System.Serializable]
public class TTSRequest
{
    public string model;
    public string input;
    public string voice;
    public string response_format;
}

public class TextToSpeech : MonoBehaviour
{
    public static TextToSpeech Instance { get; private set; }

    private string ttsApiUrl = "https://api.openai.com/v1/audio/speech";
    private AudioSource audioSource;
    private bool isProcessingTTS = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
        
        // Ensure we have an AudioSource
        audioSource = GetComponent<AudioSource>();
        if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
    }

    private string ApiKey => OpenAIConfigSingleton.Instance.apiKey; // neat accessor

    public void Speak(string text)
    {
        if (isProcessingTTS)
        {
            Debug.LogWarning("🔊 TTS already processing, skipping new request");
            return;
        }
        
        StartCoroutine(SendTextToSpeech(text));
    }

    private IEnumerator SendTextToSpeech(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.LogWarning("TTS input text is empty.");
            yield break;
        }

        isProcessingTTS = true;
        Debug.Log("🔊 Starting TTS processing for: " + text.Substring(0, Mathf.Min(50, text.Length)) + "...");

        TTSRequest request = new TTSRequest
        {
            model = "tts-1",
            input = text,
            voice = "nova", // or alloy, echo, fable, onyx, shimmer
            response_format = "mp3"
        };
        string jsonBody = JsonUtility.ToJson(request);

        using (UnityWebRequest www = OpenAIRequestUtils.CreatePost(ttsApiUrl, jsonBody, ApiKey))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("TTS API Error: " + www.error);
                isProcessingTTS = false;
                yield break;
            }

            byte[] mp3Data = www.downloadHandler.data;
            string filePath = Path.Combine(Application.persistentDataPath, "tts_output.mp3");
            
            // Save the file with error handling
            if (!SaveTTSFile(filePath, mp3Data))
            {
                isProcessingTTS = false;
                yield break;
            }
            
            Debug.Log("🔊 TTS MP3 saved: " + filePath);
            
            // Play the MP3
            yield return StartCoroutine(PlayMP3(filePath));
        }
    }

    private IEnumerator PlayMP3(string filePath)
    {
        Debug.Log("🔊 Loading audio clip from: " + filePath);
        
        // Check if file exists before trying to load it
        if (!File.Exists(filePath))
        {
            Debug.LogError("🔊 TTS file not found: " + filePath);
            isProcessingTTS = false;
            yield break;
        }
        
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to load MP3: " + www.error);
                isProcessingTTS = false;
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            if (clip == null)
            {
                Debug.LogError("🔊 AudioClip is null after loading");
                isProcessingTTS = false;
                yield break;
            }

            Debug.Log("🔊 Audio Clip Loaded - Length: " + clip.length + "s, Channels: " + clip.channels);
            
            // Stop any currently playing audio
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            
            // Clear previous clip and set new one
            audioSource.clip = clip;
            AudioPlaybackManager.Instance.RegisterAudioSource(audioSource);
            
            // Start speaking animation when TTS begins
            if (ChatModelAnimator.Instance != null)
            {
                ChatModelAnimator.Instance.PlaySpeaking();
            }
            
            Debug.Log("🔊 Starting audio playback...");
            audioSource.Play();
            
            // Wait for audio to finish, then stop speaking animation
            yield return new WaitForSeconds(clip.length);
            
            Debug.Log("🔊 Audio playback finished");
            
            if (ChatModelAnimator.Instance != null)
            {
                ChatModelAnimator.Instance.StopSpeaking();
            }
            
            // Clean up
            isProcessingTTS = false;
            
            // Clean up the file
            CleanupTTSFile(filePath);
        }
    }
    
    private bool SaveTTSFile(string filePath, byte[] data)
    {
        try
        {
            File.WriteAllBytes(filePath, data);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("🔊 Error saving TTS file: " + e.Message);
            return false;
        }
    }
    
    private void CleanupTTSFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log("🔊 Cleaned up TTS file: " + filePath);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("🔊 Could not clean up TTS file: " + e.Message);
        }
    }
}
