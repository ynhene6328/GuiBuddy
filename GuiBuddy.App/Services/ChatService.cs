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

    public async Task<string> SendMessageAsync(string userMessage, UiNode? appContext)
    {
        // AIRequestの作成
        var request = new AIRequest(userMessage, appContext);
        request.SystemInstruction = 
            "あなたは GUI 操作を支援するアシスタントです。\n" +
            "実際の GUI 操作はユーザが行います。\n"+
            "\n"+
            "- ユーザの入力と文脈から「現在の最終目的」を抽出してください\n"+
            "- 必ず [USER_GOAL] ～ [/USER_GOAL] 形式で出力してください\n"+
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
        // ハイライトIDの処理
        if (response.TargetElementIds != null && response.TargetElementIds.Count > 0)
        {
            // ハイライト目的なので、全要素表示(showAll)はfalseにする
            // オーバーレイが表示されていない場合に備えて、Showを呼び出す(rootが必要だが、appContextがrootとは限らない)
            // ここではappContextが表示対象のルートであると仮定するか、別途ルート取得手段が必要
            // いったんappContextがあればそれを表示する
            if (appContext != null)
            {
                _overlayService.Show(appContext, showAll: false);
            }

            foreach (var id in response.TargetElementIds)
            {
                _overlayService.Highlight(id);
            }
        }

        return response.ResponseText;
    }
}
