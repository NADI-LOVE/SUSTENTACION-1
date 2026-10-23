using Npgsql;
using SUSTENTACION;
using SUSTENTACION.PanelTrabajador;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace SUSTENTACION
{
    public partial class FormTrabajador : Form
    {
        // Paleta de colores
        private readonly Color bgMain = Color.FromArgb(21, 22, 37);
        private readonly Color bgSidebar = Color.FromArgb(16, 17, 28);
        private readonly Color bgCard = Color.FromArgb(30, 31, 48);
        private readonly Color bgCardSelected = Color.FromArgb(255, 128, 191);
        private readonly Color purpleAccent = Color.FromArgb(92, 84, 241);
        private readonly Color textWhite = Color.White;
        private readonly Color textMuted = Color.FromArgb(130, 134, 158);

        private Panel panelSidebar;
        private Panel panelHeader;
        private Panel panelMainContent;
        private Button botonActivo = null;

        private string nombreUsuario;
        private string rolUsuario;
        private DateTime fechaSeleccionada = DateTime.Now;

        // Variables para la animación del Sidebar
        private System.Windows.Forms.Timer timerSidebar;
        private bool sidebarExpandido = true;
        private const int ANCHO_MAX = 220;
        private const int ANCHO_MIN = 60;

        // Variable para guardar el ID del trabajador logueado
        private int idTrabajador;
        public bool SesionCerrada { get; private set; } = false;


        public FormTrabajador(
            int id = 1,
            string nombre = "María López",
            string rol = "Trabajador")
        {
            InitializeComponent();

            this.idTrabajador = id;
            this.nombreUsuario = nombre;
            this.rolUsuario = rol;

            ConfigurarVentana();
            InicializarTimerSidebar();
            CrearSidebar();
            CrearHeader();
            CrearMainContent();

            // Establecer el logo de ARTEMUSA
            string rutaIcono = System.IO.Path.Combine(
                AppContext.BaseDirectory,
                "logo_artemusa.ico"
            );

            if (System.IO.File.Exists(rutaIcono))
            {
                this.Icon = new Icon(rutaIcono);
            }

            this.ShowIcon = true;
        }

        private void ConfigurarVentana()
        {
            this.Text = "ARTEMISA - Panel de Trabajador";
            this.Size = new Size(1280, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = bgMain;
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.WindowState = FormWindowState.Maximized;
        }

        private void InicializarTimerSidebar()
        {
            timerSidebar = new System.Windows.Forms.Timer();
            timerSidebar.Interval = 10;
            timerSidebar.Tick += (s, e) =>
            {
                if (sidebarExpandido)
                {
                    panelSidebar.Width -= 15;
                    if (panelSidebar.Width <= ANCHO_MIN)
                    {
                        panelSidebar.Width = ANCHO_MIN;
                        timerSidebar.Stop();
                        sidebarExpandido = false;
                    }
                }
                else
                {
                    panelSidebar.Width += 15;
                    if (panelSidebar.Width >= ANCHO_MAX)
                    {
                        panelSidebar.Width = ANCHO_MAX;
                        timerSidebar.Stop();
                        sidebarExpandido = true;
                    }
                }
            };
        }

        private void CrearSidebar()
        {
            panelSidebar = new Panel
            {
                Width = ANCHO_MAX,
                Dock = DockStyle.Left,
                BackColor = bgSidebar,
                Padding = new Padding(8)
            };
            this.Controls.Add(panelSidebar);

            // Logo Marca ARTEMISA
            Panel logoPanel = new Panel { Height = 50, Dock = DockStyle.Top, BackColor = Color.Transparent };
            Label lblLogoIcon = new Label
            {
                Text = "ART",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = textWhite,
                BackColor = purpleAccent,
                Size = new Size(32, 32),
                Location = new Point(6, 8),
                TextAlign = ContentAlignment.MiddleCenter
            };
            AplicarBordesRedondeados(lblLogoIcon, 16);

            Label lblLogoText = new Label
            {
                Text = "ARTEMISA\nPanel del Trabajador",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(44, 8),
                AutoSize = true
            };

            logoPanel.Controls.Add(lblLogoIcon);
            logoPanel.Controls.Add(lblLogoText);
            panelSidebar.Controls.Add(logoPanel);

            // Tarjeta Perfil
            Panel userCard = new Panel
            {
                Size = new Size(200, 50),
                Location = new Point(5, 65),
                BackColor = Color.FromArgb(27, 29, 45)
            };
            AplicarBordesRedondeados(userCard, 10);

            Label lblAvatar = new Label
            {
                Text = "👤",
                Font = new Font("Segoe UI", 12f),
                BackColor = Color.FromArgb(235, 180, 120),
                Size = new Size(32, 32),
                Location = new Point(6, 9),
                TextAlign = ContentAlignment.MiddleCenter
            };
            AplicarBordesRedondeados(lblAvatar, 16);

            Label lblUserInfo = new Label
            {
                Text = $"{nombreUsuario}\n{rolUsuario}",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(44, 11),
                AutoSize = true
            };

            userCard.Controls.Add(lblAvatar);
            userCard.Controls.Add(lblUserInfo);
            panelSidebar.Controls.Add(userCard);

            // Menú Principal
            FlowLayoutPanel menuPanel = new FlowLayoutPanel
            {
                Location = new Point(0, 125),
                Size = new Size(220, 600),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                BackColor = Color.Transparent
            };

            AgregarEtiquetaSeccion(menuPanel, "Menú Principal");
            AgregarBotonMenu(menuPanel, "░", "Panel de Control", "Dashboard", true);
            AgregarBotonMenu(menuPanel, "💬", "Gestión de Equipos", "Chat", false);
            AgregarBotonMenu(menuPanel, "ℹ️", "Información", "Info", false);
            AgregarBotonMenu(menuPanel, "🔔", "Notificaciones y solicitudes", "Notificaciones", false);
            AgregarBotonMenu(menuPanel, "📅", "Eventos", "Event", false);

            AgregarEtiquetaSeccion(menuPanel, "Otros");
            AgregarBotonMenu(menuPanel, "⚙️", "Ajustes", "Settings", false);

            panelSidebar.Controls.Add(menuPanel);
        }

        private void AgregarEtiquetaSeccion(Control container, string texto)
        {
            Label lbl = new Label
            {
                Text = texto,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = textMuted,
                Margin = new Padding(10, 12, 0, 6),
                AutoSize = true
            };
            container.Controls.Add(lbl);
        }

        private void AgregarBotonMenu(Control contenedor, string icono, string texto, string tag, bool esActivo)
        {
            Button btn = new Button
            {
                Text = $"   {icono}   {texto}",
                Tag = tag,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = esActivo ? textWhite : textMuted,
                BackColor = esActivo ? purpleAccent : Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Height = 38,
                Width = 204,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 0, 4),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            AplicarBordesRedondeados(btn, 8);

            if (esActivo) botonActivo = btn;

            btn.Click += (s, e) =>
            {
                if (botonActivo != null)
                {
                    botonActivo.BackColor = Color.Transparent;
                    botonActivo.ForeColor = textMuted;
                }
                botonActivo = btn;
                btn.BackColor = purpleAccent;
                btn.ForeColor = textWhite;

                string opcion = btn.Tag?.ToString();
                panelMainContent.Controls.Clear();

                switch (opcion)
                {
                    case "Dashboard":
                        ConstruirCalendarioEvents();
                        break;
                    case "Chat":
                        GestionEquipo vistaGestion = new GestionEquipo();
                        vistaGestion.Dock = DockStyle.Fill;
                        panelMainContent.Controls.Add(vistaGestion);
                        break;
                    case "Info":
                        InfoPanel vistaInfo = new InfoPanel(this.idTrabajador, this.nombreUsuario);
                        vistaInfo.Dock = DockStyle.Fill;
                        panelMainContent.Controls.Add(vistaInfo);
                        break;
                    case "Notificaciones":
                        NotificacionesPanel vistaNotif = new NotificacionesPanel(this.idTrabajador, this.nombreUsuario);
                        vistaNotif.Dock = DockStyle.Fill;
                        panelMainContent.Controls.Add(vistaNotif);
                        break;
                    case "Event":
                        EventosPanel vistaEventos = new EventosPanel(this.idTrabajador, this.nombreUsuario);
                        vistaEventos.Dock = DockStyle.Fill;
                        panelMainContent.Controls.Add(vistaEventos);
                        break;
                    case "Settings":
                        AjustesPanel vistaAjustes = new AjustesPanel(this.idTrabajador, this.nombreUsuario);
                        vistaAjustes.Dock = DockStyle.Fill;
                        vistaAjustes.TemaCambiado += (esOscuro) =>
                        {
                            // Demostración visual del cambio de tema
                            MessageBox.Show(
                                esOscuro ? "🌙 Tema oscuro activado" : "☀️ Tema claro activado",
                                "Apariencia",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                            // TODO: Si decides implementar el tema claro, llama a AplicarTemaGlobal(esOscuro);
                        };
                        panelMainContent.Controls.Add(vistaAjustes);
                        break;
                    default:
                        break;
                }
            };

            contenedor.Controls.Add(btn);
        }

        private void CrearHeader()
        {
            panelHeader = new Panel
            {
                Height = 60,
                Dock = DockStyle.Top,
                BackColor = bgMain,
                Padding = new Padding(15, 10, 15, 10)
            };
            this.Controls.Add(panelHeader);

            Button btnMenuToggle = new Button
            {
                Text = "≡",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = textWhite,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(35, 35),
                Location = new Point(10, 12),
                Cursor = Cursors.Hand
            };
            btnMenuToggle.FlatAppearance.BorderSize = 0;
            btnMenuToggle.Click += (s, e) => timerSidebar.Start();
            panelHeader.Controls.Add(btnMenuToggle);

            Label lblTitle = new Label
            {
                Text = "Panel del Trabajador",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(50, 16),
                AutoSize = true
            };
            panelHeader.Controls.Add(lblTitle);

            Button btnCerrarSesion = new Button
            {
                Text = "🚪 Cerrar Sesión",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = textWhite,
                BackColor = Color.FromArgb(220, 53, 69),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(130, 32),
                Location = new Point(panelHeader.Width - 145, 14),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnCerrarSesion.FlatAppearance.BorderSize = 0;
            AplicarBordesRedondeados(btnCerrarSesion, 8);

            btnCerrarSesion.Click += (s, e) =>
            {
                if (MessageBox.Show("¿Está seguro de que desea cerrar sesión?", "ARTEMUSA", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    SesionCerrada = true;
                    this.Close();
                }
            };

            panelHeader.Controls.Add(btnCerrarSesion);
        }

        private void CrearMainContent()
        {
            panelMainContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgMain,
                Padding = new Padding(15)
            };
            this.Controls.Add(panelMainContent);
            panelMainContent.BringToFront();

            ConstruirCalendarioEvents();
        }

        private void ConstruirCalendarioEvents()
        {
            panelMainContent.Controls.Clear();

            // 1. Panel lateral derecho (Lista de Eventos)
            Panel panelEventList = new Panel
            {
                Width = 240,
                Dock = DockStyle.Right,
                BackColor = bgCard,
                Padding = new Padding(10)
            };
            AplicarBordesRedondeados(panelEventList, 12);

            Label lblEventListTitle = new Label
            {
                Text = "Lista de Eventos\nProgramados y retornados",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = textWhite,
                Dock = DockStyle.Top,
                Height = 38
            };
            panelEventList.Controls.Add(lblEventListTitle);

            FlowLayoutPanel flowEvents = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true
            };

            System.Threading.Tasks.Task.Run(() =>
            {
                CargarEventosEnLista(flowEvents, fechaSeleccionada);
            });

            panelEventList.Controls.Add(flowEvents);
            lblEventListTitle.SendToBack();

            // 2. Área Central del Calendario
            Panel calendarArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 10, 0)
            };

            Panel panelMonthHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.Transparent
            };

            ComboBox cbMeses = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = textWhite,
                BackColor = bgCard,
                Width = 180,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(0, 2)
            };

            DateTime baseDate = new DateTime(fechaSeleccionada.Year, 1, 1);
            for (int i = 0; i < 12; i++)
            {
                DateTime m = baseDate.AddMonths(i);
                cbMeses.Items.Add($"{m.ToString("MMMM", new CultureInfo("es-ES"))} {m.Year}");
            }
            cbMeses.SelectedIndex = fechaSeleccionada.Month - 1;

            cbMeses.SelectedIndexChanged += (s, e) =>
            {
                fechaSeleccionada = new DateTime(fechaSeleccionada.Year, cbMeses.SelectedIndex + 1, 1);
                ConstruirCalendarioEvents();
            };

            panelMonthHeader.Controls.Add(cbMeses);
            calendarArea.Controls.Add(panelMonthHeader);

            TableLayoutPanel gridDiasHeader = new TableLayoutPanel
            {
                RowCount = 1,
                ColumnCount = 7,
                Dock = DockStyle.Top,
                Height = 22,
                BackColor = Color.Transparent
            };
            for (int col = 0; col < 7; col++)
                gridDiasHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14.2857f));

            string[] dias = { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" };
            for (int i = 0; i < 7; i++)
            {
                Label lblDia = new Label
                {
                    Text = dias[i],
                    Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                    ForeColor = textMuted,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                gridDiasHeader.Controls.Add(lblDia, i, 0);
            }
            calendarArea.Controls.Add(gridDiasHeader);

            // CARGAR DÍAS CON EVENTOS
            HashSet<int> diasConEventos = new HashSet<int>();
            Conexion conexionDias = new Conexion();
            try
            {
                NpgsqlConnection conDias = conexionDias.ObtenerConexion();
                string queryDias = @"
                    SELECT DISTINCT EXTRACT(DAY FROM fecha_salida)::int AS dia
                    FROM salidas
                    WHERE EXTRACT(MONTH FROM fecha_salida) = @mes
                      AND EXTRACT(YEAR FROM fecha_salida) = @anio
                      AND (estado IS NULL OR estado <> 'Devuelto')";

                NpgsqlCommand cmdDias = new NpgsqlCommand(queryDias, conDias);
                cmdDias.Parameters.AddWithValue("@mes", fechaSeleccionada.Month);
                cmdDias.Parameters.AddWithValue("@anio", fechaSeleccionada.Year);

                NpgsqlDataReader readerDias = cmdDias.ExecuteReader();
                while (readerDias.Read())
                {
                    diasConEventos.Add(Convert.ToInt32(readerDias["dia"]));
                }
            }
            catch { }
            finally { conexionDias.CerrarConexion(); }

            DateTime primerDiaDelMes = new DateTime(fechaSeleccionada.Year, fechaSeleccionada.Month, 1);
            int diasEnMes = DateTime.DaysInMonth(fechaSeleccionada.Year, fechaSeleccionada.Month);

            TableLayoutPanel gridCalendar = new TableLayoutPanel
            {
                RowCount = 6,
                ColumnCount = 7,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            for (int col = 0; col < 7; col++)
                gridCalendar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14.2857f));
            for (int row = 0; row < 6; row++)
                gridCalendar.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666f));

            int offsetInicio = ((int)primerDiaDelMes.DayOfWeek + 6) % 7;
            int contadorDia = 1;

            for (int r = 0; r < 6; r++)
            {
                for (int c = 0; c < 7; c++)
                {
                    int indiceCelda = (r * 7) + c;
                    Panel pnlDia = new Panel
                    {
                        Dock = DockStyle.Fill,
                        Margin = new Padding(2),
                        BackColor = bgCard
                    };

                    if (indiceCelda >= offsetInicio && contadorDia <= diasEnMes)
                    {
                        bool esHoy = (contadorDia == DateTime.Now.Day && fechaSeleccionada.Month == DateTime.Now.Month && fechaSeleccionada.Year == DateTime.Now.Year);
                        if (esHoy)
                        {
                            pnlDia.BackColor = bgCardSelected;
                        }

                        bool tieneEventos = diasConEventos.Contains(contadorDia);
                        if (tieneEventos && !esHoy)
                        {
                            pnlDia.BackColor = Color.FromArgb(120, 200, 230);
                        }
                        else if (tieneEventos && esHoy)
                        {
                            pnlDia.BackColor = Color.FromArgb(255, 150, 200);
                        }

                        AplicarBordesRedondeados(pnlDia, 6);

                        Label lblNum = new Label
                        {
                            Text = contadorDia.ToString(),
                            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                            ForeColor = (esHoy || tieneEventos) ? Color.White : textWhite,
                            Location = new Point(4, 4),
                            AutoSize = true
                        };
                        pnlDia.Controls.Add(lblNum);

                        int diaActual = contadorDia;
                        DateTime fechaDelPanel = new DateTime(fechaSeleccionada.Year, fechaSeleccionada.Month, diaActual);

                        if (fechaDelPanel >= DateTime.Today)
                        {
                            pnlDia.Cursor = Cursors.Hand;
                            int diaParaEvento = diaActual;
                            bool tieneEventoEsteDia = tieneEventos;

                            pnlDia.Click += (s, ev) =>
                            {
                                if (tieneEventoEsteDia)
                                {
                                    this.Hide();

                                    try
                                    {
                                        using (var formRetorno = new FormRetornoEquipo(
                                            this.idTrabajador,
                                            this.nombreUsuario,
                                            fechaDelPanel))
                                        {
                                            if (formRetorno.ShowDialog() == DialogResult.OK)
                                            {
                                                ConstruirCalendarioEvents();
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        this.Show();
                                        this.BringToFront();
                                        this.Activate();
                                    }
                                }
                                else
                                {
                                    this.Hide();
                                    using (var formSalida = new FormSalidaEquipo(this.idTrabajador, this.nombreUsuario, fechaDelPanel))
                                    {
                                        formSalida.ShowDialog();
                                        if (formSalida.DialogResult == DialogResult.OK)
                                        {
                                            ConstruirCalendarioEvents();
                                        }
                                    }
                                    this.Show();
                                    this.BringToFront();
                                }
                            };

                            pnlDia.MouseEnter += (s, ev) => pnlDia.BackColor = Color.FromArgb(60, 62, 90);
                            pnlDia.MouseLeave += (s, ev) =>
                            {
                                if (esHoy && tieneEventoEsteDia)
                                    pnlDia.BackColor = Color.FromArgb(255, 150, 200);
                                else if (esHoy)
                                    pnlDia.BackColor = bgCardSelected;
                                else if (tieneEventoEsteDia)
                                    pnlDia.BackColor = Color.FromArgb(120, 200, 230);
                                else
                                    pnlDia.BackColor = bgCard;
                            };
                        }
                        else
                        {
                            pnlDia.Cursor = Cursors.No;
                        }

                        contadorDia++;
                    }
                    else
                    {
                        pnlDia.BackColor = Color.Transparent;
                    }

                    gridCalendar.Controls.Add(pnlDia, c, r);
                }
            }

            calendarArea.Controls.Add(gridCalendar);
            gridCalendar.BringToFront();

            panelMainContent.Controls.Add(calendarArea);
            panelMainContent.Controls.Add(panelEventList);
        }

        private void CargarEventosEnLista(FlowLayoutPanel contenedor, DateTime mes)
        {
            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();

                string query = @"
                    SELECT 
                        s.id_salida,
                        s.fecha_salida,
                        s.encargado,
                        s.ubicacion,
                        s.detalles,
                        s.estado,
                        e.hora_inicio,
                        e.hora_fin,
                        e.titulo AS titulo_evento,
                        STRING_AGG(eq.nombre, ', ') AS equipos,
                        CASE 
                            WHEN EXISTS (
                                SELECT 1 FROM retornos r 
                                WHERE r.id_salida = s.id_salida
                            ) THEN 'Retornado'
                            ELSE 'Activo'
                        END AS estado_retorno
                    FROM salidas s
                    LEFT JOIN eventos e ON e.id = s.id_evento
                    LEFT JOIN salida_equipos se ON s.id_salida = se.id_salida
                    LEFT JOIN equipos eq ON se.id_equipo = eq.id
                    WHERE EXTRACT(MONTH FROM s.fecha_salida) = @mes 
                      AND EXTRACT(YEAR FROM s.fecha_salida) = @anio
                    GROUP BY s.id_salida, s.fecha_salida, s.encargado, 
                             s.ubicacion, s.detalles, s.estado,
                             e.hora_inicio, e.hora_fin, e.titulo
                    ORDER BY s.fecha_salida ASC";

                NpgsqlCommand cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@mes", mes.Month);
                cmd.Parameters.AddWithValue("@anio", mes.Year);

                NpgsqlDataReader reader = cmd.ExecuteReader();

                bool hay = false;
                while (reader.Read())
                {
                    hay = true;
                    DateTime fecha = Convert.ToDateTime(reader["fecha_salida"]);
                    string encargado = reader["encargado"].ToString();
                    string ubicacion = reader["ubicacion"].ToString();
                    string equipos = reader["equipos"]?.ToString() ?? "";
                    string estadoRetorno = reader["estado_retorno"].ToString();
                    string tituloEvento = reader["titulo_evento"]?.ToString() ?? "";

                    string horaTexto = "00:00";
                    if (reader["hora_inicio"] != DBNull.Value)
                    {
                        object valorHora = reader["hora_inicio"];

                        if (valorHora is TimeOnly timeOnly)
                            horaTexto = timeOnly.ToString("HH:mm");
                        else if (valorHora is TimeSpan timeSpan)
                            horaTexto = timeSpan.ToString(@"hh\:mm");
                        else
                        {
                            string str = valorHora.ToString();
                            if (TimeSpan.TryParse(str, out TimeSpan ts))
                                horaTexto = ts.ToString(@"hh\:mm");
                        }
                    }

                    string fechaTexto = fecha.ToString("dd MMM yyyy", new CultureInfo("es-ES"));

                    string titulo = estadoRetorno == "Retornado"
                        ? $"✅ Retorno - {encargado}"
                        : $"📤 Salida - {encargado}";

                    string detalles = $"{equipos} | {ubicacion}";

                    Color colorTitulo = estadoRetorno == "Retornado"
                        ? Color.FromArgb(100, 220, 160)
                        : textWhite;

                    Panel tarjeta = CrearTarjetaEvento(fechaTexto, titulo, detalles, horaTexto, colorTitulo);

                    if (contenedor.InvokeRequired)
                        contenedor.Invoke(new Action(() => contenedor.Controls.Add(tarjeta)));
                    else
                        contenedor.Controls.Add(tarjeta);
                }

                if (!hay)
                {
                    Label lblVacio = new Label
                    {
                        Text = "No hay salidas programadas este mes.",
                        Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                        ForeColor = textMuted,
                        AutoSize = true,
                        Margin = new Padding(10)
                    };

                    if (contenedor.InvokeRequired)
                        contenedor.Invoke(new Action(() => contenedor.Controls.Add(lblVacio)));
                    else
                        contenedor.Controls.Add(lblVacio);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar eventos:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }
        }

        private Panel CrearTarjetaEvento(string fecha, string titulo, string detalles, string hora, Color colorTitulo)
        {
            Panel card = new Panel
            {
                Size = new Size(215, 70),
                Margin = new Padding(0, 0, 0, 8),
                BackColor = Color.FromArgb(24, 25, 40)
            };
            AplicarBordesRedondeados(card, 8);

            Label lblFecha = new Label
            {
                Text = fecha,
                Font = new Font("Segoe UI", 7f),
                ForeColor = purpleAccent,
                Location = new Point(6, 6),
                AutoSize = true
            };

            Label lblTitulo = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = colorTitulo,
                Location = new Point(6, 20),
                AutoSize = true
            };

            Label lblHora = new Label
            {
                Text = $"🕒 {hora}",
                Font = new Font("Segoe UI", 7f),
                ForeColor = textMuted,
                Location = new Point(6, 44),
                AutoSize = true
            };

            Label lblDetalles = new Label
            {
                Text = detalles,
                Font = new Font("Segoe UI", 6.5f),
                ForeColor = textMuted,
                Location = new Point(6, 57),
                Size = new Size(200, 15),
                AutoEllipsis = true
            };

            card.Controls.Add(lblFecha);
            card.Controls.Add(lblTitulo);
            card.Controls.Add(lblHora);
            card.Controls.Add(lblDetalles);

            return card;
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

        private void FormTrabajador_Load(object sender, EventArgs e)
        {
        }

        // 🚀 MÉTODO PARA EL TEMA GLOBAL (Preparado para el futuro)
        private void AplicarTemaGlobal(bool esOscuro)
        {
            // Si decides implementar el tema claro completo, aquí debes
            // cambiar los colores de this.BackColor, panelSidebar, etc.
            // y luego recargar los paneles hijos.

            /*
            this.BackColor = esOscuro ? bgMain : Color.White;
            panelSidebar.BackColor = esOscuro ? bgSidebar : Color.FromArgb(240, 240, 240);
            // etc...
            */
        }
    }
}