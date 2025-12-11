using GuiBuddy.App.Views;
using GuiBuddy.Core.Models;
using GuiBuddy.Core.Services;

namespace GuiBuddy.App.Services
{
    public class OverlayService : IOverlayService
    {
        private OverlayWindow? _overlayWindow;

        public void Show(UiNode root)
        {
            if (_overlayWindow == null)
            {
                _overlayWindow = new OverlayWindow();
                _overlayWindow.Closed += (s, e) => _overlayWindow = null;
            }

            _overlayWindow.UpdateData(root);
            _overlayWindow.Show();
        }

        public void Hide()
        {
            _overlayWindow?.Close();
            _overlayWindow = null;
        }

        public void Update(UiNode root)
        {
            if (_overlayWindow != null)
            {
                _overlayWindow.UpdateData(root);
            }
        }
    }
}
