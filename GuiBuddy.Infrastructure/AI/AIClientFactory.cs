using System;
using GuiBuddy.Core.Models;
using GuiBuddy.Core.Services;

namespace GuiBuddy.Infrastructure.AI;

/// <summary>
/// AIクライアントを生成するファクトリの実装。
/// </summary>
public class AIClientFactory : IAIClientFactory
{
    private readonly ISettingsService _settingsService;
    private readonly IResponseParser _responseParser;

    public AIClientFactory(ISettingsService settingsService, IResponseParser responseParser)
    {
        _settingsService = settingsService;
        _responseParser = responseParser;
    }

    public IAIClient Create(string provider, string model)
    {
        return provider switch
        {
            "Gemini" => new GeminiSdkClient(_settingsService, _responseParser, model),
            "OpenAI" => new OpenAIClient(_settingsService, _responseParser, model),
            _ => throw new NotSupportedException($"プロバイダー '{provider}' はサポートされていません。")
        };
    }

    public IAIClient CreateFromSettings()
    {
        string provider = _settingsService.GetSelectedProvider();
        string model = _settingsService.GetSelectedModel();
        return Create(provider, model);
    }
}
