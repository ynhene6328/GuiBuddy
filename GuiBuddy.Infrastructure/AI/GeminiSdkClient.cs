using System;
using System.Text;
using System.Threading.Tasks;
using Google.GenAI;
using GuiBuddy.Core.Models;
using GuiBuddy.Core.Services;

namespace GuiBuddy.Infrastructure.AI;

public class GeminiSdkClient : IAIClient
{
    private readonly ISettingsService _settingsService;
    private readonly IResponseParser _responseParser;
    private readonly string _modelName;
    private const string ProviderName = "Gemini";

    public GeminiSdkClient(ISettingsService settingsService, IResponseParser responseParser, string modelName = "gemini-2.5-flash")
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
            return new AIResponse("APIキーが設定されていません。設定画面でGemini APIキーを設定してください。");
        }

        try
        {
            var client = new Client(apiKey: apiKey);
            
            // プロンプトはChatService側で既に構築済み
            string promptToSend = request.UserMessage;

            var response = await client.Models.GenerateContentAsync(
                _modelName, 
                promptToSend
            );

            // レスポンスからテキストを抽出
            string responseText = "";
            if (response.Candidates != null && response.Candidates.Count > 0)
            {
                var first = response.Candidates[0];
                if (first.Content != null && first.Content.Parts != null && first.Content.Parts.Count > 0)
                {
                    responseText = first.Content.Parts[0].Text ?? "";
                }
            }
            
            // パーサーを使用して構造化レスポンスを返す
            return _responseParser.Parse(responseText);
        }
        catch (Exception ex)
        {
            return new AIResponse($"Geminiとの通信中にエラーが発生しました: {ex.Message}");
        }
    }
}
