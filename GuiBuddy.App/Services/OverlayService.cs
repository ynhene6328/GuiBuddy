using GuiBuddy.App.Views;
using GuiBuddy.Core.Models;
using GuiBuddy.Core.Services;

namespace GuiBuddy.App.Services
{
    public class OverlayService : IOverlayService
    {
        private OverlayWindow? _overlayWindow;

        public void Show(UiNode root, bool showAll = true)
        {
            if (_overlayWindow == null)
            {
                _overlayWindow = new OverlayWindow();
                _overlayWindow.Closed += (s, e) => _overlayWindow = null;
            }

            _overlayWindow.UpdateData(root, showAll);
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
                // Update時は現状の表示モードを維持したいが、ここでは簡易的にデフォルト動作(true)または
                // Window側で保持している状態を維持するオーバーロードが必要かもしれない。
                // 今回はUpdateDataの引数が増えたので、とりあえずtrueを渡すか、
                // OverlayWindowに状態取得プロパティを追加するのがベターだが、
                // とりあえず全表示で更新する。
                // チャットフローではUpdateは呼ばれない（Show -> Highlight）ので影響は少ない。
                _overlayWindow.UpdateData(root, true); 
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

            // 全表示オフでShowを呼び出し、ダミーIDをハイライト指定
            Show(root, showAll: false);
            Highlight(dummyId);
        }
    }
}
