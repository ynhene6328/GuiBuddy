using System;
using System.Runtime.InteropServices;
using GuiBuddy.Core.Models;
using GuiBuddy.Core.Services;

namespace GuiBuddy.Infrastructure.Services;

/// <summary>
/// ユーザーの入力活動と対象ウィンドウのアクティブ状態を監視するサービスの実装。
/// </summary>
public class UserActivityMonitor : IUserActivityMonitor
{
    private readonly IWindowService _windowService;
    private System.Timers.Timer? _pollingTimer;
    private IntPtr _targetWindowHandle;
    private uint _lastInputTick;
    private readonly object _lock = new();

    public event EventHandler? InputDetected;

    public bool IsMonitoring => _pollingTimer != null && _pollingTimer.Enabled;

    public UserActivityMonitor(IWindowService windowService)
    {
        _windowService = windowService;
    }

    public void StartMonitoring(IntPtr targetWindowHandle)
    {
        lock (_lock)
        {
            if (IsMonitoring && _targetWindowHandle == targetWindowHandle)
                return;

            StopMonitoring();

            _targetWindowHandle = targetWindowHandle;
            _lastInputTick = GetLastInputTick();

            // 500ms間隔でポーリング
            _pollingTimer = new System.Timers.Timer(500);
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
        }
    }

    private void CheckActivity()
    {
        try
        {
            var last = GetLastInputTick();

            // UIスレッドではない可能性があるため、フォアグラウンドウィンドウの取得には注意が必要だが、
            // Win32 APIベースのGetForegroundWindowはスレッドセーフ（OSコール）なので通常問題ない。
            var fg = _windowService.GetForegroundWindow();

            // 入力があり（Tick更新）、かつフォアグラウンドウィンドウが対象ウィンドウと一致する場合
            if (last != _lastInputTick && fg != null && _targetWindowHandle != IntPtr.Zero && fg.Handle == _targetWindowHandle)
            {
                _lastInputTick = last;
                InputDetected?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                // アクティブでない間もTickは更新しておく（復帰時に即発火しないようにするため）
                 _lastInputTick = last;
            }
        }
        catch
        {
            // ポーリング中の例外は無視
        }
    }

    public void Dispose()
    {
        StopMonitoring();
    }

    // --- Win32 API ---

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    private static uint GetLastInputTick()
    {
        try
        {
            var lii = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>(), dwTime = 0 };
            if (GetLastInputInfo(ref lii))
            {
                return lii.dwTime;
            }
        }
        catch { }
        return 0;
    }
}
