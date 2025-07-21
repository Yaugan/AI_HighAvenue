using UnityEngine;
using Firebase.RemoteConfig;
using System.Threading.Tasks;
using System;
using Firebase.Extensions;

public class remoteConfigScript : MonoBehaviour
{
    private void Awake()
    {
        CheckRemoteConfigValues();
    }

    public Task CheckRemoteConfigValues()
    {
        Debug.Log("Fetching Data...");
        Task fetchTask = FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero);
        return fetchTask.ContinueWithOnMainThread(FetchComplete);
    }

    private void FetchComplete(Task fetchTask)
    {
        if (!fetchTask.IsCompleted)
        {
            Debug.LogError("Retrieval hasn't finished.");
        }

        var remoteConfig = FirebaseRemoteConfig.DefaultInstance;
        var info = remoteConfig.Info;
        if(info.LastFetchStatus != LastFetchStatus.Success)
        {
            Debug.LogError($"{nameof(FetchComplete)} was unsuccessful \n {nameof(info.LastFetchStatus)} : {info.LastFetchStatus}");
            return;
        }

        // Fetch successful. Parameter values must be activated before they can be used.
        remoteConfig.ActivateAsync().ContinueWithOnMainThread(task => {
            Debug.Log($"Remote data loaded and ready to use. Last fetch time : {DateTime.Now}");
            print("Total Values : " + remoteConfig.AllValues.Count);
        
        });
    }
}
