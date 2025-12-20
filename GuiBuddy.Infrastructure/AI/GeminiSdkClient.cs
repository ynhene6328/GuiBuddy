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
            
            // プロンプトの構築
            var fullPrompt = BuildPrompt(request);

            // リクエスト送信
            // gemini-2.5-flash モデルを使用してコンテンツを生成
            var response = await client.Models.GenerateContentAsync(
                ModelName, 
                fullPrompt
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

    private string BuildPrompt(AIRequest request)
    {
        var sb = new StringBuilder();

        // System Instruction
        if (!string.IsNullOrEmpty(request.SystemInstruction))
        {
            sb.AppendLine("[System Instruction]");
            sb.AppendLine(request.SystemInstruction);
            sb.AppendLine();
        }

        // USER_GOAL
        if (!string.IsNullOrEmpty(request.UserGoal))
        {
            sb.AppendLine("[Current USER_GOAL]");
            sb.AppendLine(request.UserGoal);
            sb.AppendLine();
        }
        // Context (UI Elements)
        if (request.Context != null)
        {
            sb.AppendLine("[Current UI Context]");
            sb.AppendLine(SummarizeUiTree(request.Context));
            sb.AppendLine();
        }

        // User Message
        sb.AppendLine("[User Request]");
        sb.AppendLine(request.UserMessage);

        return sb.ToString();
    }

    private string SummarizeUiTree(UiNode root)
    {
        // Simple BFS/DFS to list simplified elements
        // Limit depth or count to avoid token overflow if necessary
        var sb = new StringBuilder();
        SummarizeNodeRecursive(root, sb, 0);
        return sb.ToString();
    }

    private void SummarizeNodeRecursive(UiNode node, StringBuilder sb, int depth)
    {
        // Filter mainly interesting elements (Control, Button, Edit, etc.)
        // For now, dump all non-pane structural nodes if possible
        string indent = new string(' ', depth * 2);
        sb.AppendLine($"{indent}- ID:{node.Id}, Type:{node.Type}, Name:\"{node.Name}\", Hint:\"{node.Hint}\"");

        foreach (var child in node.Children)
        {
            SummarizeNodeRecursive(child, sb, depth + 1);
        }
    }
}
