using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace SUSTENTACION.PanelTrabajador
{
    public class EventosPanel : UserControl
    {
        // ═══════════════════════════════════════════
        // PALETA
        // ═══════════════════════════════════════════
        private readonly Color bgMain = Color.FromArgb(15, 15, 20);
        private readonly Color bgCard = Color.FromArgb(25, 25, 35);
        private readonly Color bgCardHover = Color.FromArgb(35, 35, 50);
        private readonly Color bgMapPlaceholder = Color.FromArgb(20, 25, 40);
        private readonly Color accentPurple = Color.FromArgb(124, 92, 255);
        private readonly Color accentGreen = Color.FromArgb(46, 204, 148);
        private readonly Color accentYellow = Color.FromArgb(255, 193, 7);
        private readonly Color accentRed = Color.FromArgb(240, 90, 110);
        private readonly Color accentCyan = Color.FromArgb(0, 188, 212);
        private readonly Color textWhite = Color.White;
        private readonly Color textMuted = Color.FromArgb(150, 150, 160);

        private int idTrabajador;
        private string nombreTrabajador;

        // Controles
        private FlowLayoutPanel flpEventos;
        private ComboBox cboFiltroEstado;
        private TextBox txtBuscar;

        // Clase interna
        private class EventoItem
        {
            public int Id { get; set; }
            public string Titulo { get; set; } = "";
            public DateTime Fecha { get; set; }
            public string HoraInicio { get; set; } = "";
            public string HoraFin { get; set; } = "";
            public string TipoEvento { get; set; } = "";
            public string Descripcion { get; set; } = "";
            public string Ubicacion { get; set; } = "";
            public string Referencia { get; set; } = "";
            public string Estado { get; set; } = "";
            public string Encargado { get; set; } = "";
            public string ImagenUrl { get; set; } = "";
            public string Equipos { get; set; } = "";
        }

        public EventosPanel(int idTrabajador = 1, string nombreTrabajador = "Trabajador")
        {
            this.idTrabajador = idTrabajador;
            this.nombreTrabajador = nombreTrabajador;

            InitializeComponentes();
            CargarEventos();
        }

        private void InitializeComponentes()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = bgMain;
            this.Padding = new Padding(24, 20, 24, 20);

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90f));  // Header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));  // Filtros
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Grid de eventos

            // ─── HEADER ───
            Panel headerPanel = new Panel { Dock = DockStyle.Fill, BackColor = bgCard, Padding = new Padding(20) };
            AplicarBordesRedondeados(headerPanel, 10);

            headerPanel.Controls.Add(new Label
            {
                Text = "📅  Eventos",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(20, 14),
                AutoSize = true
            });

            headerPanel.Controls.Add(new Label
            {
                Text = "Todos los eventos registrados en el sistema",
                Font = new Font("Segoe UI", 9f),
                ForeColor = textMuted,
                Location = new Point(22, 45),
                AutoSize = true
            });

            mainLayout.Controls.Add(headerPanel, 0, 0);

            // ─── FILTROS ───
            FlowLayoutPanel filtrosPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 10, 0, 5)
            };

            // Buscador
            Panel panelBuscar = new Panel
            {
                Size = new Size(320, 38),
                BackColor = bgCard,
                Padding = new Padding(14, 0, 14, 0),
                Margin = new Padding(0, 0, 10, 0)
            };
            AplicarBordesRedondeados(panelBuscar, 10);

            panelBuscar.Controls.Add(new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI", 11f),
                ForeColor = textMuted,
                Location = new Point(12, 10),
                AutoSize = true,
                BackColor = bgCard
            });

            txtBuscar = new TextBox
            {
                Location = new Point(42, 11),
                Width = 260,
                BackColor = bgCard,
                ForeColor = textWhite,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10f),
                PlaceholderText = "Buscar evento, ubicación..."
            };
            txtBuscar.TextChanged += (s, e) => CargarEventos();
            panelBuscar.Controls.Add(txtBuscar);

            filtrosPanel.Controls.Add(panelBuscar);

            // Filtro de estado
            cboFiltroEstado = new ComboBox
            {
                Width = 180,
                Height = 38,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = bgCard,
                ForeColor = textWhite,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f),
                Margin = new Padding(0, 0, 10, 0)
            };
            cboFiltroEstado.Items.AddRange(new object[]
            {
                "Todos los estados",
                "Programado",
                "En curso",
                "Finalizado"
            });
            cboFiltroEstado.SelectedIndex = 0;
            cboFiltroEstado.SelectedIndexChanged += (s, e) => CargarEventos();
            filtrosPanel.Controls.Add(cboFiltroEstado);

            mainLayout.Controls.Add(filtrosPanel, 0, 1);

            // ─── GRID DE EVENTOS ───
            flpEventos = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            mainLayout.Controls.Add(flpEventos, 0, 2);

            this.Controls.Add(mainLayout);
        }

        //
        //
        //
        // 🔥 Helper para convertir cualquier tipo de fecha a DateTime
        private DateTime ConvertirFecha(object valor)
        {
            if (valor == null || valor == DBNull.Value)
                return DateTime.Now;

            // DateOnly (columna DATE de PostgreSQL en .NET 6+)
            if (valor is DateOnly d)
                return d.ToDateTime(TimeOnly.MinValue);

            // DateTime clásico
            if (valor is DateTime dt)
                return dt;

            // Fallback
            try { return Convert.ToDateTime(valor); }
            catch { return DateTime.Now; }
        }

        // ═══════════════════════════════════════════
        // CARGAR EVENTOS
        // ═══════════════════════════════════════════
        private void CargarEventos()
        {
            flpEventos.Controls.Clear();
            flpEventos.SuspendLayout();

            List<EventoItem> eventos = new List<EventoItem>();

            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();

                // 🔥 SQL más simple: subconsultas en lugar de JOIN + GROUP BY
                string sql = @"
            SELECT 
                e.id,
                e.titulo,
                e.fecha,
                e.horario,
                e.tipo_evento,
                e.detalles,
                e.hora_inicio,
                e.hora_fin,
                COALESCE(
                    (SELECT string_agg(l.nombre_lugar, ', ')
                     FROM lugares_evento l 
                     WHERE l.id_evento = e.id), 
                    ''
                ) AS ubicacion,
                COALESCE(
                    (SELECT string_agg(l.referencia, ', ')
                     FROM lugares_evento l 
                     WHERE l.id_evento = e.id AND l.referencia IS NOT NULL AND l.referencia <> ''), 
                    ''
                ) AS referencia,
                COALESCE(
                    (SELECT string_agg(eq.nombre, ', ')
                     FROM asignaciones_equipos ae
                     INNER JOIN equipos eq ON eq.id = ae.id_equipo
                     WHERE ae.id_evento = e.id), 
                    ''
                ) AS equipos
            FROM eventos e
            WHERE 1=1";

                // Filtro de búsqueda
                if (!string.IsNullOrWhiteSpace(txtBuscar.Text) &&
                    !txtBuscar.Text.StartsWith("Buscar"))
                {
                    sql += @" AND (
                        e.titulo ILIKE @busqueda 
                        OR e.detalles ILIKE @busqueda 
                        OR e.tipo_evento ILIKE @busqueda
                        OR EXISTS (
                            SELECT 1 FROM lugares_evento l2 
                            WHERE l2.id_evento = e.id 
                              AND l2.nombre_lugar ILIKE @busqueda
                        )
                    )";
                }

                sql += " ORDER BY e.fecha DESC LIMIT 100";

                System.Diagnostics.Debug.WriteLine("SQL Eventos: " + sql);

                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, con))
                {
                    if (!string.IsNullOrWhiteSpace(txtBuscar.Text) &&
                        !txtBuscar.Text.StartsWith("Buscar"))
                    {
                        cmd.Parameters.AddWithValue("@busqueda", "%" + txtBuscar.Text.Trim() + "%");
                    }

                    using (NpgsqlDataReader rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            // 🔥 Usa el helper
                            DateTime fecha = ConvertirFecha(rd["fecha"]);

                            eventos.Add(new EventoItem
                            {
                                Id = Convert.ToInt32(rd["id"]),
                                Titulo = rd["titulo"]?.ToString() ?? "Sin título",
                                Fecha = fecha,
                                HoraInicio = FormatearHora(rd["hora_inicio"]),
                                HoraFin = FormatearHora(rd["hora_fin"]),
                                TipoEvento = rd["tipo_evento"]?.ToString() ?? "",
                                Descripcion = rd["detalles"]?.ToString() ?? "",
                                Ubicacion = rd["ubicacion"]?.ToString() ?? "",
                                Referencia = rd["referencia"]?.ToString() ?? "",
                                Equipos = rd["equipos"]?.ToString() ?? "",
                                Estado = CalcularEstado(fecha)
                            });
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Eventos cargados: {eventos.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ERROR EVENTOS: " + ex.Message);
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine("DETALLE: " + ex.InnerException.Message);

                // 🔥 Mostrar el error en pantalla temporalmente para debug
                MessageBox.Show(
                    "Error al cargar eventos:\n\n" + ex.Message +
                    (ex.InnerException != null ? "\n\n" + ex.InnerException.Message : ""),
                    "Debug",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }

            // Filtro por estado (cliente)
            if (cboFiltroEstado != null && cboFiltroEstado.SelectedIndex > 0)
            {
                string filtro = cboFiltroEstado.SelectedItem.ToString();
                eventos = eventos.FindAll(e => e.Estado == filtro);
            }

            if (eventos.Count == 0)
            {
                MostrarVacio();
            }
            else
            {
                foreach (var ev in eventos)
                    flpEventos.Controls.Add(CrearTarjetaEvento(ev));
            }

            flpEventos.ResumeLayout();
        }

        private string FormatearHora(object valor)
        {
            if (valor == null || valor == DBNull.Value) return "--:--";

            if (valor is TimeOnly to) return to.ToString("HH:mm");
            if (valor is TimeSpan ts) return ts.ToString(@"hh\:mm");

            string str = valor.ToString();
            if (TimeSpan.TryParse(str, out TimeSpan tsParse))
                return tsParse.ToString(@"hh\:mm");

            return str;
        }

        private string CalcularEstado(DateTime fecha)
        {
            if (fecha.Date == DateTime.Today) return "En curso";
            if (fecha.Date > DateTime.Today) return "Programado";
            return "Finalizado";
        }

        private void MostrarVacio()
        {
            Panel item = new Panel
            {
                Width = 900,
                Height = 120,
                Margin = new Padding(0, 20, 0, 0),
                BackColor = bgCard
            };
            AplicarBordesRedondeados(item, 10);

            item.Controls.Add(new Label
            {
                Text = "📭  No hay eventos registrados",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = textMuted,
                Dock = DockStyle.Top,
                Height = 60,
                TextAlign = ContentAlignment.MiddleCenter
            });

            item.Controls.Add(new Label
            {
                Text = "Crea uno desde el calendario del Panel de Control",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(100, 100, 120),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopCenter
            });

            flpEventos.Controls.Add(item);
        }

        // ═══════════════════════════════════════════
        // TARJETA DE EVENTO
        // ═══════════════════════════════════════════
        private Panel CrearTarjetaEvento(EventoItem ev)
        {
            Panel card = new Panel
            {
                Width = 900,
                Height = 220,
                Margin = new Padding(0, 0, 0, 12),
                BackColor = bgCard
            };
            AplicarBordesRedondeados(card, 12);

            // Borde sutil
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(45, 45, 60), 1.2f))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var p = ObtenerCaminoRedondeado(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 12))
                        e.Graphics.DrawPath(pen, p);
                }
            };

            // ═══════════════════════════════════
            // MINI-MAPA (izquierda) — simulado
            // ═══════════════════════════════════
            Panel panelMapa = new Panel
            {
                Size = new Size(220, 190),
                Location = new Point(15, 15),
                BackColor = bgMapPlaceholder
            };
            AplicarBordesRedondeados(panelMapa, 10);

            // Dibujar "mapa" simulado con marcador
            panelMapa.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                // Grid sutil (líneas tipo mapa)
                using (var gridPen = new Pen(Color.FromArgb(30, 40, 60), 1f))
                {
                    for (int i = 20; i < panelMapa.Width; i += 30)
                        e.Graphics.DrawLine(gridPen, i, 0, i, panelMapa.Height);
                    for (int i = 20; i < panelMapa.Height; i += 30)
                        e.Graphics.DrawLine(gridPen, 0, i, panelMapa.Width, i);
                }

                // Círculo de "ubicación" con anillo
                int cx = panelMapa.Width / 2;
                int cy = panelMapa.Height / 2;

                using (var glow = new SolidBrush(Color.FromArgb(40, 240, 90, 110)))
                    e.Graphics.FillEllipse(glow, cx - 30, cy - 30, 60, 60);

                using (var ring = new Pen(accentRed, 2f))
                    e.Graphics.DrawEllipse(ring, cx - 22, cy - 22, 44, 44);

                // Pin del mapa
                using (var pin = new SolidBrush(accentRed))
                    e.Graphics.FillEllipse(pin, cx - 8, cy - 8, 16, 16);

                using (var pinInner = new SolidBrush(Color.White))
                    e.Graphics.FillEllipse(pinInner, cx - 3, cy - 3, 6, 6);

                // Texto debajo
                TextRenderer.DrawText(
                    e.Graphics,
                    "📍 Ver en mapa",
                    new Font("Segoe UI", 8f, FontStyle.Bold),
                    new Rectangle(0, panelMapa.Height - 28, panelMapa.Width, 20),
                    Color.FromArgb(200, 200, 220),
                    TextFormatFlags.HorizontalCenter);
            };

            panelMapa.Cursor = Cursors.Hand;
            panelMapa.Click += (s, e) => AbrirMapa(ev.Ubicacion, ev.Referencia);

            card.Controls.Add(panelMapa);

            // ═══════════════════════════════════
            // BADGE DE ESTADO (arriba a la derecha)
            // ═══════════════════════════════════
            Color colorEstado = accentPurple;
            string iconoEstado = "📅";
            if (ev.Estado == "En curso") { colorEstado = accentGreen; iconoEstado = "🟢"; }
            else if (ev.Estado == "Finalizado") { colorEstado = accentYellow; iconoEstado = "✅"; }

            Panel badge = new Panel
            {
                Size = new Size(110, 26),
                Location = new Point(card.Width - 130, 20),
                BackColor = colorEstado
            };
            AplicarBordesRedondeados(badge, 13);
            badge.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            badge.Controls.Add(new Label
            {
                Text = iconoEstado + "  " + ev.Estado.ToUpper(),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            });
            card.Controls.Add(badge);

            // ═══════════════════════════════════
            // TÍTULO
            // ═══════════════════════════════════
            Label lblTitulo = new Label
            {
                Text = ev.Titulo,
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(250, 20),
                Size = new Size(card.Width - 400, 26),
                AutoEllipsis = true,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            card.Controls.Add(lblTitulo);

            // ═══════════════════════════════════
            // TIPO DE EVENTO
            // ═══════════════════════════════════
            if (!string.IsNullOrWhiteSpace(ev.TipoEvento))
            {
                Panel chipTipo = new Panel
                {
                    Size = new Size(140, 22),
                    Location = new Point(252, 50),
                    BackColor = Color.FromArgb(45, 35, 80)
                };
                AplicarBordesRedondeados(chipTipo, 11);
                chipTipo.Controls.Add(new Label
                {
                    Text = "🏷  " + ev.TipoEvento,
                    Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                    ForeColor = accentPurple,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent
                });
                card.Controls.Add(chipTipo);
            }

            // ═══════════════════════════════════
            // FECHA Y HORA
            // ═══════════════════════════════════
            Label lblFecha = new Label
            {
                Text = "📅  " + ev.Fecha.ToString("dddd, dd 'de' MMMM 'de' yyyy", new CultureInfo("es-ES")),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = accentCyan,
                Location = new Point(252, 82),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblFecha);

            Label lblHora = new Label
            {
                Text = "🕒  " + ev.HoraInicio + " - " + ev.HoraFin,
                Font = new Font("Segoe UI", 9f),
                ForeColor = textMuted,
                Location = new Point(252, 104),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblHora);

            // ═══════════════════════════════════
            // UBICACIÓN
            // ═══════════════════════════════════
            Label lblUbicacion = new Label
            {
                Text = "📍  " + (string.IsNullOrWhiteSpace(ev.Ubicacion) ? "Sin ubicación" : ev.Ubicacion),
                Font = new Font("Segoe UI", 9f),
                ForeColor = string.IsNullOrWhiteSpace(ev.Ubicacion) ? textMuted : textWhite,
                Location = new Point(252, 126),
                Size = new Size(card.Width - 400, 18),
                AutoEllipsis = true,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            card.Controls.Add(lblUbicacion);

            // ═══════════════════════════════════
            // DESCRIPCIÓN
            // ═══════════════════════════════════
            if (!string.IsNullOrWhiteSpace(ev.Descripcion))
            {
                Label lblDesc = new Label
                {
                    Text = "📝  " + ev.Descripcion,
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = textMuted,
                    Location = new Point(252, 148),
                    Size = new Size(card.Width - 400, 32),
                    AutoEllipsis = true,
                    BackColor = Color.Transparent,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                card.Controls.Add(lblDesc);
            }

            // ═══════════════════════════════════
            // EQUIPOS (chip resumen)
            // ═══════════════════════════════════
            if (!string.IsNullOrWhiteSpace(ev.Equipos))
            {
                int cantEquipos = ev.Equipos.Split(',').Length;
                Label lblEquipos = new Label
                {
                    Text = "🎬  " + cantEquipos + " equipos asignados",
                    Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                    ForeColor = accentYellow,
                    Location = new Point(252, 182),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                card.Controls.Add(lblEquipos);
            }

            // ═══════════════════════════════════
            // HOVER
            // ═══════════════════════════════════
            card.Cursor = Cursors.Hand;
            card.MouseEnter += (s, e) => card.BackColor = bgCardHover;
            card.MouseLeave += (s, e) => card.BackColor = bgCard;
            card.Click += (s, e) => AbrirDetalleEvento(ev);

            // Propagar el click a los hijos
            foreach (Control c in card.Controls)
            {
                if (!(c is Panel p && p.BackColor == bgMapPlaceholder) &&
                    !(c is Panel b && b.BackColor != bgCard && b.BackColor != Color.Transparent))
                {
                    c.Cursor = Cursors.Hand;
                    c.Click += (s, e) => AbrirDetalleEvento(ev);
                }
            }

            return card;
        }

        private void AbrirMapa(string ubicacion, string referencia)
        {
            string texto = string.IsNullOrWhiteSpace(ubicacion) ? "Sin ubicación" : ubicacion;
            MessageBox.Show(
                $"📍  Ubicación del evento\n\n" +
                $"Lugar: {texto}\n" +
                $"Referencia: {(string.IsNullOrWhiteSpace(referencia) ? "—" : referencia)}\n\n" +
                $"Aquí se abriría el mapa con la ruta.",
                "Ver en mapa",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void AbrirDetalleEvento(EventoItem ev)
        {
            MessageBox.Show(
                $"📅  {ev.Titulo}\n\n" +
                $"Tipo: {ev.TipoEvento}\n" +
                $"Fecha: {ev.Fecha:dd/MM/yyyy}\n" +
                $"Hora: {ev.HoraInicio} - {ev.HoraFin}\n" +
                $"Ubicación: {ev.Ubicacion}\n" +
                $"Equipos: {ev.Equipos}\n\n" +
                $"Descripción:\n{ev.Descripcion}",
                "Detalle del evento",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // ═══════════════════════════════════════════
        // UTILIDAD
        // ═══════════════════════════════════════════
        private GraphicsPath ObtenerCaminoRedondeado(Rectangle rect, int radio)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radio * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void AplicarBordesRedondeados(Control control, int radio)
        {
            Action recalcularRegion = () =>
            {
                if (control.Width <= 0 || control.Height <= 0) return;
                using (GraphicsPath path = ObtenerCaminoRedondeado(new Rectangle(0, 0, control.Width, control.Height), radio))
                    control.Region = new Region(path);
            };
            control.Resize += (s, e) => recalcularRegion();
            recalcularRegion();
        }
    }
}