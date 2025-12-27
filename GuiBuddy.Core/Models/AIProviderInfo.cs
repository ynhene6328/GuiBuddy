using System.Collections.Generic;

namespace GuiBuddy.Core.Models;

/// <summary>
/// AIプロバイダーの情報を保持するクラス。
/// </summary>
public class AIProviderInfo
{
    /// <summary>
    /// プロバイダー名 (例: "Gemini", "OpenAI")
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 利用可能なモデル名のリスト
    /// </summary>
    public IReadOnlyList<string> Models { get; }

    public AIProviderInfo(string name, IReadOnlyList<string> models)
    {
        Name = name;
        Models = models;
    }

    /// <summary>
    /// 利用可能なプロバイダー一覧（ハードコード）
    /// </summary>
    public static readonly IReadOnlyList<AIProviderInfo> AvailableProviders = new[]
    {
        new AIProviderInfo("Gemini", new[] { "gemini-2.5-flash", "gemini-2.5-flash-lite", "gemini-3-flash", "gemma-3-12b" }),
        new AIProviderInfo("OpenAI", new[] { "gpt-4o", "gpt-4o-mini", "o1" }),
        new AIProviderInfo("OpenRouter", new[] { "nvidia/nemotron-3-nano-30b-a3b:free", "nvidia/llama-3.1-nemotron-70b-instruct" }),
    };
}
