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

        public void Highlight(int nodeId)
        {
            if (_overlayWindow != null)
            {
                _overlayWindow.HighlightNode(nodeId);
            }
        }

        public void ClearHighlight()
        {
            if (_overlayWindow != null)
            {
                _overlayWindow.ClearHighlight();
            }
        }

        public void HighlightWindow(WindowInfo window)
        {
            // ウィンドウの矩形情報を持つダミーノードを作成
            // IDは衝突しない適当な負の値を使用
            var dummyId = -999;
            var root = new UiNode
            {
                Id = dummyId,
                Name = window.Title,
                Type = "Window",
                Bounds = new UiBounds 
                { 
                    X = (int)window.Bounds.X, 
                    Y = (int)window.Bounds.Y, 
                    Width = (int)window.Bounds.Width, 
                    Height = (int)window.Bounds.Height 
                },
                Hint = window.Title
            };

            // Showを呼び出し、ダミーIDをハイライト指定
            Show(root);
            Highlight(dummyId);
        }
    }
}
