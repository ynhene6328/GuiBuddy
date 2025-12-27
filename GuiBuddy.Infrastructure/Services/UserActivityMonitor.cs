using System;
using System.Text.Json;
using System.Runtime.InteropServices;
using GuiBuddy.Core.Models;
using GuiBuddy.Core.Services;

namespace GuiBuddy.Infrastructure.Services;

/// <summary>
/// 定期的にUI構造を取得・比較し、変化を検知するサービスの実装。
/// </summary>
public class UserActivityMonitor : IUserActivityMonitor
{
    private readonly IWindowService _windowService;
    private readonly IUIMapService _uiMapService;
    private System.Timers.Timer? _pollingTimer;
    private IntPtr _targetWindowHandle;
    private string? _lastUiSnapshotJson;
    private readonly object _lock = new();

    public event EventHandler? InputDetected;

    public bool IsMonitoring => _pollingTimer != null && _pollingTimer.Enabled;

    public UserActivityMonitor(IWindowService windowService, IUIMapService uiMapService)
    {
        _windowService = windowService;
        _uiMapService = uiMapService;
    }

    public void StartMonitoring(IntPtr targetWindowHandle)
    {
        lock (_lock)
        {
            if (IsMonitoring && _targetWindowHandle == targetWindowHandle)
                return;

            StopMonitoring();

            _targetWindowHandle = targetWindowHandle;
            _lastUiSnapshotJson = null;

            // 2000ms間隔でポーリング
            _pollingTimer = new System.Timers.Timer(2000);
            _pollingTimer.Elapsed += (s, e) => CheckActivity();
            _pollingTimer.AutoReset = true;
            _pollingTimer.Start();
        }
    }

    public void StopMonitoring()
    {
        lock (_lock)
        {
            if (_pollingTimer != null)
            {
                _pollingTimer.Stop();
                _pollingTimer.Dispose();
                _pollingTimer = null;
            }
            _targetWindowHandle = IntPtr.Zero;
            _lastUiSnapshotJson = null;
        }
    }

    private void CheckActivity()
    {
        // ロック内で行うべきか議論があるが、Timer callbackは再入可能ではない（AutoReset=trueでも前の処理が終わるまで次が呼ばれるわけではない...いや、TimerはReentrantだ。
        // 重い処理なので、万が一前回の処理が終わっていなければスキップしたい。
        if (!System.Threading.Monitor.TryEnter(_lock))
        {
            return; 
        }

        try
        {
            if (_targetWindowHandle == IntPtr.Zero) return;

            // 1. UI構造の取得
            var root = _windowService.GetWindowStructure(_targetWindowHandle);
            if (root == null) return;

            // 2. マップ生成とAI用ノードへの変換
            var map = _uiMapService.GenerateMap(root);
            var aiNode = _uiMapService.ConvertToAiNode(map.Root);

            // 3. JSONシリアライズ
            var json = JsonSerializer.Serialize(aiNode);

            // 4. 比較
            if (_lastUiSnapshotJson != null && json != _lastUiSnapshotJson)
            {
                // 変化あり
                _lastUiSnapshotJson = json;
                InputDetected?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                // 初回、または変化なし
                _lastUiSnapshotJson = json;
            }
        }
        catch
        {
            // ポーリング中の例外は無視（対象ウィンドウが閉じられた場合など）
        }
        finally
        {
            System.Threading.Monitor.Exit(_lock);
        }
    }

    public void Dispose()
    {
        StopMonitoring();
    }
}
