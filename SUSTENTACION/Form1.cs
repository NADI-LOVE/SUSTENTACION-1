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
        private PictureBox picLogo;
        private Label lblAppTitle;
        private Label lblAppSubtitle;

        // Controles del lado derecho (Azul - Formulario)
        private Label lblHeaderTitle;
        private Label lblHeaderSubtitle;
        private Label lblEmail;
        private TextBox txtEmail;
        private Panel pnlEmailBorder; // Borde interactivo para Email
        private Label lblPassword;
        private TextBox txtPassword;
        private Panel pnlPasswordBorder; // Borde interactivo para Password
        private Button btnTogglePassword; // Botón para mostrar/ocultar contraseña
        private CheckBox chkRememberMe;
        private LinkLabel lblRecoverPassword;
        private Button btnLogin;

        public Form1()
        {
            InitializeComponent();
            ConstruirInterfaz();
            CargarCredencialesGuardadas();
        }

        private void ConstruirInterfaz()
        {
            // --- Configuración básica de la ventana ---
            this.Size = new Size(900, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Text = "Iniciar Sesión - Artemusa Inventario";

            panelBackground = new DiagonalPanel
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(panelBackground);

            // ==========================================
            // LADO IZQUIERDO: Marca / Logotipo (Fondo Blanco)
            // ==========================================

            // ==========================================
            // LADO IZQUIERDO: Marca / Logotipo (Fondo Blanco)
            // ==========================================

            picLogo = new PictureBox
            {
                Size = new Size(130, 130),
                Location = new Point(125, 90), // Subido ligeramente para liberar espacio vertical
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Image = SUSTENTACION.Properties.Resources.logo
            };
            panelBackground.Controls.Add(picLogo);

            lblAppTitle = new Label
            {
                Text = "ARTEMUSA",
                Font = new Font("Segoe UI", 22f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 35, 75),
                AutoSize = true,
                Location = new Point(90, 235), // Posición vertical ajustada
                BackColor = Color.Transparent
            };
            panelBackground.Controls.Add(lblAppTitle);

            lblAppSubtitle = new Label
            {
                Text = "Sistema de Gestión e Inventario",
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(110, 125, 150),
                AutoSize = true,
                Location = new Point(78, 280), // Bajado a Y = 280 para separarlo de ARTEMUSA
                BackColor = Color.Transparent
            };
            panelBackground.Controls.Add(lblAppSubtitle);

            // ==========================================
            // LADO DERECHO: Formulario de Login (Fondo Azul)
            // ==========================================

            int startX = 495;

            // --- Título del Formulario ---
            lblHeaderTitle = new Label
            {
                Text = "Iniciar Sesión",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(startX, 60),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            panelBackground.Controls.Add(lblHeaderTitle);

            // --- Subtítulo del Formulario ---
            lblHeaderSubtitle = new Label
            {
                Text = "Ingresa tus credenciales para acceder",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(195, 220, 255),
                Location = new Point(startX, 102),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            panelBackground.Controls.Add(lblHeaderSubtitle);

            // --- Label y Campo: Correo Electrónico ---
            lblEmail = new Label
            {
                Text = "Correo Electrónico",
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(230, 240, 255),
                Location = new Point(startX, 145),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            panelBackground.Controls.Add(lblEmail);

            pnlEmailBorder = new Panel
            {
                Location = new Point(startX, 170),
                Size = new Size(310, 36),
                BackColor = Color.White,
                Padding = new Padding(3)
            };

            txtEmail = new TextBox
            {
                Font = new Font("Segoe UI", 10.5f),
                Size = new Size(300, 28),
                Location = new Point(5, 5),
                ForeColor = Color.Gray,
                Text = "ejemplo@artemusa.com",
                BorderStyle = BorderStyle.None
            };

            txtEmail.GotFocus += (s, e) =>
            {
                if (txtEmail.Text == "ejemplo@artemusa.com")
                {
                    txtEmail.Text = "";
                    txtEmail.ForeColor = Color.FromArgb(30, 30, 30);
                }
                pnlEmailBorder.BackColor = Color.FromArgb(80, 160, 255);
            };

            txtEmail.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtEmail.Text))
                {
                    txtEmail.Text = "ejemplo@artemusa.com";
                    txtEmail.ForeColor = Color.Gray;
                }
                pnlEmailBorder.BackColor = Color.White;
            };

            pnlEmailBorder.Controls.Add(txtEmail);
            panelBackground.Controls.Add(pnlEmailBorder);

            // --- Label y Campo: Contraseña ---
            lblPassword = new Label
            {
                Text = "Contraseña",
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(230, 240, 255),
                Location = new Point(startX, 220),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            panelBackground.Controls.Add(lblPassword);

            pnlPasswordBorder = new Panel
            {
                Location = new Point(startX, 245),
                Size = new Size(310, 36),
                BackColor = Color.White,
                Padding = new Padding(3)
            };

            txtPassword = new TextBox
            {
                Font = new Font("Segoe UI", 10.5f),
                Size = new Size(260, 28),
                Location = new Point(5, 5),
                ForeColor = Color.Gray,
                Text = "••••••••",
                UseSystemPasswordChar = false,
                BorderStyle = BorderStyle.None
            };

            btnTogglePassword = new Button
            {
                Text = "👁",
                Size = new Size(36, 28),
                Location = new Point(268, 3),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 10f)
            };
            btnTogglePassword.FlatAppearance.BorderSize = 0;
            btnTogglePassword.Click += (s, e) =>
            {
                if (txtPassword.Text != "••••••••")
                {
                    txtPassword.UseSystemPasswordChar = !txtPassword.UseSystemPasswordChar;
                    btnTogglePassword.Text = txtPassword.UseSystemPasswordChar ? "👁" : "🙈";
                }
            };

            txtPassword.GotFocus += (s, e) =>
            {
                if (txtPassword.Text == "••••••••")
                {
                    txtPassword.Text = "";
                    txtPassword.ForeColor = Color.FromArgb(30, 30, 30);
                    txtPassword.UseSystemPasswordChar = true;
                }
                pnlPasswordBorder.BackColor = Color.FromArgb(80, 160, 255);
            };

            txtPassword.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtPassword.Text))
                {
                    txtPassword.UseSystemPasswordChar = false;
                    txtPassword.Text = "••••••••";
                    txtPassword.ForeColor = Color.Gray;
                }
                pnlPasswordBorder.BackColor = Color.White;
            };

            pnlPasswordBorder.Controls.Add(txtPassword);
            pnlPasswordBorder.Controls.Add(btnTogglePassword);
            panelBackground.Controls.Add(pnlPasswordBorder);

            // --- Checkbox Recordarme ---
            chkRememberMe = new CheckBox
            {
                Text = "Recordarme",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(230, 240, 255),
                Location = new Point(startX, 295),
                AutoSize = true,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            panelBackground.Controls.Add(chkRememberMe);

            // --- Enlace Recuperar Contraseña ---
            lblRecoverPassword = new LinkLabel
            {
                Text = "¿Olvidaste tu contraseña?",
                Font = new Font("Segoe UI", 9f),
                LinkColor = Color.FromArgb(195, 220, 255),
                ActiveLinkColor = Color.White,
                VisitedLinkColor = Color.FromArgb(195, 220, 255),
                AutoSize = true,
                Location = new Point(startX + 145),
                BackColor = Color.Transparent
            };
            lblRecoverPassword.Location = new Point(startX + 145, 296);
            lblRecoverPassword.LinkClicked += LblRecoverPassword_LinkClicked;
            panelBackground.Controls.Add(lblRecoverPassword);

            // --- Botón Iniciar Sesión ---
            btnLogin = new Button
            {
                Text = "INICIAR SESIÓN",
                Size = new Size(310, 44),
                Location = new Point(startX, 342),
                BackColor = Color.FromArgb(35, 115, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;

            btnLogin.MouseEnter += (s, e) => btnLogin.BackColor = Color.FromArgb(25, 98, 210);
            btnLogin.MouseLeave += (s, e) => btnLogin.BackColor = Color.FromArgb(35, 115, 235);

            btnLogin.Click += BtnLogin_Click;
            panelBackground.Controls.Add(btnLogin);

            this.AcceptButton = btnLogin;
        }

        private void CargarCredencialesGuardadas()
        {
            string correoGuardado = SUSTENTACION.Properties.Settings.Default.SavedEmail;
            bool recordar = SUSTENTACION.Properties.Settings.Default.RememberMe;

            if (recordar && !string.IsNullOrEmpty(correoGuardado))
            {
                txtEmail.Text = correoGuardado;
                txtEmail.ForeColor = Color.FromArgb(30, 30, 30);
                chkRememberMe.Checked = true;
            }
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(email) || email == "ejemplo@artemusa.com" ||
                string.IsNullOrEmpty(password) || password == "••••••••")
            {
                MessageBox.Show("Por favor ingresa tu correo y contraseña.", "Campos requeridos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnLogin.Enabled = false;
            btnLogin.Text = "VERIFICANDO...";
            btnLogin.BackColor = Color.FromArgb(100, 130, 170);

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

                                if (chkRememberMe.Checked)
                                {
                                    SUSTENTACION.Properties.Settings.Default.SavedEmail = email;
                                    SUSTENTACION.Properties.Settings.Default.RememberMe = true;
                                }
                                else
                                {
                                    SUSTENTACION.Properties.Settings.Default.SavedEmail = "";
                                    SUSTENTACION.Properties.Settings.Default.RememberMe = false;
                                }
                                SUSTENTACION.Properties.Settings.Default.Save();

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
                btnLogin.Enabled = true;
                btnLogin.Text = "INICIAR SESIÓN";
                btnLogin.BackColor = Color.FromArgb(35, 115, 235);
            }
        }

        private void LblRecoverPassword_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show("Módulo en desarrollo.\nPor favor, contacta al administrador del sistema para restablecer tu acceso.",
                            "Recuperar Contraseña", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

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

            // 1. Fondo general claro
            Color fondoGeneral = Color.FromArgb(232, 238, 245);
            g.Clear(fondoGeneral);

            // 2. Dimensiones de la tarjeta blanca central
            int marginHorizontal = 25;
            int marginVertical = 30;
            Rectangle cardRect = new Rectangle(
                marginHorizontal,
                marginVertical,
                this.Width - (marginHorizontal * 2),
                this.Height - (marginVertical * 2)
            );

            // 3. Sombra de la tarjeta
            for (int i = 1; i <= 8; i++)
            {
                int alpha = (int)(18 * (1.0 - (i / 8.0)));
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

            // 4. Base blanca de la tarjeta central
            using (SolidBrush whiteBrush = new SolidBrush(Color.White))
            {
                g.FillRectangle(whiteBrush, cardRect);
            }

            // 5. Polígono azul que abarca HASTA LOS BORDES de la ventana (0 y Height)
            Point[] diagonalPoints = new Point[]
            {
            new Point((int)(this.Width * 0.52), 0),
            new Point(this.Width, 0),
            new Point(this.Width, this.Height),
            new Point((int)(this.Width * 0.36), this.Height)
            };

            Color azulMuestra = Color.FromArgb(22, 70, 145);
            using (SolidBrush blueBrush = new SolidBrush(azulMuestra))
            {
                g.FillPolygon(blueBrush, diagonalPoints);
            }

            // 6. Línea de relieve y sombra en el corte diagonal
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddLine(
                    (int)(this.Width * 0.52), 0,
                    (int)(this.Width * 0.36), this.Height
                );

                using (Pen penSombra = new Pen(Color.FromArgb(60, 0, 0, 0), 5f))
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