using GuiBuddy.Core.Models;

namespace GuiBuddy.Core.Services;

public interface IWindowService
{
    IEnumerable<WindowInfo> GetWindows();
}
