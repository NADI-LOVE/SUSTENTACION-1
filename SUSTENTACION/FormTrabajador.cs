using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SUSTENTACION
{
    public partial class FormTrabajador : Form
    {
        // Paleta de Colores (Light / Emerald Green)
        private readonly Color greenPrimary = Color.FromArgb(0, 178, 107);
        private readonly Color greenHover = Color.FromArgb(0, 150, 90);
        private readonly Color bgLight = Color.FromArgb(242, 244, 247);
        private readonly Color bgWhite = Color.White;
        private readonly Color textDark = Color.FromArgb(40, 50, 60);
        private readonly Color textGray = Color.FromArgb(130, 140, 150);

        // Paneles principales
        private Panel panelSidebar;
        private Panel panelHeader;
        private Panel panelMainContent;

        // Datos del usuario
        private string nombreUsuario;
        private string rolUsuario;

        public FormTrabajador(string nombre, string rol)
        {
            InitializeComponent();
            this.nombreUsuario = nombre;
            this.rolUsuario = rol;

            ConstruirInterfaz();
        }

        private void ConstruirInterfaz()
        {
            // Ventana Principal mejor proporcionada
            this.Size = new Size(1360, 780);
            this.MinimumSize = new Size(1150, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Artemusa Inventario - Panel de Trabajador";
            this.BackColor = bgLight;
            this.DoubleBuffered = true;

            // EL ORDEN DEL DOCK ES CLAVE: 1. Sidebar, 2. Header, 3. MainContent
            CrearSidebar();
            CrearHeader();
            CrearMainContent();
        }

        private void CrearSidebar()
        {
            panelSidebar = new Panel
            {
                Width = 220,
                Dock = DockStyle.Left,
                BackColor = greenPrimary,
                Padding = new Padding(15)
            };
            this.Controls.Add(panelSidebar);

            // LOGO Superior
            Label lblLogo = new Label
            {
                Text = "💚 ARTEMUSA",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 20),
                AutoSize = true
            };
            panelSidebar.Controls.Add(lblLogo);

            Label lblRoleSub = new Label
            {
                Text = "PANEL TRABAJADOR",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 245, 220),
                Location = new Point(18, 45),
                AutoSize = true
            };
            panelSidebar.Controls.Add(lblRoleSub);

            // Botón Activo
            Button btnDashboard = new Button
            {
                Text = "  Dashboard",
                Size = new Size(190, 40),
                Location = new Point(15, 85),
                BackColor = Color.White,
                ForeColor = greenPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };
            btnDashboard.FlatAppearance.BorderSize = 0;
            AplicarBordesRedondeados(btnDashboard, 12);
            panelSidebar.Controls.Add(btnDashboard);

            // Menú de opciones
            string[] opciones = { "Mis Pedidos", "Stock Productos", "Registrar Entrada", "Registrar Salida", "Soporte" };
            int topPos = 135;

            foreach (string opc in opciones)
            {
                Button btn = new Button
                {
                    Text = $"  {opc}",
                    Size = new Size(190, 36),
                    Location = new Point(15, topPos),
                    BackColor = Color.Transparent,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = greenHover;
                panelSidebar.Controls.Add(btn);
                topPos += 42;
            }

            // BOTÓN CERRAR SESIÓN (Fijo abajo)
            Button btnCerrarSesion = new Button
            {
                Text = "🚪  Cerrar Sesión",
                Size = new Size(190, 40),
                Location = new Point(15, panelSidebar.Height - 60),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };
            btnCerrarSesion.FlatAppearance.BorderSize = 0;
            AplicarBordesRedondeados(btnCerrarSesion, 10);
            btnCerrarSesion.Click += BtnCerrarSesion_Click;

            panelSidebar.Controls.Add(btnCerrarSesion);
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

            // Caja de búsqueda ajustada
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

            // Perfil Usuario (Arriba a la derecha)
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
                Padding = new Padding(20)
            };
            this.Controls.Add(panelMainContent);

            // 1. Tarjeta Usuario / Turno (Lado Izquierdo Arriba)
            Panel cardUserStatus = new Panel
            {
                Size = new Size(250, 190),
                Location = new Point(20, 10),
                BackColor = bgWhite
            };
            AplicarBordesRedondeados(cardUserStatus, 15);

            Label lblStatusTitle = new Label
            {
                Text = "Turno Disponible",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = greenPrimary,
                Location = new Point(65, 12),
                AutoSize = true
            };
            cardUserStatus.Controls.Add(lblStatusTitle);

            Label lblAvatar = new Label
            {
                Text = "🧑‍💼",
                Font = new Font("Segoe UI", 32f),
                Location = new Point(95, 40),
                AutoSize = true
            };
            cardUserStatus.Controls.Add(lblAvatar);

            Label lblWorkingInfo = new Label
            {
                Text = $"{nombreUsuario}\n08:00 AM - 05:00 PM",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = textDark,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(45, 125),
                AutoSize = true
            };
            cardUserStatus.Controls.Add(lblWorkingInfo);
            panelMainContent.Controls.Add(cardUserStatus);

            // 2. Bloque de Muestras / KPI Cards (Matriz de 3x2 organizada)
            int kpiX = 290;
            int kpiY = 10;
            int kpiWidth = 160;
            int kpiHeight = 85;

            // Fila 1
            CrearKPICard("40", "Entradas Hoy", "Ayer: 32 Entradas", kpiX, kpiY, kpiWidth, kpiHeight);
            CrearKPICard("21", "Salidas Hoy", "Ayer: 18 Salidas", kpiX + 175, kpiY, kpiWidth, kpiHeight);
            CrearKPICard("14", "Revisiones", "Pendientes: 2", kpiX + 350, kpiY, kpiWidth, kpiHeight);

            // Fila 2
            CrearKPICard("15", "Categorías", "Actualizadas", kpiX, kpiY + 100, kpiWidth, kpiHeight);
            CrearKPICard("36", "Productos", "Bajo Stock", kpiX + 175, kpiY + 100, kpiWidth, kpiHeight);
            CrearKPICard("S/ 52,140", "Valor Almacén", "Total registrado", kpiX + 350, kpiY + 100, kpiWidth, kpiHeight);

            // 3. Gráfico (Lado Izquierdo Abajo)
            Panel cardChart = new Panel
            {
                Size = new Size(520, 260),
                Location = new Point(20, 220),
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

                int x = 45;
                for (int i = 0; i < entradas.Length; i++)
                {
                    g.FillRectangle(new SolidBrush(greenPrimary), x, 210 - entradas[i] * 1.6f, 14, entradas[i] * 1.6f);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(240, 180, 40)), x + 18, 210 - salidas[i] * 1.6f, 14, salidas[i] * 1.6f);
                    x += 65;
                }
            };
            panelMainContent.Controls.Add(cardChart);

            // 4. Calendario (Lado Derecho Abajo)
            Panel cardCalendar = new Panel
            {
                Size = new Size(270, 260),
                Location = new Point(560, 220),
                BackColor = bgWhite
            };
            AplicarBordesRedondeados(cardCalendar, 15);

            MonthCalendar calendar = new MonthCalendar
            {
                Location = new Point(18, 20),
                ShowTodayCircle = true
            };
            cardCalendar.Controls.Add(calendar);
            panelMainContent.Controls.Add(cardCalendar);
        }

        private void CrearKPICard(string valor, string titulo, string subtexto, int x, int y, int width, int height)
        {
            Panel card = new Panel
            {
                Size = new Size(width, height),
                Location = new Point(x, y),
                BackColor = bgWhite
            };
            AplicarBordesRedondeados(card, 12);

            Label lblV = new Label
            {
                Text = valor,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = textDark,
                Location = new Point(12, 8),
                AutoSize = true
            };
            card.Controls.Add(lblV);

            Label lblT = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = greenPrimary,
                Location = new Point(12, 36),
                AutoSize = true
            };
            card.Controls.Add(lblT);

            Label lblS = new Label
            {
                Text = subtexto,
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = textGray,
                Location = new Point(12, 58),
                AutoSize = true
            };
            card.Controls.Add(lblS);

            panelMainContent.Controls.Add(card);
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
            GraphicsPath path = new GraphicsPath();
            path.AddArc(0, 0, radio, radio, 180, 90);
            path.AddArc(control.Width - radio, 0, radio, radio, 270, 90);
            path.AddArc(control.Width - radio, control.Height - radio, radio, radio, 0, 90);
            path.AddArc(0, control.Height - radio, radio, radio, 90, 90);
            path.CloseAllFigures();
            control.Region = new Region(path);
        }
    }
}