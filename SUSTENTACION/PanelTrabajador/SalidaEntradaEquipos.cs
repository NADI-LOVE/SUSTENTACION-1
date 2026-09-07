using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace SUSTENTACION.PanelTrabajador
{
    public class SalidaEntradaEquipos : UserControl
    {
        // Paleta de Colores Dark UI
        private readonly Color bgDark = Color.FromArgb(24, 25, 38);
        private readonly Color cardBg = Color.FromArgb(32, 34, 53);
        private readonly Color primaryPurple = Color.FromArgb(108, 93, 211);
        private readonly Color textWhite = Color.FromArgb(255, 255, 255);
        private readonly Color textGray = Color.FromArgb(130, 134, 160);

        // Estructura de Datos para Eventos
        public class EventoItem
        {
            public int Id { get; set; }
            public DateTime Fecha { get; set; }
            public string Titulo { get; set; }
            public string Horario { get; set; }
            public string Detalles { get; set; }
        }

        private MonthCalendar monthCalendar;
        private FlowLayoutPanel panelListaEventos;

        public SalidaEntradaEquipos()
        {
            InitializeComponentes();
            CargarEventosDesdeBD();
        }

        private void InitializeComponentes()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = bgDark;
            this.Padding = new Padding(20);

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));

            // PANEL IZQUIERDO: CALENDARIO INTERACTIVO
            Panel leftPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 15, 0) };

            Label lblTitulo = new Label
            {
                Text = "Gestión de Equipos y Eventos",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(0, 5),
                AutoSize = true
            };
            leftPanel.Controls.Add(lblTitulo);

            Button btnNuevo = new Button
            {
                Text = "+ Agregar Evento",
                Size = new Size(150, 36),
                Location = new Point(0, 40),
                BackColor = primaryPurple,
                ForeColor = textWhite,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnNuevo.FlatAppearance.BorderSize = 0;
            AplicarBordesRedondeados(btnNuevo, 8);
            btnNuevo.Click += (s, e) => AbrirFormularioEvento(monthCalendar.SelectionStart, null);
            leftPanel.Controls.Add(btnNuevo);

            monthCalendar = new MonthCalendar
            {
                Location = new Point(0, 90),
                MaxSelectionCount = 1,
                ShowTodayCircle = true
            };
            monthCalendar.DateSelected += (s, e) => AbrirFormularioEvento(e.Start, null);
            leftPanel.Controls.Add(monthCalendar);

            // PANEL DERECHO: LISTA DINÁMICA DE EVENTOS
            Panel rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = cardBg,
                Padding = new Padding(15)
            };
            AplicarBordesRedondeados(rightPanel, 12);

            Label lblSideTitle = new Label
            {
                Text = "Próximos Eventos / Salidas",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(15, 15),
                AutoSize = true
            };
            rightPanel.Controls.Add(lblSideTitle);

            panelListaEventos = new FlowLayoutPanel
            {
                Location = new Point(15, 50),
                Size = new Size(rightPanel.Width - 30, 480),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            rightPanel.Controls.Add(panelListaEventos);

            mainLayout.Controls.Add(leftPanel, 0, 0);
            mainLayout.Controls.Add(rightPanel, 1, 0);
            this.Controls.Add(mainLayout);
        }

        // =========================================================
        // MÉTODOS DE BASE DE DATOS (MYSQL)
        // =========================================================

        private void CargarEventosDesdeBD()
        {
            panelListaEventos.Controls.Clear();
            Conexion conexionDB = new Conexion();

            try
            {
                MySqlConnection con = conexionDB.ObtenerConexion();
                string query = "SELECT id, fecha, titulo, horario, detalles FROM eventos ORDER BY fecha ASC";
                MySqlCommand cmd = new MySqlCommand(query, con);
                MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    EventoItem ev = new EventoItem
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        Fecha = Convert.ToDateTime(reader["fecha"]),
                        Titulo = reader["titulo"].ToString(),
                        Horario = reader["horario"].ToString(),
                        Detalles = reader["detalles"].ToString()
                    };

                    CrearTarjetaEventoUI(ev);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar eventos: " + ex.Message, "Error MySQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }
        }

        private void GuardarEventoEnBD(EventoItem ev)
        {
            Conexion conexionDB = new Conexion();

            try
            {
                MySqlConnection con = conexionDB.ObtenerConexion();
                string query;

                if (ev.Id == 0)
                {
                    query = "INSERT INTO eventos (fecha, titulo, horario, detalles) VALUES (@fecha, @titulo, @horario, @detalles)";
                }
                else
                {
                    query = "UPDATE eventos SET fecha=@fecha, titulo=@titulo, horario=@horario, detalles=@detalles WHERE id=@id";
                }

                MySqlCommand cmd = new MySqlCommand(query, con);
                cmd.Parameters.AddWithValue("@fecha", ev.Fecha.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@titulo", ev.Titulo);
                cmd.Parameters.AddWithValue("@horario", ev.Horario);
                cmd.Parameters.AddWithValue("@detalles", ev.Detalles);

                if (ev.Id != 0)
                {
                    cmd.Parameters.AddWithValue("@id", ev.Id);
                }

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar en BD: " + ex.Message, "Error MySQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }

            CargarEventosDesdeBD();
        }

        private void EliminarEventoBD(int id)
        {
            Conexion conexionDB = new Conexion();

            try
            {
                MySqlConnection con = conexionDB.ObtenerConexion();
                string query = "DELETE FROM eventos WHERE id=@id";
                MySqlCommand cmd = new MySqlCommand(query, con);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al eliminar evento: " + ex.Message, "Error MySQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }

            CargarEventosDesdeBD();
        }

        // =========================================================
        // COMPONENTES DINÁMICOS Y MODALES
        // =========================================================

        private void CrearTarjetaEventoUI(EventoItem ev)
        {
            Panel card = new Panel
            {
                Size = new Size(panelListaEventos.Width - 25, 80),
                Margin = new Padding(0, 0, 0, 10),
                BackColor = Color.FromArgb(41, 44, 69)
            };
            AplicarBordesRedondeados(card, 8);

            Label lblFecha = new Label
            {
                Text = ev.Fecha.ToString("dd MMM yyyy"),
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = primaryPurple,
                Location = new Point(10, 8),
                AutoSize = true
            };

            Label lblTit = new Label
            {
                Text = ev.Titulo,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(10, 26),
                AutoSize = true
            };

            Label lblHor = new Label
            {
                Text = $"🕒 {ev.Horario} | {ev.Detalles}",
                Font = new Font("Segoe UI", 8f),
                ForeColor = textGray,
                Location = new Point(10, 50),
                AutoSize = true
            };

            Button btnEditar = new Button
            {
                Text = "✏️",
                Size = new Size(28, 28),
                Location = new Point(card.Width - 65, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                ForeColor = textWhite,
                Cursor = Cursors.Hand
            };
            btnEditar.FlatAppearance.BorderSize = 0;
            btnEditar.Click += (s, e) => AbrirFormularioEvento(ev.Fecha, ev);

            Button btnEliminar = new Button
            {
                Text = "🗑️",
                Size = new Size(28, 28),
                Location = new Point(card.Width - 32, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.IndianRed,
                Cursor = Cursors.Hand
            };
            btnEliminar.FlatAppearance.BorderSize = 0;
            btnEliminar.Click += (s, e) =>
            {
                if (MessageBox.Show("¿Deseas eliminar este evento?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    EliminarEventoBD(ev.Id);
                }
            };

            card.Controls.Add(lblFecha);
            card.Controls.Add(lblTit);
            card.Controls.Add(lblHor);
            card.Controls.Add(btnEditar);
            card.Controls.Add(btnEliminar);

            panelListaEventos.Controls.Add(card);
        }

        private void AbrirFormularioEvento(DateTime fechaSeleccionada, EventoItem eventoExistente)
        {
            Form formModal = new Form
            {
                Text = (eventoExistente == null) ? "Registrar Evento" : "Editar Evento",
                Size = new Size(350, 320),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = cardBg
            };

            Label lblT = new Label { Text = "Título del Evento:", ForeColor = textWhite, Location = new Point(20, 15), AutoSize = true };
            TextBox txtTitulo = new TextBox { Location = new Point(20, 35), Width = 290, Text = eventoExistente?.Titulo ?? "" };

            Label lblH = new Label { Text = "Horario (ej. 08:00 AM - 10:00 AM):", ForeColor = textWhite, Location = new Point(20, 75), AutoSize = true };
            TextBox txtHorario = new TextBox { Location = new Point(20, 95), Width = 290, Text = eventoExistente?.Horario ?? "" };

            Label lblD = new Label { Text = "Código de Equipo / Detalle:", ForeColor = textWhite, Location = new Point(20, 135), AutoSize = true };
            TextBox txtDetalles = new TextBox { Location = new Point(20, 155), Width = 290, Text = eventoExistente?.Detalles ?? "" };

            Button btnGuardar = new Button
            {
                Text = "Guardar en BD",
                DialogResult = DialogResult.OK,
                Location = new Point(20, 210),
                Width = 290,
                Height = 35,
                BackColor = primaryPurple,
                ForeColor = textWhite,
                FlatStyle = FlatStyle.Flat
            };

            formModal.Controls.AddRange(new Control[] { lblT, txtTitulo, lblH, txtHorario, lblD, txtDetalles, btnGuardar });

            if (formModal.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(txtTitulo.Text))
            {
                EventoItem nuevoEvento = new EventoItem
                {
                    Id = eventoExistente?.Id ?? 0,
                    Fecha = fechaSeleccionada,
                    Titulo = txtTitulo.Text,
                    Horario = txtHorario.Text,
                    Detalles = txtDetalles.Text
                };

                GuardarEventoEnBD(nuevoEvento);
            }
        }

        private void AplicarBordesRedondeados(Control control, int radio)
        {
            Action recalcularRegion = () =>
            {
                if (control.Width <= 0 || control.Height <= 0) return;

                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddArc(0, 0, radio, radio, 180, 90);
                    path.AddArc(control.Width - radio, 0, radio, radio, 270, 90);
                    path.AddArc(control.Width - radio, control.Height - radio, radio, radio, 0, 90);
                    path.AddArc(0, control.Height - radio, radio, radio, 90, 90);
                    path.CloseAllFigures();
                    control.Region = new Region(path);
                }
            };

            control.Resize += (s, e) => recalcularRegion();
            recalcularRegion();
        }
    }
}