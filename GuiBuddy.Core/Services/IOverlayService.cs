using GuiBuddy.Core.Models;

namespace GuiBuddy.Core.Services;

public interface IOverlayService
{
    void Show(UiNode root);
    void Hide();
    void Update(UiNode root);
    void Highlight(int nodeId);
    void ClearHighlight();
    void HighlightWindow(WindowInfo window);
}
