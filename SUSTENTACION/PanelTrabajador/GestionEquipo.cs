using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace SUSTENTACION.PanelTrabajador
{
    public class GestionEquipo : UserControl
    {
        // Paleta Dark Teal / Esmeralda
        private readonly Color bgMain = Color.FromArgb(17, 31, 36);
        private readonly Color bgCard = Color.FromArgb(23, 40, 47);
        private readonly Color bgHeaderTable = Color.FromArgb(29, 50, 58);
        private readonly Color bgRowHover = Color.FromArgb(35, 60, 70);
        private readonly Color accentGreen = Color.FromArgb(0, 230, 118);
        private readonly Color textWhite = Color.FromArgb(240, 245, 245);
        private readonly Color textMuted = Color.FromArgb(130, 160, 170);
        private readonly Color bordeSuave = Color.FromArgb(45, 70, 80);

        private DataGridView dgvInventario;
        private ComboBox cmbCategoria, cmbEstado, cmbTecnico;
        private TextBox txtBuscar;

        // Autocompletado
        private ListBox lstSugerencias;
        private Panel panelSugerenciasContenedor;
        private System.Windows.Forms.Timer timerBusqueda;
        private List<EquipoSugerencia> sugerenciasActuales = new List<EquipoSugerencia>();

        // Metricas KPI
        private Label lblValTotal, lblValEvento, lblValDisponibles, lblValReservadas, lblValDanados, lblValDevuelvenHoy;
        private Label lblSubDevuelvenHoy;   // ← NUEVO

        // Clase interna para sugerencias
        private class EquipoSugerencia
        {
            public int Id { get; set; }
            public string Codigo { get; set; } = "";
            public string Nombre { get; set; } = "";
            public string Categoria { get; set; } = "";
            public string Estado { get; set; } = "";
            public int Stock { get; set; }
            public override string ToString() =>
                $"{(string.IsNullOrEmpty(Codigo) ? "(sin código)" : Codigo)}  —  {Nombre}   [{Stock} disp.]";
        }

        public GestionEquipo()
        {
            InitializeComponentes();

            this.Load += async (s, e) =>
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    CargarFiltroTecnicos();
                    CargarFiltroCategorias();
                });

                this.BeginInvoke(new Action(() =>
                {
                    CargarInventarioEquipos();
                    CalcularMetricasKPI();
                }));
            };
        }

        private void InitializeComponentes()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = bgMain;
            this.Padding = new Padding(24, 20, 24, 20);

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100f));   // Header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 230f));   // KPIs ← 190 → 230
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));    // Tabla

            // ═══════════════════════════════════════════
            // HEADER
            // ═══════════════════════════════════════════
            Panel headerPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 14) };

            Label lblIconTitulo = new Label
            {
                Text = "📦",
                Font = new Font("Segoe UI", 22f),
                ForeColor = accentGreen,
                Location = new Point(0, 3),
                Size = new Size(40, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };
            headerPanel.Controls.Add(lblIconTitulo);

            Label lblTitulo = new Label
            {
                Text = "Inventario de Equipos",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(46, 5),
                AutoSize = true
            };

            Label lblSubtitulo = new Label
            {
                Text = "Gestión, control y seguimiento del inventario audiovisual",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = textMuted,
                Location = new Point(49, 42),
                AutoSize = true
            };

            // ── Buscador con autocompletado ──
            Panel panelBuscar = new Panel
            {
                Size = new Size(360, 44),
                Location = new Point(340, 12),
                BackColor = bgCard,
                Padding = new Padding(14, 0, 14, 0)
            };
            AplicarBordesRedondeados(panelBuscar, 10);
            panelBuscar.Paint += (s, e) =>
            {
                using (var pen = new Pen(bordeSuave, 1.2f))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var p = ObternerCaminoRedondeado(new Rectangle(0, 0, panelBuscar.Width - 1, panelBuscar.Height - 1), 10))
                        e.Graphics.DrawPath(pen, p);
                }
            };

            Label lblIconBuscar = new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI", 12),
                ForeColor = textMuted,
                Location = new Point(12, 12),
                AutoSize = true,
                BackColor = bgCard
            };
            panelBuscar.Controls.Add(lblIconBuscar);

            txtBuscar = new TextBox
            {
                Location = new Point(42, 12),
                Width = 305,
                BackColor = bgCard,
                ForeColor = textWhite,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10f),
                Text = ""
            };
            panelBuscar.Controls.Add(txtBuscar);

            headerPanel.Controls.Add(lblTitulo);
            headerPanel.Controls.Add(lblSubtitulo);
            headerPanel.Controls.Add(panelBuscar);

            // Filtros (a la derecha)
            FlowLayoutPanel pnlFiltros = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                WrapContents = false,
                Padding = new Padding(0, 10, 0, 0)
            };

            cmbCategoria = CrearComboBoxFiltro(new string[] { "Categoría: Todas" });
            cmbEstado = CrearComboBoxFiltro(new string[] { "Estado: Todos", "Disponible", "En Evento", "En Mantenimiento", "En Observación", "Con Observaciones", "Dañado" });
            cmbTecnico = CrearComboBoxFiltro(new string[] { "Técnico: Todos" });

            cmbCategoria.SelectedIndexChanged += (s, e) => CargarInventarioEquipos();
            cmbEstado.SelectedIndexChanged += (s, e) => CargarInventarioEquipos();
            cmbTecnico.SelectedIndexChanged += (s, e) => CargarInventarioEquipos();

            pnlFiltros.Controls.AddRange(new Control[] { cmbCategoria, cmbEstado, cmbTecnico });
            headerPanel.Controls.Add(pnlFiltros);

            // ═══════════════════════════════════════════
            // TARJETAS KPI
            // ═══════════════════════════════════════════
            TableLayoutPanel kpiLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Margin = new Padding(0, 0, 0, 16)
            };
            kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            kpiLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            kpiLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            Panel cardTotal = CrearTarjetaKPI("EQUIPOS EN ALMACÉN", "Modelos registrados", "0",
                Color.FromArgb(0, 180, 216), "📦", out lblValTotal);

            Panel cardEvento = CrearTarjetaKPI("EN EVENTO", "Actualmente fuera", "0",
                Color.FromArgb(139, 92, 246), "🚚", out lblValEvento);

            Panel cardDisp = CrearTarjetaKPI("DISPONIBLES", "Listos para salir", "0",
                accentGreen, "✅", out lblValDisponibles);

            Panel cardRes = CrearTarjetaKPI("RESERVADOS", "Apartados para eventos", "0",
                Color.FromArgb(247, 127, 0), "⏳", out lblValReservadas);

            // 🔥 CAMBIADO: "Dañados" → "Requieren atención"
            Panel cardDan = CrearTarjetaKPI("REQUIEREN ATENCIÓN", "Con observaciones o dañados", "0",
                Color.FromArgb(214, 40, 40), "⚠️", out lblValDanados);

            Panel cardDev = CrearTarjetaKPI("PRÓXIMOS RETORNOS", "Equipos a devolver", "0",
                Color.FromArgb(72, 149, 239), "🕒", out lblValDevuelvenHoy, out lblSubDevuelvenHoy);

            cardDev.Cursor = Cursors.Hand;
            cardDev.Click += (s, e) => FiltrarEquiposDevuelvenHoy();

            kpiLayout.Controls.Add(cardTotal, 0, 0);
            kpiLayout.Controls.Add(cardEvento, 1, 0);
            kpiLayout.Controls.Add(cardDisp, 2, 0);
            kpiLayout.Controls.Add(cardRes, 0, 1);
            kpiLayout.Controls.Add(cardDan, 1, 1);
            kpiLayout.Controls.Add(cardDev, 2, 1);

            // ═══════════════════════════════════════════
            // TABLA
            // ═══════════════════════════════════════════
            Panel containerTabla = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgCard,
                Padding = new Padding(18, 14, 18, 18)
            };
            AplicarBordesRedondeados(containerTabla, 12);

            Label lblTablaTitulo = new Label
            {
                Text = "📋  Listado de equipos",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = textWhite,
                Dock = DockStyle.Top,
                Height = 36,
                Padding = new Padding(4, 4, 0, 0)
            };
            containerTabla.Controls.Add(lblTablaTitulo);

            dgvInventario = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = bgCard,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(35, 60, 70),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                ColumnHeadersHeight = 44,
                RowTemplate = { Height = 48 },
                EnableHeadersVisualStyles = false,
                AllowUserToResizeRows = false
            };

            typeof(DataGridView).InvokeMember("DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null, dgvInventario, new object[] { true });

            dgvInventario.ColumnHeadersDefaultCellStyle.BackColor = bgHeaderTable;
            dgvInventario.ColumnHeadersDefaultCellStyle.ForeColor = textMuted;
            dgvInventario.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgvInventario.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 0, 10, 0);
            dgvInventario.ColumnHeadersDefaultCellStyle.SelectionBackColor = bgHeaderTable;

            dgvInventario.DefaultCellStyle.BackColor = bgCard;
            dgvInventario.DefaultCellStyle.ForeColor = textWhite;
            dgvInventario.DefaultCellStyle.Font = new Font("Segoe UI", 9f);
            dgvInventario.DefaultCellStyle.SelectionBackColor = bgRowHover;
            dgvInventario.DefaultCellStyle.SelectionForeColor = accentGreen;
            dgvInventario.DefaultCellStyle.Padding = new Padding(10, 0, 10, 0);

            dgvInventario.CellPainting += DgvInventario_CellPainting;

            containerTabla.Controls.Add(dgvInventario);
            lblTablaTitulo.BringToFront();
            dgvInventario.BringToFront();

            mainLayout.Controls.Add(headerPanel, 0, 0);
            mainLayout.Controls.Add(kpiLayout, 0, 1);
            mainLayout.Controls.Add(containerTabla, 0, 2);

            this.Controls.Add(mainLayout);

            // ── ListBox de sugerencias (flotante) ──
            panelSugerenciasContenedor = new Panel
            {
                Visible = false,
                BackColor = Color.FromArgb(23, 40, 47),
                BorderStyle = BorderStyle.FixedSingle
            };
            AplicarBordesRedondeados(panelSugerenciasContenedor, 8);
            panelSugerenciasContenedor.Paint += (s, e) =>
            {
                using (var pen = new Pen(accentGreen, 1.4f))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var p = ObternerCaminoRedondeado(
                        new Rectangle(0, 0, panelSugerenciasContenedor.Width - 1, panelSugerenciasContenedor.Height - 1), 8))
                        e.Graphics.DrawPath(pen, p);
                }
            };
            this.Controls.Add(panelSugerenciasContenedor);
            panelSugerenciasContenedor.BringToFront();

            lstSugerencias = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(23, 40, 47),
                ForeColor = textWhite,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10),
                IntegralHeight = false,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 44
            };
            lstSugerencias.DrawItem += LstSugerencias_DrawItem;
            lstSugerencias.Click += (s, e) => InsertarSugerenciaSeleccionada();
            lstSugerencias.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    InsertarSugerenciaSeleccionada();
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    OcultarSugerencias();
                    txtBuscar.Focus();
                }
            };
            panelSugerenciasContenedor.Controls.Add(lstSugerencias);

            // ── Timer para búsqueda con debounce ──
            timerBusqueda = new System.Windows.Forms.Timer { Interval = 220 };
            timerBusqueda.Tick += (s, e) =>
            {
                timerBusqueda.Stop();
                BuscarEquiposSugerencias(txtBuscar.Text.Trim());
            };

            txtBuscar.TextChanged += (s, e) =>
            {
                timerBusqueda.Stop();
                timerBusqueda.Start();
            };

            txtBuscar.Enter += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtBuscar.Text))
                {
                    timerBusqueda.Stop();
                    BuscarEquiposSugerencias("");
                }
            };

            txtBuscar.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtBuscar.Text))
                {
                    timerBusqueda.Stop();
                    BuscarEquiposSugerencias("");
                }
            };

            txtBuscar.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Down && lstSugerencias.Items.Count > 0 && panelSugerenciasContenedor.Visible)
                {
                    e.SuppressKeyPress = true;
                    lstSugerencias.Focus();
                    if (lstSugerencias.SelectedIndex < 0)
                        lstSugerencias.SelectedIndex = 0;
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    if (panelSugerenciasContenedor.Visible && lstSugerencias.Items.Count > 0)
                    {
                        if (lstSugerencias.SelectedIndex < 0)
                            lstSugerencias.SelectedIndex = 0;
                        InsertarSugerenciaSeleccionada();
                    }
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    OcultarSugerencias();
                }
            };

            txtBuscar.Leave += (s, e) =>
            {
                var t = new System.Windows.Forms.Timer { Interval = 180 };
                t.Tick += (ss, ee) =>
                {
                    t.Stop();
                    t.Dispose();
                    if (!panelSugerenciasContenedor.Focused && !lstSugerencias.Focused)
                        OcultarSugerencias();
                };
                t.Start();
            };
        }

        // ═══════════════════════════════════════════
        // AUTOCOMPLETADO
        // ═══════════════════════════════════════════
        private void LstSugerencias_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= lstSugerencias.Items.Count) return;
            var item = lstSugerencias.Items[e.Index] as EquipoSugerencia;
            if (item == null) return;

            bool seleccionado = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color fondoItem = seleccionado ? Color.FromArgb(30, 60, 70) : Color.FromArgb(23, 40, 47);

            using (var brush = new SolidBrush(fondoItem))
                e.Graphics.FillRectangle(brush, e.Bounds);

            if (seleccionado)
            {
                using (var pen = new Pen(accentGreen, 1.4f))
                    e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Top, e.Bounds.Left, e.Bounds.Bottom);
            }

            // Código (badge)
            string codigo = string.IsNullOrEmpty(item.Codigo) ? "—" : item.Codigo;
            var rectCod = new Rectangle(e.Bounds.X + 12, e.Bounds.Y + 10, 110, 24);
            using (var path = ObternerCaminoRedondeado(rectCod, 6))
            using (var brush = new SolidBrush(Color.FromArgb(30, 60, 70)))
                e.Graphics.FillPath(brush, path);

            TextRenderer.DrawText(
                e.Graphics, codigo,
                new Font("Segoe UI", 9, FontStyle.Bold),
                rectCod, accentGreen,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            // Nombre
            var rectNom = new Rectangle(e.Bounds.X + 132, e.Bounds.Y + 6, e.Bounds.Width - 260, 20);
            TextRenderer.DrawText(
                e.Graphics, item.Nombre,
                new Font("Segoe UI", 10, FontStyle.Bold),
                rectNom, textWhite,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            // Categoría + estado
            string sub = "";
            if (!string.IsNullOrEmpty(item.Categoria)) sub += item.Categoria;
            if (!string.IsNullOrEmpty(item.Estado))
                sub += (sub.Length > 0 ? "  •  " : "") + item.Estado;

            if (sub.Length > 0)
            {
                var rectSub = new Rectangle(e.Bounds.X + 132, e.Bounds.Y + 24, e.Bounds.Width - 260, 16);
                TextRenderer.DrawText(
                    e.Graphics, sub,
                    new Font("Segoe UI", 8.5f),
                    rectSub, textMuted,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            // Stock
            Color colStock = item.Stock <= 0
                ? Color.FromArgb(214, 40, 40)
                : (item.Stock < 3 ? Color.FromArgb(247, 127, 0) : accentGreen);

            var rectStock = new Rectangle(e.Bounds.Right - 100, e.Bounds.Y + 12, 88, 22);
            TextRenderer.DrawText(
                e.Graphics, $"Stock: {item.Stock}",
                new Font("Segoe UI", 9, FontStyle.Bold),
                rectStock, colStock,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
        }

        private void BuscarEquiposSugerencias(string texto)
        {
            sugerenciasActuales = new List<EquipoSugerencia>();

            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();

                string q;
                bool vacio = string.IsNullOrWhiteSpace(texto);

                if (vacio)
                {
                    q = @"
                        SELECT id, codigo, nombre, stock, estado, categoria
                        FROM equipos
                        WHERE stock > 0
                        ORDER BY 
                            CASE WHEN estado ILIKE '%disponible%' THEN 0 ELSE 1 END,
                            codigo
                        LIMIT 30";
                }
                else
                {
                    q = @"
                        SELECT id, codigo, nombre, stock, estado, categoria
                        FROM equipos
                        WHERE stock > 0
                          AND (
                              codigo ILIKE @patron 
                              OR nombre ILIKE @patron
                              OR categoria ILIKE @patron
                          )
                        ORDER BY
                            CASE 
                                WHEN codigo ILIKE @exacto THEN 0
                                WHEN codigo ILIKE @empieza THEN 1
                                WHEN nombre ILIKE @empieza THEN 2
                                WHEN codigo ILIKE @patron THEN 3
                                WHEN nombre ILIKE @patron THEN 4
                                WHEN categoria ILIKE @patron THEN 5
                                ELSE 6
                            END,
                            codigo
                        LIMIT 20";
                }

                using (NpgsqlCommand cmd = new NpgsqlCommand(q, con))
                {
                    if (!vacio)
                    {
                        cmd.Parameters.AddWithValue("@patron", "%" + texto + "%");
                        cmd.Parameters.AddWithValue("@exacto", texto);
                        cmd.Parameters.AddWithValue("@empieza", texto + "%");
                    }

                    using (NpgsqlDataReader rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            sugerenciasActuales.Add(new EquipoSugerencia
                            {
                                Id = Convert.ToInt32(rd["id"]),
                                Codigo = rd["codigo"]?.ToString() ?? "",
                                Nombre = rd["nombre"]?.ToString() ?? "",
                                Categoria = rd["categoria"]?.ToString() ?? "",
                                Estado = rd["estado"]?.ToString() ?? "",
                                Stock = rd["stock"] != DBNull.Value ? Convert.ToInt32(rd["stock"]) : 0
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error autocompletado: " + ex.Message);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }

            if (sugerenciasActuales.Count == 0)
            {
                if (!string.IsNullOrWhiteSpace(texto))
                {
                    lstSugerencias.Items.Clear();
                    lstSugerencias.Items.Add("(sin resultados)");

                    Point posCampoVacio = txtBuscar.PointToScreen(Point.Empty);
                    Point posPanelVacio = this.PointToClient(posCampoVacio);

                    panelSugerenciasContenedor.Location = new Point(posPanelVacio.X, posPanelVacio.Y + txtBuscar.Height + 34);
                    panelSugerenciasContenedor.Size = new Size(360, 50);
                    panelSugerenciasContenedor.Visible = true;
                    panelSugerenciasContenedor.BringToFront();
                }
                else
                {
                    OcultarSugerencias();
                }
                return;
            }

            lstSugerencias.Items.Clear();
            foreach (var s in sugerenciasActuales)
                lstSugerencias.Items.Add(s);

            Point posCampo = txtBuscar.PointToScreen(Point.Empty);
            Point posPanel = this.PointToClient(posCampo);

            int anchoLista = 360;
            int altoLista = Math.Min(320, sugerenciasActuales.Count * 44 + 4);

            panelSugerenciasContenedor.Location = new Point(posPanel.X, posPanel.Y + txtBuscar.Height + 34);
            panelSugerenciasContenedor.Size = new Size(anchoLista, altoLista);
            panelSugerenciasContenedor.Visible = true;
            panelSugerenciasContenedor.BringToFront();
            lstSugerencias.SelectedIndex = 0;
        }

        private void InsertarSugerenciaSeleccionada()
        {
            if (lstSugerencias.SelectedItem is EquipoSugerencia sug)
            {
                txtBuscar.Text = sug.Codigo;
                OcultarSugerencias();
                txtBuscar.Focus();
                txtBuscar.SelectionStart = txtBuscar.Text.Length;
            }
        }

        private void OcultarSugerencias()
        {
            if (panelSugerenciasContenedor != null)
            {
                panelSugerenciasContenedor.Visible = false;
                lstSugerencias?.Items.Clear();
            }
            sugerenciasActuales?.Clear();
        }

        // ═══════════════════════════════════════════
        // CELL PAINTING
        // ═══════════════════════════════════════════
        private void DgvInventario_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string headerText = dgvInventario.Columns[e.ColumnIndex].HeaderText;

            // Observaciones
            if (headerText.IndexOf("observación", StringComparison.OrdinalIgnoreCase) >= 0 ||
                headerText.IndexOf("observacion", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                string valor = dgvInventario.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "";
                e.PaintBackground(e.CellBounds, true);

                if (!string.IsNullOrWhiteSpace(valor) && valor != "—")
                {
                    TextRenderer.DrawText(e.Graphics, valor,
                        new Font("Segoe UI", 8.5f, FontStyle.Italic),
                        e.CellBounds, Color.FromArgb(245, 200, 100),
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.EndEllipsis | TextFormatFlags.PathEllipsis);
                }
                else
                {
                    TextRenderer.DrawText(e.Graphics, "—",
                        new Font("Segoe UI", 9f),
                        e.CellBounds, textMuted,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                }
                e.Handled = true;
                return;
            }

            // Estado con badges
            if (headerText.IndexOf("Estado", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                object cellValue = dgvInventario.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                string estado = cellValue?.ToString()?.Trim() ?? "";
                if (string.IsNullOrEmpty(estado)) return;

                e.PaintBackground(e.CellBounds, true);

                string estadoLower = estado.ToLower();
                Color badgeBg = Color.FromArgb(40, 60, 70);
                Color badgeText = textWhite;

                if (estadoLower == "disponible")
                    badgeBg = Color.FromArgb(16, 185, 129);
                else if (estadoLower == "en evento" || estadoLower == "evento")
                    badgeBg = Color.FromArgb(139, 92, 246);
                else if (estadoLower == "dañado" || estadoLower == "en mantenimiento" || estadoLower == "mantenimiento")
                    badgeBg = Color.FromArgb(239, 68, 68);
                else if (estadoLower == "reservado")
                    badgeBg = Color.FromArgb(247, 127, 0);
                else if (estadoLower == "en observación" ||
                         estadoLower == "en observacion" ||
                         estadoLower == "con observaciones")
                    badgeBg = Color.FromArgb(245, 158, 11);

                Rectangle badgeRect = new Rectangle(
                    e.CellBounds.X + 6,
                    e.CellBounds.Y + (e.CellBounds.Height - 22) / 2,
                    Math.Min(e.CellBounds.Width - 12, 100),
                    22
                );

                using (GraphicsPath path = ObternerCaminoRedondeado(badgeRect, 10))
                using (SolidBrush brush = new SolidBrush(badgeBg))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                }

                TextRenderer.DrawText(e.Graphics, estado,
                    new Font("Segoe UI", 8f, FontStyle.Bold),
                    badgeRect, badgeText,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                e.Handled = true;
            }
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════
        private ComboBox CrearComboBoxFiltro(string[] opciones)
        {
            ComboBox cb = new ComboBox
            {
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 8.5f),
                BackColor = bgCard,
                ForeColor = textWhite,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(4, 0, 4, 0)
            };
            cb.Items.AddRange(opciones);
            cb.SelectedIndex = 0;
            return cb;
        }

        // ═══════════════════════════════════════════════════
        // VERSIÓN 1: Sin lblSubOut (para los 5 KPIs normales)
        // ═══════════════════════════════════════════════════
        private Panel CrearTarjetaKPI(string titulo, string subtitulo, string valorInicial,
            Color colorIcono, string icono, out Label lblValOut)
        {
            return CrearTarjetaKPI(titulo, subtitulo, valorInicial, colorIcono, icono,
                out lblValOut, out _);
        }

        // ═══════════════════════════════════════════════════
        // VERSIÓN 2: Con lblSubOut (para el KPI "Próximos retornos")
        // ═══════════════════════════════════════════════════
        private Panel CrearTarjetaKPI(string titulo, string subtitulo, string valorInicial,
            Color colorIcono, string icono, out Label lblValOut, out Label lblSubOut)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgCard,
                Margin = new Padding(5)
            };
            AplicarBordesRedondeados(card, 12);
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(bordeSuave, 1.2f))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var p = ObternerCaminoRedondeado(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 12))
                        e.Graphics.DrawPath(pen, p);
                }
            };

            // ── Ícono a la izquierda ──
            Panel iconBg = new Panel
            {
                Size = new Size(40, 40),
                Location = new Point(16, 14),
                BackColor = colorIcono
            };
            AplicarBordesRedondeados(iconBg, 10);

            Label lblIcon = new Label
            {
                Text = icono,
                Font = new Font("Segoe UI", 13f),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            iconBg.Controls.Add(lblIcon);

            // ── Título ──
            Label lblTitle = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = textMuted,
                Location = new Point(66, 14),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            // ── Sub-título (solo UNA vez) ──
            Label lblSub = new Label
            {
                Text = subtitulo,
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(90, 120, 130),
                Location = new Point(66, 29),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            // ── Valor grande ──
            Label lblVal = new Label
            {
                Text = valorInicial,
                Font = new Font("Segoe UI", 22f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(62, 46),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            lblValOut = lblVal;
            lblSubOut = lblSub;

            card.Controls.AddRange(new Control[] { iconBg, lblTitle, lblSub, lblVal });
            return card;
        }

        private void CargarFiltroTecnicos()
        {
            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();
                NpgsqlCommand cmd = new NpgsqlCommand("SELECT nombre FROM trabajadores ORDER BY nombre ASC", con);
                NpgsqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                    cmbTecnico.Items.Add(reader["nombre"].ToString());
            }
            catch { }
            finally { conexionDB.CerrarConexion(); }
        }

        private void CargarFiltroCategorias()
        {
            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();
                NpgsqlCommand cmd = new NpgsqlCommand("SELECT DISTINCT categoria FROM equipos WHERE categoria IS NOT NULL AND categoria != ''", con);
                NpgsqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                    cmbCategoria.Items.Add(reader["categoria"].ToString());
            }
            catch { }
            finally { conexionDB.CerrarConexion(); }
        }

        private void CalcularMetricasKPI()
        {
            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();

                // ─── Parte 1: KPIs básicos (equipos) ───
                string sqlEquipos = @"
            SELECT 
                COUNT(*) AS total_equipos,
                COALESCE(SUM(CASE WHEN LOWER(TRIM(estado)) = 'en evento' THEN stock ELSE 0 END), 0) AS en_evento,
                COALESCE(SUM(CASE WHEN LOWER(TRIM(estado)) = 'disponible' THEN stock ELSE 0 END), 0) AS disponibles,
                COALESCE(SUM(CASE WHEN LOWER(TRIM(estado)) = 'reservado' THEN stock ELSE 0 END), 0) AS reservados,
                COALESCE(SUM(CASE WHEN LOWER(TRIM(estado)) IN 
                    ('dañado','en mantenimiento','mantenimiento','con observaciones','en observación','en observacion') 
                    THEN stock ELSE 0 END), 0) AS requieren_atencion
            FROM equipos";

                using (NpgsqlCommand cmd = new NpgsqlCommand(sqlEquipos, con))
                using (NpgsqlDataReader rd = cmd.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        lblValTotal.Text = rd["total_equipos"].ToString();
                        lblValEvento.Text = rd["en_evento"].ToString();
                        lblValDisponibles.Text = rd["disponibles"].ToString();
                        lblValReservadas.Text = rd["reservados"].ToString();
                        lblValDanados.Text = rd["requieren_atencion"].ToString();
                    }
                }

                // ─── Parte 2: Próximos retornos (equipos pendientes) ───
                lblValDevuelvenHoy.Text = "0";
                if (lblSubDevuelvenHoy != null) lblSubDevuelvenHoy.Text = "Sin retornos pendientes";

                try
                {
                    // Cuenta equipos que están fuera y cuyo evento aún no se ha finalizado.
                    // Trae también la fecha+hora mínima de retorno pendiente.
                    string sqlProximos = @"
        SELECT 
            COUNT(DISTINCT eq.id) AS equipos_pendientes,
            MIN(ev.fecha) AS fecha_min
        FROM eventos ev
        INNER JOIN salidas s ON s.id_evento = ev.id
        INNER JOIN salida_equipos se ON se.id_salida = s.id_salida
        INNER JOIN equipos eq ON eq.id = se.id_equipo
        WHERE COALESCE(LOWER(s.estado), '') <> 'finalizada'
          AND ev.fecha >= CURRENT_DATE";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(sqlProximos, con))
                    using (NpgsqlDataReader rd = cmd.ExecuteReader())
                    {
                        if (rd.Read())
                        {
                            int equipos = rd["equipos_pendientes"] != DBNull.Value
                                ? Convert.ToInt32(rd["equipos_pendientes"]) : 0;

                            lblValDevuelvenHoy.Text = equipos.ToString();

                            if (equipos == 0)
                            {
                                if (lblSubDevuelvenHoy != null)
                                    lblSubDevuelvenHoy.Text = "Sin retornos pendientes";
                            }
                            else if (rd["fecha_min"] != DBNull.Value)
                            {
                                // 🔥 La columna es DATE → Npgsql la devuelve como DateOnly
                                object valorFecha = rd["fecha_min"];

                                DateTime fecha;
                                if (valorFecha is DateOnly d)
                                    fecha = d.ToDateTime(TimeOnly.MinValue);
                                else if (valorFecha is DateTime dt)
                                    fecha = dt;
                                else
                                    fecha = Convert.ToDateTime(valorFecha);

                                string textoFecha;

                                if (fecha.Date == DateTime.Today)
                                    textoFecha = "Hoy";
                                else if (fecha.Date == DateTime.Today.AddDays(1))
                                    textoFecha = "Mañana";
                                else
                                    textoFecha = "Próx: " + fecha.ToString("dd/MM/yyyy");

                                if (lblSubDevuelvenHoy != null)
                                    lblSubDevuelvenHoy.Text = textoFecha;
                            }
                        }
                    }
                }
                catch (Exception exHoy)
                {
                    System.Diagnostics.Debug.WriteLine("Error KPI próximos retornos: " + exHoy.Message);
                    lblValDevuelvenHoy.Text = "0";
                    if (lblSubDevuelvenHoy != null) lblSubDevuelvenHoy.Text = "Error al cargar";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error KPIs: " + ex.Message);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }
        }

        private void CargarInventarioEquipos()
        {
            // Ocultar sugerencias al recargar
            OcultarSugerencias();

            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();

                string query = @"
                    SELECT 
                        e.id AS ""ID"",
                        e.codigo AS ""Código"",
                        e.nombre AS ""Equipo"",
                        e.categoria AS ""Categoría"",
                        e.ubicacion AS ""Ubicación"",
                        e.stock AS ""Stock"",
                        e.estado AS ""Estado"",
                        COALESCE(r.observaciones, '') AS ""Últ. observación"",
                        COALESCE(TO_CHAR(r.fecha_retorno, 'DD/MM/YYYY'), '') AS ""Últ. retorno""
                    FROM equipos e
                    LEFT JOIN retornos r ON r.id_equipo = e.id
                        AND r.fecha_retorno = (
                            SELECT MAX(r2.fecha_retorno) 
                            FROM retornos r2 
                            WHERE r2.id_equipo = e.id
                        )
                    WHERE 1=1";

                if (txtBuscar != null && !string.IsNullOrWhiteSpace(txtBuscar.Text))
                    query += " AND (e.nombre ILIKE @busqueda OR e.codigo ILIKE @busqueda OR e.categoria ILIKE @busqueda)";

                if (cmbCategoria != null && cmbCategoria.SelectedIndex > 0)
                    query += " AND e.categoria = @categoria";

                if (cmbEstado != null && cmbEstado.SelectedIndex > 0)
                    query += " AND e.estado = @estado";

                query += " ORDER BY e.id ASC";

                NpgsqlCommand cmd = new NpgsqlCommand(query, con);

                if (txtBuscar != null && !string.IsNullOrWhiteSpace(txtBuscar.Text))
                    cmd.Parameters.AddWithValue("@busqueda", "%" + txtBuscar.Text.Trim() + "%");

                if (cmbCategoria != null && cmbCategoria.SelectedIndex > 0)
                    cmd.Parameters.AddWithValue("@categoria", cmbCategoria.SelectedItem.ToString());

                if (cmbEstado != null && cmbEstado.SelectedIndex > 0)
                    cmd.Parameters.AddWithValue("@estado", cmbEstado.SelectedItem.ToString());

                NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                dgvInventario.DataSource = dt;

                if (dgvInventario.Columns.Contains("ID"))
                    dgvInventario.Columns["ID"].FillWeight = 30;
                if (dgvInventario.Columns.Contains("Código"))
                    dgvInventario.Columns["Código"].FillWeight = 70;
                if (dgvInventario.Columns.Contains("Equipo"))
                    dgvInventario.Columns["Equipo"].FillWeight = 100;
                if (dgvInventario.Columns.Contains("Categoría"))
                    dgvInventario.Columns["Categoría"].FillWeight = 60;
                if (dgvInventario.Columns.Contains("Ubicación"))
                    dgvInventario.Columns["Ubicación"].FillWeight = 55;
                if (dgvInventario.Columns.Contains("Stock"))
                    dgvInventario.Columns["Stock"].FillWeight = 35;
                if (dgvInventario.Columns.Contains("Estado"))
                    dgvInventario.Columns["Estado"].FillWeight = 60;
                if (dgvInventario.Columns.Contains("Últ. retorno"))
                    dgvInventario.Columns["Últ. retorno"].FillWeight = 55;
                if (dgvInventario.Columns.Contains("Últ. observación"))
                    dgvInventario.Columns["Últ. observación"].FillWeight = 120;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar inventario:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }
        }

        private void FiltrarEquiposDevuelvenHoy()
        {
            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();

                string query = @"SELECT 
                            id AS ""ID Evento"", 
                            titulo AS ""Evento"", 
                            fecha AS ""Fecha"", 
                            horario AS ""Horario"", 
                            detalles AS ""Detalles""
                         FROM eventos 
                         WHERE DATE(fecha) = CURRENT_DATE";

                NpgsqlCommand cmd = new NpgsqlCommand(query, con);
                NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    string mensaje = "Equipos que devuelven hoy:\n\n";
                    foreach (DataRow row in dt.Rows)
                    {
                        mensaje += $"ID: {row["ID Evento"]}\n";
                        mensaje += $"Evento: {row["Evento"]}\n";
                        mensaje += $"Horario: {row["Horario"]}\n";
                        mensaje += $"Detalles: {row["Detalles"]}\n";
                        mensaje += "-----------------------------\n";
                    }
                    MessageBox.Show(mensaje, "Devoluciones de Hoy", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("No hay equipos que devuelvan hoy.", "Devoluciones de Hoy", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al consultar devoluciones de hoy: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }
        }

        private GraphicsPath ObternerCaminoRedondeado(Rectangle rect, int radio)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radio * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void AplicarBordesRedondeados(Control control, int radio)
        {
            Action recalcularRegion = () =>
            {
                if (control.Width <= 0 || control.Height <= 0) return;
                using (GraphicsPath path = ObternerCaminoRedondeado(new Rectangle(0, 0, control.Width, control.Height), radio))
                {
                    control.Region = new Region(path);
                }
            };
            control.Resize += (s, e) => recalcularRegion();
            recalcularRegion();
        }
    }
}