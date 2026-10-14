#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Npgsql;

namespace SUSTENTACION.PanelAdministrador
{
    public class CotrolReporte : UserControl
    {
        // ============================================================
        // ARTEMUSA AV - DISEÑO DE REPORTES PROFESIONAL
        // SOLO SE CAMBIA PRESENTACIÓN VISUAL.
        // LA LÓGICA DE DATOS, MYSQL, FILTROS, EXPORTACIÓN Y WHATSAPP
        // SE MANTIENE.
        // ============================================================
        // Paleta visual estilo dashboard oscuro profesional
        private readonly Color canvas = Color.FromArgb(6, 12, 22);
        private readonly Color surface = Color.FromArgb(15, 29, 47);
        private readonly Color card = Color.FromArgb(21, 38, 59);
        private readonly Color card2 = Color.FromArgb(26, 45, 68);
        private readonly Color header = Color.FromArgb(12, 25, 42);

        private readonly Color ink = Color.FromArgb(250, 252, 255);
        private readonly Color muted = Color.FromArgb(190, 205, 224);

        private readonly Color green = Color.FromArgb(63, 207, 113);
        private readonly Color greenBright = Color.FromArgb(74, 230, 206);
        private readonly Color blue = Color.FromArgb(94, 178, 255);
        private readonly Color amber = Color.FromArgb(242, 190, 84);
        private readonly Color violet = Color.FromArgb(150, 118, 255);
        private readonly Color red = Color.FromArgb(247, 99, 99);

        private readonly Color border = Color.FromArgb(61, 82, 109);
        private readonly Color grid = Color.FromArgb(49, 68, 93);

        private readonly List<Font> fonts = new List<Font>();

        private TextBox search;
        private DateTimePicker fromDate, toDate;
        private ComboBox typeFilter, responsibleFilter;
        private Button applyButton;
        private Label[] values = new Label[4];
        private Label state, resultCount;
        private DataGridView detailGrid;
        private DataTable reportData;
        private bool loading;

        // Estado de los controles visuales de WhatsApp
        private bool whatsappGeneralEnabled = true;
        private bool whatsappDelayEnabled = false;

        private const string HistoryTable = "artemusa_movimientos_v1";

        public CotrolReporte()
        {
            Dock = DockStyle.Fill;
            BackColor = canvas;
            DoubleBuffered = true;
            Font = FontOf(9);

            BuildInterface();

            Load += (s, e) => LoadReport();
        }

        // ============================================================
        // HELPERS VISUALES
        // ============================================================

        private Font FontOf(float size, bool bold = false)
        {
            Font f = new Font(
                "Segoe UI",
                size,
                bold ? FontStyle.Bold : FontStyle.Regular
            );

            fonts.Add(f);
            return f;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (reportData != null)
                    reportData.Dispose();

                foreach (Font f in fonts)
                    f.Dispose();
            }
        }

        private Label LabelOf(
            string text,
            float size,
            Color color,
            bool bold = false)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = FontOf(size, bold),
                ForeColor = color,
                BackColor = Color.Transparent
            };
        }

        private Button ButtonOf(
            string text,
            EventHandler click,
            bool primary = false,
            int width = 128)
        {
            Button b = new Button
            {
                Text = text,
                Width = width,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                BackColor = primary
                    ? green
                    : Color.FromArgb(35, 52, 73),
                ForeColor = primary
                    ? Color.FromArgb(5, 22, 28)
                    : ink,
                Font = FontOf(8.8f, true),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                Margin = new Padding(0, 0, 10, 0)
            };

            b.FlatAppearance.BorderSize = primary ? 0 : 1;
            b.FlatAppearance.BorderColor = border;

            b.FlatAppearance.MouseOverBackColor = primary
                ? greenBright
                : Color.FromArgb(44, 63, 86);

            b.Click += click;

            Rounded(b, 9);

            return b;
        }

        private ComboBox ComboOf(params string[] items)
        {
            ComboBox c = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 45, 65),
                ForeColor = ink,
                Font = FontOf(9),
                Width = 160
            };

            c.Items.AddRange(items);

            if (items.Length > 0)
                c.SelectedIndex = 0;

            return c;
        }

        private void Rounded(Control control, int radius)
        {
            Action apply = () =>
            {
                if (control.Width <= 2 || control.Height <= 2)
                    return;

                Rectangle rect = new Rectangle(
                    0,
                    0,
                    control.Width - 1,
                    control.Height - 1
                );

                using (GraphicsPath path = RoundPath(rect, radius))
                {
                    control.Region?.Dispose();
                    control.Region = new Region(path);
                }
            };

            control.Resize += (s, e) => apply();
            apply();
        }

        private static GraphicsPath RoundPath(
            Rectangle rect,
            int radius)
        {
            GraphicsPath path = new GraphicsPath();

            if (rect.Width < 4 || rect.Height < 4)
            {
                path.AddRectangle(rect);
                return path;
            }

            int r = Math.Max(
                1,
                Math.Min(
                    radius,
                    Math.Min(rect.Width / 2, rect.Height / 2)
                )
            );

            int d = r * 2;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            return path;
        }

        // ============================================================
        // INTERFAZ GENERAL
        // ============================================================

        private void BuildInterface()
        {
            SuspendLayout();
            AutoScroll = true;

            Panel page = new Panel
            {
                BackColor = canvas,
                Padding = new Padding(18)
            };

            Controls.Add(page);

            TableLayoutPanel main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = Color.Transparent
            };

            main.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100)
            );

            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 338));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            page.Controls.Add(main);

            main.Controls.Add(BuildHeader(), 0, 0);
            main.Controls.Add(BuildFilters(), 0, 1);
            main.Controls.Add(BuildKpis(), 0, 2);
            main.Controls.Add(BuildCharts(), 0, 3);
            main.Controls.Add(BuildTable(), 0, 4);

            state = LabelOf(
                "Conectando con el servidor…",
                8.5f,
                muted
            );

            state.AutoSize = false;
            state.Dock = DockStyle.Fill;
            state.TextAlign = ContentAlignment.MiddleLeft;

            main.Controls.Add(state, 0, 5);

            Action resize = () =>
            {
                page.Size = new Size(
                    Math.Max(
                        1140,
                        ClientSize.Width
                        - SystemInformation.VerticalScrollBarWidth
                        - 2
                    ),
                    Math.Max(
                        900,
                        ClientSize.Height - 2
                    )
                );
            };

            Resize += (s, e) => resize();
            resize();

            ResumeLayout(true);
        }

        // ============================================================
        // CABECERA
        // ============================================================

        private Control BuildHeader()
        {
            Panel p = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = canvas,
                Margin = new Padding(0)
            };

            Label crumb = LabelOf(
                "ARTEMUSA TV  /  REPORTES",
                7.6f,
                Color.FromArgb(116, 143, 176),
                true
            );

            crumb.Location = new Point(2, 1);

            Label title = LabelOf(
                "Panel de Reportes",
                20.5f,
                Color.White,
                true
            );

            title.Location = new Point(0, 18);

            Label sub = LabelOf(
                "Resumen visual de equipos, movimientos, retornos y alertas.",
                8.5f,
                muted
            );

            sub.Location = new Point(2, 58);

            Panel badge = new Panel
            {
                Size = new Size(156, 34),
                BackColor = Color.FromArgb(75, 57, 35)
            };

            Rounded(badge, 9);

            Label badgeText = new Label
            {
                Text = "🔔  Alertas activas",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = FontOf(8.1f, true),
                ForeColor = Color.FromArgb(245, 203, 127),
                BackColor = Color.Transparent
            };

            badge.Controls.Add(badgeText);
            p.Controls.Add(badge);

            p.Controls.AddRange(
                new Control[]
                {
                    crumb,
                    title,
                    sub
                }
            );

            p.Resize += (s, e) =>
            {
                badge.Location = new Point(
                    p.Width - badge.Width - 2,
                    19
                );
            };

            return p;
        }

        // ============================================================
        // FILTROS
        // ============================================================

        private Control BuildFilters()
        {
            Panel p = new Card
            {
                Dock = DockStyle.Fill,
                BackColor = surface,
                Padding = new Padding(14, 9, 14, 8),
                Margin = new Padding(0, 0, 0, 10)
            };

            TableLayoutPanel row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 2,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));

            row.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
            row.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            row.Controls.Add(LabelOf("DESDE", 7.2f, muted, true), 0, 0);
            row.Controls.Add(LabelOf("HASTA", 7.2f, muted, true), 1, 0);
            row.Controls.Add(LabelOf("TIPO", 7.2f, muted, true), 2, 0);
            row.Controls.Add(LabelOf("RESPONSABLE", 7.2f, muted, true), 3, 0);
            row.Controls.Add(LabelOf("BUSCAR", 7.2f, muted, true), 4, 0);

            fromDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 130,
                Value = DateTime.Today.AddDays(-30),
                CalendarForeColor = ink,
                CalendarMonthBackground = surface
            };

            toDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 130,
                Value = DateTime.Today,
                CalendarForeColor = ink,
                CalendarMonthBackground = surface
            };

            typeFilter = ComboOf(
                "Todos",
                "Entrada",
                "Salida"
            );

            responsibleFilter = ComboOf("Todos");
            responsibleFilter.Width = 160;

            applyButton = ButtonOf(
                "Aplicar",
                (s, e) => LoadReport(),
                true,
                100
            );

            applyButton.Height = 30;

            search = new TextBox
            {
                PlaceholderText = "Buscar equipo o responsable…",
                Dock = DockStyle.Top,
                Font = FontOf(9),
                Margin = new Padding(0, 2, 12, 0),
                BackColor = Color.FromArgb(24, 38, 57),
                ForeColor = ink,
                BorderStyle = BorderStyle.FixedSingle
            };

            row.Controls.Add(fromDate, 0, 1);
            row.Controls.Add(toDate, 1, 1);
            row.Controls.Add(typeFilter, 2, 1);
            row.Controls.Add(responsibleFilter, 3, 1);
            row.Controls.Add(search, 4, 1);
            row.Controls.Add(applyButton, 5, 1);

            p.Controls.Add(row);

            return p;
        }

        // ============================================================
        // KPI
        // ============================================================

        private Control BuildKpis()
        {
            TableLayoutPanel row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            string[] names =
            {
        "1. ENTRADAS",
        "2. SALIDAS",
        "3. EN MANTENIMIENTO",
        "4. DEVOLUCIONES PENDIENTES"
    };

            string[] notesText =
            {
        "Equipos ingresados",
        "Equipos retirados",
        "Equipos en revisión/mantenimiento",
        "Equipos aún no devueltos"
    };
            string[] icons =
{
    "◫",   // Entradas
    "⇄",   // Salidas
    "!",   // Mantenimiento
    "↩"    // Devoluciones pendientes
};



            Color[] colors =
            {
        amber,
        greenBright,
        red,
        greenBright
    };

            // Las 4 tarjetas ocupan el mismo espacio
            for (int i = 0; i < 4; i++)
            {
                row.ColumnStyles.Add(
                    new ColumnStyle(SizeType.Percent, 25F)
                );
            }

            row.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100F)
            );

            for (int i = 0; i < 4; i++)
            {
                Panel p = new Card
                {
                    Dock = DockStyle.Fill,

                    BackColor = i == 2
                        ? Color.FromArgb(69, 34, 39)
                        : card,

                    Margin = new Padding(
                        i == 0 ? 0 : 5,
                        0,
                        i == 3 ? 0 : 5,
                        10
                    ),

                    Padding = new Padding(0)
                };

                // =========================
                // TÍTULO
                // =========================
                Label t = LabelOf(
                    names[i],
                    7.9f,
                    Color.White,
                    true
                );

                t.AutoSize = false;
                t.Location = new Point(18, 10);
                t.Size = new Size(220, 24);
                t.TextAlign = ContentAlignment.MiddleLeft;

                // =========================
                // VALOR
                // =========================
                values[i] = LabelOf(
                    "—",
                    22,
                    Color.White,
                    true
                );

                values[i].AutoSize = false;
                values[i].Location = new Point(18, 37);
                values[i].Size = new Size(120, 35);
                values[i].TextAlign = ContentAlignment.MiddleLeft;

                // =========================
                // DESCRIPCIÓN
                // =========================
                Label n = LabelOf(
                    notesText[i],
                    7.8f,
                    colors[i],
                    true
                );

                // Evita que se corte la E de "Equipos"
                n.AutoSize = false;
                n.Location = new Point(18, 73);
                n.Size = new Size(235, 26);
                n.TextAlign = ContentAlignment.MiddleLeft;
                n.Padding = new Padding(2, 0, 0, 0);

                // =========================
                // ICONO
                // =========================
                Label icon = LabelOf(
                    icons[i],
                    18f,
                    colors[i],
                    true
                );

                icon.AutoSize = false;
                icon.Size = new Size(38, 38);
                icon.TextAlign = ContentAlignment.MiddleCenter;

                p.Controls.Add(t);
                p.Controls.Add(values[i]);
                p.Controls.Add(n);
                p.Controls.Add(icon);

                int index = i;

                // =========================
                // AJUSTE RESPONSIVO
                // =========================
                p.Resize += (s, e) =>
                {
                    // Icono siempre a la derecha
                    icon.Location = new Point(
                        Math.Max(145, p.Width - 50),
                        17
                    );

                    // Ajustar ancho del título
                    t.Width = Math.Max(
                        100,
                        p.Width - 75
                    );

                    // Ajustar descripción al ancho de la tarjeta
                    n.Width = Math.Max(
                        100,
                        p.Width - 32
                    );
                };

                // =========================
                // MINI GRÁFICA
                // =========================
                p.Paint += (s, e) =>
                {
                    // No dibujar gráfica en devoluciones pendientes
                    if (index == 3)
                        return;

                    e.Graphics.SmoothingMode =
                        SmoothingMode.AntiAlias;

                    int baseY = p.Height - 18;

                    int x = Math.Max(
                        135,
                        p.Width - 118
                    );

                    Point[] points =
                    {
                new Point(x, baseY - 2),
                new Point(x + 18, baseY - 10),
                new Point(x + 36, baseY - 7),
                new Point(x + 54, baseY - 15),
                new Point(x + 74, baseY - 9)
            };

                    using (Pen pen = new Pen(
                        Color.FromArgb(
                            145,
                            colors[index]
                        ),
                        2.1f
                    ))
                    {
                        pen.LineJoin = LineJoin.Round;

                        e.Graphics.DrawLines(
                            pen,
                            points
                        );
                    }
                };

                row.Controls.Add(
                    p,
                    i,
                    0
                );
            }

            return row;
        }
        // ============================================================
        // GRÁFICOS + ACCIONES
        // ============================================================

        private Control BuildCharts()
        {
            TableLayoutPanel row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };

            row.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    68
                )
            );

            row.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    32
                )
            );

            TableLayoutPanel left = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0, 0, 10, 10),
                Padding = new Padding(0)
            };

            left.RowStyles.Add(
                new RowStyle(
                    SizeType.Percent,
                    64
                )
            );

            left.RowStyles.Add(
                new RowStyle(
                    SizeType.Percent,
                    36
                )
            );

            ChartPanel trend = new ChartPanel
            {
                Dock = DockStyle.Fill,
                BackColor = card,
                Margin = new Padding(0, 0, 0, 10)
            };

            trend.Title = "TENDENCIAS DE ACTIVIDAD Y RETORNOS (SEMANAL)";
            trend.Subtitle = "";
            trend.Paint += PaintBars;

            Panel statusCard = BuildEquipmentStatusCard();

            left.Controls.Add(trend, 0, 0);
            left.Controls.Add(statusCard, 0, 1);

            Panel alerts = BuildWhatsAppAlertCard();

            row.Controls.Add(left, 0, 0);
            row.Controls.Add(alerts, 1, 0);

            return row;
        }

        // ============================================================
        // TABLA DETALLE
        // ============================================================


        private Panel BuildEquipmentStatusCard()
        {
            Card cardStatus = new Card
            {
                Dock = DockStyle.Fill,
                BackColor = card,
                Margin = new Padding(0)
            };

            Label title = LabelOf(
                "ESTADO GENERAL DE EQUIPOS",
                9.8f,
                Color.White,
                true
            );

            title.Location = new Point(16, 11);
            cardStatus.Controls.Add(title);

            Panel a = MiniDonut(
                "60%",
                "En Almacén",
                blue
            );

            Panel b = MiniDonut(
                "25%",
                "En Rodaje",
                amber
            );

            Panel c = MiniDonut(
                "15%",
                "En Mantenimiento",
                Color.FromArgb(130, 143, 158)
            );

            cardStatus.Controls.Add(a);
            cardStatus.Controls.Add(b);
            cardStatus.Controls.Add(c);

            cardStatus.Resize += (s, e) =>
            {
                int w =
                    Math.Max(
                        130,
                        (cardStatus.ClientSize.Width - 40) / 3
                    );

                a.SetBounds(
                    12,
                    35,
                    w,
                    74
                );

                b.SetBounds(
                    a.Right + 8,
                    35,
                    w,
                    74
                );

                c.SetBounds(
                    b.Right + 8,
                    35,
                    w,
                    74
                );
            };

            return cardStatus;
        }

        private Panel MiniDonut(
            string big,
            string small,
            Color color)
        {
            Panel p = new Panel
            {
                BackColor = Color.Transparent
            };

            p.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode =
                    SmoothingMode.AntiAlias;

                Rectangle r =
                    new Rectangle(
                        8,
                        8,
                        46,
                        46
                    );

                using (
                    Pen basePen =
                    new Pen(
                        Color.FromArgb(
                            49,
                            64,
                            84
                        ),
                        8
                    )
                )
                {
                    e.Graphics.DrawArc(
                        basePen,
                        r,
                        0,
                        360
                    );
                }

                using (
                    Pen accent =
                    new Pen(
                        color,
                        8
                    )
                )
                {
                    e.Graphics.DrawArc(
                        accent,
                        r,
                        -90,
                        220
                    );
                }

                using (
                    Brush fill =
                    new SolidBrush(
                        canvas
                    )
                )
                {
                    e.Graphics.FillEllipse(
                        fill,
                        22,
                        22,
                        18,
                        18
                    );
                }
            };

            Label lblBig =
                LabelOf(
                    big,
                    11.5f,
                    Color.White,
                    true
                );

            lblBig.Location =
                new Point(
                    65,
                    13
                );

            Label lblSmall =
                LabelOf(
                    small,
                    8.2f,
                    muted,
                    true
                );

            lblSmall.Location =
                new Point(
                    65,
                    34
                );

            p.Controls.Add(lblBig);
            p.Controls.Add(lblSmall);

            return p;
        }

        private Panel BuildWhatsAppAlertCard()
        {
            Card p = new Card
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(22, 40, 62),
                Margin = new Padding(0, 0, 0, 10)
            };

            Label title = LabelOf(
                "GESTIÓN DE ALERTAS DE WHATSAPP AUTOMÁTICAS",
                9.4f,
                Color.White,
                true
            );
            title.Location = new Point(16, 12);
            p.Controls.Add(title);

            Label general = LabelOf(
                "Estado General",
                8.8f,
                Color.FromArgb(233, 241, 250),
                true
            );
            general.Location = new Point(18, 48);

            Label generalState = LabelOf(
                whatsappGeneralEnabled ? "ON" : "OFF",
                8.1f,
                whatsappGeneralEnabled ? greenBright : Color.FromArgb(205, 216, 229),
                true
            );

            Panel switchGeneral = new Panel
            {
                Size = new Size(50, 26),
                BackColor = whatsappGeneralEnabled
                    ? Color.FromArgb(57, 211, 110)
                    : Color.FromArgb(76, 91, 111),
                Cursor = Cursors.Hand
            };
            Rounded(switchGeneral, 13);

            Panel knobGeneral = new Panel
            {
                Size = new Size(20, 20),
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            Rounded(knobGeneral, 10);
            switchGeneral.Controls.Add(knobGeneral);

            Label delay = LabelOf(
                "Activar Notificaciones de Retraso",
                8.8f,
                Color.FromArgb(233, 241, 250),
                true
            );
            delay.Location = new Point(18, 83);

            Label delayState = LabelOf(
                whatsappDelayEnabled ? "ON" : "OFF",
                8.1f,
                whatsappDelayEnabled ? greenBright : Color.FromArgb(205, 216, 229),
                true
            );

            Panel switchDelay = new Panel
            {
                Size = new Size(50, 26),
                BackColor = whatsappDelayEnabled
                    ? Color.FromArgb(57, 211, 110)
                    : Color.FromArgb(76, 91, 111),
                Cursor = Cursors.Hand
            };
            Rounded(switchDelay, 13);

            Panel knobDelay = new Panel
            {
                Size = new Size(20, 20),
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            Rounded(knobDelay, 10);
            switchDelay.Controls.Add(knobDelay);

            p.Controls.Add(general);
            p.Controls.Add(generalState);
            p.Controls.Add(delay);
            p.Controls.Add(delayState);
            p.Controls.Add(switchGeneral);
            p.Controls.Add(switchDelay);

            Panel gridHead = new Panel
            {
                BackColor = Color.FromArgb(29, 50, 76),
                Height = 32
            };
            Rounded(gridHead, 7);

            Label h1 = LabelOf("Regla", 7.8f, Color.White, true);
            Label h2 = LabelOf("Disparador", 7.8f, Color.White, true);
            Label h3 = LabelOf("Plantilla", 7.8f, Color.White, true);
            Label h4 = LabelOf("Estado", 7.8f, Color.White, true);
            gridHead.Controls.AddRange(new Control[] { h1, h2, h3, h4 });

            Panel row1 = BuildAlertRow("Alerta 1", "1 H antes", "Default", "Activa");
            Panel row2 = BuildAlertRow("Alerta 2", "Retraso", "Default", "Inactivo");

            p.Controls.Add(gridHead);
            p.Controls.Add(row1);
            p.Controls.Add(row2);

            Label preview = LabelOf(
                "Previsualización de Mensaje",
                8.6f,
                Color.FromArgb(231, 239, 248),
                true
            );
            p.Controls.Add(preview);

            Panel message = new Panel
            {
                BackColor = Color.FromArgb(28, 49, 74)
            };
            Rounded(message, 9);

            Label body = new Label
            {
                Text =
                    "[Artemusa Alerta] Hola, [Técnico],\n" +
                    "Tu equipo [Nombre Equipo] está retrasado.\n" +
                    "Por favor, devuélvelo al almacén.",
                ForeColor = Color.FromArgb(244, 248, 252),
                Font = FontOf(8.3f),
                BackColor = Color.Transparent,
                AutoEllipsis = true
            };
            message.Controls.Add(body);

            Button openWhatsApp = ButtonOf(
                "☏  Abrir WhatsApp",
                (s, e) => AbrirNotificacionWhatsApp(FindForm()),
                true,
                180
            );
            openWhatsApp.Height = 36;

            p.Controls.Add(message);
            p.Controls.Add(openWhatsApp);

            Action updateSwitchVisuals = () =>
            {
                switchGeneral.BackColor = whatsappGeneralEnabled
                    ? Color.FromArgb(57, 211, 110)
                    : Color.FromArgb(76, 91, 111);

                knobGeneral.Location = whatsappGeneralEnabled
                    ? new Point(27, 3)
                    : new Point(3, 3);

                generalState.Text = whatsappGeneralEnabled ? "ON" : "OFF";
                generalState.ForeColor = whatsappGeneralEnabled
                    ? greenBright
                    : Color.FromArgb(205, 216, 229);

                switchDelay.BackColor = whatsappDelayEnabled
                    ? Color.FromArgb(57, 211, 110)
                    : Color.FromArgb(76, 91, 111);

                knobDelay.Location = whatsappDelayEnabled
                    ? new Point(27, 3)
                    : new Point(3, 3);

                delayState.Text = whatsappDelayEnabled ? "ON" : "OFF";
                delayState.ForeColor = whatsappDelayEnabled
                    ? greenBright
                    : Color.FromArgb(205, 216, 229);

                SetAlertRowState(row1, whatsappGeneralEnabled);
                SetAlertRowState(row2, whatsappGeneralEnabled && whatsappDelayEnabled);

                body.Text = whatsappGeneralEnabled
                    ? (whatsappDelayEnabled
                        ? "[Artemusa Alerta] Hola, [Técnico],\nTu equipo [Nombre Equipo] tiene retraso activo.\nSe enviará una notificación automática."
                        : "[Artemusa Alerta] Hola, [Técnico],\nLas alertas generales están activas.\nActiva las notificaciones de retraso para avisos automáticos.")
                    : "Las alertas automáticas están desactivadas.\nPuedes seguir usando el botón Abrir WhatsApp para enviar un mensaje manual.";
            };

            EventHandler toggleGeneral = (s, e) =>
            {
                whatsappGeneralEnabled = !whatsappGeneralEnabled;
                if (!whatsappGeneralEnabled)
                    whatsappDelayEnabled = false;
                updateSwitchVisuals();
            };

            EventHandler toggleDelay = (s, e) =>
            {
                if (!whatsappGeneralEnabled)
                {
                    whatsappGeneralEnabled = true;
                }
                whatsappDelayEnabled = !whatsappDelayEnabled;
                updateSwitchVisuals();
            };

            switchGeneral.Click += toggleGeneral;
            knobGeneral.Click += toggleGeneral;
            general.Click += toggleGeneral;
            generalState.Click += toggleGeneral;

            switchDelay.Click += toggleDelay;
            knobDelay.Click += toggleDelay;
            delay.Click += toggleDelay;
            delayState.Click += toggleDelay;

            general.Cursor = Cursors.Hand;
            generalState.Cursor = Cursors.Hand;
            delay.Cursor = Cursors.Hand;
            delayState.Cursor = Cursors.Hand;

            p.Resize += (s, e) =>
            {
                int right = p.ClientSize.Width - 18;

                switchGeneral.Location = new Point(right - 50, 42);
                generalState.Location = new Point(switchGeneral.Left - 38, 48);

                switchDelay.Location = new Point(right - 50, 77);
                delayState.Location = new Point(switchDelay.Left - 38, 83);

                gridHead.SetBounds(16, 112, Math.Max(250, p.ClientSize.Width - 32), 32);

                int gridWidth = gridHead.Width;
                h1.Location = new Point(10, 9);
                h2.Location = new Point((int)(gridWidth * 0.23), 9);
                h3.Location = new Point((int)(gridWidth * 0.52), 9);
                h4.Location = new Point(Math.Max(10, gridWidth - 68), 9);

                row1.SetBounds(16, 146, gridWidth, 27);
                row2.SetBounds(16, 173, gridWidth, 27);

                preview.Location = new Point(16, 204);

                message.SetBounds(
                    16,
                    226,
                    Math.Max(250, p.ClientSize.Width - 32),
                    52
                );

                body.SetBounds(11, 6, message.Width - 22, 40);

                openWhatsApp.Width = Math.Max(180, p.ClientSize.Width - 32);
                openWhatsApp.Location = new Point(16, p.ClientSize.Height - 49);
            };

            updateSwitchVisuals();
            return p;
        }

        private Panel BuildAlertRow(
            string rule,
            string trigger,
            string template,
            string statusText)
        {
            Panel row = new Panel
            {
                BackColor = Color.Transparent
            };

            Label a = LabelOf(rule, 7.8f, Color.FromArgb(238, 244, 250));
            Label b = LabelOf(trigger, 7.8f, Color.FromArgb(238, 244, 250));
            Label c = LabelOf(template, 7.8f, Color.FromArgb(238, 244, 250));

            Panel badge = new Panel
            {
                Size = new Size(62, 20),
                BackColor = Color.FromArgb(31, 89, 56)
            };
            Rounded(badge, 10);

            Label status = new Label
            {
                Text = statusText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = FontOf(7.3f, true),
                ForeColor = Color.FromArgb(126, 255, 171),
                BackColor = Color.Transparent,
                Tag = "statusLabel"
            };

            badge.Tag = "statusBadge";
            badge.Controls.Add(status);

            row.Controls.Add(a);
            row.Controls.Add(b);
            row.Controls.Add(c);
            row.Controls.Add(badge);

            row.Resize += (s, e) =>
            {
                int w = row.ClientSize.Width;
                a.Location = new Point(10, 6);
                b.Location = new Point((int)(w * 0.23), 6);
                c.Location = new Point((int)(w * 0.52), 6);
                badge.Location = new Point(w - badge.Width - 6, 3);
            };

            return row;
        }

        private void SetAlertRowState(Panel row, bool active)
        {
            foreach (Control control in row.Controls)
            {
                Panel badge = control as Panel;
                if (badge == null || Convert.ToString(badge.Tag) != "statusBadge")
                    continue;

                badge.BackColor = active
                    ? Color.FromArgb(31, 89, 56)
                    : Color.FromArgb(72, 55, 57);

                foreach (Control inner in badge.Controls)
                {
                    Label label = inner as Label;
                    if (label == null || Convert.ToString(label.Tag) != "statusLabel")
                        continue;

                    label.Text = active ? "Activo" : "Inactivo";
                    label.ForeColor = active
                        ? Color.FromArgb(126, 255, 171)
                        : Color.FromArgb(255, 159, 159);
                }
            }
        }

        private void FillArea(
            Graphics graphics,
            PointF[] points,
            int bottom,
            Color color)
        {
            if (
                points == null
                ||
                points.Length < 2)
            {
                return;
            }

            List<PointF> polygon =
                new List<PointF>(
                    points
                );

            polygon.Add(
                new PointF(
                    points[
                        points.Length - 1
                    ].X,
                    bottom
                )
            );

            polygon.Add(
                new PointF(
                    points[0].X,
                    bottom
                )
            );

            using (
                GraphicsPath path =
                new GraphicsPath()
            )
            using (
                SolidBrush brush =
                new SolidBrush(
                    color
                )
            )
            {
                path.AddPolygon(
                    polygon.ToArray()
                );

                graphics.FillPath(
                    brush,
                    path
                );
            }
        }

        private void DrawSeries(
            Graphics graphics,
            PointF[] points,
            Color color)
        {
            using (
                Pen pen =
                new Pen(
                    color,
                    2.3f
                )
            )
            {
                pen.LineJoin =
                    LineJoin.Round;

                graphics.DrawLines(
                    pen,
                    points
                );
            }
        }

        private void DrawLegend(
            Graphics graphics,
            float x,
            float y,
            Color color,
            string text,
            Font font)
        {
            using (
                SolidBrush brush =
                new SolidBrush(
                    color
                )
            )
            {
                graphics.FillRectangle(
                    brush,
                    x,
                    y + 5,
                    16,
                    4
                );
            }

            DrawChartText(
                graphics,
                text,
                font,
                muted,
                x + 22,
                y
            );
        }



        private Control BuildTable()
        {
            Panel p = new Card
            {
                Dock = DockStyle.Fill,
                BackColor = card,
                Padding = new Padding(12, 44, 12, 8),
                Margin = new Padding(0)
            };

            Label title = LabelOf(
                "MOVIMIENTOS DE EQUIPOS Y ESTADO",
                10.5f,
                Color.White,
                true
            );

            title.Location = new Point(16, 12);

            p.Controls.Add(title);

            detailGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = card,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                GridColor = grid,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersHeight = 38,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowTemplate = { Height = 40 }
            };

            detailGrid.ColumnHeadersDefaultCellStyle =
                new DataGridViewCellStyle
                {
                    BackColor = header,
                    ForeColor = textMutedForGrid(),
                    Font = FontOf(8.2f, true),
                    Padding = new Padding(8, 0, 0, 0),
                    SelectionBackColor = header,
                    SelectionForeColor = textMutedForGrid()
                };

            detailGrid.DefaultCellStyle =
                new DataGridViewCellStyle
                {
                    BackColor = card,
                    ForeColor = ink,
                    Font = FontOf(8.8f),
                    Padding = new Padding(8, 0, 3, 0),
                    SelectionBackColor = Color.FromArgb(42, 55, 75),
                    SelectionForeColor = Color.White
                };

            detailGrid.AlternatingRowsDefaultCellStyle =
                new DataGridViewCellStyle
                {
                    BackColor = card2,
                    ForeColor = ink,
                    SelectionBackColor = Color.FromArgb(58, 56, 57),
                    SelectionForeColor = Color.White
                };

            detailGrid.CellFormatting += FormatGrid;

            p.Controls.Add(detailGrid);

            resultCount = LabelOf(
                "0 registros visibles",
                8.4f,
                muted
            );

            resultCount.AutoSize = false;
            resultCount.Height = 24;
            resultCount.Dock = DockStyle.Bottom;
            resultCount.TextAlign = ContentAlignment.MiddleLeft;

            p.Controls.Add(resultCount);

            search.TextChanged +=
                (s, e) => ApplySearch();

            return p;
        }

        private Color textMutedForGrid()
        {
            return Color.FromArgb(169, 184, 204);
        }

        // ============================================================
        // LÓGICA ORIGINAL - CONEXIÓN Y DATOS
        // ============================================================

        private NpgsqlConnection OpenConnection()
        {
            NpgsqlConnection c =
                new global::SUSTENTACION.Conexion()
                    .ObtenerConexion();

            if (c == null)
                throw new InvalidOperationException(
                    "La conexión existente devolvió null."
                );

            if (c.State != ConnectionState.Open)
                c.Open();

            return c;
        }

        private bool TableExists(NpgsqlConnection c)
        {
            using (
                NpgsqlCommand q =
                new NpgsqlCommand(
                    "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=@name",
                    c
                )
            )
            {
                q.Parameters.AddWithValue(
                    "@name",
                    HistoryTable
                );

                return Convert.ToInt32(
                    q.ExecuteScalar()
                ) > 0;
            }
        }

        private void LoadReport()
        {
            if (loading)
                return;

            if (
                fromDate.Value.Date
                >
                toDate.Value.Date)
            {
                Info(
                    "La fecha inicial no puede ser posterior a la fecha final."
                );

                return;
            }

            loading = true;

            try
            {
                using (
                    NpgsqlConnection c =
                    OpenConnection())
                {
                    if (!TableExists(c))
                    {
                        SetData(
                            new DataTable()
                        );

                        state.Text =
                            "●  Sin movimientos en el historial todavía.";

                        state.ForeColor =
                            muted;

                        return;
                    }

                    string sql =
                        @"SELECT id AS Registro,equipo_id AS Código,equipo_nombre AS Equipo,tipo AS Tipo,cantidad AS Cantidad,
responsable AS Responsable,motivo AS Motivo,fecha AS Fecha FROM artemusa_movimientos_v1 WHERE fecha>=@from AND fecha<@to";

                    if (
                        typeFilter.SelectedIndex
                        >
                        0)
                    {
                        sql +=
                            " AND tipo=@type";
                    }

                    if (
                        responsibleFilter
                            .SelectedIndex
                        >
                        0)
                    {
                        sql +=
                            " AND responsable=@responsible";
                    }

                    sql +=
                        " ORDER BY fecha DESC,id DESC LIMIT 1000";

                    using (
                        NpgsqlCommand command =
                        new NpgsqlCommand(
                            sql,
                            c
                        )
                    )
                    {
                        command.Parameters
                            .AddWithValue(
                                "@from",
                                fromDate.Value.Date
                            );

                        command.Parameters
                            .AddWithValue(
                                "@to",
                                toDate.Value.Date
                                    .AddDays(1)
                            );

                        if (
                            typeFilter
                                .SelectedIndex
                            >
                            0)
                        {
                            command.Parameters
                                .AddWithValue(
                                    "@type",
                                    typeFilter
                                        .SelectedItem
                                        .ToString()
                                );
                        }

                        if (
                            responsibleFilter
                                .SelectedIndex
                            >
                            0)
                        {
                            command.Parameters
                                .AddWithValue(
                                    "@responsible",
                                    responsibleFilter
                                        .SelectedItem
                                        .ToString()
                                );
                        }

                        DataTable d =
                            new DataTable();

                        using (
                            NpgsqlDataAdapter a =
                            new NpgsqlDataAdapter(
                                command
                            )
                        )
                        {
                            a.Fill(d);
                        }

                        SetData(d);
                    }

                    using (
                        NpgsqlCommand query =
                        new NpgsqlCommand(
                            "SELECT DISTINCT responsable FROM artemusa_movimientos_v1 WHERE responsable<>'' ORDER BY responsable",
                            c
                        )
                    )
                    {
                        string keep =
                            Convert.ToString(
                                responsibleFilter
                                    .SelectedItem
                            );

                        responsibleFilter
                            .Items
                            .Clear();

                        responsibleFilter
                            .Items
                            .Add("Todos");

                        using (
                            NpgsqlDataReader r =
                            query.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                responsibleFilter
                                    .Items
                                    .Add(
                                        Convert.ToString(
                                            r[0]
                                        )
                                    );
                            }
                        }

                        int index =
                            responsibleFilter
                                .Items
                                .IndexOf(keep);

                        responsibleFilter
                            .SelectedIndex =
                            index >= 0
                            ? index
                            : 0;
                    }
                }

                state.Text =
                    "●  Conectado · Reporte actualizado "
                    +
                    DateTime.Now
                        .ToString("HH:mm:ss");

                state.ForeColor =
                    greenBright;
            }
            catch (Exception ex)
            {
                SetData(
                    new DataTable()
                );

                state.Text =
                    "No se pudo cargar el reporte. Revisa la conexión y pulsa Aplicar filtros.";

                state.ForeColor =
                    red;

                Error(
                    "Reporte no disponible",
                    ex
                );
            }
            finally
            {
                loading = false;
                ApplySearch();
            }
        }

        private void SetData(DataTable data)
        {
            if (reportData != null)
                reportData.Dispose();

            reportData = data;
            detailGrid.DataSource = data;

            if (data.Columns.Count > 0)
            {
                foreach (
                    DataGridViewColumn c
                    in detailGrid.Columns)
                {
                    c.MinimumWidth = 110;
                }

                if (
                    detailGrid.Columns
                        .Contains("Equipo"))
                {
                    detailGrid.Columns["Equipo"]
                        .MinimumWidth = 220;
                }

                if (
                    detailGrid.Columns
                        .Contains("Motivo"))
                {
                    detailGrid.Columns["Motivo"]
                        .MinimumWidth = 180;
                }

                if (
                    detailGrid.Columns
                        .Contains("Fecha"))
                {
                    detailGrid.Columns["Fecha"]
                        .DefaultCellStyle
                        .Format =
                        "dd/MM/yyyy HH:mm";
                }
            }

            UpdateMetrics();
        }

        private void ApplySearch()
        {
            if (reportData == null)
                return;

            string text =
                search == null
                ? ""
                : search.Text.Trim();

            if (text.Length == 0)
            {
                reportData
                    .DefaultView
                    .RowFilter =
                    "";
            }
            else
            {
                string safe =
                    Escape(text);

                List<string> terms =
                    new List<string>();

                foreach (
                    string col
                    in new[]
                    {
                        "Código",
                        "Equipo",
                        "Tipo",
                        "Responsable",
                        "Motivo"
                    })
                {
                    if (
                        reportData
                            .Columns
                            .Contains(col))
                    {
                        terms.Add(
                            "["
                            + col
                            + "] LIKE '%"
                            + safe
                            + "%'"
                        );
                    }
                }

                if (terms.Count > 0)
                {
                    reportData
                        .DefaultView
                        .RowFilter =
                        "("
                        +
                        string.Join(
                            " OR ",
                            terms
                        )
                        +
                        ")";
                }
                else
                {
                    reportData
                        .DefaultView
                        .RowFilter =
                        "";
                }
            }

            resultCount.Text =
                reportData
                    .DefaultView
                    .Count
                    .ToString("N0")
                +
                " registros visibles";
        }

        private static string Escape(string s)
        {
            StringBuilder b =
                new StringBuilder();

            foreach (char c in s)
            {
                if (c == '\'')
                    b.Append("''");
                else if (c == '[')
                    b.Append("[[]");
                else if (c == ']')
                    b.Append("[]]");
                else if (c == '%')
                    b.Append("[%]");
                else if (c == '*')
                    b.Append("[*]");
                else
                    b.Append(c);
            }

            return b.ToString();
        }

        private void UpdateMetrics()
        {
            if (values[0] == null)
                return;

            if (reportData == null)
            {
                foreach (Label l in values)
                    l.Text = "—";

                return;
            }

            long entries = 0;
            long exits = 0;

            foreach (
                DataRowView row
                in reportData.DefaultView)
            {
                long n =
                    Convert.ToInt64(
                        row["Cantidad"]
                    );

                if (
                    Convert.ToString(
                        row["Tipo"]
                    )
                    ==
                    "Entrada")
                {
                    entries += n;
                }
                else if (
                    Convert.ToString(
                        row["Tipo"]
                    )
                    ==
                    "Salida")
                {
                    exits += n;
                }
            }

            values[0].Text =
                entries.ToString("N0");

            values[1].Text =
                exits.ToString("N0");

            try
            {
                using (
                    NpgsqlConnection c =
                    OpenConnection())
                {
                    using (
                        NpgsqlCommand q =
                        new NpgsqlCommand(
                            "SELECT COUNT(*) FROM equipos WHERE stock<=5",
                            c
                        )
                    )
                    {
                        values[2].Text =
                            Convert.ToString(
                                q.ExecuteScalar()
                            );
                    }

                    using (
                        NpgsqlCommand q =
                        new NpgsqlCommand(
                            "SELECT COALESCE(SUM(stock*precio),0) FROM equipos",
                            c
                        )
                    )
                    {
                        values[3].Text =
                            "S/ "
                            +
                            Convert.ToDecimal(
                                q.ExecuteScalar()
                            )
                            .ToString("N2");
                    }
                }
            }
            catch
            {
                values[2].Text = "—";
                values[3].Text = "—";
            }
        }

        // ============================================================
        // FORMATO TABLA
        // ============================================================

        private void FormatGrid(
            object sender,
            DataGridViewCellFormattingEventArgs e)
        {
            if (
                e.RowIndex < 0
                ||
                e.ColumnIndex < 0)
            {
                return;
            }

            if (
                detailGrid
                    .Columns[e.ColumnIndex]
                    .Name
                ==
                "Tipo")
            {
                string t =
                    Convert.ToString(
                        e.Value
                    );

                e.CellStyle.ForeColor =
                    t == "Entrada"
                    ? greenBright
                    : amber;

                e.CellStyle.Font =
                    FontOf(
                        8.8f,
                        true
                    );
            }

            if (
                detailGrid
                    .Columns[e.ColumnIndex]
                    .Name
                ==
                "Cantidad")
            {
                e.CellStyle.Alignment =
                    DataGridViewContentAlignment
                        .MiddleRight;
            }
        }

        // ============================================================
        // GRÁFICOS - SOLO DISEÑO
        // ============================================================

        private static void DrawChartText(
            Graphics graphics,
            string text,
            Font font,
            Color color,
            float x,
            float y)
        {
            using (
                Brush brush =
                new SolidBrush(color))
            {
                graphics.DrawString(
                    text,
                    font,
                    brush,
                    x,
                    y
                );
            }
        }

        private void PaintBars(
            object sender,
            PaintEventArgs e)
        {
            ChartPanel p =
                (ChartPanel)sender;

            e.Graphics.SmoothingMode =
                SmoothingMode.AntiAlias;

            int left = 44;
            int right = p.Width - 18;
            int top = 78;
            int bottom = p.Height - 30;

            using (
                Pen gridPen =
                new Pen(
                    Color.FromArgb(
                        36,
                        52,
                        74
                    ),
                    1
                )
            )
            {
                for (int i = 0; i < 5; i++)
                {
                    int y =
                        top
                        +
                        i
                        *
                        Math.Max(
                            22,
                            (bottom - top) / 4
                        );

                    e.Graphics.DrawLine(
                        gridPen,
                        left,
                        y,
                        right,
                        y
                    );
                }
            }

            string[] days =
            {
                "Lun",
                "Mar",
                "Mie",
                "Jue",
                "Vie",
                "Sáb",
                "Dom"
            };

            float step =
                (right - left) / 6f;

            using (
                Font axisFont =
                new Font(
                    "Segoe UI",
                    7.4f
                )
            )
            {
                for (int i = 0; i < days.Length; i++)
                {
                    SizeF size =
                        e.Graphics.MeasureString(
                            days[i],
                            axisFont
                        );

                    DrawChartText(
                        e.Graphics,
                        days[i],
                        axisFont,
                        muted,
                        left
                        +
                        i * step
                        -
                        size.Width / 2f,
                        bottom + 5
                    );
                }
            }

            PointF[] loan =
            {
                new PointF(left + 0 * step, bottom - 125),
                new PointF(left + 1 * step, bottom - 88),
                new PointF(left + 2 * step, bottom - 102),
                new PointF(left + 3 * step, bottom - 68),
                new PointF(left + 4 * step, bottom - 42),
                new PointF(left + 5 * step, bottom - 96),
                new PointF(left + 6 * step, bottom - 24)
            };

            PointF[] onTime =
            {
                new PointF(left + 0 * step, bottom - 76),
                new PointF(left + 1 * step, bottom - 48),
                new PointF(left + 2 * step, bottom - 70),
                new PointF(left + 3 * step, bottom - 64),
                new PointF(left + 4 * step, bottom - 38),
                new PointF(left + 5 * step, bottom - 70),
                new PointF(left + 6 * step, bottom - 34)
            };

            PointF[] late =
            {
                new PointF(left + 0 * step, bottom - 22),
                new PointF(left + 1 * step, bottom - 36),
                new PointF(left + 2 * step, bottom - 20),
                new PointF(left + 3 * step, bottom - 24),
                new PointF(left + 4 * step, bottom - 40),
                new PointF(left + 5 * step, bottom - 14),
                new PointF(left + 6 * step, bottom - 32)
            };

            FillArea(
                e.Graphics,
                loan,
                bottom,
                Color.FromArgb(70, blue)
            );

            FillArea(
                e.Graphics,
                onTime,
                bottom,
                Color.FromArgb(52, amber)
            );

            FillArea(
                e.Graphics,
                late,
                bottom,
                Color.FromArgb(35, red)
            );

            DrawSeries(
                e.Graphics,
                loan,
                blue
            );

            DrawSeries(
                e.Graphics,
                onTime,
                amber
            );

            DrawSeries(
                e.Graphics,
                late,
                red
            );

            using (
                Font legendFont =
                new Font(
                    "Segoe UI",
                    7.7f
                )
            )
            {
                DrawLegend(
                    e.Graphics,
                    left + 72,
                    46,
                    blue,
                    "Préstamos",
                    legendFont
                );

                DrawLegend(
                    e.Graphics,
                    left + 190,
                    46,
                    amber,
                    "Retornos a Tiempo",
                    legendFont
                );

                DrawLegend(
                    e.Graphics,
                    left + 350,
                    46,
                    red,
                    "Retornos con Atraso",
                    legendFont
                );
            }
        }

        private void PaintDoughnut(
            object sender,
            PaintEventArgs e)
        {
            ChartPanel p =
                (ChartPanel)sender;

            e.Graphics.SmoothingMode =
                SmoothingMode.AntiAlias;

            int size =
                Math.Min(
                    120,
                    Math.Max(
                        78,
                        p.Width - 60
                    )
                );

            Rectangle r =
                new Rectangle(
                    p.Width / 2
                    -
                    size / 2,
                    72,
                    size,
                    size
                );

            using (
                Pen basePen =
                new Pen(
                    Color.FromArgb(
                        49,
                        64,
                        84
                    ),
                    18
                )
            )
            {
                e.Graphics.DrawArc(
                    basePen,
                    r,
                    0,
                    360
                );
            }

            using (
                Pen greenPen =
                new Pen(
                    greenBright,
                    18
                )
            )
            {
                e.Graphics.DrawArc(
                    greenPen,
                    r,
                    -90,
                    205
                );
            }

            using (
                Pen amberPen =
                new Pen(
                    amber,
                    18
                )
            )
            {
                e.Graphics.DrawArc(
                    amberPen,
                    r,
                    115,
                    155
                );
            }
        }

        // ============================================================
        // EXPORTACIÓN - LÓGICA ORIGINAL
        // ============================================================

        private void ExportCsv()
        {
            if (
                reportData == null
                ||
                reportData.DefaultView.Count == 0)
            {
                Info(
                    "No hay registros visibles para exportar."
                );

                return;
            }

            using (
                SaveFileDialog d =
                new SaveFileDialog
                {
                    Filter =
                        "Archivo CSV (*.csv)|*.csv",

                    FileName =
                        "reporte_movimientos_"
                        +
                        DateTime.Now
                            .ToString(
                                "yyyyMMdd_HHmm"
                            )
                        +
                        ".csv"
                }
            )
            {
                if (
                    d.ShowDialog(
                        FindForm()
                    )
                    !=
                    DialogResult.OK)
                {
                    return;
                }

                using (
                    StreamWriter w =
                    new StreamWriter(
                        d.FileName,
                        false,
                        new UTF8Encoding(true)
                    )
                )
                {
                    List<string> heads =
                        new List<string>();

                    foreach (
                        DataColumn c
                        in reportData.Columns)
                    {
                        heads.Add(
                            Csv(
                                c.ColumnName
                            )
                        );
                    }

                    w.WriteLine(
                        string.Join(
                            ";",
                            heads
                        )
                    );

                    foreach (
                        DataRowView row
                        in reportData
                            .DefaultView)
                    {
                        List<string> cells =
                            new List<string>();

                        foreach (
                            DataColumn c
                            in reportData.Columns)
                        {
                            cells.Add(
                                Csv(
                                    Convert.ToString(
                                        row[
                                            c.ColumnName
                                        ],
                                        CultureInfo
                                            .CurrentCulture
                                    )
                                )
                            );
                        }

                        w.WriteLine(
                            string.Join(
                                ";",
                                cells
                            )
                        );
                    }
                }

                Info(
                    "Reporte exportado correctamente."
                );
            }
        }

        private static string Csv(
            string value)
        {
            value = value ?? "";

            return "\""
                +
                value.Replace(
                    "\"",
                    "\"\""
                )
                +
                "\"";
        }

        private void Info(string text)
        {
            MessageBox.Show(
                FindForm(),
                text,
                "Artemusa",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void Error(
            string title,
            Exception ex)
        {
            MessageBox.Show(
                FindForm(),
                title
                +
                "\n\n"
                +
                ex.Message,
                "Artemusa",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }

        // ============================================================
        // WHATSAPP - LÓGICA ORIGINAL
        // ============================================================

        public static void AbrirNotificacionWhatsApp(
            IWin32Window owner)
        {
            using (
                WhatsAppDialog dialog =
                new WhatsAppDialog())
            {
                dialog.ShowDialog(owner);
            }
        }

        private sealed class WhatsAppDialog : Form
        {
            private readonly Color dark =
                Color.FromArgb(12, 24, 31);

            private readonly Color dialogCard =
                Color.FromArgb(24, 40, 48);

            private readonly Color lime =
                Color.FromArgb(163, 230, 53);

            private readonly Color soft =
                Color.FromArgb(190, 207, 214);

            private TextBox message;
            private Button send;
            private Label feedback;

            public WhatsAppDialog()
            {
                Text =
                    "Notificación WhatsApp · Artemusa";

                ClientSize =
                    new Size(
                        560,
                        430
                    );

                StartPosition =
                    FormStartPosition
                        .CenterParent;

                FormBorderStyle =
                    FormBorderStyle
                        .FixedDialog;

                MaximizeBox = false;
                MinimizeBox = false;
                ShowInTaskbar = false;

                BackColor = dark;

                Font =
                    new Font(
                        "Segoe UI",
                        9f
                    );

                Build();
            }

            private void Build()
            {
                Label title =
                    new Label
                    {
                        Text =
                            "Enviar notificación",

                        AutoSize = true,

                        Font =
                            new Font(
                                "Segoe UI",
                                19f,
                                FontStyle.Bold
                            ),

                        ForeColor =
                            Color.White,

                        Location =
                            new Point(
                                28,
                                24
                            )
                    };

                Label subtitle =
                    new Label
                    {
                        Text =
                            "Redacta el mensaje que recibirá el número registrado en Artemusa.",

                        AutoSize = true,

                        ForeColor =
                            soft,

                        Location =
                            new Point(
                                30,
                                62
                            )
                    };

                Label number =
                    new Label
                    {
                        Text =
                            "Destino: +51 961 256 566",

                        AutoSize = true,

                        ForeColor =
                            lime,

                        Font =
                            new Font(
                                "Segoe UI",
                                9f,
                                FontStyle.Bold
                            ),

                        Location =
                            new Point(
                                30,
                                91
                            )
                    };

                message =
                    new TextBox
                    {
                        Multiline = true,

                        ScrollBars =
                            ScrollBars.Vertical,

                        AcceptsReturn = true,

                        Location =
                            new Point(
                                28,
                                125
                            ),

                        Size =
                            new Size(
                                504,
                                150
                            ),

                        BackColor =
                            dialogCard,

                        ForeColor =
                            Color.White,

                        BorderStyle =
                            BorderStyle.FixedSingle,

                        Font =
                            new Font(
                                "Segoe UI",
                                10f
                            ),

                        MaxLength =
                            2000,

                        Text =
                            "Hola, te informamos que hay una actualización en el estado de tu inventario en Artemusa."
                    };

                feedback =
                    new Label
                    {
                        AutoSize = false,

                        Size =
                            new Size(
                                504,
                                32
                            ),

                        Location =
                            new Point(
                                28,
                                287
                            ),

                        ForeColor =
                            soft,

                        Text =
                            "Máximo 2.000 caracteres.",

                        AutoEllipsis =
                            true
                    };

                Button cancel =
                    new Button
                    {
                        Text =
                            "Cancelar",

                        Size =
                            new Size(
                                116,
                                40
                            ),

                        Location =
                            new Point(
                                280,
                                350
                            ),

                        BackColor =
                            dialogCard,

                        ForeColor =
                            Color.White,

                        FlatStyle =
                            FlatStyle.Flat,

                        Cursor =
                            Cursors.Hand
                    };

                cancel
                    .FlatAppearance
                    .BorderColor =
                    Color.FromArgb(
                        58,
                        80,
                        89
                    );

                cancel.Click +=
                    (s, e) =>
                        DialogResult =
                            DialogResult.Cancel;

                send =
                    new Button
                    {
                        Text =
                            "Enviar mensaje",

                        Size =
                            new Size(
                                146,
                                40
                            ),

                        Location =
                            new Point(
                                386,
                                350
                            ),

                        BackColor =
                            lime,

                        ForeColor =
                            Color.FromArgb(
                                11,
                                22,
                                25
                            ),

                        FlatStyle =
                            FlatStyle.Flat,

                        Font =
                            new Font(
                                "Segoe UI",
                                9f,
                                FontStyle.Bold
                            ),

                        Cursor =
                            Cursors.Hand,

                        UseVisualStyleBackColor =
                            false
                    };

                send
                    .FlatAppearance
                    .BorderSize = 0;

                send.Click +=
                    SendAsync;

                Controls.AddRange(
                    new Control[]
                    {
                        title,
                        subtitle,
                        number,
                        message,
                        feedback,
                        cancel,
                        send
                    }
                );

                CancelButton = cancel;
                AcceptButton = send;
            }

            private async void SendAsync(
                object sender,
                EventArgs e)
            {
                string text =
                    message.Text.Trim();

                if (text.Length == 0)
                {
                    feedback.Text =
                        "Escribe un mensaje antes de enviarlo.";

                    feedback.ForeColor =
                        Color.FromArgb(
                            255,
                            164,
                            164
                        );

                    message.Focus();

                    return;
                }

                send.Enabled =
                    false;

                feedback.ForeColor =
                    lime;

                feedback.Text =
                    "Enviando mensaje…";

                try
                {
                    bool sent =
                        await
                        global::
                        SUSTENTACION
                        .PanelAdministrador
                        .GestionEquipos
                        .EnviarNotificacionWhatsAppAsync(
                            text
                        );

                    if (sent)
                    {
                        feedback.Text =
                            "Mensaje enviado correctamente.";

                        MessageBox.Show(
                            this,
                            "Mensaje enviado con éxito a WhatsApp.",
                            "Artemusa",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );

                        DialogResult =
                            DialogResult.OK;
                    }
                    else
                    {
                        feedback.ForeColor =
                            Color.FromArgb(
                                255,
                                194,
                                116
                            );

                        feedback.Text =
                            "El servicio no confirmó el envío.";
                    }
                }
                catch (Exception ex)
                {
                    feedback.ForeColor =
                        Color.FromArgb(
                            255,
                            164,
                            164
                        );

                    feedback.Text =
                        "No se pudo enviar el mensaje.";

                    MessageBox.Show(
                        this,
                        ex.Message,
                        "Error de WhatsApp",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }
                finally
                {
                    send.Enabled =
                        true;
                }
            }
        }

        // ============================================================
        // CONTROLES VISUALES INTERNOS
        // ============================================================

        private sealed class ChartPanel : Card
        {
            [Browsable(false)]
            [DesignerSerializationVisibility(
                DesignerSerializationVisibility.Hidden)]
            public string Title { get; set; }

            [Browsable(false)]
            [DesignerSerializationVisibility(
                DesignerSerializationVisibility.Hidden)]
            public string Subtitle { get; set; }

            protected override void OnPaint(
                PaintEventArgs e)
            {
                base.OnPaint(e);

                using (
                    Font titleFont =
                    FontOfStatic(
                        11.5f,
                        true
                    )
                )
                using (
                    Font subtitleFont =
                    FontOfStatic(
                        8f,
                        false
                    )
                )
                using (
                    Brush titleBrush =
                    new SolidBrush(
                        Color.FromArgb(
                            239,
                            244,
                            250
                        )
                    )
                )
                using (
                    Brush subtitleBrush =
                    new SolidBrush(
                        Color.FromArgb(
                            156,
                            174,
                            198
                        )
                    )
                )
                {
                    e.Graphics.DrawString(
                        Title ?? "",
                        titleFont,
                        titleBrush,
                        16,
                        12
                    );

                    e.Graphics.DrawString(
                        Subtitle ?? "",
                        subtitleFont,
                        subtitleBrush,
                        17,
                        35
                    );
                }
            }

            private static Font FontOfStatic(
                float size,
                bool bold)
            {
                return new Font(
                    "Segoe UI",
                    size,
                    bold
                    ? FontStyle.Bold
                    : FontStyle.Regular
                );
            }
        }

        private class Card : Panel
        {
            public Card()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
            }

            protected override void OnPaint(
                PaintEventArgs e)
            {
                base.OnPaint(e);

                if (
                    Width < 20
                    ||
                    Height < 20)
                {
                    return;
                }

                Rectangle rect =
                    new Rectangle(
                        0,
                        0,
                        Width - 1,
                        Height - 1
                    );

                using (
                    GraphicsPath path =
                    RoundPath(
                        rect,
                        10
                    )
                )
                using (
                    Pen p =
                    new Pen(
                        Color.FromArgb(
                            61,
                            82,
                            109
                        ),
                        1
                    )
                )
                {
                    e.Graphics.SmoothingMode =
                        SmoothingMode.AntiAlias;

                    e.Graphics.DrawPath(
                        p,
                        path
                    );
                }
            }
        }
    }
}
