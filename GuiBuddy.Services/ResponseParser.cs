using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using GuiBuddy.Core.Models;
using GuiBuddy.Core.Services;

namespace GuiBuddy.Services;

/// <summary>
/// AIからの生テキスト応答をパースして構造化データに変換するサービスの実装。
/// </summary>
public class ResponseParser : IResponseParser
{
    public AIResponse Parse(string rawText)
    {
        try
        {
            var content = ParseJsonContent(rawText);
            if (content != null)
            {
                // TargetIdのリスト作成
                var targetIds = new List<int>();
                if (content.TargetElementId.HasValue)
                {
                    targetIds.Add(content.TargetElementId.Value);
                }

                // 構造化されたAIResponseを作成
                return new AIResponse(content.Explanation)
                {
                    Content = content,
                    TargetElementIds = targetIds,
                    UserGoal = content.UserGoal,
                    ContextSummary = content.ContextSummary
                };
            }
        }
        catch
        {
            // パース失敗時はフォールバック
        }

        // フォールバック: 生テキストをそのまま返す
        return new AIResponse(rawText)
        {
            Content = new AIContent
            {
                Explanation = rawText,
                UserGoal = string.Empty
            }
        };
    }

    private AIContent? ParseJsonContent(string rawText)
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

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        return JsonSerializer.Deserialize<AIContent>(jsonString, options);
    }
}
