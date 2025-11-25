using System;
using System.Collections.Generic;
using System.Windows.Forms;

using Rdr2Defusal.UI;

namespace Rdr2Defusal.UI
{
    /// <summary>
    /// Arrow-key driven debug menu that renders with graphics natives (no subtitles).
    /// </summary>
    public sealed class DebugMenu
    {
        private readonly List<DebugMenuPage> _pages = new List<DebugMenuPage>();
        private int _pageIndex;
        private int _itemIndex;

        private bool _listeningForBind;
        private KeybindEntry _pendingBind;

        private bool _dirty = true;
        private ThemeColors _theme = ThemeColors.Dark();

        // Layout (normalized screen space)
        private const float PanelX = 0.02f;
        private const float PanelY = 0.10f;
        private const float PanelWidth = 0.32f;
        private const float RowHeight = 0.030f;
        private const float HeaderHeight = 0.034f;
        private const float Padding = 0.008f;
        private const int VisibleRows = 11;

        public bool Visible { get; private set; }

        public void AddPage(DebugMenuPage page)
        {
            _pages.Add(page);
        }

        public void Toggle()
        {
            Visible = !Visible;
            _dirty = true;
        }

        public void SetVisible(bool value)
        {
            if (Visible == value) return;
            Visible = value;
            _dirty = true;
        }

        public bool HandleKey(Keys key)
        {
            if (!Visible) return false;

            if (_listeningForBind)
            {
                if (key != Keys.F7 && key != Keys.None)
                {
                    _pendingBind.Key = key;
                    _pendingBind.OnRebind?.Invoke(key);
                }

                _listeningForBind = false;
                _pendingBind = null;
                return true;
            }

            if (key == Keys.Left)
            {
                _pageIndex = (_pageIndex - 1 + _pages.Count) % _pages.Count;
                _itemIndex = 0;
                return true;
            }

            if (key == Keys.Right)
            {
                _pageIndex = (_pageIndex + 1) % _pages.Count;
                _itemIndex = 0;
                return true;
            }

            if (key == Keys.Up)
            {
                int count = _pages[_pageIndex].Items.Count;
                _itemIndex = (_itemIndex - 1 + count) % count;
                return true;
            }

            if (key == Keys.Down)
            {
                int count = _pages[_pageIndex].Items.Count;
                _itemIndex = (_itemIndex + 1) % count;
                return true;
            }

            if (key == Keys.Delete || key == Keys.Back)
            {
                DebugMenuItem item = _pages[_pageIndex].Items[_itemIndex];
                if (item.IsBinding && item.OnClearBinding != null)
                {
                    item.OnClearBinding();
                    return true;
                }
            }

            if (key == Keys.Enter)
            {
                DebugMenuItem item = _pages[_pageIndex].Items[_itemIndex];
                if (item.IsBinding)
                {
                    _listeningForBind = true;
                    _pendingBind = item.BindEntry;
                    return true;
                }

                item.OnActivate?.Invoke();
                return true;
            }

            return false;
        }

        public void Tick(float dt)
        {
            if (!Visible) return;
            Render();
        }

        public void Render()
        {
            if (!Visible) return;
            if (_pages.Count == 0) return;

            DebugMenuPage page = _pages[_pageIndex];

            int total = page.Items.Count;
            int start = _itemIndex - VisibleRows / 2;
            if (start < 0) start = 0;
            if (start > total - VisibleRows) start = Math.Max(0, total - VisibleRows);
            int end = Math.Min(total, start + VisibleRows);

            float panelHeight = HeaderHeight + (end - start) * RowHeight + Padding * 2f;
            float panelCenterX = PanelX + PanelWidth * 0.5f;
            float panelCenterY = PanelY + panelHeight * 0.5f;

            // Panel background
            NativeDraw.Rect(panelCenterX, panelCenterY, PanelWidth, panelHeight,
                _theme.PanelR, _theme.PanelG, _theme.PanelB, _theme.PanelA);

            // Header bar
            float headerCenterY = PanelY + HeaderHeight * 0.5f;
            NativeDraw.Rect(panelCenterX, headerCenterY, PanelWidth, HeaderHeight,
                _theme.HeaderR, _theme.HeaderG, _theme.HeaderB, _theme.HeaderA);
            NativeDraw.Text($"Debug Menu | {page.Title} ({_pageIndex + 1}/{_pages.Count})",
                PanelX + Padding, PanelY + 0.002f, 0.35f, 0.35f,
                _theme.TextR, _theme.TextG, _theme.TextB, _theme.TextA);

            // Rows
            for (int i = start; i < end; i++)
            {
                float rowY = PanelY + HeaderHeight + Padding + (i - start) * RowHeight;
                float rowCenterY = rowY + RowHeight * 0.5f;
                bool selected = (i == _itemIndex);

                NativeDraw.Rect(panelCenterX, rowCenterY, PanelWidth - Padding * 2f, RowHeight - 0.002f,
                    selected ? _theme.RowSelR : _theme.RowR,
                    selected ? _theme.RowSelG : _theme.RowG,
                    selected ? _theme.RowSelB : _theme.RowB,
                    selected ? _theme.RowSelA : _theme.RowA);

                DebugMenuItem item = page.Items[i];
                string value = item.GetValue?.Invoke();
                string suffix = "";
                if (item.IsBinding)
                {
                    if (_listeningForBind && _pendingBind == item.BindEntry) suffix = " [press key]";
                    else if (!string.IsNullOrEmpty(value)) suffix = $" [{value}]";
                }
                else if (!string.IsNullOrEmpty(value))
                {
                    suffix = $" : {value}";
                }

                NativeDraw.TextWrapped(item.Label + suffix, PanelX + Padding * 1.5f, rowY + 0.002f, 0.34f, 0.34f,
                    _theme.TextR, _theme.TextG, _theme.TextB, _theme.TextA, PanelWidth - Padding * 3f);
            }

            // Footer hint minimal to avoid wrap
            float footerY = PanelY + panelHeight - Padding - 0.022f;
            NativeDraw.Text("Arrows | Enter | Del | F7",
                PanelX + Padding * 1.5f, footerY, 0.30f, 0.30f,
                _theme.FooterR, _theme.FooterG, _theme.FooterB, _theme.FooterA);
        }

        public void ApplyTheme(ThemeColors theme)
        {
            _theme = theme;
            _dirty = true;
        }
    }

    public sealed class DebugMenuPage
    {
        public string Title { get; set; }
        public List<DebugMenuItem> Items { get; } = new List<DebugMenuItem>();
    }

    public sealed class DebugMenuItem
    {
        public string Label { get; set; }
        public Func<string> GetValue { get; set; }
        public Action OnActivate { get; set; }

        // Binding helpers
        public bool IsBinding { get; set; }
        public KeybindEntry BindEntry { get; set; }
        public Action OnClearBinding { get; set; }
    }
}
