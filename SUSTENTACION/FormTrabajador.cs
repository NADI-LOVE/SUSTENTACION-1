using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SUSTENTACION;
using SUSTENTACION.PanelTrabajador;

namespace SUSTENTACION
{
    public partial class FormTrabajador : Form
    {
        // Paleta de colores consistente
        private readonly Color greenPrimary = Color.FromArgb(76, 175, 80);
        private readonly Color greenHover = Color.FromArgb(67, 160, 71);
        private readonly Color bgLight = Color.FromArgb(245, 247, 250);
        private readonly Color bgWhite = Color.White;
        private readonly Color textDark = Color.FromArgb(33, 33, 33);
        private readonly Color textGray = Color.FromArgb(117, 117, 117);

        // Referencias a paneles principales y estado de navegación
        private Panel panelSidebar;
        private Panel panelHeader;
        private Panel panelMainContent;
        private Button botonActivo = null;

        // Propiedades del usuario que inicia sesión
        private string nombreUsuario;
        private string rolUsuario;

        public FormTrabajador(string nombre = "Trabajador", string rol = "Operador")
        {
            InitializeComponent();
            this.nombreUsuario = nombre ?? "Trabajador";
            this.rolUsuario = rol ?? "Operador";

            ConfigurarVentana();
            CrearSidebar();
            CrearHeader();
            CrearMainContent();
        }

        private void ConfigurarVentana()
        {
            this.Text = "Panel de Trabajador - Sistema de Gestión de Almacén";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = bgLight;
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            this.DoubleBuffered = true;
        }

        private void CrearSidebar()
        {
            panelSidebar = new Panel
            {
                Width = 220,
                Dock = DockStyle.Left,
                BackColor = greenPrimary,
                Padding = new Padding(10)
            };
            this.Controls.Add(panelSidebar);

            // Título / Logo
            Label lblBrand = new Label
            {
                Text = "ALMACÉN",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 60,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelSidebar.Controls.Add(lblBrand);

            // Contenedor de botones para mantenerlos ordenados
            FlowLayoutPanel menuPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true
            };
            panelSidebar.Controls.Add(menuPanel);
            lblBrand.SendToBack();

            // Creación de botones del menú
            AgregarBotonMenu(menuPanel, "📥 Registrar Entrada", "Registrar Entrada");
            AgregarBotonMenu(menuPanel, "📤 Registrar Salida", "Registrar Salida");
            AgregarBotonMenu(menuPanel, "📦 Gestión Equipos", "Gestión de Equipos");
            AgregarBotonMenu(menuPanel, "🛠️ Stock Productos", "Stock Productos");
            AgregarBotonMenu(menuPanel, "💬 Soporte", "Soporte");

            // Botón de Cerrar Sesión en la parte inferior
            Button btnCerrarSesion = new Button
            {
                Text = "🚪 Cerrar Sesión",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(200, 230, 80, 80),
                FlatStyle = FlatStyle.Flat,
                Height = 40,
                Width = 195,
                Dock = DockStyle.Bottom,
                Margin = new Padding(0, 10, 0, 10),
                Cursor = Cursors.Hand
            };
            btnCerrarSesion.FlatAppearance.BorderSize = 0;
            btnCerrarSesion.Click += BtnCerrarSesion_Click;
            AplicarBordesRedondeados(btnCerrarSesion, 12);
            panelSidebar.Controls.Add(btnCerrarSesion);
        }

        private void AgregarBotonMenu(Control contenedor, string texto, string tag)
        {
            Button btn = new Button
            {
                Text = texto,
                Tag = tag,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Height = 42,
                Width = 195,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0),
                Margin = new Padding(0, 0, 0, 8),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += MenuButton_Click;

            btn.MouseEnter += (s, e) => { if (btn != botonActivo) btn.BackColor = greenHover; };
            btn.MouseLeave += (s, e) => { if (btn != botonActivo) btn.BackColor = Color.Transparent; };

            contenedor.Controls.Add(btn);
        }

        // Evento que gestiona las opciones del menú
        private void MenuButton_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            ActivarBoton(btn);

            string opcion = btn.Tag?.ToString();

            switch (opcion)
            {
                case "Registrar Entrada":
                case "Registrar Salida":
                    // Cargamos el UserControl de Salida y Entrada de Equipos
                    AbrirUserControl(new SalidaEntradaEquipos());
                    break;

                case "Gestión de Equipos":
                    // Muestra la interfaz de Gestión de Equipos / Almacén TV con la tabla
                    AbrirUserControl(new GestionEquipo());
                    break;

                case "Stock Productos":
                    // Limpia el contenido para dejarlo vacío
                    panelMainContent.Controls.Clear();
                    break;

                case "Soporte":
                    // Reservado para el módulo de soporte
                    panelMainContent.Controls.Clear();
                    break;
            }
        }

        // Método encargado de limpiar el panel central y cargar la nueva vista
        private void AbrirUserControl(UserControl uc)
        {
            panelMainContent.Controls.Clear();
            uc.Dock = DockStyle.Fill;
            panelMainContent.Controls.Add(uc);
            uc.BringToFront();
        }

        // Cambia la apariencia visual del botón seleccionado
        private void ActivarBoton(Button btn)
        {
            if (botonActivo != null)
            {
                botonActivo.BackColor = Color.Transparent;
                botonActivo.ForeColor = Color.White;
            }

            botonActivo = btn;
            botonActivo.BackColor = Color.White;
            botonActivo.ForeColor = greenPrimary;
            AplicarBordesRedondeados(botonActivo, 12);
        }

        private void CrearHeader()
        {
            panelHeader = new Panel
            {
                Height = 60,
                Dock = DockStyle.Top,
                BackColor = bgLight,
                Padding = new Padding(20, 10, 20, 10)
            };
            this.Controls.Add(panelHeader);

            Panel panelSearch = new Panel
            {
                Size = new Size(260, 36),
                Location = new Point(20, 12),
                BackColor = bgWhite
            };
            AplicarBordesRedondeados(panelSearch, 15);

            TextBox txtSearch = new TextBox
            {
                Text = "Buscar en inventario...",
                Font = new Font("Segoe UI", 9f),
                ForeColor = textGray,
                BorderStyle = BorderStyle.None,
                Size = new Size(210, 20),
                Location = new Point(15, 9)
            };
            panelSearch.Controls.Add(txtSearch);
            panelHeader.Controls.Add(panelSearch);

            Label lblUser = new Label
            {
                Text = $"👤 {nombreUsuario} ({rolUsuario})",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = textDark,
                Location = new Point(panelHeader.Width - 280, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AutoSize = true
            };
            panelHeader.Controls.Add(lblUser);
        }

        private void CrearMainContent()
        {
            panelMainContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgLight,
                AutoScroll = true,
                Padding = new Padding(25)
            };
            this.Controls.Add(panelMainContent);
            panelMainContent.BringToFront();

            CargarDashboardDefault();
        }

        private void CargarDashboardDefault()
        {
            panelMainContent.Controls.Clear();

            FlowLayoutPanel flowContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            // FILA 1: STATUS USUARIO + KPIs
            FlowLayoutPanel rowTop = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0, 0, 0, 20)
            };

            Panel cardUserStatus = new Panel
            {
                Size = new Size(230, 185),
                Margin = new Padding(0, 0, 20, 10),
                BackColor = bgWhite
            };
            AplicarBordesRedondeados(cardUserStatus, 15);

            Label lblStatusTitle = new Label
            {
                Text = "Turno Disponible",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = greenPrimary,
                Location = new Point(55, 12),
                AutoSize = true
            };
            cardUserStatus.Controls.Add(lblStatusTitle);

            Label lblAvatar = new Label
            {
                Text = "🧑‍💼",
                Font = new Font("Segoe UI", 28f),
                Location = new Point(85, 38),
                AutoSize = true
            };
            cardUserStatus.Controls.Add(lblAvatar);

            Label lblWorkingInfo = new Label
            {
                Text = $"{nombreUsuario}\n08:00 AM - 05:00 PM",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = textDark,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(35, 120),
                AutoSize = true
            };
            cardUserStatus.Controls.Add(lblWorkingInfo);
            rowTop.Controls.Add(cardUserStatus);

            FlowLayoutPanel gridKPIs = new FlowLayoutPanel
            {
                Size = new Size(550, 185),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0)
            };

            gridKPIs.Controls.Add(CrearKPICard("40", "Entradas Hoy", "Ayer: 32 Entradas"));
            gridKPIs.Controls.Add(CrearKPICard("21", "Salidas Hoy", "Ayer: 18 Salidas"));
            gridKPIs.Controls.Add(CrearKPICard("14", "Revisiones", "Pendientes: 2"));
            gridKPIs.Controls.Add(CrearKPICard("15", "Categorías", "Actualizadas"));
            gridKPIs.Controls.Add(CrearKPICard("36", "Productos", "Bajo Stock"));
            gridKPIs.Controls.Add(CrearKPICard("S/ 52,140", "Valor Almacén", "Total registrado"));

            rowTop.Controls.Add(gridKPIs);
            flowContainer.Controls.Add(rowTop);

            // FILA 2: GRÁFICO + CALENDARIO
            FlowLayoutPanel rowBottom = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0)
            };

            Panel cardChart = new Panel
            {
                Size = new Size(500, 260),
                Margin = new Padding(0, 0, 20, 10),
                BackColor = bgWhite
            };
            AplicarBordesRedondeados(cardChart, 15);

            Label lblChartTitle = new Label
            {
                Text = "Reporte de Movimientos Mensual",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = textDark,
                Location = new Point(20, 15),
                AutoSize = true
            };
            cardChart.Controls.Add(lblChartTitle);

            cardChart.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                int[] entradas = { 40, 60, 30, 80, 50, 90, 70 };
                int[] salidas = { 30, 40, 50, 60, 40, 80, 60 };

                int x = 40;
                for (int i = 0; i < entradas.Length; i++)
                {
                    g.FillRectangle(new SolidBrush(greenPrimary), x, 210 - entradas[i] * 1.5f, 14, entradas[i] * 1.5f);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(240, 180, 40)), x + 18, 210 - salidas[i] * 1.5f, 14, salidas[i] * 1.5f);
                    x += 62;
                }
            };
            rowBottom.Controls.Add(cardChart);

            Panel cardCalendar = new Panel
            {
                Size = new Size(280, 260),
                Margin = new Padding(0, 0, 0, 10),
                BackColor = bgWhite
            };
            AplicarBordesRedondeados(cardCalendar, 15);

            MonthCalendar calendar = new MonthCalendar
            {
                Location = new Point(22, 20),
                ShowTodayCircle = true
            };
            cardCalendar.Controls.Add(calendar);
            rowBottom.Controls.Add(cardCalendar);

            flowContainer.Controls.Add(rowBottom);
            panelMainContent.Controls.Add(flowContainer);
        }

        private Panel CrearKPICard(string valor, string titulo, string subtexto)
        {
            Panel card = new Panel
            {
                Size = new Size(165, 85),
                Margin = new Padding(0, 0, 15, 12),
                BackColor = bgWhite
            };
            AplicarBordesRedondeados(card, 12);

            Label lblV = new Label
            {
                Text = valor,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = textDark,
                Location = new Point(10, 8),
                AutoSize = true
            };
            card.Controls.Add(lblV);

            Label lblT = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = greenPrimary,
                Location = new Point(10, 34),
                AutoSize = true
            };
            card.Controls.Add(lblT);

            Label lblS = new Label
            {
                Text = subtexto,
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = textGray,
                Location = new Point(10, 56),
                AutoSize = true
            };
            card.Controls.Add(lblS);

            return card;
        }

        private void BtnCerrarSesion_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("¿Estás seguro de que deseas cerrar sesión?",
                                                "Cerrar Sesión",
                                                MessageBoxButtons.YesNo,
                                                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                Form1 login = new Form1();
                login.Show();
                this.Close();
            }
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