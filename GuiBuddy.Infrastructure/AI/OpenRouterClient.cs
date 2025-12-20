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
/// OpenRouter API経由でのAIクライアント実装。
/// 主にNVIDIA Nemotronなどのモデルにアクセスするために使用。
/// </summary>
public class OpenRouterClient : IAIClient
{
    private readonly ISettingsService _settingsService;
    private readonly IResponseParser _responseParser;
    private readonly string _modelName;
    private const string ProviderName = "OpenRouter";
    private const string ApiEndpoint = "https://openrouter.ai/api/v1/chat/completions";
    private static readonly HttpClient _httpClient = new();

    public OpenRouterClient(ISettingsService settingsService, IResponseParser responseParser, string modelName)
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
            return new AIResponse("APIキーが設定されていません。設定画面でOpenRouter APIキーを設定してください。");
        }

        try
        {
            // リクエストボディの構築
            var requestBody = new
            {
                model = _modelName,
                messages = new[]
                {
                    new { role = "user", content = request.UserMessage }
                },
                temperature = 0.7,
                max_tokens = 4096
            };

            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            // リクエストの作成
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ApiEndpoint)
            {
                Content = content
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            // OpenRouter推奨ヘッダ
            httpRequest.Headers.Add("HTTP-Referer", "https://guibuddy.app");
            httpRequest.Headers.Add("X-Title", "GuiBuddy");

            // 送信
            var httpResponse = await _httpClient.SendAsync(httpRequest);
            var responseJson = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
            {
                return new AIResponse($"OpenRouter APIエラー: {httpResponse.StatusCode} - {responseJson}");
            }

            // レスポンスからテキストを抽出 (OpenAI互換フォーマット)
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;
            string responseText = "";

            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var contentElement))
                {
                    responseText = contentElement.GetString() ?? "";
                }
            }

            return _responseParser.Parse(responseText);
        }
        catch (Exception ex)
        {
            return new AIResponse($"OpenRouterとの通信中にエラーが発生しました: {ex.Message}");
        }
    }
}
