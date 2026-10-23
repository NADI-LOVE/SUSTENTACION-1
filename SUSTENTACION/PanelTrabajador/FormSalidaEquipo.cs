using Npgsql;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Linq;

namespace SUSTENTACION.PanelTrabajador
{
    public class FormSalidaEquipo : Form
    {
        // ==========================================
        // PALETA PREMIUM PÚRPURA
        // ==========================================
        private readonly Color fondo = Color.FromArgb(10, 9, 18);
        private readonly Color tarjeta = Color.FromArgb(30, 28, 48);
        private readonly Color campo = Color.FromArgb(45, 42, 68);
        private readonly Color tarjetaHover = Color.FromArgb(35, 34, 50);
        private readonly Color campoFocus = Color.FromArgb(45, 44, 70);
        private readonly Color morado = Color.FromArgb(124, 92, 255);
        private readonly Color moradoHover = Color.FromArgb(145, 118, 255);
        private readonly Color moradoSuave = Color.FromArgb(50, 40, 90);
        private readonly Color blanco = Color.FromArgb(245, 243, 255);
        private readonly Color gris = Color.FromArgb(150, 148, 175);
        private readonly Color grisOscuro = Color.FromArgb(80, 78, 100);
        private readonly Color verde = Color.FromArgb(46, 204, 148);
        private readonly Color verdeHover = Color.FromArgb(60, 220, 165);
        private readonly Color naranja = Color.FromArgb(255, 145, 80);
        private readonly Color rojo = Color.FromArgb(240, 90, 110);
        private readonly Color amarillo = Color.FromArgb(255, 210, 100);
        private readonly Color azul = Color.FromArgb(90, 160, 255);

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

        // ==========================================
        // AUTOCOMPLETADO EQUIPOS
        // ==========================================
        private ListBox lstSugerencias;
        private Panel panelSugerenciasContenedor;
        private TextBox txtCodigoActivo;
        private System.Windows.Forms.Timer timerBusqueda;
        private List<EquipoSugerencia> sugerenciasActuales = new List<EquipoSugerencia>();

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
            public string Categoria { get; set; } = "";
            public string Estado { get; set; } = "";
            public int Stock { get; set; }
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

        private class EquipoSugerencia
        {
            public int Id { get; set; }
            public string Codigo { get; set; } = "";
            public string Nombre { get; set; } = "";
            public string Categoria { get; set; } = "";
            public string Estado { get; set; } = "";
            public int Stock { get; set; }
            public override string ToString() =>
                $"{(string.IsNullOrEmpty(Codigo) ? "(sin código)" : Codigo)}  —  {Nombre}   [{Stock} disp.]";
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

            // ==========================================
            // ÍCONO PERSONALIZADO DE ARTEMUSA
            // ==========================================
            string rutaIcono = System.IO.Path.Combine(
                AppContext.BaseDirectory,
                "logo_artemusa.ico"
            );

            if (System.IO.File.Exists(rutaIcono))
            {
                this.Icon = new Icon(rutaIcono);
            }

            this.ShowIcon = true;

            // ==========================================
            // AJUSTAR EL MAPA AL REDIMENSIONAR
            // ==========================================
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
            this.Text = "ARTEMUSA | Registrar salida de equipos audiovisuales";
            this.Size = new Size(1400, 900);
            this.MinimumSize = new Size(1200, 800);
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = fondo;
            this.Font = new Font("Segoe UI", 9);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;

            panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 142,
                BackColor = fondo
            };
            this.Controls.Add(panelHeader);

            Panel lineaMorada = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 2,
                BackColor = morado
            };
            panelHeader.Controls.Add(lineaMorada);

            Label titulo = new Label
            {
                Text = "Registrar salida de equipos audiovisuales",
                Font = new Font("Segoe UI", 19, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(24, 15),
                AutoSize = true
            };
            panelHeader.Controls.Add(titulo);

            string[] nombresPasos = { "Evento", "Lugares", "Personal", "Equipos", "Resumen" };
            int anchoPaso = 225;
            int xInicial = 24;
            int yPaso = 79;

            for (int i = 0; i < TOTAL_PASOS; i++)
            {
                int numPaso = i + 1;
                int x = xInicial + (i * anchoPaso);

                Label lblNum = new Label
                {
                    Text = numPaso.ToString(),
                    Font = new Font("Segoe UI", 11, FontStyle.Bold),
                    ForeColor = blanco,
                    BackColor = Color.FromArgb(45, 55, 85),
                    Size = new Size(40, 40),
                    Location = new Point(x, yPaso),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                AplicarBordeRedondeado(lblNum, 20);
                panelHeader.Controls.Add(lblNum);

                Label lblNombre = new Label
                {
                    Text = nombresPasos[i],
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = gris,
                    Location = new Point(x + 50, yPaso + 11),
                    AutoSize = true
                };
                panelHeader.Controls.Add(lblNombre);

                lblPasos[i] = lblNum;

                if (i < TOTAL_PASOS - 1)
                {
                    Panel linea = new Panel
                    {
                        BackColor = Color.FromArgb(45, 55, 85),
                        Size = new Size(anchoPaso - 56, 2),
                        Location = new Point(x + 45, yPaso + 19)
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
                Height = 80,
                BackColor = Color.FromArgb(18, 29, 48)
            };
            this.Controls.Add(panelFooter);
            panelFooter.BringToFront();

            btnAnterior = new Button
            {
                Text = "←  ANTERIOR",
                Width = 160,
                Height = 48,
                Location = new Point(24, 16),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 55, 85),
                ForeColor = blanco,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnAnterior.FlatAppearance.BorderSize = 0;
            btnAnterior.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 72, 105);
            btnAnterior.Click += (s, e) => AnteriorPaso();
            AplicarBordeRedondeado(btnAnterior, 8);
            panelFooter.Controls.Add(btnAnterior);

            btnSiguiente = new Button
            {
                Text = "SIGUIENTE  →",
                Width = 230,
                Height = 48,
                Location = new Point(1050, 16),
                FlatStyle = FlatStyle.Flat,
                BackColor = morado,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnSiguiente.FlatAppearance.BorderSize = 0;
            btnSiguiente.FlatAppearance.MouseOverBackColor = moradoHover;
            btnSiguiente.Click += (s, e) => SiguientePaso();
            AplicarBordeRedondeado(btnSiguiente, 8);
            panelFooter.Controls.Add(btnSiguiente);

            panelFooter.Resize += (s, e) =>
            {
                btnSiguiente.Left = panelFooter.Width - btnSiguiente.Width - 24;
            };
        }

        // ==========================================
        // BORDE REDONDEADO
        // ==========================================
        private void AplicarBordeRedondeado(Control control, int radio)
        {
            Action recalcular = () =>
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

            control.Resize += (s, e) => recalcular();
            recalcular();
        }

        // ==========================================
        // RECTÁNGULO REDONDEADO (helper gráfico)
        // ==========================================
        private GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            if (d <= 0) { path.AddRectangle(bounds); return path; }
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseAllFigures();
            return path;
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
                    lblPasos[i].BackColor = Color.FromArgb(45, 55, 85);
                    lblPasos[i].Text = numPaso.ToString();
                }
            }

            btnAnterior.Enabled = pasoActual > 1;

            if (pasoActual == TOTAL_PASOS)
            {
                btnSiguiente.Text = "✓  GUARDAR TODO";
                btnSiguiente.BackColor = verde;
                btnSiguiente.FlatAppearance.MouseOverBackColor = verdeHover;
            }
            else
            {
                btnSiguiente.Text = "SIGUIENTE  →";
                btnSiguiente.BackColor = morado;
                btnSiguiente.FlatAppearance.MouseOverBackColor = moradoHover;
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
        // PASO 1: DATOS DEL EVENTO — DISEÑO PREMIUM
        // ==========================================
        private void MostrarPaso1_Evento()
        {
            int anchoDisponible = panelContenido.ClientSize.Width;

            Label titulo = new Label
            {
                Text = "①  DATOS DEL EVENTO",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(0, 25),
                Size = new Size(anchoDisponible, 45),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panelContenido.Controls.Add(titulo);

            Label subtitulo = new Label
            {
                Text = "Completa la información del evento audiovisual",
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = gris,
                Location = new Point(0, 75),
                Size = new Size(anchoDisponible, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panelContenido.Controls.Add(subtitulo);

            Panel lineaDeco = new Panel
            {
                Size = new Size(120, 3),
                BackColor = morado,
                Location = new Point((anchoDisponible - 120) / 2, 108),
                Anchor = AnchorStyles.Top
            };
            AplicarBordeRedondeado(lineaDeco, 2);
            panelContenido.Controls.Add(lineaDeco);

            int anchoTotal = Math.Min(1300, anchoDisponible - 100);
            int gap = 24;
            int anchoCol = (anchoTotal - gap) / 2;
            int altoTarjeta = 480;
            int xInicio = (anchoDisponible - anchoTotal) / 2;
            if (xInicio < 30) xInicio = 30;
            int yInicio = 150;

            // ════════════════════════════════════════════
            // TARJETA IZQUIERDA: INFORMACIÓN BÁSICA
            // ════════════════════════════════════════════
            Panel cardIzq = CrearTarjetaPanel(xInicio, yInicio, anchoCol, altoTarjeta);
            cardIzq.Controls.Add(new Label { Text = "📋", Font = new Font("Segoe UI", 18), ForeColor = morado, Location = new Point(30, 26), Size = new Size(38, 38), TextAlign = ContentAlignment.MiddleCenter });
            cardIzq.Controls.Add(new Label { Text = "INFORMACIÓN BÁSICA", Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = morado, Location = new Point(78, 34), AutoSize = true });
            cardIzq.Controls.Add(new Panel { Location = new Point(30, 70), Size = new Size(anchoCol - 60, 1), BackColor = Color.FromArgb(60, 55, 90) });

            int xIzq = 30;
            int anchoCampoIzq = anchoCol - 60;
            int yIzq = 90;

            // ─── Nombre ───
            AgregarLabelConAsterisco(cardIzq, "Nombre del evento", xIzq, yIzq);
            Panel panelNombre = new Panel { Location = new Point(xIzq, yIzq + 28), Size = new Size(anchoCampoIzq, 44), BackColor = campo, Padding = new Padding(14, 0, 14, 0) };
            AplicarBordeRedondeado(panelNombre, 10);
            TextBox txtNombreEvento = new TextBox { Location = new Point(14, 11), Size = new Size(anchoCampoIzq - 28, 22), BackColor = campo, ForeColor = blanco, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10.5f), Text = datosEvento.Nombre };
            txtNombreEvento.TextChanged += (s, e) => datosEvento.Nombre = txtNombreEvento.Text;
            panelNombre.Controls.Add(txtNombreEvento);
            cardIzq.Controls.Add(panelNombre);
            yIzq += 88;

            // ─── Tipo de evento (Custom Dropdown) ───
            AgregarLabelConAsterisco(cardIzq, "Tipo de evento", xIzq, yIzq);
            Panel panelTipo = new Panel { Location = new Point(xIzq, yIzq + 28), Size = new Size(anchoCampoIzq, 44), BackColor = campo, Cursor = Cursors.Hand };
            AplicarBordeRedondeado(panelTipo, 10);

            Label lblTipo = new Label { Text = datosEvento.TipoEvento, Font = new Font("Segoe UI", 10.5f), ForeColor = blanco, Location = new Point(14, 12), AutoSize = true, BackColor = campo };
            Label lblFlechaTipo = new Label { Text = "▼", Font = new Font("Segoe UI", 8), ForeColor = gris, Location = new Point(anchoCampoIzq - 30, 15), AutoSize = true, BackColor = campo };
            ComboBox cboTipoReal = new ComboBox { Location = new Point(0, 0), Size = new Size(1, 1), DropDownStyle = ComboBoxStyle.DropDownList, Visible = false };
            cboTipoReal.Items.AddRange(new object[] { "Conferencia", "Taller", "Reunión", "Producción audiovisual", "Evento deportivo", "Evento corporativo", "Matrimonio", "Fiesta patronal", "Bautizo", "Cumpleaños", "Aniversario", "Graduación", "Evento religioso", "Concierto", "Yunza", "Carnaval", "Fiesta de la Candelaria", "Otro" });
            cboTipoReal.SelectedItem = datosEvento.TipoEvento;
            if (cboTipoReal.SelectedIndex < 0) cboTipoReal.SelectedIndex = 0;
            cboTipoReal.SelectedIndexChanged += (s, e) => { datosEvento.TipoEvento = cboTipoReal.SelectedItem?.ToString() ?? "Conferencia"; lblTipo.Text = datosEvento.TipoEvento; };

            panelTipo.Controls.Add(lblTipo);
            panelTipo.Controls.Add(lblFlechaTipo);
            panelTipo.Controls.Add(cboTipoReal);

            panelTipo.Click += (s, e) => cboTipoReal.DroppedDown = true;
            lblTipo.Click += (s, e) => cboTipoReal.DroppedDown = true;
            lblFlechaTipo.Click += (s, e) => cboTipoReal.DroppedDown = true;

            cardIzq.Controls.Add(panelTipo);
            yIzq += 88;

            // ─── Descripción ───
            cardIzq.Controls.Add(new Label { Text = "Descripción", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = blanco, Location = new Point(xIzq, yIzq), AutoSize = true });
            Panel panelDesc = new Panel { Location = new Point(xIzq, yIzq + 28), Size = new Size(anchoCampoIzq, 130), BackColor = campo, Padding = new Padding(14, 10, 14, 10) };
            AplicarBordeRedondeado(panelDesc, 10);
            TextBox txtDescripcionEvento = new TextBox { Location = new Point(14, 10), Size = new Size(anchoCampoIzq - 28, 110), Multiline = true, BackColor = campo, ForeColor = blanco, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10), ScrollBars = ScrollBars.Vertical, Text = datosEvento.Descripcion };
            txtDescripcionEvento.TextChanged += (s, e) => datosEvento.Descripcion = txtDescripcionEvento.Text;
            panelDesc.Controls.Add(txtDescripcionEvento);
            cardIzq.Controls.Add(panelDesc);

            panelContenido.Controls.Add(cardIzq);

            // ════════════════════════════════════════════
            // TARJETA DERECHA: FECHA Y HORA
            // ════════════════════════════════════════════
            Panel cardDer = CrearTarjetaPanel(xInicio + anchoCol + gap, yInicio, anchoCol, altoTarjeta);
            cardDer.Controls.Add(new Label { Text = "📅", Font = new Font("Segoe UI", 18), ForeColor = morado, Location = new Point(30, 26), Size = new Size(38, 38), TextAlign = ContentAlignment.MiddleCenter });
            cardDer.Controls.Add(new Label { Text = "FECHA Y HORA", Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = morado, Location = new Point(78, 34), AutoSize = true });
            cardDer.Controls.Add(new Panel { Location = new Point(30, 70), Size = new Size(anchoCol - 60, 1), BackColor = Color.FromArgb(60, 55, 90) });

            int xDer = 30;
            int anchoCampoDer = anchoCol - 60;
            int yDer = 90;

            // ─── Fecha inicio (BLOQUEADA) ───
            cardDer.Controls.Add(new Label { Text = "Fecha de inicio", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = blanco, Location = new Point(xDer, yDer), AutoSize = true });

            Panel panelFechaIni = new Panel { Location = new Point(xDer, yDer + 28), Size = new Size(anchoCampoDer, 44), BackColor = campo };
            AplicarBordeRedondeado(panelFechaIni, 10);

            Label lblFechaIni = new Label { Text = datosEvento.FechaInicio.ToString("dddd, dd 'de' MMMM 'de' yyyy"), ForeColor = gris, Font = new Font("Segoe UI", 10.5f), Location = new Point(14, 12), AutoSize = true, BackColor = campo };
            Label lblIcoCal = new Label { Text = "🔒", Font = new Font("Segoe UI", 11), ForeColor = morado, Location = new Point(anchoCampoDer - 35, 11), AutoSize = true, BackColor = campo };

            panelFechaIni.Controls.Add(lblFechaIni);
            panelFechaIni.Controls.Add(lblIcoCal);
            cardDer.Controls.Add(panelFechaIni);
            yDer += 88;

            // ─── Hora inicio (Custom) ───
            AgregarLabelConAsterisco(cardDer, "Hora de inicio", xDer, yDer);
            Panel panelHoraIni = CrearSelectorHoraCustom(xDer, yDer + 28, anchoCampoDer, datosEvento.HoraInicio, out ComboBox cboHoraIni, out ComboBox cboMinIni);
            cboHoraIni.SelectedIndexChanged += (s, e) => { int h = int.Parse(cboHoraIni.SelectedItem.ToString()); int m = int.Parse(cboMinIni.SelectedItem.ToString()); datosEvento.HoraInicio = DateTime.Today.AddHours(h).AddMinutes(m); };
            cboMinIni.SelectedIndexChanged += (s, e) => { int h = int.Parse(cboHoraIni.SelectedItem.ToString()); int m = int.Parse(cboMinIni.SelectedItem.ToString()); datosEvento.HoraInicio = DateTime.Today.AddHours(h).AddMinutes(m); };
            cardDer.Controls.Add(panelHoraIni);
            yDer += 88;

            // ─── Fecha fin (Custom - Editable) ───
            cardDer.Controls.Add(new Label { Text = "Fecha de fin  (opcional)", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = blanco, Location = new Point(xDer, yDer), AutoSize = true });

            Panel panelFechaFin = new Panel { Location = new Point(xDer, yDer + 28), Size = new Size(anchoCampoDer, 44), BackColor = campo, Cursor = Cursors.Hand };
            AplicarBordeRedondeado(panelFechaFin, 10);

            Label lblFechaFin = new Label { Text = datosEvento.FechaFin.ToString("dddd, dd 'de' MMMM 'de' yyyy"), ForeColor = blanco, Font = new Font("Segoe UI", 10.5f), Location = new Point(14, 12), AutoSize = true, BackColor = campo };
            Label lblIcoCalFin = new Label { Text = "📅", Font = new Font("Segoe UI", 11), ForeColor = morado, Location = new Point(anchoCampoDer - 35, 11), AutoSize = true, BackColor = campo };

            DateTimePicker dtpFechaFin = new DateTimePicker
            {
                Location = new Point(0, 0),
                Size = new Size(1, 1),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dddd, dd 'de' MMMM 'de' yyyy",
                Visible = false,
                Value = datosEvento.FechaFin
            };

            dtpFechaFin.ValueChanged += (s, e) => { datosEvento.FechaFin = dtpFechaFin.Value; lblFechaFin.Text = dtpFechaFin.Value.ToString("dddd, dd 'de' MMMM 'de' yyyy"); };

            panelFechaFin.Controls.Add(lblFechaFin);
            panelFechaFin.Controls.Add(lblIcoCalFin);
            panelFechaFin.Controls.Add(dtpFechaFin);

            panelFechaFin.Click += (s, e) => SendMessage(dtpFechaFin.Handle, WM_SYSKEYDOWN, (int)Keys.Down, 0);
            lblFechaFin.Click += (s, e) => SendMessage(dtpFechaFin.Handle, WM_SYSKEYDOWN, (int)Keys.Down, 0);
            lblIcoCalFin.Click += (s, e) => SendMessage(dtpFechaFin.Handle, WM_SYSKEYDOWN, (int)Keys.Down, 0);

            cardDer.Controls.Add(panelFechaFin);
            yDer += 88;

            // ─── Hora fin (Custom) ───
            cardDer.Controls.Add(new Label { Text = "Hora de fin  (opcional)", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = blanco, Location = new Point(xDer, yDer), AutoSize = true });
            Panel panelHoraFin = CrearSelectorHoraCustom(xDer, yDer + 28, anchoCampoDer, datosEvento.HoraFin, out ComboBox cboHoraFin, out ComboBox cboMinFin);
            cboHoraFin.SelectedIndexChanged += (s, e) => { int h = int.Parse(cboHoraFin.SelectedItem.ToString()); int m = int.Parse(cboMinFin.SelectedItem.ToString()); datosEvento.HoraFin = DateTime.Today.AddHours(h).AddMinutes(m); };
            cboMinFin.SelectedIndexChanged += (s, e) => { int h = int.Parse(cboHoraFin.SelectedItem.ToString()); int m = int.Parse(cboMinFin.SelectedItem.ToString()); datosEvento.HoraFin = DateTime.Today.AddHours(h).AddMinutes(m); };
            cardDer.Controls.Add(panelHoraFin);

            panelContenido.Controls.Add(cardDer);

            // ════════════════════════════════════════════
            // CAJA DE CONSEJO
            // ════════════════════════════════════════════
            Panel panelTip = new Panel { Location = new Point(xInicio, yInicio + altoTarjeta + 25), Size = new Size(anchoTotal, 60), BackColor = moradoSuave };
            AplicarBordeRedondeado(panelTip, 12);
            panelTip.Controls.Add(new Label { Text = "💡", Font = new Font("Segoe UI", 14), ForeColor = morado, Location = new Point(20, 18), Size = new Size(30, 24), TextAlign = ContentAlignment.MiddleCenter });
            panelTip.Controls.Add(new Label { Text = "Consejo: Mientras más detallada sea la información, mejor será el control del evento.", Font = new Font("Segoe UI", 10, FontStyle.Italic), ForeColor = blanco, Location = new Point(55, 20), AutoSize = true });
            panelContenido.Controls.Add(panelTip);
        }

        // ==========================================
        // HELPERS DEL PASO 1
        // ==========================================
        private Panel CrearTarjetaPanel(int x, int y, int ancho, int alto)
        {
            Panel card = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(ancho, alto),
                BackColor = tarjeta,
                Padding = new Padding(0),
                Anchor = AnchorStyles.Top
            };
            AplicarBordeRedondeado(card, 16);

            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(60, 55, 90), 1.5f))
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                }
            };

            return card;
        }

        private void AgregarLabelConAsterisco(Panel card, string texto, int x, int y)
        {
            card.Controls.Add(new Label
            {
                Text = texto,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(x, y),
                AutoSize = true
            });

            Label lblAster = new Label
            {
                Text = " *",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = rojo,
                Location = new Point(x + 130, y),
                AutoSize = true
            };
            card.Controls.Add(lblAster);
        }

        private Panel CrearSelectorHoraCustom(int x, int y, int ancho, DateTime horaInicial, out ComboBox cboHora, out ComboBox cboMin)
        {
            Panel panelHora = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(ancho, 44),
                BackColor = campo
            };
            AplicarBordeRedondeado(panelHora, 10);

            panelHora.Controls.Add(new Label { Text = "🕐", Font = new Font("Segoe UI", 12), ForeColor = morado, Location = new Point(12, 12), AutoSize = true, BackColor = campo });

            ComboBox horaTemp = new ComboBox
            {
                Location = new Point(40, 10),
                Size = new Size(50, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = campo,
                ForeColor = blanco,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            for (int h = 0; h < 24; h++) horaTemp.Items.Add(h.ToString("D2"));
            horaTemp.SelectedIndex = horaInicial.Hour;

            panelHora.Controls.Add(new Label { Text = ":", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = gris, Location = new Point(95, 11), AutoSize = true, BackColor = campo });

            ComboBox minTemp = new ComboBox
            {
                Location = new Point(110, 10),
                Size = new Size(50, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = campo,
                ForeColor = blanco,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            for (int m = 0; m < 60; m += 5) minTemp.Items.Add(m.ToString("D2"));
            minTemp.SelectedIndex = horaInicial.Minute / 5;

            panelHora.Controls.Add(horaTemp);
            panelHora.Controls.Add(minTemp);

            cboHora = horaTemp;
            cboMin = minTemp;

            return panelHora;
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
                Text = "Haz clic en 'Buscar en mapa' para ubicar cada lugar del evento.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = gris,
                Location = new Point(0, 35),
                AutoSize = true
            };
            panelContenido.Controls.Add(subtitulo);

            Panel lineaDeco = new Panel
            {
                Location = new Point(0, 58),
                Size = new Size(400, 2),
                BackColor = morado
            };
            panelContenido.Controls.Add(lineaDeco);

            Panel contenedor = new Panel
            {
                Location = new Point(0, 75),
                BackColor = fondo,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            contenedor.Size = new Size(panelContenido.Width - 30, panelContenido.Height - 90);
            panelContenido.Controls.Add(contenedor);

            Panel panelIzq = new Panel
            {
                Dock = DockStyle.Left,
                Width = 450,
                BackColor = fondo,
                Padding = new Padding(0, 0, 20, 0)
            };
            contenedor.Controls.Add(panelIzq);

            Button btnAgregar = new Button
            {
                Text = "+  AGREGAR LUGAR",
                Width = 240,
                Height = 45,
                Location = new Point(0, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = morado,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAgregar.FlatAppearance.BorderSize = 0;
            btnAgregar.FlatAppearance.MouseOverBackColor = moradoHover;
            btnAgregar.Click += (s, e) => AgregarLugarUI();
            AplicarBordeRedondeado(btnAgregar, 8);
            panelIzq.Controls.Add(btnAgregar);

            FlowLayoutPanel flpLugares = new FlowLayoutPanel
            {
                Location = new Point(0, 55),
                Size = new Size(panelIzq.Width, panelIzq.Height - 55),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = fondo,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Name = "flpLugares"
            };
            panelIzq.Controls.Add(flpLugares);

            foreach (var l in lugares)
                AgregarLugarUI(l);

            Panel panelDer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = tarjeta,
                Padding = new Padding(0)
            };
            AplicarBordeRedondeado(panelDer, 14);
            contenedor.Controls.Add(panelDer);
            panelDer.BringToFront();

            lblEstadoMapa = new Label
            {
                Dock = DockStyle.Top,
                Height = 35,
                Text = "Cargando mapa...",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = morado,
                BackColor = tarjeta,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0)
            };
            panelDer.Controls.Add(lblEstadoMapa);

            webMapa = new Microsoft.Web.WebView2.WinForms.WebView2
            {
                Dock = DockStyle.Fill,
                DefaultBackgroundColor = tarjeta
            };
            panelDer.Controls.Add(webMapa);
            webMapa.BringToFront();
            lblEstadoMapa.BringToFront();

            this.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (webMapa != null)
                    {
                        AplicarBordeRedondeado(webMapa, 10);
                        if (webMapa.CoreWebView2 != null)
                            webMapa.CoreWebView2.ExecuteScriptAsync("map.invalidateSize()");
                    }
                }
                catch { }
            }));

            mapaListo = false;
            this.BeginInvoke(new Action(async () => await InicializarMapaAsync()));
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
                Width = 470,
                Height = 260,
                BackColor = tarjeta,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(15)
            };
            AplicarBordeRedondeado(card, 12);

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
                Size = new Size(390, 29),
                BackColor = campo,
                ForeColor = blanco,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                Text = lugar.Nombre,
                Name = "txtNombreLugar"
            };
            txtNombre.TextChanged += (s, e) => lugar.Nombre = txtNombre.Text.Trim();
            card.Controls.Add(txtNombre);

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
                Size = new Size(390, 29),
                BackColor = campo,
                ForeColor = blanco,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                Text = lugar.Direccion,
                Name = "txtDireccion"
            };
            txtDireccion.TextChanged += (s, e) => lugar.Direccion = txtDireccion.Text.Trim();
            card.Controls.Add(txtDireccion);

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
                Size = new Size(390, 29),
                BackColor = campo,
                ForeColor = blanco,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                Text = lugar.Referencia,
                Name = "txtReferencia"
            };
            txtReferencia.TextChanged += (s, e) => lugar.Referencia = txtReferencia.Text.Trim();
            card.Controls.Add(txtReferencia);

            Button btnMapa = new Button
            {
                Text = "🗺️ BUSCAR EN MAPA",
                Width = 180,
                Height = 32,
                Location = new Point(225, 205),
                FlatStyle = FlatStyle.Flat,
                BackColor = azul,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnMapa.FlatAppearance.BorderSize = 0;
            btnMapa.Click += (s, e) => SeleccionarLugarParaMapa(lugar, card);
            AplicarBordeRedondeado(btnMapa, 6);
            card.Controls.Add(btnMapa);

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
            AplicarBordeRedondeado(btnQuitar, 6);
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

        private void SeleccionarLugarParaMapa(LugarEvento lugar, Panel card)
        {
            var flp = panelContenido.Controls.Find("flpLugares", true).Length > 0
                ? panelContenido.Controls.Find("flpLugares", true)[0] as FlowLayoutPanel
                : null;
            if (flp != null)
            {
                foreach (Control c in flp.Controls)
                    c.BackColor = tarjeta;
            }

            card.BackColor = Color.FromArgb(60, 75, 110);
            lugarSeleccionado = lugar;

            if (lblEstadoMapa != null)
                lblEstadoMapa.Text = $"📍 Selecciona el destino en el mapa para: Lugar";

            if (mapaListo && webMapa?.CoreWebView2 != null)
            {
                string busqueda = !string.IsNullOrWhiteSpace(lugar.Direccion)
                                  ? lugar.Direccion
                                  : lugar.Nombre;

                if (!string.IsNullOrWhiteSpace(busqueda))
                    webMapa.CoreWebView2.PostWebMessageAsString(busqueda);
            }
        }

        private async System.Threading.Tasks.Task InicializarMapaAsync()
        {
            try
            {
                if (webMapa == null) return;

                lblEstadoMapa.Text = "Cargando motor del mapa...";

                await webMapa.EnsureCoreWebView2Async();
                webMapa.CoreWebView2.WebMessageReceived -= RecibirMensajeMapa;
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

        private void RecibirMensajeMapa(object sender,
    Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            string mensaje;
            try { mensaje = e.TryGetWebMessageAsString(); }
            catch { return; }

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
                        lblEstadoMapa.Text = "⚠ Primero haz clic en el botón 🗺️ BUSCAR EN MAPA";
                    return;
                }

                string valorNombre = !string.IsNullOrWhiteSpace(nombre) ? nombre : direccion;
                if (!string.IsNullOrWhiteSpace(valorNombre))
                {
                    lugarSeleccionado.Nombre = valorNombre;
                    ActualizarTextBox(lugarSeleccionado.Panel, "txtNombreLugar", valorNombre);
                }

                if (!string.IsNullOrWhiteSpace(direccion))
                {
                    lugarSeleccionado.Direccion = direccion;
                    ActualizarTextBox(lugarSeleccionado.Panel, "txtDireccion", direccion);
                }

                if (!string.IsNullOrWhiteSpace(localidad))
                {
                    lugarSeleccionado.Referencia = localidad;
                    ActualizarTextBox(lugarSeleccionado.Panel, "txtReferencia", localidad);
                }

                if (lblEstadoMapa != null)
                    lblEstadoMapa.Text = "✅ Ubicación cargada correctamente.";
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
                Size = new Size(panelContenido.ClientSize.Width, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panelContenido.Controls.Add(titulo);

            Label subtitulo = new Label
            {
                Text = "Selecciona a los trabajadores que te acompañarán en el evento.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = gris,
                Location = new Point(0, 35),
                Size = new Size(panelContenido.ClientSize.Width, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panelContenido.Controls.Add(subtitulo);

            Panel lineaDeco = new Panel
            {
                Location = new Point((panelContenido.ClientSize.Width - 400) / 2, 58),
                Size = new Size(400, 2),
                BackColor = morado,
                Anchor = AnchorStyles.Top
            };
            panelContenido.Controls.Add(lineaDeco);

            var trabajadores = new List<TrabajadorItem>();
            var externos = new List<PersonalExternoItem>();

            idEncargadoSeleccionado = idTrabajador;

            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();

                string qTrab = @"
                    SELECT t.id_trabajador, t.nombre, r.nombre_rol
                    FROM trabajadores t
                    INNER JOIN roles r ON r.id_rol = t.id_rol
                    WHERE t.activo = true 
                      AND r.nombre_rol ILIKE 'Trabajador'
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

            int anchoContenido = Math.Min(1000, panelContenido.Width - 100);
            int xInicio = (panelContenido.Width - anchoContenido) / 2;
            if (xInicio < 30) xInicio = 30;

            Panel panelScroll = new Panel
            {
                Location = new Point(xInicio, 75),
                Size = new Size(anchoContenido, panelContenido.Height - 100),
                AutoScroll = true,
                BackColor = fondo,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            panelContenido.Controls.Add(panelScroll);

            Panel cardSub = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(anchoContenido - 25, 280),
                BackColor = tarjeta,
                Padding = new Padding(20),
                Name = "cardSub"
            };
            AplicarBordeRedondeado(cardSub, 12);
            panelScroll.Controls.Add(cardSub);

            cardSub.Controls.Add(new Label
            {
                Text = "👥  TRABAJADORES DEL SISTEMA",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = morado,
                Location = new Point(20, 15),
                AutoSize = true
            });

            FlowLayoutPanel flpSubs = new FlowLayoutPanel
            {
                Location = new Point(20, 45),
                Size = new Size(anchoContenido - 85, 210),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = true,
                BackColor = tarjeta,
                Name = "flpSubs"
            };
            cardSub.Controls.Add(flpSubs);

            foreach (var t in trabajadores)
            {
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

            if (trabajadores.Count == 0)
            {
                flpSubs.Controls.Add(new Label
                {
                    Text = "No hay otros trabajadores registrados.",
                    Font = new Font("Segoe UI", 9, FontStyle.Italic),
                    ForeColor = gris,
                    AutoSize = true
                });
            }

            Panel cardExt = new Panel
            {
                Location = new Point(0, 295),
                Size = new Size(anchoContenido - 25, 240),
                BackColor = tarjeta,
                Padding = new Padding(20)
            };
            AplicarBordeRedondeado(cardExt, 12);
            panelScroll.Controls.Add(cardExt);

            cardExt.Controls.Add(new Label
            {
                Text = "🧑‍🔧  PERSONAL EXTERNO",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = morado,
                Location = new Point(20, 15),
                AutoSize = true
            });

            Button btnAddExt = new Button
            {
                Text = "+  AGREGAR PERSONAL EXTERNO",
                Width = 250,
                Height = 35,
                Location = new Point(anchoContenido - 300, 12),
                FlatStyle = FlatStyle.Flat,
                BackColor = morado,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnAddExt.FlatAppearance.BorderSize = 0;
            btnAddExt.FlatAppearance.MouseOverBackColor = moradoHover;
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
            AplicarBordeRedondeado(btnAddExt, 8);
            cardExt.Controls.Add(btnAddExt);

            FlowLayoutPanel flpExt = new FlowLayoutPanel
            {
                Location = new Point(20, 55),
                Size = new Size(anchoContenido - 85, 170),
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
                    Text = "No hay personal externo registrado aún.",
                    Font = new Font("Segoe UI", 9, FontStyle.Italic),
                    ForeColor = gris,
                    AutoSize = true
                });
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
                Text = "Escribe el código o nombre del equipo y selecciónalo de la lista de sugerencias.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = gris,
                Location = new Point(0, 35),
                AutoSize = true
            };
            panelContenido.Controls.Add(subtitulo);

            Panel lineaDeco = new Panel
            {
                Location = new Point(0, 58),
                Size = new Size(400, 2),
                BackColor = morado
            };
            panelContenido.Controls.Add(lineaDeco);

            Panel panelAdd = new Panel
            {
                Location = new Point(0, 75),
                Size = new Size(1255, 110),
                BackColor = tarjeta,
                Padding = new Padding(15),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            AplicarBordeRedondeado(panelAdd, 12);
            panelContenido.Controls.Add(panelAdd);

            panelAdd.Controls.Add(new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI", 12),
                ForeColor = morado,
                Location = new Point(18, 22),
                Size = new Size(24, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            });

            panelAdd.Controls.Add(new Label
            {
                Text = "Buscar equipo por código o nombre:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(50, 22),
                AutoSize = true
            });

            Panel panelBuscar = new Panel
            {
                Location = new Point(48, 48),
                Size = new Size(600, 42),
                BackColor = campo
            };
            AplicarBordeRedondeado(panelBuscar, 10);
            panelBuscar.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(70, 65, 110), 1.2f))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var p = RoundedRect(new Rectangle(0, 0, panelBuscar.Width - 1, panelBuscar.Height - 1), 10))
                        e.Graphics.DrawPath(pen, p);
                }
            };
            panelAdd.Controls.Add(panelBuscar);

            TextBox txtCodigo = new TextBox
            {
                Location = new Point(14, 10),
                Size = new Size(572, 22),
                BackColor = campo,
                ForeColor = blanco,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11),
                PlaceholderText = "Ej: CAM01, Micrófono, Cámara..."
            };
            panelBuscar.Controls.Add(txtCodigo);
            txtCodigoActivo = txtCodigo;

            panelSugerenciasContenedor = new Panel
            {
                Visible = false,
                BackColor = Color.FromArgb(22, 20, 36),
                BorderStyle = BorderStyle.FixedSingle
            };
            AplicarBordeRedondeado(panelSugerenciasContenedor, 8);
            panelSugerenciasContenedor.Paint += (s, e) =>
            {
                using (var pen = new Pen(morado, 1.4f))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var p = RoundedRect(
                        new Rectangle(0, 0, panelSugerenciasContenedor.Width - 1, panelSugerenciasContenedor.Height - 1), 8))
                        e.Graphics.DrawPath(pen, p);
                }
            };
            panelContenido.Controls.Add(panelSugerenciasContenedor);
            panelSugerenciasContenedor.BringToFront();

            lstSugerencias = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(22, 20, 36),
                ForeColor = blanco,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10),
                IntegralHeight = false,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 44
            };
            lstSugerencias.DrawItem += LstSugerencias_DrawItem;
            lstSugerencias.Click += (s, e) => InsertarSugerenciaSeleccionada();
            lstSugerencias.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    InsertarSugerenciaSeleccionada();
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    OcultarSugerencias();
                    txtCodigo.Focus();
                }
            };
            panelSugerenciasContenedor.Controls.Add(lstSugerencias);

            timerBusqueda = new System.Windows.Forms.Timer { Interval = 220 };
            timerBusqueda.Tick += (s, e) =>
            {
                timerBusqueda.Stop();
                BuscarEquiposSugerencias(txtCodigo.Text.Trim());
            };

            txtCodigo.TextChanged += (s, e) =>
            {
                timerBusqueda.Stop();
                timerBusqueda.Start();
            };

            txtCodigo.Enter += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtCodigo.Text))
                {
                    timerBusqueda.Stop();
                    BuscarEquiposSugerencias("");
                }
            };

            txtCodigo.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtCodigo.Text))
                {
                    timerBusqueda.Stop();
                    BuscarEquiposSugerencias("");
                }
            };

            txtCodigo.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Down && lstSugerencias.Items.Count > 0 && panelSugerenciasContenedor.Visible)
                {
                    e.SuppressKeyPress = true;
                    lstSugerencias.Focus();
                    if (lstSugerencias.SelectedIndex < 0)
                        lstSugerencias.SelectedIndex = 0;
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    if (panelSugerenciasContenedor.Visible && lstSugerencias.Items.Count > 0)
                    {
                        if (lstSugerencias.SelectedIndex < 0)
                            lstSugerencias.SelectedIndex = 0;
                        InsertarSugerenciaSeleccionada();
                    }
                    else
                    {
                        string cod = txtCodigo.Text.Trim();
                        if (!string.IsNullOrWhiteSpace(cod))
                            AgregarEquipoPorCodigo(cod);
                    }
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    OcultarSugerencias();
                }
            };

            txtCodigo.Leave += (s, e) =>
            {
                var t = new System.Windows.Forms.Timer { Interval = 180 };
                t.Tick += (ss, ee) =>
                {
                    t.Stop();
                    t.Dispose();
                    if (!panelSugerenciasContenedor.Focused && !lstSugerencias.Focused)
                        OcultarSugerencias();
                };
                t.Start();
            };

            Button btnAdd = new Button
            {
                Text = "＋  AGREGAR SELECCIONADO",
                Width = 240,
                Height = 42,
                Location = new Point(670, 48),
                FlatStyle = FlatStyle.Flat,
                BackColor = morado,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.FlatAppearance.MouseOverBackColor = moradoHover;
            btnAdd.Click += (s, e) =>
            {
                if (panelSugerenciasContenedor.Visible && lstSugerencias.Items.Count > 0)
                {
                    if (lstSugerencias.SelectedIndex < 0)
                        lstSugerencias.SelectedIndex = 0;
                    InsertarSugerenciaSeleccionada();
                }
                else
                {
                    string cod = txtCodigo.Text.Trim();
                    if (string.IsNullOrWhiteSpace(cod))
                    {
                        MessageBox.Show("Escribe un código o selecciona un equipo de la lista.");
                        return;
                    }
                    AgregarEquipoPorCodigo(cod);
                    txtCodigo.Clear();
                    txtCodigo.Focus();
                }
            };
            AplicarBordeRedondeado(btnAdd, 10);
            panelAdd.Controls.Add(btnAdd);

            Label lblContador = new Label
            {
                Text = "0 equipos asignados",
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = gris,
                Location = new Point(930, 58),
                AutoSize = true,
                Name = "lblContadorEquipos"
            };
            panelAdd.Controls.Add(lblContador);

            FlowLayoutPanel flpEquipos = new FlowLayoutPanel
            {
                Location = new Point(0, 200),
                Size = new Size(1255, 480),
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

            ActualizarContadorEquipos();
        }

        private void LstSugerencias_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= lstSugerencias.Items.Count) return;

            var item = lstSugerencias.Items[e.Index] as EquipoSugerencia;
            if (item == null) return;

            bool seleccionado = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color fondoItem = seleccionado ? moradoSuave : Color.FromArgb(22, 20, 36);

            using (var brush = new SolidBrush(fondoItem))
                e.Graphics.FillRectangle(brush, e.Bounds);

            if (seleccionado)
            {
                using (var pen = new Pen(morado, 1.4f))
                    e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Top, e.Bounds.Left, e.Bounds.Bottom);
            }

            string codigo = string.IsNullOrEmpty(item.Codigo) ? "—" : item.Codigo;
            var rectCod = new Rectangle(e.Bounds.X + 12, e.Bounds.Y + 10, 110, 24);
            using (var path = RoundedRect(rectCod, 6))
            using (var brush = new SolidBrush(moradoSuave))
                e.Graphics.FillPath(brush, path);

            TextRenderer.DrawText(
                e.Graphics, codigo,
                new Font("Segoe UI", 9, FontStyle.Bold),
                rectCod, morado,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            var rectNom = new Rectangle(e.Bounds.X + 132, e.Bounds.Y + 6, e.Bounds.Width - 260, 20);
            TextRenderer.DrawText(
                e.Graphics, item.Nombre,
                new Font("Segoe UI", 10, FontStyle.Bold),
                rectNom, blanco,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            string sub = "";
            if (!string.IsNullOrEmpty(item.Categoria)) sub += item.Categoria;
            if (!string.IsNullOrEmpty(item.Estado))
                sub += (sub.Length > 0 ? "  •  " : "") + item.Estado;

            if (sub.Length > 0)
            {
                var rectSub = new Rectangle(e.Bounds.X + 132, e.Bounds.Y + 24, e.Bounds.Width - 260, 16);
                TextRenderer.DrawText(
                    e.Graphics, sub,
                    new Font("Segoe UI", 8.5f),
                    rectSub, gris,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            Color colStock = item.Stock <= 0 ? rojo : (item.Stock < 3 ? naranja : verde);
            var rectStock = new Rectangle(e.Bounds.Right - 100, e.Bounds.Y + 12, 88, 22);
            TextRenderer.DrawText(
                e.Graphics, $"Stock: {item.Stock}",
                new Font("Segoe UI", 9, FontStyle.Bold),
                rectStock, colStock,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
        }

        private void BuscarEquiposSugerencias(string texto)
        {
            sugerenciasActuales = new List<EquipoSugerencia>();

            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();

                string q;
                bool vacio = string.IsNullOrWhiteSpace(texto);

                if (vacio)
                {
                    q = @"
                SELECT id, codigo, nombre, stock, estado, categoria
                FROM equipos
                WHERE stock > 0
                ORDER BY 
                    CASE WHEN estado ILIKE '%disponible%' THEN 0 ELSE 1 END,
                    codigo
                LIMIT 30";
                }
                else
                {
                    q = @"
                SELECT id, codigo, nombre, stock, estado, categoria
                FROM equipos
                WHERE stock > 0
                  AND (
                      codigo ILIKE @patron 
                      OR nombre ILIKE @patron
                      OR categoria ILIKE @patron
                  )
                ORDER BY
                    CASE 
                        WHEN codigo ILIKE @exacto THEN 0
                        WHEN codigo ILIKE @empieza THEN 1
                        WHEN nombre ILIKE @empieza THEN 2
                        WHEN codigo ILIKE @patron THEN 3
                        WHEN nombre ILIKE @patron THEN 4
                        WHEN categoria ILIKE @patron THEN 5
                        ELSE 6
                    END,
                    codigo
                LIMIT 20";
                }

                using (NpgsqlCommand cmd = new NpgsqlCommand(q, con))
                {
                    if (!vacio)
                    {
                        cmd.Parameters.AddWithValue("@patron", "%" + texto + "%");
                        cmd.Parameters.AddWithValue("@exacto", texto);
                        cmd.Parameters.AddWithValue("@empieza", texto + "%");
                    }

                    using (NpgsqlDataReader rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            sugerenciasActuales.Add(new EquipoSugerencia
                            {
                                Id = Convert.ToInt32(rd["id"]),
                                Codigo = rd["codigo"]?.ToString() ?? "",
                                Nombre = rd["nombre"]?.ToString() ?? "",
                                Categoria = rd["categoria"]?.ToString() ?? "",
                                Estado = rd["estado"]?.ToString() ?? "",
                                Stock = rd["stock"] != DBNull.Value ? Convert.ToInt32(rd["stock"]) : 0
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error autocompletado: " + ex.Message);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }

            sugerenciasActuales = sugerenciasActuales
                .Where(s => !equiposAsignados.Any(eq => eq.IdEquipo == s.Id))
                .ToList();

            if (sugerenciasActuales.Count == 0)
            {
                if (!string.IsNullOrWhiteSpace(texto))
                {
                    lstSugerencias.Items.Clear();
                    lstSugerencias.Items.Add("(sin resultados)");

                    Point posCampoVacio = txtCodigoActivo.PointToScreen(Point.Empty);
                    Point posPanelVacio = panelContenido.PointToClient(posCampoVacio);

                    panelSugerenciasContenedor.Location = new Point(posPanelVacio.X, posPanelVacio.Y + txtCodigoActivo.Height + 32);
                    panelSugerenciasContenedor.Size = new Size(600, 50);
                    panelSugerenciasContenedor.Visible = true;
                    panelSugerenciasContenedor.BringToFront();
                }
                else
                {
                    OcultarSugerencias();
                }
                return;
            }

            lstSugerencias.Items.Clear();
            foreach (var s in sugerenciasActuales)
                lstSugerencias.Items.Add(s);

            Point posCampo = txtCodigoActivo.PointToScreen(Point.Empty);
            Point posPanel = panelContenido.PointToClient(posCampo);

            int anchoLista = 600;
            int altoLista = Math.Min(320, sugerenciasActuales.Count * 44 + 4);

            panelSugerenciasContenedor.Location = new Point(posPanel.X, posPanel.Y + txtCodigoActivo.Height + 32);
            panelSugerenciasContenedor.Size = new Size(anchoLista, altoLista);
            panelSugerenciasContenedor.Visible = true;
            panelSugerenciasContenedor.BringToFront();
            lstSugerencias.SelectedIndex = 0;
        }

        private void InsertarSugerenciaSeleccionada()
        {
            if (lstSugerencias.SelectedItem is EquipoSugerencia sug)
            {
                AgregarEquipoDesdeSugerencia(sug);
                OcultarSugerencias();
                txtCodigoActivo?.Clear();
                txtCodigoActivo?.Focus();
            }
        }

        private void OcultarSugerencias()
        {
            if (panelSugerenciasContenedor != null)
            {
                panelSugerenciasContenedor.Visible = false;
                lstSugerencias?.Items.Clear();
            }
            sugerenciasActuales?.Clear();
        }

        private void AgregarEquipoDesdeSugerencia(EquipoSugerencia s)
        {
            if (s.Stock <= 0)
            {
                MessageBox.Show($"El equipo '{s.Nombre}' no tiene stock disponible.",
                    "Sin stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (equiposAsignados.Any(e => e.IdEquipo == s.Id))
            {
                MessageBox.Show("Este equipo ya fue agregado.");
                return;
            }

            var equipo = new EquipoAsignado
            {
                IdEquipo = s.Id,
                Codigo = s.Codigo,
                Nombre = s.Nombre,
                Categoria = s.Categoria,
                Estado = s.Estado,
                Stock = s.Stock
            };
            equiposAsignados.Add(equipo);
            CrearTarjetaEquipoUI(equipo);
            ActualizarContadorEquipos();
        }

        private void AgregarEquipoPorCodigo(string codigo)
        {
            OcultarSugerencias();

            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();
                string q = @"SELECT id, codigo, nombre, stock, estado, categoria
                     FROM equipos WHERE codigo ILIKE @codigo LIMIT 1";

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
                        int stock = Convert.ToInt32(rd["stock"]);
                        string estado = rd["estado"]?.ToString() ?? "";
                        string categoria = rd["categoria"]?.ToString() ?? "";

                        if (stock <= 0)
                        {
                            MessageBox.Show(
                                $"El equipo '{nom}' no tiene stock disponible.\n" +
                                $"Estado actual: {estado}",
                                "Sin stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        if (equiposAsignados.Any(e => e.IdEquipo == idEq))
                        {
                            MessageBox.Show("Este equipo ya fue agregado.");
                            return;
                        }

                        var equipo = new EquipoAsignado
                        {
                            IdEquipo = idEq,
                            Codigo = cod,
                            Nombre = nom,
                            Categoria = categoria,
                            Estado = estado,
                            Stock = stock
                        };
                        equiposAsignados.Add(equipo);
                        CrearTarjetaEquipoUI(equipo);
                        ActualizarContadorEquipos();

                        txtCodigoActivo?.Clear();
                        txtCodigoActivo?.Focus();
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
                Width = 1220,
                Height = 150,
                BackColor = tarjeta,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(20)
            };
            AplicarBordeRedondeado(card, 12);
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(60, 55, 90), 1.2f))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var p = RoundedRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 12))
                        e.Graphics.DrawPath(pen, p);
                }
            };

            // ── Badge de código ──
            Panel badgeCod = new Panel
            {
                Location = new Point(20, 18),
                Size = new Size(140, 32),
                BackColor = moradoSuave
            };
            AplicarBordeRedondeado(badgeCod, 8);
            badgeCod.Controls.Add(new Label
            {
                Text = string.IsNullOrEmpty(eq.Codigo) ? "—" : eq.Codigo,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = morado,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            });
            card.Controls.Add(badgeCod);

            // ── Nombre del equipo ──
            card.Controls.Add(new Label
            {
                Text = eq.Nombre,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(175, 22),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            // ── Chip de categoría ──
            if (!string.IsNullOrWhiteSpace(eq.Categoria))
            {
                Panel chipCat = new Panel
                {
                    Location = new Point(178, 50),
                    Size = new Size(120, 22),
                    BackColor = Color.FromArgb(45, 55, 85)
                };
                AplicarBordeRedondeado(chipCat, 10);
                chipCat.Controls.Add(new Label
                {
                    Text = eq.Categoria,
                    Font = new Font("Segoe UI", 8, FontStyle.Bold),
                    ForeColor = azul,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent
                });
                card.Controls.Add(chipCat);
            }

            // ── Chip de stock ──
            Color colStock = eq.Stock <= 0 ? rojo : (eq.Stock < 3 ? naranja : verde);
            string txtStock = "Stock: " + eq.Stock;
            Panel chipStock = new Panel
            {
                Location = new Point(string.IsNullOrWhiteSpace(eq.Categoria) ? 178 : 310, 50),
                Size = new Size(90, 22),
                BackColor = Color.FromArgb(30, 30, 45)
            };
            AplicarBordeRedondeado(chipStock, 10);
            chipStock.Controls.Add(new Label
            {
                Text = txtStock,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = colStock,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            });
            card.Controls.Add(chipStock);

            // ── Botón quitar ──
            Button btnQ = new Button
            {
                Text = "✗  QUITAR",
                Width = 110,
                Height = 32,
                Location = new Point(1080, 18),
                FlatStyle = FlatStyle.Flat,
                BackColor = rojo,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnQ.FlatAppearance.BorderSize = 0;
            btnQ.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 110, 130);
            btnQ.Click += (s, e) =>
            {
                equiposAsignados.Remove(eq);
                flp.Controls.Remove(card);
                ActualizarContadorEquipos();
            };
            AplicarBordeRedondeado(btnQ, 8);
            card.Controls.Add(btnQ);

            // ─────────────────────────────────────────
            // LUGAR
            // ─────────────────────────────────────────
            card.Controls.Add(new Label
            {
                Text = "📍  Lugar",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = gris,
                Location = new Point(20, 92),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            Panel panelLugar = new Panel
            {
                Location = new Point(20, 112),
                Size = new Size(500, 34),
                BackColor = campo
            };
            AplicarBordeRedondeado(panelLugar, 8);

            ComboBox cboLugar = new ComboBox
            {
                Location = new Point(4, 4),
                Size = new Size(492, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = campo,
                ForeColor = blanco,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f),
                DrawMode = DrawMode.OwnerDrawFixed // 🔥 PARA DIBUJAR LOS COLORES OSCUROS
            };
            for (int i = 0; i < lugares.Count; i++)
                cboLugar.Items.Add(lugares[i].Nombre);

            if (cboLugar.Items.Count > 0)
            {
                cboLugar.SelectedIndex = 0;
                eq.IdLugar = 0;
            }

            cboLugar.SelectedIndexChanged += (s, e) =>
            {
                if (cboLugar.SelectedIndex >= 0)
                {
                    eq.IdLugar = cboLugar.SelectedIndex;
                }
            };

            // 🔥 DIBUJAMOS LOS COLORES DE LA LISTA
            cboLugar.DrawItem += (s, e) =>
            {
                e.DrawBackground();
                if (e.Index < 0) return;

                bool seleccionado = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                Color fondoItem = seleccionado ? moradoSuave : Color.FromArgb(22, 20, 36);

                using (var brush = new SolidBrush(fondoItem))
                    e.Graphics.FillRectangle(brush, e.Bounds);

                string texto = cboLugar.Items[e.Index].ToString();
                using (var brushTexto = new SolidBrush(blanco))
                using (var format = new StringFormat { LineAlignment = StringAlignment.Center })
                {
                    e.Graphics.DrawString(texto, cboLugar.Font, brushTexto, e.Bounds, format);
                }
            };

            panelLugar.Controls.Add(cboLugar);
            card.Controls.Add(panelLugar);

            // ─────────────────────────────────────────
            // RESPONSABLE
            // ─────────────────────────────────────────
            card.Controls.Add(new Label
            {
                Text = "👤  Responsable",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = gris,
                Location = new Point(540, 92),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            Panel panelResp = new Panel
            {
                Location = new Point(540, 112),
                Size = new Size(500, 34),
                BackColor = campo
            };
            AplicarBordeRedondeado(panelResp, 8);

            ComboBox cboResp = new ComboBox
            {
                Location = new Point(4, 4),
                Size = new Size(492, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = campo,
                ForeColor = blanco,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f),
                DrawMode = DrawMode.OwnerDrawFixed
            };

            CargarResponsables(cboResp);

            if (cboResp.Items.Count > 0)
            {
                cboResp.SelectedIndex = 0;
                if (cboResp.SelectedItem is ResponsableItem ri)
                {
                    eq.ResponsableTipo = ri.Tipo;
                    eq.ResponsableId = ri.Id;
                    eq.ResponsableNombre = ri.Nombre;
                }
            }

            cboResp.SelectedIndexChanged += (s, e) =>
            {
                if (cboResp.SelectedItem is ResponsableItem ri)
                {
                    eq.ResponsableTipo = ri.Tipo;
                    eq.ResponsableId = ri.Id;
                    eq.ResponsableNombre = ri.Nombre;
                }
            };

            // 🔥 DIBUJAMOS LOS COLORES DE LA LISTA
            cboResp.DrawItem += (s, e) =>
            {
                e.DrawBackground();
                if (e.Index < 0) return;

                bool seleccionado = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                Color fondoItem = seleccionado ? moradoSuave : Color.FromArgb(22, 20, 36);

                using (var brush = new SolidBrush(fondoItem))
                    e.Graphics.FillRectangle(brush, e.Bounds);

                string texto = cboResp.Items[e.Index].ToString();
                using (var brushTexto = new SolidBrush(blanco))
                using (var format = new StringFormat { LineAlignment = StringAlignment.Center })
                {
                    e.Graphics.DrawString(texto, cboResp.Font, brushTexto, e.Bounds, format);
                }
            };

            panelResp.Controls.Add(cboResp);
            card.Controls.Add(panelResp);

            eq.Panel = card;
            flp.Controls.Add(card);
        }

        private void ActualizarContadorEquipos()
        {
            foreach (Control c in panelContenido.Controls)
            {
                if (c is Panel p)
                {
                    var lbl = p.Controls["lblContadorEquipos"] as Label;
                    if (lbl != null)
                    {
                        lbl.Text = equiposAsignados.Count + " equipos asignados";
                        break;
                    }
                }
            }
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
            // Contenedor central para que no se vea pegado a la izquierda
            int anchoContenido = Math.Min(1000, panelContenido.Width - 100);
            int xInicio = (panelContenido.Width - anchoContenido) / 2;
            if (xInicio < 30) xInicio = 30;

            // ─── TÍTULO CENTRADO ───
            Label titulo = new Label
            {
                Text = "⑤  RESUMEN DEL EVENTO",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(0, 0),
                Size = new Size(panelContenido.ClientSize.Width, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panelContenido.Controls.Add(titulo);

            // ─── SUBTÍTULO CENTRADO ───
            Label subtitulo = new Label
            {
                Text = "Revisa toda la información antes de guardar.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = gris,
                Location = new Point(0, 35),
                Size = new Size(panelContenido.ClientSize.Width, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panelContenido.Controls.Add(subtitulo);

            // ─── LÍNEA MORADA DECORATIVA CENTRADA ───
            Panel lineaDeco = new Panel
            {
                Location = new Point((panelContenido.ClientSize.Width - 400) / 2, 58),
                Size = new Size(400, 2),
                BackColor = morado,
                Anchor = AnchorStyles.Top
            };
            panelContenido.Controls.Add(lineaDeco);

            // ─── PANEL DEL RESUMEN ───
            Panel panelResumen = new Panel
            {
                Location = new Point(xInicio, 75),
                Size = new Size(anchoContenido, panelContenido.Height - 100),
                AutoScroll = true,
                BackColor = tarjeta,
                Padding = new Padding(30),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            AplicarBordeRedondeado(panelResumen, 14);
            panelContenido.Controls.Add(panelResumen);

            int y = 0;

            // ─── SECCIÓN EVENTO ───
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

            // ─── SECCIÓN LUGARES ───
            y = AgregarSeccionResumen(panelResumen, "📍  LUGARES (" + lugares.Count + ")", y);
            foreach (var l in lugares)
            {
                string texto = "• " + l.Nombre;
                if (!string.IsNullOrWhiteSpace(l.Direccion))
                    texto += "  →  " + l.Direccion;
                y = AgregarLineaResumen(panelResumen, "", texto, y);
            }
            y += 15;

            // ─── SECCIÓN PERSONAL ───
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

            // ─── SECCIÓN EQUIPOS ───
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
                    DateTime fechaEventoConHora = datosEvento.FechaInicio.Date + datosEvento.HoraInicio.TimeOfDay;

                    cmd.Parameters.AddWithValue("@fecha", fechaEventoConHora);
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
                    DateTime fechaSalidaConHora = datosEvento.FechaInicio.Date + datosEvento.HoraInicio.TimeOfDay;
                    cmd.Parameters.AddWithValue("@fecha", fechaSalidaConHora);
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

                foreach (var eq in equiposAsignados)
                {
                    string sqlStock = @"
                        UPDATE equipos
                        SET stock = stock - 1,
                            estado = 'En evento'
                        WHERE id = @id AND stock > 0";

                    using (NpgsqlCommand cmdStock = new NpgsqlCommand(sqlStock, con, transaccion))
                    {
                        cmdStock.Parameters.AddWithValue("@id", eq.IdEquipo);
                        int filas = cmdStock.ExecuteNonQuery();

                        if (filas == 0)
                        {
                            throw new Exception(
                                $"⚠ El equipo '{eq.Codigo} - {eq.Nombre}' ya no tiene stock disponible.\n\n" +
                                $"Otro usuario pudo haberlo tomado, o el stock llegó a 0.\n" +
                                $"Actualiza la lista de equipos e intenta de nuevo.");
                        }
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

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_SYSKEYDOWN = 0x104;
    }
}
