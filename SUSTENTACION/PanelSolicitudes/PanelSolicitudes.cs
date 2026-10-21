using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;

namespace SUSTENTACION.PanelSolicitudes
{
    public class PanelSolicitudes : UserControl
    {
        // ---------- Paleta ----------
        private readonly Color fondo = Color.FromArgb(243, 246, 250);
        private readonly Color tarjeta = Color.White;
        private readonly Color verde = Color.FromArgb(19, 125, 91);
        private readonly Color texto = Color.FromArgb(26, 39, 54);
        private readonly Color tenue = Color.FromArgb(106, 122, 140);
        private readonly Color linea = Color.FromArgb(226, 232, 239);
        private readonly Color rojo = Color.FromArgb(178, 45, 45);
        private readonly Color ambar = Color.FromArgb(190, 130, 30);
        private readonly Color azul = Color.FromArgb(28, 107, 185);

        // ---------- Contenedores ----------
        private Panel panelScroll; // NUEVO: Panel para el Scroll
        private TableLayoutPanel principal;

        // ---------- KPIs ----------
        private Label lblPendientes, lblAprobadas, lblRechazadas, lblTotal;
        private Label lblConexion;

        // ---------- Búsqueda de usuario ----------
        private Panel contenedorBuscar;
        private TextBox txtBuscarUsuario;
        private Button btnBuscarUsuario;
        private DataGridView gridUsuarios;
        private DataTable datosUsuarios = new DataTable();

        // ---------- Solicitudes ----------
        private Panel contenedorSolicitudes;
        private TextBox txtBuscarSolicitud;
        private ComboBox cmbEstado;
        private DataGridView gridSolicitudes;
        private DataTable datosSolicitudes = new DataTable();

        // ---------- Detalle ----------
        private Panel contenedorDetalle;
        private Label lblDetalleNombre, lblDetalleDni, lblDetalleCorreo,
                      lblDetalleTelefono, lblDetalleRol, lblDetalleTipoCambio,
                      lblDetalleDescripcion, lblDetalleFecha;

        // ---------- Botones ----------
        private Button btnAprobar, btnRechazar, btnActualizar;
        private bool cargando;

        public PanelSolicitudes()
        {
            Dock = DockStyle.Fill;
            BackColor = fondo;
            Font = new Font("Segoe UI", 9F);
            ConstruirInterfaz();
            Load += (s, e) =>
            {
                if (!EsUsuarioAdministrador()) { BloquearAcceso(); return; }
                CargarTodo(true);
            };
        }

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
        //  INTERFAZ (SOLUCIÓN AL SCROLL)
        // =================================================================
        private void ConstruirInterfaz()
        {
            // 1. Creamos un panel contenedor con AutoScroll
            panelScroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = fondo
            };
            Controls.Add(panelScroll);

            // 2. El TableLayoutPanel va dentro del panel con scroll
            principal = new TableLayoutPanel
            {
                Dock = DockStyle.Top, // Importante: Top en vez de Fill
                ColumnCount = 1,
                RowCount = 6,
                BackColor = fondo,
                Padding = new Padding(22),
                AutoSize = true,      // Permite que el panel crezca verticalmente
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            principal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            principal.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));    // Encabezado
            principal.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));   // KPIs
            principal.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // Buscar usuario
            principal.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // Solicitudes
            principal.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // Detalle
            principal.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));    // Margen inferior extra

            panelScroll.Controls.Add(principal);

            // 3. Agregamos los controles
            principal.Controls.Add(ConstruirEncabezado(), 0, 0);
            principal.Controls.Add(ConstruirKpis(), 0, 1);

            var seccionBuscar = ConstruirSeccionDesplegable(
                "🔎  BUSCAR USUARIO (por DNI, correo, nombre o celular)",
                true,
                out contenedorBuscar);
            ContruirContenidoBuscar(contenedorBuscar);
            principal.Controls.Add(seccionBuscar, 0, 2);

            var seccionSolicitudes = ConstruirSeccionDesplegable(
                "📋  SOLICITUDES DE CAMBIO",
                true,
                out contenedorSolicitudes);
            ContruirContenidoSolicitudes(contenedorSolicitudes);
            principal.Controls.Add(seccionSolicitudes, 0, 3);

            var seccionDetalle = ConstruirSeccionDesplegable(
                "👤  DATOS DEL SOLICITANTE",
                false,
                out contenedorDetalle);
            ContruirContenidoDetalle(contenedorDetalle);
            principal.Controls.Add(seccionDetalle, 0, 4);
        }

        // -----------------------------------------------------------------
        //  Encabezado
        // -----------------------------------------------------------------
        private Control ConstruirEncabezado()
        {
            var encabezado = new Panel { Dock = DockStyle.Fill, BackColor = fondo };
            encabezado.Controls.Add(new Label
            {
                Text = "ARTEMUSA TV / ADMINISTRACIÓN",
                AutoSize = true,
                ForeColor = verde,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Location = new Point(2, 0)
            });
            encabezado.Controls.Add(new Label
            {
                Text = "Solicitudes de cambio",
                AutoSize = true,
                ForeColor = texto,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                Location = new Point(0, 23)
            });
            encabezado.Controls.Add(new Label
            {
                Text = "Busca al usuario, revisa sus datos y aprueba o rechaza sus solicitudes.",
                AutoSize = true,
                ForeColor = tenue,
                Location = new Point(2, 62)
            });
            lblConexion = new Label
            {
                Text = "● Verificando conexión",
                AutoSize = true,
                ForeColor = tenue,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(650, 5)
            };
            encabezado.Controls.Add(lblConexion);
            encabezado.Resize += (s, e) =>
                lblConexion.Location = new Point(Math.Max(0, encabezado.ClientSize.Width - lblConexion.Width), 5);
            return encabezado;
        }

        // -----------------------------------------------------------------
        //  KPIs
        // -----------------------------------------------------------------
        private Control ConstruirKpis()
        {
            var kpis = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = fondo,
                Padding = new Padding(0, 4, 0, 8)
            };
            for (int i = 0; i < 4; i++)
                kpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            lblPendientes = AgregarKpi(kpis, 0, "PENDIENTES", "0", ambar);
            lblAprobadas = AgregarKpi(kpis, 1, "APROBADAS", "0", verde);
            lblRechazadas = AgregarKpi(kpis, 2, "RECHAZADAS", "0", rojo);
            lblTotal = AgregarKpi(kpis, 3, "TOTAL DE SOLICITUDES", "0", azul);
            return kpis;
        }

        private Label AgregarKpi(TableLayoutPanel panel, int columna, string titulo, string valor, Color color)
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
                Text = valor,
                AutoSize = true,
                ForeColor = texto,
                Font = new Font("Segoe UI", 21F, FontStyle.Bold),
                Location = new Point(12, 36)
            };
            card.Controls.Add(lbl);
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(color, 3))
                    e.Graphics.DrawLine(pen, 12, card.Height - 3, card.Width - 12, card.Height - 3);
            };
            panel.Controls.Add(card, columna, 0);
            return lbl;
        }

        // =================================================================
        //  SECCIÓN DESPLEGABLE
        // =================================================================
        private Panel ConstruirSeccionDesplegable(string titulo, bool expandido, out Panel contenido)
        {
            var seccion = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = tarjeta,
                Margin = new Padding(0, 4, 0, 10),
                Padding = new Padding(0)
            };

            var cabecera = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.FromArgb(248, 250, 252),
                Cursor = Cursors.Hand
            };
            var flecha = new Label
            {
                Text = expandido ? "▾" : "▸",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = verde,
                AutoSize = true,
                Location = new Point(12, 10)
            };
            var lblTitulo = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = texto,
                AutoSize = true,
                Location = new Point(36, 13)
            };
            cabecera.Controls.Add(flecha);
            cabecera.Controls.Add(lblTitulo);
            cabecera.Paint += (s, e) =>
            {
                using (var pen = new Pen(linea, 1))
                    e.Graphics.DrawLine(pen, 0, cabecera.Height - 1, cabecera.Width, cabecera.Height - 1);
            };

            var contenidoLocal = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = tarjeta,
                Padding = new Padding(14, 8, 14, 14),
                Visible = expandido
            };

            EventHandler alternar = delegate
            {
                contenidoLocal.Visible = !contenidoLocal.Visible;
                flecha.Text = contenidoLocal.Visible ? "▾" : "▸";
                seccion.Height = contenidoLocal.Visible
                    ? (cabecera.Height + contenidoLocal.Height)
                    : cabecera.Height;
                seccion.PerformLayout();
            };
            cabecera.Click += alternar;
            flecha.Click += alternar;
            lblTitulo.Click += alternar;

            seccion.Controls.Add(contenidoLocal);
            seccion.Controls.Add(cabecera);

            contenido = contenidoLocal;
            return seccion;
        }

        // =================================================================
        //  CONTENIDO: BUSCAR USUARIO
        // =================================================================
        private void ContruirContenidoBuscar(Panel contenedor)
        {
            txtBuscarUsuario = new TextBox
            {
                Location = new Point(14, 10),
                Width = 380,
                Font = new Font("Segoe UI", 10F)
            };
            txtBuscarUsuario.PlaceholderText = "Ej: 45896321, lopez@gmail.com o María López";
            txtBuscarUsuario.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    BuscarUsuarios();
                }
            };
            contenedor.Controls.Add(txtBuscarUsuario);

            btnBuscarUsuario = new Button
            {
                Text = "Buscar",
                Width = 110,
                Height = 28,
                Location = new Point(404, 9),
                BackColor = azul,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnBuscarUsuario.FlatAppearance.BorderSize = 0;
            btnBuscarUsuario.Click += (s, e) => BuscarUsuarios();
            contenedor.Controls.Add(btnBuscarUsuario);

            var btnLimpiar = new Button
            {
                Text = "Limpiar",
                Width = 90,
                Height = 28,
                Location = new Point(520, 9),
                BackColor = Color.FromArgb(230, 236, 242),
                ForeColor = texto,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLimpiar.FlatAppearance.BorderSize = 0;
            btnLimpiar.Click += (s, e) =>
            {
                txtBuscarUsuario.Clear();
                datosUsuarios = new DataTable();
                gridUsuarios.DataSource = datosUsuarios;
            };
            contenedor.Controls.Add(btnLimpiar);

            gridUsuarios = new DataGridView
            {
                Location = new Point(14, 46),
                Size = new Size(contenedor.Width - 28, 155),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = tarjeta,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                GridColor = linea
            };
            gridUsuarios.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(240, 244, 248),
                ForeColor = tenue,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                SelectionBackColor = Color.FromArgb(240, 244, 248),
                SelectionForeColor = tenue,
                Padding = new Padding(6)
            };
            gridUsuarios.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = texto,
                SelectionBackColor = Color.FromArgb(226, 243, 235),
                SelectionForeColor = texto,
                Padding = new Padding(5)
            };
            gridUsuarios.RowTemplate.Height = 32;
            contenedor.Controls.Add(gridUsuarios);
            contenedor.Height = 220;
        }

        // =================================================================
        //  CONTENIDO: SOLICITUDES
        // =================================================================
        private void ContruirContenidoSolicitudes(Panel contenedor)
        {
            var filtros = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = tarjeta
            };
            contenedor.Controls.Add(filtros);

            filtros.Controls.Add(new Label
            {
                Text = "Buscar en solicitudes",
                AutoSize = true,
                ForeColor = tenue,
                Location = new Point(0, 4)
            });
            txtBuscarSolicitud = new TextBox { Location = new Point(0, 25), Width = 240 };
            txtBuscarSolicitud.PlaceholderText = "Usuario, tipo o descripción";
            txtBuscarSolicitud.TextChanged += (s, e) => AplicarFiltros();
            filtros.Controls.Add(txtBuscarSolicitud);

            filtros.Controls.Add(new Label
            {
                Text = "Estado",
                AutoSize = true,
                ForeColor = tenue,
                Location = new Point(254, 4)
            });
            cmbEstado = new ComboBox
            {
                Location = new Point(254, 25),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbEstado.Items.AddRange(new object[] { "Todos", "Pendiente", "Aprobada", "Rechazada" });
            cmbEstado.SelectedIndex = 0;
            cmbEstado.SelectedIndexChanged += (s, e) => AplicarFiltros();
            filtros.Controls.Add(cmbEstado);

            btnActualizar = new Button
            {
                Text = "↻  Actualizar",
                Width = 120,
                Height = 32,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(filtros.Width - 130, 23),
                BackColor = verde,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnActualizar.FlatAppearance.BorderSize = 0;
            btnActualizar.Click += (s, e) => CargarTodo(true);
            filtros.Controls.Add(btnActualizar);

            btnAprobar = new Button
            {
                Text = "✔  Aprobar",
                Width = 110,
                Height = 32,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(filtros.Width - 370, 23),
                BackColor = verde,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnAprobar.FlatAppearance.BorderSize = 0;
            btnAprobar.Click += (s, e) => ResolverSolicitud(true);
            filtros.Controls.Add(btnAprobar);

            btnRechazar = new Button
            {
                Text = "✖  Rechazar",
                Width = 110,
                Height = 32,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(filtros.Width - 250, 23),
                BackColor = rojo,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnRechazar.FlatAppearance.BorderSize = 0;
            btnRechazar.Click += (s, e) => ResolverSolicitud(false);
            filtros.Controls.Add(btnRechazar);

            filtros.Resize += (s, e) =>
            {
                btnActualizar.Location = new Point(filtros.ClientSize.Width - btnActualizar.Width - 8, 23);
                btnRechazar.Location = new Point(btnActualizar.Left - btnRechazar.Width - 8, 23);
                btnAprobar.Location = new Point(btnRechazar.Left - btnAprobar.Width - 8, 23);
            };

            gridSolicitudes = new DataGridView
            {
                Location = new Point(0, 78),
                Size = new Size(contenedor.Width - 28, 220),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = tarjeta,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                GridColor = linea
            };
            gridSolicitudes.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(240, 244, 248),
                ForeColor = tenue,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                SelectionBackColor = Color.FromArgb(240, 244, 248),
                SelectionForeColor = tenue,
                Padding = new Padding(6)
            };
            gridSolicitudes.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = texto,
                SelectionBackColor = Color.FromArgb(226, 243, 235),
                SelectionForeColor = texto,
                Padding = new Padding(5)
            };
            gridSolicitudes.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 251, 253);
            gridSolicitudes.RowTemplate.Height = 36;
            gridSolicitudes.SelectionChanged += (s, e) =>
            {
                ActualizarBotones();
                ActualizarDetalle();
            };
            gridSolicitudes.CellFormatting += FormatearEstado;
            contenedor.Controls.Add(gridSolicitudes);
            contenedor.Height = 310;
        }

        // =================================================================
        //  CONTENIDO: DETALLE
        // =================================================================
        private void ContruirContenidoDetalle(Panel contenedor)
        {
            int xCol1 = 14, xCol2 = 340;
            int y = 8;
            lblDetalleNombre = AgregarDatoDetalle(contenedor, "Nombre", xCol1, y);
            lblDetalleDni = AgregarDatoDetalle(contenedor, "DNI", xCol2, y);
            y += 42;
            lblDetalleCorreo = AgregarDatoDetalle(contenedor, "Correo", xCol1, y);
            lblDetalleTelefono = AgregarDatoDetalle(contenedor, "Celular", xCol2, y);
            y += 42;
            lblDetalleRol = AgregarDatoDetalle(contenedor, "Rol", xCol1, y);
            lblDetalleTipoCambio = AgregarDatoDetalle(contenedor, "Tipo de cambio solicitado", xCol2, y);
            y += 42;
            lblDetalleFecha = AgregarDatoDetalle(contenedor, "Fecha de solicitud", xCol1, y);
            lblDetalleDescripcion = AgregarDatoDetalle(contenedor, "Valor nuevo / Motivo", xCol2, y, 400);
            contenedor.Height = 220;
        }

        private Label AgregarDatoDetalle(Panel contenedor, string etiqueta, int x, int y, int anchoValor = 300)
        {
            contenedor.Controls.Add(new Label
            {
                Text = etiqueta.ToUpperInvariant(),
                AutoSize = true,
                ForeColor = tenue,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                Location = new Point(x, y)
            });
            var valor = new Label
            {
                Text = "—",
                AutoSize = false,
                Size = new Size(anchoValor, 20),
                ForeColor = texto,
                Font = new Font("Segoe UI", 10F),
                Location = new Point(x, y + 15),
                AutoEllipsis = true
            };
            contenedor.Controls.Add(valor);
            return valor;
        }

        // =================================================================
        //  CONEXIÓN
        // =================================================================
        private NpgsqlConnection AbrirConexion()
        {
            var conexion = new global::SUSTENTACION.Conexion().ObtenerConexion();
            if (conexion == null) throw new InvalidOperationException("La conexión devolvió un valor vacío.");
            if (conexion.State != ConnectionState.Open) conexion.Open();
            return conexion;
        }

        private void CargarTodo(bool mostrarErrores)
        {
            if (cargando || IsDisposed) return;
            cargando = true;
            try
            {
                using (var conexion = AbrirConexion())
                {
                    datosSolicitudes = LeerSolicitudes(conexion);
                    ActualizarKpis(conexion);
                }
                gridSolicitudes.DataSource = datosSolicitudes;
                ConfigurarColumnasSolicitudes();
                AplicarFiltros();
                ActualizarBotones();
                ActualizarDetalle();
                lblConexion.Text = "● Conectado a Supabase";
                lblConexion.ForeColor = verde;
            }
            catch (Exception ex)
            {
                lblConexion.Text = "● Error de conexión o consulta";
                lblConexion.ForeColor = Color.Firebrick;
                gridSolicitudes.DataSource = null;
                if (mostrarErrores)
                    MessageBox.Show("No se pudieron cargar las solicitudes.\r\n\r\n" + ex.Message,
                        "Panel de Solicitudes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { cargando = false; }
        }

        private void BuscarUsuarios()
        {
            string filtro = txtBuscarUsuario.Text.Trim();
            if (string.IsNullOrWhiteSpace(filtro))
            {
                datosUsuarios = new DataTable();
                gridUsuarios.DataSource = datosUsuarios;
                return;
            }

            try
            {
                var tabla = new DataTable();
                using (var conexion = AbrirConexion())
                using (var adaptador = new NpgsqlDataAdapter(@"
SELECT
    t.id_trabajador,
    COALESCE(t.nombre, '')  AS nombre,
    COALESCE(t.dni, '')     AS dni,
    COALESCE(t.correo, '')  AS correo,
    COALESCE(t.telefono, '')AS telefono,
    COALESCE(t.cargo, '')   AS cargo,
    CASE WHEN t.activo THEN 'Activo' ELSE 'Inactivo' END AS estado
FROM public.trabajadores t
WHERE
    t.dni      ILIKE '%' || @f || '%'
 OR t.correo   ILIKE '%' || @f || '%'
 OR t.nombre   ILIKE '%' || @f || '%'
 OR t.telefono ILIKE '%' || @f || '%'
ORDER BY t.nombre
LIMIT 50;", conexion))
                {
                    adaptador.SelectCommand.Parameters.AddWithValue("@f", filtro);
                    adaptador.Fill(tabla);
                }
                datosUsuarios = tabla;
                gridUsuarios.DataSource = tabla;
                ConfigurarColumnasUsuarios();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "No se pudo buscar usuarios.\r\n\r\n" + ex.Message,
                    "Panel de Solicitudes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConfigurarColumnasUsuarios()
        {
            if (gridUsuarios.Columns.Count == 0) return;
            Ocultar(gridUsuarios, "id_trabajador");
            Renombrar(gridUsuarios, "nombre", "Nombre");
            Renombrar(gridUsuarios, "dni", "DNI");
            Renombrar(gridUsuarios, "correo", "Correo");
            Renombrar(gridUsuarios, "telefono", "Celular");
            Renombrar(gridUsuarios, "cargo", "Cargo");
            Renombrar(gridUsuarios, "estado", "Estado");
        }

        private DataTable LeerSolicitudes(NpgsqlConnection conexion)
        {
            string rol = ObtenerRolActual();
            string filtro = rol == "SuperAdmin"
                ? "WHERE COALESCE(sc.aprobador_requerido, 'SuperAdmin') = 'SuperAdmin'"
                : "WHERE COALESCE(sc.aprobador_requerido, 'Admin') = 'Admin'";

            string sql = @"
SELECT
    sc.id_solicitud,
    sc.id_trabajador,
    COALESCE(t.nombre, 'Usuario #' || sc.id_trabajador::text) AS trabajador,
    COALESCE(t.dni, '')      AS dni,
    COALESCE(t.correo, '')   AS correo,
    COALESCE(t.telefono, '') AS telefono,
    COALESCE(t.cargo, '')    AS cargo,
    sc.tipo_cambio,
    sc.descripcion,
    COALESCE(sc.valor_nuevo, '') AS valor_nuevo,
    COALESCE(sc.estado, 'Pendiente') AS estado,
    sc.fecha_solicitud,
    COALESCE(sc.respuesta_admin, '') AS respuesta_admin,
    sc.id_admin,
    COALESCE(a.nombre, '') AS admin,
    COALESCE(sc.rol_solicitante, 'Trabajador') AS rol_solicitante,
    COALESCE(sc.aprobador_requerido, 'Admin') AS aprobador_requerido
FROM public.solicitudes_cambio sc
LEFT JOIN public.trabajadores t ON t.id_trabajador = sc.id_trabajador
LEFT JOIN public.trabajadores a ON a.id_trabajador = sc.id_admin
" + filtro + @"
ORDER BY
    CASE COALESCE(sc.estado,'Pendiente')
        WHEN 'Pendiente' THEN 1
        WHEN 'Aprobada'  THEN 2
        ELSE 3
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
            string rol = ObtenerRolActual();
            string filtro = rol == "SuperAdmin"
                ? "WHERE COALESCE(aprobador_requerido, 'SuperAdmin') = 'SuperAdmin'"
                : "WHERE COALESCE(aprobador_requerido, 'Admin') = 'Admin'";

            string sql = @"
SELECT
    COUNT(*) FILTER (WHERE COALESCE(estado,'Pendiente') = 'Pendiente') AS pendientes,
    COUNT(*) FILTER (WHERE estado = 'Aprobada')  AS aprobadas,
    COUNT(*) FILTER (WHERE estado = 'Rechazada') AS rechazadas,
    COUNT(*) AS total
FROM public.solicitudes_cambio
" + filtro + ";";

            using (var cmd = new NpgsqlCommand(sql, conexion))
            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.Read()) return;
                lblPendientes.Text = Convert.ToInt64(reader["pendientes"]).ToString("N0");
                lblAprobadas.Text = Convert.ToInt64(reader["aprobadas"]).ToString("N0");
                lblRechazadas.Text = Convert.ToInt64(reader["rechazadas"]).ToString("N0");
                lblTotal.Text = Convert.ToInt64(reader["total"]).ToString("N0");
            }
        }

        private void ConfigurarColumnasSolicitudes()
        {
            if (gridSolicitudes.Columns.Count == 0) return;
            Ocultar(gridSolicitudes, "id_solicitud");
            Ocultar(gridSolicitudes, "id_trabajador");
            Ocultar(gridSolicitudes, "id_admin");
            Ocultar(gridSolicitudes, "dni");
            Ocultar(gridSolicitudes, "correo");
            Ocultar(gridSolicitudes, "telefono");
            Ocultar(gridSolicitudes, "cargo");
            Ocultar(gridSolicitudes, "valor_nuevo");
            Renombrar(gridSolicitudes, "trabajador", "Usuario");
            Renombrar(gridSolicitudes, "rol_solicitante", "Rol");
            Renombrar(gridSolicitudes, "tipo_cambio", "Tipo de cambio");
            Renombrar(gridSolicitudes, "descripcion", "Descripción / Motivo");
            Renombrar(gridSolicitudes, "estado", "Estado");
            Renombrar(gridSolicitudes, "fecha_solicitud", "Fecha");
            Renombrar(gridSolicitudes, "respuesta_admin", "Respuesta");
            Renombrar(gridSolicitudes, "admin", "Atendido por");

            AjustarAncho(gridSolicitudes, "trabajador", 130);
            AjustarAncho(gridSolicitudes, "rol_solicitante", 80);
            AjustarAncho(gridSolicitudes, "tipo_cambio", 130);
            AjustarAncho(gridSolicitudes, "descripcion", 260);
            AjustarAncho(gridSolicitudes, "estado", 90);
            AjustarAncho(gridSolicitudes, "fecha_solicitud", 120);
            AjustarAncho(gridSolicitudes, "respuesta_admin", 160);
            AjustarAncho(gridSolicitudes, "admin", 110);

            if (gridSolicitudes.Columns.Contains("fecha_solicitud"))
                gridSolicitudes.Columns["fecha_solicitud"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        }

        private static void Ocultar(DataGridView g, string nombre)
        {
            if (g.Columns.Contains(nombre)) g.Columns[nombre].Visible = false;
        }

        private static void Renombrar(DataGridView g, string nombre, string titulo)
        {
            if (g.Columns.Contains(nombre)) g.Columns[nombre].HeaderText = titulo;
        }

        private static void AjustarAncho(DataGridView g, string nombre, int peso)
        {
            if (g.Columns.Contains(nombre)) g.Columns[nombre].FillWeight = peso;
        }

        private void AplicarFiltros()
        {
            if (datosSolicitudes == null || datosSolicitudes.DefaultView == null) return;
            var condiciones = new System.Collections.Generic.List<string>();
            string buscar = txtBuscarSolicitud == null ? "" : txtBuscarSolicitud.Text.Trim();
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                string valor = buscar.Replace("'", "''").Replace("[", "[[]").Replace("%", "[%]").Replace("*", "[*]");
                var partes = new System.Collections.Generic.List<string>();
                foreach (string col in new[] { "trabajador", "tipo_cambio", "descripcion", "respuesta_admin", "admin" })
                    if (datosSolicitudes.Columns.Contains(col))
                        partes.Add("CONVERT(" + col + ", 'System.String') LIKE '%" + valor + "%'");
                if (partes.Count > 0) condiciones.Add("(" + string.Join(" OR ", partes) + ")");
            }
            if (cmbEstado != null && cmbEstado.SelectedIndex > 0)
                condiciones.Add("estado = '" + cmbEstado.SelectedItem.ToString().Replace("'", "''") + "'");
            try { datosSolicitudes.DefaultView.RowFilter = string.Join(" AND ", condiciones); }
            catch { datosSolicitudes.DefaultView.RowFilter = ""; }
            ActualizarBotones();
        }

        private void ActualizarBotones()
        {
            bool pendiente = false;
            if (gridSolicitudes != null && gridSolicitudes.CurrentRow != null)
            {
                var vista = gridSolicitudes.CurrentRow.DataBoundItem as DataRowView;
                if (vista != null)
                {
                    string estado = Convert.ToString(vista.Row["estado"]);
                    pendiente = string.Equals(estado, "Pendiente", StringComparison.OrdinalIgnoreCase);
                }
            }
            if (btnAprobar != null)
            {
                btnAprobar.Enabled = pendiente;
                btnAprobar.BackColor = pendiente ? verde : Color.FromArgb(180, 200, 190);
            }
            if (btnRechazar != null)
            {
                btnRechazar.Enabled = pendiente;
                btnRechazar.BackColor = pendiente ? rojo : Color.FromArgb(210, 175, 175);
            }
        }

        private void FormatearEstado(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (gridSolicitudes.Columns[e.ColumnIndex].Name != "estado") return;
            string estado = Convert.ToString(e.Value);
            if (string.Equals(estado, "Pendiente", StringComparison.OrdinalIgnoreCase))
                e.CellStyle.ForeColor = ambar;
            else if (string.Equals(estado, "Aprobada", StringComparison.OrdinalIgnoreCase))
                e.CellStyle.ForeColor = verde;
            else if (string.Equals(estado, "Rechazada", StringComparison.OrdinalIgnoreCase))
                e.CellStyle.ForeColor = rojo;
        }

        private void ActualizarDetalle()
        {
            if (lblDetalleNombre == null) return;
            if (gridSolicitudes.CurrentRow == null) { LimpiarDetalle(); return; }
            var vista = gridSolicitudes.CurrentRow.DataBoundItem as DataRowView;
            if (vista == null) { LimpiarDetalle(); return; }
            var r = vista.Row;

            lblDetalleNombre.Text = Valor(r, "trabajador");
            lblDetalleDni.Text = Mostrar(Valor(r, "dni"));
            lblDetalleCorreo.Text = Mostrar(Valor(r, "correo"));
            lblDetalleTelefono.Text = Mostrar(Valor(r, "telefono"));
            lblDetalleRol.Text = Valor(r, "rol_solicitante");
            lblDetalleTipoCambio.Text = Valor(r, "tipo_cambio");

            string fecha = Valor(r, "fecha_solicitud");
            if (r.Table.Columns.Contains("fecha_solicitud") && !r.IsNull("fecha_solicitud"))
                fecha = Convert.ToDateTime(r["fecha_solicitud"]).ToString("dd/MM/yyyy HH:mm");
            lblDetalleFecha.Text = fecha;

            string valorNuevo = Valor(r, "valor_nuevo");
            string descripcion = Valor(r, "descripcion");
            string compuesto = "";
            if (!string.IsNullOrWhiteSpace(valorNuevo))
                compuesto = "Nuevo: " + valorNuevo;
            if (!string.IsNullOrWhiteSpace(descripcion))
                compuesto += (compuesto.Length > 0 ? "  |  " : "") + "Motivo: " + descripcion;
            lblDetalleDescripcion.Text = compuesto.Length > 0 ? compuesto : "—";
        }

        private void LimpiarDetalle()
        {
            lblDetalleNombre.Text = "—";
            lblDetalleDni.Text = "—";
            lblDetalleCorreo.Text = "—";
            lblDetalleTelefono.Text = "—";
            lblDetalleRol.Text = "—";
            lblDetalleTipoCambio.Text = "—";
            lblDetalleFecha.Text = "—";
            lblDetalleDescripcion.Text = "—";
        }

        private string Mostrar(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? "—" : valor;
        }

        private void ResolverSolicitud(bool aprobar)
        {
            if (gridSolicitudes.CurrentRow == null) { Aviso("Selecciona una solicitud."); return; }
            var vista = gridSolicitudes.CurrentRow.DataBoundItem as DataRowView;
            if (vista == null) return;
            var r = vista.Row;

            string estadoActual = Convert.ToString(r["estado"]);
            if (!string.Equals(estadoActual, "Pendiente", StringComparison.OrdinalIgnoreCase))
            {
                Aviso("Esta solicitud ya fue resuelta (" + estadoActual + ").");
                return;
            }

            int idSolicitud = Convert.ToInt32(r["id_solicitud"]);
            int idTrabajador = Convert.ToInt32(r["id_trabajador"]);
            string tipoCambio = Convert.ToString(r["tipo_cambio"]);

            string valorSolicitado = r.Table.Columns.Contains("valor_nuevo")
                                        ? Convert.ToString(r["valor_nuevo"])
                                        : Convert.ToString(r["descripcion"]);

            string valorNuevo = null;
            string respuesta = PedirRespuesta(aprobar, tipoCambio,
                Convert.ToString(r["trabajador"]),
                Convert.ToString(r["dni"]),
                Convert.ToString(r["correo"]),
                valorSolicitado,
                out valorNuevo);

            if (respuesta == null) return;

            int idAdminActual = ObtenerIdAdminActual();

            try
            {
                using (var conexion = AbrirConexion())
                using (var transaccion = conexion.BeginTransaction())
                {
                    const string sqlUpdate = @"
UPDATE public.solicitudes_cambio
SET estado = @estado,
    respuesta_admin = @respuesta,
    id_admin = @id_admin
WHERE id_solicitud = @id AND COALESCE(estado,'Pendiente') = 'Pendiente'";
                    using (var cmd = new NpgsqlCommand(sqlUpdate, conexion, transaccion))
                    {
                        cmd.Parameters.AddWithValue("@estado", aprobar ? "Aprobada" : "Rechazada");
                        cmd.Parameters.AddWithValue("@respuesta", (object)respuesta ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@id_admin", idAdminActual == 0 ? (object)DBNull.Value : idAdminActual);
                        cmd.Parameters.AddWithValue("@id", idSolicitud);
                        if (cmd.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException("La solicitud ya fue resuelta por otro administrador.");
                    }

                    if (aprobar)
                    {
                        if (string.IsNullOrWhiteSpace(valorNuevo))
                            throw new InvalidOperationException("La solicitud no incluye un valor nuevo para aplicar.");

                        const string sqlValor = @"
UPDATE public.solicitudes_cambio
SET valor_nuevo = @valor
WHERE id_solicitud = @id";
                        using (var cmd = new NpgsqlCommand(sqlValor, conexion, transaccion))
                        {
                            cmd.Parameters.AddWithValue("@valor", valorNuevo);
                            cmd.Parameters.AddWithValue("@id", idSolicitud);
                            cmd.ExecuteNonQuery();
                        }

                        AplicarCambio(conexion, transaccion, idTrabajador, tipoCambio, valorNuevo);
                    }

                    transaccion.Commit();
                }
                CargarTodo(false);
                MessageBox.Show(this, aprobar ? "Solicitud aprobada y cambio aplicado." : "Solicitud rechazada.",
                    "Panel de Solicitudes", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "No se pudo resolver la solicitud.\r\n\r\n" + ex.Message,
                    "Panel de Solicitudes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AplicarCambio(NpgsqlConnection conexion, NpgsqlTransaction transaccion,
                                   int idTrabajador, string tipoCambio, string nuevoValor)
        {
            string columna;
            switch ((tipoCambio ?? "").ToLowerInvariant().Trim())
            {
                case "contraseña":
                case "contrasena":
                case "password": columna = "password"; break;
                case "correo":
                case "email": columna = "correo"; break;
                case "dni":
                case "documento": columna = "dni"; break;
                case "nombre":
                case "nombre completo": columna = "nombre"; break;
                case "celular":
                case "telefono":
                case "teléfono": columna = "telefono"; break;
                default:
                    throw new InvalidOperationException(
                        "Tipo de cambio no soportado: '" + tipoCambio + "'. " +
                        "Agrégalo en AplicarCambio().");
            }

            string sql = "UPDATE public.trabajadores SET " + columna + " = @valor WHERE id_trabajador = @id";
            using (var cmd = new NpgsqlCommand(sql, conexion, transaccion))
            {
                cmd.Parameters.AddWithValue("@valor", nuevoValor.Trim());
                cmd.Parameters.AddWithValue("@id", idTrabajador);
                if (cmd.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException("No se pudo aplicar el cambio sobre el usuario.");
            }
        }

        private string PedirRespuesta(bool aprobar, string tipoCambio,
            string nombreSolicitante, string dni, string correo,
            string valorSolicitado, out string valorNuevo)
        {
            valorNuevo = null;

            using (var dlg = new Form
            {
                Text = aprobar ? "Aprobar solicitud" : "Rechazar solicitud",
                Size = new Size(560, aprobar ? 470 : 300),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            })
            {
                dlg.Controls.Add(new Label
                {
                    Text = "Solicitante: " + nombreSolicitante,
                    AutoSize = true,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    ForeColor = texto,
                    Location = new Point(18, 12)
                });
                dlg.Controls.Add(new Label
                {
                    Text = "DNI: " + (string.IsNullOrWhiteSpace(dni) ? "—" : dni)
                        + "   ·   Correo: " + (string.IsNullOrWhiteSpace(correo) ? "—" : correo),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 8.5F),
                    ForeColor = tenue,
                    Location = new Point(18, 36)
                });

                dlg.Controls.Add(new Label
                {
                    Text = "Tipo de cambio: " + tipoCambio,
                    AutoSize = true,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = verde,
                    Location = new Point(18, 62)
                });

                int yRespuesta = 100;
                TextBox txtValor = null;

                if (aprobar)
                {
                    string etiquetaValor;
                    string tipoNorm = (tipoCambio ?? "").ToLowerInvariant().Trim();
                    bool esPassword = tipoNorm.Contains("contrase") || tipoNorm.Contains("contrasena") || tipoNorm.Contains("password");

                    switch (tipoNorm)
                    {
                        case "contraseña":
                        case "contrasena":
                        case "password":
                            etiquetaValor = "Nueva contraseña a asignar *"; break;
                        case "correo":
                        case "email":
                            etiquetaValor = "Nuevo correo electrónico *"; break;
                        case "dni":
                        case "documento":
                            etiquetaValor = "Nuevo DNI *"; break;
                        case "nombre":
                        case "nombre completo":
                            etiquetaValor = "Nuevo nombre completo *"; break;
                        case "celular":
                        case "telefono":
                        case "teléfono":
                            etiquetaValor = "Nuevo celular *"; break;
                        default:
                            etiquetaValor = "Nuevo valor a asignar *"; break;
                    }

                    dlg.Controls.Add(new Label
                    {
                        Text = etiquetaValor,
                        AutoSize = true,
                        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                        ForeColor = texto,
                        Location = new Point(18, 88)
                    });

                    txtValor = new TextBox
                    {
                        Location = new Point(18, 110),
                        Size = new Size(510, 26),
                        Font = new Font("Segoe UI", 10F),
                        Text = valorSolicitado ?? "",
                        UseSystemPasswordChar = esPassword
                    };
                    dlg.Controls.Add(txtValor);

                    if (esPassword)
                    {
                        var chkVer = new CheckBox
                        {
                            Text = "Ver contraseña",
                            AutoSize = true,
                            Location = new Point(18, 140),
                            ForeColor = tenue
                        };
                        chkVer.CheckedChanged += (s, e) =>
                            txtValor.UseSystemPasswordChar = !chkVer.Checked;
                        dlg.Controls.Add(chkVer);
                    }

                    dlg.Controls.Add(new Label
                    {
                        Text = "Este valor reemplazará el dato actual del usuario en la base de datos.",
                        AutoSize = true,
                        Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                        ForeColor = tenue,
                        Location = new Point(18, esPassword ? 166 : 142)
                    });

                    yRespuesta = esPassword ? 200 : 180;
                }

                dlg.Controls.Add(new Label
                {
                    Text = aprobar
                        ? "Comentario para el usuario (opcional):"
                        : "Motivo del rechazo:",
                    AutoSize = true,
                    Location = new Point(18, yRespuesta),
                    ForeColor = tenue
                });

                var txtRespuesta = new TextBox
                {
                    Location = new Point(18, yRespuesta + 24),
                    Size = new Size(510, aprobar ? 80 : 120),
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical
                };
                if (!aprobar) txtRespuesta.Text = "Solicitud rechazada por el administrador.";
                dlg.Controls.Add(txtRespuesta);

                int yBotones = dlg.ClientSize.Height - 50;

                var ok = new Button
                {
                    Text = aprobar ? "Aprobar y aplicar" : "Rechazar",
                    DialogResult = DialogResult.OK,
                    Location = new Point(dlg.ClientSize.Width - 190, yBotones),
                    Size = new Size(160, 32),
                    BackColor = aprobar ? verde : rojo,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                ok.FlatAppearance.BorderSize = 0;
                dlg.Controls.Add(ok);

                var cancelar = new Button
                {
                    Text = "Cancelar",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(dlg.ClientSize.Width - 300, yBotones),
                    Size = new Size(100, 32)
                };
                dlg.Controls.Add(cancelar);

                dlg.AcceptButton = ok;
                dlg.CancelButton = cancelar;

                if (aprobar && txtValor != null)
                {
                    ok.Click += (s, e) =>
                    {
                        if (string.IsNullOrWhiteSpace(txtValor.Text))
                        {
                            MessageBox.Show(dlg,
                                "Debes ingresar el nuevo valor antes de aprobar.",
                                "Dato requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            txtValor.Focus();
                            dlg.DialogResult = DialogResult.None;
                        }
                    };
                }

                var resultado = dlg.ShowDialog(FindForm());
                if (resultado != DialogResult.OK) return null;

                if (aprobar && txtValor != null)
                    valorNuevo = txtValor.Text.Trim();

                return txtRespuesta.Text.Trim();
            }
        }

        private string Valor(DataRow fila, string columna)
        {
            if (!fila.Table.Columns.Contains(columna) || fila.IsNull(columna)) return "—";
            return Convert.ToString(fila[columna]) ?? "—";
        }

        private void Aviso(string texto)
        {
            MessageBox.Show(this, texto, "Panel de Solicitudes", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}