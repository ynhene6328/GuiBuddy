using System.Text.Json.Serialization;

namespace GuiBuddy.Core.Models;

/// <summary>
/// AIからの構造化された応答内容を表します。
/// </summary>
public class AIContent
{
    /// <summary>
    /// ユーザーへの説明メッセージ（日本語）
    /// </summary>
    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// ハイライト対象のUI要素ID (UiNode.Id)
    /// </summary>
    [JsonPropertyName("targetElementId")]
    public int? TargetElementId { get; set; }

    /// <summary>
    /// ユーザーの最終目的の要約
    /// </summary>
    [JsonPropertyName("userGoal")]
    public string UserGoal { get; set; } = string.Empty;

    /// <summary>
    /// これまでの会話の要点と文脈の要約
    /// </summary>
    [JsonPropertyName("contextSummary")]
    public string ContextSummary { get; set; } = string.Empty;
}
