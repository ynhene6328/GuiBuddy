using GuiBuddy.Core.Models;

namespace GuiBuddy.Core.Services;

/// <summary>
/// AIクライアントを生成するファクトリインターフェース。
/// </summary>
public interface IAIClientFactory
{
    /// <summary>
    /// 指定されたプロバイダーとモデルに対応するAIクライアントを生成します。
    /// </summary>
    /// <param name="provider">プロバイダー名</param>
    /// <param name="model">モデル名</param>
    /// <returns>AIクライアント</returns>
    IAIClient Create(string provider, string model);

    /// <summary>
    /// 現在の設定に基づいてAIクライアントを生成します。
    /// </summary>
    /// <returns>AIクライアント</returns>
    IAIClient CreateFromSettings();
}
