using GuiBuddy.Core.Models;

namespace GuiBuddy.Core.Services;

public interface IOverlayService
{
    void Show(UiNode root);
    void Hide();
    void Update(UiNode root);
}
