using UnityEngine;

public class AudioPlaybackManager : MonoBehaviour
{
    public static AudioPlaybackManager Instance { get; private set; }

    private AudioSource currentAudioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    public void RegisterAudioSource(AudioSource source)
    {
        if (source == null)
        {
            Debug.LogWarning("🔊 AudioPlaybackManager: Attempted to register null AudioSource");
            return;
        }

        // Don't destroy the previous AudioSource if it's the same one
        if (currentAudioSource != null && currentAudioSource != source)
        {
            Debug.Log("🔊 Stopping previous audio source");
            if (currentAudioSource.isPlaying)
            {
                currentAudioSource.Stop();
            }
            // Don't destroy the AudioSource component, just stop it
        }

        currentAudioSource = source;
        Debug.Log("🔊 Registered new AudioSource: " + source.name);
    }

    public void PlayAudio()
    {
        if (currentAudioSource != null && !currentAudioSource.isPlaying)
        {
            currentAudioSource.Play();
            Debug.Log("🔊 Playing audio via AudioPlaybackManager");
        }
        else if (currentAudioSource == null)
        {
            Debug.LogWarning("🔊 No AudioSource registered to play");
        }
        else if (currentAudioSource.isPlaying)
        {
            Debug.Log("🔊 Audio is already playing");
        }
    }

    public void PauseAudio()
    {
        if (currentAudioSource != null && currentAudioSource.isPlaying)
        {
            currentAudioSource.Pause();
            Debug.Log("🔊 Paused audio");
        }
    }

    public void StopAudio()
    {
        if (currentAudioSource != null && currentAudioSource.isPlaying)
        {
            currentAudioSource.Stop();
            Debug.Log("🔊 Stopped audio");
        }
    }

    public bool IsAudioPlaying()
    {
        return currentAudioSource != null && currentAudioSource.isPlaying;
    }

    public AudioSource GetCurrentAudioSource()
    {
        return currentAudioSource;
    }
}
