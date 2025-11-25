namespace Rdr2Defusal.UI
{
    public sealed class ThemeColors
    {
        public int PanelR, PanelG, PanelB, PanelA;
        public int HeaderR, HeaderG, HeaderB, HeaderA;
        public int RowR, RowG, RowB, RowA;
        public int RowSelR, RowSelG, RowSelB, RowSelA;
        public int TextR, TextG, TextB, TextA;
        public int FooterR, FooterG, FooterB, FooterA;

        public static ThemeColors Dark()
        {
            return new ThemeColors
            {
                PanelR = 0, PanelG = 0, PanelB = 0, PanelA = 190,
                HeaderR = 25, HeaderG = 25, HeaderB = 25, HeaderA = 220,
                RowR = 18, RowG = 18, RowB = 18, RowA = 160,
                RowSelR = 60, RowSelG = 90, RowSelB = 150, RowSelA = 230,
                TextR = 240, TextG = 240, TextB = 240, TextA = 250,
                FooterR = 180, FooterG = 180, FooterB = 180, FooterA = 210
            };
        }

        public static ThemeColors Light()
        {
            return new ThemeColors
            {
                PanelR = 230, PanelG = 230, PanelB = 230, PanelA = 220,
                HeaderR = 210, HeaderG = 210, HeaderB = 210, HeaderA = 240,
                RowR = 200, RowG = 200, RowB = 200, RowA = 190,
                RowSelR = 140, RowSelG = 180, RowSelB = 230, RowSelA = 240,
                TextR = 25, TextG = 25, TextB = 25, TextA = 255,
                FooterR = 80, FooterG = 80, FooterB = 80, FooterA = 230
            };
        }

        public static ThemeColors HighContrast()
        {
            return new ThemeColors
            {
                PanelR = 10, PanelG = 10, PanelB = 10, PanelA = 240,
                HeaderR = 30, HeaderG = 30, HeaderB = 30, HeaderA = 250,
                RowR = 10, RowG = 10, RowB = 10, RowA = 230,
                RowSelR = 200, RowSelG = 80, RowSelB = 20, RowSelA = 250,
                TextR = 255, TextG = 255, TextB = 255, TextA = 255,
                FooterR = 220, FooterG = 220, FooterB = 220, FooterA = 240
            };
        }

        public static ThemeColors RockstarMuted()
        {
            return new ThemeColors
            {
                PanelR = 18, PanelG = 16, PanelB = 12, PanelA = 210,
                HeaderR = 32, HeaderG = 28, HeaderB = 18, HeaderA = 230,
                RowR = 22, RowG = 20, RowB = 16, RowA = 200,
                RowSelR = 140, RowSelG = 92, RowSelB = 42, RowSelA = 240,
                TextR = 245, TextG = 235, TextB = 210, TextA = 255,
                FooterR = 200, FooterG = 190, FooterB = 160, FooterA = 230
            };
        }

        public static ThemeColors RockstarLight()
        {
            return new ThemeColors
            {
                PanelR = 210, PanelG = 195, PanelB = 170, PanelA = 210,
                HeaderR = 180, HeaderG = 160, HeaderB = 130, HeaderA = 230,
                RowR = 200, RowG = 185, RowB = 160, RowA = 190,
                RowSelR = 220, RowSelG = 150, RowSelB = 70, RowSelA = 240,
                TextR = 30, TextG = 25, TextB = 20, TextA = 255,
                FooterR = 60, FooterG = 50, FooterB = 40, FooterA = 230
            };
        }
    }
}
