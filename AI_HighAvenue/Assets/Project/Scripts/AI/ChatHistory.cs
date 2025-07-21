using System;
using System.Collections.Generic;

[Serializable]
public class ChatHistory
{
    public List<ChatMessageWithMeta> messages = new List<ChatMessageWithMeta>();
}

[Serializable]
public class ChatMessageWithMeta
{
    public string role;      // user or assistant
    public string content;
    public string username;  // extra metadata
    public string mood;      // extra metadata
    public string timestamp;
}
