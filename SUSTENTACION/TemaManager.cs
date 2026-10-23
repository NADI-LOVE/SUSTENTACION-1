using System.Drawing;

namespace SUSTENTACION
{
    public static class TemaManager
    {
        public static bool EsOscuro { get; set; } = true;

        // Paleta OSCURO (actual)
        public static Color BgMainOscuro = Color.FromArgb(21, 22, 37);
        public static Color BgSidebarOscuro = Color.FromArgb(16, 17, 28);
        public static Color BgCardOscuro = Color.FromArgb(30, 31, 48);
        public static Color TextWhiteOscuro = Color.White;
        public static Color TextMutedOscuro = Color.FromArgb(130, 134, 158);

        // Paleta CLARO (nueva)
        public static Color BgMainClaro = Color.FromArgb(245, 246, 250);
        public static Color BgSidebarClaro = Color.FromArgb(230, 232, 240);
        public static Color BgCardClaro = Color.FromArgb(255, 255, 255);
        public static Color TextWhiteClaro = Color.FromArgb(30, 30, 50);
        public static Color TextMutedClaro = Color.FromArgb(100, 105, 130);

        // Accesos rápidos según tema
        public static Color BgMain => EsOscuro ? BgMainOscuro : BgMainClaro;
        public static Color BgSidebar => EsOscuro ? BgSidebarOscuro : BgSidebarClaro;
        public static Color BgCard => EsOscuro ? BgCardOscuro : BgCardClaro;
        public static Color TextWhite => EsOscuro ? TextWhiteOscuro : TextWhiteClaro;
        public static Color TextMuted => EsOscuro ? TextMutedOscuro : TextMutedClaro;
    }
}