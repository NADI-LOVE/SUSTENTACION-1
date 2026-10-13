using Npgsql;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SUSTENTACION.PanelTrabajador
{
    public class FormSalidaEquipo : Form
    {
        // ==========================================
        // PALETA DE COLORES
        // ==========================================
        private readonly Color fondo = Color.FromArgb(17, 19, 32);
        private readonly Color tarjeta = Color.FromArgb(26, 29, 46);
        private readonly Color campo = Color.FromArgb(37, 41, 62);
        private readonly Color morado = Color.FromArgb(100, 88, 245);
        private readonly Color blanco = Color.FromArgb(245, 246, 255);
        private readonly Color gris = Color.FromArgb(158, 164, 188);
        private readonly Color verde = Color.FromArgb(22, 177, 127);
        private readonly Color naranja = Color.FromArgb(255, 160, 60);
        private readonly Color rojo = Color.FromArgb(230, 80, 80);
        private readonly Color amarillo = Color.FromArgb(245, 200, 70);

        // ==========================================
        // DATOS RECIBIDOS
        // ==========================================
        private readonly int idTrabajador;
        private readonly string nombreTrabajador;
        private readonly DateTime fechaSeleccionada;

        // ==========================================
        // ESTADO DEL WIZARD
        // ==========================================
        private int pasoActual = 1;
        private const int TOTAL_PASOS = 5;

        // ==========================================
        // DATOS PERSISTENTES
        // ==========================================
        private readonly DatosEvento datosEvento = new DatosEvento();
        private readonly List<LugarEvento> lugares = new List<LugarEvento>();
        private int idEncargadoSeleccionado = 0;
        private readonly List<int> idsSubTrabajadores = new List<int>();
        private readonly List<int> idsPersonalExterno = new List<int>();
        private readonly List<EquipoAsignado> equiposAsignados = new List<EquipoAsignado>();

        // ==========================================
        // COMPONENTES DE LA UI
        // ==========================================
        private Panel panelHeader;
        private Panel panelContenido;
        private Panel panelFooter;
        private Button btnAnterior;
        private Button btnSiguiente;
        private readonly Label[] lblPasos = new Label[5];

        // ==========================================
        // COMPONENTES DEL MAPA
        // ==========================================
        private Microsoft.Web.WebView2.WinForms.WebView2 webMapa;
        private Label lblEstadoMapa;
        private bool mapaListo = false;
        private LugarEvento lugarSeleccionado = null;
        private bool mapaInicializado = false;
        private string campoActivoParaMapa = "direccion";

        // ==========================================
        // CLASES INTERNAS
        // ==========================================
        private class DatosEvento
        {
            public string Nombre { get; set; } = "";
            public string TipoEvento { get; set; } = "Conferencia";
            public DateTime FechaInicio { get; set; } = DateTime.Now;
            public DateTime HoraInicio { get; set; } = DateTime.Today.AddHours(8);
            public DateTime FechaFin { get; set; } = DateTime.Now;
            public DateTime HoraFin { get; set; } = DateTime.Today.AddHours(18);
            public string Descripcion { get; set; } = "";
        }

        private class LugarEvento
        {
            public string Nombre { get; set; } = "";
            public string Direccion { get; set; } = "";
            public string Referencia { get; set; } = "";
            public decimal? Latitud { get; set; }
            public decimal? Longitud { get; set; }
            public Panel Panel { get; set; }
        }

        private class EquipoAsignado
        {
            public int IdEquipo { get; set; }
            public string Codigo { get; set; } = "";
            public string Nombre { get; set; } = "";
            public int? IdLugar { get; set; }
            public string ResponsableTipo { get; set; } = "";
            public int ResponsableId { get; set; }
            public string ResponsableNombre { get; set; } = "";
            public Panel Panel { get; set; }
        }

        private class TrabajadorItem
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = "";
            public string Rol { get; set; } = "";
        }

        private class PersonalExternoItem
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = "";
            public string Celular { get; set; } = "";
        }

        private class ResponsableItem
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = "";
            public string Tipo { get; set; } = "";
            public override string ToString() => Nombre;
        }

        // ==========================================
        // CONSTRUCTOR
        // ==========================================
        public FormSalidaEquipo(int idTrabajador, string nombreTrabajador, DateTime fecha)
        {
            this.idTrabajador = idTrabajador;
            this.nombreTrabajador = nombreTrabajador;
            this.fechaSeleccionada = fecha;

            datosEvento.FechaInicio = fecha;
            datosEvento.FechaFin = fecha;

            // 🆕 Redimensionar el mapa cuando cambia el tamaño de la ventana
            this.Resize += (s, e) =>
            {
                try
                {
                    if (webMapa != null && webMapa.CoreWebView2 != null)
                    {
                        webMapa.CoreWebView2.ExecuteScriptAsync("map.invalidateSize()");
                    }
                }
                catch { }
            };

            CrearInterfazBase();
            MostrarPaso();
        }

        // ==========================================
        // INTERFAZ BASE
        // ==========================================
        private void CrearInterfazBase()
        {
            this.Text = "ARTEMUSA | Registrar salida audiovisual";
            this.Size = new Size(1250, 830);
            this.MinimumSize = new Size(1080, 730);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = fondo;
            this.Font = new Font("Segoe UI", 9);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;

            panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 135,
                BackColor = fondo
            };
            this.Controls.Add(panelHeader);

            Label titulo = new Label
            {
                Text = "REGISTRAR SALIDA DE EQUIPOS AUDIOVISUALES",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(24, 15),
                AutoSize = true
            };
            panelHeader.Controls.Add(titulo);

            string[] nombresPasos = { "Evento", "Lugares", "Personal", "Equipos", "Resumen" };
            int anchoPaso = 200;
            int xInicial = 24;
            int yPaso = 70;

            for (int i = 0; i < TOTAL_PASOS; i++)
            {
                int numPaso = i + 1;
                int x = xInicial + (i * anchoPaso);

                Label lblNum = new Label
                {
                    Text = numPaso.ToString(),
                    Font = new Font("Segoe UI", 11, FontStyle.Bold),
                    ForeColor = blanco,
                    BackColor = Color.FromArgb(50, 55, 80),
                    Size = new Size(38, 38),
                    Location = new Point(x, yPaso),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                panelHeader.Controls.Add(lblNum);

                Label lblNombre = new Label
                {
                    Text = nombresPasos[i],
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = gris,
                    Location = new Point(x + 46, yPaso + 10),
                    AutoSize = true
                };
                panelHeader.Controls.Add(lblNombre);

                lblPasos[i] = lblNum;

                if (i < TOTAL_PASOS - 1)
                {
                    Panel linea = new Panel
                    {
                        BackColor = Color.FromArgb(50, 55, 80),
                        Size = new Size(anchoPaso - 46, 2),
                        Location = new Point(x + 42, yPaso + 18)
                    };
                    panelHeader.Controls.Add(linea);
                }
            }

            panelContenido = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = fondo,
                Padding = new Padding(24, 10, 24, 10),
                AutoScroll = true
            };
            this.Controls.Add(panelContenido);
            panelContenido.BringToFront();

            panelFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 75,
                BackColor = fondo
            };
            this.Controls.Add(panelFooter);

            btnAnterior = new Button
            {
                Text = "←  ANTERIOR",
                Width = 160,
                Height = 45,
                Location = new Point(24, 15),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(65, 68, 88),
                ForeColor = blanco,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnAnterior.FlatAppearance.BorderSize = 0;
            btnAnterior.Click += (s, e) => AnteriorPaso();
            panelFooter.Controls.Add(btnAnterior);

            btnSiguiente = new Button
            {
                Text = "SIGUIENTE  →",
                Width = 220,
                Height = 45,
                Location = new Point(980, 15),
                FlatStyle = FlatStyle.Flat,
                BackColor = morado,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnSiguiente.FlatAppearance.BorderSize = 0;
            btnSiguiente.Click += (s, e) => SiguientePaso();
            panelFooter.Controls.Add(btnSiguiente);

            panelFooter.Resize += (s, e) =>
            {
                btnSiguiente.Left = panelFooter.Width - btnSiguiente.Width - 24;
            };
        }

        // ==========================================
        // NAVEGACIÓN
        // ==========================================
        private void MostrarPaso()
        {
            panelContenido.Controls.Clear();

            for (int i = 0; i < TOTAL_PASOS; i++)
            {
                int numPaso = i + 1;
                if (numPaso < pasoActual)
                {
                    lblPasos[i].BackColor = verde;
                    lblPasos[i].Text = "✓";
                }
                else if (numPaso == pasoActual)
                {
                    lblPasos[i].BackColor = morado;
                    lblPasos[i].Text = numPaso.ToString();
                }
                else
                {
                    lblPasos[i].BackColor = Color.FromArgb(50, 55, 80);
                    lblPasos[i].Text = numPaso.ToString();
                }
            }

            btnAnterior.Enabled = pasoActual > 1;

            if (pasoActual == TOTAL_PASOS)
            {
                btnSiguiente.Text = "✓  GUARDAR TODO";
                btnSiguiente.BackColor = verde;
            }
            else
            {
                btnSiguiente.Text = "SIGUIENTE  →";
                btnSiguiente.BackColor = morado;
            }

            switch (pasoActual)
            {
                case 1: MostrarPaso1_Evento(); break;
                case 2: MostrarPaso2_Lugares(); break;
                case 3: MostrarPaso3_Personal(); break;
                case 4: MostrarPaso4_Equipos(); break;
                case 5: MostrarPaso5_Resumen(); break;
            }
        }

        private void SiguientePaso()
        {
            if (!ValidarPasoActual()) return;

            if (pasoActual == TOTAL_PASOS)
            {
                GuardarTodo();
                return;
            }

            pasoActual++;
            MostrarPaso();
        }

        private void AnteriorPaso()
        {
            if (pasoActual > 1)
            {
                pasoActual--;
                MostrarPaso();
            }
        }

        private bool ValidarPasoActual()
        {
            switch (pasoActual)
            {
                case 1: return ValidarPaso1();
                case 2: return ValidarPaso2();
                case 3: return ValidarPaso3();
                case 4: return ValidarPaso4();
                default: return true;
            }
        }

        // ==========================================
        // PASO 1: DATOS DEL EVENTO
        // ==========================================
        private void MostrarPaso1_Evento()
        {
            Label titulo = new Label
            {
                Text = "①  DATOS DEL EVENTO",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(0, 0),
                AutoSize = true
            };
            panelContenido.Controls.Add(titulo);

            Label subtitulo = new Label
            {
                Text = "Información básica del evento o servicio audiovisual",
                Font = new Font("Segoe UI", 9),
                ForeColor = gris,
                Location = new Point(0, 35),
                AutoSize = true
            };
            panelContenido.Controls.Add(subtitulo);

            Panel tarjeta1 = new Panel
            {
                Location = new Point(0, 75),
                Size = new Size(720, 470),
                BackColor = tarjeta,
                Padding = new Padding(25)
            };
            panelContenido.Controls.Add(tarjeta1);

            int y = 20;

            tarjeta1.Controls.Add(new Label
            {
                Text = "Nombre del evento *",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(25, y),
                AutoSize = true
            });
            TextBox txtNombreEvento = new TextBox
            {
                Location = new Point(25, y + 25),
                Size = new Size(650, 32),
                BackColor = campo,
                ForeColor = blanco,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                Text = datosEvento.Nombre
            };
            txtNombreEvento.TextChanged += (s, e) => datosEvento.Nombre = txtNombreEvento.Text;
            tarjeta1.Controls.Add(txtNombreEvento);
            y += 70;

            tarjeta1.Controls.Add(new Label
            {
                Text = "Tipo de evento *",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(25, y),
                AutoSize = true
            });
            ComboBox cboTipoEvento = new ComboBox
            {
                Location = new Point(25, y + 25),
                Size = new Size(650, 32),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = campo,
                ForeColor = blanco,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10)
            };
            cboTipoEvento.Items.AddRange(new object[]
            {
                "Conferencia", "Taller", "Reunión", "Producción audiovisual",
                "Evento deportivo", "Evento corporativo", "Matrimonio",
                "Fiesta patronal", "Bautizo", "Cumpleaños", "Aniversario",
                "Graduación", "Evento religioso", "Concierto", "Yunza",
                "Carnaval", "Fiesta de la Candelaria", "Otro"
            });
            cboTipoEvento.SelectedItem = datosEvento.TipoEvento;
            if (cboTipoEvento.SelectedIndex < 0) cboTipoEvento.SelectedIndex = 0;
            cboTipoEvento.SelectedIndexChanged += (s, e) =>
                datosEvento.TipoEvento = cboTipoEvento.SelectedItem?.ToString() ?? "Conferencia";
            tarjeta1.Controls.Add(cboTipoEvento);
            y += 70;

            tarjeta1.Controls.Add(new Label
            {
                Text = "Fecha y hora de inicio *",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(25, y),
                AutoSize = true
            });

            DateTimePicker dtpFechaInicio = new DateTimePicker
            {
                Location = new Point(25, y + 25),
                Size = new Size(320, 32),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 10),
                Value = datosEvento.FechaInicio
            };
            dtpFechaInicio.ValueChanged += (s, e) => datosEvento.FechaInicio = dtpFechaInicio.Value;
            tarjeta1.Controls.Add(dtpFechaInicio);

            DateTimePicker dtpHoraInicio = new DateTimePicker
            {
                Location = new Point(355, y + 25),
                Size = new Size(320, 32),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Font = new Font("Segoe UI", 10),
                Value = datosEvento.HoraInicio
            };
            dtpHoraInicio.ValueChanged += (s, e) => datosEvento.HoraInicio = dtpHoraInicio.Value;
            tarjeta1.Controls.Add(dtpHoraInicio);
            y += 70;

            tarjeta1.Controls.Add(new Label
            {
                Text = "Fecha y hora de fin (opcional)",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(25, y),
                AutoSize = true
            });

            DateTimePicker dtpFechaFin = new DateTimePicker
            {
                Location = new Point(25, y + 25),
                Size = new Size(320, 32),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 10),
                Value = datosEvento.FechaFin
            };
            dtpFechaFin.ValueChanged += (s, e) => datosEvento.FechaFin = dtpFechaFin.Value;
            tarjeta1.Controls.Add(dtpFechaFin);

            DateTimePicker dtpHoraFin = new DateTimePicker
            {
                Location = new Point(355, y + 25),
                Size = new Size(320, 32),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Font = new Font("Segoe UI", 10),
                Value = datosEvento.HoraFin
            };
            dtpHoraFin.ValueChanged += (s, e) => datosEvento.HoraFin = dtpHoraFin.Value;
            tarjeta1.Controls.Add(dtpHoraFin);
            y += 70;

            tarjeta1.Controls.Add(new Label
            {
                Text = "Descripción (opcional)",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(25, y),
                AutoSize = true
            });
            TextBox txtDescripcionEvento = new TextBox
            {
                Location = new Point(25, y + 25),
                Size = new Size(650, 65),
                Multiline = true,
                BackColor = campo,
                ForeColor = blanco,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                ScrollBars = ScrollBars.Vertical,
                Text = datosEvento.Descripcion
            };
            txtDescripcionEvento.TextChanged += (s, e) => datosEvento.Descripcion = txtDescripcionEvento.Text;
            tarjeta1.Controls.Add(txtDescripcionEvento);
        }

        private bool ValidarPaso1()
        {
            if (string.IsNullOrWhiteSpace(datosEvento.Nombre))
            {
                MessageBox.Show("Ingresa el nombre del evento.");
                return false;
            }
            if (datosEvento.FechaFin.Date < datosEvento.FechaInicio.Date)
            {
                MessageBox.Show("La fecha fin no puede ser anterior a la fecha de inicio.");
                return false;
            }
            return true;
        }

        // ==========================================
        // PASO 2: LUGARES CON MAPA
        // ==========================================
        private void MostrarPaso2_Lugares()
        {
            Label titulo = new Label
            {
                Text = "②  LUGARES DEL EVENTO",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(0, 0),
                AutoSize = true
            };
            panelContenido.Controls.Add(titulo);

            Label subtitulo = new Label
            {
                Text = "Haz clic en 'Buscar en mapa' para ubicar cada lugar.",
                Font = new Font("Segoe UI", 9),
                ForeColor = gris,
                Location = new Point(0, 35),
                AutoSize = true
            };
            panelContenido.Controls.Add(subtitulo);

            // Contenedor 2 columnas
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Location = new Point(0, 75),
                Size = new Size(1180, 580),
                ColumnCount = 2,
                RowCount = 1,
                BackColor = fondo,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            panelContenido.Controls.Add(layout);

            // ─────────────────────────
            // COLUMNA IZQUIERDA (lugares)
            // ─────────────────────────
            Panel panelIzq = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = fondo,
                Padding = new Padding(0, 0, 8, 0)
            };
            layout.Controls.Add(panelIzq, 0, 0);

            Button btnAgregar = new Button
            {
                Text = "+  AGREGAR LUGAR",
                Width = 220,
                Height = 42,
                Location = new Point(0, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = morado,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAgregar.FlatAppearance.BorderSize = 0;
            btnAgregar.Click += (s, e) => AgregarLugarUI();
            panelIzq.Controls.Add(btnAgregar);

            FlowLayoutPanel flpLugares = new FlowLayoutPanel
            {
                Location = new Point(0, 55),
                Size = new Size(570, 520),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = fondo,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Name = "flpLugares"
            };
            panelIzq.Controls.Add(flpLugares);

            foreach (var l in lugares)
                AgregarLugarUI(l);

            // ─────────────────────────
            // COLUMNA DERECHA (mapa)
            // ─────────────────────────
            Panel panelDer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = tarjeta,
                Padding = new Padding(12)
            };
            layout.Controls.Add(panelDer, 1, 0);

            lblEstadoMapa = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                Text = "Cargando mapa...",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = morado,
                BackColor = tarjeta,
                Padding = new Padding(4, 5, 0, 0)
            };
            panelDer.Controls.Add(lblEstadoMapa);

            webMapa = new Microsoft.Web.WebView2.WinForms.WebView2
            {
                Dock = DockStyle.Fill,
                DefaultBackgroundColor = Color.FromArgb(31, 35, 51)
            };
            panelDer.Controls.Add(webMapa);
            webMapa.BringToFront();
            lblEstadoMapa.BringToFront();

            // 🆕 Forzar el tamaño después de que se muestre el panel
            this.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (webMapa != null && webMapa.CoreWebView2 != null)
                    {
                        webMapa.CoreWebView2.ExecuteScriptAsync("map.invalidateSize()");
                    }
                }
                catch { }
            }));

            // Inicializar el mapa la primera vez
            if (!mapaInicializado)
            {
                mapaInicializado = true;
                this.BeginInvoke(new Action(async () => await InicializarMapaAsync()));
            }
        }

        private void AgregarLugarUI(LugarEvento lugarExistente = null)
        {
            var flp = panelContenido.Controls.Find("flpLugares", true).Length > 0
                ? panelContenido.Controls.Find("flpLugares", true)[0] as FlowLayoutPanel
                : null;
            if (flp == null) return;

            var lugar = lugarExistente ?? new LugarEvento();
            bool esNuevo = lugarExistente == null;
            if (esNuevo) lugares.Add(lugar);

            Panel card = new Panel
            {
                Width = 530,
                Height = 260,
                BackColor = tarjeta,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(15)
            };

            int numLugar = flp.Controls.Count + 1;

            Label lblNum = new Label
            {
                Text = "Lugar " + numLugar,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = morado,
                Location = new Point(15, 8),
                AutoSize = true,
                Tag = "numero"
            };
            card.Controls.Add(lblNum);

            // ═══════════════════════════════════════
            // NOMBRE + botón 📍
            // ═══════════════════════════════════════
            card.Controls.Add(new Label
            {
                Text = "Nombre del lugar *",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(15, 38),
                AutoSize = true
            });
            TextBox txtNombre = new TextBox
            {
                Location = new Point(15, 56),
                Size = new Size(450, 26),
                BackColor = campo,
                ForeColor = blanco,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                Text = lugar.Nombre,
                Name = "txtNombreLugar"
            };
            txtNombre.TextChanged += (s, e) => lugar.Nombre = txtNombre.Text.Trim();
            card.Controls.Add(txtNombre);

            Button btnNombre = new Button
            {
                Text = "📍",
                Width = 40,
                Height = 26,
                Location = new Point(470, 56),
                FlatStyle = FlatStyle.Flat,
                BackColor = morado,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnNombre.FlatAppearance.BorderSize = 0;
            btnNombre.Click += (s, e) => SeleccionarLugarParaMapa(lugar, card, "nombre");
            card.Controls.Add(btnNombre);

            // ═══════════════════════════════════════
            // DIRECCIÓN + botón 📍
            // ═══════════════════════════════════════
            card.Controls.Add(new Label
            {
                Text = "Dirección",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(15, 92),
                AutoSize = true
            });
            TextBox txtDireccion = new TextBox
            {
                Location = new Point(15, 110),
                Size = new Size(450, 26),
                BackColor = campo,
                ForeColor = blanco,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                Text = lugar.Direccion,
                Name = "txtDireccion"
            };
            txtDireccion.TextChanged += (s, e) => lugar.Direccion = txtDireccion.Text.Trim();
            card.Controls.Add(txtDireccion);

            Button btnDir = new Button
            {
                Text = "📍",
                Width = 40,
                Height = 26,
                Location = new Point(470, 110),
                FlatStyle = FlatStyle.Flat,
                BackColor = morado,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDir.FlatAppearance.BorderSize = 0;
            btnDir.Click += (s, e) => SeleccionarLugarParaMapa(lugar, card, "direccion");
            card.Controls.Add(btnDir);

            // ═══════════════════════════════════════
            // REFERENCIA + botón 📍
            // ═══════════════════════════════════════
            card.Controls.Add(new Label
            {
                Text = "Referencia",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(15, 146),
                AutoSize = true
            });
            TextBox txtReferencia = new TextBox
            {
                Location = new Point(15, 164),
                Size = new Size(450, 26),
                BackColor = campo,
                ForeColor = blanco,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                Text = lugar.Referencia,
                Name = "txtReferencia"
            };
            txtReferencia.TextChanged += (s, e) => lugar.Referencia = txtReferencia.Text.Trim();
            card.Controls.Add(txtReferencia);

            Button btnRef = new Button
            {
                Text = "📍",
                Width = 40,
                Height = 26,
                Location = new Point(470, 164),
                FlatStyle = FlatStyle.Flat,
                BackColor = morado,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRef.FlatAppearance.BorderSize = 0;
            btnRef.Click += (s, e) => SeleccionarLugarParaMapa(lugar, card, "referencia");
            card.Controls.Add(btnRef);

            // ═══════════════════════════════════════
            // BOTÓN QUITAR
            // ═══════════════════════════════════════
            Button btnQuitar = new Button
            {
                Text = "✗ QUITAR LUGAR",
                Width = 140,
                Height = 32,
                Location = new Point(15, 205),
                FlatStyle = FlatStyle.Flat,
                BackColor = rojo,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnQuitar.FlatAppearance.BorderSize = 0;
            btnQuitar.Click += (s, e) =>
            {
                lugares.Remove(lugar);
                flp.Controls.Remove(card);
                RenumerarLugares();
                if (lugarSeleccionado == lugar) lugarSeleccionado = null;
            };
            card.Controls.Add(btnQuitar);

            lugar.Panel = card;
            card.Tag = lugar;
            flp.Controls.Add(card);
        }

        private void RenumerarLugares()
        {
            var flp = panelContenido.Controls.Find("flpLugares", true).Length > 0
                ? panelContenido.Controls.Find("flpLugares", true)[0] as FlowLayoutPanel
                : null;
            if (flp == null) return;

            for (int i = 0; i < flp.Controls.Count; i++)
            {
                var card = flp.Controls[i] as Panel;
                if (card == null) continue;
                foreach (Control c in card.Controls)
                {
                    if (c is Label lbl && lbl.Tag != null && lbl.Tag.ToString() == "numero")
                    {
                        lbl.Text = "Lugar " + (i + 1);
                        break;
                    }
                }
            }
        }

        private bool ValidarPaso2()
        {
            if (lugares.Count == 0)
            {
                MessageBox.Show("Agrega al menos un lugar.");
                return false;
            }
            foreach (var l in lugares)
            {
                if (string.IsNullOrWhiteSpace(l.Nombre))
                {
                    MessageBox.Show("Todos los lugares deben tener nombre.");
                    return false;
                }
            }
            return true;
        }

        // ==========================================
        // SELECCIONAR LUGAR PARA MAPA
        // ==========================================
        private void SeleccionarLugarParaMapa(LugarEvento lugar, Panel card, string campo)
        {
            var flp = panelContenido.Controls.Find("flpLugares", true).Length > 0
                ? panelContenido.Controls.Find("flpLugares", true)[0] as FlowLayoutPanel
                : null;
            if (flp != null)
            {
                foreach (Control c in flp.Controls)
                    c.BackColor = tarjeta;
            }

            card.BackColor = Color.FromArgb(45, 48, 70);
            lugarSeleccionado = lugar;
            campoActivoParaMapa = campo;

            string nombreCampo = campo == "nombre" ? "Nombre" :
                                 campo == "direccion" ? "Dirección" : "Referencia";

            if (lblEstadoMapa != null)
                lblEstadoMapa.Text = $"📍 Seleccionando: {nombreCampo}";

            if (mapaListo && webMapa?.CoreWebView2 != null)
            {
                string busqueda = "";
                if (campo == "nombre") busqueda = lugar.Nombre;
                else if (campo == "direccion") busqueda = lugar.Direccion;
                else busqueda = lugar.Referencia;

                if (string.IsNullOrWhiteSpace(busqueda))
                    busqueda = lugar.Direccion;
                if (string.IsNullOrWhiteSpace(busqueda))
                    busqueda = lugar.Nombre;

                if (!string.IsNullOrWhiteSpace(busqueda))
                    webMapa.CoreWebView2.PostWebMessageAsString(busqueda);
            }
        }

        // ==========================================
        // INICIALIZAR MAPA
        // ==========================================
        private async System.Threading.Tasks.Task InicializarMapaAsync()
        {
            try
            {
                if (webMapa == null) return;

                lblEstadoMapa.Text = "Cargando motor del mapa...";

                await webMapa.EnsureCoreWebView2Async();
                // 🔧 TEMPORAL: Abrir DevTools para debug
                webMapa.CoreWebView2.OpenDevToolsWindow();

                webMapa.CoreWebView2.WebMessageReceived += RecibirMensajeMapa;

                string rutaMapa = System.IO.Path.Combine(
                    Application.StartupPath,
                    "PanelTrabajador",
                    "Mapas",
                    "mapa.html");

                if (!System.IO.File.Exists(rutaMapa))
                {
                    rutaMapa = System.IO.Path.Combine(
                        Application.StartupPath,
                        "Mapas",
                        "mapa.html");
                }

                if (!System.IO.File.Exists(rutaMapa))
                {
                    lblEstadoMapa.Text = "No se encontró mapa.html";
                    return;
                }

                string carpetaMapa = System.IO.Path.GetDirectoryName(rutaMapa);

                webMapa.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "artemusa.local",
                    carpetaMapa,
                    Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);

                webMapa.CoreWebView2.Navigate("https://artemusa.local/mapa.html");
            }
            catch (Exception ex)
            {
                if (lblEstadoMapa != null)
                    lblEstadoMapa.Text = "Error al cargar mapa";
                System.Diagnostics.Debug.WriteLine("Error mapa: " + ex.Message);
            }
        }

        // ==========================================
        // RECIBIR MENSAJES DEL MAPA
        // ==========================================
        private void RecibirMensajeMapa(object sender,
    Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            string mensaje;
            try { mensaje = e.TryGetWebMessageAsString(); }
            catch { return; }

            System.Diagnostics.Debug.WriteLine("MENSAJE MAPA: " + mensaje);

            if (mensaje == "mapa_listo")
            {
                mapaListo = true;
                if (lblEstadoMapa != null)
                    lblEstadoMapa.Text = "Mapa listo. Elige un lugar y búscalo.";
            }
            else if (mensaje.StartsWith("ubicacion|"))
            {
                string resto = mensaje.Substring("ubicacion|".Length);
                string[] partes = resto.Split('|');

                string direccion = partes.Length > 0 ? partes[0] : "";
                string localidad = partes.Length > 1 ? partes[1] : "";
                string nombre = partes.Length > 2 ? partes[2] : "";

                if (lugarSeleccionado == null)
                {
                    if (lblEstadoMapa != null)
                        lblEstadoMapa.Text = "⚠ Primero haz clic en un botón 📍";
                    return;
                }

                string valor = "";

                switch (campoActivoParaMapa)
                {
                    case "nombre":
                        valor = !string.IsNullOrWhiteSpace(nombre) ? nombre : direccion;
                        if (!string.IsNullOrWhiteSpace(valor))
                        {
                            lugarSeleccionado.Nombre = valor;
                            ActualizarTextBox(lugarSeleccionado.Panel, "txtNombreLugar", valor);
                        }
                        break;

                    case "direccion":
                        valor = direccion;
                        if (!string.IsNullOrWhiteSpace(valor))
                        {
                            lugarSeleccionado.Direccion = valor;
                            ActualizarTextBox(lugarSeleccionado.Panel, "txtDireccion", valor);
                        }
                        break;

                    case "referencia":
                        valor = localidad;
                        if (!string.IsNullOrWhiteSpace(valor))
                        {
                            lugarSeleccionado.Referencia = valor;
                            ActualizarTextBox(lugarSeleccionado.Panel, "txtReferencia", valor);
                        }
                        break;
                }

                if (!string.IsNullOrWhiteSpace(valor))
                {
                    if (lblEstadoMapa != null)
                        lblEstadoMapa.Text = "✅ " + campoActivoParaMapa + ": " + valor;
                }
                else
                {
                    if (lblEstadoMapa != null)
                        lblEstadoMapa.Text = "⚠ No se obtuvo el dato esperado.";
                }
            }
            else if (mensaje.StartsWith("estado|"))
            {
                if (lblEstadoMapa != null)
                    lblEstadoMapa.Text = mensaje.Substring("estado|".Length);
            }
        }

        private void ActualizarTextBox(Panel panelCard, string nombreTb, string valor)
        {
            if (panelCard == null) return;
            foreach (Control c in panelCard.Controls)
            {
                if (c is TextBox tb && tb.Name == nombreTb)
                {
                    tb.Text = valor;
                    break;
                }
            }
        }

        // ==========================================
        // PASO 3: PERSONAL
        // ==========================================
        private void MostrarPaso3_Personal()
        {
            Label titulo = new Label
            {
                Text = "③  PERSONAL DEL EVENTO",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(0, 0),
                AutoSize = true
            };
            panelContenido.Controls.Add(titulo);

            Label subtitulo = new Label
            {
                Text = "Selecciona quién estará a cargo y quiénes participarán del evento.",
                Font = new Font("Segoe UI", 9),
                ForeColor = gris,
                Location = new Point(0, 35),
                AutoSize = true
            };
            panelContenido.Controls.Add(subtitulo);

            var trabajadores = new List<TrabajadorItem>();
            var externos = new List<PersonalExternoItem>();

            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();

                string qTrab = @"
                    SELECT t.id_trabajador, t.nombre, r.nombre_rol
                    FROM trabajadores t
                    LEFT JOIN roles r ON r.id_rol = t.id_rol
                    WHERE t.activo = true
                    ORDER BY t.nombre";
                using (NpgsqlCommand cmd = new NpgsqlCommand(qTrab, con))
                using (NpgsqlDataReader rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        trabajadores.Add(new TrabajadorItem
                        {
                            Id = Convert.ToInt32(rd["id_trabajador"]),
                            Nombre = rd["nombre"].ToString(),
                            Rol = rd["nombre_rol"]?.ToString() ?? "Sin rol"
                        });
                    }
                }

                string qExt = @"
                    SELECT id_personal, nombre, apellido, celular
                    FROM personal_externo
                    WHERE activo = true
                    ORDER BY nombre, apellido";
                using (NpgsqlCommand cmd = new NpgsqlCommand(qExt, con))
                using (NpgsqlDataReader rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        externos.Add(new PersonalExternoItem
                        {
                            Id = Convert.ToInt32(rd["id_personal"]),
                            Nombre = rd["nombre"].ToString() + " " + rd["apellido"].ToString(),
                            Celular = rd["celular"]?.ToString() ?? ""
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar personal:\n" + ex.Message);
                return;
            }
            finally
            {
                conexionDB.CerrarConexion();
            }

            Panel panelScroll = new Panel
            {
                Location = new Point(0, 75),
                Size = new Size(1180, 580),
                AutoScroll = true,
                BackColor = fondo,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            panelContenido.Controls.Add(panelScroll);

            // ENCARGADO
            Panel cardEnc = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(1150, 100),
                BackColor = tarjeta,
                Padding = new Padding(20)
            };
            panelScroll.Controls.Add(cardEnc);

            cardEnc.Controls.Add(new Label
            {
                Text = "ENCARGADO PRINCIPAL *",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = morado,
                Location = new Point(20, 15),
                AutoSize = true
            });

            ComboBox cboEncargado = new ComboBox
            {
                Location = new Point(20, 45),
                Size = new Size(600, 32),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = campo,
                ForeColor = blanco,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10)
            };
            foreach (var t in trabajadores)
                cboEncargado.Items.Add($"{t.Nombre} ({t.Rol})");

            for (int i = 0; i < trabajadores.Count; i++)
            {
                if (trabajadores[i].Id == idTrabajador)
                {
                    cboEncargado.SelectedIndex = i;
                    break;
                }
            }
            if (cboEncargado.SelectedIndex < 0 && cboEncargado.Items.Count > 0)
                cboEncargado.SelectedIndex = 0;

            cboEncargado.SelectedIndexChanged += (s, e) =>
            {
                if (cboEncargado.SelectedIndex >= 0)
                {
                    idEncargadoSeleccionado = trabajadores[cboEncargado.SelectedIndex].Id;
                    RefrescarSubTrabajadores(panelScroll, trabajadores);
                }
            };
            cardEnc.Controls.Add(cboEncargado);

            if (cboEncargado.SelectedIndex >= 0)
                idEncargadoSeleccionado = trabajadores[cboEncargado.SelectedIndex].Id;

            // SUBS
            Panel cardSub = new Panel
            {
                Location = new Point(0, 115),
                Size = new Size(1150, 220),
                BackColor = tarjeta,
                Padding = new Padding(20),
                Name = "cardSub"
            };
            panelScroll.Controls.Add(cardSub);

            cardSub.Controls.Add(new Label
            {
                Text = "TRABAJADORES DEL SISTEMA (sub-trabajadores)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = morado,
                Location = new Point(20, 15),
                AutoSize = true
            });

            FlowLayoutPanel flpSubs = new FlowLayoutPanel
            {
                Location = new Point(20, 45),
                Size = new Size(1100, 160),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = true,
                BackColor = tarjeta,
                Name = "flpSubs"
            };
            cardSub.Controls.Add(flpSubs);

            RefrescarSubTrabajadores(panelScroll, trabajadores);

            // EXTERNOS
            Panel cardExt = new Panel
            {
                Location = new Point(0, 350),
                Size = new Size(1150, 240),
                BackColor = tarjeta,
                Padding = new Padding(20)
            };
            panelScroll.Controls.Add(cardExt);

            cardExt.Controls.Add(new Label
            {
                Text = "PERSONAL EXTERNO",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = morado,
                Location = new Point(20, 15),
                AutoSize = true
            });

            Button btnAddExt = new Button
            {
                Text = "+  AGREGAR PERSONAL EXTERNO",
                Width = 280,
                Height = 35,
                Location = new Point(820, 12),
                FlatStyle = FlatStyle.Flat,
                BackColor = morado,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddExt.FlatAppearance.BorderSize = 0;
            btnAddExt.Click += (s, e) =>
            {
                using (var f = new FormAgregarPersonalExterno())
                {
                    if (f.ShowDialog() == DialogResult.OK)
                    {
                        MostrarPaso();
                    }
                }
            };
            cardExt.Controls.Add(btnAddExt);

            FlowLayoutPanel flpExt = new FlowLayoutPanel
            {
                Location = new Point(20, 55),
                Size = new Size(1100, 170),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = true,
                BackColor = tarjeta
            };
            cardExt.Controls.Add(flpExt);

            foreach (var ext in externos)
            {
                CheckBox chk = new CheckBox
                {
                    Text = (ext.Celular != "" ? "📱 " + ext.Celular + "  |  " : "") + ext.Nombre,
                    Font = new Font("Segoe UI", 9.5f),
                    ForeColor = blanco,
                    Width = 350,
                    Height = 28,
                    Margin = new Padding(0, 4, 20, 4),
                    Checked = idsPersonalExterno.Contains(ext.Id),
                    Cursor = Cursors.Hand
                };
                int idExt = ext.Id;
                chk.CheckedChanged += (s, e) =>
                {
                    if (chk.Checked)
                    {
                        if (!idsPersonalExterno.Contains(idExt))
                            idsPersonalExterno.Add(idExt);
                    }
                    else idsPersonalExterno.Remove(idExt);
                };
                flpExt.Controls.Add(chk);
            }

            if (externos.Count == 0)
            {
                flpExt.Controls.Add(new Label
                {
                    Text = "No hay personal externo aún.",
                    Font = new Font("Segoe UI", 9, FontStyle.Italic),
                    ForeColor = gris,
                    AutoSize = true
                });
            }
        }

        private void RefrescarSubTrabajadores(Panel panelScroll, List<TrabajadorItem> trabajadores)
        {
            var cardSub = panelScroll.Controls["cardSub"] as Panel;
            if (cardSub == null) return;
            var flpSubs = cardSub.Controls["flpSubs"] as FlowLayoutPanel;
            if (flpSubs == null) return;

            flpSubs.Controls.Clear();

            foreach (var t in trabajadores)
            {
                if (t.Id == idEncargadoSeleccionado) continue;

                CheckBox chk = new CheckBox
                {
                    Text = $"{t.Nombre} ({t.Rol})",
                    Font = new Font("Segoe UI", 9.5f),
                    ForeColor = blanco,
                    Width = 350,
                    Height = 28,
                    Margin = new Padding(0, 4, 20, 4),
                    Checked = idsSubTrabajadores.Contains(t.Id),
                    Cursor = Cursors.Hand
                };
                int idTrab = t.Id;
                chk.CheckedChanged += (s, e) =>
                {
                    if (chk.Checked)
                    {
                        if (!idsSubTrabajadores.Contains(idTrab))
                            idsSubTrabajadores.Add(idTrab);
                    }
                    else idsSubTrabajadores.Remove(idTrab);
                };
                flpSubs.Controls.Add(chk);
            }
        }

        private bool ValidarPaso3()
        {
            if (idEncargadoSeleccionado == 0)
            {
                MessageBox.Show("Selecciona un encargado principal.");
                return false;
            }
            return true;
        }

        // ==========================================
        // PASO 4: EQUIPOS
        // ==========================================
        private void MostrarPaso4_Equipos()
        {
            Label titulo = new Label
            {
                Text = "④  EQUIPOS DEL EVENTO",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(0, 0),
                AutoSize = true
            };
            panelContenido.Controls.Add(titulo);

            Label subtitulo = new Label
            {
                Text = "Agrega equipos por código de barras y asígnalos a un lugar y responsable.",
                Font = new Font("Segoe UI", 9),
                ForeColor = gris,
                Location = new Point(0, 35),
                AutoSize = true
            };
            panelContenido.Controls.Add(subtitulo);

            Panel panelAdd = new Panel
            {
                Location = new Point(0, 75),
                Size = new Size(1180, 70),
                BackColor = tarjeta,
                Padding = new Padding(15)
            };
            panelContenido.Controls.Add(panelAdd);

            panelAdd.Controls.Add(new Label
            {
                Text = "Código:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(15, 22),
                AutoSize = true
            });

            TextBox txtCodigo = new TextBox
            {
                Location = new Point(90, 18),
                Size = new Size(450, 30),
                BackColor = campo,
                ForeColor = blanco,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            panelAdd.Controls.Add(txtCodigo);

            Button btnAdd = new Button
            {
                Text = "+  AGREGAR EQUIPO",
                Width = 200,
                Height = 32,
                Location = new Point(555, 17),
                FlatStyle = FlatStyle.Flat,
                BackColor = morado,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize = 0;

            Action agregarEquipo = () =>
            {
                string cod = txtCodigo.Text.Trim();
                if (string.IsNullOrWhiteSpace(cod))
                {
                    MessageBox.Show("Ingresa un código.");
                    return;
                }
                AgregarEquipoPorCodigo(cod);
                txtCodigo.Clear();
                txtCodigo.Focus();
            };

            btnAdd.Click += (s, e) => agregarEquipo();
            txtCodigo.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    agregarEquipo();
                }
            };
            panelAdd.Controls.Add(btnAdd);

            FlowLayoutPanel flpEquipos = new FlowLayoutPanel
            {
                Location = new Point(0, 155),
                Size = new Size(1180, 500),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = fondo,
                Name = "flpEquipos",
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            panelContenido.Controls.Add(flpEquipos);

            foreach (var eq in equiposAsignados)
                CrearTarjetaEquipoUI(eq);
        }

        private void AgregarEquipoPorCodigo(string codigo)
        {
            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();
                string q = "SELECT id, codigo, nombre FROM equipos WHERE codigo = @codigo";

                using (NpgsqlCommand cmd = new NpgsqlCommand(q, con))
                {
                    cmd.Parameters.AddWithValue("@codigo", codigo);
                    using (NpgsqlDataReader rd = cmd.ExecuteReader())
                    {
                        if (!rd.Read())
                        {
                            MessageBox.Show("No se encontró ese código.");
                            return;
                        }

                        int idEq = Convert.ToInt32(rd["id"]);
                        string cod = rd["codigo"].ToString();
                        string nom = rd["nombre"].ToString();

                        foreach (var e in equiposAsignados)
                        {
                            if (e.IdEquipo == idEq)
                            {
                                MessageBox.Show("Este equipo ya fue agregado.");
                                return;
                            }
                        }

                        var equipo = new EquipoAsignado
                        {
                            IdEquipo = idEq,
                            Codigo = cod,
                            Nombre = nom
                        };
                        equiposAsignados.Add(equipo);
                        CrearTarjetaEquipoUI(equipo);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }
        }

        private void CrearTarjetaEquipoUI(EquipoAsignado eq)
        {
            var flp = panelContenido.Controls["flpEquipos"] as FlowLayoutPanel;
            if (flp == null) return;

            Panel card = new Panel
            {
                Width = 1150,
                Height = 130,
                BackColor = tarjeta,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(20)
            };

            card.Controls.Add(new Label
            {
                Text = eq.Codigo,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = morado,
                Location = new Point(20, 15),
                AutoSize = true
            });

            card.Controls.Add(new Label
            {
                Text = eq.Nombre,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(20, 40),
                AutoSize = true
            });

            card.Controls.Add(new Label
            {
                Text = "Lugar:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(20, 75),
                AutoSize = true
            });

            ComboBox cboLugar = new ComboBox
            {
                Location = new Point(75, 72),
                Size = new Size(400, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = campo,
                ForeColor = blanco,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            for (int i = 0; i < lugares.Count; i++)
                cboLugar.Items.Add(lugares[i].Nombre);
            if (cboLugar.Items.Count > 0) cboLugar.SelectedIndex = 0;
            cboLugar.SelectedIndexChanged += (s, e) => eq.IdLugar = cboLugar.SelectedIndex;
            card.Controls.Add(cboLugar);

            card.Controls.Add(new Label
            {
                Text = "Responsable:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(510, 75),
                AutoSize = true
            });

            ComboBox cboResp = new ComboBox
            {
                Location = new Point(610, 72),
                Size = new Size(400, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = campo,
                ForeColor = blanco,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };

            CargarResponsables(cboResp);
            cboResp.SelectedIndexChanged += (s, e) =>
            {
                if (cboResp.SelectedItem is ResponsableItem ri)
                {
                    eq.ResponsableTipo = ri.Tipo;
                    eq.ResponsableId = ri.Id;
                    eq.ResponsableNombre = ri.Nombre;
                }
            };
            card.Controls.Add(cboResp);

            Button btnQ = new Button
            {
                Text = "✗ QUITAR",
                Width = 110,
                Height = 30,
                Location = new Point(1020, 15),
                FlatStyle = FlatStyle.Flat,
                BackColor = rojo,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnQ.FlatAppearance.BorderSize = 0;
            btnQ.Click += (s, e) =>
            {
                equiposAsignados.Remove(eq);
                flp.Controls.Remove(card);
            };
            card.Controls.Add(btnQ);

            eq.Panel = card;
            flp.Controls.Add(card);
        }

        private void CargarResponsables(ComboBox cbo)
        {
            cbo.Items.Clear();

            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();

                if (idEncargadoSeleccionado > 0)
                {
                    using (NpgsqlCommand cmd = new NpgsqlCommand(
                        "SELECT nombre FROM trabajadores WHERE id_trabajador = @id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", idEncargadoSeleccionado);
                        object r = cmd.ExecuteScalar();
                        if (r != null)
                            cbo.Items.Add(new ResponsableItem
                            {
                                Id = idEncargadoSeleccionado,
                                Nombre = r.ToString() + " (Encargado)",
                                Tipo = "T"
                            });
                    }
                }

                foreach (int idSub in idsSubTrabajadores)
                {
                    using (NpgsqlCommand cmd = new NpgsqlCommand(
                        "SELECT nombre FROM trabajadores WHERE id_trabajador = @id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", idSub);
                        object r = cmd.ExecuteScalar();
                        if (r != null)
                            cbo.Items.Add(new ResponsableItem
                            {
                                Id = idSub,
                                Nombre = r.ToString(),
                                Tipo = "T"
                            });
                    }
                }

                foreach (int idExt in idsPersonalExterno)
                {
                    using (NpgsqlCommand cmd = new NpgsqlCommand(
                        "SELECT nombre || ' ' || apellido FROM personal_externo WHERE id_personal = @id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", idExt);
                        object r = cmd.ExecuteScalar();
                        if (r != null)
                            cbo.Items.Add(new ResponsableItem
                            {
                                Id = idExt,
                                Nombre = r.ToString() + " (externo)",
                                Tipo = "E"
                            });
                    }
                }
            }
            catch { }
            finally
            {
                conexionDB.CerrarConexion();
            }

            if (cbo.Items.Count > 0) cbo.SelectedIndex = 0;
        }

        private bool ValidarPaso4()
        {
            if (equiposAsignados.Count == 0)
            {
                MessageBox.Show("Agrega al menos un equipo.");
                return false;
            }
            foreach (var eq in equiposAsignados)
            {
                if (eq.IdLugar == null)
                {
                    MessageBox.Show("El equipo " + eq.Codigo + " no tiene lugar.");
                    return false;
                }
                if (eq.ResponsableId == 0)
                {
                    MessageBox.Show("El equipo " + eq.Codigo + " no tiene responsable.");
                    return false;
                }
            }
            return true;
        }

        // ==========================================
        // PASO 5: RESUMEN
        // ==========================================
        private void MostrarPaso5_Resumen()
        {
            Label titulo = new Label
            {
                Text = "⑤  RESUMEN DEL EVENTO",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(0, 0),
                AutoSize = true
            };
            panelContenido.Controls.Add(titulo);

            Label subtitulo = new Label
            {
                Text = "Revisa toda la información antes de guardar.",
                Font = new Font("Segoe UI", 9),
                ForeColor = gris,
                Location = new Point(0, 35),
                AutoSize = true
            };
            panelContenido.Controls.Add(subtitulo);

            Panel panelResumen = new Panel
            {
                Location = new Point(0, 75),
                Size = new Size(1180, 600),
                AutoScroll = true,
                BackColor = fondo,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            panelContenido.Controls.Add(panelResumen);

            int y = 0;

            y = AgregarSeccionResumen(panelResumen, "📋  EVENTO", y);
            y = AgregarLineaResumen(panelResumen, "Nombre:", datosEvento.Nombre, y);
            y = AgregarLineaResumen(panelResumen, "Tipo:", datosEvento.TipoEvento, y);
            y = AgregarLineaResumen(panelResumen, "Inicio:",
                datosEvento.FechaInicio.ToString("dd/MM/yyyy") + " " +
                datosEvento.HoraInicio.ToString("HH:mm"), y);
            y = AgregarLineaResumen(panelResumen, "Fin:",
                datosEvento.FechaFin.ToString("dd/MM/yyyy") + " " +
                datosEvento.HoraFin.ToString("HH:mm"), y);
            if (!string.IsNullOrWhiteSpace(datosEvento.Descripcion))
                y = AgregarLineaResumen(panelResumen, "Descripción:", datosEvento.Descripcion, y);
            y += 15;

            y = AgregarSeccionResumen(panelResumen, "📍  LUGARES (" + lugares.Count + ")", y);
            foreach (var l in lugares)
            {
                string texto = "• " + l.Nombre;
                if (!string.IsNullOrWhiteSpace(l.Direccion))
                    texto += "  →  " + l.Direccion;
                y = AgregarLineaResumen(panelResumen, "", texto, y);
            }
            y += 15;

            y = AgregarSeccionResumen(panelResumen, "👥  PERSONAL", y);
            y = AgregarLineaResumen(panelResumen, "Encargado:",
                ObtenerNombreTrabajador(idEncargadoSeleccionado), y);

            if (idsSubTrabajadores.Count > 0)
            {
                string subs = "";
                foreach (int id in idsSubTrabajadores)
                {
                    string nombre = ObtenerNombreTrabajador(id);
                    if (!string.IsNullOrWhiteSpace(nombre))
                        subs += nombre + ", ";
                }
                if (subs.Length > 2) subs = subs.TrimEnd(',', ' ');
                y = AgregarLineaResumen(panelResumen, "Sub-trabajadores:", subs, y);
            }

            if (idsPersonalExterno.Count > 0)
            {
                string ext = "";
                foreach (int id in idsPersonalExterno)
                {
                    string nombre = ObtenerNombreExterno(id);
                    if (!string.IsNullOrWhiteSpace(nombre))
                        ext += nombre + ", ";
                }
                if (ext.Length > 2) ext = ext.TrimEnd(',', ' ');
                y = AgregarLineaResumen(panelResumen, "Personal externo:", ext, y);
            }
            y += 15;

            y = AgregarSeccionResumen(panelResumen, "🎥  EQUIPOS (" + equiposAsignados.Count + ")", y);
            foreach (var eq in equiposAsignados)
            {
                string lugarNombre = eq.IdLugar.HasValue && eq.IdLugar.Value < lugares.Count
                    ? lugares[eq.IdLugar.Value].Nombre
                    : "(sin lugar)";

                string texto = "• " + eq.Codigo + " - " + eq.Nombre +
                               "  →  " + lugarNombre +
                               "  |  " + eq.ResponsableNombre;
                y = AgregarLineaResumen(panelResumen, "", texto, y);
            }
        }

        private int AgregarSeccionResumen(Panel panel, string titulo, int y)
        {
            Label lbl = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = morado,
                Location = new Point(0, y),
                AutoSize = true
            };
            panel.Controls.Add(lbl);
            return y + 32;
        }

        private int AgregarLineaResumen(Panel panel, string etiqueta, string valor, int y)
        {
            if (!string.IsNullOrEmpty(etiqueta))
            {
                Label lblE = new Label
                {
                    Text = etiqueta,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = gris,
                    Location = new Point(20, y),
                    Size = new Size(150, 22)
                };
                panel.Controls.Add(lblE);
            }

            Label lblV = new Label
            {
                Text = valor,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = blanco,
                Location = new Point(string.IsNullOrEmpty(etiqueta) ? 20 : 180, y),
                Size = new Size(940, 22)
            };
            panel.Controls.Add(lblV);

            return y + 26;
        }

        private string ObtenerNombreTrabajador(int id)
        {
            if (id == 0) return "";
            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();
                using (NpgsqlCommand cmd = new NpgsqlCommand(
                    "SELECT nombre FROM trabajadores WHERE id_trabajador = @id", con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    object r = cmd.ExecuteScalar();
                    return r?.ToString() ?? "";
                }
            }
            catch { return ""; }
            finally { conexionDB.CerrarConexion(); }
        }

        private string ObtenerNombreExterno(int id)
        {
            if (id == 0) return "";
            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();
                using (NpgsqlCommand cmd = new NpgsqlCommand(
                    "SELECT nombre || ' ' || apellido FROM personal_externo WHERE id_personal = @id", con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    object r = cmd.ExecuteScalar();
                    return r?.ToString() ?? "";
                }
            }
            catch { return ""; }
            finally { conexionDB.CerrarConexion(); }
        }

        // ==========================================
        // GUARDAR TODO
        // ==========================================
        private void GuardarTodo()
        {
            if (MessageBox.Show(
                "¿Confirmas guardar el evento con todos los datos?",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                != DialogResult.Yes)
                return;

            btnSiguiente.Enabled = false;
            btnSiguiente.Text = "Guardando...";

            Conexion conexionDB = new Conexion();
            NpgsqlTransaction transaccion = null;

            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();
                transaccion = con.BeginTransaction();

                int idEvento = 0;
                string sqlEvento = @"
                    INSERT INTO eventos
                    (fecha, titulo, horario, detalles, hora_inicio, hora_fin, tipo_evento, id_trabajador)
                    VALUES
                    (@fecha, @titulo, @horario, @detalles, @horaInicio, @horaFin, @tipo, @trabajador)
                    RETURNING id";

                using (NpgsqlCommand cmd = new NpgsqlCommand(sqlEvento, con, transaccion))
                {
                    cmd.Parameters.AddWithValue("@fecha", datosEvento.FechaInicio.Date);
                    cmd.Parameters.AddWithValue("@titulo", datosEvento.Nombre);
                    cmd.Parameters.AddWithValue("@horario",
                        datosEvento.HoraInicio.ToString("HH:mm") + " - " + datosEvento.HoraFin.ToString("HH:mm"));
                    cmd.Parameters.AddWithValue("@detalles",
                        string.IsNullOrWhiteSpace(datosEvento.Descripcion)
                            ? (object)DBNull.Value
                            : datosEvento.Descripcion);
                    cmd.Parameters.AddWithValue("@horaInicio", datosEvento.HoraInicio.TimeOfDay);
                    cmd.Parameters.AddWithValue("@horaFin", datosEvento.HoraFin.TimeOfDay);
                    cmd.Parameters.AddWithValue("@tipo", datosEvento.TipoEvento);
                    cmd.Parameters.AddWithValue("@trabajador", idEncargadoSeleccionado);

                    idEvento = Convert.ToInt32(cmd.ExecuteScalar());
                }

                int idSalida = 0;
                string sqlSalida = @"
                    INSERT INTO salidas
                    (id_trabajador, encargado, fecha_salida, ubicacion, detalles, estado, id_evento)
                    VALUES
                    (@trabajador, @encargado, @fecha, @ubicacion, @detalles, 'Programada', @idEvento)
                    RETURNING id_salida";

                string encargadoNombre = "";
                using (NpgsqlCommand cmdN = new NpgsqlCommand(
                    "SELECT nombre FROM trabajadores WHERE id_trabajador = @id", con, transaccion))
                {
                    cmdN.Parameters.AddWithValue("@id", idEncargadoSeleccionado);
                    object r = cmdN.ExecuteScalar();
                    encargadoNombre = r?.ToString() ?? "";
                }
                string ubicacionResumen = lugares.Count > 0 ? lugares[0].Nombre : "";
                if (lugares.Count > 1) ubicacionResumen += " (+" + (lugares.Count - 1) + " más)";

                using (NpgsqlCommand cmd = new NpgsqlCommand(sqlSalida, con, transaccion))
                {
                    cmd.Parameters.AddWithValue("@trabajador", idTrabajador);
                    cmd.Parameters.AddWithValue("@encargado", encargadoNombre);
                    cmd.Parameters.AddWithValue("@fecha", datosEvento.FechaInicio.Date);
                    cmd.Parameters.AddWithValue("@ubicacion", ubicacionResumen);
                    cmd.Parameters.AddWithValue("@detalles",
                        string.IsNullOrWhiteSpace(datosEvento.Descripcion)
                            ? (object)DBNull.Value
                            : datosEvento.Descripcion);
                    cmd.Parameters.AddWithValue("@idEvento", idEvento);

                    idSalida = Convert.ToInt32(cmd.ExecuteScalar());
                }

                foreach (var l in lugares)
                {
                    string sqlLugar = @"
                        INSERT INTO lugares_evento
                        (id_evento, nombre_lugar, direccion, referencia)
                        VALUES (@evento, @nombre, @direccion, @referencia)";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(sqlLugar, con, transaccion))
                    {
                        cmd.Parameters.AddWithValue("@evento", idEvento);
                        cmd.Parameters.AddWithValue("@nombre", l.Nombre);
                        cmd.Parameters.AddWithValue("@direccion",
                            string.IsNullOrWhiteSpace(l.Direccion) ? (object)DBNull.Value : l.Direccion);
                        cmd.Parameters.AddWithValue("@referencia",
                            string.IsNullOrWhiteSpace(l.Referencia) ? (object)DBNull.Value : l.Referencia);
                        cmd.ExecuteNonQuery();
                    }
                }

                foreach (var eq in equiposAsignados)
                {
                    string sqlSE = @"
                        INSERT INTO salida_equipos (id_salida, id_equipo)
                        VALUES (@salida, @equipo)";
                    using (NpgsqlCommand cmd = new NpgsqlCommand(sqlSE, con, transaccion))
                    {
                        cmd.Parameters.AddWithValue("@salida", idSalida);
                        cmd.Parameters.AddWithValue("@equipo", eq.IdEquipo);
                        cmd.ExecuteNonQuery();
                    }
                }

                foreach (var eq in equiposAsignados)
                {
                    string sqlAsig = @"
                        INSERT INTO asignaciones_equipos
                        (id_evento, id_lugar, id_equipo, id_trabajador, id_personal_externo)
                        VALUES (@evento, @lugar, @equipo, @trab, @ext)";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(sqlAsig, con, transaccion))
                    {
                        cmd.Parameters.AddWithValue("@evento", idEvento);
                        cmd.Parameters.AddWithValue("@lugar", DBNull.Value);
                        cmd.Parameters.AddWithValue("@equipo", eq.IdEquipo);

                        if (eq.ResponsableTipo == "T")
                        {
                            cmd.Parameters.AddWithValue("@trab", eq.ResponsableId);
                            cmd.Parameters.AddWithValue("@ext", DBNull.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@trab", DBNull.Value);
                            cmd.Parameters.AddWithValue("@ext", eq.ResponsableId);
                        }
                        cmd.ExecuteNonQuery();
                    }
                }

                transaccion.Commit();

                MessageBox.Show(
                    "¡Evento registrado correctamente!\n\n" +
                    "Se guardó el evento, la salida, los lugares, los equipos y las asignaciones.",
                    "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                if (transaccion != null)
                {
                    try { transaccion.Rollback(); } catch { }
                }

                string msg = ex.Message;
                if (ex.InnerException != null)
                    msg += "\n\nDetalle: " + ex.InnerException.Message;

                MessageBox.Show("Error al guardar:\n" + msg,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                btnSiguiente.Enabled = true;
                btnSiguiente.Text = "✓  GUARDAR TODO";
            }
            finally
            {
                conexionDB.CerrarConexion();
            }
        }
    }
}