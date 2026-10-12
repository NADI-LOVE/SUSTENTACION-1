using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using SUSTENTACION;

namespace SUSTENTACION.PanelMovimiento
{
    /// <summary>
    /// Panel de movimientos para el inventario de equipos audiovisuales de Artemusa TV.
    /// Lee el inventario y los responsables activos; guarda el responsable elegido
    /// junto con el movimiento en el esquema normalizado de Artemusa TV.
    /// </summary>
    public class PanelMovimiento : UserControl
    {
        private readonly Color bgCanvas = Color.FromArgb(243, 246, 250);
        private readonly Color bgCard = Color.White;
        private readonly Color accentGreen = Color.FromArgb(19, 125, 91);
        private readonly Color textMain = Color.FromArgb(26, 39, 54);
        private readonly Color textMuted = Color.FromArgb(106, 122, 140);
        private readonly Color lineColor = Color.FromArgb(226, 232, 239);

        private FlowLayoutPanel flowContainer = null!;
        private FlowLayoutPanel rowKPIs = null!;
        private Panel pnlHeader = null!;
        private Panel pnlToolbar = null!;
        private Panel pnlTablaCard = null!;
        private TextBox txtBuscar = null!;
        private ComboBox cmbTipo = null!;
        private DataGridView dgvMovimientos = null!;
        private Label lblEstadoConexion = null!;
        private readonly Dictionary<string, Label> valoresKpi = new Dictionary<string, Label>();
        private DataTable datosMovimientos = new DataTable();

        private static readonly OpcionTipo[] tiposMovimiento =
        {
            new OpcionTipo("INGRESO", "Ingreso"),
            new OpcionTipo("SALIDA", "Salida"),
            new OpcionTipo("TRASLADO", "Traslado"),
            new OpcionTipo("DEVOLUCION", "Devolución"),
            new OpcionTipo("MANTENIMIENTO", "Mantenimiento")
        };

        public PanelMovimiento()
        {
            BackColor = bgCanvas;
            Dock = DockStyle.Fill;
            DoubleBuffered = true;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            ConstruirInterfaz();
            Resize += (s, e) => AjustarTamanos();
            AjustarTamanos();
            RefrescarDatos(false);
        }

        private void ConstruirInterfaz()
        {
            flowContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(26, 22, 26, 24)
            };
            Controls.Add(flowContainer);

            ConstruirEncabezado();
            ConstruirIndicadores();
            ConstruirHerramientas();
            ConstruirTabla();
        }

        private void ConstruirEncabezado()
        {
            pnlHeader = new Panel
            {
                Height = 112,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 16)
            };

            Label lblMarca = new Label
            {
                Text = "ARTEMUSA TV / MOVIMIENTOS DE EQUIPOS",
                AutoSize = true,
                ForeColor = accentGreen,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Location = new Point(2, 2)
            };
            pnlHeader.Controls.Add(lblMarca);

            Label lblTitulo = new Label
            {
                Text = "Movimientos de equipos audiovisuales",
                AutoSize = true,
                ForeColor = textMain,
                Font = new Font("Segoe UI", 23f, FontStyle.Bold),
                Location = new Point(0, 25)
            };
            pnlHeader.Controls.Add(lblTitulo);

            Label lblSubtitulo = new Label
            {
                Text = "Entradas, salidas, traslados, devoluciones y mantenimiento de equipos.",
                AutoSize = true,
                ForeColor = textMuted,
                Font = new Font("Segoe UI", 9.2f),
                Location = new Point(3, 76)
            };

            pnlHeader.Controls.Add(lblSubtitulo);

            lblEstadoConexion = new Label
            {
                Text = "●  Verificando base de datos...",
                AutoSize = true,
                ForeColor = textMuted,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(600, 5)
            };
            pnlHeader.Controls.Add(lblEstadoConexion);

            Button btnRegistrar = CrearBoton("＋  Registrar movimiento", accentGreen, Color.White, 190, 44);
            btnRegistrar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRegistrar.Click += (s, e) => AbrirDialogoMovimiento(null);
            pnlHeader.Controls.Add(btnRegistrar);

            pnlHeader.Resize += (s, e) =>
            {
                btnRegistrar.Location = new Point(Math.Max(0, pnlHeader.Width - btnRegistrar.Width), 36);
                lblEstadoConexion.Location = new Point(
                    Math.Max(0, pnlHeader.Width - lblEstadoConexion.PreferredWidth), 8);
            };

            flowContainer.Controls.Add(pnlHeader);
        }

        private void ConstruirIndicadores()
        {
            rowKPIs = new FlowLayoutPanel
            {
                AutoSize = false,
                Height = 132,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 16)
            };
            rowKPIs.Controls.Add(CrearCardKPI(
    "ENTRADAS HOY",
    "ingresos",
    "Equipos ingresados hoy",
    accentGreen,
    "↙"
));

            rowKPIs.Controls.Add(CrearCardKPI(
                "SALIDAS HOY",
                "salidas",
                "Equipos retirados hoy",
                Color.FromArgb(193, 91, 47),
                "↗"
            ));

            rowKPIs.Controls.Add(CrearCardKPI(
                "DEVOLUCIONES HOY",
                "devolucionesHoy",
                "Equipos devueltos hoy",
                Color.FromArgb(28, 107, 185),
                "↩"
            ));

            rowKPIs.Controls.Add(CrearCardKPI(
                "MOVIMIENTOS DEL MES",
                "mes",
                "Movimientos registrados este mes",
                Color.FromArgb(119, 75, 190),
                "≡"
            ));



            flowContainer.Controls.Add(rowKPIs);
        }

        private void ConstruirHerramientas()
        {
            pnlToolbar = new Panel
            {
                Height = 78,
                BackColor = bgCard,
                Margin = new Padding(0, 0, 0, 16)
            };
            AplicarBordesRedondeados(pnlToolbar, 18);

            pnlToolbar.Controls.Add(new Label
            {
                Text = "Movimientos",
                ForeColor = textMain,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(17, 15)
            });
            pnlToolbar.Controls.Add(new Label
            {
                Text = "Búsqueda y filtros",
                ForeColor = textMuted,
                Font = new Font("Segoe UI", 8.2f),
                AutoSize = true,
                Location = new Point(18, 42)
            });

            Panel pnlBuscar = new Panel
            {
                Size = new Size(450, 42),
                Location = new Point(174, 18),
                BackColor = Color.FromArgb(245, 247, 250),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            AplicarBordesRedondeados(pnlBuscar, 10);

            pnlBuscar.Controls.Add(new Label
            {
                Text = "⌕",
                ForeColor = textMuted,
                Font = new Font("Segoe UI Symbol", 17f),
                Location = new Point(10, 4),
                Size = new Size(26, 32),
                TextAlign = ContentAlignment.MiddleCenter
            });

            txtBuscar = new TextBox
            {
                Text = "Buscar por equipo, código, responsable o ubicación...",
                Font = new Font("Segoe UI", 9.1f),
                ForeColor = textMuted,
                BackColor = Color.FromArgb(245, 247, 250),
                BorderStyle = BorderStyle.None,
                Location = new Point(42, 12),
                Size = new Size(390, 22),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            txtBuscar.Enter += (s, e) =>
            {
                if (txtBuscar.Text.StartsWith("Buscar por", StringComparison.OrdinalIgnoreCase))
                {
                    txtBuscar.Clear();
                    txtBuscar.ForeColor = textMain;
                }
            };
            txtBuscar.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtBuscar.Text))
                {
                    txtBuscar.Text = "Buscar por equipo, código, responsable o ubicación...";
                    txtBuscar.ForeColor = textMuted;
                }
            };
            txtBuscar.TextChanged += (s, e) => AplicarFiltros();
            pnlBuscar.Controls.Add(txtBuscar);
            pnlToolbar.Controls.Add(pnlBuscar);

            cmbTipo = new ComboBox
            {
                Size = new Size(180, 42),
                Location = new Point(0, 18),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = bgCard,
                ForeColor = textMain,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            cmbTipo.Items.Add("Todos los tipos");
            foreach (OpcionTipo tipo in tiposMovimiento)
                cmbTipo.Items.Add(tipo.Texto);
            cmbTipo.SelectedIndex = 0;
            cmbTipo.SelectedIndexChanged += (s, e) => AplicarFiltros();
            pnlToolbar.Controls.Add(cmbTipo);

            Button btnActualizar = CrearBoton("⟳", Color.FromArgb(238, 243, 247), textMain, 42, 42);
            btnActualizar.Font = new Font("Segoe UI Symbol", 15f, FontStyle.Bold);
            btnActualizar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnActualizar.Click += (s, e) => RefrescarDatos(true);
            pnlToolbar.Controls.Add(btnActualizar);

            pnlToolbar.Resize += (s, e) =>
            {
                btnActualizar.Location = new Point(pnlToolbar.Width - btnActualizar.Width - 14, 18);
                cmbTipo.Location = new Point(pnlToolbar.Width - btnActualizar.Width - cmbTipo.Width - 24, 18);
                pnlBuscar.Width = Math.Max(250, cmbTipo.Left - pnlBuscar.Left - 12);
                txtBuscar.Width = Math.Max(180, pnlBuscar.Width - 56);
            };

            flowContainer.Controls.Add(pnlToolbar);
        }

        private void ConstruirTabla()
        {
            pnlTablaCard = new Panel
            {
                Height = 430,
                BackColor = bgCard,
                Padding = new Padding(16, 70, 16, 14),
                Margin = new Padding(0)
            };
            AplicarBordesRedondeados(pnlTablaCard, 16);

            dgvMovimientos = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = bgCard,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = lineColor,
                EnableHeadersVisualStyles = false,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 48 },
                ShowCellToolTips = true
            };

            dgvMovimientos.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(240, 244, 248),
                ForeColor = textMuted,
                SelectionBackColor = Color.FromArgb(240, 244, 248),
                SelectionForeColor = textMuted,
                Font = new Font("Segoe UI", 8.3f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 4, 0)
            };
            dgvMovimientos.ColumnHeadersHeight = 42;

            dgvMovimientos.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = bgCard,
                ForeColor = textMain,
                SelectionBackColor = Color.FromArgb(226, 243, 235),
                SelectionForeColor = textMain,
                Font = new Font("Segoe UI", 8.8f),
                Padding = new Padding(8, 0, 4, 0),
                WrapMode = DataGridViewTriState.False
            };
            dgvMovimientos.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 251, 253);
            dgvMovimientos.CellPainting += PintarTipoMovimiento;
            dgvMovimientos.DataBindingComplete += (s, e) => AjustarColumnasVisuales();
            dgvMovimientos.CellDoubleClick += (s, e) => MostrarDetalleMovimiento(e.RowIndex);
            dgvMovimientos.CellToolTipTextNeeded += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                string column = dgvMovimientos.Columns[e.ColumnIndex].Name;
                if (column == "observacion" || column == "motivo")
                    e.ToolTipText = Convert.ToString(dgvMovimientos.Rows[e.RowIndex].Cells[e.ColumnIndex].Value) ?? "";
            };

            pnlTablaCard.Controls.Add(dgvMovimientos);
            pnlTablaCard.Controls.Add(new Label
            {
                Text = "Historial de movimientos",
                AutoSize = true,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = textMain,
                Location = new Point(19, 13)
            });
            pnlTablaCard.Controls.Add(new Label
            {
                Text = "Trazabilidad de equipos, ubicaciones y responsables",
                AutoSize = true,
                Font = new Font("Segoe UI", 8.3f),
                ForeColor = textMuted,
                Location = new Point(20, 40)
            });

            flowContainer.Controls.Add(pnlTablaCard);
        }

        private Panel CrearCardKPI(string titulo, string clave, string descripcion, Color color, string icono)
        {
            Panel card = new Panel
            {
                Size = new Size(240, 132),
                BackColor = bgCard
            };
            AplicarBordesRedondeados(card, 20);

            Label lblTitulo = new Label
            {
                Text = titulo,
                AutoSize = true,
                ForeColor = textMuted,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Location = new Point(17, 16)
            };
            Label lblValor = new Label
            {
                Text = "0",
                AutoSize = false,
                AutoEllipsis = true,
                ForeColor = textMain,
                Font = new Font("Segoe UI", 21f, FontStyle.Bold),
                Location = new Point(16, 43),
                Size = new Size(205, 42)
            };
            valoresKpi[clave] = lblValor;

            Label lblDescripcion = new Label
            {
                Text = descripcion,
                AutoSize = true,
                ForeColor = color,
                Font = new Font("Segoe UI", 8.1f),
                Location = new Point(18, 99)
            };
            Label lblIcono = new Label
            {
                Text = icono,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = color,
                BackColor = Color.FromArgb(
                    Math.Min(255, 235 + color.R / 13),
                    Math.Min(255, 235 + color.G / 13),
                    Math.Min(255, 235 + color.B / 13)),
                Font = new Font("Segoe UI Symbol", 12f, FontStyle.Bold),
                Size = new Size(30, 30),
                Location = new Point(card.Width - 44, 12)
            };
            AplicarBordesRedondeados(lblIcono, 10);

            card.Controls.AddRange(new Control[] { lblTitulo, lblValor, lblDescripcion, lblIcono });
            card.Resize += (s, e) =>
            {
                lblIcono.Location = new Point(card.Width - 44, 12);
                lblValor.Width = Math.Max(80, card.Width - 28);
            };
            card.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(color, 3))
                    e.Graphics.DrawLine(pen, 17, card.Height - 2, card.Width - 17, card.Height - 2);
            };
            return card;
        }

        private Button CrearBoton(string texto, Color fondo, Color textoColor, int ancho, int alto)
        {
            Button boton = new Button
            {
                Text = texto,
                Size = new Size(ancho, alto),
                BackColor = fondo,
                ForeColor = textoColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            boton.FlatAppearance.BorderSize = 0;
            boton.FlatAppearance.MouseOverBackColor = fondo == accentGreen
                ? Color.FromArgb(15, 105, 76)
                : Color.FromArgb(226, 232, 239);
            AplicarBordesRedondeados(boton, 10);
            return boton;
        }

        private void AjustarTamanos()
        {
            if (flowContainer == null || pnlHeader == null || rowKPIs == null ||
                pnlToolbar == null || pnlTablaCard == null)
                return;

            int ancho = Math.Max(980,
                flowContainer.ClientSize.Width
                - flowContainer.Padding.Horizontal
                - SystemInformation.VerticalScrollBarWidth - 4);

            flowContainer.SuspendLayout();
            pnlHeader.Width = ancho;
            pnlToolbar.Width = ancho;
            pnlTablaCard.Width = ancho;
            rowKPIs.Width = ancho;

            int separacion = 12;
            int anchoTarjeta = Math.Max(215, (ancho - separacion * 3) / 4);
            for (int i = 0; i < rowKPIs.Controls.Count; i++)
            {
                rowKPIs.Controls[i].Width = anchoTarjeta;
                rowKPIs.Controls[i].Margin = new Padding(0, 0, i == 3 ? 0 : separacion, 0);
            }

            int usedHeight =
                flowContainer.Padding.Vertical
                + pnlHeader.Height + pnlHeader.Margin.Bottom
                + rowKPIs.Height + rowKPIs.Margin.Bottom
                + pnlToolbar.Height + pnlToolbar.Margin.Bottom;
            pnlTablaCard.Height = Math.Max(390, flowContainer.ClientSize.Height - usedHeight - 8);
            flowContainer.ResumeLayout(true);
        }

        private void RefrescarDatos(bool mostrarMensajeError)
        {
            try
            {
                using (MySqlConnection conexion = new Conexion().ObtenerConexion())
                {
                    if (conexion.State != ConnectionState.Open)
                        conexion.Open();

                    datosMovimientos = LeerMovimientos(conexion);
                    dgvMovimientos.DataSource = datosMovimientos.DefaultView;
                    AjustarColumnasVisuales();
                    ActualizarIndicadores(conexion);
                }

                lblEstadoConexion.Text = "●  Base de datos conectada";
                lblEstadoConexion.ForeColor = accentGreen;
                AplicarFiltros();
            }
            catch (Exception ex)
            {
                lblEstadoConexion.Text = "●  Sin conexión a la base de datos";
                lblEstadoConexion.ForeColor = Color.FromArgb(190, 58, 66);
                datosMovimientos = new DataTable();
                dgvMovimientos.DataSource = datosMovimientos;
                foreach (Label valor in valoresKpi.Values)
                    valor.Text = "—";

                if (mostrarMensajeError)
                {
                    MessageBox.Show(
                        "No se pudieron cargar los movimientos de Artemusa TV.\n\n" + ex.Message,
                        "Error de conexión",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private DataTable LeerMovimientos(MySqlConnection conexion)
        {
            string consulta = @"
SELECT
    m.id_movimiento,
    CONCAT('MOV-', LPAD(m.id_movimiento, 6, '0')) AS codigo_movimiento,
    CASE m.tipo_movimiento
        WHEN 'INGRESO' THEN 'Ingreso'
        WHEN 'SALIDA' THEN 'Salida'
        WHEN 'TRASLADO' THEN 'Traslado'
        WHEN 'DEVOLUCION' THEN 'Devolución'
        WHEN 'MANTENIMIENTO' THEN 'Mantenimiento'
        ELSE m.tipo_movimiento
    END AS tipo,
    COALESCE(e.codigo, '') AS codigo_equipo,
    COALESCE(e.nombre, 'Equipo audiovisual') AS equipo,
    COALESCE(e.categoria, '') AS categoria,
    COALESCE(m.cantidad, 1) AS cantidad,
    COALESCE(
        NULLIF(TRIM(CONCAT_WS(' ', NULLIF(r.nombre, ''), NULLIF(r.apellido, ''))), ''),
        NULLIF(TRIM(CONCAT_WS(' ', NULLIF(t.nombre, ''), NULLIF(t.apellido, ''))), ''),
        CONCAT('Responsable #', COALESCE(m.id_responsable, t.id_trabajador, m.id_usuario, m.id_trabajador))
    ) AS responsable,
    COALESCE(m.ubicacion_origen, '') AS origen,
    COALESCE(m.ubicacion_destino, '') AS destino,
    COALESCE(m.motivo, '') AS motivo,
    COALESCE(m.observacion, '') AS observacion,
    CONCAT(
        DATE_FORMAT(m.fecha_movimiento, '%d/%m/%Y'),
        ' ',
        TIME_FORMAT(m.hora_movimiento, '%H:%i')
    ) AS fecha_hora
FROM movimiento m
LEFT JOIN equipos e ON e.id = m.id_equipo
LEFT JOIN responsable r ON r.id_responsable = m.id_responsable
LEFT JOIN trabajadores t
    ON t.id_trabajador = COALESCE(m.id_usuario, m.id_trabajador)
ORDER BY m.fecha_movimiento DESC, m.hora_movimiento DESC, m.id_movimiento DESC;";

            DataTable tabla = new DataTable();
            using (MySqlDataAdapter adaptador = new MySqlDataAdapter(consulta, conexion))
                adaptador.Fill(tabla);
            return tabla;
        }

        private void ActualizarIndicadores(MySqlConnection conexion)
        {
            const string consulta = @"
SELECT
    COALESCE(SUM(CASE WHEN tipo_movimiento = 'INGRESO'
        AND fecha_movimiento = CURDATE() THEN cantidad ELSE 0 END), 0) AS ingresos_hoy,
    COALESCE(SUM(CASE WHEN tipo_movimiento = 'SALIDA'
        AND fecha_movimiento = CURDATE() THEN cantidad ELSE 0 END), 0) AS salidas_hoy,
    COALESCE(SUM(CASE WHEN tipo_movimiento = 'DEVOLUCION'
        AND fecha_movimiento = CURDATE() THEN cantidad ELSE 0 END), 0) AS devoluciones_hoy,
    COALESCE(SUM(CASE WHEN YEAR(fecha_movimiento) = YEAR(CURDATE())
        AND MONTH(fecha_movimiento) = MONTH(CURDATE()) THEN 1 ELSE 0 END), 0) AS movimientos_mes
FROM movimiento;";

            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            using (MySqlDataReader lector = comando.ExecuteReader())
            {
                if (!lector.Read()) return;
                valoresKpi["ingresos"].Text = Convert.ToInt32(lector["ingresos_hoy"]).ToString("N0");
                valoresKpi["salidas"].Text = Convert.ToInt32(lector["salidas_hoy"]).ToString("N0");
                valoresKpi["devoluciones"].Text = Convert.ToInt32(lector["devoluciones_hoy"]).ToString("N0");
                valoresKpi["mes"].Text = Convert.ToInt32(lector["movimientos_mes"]).ToString("N0");
            }
        }

        private void AplicarFiltros()
        {
            if (datosMovimientos == null || datosMovimientos.DefaultView == null)
                return;

            List<string> condiciones = new List<string>();
            if (cmbTipo != null && cmbTipo.SelectedIndex > 0)
            {
                string tipo = EscaparFiltro(Convert.ToString(cmbTipo.SelectedItem) ?? "");
                condiciones.Add("tipo = '" + tipo + "'");
            }

            string busqueda = txtBuscar == null ? "" : txtBuscar.Text.Trim();
            if (!string.IsNullOrWhiteSpace(busqueda) &&
                !busqueda.StartsWith("Buscar por", StringComparison.OrdinalIgnoreCase))
            {
                string valor = EscaparFiltro(busqueda);
                string[] columnas =
                {
                    "codigo_movimiento", "codigo_equipo", "equipo", "categoria",
                    "responsable", "origen", "destino", "motivo", "observacion"
                };
                List<string> busquedas = new List<string>();
                foreach (string columna in columnas)
                {
                    if (datosMovimientos.Columns.Contains(columna))
                        busquedas.Add(columna + " LIKE '%" + valor + "%'");
                }
                if (busquedas.Count > 0)
                    condiciones.Add("(" + string.Join(" OR ", busquedas) + ")");
            }

            datosMovimientos.DefaultView.RowFilter = string.Join(" AND ", condiciones);
        }

        private string EscaparFiltro(string valor)
        {
            return valor
                .Replace("'", "''")
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("*", "[*]");
        }

        private void AbrirDialogoMovimiento(string? tipoInicial)
        {
            try
            {
                List<OpcionEquipo> equipos;
                List<OpcionTrabajador> trabajadores;

                using (MySqlConnection conexion = new Conexion().ObtenerConexion())
                {
                    if (conexion.State != ConnectionState.Open)
                        conexion.Open();
                    equipos = LeerEquipos(conexion);
                    trabajadores = LeerTrabajadores(conexion);
                }

                if (equipos.Count == 0)
                {
                    MessageBox.Show(
                        "Primero registra al menos un equipo audiovisual en la tabla equipos.",
                        "No hay equipos disponibles",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }
                if (trabajadores.Count == 0)
                {
                    MessageBox.Show(
                        "Primero registra un responsable activo en el panel Trabajadores.",
                        "No hay responsables disponibles",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                using (Form formulario = CrearFormularioMovimiento(equipos, trabajadores, tipoInicial))
                {
                    if (formulario.ShowDialog(FindForm()) == DialogResult.OK)
                        RefrescarDatos(false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudieron cargar los equipos o trabajadores.\n\n" + ex.Message,
                    "Error al abrir el registro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private List<OpcionEquipo> LeerEquipos(MySqlConnection conexion)
        {
            const string consulta = @"
SELECT id, codigo, nombre, categoria, ubicacion, stock
FROM equipos
ORDER BY nombre;";
            List<OpcionEquipo> resultado = new List<OpcionEquipo>();
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            using (MySqlDataReader lector = comando.ExecuteReader())
            {
                while (lector.Read())
                {
                    resultado.Add(new OpcionEquipo
                    {
                        Id = Convert.ToInt32(lector["id"]),
                        Codigo = Convert.ToString(lector["codigo"]) ?? "",
                        Nombre = Convert.ToString(lector["nombre"]) ?? "Equipo audiovisual",
                        Categoria = Convert.ToString(lector["categoria"]) ?? "",
                        Ubicacion = Convert.ToString(lector["ubicacion"]) ?? "",
                        Stock = Convert.ToInt32(lector["stock"])
                    });
                }
            }
            return resultado;
        }

        private List<OpcionTrabajador> LeerTrabajadores(MySqlConnection conexion)
        {
            const string consulta = @"
SELECT r.id_responsable,
       TRIM(CONCAT_WS(' ', NULLIF(r.nombre, ''), NULLIF(r.apellido, ''))) AS nombre_responsable
FROM responsable r
WHERE r.estado = 'Activo'
ORDER BY r.nombre, r.apellido;";
            List<OpcionTrabajador> resultado = new List<OpcionTrabajador>();
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            using (MySqlDataReader lector = comando.ExecuteReader())
            {
                while (lector.Read())
                {
                    resultado.Add(new OpcionTrabajador
                    {
                        Id = Convert.ToInt32(lector["id_responsable"]),
                        Nombre = Convert.ToString(lector["nombre_responsable"]) ?? ""
                    });
                }
            }
            return resultado;
        }

        private Form CrearFormularioMovimiento(
            List<OpcionEquipo> equipos,
            List<OpcionTrabajador> trabajadores,
            string? tipoInicial)
        {
            Form formulario = new Form
            {
                Text = "Registrar movimiento · Artemusa TV",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(700, 755),
                BackColor = bgCanvas,
                Font = new Font("Segoe UI", 9f)
            };

            Label lblMarca = new Label
            {
                Text = "ARTEMUSA TV / MOVIMIENTOS DE EQUIPOS",
                AutoSize = true,
                ForeColor = accentGreen,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Location = new Point(30, 20)
            };
            formulario.Controls.Add(lblMarca);
            formulario.Controls.Add(new Label
            {
                Text = "Registrar movimiento",
                AutoSize = true,
                ForeColor = textMain,
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                Location = new Point(28, 42)
            });
            formulario.Controls.Add(new Label
            {
                Text = "Selecciona el equipo, el responsable y registra la ubicación.",
                AutoSize = true,
                ForeColor = textMuted,
                Font = new Font("Segoe UI", 8.7f),
                Location = new Point(31, 79)
            });

            ComboBox cmbMovimiento = CrearComboFormulario(
                formulario, "Tipo de movimiento", 31, 117, 638);
            foreach (OpcionTipo tipo in tiposMovimiento)
                cmbMovimiento.Items.Add(tipo);
            cmbMovimiento.SelectedIndex = 0;
            if (!string.IsNullOrWhiteSpace(tipoInicial))
            {
                for (int i = 0; i < cmbMovimiento.Items.Count; i++)
                {
                    OpcionTipo? item = cmbMovimiento.Items[i] as OpcionTipo;
                    if (item != null && item.Codigo == tipoInicial)
                    {
                        cmbMovimiento.SelectedIndex = i;
                        break;
                    }
                }
            }

            ComboBox cmbEquipo = CrearComboFormulario(
                formulario, "Equipo audiovisual", 31, 197, 638);
            foreach (OpcionEquipo equipo in equipos)
                cmbEquipo.Items.Add(equipo);
            cmbEquipo.SelectedIndex = 0;

            ComboBox cmbTrabajador = CrearComboFormulario(
                formulario, "Responsable", 31, 277, 638);
            foreach (OpcionTrabajador trabajador in trabajadores)
                cmbTrabajador.Items.Add(trabajador);
            cmbTrabajador.SelectedIndex = 0;

            TextBox txtOrigen = CrearTextoFormulario(
                formulario, "Ubicación de origen", 31, 357, 306, 38, false);
            TextBox txtDestino = CrearTextoFormulario(
                formulario, "Ubicación de destino", 363, 357, 306, 38, false);
            txtOrigen.MaxLength = 120;
            txtDestino.MaxLength = 120;

            formulario.Controls.Add(new Label
            {
                Text = "Cantidad",
                AutoSize = true,
                ForeColor = textMain,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Location = new Point(31, 437)
            });
            NumericUpDown numCantidad = new NumericUpDown
            {
                Location = new Point(31, 460),
                Size = new Size(150, 38),
                Minimum = 1,
                Maximum = 100000,
                Value = 1,
                ThousandsSeparator = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                ForeColor = textMain,
                Font = new Font("Segoe UI", 9f)
            };
            formulario.Controls.Add(numCantidad);

            TextBox txtMotivo = CrearTextoFormulario(
                formulario, "Motivo", 205, 437, 464, 38, false);
            txtMotivo.MaxLength = 255;

            TextBox txtObservacion = CrearTextoFormulario(
                formulario, "Observación (opcional)", 31, 517, 638, 78, true);
            txtObservacion.MaxLength = 4000;

            Action actualizarOrigen = () =>
            {
                OpcionEquipo? equipo = cmbEquipo.SelectedItem as OpcionEquipo;
                if (equipo != null)
                    txtOrigen.Text = equipo.Ubicacion;
            };
            cmbEquipo.SelectedIndexChanged += (s, e) => actualizarOrigen();
            actualizarOrigen();

            Label lblNota = new Label
            {
                Text = "Cada ficha tiene una ubicación; un traslado mueve todo su stock.",
                AutoSize = true,
                ForeColor = textMuted,
                Font = new Font("Segoe UI", 8f),
                Location = new Point(32, 632)
            };
            formulario.Controls.Add(lblNota);

            Button btnCancelar = CrearBoton(
                "Cancelar", Color.FromArgb(231, 236, 242), textMain, 115, 42);
            btnCancelar.Location = new Point(345, 690);
            btnCancelar.DialogResult = DialogResult.Cancel;
            formulario.Controls.Add(btnCancelar);

            Button btnGuardar = CrearBoton(
                "Guardar movimiento", accentGreen, Color.White, 205, 42);
            btnGuardar.Location = new Point(
                formulario.ClientSize.Width - btnGuardar.Width - 30, 690);
            btnGuardar.DialogResult = DialogResult.None;
            formulario.Controls.Add(btnGuardar);

            formulario.CancelButton = btnCancelar;
            formulario.AcceptButton = btnGuardar;

            btnGuardar.Click += (s, e) =>
            {
                OpcionTipo? tipo = cmbMovimiento.SelectedItem as OpcionTipo;
                OpcionEquipo? equipo = cmbEquipo.SelectedItem as OpcionEquipo;
                OpcionTrabajador? trabajador = cmbTrabajador.SelectedItem as OpcionTrabajador;
                int cantidad = Decimal.ToInt32(numCantidad.Value);
                string motivo = txtMotivo.Text.Trim();
                string origen = txtOrigen.Text.Trim();
                string destino = txtDestino.Text.Trim();

                if (tipo == null || equipo == null || trabajador == null)
                {
                    MessageBox.Show(formulario,
                        "Selecciona el tipo de movimiento, el equipo y el responsable.",
                        "Datos incompletos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (motivo.Length == 0)
                {
                    MessageBox.Show(formulario,
                        "Escribe el motivo del movimiento.",
                        "Falta el motivo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtMotivo.Focus();
                    return;
                }
                if (tipo.Codigo == "TRASLADO" && origen.Length == 0)
                {
                    MessageBox.Show(formulario,
                        "Para un traslado, indica la ubicación de origen.",
                        "Falta el origen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtOrigen.Focus();
                    return;
                }
                if (tipo.Codigo != "MANTENIMIENTO" && destino.Length == 0)
                {
                    MessageBox.Show(formulario,
                        "Indica la ubicación de destino del equipo.",
                        "Falta el destino", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtDestino.Focus();
                    return;
                }

                try
                {
                    GuardarMovimiento(
                        tipo.Codigo,
                        equipo.Id,
                        trabajador.Id,
                        cantidad,
                        motivo,
                        txtObservacion.Text.Trim(),
                        origen,
                        destino);

                    MessageBox.Show(formulario,
                        "El movimiento se guardó correctamente.",
                        "Artemusa TV", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    formulario.DialogResult = DialogResult.OK;
                    formulario.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(formulario,
                        "No se pudo guardar el movimiento.\n\n" + ex.Message,
                        "Error al guardar", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            return formulario;
        }

        private ComboBox CrearComboFormulario(Form formulario, string titulo, int x, int y, int ancho)
        {
            formulario.Controls.Add(new Label
            {
                Text = titulo,
                AutoSize = true,
                ForeColor = textMain,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Location = new Point(x, y)
            });
            ComboBox combo = new ComboBox
            {
                Location = new Point(x, y + 23),
                Size = new Size(ancho, 38),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = textMain,
                Font = new Font("Segoe UI", 9f)
            };
            formulario.Controls.Add(combo);
            return combo;
        }

        private TextBox CrearTextoFormulario(
            Form formulario, string titulo, int x, int y, int ancho, int alto, bool multilinea)
        {
            formulario.Controls.Add(new Label
            {
                Text = titulo,
                AutoSize = true,
                ForeColor = textMain,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Location = new Point(x, y)
            });
            TextBox caja = new TextBox
            {
                Location = new Point(x, y + 23),
                Size = new Size(ancho, alto),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                ForeColor = textMain,
                Font = new Font("Segoe UI", 9f),
                Multiline = multilinea,
                ScrollBars = multilinea ? ScrollBars.Vertical : ScrollBars.None
            };
            formulario.Controls.Add(caja);
            return caja;
        }

        private void GuardarMovimiento(
            string tipo,
            int idEquipo,
            int idResponsable,
            int cantidad,
            string motivo,
            string observacion,
            string origen,
            string destino)
        {
            if (cantidad < 1)
                throw new InvalidOperationException("La cantidad debe ser mayor que cero.");

            using (MySqlConnection conexion = new Conexion().ObtenerConexion())
            {
                if (conexion.State != ConnectionState.Open)
                    conexion.Open();

                using (MySqlTransaction transaccion = conexion.BeginTransaction())
                {
                    try
                    {
                        using (MySqlCommand validarResponsable = new MySqlCommand(
                            "SELECT COUNT(*) FROM responsable WHERE id_responsable=@id AND estado='Activo'",
                            conexion, transaccion))
                        {
                            validarResponsable.Parameters.Add("@id", MySqlDbType.Int32).Value = idResponsable;
                            if (Convert.ToInt32(validarResponsable.ExecuteScalar()) != 1)
                                throw new InvalidOperationException(
                                    "El responsable seleccionado ya no está activo. Cierra y vuelve a abrir el formulario.");
                        }

                        int stockActual;
                        string ubicacionActual;
                        const string bloquearEquipo = @"
SELECT stock, ubicacion
FROM equipo
WHERE id_equipo = @id_equipo
FOR UPDATE;";

                        using (MySqlCommand comando = new MySqlCommand(bloquearEquipo, conexion, transaccion))
                        {
                            comando.Parameters.Add("@id_equipo", MySqlDbType.Int32).Value = idEquipo;
                            using (MySqlDataReader lector = comando.ExecuteReader())
                            {
                                if (!lector.Read())
                                    throw new InvalidOperationException("El equipo seleccionado ya no existe.");

                                stockActual = Convert.ToInt32(lector["stock"]);
                                ubicacionActual = Convert.ToString(lector["ubicacion"]) ?? "";
                            }
                        }

                        string origenRegistro = string.IsNullOrWhiteSpace(origen)
                            ? ubicacionActual
                            : origen.Trim();
                        string destinoRegistro = destino.Trim();
                        if ((tipo == "INGRESO" || tipo == "DEVOLUCION" || tipo == "TRASLADO")
                            && string.IsNullOrWhiteSpace(destinoRegistro))
                            throw new InvalidOperationException("Indica la ubicación de destino.");
                        if ((tipo == "INGRESO" || tipo == "DEVOLUCION")
                            && stockActual > 0
                            && !string.Equals(
                                ubicacionActual.Trim(), destinoRegistro,
                                StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException(
                                "La ficha de este equipo guarda una sola ubicación. "
                                + "Mientras conserve stock, registra el ingreso o devolución en "
                                + "'" + ubicacionActual + "'. Para cambiarlo, registra un traslado de todo el stock.");
                        if (tipo == "TRASLADO"
                            && string.Equals(
                                ubicacionActual.Trim(), destinoRegistro,
                                StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException(
                                "La ubicación de destino debe ser distinta de la ubicación actual.");

                        int stockNuevo = stockActual;
                        bool debeActualizarUbicacion = false;

                        switch (tipo)
                        {
                            case "INGRESO":
                            case "DEVOLUCION":
                                if (stockActual > int.MaxValue - cantidad)
                                    throw new InvalidOperationException("La cantidad supera el límite de stock permitido.");
                                stockNuevo = stockActual + cantidad;
                                debeActualizarUbicacion = true;
                                break;

                            case "SALIDA":
                                if (cantidad > stockActual)
                                    throw new InvalidOperationException(
                                        "No hay stock suficiente. Disponible: " + stockActual + ".");
                                stockNuevo = stockActual - cantidad;
                                break;

                            case "TRASLADO":
                                if (stockActual < 1)
                                    throw new InvalidOperationException("No hay stock para trasladar.");
                                if (cantidad != stockActual)
                                    throw new InvalidOperationException(
                                        "Este inventario guarda una sola ubicación por equipo. "
                                        + "Para un traslado, la cantidad debe ser todo el stock actual ("
                                        + stockActual + ").");
                                debeActualizarUbicacion = true;
                                break;

                            case "MANTENIMIENTO":
                                if (cantidad > stockActual)
                                    throw new InvalidOperationException(
                                        "La cantidad en mantenimiento no puede superar el stock actual ("
                                        + stockActual + ").");
                                break;

                            default:
                                throw new InvalidOperationException("El tipo de movimiento no es válido.");
                        }

                        int? idUbicacionOrigen = string.IsNullOrWhiteSpace(origenRegistro)
                            ? (int?)null
                            : ObtenerOCrearUbicacion(conexion, transaccion, origenRegistro);
                        int? idUbicacionDestino = string.IsNullOrWhiteSpace(destinoRegistro)
                            ? (int?)null
                            : ObtenerOCrearUbicacion(conexion, transaccion, destinoRegistro);

                        const string actualizarEquipo = @"
UPDATE equipo
SET stock = @stock,
    estado_registro = CASE
        WHEN @stock = 0 THEN 'Agotado'
        WHEN estado_registro = 'Agotado' AND @stock > 0 THEN 'Disponible'
        ELSE estado_registro
    END
WHERE id_equipo = @id_equipo;";

                        using (MySqlCommand comando = new MySqlCommand(actualizarEquipo, conexion, transaccion))
                        {
                            comando.Parameters.Add("@stock", MySqlDbType.Int32).Value = stockNuevo;
                            comando.Parameters.Add("@id_equipo", MySqlDbType.Int32).Value = idEquipo;
                            comando.ExecuteNonQuery();
                        }

                        if (debeActualizarUbicacion)
                        {
                            if (!idUbicacionDestino.HasValue)
                                throw new InvalidOperationException("No se pudo resolver la ubicación de destino.");

                            const string sqlActualizarUbicacion = @"
UPDATE equipo
SET id_ubicacion = @id_ubicacion,
    ubicacion = @ubicacion
WHERE id_equipo = @id_equipo;";
                            using (MySqlCommand comando = new MySqlCommand(
                                sqlActualizarUbicacion, conexion, transaccion))
                            {
                                comando.Parameters.Add("@id_ubicacion", MySqlDbType.Int32).Value = idUbicacionDestino.Value;
                                comando.Parameters.Add("@ubicacion", MySqlDbType.VarChar).Value = destinoRegistro;
                                comando.Parameters.Add("@id_equipo", MySqlDbType.Int32).Value = idEquipo;
                                comando.ExecuteNonQuery();
                            }
                        }

                        const string insertarMovimiento = @"
INSERT INTO movimiento
    (tipo_movimiento, cantidad, fecha_movimiento, hora_movimiento, motivo, observacion,
     id_equipo, id_responsable, id_usuario, id_trabajador,
     id_ubicacion_origen, id_ubicacion_destino,
     ubicacion_origen, ubicacion_destino)
VALUES
    (@tipo, @cantidad, CURDATE(), CURTIME(), @motivo, @observacion,
     @id_equipo, @id_responsable, NULL, NULL, @id_ubicacion_origen, @id_ubicacion_destino,
     @origen, @destino);";

                        using (MySqlCommand comando = new MySqlCommand(insertarMovimiento, conexion, transaccion))
                        {
                            comando.Parameters.Add("@tipo", MySqlDbType.VarChar).Value = tipo;
                            comando.Parameters.Add("@cantidad", MySqlDbType.Int32).Value = cantidad;
                            comando.Parameters.Add("@motivo", MySqlDbType.VarChar).Value = motivo;
                            comando.Parameters.Add("@observacion", MySqlDbType.Text).Value =
                                string.IsNullOrWhiteSpace(observacion) ? (object)DBNull.Value : observacion;
                            comando.Parameters.Add("@id_equipo", MySqlDbType.Int32).Value = idEquipo;
                            comando.Parameters.Add("@id_responsable", MySqlDbType.Int32).Value = idResponsable;
                            comando.Parameters.Add("@id_ubicacion_origen", MySqlDbType.Int32).Value =
                                idUbicacionOrigen.HasValue ? (object)idUbicacionOrigen.Value : DBNull.Value;
                            comando.Parameters.Add("@id_ubicacion_destino", MySqlDbType.Int32).Value =
                                idUbicacionDestino.HasValue ? (object)idUbicacionDestino.Value : DBNull.Value;
                            comando.Parameters.Add("@origen", MySqlDbType.VarChar).Value =
                                string.IsNullOrWhiteSpace(origenRegistro) ? (object)DBNull.Value : origenRegistro;
                            comando.Parameters.Add("@destino", MySqlDbType.VarChar).Value =
                                string.IsNullOrWhiteSpace(destinoRegistro) ? (object)DBNull.Value : destinoRegistro;
                            comando.ExecuteNonQuery();
                        }

                        transaccion.Commit();
                    }
                    catch
                    {
                        try { transaccion.Rollback(); } catch { }
                        throw;
                    }
                }
            }
        }

        private int ObtenerOCrearUbicacion(
            MySqlConnection conexion,
            MySqlTransaction transaccion,
            string nombre)
        {
            string nombreNormalizado = nombre.Trim();
            if (nombreNormalizado.Length == 0)
                throw new InvalidOperationException("El nombre de la ubicación está vacío.");
            if (nombreNormalizado.Length > 120)
                nombreNormalizado = nombreNormalizado.Substring(0, 120);

            const string insertar = @"
INSERT INTO ubicacion (nombre_ubicacion, estado)
VALUES (@nombre, 'Activo')
ON DUPLICATE KEY UPDATE nombre_ubicacion = VALUES(nombre_ubicacion);";
            using (MySqlCommand comando = new MySqlCommand(insertar, conexion, transaccion))
            {
                comando.Parameters.Add("@nombre", MySqlDbType.VarChar).Value = nombreNormalizado;
                comando.ExecuteNonQuery();
            }

            const string consultarId = @"
SELECT id_ubicacion
FROM ubicacion
WHERE nombre_ubicacion = @nombre
LIMIT 1;";
            using (MySqlCommand comando = new MySqlCommand(consultarId, conexion, transaccion))
            {
                comando.Parameters.Add("@nombre", MySqlDbType.VarChar).Value = nombreNormalizado;
                object? resultado = comando.ExecuteScalar();
                if (resultado == null || resultado == DBNull.Value)
                    throw new InvalidOperationException("No se pudo registrar la ubicación.");
                return Convert.ToInt32(resultado);
            }
        }

        private void AjustarColumnasVisuales()
        {
            if (dgvMovimientos.Columns.Count == 0) return;

            ConfigurarColumna("id_movimiento", "", false, 0, 0);
            ConfigurarColumna("codigo_movimiento", "Código", true, 98, 75);
            ConfigurarColumna("tipo", "Tipo", true, 118, 88);
            ConfigurarColumna("codigo_equipo", "Código de equipo", true, 112, 85);
            ConfigurarColumna("equipo", "Equipo audiovisual", true, 180, 155);
            ConfigurarColumna("categoria", "Categoría", true, 115, 90);
            ConfigurarColumna("cantidad", "Cantidad", true, 82, 58);
            ConfigurarColumna("responsable", "Responsable", true, 145, 120);
            ConfigurarColumna("origen", "Ubicación de origen", true, 125, 100);
            ConfigurarColumna("destino", "Ubicación de destino", true, 125, 100);
            ConfigurarColumna("motivo", "Motivo", true, 145, 115);
            ConfigurarColumna("observacion", "Observación", true, 145, 110);
            ConfigurarColumna("fecha_hora", "Fecha y hora", true, 130, 105);

            if (dgvMovimientos.Columns.Contains("codigo_movimiento"))
            {
                dgvMovimientos.Columns["codigo_movimiento"].DefaultCellStyle.ForeColor = textMuted;
                dgvMovimientos.Columns["codigo_movimiento"].DefaultCellStyle.Font =
                    new Font("Consolas", 8.5f);
            }
            if (dgvMovimientos.Columns.Contains("tipo"))
                dgvMovimientos.Columns["tipo"].DefaultCellStyle.Alignment =
                    DataGridViewContentAlignment.MiddleCenter;
            if (dgvMovimientos.Columns.Contains("cantidad"))
                dgvMovimientos.Columns["cantidad"].DefaultCellStyle.Alignment =
                    DataGridViewContentAlignment.MiddleCenter;
        }

        private void ConfigurarColumna(
            string nombre, string titulo, bool visible, int anchoMinimo, float peso)
        {
            if (!dgvMovimientos.Columns.Contains(nombre)) return;
            DataGridViewColumn columna = dgvMovimientos.Columns[nombre];
            columna.HeaderText = titulo;
            columna.Visible = visible;
            if (!visible) return;
            columna.MinimumWidth = anchoMinimo;
            columna.FillWeight = peso;
            columna.SortMode = DataGridViewColumnSortMode.Automatic;
        }

        private void PintarTipoMovimiento(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 ||
                dgvMovimientos.Columns[e.ColumnIndex].Name != "tipo")
                return;

            string texto = Convert.ToString(e.FormattedValue) ?? "";
            Color frente;
            Color fondo;
            if (texto == "Ingreso")
            {
                frente = accentGreen;
                fondo = Color.FromArgb(226, 244, 234);
            }
            else if (texto == "Salida")
            {
                frente = Color.FromArgb(183, 65, 64);
                fondo = Color.FromArgb(254, 234, 236);
            }
            else if (texto == "Devolución")
            {
                frente = Color.FromArgb(28, 107, 185);
                fondo = Color.FromArgb(228, 241, 253);
            }
            else if (texto == "Traslado")
            {
                frente = Color.FromArgb(119, 75, 190);
                fondo = Color.FromArgb(240, 233, 251);
            }
            else if (texto == "Mantenimiento")
            {
                frente = Color.FromArgb(171, 112, 27);
                fondo = Color.FromArgb(252, 243, 222);
            }
            else
            {
                return;
            }

            e.Paint(e.CellBounds,
                DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);
            int ancho = Math.Max(74, Math.Min(132, e.CellBounds.Width - 16));
            Rectangle badge = new Rectangle(
                e.CellBounds.Left + (e.CellBounds.Width - ancho) / 2,
                e.CellBounds.Top + (e.CellBounds.Height - 27) / 2,
                ancho, 27);

            using (GraphicsPath path = CrearRutaRedondeada(badge, 10))
            using (SolidBrush brush = new SolidBrush(fondo))
            using (Font font = new Font("Segoe UI", 8f, FontStyle.Bold))
            {
                SmoothingMode anterior = e.Graphics.SmoothingMode;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(brush, path);
                e.Graphics.SmoothingMode = anterior;
                TextRenderer.DrawText(
                    e.Graphics, texto, font, badge, frente,
                    TextFormatFlags.HorizontalCenter
                    | TextFormatFlags.VerticalCenter
                    | TextFormatFlags.NoPrefix
                    | TextFormatFlags.EndEllipsis);
            }
            e.Handled = true;
        }

        private void MostrarDetalleMovimiento(int fila)
        {
            if (fila < 0 || fila >= dgvMovimientos.Rows.Count) return;
            DataGridViewRow registro = dgvMovimientos.Rows[fila];

            string Valor(string columna)
            {
                return dgvMovimientos.Columns.Contains(columna)
                    ? Convert.ToString(registro.Cells[columna].Value) ?? ""
                    : "";
            }

            string detalle =
                "Código: " + Valor("codigo_movimiento") + Environment.NewLine
                + "Tipo: " + Valor("tipo") + Environment.NewLine
                + "Equipo: " + Valor("codigo_equipo") + " · " + Valor("equipo") + Environment.NewLine
                + "Cantidad: " + Valor("cantidad") + Environment.NewLine
                + "Categoría: " + Valor("categoria") + Environment.NewLine
                + "Responsable: " + Valor("responsable") + Environment.NewLine
                + "Origen: " + Valor("origen") + Environment.NewLine
                + "Destino: " + Valor("destino") + Environment.NewLine
                + "Fecha y hora: " + Valor("fecha_hora") + Environment.NewLine
                + "Motivo: " + Valor("motivo") + Environment.NewLine
                + "Observación: " + Valor("observacion");

            MessageBox.Show(
                FindForm(), detalle, "Detalle del movimiento",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AplicarBordesRedondeados(Control control, int diametro)
        {
            Action recalcular = () =>
            {
                if (control.Width <= 1 || control.Height <= 1) return;
                int d = Math.Max(2, Math.Min(diametro, Math.Min(control.Width, control.Height)));
                using (GraphicsPath ruta = new GraphicsPath())
                {
                    ruta.AddArc(0, 0, d, d, 180, 90);
                    ruta.AddArc(control.Width - d, 0, d, d, 270, 90);
                    ruta.AddArc(control.Width - d, control.Height - d, d, d, 0, 90);
                    ruta.AddArc(0, control.Height - d, d, d, 90, 90);
                    ruta.CloseFigure();
                    Region? regionAnterior = control.Region;
                    control.Region = new Region(ruta);
                    regionAnterior?.Dispose();
                }
            };
            control.Resize += (s, e) => recalcular();
            recalcular();
        }

        private GraphicsPath CrearRutaRedondeada(Rectangle rectangulo, int diametro)
        {
            int d = Math.Max(2, Math.Min(diametro, Math.Min(rectangulo.Width, rectangulo.Height)));
            GraphicsPath ruta = new GraphicsPath();
            ruta.AddArc(rectangulo.Left, rectangulo.Top, d, d, 180, 90);
            ruta.AddArc(rectangulo.Right - d, rectangulo.Top, d, d, 270, 90);
            ruta.AddArc(rectangulo.Right - d, rectangulo.Bottom - d, d, d, 0, 90);
            ruta.AddArc(rectangulo.Left, rectangulo.Bottom - d, d, d, 90, 90);
            ruta.CloseFigure();
            return ruta;
        }

        private sealed class OpcionTipo
        {
            public string Codigo { get; }
            public string Texto { get; }

            public OpcionTipo(string codigo, string texto)
            {
                Codigo = codigo;
                Texto = texto;
            }

            public override string ToString() => Texto;
        }

        private sealed class OpcionEquipo
        {
            public int Id { get; set; }
            public string Codigo { get; set; } = "";
            public string Nombre { get; set; } = "";
            public string Categoria { get; set; } = "";
            public string Ubicacion { get; set; } = "";
            public int Stock { get; set; }

            public override string ToString()
            {
                string identificador = string.IsNullOrWhiteSpace(Codigo)
                    ? "ID " + Id
                    : Codigo;
                return identificador + "  ·  " + Nombre
                    + (string.IsNullOrWhiteSpace(Categoria) ? "" : "  (" + Categoria + ")")
                    + "  ·  Stock: " + Stock;
            }
        }

        private sealed class OpcionTrabajador
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = "";

            public override string ToString()
            {
                return string.IsNullOrWhiteSpace(Nombre)
                    ? "Trabajador #" + Id
                    : Nombre;
            }
        }
    }
}
