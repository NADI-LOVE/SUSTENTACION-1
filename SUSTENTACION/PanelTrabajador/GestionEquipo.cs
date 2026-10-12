using MySql.Data.MySqlClient;
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
            CargarFiltroTecnicos();
            CargarFiltroCategorias();
            CargarInventarioEquipos();
            CalcularMetricasKPI();
        }

        private void InitializeComponentes()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = bgMain;
            this.Padding = new Padding(20);

            // Estructura limpia de 4 filas
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));  // 1. Header Título
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 180f)); // 2. Tarjetas KPI
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));  // 3. Barra de Búsqueda y Filtros
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // 4. Tabla Inventario

            // ==========================================
            // 1. HEADER TÍTULO
            // ==========================================
            Panel headerPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 5) };

            Label lblTitulo = new Label
            {
                Text = "Inventory",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(0, 0),
                AutoSize = true
            };

            headerPanel.Controls.Add(lblTitulo);

            // TARJETAS KPI
            TableLayoutPanel kpiLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Margin = new Padding(0, 0, 0, 10)
            };
            kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            kpiLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            kpiLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            Panel cardTotal = CrearTarjetaKPI("TOTAL UNIDADES", "0", Color.FromArgb(59, 130, 246), "📦", out lblValTotal);
            Panel cardEvento = CrearTarjetaKPI("EQUIPOS EN EVENTO (FUERA)", "0", Color.FromArgb(139, 92, 246), "🚚", out lblValEvento);
            Panel cardDisp = CrearTarjetaKPI("UNIDADES DISPONIBLES", "0", Color.FromArgb(16, 185, 129), "✅", out lblValDisponibles);
            Panel cardRes = CrearTarjetaKPI("UNIDADES RESERVADAS", "0", Color.FromArgb(245, 158, 11), "⏳", out lblValReservadas);
            Panel cardDan = CrearTarjetaKPI("EQUIPOS DAÑADOS / REVISIÓN", "0", Color.FromArgb(239, 68, 68), "⚠️", out lblValDanados);
            Panel cardDev = CrearTarjetaKPI("DEVUELVEN HOY", "0", Color.FromArgb(168, 85, 247), "🕒", out lblValDevuelvenHoy);

            cardDev.Cursor = Cursors.Hand;
            cardDev.Click += (s, e) => FiltrarEquiposDevuelvenHoy();

            kpiLayout.Controls.Add(cardTotal, 0, 0);
            kpiLayout.Controls.Add(cardEvento, 1, 0);
            kpiLayout.Controls.Add(cardDisp, 2, 0);
            kpiLayout.Controls.Add(cardRes, 0, 1);
            kpiLayout.Controls.Add(cardDan, 1, 1);
            kpiLayout.Controls.Add(cardDev, 2, 1);

            // ==========================================
            // 3. BARRA DE BÚSQUEDA Y FILTROS (Debajo de los KPI)
            // ==========================================
            Panel pnlBarraHerramientas = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 5)
            };

            // Buscador a la izquierda
            txtBuscar = new TextBox
            {
                Location = new Point(0, 5),
                Width = 240,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.Gray,
                Text = "Buscar en inventario..."
            };

            txtBuscar.Enter += (s, e) =>
            {
                if (txtBuscar.Text == "Buscar en inventario...")
                {
                    txtBuscar.Text = "";
                    txtBuscar.ForeColor = textWhite;
                }
            };

            txtBuscar.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtBuscar.Text))
                {
                    txtBuscar.Text = "Buscar en inventario...";
                    txtBuscar.ForeColor = Color.Gray;
                }
            };

            txtBuscar.TextChanged += (s, e) => CargarInventarioEquipos();

            // Filtros desplegables alineados a la derecha
            FlowLayoutPanel pnlBarraFiltros = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true
            };

            cmbCategoria = CrearComboBoxFiltro(new string[] { "Categoría: Todas" });
            cmbEstado = CrearComboBoxFiltro(new string[] { "Estado: Todos", "Disponible", "En Evento", "En Mantenimiento", "Dañado" });
            cmbTecnico = CrearComboBoxFiltro(new string[] { "Técnico: Todos" });

            cmbCategoria.SelectedIndexChanged += (s, e) => CargarInventarioEquipos();
            cmbEstado.SelectedIndexChanged += (s, e) => CargarInventarioEquipos();
            cmbTecnico.SelectedIndexChanged += (s, e) => CargarInventarioEquipos();

            pnlBarraFiltros.Controls.Add(cmbTecnico);
            pnlBarraFiltros.Controls.Add(cmbEstado);
            pnlBarraFiltros.Controls.Add(cmbCategoria);

            pnlBarraHerramientas.Controls.Add(txtBuscar);
            pnlBarraHerramientas.Controls.Add(pnlBarraFiltros);

            // ==========================================
            // 4. TABLA DE INVENTARIO
            // ==========================================
            Panel containerTabla = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgCard,
                Padding = new Padding(12)
            };
            AplicarBordesRedondeados(containerTabla, 12);

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
                ColumnHeadersHeight = 40,
                RowTemplate = { Height = 45 },
                EnableHeadersVisualStyles = false
            };

            typeof(DataGridView).InvokeMember("DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null, dgvInventario, new object[] { true });

            dgvInventario.ColumnHeadersDefaultCellStyle.BackColor = bgHeaderTable;
            dgvInventario.ColumnHeadersDefaultCellStyle.ForeColor = textMuted;
            dgvInventario.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);

            dgvInventario.DefaultCellStyle.BackColor = bgCard;
            dgvInventario.DefaultCellStyle.ForeColor = textWhite;
            dgvInventario.DefaultCellStyle.Font = new Font("Segoe UI", 9f);
            dgvInventario.DefaultCellStyle.SelectionBackColor = bgRowHover;
            dgvInventario.DefaultCellStyle.SelectionForeColor = accentGreen;
            dgvInventario.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);

            // SOLO CellPainting, sin CellFormatting
            dgvInventario.CellPainting += DgvInventario_CellPainting;

            containerTabla.Controls.Add(dgvInventario);

            // Integración en el layout principal
            mainLayout.Controls.Add(headerPanel, 0, 0);
            mainLayout.Controls.Add(kpiLayout, 0, 1);
            mainLayout.Controls.Add(pnlBarraHerramientas, 0, 2);
            mainLayout.Controls.Add(containerTabla, 0, 3);

            this.Controls.Add(mainLayout);
        }

        private void DgvInventario_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                string headerText = dgvInventario.Columns[e.ColumnIndex].HeaderText;

                // Verificamos si es la columna de Estado
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
                Width = 190,
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
            Panel card = new Panel { Dock = DockStyle.Fill, BackColor = bgCard, Margin = new Padding(4) };
            AplicarBordesRedondeados(card, 8);

            Panel iconBg = new Panel { Size = new Size(34, 34), Location = new Point(12, 12), BackColor = colorIcono };
            AplicarBordesRedondeados(iconBg, 6);

            Label lblIcon = new Label { Text = icono, Font = new Font("Segoe UI", 9f), ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            iconBg.Controls.Add(lblIcon);

            Label lblTitle = new Label { Text = titulo, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = textMuted, Location = new Point(54, 12), AutoSize = true };
            Label lblVal = new Label { Text = valorInicial, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = textWhite, Location = new Point(54, 26), AutoSize = true };

            lblValOut = lblVal;
            card.Controls.AddRange(new Control[] { iconBg, lblTitle, lblVal });
            return card;
        }

        // ==========================================
        // LÓGICA Y CÁLCULOS DE BASE DE DATOS MYSQL
        // ==========================================

        private void CalcularMetricasKPI()
        {
            Conexion conexionDB = new Conexion();
            try
            {
                MySqlConnection con = conexionDB.ObtenerConexion();

                string sqlEquipos = @"SELECT 
                                        IFNULL(SUM(stock), 0) AS total,
                                        IFNULL(SUM(CASE WHEN estado = 'Disponible' THEN stock ELSE 0 END), 0) AS disponibles,
                                        IFNULL(SUM(CASE WHEN estado = 'En Evento' THEN stock ELSE 0 END), 0) AS en_evento,
                                        IFNULL(SUM(CASE WHEN estado = 'Reservado' THEN stock ELSE 0 END), 0) AS reservados,
                                        IFNULL(SUM(CASE WHEN estado IN ('Dañado', 'En Mantenimiento') THEN stock ELSE 0 END), 0) AS danados
                                     FROM equipos";

                MySqlCommand cmdEq = new MySqlCommand(sqlEquipos, con);
                using (MySqlDataReader rd = cmdEq.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        lblValTotal.Text = rd["total"].ToString();
                        lblValDisponibles.Text = rd["disponibles"].ToString();
                        lblValEvento.Text = rd["en_evento"].ToString();
                        lblValReservadas.Text = rd["reservados"].ToString();
                        lblValDanados.Text = rd["danados"].ToString();
                    }
                }

                string sqlHoy = "SELECT COUNT(*) FROM eventos WHERE fecha = CURDATE()";
                MySqlCommand cmdHoy = new MySqlCommand(sqlHoy, con);
                lblValDevuelvenHoy.Text = cmdHoy.ExecuteScalar().ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al calcular tarjetas KPI: " + ex.Message, "Error MySQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }
        }

        private void CargarFiltroTecnicos()
        {
            Conexion conexionDB = new Conexion();
            try
            {
                MySqlConnection con = conexionDB.ObtenerConexion();
                MySqlCommand cmd = new MySqlCommand("SELECT nombre FROM trabajadores ORDER BY nombre ASC", con);
                MySqlDataReader reader = cmd.ExecuteReader();
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
                MySqlConnection con = conexionDB.ObtenerConexion();
                MySqlCommand cmd = new MySqlCommand("SELECT DISTINCT categoria FROM equipos WHERE categoria IS NOT NULL AND categoria != ''", con);
                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                    cmbCategoria.Items.Add(reader["categoria"].ToString());
            }
            catch { }
            finally { conexionDB.CerrarConexion(); }
        }

        private void CargarInventarioEquipos()
        {
            Conexion conexionDB = new Conexion();
            try
            {
                MySqlConnection con = conexionDB.ObtenerConexion();

                string query = @"SELECT 
                    id AS 'ID', 
                    codigo AS 'Código / SKU', 
                    nombre AS 'Equipo / Modelo', 
                    categoria AS 'Categoría', 
                    ubicacion AS 'Ubicación', 
                    stock AS 'Stock', 
                    estado AS 'Estado', 
                    descripcion AS 'Descripción' 
                 FROM equipos WHERE 1=1";

                if (cmbCategoria != null && cmbCategoria.SelectedIndex > 0)
                    query += " AND categoria = @categoria";

                if (cmbEstado != null && cmbEstado.SelectedIndex > 0)
                    query += " AND estado = @estado";

                if (txtBuscar != null && !string.IsNullOrWhiteSpace(txtBuscar.Text) && txtBuscar.Text != "Buscar en inventario..." && txtBuscar.Text != "Buscar equipo...")
                {
                    query += " AND (codigo LIKE @busqueda OR nombre LIKE @busqueda OR ubicacion LIKE @busqueda OR descripcion LIKE @busqueda)";
                }

                query += " ORDER BY id ASC";

                MySqlCommand cmd = new MySqlCommand(query, con);

                if (cmbCategoria != null && cmbCategoria.SelectedIndex > 0)
                    cmd.Parameters.AddWithValue("@categoria", cmbCategoria.SelectedItem.ToString());

                if (cmbEstado != null && cmbEstado.SelectedIndex > 0)
                    cmd.Parameters.AddWithValue("@estado", cmbEstado.SelectedItem.ToString());

                if (txtBuscar != null && !string.IsNullOrWhiteSpace(txtBuscar.Text) && txtBuscar.Text != "Buscar en inventario..." && txtBuscar.Text != "Buscar equipo...")
                    cmd.Parameters.AddWithValue("@busqueda", "%" + txtBuscar.Text.Trim() + "%");

                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                dgvInventario.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar inventario: " + ex.Message, "Error MySQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MySqlConnection con = conexionDB.ObtenerConexion();

                string query = @"SELECT 
                            id AS 'ID Evento', 
                            titulo AS 'Evento', 
                            fecha AS 'Fecha', 
                            horario AS 'Horario', 
                            detalles AS 'Detalles'
                         FROM eventos 
                         WHERE DATE(fecha) = CURDATE()";

                MySqlCommand cmd = new MySqlCommand(query, con);
                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
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