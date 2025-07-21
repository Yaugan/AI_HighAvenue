using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.Networking;

public class VoiceToWhisper : MonoBehaviour
{
    public static VoiceToWhisper Instance { get; private set; }

    private AudioClip recordedClip;
    private string micDevice;
    private bool isRecording = false;

    public string whisperApiUrl = "https://api.openai.com/v1/audio/transcriptions";

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    private string ApiKey => OpenAIConfigSingleton.Instance.apiKey; // neat accessor

    public void StartRecording()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("❌ No microphone devices found");
            return;
        }

        micDevice = Microphone.devices[0];
        recordedClip = Microphone.Start(micDevice, true, 300, 16000);
        isRecording = true;

        Debug.Log("🎙️ Started recording");
    }

    public void StopRecordingAndSave()
    {
        if (!isRecording) return;

        int position = Microphone.GetPosition(micDevice);
        Microphone.End(micDevice);
        isRecording = false;

        Debug.Log("🛑 Stopped recording");

        if (position <= 0)
        {
            Debug.LogError("❌ No audio recorded.");
            return;
        }

        // Trim the clip to only what was recorded
        float[] samples = new float[recordedClip.samples * recordedClip.channels];
        recordedClip.GetData(samples, 0);

        float[] trimmedSamples = new float[position * recordedClip.channels];
        System.Array.Copy(samples, trimmedSamples, trimmedSamples.Length);

        AudioClip trimmedClip = AudioClip.Create("TrimmedClip", position, recordedClip.channels, recordedClip.frequency, false);
        trimmedClip.SetData(trimmedSamples, 0);

        // Save trimmed clip
        string path = Path.Combine(Application.persistentDataPath, "recorded_audio.wav");
        SaveWavFile(trimmedClip, path);
        Debug.Log("💾 Trimmed WAV saved: " + path);

        StartCoroutine(SendToWhisper(path));
    }

    private void SaveWavFile(AudioClip clip, string path)
    {
        if (clip == null)
        {
            Debug.LogError("❌ Cannot save null AudioClip.");
            return;
        }

        byte[] wavData = WavUtility.FromAudioClip(clip);
        File.WriteAllBytes(path, wavData);
    }

    private IEnumerator SendToWhisper(string filePath)
    {
        Debug.Log("📡 Sending audio to Whisper...");

        if (!File.Exists(filePath))
        {
            Debug.LogError("❌ Audio file not found: " + filePath);
            yield break;
        }

        byte[] audioData = File.ReadAllBytes(filePath);

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", audioData, "recorded_audio.wav", "audio/wav");
        form.AddField("model", "whisper-1");

        using (UnityWebRequest www = OpenAIRequestUtils.CreateFormPost(whisperApiUrl, form, ApiKey))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("❌ Whisper API error: " + www.error);
                Debug.LogError(www.downloadHandler.text);
            }
            else
            {
                string json = www.downloadHandler.text;
                Debug.Log("✅ Whisper response: " + json);

                string transcribedText = ExtractTextFromWhisperResponse(json);

                if (!string.IsNullOrEmpty(transcribedText))
                {
                    Debug.Log("📝 Transcribed Text: " + transcribedText);
                    ChatgptClient.Instance.AskChatGPT(transcribedText);
                }
                else
                {
                    Debug.LogWarning("⚠️ No transcription found in Whisper response.");
                }               
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log("🧹 Temp WAV file deleted: " + filePath);
            }
        }

       
    }

    private string ExtractTextFromWhisperResponse(string json)
    {
        // crude parse: expects JSON like { "text": "hello there" }
        int start = json.IndexOf("\"text\":");
        if (start == -1) return null;

        int firstQuote = json.IndexOf('"', start + 7);
        int secondQuote = json.IndexOf('"', firstQuote + 1);

        if (firstQuote == -1 || secondQuote == -1) return null;

        return json.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
    }
}
