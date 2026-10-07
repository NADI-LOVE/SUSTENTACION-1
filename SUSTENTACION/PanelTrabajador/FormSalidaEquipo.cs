using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace SUSTENTACION.PanelTrabajador
{
    public class FormSalidaEquipo : Form
    {
        private readonly Color bgMain = Color.FromArgb(21, 22, 37);
        private readonly Color bgCard = Color.FromArgb(30, 31, 48);
        private readonly Color purpleAccent = Color.FromArgb(92, 84, 241);
        private readonly Color textWhite = Color.White;
        private readonly Color textMuted = Color.FromArgb(130, 134, 158);

        private int idTrabajador;
        private string nombreTrabajador;
        private DateTime fechaSeleccionada;

        private TextBox txtCodigoBarras;
        private TextBox txtEncargado;
        private TextBox txtUbicacion;
        private TextBox txtDetalles;
        private ListBox lstEquiposAgregados;
        private WebBrowser webMapa; // Para mostrar Google Maps
        private List<int> equiposIds = new List<int>();

        public FormSalidaEquipo(int idTrabajador, string nombreTrabajador, DateTime fecha)
        {
            this.idTrabajador = idTrabajador;
            this.nombreTrabajador = nombreTrabajador;
            this.fechaSeleccionada = fecha;

            InicializarComponentes();
        }

        private void InicializarComponentes()
        {
            this.Text = $"Registrar Salida - {fechaSeleccionada:dd/MM/yyyy}";
            this.Size = new Size(900, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = bgMain;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // ==========================================
            // PANEL IZQUIERDO: FORMULARIO
            // ==========================================
            Panel leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 480,
                BackColor = bgCard,
                Padding = new Padding(20)
            };

            // Título
            Label lblTitulo = new Label
            {
                Text = "📦 Registrar Salida de Equipos",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(20, 20),
                AutoSize = true
            };
            leftPanel.Controls.Add(lblTitulo);

            // Fecha
            Label lblFecha = new Label
            {
                Text = $"📅 Fecha: {fechaSeleccionada:dddd, dd MMMM yyyy}",
                Font = new Font("Segoe UI", 9f),
                ForeColor = textMuted,
                Location = new Point(20, 55),
                AutoSize = true
            };
            leftPanel.Controls.Add(lblFecha);

            // Código de barras (pistola lectora)
            Label lblCodigo = new Label
            {
                Text = "Código de Barras (escanear con pistola):",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(20, 90),
                AutoSize = true
            };
            leftPanel.Controls.Add(lblCodigo);

            txtCodigoBarras = new TextBox
            {
                Location = new Point(20, 115),
                Width = 420,
                Height = 30,
                Font = new Font("Consolas", 12f),
                BackColor = Color.White,
                ForeColor = Color.Black
            };
            // Cuando la pistola escanea, envía Enter al final. Detectamos eso:
            txtCodigoBarras.KeyDown += TxtCodigoBarras_KeyDown;
            leftPanel.Controls.Add(txtCodigoBarras);

            // Botón para agregar manualmente
            Button btnAgregar = new Button
            {
                Text = "➕ Agregar Equipo",
                Location = new Point(20, 155),
                Width = 420,
                Height = 35,
                BackColor = purpleAccent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAgregar.FlatAppearance.BorderSize = 0;
            btnAgregar.Click += (s, e) => AgregarEquipoPorCodigo(txtCodigoBarras.Text.Trim());
            leftPanel.Controls.Add(btnAgregar);

            // Lista de equipos agregados
            Label lblLista = new Label
            {
                Text = "Equipos Agregados:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(20, 200),
                AutoSize = true
            };
            leftPanel.Controls.Add(lblLista);

            lstEquiposAgregados = new ListBox
            {
                Location = new Point(20, 225),
                Size = new Size(420, 120),
                BackColor = Color.FromArgb(40, 42, 60),
                ForeColor = textWhite,
                Font = new Font("Segoe UI", 9f),
                BorderStyle = BorderStyle.FixedSingle
            };
            leftPanel.Controls.Add(lstEquiposAgregados);

            // Encargado
            Label lblEncargado = new Label
            {
                Text = "Encargado:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(20, 360),
                AutoSize = true
            };
            leftPanel.Controls.Add(lblEncargado);

            txtEncargado = new TextBox
            {
                Location = new Point(20, 385),
                Width = 420,
                Height = 30,
                Font = new Font("Segoe UI", 10f),
                BackColor = Color.White,
                ForeColor = Color.Black,
                Text = nombreTrabajador // Por defecto el trabajador logueado
            };
            leftPanel.Controls.Add(txtEncargado);

            // Ubicación
            Label lblUbicacion = new Label
            {
                Text = "Ubicación del Evento:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(20, 425),
                AutoSize = true
            };
            leftPanel.Controls.Add(lblUbicacion);

            txtUbicacion = new TextBox
            {
                Location = new Point(20, 450),
                Width = 420,
                Height = 30,
                Font = new Font("Segoe UI", 10f),
                BackColor = Color.White,
                ForeColor = Color.Black
            };
            // Cuando el usuario escriba la ubicación, actualizar el mapa
            txtUbicacion.TextChanged += (s, e) => ActualizarMapa(txtUbicacion.Text);
            leftPanel.Controls.Add(txtUbicacion);

            // Detalles
            Label lblDetalles = new Label
            {
                Text = "Detalles adicionales:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(20, 490),
                AutoSize = true
            };
            leftPanel.Controls.Add(lblDetalles);

            txtDetalles = new TextBox
            {
                Location = new Point(20, 515),
                Width = 420,
                Height = 50,
                Multiline = true,
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.White,
                ForeColor = Color.Black
            };
            leftPanel.Controls.Add(txtDetalles);

            // Botones
            Button btnGuardar = new Button
            {
                Text = "💾 Guardar Salida",
                Location = new Point(20, 580),
                Width = 200,
                Height = 40,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += (s, e) => GuardarSalida();
            leftPanel.Controls.Add(btnGuardar);

            Button btnCancelar = new Button
            {
                Text = "❌ Cancelar",
                Location = new Point(240, 580),
                Width = 200,
                Height = 40,
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) => this.Close();
            leftPanel.Controls.Add(btnCancelar);

            this.Controls.Add(leftPanel);

            // ==========================================
            // PANEL DERECHO: MAPA DE GOOGLE MAPS
            // ==========================================
            Panel rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgMain,
                Padding = new Padding(20)
            };

            Label lblMapa = new Label
            {
                Text = "🗺️ Mapa de Ubicación",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(20, 20),
                AutoSize = true
            };
            rightPanel.Controls.Add(lblMapa);

            webMapa = new WebBrowser
            {
                Location = new Point(20, 55),
                Size = new Size(370, 550),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            rightPanel.Controls.Add(webMapa);

            this.Controls.Add(rightPanel);

            // Cargar mapa inicial
            ActualizarMapa("");
        }

        // ==========================================
        // ESCANEAR CÓDIGO DE BARRAS
        // ==========================================
        private void TxtCodigoBarras_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Evita el "ding" del Enter
                AgregarEquipoPorCodigo(txtCodigoBarras.Text.Trim());
                txtCodigoBarras.Clear();
            }
        }

        private void AgregarEquipoPorCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return;

            Conexion conexionDB = new Conexion();
            try
            {
                MySqlConnection con = conexionDB.ObtenerConexion();
                string query = "SELECT id, codigo, nombre FROM equipos WHERE codigo = @codigo";
                MySqlCommand cmd = new MySqlCommand(query, con);
                cmd.Parameters.AddWithValue("@codigo", codigo);

                MySqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    int idEquipo = Convert.ToInt32(reader["id"]);
                    string nombreEquipo = reader["nombre"].ToString();

                    if (!equiposIds.Contains(idEquipo))
                    {
                        equiposIds.Add(idEquipo);
                        lstEquiposAgregados.Items.Add($"{codigo} - {nombreEquipo}");
                    }
                    else
                    {
                        MessageBox.Show("Este equipo ya está en la lista.", "Duplicado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show($"No se encontró ningún equipo con el código: {codigo}", "No encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al buscar equipo: " + ex.Message, "Error MySQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }
        }

        // ==========================================
        // ACTUALIZAR MAPA DE GOOGLE MAPS
        // ==========================================
        private void ActualizarMapa(string direccion)
        {
            if (string.IsNullOrWhiteSpace(direccion))
            {
                // Mapa por defecto (Lima, Perú)
                string url = "https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d3901.847604927243!2d-77.042754!3d-12.046374!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x0%3A0x0!2zMTLCsDAyJzQ2LjkiUyA3N8KwMDInMzMuOSJX!5e0!3m2!1ses!2spe!4v1234567890";
                webMapa.Navigate(url);
            }
            else
            {
                // Mapa con la dirección buscada
                string encoded = Uri.EscapeDataString(direccion);
                string url = $"https://www.google.com/maps?q={encoded}&output=embed";
                webMapa.Navigate(url);
            }
        }

        // ==========================================
        // GUARDAR SALIDA EN LA BASE DE DATOS
        // ==========================================
        private void GuardarSalida()
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(txtEncargado.Text))
            {
                MessageBox.Show("Ingresa el nombre del encargado.", "Campo requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtUbicacion.Text))
            {
                MessageBox.Show("Ingresa la ubicación del evento.", "Campo requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (equiposIds.Count == 0)
            {
                MessageBox.Show("Agrega al menos un equipo.", "Campo requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Conexion conexionDB = new Conexion();
            try
            {
                MySqlConnection con = conexionDB.ObtenerConexion();

                // 1. Insertar la salida
                string querySalida = @"INSERT INTO salidas 
                    (id_trabajador, encargado, fecha_salida, ubicacion, detalles, estado) 
                    VALUES (@idTrabajador, @encargado, @fecha, @ubicacion, @detalles, 'Programada')";

                MySqlCommand cmdSalida = new MySqlCommand(querySalida, con);
                cmdSalida.Parameters.AddWithValue("@idTrabajador", idTrabajador);
                cmdSalida.Parameters.AddWithValue("@encargado", txtEncargado.Text);
                cmdSalida.Parameters.AddWithValue("@fecha", fechaSeleccionada.ToString("yyyy-MM-dd HH:mm:ss"));
                cmdSalida.Parameters.AddWithValue("@ubicacion", txtUbicacion.Text);
                cmdSalida.Parameters.AddWithValue("@detalles", txtDetalles.Text);
                cmdSalida.ExecuteNonQuery();

                int idSalida = (int)cmdSalida.LastInsertedId;

                // 2. Insertar los equipos
                foreach (int idEquipo in equiposIds)
                {
                    string queryEquipo = @"INSERT INTO salida_equipos 
                        (id_salida, id_equipo) VALUES (@idSalida, @idEquipo)";
                    MySqlCommand cmdEquipo = new MySqlCommand(queryEquipo, con);
                    cmdEquipo.Parameters.AddWithValue("@idSalida", idSalida);
                    cmdEquipo.Parameters.AddWithValue("@idEquipo", idEquipo);
                    cmdEquipo.ExecuteNonQuery();
                }

                MessageBox.Show("¡Salida registrada correctamente!", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar salida: " + ex.Message, "Error MySQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }
        }
    }
}