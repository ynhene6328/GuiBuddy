using GuiBuddy.Core.Models;

namespace GuiBuddy.Core.Services;

public interface IWindowService
{
    IEnumerable<WindowInfo> GetWindows();
    UIElementInfo? GetWindowStructure(IntPtr windowHandle);
    WindowInfo? GetForegroundWindow();
    bool ScrollToElement(IntPtr windowHandle, string automationId);
}
