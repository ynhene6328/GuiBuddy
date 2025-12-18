using GuiBuddy.Core.Models;

namespace GuiBuddy.Core.Repositories;

public interface IWindowRepository
{
    IEnumerable<WindowInfo> GetAllWindows();
    UIElementInfo? GetWindowStructure(IntPtr windowHandle);
    bool ScrollToElement(IntPtr windowHandle, string targetKey, bool useRuntimeId = false);
}
