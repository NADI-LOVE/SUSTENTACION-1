using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;
// Se agrega la directiva using para acceder al UserControl GestionEquipo
using SUSTENTACION.PanelTrabajador;

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

        public FormTrabajador(string nombre = "María López", string rol = "Trabajador")
        {
            InitializeComponent();
            this.nombreUsuario = nombre;
            this.rolUsuario = rol;

            ConfigurarVentana();
            InicializarTimerSidebar();
            CrearSidebar();
            CrearHeader();
            CrearMainContent();
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
        }

        private void InicializarTimerSidebar()
        {
            timerSidebar = new System.Windows.Forms.Timer();
            timerSidebar.Interval = 10; // Velocidad de la animación
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

            // CAMBIO: Se cambió el nombre de "Mensajes" a "Gestión de Equipos"
            AgregarBotonMenu(menuPanel, "💬", "Gestión de Equipos", "Chat", false);

            // CAMBIO: Se cambió el nombre de "estudiantes" a "Informacion"
            AgregarBotonMenu(menuPanel, "ℹ️", "Información", "Info", false);
            AgregarBotonMenu(menuPanel, "👥", "Docentes", "Teacher", false);
            AgregarBotonMenu(menuPanel, "📅", "Eventos", "Event", false);

            AgregarEtiquetaSeccion(menuPanel, "Otros");
            AgregarBotonMenu(menuPanel, "📊", "Finanzas", "Finance", false);
            AgregarBotonMenu(menuPanel, "🍴", "Servicios", "Food", false);
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
                // Gestionar estado visual de botones
                if (botonActivo != null)
                {
                    botonActivo.BackColor = Color.Transparent;
                    botonActivo.ForeColor = textMuted;
                }
                botonActivo = btn;
                btn.BackColor = purpleAccent;
                btn.ForeColor = textWhite;

                // Lógica de navegación
                string opcion = btn.Tag?.ToString();
                panelMainContent.Controls.Clear(); // Limpiar panel principal

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
                    case "Info": // NUEVO CASO
                        InfoPanel vistaInfo = new InfoPanel();
                        vistaInfo.Dock = DockStyle.Fill;
                        panelMainContent.Controls.Add(vistaInfo);
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

            // Botón Hamburguesa de Tres Rayas (Inicia la animación)
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

            // Botón Cerrar Sesión (Reemplaza a Buscar y + Nuevo)
            Button btnCerrarSesion = new Button
            {
                Text = "🚪 Cerrar Sesión",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = textWhite,
                BackColor = Color.FromArgb(220, 53, 69), // Color rojo elegante
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
                if (MessageBox.Show("¿Está seguro de que desea cerrar sesión?", "ARTEMISA", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
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
                Text = "Lista de Eventos\nActividades programadas",
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
            flowEvents.Controls.Add(CrearTarjetaEvento("Septiembre 2026", "Reunión de ARTEMISA", "Gratis", "09:00 - 10:00 AM"));
            flowEvents.Controls.Add(CrearTarjetaEvento("Septiembre 2026", "Capacitación Trabajadores", "$10.0", "02:00 - 05:00 PM"));
            flowEvents.Controls.Add(CrearTarjetaEvento("Septiembre 2026", "Sustentación de Proyecto", "Gratis", "08:00 - 12:00 PM"));

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

                        AplicarBordesRedondeados(pnlDia, 6);

                        Label lblNum = new Label
                        {
                            Text = contadorDia.ToString(),
                            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                            ForeColor = esHoy ? Color.Black : textWhite,
                            Location = new Point(4, 4),
                            AutoSize = true
                        };
                        pnlDia.Controls.Add(lblNum);
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

        private Panel CrearTarjetaEvento(string fecha, string titulo, string precio, string hora)
        {
            Panel card = new Panel
            {
                Size = new Size(215, 70),
                Margin = new Padding(0, 0, 0, 8),
                BackColor = Color.FromArgb(24, 25, 40)
            };
            AplicarBordesRedondeados(card, 8);

            Label lblFecha = new Label { Text = fecha, Font = new Font("Segoe UI", 7f), ForeColor = purpleAccent, Location = new Point(6, 6), AutoSize = true };
            Label lblTitulo = new Label { Text = titulo, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = textWhite, Location = new Point(6, 20), AutoSize = true };
            Label lblHora = new Label { Text = $"🕒 {hora}", Font = new Font("Segoe UI", 7f), ForeColor = textMuted, Location = new Point(6, 44), AutoSize = true };
            Label lblPrecio = new Label { Text = precio, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = textWhite, Location = new Point(170, 20), AutoSize = true };

            card.Controls.Add(lblFecha);
            card.Controls.Add(lblTitulo);
            card.Controls.Add(lblHora);
            card.Controls.Add(lblPrecio);

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
    }
}