using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Npgsql;
using NpgsqlTypes;

// Se eliminó la clase exterior que causaba el conflicto.
// El nombre de la clase ahora es diferente al del namespace.
namespace SUSTENTACION.PanelSolicitudesEquipo
{
    public class PanelSolicitudesControl : UserControl
    {
        /// <summary>
        /// Panel de administración: SOLICITUDES DE EQUIPO.
        /// Flujo:  Pendiente → Aprobada / Rechazada → Entregada.
        /// Incluye: formulario de nueva solicitud, KPIs, filtros, grilla coloreada,
        /// detalle completo, control de stock (opcional), exportación a CSV.
        /// </summary>
        // =================================================================
        //  CONFIGURACIÓN  (ajusta estos nombres a tu base de datos)
        // =================================================================
        // Tabla de inventario usada para el catálogo y el control de stock.
        // Si la tabla/columnas no existen, el panel funciona igual (sin stock).
        private const string TablaInventario = "public.equipos";
        private const string ColInvNombre = "nombre";
        private const string ColInvCategoria = "categoria";
        private const string ColInvStock = "stock";

        // true = al marcar "Entregada" se descuenta la cantidad del stock.
        private const bool DescontarStockAlEntregar = true;

        // Script de la tabla (se crea sola si no existe y tienes permisos).
        private const string SqlCrearTabla = @"
CREATE TABLE IF NOT EXISTS public.solicitudes_equipo (
    id_solicitud     SERIAL PRIMARY KEY,
    id_trabajador    INTEGER      NOT NULL,
    categoria        VARCHAR(60),
    equipo           VARCHAR(150) NOT NULL,
    cantidad         INTEGER      NOT NULL DEFAULT 1 CHECK (cantidad > 0),
    prioridad        VARCHAR(20)  NOT NULL DEFAULT 'Media',
    tipo_entrega     VARCHAR(40),
    area_destino     VARCHAR(120),
    fecha_requerida  DATE,
    motivo           TEXT,
    estado           VARCHAR(20)  NOT NULL DEFAULT 'Pendiente',
    fecha_solicitud  TIMESTAMP    NOT NULL DEFAULT now(),
    respuesta_admin  TEXT,
    id_admin         INTEGER,
    fecha_resolucion TIMESTAMP,
    fecha_entrega    TIMESTAMP
);";

        // =================================================================
        //  Paleta
        // =================================================================
        private readonly Color fondo = Color.FromArgb(243, 246, 250);
        private readonly Color tarjeta = Color.White;
        private readonly Color verde = Color.FromArgb(19, 125, 91);
        private readonly Color texto = Color.FromArgb(26, 39, 54);
        private readonly Color tenue = Color.FromArgb(106, 122, 140);
        private readonly Color linea = Color.FromArgb(226, 232, 239);
        private readonly Color rojo = Color.FromArgb(178, 45, 45);
        private readonly Color ambar = Color.FromArgb(190, 130, 30);
        private readonly Color azul = Color.FromArgb(28, 107, 185);
        private readonly Color morado = Color.FromArgb(110, 80, 170);

        // =================================================================
        //  Tipos auxiliares
        // =================================================================
        private class Seccion
        {
            public Panel Panel, Cabecera, Contenido;
            public Label Flecha;
            public bool Expandido;
            public int AlturaContenido;
        }

        private class Item
        {
            public int Id;
            public string Texto;
            public override string ToString() { return Texto; }
        }

        private class EquipoInv
        {
            public string Nombre;
            public string Categoria;
            public int Stock;
        }

        private enum Accion { Aprobar, Rechazar, Entregar }

        // =================================================================
        //  Controles
        // =================================================================
        private Panel panelScroll, encabezado;
        private TableLayoutPanel kpis;
        private readonly List<Seccion> secciones = new List<Seccion>();
        private Seccion secForm, secLista, secDetalle;
        private bool reacomodando;

        private Label lblConexion;
        private Label lblPendientes, lblAprobadas, lblEntregadas, lblRechazadas, lblTotal, lblUrgentes;

        // Formulario
        private ComboBox cmbSolicitante, cmbCategoria, cmbEquipo, cmbPrioridad, cmbTipoEntrega;
        private NumericUpDown nudCantidad;
        private DateTimePicker dtpRequerida;
        private TextBox txtArea, txtStock, txtMotivo;
        private Button btnRegistrar, btnLimpiar;

        // Lista
        private TextBox txtBuscar;
        private ComboBox cmbEstadoFiltro, cmbPrioridadFiltro;
        private Label lblContador;
        private DataGridView gridSolicitudes;
        private DataTable datosSolicitudes = new DataTable();
        private Button btnAprobar, btnRechazar, btnEntregar, btnExportar, btnActualizar;

        // Detalle
        private readonly Dictionary<string, Label> detalle = new Dictionary<string, Label>();

        // Estado interno
        private readonly List<EquipoInv> catalogo = new List<EquipoInv>();
        private bool inventarioDisponible;
        private bool esquemaVerificado;
        private bool cargando;
        private readonly Font fuenteNegrita = new Font("Segoe UI", 8.5F, FontStyle.Bold);

        private static readonly string[] CategoriasBase =
            { "Cómputo", "Periféricos", "Cámaras", "Audio", "Iluminación", "Redes", "Herramientas", "Otros" };

        // =================================================================
        //  Constructor
        // =================================================================
        public PanelSolicitudesControl()
        {
            Dock = DockStyle.Fill;
            BackColor = fondo;
            Font = new Font("Segoe UI", 9F);
            DoubleBuffered = true;
            ConstruirInterfaz();
            Load += (s, e) =>
            {
                if (!EsUsuarioAdministrador()) { BloquearAcceso(); return; }
                Reacomodar();
                CargarTodo(true);
            };
        }

        // ---------- Sesión (conecta aquí tu usuario logueado) ----------
        private string ObtenerRolActual()
        {
            try { return "Admin"; }
            catch { return "Admin"; }
        }

        private int ObtenerIdAdminActual()
        {
            try { return 0; }
            catch { return 0; }
        }

        private bool EsUsuarioAdministrador()
        {
            string rol = ObtenerRolActual();
            return rol == "Admin" || rol == "SuperAdmin";
        }

        private void BloquearAcceso()
        {
            Controls.Clear();
            Controls.Add(new Label
            {
                Text = "Acceso denegado.\nEsta sección es solo para administradores.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Firebrick,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold)
            });
        }

        // =================================================================
        //  INTERFAZ
        // =================================================================
        private void ConstruirInterfaz()
        {
            panelScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = fondo };
            Controls.Add(panelScroll);

            encabezado = ConstruirEncabezado();
            panelScroll.Controls.Add(encabezado);

            kpis = ConstruirKpis();
            panelScroll.Controls.Add(kpis);

            secForm = ConstruirSeccion("📝  NUEVA SOLICITUD DE EQUIPO", true, 380);
            ConstruirFormulario(secForm.Contenido);

            secLista = ConstruirSeccion("📦  SOLICITUDES DE EQUIPO", true, 450);
            ConstruirLista(secLista.Contenido);

            secDetalle = ConstruirSeccion("🔍  DETALLE DE LA SOLICITUD", true, 400);
            ConstruirDetalle(secDetalle.Contenido);

            secciones.Add(secForm);
            secciones.Add(secLista);
            secciones.Add(secDetalle);
            foreach (var sec in secciones) panelScroll.Controls.Add(sec.Panel);

            panelScroll.Resize += (s, e) => Reacomodar();
            Reacomodar();
        }

        private void Reacomodar()
        {
            if (panelScroll == null || reacomodando) return;
            reacomodando = true;
            try
            {
                const int margen = 22, hueco = 12;
                int ancho = Math.Max(760, panelScroll.Width - SystemInformation.VerticalScrollBarWidth - margen * 2);
                int offset = panelScroll.AutoScrollPosition.Y;
                int y = 14;

                encabezado.SetBounds(margen, y + offset, ancho, 88);
                y += 88 + 6;

                kpis.SetBounds(margen, y + offset, ancho, 104);
                y += 104 + 14;

                foreach (var sec in secciones)
                {
                    int alto = 42 + (sec.Expandido ? sec.AlturaContenido : 0);
                    sec.Panel.SetBounds(margen, y + offset, ancho, alto);
                    y += alto + hueco;
                }
                y += 20;
                panelScroll.AutoScrollMinSize = new Size(ancho + margen * 2, y);
            }
            finally { reacomodando = false; }
        }

        // -----------------------------------------------------------------
        //  Encabezado
        // -----------------------------------------------------------------
        private Panel ConstruirEncabezado()
        {
            var p = new Panel { BackColor = fondo };
            p.Controls.Add(new Label
            {
                Text = "ARTEMUSA TV / ADMINISTRACIÓN",
                AutoSize = true,
                ForeColor = verde,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Location = new Point(2, 0)
            });
            p.Controls.Add(new Label
            {
                Text = "Solicitudes de equipo",
                AutoSize = true,
                ForeColor = texto,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                Location = new Point(0, 23)
            });
            p.Controls.Add(new Label
            {
                Text = "Registra pedidos de equipo, revisa el stock y aprueba, rechaza o entrega cada solicitud.",
                AutoSize = true,
                ForeColor = tenue,
                Location = new Point(2, 62)
            });
            lblConexion = new Label
            {
                Text = "● Verificando conexión",
                AutoSize = true,
                ForeColor = tenue,
                Location = new Point(600, 5)
            };
            p.Controls.Add(lblConexion);
            p.Resize += (s, e) => PosicionarConexion();
            return p;
        }

        private void PosicionarConexion()
        {
            if (lblConexion == null || encabezado == null) return;
            lblConexion.Location = new Point(Math.Max(0, encabezado.ClientSize.Width - lblConexion.Width), 5);
        }

        private void SetConexion(string textoEstado, Color color)
        {
            lblConexion.Text = textoEstado;
            lblConexion.ForeColor = color;
            PosicionarConexion();
        }

        // -----------------------------------------------------------------
        //  KPIs
        // -----------------------------------------------------------------
        private TableLayoutPanel ConstruirKpis()
        {
            var t = new TableLayoutPanel
            {
                ColumnCount = 5,
                RowCount = 1,
                BackColor = fondo
            };
            for (int i = 0; i < 5; i++) t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            lblPendientes = AgregarKpi(t, 0, "PENDIENTES", ambar, out lblUrgentes);
            Label sub;
            lblAprobadas = AgregarKpi(t, 1, "APROBADAS", azul, out sub);
            lblEntregadas = AgregarKpi(t, 2, "ENTREGADAS", verde, out sub);
            lblRechazadas = AgregarKpi(t, 3, "RECHAZADAS", rojo, out sub);
            lblTotal = AgregarKpi(t, 4, "TOTAL", morado, out sub);
            return t;
        }

        private Label AgregarKpi(TableLayoutPanel panel, int columna, string titulo, Color color, out Label subtitulo)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = tarjeta, Margin = new Padding(0, 0, 10, 0) };
            card.Controls.Add(new Label
            {
                Text = titulo,
                AutoSize = true,
                ForeColor = tenue,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Location = new Point(13, 12)
            });
            var lbl = new Label
            {
                Text = "0",
                AutoSize = true,
                ForeColor = texto,
                Font = new Font("Segoe UI", 21F, FontStyle.Bold),
                Location = new Point(12, 30)
            };
            card.Controls.Add(lbl);
            subtitulo = new Label
            {
                Text = "",
                AutoSize = true,
                ForeColor = tenue,
                Font = new Font("Segoe UI", 8F),
                Location = new Point(14, 74)
            };
            card.Controls.Add(subtitulo);
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(color, 3))
                    e.Graphics.DrawLine(pen, 12, card.Height - 3, card.Width - 12, card.Height - 3);
            };
            panel.Controls.Add(card, columna, 0);
            return lbl;
        }

        // -----------------------------------------------------------------
        //  Sección desplegable
        // -----------------------------------------------------------------
        private Seccion ConstruirSeccion(string titulo, bool expandido, int alturaContenido)
        {
            var sec = new Seccion { Expandido = expandido, AlturaContenido = alturaContenido };

            sec.Panel = new Panel { BackColor = tarjeta };
            sec.Cabecera = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.FromArgb(248, 250, 252),
                Cursor = Cursors.Hand
            };
            sec.Flecha = new Label
            {
                Text = expandido ? "▾" : "▸",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = verde,
                AutoSize = true,
                Location = new Point(12, 9)
            };
            var lblTitulo = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = texto,
                AutoSize = true,
                Location = new Point(36, 13)
            };
            sec.Cabecera.Controls.Add(sec.Flecha);
            sec.Cabecera.Controls.Add(lblTitulo);
            sec.Cabecera.Paint += (s, e) =>
            {
                using (var pen = new Pen(linea, 1))
                    e.Graphics.DrawLine(pen, 0, sec.Cabecera.Height - 1, sec.Cabecera.Width, sec.Cabecera.Height - 1);
            };

            sec.Contenido = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = tarjeta,
                Padding = new Padding(14, 12, 14, 12),
                Visible = expandido
            };

            EventHandler alternar = (s, e) =>
            {
                sec.Expandido = !sec.Expandido;
                sec.Contenido.Visible = sec.Expandido;
                sec.Flecha.Text = sec.Expandido ? "▾" : "▸";
                Reacomodar();
            };
            sec.Cabecera.Click += alternar;
            sec.Flecha.Click += alternar;
            lblTitulo.Click += alternar;

            sec.Panel.Controls.Add(sec.Contenido);
            sec.Panel.Controls.Add(sec.Cabecera);
            sec.Contenido.BringToFront();

            sec.Panel.Paint += (s, e) =>
            {
                using (var pen = new Pen(linea, 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, sec.Panel.Width - 1, sec.Panel.Height - 1);
            };
            return sec;
        }

        // =================================================================
        //  FORMULARIO: NUEVA SOLICITUD
        // =================================================================
        private void ConstruirFormulario(Panel cont)
        {
            var t = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 5,
                BackColor = tarjeta
            };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

            var fuenteInput = new Font("Segoe UI", 9.5F);

            // Fila 0
            cmbSolicitante = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                Font = fuenteInput
            };
            cmbCategoria = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = fuenteInput };
            cmbCategoria.SelectedIndexChanged += (s, e) => LlenarEquipos();
            cmbEquipo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                Font = fuenteInput
            };
            cmbEquipo.SelectedIndexChanged += (s, e) => ActualizarStock();
            cmbEquipo.TextChanged += (s, e) => ActualizarStock();

            t.Controls.Add(Campo("Solicitante (nombre o DNI) *", cmbSolicitante), 0, 0);
            t.Controls.Add(Campo("Categoría *", cmbCategoria), 1, 0);
            t.Controls.Add(Campo("Equipo solicitado *", cmbEquipo), 2, 0);

            // Fila 1
            nudCantidad = new NumericUpDown { Minimum = 1, Maximum = 999, Value = 1, Font = fuenteInput };
            nudCantidad.ValueChanged += (s, e) => ActualizarStock();
            cmbPrioridad = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = fuenteInput };
            cmbPrioridad.Items.AddRange(new object[] { "Baja", "Media", "Alta", "Urgente" });
            cmbPrioridad.SelectedIndex = 1;
            dtpRequerida = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                MinDate = DateTime.Today,
                Value = DateTime.Today.AddDays(3),
                Font = fuenteInput
            };
            t.Controls.Add(Campo("Cantidad *", nudCantidad), 0, 1);
            t.Controls.Add(Campo("Prioridad *", cmbPrioridad), 1, 1);
            t.Controls.Add(Campo("Fecha requerida *", dtpRequerida), 2, 1);

            // Fila 2
            txtArea = new TextBox { Font = fuenteInput };
            txtArea.PlaceholderText = "Ej: Estudio 2, Edición, Administración";
            cmbTipoEntrega = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = fuenteInput };
            cmbTipoEntrega.Items.AddRange(new object[] { "Asignación permanente", "Préstamo temporal", "Reemplazo" });
            cmbTipoEntrega.SelectedIndex = 0;
            txtStock = new TextBox
            {
                ReadOnly = true,
                Font = fuenteInput,
                Text = "—",
                BackColor = Color.FromArgb(248, 250, 252),
                TabStop = false
            };
            t.Controls.Add(Campo("Área / ubicación destino", txtArea), 0, 2);
            t.Controls.Add(Campo("Tipo de entrega", cmbTipoEntrega), 1, 2);
            t.Controls.Add(Campo("Stock disponible", txtStock), 2, 2);

            // Fila 3: motivo
            txtMotivo = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = fuenteInput
            };
            txtMotivo.PlaceholderText = "Explica para qué se necesita el equipo (mínimo 5 caracteres).";
            var campoMotivo = Campo("Motivo / justificación *", txtMotivo, true);
            campoMotivo.Margin = new Padding(0);
            t.Controls.Add(campoMotivo, 0, 3);
            t.SetColumnSpan(campoMotivo, 3);

            // Fila 4: botones
            var flujo = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = tarjeta,
                Padding = new Padding(0, 8, 0, 0)
            };
            btnRegistrar = Boton("＋  Registrar solicitud", 180, verde, Color.White);
            btnRegistrar.Click += (s, e) => RegistrarSolicitud();
            btnLimpiar = Boton("Limpiar", 100, Color.FromArgb(230, 236, 242), texto);
            btnLimpiar.Click += (s, e) => LimpiarFormulario();
            flujo.Controls.Add(btnRegistrar);
            flujo.Controls.Add(btnLimpiar);
            t.Controls.Add(flujo, 0, 4);
            t.SetColumnSpan(flujo, 3);

            cont.Controls.Add(t);
            LlenarCategorias();
        }

        private Panel Campo(string etiqueta, Control ctl, bool llenar = false)
        {
            var p = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 14, 0), BackColor = tarjeta };
            ctl.Dock = llenar ? DockStyle.Fill : DockStyle.Top;
            p.Controls.Add(ctl);
            p.Controls.Add(new Label
            {
                Text = etiqueta.ToUpperInvariant(),
                Dock = DockStyle.Top,
                Height = 20,
                ForeColor = tenue,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold)
            });
            ctl.BringToFront();
            return p;
        }

        private Button Boton(string textoBoton, int ancho, Color fondoBtn, Color colorTexto)
        {
            var b = new Button
            {
                Text = textoBoton,
                Size = new Size(ancho, 32),
                BackColor = fondoBtn,
                ForeColor = colorTexto,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private void LlenarCategorias()
        {
            string actual = Convert.ToString(cmbCategoria.SelectedItem);
            var lista = new List<string>(CategoriasBase);
            foreach (var eq in catalogo)
                if (!string.IsNullOrWhiteSpace(eq.Categoria) && !lista.Contains(eq.Categoria))
                    lista.Add(eq.Categoria);

            cmbCategoria.Items.Clear();
            cmbCategoria.Items.AddRange(lista.ToArray());
            int idx = string.IsNullOrEmpty(actual) ? 0 : cmbCategoria.Items.IndexOf(actual);
            cmbCategoria.SelectedIndex = idx >= 0 ? idx : 0;
        }

        private void LlenarEquipos()
        {
            string cat = Convert.ToString(cmbCategoria.SelectedItem);
            cmbEquipo.Items.Clear();
            foreach (var eq in catalogo)
                if (string.Equals(eq.Categoria, cat, StringComparison.OrdinalIgnoreCase))
                    cmbEquipo.Items.Add(eq.Nombre);
            cmbEquipo.Text = "";
            ActualizarStock();
        }

        private EquipoInv BuscarEquipo(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return null;
            foreach (var eq in catalogo)
                if (string.Equals(eq.Nombre, nombre.Trim(), StringComparison.OrdinalIgnoreCase))
                    return eq;
            return null;
        }

        private void ActualizarStock()
        {
            if (txtStock == null) return;
            if (!inventarioDisponible)
            {
                txtStock.Text = "Inventario no conectado";
                txtStock.ForeColor = tenue;
                return;
            }
            var eq = BuscarEquipo(cmbEquipo.Text);
            if (eq == null)
            {
                txtStock.Text = string.IsNullOrWhiteSpace(cmbEquipo.Text) ? "—" : "No está en el inventario";
                txtStock.ForeColor = tenue;
                return;
            }
            txtStock.Text = eq.Stock + " disponible(s)";
            txtStock.ForeColor = eq.Stock >= (int)nudCantidad.Value && eq.Stock > 0 ? verde : rojo;
        }

        private Item ObtenerSolicitanteSeleccionado()
        {
            var it = cmbSolicitante.SelectedItem as Item;
            if (it != null && string.Equals(it.Texto, cmbSolicitante.Text, StringComparison.OrdinalIgnoreCase))
                return it;
            foreach (object obj in cmbSolicitante.Items)
            {
                var x = obj as Item;
                if (x != null && string.Equals(x.Texto, cmbSolicitante.Text.Trim(), StringComparison.OrdinalIgnoreCase))
                    return x;
            }
            return null;
        }

        private void LimpiarFormulario()
        {
            cmbSolicitante.SelectedIndex = -1;
            cmbSolicitante.Text = "";
            cmbEquipo.Text = "";
            nudCantidad.Value = 1;
            cmbPrioridad.SelectedIndex = 1;
            dtpRequerida.Value = DateTime.Today.AddDays(3);
            txtArea.Clear();
            cmbTipoEntrega.SelectedIndex = 0;
            txtMotivo.Clear();
            ActualizarStock();
        }

        private void RegistrarSolicitud()
        {
            var solicitante = ObtenerSolicitanteSeleccionado();
            if (solicitante == null) { Aviso("Selecciona un solicitante de la lista."); cmbSolicitante.Focus(); return; }

            string equipo = cmbEquipo.Text.Trim();
            if (equipo.Length == 0) { Aviso("Indica el equipo solicitado."); cmbEquipo.Focus(); return; }

            int cantidad = (int)nudCantidad.Value;
            string motivo = txtMotivo.Text.Trim();
            if (motivo.Length < 5) { Aviso("Escribe el motivo de la solicitud (mínimo 5 caracteres)."); txtMotivo.Focus(); return; }

            if (dtpRequerida.Value.Date < DateTime.Today) { Aviso("La fecha requerida no puede ser anterior a hoy."); return; }

            var inv = BuscarEquipo(equipo);
            if (inventarioDisponible && inv != null && inv.Stock < cantidad)
            {
                var r = MessageBox.Show(this,
                    "El stock actual de \"" + inv.Nombre + "\" es " + inv.Stock + " y se solicitan " + cantidad + ".\r\n\r\n¿Registrar la solicitud de todos modos?",
                    "Stock insuficiente", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r != DialogResult.Yes) return;
            }

            string categoria = inv != null ? inv.Categoria : Convert.ToString(cmbCategoria.SelectedItem);

            try
            {
                using (var conexion = AbrirConexion())
                {
                    AsegurarEsquema(conexion);
                    using (var cmd = new NpgsqlCommand(@"
INSERT INTO public.solicitudes_equipo
    (id_trabajador, categoria, equipo, cantidad, prioridad, tipo_entrega,
     area_destino, fecha_requerida, motivo, estado, fecha_solicitud)
VALUES
    (@trab, @cat, @equipo, @cant, @prio, @tipo, @area, @freq, @motivo, 'Pendiente', now());", conexion))
                    {
                        cmd.Parameters.Add("@trab", NpgsqlDbType.Integer).Value = solicitante.Id;
                        cmd.Parameters.Add("@cat", NpgsqlDbType.Varchar).Value = (object)categoria ?? DBNull.Value;
                        cmd.Parameters.Add("@equipo", NpgsqlDbType.Varchar).Value = equipo;
                        cmd.Parameters.Add("@cant", NpgsqlDbType.Integer).Value = cantidad;
                        cmd.Parameters.Add("@prio", NpgsqlDbType.Varchar).Value = Convert.ToString(cmbPrioridad.SelectedItem);
                        cmd.Parameters.Add("@tipo", NpgsqlDbType.Varchar).Value = Convert.ToString(cmbTipoEntrega.SelectedItem);
                        cmd.Parameters.Add("@area", NpgsqlDbType.Varchar).Value =
                            string.IsNullOrWhiteSpace(txtArea.Text) ? (object)DBNull.Value : txtArea.Text.Trim();
                        cmd.Parameters.Add("@freq", NpgsqlDbType.Date).Value = dtpRequerida.Value.Date;
                        cmd.Parameters.Add("@motivo", NpgsqlDbType.Text).Value = motivo;
                        cmd.ExecuteNonQuery();
                    }
                }
                LimpiarFormulario();
                CargarTodo(false);
                MessageBox.Show(this, "Solicitud de equipo registrada correctamente.",
                    "Solicitudes de equipo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "No se pudo registrar la solicitud.\r\n\r\n" + ex.Message,
                    "Solicitudes de equipo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =================================================================
        //  LISTA DE SOLICITUDES
        // =================================================================
        private void ConstruirLista(Panel cont)
        {
            // Grilla (Fill) primero, luego el panel de filtros (Top)
            gridSolicitudes = new DataGridView { Dock = DockStyle.Fill };
            EstilizarGrid(gridSolicitudes, 36);
            gridSolicitudes.SelectionChanged += (s, e) =>
            {
                ActualizarBotones();
                ActualizarDetalle();
            };
            gridSolicitudes.CellFormatting += FormatearCeldas;
            cont.Controls.Add(gridSolicitudes);

            var filtros = new Panel { Dock = DockStyle.Top, Height = 108, BackColor = tarjeta };
            cont.Controls.Add(filtros);
            gridSolicitudes.BringToFront();

            filtros.Controls.Add(new Label { Text = "Buscar", AutoSize = true, ForeColor = tenue, Location = new Point(0, 2) });
            txtBuscar = new TextBox { Location = new Point(0, 22), Width = 260 };
            txtBuscar.PlaceholderText = "Solicitante, equipo, área o motivo";
            txtBuscar.TextChanged += (s, e) => AplicarFiltros();
            filtros.Controls.Add(txtBuscar);

            filtros.Controls.Add(new Label { Text = "Estado", AutoSize = true, ForeColor = tenue, Location = new Point(274, 2) });
            cmbEstadoFiltro = new ComboBox { Location = new Point(274, 22), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbEstadoFiltro.Items.AddRange(new object[] { "Todos", "Pendiente", "Aprobada", "Entregada", "Rechazada" });
            cmbEstadoFiltro.SelectedIndex = 0;
            cmbEstadoFiltro.SelectedIndexChanged += (s, e) => AplicarFiltros();
            filtros.Controls.Add(cmbEstadoFiltro);

            filtros.Controls.Add(new Label { Text = "Prioridad", AutoSize = true, ForeColor = tenue, Location = new Point(438, 2) });
            cmbPrioridadFiltro = new ComboBox { Location = new Point(438, 22), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbPrioridadFiltro.Items.AddRange(new object[] { "Todas", "Urgente", "Alta", "Media", "Baja" });
            cmbPrioridadFiltro.SelectedIndex = 0;
            cmbPrioridadFiltro.SelectedIndexChanged += (s, e) => AplicarFiltros();
            filtros.Controls.Add(cmbPrioridadFiltro);

            lblContador = new Label
            {
                Text = "0 solicitudes",
                AutoSize = true,
                ForeColor = tenue,
                Location = new Point(604, 26)
            };
            filtros.Controls.Add(lblContador);

            var flujo = new FlowLayoutPanel
            {
                Location = new Point(0, 62),
                Size = new Size(760, 38),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = tarjeta
            };
            btnAprobar = Boton("✔  Aprobar", 120, verde, Color.White);
            btnAprobar.Enabled = false;
            btnAprobar.Click += (s, e) => ResolverSolicitud(Accion.Aprobar);
            btnRechazar = Boton("✖  Rechazar", 120, rojo, Color.White);
            btnRechazar.Enabled = false;
            btnRechazar.Click += (s, e) => ResolverSolicitud(Accion.Rechazar);
            btnEntregar = Boton("📦  Marcar entregada", 160, azul, Color.White);
            btnEntregar.Enabled = false;
            btnEntregar.Click += (s, e) => ResolverSolicitud(Accion.Entregar);
            btnExportar = Boton("⭳  Exportar CSV", 130, Color.FromArgb(230, 236, 242), texto);
            btnExportar.Click += (s, e) => ExportarCsv();
            btnActualizar = Boton("↻  Actualizar", 120, Color.FromArgb(230, 236, 242), texto);
            btnActualizar.Click += (s, e) => CargarTodo(true);
            flujo.Controls.AddRange(new Control[] { btnAprobar, btnRechazar, btnEntregar, btnExportar, btnActualizar });
            filtros.Controls.Add(flujo);
        }

        private void EstilizarGrid(DataGridView g, int altoFila)
        {
            g.ReadOnly = true;
            g.AllowUserToAddRows = false;
            g.AllowUserToDeleteRows = false;
            g.AllowUserToResizeRows = false;
            g.MultiSelect = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            g.BackgroundColor = tarjeta;
            g.BorderStyle = BorderStyle.None;
            g.RowHeadersVisible = false;
            g.EnableHeadersVisualStyles = false;
            g.GridColor = linea;
            g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.ColumnHeadersHeight = 34;
            g.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(240, 244, 248),
                ForeColor = tenue,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                SelectionBackColor = Color.FromArgb(240, 244, 248),
                SelectionForeColor = tenue,
                Padding = new Padding(6, 0, 0, 0)
            };
            g.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = texto,
                SelectionBackColor = Color.FromArgb(226, 243, 235),
                SelectionForeColor = texto,
                Padding = new Padding(5, 0, 0, 0)
            };
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 251, 253);
            g.RowTemplate.Height = altoFila;
        }

        private void ConfigurarColumnas()
        {
            var g = gridSolicitudes;
            if (g.Columns.Count == 0) return;

            foreach (string c in new[] { "id_solicitud", "id_trabajador", "id_admin", "dni", "correo", "telefono",
                                         "cargo", "categoria", "tipo_entrega", "area_destino", "motivo", "respuesta_admin" })
                if (g.Columns.Contains(c)) g.Columns[c].Visible = false;

            Config(g, "fecha_solicitud", "Fecha", 110, "dd/MM/yyyy HH:mm");
            Config(g, "trabajador", "Solicitante", 150, null);
            Config(g, "equipo", "Equipo", 190, null);
            Config(g, "cantidad", "Cant.", 50, null);
            Config(g, "prioridad", "Prioridad", 80, null);
            Config(g, "estado", "Estado", 90, null);
            Config(g, "fecha_requerida", "Requerida para", 100, "dd/MM/yyyy");
            Config(g, "admin", "Atendido por", 120, null);

            if (g.Columns.Contains("cantidad"))
                g.Columns["cantidad"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        private static void Config(DataGridView g, string nombre, string titulo, int peso, string formato)
        {
            if (!g.Columns.Contains(nombre)) return;
            var col = g.Columns[nombre];
            col.HeaderText = titulo;
            col.FillWeight = peso;
            col.MinimumWidth = 50;
            if (formato != null) col.DefaultCellStyle.Format = formato;
        }

        private void FormatearCeldas(object sender, DataGridViewCellFormattingEventArgs ev)
        {
            if (ev.RowIndex < 0 || ev.ColumnIndex < 0) return;
            string col = gridSolicitudes.Columns[ev.ColumnIndex].Name;

            if (col == "estado")
            {
                ev.CellStyle.ForeColor = ColorEstado(Convert.ToString(ev.Value));
                ev.CellStyle.Font = fuenteNegrita;
            }
            else if (col == "prioridad")
            {
                ev.CellStyle.ForeColor = ColorPrioridad(Convert.ToString(ev.Value));
                ev.CellStyle.Font = fuenteNegrita;
            }
            else if (col == "fecha_requerida" && ev.Value is DateTime)
            {
                // Marca en rojo las solicitudes vencidas que siguen abiertas
                string estado = Convert.ToString(gridSolicitudes.Rows[ev.RowIndex].Cells["estado"].Value);
                bool abierta = estado == "Pendiente" || estado == "Aprobada";
                if (abierta && ((DateTime)ev.Value).Date < DateTime.Today)
                {
                    ev.CellStyle.ForeColor = rojo;
                    ev.CellStyle.Font = fuenteNegrita;
                }
            }
        }

        private Color ColorEstado(string estado)
        {
            switch (estado)
            {
                case "Pendiente": return ambar;
                case "Aprobada": return azul;
                case "Entregada": return verde;
                case "Rechazada": return rojo;
                default: return texto;
            }
        }

        private Color ColorPrioridad(string prioridad)
        {
            switch (prioridad)
            {
                case "Urgente": return rojo;
                case "Alta": return ambar;
                case "Media": return azul;
                case "Baja": return tenue;
                default: return texto;
            }
        }

        // =================================================================
        //  DETALLE
        // =================================================================
        private void ConstruirDetalle(Panel cont)
        {
            var t = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 7,
                BackColor = tarjeta
            };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            for (int i = 0; i < 5; i++) t.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));

            string[][] campos =
            {
                new[] { "solicitante", "Solicitante" }, new[] { "dni", "DNI" },               new[] { "correo", "Correo" },
                new[] { "telefono", "Celular" },        new[] { "cargo", "Cargo" },           new[] { "estado", "Estado" },
                new[] { "equipo", "Equipo" },           new[] { "categoria", "Categoría" },   new[] { "cantidad", "Cantidad" },
                new[] { "prioridad", "Prioridad" },     new[] { "tipo_entrega", "Tipo de entrega" }, new[] { "area_destino", "Área destino" },
                new[] { "fecha_solicitud", "Fecha de solicitud" }, new[] { "fecha_requerida", "Fecha requerida" }, new[] { "admin", "Atendido por" }
            };
            for (int i = 0; i < campos.Length; i++)
                t.Controls.Add(CeldaDetalle(campos[i][0], campos[i][1], false), i % 3, i / 3);

            var motivo = CeldaDetalle("motivo", "Motivo de la solicitud", true);
            t.Controls.Add(motivo, 0, 5);
            t.SetColumnSpan(motivo, 3);

            var respuesta = CeldaDetalle("respuesta_admin", "Respuesta del administrador", true);
            t.Controls.Add(respuesta, 0, 6);
            t.SetColumnSpan(respuesta, 3);

            cont.Controls.Add(t);
        }

        private Panel CeldaDetalle(string clave, string titulo, bool grande)
        {
            var p = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 14, 0), BackColor = tarjeta };
            var valor = new Label
            {
                Text = "—",
                AutoSize = false,
                Dock = DockStyle.Fill,
                ForeColor = texto,
                Font = new Font("Segoe UI", grande ? 9.5F : 10F),
                TextAlign = ContentAlignment.TopLeft,
                AutoEllipsis = !grande
            };
            p.Controls.Add(valor);
            p.Controls.Add(new Label
            {
                Text = titulo.ToUpperInvariant(),
                Dock = DockStyle.Top,
                Height = 18,
                ForeColor = tenue,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold)
            });
            valor.BringToFront();
            detalle[clave] = valor;
            return p;
        }

        private void SetDetalle(string clave, string valor)
        {
            Label l;
            if (detalle.TryGetValue(clave, out l))
            {
                l.Text = string.IsNullOrWhiteSpace(valor) ? "—" : valor;
                l.ForeColor = texto;
            }
        }

        private void ActualizarDetalle()
        {
            if (detalle.Count == 0) return;
            var r = FilaActual();
            if (r == null) { LimpiarDetalle(); return; }

            SetDetalle("solicitante", Valor(r, "trabajador"));
            SetDetalle("dni", Valor(r, "dni"));
            SetDetalle("correo", Valor(r, "correo"));
            SetDetalle("telefono", Valor(r, "telefono"));
            SetDetalle("cargo", Valor(r, "cargo"));
            SetDetalle("equipo", Valor(r, "equipo"));
            SetDetalle("categoria", Valor(r, "categoria"));
            SetDetalle("cantidad", Valor(r, "cantidad"));
            SetDetalle("tipo_entrega", Valor(r, "tipo_entrega"));
            SetDetalle("area_destino", Valor(r, "area_destino"));
            SetDetalle("admin", Valor(r, "admin"));
            SetDetalle("motivo", Valor(r, "motivo"));
            SetDetalle("respuesta_admin", Valor(r, "respuesta_admin"));
            SetDetalle("fecha_solicitud", Fecha(r, "fecha_solicitud", "dd/MM/yyyy HH:mm"));
            SetDetalle("fecha_requerida", Fecha(r, "fecha_requerida", "dd/MM/yyyy"));

            string estado = Valor(r, "estado");
            SetDetalle("estado", estado);
            detalle["estado"].ForeColor = ColorEstado(estado);
            detalle["estado"].Font = new Font("Segoe UI", 10F, FontStyle.Bold);

            string prio = Valor(r, "prioridad");
            SetDetalle("prioridad", prio);
            detalle["prioridad"].ForeColor = ColorPrioridad(prio);
            detalle["prioridad"].Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        }

        private void LimpiarDetalle()
        {
            foreach (var kv in detalle) { kv.Value.Text = "—"; kv.Value.ForeColor = texto; }
        }

        private string Valor(DataRow fila, string columna)
        {
            if (!fila.Table.Columns.Contains(columna) || fila.IsNull(columna)) return "—";
            string v = Convert.ToString(fila[columna]);
            return string.IsNullOrWhiteSpace(v) ? "—" : v;
        }

        private string Fecha(DataRow fila, string columna, string formato)
        {
            if (!fila.Table.Columns.Contains(columna) || fila.IsNull(columna)) return "—";
            return Convert.ToDateTime(fila[columna]).ToString(formato);
        }

        private DataRow FilaActual()
        {
            if (gridSolicitudes == null || gridSolicitudes.CurrentRow == null) return null;
            var v = gridSolicitudes.CurrentRow.DataBoundItem as DataRowView;
            return v == null ? null : v.Row;
        }

        // =================================================================
        //  CONEXIÓN Y DATOS
        // =================================================================
        private NpgsqlConnection AbrirConexion()
        {
            var conexion = new global::SUSTENTACION.Conexion().ObtenerConexion();
            if (conexion == null) throw new InvalidOperationException("La conexión devolvió un valor vacío.");
            if (conexion.State != ConnectionState.Open) conexion.Open();
            return conexion;
        }

        private void AsegurarEsquema(NpgsqlConnection conexion)
        {
            if (esquemaVerificado) return;
            try
            {
                using (var cmd = new NpgsqlCommand(SqlCrearTabla, conexion))
                    cmd.ExecuteNonQuery();
                esquemaVerificado = true;
            }
            catch
            {
                // Sin permisos para crear: se asume que la tabla ya existe.
            }
        }

        private void CargarTodo(bool mostrarErrores)
        {
            if (cargando || IsDisposed) return;
            cargando = true;
            int? idSel = IdSeleccionado();
            try
            {
                using (var conexion = AbrirConexion())
                {
                    AsegurarEsquema(conexion);
                    CargarCatalogo(conexion);
                    CargarTrabajadores(conexion);
                    datosSolicitudes = LeerSolicitudes(conexion);
                    ActualizarKpis(conexion);
                }
                gridSolicitudes.DataSource = datosSolicitudes;
                ConfigurarColumnas();
                AplicarFiltros();
                RestaurarSeleccion(idSel);
                ActualizarBotones();
                ActualizarDetalle();
                SetConexion("● Conectado a Supabase", verde);
            }
            catch (Exception ex)
            {
                SetConexion("● Error de conexión o consulta", Color.Firebrick);
                gridSolicitudes.DataSource = null;
                if (mostrarErrores)
                    MessageBox.Show(this, "No se pudieron cargar las solicitudes de equipo.\r\n\r\n" + ex.Message,
                        "Solicitudes de equipo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { cargando = false; }
        }

        private int? IdSeleccionado()
        {
            var r = FilaActual();
            if (r == null || !r.Table.Columns.Contains("id_solicitud") || r.IsNull("id_solicitud")) return null;
            return Convert.ToInt32(r["id_solicitud"]);
        }

        private void RestaurarSeleccion(int? id)
        {
            if (!id.HasValue || gridSolicitudes.Rows.Count == 0) return;
            var primera = gridSolicitudes.Columns.GetFirstColumn(DataGridViewElementStates.Visible);
            if (primera == null) return;
            foreach (DataGridViewRow fila in gridSolicitudes.Rows)
            {
                var v = fila.DataBoundItem as DataRowView;
                if (v != null && Convert.ToInt32(v.Row["id_solicitud"]) == id.Value)
                {
                    gridSolicitudes.CurrentCell = fila.Cells[primera.Index];
                    fila.Selected = true;
                    return;
                }
            }
        }

        private void CargarCatalogo(NpgsqlConnection conexion)
        {
            catalogo.Clear();
            inventarioDisponible = false;
            try
            {
                string sql = "SELECT COALESCE(" + ColInvNombre + "::text,'') AS nombre, " +
                             "COALESCE(" + ColInvCategoria + "::text,'Otros') AS categoria, " +
                             "COALESCE(" + ColInvStock + ",0) AS stock " +
                             "FROM " + TablaInventario + " ORDER BY 2, 1 LIMIT 3000";
                using (var cmd = new NpgsqlCommand(sql, conexion))
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        string nombre = Convert.ToString(rd["nombre"]);
                        if (string.IsNullOrWhiteSpace(nombre)) continue;
                        catalogo.Add(new EquipoInv
                        {
                            Nombre = nombre,
                            Categoria = Convert.ToString(rd["categoria"]),
                            Stock = Convert.ToInt32(rd["stock"])
                        });
                    }
                }
                inventarioDisponible = true;
            }
            catch
            {
                // La tabla de inventario no existe o tiene otras columnas:
                // el panel sigue funcionando sin catálogo ni control de stock.
                inventarioDisponible = false;
                catalogo.Clear();
            }
            LlenarCategorias();
            ActualizarStock();
        }

        private void CargarTrabajadores(NpgsqlConnection conexion)
        {
            var itemsNuevos = new List<object>();
            try
            {
                using (var cmd = new NpgsqlCommand(@"
SELECT id_trabajador,
       COALESCE(nombre,'')  AS nombre,
       COALESCE(dni,'')     AS dni
FROM public.trabajadores
WHERE COALESCE(activo, true) = true
ORDER BY nombre
LIMIT 1500;", conexion))
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        string dni = Convert.ToString(rd["dni"]);
                        itemsNuevos.Add(new Item
                        {
                            Id = Convert.ToInt32(rd["id_trabajador"]),
                            Texto = Convert.ToString(rd["nombre"]) + (dni.Length > 0 ? "  —  " + dni : "")
                        });
                    }
                }
            }
            catch { return; }

            var previo = ObtenerSolicitanteSeleccionado();
            cmbSolicitante.Items.Clear();
            cmbSolicitante.Items.AddRange(itemsNuevos.ToArray());
            if (previo != null)
                foreach (object o in cmbSolicitante.Items)
                {
                    var it = o as Item;
                    if (it != null && it.Id == previo.Id) { cmbSolicitante.SelectedItem = o; break; }
                }
        }

        private DataTable LeerSolicitudes(NpgsqlConnection conexion)
        {
            const string sql = @"
SELECT
    sc.id_solicitud,
    sc.id_trabajador,
    sc.fecha_solicitud,
    COALESCE(t.nombre, 'Usuario #' || sc.id_trabajador::text) AS trabajador,
    sc.equipo,
    sc.cantidad,
    COALESCE(sc.prioridad, 'Media')      AS prioridad,
    COALESCE(sc.estado, 'Pendiente')     AS estado,
    sc.fecha_requerida,
    COALESCE(a.nombre, '')               AS admin,
    COALESCE(sc.categoria, '')           AS categoria,
    COALESCE(sc.tipo_entrega, '')        AS tipo_entrega,
    COALESCE(sc.area_destino, '')        AS area_destino,
    COALESCE(sc.motivo, '')              AS motivo,
    COALESCE(sc.respuesta_admin, '')     AS respuesta_admin,
    sc.id_admin,
    COALESCE(t.dni, '')                  AS dni,
    COALESCE(t.correo, '')               AS correo,
    COALESCE(t.telefono, '')             AS telefono,
    COALESCE(t.cargo, '')                AS cargo
FROM public.solicitudes_equipo sc
LEFT JOIN public.trabajadores t ON t.id_trabajador = sc.id_trabajador
LEFT JOIN public.trabajadores a ON a.id_trabajador = sc.id_admin
ORDER BY
    CASE COALESCE(sc.estado,'Pendiente')
        WHEN 'Pendiente' THEN 1
        WHEN 'Aprobada'  THEN 2
        WHEN 'Entregada' THEN 3
        ELSE 4
    END,
    CASE COALESCE(sc.prioridad,'Media')
        WHEN 'Urgente' THEN 1
        WHEN 'Alta'    THEN 2
        WHEN 'Media'   THEN 3
        ELSE 4
    END,
    sc.fecha_solicitud DESC NULLS LAST,
    sc.id_solicitud DESC;";

            var tabla = new DataTable();
            using (var adaptador = new NpgsqlDataAdapter(sql, conexion))
                adaptador.Fill(tabla);
            return tabla;
        }

        private void ActualizarKpis(NpgsqlConnection conexion)
        {
            const string sql = @"
SELECT
    COUNT(*) FILTER (WHERE COALESCE(estado,'Pendiente') = 'Pendiente') AS pendientes,
    COUNT(*) FILTER (WHERE estado = 'Aprobada')  AS aprobadas,
    COUNT(*) FILTER (WHERE estado = 'Entregada') AS entregadas,
    COUNT(*) FILTER (WHERE estado = 'Rechazada') AS rechazadas,
    COUNT(*) FILTER (WHERE COALESCE(estado,'Pendiente') = 'Pendiente'
                       AND COALESCE(prioridad,'Media') = 'Urgente') AS urgentes,
    COUNT(*) AS total
FROM public.solicitudes_equipo;";

            using (var cmd = new NpgsqlCommand(sql, conexion))
            using (var rd = cmd.ExecuteReader())
            {
                if (!rd.Read()) return;
                lblPendientes.Text = Convert.ToInt64(rd["pendientes"]).ToString("N0");
                lblAprobadas.Text = Convert.ToInt64(rd["aprobadas"]).ToString("N0");
                lblEntregadas.Text = Convert.ToInt64(rd["entregadas"]).ToString("N0");
                lblRechazadas.Text = Convert.ToInt64(rd["rechazadas"]).ToString("N0");
                lblTotal.Text = Convert.ToInt64(rd["total"]).ToString("N0");

                long urgentes = Convert.ToInt64(rd["urgentes"]);
                lblUrgentes.Text = urgentes > 0 ? "● " + urgentes + " urgente(s)" : "Sin urgentes";
                lblUrgentes.ForeColor = urgentes > 0 ? rojo : tenue;
            }
        }

        // =================================================================
        //  FILTROS Y BOTONES
        // =================================================================
        private static string EscaparLike(string valor)
        {
            return valor.Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]").Replace("*", "[*]");
        }

        private void AplicarFiltros()
        {
            if (datosSolicitudes == null || datosSolicitudes.DefaultView == null) return;
            var condiciones = new List<string>();

            string buscar = txtBuscar == null ? "" : txtBuscar.Text.Trim();
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                string valor = EscaparLike(buscar);
                var partes = new List<string>();
                foreach (string col in new[] { "trabajador", "equipo", "categoria", "area_destino", "motivo", "respuesta_admin", "admin", "dni" })
                    if (datosSolicitudes.Columns.Contains(col))
                        partes.Add("CONVERT(" + col + ", 'System.String') LIKE '%" + valor + "%'");
                if (partes.Count > 0) condiciones.Add("(" + string.Join(" OR ", partes) + ")");
            }
            if (cmbEstadoFiltro != null && cmbEstadoFiltro.SelectedIndex > 0 && datosSolicitudes.Columns.Contains("estado"))
                condiciones.Add("estado = '" + EscaparLike(cmbEstadoFiltro.SelectedItem.ToString()) + "'");
            if (cmbPrioridadFiltro != null && cmbPrioridadFiltro.SelectedIndex > 0 && datosSolicitudes.Columns.Contains("prioridad"))
                condiciones.Add("prioridad = '" + EscaparLike(cmbPrioridadFiltro.SelectedItem.ToString()) + "'");

            try { datosSolicitudes.DefaultView.RowFilter = string.Join(" AND ", condiciones); }
            catch { datosSolicitudes.DefaultView.RowFilter = ""; }

            if (lblContador != null)
                lblContador.Text = datosSolicitudes.DefaultView.Count + " solicitud(es)";
            ActualizarBotones();
        }

        private void EstadoBoton(Button b, bool activo, Color colorActivo)
        {
            if (b == null) return;
            b.Enabled = activo;
            b.BackColor = activo ? colorActivo : Color.FromArgb(215, 222, 230);
            b.ForeColor = activo ? Color.White : tenue;
        }

        private void ActualizarBotones()
        {
            var r = FilaActual();
            string estado = r == null ? "" : Convert.ToString(r["estado"]);
            bool pendiente = string.Equals(estado, "Pendiente", StringComparison.OrdinalIgnoreCase);
            bool aprobada = string.Equals(estado, "Aprobada", StringComparison.OrdinalIgnoreCase);
            EstadoBoton(btnAprobar, pendiente, verde);
            EstadoBoton(btnRechazar, pendiente, rojo);
            EstadoBoton(btnEntregar, aprobada, azul);
        }

        // =================================================================
        //  RESOLVER SOLICITUD  (Aprobar / Rechazar / Entregar)
        // =================================================================
        private void ResolverSolicitud(Accion accion)
        {
            var r = FilaActual();
            if (r == null) { Aviso("Selecciona una solicitud."); return; }

            string estadoActual = Convert.ToString(r["estado"]);
            if (accion == Accion.Entregar)
            {
                if (!string.Equals(estadoActual, "Aprobada", StringComparison.OrdinalIgnoreCase))
                { Aviso("Solo se pueden entregar solicitudes aprobadas (estado actual: " + estadoActual + ")."); return; }
            }
            else if (!string.Equals(estadoActual, "Pendiente", StringComparison.OrdinalIgnoreCase))
            {
                Aviso("Esta solicitud ya fue resuelta (" + estadoActual + ")."); return;
            }

            int idSolicitud = Convert.ToInt32(r["id_solicitud"]);
            string equipo = Convert.ToString(r["equipo"]);
            int cantidad = Convert.ToInt32(r["cantidad"]);

            string resumen = ConstruirResumen(r);
            var inv = BuscarEquipo(equipo);
            if (inventarioDisponible && inv != null && accion != Accion.Rechazar && inv.Stock < cantidad)
                resumen += Environment.NewLine + Environment.NewLine +
                           "⚠ Stock actual: " + inv.Stock + " (menor a la cantidad solicitada).";

            string comentario;
            switch (accion)
            {
                case Accion.Aprobar:
                    comentario = PedirComentario("Aprobar solicitud", resumen, false, "Aprobar", verde, ""); break;
                case Accion.Rechazar:
                    comentario = PedirComentario("Rechazar solicitud", resumen, true, "Rechazar", rojo, ""); break;
                default:
                    comentario = PedirComentario("Registrar entrega", resumen, false, "Confirmar entrega", azul,
                        "Equipo entregado al solicitante."); break;
            }
            if (comentario == null) return;

            int idAdmin = ObtenerIdAdminActual();

            try
            {
                using (var conexion = AbrirConexion())
                using (var tx = conexion.BeginTransaction())
                {
                    string sql;
                    if (accion == Accion.Entregar)
                        sql = @"
UPDATE public.solicitudes_equipo
SET estado = 'Entregada',
    fecha_entrega = now(),
    respuesta_admin = CASE WHEN @resp = '' THEN respuesta_admin ELSE @resp END,
    id_admin = COALESCE(@admin, id_admin)
WHERE id_solicitud = @id AND estado = 'Aprobada'";
                    else
                        sql = @"
UPDATE public.solicitudes_equipo
SET estado = @estado,
    respuesta_admin = @resp,
    id_admin = @admin,
    fecha_resolucion = now()
WHERE id_solicitud = @id AND COALESCE(estado,'Pendiente') = 'Pendiente'";

                    using (var cmd = new NpgsqlCommand(sql, conexion, tx))
                    {
                        if (accion != Accion.Entregar)
                            cmd.Parameters.Add("@estado", NpgsqlDbType.Varchar).Value =
                                accion == Accion.Aprobar ? "Aprobada" : "Rechazada";
                        cmd.Parameters.Add("@resp", NpgsqlDbType.Text).Value = comentario;
                        cmd.Parameters.Add("@admin", NpgsqlDbType.Integer).Value =
                            idAdmin == 0 ? (object)DBNull.Value : idAdmin;
                        cmd.Parameters.Add("@id", NpgsqlDbType.Integer).Value = idSolicitud;
                        if (cmd.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException("La solicitud ya fue modificada por otro administrador.");
                    }

                    if (accion == Accion.Entregar && DescontarStockAlEntregar && inventarioDisponible)
                        DescontarStock(conexion, tx, equipo, cantidad);

                    tx.Commit();
                }

                CargarTodo(false);
                string msg = accion == Accion.Aprobar ? "Solicitud aprobada."
                           : accion == Accion.Rechazar ? "Solicitud rechazada."
                           : "Entrega registrada" + (DescontarStockAlEntregar && inventarioDisponible ? " y stock actualizado." : ".");
                MessageBox.Show(this, msg, "Solicitudes de equipo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "No se pudo completar la operación.\r\n\r\n" + ex.Message,
                    "Solicitudes de equipo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DescontarStock(NpgsqlConnection conexion, NpgsqlTransaction tx, string equipo, int cantidad)
        {
            string sqlSel = "SELECT " + ColInvStock + " FROM " + TablaInventario +
                            " WHERE lower(" + ColInvNombre + ") = lower(@n) FOR UPDATE";
            object res;
            using (var cmd = new NpgsqlCommand(sqlSel, conexion, tx))
            {
                cmd.Parameters.AddWithValue("@n", equipo);
                res = cmd.ExecuteScalar();
            }
            if (res == null || res == DBNull.Value) return; // equipo no registrado en inventario

            int stock = Convert.ToInt32(res);
            if (stock < cantidad)
                throw new InvalidOperationException(
                    "Stock insuficiente de \"" + equipo + "\": hay " + stock + " y se entregan " + cantidad + ".");

            string sqlUpd = "UPDATE " + TablaInventario + " SET " + ColInvStock + " = " + ColInvStock +
                            " - @c WHERE lower(" + ColInvNombre + ") = lower(@n)";
            using (var cmd = new NpgsqlCommand(sqlUpd, conexion, tx))
            {
                cmd.Parameters.AddWithValue("@c", cantidad);
                cmd.Parameters.AddWithValue("@n", equipo);
                cmd.ExecuteNonQuery();
            }
        }

        private string ConstruirResumen(DataRow r)
        {
            string nl = Environment.NewLine;
            return "Solicitante:  " + Valor(r, "trabajador") + nl +
                   "DNI:  " + Valor(r, "dni") + "    ·    Cargo:  " + Valor(r, "cargo") + nl +
                   "Equipo:  " + Valor(r, "equipo") + "  (x" + Valor(r, "cantidad") + ")  —  " + Valor(r, "categoria") + nl +
                   "Prioridad:  " + Valor(r, "prioridad") + "    ·    Entrega:  " + Valor(r, "tipo_entrega") + nl +
                   "Requerido para:  " + Fecha(r, "fecha_requerida", "dd/MM/yyyy") + "    ·    Área:  " + Valor(r, "area_destino") + nl +
                   "Motivo:  " + Valor(r, "motivo");
        }

        private string PedirComentario(string titulo, string resumen, bool obligatorio,
                                       string textoBoton, Color colorBoton, string inicial)
        {
            using (var dlg = new Form
            {
                Text = titulo,
                ClientSize = new Size(560, 400),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            })
            {
                dlg.Controls.Add(new Label
                {
                    Text = titulo.ToUpperInvariant(),
                    AutoSize = true,
                    ForeColor = colorBoton,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    Location = new Point(18, 14)
                });
                dlg.Controls.Add(new Label
                {
                    Text = resumen,
                    Location = new Point(18, 44),
                    Size = new Size(524, 170),
                    ForeColor = texto,
                    Font = new Font("Segoe UI", 9.5F)
                });
                dlg.Controls.Add(new Label
                {
                    Text = obligatorio ? "Motivo (obligatorio):" : "Comentario para el solicitante (opcional):",
                    AutoSize = true,
                    ForeColor = tenue,
                    Location = new Point(18, 224)
                });
                var txt = new TextBox
                {
                    Location = new Point(18, 248),
                    Size = new Size(524, 90),
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    Text = inicial ?? ""
                };
                dlg.Controls.Add(txt);

                var ok = new Button
                {
                    Text = textoBoton,
                    DialogResult = DialogResult.OK,
                    Location = new Point(dlg.ClientSize.Width - 198, 350),
                    Size = new Size(180, 32),
                    BackColor = colorBoton,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                ok.FlatAppearance.BorderSize = 0;
                dlg.Controls.Add(ok);

                var cancelar = new Button
                {
                    Text = "Cancelar",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(dlg.ClientSize.Width - 308, 350),
                    Size = new Size(100, 32)
                };
                dlg.Controls.Add(cancelar);
                dlg.AcceptButton = null;
                dlg.CancelButton = cancelar;

                dlg.FormClosing += (s, e) =>
                {
                    if (dlg.DialogResult == DialogResult.OK && obligatorio && string.IsNullOrWhiteSpace(txt.Text))
                    {
                        MessageBox.Show(dlg, "Debes escribir el motivo del rechazo.",
                            "Dato requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        e.Cancel = true;
                        txt.Focus();
                    }
                };

                return dlg.ShowDialog(FindForm()) == DialogResult.OK ? txt.Text.Trim() : null;
            }
        }

        // =================================================================
        //  EXPORTAR CSV
        // =================================================================
        private void ExportarCsv()
        {
            if (datosSolicitudes == null || datosSolicitudes.DefaultView.Count == 0)
            {
                Aviso("No hay solicitudes para exportar.");
                return;
            }

            using (var dlg = new SaveFileDialog
            {
                Filter = "Archivo CSV (*.csv)|*.csv",
                FileName = "solicitudes_equipo_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv"
            })
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

                string[][] cols =
                {
                    new[] { "id_solicitud", "N°" },
                    new[] { "fecha_solicitud", "Fecha de solicitud" },
                    new[] { "trabajador", "Solicitante" },
                    new[] { "dni", "DNI" },
                    new[] { "categoria", "Categoría" },
                    new[] { "equipo", "Equipo" },
                    new[] { "cantidad", "Cantidad" },
                    new[] { "prioridad", "Prioridad" },
                    new[] { "tipo_entrega", "Tipo de entrega" },
                    new[] { "area_destino", "Área destino" },
                    new[] { "fecha_requerida", "Fecha requerida" },
                    new[] { "estado", "Estado" },
                    new[] { "admin", "Atendido por" },
                    new[] { "motivo", "Motivo" },
                    new[] { "respuesta_admin", "Respuesta" }
                };

                var sb = new StringBuilder();
                var cab = new List<string>();
                foreach (var c in cols) cab.Add(CsvCelda(c[1]));
                sb.AppendLine(string.Join(",", cab));

                foreach (DataRowView vr in datosSolicitudes.DefaultView)
                {
                    var fila = new List<string>();
                    foreach (var c in cols)
                    {
                        object v = datosSolicitudes.Columns.Contains(c[0]) ? vr.Row[c[0]] : null;
                        string s2;
                        if (v == null || v == DBNull.Value) s2 = "";
                        else if (v is DateTime) s2 = ((DateTime)v).ToString("yyyy-MM-dd HH:mm");
                        else s2 = Convert.ToString(v);
                        fila.Add(CsvCelda(s2));
                    }
                    sb.AppendLine(string.Join(",", fila));
                }

                try
                {
                    File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
                    MessageBox.Show(this, "Archivo exportado correctamente.", "Solicitudes de equipo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "No se pudo guardar el archivo.\r\n\r\n" + ex.Message,
                        "Solicitudes de equipo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static string CsvCelda(string valor)
        {
            if (valor == null) return "";
            if (valor.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
                return "\"" + valor.Replace("\"", "\"\"") + "\"";
            return valor;
        }

        // =================================================================
        //  Utilidades
        // =================================================================
        private void Aviso(string mensaje)
        {
            MessageBox.Show(this, mensaje, "Solicitudes de equipo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}