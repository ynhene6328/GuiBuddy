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

    public async Task<string> SendMessageAsync(string userMessage, UiNode? appContext)
    {
        // 実際のシナリオでは、appContextをシリアライズしてプロンプトに含めます。
        // Step 5 (Mock) では、ユーザーメッセージをそのまま渡します。
        
        string response = await _aiClient.SendAsync(userMessage);

        // ハイライトコマンドを解析: [HIGHLIGHT:123]
        var match = Regex.Match(response, @"\[HIGHLIGHT:(\d+)\]");
        string displayResponse = response;

        if (match.Success)
        {
            if (int.TryParse(match.Groups[1].Value, out int nodeId))
            {
                // ユーザーの指摘対応: オーバーレイが表示されていない場合に備えて、Showを呼び出す
                if (appContext != null)
                {
                    // ハイライト目的なので、全要素表示(showAll)はfalseにする
                    _overlayService.Show(appContext, showAll: false);
                }
                _overlayService.Highlight(nodeId);
            }

            // 表示用にレスポンスからコマンドを除去（オプションですが、コマンドを隠したほうがUXが良いです）
            // match.Value が空文字でないことは match.Success で保証されていますが、念のため空チェックも可能です
            if (!string.IsNullOrEmpty(match.Value))
            {
                displayResponse = response.Replace(match.Value, "").Trim();
            }
        }

        return displayResponse;
    }
}
