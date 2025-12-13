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
        
        var response = new AIResponse("はい、何でしょうか？ 具体的な操作を指示してください。");

        if (request.UserMessage.Contains("ボタン"))
        {
            response.ResponseText = "ボタンが見つかりました。「検索」ボタンを操作します。";
            response.TargetElementIds.Add(2);
            response.Confidence = 0.95;
        }

        return Task.FromResult(response);
    }
}
