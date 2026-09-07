using MySql.Data.MySqlClient;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SUSTENTACION
{
    public partial class Form1 : Form
    {
        // Contenedor principal personalizado con la división diagonal
        private DiagonalPanel panelBackground;

        // Controles del lado izquierdo (Blanco)
        private Label lblLogoIcon;
        private Label lblAppTitle;
        private Label lblAppSubtitle;

        // Controles del lado derecho (Azul - Formulario)
        private Label lblEmail;
        private TextBox txtEmail;
        private Label lblPassword;
        private TextBox txtPassword;
        private CheckBox chkRememberMe;
        private LinkLabel lblRecoverPassword;
        private Button btnLogin;

        public Form1()
        {
            InitializeComponent();
            ConstruirInterfaz();
        }

        private void ConstruirInterfaz()
        {
            // --- Configuración básica de la ventana ---
            this.Size = new Size(850, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Text = "Iniciar Sesión - Artemusa Inventario";

            // Panel contenedor que dibuja la división diagonal tipo UI moderna
            panelBackground = new DiagonalPanel
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(panelBackground);

            // ==========================================
            // LADO IZQUIERDO: Marca / Logotipo (Fondo Blanco)
            // ==========================================

            // Ícono de flor/hoja (Representación mediante símbolo)
            lblLogoIcon = new Label
            {
                Text = "🍁",
                Font = new Font("Segoe UI Symbol", 55, FontStyle.Regular),
                ForeColor = Color.FromArgb(28, 86, 170),
                AutoSize = true,
                Location = new Point(140, 150),
                BackColor = Color.Transparent
            };
            panelBackground.Controls.Add(lblLogoIcon);

            // Título Principal
            lblAppTitle = new Label
            {
                Text = "ARTEMUSA",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 50, 100),
                AutoSize = true,
                Location = new Point(100, 240),
                BackColor = Color.Transparent
            };
            panelBackground.Controls.Add(lblAppTitle);

            // Subtítulo
            lblAppSubtitle = new Label
            {
                Text = "Sistema de Gestión e Inventario",
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 110, 130),
                AutoSize = true,
                Location = new Point(95, 280),
                BackColor = Color.Transparent
            };
            panelBackground.Controls.Add(lblAppSubtitle);


            // ==========================================
            // LADO DERECHO: Formulario de Login (Fondo Azul)
            // ==========================================

            int startX = 490; // Posición horizontal para alinear controles en el lado azul

            // Label Correo Electrónico
            lblEmail = new Label
            {
                Text = "Tu correo electrónico",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(220, 235, 255),
                Location = new Point(startX, 115),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            panelBackground.Controls.Add(lblEmail);

            // Input Correo Electrónico
            txtEmail = new TextBox
            {
                Font = new Font("Segoe UI", 11f),
                Location = new Point(startX, 140),
                Size = new Size(270, 30),
                ForeColor = Color.FromArgb(40, 40, 40),
                BorderStyle = BorderStyle.FixedSingle
            };
            panelBackground.Controls.Add(txtEmail);

            // Label Contraseña
            lblPassword = new Label
            {
                Text = "Contraseña",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(220, 235, 255),
                Location = new Point(startX, 190),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            panelBackground.Controls.Add(lblPassword);

            // Input Contraseña
            txtPassword = new TextBox
            {
                Font = new Font("Segoe UI", 11f),
                Location = new Point(startX, 215),
                Size = new Size(270, 30),
                UseSystemPasswordChar = true,
                ForeColor = Color.FromArgb(40, 40, 40),
                BorderStyle = BorderStyle.FixedSingle
            };
            panelBackground.Controls.Add(txtPassword);

            // Checkbox Recordarme
            chkRememberMe = new CheckBox
            {
                Text = "Recordarme",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(220, 235, 255),
                Location = new Point(startX, 260),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            panelBackground.Controls.Add(chkRememberMe);

            // Enlace Recuperar Contraseña
            lblRecoverPassword = new LinkLabel
            {
                Text = "Recuperar contraseña",
                Font = new Font("Segoe UI", 8.5f),
                LinkColor = Color.FromArgb(170, 205, 255),
                ActiveLinkColor = Color.White,
                VisitedLinkColor = Color.FromArgb(170, 205, 255),
                AutoSize = true,
                Location = new Point(startX + 140, 261),
                BackColor = Color.Transparent
            };
            panelBackground.Controls.Add(lblRecoverPassword);

            // Botón Iniciar Sesión (Azul más claro destacado)
            btnLogin = new Button
            {
                Text = "INICIAR SESIÓN",
                Size = new Size(270, 40),
                Location = new Point(startX, 305),
                BackColor = Color.FromArgb(85, 130, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;
            panelBackground.Controls.Add(btnLogin);
        }

        // --- Lógica de Inicio de Sesión ---
        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Por favor ingresa tu correo y contraseña.", "Campos requeridos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Conexion conexionBD = new Conexion();

            try
            {
                using (MySqlConnection conn = conexionBD.ObtenerConexion())
                {
                    string query = @"SELECT t.nombre, r.nombre_rol 
                                     FROM trabajadores t 
                                     INNER JOIN roles r ON t.id_rol = r.id_rol 
                                     WHERE t.correo = @correo AND t.password = @password";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@correo", email);
                        cmd.Parameters.AddWithValue("@password", password);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string nombreTrabajador = reader["nombre"].ToString();
                                string nombreRol = reader["nombre_rol"].ToString();

                                MessageBox.Show($"¡Bienvenido {nombreTrabajador}!\nSe inició sesión con éxito como: {nombreRol}",
                                                "Bienvenido", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                txtEmail.Clear();
                                txtPassword.Clear();

                                this.Hide();

                                if (nombreRol.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
                                {
                                    FormAdmin dashboardAdmin = new FormAdmin(nombreTrabajador, nombreRol);
                                    dashboardAdmin.Show();
                                }
                                else
                                {
                                    FormTrabajador dashboardTrabajador = new FormTrabajador(nombreTrabajador, nombreRol);
                                    dashboardTrabajador.Show();
                                }
                            }
                            else
                            {
                                MessageBox.Show("El correo o la contraseña son incorrectos.", "Error de Autenticación", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ocurrió un error al intentar conectar: " + ex.Message, "Error de Conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                conexionBD.CerrarConexion();
            }
        }
    }

    // --- Clase Auxiliar para dibujar el corte diagonal azul/blanco ---
    public class DiagonalPanel : Panel
    {
        public DiagonalPanel()
        {
            this.DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 1. Color de fondo general de la ventana (Gris azulado claro tipo Muestra)
            Color fondoGeneral = Color.FromArgb(235, 240, 248);
            g.Clear(fondoGeneral);

            // 2. Definir las dimensiones de la tarjeta central
            int marginHorizontal = 30;
            int marginVertical = 35;
            Rectangle cardRect = new Rectangle(
                marginHorizontal,
                marginVertical,
                this.Width - (marginHorizontal * 2),
                this.Height - (marginVertical * 2)
            );

            // 3. Puntos de la diagonal extendida (Sobresale arriba y abajo del panel blanco)
            Point[] diagonalPoints = new Point[]
            {
            new Point((int)(this.Width * 0.52), 0),             // Arriba en el borde superior completo
            new Point(this.Width, 0),                           // Esquina superior derecha
            new Point(this.Width, this.Height),                // Esquina inferior derecha
            new Point((int)(this.Width * 0.36), this.Height)    // Abajo en el borde inferior completo
            };

            // 4. Dibujar Sombra Proyectada debajo de la Tarjeta Blanca
            for (int i = 1; i <= 10; i++)
            {
                int alpha = (int)(20 * (1.0 - (i / 10.0)));
                using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0)))
                {
                    Rectangle shadowRect = new Rectangle(
                        cardRect.X - i / 2,
                        cardRect.Y + i / 2,
                        cardRect.Width + i,
                        cardRect.Height + i
                    );
                    g.FillRectangle(shadowBrush, shadowRect);
                }
            }

            // 5. Dibujar el lado Izquierdo Blanco (Tarjeta central)
            using (SolidBrush whiteBrush = new SolidBrush(Color.White))
            {
                g.FillRectangle(whiteBrush, cardRect);
            }

            // 6. Dibujar el Polígono Azul Diagonal Completo
            Color azulMuestra = Color.FromArgb(24, 85, 165);
            using (SolidBrush blueBrush = new SolidBrush(azulMuestra))
            {
                g.FillPolygon(blueBrush, diagonalPoints);
            }

            // 7. Sombra de relieve en el borde inclinado para la profundidad
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddLine(
                    (int)(this.Width * 0.52), 0,
                    (int)(this.Width * 0.36), this.Height
                );

                // Sombra suave en el borde de la corteza azul
                using (Pen penSombra = new Pen(Color.FromArgb(60, 0, 0, 0), 6f))
                {
                    g.DrawPath(penSombra, path);
                }
                using (Pen penLuz = new Pen(Color.FromArgb(30, 255, 255, 255), 2f))
                {
                    g.DrawPath(penLuz, path);
                }
            }
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            this.Invalidate();
        }
    }
}