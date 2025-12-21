using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GuiBuddy.Core.Models;
using GuiBuddy.Core.Services;

namespace GuiBuddy.App.Services;

public class ChatService : IChatService
{
    private readonly IAIClientFactory _aiClientFactory;
    private readonly IPromptService _promptService;
    private string? _currentUserGoal;

    public ChatService(IAIClientFactory aiClientFactory, IPromptService promptService)
    {
        _aiClientFactory = aiClientFactory;
        _promptService = promptService;
    }

    public async Task<AIResponse> SendMessageAsync(string userMessage, UiNode? appContext)
    {
        // AIRequestの作成
        var request = new AIRequest(userMessage, appContext)
        {
            SystemInstruction =
                "あなたは GUI 操作を支援するアシスタントです。\n" +
                "実際の GUI 操作はユーザが行います。\n" +
                "\n" +
                "【厳守】\n" +
                "- 出力は必ず JSON のみとし、それ以外のテキストは一切出力しないでください\n" +
                "- ハイライト対象は [Current UI Context] に含まれる UI 要素のみから選んでください\n" +
                "- 今回提示するのは「最終目的に向けた次の一手」だけにしてください\n" +
                "- 操作は提案に留め、断定的・命令的な表現は避けてください\n" +
                "\n" +
                "```json\n" +
                "{\n" +
                "  \"explanation\": \"ユーザーへの説明（日本語）\",\n" +
                "  \"targetElementId\": 123,\n" +
                "  \"userGoal\": \"ユーザーの最終目的の要約\"\n" +
                "}\n" +
                "```\n" +
                "\n" +
                "- targetElementId は該当要素が無い場合は null にしてください\n" +
                "\n" +
                "【判断指針】\n" +
                "1. ユーザの発言と [Current USER_GOAL] から、現在の最終目的を解釈する\n" +
                "2. 最終目的を達成するための現実的な方針を検討する\n" +
                "3. 現在の UI 状態で実行可能な「最初の一手」を決める\n" +
                "4. その操作に対応する UI 要素が [Current UI Context] に存在するか確認する\n" +
                "   - 存在する場合：その要素を targetElementId に指定する\n" +
                "   - 存在しない場合：理由を explanation に記載し、targetElementId は null にする\n" +
                "5. userGoal は、必要に応じて簡潔に更新する\n",
            UserGoal = _currentUserGoal
        };

        // プロンプト構築
        string fullPrompt = _promptService.BuildFullPrompt(request);

        // AIクライアントへの送信用リクエストを作成
        var sendRequest = new AIRequest(fullPrompt, null);

        // ファクトリから現在の設定に基づいたクライアントを取得
        var aiClient = _aiClientFactory.CreateFromSettings();

        // AIクライアントへの送信（パース済みのレスポンスが返る）
        AIResponse response = await aiClient.SendAsync(sendRequest);

        // UserGoalの状態更新
        if (!string.IsNullOrWhiteSpace(response.UserGoal))
        {
            _currentUserGoal = response.UserGoal;
        }

        return response;
    }
}

