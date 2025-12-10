using GuiBuddy.Core.Models;

namespace GuiBuddy.Core.Repositories;

public interface IWindowRepository
{
    IEnumerable<WindowInfo> GetAllWindows();
    UIElementInfo? GetWindowStructure(IntPtr windowHandle);
}
