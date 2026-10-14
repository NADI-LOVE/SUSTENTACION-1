using Npgsql;
using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
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

        private DataGridView dgvInventario;
        private ComboBox cmbCategoria, cmbEstado, cmbTecnico;
        private TextBox txtBuscar;

        // Metricas KPI
        private Label lblValTotal, lblValEvento, lblValDisponibles, lblValReservadas, lblValDanados, lblValDevuelvenHoy;

        public GestionEquipo()
        {
            InitializeComponentes();

            // 🔥 Cargar datos en segundo plano para no congelar la UI
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
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 200f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

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

            // Buscador mejorado
            Panel panelBuscar = new Panel
            {
                Size = new Size(280, 42),
                Location = new Point(340, 12),
                BackColor = bgCard,
                Padding = new Padding(14, 0, 14, 0)
            };
            AplicarBordesRedondeados(panelBuscar, 10);

            Label lblIconBuscar = new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI", 12),
                ForeColor = textMuted,
                Location = new Point(12, 11),
                AutoSize = true,
                BackColor = bgCard
            };
            panelBuscar.Controls.Add(lblIconBuscar);

            txtBuscar = new TextBox
            {
                Location = new Point(42, 11),
                Width = 225,
                BackColor = bgCard,
                ForeColor = textWhite,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10f),
                Text = "Buscar equipo, código o categoría..."
            };
            txtBuscar.GotFocus += (s, e) => { if (txtBuscar.Text.StartsWith("Buscar")) txtBuscar.Text = ""; };
            txtBuscar.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txtBuscar.Text)) txtBuscar.Text = "Buscar equipo, código o categoría..."; };
            txtBuscar.TextChanged += (s, e) => CargarInventarioEquipos();
            panelBuscar.Controls.Add(txtBuscar);

            // Filtros mejorados
            FlowLayoutPanel pnlFiltros = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                WrapContents = false,
                Padding = new Padding(0, 8, 0, 0)
            };

            cmbCategoria = CrearComboBoxFiltro(new string[] { "Categoría: Todas" });
            cmbEstado = CrearComboBoxFiltro(new string[] { "Estado: Todos", "Disponible", "En Evento", "En Mantenimiento", "En Observación", "Con Observaciones", "Dañado" });
            cmbTecnico = CrearComboBoxFiltro(new string[] { "Técnico: Todos" });

            cmbCategoria.SelectedIndexChanged += (s, e) => CargarInventarioEquipos();
            cmbEstado.SelectedIndexChanged += (s, e) => CargarInventarioEquipos();
            cmbTecnico.SelectedIndexChanged += (s, e) => CargarInventarioEquipos();

            pnlFiltros.Controls.AddRange(new Control[] { cmbCategoria, cmbEstado, cmbTecnico });

            headerPanel.Controls.Add(lblTitulo);
            headerPanel.Controls.Add(lblSubtitulo);
            headerPanel.Controls.Add(panelBuscar);
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

            Panel cardTotal = CrearTarjetaKPI("TOTAL UNIDADES", "0", Color.FromArgb(0, 180, 216), "📦", out lblValTotal);
            Panel cardEvento = CrearTarjetaKPI("EQUIPOS EN EVENTO", "0", Color.FromArgb(139, 92, 246), "🚚", out lblValEvento);
            Panel cardDisp = CrearTarjetaKPI("UNIDADES DISPONIBLES", "0", accentGreen, "✅", out lblValDisponibles);
            Panel cardRes = CrearTarjetaKPI("UNIDADES RESERVADAS", "0", Color.FromArgb(247, 127, 0), "⏳", out lblValReservadas);
            Panel cardDan = CrearTarjetaKPI("EQUIPOS DAÑADOS", "0", Color.FromArgb(214, 40, 40), "⚠️", out lblValDanados);
            Panel cardDev = CrearTarjetaKPI("DEVUELVEN HOY", "0", Color.FromArgb(72, 149, 239), "🕒", out lblValDevuelvenHoy);

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
        }

        private void DgvInventario_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                string headerText = dgvInventario.Columns[e.ColumnIndex].HeaderText;

                // 🔥 Columna de observaciones: se ve en color amarillo si tiene contenido
                if (headerText.IndexOf("observación", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    headerText.IndexOf("observacion", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string valor = dgvInventario.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "";

                    e.PaintBackground(e.CellBounds, true);

                    if (!string.IsNullOrWhiteSpace(valor) && valor != "—")
                    {
                        TextRenderer.DrawText(
                            e.Graphics,
                            valor,
                            new Font("Segoe UI", 8.5f, FontStyle.Italic),
                            e.CellBounds,
                            Color.FromArgb(245, 200, 100),
                            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                            TextFormatFlags.EndEllipsis | TextFormatFlags.PathEllipsis
                        );
                    }
                    else
                    {
                        TextRenderer.DrawText(
                            e.Graphics,
                            "—",
                            new Font("Segoe UI", 9f),
                            e.CellBounds,
                            textMuted,
                            TextFormatFlags.Left | TextFormatFlags.VerticalCenter
                        );
                    }

                    e.Handled = true;
                    return;
                }

                // Columna de Estado (badges)
                if (headerText.IndexOf("Estado", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Obtenemos el valor directamente de la fila del DataGridView
                    // Esto es más fiable que e.Value, que a veces está vacío por el formateo
                    object cellValue = dgvInventario.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                    string estado = cellValue?.ToString()?.Trim() ?? "";

                    if (string.IsNullOrEmpty(estado))
                        return;

                    // Pintamos el fondo de la celda primero
                    e.PaintBackground(e.CellBounds, true);

                    // Normalizamos el valor para comparar
                    string estadoLower = estado.ToLower();
                    Color badgeBg = Color.FromArgb(40, 60, 70); // Gris por defecto
                    Color badgeText = textWhite;

                    if (estadoLower == "disponible")
                        badgeBg = Color.FromArgb(16, 185, 129); // Verde
                    else if (estadoLower == "en evento" || estadoLower == "evento")
                        badgeBg = Color.FromArgb(139, 92, 246); // Morado
                    else if (estadoLower == "dañado" || estadoLower == "en mantenimiento" || estadoLower == "mantenimiento")
                        badgeBg = Color.FromArgb(239, 68, 68); // Rojo
                    else if (estadoLower == "reservado")
                        badgeBg = Color.FromArgb(247, 127, 0); // Naranja
                    else if (estadoLower == "en observación" ||
                             estadoLower == "en observacion" ||
                             estadoLower == "con observaciones")
                        badgeBg = Color.FromArgb(245, 158, 11); // Amarillo

                    // Dibujamos el badge redondeado
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

                    // Dibujamos el texto dentro del badge
                    TextRenderer.DrawText(
                        e.Graphics,
                        estado,
                        new Font("Segoe UI", 8f, FontStyle.Bold),
                        badgeRect,
                        badgeText,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                    );

                    // IMPORTANTE: Indicamos que ya hemos pintado todo, para que el DataGridView
                    // no dibuje el texto original encima del badge.
                    e.Handled = true;
                }
            }
        }

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

        private Panel CrearTarjetaKPI(string titulo, string valorInicial, Color colorIcono, string icono, out Label lblValOut)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgCard,
                Margin = new Padding(5)
            };
            AplicarBordesRedondeados(card, 12);

            // Ícono más grande
            Panel iconBg = new Panel
            {
                Size = new Size(52, 52),
                Location = new Point(22, 22),
                BackColor = colorIcono
            };
            AplicarBordesRedondeados(iconBg, 12);

            Label lblIcon = new Label
            {
                Text = icono,
                Font = new Font("Segoe UI", 16f),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            iconBg.Controls.Add(lblIcon);

            Label lblTitle = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = textMuted,
                Location = new Point(88, 24),
                AutoSize = true
            };

            Label lblVal = new Label
            {
                Text = valorInicial,
                Font = new Font("Segoe UI", 22f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(85, 44),
                AutoSize = true
            };

            lblValOut = lblVal;
            card.Controls.AddRange(new Control[] { iconBg, lblTitle, lblVal });
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

                string sqlEquipos = @"SELECT 
                    COUNT(*) AS total_equipos,
                    COALESCE(SUM(CASE WHEN LOWER(TRIM(estado)) = 'disponible' THEN stock ELSE 0 END), 0) AS disponibles,
                    COALESCE(SUM(CASE WHEN LOWER(TRIM(estado)) = 'en evento' THEN stock ELSE 0 END), 0) AS en_evento,
                    COALESCE(SUM(CASE WHEN LOWER(TRIM(estado)) = 'reservado' THEN stock ELSE 0 END), 0) AS reservados,
                    COALESCE(SUM(CASE WHEN LOWER(TRIM(estado)) IN ('dañado', 'en mantenimiento', 'mantenimiento') THEN stock ELSE 0 END), 0) AS danados
                 FROM equipos";

                NpgsqlCommand cmdEq = new NpgsqlCommand(sqlEquipos, con);
                using (NpgsqlDataReader rd = cmdEq.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        lblValTotal.Text = rd["total_equipos"].ToString();
                        lblValDisponibles.Text = rd["disponibles"].ToString();
                        lblValEvento.Text = rd["en_evento"].ToString();
                        lblValReservadas.Text = rd["reservados"].ToString();
                        lblValDanados.Text = rd["danados"].ToString();
                    }
                }

                string sqlHoy = "SELECT COUNT(*) FROM eventos WHERE DATE(fecha) = CURRENT_DATE";
                NpgsqlCommand cmdHoy = new NpgsqlCommand(sqlHoy, con);
                object resHoy = cmdHoy.ExecuteScalar();
                lblValDevuelvenHoy.Text = resHoy != null ? resHoy.ToString() : "0";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al calcular KPIs: " + ex.Message, "Error MySQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }
        }

        private void CargarInventarioEquipos()
        {
            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();

                // Query simple con LEFT JOIN
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

                if (txtBuscar != null &&
                    !string.IsNullOrWhiteSpace(txtBuscar.Text) &&
                    !txtBuscar.Text.StartsWith("Buscar"))
                {
                    query += " AND (e.nombre ILIKE @busqueda OR e.codigo ILIKE @busqueda OR e.categoria ILIKE @busqueda)";
                }

                if (cmbCategoria != null && cmbCategoria.SelectedIndex > 0)
                    query += " AND e.categoria = @categoria";

                if (cmbEstado != null && cmbEstado.SelectedIndex > 0)
                    query += " AND e.estado = @estado";

                query += " ORDER BY e.id ASC";

                NpgsqlCommand cmd = new NpgsqlCommand(query, con);

                if (txtBuscar != null &&
                    !string.IsNullOrWhiteSpace(txtBuscar.Text) &&
                    !txtBuscar.Text.StartsWith("Buscar"))
                {
                    cmd.Parameters.AddWithValue("@busqueda", "%" + txtBuscar.Text.Trim() + "%");
                }

                if (cmbCategoria != null && cmbCategoria.SelectedIndex > 0)
                    cmd.Parameters.AddWithValue("@categoria", cmbCategoria.SelectedItem.ToString());

                if (cmbEstado != null && cmbEstado.SelectedIndex > 0)
                    cmd.Parameters.AddWithValue("@estado", cmbEstado.SelectedItem.ToString());

                NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                // 🔍 DEBUG: ver cuántas filas vienen
                System.Diagnostics.Debug.WriteLine("FILAS: " + dt.Rows.Count);
                if (dt.Rows.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine("COLUMNAS: " + string.Join(", ", dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName)));
                }

                // 🔍 DEBUG: mostrar cuántas filas vienen
                MessageBox.Show(
                    $"Filas: {dt.Rows.Count}\n" +
                    $"Columnas: {dt.Columns.Count}\n" +
                    $"Nombres: {string.Join(", ", dt.Columns.Cast<System.Data.DataColumn>().Select(c => c.ColumnName))}",
                    "Debug Query");

                dgvInventario.DataSource = dt;

                // Ajustar anchos
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
                MessageBox.Show("Error al consultar devoluciones de hoy: " + ex.Message, "Error MySQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
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