using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace SUSTENTACION.PanelTrabajador
{
    public class GestionEquipo : UserControl
    {
        // Paleta de colores ajustada al diseño moderno
        private readonly Color bgMain = Color.FromArgb(245, 246, 250);
        private readonly Color cardBg = Color.White;
        private readonly Color textDark = Color.FromArgb(30, 41, 59);
        private readonly Color textGray = Color.FromArgb(100, 116, 139);
        private readonly Color primaryBlue = Color.FromArgb(14, 165, 233);

        private DataGridView dgvInventario;
        private ComboBox cmbCategoria, cmbEstado, cmbTecnico;

        // Referencias a los labels de valores en las 6 tarjetas KPI
        private Label lblValTotal, lblValEvento, lblValDisponibles, lblValReservadas, lblValDanados, lblValDevuelvenHoy;

        public GestionEquipo()
        {
            InitializeComponentes();
            CargarFiltroTecnicos();
            CargarFiltroCategorias();
            CargarInventarioEquipos();
            CalcularMetricasKPI(); // Carga de datos reales en las 6 tarjetas KPI
        }

        private void InitializeComponentes()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = bgMain;
            this.Padding = new Padding(20);

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f));  // Header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 180f)); // Tarjetas KPI
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Tabla

            // ==========================================
            // 1. HEADER SUPERIOR CON FILTROS DE CONTROL
            // ==========================================
            Panel headerPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10) };

            Panel iconTitle = new Panel
            {
                Size = new Size(40, 40),
                Location = new Point(0, 5),
                BackColor = primaryBlue
            };
            AplicarBordesRedondeados(iconTitle, 12);

            Label lblIcon = new Label
            {
                Text = "📽️",
                Font = new Font("Segoe UI", 12f),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            iconTitle.Controls.Add(lblIcon);

            Label lblTitulo = new Label
            {
                Text = "Control de Equipos para Eventos",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = textDark,
                Location = new Point(50, 2),
                AutoSize = true
            };

            Label lblSubtitulo = new Label
            {
                Text = "Registro de salidas, retornos y asignación de responsabilidad a técnicos",
                Font = new Font("Segoe UI", 9f),
                ForeColor = textGray,
                Location = new Point(50, 28),
                AutoSize = true
            };

            // Filtros alineados a la derecha
            FlowLayoutPanel pnlFiltros = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                WrapContents = false
            };

            cmbCategoria = CrearComboBoxFiltro(new string[] { "Categoría: Todas" });
            cmbEstado = CrearComboBoxFiltro(new string[] { "Estado: Todos", "Disponible", "En Evento", "En Mantenimiento", "Dañado" });
            cmbTecnico = CrearComboBoxFiltro(new string[] { "Técnico: Todos" });

            // Eventos de filtrado en tiempo real
            cmbCategoria.SelectedIndexChanged += (s, e) => CargarInventarioEquipos();
            cmbEstado.SelectedIndexChanged += (s, e) => CargarInventarioEquipos();
            cmbTecnico.SelectedIndexChanged += (s, e) => CargarInventarioEquipos();

            pnlFiltros.Controls.AddRange(new Control[] { cmbCategoria, cmbEstado, cmbTecnico });

            headerPanel.Controls.Add(iconTitle);
            headerPanel.Controls.Add(lblTitulo);
            headerPanel.Controls.Add(lblSubtitulo);
            headerPanel.Controls.Add(pnlFiltros);

            // ==========================================
            // 2. MÉTRICAS / TARJETAS KPI (2 FILAS DE 3)
            // ==========================================
            TableLayoutPanel kpiLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Margin = new Padding(0, 0, 0, 15)
            };
            kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            kpiLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            kpiLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            // Creación con captura de Labels de valor dinámico
            Panel cardTotal = CrearTarjetaKPI("TOTAL UNIDADES", "0", Color.FromArgb(59, 130, 246), "📦", out lblValTotal);
            Panel cardEvento = CrearTarjetaKPI("EQUIPOS EN EVENTO (FUERA)", "0", Color.FromArgb(139, 92, 246), "🚚", out lblValEvento);
            Panel cardDisp = CrearTarjetaKPI("UNIDADES DISPONIBLES", "0", Color.FromArgb(16, 185, 129), "✅", out lblValDisponibles);
            Panel cardRes = CrearTarjetaKPI("UNIDADES RESERVADAS", "0", Color.FromArgb(245, 158, 11), "⏳", out lblValReservadas);
            Panel cardDan = CrearTarjetaKPI("EQUIPOS DAÑADOS / REVISIÓN", "0", Color.FromArgb(239, 68, 68), "⚠️", out lblValDanados);
            Panel cardDev = CrearTarjetaKPI("DEVUELVEN HOY", "0", Color.FromArgb(168, 85, 247), "🕒", out lblValDevuelvenHoy);

            // Evento interactivo para filtrar devoluciones del día
            cardDev.Cursor = Cursors.Hand;
            cardDev.Click += (s, e) => FiltrarEquiposDevuelvenHoy();

            kpiLayout.Controls.Add(cardTotal, 0, 0);
            kpiLayout.Controls.Add(cardEvento, 1, 0);
            kpiLayout.Controls.Add(cardDisp, 2, 0);
            kpiLayout.Controls.Add(cardRes, 0, 1);
            kpiLayout.Controls.Add(cardDan, 1, 1);
            kpiLayout.Controls.Add(cardDev, 2, 1);

            // ==========================================
            // 3. TABLA DE INVENTARIO Y SALIDAS
            // ==========================================
            Panel containerTabla = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = cardBg,
                Padding = new Padding(15)
            };
            AplicarBordesRedondeados(containerTabla, 16);

            dgvInventario = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = cardBg,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                ColumnHeadersHeight = 40,
                RowTemplate = { Height = 45 }
            };

            dgvInventario.EnableHeadersVisualStyles = false;
            dgvInventario.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
            dgvInventario.ColumnHeadersDefaultCellStyle.ForeColor = textGray;
            dgvInventario.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            dgvInventario.DefaultCellStyle.Font = new Font("Segoe UI", 9f);
            dgvInventario.DefaultCellStyle.SelectionBackColor = Color.FromArgb(243, 244, 246);
            dgvInventario.DefaultCellStyle.SelectionForeColor = textDark;

            containerTabla.Controls.Add(dgvInventario);

            // Integración final
            mainLayout.Controls.Add(headerPanel, 0, 0);
            mainLayout.Controls.Add(kpiLayout, 0, 1);
            mainLayout.Controls.Add(containerTabla, 0, 2);

            this.Controls.Add(mainLayout);
        }

        // ==========================================
        // COMPONENTES AUXILIARES DE DISEÑO
        // ==========================================

        private ComboBox CrearComboBoxFiltro(string[] opciones)
        {
            ComboBox cb = new ComboBox
            {
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f),
                Margin = new Padding(5, 5, 0, 0)
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
                BackColor = cardBg,
                Margin = new Padding(5)
            };
            AplicarBordesRedondeados(card, 12);

            Panel iconBg = new Panel
            {
                Size = new Size(38, 38),
                Location = new Point(15, 15),
                BackColor = colorIcono
            };
            AplicarBordesRedondeados(iconBg, 10);

            Label lblIcon = new Label
            {
                Text = icono,
                Font = new Font("Segoe UI", 11f),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            iconBg.Controls.Add(lblIcon);

            Label lblTitle = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = textGray,
                Location = new Point(62, 14),
                AutoSize = true
            };

            Label lblVal = new Label
            {
                Text = valorInicial,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = textDark,
                Location = new Point(62, 28),
                AutoSize = true
            };

            lblValOut = lblVal;

            card.Controls.Add(iconBg);
            card.Controls.Add(lblTitle);
            card.Controls.Add(lblVal);

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

                // 1. Obtención de sumas agrupadas desde la tabla 'equipos'
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

                // 2. Conteo de devoluciones para el día actual desde 'eventos'
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
                string query = "SELECT nombre FROM trabajadores ORDER BY nombre ASC";
                MySqlCommand cmd = new MySqlCommand(query, con);
                MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    cmbTecnico.Items.Add(reader["nombre"].ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar lista de trabajadores: " + ex.Message, "Error MySQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }
        }

        private void CargarFiltroCategorias()
        {
            Conexion conexionDB = new Conexion();
            try
            {
                MySqlConnection con = conexionDB.ObtenerConexion();
                string query = "SELECT DISTINCT categoria FROM equipos WHERE categoria IS NOT NULL AND categoria != '' ORDER BY categoria ASC";
                MySqlCommand cmd = new MySqlCommand(query, con);
                MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    cmbCategoria.Items.Add(reader["categoria"].ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar categorías: " + ex.Message, "Error MySQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MySqlConnection con = conexionDB.ObtenerConexion();

                string query = @"SELECT 
                                    id AS 'Cód.', 
                                    codigo AS 'Código', 
                                    nombre AS 'Equipo / Accesorio', 
                                    categoria AS 'Categoría', 
                                    ubicacion AS 'Ubicación', 
                                    stock AS 'Stock', 
                                    estado AS 'Estado', 
                                    descripcion AS 'Descripción / Notas' 
                                 FROM equipos WHERE 1=1";

                if (cmbCategoria != null && cmbCategoria.SelectedIndex > 0)
                    query += " AND categoria = @categoria";

                if (cmbEstado != null && cmbEstado.SelectedIndex > 0)
                    query += " AND estado = @estado";

                query += " ORDER BY id ASC";

                MySqlCommand cmd = new MySqlCommand(query, con);

                if (cmbCategoria != null && cmbCategoria.SelectedIndex > 0)
                    cmd.Parameters.AddWithValue("@categoria", cmbCategoria.SelectedItem.ToString());

                if (cmbEstado != null && cmbEstado.SelectedIndex > 0)
                    cmd.Parameters.AddWithValue("@estado", cmbEstado.SelectedItem.ToString());

                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                dgvInventario.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar los datos del inventario: " + ex.Message, "Error MySQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                                    id AS 'Cód.', 
                                    fecha AS 'Fecha Retorno', 
                                    titulo AS 'Evento / Registro', 
                                    horario AS 'Horario', 
                                    detalles AS 'Código Equipo'
                                 FROM eventos 
                                 WHERE fecha = CURDATE()";

                MySqlCommand cmd = new MySqlCommand(query, con);
                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                dgvInventario.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al filtrar devoluciones de hoy: " + ex.Message, "Error MySQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }
        }

        private void AplicarBordesRedondeados(Control control, int radio)
        {
            Action recalcularRegion = () =>
            {
                if (control.Width <= 0 || control.Height <= 0) return;

                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddArc(0, 0, radio, radio, 180, 90);
                    path.AddArc(control.Width - radio, 0, radio, radio, 270, 90);
                    path.AddArc(control.Width - radio, control.Height - radio, radio, radio, 0, 90);
                    path.AddArc(0, control.Height - radio, radio, radio, 90, 90);
                    path.CloseAllFigures();
                    control.Region = new Region(path);
                }
            };

            control.Resize += (s, e) => recalcularRegion();
            recalcularRegion();
        }
    }
}