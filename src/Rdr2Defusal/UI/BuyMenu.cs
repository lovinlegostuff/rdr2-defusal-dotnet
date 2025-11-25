using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Rdr2Defusal.UI
{
    /// <summary>
    /// Simple buy menu overlay with categories and pretty names.
    /// Not wired to economy yet; acts as selection UI and invokes a callback.
    /// </summary>
    public sealed class BuyMenu
    {
        private readonly List<BuyCategory> _categories = new List<BuyCategory>();
        private int _catIndex;
        private int _itemIndex;
        private bool _visible;
        private Action<BuyItem> _onPurchase;
        private int _playerMoney;

        public bool Visible => _visible;

        public void SetItems(IEnumerable<BuyCategory> cats, Action<BuyItem> onPurchase)
        {
            _categories.Clear();
            _categories.AddRange(cats);
            _catIndex = 0;
            _itemIndex = 0;
            _onPurchase = onPurchase;
        }

        public void SetPlayerMoney(int money)
        {
            _playerMoney = money;
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

            if (key == Keys.Left)
            {
                _catIndex = (_catIndex - 1 + _categories.Count) % _categories.Count;
                _itemIndex = 0;
                return true;
            }
            if (key == Keys.Right)
            {
                _catIndex = (_catIndex + 1) % _categories.Count;
                _itemIndex = 0;
                return true;
            }
            if (key == Keys.Up)
            {
                int count = _categories[_catIndex].Items.Count;
                _itemIndex = (_itemIndex - 1 + count) % count;
                return true;
            }
            if (key == Keys.Down)
            {
                int count = _categories[_catIndex].Items.Count;
                _itemIndex = (_itemIndex + 1) % count;
                return true;
            }
            if (key == Keys.Enter)
            {
                BuyItem item = _categories[_catIndex].Items[_itemIndex];
                _onPurchase?.Invoke(item);
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
            if (_categories.Count == 0) return;

            const float panelX = 0.55f;
            const float panelY = 0.10f;
            const float panelW = 0.36f;
            const float rowH = 0.030f;
            const float headerH = 0.034f;
            const float pad = 0.008f;

            BuyCategory cat = _categories[_catIndex];
            int visible = Math.Min(10, cat.Items.Count);
            float panelH = headerH + visible * rowH + pad * 2f;
            float centerX = panelX + panelW * 0.5f;
            float centerY = panelY + panelH * 0.5f;

            NativeDraw.Rect(centerX, centerY, panelW, panelH, 16, 14, 12, 220);
            NativeDraw.Rect(centerX, panelY + headerH * 0.5f, panelW, headerH, 32, 28, 18, 235);
            NativeDraw.Text($"Buy | {cat.Name} ({_catIndex + 1}/{_categories.Count}) | ${_playerMoney}", panelX + pad, panelY + 0.002f, 0.34f, 0.34f, 245, 235, 210, 255);

            int start = Math.Max(0, _itemIndex - visible / 2);
            if (start > cat.Items.Count - visible) start = Math.Max(0, cat.Items.Count - visible);
            int end = Math.Min(cat.Items.Count, start + visible);

            for (int i = start; i < end; i++)
            {
                float rowY = panelY + headerH + pad + (i - start) * rowH;
                float rowCenterY = rowY + rowH * 0.5f;
                bool sel = i == _itemIndex;
                NativeDraw.Rect(centerX, rowCenterY, panelW - pad * 2f, rowH - 0.002f,
                    sel ? 140 : 40,
                    sel ? 92 : 40,
                    sel ? 42 : 40,
                    sel ? 240 : 160);

                BuyItem item = cat.Items[i];
                string line = $"{item.Pretty} ${item.Price}";
                NativeDraw.TextWrapped(line, panelX + pad * 1.5f, rowY + 0.002f, 0.34f, 0.34f, 245, 235, 210, 250, panelW - pad * 3f);
            }
        }
    }

    public sealed class BuyCategory
    {
        public string Name { get; set; }
        public List<BuyItem> Items { get; set; } = new List<BuyItem>();
    }

    public sealed class BuyItem
    {
        public string Pretty { get; set; }
        public string Weapon { get; set; }
        public int Price { get; set; }
        public string Slot { get; set; }
    }
}
