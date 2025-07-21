using UnityEngine;

[CreateAssetMenu(fileName = "OpenAIConfig", menuName = "Config/OpenAIConfig")]
public class OpenAIConfig : ScriptableObject
{
    [Header("🔐 OpenAI API Key")]
    [TextArea]
    public string apiKey = "YOUR_API_KEY_HERE";
}
