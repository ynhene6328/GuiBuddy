using System;
using System.Threading.Tasks;
using GuiBuddy.Core.Models;
using GuiBuddy.Core.Services;

namespace GuiBuddy.Infrastructure.AI;

public class MockAIClient : IAIClient
{
    private readonly IResponseParser _responseParser;

    public MockAIClient(IResponseParser responseParser)
    {
        _responseParser = responseParser;
    }

    public Task<AIResponse> SendAsync(AIRequest request)
    {
        var message = request.UserMessage;

        // Mock JSON responses for testing
        string mockJsonResponse;

        if (message.Contains("target:") || message.Contains("scroll:"))
        {
            var parts = message.Split(new[] { ":", "：" }, StringSplitOptions.None);
            if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out int id))
            {
                mockJsonResponse = $@"{{
  ""explanation"": ""Mock JSON Response for ID {id}"",
  ""targetElementId"": {id},
  ""userGoal"": ""Verify JSON Parsing""
}}";
                return Task.FromResult(_responseParser.Parse(mockJsonResponse));
            }
        }

        if (message.Contains("ボタン"))
        {
            mockJsonResponse = @"{
  ""explanation"": ""ボタンが見つかりました。「検索」ボタンを操作します。"",
  ""targetElementId"": 2,
  ""userGoal"": ""ボタン操作の確認""
}";
            return Task.FromResult(_responseParser.Parse(mockJsonResponse));
        }

        // デフォルトレスポンス
        mockJsonResponse = @"{
  ""explanation"": ""Mock Response"",
  ""targetElementId"": null,
  ""userGoal"": ""テスト""
}";
        return Task.FromResult(_responseParser.Parse(mockJsonResponse));
    }
}
