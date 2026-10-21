#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Npgsql;

namespace SUSTENTACION.PanelAdministrador
{
    public class CotrolReporte : UserControl
    {
        // ============================================================
        // PALETA (ESTILO CLARO / WEB MODERNO)
        // ============================================================
        private readonly Color canvas = Color.FromArgb(244, 248, 250);
        private readonly Color surface = Color.White;

        private readonly Color ink = Color.FromArgb(30, 41, 59);
        private readonly Color muted = Color.FromArgb(100, 116, 139);

        private readonly Color green = Color.FromArgb(34, 197, 94);
        private readonly Color greenBg = Color.FromArgb(220, 252, 231);
        private readonly Color blue = Color.FromArgb(99, 102, 241);
        private readonly Color amber = Color.FromArgb(245, 158, 11);
        private readonly Color amberBg = Color.FromArgb(254, 243, 199);
        private readonly Color red = Color.FromArgb(239, 68, 68);
        private readonly Color redBg = Color.FromArgb(254, 226, 226);
        private readonly Color grayBar = Color.FromArgb(209, 213, 219);

        private readonly Color border = Color.FromArgb(226, 232, 240);
        private readonly Color grid = Color.FromArgb(241, 245, 249);

        private readonly List<Font> fonts = new List<Font>();

        private TextBox search;
        private ComboBox stateFilter;
        private Button applyButton;
        private Button exportButton;

        private ComboBox cmbPageSize;
        private Label lblPagination;

        private Label[] values = new Label[4];
        private Label state;
        private DataGridView detailGrid;
        private DataTable reportData;
        private bool loading;

        // Referencias a los gráficos
        private OverviewBarChart chartBarsRef;
        private OverviewDonutChart chartDonutRef;

        // Variables para los filtros de fecha
        private Button btnRangoFechas;
        private Panel panelDropDownFechas;
        private DateTime fechaDesdeActual = DateTime.Today.AddDays(-30);
        private DateTime fechaHastaActual = DateTime.Today;

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
        // HELPERS
        // ============================================================
        private Font FontOf(float size, bool bold = false)
        {
            Font f = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point);
            fonts.Add(f);
            return f;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (reportData != null) reportData.Dispose();
                foreach (Font f in fonts) f.Dispose();
            }
        }

        private Label LabelOf(string text, float size, Color color, bool bold = false)
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

        private Button ButtonOf(string text, EventHandler click, bool primary = false, int width = 128)
        {
            Button b = new Button
            {
                Text = text,
                Width = width,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                BackColor = primary ? blue : Color.White,
                ForeColor = primary ? Color.White : ink,
                Font = FontOf(9f, true),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                Margin = new Padding(0, 0, 10, 0)
            };

            b.FlatAppearance.BorderSize = primary ? 0 : 1;
            b.FlatAppearance.BorderColor = border;
            b.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(79, 70, 229) : Color.FromArgb(241, 245, 249);

            b.Click += click;
            Rounded(b, 8);
            return b;
        }

        private ComboBox ComboOf(params string[] items)
        {
            ComboBox c = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = ink,
                Font = FontOf(9),
                Width = 160
            };

            c.Items.AddRange(items);
            if (items.Length > 0) c.SelectedIndex = 0;
            return c;
        }

        private void Rounded(Control control, int radius)
        {
            Action apply = () =>
            {
                if (control.Width <= 2 || control.Height <= 2) return;
                Rectangle rect = new Rectangle(0, 0, control.Width - 1, control.Height - 1);
                using (GraphicsPath path = RoundPath(rect, radius))
                {
                    control.Region?.Dispose();
                    control.Region = new Region(path);
                }
            };

            control.Resize += (s, e) => apply();
            apply();
        }

        private static GraphicsPath RoundPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (rect.Width < 4 || rect.Height < 4)
            {
                path.AddRectangle(rect);
                return path;
            }

            int r = Math.Max(1, Math.Min(radius, Math.Min(rect.Width / 2, rect.Height / 2)));
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

            Panel page = new Panel { BackColor = canvas, Padding = new Padding(24) };
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

            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));   // Header
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));   // Filtros de Tiempo
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));  // KPIs
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 380));  // Gráficos
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 450));  // Tabla
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));   // Footer

            page.Controls.Add(main);

            main.Controls.Add(BuildHeader(), 0, 0);
            main.Controls.Add(BuildTopFilters(), 0, 1);
            main.Controls.Add(BuildKpis(), 0, 2);
            main.Controls.Add(BuildChartsLayout(), 0, 3);
            main.Controls.Add(BuildTableWithFilters(), 0, 4);

            state = LabelOf("Conectando con el servidor…", 8.5f, muted);
            state.AutoSize = false;
            state.Dock = DockStyle.Fill;
            state.TextAlign = ContentAlignment.MiddleLeft;
            main.Controls.Add(state, 0, 5);

            Action resize = () =>
            {
                page.Size = new Size(
                    Math.Max(1140, ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 2),
                    Math.Max(1200, ClientSize.Height - 2));
            };

            Resize += (s, e) => resize();
            resize();

            ResumeLayout(true);
        }

        private Control BuildHeader()
        {
            Panel p = new Panel { Dock = DockStyle.Fill, BackColor = canvas, Margin = new Padding(0) };

            Label crumb = LabelOf("GESTIÓN Y ANÁLISIS", 8f, muted, true);
            crumb.Location = new Point(2, 1);

            Label title = LabelOf("Reportes de equipos", 22f, ink, true);
            title.Location = new Point(0, 18);

            Label sub = LabelOf("Monitorea el rendimiento, disponibilidad y estado de tus equipos audiovisuales.",
                9f, muted);
            sub.Location = new Point(2, 58);

            exportButton = ButtonOf("⬇  Exportar reporte", (s, e) => ExportCsv(), false, 150);
            exportButton.Height = 38;
            exportButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            exportButton.Location = new Point(p.Width - exportButton.Width - 160, 20);

            applyButton = ButtonOf("+  Actualizar", (s, e) => LoadReport(), true, 130);
            applyButton.Height = 38;
            applyButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            applyButton.Location = new Point(p.Width - applyButton.Width, 20);

            p.Controls.Add(crumb);
            p.Controls.Add(title);
            p.Controls.Add(sub);
            p.Controls.Add(exportButton);
            p.Controls.Add(applyButton);

            p.Resize += (s, e) =>
            {
                applyButton.Location = new Point(p.Width - applyButton.Width, 20);
                exportButton.Location = new Point(applyButton.Left - exportButton.Width - 10, 20);
            };

            return p;
        }

        private Control BuildTopFilters()
        {
            Panel p = new Panel { Dock = DockStyle.Fill, BackColor = canvas, Margin = new Padding(0, 0, 0, 15) };

            Panel tabsPanel = new Panel
            {
                Size = new Size(360, 40),
                Location = new Point(0, 5),
                BackColor = Color.FromArgb(241, 245, 249)
            };
            Rounded(tabsPanel, 8);

            Button btn7 = CrearBotonTab("7 días", 6);
            Button btn30 = CrearBotonTab("30 días", 96);
            Button btn3M = CrearBotonTab("3 meses", 186);
            Button btn1A = CrearBotonTab("Este año", 276);

            btn30.BackColor = Color.White;
            btn30.ForeColor = ink;
            btn30.Font = FontOf(9f, true);
            btn30.FlatAppearance.BorderSize = 1;
            btn30.FlatAppearance.BorderColor = border;

            tabsPanel.Controls.Add(btn7);
            tabsPanel.Controls.Add(btn30);
            tabsPanel.Controls.Add(btn3M);
            tabsPanel.Controls.Add(btn1A);
            p.Controls.Add(tabsPanel);

            btnRangoFechas = new Button
            {
                Text = "📅  " + fechaDesdeActual.ToString("dd MMM") + " — " + fechaHastaActual.ToString("dd MMM yyyy") + "  ▾",
                Size = new Size(240, 40),
                Location = new Point(p.Width - 240, 5),
                BackColor = Color.White,
                ForeColor = ink,
                FlatStyle = FlatStyle.Flat,
                Font = FontOf(9f, true),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnRangoFechas.FlatAppearance.BorderColor = border;
            Rounded(btnRangoFechas, 8);
            btnRangoFechas.Click += (s, e) => ToggleDropDownFechas();
            p.Controls.Add(btnRangoFechas);

            panelDropDownFechas = new Panel
            {
                Size = new Size(280, 250),
                BackColor = Color.White,
                Visible = false
            };
            Rounded(panelDropDownFechas, 12);
            panelDropDownFechas.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, panelDropDownFechas.ClientRectangle, border, ButtonBorderStyle.Solid);

            panelDropDownFechas.Controls.Add(LabelOf("SELECCIONAR PERIODO", 8f, muted, true));
            panelDropDownFechas.Controls[0].Location = new Point(20, 15);

            AgregarOpcionDesplegable("7 días", "12 Jun — 18 Jun 2025", 40, () => { fechaDesdeActual = DateTime.Today.AddDays(-7); fechaHastaActual = DateTime.Today; });
            AgregarOpcionDesplegable("30 días", "20 May — 18 Jun 2025", 90, () => { fechaDesdeActual = DateTime.Today.AddDays(-30); fechaHastaActual = DateTime.Today; });
            AgregarOpcionDesplegable("3 meses", "20 Mar — 18 Jun 2025", 140, () => { fechaDesdeActual = DateTime.Today.AddMonths(-3); fechaHastaActual = DateTime.Today; });
            AgregarOpcionDesplegable("Este año", "1 Ene — 18 Jun 2025", 190, () => { fechaDesdeActual = new DateTime(DateTime.Today.Year, 1, 1); fechaHastaActual = DateTime.Today; });

            btn7.Click += (s, e) => { SetTabActive(btn7, btn30, btn3M, btn1A); fechaDesdeActual = DateTime.Today.AddDays(-7); fechaHastaActual = DateTime.Today; ActualizarRangoYRefrescar(); };
            btn30.Click += (s, e) => { SetTabActive(btn30, btn7, btn3M, btn1A); fechaDesdeActual = DateTime.Today.AddDays(-30); fechaHastaActual = DateTime.Today; ActualizarRangoYRefrescar(); };
            btn3M.Click += (s, e) => { SetTabActive(btn3M, btn7, btn30, btn1A); fechaDesdeActual = DateTime.Today.AddMonths(-3); fechaHastaActual = DateTime.Today; ActualizarRangoYRefrescar(); };
            btn1A.Click += (s, e) => { SetTabActive(btn1A, btn7, btn30, btn3M); fechaDesdeActual = new DateTime(DateTime.Today.Year, 1, 1); fechaHastaActual = DateTime.Today; ActualizarRangoYRefrescar(); };

            p.Resize += (s, e) =>
            {
                btnRangoFechas.Location = new Point(p.Width - btnRangoFechas.Width, 5);
            };

            return p;
        }

        private void ToggleDropDownFechas()
        {
            if (panelDropDownFechas.Visible)
            {
                panelDropDownFechas.Visible = false;
                return;
            }

            Form mainForm = FindForm();
            if (mainForm == null) return;

            Point posicionGlobal = btnRangoFechas.PointToScreen(Point.Empty);
            Point posicionRelativa = mainForm.PointToClient(posicionGlobal);

            panelDropDownFechas.Location = new Point(
                posicionRelativa.X + btnRangoFechas.Width - panelDropDownFechas.Width,
                posicionRelativa.Y + btnRangoFechas.Height + 5
            );

            if (!mainForm.Controls.Contains(panelDropDownFechas))
            {
                mainForm.Controls.Add(panelDropDownFechas);
            }

            panelDropDownFechas.BringToFront();
            panelDropDownFechas.Visible = true;

            EventHandler clickFuera = null;
            clickFuera = (s, e) =>
            {
                if (!panelDropDownFechas.Bounds.Contains(mainForm.PointToClient(Cursor.Position)))
                {
                    panelDropDownFechas.Visible = false;
                    mainForm.Click -= clickFuera;
                }
            };
            mainForm.Click += clickFuera;
        }

        private void AgregarOpcionDesplegable(string titulo, string subtitulo, int y, Action accion)
        {
            Panel opcion = new Panel { Location = new Point(10, y), Size = new Size(260, 40), Cursor = Cursors.Hand };

            Label lblTitulo = LabelOf(titulo, 9f, ink, true);
            lblTitulo.Location = new Point(10, 5);

            Label lblSub = LabelOf(subtitulo, 8f, muted);
            lblSub.Location = new Point(10, 22);

            opcion.Controls.Add(lblTitulo);
            opcion.Controls.Add(lblSub);

            opcion.Click += (s, e) => { accion(); ToggleDropDownFechas(); };
            lblTitulo.Click += (s, e) => { accion(); ToggleDropDownFechas(); };
            lblSub.Click += (s, e) => { accion(); ToggleDropDownFechas(); };

            panelDropDownFechas.Controls.Add(opcion);
        }

        private void ActualizarRangoYRefrescar()
        {
            btnRangoFechas.Text = "📅  " + fechaDesdeActual.ToString("dd MMM") + " — " + fechaHastaActual.ToString("dd MMM yyyy") + "  ▾";
            LoadReport();
        }

        private Button CrearBotonTab(string texto, int x)
        {
            Button b = new Button
            {
                Text = texto,
                Size = new Size(84, 32),
                Location = new Point(x, 4),
                BackColor = Color.Transparent,
                ForeColor = muted,
                FlatStyle = FlatStyle.Flat,
                Font = FontOf(9f, false),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private void SetTabActive(Button active, params Button[] others)
        {
            active.BackColor = Color.White;
            active.ForeColor = ink;
            active.Font = FontOf(9f, true);
            active.FlatAppearance.BorderSize = 1;
            active.FlatAppearance.BorderColor = border;

            foreach (var btn in others)
            {
                btn.BackColor = Color.Transparent;
                btn.ForeColor = muted;
                btn.Font = FontOf(9f, false);
                btn.FlatAppearance.BorderSize = 0;
            }
        }

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

            string[] names = { "Total de equipos", "Tasa de utilización", "Incidencias abiertas", "Horas de uso" };
            string[] valuesText = { "0", "0%", "0", "0h" };
            string[] icons = { "📦", "📅", "⚠️", "⏱️" };
            Color[] colors = { blue, Color.FromArgb(139, 92, 246), amber, blue };

            for (int i = 0; i < 4; i++)
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            row.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            for (int i = 0; i < 4; i++)
            {
                Panel p = new Card
                {
                    Dock = DockStyle.Fill,
                    BackColor = surface,
                    Margin = new Padding(i == 0 ? 0 : 6, 0, i == 3 ? 0 : 6, 10),
                    Padding = new Padding(15)
                };

                TableLayoutPanel layoutTarjeta = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 3,
                    Margin = new Padding(0),
                    Padding = new Padding(0)
                };
                layoutTarjeta.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
                layoutTarjeta.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                layoutTarjeta.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));

                Label icon = LabelOf(icons[i], 14f, colors[i]);
                Panel iconBox = new Panel
                {
                    Size = new Size(32, 32),
                    BackColor = Color.FromArgb(240, 245, 255),
                    Dock = DockStyle.Left,
                    Margin = new Padding(0)
                };
                Rounded(iconBox, 8);
                icon.Dock = DockStyle.Fill;
                icon.TextAlign = ContentAlignment.MiddleCenter;
                iconBox.Controls.Add(icon);
                layoutTarjeta.Controls.Add(iconBox, 0, 0);

                Label t = LabelOf(names[i], 9.5f, muted);
                t.AutoSize = false;
                t.Dock = DockStyle.Fill;
                t.TextAlign = ContentAlignment.BottomLeft;
                layoutTarjeta.Controls.Add(t, 0, 1);

                values[i] = LabelOf(valuesText[i], 22f, ink, true);
                values[i].AutoSize = false;
                values[i].Dock = DockStyle.Fill;
                values[i].TextAlign = ContentAlignment.TopLeft;
                layoutTarjeta.Controls.Add(values[i], 0, 2);

                p.Controls.Add(layoutTarjeta);
                row.Controls.Add(p, i, 0);
            }

            return row;
        }

        private Control BuildChartsLayout()
        {
            TableLayoutPanel row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 15)
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));

            Panel cardBars = new Panel { Dock = DockStyle.Fill, BackColor = surface, Margin = new Padding(0, 0, 10, 0), Padding = new Padding(20) };
            Rounded(cardBars, 12);
            cardBars.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, cardBars.ClientRectangle, border, ButtonBorderStyle.Solid);

            Label lblTitleBars = LabelOf("Actividad de equipos", 14f, ink, true);
            lblTitleBars.Location = new Point(24, 20);
            cardBars.Controls.Add(lblTitleBars);

            Label subBars = LabelOf("Uso y reservas durante los últimos 12 meses", 9f, muted);
            subBars.Location = new Point(24, 45);
            cardBars.Controls.Add(subBars);

            Panel legendBars = new Panel { Location = new Point(cardBars.Width - 200, 25), Size = new Size(180, 20), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            Panel dotHoras = new Panel { BackColor = blue, Size = new Size(10, 10), Location = new Point(0, 5) };
            Label lblHoras = LabelOf("Salidas", 8.5f, muted);
            lblHoras.Location = new Point(15, 1);
            Panel dotReservas = new Panel { BackColor = grayBar, Size = new Size(10, 10), Location = new Point(100, 5) };
            Label lblReservas = LabelOf("Retornos", 8.5f, muted);
            lblReservas.Location = new Point(115, 1);
            legendBars.Controls.Add(dotHoras); legendBars.Controls.Add(lblHoras);
            legendBars.Controls.Add(dotReservas); legendBars.Controls.Add(lblReservas);
            cardBars.Controls.Add(legendBars);

            chartBarsRef = new OverviewBarChart
            {
                Location = new Point(24, 70),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            cardBars.Controls.Add(chartBarsRef);
            cardBars.Resize += (s, e) =>
            {
                chartBarsRef.Size = new Size(cardBars.Width - 48, cardBars.Height - 100);
                legendBars.Location = new Point(cardBars.Width - 200, 25);
            };

            Panel cardDonut = new Panel { Dock = DockStyle.Fill, BackColor = surface, Margin = new Padding(10, 0, 0, 0), Padding = new Padding(20) };
            Rounded(cardDonut, 12);
            cardDonut.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, cardDonut.ClientRectangle, border, ButtonBorderStyle.Solid);

            Label lblTitleDonut = LabelOf("Estado del inventario", 14f, ink, true);
            lblTitleDonut.Location = new Point(24, 20);
            cardDonut.Controls.Add(lblTitleDonut);

            Label subDonut = LabelOf("Distribución actual", 9f, muted);
            subDonut.Location = new Point(24, 45);
            cardDonut.Controls.Add(subDonut);

            chartDonutRef = new OverviewDonutChart
            {
                Location = new Point(24, 70),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            cardDonut.Controls.Add(chartDonutRef);
            cardDonut.Resize += (s, e) => chartDonutRef.Size = new Size(cardDonut.Width - 48, cardDonut.Height - 100);

            row.Controls.Add(cardBars, 0, 0);
            row.Controls.Add(cardDonut, 1, 0);

            return row;
        }

        private Control BuildTableWithFilters()
        {
            Panel p = new Card
            {
                Dock = DockStyle.Fill,
                BackColor = surface,
                Padding = new Padding(24),
                Margin = new Padding(0)
            };

            Label title = LabelOf("Incidencias recientes", 15f, ink, true);
            title.Location = new Point(24, 20);

            Label sub = LabelOf("Seguimiento de reportes y mantenimientos del equipo", 9f, muted);
            sub.Location = new Point(24, 45);

            p.Controls.Add(title);
            p.Controls.Add(sub);

            Panel searchContainer = new Panel
            {
                Size = new Size(260, 36),
                Location = new Point(p.Width - 280, 25),
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Rounded(searchContainer, 8);
            searchContainer.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, searchContainer.ClientRectangle, border, ButtonBorderStyle.Solid);

            search = new TextBox
            {
                PlaceholderText = "🔍  Buscar incidencia...",
                Font = FontOf(9),
                Width = 240,
                Location = new Point(10, 8),
                BackColor = Color.White,
                ForeColor = ink,
                BorderStyle = BorderStyle.None
            };
            searchContainer.Controls.Add(search);
            p.Controls.Add(searchContainer);

            stateFilter = ComboOf("Todos los estados", "A Tiempo", "Por Vencer", "Vencidos");
            stateFilter.Width = 150;
            stateFilter.Height = 36;
            stateFilter.Location = new Point(searchContainer.Left - 160, 25);
            stateFilter.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            p.Controls.Add(stateFilter);

            detailGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = surface,
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
                ColumnHeadersHeight = 50,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowTemplate = { Height = 60 },
                AllowUserToResizeRows = false
            };

            detailGrid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(249, 250, 251),
                ForeColor = muted,
                Font = FontOf(8f, true),
                Padding = new Padding(15, 0, 0, 0),
                SelectionBackColor = Color.FromArgb(249, 250, 251),
                SelectionForeColor = muted
            };

            detailGrid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = surface,
                ForeColor = ink,
                Font = FontOf(9.5f),
                Padding = new Padding(15, 0, 3, 0),
                SelectionBackColor = Color.FromArgb(241, 245, 249),
                SelectionForeColor = ink
            };

            detailGrid.CellPainting += DetailGrid_CellPainting;
            detailGrid.CellContentClick += Grid_CellContentClick;

            detailGrid.Location = new Point(24, 80);
            detailGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            Panel panelPaginacion = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = surface
            };

            Label lblShow = LabelOf("Mostrar", 9f, muted);
            lblShow.Location = new Point(24, 15);
            panelPaginacion.Controls.Add(lblShow);

            cmbPageSize = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = ink,
                Font = FontOf(9),
                Width = 60,
                Location = new Point(80, 12)
            };
            cmbPageSize.Items.AddRange(new object[] { "10", "25", "50", "Todos" });
            cmbPageSize.SelectedIndex = 0;
            cmbPageSize.SelectedIndexChanged += (s, e) => ApplyPagination();
            panelPaginacion.Controls.Add(cmbPageSize);

            Label lblEntries = LabelOf("registros", 9f, muted);
            lblEntries.Location = new Point(145, 15);
            panelPaginacion.Controls.Add(lblEntries);

            lblPagination = LabelOf("Mostrando 0 de 0 registros", 9f, muted);
            lblPagination.Location = new Point(215, 15);
            panelPaginacion.Controls.Add(lblPagination);

            p.Controls.Add(panelPaginacion);
            p.Controls.Add(detailGrid);

            p.Resize += (s, e) =>
            {
                searchContainer.Location = new Point(p.Width - searchContainer.Width - 24, 25);
                stateFilter.Location = new Point(searchContainer.Left - stateFilter.Width - 10, 25);

                detailGrid.SetBounds(24, 80, p.Width - 48, p.Height - 150);
                panelPaginacion.SetBounds(24, p.Height - 60, p.Width - 48, 50);
            };

            search.TextChanged += (s, e) => ApplySearch();
            stateFilter.SelectedIndexChanged += (s, e) => ApplySearch();

            return p;
        }

        private void DetailGrid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && detailGrid.Columns[e.ColumnIndex].Name == "EstadoPrestamo")
            {
                e.PaintBackground(e.CellBounds, true);

                string val = Convert.ToString(e.Value);
                Color bg = greenBg;
                Color fg = green;

                if (val.Contains("🔴")) { bg = redBg; fg = red; }
                else if (val.Contains("🟡")) { bg = amberBg; fg = amber; }

                string texto = "A tiempo";
                if (val.Contains("Vencido")) texto = "Vencido";
                else if (val.Contains("Por vencer")) texto = "Por vencer";

                Rectangle rect = new Rectangle(e.CellBounds.X + 15, e.CellBounds.Y + 16, e.CellBounds.Width - 30, 26);

                using (GraphicsPath path = RoundPath(rect, 13))
                using (SolidBrush brush = new SolidBrush(bg))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                }

                TextRenderer.DrawText(e.Graphics, texto, FontOf(8.5f, true), rect, fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                e.Handled = true;
            }

            if (e.RowIndex >= 0 && detailGrid.Columns[e.ColumnIndex].Name == "Tipo")
            {
                e.PaintBackground(e.CellBounds, true);
                string val = Convert.ToString(e.Value);
                Color fg = val == "Entrada" ? green : amber;

                Rectangle rect = new Rectangle(e.CellBounds.X + 15, e.CellBounds.Y, e.CellBounds.Width - 15, e.CellBounds.Height);
                TextRenderer.DrawText(e.Graphics, val, FontOf(9.5f, true), rect, fg,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

                e.Handled = true;
            }
        }

        // ============================================================
        // CONEXIÓN Y DATOS
        // ============================================================
        private NpgsqlConnection OpenConnection()
        {
            NpgsqlConnection c = new global::SUSTENTACION.Conexion().ObtenerConexion();
            if (c == null) throw new InvalidOperationException("La conexión existente devolvió null.");
            if (c.State != ConnectionState.Open) c.Open();
            return c;
        }

        private void LoadReport()
        {
            if (loading) return;

            loading = true;

            try
            {
                using (NpgsqlConnection c = OpenConnection())
                {
                    string sqlSalidas = @"
                        SELECT 
                            s.id_salida AS Registro, 
                            e.codigo AS Código, 
                            e.nombre AS Equipo, 
                            'Salida' AS Tipo, 
                            1 AS Cantidad, 
                            COALESCE(t.nombre, 'Sin asignar') AS Responsable, 
                            COALESCE(t.telefono, '') AS Telefono,
                            COALESCE(s.detalles, '') AS Motivo, 
                            s.fecha_salida AS Fecha,
                            (CURRENT_DATE - s.fecha_salida::date) AS DiasTranscurridos
                        FROM public.salida_equipos se
                        JOIN public.salidas s ON s.id_salida = se.id_salida
                        JOIN public.equipos e ON e.id = se.id_equipo
                        LEFT JOIN public.trabajadores t ON t.id_trabajador = s.id_trabajador
                        WHERE s.fecha_salida >= @desde AND s.fecha_salida < @hasta
                        ORDER BY s.fecha_salida DESC LIMIT 500";

                    string sqlEntradas = @"
                        SELECT 
                            r.id_retorno AS Registro, 
                            COALESCE(e.codigo, '') AS Código, 
                            COALESCE(e.nombre, '') AS Equipo, 
                            'Entrada' AS Tipo, 
                            1 AS Cantidad, 
                            COALESCE(t.nombre, 'Sin asignar') AS Responsable, 
                            COALESCE(t.telefono, '') AS Telefono,
                            COALESCE(r.observaciones, '') AS Motivo, 
                            r.fecha_retorno AS Fecha,
                            0 AS DiasTranscurridos
                        FROM public.retornos r
                        LEFT JOIN public.equipos e ON e.id = r.id_equipo
                        LEFT JOIN public.trabajadores t ON t.id_trabajador = r.id_trabajador
                        WHERE r.fecha_retorno >= @desde AND r.fecha_retorno < @hasta
                        ORDER BY r.fecha_retorno DESC LIMIT 500";

                    DataTable dSalidas = new DataTable();
                    using (NpgsqlCommand cmdS = new NpgsqlCommand(sqlSalidas, c))
                    {
                        cmdS.Parameters.AddWithValue("@desde", fechaDesdeActual);
                        cmdS.Parameters.AddWithValue("@hasta", fechaHastaActual.AddDays(1));
                        using (NpgsqlDataAdapter a = new NpgsqlDataAdapter(cmdS)) a.Fill(dSalidas);
                    }

                    DataTable dEntradas = new DataTable();
                    using (NpgsqlCommand cmdE = new NpgsqlCommand(sqlEntradas, c))
                    {
                        cmdE.Parameters.AddWithValue("@desde", fechaDesdeActual);
                        cmdE.Parameters.AddWithValue("@hasta", fechaHastaActual.AddDays(1));
                        using (NpgsqlDataAdapter a = new NpgsqlDataAdapter(cmdE)) a.Fill(dEntradas);
                    }

                    DataTable dCompleta = dSalidas;
                    if (dEntradas.Rows.Count > 0) dCompleta.Merge(dEntradas);

                    if (!dCompleta.Columns.Contains("EstadoPrestamo")) dCompleta.Columns.Add("EstadoPrestamo", typeof(string));
                    if (!dCompleta.Columns.Contains("Accion")) dCompleta.Columns.Add("Accion", typeof(string));
                    if (!dCompleta.Columns.Contains("Accion2")) dCompleta.Columns.Add("Accion2", typeof(string));

                    foreach (DataRow row in dCompleta.Rows)
                    {
                        if (row["Tipo"].ToString() == "Salida")
                        {
                            int dias = Convert.ToInt32(row["DiasTranscurridos"]);

                            if (dias > 3)
                            {
                                row["EstadoPrestamo"] = "🔴 Vencido";
                                row["Accion"] = "Recordar";
                                row["Accion2"] = "🔧 Mantenimiento";
                            }
                            else if (dias >= 2)
                            {
                                row["EstadoPrestamo"] = "🟡 Por vencer";
                                row["Accion"] = "Recordar";
                                row["Accion2"] = "🔧 Mantenimiento";
                            }
                            else
                            {
                                row["EstadoPrestamo"] = "🟢 A tiempo";
                                row["Accion"] = "Recordar";
                                row["Accion2"] = "🔧 Mantenimiento";
                            }
                        }
                        else
                        {
                            row["EstadoPrestamo"] = "-";
                            row["Accion"] = "";
                            row["Accion2"] = "";
                        }
                    }

                    SetData(dCompleta);

                    ActualizarGraficosReales(c);
                }

                state.Text = "●  Conectado · Reporte actualizado " + DateTime.Now.ToString("HH:mm:ss");
                state.ForeColor = green;
            }
            catch (Exception ex)
            {
                SetData(new DataTable());
                state.Text = "No se pudo cargar el reporte. Revisa la conexión.";
                state.ForeColor = red;
                Error("Reporte no disponible", ex);
            }
            finally
            {
                loading = false;
                ApplySearch();
            }
        }

        private void ActualizarGraficosReales(NpgsqlConnection c)
        {
            int totalEquipos = 0, disponibles = 0, enUso = 0, mantenimiento = 0, noDisponibles = 0;

            using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT COUNT(*), estado FROM public.equipos GROUP BY estado", c))
            using (NpgsqlDataReader r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    int count = Convert.ToInt32(r[0]);
                    string estado = Convert.ToString(r[1]).ToLower();
                    totalEquipos += count;

                    if (estado.Contains("dispon")) disponibles += count;
                    else if (estado.Contains("manten")) mantenimiento += count;
                    else if (estado.Contains("uso") || estado.Contains("asign")) enUso += count;
                    else noDisponibles += count;
                }
            }

            using (NpgsqlCommand cmd = new NpgsqlCommand(@"
                SELECT COUNT(*) FROM public.salida_equipos se
                JOIN public.salidas s ON s.id_salida = se.id_salida
                WHERE NOT EXISTS (SELECT 1 FROM public.retornos r WHERE r.id_salida = s.id_salida)", c))
            {
                int prestadosReales = Convert.ToInt32(cmd.ExecuteScalar());
                if (prestadosReales > enUso)
                {
                    noDisponibles += (enUso - prestadosReales);
                    enUso = prestadosReales;
                }
            }

            chartDonutRef.SetData(
                new float[] { disponibles, enUso, mantenimiento, noDisponibles },
                new string[] { "Disponibles", "En uso", "Mantenimiento", "No disponibles" },
                totalEquipos
            );

            List<int> salidasPorMes = new List<int>(new int[12]);
            List<int> retornosPorMes = new List<int>(new int[12]);

            using (NpgsqlCommand cmd = new NpgsqlCommand(@"
                SELECT EXTRACT(MONTH FROM fecha_salida) AS mes, COUNT(*) 
                FROM public.salidas 
                WHERE EXTRACT(YEAR FROM fecha_salida) = EXTRACT(YEAR FROM CURRENT_DATE)
                GROUP BY mes", c))
            using (NpgsqlDataReader r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    int mes = Convert.ToInt32(r[0]) - 1;
                    if (mes >= 0 && mes < 12) salidasPorMes[mes] = Convert.ToInt32(r[1]);
                }
            }

            using (NpgsqlCommand cmd = new NpgsqlCommand(@"
                SELECT EXTRACT(MONTH FROM fecha_retorno) AS mes, COUNT(*) 
                FROM public.retornos 
                WHERE EXTRACT(YEAR FROM fecha_retorno) = EXTRACT(YEAR FROM CURRENT_DATE)
                GROUP BY mes", c))
            using (NpgsqlDataReader r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    int mes = Convert.ToInt32(r[0]) - 1;
                    if (mes >= 0 && mes < 12) retornosPorMes[mes] = Convert.ToInt32(r[1]);
                }
            }

            chartBarsRef.SetData(salidasPorMes, retornosPorMes);

            values[0].Text = totalEquipos.ToString("N0");

            double tasa = totalEquipos > 0 ? ((double)enUso / totalEquipos) * 100 : 0;
            values[1].Text = tasa.ToString("0.0") + "%";

            values[2].Text = mantenimiento.ToString("N0");
        }

        private void SetData(DataTable data)
        {
            if (reportData != null) reportData.Dispose();

            reportData = data;
            detailGrid.DataSource = data;

            if (data.Columns.Count > 0)
            {
                Ocultar("Telefono");
                Ocultar("DiasTranscurridos");
                Ocultar("Registro"); // Ocultamos el ID interno

                Renombrar("Código", "CÓDIGO");
                Renombrar("Equipo", "EQUIPO");
                Renombrar("Tipo", "TIPO");
                Renombrar("Cantidad", "CANT.");
                Renombrar("Responsable", "RESPONSABLE");
                Renombrar("Motivo", "MOTIVO");
                Renombrar("Fecha", "FECHA");
                Renombrar("EstadoPrestamo", "ESTADO");
                Renombrar("Accion", "ACCIÓN");
                Renombrar("Accion2", "MANTENIMIENTO"); // Título más claro

                if (detailGrid.Columns.Contains("Fecha"))
                    detailGrid.Columns["Fecha"].DefaultCellStyle.Format = "dd MMM, yyyy";

                if (detailGrid.Columns.Contains("Accion"))
                {
                    detailGrid.Columns["Accion"].DefaultCellStyle.ForeColor = blue;
                    detailGrid.Columns["Accion"].DefaultCellStyle.Font = FontOf(9.5f, true);
                }

                if (detailGrid.Columns.Contains("Accion2"))
                {
                    detailGrid.Columns["Accion2"].Width = 130;
                    detailGrid.Columns["Accion2"].DefaultCellStyle.ForeColor = amber;
                    detailGrid.Columns["Accion2"].DefaultCellStyle.Font = FontOf(9.5f, true);
                }
            }

            ApplyPagination();
        }

        private void Ocultar(string col) { if (detailGrid.Columns.Contains(col)) detailGrid.Columns[col].Visible = false; }
        private void Renombrar(string col, string header) { if (detailGrid.Columns.Contains(col)) detailGrid.Columns[col].HeaderText = header; }

        private void ApplySearch()
        {
            if (reportData == null) return;

            string text = search == null ? "" : search.Text.Trim();
            List<string> terms = new List<string>();

            if (text.Length > 0)
            {
                string safe = Escape(text);
                foreach (string col in new[] { "Código", "Equipo", "Tipo", "Responsable", "Motivo" })
                {
                    if (reportData.Columns.Contains(col))
                        terms.Add("[" + col + "] LIKE '%" + safe + "%'");
                }
            }

            if (stateFilter != null && stateFilter.SelectedIndex > 0)
            {
                string selectedState = stateFilter.SelectedItem.ToString();
                if (selectedState == "A Tiempo")
                    terms.Add("EstadoPrestamo LIKE '%🟢%'");
                else if (selectedState == "Por Vencer")
                    terms.Add("EstadoPrestamo LIKE '%🟡%'");
                else if (selectedState == "Vencidos")
                    terms.Add("EstadoPrestamo LIKE '%🔴%'");
            }

            reportData.DefaultView.RowFilter = terms.Count > 0
                ? string.Join(" AND ", terms)
                : "";

            ApplyPagination();
        }

        private void ApplyPagination()
        {
            if (reportData == null || lblPagination == null) return;

            int total = reportData.DefaultView.Count;
            string selected = cmbPageSize?.SelectedItem?.ToString() ?? "Todos";
            int limit = selected == "Todos" ? total : Convert.ToInt32(selected);

            DataTable paginatedTable = reportData.Clone();
            for (int i = 0; i < Math.Min(total, limit); i++)
            {
                paginatedTable.ImportRow(reportData.DefaultView[i].Row);
            }

            detailGrid.DataSource = paginatedTable;
            lblPagination.Text = $"Mostrando {Math.Min(total, limit)} de {total} registros";
        }

        private static string Escape(string s)
        {
            StringBuilder b = new StringBuilder();

            foreach (char c in s)
            {
                if (c == '\'') b.Append("''");
                else if (c == '[') b.Append("[[]");
                else if (c == ']') b.Append("[]]");
                else if (c == '%') b.Append("[%]");
                else if (c == '*') b.Append("[*]");
                else b.Append(c);
            }

            return b.ToString();
        }

        private void Grid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // 1. Lógica para MANTENIMIENTO (Accion2)
            if (detailGrid.Columns[e.ColumnIndex].Name == "Accion2")
            {
                var fila = detailGrid.Rows[e.RowIndex];
                string equipo = Convert.ToString(fila.Cells["Equipo"].Value);
                string idRegistro = Convert.ToString(fila.Cells["Registro"].Value);

                var confirm = MessageBox.Show($"¿Estás seguro de pasar el equipo *{equipo}* a Mantenimiento?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm == DialogResult.Yes)
                {
                    try
                    {
                        using (var conexion = OpenConnection())
                        {
                            string sql = "UPDATE public.equipos SET estado = 'Mantenimiento' WHERE id = (SELECT id_equipo FROM public.salida_equipos WHERE id_salida = @id LIMIT 1)";
                            using (var cmd = new NpgsqlCommand(sql, conexion))
                            {
                                cmd.Parameters.AddWithValue("@id", Convert.ToInt32(idRegistro));
                                cmd.ExecuteNonQuery();
                            }
                        }
                        MessageBox.Show("Equipo enviado a Mantenimiento.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadReport();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al cambiar estado: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                return; // Cortar ejecución aquí
            }

            // 2. Lógica para WHATSAPP (Accion)
            if (detailGrid.Columns[e.ColumnIndex].Name == "Accion")
            {
                var fila = detailGrid.Rows[e.RowIndex];
                string equipo = Convert.ToString(fila.Cells["Equipo"].Value);
                string responsable = Convert.ToString(fila.Cells["Responsable"].Value);
                string telefono = Convert.ToString(fila.Cells["Telefono"].Value);
                string tipo = Convert.ToString(fila.Cells["Tipo"].Value);
                string idRegistro = Convert.ToString(fila.Cells["Registro"].Value);

                if (tipo != "Salida") return;

                if (string.IsNullOrWhiteSpace(telefono))
                {
                    MessageBox.Show("El trabajador no tiene un número de teléfono registrado.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string numLimpio = new string(telefono.Where(char.IsDigit).ToArray());
                if (numLimpio.Length == 9) numLimpio = "51" + numLimpio;

                string mensaje = $"Hola *{responsable}*, te recordamos que tienes el equipo *{equipo}* pendiente de devolución en Artemusa TV.";
                string url = $"https://wa.me/{numLimpio}?text={Uri.EscapeDataString(mensaje)}";

                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });

                    using (var conexion = OpenConnection())
                    {
                        string sqlUpdate = @"
                            UPDATE public.salidas 
                            SET detalles = COALESCE(detalles, '') || ' | 📱 Recordatorio enviado el ' || to_char(NOW(), 'DD/MM/YYYY HH24:MI')
                            WHERE id_salida = @id";

                        using (var cmd = new NpgsqlCommand(sqlUpdate, conexion))
                        {
                            cmd.Parameters.AddWithValue("@id", Convert.ToInt32(idRegistro));
                            cmd.ExecuteNonQuery();
                        }
                    }

                    LoadReport();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("No se pudo abrir WhatsApp o guardar el registro: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportCsv()
        {
            if (reportData == null || reportData.DefaultView.Count == 0)
            {
                Info("No hay registros visibles para exportar.");
                return;
            }

            using (SaveFileDialog d = new SaveFileDialog
            {
                Filter = "Archivo CSV (*.csv)|*.csv",
                FileName = "reporte_artemusa_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv"
            })
            {
                if (d.ShowDialog(FindForm()) != DialogResult.OK) return;

                using (StreamWriter w = new StreamWriter(d.FileName, false, new UTF8Encoding(true)))
                {
                    List<string> heads = new List<string>();
                    foreach (DataColumn c in reportData.Columns)
                    {
                        if (c.ColumnName != "Accion" && c.ColumnName != "Accion2" && c.ColumnName != "Telefono" && c.ColumnName != "DiasTranscurridos")
                            heads.Add(Csv(c.ColumnName));
                    }

                    w.WriteLine(string.Join(";", heads));

                    foreach (DataRowView row in reportData.DefaultView)
                    {
                        List<string> cells = new List<string>();
                        foreach (DataColumn c in reportData.Columns)
                        {
                            if (c.ColumnName != "Accion" && c.ColumnName != "Accion2" && c.ColumnName != "Telefono" && c.ColumnName != "DiasTranscurridos")
                                cells.Add(Csv(Convert.ToString(row[c.ColumnName], CultureInfo.CurrentCulture)));
                        }

                        w.WriteLine(string.Join(";", cells));
                    }
                }

                Info("Reporte exportado correctamente.");
            }
        }

        private static string Csv(string value)
        {
            value = value ?? "";
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private void Info(string text)
        {
            MessageBox.Show(FindForm(), text, "Artemusa TV",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Error(string title, Exception ex)
        {
            MessageBox.Show(FindForm(), title + "\n\n" + ex.Message, "Artemusa TV",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // ============================================================
        // CONTROLES PERSONALIZADOS DE GRÁFICOS
        // ============================================================
        private sealed class OverviewBarChart : Panel
        {
            private List<int> _salidas = new List<int>(new int[12]);
            private List<int> _retornos = new List<int>(new int[12]);

            public OverviewBarChart()
            {
                DoubleBuffered = true;
                BackColor = Color.Transparent;
            }

            public void SetData(List<int> salidas, List<int> retornos)
            {
                _salidas = salidas;
                _retornos = retornos;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                int left = 40;
                int right = Width - 10;
                int top = 10;
                int bottom = Height - 30;
                int w = right - left;
                int h = bottom - top;

                int maxVal = 0;
                for (int i = 0; i < 12; i++)
                {
                    if (_salidas[i] > maxVal) maxVal = _salidas[i];
                    if (_retornos[i] > maxVal) maxVal = _retornos[i];
                }

                if (maxVal == 0) maxVal = 10;
                maxVal = ((maxVal / 10) + 1) * 10;

                using (Pen gridPen = new Pen(Color.FromArgb(241, 245, 249), 1))
                using (Font fontY = new Font("Segoe UI", 7.5f))
                using (Brush brushText = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    for (int i = 0; i <= 4; i++)
                    {
                        int y = bottom - (i * h / 4);
                        e.Graphics.DrawLine(gridPen, left, y, right, y);
                        int val = (maxVal * i / 4);
                        e.Graphics.DrawString(val + "", fontY, brushText, 5, y - 7);
                    }
                }

                int barras = 12;
                int groupW = w / barras;
                int barW = 10;
                int gap = 4;

                using (Brush barBrush = new SolidBrush(Color.FromArgb(99, 102, 241)))
                using (Brush reserveBrush = new SolidBrush(Color.FromArgb(209, 213, 219)))
                using (Font fontX = new Font("Segoe UI", 8f))
                using (Brush brushX = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    for (int i = 0; i < barras; i++)
                    {
                        int valSalidas = _salidas[i];
                        int valRetornos = _retornos[i];

                        int hSalidas = (int)(((double)valSalidas / maxVal) * h);
                        int hRetornos = (int)(((double)valRetornos / maxVal) * h);

                        int xBase = left + (i * groupW) + (groupW - (barW * 2 + gap)) / 2;

                        Rectangle rectSalidas = new Rectangle(xBase, bottom - hSalidas, barW, hSalidas);
                        using (GraphicsPath path = RoundPath(rectSalidas, 4))
                            e.Graphics.FillPath(barBrush, path);

                        Rectangle rectRetornos = new Rectangle(xBase + barW + gap, bottom - hRetornos, barW, hRetornos);
                        using (GraphicsPath path = RoundPath(rectRetornos, 4))
                            e.Graphics.FillPath(reserveBrush, path);

                        string[] meses = { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };
                        string mes = meses[i];
                        SizeF size = e.Graphics.MeasureString(mes, fontX);
                        e.Graphics.DrawString(mes, fontX, brushX, xBase + barW - (size.Width / 2) + gap / 2, bottom + 5);
                    }
                }
            }

            private static GraphicsPath RoundPath(Rectangle rect, int radius)
            {
                GraphicsPath path = new GraphicsPath();
                if (rect.Width < 4 || rect.Height < 4) { path.AddRectangle(rect); return path; }
                int r = Math.Max(1, Math.Min(radius, Math.Min(rect.Width / 2, rect.Height / 2)));
                int d = r * 2;
                path.AddArc(rect.X, rect.Y, d, d, 180, 90);
                path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                return path;
            }
        }

        private sealed class OverviewDonutChart : Panel
        {
            private float[] _percentages = new float[4];
            private string[] _labels = new string[4];
            private int _total = 0;

            public OverviewDonutChart()
            {
                DoubleBuffered = true;
                BackColor = Color.Transparent;
            }

            public void SetData(float[] values, string[] labels, int total)
            {
                _labels = labels;
                _total = total;

                _percentages = new float[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    _percentages[i] = total > 0 ? (values[i] / (float)total) * 100f : 0;
                }

                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                int size = Math.Min(Width / 2, Height - 20);
                Rectangle rectDonut = new Rectangle(10, (Height - size) / 2, size, size);

                Color[] colors = {
                    Color.FromArgb(99, 102, 241),
                    Color.FromArgb(52, 211, 153),
                    Color.FromArgb(245, 158, 11),
                    Color.FromArgb(209, 213, 219)
                };

                float startAngle = -90f;

                for (int i = 0; i < _percentages.Length; i++)
                {
                    float sweepAngle = (_percentages[i] / 100f) * 360f;
                    using (SolidBrush brush = new SolidBrush(colors[i]))
                        e.Graphics.FillPie(brush, rectDonut, startAngle, sweepAngle);
                    startAngle += sweepAngle;
                }

                int holeSize = (int)(size * 0.65);
                Rectangle rectHole = new Rectangle(
                    rectDonut.X + (size - holeSize) / 2,
                    rectDonut.Y + (size - holeSize) / 2,
                    holeSize, holeSize);

                using (SolidBrush brushHole = new SolidBrush(Color.White))
                    e.Graphics.FillEllipse(brushHole, rectHole);

                using (Font fontTotal = new Font("Segoe UI", 16f, FontStyle.Bold))
                using (Font fontSub = new Font("Segoe UI", 8f))
                using (Brush brushInk = new SolidBrush(Color.FromArgb(30, 41, 59)))
                using (Brush brushMuted = new SolidBrush(Color.FromArgb(100, 116, 139)))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    e.Graphics.DrawString(_total.ToString(), fontTotal, brushInk, new Rectangle(rectHole.X, rectHole.Y - 10, rectHole.Width, rectHole.Height), sf);
                    e.Graphics.DrawString("Equipos", fontSub, brushMuted, new Rectangle(rectHole.X, rectHole.Y + 15, rectHole.Width, rectHole.Height), sf);
                }

                int legendX = rectDonut.Right + 20;
                int legendY = 20;
                int rowH = 35;

                using (Font fontLabel = new Font("Segoe UI", 8.5f))
                using (Font fontValue = new Font("Segoe UI", 9f, FontStyle.Bold))
                using (Font fontPercent = new Font("Segoe UI", 7.5f))
                using (Brush brushInk = new SolidBrush(Color.FromArgb(30, 41, 59)))
                using (Brush brushMuted = new SolidBrush(Color.FromArgb(100, 116, 139)))
                {
                    for (int i = 0; i < _labels.Length; i++)
                    {
                        int y = legendY + (i * rowH);

                        using (SolidBrush b = new SolidBrush(colors[i]))
                            e.Graphics.FillEllipse(b, legendX, y + 5, 8, 8);

                        e.Graphics.DrawString(_labels[i], fontLabel, brushMuted, legendX + 15, y);

                        int valCount = (int)Math.Round((_percentages[i] / 100f) * _total);
                        string val = valCount.ToString();
                        string pct = _percentages[i].ToString("0.0") + "%";

                        SizeF valSize = e.Graphics.MeasureString(val, fontValue);
                        SizeF pctSize = e.Graphics.MeasureString(pct, fontPercent);

                        e.Graphics.DrawString(val, fontValue, brushInk, legendX + 130 - valSize.Width, y - 2);
                        e.Graphics.DrawString(pct, fontPercent, brushMuted, legendX + 130 - pctSize.Width, y + 12);
                    }
                }
            }
        }

        private class Card : Panel
        {
            public Card()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
                SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (Width < 20 || Height < 20) return;

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

                using (GraphicsPath path = RoundPath(rect, 12))
                {
                    using (Pen p = new Pen(Color.FromArgb(226, 232, 240), 1f))
                    {
                        e.Graphics.DrawPath(p, path);
                    }
                }
            }
        }
    }
}