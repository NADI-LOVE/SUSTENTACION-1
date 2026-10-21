using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Npgsql;
using NpgsqlTypes;
using SUSTENTACION.PanelTrabajador;

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

        private Control ConstruirEncabezado()
        {
            Panel encabezado = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = colorTexto,
                Margin = new Padding(0, 0, 0, 12),
                MinimumSize = new Size(0, 110)
            };

            Label marca = Etiqueta(
                "ARTEMUSA TV  /  INVENTARIO AUDIOVISUAL",
                8f,
                Color.FromArgb(151, 216, 194),
                true
            );
            marca.Location = new Point(20, 10);

            Label titulo = Etiqueta(
                "Control de equipos",
                22f,
                Color.White,
                true
            );
            titulo.Location = new Point(18, 32);

            Label subtitulo = Etiqueta(
                "Gestión y seguimiento de equipos audiovisuales",
                8.5f,
                Color.FromArgb(183, 200, 210)
            );
            subtitulo.Location = new Point(21, 72);
            subtitulo.AutoSize = true;

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
                "STOCK",
                "",
                ""
            };

            for (int i = 0; i < titulos.Length; i++)
            {
                Label titulo = Etiqueta(titulos[i], 7.2f, colorSecundario, true);
                titulo.Dock = DockStyle.Fill;
                fila.Controls.Add(titulo, i, 0);
            }

            txtBuscar = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = CrearFuente(9),
                Margin = new Padding(0, 8, 8, 0)
            };

            cmbCategoria = Combo("Todas");
            cmbEstado = Combo("Todos");

            cmbExistencias = Combo("Todas");
            cmbExistencias.Items.Clear();
            cmbExistencias.Items.AddRange(new object[]
            {
                "Todas",
                "Con existencias",
                "Sin existencias"
            });
            cmbExistencias.SelectedIndex = 0;

            nudUmbral = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 100,
                Visible = false
            };

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

        private NpgsqlConnection AbrirConexion()
        {
            NpgsqlConnection conexion = new Conexion().ObtenerConexion();
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
                var nueva = new DataTable();
                using (var conexion = AbrirConexion())
                using (var adaptador = new NpgsqlDataAdapter(@"
SELECT id, codigo, nombre, COALESCE(categoria,'General') AS categoria,
       COALESCE(marca,'') AS marca, COALESCE(modelo,'') AS modelo,
       ''::text AS numero_serie,
       COALESCE(ubicacion,'') AS ubicacion, COALESCE(stock,0) AS stock,
       COALESCE(estado,'Disponible') AS estado, COALESCE(descripcion,'') AS descripcion,
       NULL::integer AS id_ubicacion
FROM public.equipos ORDER BY nombre", conexion)) adaptador.Fill(nueva);
                var anterior = datosEquipos;
                datosEquipos = nueva;
                dgvEquipos.DataSource = nueva;
                if (anterior != null) anterior.Dispose();
                PoblarFiltro(cmbCategoria, "Todas", "categoria");
                PoblarFiltro(cmbEstado, "Todos", "estado");
                ConfigurarColumnasEquipos();
                EstablecerConexion(true);
                ActualizarIndicadores();
                lblEstadoConexion.Text = "●  Conectado a Supabase · Actualizado " + DateTime.Now.ToString("HH:mm:ss");
                lblEstadoConexion.ForeColor = colorVerde;
            }
            catch (Exception ex)
            {
                dgvEquipos.DataSource = null;
                if (datosEquipos != null) { datosEquipos.Dispose(); datosEquipos = null; }
                EstablecerConexion(false);
                ActualizarIndicadores();
                lblEstadoConexion.Text = "Error de conexión o consulta: " + ex.Message;
                lblEstadoConexion.ForeColor = Color.Firebrick;
                MostrarError("No se pudo cargar el inventario", ex);
            }
            finally { cargando = false; AplicarFiltros(); ActualizarSeleccion(); }
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
            if (datosEquipos == null) { foreach (var x in indicadores) x.Text = "—"; return; }
            int disponibles = 0, mantenimiento = 0, asignados = 0;
            foreach (DataRow fila in datosEquipos.Rows)
            {
                string estado = Convert.ToString(fila["estado"]).Trim();
                if (estado.Equals("Disponible", StringComparison.OrdinalIgnoreCase) && StockValue(fila["stock"]) > 0) disponibles++;
                if (estado.IndexOf("mantenimiento", StringComparison.OrdinalIgnoreCase) >= 0) mantenimiento++;
                if (estado.Equals("Asignado", StringComparison.OrdinalIgnoreCase) || estado.Equals("En uso", StringComparison.OrdinalIgnoreCase)) asignados++;
            }
            indicadores[0].Text = datosEquipos.Rows.Count.ToString("N0");
            indicadores[1].Text = disponibles.ToString("N0");
            indicadores[2].Text = asignados.ToString("N0");
            indicadores[3].Text = mantenimiento.ToString("N0");
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
                else if (cmbExistencias.SelectedIndex == 2) condiciones.Add("[stock] = 0");
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
            if (dtDesde.Value.Date > dtHasta.Value.Date) { Informar("La fecha inicial no puede ser posterior a la final."); return; }
            try
            {
                var nuevo = new DataTable();
                const string sql = @"
SELECT x.id_movimiento, to_char(x.fecha_evento,'DD/MM/YYYY') AS fecha,
       to_char(x.fecha_evento,'HH24:MI') AS hora, x.tipo,
       x.codigo, x.equipo, x.responsable, x.motivo, x.origen, x.destino,
       x.observacion, x.responsable_id, x.id_equipo
FROM (
 SELECT se.id AS id_movimiento, s.fecha_salida AS fecha_evento, 'Salida'::text AS tipo,
        e.codigo, e.nombre AS equipo, COALESCE(t.nombre,'') AS responsable,
        COALESCE(s.detalles,'') AS motivo, COALESCE(e.ubicacion,'') AS origen,
        COALESCE(s.ubicacion,'') AS destino, COALESCE(se.codigo_escaneado,'') AS observacion,
        s.id_trabajador AS responsable_id, e.id AS id_equipo
 FROM public.salida_equipos se JOIN public.salidas s ON s.id_salida=se.id_salida
 JOIN public.equipos e ON e.id=se.id_equipo
 LEFT JOIN public.trabajadores t ON t.id_trabajador=s.id_trabajador
 UNION ALL
 SELECT -r.id_retorno AS id_movimiento, r.fecha_retorno AS fecha_evento,
        'Devolución'::text AS tipo, COALESCE(e.codigo,''), COALESCE(e.nombre,''),
        COALESCE(t.nombre,''), COALESCE(r.estado_equipo,''),
        ''::text, COALESCE(e.ubicacion,''), COALESCE(r.observaciones,''),
        r.id_trabajador, r.id_equipo
 FROM public.retornos r LEFT JOIN public.equipos e ON e.id=r.id_equipo
 LEFT JOIN public.trabajadores t ON t.id_trabajador=r.id_trabajador
) x WHERE x.fecha_evento >= @desde AND x.fecha_evento < @hasta
ORDER BY x.fecha_evento DESC LIMIT 1000";
                using (var conexion = AbrirConexion())
                using (var comando = new NpgsqlCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue("@desde", dtDesde.Value.Date);
                    comando.Parameters.AddWithValue("@hasta", dtHasta.Value.Date.AddDays(1));
                    using (var adaptador = new NpgsqlDataAdapter(comando)) adaptador.Fill(nuevo);
                }
                var anterior = datosMovimientos;
                datosMovimientos = nuevo;
                dgvMovimientos.DataSource = nuevo;
                if (anterior != null) anterior.Dispose();
                ConfigurarColumnasMovimientos();
                lblAvisoHistorial.Text = "Historial de salidas y devoluciones existentes en Supabase.";
                AplicarFiltros();
            }
            catch (Exception ex) { MostrarError("No se pudo cargar el historial", ex); }
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

        // ------------------------------------------------------------------
        //  EDICIÓN / ALTA DE EQUIPO
        // ------------------------------------------------------------------
        private void EditarEquipo(bool editar)
        {
            if (!conectado || ocupado) return;
            DataRow fila = editar ? EquipoSeleccionado() : null;
            if (editar && fila == null) return;

            using (var dialogo = CrearDialogo(editar ? "Editar equipo" : "Nuevo equipo", new Size(530, 690)))
            {
                var titulo = Etiqueta(editar ? "Editar equipo audiovisual" : "Registrar equipo audiovisual", 16, colorTexto, true);
                titulo.Location = new Point(22, 12); dialogo.Controls.Add(titulo);

                // ---- Código (registro manual) ----
                Label lblCodigo = Etiqueta("Código *", 8.5f, colorSecundario, true);
                lblCodigo.Location = new Point(22, 55);
                dialogo.Controls.Add(lblCodigo);

                TextBox codigo = new TextBox
                {
                    Location = new Point(22, 76),
                    Size = new Size(486, 26),
                    Text = editar ? Convert.ToString(fila["codigo"]) : "",
                    MaxLength = 80,
                    Font = Font
                };
                dialogo.Controls.Add(codigo);

                // ---- Nombre ----
                var nombre = AgregarCampo(dialogo, "Nombre *", 105, editar ? Convert.ToString(fila["nombre"]) : "", 120);

                // ---- Categoría + Marca ----
                Label lblCategoria = Etiqueta("Categoría", 8.5f, colorSecundario, true);
                lblCategoria.Location = new Point(22, 155);
                dialogo.Controls.Add(lblCategoria);

                TextBox categoria = new TextBox
                {
                    Location = new Point(22, 176),
                    Size = new Size(230, 26),
                    Font = Font,
                    Text = editar ? Convert.ToString(fila["categoria"]) : "General",
                    MaxLength = 100
                };
                dialogo.Controls.Add(categoria);

                Label lblMarca = Etiqueta("Marca", 8.5f, colorSecundario, true);
                lblMarca.Location = new Point(268, 155);
                dialogo.Controls.Add(lblMarca);

                TextBox marca = new TextBox
                {
                    Location = new Point(268, 176),
                    Size = new Size(240, 26),
                    Font = Font,
                    Text = editar ? Convert.ToString(fila["marca"]) : "",
                    MaxLength = 100
                };
                dialogo.Controls.Add(marca);

                // ---- Modelo + Ubicación ----
                Label lblModelo = Etiqueta("Modelo", 8.5f, colorSecundario, true);
                lblModelo.Location = new Point(22, 212);
                dialogo.Controls.Add(lblModelo);

                TextBox modelo = new TextBox
                {
                    Location = new Point(22, 233),
                    Size = new Size(230, 26),
                    Font = Font,
                    Text = editar ? Convert.ToString(fila["modelo"]) : "",
                    MaxLength = 100
                };
                dialogo.Controls.Add(modelo);

                Label lblUbicacion = Etiqueta("Ubicación", 8.5f, colorSecundario, true);
                lblUbicacion.Location = new Point(268, 212);
                dialogo.Controls.Add(lblUbicacion);

                TextBox ubicacion = new TextBox
                {
                    Location = new Point(268, 233),
                    Size = new Size(240, 26),
                    Font = Font,
                    Text = editar ? Convert.ToString(fila["ubicacion"]) : "Almacén Principal",
                    MaxLength = 150
                };
                dialogo.Controls.Add(ubicacion);

                // ---- Estado ----
                var estado = AgregarCampo(dialogo, "Estado", 269, editar ? Convert.ToString(fila["estado"]) : "Disponible", 60);

                // ---- Stock ----
                Label lblStock = Etiqueta("Stock (unidades) *", 8.5f, colorSecundario, true);
                lblStock.Location = new Point(22, 319);
                dialogo.Controls.Add(lblStock);

                NumericUpDown stock = new NumericUpDown
                {
                    Location = new Point(22, 340),
                    Size = new Size(180, 27),
                    Minimum = 0,
                    Maximum = int.MaxValue,
                    ThousandsSeparator = true,
                    Font = Font,
                    TextAlign = HorizontalAlignment.Right
                };
                try
                {
                    stock.Value = editar && fila["stock"] != DBNull.Value
                        ? Convert.ToDecimal(fila["stock"], CultureInfo.InvariantCulture)
                        : 0m;
                }
                catch { stock.Value = 0m; }
                dialogo.Controls.Add(stock);

                Label notaStock = Etiqueta("Sugerencia: si el equipo ya existía usa Entrada para aumentar stock.", 7.8f, colorSecundario);
                notaStock.Location = new Point(210, 344);
                dialogo.Controls.Add(notaStock);

                // ---- Descripción ----
                var descripcion = AgregarCampo(dialogo, "Descripción", 385, editar ? Convert.ToString(fila["descripcion"]) : "", 1000, true);

                // ---- Botones ----
                var cancelar = Boton("Cancelar", false, (a, b) => dialogo.Close(), 105);
                cancelar.Location = new Point(210, 625); dialogo.Controls.Add(cancelar);
                var guardar = Boton("Guardar", true, (a, b) => { }, 145);
                guardar.Location = new Point(330, 625); dialogo.Controls.Add(guardar);
                dialogo.CancelButton = cancelar;

                // ---- GUARDAR ----
                guardar.Click += delegate
                {
                    if (string.IsNullOrWhiteSpace(codigo.Text) || string.IsNullOrWhiteSpace(nombre.Text))
                    {
                        Informar("Código y nombre son obligatorios.");
                        return;
                    }

                    guardar.Enabled = cancelar.Enabled = false;
                    ocupado = true;
                    EstablecerConexion(conectado);
                    try
                    {
                        GuardarEquipo(
                            editar,
                            editar ? Convert.ToInt32(fila["id"]) : 0,
                            codigo.Text,
                            nombre.Text,
                            categoria.Text,
                            marca.Text,
                            modelo.Text,
                            "",
                            ubicacion.Text,
                            estado.Text.Trim(),
                            descripcion.Text.Trim(),
                            (int)stock.Value);

                        dialogo.DialogResult = DialogResult.OK;
                        dialogo.Close();
                    }
                    catch (CodigoEquipoDuplicadoException ex)
                    {
                        Informar(ex.Message);
                        codigo.Focus(); codigo.SelectAll();
                    }
                    catch (PostgresException ex) when (ex.SqlState == "23505")
                    {
                        Informar("El código '" + codigo.Text.Trim().ToUpperInvariant() +
                                  "' ya está registrado. Usa un código distinto o el botón Entrada para aumentar stock.");
                        codigo.Focus(); codigo.SelectAll();
                    }
                    catch (Exception ex)
                    {
                        MostrarError("No se pudo guardar", ex);
                    }
                    finally
                    {
                        ocupado = false;
                        guardar.Enabled = cancelar.Enabled = true;
                        EstablecerConexion(conectado);
                    }
                };

                if (dialogo.ShowDialog(FindForm()) == DialogResult.OK)
                    CargarDatosInventario();
            }
        }

        // ------------------------------------------------------------------
        //  GUARDAR EQUIPO (con verificación de duplicados)
        // ------------------------------------------------------------------
        private void GuardarEquipo(bool editar, int id, string codigo, string nombre, string categoria,
            string marca, string modelo, string serie, string ubicacion, string estado, string descripcion,
            int stock)
        {
            codigo = (codigo ?? "").Trim().ToUpperInvariant();
            nombre = (nombre ?? "").Trim();

            if (codigo.Length == 0)
                throw new InvalidOperationException("El código no puede estar vacío.");
            if (nombre.Length == 0)
                throw new InvalidOperationException("El nombre no puede estar vacío.");

            using (var conexion = AbrirConexion())
            using (var transaccion = conexion.BeginTransaction())
            {
                const string sqlExiste = @"
SELECT 1 FROM public.equipos
WHERE UPPER(TRIM(codigo)) = @codigo
  AND (@id = 0 OR id <> @id)
LIMIT 1";

                using (var cmdExiste = new NpgsqlCommand(sqlExiste, conexion, transaccion))
                {
                    cmdExiste.Parameters.AddWithValue("@codigo", codigo);
                    cmdExiste.Parameters.AddWithValue("@id", editar ? id : 0);
                    if (cmdExiste.ExecuteScalar() != null)
                        throw new CodigoEquipoDuplicadoException(codigo);
                }

                if (!editar)
                {
                    using (var bloqueo = new NpgsqlCommand(
                        "SELECT pg_advisory_xact_lock(192837465)", conexion, transaccion))
                        bloqueo.ExecuteNonQuery();
                }

                string sql = editar
                    ? @"UPDATE public.equipos
                           SET codigo=@codigo, nombre=@nombre, categoria=@categoria,
                               marca=@marca, modelo=@modelo,
                               ubicacion=@ubicacion, estado=@estado,
                               descripcion=@descripcion, stock=@stock
                         WHERE id=@id"
                    : @"INSERT INTO public.equipos
                           (id, codigo, nombre, categoria, marca, modelo,
                            ubicacion, stock, estado, descripcion)
                        VALUES ((SELECT COALESCE(MAX(id),0)+1 FROM public.equipos),
                                @codigo, @nombre, @categoria, @marca, @modelo,
                                @ubicacion, @stock, @estado, @descripcion)";

                using (var cmd = new NpgsqlCommand(sql, conexion, transaccion))
                {
                    cmd.Parameters.AddWithValue("@codigo", codigo);
                    cmd.Parameters.AddWithValue("@nombre", nombre);
                    cmd.Parameters.AddWithValue("@categoria", categoria ?? "");
                    cmd.Parameters.AddWithValue("@marca", DbNullIfEmpty(marca));
                    cmd.Parameters.AddWithValue("@modelo", DbNullIfEmpty(modelo));
                    cmd.Parameters.AddWithValue("@ubicacion", ubicacion ?? "");
                    cmd.Parameters.AddWithValue("@estado", estado ?? "Disponible");
                    cmd.Parameters.AddWithValue("@descripcion", DbNullIfEmpty(descripcion));
                    cmd.Parameters.AddWithValue("@stock", stock);
                    if (editar) cmd.Parameters.AddWithValue("@id", id);

                    if (cmd.ExecuteNonQuery() != 1)
                        throw new InvalidOperationException("No se guardó el equipo.");
                }

                transaccion.Commit();
            }
        }

        private static object DbNullIfEmpty(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? (object)DBNull.Value : valor.Trim();
        }

        // ------------------------------------------------------------------
        //  MOVIMIENTOS (Entrada / Salida)
        // ------------------------------------------------------------------
        private DataTable CargarTrabajadoresActivos()
        {
            var tabla = new DataTable();
            using (var conexion = AbrirConexion())
            using (var adaptador = new NpgsqlDataAdapter(@"SELECT id_trabajador AS id_responsable,
 nombre AS nombre_completo FROM public.trabajadores WHERE activo=true ORDER BY nombre", conexion))
                adaptador.Fill(tabla);
            return tabla;
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
                        guardado = GuardarMovimiento(
                            Convert.ToInt32(equipo["id"]),
                            tipo,
                            (int)cantidad.Value,
                            Convert.ToInt32(responsable.SelectedValue),
                            motivo.Text.Trim(),
                            ubicacionExterna.Text.Trim(),
                            operationId);

                        if (!guardado)
                            Informar("Este movimiento ya se había guardado. No se duplicó el descuento o ingreso de stock.");

                        dialogo.DialogResult = DialogResult.OK;
                        dialogo.Close();
                    }
                    catch (InvalidOperationException ex)
                    {
                        Informar(ex.Message);
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
            if (cantidad <= 0)
                throw new InvalidOperationException("La cantidad debe ser mayor que cero.");
            if (string.IsNullOrWhiteSpace(motivo))
                throw new InvalidOperationException("Debes escribir el motivo del movimiento.");
            if (string.IsNullOrWhiteSpace(operationId))
                operationId = Guid.NewGuid().ToString("N");

            bool esEntrada = string.Equals(tipo, "Entrada", StringComparison.OrdinalIgnoreCase);

            using (var conexion = AbrirConexion())
            using (var transaccion = conexion.BeginTransaction())
            {
                const string sqlYaExiste = @"
SELECT 1 FROM public.salida_equipos
WHERE codigo_escaneado = @op
LIMIT 1";
                using (var cmd = new NpgsqlCommand(sqlYaExiste, conexion, transaccion))
                {
                    cmd.Parameters.AddWithValue("@op", operationId);
                    object ya = cmd.ExecuteScalar();
                    if (ya != null)
                    {
                        transaccion.Rollback();
                        return false;
                    }
                }

                int stockActual;
                const string sqlStock = @"
SELECT COALESCE(stock, 0)
FROM public.equipos
WHERE id = @id
FOR UPDATE";
                using (var cmd = new NpgsqlCommand(sqlStock, conexion, transaccion))
                {
                    cmd.Parameters.AddWithValue("@id", idEquipo);
                    object valor = cmd.ExecuteScalar();
                    if (valor == null)
                        throw new InvalidOperationException("El equipo ya no existe en la base de datos.");
                    stockActual = Convert.ToInt32(valor);
                }

                if (!esEntrada && stockActual < cantidad)
                    throw new InvalidOperationException(
                        "Stock insuficiente. Disponible: " + stockActual + ", solicitado: " + cantidad + ".");

                int nuevoStock = esEntrada ? stockActual + cantidad : stockActual - cantidad;

                const string sqlUpdateStock = @"
UPDATE public.equipos SET stock = @stock WHERE id = @id";
                using (var cmd = new NpgsqlCommand(sqlUpdateStock, conexion, transaccion))
                {
                    cmd.Parameters.AddWithValue("@stock", nuevoStock);
                    cmd.Parameters.AddWithValue("@id", idEquipo);
                    if (cmd.ExecuteNonQuery() != 1)
                        throw new InvalidOperationException("No se pudo actualizar el stock del equipo.");
                }

                if (esEntrada)
                {
                    const string sqlRetorno = @"
INSERT INTO public.retornos
    (id_retorno, fecha_retorno, id_equipo, id_trabajador, estado_equipo, observaciones)
VALUES
    ((SELECT COALESCE(MAX(id_retorno),0)+1 FROM public.retornos),
     @fecha, @idEquipo, @idTrabajador, @estado, @obs)";
                    using (var cmd = new NpgsqlCommand(sqlRetorno, conexion, transaccion))
                    {
                        cmd.Parameters.AddWithValue("@fecha", DateTime.Now);
                        cmd.Parameters.AddWithValue("@idEquipo", idEquipo);
                        cmd.Parameters.AddWithValue("@idTrabajador", idResponsable);
                        cmd.Parameters.AddWithValue("@estado", "Disponible");
                        string obs = "ENTRADA | " + motivo;
                        if (!string.IsNullOrWhiteSpace(ubicacionExterna))
                            obs += " | Origen: " + ubicacionExterna;
                        obs += " | OP:" + operationId;
                        cmd.Parameters.AddWithValue("@obs", obs);
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    int idSalida;
                    const string sqlSalida = @"
INSERT INTO public.salidas
    (id_salida, fecha_salida, id_trabajador, ubicacion, detalles)
VALUES
    ((SELECT COALESCE(MAX(id_salida),0)+1 FROM public.salidas),
     @fecha, @idTrabajador, @ubicacion, @detalles)
RETURNING id_salida";
                    using (var cmd = new NpgsqlCommand(sqlSalida, conexion, transaccion))
                    {
                        cmd.Parameters.AddWithValue("@fecha", DateTime.Now);
                        cmd.Parameters.AddWithValue("@idTrabajador", idResponsable);
                        cmd.Parameters.AddWithValue("@ubicacion", (object)ubicacionExterna ?? DBNull.Value);
                        string detalles = motivo;
                        if (!string.IsNullOrWhiteSpace(ubicacionExterna))
                            detalles += " | Destino: " + ubicacionExterna;
                        cmd.Parameters.AddWithValue("@detalles", detalles);
                        idSalida = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    const string sqlDetalle = @"
INSERT INTO public.salida_equipos
    (id_salida, id_equipo, codigo_escaneado)
VALUES
    (@idSalida, @idEquipo, @op)";
                    using (var cmd = new NpgsqlCommand(sqlDetalle, conexion, transaccion))
                    {
                        cmd.Parameters.AddWithValue("@idSalida", idSalida);
                        cmd.Parameters.AddWithValue("@idEquipo", idEquipo);
                        cmd.Parameters.AddWithValue("@op", operationId);
                        cmd.ExecuteNonQuery();
                    }
                }

                transaccion.Commit();
                return true;
            }
        }

        // ------------------------------------------------------------------
        //  EXPORTAR CSV
        // ------------------------------------------------------------------
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