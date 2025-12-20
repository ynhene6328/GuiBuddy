using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GuiBuddy.Core.Models;
using GuiBuddy.Core.Services;

namespace GuiBuddy.Infrastructure.AI;

/// <summary>
/// OpenAI APIクライアントの実装。
/// </summary>
public class OpenAIClient : IAIClient
{
    private readonly ISettingsService _settingsService;
    private readonly IResponseParser _responseParser;
    private readonly string _modelName;
    private const string ProviderName = "OpenAI";
    private static readonly HttpClient _httpClient = new();

    public OpenAIClient(ISettingsService settingsService, IResponseParser responseParser, string modelName)
    {
        _settingsService = settingsService;
        _responseParser = responseParser;
        _modelName = modelName;
    }

    public async Task<AIResponse> SendAsync(AIRequest request)
    {
        string? apiKey = _settingsService.GetApiKey(ProviderName);
        if (string.IsNullOrEmpty(apiKey))
        {
            return new AIResponse("APIキーが設定されていません。設定画面でOpenAI APIキーを設定してください。");
        }

        try
        {
            // OpenAI Chat Completions API
            var requestBody = new
            {
                model = _modelName,
                messages = new[]
                {
                    new { role = "user", content = request.UserMessage }
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var httpResponse = await _httpClient.SendAsync(httpRequest);
            var responseJson = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
            {
                return new AIResponse($"OpenAI APIエラー: {httpResponse.StatusCode} - {responseJson}");
            }

            // レスポンスからテキストを抽出
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;
            string responseText = "";

            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var content))
                {
                    responseText = content.GetString() ?? "";
                }
            }

            return _responseParser.Parse(responseText);
        }
        catch (Exception ex)
        {
            return new AIResponse($"OpenAIとの通信中にエラーが発生しました: {ex.Message}");
        }
    }
}
