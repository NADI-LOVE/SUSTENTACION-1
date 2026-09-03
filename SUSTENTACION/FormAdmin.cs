using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SUSTENTACION
{
    public partial class FormAdmin : Form
    {
        // Paleta Dark Mode / Neon
        private readonly Color bgDark = Color.FromArgb(18, 18, 18);
        private readonly Color bgSidebar = Color.FromArgb(24, 24, 24);
        private readonly Color bgCard = Color.FromArgb(32, 32, 32);
        private readonly Color accentGreen = Color.FromArgb(163, 230, 53); // #A3E635
        private readonly Color textWhite = Color.FromArgb(240, 240, 240);
        private readonly Color textGray = Color.FromArgb(150, 150, 150);

        // Paneles principales
        private Panel panelSidebar;
        private Panel panelRightSidebar;
        private Panel panelHeader;
        private Panel panelMainContent;

        // Datos del usuario
        private string nombreUsuario;
        private string rolUsuario;

        public FormAdmin(string nombre, string rol)
        {
            InitializeComponent();
            this.nombreUsuario = nombre;
            this.rolUsuario = rol;

            ConstruirInterfaz();
        }

        private void ConstruirInterfaz()
        {
            // Ventana Principal
            this.Size = new Size(1350, 800);
            this.MinimumSize = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Artemusa Inventario - Panel de Administración";
            this.BackColor = bgDark;
            this.DoubleBuffered = true;

            // 1. Sidebar Izquierdo (Menú)
            CrearSidebar();

            // 2. Sidebar Derecho (Notificaciones)
            CrearRightSidebar();

            // 3. Header Superior (Barra con usuario y Cerrar Sesión)
            CrearHeader();

            // 4. Panel de Contenido Central (Tarjetas y Tablas)
            CrearMainContent();
        }

        private void CrearSidebar()
        {
            panelSidebar = new Panel
            {
                Width = 220,
                Dock = DockStyle.Left,
                BackColor = bgSidebar,
                Padding = new Padding(15)
            };
            this.Controls.Add(panelSidebar);

            // LOGO / Nombre
            Label lblLogo = new Label
            {
                Text = "ARTEMUSA",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = accentGreen,
                Location = new Point(15, 20),
                AutoSize = true
            };
            panelSidebar.Controls.Add(lblLogo);

            Label lblDashboards = new Label
            {
                Text = "DASHBOARDS",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = textGray,
                Location = new Point(15, 65),
                AutoSize = true
            };
            panelSidebar.Controls.Add(lblDashboards);

            // Botón Activo
            Button btnOverview = new Button
            {
                Text = "   Overview",
                Size = new Size(190, 40),
                Location = new Point(15, 90),
                BackColor = accentGreen,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };
            btnOverview.FlatAppearance.BorderSize = 0;
            AplicarBordesRedondeados(btnOverview, 12);
            panelSidebar.Controls.Add(btnOverview);

            // Menú de Navegación
            string[] menuItems = { "Inventario", "Movimientos", "Trabajadores", "Categorías", "Reportes" };
            int topPos = 140;
            foreach (string item in menuItems)
            {
                Button btn = new Button
                {
                    Text = $"   {item}",
                    Size = new Size(190, 36),
                    Location = new Point(15, topPos),
                    BackColor = Color.Transparent,
                    ForeColor = textGray,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9f),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 40, 40);
                panelSidebar.Controls.Add(btn);
                topPos += 42;
            }
        }

        private void CrearRightSidebar()
        {
            panelRightSidebar = new Panel
            {
                Width = 250,
                Dock = DockStyle.Right,
                BackColor = bgSidebar,
                Padding = new Padding(15)
            };
            this.Controls.Add(panelRightSidebar);

            // Título
            Label lblNotifTitle = new Label
            {
                Text = "Notificaciones",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(15, 20),
                AutoSize = true
            };
            panelRightSidebar.Controls.Add(lblNotifTitle);

            string[] notifs = {
                "📦 12 Productos con bajo stock.",
                "🛒 45 Nuevos registros hoy.",
                "👤 Nuevo trabajador añadido.",
                "📊 Reporte mensual generado."
            };

            int topPos = 55;
            foreach (string notif in notifs)
            {
                Label lbl = new Label
                {
                    Text = notif,
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = textGray,
                    Location = new Point(15, topPos),
                    Size = new Size(220, 35)
                };
                panelRightSidebar.Controls.Add(lbl);
                topPos += 40;
            }

            // Módulo Usuario Activo
            Label lblContacts = new Label
            {
                Text = "Usuario en sesión",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(15, topPos + 20),
                AutoSize = true
            };
            panelRightSidebar.Controls.Add(lblContacts);

            Panel cardContact = new Panel
            {
                Size = new Size(220, 50),
                Location = new Point(15, topPos + 50),
                BackColor = accentGreen
            };
            AplicarBordesRedondeados(cardContact, 15);

            Label lblContactName = new Label
            {
                Text = $"👤 {nombreUsuario}\n   Role: {rolUsuario}",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.Black,
                Location = new Point(12, 9),
                AutoSize = true
            };
            cardContact.Controls.Add(lblContactName);
            panelRightSidebar.Controls.Add(cardContact);
        }

        private void CrearHeader()
        {
            panelHeader = new Panel
            {
                Height = 60,
                Dock = DockStyle.Top,
                BackColor = bgDark,
                Padding = new Padding(20, 10, 20, 10)
            };
            this.Controls.Add(panelHeader);

            Label lblBreadcrumb = new Label
            {
                Text = "Dashboards / Overview",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = textGray,
                Location = new Point(20, 20),
                AutoSize = true
            };
            panelHeader.Controls.Add(lblBreadcrumb);

            // Botón de Cerrar Sesión alineado a la derecha
            Button btnCerrarSesion = new Button
            {
                Text = "Cerrar Sesión",
                Size = new Size(120, 35),
                Location = new Point(panelHeader.Width - 140, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.FromArgb(220, 38, 38), // Rojo
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCerrarSesion.FlatAppearance.BorderSize = 0;
            AplicarBordesRedondeados(btnCerrarSesion, 10);
            btnCerrarSesion.Click += BtnCerrarSesion_Click;

            panelHeader.Controls.Add(btnCerrarSesion);
        }

        private void CrearMainContent()
        {
            panelMainContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgDark,
                AutoScroll = true,
                Padding = new Padding(20)
            };
            this.Controls.Add(panelMainContent);

            Label lblOverview = new Label
            {
                Text = "Resumen General",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(20, 10),
                AutoSize = true
            };
            panelMainContent.Controls.Add(lblOverview);

            // --- FILA 1: KPIs ---
            CrearCardKPI("Total Productos", "1,248", "^ 3.2% este mes", 20, 55, 180);
            CrearCardKPI("Stock Disponible", "18,221", "Unidades en almacén", 215, 55, 180);
            CrearCardKPI("Meta de Ventas", "84%", "Objetivo $50K", 410, 55, 180);
            CrearCardKPI("Bajo Stock", "12", "Requieren pedido", 605, 55, 180);

            // --- FILA 2: GRÁFICO DONUT + BANNER DESTACADO ---
            Panel cardSales = new Panel
            {
                Size = new Size(490, 210),
                Location = new Point(20, 170),
                BackColor = bgCard
            };
            AplicarBordesRedondeados(cardSales, 15);

            Label lblSalesTitle = new Label
            {
                Text = "Distribución de Inventario",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(15, 15),
                AutoSize = true
            };
            cardSales.Controls.Add(lblSalesTitle);

            cardSales.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen pGreen = new Pen(accentGreen, 12))
                using (Pen pDark = new Pen(Color.FromArgb(60, 60, 60), 12))
                {
                    e.Graphics.DrawArc(pDark, 20, 55, 120, 120, 0, 360);
                    e.Graphics.DrawArc(pGreen, 20, 55, 120, 120, -90, 240);
                }
            };

            Label lblDonutText = new Label
            {
                Text = "102k\nUnidades",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(50, 95),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = true
            };
            cardSales.Controls.Add(lblDonutText);

            Label lblLegend = new Label
            {
                Text = "● Electrónica: 55,640\n\n● Herramientas: 11,420\n\n● Ropa / Accesorios: 1,840",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = textGray,
                Location = new Point(165, 60),
                AutoSize = true
            };
            cardSales.Controls.Add(lblLegend);
            panelMainContent.Controls.Add(cardSales);

            // Card Banner Promocional / Alerta
            Panel cardGreenBanner = new Panel
            {
                Size = new Size(275, 210),
                Location = new Point(525, 170),
                BackColor = Color.FromArgb(20, 50, 25)
            };
            AplicarBordesRedondeados(cardGreenBanner, 15);

            Label lblBannerPrice = new Label
            {
                Text = "Artemusa Pro",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = accentGreen,
                Location = new Point(15, 20),
                AutoSize = true
            };
            cardGreenBanner.Controls.Add(lblBannerPrice);

            Label lblBannerDesc = new Label
            {
                Text = "Gestiona permisos de usuarios, alertas automáticas de stock y exportaciones a Excel.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = textWhite,
                Location = new Point(18, 65),
                Size = new Size(240, 60)
            };
            cardGreenBanner.Controls.Add(lblBannerDesc);

            Button btnGetStarted = new Button
            {
                Text = "Ver Módulos",
                Size = new Size(235, 35),
                Location = new Point(20, 145),
                BackColor = accentGreen,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGetStarted.FlatAppearance.BorderSize = 0;
            AplicarBordesRedondeados(btnGetStarted, 10);
            cardGreenBanner.Controls.Add(btnGetStarted);

            panelMainContent.Controls.Add(cardGreenBanner);

            // --- FILA 3: TABLA DE PRODUCTOS Y MOVIMIENTOS ---
            CrearTablaProductos(20, 400);
        }

        private void CrearCardKPI(string titulo, string valor, string subtexto, int x, int y, int width)
        {
            Panel card = new Panel
            {
                Size = new Size(width, 95),
                Location = new Point(x, y),
                BackColor = bgCard
            };
            AplicarBordesRedondeados(card, 12);

            Label lblT = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 8f),
                ForeColor = textGray,
                Location = new Point(12, 10),
                AutoSize = true
            };
            card.Controls.Add(lblT);

            Label lblV = new Label
            {
                Text = valor,
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(10, 28),
                AutoSize = true
            };
            card.Controls.Add(lblV);

            Label lblSub = new Label
            {
                Text = subtexto,
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = accentGreen,
                Location = new Point(12, 68),
                AutoSize = true
            };
            card.Controls.Add(lblSub);

            panelMainContent.Controls.Add(card);
        }

        private void CrearTablaProductos(int x, int y)
        {
            Panel cardTable = new Panel
            {
                Size = new Size(780, 220),
                Location = new Point(x, y),
                BackColor = bgCard
            };
            AplicarBordesRedondeados(cardTable, 15);

            Label lblTitle = new Label
            {
                Text = "Últimos Movimientos de Almacén",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(15, 12),
                AutoSize = true
            };
            cardTable.Controls.Add(lblTitle);

            DataGridView dgv = new DataGridView
            {
                Location = new Point(15, 45),
                Size = new Size(750, 155),
                BackgroundColor = bgCard,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            // Estilos para encajar en el Modo Oscuro
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(45, 45, 45);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = accentGreen;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

            dgv.DefaultCellStyle.BackColor = bgCard;
            dgv.DefaultCellStyle.ForeColor = textWhite;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(50, 50, 50);
            dgv.DefaultCellStyle.SelectionForeColor = accentGreen;
            dgv.GridColor = Color.FromArgb(50, 50, 50);

            dgv.Columns.Add("Producto", "Producto");
            dgv.Columns.Add("Cantidad", "Cantidad");
            dgv.Columns.Add("Valor", "Valor Total");

            dgv.Columns[0].Width = 370;
            dgv.Columns[1].Width = 150;
            dgv.Columns[2].Width = 200;

            // Filas de prueba adaptadas al inventario
            dgv.Rows.Add("Laptop ASUS TUF Gaming", "15 Unidades", "$18,750");
            dgv.Rows.Add("Teclado Mecánico RGB", "45 Unidades", "$2,250");
            dgv.Rows.Add("Monitor LG 27\" IPS 144Hz", "20 Unidades", "$5,800");

            cardTable.Controls.Add(dgv);
            panelMainContent.Controls.Add(cardTable);
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