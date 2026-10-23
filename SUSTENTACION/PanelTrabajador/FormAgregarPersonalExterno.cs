using System;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;

namespace SUSTENTACION.PanelTrabajador
{
    public class FormAgregarPersonalExterno : Form
    {
        private readonly Color fondo = Color.FromArgb(17, 19, 32);
        private readonly Color tarjeta = Color.FromArgb(26, 29, 46);
        private readonly Color campo = Color.FromArgb(37, 41, 62);
        private readonly Color blanco = Color.FromArgb(245, 246, 255);
        private readonly Color gris = Color.FromArgb(158, 164, 188);
        private readonly Color verde = Color.FromArgb(22, 177, 127);

        private TextBox txtNombre;
        private TextBox txtApellido;
        private TextBox txtCelular;
        private TextBox txtDni;
        private TextBox txtDireccion;

        public int IdPersonalCreado { get; private set; }
        public string NombreCompletoCreado { get; private set; } = "";

        public FormAgregarPersonalExterno()
        {
            CrearInterfaz();
        }

        private void CrearInterfaz()
        {
            this.Text = "ARTEMUSA | Agregar personal externo";
            this.Size = new Size(580, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = fondo;
            this.Font = new Font("Segoe UI", 9);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            this.Controls.Add(new Label
            {
                Text = "AGREGAR PERSONAL EXTERNO",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(24, 20),
                AutoSize = true
            });

            this.Controls.Add(new Label
            {
                Text = "Personas sin acceso al sistema (apoyo eventual)",
                Font = new Font("Segoe UI", 9),
                ForeColor = gris,
                Location = new Point(26, 55),
                AutoSize = true
            });

            Panel tarjeta1 = new Panel
            {
                Location = new Point(24, 95),
                Size = new Size(530, 380),
                BackColor = tarjeta,
                Padding = new Padding(20)
            };
            this.Controls.Add(tarjeta1);

            int y = 20;

            tarjeta1.Controls.Add(new Label
            {
                Text = "Nombre *",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(20, y),
                AutoSize = true
            });
            txtNombre = new TextBox
            {
                Location = new Point(20, y + 22),
                Size = new Size(490, 30),
                BackColor = campo,
                ForeColor = blanco,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            tarjeta1.Controls.Add(txtNombre);
            y += 60;

            tarjeta1.Controls.Add(new Label
            {
                Text = "Apellido *",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(20, y),
                AutoSize = true
            });
            txtApellido = new TextBox
            {
                Location = new Point(20, y + 22),
                Size = new Size(490, 30),
                BackColor = campo,
                ForeColor = blanco,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            tarjeta1.Controls.Add(txtApellido);
            y += 60;

            tarjeta1.Controls.Add(new Label
            {
                Text = "Celular",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(20, y),
                AutoSize = true
            });
            txtCelular = new TextBox
            {
                Location = new Point(20, y + 22),
                Size = new Size(230, 30),
                BackColor = campo,
                ForeColor = blanco,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            tarjeta1.Controls.Add(txtCelular);

            tarjeta1.Controls.Add(new Label
            {
                Text = "DNI",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(270, y),
                AutoSize = true
            });
            txtDni = new TextBox
            {
                Location = new Point(270, y + 22),
                Size = new Size(240, 30),
                BackColor = campo,
                ForeColor = blanco,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            tarjeta1.Controls.Add(txtDni);
            y += 60;

            tarjeta1.Controls.Add(new Label
            {
                Text = "Dirección",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(20, y),
                AutoSize = true
            });
            txtDireccion = new TextBox
            {
                Location = new Point(20, y + 22),
                Size = new Size(490, 30),
                BackColor = campo,
                ForeColor = blanco,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            tarjeta1.Controls.Add(txtDireccion);

            Button btnGuardar = new Button
            {
                Text = "✓  AGREGAR",
                Width = 180,
                Height = 42,
                Location = new Point(24, 490),
                FlatStyle = FlatStyle.Flat,
                BackColor = verde,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += (s, e) => Guardar();
            this.Controls.Add(btnGuardar);

            Button btnCancelar = new Button
            {
                Text = "CANCELAR",
                Width = 140,
                Height = 42,
                Location = new Point(414, 490),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(65, 68, 88),
                ForeColor = blanco,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };
            this.Controls.Add(btnCancelar);
        }

        private void Guardar()
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("Ingresa el nombre.");
                txtNombre.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtApellido.Text))
            {
                MessageBox.Show("Ingresa el apellido.");
                txtApellido.Focus();
                return;
            }

            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();

                string query = @"
                    INSERT INTO personal_externo
                    (nombre, apellido, celular, dni, direccion)
                    VALUES (@nombre, @apellido, @celular, @dni, @direccion)
                    RETURNING id_personal";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@nombre", txtNombre.Text.Trim());
                    cmd.Parameters.AddWithValue("@apellido", txtApellido.Text.Trim());
                    cmd.Parameters.AddWithValue("@celular",
                        string.IsNullOrWhiteSpace(txtCelular.Text) ? (object)DBNull.Value : txtCelular.Text.Trim());
                    cmd.Parameters.AddWithValue("@dni",
                        string.IsNullOrWhiteSpace(txtDni.Text) ? (object)DBNull.Value : txtDni.Text.Trim());
                    cmd.Parameters.AddWithValue("@direccion",
                        string.IsNullOrWhiteSpace(txtDireccion.Text) ? (object)DBNull.Value : txtDireccion.Text.Trim());

                    object resultado = cmd.ExecuteScalar();

                    if (resultado != null && resultado != DBNull.Value)
                    {
                        IdPersonalCreado = Convert.ToInt32(resultado);
                        NombreCompletoCreado = txtNombre.Text.Trim() + " " + txtApellido.Text.Trim();

                        MessageBox.Show(
                            "Personal externo agregado correctamente.",
                            "Éxito",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al agregar personal externo:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }
        }
    }
}