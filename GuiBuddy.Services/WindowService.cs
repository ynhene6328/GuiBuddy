using GuiBuddy.Core.Models;
using GuiBuddy.Core.Repositories;
using GuiBuddy.Core.Services;

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
}
