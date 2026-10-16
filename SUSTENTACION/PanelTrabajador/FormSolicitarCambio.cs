using Npgsql;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SUSTENTACION.PanelTrabajador
{
    public class FormSolicitarCambio : Form
    {
        private readonly int idTrabajador;
        private readonly string nombreTrabajador;

        private ComboBox cboTipo;
        private TextBox txtDescripcion;
        private Button btnEnviar;
        private Button btnCancelar;

        // Paleta
        private readonly Color fondo = Color.FromArgb(15, 15, 20);
        private readonly Color bgCard = Color.FromArgb(25, 25, 35);
        private readonly Color campo = Color.FromArgb(35, 35, 50);
        private readonly Color accentOrange = Color.FromArgb(255, 87, 34);
        private readonly Color textWhite = Color.White;
        private readonly Color textMuted = Color.FromArgb(150, 150, 160);

        public FormSolicitarCambio(int idTrabajador, string nombreTrabajador)
        {
            this.idTrabajador = idTrabajador;
            this.nombreTrabajador = nombreTrabajador;

            InicializarUI();
        }

        private void InicializarUI()
        {
            this.Text = "Solicitar cambio de datos";
            this.Size = new Size(560, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = fondo;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Título
            Label lblTitulo = new Label
            {
                Text = "📩  Solicitud al administrador",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(30, 25),
                AutoSize = true
            };
            this.Controls.Add(lblTitulo);

            Label lblSub = new Label
            {
                Text = $"Hola {nombreTrabajador}, cuéntanos qué dato deseas cambiar.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = textMuted,
                Location = new Point(32, 58),
                AutoSize = true
            };
            this.Controls.Add(lblSub);

            // Separador
            Panel linea = new Panel
            {
                Location = new Point(30, 88),
                Size = new Size(500, 1),
                BackColor = Color.FromArgb(45, 45, 60)
            };
            this.Controls.Add(linea);

            // Card principal
            Panel card = new Panel
            {
                Location = new Point(30, 105),
                Size = new Size(500, 280),
                BackColor = bgCard
            };
            AplicarBordesRedondeados(card, 12);
            this.Controls.Add(card);

            // Tipo de cambio
            card.Controls.Add(new Label
            {
                Text = "Tipo de cambio *",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(20, 20),
                AutoSize = true
            });

            cboTipo = new ComboBox
            {
                Location = new Point(20, 45),
                Size = new Size(460, 32),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = campo,
                ForeColor = textWhite,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f)
            };
            cboTipo.Items.AddRange(new object[]
            {
                "Contraseña",
                "Correo electrónico",
                "Celular / teléfono",
                "Nombre completo",
                "DNI",
                "Rol / cargo",
                "Otro"
            });
            cboTipo.SelectedIndex = 0;
            card.Controls.Add(cboTipo);

            // Descripción
            card.Controls.Add(new Label
            {
                Text = "Describe tu solicitud *",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(20, 95),
                AutoSize = true
            });

            Panel panelDesc = new Panel
            {
                Location = new Point(20, 120),
                Size = new Size(460, 130),
                BackColor = campo,
                Padding = new Padding(12, 10, 12, 10)
            };
            AplicarBordesRedondeados(panelDesc, 8);
            card.Controls.Add(panelDesc);

            txtDescripcion = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                BackColor = campo,
                ForeColor = textWhite,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10f),
                ScrollBars = ScrollBars.Vertical,
                MaxLength = 500
            };
            panelDesc.Controls.Add(txtDescripcion);

            // Nota informativa
            Label lblNota = new Label
            {
                Text = "ℹ️  El admin revisará tu solicitud y te responderá en notificaciones.",
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = textMuted,
                Location = new Point(30, 400),
                AutoSize = true
            };
            this.Controls.Add(lblNota);

            // Botón Cancelar
            btnCancelar = new Button
            {
                Text = "Cancelar",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = textWhite,
                BackColor = Color.FromArgb(50, 50, 70),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 42),
                Location = new Point(250, 415),
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            AplicarBordesRedondeados(btnCancelar, 8);
            btnCancelar.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancelar);

            // Botón Enviar
            btnEnviar = new Button
            {
                Text = "📩  Enviar solicitud",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = textWhite,
                BackColor = accentOrange,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(180, 42),
                Location = new Point(380, 415),
                Cursor = Cursors.Hand
            };
            btnEnviar.FlatAppearance.BorderSize = 0;
            btnEnviar.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 120, 80);
            AplicarBordesRedondeados(btnEnviar, 8);
            btnEnviar.Click += BtnEnviar_Click;
            this.Controls.Add(btnEnviar);
        }

        private void BtnEnviar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDescripcion.Text))
            {
                MessageBox.Show("Por favor escribe una descripción.",
                    "Falta información", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDescripcion.Focus();
                return;
            }

            btnEnviar.Enabled = false;
            btnEnviar.Text = "Enviando...";

            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();

                string sql = @"
                    INSERT INTO solicitudes_cambio 
                        (id_trabajador, tipo_cambio, descripcion, estado, fecha_solicitud)
                    VALUES 
                        (@idTrab, @tipo, @desc, 'Pendiente', NOW())";

                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@idTrab", idTrabajador);
                    cmd.Parameters.AddWithValue("@tipo", cboTipo.SelectedItem.ToString());
                    cmd.Parameters.AddWithValue("@desc", txtDescripcion.Text.Trim());

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show(
                    "✅ Solicitud enviada correctamente.\n\n" +
                    "El administrador será notificado y podrás ver la respuesta en 'Mis solicitudes'.",
                    "Solicitud enviada",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al enviar:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnEnviar.Enabled = true;
                btnEnviar.Text = "📩  Enviar solicitud";
            }
            finally
            {
                conexionDB.CerrarConexion();
            }
        }

        private void AplicarBordesRedondeados(Control control, int radio)
        {
            Action recalc = () =>
            {
                if (control.Width <= 0 || control.Height <= 0) return;
                using (var path = new GraphicsPath())
                {
                    path.AddArc(0, 0, radio, radio, 180, 90);
                    path.AddArc(control.Width - radio, 0, radio, radio, 270, 90);
                    path.AddArc(control.Width - radio, control.Height - radio, radio, radio, 0, 90);
                    path.AddArc(0, control.Height - radio, radio, radio, 90, 90);
                    path.CloseAllFigures();
                    control.Region = new Region(path);
                }
            };
            control.Resize += (s, e) => recalc();
            recalc();
        }
    }
}