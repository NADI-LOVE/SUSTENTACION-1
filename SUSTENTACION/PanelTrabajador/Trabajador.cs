#nullable disable
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Npgsql;
using SUSTENTACION.Temas;

namespace SUSTENTACION.PanelTrabajador
{
    /// <summary>
    /// Módulo "Gestión de trabajadores" - ARTEMUSA TV · Artemusa Perú S.R.L.
    ///
    ///  • Trabajadores internos: administradores y encargados del sistema (tabla public.trabajadores).
    ///  • Personal externo: técnicos nuevos o a quienes se les asigna equipo audiovisual
    ///    (tabla public.personal_externo).
    ///
    /// Para sincronizar con el botón "Modo claro / oscuro" del panel principal llama a
    /// <see cref="AplicarTema"/> (true = oscuro, false = claro).
    /// </summary>
    public class Trabajador : TemaUserControl
    {
        // ───────────────────────── Constantes ─────────────────────────
        private const int AnchoMinimo = 1060;
        private const int AltoMinimo = 720;
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private const ControlStyles EstiloPintado =
            ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw;

        // ───────────────────────── Controles ─────────────────────────
        private Panel lienzo;
        private TableLayoutPanel main;
        private BotonModerno btnActualizar, btnNuevo, btnLimpiar, btnExportar;
        private TarjetaResumen cTotal, cActivos, cInactivos, cExternos;
        private CampoTexto buscar;
        private SelectorModerno filtroEstado, filtroRol;
        private PestanasModernas pestanas;
        private DataGridView tablaInternos, tablaExternos;
        private EtiquetaTema lblVacio, lblCantidad, lblConexion;
        private Aviso aviso;
        private ToolTip tip;

        // ───────────────────────── Datos ─────────────────────────
        private DataTable internos, externos, departamentosDatos, rolesDatos;
        private bool cargando;
        private int? seleccionInterno, seleccionExterno;

        // Estado del icono sobre el que está el mouse en las tablas
        private DataGridView gridHover;
        private int filaHover = -1, iconoHover = -1;

        public Trabajador()
        {
            Dock = DockStyle.Fill;
            DoubleBuffered = true;
            Font = Tema.Fuente(9F); // Usa tu fuente local

            tip = new ToolTip();
            CrearInterfaz(); // Crea las tablas y los controles

            SizeChanged += (s, e) => PosicionarAviso();
            Load += (s, e) => { AjustarContenedor(); ActualizarDatos(); };

            // 1. Aplica el tema inicial (por si arranca en modo oscuro)
            AplicarTema();

            // 2. Escucha el evento global y actualiza el estilo de las tablas cuando cambie el tema
            TemaApp.TemaCambiado += () =>
            {
                AplicarEstiloGrid(tablaInternos);
                AplicarEstiloGrid(tablaExternos);
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && tip != null) tip.Dispose();
            base.Dispose(disposing);
        }

        // ═════════════════════════ API PÚBLICA ═════════════════════════

        /// <summary>true = modo oscuro (por defecto), false = modo claro.</summary>
        public bool ModoOscuro { get { return Tema.Oscuro; } }

        public void AplicarTema(bool oscuro)
        {
            Tema.Oscuro = oscuro;
            BackColor = Tema.Fondo;
            AplicarTemaA(this);
            Invalidate(true);
        }

        public void CargarTrabajadores() { ActualizarDatos(); }

        private void AplicarTemaA(Control padre)
        {
            foreach (Control c in padre.Controls)
            {
                if (c is TarjetaPanel) c.BackColor = Tema.Superficie;
                else if (c is DataGridView) AplicarEstiloGrid((DataGridView)c);
                AplicarTemaA(c);
            }
        }

        // ═════════════════════════ INTERFAZ ═════════════════════════
        private void CrearInterfaz()
        {
            lienzo = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(22, 18, 22, 18)
            };
            lienzo.AutoScrollMinSize = new Size(AnchoMinimo + 44, AltoMinimo + 36);
            lienzo.SizeChanged += (s, e) => AjustarContenedor();

            main = new TableLayoutPanel
            {
                Dock = DockStyle.None,
                ColumnCount = 1,
                RowCount = 5,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));    // encabezado
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 124));   // tarjetas
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));    // filtros
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));    // tabla
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));    // estado de conexión
            lienzo.Controls.Add(main);
            Controls.Add(lienzo);

            main.Controls.Add(CrearEncabezado(), 0, 0);
            main.Controls.Add(CrearTarjetas(), 0, 1);
            main.Controls.Add(CrearBarraFiltros(), 0, 2);
            main.Controls.Add(CrearTarjetaTabla(), 0, 3);

            lblConexion = new EtiquetaTema("Conectando con Supabase...", 8.75F, FontStyle.Regular, RolTexto.Libre)
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Tema.Suave
            };
            main.Controls.Add(lblConexion, 0, 4);

            // Aviso flotante (toast). Se agrega al final y se trae al frente.
            aviso = new Aviso();
            Controls.Add(aviso);
            aviso.BringToFront();

            ActualizarBotones();
        }

        private void AjustarContenedor()
        {
            if (main == null) return;
            int w = Math.Max(lienzo.ClientSize.Width - lienzo.Padding.Horizontal, AnchoMinimo);
            int h = Math.Max(lienzo.ClientSize.Height - lienzo.Padding.Vertical, AltoMinimo);
            main.SetBounds(lienzo.Padding.Left + lienzo.AutoScrollPosition.X,
                           lienzo.Padding.Top + lienzo.AutoScrollPosition.Y, w, h);
        }

        private void PosicionarAviso()
        {
            if (aviso == null) return;
            aviso.Location = new Point(Math.Max(10, Width - aviso.Width - 28), Math.Max(10, Height - aviso.Height - 28));
        }

        private Control CrearEncabezado()
        {
            var p = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };

            var titulo = new EtiquetaTema("Gestión de trabajadores", 20F, FontStyle.Bold, RolTexto.Titulo)
            { Location = new Point(0, 2) };
            var subtitulo = new EtiquetaTema("Administra el personal y consulta sus actividades", 9.5F, FontStyle.Regular, RolTexto.Suave)
            { Location = new Point(2, 44) };

            btnActualizar = new BotonModerno("Actualizar", "↻", EstiloBoton.Secundario, 130);
            btnNuevo = new BotonModerno("Nuevo técnico externo", "+", EstiloBoton.Primario, 220);
            btnActualizar.Click += (s, e) => ActualizarDatos();
            btnNuevo.Click += (s, e) => AbrirEditorExterno(null);

            p.Controls.AddRange(new Control[] { titulo, subtitulo, btnActualizar, btnNuevo });
            p.Resize += (s, e) =>
            {
                btnNuevo.Location = new Point(p.Width - btnNuevo.Width, 8);
                btnActualizar.Location = new Point(btnNuevo.Left - btnActualizar.Width - 12, 8);
            };
            return p;
        }

        private Control CrearTarjetas()
        {
            var t = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            for (int i = 0; i < 4; i++) t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            cTotal = new TarjetaResumen("TOTAL DE TRABAJADORES", "☺", Tema.Azul);
            cActivos = new TarjetaResumen("TRABAJADORES ACTIVOS", "✓", Tema.Verde);
            cInactivos = new TarjetaResumen("TRABAJADORES INACTIVOS", "⊘", Tema.Naranja);
            cExternos = new TarjetaResumen("PERSONAL EXTERNO", "↗", Tema.Morado);

            var lista = new[] { cTotal, cActivos, cInactivos, cExternos };
            for (int i = 0; i < lista.Length; i++)
            {
                lista[i].Dock = DockStyle.Fill;
                lista[i].Margin = new Padding(i == 0 ? 0 : 7, 0, i == 3 ? 0 : 7, 12);
                t.Controls.Add(lista[i], i, 0);
            }
            return t;
        }

        private Control CrearBarraFiltros()
        {
            var tarjeta = new TarjetaPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 14),
                Padding = new Padding(16, 14, 16, 14)
            };

            var t = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            buscar = new CampoTexto
            {
                Lupa = true,
                Placeholder = "Buscar por nombre, DNI, contacto o correo...",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 12, 0)
            };

            filtroEstado = new SelectorModerno { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0) };
            filtroEstado.Agregar("Todos los estados");
            filtroEstado.Agregar("Activos");
            filtroEstado.Agregar("Inactivos");
            filtroEstado.SelectedIndex = 0;

            filtroRol = new SelectorModerno { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0) };
            filtroRol.Agregar("Todos los roles");
            filtroRol.SelectedIndex = 0;

            btnLimpiar = new BotonModerno("Limpiar", "✕", EstiloBoton.Secundario, 100)
            { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0) };
            btnExportar = new BotonModerno("Exportar CSV", "⇩", EstiloBoton.Secundario, 140)
            { Dock = DockStyle.Fill, Margin = Padding.Empty };

            buscar.TextoCambiado += (s, e) => Filtrar();
            filtroEstado.CambioSeleccion += (s, e) => Filtrar();
            filtroRol.CambioSeleccion += (s, e) => Filtrar();
            btnLimpiar.Click += (s, e) => LimpiarFiltros();
            btnExportar.Click += (s, e) => ExportarCsv();

            t.Controls.Add(buscar, 0, 0);
            t.Controls.Add(filtroEstado, 1, 0);
            t.Controls.Add(filtroRol, 2, 0);
            t.Controls.Add(btnLimpiar, 3, 0);
            t.Controls.Add(btnExportar, 4, 0);
            tarjeta.Controls.Add(t);
            return tarjeta;
        }

        private Control CrearTarjetaTabla()
        {
            var tarjeta = new TarjetaPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 6),
                Padding = new Padding(18, 14, 18, 16)
            };

            // Zona de tablas (se agrega primero porque es Dock.Fill)
            var zonaTablas = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            tablaInternos = CrearGrid(false);
            tablaExternos = CrearGrid(true);
            tablaInternos.Visible = true;
            tablaExternos.Visible = false;

            lblVacio = new EtiquetaTema("", 10F, FontStyle.Regular, RolTexto.Suave)
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            zonaTablas.Controls.Add(tablaInternos);
            zonaTablas.Controls.Add(tablaExternos);
            zonaTablas.Controls.Add(lblVacio);

            // Barra superior: pestañas + contador
            var barra = new Panel { Dock = DockStyle.Top, Height = 52, Margin = Padding.Empty };
            lblCantidad = new EtiquetaTema("Sin registros", 9F, FontStyle.Regular, RolTexto.Suave)
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight
            };
            pestanas = new PestanasModernas { Dock = DockStyle.Left, Width = 560 };
            pestanas.SetTextos(new[] { "Trabajadores internos", "Personal externo" }, new[] { "☺", "↗" });
            pestanas.Indice = 1;
            pestanas.CambioIndice += (s, e) => { Filtrar(); ActualizarBotones(); MostrarPestana(); };
            barra.Controls.Add(lblCantidad);
            barra.Controls.Add(pestanas);

            tarjeta.Controls.Add(zonaTablas);
            tarjeta.Controls.Add(barra);

            MostrarPestana();
            return tarjeta;
        }

        /// <summary>Muestra la tabla de la pestaña activa o el mensaje de "sin resultados".</summary>
        private void MostrarPestana()
        {
            if (pestanas == null || tablaInternos == null || lblVacio == null) return;
            bool ext = pestanas.Indice == 1;
            DataGridView activa = ext ? tablaExternos : tablaInternos;
            DataGridView otra = ext ? tablaInternos : tablaExternos;
            otra.Visible = false;

            bool vacio = activa.RowCount == 0;
            activa.Visible = !vacio;
            lblVacio.Visible = vacio;

            DataTable datos = ext ? externos : internos;
            lblVacio.Text = (datos == null || datos.Rows.Count == 0)
                ? "Todavía no hay registros en esta sección."
                : "No se encontraron resultados con los filtros aplicados.";
        }

        // ═════════════════════════ TABLAS ═════════════════════════
        private DataGridView CrearGrid(bool externo)
        {
            var t = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BorderStyle = BorderStyle.None,
                EnableHeadersVisualStyles = false,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                EditMode = DataGridViewEditMode.EditProgrammatically
            };
            t.ColumnHeadersHeight = 44;
            t.RowTemplate.Height = 56;
            typeof(DataGridView).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(t, true, null);

            var acciones = Col("acciones", "ACCIONES", null, 0);
            acciones.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            acciones.Width = externo ? 176 : 140;

            if (externo)
            {
                var fecha = Col("fecha_registro", "FECHA DE REGISTRO", "fecha_registro", 120);
                fecha.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                t.Columns.AddRange(
                    Col("num", "N.º", null, 40),
                    Col("nombre", "TÉCNICO EXTERNO", "nombre_completo", 190),
                    Col("dni", "DNI", "dni", 90),
                    Col("celular", "CELULAR", "celular", 100),
                    Col("direccion", "DIRECCIÓN", "direccion", 170),
                    Col("estado", "ESTADO", "estado", 100),
                    fecha,
                    acciones);
            }
            else
            {
                t.Columns.AddRange(
                    Col("num", "N.º", null, 40),
                    Col("nombre", "TRABAJADOR", "nombre", 180),
                    Col("dni", "DNI", "dni", 90),
                    Col("telefono", "TELÉFONO", "telefono", 100),
                    Col("correo", "CORREO", "correo", 190),
                    Col("departamento", "DEPARTAMENTO", "departamento", 140),
                    Col("rol", "ROL", "rol", 120),
                    Col("estado", "ESTADO", "estado", 100),
                    acciones);
            }

            AplicarEstiloGrid(t);

            t.CellPainting += Grid_CellPainting;
            t.CellFormatting += Grid_CellFormatting;
            t.CellMouseMove += Grid_CellMouseMove;
            t.CellMouseLeave += Grid_CellMouseLeave;
            t.CellMouseClick += Grid_CellMouseClick;
            t.CellDoubleClick += Grid_CellDoubleClick;
            t.SelectionChanged += (s, e) => GuardarSeleccion(t);
            t.DataBindingComplete += (s, e) => { t.ClearSelection(); };
            return t;
        }

        private static DataGridViewTextBoxColumn Col(string nombre, string titulo, string propiedad, float peso)
        {
            return new DataGridViewTextBoxColumn
            {
                Name = nombre,
                HeaderText = titulo,
                DataPropertyName = propiedad ?? "",
                FillWeight = Math.Max(1F, peso),
                MinimumWidth = 60,
                SortMode = string.IsNullOrEmpty(propiedad)
                    ? DataGridViewColumnSortMode.NotSortable
                    : DataGridViewColumnSortMode.Automatic
            };
        }

        private void AplicarEstiloGrid(DataGridView t)
        {
            // 1. Configuración del fondo y bordes de la tabla
            t.BackgroundColor = Tema.Superficie;
            t.GridColor = Tema.Borde;
            t.BorderStyle = BorderStyle.None;
            t.EnableHeadersVisualStyles = false; // Vital para que Windows no pinte los headers de gris por defecto
            t.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            t.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            // 2. Estilo de los Encabezados (Columnas)
            t.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Tema.Elevado,
                ForeColor = Tema.Suave,
                Font = Tema.Fuente(8F, FontStyle.Bold),
                Padding = new Padding(10, 0, 0, 0),
                SelectionBackColor = Tema.Elevado,
                SelectionForeColor = Tema.Suave,
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };

            // 3. Estilo de las Celdas Normales
            t.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Tema.Superficie,
                ForeColor = Tema.Texto,
                Font = Tema.Fuente(9.25F),
                SelectionBackColor = Tema.Seleccion,
                SelectionForeColor = Tema.Texto,
                Padding = new Padding(10, 0, 0, 0),
                NullValue = "—"
            };

            // 4. Estilo de las Filas Alternas (Efecto cebra)
            t.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Tema.FilaAlterna,
                ForeColor = Tema.Texto,
                SelectionBackColor = Tema.Seleccion,
                SelectionForeColor = Tema.Texto
            };

            // 5. Detalles específicos de columnas
            if (t.Columns.Contains("num"))
                t.Columns["num"].DefaultCellStyle.ForeColor = Tema.Suave;

            if (t.Columns.Contains("fecha_registro"))
                t.Columns["fecha_registro"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        }

        // ── Iconos de la columna Acciones ──
        private static Icono[] AccionesDe(bool externo)
        {
            return externo
                ? new[] { Icono.Ver, Icono.WhatsApp, Icono.Editar, Icono.Power }
                : new[] { Icono.Ver, Icono.Editar, Icono.Power };
        }

        private static string TextoIcono(Icono i, bool activo)
        {
            switch (i)
            {
                case Icono.Ver: return "Ver detalles";
                case Icono.Editar: return "Editar";
                case Icono.WhatsApp: return "Abrir WhatsApp";
                default: return activo ? "Desactivar" : "Reactivar";
            }
        }

        private static Rectangle RectIcono(Rectangle celda, int i)
        {
            return new Rectangle(celda.X + 8 + i * 38, celda.Y + (celda.Height - 32) / 2, 32, 32);
        }

        private int IconoEn(DataGridView grid, int col, int fila, Point local)
        {
            Rectangle celda = grid.GetCellDisplayRectangle(col, fila, false);
            int n = AccionesDe(grid == tablaExternos).Length;
            for (int i = 0; i < n; i++)
                if (RectIcono(new Rectangle(0, 0, celda.Width, celda.Height), i).Contains(local)) return i;
            return -1;
        }

        private static bool FilaActiva(DataGridView grid, int fila)
        {
            if (fila < 0 || fila >= grid.RowCount) return false;
            DataRowView v = grid.Rows[fila].DataBoundItem as DataRowView;
            return v != null && Convert.ToString(v["estado"]) == "Activo";
        }

        private void InvalidarAcciones(DataGridView grid, int fila)
        {
            if (fila < 0 || fila >= grid.RowCount || !grid.Columns.Contains("acciones")) return;
            grid.InvalidateCell(grid.Columns["acciones"].Index, fila);
        }

        // ── Eventos de las tablas ──
        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var grid = (DataGridView)sender;
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (grid.Columns[e.ColumnIndex].Name == "num")
            {
                e.Value = (e.RowIndex + 1).ToString(Inv);
                e.FormattingApplied = true;
            }
        }

        private void Grid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            var grid = (DataGridView)sender;
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string col = grid.Columns[e.ColumnIndex].Name;
            if (col != "nombre" && col != "estado" && col != "rol" && col != "acciones") return;

            e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border |
                                  DataGridViewPaintParts.SelectionBackground);
            var g = e.Graphics;
            Tema.Calidad(g);
            var celda = e.CellBounds;
            string texto = Convert.ToString(e.FormattedValue ?? "").Trim();

            if (col == "nombre")
            {
                var av = new RectangleF(celda.X + 12, celda.Y + (celda.Height - 36) / 2f, 36, 36);
                using (var b = new SolidBrush(Tema.ColorAvatar(texto))) g.FillEllipse(b, av);
                Tema.DibujarEn(g, Tema.Iniciales(texto), Tema.Fuente(9F, FontStyle.Bold), Color.White, av,
                    StringAlignment.Center, StringAlignment.Center);
                Tema.DibujarEn(g, texto, Tema.Fuente(9.5F, FontStyle.Bold), Tema.Texto,
                    new RectangleF(av.Right + 10, celda.Y, Math.Max(10, celda.Width - 36 - 12 - 10 - 8), celda.Height));
            }
            else if (col == "estado" || col == "rol")
            {
                if (texto.Length > 0 && texto != "—")
                {
                    bool esEstado = col == "estado";
                    Color c = esEstado
                        ? (texto == "Activo" ? Tema.Verde : Tema.Peligro)
                        : Tema.ColorRol(texto);
                    var f = Tema.Fuente(8F, FontStyle.Bold);
                    float w = Math.Min(celda.Width - 14, Tema.AnchoTexto(g, texto, f) + (esEstado ? 38 : 28));
                    Tema.Insignia(g, texto, c, new RectangleF(celda.X + 10, celda.Y + (celda.Height - 24) / 2f, w, 24), esEstado);
                }
            }
            else // acciones
            {
                bool externo = grid == tablaExternos;
                var iconos = AccionesDe(externo);
                bool activo = FilaActiva(grid, e.RowIndex);
                for (int i = 0; i < iconos.Length; i++)
                {
                    var r = RectIcono(celda, i);
                    bool hover = grid == gridHover && e.RowIndex == filaHover && iconoHover == i;
                    using (var p = Tema.Redondo(r, 9))
                    {
                        using (var b = new SolidBrush(hover ? Tema.Borde : Tema.Elevado)) g.FillPath(b, p);
                        using (var pen = new Pen(hover ? Tema.AcentoTexto : Tema.Borde, 1F)) g.DrawPath(pen, p);
                    }
                    Color cIcono = iconos[i] == Icono.Power ? (activo ? Tema.Peligro : Tema.Verde)
                                 : iconos[i] == Icono.WhatsApp ? Tema.WhatsApp
                                 : Tema.Suave;
                    if (hover && iconos[i] != Icono.Power && iconos[i] != Icono.WhatsApp) cIcono = Tema.Texto;
                    Tema.DibujarIcono(g, iconos[i], r, cIcono);
                }
            }
            e.Handled = true;
        }

        private void Grid_CellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
        {
            var grid = (DataGridView)sender;
            int icono = -1;
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && grid.Columns[e.ColumnIndex].Name == "acciones")
                icono = IconoEn(grid, e.ColumnIndex, e.RowIndex, e.Location);

            if (grid == gridHover && e.RowIndex == filaHover && icono == iconoHover) return;

            if (gridHover != null && iconoHover >= 0) InvalidarAcciones(gridHover, filaHover);
            gridHover = grid; filaHover = e.RowIndex; iconoHover = icono;
            grid.Cursor = icono >= 0 ? Cursors.Hand : Cursors.Default;

            if (icono >= 0)
            {
                var iconos = AccionesDe(grid == tablaExternos);
                tip.SetToolTip(grid, TextoIcono(iconos[icono], FilaActiva(grid, e.RowIndex)));
                InvalidarAcciones(grid, e.RowIndex);
            }
            else tip.SetToolTip(grid, "");
        }

        private void Grid_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            var grid = (DataGridView)sender;
            if (gridHover != null && iconoHover >= 0) InvalidarAcciones(gridHover, filaHover);
            gridHover = null; filaHover = -1; iconoHover = -1;
            grid.Cursor = Cursors.Default;
            tip.SetToolTip(grid, "");
        }

        private void Grid_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            var grid = (DataGridView)sender;
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].Name != "acciones") return;
            int i = IconoEn(grid, e.ColumnIndex, e.RowIndex, e.Location);
            if (i < 0) return;
            DataRowView v = grid.Rows[e.RowIndex].DataBoundItem as DataRowView;
            if (v == null) return;
            bool externo = grid == tablaExternos;
            EjecutarAccion(externo, AccionesDe(externo)[i], v.Row);
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            var grid = (DataGridView)sender;
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].Name == "acciones") return;
            DataRowView v = grid.Rows[e.RowIndex].DataBoundItem as DataRowView;
            if (v == null) return;
            if (grid == tablaExternos) VerFichaExterno(v.Row); else VerFichaInterno(v.Row);
        }

        private void EjecutarAccion(bool externo, Icono icono, DataRow fila)
        {
            switch (icono)
            {
                case Icono.Ver:
                    if (externo) VerFichaExterno(fila); else VerFichaInterno(fila);
                    break;
                case Icono.Editar:
                    if (externo) AbrirEditorExterno(fila); else AbrirEditorInterno(fila);
                    break;
                case Icono.WhatsApp:
                    AbrirWhatsApp(fila);
                    break;
                case Icono.Power:
                    if (externo) CambiarEstadoExterno(fila); else CambiarEstadoInterno(fila);
                    break;
            }
        }

        private void GuardarSeleccion(DataGridView grid)
        {
            if (cargando || grid.CurrentRow == null) return;
            DataRowView v = grid.CurrentRow.DataBoundItem as DataRowView;
            if (v == null) return;
            if (grid == tablaExternos) seleccionExterno = Convert.ToInt32(v["id_personal"]);
            else seleccionInterno = Convert.ToInt32(v["id"]);
        }

        private static void RestaurarSeleccion(DataGridView grid, string columnaId, int? id)
        {
            if (!id.HasValue) return;
            try
            {
                foreach (DataGridViewRow fila in grid.Rows)
                {
                    DataRowView v = fila.DataBoundItem as DataRowView;
                    if (v != null && Convert.ToInt32(v[columnaId]) == id.Value)
                    {
                        grid.CurrentCell = fila.Cells["nombre"];
                        return;
                    }
                }
            }
            catch (InvalidOperationException) { /* la celda no se pudo seleccionar: se ignora */ }
        }

        // ═════════════════════════ DATOS (Supabase) ═════════════════════════
        private NpgsqlConnection Conectar()
        {
            NpgsqlConnection c = new global::SUSTENTACION.Conexion().ObtenerConexion();
            if (c == null) throw new InvalidOperationException("La conexión a la base de datos no está disponible.");
            if (c.State != ConnectionState.Open) c.Open();
            return c;
        }

        private void ActualizarDatos()
        {
            if (cargando) return;
            cargando = true;
            btnActualizar.Text = "Actualizando...";
            btnActualizar.Enabled = false;
            bool exito = false;

            try
            {
                DataTable nuevosI = new DataTable(), nuevosE = new DataTable();
                DataTable nuevosD = new DataTable(), nuevosR = new DataTable();

                using (NpgsqlConnection c = Conectar())
                {
                    const string qInternos = @"SELECT t.id_trabajador AS id, t.nombre AS nombre,
                        COALESCE(t.dni,'') AS dni, COALESCE(t.telefono,'') AS telefono,
                        t.correo, t.id_rol, t.id_departamento, COALESCE(r.nombre_rol,'Sin rol') AS rol,
                        COALESCE(d.nombre_departamento,'Sin departamento') AS departamento,
                        CASE WHEN t.activo THEN 'Activo' ELSE 'Inactivo' END AS estado
                        FROM public.trabajadores t
                        LEFT JOIN public.departamentos d ON d.id_departamento=t.id_departamento
                        LEFT JOIN public.roles r ON r.id_rol=t.id_rol
                        ORDER BY t.nombre";
                    using (var a = new NpgsqlDataAdapter(qInternos, c)) a.Fill(nuevosI);

                    const string qExternos = @"SELECT id_personal, nombre, apellido, dni, celular,
                        direccion, activo, fecha_registro,
                        CASE WHEN activo THEN 'Activo' ELSE 'Inactivo' END AS estado
                        FROM public.personal_externo ORDER BY id_personal DESC";
                    using (var a = new NpgsqlDataAdapter(qExternos, c)) a.Fill(nuevosE);

                    using (var a = new NpgsqlDataAdapter(
                        "SELECT id_departamento, nombre_departamento FROM public.departamentos ORDER BY nombre_departamento", c))
                        a.Fill(nuevosD);
                    using (var a = new NpgsqlDataAdapter(
                        "SELECT id_rol, nombre_rol FROM public.roles ORDER BY nombre_rol", c))
                        a.Fill(nuevosR);
                }

                nuevosE.Columns.Add("nombre_completo", typeof(string), "nombre + ' ' + apellido");

                DataTable viejoI = internos, viejoE = externos;
                internos = nuevosI; externos = nuevosE;
                if (departamentosDatos != null) departamentosDatos.Dispose();
                if (rolesDatos != null) rolesDatos.Dispose();
                departamentosDatos = nuevosD; rolesDatos = nuevosR;

                tablaInternos.DataSource = internos;
                tablaExternos.DataSource = externos;
                if (viejoI != null) viejoI.Dispose();
                if (viejoE != null) viejoE.Dispose();

                PoblarFiltroRol();
                ActualizarTarjetas();

                lblConexion.Text = "●  Conectado a Supabase  ·  " + DateTime.Now.ToString("HH:mm:ss", Inv);
                lblConexion.ForeColor = Tema.Verde;
                exito = true;
            }
            catch (Exception ex)
            {
                lblConexion.Text = "●  Error al cargar datos: " + ex.Message;
                lblConexion.ForeColor = Tema.Peligro;
                cTotal.Valor = cActivos.Valor = cInactivos.Valor = cExternos.Valor = "—";
                Informar("Error al consultar Supabase", ex.Message, "!");
            }
            finally
            {
                cargando = false;
                btnActualizar.Text = "Actualizar";
                btnActualizar.Enabled = true;
                Filtrar();
                ActualizarBotones();
                if (exito)
                {
                    RestaurarSeleccion(tablaInternos, "id", seleccionInterno);
                    RestaurarSeleccion(tablaExternos, "id_personal", seleccionExterno);
                }
            }
        }

        private void PoblarFiltroRol()
        {
            string actual = filtroRol.SelectedItem as string;
            filtroRol.LimpiarItems();
            filtroRol.Agregar("Todos los roles");
            foreach (DataRow r in rolesDatos.Rows) filtroRol.Agregar(Convert.ToString(r["nombre_rol"]));
            int idx = actual == null ? 0 : filtroRol.IndexOf(actual);
            filtroRol.SelectedIndex = idx < 0 ? 0 : idx;
        }

        private void ActualizarTarjetas()
        {
            int totalI = internos.Rows.Count;
            int totalE = externos.Rows.Count;
            int activosI = internos.Select("estado = 'Activo'").Length;
            int activosE = externos.Select("estado = 'Activo'").Length;
            int total = totalI + totalE;
            int activos = activosI + activosE;
            int inactivos = total - activos;

            cTotal.Valor = total.ToString("N0");
            cTotal.Detalle = totalI + " internos  ·  " + totalE + " externos";
            cActivos.Valor = activos.ToString("N0");
            cActivos.Detalle = (total == 0 ? 0 : (int)Math.Round(activos * 100.0 / total)) + "% del personal";
            cInactivos.Valor = inactivos.ToString("N0");
            cInactivos.Detalle = "Cuentas desactivadas";
            cExternos.Valor = totalE.ToString("N0");
            cExternos.Detalle = activosE + " activos con equipos";

            pestanas.SetTextos(
                new[] { "Trabajadores internos  ·  " + totalI, "Personal externo  ·  " + totalE },
                new[] { "☺", "↗" });
        }

        // ═════════════════════════ FILTROS Y BOTONES ═════════════════════════
        private void LimpiarFiltros()
        {
            buscar.Texto = "";
            filtroEstado.SelectedIndex = 0;
            filtroRol.SelectedIndex = 0;
        }

        private static string EscaparFiltro(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s ?? "")
            {
                if (c == '\'') sb.Append("''");
                else if (c == '[' || c == ']' || c == '%' || c == '*') sb.Append('[').Append(c).Append(']');
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private void Filtrar()
        {
            if (cargando || buscar == null || pestanas == null || lblCantidad == null) return;
            bool ext = pestanas.Indice == 1;
            DataTable datos = ext ? externos : internos;
            if (datos == null)
            {
                lblCantidad.Text = "Sin registros";
                MostrarPestana();
                return;
            }

            var condiciones = new List<string>();

            string texto = EscaparFiltro(buscar.Texto.Trim());
            if (texto.Length > 0)
            {
                string[] campos = ext
                    ? new[] { "nombre", "apellido", "dni", "celular", "direccion" }
                    : new[] { "nombre", "dni", "telefono", "correo", "departamento" };
                var partes = campos.Select(c => "Convert([" + c + "], 'System.String') LIKE '%" + texto + "%'");
                condiciones.Add("(" + string.Join(" OR ", partes) + ")");
            }

            if (filtroEstado.SelectedIndex == 1) condiciones.Add("[estado] = 'Activo'");
            else if (filtroEstado.SelectedIndex == 2) condiciones.Add("[estado] = 'Inactivo'");

            if (!ext && filtroRol.SelectedIndex > 0)
                condiciones.Add("[rol] = '" + EscaparFiltro(Convert.ToString(filtroRol.SelectedItem)) + "'");

            try { datos.DefaultView.RowFilter = string.Join(" AND ", condiciones); }
            catch (EvaluateException) { datos.DefaultView.RowFilter = ""; }

            lblCantidad.Text = "Mostrando " + datos.DefaultView.Count + " de " + datos.Rows.Count +
                               (ext ? " técnicos" : " trabajadores");
            MostrarPestana();
        }

        private void ActualizarBotones()
        {
            if (btnNuevo == null || pestanas == null) return;
            bool ext = pestanas.Indice == 1;
            btnNuevo.Enabled = ext && !cargando;
            btnExportar.Enabled = !cargando;
            filtroRol.Enabled = !ext;
            tip.SetToolTip(btnNuevo, ext ? "" : "Disponible en la pestaña «Personal externo»");
        }

        // ═════════════════════════ FORMULARIO: TÉCNICO EXTERNO ═════════════════════════
        private void AbrirEditorExterno(DataRow registro)
        {
            bool editar = registro != null;
            DialogResult resultado;

            using (var d = new DialogoBase(
                editar ? "Editar técnico externo" : "Nuevo técnico externo",
                "Persona que puede ser responsable de equipos audiovisuales.",
                "↗", new Size(660, 470), Tema.Morado))
            {
                var nombre = new CampoTexto { Placeholder = "Ej. Carlos", MaxLength = 100, Texto = Valor(registro, "nombre") };
                var apellido = new CampoTexto { Placeholder = "Ej. Ramírez Soto", MaxLength = 100, Texto = Valor(registro, "apellido") };
                var dni = new CampoTexto { Placeholder = "8 dígitos", MaxLength = 8, SoloDigitos = true, Texto = Valor(registro, "dni") };
                var celular = new CampoTexto { Placeholder = "9XXXXXXXX", MaxLength = 9, SoloDigitos = true, Texto = Valor(registro, "celular") };
                var direccion = new CampoTexto { Placeholder = "Dirección (opcional)", MaxLength = 150, Texto = Valor(registro, "direccion") };
                var activo = new InterruptorModerno("Técnico activo", "Técnico inactivo")
                { Activo = registro == null || Convert.ToBoolean(registro["activo"]) };

                var rejilla = NuevaRejilla(3);
                rejilla.Controls.Add(Celda("Nombres *", nombre));
                rejilla.Controls.Add(Celda("Apellidos *", apellido));
                rejilla.Controls.Add(Celda("DNI (8 dígitos) *", dni));
                rejilla.Controls.Add(Celda("Celular (9 dígitos) *", celular));
                rejilla.Controls.Add(Celda("Dirección", direccion));
                rejilla.Controls.Add(Celda("Estado", activo));
                d.Cuerpo.Controls.Add(rejilla);

                var guardar = new BotonModerno("Guardar técnico", "✓", EstiloBoton.Primario, 170) { Margin = new Padding(10, 0, 0, 0) };
                var cancelar = new BotonModerno("Cancelar", "", EstiloBoton.Secundario, 120) { Margin = new Padding(10, 0, 0, 0) };
                d.Pie.Controls.Add(guardar);
                d.Pie.Controls.Add(cancelar);
                cancelar.Click += (s, e) => { d.DialogResult = DialogResult.Cancel; d.Close(); };

                guardar.Click += (s, e) =>
                {
                    bool hayError = false;
                    hayError |= Marcar(nombre, string.IsNullOrWhiteSpace(nombre.Texto));
                    hayError |= Marcar(apellido, string.IsNullOrWhiteSpace(apellido.Texto));
                    hayError |= Marcar(dni, !Regex.IsMatch(dni.Texto.Trim(), @"^\d{8}$"));
                    hayError |= Marcar(celular, !Regex.IsMatch(celular.Texto.Trim(), @"^9\d{8}$"));
                    if (hayError)
                    {
                        d.MensajeError.Text = "Revisa los campos en rojo: DNI de 8 dígitos y celular de 9 dígitos que empiece por 9.";
                        return;
                    }
                    d.MensajeError.Text = "";
                    guardar.Enabled = false;
                    try
                    {
                        using (NpgsqlConnection c = Conectar())
                        {
                            string sql = editar
                                ? @"UPDATE public.personal_externo SET nombre=@nombre, apellido=@apellido,
                                    dni=@dni, celular=@celular, direccion=@direccion, activo=@activo WHERE id_personal=@id"
                                : @"INSERT INTO public.personal_externo (nombre, apellido, dni, celular, direccion, activo)
                                    VALUES (@nombre, @apellido, @dni, @celular, @direccion, @activo)";
                            using (var cmd = new NpgsqlCommand(sql, c))
                            {
                                cmd.Parameters.AddWithValue("@nombre", nombre.Texto.Trim());
                                cmd.Parameters.AddWithValue("@apellido", apellido.Texto.Trim());
                                cmd.Parameters.AddWithValue("@dni", dni.Texto.Trim());
                                cmd.Parameters.AddWithValue("@celular", celular.Texto.Trim());
                                cmd.Parameters.AddWithValue("@direccion", direccion.Texto.Trim());
                                cmd.Parameters.AddWithValue("@activo", activo.Activo);
                                if (editar) cmd.Parameters.AddWithValue("@id", Convert.ToInt32(registro["id_personal"]));
                                if (cmd.ExecuteNonQuery() != 1)
                                    throw new InvalidOperationException("No se guardó el registro esperado.");
                            }
                        }
                        d.DialogResult = DialogResult.OK;
                        d.Close();
                    }
                    catch (PostgresException ex) when (ex.SqlState == "23505")
                    {
                        Marcar(dni, true);
                        d.MensajeError.Text = "Ya existe un registro con ese DNI.";
                    }
                    catch (Exception ex) { d.MensajeError.Text = "No se pudo guardar: " + ex.Message; }
                    finally { guardar.Enabled = true; }
                };

                resultado = d.ShowDialog(FindForm());
            }

            if (resultado == DialogResult.OK)
            {
                aviso.Mostrar(editar ? "Técnico actualizado correctamente" : "Técnico registrado correctamente", true);
                ActualizarDatos();
            }
        }

        // ═════════════════════════ FORMULARIO: TRABAJADOR INTERNO ═════════════════════════
        private void AbrirEditorInterno(DataRow registro)
        {
            if (registro == null) return;
            if (departamentosDatos == null || rolesDatos == null || rolesDatos.Rows.Count == 0)
            {
                Informar("Sin roles disponibles", "No hay roles registrados en Supabase.", "!");
                return;
            }

            DialogResult resultado;
            using (var d = new DialogoBase(
                "Editar trabajador interno",
                "Datos administrativos. La contraseña y el acceso no se modifican aquí.",
                "☺", new Size(660, 560), Tema.AzulVivo))
            {
                var nombre = new CampoTexto { MaxLength = 150, Texto = Valor(registro, "nombre") };
                var dni = new CampoTexto { Placeholder = "8 dígitos", MaxLength = 8, SoloDigitos = true, Texto = Valor(registro, "dni") };
                var telefono = new CampoTexto { Placeholder = "9XXXXXXXX", MaxLength = 9, SoloDigitos = true, Texto = Valor(registro, "telefono") };
                var correo = new CampoTexto { Placeholder = "correo@empresa.com", MaxLength = 150, Texto = Valor(registro, "correo") };

                var departamento = new SelectorModerno();
                departamento.Agregar(new Opcion(-1, "Sin departamento"));
                foreach (DataRow dep in departamentosDatos.Rows)
                    departamento.Agregar(new Opcion(Convert.ToInt32(dep["id_departamento"]), Convert.ToString(dep["nombre_departamento"])));
                SeleccionarOpcion(departamento, registro["id_departamento"] == DBNull.Value ? -1 : Convert.ToInt32(registro["id_departamento"]));

                var rol = new SelectorModerno { Placeholder = "Selecciona un rol" };
                foreach (DataRow r in rolesDatos.Rows)
                    rol.Agregar(new Opcion(Convert.ToInt32(r["id_rol"]), Convert.ToString(r["nombre_rol"])));
                SeleccionarOpcion(rol, Convert.ToInt32(registro["id_rol"]));

                var activo = new InterruptorModerno("Trabajador activo", "Trabajador inactivo")
                { Activo = Convert.ToString(registro["estado"]) == "Activo" };

                var rejilla = NuevaRejilla(4);
                rejilla.Controls.Add(Celda("Nombre completo *", nombre));
                rejilla.Controls.Add(Celda("DNI (8 dígitos)", dni));
                rejilla.Controls.Add(Celda("Celular (9 dígitos)", telefono));
                rejilla.Controls.Add(Celda("Correo electrónico *", correo));
                rejilla.Controls.Add(Celda("Departamento (opcional)", departamento));
                rejilla.Controls.Add(Celda("Rol *", rol));
                rejilla.Controls.Add(Celda("Estado de la cuenta", activo));
                d.Cuerpo.Controls.Add(rejilla);

                var guardar = new BotonModerno("Guardar cambios", "✓", EstiloBoton.Primario, 180) { Margin = new Padding(10, 0, 0, 0) };
                var cancelar = new BotonModerno("Cancelar", "", EstiloBoton.Secundario, 120) { Margin = new Padding(10, 0, 0, 0) };
                d.Pie.Controls.Add(guardar);
                d.Pie.Controls.Add(cancelar);
                cancelar.Click += (s, e) => { d.DialogResult = DialogResult.Cancel; d.Close(); };

                guardar.Click += (s, e) =>
                {
                    string doc = dni.Texto.Trim();
                    string tel = telefono.Texto.Trim();
                    bool hayError = false;
                    hayError |= Marcar(nombre, string.IsNullOrWhiteSpace(nombre.Texto));
                    hayError |= Marcar(correo, !Regex.IsMatch(correo.Texto.Trim(), @"^[^\s@]+@[^\s@]+\.[^\s@]+$"));
                    hayError |= Marcar(dni, doc.Length > 0 && !Regex.IsMatch(doc, @"^\d{8}$"));
                    hayError |= Marcar(telefono, tel.Length > 0 && !Regex.IsMatch(tel, @"^9\d{8}$"));
                    if (rol.SelectedItem == null) { hayError = true; }
                    if (hayError)
                    {
                        d.MensajeError.Text = "Verifica nombre, correo, DNI, celular y que hayas elegido un rol.";
                        return;
                    }
                    d.MensajeError.Text = "";
                    guardar.Enabled = false;
                    try
                    {
                        using (NpgsqlConnection c = Conectar())
                        using (var cmd = new NpgsqlCommand(@"
                            UPDATE public.trabajadores
                            SET nombre=@nombre, dni=@dni, telefono=@telefono, correo=@correo,
                                id_departamento=@departamento, id_rol=@rol, activo=@activo,
                                fecha_actualizacion=NOW()
                            WHERE id_trabajador=@id", c))
                        {
                            cmd.Parameters.AddWithValue("@nombre", nombre.Texto.Trim());
                            cmd.Parameters.Add("@dni", NpgsqlTypes.NpgsqlDbType.Varchar).Value = doc.Length == 0 ? (object)DBNull.Value : doc;
                            cmd.Parameters.Add("@telefono", NpgsqlTypes.NpgsqlDbType.Varchar).Value = tel.Length == 0 ? (object)DBNull.Value : tel;
                            cmd.Parameters.AddWithValue("@correo", correo.Texto.Trim());
                            Opcion dep = departamento.SelectedItem as Opcion;
                            cmd.Parameters.Add("@departamento", NpgsqlTypes.NpgsqlDbType.Integer).Value =
                                dep == null || dep.Id < 0 ? (object)DBNull.Value : dep.Id;
                            cmd.Parameters.AddWithValue("@rol", ((Opcion)rol.SelectedItem).Id);
                            cmd.Parameters.AddWithValue("@activo", activo.Activo);
                            cmd.Parameters.AddWithValue("@id", Convert.ToInt32(registro["id"]));
                            if (cmd.ExecuteNonQuery() != 1)
                                throw new InvalidOperationException("No se actualizó el trabajador.");
                        }
                        d.DialogResult = DialogResult.OK;
                        d.Close();
                    }
                    catch (PostgresException ex) when (ex.SqlState == "23505")
                    {
                        d.MensajeError.Text = "El DNI o el correo ya está registrado en otro trabajador.";
                    }
                    catch (Exception ex) { d.MensajeError.Text = "No se pudieron guardar los cambios: " + ex.Message; }
                    finally { guardar.Enabled = true; }
                };

                resultado = d.ShowDialog(FindForm());
            }

            if (resultado == DialogResult.OK)
            {
                aviso.Mostrar("Trabajador actualizado correctamente", true);
                ActualizarDatos();
            }
        }

        // ═════════════════════════ CAMBIO DE ESTADO ═════════════════════════
        private void CambiarEstadoExterno(DataRow r)
        {
            if (r == null) return;
            bool nuevoEstado = !Convert.ToBoolean(r["activo"]);
            string nombre = Convert.ToString(r["nombre_completo"]);
            if (!Confirmar(nuevoEstado ? "Reactivar técnico" : "Desactivar técnico",
                "¿" + (nuevoEstado ? "Reactivar" : "Desactivar") + " a " + nombre + "?\nEl historial se conservará.",
                nuevoEstado ? "✓" : "!", !nuevoEstado, nuevoEstado ? "Reactivar" : "Desactivar")) return;
            try
            {
                using (NpgsqlConnection c = Conectar())
                using (var cmd = new NpgsqlCommand(
                    "UPDATE public.personal_externo SET activo=@activo WHERE id_personal=@id AND activo=@anterior", c))
                {
                    cmd.Parameters.AddWithValue("@activo", nuevoEstado);
                    cmd.Parameters.AddWithValue("@anterior", !nuevoEstado);
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(r["id_personal"]));
                    if (cmd.ExecuteNonQuery() != 1)
                        throw new InvalidOperationException("El registro cambió en otra sesión. Actualiza la lista.");
                }
                aviso.Mostrar(nuevoEstado ? "Técnico reactivado" : "Técnico desactivado", true);
                ActualizarDatos();
            }
            catch (Exception ex) { Informar("No se pudo cambiar el estado", ex.Message, "!"); }
        }

        private void CambiarEstadoInterno(DataRow r)
        {
            if (r == null) return;
            bool estabaActivo = Convert.ToString(r["estado"]) == "Activo";
            bool nuevoEstado = !estabaActivo;
            string nombre = Convert.ToString(r["nombre"]);
            if (!Confirmar(nuevoEstado ? "Reactivar trabajador" : "Desactivar trabajador",
                "¿" + (nuevoEstado ? "Reactivar" : "Desactivar") + " la cuenta de " + nombre + "?",
                nuevoEstado ? "✓" : "!", !nuevoEstado, nuevoEstado ? "Reactivar" : "Desactivar")) return;
            try
            {
                using (NpgsqlConnection c = Conectar())
                using (var cmd = new NpgsqlCommand(@"UPDATE public.trabajadores
                    SET activo=@activo, fecha_actualizacion=NOW()
                    WHERE id_trabajador=@id AND activo=@anterior", c))
                {
                    cmd.Parameters.AddWithValue("@activo", nuevoEstado);
                    cmd.Parameters.AddWithValue("@anterior", !nuevoEstado);
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(r["id"]));
                    if (cmd.ExecuteNonQuery() != 1)
                        throw new InvalidOperationException("El registro cambió en otra sesión. Actualiza la lista.");
                }
                aviso.Mostrar(nuevoEstado ? "Cuenta reactivada" : "Cuenta desactivada", true);
                ActualizarDatos();
            }
            catch (Exception ex) { Informar("No se pudo cambiar el estado", ex.Message, "!"); }
        }

        // ═════════════════════════ FICHAS ═════════════════════════
        private void VerFichaExterno(DataRow r)
        {
            if (r == null) return;
            DateTime? fecha = r["fecha_registro"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["fecha_registro"]);
            var datos = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("DNI", Valor(r, "dni")),
                new KeyValuePair<string, string>("Celular", Valor(r, "celular")),
                new KeyValuePair<string, string>("Dirección", Valor(r, "direccion")),
                new KeyValuePair<string, string>("Fecha de registro", fecha.HasValue ? fecha.Value.ToString("dd/MM/yyyy HH:mm", Inv) : "—")
            };
            bool editar = MostrarFicha("Ficha del técnico", "Técnico externo", "↗", Tema.Morado,
                Valor(r, "nombre_completo"), "Técnico externo", Convert.ToBoolean(r["activo"]), "Externo", datos);
            if (editar) AbrirEditorExterno(r);
        }

        private void VerFichaInterno(DataRow r)
        {
            if (r == null) return;
            var datos = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("DNI", Valor(r, "dni")),
                new KeyValuePair<string, string>("Teléfono", Valor(r, "telefono")),
                new KeyValuePair<string, string>("Correo", Valor(r, "correo")),
                new KeyValuePair<string, string>("Departamento", Valor(r, "departamento"))
            };
            bool editar = MostrarFicha("Ficha del trabajador", "Trabajador interno", "☺", Tema.AzulVivo,
                Valor(r, "nombre"), "Trabajador interno del sistema", Convert.ToString(r["estado"]) == "Activo",
                Valor(r, "rol"), datos);
            if (editar) AbrirEditorInterno(r);
        }

        /// <summary>Muestra la ficha. Devuelve true si el usuario pulsó "Editar".</summary>
        private bool MostrarFicha(string titulo, string subtitulo, string glifo, Color acento,
            string nombre, string descripcion, bool activo, string rol, List<KeyValuePair<string, string>> datos)
        {
            bool editar = false;
            using (var d = new DialogoBase(titulo, subtitulo, glifo, new Size(640, 560), acento))
            {
                var ficha = new FichaPersona
                {
                    Dock = DockStyle.Fill,
                    Nombre = nombre,
                    Subtitulo = descripcion,
                    Activo = activo,
                    Rol = rol,
                    Datos = datos
                };
                d.Cuerpo.Controls.Add(ficha);

                var cerrar = new BotonModerno("Cerrar", "", EstiloBoton.Secundario, 120) { Margin = new Padding(10, 0, 0, 0) };
                var editarBtn = new BotonModerno("Editar", "✎", EstiloBoton.Primario, 130) { Margin = new Padding(10, 0, 0, 0) };
                d.Pie.Controls.Add(editarBtn);
                d.Pie.Controls.Add(cerrar);
                cerrar.Click += (s, e) => { d.DialogResult = DialogResult.Cancel; d.Close(); };
                editarBtn.Click += (s, e) => { editar = true; d.DialogResult = DialogResult.OK; d.Close(); };
                d.ShowDialog(FindForm());
            }
            return editar;
        }

        // ═════════════════════════ WHATSAPP ═════════════════════════
        private void AbrirWhatsApp(DataRow tecnico)
        {
            if (tecnico == null) return;
            string celular = Regex.Replace(Convert.ToString(tecnico["celular"]) ?? "", @"\D", "");
            // Perú: 9 dígitos nacionales; wa.me requiere el prefijo internacional 51.
            if (Regex.IsMatch(celular, @"^9\d{8}$")) celular = "51" + celular;
            if (!Regex.IsMatch(celular, @"^519[0-9]{8}$"))
            {
                Informar("WhatsApp", "El técnico no tiene un celular peruano válido de 9 dígitos.", "!");
                return;
            }
            string nombre = Convert.ToString(tecnico["nombre_completo"]);
            string mensaje = "Hola " + nombre + ", le escribimos de Artemusa TV para coordinar sobre los equipos audiovisuales. Saludos.";
            string url = "https://wa.me/" + celular + "?text=" + Uri.EscapeDataString(mensaje);
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch (Exception ex) { Informar("WhatsApp", "No se pudo abrir WhatsApp: " + ex.Message, "!"); }
        }

        // ═════════════════════════ EXPORTAR CSV ═════════════════════════
        private void ExportarCsv()
        {
            bool ext = pestanas.Indice == 1;
            DataTable datos = ext ? externos : internos;
            if (datos == null || datos.DefaultView.Count == 0)
            {
                Informar("Exportar CSV", "No hay datos visibles para exportar.", "!");
                return;
            }
            string[] campos = ext
                ? new[] { "id_personal", "nombre", "apellido", "dni", "celular", "direccion", "estado", "fecha_registro" }
                : new[] { "id", "nombre", "dni", "telefono", "correo", "departamento", "rol", "estado" };

            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "CSV (*.csv)|*.csv";
                dlg.FileName = "artemusa_personal_" + DateTime.Now.ToString("yyyyMMdd_HHmm", Inv) + ".csv";
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                try
                {
                    using (var w = new StreamWriter(dlg.FileName, false, new UTF8Encoding(true)))
                    {
                        w.WriteLine(string.Join(";", campos));
                        foreach (DataRowView r in datos.DefaultView)
                        {
                            var valores = new string[campos.Length];
                            for (int i = 0; i < campos.Length; i++)
                                valores[i] = EscaparCsv(Convert.ToString(r[campos[i]], CultureInfo.CurrentCulture));
                            w.WriteLine(string.Join(";", valores));
                        }
                    }
                    aviso.Mostrar("CSV exportado correctamente", true);
                }
                catch (Exception ex) { Informar("Error al exportar", ex.Message, "!"); }
            }
        }

        private static string EscaparCsv(string valor)
        {
            valor = valor ?? "";
            string v = valor.TrimStart();
            if (v.Length > 0 && "=+-@".IndexOf(v[0]) >= 0) valor = "'" + valor;   // evita inyección de fórmulas
            return "\"" + valor.Replace("\"", "\"\"") + "\"";
        }

        // ═════════════════════════ AYUDAS DE FORMULARIOS ═════════════════════════
        private static string Valor(DataRow fila, string clave)
        {
            if (fila == null || !fila.Table.Columns.Contains(clave) || fila[clave] == DBNull.Value) return "";
            return Convert.ToString(fila[clave]);
        }

        private static bool Marcar(CampoTexto campo, bool invalido)
        {
            campo.Error = invalido;
            return invalido;
        }

        private static TableLayoutPanel NuevaRejilla(int filas)
        {
            var t = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = filas,
                Height = filas * 86,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            for (int i = 0; i < filas; i++) t.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
            return t;
        }

        /// <summary>Etiqueta + control, listos para colocar en la rejilla de un formulario.</summary>
        private static Control Celda(string etiqueta, Control campo)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 14, 0), Margin = Padding.Empty };
            campo.Dock = DockStyle.Top;
            panel.Controls.Add(campo);      // se agrega primero: queda debajo de la etiqueta
            panel.Controls.Add(new EtiquetaTema(etiqueta, 8.5F, FontStyle.Bold, RolTexto.Suave)
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 26,
                TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(2, 0, 0, 4)
            });
            return panel;
        }

        private static void SeleccionarOpcion(SelectorModerno lista, int id)
        {
            for (int i = 0; i < lista.Elementos.Count; i++)
            {
                Opcion o = lista.Elementos[i] as Opcion;
                if (o != null && o.Id == id) { lista.SelectedIndex = i; return; }
            }
            if (lista.Elementos.Count > 0 && id == -1) lista.SelectedIndex = 0;
        }

        private bool Confirmar(string titulo, string texto, string glifo, bool peligro, string textoSi)
        {
            using (var d = new DialogoBase(titulo, "", glifo, new Size(480, 270), peligro ? Tema.Peligro : Tema.Acento))
            {
                d.Cuerpo.Controls.Add(new Label
                {
                    Text = texto,
                    Dock = DockStyle.Fill,
                    ForeColor = Tema.Texto,
                    Font = Tema.Fuente(10.5F),
                    Padding = new Padding(2, 14, 0, 0)
                });
                var si = new BotonModerno(textoSi, "", peligro ? EstiloBoton.Peligro : EstiloBoton.Primario, 140) { Margin = new Padding(10, 0, 0, 0) };
                var no = new BotonModerno("Cancelar", "", EstiloBoton.Secundario, 120) { Margin = new Padding(10, 0, 0, 0) };
                d.Pie.Controls.Add(si);
                d.Pie.Controls.Add(no);
                si.Click += (s, e) => { d.DialogResult = DialogResult.OK; d.Close(); };
                no.Click += (s, e) => { d.DialogResult = DialogResult.Cancel; d.Close(); };
                return d.ShowDialog(FindForm()) == DialogResult.OK;
            }
        }

        private void Informar(string titulo, string texto, string glifo)
        {
            using (var d = new DialogoBase(titulo, "", glifo, new Size(500, 280), Tema.Naranja))
            {
                d.Cuerpo.Controls.Add(new Label
                {
                    Text = texto,
                    Dock = DockStyle.Fill,
                    ForeColor = Tema.Texto,
                    Font = Tema.Fuente(10F),
                    Padding = new Padding(2, 14, 0, 0)
                });
                var ok = new BotonModerno("Entendido", "", EstiloBoton.Primario, 130) { Margin = new Padding(10, 0, 0, 0) };
                d.Pie.Controls.Add(ok);
                ok.Click += (s, e) => { d.DialogResult = DialogResult.OK; d.Close(); };
                d.ShowDialog(FindForm());
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //                              TEMA
        // ═════════════════════════════════════════════════════════════════
        private enum EstiloBoton { Primario, Secundario, Peligro }
        private enum Icono { Ver, Editar, WhatsApp, Power }
        private enum RolTexto { Titulo, Normal, Suave, Libre }

        private sealed class Opcion
        {
            public int Id { get; private set; }
            public string Nombre { get; private set; }
            public Opcion(int id, string nombre) { Id = id; Nombre = nombre; }
            public override string ToString() { return Nombre; }
        }

        private static class Tema
        {
            public static bool Oscuro = true;

            private static Color C(int r, int g, int b) { return Color.FromArgb(r, g, b); }

            public static Color Fondo { get { return Oscuro ? C(6, 11, 21) : C(240, 244, 248); } }
            public static Color Superficie { get { return Oscuro ? C(13, 20, 35) : Color.White; } }
            public static Color Elevado { get { return Oscuro ? C(20, 30, 50) : C(246, 248, 251); } }
            public static Color Borde { get { return Oscuro ? C(31, 44, 70) : C(221, 228, 237); } }
            public static Color Texto { get { return Oscuro ? C(233, 238, 247) : C(15, 23, 42); } }
            public static Color Suave { get { return Oscuro ? C(131, 148, 175) : C(100, 116, 139); } }
            public static Color Seleccion { get { return Oscuro ? C(20, 42, 42) : C(231, 246, 226); } }

            // AQUÍ ESTÁ LA CORRECCIÓN: En modo claro usamos Color.White en lugar de un gris claro
            public static Color FilaAlterna { get { return Oscuro ? C(11, 18, 32) : Color.White; } }

            public static Color AcentoTexto { get { return Oscuro ? Acento : C(30, 140, 60); } }

            public static readonly Color Acento = Color.FromArgb(132, 214, 38);
            public static readonly Color AcentoOsc = Color.FromArgb(46, 170, 72);
            public static readonly Color TextoSobreAcento = Color.FromArgb(8, 28, 14);
            public static readonly Color Verde = Color.FromArgb(34, 197, 94);
            public static readonly Color Peligro = Color.FromArgb(239, 68, 68);
            public static readonly Color Naranja = Color.FromArgb(245, 158, 11);
            public static readonly Color Azul = Color.FromArgb(59, 130, 246);
            public static readonly Color AzulVivo = Color.FromArgb(59, 130, 246);
            public static readonly Color Morado = Color.FromArgb(168, 85, 247);
            public static readonly Color Cian = Color.FromArgb(34, 211, 238);
            public static readonly Color WhatsApp = Color.FromArgb(37, 211, 102);

            private static readonly Dictionary<string, Font> fuentes = new Dictionary<string, Font>();

            public static Font Fuente(float tam, FontStyle estilo = FontStyle.Regular, string familia = "Segoe UI")
            {
                string k = familia + "|" + tam.ToString(CultureInfo.InvariantCulture) + "|" + (int)estilo;
                Font f;
                if (!fuentes.TryGetValue(k, out f))
                {
                    f = new Font(familia, tam, estilo);
                    fuentes[k] = f;
                }
                return f;
            }

            public static Font Simbolo(float tam) { return Fuente(tam, FontStyle.Regular, "Segoe UI Symbol"); }

            public static void Calidad(Graphics g)
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            }

            public static Color Alfa(Color c, int a) { return Color.FromArgb(a, c.R, c.G, c.B); }

            public static Color FondoDe(Control c)
            {
                return c.Parent != null ? c.Parent.BackColor : Fondo;
            }

            public static GraphicsPath Redondo(RectangleF r, float radio)
            {
                var p = new GraphicsPath();
                if (r.Width <= 1 || r.Height <= 1) return p;
                float d = Math.Min(radio * 2, Math.Min(r.Width, r.Height));
                p.AddArc(r.X, r.Y, d, d, 180, 90);
                p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                p.CloseFigure();
                return p;
            }

            /// <summary>Fondo del padre + tarjeta redondeada con borde.</summary>
            public static void Tarjeta(Graphics g, Control c, float radio = 16F)
            {
                g.Clear(FondoDe(c));
                var rf = new RectangleF(0.5F, 0.5F, c.Width - 2, c.Height - 2);
                using (var p = Redondo(rf, radio))
                {
                    using (var b = new SolidBrush(Superficie)) g.FillPath(b, p);
                    using (var pen = new Pen(Borde, 1F)) g.DrawPath(pen, p);
                }
            }

            public static void Dibujar(Graphics g, string t, Font f, Color c, float x, float y)
            {
                using (var b = new SolidBrush(c)) g.DrawString(t, f, b, x, y);
            }

            public static void DibujarEn(Graphics g, string t, Font f, Color c, RectangleF r,
                StringAlignment h = StringAlignment.Near, StringAlignment v = StringAlignment.Center)
            {
                using (var b = new SolidBrush(c))
                using (var sf = new StringFormat
                {
                    Alignment = h,
                    LineAlignment = v,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                })
                    g.DrawString(t, f, b, r, sf);
            }

            public static float AnchoTexto(Graphics g, string t, Font f)
            {
                return (float)Math.Ceiling(g.MeasureString(t, f).Width);
            }

            /// <summary>Etiqueta (badge) con fondo translúcido del color indicado.</summary>
            public static void Insignia(Graphics g, string texto, Color color, RectangleF r, bool punto)
            {
                using (var p = Redondo(r, r.Height / 2f))
                using (var b = new SolidBrush(Alfa(color, Oscuro ? 40 : 34)))
                    g.FillPath(b, p);

                float tx = r.X;
                if (punto)
                {
                    using (var b = new SolidBrush(color)) g.FillEllipse(b, r.X + 11, r.Y + r.Height / 2f - 3, 6, 6);
                    tx += 14;
                }
                Color txt = Oscuro ? color : ControlPaint.Dark(color, 0.25F);
                DibujarEn(g, texto, Fuente(8F, FontStyle.Bold), txt,
                    new RectangleF(tx, r.Y, r.Width - (tx - r.X), r.Height), StringAlignment.Center, StringAlignment.Center);
            }

            public static string Iniciales(string nombre)
            {
                var partes = (nombre ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (partes.Length == 0) return "?";
                if (partes.Length == 1) return partes[0].Substring(0, 1).ToUpper();
                return (partes[0].Substring(0, 1) + partes[1].Substring(0, 1)).ToUpper();
            }

            private static readonly Color[] PaletaAvatar =
            {
        Color.FromArgb(37, 99, 235), Color.FromArgb(124, 58, 237), Color.FromArgb(5, 150, 105),
        Color.FromArgb(217, 119, 6), Color.FromArgb(219, 39, 119), Color.FromArgb(8, 145, 178)
    };

            public static Color ColorAvatar(string nombre)
            {
                int h = 0;
                foreach (char c in nombre ?? "") h = (h * 31 + c) & 0x7fffffff;
                return PaletaAvatar[h % PaletaAvatar.Length];
            }

            public static Color ColorRol(string rol)
            {
                string r = (rol ?? "").Trim().ToLowerInvariant();
                if (r.Contains("admin")) return Morado;
                if (r.Contains("trabajador") || r.Contains("empleado")) return Azul;
                if (r.Contains("encargado") || r.Contains("supervisor")) return Cian;
                if (r.Contains("extern")) return Naranja;
                return Suave;
            }

            public static void DibujarIcono(Graphics g, Icono ic, RectangleF r, Color c)
            {
                float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
                using (var pen = new Pen(c, 1.7F) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                using (var br = new SolidBrush(c))
                {
                    switch (ic)
                    {
                        case Icono.Ver:
                            g.DrawEllipse(pen, cx - 8, cy - 5, 16, 10);
                            g.FillEllipse(br, cx - 2.5F, cy - 2.5F, 5, 5);
                            break;
                        case Icono.Editar:
                            using (var gruesa = new Pen(c, 3F) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                                g.DrawLine(gruesa, cx - 4, cy + 4, cx + 4, cy - 4);
                            g.DrawLine(pen, cx - 7, cy + 8, cx - 2, cy + 8);
                            break;
                        case Icono.WhatsApp:
                            using (var verde = new SolidBrush(WhatsApp))
                                g.FillEllipse(verde, cx - 10, cy - 10, 20, 20);
                            DibujarEn(g, "✆", Simbolo(9F), Color.White, new RectangleF(cx - 10, cy - 10, 20, 20),
                                StringAlignment.Center, StringAlignment.Center);
                            break;
                        case Icono.Power:
                            g.DrawArc(pen, cx - 6, cy - 6, 12, 12, -55, 290);
                            g.DrawLine(pen, cx, cy - 8, cx, cy - 1);
                            break;
                    }
                }
            }
        }
        // ═════════════════════════════════════════════════════════════════
        //                     CONTROLES PERSONALIZADOS
        // ═════════════════════════════════════════════════════════════════

        // ───────────── Etiqueta que sigue el tema ─────────────
        private sealed class EtiquetaTema : Label
        {
            public RolTexto Rol;

            public EtiquetaTema(string texto, float tam, FontStyle estilo, RolTexto rol)
            {
                Text = texto;
                AutoSize = true;
                BackColor = Color.Transparent;
                Font = Tema.Fuente(tam, estilo);
                Rol = rol;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                if (Rol != RolTexto.Libre)
                {
                    Color c = Rol == RolTexto.Suave ? Tema.Suave : Tema.Texto;
                    if (ForeColor != c) ForeColor = c;
                }
                base.OnPaint(e);
            }
        }

        // ───────────── Tarjeta contenedora (borde redondeado) ─────────────
        private sealed class TarjetaPanel : Panel
        {
            public TarjetaPanel()
            {
                SetStyle(EstiloPintado, true);
                BackColor = Tema.Superficie;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Tema.Calidad(e.Graphics);
                Tema.Tarjeta(e.Graphics, this);
            }
        }

        // ───────────── Tarjeta de resumen ─────────────
        private sealed class TarjetaResumen : Control
        {
            private readonly string titulo, glifo;
            private readonly Color color;
            private string valor = "—", detalle = "";

            public string Valor { get { return valor; } set { valor = value; Invalidate(); } }
            public string Detalle { get { return detalle; } set { detalle = value; Invalidate(); } }

            public TarjetaResumen(string titulo, string glifo, Color color)
            {
                this.titulo = titulo; this.glifo = glifo; this.color = color;
                SetStyle(EstiloPintado, true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                Tema.Calidad(g);
                Tema.Tarjeta(g, this);
                if (Width < 120 || Height < 80) return;

                var ic = new RectangleF(18, 18, 48, 48);
                using (var p = Tema.Redondo(ic, 14))
                using (var b = new SolidBrush(Tema.Alfa(color, 40)))
                    g.FillPath(b, p);
                Tema.DibujarEn(g, glifo, Tema.Simbolo(17F), color, ic, StringAlignment.Center, StringAlignment.Center);

                Tema.DibujarEn(g, titulo, Tema.Fuente(8F, FontStyle.Bold), Tema.Suave,
                    new RectangleF(80, 16, Width - 94, 18));
                Tema.Dibujar(g, valor, Tema.Fuente(24F, FontStyle.Bold), Tema.Texto, 77, 32);

                using (var pen = new Pen(Tema.Borde, 1F))
                    g.DrawLine(pen, 18, Height - 36, Width - 18, Height - 36);
                Tema.DibujarEn(g, detalle, Tema.Fuente(8.25F), Tema.Suave,
                    new RectangleF(18, Height - 34, Width - 36, 26));
            }
        }

        // ───────────── Botón moderno ─────────────
        private sealed class BotonModerno : Control
        {
            private readonly string glifo;
            private readonly EstiloBoton estilo;
            private bool hover, presionado;

            public BotonModerno(string texto, string glifo, EstiloBoton estilo, int ancho)
            {
                this.glifo = glifo; this.estilo = estilo;
                Text = texto;
                Size = new Size(ancho, 44);
                Cursor = Cursors.Hand;
                SetStyle(EstiloPintado, true);
                SetStyle(ControlStyles.Selectable, false);
                TextChanged += (s, e) => Invalidate();
            }

            protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
            protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; presionado = false; Invalidate(); }
            protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); presionado = true; Invalidate(); }
            protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); presionado = false; Invalidate(); }
            protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                Tema.Calidad(g);
                g.Clear(Tema.FondoDe(this));
                if (Width < 30 || Height < 20) return;

                var r = new RectangleF(0.5F, 0.5F, Width - 2, Height - 2);
                Color colorTexto;
                int alfa = Enabled ? 255 : 90;

                using (var p = Tema.Redondo(r, 12))
                {
                    if (estilo == EstiloBoton.Primario)
                    {
                        Color a = Tema.AcentoOsc, b = Tema.Acento;
                        if (hover) { a = ControlPaint.Light(a, 0.15F); b = ControlPaint.Light(b, 0.15F); }
                        if (presionado) { a = ControlPaint.Dark(a, 0.1F); b = ControlPaint.Dark(b, 0.1F); }
                        using (var br = new LinearGradientBrush(r, Tema.Alfa(a, alfa), Tema.Alfa(b, alfa), LinearGradientMode.Horizontal))
                            g.FillPath(br, p);
                        colorTexto = Tema.Alfa(Tema.TextoSobreAcento, alfa);
                    }
                    else if (estilo == EstiloBoton.Peligro)
                    {
                        Color baseC = Tema.Peligro;
                        using (var br = new SolidBrush(Tema.Alfa(baseC, (hover ? 70 : 40) * alfa / 255)))
                            g.FillPath(br, p);
                        using (var pen = new Pen(Tema.Alfa(baseC, 130 * alfa / 255), 1F)) g.DrawPath(pen, p);
                        colorTexto = Tema.Alfa(Tema.Oscuro ? baseC : ControlPaint.Dark(baseC, 0.15F), alfa);
                    }
                    else
                    {
                        using (var br = new SolidBrush(Tema.Alfa(hover ? Tema.Borde : Tema.Elevado, alfa)))
                            g.FillPath(br, p);
                        using (var pen = new Pen(Tema.Alfa(Tema.Borde, alfa), 1F)) g.DrawPath(pen, p);
                        colorTexto = Tema.Alfa(Tema.Texto, alfa);
                    }
                }

                Font f = Tema.Fuente(9.25F, FontStyle.Bold);
                Font fg = Tema.Simbolo(10.5F);
                bool conGlifo = !string.IsNullOrEmpty(glifo);
                float wt = Tema.AnchoTexto(g, Text, f);
                float wg = conGlifo ? Tema.AnchoTexto(g, glifo, fg) + 6 : 0;
                float x = (Width - (wt + wg)) / 2f;
                if (conGlifo)
                    Tema.DibujarEn(g, glifo, fg, colorTexto, new RectangleF(x, 0, wg + 4, Height), StringAlignment.Near);
                Tema.DibujarEn(g, Text, f, colorTexto, new RectangleF(x + wg, 0, wt + 8, Height), StringAlignment.Near);
            }
        }

        // ───────────── Campo de texto redondeado ─────────────
        private sealed class CampoTexto : Control
        {
            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

            private readonly TextBox caja;
            private bool enfocado, error, lupa, soloDigitos;
            private string placeholder = "";

            public event EventHandler TextoCambiado;

            public string Texto { get { return caja.Text; } set { caja.Text = value ?? ""; } }
            public int MaxLength { get { return caja.MaxLength; } set { caja.MaxLength = value; } }
            public bool SoloDigitos { get { return soloDigitos; } set { soloDigitos = value; } }
            public bool Lupa { get { return lupa; } set { lupa = value; PerformLayout(); Invalidate(); } }
            public bool Error { get { return error; } set { error = value; Invalidate(); } }

            public string Placeholder
            {
                get { return placeholder; }
                set { placeholder = value ?? ""; AplicarPlaceholder(); }
            }

            public CampoTexto()
            {
                SetStyle(EstiloPintado, true);
                Height = 44;
                Cursor = Cursors.IBeam;

                caja = new TextBox { BorderStyle = BorderStyle.None, Font = Tema.Fuente(10F) };
                caja.HandleCreated += (s, e) => AplicarPlaceholder();
                caja.TextChanged += (s, e) =>
                {
                    if (error) { error = false; Invalidate(); }
                    var h = TextoCambiado;
                    if (h != null) h(this, EventArgs.Empty);
                };
                caja.KeyPress += (s, e) =>
                {
                    if (soloDigitos && !char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true;
                };
                caja.Enter += (s, e) => { enfocado = true; Invalidate(); };
                caja.Leave += (s, e) => { enfocado = false; Invalidate(); };
                Controls.Add(caja);
            }

            private void AplicarPlaceholder()
            {
                if (caja.IsHandleCreated) SendMessage(caja.Handle, 0x1501, (IntPtr)1, placeholder);   // EM_SETCUEBANNER
            }

            protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); caja.Focus(); }

            protected override void OnLayout(LayoutEventArgs levent)
            {
                base.OnLayout(levent);

                // AGREGA ESTA LÍNEA AQUÍ:
                if (caja == null) return;

                int izq = lupa ? 44 : 16;
                caja.SetBounds(izq, Math.Max(0, (Height - caja.Height) / 2), Math.Max(10, Width - izq - 16), caja.Height);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                Tema.Calidad(g);
                g.Clear(Tema.FondoDe(this));

                caja.BackColor = Tema.Elevado;
                caja.ForeColor = Tema.Texto;

                var r = new RectangleF(0.5F, 0.5F, Width - 2, Height - 2);
                using (var p = Tema.Redondo(r, 12))
                {
                    using (var b = new SolidBrush(Tema.Elevado)) g.FillPath(b, p);
                    Color borde = error ? Tema.Peligro : (enfocado ? Tema.AcentoTexto : Tema.Borde);
                    using (var pen = new Pen(borde, error || enfocado ? 1.6F : 1F)) g.DrawPath(pen, p);
                }

                if (lupa)
                {
                    float cy = Height / 2f;
                    using (var pen = new Pen(Tema.Suave, 1.8F) { EndCap = LineCap.Round })
                    {
                        g.DrawEllipse(pen, 17, cy - 8, 11, 11);
                        g.DrawLine(pen, 26, cy + 1, 31, cy + 6);
                    }
                }
            }
        }

        // ───────────── Selector (lista desplegable moderna) ─────────────
        private sealed class SelectorModerno : Control
        {
            private readonly List<object> items = new List<object>();
            private int indice = -1;
            private bool hover;
            private ToolStripDropDown popup;

            public event EventHandler CambioSeleccion;
            public string Placeholder = "Seleccionar";

            public IList<object> Elementos { get { return items; } }
            public object SelectedItem { get { return indice >= 0 && indice < items.Count ? items[indice] : null; } }

            public int SelectedIndex
            {
                get { return indice; }
                set
                {
                    indice = (value >= 0 && value < items.Count) ? value : -1;
                    Invalidate();
                    var h = CambioSeleccion;
                    if (h != null) h(this, EventArgs.Empty);
                }
            }

            public SelectorModerno()
            {
                SetStyle(EstiloPintado, true);
                Height = 44;
                Cursor = Cursors.Hand;
            }

            public void Agregar(object o) { items.Add(o); }
            public void LimpiarItems() { items.Clear(); indice = -1; }
            public int IndexOf(string texto) { return items.FindIndex(o => o.ToString() == texto); }

            protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
            protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }
            protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
            protected override void OnMouseClick(MouseEventArgs e) { base.OnMouseClick(e); MostrarLista(); }

            private void MostrarLista()
            {
                if (!Enabled || items.Count == 0) return;
                int resaltado = -1;

                var lista = new ListBox
                {
                    BorderStyle = BorderStyle.None,
                    DrawMode = DrawMode.OwnerDrawFixed,
                    ItemHeight = 38,
                    IntegralHeight = false,
                    BackColor = Tema.Elevado,
                    ForeColor = Tema.Texto
                };
                foreach (object o in items) lista.Items.Add(o);
                if (indice >= 0) lista.SelectedIndex = indice;

                lista.DrawItem += (s, ev) =>
                {
                    if (ev.Index < 0) return;
                    bool sel = ev.Index == indice;
                    Color fondoItem = ev.Index == resaltado ? Tema.Borde : Tema.Elevado;
                    using (var b = new SolidBrush(fondoItem)) ev.Graphics.FillRectangle(b, ev.Bounds);
                    TextRenderer.DrawText(ev.Graphics, lista.Items[ev.Index].ToString(),
                        Tema.Fuente(9.5F, sel ? FontStyle.Bold : FontStyle.Regular),
                        new Rectangle(ev.Bounds.X + 14, ev.Bounds.Y, ev.Bounds.Width - 44, ev.Bounds.Height),
                        sel ? Tema.AcentoTexto : Tema.Texto,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                    if (sel)
                        TextRenderer.DrawText(ev.Graphics, "✓", Tema.Simbolo(10F),
                            new Rectangle(ev.Bounds.Right - 30, ev.Bounds.Y, 24, ev.Bounds.Height),
                            Tema.AcentoTexto, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
                };
                lista.MouseMove += (s, ev) =>
                {
                    int i = lista.IndexFromPoint(ev.Location);
                    if (i != resaltado) { resaltado = i; lista.Invalidate(); }
                };
                lista.MouseClick += (s, ev) =>
                {
                    int i = lista.IndexFromPoint(ev.Location);
                    if (i < 0) return;
                    popup.Close();
                    SelectedIndex = i;
                };

                lista.Size = new Size(Width, Math.Min(items.Count, 7) * 38 + 2);
                var host = new ToolStripControlHost(lista)
                {
                    Margin = Padding.Empty,
                    Padding = Padding.Empty,
                    AutoSize = false,
                    Size = lista.Size
                };
                popup = new ToolStripDropDown
                {
                    Padding = Padding.Empty,
                    BackColor = Tema.Elevado,
                    AutoClose = true,
                    DropShadowEnabled = false
                };
                popup.Items.Add(host);
                popup.Show(this, new Point(0, Height + 4));
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                Tema.Calidad(g);
                g.Clear(Tema.FondoDe(this));
                int alfa = Enabled ? 255 : 100;

                var r = new RectangleF(0.5F, 0.5F, Width - 2, Height - 2);
                using (var p = Tema.Redondo(r, 12))
                {
                    using (var b = new SolidBrush(Tema.Alfa(Tema.Elevado, alfa))) g.FillPath(b, p);
                    using (var pen = new Pen(Tema.Alfa(hover && Enabled ? Tema.AcentoTexto : Tema.Borde, alfa), 1F)) g.DrawPath(pen, p);
                }

                object sel = SelectedItem;
                string texto = sel != null ? sel.ToString() : Placeholder;
                Tema.DibujarEn(g, texto, Tema.Fuente(9.5F), Tema.Alfa(sel != null ? Tema.Texto : Tema.Suave, alfa),
                    new RectangleF(16, 0, Width - 48, Height));

                float cx = Width - 22, cy = Height / 2f;
                using (var pen = new Pen(Tema.Alfa(Tema.Suave, alfa), 1.8F) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawLine(pen, cx - 4, cy - 2, cx, cy + 2);
                    g.DrawLine(pen, cx, cy + 2, cx + 4, cy - 2);
                }
            }
        }

        // ───────────── Interruptor Activo / Inactivo ─────────────
        private sealed class InterruptorModerno : Control
        {
            private readonly string textoOn, textoOff;
            private bool activo;

            public bool Activo { get { return activo; } set { activo = value; Invalidate(); } }

            public InterruptorModerno(string textoOn, string textoOff)
            {
                this.textoOn = textoOn; this.textoOff = textoOff;
                SetStyle(EstiloPintado, true);
                Height = 44;
                Cursor = Cursors.Hand;
            }

            protected override void OnMouseClick(MouseEventArgs e) { base.OnMouseClick(e); Activo = !activo; }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                Tema.Calidad(g);
                g.Clear(Tema.FondoDe(this));

                var pista = new RectangleF(2, (Height - 26) / 2f, 48, 26);
                using (var p = Tema.Redondo(pista, 13))
                using (var b = new SolidBrush(activo ? Tema.AcentoOsc : Tema.Borde))
                    g.FillPath(b, p);
                float d = 20;
                float x = activo ? pista.Right - d - 3 : pista.X + 3;
                g.FillEllipse(Brushes.White, x, pista.Y + 3, d, d);

                Tema.DibujarEn(g, activo ? textoOn : textoOff, Tema.Fuente(9.5F, FontStyle.Bold),
                    activo ? Tema.AcentoTexto : Tema.Suave, new RectangleF(62, 0, Width - 62, Height));
            }
        }

        // ───────────── Pestañas segmentadas ─────────────
        private sealed class PestanasModernas : Control
        {
            private string[] textos = new string[0], glifos = new string[0];
            private int indice, hover = -1;

            public event EventHandler CambioIndice;

            public int Indice
            {
                get { return indice; }
                set
                {
                    if (value == indice) return;
                    indice = value;
                    Invalidate();
                    var h = CambioIndice;
                    if (h != null) h(this, EventArgs.Empty);
                }
            }

            public PestanasModernas()
            {
                SetStyle(EstiloPintado, true);
                Height = 44;
                Cursor = Cursors.Hand;
            }

            public void SetTextos(string[] t, string[] g)
            {
                textos = t; glifos = g;
                Invalidate();
            }

            private int TabEn(Point p)
            {
                if (textos.Length == 0) return -1;
                float w = (Width - 8f) / textos.Length;
                int i = (int)((p.X - 4) / w);
                return (i >= 0 && i < textos.Length) ? i : -1;
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                int i = TabEn(e.Location);
                if (i != hover) { hover = i; Invalidate(); }
            }

            protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = -1; Invalidate(); }

            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                int i = TabEn(e.Location);
                if (i >= 0) Indice = i;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                Tema.Calidad(g);
                g.Clear(Tema.FondoDe(this));

                var caja = new RectangleF(0.5F, 0.5F, Width - 2, Height - 2);
                using (var p = Tema.Redondo(caja, 13))
                    using (var b = new SolidBrush(Tema.Elevado)) g.FillPath(b, p);

                int n = textos.Length;
                if (n == 0) return;
                float w = (Width - 8f) / n;
                for (int i = 0; i < n; i++)
                {
                    var r = new RectangleF(4 + i * w, 4, w - 2, Height - 8);
                    bool sel = i == indice;
                    if (sel)
                    {
                        using (var p = Tema.Redondo(r, 10))
                        {
                            using (var b = new SolidBrush(Tema.Superficie)) g.FillPath(b, p);
                            using (var pen = new Pen(Tema.Alfa(Tema.Acento, 170), 1.4F)) g.DrawPath(pen, p);
                        }
                    }
                    else if (i == hover)
                    {
                        using (var p = Tema.Redondo(r, 10))
                        using (var b = new SolidBrush(Tema.Alfa(Tema.Borde, 120))) g.FillPath(b, p);
                    }
                    string g0 = i < glifos.Length ? glifos[i] + "  " : "";
                    Tema.DibujarEn(g, g0 + textos[i], Tema.Fuente(9.25F, sel ? FontStyle.Bold : FontStyle.Regular),
                        sel ? Tema.Texto : Tema.Suave, r, StringAlignment.Center, StringAlignment.Center);
                }
            }
        }

        // ───────────── Aviso flotante (toast) ─────────────
        private sealed class Aviso : Control
        {
            private readonly System.Windows.Forms.Timer reloj;
            private string texto = "", glifo = "✓";
            private Color color = Tema.Verde;

            public Aviso()
            {
                SetStyle(EstiloPintado, true);
                Size = new Size(360, 56);
                Visible = false;
                reloj = new System.Windows.Forms.Timer { Interval = 3200 };
                reloj.Tick += (s, e) => { reloj.Stop(); Visible = false; };
            }

            public void Mostrar(string mensaje, bool exito)
            {
                texto = mensaje;
                color = exito ? Tema.Verde : Tema.Naranja;
                glifo = exito ? "✓" : "!";
                Visible = true;
                BringToFront();
                reloj.Stop();
                reloj.Start();
                Invalidate();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) reloj.Dispose();
                base.Dispose(disposing);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                Tema.Calidad(g);
                g.Clear(Tema.FondoDe(this));
                var r = new RectangleF(0.5F, 0.5F, Width - 2, Height - 2);
                using (var p = Tema.Redondo(r, 14))
                {
                    using (var b = new SolidBrush(Tema.Elevado)) g.FillPath(b, p);
                    using (var pen = new Pen(Tema.Alfa(color, 160), 1.2F)) g.DrawPath(pen, p);
                }
                var ic = new RectangleF(14, (Height - 30) / 2f, 30, 30);
                using (var b = new SolidBrush(Tema.Alfa(color, 50))) g.FillEllipse(b, ic);
                Tema.DibujarEn(g, glifo, Tema.Fuente(11F, FontStyle.Bold), color, ic, StringAlignment.Center, StringAlignment.Center);
                Tema.DibujarEn(g, texto, Tema.Fuente(9.5F, FontStyle.Bold), Tema.Texto,
                    new RectangleF(56, 0, Width - 70, Height));
            }
        }

        // ───────────── Ficha de persona (detalles) ─────────────
        private sealed class FichaPersona : Control
        {
            public string Nombre = "", Subtitulo = "", Rol = "";
            public bool Activo;
            public List<KeyValuePair<string, string>> Datos = new List<KeyValuePair<string, string>>();

            public FichaPersona() { SetStyle(EstiloPintado, true); }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                Tema.Calidad(g);
                g.Clear(Tema.FondoDe(this));

                // Avatar y nombre
                var av = new RectangleF(0, 6, 66, 66);
                using (var b = new SolidBrush(Tema.ColorAvatar(Nombre))) g.FillEllipse(b, av);
                Tema.DibujarEn(g, Tema.Iniciales(Nombre), Tema.Fuente(17F, FontStyle.Bold), Color.White, av,
                    StringAlignment.Center, StringAlignment.Center);
                Tema.DibujarEn(g, Nombre, Tema.Fuente(16F, FontStyle.Bold), Tema.Texto, new RectangleF(82, 6, Width - 82, 30));
                Tema.DibujarEn(g, Subtitulo, Tema.Fuente(9.5F), Tema.Suave, new RectangleF(82, 36, Width - 82, 20));

                // Insignias
                float bx = 82;
                string est = Activo ? "Activo" : "Inactivo";
                float w1 = Tema.AnchoTexto(g, est, Tema.Fuente(8F, FontStyle.Bold)) + 40;
                Tema.Insignia(g, est, Activo ? Tema.Verde : Tema.Peligro, new RectangleF(bx, 54, w1, 24), true);
                bx += w1 + 8;
                if (!string.IsNullOrEmpty(Rol) && Rol != "—")
                {
                    float w2 = Tema.AnchoTexto(g, Rol, Tema.Fuente(8F, FontStyle.Bold)) + 30;
                    Tema.Insignia(g, Rol, Tema.ColorRol(Rol), new RectangleF(bx, 54, w2, 24), false);
                }

                // Datos en dos columnas
                float y0 = 104;
                float mitad = Width / 2f;
                for (int i = 0; i < Datos.Count; i++)
                {
                    float x = (i % 2) * mitad;
                    float y = y0 + (i / 2) * 58;
                    Tema.Dibujar(g, Datos[i].Key.ToUpper(), Tema.Fuente(7.75F, FontStyle.Bold), Tema.Suave, x, y);
                    string v = string.IsNullOrWhiteSpace(Datos[i].Value) ? "—" : Datos[i].Value;
                    Tema.DibujarEn(g, v, Tema.Fuente(10.5F, FontStyle.Bold), Tema.Texto,
                        new RectangleF(x, y + 16, mitad - 18, 26));
                }

                // Historial (estado vacío)
                float yh = y0 + ((Datos.Count + 1) / 2) * 58 + 4;
                var caja = new RectangleF(0, yh, Width - 1, Math.Max(0, Height - yh - 2));
                if (caja.Height < 60) return;
                using (var p = Tema.Redondo(caja, 14))
                {
                    using (var b = new SolidBrush(Tema.Elevado)) g.FillPath(b, p);
                    using (var pen = new Pen(Tema.Borde, 1F)) g.DrawPath(pen, p);
                }
                Tema.Dibujar(g, "HISTORIAL DE MOVIMIENTOS", Tema.Fuente(7.75F, FontStyle.Bold), Tema.Suave, 18, yh + 14);
                Tema.DibujarEn(g, "Aún no hay movimientos vinculados a este registro.", Tema.Fuente(9.5F, FontStyle.Bold), Tema.Texto,
                    new RectangleF(18, yh + 36, Width - 36, 22));
                Tema.DibujarEn(g, "Los retiros y devoluciones de equipos aparecerán aquí cuando se integre el módulo Movimientos.",
                    Tema.Fuente(8.75F), Tema.Suave, new RectangleF(18, yh + 58, Width - 36, 22));
            }
        }

        // ───────────── Ventana modal moderna ─────────────
        private sealed class DialogoBase : Form
        {
            [DllImport("user32.dll")]
            private static extern bool ReleaseCapture();
            [DllImport("user32.dll")]
            private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

            public readonly Panel Cuerpo = new Panel();
            public readonly FlowLayoutPanel Pie = new FlowLayoutPanel();
            public readonly Label MensajeError = new Label();

            private readonly Panel cabecera = new Panel();
            private readonly Panel barraPie = new Panel();
            private readonly string titulo, subtitulo, glifo;
            private readonly Color acento;

            public DialogoBase(string titulo, string subtitulo, string glifo, Size cliente, Color acento)
            {
                this.titulo = titulo; this.subtitulo = subtitulo; this.glifo = glifo; this.acento = acento;

                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.CenterParent;
                ShowInTaskbar = false;
                ClientSize = cliente;
                BackColor = Tema.Superficie;
                Padding = new Padding(1);
                DoubleBuffered = true;
                Font = Tema.Fuente(9F);

                Cuerpo.Dock = DockStyle.Fill;
                Cuerpo.Padding = new Padding(26, 6, 26, 6);

                cabecera.Dock = DockStyle.Top;
                cabecera.Height = 88;
                cabecera.Paint += PintarCabecera;
                cabecera.MouseDown += Arrastrar;

                barraPie.Dock = DockStyle.Bottom;
                barraPie.Height = 78;
                barraPie.Padding = new Padding(26, 16, 26, 16);
                barraPie.Paint += (s, e) =>
                {
                    using (var pen = new Pen(Tema.Borde, 1F)) e.Graphics.DrawLine(pen, 0, 0, barraPie.Width, 0);
                };

                Pie.Dock = DockStyle.Right;
                Pie.FlowDirection = FlowDirection.RightToLeft;
                Pie.WrapContents = false;
                Pie.AutoSize = true;
                Pie.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                Pie.Margin = Padding.Empty;

                MensajeError.Dock = DockStyle.Fill;
                MensajeError.ForeColor = Tema.Peligro;
                MensajeError.Font = Tema.Fuente(9F, FontStyle.Bold);
                MensajeError.TextAlign = ContentAlignment.MiddleLeft;

                // Orden de agregado: Fill primero, luego los bordes
                barraPie.Controls.Add(MensajeError);
                barraPie.Controls.Add(Pie);
                Controls.Add(Cuerpo);
                Controls.Add(cabecera);
                Controls.Add(barraPie);

                var cerrar = new Label
                {
                    Text = "✕",
                    AutoSize = false,
                    Size = new Size(34, 34),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand,
                    ForeColor = Tema.Suave,
                    Font = Tema.Simbolo(11F),
                    BackColor = Color.Transparent
                };
                cerrar.MouseEnter += (s, e) => cerrar.ForeColor = Tema.Peligro;
                cerrar.MouseLeave += (s, e) => cerrar.ForeColor = Tema.Suave;
                cerrar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
                cabecera.Controls.Add(cerrar);
                cabecera.Resize += (s, e) => cerrar.Location = new Point(cabecera.Width - 34 - 18, 18);
            }

            private void PintarCabecera(object sender, PaintEventArgs e)
            {
                var g = e.Graphics;
                Tema.Calidad(g);
                var ic = new RectangleF(26, 22, 46, 46);
                using (var p = Tema.Redondo(ic, 13))
                using (var b = new SolidBrush(Tema.Alfa(acento, 42)))
                    g.FillPath(b, p);
                Color cg = acento == Tema.Acento ? Tema.AcentoTexto : acento;
                Tema.DibujarEn(g, glifo, Tema.Simbolo(16F), cg, ic, StringAlignment.Center, StringAlignment.Center);

                bool sinSub = string.IsNullOrEmpty(subtitulo);
                Tema.DibujarEn(g, titulo, Tema.Fuente(14F, FontStyle.Bold), Tema.Texto,
                    new RectangleF(86, sinSub ? 30 : 20, cabecera.Width - 150, 28));
                if (!sinSub)
                    Tema.DibujarEn(g, subtitulo, Tema.Fuente(9F), Tema.Suave, new RectangleF(86, 48, cabecera.Width - 150, 22));
                using (var pen = new Pen(Tema.Borde, 1F))
                    g.DrawLine(pen, 0, cabecera.Height - 1, cabecera.Width, cabecera.Height - 1);
            }

            private void Arrastrar(object sender, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.Left) return;
                ReleaseCapture();
                SendMessage(Handle, 0xA1, 2, 0);   // WM_NCLBUTTONDOWN + HTCAPTION
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                using (var pen = new Pen(Tema.Borde, 1F))
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }

            protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
            {
                if (keyData == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); return true; }
                return base.ProcessCmdKey(ref msg, keyData);
            }
        }
    }
}