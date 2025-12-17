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
            "あなたはGUI操作アシスタントです。提供された[Current UI Context]を分析し、ユーザーの要望に最も合致するUI要素を特定してください。\n" +
            "回答の最後には必ず、特定した要素のIDを `[HIGHLIGHT:数値]` の形式で付記してください。\n" +
            "例: 「保存ボタンはこちらです。[HIGHLIGHT:123]」\n" +
            "該当する要素がない場合は、その理由を説明してください。";
        
        // AIクライアントへの送信
        AIResponse response = await _aiClient.SendAsync(request);

        // ChatServiceでのハイライト処理は削除し、ViewModelに任せる
        // これによりViewModel側でスクロール等の制御が可能になる

        return response;
    }
}
