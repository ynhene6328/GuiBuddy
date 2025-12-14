using System.Collections.Generic;

namespace GuiBuddy.Core.Models;

public class AIResponse
{
    public string ResponseText { get; set; } = string.Empty;
    public List<int> TargetElementIds { get; set; } = new();
    public double? Confidence { get; set; }
    public string? UserGoal { get; set; }

    public AIResponse(string responseText)
    {
        ResponseText = responseText;
    }
}
