using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class OpenAIUsageLimiter : MonoBehaviour
{
    public static OpenAIUsageLimiter Instance { get; private set; }

    [Header("Call Limits")]
    public int maxCallsPerDay = 50;
    public int maxMessagesInContext = 6;

    private int callsToday;
    private string todayDate;

    private const string DateKey = "OpenAI_LastCallDate";
    private const string CallsKey = "OpenAI_CallsToday";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(this);

        todayDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        string savedDate = PlayerPrefs.GetString(DateKey, todayDate);
        callsToday = PlayerPrefs.GetInt(CallsKey, 0);

        if (savedDate != todayDate)
        {
            callsToday = 0;
            PlayerPrefs.SetString(DateKey, todayDate);
            PlayerPrefs.SetInt(CallsKey, 0);
            PlayerPrefs.Save();
        }
    }

    public bool CanMakeCall()
    {
        return callsToday < maxCallsPerDay;
    }

    public void RegisterCall()
    {
        callsToday++;
        PlayerPrefs.SetInt(CallsKey, callsToday);
        PlayerPrefs.Save();
    }

    public List<ChatMessageWithMeta> TrimContext(List<ChatMessageWithMeta> fullHistory)
    {
        return fullHistory
            .Where(msg => msg.role != "system")
            .TakeLast(maxMessagesInContext)
            .ToList();
    }
}
