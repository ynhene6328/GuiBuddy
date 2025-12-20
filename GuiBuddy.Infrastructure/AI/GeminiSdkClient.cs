using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Google.GenAI; // Namespace based on package name, verification needed during build
using GuiBuddy.Core.Models;
using GuiBuddy.Core.Services;

namespace GuiBuddy.Infrastructure.AI;

public class GeminiSdkClient : IAIClient
{
    private readonly ISettingsService _settingsService;
    private const string ProviderName = "Gemini";
    private const string ModelName = "gemini-2.5-flash"; // Cost-effective model

    public GeminiSdkClient(ISettingsService settingsService)
    {
        _settingsService = settingsService;
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
            // Google.GenAI SDKクライアントの初期化
            var client = new Client(apiKey: apiKey);
            
            // プロンプトの準備
            // ChatService側ですでに整形済みのプロンプトがUserMessageに入っている前提
            string promptToSend = request.UserMessage;

            // リクエスト送信
            var response = await client.Models.GenerateContentAsync(
                ModelName, 
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
            
            return new AIResponse(responseText);
        }
        catch (Exception ex)
        {
            return new AIResponse($"Geminiとの通信中にエラーが発生しました: {ex.Message}");
        }
    }
    // BuildPrompt, SummarizeUiTree, SummarizeNodeRecursive removed
}
