using System.Threading.Tasks;
using GuiBuddy.Core.Models;
using GuiBuddy.Core.Services;

namespace GuiBuddy.Infrastructure.AI;

public class MockAIClient : IAIClient
{
    public Task<AIResponse> SendAsync(AIRequest request)
    {
        // Simple mock logic for testing
        // If prompt contains "ボタン", assume user wants to find a button.
        // We act as if we found a button with ID 2 (usually a child of root).
        
        // Mock Logic: Input "scroll:123" or "target:123" to target a specific ID
        var message = request.UserMessage;
        var response = new AIResponse("Mock Response");

        if (message.Contains("target:") || message.Contains("scroll:"))
        {
            var parts = message.Split(new[] { ":", "：" }, StringSplitOptions.None);
            if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out int id))
            {
                // JSON Response for verification
                response.ResponseText = $@"```json
{{
  ""explanation"": ""Mock JSON Response for ID {id}"",
  ""targetElementId"": {id},
  ""userGoal"": ""Verify JSON Parsing""
}}
```";
                // Note: We do NOT set response.TargetElementIds here directly.
                // ChatService should parse the JSON and populate it.
                return Task.FromResult(response);
            }
        }

        if (request.UserMessage.Contains("ボタン"))
        {
            response.ResponseText = @"```json
{
  ""explanation"": ""ボタンが見つかりました。「検索」ボタンを操作します。"",
  ""targetElementId"": 2,
  ""userGoal"": ""ボタン操作の確認""
}
```";
            response.Confidence = 0.95;
        }

        return Task.FromResult(response);
    }
}
