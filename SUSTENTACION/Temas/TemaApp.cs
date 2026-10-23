using System;
using System.Drawing;

namespace SUSTENTACION.Temas
{
    public static class TemaApp
    {
        // Evento que avisa a los paneles cuando el tema cambia
        public static event Action TemaCambiado;

        private static bool _esModoOscuro = true;

        public static bool EsModoOscuro
        {
            get => _esModoOscuro;
            set
            {
                if (_esModoOscuro != value)
                {
                    _esModoOscuro = value;
                    TemaCambiado?.Invoke();
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  COLORES GENERALES
        // ═══════════════════════════════════════════════════════════════

        public static Color FondoApp => EsModoOscuro ? Color.FromArgb(10, 14, 20) : Color.FromArgb(243, 246, 250);
        public static Color Superficie => EsModoOscuro ? Color.FromArgb(22, 28, 36) : Color.White;
        public static Color SuperficieAlt => EsModoOscuro ? Color.FromArgb(18, 24, 32) : Color.FromArgb(248, 250, 252);

        public static Color Texto => EsModoOscuro ? Color.FromArgb(245, 247, 250) : Color.FromArgb(30, 41, 59);
        public static Color TextoTenue => EsModoOscuro ? Color.FromArgb(148, 163, 184) : Color.FromArgb(100, 116, 139);

        // ═══════════════════════════════════════════════════════════════
        //  ALIAS COMPATIBLES CON FormAdmin Y TemaUserControl
        // ═══════════════════════════════════════════════════════════════

        public static Color BgDark => FondoApp;
        public static Color BgCard => Superficie;
        public static Color BgHeader => EsModoOscuro ? Color.FromArgb(10, 14, 20) : Color.White;
        public static Color BgSidebar => EsModoOscuro ? Color.FromArgb(15, 20, 28) : Color.FromArgb(248, 250, 252);
        public static Color BgCardHover => EsModoOscuro ? Color.FromArgb(28, 35, 44) : Color.FromArgb(232, 240, 235);
        public static Color BgInput => SuperficieAlt;

        public static Color LightCanvas => Color.FromArgb(243, 246, 250);
        public static Color LightSidebar => Color.FromArgb(248, 250, 252);
        public static Color LightHeader => Color.White;
        public static Color LightCard => Color.White;
        public static Color LightCardHover => Color.FromArgb(232, 240, 235);
        public static Color LightInput => Color.FromArgb(247, 250, 252);

        public static Color TextWhite => Texto;
        public static Color TextSoft => EsModoOscuro ? Color.FromArgb(203, 213, 225) : Color.FromArgb(60, 76, 90);
        public static Color TextGray => TextoTenue;

        public static Color BorderColor => EsModoOscuro ? Color.FromArgb(39, 47, 58) : Color.FromArgb(226, 232, 240);
        public static Color Borde => BorderColor;
        public static Color LightBorder => Color.FromArgb(226, 232, 240);
        public static Color LightText => Color.FromArgb(30, 41, 59);
        public static Color LightSoft => Color.FromArgb(60, 76, 90);
        public static Color LightGray => Color.FromArgb(100, 116, 139);

        // ═══════════════════════════════════════════════════════════════
        //  COLORES DE ESTADO
        // ═══════════════════════════════════════════════════════════════

        public static Color Exito => Color.FromArgb(34, 197, 94);
        public static Color ExitoSuave => EsModoOscuro ? Color.FromArgb(22, 50, 35) : Color.FromArgb(220, 252, 231);
        public static Color Advertencia => Color.FromArgb(245, 158, 11);
        public static Color AdvertenciaSuave => EsModoOscuro ? Color.FromArgb(50, 40, 20) : Color.FromArgb(254, 243, 199);
        public static Color Peligro => Color.FromArgb(239, 68, 68);
        public static Color PeligroSuave => EsModoOscuro ? Color.FromArgb(50, 25, 25) : Color.FromArgb(254, 226, 226);

        // ═══════════════════════════════════════════════════════════════
        //  ACENTOS
        // ═══════════════════════════════════════════════════════════════

        public static Color AccentGreen => Color.FromArgb(163, 230, 53);
        public static Color AcentoVerde => AccentGreen;
        public static Color AccentGreenSoft => Color.FromArgb(102, 204, 51);
        public static Color AccentBlue => Color.FromArgb(56, 189, 248);
        public static Color AcentoAzul => AccentBlue;
        public static Color AccentPurple => Color.FromArgb(168, 85, 247);
        public static Color AcentoMorado => AccentPurple;
        public static Color AccentOrange => Color.FromArgb(251, 146, 60);
        public static Color AccentRed => Color.FromArgb(239, 68, 68);
        public static Color AccentPink => Color.FromArgb(236, 72, 153);

        // ═══════════════════════════════════════════════════════════════
        //  COLORES DEL DASHBOARD (PanelResumen)
        // ═══════════════════════════════════════════════════════════════

        public static Color DashFondo => EsModoOscuro ? Color.FromArgb(5, 11, 24) : Color.FromArgb(241, 245, 248);
        public static Color DashTarjeta => EsModoOscuro ? Color.FromArgb(10, 20, 40) : Color.White;
        public static Color DashTarjeta2 => EsModoOscuro ? Color.FromArgb(14, 27, 52) : Color.FromArgb(248, 250, 252);
        public static Color DashLinea => EsModoOscuro ? Color.FromArgb(27, 45, 82) : Color.FromArgb(224, 232, 237);
        public static Color DashTexto => EsModoOscuro ? Color.FromArgb(232, 240, 255) : Color.FromArgb(24, 42, 55);
        public static Color DashTenue => EsModoOscuro ? Color.FromArgb(128, 147, 181) : Color.FromArgb(104, 122, 137);

        // ═══════════════════════════════════════════════════════════════
        //  MÉTODOS AUXILIARES
        // ═══════════════════════════════════════════════════════════════

        public static void AlternarTema()
        {
            EsModoOscuro = !EsModoOscuro;
        }
    }
}