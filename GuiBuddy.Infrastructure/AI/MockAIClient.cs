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

        if (message.Contains("target:"))
        {
            var parts = message.Split(new[] { "target:" }, StringSplitOptions.None);
            if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out int id))
            {
                response.ResponseText = $"Mock Target ID: {id}";
                response.TargetElementIds.Add(id);
                return Task.FromResult(response);
            }
        }
        
        if (message.Contains("scroll:"))
        {
            var parts = message.Split(new[] { "scroll:" }, StringSplitOptions.None);
            if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out int id))
            {
                response.ResponseText = $"Mock Scroll Request ID: {id}";
                response.TargetElementIds.Add(id);
                return Task.FromResult(response);
            }
        }

        if (request.UserMessage.Contains("ボタン"))
        {
            response.ResponseText = "ボタンが見つかりました。「検索」ボタンを操作します。";
            response.TargetElementIds.Add(2);
            response.Confidence = 0.95;
        }

        return Task.FromResult(response);
    }
}
