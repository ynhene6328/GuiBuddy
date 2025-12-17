using GuiBuddy.Core.Models;
using GuiBuddy.Core.Repositories;
using GuiBuddy.Core.Services;
using System.Windows; // For Rect

namespace GuiBuddy.Services;

public class WindowService : IWindowService
{
    private readonly IWindowRepository _repository;

    public WindowService(IWindowRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<WindowInfo> GetWindows()
    {
        // ここでフィルタリングロジックなどを追加可能
        // 例: タイトルが空のものを除外するなど
        return _repository.GetAllWindows()
            .Where(w => !string.IsNullOrWhiteSpace(w.Title) && w.Bounds.Width > 0 && w.Bounds.Height > 0);
    }

    public UIElementInfo? GetWindowStructure(IntPtr windowHandle)
    {
        return _repository.GetWindowStructure(windowHandle);
    }
    public WindowInfo? GetForegroundWindow()
    {
        var handle = GetForegroundWindowNative();
        if (handle == IntPtr.Zero) return null;

        var threadId = GetWindowThreadProcessId(handle, out var processId);
        
        // AutomationWindowRepository経由で情報取得を試みるのがベストだが、
        // ここでは簡易的にHandleとProcessIdだけを持つWindowInfoを返す
        // 必要ならRepositoryにGetWindowInfo(handle)を追加する
        // 今回はWindowInfoのProcessIdを活用するため、必要な情報だけセットする
        
        // タイトルやRectも取得したい場合は GetWindowRect や GetWindowText も必要になるが、
        // 追従判定には ProcessId と Handle があれば十分。
        
        return new WindowInfo
        {
            Handle = handle,
            ProcessId = (int)processId,
            Title = string.Empty, // 判定用なので空でOK
            Bounds = new Rect()   // 判定用なので空でOK
        };
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetForegroundWindow")]
    private static extern IntPtr GetForegroundWindowNative();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}

