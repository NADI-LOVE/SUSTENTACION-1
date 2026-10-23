using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

// ======================================================================
//  PANEL "EVENTOS" - ARTEMUSA INVENTARIO AUDIOVISUAL
//  SOLO DISEÑO: no usa base de datos. Trabaja con datos de muestra.
//
//  Para conectar datos reales más adelante basta con llamar:
//      panel.CargarEventos(listaDeEventoItem);
//  y suscribirse a los eventos públicos (NuevoEventoSolicitado, etc.).
//
//  Iconos: usa la fuente "Segoe MDL2 Assets" (incluida en Windows 10/11).
// ======================================================================
namespace SUSTENTACION.PanelEventos
{
    // ------------------------------------------------------------------
    //  MODELO
    // ------------------------------------------------------------------
    public enum EstadoEvento { Proximo = 0, EnCurso = 1, Realizado = 2, Cancelado = 3 }

    public class EventoItem
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Ubicacion { get; set; }
        public string Tipo { get; set; }
        public string Prioridad { get; set; }      // Alta | Media | Baja
        public string Responsable { get; set; }
        public DateTime Inicio { get; set; }
        public DateTime Fin { get; set; }
        public EstadoEvento Estado { get; set; }
        public double? Latitud { get; set; }
        public double? Longitud { get; set; }
    }

    // ------------------------------------------------------------------
    //  TEMA (colores, fuentes, iconos)
    // ------------------------------------------------------------------
    internal static class Tema
    {
        public static readonly Color Fondo = Color.FromArgb(244, 247, 251);
        public static readonly Color Borde = Color.FromArgb(226, 232, 240);
        public static readonly Color Texto = Color.FromArgb(15, 23, 42);
        public static readonly Color Tenue = Color.FromArgb(100, 116, 139);
        public static readonly Color Primario = Color.FromArgb(37, 99, 235);
        public static readonly Color PrimarioHover = Color.FromArgb(29, 78, 216);
        public static readonly Color Azul = Color.FromArgb(59, 130, 246);
        public static readonly Color Verde = Color.FromArgb(34, 197, 94);
        public static readonly Color Morado = Color.FromArgb(139, 92, 246);
        public static readonly Color Rojo = Color.FromArgb(239, 68, 68);
        public static readonly Color Naranja = Color.FromArgb(249, 115, 22);
        public static readonly Color Ambar = Color.FromArgb(245, 158, 11);
        public static readonly Color Gris = Color.FromArgb(148, 163, 184);

        public static readonly Font F8 = new Font("Segoe UI", 8F);
        public static readonly Font F8B = new Font("Segoe UI", 8F, FontStyle.Bold);
        public static readonly Font F9 = new Font("Segoe UI", 9F);
        public static readonly Font F9B = new Font("Segoe UI", 9F, FontStyle.Bold);
        public static readonly Font F9I = new Font("Segoe UI", 9F, FontStyle.Italic);
        public static readonly Font F11B = new Font("Segoe UI", 11F, FontStyle.Bold);
        public static readonly Font F12B = new Font("Segoe UI", 12F, FontStyle.Bold);
        public static readonly Font F20B = new Font("Segoe UI", 20F, FontStyle.Bold);
        public static readonly Font F24B = new Font("Segoe UI", 24F, FontStyle.Bold);

        public static readonly Font Ico9 = new Font("Segoe MDL2 Assets", 9F);
        public static readonly Font Ico10 = new Font("Segoe MDL2 Assets", 10F);
        public static readonly Font Ico11 = new Font("Segoe MDL2 Assets", 11F);
        public static readonly Font Ico18 = new Font("Segoe MDL2 Assets", 18F);

        public const string GCalendario = "\uE787";
        public const string GReloj = "\uE121";
        public const string GCheck = "\uE73E";
        public const string GCancelar = "\uE711";
        public const string GPin = "\uE81D";
        public const string GOjo = "\uE7B3";
        public const string GEditar = "\uE70F";
        public const string GMas = "\uE712";
        public const string GRefrescar = "\uE72C";
        public const string GAgregar = "\uE710";
        public const string GDescargar = "\uE896";
        public const string GEliminar = "\uE74D";

        public static Color ColorEstado(EstadoEvento e)
        {
            switch (e)
            {
                case EstadoEvento.Proximo: return Azul;
                case EstadoEvento.EnCurso: return Verde;
                case EstadoEvento.Realizado: return Morado;
                default: return Rojo;
            }
        }

        public static string TextoEstado(EstadoEvento e)
        {
            switch (e)
            {
                case EstadoEvento.Proximo: return "Próximo";
                case EstadoEvento.EnCurso: return "En curso";
                case EstadoEvento.Realizado: return "Realizado";
                default: return "Cancelado";
            }
        }

        public static Color ColorPrioridad(string p)
        {
            switch (p)
            {
                case "Alta": return Rojo;
                case "Media": return Ambar;
                default: return Gris;
            }
        }

        public static Color ColorTipo(string t)
        {
            switch (t)
            {
                case "Conferencia": return Azul;
                case "Taller": return Color.FromArgb(20, 184, 166);
                case "Producción": return Morado;
                case "Reunión": return Naranja;
                case "Cobertura": return Color.FromArgb(99, 102, 241);
                case "Corporativo": return Color.FromArgb(236, 72, 153);
                default: return Gris;
            }
        }

        public static Color Suave(Color c, float k = 0.15f)
        {
            return Color.FromArgb((int)(255 - (255 - c.R) * k), (int)(255 - (255 - c.G) * k), (int)(255 - (255 - c.B) * k));
        }

        public static Color Oscuro(Color c)
        {
            return Color.FromArgb((int)(c.R * 0.78), (int)(c.G * 0.78), (int)(c.B * 0.78));
        }
    }

    // ------------------------------------------------------------------
    //  UTILIDADES DE DIBUJO
    // ------------------------------------------------------------------
    internal static class Gfx
    {
        public static GraphicsPath RoundRect(RectangleF r, float rad)
        {
            var p = new GraphicsPath();
            float d = Math.Min(rad * 2, Math.Min(r.Width, r.Height));
            if (d <= 0) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void Rellenar(Graphics g, RectangleF r, float rad, Color c)
        {
            using (var p = RoundRect(r, rad))
            using (var b = new SolidBrush(c))
                g.FillPath(b, p);
        }

        public static void Borde(Graphics g, RectangleF r, float rad, Color c, float w = 1f)
        {
            using (var p = RoundRect(r, rad))
            using (var pen = new Pen(c, w))
                g.DrawPath(pen, p);
        }

        public static void Texto(Graphics g, string t, Font f, Color c, RectangleF r,
            StringAlignment h = StringAlignment.Near, StringAlignment v = StringAlignment.Near, bool ajustar = false)
        {
            using (var sf = new StringFormat { Alignment = h, LineAlignment = v, Trimming = StringTrimming.EllipsisCharacter })
            using (var b = new SolidBrush(c))
            {
                if (!ajustar) sf.FormatFlags = StringFormatFlags.NoWrap;
                g.DrawString(t ?? "", f, b, r, sf);
            }
        }

        public static void Icono(Graphics g, string glyph, Font f, Color c, RectangleF r)
        {
            Texto(g, glyph, f, c, r, StringAlignment.Center, StringAlignment.Center);
        }

        public static float Ancho(Graphics g, string t, Font f)
        {
            return g.MeasureString(t ?? "", f, 2000, StringFormat.GenericTypographic).Width;
        }

        public static float AnchoPill(Graphics g, string t)
        {
            return Ancho(g, t, Tema.F8B) + 20;
        }

        public static float Pill(Graphics g, string t, Color c, float x, float y, float h = 22)
        {
            float w = AnchoPill(g, t);
            var r = new RectangleF(x, y, w, h);
            Rellenar(g, r, h / 2, Tema.Suave(c));
            Texto(g, t, Tema.F8B, Tema.Oscuro(c), r, StringAlignment.Center, StringAlignment.Center);
            return w;
        }

        public static void BarraSup(Graphics g, RectangleF r, Color c, float rad = 4)
        {
            if (r.Height <= 0 || r.Width <= 0) return;
            rad = Math.Min(rad, Math.Min(r.Width / 2, r.Height));
            using (var p = new GraphicsPath())
            using (var b = new SolidBrush(c))
            {
                p.AddArc(r.X, r.Y, rad * 2, rad * 2, 180, 90);
                p.AddArc(r.Right - rad * 2, r.Y, rad * 2, rad * 2, 270, 90);
                p.AddLine(r.Right, r.Bottom, r.X, r.Bottom);
                p.CloseFigure();
                g.FillPath(b, p);
            }
        }

        // (x, y) = punta inferior del marcador
        public static void Pin(Graphics g, float x, float y, Color c, float s)
        {
            float r = 10f * s, cy = y - 2.1f * r;
            using (var path = new GraphicsPath())
            {
                path.AddArc(x - r, cy - r, 2 * r, 2 * r, 151.6f, 236.8f);
                path.AddLine(path.GetLastPoint(), new PointF(x, y));
                path.CloseFigure();

                using (var sh = new SolidBrush(Color.FromArgb(45, 0, 0, 0)))
                    g.FillEllipse(sh, x - r * 0.6f, y - 2f, r * 1.2f, 4f);
                using (var b = new SolidBrush(c)) g.FillPath(b, path);
                using (var pen = new Pen(Color.White, 1.5f)) g.DrawPath(pen, path);
                using (var b = new SolidBrush(Color.White))
                    g.FillEllipse(b, x - r * 0.38f, cy - r * 0.38f, r * 0.76f, r * 0.76f);
            }
        }
    }

    // ------------------------------------------------------------------
    //  CONTROLES PERSONALIZADOS
    // ------------------------------------------------------------------
    internal class Card : Panel
    {
        public int Radius = 12;
        public Action<Graphics, Rectangle> Dibujar;

        public Card()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Tema.Fondo;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            var r = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            Gfx.Rellenar(g, r, Radius, Color.White);
            Gfx.Borde(g, r, Radius, Tema.Borde);
            if (Dibujar != null) Dibujar(g, ClientRectangle);
        }
    }

    internal class RBtn : Control
    {
        public string Glyph = "";
        public string Caption = "";
        public Color Relleno = Color.White;
        public Color RellenoHover = Color.FromArgb(241, 245, 249);
        public Color Linea = Tema.Borde;
        public Color Fore = Tema.Texto;
        public int Radio = 8;
        private bool _over;

        public RBtn()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            Font = Tema.F9B;
            Size = new Size(110, 34);
        }

        protected override void OnMouseEnter(EventArgs e) { _over = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _over = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(Parent != null ? Parent.BackColor : SystemColors.Control);

            var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            Gfx.Rellenar(g, r, Radio, _over ? RellenoHover : Relleno);
            if (Linea.A > 0) Gfx.Borde(g, r, Radio, Linea);

            float gw = Glyph.Length > 0 ? 22 : 0;
            float tw = Gfx.Ancho(g, Caption, Font);
            float x = (Width - (gw + tw)) / 2f;
            if (Glyph.Length > 0)
                Gfx.Icono(g, Glyph, Tema.Ico10, Fore, new RectangleF(x, 0, 20, Height));
            Gfx.Texto(g, Caption, Font, Fore, new RectangleF(x + gw, 0, tw + 12, Height),
                StringAlignment.Near, StringAlignment.Center);
        }
    }

    // ------------------------------------------------------------------
    //  PANEL PRINCIPAL
    // ------------------------------------------------------------------
    public class PanelEventosControl : UserControl
    {
        // ---------- API pública ----------
        public event Action NuevoEventoSolicitado;
        public event Action ActualizarSolicitado;
        public event Action ExportarSolicitado;
        public event Action VerTodosProximosSolicitado;
        public event Action<EventoItem> VerDetallesSolicitado;
        public event Action<EventoItem> EditarSolicitado;
        public event Action<EventoItem> CancelarSolicitado;
        public event Action<EventoItem> EliminarSolicitado;

        // ---------- datos ----------
        private readonly List<EventoItem> todos = new List<EventoItem>();
        private List<EventoItem> filtrados = new List<EventoItem>();
        private EventoItem seleccionado;
        private int pagina, pageSize = 10, mesesRango = 6;
        private string colOrden = "Fecha";
        private bool ordenAsc = false;
        private bool llenando, inicializando;

        // ---------- controles ----------
        private Panel panelScroll, header;
        private TableLayoutPanel root, kpiTabla;
        private Label lblPunto, lblConexion;
        private RBtn btnActualizarTop, btnNuevo;
        private readonly List<Card> kpis = new List<Card>();
        private Card cardMapa, cardMensual, cardDona, cardTipos, cardProximos, cardTabla;
        private TextBox txtMapaBuscar, txtBuscar;
        private ComboBox cmbPeriodo, cmbEstado, cmbPrioridad, cmbUbicacion, cmbMostrar;
        private DateTimePicker dtDesde, dtHasta;
        private DataGridView gridEventos;
        private Panel pager, barraFiltros, barraAcciones;
        private Label lblResultados;
        private RBtn btnActualizarTabla;
        private ContextMenuStrip ctxMas;
        private EventoItem eventoMenu;

        // ---------- mapa ----------
        private const double CLat = -15.8402, CLng = -70.0219, EscalaBase = 4500;
        private float zoom = 1f, panX, panY;
        private bool arrastrando, movido;
        private Point dragIni;
        private PointF panIni;
        private Rectangle rZoomIn, rZoomOut, rPopupCerrar, rPopupDetalles;
        private readonly List<PinInfo> pins = new List<PinInfo>();
        private List<PinInfo> pinsOrdenados = new List<PinInfo>();
        private List<Tuple<PointF[], bool>> calles;

        private class PinInfo { public EventoItem Ev; public PointF Tip; public float Escala; }

        // ---------- próximos ----------
        private Rectangle rVerTodos;
        private readonly List<Tuple<Rectangle, EventoItem>> itemsProx = new List<Tuple<Rectangle, EventoItem>>();

        // ---------- tabla ----------
        private readonly List<Tuple<Rectangle, int>> pagerRects = new List<Tuple<Rectangle, int>>();

        private static readonly Dictionary<string, string> Encabezados = new Dictionary<string, string>
        {
            { "Fecha", "Fecha" }, { "Nombre", "Nombre del evento" }, { "Ubicacion", "Ubicación" },
            { "Tipo", "Tipo" }, { "Estado", "Estado" }, { "Prioridad", "Prioridad" },
            { "Responsable", "Responsable" }, { "Acciones", "Acciones" }
        };

        private static readonly HashSet<string> Ordenables = new HashSet<string>
            { "Fecha", "Nombre", "Ubicacion", "Estado", "Prioridad", "Responsable" };

        private static readonly Dictionary<string, Func<EventoItem, IComparable>> Selectores =
            new Dictionary<string, Func<EventoItem, IComparable>>
        {
            { "Fecha", e => e.Inicio },
            { "Nombre", e => e.Nombre },
            { "Ubicacion", e => e.Ubicacion },
            { "Estado", e => (int)e.Estado },
            { "Prioridad", e => e.Prioridad == "Alta" ? 0 : e.Prioridad == "Media" ? 1 : 2 },
            { "Responsable", e => e.Responsable }
        };

        // ============================================================
        //  CONSTRUCTOR
        // ============================================================
        public PanelEventosControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Tema.Fondo;
            Font = Tema.F9;
            DoubleBuffered = true;

            ConstruirInterfaz();
            CargarEventos(GenerarDatosDemo());
        }

        // ============================================================
        //  API
        // ============================================================
        public void CargarEventos(IEnumerable<EventoItem> eventos)
        {
            todos.Clear();
            if (eventos != null) todos.AddRange(eventos);
            seleccionado = null;

            inicializando = true;
            string previa = cmbUbicacion.SelectedItem as string;
            cmbUbicacion.Items.Clear();
            cmbUbicacion.Items.Add("Todas");
            foreach (var u in todos.Select(e => e.Ubicacion).Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().OrderBy(u => u))
                cmbUbicacion.Items.Add(u);
            int idx = previa != null ? cmbUbicacion.Items.IndexOf(previa) : 0;
            cmbUbicacion.SelectedIndex = idx >= 0 ? idx : 0;
            inicializando = false;

            pagina = 0;
            Refrescar();
        }

        // ============================================================
        //  CONSTRUCCIÓN DE LA INTERFAZ
        // ============================================================
        private void ConstruirInterfaz()
        {
            panelScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Tema.Fondo };
            Controls.Add(panelScroll);

            root = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Tema.Fondo,
                Padding = new Padding(24, 18, 24, 24)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));   // encabezado
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 118F));  // KPIs
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 454F));  // mapa + gráficos
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 600F));  // tabla (se ajusta)
            panelScroll.Controls.Add(root);

            root.Controls.Add(ConstruirEncabezado(), 0, 0);
            root.Controls.Add(ConstruirKpis(), 0, 1);
            root.Controls.Add(ConstruirMedio(), 0, 2);
            root.Controls.Add(ConstruirTabla(), 0, 3);

            AjustarAlturas();
        }

        private void AjustarAlturas()
        {
            int tablaH = 252 + pageSize * 38;
            root.RowStyles[3].Height = tablaH;
            root.Height = root.Padding.Vertical + 72 + 118 + 454 + tablaH;
            root.MinimumSize = new Size(1100, root.Height);
        }

        // ------------------------------------------------------------
        //  ENCABEZADO
        // ------------------------------------------------------------
        private Control ConstruirEncabezado()
        {
            header = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Fondo };

            header.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                var ic = new RectangleF(0, 8, 50, 50);
                Gfx.Rellenar(g, ic, 12, Tema.Primario);
                Gfx.Icono(g, Tema.GCalendario, Tema.Ico18, Color.White, ic);
            };

            var lblTitulo = new Label
            {
                Text = "Eventos",
                Font = Tema.F20B,
                ForeColor = Tema.Texto,
                BackColor = Tema.Fondo,
                AutoSize = true,
                Location = new Point(62, 2)
            };
            var lblSub = new Label
            {
                Text = "Gestiona y visualiza los eventos de Artemusa, con ubicación, estado y detalles.",
                Font = Tema.F9,
                ForeColor = Tema.Tenue,
                BackColor = Tema.Fondo,
                AutoSize = true,
                Location = new Point(64, 42)
            };

            lblPunto = new Label { Text = "●", Font = Tema.F9, ForeColor = Tema.Verde, BackColor = Tema.Fondo, AutoSize = true };
            lblConexion = new Label { Text = "Vista de diseño · datos de muestra", Font = Tema.F8, ForeColor = Tema.Tenue, BackColor = Tema.Fondo, AutoSize = true };

            btnActualizarTop = new RBtn { Glyph = Tema.GRefrescar, Caption = "Actualizar", Size = new Size(116, 36), BackColor = Tema.Fondo };
            btnActualizarTop.Click += (s, e) => { ActualizarSolicitado?.Invoke(); Refrescar(); };

            btnNuevo = new RBtn
            {
                Glyph = Tema.GAgregar,
                Caption = "Nuevo evento",
                Size = new Size(140, 36),
                BackColor = Tema.Fondo,
                Relleno = Tema.Primario,
                RellenoHover = Tema.PrimarioHover,
                Linea = Color.Empty,
                Fore = Color.White
            };
            btnNuevo.Click += (s, e) => NuevoEventoSolicitado?.Invoke();

            header.Controls.AddRange(new Control[] { lblTitulo, lblSub, lblPunto, lblConexion, btnActualizarTop, btnNuevo });

            header.Resize += (s, e) => PosicionarEncabezado();
            return header;
        }

        private void PosicionarEncabezado()
        {
            btnNuevo.Location = new Point(header.Width - btnNuevo.Width, 16);
            btnActualizarTop.Location = new Point(btnNuevo.Left - btnActualizarTop.Width - 10, 16);
            lblConexion.Location = new Point(btnActualizarTop.Left - lblConexion.Width - 20, 27);
            lblPunto.Location = new Point(lblConexion.Left - lblPunto.Width + 2, 25);
        }

        // ------------------------------------------------------------
        //  KPIs
        // ------------------------------------------------------------
        private Control ConstruirKpis()
        {
            kpiTabla = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                BackColor = Tema.Fondo,
                Margin = new Padding(0, 0, 0, 14)
            };
            for (int i = 0; i < 5; i++) kpiTabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

            CrearKpi(0, "Total de eventos", Tema.GCalendario, Tema.Primario,
                () => Tuple.Create(filtrados.Count.ToString(), "eventos registrados"));
            CrearKpi(1, "Próximos", Tema.GReloj, Tema.Azul,
                () => Tuple.Create(Contar(EstadoEvento.Proximo).ToString(), Contar(EstadoEvento.EnCurso) + " en curso ahora"));
            CrearKpi(2, "Realizados", Tema.GCheck, Tema.Verde,
                () => Tuple.Create(Contar(EstadoEvento.Realizado).ToString(), Porcentaje(Contar(EstadoEvento.Realizado)) + "% del total"));
            CrearKpi(3, "Cancelados", Tema.GCancelar, Tema.Rojo,
                () => Tuple.Create(Contar(EstadoEvento.Cancelado).ToString(), Porcentaje(Contar(EstadoEvento.Cancelado)) + "% del total"));
            CrearKpi(4, "Ubicaciones", Tema.GPin, Tema.Morado,
                () => Tuple.Create(filtrados.Select(e => e.Ubicacion).Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().Count().ToString(), "lugares distintos"));

            return kpiTabla;
        }

        private void CrearKpi(int col, string titulo, string glyph, Color color, Func<Tuple<string, string>> datos)
        {
            var c = new Card { Dock = DockStyle.Fill, Margin = new Padding(0, 0, col == 4 ? 0 : 12, 0) };
            c.Dibujar = (g, r) =>
            {
                var d = datos();
                var ic = new RectangleF(16, 16, 48, 48);
                Gfx.Rellenar(g, ic, 12, color);
                Gfx.Icono(g, glyph, Tema.Ico18, Color.White, ic);
                Gfx.Texto(g, titulo, Tema.F9B, Tema.Texto, new RectangleF(78, 14, r.Width - 90, 20));
                Gfx.Texto(g, d.Item1, Tema.F20B, Tema.Texto, new RectangleF(77, 32, r.Width - 90, 36));
                Gfx.Texto(g, d.Item2, Tema.F8, Tema.Tenue, new RectangleF(16, 76, r.Width - 28, 18));
            };
            kpis.Add(c);
            kpiTabla.Controls.Add(c, col, 0);
        }

        private int Contar(EstadoEvento e) { return filtrados.Count(x => x.Estado == e); }
        private int Porcentaje(int n) { return filtrados.Count == 0 ? 0 : (int)Math.Round(n * 100.0 / filtrados.Count); }

        // ------------------------------------------------------------
        //  BLOQUE MEDIO: MAPA + GRÁFICOS
        // ------------------------------------------------------------
        private Control ConstruirMedio()
        {
            var mid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                BackColor = Tema.Fondo,
                Margin = new Padding(0, 0, 0, 14)
            };
            mid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
            mid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            mid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            mid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            mid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            // ----- Mapa -----
            cardMapa = new Card { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0) };
            cardMapa.Dibujar = DibujarMapa;
            cardMapa.MouseDown += Mapa_MouseDown;
            cardMapa.MouseMove += Mapa_MouseMove;
            cardMapa.MouseUp += Mapa_MouseUp;
            txtMapaBuscar = new TextBox
            {
                Location = new Point(26, 60),
                Width = 210,
                Font = Tema.F9,
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Buscar evento o lugar..."
            };
            txtMapaBuscar.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; BuscarEnMapa(); }
            };
            cardMapa.Controls.Add(txtMapaBuscar);
            mid.Controls.Add(cardMapa, 0, 0);
            mid.SetRowSpan(cardMapa, 2);

            // ----- Eventos por mes -----
            cardMensual = new Card { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 7) };
            cardMensual.Dibujar = DibujarMensual;
            cmbPeriodo = CrearCombo(new[] { "Últimos 3 meses", "Últimos 6 meses", "Últimos 12 meses" }, 128);
            cmbPeriodo.SelectedIndex = 1;
            cmbPeriodo.SelectedIndexChanged += (s, e) =>
            {
                mesesRango = cmbPeriodo.SelectedIndex == 0 ? 3 : cmbPeriodo.SelectedIndex == 1 ? 6 : 12;
                cardMensual.Invalidate();
            };
            cardMensual.Controls.Add(cmbPeriodo);
            cardMensual.Resize += (s, e) => cmbPeriodo.Location = new Point(cardMensual.Width - cmbPeriodo.Width - 14, 13);
            cmbPeriodo.Location = new Point(200, 13);
            mid.Controls.Add(cardMensual, 1, 0);

            // ----- Tipos de eventos -----
            cardTipos = new Card { Dock = DockStyle.Fill, Margin = new Padding(0, 7, 12, 0) };
            cardTipos.Dibujar = DibujarTipos;
            mid.Controls.Add(cardTipos, 1, 1);

            // ----- Estados (dona) -----
            cardDona = new Card { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 7) };
            cardDona.Dibujar = DibujarDona;
            mid.Controls.Add(cardDona, 2, 0);

            // ----- Próximos eventos -----
            cardProximos = new Card { Dock = DockStyle.Fill, Margin = new Padding(0, 7, 0, 0) };
            cardProximos.Dibujar = DibujarProximos;
            cardProximos.MouseClick += (s, e) =>
            {
                if (rVerTodos.Contains(e.Location)) { VerTodosProximosSolicitado?.Invoke(); return; }
                foreach (var it in itemsProx)
                    if (it.Item1.Contains(e.Location)) { SeleccionarEvento(it.Item2, true, true); return; }
            };
            mid.Controls.Add(cardProximos, 2, 1);

            return mid;
        }

        private ComboBox CrearCombo(string[] items, int ancho)
        {
            var c = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Font = Tema.F9,
                BackColor = Color.White,
                ForeColor = Tema.Texto,
                Width = ancho
            };
            c.Items.AddRange(items);
            if (items.Length > 0) c.SelectedIndex = 0;
            return c;
        }

        // ============================================================
        //  MAPA (dibujado de muestra, listo para reemplazar por Leaflet/OSM)
        // ============================================================
        private Rectangle RectMapa()
        {
            return new Rectangle(14, 52, Math.Max(10, cardMapa.Width - 28), Math.Max(10, cardMapa.Height - 66));
        }

        private void ConstruirCalles()
        {
            calles = new List<Tuple<PointF[], bool>>();
            var rnd = new Random(21);

            for (int i = -9; i <= 9; i++)
            {
                float y = i * 85 + rnd.Next(-18, 18);
                var pts = new List<PointF>();
                for (float x = -900; x <= 900; x += 225) pts.Add(new PointF(x, y + rnd.Next(-22, 22)));
                calles.Add(Tuple.Create(pts.ToArray(), i % 3 == 0));
            }
            for (int i = -10; i <= 10; i++)
            {
                float x = i * 95 + rnd.Next(-18, 18);
                var pts = new List<PointF>();
                for (float y = -800; y <= 800; y += 200) pts.Add(new PointF(x + rnd.Next(-22, 22), y));
                calles.Add(Tuple.Create(pts.ToArray(), i % 4 == 0));
            }
            calles.Add(Tuple.Create(new[] { new PointF(-900, 500), new PointF(-300, 150), new PointF(300, -100), new PointF(900, -420) }, true));
            calles.Add(Tuple.Create(new[] { new PointF(-900, -200), new PointF(-350, 60), new PointF(100, 330), new PointF(500, 700) }, true));
        }

        private void DibujarFondoMapa(Graphics g)
        {
            if (calles == null) ConstruirCalles();

            // parques
            using (var b = new SolidBrush(Color.FromArgb(205, 228, 198)))
            {
                g.FillEllipse(b, -320, -170, 170, 115);
                g.FillEllipse(b, 120, 190, 210, 125);
                g.FillEllipse(b, -70, -340, 130, 85);
                g.FillEllipse(b, -430, 210, 150, 100);
            }

            // calles: bordes y relleno
            foreach (var c in calles)
            {
                using (var pen = new Pen(c.Item2 ? Color.FromArgb(232, 198, 110) : Color.FromArgb(205, 211, 218), c.Item2 ? 9f : 6f))
                {
                    pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round; pen.LineJoin = LineJoin.Round;
                    g.DrawCurve(pen, c.Item1, 0.4f);
                }
            }
            foreach (var c in calles)
            {
                using (var pen = new Pen(c.Item2 ? Color.FromArgb(252, 226, 150) : Color.White, c.Item2 ? 6f : 4f))
                {
                    pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round; pen.LineJoin = LineJoin.Round;
                    g.DrawCurve(pen, c.Item1, 0.4f);
                }
            }

            // lago
            var lago = new[]
            {
                new PointF(160, -700), new PointF(1000, -700), new PointF(1000, 40), new PointF(760, 30),
                new PointF(620, -60), new PointF(470, -130), new PointF(330, -250), new PointF(180, -420)
            };
            using (var b = new SolidBrush(Color.FromArgb(170, 211, 236)))
                g.FillClosedCurve(b, lago, FillMode.Winding, 0.4f);
            using (var pen = new Pen(Color.FromArgb(140, 190, 225), 2f))
                g.DrawClosedCurve(pen, lago, 0.4f, FillMode.Winding);
        }

        private PointF MundoAPantalla(Rectangle m, float wx, float wy)
        {
            return new PointF(m.X + m.Width / 2f + panX + wx * zoom, m.Y + m.Height / 2f + panY + wy * zoom);
        }

        private void DibujarMapa(Graphics g, Rectangle r)
        {
            Gfx.Icono(g, Tema.GPin, Tema.Ico11, Tema.Texto, new RectangleF(16, 14, 22, 26));
            Gfx.Texto(g, "Ubicación de eventos", Tema.F11B, Tema.Texto, new RectangleF(40, 14, r.Width - 60, 26),
                StringAlignment.Near, StringAlignment.Center);

            var m = RectMapa();
            if (m.Width < 80 || m.Height < 80) return;

            var estadoG = g.Save();
            using (var clip = Gfx.RoundRect(m, 10))
                g.SetClip(clip, CombineMode.Replace);

            using (var b = new SolidBrush(Color.FromArgb(236, 240, 237)))
                g.FillRectangle(b, m);

            g.TranslateTransform(m.X + m.Width / 2f + panX, m.Y + m.Height / 2f + panY);
            g.ScaleTransform(zoom, zoom);
            DibujarFondoMapa(g);
            g.ResetTransform();

            // etiquetas
            var pPuno = MundoAPantalla(m, -8, -40);
            Gfx.Texto(g, "Puno", Tema.F12B, Color.FromArgb(45, 55, 75),
                new RectangleF(pPuno.X - 50, pPuno.Y - 12, 100, 24), StringAlignment.Center, StringAlignment.Center);
            var pLago = MundoAPantalla(m, 560, -170);
            Gfx.Texto(g, "Lago Titicaca", Tema.F9I, Color.FromArgb(80, 130, 180),
                new RectangleF(pLago.X - 60, pLago.Y - 10, 120, 20), StringAlignment.Center, StringAlignment.Center);

            // marcadores
            pins.Clear();
            double k = EscalaBase * zoom;
            foreach (var ev in filtrados.Where(e => e.Latitud.HasValue && e.Longitud.HasValue))
            {
                float x = (float)(m.X + m.Width / 2.0 + panX + (ev.Longitud.Value - CLng) * k);
                float y = (float)(m.Y + m.Height / 2.0 + panY - (ev.Latitud.Value - CLat) * k);
                if (x < m.X - 20 || x > m.Right + 20 || y < m.Y - 10 || y > m.Bottom + 60) continue;
                pins.Add(new PinInfo { Ev = ev, Tip = new PointF(x, y), Escala = ReferenceEquals(ev, seleccionado) ? 1.3f : 1f });
            }
            pinsOrdenados = pins.OrderBy(p => ReferenceEquals(p.Ev, seleccionado) ? 1 : 0).ThenBy(p => p.Tip.Y).ToList();
            foreach (var p in pinsOrdenados)
                Gfx.Pin(g, p.Tip.X, p.Tip.Y, Tema.ColorEstado(p.Ev.Estado), p.Escala);

            // ventana emergente del evento seleccionado
            rPopupCerrar = Rectangle.Empty;
            rPopupDetalles = Rectangle.Empty;
            var sel = pinsOrdenados.FirstOrDefault(p => ReferenceEquals(p.Ev, seleccionado));
            if (sel != null) DibujarPopup(g, m, sel);

            // controles de zoom
            rZoomIn = new Rectangle(m.Right - 40, m.Y + 10, 30, 30);
            rZoomOut = new Rectangle(m.Right - 40, m.Y + 40, 30, 30);
            foreach (var zr in new[] { rZoomIn, rZoomOut })
            {
                Gfx.Rellenar(g, zr, 6, Color.White);
                Gfx.Borde(g, zr, 6, Tema.Borde);
            }
            Gfx.Texto(g, "+", Tema.F12B, Tema.Texto, rZoomIn, StringAlignment.Center, StringAlignment.Center);
            Gfx.Texto(g, "−", Tema.F12B, Tema.Texto, rZoomOut, StringAlignment.Center, StringAlignment.Center);

            // leyenda
            string[] nombres = { "Próximo", "En curso", "Realizado", "Cancelado" };
            float total = 10;
            foreach (var n in nombres) total += 16 + Gfx.Ancho(g, n, Tema.F8) + 14;
            var leg = new RectangleF(m.X + 10, m.Bottom - 34, total, 24);
            Gfx.Rellenar(g, leg, 6, Color.FromArgb(240, 255, 255, 255));
            Gfx.Borde(g, leg, 6, Tema.Borde);
            float lx = leg.X + 10;
            for (int i = 0; i < nombres.Length; i++)
            {
                using (var b = new SolidBrush(Tema.ColorEstado((EstadoEvento)i)))
                    g.FillEllipse(b, lx, leg.Y + 8, 8, 8);
                float w = Gfx.Ancho(g, nombres[i], Tema.F8);
                Gfx.Texto(g, nombres[i], Tema.F8, Tema.Texto, new RectangleF(lx + 14, leg.Y, w + 10, 24),
                    StringAlignment.Near, StringAlignment.Center);
                lx += 16 + w + 14;
            }

            Gfx.Texto(g, "Mapa de muestra (diseño)", Tema.F8, Tema.Tenue,
                new RectangleF(m.Right - 170, m.Bottom - 18, 162, 14), StringAlignment.Far, StringAlignment.Center);

            g.Restore(estadoG);

            // borde del mapa
            Gfx.Borde(g, m, 10, Tema.Borde);

            if (filtrados.Count(e => e.Latitud.HasValue) == 0 && filtrados.Count > 0)
                Gfx.Texto(g, "Los eventos filtrados no tienen coordenadas", Tema.F9, Tema.Tenue,
                    new RectangleF(m.X, m.Y, m.Width, m.Height), StringAlignment.Center, StringAlignment.Center);
        }

        private void DibujarPopup(Graphics g, Rectangle m, PinInfo sel)
        {
            const int w = 232, h = 112;
            var ev = sel.Ev;
            float headTop = sel.Tip.Y - 3.1f * 10 * sel.Escala;
            float px = Math.Max(m.X + 8, Math.Min(sel.Tip.X - w / 2f, m.Right - w - 8));
            float py = headTop - h - 10;
            bool abajo = false;
            if (py < m.Y + 8) { py = sel.Tip.Y + 12; abajo = true; }
            var pr = new RectangleF(px, py, w, h);

            Gfx.Rellenar(g, new RectangleF(pr.X, pr.Y + 2, pr.Width, pr.Height), 10, Color.FromArgb(35, 15, 23, 42));
            Gfx.Rellenar(g, pr, 10, Color.White);
            Gfx.Borde(g, pr, 10, Tema.Borde);

            float ax = Math.Max(pr.X + 16, Math.Min(sel.Tip.X, pr.Right - 16));
            using (var b = new SolidBrush(Color.White))
            {
                if (!abajo) g.FillPolygon(b, new[] { new PointF(ax - 8, pr.Bottom - 1), new PointF(ax + 8, pr.Bottom - 1), new PointF(ax, pr.Bottom + 8) });
                else g.FillPolygon(b, new[] { new PointF(ax - 8, pr.Y + 1), new PointF(ax + 8, pr.Y + 1), new PointF(ax, pr.Y - 8) });
            }

            Gfx.Texto(g, ev.Nombre, Tema.F9B, Tema.Texto, new RectangleF(pr.X + 12, pr.Y + 10, w - 44, 20));
            rPopupCerrar = new Rectangle((int)pr.Right - 30, (int)pr.Y + 6, 24, 24);
            Gfx.Icono(g, Tema.GCancelar, Tema.Ico9, Tema.Tenue, rPopupCerrar);

            Gfx.Icono(g, Tema.GCalendario, Tema.Ico9, Tema.Tenue, new RectangleF(pr.X + 10, pr.Y + 36, 16, 18));
            Gfx.Texto(g, ev.Inicio.ToString("dd/MM/yyyy - HH:mm"), Tema.F8, Tema.Texto,
                new RectangleF(pr.X + 30, pr.Y + 36, w - 40, 18), StringAlignment.Near, StringAlignment.Center);
            Gfx.Icono(g, Tema.GPin, Tema.Ico9, Tema.Tenue, new RectangleF(pr.X + 10, pr.Y + 56, 16, 18));
            Gfx.Texto(g, ev.Ubicacion, Tema.F8, Tema.Texto,
                new RectangleF(pr.X + 30, pr.Y + 56, w - 40, 18), StringAlignment.Near, StringAlignment.Center);

            Gfx.Pill(g, Tema.TextoEstado(ev.Estado), Tema.ColorEstado(ev.Estado), pr.X + 12, pr.Y + 82, 20);
            rPopupDetalles = new Rectangle((int)pr.Right - 96, (int)pr.Y + 82, 86, 20);
            Gfx.Texto(g, "Ver detalles", Tema.F8B, Tema.Primario, rPopupDetalles, StringAlignment.Far, StringAlignment.Center);
        }

        private void Mapa_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || !RectMapa().Contains(e.Location)) return;
            arrastrando = true; movido = false;
            dragIni = e.Location;
            panIni = new PointF(panX, panY);
        }

        private void Mapa_MouseMove(object sender, MouseEventArgs e)
        {
            if (arrastrando)
            {
                int dx = e.X - dragIni.X, dy = e.Y - dragIni.Y;
                if (Math.Abs(dx) + Math.Abs(dy) > 4) movido = true;
                if (movido)
                {
                    panX = panIni.X + dx; panY = panIni.Y + dy;
                    cardMapa.Cursor = Cursors.SizeAll;
                    cardMapa.Invalidate();
                }
                return;
            }
            bool mano = rZoomIn.Contains(e.Location) || rZoomOut.Contains(e.Location) ||
                        rPopupCerrar.Contains(e.Location) || rPopupDetalles.Contains(e.Location) ||
                        HitPin(e.Location) != null;
            cardMapa.Cursor = mano ? Cursors.Hand : Cursors.Default;
        }

        private void Mapa_MouseUp(object sender, MouseEventArgs e)
        {
            if (!arrastrando) return;
            arrastrando = false;
            cardMapa.Cursor = Cursors.Default;
            if (!movido) ClickMapa(e.Location);
        }

        private PinInfo HitPin(Point p)
        {
            for (int i = pinsOrdenados.Count - 1; i >= 0; i--)
            {
                var pin = pinsOrdenados[i];
                float cx = pin.Tip.X, cy = pin.Tip.Y - 21f * pin.Escala, rad = 13f * pin.Escala;
                float dx = p.X - cx, dy = p.Y - cy;
                if (dx * dx + dy * dy <= rad * rad) return pin;
            }
            return null;
        }

        private void ClickMapa(Point p)
        {
            if (rZoomIn.Contains(p)) { CambiarZoom(1.25f); return; }
            if (rZoomOut.Contains(p)) { CambiarZoom(1f / 1.25f); return; }
            if (seleccionado != null && rPopupCerrar.Contains(p))
            {
                seleccionado = null;
                gridEventos.ClearSelection();
                cardMapa.Invalidate();
                return;
            }
            if (seleccionado != null && rPopupDetalles.Contains(p))
            {
                VerDetallesSolicitado?.Invoke(seleccionado);
                return;
            }
            var pin = HitPin(p);
            if (pin != null) SeleccionarEvento(pin.Ev, false, true);
        }

        private void CambiarZoom(float factor)
        {
            float nuevo = Math.Max(0.6f, Math.Min(2.8f, zoom * factor));
            float ratio = nuevo / zoom;
            panX *= ratio; panY *= ratio;
            zoom = nuevo;
            cardMapa.Invalidate();
        }

        private void CentrarEn(EventoItem ev)
        {
            if (ev == null || !ev.Latitud.HasValue || !ev.Longitud.HasValue) return;
            double k = EscalaBase * zoom;
            panX = (float)(-(ev.Longitud.Value - CLng) * k);
            panY = (float)(34 + (ev.Latitud.Value - CLat) * k);
        }

        private void BuscarEnMapa()
        {
            string q = (txtMapaBuscar.Text ?? "").Trim();
            if (q.Length == 0) return;
            var ev = filtrados.FirstOrDefault(e => e.Latitud.HasValue &&
                (Contiene(e.Nombre, q) || Contiene(e.Ubicacion, q)));
            if (ev != null) SeleccionarEvento(ev, true, true);
            else System.Media.SystemSounds.Beep.Play();
        }

        // ============================================================
        //  SINCRONIZACIÓN TABLA <-> MAPA
        // ============================================================
        private void SeleccionarEvento(EventoItem ev, bool centrar, bool sincronizarTabla)
        {
            seleccionado = ev;
            if (centrar && ev != null) CentrarEn(ev);

            if (ev != null && sincronizarTabla)
            {
                int idx = filtrados.IndexOf(ev);
                if (idx >= 0)
                {
                    int pg = idx / pageSize;
                    if (pg != pagina) { pagina = pg; LlenarGrid(); }
                    llenando = true;
                    foreach (DataGridViewRow row in gridEventos.Rows)
                        row.Selected = ReferenceEquals(row.Tag, ev);
                    llenando = false;
                }
            }
            cardMapa.Invalidate();
            pager.Invalidate();
        }

        // ============================================================
        //  GRÁFICOS
        // ============================================================
        private static void NiceScale(int max, out int step, out int top)
        {
            step = max <= 5 ? 1 : max <= 10 ? 2 : max <= 25 ? 5 : max <= 50 ? 10 : 20;
            top = ((max + step - 1) / step) * step;
            top = Math.Max(top, 4 * step);
        }

        private static void DibujarEjeY(Graphics g, float left, float right, float yTop, float yBot, int top, int step)
        {
            for (int v = 0; v <= top; v += step)
            {
                float y = yBot - v * (yBot - yTop) / (float)top;
                using (var pen = new Pen(v == 0 ? Color.FromArgb(226, 232, 240) : Color.FromArgb(241, 245, 249)))
                    g.DrawLine(pen, left, y, right, y);
                Gfx.Texto(g, v.ToString(), Tema.F8, Tema.Tenue, new RectangleF(left - 34, y - 8, 28, 16),
                    StringAlignment.Far, StringAlignment.Center);
            }
        }

        private static string Cap(string s)
        {
            s = (s ?? "").TrimEnd('.');
            return s.Length == 0 ? s : char.ToUpper(s[0]) + s.Substring(1);
        }

        private List<Tuple<DateTime, int[]>> EventosPorMes(int n)
        {
            DateTime baseFecha = DateTime.Today;
            if (filtrados.Count > 0)
            {
                var max = filtrados.Max(e => e.Inicio);
                if (max > baseFecha) baseFecha = max;
            }
            var fin = new DateTime(baseFecha.Year, baseFecha.Month, 1);
            var lista = new List<Tuple<DateTime, int[]>>();
            for (int i = n - 1; i >= 0; i--)
            {
                var mes = fin.AddMonths(-i);
                var cnt = new int[4];
                foreach (var e in filtrados)
                    if (e.Inicio.Year == mes.Year && e.Inicio.Month == mes.Month) cnt[(int)e.Estado]++;
                lista.Add(Tuple.Create(mes, cnt));
            }
            return lista;
        }

        private void DibujarMensual(Graphics g, Rectangle r)
        {
            Gfx.Texto(g, "Eventos por mes", Tema.F11B, Tema.Texto, new RectangleF(18, 12, r.Width - 170, 26),
                StringAlignment.Near, StringAlignment.Center);

            var serie = EventosPorMes(mesesRango);
            int max = Math.Max(1, serie.Max(s => s.Item2.Max()));
            int step, top;
            NiceScale(max, out step, out top);

            float left = 44, right = r.Width - 16, yTop = 54, yBot = r.Height - 56;
            if (right - left < 60 || yBot - yTop < 40) return;
            DibujarEjeY(g, left, right, yTop, yBot, top, step);

            if (filtrados.Count == 0)
                Gfx.Texto(g, "Sin datos para mostrar", Tema.F9, Tema.Tenue,
                    new RectangleF(left, yTop, right - left, yBot - yTop), StringAlignment.Center, StringAlignment.Center);

            float groupW = (right - left) / serie.Count;
            float barW = Math.Max(2f, Math.Min(10f, (groupW - 6) / 4f));
            float totalW = 4 * barW + 3 * 2;
            var es = new CultureInfo("es-PE");

            for (int i = 0; i < serie.Count; i++)
            {
                float x0 = left + i * groupW + (groupW - totalW) / 2f;
                for (int s = 0; s < 4; s++)
                {
                    int n = serie[i].Item2[s];
                    if (n == 0) continue;
                    float h = n / (float)top * (yBot - yTop);
                    Gfx.BarraSup(g, new RectangleF(x0 + s * (barW + 2), yBot - h, barW, h), Tema.ColorEstado((EstadoEvento)s), 3);
                }
                Gfx.Texto(g, Cap(serie[i].Item1.ToString("MMM", es)), Tema.F8, Tema.Tenue,
                    new RectangleF(left + i * groupW, yBot + 6, groupW, 16), StringAlignment.Center, StringAlignment.Near);
            }

            // leyenda
            string[] nombres = { "Próximos", "En curso", "Realizados", "Cancelados" };
            float total = 0;
            foreach (var nm in nombres) total += 16 + Gfx.Ancho(g, nm, Tema.F8) + 12;
            float lx = Math.Max(10, (r.Width - total) / 2f);
            float ly = r.Height - 26;
            for (int i = 0; i < 4; i++)
            {
                using (var b = new SolidBrush(Tema.ColorEstado((EstadoEvento)i)))
                    g.FillEllipse(b, lx, ly + 6, 8, 8);
                float w = Gfx.Ancho(g, nombres[i], Tema.F8);
                Gfx.Texto(g, nombres[i], Tema.F8, Tema.Tenue, new RectangleF(lx + 13, ly, w + 10, 20),
                    StringAlignment.Near, StringAlignment.Center);
                lx += 16 + w + 12;
            }
        }

        private void DibujarDona(Graphics g, Rectangle r)
        {
            Gfx.Texto(g, "Estados de eventos", Tema.F11B, Tema.Texto, new RectangleF(18, 12, r.Width - 30, 26),
                StringAlignment.Near, StringAlignment.Center);

            int[] v = new int[4];
            foreach (var e in filtrados) v[(int)e.Estado]++;
            int total = filtrados.Count;

            int size = Math.Max(80, Math.Min(r.Height - 66, (int)(r.Width * 0.46)));
            float grosor = size * 0.2f;
            int x0 = 22, y0 = 44 + (r.Height - 44 - size) / 2;
            var arc = new RectangleF(x0 + grosor / 2, y0 + grosor / 2, size - grosor, size - grosor);

            using (var pen = new Pen(Color.FromArgb(241, 245, 249), grosor))
                g.DrawEllipse(pen, arc);

            int usados = v.Count(n => n > 0);
            float ang = -90f;
            for (int i = 0; i < 4; i++)
            {
                if (v[i] == 0 || total == 0) continue;
                float sweep = v[i] * 360f / total;
                float gap = usados > 1 ? 2f : 0f;
                using (var pen = new Pen(Tema.ColorEstado((EstadoEvento)i), grosor))
                {
                    pen.StartCap = LineCap.Flat; pen.EndCap = LineCap.Flat;
                    g.DrawArc(pen, arc, ang + gap / 2, Math.Max(0.5f, sweep - gap));
                }
                ang += sweep;
            }

            Gfx.Texto(g, total.ToString(), Tema.F24B, Tema.Texto, new RectangleF(x0, y0 + size / 2f - 26, size, 34),
                StringAlignment.Center, StringAlignment.Center);
            Gfx.Texto(g, "Total", Tema.F9, Tema.Tenue, new RectangleF(x0, y0 + size / 2f + 8, size, 18),
                StringAlignment.Center, StringAlignment.Center);

            string[] nombres = { "Próximos", "En curso", "Realizados", "Cancelados" };
            float lx = x0 + size + 26;
            float avail = r.Width - lx - 16;
            float ly = y0 + (size - 4 * 30) / 2f + 4;
            for (int i = 0; i < 4; i++)
            {
                using (var b = new SolidBrush(Tema.ColorEstado((EstadoEvento)i)))
                    g.FillEllipse(b, lx, ly + 6, 10, 10);
                Gfx.Texto(g, nombres[i], Tema.F9, Tema.Tenue, new RectangleF(lx + 18, ly, Math.Max(10, avail - 18), 22),
                    StringAlignment.Near, StringAlignment.Center);
                Gfx.Texto(g, v[i] + " (" + Porcentaje(v[i]) + "%)", Tema.F9B, Tema.Texto,
                    new RectangleF(lx + 18, ly, Math.Max(10, avail - 18), 22), StringAlignment.Far, StringAlignment.Center);
                ly += 30;
            }
        }

        private void DibujarTipos(Graphics g, Rectangle r)
        {
            Gfx.Texto(g, "Tipos de eventos", Tema.F11B, Tema.Texto, new RectangleF(18, 12, r.Width - 30, 26),
                StringAlignment.Near, StringAlignment.Center);

            var grupos = filtrados
                .GroupBy(e => string.IsNullOrWhiteSpace(e.Tipo) ? "Otros" : e.Tipo)
                .Select(x => Tuple.Create(x.Key, x.Count()))
                .OrderByDescending(x => x.Item2).ToList();
            if (grupos.Count > 5)
            {
                int resto = grupos.Skip(4).Sum(x => x.Item2);
                grupos = grupos.Take(4).ToList();
                grupos.Add(Tuple.Create("Otros", resto));
            }

            float left = 44, right = r.Width - 16, yTop = 56, yBot = r.Height - 46;
            if (right - left < 60 || yBot - yTop < 40) return;

            if (grupos.Count == 0)
            {
                Gfx.Texto(g, "Sin datos para mostrar", Tema.F9, Tema.Tenue,
                    new RectangleF(left, yTop, right - left, yBot - yTop), StringAlignment.Center, StringAlignment.Center);
                return;
            }

            int step, top;
            NiceScale(grupos.Max(x => x.Item2), out step, out top);
            DibujarEjeY(g, left, right, yTop, yBot, top, step);

            Color[] paleta = { Tema.Azul, Tema.Verde, Tema.Naranja, Tema.Morado, Tema.Gris };
            float groupW = (right - left) / grupos.Count;
            float bw = Math.Min(46f, groupW * 0.55f);

            for (int i = 0; i < grupos.Count; i++)
            {
                float x = left + i * groupW + (groupW - bw) / 2f;
                float h = grupos[i].Item2 / (float)top * (yBot - yTop);
                Gfx.BarraSup(g, new RectangleF(x, yBot - h, bw, h), paleta[i % paleta.Length], 4);
                Gfx.Texto(g, grupos[i].Item2.ToString(), Tema.F9B, Tema.Texto,
                    new RectangleF(x - 14, yBot - h - 19, bw + 28, 17), StringAlignment.Center, StringAlignment.Far);
                Gfx.Texto(g, grupos[i].Item1, Tema.F8, Tema.Tenue,
                    new RectangleF(left + i * groupW, yBot + 6, groupW, 34), StringAlignment.Center, StringAlignment.Near, true);
            }
        }

        private void DibujarProximos(Graphics g, Rectangle r)
        {
            Gfx.Icono(g, Tema.GCalendario, Tema.Ico11, Tema.Texto, new RectangleF(16, 12, 22, 26));
            Gfx.Texto(g, "Próximos eventos", Tema.F11B, Tema.Texto, new RectangleF(40, 12, r.Width - 130, 26),
                StringAlignment.Near, StringAlignment.Center);

            const string ver = "Ver todos";
            float vw = Gfx.Ancho(g, ver, Tema.F9) + 4;
            rVerTodos = new Rectangle((int)(r.Width - 16 - vw), 12, (int)vw, 26);
            Gfx.Texto(g, ver, Tema.F9, Tema.Primario, rVerTodos, StringAlignment.Far, StringAlignment.Center);

            itemsProx.Clear();
            var prox = filtrados.Where(e => e.Estado == EstadoEvento.Proximo).OrderBy(e => e.Inicio).Take(3).ToList();
            if (prox.Count == 0)
            {
                Gfx.Texto(g, "No hay eventos programados", Tema.F9, Tema.Tenue,
                    new RectangleF(0, 44, r.Width, r.Height - 44), StringAlignment.Center, StringAlignment.Center);
                return;
            }

            var es = new CultureInfo("es-PE");
            int y = 44;
            for (int i = 0; i < prox.Count; i++)
            {
                var ev = prox[i];
                itemsProx.Add(Tuple.Create(new Rectangle(8, y, r.Width - 16, 54), ev));

                var badge = new RectangleF(16, y + 2, 42, 44);
                Gfx.Rellenar(g, badge, 8, Tema.Suave(Tema.Azul, 0.14f));
                Gfx.Texto(g, ev.Inicio.ToString("dd"), Tema.F11B, Tema.Texto,
                    new RectangleF(badge.X, badge.Y + 3, badge.Width, 22), StringAlignment.Center, StringAlignment.Center);
                Gfx.Texto(g, Cap(ev.Inicio.ToString("MMM", es)), Tema.F8B, Tema.Tenue,
                    new RectangleF(badge.X, badge.Y + 24, badge.Width, 16), StringAlignment.Center, StringAlignment.Center);

                float pillW = Gfx.AnchoPill(g, "Próximo");
                float textoW = r.Width - 70 - pillW - 28;
                Gfx.Texto(g, ev.Nombre, Tema.F9B, Tema.Texto, new RectangleF(70, y, textoW, 18),
                    StringAlignment.Near, StringAlignment.Center);
                Gfx.Icono(g, Tema.GReloj, Tema.Ico9, Tema.Tenue, new RectangleF(70, y + 18, 14, 16));
                Gfx.Texto(g, ev.Inicio.ToString("HH:mm") + " - " + ev.Fin.ToString("HH:mm"), Tema.F8, Tema.Tenue,
                    new RectangleF(88, y + 18, textoW, 16), StringAlignment.Near, StringAlignment.Center);
                Gfx.Icono(g, Tema.GPin, Tema.Ico9, Tema.Tenue, new RectangleF(70, y + 34, 14, 16));
                Gfx.Texto(g, ev.Ubicacion, Tema.F8, Tema.Tenue, new RectangleF(88, y + 34, textoW, 16),
                    StringAlignment.Near, StringAlignment.Center);

                Gfx.Pill(g, "Próximo", Tema.Azul, r.Width - 16 - pillW, y + 12, 22);

                if (i < prox.Count - 1)
                    using (var pen = new Pen(Color.FromArgb(241, 245, 249)))
                        g.DrawLine(pen, 16, y + 53, r.Width - 16, y + 53);
                y += 54;
            }
        }

        // ============================================================
        //  TABLA
        // ============================================================
        private Control ConstruirTabla()
        {
            cardTabla = new Card { Dock = DockStyle.Fill, Margin = new Padding(0), Padding = new Padding(14, 12, 14, 10) };

            // grid (se agrega primero para que el Dock Fill se resuelva al final)
            gridEventos = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 38,
                GridColor = Color.FromArgb(238, 242, 247),
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            };
            gridEventos.RowTemplate.Height = 38;

            gridEventos.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Tema.Tenue,
                Font = Tema.F9B,
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                SelectionBackColor = Color.FromArgb(241, 245, 249),
                SelectionForeColor = Tema.Tenue
            };
            gridEventos.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Tema.Texto,
                Font = Tema.F9,
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                SelectionBackColor = Color.FromArgb(239, 246, 255),
                SelectionForeColor = Tema.Texto
            };

            gridEventos.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Sel",
                HeaderText = "",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 42,
                Resizable = DataGridViewTriState.False
            });
            AgregarColumna("Fecha", 105);
            AgregarColumna("Nombre", 210);
            AgregarColumna("Ubicacion", 170);
            AgregarColumna("Tipo", 105);
            AgregarColumna("Estado", 100);
            AgregarColumna("Prioridad", 90);
            AgregarColumna("Responsable", 130);
            var colAcc = new DataGridViewTextBoxColumn
            {
                Name = "Acciones",
                HeaderText = Encabezados["Acciones"],
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 120,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            gridEventos.Columns.Add(colAcc);

            gridEventos.CellPainting += Grid_CellPainting;
            gridEventos.CellMouseClick += Grid_CellMouseClick;
            gridEventos.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var ev = gridEventos.Rows[e.RowIndex].Tag as EventoItem;
                if (ev != null) VerDetallesSolicitado?.Invoke(ev);
            };
            gridEventos.ColumnHeaderMouseClick += (s, e) =>
            {
                string nombre = gridEventos.Columns[e.ColumnIndex].Name;
                if (!Ordenables.Contains(nombre)) return;
                if (colOrden == nombre) ordenAsc = !ordenAsc; else { colOrden = nombre; ordenAsc = true; }
                pagina = 0;
                Refrescar();
            };
            gridEventos.SelectionChanged += (s, e) =>
            {
                if (llenando || gridEventos.SelectedRows.Count == 0) return;
                var ev = gridEventos.SelectedRows[0].Tag as EventoItem;
                if (ev == null) return;
                seleccionado = ev;
                CentrarEn(ev);
                cardMapa.Invalidate();
            };

            pager = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.White };
            pager.Paint += DibujarPager;
            pager.MouseClick += Pager_MouseClick;

            barraAcciones = ConstruirBarraAcciones();
            barraFiltros = ConstruirBarraFiltros();
            var titulo = ConstruirTituloTabla();

            cardTabla.Controls.Add(gridEventos);
            cardTabla.Controls.Add(pager);
            cardTabla.Controls.Add(barraAcciones);
            cardTabla.Controls.Add(barraFiltros);
            cardTabla.Controls.Add(titulo);

            // menú "más acciones"
            ctxMas = new ContextMenuStrip { Font = Tema.F9 };
            ctxMas.Items.Add("Cancelar evento", null, (s, e) => { if (eventoMenu != null) CancelarSolicitado?.Invoke(eventoMenu); });
            ctxMas.Items.Add("Eliminar", null, (s, e) => { if (eventoMenu != null) EliminarSolicitado?.Invoke(eventoMenu); });

            return cardTabla;
        }

        private void AgregarColumna(string nombre, float peso)
        {
            gridEventos.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = nombre,
                HeaderText = Encabezados[nombre],
                FillWeight = peso,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.Programmatic
            });
        }

        private Control ConstruirTituloTabla()
        {
            var p = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.White };
            var ico = new Label
            {
                Text = Tema.GCalendario,
                Font = Tema.Ico11,
                ForeColor = Tema.Texto,
                BackColor = Color.White,
                AutoSize = false,
                Size = new Size(24, 28),
                Location = new Point(0, 2),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var lbl = new Label
            {
                Text = "Eventos",
                Font = Tema.F11B,
                ForeColor = Tema.Texto,
                BackColor = Color.White,
                AutoSize = false,
                Size = new Size(200, 28),
                Location = new Point(26, 2),
                TextAlign = ContentAlignment.MiddleLeft
            };
            p.Controls.Add(ico);
            p.Controls.Add(lbl);
            return p;
        }

        private Panel ConstruirBarraFiltros()
        {
            var p = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Color.White };

            txtBuscar = new TextBox
            {
                Location = new Point(0, 22),
                Width = 250,
                Font = Tema.F9,
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Buscar por nombre, ubicación o responsable..."
            };
            txtBuscar.TextChanged += (s, e) => FiltrosCambiaron();
            p.Controls.Add(txtBuscar);

            cmbEstado = CrearCombo(new[] { "Todos", "Próximo", "En curso", "Realizado", "Cancelado" }, 130);
            cmbPrioridad = CrearCombo(new[] { "Todas", "Alta", "Media", "Baja" }, 110);
            cmbUbicacion = CrearCombo(new[] { "Todas" }, 170);
            dtDesde = new DateTimePicker { Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false, Font = Tema.F9 };
            dtHasta = new DateTimePicker { Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false, Font = Tema.F9 };

            AgregarCampo(p, "Estado", cmbEstado, 266, 130);
            AgregarCampo(p, "Prioridad", cmbPrioridad, 408, 110);
            AgregarCampo(p, "Ubicación", cmbUbicacion, 530, 170);
            AgregarCampo(p, "Desde", dtDesde, 712, 120);
            AgregarCampo(p, "Hasta", dtHasta, 844, 120);

            cmbEstado.SelectedIndexChanged += (s, e) => FiltrosCambiaron();
            cmbPrioridad.SelectedIndexChanged += (s, e) => FiltrosCambiaron();
            cmbUbicacion.SelectedIndexChanged += (s, e) => FiltrosCambiaron();
            dtDesde.ValueChanged += (s, e) => FiltrosCambiaron();
            dtHasta.ValueChanged += (s, e) => FiltrosCambiaron();

            lblResultados = new Label
            {
                Text = "0 resultados",
                Font = Tema.F9,
                ForeColor = Tema.Tenue,
                BackColor = Color.White,
                AutoSize = true,
                Location = new Point(1000, 26)
            };
            p.Controls.Add(lblResultados);
            p.Resize += (s, e) => PosicionarResultados();
            return p;
        }

        private void AgregarCampo(Panel p, string etiqueta, Control c, int x, int ancho)
        {
            var l = new Label
            {
                Text = etiqueta,
                Font = Tema.F8,
                ForeColor = Tema.Tenue,
                BackColor = Color.White,
                AutoSize = true,
                Location = new Point(x + 2, 3)
            };
            c.Location = new Point(x, 22);
            c.Width = ancho;
            p.Controls.Add(l);
            p.Controls.Add(c);
        }

        private void PosicionarResultados()
        {
            if (lblResultados == null || barraFiltros == null) return;
            lblResultados.Location = new Point(barraFiltros.Width - lblResultados.Width, 26);
        }

        private Panel ConstruirBarraAcciones()
        {
            var p = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.White };

            var bDet = new RBtn { Glyph = Tema.GOjo, Caption = "Ver detalles", Size = new Size(124, 34), Location = new Point(0, 6), BackColor = Color.White };
            bDet.Click += (s, e) => { var ev = EventoOAviso(); if (ev != null) VerDetallesSolicitado?.Invoke(ev); };

            var bEd = new RBtn { Glyph = Tema.GEditar, Caption = "Editar", Size = new Size(98, 34), Location = new Point(134, 6), BackColor = Color.White };
            bEd.Click += (s, e) => { var ev = EventoOAviso(); if (ev != null) EditarSolicitado?.Invoke(ev); };

            var bEl = new RBtn
            {
                Glyph = Tema.GEliminar,
                Caption = "Eliminar",
                Size = new Size(104, 34),
                Location = new Point(242, 6),
                BackColor = Color.White,
                Fore = Tema.Rojo,
                Linea = Color.FromArgb(254, 202, 202),
                RellenoHover = Color.FromArgb(254, 242, 242)
            };
            bEl.Click += (s, e) => { var ev = EventoOAviso(); if (ev != null) EliminarSolicitado?.Invoke(ev); };

            var bExp = new RBtn
            {
                Glyph = Tema.GDescargar,
                Caption = "Exportar  ▾",
                Size = new Size(122, 34),
                Location = new Point(356, 6),
                BackColor = Color.White,
                Relleno = Tema.Primario,
                RellenoHover = Tema.PrimarioHover,
                Linea = Color.Empty,
                Fore = Color.White
            };
            bExp.Click += (s, e) => ExportarSolicitado?.Invoke();

            btnActualizarTabla = new RBtn { Glyph = Tema.GRefrescar, Caption = "Actualizar", Size = new Size(110, 34), BackColor = Color.White };
            btnActualizarTabla.Click += (s, e) => { ActualizarSolicitado?.Invoke(); Refrescar(); };

            var lblMostrar = new Label
            {
                Text = "Mostrar:",
                Font = Tema.F9,
                ForeColor = Tema.Tenue,
                BackColor = Color.White,
                AutoSize = true
            };
            cmbMostrar = CrearCombo(new[] { "5", "10", "20", "50" }, 64);
            cmbMostrar.SelectedIndex = 1;
            cmbMostrar.SelectedIndexChanged += (s, e) =>
            {
                pageSize = int.Parse((string)cmbMostrar.SelectedItem);
                pagina = 0;
                AjustarAlturas();
                Refrescar();
            };

            p.Controls.AddRange(new Control[] { bDet, bEd, bEl, bExp, btnActualizarTabla, lblMostrar, cmbMostrar });
            p.Resize += (s, e) =>
            {
                cmbMostrar.Location = new Point(p.Width - cmbMostrar.Width, 10);
                lblMostrar.Location = new Point(cmbMostrar.Left - lblMostrar.Width - 6, 13);
                btnActualizarTabla.Location = new Point(lblMostrar.Left - btnActualizarTabla.Width - 14, 6);
            };
            return p;
        }

        private EventoItem EventoOAviso()
        {
            if (seleccionado == null)
            {
                MessageBox.Show("Selecciona un evento de la tabla o del mapa.", "Eventos",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }
            return seleccionado;
        }

        // ---------- pintado de celdas ----------
        private void Grid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string col = gridEventos.Columns[e.ColumnIndex].Name;
            if (col != "Tipo" && col != "Estado" && col != "Prioridad" && col != "Acciones") return;

            var ev = gridEventos.Rows[e.RowIndex].Tag as EventoItem;
            if (ev == null) return;

            e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border | DataGridViewPaintParts.SelectionBackground);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            float y = e.CellBounds.Y + (e.CellBounds.Height - 22) / 2f;

            if (col == "Acciones")
            {
                string[] iconos = { Tema.GOjo, Tema.GEditar, Tema.GMas };
                for (int i = 0; i < 3; i++)
                {
                    var rb = new RectangleF(e.CellBounds.X + 8 + i * 32, e.CellBounds.Y + (e.CellBounds.Height - 26) / 2f, 26, 26);
                    Gfx.Rellenar(g, rb, 6, Color.White);
                    Gfx.Borde(g, rb, 6, Tema.Borde);
                    Gfx.Icono(g, iconos[i], Tema.Ico9, Tema.Texto, rb);
                }
            }
            else
            {
                Color c = col == "Estado" ? Tema.ColorEstado(ev.Estado)
                        : col == "Prioridad" ? Tema.ColorPrioridad(ev.Prioridad)
                        : Tema.ColorTipo(ev.Tipo);
                Gfx.Pill(g, Convert.ToString(e.Value), c, e.CellBounds.X + 8, y, 22);
            }
            e.Handled = true;
        }

        private void Grid_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (gridEventos.Columns[e.ColumnIndex].Name != "Acciones") return;
            var ev = gridEventos.Rows[e.RowIndex].Tag as EventoItem;
            if (ev == null) return;

            int rel = e.X - 8;
            if (rel < 0) return;
            int idx = rel / 32;
            if (idx > 2 || rel % 32 > 26) return;

            if (idx == 0) VerDetallesSolicitado?.Invoke(ev);
            else if (idx == 1) EditarSolicitado?.Invoke(ev);
            else { eventoMenu = ev; ctxMas.Show(Cursor.Position); }
        }

        // ---------- paginación ----------
        private void DibujarPager(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            pagerRects.Clear();

            int total = filtrados.Count;
            int desde = total == 0 ? 0 : pagina * pageSize + 1;
            int hasta = Math.Min(total, (pagina + 1) * pageSize);
            Gfx.Texto(g, "Mostrando " + desde + " - " + hasta + " de " + total + " resultados", Tema.F9, Tema.Tenue,
                new RectangleF(0, 0, 420, pager.Height), StringAlignment.Near, StringAlignment.Center);

            int pages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            var items = new List<Tuple<string, int>> { Tuple.Create("‹", -1) };
            int inicio = Math.Max(0, Math.Min(pagina - 2, pages - 5));
            for (int i = inicio; i < Math.Min(pages, inicio + 5); i++)
                items.Add(Tuple.Create((i + 1).ToString(), i));
            items.Add(Tuple.Create("›", -2));

            const int size = 30, gap = 6;
            int totalW = items.Count * size + (items.Count - 1) * gap;
            int x = pager.Width - totalW;
            int y = (pager.Height - size) / 2;

            foreach (var it in items)
            {
                var rc = new Rectangle(x, y, size, size);
                bool activo = it.Item2 == pagina;
                bool deshab = (it.Item2 == -1 && pagina <= 0) || (it.Item2 == -2 && pagina >= pages - 1);
                Gfx.Rellenar(g, rc, 7, activo ? Tema.Primario : Color.White);
                if (!activo) Gfx.Borde(g, rc, 7, Tema.Borde);
                Gfx.Texto(g, it.Item1, Tema.F9B, activo ? Color.White : deshab ? Tema.Gris : Tema.Texto, rc,
                    StringAlignment.Center, StringAlignment.Center);
                pagerRects.Add(Tuple.Create(rc, it.Item2));
                x += size + gap;
            }
        }

        private void Pager_MouseClick(object sender, MouseEventArgs e)
        {
            int pages = Math.Max(1, (int)Math.Ceiling(filtrados.Count / (double)pageSize));
            foreach (var it in pagerRects)
            {
                if (!it.Item1.Contains(e.Location)) continue;
                if (it.Item2 == -1) { if (pagina > 0) pagina--; else return; }
                else if (it.Item2 == -2) { if (pagina < pages - 1) pagina++; else return; }
                else pagina = it.Item2;
                LlenarGrid();
                pager.Invalidate();
                return;
            }
        }

        // ============================================================
        //  FILTRADO / REFRESCO
        // ============================================================
        private static bool Contiene(string texto, string q)
        {
            return (texto ?? "").IndexOf(q, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        private void FiltrosCambiaron()
        {
            if (inicializando) return;
            pagina = 0;
            Refrescar();
        }

        private void AplicarFiltros()
        {
            IEnumerable<EventoItem> lista = todos;

            string q = (txtBuscar.Text ?? "").Trim();
            if (q.Length > 0)
                lista = lista.Where(e => Contiene(e.Nombre, q) || Contiene(e.Ubicacion, q) ||
                                         Contiene(e.Responsable, q) || Contiene(e.Tipo, q));

            if (cmbEstado.SelectedIndex > 0)
            {
                var est = (EstadoEvento)(cmbEstado.SelectedIndex - 1);
                lista = lista.Where(e => e.Estado == est);
            }
            if (cmbPrioridad.SelectedIndex > 0)
            {
                string pr = (string)cmbPrioridad.SelectedItem;
                lista = lista.Where(e => e.Prioridad == pr);
            }
            if (cmbUbicacion.SelectedIndex > 0)
            {
                string ub = (string)cmbUbicacion.SelectedItem;
                lista = lista.Where(e => e.Ubicacion == ub);
            }
            if (dtDesde.Checked) lista = lista.Where(e => e.Inicio.Date >= dtDesde.Value.Date);
            if (dtHasta.Checked) lista = lista.Where(e => e.Inicio.Date <= dtHasta.Value.Date);

            var sel = Selectores[colOrden];
            lista = ordenAsc ? lista.OrderBy(sel) : lista.OrderByDescending(sel);
            filtrados = lista.ToList();
        }

        private void ActualizarEncabezados()
        {
            foreach (DataGridViewColumn c in gridEventos.Columns)
            {
                string baseTxt;
                if (!Encabezados.TryGetValue(c.Name, out baseTxt)) continue;
                c.HeaderText = baseTxt + (c.Name == colOrden ? (ordenAsc ? "  ▲" : "  ▼") : "");
            }
        }

        private void Refrescar()
        {
            if (gridEventos == null) return;
            AplicarFiltros();

            int pages = Math.Max(1, (int)Math.Ceiling(filtrados.Count / (double)pageSize));
            if (pagina >= pages) pagina = pages - 1;
            if (seleccionado != null && !filtrados.Contains(seleccionado)) seleccionado = null;

            ActualizarEncabezados();
            LlenarGrid();
            lblResultados.Text = filtrados.Count + " resultados";
            PosicionarResultados();

            foreach (var k in kpis) k.Invalidate();
            cardMensual.Invalidate();
            cardDona.Invalidate();
            cardTipos.Invalidate();
            cardProximos.Invalidate();
            cardMapa.Invalidate();
            pager.Invalidate();
        }

        private void LlenarGrid()
        {
            llenando = true;
            try
            {
                gridEventos.Rows.Clear();
                foreach (var ev in filtrados.Skip(pagina * pageSize).Take(pageSize))
                {
                    int i = gridEventos.Rows.Add(false, ev.Inicio.ToString("dd/MM/yyyy HH:mm"), ev.Nombre,
                        ev.Ubicacion, ev.Tipo, Tema.TextoEstado(ev.Estado), ev.Prioridad, ev.Responsable, "");
                    gridEventos.Rows[i].Tag = ev;
                }
                gridEventos.ClearSelection();
                if (seleccionado != null)
                    foreach (DataGridViewRow row in gridEventos.Rows)
                        if (ReferenceEquals(row.Tag, seleccionado)) { row.Selected = true; break; }
            }
            finally { llenando = false; }
        }

        // ============================================================
        //  DATOS DE MUESTRA (solo para ver el diseño)
        // ============================================================
        private static List<EventoItem> GenerarDatosDemo()
        {
            var rnd = new Random(7);
            string[] nombres =
            {
                "Taller de Producción Audiovisual", "Grabación de Spot Publicitario", "Conferencia Tecnológica",
                "Reunión de Producción", "Cobertura de Evento", "Presentación de Equipos",
                "Reunión de Planificación", "Capacitación en Iluminación", "Transmisión en Vivo",
                "Sesión de Fotos Corporativa", "Mantenimiento de Equipos", "Lanzamiento de Producto"
            };
            string[] tipos =
            {
                "Taller", "Producción", "Conferencia", "Reunión", "Cobertura", "Conferencia",
                "Reunión", "Taller", "Producción", "Corporativo", "Reunión", "Corporativo"
            };
            string[] responsables = { "Carlos Méndez", "Ana Torres", "Luis Rojas", "María López", "Jorge Quispe", "Rosa Mamani" };
            string[] prioridades = { "Alta", "Media", "Baja" };
            var lugares = new[]
            {
                Tuple.Create("Centro de Convenciones Puno", -15.8350, -70.0050),
                Tuple.Create("Sala de Conferencias - Puno", -15.8405, -70.0275),
                Tuple.Create("Estudio Central", -15.8470, -70.0190),
                Tuple.Create("Oficina Principal", -15.8300, -70.0245),
                Tuple.Create("Plaza de Armas - Puno", -15.8395, -70.0215),
                Tuple.Create("Teatro Municipal", -15.8520, -70.0310)
            };

            const string estados = "RPRRPCRXPRRPCRRPRXPRRPRP";   // 8 P, 2 C, 12 R, 2 X
            var lista = new List<EventoItem>();
            int p = 0, rr = 0;

            for (int i = 0; i < estados.Length; i++)
            {
                char c = estados[i];
                int hora = 8 + rnd.Next(0, 9);
                DateTime inicio;
                EstadoEvento est;

                if (c == 'P') { est = EstadoEvento.Proximo; inicio = DateTime.Today.AddDays(2 + 3 * p++).AddHours(hora); }
                else if (c == 'C') { est = EstadoEvento.EnCurso; inicio = DateTime.Now.AddHours(-1); }
                else if (c == 'R') { est = EstadoEvento.Realizado; inicio = DateTime.Today.AddDays(-5 - 9 * rr++).AddHours(hora); }
                else { est = EstadoEvento.Cancelado; inicio = DateTime.Today.AddDays(rnd.Next(-20, 15)).AddHours(hora); }

                var lugar = lugares[rnd.Next(lugares.Length)];
                lista.Add(new EventoItem
                {
                    Id = i + 1,
                    Nombre = nombres[i % nombres.Length],
                    Tipo = tipos[i % tipos.Length],
                    Ubicacion = lugar.Item1,
                    Latitud = lugar.Item2 + (rnd.NextDouble() - 0.5) * 0.006,
                    Longitud = lugar.Item3 + (rnd.NextDouble() - 0.5) * 0.006,
                    Responsable = responsables[rnd.Next(responsables.Length)],
                    Prioridad = prioridades[rnd.Next(prioridades.Length)],
                    Inicio = inicio,
                    Fin = inicio.AddHours(2 + rnd.Next(0, 2)),
                    Estado = est
                });
            }
            return lista;
        }
    }
}