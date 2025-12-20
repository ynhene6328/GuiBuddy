using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GuiBuddy.Core.Models;
using GuiBuddy.Core.Services;

namespace GuiBuddy.App.Services;

public class ChatService : IChatService
{
    private readonly IAIClient _aiClient;
    private readonly IPromptService _promptService;
    private string? _currentUserGoal;

    public ChatService(IAIClient aiClient, IPromptService promptService)
    {
        _aiClient = aiClient;
        _promptService = promptService;
    }

    public async Task<AIResponse> SendMessageAsync(string userMessage, UiNode? appContext)
    {
        // AIRequestの作成
        var request = new AIRequest(userMessage, appContext)
        {
            SystemInstruction = 
                "あなたは GUI 操作を支援するアシスタントです。\n" +
                "実際の GUI 操作はユーザが行います。\n"+
                "\n"+
                "【重要】以下のJSONフォーマットのみで応答してください。それ以外のテキストは出力しないでください。\n"+
                "```json\n"+
                "{\n"+
                "  \"explanation\": \"ユーザーへの説明メッセージ（日本語）\",\n"+
                "  \"targetElementId\": 123, // ハイライト対象のUiNode.Id (ない場合は null)\n"+
                "  \"userGoal\": \"ユーザーの最終目的の要約\"\n"+
                "}\n"+
                "```\n"+
                "\n"+
                "- `explanation`: 操作提案や回答をここに記述します。断定的な命令は避けてください。\n"+
                "- `targetElementId`: 操作対象となるUI要素のIDを整数で指定します。存在しない場合は null にしてください。\n"+
                "- `userGoal`: ユーザーの意図を汲み取り、現在のゴールを短く更新してください。",
            UserGoal = _currentUserGoal
        };

        // プロンプト構築
        string fullPrompt = _promptService.BuildFullPrompt(request);

        // AIクライアントへの送信用リクエストを作成
        var sendRequest = new AIRequest(fullPrompt, null);

        // AIクライアントへの送信（パース済みのレスポンスが返る）
        AIResponse response = await _aiClient.SendAsync(sendRequest);

        // UserGoalの状態更新
        if (!string.IsNullOrWhiteSpace(response.UserGoal))
        {
            _currentUserGoal = response.UserGoal;
        }

        return response;
    }
}

