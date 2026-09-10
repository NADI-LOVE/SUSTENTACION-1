using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using SUSTENTACION.PanelAdministrador;

namespace SUSTENTACION
{
    public class FormEnviarMensaje : Form
    {
        private TextBox txtMensaje;
        private Button btnEnviar;
        private Label lblEstado;

        // Paleta Dark / Neon
        private readonly Color bgDark = Color.FromArgb(18, 18, 18);
        private readonly Color bgCard = Color.FromArgb(32, 32, 32);
        private readonly Color accentGreen = Color.FromArgb(163, 230, 53);
        private readonly Color textWhite = Color.FromArgb(240, 240, 240);

        public FormEnviarMensaje()
        {
            ConstruirUI();
        }

        private void ConstruirUI()
        {
            this.Size = new Size(500, 380);
            this.Text = "Notificaciones WhatsApp - Artemusa";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = bgDark;

            // Título
            Label lblTitulo = new Label
            {
                Text = "💬 Redactar Notificación de WhatsApp",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = accentGreen,
                Location = new Point(25, 20),
                AutoSize = true
            };
            this.Controls.Add(lblTitulo);

            // Subtítulo
            Label lblSub = new Label
            {
                Text = "El mensaje será enviado al número registrado (51961256566):",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.LightGray,
                Location = new Point(25, 50),
                AutoSize = true
            };
            this.Controls.Add(lblSub);

            // Caja de texto del mensaje
            txtMensaje = new TextBox
            {
                Multiline = true,
                Size = new Size(430, 150),
                Location = new Point(25, 80),
                BackColor = bgCard,
                ForeColor = textWhite,
                Font = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.FixedSingle,
                Text = "Hola, te informamos que hay una actualización en el estado de tu inventario en Artemusa."
            };
            this.Controls.Add(txtMensaje);

            // Estado de envío
            lblEstado = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = accentGreen,
                Location = new Point(25, 240),
                AutoSize = true
            };
            this.Controls.Add(lblEstado);

            // Botón Enviar
            btnEnviar = new Button
            {
                Text = "Enviar Mensaje",
                Size = new Size(160, 40),
                Location = new Point(295, 270),
                BackColor = accentGreen,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnEnviar.FlatAppearance.BorderSize = 0;
            btnEnviar.Click += BtnEnviar_Click;
            this.Controls.Add(btnEnviar);
        }

        private async void BtnEnviar_Click(object sender, EventArgs e)
        {
            string texto = txtMensaje.Text.Trim();

            if (string.IsNullOrEmpty(texto))
            {
                MessageBox.Show("Por favor, ingresa un mensaje antes de enviar.", "Campo Vacío", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnEnviar.Enabled = false;
            lblEstado.Text = "Enviando mensaje...";

            bool exito = await GestionEquipos.EnviarNotificacionWhatsAppAsync(texto);

            btnEnviar.Enabled = true;
            lblEstado.Text = "";

            if (exito)
            {
                MessageBox.Show("¡Mensaje enviado con éxito a WhatsApp!", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
        }
    }
}