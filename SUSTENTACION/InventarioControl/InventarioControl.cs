using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace SUSTENTACION
{
    /// <summary>
    /// Panel de inventario audiovisual Artemusa TV.
    /// Usa Conexion.ObtenerConexion() y las tablas/vistas del esquema artemusa_inventario.
    /// </summary>
    public class InventarioControl : UserControl
    {
        private sealed class CodigoEquipoDuplicadoException : Exception
        {
            public CodigoEquipoDuplicadoException(string codigo)
                : base("El código '" + codigo + "' ya está registrado. Si es el mismo equipo y necesitas aumentar las existencias, usa el botón Entrada. Para registrar otro equipo por separado, escribe un código distinto.")
            {
            }
        }

        private readonly Color colorFondo = Color.FromArgb(241, 245, 248);
        private readonly Color colorTexto = Color.FromArgb(24, 42, 55);
        private readonly Color colorSecundario = Color.FromArgb(104, 122, 137);
        private readonly Color colorVerde = Color.FromArgb(18, 124, 91);
        private readonly Color colorLinea = Color.FromArgb(224, 232, 237);
        private readonly List<Font> fuentes = new List<Font>();
        private readonly List<Button> botonesEscritura = new List<Button>();

        private Label[] indicadores = new Label[4];
        private Label lblEstadoConexion;
        private Label lblConteo;
        private Label lblSeleccion;
        private Label lblAvisoHistorial;
        private TextBox txtBuscar;
        private ComboBox cmbCategoria;
        private ComboBox cmbEstado;
        private ComboBox cmbExistencias;
        private ComboBox cmbTipoHistorial;
        private NumericUpDown nudUmbral;
        private DateTimePicker dtDesde;
        private DateTimePicker dtHasta;
        private DataGridView dgvEquipos;
        private DataGridView dgvMovimientos;
        private TabControl tabPrincipal;
        private Button btnActualizar;
        private Button btnEntrada;
        private Button btnSalida;
        private Button btnEditar;
        private Button btnNuevo;
        private DataTable datosEquipos;
        private DataTable datosMovimientos;
        private bool conectado;
        private bool cargando;
        private bool ocupado;

        public InventarioControl()
        {
            Dock = DockStyle.Fill;
            BackColor = colorFondo;
            DoubleBuffered = true;
            Font = CrearFuente(9.5f);
            ConstruirInterfaz();
            Load += delegate { CargarDatosInventario(); };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (datosEquipos != null) datosEquipos.Dispose();
                if (datosMovimientos != null) datosMovimientos.Dispose();
                foreach (Font fuente in fuentes) fuente.Dispose();
            }
            base.Dispose(disposing);
        }

        private Font CrearFuente(float tamano, bool negrita = false)
        {
            Font fuente = new Font("Segoe UI", tamano, negrita ? FontStyle.Bold : FontStyle.Regular);
            fuentes.Add(fuente);
            return fuente;
        }

        private Label Etiqueta(string texto, float tamano, Color color, bool negrita = false)
        {
            return new Label
            {
                Text = texto,
                Font = CrearFuente(tamano, negrita),
                ForeColor = color,
                BackColor = Color.Transparent,
                AutoSize = true
            };
        }

        private Button Boton(string texto, bool principal, EventHandler accion, int ancho = 128)
        {
            Button boton = new Button
            {
                Text = texto,
                Width = ancho,
                Height = 38,
                BackColor = principal ? colorVerde : Color.White,
                ForeColor = principal ? Color.White : colorTexto,
                FlatStyle = FlatStyle.Flat,
                Font = CrearFuente(9, true),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 9, 0),
                UseVisualStyleBackColor = false
            };
            boton.FlatAppearance.BorderColor = principal ? colorVerde : colorLinea;
            boton.FlatAppearance.MouseOverBackColor = principal ? Color.FromArgb(14, 103, 75) : colorFondo;
            boton.Click += accion;
            return boton;
        }

        private ComboBox Combo(string primeraOpcion)
        {
            ComboBox combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = colorTexto,
                Font = CrearFuente(9),
                Dock = DockStyle.Fill
            };
            combo.Items.Add(primeraOpcion);
            combo.SelectedIndex = 0;
            return combo;
        }

        private void ConstruirInterfaz()
        {
            TableLayoutPanel principal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = colorFondo,
                Padding = new Padding(18),
                ColumnCount = 1,
                RowCount = 6
            };
            principal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            principal.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
            principal.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
            principal.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
            principal.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            principal.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            principal.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            Controls.Add(principal);

            principal.Controls.Add(ConstruirEncabezado(), 0, 0);
            principal.Controls.Add(ConstruirIndicadores(), 0, 1);
            principal.Controls.Add(ConstruirFiltros(), 0, 2);
            principal.Controls.Add(ConstruirAcciones(), 0, 3);
            principal.Controls.Add(ConstruirPestanas(), 0, 4);

            lblEstadoConexion = Etiqueta("Conectando con la base de datos…", 8.5f, colorSecundario);
            lblEstadoConexion.Dock = DockStyle.Fill;
            lblEstadoConexion.TextAlign = ContentAlignment.MiddleLeft;
            principal.Controls.Add(lblEstadoConexion, 0, 5);
        }
//Ajustar el panel de contro de equipo encabezado
        private Control ConstruirEncabezado()
        {
            Panel encabezado = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = colorTexto,
                Margin = new Padding(0, 0, 0, 12),
                MinimumSize = new Size(0, 110)
            };

            // Texto superior
            Label marca = Etiqueta(
                "ARTEMUSA TV  /  INVENTARIO AUDIOVISUAL",
                8f,
                Color.FromArgb(151, 216, 194),
                true
            );
            marca.Location = new Point(20, 10);

            // Título principal
            Label titulo = Etiqueta(
                "Control de equipos",
                22f,
                Color.White,
                true
            );
            titulo.Location = new Point(18, 32);

            // Subtítulo
            Label subtitulo = Etiqueta(
                "Gestión y seguimiento de equipos audiovisuales",
                8.5f,
                Color.FromArgb(183, 200, 210)
            );
            subtitulo.Location = new Point(21, 72);
            subtitulo.AutoSize = true;

            // Botón Actualizar
            btnActualizar = Boton(
                "↻  Actualizar",
                false,
                delegate { ActualizarVista(); },
                122
            );

            btnActualizar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnActualizar.Location = new Point(
                Math.Max(14, encabezado.Width - btnActualizar.Width - 14),
                28
            );

            // Mantener el botón alineado a la derecha
            encabezado.Resize += delegate
            {
                btnActualizar.Location = new Point(
                    Math.Max(14, encabezado.Width - btnActualizar.Width - 14),
                    28
                );
            };

            encabezado.Controls.Add(marca);
            encabezado.Controls.Add(titulo);
            encabezado.Controls.Add(subtitulo);
            encabezado.Controls.Add(btnActualizar);

            return encabezado;
        }

        private Control ConstruirIndicadores()
        {
            TableLayoutPanel fila = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0)
            };
            string[] titulos =
{
    "EQUIPOS REGISTRADOS",
    "DISPONIBLES",
    "ASIGNADOS",
    "EN MANTENIMIENTO"
};

            string[] detalles =
            {
    "Total de equipos registrados",
    "Equipos disponibles actualmente",
    "Equipos con asignación activa",
    "Equipos actualmente en mantenimiento"
};

            Color[] colores =
            {
    colorVerde,
    Color.FromArgb(43, 103, 163),
    Color.FromArgb(172, 102, 17),
    Color.FromArgb(119, 79, 178)
};

            Color[] fondos =
            {
    Color.FromArgb(229, 244, 237),
    Color.FromArgb(233, 242, 252),
    Color.FromArgb(255, 245, 226),
    Color.FromArgb(241, 236, 252)
};


            for (int i = 0; i < 4; i++)
            {
                fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
                Panel tarjeta = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = fondos[i],
                    Margin = new Padding(0, 0, i == 3 ? 0 : 10, 10),
                    Padding = new Padding(12)
                };
                Label titulo = Etiqueta(titulos[i], 7.3f, colores[i], true);
                titulo.Location = new Point(12, 9);
                indicadores[i] = Etiqueta("—", 23, colorTexto, true);
                indicadores[i].Location = new Point(10, 28);
                indicadores[i].AutoSize = false;
                indicadores[i].Size = new Size(190, 34);
                indicadores[i].AutoEllipsis = true;
                Label detalle = Etiqueta(detalles[i], 7.2f, colorSecundario);
                detalle.Location = new Point(13, 66);
                tarjeta.Controls.AddRange(new Control[] { titulo, indicadores[i], detalle });
                fila.Controls.Add(tarjeta, i, 0);
            }
            return fila;
        }

        private Control ConstruirFiltros()
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 9),
                Padding = new Padding(10)
            };
            TableLayoutPanel fila = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 2 };
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 158));
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135));
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
            fila.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            fila.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            string[] titulos =
{
    "BUSCAR EQUIPO",
    "CATEGORÍA",
    "ESTADO",
    "CONDICIÓN",
    "UBICACIÓN",
    ""
};

            for (int i = 0; i < titulos.Length; i++)
            {
                Label titulo = Etiqueta(
                    titulos[i],
                    7.2f,
                    colorSecundario,
                    true
                );

                titulo.Dock = DockStyle.Fill;
                fila.Controls.Add(titulo, i, 0);
            }

            // BUSCADOR
            txtBuscar = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = CrearFuente(9),
                Margin = new Padding(0, 8, 8, 0)
            };

            // CATEGORÍA
            cmbCategoria = Combo("Todas");

            // ESTADO
            cmbEstado = Combo("Todos");

            // CONDICIÓN
            cmbExistencias = Combo("Todas");
            cmbExistencias.Items.Clear();
            cmbExistencias.Items.AddRange(new object[]
            {
    "Todas",
    "Bueno",
    "Regular",
    "Malo"
            });
            cmbExistencias.SelectedIndex = 0;

            // UBICACIÓN
            nudUmbral = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 100,
                Visible = false
            };

            // BOTÓN LIMPIAR
            Button limpiar = Boton(
                "Limpiar",
                false,
                delegate
                {
                    txtBuscar.Clear();
                    cmbCategoria.SelectedIndex = 0;
                    cmbEstado.SelectedIndex = 0;
                    cmbExistencias.SelectedIndex = 0;
                    AplicarFiltros();
                },
                72
            );

            limpiar.Height = 29;

            fila.Controls.Add(txtBuscar, 0, 1);
            fila.Controls.Add(cmbCategoria, 1, 1);
            fila.Controls.Add(cmbEstado, 2, 1);
            fila.Controls.Add(cmbExistencias, 3, 1);
            fila.Controls.Add(nudUmbral, 4, 1);
            fila.Controls.Add(limpiar, 5, 1);
            panel.Controls.Add(fila);

            txtBuscar.TextChanged += delegate { AplicarFiltros(); };
            cmbCategoria.SelectedIndexChanged += delegate { AplicarFiltros(); };
            cmbEstado.SelectedIndexChanged += delegate { AplicarFiltros(); };
            cmbExistencias.SelectedIndexChanged += delegate { AplicarFiltros(); };
            nudUmbral.ValueChanged += delegate { ActualizarIndicadores(); AplicarFiltros(); };
            return panel;
        }

        private Control ConstruirAcciones()
        {
            FlowLayoutPanel barra = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0, 3, 0, 0)
            };
            btnNuevo = Boton("+  Nuevo equipo", true, delegate { EditarEquipo(false); }, 145);
            btnEntrada = Boton("↙  Entrada", true, delegate { RegistrarMovimiento("Entrada"); }, 120);
            btnSalida = Boton("↗  Salida", false, delegate { RegistrarMovimiento("Salida"); }, 120);
            btnEditar = Boton("Editar equipo", false, delegate { EditarEquipo(true); }, 125);
            Button exportar = Boton("Exportar CSV", false, delegate { ExportarCsv(); }, 122);
            lblConteo = Etiqueta("Sin registros", 8.5f, colorSecundario);
            lblConteo.Margin = new Padding(8, 10, 0, 0);
            barra.Controls.AddRange(new Control[] { btnNuevo, btnEntrada, btnSalida, btnEditar, exportar, lblConteo });
            botonesEscritura.AddRange(new[] { btnNuevo, btnEntrada, btnSalida, btnEditar });
            return barra;
        }

        private Control ConstruirPestanas()
        {
            tabPrincipal = new TabControl { Dock = DockStyle.Fill, Font = CrearFuente(9.5f, true), Padding = new Point(18, 8) };
            TabPage paginaEquipos = new TabPage("  Equipos  ") { BackColor = Color.White, Padding = new Padding(10) };
            TabPage paginaMovimientos = new TabPage("  Historial de movimientos  ") { BackColor = Color.White, Padding = new Padding(10) };
            dgvEquipos = CrearGrilla();
            dgvMovimientos = CrearGrilla();
            lblSeleccion = Etiqueta("Selecciona un equipo para registrar una entrada, salida o editarlo.", 8.5f, colorSecundario);
            lblSeleccion.AutoSize = false;
            lblSeleccion.Dock = DockStyle.Bottom;
            lblSeleccion.Height = 28;
            lblSeleccion.TextAlign = ContentAlignment.MiddleLeft;
            paginaEquipos.Controls.Add(dgvEquipos);
            paginaEquipos.Controls.Add(lblSeleccion);
            dgvEquipos.SelectionChanged += delegate { ActualizarSeleccion(); };
            dgvEquipos.CellDoubleClick += delegate (object s, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex >= 0 && conectado) EditarEquipo(true);
            };
            dgvEquipos.CellFormatting += FormatearEquipo;

            FlowLayoutPanel filtrosHistorial = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 3)
            };
            dtDesde = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 108, Value = DateTime.Today.AddDays(-30) };
            dtHasta = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 108, Value = DateTime.Today };
            cmbTipoHistorial = Combo("Todos los movimientos");
            cmbTipoHistorial.Items.AddRange(new object[] { "Entrada", "Salida", "Traslado", "Devolución", "Mantenimiento" });
            cmbTipoHistorial.Width = 175;
            Button consultar = Boton("Consultar", false, delegate { CargarHistorial(); }, 100);
            consultar.Height = 28;
            filtrosHistorial.Controls.Add(Etiqueta("Desde", 8.5f, colorSecundario));
            filtrosHistorial.Controls.Add(dtDesde);
            filtrosHistorial.Controls.Add(Etiqueta("Hasta", 8.5f, colorSecundario));
            filtrosHistorial.Controls.Add(dtHasta);
            filtrosHistorial.Controls.Add(cmbTipoHistorial);
            filtrosHistorial.Controls.Add(consultar);
            lblAvisoHistorial = Etiqueta("Consulta los movimientos registrados en la base de datos.", 8, colorSecundario);
            lblAvisoHistorial.Dock = DockStyle.Bottom;
            lblAvisoHistorial.Height = 26;
            lblAvisoHistorial.AutoEllipsis = true;
            paginaMovimientos.Controls.Add(dgvMovimientos);
            paginaMovimientos.Controls.Add(filtrosHistorial);
            paginaMovimientos.Controls.Add(lblAvisoHistorial);

            tabPrincipal.TabPages.Add(paginaEquipos);
            tabPrincipal.TabPages.Add(paginaMovimientos);
            tabPrincipal.SelectedIndexChanged += delegate
            {
                bool enEquipos = tabPrincipal.SelectedIndex == 0;
                cmbCategoria.Enabled = cmbEstado.Enabled = cmbExistencias.Enabled = nudUmbral.Enabled = enEquipos;
                ActualizarSeleccion();
                AplicarFiltros();
                if (!enEquipos && conectado) CargarHistorial();
            };
            cmbTipoHistorial.SelectedIndexChanged += delegate { AplicarFiltros(); };
            return tabPrincipal;
        }

        private DataGridView CrearGrilla()
        {
            DataGridView grilla = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = colorLinea,
                ColumnHeadersHeight = 40,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                Font = CrearFuente(8.5f)
            };
            grilla.RowTemplate.Height = 40;
            grilla.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(242, 246, 249),
                ForeColor = colorSecundario,
                Font = CrearFuente(8, true),
                Padding = new Padding(7, 0, 0, 0),
                SelectionBackColor = Color.FromArgb(242, 246, 249),
                SelectionForeColor = colorSecundario
            };
            grilla.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = colorTexto,
                Font = CrearFuente(8.5f),
                Padding = new Padding(7, 0, 4, 0),
                SelectionBackColor = Color.FromArgb(221, 242, 232),
                SelectionForeColor = colorTexto
            };
            grilla.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 253);
            return grilla;
        }

        private MySqlConnection AbrirConexion()
        {
            // Cada operación obtiene una instancia nueva: el llamador la libera con using.
            MySqlConnection conexion = new Conexion().ObtenerConexion();
            if (conexion == null) throw new InvalidOperationException("Conexion.ObtenerConexion() devolvió una conexión vacía.");
            try
            {
                if (conexion.State != ConnectionState.Open) conexion.Open();
                return conexion;
            }
            catch
            {
                conexion.Dispose();
                throw;
            }
        }

        public void CargarDatosInventario()
        {
            if (cargando || ocupado) return;
            cargando = true;
            try
            {
                DataTable nueva = new DataTable();
                using (MySqlConnection conexion = AbrirConexion())
                using (MySqlDataAdapter adaptador = new MySqlDataAdapter(
                    "SELECT id, codigo, nombre, categoria, marca, modelo, numero_serie, ubicacion, stock, estado, descripcion, id_ubicacion FROM equipos ORDER BY nombre", conexion))
                {
                    adaptador.Fill(nueva);
                }
                foreach (DataRow fila in nueva.Rows) StockValue(fila["stock"]);
                DataTable anterior = datosEquipos;
                datosEquipos = nueva;
                dgvEquipos.DataSource = nueva;
                if (anterior != null) anterior.Dispose();

                PoblarFiltro(cmbCategoria, "Todas", "categoria");
                PoblarFiltro(cmbEstado, "Todos", "estado");
                ConfigurarColumnasEquipos();
                EstablecerConexion(true);
                ActualizarIndicadores();
                lblEstadoConexion.Text = "●  Conectado a artemusa_inventario  ·  Actualizado " + DateTime.Now.ToString("HH:mm:ss");
                lblEstadoConexion.ForeColor = colorVerde;
            }
            catch (Exception ex)
            {
                dgvEquipos.DataSource = null;
                if (datosEquipos != null) { datosEquipos.Dispose(); datosEquipos = null; }
                if (datosMovimientos != null) { datosMovimientos.Dispose(); datosMovimientos = null; }
                dgvMovimientos.DataSource = null;
                EstablecerConexion(false);
                ActualizarIndicadores();
                lblEstadoConexion.Text = "Sin conexión o esquema incompatible. Pulsa Actualizar para reintentar.";
                lblEstadoConexion.ForeColor = Color.Firebrick;
                MostrarError("No se pudo cargar el inventario", ex);
            }
            finally
            {
                cargando = false;
                AplicarFiltros();
                ActualizarSeleccion();
            }
        }

        private void ConfigurarColumnasEquipos()
        {
            if (dgvEquipos.Columns.Count == 0) return;
            string[] nombres = { "codigo", "nombre", "categoria", "marca", "modelo", "ubicacion", "stock", "estado" };
            string[] titulos = { "Código", "Equipo", "Categoría", "Marca", "Modelo", "Ubicación", "Stock", "Estado" };
            for (int i = 0; i < nombres.Length; i++)
            {
                if (!dgvEquipos.Columns.Contains(nombres[i])) continue;
                DataGridViewColumn columna = dgvEquipos.Columns[nombres[i]];
                columna.HeaderText = titulos[i];
                columna.MinimumWidth = i == 1 ? 155 : 80;
                columna.Visible = true;
                columna.FillWeight = i == 1 ? 180 : (i == 5 ? 120 : 95);
            }
            foreach (string oculta in new[] { "id", "numero_serie", "descripcion", "id_ubicacion" })
                if (dgvEquipos.Columns.Contains(oculta)) dgvEquipos.Columns[oculta].Visible = false;
            if (dgvEquipos.Columns.Contains("stock")) dgvEquipos.Columns["stock"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        private void ConfigurarColumnasMovimientos()
        {
            if (dgvMovimientos.Columns.Count == 0) return;
            string[] nombres = { "fecha", "hora", "tipo", "codigo", "equipo", "responsable", "motivo", "origen", "destino", "observacion" };
            string[] titulos = { "Fecha", "Hora", "Movimiento", "Código", "Equipo", "Responsable", "Motivo", "Origen", "Destino", "Detalle de stock" };
            for (int i = 0; i < nombres.Length; i++)
            {
                if (!dgvMovimientos.Columns.Contains(nombres[i])) continue;
                dgvMovimientos.Columns[nombres[i]].HeaderText = titulos[i];
                dgvMovimientos.Columns[nombres[i]].MinimumWidth = i == 4 || i == 9 ? 145 : 90;
                dgvMovimientos.Columns[nombres[i]].Visible = true;
                dgvMovimientos.Columns[nombres[i]].FillWeight = i == 4 || i == 9 ? 150 : 90;
            }
            foreach (string oculta in new[] { "id_movimiento", "id_equipo", "responsable_id" })
                if (dgvMovimientos.Columns.Contains(oculta)) dgvMovimientos.Columns[oculta].Visible = false;
        }

        private void PoblarFiltro(ComboBox combo, string textoTodos, string columna)
        {
            if (combo == null || datosEquipos == null) return;
            string seleccionado = Convert.ToString(combo.SelectedItem);
            SortedSet<string> valores = new SortedSet<string>(StringComparer.CurrentCultureIgnoreCase);
            foreach (DataRow fila in datosEquipos.Rows)
                if (!fila.IsNull(columna)) valores.Add(Convert.ToString(fila[columna]));
            combo.Items.Clear();
            combo.Items.Add(textoTodos);
            foreach (string valor in valores) combo.Items.Add(valor);
            int indice = combo.Items.IndexOf(seleccionado);
            combo.SelectedIndex = indice >= 0 ? indice : 0;
        }

        private void EstablecerConexion(bool valor)
        {
            conectado = valor;
            foreach (Button boton in botonesEscritura) boton.Enabled = valor && !ocupado;
            if (btnActualizar != null) btnActualizar.Enabled = true;
            ActualizarSeleccion();
        }

        private void ActualizarIndicadores()
        {
            if (indicadores[0] == null) return;
            if (datosEquipos == null)
            {
                foreach (Label indicador in indicadores) indicador.Text = "—";
                return;
            }
            long unidades = 0;
            int alertas = 0;
            int disponibles = 0;
            foreach (DataRow fila in datosEquipos.Rows)
            {
                long stock = StockValue(fila["stock"]);
                unidades += stock;
                if (stock == 0 || stock <= nudUmbral.Value) alertas++;
                if (string.Equals(Convert.ToString(fila["estado"]), "Disponible", StringComparison.OrdinalIgnoreCase)) disponibles++;
            }
            indicadores[0].Text = datosEquipos.Rows.Count.ToString("N0");
            indicadores[1].Text = unidades.ToString("N0");
            indicadores[2].Text = alertas.ToString("N0");
            indicadores[3].Text = disponibles.ToString("N0");
        }

        private static long StockValue(object valor)
        {
            if (valor == null || valor == DBNull.Value) throw new InvalidOperationException("El stock está vacío en un equipo.");
            decimal numero = Convert.ToDecimal(valor, CultureInfo.InvariantCulture);
            if (numero < 0 || numero != decimal.Truncate(numero) || numero > int.MaxValue)
                throw new InvalidOperationException("El stock debe ser un número entero entre 0 e Int32.MaxValue.");
            return (long)numero;
        }

        private static string EscaparFiltro(string valor)
        {
            return (valor ?? "").Replace("'", "''").Replace("[", "[[]").Replace("]", "[]]").Replace("%", "[%]").Replace("*", "[*]");
        }

        private void AplicarFiltros()
        {
            if (cargando || tabPrincipal == null || lblConteo == null) return;
            bool equiposSeleccionados = tabPrincipal.SelectedIndex == 0;
            DataTable tabla = equiposSeleccionados ? datosEquipos : datosMovimientos;
            if (tabla == null) { lblConteo.Text = "Sin registros"; return; }

            List<string> condiciones = new List<string>();
            string busqueda = EscaparFiltro(txtBuscar.Text.Trim());
            if (busqueda.Length > 0)
            {
                string[] columnas = equiposSeleccionados
                    ? new[] { "codigo", "nombre", "categoria", "marca", "modelo", "ubicacion", "estado" }
                    : new[] { "tipo", "codigo", "equipo", "responsable", "motivo", "origen", "destino", "observacion" };
                List<string> expresiones = new List<string>();
                foreach (string columna in columnas)
                    if (tabla.Columns.Contains(columna))
                        expresiones.Add("CONVERT([" + columna + "], 'System.String') LIKE '%" + busqueda + "%'");
                if (expresiones.Count > 0) condiciones.Add("(" + string.Join(" OR ", expresiones) + ")");
            }

            if (equiposSeleccionados)
            {
                if (cmbCategoria.SelectedIndex > 0) condiciones.Add("[categoria] = '" + EscaparFiltro(Convert.ToString(cmbCategoria.SelectedItem)) + "'");
                if (cmbEstado.SelectedIndex > 0) condiciones.Add("[estado] = '" + EscaparFiltro(Convert.ToString(cmbEstado.SelectedItem)) + "'");
                if (cmbExistencias.SelectedIndex == 1) condiciones.Add("[stock] > 0");
                else if (cmbExistencias.SelectedIndex == 2) condiciones.Add("[stock] > 0 AND [stock] <= " + nudUmbral.Value.ToString(CultureInfo.InvariantCulture));
                else if (cmbExistencias.SelectedIndex == 3) condiciones.Add("[stock] = 0");
            }
            else if (cmbTipoHistorial.SelectedIndex > 0)
            {
                condiciones.Add("[tipo] = '" + EscaparFiltro(Convert.ToString(cmbTipoHistorial.SelectedItem)) + "'");
            }

            try { tabla.DefaultView.RowFilter = string.Join(" AND ", condiciones); }
            catch (EvaluateException ex)
            {
                tabla.DefaultView.RowFilter = "";
                lblEstadoConexion.Text = "No se pudo aplicar el filtro: " + ex.Message;
            }
            lblConteo.Text = tabla.DefaultView.Count.ToString("N0") + " registros visibles";
            if (dgvEquipos != null) dgvEquipos.Invalidate();
            if (dgvMovimientos != null) dgvMovimientos.Invalidate();
            ActualizarSeleccion();
        }

        private DataRow EquipoSeleccionado()
        {
            if (dgvEquipos == null || dgvEquipos.CurrentRow == null) return null;
            DataRowView vista = dgvEquipos.CurrentRow.DataBoundItem as DataRowView;
            return vista == null ? null : vista.Row;
        }

        private void ActualizarSeleccion()
        {
            if (dgvEquipos == null || lblSeleccion == null || tabPrincipal == null) return;
            DataRow fila = EquipoSeleccionado();
            bool habilitado = conectado && !ocupado && tabPrincipal.SelectedIndex == 0 && fila != null;
            btnEntrada.Enabled = btnSalida.Enabled = btnEditar.Enabled = habilitado;
            if (fila == null)
                lblSeleccion.Text = "Selecciona un equipo para registrar una entrada, salida o editarlo.";
            else
                lblSeleccion.Text = Convert.ToString(fila["codigo"]) + "  ·  " + Convert.ToString(fila["nombre"]) + "  ·  Stock: " + Convert.ToString(fila["stock"]);
        }

        private void FormatearEquipo(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string nombre = dgvEquipos.Columns[e.ColumnIndex].Name;
            if (nombre == "stock" && e.Value != null && e.Value != DBNull.Value)
            {
                long stock = StockValue(e.Value);
                e.CellStyle.ForeColor = stock == 0 ? Color.Firebrick : stock <= nudUmbral.Value ? Color.FromArgb(165, 101, 16) : colorVerde;
            }
            if (nombre == "estado") e.CellStyle.ForeColor = colorVerde;
        }

        private void CargarHistorial()
        {
            if (!conectado || ocupado) return;
            if (dtDesde.Value.Date > dtHasta.Value.Date)
            {
                Informar("La fecha inicial no puede ser posterior a la fecha final.");
                return;
            }
            try
            {
                DataTable nuevo = new DataTable();
                const string sql = @"
SELECT m.id_movimiento,
       DATE_FORMAT(m.fecha_movimiento, '%d/%m/%Y') AS fecha,
       TIME_FORMAT(m.hora_movimiento, '%H:%i') AS hora,
       CASE m.tipo_movimiento
         WHEN 'INGRESO' THEN 'Entrada'
         WHEN 'SALIDA' THEN 'Salida'
         WHEN 'TRASLADO' THEN 'Traslado'
         WHEN 'DEVOLUCION' THEN 'Devolución'
         WHEN 'MANTENIMIENTO' THEN 'Mantenimiento'
         ELSE m.tipo_movimiento
       END AS tipo,
       COALESCE(e.codigo, '') AS codigo,
       COALESCE(e.nombre, '') AS equipo,
       COALESCE(
           NULLIF(TRIM(CONCAT_WS(' ', r.nombre, r.apellido)), ''),
           NULLIF(TRIM(CONCAT_WS(' ', u.nombre, u.apellido)), ''),
           CONCAT('Responsable #', COALESCE(m.id_responsable, m.id_usuario, m.id_trabajador))
       ) AS responsable,
       COALESCE(m.motivo, '') AS motivo,
       COALESCE(m.ubicacion_origen, '') AS origen,
       COALESCE(m.ubicacion_destino, '') AS destino,
       COALESCE(SUBSTRING_INDEX(m.observacion, '; REFOP=', 1), '') AS observacion,
       COALESCE(m.id_responsable, m.id_usuario, m.id_trabajador) AS responsable_id,
       m.id_equipo
FROM movimiento m
LEFT JOIN equipos e ON e.id = m.id_equipo
LEFT JOIN responsable r ON r.id_responsable = m.id_responsable
LEFT JOIN usuario u ON u.id_usuario = COALESCE(m.id_usuario, m.id_trabajador)
WHERE m.fecha_movimiento >= @desde AND m.fecha_movimiento < @hasta
ORDER BY m.fecha_movimiento DESC, m.hora_movimiento DESC, m.id_movimiento DESC
LIMIT 1000";
                using (MySqlConnection conexion = AbrirConexion())
                using (MySqlCommand comando = new MySqlCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue("@desde", dtDesde.Value.Date);
                    comando.Parameters.AddWithValue("@hasta", dtHasta.Value.Date.AddDays(1));
                    using (MySqlDataAdapter adaptador = new MySqlDataAdapter(comando)) adaptador.Fill(nuevo);
                }
                DataTable anterior = datosMovimientos;
                datosMovimientos = nuevo;
                dgvMovimientos.DataSource = nuevo;
                if (anterior != null) anterior.Dispose();
                ConfigurarColumnasMovimientos();
                lblAvisoHistorial.Text = "Hasta 1.000 movimientos del período · La cantidad y el stock se muestran en el detalle.";
                AplicarFiltros();
            }
            catch (Exception ex)
            {
                dgvMovimientos.DataSource = null;
                if (datosMovimientos != null) { datosMovimientos.Dispose(); datosMovimientos = null; }
                lblAvisoHistorial.Text = "No se pudo consultar el historial. Verifica la tabla movimiento.";
                AplicarFiltros();
                MostrarError("Historial no disponible", ex);
            }
        }

        private void ActualizarVista()
        {
            CargarDatosInventario();
            if (conectado && tabPrincipal.SelectedIndex == 1) CargarHistorial();
        }

        private Form CrearDialogo(string titulo, Size tamano)
        {
            return new Form
            {
                Text = titulo,
                ClientSize = tamano,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White,
                Font = Font,
                ShowInTaskbar = false,
                AutoScaleMode = AutoScaleMode.Font
            };
        }

        private TextBox AgregarCampo(Form dialogo, string titulo, int y, string valor, int maximo, bool multilinea = false)
        {
            Label etiqueta = Etiqueta(titulo, 8.5f, colorSecundario, true);
            etiqueta.Location = new Point(22, y);
            dialogo.Controls.Add(etiqueta);
            TextBox campo = new TextBox
            {
                Location = new Point(22, y + 21),
                Size = new Size(dialogo.ClientSize.Width - 44, multilinea ? 54 : 26),
                Text = valor ?? "",
                MaxLength = maximo,
                Font = Font,
                Multiline = multilinea,
                ScrollBars = multilinea ? ScrollBars.Vertical : ScrollBars.None
            };
            dialogo.Controls.Add(campo);
            return campo;
        }

        private void EditarEquipo(bool editar)
        {
            if (!conectado || ocupado) return;
            DataRow fila = editar ? EquipoSeleccionado() : null;
            if (editar && fila == null) return;

            using (Form dialogo = CrearDialogo(editar ? "Editar equipo audiovisual" : "Registrar equipo audiovisual", new Size(560, 650)))
            {
                Label encabezado = Etiqueta(editar ? "Actualizar datos del equipo" : "Nuevo equipo de Artemusa TV", 16, colorTexto, true);
                encabezado.Location = new Point(22, 14);
                dialogo.Controls.Add(encabezado);
                TextBox codigo = AgregarCampo(dialogo, "Código del equipo *", 52, editar ? Convert.ToString(fila["codigo"]) : "", 50);
                TextBox nombre = AgregarCampo(dialogo, "Nombre del equipo *", 101, editar ? Convert.ToString(fila["nombre"]) : "", 120);
                TextBox categoria = AgregarCampo(dialogo, "Tipo / categoría *", 150, editar ? Convert.ToString(fila["categoria"]) : "", 100);
                TextBox marca = AgregarCampo(dialogo, "Marca", 199, editar ? Convert.ToString(fila["marca"]) : "", 80);
                TextBox modelo = AgregarCampo(dialogo, "Modelo", 248, editar ? Convert.ToString(fila["modelo"]) : "", 80);
                TextBox serie = AgregarCampo(dialogo, "Número de serie", 297, editar ? Convert.ToString(fila["numero_serie"]) : "", 100);
                // ubicacion.nombre_ubicacion admite hasta 120 caracteres en la base.
                TextBox ubicacion = AgregarCampo(dialogo, "Ubicación actual *", 346, editar ? Convert.ToString(fila["ubicacion"]) : "Almacén Principal", 120);
                TextBox estado = AgregarCampo(dialogo, "Estado del registro *", 395, editar ? Convert.ToString(fila["estado"]) : "Disponible", 50);
                TextBox descripcion = AgregarCampo(dialogo, "Descripción", 444, editar ? Convert.ToString(fila["descripcion"]) : "", 1000, true);

                Label aviso = Etiqueta("El stock se controla con los botones Entrada y Salida.", 8, colorSecundario);
                aviso.Location = new Point(24, 526);
                dialogo.Controls.Add(aviso);
                Button cancelar = Boton("Cancelar", false, delegate { dialogo.Close(); }, 105);
                cancelar.Location = new Point(318, 580);
                Button guardar = Boton("Guardar equipo", true, delegate { }, 175);
                guardar.Location = new Point(430, 580);
                guardar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                cancelar.Anchor = guardar.Anchor;
                dialogo.Controls.Add(cancelar);
                dialogo.Controls.Add(guardar);
                dialogo.CancelButton = cancelar;

                guardar.Click += delegate
                {
                    if (codigo.Text.Trim().Length == 0 || nombre.Text.Trim().Length == 0 || categoria.Text.Trim().Length == 0
                        || ubicacion.Text.Trim().Length == 0 || estado.Text.Trim().Length == 0)
                    {
                        Informar("Completa el código, nombre, categoría, ubicación y estado.");
                        return;
                    }
                    guardar.Enabled = cancelar.Enabled = false;
                    ocupado = true;
                    EstablecerConexion(conectado);
                    try
                    {
                        GuardarEquipo(editar, editar ? Convert.ToInt32(fila["id"]) : 0,
                            codigo.Text.Trim(), nombre.Text.Trim(), categoria.Text.Trim(), marca.Text.Trim(),
                            modelo.Text.Trim(), serie.Text.Trim(), ubicacion.Text.Trim(), estado.Text.Trim(), descripcion.Text.Trim());
                        dialogo.DialogResult = DialogResult.OK;
                        dialogo.Close();
                    }
                    catch (CodigoEquipoDuplicadoException ex)
                    {
                        Informar(ex.Message);
                        codigo.Focus();
                        codigo.SelectAll();
                    }
                    catch (MySqlException ex)
                    {
                        if (ex.Number == 1062)
                        {
                            Informar("Ese código de equipo ya está registrado. Si necesitas aumentar las existencias del mismo equipo, usa el botón Entrada; para otro equipo separado, elige un código distinto.");
                            codigo.Focus();
                            codigo.SelectAll();
                        }
                        else MostrarError("No se pudo guardar el equipo", ex);
                    }
                    catch (Exception ex)
                    {
                        MostrarError("No se pudo guardar el equipo", ex);
                    }
                    finally
                    {
                        ocupado = false;
                        guardar.Enabled = cancelar.Enabled = true;
                        EstablecerConexion(conectado);
                    }
                };
                if (dialogo.ShowDialog(FindForm()) == DialogResult.OK) CargarDatosInventario();
            }
        }

        private void GuardarEquipo(bool editar, int id, string codigo, string nombre, string categoria,
            string marca, string modelo, string serie, string ubicacion, string estado, string descripcion)
        {
            using (MySqlConnection conexion = AbrirConexion())
            using (MySqlTransaction transaccion = conexion.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    using (MySqlCommand validarCodigo = new MySqlCommand(
                        "SELECT COUNT(*) FROM equipo WHERE codigo_equipo=@codigo AND id_equipo<>@id", conexion, transaccion))
                    {
                        validarCodigo.Parameters.AddWithValue("@codigo", codigo);
                        validarCodigo.Parameters.AddWithValue("@id", editar ? id : 0);
                        if (Convert.ToInt32(validarCodigo.ExecuteScalar()) > 0)
                            throw new CodigoEquipoDuplicadoException(codigo);
                    }

                    int idTipo = ObtenerOCrearTipo(conexion, transaccion, categoria);
                    int idUbicacion = ObtenerOCrearUbicacion(conexion, transaccion, ubicacion);
                    const string actualizar = @"
UPDATE equipo
SET codigo_equipo=@codigo, nombre=@nombre, categoria=@categoria,
    marca=@marca, modelo=@modelo, numero_serie=@serie, ubicacion=@ubicacion,
    estado_registro=@estado, descripcion=@descripcion,
    id_tipo_equipo=@idTipo, id_ubicacion=@idUbicacion
WHERE id_equipo=@id";
                    const string insertar = @"
INSERT INTO equipo
(codigo_equipo, nombre, categoria, marca, modelo, numero_serie, ubicacion,
 estado_registro, estado_operativo, descripcion, id_tipo_equipo, id_ubicacion, stock)
VALUES
(@codigo, @nombre, @categoria, @marca, @modelo, @serie, @ubicacion,
 @estado, 'Operativo', @descripcion, @idTipo, @idUbicacion, 0)";
                    using (MySqlCommand comando = new MySqlCommand(editar ? actualizar : insertar, conexion, transaccion))
                    {
                        comando.Parameters.AddWithValue("@codigo", codigo);
                        comando.Parameters.AddWithValue("@nombre", nombre);
                        comando.Parameters.AddWithValue("@categoria", categoria);
                        comando.Parameters.AddWithValue("@marca", DbNullIfEmpty(marca));
                        comando.Parameters.AddWithValue("@modelo", DbNullIfEmpty(modelo));
                        comando.Parameters.AddWithValue("@serie", DbNullIfEmpty(serie));
                        comando.Parameters.AddWithValue("@ubicacion", ubicacion);
                        comando.Parameters.AddWithValue("@estado", estado);
                        comando.Parameters.AddWithValue("@descripcion", DbNullIfEmpty(descripcion));
                        comando.Parameters.AddWithValue("@idTipo", idTipo);
                        comando.Parameters.AddWithValue("@idUbicacion", idUbicacion);
                        if (editar) comando.Parameters.AddWithValue("@id", id);
                        int afectadas = comando.ExecuteNonQuery();
                        if (editar && afectadas == 0)
                        {
                            using (MySqlCommand existe = new MySqlCommand("SELECT COUNT(*) FROM equipo WHERE id_equipo=@id", conexion, transaccion))
                            {
                                existe.Parameters.AddWithValue("@id", id);
                                if (Convert.ToInt32(existe.ExecuteScalar()) == 0) throw new InvalidOperationException("El equipo ya no existe. Actualiza el catálogo.");
                            }
                        }
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

        private int ObtenerOCrearTipo(MySqlConnection conexion, MySqlTransaction transaccion, string nombre)
        {
            using (MySqlCommand insertar = new MySqlCommand(
                "INSERT IGNORE INTO tipo_equipo(nombre_tipo, estado) VALUES(@nombre, 'Activo')", conexion, transaccion))
            {
                insertar.Parameters.AddWithValue("@nombre", nombre);
                insertar.ExecuteNonQuery();
            }
            using (MySqlCommand consultar = new MySqlCommand(
                "SELECT id_tipo_equipo FROM tipo_equipo WHERE nombre_tipo=@nombre LIMIT 1", conexion, transaccion))
            {
                consultar.Parameters.AddWithValue("@nombre", nombre);
                object resultado = consultar.ExecuteScalar();
                if (resultado == null || resultado == DBNull.Value) throw new InvalidOperationException("No se pudo obtener el tipo de equipo.");
                return Convert.ToInt32(resultado);
            }
        }

        private int ObtenerOCrearUbicacion(MySqlConnection conexion, MySqlTransaction transaccion, string nombre)
        {
            using (MySqlCommand insertar = new MySqlCommand(
                "INSERT IGNORE INTO ubicacion(nombre_ubicacion, estado) VALUES(@nombre, 'Activo')", conexion, transaccion))
            {
                insertar.Parameters.AddWithValue("@nombre", nombre);
                insertar.ExecuteNonQuery();
            }
            using (MySqlCommand consultar = new MySqlCommand(
                "SELECT id_ubicacion FROM ubicacion WHERE nombre_ubicacion=@nombre LIMIT 1", conexion, transaccion))
            {
                consultar.Parameters.AddWithValue("@nombre", nombre);
                object resultado = consultar.ExecuteScalar();
                if (resultado == null || resultado == DBNull.Value) throw new InvalidOperationException("No se pudo obtener la ubicación del equipo.");
                return Convert.ToInt32(resultado);
            }
        }

        private static object DbNullIfEmpty(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? (object)DBNull.Value : valor.Trim();
        }

        private DataTable CargarTrabajadoresActivos()
        {
            DataTable trabajadores = new DataTable();
            const string sql = @"
SELECT id_responsable,
       CONCAT_WS(' ', NULLIF(nombre,''), NULLIF(apellido,'')) AS nombre_completo
FROM responsable
WHERE estado='Activo'
ORDER BY nombre, apellido";
            using (MySqlConnection conexion = AbrirConexion())
            using (MySqlDataAdapter adaptador = new MySqlDataAdapter(sql, conexion)) adaptador.Fill(trabajadores);
            return trabajadores;
        }

        private void RegistrarMovimiento(string tipo)
        {
            DataRow equipo = EquipoSeleccionado();
            if (!conectado || ocupado || equipo == null) return;

            DataTable trabajadores = null;
            try { trabajadores = CargarTrabajadoresActivos(); }
            catch (Exception ex) { MostrarError("No se pudo cargar la lista de trabajadores", ex); return; }
            if (trabajadores.Rows.Count == 0)
            {
                trabajadores.Dispose();
                Informar("No hay responsables activos. Registra o activa una persona en el panel Trabajadores.");
                return;
            }

            string operationId = Guid.NewGuid().ToString("N");
            using (Form dialogo = CrearDialogo(tipo + " de equipo", new Size(530, 500)))
            {
                Label titulo = Etiqueta("Registrar " + tipo.ToLowerInvariant(), 17, colorTexto, true);
                titulo.Location = new Point(22, 15);
                dialogo.Controls.Add(titulo);
                Label detalleEquipo = Etiqueta(Convert.ToString(equipo["codigo"]) + " · " + Convert.ToString(equipo["nombre"])
                    + " · Stock: " + Convert.ToString(equipo["stock"]), 8.5f, colorSecundario);
                detalleEquipo.Location = new Point(24, 48);
                detalleEquipo.AutoSize = false;
                detalleEquipo.Size = new Size(480, 28);
                detalleEquipo.AutoEllipsis = true;
                dialogo.Controls.Add(detalleEquipo);

                Label lblCantidad = Etiqueta("Cantidad de unidades *", 8.5f, colorSecundario, true);
                lblCantidad.Location = new Point(24, 82);
                NumericUpDown cantidad = new NumericUpDown
                {
                    Minimum = 1,
                    Maximum = int.MaxValue,
                    Value = 1,
                    ThousandsSeparator = true,
                    Location = new Point(24, 103),
                    Size = new Size(480, 27),
                    Font = Font
                };
                dialogo.Controls.Add(lblCantidad);
                dialogo.Controls.Add(cantidad);

                Label lblTrabajador = Etiqueta("Responsable del movimiento *", 8.5f, colorSecundario, true);
                lblTrabajador.Location = new Point(24, 140);
                ComboBox responsable = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Location = new Point(24, 161),
                    Size = new Size(480, 27),
                    Font = Font,
                    DataSource = trabajadores,
                    DisplayMember = "nombre_completo",
                    ValueMember = "id_responsable"
                };
                if (responsable.Items.Count > 0) responsable.SelectedIndex = 0;
                dialogo.Controls.Add(lblTrabajador);
                dialogo.Controls.Add(responsable);

                TextBox motivo = AgregarCampo(dialogo, "Motivo *", 198, "", 255);
                TextBox ubicacionExterna = AgregarCampo(dialogo,
                    tipo == "Entrada" ? "Origen / proveedor (opcional)" : "Destino / área (opcional)", 247, "", 150);
                Label nota = Etiqueta("El sistema verificará el stock y guardará el historial en una transacción.", 8, colorSecundario);
                nota.Location = new Point(24, 301);
                dialogo.Controls.Add(nota);

                Button cancelar = Boton("Cancelar", false, delegate { dialogo.Close(); }, 105);
                cancelar.Location = new Point(290, 425);
                Button guardar = Boton("Guardar " + tipo.ToLowerInvariant(), true, delegate { }, 185);
                guardar.Location = new Point(400, 425);
                cancelar.Anchor = guardar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                dialogo.Controls.Add(cancelar);
                dialogo.Controls.Add(guardar);
                dialogo.CancelButton = cancelar;

                bool guardado = false;
                guardar.Click += delegate
                {
                    if (string.IsNullOrWhiteSpace(motivo.Text) || responsable.SelectedValue == null)
                    {
                        Informar("Selecciona el responsable y escribe el motivo.");
                        return;
                    }
                    guardar.Enabled = cancelar.Enabled = false;
                    ocupado = true;
                    EstablecerConexion(conectado);
                    try
                    {
                        guardado = GuardarMovimiento(Convert.ToInt32(equipo["id"]), tipo, (int)cantidad.Value,
                            Convert.ToInt32(responsable.SelectedValue), motivo.Text.Trim(), ubicacionExterna.Text.Trim(), operationId);
                        if (!guardado) Informar("Este movimiento ya se había guardado. No se duplicó el descuento o ingreso de stock.");
                        dialogo.DialogResult = DialogResult.OK;
                        dialogo.Close();
                    }
                    catch (Exception ex)
                    {
                        MostrarError("No se pudo guardar el movimiento", ex);
                    }
                    finally
                    {
                        ocupado = false;
                        guardar.Enabled = cancelar.Enabled = true;
                        EstablecerConexion(conectado);
                    }
                };

                try
                {
                    if (dialogo.ShowDialog(FindForm()) == DialogResult.OK)
                    {
                        CargarDatosInventario();
                        if (tabPrincipal.SelectedIndex == 1) CargarHistorial();
                        if (guardado) Informar("Movimiento guardado. El stock y el historial quedaron actualizados.");
                    }
                }
                finally { trabajadores.Dispose(); }
            }
        }

        private bool GuardarMovimiento(int idEquipo, string tipo, int cantidad, int idResponsable,
            string motivo, string ubicacionExterna, string operationId)
        {
            if (cantidad <= 0 || (tipo != "Entrada" && tipo != "Salida")) throw new ArgumentException("El tipo o la cantidad del movimiento no son válidos.");
            using (MySqlConnection conexion = AbrirConexion())
            using (MySqlTransaction transaccion = conexion.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    using (MySqlCommand validarResponsable = new MySqlCommand(
                        "SELECT COUNT(*) FROM responsable WHERE id_responsable=@id AND estado='Activo'",
                        conexion, transaccion))
                    {
                        validarResponsable.Parameters.AddWithValue("@id", idResponsable);
                        if (Convert.ToInt32(validarResponsable.ExecuteScalar()) != 1)
                            throw new InvalidOperationException(
                                "El responsable seleccionado ya no está activo. Actualiza la lista e inténtalo de nuevo.");
                    }

                    string marcador = "%REFOP=" + operationId + "%";
                    using (MySqlCommand buscar = new MySqlCommand(
                        "SELECT id_movimiento FROM movimiento WHERE observacion LIKE @marcador LIMIT 1", conexion, transaccion))
                    {
                        buscar.Parameters.AddWithValue("@marcador", marcador);
                        if (buscar.ExecuteScalar() != null)
                        {
                            transaccion.Rollback();
                            return false;
                        }
                    }

                    long stockAnterior;
                    int? idUbicacionActual;
                    string ubicacionActual;
                    using (MySqlCommand bloquear = new MySqlCommand(
                        "SELECT stock, id_ubicacion, ubicacion FROM equipo WHERE id_equipo=@id FOR UPDATE", conexion, transaccion))
                    {
                        bloquear.Parameters.AddWithValue("@id", idEquipo);
                        using (MySqlDataReader lector = bloquear.ExecuteReader())
                        {
                            if (!lector.Read()) throw new InvalidOperationException("El equipo ya no existe. Actualiza el inventario.");
                            stockAnterior = StockValue(lector["stock"]);
                            idUbicacionActual = lector["id_ubicacion"] == DBNull.Value ? (int?)null : Convert.ToInt32(lector["id_ubicacion"]);
                            ubicacionActual = lector["ubicacion"] == DBNull.Value ? "" : Convert.ToString(lector["ubicacion"]);
                        }
                    }
                    if (tipo == "Salida" && cantidad > stockAnterior)
                        throw new InvalidOperationException("Stock insuficiente. Disponible actualmente: " + stockAnterior + ".");

                    long stockNuevo = tipo == "Entrada" ? stockAnterior + cantidad : stockAnterior - cantidad;
                    if (stockNuevo > int.MaxValue) throw new InvalidOperationException("La cantidad supera el máximo de stock admitido.");
                    using (MySqlCommand actualizar = new MySqlCommand(
                        "UPDATE equipo SET stock=@stock WHERE id_equipo=@id", conexion, transaccion))
                    {
                        actualizar.Parameters.AddWithValue("@stock", stockNuevo);
                        actualizar.Parameters.AddWithValue("@id", idEquipo);
                        if (actualizar.ExecuteNonQuery() != 1) throw new InvalidOperationException("No se pudo actualizar el stock del equipo.");
                    }

                    string tipoSql = tipo == "Entrada" ? "INGRESO" : "SALIDA";
                    string origen = tipo == "Entrada" ? DbNullIfEmpty(ubicacionExterna) as string : ubicacionActual;
                    string destino = tipo == "Entrada" ? ubicacionActual : DbNullIfEmpty(ubicacionExterna) as string;
                    object idOrigen = tipo == "Entrada" ? (object)DBNull.Value : (idUbicacionActual.HasValue ? (object)idUbicacionActual.Value : DBNull.Value);
                    object idDestino = tipo == "Entrada" ? (idUbicacionActual.HasValue ? (object)idUbicacionActual.Value : DBNull.Value) : (object)DBNull.Value;
                    string observacion = "Cantidad: " + cantidad.ToString(CultureInfo.InvariantCulture)
                        + "; stock anterior: " + stockAnterior.ToString(CultureInfo.InvariantCulture)
                        + "; stock nuevo: " + stockNuevo.ToString(CultureInfo.InvariantCulture)
                        + "; REFOP=" + operationId;

                    const string insertar = @"
INSERT INTO movimiento
(tipo_movimiento, cantidad, fecha_movimiento, hora_movimiento, motivo, observacion,
 id_equipo, id_responsable, id_usuario, id_trabajador,
 id_ubicacion_origen, id_ubicacion_destino,
 ubicacion_origen, ubicacion_destino)
VALUES
(@tipo, @cantidad, CURDATE(), CURTIME(), @motivo, @observacion,
 @idEquipo, @idResponsable, NULL, NULL, @idOrigen, @idDestino,
 @origen, @destino)";
                    using (MySqlCommand comando = new MySqlCommand(insertar, conexion, transaccion))
                    {
                        comando.Parameters.AddWithValue("@tipo", tipoSql);
                        comando.Parameters.AddWithValue("@cantidad", cantidad);
                        comando.Parameters.AddWithValue("@motivo", motivo);
                        comando.Parameters.AddWithValue("@observacion", observacion);
                        comando.Parameters.AddWithValue("@idEquipo", idEquipo);
                        comando.Parameters.AddWithValue("@idResponsable", idResponsable);
                        comando.Parameters.AddWithValue("@idOrigen", idOrigen);
                        comando.Parameters.AddWithValue("@idDestino", idDestino);
                        comando.Parameters.AddWithValue("@origen", origen == null ? (object)DBNull.Value : origen);
                        comando.Parameters.AddWithValue("@destino", destino == null ? (object)DBNull.Value : destino);
                        comando.ExecuteNonQuery();
                    }
                    transaccion.Commit();
                    return true;
                }
                catch
                {
                    try { transaccion.Rollback(); } catch { }
                    throw;
                }
            }
        }

        private void ExportarCsv()
        {
            DataTable tabla = tabPrincipal.SelectedIndex == 0 ? datosEquipos : datosMovimientos;
            if (tabla == null || tabla.DefaultView.Count == 0)
            {
                Informar("No hay registros visibles para exportar.");
                return;
            }
            using (SaveFileDialog dialogo = new SaveFileDialog
            {
                Filter = "Archivo CSV (*.csv)|*.csv",
                FileName = (tabPrincipal.SelectedIndex == 0 ? "inventario_" : "movimientos_") + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv"
            })
            {
                if (dialogo.ShowDialog(FindForm()) != DialogResult.OK) return;
                try
                {
                    using (StreamWriter escritor = new StreamWriter(dialogo.FileName, false, new UTF8Encoding(true)))
                    {
                        List<DataColumn> columnas = new List<DataColumn>();
                        foreach (DataColumn columna in tabla.Columns) columnas.Add(columna);
                        List<string> encabezados = new List<string>();
                        foreach (DataColumn columna in columnas) encabezados.Add(CeldaCsv(columna.ColumnName));
                        escritor.WriteLine(string.Join(";", encabezados));
                        foreach (DataRowView fila in tabla.DefaultView)
                        {
                            List<string> celdas = new List<string>();
                            foreach (DataColumn columna in columnas)
                                celdas.Add(CeldaCsv(Convert.ToString(fila[columna.ColumnName], CultureInfo.CurrentCulture)));
                            escritor.WriteLine(string.Join(";", celdas));
                        }
                    }
                    Informar("Archivo CSV exportado.");
                }
                catch (Exception ex) { MostrarError("No se pudo exportar el CSV", ex); }
            }
        }

        private static string CeldaCsv(string valor)
        {
            valor = valor ?? "";
            string inicio = valor.TrimStart();
            if (inicio.Length > 0 && "=+-@".IndexOf(inicio[0]) >= 0) valor = "'" + valor;
            return "\"" + valor.Replace("\"", "\"\"") + "\"";
        }

        private void Informar(string texto)
        {
            MessageBox.Show(FindForm(), texto, "Artemusa Inventario", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void MostrarError(string titulo, Exception excepcion)
        {
            MessageBox.Show(FindForm(), titulo + "\n\n" + excepcion.Message,
                "Artemusa Inventario", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
