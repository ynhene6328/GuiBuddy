using System.Collections.Generic;

namespace GuiBuddy.Core.Models;

public class AIResponse
{
    public string ResponseText { get; init; } = string.Empty;
    public IReadOnlyList<int> TargetElementIds { get; init; } = Array.Empty<int>();
    public double? Confidence { get; init; }
    public string? UserGoal { get; init; }
    public string? ContextSummary { get; init; }

    public AIContent? Content { get; init; }

    public AIResponse(string responseText)
    {
        ResponseText = responseText;
    }
}
