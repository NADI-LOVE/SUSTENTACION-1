#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace SUSTENTACION.PanelAdministrador
{
    /// <summary>
    /// Módulo Reportes (panel Administrador) · ARTEMUSA TV
    /// Etapa 1: diseño visual + interacción con DATOS FICTICIOS (sin base de datos).
    ///
    /// Integración con el tema del panel:
    ///     reportes.AplicarTema(true);   // oscuro
    ///     reportes.AplicarTema(false);  // claro
    /// (llámalo desde el botón "Modo claro/oscuro" que ya existe en la barra lateral).
    ///
    /// Para conectar Supabase más adelante basta con reemplazar la clase interna "Demo"
    /// (única fuente de datos) por consultas reales.
    /// </summary>
    public class CotrolReporte : UserControl
    {
        // ============================================================
        //  TEMA
        // ============================================================
        private sealed class Tema
        {
            public bool Oscuro;
            public Color Canvas, Surface, SurfaceAlt, Border, Ink, Muted;
            public Color Accent, AccentInk, AccentText;
            public Color Ok, Info, Warn, Bad, Violet, Teal;

            public static Tema Dark()
            {
                return new Tema
                {
                    Oscuro = true,
                    Canvas = Color.FromArgb(10, 14, 20),
                    Surface = Color.FromArgb(22, 28, 36),
                    SurfaceAlt = Color.FromArgb(15, 20, 28),
                    Border = Color.FromArgb(42, 51, 64),
                    Ink = Color.FromArgb(232, 236, 242),
                    Muted = Color.FromArgb(140, 153, 170),
                    Accent = Color.FromArgb(163, 230, 53),
                    AccentInk = Color.FromArgb(14, 22, 4),
                    AccentText = Color.FromArgb(163, 230, 53),
                    Ok = Color.FromArgb(163, 230, 53),
                    Info = Color.FromArgb(56, 189, 248),
                    Warn = Color.FromArgb(251, 146, 60),
                    Bad = Color.FromArgb(248, 113, 113),
                    Violet = Color.FromArgb(167, 139, 250),
                    Teal = Color.FromArgb(45, 212, 191)
                };
            }

            public static Tema Light()
            {
                return new Tema
                {
                    Oscuro = false,
                    Canvas = Color.FromArgb(244, 247, 250),
                    Surface = Color.White,
                    SurfaceAlt = Color.FromArgb(241, 245, 249),
                    Border = Color.FromArgb(226, 232, 240),
                    Ink = Color.FromArgb(15, 23, 42),
                    Muted = Color.FromArgb(100, 116, 139),
                    Accent = Color.FromArgb(163, 230, 53),
                    AccentInk = Color.FromArgb(20, 30, 6),
                    AccentText = Color.FromArgb(63, 98, 18),
                    Ok = Color.FromArgb(88, 150, 18),
                    Info = Color.FromArgb(2, 132, 199),
                    Warn = Color.FromArgb(234, 88, 12),
                    Bad = Color.FromArgb(220, 38, 38),
                    Violet = Color.FromArgb(124, 58, 237),
                    Teal = Color.FromArgb(13, 148, 136)
                };
            }
        }

        private static Tema T = Tema.Dark();

        public bool ModoOscuro
        {
            get { return T.Oscuro; }
            set { AplicarTema(value); }
        }

        public void AplicarTema(bool oscuro)
        {
            T = oscuro ? Tema.Dark() : Tema.Light();
            BuildUi();
            Recalcular();
        }

        // ============================================================
        //  HELPERS GENERALES
        // ============================================================
        private const TextFormatFlags TL = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;
        private const TextFormatFlags TC = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;
        private const TextFormatFlags TR = TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;

        private static readonly Dictionary<string, Font> FontCache = new Dictionary<string, Font>();

        private static Font F(float size, bool bold = false)
        {
            string key = size.ToString(CultureInfo.InvariantCulture) + (bold ? "b" : "r");
            Font f;
            if (!FontCache.TryGetValue(key, out f))
            {
                f = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point);
                FontCache[key] = f;
            }
            return f;
        }

        private static readonly CultureInfo ES = CrearCultura();

        private static CultureInfo CrearCultura()
        {
            try { return new CultureInfo("es-PE"); }
            catch { return CultureInfo.CurrentCulture; }
        }

        private static string Fecha(DateTime d) { return d.ToString("dd MMM yyyy", ES); }
        private static string FechaHora(DateTime d) { return d.ToString("dd MMM yyyy · HH:mm", ES); }

        private static Color Mix(Color bg, Color fg, float a)
        {
            a = Math.Max(0f, Math.Min(1f, a));
            return Color.FromArgb(
                (int)(bg.R + (fg.R - bg.R) * a),
                (int)(bg.G + (fg.G - bg.G) * a),
                (int)(bg.B + (fg.B - bg.B) * a));
        }

        private static GraphicsPath Round(RectangleF r, float radius)
        {
            GraphicsPath p = new GraphicsPath();
            float d = Math.Min(radius * 2f, Math.Min(r.Width, r.Height));
            if (d <= 1f) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        private static void Txt(Graphics g, string s, Font f, Rectangle r, Color c, TextFormatFlags fl)
        {
            TextRenderer.DrawText(g, s, f, r, c, fl);
        }

        private static Label Lbl(string text, float size, Color color, bool bold = false)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = F(size, bold),
                ForeColor = color,
                BackColor = Color.Transparent
            };
        }

        /// <summary>Color de fondo "real" sobre el que está dibujado un control (para pintar esquinas redondeadas).</summary>
        private static Color OuterOf(Control c)
        {
            for (Control p = c.Parent; p != null; p = p.Parent)
            {
                Card card = p as Card;
                if (card != null) return card.Fill.HasValue ? card.Fill.Value : T.Surface;
                if (p is ScrollPanel) return T.Canvas;
                if (p.BackColor.A == 255 && p.BackColor != Color.Empty) return p.BackColor;
            }
            return T.Canvas;
        }

        private static bool TryKind(string text, out Color c)
        {
            switch (text)
            {
                case "Disponible":
                case "Devuelto":
                case "Reparado":
                case "Bueno":
                case "Entrada":
                    c = T.Ok; return true;
                case "Prestado":
                case "Salida":
                case "En reparación":
                    c = T.Info; return true;
                case "Mantenimiento":
                case "Regular":
                case "Pendiente":
                    c = T.Warn; return true;
                case "Fuera de servicio":
                case "Malo":
                case "Vencido":
                    c = T.Bad; return true;
                case "Traslado":
                    c = T.Violet; return true;
                case "Devolución":
                    c = T.Teal; return true;
            }
            c = Color.Empty;
            return false;
        }

        private enum Ico { Box, Check, Out, Cog, Clock, Swap, List, Person, Search }

        private static void DrawIcon(Graphics g, Ico ico, RectangleF r, Color c)
        {
            SmoothingMode old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(c, Math.Max(1.5f, r.Width / 11f)))
            using (SolidBrush br = new SolidBrush(c))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                Func<float, float, PointF> P = (x, y) => new PointF(r.X + r.Width * x, r.Y + r.Height * y);

                switch (ico)
                {
                    case Ico.Box:
                        g.DrawPolygon(pen, new[] { P(.5f, .06f), P(.92f, .28f), P(.92f, .72f), P(.5f, .94f), P(.08f, .72f), P(.08f, .28f) });
                        g.DrawLine(pen, P(.08f, .28f), P(.5f, .5f));
                        g.DrawLine(pen, P(.92f, .28f), P(.5f, .5f));
                        g.DrawLine(pen, P(.5f, .5f), P(.5f, .94f));
                        break;
                    case Ico.Check:
                        g.DrawEllipse(pen, r.X + r.Width * .06f, r.Y + r.Height * .06f, r.Width * .88f, r.Height * .88f);
                        g.DrawLines(pen, new[] { P(.30f, .52f), P(.45f, .67f), P(.72f, .36f) });
                        break;
                    case Ico.Out:
                        g.DrawLine(pen, P(.22f, .78f), P(.78f, .22f));
                        g.DrawLines(pen, new[] { P(.40f, .22f), P(.78f, .22f), P(.78f, .60f) });
                        break;
                    case Ico.Cog:
                        g.DrawEllipse(pen, r.X + r.Width * .30f, r.Y + r.Height * .30f, r.Width * .40f, r.Height * .40f);
                        for (int k = 0; k < 8; k++)
                        {
                            double a = k * Math.PI / 4.0;
                            float cs = (float)Math.Cos(a), sn = (float)Math.Sin(a);
                            g.DrawLine(pen, P(.5f + .30f * cs, .5f + .30f * sn), P(.5f + .46f * cs, .5f + .46f * sn));
                        }
                        break;
                    case Ico.Clock:
                        g.DrawEllipse(pen, r.X + r.Width * .06f, r.Y + r.Height * .06f, r.Width * .88f, r.Height * .88f);
                        g.DrawLines(pen, new[] { P(.5f, .26f), P(.5f, .5f), P(.68f, .62f) });
                        break;
                    case Ico.Swap:
                        g.DrawLine(pen, P(.14f, .34f), P(.86f, .34f));
                        g.DrawLines(pen, new[] { P(.68f, .16f), P(.86f, .34f), P(.68f, .52f) });
                        g.DrawLine(pen, P(.86f, .68f), P(.14f, .68f));
                        g.DrawLines(pen, new[] { P(.32f, .50f), P(.14f, .68f), P(.32f, .86f) });
                        break;
                    case Ico.List:
                        for (int k = 0; k < 3; k++)
                        {
                            float y = .26f + k * .24f;
                            g.FillEllipse(br, P(.14f, y).X - 1.6f, P(.14f, y).Y - 1.6f, 3.2f, 3.2f);
                            g.DrawLine(pen, P(.36f, y), P(.88f, y));
                        }
                        break;
                    case Ico.Person:
                        g.DrawEllipse(pen, r.X + r.Width * .34f, r.Y + r.Height * .10f, r.Width * .32f, r.Height * .32f);
                        g.DrawArc(pen, r.X + r.Width * .16f, r.Y + r.Height * .52f, r.Width * .68f, r.Height * .70f, 180, 180);
                        break;
                    case Ico.Search:
                        g.DrawEllipse(pen, r.X + r.Width * .10f, r.Y + r.Height * .10f, r.Width * .56f, r.Height * .56f);
                        g.DrawLine(pen, P(.58f, .58f), P(.90f, .90f));
                        break;
                }
            }
            g.SmoothingMode = old;
        }

        // ============================================================
        //  MODELO Y DATOS FICTICIOS
        // ============================================================
        private sealed class Equipo
        {
            public string Codigo, Nombre, Categoria, Ubicacion, Estado, Condicion;
            public int Cantidad;
        }

        private sealed class Movimiento
        {
            public Equipo Equipo;
            public string Tipo, Trabajador, Destino, Observaciones;
            public DateTime Fecha;
        }

        private sealed class Prestamo
        {
            public Equipo Equipo;
            public string Trabajador, Estado, CondicionDevolucion;
            public DateTime Salida, Prevista;
            public DateTime? Devolucion;
        }

        private sealed class Mant
        {
            public Equipo Equipo;
            public string Problema, Descripcion, Responsable, Estado;
            public DateTime Ingreso;
            public DateTime? Fin;
            public int Fotos;
        }

        private static class Demo
        {
            public static readonly List<Equipo> Equipos = new List<Equipo>();
            public static readonly List<Movimiento> Movimientos = new List<Movimiento>();
            public static readonly List<Prestamo> Prestamos = new List<Prestamo>();
            public static readonly List<Mant> Mantenimientos = new List<Mant>();

            public static readonly string[] Categorias = { "Cámaras", "Iluminación", "Audio", "Soportes", "Monitores", "Transmisión", "Accesorios" };
            public static readonly string[] Trabajadores =
            {
                "Carlos Mendoza", "Lucía Fernández", "Jorge Quispe", "Rosa Huamán",
                "Diego Salazar", "Mariana Torres", "Andrés Rojas", "Valeria Castillo"
            };

            static Demo()
            {
                Random r = new Random(2026);
                DateTime hoy = DateTime.Today;

                string[,] spec =
                {
                    { "Sony FX6 Full-Frame", "Cámaras", "CAM" },
                    { "Canon C70 Cinema", "Cámaras", "CAM" },
                    { "Blackmagic Pocket 6K Pro", "Cámaras", "CAM" },
                    { "Sony A7S III", "Cámaras", "CAM" },
                    { "Aputure 300d II", "Iluminación", "LUZ" },
                    { "Nanlite Forza 500", "Iluminación", "LUZ" },
                    { "Panel LED Godox SL-60W", "Iluminación", "LUZ" },
                    { "Softbox Octa 120 cm", "Iluminación", "LUZ" },
                    { "Rode NTG5 Shotgun", "Audio", "AUD" },
                    { "Sennheiser EW 100 G4", "Audio", "AUD" },
                    { "Zoom H6 Grabadora", "Audio", "AUD" },
                    { "Shure SM7B", "Audio", "AUD" },
                    { "Trípode Manfrotto 504X", "Soportes", "SOP" },
                    { "Gimbal DJI RS 3 Pro", "Soportes", "SOP" },
                    { "Slider Edelkrone 80 cm", "Soportes", "SOP" },
                    { "Grúa Jib 3 m", "Soportes", "SOP" },
                    { "Atomos Ninja V", "Monitores", "MON" },
                    { "SmallHD Focus 7\"", "Monitores", "MON" },
                    { "Monitor Lilliput 15\"", "Monitores", "MON" },
                    { "ATEM Mini Pro", "Transmisión", "TRX" },
                    { "Teleprompter 19\"", "Transmisión", "TRX" },
                    { "Intercom Hollyland Solidcom", "Transmisión", "TRX" },
                    { "Encoder Web Presenter", "Transmisión", "TRX" },
                    { "Batería V-Mount 150 Wh", "Accesorios", "ACC" },
                    { "Cable SDI 15 m", "Accesorios", "ACC" },
                    { "Lente Sigma 24-70 mm f/2.8", "Accesorios", "ACC" },
                    { "Tarjeta CFexpress 512 GB", "Accesorios", "ACC" },
                    { "Cargador Dual V-Mount", "Accesorios", "ACC" }
                };

                string[] patron =
                {
                    "Disponible", "Prestado", "Disponible", "Mantenimiento", "Disponible", "Prestado", "Disponible",
                    "Disponible", "Prestado", "Mantenimiento", "Disponible", "Fuera de servicio", "Prestado", "Disponible"
                };
                string[] ubicDisp = { "Almacén principal", "Estudio A", "Estudio B", "Sala de edición" };
                string[] tecnicos = { "Ing. Raúl Paredes", "Téc. Sandra Núñez", "Téc. Marco Villanueva", "Proveedor externo" };

                Func<string[], string> pick = a => a[r.Next(a.Length)];
                Func<int, DateTime> conHora = dias => hoy.AddDays(-dias).AddHours(8 + r.Next(10)).AddMinutes(r.Next(4) * 15);

                // ---------- Equipos ----------
                Dictionary<string, int> cont = new Dictionary<string, int>();
                for (int i = 0; i < spec.GetLength(0); i++)
                {
                    string pre = spec[i, 2];
                    cont[pre] = cont.ContainsKey(pre) ? cont[pre] + 1 : 1;
                    string estado = patron[i % patron.Length];
                    double q = r.NextDouble();
                    string cond;
                    if (estado == "Fuera de servicio") cond = "Malo";
                    else if (estado == "Mantenimiento") cond = q < .6 ? "Regular" : "Malo";
                    else cond = q < .62 ? "Bueno" : (q < .90 ? "Regular" : "Malo");

                    string ubic;
                    if (estado == "Disponible") ubic = pick(ubicDisp);
                    else if (estado == "Prestado") ubic = r.Next(2) == 0 ? "En campo" : "Unidad móvil";
                    else if (estado == "Mantenimiento") ubic = "Taller técnico";
                    else ubic = "Almacén de baja";

                    Equipos.Add(new Equipo
                    {
                        Codigo = "ART-" + pre + "-" + cont[pre].ToString("000"),
                        Nombre = spec[i, 0],
                        Categoria = spec[i, 1],
                        Ubicacion = ubic,
                        Estado = estado,
                        Condicion = cond,
                        Cantidad = estado == "Disponible" ? r.Next(1, 9) : 0
                    });
                }

                string[] destSalida = { "Locación externa", "Evento corporativo", "Estudio de cliente", "Unidad móvil" };
                string[] obsSalida = { "Producción en exteriores", "Cobertura de evento", "Grabación de programa", "Transmisión en vivo" };
                string[] obsDev = { "Devuelto sin novedades", "Revisión conforme", "Se reporta desgaste leve", "Entrega completa con accesorios" };
                string[] obsEntrada = { "Ingreso por compra", "Reposición de stock", "Donación de proveedor" };
                string[] obsTraslado = { "Cambio de sede", "Reubicación en almacén", "Reasignación entre estudios" };

                // ---------- Préstamos activos (equipos prestados) ----------
                foreach (Equipo eq in Equipos.Where(x => x.Estado == "Prestado").ToList())
                {
                    DateTime sal = conHora(r.Next(1, 13));
                    DateTime prev = sal.Date.AddDays(r.Next(2, 8));
                    string trab = pick(Trabajadores);
                    Prestamos.Add(new Prestamo
                    {
                        Equipo = eq,
                        Trabajador = trab,
                        Salida = sal,
                        Prevista = prev,
                        Devolucion = null,
                        Estado = prev < hoy ? "Vencido" : "Pendiente",
                        CondicionDevolucion = "—"
                    });
                    Movimientos.Add(new Movimiento { Equipo = eq, Tipo = "Salida", Fecha = sal, Trabajador = trab, Destino = pick(destSalida), Observaciones = pick(obsSalida) });
                }

                // ---------- Historial de préstamos devueltos ----------
                for (int i = 0; i < 70; i++)
                {
                    Equipo eq = Equipos[r.Next(Equipos.Count)];
                    DateTime sal = conHora(r.Next(4, 360));
                    DateTime prev = sal.Date.AddDays(r.Next(2, 8));
                    DateTime dev = prev.AddDays(r.Next(-1, 4));
                    if (dev <= sal.Date) dev = sal.Date.AddDays(1);
                    if (dev > hoy) dev = hoy;
                    DateTime devH = dev.AddHours(9 + r.Next(9)).AddMinutes(r.Next(4) * 15);
                    double q = r.NextDouble();
                    string trab = pick(Trabajadores);

                    Prestamos.Add(new Prestamo
                    {
                        Equipo = eq,
                        Trabajador = trab,
                        Salida = sal,
                        Prevista = prev,
                        Devolucion = devH,
                        Estado = "Devuelto",
                        CondicionDevolucion = q < .70 ? "Bueno" : (q < .92 ? "Regular" : "Malo")
                    });
                    Movimientos.Add(new Movimiento { Equipo = eq, Tipo = "Salida", Fecha = sal, Trabajador = trab, Destino = pick(destSalida), Observaciones = pick(obsSalida) });
                    Movimientos.Add(new Movimiento { Equipo = eq, Tipo = "Devolución", Fecha = devH, Trabajador = trab, Destino = "Almacén principal", Observaciones = pick(obsDev) });
                }

                // ---------- Entradas y traslados ----------
                for (int i = 0; i < 60; i++)
                {
                    Equipo eq = Equipos[r.Next(Equipos.Count)];
                    bool entrada = r.NextDouble() < .4;
                    Movimientos.Add(new Movimiento
                    {
                        Equipo = eq,
                        Tipo = entrada ? "Entrada" : "Traslado",
                        Fecha = conHora(r.Next(0, 360)),
                        Trabajador = pick(Trabajadores),
                        Destino = entrada ? "Almacén principal" : pick(ubicDisp),
                        Observaciones = entrada ? pick(obsEntrada) : pick(obsTraslado)
                    });
                }

                // ---------- Mantenimiento ----------
                string[] prob = { "Falla de encendido", "Pantalla dañada", "Ruido en audio", "Conector dañado", "Sobrecalentamiento", "Lente con hongos", "Batería no carga", "Sensor sucio" };
                string[] rep = { "Reemplazo de fuente de poder", "Cambio de panel LCD", "Limpieza de circuitos y cambio de cable", "Soldadura de conector", "Limpieza de ventilación y pasta térmica", "Limpieza especializada de lente", "Reemplazo de celdas de batería", "Limpieza profesional del sensor" };

                int k = 0;
                foreach (Equipo eq in Equipos.Where(x => x.Estado == "Mantenimiento").ToList())
                {
                    int ix = r.Next(prob.Length);
                    string est = k % 2 == 0 ? "En reparación" : "Pendiente";
                    Mantenimientos.Add(new Mant
                    {
                        Equipo = eq,
                        Problema = prob[ix],
                        Descripcion = rep[ix],
                        Ingreso = hoy.AddDays(-r.Next(1, 10)),
                        Responsable = est == "Pendiente" ? "Sin asignar" : pick(tecnicos),
                        Estado = est,
                        Fin = null,
                        Fotos = est == "En reparación" ? r.Next(0, 2) : 0
                    });
                    k++;
                }

                for (int i = 0; i < 14; i++)
                {
                    Equipo eq = Equipos[r.Next(Equipos.Count)];
                    int ix = r.Next(prob.Length);
                    DateTime ing = hoy.AddDays(-r.Next(8, 340));
                    DateTime fin = ing.AddDays(r.Next(2, 13));
                    if (fin > hoy) fin = hoy;
                    Mantenimientos.Add(new Mant
                    {
                        Equipo = eq,
                        Problema = prob[ix],
                        Descripcion = rep[ix],
                        Ingreso = ing,
                        Responsable = pick(tecnicos),
                        Estado = "Reparado",
                        Fin = fin,
                        Fotos = r.Next(1, 4)
                    });
                }

                Movimientos.Sort((a, b) => b.Fecha.CompareTo(a.Fecha));
                Prestamos.Sort((a, b) => b.Salida.CompareTo(a.Salida));
                Mantenimientos.Sort((a, b) => b.Ingreso.CompareTo(a.Ingreso));
            }
        }

        // ============================================================
        //  CONTROLES PERSONALIZADOS
        // ============================================================

        // ---- Panel con scroll que puede recibir el foco (rueda del mouse) ----
        private sealed class ScrollPanel : Panel
        {
            public ScrollPanel()
            {
                DoubleBuffered = true;
                SetStyle(ControlStyles.Selectable, true);
                TabStop = true;
            }
        }

        // ---- Tarjeta con esquinas redondeadas ----
        private class Card : Panel
        {
            public Color? Fill;
            public int Radius = 14;

            public Card()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                e.Graphics.Clear(Parent != null ? Parent.BackColor : T.Canvas);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (Width < 6 || Height < 6) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                RectangleF rect = new RectangleF(.5f, .5f, Width - 1.5f, Height - 1.5f);
                using (GraphicsPath path = Round(rect, Radius))
                using (SolidBrush b = new SolidBrush(Fill.HasValue ? Fill.Value : T.Surface))
                using (Pen pen = new Pen(T.Border, 1f))
                {
                    e.Graphics.FillPath(b, path);
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }

        // ---- Botón ----
        private enum BtnStyle { Primary, Secondary, Ghost }

        private sealed class UiButton : Control
        {
            public BtnStyle Style = BtnStyle.Secondary;
            public bool Active;
            private bool hover, down;

            public UiButton(string text, int w, int h)
            {
                Text = text;
                Size = new Size(w, h);
                Cursor = Cursors.Hand;
                Font = F(9f, true);
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }

            protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
            protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
            protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                Color outer = OuterOf(this);
                g.Clear(outer);
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Color fill, stroke, fg;
                if (Style == BtnStyle.Primary || Active)
                {
                    fill = Mix(T.Accent, Color.White, hover ? .15f : 0f);
                    if (down) fill = Mix(T.Accent, Color.Black, .12f);
                    stroke = fill;
                    fg = T.AccentInk;
                }
                else if (Style == BtnStyle.Secondary)
                {
                    fill = Mix(T.SurfaceAlt, T.Ink, hover ? .08f : 0f);
                    stroke = hover ? Mix(T.Border, T.Accent, .5f) : T.Border;
                    fg = T.Ink;
                }
                else
                {
                    fill = hover ? Mix(outer, T.Ink, .08f) : outer;
                    stroke = fill;
                    fg = hover ? T.Ink : T.Muted;
                }

                if (!Enabled)
                {
                    fill = Mix(outer, fill, .5f);
                    stroke = Mix(outer, stroke, .5f);
                    fg = Mix(outer, fg, .4f);
                }

                RectangleF rect = new RectangleF(.5f, .5f, Width - 1.5f, Height - 1.5f);
                using (GraphicsPath path = Round(rect, 10))
                using (SolidBrush br = new SolidBrush(fill))
                using (Pen pen = new Pen(stroke))
                {
                    g.FillPath(br, path);
                    g.DrawPath(pen, path);
                }
                Txt(g, Text, Font, ClientRectangle, fg, TC);
            }
        }

        private static void PaintField(Graphics g, Control c, bool hover, bool focused)
        {
            Color outer = OuterOf(c);
            g.Clear(outer);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color fill = T.SurfaceAlt;
            Color stroke = (hover || focused) ? Mix(T.Border, T.Accent, focused ? .9f : .55f) : T.Border;
            if (!c.Enabled)
            {
                fill = Mix(outer, T.SurfaceAlt, .45f);
                stroke = Mix(outer, T.Border, .6f);
            }
            RectangleF rect = new RectangleF(.5f, .5f, c.Width - 1.5f, c.Height - 1.5f);
            using (GraphicsPath path = Round(rect, 10))
            using (SolidBrush br = new SolidBrush(fill))
            using (Pen pen = new Pen(stroke))
            {
                g.FillPath(br, path);
                g.DrawPath(pen, path);
            }
        }

        private static Color FieldFg(Control c)
        {
            return c.Enabled ? T.Ink : Mix(OuterOf(c), T.Muted, .5f);
        }

        // ---- Menú desplegable con tema ----
        private sealed class ThemedColors : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground { get { return T.Surface; } }
            public override Color MenuBorder { get { return T.Border; } }
            public override Color MenuItemBorder { get { return Mix(T.Surface, T.Accent, .16f); } }
            public override Color MenuItemSelected { get { return Mix(T.Surface, T.Accent, .16f); } }
            public override Color MenuItemSelectedGradientBegin { get { return Mix(T.Surface, T.Accent, .16f); } }
            public override Color MenuItemSelectedGradientEnd { get { return Mix(T.Surface, T.Accent, .16f); } }
            public override Color ImageMarginGradientBegin { get { return T.Surface; } }
            public override Color ImageMarginGradientMiddle { get { return T.Surface; } }
            public override Color ImageMarginGradientEnd { get { return T.Surface; } }
        }

        private static ContextMenuStrip NewMenu(int minWidth)
        {
            ContextMenuStrip m = new ContextMenuStrip();
            m.Font = F(9.5f);
            m.BackColor = T.Surface;
            m.ForeColor = T.Ink;
            m.ShowImageMargin = false;
            m.MinimumSize = new Size(minWidth, 0);
            m.Renderer = new ToolStripProfessionalRenderer(new ThemedColors()) { RoundedEdges = false };
            return m;
        }

        private static void AddMenuItem(ContextMenuStrip m, string text, Action action)
        {
            ToolStripMenuItem it = new ToolStripMenuItem(text);
            it.ForeColor = T.Ink;
            it.Padding = new Padding(6, 6, 6, 6);
            it.Click += (s, e) => action();
            m.Items.Add(it);
        }

        // ---- Lista desplegable ----
        private sealed class SelectBox : Control
        {
            public readonly List<string> Items = new List<string>();
            private int index;
            private bool hover;
            public event EventHandler Picked;

            public SelectBox()
            {
                Height = 40;
                Cursor = Cursors.Hand;
                Font = F(9.5f);
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }

            public int SelectedIndex { get { return index; } set { index = value; Invalidate(); } }
            public string Current { get { return index >= 0 && index < Items.Count ? Items[index] : ""; } }

            public void SetItems(IEnumerable<string> items, int selected)
            {
                Items.Clear();
                Items.AddRange(items);
                index = Math.Min(Math.Max(selected, 0), Math.Max(Items.Count - 1, 0));
                Invalidate();
            }

            protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

            protected override void OnClick(EventArgs e)
            {
                base.OnClick(e);
                if (!Enabled || Items.Count == 0) return;
                ContextMenuStrip m = NewMenu(Width);
                for (int i = 0; i < Items.Count; i++)
                {
                    int idx = i;
                    ToolStripMenuItem it = new ToolStripMenuItem(Items[i]);
                    it.ForeColor = idx == index ? T.AccentText : T.Ink;
                    if (idx == index) it.Font = F(9.5f, true);
                    it.Padding = new Padding(6, 6, 6, 6);
                    it.Click += (s, a) =>
                    {
                        index = idx;
                        Invalidate();
                        EventHandler h = Picked;
                        if (h != null) h(this, EventArgs.Empty);
                    };
                    m.Items.Add(it);
                }
                m.Show(this, new Point(0, Height + 4));
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                PaintField(g, this, hover, false);
                Txt(g, Current, Font, new Rectangle(14, 0, Width - 44, Height), FieldFg(this), TL);
                using (Pen pen = new Pen(Enabled ? T.Muted : Mix(OuterOf(this), T.Muted, .5f), 1.8f))
                {
                    pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                    float x = Width - 20, y = Height / 2f;
                    g.DrawLines(pen, new[] { new PointF(x - 4, y - 2), new PointF(x, y + 2), new PointF(x + 4, y - 2) });
                }
            }
        }

        // ---- Selector de fecha ----
        private sealed class DateBox : Control
        {
            private DateTime value = DateTime.Today;
            private bool hover;
            public event EventHandler UserChanged;

            public DateBox()
            {
                Height = 40;
                Cursor = Cursors.Hand;
                Font = F(9.5f);
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }

            public DateTime Value { get { return value; } set { this.value = value.Date; Invalidate(); } }

            protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

            protected override void OnClick(EventArgs e)
            {
                base.OnClick(e);
                if (!Enabled) return;

                MonthCalendar cal = new MonthCalendar();
                cal.MaxSelectionCount = 1;
                cal.ShowToday = false;
                cal.ShowTodayCircle = true;
                cal.FirstDayOfWeek = Day.Monday;
                cal.BackColor = T.Surface;
                cal.ForeColor = T.Ink;
                cal.TitleBackColor = T.SurfaceAlt;
                cal.TitleForeColor = T.Ink;
                cal.TrailingForeColor = T.Muted;
                cal.SetDate(value);

                ToolStripControlHost host = new ToolStripControlHost(cal);
                host.Margin = Padding.Empty;
                host.Padding = Padding.Empty;

                ToolStripDropDown dd = new ToolStripDropDown();
                dd.Padding = Padding.Empty;
                dd.BackColor = T.Surface;
                dd.AutoClose = true;
                dd.Items.Add(host);

                cal.DateSelected += (s, a) =>
                {
                    value = a.Start.Date;
                    Invalidate();
                    dd.Close();
                    EventHandler h = UserChanged;
                    if (h != null) h(this, EventArgs.Empty);
                };
                dd.Show(this, new Point(0, Height + 4));
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                PaintField(g, this, hover, false);
                Txt(g, Fecha(value), Font, new Rectangle(14, 0, Width - 48, Height), FieldFg(this), TL);

                Color ic = Enabled ? T.Muted : Mix(OuterOf(this), T.Muted, .5f);
                using (Pen pen = new Pen(ic, 1.5f))
                {
                    float x = Width - 32, y = Height / 2f - 7;
                    g.DrawRectangle(pen, x, y + 2, 15, 13);
                    g.DrawLine(pen, x, y + 6, x + 15, y + 6);
                    g.DrawLine(pen, x + 4, y, x + 4, y + 4);
                    g.DrawLine(pen, x + 11, y, x + 11, y + 4);
                }
            }
        }

        // ---- Cuadro de texto ----
        private sealed class InputBox : Control
        {
            public readonly TextBox Inner = new TextBox();
            private readonly bool searchIcon;
            public event EventHandler Changed;

            public InputBox(string placeholder, bool icon)
            {
                searchIcon = icon;
                Height = 40;
                Font = F(9.5f);
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

                Inner.BorderStyle = BorderStyle.None;
                Inner.Font = F(9.5f);
                Inner.PlaceholderText = placeholder;
                Inner.BackColor = T.SurfaceAlt;
                Inner.ForeColor = T.Ink;
                Inner.GotFocus += (s, e) => Invalidate();
                Inner.LostFocus += (s, e) => Invalidate();
                Inner.TextChanged += (s, e) =>
                {
                    EventHandler h = Changed;
                    if (h != null) h(this, EventArgs.Empty);
                };
                Controls.Add(Inner);
                PositionInner();
            }

            public override string Text
            {
                get { return Inner.Text; }
                set { Inner.Text = value; }
            }

            private void PositionInner()
            {
                int left = searchIcon ? 38 : 14;
                Inner.Left = left;
                Inner.Width = Math.Max(20, Width - left - 14);
                Inner.Top = Math.Max(0, (Height - Inner.Height) / 2);
            }

            protected override void OnResize(EventArgs e) { base.OnResize(e); PositionInner(); }
            protected override void OnMouseDown(MouseEventArgs e) { Inner.Focus(); base.OnMouseDown(e); }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                PaintField(g, this, false, Inner.Focused);
                if (searchIcon) DrawIcon(g, Ico.Search, new RectangleF(13, Height / 2f - 9, 18, 18), T.Muted);
            }
        }

        // ---- Pestañas de período rápido ----
        private sealed class Segmented : Control
        {
            private readonly string[] items;
            private int sel;
            private int hover = -1;
            public event EventHandler SelectedChanged;

            public Segmented(string[] it, int selected)
            {
                items = it;
                sel = selected;
                Cursor = Cursors.Hand;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }

            public int SelectedIndex { get { return sel; } set { sel = value; Invalidate(); } }

            private int IndexAt(int x)
            {
                if (items.Length == 0) return -1;
                float w = (Width - 8f) / items.Length;
                int i = (int)((x - 4) / w);
                return Math.Max(0, Math.Min(items.Length - 1, i));
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                int i = IndexAt(e.X);
                if (i != hover) { hover = i; Invalidate(); }
                base.OnMouseMove(e);
            }

            protected override void OnMouseLeave(EventArgs e) { hover = -1; Invalidate(); base.OnMouseLeave(e); }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                if (e.Button != MouseButtons.Left) return;
                int i = IndexAt(e.X);
                if (i < 0) return;
                sel = i;
                Invalidate();
                EventHandler h = SelectedChanged;
                if (h != null) h(this, EventArgs.Empty);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.Clear(OuterOf(this));
                g.SmoothingMode = SmoothingMode.AntiAlias;
                RectangleF rect = new RectangleF(.5f, .5f, Width - 1.5f, Height - 1.5f);
                using (GraphicsPath path = Round(rect, 12))
                using (SolidBrush br = new SolidBrush(T.Surface))
                using (Pen pen = new Pen(T.Border))
                {
                    g.FillPath(br, path);
                    g.DrawPath(pen, path);
                }
                if (items.Length == 0) return;

                float w = (Width - 8f) / items.Length;
                for (int i = 0; i < items.Length; i++)
                {
                    RectangleF r = new RectangleF(4 + w * i, 4, w, Height - 8f);
                    bool on = i == sel;
                    if (on)
                    {
                        using (GraphicsPath path = Round(r, 9))
                        using (SolidBrush br = new SolidBrush(Mix(T.Surface, T.Accent, .18f)))
                        using (Pen pen = new Pen(Mix(T.Surface, T.Accent, .55f)))
                        {
                            g.FillPath(br, path);
                            g.DrawPath(pen, path);
                        }
                    }
                    Color fg = on ? T.AccentText : (i == hover ? T.Ink : T.Muted);
                    Txt(g, items[i], F(9f, on), Rectangle.Round(r), fg, TC);
                }
            }
        }

        // ---- Icono en cuadro redondeado ----
        private sealed class IconBadge : Control
        {
            public Ico Glyph;
            public Color Tint;

            public IconBadge(Ico glyph, Color tint, int size)
            {
                Glyph = glyph;
                Tint = tint;
                Size = new Size(size, size);
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                Color outer = OuterOf(this);
                g.Clear(outer);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = Round(new RectangleF(0, 0, Width - 1, Height - 1), 10))
                using (SolidBrush br = new SolidBrush(Mix(outer, Tint, .16f)))
                    g.FillPath(br, path);
                float s = Width * .5f;
                DrawIcon(g, Glyph, new RectangleF((Width - s) / 2f, (Height - s) / 2f, s, s), Tint);
            }
        }

        // ---- Tarjeta de categoría de reporte ----
        private sealed class TabCard : Control
        {
            public Ico Glyph;
            public string Title, Desc;
            public int Count;
            public bool Selected;
            private bool hover;

            public TabCard(Ico glyph, string title, string desc)
            {
                Glyph = glyph;
                Title = title;
                Desc = desc;
                Cursor = Cursors.Hand;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }

            protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.Clear(OuterOf(this));
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Color fill = Selected ? Mix(T.Surface, T.Accent, .10f) : T.Surface;
                Color stroke = Selected ? T.Accent : (hover ? Mix(T.Border, T.Accent, .5f) : T.Border);
                RectangleF rect = new RectangleF(.5f, .5f, Width - 1.5f, Height - 1.5f);
                using (GraphicsPath path = Round(rect, 14))
                using (SolidBrush br = new SolidBrush(fill))
                using (Pen pen = new Pen(stroke, Selected ? 1.6f : 1f))
                {
                    g.FillPath(br, path);
                    g.DrawPath(pen, path);
                }

                Color tint = Selected ? T.AccentText : T.Muted;
                RectangleF ib = new RectangleF(16, (Height - 44) / 2f, 44, 44);
                using (GraphicsPath path = Round(ib, 11))
                using (SolidBrush br = new SolidBrush(Mix(fill, tint, .16f)))
                    g.FillPath(br, path);
                DrawIcon(g, Glyph, new RectangleF(ib.X + 12, ib.Y + 12, 20, 20), tint);

                Txt(g, Title, F(9.5f, true), new Rectangle(72, 17, Width - 72 - 14, 22), T.Ink, TL);

                string c = Count.ToString();
                Size ts = TextRenderer.MeasureText(c, F(8.5f, true), new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                int pw = Math.Max(30, ts.Width + 18);
                Rectangle pill = new Rectangle(Width - 16 - pw, 45, pw, 22);
                using (GraphicsPath path = Round(pill, 11))
                using (SolidBrush br = new SolidBrush(Selected ? T.Accent : T.SurfaceAlt))
                    g.FillPath(br, path);
                Txt(g, c, F(8.5f, true), pill, Selected ? T.AccentInk : T.Muted, TC);

                Txt(g, Desc, F(8.5f), new Rectangle(72, 45, Math.Max(10, Width - 72 - pw - 28), 22), T.Muted, TL);
            }
        }

        // ---- Notificación flotante ----
        private sealed class Toast : Control
        {
            private string title = "", msg = "";
            private Color accent = Color.Gray;
            private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

            public Toast()
            {
                Size = new Size(400, 84);
                Visible = false;
                Cursor = Cursors.Hand;
                Anchor = AnchorStyles.Top | AnchorStyles.Right;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                timer.Interval = 3800;
                timer.Tick += (s, e) => { timer.Stop(); Visible = false; };
                Click += (s, e) => { timer.Stop(); Visible = false; };
            }

            public void Display(string t, string m, Color c)
            {
                title = t; msg = m; accent = c;
                if (Parent != null) Location = new Point(Math.Max(8, Parent.ClientSize.Width - Width - 28), 20);
                Visible = true;
                BringToFront();
                timer.Stop();
                timer.Start();
                Invalidate();
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                if (Width > 4 && Height > 4)
                {
                    using (GraphicsPath p = Round(new RectangleF(0, 0, Width, Height), 12))
                    {
                        if (Region != null) Region.Dispose();
                        Region = new Region(p);
                    }
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) timer.Dispose();
                base.Dispose(disposing);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.Clear(T.Surface);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen pen = new Pen(T.Border, 2f))
                    g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                using (SolidBrush br = new SolidBrush(accent))
                    g.FillRectangle(br, 0, 0, 5, Height);
                Txt(g, title, F(10f, true), new Rectangle(22, 12, Width - 40, 22), T.Ink, TL);
                Txt(g, msg, F(9f), new Rectangle(22, 36, Width - 40, 40), T.Muted, TextFormatFlags.Left | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
            }
        }

        // ---- Estado vacío ----
        private sealed class EmptyState : Control
        {
            public EmptyState()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.Clear(T.Surface);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int cx = Width / 2;
                Rectangle ib = new Rectangle(cx - 28, 24, 56, 56);
                using (GraphicsPath p = Round(ib, 16))
                using (SolidBrush br = new SolidBrush(Mix(T.Surface, T.Muted, .14f)))
                    g.FillPath(br, p);
                DrawIcon(g, Ico.Search, new RectangleF(ib.X + 15, ib.Y + 15, 26, 26), T.Muted);
                Txt(g, "No se encontraron resultados", F(11.5f, true), new Rectangle(0, 94, Width, 26), T.Ink, TC);
                Txt(g, "Prueba ajustando los filtros, el rango de fechas o la búsqueda rápida.", F(9f), new Rectangle(0, 122, Width, 22), T.Muted, TC);
            }
        }

        // ---- Tabla ----
        private sealed class FlatGrid : DataGridView
        {
            public FlatGrid() { DoubleBuffered = true; }

            protected override void OnMouseWheel(MouseEventArgs e)
            {
                for (Control p = Parent; p != null; p = p.Parent)
                {
                    ScrollableControl sc = p as ScrollableControl;
                    if (sc != null && sc.AutoScroll)
                    {
                        sc.AutoScrollPosition = new Point(-sc.AutoScrollPosition.X, -sc.AutoScrollPosition.Y - e.Delta);
                        return;
                    }
                }
                base.OnMouseWheel(e);
            }
        }

        // ============================================================
        //  GRÁFICOS
        // ============================================================
        private sealed class DonutChart : Control
        {
            private string[] labels = new string[0];
            private int[] values = new int[0];
            private Color[] colors = new Color[0];

            public DonutChart()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }

            public void SetData(string[] l, int[] v, Color[] c)
            {
                labels = l; values = v; colors = c;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.Clear(T.Surface);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                if (Width < 120 || Height < 80) return;

                int total = values.Sum();
                int size = Math.Max(90, Math.Min(Height - 10, (int)(Width * .46f)));
                Rectangle ring = new Rectangle(4, (Height - size) / 2, size, size);
                float pw = size * .15f;
                Rectangle arc = Rectangle.Inflate(ring, -(int)(pw / 2), -(int)(pw / 2));

                using (Pen track = new Pen(Mix(T.Surface, T.Muted, .16f), pw))
                    g.DrawEllipse(track, arc);

                float start = -90f;
                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i] <= 0 || total <= 0) continue;
                    float sweep = values[i] / (float)total * 360f;
                    float gap = sweep > 8f ? 3f : 0f;
                    using (Pen p = new Pen(colors[i], pw))
                        g.DrawArc(p, arc, start + gap / 2f, sweep - gap);
                    start += sweep;
                }

                Txt(g, total.ToString(), F(20f, true), new Rectangle(ring.X, ring.Y + size / 2 - 26, ring.Width, 30), T.Ink, TC);
                Txt(g, "Equipos", F(8.5f), new Rectangle(ring.X, ring.Y + size / 2 + 4, ring.Width, 18), T.Muted, TC);

                int x0 = ring.Right + 28;
                int n = labels.Length;
                int rowH = 40;
                int y0 = Math.Max(0, (Height - n * rowH) / 2);
                int right = Width - 6;
                for (int i = 0; i < n; i++)
                {
                    int y = y0 + i * rowH;
                    using (SolidBrush b = new SolidBrush(colors[i]))
                        g.FillEllipse(b, x0, y + 7, 9, 9);
                    Txt(g, labels[i], F(9.5f), new Rectangle(x0 + 18, y, Math.Max(10, right - x0 - 70), 22), T.Ink, TL);
                    Txt(g, values[i].ToString(), F(10f, true), new Rectangle(right - 50, y, 50, 22), T.Ink, TR);
                    string pct = total > 0 ? (values[i] * 100.0 / total).ToString("0.#", CultureInfo.InvariantCulture) + "%" : "0%";
                    Txt(g, pct + " del total", F(8f), new Rectangle(x0 + 18, y + 19, Math.Max(10, right - x0 - 20), 16), T.Muted, TL);
                }
            }
        }

        private sealed class BarChart : Control
        {
            private string[] labels = new string[0];
            private int[] a = new int[0];
            private int[] b = new int[0];

            public BarChart()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }

            public void SetData(string[] l, int[] salidas, int[] ingresos)
            {
                labels = l; a = salidas; b = ingresos;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.Clear(T.Surface);
                g.SmoothingMode = SmoothingMode.AntiAlias;

                int n = labels.Length;
                int left = 32, right = Width - 4, top = 32, bottom = Height - 26;
                if (n == 0 || right - left < 60 || bottom - top < 40) return;

                int max = 0;
                for (int i = 0; i < n; i++) { max = Math.Max(max, Math.Max(a[i], b[i])); }
                int niceMax = Math.Max(4, (int)Math.Ceiling(max / 4.0) * 4);

                // leyenda
                using (SolidBrush b1 = new SolidBrush(T.Info))
                    g.FillEllipse(b1, left, 8, 9, 9);
                Txt(g, "Salidas", F(8.5f), new Rectangle(left + 14, 3, 60, 18), T.Muted, TL);
                using (SolidBrush b2 = new SolidBrush(T.Ok))
                    g.FillEllipse(b2, left + 84, 8, 9, 9);
                Txt(g, "Ingresos", F(8.5f), new Rectangle(left + 98, 3, 70, 18), T.Muted, TL);

                using (Pen gridPen = new Pen(Mix(T.Surface, T.Muted, .14f), 1f))
                {
                    for (int k = 0; k <= 4; k++)
                    {
                        int y = bottom - (bottom - top) * k / 4;
                        g.DrawLine(gridPen, left, y, right, y);
                        Txt(g, (niceMax * k / 4).ToString(), F(7.5f), new Rectangle(0, y - 8, left - 6, 16), T.Muted, TR);
                    }
                }

                float groupW = (right - left) / (float)n;
                float barW = Math.Min(20f, groupW / 3.2f);
                float gap = 4f;
                float span = bottom - top;

                for (int i = 0; i < n; i++)
                {
                    float cx = left + groupW * (i + .5f);
                    float hA = a[i] / (float)niceMax * span;
                    float hB = b[i] / (float)niceMax * span;

                    if (hA > 0.5f)
                    {
                        RectangleF ra = new RectangleF(cx - gap / 2f - barW, bottom - hA, barW, hA);
                        using (GraphicsPath p = Round(ra, 4))
                        using (SolidBrush br = new SolidBrush(T.Info))
                            g.FillPath(br, p);
                        Txt(g, a[i].ToString(), F(7.5f), new Rectangle((int)ra.X - 6, (int)ra.Y - 15, (int)barW + 12, 14), T.Muted, TC);
                    }
                    if (hB > 0.5f)
                    {
                        RectangleF rb = new RectangleF(cx + gap / 2f, bottom - hB, barW, hB);
                        using (GraphicsPath p = Round(rb, 4))
                        using (SolidBrush br = new SolidBrush(T.Ok))
                            g.FillPath(br, p);
                        Txt(g, b[i].ToString(), F(7.5f), new Rectangle((int)rb.X - 6, (int)rb.Y - 15, (int)barW + 12, 14), T.Muted, TC);
                    }
                    Txt(g, labels[i], F(8f), new Rectangle((int)(cx - groupW / 2f), bottom + 5, (int)groupW, 18), T.Muted, TC);
                }
            }
        }

        private sealed class HBarChart : Control
        {
            private string[] labels = new string[0];
            private int[] values = new int[0];
            private Color[] colors = new Color[0];

            public HBarChart()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }

            public void SetData(string[] l, int[] v, Color[] c)
            {
                labels = l; values = v; colors = c;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.Clear(T.Surface);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int n = labels.Length;
                if (n == 0 || Width < 80) return;

                int max = Math.Max(1, values.Max());
                int rowH = Math.Min(64, Height / n);
                int y0 = Math.Max(0, (Height - rowH * n) / 2);

                for (int i = 0; i < n; i++)
                {
                    int y = y0 + i * rowH;
                    Txt(g, labels[i], F(9.5f), new Rectangle(0, y, Width - 60, 22), T.Ink, TL);
                    Txt(g, values[i].ToString(), F(11f, true), new Rectangle(Width - 56, y - 1, 56, 24), T.Ink, TR);

                    RectangleF track = new RectangleF(0, y + 28, Width - 2, 8);
                    using (GraphicsPath p = Round(track, 4))
                    using (SolidBrush br = new SolidBrush(Mix(T.Surface, T.Muted, .16f)))
                        g.FillPath(br, p);

                    if (values[i] > 0)
                    {
                        float w = Math.Max(10f, track.Width * values[i] / max);
                        using (GraphicsPath p = Round(new RectangleF(track.X, track.Y, w, track.Height), 4))
                        using (SolidBrush br = new SolidBrush(colors[i]))
                            g.FillPath(br, p);
                    }
                }
            }
        }

        // ============================================================
        //  ESTADO DE LA PANTALLA
        // ============================================================
        private enum Seccion { Inventario = 0, Movimientos = 1, Prestamos = 2, Mantenimiento = 3 }

        private sealed class Col
        {
            public string H; public float W; public string K;
            public Col(string h, float w, string k = null) { H = h; W = w; K = k; }
        }

        private const int HeaderH = 46;
        private const int RowH = 50;
        private static readonly string[] TiposMov = { "Todos", "Entrada", "Salida", "Traslado", "Devolución" };

        private Seccion seccion = Seccion.Inventario;

        // filtros confirmados (se aplican con "Aplicar filtros")
        private int periodoIdx = 1;
        private DateTime fDesde = DateTime.Today.AddDays(-30);
        private DateTime fHasta = DateTime.Today;
        private string fCategoria = "Todas", fTexto = "", fTrabajador = "Todos", fEstado = "Todos", fTipo = "Todos";
        private string busquedaRapida = "";
        private int pageSize = 10, pagina = 1;
        private bool building;

        private ScrollPanel scroll;
        private Toast toast;
        private UiButton btnActualizar, btnExportar, btnPdf, btnExcel, btnPrint, btnAplicar, btnLimpiar;
        private Segmented segPeriodo;
        private Label lblRango;
        private readonly Label[] kpiValue = new Label[5];
        private readonly Label[] kpiSub = new Label[5];
        private TabCard[] tabCards;
        private DateBox dtDesde, dtHasta;
        private SelectBox selCategoria, selTrabajador, selEstado, selTipo, selPagina;
        private InputBox inpEquipo, inpBusqueda;
        private Label[] caps;
        private Control[] filterCtrls;
        private Label lblHint;
        private DonutChart donut;
        private BarChart barras;
        private HBarChart hbar;
        private Panel tableHost, pagerHost, footerPanel;
        private FlatGrid grid;
        private EmptyState emptyPanel;
        private Label lblTablaTitulo, lblTablaSub, lblInfo, lblFoot;
        private int columnsFor = -1;
        private int hoverRow = -1;
        private List<string[]> baseRows = new List<string[]>();
        private List<string[]> viewRows = new List<string[]>();

        public CotrolReporte()
        {
            Dock = DockStyle.Fill;
            DoubleBuffered = true;
            Font = F(9f);
            BackColor = T.Canvas;

            BuildUi();
            Recalcular();
        }

        // ============================================================
        //  CONSTRUCCIÓN DE LA INTERFAZ
        // ============================================================
        private void BuildUi()
        {
            building = true;
            SuspendLayout();

            if (scroll != null) { Controls.Remove(scroll); scroll.Dispose(); }
            if (toast != null) { Controls.Remove(toast); toast.Dispose(); }
            columnsFor = -1;
            hoverRow = -1;
            BackColor = T.Canvas;

            scroll = new ScrollPanel { Dock = DockStyle.Fill, BackColor = T.Canvas, AutoScroll = true };
            scroll.AutoScrollMinSize = new Size(1100, 0);

            List<Control> secciones = new List<Control>
            {
                BuildHeader(),
                BuildPeriodRow(),
                BuildKpiRow(),
                BuildTabsRow(),
                BuildFilters(),
                BuildCharts(),
                BuildTable(),
                BuildFootnote()
            };
            for (int i = secciones.Count - 1; i >= 0; i--) scroll.Controls.Add(secciones[i]);

            Controls.Add(scroll);
            toast = new Toast();
            Controls.Add(toast);
            toast.BringToFront();

            HookWheel(scroll);
            ResumeLayout(true);
            building = false;
        }

        private void HookWheel(Control root)
        {
            foreach (Control c in root.Controls)
            {
                if (c is InputBox || c is FlatGrid) continue;
                c.MouseEnter += (s, e) =>
                {
                    bool escribiendo = (inpEquipo != null && inpEquipo.Inner.Focused) || (inpBusqueda != null && inpBusqueda.Inner.Focused);
                    if (scroll != null && !escribiendo) scroll.Focus();
                };
                c.MouseDown += (s, e) => { if (scroll != null) scroll.Focus(); };
                HookWheel(c);
            }
        }

        private void RunSilent(Action a)
        {
            bool old = building;
            building = true;
            try { a(); }
            finally { building = old; }
        }

        // ---------------- Encabezado ----------------
        private Control BuildHeader()
        {
            Panel host = new Panel { Dock = DockStyle.Top, Height = 104, BackColor = T.Canvas, Padding = new Padding(24, 18, 24, 6) };
            Panel p = new Panel { Dock = DockStyle.Fill, BackColor = T.Canvas };

            Label crumb = Lbl("GESTIÓN Y ANÁLISIS", 8f, T.AccentText, true);
            crumb.Location = new Point(2, 0);
            Label title = Lbl("Reportes audiovisuales", 22f, T.Ink, true);
            title.Location = new Point(0, 16);
            Label sub = Lbl("Consulta, seguimiento y control de equipos audiovisuales", 9.5f, T.Muted);
            sub.Location = new Point(2, 58);

            btnExportar = new UiButton("Exportar reporte  ▾", 160, 40);
            btnExportar.Click += (s, e) => MostrarMenuExportar(btnExportar);
            btnActualizar = new UiButton("↻  Actualizar", 130, 40) { Style = BtnStyle.Primary };
            btnActualizar.Click += (s, e) =>
            {
                Recalcular();
                ShowToast("Vista actualizada", "Los datos de demostración se recargaron correctamente.", T.Ok);
            };

            p.Controls.Add(crumb);
            p.Controls.Add(title);
            p.Controls.Add(sub);
            p.Controls.Add(btnExportar);
            p.Controls.Add(btnActualizar);

            Action pos = () =>
            {
                btnActualizar.Location = new Point(Math.Max(0, p.Width - btnActualizar.Width), 14);
                btnExportar.Location = new Point(Math.Max(0, btnActualizar.Left - btnExportar.Width - 10), 14);
            };
            p.Resize += (s, e) => pos();
            host.Controls.Add(p);
            pos();
            return host;
        }

        private void MostrarMenuExportar(Control anchor)
        {
            ContextMenuStrip m = NewMenu(anchor.Width);
            AddMenuItem(m, "Exportar a PDF", () => SimularExportacion("PDF"));
            AddMenuItem(m, "Exportar a Excel", () => SimularExportacion("Excel"));
            AddMenuItem(m, "Imprimir reporte", () => SimularExportacion("impresión"));
            m.Show(anchor, new Point(0, anchor.Height + 4));
        }

        // ---------------- Período ----------------
        private Control BuildPeriodRow()
        {
            Panel host = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = T.Canvas, Padding = new Padding(24, 0, 24, 16) };
            Panel p = new Panel { Dock = DockStyle.Fill, BackColor = T.Canvas };

            segPeriodo = new Segmented(new[] { "7 días", "30 días", "3 meses", "Este año" }, periodoIdx)
            {
                Size = new Size(420, 42),
                Location = new Point(0, 0)
            };
            segPeriodo.SelectedChanged += (s, e) =>
            {
                if (building) return;
                AplicarPeriodoRapido(segPeriodo.SelectedIndex);
            };

            Card chip = new Card { Size = new Size(320, 42), Radius = 12 };
            lblRango = Lbl("", 9.5f, T.Ink, true);
            lblRango.AutoSize = false;
            lblRango.Dock = DockStyle.Fill;
            lblRango.TextAlign = ContentAlignment.MiddleCenter;
            chip.Controls.Add(lblRango);

            p.Controls.Add(segPeriodo);
            p.Controls.Add(chip);
            p.Resize += (s, e) => chip.Location = new Point(Math.Max(0, p.Width - chip.Width), 0);
            host.Controls.Add(p);
            chip.Location = new Point(Math.Max(0, p.Width - chip.Width), 0);
            return host;
        }

        private void AplicarPeriodoRapido(int idx)
        {
            DateTime hoy = DateTime.Today;
            DateTime d;
            switch (idx)
            {
                case 0: d = hoy.AddDays(-7); break;
                case 1: d = hoy.AddDays(-30); break;
                case 2: d = hoy.AddMonths(-3); break;
                default: d = new DateTime(hoy.Year, 1, 1); break;
            }
            dtDesde.Value = d;
            dtHasta.Value = hoy;
            AplicarFiltros();
        }

        private int DetectarPeriodo()
        {
            DateTime hoy = DateTime.Today;
            if (fHasta.Date != hoy) return -1;
            DateTime d = fDesde.Date;
            if (d == hoy.AddDays(-7)) return 0;
            if (d == hoy.AddDays(-30)) return 1;
            if (d == hoy.AddMonths(-3)) return 2;
            if (d == new DateTime(hoy.Year, 1, 1)) return 3;
            return -1;
        }

        // ---------------- Tarjetas de resumen ----------------
        private Control BuildKpiRow()
        {
            Panel host = new Panel { Dock = DockStyle.Top, Height = 144, BackColor = T.Canvas, Padding = new Padding(24, 0, 24, 16) };
            TableLayoutPanel t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1, BackColor = T.Canvas, Margin = Padding.Empty };
            for (int i = 0; i < 5; i++) t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            string[] titles = { "Total de equipos", "Disponibles", "Prestados", "En mantenimiento", "Devol. pendientes" };
            Ico[] icons = { Ico.Box, Ico.Check, Ico.Out, Ico.Cog, Ico.Clock };
            Color[] colors = { T.AccentText, T.Teal, T.Info, T.Warn, T.Bad };

            for (int i = 0; i < 5; i++)
            {
                Card card = new Card { Dock = DockStyle.Fill, Margin = new Padding(i == 0 ? 0 : 6, 0, i == 4 ? 0 : 6, 0) };

                Label title = Lbl(titles[i], 9f, T.Muted, true);
                title.Location = new Point(18, 18);
                kpiValue[i] = Lbl("0", 26f, T.Ink, true);
                kpiValue[i].Location = new Point(14, 44);
                kpiSub[i] = Lbl("", 8.5f, T.Muted);
                kpiSub[i].Location = new Point(18, 94);

                IconBadge badge = new IconBadge(icons[i], colors[i], 36);
                card.Controls.Add(title);
                card.Controls.Add(kpiValue[i]);
                card.Controls.Add(kpiSub[i]);
                card.Controls.Add(badge);
                card.Resize += (s, e) => badge.Location = new Point(card.Width - 18 - badge.Width, 14);
                badge.Location = new Point(Math.Max(0, card.Width - 18 - badge.Width), 14);

                t.Controls.Add(card, i, 0);
            }

            host.Controls.Add(t);
            return host;
        }

        // ---------------- Categorías de reporte ----------------
        private Control BuildTabsRow()
        {
            Panel host = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = T.Canvas, Padding = new Padding(24, 0, 24, 16) };
            TableLayoutPanel t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = T.Canvas, Margin = Padding.Empty };
            for (int i = 0; i < 4; i++) t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            string[] titles = { "Inventario", "Movimientos", "Préstamos y devoluciones", "Mantenimiento" };
            string[] descs = { "Stock y condición", "Entradas y salidas", "Control de préstamos", "Averías y reparaciones" };
            Ico[] icons = { Ico.List, Ico.Swap, Ico.Person, Ico.Cog };

            tabCards = new TabCard[4];
            for (int i = 0; i < 4; i++)
            {
                Seccion destino = (Seccion)i;
                TabCard tc = new TabCard(icons[i], titles[i], descs[i])
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(i == 0 ? 0 : 6, 0, i == 3 ? 0 : 6, 0),
                    Selected = i == (int)seccion
                };
                tc.Click += (s, e) => CambiarSeccion(destino);
                tabCards[i] = tc;
                t.Controls.Add(tc, i, 0);
            }

            host.Controls.Add(t);
            return host;
        }

        private void CambiarSeccion(Seccion s)
        {
            if (s == seccion) return;
            seccion = s;
            fEstado = "Todos";
            busquedaRapida = "";
            RunSilent(() => inpBusqueda.Text = "");
            for (int i = 0; i < tabCards.Length; i++)
            {
                tabCards[i].Selected = i == (int)seccion;
                tabCards[i].Invalidate();
            }
            ConfigurarFiltros();
            pagina = 1;
            Recalcular();
        }

        // ---------------- Filtros ----------------
        private Panel Field(string caption, Control ctl, out Label cap)
        {
            Panel p = new Panel { Dock = DockStyle.Fill, BackColor = T.Surface, Margin = new Padding(0, 0, 14, 6) };
            cap = Lbl(caption.ToUpperInvariant(), 7.5f, T.Muted, true);
            cap.Location = new Point(0, 2);
            ctl.Location = new Point(0, 24);
            ctl.Size = new Size(Math.Max(40, p.Width), 40);
            p.Controls.Add(cap);
            p.Controls.Add(ctl);
            p.Resize += (s, e) => ctl.Width = Math.Max(40, p.Width);
            return p;
        }

        private Control BuildFilters()
        {
            Panel host = new Panel { Dock = DockStyle.Top, Height = 232, BackColor = T.Canvas, Padding = new Padding(24, 0, 24, 16) };
            Card card = new Card { Dock = DockStyle.Fill, Padding = new Padding(1, 10, 1, 10) };

            dtDesde = new DateBox { Value = fDesde };
            dtHasta = new DateBox { Value = fHasta };
            dtDesde.UserChanged += (s, e) => segPeriodo.SelectedIndex = -1;
            dtHasta.UserChanged += (s, e) => segPeriodo.SelectedIndex = -1;

            selCategoria = new SelectBox();
            selCategoria.SetItems(new[] { "Todas" }.Concat(Demo.Categorias), Math.Max(0, Array.IndexOf(Demo.Categorias, fCategoria) + 1));

            inpEquipo = new InputBox("Nombre o código…", true) { Text = fTexto };
            inpEquipo.Inner.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AplicarFiltros(); }
            };

            selTrabajador = new SelectBox();
            selTrabajador.SetItems(new[] { "Todos" }.Concat(Demo.Trabajadores), Math.Max(0, Array.IndexOf(Demo.Trabajadores, fTrabajador) + 1));

            selEstado = new SelectBox();
            selTipo = new SelectBox();
            selTipo.SetItems(TiposMov, Math.Max(0, Array.IndexOf(TiposMov, fTipo)));

            Control[] cs = { dtDesde, dtHasta, selCategoria, inpEquipo, selTrabajador, selEstado, selTipo };
            string[] names = { "Fecha inicial", "Fecha final", "Categoría", "Equipo (nombre o código)", "Trabajador responsable", "Estado", "Tipo de movimiento" };

            TableLayoutPanel tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 2,
                BackColor = T.Surface,
                Padding = new Padding(20, 0, 6, 0),
                Margin = Padding.Empty
            };
            for (int i = 0; i < 4; i++) tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));

            caps = new Label[cs.Length];
            for (int i = 0; i < cs.Length; i++)
            {
                Label cap;
                Panel f = Field(names[i], cs[i], out cap);
                caps[i] = cap;
                tlp.Controls.Add(f, i % 4, i / 4);
            }
            filterCtrls = cs;

            btnAplicar = new UiButton("Aplicar filtros", 140, 40) { Style = BtnStyle.Primary, Location = new Point(0, 24) };
            btnAplicar.Click += (s, e) =>
            {
                AplicarFiltros();
                ShowToast("Filtros aplicados", "Las tablas, tarjetas y gráficos se actualizaron.", T.Ok);
            };
            btnLimpiar = new UiButton("Limpiar", 110, 40) { Location = new Point(150, 24) };
            btnLimpiar.Click += (s, e) => LimpiarFiltros();

            Panel btns = new Panel { Dock = DockStyle.Fill, BackColor = T.Surface, Margin = new Padding(0, 0, 0, 6) };
            btns.Controls.Add(btnAplicar);
            btns.Controls.Add(btnLimpiar);
            tlp.Controls.Add(btns, 3, 1);

            Panel head = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = T.Surface };
            Label title = Lbl("Filtros", 11.5f, T.Ink, true);
            title.Location = new Point(20, 12);
            lblHint = Lbl("", 8.5f, T.Muted);
            lblHint.AutoSize = false;
            lblHint.Dock = DockStyle.Right;
            lblHint.Width = 660;
            lblHint.TextAlign = ContentAlignment.MiddleRight;
            lblHint.Padding = new Padding(0, 0, 20, 0);
            head.Controls.Add(lblHint);
            head.Controls.Add(title);

            card.Controls.Add(tlp);
            card.Controls.Add(head);
            host.Controls.Add(card);

            ConfigurarFiltros();
            return host;
        }

        private void ConfigurarFiltros()
        {
            bool inv = seccion == Seccion.Inventario;
            bool mov = seccion == Seccion.Movimientos;
            bool pre = seccion == Seccion.Prestamos;

            dtDesde.Enabled = !inv;
            dtHasta.Enabled = !inv;
            selTrabajador.Enabled = mov || pre;
            selEstado.Enabled = !mov;
            selTipo.Enabled = mov;

            string[] est;
            switch (seccion)
            {
                case Seccion.Inventario: est = new[] { "Todos", "Disponible", "Prestado", "Mantenimiento", "Fuera de servicio" }; break;
                case Seccion.Prestamos: est = new[] { "Todos", "Pendiente", "Devuelto", "Vencido" }; break;
                case Seccion.Mantenimiento: est = new[] { "Todos", "Pendiente", "En reparación", "Reparado" }; break;
                default: est = new[] { "Todos" }; break;
            }
            int ix = Array.IndexOf(est, fEstado);
            selEstado.SetItems(est, ix < 0 ? 0 : ix);
            if (ix < 0) fEstado = "Todos";

            for (int i = 0; i < caps.Length; i++)
                caps[i].ForeColor = filterCtrls[i].Enabled ? T.Muted : Mix(T.Surface, T.Muted, .45f);

            switch (seccion)
            {
                case Seccion.Inventario: lblHint.Text = "El inventario refleja el estado actual: el rango de fechas y el trabajador no aplican."; break;
                case Seccion.Movimientos: lblHint.Text = "Consulta de solo lectura: este reporte no permite registrar movimientos."; break;
                case Seccion.Prestamos: lblHint.Text = "Los préstamos activos (pendientes o vencidos) se muestran siempre."; break;
                default: lblHint.Text = "Incluye las órdenes abiertas y las cerradas dentro del rango de fechas."; break;
            }
        }

        private void AplicarFiltros()
        {
            if (dtDesde.Value.Date > dtHasta.Value.Date)
            {
                DateTime tmp = dtDesde.Value;
                dtDesde.Value = dtHasta.Value;
                dtHasta.Value = tmp;
                ShowToast("Rango corregido", "La fecha inicial era posterior a la final; se intercambiaron.", T.Warn);
            }

            fDesde = dtDesde.Value.Date;
            fHasta = dtHasta.Value.Date;
            fCategoria = selCategoria.Current;
            fTexto = inpEquipo.Text.Trim();
            fTrabajador = selTrabajador.Current;
            fEstado = selEstado.Current;
            fTipo = selTipo.Current;

            periodoIdx = DetectarPeriodo();
            segPeriodo.SelectedIndex = periodoIdx;

            pagina = 1;
            Recalcular();
        }

        private void LimpiarFiltros()
        {
            RunSilent(() =>
            {
                dtDesde.Value = DateTime.Today.AddDays(-30);
                dtHasta.Value = DateTime.Today;
                selCategoria.SelectedIndex = 0;
                inpEquipo.Text = "";
                selTrabajador.SelectedIndex = 0;
                selEstado.SelectedIndex = 0;
                selTipo.SelectedIndex = 0;
                inpBusqueda.Text = "";
            });
            busquedaRapida = "";
            AplicarFiltros();
            ShowToast("Filtros restablecidos", "Se restauró el período de 30 días y se limpiaron los filtros.", T.Info);
        }

        // ---------------- Gráficos ----------------
        private Control BuildCharts()
        {
            Panel host = new Panel { Dock = DockStyle.Top, Height = 316, BackColor = T.Canvas, Padding = new Padding(24, 0, 24, 16) };
            TableLayoutPanel t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = T.Canvas, Margin = Padding.Empty };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26f));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            donut = new DonutChart();
            barras = new BarChart();
            hbar = new HBarChart();

            t.Controls.Add(ChartCard("Estado del inventario", "Distribución actual por estado", donut, new Padding(0, 0, 6, 0)), 0, 0);
            t.Controls.Add(ChartCard("Movimientos por período", "Salidas e ingresos de los últimos 6 meses", barras, new Padding(6, 0, 6, 0)), 1, 0);
            t.Controls.Add(ChartCard("Equipos en mantenimiento", "Órdenes por estado de reparación", hbar, new Padding(6, 0, 0, 0)), 2, 0);

            host.Controls.Add(t);
            return host;
        }

        private Card ChartCard(string title, string sub, Control chart, Padding margin)
        {
            Card card = new Card { Dock = DockStyle.Fill, Margin = margin };
            Label t1 = Lbl(title, 12f, T.Ink, true);
            t1.Location = new Point(20, 16);
            Label t2 = Lbl(sub, 8.5f, T.Muted);
            t2.Location = new Point(21, 42);
            chart.Location = new Point(20, 72);
            card.Controls.Add(t1);
            card.Controls.Add(t2);
            card.Controls.Add(chart);
            Action fit = () => chart.Size = new Size(Math.Max(40, card.Width - 40), Math.Max(40, card.Height - 72 - 16));
            card.Resize += (s, e) => fit();
            fit();
            return card;
        }

        // ---------------- Tabla ----------------
        private Control BuildTable()
        {
            tableHost = new Panel { Dock = DockStyle.Top, Height = 760, BackColor = T.Canvas, Padding = new Padding(24, 0, 24, 16) };
            Card card = new Card { Dock = DockStyle.Fill, Padding = new Padding(1, 14, 1, 14) };

            // --- cuerpo
            Panel gridHost = new Panel { Dock = DockStyle.Fill, BackColor = T.Surface, Padding = new Padding(14, 0, 14, 0) };
            emptyPanel = new EmptyState { Dock = DockStyle.Fill, Visible = false };
            grid = new FlatGrid { Dock = DockStyle.Top };
            ConfigurarGrid();
            gridHost.Controls.Add(emptyPanel);
            gridHost.Controls.Add(grid);

            // --- pie (paginación)
            footerPanel = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = T.Surface };
            Label lblMostrar = Lbl("Mostrar", 9f, T.Muted);
            lblMostrar.Location = new Point(24, 23);
            selPagina = new SelectBox { Size = new Size(84, 34), Location = new Point(80, 15) };
            selPagina.SetItems(new[] { "10", "25", "50", "Todos" }, pageSize <= 0 ? 3 : (pageSize == 10 ? 0 : (pageSize == 25 ? 1 : 2)));
            selPagina.Picked += (s, e) =>
            {
                pageSize = selPagina.Current == "Todos" ? 0 : int.Parse(selPagina.Current, CultureInfo.InvariantCulture);
                pagina = 1;
                RenderPagina();
            };
            Label lblReg = Lbl("registros", 9f, T.Muted);
            lblReg.Location = new Point(172, 23);
            lblInfo = Lbl("", 9f, T.Muted);
            lblInfo.Location = new Point(250, 23);
            pagerHost = new Panel { BackColor = T.Surface, Size = new Size(10, 34), Location = new Point(400, 15) };
            footerPanel.Controls.Add(lblMostrar);
            footerPanel.Controls.Add(selPagina);
            footerPanel.Controls.Add(lblReg);
            footerPanel.Controls.Add(lblInfo);
            footerPanel.Controls.Add(pagerHost);
            footerPanel.Resize += (s, e) => pagerHost.Left = Math.Max(0, footerPanel.Width - pagerHost.Width - 24);

            // --- barra superior
            Panel top = new Panel { Dock = DockStyle.Top, Height = 84, BackColor = T.Surface };
            lblTablaTitulo = Lbl("", 13f, T.Ink, true);
            lblTablaTitulo.Location = new Point(24, 16);
            lblTablaSub = Lbl("", 9f, T.Muted);
            lblTablaSub.Location = new Point(25, 46);

            btnPdf = new UiButton("PDF", 76, 40);
            btnPdf.Click += (s, e) => SimularExportacion("PDF");
            btnExcel = new UiButton("Excel", 84, 40);
            btnExcel.Click += (s, e) => SimularExportacion("Excel");
            btnPrint = new UiButton("Imprimir", 100, 40);
            btnPrint.Click += (s, e) => SimularExportacion("impresión");

            inpBusqueda = new InputBox("Búsqueda rápida…", true) { Size = new Size(250, 40) };
            inpBusqueda.Changed += (s, e) =>
            {
                if (building) return;
                busquedaRapida = inpBusqueda.Text;
                AplicarBusquedaRapida();
            };

            top.Controls.Add(lblTablaTitulo);
            top.Controls.Add(lblTablaSub);
            top.Controls.Add(btnPdf);
            top.Controls.Add(btnExcel);
            top.Controls.Add(btnPrint);
            top.Controls.Add(inpBusqueda);

            Action pos = () =>
            {
                inpBusqueda.Location = new Point(Math.Max(0, top.Width - 24 - inpBusqueda.Width), 22);
                btnPrint.Location = new Point(Math.Max(0, inpBusqueda.Left - 12 - btnPrint.Width), 22);
                btnExcel.Location = new Point(Math.Max(0, btnPrint.Left - 8 - btnExcel.Width), 22);
                btnPdf.Location = new Point(Math.Max(0, btnExcel.Left - 8 - btnPdf.Width), 22);
            };
            top.Resize += (s, e) => pos();
            pos();

            // Orden de acoplamiento: el control "Fill" primero (índice 0)
            card.Controls.Add(gridHost);
            card.Controls.Add(footerPanel);
            card.Controls.Add(top);
            tableHost.Controls.Add(card);
            return tableHost;
        }

        private void ConfigurarGrid()
        {
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.AllowUserToResizeColumns = false;
            grid.AllowUserToOrderColumns = false;
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = HeaderH;
            grid.RowTemplate.Height = RowH;
            grid.ScrollBars = ScrollBars.None;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.BackgroundColor = T.Surface;
            grid.GridColor = T.Border;
            grid.Height = HeaderH + 2;

            Color sel = Mix(T.Surface, T.Accent, .13f);

            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = T.SurfaceAlt,
                ForeColor = T.Muted,
                Font = F(8f, true),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0),
                SelectionBackColor = T.SurfaceAlt,
                SelectionForeColor = T.Muted
            };
            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = T.Surface,
                ForeColor = T.Ink,
                Font = F(9.5f),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 6, 0),
                SelectionBackColor = sel,
                SelectionForeColor = T.Ink
            };
            grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Mix(T.Surface, T.Ink, .028f)
            };

            grid.CellPainting += Grid_CellPainting;
            grid.CellMouseEnter += Grid_CellMouseEnter;
            grid.MouseLeave += (s, e) => QuitarHover();
        }

        private void Grid_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == hoverRow) return;
            QuitarHover();
            if (e.RowIndex >= 0 && e.RowIndex < grid.Rows.Count)
            {
                grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = Mix(T.Surface, T.Accent, .07f);
                hoverRow = e.RowIndex;
            }
        }

        private void QuitarHover()
        {
            if (grid != null && hoverRow >= 0 && hoverRow < grid.Rows.Count)
                grid.Rows[hoverRow].DefaultCellStyle.BackColor = Color.Empty;
            hoverRow = -1;
        }

        private void Grid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string kind = grid.Columns[e.ColumnIndex].Tag as string;
            if (kind != "badge" && kind != "ev") return;

            string text = Convert.ToString(e.Value) ?? "";
            e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border | DataGridViewPaintParts.SelectionBackground);

            Graphics g = e.Graphics;
            Rectangle cell = e.CellBounds;
            bool selected = (e.State & DataGridViewElementStates.Selected) != 0;
            Color rowBack = selected ? e.CellStyle.SelectionBackColor : e.CellStyle.BackColor;
            Rectangle textRect = new Rectangle(cell.X + 14, cell.Y, Math.Max(10, cell.Width - 18), cell.Height);

            Color c;
            if (kind == "badge" && TryKind(text, out c))
            {
                Font f = F(8.5f, true);
                Size ts = TextRenderer.MeasureText(text, f, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                int h = 26;
                Rectangle pill = new Rectangle(cell.X + 14, cell.Y + (cell.Height - h) / 2, Math.Min(ts.Width + 36, Math.Max(40, cell.Width - 20)), h);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = Round(pill, 13))
                using (SolidBrush br = new SolidBrush(Mix(rowBack, c, .16f)))
                    g.FillPath(br, path);
                using (SolidBrush dot = new SolidBrush(c))
                    g.FillEllipse(dot, pill.X + 11, pill.Y + h / 2 - 3, 6, 6);
                Txt(g, text, f, new Rectangle(pill.X + 22, pill.Y, Math.Max(4, pill.Width - 26), h), c, TL);
            }
            else if (kind == "ev" && text != "Sin evidencia")
            {
                Txt(g, "▣  " + text, F(9f, true), textRect, T.Info, TL);
            }
            else
            {
                Txt(g, text, F(9.5f), textRect, T.Muted, TL);
            }
            e.Handled = true;
        }

        private Control BuildFootnote()
        {
            Panel host = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = T.Canvas, Padding = new Padding(26, 0, 24, 8) };
            lblFoot = Lbl("", 8.5f, T.Muted);
            lblFoot.Location = new Point(26, 2);
            host.Controls.Add(lblFoot);
            return host;
        }

        // ============================================================
        //  LÓGICA DE DATOS (solo datos ficticios)
        // ============================================================
        private IEnumerable<Equipo> EquiposFiltrados()
        {
            return Demo.Equipos.Where(e =>
                (fCategoria == "Todas" || e.Categoria == fCategoria) &&
                (fTexto.Length == 0 ||
                 e.Codigo.IndexOf(fTexto, StringComparison.OrdinalIgnoreCase) >= 0 ||
                 e.Nombre.IndexOf(fTexto, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private bool EnRango(DateTime d)
        {
            return d.Date >= fDesde.Date && d.Date <= fHasta.Date;
        }

        private static string TituloSeccion(Seccion s)
        {
            switch (s)
            {
                case Seccion.Inventario: return "Reporte de inventario";
                case Seccion.Movimientos: return "Reporte de movimientos";
                case Seccion.Prestamos: return "Préstamos y devoluciones";
                default: return "Reporte de mantenimiento";
            }
        }

        private static string SubtituloSeccion(Seccion s)
        {
            switch (s)
            {
                case Seccion.Inventario: return "Estado operativo y condición física de cada equipo";
                case Seccion.Movimientos: return "Historial de entradas, salidas, traslados y devoluciones · solo consulta";
                case Seccion.Prestamos: return "Seguimiento de equipos prestados y su devolución";
                default: return "Averías, reparaciones y evidencias fotográficas";
            }
        }

        private static Col[] ColsDe(Seccion s)
        {
            switch (s)
            {
                case Seccion.Inventario:
                    return new[]
                    {
                        new Col("CÓDIGO", 12, "m"), new Col("EQUIPO", 24, "b"), new Col("CATEGORÍA", 13),
                        new Col("UBICACIÓN", 16), new Col("DISPONIBLE", 10), new Col("ESTADO", 15, "badge"), new Col("CONDICIÓN", 13, "badge")
                    };
                case Seccion.Movimientos:
                    return new[]
                    {
                        new Col("CÓDIGO", 11, "m"), new Col("EQUIPO", 20, "b"), new Col("TIPO", 12, "badge"),
                        new Col("FECHA Y HORA", 15), new Col("RESPONSABLE", 15), new Col("DESTINO", 14), new Col("OBSERVACIONES", 20)
                    };
                case Seccion.Prestamos:
                    return new[]
                    {
                        new Col("CÓDIGO", 10, "m"), new Col("EQUIPO", 21, "b"), new Col("RESPONSABLE", 14),
                        new Col("SALIDA", 11), new Col("PREVISTA", 11), new Col("DEVOLUCIÓN", 12),
                        new Col("ESTADO", 12, "badge"), new Col("CONDICIÓN AL DEVOLVER", 15, "badge")
                    };
                default:
                    return new[]
                    {
                        new Col("EQUIPO", 17, "b"), new Col("PROBLEMA", 14), new Col("INGRESO", 10),
                        new Col("REPARACIÓN", 19), new Col("RESPONSABLE", 13), new Col("ESTADO", 13, "badge"),
                        new Col("FINALIZACIÓN", 10), new Col("EVIDENCIA", 11, "ev")
                    };
            }
        }

        private List<string[]> BuildRows(Seccion s)
        {
            HashSet<Equipo> baseSet = new HashSet<Equipo>(EquiposFiltrados());
            string estado = s == seccion ? fEstado : "Todos";
            List<string[]> rows = new List<string[]>();

            switch (s)
            {
                case Seccion.Inventario:
                    foreach (Equipo e in Demo.Equipos)
                    {
                        if (!baseSet.Contains(e)) continue;
                        if (estado != "Todos" && e.Estado != estado) continue;
                        rows.Add(new[] { e.Codigo, e.Nombre, e.Categoria, e.Ubicacion, e.Cantidad.ToString(), e.Estado, e.Condicion });
                    }
                    break;

                case Seccion.Movimientos:
                    foreach (Movimiento m in Demo.Movimientos)
                    {
                        if (!baseSet.Contains(m.Equipo) || !EnRango(m.Fecha)) continue;
                        if (fTrabajador != "Todos" && m.Trabajador != fTrabajador) continue;
                        if (fTipo != "Todos" && m.Tipo != fTipo) continue;
                        rows.Add(new[] { m.Equipo.Codigo, m.Equipo.Nombre, m.Tipo, FechaHora(m.Fecha), m.Trabajador, m.Destino, m.Observaciones });
                    }
                    break;

                case Seccion.Prestamos:
                    foreach (Prestamo p in Demo.Prestamos)
                    {
                        if (!baseSet.Contains(p.Equipo)) continue;
                        if (p.Devolucion.HasValue && !EnRango(p.Salida)) continue;
                        if (fTrabajador != "Todos" && p.Trabajador != fTrabajador) continue;
                        if (estado != "Todos" && p.Estado != estado) continue;
                        rows.Add(new[]
                        {
                            p.Equipo.Codigo, p.Equipo.Nombre, p.Trabajador, Fecha(p.Salida), Fecha(p.Prevista),
                            p.Devolucion.HasValue ? Fecha(p.Devolucion.Value) : "—", p.Estado, p.CondicionDevolucion
                        });
                    }
                    break;

                default:
                    foreach (Mant m in Demo.Mantenimientos)
                    {
                        if (!baseSet.Contains(m.Equipo)) continue;
                        if (m.Estado == "Reparado" && !EnRango(m.Ingreso)) continue;
                        if (estado != "Todos" && m.Estado != estado) continue;
                        rows.Add(new[]
                        {
                            m.Equipo.Nombre, m.Problema, Fecha(m.Ingreso), m.Descripcion, m.Responsable, m.Estado,
                            m.Fin.HasValue ? Fecha(m.Fin.Value) : "—",
                            m.Fotos > 0 ? (m.Fotos == 1 ? "1 foto" : m.Fotos + " fotos") : "Sin evidencia"
                        });
                    }
                    break;
            }
            return rows;
        }

        // ============================================================
        //  ACTUALIZACIÓN DE PANTALLA
        // ============================================================
        private void Recalcular()
        {
            if (building || grid == null) return;

            Dictionary<Seccion, List<string[]>> porSeccion = new Dictionary<Seccion, List<string[]>>();
            foreach (Seccion s in Enum.GetValues(typeof(Seccion)))
                porSeccion[s] = BuildRows(s);

            for (int i = 0; i < tabCards.Length; i++)
            {
                tabCards[i].Count = porSeccion[(Seccion)i].Count;
                tabCards[i].Selected = i == (int)seccion;
                tabCards[i].Invalidate();
            }

            baseRows = porSeccion[seccion];

            ActualizarKpis();
            ActualizarGraficos();

            lblRango.Text = seccion == Seccion.Inventario
                ? "Inventario al " + Fecha(DateTime.Today)
                : "Período:  " + Fecha(fDesde) + "  —  " + Fecha(fHasta);
            lblTablaTitulo.Text = TituloSeccion(seccion);
            lblTablaSub.Text = SubtituloSeccion(seccion);
            lblFoot.Text = "Datos de demostración · Actualizado a las " + DateTime.Now.ToString("HH:mm:ss") + " · La conexión con Supabase se implementará posteriormente.";

            AplicarBusquedaRapida();
        }

        private void ActualizarKpis()
        {
            HashSet<Equipo> set = new HashSet<Equipo>(EquiposFiltrados());
            int total = set.Count;
            int disp = set.Count(e => e.Estado == "Disponible");
            int pres = set.Count(e => e.Estado == "Prestado");
            int mant = set.Count(e => e.Estado == "Mantenimiento");
            List<Prestamo> activos = Demo.Prestamos.Where(p => set.Contains(p.Equipo) && p.Estado != "Devuelto").ToList();
            int venc = activos.Count(p => p.Estado == "Vencido");
            int abiertas = Demo.Mantenimientos.Count(m => set.Contains(m.Equipo) && m.Estado != "Reparado");
            int cats = set.Select(e => e.Categoria).Distinct().Count();

            Func<int, string> pct = n => (total == 0 ? 0 : n * 100.0 / total).ToString("0.#", CultureInfo.InvariantCulture) + "% del inventario";

            kpiValue[0].Text = total.ToString();
            kpiSub[0].Text = cats + (cats == 1 ? " categoría" : " categorías");
            kpiValue[1].Text = disp.ToString();
            kpiSub[1].Text = pct(disp);
            kpiValue[2].Text = pres.ToString();
            kpiSub[2].Text = pct(pres);
            kpiValue[3].Text = mant.ToString();
            kpiSub[3].Text = abiertas + (abiertas == 1 ? " orden abierta" : " órdenes abiertas");
            kpiValue[4].Text = activos.Count.ToString();
            kpiSub[4].Text = venc + (venc == 1 ? " vencida" : " vencidas");
        }

        private void ActualizarGraficos()
        {
            HashSet<Equipo> set = new HashSet<Equipo>(EquiposFiltrados());

            // Dona: estado del inventario
            string[] estados = { "Disponible", "Prestado", "Mantenimiento", "Fuera de servicio" };
            int[] vals = estados.Select(es => set.Count(e => e.Estado == es)).ToArray();
            donut.SetData(estados, vals, new[] { T.Ok, T.Info, T.Warn, T.Bad });

            // Barras: movimientos de los últimos 6 meses
            DateTime ini = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            string[] lbl = new string[6];
            int[] sal = new int[6];
            int[] ing = new int[6];
            for (int i = 0; i < 6; i++)
            {
                DateTime mes = ini.AddMonths(-(5 - i));
                string nombre = mes.ToString("MMM", ES).TrimEnd('.');
                lbl[i] = ES.TextInfo.ToTitleCase(nombre);
                foreach (Movimiento m in Demo.Movimientos)
                {
                    if (!set.Contains(m.Equipo)) continue;
                    if (m.Fecha.Year != mes.Year || m.Fecha.Month != mes.Month) continue;
                    if (m.Tipo == "Salida") sal[i]++;
                    else if (m.Tipo == "Entrada" || m.Tipo == "Devolución") ing[i]++;
                }
            }
            barras.SetData(lbl, sal, ing);

            // Barras horizontales: órdenes de mantenimiento por estado
            string[] ms = { "Pendiente", "En reparación", "Reparado" };
            int[] mv = ms.Select(es => Demo.Mantenimientos.Count(m => set.Contains(m.Equipo) && m.Estado == es)).ToArray();
            hbar.SetData(ms, mv, new[] { T.Warn, T.Info, T.Ok });
        }

        private void AplicarBusquedaRapida()
        {
            string q = (busquedaRapida ?? "").Trim();
            if (q.Length == 0) viewRows = baseRows;
            else viewRows = baseRows.Where(r => r.Any(c => c.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            pagina = 1;
            RenderPagina();
        }

        private void EnsureColumns()
        {
            if (columnsFor == (int)seccion && grid.Columns.Count > 0) return;
            grid.Rows.Clear();
            grid.Columns.Clear();
            foreach (Col c in ColsDe(seccion))
            {
                DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
                col.HeaderText = c.H;
                col.FillWeight = c.W;
                col.MinimumWidth = 70;
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
                col.Tag = c.K;
                if (c.K == "b") col.DefaultCellStyle.Font = F(9.5f, true);
                if (c.K == "m") col.DefaultCellStyle.ForeColor = T.Muted;
                grid.Columns.Add(col);
            }
            columnsFor = (int)seccion;
        }

        private void RenderPagina()
        {
            if (grid == null) return;

            int total = viewRows.Count;
            int size = pageSize <= 0 ? Math.Max(total, 1) : pageSize;
            int pages = Math.Max(1, (int)Math.Ceiling(total / (double)size));
            if (pagina > pages) pagina = pages;
            if (pagina < 1) pagina = 1;
            int start = (pagina - 1) * size;
            int take = Math.Max(0, Math.Min(size, total - start));

            QuitarHover();
            EnsureColumns();
            grid.SuspendLayout();
            grid.Rows.Clear();
            for (int i = 0; i < take; i++)
                grid.Rows.Add((object[])viewRows[start + i]);
            grid.ClearSelection();
            grid.CurrentCell = null;
            grid.Height = HeaderH + take * RowH + 2;
            grid.ResumeLayout();

            emptyPanel.Visible = total == 0;
            int body = HeaderH + take * RowH + 2 + (total == 0 ? 200 : 0);
            tableHost.Height = 192 + body;

            lblInfo.Text = total == 0
                ? "Sin registros para mostrar"
                : "Mostrando " + (start + 1) + "–" + (start + take) + " de " + total + " registros";

            BuildPager(pages);
        }

        private void BuildPager(int pages)
        {
            pagerHost.SuspendLayout();
            foreach (Control c in pagerHost.Controls.Cast<Control>().ToList())
            {
                pagerHost.Controls.Remove(c);
                c.Dispose();
            }

            int x = 0;
            Action<string, int, bool, bool, int> add = (text, w, active, enabled, target) =>
            {
                UiButton b = new UiButton(text, w, 34) { Style = BtnStyle.Ghost, Active = active, Enabled = enabled, Location = new Point(x, 0) };
                if (target > 0) b.Click += (s, e) => { pagina = target; RenderPagina(); };
                pagerHost.Controls.Add(b);
                x += w + 6;
            };

            add("‹", 36, false, pagina > 1, pagina - 1);

            int last = 0;
            for (int i = 1; i <= pages; i++)
            {
                bool mostrar = i == 1 || i == pages || Math.Abs(i - pagina) <= 1;
                if (!mostrar) continue;
                if (last != 0 && i - last > 1) add("…", 30, false, false, 0);
                add(i.ToString(), 36, i == pagina, true, i);
                last = i;
            }

            add("›", 36, false, pagina < pages, pagina + 1);

            pagerHost.Width = Math.Max(1, x - 6);
            pagerHost.Left = Math.Max(0, footerPanel.Width - pagerHost.Width - 24);
            pagerHost.ResumeLayout();
        }

        // ============================================================
        //  NOTIFICACIONES Y EXPORTACIÓN SIMULADA
        // ============================================================
        private void ShowToast(string title, string msg, Color accent)
        {
            if (toast != null) toast.Display(title, msg, accent);
        }

        private void SimularExportacion(string tipo)
        {
            string titulo = tipo == "impresión" ? "Imprimir reporte" : "Exportar a " + tipo;
            ShowToast(titulo, "Simulación del «" + TituloSeccion(seccion) + "». Esta función se implementará posteriormente.", T.Info);
        }
    }
}