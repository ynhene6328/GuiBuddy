using GuiBuddy.Core.Models;

namespace GuiBuddy.Core.Services;

/// <summary>
/// AIからの生テキスト応答をパースして構造化データに変換するサービス。
/// </summary>
public interface IResponseParser
{
    /// <summary>
    /// 生テキストをパースして構造化されたAIResponseを生成します。
    /// </summary>
    /// <param name="rawText">AIからの生レスポンステキスト</param>
    /// <returns>構造化されたAIResponse</returns>
    AIResponse Parse(string rawText);
}
