using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;

namespace SUSTENTACION.PanelTrabajador
{
    public class FormRetornoEquipo : Form
    {
        // ==========================================
        // PALETA DE COLORES
        // ==========================================
        private readonly Color fondo = Color.FromArgb(17, 19, 32);
        private readonly Color tarjeta = Color.FromArgb(26, 29, 46);
        private readonly Color campo = Color.FromArgb(37, 41, 62);
        private readonly Color morado = Color.FromArgb(100, 88, 245);
        private readonly Color blanco = Color.FromArgb(245, 246, 255);
        private readonly Color gris = Color.FromArgb(158, 164, 188);
        private readonly Color verde = Color.FromArgb(22, 177, 127);
        private readonly Color naranja = Color.FromArgb(255, 160, 60);
        private readonly Color rojo = Color.FromArgb(230, 80, 80);
        private readonly Color amarillo = Color.FromArgb(245, 200, 70);

        // ==========================================
        // DATOS
        // ==========================================
        private readonly int idTrabajador;
        private readonly string nombreTrabajador;
        private readonly DateTime fechaSeleccionada;

        private int idSalida = 0;
        private List<EquipoRetorno> equipos = new List<EquipoRetorno>();

        // ==========================================
        // COMPONENTES
        // ==========================================
        private Label lblSalidaInfo;
        private FlowLayoutPanel flpEquipos;
        private Label lblContador;
        private Label lblSinEquipos;
        private Label lblTituloEvento;
        private Panel panelRetorno;
        private Panel panelAgregar;
        private Button btnTabRetorno;
        private Button btnTabAgregar;
        private TextBox txtEscaner;
        private ListBox lstSugerenciasEquipo;
        private Panel panelSugerenciasEquipo;
        private Panel tarjetaBusquedaEquipo;
        private System.Windows.Forms.Timer temporizadorSugerencias;
        private bool seleccionandoSugerencia = false;

        private class SugerenciaEquipo
        {
            public string Codigo { get; set; } = "";
            public string Nombre { get; set; } = "";
            public int Stock { get; set; }
            public override string ToString() => $"{Codigo}  |  {Nombre}  ({Stock} disponibles)";
        }
        private Label lblEstadoEscaneo;
        private DataGridView dgvEquiposAgregar;
        private Label lblResumenAgregar;
        private Label lblTablaVacia;
        private Button btnGuardarAdicional;
        private int idEvento = 0;
        private readonly HashSet<int> equiposYaAsignados = new();
        private bool historialAsignacionesValidado = false;
        private DataGridView dgvYaAsignados;
        private Label lblAsignados;
        private TextBox txtBuscarRetorno;
        private ComboBox cboResponsableRetorno;
        private ComboBox cboResponsableAdicional;
        private readonly List<(string Tipo, int Id, string Nombre)> responsablesDisponibles = new();
        private readonly List<EquipoRetorno> equiposTodos = new();
        private bool filtrandoRetorno;
        private readonly Dictionary<int, EquipoAdicional> seleccionAdicional = new();

        private class EquipoAdicional
        {
            public int Id { get; set; }
            public string Codigo { get; set; } = "";
            public string Nombre { get; set; } = "";
            public int Disponible { get; set; }
            public int Cantidad { get; set; } = 1;
            public string ResponsableTipo { get; set; } = "";
            public int ResponsableId { get; set; }
            public string ResponsableNombre { get; set; } = "";
        }

        // ==========================================
        // MODELO
        // ==========================================
        private class EquipoRetorno
        {
            public int Id { get; set; }
            public string Codigo { get; set; }
            public string Nombre { get; set; }
            public int IdSalida { get; set; }
            public string Responsable { get; set; } = "Sin responsable registrado";
            public bool Incluir { get; set; } = false;   // ← Ahora FALSE por defecto
            public string Estado { get; set; } = "Buen estado";
            public string Observaciones { get; set; } = "";

            public CheckBox ChkIncluir { get; set; }
            public RadioButton RbBueno { get; set; }
            public RadioButton RbFalla { get; set; }
            public RadioButton RbFallando { get; set; }
            public TextBox TxtObservaciones { get; set; }
            public Panel Tarjeta { get; set; }
        }

        public FormRetornoEquipo(
            int idTrabajador,
            string nombreTrabajador,
            DateTime fecha)
        {
            this.idTrabajador = idTrabajador;
            this.nombreTrabajador = nombreTrabajador;
            this.fechaSeleccionada = fecha;

            CrearInterfaz();

            // Ícono de ARTEMUSA
            string rutaIcono = System.IO.Path.Combine(
                AppContext.BaseDirectory,
                "logo_artemusa.ico"
            );

            if (System.IO.File.Exists(rutaIcono))
            {
                this.Icon = new Icon(rutaIcono);
            }

            this.ShowIcon = true;
            this.ShowInTaskbar = true;

            // Asignar el ícono cuando la ventana ya esté creada
            this.Shown += (s, e) =>
            {
                string ruta = System.IO.Path.Combine(
                    AppContext.BaseDirectory,
                    "logo_artemusa.ico"
                );

                if (System.IO.File.Exists(ruta))
                {
                    this.Icon = new Icon(ruta);
                }
            };

            // Cargar los datos del evento al abrir el formulario
            this.Load += (s, e) => CargarDatos();

            // Liberar el temporizador al cerrar
            this.FormClosed += (s, e) =>
            {
                temporizadorSugerencias?.Dispose();
            };
        }

        // ==========================================
        // CREAR INTERFAZ
        // ==========================================
        private void CrearInterfaz()
        {
            Text = "ARTEMUSA | Gestión de equipos del evento";
            Size = new Size(1400, 900);
            MinimumSize = new Size(1000, 720);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    e.Handled = true;
                    VolverAlDashboard();
                }
            };
            BackColor = fondo;
            Font = new Font("Segoe UI", 9);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;

            // Cabecera común de las dos operaciones.
            var header = new Panel { Dock = DockStyle.Top, Height = 145, BackColor = tarjeta, Padding = new Padding(25, 12, 25, 0) };
            lblTituloEvento = new Label
            {
                Text = "Cargando evento...",
                Dock = DockStyle.Top,
                Height = 42,
                Font = new Font("Segoe UI", 17, FontStyle.Bold),
                ForeColor = blanco,
                AutoEllipsis = true
            };
            var subtitulo = new Label
            {
                Text = "Gestión de equipos | Selecciona la operación que deseas realizar",
                Dock = DockStyle.Top,
                Height = 28,
                ForeColor = gris
            };
            lblSalidaInfo = new Label
            {
                Text = "Cargando información...",
                Dock = DockStyle.Top,
                Height = 35,
                ForeColor = morado,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoEllipsis = true
            };
            header.Controls.Add(lblSalidaInfo);
            header.Controls.Add(subtitulo);
            header.Controls.Add(lblTituloEvento);
            Controls.Add(header);

            var tabs = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 65,
                Padding = new Padding(24, 12, 0, 8),
                BackColor = fondo,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            btnTabAgregar = CrearBoton("+  AGREGAR EQUIPOS", morado, 245);
            btnTabRetorno = CrearBoton("↩  DEVOLVER EQUIPOS", naranja, 245);
            btnTabAgregar.Click += (s, e) => CambiarPestana(false);
            btnTabRetorno.Click += (s, e) => CambiarPestana(true);
            tabs.Controls.Add(btnTabAgregar);
            tabs.Controls.Add(btnTabRetorno);
            Controls.Add(tabs);

            // El contenido de ambas pestañas comparte exactamente la misma ventana.
            panelRetorno = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 12, 24, 18), BackColor = fondo };
            panelAgregar = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 12, 24, 18), BackColor = fondo, Visible = false };
            Controls.Add(panelRetorno);
            Controls.Add(panelAgregar);
            panelAgregar.BringToFront();
            panelRetorno.BringToFront();
            tabs.BringToFront();
            header.BringToFront();

            // Barra de navegación, como en el formulario de salida.
            // Se construye después de las pestañas para mantener la jerarquía de Dock.
            var barraNavegacion = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 76,
                BackColor = Color.FromArgb(18, 29, 48),
                Padding = new Padding(24, 13, 24, 13)
            };
            var btnVolver = CrearBoton("←  VOLVER AL DASHBOARD", Color.FromArgb(55, 62, 91), 245);
            btnVolver.Height = 48;
            btnVolver.Dock = DockStyle.Left;
            btnVolver.Click += (s, e) => VolverAlDashboard();
            barraNavegacion.Controls.Add(btnVolver);
            Controls.Add(barraNavegacion);
            barraNavegacion.BringToFront();

            CrearSeccionRetorno();
            CrearSeccionAgregar();
            CambiarPestana(false);
        }

        private void VolverAlDashboard()
        {
            // Al cerrar, FormTrabajador vuelve a mostrarse desde su bloque finally.
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private Button CrearBoton(string texto, Color color, int ancho)
        {
            var btn = new Button
            {
                Text = texto,
                Width = ancho,
                Height = 43,
                Margin = new Padding(0, 0, 12, 0),
                BackColor = color,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void CrearSeccionRetorno()
        {
            var barra = new Panel { Dock = DockStyle.Top, Height = 112, BackColor = fondo };
            lblContador = new Label
            {
                Text = "0 equipos seleccionados",
                Dock = DockStyle.Top,
                Height = 28,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            var filaFiltros = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 57,
                ColumnCount = 2,
                BackColor = fondo,
                Padding = new Padding(0, 3, 0, 5)
            };
            filaFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            filaFiltros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            txtBuscarRetorno = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = campo,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 11),
                PlaceholderText = "Escanear código de barras o buscar nombre..."
            };
            txtBuscarRetorno.TextChanged += (s, e) => FiltrarEquiposRetorno();
            txtBuscarRetorno.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; FiltrarEquiposRetorno(); }
            };
            cboResponsableRetorno = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = campo,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 10)
            };
            cboResponsableRetorno.Items.Add("Todos los responsables");
            cboResponsableRetorno.SelectedIndex = 0;
            cboResponsableRetorno.SelectedIndexChanged += (s, e) => FiltrarEquiposRetorno();
            filaFiltros.Controls.Add(txtBuscarRetorno, 0, 0);
            filaFiltros.Controls.Add(cboResponsableRetorno, 1, 0);
            barra.Controls.Add(filaFiltros);
            barra.Controls.Add(lblContador);

            var pie = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 10, 0, 0)
            };
            var btnRetornar = CrearBoton("REGISTRAR DEVOLUCIÓN", naranja, 290);
            btnRetornar.Name = "btnRetornar";
            btnRetornar.Click += (s, e) => RegistrarRetorno();
            pie.Controls.Add(btnRetornar);
            flpEquipos = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = fondo,
                Padding = new Padding(0, 10, 0, 0)
            };
            lblSinEquipos = new Label
            {
                Text = "No hay equipos pendientes que coincidan con los filtros.",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = verde,
                Dock = DockStyle.Top,
                Height = 42,
                Visible = false
            };
            panelRetorno.Controls.Add(flpEquipos);
            panelRetorno.Controls.Add(lblSinEquipos);
            panelRetorno.Controls.Add(barra);
            panelRetorno.Controls.Add(pie);
            panelRetorno.Resize += (s, e) => AjustarTarjetas();
        }

        private void FiltrarEquiposRetorno()
        {
            if (filtrandoRetorno || flpEquipos == null) return;
            var texto = txtBuscarRetorno?.Text?.Trim() ?? "";
            string responsable = cboResponsableRetorno?.SelectedItem?.ToString() ?? "Todos los responsables";
            var visibles = equiposTodos.FindAll(eq =>
                (texto.Length == 0 ||
                 eq.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                 eq.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                 eq.Responsable.Contains(texto, StringComparison.OrdinalIgnoreCase)) &&
                (responsable == "Todos los responsables" || eq.Responsable == responsable));
            flpEquipos.SuspendLayout();
            flpEquipos.Controls.Clear();
            foreach (var eq in visibles) flpEquipos.Controls.Add(eq.Tarjeta);
            flpEquipos.ResumeLayout();
            lblSinEquipos.Visible = visibles.Count == 0;
            lblContador.Text = $"{equiposTodos.FindAll(e => e.Incluir).Count} seleccionados · {visibles.Count} visibles · {equiposTodos.Count} pendientes";
            AjustarTarjetas();
        }

        // Vista de búsqueda y preparación de una salida adicional.
        // El registro definitivo permanece deshabilitado hasta validar el modelo de datos.
        private void CrearSeccionAgregar()
        {
            tarjetaBusquedaEquipo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 167,
                BackColor = tarjeta,
                Padding = new Padding(18, 14, 18, 12)
            };
            var titulo = new Label
            {
                Text = "ESCANEAR O BUSCAR EQUIPO",
                Dock = DockStyle.Top,
                Height = 30,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            var ayuda = new Label
            {
                Text = "Escanea un código de barras o escribe el código o nombre del equipo.",
                Dock = DockStyle.Top,
                Height = 28,
                ForeColor = gris,
                Font = new Font("Segoe UI", 9.5f)
            };
            var fila = new Panel { Dock = DockStyle.Top, Height = 49 };
            var buscar = CrearBoton("BUSCAR", morado, 125);
            buscar.Dock = DockStyle.Right;
            buscar.Margin = new Padding(0);
            buscar.Click += (s, e) =>
            {
                temporizadorSugerencias.Stop();
                OcultarSugerenciasEquipo();
                BuscarEquipoParaAgregar();
            };
            txtEscaner = new TextBox
            {
                PlaceholderText = "Código de barras o nombre del equipo...",
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Font = new Font("Segoe UI", 12),
                BackColor = campo,
                ForeColor = blanco,
                BorderStyle = BorderStyle.FixedSingle
            };
            temporizadorSugerencias = new System.Windows.Forms.Timer { Interval = 300 };
            temporizadorSugerencias.Tick += (s, e) =>
            {
                temporizadorSugerencias.Stop();
                CargarSugerenciasEquipo();
            };
            txtEscaner.TextChanged += (s, e) =>
            {
                if (seleccionandoSugerencia) return;
                OcultarSugerenciasEquipo();
                temporizadorSugerencias.Stop();
                if (txtEscaner.Text.Trim().Length >= 2)
                    temporizadorSugerencias.Start();
            };
            txtEscaner.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Down && lstSugerenciasEquipo.Visible && lstSugerenciasEquipo.Items.Count > 0)
                {
                    e.SuppressKeyPress = true;
                    lstSugerenciasEquipo.Focus();
                    lstSugerenciasEquipo.SelectedIndex = 0;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    OcultarSugerenciasEquipo();
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    temporizadorSugerencias.Stop();
                    OcultarSugerenciasEquipo();
                    BuscarEquipoParaAgregar();
                }
            };
            fila.Controls.Add(txtEscaner);
            fila.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 12 });
            fila.Controls.Add(buscar);
            lblEstadoEscaneo = new Label
            {
                Text = "Listo para escanear. El lector USB puede enviar Enter automáticamente.",
                Dock = DockStyle.Top,
                Height = 31,
                ForeColor = gris,
                Font = new Font("Segoe UI", 9),
                AutoEllipsis = true
            };
            // Lista acotada a 4 resultados visibles. La tarjeta aumenta de altura
            // cuando hay sugerencias para NO tapar la tabla de equipos.
            panelSugerenciasEquipo = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 0,
                Visible = false,
                BackColor = campo,
                Padding = new Padding(0, 5, 0, 0)
            };
            lstSugerenciasEquipo = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = campo,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 10),
                IntegralHeight = false,
                ItemHeight = 32,
                HorizontalScrollbar = true
            };
            lstSugerenciasEquipo.MouseClick += (s, e) => ElegirSugerenciaEquipo();
            lstSugerenciasEquipo.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    ElegirSugerenciaEquipo();
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    e.SuppressKeyPress = true;
                    OcultarSugerenciasEquipo();
                    txtEscaner.Focus();
                }
            };
            panelSugerenciasEquipo.Controls.Add(lstSugerenciasEquipo);

            // Dock Top: orden inverso para mostrar primero el título.
            tarjetaBusquedaEquipo.Controls.Add(panelSugerenciasEquipo);
            tarjetaBusquedaEquipo.Controls.Add(lblEstadoEscaneo);
            tarjetaBusquedaEquipo.Controls.Add(fila);
            tarjetaBusquedaEquipo.Controls.Add(ayuda);
            tarjetaBusquedaEquipo.Controls.Add(titulo);

            var responsableAdicionalPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = tarjeta,
                Padding = new Padding(16, 7, 16, 10)
            };
            var labelResponsable = new Label
            {
                Text = "RESPONSABLE QUE RETIRARÁ Y DEVOLVERÁ LOS EQUIPOS NUEVOS",
                Dock = DockStyle.Top,
                Height = 27,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            cboResponsableAdicional = new ComboBox
            {
                Dock = DockStyle.None,
                Location = new Point(16, 36),
                Size = new Size(420, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = campo,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 10),
                IntegralHeight = false,
                MaxDropDownItems = 6,
                DropDownHeight = 160
            };
            cboResponsableAdicional.Items.Add("Selecciona un responsable...");
            cboResponsableAdicional.SelectedIndex = 0;
            cboResponsableAdicional.SelectedIndexChanged += (s, e) =>
            {
                // Asociar responsable también a los equipos ya seleccionados.
                if (cboResponsableAdicional.SelectedIndex < 1) return;
                var resp = responsablesDisponibles[cboResponsableAdicional.SelectedIndex - 1];
                foreach (var eq in seleccionAdicional.Values)
                {
                    eq.ResponsableTipo = resp.Tipo;
                    eq.ResponsableId = resp.Id;
                    eq.ResponsableNombre = resp.Nombre;
                }
                RefrescarSeleccionAdicional();
            };
            responsableAdicionalPanel.Controls.Add(cboResponsableAdicional);
            responsableAdicionalPanel.Controls.Add(labelResponsable);

            var cabeceraLista = new Panel { Dock = DockStyle.Top, Height = 59, BackColor = fondo };
            cabeceraLista.Controls.Add(new Label
            {
                Text = "EQUIPOS SELECCIONADOS",
                Dock = DockStyle.Left,
                Width = 430,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            });
            lblResumenAgregar = new Label
            {
                Text = "0 equipos · 0 unidades",
                Dock = DockStyle.Right,
                Width = 260,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = morado,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            cabeceraLista.Controls.Add(lblResumenAgregar);

            dgvEquiposAgregar = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = tarjeta,
                BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(52, 56, 76),
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 44,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                RowTemplate = { Height = 43 },
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 10)
            };
            dgvEquiposAgregar.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(36, 39, 61);
            dgvEquiposAgregar.ColumnHeadersDefaultCellStyle.ForeColor = blanco;
            dgvEquiposAgregar.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgvEquiposAgregar.DefaultCellStyle.BackColor = tarjeta;
            dgvEquiposAgregar.DefaultCellStyle.ForeColor = blanco;
            dgvEquiposAgregar.DefaultCellStyle.SelectionBackColor = Color.FromArgb(65, 56, 112);
            dgvEquiposAgregar.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvEquiposAgregar.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(31, 34, 53);
            dgvEquiposAgregar.AlternatingRowsDefaultCellStyle.ForeColor = blanco;
            dgvEquiposAgregar.Columns.Add("codigo", "CÓDIGO");
            dgvEquiposAgregar.Columns.Add("nombre", "EQUIPO AUDIOVISUAL");
            dgvEquiposAgregar.Columns.Add("cantidad", "CANTIDAD");
            dgvEquiposAgregar.Columns.Add("disponible", "DISPONIBLE");
            dgvEquiposAgregar.Columns.Add("responsableNuevo", "RESPONSABLE");
            dgvEquiposAgregar.Columns[0].FillWeight = 25;
            dgvEquiposAgregar.Columns[1].FillWeight = 50;
            dgvEquiposAgregar.Columns[2].FillWeight = 15;
            dgvEquiposAgregar.Columns[3].FillWeight = 15;
            dgvEquiposAgregar.Columns[4].FillWeight = 35;

            // Mensaje de estado vacío: no interfiere con los encabezados.
            lblTablaVacia = new Label
            {
                Text = "Todavía no hay equipos en la selección.\n\nEscanea un código o busca un equipo para comenzar.",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = gris,
                BackColor = tarjeta,
                Font = new Font("Segoe UI", 11),
                Dock = DockStyle.Fill
            };
            var contenedorTabla = new Panel { Dock = DockStyle.Fill, BackColor = tarjeta };
            contenedorTabla.Controls.Add(dgvEquiposAgregar);
            // El mensaje vacío se muestra en una franja dentro de la tabla, sin ocultar columnas.
            lblTablaVacia.Dock = DockStyle.Top;
            lblTablaVacia.Height = 130;
            contenedorTabla.Controls.Add(lblTablaVacia);
            dgvEquiposAgregar.BringToFront();
            lblTablaVacia.BringToFront();

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 118, BackColor = fondo };
            var nota = new Label
            {
                Text = "La selección es una vista previa. Guardar una nueva salida aún requiere validar responsables y asignaciones.",
                ForeColor = amarillo,
                Dock = DockStyle.Top,
                Height = 44,
                Font = new Font("Segoe UI", 9)
            };
            var filaBotones = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 10, 0, 0),
                WrapContents = false
            };
            btnGuardarAdicional = CrearBoton("CONFIRMAR SALIDA ADICIONAL", morado, 300);
            btnGuardarAdicional.Enabled = false;
            var quitar = CrearBoton("QUITAR SELECCIONADO", Color.FromArgb(65, 68, 88), 220);
            quitar.Click += (s, e) => QuitarEquipoSeleccionado();
            filaBotones.Controls.Add(btnGuardarAdicional);
            filaBotones.Controls.Add(quitar);
            footer.Controls.Add(filaBotones);
            footer.Controls.Add(nota);

            panelAgregar.Controls.Add(contenedorTabla);
            panelAgregar.Controls.Add(cabeceraLista);
            panelAgregar.Controls.Add(footer);
            // Se mostrará debajo del buscador y encima de la selección nueva.
            panelAgregar.Controls.Add(responsableAdicionalPanel);
            panelAgregar.Controls.Add(CrearPanelAsignados());
            panelAgregar.Controls.Add(tarjetaBusquedaEquipo);
        }

        private Panel CrearPanelAsignados()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 169,
                BackColor = Color.FromArgb(23, 26, 43),
                Padding = new Padding(12, 8, 12, 8)
            };
            lblAsignados = new Label
            {
                Text = "EQUIPOS QUE YA SALIERON EN ESTE EVENTO",
                Dock = DockStyle.Top,
                Height = 30,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            dgvYaAsignados = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = tarjeta,
                BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(51, 54, 77),
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 29,
                Font = new Font("Segoe UI", 9)
            };
            dgvYaAsignados.ColumnHeadersDefaultCellStyle.BackColor = campo;
            dgvYaAsignados.ColumnHeadersDefaultCellStyle.ForeColor = blanco;
            dgvYaAsignados.DefaultCellStyle.BackColor = tarjeta;
            dgvYaAsignados.DefaultCellStyle.ForeColor = blanco;
            dgvYaAsignados.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 55, 100);
            dgvYaAsignados.DefaultCellStyle.SelectionForeColor = blanco;
            dgvYaAsignados.Columns.Add("codigoPrevio", "CÓDIGO");
            dgvYaAsignados.Columns.Add("nombrePrevio", "EQUIPO");
            dgvYaAsignados.Columns.Add("responsablePrevio", "RESPONSABLE");
            dgvYaAsignados.Columns.Add("estadoPrevio", "SITUACIÓN");
            dgvYaAsignados.Columns[0].FillWeight = 25;
            dgvYaAsignados.Columns[1].FillWeight = 35;
            dgvYaAsignados.Columns[2].FillWeight = 35;
            dgvYaAsignados.Columns[3].FillWeight = 25;
            panel.Controls.Add(dgvYaAsignados);
            panel.Controls.Add(lblAsignados);
            return panel;
        }

        private void CargarEquiposYaAsignados()
        {
            historialAsignacionesValidado = false;
            equiposYaAsignados.Clear();
            dgvYaAsignados.Rows.Clear();
            if (idSalida == 0) return;

            var conexionDB = new Conexion();
            try
            {
                var con = conexionDB.ObtenerConexion();
                if (con.State != System.Data.ConnectionState.Open) con.Open();

                // Incluye salidas iniciales y adicionales del mismo evento.
                // No se permite volver a seleccionar un equipo ya asignado,
                // aunque su devolución haya sido registrada.
                const string sql = @"
                    SELECT DISTINCT e.id, e.codigo, e.nombre,
                           COALESCE(
                             (SELECT COALESCE(t.nombre, CONCAT_WS(' ', p.nombre, p.apellido))
                              FROM asignaciones_equipos ae
                              LEFT JOIN trabajadores t ON t.id_trabajador = ae.id_trabajador
                              LEFT JOIN personal_externo p ON p.id_personal = ae.id_personal_externo
                              WHERE ae.id_evento = s.id_evento AND ae.id_equipo = e.id
                              LIMIT 1),
                             s.encargado, 'Sin responsable') AS responsable,
                           CASE WHEN EXISTS (
                             SELECT 1 FROM salida_equipos sx
                             JOIN salidas ss ON ss.id_salida = sx.id_salida
                             WHERE sx.id_equipo = e.id AND ss.id_evento = s.id_evento
                               AND NOT EXISTS (
                                 SELECT 1 FROM retornos r
                                 WHERE r.id_salida = ss.id_salida AND r.id_equipo = e.id
                               )
                           ) THEN 'Pendiente de devolución' ELSE 'Devuelto' END AS situacion
                    FROM equipos e
                    JOIN salida_equipos se ON se.id_equipo = e.id
                    JOIN salidas s ON s.id_salida = se.id_salida
                    WHERE (@evento > 0 AND s.id_evento = @evento)
                       OR (@evento = 0 AND s.id_salida = @salida)
                    ORDER BY e.nombre";
                using var cmd = new NpgsqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@evento", idEvento);
                cmd.Parameters.AddWithValue("@salida", idSalida);
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    int id = Convert.ToInt32(rd["id"]);
                    equiposYaAsignados.Add(id);
                    dgvYaAsignados.Rows.Add(
                        rd["codigo"]?.ToString() ?? "",
                        rd["nombre"]?.ToString() ?? "",
                        rd["responsable"]?.ToString() ?? "Sin responsable",
                        rd["situacion"]?.ToString() ?? "");
                }
                lblAsignados.Text = $"EQUIPOS QUE YA SALIERON EN ESTE EVENTO ({equiposYaAsignados.Count})";
                historialAsignacionesValidado = true;
            }
            catch (Exception ex)
            {
                lblAsignados.Text = "No se pudo cargar el historial del evento";
                lblEstadoEscaneo.Text = "Error al verificar equipos asignados: " + ex.Message;
                lblEstadoEscaneo.ForeColor = rojo;
            }
            finally { conexionDB.CerrarConexion(); }
        }

        private void CargarResponsablesDisponibles()
        {
            responsablesDisponibles.Clear();
            cboResponsableAdicional.Items.Clear();
            cboResponsableAdicional.Items.Add("Selecciona un responsable...");
            var conexion = new Conexion();
            try
            {
                var con = conexion.ObtenerConexion();
                if (con.State != System.Data.ConnectionState.Open) con.Open();
                const string sql = @"
                    SELECT DISTINCT tipo, id, nombre
                    FROM (
                        -- Encargado principal del evento
                        SELECT
                            'T' AS tipo,
                            t.id_trabajador AS id,
                            t.nombre AS nombre
                        FROM eventos ev
                        INNER JOIN trabajadores t
                            ON t.id_trabajador = ev.id_trabajador
                        WHERE ev.id = @evento
                          AND t.activo = TRUE

                        UNION

                        -- Trabajadores con equipos asignados al evento
                        SELECT
                            'T',
                            t.id_trabajador,
                            t.nombre
                        FROM asignaciones_equipos ae
                        INNER JOIN trabajadores t
                            ON t.id_trabajador = ae.id_trabajador
                        WHERE ae.id_evento = @evento
                          AND t.activo = TRUE

                        UNION

                        -- Personal externo con equipos asignados
                        SELECT
                            'E',
                            p.id_personal,
                            CONCAT_WS(' ', p.nombre, p.apellido)
                        FROM asignaciones_equipos ae
                        INNER JOIN personal_externo p
                            ON p.id_personal = ae.id_personal_externo
                        WHERE ae.id_evento = @evento
                          AND p.activo = TRUE
                    ) AS participantes
                    ORDER BY nombre";
                using var cmd = new NpgsqlCommand(sql, con);

                // Identificar el evento seleccionado
                cmd.Parameters.AddWithValue("@evento", idEvento);

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var persona = (r["tipo"].ToString() ?? "T",
                                   Convert.ToInt32(r["id"]),
                                   r["nombre"].ToString() ?? "Sin nombre");
                    responsablesDisponibles.Add(persona);
                    cboResponsableAdicional.Items.Add(
                        persona.Item3 + (persona.Item1 == "E" ? " (Externo)" : " (Trabajador)"));
                }
                cboResponsableAdicional.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                cboResponsableAdicional.Items.Clear();
                cboResponsableAdicional.Items.Add("Error al cargar responsables");
                cboResponsableAdicional.SelectedIndex = 0;
                lblEstadoEscaneo.Text = "No se pudieron cargar responsables: " + ex.Message;
                lblEstadoEscaneo.ForeColor = rojo;
            }
            finally { conexion.CerrarConexion(); }
        }

        private void CambiarPestana(bool retorno)
        {
            temporizadorSugerencias?.Stop();
            OcultarSugerenciasEquipo();
            panelRetorno.Visible = retorno;
            panelAgregar.Visible = !retorno;
            btnTabRetorno.BackColor = retorno ? naranja : Color.FromArgb(65, 68, 88);
            btnTabAgregar.BackColor = !retorno ? morado : Color.FromArgb(65, 68, 88);
            if (retorno) panelRetorno.BringToFront();
            else
            {
                panelAgregar.BringToFront();
                txtEscaner?.Focus();
            }
        }

        private void AjustarTarjetas()
        {
            if (flpEquipos == null) return;
            int width = Math.Max(920, flpEquipos.ClientSize.Width - 28);
            foreach (Control control in flpEquipos.Controls)
                control.Width = width;
        }

        private void BuscarEquipoParaAgregar()
        {
            string codigo = txtEscaner.Text.Trim();
            if (codigo.Length == 0)
            {
                lblEstadoEscaneo.Text = "Ingresa o escanea un código.";
                lblEstadoEscaneo.ForeColor = amarillo;
                txtEscaner.Focus();
                return;
            }

            if (!historialAsignacionesValidado)
            {
                lblEstadoEscaneo.Text = "Primero se debe cargar correctamente el historial del evento.";
                lblEstadoEscaneo.ForeColor = amarillo;
                return;
            }

            var conexionDB = new Conexion();
            try
            {
                var con = conexionDB.ObtenerConexion();
                if (con.State != System.Data.ConnectionState.Open) con.Open();
                const string sql = @"SELECT id, codigo, nombre, stock, estado
                                     FROM equipos
                                     WHERE LOWER(codigo) = LOWER(@codigo)
                                        OR LOWER(nombre) = LOWER(@codigo)
                                     ORDER BY CASE WHEN LOWER(codigo) = LOWER(@codigo) THEN 0 ELSE 1 END
                                     LIMIT 2";
                using var cmd = new NpgsqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@evento", idEvento);
                cmd.Parameters.AddWithValue("@codigo", codigo);
                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    lblEstadoEscaneo.Text = $"No se encontró '{codigo}'. Comprueba el código o escribe el nombre exacto.";
                    lblEstadoEscaneo.ForeColor = rojo;
                    return;
                }

                var equipo = new EquipoAdicional
                {
                    Id = Convert.ToInt32(reader["id"]),
                    Codigo = reader["codigo"]?.ToString() ?? "",
                    Nombre = reader["nombre"]?.ToString() ?? "",
                    Disponible = Convert.ToInt32(reader["stock"])
                };
                string estado = reader["estado"]?.ToString() ?? "";
                // Si el nombre corresponde a varias referencias, no elegir una al azar.
                if (reader.Read())
                {
                    lblEstadoEscaneo.Text = "Hay varios equipos con ese nombre. Escanea o introduce el código exacto.";
                    lblEstadoEscaneo.ForeColor = amarillo;
                    return;
                }
                if (equiposYaAsignados.Contains(equipo.Id))
                {
                    lblEstadoEscaneo.Text = $"{equipo.Codigo} - {equipo.Nombre} ya fue asignado a este evento. No puedes agregarlo nuevamente.";
                    lblEstadoEscaneo.ForeColor = amarillo;
                    return;
                }
                if (equipo.Disponible <= 0 ||
                    estado.Equals("En mantenimiento", StringComparison.OrdinalIgnoreCase))
                {
                    lblEstadoEscaneo.Text = $"{equipo.Nombre} no está disponible ({estado}, stock: {equipo.Disponible}).";
                    lblEstadoEscaneo.ForeColor = rojo;
                    return;
                }

                if (seleccionAdicional.TryGetValue(equipo.Id, out var existente))
                {
                    if (existente.Cantidad >= equipo.Disponible)
                    {
                        lblEstadoEscaneo.Text = "No puedes seleccionar más unidades que el stock disponible.";
                        lblEstadoEscaneo.ForeColor = amarillo;
                        return;
                    }
                    existente.Cantidad++;
                }
                else
                {
                    if (cboResponsableAdicional.SelectedIndex <= 0)
                    {
                        lblEstadoEscaneo.Text = "Primero selecciona a la persona responsable del equipo.";
                        lblEstadoEscaneo.ForeColor = amarillo;
                        return;
                    }
                    var resp = responsablesDisponibles[cboResponsableAdicional.SelectedIndex - 1];
                    equipo.ResponsableTipo = resp.Tipo;
                    equipo.ResponsableId = resp.Id;
                    equipo.ResponsableNombre = resp.Nombre;
                    seleccionAdicional.Add(equipo.Id, equipo);
                }

                RefrescarSeleccionAdicional();
                lblEstadoEscaneo.Text = $"Equipo agregado a la lista: {equipo.Codigo} - {equipo.Nombre}.";
                lblEstadoEscaneo.ForeColor = verde;
            }
            catch (Exception ex)
            {
                lblEstadoEscaneo.Text = "Error al consultar el código: " + ex.Message;
                lblEstadoEscaneo.ForeColor = rojo;
            }
            finally
            {
                conexionDB.CerrarConexion();
                txtEscaner.Clear();
                txtEscaner.Focus();
            }
        }

        // Autocompletado: código + nombre (sin afectar el lector de barras USB).
        private void CargarSugerenciasEquipo()
        {
            string texto = txtEscaner.Text.Trim();
            if (texto.Length < 2 || !panelAgregar.Visible) return;

            var conexionDB = new Conexion();
            try
            {
                var con = conexionDB.ObtenerConexion();
                if (con.State != System.Data.ConnectionState.Open) con.Open();
                const string sql = @"
                    SELECT codigo, nombre, stock
                    FROM equipos
                    WHERE (codigo ILIKE @buscar OR nombre ILIKE @buscar)
                      AND NOT EXISTS (
                        SELECT 1 FROM salida_equipos se
                        JOIN salidas s ON s.id_salida = se.id_salida
                        WHERE se.id_equipo = equipos.id
                          AND ((@evento > 0 AND s.id_evento = @evento)
                            OR (@evento = 0 AND s.id_salida = @salida))
                      )
                    ORDER BY
                        CASE WHEN LOWER(codigo) = LOWER(@exacto) THEN 0
                             WHEN codigo ILIKE @prefijo THEN 1 ELSE 2 END,
                        nombre
                    LIMIT 6";
                using var cmd = new NpgsqlCommand(sql, con);
                cmd.CommandTimeout = 5;
                cmd.Parameters.AddWithValue("@buscar", "%" + EscaparLike(texto) + "%");
                cmd.Parameters.AddWithValue("@prefijo", EscaparLike(texto) + "%");
                cmd.Parameters.AddWithValue("@exacto", texto);
                cmd.Parameters.AddWithValue("@evento", idEvento);
                cmd.Parameters.AddWithValue("@salida", idSalida);
                using var rd = cmd.ExecuteReader();
                lstSugerenciasEquipo.Items.Clear();
                while (rd.Read())
                {
                    lstSugerenciasEquipo.Items.Add(new SugerenciaEquipo
                    {
                        Codigo = rd["codigo"]?.ToString() ?? "",
                        Nombre = rd["nombre"]?.ToString() ?? "",
                        Stock = Convert.ToInt32(rd["stock"])
                    });
                }
                if (lstSugerenciasEquipo.Items.Count > 0)
                {
                    int alto = Math.Min(4, lstSugerenciasEquipo.Items.Count) * 32 + 10;
                    panelSugerenciasEquipo.Height = alto;
                    panelSugerenciasEquipo.Visible = true;
                    tarjetaBusquedaEquipo.Height = 167 + alto;
                }
            }
            catch (Exception ex)
            {
                OcultarSugerenciasEquipo();
                lblEstadoEscaneo.Text = "No se pudieron consultar sugerencias: " + ex.Message;
                lblEstadoEscaneo.ForeColor = rojo;
            }
            finally { conexionDB.CerrarConexion(); }
        }

        // Escapa los comodines de LIKE para que %, _ y \ se busquen como texto.
        private static string EscaparLike(string valor) =>
            valor.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

        private void OcultarSugerenciasEquipo()
        {
            if (panelSugerenciasEquipo == null) return;
            panelSugerenciasEquipo.Visible = false;
            panelSugerenciasEquipo.Height = 0;
            tarjetaBusquedaEquipo.Height = 167;
        }

        private void ElegirSugerenciaEquipo()
        {
            if (lstSugerenciasEquipo.SelectedItem is not SugerenciaEquipo opcion) return;
            temporizadorSugerencias.Stop();
            seleccionandoSugerencia = true;
            txtEscaner.Text = opcion.Codigo;
            seleccionandoSugerencia = false;
            OcultarSugerenciasEquipo();
            BuscarEquipoParaAgregar();
        }

        private void QuitarEquipoSeleccionado()
        {
            if (dgvEquiposAgregar.SelectedRows.Count == 0) return;
            var row = dgvEquiposAgregar.SelectedRows[0];
            if (row.Tag is int id)
            {
                seleccionAdicional.Remove(id);
                RefrescarSeleccionAdicional();
            }
            txtEscaner.Focus();
        }

        private void RefrescarSeleccionAdicional()
        {
            dgvEquiposAgregar.Rows.Clear();
            foreach (var eq in seleccionAdicional.Values.OrderBy(e => e.Nombre))
            {
                int i = dgvEquiposAgregar.Rows.Add(
                    eq.Codigo, eq.Nombre, eq.Cantidad, eq.Disponible, eq.ResponsableNombre);
                dgvEquiposAgregar.Rows[i].Tag = eq.Id;
            }
            int unidades = seleccionAdicional.Values.Sum(e => e.Cantidad);
            lblResumenAgregar.Text = $"{seleccionAdicional.Count} equipos · {unidades} unidades";
            lblTablaVacia.Visible = seleccionAdicional.Count == 0;
        }

        // ==========================================
        // CARGAR DATOS
        // ==========================================
        private void CargarDatos()
        {
            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();

                // 🔥 Verificar que la conexión esté viva
                if (con.State != System.Data.ConnectionState.Open)
                {
                    con.Open();
                }

                // ==========================================
                // 1. Obtener la salida del día
                // ==========================================
                string querySalida = @"
                    SELECT s.id_salida, s.encargado, s.ubicacion, s.id_evento,
                           COALESCE(ev.titulo, 'Evento sin título') AS titulo_evento
                    FROM salidas s
                    LEFT JOIN eventos ev ON ev.id = s.id_evento
                    WHERE DATE(s.fecha_salida) = @fecha
                      AND (s.estado IS NULL OR s.estado <> 'Devuelto')
                    ORDER BY s.id_salida DESC
                    LIMIT 1";

                int idSalidaTmp = 0;
                string encargado = "";
                string ubicacion = "";
                string tituloEvento = "Evento";

                // 🔥 Usar un bloque separado para el reader
                using (NpgsqlCommand cmd = new NpgsqlCommand(querySalida, con))
                {
                    cmd.CommandTimeout = 60;
                    cmd.Parameters.AddWithValue("@fecha", fechaSeleccionada.Date);

                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            idSalidaTmp = Convert.ToInt32(reader["id_salida"]);
                            encargado = reader["encargado"].ToString();
                            ubicacion = reader["ubicacion"].ToString();
                            tituloEvento = reader["titulo_evento"]?.ToString() ?? "Evento";
                            idEvento = reader["id_evento"] == DBNull.Value ? 0 : Convert.ToInt32(reader["id_evento"]);
                        }
                        else
                        {
                            lblSalidaInfo.Text = "No se encontró una salida activa para este día.";
                            lblSalidaInfo.ForeColor = rojo;
                            return;
                        }
                    }
                }

                // Guardar fuera del using
                idSalida = idSalidaTmp;
                lblTituloEvento.Text = tituloEvento;
                lblSalidaInfo.Text =
                    $"Salida del {fechaSeleccionada:dd/MM/yyyy}   |   " +
                    $"Encargado: {encargado}   |   Ubicación: {ubicacion}";
                CargarEquiposYaAsignados();
                CargarResponsablesDisponibles();

                // ==========================================
                // 2. Obtener SOLO los equipos que aún NO han sido retornados
                // ==========================================
                string queryEquipos = @"
                    SELECT e.id, e.codigo, e.nombre, s.id_salida,
                           COALESCE(
                             (SELECT COALESCE(t.nombre, CONCAT_WS(' ', p.nombre, p.apellido))
                              FROM asignaciones_equipos ae
                              LEFT JOIN trabajadores t ON t.id_trabajador = ae.id_trabajador
                              LEFT JOIN personal_externo p ON p.id_personal = ae.id_personal_externo
                              WHERE ae.id_evento = s.id_evento
                                AND ae.id_equipo = e.id
                              LIMIT 1),
                             s.encargado, 'Sin responsable') AS responsable
                    FROM equipos e
                    JOIN salida_equipos se ON se.id_equipo = e.id
                    JOIN salidas s ON s.id_salida = se.id_salida
                    WHERE ((@evento > 0 AND s.id_evento = @evento)
                        OR (@evento = 0 AND s.id_salida = @salida))
                      AND NOT EXISTS (
                          SELECT 1 FROM retornos r
                          WHERE r.id_salida = se.id_salida
                            AND r.id_equipo = se.id_equipo
                      )
                    ORDER BY e.nombre, s.id_salida";

                using (NpgsqlCommand cmd = new NpgsqlCommand(queryEquipos, con))
                {
                    cmd.CommandTimeout = 60;
                    cmd.Parameters.AddWithValue("@salida", idSalida);
                    cmd.Parameters.AddWithValue("@evento", idEvento);

                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            equipos.Add(new EquipoRetorno
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                Codigo = reader["codigo"].ToString(),
                                Nombre = reader["nombre"].ToString(),
                                IdSalida = Convert.ToInt32(reader["id_salida"]),
                                Responsable = reader["responsable"]?.ToString() ?? "Sin responsable"
                            });
                        }
                    }
                }

                // ==========================================
                // 3. Si no quedan equipos pendientes
                // ==========================================
                if (equipos.Count == 0)
                {
                    lblSinEquipos.Visible = true;
                    lblContador.Visible = false;

                    var btnRet = this.Controls.Find("btnRetornar", true);
                    if (btnRet.Length > 0) btnRet[0].Enabled = false;

                    return;
                }

                // ==========================================
                // 4. Crear tarjetas
                // ==========================================
                foreach (var eq in equipos)
                {
                    CrearTarjetaEquipo(eq);
                    equiposTodos.Add(eq);
                }

                filtrandoRetorno = true;
                cboResponsableRetorno.Items.Clear();
                cboResponsableRetorno.Items.Add("Todos los responsables");
                foreach (var nombre in equiposTodos.Select(e => e.Responsable).Distinct().OrderBy(x => x))
                    cboResponsableRetorno.Items.Add(nombre);
                cboResponsableRetorno.SelectedIndex = 0;
                filtrandoRetorno = false;
                FiltrarEquiposRetorno();
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                if (ex.InnerException != null)
                    msg += "\n\nDetalle: " + ex.InnerException.Message;

                MessageBox.Show("Error al cargar datos:\n" + msg,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }
        }

        // ==========================================
        // CREAR TARJETA
        // ==========================================
        private void CrearTarjetaEquipo(EquipoRetorno eq)
        {
            Panel card = new Panel
            {
                Width = 1010,
                Height = 130,
                BackColor = tarjeta,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(15)
            };
            eq.Tarjeta = card;

            // Checkbox DESMARCADO por defecto
            CheckBox chkIncluir = new CheckBox
            {
                Text = "",
                Location = new Point(10, 15),
                Size = new Size(20, 20),
                Checked = false,           // ← AHORA FALSE
                Cursor = Cursors.Hand
            };
            chkIncluir.CheckedChanged += (s, e) =>
            {
                eq.Incluir = chkIncluir.Checked;
                card.BackColor = eq.Incluir
                    ? Color.FromArgb(35, 38, 58)   // Seleccionado: un poco más claro
                    : tarjeta;
                ActualizarContador();
            };
            eq.ChkIncluir = chkIncluir;
            card.Controls.Add(chkIncluir);

            // Código
            Label lblCodigo = new Label
            {
                Text = eq.Codigo,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = morado,
                Location = new Point(40, 12),
                AutoSize = true
            };
            card.Controls.Add(lblCodigo);

            // Nombre
            Label lblNombre = new Label
            {
                Text = eq.Nombre,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(40, 35),
                AutoSize = true
            };
            card.Controls.Add(lblNombre);

            card.Controls.Add(new Label
            {
                Text = "Responsable de devolución: " + eq.Responsable,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = amarillo,
                Location = new Point(40, 55),
                Size = new Size(560, 20),
                AutoEllipsis = true
            });

            // Radio Buen estado
            RadioButton rbBueno = new RadioButton
            {
                Text = "✓ Buen estado",
                Font = new Font("Segoe UI", 9),
                ForeColor = verde,
                Location = new Point(40, 75),
                Size = new Size(180, 22),
                Checked = true,
                Cursor = Cursors.Hand
            };
            rbBueno.CheckedChanged += (s, e) =>
            {
                if (rbBueno.Checked)
                {
                    eq.Estado = "Buen estado";
                    eq.TxtObservaciones.Enabled = false;
                    eq.TxtObservaciones.Text = "";
                }
            };
            eq.RbBueno = rbBueno;
            card.Controls.Add(rbBueno);

            // Radio Presenta fallas
            RadioButton rbFalla = new RadioButton
            {
                Text = "⚠ Presenta fallas",
                Font = new Font("Segoe UI", 9),
                ForeColor = amarillo,
                Location = new Point(230, 75),
                Size = new Size(180, 22),
                Cursor = Cursors.Hand
            };
            rbFalla.CheckedChanged += (s, e) =>
            {
                if (rbFalla.Checked)
                {
                    eq.Estado = "Presenta fallas";
                    eq.TxtObservaciones.Enabled = true;
                    eq.TxtObservaciones.Focus();
                }
            };
            eq.RbFalla = rbFalla;
            card.Controls.Add(rbFalla);

            // Radio Está fallando
            RadioButton rbFallando = new RadioButton
            {
                Text = "✗ Está fallando",
                Font = new Font("Segoe UI", 9),
                ForeColor = rojo,
                Location = new Point(420, 75),
                Size = new Size(180, 22),
                Cursor = Cursors.Hand
            };
            rbFallando.CheckedChanged += (s, e) =>
            {
                if (rbFallando.Checked)
                {
                    eq.Estado = "Está fallando";
                    eq.TxtObservaciones.Enabled = true;
                    eq.TxtObservaciones.Focus();
                }
            };
            eq.RbFallando = rbFallando;
            card.Controls.Add(rbFallando);

            // Observaciones
            TextBox txtObs = new TextBox
            {
                Location = new Point(620, 12),
                Size = new Size(370, 28),
                BackColor = campo,
                ForeColor = blanco,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9),
                Enabled = false,
                Text = ""
            };
            eq.TxtObservaciones = txtObs;
            card.Controls.Add(txtObs);

            Label lblObs = new Label
            {
                Text = "Observaciones (obligatorio si hay fallas):",
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = gris,
                Location = new Point(620, 45),
                AutoSize = true
            };
            card.Controls.Add(lblObs);

            flpEquipos.Controls.Add(card);
        }

        // ==========================================
        // CONTADOR
        // ==========================================
        private void ActualizarContador()
        {
            int seleccionados = 0;
            foreach (var eq in equipos)
                if (eq.Incluir) seleccionados++;

            lblContador.Text = $"{seleccionados} de {equipos.Count} equipos seleccionados";
        }

        // ==========================================
        // REGISTRAR RETORNO
        // ==========================================
        private void RegistrarRetorno()
        {
            var equiposSeleccionados = equipos.FindAll(e => e.Incluir);

            if (equiposSeleccionados.Count == 0)
            {
                MessageBox.Show(
                    "Marca al menos un equipo (con el checkbox) para retornar.",
                    "Sin equipos seleccionados",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Validar observaciones
            foreach (var eq in equiposSeleccionados)
            {
                bool requiereObs = eq.Estado == "Presenta fallas" || eq.Estado == "Está fallando";
                if (requiereObs && string.IsNullOrWhiteSpace(eq.TxtObservaciones.Text))
                {
                    MessageBox.Show(
                        $"Debes describir la falla del equipo:\n{eq.Codigo} - {eq.Nombre}",
                        "Observación requerida",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    eq.TxtObservaciones.Focus();
                    return;
                }

                eq.Observaciones = eq.TxtObservaciones.Text.Trim();
            }

            // Confirmar
            string resumen = $"¿Confirmar retorno de {equiposSeleccionados.Count} equipo(s)?\n\n";
            foreach (var eq in equiposSeleccionados)
                resumen += $"• {eq.Codigo} - {eq.Estado}\n";

            if (MessageBox.Show(resumen, "Confirmar retorno",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            Conexion conexionDB = new Conexion();
            NpgsqlTransaction transaccion = null;

            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();
                transaccion = con.BeginTransaction();

                foreach (var eq in equiposSeleccionados)
                {
                    // INSERT en retornos
                    string insertRetorno = @"
                        INSERT INTO retornos
                        (id_salida, id_trabajador, id_equipo, fecha_retorno, observaciones, estado, estado_equipo)
                        VALUES (@salida, @trabajador, @equipo, @fecha, @obs, 'Completado', @estadoEquipo)";

                    using (NpgsqlCommand cmdRet = new NpgsqlCommand(insertRetorno, con, transaccion))
                    {
                        cmdRet.Parameters.AddWithValue("@salida", eq.IdSalida);
                        cmdRet.Parameters.AddWithValue("@trabajador", idTrabajador);
                        cmdRet.Parameters.AddWithValue("@equipo", eq.Id);
                        cmdRet.Parameters.AddWithValue("@fecha", DateTime.Now);
                        cmdRet.Parameters.AddWithValue("@obs",
                            string.IsNullOrWhiteSpace(eq.Observaciones)
                                ? (object)DBNull.Value
                                : eq.Observaciones);
                        cmdRet.Parameters.AddWithValue("@estadoEquipo", eq.Estado);
                        cmdRet.ExecuteNonQuery();
                    }

                    // UPDATE estado del equipo
                    // UPDATE estado del equipo + SUBIR STOCK
                    string nuevoEstado = eq.Estado == "Buen estado"
                        ? "Disponible"
                        : eq.Estado == "Presenta fallas"
                            ? "Con observaciones"
                            : "En mantenimiento";

                    string updateEquipo = @"
                        UPDATE equipos
                        SET estado = @estado,
                            stock = stock + 1
                        WHERE id = @id";

                    using (NpgsqlCommand cmdEq = new NpgsqlCommand(updateEquipo, con, transaccion))
                    {
                        cmdEq.Parameters.AddWithValue("@estado", nuevoEstado);
                        cmdEq.Parameters.AddWithValue("@id", eq.Id);
                        cmdEq.ExecuteNonQuery();
                    }
                }

                // Finalizar cada salida cuando todos sus equipos hayan regresado.
                // Se revisan las salidas afectadas, no solamente la primera del evento.
                foreach (int salidaAfectada in equiposSeleccionados.Select(e => e.IdSalida).Distinct())
                {
                    using var cmdPend = new NpgsqlCommand(@"
                        SELECT COUNT(*) FROM salida_equipos se
                        WHERE se.id_salida = @salida
                          AND NOT EXISTS (
                            SELECT 1 FROM retornos r
                            WHERE r.id_salida = se.id_salida
                              AND r.id_equipo = se.id_equipo)", con, transaccion);
                    cmdPend.Parameters.AddWithValue("@salida", salidaAfectada);
                    int pendientes = Convert.ToInt32(cmdPend.ExecuteScalar());
                    if (pendientes == 0)
                    {
                        using var cmdUpd = new NpgsqlCommand(
                            "UPDATE salidas SET estado = 'Devuelto' WHERE id_salida = @id",
                            con, transaccion);
                        cmdUpd.Parameters.AddWithValue("@id", salidaAfectada);
                        cmdUpd.ExecuteNonQuery();
                    }
                }

                transaccion.Commit();

                MessageBox.Show(
                    $"{equiposSeleccionados.Count} equipo(s) retornado(s) correctamente.",
                    "Éxito",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                if (transaccion != null)
                {
                    try { transaccion.Rollback(); } catch { }
                }

                MessageBox.Show(
                    "Error al registrar retorno:\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }
        }
    }
}