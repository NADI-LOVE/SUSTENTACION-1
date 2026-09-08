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
        // Paleta de Colores "RemindMe" / Modern Light UI
        private readonly Color bgMain = Color.FromArgb(243, 241, 250);       // Violeta muy claro de fondo
        private readonly Color cardBg = Color.White;                          // Tarjetas blancas
        private readonly Color primaryPurple = Color.FromArgb(112, 60, 222);   // Púrpura principal (#703CDE)
        private readonly Color textDark = Color.FromArgb(40, 40, 60);         // Texto principal oscuro
        private readonly Color textGray = Color.FromArgb(140, 145, 165);       // Texto secundario
        private readonly Color highlightPink = Color.FromArgb(255, 105, 140);  // Rosa acento
        private readonly Color softYellow = Color.FromArgb(254, 249, 231);    // Pastel para eventos
        private readonly Color softPurple = Color.FromArgb(240, 235, 255);    // Púrpura suave pastel

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
        private Label lblBannerTitle;

        public SalidaEntradaEquipos()
        {
            InitializeComponentes();
            CargarEventosPorFecha(DateTime.Today);
        }

        private void InitializeComponentes()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = bgMain;
            this.Padding = new Padding(25);

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f)); // Header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Contenido

            // ==========================================
            // HEADER SUPERIOR (Saludo + Subtítulo)
            // ==========================================
            Panel headerPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10) };

            Label lblSaludo = new Label
            {
                Text = "Hola, Trabajador",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = textDark,
                Location = new Point(0, 5),
                AutoSize = true
            };

            Label lblSubtitulo = new Label
            {
                Text = "Gestión de Salida, Entrada de Equipos y Eventos",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = textGray,
                Location = new Point(0, 35),
                AutoSize = true
            };

            headerPanel.Controls.Add(lblSaludo);
            headerPanel.Controls.Add(lblSubtitulo);

            // ==========================================
            // PANEL IZQUIERDO: CALENDARIO PRINCIPAL
            // ==========================================
            Panel leftContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = cardBg,
                Margin = new Padding(0, 0, 15, 0),
                Padding = new Padding(20)
            };
            AplicarBordesRedondeados(leftContainer, 16);

            Panel headerCalendar = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.Transparent };

            Label lblMes = new Label
            {
                Text = "Calendario de Agenda",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = textDark,
                Location = new Point(0, 10),
                AutoSize = true
            };

            Button btnNuevo = new Button
            {
                Text = "+ Agregar",
                Size = new Size(110, 35),
                Dock = DockStyle.Right,
                BackColor = primaryPurple,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnNuevo.FlatAppearance.BorderSize = 0;
            AplicarBordesRedondeados(btnNuevo, 10);
            btnNuevo.Click += (s, e) =>
            {
                DateTime fechaSeleccionada = monthCalendar.SelectionStart.Date;
                if (fechaSeleccionada < DateTime.Today)
                {
                    MessageBox.Show("No se pueden registrar eventos en fechas pasadas.", "Fecha no permitida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                AbrirFormularioEvento(fechaSeleccionada, null);
            };

            headerCalendar.Controls.Add(lblMes);
            headerCalendar.Controls.Add(btnNuevo);

            monthCalendar = new MonthCalendar
            {
                Dock = DockStyle.Fill,
                MaxSelectionCount = 1,
                ShowTodayCircle = true,
                BackColor = cardBg,
                TitleBackColor = primaryPurple,
                TitleForeColor = Color.White
            };

            // AL HACER CLIC EN CUALQUIER FECHA
            monthCalendar.DateSelected += (s, e) =>
            {
                DateTime fechaElegida = e.Start.Date;

                // 1. Actualiza la lista lateral en la derecha con los datos del día
                CargarEventosPorFecha(fechaElegida);

                // 2. Si es una fecha actual o futura, abre el formulario para agregar
                if (fechaElegida >= DateTime.Today)
                {
                    AbrirFormularioEvento(fechaElegida, null);
                }
            };

            leftContainer.Controls.Add(monthCalendar);
            leftContainer.Controls.Add(headerCalendar);

            // ==========================================
            // PANEL DERECHO: DETALLES Y LISTA
            // ==========================================
            Panel rightContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            // Card Rosa Superior
            Panel bannerCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = highlightPink,
                Margin = new Padding(0, 0, 0, 15),
                Padding = new Padding(15)
            };
            AplicarBordesRedondeados(bannerCard, 14);

            lblBannerTitle = new Label
            {
                Text = "Historial y Próximas Salidas",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 15),
                AutoSize = true
            };

            Label lblBannerSub = new Label
            {
                Text = "Mantén el control de tus equipos asignados",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(240, 240, 240),
                Location = new Point(15, 40),
                AutoSize = true
            };

            bannerCard.Controls.Add(lblBannerTitle);
            bannerCard.Controls.Add(lblBannerSub);

            // Contenedor de Lista de Eventos
            Panel listCardContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = cardBg,
                Margin = new Padding(0, 15, 0, 0),
                Padding = new Padding(15)
            };
            AplicarBordesRedondeados(listCardContainer, 16);

            panelListaEventos = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            listCardContainer.Controls.Add(panelListaEventos);

            rightContainer.Controls.Add(listCardContainer);
            rightContainer.Controls.Add(bannerCard);

            // Grid Layout
            mainLayout.Controls.Add(headerPanel, 0, 0);
            mainLayout.SetColumnSpan(headerPanel, 2);
            mainLayout.Controls.Add(leftContainer, 0, 1);
            mainLayout.Controls.Add(rightContainer, 1, 1);

            this.Controls.Add(mainLayout);
        }

        // =========================================================
        // MÉTODOS DE BASE DE DATOS (MYSQL)
        // =========================================================

        private void CargarEventosPorFecha(DateTime fecha)
        {
            panelListaEventos.Controls.Clear();

            if (fecha < DateTime.Today)
            {
                lblBannerTitle.Text = $"Historial ({fecha:dd/MM/yyyy})";
            }
            else
            {
                lblBannerTitle.Text = $"Próximas Salidas ({fecha:dd/MM/yyyy})";
            }

            Conexion conexionDB = new Conexion();

            try
            {
                MySqlConnection con = conexionDB.ObtenerConexion();
                string query = "SELECT id, fecha, titulo, horario, detalles FROM eventos WHERE fecha = @fecha ORDER BY id DESC";
                MySqlCommand cmd = new MySqlCommand(query, con);
                cmd.Parameters.AddWithValue("@fecha", fecha.ToString("yyyy-MM-dd"));

                MySqlDataReader reader = cmd.ExecuteReader();

                bool tieneRegistros = false;
                while (reader.Read())
                {
                    tieneRegistros = true;
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

                if (!tieneRegistros)
                {
                    Label lblVacio = new Label
                    {
                        Text = "No hay registros para esta fecha.",
                        Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                        ForeColor = textGray,
                        AutoSize = true,
                        Margin = new Padding(10)
                    };
                    panelListaEventos.Controls.Add(lblVacio);
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

            CargarEventosPorFecha(ev.Fecha);
        }

        private void EliminarEventoBD(int id, DateTime fechaActual)
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

            CargarEventosPorFecha(fechaActual);
        }

        // =========================================================
        // DISEÑO DE TARJETAS Y MODAL
        // =========================================================

        private void CrearTarjetaEventoUI(EventoItem ev)
        {
            Panel card = new Panel
            {
                Size = new Size(panelListaEventos.Width - 25, 90),
                Margin = new Padding(0, 0, 0, 12),
                BackColor = (ev.Fecha < DateTime.Today) ? Color.FromArgb(240, 240, 240) : ((ev.Id % 2 == 0) ? softPurple : softYellow)
            };
            AplicarBordesRedondeados(card, 12);

            Label lblFecha = new Label
            {
                Text = $"📅 {ev.Fecha:dd MMM yyyy}",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = (ev.Fecha < DateTime.Today) ? textGray : primaryPurple,
                Location = new Point(12, 10),
                AutoSize = true
            };

            Label lblTit = new Label
            {
                Text = ev.Titulo + (ev.Fecha < DateTime.Today ? " (Historial)" : ""),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = textDark,
                Location = new Point(12, 28),
                AutoSize = true
            };

            Label lblHor = new Label
            {
                Text = $"🕒 {ev.Horario} | {ev.Detalles}",
                Font = new Font("Segoe UI", 8f),
                ForeColor = textGray,
                Location = new Point(12, 55),
                AutoSize = true
            };

            if (ev.Fecha >= DateTime.Today)
            {
                Button btnEditar = new Button
                {
                    Text = "✏️",
                    Size = new Size(26, 26),
                    Location = new Point(card.Width - 62, 12),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = textDark,
                    Cursor = Cursors.Hand
                };
                btnEditar.FlatAppearance.BorderSize = 0;
                btnEditar.Click += (s, e) => AbrirFormularioEvento(ev.Fecha, ev);
                card.Controls.Add(btnEditar);
            }

            Button btnEliminar = new Button
            {
                Text = "🗑️",
                Size = new Size(26, 26),
                Location = new Point(card.Width - 32, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                ForeColor = highlightPink,
                Cursor = Cursors.Hand
            };
            btnEliminar.FlatAppearance.BorderSize = 0;
            btnEliminar.Click += (s, e) =>
            {
                if (MessageBox.Show("¿Deseas eliminar este registro?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    EliminarEventoBD(ev.Id, ev.Fecha);
                }
            };

            card.Controls.Add(lblFecha);
            card.Controls.Add(lblTit);
            card.Controls.Add(lblHor);
            card.Controls.Add(btnEliminar);

            panelListaEventos.Controls.Add(card);
        }

        private void AbrirFormularioEvento(DateTime fechaSeleccionada, EventoItem eventoExistente)
        {
            if (fechaSeleccionada.Date < DateTime.Today)
            {
                MessageBox.Show("No se pueden registrar ni modificar eventos en fechas pasadas.", "Acción No Permitida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Form formModal = new Form
            {
                Text = (eventoExistente == null) ? $"Registrar Evento ({fechaSeleccionada:dd/MM/yyyy})" : "Editar Evento",
                Size = new Size(360, 340),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = cardBg
            };

            Label lblT = new Label { Text = "Título del Evento:", ForeColor = textDark, Location = new Point(20, 15), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            TextBox txtTitulo = new TextBox { Location = new Point(20, 38), Width = 300, Text = eventoExistente?.Titulo ?? "", Font = new Font("Segoe UI", 9.5f) };

            Label lblH = new Label { Text = "Horario (ej. 11:00 AM - 01:00 PM):", ForeColor = textDark, Location = new Point(20, 80), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            TextBox txtHorario = new TextBox { Location = new Point(20, 103), Width = 300, Text = eventoExistente?.Horario ?? "", Font = new Font("Segoe UI", 9.5f) };

            Label lblD = new Label { Text = "Código de Equipo / Detalle:", ForeColor = textDark, Location = new Point(20, 145), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            TextBox txtDetalles = new TextBox { Location = new Point(20, 168), Width = 300, Text = eventoExistente?.Detalles ?? "", Font = new Font("Segoe UI", 9.5f) };

            Button btnGuardar = new Button
            {
                Text = "Guardar Evento",
                DialogResult = DialogResult.OK,
                Location = new Point(20, 225),
                Width = 300,
                Height = 40,
                BackColor = primaryPurple,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            AplicarBordesRedondeados(btnGuardar, 8);

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