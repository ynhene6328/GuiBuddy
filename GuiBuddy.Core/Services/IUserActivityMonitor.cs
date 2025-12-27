using System;

namespace GuiBuddy.Core.Services;

/// <summary>
/// ユーザーの活動（入力操作）と対象ウィンドウの状態を監視するサービス。
/// </summary>
public interface IUserActivityMonitor : IDisposable
{
    /// <summary>
    /// 監視条件（ユーザー入力あり かつ 対象ウィンドウがアクティブ）を満たした際に発生します。
    /// </summary>
    event EventHandler InputDetected;

    /// <summary>
    /// 監視を開始します。
    /// </summary>
    /// <param name="targetWindowHandle">監視対象のウィンドウハンドル</param>
    void StartMonitoring(IntPtr targetWindowHandle);

    /// <summary>
    /// 監視を停止します。
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// 現在監視中かどうかを取得します。
    /// </summary>
    bool IsMonitoring { get; }
}
