#nullable disable
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Npgsql;
using NpgsqlTypes;
using SUSTENTACION.PanelTrabajador;
using SUSTENTACION.Temas;

namespace SUSTENTACION
{
    public class InventarioControl : TemaUserControl
    {
        private sealed class CodigoEquipoDuplicadoException : Exception
        {
            public CodigoEquipoDuplicadoException(string codigo)
                : base("El código '" + codigo + "' ya está registrado. Si es el mismo equipo y necesitas aumentar las existencias, usa el botón Entrada. Para registrar otro equipo por separado, escribe un código distinto.")
            {
            }
        }

        // ==============================================================
        //  PALETA DINÁMICA (Lee los colores de TemaApp en tiempo real)
        // ==============================================================
        private Color bgDark => TemaApp.FondoApp;
        private Color bgCard => TemaApp.Superficie;
        private Color bgInput => TemaApp.SuperficieAlt;
        private Color bgHover => TemaApp.SuperficieAlt;
        private Color borde => TemaApp.Borde;
        private Color txtMain => TemaApp.Texto;
        private Color txtSoft => TemaApp.TextoTenue;
        private Color txtGray => TemaApp.TextoTenue;

        private Color verde => TemaApp.Exito;
        private Color verdeHover => TemaApp.ExitoSuave;
        private Color azul => TemaApp.AccentBlue;
        private Color cian => TemaApp.AccentBlue;
        private Color naranja => TemaApp.Advertencia;
        private Color morado => TemaApp.AccentPurple;
        private Color rojo => TemaApp.Peligro;
        private Color amarillo => TemaApp.Advertencia;
        private Color textoClaro => TemaApp.Texto;

        private static readonly string[] EstadosEquipo =
            { "Disponible", "Asignado", "Prestado", "Mantenimiento", "Con observaciones", "Baja" };

        private static readonly string[] MotivosEntrada =
            { "Compra nueva", "Reposición de stock", "Devolución de préstamo", "Reparación finalizada", "Ajuste de inventario" };

        private static readonly string[] MotivosSalida =
            { "Grabación en exteriores", "Producción en estudio", "Evento en vivo", "Préstamo a trabajador", "Envío a reparación", "Baja / descarte" };

        private const int EM_SETCUEBANNER = 0x1501;
        private const int PaginaMinAncho = 1120;
        private const int PaginaMinAlto = 1040;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        // ==============================================================
        //  CAMPOS
        // ==============================================================
        private readonly List<Font> fuentes = new List<Font>();
        private readonly List<Button> botonesEscritura = new List<Button>();
        private readonly List<Button> chips = new List<Button>();
        private readonly Dictionary<string, Button> chipPorClave = new Dictionary<string, Button>();

        private readonly Label[] indicadores = new Label[6];
        private readonly Label[] detallesKpi = new Label[6];

        private Label lblEstadoConexion;
        private Label lblAtajos;
        private Label lblConteo;
        private Label lblVacio;
        private Label lblAvisoHistorial;
        private Label lblHistTotal;
        private Label lblHistEntradas;
        private Label lblHistSalidas;
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
        private Panel paginaEquipos;
        private Panel paginaMovimientos;
        private Button btnTabEquipos;
        private Button btnTabMovimientos;
        private Button btnActualizar;
        private Button btnEntrada;
        private Button btnSalida;
        private Button btnEditar;
        private Button btnNuevo;
        private Button btnEliminar;
        private FlowLayoutPanel panelAcciones;
        private ContextMenuStrip menuContextual;
        private System.Windows.Forms.Timer timerDetalle;

        private Panel scrollDetalle;
        private Panel contenedorDetalle;
        private Label lblDetPlaceholder;
        private Label lblDetCodigo;
        private Label lblDetEstado;
        private Label lblDetNombre;
        private Label lblDetMarcaModelo;
        private Label lblDetCategoria;
        private Label lblDetUbicacion;
        private Label lblDetStock;
        private Label lblDetStockEstado;
        private Label lblDetDescripcion;
        private Panel barraStock;
        private Panel panelDetMov;
        private long detStock;
        private int detEstadoColorArgb;

        private DataTable datosEquipos;
        private DataTable datosMovimientos;
        private long maxStock = 1;
        private string filtroRapido = "todos";
        private int pestanaActual;
        private bool conectado;
        private bool cargando;
        private bool ocupado;
        private bool construido;

        public InventarioControl()
        {
            Dock = DockStyle.Fill;
            BackColor = bgDark;
            ForeColor = txtMain;
            DoubleBuffered = true;
            Font = CrearFuente(9.5f);
            AutoScroll = true;
            AutoScrollMinSize = new Size(PaginaMinAncho, PaginaMinAlto);

            timerDetalle = new System.Windows.Forms.Timer { Interval = 250 };
            timerDetalle.Tick += delegate
            {
                timerDetalle.Stop();
                CargarMovimientosDetalle();
            };

            ConstruirInterfaz();
            Load += delegate { CargarDatosInventario(); };

            AplicarTema();

            TemaApp.TemaCambiado += () =>
            {
                if (dgvEquipos != null) AplicarEstiloGridManual(dgvEquipos);
                if (dgvMovimientos != null) AplicarEstiloGridManual(dgvMovimientos);
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (timerDetalle != null) { timerDetalle.Stop(); timerDetalle.Dispose(); }
                if (menuContextual != null) menuContextual.Dispose();
                if (datosEquipos != null) datosEquipos.Dispose();
                if (datosMovimientos != null) datosMovimientos.Dispose();
                foreach (Font fuente in fuentes) fuente.Dispose();
            }
            base.Dispose(disposing);
        }

        // ==============================================================
        //  ATAJOS DE TECLADO
        // ==============================================================
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.N) && conectado && !ocupado) { EditarEquipo(false); return true; }
            if (keyData == (Keys.Control | Keys.F)) { if (txtBuscar != null) { txtBuscar.Focus(); txtBuscar.SelectAll(); } return true; }
            if (keyData == Keys.F5) { ActualizarVista(); return true; }
            if (keyData == Keys.Delete && dgvEquipos != null && dgvEquipos.Focused && pestanaActual == 0)
            {
                EliminarEquipo();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ==============================================================
        //  HELPERS VISUALES
        // ==============================================================
        private Font CrearFuente(float tamano, bool negrita = false)
        {
            Font fuente = new Font("Segoe UI", tamano, negrita ? FontStyle.Bold : FontStyle.Regular);
            fuentes.Add(fuente);
            return fuente;
        }

        private Font CrearFuenteSimbolos(float tamano, bool negrita = false)
        {
            Font fuente = new Font("Segoe UI Symbol", tamano, negrita ? FontStyle.Bold : FontStyle.Regular);
            fuentes.Add(fuente);
            return fuente;
        }

        private Label Etiqueta(string texto, float tamano, Color color, bool negrita = false)
        {
            // Si estamos en modo claro y el texto es blanco o claro, lo cambiamos a gris oscuro
            Color colorFinal = color;
            if (!TemaApp.EsModoOscuro)
            {
                if (color == Color.White || color == Color.FromArgb(245, 247, 250) || color == TemaApp.Exito)
                {
                    colorFinal = TemaApp.Texto; // Gris oscuro
                }
            }

            return new Label
            {
                Text = texto,
                Font = CrearFuente(tamano, negrita),
                ForeColor = colorFinal,
                BackColor = Color.Transparent,
                AutoSize = true
            };
        }

        private static GraphicsPath Redondo(Rectangle rect, int radio)
        {
            var path = new GraphicsPath();
            if (rect.Width <= 0 || rect.Height <= 0) return path;
            int d = Math.Max(1, Math.Min(radio * 2, Math.Min(rect.Width, rect.Height)));
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void Redondear(Control control, int radio)
        {
            Action aplicar = delegate
            {
                if (control.Width <= 0 || control.Height <= 0) return;
                using (GraphicsPath path = Redondo(new Rectangle(0, 0, control.Width, control.Height), radio))
                {
                    Region anterior = control.Region;
                    control.Region = new Region(path);
                    if (anterior != null) anterior.Dispose();
                }
            };
            control.Resize += delegate { aplicar(); };
            aplicar();
        }

        private static Color Mezclar(Color a, Color b, float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        private static Color ConAlfa(Color c, int alfa)
        {
            return Color.FromArgb(Math.Max(0, Math.Min(255, alfa)), c.R, c.G, c.B);
        }

        private sealed class Tarjeta : Panel
        {
            public int Radio = 14;

            public Tarjeta()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                DoubleBuffered = true;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Parent != null ? Parent.BackColor : SystemColors.Control);

                var r = new Rectangle(0, 0, Width - 1, Height - 1);
                Color linea = TemaApp.EsModoOscuro
                    ? Color.FromArgb(39, 47, 58)
                    : Color.FromArgb(214, 225, 231);

                using (GraphicsPath path = Redondo(r, Radio))
                using (var fondo = new SolidBrush(BackColor))
                using (var pen = new Pen(linea, 1))
                {
                    g.FillPath(fondo, path);
                    g.DrawPath(pen, path);
                }

                base.OnPaint(e);
            }
        }

        private enum EstiloBoton { Principal, Secundario, Peligro }

        private Button Boton(string texto, EstiloBoton estilo, EventHandler accion, int ancho = 120, int alto = 38)
        {
            Button boton = new Button
            {
                Text = texto,
                Width = ancho,
                Height = alto,
                FlatStyle = FlatStyle.Flat,
                Font = CrearFuente(9, true),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0),
                UseVisualStyleBackColor = false,
                TabStop = false
            };

            switch (estilo)
            {
                case EstiloBoton.Principal:
                    boton.BackColor = TemaApp.Exito;
                    boton.ForeColor = Color.Black;
                    boton.FlatAppearance.BorderColor = TemaApp.Exito;
                    boton.FlatAppearance.MouseOverBackColor = TemaApp.ExitoSuave;
                    break;
                case EstiloBoton.Peligro:
                    boton.BackColor = bgCard;
                    boton.ForeColor = rojo;
                    boton.FlatAppearance.BorderColor = Color.FromArgb(120, 45, 50);
                    boton.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 24, 28);
                    break;
                default:
                    boton.BackColor = bgCard;
                    boton.ForeColor = txtSoft;
                    boton.FlatAppearance.BorderColor = borde;
                    boton.FlatAppearance.MouseOverBackColor = bgHover;
                    break;
            }

            boton.FlatAppearance.BorderSize = 1;
            boton.Click += accion;
            Redondear(boton, 10);
            return boton;
        }

        private ComboBox Combo(string primeraOpcion)
        {
            ComboBox combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = bgInput,
                ForeColor = txtMain,
                Font = CrearFuente(9),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 10, 0)
            };
            combo.Items.Add(primeraOpcion);
            combo.SelectedIndex = 0;
            return combo;
        }

        // ==============================================================
        //  CONSTRUCCIÓN DE LA INTERFAZ
        // ==============================================================
        private void ConstruirInterfaz()
        {
            TableLayoutPanel principal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = bgDark,
                Padding = new Padding(0),
                Margin = new Padding(0),
                ColumnCount = 1,
                RowCount = 6
            };
            principal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            principal.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
            principal.RowStyles.Add(new RowStyle(SizeType.Absolute, 106));
            principal.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
            principal.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            principal.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            principal.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            Controls.Add(principal);

            principal.Controls.Add(ConstruirEncabezado(), 0, 0);
            principal.Controls.Add(ConstruirIndicadores(), 0, 1);
            principal.Controls.Add(ConstruirFiltros(), 0, 2);
            principal.Controls.Add(ConstruirBarraAcciones(), 0, 3);
            principal.Controls.Add(ConstruirContenido(), 0, 4);
            principal.Controls.Add(ConstruirBarraEstado(), 0, 5);

            construido = true;
            CambiarPestana(0);
            ActualizarChips();
        }

        private Control ConstruirEncabezado()
        {
            Tarjeta encabezado = new Tarjeta
            {
                Dock = DockStyle.Fill,
                BackColor = bgCard,
                Margin = new Padding(0, 0, 0, 10)
            };

            encabezado.Paint += delegate (object s, PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var icono = new Rectangle(20, 17, 54, 54);
                using (GraphicsPath p = Redondo(icono, 14))
                using (var br = new LinearGradientBrush(icono, verde, Color.FromArgb(102, 204, 51), LinearGradientMode.ForwardDiagonal))
                    g.FillPath(br, p);
                using (var f = new Font("Segoe UI Symbol", 20f, FontStyle.Bold))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    g.DrawString("▣", f, Brushes.Black, icono, sf);
            };

            Label marca = Etiqueta("ARTEMUSA TV  /  INVENTARIO AUDIOVISUAL", 7.75f, verde, true);
            marca.Location = new Point(90, 12);

            Label titulo = Etiqueta("Control de equipos", 20f, txtMain, true);
            titulo.Location = new Point(88, 27);

            Label subtitulo = Etiqueta("Gestión y seguimiento de equipos audiovisuales", 9f, txtGray);
            subtitulo.Location = new Point(91, 64);

            btnActualizar = Boton("↻  Actualizar", EstiloBoton.Secundario, delegate { ActualizarVista(); }, 130, 40);
            btnActualizar.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            Button btnExportar = Boton("⇩  Exportar CSV", EstiloBoton.Secundario, delegate { ExportarCsv(); }, 142, 40);
            btnExportar.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            encabezado.Resize += delegate
            {
                btnActualizar.Location = new Point(Math.Max(14, encabezado.Width - btnActualizar.Width - 20), 26);
                btnExportar.Location = new Point(btnActualizar.Left - btnExportar.Width - 8, 26);
            };
            btnActualizar.Location = new Point(Math.Max(14, encabezado.Width - btnActualizar.Width - 20), 26);
            btnExportar.Location = new Point(btnActualizar.Left - btnExportar.Width - 8, 26);

            encabezado.Controls.Add(marca);
            encabezado.Controls.Add(titulo);
            encabezado.Controls.Add(subtitulo);
            encabezado.Controls.Add(btnActualizar);
            encabezado.Controls.Add(btnExportar);
            return encabezado;
        }

        private Control ConstruirIndicadores()
        {
            TableLayoutPanel fila = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 1,
                BackColor = bgDark,
                Margin = new Padding(0)
            };
            for (int i = 0; i < 6; i++)
                fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 6f));

            string[] titulos = { "EQUIPOS REGISTRADOS", "UNIDADES EN STOCK", "DISPONIBLES", "ASIGNADOS", "EN MANTENIMIENTO", "STOCK BAJO" };
            string[] detalles =
            {
                "Total en el inventario",
                "Suma de existencias",
                "Listos para usar",
                "En uso o prestados",
                "Fuera de servicio",
                "Requieren reposición"
            };
            string[] iconos = { "▣", "◈", "✓", "☻", "⚙", "!" };
            Color[] colores = { azul, cian, verde, morado, naranja, rojo };
            string[] filtros = { "todos", "todos", "disponibles", "asignados", "mantenimiento", "bajo" };

            for (int i = 0; i < 6; i++)
            {
                Tarjeta tarjeta = CrearKpi(i, titulos[i], detalles[i], iconos[i], colores[i], filtros[i]);
                tarjeta.Margin = new Padding(i == 0 ? 0 : 5, 0, i == 5 ? 0 : 5, 10);
                fila.Controls.Add(tarjeta, i, 0);
            }
            return fila;
        }

        private Tarjeta CrearKpi(int indice, string titulo, string detalle, string icono, Color acento, string filtro)
        {
            Tarjeta tarjeta = new Tarjeta
            {
                Dock = DockStyle.Fill,
                BackColor = bgCard,
                Cursor = Cursors.Hand
            };

            tarjeta.Paint += delegate (object s, PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var circulo = new Rectangle(14, 16, 40, 40);
                using (var b = new SolidBrush(ConAlfa(acento, 40)))
                    g.FillEllipse(b, circulo);
                using (var pen = new Pen(ConAlfa(acento, 120), 1.2f))
                    g.DrawEllipse(pen, circulo);
                using (var f = new Font("Segoe UI Symbol", 13f, FontStyle.Bold))
                using (var b = new SolidBrush(acento))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    g.DrawString(icono, f, b, circulo, sf);

                using (var pen = new Pen(ConAlfa(acento, 150), 2f))
                    g.DrawLine(pen, 22, tarjeta.Height - 4, tarjeta.Width - 22, tarjeta.Height - 4);
            };

            Label lblTitulo = Etiqueta(titulo, 7.5f, txtGray, true);
            lblTitulo.AutoSize = false;
            lblTitulo.AutoEllipsis = true;
            lblTitulo.Location = new Point(62, 14);
            lblTitulo.Size = new Size(120, 16);

            Label lblValor = Etiqueta("—", 21f, txtMain, true);
            lblValor.AutoSize = false;
            lblValor.AutoEllipsis = true;
            lblValor.Location = new Point(59, 28);
            lblValor.Size = new Size(120, 38);

            Label lblDetalle = Etiqueta(detalle, 7.75f, txtGray);
            lblDetalle.AutoSize = false;
            lblDetalle.AutoEllipsis = true;
            lblDetalle.Location = new Point(14, 72);
            lblDetalle.Size = new Size(160, 16);

            indicadores[indice] = lblValor;
            detallesKpi[indice] = lblDetalle;

            tarjeta.Controls.Add(lblTitulo);
            tarjeta.Controls.Add(lblValor);
            tarjeta.Controls.Add(lblDetalle);

            tarjeta.Resize += delegate
            {
                lblTitulo.Width = Math.Max(40, tarjeta.Width - 72);
                lblValor.Width = Math.Max(40, tarjeta.Width - 68);
                lblDetalle.Width = Math.Max(40, tarjeta.Width - 28);
            };

            EventHandler clic = delegate { AplicarFiltroRapido(filtro); };
            tarjeta.Click += clic;
            foreach (Control hijo in tarjeta.Controls)
            {
                hijo.Click += clic;
                hijo.Cursor = Cursors.Hand;
            }
            return tarjeta;
        }

        private Control ConstruirFiltros()
        {
            Tarjeta panel = new Tarjeta
            {
                Dock = DockStyle.Fill,
                BackColor = bgCard,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(14, 8, 14, 10)
            };

            TableLayoutPanel fila = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            fila.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            fila.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            string[] titulos = { "BUSCAR EQUIPO", "CATEGORÍA", "ESTADO", "EXISTENCIAS", "STOCK BAJO ≤", "" };
            for (int i = 0; i < titulos.Length; i++)
            {
                Label titulo = Etiqueta(titulos[i], 7.5f, txtGray, true);
                titulo.AutoSize = false;
                titulo.Dock = DockStyle.Fill;
                fila.Controls.Add(titulo, i, 0);
            }

            Tarjeta caja = new Tarjeta
            {
                Dock = DockStyle.Fill,
                BackColor = bgInput,
                Radio = 9,
                Margin = new Padding(0, 2, 10, 2)
            };
            Label lupa = new Label
            {
                Text = "⌕",
                Font = CrearFuenteSimbolos(13f, true),
                ForeColor = txtGray,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(30, 28),
                Location = new Point(8, 0),
                TextAlign = ContentAlignment.MiddleCenter
            };
            txtBuscar = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = bgInput,
                ForeColor = txtMain,
                Font = CrearFuente(9.5f)
            };
            txtBuscar.HandleCreated += delegate
            {
                SendMessage(txtBuscar.Handle, EM_SETCUEBANNER, (IntPtr)1, "Código, nombre, marca, modelo, ubicación…");
            };
            caja.Controls.Add(lupa);
            caja.Controls.Add(txtBuscar);
            caja.Resize += delegate
            {
                txtBuscar.Location = new Point(40, Math.Max(0, (caja.Height - txtBuscar.Height) / 2));
                txtBuscar.Width = Math.Max(40, caja.Width - 52);
                lupa.Top = Math.Max(0, (caja.Height - lupa.Height) / 2);
            };

            cmbCategoria = Combo("Todas");
            cmbEstado = Combo("Todos");

            cmbExistencias = Combo("Todas");
            cmbExistencias.Items.Clear();
            cmbExistencias.Items.AddRange(new object[] { "Todas", "Con existencias", "Sin existencias" });
            cmbExistencias.SelectedIndex = 0;

            nudUmbral = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 1000,
                Value = 2,
                BackColor = bgInput,
                ForeColor = txtMain,
                Font = CrearFuente(9),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 10, 0),
                TextAlign = HorizontalAlignment.Center
            };

            Button limpiar = Boton("Limpiar", EstiloBoton.Secundario, delegate { LimpiarFiltros(); }, 80, 30);
            limpiar.Dock = DockStyle.Fill;
            limpiar.Margin = new Padding(0, 3, 0, 3);

            fila.Controls.Add(caja, 0, 1);
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

        private void LimpiarFiltros()
        {
            cargando = true;
            txtBuscar.Clear();
            cmbCategoria.SelectedIndex = 0;
            cmbEstado.SelectedIndex = 0;
            cmbExistencias.SelectedIndex = 0;
            filtroRapido = "todos";
            cargando = false;
            ActualizarChips();
            AplicarFiltros();
        }

        private Control ConstruirBarraAcciones()
        {
            Panel barra = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgDark,
                Margin = new Padding(0, 0, 0, 8)
            };

            FlowLayoutPanel pestanas = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0),
                Padding = new Padding(0, 3, 0, 0),
                BackColor = Color.Transparent
            };
            btnTabEquipos = Boton("▣   Equipos", EstiloBoton.Secundario, delegate { CambiarPestana(0); }, 128, 40);
            btnTabMovimientos = Boton("⇄   Historial de movimientos", EstiloBoton.Secundario, delegate { CambiarPestana(1); }, 222, 40);
            pestanas.Controls.Add(btnTabEquipos);
            pestanas.Controls.Add(btnTabMovimientos);

            panelAcciones = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0),
                Padding = new Padding(0, 3, 0, 0),
                BackColor = Color.Transparent
            };
            btnNuevo = Boton("+  Nuevo equipo", EstiloBoton.Principal, delegate { EditarEquipo(false); }, 140, 40);
            btnEntrada = Boton("↙  Entrada", EstiloBoton.Secundario, delegate { RegistrarMovimiento("Entrada"); }, 112, 40);
            btnEntrada.ForeColor = verde;
            btnSalida = Boton("↗  Salida", EstiloBoton.Secundario, delegate { RegistrarMovimiento("Salida"); }, 108, 40);
            btnSalida.ForeColor = naranja;
            btnEditar = Boton("✎  Editar", EstiloBoton.Secundario, delegate { EditarEquipo(true); }, 104, 40);
            btnEliminar = Boton("🗑  Eliminar", EstiloBoton.Peligro, delegate { EliminarEquipo(); }, 112, 40);
            btnEliminar.Margin = new Padding(0);
            panelAcciones.Controls.AddRange(new Control[] { btnNuevo, btnEntrada, btnSalida, btnEditar, btnEliminar });
            botonesEscritura.AddRange(new[] { btnNuevo, btnEntrada, btnSalida, btnEditar, btnEliminar });

            barra.Controls.Add(panelAcciones);
            barra.Controls.Add(pestanas);
            return barra;
        }

        private void CambiarPestana(int indice)
        {
            if (!construido) return;
            pestanaActual = indice;
            bool enEquipos = indice == 0;

            paginaEquipos.Visible = enEquipos;
            paginaMovimientos.Visible = !enEquipos;
            panelAcciones.Visible = enEquipos;

            EstilizarPestana(btnTabEquipos, enEquipos);
            EstilizarPestana(btnTabMovimientos, !enEquipos);

            cmbCategoria.Enabled = cmbEstado.Enabled = cmbExistencias.Enabled = nudUmbral.Enabled = enEquipos;
            ActualizarSeleccion();
            AplicarFiltros();
            if (!enEquipos && conectado) CargarHistorial();
        }

        private void EstilizarPestana(Button boton, bool activa)
        {
            if (activa)
            {
                boton.BackColor = bgHover;
                boton.ForeColor = verde;
                boton.FlatAppearance.BorderColor = verde;
            }
            else
            {
                boton.BackColor = bgCard;
                boton.ForeColor = txtGray;
                boton.FlatAppearance.BorderColor = borde;
            }
        }

        private Control ConstruirContenido()
        {
            Panel contenedor = new Panel { Dock = DockStyle.Fill, BackColor = bgDark, Margin = new Padding(0) };
            paginaEquipos = ConstruirPaginaEquipos();
            paginaMovimientos = ConstruirPaginaMovimientos();
            contenedor.Controls.Add(paginaEquipos);
            contenedor.Controls.Add(paginaMovimientos);
            return contenedor;
        }

        private Panel ConstruirPaginaEquipos()
        {
            TableLayoutPanel pagina = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = bgDark,
                Margin = new Padding(0)
            };
            pagina.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            pagina.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350));

            Tarjeta tarjetaTabla = new Tarjeta
            {
                Dock = DockStyle.Fill,
                BackColor = bgCard,
                Margin = new Padding(0, 0, 10, 0),
                Padding = new Padding(8, 6, 8, 8)
            };

            Panel barraChips = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.Transparent, Padding = new Padding(6, 6, 6, 4) };
            lblConteo = Etiqueta("Sin registros", 8.5f, txtGray);
            lblConteo.AutoSize = false;
            lblConteo.Dock = DockStyle.Right;
            lblConteo.Width = 170;
            lblConteo.TextAlign = ContentAlignment.MiddleRight;

            FlowLayoutPanel flujoChips = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            AgregarChip(flujoChips, "todos", "Todos");
            AgregarChip(flujoChips, "disponibles", "Disponibles");
            AgregarChip(flujoChips, "asignados", "Asignados");
            AgregarChip(flujoChips, "mantenimiento", "Mantenimiento");
            AgregarChip(flujoChips, "bajo", "Stock bajo");
            AgregarChip(flujoChips, "agotados", "Agotados");
            barraChips.Controls.Add(flujoChips);
            barraChips.Controls.Add(lblConteo);

            dgvEquipos = CrearGrilla();
            dgvEquipos.CellPainting += PintarCeldaEquipo;
            dgvEquipos.SelectionChanged += delegate { ActualizarSeleccion(); };
            dgvEquipos.CellDoubleClick += delegate (object s, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex >= 0 && conectado) EditarEquipo(true);
            };
            dgvEquipos.MouseDown += delegate (object s, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.Right) return;
                DataGridView.HitTestInfo info = dgvEquipos.HitTest(e.X, e.Y);
                if (info.RowIndex >= 0)
                {
                    dgvEquipos.ClearSelection();
                    dgvEquipos.Rows[info.RowIndex].Selected = true;
                    dgvEquipos.CurrentCell = dgvEquipos.Rows[info.RowIndex].Cells[FirstVisibleColumn(dgvEquipos)];
                }
            };
            menuContextual = CrearMenuContextual();
            dgvEquipos.ContextMenuStrip = menuContextual;

            lblVacio = Etiqueta("No se encontraron equipos con los filtros actuales.", 10f, txtGray);
            lblVacio.AutoSize = false;
            lblVacio.Dock = DockStyle.Fill;
            lblVacio.TextAlign = ContentAlignment.MiddleCenter;
            lblVacio.BackColor = bgCard;
            lblVacio.Visible = false;

            Panel areaGrilla = new Panel { Dock = DockStyle.Fill, BackColor = bgCard };
            areaGrilla.Controls.Add(dgvEquipos);
            areaGrilla.Controls.Add(lblVacio);
            lblVacio.BringToFront();

            tarjetaTabla.Controls.Add(areaGrilla);
            tarjetaTabla.Controls.Add(barraChips);

            Tarjeta tarjetaDetalle = ConstruirDetalle();

            pagina.Controls.Add(tarjetaTabla, 0, 0);
            pagina.Controls.Add(tarjetaDetalle, 1, 0);

            Panel host = new Panel { Dock = DockStyle.Fill, BackColor = bgDark };
            host.Controls.Add(pagina);
            return host;
        }

        private static int FirstVisibleColumn(DataGridView grilla)
        {
            foreach (DataGridViewColumn c in grilla.Columns)
                if (c.Visible) return c.Index;
            return 0;
        }

        private void AgregarChip(FlowLayoutPanel flujo, string clave, string texto)
        {
            Button chip = new Button
            {
                Text = texto,
                Height = 30,
                Width = Math.Max(74, texto.Length * 8 + 28),
                FlatStyle = FlatStyle.Flat,
                Font = CrearFuente(8.5f, true),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 1, 8, 0),
                UseVisualStyleBackColor = false,
                TabStop = false,
                Tag = clave
            };
            chip.FlatAppearance.BorderSize = 1;
            chip.Click += delegate { AplicarFiltroRapido(clave); };
            Redondear(chip, 15);
            chips.Add(chip);
            chipPorClave[clave] = chip;
            flujo.Controls.Add(chip);
        }

        private void AplicarFiltroRapido(string clave)
        {
            filtroRapido = clave;
            if (pestanaActual != 0) CambiarPestana(0);
            ActualizarChips();
            AplicarFiltros();
        }

        private void ActualizarChips()
        {
            foreach (Button chip in chips)
            {
                bool activo = Convert.ToString(chip.Tag) == filtroRapido;
                chip.BackColor = activo ? bgHover : bgCard;
                chip.ForeColor = activo ? verde : txtGray;
                chip.FlatAppearance.BorderColor = activo ? verde : borde;
                chip.FlatAppearance.MouseOverBackColor = bgHover;
            }
        }

        // ==============================================================
        //  PANEL DE DETALLE DEL EQUIPO
        // ==============================================================
        private Tarjeta ConstruirDetalle()
        {
            Tarjeta tarjeta = new Tarjeta
            {
                Dock = DockStyle.Fill,
                BackColor = bgCard,
                Margin = new Padding(0),
                Padding = new Padding(6)
            };

            scrollDetalle = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgCard,
                AutoScroll = true
            };
            scrollDetalle.AutoScrollMinSize = new Size(0, 700);

            lblDetPlaceholder = Etiqueta("Selecciona un equipo de la tabla\npara ver su detalle aquí.", 9.5f, txtGray);
            lblDetPlaceholder.AutoSize = false;
            lblDetPlaceholder.Dock = DockStyle.Fill;
            lblDetPlaceholder.TextAlign = ContentAlignment.MiddleCenter;
            lblDetPlaceholder.BackColor = bgCard;

            contenedorDetalle = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(320, 700),
                BackColor = bgCard,
                Visible = false
            };

            int x = 16;
            int ancho = 290;

            Label titulo = Etiqueta("DETALLE DEL EQUIPO", 7.5f, txtGray, true);
            titulo.Location = new Point(x, 14);
            contenedorDetalle.Controls.Add(titulo);

            lblDetCodigo = Etiqueta("—", 9.5f, verde, true);
            lblDetCodigo.Location = new Point(x, 38);
            contenedorDetalle.Controls.Add(lblDetCodigo);

            lblDetEstado = new Label
            {
                AutoSize = true,
                Font = CrearFuente(8.25f, true),
                Padding = new Padding(10, 4, 10, 4),
                ForeColor = txtMain,
                BackColor = bgHover,
                Location = new Point(x, 64)
            };
            lblDetEstado.SizeChanged += delegate
            {
                if (lblDetEstado.Width > 0 && lblDetEstado.Height > 0)
                {
                    using (GraphicsPath p = Redondo(new Rectangle(0, 0, lblDetEstado.Width, lblDetEstado.Height), lblDetEstado.Height / 2))
                    {
                        Region anterior = lblDetEstado.Region;
                        lblDetEstado.Region = new Region(p);
                        if (anterior != null) anterior.Dispose();
                    }
                }
            };
            contenedorDetalle.Controls.Add(lblDetEstado);

            lblDetNombre = Etiqueta("—", 14f, txtMain, true);
            lblDetNombre.AutoSize = false;
            lblDetNombre.Location = new Point(x, 100);
            lblDetNombre.Size = new Size(ancho, 54);
            contenedorDetalle.Controls.Add(lblDetNombre);

            lblDetMarcaModelo = Etiqueta("—", 9f, txtGray);
            lblDetMarcaModelo.AutoSize = false;
            lblDetMarcaModelo.AutoEllipsis = true;
            lblDetMarcaModelo.Location = new Point(x, 156);
            lblDetMarcaModelo.Size = new Size(ancho, 20);
            contenedorDetalle.Controls.Add(lblDetMarcaModelo);

            contenedorDetalle.Controls.Add(Separador(x, 186, ancho));

            Label kCategoria = Etiqueta("CATEGORÍA", 7.25f, txtGray, true);
            kCategoria.Location = new Point(x, 198);
            contenedorDetalle.Controls.Add(kCategoria);
            lblDetCategoria = Etiqueta("—", 9.5f, txtMain, true);
            lblDetCategoria.AutoSize = false;
            lblDetCategoria.AutoEllipsis = true;
            lblDetCategoria.Location = new Point(x, 215);
            lblDetCategoria.Size = new Size(138, 22);
            contenedorDetalle.Controls.Add(lblDetCategoria);

            Label kUbicacion = Etiqueta("UBICACIÓN", 7.25f, txtGray, true);
            kUbicacion.Location = new Point(x + 150, 198);
            contenedorDetalle.Controls.Add(kUbicacion);
            lblDetUbicacion = Etiqueta("—", 9.5f, txtMain, true);
            lblDetUbicacion.AutoSize = false;
            lblDetUbicacion.AutoEllipsis = true;
            lblDetUbicacion.Location = new Point(x + 150, 215);
            lblDetUbicacion.Size = new Size(140, 22);
            contenedorDetalle.Controls.Add(lblDetUbicacion);

            Label kStock = Etiqueta("STOCK DISPONIBLE", 7.25f, txtGray, true);
            kStock.Location = new Point(x, 252);
            contenedorDetalle.Controls.Add(kStock);

            lblDetStock = Etiqueta("0", 20f, txtMain, true);
            lblDetStock.AutoSize = false;
            lblDetStock.Location = new Point(x - 2, 268);
            lblDetStock.Size = new Size(150, 36);
            contenedorDetalle.Controls.Add(lblDetStock);

            lblDetStockEstado = Etiqueta("", 8.5f, txtGray, true);
            lblDetStockEstado.AutoSize = false;
            lblDetStockEstado.TextAlign = ContentAlignment.MiddleRight;
            lblDetStockEstado.Location = new Point(x + 140, 276);
            lblDetStockEstado.Size = new Size(150, 22);
            contenedorDetalle.Controls.Add(lblDetStockEstado);

            barraStock = new Panel { Location = new Point(x, 310), Size = new Size(ancho, 10), BackColor = bgCard };
            barraStock.Paint += PintarBarraStock;
            contenedorDetalle.Controls.Add(barraStock);

            Label kDesc = Etiqueta("DESCRIPCIÓN", 7.25f, txtGray, true);
            kDesc.Location = new Point(x, 336);
            contenedorDetalle.Controls.Add(kDesc);

            lblDetDescripcion = Etiqueta("—", 9f, txtSoft);
            lblDetDescripcion.AutoSize = false;
            lblDetDescripcion.Location = new Point(x, 354);
            lblDetDescripcion.Size = new Size(ancho, 62);
            contenedorDetalle.Controls.Add(lblDetDescripcion);

            contenedorDetalle.Controls.Add(Separador(x, 428, ancho));

            Label kMov = Etiqueta("ÚLTIMOS MOVIMIENTOS", 7.25f, txtGray, true);
            kMov.Location = new Point(x, 440);
            contenedorDetalle.Controls.Add(kMov);

            LinkLabel verHistorial = new LinkLabel
            {
                Text = "Ver historial",
                AutoSize = true,
                Font = CrearFuente(8.25f, true),
                LinkColor = cian,
                ActiveLinkColor = verde,
                VisitedLinkColor = cian,
                LinkBehavior = LinkBehavior.HoverUnderline,
                BackColor = Color.Transparent,
                Location = new Point(x + ancho - 70, 438)
            };
            verHistorial.Click += delegate
            {
                DataRow fila = EquipoSeleccionado();
                if (fila != null) txtBuscar.Text = Convert.ToString(fila["codigo"]);
                CambiarPestana(1);
            };
            contenedorDetalle.Controls.Add(verHistorial);

            panelDetMov = new Panel
            {
                Location = new Point(x, 464),
                Size = new Size(ancho, 230),
                BackColor = bgCard
            };
            contenedorDetalle.Controls.Add(panelDetMov);

            scrollDetalle.Controls.Add(contenedorDetalle);
            scrollDetalle.Controls.Add(lblDetPlaceholder);
            lblDetPlaceholder.BringToFront();
            tarjeta.Controls.Add(scrollDetalle);
            return tarjeta;
        }

        private Panel Separador(int x, int y, int ancho)
        {
            return new Panel { Location = new Point(x, y), Size = new Size(ancho, 1), BackColor = borde };
        }

        private void PintarBarraStock(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 1, barraStock.Width - 1, barraStock.Height - 3);
            using (GraphicsPath p = Redondo(r, 4))
            using (var b = new SolidBrush(Color.FromArgb(50, 128, 147, 181)))
                g.FillPath(b, p);

            if (detStock <= 0) return;
            float fraccion = Math.Max(0.05f, Math.Min(1f, detStock / (float)Math.Max(1, maxStock)));
            var rf = new Rectangle(0, 1, Math.Max(8, (int)(r.Width * fraccion)), r.Height);
            Color c = ColorStock(detStock);
            using (GraphicsPath p = Redondo(rf, 4))
            using (var b = new SolidBrush(c))
                g.FillPath(b, p);
        }

        private Color ColorStock(long stock)
        {
            if (stock <= 0) return rojo;
            if (nudUmbral != null && stock <= (long)nudUmbral.Value) return naranja;
            return verde;
        }

        private Color ColorEstado(string estado)
        {
            string e = (estado ?? "").Trim().ToLowerInvariant();
            if (e == "disponible") return verde;
            if (e == "asignado" || e == "en uso") return azul;
            if (e == "prestado") return morado;
            if (e.Contains("mantenimiento")) return naranja;
            if (e.Contains("observaciones")) return amarillo;
            if (e == "baja") return rojo;
            return txtGray;
        }

        private void ActualizarDetalle(DataRow fila)
        {
            if (contenedorDetalle == null) return;

            if (fila == null)
            {
                contenedorDetalle.Visible = false;
                lblDetPlaceholder.Visible = true;
                lblDetPlaceholder.BringToFront();
                return;
            }

            lblDetPlaceholder.Visible = false;
            contenedorDetalle.Visible = true;

            string estado = Convert.ToString(fila["estado"]);
            Color ce = ColorEstado(estado);
            lblDetCodigo.Text = Convert.ToString(fila["codigo"]);
            lblDetEstado.Text = estado;
            lblDetEstado.ForeColor = ce;
            lblDetEstado.BackColor = Mezclar(bgCard, ce, 0.20f);
            detEstadoColorArgb = ce.ToArgb();

            lblDetNombre.Text = Convert.ToString(fila["nombre"]);

            string marca = Convert.ToString(fila["marca"]).Trim();
            string modelo = Convert.ToString(fila["modelo"]).Trim();
            string mm = (marca + "  " + modelo).Trim();
            lblDetMarcaModelo.Text = mm.Length == 0 ? "Sin marca ni modelo" : (marca.Length > 0 && modelo.Length > 0 ? marca + "  ·  " + modelo : mm);

            lblDetCategoria.Text = Convert.ToString(fila["categoria"]);
            string ubic = Convert.ToString(fila["ubicacion"]).Trim();
            lblDetUbicacion.Text = ubic.Length == 0 ? "Sin ubicación" : ubic;

            detStock = StockSeguro(fila["stock"]);
            lblDetStock.Text = detStock.ToString("N0") + (detStock == 1 ? "  unidad" : "  unidades");
            lblDetStock.ForeColor = ColorStock(detStock);

            if (detStock <= 0)
            {
                lblDetStockEstado.Text = "AGOTADO";
                lblDetStockEstado.ForeColor = rojo;
            }
            else if (detStock <= (long)nudUmbral.Value)
            {
                lblDetStockEstado.Text = "STOCK BAJO";
                lblDetStockEstado.ForeColor = naranja;
            }
            else
            {
                lblDetStockEstado.Text = "STOCK NORMAL";
                lblDetStockEstado.ForeColor = verde;
            }
            barraStock.Invalidate();

            string desc = Convert.ToString(fila["descripcion"]).Trim();
            lblDetDescripcion.Text = desc.Length == 0 ? "Sin descripción registrada." : desc;

            timerDetalle.Stop();
            panelDetMov.Controls.Clear();
            panelDetMov.Controls.Add(MiniTexto("Cargando…", 0));
            timerDetalle.Start();
        }

        private Label MiniTexto(string texto, int y)
        {
            Label l = Etiqueta(texto, 8.5f, txtGray);
            l.Location = new Point(0, y + 4);
            return l;
        }

        private void CargarMovimientosDetalle()
        {
            DataRow equipo = EquipoSeleccionado();
            if (equipo == null || !conectado || panelDetMov == null) return;

            DataTable tabla = new DataTable();
            try
            {
                const string sql = @"
SELECT x.fecha, x.tipo, x.responsable, x.detalle
FROM (
  SELECT s.fecha_salida AS fecha, 'Salida'::text AS tipo,
         COALESCE(t.nombre,'') AS responsable,
         split_part(COALESCE(s.detalles,''),' | ',1) AS detalle
  FROM public.salida_equipos se
  JOIN public.salidas s ON s.id_salida = se.id_salida
  LEFT JOIN public.trabajadores t ON t.id_trabajador = s.id_trabajador
  WHERE se.id_equipo = @id
  UNION ALL
  SELECT r.fecha_retorno,
         CASE WHEN COALESCE(r.observaciones,'') LIKE 'ENTRADA |%' THEN 'Entrada' ELSE 'Devolución' END,
         COALESCE(t.nombre,''),
         CASE WHEN COALESCE(r.observaciones,'') LIKE 'ENTRADA |%'
              THEN split_part(COALESCE(r.observaciones,''),' | ',2)
              ELSE COALESCE(r.estado_equipo,'') END
  FROM public.retornos r
  LEFT JOIN public.trabajadores t ON t.id_trabajador = r.id_trabajador
  WHERE r.id_equipo = @id
) x
ORDER BY x.fecha DESC
LIMIT 6";
                using (var conexion = AbrirConexion())
                using (var comando = new NpgsqlCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue("@id", Convert.ToInt32(equipo["id"]));
                    using (var adaptador = new NpgsqlDataAdapter(comando)) adaptador.Fill(tabla);
                }
            }
            catch (Exception)
            {
                panelDetMov.Controls.Clear();
                panelDetMov.Controls.Add(MiniTexto("No se pudo cargar el historial.", 0));
                return;
            }

            panelDetMov.Controls.Clear();
            if (tabla.Rows.Count == 0)
            {
                panelDetMov.Controls.Add(MiniTexto("Este equipo aún no tiene movimientos.", 0));
                return;
            }

            int y = 0;
            foreach (DataRow fila in tabla.Rows)
            {
                string tipo = Convert.ToString(fila["tipo"]);
                Color color = ColorTipo(tipo);
                string glifo = tipo == "Salida" ? "↗" : tipo == "Entrada" ? "↙" : "↩";
                string fecha = fila.IsNull("fecha") ? "—" : Convert.ToDateTime(fila["fecha"]).ToString("dd/MM/yyyy HH:mm");

                Label l1 = Etiqueta(glifo + "  " + tipo + "  ·  " + fecha, 8.5f, color, true);
                l1.AutoSize = false;
                l1.Location = new Point(0, y);
                l1.Size = new Size(290, 18);
                l1.AutoEllipsis = true;
                panelDetMov.Controls.Add(l1);

                string resp = Convert.ToString(fila["responsable"]);
                string det = Convert.ToString(fila["detalle"]);
                string sub = (resp + (det.Length > 0 ? "  ·  " + det : "")).Trim();
                Label l2 = Etiqueta(sub.Length == 0 ? "Sin detalle" : sub, 8f, txtGray);
                l2.AutoSize = false;
                l2.Location = new Point(18, y + 18);
                l2.Size = new Size(272, 16);
                l2.AutoEllipsis = true;
                panelDetMov.Controls.Add(l2);

                y += 40;
            }
        }

        private Color ColorTipo(string tipo)
        {
            string t = (tipo ?? "").Trim().ToLowerInvariant();
            if (t == "entrada") return verde;
            if (t == "salida") return naranja;
            if (t.StartsWith("devol")) return azul;
            if (t == "traslado") return morado;
            if (t == "mantenimiento") return amarillo;
            return txtGray;
        }

        // ==============================================================
        //  PÁGINA DE HISTORIAL DE MOVIMIENTOS
        // ==============================================================
        private Panel ConstruirPaginaMovimientos()
        {
            Tarjeta tarjeta = new Tarjeta
            {
                Dock = DockStyle.Fill,
                BackColor = bgCard,
                Margin = new Padding(0),
                Padding = new Padding(10, 8, 10, 8)
            };

            Panel filtros = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Color.Transparent };
            FlowLayoutPanel flujo = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 760,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(4, 10, 0, 0)
            };

            Label desde = Etiqueta("Desde", 8.5f, txtGray, true);
            desde.Margin = new Padding(0, 7, 6, 0);
            Label hasta = Etiqueta("Hasta", 8.5f, txtGray, true);
            hasta.Margin = new Padding(8, 7, 6, 0);

            dtDesde = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 112, Value = DateTime.Today.AddDays(-30), Margin = new Padding(0, 4, 0, 0) };
            dtHasta = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 112, Value = DateTime.Today, Margin = new Padding(0, 4, 8, 0) };

            cmbTipoHistorial = Combo("Todos los movimientos");
            cmbTipoHistorial.Dock = DockStyle.None;
            cmbTipoHistorial.Items.AddRange(new object[] { "Entrada", "Salida", "Devolución" });
            cmbTipoHistorial.Width = 190;
            cmbTipoHistorial.Margin = new Padding(8, 4, 8, 0);
            cmbTipoHistorial.SelectedIndexChanged += delegate { AplicarFiltros(); };

            Button consultar = Boton("Consultar", EstiloBoton.Principal, delegate { CargarHistorial(); }, 108, 32);
            consultar.Margin = new Padding(4, 2, 8, 0);

            Button hoy = Boton("Hoy", EstiloBoton.Secundario, delegate
            {
                dtDesde.Value = DateTime.Today; dtHasta.Value = DateTime.Today; CargarHistorial();
            }, 62, 32);
            hoy.Margin = new Padding(0, 2, 6, 0);
            Button mes = Boton("30 días", EstiloBoton.Secundario, delegate
            {
                dtDesde.Value = DateTime.Today.AddDays(-30); dtHasta.Value = DateTime.Today; CargarHistorial();
            }, 80, 32);
            mes.Margin = new Padding(0, 2, 0, 0);

            flujo.Controls.AddRange(new Control[] { desde, dtDesde, hasta, dtHasta, cmbTipoHistorial, consultar, hoy, mes });

            FlowLayoutPanel resumen = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 14, 6, 0)
            };
            lblHistSalidas = Etiqueta("Salidas: 0", 9f, naranja, true);
            lblHistEntradas = Etiqueta("Entradas: 0", 9f, verde, true);
            lblHistTotal = Etiqueta("Movimientos: 0", 9f, txtSoft, true);
            lblHistSalidas.Margin = new Padding(14, 0, 0, 0);
            lblHistEntradas.Margin = new Padding(14, 0, 0, 0);
            resumen.Controls.Add(lblHistSalidas);
            resumen.Controls.Add(lblHistEntradas);
            resumen.Controls.Add(lblHistTotal);

            filtros.Controls.Add(resumen);
            filtros.Controls.Add(flujo);

            dgvMovimientos = CrearGrilla();
            dgvMovimientos.CellPainting += PintarCeldaMovimiento;

            lblAvisoHistorial = Etiqueta("Consulta los movimientos registrados en la base de datos.", 8f, txtGray);
            lblAvisoHistorial.AutoSize = false;
            lblAvisoHistorial.Dock = DockStyle.Bottom;
            lblAvisoHistorial.Height = 24;
            lblAvisoHistorial.AutoEllipsis = true;
            lblAvisoHistorial.TextAlign = ContentAlignment.MiddleLeft;

            Panel areaGrilla = new Panel { Dock = DockStyle.Fill, BackColor = bgCard };
            areaGrilla.Controls.Add(dgvMovimientos);

            tarjeta.Controls.Add(areaGrilla);
            tarjeta.Controls.Add(lblAvisoHistorial);
            tarjeta.Controls.Add(filtros);

            Panel host = new Panel { Dock = DockStyle.Fill, BackColor = bgDark, Visible = false };
            host.Controls.Add(tarjeta);
            return host;
        }

        private Control ConstruirBarraEstado()
        {
            TableLayoutPanel barra = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = bgDark,
                Margin = new Padding(0)
            };
            barra.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            barra.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            lblEstadoConexion = Etiqueta("Conectando con la base de datos…", 8.5f, txtGray);
            lblEstadoConexion.AutoSize = false;
            lblEstadoConexion.Dock = DockStyle.Fill;
            lblEstadoConexion.TextAlign = ContentAlignment.MiddleLeft;

            lblAtajos = Etiqueta("Ctrl+N nuevo  ·  Ctrl+F buscar  ·  F5 actualizar  ·  Clic derecho: más acciones", 8f, txtGray);
            lblAtajos.Anchor = AnchorStyles.Right;
            lblAtajos.Margin = new Padding(0, 5, 0, 0);

            barra.Controls.Add(lblEstadoConexion, 0, 0);
            barra.Controls.Add(lblAtajos, 1, 0);
            return barra;
        }

        private DataGridView CrearGrilla()
        {
            DataGridView grilla = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = bgCard,
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
                GridColor = TemaApp.Borde,
                ColumnHeadersHeight = 40,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                Font = CrearFuente(8.75f)
            };
            grilla.RowTemplate.Height = 44;

            grilla.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = TemaApp.SuperficieAlt,
                ForeColor = TemaApp.Exito,
                Font = CrearFuente(8, true),
                Padding = new Padding(10, 0, 0, 0),
                SelectionBackColor = TemaApp.SuperficieAlt,
                SelectionForeColor = TemaApp.Exito
            };

            grilla.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = TemaApp.Superficie,
                ForeColor = TemaApp.Texto,
                Font = CrearFuente(8.75f),
                Padding = new Padding(10, 0, 4, 0),
                SelectionBackColor = TemaApp.EsModoOscuro ? Color.FromArgb(32, 42, 53) : Color.FromArgb(226, 243, 235),
                SelectionForeColor = TemaApp.Texto
            };

            grilla.AlternatingRowsDefaultCellStyle.BackColor = TemaApp.SuperficieAlt;
            grilla.AlternatingRowsDefaultCellStyle.ForeColor = TemaApp.Texto;
            grilla.AlternatingRowsDefaultCellStyle.SelectionBackColor = grilla.DefaultCellStyle.SelectionBackColor;
            grilla.AlternatingRowsDefaultCellStyle.SelectionForeColor = TemaApp.Texto;

            return grilla;
        }

        private void AplicarEstiloGridManual(DataGridView grid)
        {
            if (grid == null) return;

            // Fondo general
            grid.BackgroundColor = TemaApp.Superficie;
            grid.GridColor = TemaApp.Borde;
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;

            // Encabezados
            grid.ColumnHeadersDefaultCellStyle.BackColor = TemaApp.SuperficieAlt;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TemaApp.TextoTenue;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = TemaApp.SuperficieAlt;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = TemaApp.TextoTenue;

            // Filas Normales
            grid.DefaultCellStyle.BackColor = TemaApp.Superficie;
            grid.DefaultCellStyle.ForeColor = TemaApp.Texto;
            grid.DefaultCellStyle.SelectionBackColor = TemaApp.EsModoOscuro ? Color.FromArgb(32, 42, 53) : Color.FromArgb(226, 243, 235);
            grid.DefaultCellStyle.SelectionForeColor = TemaApp.Texto;

            // LA SOLUCIÓN: Forzar a que las filas alternas sean EXACTAMENTE iguales a las normales
            grid.AlternatingRowsDefaultCellStyle.BackColor = TemaApp.Superficie;
            grid.AlternatingRowsDefaultCellStyle.ForeColor = TemaApp.Texto;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = grid.DefaultCellStyle.SelectionBackColor;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = TemaApp.Texto;

            // Forzar el color del texto en cada celda al dibujarse (por si acaso)
            grid.CellFormatting -= Grid_CellFormatting_Color;
            grid.CellFormatting += Grid_CellFormatting_Color;

            // Forzar el pintado de fondo de cada fila
            grid.RowPrePaint -= Grid_RowPrePaint;
            grid.RowPrePaint += Grid_RowPrePaint;

            grid.Refresh();
        }

        private void Grid_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            var grid = sender as DataGridView;
            if (grid == null) return;

            // Forzar que el fondo de CADA fila sea el color de la superficie
            using (var brush = new SolidBrush(TemaApp.Superficie))
            {
                e.Graphics.FillRectangle(brush, e.RowBounds);
            }
        }

        private void Grid_CellFormatting_Color(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.CellStyle.BackColor == TemaApp.Superficie)
            {
                e.CellStyle.ForeColor = TemaApp.Texto;
            }
        }

        // ==============================================================
        //  MENÚ CONTEXTUAL
        // ==============================================================
        private sealed class TablaColoresMenu : ProfessionalColorTable
        {
            private static Color Fondo => TemaApp.Superficie;
            private static Color Sel => TemaApp.SuperficieAlt;
            private static Color Linea => TemaApp.Borde;

            public override Color MenuItemSelected { get { return Sel; } }
            public override Color MenuItemBorder { get { return Linea; } }
            public override Color MenuBorder { get { return Linea; } }
            public override Color ToolStripDropDownBackground { get { return Fondo; } }
            public override Color ImageMarginGradientBegin { get { return Fondo; } }
            public override Color ImageMarginGradientMiddle { get { return Fondo; } }
            public override Color ImageMarginGradientEnd { get { return Fondo; } }
            public override Color SeparatorDark { get { return Linea; } }
            public override Color SeparatorLight { get { return Linea; } }
            public override Color MenuItemSelectedGradientBegin { get { return Sel; } }
            public override Color MenuItemSelectedGradientEnd { get { return Sel; } }
            public override Color MenuItemPressedGradientBegin { get { return Sel; } }
            public override Color MenuItemPressedGradientMiddle { get { return Sel; } }
            public override Color MenuItemPressedGradientEnd { get { return Sel; } }
        }

        private ContextMenuStrip CrearMenuContextual()
        {
            ContextMenuStrip menu = new ContextMenuStrip
            {
                BackColor = bgCard,
                ForeColor = txtSoft,
                ShowImageMargin = false,
                Font = CrearFuente(9),
                Renderer = new ToolStripProfessionalRenderer(new TablaColoresMenu())
            };

            ToolStripMenuItem Item(string texto, EventHandler accion)
            {
                var it = new ToolStripMenuItem(texto) { BackColor = bgCard, ForeColor = txtSoft };
                it.Click += accion;
                return it;
            }

            menu.Items.Add(Item("✎   Editar equipo", delegate { EditarEquipo(true); }));
            menu.Items.Add(Item("↙   Registrar entrada", delegate { RegistrarMovimiento("Entrada"); }));
            menu.Items.Add(Item("↗   Registrar salida", delegate { RegistrarMovimiento("Salida"); }));
            menu.Items.Add(new ToolStripSeparator());

            var estados = new ToolStripMenuItem("◉   Cambiar estado a…") { BackColor = bgCard, ForeColor = txtSoft };
            foreach (string estado in EstadosEquipo)
            {
                string valor = estado;
                var it = new ToolStripMenuItem(valor) { BackColor = bgCard, ForeColor = ColorEstado(valor) };
                it.Click += delegate { CambiarEstado(valor); };
                estados.DropDownItems.Add(it);
            }
            menu.Items.Add(estados);
            menu.Items.Add(Item("⧉   Duplicar equipo", delegate { DuplicarEquipo(); }));
            menu.Items.Add(Item("⎘   Copiar código", delegate
            {
                DataRow fila = EquipoSeleccionado();
                if (fila != null) Clipboard.SetText(Convert.ToString(fila["codigo"]));
            }));
            menu.Items.Add(new ToolStripSeparator());
            var eliminar = Item("🗑   Eliminar equipo", delegate { EliminarEquipo(); });
            eliminar.ForeColor = rojo;
            menu.Items.Add(eliminar);

            menu.Opening += delegate (object s, System.ComponentModel.CancelEventArgs e)
            {
                if (!conectado || ocupado || EquipoSeleccionado() == null) e.Cancel = true;
            };
            return menu;
        }

        // ==============================================================
        //  PINTADO DE CELDAS (badges, barra de stock)
        // ==============================================================
        private void DibujarBadge(Graphics g, string texto, Color acento, Rectangle celda)
        {
            using (Font f = new Font("Segoe UI", 8f, FontStyle.Bold))
            {
                SizeF tam = g.MeasureString(texto, f);
                int w = Math.Min(celda.Width - 12, (int)tam.Width + 22);
                int h = 22;
                var pill = new Rectangle(celda.X + 8, celda.Y + (celda.Height - h) / 2, Math.Max(30, w), h);
                using (GraphicsPath p = Redondo(pill, h / 2))
                {
                    using (var b = new SolidBrush(ConAlfa(acento, TemaApp.EsModoOscuro ? 38 : 50))) g.FillPath(b, p);
                    using (var pen = new Pen(ConAlfa(acento, TemaApp.EsModoOscuro ? 110 : 150), 1)) g.DrawPath(pen, p);
                }
                using (var b = new SolidBrush(acento))
                using (var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                })
                    g.DrawString(texto, f, b, pill, sf);
            }
        }

        private void PintarCeldaEquipo(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string col = dgvEquipos.Columns[e.ColumnIndex].Name;
            if (col != "estado" && col != "stock") return;

            e.Paint(e.CellBounds, DataGridViewPaintParts.Background |
                                  DataGridViewPaintParts.Border |
                                  DataGridViewPaintParts.SelectionBackground);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            if (col == "estado")
            {
                string texto = Convert.ToString(e.Value ?? "").Trim();
                if (texto.Length > 0) DibujarBadge(g, texto, ColorEstado(texto), e.CellBounds);
            }
            else
            {
                long stock = StockSeguro(e.Value);
                Color c = ColorStock(stock);
                var celda = e.CellBounds;

                using (Font f = new Font("Segoe UI", 9f, FontStyle.Bold))
                using (var b = new SolidBrush(c))
                using (var sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near })
                    g.DrawString(stock.ToString("N0"), f, b, new Rectangle(celda.X + 10, celda.Y, 44, celda.Height), sf);

                int xBarra = celda.X + 56;
                int anchoBarra = celda.Width - 56 - 16;
                if (anchoBarra >= 30)
                {
                    var pista = new Rectangle(xBarra, celda.Y + (celda.Height - 6) / 2, anchoBarra, 6);
                    using (GraphicsPath p = Redondo(pista, 3))
                    using (var b = new SolidBrush(Color.FromArgb(50, 128, 147, 181)))
                        g.FillPath(b, p);

                    if (stock > 0)
                    {
                        float fr = Math.Max(0.06f, Math.Min(1f, stock / (float)Math.Max(1, maxStock)));
                        var relleno = new Rectangle(pista.X, pista.Y, Math.Max(6, (int)(pista.Width * fr)), pista.Height);
                        using (GraphicsPath p = Redondo(relleno, 3))
                        using (var b = new SolidBrush(c))
                            g.FillPath(b, p);
                    }
                }
            }
            e.Handled = true;
        }

        private void PintarCeldaMovimiento(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string col = dgvMovimientos.Columns[e.ColumnIndex].Name;
            if (col != "tipo") return;

            e.Paint(e.CellBounds, DataGridViewPaintParts.Background |
                                  DataGridViewPaintParts.Border |
                                  DataGridViewPaintParts.SelectionBackground);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            string texto = Convert.ToString(e.Value ?? "").Trim();
            if (texto.Length > 0) DibujarBadge(g, texto, ColorTipo(texto), e.CellBounds);
            e.Handled = true;
        }

        // ==============================================================
        //  ACCESO A DATOS
        // ==============================================================
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

            int idSeleccionado = -1;
            DataRow filaPrevia = EquipoSeleccionado();
            if (filaPrevia != null) int.TryParse(Convert.ToString(filaPrevia["id"]), out idSeleccionado);

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

                maxStock = 1;
                foreach (DataRow fila in nueva.Rows)
                    maxStock = Math.Max(maxStock, StockSeguro(fila["stock"]));

                PoblarFiltro(cmbCategoria, "Todas", "categoria");
                PoblarFiltro(cmbEstado, "Todos", "estado");
                ConfigurarColumnasEquipos();
                EstablecerConexion(true);
                ActualizarIndicadores();
                lblEstadoConexion.Text = "●  Conectado a Supabase · Actualizado " + DateTime.Now.ToString("HH:mm:ss");
                lblEstadoConexion.ForeColor = verde;
            }
            catch (Exception ex)
            {
                dgvEquipos.DataSource = null;
                if (datosEquipos != null) { datosEquipos.Dispose(); datosEquipos = null; }
                EstablecerConexion(false);
                ActualizarIndicadores();
                lblEstadoConexion.Text = "●  Error de conexión o consulta: " + ex.Message;
                lblEstadoConexion.ForeColor = rojo;
                MostrarError("No se pudo cargar el inventario", ex);
            }
            finally
            {
                cargando = false;
                AplicarFiltros();
                RestaurarSeleccion(idSeleccionado);
                ActualizarSeleccion();
            }
        }

        private void RestaurarSeleccion(int id)
        {
            if (id < 0 || dgvEquipos == null) return;
            foreach (DataGridViewRow fila in dgvEquipos.Rows)
            {
                DataRowView vista = fila.DataBoundItem as DataRowView;
                if (vista == null) continue;
                int actual;
                if (int.TryParse(Convert.ToString(vista.Row["id"]), out actual) && actual == id)
                {
                    dgvEquipos.ClearSelection();
                    fila.Selected = true;
                    int col = FirstVisibleColumn(dgvEquipos);
                    dgvEquipos.CurrentCell = fila.Cells[col];
                    return;
                }
            }
        }

        private void ConfigurarColumnasEquipos()
        {
            if (dgvEquipos.Columns.Count == 0) return;
            string[] nombres = { "codigo", "nombre", "categoria", "marca", "modelo", "ubicacion", "stock", "estado" };
            string[] titulos = { "CÓDIGO", "EQUIPO", "CATEGORÍA", "MARCA", "MODELO", "UBICACIÓN", "STOCK", "ESTADO" };
            float[] pesos = { 90, 190, 120, 95, 100, 120, 130, 130 };

            for (int i = 0; i < nombres.Length; i++)
            {
                if (!dgvEquipos.Columns.Contains(nombres[i])) continue;
                DataGridViewColumn columna = dgvEquipos.Columns[nombres[i]];
                columna.HeaderText = titulos[i];
                columna.MinimumWidth = i == 1 ? 150 : (nombres[i] == "stock" ? 120 : (nombres[i] == "estado" ? 115 : 80));
                columna.Visible = true;
                columna.FillWeight = pesos[i];
                columna.SortMode = DataGridViewColumnSortMode.Automatic;
                columna.DisplayIndex = i;
            }
            foreach (string oculta in new[] { "id", "numero_serie", "descripcion", "id_ubicacion" })
                if (dgvEquipos.Columns.Contains(oculta)) dgvEquipos.Columns[oculta].Visible = false;

            if (dgvEquipos.Columns.Contains("codigo"))
                dgvEquipos.Columns["codigo"].DefaultCellStyle.ForeColor = cian;
            if (dgvEquipos.Columns.Contains("nombre"))
                dgvEquipos.Columns["nombre"].DefaultCellStyle.Font = CrearFuente(8.75f, true);
        }

        private void ConfigurarColumnasMovimientos()
        {
            if (dgvMovimientos.Columns.Count == 0) return;
            string[] nombres = { "fecha", "hora", "tipo", "codigo", "equipo", "cantidad", "responsable", "motivo", "origen", "destino", "observacion" };
            string[] titulos = { "FECHA", "HORA", "MOVIMIENTO", "CÓDIGO", "EQUIPO", "CANT.", "RESPONSABLE", "MOTIVO", "ORIGEN", "DESTINO", "OBSERVACIÓN" };
            float[] pesos = { 85, 60, 110, 90, 160, 55, 120, 140, 100, 100, 140 };

            for (int i = 0; i < nombres.Length; i++)
            {
                if (!dgvMovimientos.Columns.Contains(nombres[i])) continue;
                DataGridViewColumn c = dgvMovimientos.Columns[nombres[i]];
                c.HeaderText = titulos[i];
                c.MinimumWidth = nombres[i] == "cantidad" ? 52 : (nombres[i] == "hora" ? 56 : (nombres[i] == "tipo" ? 110 : 80));
                c.Visible = true;
                c.FillWeight = pesos[i];
                c.DisplayIndex = i;
            }
            foreach (string oculta in new[] { "id_movimiento", "id_equipo", "responsable_id" })
                if (dgvMovimientos.Columns.Contains(oculta)) dgvMovimientos.Columns[oculta].Visible = false;

            if (dgvMovimientos.Columns.Contains("cantidad"))
                dgvMovimientos.Columns["cantidad"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (dgvMovimientos.Columns.Contains("codigo"))
                dgvMovimientos.Columns["codigo"].DefaultCellStyle.ForeColor = cian;
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

        private static long StockSeguro(object valor)
        {
            if (valor == null || valor == DBNull.Value) return 0;
            try
            {
                decimal numero = Convert.ToDecimal(valor, CultureInfo.InvariantCulture);
                if (numero < 0) return 0;
                return (long)decimal.Truncate(numero);
            }
            catch { return 0; }
        }

        private void ActualizarIndicadores()
        {
            if (indicadores[0] == null) return;
            if (datosEquipos == null)
            {
                foreach (Label x in indicadores) if (x != null) x.Text = "—";
                return;
            }

            long umbral = (long)nudUmbral.Value;
            int disponibles = 0, mantenimiento = 0, asignados = 0, bajo = 0, agotados = 0;
            long unidades = 0;

            foreach (DataRow fila in datosEquipos.Rows)
            {
                string estado = Convert.ToString(fila["estado"]).Trim();
                long stock = StockSeguro(fila["stock"]);
                unidades += stock;

                if (estado.Equals("Disponible", StringComparison.OrdinalIgnoreCase) && stock > 0) disponibles++;
                if (estado.IndexOf("mantenimiento", StringComparison.OrdinalIgnoreCase) >= 0) mantenimiento++;
                if (estado.Equals("Asignado", StringComparison.OrdinalIgnoreCase) ||
                    estado.Equals("En uso", StringComparison.OrdinalIgnoreCase) ||
                    estado.Equals("Prestado", StringComparison.OrdinalIgnoreCase)) asignados++;

                if (stock == 0) agotados++;
                else if (stock <= umbral) bajo++;
            }

            indicadores[0].Text = datosEquipos.Rows.Count.ToString("N0");
            indicadores[1].Text = unidades.ToString("N0");
            indicadores[2].Text = disponibles.ToString("N0");
            indicadores[3].Text = asignados.ToString("N0");
            indicadores[4].Text = mantenimiento.ToString("N0");
            indicadores[5].Text = (agotados + bajo).ToString("N0");
            indicadores[5].ForeColor = (agotados + bajo) > 0 ? rojo : txtMain;
            detallesKpi[5].Text = agotados + " agotados · " + bajo + " con stock bajo";
        }

        private static string EscaparFiltro(string valor)
        {
            return (valor ?? "").Replace("'", "''").Replace("[", "[[]").Replace("]", "[]]").Replace("%", "[%]").Replace("*", "[*]");
        }

        private void AplicarFiltros()
        {
            if (!construido || cargando || lblConteo == null) return;
            bool equiposSeleccionados = pestanaActual == 0;
            DataTable tabla = equiposSeleccionados ? datosEquipos : datosMovimientos;
            if (tabla == null)
            {
                lblConteo.Text = "Sin registros";
                if (lblVacio != null) lblVacio.Visible = false;
                return;
            }

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

                switch (filtroRapido)
                {
                    case "disponibles": condiciones.Add("[estado] = 'Disponible' AND [stock] > 0"); break;
                    case "asignados": condiciones.Add("([estado] = 'Asignado' OR [estado] = 'En uso' OR [estado] = 'Prestado')"); break;
                    case "mantenimiento": condiciones.Add("[estado] LIKE '%antenimiento%'"); break;
                    case "bajo": condiciones.Add("[stock] <= " + ((long)nudUmbral.Value).ToString(CultureInfo.InvariantCulture)); break;
                    case "agotados": condiciones.Add("[stock] = 0"); break;
                }
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

            int visibles = tabla.DefaultView.Count;
            lblConteo.Text = visibles.ToString("N0") + (visibles == 1 ? " registro" : " registros");
            if (lblVacio != null) lblVacio.Visible = equiposSeleccionados && visibles == 0 && datosEquipos != null;
            if (lblVacio != null && lblVacio.Visible) lblVacio.BringToFront();
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

        private int ultimoIdDetalle = -1;

        private void ActualizarSeleccion()
        {
            if (dgvEquipos == null || btnEntrada == null) return;
            DataRow fila = EquipoSeleccionado();
            bool habilitado = conectado && !ocupado && pestanaActual == 0 && fila != null;
            btnEntrada.Enabled = btnSalida.Enabled = btnEditar.Enabled = btnEliminar.Enabled = habilitado;

            int id = -1;
            if (fila != null) int.TryParse(Convert.ToString(fila["id"]), out id);

            if (id == ultimoIdDetalle && id != -1 && contenedorDetalle != null && contenedorDetalle.Visible) return;
            ultimoIdDetalle = id;
            ActualizarDetalle(fila);
        }

        // ==============================================================
        //  HISTORIAL
        // ==============================================================
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
       x.codigo, x.equipo, x.cantidad, x.responsable, x.motivo, x.origen, x.destino,
       x.observacion, x.responsable_id, x.id_equipo
FROM (
 SELECT se.id AS id_movimiento, s.fecha_salida AS fecha_evento, 'Salida'::text AS tipo,
        e.codigo, e.nombre AS equipo,
        COALESCE(NULLIF(substring(COALESCE(s.detalles,'') from 'Cantidad: ([0-9]+)'),'')::int, 1) AS cantidad,
        COALESCE(t.nombre,'') AS responsable,
        split_part(COALESCE(s.detalles,''), ' | ', 1) AS motivo,
        COALESCE(e.ubicacion,'') AS origen,
        COALESCE(s.ubicacion,'') AS destino,
        ''::text AS observacion,
        s.id_trabajador AS responsable_id, e.id AS id_equipo
 FROM public.salida_equipos se JOIN public.salidas s ON s.id_salida=se.id_salida
 JOIN public.equipos e ON e.id=se.id_equipo
 LEFT JOIN public.trabajadores t ON t.id_trabajador=s.id_trabajador
 UNION ALL
 SELECT -r.id_retorno AS id_movimiento, r.fecha_retorno AS fecha_evento,
        CASE WHEN COALESCE(r.observaciones,'') LIKE 'ENTRADA |%' THEN 'Entrada' ELSE 'Devolución' END AS tipo,
        COALESCE(e.codigo,''), COALESCE(e.nombre,''),
        COALESCE(NULLIF(substring(COALESCE(r.observaciones,'') from 'Cantidad: ([0-9]+)'),'')::int, 1),
        COALESCE(t.nombre,''),
        CASE WHEN COALESCE(r.observaciones,'') LIKE 'ENTRADA |%'
             THEN split_part(COALESCE(r.observaciones,''), ' | ', 2)
             ELSE COALESCE(r.estado_equipo,'') END,
        COALESCE(btrim(substring(COALESCE(r.observaciones,'') from 'Origen: ([^|]*)')),''),
        COALESCE(e.ubicacion,''),
        CASE WHEN COALESCE(r.observaciones,'') LIKE 'ENTRADA |%'
             THEN '' ELSE COALESCE(r.observaciones,'') END,
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

                long entradas = 0, salidas = 0;
                foreach (DataRow fila in nuevo.Rows)
                {
                    long cant = StockSeguro(fila["cantidad"]);
                    string tipo = Convert.ToString(fila["tipo"]);
                    if (tipo == "Entrada" || tipo == "Devolución") entradas += cant;
                    else if (tipo == "Salida") salidas += cant;
                }
                lblHistTotal.Text = "Movimientos: " + nuevo.Rows.Count.ToString("N0");
                lblHistEntradas.Text = "Entradas: " + entradas.ToString("N0") + " u.";
                lblHistSalidas.Text = "Salidas: " + salidas.ToString("N0") + " u.";

                lblAvisoHistorial.Text = "Historial de entradas, salidas y devoluciones registradas en Supabase.";
                AplicarFiltros();
            }
            catch (Exception ex) { MostrarError("No se pudo cargar el historial", ex); }
        }

        private void ActualizarVista()
        {
            CargarDatosInventario();
            if (conectado && pestanaActual == 1) CargarHistorial();
        }

        // ==============================================================
        //  DIÁLOGOS (se adaptan al tema claro/oscuro de la ventana)
        // ==============================================================
        private sealed class PaletaDlg
        {
            public Color Fondo, Input, Texto, Suave, Gris, Borde;
        }

        private PaletaDlg Paleta()
        {
            bool oscuro = BackColor.GetBrightness() < 0.5f;
            if (oscuro)
                return new PaletaDlg
                {
                    Fondo = bgCard,
                    Input = bgInput,
                    Texto = txtMain,
                    Suave = txtSoft,
                    Gris = txtGray,
                    Borde = borde
                };

            return new PaletaDlg
            {
                Fondo = Color.White,
                Input = Color.FromArgb(247, 250, 252),
                Texto = Color.FromArgb(23, 36, 48),
                Suave = Color.FromArgb(60, 76, 90),
                Gris = Color.FromArgb(96, 112, 126),
                Borde = Color.FromArgb(214, 225, 231)
            };
        }

        private Form CrearDialogo(string titulo, Size tamano, PaletaDlg pal)
        {
            return new Form
            {
                Text = titulo,
                ClientSize = tamano,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = pal.Fondo,
                ForeColor = pal.Texto,
                Font = Font,
                ShowInTaskbar = false,
                AutoScaleMode = AutoScaleMode.None
            };
        }

        private Button BotonDialogo(string texto, bool principal, int ancho, PaletaDlg pal)
        {
            Button b = new Button
            {
                Text = texto,
                Width = ancho,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                Font = CrearFuente(9, true),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderSize = 1;
            if (principal)
            {
                b.BackColor = verde;
                b.ForeColor = Color.Black;
                b.FlatAppearance.BorderColor = verde;
                b.FlatAppearance.MouseOverBackColor = verdeHover;
            }
            else
            {
                b.BackColor = pal.Input;
                b.ForeColor = pal.Suave;
                b.FlatAppearance.BorderColor = pal.Borde;
                b.FlatAppearance.MouseOverBackColor = pal.Borde;
            }
            Redondear(b, 10);
            return b;
        }

        private Label EtiquetaDialogo(Form dlg, string texto, int x, int y, PaletaDlg pal)
        {
            Label l = Etiqueta(texto, 7.75f, pal.Gris, true);
            l.Location = new Point(x, y);
            dlg.Controls.Add(l);
            return l;
        }

        private TextBox CampoDialogo(Form dlg, string titulo, int x, int y, int ancho, string valor, int maximo, PaletaDlg pal, bool multilinea = false, int alto = 28)
        {
            EtiquetaDialogo(dlg, titulo, x, y, pal);
            TextBox campo = new TextBox
            {
                Location = new Point(x, y + 20),
                Size = new Size(ancho, multilinea ? alto : 27),
                Text = valor ?? "",
                MaxLength = maximo,
                Font = CrearFuente(9.5f),
                BackColor = pal.Input,
                ForeColor = pal.Texto,
                BorderStyle = BorderStyle.FixedSingle,
                Multiline = multilinea,
                ScrollBars = multilinea ? ScrollBars.Vertical : ScrollBars.None
            };
            dlg.Controls.Add(campo);
            return campo;
        }

        private ComboBox ComboDialogo(Form dlg, string titulo, int x, int y, int ancho, IEnumerable<string> opciones, string valor, PaletaDlg pal)
        {
            EtiquetaDialogo(dlg, titulo, x, y, pal);
            ComboBox combo = new ComboBox
            {
                Location = new Point(x, y + 20),
                Size = new Size(ancho, 27),
                DropDownStyle = ComboBoxStyle.DropDown,
                FlatStyle = FlatStyle.Flat,
                Font = CrearFuente(9.5f),
                BackColor = pal.Input,
                ForeColor = pal.Texto,
                MaxLength = 150
            };
            foreach (string o in opciones) combo.Items.Add(o);
            combo.Text = valor ?? "";
            dlg.Controls.Add(combo);
            return combo;
        }

        private IEnumerable<string> ValoresDistintos(string columna, params string[] adicionales)
        {
            SortedSet<string> valores = new SortedSet<string>(StringComparer.CurrentCultureIgnoreCase);
            foreach (string a in adicionales) valores.Add(a);
            if (datosEquipos != null)
                foreach (DataRow fila in datosEquipos.Rows)
                    if (!fila.IsNull(columna))
                    {
                        string v = Convert.ToString(fila[columna]).Trim();
                        if (v.Length > 0) valores.Add(v);
                    }
            return valores;
        }

        // ==============================================================
        //  ALTA / EDICIÓN / DUPLICADO DE EQUIPO
        // ==============================================================
        private void EditarEquipo(bool editar)
        {
            AbrirDialogoEquipo(editar, editar ? EquipoSeleccionado() : null, false);
        }

        private void DuplicarEquipo()
        {
            DataRow fila = EquipoSeleccionado();
            if (fila == null) return;
            AbrirDialogoEquipo(false, fila, true);
        }

        private string GenerarCodigo(string categoria)
        {
            string limpia = (categoria ?? "").Normalize(NormalizationForm.FormD);
            StringBuilder letras = new StringBuilder();
            foreach (char ch in limpia)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetter(ch)) letras.Append(char.ToUpperInvariant(ch));
                if (letras.Length == 3) break;
            }
            string prefijo = letras.Length >= 2 ? letras.ToString() : "EQ";

            int maximo = 0;
            HashSet<string> existentes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (datosEquipos != null)
            {
                foreach (DataRow fila in datosEquipos.Rows)
                {
                    string cod = Convert.ToString(fila["codigo"]).Trim();
                    existentes.Add(cod);
                    if (!cod.StartsWith(prefijo + "-", StringComparison.OrdinalIgnoreCase)) continue;
                    int n;
                    if (int.TryParse(cod.Substring(prefijo.Length + 1), out n)) maximo = Math.Max(maximo, n);
                }
            }

            string nuevo;
            do
            {
                maximo++;
                nuevo = prefijo + "-" + maximo.ToString("000");
            } while (existentes.Contains(nuevo));
            return nuevo;
        }

        private void AbrirDialogoEquipo(bool editar, DataRow origen, bool duplicar)
        {
            if (!conectado || ocupado) return;
            if (editar && origen == null) return;

            PaletaDlg pal = Paleta();
            bool usarOrigen = origen != null;

            using (var dialogo = CrearDialogo(editar ? "Editar equipo" : (duplicar ? "Duplicar equipo" : "Nuevo equipo"), new Size(560, 590), pal))
            {
                Label titulo = Etiqueta(editar ? "Editar equipo audiovisual" : (duplicar ? "Duplicar equipo audiovisual" : "Registrar equipo audiovisual"), 16, pal.Texto, true);
                titulo.Location = new Point(24, 18);
                dialogo.Controls.Add(titulo);

                Label subtitulo = Etiqueta("Los campos marcados con * son obligatorios.", 8.5f, pal.Gris);
                subtitulo.Location = new Point(26, 52);
                dialogo.Controls.Add(subtitulo);

                string valCodigo = editar ? Convert.ToString(origen["codigo"]) : "";
                TextBox codigo = CampoDialogo(dialogo, "CÓDIGO *", 24, 84, 372, valCodigo, 80, pal);

                TextBox nombre = CampoDialogo(dialogo, "NOMBRE *", 24, 138, 512,
                    usarOrigen ? Convert.ToString(origen["nombre"]) : "", 120, pal);

                ComboBox categoria = ComboDialogo(dialogo, "CATEGORÍA", 24, 192, 250,
                    ValoresDistintos("categoria", "General", "Cámaras y Lentes", "Audio y Micrófonos", "Iluminación", "Trípodes y Soportes"),
                    usarOrigen ? Convert.ToString(origen["categoria"]) : "General", pal);

                TextBox marca = CampoDialogo(dialogo, "MARCA", 286, 192, 250,
                    usarOrigen ? Convert.ToString(origen["marca"]) : "", 100, pal);

                TextBox modelo = CampoDialogo(dialogo, "MODELO", 24, 246, 250,
                    usarOrigen ? Convert.ToString(origen["modelo"]) : "", 100, pal);

                ComboBox ubicacion = ComboDialogo(dialogo, "UBICACIÓN", 286, 246, 250,
                    ValoresDistintos("ubicacion", "Almacén Principal"),
                    usarOrigen ? Convert.ToString(origen["ubicacion"]) : "Almacén Principal", pal);

                ComboBox estado = ComboDialogo(dialogo, "ESTADO", 24, 300, 250, EstadosEquipo,
                    usarOrigen && !duplicar ? Convert.ToString(origen["estado"]) : "Disponible", pal);

                EtiquetaDialogo(dialogo, "STOCK (UNIDADES) *", 286, 300, pal);
                NumericUpDown stock = new NumericUpDown
                {
                    Location = new Point(286, 320),
                    Size = new Size(160, 27),
                    Minimum = 0,
                    Maximum = int.MaxValue,
                    ThousandsSeparator = true,
                    Font = CrearFuente(9.5f),
                    BackColor = pal.Input,
                    ForeColor = pal.Texto,
                    TextAlign = HorizontalAlignment.Right
                };
                try
                {
                    stock.Value = editar && origen["stock"] != DBNull.Value
                        ? Convert.ToDecimal(origen["stock"], CultureInfo.InvariantCulture)
                        : (duplicar ? 1m : 0m);
                }
                catch { stock.Value = 0m; }
                dialogo.Controls.Add(stock);

                Label notaStock = Etiqueta("Si el equipo ya existe, usa Entrada para sumar stock.", 7.75f, pal.Gris);
                notaStock.Location = new Point(286, 352);
                dialogo.Controls.Add(notaStock);

                TextBox descripcion = CampoDialogo(dialogo, "DESCRIPCIÓN", 24, 380, 512,
                    usarOrigen ? Convert.ToString(origen["descripcion"]) : "", 1000, pal, true, 84);

                Button generar = BotonDialogo("✦  Generar", false, 128, pal);
                generar.Location = new Point(408, 102);
                generar.Height = 28;
                generar.Click += delegate { codigo.Text = GenerarCodigo(categoria.Text); };
                dialogo.Controls.Add(generar);

                Button cancelar = BotonDialogo("Cancelar", false, 110, pal);
                cancelar.Location = new Point(300, 520);
                dialogo.Controls.Add(cancelar);
                cancelar.Click += delegate { dialogo.Close(); };

                Button guardar = BotonDialogo(editar ? "Guardar cambios" : "Guardar equipo", true, 150, pal);
                guardar.Location = new Point(386, 520);
                dialogo.Controls.Add(guardar);
                dialogo.CancelButton = cancelar;

                if (!editar && !duplicar) codigo.Text = GenerarCodigo(categoria.Text);

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
                            editar ? Convert.ToInt32(origen["id"]) : 0,
                            codigo.Text,
                            nombre.Text,
                            string.IsNullOrWhiteSpace(categoria.Text) ? "General" : categoria.Text.Trim(),
                            marca.Text,
                            modelo.Text,
                            "",
                            string.IsNullOrWhiteSpace(ubicacion.Text) ? "Almacén Principal" : ubicacion.Text.Trim(),
                            string.IsNullOrWhiteSpace(estado.Text) ? "Disponible" : estado.Text.Trim(),
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
        //  CAMBIAR ESTADO / ELIMINAR
        // ------------------------------------------------------------------
        private void CambiarEstado(string nuevoEstado)
        {
            DataRow fila = EquipoSeleccionado();
            if (!conectado || ocupado || fila == null) return;

            ocupado = true;
            EstablecerConexion(conectado);
            try
            {
                using (var conexion = AbrirConexion())
                using (var cmd = new NpgsqlCommand("UPDATE public.equipos SET estado=@estado WHERE id=@id", conexion))
                {
                    cmd.Parameters.AddWithValue("@estado", nuevoEstado);
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(fila["id"]));
                    if (cmd.ExecuteNonQuery() != 1)
                        throw new InvalidOperationException("No se pudo cambiar el estado del equipo.");
                }
            }
            catch (Exception ex)
            {
                MostrarError("No se pudo cambiar el estado", ex);
                return;
            }
            finally
            {
                ocupado = false;
                EstablecerConexion(conectado);
            }
            CargarDatosInventario();
        }

        private void EliminarEquipo()
        {
            DataRow fila = EquipoSeleccionado();
            if (!conectado || ocupado || fila == null) return;

            string cod = Convert.ToString(fila["codigo"]);
            string nom = Convert.ToString(fila["nombre"]);
            int id = Convert.ToInt32(fila["id"]);

            DialogResult confirmar = MessageBox.Show(FindForm(),
                "¿Eliminar definitivamente el equipo " + cod + " · " + nom + "?\n\nEsta acción no se puede deshacer.",
                "Eliminar equipo", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (confirmar != DialogResult.Yes) return;

            bool ofrecerBaja = false;
            ocupado = true;
            EstablecerConexion(conectado);
            try
            {
                using (var conexion = AbrirConexion())
                using (var cmd = new NpgsqlCommand("DELETE FROM public.equipos WHERE id=@id", conexion))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (PostgresException ex) when (ex.SqlState == "23503")
            {
                ofrecerBaja = true;
            }
            catch (Exception ex)
            {
                MostrarError("No se pudo eliminar el equipo", ex);
                return;
            }
            finally
            {
                ocupado = false;
                EstablecerConexion(conectado);
            }

            if (ofrecerBaja)
            {
                DialogResult baja = MessageBox.Show(FindForm(),
                    "Este equipo tiene movimientos registrados y no puede eliminarse.\n\n¿Deseas marcarlo como \"Baja\" para conservar su historial?",
                    "Equipo con historial", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (baja == DialogResult.Yes) CambiarEstado("Baja");
                return;
            }

            ultimoIdDetalle = -1;
            CargarDatosInventario();
        }

        // ==============================================================
        //  MOVIMIENTOS (Entrada / Salida)
        // ==============================================================
        private DataTable CargarTrabajadoresActivos()
        {
            var tabla = new DataTable();
            using (var conexion = AbrirConexion())
            using (var adaptador = new NpgsqlDataAdapter(@"SELECT id_trabajador AS id_responsable,
 nombre AS nombre_completo FROM public.trabajadores WHERE activo=true ORDER BY nombre", conexion))
                adaptador.Fill(tabla);
            return tabla;
        }

        private static string LimpiarTexto(string valor)
        {
            return (valor ?? "").Replace("|", "/").Trim();
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

            bool esEntrada = string.Equals(tipo, "Entrada", StringComparison.OrdinalIgnoreCase);
            int stockActual = (int)Math.Min(int.MaxValue, StockSeguro(equipo["stock"]));
            string operationId = Guid.NewGuid().ToString("N");
            PaletaDlg pal = Paleta();

            using (Form dialogo = CrearDialogo(tipo + " de equipo", new Size(540, 470), pal))
            {
                Label titulo = Etiqueta("Registrar " + tipo.ToLowerInvariant(), 17, pal.Texto, true);
                titulo.Location = new Point(24, 16);
                dialogo.Controls.Add(titulo);

                Label detalleEquipo = Etiqueta(Convert.ToString(equipo["codigo"]) + "  ·  " + Convert.ToString(equipo["nombre"]), 9f, esEntrada ? verde : naranja, true);
                detalleEquipo.AutoSize = false;
                detalleEquipo.Location = new Point(26, 52);
                detalleEquipo.Size = new Size(490, 22);
                detalleEquipo.AutoEllipsis = true;
                dialogo.Controls.Add(detalleEquipo);

                EtiquetaDialogo(dialogo, "CANTIDAD DE UNIDADES *", 24, 88, pal);
                NumericUpDown cantidad = new NumericUpDown
                {
                    Minimum = 1,
                    Maximum = int.MaxValue,
                    Value = 1,
                    ThousandsSeparator = true,
                    Location = new Point(24, 108),
                    Size = new Size(150, 27),
                    Font = CrearFuente(9.5f),
                    BackColor = pal.Input,
                    ForeColor = pal.Texto,
                    TextAlign = HorizontalAlignment.Right
                };
                dialogo.Controls.Add(cantidad);

                EtiquetaDialogo(dialogo, "STOCK RESULTANTE", 200, 88, pal);
                Label lblPrevio = Etiqueta("", 11f, pal.Texto, true);
                lblPrevio.Location = new Point(200, 109);
                dialogo.Controls.Add(lblPrevio);

                EtiquetaDialogo(dialogo, "RESPONSABLE DEL MOVIMIENTO *", 24, 148, pal);
                ComboBox responsable = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    FlatStyle = FlatStyle.Flat,
                    Location = new Point(24, 168),
                    Size = new Size(492, 27),
                    Font = CrearFuente(9.5f),
                    BackColor = pal.Input,
                    ForeColor = pal.Texto,
                    DataSource = trabajadores,
                    DisplayMember = "nombre_completo",
                    ValueMember = "id_responsable"
                };
                if (responsable.Items.Count > 0) responsable.SelectedIndex = 0;
                dialogo.Controls.Add(responsable);

                ComboBox motivo = ComboDialogo(dialogo, "MOTIVO *", 24, 208, 492,
                    esEntrada ? MotivosEntrada : MotivosSalida, "", pal);
                motivo.MaxLength = 200;

                TextBox ubicacionExterna = CampoDialogo(dialogo,
                    esEntrada ? "ORIGEN / PROVEEDOR (OPCIONAL)" : "DESTINO / ÁREA (OPCIONAL)",
                    24, 268, 492, "", 150, pal);

                Label nota = Etiqueta("El sistema verificará el stock y guardará el historial en una sola transacción.", 8f, pal.Gris);
                nota.Location = new Point(24, 328);
                dialogo.Controls.Add(nota);

                Button cancelar = BotonDialogo("Cancelar", false, 110, pal);
                cancelar.Location = new Point(290, 400);
                cancelar.Click += delegate { dialogo.Close(); };
                dialogo.Controls.Add(cancelar);

                Button guardar = BotonDialogo("Guardar " + tipo.ToLowerInvariant(), true, 100 + tipo.Length * 6, pal);
                guardar.Location = new Point(516 - guardar.Width, 400);
                dialogo.Controls.Add(guardar);
                cancelar.Left = guardar.Left - 10 - cancelar.Width;
                dialogo.CancelButton = cancelar;

                Action actualizarPrevio = delegate
                {
                    long nuevo = esEntrada ? (long)stockActual + (long)cantidad.Value : (long)stockActual - (long)cantidad.Value;
                    lblPrevio.Text = stockActual.ToString("N0") + "  →  " + nuevo.ToString("N0");
                    bool invalido = nuevo < 0;
                    lblPrevio.ForeColor = invalido ? rojo : (esEntrada ? verde : naranja);
                    guardar.Enabled = !invalido;
                };
                cantidad.ValueChanged += delegate { actualizarPrevio(); };
                actualizarPrevio();

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
                            LimpiarTexto(motivo.Text),
                            LimpiarTexto(ubicacionExterna.Text),
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
                        actualizarPrevio();
                    }
                };

                try
                {
                    if (dialogo.ShowDialog(FindForm()) == DialogResult.OK)
                    {
                        CargarDatosInventario();
                        if (pestanaActual == 1) CargarHistorial();
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
SELECT 1
WHERE EXISTS (SELECT 1 FROM public.salida_equipos WHERE codigo_escaneado = @op)
   OR EXISTS (SELECT 1 FROM public.retornos WHERE observaciones LIKE '%OP:' || @op)";
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
                        string obs = "ENTRADA | " + motivo + " | Cantidad: " + cantidad;
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
                        cmd.Parameters.AddWithValue("@ubicacion", string.IsNullOrWhiteSpace(ubicacionExterna) ? (object)DBNull.Value : ubicacionExterna);
                        string detalles = motivo + " | Cantidad: " + cantidad;
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

        // ==============================================================
        //  EXPORTAR CSV (solo columnas visibles, con encabezados en español)
        // ==============================================================
        private void ExportarCsv()
        {
            bool enEquipos = pestanaActual == 0;
            DataTable tabla = enEquipos ? datosEquipos : datosMovimientos;
            DataGridView grilla = enEquipos ? dgvEquipos : dgvMovimientos;
            if (tabla == null || tabla.DefaultView.Count == 0)
            {
                Informar("No hay registros visibles para exportar.");
                return;
            }

            using (SaveFileDialog dialogo = new SaveFileDialog
            {
                Filter = "Archivo CSV (*.csv)|*.csv",
                FileName = (enEquipos ? "inventario_" : "movimientos_") + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv"
            })
            {
                if (dialogo.ShowDialog(FindForm()) != DialogResult.OK) return;
                try
                {
                    List<DataGridViewColumn> columnas = grilla.Columns.Cast<DataGridViewColumn>()
                        .Where(c => c.Visible && !string.IsNullOrEmpty(c.DataPropertyName))
                        .OrderBy(c => c.DisplayIndex)
                        .ToList();

                    using (StreamWriter escritor = new StreamWriter(dialogo.FileName, false, new UTF8Encoding(true)))
                    {
                        escritor.WriteLine(string.Join(";", columnas.Select(c => CeldaCsv(c.HeaderText))));
                        foreach (DataRowView fila in tabla.DefaultView)
                        {
                            List<string> celdas = new List<string>();
                            foreach (DataGridViewColumn c in columnas)
                                celdas.Add(CeldaCsv(Convert.ToString(fila[c.DataPropertyName], CultureInfo.CurrentCulture)));
                            escritor.WriteLine(string.Join(";", celdas));
                        }
                    }
                    Informar("Archivo CSV exportado: " + Path.GetFileName(dialogo.FileName));
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

        // ==============================================================
        //  MENSAJES
        // ==============================================================
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