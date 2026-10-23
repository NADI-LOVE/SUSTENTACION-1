using Npgsql;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SUSTENTACION
{
    public partial class Form1 : Form
    {
        private readonly Color azulOscuro = Color.FromArgb(17, 35, 70);
        private readonly Color azulPrincipal = Color.FromArgb(38, 97, 211);
        private readonly Color grisTexto = Color.FromArgb(74, 87, 106);
        private readonly Color grisBorde = Color.FromArgb(217, 225, 235);

        private DiagonalLoginPanel fondoDiagonal;
        private TextBox txtEmail;
        private TextBox txtPassword;
        private RoundedPanel pnlEmail;
        private RoundedPanel pnlPassword;
        private Button btnTogglePassword;
        private Button btnLogin;
        private CheckBox chkRememberMe;
        private LinkLabel lblRecoverPassword;
        private Label lblError;
        private bool mostrandoPassword = false;

        public Form1()
        {
            ConstruirInterfaz();

            string rutaIcono = System.IO.Path.Combine(
                AppContext.BaseDirectory,
                "logo_artemusa.ico"
            );

            if (System.IO.File.Exists(rutaIcono))
            {
                this.Icon = new Icon(rutaIcono);
            }

            this.ShowIcon = true;

            CargarCredencialesGuardadas();
        }

        private void ConstruirInterfaz()
        {
            SuspendLayout();
            // Los controles visuales se crean en código; el diseñador queda intacto.
            ClientSize = new Size(900, 560);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "ARTEMUSA | Iniciar sesión";
            BackColor = Color.FromArgb(233, 238, 245);
            Font = new Font("Segoe UI", 10f);
            AutoScaleMode = AutoScaleMode.None; // Mantiene las proporciones del diseño fijo.

            fondoDiagonal = new DiagonalLoginPanel { Dock = DockStyle.Fill };
            Controls.Add(fondoDiagonal);
            fondoDiagonal.BringToFront();

            // Columna izquierda, sobre la tarjeta blanca del fondo.
            var logo = new PictureBox
            {
                Location = new Point(119, 92),
                Size = new Size(145, 145),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Image = Properties.Resources.logo
            };
            fondoDiagonal.Controls.Add(logo);
            AddLabel(fondoDiagonal, "ARTEMUSA", new Point(53, 238), new Size(315, 58),
                24f, FontStyle.Bold, azulOscuro, ContentAlignment.MiddleCenter);
            AddLabel(fondoDiagonal, "CONTROL DE EQUIPOS AUDIOVISUALES",
                new Point(45, 299), new Size(325, 28), 9.2f, FontStyle.Bold,
                Color.FromArgb(36, 93, 161), ContentAlignment.MiddleCenter);
            AddLabel(fondoDiagonal, "Registro, inventario y control de los\nequipos audiovisuales de la empresa.",
                new Point(65, 355), new Size(265, 64), 9.4f, FontStyle.Regular,
                grisTexto, ContentAlignment.MiddleCenter);
            AddLabel(fondoDiagonal, "ARTEMUSA PERÚ S.R.L.", new Point(78, 468),
                new Size(230, 25), 8.7f, FontStyle.Regular,
                grisTexto, ContentAlignment.MiddleCenter);

            // Formulario directamente sobre el fondo azul (sin tarjeta blanca adicional).
            const int x = 510;
            const int fieldWidth = 318;
            AddLabel(fondoDiagonal, "Iniciar sesión", new Point(x, 77),
                new Size(fieldWidth + 12, 48), 22f, FontStyle.Bold, Color.White);
            AddLabel(fondoDiagonal, "Ingresa tus credenciales para acceder",
                new Point(x, 120), new Size(fieldWidth + 18, 27), 9.5f,
                FontStyle.Regular, Color.FromArgb(219, 232, 251));

            AddLabel(fondoDiagonal, "Correo electrónico", new Point(x, 169),
                new Size(fieldWidth, 24), 9.4f, FontStyle.Bold, Color.White);
            pnlEmail = CrearCampo(new Point(x, 197), fieldWidth);
            fondoDiagonal.Controls.Add(pnlEmail);
            txtEmail = new TextBox
            {
                Location = new Point(14, 11),
                Width = fieldWidth - 30,
                Font = new Font("Segoe UI", 10.5f),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = azulOscuro,
                MaxLength = 254
            };
            pnlEmail.Controls.Add(txtEmail);
            ConfigurarFoco(txtEmail, pnlEmail);

            AddLabel(fondoDiagonal, "Contraseña", new Point(x, 263),
                new Size(fieldWidth, 24), 9.4f, FontStyle.Bold, Color.White);
            pnlPassword = CrearCampo(new Point(x, 291), fieldWidth);
            fondoDiagonal.Controls.Add(pnlPassword);
            txtPassword = new TextBox
            {
                Location = new Point(14, 11),
                Width = fieldWidth - 111,
                Font = new Font("Segoe UI", 10.5f),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = azulOscuro,
                UseSystemPasswordChar = true,
                MaxLength = 128
            };
            pnlPassword.Controls.Add(txtPassword);
            ConfigurarFoco(txtPassword, pnlPassword);

            btnTogglePassword = new Button
            {
                Text = "Mostrar",
                Location = new Point(fieldWidth - 88, 5),
                Size = new Size(80, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = azulPrincipal,
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TabStop = false,
                AccessibleName = "Mostrar u ocultar contraseña"
            };
            btnTogglePassword.FlatAppearance.BorderSize = 0;
            btnTogglePassword.Click += (s, e) =>
            {
                mostrandoPassword = !mostrandoPassword;
                txtPassword.UseSystemPasswordChar = !mostrandoPassword;
                btnTogglePassword.Text = mostrandoPassword ? "Ocultar" : "Mostrar";
                txtPassword.Focus();
                txtPassword.SelectionStart = txtPassword.TextLength;
            };
            pnlPassword.Controls.Add(btnTogglePassword);

            chkRememberMe = new CheckBox
            {
                Text = "Recordar correo",
                Location = new Point(x, 346),
                Size = new Size(143, 26),
                ForeColor = Color.FromArgb(231, 239, 253),
                Font = new Font("Segoe UI", 8.5f),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            fondoDiagonal.Controls.Add(chkRememberMe);

            lblRecoverPassword = new LinkLabel
            {
                Text = "¿Olvidaste tu contraseña?",
                Location = new Point(x + 142, 348),
                Size = new Size(176, 24),
                TextAlign = ContentAlignment.MiddleRight,
                LinkColor = Color.FromArgb(225, 238, 255),
                ActiveLinkColor = Color.White,
                VisitedLinkColor = Color.FromArgb(225, 238, 255),
                Font = new Font("Segoe UI", 8.5f),
                BackColor = Color.Transparent
            };
            lblRecoverPassword.LinkClicked += LblRecoverPassword_LinkClicked;
            fondoDiagonal.Controls.Add(lblRecoverPassword);

            lblError = AddLabel(fondoDiagonal, "", new Point(x, 377),
                new Size(fieldWidth, 24), 8.8f, FontStyle.Regular,
                Color.FromArgb(255, 224, 225));

            btnLogin = new Button
            {
                Text = "INICIAR SESIÓN",
                Location = new Point(x, 410),
                Size = new Size(fieldWidth, 45),
                BackColor = Color.FromArgb(36, 123, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.MouseEnter += (s, e) =>
            {
                if (btnLogin.Enabled) btnLogin.BackColor = Color.FromArgb(25, 105, 215);
            };
            btnLogin.MouseLeave += (s, e) =>
            {
                if (btnLogin.Enabled) btnLogin.BackColor = Color.FromArgb(36, 123, 235);
            };
            btnLogin.Click += async (s, e) => await IniciarSesionAsync();
            fondoDiagonal.Controls.Add(btnLogin);
            AddLabel(fondoDiagonal, "Acceso exclusivo para personal autorizado",
                new Point(x, 466), new Size(fieldWidth, 25), 8.7f,
                FontStyle.Regular, Color.FromArgb(195, 216, 245),
                ContentAlignment.MiddleCenter);

            AcceptButton = btnLogin;
            ResumeLayout(false);
        }

        private RoundedPanel CrearCampo(Point posicion, int ancho)
        {
            return new RoundedPanel
            {
                Location = posicion,
                Size = new Size(ancho, 45),
                BackColor = Color.White,
                BorderColor = grisBorde,
                BorderRadius = 11
            };
        }

        private void ConfigurarFoco(TextBox campo, RoundedPanel marco)
        {
            campo.Enter += (s, e) => { marco.BorderColor = Color.FromArgb(74, 177, 234); marco.Invalidate(); if (lblError != null) lblError.Text = ""; };
            campo.Leave += (s, e) => { marco.BorderColor = grisBorde; marco.Invalidate(); };
        }

        private static Label AddLabel(Control parent, string texto, Point posicion, Size tamano,
            float fuente, FontStyle estilo, Color color,
            ContentAlignment alineacion = ContentAlignment.MiddleLeft)
        {
            var etiqueta = new Label
            {
                Text = texto,
                Location = posicion,
                Size = tamano,
                Font = new Font("Segoe UI", fuente, estilo),
                ForeColor = color,
                BackColor = Color.Transparent,
                TextAlign = alineacion
            };
            parent.Controls.Add(etiqueta);
            return etiqueta;
        }

        private void CargarCredencialesGuardadas()
        {
            bool recordar = Properties.Settings.Default.RememberMe;
            string correo = Properties.Settings.Default.SavedEmail;
            if (recordar && !string.IsNullOrWhiteSpace(correo))
            {
                txtEmail.Text = correo;
                chkRememberMe.Checked = true;
            }
        }

        private void GuardarPreferencias()
        {
            // Solo recuerda el correo, nunca la contraseña.
            Properties.Settings.Default.RememberMe = chkRememberMe.Checked;
            Properties.Settings.Default.SavedEmail = chkRememberMe.Checked ? txtEmail.Text.Trim() : "";
            Properties.Settings.Default.Save();
        }

        private async Task IniciarSesionAsync()
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text; // No eliminar espacios de una contraseña válida.
            lblError.Text = "";

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            {
                lblError.Text = "Completa tu correo y contraseña.";
                return;
            }
            try { var validacion = new MailAddress(email); if (!string.Equals(validacion.Address, email, StringComparison.OrdinalIgnoreCase)) throw new FormatException(); }
            catch (FormatException) { lblError.Text = "Ingresa un correo electrónico válido."; return; }

            btnLogin.Enabled = false;
            btnLogin.Text = "VERIFICANDO ACCESO...";
            btnLogin.BackColor = Color.FromArgb(117, 152, 211);

            try
            {
                // Mantiene la consulta de la base actual. Se recomienda migrar contraseñas a hashes seguros.
                using (NpgsqlConnection conn = new Conexion().ObtenerConexion())
                using (var cmd = new NpgsqlCommand(@"
                    SELECT t.id_trabajador, t.nombre, r.nombre_rol
                    FROM trabajadores t
                    INNER JOIN roles r ON t.id_rol = r.id_rol
                    WHERE t.correo = @correo AND t.password = @password", conn))
                {
                    cmd.Parameters.AddWithValue("@correo", email);
                    cmd.Parameters.AddWithValue("@password", password);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync())
                        {
                            lblError.Text = "Correo o contraseña incorrectos.";
                            return;
                        }

                        int idTrabajador = Convert.ToInt32(reader["id_trabajador"]);
                        string nombreTrabajador = reader["nombre"].ToString();
                        string nombreRol = reader["nombre_rol"].ToString();
                        GuardarPreferencias();

                        Form dashboard;
                        if (nombreRol.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
                            dashboard = new FormAdmin(nombreTrabajador, nombreRol);
                        else
                            dashboard = new FormTrabajador(idTrabajador, nombreTrabajador, nombreRol);

                        // 👇 NUEVO bloque (reemplaza a "dashboard.FormClosed += (s, e) => this.Close();")
                        dashboard.FormClosed += (s, e) =>
                        {
                            bool fueLogout = false;

                            if (dashboard is FormAdmin admin)
                                fueLogout = admin.SesionCerrada;
                            else if (dashboard is FormTrabajador trabajador)
                                fueLogout = trabajador.SesionCerrada;

                            if (fueLogout)
                            {
                                txtPassword.Clear();
                                if (!chkRememberMe.Checked)
                                    txtEmail.Clear();
                                this.Show();
                                this.BringToFront();
                                txtEmail.Focus();
                            }
                            else
                            {
                                this.Close();
                            }
                        };

                        txtPassword.Clear();
                        this.Hide();
                        dashboard.Show();
                    }
                }
            }
            catch (Exception ex)
            {
                // No mostrar detalles de la BD al usuario final.
                System.Diagnostics.Debug.WriteLine(ex);
                lblError.Text = "No se pudo iniciar sesión. Verifica la conexión.";
            }
            finally
            {
                btnLogin.Enabled = true;
                btnLogin.Text = "INICIAR SESIÓN";
                btnLogin.BackColor = Color.FromArgb(36, 123, 235);
            }
        }

        private void LblRecoverPassword_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show("Comunícate con el administrador para recuperar tu acceso.",
                "Recuperación de acceso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    // Contorno redondeado REAL: recorta también la superficie del Panel.
    // Esto evita que aparezca un rectángulo blanco detrás de las esquinas.
    internal class RoundedPanel : Panel
    {
        private Color _borderColor = Color.FromArgb(217, 225, 235);
        private int _borderRadius = 11;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color BorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value; Invalidate(); }
        }

        [System.ComponentModel.DefaultValue(11)]
        public int BorderRadius
        {
            get { return _borderRadius; }
            set { _borderRadius = Math.Max(1, value); ActualizarRegion(); Invalidate(); }
        }

        public RoundedPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            ActualizarRegion();
        }

        private void ActualizarRegion()
        {
            if (Width < 4 || Height < 4) return;
            using (var ruta = CrearRedondeado(new Rectangle(0, 0, Width, Height), _borderRadius))
            {
                Region vieja = Region;
                Region = new Region(ruta);
                if (vieja != null) vieja.Dispose();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var ruta = CrearRedondeado(new Rectangle(1, 1, Width - 3, Height - 3), _borderRadius))
            using (var borde = new Pen(_borderColor, 1.5f))
            {
                e.Graphics.DrawPath(borde, ruta);
            }
        }

        private static GraphicsPath CrearRedondeado(Rectangle r, int radius)
        {
            int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            var p = new GraphicsPath();
            p.AddArc(r.Left, r.Top, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    // Panel decorativo: tarjeta blanca con sombra, franja gris y corte diagonal azul.
    internal class DiagonalLoginPanel : Panel
    {
        public DiagonalLoginPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(233, 238, 245);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.FromArgb(232, 238, 246));

            // Tarjeta blanca del diseño original, ligeramente levantada del fondo.
            Rectangle tarjeta = new Rectangle(25, 30, Width - 50, Height - 60);
            using (var sombra = new SolidBrush(Color.FromArgb(28, 30, 48, 73)))
                g.FillRectangle(sombra, tarjeta.X + 5, tarjeta.Y + 6, tarjeta.Width, tarjeta.Height);
            using (var blanco = new SolidBrush(Color.White))
                g.FillRectangle(blanco, tarjeta);

            // Corte inclinado como en la referencia: azul alcanza los bordes,
            // con una estrecha línea gris luminosa en la unión.
            Point arriba = new Point((int)(Width * 0.525), 0);
            Point abajo = new Point((int)(Width * 0.365), Height);
            Point[] poligonoAzul = {
                arriba, new Point(Width, 0), new Point(Width, Height), abajo
            };
            using (var azul = new SolidBrush(Color.FromArgb(28, 75, 148)))
                g.FillPolygon(azul, poligonoAzul);
            using (var bordeOscuro = new Pen(Color.FromArgb(80, 16, 42, 84), 5f))
                g.DrawLine(bordeOscuro, arriba, abajo);
            using (var bordeClaro = new Pen(Color.FromArgb(205, 229, 237, 248), 2f))
                g.DrawLine(bordeClaro, new Point(arriba.X - 3, arriba.Y),
                    new Point(abajo.X - 3, abajo.Y));
        }
    }
}
