using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Rdr2Defusal.UI
{
    /// <summary>
    /// Simple callout command menu toggled by F3.
    /// </summary>
    public sealed class CalloutMenu
    {
        private readonly List<CalloutItem> _items = new List<CalloutItem>();
        private int _index;
        private bool _visible;
        private Action<string> _onCommand;

        public bool Visible => _visible;

        public void SetItems(IEnumerable<CalloutItem> items, Action<string> onCommand)
        {
            _items.Clear();
            _items.AddRange(items);
            _index = 0;
            _onCommand = onCommand;
        }

        public void Show()
        {
            _visible = true;
        }

        public void Hide()
        {
            _visible = false;
        }

        public bool HandleKey(Keys key)
        {
            if (!_visible) return false;

            if (key == Keys.Up)
            {
                _index = (_index - 1 + _items.Count) % _items.Count;
                return true;
            }
            if (key == Keys.Down)
            {
                _index = (_index + 1) % _items.Count;
                return true;
            }
            if (key == Keys.Enter)
            {
                _onCommand?.Invoke(_items[_index].Command);
                return true;
            }
            if (key == Keys.Escape || key == Keys.Back)
            {
                Hide();
                return true;
            }

            return false;
        }

        public void Draw()
        {
            if (!_visible) return;
            if (_items.Count == 0) return;

            const float x = 0.70f;
            const float y = 0.35f;
            const float w = 0.24f;
            const float rowH = 0.030f;
            const float pad = 0.008f;

            int visible = Math.Min(8, _items.Count);
            float h = visible * rowH + pad * 2f;
            float centerX = x + w * 0.5f;
            float centerY = y + h * 0.5f;

            NativeDraw.Rect(centerX, centerY, w, h, 0, 0, 0, 200);

            int start = Math.Max(0, _index - visible / 2);
            if (start > _items.Count - visible) start = Math.Max(0, _items.Count - visible);
            int end = Math.Min(_items.Count, start + visible);

            for (int i = start; i < end; i++)
            {
                float rowY = y + pad + (i - start) * rowH;
                float rowCenterY = rowY + rowH * 0.5f;
                bool sel = i == _index;
                NativeDraw.Rect(centerX, rowCenterY, w - pad * 2f, rowH - 0.002f,
                    sel ? 180 : 40,
                    sel ? 140 : 40,
                    sel ? 60 : 40,
                    sel ? 240 : 160);
                NativeDraw.TextWrapped(_items[i].Label, x + pad * 1.2f, rowY + 0.002f, 0.34f, 0.34f, 245, 235, 210, 255, w - pad * 3f);
            }
        }
    }

    public sealed class CalloutItem
    {
        public string Label { get; set; }
        public string Command { get; set; }
    }
}
