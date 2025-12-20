using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GuiBuddy.Core.Models;
using GuiBuddy.Core.Services;

namespace GuiBuddy.App.Services;

public class ChatService : IChatService
{
    private readonly IAIClient _aiClient;
    private readonly IOverlayService _overlayService;
    private string? _currentUserGoal;

    public ChatService(IAIClient aiClient, IOverlayService overlayService)
    {
        _aiClient = aiClient;
        _overlayService = overlayService;
    }

    public async Task<AIResponse> SendMessageAsync(string userMessage, UiNode? appContext)
    {
        // AIRequestの作成
        var request = new AIRequest(userMessage, appContext);
        request.SystemInstruction = 
            "あなたは GUI 操作を支援するアシスタントです。\n" +
            "実際の GUI 操作はユーザが行います。\n"+
            "\n"+
            "【重要】以下のJSONフォーマットのみで応答してください。それ以外のテキストは出力しないでください。\n"+
            "```json\n"+
            "{\n"+
            "  \"explanation\": \"ユーザーへの説明メッセージ（日本語）\",\n"+
            "  \"targetElementId\": 123, // ハイライト対象のUiNode.Id (ない場合は null, 複数の場合は代表ID)\n"+
            "  \"userGoal\": \"ユーザーの最終目的の要約（文脈から推測されるゴール）\"\n"+
            "}\n"+
            "```\n"+
            "\n"+
            "- `explanation`: 操作提案や回答をここに記述します。断定的な命令は避けてください。\n"+
            "- `targetElementId`: 操作対象となるUI要素のIDを整数で指定します。存在しない場合は null にしてください。\n"+
            "- `userGoal`: ユーザーの意図を汲み取り、現在のゴールを短く更新してください。";
        
        request.UserGoal = _currentUserGoal;

        // AIクライアントへの送信
        AIResponse rawResponse = await _aiClient.SendAsync(request);

        // JSON解析と構造化データの構築
        try 
        {
            var content = ParseJsonContent(rawResponse.ResponseText);
            if (content != null)
            {
                rawResponse.Content = content;
                rawResponse.ResponseText = content.Explanation; // 互換性のためにテキストもセット
                
                // TargetIdのセット（リストへの変換）
                rawResponse.TargetElementIds.Clear();
                if (content.TargetElementId.HasValue)
                {
                    rawResponse.TargetElementIds.Add(content.TargetElementId.Value);
                }

                // UserGoalの更新
                if (!string.IsNullOrWhiteSpace(content.UserGoal))
                {
                    rawResponse.UserGoal = content.UserGoal;
                    _currentUserGoal = content.UserGoal;
                }
            }
        }
        catch 
        {
            // パース失敗時のフォールバック: 生のレスポンスをそのまま使用
            // 既存のAIClientの処理（[HIGHLIGHT]等のパース）に依存するか、
            // ここで最低限のテキストとして扱う。
            // AIClient内ですでに [HIGHLIGHT] パースは行われているため、TargetElementIdsなどはセットされている可能性がある。
            // JSONパース失敗 = おそらく平文で返ってきた -> そのまま表示
            rawResponse.Content = new AIContent 
            { 
                Explanation = rawResponse.ResponseText,
                UserGoal = rawResponse.UserGoal ?? _currentUserGoal ?? ""
            };
        }

        return rawResponse;
    }

    private AIContent? ParseJsonContent(string rawText)
    {
        try
        {
            // マークダウンの ```json ブロックを除去して抽出
            string jsonString = rawText;
            var match = Regex.Match(rawText, @"```json\s*(.*?)\s*```", RegexOptions.Singleline);
            if (match.Success)
            {
                jsonString = match.Groups[1].Value;
            }
            else
            {
                 // ``` なしのJSONの場合も考慮して、最初の { から 最後の } までを抽出
                 int start = rawText.IndexOf('{');
                 int end = rawText.LastIndexOf('}');
                 if (start >= 0 && end > start)
                 {
                     jsonString = rawText.Substring(start, end - start + 1);
                 }
            }

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            
            return System.Text.Json.JsonSerializer.Deserialize<AIContent>(jsonString, options);
        }
        catch
        {
            return null;
        }
    }
}
