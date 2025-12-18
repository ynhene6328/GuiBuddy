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
            "- ユーザの入力と文脈から「現在の最終目的」を抽出してください\n"+
            "- 回答の先頭には必ず、[USER_GOAL]現在の最終目的[/USER_GOAL] を出力してください、「現在の最終目的」は抽出した内容に置き換えてください\n"+
            "- UI 操作が必要だと判断した場合のみ、説明文の最後に [HIGHLIGHT:要素ID] を付けてください\n"+
            "- 操作は提案のみ行い、断定的な命令は避けてください";
        request.UserGoal = _currentUserGoal;

        // AIクライアントへの送信
        AIResponse response = await _aiClient.SendAsync(request);

        // USER_GOAL の更新
        if (!string.IsNullOrWhiteSpace(response.UserGoal))
        {
            _currentUserGoal = response.UserGoal;
        }
        // ChatServiceでのハイライト処理は削除し、ViewModelに任せる
        // これによりViewModel側でスクロール等の制御が可能になる

        return response;
    }
}
