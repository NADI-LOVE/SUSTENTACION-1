using System.Drawing;

namespace SUSTENTACION.Temas
{
    public static class TemaApp
    {
        // Estado global del tema
        public static bool EsModoOscuro { get; set; } = true;

        // ── COLORES DEL FORMULARIO (FormAdmin) ──
        public static Color BgDark = Color.FromArgb(10, 14, 20);
        public static Color BgSidebar = Color.FromArgb(15, 20, 28);
        public static Color BgHeader = Color.FromArgb(10, 14, 20);
        public static Color BgCard = Color.FromArgb(22, 28, 36);
        public static Color BgCardHover = Color.FromArgb(28, 35, 44);
        public static Color BgInput = Color.FromArgb(18, 24, 32);

        public static Color AccentGreen = Color.FromArgb(163, 230, 53);
        public static Color AccentGreenSoft = Color.FromArgb(102, 204, 51);
        public static Color AccentBlue = Color.FromArgb(56, 189, 248);
        public static Color AccentOrange = Color.FromArgb(251, 146, 60);
        public static Color AccentPink = Color.FromArgb(236, 72, 153);
        public static Color AccentPurple = Color.FromArgb(168, 85, 247);

        public static Color TextWhite = Color.FromArgb(245, 247, 250);
        public static Color TextSoft = Color.FromArgb(203, 213, 225);
        public static Color TextGray = Color.FromArgb(148, 163, 184);
        public static Color BorderColor = Color.FromArgb(39, 47, 58);

        public static Color LightCanvas = Color.FromArgb(244, 248, 250);
        public static Color LightSidebar = Color.FromArgb(248, 250, 252);
        public static Color LightHeader = Color.White;
        public static Color LightCard = Color.White;
        public static Color LightCardHover = Color.FromArgb(232, 240, 235);
        public static Color LightInput = Color.FromArgb(247, 250, 252);
        public static Color LightText = Color.FromArgb(23, 36, 48);
        public static Color LightSoft = Color.FromArgb(60, 76, 90);
        public static Color LightGray = Color.FromArgb(96, 112, 126);
        public static Color LightBorder = Color.FromArgb(214, 225, 231);

        // ── COLORES DEL PANEL RESUMEN (Dashboard) ──
        public static Color FondoOscuroDash = Color.FromArgb(5, 11, 24);
        public static Color TarjetaOscuraDash = Color.FromArgb(10, 20, 40);
        public static Color TarjetaOscura2Dash = Color.FromArgb(14, 27, 52);
        public static Color LineaOscuraDash = Color.FromArgb(27, 45, 82);
        public static Color TextoOscuroDash = Color.FromArgb(232, 240, 255);
        public static Color TenueOscuroDash = Color.FromArgb(128, 147, 181);

        public static Color FondoClaroDash = Color.FromArgb(241, 245, 248);
        public static Color TarjetaClaraDash = Color.White;
        public static Color TarjetaClara2Dash = Color.FromArgb(248, 250, 252);
        public static Color LineaClaraDash = Color.FromArgb(224, 232, 237);
        public static Color TextoClaroDash = Color.FromArgb(24, 42, 55);
        public static Color TenueClaroDash = Color.FromArgb(104, 122, 137);

        // ── PROPIEDADES DINÁMICAS (Para el Dashboard) ──
        public static Color DashFondo => EsModoOscuro ? FondoOscuroDash : FondoClaroDash;
        public static Color DashTarjeta => EsModoOscuro ? TarjetaOscuraDash : TarjetaClaraDash;
        public static Color DashTarjeta2 => EsModoOscuro ? TarjetaOscura2Dash : TarjetaClara2Dash;
        public static Color DashLinea => EsModoOscuro ? LineaOscuraDash : LineaClaraDash;
        public static Color DashTexto => EsModoOscuro ? TextoOscuroDash : TextoClaroDash;
        public static Color DashTenue => EsModoOscuro ? TenueOscuroDash : TenueClaroDash;
    }
}