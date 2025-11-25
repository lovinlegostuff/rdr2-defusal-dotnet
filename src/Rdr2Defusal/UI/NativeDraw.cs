using System;
using RDR2.Native;

namespace Rdr2Defusal.UI
{
    /// <summary>
    /// Thin wrappers around RDR2 graphics/text natives for simple in-game UI.
    /// Hashes sourced from alloc8or RDR3 Native DB.
    /// </summary>
    public static class NativeDraw
    {
        private const ulong DRAW_RECT_HASH = 0x405224591DF02025;
        private const ulong DISPLAY_TEXT_HASH = 0xD79334A4BB99BAD1;
        private const ulong SET_TEXT_COLOR_HASH = 0x50A41AD966910F03;
        private const ulong SET_TEXT_SCALE_HASH = 0xA1253A3C870B6843; // _BG_SET_TEXT_SCALE
        private const ulong BG_SET_TEXT_COLOR_HASH = 0x16FA5CE47F184F1E; // _BG_SET_TEXT_COLOR

        public static void Rect(float x, float y, float width, float height, int r, int g, int b, int a)
        {
            Function.Call(DRAW_RECT_HASH,
                new InputArgument(x),
                new InputArgument(y),
                new InputArgument(width),
                new InputArgument(height),
                new InputArgument(r),
                new InputArgument(g),
                new InputArgument(b),
                new InputArgument(a),
                new InputArgument(false),
                new InputArgument(false));
        }

        public static void Text(string text, float x, float y, float scaleX, float scaleY, int r, int g, int b, int a)
        {
            // Set both bg/text colors to keep glyphs consistent.
            Function.Call(BG_SET_TEXT_COLOR_HASH,
                new InputArgument(r),
                new InputArgument(g),
                new InputArgument(b),
                new InputArgument(a));

            Function.Call(SET_TEXT_COLOR_HASH,
                new InputArgument(r),
                new InputArgument(g),
                new InputArgument(b),
                new InputArgument(a));

            Function.Call(SET_TEXT_SCALE_HASH,
                new InputArgument(scaleX),
                new InputArgument(scaleY));

            Function.Call(DISPLAY_TEXT_HASH,
                new InputArgument(text),
                new InputArgument(x),
                new InputArgument(y));
        }

        public static void TextWrapped(string text, float x, float y, float scaleX, float scaleY, int r, int g, int b, int a, float maxWidth)
        {
            if (string.IsNullOrEmpty(text))
                return;

            // Rough wrap based on character count; not pixel-perfect but avoids overflow.
            int maxChars = (int)(maxWidth / (scaleX * 0.012f));
            if (maxChars < 8) maxChars = 8;

            int idx = 0;
            float lineY = y;
            while (idx < text.Length)
            {
                int take = Math.Min(maxChars, text.Length - idx);
                int end = idx + take;
                if (end < text.Length)
                {
                    int lastSpace = text.LastIndexOf(' ', end, take);
                    if (lastSpace > idx) take = lastSpace - idx;
                }
                string line = text.Substring(idx, take);
                Text(line, x, lineY, scaleX, scaleY, r, g, b, a);
                idx += take;
                lineY += scaleY * 0.018f;
            }
        }
    }
}
