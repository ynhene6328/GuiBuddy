using GuiBuddy.Core.Models;

namespace GuiBuddy.Core.Services;

/// <summary>
/// AIへの送信プロンプトを構築するサービスインターフェース。
/// </summary>
public interface IPromptService
{
    /// <summary>
    /// AIリクエスト情報から、送信用の完全なプロンプト文字列を構築します。
    /// </summary>
    /// <param name="request">AIリクエスト情報</param>
    /// <returns>構築されたプロンプト文字列</returns>
    string BuildFullPrompt(AIRequest request);
}
