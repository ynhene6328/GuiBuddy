using System;

namespace GuiBuddy.Core.Models;

public class AIRequest
{
    public string UserMessage { get; set; } = string.Empty;
    public string SystemInstruction { get; set; } = string.Empty;
    public UiNode? Context { get; set; }

    public AIRequest(string userMessage, UiNode? context = null)
    {
        UserMessage = userMessage;
        Context = context;
    }
}
