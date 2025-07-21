using UnityEngine.Networking;
using System.Text;
using UnityEngine;

public static class OpenAIRequestUtils
{
    public static UnityWebRequest CreatePost(string url, string jsonBody, string apiKey)
    {
        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        return request;
    }

    public static UnityWebRequest CreateFormPost(string url, WWWForm form, string apiKey)
    {
        var request = UnityWebRequest.Post(url, form);
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        return request;
    }
}