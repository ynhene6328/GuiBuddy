using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using GuiBuddy.Core.Models;

namespace GuiBuddy.App.Views
{
    public partial class OverlayWindow : Window
    {
        private UiNode? _rootNode;
        private readonly Pen _borderPen;
        private readonly Brush _hintBackgroundBrush;
        private readonly Brush _hintForegroundBrush;
        private readonly Typeface _typeface;
        private System.Windows.Threading.DispatcherTimer? _inputTimer;
        private int? _highlightedNodeId;
        private readonly Pen _highlightPen;

        public OverlayWindow()
        {
            InitializeComponent();
            
            // Resource initialization for drawing
            _borderPen = new Pen(new SolidColorBrush(Color.FromArgb(128, 0, 120, 215)), 2); // #800078D7
            _borderPen.Freeze();
            
            _highlightPen = new Pen(Brushes.Red, 4);
            _highlightPen.Freeze();
            
            _hintBackgroundBrush = new SolidColorBrush(Color.FromArgb(200, 30, 30, 30)); // Dark semi-transparent
            _hintBackgroundBrush.Freeze();
            
            _hintForegroundBrush = Brushes.White;
            _typeface = new Typeface("Segoe UI");

            // マルチディスプレイ対応: 仮想スクリーン全体をカバーするように設定
            this.Left = SystemParameters.VirtualScreenLeft;
            this.Top = SystemParameters.VirtualScreenTop;
            this.Width = SystemParameters.VirtualScreenWidth;
            this.Height = SystemParameters.VirtualScreenHeight;
        }

        private bool _showAllNodes = true;



        public void UpdateData(UiNode root, bool showAll)
        {
            _rootNode = root;
            _showAllNodes = showAll; // 全表示フラグを更新
            InvalidateVisual(); // 再描画をリクエスト
        }

        public void HighlightNode(int nodeId)
        {
            _highlightedNodeId = nodeId;
            _showAllNodes = false; // ハイライト時は全表示をオフにする（混在させたい場合はロジック調整）
            InvalidateVisual();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            SetClickThrough();
            StartInputPolling();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _inputTimer?.Stop();
        }

        private void SetClickThrough()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
        }

        private void StartInputPolling()
        {
            _inputTimer = new System.Windows.Threading.DispatcherTimer();
            _inputTimer.Interval = TimeSpan.FromMilliseconds(100);
            _inputTimer.Tick += (s, e) =>
            {
                if ((GetAsyncKeyState(0x1B) & 0x8000) != 0) // VK_ESCAPE = 0x1B
                {
                    this.Close();
                }
            };
            _inputTimer.Start();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (_rootNode == null) return;

            // ハイライトIDがなく、かつ全表示モードでもない場合は描画しない
            if (!_highlightedNodeId.HasValue && !_showAllNodes)
            {
                return;
            }

            // マルチディスプレイ対応: 座標変換
            // ウィンドウの左上が (VirtualScreenLeft, VirtualScreenTop) なので、
            // スクリーン絶対座標 (0,0) を描画するには、(-Left, -Top) だけずらす必要がある
            dc.PushTransform(new TranslateTransform(-this.Left, -this.Top));

            DrawNodeRecursive(dc, _rootNode);

            dc.Pop(); // Transform解除
        }

        private void DrawNodeRecursive(DrawingContext dc, UiNode node)
        {
            // 境界ボックスの描画
            if (node.Bounds != null)
            {
                bool isHighlightTarget = _highlightedNodeId.HasValue && node.Id == _highlightedNodeId.Value;
                bool shouldDraw = false;
                Pen? pen = null;

                if (isHighlightTarget)
                {
                    // ハイライト対象の場合
                    shouldDraw = true;
                    pen = _highlightPen;
                }
                else if (_showAllNodes && !_highlightedNodeId.HasValue)
                {
                    // 全表示モードかつハイライト指定がない場合
                    shouldDraw = true;
                    pen = _borderPen;
                }

                if (shouldDraw && pen != null)
                {
                    var rect = new Rect(node.Bounds.X, node.Bounds.Y, node.Bounds.Width, node.Bounds.Height);
                    
                    // 矩形枠線を描画
                    dc.DrawRectangle(null, pen, rect);

                    // ヒントを描画（全表示モードまたはハイライト対象のみ）
                    if (!string.IsNullOrEmpty(node.Hint))
                    {
                        DrawHint(dc, node.Hint, rect);
                    }
                }
            }

            // 子要素の描画
            foreach (var child in node.Children)
            {
                DrawNodeRecursive(dc, child);
            }
        }

        private void DrawHint(DrawingContext dc, string hint, Rect bounds)
        {
            var formattedText = new FormattedText(
                hint,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                _typeface,
                12,
                _hintForegroundBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            // Draw Background for text
            var textBgRect = new Rect(bounds.Left, bounds.Top - formattedText.Height - 2, formattedText.Width + 8, formattedText.Height + 2);
            
            // Adjust if outside of screen or bounds (simple adjustment)
            if (textBgRect.Top < 0) textBgRect.Y = bounds.Top + 2;

            dc.DrawRoundedRectangle(_hintBackgroundBrush, null, textBgRect, 2, 2);
            dc.DrawText(formattedText, new Point(textBgRect.X + 4, textBgRect.Y + 1));
        }

        // Win32 API
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
    }
}
