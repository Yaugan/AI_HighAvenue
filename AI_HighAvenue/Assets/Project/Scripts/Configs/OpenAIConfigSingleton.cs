using UnityEngine;

public class OpenAIConfigSingleton : MonoBehaviour
{
    public static OpenAIConfig Instance { get; private set; }

    [SerializeField] private OpenAIConfig config;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = config;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
