using GuiBuddy.Core.Models;

namespace GuiBuddy.Core.Services;

public interface IOverlayService
{
    void Show(UiNode root, bool showAll = true);
    void Hide();
    void Update(UiNode root);
    void Highlight(int nodeId);
    void HighlightWindow(WindowInfo window);
}
