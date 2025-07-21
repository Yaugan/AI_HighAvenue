using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

public class ChatgptClient : MonoBehaviour
{
    public static ChatgptClient Instance { get; private set; }

    private string chatGptUrl = "https://api.openai.com/v1/chat/completions";
    //private string moderationUrl = "https://api.openai.com/v1/moderations";

    private string historyFilePath;
    private ChatHistory chatHistory;

    public string latestAIResponse = "";

    //[Header("Prompt Input")]
    //public string userInput = "Hello AI!";

    // Replace with real user data later
    private string currentUserName = "Kiddo";
    private string currentUserMood = "curious";

    private string ApiKey => OpenAIConfigSingleton.Instance.apiKey; // neat accessor

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this);

        historyFilePath = Path.Combine(Application.persistentDataPath, "chat_history.json");
        LoadHistoryOrInitialize();
    }

    public void AskChatGPT(string input)
    {
        if (!OpenAIUsageLimiter.Instance.CanMakeCall())
        {
            Debug.LogWarning("🛑 Daily OpenAI API call limit reached.");
            return;
        }

        StartCoroutine(ModerateAndSend(input));
    }

    private IEnumerator ModerateAndSend(string input)
    {
        ////// Step 1: Moderate the input first
        //string modJson = JsonUtility.ToJson(new ModerationRequest { input = input });

        //using (UnityWebRequest modRequest = OpenAIRequestUtils.CreatePost(moderationUrl, modJson, ApiKey))
        //{
        //    yield return modRequest.SendWebRequest();
        //    yield return new WaitForSeconds(1f);

        //    if (modRequest.result != UnityWebRequest.Result.Success)
        //    {
        //        Debug.LogError("Moderation Failed: " + modRequest.error);
        //        yield break;
        //    }

        //    string responseText = modRequest.downloadHandler.text;
        //    if (responseText.Contains("\"flagged\":true"))
        //    {
        //        Debug.LogWarning("Input flagged by moderation API. Message is not safe for kids.");
        //        yield break;
        //    }
        //}

        // Step 2: If safe, send to ChatGPT

        OpenAIUsageLimiter.Instance.RegisterCall();

        // Add user's message to history
        chatHistory.messages.Add(new ChatMessageWithMeta
        {
            role = "user",
            content = input,
            username = currentUserName,
            mood = currentUserMood,
            timestamp = DateTime.UtcNow.ToString("o")
        });

        // Build message list with trimming to save tokens
        var trimmed = OpenAIUsageLimiter.Instance.TrimContext(chatHistory.messages);


        var messagesForOpenAI = new List<object>
        {
            new
            {
                role = "system",
                content = "You are a friendly, wise, and playful companion for kids. Think of yourself as a mix of a teacher, a friend, and a gentle guardian — someone who plays with children, fuels their curiosity, and helps them grow. Always provide responses that are safe, age-appropriate, and positive. Avoid and redirect topics involving hate, violence, harassment, or anything inappropriate. Use gentle wording and ensure your tone is nurturing, encouraging, and kind. Help children learn good habits, explore interesting topics, and stay curious about the world. Suggest fun challenges or questions to make learning interactive. Be someone they can trust and look up to when their parents aren’t around. Guide them in becoming kind, thoughtful, and curious individuals."
            }
        };

        foreach (var msg in trimmed)
        {
            messagesForOpenAI.Add(new
            {
                role = msg.role,
                content = msg.content
            });
        }

        var chatPayload = new
        {
            model = "gpt-3.5-turbo",
            messages = messagesForOpenAI
        };

        string chatJson = JsonConvert.SerializeObject(chatPayload);

        // Start thinking animation when sending request to ChatGPT
        if (ChatModelAnimator.Instance != null)
        {
            ChatModelAnimator.Instance.PlayThinking();
        }

        using (UnityWebRequest chatRequest = OpenAIRequestUtils.CreatePost(chatGptUrl, chatJson, ApiKey))
        {
            yield return chatRequest.SendWebRequest();

            if (chatRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("ChatGPT Error: " + chatRequest.error);
                // Stop thinking animation on error
                if (ChatModelAnimator.Instance != null)
                {
                    ChatModelAnimator.Instance.PlayIdle();
                }
                yield break;
            }

            // Parse response
            var response = JsonConvert.DeserializeObject<ChatGPTResponseWrapper>(chatRequest.downloadHandler.text);
            latestAIResponse = response.choices[0].message.content;
            Debug.Log("🤖 ChatGPT: " + latestAIResponse);

            // Save assistant reply
            chatHistory.messages.Add(new ChatMessageWithMeta
            {
                role = "assistant",
                content = latestAIResponse,
                username = "AI",
                mood = "helpful",
                timestamp = DateTime.UtcNow.ToString("o")
            });

            SaveHistoryToFile();

            // 🔊 Trigger TTS
            TextToSpeech.Instance.Speak(latestAIResponse);
        }
    }

    private void LoadHistoryOrInitialize()
    {
        if (File.Exists(historyFilePath))
        {
            try
            {
                byte[] encryptedData = File.ReadAllBytes(historyFilePath);
                string decryptedJson = EncryptionUtils.DecryptStringFromBytes(encryptedData);
                chatHistory = JsonConvert.DeserializeObject<ChatHistory>(decryptedJson) ?? new ChatHistory();
            }
            catch (Exception e)
            {
                Debug.LogError("🔓 Failed to decrypt or load chat history. Starting fresh. Error: " + e.Message);
                chatHistory = new ChatHistory();
            }
        }
        else
        {
            chatHistory = new ChatHistory();
        }
    }

    private void SaveHistoryToFile()
    {
        try
        {
            string json = JsonConvert.SerializeObject(chatHistory, Formatting.Indented);
            byte[] encryptedData = EncryptionUtils.EncryptStringToBytes(json);
            File.WriteAllBytes(historyFilePath, encryptedData);
        }
        catch (Exception e)
        {
            Debug.LogError("🔐 Failed to save encrypted chat history: " + e.Message);
        }
    }

    public void ResetHistory()
    {
        chatHistory = new ChatHistory();
        File.Delete(historyFilePath);
        Debug.Log("🔄 Chat history reset.");
    }

    // Optional: Set user metadata dynamically
    public void SetUserMetadata(string username, string mood)
    {
        currentUserName = username;
        currentUserMood = mood;
    }

}

