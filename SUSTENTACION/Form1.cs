using MySql.Data.MySqlClient;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SUSTENTACION
{
    public partial class Form1 : Form
    {
        // Declaración de controles
        private Panel panelIzquierdo;
        private Panel panelDerecho;
        private Label lblWelcome;
        private Label lblLoginTitle;
        private Label lblEmailPlaceholder;
        private TextBox txtEmail;
        private Panel lineEmail;
        private Label lblPasswordPlaceholder;
        private TextBox txtPassword;
        private Panel linePassword;
        private LinkLabel lblForgot;
        private Button btnLogin;
        private Label lblSignUpPrefix;
        private LinkLabel lblSignUpLink;

        public Form1()
        {
            InitializeComponent();
            ConstruirInterfaz();
        }

        private void ConstruirInterfaz()
        {
            // --- Configuración del Formulario ---
            this.Size = new Size(850, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Text = "Login - Artemusa Inventario";
            this.BackColor = Color.FromArgb(240, 240, 240);

            // --- Contenedor Principal (Panel Derecho / Formulario) ---
            panelDerecho = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            this.Controls.Add(panelDerecho);

            // --- Panel Izquierdo (Imagen / Bienvenida) ---
            panelIzquierdo = new Panel
            {
                Width = 380,
                Dock = DockStyle.Left,
                BackColor = Color.FromArgb(15, 32, 67) // Color azul oscuro de respaldo
            };
            this.Controls.Add(panelIzquierdo);

            // Texto "WELCOME"
            lblWelcome = new Label
            {
                Text = "WELCOME",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(110, 140),
                BackColor = Color.Transparent
            };
            panelIzquierdo.Controls.Add(lblWelcome);

            // --- Elementos del Formulario (Panel Derecho) ---

            // Título "Login"
            lblLoginTitle = new Label
            {
                Text = "Login",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                AutoSize = true,
                Location = new Point(180, 80)
            };
            panelDerecho.Controls.Add(lblLoginTitle);

            // Campo Email
            lblEmailPlaceholder = new Label
            {
                Text = "Email",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.Gray,
                Location = new Point(80, 150),
                AutoSize = true
            };
            panelDerecho.Controls.Add(lblEmailPlaceholder);

            txtEmail = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10f),
                Location = new Point(80, 175),
                Size = new Size(300, 20),
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            panelDerecho.Controls.Add(txtEmail);

            lineEmail = new Panel
            {
                Size = new Size(300, 1),
                Location = new Point(80, 198),
                BackColor = Color.Silver
            };
            panelDerecho.Controls.Add(lineEmail);

            // Campo Password
            lblPasswordPlaceholder = new Label
            {
                Text = "Password",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.Gray,
                Location = new Point(80, 220),
                AutoSize = true
            };
            panelDerecho.Controls.Add(lblPasswordPlaceholder);

            txtPassword = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10f),
                Location = new Point(80, 245),
                Size = new Size(300, 20),
                UseSystemPasswordChar = true,
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            panelDerecho.Controls.Add(txtPassword);

            linePassword = new Panel
            {
                Size = new Size(300, 1),
                Location = new Point(80, 268),
                BackColor = Color.Silver
            };
            panelDerecho.Controls.Add(linePassword);

            // Link "¿Forgot password?"
            lblForgot = new LinkLabel
            {
                Text = "Forgot password?",
                Font = new Font("Segoe UI", 8f),
                LinkColor = Color.Gray,
                ActiveLinkColor = Color.FromArgb(10, 25, 70),
                VisitedLinkColor = Color.Gray,
                AutoSize = true,
                Location = new Point(285, 275)
            };
            panelDerecho.Controls.Add(lblForgot);

            // Botón "Login" con bordes redondeados
            btnLogin = new Button
            {
                Text = "Login",
                Size = new Size(300, 42),
                Location = new Point(80, 320),
                BackColor = Color.FromArgb(10, 25, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            AplicarBordesRedondeados(btnLogin, 20);

            // Evento Clic del Botón Login
            btnLogin.Click += BtnLogin_Click;
            panelDerecho.Controls.Add(btnLogin);

            // Texto inferior "Don't have an account? Sign up"
            lblSignUpPrefix = new Label
            {
                Text = "Don't have an account?",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(125, 430)
            };
            panelDerecho.Controls.Add(lblSignUpPrefix);

            lblSignUpLink = new LinkLabel
            {
                Text = "Sign up",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                LinkColor = Color.FromArgb(10, 25, 70),
                ActiveLinkColor = Color.Blue,
                VisitedLinkColor = Color.FromArgb(10, 25, 70),
                AutoSize = true,
                Location = new Point(275, 430)
            };
            panelDerecho.Controls.Add(lblSignUpLink);
        }

        // --- Lógica para conectar y validar en la Base de Datos mostrando el Rol ---
        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text.Trim();

            // Validar que los campos no estén vacíos
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
                    // Consulta con JOIN entre la tabla 'trabajadores' y 'roles'
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
                            if (reader.Read()) // Si encuentra coincidencia
                            {
                                string nombreTrabajador = reader["nombre"].ToString();
                                string nombreRol = reader["nombre_rol"].ToString();

                                MessageBox.Show($"¡Bienvenido {nombreTrabajador}!\nSe inició sesión con éxito como: {nombreRol}",
                                                "Bienvenido", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                // Limpiar los campos después del login
                                txtEmail.Clear();
                                txtPassword.Clear();
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

        // Método auxiliar para redondear las esquinas del botón
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