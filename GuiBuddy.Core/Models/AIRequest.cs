using System;

namespace GuiBuddy.Core.Models;

public class AIRequest
{
    public string UserMessage { get; init; }
    public string SystemInstruction { get; init; } = string.Empty;
    public UiNode? Context { get; init; }
    public string? UserGoal { get; init; }

    public AIRequest(string userMessage, UiNode? context = null)
    {
        UserMessage = userMessage;
        Context = context;
    }
}
